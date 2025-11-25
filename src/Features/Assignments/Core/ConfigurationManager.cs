using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Enlisted.Mod.Core.Logging;
using Enlisted.Features.Enlistment.Core;

namespace Enlisted.Features.Assignments.Core
{
    /// <summary>
    /// Safe configuration loading with schema versioning and validation.
    /// Provides robust JSON loading following Blueprint standards for fail-safe operation.
    /// </summary>
    public static class ConfigurationManager
    {
        private static DutiesSystemConfig _cachedDutiesConfig;
        private static ProgressionConfig _cachedProgressionConfig;
        
        /// <summary>
        /// Load duties system configuration with comprehensive error handling and schema validation.
        /// Falls back to embedded defaults if file loading fails.
        /// </summary>
        public static DutiesSystemConfig LoadDutiesConfig()
        {
            if (_cachedDutiesConfig != null)
                return _cachedDutiesConfig;
                
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
                    ModLogger.Error("Config", $"Unsupported duties config schema version: {config?.SchemaVersion}. Expected: 1");
                    return CreateDefaultDutiesConfig();
                }
                
                // Validate configuration structure and required fields
                if (config.Duties == null || config.Duties.Count == 0)
                {
                    ModLogger.Error("Config", "Duties config contains no duty definitions");
                    return CreateDefaultDutiesConfig();
                }
                
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
        /// Create minimal working default configuration when JSON loading fails.
        /// Ensures the mod remains functional even with missing or corrupt config files.
        /// </summary>
        private static DutiesSystemConfig CreateDefaultDutiesConfig()
        {
            var defaultConfig = new DutiesSystemConfig
            {
                SchemaVersion = 1,
                Enabled = true,
                DutySlots = new System.Collections.Generic.Dictionary<string, int>
                {
                    ["tier_1"] = 1,
                    ["tier_2"] = 1,
                    ["tier_3"] = 2,
                    ["tier_4"] = 2,
                    ["tier_5"] = 3,
                    ["tier_6"] = 3
                },
                XpSources = new System.Collections.Generic.Dictionary<string, int>
                {
                    ["daily_service"] = 25,
                    ["duty_performance"] = 15,
                    ["battle_participation"] = 50
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
                RequiredFormations = new System.Collections.Generic.List<string> { "infantry", "archer" }
            };
            
            ModLogger.Info("Config", "Using fallback duties configuration");
            return defaultConfig;
        }
        
        /// <summary>
        /// Get the full path to a ModuleData configuration file.
        /// Works with any Bannerlord installation location.
        /// </summary>
        private static string GetModuleDataPath(string fileName)
        {
            try
            {
                // Get the assembly location (bin folder) and go up to module root
                var assemblyPath = Assembly.GetExecutingAssembly().Location;
                var binFolder = Path.GetDirectoryName(assemblyPath); // bin
                var binParent = Path.GetDirectoryName(binFolder); // Win64_Shipping_wEditor or similar
                var moduleFolder = Path.GetDirectoryName(binParent); // Module root
                var moduleDataPath = Path.Combine(moduleFolder, "ModuleData", "Enlisted", fileName);
                
                return moduleDataPath;
            }
            catch (Exception ex)
            {
                ModLogger.Error("Config", $"Error determining config path for {fileName}", ex);
                return fileName; // Fallback to relative path
            }
        }
        
        /// <summary>
        /// Load progression configuration with comprehensive error handling and schema validation.
        /// Falls back to embedded defaults if file loading fails.
        /// </summary>
        public static ProgressionConfig LoadProgressionConfig()
        {
            if (_cachedProgressionConfig != null)
                return _cachedProgressionConfig;
                
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
                
                // Schema validation
                if (config?.SchemaVersion != 1)
                {
                    ModLogger.Error("Config", $"Unsupported progression config schema version: {config?.SchemaVersion}. Expected: 1");
                    return CreateDefaultProgressionConfig();
                }
                
                // Basic validation
                if (config.TierProgression?.Requirements == null || config.TierProgression.Requirements.Count == 0)
                {
                    ModLogger.Error("Config", "Progression config contains no tier requirements");
                    return CreateDefaultProgressionConfig();
                }
                
                _cachedProgressionConfig = config;
                ModLogger.Info("Config", $"Progression config loaded from {configPath} ({config.TierProgression.Requirements.Count} tiers)");
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
        /// Create minimal working default configuration when JSON loading fails.
        /// Uses values from progression_config.json as defaults.
        /// </summary>
        private static ProgressionConfig CreateDefaultProgressionConfig()
        {
            var defaultConfig = new ProgressionConfig
            {
                SchemaVersion = 1,
                Enabled = true,
                TierProgression = new TierProgressionConfig
                {
                    Requirements = new System.Collections.Generic.List<TierRequirement>
                    {
                        new TierRequirement { Tier = 1, XpRequired = 0, Name = "Levy" },
                        new TierRequirement { Tier = 2, XpRequired = 500, Name = "Footman" },
                        new TierRequirement { Tier = 3, XpRequired = 2000, Name = "Serjeant" },
                        new TierRequirement { Tier = 4, XpRequired = 5000, Name = "Man-at-Arms" },
                        new TierRequirement { Tier = 5, XpRequired = 10000, Name = "Banner Sergeant" },
                        new TierRequirement { Tier = 6, XpRequired = 18000, Name = "Household Guard" }
                    }
                }
            };
            
            ModLogger.Info("Config", "Using fallback progression configuration");
            return defaultConfig;
        }
        
        /// <summary>
        /// Get tier XP requirements array from progression config.
        /// Returns array where index matches tier number (1-based).
        /// Array[0] is unused, Array[tier] contains the XP threshold needed to promote FROM that tier.
        /// Fix: JSON stores cumulative XP (tier 1=0, tier 2=500), but we need promotion thresholds.
        /// So Array[1] should be 500 (XP needed to go from tier 1→2), Array[2] should be 2000 (tier 2→3), etc.
        /// </summary>
        public static int[] GetTierXPRequirements()
        {
            var config = LoadProgressionConfig();
            if (config?.TierProgression?.Requirements == null || config.TierProgression.Requirements.Count == 0)
            {
                // Fix: Fallback array should map current tier to next tier's requirement
                // Array[1]=500 (need 500 XP to promote from tier 1 to tier 2)
                // Array[2]=2000 (need 2000 XP to promote from tier 2 to tier 3), etc.
                // Last element repeats max tier requirement to prevent out-of-bounds
                return new int[] { 0, 500, 2000, 5000, 10000, 18000, 18000 };
            }
            
            // Sort by tier and extract XP requirements
            var sorted = config.TierProgression.Requirements
                .OrderBy(r => r.Tier)
                .ToList();
            
            var maxTier = sorted.Max(r => r.Tier);
            // Create array with 1-based indexing: [0] unused, [1..maxTier] contain promotion thresholds
            // Fix: Use NEXT tier's requirement as the promotion threshold for current tier
            var requirements = new int[maxTier + 2]; // +2 to include maxTier+1 for safety
            
            for (int i = 0; i < sorted.Count; i++)
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
            for (int i = maxTier + 1; i < requirements.Length; i++)
            {
                requirements[i] = maxRequirement;
            }
            
            return requirements;
        }
        
        /// <summary>
        /// Get XP required for a specific tier from progression config.
        /// </summary>
        public static int GetXPRequiredForTier(int tier)
        {
            var requirements = GetTierXPRequirements();
            if (tier >= 0 && tier < requirements.Length)
            {
                return requirements[tier];
            }
            
            // Return last tier's requirement if tier is out of range
            return requirements.Length > 0 ? requirements[requirements.Length - 1] : 0;
        }
        
        /// <summary>
        /// Clear cached configuration. Used for testing or config reloading.
        /// </summary>
        public static void ClearCache()
        {
            _cachedDutiesConfig = null;
            _cachedProgressionConfig = null;
        }
    }
}
