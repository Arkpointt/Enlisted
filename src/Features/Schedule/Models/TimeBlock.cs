namespace Enlisted.Features.Schedule.Models
{
    /// <summary>
    /// Represents the 4 time blocks in a day for schedule assignments.
    /// Simplified from 6 blocks to 4 for clearer player experience.
    /// </summary>
    public enum TimeBlock
    {
        /// <summary>Morning: 6:00-12:00 (Training, patrols, primary duties)</summary>
        Morning = 0,
        
        /// <summary>Afternoon: 12:00-18:00 (Work details, secondary duties)</summary>
        Afternoon = 1,
        
        /// <summary>Dusk: 18:00-22:00 (Free time, social activities)</summary>
        Dusk = 2,
        
        /// <summary>Night: 22:00-6:00 (Rest, watch rotation for some)</summary>
        Night = 3
    }
}
