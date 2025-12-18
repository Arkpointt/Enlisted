using System;
// Removed: CampBulletinIntegration no longer used - bulletin screen deleted
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Lances.Events;
using Enlisted.Features.Schedule.Behaviors;
using Enlisted.Features.Schedule.Events;
using Enlisted.Features.Schedule.Models;
using Enlisted.Mod.Core.Logging;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace Enlisted.Features.Schedule.Core
{
    /// <summary>
    /// Executes schedule blocks when their time arrives.
    /// Applies fatigue costs, triggers events, awards XP, and marks completion.
    /// Phase 4: Basic execution with fatigue and XP.
    /// </summary>
    public static class ScheduleExecutor
    {
        private const string LogCategory = "ScheduleExecutor";

        /// <summary>
        /// Execute the current schedule block.
        /// Called when a new time block begins or player manually starts an activity.
        /// </summary>
        public static void ExecuteScheduleBlock(ScheduledBlock block, Hero player = null)
        {
            if (block == null)
            {
                ModLogger.Warn(LogCategory, "Cannot execute null schedule block");
                return;
            }

            if (block.IsCompleted)
            {
                ModLogger.Debug(LogCategory, $"Block {block.Title} already completed, skipping execution");
                return;
            }

            // Use player hero if not provided
            if (player == null)
            {
                player = Hero.MainHero;
            }

            ModLogger.Info(LogCategory, $"Executing schedule block: {block.Title} ({block.TimeBlock})");

            // Mark block as active
            block.Activate();

            // Block activated - bulletin integration removed

            // Apply fatigue cost (if positive)
            if (block.FatigueCost > 0)
            {
                ApplyFatigueCost(block.FatigueCost, player);
            }
            else if (block.FatigueCost < 0)
            {
                // Negative fatigue = recovery
                RecoverFatigue(-block.FatigueCost, player);
            }

            // Roll for event trigger
            bool eventTriggered = RollForEvent(block.EventChance);
            if (eventTriggered)
            {
                ModLogger.Debug(LogCategory, $"Event triggered for {block.BlockType} (chance: {block.EventChance * 100}%)");
                // Phase 4: Event system stub - will be expanded in future phases
                TriggerScheduleEvent(block, player);
            }

            // Phase 4: Mark as completed immediately (Phase 4+ will add proper tracking)
            // In future phases, blocks will complete at the end of the time period
            // For now, we complete immediately when execution starts
            CompleteBlock(block, player);
        }

        /// <summary>
        /// Apply fatigue cost to the player.
        /// </summary>
        private static void ApplyFatigueCost(int fatigueCost, Hero player)
        {
            ModLogger.Debug(LogCategory, $"Applying fatigue cost: +{fatigueCost}");

            // Phase 4: Basic fatigue tracking
            // TODO: Integrate with existing fatigue system if available
            // For now, just log the cost
            ModLogger.Info(LogCategory, $"{player.Name} spent {fatigueCost} fatigue on activity");

            // Future: Update player fatigue state
            // var fatigueState = GetPlayerFatigueState(player);
            // fatigueState.CurrentFatigue += fatigueCost;
        }

        /// <summary>
        /// Recover fatigue (for rest activities).
        /// </summary>
        private static void RecoverFatigue(int recoveryAmount, Hero player)
        {
            ModLogger.Debug(LogCategory, $"Recovering fatigue: -{recoveryAmount}");
            ModLogger.Info(LogCategory, $"{player.Name} recovered {recoveryAmount} fatigue from rest");

            // Future: Update player fatigue state
            // var fatigueState = GetPlayerFatigueState(player);
            // fatigueState.CurrentFatigue = Math.Max(0, fatigueState.CurrentFatigue - recoveryAmount);
        }

        /// <summary>
        /// Roll for event trigger based on event chance.
        /// </summary>
        private static bool RollForEvent(float eventChance)
        {
            if (eventChance <= 0)
                return false;

            float roll = MBRandom.RandomFloat;
            bool triggered = roll < eventChance;

            ModLogger.Debug(LogCategory, $"Event roll: {roll:F3} vs {eventChance:F3} = {(triggered ? "TRIGGERED" : "no event")}");

            return triggered;
        }

        /// <summary>
        /// Trigger a schedule event for this block.
        /// Shows an inquiry popup to the player with an event related to their current activity.
        /// Awards XP based on event type.
        /// Auto-resumes game after player responds.
        /// </summary>
        private static void TriggerScheduleEvent(ScheduledBlock block, Hero player)
        {
            ModLogger.Info(LogCategory, $"Schedule event triggered: {block.BlockType} during {block.TimeBlock}");

            // Data-driven catalog for "Continue-only" popups.
            // If the catalog is missing or has no matching entries, we fall back to the original hardcoded placeholder mapping.
            var popup = SchedulePopupEventCatalogLoader.TryPickFor(block);
            var enlistment = EnlistmentBehavior.Instance;

            string eventTitle;
            string eventMessage;
            SkillObject skill;
            int xpAmount;

            if (popup != null)
            {
                eventTitle = LanceLifeEventText.Resolve(popup.TitleId, popup.TitleFallback, GetEventTitle(block.BlockType), enlistment);
                eventMessage = LanceLifeEventText.Resolve(popup.BodyId, popup.BodyFallback, GetPlaceholderEventMessage(block.BlockType), enlistment);

                skill = SchedulePopupEventCatalogLoader.TryResolveSkill(popup.Skill);
                xpAmount = popup.Xp;

                // If authors omit skill/xp, keep the experience behavior consistent by falling back to the block-type mapping.
                if (skill == null || xpAmount <= 0)
                {
                    var fallback = GetEventReward(block.BlockType);
                    skill ??= fallback.skill;
                    if (xpAmount <= 0)
                    {
                        xpAmount = fallback.xpAmount;
                    }
                }

                ModLogger.Debug(LogCategory, $"Schedule popup picked: {popup.Id} (activity={block.ActivityId}, blockType={block.BlockType})");
            }
            else
            {
                eventTitle = GetEventTitle(block.BlockType);
                eventMessage = GetPlaceholderEventMessage(block.BlockType);
                (skill, xpAmount) = GetEventReward(block.BlockType);
            }

            var rewardLine = (skill != null && xpAmount > 0) ? $"\n\n[+{xpAmount} {skill.Name} Experience]" : string.Empty;

            ModLogger.Debug(LogCategory,
                $"Event: {eventMessage} (Reward: {(skill != null ? $"{xpAmount} {skill.Name} XP" : "none")})");
            
            // Store time control mode before showing inquiry so we can restore it after
            var previousTimeMode = Campaign.Current?.TimeControlMode ?? CampaignTimeControlMode.Stop;
            var wasTimeLocked = Campaign.Current?.TimeControlModeLock ?? false;

            // Show event popup to player
            InformationManager.ShowInquiry(
                new InquiryData(
                    titleText: eventTitle,
                    text: $"{eventMessage}{rewardLine}",
                    isAffirmativeOptionShown: true,
                    isNegativeOptionShown: false,
                    affirmativeText: new TextObject("{=ll_default_continue}Continue").ToString(),
                    negativeText: null,
                    affirmativeAction: () => 
                    {
                        // Award XP based on event type
                        if (player != null && skill != null && xpAmount > 0)
                        {
                            player.AddSkillXp(skill, xpAmount);
                            ModLogger.Info(LogCategory, $"Event XP awarded: +{xpAmount} {skill.Name}");
                            
                            // Show notification
                            InformationManager.DisplayMessage(
                                new InformationMessage(
                                    BuildXpGainedMessage(xpAmount, skill, eventTitle),
                                    Color.FromUint(0xFF88FF88) // Light green
                                )
                            );
                        }
                        
                        // Auto-resume the game after player responds
                        ResumeGameAfterEvent(previousTimeMode, wasTimeLocked);
                    },
                    negativeAction: null
                )
            );
        }

        private static string BuildXpGainedMessage(int xpAmount, SkillObject skill, string eventTitle)
        {
            var t = new TextObject("{=enl_sched_popup_xp_gained}Gained {XP} {SKILL} experience from {EVENT_TITLE}");
            t.SetTextVariable("XP", xpAmount);
            t.SetTextVariable("SKILL", skill?.Name?.ToString() ?? string.Empty);
            t.SetTextVariable("EVENT_TITLE", eventTitle ?? string.Empty);
            return t.ToString();
        }
        
        /// <summary>
        /// Resume the game after an event inquiry is dismissed.
        /// Restores previous time control mode or defaults to normal speed.
        /// </summary>
        private static void ResumeGameAfterEvent(CampaignTimeControlMode previousMode, bool wasLocked)
        {
            try
            {
                // Unpause the game engine if it's paused
                if (MBCommon.IsPaused)
                {
                    MBCommon.UnPauseGameEngine();
                }
                
                // Restore time control mode
                if (Campaign.Current != null)
                {
                    // If was stopped/paused before, resume at normal speed
                    // Otherwise restore the previous speed
                    if (previousMode == CampaignTimeControlMode.Stop || previousMode == CampaignTimeControlMode.StoppablePlay)
                    {
                        Campaign.Current.TimeControlMode = CampaignTimeControlMode.StoppablePlay;
                    }
                    else
                    {
                        Campaign.Current.TimeControlMode = previousMode;
                    }
                    
                    // Only restore lock if it was locked for a reason (not just our inquiry)
                    if (!wasLocked)
                    {
                        Campaign.Current.SetTimeControlModeLock(false);
                    }
                }
                
                ModLogger.Debug(LogCategory, $"Game resumed after event (mode: {Campaign.Current?.TimeControlMode})");
            }
            catch (System.Exception ex)
            {
                ModLogger.Warn(LogCategory, $"Failed to resume game after event: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Get the skill and XP reward for an event based on activity type.
        /// Different activities train different skills.
        /// </summary>
        private static (SkillObject skill, int xpAmount) GetEventReward(ScheduleBlockType blockType)
        {
            return blockType switch
            {
                // Combat-related activities
                ScheduleBlockType.TrainingDrill => (DefaultSkills.OneHanded, 15),
                ScheduleBlockType.PatrolDuty => (DefaultSkills.Scouting, 12),
                ScheduleBlockType.SentryDuty => (DefaultSkills.Scouting, 10),
                ScheduleBlockType.WatchDuty => (DefaultSkills.Scouting, 8),
                
                // Scouting and exploration
                ScheduleBlockType.ScoutingMission => (DefaultSkills.Scouting, 20),
                ScheduleBlockType.ForagingDuty => (DefaultSkills.Scouting, 10),
                
                // Leadership and tactics
                ScheduleBlockType.WorkDetail => (DefaultSkills.Engineering, 10),
                
                // Social activities build leadership
                ScheduleBlockType.Rest => (DefaultSkills.Leadership, 5),
                ScheduleBlockType.FreeTime => (DefaultSkills.Charm, 8),
                
                // Default
                _ => (DefaultSkills.Leadership, 5)
            };
        }
        
        /// <summary>
        /// Get event title based on block type.
        /// </summary>
        private static string GetEventTitle(ScheduleBlockType blockType)
        {
            return blockType switch
            {
                ScheduleBlockType.PatrolDuty => "Patrol Encounter",
                ScheduleBlockType.TrainingDrill => "Training Moment",
                ScheduleBlockType.SentryDuty => "Night Watch",
                ScheduleBlockType.ScoutingMission => "Scout Report",
                ScheduleBlockType.ForagingDuty => "Foraging Discovery",
                ScheduleBlockType.WorkDetail => "Work Detail",
                ScheduleBlockType.WatchDuty => "Camp Watch",
                ScheduleBlockType.Rest => "Camp Gossip",
                ScheduleBlockType.FreeTime => "Soldiers' Talk",
                _ => "Camp Event"
            };
        }

        /// <summary>
        /// Get a placeholder event message based on block type.
        /// Phase 4: Temporary until full event system is implemented.
        /// </summary>
        private static string GetPlaceholderEventMessage(ScheduleBlockType blockType)
        {
            return blockType switch
            {
                ScheduleBlockType.PatrolDuty => "While on patrol, you spot suspicious tracks in the distance.",
                ScheduleBlockType.TrainingDrill => "During training, your lance leader notices your improved form.",
                ScheduleBlockType.SentryDuty => "A late-night disturbance alerts you to movement near the camp.",
                ScheduleBlockType.ScoutingMission => "Your scouting mission reveals an enemy camp ahead.",
                ScheduleBlockType.ForagingDuty => "You discover a hidden cache of supplies in the woods.",
                ScheduleBlockType.WorkDetail => "While maintaining equipment, you notice a potential improvement.",
                ScheduleBlockType.WatchDuty => "During your watch, an officer makes an inspection round.",
                ScheduleBlockType.Rest => "You overhear interesting camp gossip during your rest.",
                ScheduleBlockType.FreeTime => "You spend your free time with fellow soldiers, building camaraderie.",
                _ => "Something interesting happens during your duty."
            };
        }

        /// <summary>
        /// Complete the schedule block and apply rewards.
        /// </summary>
        private static void CompleteBlock(ScheduledBlock block, Hero player)
        {
            if (block.IsCompleted)
                return;

            ModLogger.Debug(LogCategory, $"Completing schedule block: {block.Title}");

            // Mark as completed (this also triggers need recovery in ScheduleBehavior)
            ScheduleBehavior.Instance?.CompleteScheduleBlock(block);

            // Award XP if any
            if (block.XPReward > 0)
            {
                AwardXP(block.XPReward, player);
            }

            ModLogger.Info(LogCategory, $"Block completed: {block.Title} (XP: {block.XPReward}, Fatigue: {block.FatigueCost})");
        }

        /// <summary>
        /// Award XP to the player for completing a schedule block.
        /// </summary>
        private static void AwardXP(int xpAmount, Hero player)
        {
            if (xpAmount <= 0)
                return;

            ModLogger.Debug(LogCategory, $"Awarding {xpAmount} XP to {player.Name}");

            // Phase 4: Basic XP award
            // Bannerlord's XP system - add to character skills
            // For schedule activities, we'll award general XP
            player.AddSkillXp(DefaultSkills.Leadership, xpAmount);

            ModLogger.Info(LogCategory, $"{player.Name} gained {xpAmount} Leadership XP");

            // Future: Award specific skills based on activity type
            // Training → One Handed/Two Handed/Polearm
            // Scouting → Scouting skill
            // Patrol → Tactics
        }

        /// <summary>
        /// Check if it's time to start a new schedule block.
        /// Called from hourly tick to detect time block transitions.
        /// </summary>
        public static bool ShouldStartNewBlock(TimeBlock currentTimeBlock, TimeBlock previousTimeBlock)
        {
            return currentTimeBlock != previousTimeBlock;
        }

        /// <summary>
        /// Auto-start the current schedule block if time has arrived.
        /// Phase 4: Automatic execution when time block changes.
        /// </summary>
        public static void AutoStartCurrentBlock()
        {
            var schedule = ScheduleBehavior.Instance?.CurrentSchedule;
            if (schedule == null)
            {
                ModLogger.Debug(LogCategory, "No current schedule, skipping auto-start");
                return;
            }

            var currentBlock = ScheduleBehavior.Instance?.GetCurrentActiveBlock();
            if (currentBlock == null)
            {
                ModLogger.Debug(LogCategory, "No active block for current time, skipping auto-start");
                return;
            }

            if (currentBlock.IsCompleted)
            {
                ModLogger.Debug(LogCategory, $"Current block {currentBlock.Title} already completed");
                return;
            }

            if (currentBlock.IsActive)
            {
                ModLogger.Debug(LogCategory, $"Current block {currentBlock.Title} already active");
                return;
            }

            // Auto-start the block
            ModLogger.Info(LogCategory, $"Auto-starting schedule block: {currentBlock.Title}");
            ExecuteScheduleBlock(currentBlock, Hero.MainHero);
        }
    }
}

