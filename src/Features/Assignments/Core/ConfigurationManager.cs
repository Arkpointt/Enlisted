using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Enlisted.Features.Enlistment.Core;
using Enlisted.Mod.Core.Logging;
using Newtonsoft.Json;

namespace Enlisted.Features.Assignments.Core
{
    /// <summary>
    ///     Safe configuration loading with schema versioning and validation.
    ///     Provides robust JSON loading following Blueprint standards for fail-safe operation.
    /// </summary>
    public static class ConfigurationManager
    {
        private static DutiesSystemConfig _cachedDutiesConfig;
        private static ProgressionConfig _cachedProgressionConfig;
        private static FinanceConfig _cachedFinanceConfig;
        private static GameplayConfig _cachedGameplayConfig;
        private static RetirementConfig _cachedRetirementConfig;
        private static LancesFeatureConfig _cachedLancesConfig;
        private static LanceLifeConfig _cachedLanceLifeConfig;
        private static QuartermasterConfig _cachedQuartermasterConfig;
        private static CampLifeConfig _cachedCampLifeConfig;
        private static EscalationConfig _cachedEscalationConfig;
        private static LancePersonasConfig _cachedLancePersonasConfig;
        private static PlayerConditionsConfig _cachedPlayerConditionsConfig;
        private static CampActivitiesConfig _cachedCampActivitiesConfig;
        private static LanceCatalogConfig _cachedLanceCatalogConfig;
        private static LanceLifeEventsConfig _cachedLanceLifeEventsConfig;
        private static Lances.Events.Decisions.DecisionEventConfig _cachedDecisionEventsConfig;

        /// <summary>
        ///     Load duties system configuration with comprehensive error handling and schema validation.
        ///     Falls back to embedded defaults if file loading fails.
        /// </summary>
        public static DutiesSystemConfig LoadDutiesConfig()
        {
            if (_cachedDutiesConfig != null)
            {
                return _cachedDutiesConfig;
            }

            try
            {
                var configPath = GetModuleDataPath("duties_system.json");

                if (!File.Exists(configPath))
                {
                    ModLogger.Error("Config", $"Duties config not found at: {configPath}");
                    return CreateDefaultDutiesConfig();
                }

                var jsonContent = File.ReadAllText(configPath);
                var config = JsonConvert.DeserializeObject<DutiesSystemConfig>(jsonContent);

                // Schema validation
                if (config?.SchemaVersion != 1)
                {
                    ModLogger.Error("Config",
                        $"Unsupported duties config schema version: {config?.SchemaVersion}. Expected: 1");
                    return CreateDefaultDutiesConfig();
                }

                // Validate configuration structure and required fields
                if (config.Duties == null || config.Duties.Count == 0)
                {
                    ModLogger.Error("Config", "Duties config contains no duty definitions");
                    return CreateDefaultDutiesConfig();
                }

                // Phase 4.5: Expansion gating (War Sails / NavalDLC).
                ApplyExpansionGates(config);

                _cachedDutiesConfig = config;
                ModLogger.Info("Config", $"Duties config loaded successfully: {config.Duties.Count} duties");
                return config;
            }
            catch (JsonException ex)
            {
                ModLogger.Error("Config", "JSON parsing error in duties config", ex);
                return CreateDefaultDutiesConfig();
            }
            catch (Exception ex)
            {
                ModLogger.Error("Config", "Unexpected error loading duties config", ex);
                return CreateDefaultDutiesConfig();
            }
        }

        /// <summary>
        ///     Create a minimal working default configuration when JSON loading fails.
        ///     Ensures the mod remains functional even with missing or corrupt config files.
        /// </summary>
        private static DutiesSystemConfig CreateDefaultDutiesConfig()
        {
            var defaultConfig = new DutiesSystemConfig
            {
                SchemaVersion = 1,
                Enabled = true,
                DutySlots = new Dictionary<string, int>
                {
                    ["tier_1"] = 1,
                    ["tier_2"] = 1,
                    ["tier_3"] = 2,
                    ["tier_4"] = 2,
                    ["tier_5"] = 3,
                    ["tier_6"] = 3
                },
                XpSources = new Dictionary<string, int>
                {
                    ["daily_service"] = 25, ["duty_performance"] = 15, ["battle_participation"] = 50
                }
            };

            // Add basic runner duty as fallback
            defaultConfig.Duties["runner"] = new DutyDefinition
            {
                Id = "runner",
                DisplayName = "Runner",
                Description = "Carry messages and supplies around the camp",
                MinTier = 1,
                MaxConcurrent = 3,
                TargetSkill = "Athletics",
                SkillXpDaily = 15,
                WageMultiplier = 0.8f,
                RequiredFormations = ["infantry", "archer"]
            };

            ModLogger.Info("Config", "Using fallback duties configuration");
            return defaultConfig;
        }

        private static void ApplyExpansionGates(DutiesSystemConfig config)
        {
            try
            {
                if (config == null)
                {
                    return;
                }

                var hasWarSails = IsWarSailsLoaded();

                // Filter duties
                if (config.Duties != null && config.Duties.Count > 0)
                {
                    var toRemove = config.Duties
                        .Where(kvp => ShouldSkipForExpansion(kvp.Value, hasWarSails))
                        .Select(kvp => kvp.Key)
                        .ToList();

                    foreach (var key in toRemove)
                    {
                        config.Duties.Remove(key);
                    }
                }

                // NOTE: Professions were removed from the duties system in favor of unified duty definitions.
                // All duties (including former "professions") are now in the Duties dictionary.
            }
            catch (Exception ex)
            {
                ModLogger.Warn("Config", $"Expansion gating failed; proceeding without gating: {ex.Message}");
            }
        }

        private static bool ShouldSkipForExpansion(DutyDefinition def, bool hasWarSails)
        {
            if (def == null)
            {
                return false;
            }

            // Phase 4.5: Gate Naval duties on War Sails (NavalDLC).
            // This is intentionally tolerant: duties declare "naval" formation, and we drop them if the DLC isn't present.
            if (def.RequiredFormations != null &&
                def.RequiredFormations.Any(f => string.Equals(f, "naval", StringComparison.OrdinalIgnoreCase)))
            {
                return !hasWarSails;
            }

            return false;
        }

        private static bool IsWarSailsLoaded()
        {
            try
            {
                // Naval DLC assembly name is typically "NavalDLC". Avoid direct references.
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var a in assemblies)
                {
                    var name = a.GetName().Name ?? string.Empty;
                    if (string.Equals(name, "NavalDLC", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                // Fallback: type check (works if the assembly is loaded but name lookup is unreliable).
                var t = Type.GetType("NavalDLC.CampaignBehaviors.RaftStateCampaignBehavior, NavalDLC");
                return t != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        ///     Get the full path to a ModuleData configuration file.
        ///     Works with any Bannerlord installation location.
        ///     Always returns a non-null path; falls back to AppContext.BaseDirectory if needed.
        /// </summary>
        private static string GetModuleDataPath(string fileName)
        {
            try
            {
                // Get the assembly location (bin folder) and go up to the module root
                var assemblyPath = Assembly.GetExecutingAssembly().Location;
                var binFolder = Path.GetDirectoryName(assemblyPath); // bin
                var binParent = Path.GetDirectoryName(binFolder); // Win64_Shipping_wEditor or similar
                var moduleFolder = Path.GetDirectoryName(binParent); // Module root
                var baseFolder = moduleFolder ?? binParent ?? binFolder ?? AppContext.BaseDirectory;
                var moduleDataPath = Path.Combine(baseFolder, "ModuleData", "Enlisted", fileName);
                return moduleDataPath;
            }
            catch (Exception ex)
            {
                ModLogger.Error("Config", $"Error determining config path for {fileName}", ex);
                // Fallback to a safe non-null default under application base directory
                return Path.Combine(AppContext.BaseDirectory, "ModuleData", "Enlisted", fileName);
            }
        }

        /// <summary>
        ///     Expose module data path resolution for additive content loaders (e.g., lance stories).
        ///     This keeps file path logic consistent across Enlisted systems without duplicating it.
        /// </summary>
        public static string GetModuleDataPathForConsumers(string fileName)
        {
            return GetModuleDataPath(fileName);
        }

        /// <summary>
        ///     Load progression configuration with comprehensive error handling and schema validation.
        ///     Falls back to embedded defaults if file loading fails.
        /// </summary>
        private static ProgressionConfig LoadProgressionConfig()
        {
            if (_cachedProgressionConfig != null)
            {
                return _cachedProgressionConfig;
            }

            try
            {
                var configPath = GetModuleDataPath("progression_config.json");

                if (!File.Exists(configPath))
                {
                    ModLogger.Error("Config", $"Progression config not found at: {configPath}");
                    return CreateDefaultProgressionConfig();
                }

                var jsonContent = File.ReadAllText(configPath);
                var config = JsonConvert.DeserializeObject<ProgressionConfig>(jsonContent);

                // Schema validation (support versions 1 and 2 - v2 adds culture_ranks)
                if (config?.SchemaVersion != 1 && config?.SchemaVersion != 2)
                {
                    ModLogger.Error("Config",
                        $"Unsupported progression config schema version: {config?.SchemaVersion}. Expected: 1 or 2");
                    return CreateDefaultProgressionConfig();
                }

                // Basic validation
                if (config.TierProgression?.Requirements == null || config.TierProgression.Requirements.Count == 0)
                {
                    ModLogger.Error("Config", "Progression config contains no tier requirements");
                    return CreateDefaultProgressionConfig();
                }

                _cachedProgressionConfig = config;
                ModLogger.Info("Config",
                    $"Progression config loaded from {configPath} ({config.TierProgression.Requirements.Count} tiers)");
                return config;
            }
            catch (JsonException ex)
            {
                ModLogger.Error("Config", "JSON parsing error in progression config", ex);
                return CreateDefaultProgressionConfig();
            }
            catch (Exception ex)
            {
                ModLogger.Error("Config", "Unexpected error loading progression config", ex);
                return CreateDefaultProgressionConfig();
            }
        }

        /// <summary>
        ///     Create a minimal working default configuration when JSON loading fails.
        ///     Uses values from progression_config.json as defaults.
        /// </summary>
        private static ProgressionConfig CreateDefaultProgressionConfig()
        {
            var defaultConfig = new ProgressionConfig
            {
                SchemaVersion = 1,
                Enabled = true,
                TierProgression = new TierProgressionConfig
                {
                    Requirements =
                    [
                        new TierRequirement { Tier = 1, XpRequired = 0, Name = "Levy" },
                        new TierRequirement { Tier = 2, XpRequired = 800, Name = "Footman" },
                        new TierRequirement { Tier = 3, XpRequired = 3000, Name = "Serjeant" },
                        new TierRequirement { Tier = 4, XpRequired = 6000, Name = "Man-at-Arms" },
                        new TierRequirement { Tier = 5, XpRequired = 11000, Name = "Banner Serjeant" },
                        new TierRequirement { Tier = 6, XpRequired = 19000, Name = "Household Guard" }
                    ]
                }
            };

            ModLogger.Info("Config", "Using fallback progression configuration");
            return defaultConfig;
        }

        /// <summary>
        ///     Get tier XP requirements array from progression config.
        ///     Returns array where the index matches tier number (1-based).
        ///     Array[0] is unused, Array[tier] contains the XP threshold needed to promote FROM that tier.
        ///     Fix: JSON stores cumulative XP (tier 1=0, tier 2=800), but we need promotion thresholds.
        ///     So Array[1] should be 800 (XP needed to go from tier 1→2), Array[2] should be 3000 (tier 2→3), etc.
        /// </summary>
        public static int[] GetTierXpRequirements()
        {
            var config = LoadProgressionConfig();
            if (config?.TierProgression?.Requirements == null || config.TierProgression.Requirements.Count == 0)
            {
                // Fix: The fallback array should map current tier to next tier's requirement
                // Array[1]=800 (need 800 XP to promote from tier 1 to tier 2)
                // Array[2]=3000 (need 3000 XP to promote from tier 2 to tier 3), etc.
                // Last element repeats max tier requirement to prevent out-of-bounds
                return [0, 800, 3000, 6000, 11000, 19000, 19000];
            }

            // Sort by tier and extract XP requirements
            var sorted = config.TierProgression.Requirements
                .OrderBy(r => r.Tier)
                .ToList();

            var maxTier = sorted.Max(r => r.Tier);
            // Create the array with 1-based indexing: [0] unused, [1..maxTier] contain promotion thresholds
            // Fix: Use NEXT tier's requirement as the promotion threshold for current tier
            var requirements = new int[maxTier + 2]; // +2 to include maxTier+1 for safety

            for (var i = 0; i < sorted.Count; i++)
            {
                var currentReq = sorted[i];
                if (currentReq.Tier >= 1 && currentReq.Tier <= maxTier)
                {
                    // Get the next tier's requirement (promotion threshold)
                    if (i + 1 < sorted.Count)
                    {
                        // Use next tier's XP requirement as the threshold for current tier
                        requirements[currentReq.Tier] = sorted[i + 1].XpRequired;
                    }
                    else
                    {
                        // Last tier: use its own requirement (can't promote further)
                        requirements[currentReq.Tier] = currentReq.XpRequired;
                    }
                }
            }

            // Fill remaining elements with max tier requirement to prevent promotion beyond max
            var maxRequirement = sorted.Last().XpRequired;
            for (var i = maxTier + 1; i < requirements.Length; i++)
            {
                requirements[i] = maxRequirement;
            }

            return requirements;
        }

        /// <summary>
        ///     Get the maximum tier defined in progression config.
        ///     Returns the actual tier count (e.g., 6 for tiers 1-6), not the array length.
        /// </summary>
        public static int GetMaxTier()
        {
            var config = LoadProgressionConfig();
            if (config?.TierProgression?.Requirements == null || config.TierProgression.Requirements.Count == 0)
            {
                return 6; // Default fallback matching progression_config.json
            }

            return config.TierProgression.Requirements.Max(r => r.Tier);
        }

        /// <summary>
        ///     Get XP required for a specific tier from progression config.
        /// </summary>
        public static int GetXpRequiredForTier(int tier)
        {
            var requirements = GetTierXpRequirements();
            if (tier >= 0 && tier < requirements.Length)
            {
                return requirements[tier];
            }

            // Return last tier's requirement if tier is out of range
            return requirements.Length > 0 ? requirements[requirements.Length - 1] : 0;
        }

        /// <summary>
        ///     Get the display name for a specific tier from progression config.
        ///     Returns the generic/mercenary rank name.
        /// </summary>
        public static string GetTierName(int tier)
        {
            var config = LoadProgressionConfig();
            var requirement = config?.TierProgression?.Requirements?.Find(r => r.Tier == tier);
            return requirement?.Name ?? $"Tier {tier}";
        }

        /// <summary>
        ///     Get the culture-specific rank title for a tier.
        ///     Falls back to the generic tier name if culture not found.
        /// </summary>
        /// <param name="tier">The tier (1-9)</param>
        /// <param name="cultureId">The culture StringId (e.g., "empire", "vlandia", "sturgia")</param>
        public static string GetCultureRankTitle(int tier, string cultureId)
        {
            if (tier < 1 || tier > 9)
            {
                return GetTierName(tier);
            }

            var config = LoadProgressionConfig();
            if (config?.CultureRanks == null || string.IsNullOrWhiteSpace(cultureId))
            {
                return GetTierName(tier);
            }

            // Normalize culture ID to match config keys
            var normalizedCulture = NormalizeCultureId(cultureId);

            if (config.CultureRanks.TryGetValue(normalizedCulture, out var cultureConfig) &&
                cultureConfig?.Ranks != null &&
                cultureConfig.Ranks.Count >= tier)
            {
                // Ranks array is 0-indexed, tier is 1-indexed
                return cultureConfig.Ranks[tier - 1];
            }

            // Fallback to mercenary/generic if culture not found
            if (config.CultureRanks.TryGetValue("mercenary", out var mercConfig) &&
                mercConfig?.Ranks != null &&
                mercConfig.Ranks.Count >= tier)
            {
                return mercConfig.Ranks[tier - 1];
            }

            return GetTierName(tier);
        }

        /// <summary>
        ///     Normalizes a culture StringId to match progression config keys.
        ///     Handles empire variants, minor factions, and mods.
        /// </summary>
        private static string NormalizeCultureId(string cultureId)
        {
            if (string.IsNullOrWhiteSpace(cultureId))
            {
                return "mercenary";
            }

            var lower = cultureId.ToLowerInvariant();

            // Empire variants
            if (lower.Contains("empire") || lower == "calradia_empire" || lower == "rome_reborn")
            {
                return "empire";
            }

            // Direct matches
            if (lower == "vlandia" || lower == "sturgia" || lower == "khuzait" || 
                lower == "battania" || lower == "aserai")
            {
                return lower;
            }

            // Mod culture mappings (add as needed)
            if (lower.Contains("viking") || lower.Contains("nord"))
            {
                return "sturgia";
            }
            if (lower.Contains("mongol") || lower.Contains("steppe"))
            {
                return "khuzait";
            }
            if (lower.Contains("celtic") || lower.Contains("highland"))
            {
                return "battania";
            }
            if (lower.Contains("desert") || lower.Contains("arab"))
            {
                return "aserai";
            }
            if (lower.Contains("feudal") || lower.Contains("knight"))
            {
                return "vlandia";
            }

            // Default to mercenary for unknown cultures
            return "mercenary";
        }

        /// <summary>
        ///     Get XP awarded for battle participation from progression config.
        /// </summary>
        public static int GetBattleParticipationXp()
        {
            var config = LoadProgressionConfig();
            return config?.XpSources?.BattleParticipation ?? 25;
        }

        /// <summary>
        ///     Get XP awarded per enemy kill from progression config.
        /// </summary>
        public static int GetXpPerKill()
        {
            var config = LoadProgressionConfig();
            return config?.XpSources?.XpPerKill ?? 1;
        }

        /// <summary>
        ///     Get daily base XP from progression config.
        /// </summary>
        public static int GetDailyBaseXp()
        {
            var config = LoadProgressionConfig();
            return config?.XpSources?.DailyBase ?? 25;
        }

        /// <summary>
        ///     Clear cached configuration. Used for testing or config reloading.
        /// </summary>
        public static void ClearCache()
        {
            _cachedDutiesConfig = null;
            _cachedProgressionConfig = null;
            _cachedFinanceConfig = null;
            _cachedGameplayConfig = null;
            _cachedRetirementConfig = null;
            _cachedLancesConfig = null;
            _cachedLanceCatalogConfig = null;
        }

        /// <summary>
        ///     Load finance configuration from enlisted_config.json.
        ///     Returns cached config or loads from file on first call.
        /// </summary>
        public static FinanceConfig LoadFinanceConfig()
        {
            if (_cachedFinanceConfig != null)
            {
                return _cachedFinanceConfig;
            }

            try
            {
                var configPath = GetModuleDataPath("enlisted_config.json");

                if (!File.Exists(configPath))
                {
                    ModLogger.Error("Config", $"Enlisted config not found at: {configPath}");
                    return CreateDefaultFinanceConfig();
                }

                var jsonContent = File.ReadAllText(configPath);
                var fullConfig = JsonConvert.DeserializeObject<EnlistedFullConfig>(jsonContent);

                if (fullConfig?.Finance == null)
                {
                    ModLogger.Info("Config", "No finance section in enlisted_config.json, using defaults");
                    return CreateDefaultFinanceConfig();
                }

                _cachedFinanceConfig = fullConfig.Finance;
                ModLogger.Info("Config", "Finance config loaded successfully");
                return _cachedFinanceConfig;
            }
            catch (Exception ex)
            {
                ModLogger.Error("Config", "Error loading finance config", ex);
                return CreateDefaultFinanceConfig();
            }
        }

        /// <summary>
        ///     Create default finance configuration when JSON loading fails.
        /// </summary>
        private static FinanceConfig CreateDefaultFinanceConfig()
        {
            return new FinanceConfig
            {
                ShowInClanTooltip = true,
                TooltipLabel = "{=enlisted_wage_income}Enlistment Wages",
                WageFormula = new WageFormulaConfig
                {
                    BaseWage = 10,
                    LevelMultiplier = 1,
                    TierMultiplier = 5,
                    XpDivisor = 200,
                    ArmyBonusMultiplier = 1.2f
                }
            };
        }

        /// <summary>
        ///     Load gameplay configuration from enlisted_config.json.
        ///     Returns cached config or loads from file on first call.
        /// </summary>
        public static GameplayConfig LoadGameplayConfig()
        {
            if (_cachedGameplayConfig != null)
            {
                return _cachedGameplayConfig;
            }

            try
            {
                var configPath = GetModuleDataPath("enlisted_config.json");

                if (!File.Exists(configPath))
                {
                    ModLogger.Error("Config", $"Enlisted config not found at: {configPath}");
                    return CreateDefaultGameplayConfig();
                }

                var jsonContent = File.ReadAllText(configPath);
                var fullConfig = JsonConvert.DeserializeObject<EnlistedFullConfig>(jsonContent);

                if (fullConfig?.Gameplay == null)
                {
                    ModLogger.Info("Config", "No gameplay section in enlisted_config.json, using defaults");
                    return CreateDefaultGameplayConfig();
                }

                // Validate reserve threshold is within safe bounds
                var config = fullConfig.Gameplay;
                if (config.ReserveTroopThreshold < 20)
                {
                    config.ReserveTroopThreshold = 20;
                }
                else if (config.ReserveTroopThreshold > 500)
                {
                    config.ReserveTroopThreshold = 500;
                }

                // Validate desertion grace period is within safe bounds
                if (config.DesertionGracePeriodDays < 1)
                {
                    config.DesertionGracePeriodDays = 1;
                }
                else if (config.DesertionGracePeriodDays > 90)
                {
                    config.DesertionGracePeriodDays = 90;
                }

                // Validate leave max days is within safe bounds (1-14 days)
                // 14 days is the documented design limit for leave before desertion penalties
                if (config.LeaveMaxDays < 1)
                {
                    config.LeaveMaxDays = 1;
                }
                else if (config.LeaveMaxDays > 14)
                {
                    config.LeaveMaxDays = 14;
                }

                _cachedGameplayConfig = config;
                ModLogger.Info("Config", "Gameplay config loaded successfully");
                return _cachedGameplayConfig;
            }
            catch (Exception ex)
            {
                ModLogger.Error("Config", "Error loading gameplay config", ex);
                return CreateDefaultGameplayConfig();
            }
        }

        /// <summary>
        ///     Create default gameplay configuration when JSON loading fails.
        /// </summary>
        private static GameplayConfig CreateDefaultGameplayConfig()
        {
            return new GameplayConfig { ReserveTroopThreshold = 100, DesertionGracePeriodDays = 14, LeaveMaxDays = 14 };
        }

        /// <summary>
        ///     Load retirement configuration from enlisted_config.json.
        ///     Returns cached config or loads from file on first call.
        /// </summary>
        public static RetirementConfig LoadRetirementConfig()
        {
            if (_cachedRetirementConfig != null)
            {
                return _cachedRetirementConfig;
            }

            try
            {
                var configPath = GetModuleDataPath("enlisted_config.json");

                if (!File.Exists(configPath))
                {
                    ModLogger.Error("Config", $"Enlisted config not found at: {configPath}");
                    return CreateDefaultRetirementConfig();
                }

                var jsonContent = File.ReadAllText(configPath);
                var fullConfig = JsonConvert.DeserializeObject<EnlistedFullConfig>(jsonContent);

                if (fullConfig?.Retirement == null)
                {
                    ModLogger.Info("Config", "No retirement section in enlisted_config.json, using defaults");
                    return CreateDefaultRetirementConfig();
                }

                var config = fullConfig.Retirement;

                // Validate config values with min/max bounds (matches LoadGameplayConfig pattern)
                // FirstTermDays: 84-365 days (3-12 Banner-lord months)
                if (config.FirstTermDays < 84)
                {
                    config.FirstTermDays = 84;
                }
                else if (config.FirstTermDays > 365)
                {
                    config.FirstTermDays = 365;
                }

                // RenewalTermDays: 28-168 days (1-6 Bannerlord months)
                if (config.RenewalTermDays < 28)
                {
                    config.RenewalTermDays = 28;
                }
                else if (config.RenewalTermDays > 168)
                {
                    config.RenewalTermDays = 168;
                }

                // CooldownDays: 14-90 days (0.5-3 Bannerlord months)
                if (config.CooldownDays < 14)
                {
                    config.CooldownDays = 14;
                }
                else if (config.CooldownDays > 90)
                {
                    config.CooldownDays = 90;
                }

                _cachedRetirementConfig = config;
                ModLogger.Info("Config", "Retirement config loaded successfully");
                return _cachedRetirementConfig;
            }
            catch (Exception ex)
            {
                ModLogger.Error("Config", "Error loading retirement config", ex);
                return CreateDefaultRetirementConfig();
            }
        }

        /// <summary>
        ///     Create default retirement configuration when JSON loading fails.
        /// </summary>
        private static RetirementConfig CreateDefaultRetirementConfig()
        {
            return new RetirementConfig();
        }

        /// <summary>
        ///     Load lance feature toggles from enlisted_config.json.
        ///     Keeps the feature gated until explicitly enabled.
        /// </summary>
        public static LancesFeatureConfig LoadLancesConfig()
        {
            if (_cachedLancesConfig != null)
            {
                return _cachedLancesConfig;
            }

            try
            {
                var configPath = GetModuleDataPath("enlisted_config.json");

                if (!File.Exists(configPath))
                {
                    ModLogger.Error("Config", $"Enlisted config not found at: {configPath}");
                    return CreateDefaultLancesFeatureConfig();
                }

                var jsonContent = File.ReadAllText(configPath);
                var fullConfig = JsonConvert.DeserializeObject<EnlistedFullConfig>(jsonContent);

                if (fullConfig?.Lances == null)
                {
                    ModLogger.Info("Config", "No lances section in enlisted_config.json, using defaults");
                    return CreateDefaultLancesFeatureConfig();
                }

                _cachedLancesConfig = fullConfig.Lances;
                ModLogger.Info("Config",
                    $"Lances feature config loaded (enabled={_cachedLancesConfig.LancesEnabled})");
                return _cachedLancesConfig;
            }
            catch (Exception ex)
            {
                ModLogger.Error("Config", "Error loading lances feature config", ex);
                return CreateDefaultLancesFeatureConfig();
            }
        }

        public static QuartermasterConfig LoadQuartermasterConfig()
        {
            if (_cachedQuartermasterConfig != null)
            {
                return _cachedQuartermasterConfig;
            }

            try
            {
                var configPath = GetModuleDataPath("enlisted_config.json");
                if (!File.Exists(configPath))
                {
                    ModLogger.Warn("Config", "enlisted_config.json not found - using defaults for quartermaster");
                    _cachedQuartermasterConfig = new QuartermasterConfig();
                    return _cachedQuartermasterConfig;
                }

                var jsonContent = File.ReadAllText(configPath);
                var fullConfig = JsonConvert.DeserializeObject<EnlistedFullConfig>(jsonContent);

                _cachedQuartermasterConfig = fullConfig?.Quartermaster ?? new QuartermasterConfig();
                return _cachedQuartermasterConfig;
            }
            catch (Exception ex)
            {
                ModLogger.Error("Config", $"Failed to load quartermaster config, using defaults. Error: {ex.Message}");
                _cachedQuartermasterConfig = new QuartermasterConfig();
                return _cachedQuartermasterConfig;
            }
        }

        public static LanceLifeConfig LoadLanceLifeConfig()
        {
            if (_cachedLanceLifeConfig != null)
            {
                return _cachedLanceLifeConfig;
            }

            try
            {
                var configPath = GetModuleDataPath("enlisted_config.json");
                if (!File.Exists(configPath))
                {
                    ModLogger.Warn("Config", "enlisted_config.json not found - using defaults for lance life");
                    _cachedLanceLifeConfig = new LanceLifeConfig();
                    return _cachedLanceLifeConfig;
                }

                var jsonContent = File.ReadAllText(configPath);
                var fullConfig = JsonConvert.DeserializeObject<EnlistedFullConfig>(jsonContent);

                _cachedLanceLifeConfig = fullConfig?.LanceLife ?? new LanceLifeConfig();
                return _cachedLanceLifeConfig;
            }
            catch (Exception ex)
            {
                ModLogger.Error("Config", $"Failed to load lance life config, using defaults. Error: {ex.Message}");
                _cachedLanceLifeConfig = new LanceLifeConfig();
                return _cachedLanceLifeConfig;
            }
        }

        public static CampLifeConfig LoadCampLifeConfig()
        {
            if (_cachedCampLifeConfig != null)
            {
                return _cachedCampLifeConfig;
            }

            try
            {
                var configPath = GetModuleDataPath("enlisted_config.json");
                if (!File.Exists(configPath))
                {
                    ModLogger.Warn("Config", "enlisted_config.json not found - using defaults for camp life");
                    _cachedCampLifeConfig = new CampLifeConfig();
                    return _cachedCampLifeConfig;
                }

                var jsonContent = File.ReadAllText(configPath);
                var fullConfig = JsonConvert.DeserializeObject<EnlistedFullConfig>(jsonContent);

                _cachedCampLifeConfig = fullConfig?.CampLife ?? new CampLifeConfig();
                return _cachedCampLifeConfig;
            }
            catch (Exception ex)
            {
                ModLogger.Error("Config", $"Failed to load camp life config, using defaults. Error: {ex.Message}");
                _cachedCampLifeConfig = new CampLifeConfig();
                return _cachedCampLifeConfig;
            }
        }

        public static EscalationConfig LoadEscalationConfig()
        {
            if (_cachedEscalationConfig != null)
            {
                return _cachedEscalationConfig;
            }

            try
            {
                var configPath = GetModuleDataPath("enlisted_config.json");
                if (!File.Exists(configPath))
                {
                    ModLogger.Warn("Config", "enlisted_config.json not found - using defaults for escalation");
                    _cachedEscalationConfig = new EscalationConfig();
                    return _cachedEscalationConfig;
                }

                var jsonContent = File.ReadAllText(configPath);
                var fullConfig = JsonConvert.DeserializeObject<EnlistedFullConfig>(jsonContent);

                _cachedEscalationConfig = fullConfig?.Escalation ?? new EscalationConfig();
                return _cachedEscalationConfig;
            }
            catch (Exception ex)
            {
                ModLogger.Error("Config", $"Failed to load escalation config, using defaults. Error: {ex.Message}");
                _cachedEscalationConfig = new EscalationConfig();
                return _cachedEscalationConfig;
            }
        }

        public static LancePersonasConfig LoadLancePersonasConfig()
        {
            if (_cachedLancePersonasConfig != null)
            {
                return _cachedLancePersonasConfig;
            }

            try
            {
                var configPath = GetModuleDataPath("enlisted_config.json");
                if (!File.Exists(configPath))
                {
                    ModLogger.Warn("Config", "enlisted_config.json not found - using defaults for lance personas");
                    _cachedLancePersonasConfig = new LancePersonasConfig();
                    return _cachedLancePersonasConfig;
                }

                var jsonContent = File.ReadAllText(configPath);
                var fullConfig = JsonConvert.DeserializeObject<EnlistedFullConfig>(jsonContent);

                _cachedLancePersonasConfig = fullConfig?.LancePersonas ?? new LancePersonasConfig();
                return _cachedLancePersonasConfig;
            }
            catch (Exception ex)
            {
                ModLogger.Error("Config", $"Failed to load lance personas config, using defaults. Error: {ex.Message}");
                _cachedLancePersonasConfig = new LancePersonasConfig();
                return _cachedLancePersonasConfig;
            }
        }

        public static PlayerConditionsConfig LoadPlayerConditionsConfig()
        {
            if (_cachedPlayerConditionsConfig != null)
            {
                return _cachedPlayerConditionsConfig;
            }

            try
            {
                var configPath = GetModuleDataPath("enlisted_config.json");
                if (!File.Exists(configPath))
                {
                    ModLogger.Warn("Config", "enlisted_config.json not found - using defaults for player conditions");
                    _cachedPlayerConditionsConfig = new PlayerConditionsConfig();
                    return _cachedPlayerConditionsConfig;
                }

                var jsonContent = File.ReadAllText(configPath);
                var fullConfig = JsonConvert.DeserializeObject<EnlistedFullConfig>(jsonContent);

                _cachedPlayerConditionsConfig = fullConfig?.PlayerConditions ?? new PlayerConditionsConfig();
                return _cachedPlayerConditionsConfig;
            }
            catch (Exception ex)
            {
                ModLogger.Error("Config", $"Failed to load player conditions config, using defaults. Error: {ex.Message}");
                _cachedPlayerConditionsConfig = new PlayerConditionsConfig();
                return _cachedPlayerConditionsConfig;
            }
        }

        public static CampActivitiesConfig LoadCampActivitiesConfig()
        {
            if (_cachedCampActivitiesConfig != null)
            {
                return _cachedCampActivitiesConfig;
            }

            try
            {
                var configPath = GetModuleDataPath("enlisted_config.json");
                if (!File.Exists(configPath))
                {
                    ModLogger.Warn("Config", "enlisted_config.json not found - using defaults for camp activities");
                    _cachedCampActivitiesConfig = new CampActivitiesConfig();
                    return _cachedCampActivitiesConfig;
                }

                var jsonContent = File.ReadAllText(configPath);
                var fullConfig = JsonConvert.DeserializeObject<EnlistedFullConfig>(jsonContent);

                _cachedCampActivitiesConfig = fullConfig?.CampActivities ?? new CampActivitiesConfig();
                return _cachedCampActivitiesConfig;
            }
            catch (Exception ex)
            {
                ModLogger.Error("Config", $"Failed to load camp activities config, using defaults. Error: {ex.Message}");
                _cachedCampActivitiesConfig = new CampActivitiesConfig();
                return _cachedCampActivitiesConfig;
            }
        }

        /// <summary>
        /// Phase 0 (Lance Life Events): feature gate and data folder selection.
        /// Loaded from the lance_life_events section of enlisted_config.json.
        /// </summary>
        public static LanceLifeEventsConfig LoadLanceLifeEventsConfig()
        {
            if (_cachedLanceLifeEventsConfig != null)
            {
                return _cachedLanceLifeEventsConfig;
            }

            try
            {
                var configPath = GetModuleDataPath("enlisted_config.json");
                if (!File.Exists(configPath))
                {
                    ModLogger.Warn("Config", "enlisted_config.json not found - using defaults for lance life events");
                    _cachedLanceLifeEventsConfig = new LanceLifeEventsConfig();
                    return _cachedLanceLifeEventsConfig;
                }

                var jsonContent = File.ReadAllText(configPath);
                var fullConfig = JsonConvert.DeserializeObject<EnlistedFullConfig>(jsonContent);

                _cachedLanceLifeEventsConfig = fullConfig?.LanceLifeEvents ?? new LanceLifeEventsConfig();
                return _cachedLanceLifeEventsConfig;
            }
            catch (Exception ex)
            {
                ModLogger.Error("Config", $"Failed to load lance life events config, using defaults. Error: {ex.Message}");
                _cachedLanceLifeEventsConfig = new LanceLifeEventsConfig();
                return _cachedLanceLifeEventsConfig;
            }
        }

        private static LancesFeatureConfig CreateDefaultLancesFeatureConfig()
        {
            return new LancesFeatureConfig
            {
                LancesEnabled = false,
                LanceSelectionCount = 3,
                UseCultureWeighting = true
            };
        }

        /// <summary>
        /// Decision Events (Track D2): CK3-style decision system configuration.
        /// Loaded from the decision_events section of enlisted_config.json.
        /// </summary>
        public static Lances.Events.Decisions.DecisionEventConfig LoadDecisionEventsConfig()
        {
            if (_cachedDecisionEventsConfig != null)
            {
                return _cachedDecisionEventsConfig;
            }

            try
            {
                var configPath = GetModuleDataPath("enlisted_config.json");
                if (!File.Exists(configPath))
                {
                    ModLogger.Warn("Config", "enlisted_config.json not found - using defaults for decision events");
                    _cachedDecisionEventsConfig = new Lances.Events.Decisions.DecisionEventConfig();
                    return _cachedDecisionEventsConfig;
                }

                var jsonContent = File.ReadAllText(configPath);
                var fullConfig = JsonConvert.DeserializeObject<EnlistedFullConfig>(jsonContent);

                _cachedDecisionEventsConfig = fullConfig?.DecisionEvents ?? new Lances.Events.Decisions.DecisionEventConfig();
                return _cachedDecisionEventsConfig;
            }
            catch (Exception ex)
            {
                ModLogger.Error("Config", $"Failed to load decision events config, using defaults. Error: {ex.Message}");
                _cachedDecisionEventsConfig = new Lances.Events.Decisions.DecisionEventConfig();
                return _cachedDecisionEventsConfig;
            }
        }

        /// <summary>
        ///     Load lance catalog configuration from lances_config.json.
        ///     Provides style definitions, lance names, and culture mapping.
        /// </summary>
        public static LanceCatalogConfig LoadLanceCatalog()
        {
            if (_cachedLanceCatalogConfig != null)
            {
                return _cachedLanceCatalogConfig;
            }

            try
            {
                var configPath = GetModuleDataPath("lances_config.json");

                if (!File.Exists(configPath))
                {
                    ModLogger.Info("Config", $"Lances catalog not found at: {configPath}, using defaults");
                    return CreateDefaultLanceCatalog();
                }

                var jsonContent = File.ReadAllText(configPath);
                var config = JsonConvert.DeserializeObject<LanceCatalogConfig>(jsonContent);

                if (config?.SchemaVersion != 1)
                {
                    ModLogger.Error("Config",
                        $"Unsupported lances config schema version: {config?.SchemaVersion}. Expected: 1");
                    return CreateDefaultLanceCatalog();
                }

                if (config.StyleDefinitions == null || config.StyleDefinitions.Count == 0)
                {
                    ModLogger.Error("Config", "Lances config contains no style definitions");
                    return CreateDefaultLanceCatalog();
                }

                _cachedLanceCatalogConfig = config;
                ModLogger.Info("Config",
                    $"Lances catalog loaded successfully: {config.StyleDefinitions.Count} style definitions");
                return config;
            }
            catch (JsonException ex)
            {
                ModLogger.Error("Config", "JSON parsing error in lances config", ex);
                return CreateDefaultLanceCatalog();
            }
            catch (Exception ex)
            {
                ModLogger.Error("Config", "Unexpected error loading lances config", ex);
                return CreateDefaultLanceCatalog();
            }
        }

        /// <summary>
        ///     Default lance catalog used when JSON is missing or invalid.
        ///     Provides coverage for core cultures and style archetypes.
        /// </summary>
        private static LanceCatalogConfig CreateDefaultLanceCatalog()
        {
            var catalog = new LanceCatalogConfig
            {
                SchemaVersion = 1,
                CultureMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["empire"] = "style_legion",
                    ["calradia_empire"] = "style_legion",
                    ["rome_reborn"] = "style_legion",
                    ["vlandia"] = "style_feudal",
                    ["culture_westerlands"] = "style_feudal",
                    ["battania"] = "style_tribal",
                    ["sturgia"] = "style_tribal",
                    ["wildling"] = "style_tribal",
                    ["khuzait"] = "style_horde",
                    ["dothraki"] = "style_horde",
                    ["aserai"] = "style_mercenary"
                }
            };

            catalog.StyleDefinitions =
            [
                new LanceStyleDefinition
                {
                    Id = "style_legion",
                    Lances =
                    [
                        new LanceDefinition
                        {
                            Id = "legion_fourth_cohort", Name = "The 4th Cohort", RoleHint = "infantry",
                            StoryId = "story_legion_fourth_cohort"
                        },
                        new LanceDefinition
                        {
                            Id = "legion_iron_column", Name = "The Iron Column", RoleHint = "infantry",
                            StoryId = "story_legion_iron_column"
                        },
                        new LanceDefinition
                        {
                            Id = "legion_emperors_eyes", Name = "The Emperor's Eyes", RoleHint = "ranged",
                            StoryId = "story_legion_emperors_eyes"
                        },
                        new LanceDefinition
                        {
                            Id = "legion_porphyry_guard", Name = "The Porphyry Guard", RoleHint = "ranged",
                            StoryId = "story_legion_porphyry_guard"
                        },
                        new LanceDefinition
                        {
                            Id = "legion_eagles_talons", Name = "The Eagle's Talons", RoleHint = "cavalry",
                            StoryId = "story_legion_eagles_talons"
                        },
                        new LanceDefinition
                        {
                            Id = "legion_immortals", Name = "The Immortals", RoleHint = "cavalry",
                            StoryId = "story_legion_immortals"
                        }
                    ]
                },
                new LanceStyleDefinition
                {
                    Id = "style_feudal",
                    Lances =
                    [
                        new LanceDefinition
                        {
                            Id = "feudal_red_chevron", Name = "The Red Chevron", RoleHint = "infantry",
                            StoryId = "story_feudal_red_chevron"
                        },
                        new LanceDefinition
                        {
                            Id = "feudal_white_gauntlets", Name = "The White Gauntlets", RoleHint = "infantry",
                            StoryId = "story_feudal_white_gauntlets"
                        },
                        new LanceDefinition
                        {
                            Id = "feudal_broken_lances", Name = "The Broken Lances", RoleHint = "cavalry",
                            StoryId = "story_feudal_broken_lances"
                        },
                        new LanceDefinition
                        {
                            Id = "feudal_iron_spur", Name = "The Iron Spur", RoleHint = "cavalry",
                            StoryId = "story_feudal_iron_spur"
                        },
                        new LanceDefinition
                        {
                            Id = "feudal_black_pennant", Name = "The Black Pennant", RoleHint = "ranged",
                            StoryId = "story_feudal_black_pennant"
                        },
                        new LanceDefinition
                        {
                            Id = "feudal_gilded_lilies", Name = "The Gilded Lilies", RoleHint = "ranged",
                            StoryId = "story_feudal_gilded_lilies"
                        }
                    ]
                },
                new LanceStyleDefinition
                {
                    Id = "style_tribal",
                    Lances =
                    [
                        new LanceDefinition
                        {
                            Id = "tribal_wolf_skins", Name = "The Wolf-Skins", RoleHint = "infantry",
                            StoryId = "story_tribal_wolf_skins"
                        },
                        new LanceDefinition
                        {
                            Id = "tribal_bears_paw", Name = "The Bear's Paw", RoleHint = "infantry",
                            StoryId = "story_tribal_bears_paw"
                        },
                        new LanceDefinition
                        {
                            Id = "tribal_raven_feeders", Name = "The Raven Feeders", RoleHint = "ranged",
                            StoryId = "story_tribal_raven_feeders"
                        },
                        new LanceDefinition
                        {
                            Id = "tribal_stags_blood", Name = "The Stag's Blood", RoleHint = "ranged",
                            StoryId = "story_tribal_stags_blood"
                        },
                        new LanceDefinition
                        {
                            Id = "tribal_shield_biters", Name = "The Shield-Biters", RoleHint = "cavalry",
                            StoryId = "story_tribal_shield_biters"
                        },
                        new LanceDefinition
                        {
                            Id = "tribal_frost_eaters", Name = "The Frost-Eaters", RoleHint = "cavalry",
                            StoryId = "story_tribal_frost_eaters"
                        }
                    ]
                },
                new LanceStyleDefinition
                {
                    Id = "style_horde",
                    Lances =
                    [
                        new LanceDefinition
                        {
                            Id = "horde_wind_riders", Name = "The Wind-Riders", RoleHint = "horsearcher",
                            StoryId = "story_horde_wind_riders"
                        },
                        new LanceDefinition
                        {
                            Id = "horde_black_sky", Name = "The Black Sky", RoleHint = "horsearcher",
                            StoryId = "story_horde_black_sky"
                        },
                        new LanceDefinition
                        {
                            Id = "horde_lightning_hooves", Name = "The Lightning Hooves", RoleHint = "cavalry",
                            StoryId = "story_horde_lightning_hooves"
                        },
                        new LanceDefinition
                        {
                            Id = "horde_cloud_lancers", Name = "The Cloud Lancers", RoleHint = "cavalry",
                            StoryId = "story_horde_cloud_lancers"
                        },
                        new LanceDefinition
                        {
                            Id = "horde_steppe_sentinels", Name = "The Steppe Sentinels", RoleHint = "infantry",
                            StoryId = "story_horde_steppe_sentinels"
                        },
                        new LanceDefinition
                        {
                            Id = "horde_skywise", Name = "The Skywise", RoleHint = "infantry",
                            StoryId = "story_horde_skywise"
                        }
                    ]
                },
                new LanceStyleDefinition
                {
                    Id = "style_mercenary",
                    Lances =
                    [
                        new LanceDefinition
                        {
                            Id = "merc_mud_eaters", Name = "The Mud-Eaters", RoleHint = "infantry",
                            StoryId = "story_merc_mud_eaters"
                        },
                        new LanceDefinition
                        {
                            Id = "merc_vanguard", Name = "The Vanguard", RoleHint = "infantry",
                            StoryId = "story_merc_vanguard"
                        },
                        new LanceDefinition
                        {
                            Id = "merc_second_sons", Name = "The Second Sons", RoleHint = "ranged",
                            StoryId = "story_merc_second_sons"
                        },
                        new LanceDefinition
                        {
                            Id = "merc_free_arrows", Name = "The Free Arrows", RoleHint = "ranged",
                            StoryId = "story_merc_free_arrows"
                        },
                        new LanceDefinition
                        {
                            Id = "merc_coin_guard", Name = "The Coin Guard", RoleHint = "cavalry",
                            StoryId = "story_merc_coin_guard"
                        },
                        new LanceDefinition
                        {
                            Id = "merc_steel_company", Name = "The Steel Company", RoleHint = "cavalry",
                            StoryId = "story_merc_steel_company"
                        }
                    ]
                }
            ];

            _cachedLanceCatalogConfig = catalog;
            ModLogger.Info("Config", "Using fallback lances catalog");
            return catalog;
        }
    }

    /// <summary>
    ///     Root config object for enlisted_config.json parsing.
    /// </summary>
    public class EnlistedFullConfig
    {
        [JsonProperty("finance")] public FinanceConfig Finance { get; set; }

        [JsonProperty("gameplay")] public GameplayConfig Gameplay { get; set; }

        [JsonProperty("retirement")] public RetirementConfig Retirement { get; set; }

        [JsonProperty("quartermaster")] public QuartermasterConfig Quartermaster { get; set; }

        [JsonProperty("camp_life")] public CampLifeConfig CampLife { get; set; }

        [JsonProperty("escalation")] public EscalationConfig Escalation { get; set; }

        [JsonProperty("lance_personas")] public LancePersonasConfig LancePersonas { get; set; }

        [JsonProperty("player_conditions")] public PlayerConditionsConfig PlayerConditions { get; set; }

        [JsonProperty("camp_activities")] public CampActivitiesConfig CampActivities { get; set; }

        [JsonProperty("lances")] public LancesFeatureConfig Lances { get; set; }

        [JsonProperty("lance_life")] public LanceLifeConfig LanceLife { get; set; }

        [JsonProperty("lance_life_events")] public LanceLifeEventsConfig LanceLifeEvents { get; set; }

        [JsonProperty("decision_events")] public Lances.Events.Decisions.DecisionEventConfig DecisionEvents { get; set; }
    }

    /// <summary>
    /// Phase 0: Lance Life Events (duty/training/general/onboarding/threshold) delivery pipeline.
    /// This config exists to provide a safe feature flag + rollback toggle before the system is wired into gameplay.
    /// </summary>
    public class LanceLifeEventsConfig
    {
        [JsonProperty("enabled")] public bool Enabled { get; set; } = true;

        // Folder under ModuleData/Enlisted/ that contains event JSON files (Phase 5 content conversion).
        [JsonProperty("events_folder")] public string EventsFolder { get; set; } = "Events";

        // Phase 2: automatic scheduling (tick evaluation + queueing).
        [JsonProperty("automatic")] public LanceLifeEventsAutomaticConfig Automatic { get; set; } = new LanceLifeEventsAutomaticConfig();

        // Phase 3: player-initiated events surfaced in Camp Activities menu.
        [JsonProperty("player_initiated")] public LanceLifeEventsPlayerInitiatedConfig PlayerInitiated { get; set; } = new LanceLifeEventsPlayerInitiatedConfig();

        // Phase 4: onboarding state machine (stage/track/variant).
        [JsonProperty("onboarding")] public LanceLifeEventsOnboardingConfig Onboarding { get; set; } = new LanceLifeEventsOnboardingConfig();

        // Phase 5b: native incident channel delivery (MapState.NextIncident). Disabled by default for safe rollout.
        [JsonProperty("incident_channel")] public LanceLifeEventsIncidentChannelConfig IncidentChannel { get; set; } =
            new LanceLifeEventsIncidentChannelConfig();
    }

    public class LanceLifeEventsAutomaticConfig
    {
        [JsonProperty("enabled")] public bool Enabled { get; set; } = true;

        // Evaluation cadence (hours). Lower values feel more reactive but cost more CPU; keep modest.
        [JsonProperty("evaluation_cadence_hours")] public int EvaluationCadenceHours { get; set; } = 6;

        // Rate limits for automatic events.
        [JsonProperty("max_events_per_day")] public int MaxEventsPerDay { get; set; } = 1;
        
        // Minimum days between ANY automatic events (hard floor - prevents spam)
        [JsonProperty("min_days_between_events")] public int MinDaysBetweenEvents { get; set; } = 3;
        
        // Target days between events (soft target - events aim to fire around this interval)
        // Events will fire after MinDaysBetweenEvents but prefer to wait until TargetDaysBetweenEvents
        [JsonProperty("target_days_between_events")] public int TargetDaysBetweenEvents { get; set; } = 7;

        // Safety: if a queued event can't fire for a long time, drop it.
        [JsonProperty("queue_timeout_hours")] public int QueueTimeoutHours { get; set; } = 24;
    }

    public class LanceLifeEventsPlayerInitiatedConfig
    {
        [JsonProperty("enabled")] public bool Enabled { get; set; } = true;

        // If true, training-section events are blocked when PlayerConditionBehavior.CanTrain() is false.
        [JsonProperty("block_training_on_severe_condition")] public bool BlockTrainingOnSevereCondition { get; set; } = true;
    }

    public class LanceLifeEventsOnboardingConfig
    {
        [JsonProperty("enabled")] public bool Enabled { get; set; } = true;

        // If true, onboarding is skipped for players who have served before (TotalEnlistments > 1).
        // (First-time enlistment still gets onboarding.)
        [JsonProperty("skip_for_veterans")] public bool SkipForVeterans { get; set; } = true;

        // How many onboarding stages exist (default 3, then complete).
        [JsonProperty("stage_count")] public int StageCount { get; set; } = 3;
    }

    public class LanceLifeEventsIncidentChannelConfig
    {
        [JsonProperty("enabled")] public bool Enabled { get; set; } = false;
    }

    /// <summary>
    ///     Phase 5: named lance role personas (text-only roster).
    ///     Loaded from the lance_personas section of enlisted_config.json.
    /// </summary>
    public class LancePersonasConfig
    {
        [JsonProperty("enabled")] public bool Enabled { get; set; }

        [JsonProperty("seed_salt")] public string SeedSalt { get; set; } = "enlisted";

        [JsonProperty("female_leader_chance")] public float FemaleLeaderChance { get; set; } = 0.2f;
        [JsonProperty("female_second_chance")] public float FemaleSecondChance { get; set; } = 0.3f;
        [JsonProperty("female_veteran_chance")] public float FemaleVeteranChance { get; set; } = 0.25f;
        [JsonProperty("female_soldier_chance")] public float FemaleSoldierChance { get; set; } = 0.2f;
        [JsonProperty("female_recruit_chance")] public float FemaleRecruitChance { get; set; } = 0.2f;
    }

    /// <summary>
    ///     Phase 5: player condition system (injury/illness/exhaustion).
    ///     Loaded from the player_conditions section of enlisted_config.json.
    /// </summary>
    public class PlayerConditionsConfig
    {
        [JsonProperty("enabled")] public bool Enabled { get; set; }

        // Data file path under ModuleData/Enlisted (relative).
        [JsonProperty("definitions_file")] public string DefinitionsFile { get; set; } = "Conditions\\condition_defs.json";

        // Treatment tuning (multipliers applied to daily recovery).
        [JsonProperty("basic_treatment_multiplier")] public float BasicTreatmentMultiplier { get; set; } = 1.5f;
        [JsonProperty("thorough_treatment_multiplier")] public float ThoroughTreatmentMultiplier { get; set; } = 2.0f;
        [JsonProperty("herbal_treatment_multiplier")] public float HerbalTreatmentMultiplier { get; set; } = 1.75f;

        // Rest-only decay for exhaustion is a later step; keep placeholder values now.
        [JsonProperty("exhaustion_enabled")] public bool ExhaustionEnabled { get; set; }
    }

    /// <summary>
    /// Phase 2 (menu_system_update): Camp Activities menu (data-driven actions).
    /// Loaded from the camp_activities section of enlisted_config.json.
    /// </summary>
    public class CampActivitiesConfig
    {
        [JsonProperty("enabled")] public bool Enabled { get; set; } = true;

        // Data file path under ModuleData/Enlisted (relative).
        [JsonProperty("definitions_file")] public string DefinitionsFile { get; set; } = "Activities\\activities.json";
    }

    /// <summary>
    ///     Feature gating and tuning for Phase 4 Escalation System.
    ///     Loaded from the escalation section of enlisted_config.json.
    /// </summary>
    public class EscalationConfig
    {
        [JsonProperty("enabled")] public bool Enabled { get; set; }

        // Passive decay intervals (in days), per docs/research/escalation_system.md.
        [JsonProperty("heat_decay_interval_days")] public int HeatDecayIntervalDays { get; set; } = 7;
        [JsonProperty("discipline_decay_interval_days")] public int DisciplineDecayIntervalDays { get; set; } = 14;
        [JsonProperty("lance_rep_decay_interval_days")] public int LanceReputationDecayIntervalDays { get; set; } = 14;
        [JsonProperty("medical_risk_decay_interval_days")] public int MedicalRiskDecayIntervalDays { get; set; } = 1;

        // Threshold event cooldown (used in Phase 4 threshold-event manager).
        [JsonProperty("threshold_event_cooldown_days")] public int ThresholdEventCooldownDays { get; set; } = 7;
    }

    /// <summary>
    ///     Finance configuration for wage calculation and display.
    ///     Loaded from enlisted_config.json finance section.
    /// </summary>
    public class FinanceConfig
    {
        [JsonProperty("show_in_clan_tooltip")] public bool ShowInClanTooltip { get; set; } = true;

        [JsonProperty("tooltip_label")]
        public string TooltipLabel { get; set; } = "{=enlisted_wage_income}Enlistment Wages";

        [JsonProperty("wage_formula")] public WageFormulaConfig WageFormula { get; set; } = new();

        // Payday cadence (muster interval) in days. Default: 12.
        [JsonProperty("payday_interval_days")]
        public int PaydayIntervalDays { get; set; } = 12;

        // Random jitter applied to payday interval (+/-). Default: 1 day.
        [JsonProperty("payday_jitter_days")]
        public float PaydayJitterDays { get; set; } = 1f;
    }

    /// <summary>
    ///     Wage calculation formula configuration.
    ///     Formula: (base + level*levelMult + tier*tierMult + xp/xpDiv) * armyBonus
    /// </summary>
    public class WageFormulaConfig
    {
        [JsonProperty("base_wage")] public int BaseWage { get; set; } = 10;

        [JsonProperty("level_multiplier")] public int LevelMultiplier { get; set; } = 1;

        [JsonProperty("tier_multiplier")] public int TierMultiplier { get; set; } = 5;

        [JsonProperty("xp_divisor")] public int XpDivisor { get; set; } = 200;

        [JsonProperty("army_bonus_multiplier")]
        public float ArmyBonusMultiplier { get; set; } = 1.2f;
    }

    /// <summary>
    ///     Gameplay tuning configuration for combat and service mechanics.
    /// </summary>
    public class GameplayConfig
    {
        [JsonProperty("reserve_troop_threshold")]
        public int ReserveTroopThreshold { get; set; } = 100;

        [JsonProperty("desertion_grace_period_days")]
        public int DesertionGracePeriodDays { get; set; } = 14;

        [JsonProperty("leave_max_days")] public int LeaveMaxDays { get; set; } = 14;
    }

    /// <summary>
    ///     Quartermaster tuning for pricing and buybacks.
    /// </summary>
    public class QuartermasterConfig
    {
        [JsonProperty("soldier_tax")] public float SoldierTax { get; set; } = 1.2f;

        [JsonProperty("buyback_rate")] public float BuybackRate { get; set; } = 0.5f;

        [JsonProperty("officer_stock_tax")] public float OfficerStockTax { get; set; } = 1.35f;
    }

    /// <summary>
    ///     Retirement system configuration for veteran benefits and term tracking.
    /// </summary>
    public class RetirementConfig
    {
        [JsonProperty("first_term_days")] public int FirstTermDays { get; set; } = 252;

        [JsonProperty("renewal_term_days")] public int RenewalTermDays { get; set; } = 84;

        [JsonProperty("cooldown_days")] public int CooldownDays { get; set; } = 42;

        [JsonProperty("first_term_gold")] public int FirstTermGold { get; set; } = 10000;

        [JsonProperty("first_term_reenlist_bonus")]
        public int FirstTermReenlistBonus { get; set; } = 20000;

        [JsonProperty("renewal_discharge_gold")]
        public int RenewalDischargeGold { get; set; } = 5000;

        [JsonProperty("renewal_continue_bonus")]
        public int RenewalContinueBonus { get; set; } = 5000;

        [JsonProperty("lord_relation_bonus")] public int LordRelationBonus { get; set; } = 30;

        [JsonProperty("faction_reputation_bonus")]
        public int FactionReputationBonus { get; set; } = 30;

        [JsonProperty("other_lords_relation_bonus")]
        public int OtherLordsRelationBonus { get; set; } = 15;

        [JsonProperty("other_lords_min_relation")]
        public int OtherLordsMinRelation { get; set; } = 50;

            [JsonProperty("pension_honorable_daily")] public int PensionHonorableDaily { get; set; } = 50;
            [JsonProperty("pension_veteran_daily")] public int PensionVeteranDaily { get; set; } = 100;
            [JsonProperty("pension_relation_stop_threshold")] public int PensionRelationStopThreshold { get; set; }

            [JsonProperty("severance_honorable")] public int SeveranceHonorable { get; set; } = 3000;
            [JsonProperty("severance_veteran")] public int SeveranceVeteran { get; set; } = 10000;
            [JsonProperty("debug_skip_gear_stripping")] public bool DebugSkipGearStripping { get; set; }

            // Probation (washout/deserter re-entry)
            [JsonProperty("probation_days")] public int ProbationDays { get; set; } = 12;
            [JsonProperty("probation_wage_multiplier")] public float ProbationWageMultiplier { get; set; } = 0.5f;
            [JsonProperty("probation_fatigue_cap")] public int ProbationFatigueCap { get; set; } = 18;

            // Commander re-entry fine (gold) to clear bad blood; influence not used for mercenaries.
            [JsonProperty("commander_reentry_fine_gold")] public int CommanderReentryFineGold { get; set; } = 3000;
    }

    /// <summary>
    ///     Feature gating and tuning for the lance system.
    ///     Loaded from the lances section of enlisted_config.json.
    /// </summary>
    public class LancesFeatureConfig
    {
        [JsonProperty("lances_enabled")] public bool LancesEnabled { get; set; }

        [JsonProperty("lance_selection_count")]
        public int LanceSelectionCount { get; set; } = 3;

        [JsonProperty("use_culture_weighting")]
        public bool UseCultureWeighting { get; set; } = true;
    }

    /// <summary>
    ///     Feature gating and tuning for lance life (text-based stories and camp activities).
    ///     Loaded from the lance_life section of enlisted_config.json.
    /// </summary>
    public class LanceLifeConfig
    {
        [JsonProperty("enabled")] public bool Enabled { get; set; }

        [JsonProperty("require_final_lance")] public bool RequireFinalLance { get; set; } = true;

        [JsonProperty("min_tier")] public int MinTier { get; set; } = 2;

        [JsonProperty("max_stories_per_week")] public int MaxStoriesPerWeek { get; set; } = 2;

        [JsonProperty("min_days_between_stories")] public int MinDaysBetweenStories { get; set; } = 2;

        // Content enable/disable controls (Phase 1 requirement).
        // These allow safe rollout and troubleshooting without editing pack JSON.
        [JsonProperty("disabled_story_ids")] public List<string> DisabledStoryIds { get; set; } = new List<string>();
        [JsonProperty("disabled_categories")] public List<string> DisabledCategories { get; set; } = new List<string>();
        [JsonProperty("disabled_pack_ids")] public List<string> DisabledPackIds { get; set; } = new List<string>();
    }

    /// <summary>
    ///     Feature gating and tuning for Camp Life Simulation (Phase 3).
    ///     Loaded from the camp_life section of enlisted_config.json.
    /// </summary>
    public class CampLifeConfig
    {
        [JsonProperty("enabled")] public bool Enabled { get; set; }

        // Trigger thresholds used by story packs and integrations.
        [JsonProperty("logistics_high_threshold")] public float LogisticsHighThreshold { get; set; } = 70f;
        [JsonProperty("morale_low_threshold")] public float MoraleLowThreshold { get; set; } = 70f;
        [JsonProperty("pay_tension_high_threshold")] public float PayTensionHighThreshold { get; set; } = 70f;
        [JsonProperty("heat_high_threshold")] public float HeatHighThreshold { get; set; } = 70f;

        // Quartermaster mood multipliers (small, contained).
        [JsonProperty("qm_purchase_fine")] public float QuartermasterPurchaseFine { get; set; } = 0.98f;
        [JsonProperty("qm_purchase_tense")] public float QuartermasterPurchaseTense { get; set; } = 1.00f;
        [JsonProperty("qm_purchase_sour")] public float QuartermasterPurchaseSour { get; set; } = 1.07f;
        [JsonProperty("qm_purchase_predatory")] public float QuartermasterPurchasePredatory { get; set; } = 1.15f;

        [JsonProperty("qm_buyback_fine")] public float QuartermasterBuybackFine { get; set; } = 1.00f;
        [JsonProperty("qm_buyback_tense")] public float QuartermasterBuybackTense { get; set; } = 0.95f;
        [JsonProperty("qm_buyback_sour")] public float QuartermasterBuybackSour { get; set; } = 0.85f;
        [JsonProperty("qm_buyback_predatory")] public float QuartermasterBuybackPredatory { get; set; } = 0.75f;
    }

    /// <summary>
    ///     Root catalog for lances_config.json defining styles, lances, and culture mapping.
    /// </summary>
    public class LanceCatalogConfig
    {
        [JsonProperty("schemaVersion")] public int SchemaVersion { get; set; } = 1;

        [JsonProperty("style_definitions")]
        public List<LanceStyleDefinition> StyleDefinitions { get; set; } = new();

        [JsonProperty("culture_map")] public Dictionary<string, string> CultureMap { get; set; } = new();
    }

    /// <summary>
    ///     Style archetype containing a collection of lance definitions.
    /// </summary>
    public class LanceStyleDefinition
    {
        [JsonProperty("id")] public string Id { get; set; }

        [JsonProperty("lances")] public List<LanceDefinition> Lances { get; set; } = new();
    }

    /// <summary>
    ///     Individual lance entry with display name, role hint, and story mapping.
    /// </summary>
    public class LanceDefinition
    {
        [JsonProperty("id")] public string Id { get; set; }

        [JsonProperty("name")] public string Name { get; set; }

        [JsonProperty("roleHint")] public string RoleHint { get; set; }

        [JsonProperty("storyId")] public string StoryId { get; set; }
    }
}
