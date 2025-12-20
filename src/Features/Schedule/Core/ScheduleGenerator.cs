using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Schedule.Behaviors;
using Enlisted.Features.Schedule.Config;
using Enlisted.Features.Schedule.Models;
using Enlisted.Mod.Core.Logging;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace Enlisted.Features.Schedule.Core
{
    /// <summary>
    /// Generates daily schedules based on lord objectives, player tier/formation, and lance needs.
    /// The AI balances the lord's orders with critical lance needs.
    /// </summary>
    public class ScheduleGenerator
    {
        private const string LogCategory = "ScheduleGenerator";
        private readonly ScheduleConfig _config;
        private readonly List<string> _decisionLog;
        
        // Track activities already scheduled today to prevent duplicates
        private readonly HashSet<string> _usedActivitiesToday;
        
        // Activities that should only be scheduled once per day (e.g., formations, inspections)
        private static readonly HashSet<string> OncePerDayActivities = new HashSet<string>
        {
            "morning_formation",
            "evening_formation",
            "inspection",
            "weapons_inspection",
            "kit_inspection",
            "morning_drill",
            "evening_drill"
        };

        public ScheduleGenerator(ScheduleConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _decisionLog = new List<string>();
            _usedActivitiesToday = new HashSet<string>();
        }

        /// <summary>
        /// Generate a daily schedule for the player based on lord's orders and current context.
        /// The AI balances the lord's orders with lance needs intelligently.
        /// </summary>
        public DailySchedule GenerateSchedule(MobileParty army, int cycleDay)
        {
            _decisionLog.Clear();
            _usedActivitiesToday.Clear(); // Reset used activities for new day
            ModLogger.Debug(LogCategory, $"Generating schedule for Day {cycleDay}/12");

            // Create schedule
            var schedule = new DailySchedule(CampaignTime.Now, cycleDay);

            // Determine lord's objective
            var objective = ArmyStateAnalyzer.GetLordObjective(army);
            var priority = ArmyStateAnalyzer.GetObjectivePriority(objective, army);
            schedule.LordObjective = objective.ToString();
            schedule.LordOrders = ArmyStateAnalyzer.GetObjectiveDescription(objective);

            ModLogger.Debug(LogCategory, $"Lord objective: {objective}, Priority: {priority}");
            _decisionLog.Add($"Lord's Orders: {objective} (Priority: {priority})");

            // Get player context
            var playerTier = GetPlayerTier();
            var playerFormation = GetPlayerFormation();
            var playerDuty = GetPlayerDuty();
            ModLogger.Debug(LogCategory, $"Player: Tier {playerTier}, Formation: {playerFormation}, Duty: {playerDuty}");

            // Assess lance needs.
            var lanceNeeds = ScheduleBehavior.Instance?.LanceNeeds;
            if (lanceNeeds == null)
            {
                ModLogger.Warn(LogCategory, "Lance needs unavailable, generating basic schedule");
                lanceNeeds = new LanceNeedsState(); // Fallback to defaults
            }

            LogNeedsAssessment(lanceNeeds);

            // Determine critical needs.
            var criticalNeeds = GetCriticalNeeds(lanceNeeds);
            var poorNeeds = GetPoorNeeds(lanceNeeds);

            // Pick an assignment strategy based on needs and current priority.
            var assignmentStrategy = DetermineAssignmentStrategy(criticalNeeds, poorNeeds, priority);
            _decisionLog.Add($"Assignment Strategy: {assignmentStrategy}");

            // Analyze the situation to determine schedule flexibility
            bool isAtPeace = IsArmyAtPeace(army);
            bool isHardship = IsHardshipSituation(army, objective, priority, lanceNeeds);
            bool playerExhausted = lanceNeeds.Rest < 25; // Player really needs rest
            bool playerTired = lanceNeeds.Rest < 50;     // Player could use rest
            
            _decisionLog.Add($"Situation: {(isAtPeace ? "Peace" : "Active")} | {(isHardship ? "HARDSHIP" : "Normal")} | Rest: {lanceNeeds.Rest}%");
            
            // Generate a context-aware schedule. Rest may be scheduled in any block when the player is exhausted.
            // Free time may be skipped during hardship. When the situation allows it, the schedule should still
            // try to take care of the player.
            
            schedule.AddBlock(GenerateContextAwareBlock(TimeBlock.Morning, objective, priority, 
                lanceNeeds, criticalNeeds, poorNeeds, assignmentStrategy, 
                playerTier, playerFormation, playerDuty, isAtPeace, isHardship, playerExhausted, playerTired));
            
            schedule.AddBlock(GenerateContextAwareBlock(TimeBlock.Afternoon, objective, priority, 
                lanceNeeds, criticalNeeds, poorNeeds, assignmentStrategy, 
                playerTier, playerFormation, playerDuty, isAtPeace, isHardship, playerExhausted, playerTired));
            
            schedule.AddBlock(GenerateContextAwareBlock(TimeBlock.Dusk, objective, priority, 
                lanceNeeds, criticalNeeds, poorNeeds, assignmentStrategy, 
                playerTier, playerFormation, playerDuty, isAtPeace, isHardship, playerExhausted, playerTired));
            
            schedule.AddBlock(GenerateContextAwareBlock(TimeBlock.Night, objective, priority, 
                lanceNeeds, criticalNeeds, poorNeeds, assignmentStrategy, 
                playerTier, playerFormation, playerDuty, isAtPeace, isHardship, playerExhausted, playerTired));

            // Add decision log to schedule
            schedule.DecisionLog = string.Join("\n", _decisionLog);

            ModLogger.Info(LogCategory, $"Schedule generated: {schedule.Blocks.Count} blocks assigned");
            ModLogger.Debug(LogCategory, $"Decision reasoning:\n{schedule.DecisionLog}");

            return schedule;
        }

        /// <summary>
        /// Generate a single schedule block for a time period.
        /// This method is retained for compatibility; GenerateBlockWithNeeds is preferred for full feature support.
        /// </summary>
        private ScheduledBlock GenerateBlock(TimeBlock timeBlock, LordObjective objective, int playerTier, string playerFormation)
        {
            // Use the player's current duty for filtering
            var playerDuty = GetPlayerDuty();
            
            // Get suitable activities for this time block and objective
            var suitableActivities = GetSuitableActivities(timeBlock, objective, playerTier, playerFormation, playerDuty);

            if (suitableActivities.Count == 0)
            {
                ModLogger.Warn(LogCategory, $"No suitable activities for {timeBlock}, using default Rest");
                suitableActivities = _config.Activities.Where(a => a.BlockType == ScheduleBlockType.Rest).ToList();
            }

            // Select an activity based on the current selection logic.
            var selectedActivity = SelectActivity(suitableActivities, timeBlock);

            // Create block with display text (use Title/Description if available, fallback to keys)
            var block = new ScheduledBlock(
                timeBlock,
                selectedActivity.BlockType,
                selectedActivity.Title ?? selectedActivity.TitleKey ?? selectedActivity.Id,
                selectedActivity.Description ?? selectedActivity.DescriptionKey ?? "No description",
                selectedActivity.FatigueCost,
                selectedActivity.XPReward,
                selectedActivity.EventChance,
                CampaignTime.Now
            );
            
            // Set activity tracking fields for schedule modification
            block.ActivityId = selectedActivity.Id;
            block.ActivityTitle = selectedActivity.Title ?? selectedActivity.TitleKey ?? selectedActivity.Id;
            block.DurationHours = 4; // Standard block duration

            ModLogger.Debug(LogCategory, $"  {timeBlock}: {selectedActivity.Id} (Fatigue: {selectedActivity.FatigueCost}, XP: {selectedActivity.XPReward})");

            return block;
        }

        /// <summary>
        /// Get activities suitable for the current context.
        /// Filters by time block preference, lord objective, tier, formation, and duty.
        /// </summary>
        private List<ScheduleActivityDefinition> GetSuitableActivities(TimeBlock timeBlock, LordObjective objective, 
            int playerTier, string playerFormation, string playerDuty)
        {
            var suitable = new List<ScheduleActivityDefinition>();

            foreach (var activity in _config.Activities)
            {
                // Check tier requirements
                if (activity.MinTier > playerTier)
                    continue;
                if (activity.MaxTier > 0 && activity.MaxTier < playerTier)
                    continue;

                // Check formation requirements (case-insensitive)
                if (activity.RequiredFormations.Count > 0 && 
                    !activity.RequiredFormations.Any(f => f.Equals(playerFormation, StringComparison.OrdinalIgnoreCase)))
                    continue;
                
                // Check duty requirements (case-insensitive)
                if (activity.RequiredDuties != null && activity.RequiredDuties.Count > 0 && 
                    !activity.RequiredDuties.Any(d => d.Equals(playerDuty, StringComparison.OrdinalIgnoreCase)))
                    continue;

                // Prefer activities that match this time block
                bool isPreferredTime = activity.PreferredTimeBlocks.Contains(timeBlock);

                // Prefer activities favored by current objective
                bool isFavoredByObjective = activity.FavoredByObjectives.Contains(objective.ToString());

                // Simple scoring logic for activity selection.
                if (isPreferredTime || isFavoredByObjective)
                {
                    suitable.Add(activity);
                }
            }

            // If no preferred activities, allow any that meet basic requirements
            if (suitable.Count == 0)
            {
                suitable = _config.Activities.Where(a =>
                    a.MinTier <= playerTier &&
                    (a.MaxTier == 0 || a.MaxTier >= playerTier) &&
                    (a.RequiredFormations.Count == 0 || a.RequiredFormations.Any(f => f.Equals(playerFormation, StringComparison.OrdinalIgnoreCase))) &&
                    (a.RequiredDuties == null || a.RequiredDuties.Count == 0 || a.RequiredDuties.Any(d => d.Equals(playerDuty, StringComparison.OrdinalIgnoreCase)))
                ).ToList();
            }

            return suitable;
        }

        /// <summary>
        /// Select an activity from suitable options.
        /// Activity selection based on time block defaults.
        /// </summary>
        private ScheduleActivityDefinition SelectActivity(List<ScheduleActivityDefinition> activities, TimeBlock timeBlock)
        {
            if (activities.Count == 0)
            {
                // Fallback to rest
                return _config.Activities.First(a => a.BlockType == ScheduleBlockType.Rest);
            }

            // Time-based defaults for the four daily blocks.
            ScheduleBlockType preferredType = timeBlock switch
            {
                TimeBlock.Morning => ScheduleBlockType.TrainingDrill,
                TimeBlock.Afternoon => ScheduleBlockType.WorkDetail,
                TimeBlock.Dusk => ScheduleBlockType.FreeTime,
                TimeBlock.Night => ScheduleBlockType.Rest,
                _ => ScheduleBlockType.FreeTime
            };

            // Try to find preferred type
            var preferred = activities.FirstOrDefault(a => a.BlockType == preferredType);
            if (preferred != null)
                return preferred;

            // Otherwise return first suitable activity
            return activities[0];
        }

        private int GetPlayerTier()
        {
            var enlistment = EnlistmentBehavior.Instance;
            return enlistment?.EnlistmentTier ?? 1;
        }

        private string GetPlayerFormation()
        {
            var dutiesBehavior = Enlisted.Features.Assignments.Behaviors.EnlistedDutiesBehavior.Instance;
            return dutiesBehavior?.PlayerFormation ?? "infantry";
        }
        
        /// <summary>
        /// Get the player's currently assigned duty (e.g., "runner", "scout", "field_medic").
        /// </summary>
        private string GetPlayerDuty()
        {
            var enlistment = EnlistmentBehavior.Instance;
            return enlistment?.SelectedDuty ?? "runner";
        }
        
        /// <summary>
        /// Check if the army is currently at peace (no active wars or idle).
        /// During peace, soldiers get more free time.
        /// </summary>
        private bool IsArmyAtPeace(MobileParty army)
        {
            if (army == null) return true;
            
            var faction = army.MapFaction;
            if (faction == null) return true;
            
            // Check if faction is at war with anyone
            foreach (var otherFaction in Campaign.Current.Factions)
            {
                if (otherFaction != faction && faction.IsAtWarWith(otherFaction))
                {
                    return false; // At war
                }
            }
            
            return true; // At peace
        }
        
        /// <summary>
        /// Determine if the army is in a hardship situation where soldiers might have to work overtime.
        /// Hardship = active siege, recent battle, critical lance needs, urgent lord orders.
        /// </summary>
        private bool IsHardshipSituation(MobileParty army, LordObjective objective, LordOrderPriority priority, LanceNeedsState lanceNeeds)
        {
            // Urgent lord orders always create hardship
            if (priority == LordOrderPriority.Critical)
                return true;
            
            // Active siege or raiding is hardship
            if (objective == LordObjective.Besieging || objective == LordObjective.Raiding)
                return true;
            
            // If multiple lance needs are critical, we're in hardship (struggling to keep up)
            int criticalCount = 0;
            if (lanceNeeds.Readiness < 30) criticalCount++;
            if (lanceNeeds.Equipment < 30) criticalCount++;
            if (lanceNeeds.Supplies < 30) criticalCount++;
            if (criticalCount >= 2)
                return true;
            
            // Random chance of hardship during war (30% chance)
            if (!IsArmyAtPeace(army))
            {
                int dayOfYear = (int)(CampaignTime.Now.GetDayOfYear % 10);
                if (dayOfYear < 3) // 30% of days during war
                    return true;
            }
            
            return false;
        }
        
        // ===== Context-Aware Block Generation =====
        
        /// <summary>
        /// Generate a schedule block that considers the full context:
        /// The generation considers the time of day expectations, the player's personal needs such as exhaustion, 
        /// lance and company mission requirements, and the overall state of peace, war, or hardship.
        /// The AI tries to take care of the player but will push them when necessary.
        /// </summary>
        private ScheduledBlock GenerateContextAwareBlock(
            TimeBlock timeBlock,
            LordObjective lordObjective,
            LordOrderPriority lordPriority,
            LanceNeedsState lanceNeeds,
            List<LanceNeed> criticalNeeds,
            List<LanceNeed> poorNeeds,
            string assignmentStrategy,
            int playerTier,
            string playerFormation,
            string playerDuty,
            bool isAtPeace,
            bool isHardship,
            bool playerExhausted,
            bool playerTired)
        {
            ScheduleActivityDefinition selectedActivity = null;
            string decisionReason = "";
            
            // PRIORITY 1: Player is exhausted - they NEED rest regardless of time
            if (playerExhausted && !_usedActivitiesToday.Contains("rest"))
            {
                selectedActivity = GetActivityById("rest");
                decisionReason = "EXHAUSTED - mandatory rest (Rest at " + lanceNeeds.Rest + "%)";
                
                if (selectedActivity != null)
                {
                    _decisionLog.Add($"{timeBlock}: {selectedActivity.Id} - {decisionReason}");
                    MarkActivityUsed(selectedActivity.Id);
                    return CreateBlock(timeBlock, selectedActivity);
                }
            }
            
            // PRIORITY 2: Time-appropriate defaults with context modifications
            switch (timeBlock)
            {
                case TimeBlock.Morning:
                    selectedActivity = GenerateMorningActivity(lordObjective, lanceNeeds, criticalNeeds, 
                        playerTier, playerFormation, playerDuty, isHardship, playerTired, out decisionReason);
                    break;
                    
                case TimeBlock.Afternoon:
                    selectedActivity = GenerateAfternoonActivity(lordObjective, lanceNeeds, criticalNeeds, poorNeeds,
                        playerTier, playerFormation, playerDuty, isAtPeace, isHardship, playerTired, out decisionReason);
                    break;
                    
                case TimeBlock.Dusk:
                    selectedActivity = GenerateDuskActivity(lordObjective, lanceNeeds, criticalNeeds,
                        playerTier, playerFormation, playerDuty, isAtPeace, isHardship, playerTired, out decisionReason);
                    break;
                    
                case TimeBlock.Night:
                    selectedActivity = GenerateNightActivity(lordObjective, lanceNeeds, criticalNeeds,
                        playerTier, playerFormation, playerDuty, isHardship, out decisionReason);
                    break;
            }
            
            // Fallback
            if (selectedActivity == null)
            {
                selectedActivity = GetDefaultActivity(timeBlock);
                decisionReason = "Default activity";
            }
            
            _decisionLog.Add($"{timeBlock}: {selectedActivity.Id} - {decisionReason}");
            MarkActivityUsed(selectedActivity.Id);
            
            return CreateBlock(timeBlock, selectedActivity);
        }
        
        /// <summary>
        /// Morning: Primary duty time. Training, patrol, or work.
        /// During hardship: might be urgent duty. If tired: lighter duty.
        /// </summary>
        private ScheduleActivityDefinition GenerateMorningActivity(
            LordObjective lordObjective,
            LanceNeedsState lanceNeeds,
            List<LanceNeed> criticalNeeds,
            int playerTier,
            string playerFormation,
            string playerDuty,
            bool isHardship,
            bool playerTired,
            out string reason)
        {
            // If player is tired but not exhausted, give them lighter duty
            if (playerTired && !isHardship)
            {
                var lightDuty = GetActivityById("work_detail") ?? GetActivityById("sentry_duty");
                if (lightDuty != null)
                {
                    reason = "Light duty (recovering from fatigue)";
                    return lightDuty;
                }
            }
            
            // During hardship, assign urgent duty based on objective
            if (isHardship)
            {
                var urgentActivity = SelectDutyActivity(TimeBlock.Morning, lordObjective, playerTier, playerFormation, playerDuty, out reason);
                if (urgentActivity != null)
                {
                    reason = "Urgent duty - " + reason;
                    return urgentActivity;
                }
            }
            
            // Normal: assign duty based on player's role and lord's orders
            var dutyActivity = SelectDutyActivity(TimeBlock.Morning, lordObjective, playerTier, playerFormation, playerDuty, out reason);
            if (dutyActivity != null)
                return dutyActivity;
            
            // Fallback to training
            reason = "Morning training drill";
            return GetActivityById("training") ?? GetDefaultActivity(TimeBlock.Morning);
        }
        
        /// <summary>
        /// Afternoon: Secondary duty or free time during peace.
        /// During hardship: no free time, full duty. If player is tired: consider rest.
        /// </summary>
        private ScheduleActivityDefinition GenerateAfternoonActivity(
            LordObjective lordObjective,
            LanceNeedsState lanceNeeds,
            List<LanceNeed> criticalNeeds,
            List<LanceNeed> poorNeeds,
            int playerTier,
            string playerFormation,
            string playerDuty,
            bool isAtPeace,
            bool isHardship,
            bool playerTired,
            out string reason)
        {
            // If player is tired and NOT in hardship, give them rest or free time
            if (playerTired && !isHardship && !_usedActivitiesToday.Contains("rest"))
            {
                var rest = GetActivityById("rest");
                if (rest != null)
                {
                    reason = "Afternoon rest (recovering)";
                    return rest;
                }
            }
            
            // Peace time and not hardship: free time in afternoon
            if (isAtPeace && !isHardship)
            {
                var freeTime = GetActivityById("free_time");
                if (freeTime != null)
                {
                    reason = "Peace time - afternoon free";
                    return freeTime;
                }
            }
            
            // Address poor needs if not in hardship
            if (poorNeeds.Count > 0 && !isHardship)
            {
                var needActivity = SelectActivityForPoorNeed(poorNeeds, lanceNeeds, TimeBlock.Afternoon, 
                    playerTier, playerFormation, playerDuty, out reason);
                if (needActivity != null)
                    return needActivity;
            }
            
            // Normal duty
            var dutyActivity = SelectDutyActivity(TimeBlock.Afternoon, lordObjective, playerTier, playerFormation, playerDuty, out reason);
            if (dutyActivity != null)
                return dutyActivity;
            
            // Fallback to work detail
            reason = "Afternoon work detail";
            return GetActivityById("work_detail") ?? GetDefaultActivity(TimeBlock.Afternoon);
        }
        
        /// <summary>
        /// Dusk: Usually free time. Soldiers need personal time.
        /// During hardship: might have to work, but AI should try to give at least some free time.
        /// </summary>
        private ScheduleActivityDefinition GenerateDuskActivity(
            LordObjective lordObjective,
            LanceNeedsState lanceNeeds,
            List<LanceNeed> criticalNeeds,
            int playerTier,
            string playerFormation,
            string playerDuty,
            bool isAtPeace,
            bool isHardship,
            bool playerTired,
            out string reason)
        {
            // During severe hardship AND morale is okay: might have to work overtime
            if (isHardship && lanceNeeds.Morale > 40 && !playerTired)
            {
                // 50% chance of overtime during hardship
                int dayCheck = (int)(CampaignTime.Now.GetDayOfYear % 2);
                if (dayCheck == 0)
                {
                    var dutyActivity = SelectDutyActivity(TimeBlock.Dusk, lordObjective, playerTier, playerFormation, playerDuty, out reason);
                    if (dutyActivity != null)
                    {
                        reason = "OVERTIME - " + reason + " (hardship conditions)";
                        return dutyActivity;
                    }
                }
            }
            
            // Normal: free time
            var freeTime = GetActivityById("free_time");
            if (freeTime != null)
            {
                if (criticalNeeds.Contains(LanceNeed.Morale))
                    reason = "Free time (critical for morale)";
                else if (playerTired)
                    reason = "Free time (soldier needs rest)";
                else
                    reason = "Evening free time";
                return freeTime;
            }
            
            // Fallback
            reason = "Personal time";
            return new ScheduleActivityDefinition
            {
                Id = "free_time_fallback",
                Title = "Free Time",
                Description = "Personal time to relax and socialize.",
                BlockType = ScheduleBlockType.FreeTime,
                FatigueCost = 0,
                XPReward = 0,
                EventChance = 0.1f
            };
        }
        
        /// <summary>
        /// Night: Usually rest. Everyone needs to sleep.
        /// During severe hardship: might have watch duty, but MUST rest if exhausted.
        /// </summary>
        private ScheduleActivityDefinition GenerateNightActivity(
            LordObjective lordObjective,
            LanceNeedsState lanceNeeds,
            List<LanceNeed> criticalNeeds,
            int playerTier,
            string playerFormation,
            string playerDuty,
            bool isHardship,
            out string reason)
        {
            // If rest is already used today AND we're in hardship, might have night watch
            if (_usedActivitiesToday.Contains("rest") && isHardship && lanceNeeds.Rest > 30)
            {
                // Player already rested today, can do night watch
                var watchDuty = GetActivityById("sentry_duty") ?? GetActivityById("lookout_signals");
                if (watchDuty != null)
                {
                    reason = "Night watch (already rested today)";
                    return watchDuty;
                }
            }
            
            // Normal: rest
            var rest = GetActivityById("rest");
            if (rest != null)
            {
                if (criticalNeeds.Contains(LanceNeed.Rest))
                    reason = "MANDATORY rest (lance is exhausted)";
                else if (lanceNeeds.Rest < 50)
                    reason = "Night rest (needed)";
                else
                    reason = "Night rest period";
                return rest;
            }
            
            // Fallback
            reason = "Sleep time";
            return new ScheduleActivityDefinition
            {
                Id = "rest_fallback",
                Title = "Rest",
                Description = "Sleep and recover from the day's fatigue.",
                BlockType = ScheduleBlockType.Rest,
                FatigueCost = -12,
                XPReward = 0,
                EventChance = 0.05f
            };
        }
        
        /// <summary>
        /// Select a duty activity appropriate for the time block and lord's objective.
        /// </summary>
        private ScheduleActivityDefinition SelectDutyActivity(
            TimeBlock timeBlock, 
            LordObjective objective, 
            int playerTier, 
            string playerFormation, 
            string playerDuty,
            out string reason)
        {
            // First, try duty-specific activities (based on player's assigned duty)
            var dutyActivities = _config.Activities
                .Where(a => 
                    a.RequiredDuties != null && 
                    a.RequiredDuties.Count > 0 &&
                    a.RequiredDuties.Any(d => d.Equals(playerDuty, StringComparison.OrdinalIgnoreCase)) &&
                    a.MinTier <= playerTier &&
                    (a.MaxTier == 0 || a.MaxTier >= playerTier) &&
                    a.BlockType != ScheduleBlockType.Rest &&
                    a.BlockType != ScheduleBlockType.FreeTime &&
                    !_usedActivitiesToday.Contains(a.Id))
                .ToList();
            
            if (dutyActivities.Count > 0)
            {
                // Prefer activities that match lord's objective
                var objectiveMatch = dutyActivities.FirstOrDefault(a => a.FavoredByObjectives.Contains(objective.ToString()));
                if (objectiveMatch != null)
                {
                    reason = $"{playerDuty} duty - {objective}";
                    return objectiveMatch;
                }
                
                reason = $"{playerDuty} duty assignment";
                return dutyActivities[0];
            }
            
            // Fall back to general duty activities
            var generalActivities = _config.Activities
                .Where(a =>
                    (a.RequiredDuties == null || a.RequiredDuties.Count == 0) &&
                    a.MinTier <= playerTier &&
                    (a.MaxTier == 0 || a.MaxTier >= playerTier) &&
                    a.BlockType != ScheduleBlockType.Rest &&
                    a.BlockType != ScheduleBlockType.FreeTime &&
                    !_usedActivitiesToday.Contains(a.Id) &&
                    a.FavoredByObjectives.Contains(objective.ToString()))
                .ToList();
            
            if (generalActivities.Count > 0)
            {
                reason = $"General duty - {objective}";
                return generalActivities[0];
            }
            
            reason = "No suitable duty found";
            return null;
        }
        
        /// <summary>
        /// Get an activity by its ID from the config.
        /// </summary>
        private ScheduleActivityDefinition GetActivityById(string id)
        {
            return _config.Activities.FirstOrDefault(a => a.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        }
        
        /// <summary>
        /// Create a ScheduledBlock from an activity definition.
        /// </summary>
        private ScheduledBlock CreateBlock(TimeBlock timeBlock, ScheduleActivityDefinition activity)
        {
            var block = new ScheduledBlock(
                timeBlock,
                activity.BlockType,
                activity.Title ?? activity.TitleKey ?? activity.Id,
                activity.Description ?? activity.DescriptionKey ?? "No description",
                activity.FatigueCost,
                activity.XPReward,
                activity.EventChance,
                CampaignTime.Now
            );
            
            block.ActivityId = activity.Id;
            block.ActivityTitle = activity.Title ?? activity.TitleKey ?? activity.Id;
            block.DurationHours = 6; // 4 blocks = 24 hours, so each is 6 hours
            
            return block;
        }
        
        // ===== Activity Scheduling Constraints =====
        
        /// <summary>
        /// Check if an activity can be scheduled for a given time block.
        /// Enforces once-per-day limits and preferred time block restrictions.
        /// </summary>
        private bool CanScheduleActivity(ScheduleActivityDefinition activity, TimeBlock timeBlock)
        {
            if (activity == null) return false;
            
            // Check if this is a once-per-day activity that's already been scheduled
            if (OncePerDayActivities.Contains(activity.Id) && _usedActivitiesToday.Contains(activity.Id))
            {
                ModLogger.Debug(LogCategory, $"Activity '{activity.Id}' already scheduled today (once-per-day limit)");
                return false;
            }
            
            // Check if activity has preferred time blocks and if current time is appropriate
            if (activity.PreferredTimeBlocks != null && activity.PreferredTimeBlocks.Count > 0)
            {
                if (!activity.PreferredTimeBlocks.Contains(timeBlock))
                {
                    // Activity has preferred times and this isn't one of them
                    // Allow scheduling but with lower priority (handled by caller)
                    ModLogger.Debug(LogCategory, $"Activity '{activity.Id}' not preferred for {timeBlock}");
                    return true; // Still allow, but caller should prefer other activities
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// Check if an activity is preferred for this time block (strict match).
        /// Returns true only if the activity explicitly lists this time block as preferred.
        /// </summary>
        private bool IsActivityPreferredForTimeBlock(ScheduleActivityDefinition activity, TimeBlock timeBlock)
        {
            if (activity?.PreferredTimeBlocks == null || activity.PreferredTimeBlocks.Count == 0)
                return true; // No preferences = always suitable
            
            return activity.PreferredTimeBlocks.Contains(timeBlock);
        }
        
        /// <summary>
        /// Mark an activity as used today (for once-per-day tracking).
        /// </summary>
        private void MarkActivityUsed(string activityId)
        {
            if (!string.IsNullOrEmpty(activityId))
            {
                _usedActivitiesToday.Add(activityId);
            }
        }
        
        /// <summary>
        /// Filter activities to only include those available for scheduling at this time block.
        /// Respects once-per-day limits and prefers activities suited for the time block.
        /// </summary>
        private List<ScheduleActivityDefinition> FilterAvailableActivities(
            List<ScheduleActivityDefinition> activities, 
            TimeBlock timeBlock)
        {
            var available = new List<ScheduleActivityDefinition>();
            var fallbacks = new List<ScheduleActivityDefinition>();
            
            foreach (var activity in activities)
            {
                if (!CanScheduleActivity(activity, timeBlock))
                    continue;
                
                // Prioritize activities that prefer this time block
                if (IsActivityPreferredForTimeBlock(activity, timeBlock))
                {
                    available.Add(activity);
                }
                else
                {
                    fallbacks.Add(activity);
                }
            }
            
            // Return preferred activities first, then fallbacks
            available.AddRange(fallbacks);
            return available;
        }

        // ===== Need-Aware Assignment Logic =====

        /// <summary>
        /// Log current needs assessment for decision tracking.
        /// </summary>
        private void LogNeedsAssessment(LanceNeedsState needs)
        {
            _decisionLog.Add($"Lance Needs Assessment:");
            _decisionLog.Add($"  Readiness: {needs.Readiness}% ({LanceNeedsManager.GetNeedStatusText(needs.Readiness)})");
            _decisionLog.Add($"  Equipment: {needs.Equipment}% ({LanceNeedsManager.GetNeedStatusText(needs.Equipment)})");
            _decisionLog.Add($"  Morale: {needs.Morale}% ({LanceNeedsManager.GetNeedStatusText(needs.Morale)})");
            _decisionLog.Add($"  Rest: {needs.Rest}% ({LanceNeedsManager.GetNeedStatusText(needs.Rest)})");
            _decisionLog.Add($"  Supplies: {needs.Supplies}% ({LanceNeedsManager.GetNeedStatusText(needs.Supplies)})");
        }

        /// <summary>
        /// Get list of critical needs (< 30%).
        /// </summary>
        private List<LanceNeed> GetCriticalNeeds(LanceNeedsState needs)
        {
            var critical = new List<LanceNeed>();
            int threshold = 30;

            if (needs.Readiness < threshold) critical.Add(LanceNeed.Readiness);
            if (needs.Equipment < threshold) critical.Add(LanceNeed.Equipment);
            if (needs.Morale < threshold) critical.Add(LanceNeed.Morale);
            if (needs.Rest < threshold) critical.Add(LanceNeed.Rest);
            if (needs.Supplies < threshold) critical.Add(LanceNeed.Supplies);

            if (critical.Count > 0)
            {
                _decisionLog.Add($"CRITICAL NEEDS: {string.Join(", ", critical)} require immediate attention!");
            }

            return critical;
        }

        /// <summary>
        /// Get list of poor needs (30-50%).
        /// </summary>
        private List<LanceNeed> GetPoorNeeds(LanceNeedsState needs)
        {
            var poor = new List<LanceNeed>();

            if (needs.Readiness >= 30 && needs.Readiness < 50) poor.Add(LanceNeed.Readiness);
            if (needs.Equipment >= 30 && needs.Equipment < 50) poor.Add(LanceNeed.Equipment);
            if (needs.Morale >= 30 && needs.Morale < 50) poor.Add(LanceNeed.Morale);
            if (needs.Rest >= 30 && needs.Rest < 50) poor.Add(LanceNeed.Rest);
            if (needs.Supplies >= 30 && needs.Supplies < 50) poor.Add(LanceNeed.Supplies);

            if (poor.Count > 0)
            {
                _decisionLog.Add($"Poor needs: {string.Join(", ", poor)} - should address soon");
            }

            return poor;
        }

        /// <summary>
        /// Determine overall assignment strategy based on needs and lord priority.
        /// Core decision-making logic for activity assignments.
        /// </summary>
        private string DetermineAssignmentStrategy(List<LanceNeed> criticalNeeds, 
            List<LanceNeed> poorNeeds, LordOrderPriority lordPriority)
        {
            // Critical needs always take priority unless lord order is also critical
            if (criticalNeeds.Count > 0)
            {
                if (lordPriority == LordOrderPriority.Critical)
                {
                    return "CRISIS_BALANCE"; // Try to address both
                }
                return "NEEDS_FIRST"; // Address critical needs before lord orders
            }

            // No critical needs, check for poor needs
            if (poorNeeds.Count > 0)
            {
                if (lordPriority == LordOrderPriority.High || lordPriority == LordOrderPriority.Critical)
                {
                    return "LORD_PRIORITY"; // Lord orders take precedence, let poor needs slide
                }
                return "BALANCED"; // Try to mix lord orders with need recovery
            }

            // All needs healthy
            return "LORD_FOCUS"; // Primarily follow lord's orders
        }

        /// <summary>
        /// Generate a single schedule block with need-aware logic.
        /// Intelligent activity selection based on needs and priorities.
        /// </summary>
        private ScheduledBlock GenerateBlockWithNeeds(
            TimeBlock timeBlock,
            LordObjective lordObjective,
            LordOrderPriority lordPriority,
            LanceNeedsState lanceNeeds,
            List<LanceNeed> criticalNeeds,
            List<LanceNeed> poorNeeds,
            string assignmentStrategy,
            int playerTier,
            string playerFormation,
            string playerDuty)
        {
            ScheduleActivityDefinition selectedActivity = null;
            string decisionReason = "";

            // Strategy-based selection
            switch (assignmentStrategy)
            {
                case "NEEDS_FIRST":
                    // Address most critical need
                    selectedActivity = SelectActivityForCriticalNeed(criticalNeeds, lanceNeeds, timeBlock, 
                        playerTier, playerFormation, playerDuty, out decisionReason);
                    break;

                case "CRISIS_BALANCE":
                    // Try to balance critical lord order with critical needs
                    // Alternate between needs and orders
                    if (timeBlock == TimeBlock.Morning)
                    {
                        selectedActivity = SelectActivityForCriticalNeed(criticalNeeds, lanceNeeds, timeBlock, 
                            playerTier, playerFormation, playerDuty, out decisionReason);
                        decisionReason += " (crisis management - addressing need)";
                    }
                    else
                    {
                        selectedActivity = SelectActivityForLordOrder(lordObjective, timeBlock, 
                            playerTier, playerFormation, playerDuty, out decisionReason);
                        decisionReason += " (crisis management - fulfilling lord order)";
                    }
                    break;

                case "LORD_PRIORITY":
                    // Lord orders take precedence, but try to address needs in dusk/night
                    if (timeBlock == TimeBlock.Dusk || timeBlock == TimeBlock.Night)
                    {
                        selectedActivity = SelectActivityForPoorNeed(poorNeeds, lanceNeeds, timeBlock, 
                            playerTier, playerFormation, playerDuty, out decisionReason);
                        decisionReason += " (addressing needs when possible)";
                    }
                    else
                    {
                        selectedActivity = SelectActivityForLordOrder(lordObjective, timeBlock, 
                            playerTier, playerFormation, playerDuty, out decisionReason);
                    }
                    break;

                case "BALANCED":
                    // Mix lord orders with need recovery
                    if (timeBlock == TimeBlock.Morning || timeBlock == TimeBlock.Dusk)
                    {
                        selectedActivity = SelectActivityForPoorNeed(poorNeeds, lanceNeeds, timeBlock, 
                            playerTier, playerFormation, playerDuty, out decisionReason);
                    }
                    else
                    {
                        selectedActivity = SelectActivityForLordOrder(lordObjective, timeBlock, 
                            playerTier, playerFormation, playerDuty, out decisionReason);
                    }
                    break;

                case "LORD_FOCUS":
                default:
                    // Needs are healthy, follow lord's orders primarily
                    selectedActivity = SelectActivityForLordOrder(lordObjective, timeBlock, 
                        playerTier, playerFormation, playerDuty, out decisionReason);
                    
                    // If no specific lord order activity, use base schedule for variety
                    if (selectedActivity == null)
                    {
                        selectedActivity = GetBaseScheduleActivity(timeBlock, lordObjective, playerTier, playerFormation, playerDuty);
                        decisionReason = "Standard military routine";
                    }
                    break;
            }

            // Ultimate fallback if still no activity selected
            if (selectedActivity == null)
            {
                selectedActivity = GetDefaultActivity(timeBlock);
                decisionReason = "Default activity (fallback)";
            }

            // Log decision
            _decisionLog.Add($"{timeBlock}: {selectedActivity.Id} - {decisionReason}");
            
            // Mark activity as used for today (prevents duplicates for once-per-day activities)
            MarkActivityUsed(selectedActivity.Id);

            // Create block with display text (use Title/Description if available, fallback to keys)
            var block = new ScheduledBlock(
                timeBlock,
                selectedActivity.BlockType,
                selectedActivity.Title ?? selectedActivity.TitleKey ?? selectedActivity.Id,
                selectedActivity.Description ?? selectedActivity.DescriptionKey ?? "No description",
                selectedActivity.FatigueCost,
                selectedActivity.XPReward,
                selectedActivity.EventChance,
                CampaignTime.Now
            );
            
            // Set activity tracking fields for schedule modification
            block.ActivityId = selectedActivity.Id;
            block.ActivityTitle = selectedActivity.Title ?? selectedActivity.TitleKey ?? selectedActivity.Id;
            block.DurationHours = 4; // Standard block duration
            
            return block;
        }

        /// <summary>
        /// Select activity to address the most critical need.
        /// </summary>
        private ScheduleActivityDefinition SelectActivityForCriticalNeed(
            List<LanceNeed> criticalNeeds,
            LanceNeedsState lanceNeeds,
            TimeBlock timeBlock,
            int playerTier,
            string playerFormation,
            string playerDuty,
            out string reason)
        {
            if (criticalNeeds.Count == 0)
            {
                reason = "No critical needs";
                return null;
            }

            // Find the most critical need (lowest value)
            LanceNeed mostCritical = criticalNeeds[0];
            int lowestValue = lanceNeeds.GetNeedValue(mostCritical);

            foreach (var need in criticalNeeds)
            {
                int value = lanceNeeds.GetNeedValue(need);
                if (value < lowestValue)
                {
                    lowestValue = value;
                    mostCritical = need;
                }
            }

            // Find activities that recover this need
            var recoveryActivities = _config.Activities.Where(a =>
                a.NeedRecovery != null &&
                a.NeedRecovery.ContainsKey(mostCritical.ToString()) &&
                a.MinTier <= playerTier &&
                (a.MaxTier == 0 || a.MaxTier >= playerTier) &&
                (a.RequiredFormations.Count == 0 || a.RequiredFormations.Any(f => f.Equals(playerFormation, StringComparison.OrdinalIgnoreCase))) &&
                (a.RequiredDuties == null || a.RequiredDuties.Count == 0 || a.RequiredDuties.Any(d => d.Equals(playerDuty, StringComparison.OrdinalIgnoreCase)))
            ).OrderByDescending(a => a.NeedRecovery[mostCritical.ToString()]).ToList();

            if (recoveryActivities.Count > 0)
            {
                reason = $"Addressing CRITICAL {mostCritical} ({lowestValue}%)";
                return recoveryActivities[0];
            }

            reason = $"No activity found for critical {mostCritical}";
            return null;
        }

        /// <summary>
        /// Select activity to address poor needs (not critical but low).
        /// </summary>
        private ScheduleActivityDefinition SelectActivityForPoorNeed(
            List<LanceNeed> poorNeeds,
            LanceNeedsState lanceNeeds,
            TimeBlock timeBlock,
            int playerTier,
            string playerFormation,
            string playerDuty,
            out string reason)
        {
            if (poorNeeds.Count == 0)
            {
                reason = "No poor needs to address";
                return SelectActivityForLordOrder(LordObjective.Patrolling, timeBlock, playerTier, playerFormation, playerDuty, out reason);
            }

            // Select first poor need
            LanceNeed targetNeed = poorNeeds[0];
            int needValue = lanceNeeds.GetNeedValue(targetNeed);

            // Find activities that recover this need
            var recoveryActivities = _config.Activities.Where(a =>
                a.NeedRecovery != null &&
                a.NeedRecovery.ContainsKey(targetNeed.ToString()) &&
                a.MinTier <= playerTier &&
                (a.MaxTier == 0 || a.MaxTier >= playerTier) &&
                (a.RequiredFormations.Count == 0 || a.RequiredFormations.Any(f => f.Equals(playerFormation, StringComparison.OrdinalIgnoreCase))) &&
                (a.RequiredDuties == null || a.RequiredDuties.Count == 0 || a.RequiredDuties.Any(d => d.Equals(playerDuty, StringComparison.OrdinalIgnoreCase)))
            ).OrderByDescending(a => a.NeedRecovery[targetNeed.ToString()]).ToList();

            if (recoveryActivities.Count > 0)
            {
                reason = $"Improving {targetNeed} ({needValue}%)";
                return recoveryActivities[0];
            }

            reason = $"No activity found for {targetNeed}";
            return null;
        }

        /// <summary>
        /// Select activity that fulfills lord's orders.
        /// </summary>
        private ScheduleActivityDefinition SelectActivityForLordOrder(
            LordObjective lordObjective,
            TimeBlock timeBlock,
            int playerTier,
            string playerFormation,
            string playerDuty,
            out string reason)
        {
            // Get activities favored by this lord objective
            var lordActivities = _config.Activities.Where(a =>
                a.FavoredByObjectives.Contains(lordObjective.ToString()) &&
                a.MinTier <= playerTier &&
                (a.MaxTier == 0 || a.MaxTier >= playerTier) &&
                (a.RequiredFormations.Count == 0 || a.RequiredFormations.Any(f => f.Equals(playerFormation, StringComparison.OrdinalIgnoreCase))) &&
                (a.RequiredDuties == null || a.RequiredDuties.Count == 0 || a.RequiredDuties.Any(d => d.Equals(playerDuty, StringComparison.OrdinalIgnoreCase)))
            ).ToList();

            // Prefer activities that also match the time block
            var timePreferredActivities = lordActivities.Where(a => a.PreferredTimeBlocks.Contains(timeBlock)).ToList();

            if (timePreferredActivities.Count > 0)
            {
                reason = $"Fulfilling lord's {lordObjective} orders";
                return timePreferredActivities[0];
            }

            if (lordActivities.Count > 0)
            {
                reason = $"Fulfilling lord's {lordObjective} orders";
                return lordActivities[0];
            }

            // Fallback to general suitable activities
            var suitableActivities = GetSuitableActivities(timeBlock, lordObjective, playerTier, playerFormation, playerDuty);
            if (suitableActivities.Count > 0)
            {
                reason = $"Following orders ({lordObjective})";
                return suitableActivities[0];
            }

            reason = "No suitable activity for lord's orders";
            return null;
        }

        /// <summary>
        /// Get default activity for a time block using a standard military routine.
        /// This creates a balanced day even when no specific activities match the context.
        /// The standard routine includes morning formation and drills, followed by primary duties
        /// like patrol or work in the morning and afternoon, with free time in the evening and dusk, 
        /// and rest at night.
        /// </summary>
        private ScheduleActivityDefinition GetDefaultActivity(TimeBlock timeBlock)
        {
            ScheduleBlockType defaultType = timeBlock switch
            {
                TimeBlock.Morning => ScheduleBlockType.TrainingDrill,
                TimeBlock.Afternoon => ScheduleBlockType.WorkDetail,
                TimeBlock.Dusk => ScheduleBlockType.FreeTime,
                TimeBlock.Night => ScheduleBlockType.Rest,
                _ => ScheduleBlockType.FreeTime
            };

            // Try to find an activity matching the default type
            var activity = _config.Activities.FirstOrDefault(a => a.BlockType == defaultType);
            
            // If no match, try these fallbacks in order
            if (activity == null)
            {
                var fallbackOrder = new[] 
                { 
                    ScheduleBlockType.FreeTime,
                    ScheduleBlockType.Rest,
                    ScheduleBlockType.PatrolDuty,
                    ScheduleBlockType.TrainingDrill,
                    ScheduleBlockType.WorkDetail
                };
                
                foreach (var fallbackType in fallbackOrder)
                {
                    activity = _config.Activities.FirstOrDefault(a => a.BlockType == fallbackType);
                    if (activity != null) break;
                }
            }
            
            // Ultimate fallback - create a minimal rest activity
            if (activity == null)
            {
                ModLogger.Warn(LogCategory, $"No activities available for {timeBlock}, creating inline rest");
                activity = new ScheduleActivityDefinition
                {
                    Id = "fallback_rest",
                    Title = "Rest",
                    Description = "Take time to rest and recover.",
                    BlockType = ScheduleBlockType.Rest,
                    FatigueCost = -5,
                    XPReward = 0,
                    EventChance = 0.05f
                };
            }
            
            return activity;
        }
        
        /// <summary>
        /// Get a standard base schedule for when needs are all healthy.
        /// Provides variety while following a realistic military routine.
        /// </summary>
        private ScheduleActivityDefinition GetBaseScheduleActivity(TimeBlock timeBlock, LordObjective objective, 
            int playerTier, string playerFormation, string playerDuty)
        {
            // Get all suitable activities for this context
            var suitable = GetSuitableActivities(timeBlock, objective, playerTier, playerFormation, playerDuty);
            
            if (suitable.Count > 0)
            {
                // Add variety by using a semi-random selection based on game day
                int dayIndex = (int)(CampaignTime.Now.GetDayOfYear % 7);
                int activityIndex = (dayIndex + (int)timeBlock) % suitable.Count;
                return suitable[activityIndex];
            }
            
            // Fallback to time-appropriate defaults
            return GetDefaultActivity(timeBlock);
        }
    }
}

