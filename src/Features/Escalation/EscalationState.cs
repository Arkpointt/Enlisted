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

        // Event system cooldown tracking (key = event id, value = last fired time).
        // Used by EventSelector to prevent the same event from firing within its cooldown period.
        public Dictionary<string, CampaignTime> EventLastFired { get; set; } =
            new Dictionary<string, CampaignTime>(StringComparer.OrdinalIgnoreCase);

        // One-time events that have already fired (stored by event id).
        // These events cannot fire again for the rest of the playthrough.
        public HashSet<string> OneTimeEventsFired { get; set; } =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Pacing system: timestamp of the last narrative event delivered.
        public CampaignTime LastNarrativeEventTime { get; set; } = CampaignTime.Zero;

        // Pacing system: next window when an event can fire (set to random 3-5 days after last event).
        public CampaignTime NextNarrativeEventWindow { get; set; } = CampaignTime.Zero;

        /// <summary>
        /// Checks if a specific event is on cooldown based on its last fired time and cooldown duration.
        /// Returns true if the event should not fire yet (still in cooldown).
        /// </summary>
        public bool IsEventOnCooldown(string eventId, int cooldownDays)
        {
            if (string.IsNullOrEmpty(eventId) || cooldownDays <= 0)
            {
                return false;
            }

            EventLastFired ??= new Dictionary<string, CampaignTime>(StringComparer.OrdinalIgnoreCase);

            if (!EventLastFired.TryGetValue(eventId, out var lastFired))
            {
                return false; // Never fired before, not on cooldown
            }

            var daysSinceLastFired = (CampaignTime.Now - lastFired).ToDays;
            return daysSinceLastFired < cooldownDays;
        }

        /// <summary>
        /// Records that an event was just fired, updating its last-fired timestamp.
        /// </summary>
        public void RecordEventFired(string eventId)
        {
            if (string.IsNullOrEmpty(eventId))
            {
                return;
            }

            EventLastFired ??= new Dictionary<string, CampaignTime>(StringComparer.OrdinalIgnoreCase);
            EventLastFired[eventId] = CampaignTime.Now;
        }

        /// <summary>
        /// Marks a one-time event as fired so it won't fire again this playthrough.
        /// </summary>
        public void RecordOneTimeEventFired(string eventId)
        {
            if (string.IsNullOrEmpty(eventId))
            {
                return;
            }

            OneTimeEventsFired ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            OneTimeEventsFired.Add(eventId);
        }

        /// <summary>
        /// Checks if a one-time event has already fired.
        /// </summary>
        public bool HasOneTimeEventFired(string eventId)
        {
            if (string.IsNullOrEmpty(eventId))
            {
                return false;
            }

            OneTimeEventsFired ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            return OneTimeEventsFired.Contains(eventId);
        }

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
            EventLastFired ??= new Dictionary<string, CampaignTime>(StringComparer.OrdinalIgnoreCase);
            OneTimeEventsFired ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
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


