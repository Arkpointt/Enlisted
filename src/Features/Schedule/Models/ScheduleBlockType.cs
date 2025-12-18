namespace Enlisted.Features.Schedule.Models
{
    /// <summary>
    /// Types of schedule blocks that can be assigned to soldiers.
    /// Each type has different requirements, rewards, and event pools.
    /// </summary>
    public enum ScheduleBlockType
    {
        /// <summary>Rest and recovery (Night, reduces fatigue)</summary>
        Rest,
        
        /// <summary>Free time - player chooses activity (Evening/Dusk)</summary>
        FreeTime,
        
        /// <summary>Guard post duty (Any time, low fatigue)</summary>
        SentryDuty,
        
        /// <summary>Patrol perimeter (Morning/Afternoon, medium fatigue)</summary>
        PatrolDuty,
        
        /// <summary>Scout enemy positions (Morning/Afternoon, high fatigue, T3+)</summary>
        ScoutingMission,
        
        /// <summary>Gather supplies and hunt (Morning/Afternoon, medium fatigue)</summary>
        ForagingDuty,
        
        /// <summary>Combat drills and training (Morning, medium fatigue)</summary>
        TrainingDrill,
        
        /// <summary>Maintenance and construction (Afternoon, low fatigue)</summary>
        WorkDetail,
        
        /// <summary>Night watch rotation (Night, low fatigue)</summary>
        WatchDuty,
        
        /// <summary>Disciplinary work assignment (Any time, high fatigue)</summary>
        PunishmentDetail,
        
        /// <summary>Special assignment from lance leader (Any time, varies)</summary>
        SpecialAssignment
    }
}

