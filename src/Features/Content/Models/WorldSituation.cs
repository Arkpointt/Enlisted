using TaleWorlds.CampaignSystem.Settlements;

namespace Enlisted.Features.Content.Models
{
    /// <summary>
    /// Snapshot of current world state for content selection.
    /// Built by WorldStateAnalyzer on each daily tick.
    /// </summary>
    public class WorldSituation
    {
        /// <summary>What the enlisted lord is doing.</summary>
        public LordSituation LordIs { get; set; }

        /// <summary>Kingdom's war posture.</summary>
        public WarStance KingdomStance { get; set; }

        /// <summary>Overall military life phase.</summary>
        public LifePhase CurrentPhase { get; set; }

        /// <summary>Expected event density.</summary>
        public ActivityLevel ExpectedActivity { get; set; }

        /// <summary>Events per week (base).</summary>
        public float RealisticEventFrequency { get; set; }

        /// <summary>Dawn, Midday, Dusk, Night.</summary>
        public DayPhase CurrentDayPhase { get; set; }

        /// <summary>0-23 for precise timing.</summary>
        public int CurrentHour { get; set; }

        /// <summary>If garrisoned.</summary>
        public Settlement CurrentSettlement { get; set; }

        /// <summary>If marching/sieging.</summary>
        public Settlement TargetSettlement { get; set; }

        /// <summary>How long in this phase.</summary>
        public int DaysInCurrentPhase { get; set; }

        /// <summary>Affects pressure.</summary>
        public bool InEnemyTerritory { get; set; }
    }
}
