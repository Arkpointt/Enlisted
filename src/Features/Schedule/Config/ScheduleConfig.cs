using System.Collections.Generic;

namespace Enlisted.Features.Schedule.Config
{
    /// <summary>
    /// Master configuration for the AI Camp Schedule system.
    /// Loaded from JSON at mod initialization.
    /// </summary>
    public class ScheduleConfig
    {
        /// <summary>List of all available schedule activities</summary>
        public List<ScheduleActivityDefinition> Activities { get; set; }
        
        /// <summary>Lance needs configuration</summary>
        public LanceNeedsConfig LanceNeeds { get; set; }
        
        /// <summary>Length of schedule cycle in days (default: 12)</summary>
        public int CycleLengthDays { get; set; }
        
        /// <summary>Whether to enable the schedule system</summary>
        public bool EnableSchedule { get; set; }
        
        /// <summary>Whether to show schedule notifications</summary>
        public bool ShowNotifications { get; set; }
        
        /// <summary>Whether to allow player to skip duties (with consequences)</summary>
        public bool AllowSkipDuties { get; set; }
        
        /// <summary>Heat penalty for skipping a duty</summary>
        public int SkipDutyHeatPenalty { get; set; }
        
        /// <summary>Reputation penalty for skipping a duty</summary>
        public int SkipDutyRepPenalty { get; set; }

        public ScheduleConfig()
        {
            Activities = new List<ScheduleActivityDefinition>();
            LanceNeeds = new LanceNeedsConfig();
            CycleLengthDays = 12;
            EnableSchedule = true;
            ShowNotifications = true;
            AllowSkipDuties = true;
            SkipDutyHeatPenalty = 10;
            SkipDutyRepPenalty = 5;
        }
    }
}

