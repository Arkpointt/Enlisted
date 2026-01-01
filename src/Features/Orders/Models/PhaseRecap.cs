using Enlisted.Features.Content.Models;
using TaleWorlds.CampaignSystem;

namespace Enlisted.Features.Orders.Models
{
    /// <summary>
    /// Represents a summary of what happened during a single duty phase.
    /// Used to build the duty log shown in player status.
    /// </summary>
    public class PhaseRecap
    {
        /// <summary>
        /// Which phase of the day this recap represents (Dawn, Midday, Dusk, Night).
        /// </summary>
        public DayPhase Phase { get; set; }

        /// <summary>
        /// When this phase occurred.
        /// </summary>
        public CampaignTime PhaseTime { get; set; }

        /// <summary>
        /// Short summary text describing what happened during this phase.
        /// Examples: "Routine watch. Nothing to report.", "Spotted tracks near perimeter."
        /// </summary>
        public string RecapText { get; set; }

        /// <summary>
        /// Whether an event fired during this phase.
        /// </summary>
        public bool EventFired { get; set; }

        /// <summary>
        /// Which day of the order this phase belongs to (1-based).
        /// </summary>
        public int OrderDay { get; set; }

        /// <summary>
        /// Which phase number within the order (1-based, for multi-day orders).
        /// </summary>
        public int PhaseNumber { get; set; }

        /// <summary>
        /// Order ID this recap belongs to.
        /// </summary>
        public string OrderId { get; set; }
    }
}
