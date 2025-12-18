namespace Enlisted.Features.Escalation
{
    /// <summary>
    /// Threshold constants for determining "states" and which threshold events become eligible.
    /// These values mirror the Phase 4 research docs and are intentionally centralized.
    /// </summary>
    public static class EscalationThresholds
    {
        // Heat thresholds
        public const int HeatWarning = 3;
        public const int HeatShakedown = 5;
        public const int HeatAudit = 7;
        public const int HeatExposed = 10;

        // Discipline thresholds
        public const int DisciplineExtraDuty = 3;
        public const int DisciplineHearing = 5;
        public const int DisciplineBlocked = 7;
        public const int DisciplineDischarge = 10;

        // Lance reputation thresholds
        public const int LanceTrusted = 20;
        public const int LanceBonded = 40;
        public const int LanceIsolated = -20;
        public const int LanceSabotage = -40;

        // Medical risk thresholds
        public const int MedicalWorsening = 3;
        public const int MedicalComplication = 4;
        public const int MedicalEmergency = 5;
    }
}


