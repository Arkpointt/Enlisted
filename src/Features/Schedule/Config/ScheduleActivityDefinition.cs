using System.Collections.Generic;
using Enlisted.Features.Schedule.Models;

namespace Enlisted.Features.Schedule.Config
{
    /// <summary>
    /// Configuration for a single schedule activity type.
    /// Loaded from JSON and used by the AI scheduling engine to generate appropriate duties.
    /// </summary>
    public class ScheduleActivityDefinition
    {
        /// <summary>Unique identifier for this activity</summary>
        public string Id { get; set; }
        
        /// <summary>Localization key for the activity title</summary>
        public string TitleKey { get; set; }
        
        /// <summary>Localization key for the activity description</summary>
        public string DescriptionKey { get; set; }
        
        /// <summary>Display title (fallback if TitleKey not found)</summary>
        public string Title { get; set; }
        
        /// <summary>Display description (fallback if DescriptionKey not found)</summary>
        public string Description { get; set; }
        
        /// <summary>Type of schedule block this represents</summary>
        public ScheduleBlockType BlockType { get; set; }
        
        /// <summary>Fatigue cost (0-24)</summary>
        public int FatigueCost { get; set; }
        
        /// <summary>XP reward for completion</summary>
        public int XPReward { get; set; }
        
        /// <summary>Base event chance (0.0-1.0)</summary>
        public float EventChance { get; set; }
        
        /// <summary>Minimum tier required (1-6)</summary>
        public int MinTier { get; set; }
        
        /// <summary>Maximum tier allowed (1-6, 0 = no limit)</summary>
        public int MaxTier { get; set; }
        
        /// <summary>Whether this activity requires Lance Leader promotion (T5-T6 role)</summary>
        public bool RequiresLanceLeader { get; set; }
        
        /// <summary>Required formations (empty = any)</summary>
        public List<string> RequiredFormations { get; set; }
        
        /// <summary>Required duties for this activity (empty = any duty)</summary>
        public List<string> RequiredDuties { get; set; }
        
        /// <summary>Preferred time blocks for this activity</summary>
        public List<TimeBlock> PreferredTimeBlocks { get; set; }
        
        /// <summary>Lord objectives that favor this activity</summary>
        public List<string> FavoredByObjectives { get; set; }
        
        /// <summary>Lance needs that this activity helps recover</summary>
        public Dictionary<string, int> NeedRecovery { get; set; }
        
        /// <summary>Lance needs that this activity degrades</summary>
        public Dictionary<string, int> NeedDegradation { get; set; }

        public ScheduleActivityDefinition()
        {
            Id = string.Empty;
            TitleKey = string.Empty;
            DescriptionKey = string.Empty;
            Title = string.Empty;
            Description = string.Empty;
            RequiredFormations = new List<string>();
            RequiredDuties = new List<string>();
            PreferredTimeBlocks = new List<TimeBlock>();
            FavoredByObjectives = new List<string>();
            NeedRecovery = new Dictionary<string, int>();
            NeedDegradation = new Dictionary<string, int>();
            MinTier = 1;
            MaxTier = 0; // 0 = no limit
            RequiresLanceLeader = false;
        }
    }
}

