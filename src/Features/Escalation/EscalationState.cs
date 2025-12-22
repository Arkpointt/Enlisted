using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;

namespace Enlisted.Features.Escalation
{
    /// <summary>
    /// Persisted escalation tracks (Phase 4).
    ///
    /// Ranges (per docs/research/escalation_system.md and master plan v2.0):
    /// - Scrutiny: 0–10
    /// - Discipline: 0–10
    /// - Soldier reputation: -50..+50 (renamed from LanceReputation)
    /// - Lord reputation: 0–100 (NEW)
    /// - Officer reputation: 0–100 (NEW)
    /// - Medical risk: 0–5
    ///
    /// This class is intentionally "dumb storage". All rules live in EscalationManager.
    /// </summary>
    [Serializable]
    public sealed class EscalationState
    {
        public const int ScrutinyMin = 0;
        public const int ScrutinyMax = 10;
        public const int DisciplineMin = 0;
        public const int DisciplineMax = 10;
        public const int SoldierReputationMin = -50;
        public const int SoldierReputationMax = 50;
        public const int LordReputationMin = 0;
        public const int LordReputationMax = 100;
        public const int OfficerReputationMin = 0;
        public const int OfficerReputationMax = 100;
        public const int MedicalRiskMin = 0;
        public const int MedicalRiskMax = 5;

        // Track values
        public int Scrutiny { get; set; }
        public int Discipline { get; set; }
        public int SoldierReputation { get; set; }
        public int LordReputation { get; set; }
        public int OfficerReputation { get; set; }
        public int MedicalRisk { get; set; }

        // Timestamps used for passive decay rules.
        // We store "last raised time" for tracks where decay requires a quiet period.
        public CampaignTime LastScrutinyRaisedTime { get; set; } = CampaignTime.Zero;
        public CampaignTime LastScrutinyDecayTime { get; set; } = CampaignTime.Zero;

        public CampaignTime LastDisciplineRaisedTime { get; set; } = CampaignTime.Zero;
        public CampaignTime LastDisciplineDecayTime { get; set; } = CampaignTime.Zero;

        public CampaignTime LastSoldierReputationDecayTime { get; set; } = CampaignTime.Zero;

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
            Scrutiny = Clamp(Scrutiny, ScrutinyMin, ScrutinyMax);
            Discipline = Clamp(Discipline, DisciplineMin, DisciplineMax);
            SoldierReputation = Clamp(SoldierReputation, SoldierReputationMin, SoldierReputationMax);
            LordReputation = Clamp(LordReputation, LordReputationMin, LordReputationMax);
            OfficerReputation = Clamp(OfficerReputation, OfficerReputationMin, OfficerReputationMax);
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


