namespace Enlisted.Features.Escalation
{
    /// <summary>
    /// Threshold constants for determining "states" and which threshold events become eligible.
    /// These values mirror the Phase 4 research docs and are intentionally centralized.
    /// </summary>
    public static class EscalationThresholds
    {
        // Scrutiny thresholds
        public const int ScrutinyWarning = 3;
        public const int ScrutinyShakedown = 5;
        public const int ScrutinyAudit = 7;
        public const int ScrutinyExposed = 10;

        // Discipline thresholds
        public const int DisciplineExtraDuty = 3;
        public const int DisciplineHearing = 5;
        public const int DisciplineBlocked = 7;
        public const int DisciplineDischarge = 10;

        // Soldier reputation thresholds
        public const int SoldierTrusted = 20;
        public const int SoldierBonded = 40;
        public const int SoldierIsolated = -20;
        public const int SoldierSabotage = -40;

        // Medical risk thresholds
        public const int MedicalWorsening = 3;
        public const int MedicalComplication = 4;
        public const int MedicalEmergency = 5;
    }
}


