using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Enlisted.Features.Assignments.Behaviors;
using Enlisted.Features.Conditions;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;
using Enlisted.Mod.Core.Triggers;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;
using EnlistedConfig = Enlisted.Features.Assignments.Core.ConfigurationManager;

namespace Enlisted.Features.Activities
{
    /// <summary>
    /// Phase 2 (menu_system_update.md): Camp Activities menu.
    ///
    /// Goals:
    /// - Data-driven activities (JSON) so we can add/remove without code.
    /// - All player-facing text localized via enlisted_strings.xml (TextObject {=id}).
    /// - Safe persistence for simple per-activity cooldown tracking.
    /// </summary>
    public sealed class CampActivitiesBehavior : CampaignBehaviorBase
    {
        private const string LogCategory = "Activities";

        public static CampActivitiesBehavior Instance { get; private set; }

        // Per-activity cooldown tracking (day number of last completion).
        // Use int to keep save container definitions simple and version-stable.
        // Synced via SyncData, not SaveableField
        private Dictionary<string, int> _lastCompletedDayByActivityId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        private List<CampActivityDefinition> _cachedDefinitions;

        public CampActivitiesBehavior()
        {
            Instance = this;
        }

        public override void RegisterEvents()
        {
            // No event subscriptions required. Activities are driven from the menu.
        }

        public override void SyncData(IDataStore dataStore)
        {
            SaveLoadDiagnostics.SafeSyncData(this, dataStore, () =>
            {
                dataStore.SyncData("ca_lastCompletedDayById", ref _lastCompletedDayByActivityId);
                _lastCompletedDayByActivityId ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            });
        }

        public bool IsEnabled()
        {
            return EnlistedConfig.LoadCampActivitiesConfig()?.Enabled == true;
        }

        public IReadOnlyList<CampActivityDefinition> GetAllActivities()
        {
            if (_cachedDefinitions != null)
            {
                return _cachedDefinitions;
            }

            _cachedDefinitions = LoadAndNormalizeDefinitions();
            return _cachedDefinitions;
        }

        public int GetAvailableActivityCountForCurrentContext()
        {
            try
            {
                if (!IsEnabled())
                {
                    return 0;
                }

                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment?.IsEnlisted != true)
                {
                    return 0;
                }

                var dayPart = CampaignTriggerTrackerBehavior.Instance?.GetDayPart();
                var dayPartToken = dayPart?.ToString().ToLowerInvariant() ?? "day";
                var formation = EnlistedDutiesBehavior.Instance?.GetPlayerFormationType()?.ToLowerInvariant() ?? "infantry";

                var currentDay = (int)CampaignTime.Now.ToDays;
                var defs = GetAllActivities();
                var count = 0;
                foreach (var def in defs)
                {
                    if (!IsActivityVisibleFor(def, enlistment, formation, dayPartToken))
                    {
                        continue;
                    }

                    if (!IsActivityEnabledFor(def, enlistment, currentDay, out _))
                    {
                        continue;
                    }

                    count++;
                }

                return count;
            }
            catch
            {
                return 0;
            }
        }

        public bool TryExecuteActivity(CampActivityDefinition def, out string failureReasonTextId)
        {
            failureReasonTextId = null;

            if (def == null)
            {
                failureReasonTextId = "act_fail_invalid";
                return false;
            }

            if (!IsEnabled())
            {
                failureReasonTextId = "act_fail_disabled";
                return false;
            }

            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment?.IsEnlisted != true)
            {
                failureReasonTextId = "act_fail_not_enlisted";
                return false;
            }

            var currentDay = (int)CampaignTime.Now.ToDays;
            if (!IsActivityEnabledFor(def, enlistment, currentDay, out failureReasonTextId))
            {
                return false;
            }

            // Apply fatigue cost first.
            if (def.FatigueCost > 0)
            {
                if (enlistment.FatigueCurrent < def.FatigueCost)
                {
                    failureReasonTextId = "act_fail_too_fatigued";
                    return false;
                }

                if (!enlistment.TryConsumeFatigue(def.FatigueCost, $"activity:{def.Id}"))
                {
                    failureReasonTextId = "act_fail_too_fatigued";
                    return false;
                }
            }

            // Apply fatigue relief (optional).
            if (def.FatigueRelief > 0)
            {
                enlistment.RestoreFatigue(def.FatigueRelief, $"activity:{def.Id}");
            }

            // Apply skill XP (data-driven).
            if (def.SkillXp != null && def.SkillXp.Count > 0)
            {
                foreach (var kvp in def.SkillXp)
                {
                    if (kvp.Value <= 0)
                    {
                        continue;
                    }

                    var skill = ResolveSkill(kvp.Key);
                    if (skill == null)
                    {
                        ModLogger.LogOnce(
                            key: $"camp_activity_unknown_skill:{def.Id}:{kvp.Key}",
                            category: LogCategory,
                            message: $"Unknown skill referenced by camp activity '{def.Id}': {kvp.Key}");
                        continue;
                    }

                    Hero.MainHero.AddSkillXp(skill, kvp.Value);
                }
            }

            // Record cooldown (day number) after successful completion.
            _lastCompletedDayByActivityId[def.Id] = currentDay;

            // Light feedback (localized).
            var msg = new TextObject("{=act_completed}Activity completed.");
            InformationManager.DisplayMessage(new InformationMessage(msg.ToString(), Colors.White));
            return true;
        }

        public bool TryGetCooldownDaysRemaining(CampActivityDefinition def, int currentDay, out int daysRemaining)
        {
            daysRemaining = 0;
            if (def == null || def.CooldownDays <= 0)
            {
                return false;
            }

            if (!_lastCompletedDayByActivityId.TryGetValue(def.Id, out var lastDay))
            {
                return false;
            }

            var readyDay = lastDay + def.CooldownDays;
            if (currentDay >= readyDay)
            {
                return false;
            }

            daysRemaining = Math.Max(1, readyDay - currentDay);
            return true;
        }

        public static bool IsActivityVisibleFor(CampActivityDefinition def, EnlistmentBehavior enlistment, string formation, string dayPartToken)
        {
            if (def == null || enlistment == null)
            {
                return false;
            }

            if (enlistment.EnlistmentTier < def.MinTier)
            {
                return false;
            }

            if (def.Formations != null && def.Formations.Count > 0)
            {
                if (string.IsNullOrWhiteSpace(formation) ||
                    !def.Formations.Any(f => string.Equals(f, formation, StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }
            }

            if (def.DayParts != null && def.DayParts.Count > 0)
            {
                if (string.IsNullOrWhiteSpace(dayPartToken) ||
                    !def.DayParts.Any(d => string.Equals(d, dayPartToken, StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsActivityEnabledFor(CampActivityDefinition def, EnlistmentBehavior enlistment, int currentDay,
            out string failureReasonTextId)
        {
            failureReasonTextId = null;

            if (def == null || enlistment == null)
            {
                failureReasonTextId = "act_fail_invalid";
                return false;
            }

            // Optional condition gating (mainly for training-like activities).
            var cond = PlayerConditionBehavior.Instance;
            if (def.BlockOnSevereCondition && cond != null && cond.IsEnabled())
            {
                if (!cond.CanTrain())
                {
                    failureReasonTextId = "act_fail_condition";
                    return false;
                }
            }

            if (TryGetCooldownDaysRemaining(def, currentDay, out _))
            {
                failureReasonTextId = "act_fail_cooldown";
                return false;
            }

            return true;
        }

        public TextObject GetActivityText(CampActivityDefinition def)
        {
            if (def == null)
            {
                return new TextObject(string.Empty);
            }

            if (!string.IsNullOrWhiteSpace(def.TextId))
            {
                return new TextObject($"{{={def.TextId}}}{def.TextFallback ?? string.Empty}");
            }

            return new TextObject(def.TextFallback ?? string.Empty);
        }

        public TextObject GetActivityHintText(CampActivityDefinition def)
        {
            if (def == null)
            {
                return new TextObject(string.Empty);
            }

            if (!string.IsNullOrWhiteSpace(def.HintId))
            {
                return new TextObject($"{{={def.HintId}}}{def.HintFallback ?? string.Empty}");
            }

            return new TextObject(def.HintFallback ?? string.Empty);
        }

        private static SkillObject ResolveSkill(string skillName)
        {
            if (string.IsNullOrWhiteSpace(skillName))
            {
                return null;
            }

            try
            {
                // SkillObject IDs are the StringId keys (e.g., "Charm", "Roguery").
                return MBObjectManager.Instance.GetObject<SkillObject>(skillName);
            }
            catch
            {
                return null;
            }
        }

        private static List<CampActivityDefinition> LoadAndNormalizeDefinitions()
        {
            try
            {
                var cfg = EnlistedConfig.LoadCampActivitiesConfig();
                var relPath = cfg?.DefinitionsFile;
                if (string.IsNullOrWhiteSpace(relPath))
                {
                    relPath = "Activities\\activities.json";
                }

                var path = EnlistedConfig.GetModuleDataPathForConsumers(relPath);
                if (!File.Exists(path))
                {
                    ModLogger.Warn(LogCategory, $"Activities definitions not found: {path}");
                    return new List<CampActivityDefinition>();
                }

                var json = File.ReadAllText(path);
                var parsed = JsonConvert.DeserializeObject<CampActivitiesDefinitionsJson>(json);
                if (parsed?.SchemaVersion != 1)
                {
                    ModLogger.Warn(LogCategory, $"Unsupported activities schemaVersion: {parsed?.SchemaVersion}");
                    return new List<CampActivityDefinition>();
                }

                var result = new List<CampActivityDefinition>();
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var a in parsed.Activities ?? new List<CampActivityJson>())
                {
                    if (a == null || string.IsNullOrWhiteSpace(a.Id))
                    {
                        continue;
                    }

                    if (!seen.Add(a.Id))
                    {
                        ModLogger.Warn(LogCategory, $"Duplicate activity id skipped: {a.Id}");
                        continue;
                    }

                    var def = new CampActivityDefinition
                    {
                        Id = a.Id.Trim(),
                        Category = (a.Category ?? string.Empty).Trim(),
                        Location = (a.Location ?? "camp_fire").Trim(), // Default to camp_fire if not specified
                        TextId = (a.TextId ?? string.Empty).Trim(),
                        TextFallback = a.Text ?? string.Empty,
                        HintId = (a.HintId ?? string.Empty).Trim(),
                        HintFallback = a.Hint ?? string.Empty,
                        MinTier = Math.Max(1, a.MinTier),
                        MaxTier = a.MaxTier, // 0 = no limit
                        RequiresLanceLeader = a.RequiresLanceLeader,
                        Formations = a.Formations?.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList() ??
                                     new List<string>(),
                        DayParts = a.DayParts?.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList() ??
                                   new List<string>(),
                        FatigueCost = Math.Max(0, a.FatigueCost),
                        FatigueRelief = Math.Max(0, a.FatigueRelief),
                        CooldownDays = Math.Max(0, a.CooldownDays),
                        SkillXp = a.SkillXp ?? new Dictionary<string, int>(),
                        BlockOnSevereCondition = a.BlockOnSevereCondition
                    };

                    result.Add(def);
                }

                ModLogger.Info(LogCategory, $"Loaded {result.Count} camp activities from JSON");
                return result;
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Failed to load activities definitions", ex);
                return new List<CampActivityDefinition>();
            }
        }
    }

    /// <summary>
    /// Save container definitions needed for CampActivitiesBehavior.
    /// </summary>
    public sealed class CampActivitiesSaveDefiner : SaveableTypeDefiner
    {
        public CampActivitiesSaveDefiner() : base(735202) { }

        protected override void DefineContainerDefinitions()
        {
            ConstructContainerDefinition(typeof(Dictionary<string, int>));
        }
    }
}


