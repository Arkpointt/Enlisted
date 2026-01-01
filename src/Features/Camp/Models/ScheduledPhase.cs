using Enlisted.Features.Content;
using Enlisted.Features.Content.Models;

namespace Enlisted.Features.Camp.Models
{
    /// <summary>
    /// Represents the scheduled activities for a single day phase in the camp routine.
    /// Each phase has two activity slots that can be active, skipped, or boosted based on
    /// world state and company conditions.
    /// </summary>
    public class ScheduledPhase
    {
        /// <summary>
        /// The day phase this schedule applies to (Dawn, Midday, Dusk, Night).
        /// </summary>
        public DayPhase Phase { get; set; }

        /// <summary>
        /// Primary activity category for this phase (e.g., "training", "social").
        /// </summary>
        public string Slot1Category { get; set; }

        /// <summary>
        /// Human-readable description of the primary activity.
        /// </summary>
        public string Slot1Description { get; set; }

        /// <summary>
        /// Weight multiplier for primary activity. Higher values increase likelihood of
        /// matching opportunities appearing. Default is 1.0.
        /// </summary>
        public float Slot1Weight { get; set; } = 1.0f;

        /// <summary>
        /// True if the primary activity is skipped due to conditions (low morale, siege, etc.).
        /// </summary>
        public bool Slot1Skipped { get; set; }

        /// <summary>
        /// Secondary activity category for this phase.
        /// </summary>
        public string Slot2Category { get; set; }

        /// <summary>
        /// Human-readable description of the secondary activity.
        /// </summary>
        public string Slot2Description { get; set; }

        /// <summary>
        /// Weight multiplier for secondary activity.
        /// </summary>
        public float Slot2Weight { get; set; } = 1.0f;

        /// <summary>
        /// True if the secondary activity is skipped due to conditions.
        /// </summary>
        public bool Slot2Skipped { get; set; }

        /// <summary>
        /// True if either slot has been modified from baseline.
        /// </summary>
        public bool HasDeviation => Slot1Skipped || Slot2Skipped || !string.IsNullOrEmpty(DeviationReason);

        /// <summary>
        /// Reason for schedule deviation, if any. Used for UI display and logging.
        /// </summary>
        public string DeviationReason { get; set; }

        /// <summary>
        /// True if the player has committed to an activity during this phase.
        /// Player commitments take precedence over scheduled activities.
        /// </summary>
        public bool HasPlayerCommitment { get; set; }

        /// <summary>
        /// Title of the player's committed activity, if any.
        /// </summary>
        public string PlayerCommitmentTitle { get; set; }

        /// <summary>
        /// Flavor text describing the atmosphere during this phase.
        /// </summary>
        public string FlavorText { get; set; }

        /// <summary>
        /// Orchestrator override that created this schedule deviation, if any.
        /// Used by CampRoutineProcessor to apply override-specific processing.
        /// </summary>
        public OrchestratorOverride OrchestratorOverride { get; set; }

        /// <summary>
        /// Creates a copy of this schedule with independent property values.
        /// </summary>
        public ScheduledPhase Clone()
        {
            return new ScheduledPhase
            {
                Phase = Phase,
                Slot1Category = Slot1Category,
                Slot1Description = Slot1Description,
                Slot1Weight = Slot1Weight,
                Slot1Skipped = Slot1Skipped,
                Slot2Category = Slot2Category,
                Slot2Description = Slot2Description,
                Slot2Weight = Slot2Weight,
                Slot2Skipped = Slot2Skipped,
                DeviationReason = DeviationReason,
                HasPlayerCommitment = HasPlayerCommitment,
                PlayerCommitmentTitle = PlayerCommitmentTitle,
                FlavorText = FlavorText,
                OrchestratorOverride = OrchestratorOverride
            };
        }
    }
}
