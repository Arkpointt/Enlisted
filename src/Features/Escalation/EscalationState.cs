using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;

namespace Enlisted.Features.Escalation
{
    /// <summary>
    /// Persisted escalation tracks (Phase 4).
    ///
    /// Ranges (per docs/research/escalation_system.md):
    /// - Heat: 0–10
    /// - Discipline: 0–10
    /// - Lance reputation: -50..+50
    /// - Medical risk: 0–5
    ///
    /// This class is intentionally "dumb storage". All rules live in EscalationManager.
    /// </summary>
    [Serializable]
    public sealed class EscalationState
    {
        public const int HeatMin = 0;
        public const int HeatMax = 10;
        public const int DisciplineMin = 0;
        public const int DisciplineMax = 10;
        public const int LanceReputationMin = -50;
        public const int LanceReputationMax = 50;
        public const int MedicalRiskMin = 0;
        public const int MedicalRiskMax = 5;

        // Track values
        public int Heat { get; set; }
        public int Discipline { get; set; }
        public int LanceReputation { get; set; }
        public int MedicalRisk { get; set; }

        // Timestamps used for passive decay rules.
        // We store "last raised time" for tracks where decay requires a quiet period.
        public CampaignTime LastHeatRaisedTime { get; set; } = CampaignTime.Zero;
        public CampaignTime LastHeatDecayTime { get; set; } = CampaignTime.Zero;

        public CampaignTime LastDisciplineRaisedTime { get; set; } = CampaignTime.Zero;
        public CampaignTime LastDisciplineDecayTime { get; set; } = CampaignTime.Zero;

        public CampaignTime LastLanceReputationDecayTime { get; set; } = CampaignTime.Zero;

        // Medical risk is special: it should reset when treated and decay only when resting.
        public CampaignTime LastMedicalRiskDecayTime { get; set; } = CampaignTime.Zero;

        // Global cooldown gate for threshold events (Phase 4+).
        public CampaignTime LastThresholdEventTime { get; set; } = CampaignTime.Zero;

        // Pending threshold event story id (consumed by LanceStoryBehavior).
        public string PendingThresholdStoryId { get; set; } = string.Empty;

        // Per-threshold cooldown tracking (key = threshold story id, value = last fired time).
        public Dictionary<string, CampaignTime> ThresholdStoryLastFired { get; set; } =
            new Dictionary<string, CampaignTime>(StringComparer.OrdinalIgnoreCase);

        public void ClampAll()
        {
            Heat = Clamp(Heat, HeatMin, HeatMax);
            Discipline = Clamp(Discipline, DisciplineMin, DisciplineMax);
            LanceReputation = Clamp(LanceReputation, LanceReputationMin, LanceReputationMax);
            MedicalRisk = Clamp(MedicalRisk, MedicalRiskMin, MedicalRiskMax);

            PendingThresholdStoryId ??= string.Empty;
            ThresholdStoryLastFired ??= new Dictionary<string, CampaignTime>(StringComparer.OrdinalIgnoreCase);
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }
            return value > max ? max : value;
        }
    }
}


