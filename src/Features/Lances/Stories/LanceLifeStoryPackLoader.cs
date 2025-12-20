using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Enlisted.Mod.Core.Logging;
using Enlisted.Mod.Core.Triggers;
using Newtonsoft.Json;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Enlisted.Features.Lances.Stories
{
    internal static class LanceLifeStoryPackLoader
    {
        private const string LogCategory = "LanceLife";

        private const int SupportedSchemaVersion = 1;

        public static LanceLifeStoryCatalog LoadCatalog()
        {
            var catalog = new LanceLifeStoryCatalog();

            try
            {
                var storyPackDir = ResolveStoryPackDirectory();
                if (string.IsNullOrWhiteSpace(storyPackDir) || !Directory.Exists(storyPackDir))
                {
                    ModLogger.Warn(LogCategory, $"Story pack directory not found: {storyPackDir}");
                    return catalog;
                }

                var files = Directory.GetFiles(storyPackDir, "*.json", SearchOption.TopDirectoryOnly)
                    .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (files.Count == 0)
                {
                    ModLogger.Warn(LogCategory, $"No story packs found in: {storyPackDir}");
                    return catalog;
                }

                var packIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var storyIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                var loadedPackCount = 0;
                var loadedStoryCount = 0;
                var skippedPackCount = 0;
                var skippedStoryCount = 0;

                // Collect warnings we want to emit once as a summary.
                var unimplementedTriggersUsed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var file in files)
                {
                    var pack = TryLoadPack(file);
                    if (pack == null)
                    {
                        skippedPackCount++;
                        continue;
                    }

                    if (pack.SchemaVersion != SupportedSchemaVersion)
                    {
                        ModLogger.Warn(LogCategory,
                            $"Skipping pack (unsupported schemaVersion={pack.SchemaVersion}, expected={SupportedSchemaVersion}): {file}");
                        skippedPackCount++;
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(pack.PackId))
                    {
                        ModLogger.Warn(LogCategory, $"Skipping pack (missing packId): {file}");
                        skippedPackCount++;
                        continue;
                    }

                    if (!packIds.Add(pack.PackId))
                    {
                        ModLogger.Warn(LogCategory, $"Skipping pack (duplicate packId={pack.PackId}): {file}");
                        skippedPackCount++;
                        continue;
                    }

                    loadedPackCount++;

                    foreach (var storyJson in pack.Stories ?? new List<LanceLifeStoryJson>())
                    {
                        if (storyJson == null)
                        {
                            skippedStoryCount++;
                            continue;
                        }

                        if (!TryNormalizeStory(pack.PackId, storyJson, out var story, out var reason,
                                out var unimplementedTriggers))
                        {
                            skippedStoryCount++;
                            ModLogger.Warn(LogCategory,
                                $"Skipping story in pack={pack.PackId} (reason={reason ?? "unknown"}): id={storyJson.Id ?? "null"}");
                            continue;
                        }

                        foreach (var token in unimplementedTriggers)
                        {
                            unimplementedTriggersUsed.Add(token);
                        }

                        if (!storyIds.Add(story.Id))
                        {
                            skippedStoryCount++;
                            ModLogger.Warn(LogCategory,
                                $"Skipping story (duplicate id={story.Id}) in pack={pack.PackId}");
                            continue;
                        }

                        catalog.Stories.Add(story);
                        loadedStoryCount++;
                    }
                }

                ModLogger.LogOnce(
                    key: "lancelife_pack_load_summary",
                    category: LogCategory,
                    message: $"Story packs loaded: packs={loadedPackCount}, stories={loadedStoryCount}, skippedPacks={skippedPackCount}, skippedStories={skippedStoryCount}");

                if (unimplementedTriggersUsed.Count > 0)
                {
                    var tokenList = string.Join(", ", unimplementedTriggersUsed.OrderBy(t => t, StringComparer.OrdinalIgnoreCase));
                    ModLogger.LogOnce(
                        key: "lancelife_unimplemented_triggers",
                        category: LogCategory,
                        message:
                        $"Some stories reference recognized but unimplemented trigger tokens (they will not fire yet): {tokenList}");
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Failed to load Lance Life story packs", ex);
            }

            return catalog;
        }

        public static string ResolveStoryPackDirectory()
        {
            // Reuse the existing ModuleData path resolution, then navigate to StoryPacks/LanceLife.
            // This keeps our file access stable across different Bannerlord installs/drives.
            var legacyFilePath = Enlisted.Features.Assignments.Core.ConfigurationManager.GetModuleDataPathForConsumers("lance_stories.json");
            if (string.IsNullOrWhiteSpace(legacyFilePath))
            {
                return null;
            }

            var moduleDataDir = Path.GetDirectoryName(legacyFilePath);
            if (string.IsNullOrWhiteSpace(moduleDataDir))
            {
                return null;
            }

            return Path.Combine(moduleDataDir, "StoryPacks", "LanceLife");
        }

        private static LanceLifeStoryPackJson TryLoadPack(string filePath)
        {
            try
            {
                var json = File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<LanceLifeStoryPackJson>(json);
            }
            catch (Exception ex)
            {
                ModLogger.Warn(LogCategory, $"Skipping pack (parse error): {filePath} | {ex.GetType().Name}: {ex.Message}");
                return null;
            }
        }

        private static bool TryNormalizeStory(
            string packId,
            LanceLifeStoryJson src,
            out LanceLifeStoryDefinition story,
            out string failureReason,
            out List<string> unimplementedTriggers)
        {
            story = null;
            failureReason = null;
            unimplementedTriggers = new List<string>();

            if (string.IsNullOrWhiteSpace(src.Id))
            {
                failureReason = "missing_id";
                return false;
            }

            if (string.IsNullOrWhiteSpace(src.Category))
            {
                failureReason = "missing_category";
                return false;
            }

            if (string.IsNullOrWhiteSpace(src.TitleId) || string.IsNullOrWhiteSpace(src.BodyId))
            {
                failureReason = "missing_titleId_or_bodyId";
                return false;
            }

            var options = src.Options ?? new List<LanceLifeStoryOptionJson>();
            if (options.Count < 2 || options.Count > 4)
            {
                failureReason = "options_count_out_of_range";
                return false;
            }

            var normalizedOptions = new List<LanceLifeOptionDefinition>();
            var sawSafeOption = false;
            foreach (var opt in options)
            {
                if (opt == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(opt.TextId))
                {
                    failureReason = "missing_option_textId";
                    return false;
                }

                var risk = NormalizeRisk(opt.Risk);
                if (risk == null)
                {
                    failureReason = "invalid_option_risk";
                    return false;
                }

                if (string.Equals(risk, "safe", StringComparison.OrdinalIgnoreCase))
                {
                    sawSafeOption = true;
                }

                // Validate skill names early (contract requirement). We fail the story if any skill key is invalid.
                if (opt.Rewards?.SkillXp != null && opt.Rewards.SkillXp.Count > 0)
                {
                    foreach (var kvp in opt.Rewards.SkillXp)
                    {
                        if (!IsValidSkillName(kvp.Key))
                        {
                            failureReason = $"invalid_skill:{kvp.Key}";
                            return false;
                        }
                    }
                }

                normalizedOptions.Add(new LanceLifeOptionDefinition
                {
                    Id = opt.Id ?? string.Empty,
                    TextId = opt.TextId,
                    HintId = opt.HintId,
                    TextFallback = opt.Text ?? string.Empty,
                    HintFallback = opt.Hint ?? string.Empty,
                    Risk = risk,
                    CostFatigue = Math.Max(0, opt.Costs?.Fatigue ?? 0),
                    CostGold = Math.Max(0, opt.Costs?.Gold ?? 0),
                    CostHeat = opt.Costs?.Heat ?? 0,
                    CostDiscipline = opt.Costs?.Discipline ?? 0,
                    EffectHeat = opt.Effects?.Heat ?? 0,
                    EffectDiscipline = opt.Effects?.Discipline ?? 0,
                    EffectLanceReputation = opt.Effects?.LanceReputation ?? 0,
                    EffectMedicalRisk = opt.Effects?.MedicalRisk ?? 0,

                    InjuryChance = NormalizeChance(opt.Injury?.Chance ?? 0f),
                    InjuryTypes = opt.Injury?.Types ?? new List<string>(),
                    InjurySeverityWeights = opt.Injury?.SeverityWeights ?? new Dictionary<string, float>(),

                    IllnessChance = NormalizeChance(opt.Illness?.Chance ?? 0f),
                    IllnessTypes = opt.Illness?.Types ?? new List<string>(),
                    IllnessSeverityWeights = opt.Illness?.SeverityWeights ?? new Dictionary<string, float>(),

                    RewardSkillXp = opt.Rewards?.SkillXp ?? new Dictionary<string, int>(),
                    RewardGold = opt.Rewards?.Gold ?? 0,
                    RewardFatigueRelief = Math.Max(0, opt.Rewards?.FatigueRelief ?? 0),
                    ResultTextId = opt.ResultTextId,
                    ResultTextFallback = opt.ResultText ?? string.Empty
                });
            }

            if (!sawSafeOption)
            {
                failureReason = "no_safe_option";
                return false;
            }

            var triggers = src.Triggers ?? new LanceLifeTriggersJson();
            var triggerAll = NormalizeTriggerList(triggers.All);
            var triggerAny = NormalizeTriggerList(triggers.Any);

            // Trigger validation: unknown tokens make the story invalid and we skip it. Recognized but unimplemented
            // tokens are allowed, but they will prevent the story from firing until the token is implemented.
            foreach (var token in triggerAll.Concat(triggerAny))
            {
                if (!CampaignTriggerTokens.IsRecognized(token))
                {
                    failureReason = $"unknown_trigger:{token}";
                    return false;
                }
                if (!CampaignTriggerTokens.IsImplemented(token))
                {
                    unimplementedTriggers.Add(token);
                }
            }

            story = new LanceLifeStoryDefinition
            {
                PackId = packId,
                Id = src.Id,
                Category = src.Category,
                Tags = src.Tags ?? new List<string>(),
                TitleId = src.TitleId,
                BodyId = src.BodyId,
                TitleFallback = src.Title ?? string.Empty,
                BodyFallback = src.Body ?? string.Empty,
                TierMin = Math.Max(1, src.TierMin),
                TierMax = Math.Max(1, src.TierMax),
                RequireFinalLance = src.RequireFinalLance,
                CooldownDays = Math.Max(0, src.CooldownDays),
                MaxPerTerm = Math.Max(0, src.MaxPerTerm),
                TriggerAll = triggerAll,
                TriggerAny = triggerAny,
                Options = normalizedOptions
            };

            // Ensure tier range is coherent.
            if (story.TierMax < story.TierMin)
            {
                failureReason = "tier_range_invalid";
                story = null;
                return false;
            }

            return true;
        }

        private static List<string> NormalizeTriggerList(List<string> src)
        {
            if (src == null || src.Count == 0)
            {
                return new List<string>();
            }

            return src
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string NormalizeRisk(string risk)
        {
            if (string.IsNullOrWhiteSpace(risk))
            {
                return null;
            }

            var v = risk.Trim().ToLowerInvariant();
            if (v == "safe" || v == "risky" || v == "corrupt")
            {
                return v;
            }
            return null;
        }

        private static float NormalizeChance(float chance)
        {
            if (chance <= 0f)
            {
                return 0f;
            }
            return chance >= 1f ? 1f : chance;
        }

        private static bool IsValidSkillName(string skillName)
        {
            if (string.IsNullOrWhiteSpace(skillName))
            {
                return false;
            }

            try
            {
                // SkillObject IDs are the StringId keys (e.g., "Charm", "Roguery").
                // This uses the in-game object database; if a key doesn't exist, it returns null.
                var obj = MBObjectManager.Instance?.GetObject<SkillObject>(skillName);
                return obj != null;
            }
            catch
            {
                // If the object system isn't ready, don't hard-fail content.
                // The runtime application step also validates and will warn if it can't resolve skills.
                return true;
            }
        }
    }
}


