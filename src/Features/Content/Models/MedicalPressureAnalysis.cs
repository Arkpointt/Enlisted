namespace Enlisted.Features.Content.Models
{
    /// <summary>
    /// Medical pressure level enumeration for orchestrator integration.
    /// Used to determine when to surface medical opportunities.
    /// </summary>
    public enum MedicalPressureLevel
    {
        /// <summary>No medical concerns.</summary>
        None = 0,

        /// <summary>Low pressure - minor concerns.</summary>
        Low = 1,

        /// <summary>Moderate pressure - attention needed.</summary>
        Moderate = 2,

        /// <summary>High pressure - urgent attention needed.</summary>
        High = 3,

        /// <summary>Critical pressure - emergency intervention needed.</summary>
        Critical = 4
    }

    /// <summary>
    /// Analysis result for medical pressure calculation.
    /// Tracks player condition state and pressure sources.
    /// </summary>
    public class MedicalPressureAnalysis
    {
        /// <summary>Whether player currently has any medical condition.</summary>
        public bool HasCondition { get; set; }

        /// <summary>Whether player has a severe condition requiring immediate care.</summary>
        public bool HasSevereCondition { get; set; }

        /// <summary>Current medical risk level from escalation system (0-5).</summary>
        public int MedicalRisk { get; set; }

        /// <summary>Current HP percentage (0-100).</summary>
        public float HealthPercent { get; set; }

        /// <summary>Days since last treatment or medical activity.</summary>
        public int DaysSinceLastTreatment { get; set; }

        /// <summary>Alias for DaysSinceLastTreatment for API compatibility.</summary>
        public int DaysUntreated => DaysSinceLastTreatment;

        /// <summary>Whether the condition is untreated (has condition but no recent treatment).</summary>
        public bool IsUntreated => HasCondition && DaysSinceLastTreatment > 2;

        /// <summary>Calculated pressure level based on all factors.</summary>
        public MedicalPressureLevel PressureLevel
        {
            get
            {
                if (HasSevereCondition || MedicalRisk >= 4)
                {
                    return MedicalPressureLevel.Critical;
                }

                if (HasCondition || MedicalRisk >= 3 || HealthPercent < 40)
                {
                    return MedicalPressureLevel.High;
                }

                if (MedicalRisk >= 2 || HealthPercent < 60)
                {
                    return MedicalPressureLevel.Moderate;
                }

                if (MedicalRisk >= 1 || HealthPercent < 80)
                {
                    return MedicalPressureLevel.Low;
                }

                return MedicalPressureLevel.None;
            }
        }
    }
}
