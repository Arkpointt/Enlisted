using Enlisted.Features.Content.Models;

namespace Enlisted.Features.Camp.Models
{
    /// <summary>
    /// A forecast entry for UI display, showing what's scheduled for a future day phase.
    /// Used in the schedule panel to let players plan their day.
    /// </summary>
    public class ScheduleForecast
    {
        /// <summary>
        /// The day phase this forecast is for.
        /// </summary>
        public DayPhase Phase { get; set; }

        /// <summary>
        /// Human-readable phase name (Dawn, Midday, Dusk, Night).
        /// </summary>
        public string PhaseName => Phase.ToString();

        /// <summary>
        /// Primary scheduled activity description, or null if skipped.
        /// </summary>
        public string PrimaryActivity { get; set; }

        /// <summary>
        /// Secondary scheduled activity description, or null if skipped.
        /// </summary>
        public string SecondaryActivity { get; set; }

        /// <summary>
        /// True if the schedule has deviated from baseline for this phase.
        /// </summary>
        public bool IsDeviation { get; set; }

        /// <summary>
        /// Reason for deviation, if any.
        /// </summary>
        public string DeviationReason { get; set; }

        /// <summary>
        /// Title of the player's committed activity, if any.
        /// </summary>
        public string PlayerCommitment { get; set; }

        /// <summary>
        /// True if the player has committed to an activity this phase.
        /// </summary>
        public bool HasPlayerCommitment => !string.IsNullOrEmpty(PlayerCommitment);

        /// <summary>
        /// True if this is the current phase (for UI highlighting).
        /// </summary>
        public bool IsCurrent { get; set; }

        /// <summary>
        /// True if this phase has already passed today (for UI dimming).
        /// </summary>
        public bool HasPassed { get; set; }

        /// <summary>
        /// Flavor text for this phase (atmosphere description).
        /// </summary>
        public string FlavorText { get; set; }
    }
}
