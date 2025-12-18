using System;
using System.Collections.Generic;
using System.Linq;
using Enlisted.Features.Schedule.Models;
using Enlisted.Mod.Core.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TaleWorlds.Engine;
using SystemPath = System.IO.Path;
using SystemFile = System.IO.File;

namespace Enlisted.Features.Schedule.Config
{
    /// <summary>
    /// Loads schedule configuration from JSON files.
    /// Handles parsing, validation, and error reporting.
    /// </summary>
    public static class ScheduleConfigLoader
    {
        private const string LogCategory = "ScheduleConfig";
        private const string ConfigFileName = "schedule_config.json";

        /// <summary>
        /// Load the schedule configuration from JSON.
        /// Returns null if loading fails.
        /// </summary>
        public static ScheduleConfig LoadConfig()
        {
            try
            {
                // Find the config file in ModuleData/Enlisted/
                string configPath = FindConfigFile();
                if (string.IsNullOrEmpty(configPath))
                {
                    ModLogger.ErrorCode(LogCategory, "E-SCHED-001",
                        $"Could not find {ConfigFileName} in ModuleData/Enlisted/");
                    return CreateDefaultConfig();
                }

                ModLogger.Info(LogCategory, $"Loading schedule config from: {configPath}");

                // Read and parse JSON
                string json = SystemFile.ReadAllText(configPath);
                var jObject = JObject.Parse(json);

                var config = new ScheduleConfig
                {
                    CycleLengthDays = jObject["cycle_length_days"]?.Value<int>() ?? 12,
                    EnableSchedule = jObject["enable_schedule"]?.Value<bool>() ?? true,
                    ShowNotifications = jObject["show_notifications"]?.Value<bool>() ?? true,
                    AllowSkipDuties = jObject["allow_skip_duties"]?.Value<bool>() ?? true,
                    SkipDutyHeatPenalty = jObject["skip_duty_heat_penalty"]?.Value<int>() ?? 10,
                    SkipDutyRepPenalty = jObject["skip_duty_rep_penalty"]?.Value<int>() ?? 5
                };

                // Load lance needs config
                var lanceNeedsObj = jObject["lance_needs"];
                if (lanceNeedsObj != null)
                {
                    config.LanceNeeds = ParseLanceNeedsConfig(lanceNeedsObj);
                }

                // Load activities
                var activitiesArray = jObject["activities"] as JArray;
                if (activitiesArray != null)
                {
                    config.Activities = ParseActivities(activitiesArray);
                    ModLogger.Info(LogCategory, $"Loaded {config.Activities.Count} schedule activities");
                }

                // Validate configuration
                ValidateConfig(config);

                ModLogger.Info(LogCategory, "Schedule configuration loaded successfully");
                return config;
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode(LogCategory, "E-SCHED-002", "Failed to load schedule config", ex);
                return CreateDefaultConfig();
            }
        }

        private static string FindConfigFile()
        {
            // Try multiple possible paths
            string[] possiblePaths = new[]
            {
                SystemPath.Combine(Utilities.GetBasePath(), "Modules", "Enlisted", "ModuleData", "Enlisted", ConfigFileName),
                SystemPath.Combine(Utilities.GetBasePath(), "Modules", "Enlisted", ConfigFileName),
                SystemPath.Combine("ModuleData", "Enlisted", ConfigFileName)
            };

            foreach (string path in possiblePaths)
            {
                if (SystemFile.Exists(path))
                {
                    return path;
                }
            }

            return null;
        }

        private static LanceNeedsConfig ParseLanceNeedsConfig(JToken lanceNeedsObj)
        {
            var config = new LanceNeedsConfig();

            var degradationRates = lanceNeedsObj["base_degradation_rates"];
            if (degradationRates != null)
            {
                var rates = degradationRates.ToObject<Dictionary<string, int>>();
                config.SetBaseDegradationRatesFromStrings(rates);
            }

            config.CombatDegradationMultiplier = lanceNeedsObj["combat_degradation_multiplier"]?.Value<float>() ?? 2.0f;
            config.SiegeDegradationMultiplier = lanceNeedsObj["siege_degradation_multiplier"]?.Value<float>() ?? 1.5f;
            config.TravelDegradationMultiplier = lanceNeedsObj["travel_degradation_multiplier"]?.Value<float>() ?? 0.8f;
            config.CriticalThresholdLow = lanceNeedsObj["critical_threshold_low"]?.Value<int>() ?? 30;
            config.CriticalThresholdHigh = lanceNeedsObj["critical_threshold_high"]?.Value<int>() ?? 20;
            config.EnableDegradation = lanceNeedsObj["enable_degradation"]?.Value<bool>() ?? true;
            config.DebugMode = lanceNeedsObj["debug_mode"]?.Value<bool>() ?? false;

            return config;
        }

        private static List<ScheduleActivityDefinition> ParseActivities(JArray activitiesArray)
        {
            var activities = new List<ScheduleActivityDefinition>();

            foreach (var activityObj in activitiesArray)
            {
                try
                {
                    var activity = new ScheduleActivityDefinition
                    {
                        Id = activityObj["id"]?.Value<string>() ?? string.Empty,
                        Title = activityObj["title"]?.Value<string>(),
                        Description = activityObj["description"]?.Value<string>(),
                        TitleKey = activityObj["title_key"]?.Value<string>() ?? string.Empty,
                        DescriptionKey = activityObj["description_key"]?.Value<string>() ?? string.Empty,
                        FatigueCost = activityObj["fatigue_cost"]?.Value<int>() ?? 0,
                        XPReward = activityObj["xp_reward"]?.Value<int>() ?? 0,
                        EventChance = activityObj["event_chance"]?.Value<float>() ?? 0.0f,
                        MinTier = activityObj["min_tier"]?.Value<int>() ?? 1,
                        MaxTier = activityObj["max_tier"]?.Value<int>() ?? 0
                    };

                    // Parse block type
                    string blockTypeStr = activityObj["block_type"]?.Value<string>();
                    if (!string.IsNullOrEmpty(blockTypeStr) && Enum.TryParse<ScheduleBlockType>(blockTypeStr, out var blockType))
                    {
                        activity.BlockType = blockType;
                    }

                    // Parse formations
                    var formationsArray = activityObj["required_formations"] as JArray;
                    if (formationsArray != null)
                    {
                        activity.RequiredFormations = formationsArray.Select(f => f.Value<string>()).ToList();
                    }
                    
                    // Parse required duties
                    var dutiesArray = activityObj["required_duties"] as JArray;
                    if (dutiesArray != null)
                    {
                        activity.RequiredDuties = dutiesArray.Select(d => d.Value<string>()).ToList();
                    }

                    // Parse preferred time blocks
                    var timeBlocksArray = activityObj["preferred_time_blocks"] as JArray;
                    if (timeBlocksArray != null)
                    {
                        foreach (var timeBlockStr in timeBlocksArray.Select(t => t.Value<string>()))
                        {
                            if (Enum.TryParse<TimeBlock>(timeBlockStr, out var timeBlock))
                            {
                                activity.PreferredTimeBlocks.Add(timeBlock);
                            }
                        }
                    }

                    // Parse favored objectives
                    var objectivesArray = activityObj["favored_by_objectives"] as JArray;
                    if (objectivesArray != null)
                    {
                        activity.FavoredByObjectives = objectivesArray.Select(o => o.Value<string>()).ToList();
                    }

                    // Parse need recovery
                    var needRecovery = activityObj["need_recovery"];
                    if (needRecovery != null)
                    {
                        activity.NeedRecovery = needRecovery.ToObject<Dictionary<string, int>>();
                    }

                    // Parse need degradation
                    var needDegradation = activityObj["need_degradation"];
                    if (needDegradation != null)
                    {
                        activity.NeedDegradation = needDegradation.ToObject<Dictionary<string, int>>();
                    }

                    activities.Add(activity);
                }
                catch (Exception ex)
                {
                    ModLogger.WarnCode(LogCategory, "W-SCHED-001", $"Failed to parse activity: {ex.Message}");
                }
            }

            return activities;
        }

        private static void ValidateConfig(ScheduleConfig config)
        {
            // Validate cycle length
            if (config.CycleLengthDays < 1 || config.CycleLengthDays > 30)
            {
                ModLogger.Warn(LogCategory, $"Invalid cycle length: {config.CycleLengthDays}, using default 12");
                config.CycleLengthDays = 12;
            }

            // Validate activities
            if (config.Activities.Count == 0)
            {
                ModLogger.Warn(LogCategory, "No activities defined in config");
            }

            // Check for required activity types
            var requiredTypes = new[] { ScheduleBlockType.Rest, ScheduleBlockType.FreeTime };
            foreach (var requiredType in requiredTypes)
            {
                if (!config.Activities.Any(a => a.BlockType == requiredType))
                {
                    ModLogger.Warn(LogCategory, $"Missing required activity type: {requiredType}");
                }
            }

            // Validate lance needs thresholds
            if (config.LanceNeeds.CriticalThresholdHigh >= config.LanceNeeds.CriticalThresholdLow)
            {
                ModLogger.Warn(LogCategory, "Critical threshold high must be less than critical threshold low");
            }
        }

        private static ScheduleConfig CreateDefaultConfig()
        {
            ModLogger.Info(LogCategory, "Creating default schedule configuration");
            
            var config = new ScheduleConfig();
            
            // Add minimal default activities
            config.Activities.Add(new ScheduleActivityDefinition
            {
                Id = "rest",
                TitleKey = "schedule_activity_rest_title",
                DescriptionKey = "schedule_activity_rest_desc",
                BlockType = ScheduleBlockType.Rest,
                FatigueCost = -12,
                XPReward = 0,
                EventChance = 0.05f,
                MinTier = 1
            });
            
            config.Activities.Add(new ScheduleActivityDefinition
            {
                Id = "free_time",
                TitleKey = "schedule_activity_free_title",
                DescriptionKey = "schedule_activity_free_desc",
                BlockType = ScheduleBlockType.FreeTime,
                FatigueCost = 0,
                XPReward = 0,
                EventChance = 0.1f,
                MinTier = 1
            });
            
            return config;
        }
    }
}

