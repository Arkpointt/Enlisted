using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Enlisted.Features.Assignments.Core;
using Enlisted.Mod.Core.Logging;
using Enlisted.Mod.Core.Triggers;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace Enlisted.Features.Lances.Events
{
    internal static class LanceLifeEventCatalogLoader
    {
        private const string LogCategory = "LanceLifeEvents";
        private const string SupportedSchemaVersion = "1";
        private const string SupportedLegacySchemaVersion = "1.0"; // For backwards compatibility with older "1.0" format

        /// <summary>
        /// Load the full events catalog from ModuleData/Enlisted/Events/*.json.
        /// This is loader + validation only; it does not schedule or fire events.
        /// </summary>
        public static LanceLifeEventCatalog LoadCatalog()
        {
            var catalog = new LanceLifeEventCatalog();

            try
            {
                var config = ConfigurationManager.LoadLanceLifeEventsConfig() ?? new LanceLifeEventsConfig();
                if (!config.Enabled)
                {
                    ModLogger.LogOnce(
                        key: "lancelife_events_disabled",
                        category: LogCategory,
                        message: "Lance Life Events are disabled by config (lance_life_events.enabled=false).");
                    return catalog;
                }

                var eventsDir = ResolveEventsDirectory(config);
                if (string.IsNullOrWhiteSpace(eventsDir) || !Directory.Exists(eventsDir))
                {
                    ModLogger.Warn(LogCategory, $"Events directory not found: {eventsDir}");
                    return catalog;
                }

                var files = Directory.GetFiles(eventsDir, "*.json", SearchOption.TopDirectoryOnly)
                    .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                // schema_version.json is a marker file, not an event file.
                files.RemoveAll(p => string.Equals(Path.GetFileName(p), "schema_version.json", StringComparison.OrdinalIgnoreCase));

                if (files.Count == 0)
                {
                    ModLogger.Warn(LogCategory, $"No event JSON files found in: {eventsDir}");
                    return catalog;
                }

                var localizationIds = TryLoadLocalizationIds(out var localizationSource);

                var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var recognizedButUnimplementedTokensUsed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var unrecognizedTokensUsed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var missingLocalizationIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // Avoid log spam: aggregate skip reasons per file and show a compact summary.
                var skippedByFile = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);
                var skipExamples = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase); // key = $"{file}|{reason}"

                var loadedEventCount = 0;
                var skippedEventCount = 0;
                var loadedFileCount = 0;
                var skippedFileCount = 0;

                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);
                    var fileJson = TryLoadEventFile(file);
                    if (fileJson == null)
                    {
                        skippedFileCount++;
                        continue;
                    }

                    if (!IsSupportedSchemaVersion(fileJson.SchemaVersion))
                    {
                        ModLogger.Warn(LogCategory,
                            $"Skipping events file (unsupported schemaVersion={fileJson.SchemaVersion}, expected={SupportedSchemaVersion}): {file}");
                        skippedFileCount++;
                        continue;
                    }

                    loadedFileCount++;

                    foreach (var raw in fileJson.Events ?? new List<LanceLifeEventDefinition>())
                    {
                        if (!TryNormalizeAndValidate(raw,
                                localizationIds,
                                missingLocalizationIds,
                                recognizedButUnimplementedTokensUsed,
                                unrecognizedTokensUsed,
                                out var normalized,
                                out var failureReason))
                        {
                            skippedEventCount++;
                            var reason = string.IsNullOrWhiteSpace(failureReason) ? "unknown" : failureReason.Trim();
                            RecordSkip(skippedByFile, skipExamples, fileName, reason, raw?.Id);
                            continue;
                        }

                        if (!seenIds.Add(normalized.Id))
                        {
                            skippedEventCount++;
                            RecordSkip(skippedByFile, skipExamples, fileName, "duplicate_id", normalized.Id);
                            continue;
                        }

                        catalog.Events.Add(normalized);
                        loadedEventCount++;
                    }
                }

                ModLogger.LogOnce(
                    key: "lancelife_events_load_summary",
                    category: LogCategory,
                    message:
                    $"Events loaded: files={loadedFileCount}, events={loadedEventCount}, skippedFiles={skippedFileCount}, skippedEvents={skippedEventCount}");

                if (skippedEventCount > 0)
                {
                    foreach (var kv in skippedByFile.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
                    {
                        var fileName = kv.Key;
                        var reasons = kv.Value ?? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                        if (reasons.Count == 0)
                        {
                            continue;
                        }

                        var reasonList = reasons
                            .OrderByDescending(r => r.Value)
                            .ThenBy(r => r.Key, StringComparer.OrdinalIgnoreCase)
                            .Select(r =>
                            {
                                var key = $"{fileName}|{r.Key}";
                                var examples = skipExamples.TryGetValue(key, out var ids) && ids.Count > 0
                                    ? $" (e.g. {string.Join(", ", ids)})"
                                    : string.Empty;
                                return $"{r.Key}={r.Value}{examples}";
                            });

                        // End-user friendly summary line per file (no spam).
                        ModLogger.Warn(LogCategory, $"Event load issues in {fileName}: {string.Join("; ", reasonList)}");
                    }
                }

                if (missingLocalizationIds.Count > 0)
                {
                    var list = string.Join(", ",
                        missingLocalizationIds
                            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                            .Take(25));
                    ModLogger.LogOnce(
                        key: "lancelife_events_missing_strings",
                        category: LogCategory,
                        message:
                        $"Some Lance Life Events reference missing localization string IDs (source={localizationSource}). Count={missingLocalizationIds.Count}. First={list}");
                }

                if (unrecognizedTokensUsed.Count > 0)
                {
                    var list = string.Join(", ",
                        unrecognizedTokensUsed
                            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                            .Take(25));
                    ModLogger.LogOnce(
                        key: "lancelife_events_unrecognized_tokens",
                        category: LogCategory,
                        message:
                        $"Some Lance Life Events reference unrecognized trigger tokens (they will never evaluate true). Count={unrecognizedTokensUsed.Count}. First={list}");
                }

                if (recognizedButUnimplementedTokensUsed.Count > 0)
                {
                    var list = string.Join(", ",
                        recognizedButUnimplementedTokensUsed
                            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                            .Take(25));
                    ModLogger.LogOnce(
                        key: "lancelife_events_unimplemented_tokens",
                        category: LogCategory,
                        message:
                        $"Some Lance Life Events reference recognized but unimplemented trigger tokens (they will evaluate false until implemented). Count={recognizedButUnimplementedTokensUsed.Count}. First={list}");
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Failed to load Lance Life Events catalog", ex);
            }

            return catalog;
        }

        private static void RecordSkip(
            Dictionary<string, Dictionary<string, int>> skippedByFile,
            Dictionary<string, List<string>> skipExamples,
            string fileName,
            string reason,
            string eventId)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = "(unknown-file)";
            }
            if (string.IsNullOrWhiteSpace(reason))
            {
                reason = "unknown";
            }

            if (!skippedByFile.TryGetValue(fileName, out var byReason))
            {
                byReason = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                skippedByFile[fileName] = byReason;
            }

            byReason[reason] = byReason.TryGetValue(reason, out var n) ? n + 1 : 1;

            if (string.IsNullOrWhiteSpace(eventId))
            {
                return;
            }

            var key = $"{fileName}|{reason}";
            if (!skipExamples.TryGetValue(key, out var ids))
            {
                ids = new List<string>();
                skipExamples[key] = ids;
            }

            // Keep a tiny sample for player-facing troubleshooting; avoid long lists.
            if (ids.Count < 3 && !ids.Contains(eventId, StringComparer.OrdinalIgnoreCase))
            {
                ids.Add(eventId);
            }
        }

        private static string ResolveEventsDirectory(LanceLifeEventsConfig config)
        {
            // Reuse the existing ModuleData path resolution, then navigate to the configured Events folder.
            var enlistedConfigPath = ConfigurationManager.GetModuleDataPathForConsumers("enlisted_config.json");
            if (string.IsNullOrWhiteSpace(enlistedConfigPath))
            {
                return null;
            }

            var moduleDataDir = Path.GetDirectoryName(enlistedConfigPath);
            if (string.IsNullOrWhiteSpace(moduleDataDir))
            {
                return null;
            }

            var folder = string.IsNullOrWhiteSpace(config?.EventsFolder) ? "Events" : config.EventsFolder.Trim();
            return Path.Combine(moduleDataDir, folder);
        }

        private static LanceLifeEventsPackJson TryLoadEventFile(string filePath)
        {
            try
            {
                var json = File.ReadAllText(filePath);

                // Support both legacy {schemaVersion:int, events:[...]} and schema packs {schemaVersion:"1.0", packId,...,events:[...]}.
                var root = JObject.Parse(json);
                var schemaToken = root["schemaVersion"];
                var schema = schemaToken?.Type switch
                {
                    JTokenType.Integer => ((int)schemaToken).ToString(),
                    JTokenType.Float => schemaToken.ToString(),
                    JTokenType.String => schemaToken.ToString(),
                    _ => string.Empty
                };

                // Try schema pack first with error logging
                var settings = new JsonSerializerSettings
                {
                    Error = (sender, args) =>
                    {
                        ModLogger.Warn(LogCategory,
                            $"JSON deserialize error in {filePath}: {args.ErrorContext.Error.Message} at path {args.ErrorContext.Path}");
                        args.ErrorContext.Handled = true; // Continue deserializing
                    }
                };
                var serializer = JsonSerializer.Create(settings);
                var pack = root.ToObject<LanceLifeEventsPackJson>(serializer) ?? new LanceLifeEventsPackJson();
                pack.SchemaVersion = string.IsNullOrWhiteSpace(pack.SchemaVersion) ? schema : pack.SchemaVersion;

                // Legacy fallback
                if (pack.Events == null)
                {
                    var legacy = root.ToObject<LanceLifeEventsFileJsonLegacy>();
                    if (legacy != null)
                    {
                        pack.SchemaVersion = legacy.SchemaVersion.ToString();
                        pack.Events = legacy.Events ?? new List<LanceLifeEventDefinition>();
                    }
                }

                return pack;
            }
            catch (Exception ex)
            {
                ModLogger.Warn(LogCategory, $"Skipping events file (parse error): {filePath} | {ex.GetType().Name}: {ex.Message}");
                return null;
            }
        }

        private static bool TryNormalizeAndValidate(
            LanceLifeEventDefinition src,
            HashSet<string> localizationIds,
            HashSet<string> missingLocalizationIds,
            HashSet<string> recognizedButUnimplementedTokensUsed,
            HashSet<string> unrecognizedTokensUsed,
            out LanceLifeEventDefinition normalized,
            out string failureReason)
        {
            normalized = null;
            failureReason = null;

            if (src == null)
            {
                failureReason = "null_event";
                return false;
            }

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

            src.Delivery ??= new LanceLifeEventDelivery();
            src.Triggers ??= new LanceLifeEventTriggers();
            src.Requirements ??= new LanceLifeEventRequirements();
            src.Timing ??= new LanceLifeEventTiming();
            src.Options ??= new List<LanceLifeEventOptionDefinition>();

            // Validate trigger tokens. We do not evaluate them here; this is vocabulary checking only.
            ValidateTokens(src.Triggers?.All, recognizedButUnimplementedTokensUsed, unrecognizedTokensUsed);
            ValidateTokens(src.Triggers?.Any, recognizedButUnimplementedTokensUsed, unrecognizedTokensUsed);
            ValidateTokens(src.Triggers?.TimeOfDay, recognizedButUnimplementedTokensUsed, unrecognizedTokensUsed);

            // Normalize schema nested content into engine-flat fields.
            NormalizeContent(src);
            NormalizeVariants(src);

            // Validate option count after content/variant normalization.
            var hasValidBaseOptions = src.Options.Count >= 2 && src.Options.Count <= 4;
            var hasValidVariantOptions = src.Variants != null &&
                                         src.Variants.Values.Any(v => v?.Options != null && v.Options.Count >= 2 && v.Options.Count <= 4);
            if (!hasValidBaseOptions && !hasValidVariantOptions)
            {
                // Debug: log details to help diagnose why options are out of range
                var contentOptCount = src.Content?.Options?.Count ?? 0;
                var baseOptCount = src.Options?.Count ?? 0;
                var variantInfo = src.Variants != null
                    ? string.Join(", ", src.Variants.Select(kvp => $"{kvp.Key}:{kvp.Value?.Options?.Count ?? 0}"))
                    : "none";
                ModLogger.Info(LogCategory,
                    $"Event '{src.Id}' rejected: baseOpts={baseOptCount}, contentOpts={contentOptCount}, variants=[{variantInfo}]");
                failureReason = "options_count_out_of_range";
                return false;
            }

            NormalizeOptions(src.Options);
            NormalizeDelivery(src);

            // Validate localization IDs if provided.
            ValidateLocalizationId(src.TitleId, localizationIds, missingLocalizationIds);
            ValidateLocalizationId(src.SetupId, localizationIds, missingLocalizationIds);
            foreach (var opt in src.Options)
            {
                if (opt == null)
                {
                    continue;
                }

                ValidateLocalizationId(opt.TextId, localizationIds, missingLocalizationIds);
                ValidateLocalizationId(opt.OutcomeTextId, localizationIds, missingLocalizationIds);
                ValidateLocalizationId(opt.OutcomeSuccessTextId, localizationIds, missingLocalizationIds);
                ValidateLocalizationId(opt.OutcomeFailureTextId, localizationIds, missingLocalizationIds);
            }

            // Variants can override options (especially onboarding). Validate those too.
            if (src.Variants != null && src.Variants.Count > 0)
            {
                foreach (var v in src.Variants.Values)
                {
                    if (v?.Options == null || v.Options.Count == 0)
                    {
                        continue;
                    }

                    foreach (var opt in v.Options)
                    {
                        if (opt == null)
                        {
                            continue;
                        }

                        ValidateLocalizationId(opt.TextId, localizationIds, missingLocalizationIds);
                        ValidateLocalizationId(opt.OutcomeTextId, localizationIds, missingLocalizationIds);
                        ValidateLocalizationId(opt.OutcomeSuccessTextId, localizationIds, missingLocalizationIds);
                        ValidateLocalizationId(opt.OutcomeFailureTextId, localizationIds, missingLocalizationIds);
                    }
                }
            }

            // Normalize required tiers.
            src.Requirements.Tier ??= new LanceLifeTierRange();
            src.Requirements.Tier.Min = Math.Max(1, src.Requirements.Tier.Min);
            src.Requirements.Tier.Max = Math.Max(src.Requirements.Tier.Min, src.Requirements.Tier.Max);

            normalized = src;
            return true;
        }

        private static void NormalizeDelivery(LanceLifeEventDefinition evt)
        {
            if (evt?.Delivery == null)
            {
                return;
            }

            var method = (evt.Delivery.Method ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(method))
            {
                method = "automatic";
            }
            evt.Delivery.Method = method;

            var channel = (evt.Delivery.Channel ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(channel))
            {
                channel = string.Equals(method, "player_initiated", StringComparison.OrdinalIgnoreCase) ? "menu" : "inquiry";
            }

            if (!string.Equals(channel, "menu", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(channel, "inquiry", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(channel, "incident", StringComparison.OrdinalIgnoreCase))
            {
                ModLogger.Warn(LogCategory,
                    $"Unrecognized delivery.channel '{evt.Delivery.Channel}' for event '{evt.Id}'. Falling back to 'inquiry'.");
                channel = "inquiry";
            }
            evt.Delivery.Channel = channel;

            // For menu channel, ensure method is player_initiated for consistency.
            if (string.Equals(channel, "menu", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(method, "player_initiated", StringComparison.OrdinalIgnoreCase))
            {
                evt.Delivery.Method = "player_initiated";
            }

            // Normalize incident trigger casing: keep the enum-like name, but allow empty/null.
            evt.Delivery.IncidentTrigger = (evt.Delivery.IncidentTrigger ?? string.Empty).Trim();

            // If channel=incident and trigger not set, leave it empty; runtime will treat as ineligible and loader will warn once.
            if (string.Equals(channel, "incident", StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrWhiteSpace(evt.Delivery.IncidentTrigger))
            {
                ModLogger.Warn(LogCategory,
                    $"Event '{evt.Id}' is channel=incident but has no delivery.incident_trigger. It will never fire until set.");
            }
        }

        private static void NormalizeContent(LanceLifeEventDefinition evt)
        {
            if (evt == null)
            {
                return;
            }

            // If schema content block is present, treat it as authoring truth.
            if (evt.Content != null)
            {
                if (string.IsNullOrWhiteSpace(evt.TitleId) && !string.IsNullOrWhiteSpace(evt.Content.TitleId))
                {
                    evt.TitleId = evt.Content.TitleId.Trim();
                }
                if (string.IsNullOrWhiteSpace(evt.SetupId) && !string.IsNullOrWhiteSpace(evt.Content.SetupId))
                {
                    evt.SetupId = evt.Content.SetupId.Trim();
                }

                if (string.IsNullOrWhiteSpace(evt.TitleFallback) && !string.IsNullOrWhiteSpace(evt.Content.Title))
                {
                    evt.TitleFallback = evt.Content.Title;
                }
                if (string.IsNullOrWhiteSpace(evt.SetupFallback) && !string.IsNullOrWhiteSpace(evt.Content.Setup))
                {
                    evt.SetupFallback = evt.Content.Setup;
                }

                // Debug: log the state before attempting to copy options
                var evtOptsBefore = evt.Options?.Count ?? -1;
                var contentOpts = evt.Content.Options?.Count ?? -1;

                if ((evt.Options == null || evt.Options.Count == 0) && evt.Content.Options != null && evt.Content.Options.Count > 0)
                {
                    evt.Options = evt.Content.Options;
                }
                else if (contentOpts == 0 && evtOptsBefore == 0)
                {
                    // Both are empty - Content.Options didn't deserialize correctly
                    ModLogger.Info(LogCategory,
                        $"NormalizeContent '{evt.Id}': Content.Options is empty (content deserialization may have failed)");
                }
            }
        }

        private static void NormalizeVariants(LanceLifeEventDefinition evt)
        {
            if (evt?.Variants == null || evt.Variants.Count == 0)
            {
                return;
            }

            foreach (var kvp in evt.Variants)
            {
                var v = kvp.Value;
                if (v == null)
                {
                    continue;
                }

                // Allow schema variant blocks to provide setup/options only; IDs/fallbacks are embedded in TextObject resolution.
                NormalizeOptions(v.Options);
            }
        }

        private static void NormalizeOptions(List<LanceLifeEventOptionDefinition> options)
        {
            if (options == null || options.Count == 0)
            {
                return;
            }

            foreach (var o in options)
            {
                if (o == null)
                {
                    continue;
                }

                // Schema outcome -> engine result text
                if (string.IsNullOrWhiteSpace(o.OutcomeTextFallback) && !string.IsNullOrWhiteSpace(o.SchemaOutcome))
                {
                    o.OutcomeTextFallback = o.SchemaOutcome;
                }

                // Schema risky outcome failure -> engine success/failure model
                if (!string.IsNullOrWhiteSpace(o.SchemaOutcomeFailure))
                {
                    if (!o.SuccessChance.HasValue && o.RiskChance.HasValue && o.RiskChance.Value > 0)
                    {
                        o.SuccessChance = Math.Max(0f, Math.Min(1f, o.RiskChance.Value / 100f));
                    }

                    // For schema conversion, we keep outcomes as fallbacks and let the applier decide which to show.
                    if (string.IsNullOrWhiteSpace(o.OutcomeSuccessTextId) && string.IsNullOrWhiteSpace(o.OutcomeFailureTextId))
                    {
                        // OutcomeTextFallback already holds "success" text; failure is kept in SchemaOutcomeFailure.
                    }
                }

                // Schema rewards.xp -> engine rewards.skillXp
                if (o.Rewards != null && (o.Rewards.SkillXp == null || o.Rewards.SkillXp.Count == 0) &&
                    o.Rewards.SchemaXp != null && o.Rewards.SchemaXp.Count > 0)
                {
                    o.Rewards.SkillXp = NormalizeSkillXp(o.Rewards.SchemaXp);
                }

                // Schema injury_risk normalization.
                if (o.Injury == null && o.Illness == null && o.InjuryRisk != null && o.InjuryRisk.Chance > 0)
                {
                    NormalizeInjuryRisk(o);
                }
            }
        }

        private static void NormalizeInjuryRisk(LanceLifeEventOptionDefinition option)
        {
            if (option?.InjuryRisk == null)
            {
                return;
            }

            var chance = Math.Max(0, option.InjuryRisk.Chance);
            if (chance <= 0)
            {
                return;
            }

            var severity = (option.InjuryRisk.Severity ?? string.Empty).Trim().ToLowerInvariant();
            var type = (option.InjuryRisk.Type ?? string.Empty).Trim().ToLowerInvariant();

            // Map schema "type" into our existing condition definitions.
            // Keep these in sync with ModuleData/Enlisted/Conditions/condition_defs.json.
            var injuryType = type switch
            {
                "strain" => "twisted_knee",
                "wound" => "blade_cut",
                "arrow" => "arrow_wound",
                _ => "blade_cut"
            };

            var illnessType = type switch
            {
                "illness" => "camp_fever",
                _ => string.Empty
            };

            var weightKey = severity switch
            {
                "minor" => "minor",
                "moderate" => "moderate",
                "severe" => "severe",
                "critical" => "critical",
                _ => "minor"
            };

            if (!string.IsNullOrWhiteSpace(illnessType))
            {
                option.Illness = new LanceLifeIllnessRoll
                {
                    Chance = Math.Min(1f, chance / 100f),
                    Types = new List<string> { illnessType },
                    SeverityWeights = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase)
                    {
                        [weightKey] = 1f
                    }
                };

                return;
            }

            option.Injury = new LanceLifeInjuryRoll
            {
                Chance = Math.Min(1f, chance / 100f),
                Types = new List<string> { injuryType },
                SeverityWeights = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase)
                {
                    [weightKey] = 1f
                }
            };
        }

        private static Dictionary<string, int> NormalizeSkillXp(Dictionary<string, int> xp)
        {
            var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (xp == null)
            {
                return result;
            }

            foreach (var kvp in xp)
            {
                var key = NormalizeSkillName(kvp.Key);
                var val = kvp.Value;
                if (!string.IsNullOrWhiteSpace(key) && val != 0)
                {
                    result[key] = val;
                }
            }

            return result;
        }

        private static string NormalizeSkillName(string raw)
        {
            var s = (raw ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(s))
            {
                return string.Empty;
            }

            // Common schema keys -> Bannerlord SkillObject StringId
            // Supports snake_case (one_handed), lowercase (onehanded), and PascalCase (OneHanded)
            return s.ToLowerInvariant() switch
            {
                // Combat skills
                "one_handed" or "onehanded" => "OneHanded",
                "two_handed" or "twohanded" => "TwoHanded",
                "polearm" => "Polearm",
                "throwing" => "Throwing",
                "bow" => "Bow",
                "crossbow" => "Crossbow",
                
                // Movement skills
                "riding" => "Riding",
                "athletics" => "Athletics",
                
                // Cunning skills
                "scouting" => "Scouting",
                "tactics" => "Tactics",
                "roguery" => "Roguery",
                
                // Social skills
                "charm" => "Charm",
                "leadership" => "Leadership",
                "trade" => "Trade",
                
                // Intelligence skills
                "steward" => "Steward",
                "medicine" => "Medicine",
                "engineering" => "Engineering",
                "smithing" or "crafting" => "Smithing",
                
                // Passthrough for PascalCase or already correct IDs
                _ => s
            };
        }

        private static bool IsSupportedSchemaVersion(string schema)
        {
            if (string.IsNullOrWhiteSpace(schema))
            {
                return false;
            }

            var normalized = schema.Trim();
            if (string.Equals(normalized, SupportedSchemaVersion, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Legacy string schema version support (e.g., "1.0")
            return string.Equals(normalized, SupportedLegacySchemaVersion, StringComparison.OrdinalIgnoreCase);
        }

        private static void ValidateTokens(
            List<string> tokens,
            HashSet<string> recognizedButUnimplementedTokensUsed,
            HashSet<string> unrecognizedTokensUsed)
        {
            if (tokens == null || tokens.Count == 0)
            {
                return;
            }

            foreach (var t in tokens)
            {
                if (string.IsNullOrWhiteSpace(t))
                {
                    continue;
                }

                var token = t.Trim();
                if (!CampaignTriggerTokens.IsRecognized(token))
                {
                    unrecognizedTokensUsed.Add(token);
                    continue;
                }

                if (!CampaignTriggerTokens.IsImplemented(token))
                {
                    recognizedButUnimplementedTokensUsed.Add(token);
                }
            }
        }

        private static void ValidateLocalizationId(string id, HashSet<string> localizationIds, HashSet<string> missingLocalizationIds)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return;
            }

            if (localizationIds == null || localizationIds.Count == 0)
            {
                // If we couldn't load localization, don't spam false negatives.
                return;
            }

            if (!localizationIds.Contains(id))
            {
                missingLocalizationIds.Add(id);
            }
        }

        private static HashSet<string> TryLoadLocalizationIds(out string source)
        {
            source = "unknown";

            try
            {
                var moduleRootConfigPath = ConfigurationManager.GetModuleDataPathForConsumers("enlisted_config.json");
                var moduleDataDir = Path.GetDirectoryName(moduleRootConfigPath);
                var moduleRootDir = string.IsNullOrWhiteSpace(moduleDataDir) ? null : Path.GetDirectoryName(moduleDataDir);

                var xmlPath = string.IsNullOrWhiteSpace(moduleRootDir)
                    ? null
                    : Path.Combine(moduleRootDir, "ModuleData", "Languages", "enlisted_strings.xml");

                if (string.IsNullOrWhiteSpace(xmlPath) || !File.Exists(xmlPath))
                {
                    source = "missing";
                    return null;
                }

                source = xmlPath;

                var doc = XDocument.Load(xmlPath);
                var ids = doc
                    .Descendants()
                    .Select(e => e.Attribute("id")?.Value)
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                return ids;
            }
            catch (Exception ex)
            {
                ModLogger.Warn(LogCategory, $"Failed to load enlisted_strings.xml for validation: {ex.GetType().Name}: {ex.Message}");
                source = "error";
                return null;
            }
        }

        private sealed class LanceLifeEventsPackJson
        {
            [JsonProperty("schemaVersion")] public string SchemaVersion { get; set; } = string.Empty;
            [JsonProperty("packId")] public string PackId { get; set; } = string.Empty;
            [JsonProperty("category")] public string Category { get; set; } = string.Empty;
            [JsonProperty("events")] public List<LanceLifeEventDefinition> Events { get; set; } = new List<LanceLifeEventDefinition>();
        }

        private sealed class LanceLifeEventsFileJsonLegacy
        {
            [JsonProperty("schemaVersion")] public int SchemaVersion { get; set; } = 1;
            [JsonProperty("events")] public List<LanceLifeEventDefinition> Events { get; set; } = new List<LanceLifeEventDefinition>();
        }
    }
}


