using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Enlisted.Features.Schedule.Models;
using Enlisted.Mod.Core.Logging;
using Newtonsoft.Json;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;
using SystemPath = System.IO.Path;
using SystemFile = System.IO.File;

namespace Enlisted.Features.Schedule.Events
{
    internal static class SchedulePopupEventCatalogLoader
    {
        private const string LogCategory = "SchedulePopupEvents";
        private const string ConfigFileName = "schedule_popup_events.json";
        private const int SupportedSchemaVersion = 1;

        private static SchedulePopupEventCatalog _catalog;
        private static bool _loaded;

        public static SchedulePopupEventDefinition TryPickFor(ScheduledBlock block)
        {
            if (block == null)
            {
                return null;
            }

            EnsureLoaded();

            var events = _catalog?.Events;
            if (events == null || events.Count == 0)
            {
                return null;
            }

            // Prefer activity-specific events.
            var activityId = (block.ActivityId ?? string.Empty).Trim();
            var byActivity = string.IsNullOrWhiteSpace(activityId)
                ? new List<SchedulePopupEventDefinition>()
                : events.Where(e => IsActivityMatch(e, activityId)).ToList();

            if (byActivity.Count > 0)
            {
                return PickWeighted(byActivity);
            }

            // Fallback to block-type events.
            var byBlockType = events.Where(e => IsBlockTypeMatch(e, block.BlockType)).ToList();
            if (byBlockType.Count > 0)
            {
                return PickWeighted(byBlockType);
            }

            return null;
        }

        public static SkillObject TryResolveSkill(string rawSkill)
        {
            if (string.IsNullOrWhiteSpace(rawSkill))
            {
                return null;
            }

            var normalized = NormalizeSkillName(rawSkill);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return null;
            }

            try
            {
                return MBObjectManager.Instance.GetObject<SkillObject>(normalized);
            }
            catch
            {
                ModLogger.LogOnce(
                    key: $"sched_popup_unknown_skill:{rawSkill}",
                    category: LogCategory,
                    message: $"Unknown skill referenced by schedule popup events: '{rawSkill}' (normalized: '{normalized}')");
                return null;
            }
        }

        private static void EnsureLoaded()
        {
            if (_loaded)
            {
                return;
            }

            _loaded = true;

            try
            {
                var configPath = FindConfigFile();
                if (string.IsNullOrWhiteSpace(configPath) || !SystemFile.Exists(configPath))
                {
                    ModLogger.Info(LogCategory, $"No {ConfigFileName} found; schedule popup events will use hardcoded fallbacks.");
                    _catalog = new SchedulePopupEventCatalog();
                    return;
                }

                var json = SystemFile.ReadAllText(configPath);
                var parsed = JsonConvert.DeserializeObject<SchedulePopupEventCatalog>(json) ?? new SchedulePopupEventCatalog();

                if (parsed.SchemaVersion != SupportedSchemaVersion)
                {
                    ModLogger.Warn(LogCategory,
                        $"Unsupported {ConfigFileName} schemaVersion={parsed.SchemaVersion} (expected {SupportedSchemaVersion}). Using empty catalog.");
                    _catalog = new SchedulePopupEventCatalog();
                    return;
                }

                // Light normalization/validation: keep only entries that can ever match.
                var cleaned = new List<SchedulePopupEventDefinition>();
                foreach (var e in parsed.Events ?? new List<SchedulePopupEventDefinition>())
                {
                    if (e == null)
                    {
                        continue;
                    }

                    e.Id = (e.Id ?? string.Empty).Trim();
                    e.ActivityId = (e.ActivityId ?? string.Empty).Trim();
                    e.BlockType = (e.BlockType ?? string.Empty).Trim();
                    e.TitleId = (e.TitleId ?? string.Empty).Trim();
                    e.TitleFallback = e.TitleFallback ?? string.Empty;
                    e.BodyId = (e.BodyId ?? string.Empty).Trim();
                    e.BodyFallback = e.BodyFallback ?? string.Empty;
                    e.Skill = (e.Skill ?? string.Empty).Trim();

                    if (string.IsNullOrWhiteSpace(e.Id))
                    {
                        continue;
                    }

                    // Require at least one selector so authors don't accidentally create global spam.
                    if (string.IsNullOrWhiteSpace(e.ActivityId) && string.IsNullOrWhiteSpace(e.BlockType))
                    {
                        ModLogger.Warn(LogCategory, $"Skipping schedule popup event '{e.Id}': no activity_id or block_type specified.");
                        continue;
                    }

                    if (e.Weight <= 0f)
                    {
                        e.Weight = 1f;
                    }

                    cleaned.Add(e);
                }

                parsed.Events = cleaned;
                _catalog = parsed;

                ModLogger.Info(LogCategory, $"Loaded {_catalog.Events.Count} schedule popup events from: {configPath}");
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, $"Failed to load {ConfigFileName}; using empty catalog.", ex);
                _catalog = new SchedulePopupEventCatalog();
            }
        }

        private static string FindConfigFile()
        {
            // Mirror ScheduleConfigLoader path behavior so installs are predictable.
            string[] possiblePaths =
            {
                SystemPath.Combine(Utilities.GetBasePath(), "Modules", "Enlisted", "ModuleData", "Enlisted", ConfigFileName),
                SystemPath.Combine(Utilities.GetBasePath(), "Modules", "Enlisted", ConfigFileName),
                SystemPath.Combine("ModuleData", "Enlisted", ConfigFileName)
            };

            foreach (var path in possiblePaths)
            {
                if (SystemFile.Exists(path))
                {
                    return path;
                }
            }

            return null;
        }

        private static SchedulePopupEventDefinition PickWeighted(List<SchedulePopupEventDefinition> candidates)
        {
            if (candidates == null || candidates.Count == 0)
            {
                return null;
            }

            // Avoid pathological values; keep selection deterministic-ish per frame by using MBRandom.
            float total = 0f;
            foreach (var c in candidates)
            {
                if (c == null)
                {
                    continue;
                }

                total += Math.Max(0.001f, c.Weight);
            }

            if (total <= 0f)
            {
                return candidates[0];
            }

            var roll = MBRandom.RandomFloat * total;
            float acc = 0f;
            foreach (var c in candidates)
            {
                if (c == null)
                {
                    continue;
                }

                acc += Math.Max(0.001f, c.Weight);
                if (roll <= acc)
                {
                    return c;
                }
            }

            return candidates[candidates.Count - 1];
        }

        private static bool IsActivityMatch(SchedulePopupEventDefinition e, string activityId)
        {
            if (e == null || string.IsNullOrWhiteSpace(e.ActivityId) || string.IsNullOrWhiteSpace(activityId))
            {
                return false;
            }

            return string.Equals(e.ActivityId.Trim(), activityId.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsBlockTypeMatch(SchedulePopupEventDefinition e, ScheduleBlockType blockType)
        {
            if (e == null || string.IsNullOrWhiteSpace(e.BlockType))
            {
                return false;
            }

            // Support "WorkDetail", "work_detail", "workdetail" etc.
            var a = NormalizeKey(e.BlockType);
            var b = NormalizeKey(blockType.ToString());
            return !string.IsNullOrWhiteSpace(a) && a == b;
        }

        private static string NormalizeKey(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                return string.Empty;
            }

            var chars = s.Trim().Where(char.IsLetterOrDigit).ToArray();
            return new string(chars).ToLowerInvariant();
        }

        private static string NormalizeSkillName(string raw)
        {
            // Keep this mapping consistent with LanceLifeEventCatalogLoader / LanceLifeEventEffectsApplier.
            // This lets authors use human-friendly keys in JSON.
            var s = (raw ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(s))
            {
                return string.Empty;
            }

            return s.ToLowerInvariant() switch
            {
                // Combat
                "one_handed" or "onehanded" => "OneHanded",
                "two_handed" or "twohanded" => "TwoHanded",
                "polearm" => "Polearm",
                "throwing" => "Throwing",
                "bow" => "Bow",
                "crossbow" => "Crossbow",

                // Movement
                "riding" => "Riding",
                "athletics" => "Athletics",

                // Cunning
                "scouting" => "Scouting",
                "tactics" => "Tactics",
                "roguery" => "Roguery",

                // Social
                "charm" => "Charm",
                "leadership" => "Leadership",
                "trade" => "Trade",

                // Intelligence
                "steward" => "Steward",
                "medicine" => "Medicine",
                "engineering" => "Engineering",
                "smithing" or "crafting" => "Smithing",

                // Passthrough for PascalCase ids
                _ => s
            };
        }
    }
}


