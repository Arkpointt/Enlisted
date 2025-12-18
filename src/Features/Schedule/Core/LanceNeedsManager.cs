using System.Collections.Generic;
using Enlisted.Features.Schedule.Behaviors;
using Enlisted.Features.Schedule.Config;
using Enlisted.Features.Schedule.Models;
using Enlisted.Mod.Core.Logging;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;

namespace Enlisted.Features.Schedule.Core
{
    /// <summary>
    /// Manages lance need degradation and recovery mechanics.
    /// Needs degrade daily based on activity and conditions, recover when addressed.
    /// </summary>
    public static class LanceNeedsManager
    {
        private const string LogCategory = "LanceNeeds";

        /// <summary>
        /// Applies daily degradation to all lance needs.
        /// Base degradation rates from config, accelerated by conditions.
        /// </summary>
        public static void ProcessDailyDegradation(LanceNeedsState needs, MobileParty army = null)
        {
            if (needs == null)
            {
                ModLogger.Error(LogCategory, "Cannot process degradation: LanceNeedsState is null");
                return;
            }

            var config = ScheduleBehavior.Instance?.Config?.LanceNeeds;
            if (config == null)
            {
                ModLogger.Error(LogCategory, "Cannot process degradation: LanceNeedsConfig is null");
                return;
            }

            ModLogger.Debug(LogCategory, "Processing daily degradation...");

            // Apply base degradation rates
            int readinessDegradation = config.BaseDegradationRates.ContainsKey(LanceNeed.Readiness) ? config.BaseDegradationRates[LanceNeed.Readiness] : 2;
            int equipmentDegradation = config.BaseDegradationRates.ContainsKey(LanceNeed.Equipment) ? config.BaseDegradationRates[LanceNeed.Equipment] : 3;
            int moraleDegradation = config.BaseDegradationRates.ContainsKey(LanceNeed.Morale) ? config.BaseDegradationRates[LanceNeed.Morale] : 1;
            int restDegradation = config.BaseDegradationRates.ContainsKey(LanceNeed.Rest) ? config.BaseDegradationRates[LanceNeed.Rest] : 4;
            int suppliesDegradation = config.BaseDegradationRates.ContainsKey(LanceNeed.Supplies) ? config.BaseDegradationRates[LanceNeed.Supplies] : 5;

            // Check for accelerated degradation conditions
            // TODO: These conditions will be refined as army state tracking improves
            bool isInCombat = army != null && army.MapEvent != null;
            bool isOnLongMarch = army != null && army.IsMoving && army.CurrentSettlement == null;
            bool lowMorale = needs.Morale < 40;

            // Accelerated degradation from combat
            if (isInCombat)
            {
                equipmentDegradation += 10; // Combat wears gear
                suppliesDegradation += 15;  // Combat consumes supplies
                ModLogger.Debug(LogCategory, "Army in combat: accelerated Equipment and Supplies degradation");
            }

            // Accelerated degradation from long marches
            if (isOnLongMarch)
            {
                restDegradation += 5;       // Marching is tiring
                readinessDegradation += 5;  // Constant movement reduces readiness
                suppliesDegradation += 8;   // Marching consumes supplies
                ModLogger.Debug(LogCategory, "Army on long march: accelerated Rest, Readiness, and Supplies degradation");
            }

            // Low morale affects readiness
            if (lowMorale)
            {
                readinessDegradation += 3;
                ModLogger.Debug(LogCategory, "Low morale: accelerated Readiness degradation");
            }

            // Apply degradation
            int oldReadiness = needs.Readiness;
            int oldEquipment = needs.Equipment;
            int oldMorale = needs.Morale;
            int oldRest = needs.Rest;
            int oldSupplies = needs.Supplies;

            needs.SetNeed(LanceNeed.Readiness, needs.Readiness - readinessDegradation);
            needs.SetNeed(LanceNeed.Equipment, needs.Equipment - equipmentDegradation);
            needs.SetNeed(LanceNeed.Morale, needs.Morale - moraleDegradation);
            needs.SetNeed(LanceNeed.Rest, needs.Rest - restDegradation);
            needs.SetNeed(LanceNeed.Supplies, needs.Supplies - suppliesDegradation);

            // Log changes
            ModLogger.Debug(LogCategory, $"Readiness: {oldReadiness} -> {needs.Readiness} (-{readinessDegradation})");
            ModLogger.Debug(LogCategory, $"Equipment: {oldEquipment} -> {needs.Equipment} (-{equipmentDegradation})");
            ModLogger.Debug(LogCategory, $"Morale: {oldMorale} -> {needs.Morale} (-{moraleDegradation})");
            ModLogger.Debug(LogCategory, $"Rest: {oldRest} -> {needs.Rest} (-{restDegradation})");
            ModLogger.Debug(LogCategory, $"Supplies: {oldSupplies} -> {needs.Supplies} (-{suppliesDegradation})");

            ModLogger.Info(LogCategory, "Daily degradation applied.");
        }

        /// <summary>
        /// Applies need recovery from completed schedule block activities.
        /// Recovery amount depends on the activity type.
        /// </summary>
        /// <summary>
        /// Process need recovery and degradation for a completed schedule block.
        /// Applies both recovery (positive effects) and degradation (costs) from the activity.
        /// </summary>
        public static void ProcessActivityRecovery(LanceNeedsState needs, ScheduledBlock completedBlock)
        {
            if (needs == null)
            {
                ModLogger.Error(LogCategory, "Cannot process recovery: LanceNeedsState is null");
                return;
            }

            if (completedBlock == null)
            {
                ModLogger.Error(LogCategory, "Cannot process recovery: ScheduledBlock is null");
                return;
            }

            var activities = ScheduleBehavior.Instance?.Config?.Activities;
            if (activities == null)
            {
                ModLogger.Error(LogCategory, "Cannot process recovery: Activities config is null");
                return;
            }

            // Find the activity definition - first by ActivityId if available, then by BlockType
            ScheduleActivityDefinition activityDef = null;
            if (!string.IsNullOrEmpty(completedBlock.ActivityId))
            {
                activityDef = activities.Find(a => a.Id == completedBlock.ActivityId);
            }
            if (activityDef == null)
            {
                activityDef = activities.Find(a => a.BlockType == completedBlock.BlockType);
            }
            
            if (activityDef == null)
            {
                ModLogger.Debug(LogCategory, $"No activity definition found for {completedBlock.BlockType}, skipping need effects.");
                return;
            }

            ModLogger.Debug(LogCategory, $"Processing need effects for: {completedBlock.Title} ({completedBlock.BlockType})");

            bool hasChanges = false;

            // Apply need recoveries (positive effects)
            if (activityDef.NeedRecovery != null && activityDef.NeedRecovery.Count > 0)
            {
                foreach (var recovery in activityDef.NeedRecovery)
                {
                    if (System.Enum.TryParse<LanceNeed>(recovery.Key, out var needType))
                    {
                        int oldValue = GetNeedValue(needs, needType);
                        needs.SetNeed(needType, oldValue + recovery.Value);
                        ModLogger.Debug(LogCategory, $"Recovery: {needType}: {oldValue} -> {needs.GetNeedValue(needType)} (+{recovery.Value})");
                        hasChanges = true;
                    }
                    else
                    {
                        ModLogger.Warn(LogCategory, $"Unknown need type in recovery: {recovery.Key}");
                    }
                }
            }

            // Apply need degradation (costs/negative effects)
            if (activityDef.NeedDegradation != null && activityDef.NeedDegradation.Count > 0)
            {
                foreach (var degradation in activityDef.NeedDegradation)
                {
                    if (System.Enum.TryParse<LanceNeed>(degradation.Key, out var needType))
                    {
                        int oldValue = GetNeedValue(needs, needType);
                        needs.SetNeed(needType, oldValue - degradation.Value);
                        ModLogger.Debug(LogCategory, $"Degradation: {needType}: {oldValue} -> {needs.GetNeedValue(needType)} (-{degradation.Value})");
                        hasChanges = true;
                    }
                    else
                    {
                        ModLogger.Warn(LogCategory, $"Unknown need type in degradation: {degradation.Key}");
                    }
                }
            }

            if (hasChanges)
            {
                ModLogger.Info(LogCategory, $"Need effects applied for {completedBlock.BlockType}.");
            }
            else
            {
                ModLogger.Debug(LogCategory, $"No need effects defined for {completedBlock.BlockType}.");
            }
        }

        /// <summary>
        /// Checks if any lance needs are critically low and returns warning messages.
        /// </summary>
        /// <returns>Dictionary of need -> warning message for critical needs</returns>
        public static Dictionary<LanceNeed, string> CheckCriticalNeeds(LanceNeedsState needs)
        {
            var warnings = new Dictionary<LanceNeed, string>();

            if (needs == null)
            {
                ModLogger.Error(LogCategory, "Cannot check critical needs: LanceNeedsState is null");
                return warnings;
            }

            var config = ScheduleBehavior.Instance?.Config?.LanceNeeds;
            if (config == null)
            {
                ModLogger.Error(LogCategory, "Cannot check critical needs: LanceNeedsConfig is null");
                return warnings;
            }

            int criticalThresholdHigh = config.CriticalThresholdHigh; // 20%
            int criticalThresholdLow = config.CriticalThresholdLow;   // 30%

            // Check each need against thresholds
            CheckNeedThreshold(needs.Readiness, LanceNeed.Readiness, criticalThresholdHigh, criticalThresholdLow, warnings);
            CheckNeedThreshold(needs.Equipment, LanceNeed.Equipment, criticalThresholdHigh, criticalThresholdLow, warnings);
            CheckNeedThreshold(needs.Morale, LanceNeed.Morale, criticalThresholdHigh, criticalThresholdLow, warnings);
            CheckNeedThreshold(needs.Rest, LanceNeed.Rest, criticalThresholdHigh, criticalThresholdLow, warnings);
            CheckNeedThreshold(needs.Supplies, LanceNeed.Supplies, criticalThresholdHigh, criticalThresholdLow, warnings);

            if (warnings.Count > 0)
            {
                ModLogger.Info(LogCategory, $"Found {warnings.Count} critical needs");
            }

            return warnings;
        }

        /// <summary>
        /// Gets the status level text for a need value (Excellent, Good, Fair, Poor, Critical).
        /// </summary>
        public static string GetNeedStatusText(int needValue)
        {
            if (needValue >= 80) return "Excellent";
            if (needValue >= 60) return "Good";
            if (needValue >= 40) return "Fair";
            if (needValue >= 20) return "Poor";
            return "Critical";
        }

        // Helper: Check individual need against thresholds and add warning if needed
        private static void CheckNeedThreshold(int needValue, LanceNeed need, int criticalHigh, int criticalLow, 
            Dictionary<LanceNeed, string> warnings)
        {
            if (needValue <= criticalHigh)
            {
                warnings[need] = $"{need} is CRITICAL ({needValue}%)! Immediate action required.";
                ModLogger.Warn(LogCategory, $"{need} is critical: {needValue}%");
            }
            else if (needValue <= criticalLow)
            {
                warnings[need] = $"{need} is low ({needValue}%). Address soon.";
                ModLogger.Debug(LogCategory, $"{need} is low: {needValue}%");
            }
        }

        // Helper: Get need value by enum
        private static int GetNeedValue(LanceNeedsState needs, LanceNeed need)
        {
            return need switch
            {
                LanceNeed.Readiness => needs.Readiness,
                LanceNeed.Equipment => needs.Equipment,
                LanceNeed.Morale => needs.Morale,
                LanceNeed.Rest => needs.Rest,
                LanceNeed.Supplies => needs.Supplies,
                _ => 0
            };
        }
    }

    // Extension method for cleaner syntax
    public static class LanceNeedsStateExtensions
    {
        public static int GetNeedValue(this LanceNeedsState needs, LanceNeed need)
        {
            return need switch
            {
                LanceNeed.Readiness => needs.Readiness,
                LanceNeed.Equipment => needs.Equipment,
                LanceNeed.Morale => needs.Morale,
                LanceNeed.Rest => needs.Rest,
                LanceNeed.Supplies => needs.Supplies,
                _ => 0
            };
        }
    }
}

