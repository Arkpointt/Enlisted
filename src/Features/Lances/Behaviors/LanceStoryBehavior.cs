using System;
using System.Collections.Generic;
using System.Linq;
using Enlisted.Features.Assignments.Core;
using Enlisted.Features.Camp;
using Enlisted.Features.Conditions;
using Enlisted.Features.Escalation;
using Enlisted.Features.Lances.Personas;
using Enlisted.Features.Lances.Text;
using EnlistedConfig = Enlisted.Features.Assignments.Core.ConfigurationManager;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Lances.Stories;
using Enlisted.Features.Schedule.Models;
using Enlisted.Mod.Core.Logging;
using Enlisted.Mod.Core.Triggers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace Enlisted.Features.Lances.Behaviors
{
    /// <summary>
    /// Manages text-based lance activities.
    ///
    /// This system runs only while the player is enlisted and is gated by configuration 
    /// and player tier. It presents occasional story popups with choices that can 
    /// award skill XP, fatigue, or gold. The system is designed to be easily 
    /// extendable via JSON and can be disabled or removed through settings.
    /// </summary>
    public sealed class LanceStoryBehavior : CampaignBehaviorBase
    {
        private const string LogCategory = "LanceLife";

        // Persisted: per-story cooldown and per-week limiter
        private readonly Dictionary<string, CampaignTime> _storyLastFired = new Dictionary<string, CampaignTime>();
        private readonly Dictionary<string, int> _storyFiredThisTerm = new Dictionary<string, int>();
        private CampaignTime _trackedEnlistmentDate = CampaignTime.Zero;
        private CampaignTime _nextGlobalEligibleTime = CampaignTime.Zero;
        private int _storiesFiredThisWeek;
        private int _storiesFiredWeekNumber;

        private LanceLifeStoryCatalog _cachedCatalog;

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, _ => RefreshCache());
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            SaveLoadDiagnostics.SafeSyncData(this, dataStore, () =>
            {
                // Persist global limiter
                dataStore.SyncData("ll_nextGlobalEligibleTime", ref _nextGlobalEligibleTime);
                dataStore.SyncData("ll_storiesFiredThisWeek", ref _storiesFiredThisWeek);
                dataStore.SyncData("ll_storiesFiredWeekNumber", ref _storiesFiredWeekNumber);
                dataStore.SyncData("ll_trackedEnlistmentDate", ref _trackedEnlistmentDate);

                // Persist per-story last-fired timestamps
                var keys = _storyLastFired.Keys.ToList();
                var count = keys.Count;
                dataStore.SyncData("ll_storyCount", ref count);

                if (dataStore.IsLoading)
                {
                    _storyLastFired.Clear();
                    for (var i = 0; i < count; i++)
                    {
                        var id = string.Empty;
                        var time = CampaignTime.Zero;
                        dataStore.SyncData($"ll_story_{i}_id", ref id);
                        dataStore.SyncData($"ll_story_{i}_time", ref time);
                        if (!string.IsNullOrWhiteSpace(id))
                        {
                            _storyLastFired[id] = time;
                        }
                    }
                }
                else
                {
                    for (var i = 0; i < keys.Count; i++)
                    {
                        var id = keys[i];
                        var time = _storyLastFired[id];
                        dataStore.SyncData($"ll_story_{i}_id", ref id);
                        dataStore.SyncData($"ll_story_{i}_time", ref time);
                    }
                }

                // Persist per-term counts (only for stories we've incremented, to avoid save bloat)
                var termKeys = _storyFiredThisTerm.Keys.ToList();
                var termCount = termKeys.Count;
                dataStore.SyncData("ll_termStoryCount", ref termCount);

                if (dataStore.IsLoading)
                {
                    _storyFiredThisTerm.Clear();
                    for (var i = 0; i < termCount; i++)
                    {
                        var id = string.Empty;
                        var value = 0;
                        dataStore.SyncData($"ll_term_story_{i}_id", ref id);
                        dataStore.SyncData($"ll_term_story_{i}_count", ref value);
                        if (!string.IsNullOrWhiteSpace(id))
                        {
                            _storyFiredThisTerm[id] = Math.Max(0, value);
                        }
                    }
                }
                else
                {
                    for (var i = 0; i < termKeys.Count; i++)
                    {
                        var id = termKeys[i];
                        var value = _storyFiredThisTerm[id];
                        dataStore.SyncData($"ll_term_story_{i}_id", ref id);
                        dataStore.SyncData($"ll_term_story_{i}_count", ref value);
                    }
                }
            });
        }

        private void RefreshCache()
        {
            _cachedCatalog = LanceLifeStoryPackLoader.LoadCatalog();
        }

        private static bool IsEnabled()
        {
            // Lance Life gating is intentionally separate from lance identity gating. The player can have a lance
            // identity without stories being enabled.
            var lancesEnabled = EnlistedConfig.LoadLancesConfig()?.LancesEnabled == true;
            var enabled = EnlistedConfig.LoadLanceLifeConfig()?.Enabled == true;
            return lancesEnabled && enabled;
        }

        private void OnDailyTick()
        {
            try
            {
                if (!IsEnabled())
                {
                    return;
                }

                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment?.IsEnlisted != true)
                {
                    return;
                }

                var cfg = EnlistedConfig.LoadLanceLifeConfig() ?? new LanceLifeConfig();
                var tier = enlistment.EnlistmentTier;
                if (tier < Math.Max(1, cfg.MinTier))
                {
                    return;
                }

                if (cfg.RequireFinalLance && enlistment.IsLanceProvisional)
                {
                    return;
                }

                // Reset per-term counts when a new enlistment term starts.
                var enlistmentDate = enlistment.EnlistmentDate;
                if (enlistmentDate != _trackedEnlistmentDate)
                {
                    _trackedEnlistmentDate = enlistmentDate;
                    _storyFiredThisTerm.Clear();
                }

                // Weekly limiter bookkeeping (reset even if we end up firing a threshold story today).
                var weekNumber = (int)(CampaignTime.Now.ToDays / 7f);
                if (weekNumber != _storiesFiredWeekNumber)
                {
                    _storiesFiredWeekNumber = weekNumber;
                    _storiesFiredThisWeek = 0;
                }

                // Only fire when safe: not in battle, encounter, or captivity (same guard as deferred bag check)
                var main = MobileParty.MainParty;
                bool inBattle = main?.Party?.MapEvent != null;
                bool inEncounter = PlayerEncounter.Current != null;
                bool isPrisoner = Hero.MainHero?.IsPrisoner == true;
                if (inBattle || inEncounter || isPrisoner)
                {
                    return;
                }

                _cachedCatalog ??= LanceLifeStoryPackLoader.LoadCatalog();
                if (_cachedCatalog?.Stories == null || _cachedCatalog.Stories.Count == 0)
                {
                    return;
                }

                var disabledIds = new HashSet<string>(cfg.DisabledStoryIds ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
                var disabledCategories = new HashSet<string>(cfg.DisabledCategories ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
                var disabledPacks = new HashSet<string>(cfg.DisabledPackIds ?? new List<string>(), StringComparer.OrdinalIgnoreCase);

                // Threshold events are consequence popups and should not be blocked by the normal Lance Life
                // frequency caps. Rate limiting still applies through the daily tick and the EscalationManager's
                // own cooldown rules.
                var escalation = EscalationManager.Instance;
                if (escalation?.IsEnabled() == true)
                {
                    escalation.EvaluateThresholdsAndQueueIfNeeded();
                    var pendingId = escalation.PendingThresholdStoryId;
                    if (!string.IsNullOrWhiteSpace(pendingId))
                    {
                        var thresholdStory = _cachedCatalog.Stories.FirstOrDefault(s =>
                            s != null && string.Equals(s.Id, pendingId, StringComparison.OrdinalIgnoreCase));

                        if (thresholdStory == null)
                        {
                            escalation.ClearPendingThresholdStory();
                        }
                        else if (IsEligible(thresholdStory, enlistment, tier, cfg, disabledIds, disabledCategories, disabledPacks) &&
                                 !IsOnCooldown(thresholdStory.Id, thresholdStory.CooldownDays) &&
                                 !IsAtTermLimit(thresholdStory))
                        {
                            ShowStoryInquiry(thresholdStory, enlistment);

                            // Update limiters immediately to prevent double-fires.
                            _storiesFiredThisWeek++;
                            _storyLastFired[thresholdStory.Id] = CampaignTime.Now;
                            IncrementTermCount(thresholdStory.Id);
                            _nextGlobalEligibleTime = CampaignTime.Now + CampaignTime.Days(Math.Max(0, cfg.MinDaysBetweenStories));

                            escalation.MarkThresholdStoryFired(thresholdStory.Id);

                            var whyThreshold = BuildWhyTriggeredSummary(thresholdStory);
                            ModLogger.Info(LogCategory,
                                $"Threshold story fired: {thresholdStory.Id} (tier={tier}, pack={thresholdStory.PackId}, cat={thresholdStory.Category}, lance={enlistment.CurrentLanceName}, why={whyThreshold})");
                            return;
                        }
                    }
                }

                if (_storiesFiredThisWeek >= Math.Max(0, cfg.MaxStoriesPerWeek))
                {
                    return;
                }

                if (_nextGlobalEligibleTime.IsFuture)
                {
                    return;
                }

                var candidates = _cachedCatalog.Stories
                    .Where(s => s != null)
                    .Where(s => IsEligible(s, enlistment, tier, cfg, disabledIds, disabledCategories, disabledPacks))
                    .Where(s => !IsOnCooldown(s.Id, s.CooldownDays))
                    .Where(s => !IsAtTermLimit(s))
                    .ToList();

                if (candidates.Count == 0)
                {
                    return;
                }

                // Keep it simple for now: pick 1 candidate at random.
                // (Later: weight by camp conditions, lance storyId, role hint, etc.)
                var picked = candidates[MBRandom.RandomInt(candidates.Count)];
                if (picked == null)
                {
                    return;
                }

                ShowStoryInquiry(picked, enlistment);

                // Update limiters immediately to prevent double-fires.
                _storiesFiredThisWeek++;
                _storyLastFired[picked.Id] = CampaignTime.Now;
                IncrementTermCount(picked.Id);
                _nextGlobalEligibleTime = CampaignTime.Now + CampaignTime.Days(Math.Max(0, cfg.MinDaysBetweenStories));

                var why = BuildWhyTriggeredSummary(picked);
                ModLogger.Info(LogCategory,
                    $"Story fired: {picked.Id} (tier={tier}, pack={picked.PackId}, cat={picked.Category}, lance={enlistment.CurrentLanceName}, why={why})");
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Error during daily lance story evaluation", ex);
            }
        }

        private bool IsEligible(
            LanceLifeStoryDefinition story,
            EnlistmentBehavior enlistment,
            int tier,
            LanceLifeConfig cfg,
            HashSet<string> disabledIds,
            HashSet<string> disabledCategories,
            HashSet<string> disabledPacks)
        {
            if (story == null || enlistment == null)
            {
                return false;
            }

            if (disabledIds.Contains(story.Id ?? string.Empty))
            {
                return false;
            }

            if (disabledCategories.Contains(story.Category ?? string.Empty))
            {
                return false;
            }

            if (disabledPacks.Contains(story.PackId ?? string.Empty))
            {
                return false;
            }

            if (tier < Math.Max(1, story.TierMin) || tier > Math.Max(1, story.TierMax))
            {
                return false;
            }

            if ((cfg?.RequireFinalLance == true || story.RequireFinalLance) && enlistment.IsLanceProvisional)
            {
                return false;
            }

            // Player conditions can restrict training stories.
            // Keep this minimal and predictable: only severe+ injury/illness blocks training category.
            if (string.Equals(story.Category, "training", StringComparison.OrdinalIgnoreCase))
            {
                var cond = PlayerConditionBehavior.Instance;
                if (cond?.IsEnabled() == true && !cond.CanTrain())
                {
                    return false;
                }
            }

            return AreTriggersSatisfied(story);
        }

        private bool IsAtTermLimit(LanceLifeStoryDefinition story)
        {
            if (story == null)
            {
                return true;
            }

            if (story.MaxPerTerm <= 0)
            {
                return false;
            }

            if (!_storyFiredThisTerm.TryGetValue(story.Id, out var count))
            {
                return false;
            }

            return count >= story.MaxPerTerm;
        }

        private void IncrementTermCount(string storyId)
        {
            if (string.IsNullOrWhiteSpace(storyId))
            {
                return;
            }

            if (!_storyFiredThisTerm.TryGetValue(storyId, out var count))
            {
                count = 0;
            }

            _storyFiredThisTerm[storyId] = count + 1;
        }

        private bool AreTriggersSatisfied(LanceLifeStoryDefinition story)
        {
            if (story == null)
            {
                return false;
            }

            // If triggers are omitted/empty, treat as always eligible.
            var all = story.TriggerAll ?? new List<string>();
            var any = story.TriggerAny ?? new List<string>();

            foreach (var token in all)
            {
                if (!IsTriggerTokenTrue(token))
                {
                    return false;
                }
            }

            if (any.Count > 0)
            {
                return any.Any(IsTriggerTokenTrue);
            }

            return true;
        }

        private bool IsTriggerTokenTrue(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            var tracker = CampaignTriggerTrackerBehavior.Instance;
            if (tracker == null)
            {
                return false;
            }

            const float eventWindowDays = 1f;

            switch (token.Trim().ToLowerInvariant())
            {
                // 4-block schedule
                case CampaignTriggerTokens.Dawn:
                case CampaignTriggerTokens.Morning:
                    return tracker.GetTimeBlock() == TimeBlock.Morning;
                case CampaignTriggerTokens.Afternoon:
                    return tracker.GetTimeBlock() == TimeBlock.Afternoon;
                case CampaignTriggerTokens.Evening:
                case CampaignTriggerTokens.Dusk:
                    return tracker.GetTimeBlock() == TimeBlock.Dusk;
                case CampaignTriggerTokens.Night:
                    return tracker.GetTimeBlock() == TimeBlock.Night;
                
                // Legacy "day" token - map to all daytime periods for backwards compatibility
                case CampaignTriggerTokens.Day:
                {
                    var block = tracker.GetTimeBlock();
                    return block == TimeBlock.Morning || block == TimeBlock.Afternoon;
                }

                case CampaignTriggerTokens.EnteredSettlement:
                    return tracker.IsWithinDays(tracker.LastSettlementEnteredTime, eventWindowDays);
                case CampaignTriggerTokens.EnteredTown:
                    return tracker.IsWithinDays(tracker.LastTownEnteredTime, eventWindowDays);
                case CampaignTriggerTokens.EnteredCastle:
                    return tracker.IsWithinDays(tracker.LastCastleEnteredTime, eventWindowDays);
                case CampaignTriggerTokens.EnteredVillage:
                    return tracker.IsWithinDays(tracker.LastVillageEnteredTime, eventWindowDays);
                case CampaignTriggerTokens.LeftSettlement:
                    return tracker.IsWithinDays(tracker.LastSettlementLeftTime, eventWindowDays);

                case CampaignTriggerTokens.LeavingBattle:
                    return tracker.IsWithinDays(tracker.LastMapEventEndedTime, eventWindowDays);

                case CampaignTriggerTokens.LogisticsHigh:
                    return CampLifeBehavior.Instance?.IsLogisticsHigh() == true;
                case CampaignTriggerTokens.MoraleLow:
                    return CampLifeBehavior.Instance?.IsMoraleLow() == true;
                case CampaignTriggerTokens.PayTensionHigh:
                    return CampLifeBehavior.Instance?.IsPayTensionHigh() == true;
                case CampaignTriggerTokens.HeatHigh:
                    return CampLifeBehavior.Instance?.IsHeatHigh() == true;

                case CampaignTriggerTokens.Heat3:
                    return EscalationManager.Instance?.IsEnabled() == true && EscalationManager.Instance.State.Heat >= 3;
                case CampaignTriggerTokens.Heat5:
                    return EscalationManager.Instance?.IsEnabled() == true && EscalationManager.Instance.State.Heat >= 5;
                case CampaignTriggerTokens.Heat7:
                    return EscalationManager.Instance?.IsEnabled() == true && EscalationManager.Instance.State.Heat >= 7;
                case CampaignTriggerTokens.Heat10:
                    return EscalationManager.Instance?.IsEnabled() == true && EscalationManager.Instance.State.Heat >= 10;

                case CampaignTriggerTokens.Discipline3:
                    return EscalationManager.Instance?.IsEnabled() == true && EscalationManager.Instance.State.Discipline >= 3;
                case CampaignTriggerTokens.Discipline5:
                    return EscalationManager.Instance?.IsEnabled() == true && EscalationManager.Instance.State.Discipline >= 5;
                case CampaignTriggerTokens.Discipline7:
                    return EscalationManager.Instance?.IsEnabled() == true && EscalationManager.Instance.State.Discipline >= 7;
                case CampaignTriggerTokens.Discipline10:
                    return EscalationManager.Instance?.IsEnabled() == true && EscalationManager.Instance.State.Discipline >= 10;

                case CampaignTriggerTokens.LanceRep20:
                    return EscalationManager.Instance?.IsEnabled() == true && EscalationManager.Instance.State.LanceReputation >= 20;
                case CampaignTriggerTokens.LanceRep40:
                    return EscalationManager.Instance?.IsEnabled() == true && EscalationManager.Instance.State.LanceReputation >= 40;
                case CampaignTriggerTokens.LanceRepNeg20:
                    return EscalationManager.Instance?.IsEnabled() == true && EscalationManager.Instance.State.LanceReputation <= -20;
                case CampaignTriggerTokens.LanceRepNeg40:
                    return EscalationManager.Instance?.IsEnabled() == true && EscalationManager.Instance.State.LanceReputation <= -40;

                case CampaignTriggerTokens.Medical3:
                    return EscalationManager.Instance?.IsEnabled() == true && EscalationManager.Instance.State.MedicalRisk >= 3;
                case CampaignTriggerTokens.Medical4:
                    return EscalationManager.Instance?.IsEnabled() == true && EscalationManager.Instance.State.MedicalRisk >= 4;
                case CampaignTriggerTokens.Medical5:
                    return EscalationManager.Instance?.IsEnabled() == true && EscalationManager.Instance.State.MedicalRisk >= 5;

                case CampaignTriggerTokens.HasInjury:
                    return PlayerConditionBehavior.Instance?.IsEnabled() == true && PlayerConditionBehavior.Instance.State.HasInjury;
                case CampaignTriggerTokens.HasIllness:
                    return PlayerConditionBehavior.Instance?.IsEnabled() == true && PlayerConditionBehavior.Instance.State.HasIllness;
                case CampaignTriggerTokens.HasCondition:
                    return PlayerConditionBehavior.Instance?.IsEnabled() == true && PlayerConditionBehavior.Instance.State.HasAnyCondition;

                default:
                    // Recognized-but-unimplemented tokens are treated as false (the loader already warns once).
                    return false;
            }
        }

        private string BuildWhyTriggeredSummary(LanceLifeStoryDefinition story)
        {
            try
            {
                if (story == null)
                {
                    return "unknown";
                }

                var tracker = CampaignTriggerTrackerBehavior.Instance;
                if (tracker == null)
                {
                    return "no_tracker";
                }

                // Keep this small and stable (INFO line). This is called only when a story fires,
                // and stories are already rate-limited (weekly cap + min days between).
                var timeBlock = tracker.GetTimeBlock().ToString();
                var all = story.TriggerAll ?? new List<string>();
                var any = story.TriggerAny ?? new List<string>();

                var allEval = all.Count == 0 ? "-" : string.Join(",", all.Select(t => $"{t}={(IsTriggerTokenTrue(t) ? "1" : "0")}"));
                var anyEval = any.Count == 0 ? "-" : string.Join(",", any.Select(t => $"{t}={(IsTriggerTokenTrue(t) ? "1" : "0")}"));

                // Provide one high-signal anchor about recent history without dumping a lot of state.
                var enteredId = tracker.LastSettlementEnteredId;
                var enteredAgeDays = tracker.LastSettlementEnteredTime == CampaignTime.Zero
                    ? -1f
                    : (CampaignTime.Now.ToDays - tracker.LastSettlementEnteredTime.ToDays);

                return $"timeblock={timeBlock};all=[{allEval}];any=[{anyEval}];lastEnter={enteredId};enterAgeDays={enteredAgeDays:0.0}";
            }
            catch (Exception ex)
            {
                // Never let diagnostics break the flow; this method is best-effort.
                ModLogger.Debug(LogCategory, $"BuildWhyTriggeredSummary failed: {ex.GetType().Name}: {ex.Message}");
                return "diag_error";
            }
        }

        private bool IsOnCooldown(string storyId, int cooldownDays)
        {
            if (string.IsNullOrWhiteSpace(storyId))
            {
                return true;
            }

            if (cooldownDays <= 0)
            {
                return false;
            }

            if (!_storyLastFired.TryGetValue(storyId, out var last))
            {
                return false;
            }

            var next = last + CampaignTime.Days(cooldownDays);
            return CampaignTime.Now < next;
        }

        private void ShowStoryInquiry(LanceLifeStoryDefinition story, EnlistmentBehavior enlistment)
        {
            // Viking Conquest-style: a short body and a few choices.
            // We use MultiSelectionInquiryData so each choice can have a tooltip/hint.
            try
            {
                // Localizable strings: each story/option can provide string IDs.
                // If the ID is missing or not found in language XML, Bannerlord falls back to the text we embed in the TextObject.
                var title = ResolveLocalizedString(story.TitleId, story.TitleFallback, "{=ll_default_title}Lance Activity", enlistment);
                var body = ResolveLocalizedString(story.BodyId, story.BodyFallback, string.Empty, enlistment);

                var options = new List<InquiryElement>();
                foreach (var option in story.Options ?? new List<LanceLifeOptionDefinition>())
                {
                    if (option == null)
                    {
                        continue;
                    }

                    var enabled = IsOptionEnabled(option, Hero.MainHero);
                    var optionText = ResolveLocalizedString(option.TextId, option.TextFallback, "{=ll_default_continue}Continue", enlistment);
                    var hint = ResolveLocalizedString(option.HintId, option.HintFallback, string.Empty, enlistment);
                    options.Add(new InquiryElement(option, optionText, null, enabled, hint));
                }

                if (options.Count == 0)
                {
                    return;
                }

                var inquiry = new MultiSelectionInquiryData(
                    titleText: title,
                    descriptionText: body,
                    inquiryElements: options,
                    isExitShown: true,
                    minSelectableOptionCount: 1,
                    maxSelectableOptionCount: 1,
                    affirmativeText: new TextObject("{=ll_inquiry_choose}Choose").ToString(),
                    negativeText: new TextObject("{=ll_inquiry_leave}Leave").ToString(),
                    affirmativeAction: selected =>
                    {
                        try
                        {
                            if (selected?.FirstOrDefault()?.Identifier is LanceLifeOptionDefinition chosen)
                            {
                                ApplyOption(chosen, enlistment);
                            }
                        }
                        catch (Exception ex)
                        {
                            ModLogger.Error(LogCategory, "Error applying lance story option", ex);
                        }
                    },
                    negativeAction: _ => { },
                    soundEventPath: string.Empty);

                MBInformationManager.ShowMultiSelectionInquiry(inquiry);
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Error showing lance story inquiry", ex);
            }
        }

        private static void ApplyOption(LanceLifeOptionDefinition option, EnlistmentBehavior enlistment)
        {
            if (option == null)
            {
                return;
            }

            // Costs
            if (option.CostFatigue > 0)
            {
                enlistment?.TryConsumeFatigue(option.CostFatigue, "lance_story");
            }

            if (option.CostGold > 0)
            {
                if (Hero.MainHero.Gold >= option.CostGold)
                {
                    GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, option.CostGold);
                }
                else
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("{=ll_not_enough_coin}You don't have enough coin.").ToString(), Colors.Red));
                    return;
                }
            }

            // NOTE: Heat/discipline meters are introduced in later phases; we parse the values now to keep packs forward-compatible.
            if (option.CostHeat != 0 || option.CostDiscipline != 0)
            {
                ModLogger.Debug(LogCategory, $"Unimplemented costs ignored (heat={option.CostHeat}, discipline={option.CostDiscipline})");
            }

            // Rewards
            if (option.RewardGold > 0)
            {
                GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, option.RewardGold);
            }

            if (option.RewardFatigueRelief > 0)
            {
                enlistment?.RestoreFatigue(option.RewardFatigueRelief, "lance_story");
            }

            if (option.RewardSkillXp != null && option.RewardSkillXp.Count > 0)
            {
                foreach (var kvp in option.RewardSkillXp)
                {
                    var skill = ResolveSkill(kvp.Key);
                    var amount = kvp.Value;
                    if (skill != null && amount > 0)
                    {
                        Hero.MainHero.AddSkillXp(skill, amount);
                    }
                }
            }

            // Escalation track effects (feature-flagged).
            // These are internal to Enlisted and are meant to create readable long-term consequences later.
            var escalation = EscalationManager.Instance;
            if (escalation?.IsEnabled() == true)
            {
                var reason = $"lance_story:{option.Id}";
                if (option.EffectHeat != 0)
                {
                    escalation.ModifyHeat(option.EffectHeat, reason);
                }
                if (option.EffectDiscipline != 0)
                {
                    escalation.ModifyDiscipline(option.EffectDiscipline, reason);
                }
                if (option.EffectLanceReputation != 0)
                {
                    escalation.ModifyLanceReputation(option.EffectLanceReputation, reason);
                }
                if (option.EffectMedicalRisk != 0)
                {
                    escalation.ModifyMedicalRisk(option.EffectMedicalRisk, reason);
                }
            }

            // Injury / illness rolls (feature-flagged).
            var conditions = PlayerConditionBehavior.Instance;
            if (conditions?.IsEnabled() == true)
            {
                TryApplyInjuryRoll(option, conditions);
                TryApplyIllnessRoll(option, conditions);
            }

            var resultText = ResolveLocalizedString(option.ResultTextId, option.ResultTextFallback, string.Empty, enlistment);
            if (!string.IsNullOrWhiteSpace(resultText))
            {
                InformationManager.DisplayMessage(new InformationMessage(resultText, Colors.White));
            }
        }

        private static void TryApplyInjuryRoll(LanceLifeOptionDefinition option, PlayerConditionBehavior conditions)
        {
            if (option == null || conditions == null)
            {
                return;
            }

            if (option.InjuryChance <= 0f || option.InjuryTypes == null || option.InjuryTypes.Count == 0)
            {
                return;
            }

            if (MBRandom.RandomFloat >= option.InjuryChance)
            {
                return;
            }

            var type = option.InjuryTypes[MBRandom.RandomInt(option.InjuryTypes.Count)];
            var severity = PickInjurySeverity(option.InjurySeverityWeights);
            var days = conditions.GetBaseRecoveryDaysForInjury(type, severity);
            conditions.TryApplyInjury(type, severity, days, $"story:{option.Id}");
        }

        private static void TryApplyIllnessRoll(LanceLifeOptionDefinition option, PlayerConditionBehavior conditions)
        {
            if (option == null || conditions == null)
            {
                return;
            }

            if (option.IllnessChance <= 0f || option.IllnessTypes == null || option.IllnessTypes.Count == 0)
            {
                return;
            }

            if (MBRandom.RandomFloat >= option.IllnessChance)
            {
                return;
            }

            var type = option.IllnessTypes[MBRandom.RandomInt(option.IllnessTypes.Count)];
            var severity = PickIllnessSeverity(option.IllnessSeverityWeights);
            var days = conditions.GetBaseRecoveryDaysForIllness(type, severity);
            conditions.TryApplyIllness(type, severity, days, $"story:{option.Id}");
        }

        private static InjurySeverity PickInjurySeverity(Dictionary<string, float> weights)
        {
            // Defaults: mostly minor.
            var wMinor = GetWeight(weights, "minor", 0.8f);
            var wModerate = GetWeight(weights, "moderate", 0.18f);
            var wSevere = GetWeight(weights, "severe", 0.02f);
            var wCritical = GetWeight(weights, "critical", 0.0f);

            var total = MathF.Max(0.0001f, wMinor + wModerate + wSevere + wCritical);
            var roll = MBRandom.RandomFloat * total;

            if ((roll -= wMinor) < 0f) return InjurySeverity.Minor;
            if ((roll -= wModerate) < 0f) return InjurySeverity.Moderate;
            if ((roll -= wSevere) < 0f) return InjurySeverity.Severe;
            return InjurySeverity.Critical;
        }

        private static IllnessSeverity PickIllnessSeverity(Dictionary<string, float> weights)
        {
            var wMild = GetWeight(weights, "mild", 0.75f);
            var wModerate = GetWeight(weights, "moderate", 0.2f);
            var wSevere = GetWeight(weights, "severe", 0.05f);
            var wCritical = GetWeight(weights, "critical", 0.0f);

            var total = MathF.Max(0.0001f, wMild + wModerate + wSevere + wCritical);
            var roll = MBRandom.RandomFloat * total;

            if ((roll -= wMild) < 0f) return IllnessSeverity.Mild;
            if ((roll -= wModerate) < 0f) return IllnessSeverity.Moderate;
            if ((roll -= wSevere) < 0f) return IllnessSeverity.Severe;
            return IllnessSeverity.Critical;
        }

        private static float GetWeight(Dictionary<string, float> weights, string key, float defaultValue)
        {
            if (weights == null || string.IsNullOrWhiteSpace(key))
            {
                return defaultValue;
            }

            return weights.TryGetValue(key, out var v) ? MathF.Max(0f, v) : defaultValue;
        }

        private static bool IsOptionEnabled(LanceLifeOptionDefinition option, Hero hero)
        {
            if (option == null || hero == null)
            {
                return false;
            }

            if (option.CostGold > 0 && hero.Gold < option.CostGold)
            {
                return false;
            }

            return true;
        }

        private static string ResolveLocalizedString(string textId, string fallbackText, string defaultText, EnlistmentBehavior enlistment)
        {
            // Prefer a provided ID, otherwise just return fallback/default.
            if (string.IsNullOrWhiteSpace(textId))
            {
                return !string.IsNullOrWhiteSpace(fallbackText) ? fallbackText : (defaultText ?? string.Empty);
            }

            var embeddedFallback = !string.IsNullOrWhiteSpace(fallbackText) ? fallbackText : (defaultText ?? string.Empty);
            var t = new TextObject("{=" + textId + "}" + embeddedFallback);
            ApplyCommonStoryTextVariables(t, enlistment);
            return t.ToString();
        }

        private static void ApplyCommonStoryTextVariables(TextObject text, EnlistmentBehavior enlistment)
        {
            LanceLifeTextVariables.ApplyCommon(text, enlistment);
        }

        private static SkillObject ResolveSkill(string skillName)
        {
            if (string.IsNullOrWhiteSpace(skillName))
            {
                return null;
            }

            try
            {
                return MBObjectManager.Instance.GetObject<SkillObject>(skillName);
            }
            catch
            {
                ModLogger.Warn(LogCategory, $"Unknown skill in Lance Life story pack: {skillName}");
                return null;
            }
        }
    }
}


