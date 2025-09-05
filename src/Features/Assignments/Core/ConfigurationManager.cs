using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Features.Assignments.Core
{
    /// <summary>
    /// Safe configuration loading with schema versioning and validation.
    /// Provides robust JSON loading following Blueprint standards for fail-safe operation.
    /// </summary>
    public static class ConfigurationManager
    {
        private static DutiesSystemConfig _cachedDutiesConfig;
        
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
                
                // Basic validation
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
                    ["tier_6"] = 3,
                    ["tier_7"] = 3
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
                // Get the assembly location (bin folder)
                var assemblyPath = Assembly.GetExecutingAssembly().Location;
                var binFolder = Path.GetDirectoryName(assemblyPath);
                var moduleFolder = Path.GetDirectoryName(binFolder);
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
        /// Clear cached configuration. Used for testing or config reloading.
        /// </summary>
        public static void ClearCache()
        {
            _cachedDutiesConfig = null;
        }
    }
}
