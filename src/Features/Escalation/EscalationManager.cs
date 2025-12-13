using System;
using System.Linq;
using Enlisted.Features.Assignments.Core;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;
using TaleWorlds.CampaignSystem;

namespace Enlisted.Features.Escalation
{
    /// <summary>
    /// Phase 4 escalation manager.
    ///
    /// Responsibilities:
    /// - Owns the persisted EscalationState (save/load via CampaignBehavior SyncData)
    /// - Provides track modification APIs (Heat/Discipline/LanceRep/MedicalRisk)
    /// - Provides readable "state" descriptions for UI ("Watched", "Hot", "Trusted", etc.)
    /// - Provides passive decay logic (integration into daily tick is a later step)
    ///
    /// Important constraints:
    /// - No instant hard fails: this manager never forces game-over; it only tracks and exposes state.
    /// - Internal-only: does not touch vanilla crime/reputation systems.
    /// </summary>
    public sealed class EscalationManager : CampaignBehaviorBase
    {
        private const string LogCategory = "Escalation";

        public static EscalationManager Instance { get; private set; }

        private readonly EscalationState _state = new EscalationState();
        private int _lastDailyTickDayNumber = -1;

        public EscalationState State => _state;

        public EscalationManager()
        {
            Instance = this;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        private void OnDailyTick()
        {
            try
            {
                if (!IsEnabled())
                {
                    return;
                }

                // Only active while enlisted
                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment?.IsEnlisted != true)
                {
                    return;
                }

                // DailyTickEvent should be daily, but keep a stable guard anyway.
                var dayNumber = (int)CampaignTime.Now.ToDays;
                if (dayNumber == _lastDailyTickDayNumber)
                {
                    return;
                }
                _lastDailyTickDayNumber = dayNumber;

                ApplyPassiveDecay();
                EvaluateThresholdsAndQueueIfNeeded();
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Escalation daily tick failed", ex);
            }
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Track values
            var heat = _state.Heat;
            var discipline = _state.Discipline;
            var rep = _state.LanceReputation;
            var medical = _state.MedicalRisk;

            dataStore.SyncData("esc_heat", ref heat);
            dataStore.SyncData("esc_discipline", ref discipline);
            dataStore.SyncData("esc_rep", ref rep);
            dataStore.SyncData("esc_medical", ref medical);

            // Timestamps
            var lastHeatRaised = _state.LastHeatRaisedTime;
            var lastHeatDecay = _state.LastHeatDecayTime;
            var lastDiscRaised = _state.LastDisciplineRaisedTime;
            var lastDiscDecay = _state.LastDisciplineDecayTime;
            var lastRepDecay = _state.LastLanceReputationDecayTime;
            var lastMedicalDecay = _state.LastMedicalRiskDecayTime;
            var lastThresholdEvent = _state.LastThresholdEventTime;

            dataStore.SyncData("esc_lastHeatRaised", ref lastHeatRaised);
            dataStore.SyncData("esc_lastHeatDecay", ref lastHeatDecay);
            dataStore.SyncData("esc_lastDiscRaised", ref lastDiscRaised);
            dataStore.SyncData("esc_lastDiscDecay", ref lastDiscDecay);
            dataStore.SyncData("esc_lastRepDecay", ref lastRepDecay);
            dataStore.SyncData("esc_lastMedicalDecay", ref lastMedicalDecay);
            dataStore.SyncData("esc_lastThresholdEvent", ref lastThresholdEvent);

            // Pending threshold event
            var pendingThreshold = _state.PendingThresholdStoryId ?? string.Empty;
            dataStore.SyncData("esc_pendingThreshold", ref pendingThreshold);

            // Per-threshold cooldown map
            var thresholdKeys = (_state.ThresholdStoryLastFired ?? Enumerable.Empty<System.Collections.Generic.KeyValuePair<string, CampaignTime>>())
                .Select(k => k.Key)
                .ToList();
            var thresholdCount = thresholdKeys.Count;
            dataStore.SyncData("esc_thresholdCount", ref thresholdCount);

            if (dataStore.IsLoading)
            {
                _state.Heat = heat;
                _state.Discipline = discipline;
                _state.LanceReputation = rep;
                _state.MedicalRisk = medical;

                _state.LastHeatRaisedTime = lastHeatRaised;
                _state.LastHeatDecayTime = lastHeatDecay;
                _state.LastDisciplineRaisedTime = lastDiscRaised;
                _state.LastDisciplineDecayTime = lastDiscDecay;
                _state.LastLanceReputationDecayTime = lastRepDecay;
                _state.LastMedicalRiskDecayTime = lastMedicalDecay;
                _state.LastThresholdEventTime = lastThresholdEvent;

                _state.PendingThresholdStoryId = pendingThreshold;
                _state.ThresholdStoryLastFired = new System.Collections.Generic.Dictionary<string, CampaignTime>(StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < thresholdCount; i++)
                {
                    var key = string.Empty;
                    var time = CampaignTime.Zero;
                    dataStore.SyncData($"esc_threshold_{i}_id", ref key);
                    dataStore.SyncData($"esc_threshold_{i}_time", ref time);
                    if (!string.IsNullOrWhiteSpace(key))
                    {
                        _state.ThresholdStoryLastFired[key] = time;
                    }
                }

                _state.ClampAll();
            }
            else
            {
                // Store in a stable order to reduce churn
                thresholdKeys.Sort(StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < thresholdKeys.Count; i++)
                {
                    var key = thresholdKeys[i];
                    var time = _state.ThresholdStoryLastFired.TryGetValue(key, out var t) ? t : CampaignTime.Zero;
                    dataStore.SyncData($"esc_threshold_{i}_id", ref key);
                    dataStore.SyncData($"esc_threshold_{i}_time", ref time);
                }
            }
        }

        public bool IsEnabled()
        {
            // Feature-flag requirement: can be disabled without removing code.
            return ConfigurationManager.LoadEscalationConfig()?.Enabled == true;
        }

        public string PendingThresholdStoryId => _state.PendingThresholdStoryId ?? string.Empty;

        public void ClearPendingThresholdStory()
        {
            _state.PendingThresholdStoryId = string.Empty;
        }

        public void MarkThresholdStoryFired(string storyId)
        {
            if (string.IsNullOrWhiteSpace(storyId))
            {
                _state.PendingThresholdStoryId = string.Empty;
                return;
            }

            _state.LastThresholdEventTime = CampaignTime.Now;
            _state.ThresholdStoryLastFired ??= new System.Collections.Generic.Dictionary<string, CampaignTime>(StringComparer.OrdinalIgnoreCase);
            _state.ThresholdStoryLastFired[storyId] = CampaignTime.Now;
            _state.PendingThresholdStoryId = string.Empty;
        }

        public bool IsThresholdStoryOnCooldown(string storyId, int cooldownDays)
        {
            if (string.IsNullOrWhiteSpace(storyId))
            {
                return true;
            }

            if (cooldownDays <= 0)
            {
                return false;
            }

            if (_state.ThresholdStoryLastFired == null || !_state.ThresholdStoryLastFired.TryGetValue(storyId, out var last))
            {
                return false;
            }

            var next = last + CampaignTime.Days(cooldownDays);
            return next.IsFuture;
        }

        public void EvaluateThresholdsAndQueueIfNeeded()
        {
            if (!IsEnabled())
            {
                return;
            }

            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment?.IsEnlisted != true)
            {
                _state.PendingThresholdStoryId = string.Empty;
                return;
            }

            var cfg = ConfigurationManager.LoadEscalationConfig() ?? new EscalationConfig();
            var cooldownDays = Math.Max(0, cfg.ThresholdEventCooldownDays);

            // Only one threshold event per day.
            var lastDay = (int)_state.LastThresholdEventTime.ToDays;
            var today = (int)CampaignTime.Now.ToDays;
            if (_state.LastThresholdEventTime != CampaignTime.Zero && lastDay == today)
            {
                return;
            }

            var candidate = PickBestThresholdCandidateId(cooldownDays);
            if (string.IsNullOrWhiteSpace(candidate))
            {
                _state.PendingThresholdStoryId = string.Empty;
                return;
            }

            // If the pending event no longer matches what we'd fire now, replace it.
            _state.PendingThresholdStoryId = candidate;
        }

        private string PickBestThresholdCandidateId(int cooldownDays)
        {
            // Priority is deterministic and "highest threshold wins" per track.
            // Order across tracks: Heat, Discipline, Medical, Lance Reputation.
            var heatCandidates = new[]
            {
                (_state.Heat >= EscalationThresholds.HeatExposed, "heat_exposed"),
                (_state.Heat >= EscalationThresholds.HeatAudit, "heat_audit"),
                (_state.Heat >= EscalationThresholds.HeatShakedown, "heat_shakedown"),
                (_state.Heat >= EscalationThresholds.HeatWarning, "heat_warning")
            };
            var disciplineCandidates = new[]
            {
                (_state.Discipline >= EscalationThresholds.DisciplineDischarge, "discipline_discharge"),
                (_state.Discipline >= EscalationThresholds.DisciplineBlocked, "discipline_blocked"),
                (_state.Discipline >= EscalationThresholds.DisciplineHearing, "discipline_hearing"),
                (_state.Discipline >= EscalationThresholds.DisciplineExtraDuty, "discipline_extra_duty")
            };
            var medicalCandidates = new[]
            {
                (_state.MedicalRisk >= EscalationThresholds.MedicalEmergency, "medical_emergency"),
                (_state.MedicalRisk >= EscalationThresholds.MedicalComplication, "medical_complication"),
                (_state.MedicalRisk >= EscalationThresholds.MedicalWorsening, "medical_worsening")
            };
            var repCandidates = new[]
            {
                (_state.LanceReputation <= EscalationThresholds.LanceSabotage, "lance_sabotage"),
                (_state.LanceReputation <= EscalationThresholds.LanceIsolated, "lance_isolated"),
                (_state.LanceReputation >= EscalationThresholds.LanceBonded, "lance_bonded"),
                (_state.LanceReputation >= EscalationThresholds.LanceTrusted, "lance_trusted")
            };

            foreach (var (ok, id) in heatCandidates)
            {
                if (ok && !IsThresholdStoryOnCooldown(id, cooldownDays))
                {
                    return id;
                }
            }
            foreach (var (ok, id) in disciplineCandidates)
            {
                if (ok && !IsThresholdStoryOnCooldown(id, cooldownDays))
                {
                    return id;
                }
            }
            foreach (var (ok, id) in medicalCandidates)
            {
                if (ok && !IsThresholdStoryOnCooldown(id, cooldownDays))
                {
                    return id;
                }
            }
            foreach (var (ok, id) in repCandidates)
            {
                if (ok && !IsThresholdStoryOnCooldown(id, cooldownDays))
                {
                    return id;
                }
            }

            return string.Empty;
        }

        #region Track modification

        public void ModifyHeat(int delta, string reason = null)
        {
            if (!IsEnabled())
            {
                return;
            }

            var oldValue = _state.Heat;
            var next = oldValue + delta;
            _state.Heat = Clamp(next, EscalationState.HeatMin, EscalationState.HeatMax);

            if (delta > 0)
            {
                // Heat decay requires a "quiet period" (no corrupt choices) since the last raise.
                _state.LastHeatRaisedTime = CampaignTime.Now;
            }

            LogTrackChange("Heat", oldValue, _state.Heat, reason);
            EvaluateThresholdsAndQueueIfNeeded();
        }

        public void ModifyDiscipline(int delta, string reason = null)
        {
            if (!IsEnabled())
            {
                return;
            }

            var oldValue = _state.Discipline;
            var next = oldValue + delta;
            _state.Discipline = Clamp(next, EscalationState.DisciplineMin, EscalationState.DisciplineMax);

            if (delta > 0)
            {
                _state.LastDisciplineRaisedTime = CampaignTime.Now;
            }

            LogTrackChange("Discipline", oldValue, _state.Discipline, reason);
            EvaluateThresholdsAndQueueIfNeeded();
        }

        public void ModifyLanceReputation(int delta, string reason = null)
        {
            if (!IsEnabled())
            {
                return;
            }

            var oldValue = _state.LanceReputation;
            var next = oldValue + delta;
            _state.LanceReputation = Clamp(next, EscalationState.LanceReputationMin, EscalationState.LanceReputationMax);
            LogTrackChange("LanceReputation", oldValue, _state.LanceReputation, reason);
            EvaluateThresholdsAndQueueIfNeeded();
        }

        public void ModifyMedicalRisk(int delta, string reason = null)
        {
            if (!IsEnabled())
            {
                return;
            }

            var oldValue = _state.MedicalRisk;
            var next = oldValue + delta;
            _state.MedicalRisk = Clamp(next, EscalationState.MedicalRiskMin, EscalationState.MedicalRiskMax);
            LogTrackChange("MedicalRisk", oldValue, _state.MedicalRisk, reason);
            EvaluateThresholdsAndQueueIfNeeded();
        }

        public void ResetMedicalRisk(string reason = null)
        {
            if (!IsEnabled())
            {
                return;
            }

            var oldValue = _state.MedicalRisk;
            _state.MedicalRisk = 0;
            LogTrackChange("MedicalRisk", oldValue, _state.MedicalRisk, reason ?? "treated");
        }

        #endregion

        #region Passive decay (logic only; integration to daily tick is a later step)

        public void ApplyPassiveDecay()
        {
            if (!IsEnabled())
            {
                return;
            }

            var cfg = ConfigurationManager.LoadEscalationConfig() ?? new EscalationConfig();
            var now = CampaignTime.Now;

            // Heat: -1 per 7 days with no corrupt choices.
            {
                var old = _state.Heat;
                if (TryDecayDown(old, _state.LastHeatDecayTime, _state.LastHeatRaisedTime, cfg.HeatDecayIntervalDays, 1,
                        EscalationState.HeatMin, EscalationState.HeatMax, now, out var updated, out var updatedTime))
                {
                    _state.Heat = updated;
                    _state.LastHeatDecayTime = updatedTime;
                    ModLogger.Debug(LogCategory, $"Heat decayed: {old} -> {updated}");
                }
            }

            // Discipline: -1 per 14 days with no infractions.
            {
                var old = _state.Discipline;
                if (TryDecayDown(old, _state.LastDisciplineDecayTime, _state.LastDisciplineRaisedTime, cfg.DisciplineDecayIntervalDays, 1,
                        EscalationState.DisciplineMin, EscalationState.DisciplineMax, now, out var updated, out var updatedTime))
                {
                    _state.Discipline = updated;
                    _state.LastDisciplineDecayTime = updatedTime;
                    ModLogger.Debug(LogCategory, $"Discipline decayed: {old} -> {updated}");
                }
            }

            // Lance reputation: trends toward 0 by 1 per 14 days.
            {
                var old = _state.LanceReputation;
                if (TryDecayTowardZero(old, _state.LastLanceReputationDecayTime, cfg.LanceReputationDecayIntervalDays, 1,
                        EscalationState.LanceReputationMin, EscalationState.LanceReputationMax, now, out var updated, out var updatedTime))
                {
                    _state.LanceReputation = updated;
                    _state.LastLanceReputationDecayTime = updatedTime;
                    ModLogger.Debug(LogCategory, $"Lance reputation decayed: {old} -> {updated}");
                }
            }
        }

        public void ApplyMedicalRestDecay(bool isResting)
        {
            if (!IsEnabled())
            {
                return;
            }

            if (!isResting)
            {
                return;
            }

            var cfg = ConfigurationManager.LoadEscalationConfig() ?? new EscalationConfig();
            var now = CampaignTime.Now;

            // Medical risk: -1 per day of rest. (But "does not decay while condition persists untreated" is handled by caller.)
            var old = _state.MedicalRisk;
            if (TryDecayDownNoQuietRequirement(old, _state.LastMedicalRiskDecayTime, cfg.MedicalRiskDecayIntervalDays, 1,
                    EscalationState.MedicalRiskMin, EscalationState.MedicalRiskMax, now, out var updated, out var updatedTime))
            {
                _state.MedicalRisk = updated;
                _state.LastMedicalRiskDecayTime = updatedTime;
                ModLogger.Debug(LogCategory, $"Medical risk decayed: {old} -> {updated}");
            }
        }

        private static bool TryDecayDown(
            int value,
            CampaignTime lastDecayTime,
            CampaignTime lastRaisedTime,
            int intervalDays,
            int amount,
            int min,
            int max,
            CampaignTime now,
            out int updatedValue,
            out CampaignTime updatedLastDecayTime)
        {
            updatedValue = value;
            updatedLastDecayTime = lastDecayTime;

            if (value <= min)
            {
                return false;
            }

            if (intervalDays <= 0)
            {
                return false;
            }

            // Require a quiet period since last raise.
            if (lastRaisedTime != CampaignTime.Zero)
            {
                var quietDays = now.ToDays - lastRaisedTime.ToDays;
                if (quietDays < intervalDays)
                {
                    return false;
                }
            }

            var sinceLastDecay = lastDecayTime == CampaignTime.Zero ? float.MaxValue : (now.ToDays - lastDecayTime.ToDays);
            if (sinceLastDecay < intervalDays)
            {
                return false;
            }

            updatedValue = Clamp(value - amount, min, max);
            updatedLastDecayTime = now;
            return updatedValue != value;
        }

        private static bool TryDecayDownNoQuietRequirement(
            int value,
            CampaignTime lastDecayTime,
            int intervalDays,
            int amount,
            int min,
            int max,
            CampaignTime now,
            out int updatedValue,
            out CampaignTime updatedLastDecayTime)
        {
            updatedValue = value;
            updatedLastDecayTime = lastDecayTime;

            if (value <= min)
            {
                return false;
            }

            if (intervalDays <= 0)
            {
                return false;
            }

            var sinceLastDecay = lastDecayTime == CampaignTime.Zero ? float.MaxValue : (now.ToDays - lastDecayTime.ToDays);
            if (sinceLastDecay < intervalDays)
            {
                return false;
            }

            updatedValue = Clamp(value - amount, min, max);
            updatedLastDecayTime = now;
            return updatedValue != value;
        }

        private static bool TryDecayTowardZero(
            int value,
            CampaignTime lastDecayTime,
            int intervalDays,
            int amount,
            int min,
            int max,
            CampaignTime now,
            out int updatedValue,
            out CampaignTime updatedLastDecayTime)
        {
            updatedValue = value;
            updatedLastDecayTime = lastDecayTime;

            if (value == 0)
            {
                return false;
            }

            if (intervalDays <= 0)
            {
                return false;
            }

            var sinceLastDecay = lastDecayTime == CampaignTime.Zero ? float.MaxValue : (now.ToDays - lastDecayTime.ToDays);
            if (sinceLastDecay < intervalDays)
            {
                return false;
            }

            if (value > 0)
            {
                updatedValue = Clamp(value - amount, min, max);
            }
            else
            {
                updatedValue = Clamp(value + amount, min, max);
            }

            updatedLastDecayTime = now;
            return updatedValue != value;
        }

        #endregion

        #region Readable status labels (for UI)

        public string GetHeatStatus()
        {
            var heat = _state.Heat;
            if (heat <= 0)
            {
                return "Clean";
            }
            if (heat <= 2)
            {
                return "Watched";
            }
            if (heat <= 4)
            {
                return "Noticed";
            }
            if (heat <= 6)
            {
                return "Hot";
            }
            if (heat <= 9)
            {
                return "Burning";
            }
            return "Exposed";
        }

        public string GetDisciplineStatus()
        {
            var d = _state.Discipline;
            if (d <= 0)
            {
                return "Clean";
            }
            if (d <= 2)
            {
                return "Minor marks";
            }
            if (d <= 4)
            {
                return "Troubled";
            }
            if (d <= 6)
            {
                return "Serious";
            }
            if (d <= 9)
            {
                return "Critical";
            }
            return "Breaking";
        }

        public string GetLanceReputationStatus()
        {
            var rep = _state.LanceReputation;
            if (rep >= 40)
            {
                return "Bonded";
            }
            if (rep >= 20)
            {
                return "Trusted";
            }
            if (rep >= 5)
            {
                return "Accepted";
            }
            if (rep >= -4)
            {
                return "Neutral";
            }
            if (rep >= -19)
            {
                return "Disliked";
            }
            if (rep >= -39)
            {
                return "Outcast";
            }
            return "Hated";
        }

        public string GetMedicalRiskStatus()
        {
            var risk = _state.MedicalRisk;
            if (risk <= 0)
            {
                return "None";
            }
            if (risk <= 2)
            {
                return "Mild";
            }
            if (risk == 3)
            {
                return "Concerning";
            }
            if (risk == 4)
            {
                return "Serious";
            }
            return "Critical";
        }

        #endregion

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }
            return value > max ? max : value;
        }

        private static void LogTrackChange(string track, int oldValue, int newValue, string reason)
        {
            if (oldValue == newValue)
            {
                return;
            }

            var why = string.IsNullOrWhiteSpace(reason) ? string.Empty : $" ({reason})";
            ModLogger.Info(LogCategory, $"{track}: {oldValue} -> {newValue}{why}");
        }
    }
}


