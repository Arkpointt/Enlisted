using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;

namespace Enlisted.Features.Schedule.Models
{
    /// <summary>
    /// Represents a single scheduled block of time with an assigned duty.
    /// Part of a daily schedule that defines what the player should be doing during a specific time period.
    /// </summary>
    public class ScheduledBlock
    {
        /// <summary>Time period when this block occurs (Dawn, Morning, etc.)</summary>
        [SaveableProperty(1)]
        public TimeBlock TimeBlock { get; set; }
        
        /// <summary>Type of activity assigned for this block</summary>
        [SaveableProperty(2)]
        public ScheduleBlockType BlockType { get; set; }
        
        /// <summary>Display title for the activity (localized)</summary>
        [SaveableProperty(3)]
        public string Title { get; set; }
        
        /// <summary>Detailed description of what the player should do (localized)</summary>
        [SaveableProperty(4)]
        public string Description { get; set; }
        
        /// <summary>How much fatigue this activity costs (0-24)</summary>
        [SaveableProperty(5)]
        public int FatigueCost { get; set; }
        
        /// <summary>XP reward for completing this block</summary>
        [SaveableProperty(6)]
        public int XPReward { get; set; }
        
        /// <summary>Probability (0.0-1.0) that an event will trigger during this block</summary>
        [SaveableProperty(7)]
        public float EventChance { get; set; }
        
        /// <summary>Whether the player has completed this block</summary>
        [SaveableProperty(8)]
        public bool IsCompleted { get; set; }
        
        /// <summary>Whether this block is currently active (player is in this time period)</summary>
        [SaveableProperty(9)]
        public bool IsActive { get; set; }
        
        /// <summary>The campaign time when this block is scheduled to start</summary>
        [SaveableProperty(10)]
        public CampaignTime ScheduledTime { get; set; }
        
        /// <summary>
        /// Reasoning text from lance leader explaining why this duty was assigned.
        /// Used for T5-T6 players to understand AI decision-making.
        /// </summary>
        [SaveableProperty(11)]
        public string LanceLeaderReasoning { get; set; }
        
        /// <summary>
        /// The activity ID from ScheduleActivityDefinition.
        /// Used for schedule modifications and approval calculations.
        /// </summary>
        [SaveableProperty(12)]
        public string ActivityId { get; set; }
        
        /// <summary>
        /// The activity title for display.
        /// Cached here to avoid constant config lookups.
        /// </summary>
        [SaveableProperty(13)]
        public string ActivityTitle { get; set; }
        
        /// <summary>
        /// Duration of this activity in hours.
        /// Calculated based on time block (typically 4 hours per block).
        /// </summary>
        [SaveableProperty(14)]
        public int DurationHours { get; set; }

        public ScheduledBlock()
        {
            Title = string.Empty;
            Description = string.Empty;
            LanceLeaderReasoning = string.Empty;
            ActivityId = string.Empty;
            ActivityTitle = string.Empty;
            IsCompleted = false;
            IsActive = false;
            DurationHours = 4; // Default: 4 hours per block
        }

        /// <summary>
        /// Create a schedule block with all required parameters.
        /// </summary>
        public ScheduledBlock(TimeBlock timeBlock, ScheduleBlockType blockType, string title, string description,
            int fatigueCost, int xpReward, float eventChance, CampaignTime scheduledTime)
        {
            TimeBlock = timeBlock;
            BlockType = blockType;
            Title = title ?? string.Empty;
            Description = description ?? string.Empty;
            FatigueCost = fatigueCost;
            XPReward = xpReward;
            EventChance = eventChance;
            ScheduledTime = scheduledTime;
            LanceLeaderReasoning = string.Empty;
            IsCompleted = false;
            IsActive = false;
        }

        /// <summary>
        /// Mark this block as completed and apply rewards/consequences.
        /// </summary>
        public void Complete()
        {
            IsCompleted = true;
            IsActive = false;
        }

        /// <summary>
        /// Activate this block (player has entered the time period).
        /// </summary>
        public void Activate()
        {
            IsActive = true;
        }

        /// <summary>
        /// Deactivate this block (time period has ended).
        /// </summary>
        public void Deactivate()
        {
            IsActive = false;
        }
    }
}

