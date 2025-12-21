using System;
using System.Linq;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Config;
using Enlisted.Mod.Core.Logging;
using TaleWorlds.CampaignSystem;

namespace Enlisted.Features.Escalation
{
    /// <summary>
    /// Phase 4 escalation manager.
    ///
    /// Responsibilities:
    /// - Owns the persisted EscalationState (save/load via CampaignBehavior SyncData)
    /// - Provides track modification APIs (Heat/Discipline/SoldierRep/MedicalRisk)
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
            SaveLoadDiagnostics.SafeSyncData(this, dataStore, () =>
            {
                // Track values
                var heat = _state.Heat;
                var discipline = _state.Discipline;
                var soldierRep = _state.SoldierReputation;
                var lordRep = _state.LordReputation;
                var officerRep = _state.OfficerReputation;
                var medical = _state.MedicalRisk;

                dataStore.SyncData("esc_heat", ref heat);
                dataStore.SyncData("esc_discipline", ref discipline);
                
                // Migration: Load old LanceReputation into SoldierReputation
                if (dataStore.IsLoading)
                {
                    int oldLanceRep = 0;
                    dataStore.SyncData("esc_rep", ref oldLanceRep);
                    soldierRep = oldLanceRep;
                    
                    // Initialize new reputation fields at neutral
                    lordRep = 50;
                    officerRep = 50;
                }
                else
                {
                    // Save new reputation system
                    dataStore.SyncData("esc_soldierRep", ref soldierRep);
                    dataStore.SyncData("esc_lordRep", ref lordRep);
                    dataStore.SyncData("esc_officerRep", ref officerRep);
                }
                
                dataStore.SyncData("esc_medical", ref medical);

                // Timestamps
                var lastHeatRaised = _state.LastHeatRaisedTime;
                var lastHeatDecay = _state.LastHeatDecayTime;
                var lastDiscRaised = _state.LastDisciplineRaisedTime;
                var lastDiscDecay = _state.LastDisciplineDecayTime;
                var lastSoldierRepDecay = _state.LastSoldierReputationDecayTime;
                var lastMedicalDecay = _state.LastMedicalRiskDecayTime;
                var lastThresholdEvent = _state.LastThresholdEventTime;

                dataStore.SyncData("esc_lastHeatRaised", ref lastHeatRaised);
                dataStore.SyncData("esc_lastHeatDecay", ref lastHeatDecay);
                dataStore.SyncData("esc_lastDiscRaised", ref lastDiscRaised);
                dataStore.SyncData("esc_lastDiscDecay", ref lastDiscDecay);
                dataStore.SyncData("esc_lastRepDecay", ref lastSoldierRepDecay);
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
                    _state.SoldierReputation = soldierRep;
                    _state.LordReputation = lordRep;
                    _state.OfficerReputation = officerRep;
                    _state.MedicalRisk = medical;

                    _state.LastHeatRaisedTime = lastHeatRaised;
                    _state.LastHeatDecayTime = lastHeatDecay;
                    _state.LastDisciplineRaisedTime = lastDiscRaised;
                    _state.LastDisciplineDecayTime = lastDiscDecay;
                    _state.LastSoldierReputationDecayTime = lastSoldierRepDecay;
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
            });
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
            // Order across tracks: Heat, Discipline, Medical, Soldier Reputation.
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
                (_state.SoldierReputation <= EscalationThresholds.SoldierSabotage, "soldier_sabotage"),
                (_state.SoldierReputation <= EscalationThresholds.SoldierIsolated, "soldier_isolated"),
                (_state.SoldierReputation >= EscalationThresholds.SoldierBonded, "soldier_bonded"),
                (_state.SoldierReputation >= EscalationThresholds.SoldierTrusted, "soldier_trusted")
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

            // Show UI notification for heat changes (only when increasing - "attention" from authorities)
            if (_state.Heat != oldValue && delta > 0)
            {
                var statusText = GetHeatStatus();
                var color = _state.Heat >= EscalationThresholds.HeatShakedown
                    ? TaleWorlds.Library.Colors.Red
                    : TaleWorlds.Library.Colors.Yellow;
                var msg = new TaleWorlds.Localization.TextObject("{=esc_heat_changed}Heat increased (+{DELTA}) - Status: {STATUS}");
                msg.SetTextVariable("DELTA", delta);
                msg.SetTextVariable("STATUS", statusText);
                TaleWorlds.Library.InformationManager.DisplayMessage(
                    new TaleWorlds.Library.InformationMessage(msg.ToString(), color));
            }

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

            // Show UI notification for discipline changes (only when increasing - problems brewing)
            if (_state.Discipline != oldValue && delta > 0)
            {
                var statusText = GetDisciplineStatus();
                var color = _state.Discipline >= EscalationThresholds.DisciplineHearing
                    ? TaleWorlds.Library.Colors.Red
                    : TaleWorlds.Library.Colors.Yellow;
                var msg = new TaleWorlds.Localization.TextObject("{=esc_discipline_changed}Discipline issues (+{DELTA}) - Status: {STATUS}");
                msg.SetTextVariable("DELTA", delta);
                msg.SetTextVariable("STATUS", statusText);
                TaleWorlds.Library.InformationManager.DisplayMessage(
                    new TaleWorlds.Library.InformationMessage(msg.ToString(), color));
            }

            EvaluateThresholdsAndQueueIfNeeded();
        }

        public void ModifySoldierReputation(int delta, string reason = null)
        {
            if (!IsEnabled())
            {
                return;
            }

            var oldValue = _state.SoldierReputation;
            var next = oldValue + delta;
            _state.SoldierReputation = Clamp(next, EscalationState.SoldierReputationMin, EscalationState.SoldierReputationMax);
            LogTrackChange("SoldierReputation", oldValue, _state.SoldierReputation, reason);

            // Show UI notification for soldier reputation changes
            if (_state.SoldierReputation != oldValue)
            {
                var change = _state.SoldierReputation - oldValue;
                var sign = change > 0 ? "+" : "";
                var statusText = GetSoldierReputationStatus();
                var color = change > 0 ? TaleWorlds.Library.Colors.Cyan : TaleWorlds.Library.Colors.Yellow;
                var msg = new TaleWorlds.Localization.TextObject("{=esc_rep_changed}Soldier Reputation: {CHANGE} ({STATUS})");
                msg.SetTextVariable("CHANGE", $"{sign}{change}");
                msg.SetTextVariable("STATUS", statusText);
                TaleWorlds.Library.InformationManager.DisplayMessage(
                    new TaleWorlds.Library.InformationMessage(msg.ToString(), color));
            }

            // Report significant changes to news system
            if (Math.Abs(delta) >= 10 && Enlisted.Features.Interface.Behaviors.EnlistedNewsBehavior.Instance != null)
            {
                string message = GetReputationChangeMessage("Soldier", delta, _state.SoldierReputation);
                Enlisted.Features.Interface.Behaviors.EnlistedNewsBehavior.Instance.AddReputationChange(
                    target: "Soldier",
                    delta: delta,
                    newValue: _state.SoldierReputation,
                    message: message,
                    dayNumber: (int)CampaignTime.Now.ToDays
                );
            }

            EvaluateThresholdsAndQueueIfNeeded();
        }

        public void ModifyLordReputation(int delta, string reason = null)
        {
            if (!IsEnabled())
            {
                return;
            }

            var oldValue = _state.LordReputation;
            var next = oldValue + delta;
            _state.LordReputation = Clamp(next, EscalationState.LordReputationMin, EscalationState.LordReputationMax);
            LogTrackChange("LordReputation", oldValue, _state.LordReputation, reason);

            // Report significant changes to news system
            if (Math.Abs(delta) >= 10 && Enlisted.Features.Interface.Behaviors.EnlistedNewsBehavior.Instance != null)
            {
                string message = GetReputationChangeMessage("Lord", delta, _state.LordReputation);
                Enlisted.Features.Interface.Behaviors.EnlistedNewsBehavior.Instance.AddReputationChange(
                    target: "Lord",
                    delta: delta,
                    newValue: _state.LordReputation,
                    message: message,
                    dayNumber: (int)CampaignTime.Now.ToDays
                );
            }
        }

        public void ModifyOfficerReputation(int delta, string reason = null)
        {
            if (!IsEnabled())
            {
                return;
            }

            var oldValue = _state.OfficerReputation;
            var next = oldValue + delta;
            _state.OfficerReputation = Clamp(next, EscalationState.OfficerReputationMin, EscalationState.OfficerReputationMax);
            LogTrackChange("OfficerReputation", oldValue, _state.OfficerReputation, reason);

            // Report significant changes to news system
            if (Math.Abs(delta) >= 10 && Enlisted.Features.Interface.Behaviors.EnlistedNewsBehavior.Instance != null)
            {
                string message = GetReputationChangeMessage("Officer", delta, _state.OfficerReputation);
                Enlisted.Features.Interface.Behaviors.EnlistedNewsBehavior.Instance.AddReputationChange(
                    target: "Officer",
                    delta: delta,
                    newValue: _state.OfficerReputation,
                    message: message,
                    dayNumber: (int)CampaignTime.Now.ToDays
                );
            }
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

            // Soldier reputation: trends toward 0 by 1 per 14 days.
            {
                var old = _state.SoldierReputation;
                if (TryDecayTowardZero(old, _state.LastSoldierReputationDecayTime, cfg.SoldierReputationDecayIntervalDays, 1,
                        EscalationState.SoldierReputationMin, EscalationState.SoldierReputationMax, now, out var updated, out var updatedTime))
                {
                    _state.SoldierReputation = updated;
                    _state.LastSoldierReputationDecayTime = updatedTime;
                    ModLogger.Debug(LogCategory, $"Soldier reputation decayed: {old} -> {updated}");
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

        public string GetSoldierReputationStatus()
        {
            var rep = _state.SoldierReputation;
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

        public string GetLordReputationStatus()
        {
            var rep = _state.LordReputation;
            if (rep >= 80)
            {
                return "Celebrated";
            }
            if (rep >= 60)
            {
                return "Trusted";
            }
            if (rep >= 40)
            {
                return "Respected";
            }
            if (rep >= 20)
            {
                return "Promising";
            }
            if (rep >= 10)
            {
                return "Neutral";
            }
            return "Questionable";
        }

        public string GetOfficerReputationStatus()
        {
            var rep = _state.OfficerReputation;
            if (rep >= 80)
            {
                return "Celebrated";
            }
            if (rep >= 60)
            {
                return "Trusted";
            }
            if (rep >= 40)
            {
                return "Respected";
            }
            if (rep >= 20)
            {
                return "Promising";
            }
            if (rep >= 10)
            {
                return "Neutral";
            }
            return "Questionable";
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

        /// <summary>
        /// Generates a contextual message for a reputation change based on target and magnitude.
        /// </summary>
        private static string GetReputationChangeMessage(string target, int delta, int newValue)
        {
            if (delta >= 20)
            {
                return target switch
                {
                    "Lord" => "Your lord took special notice of your recent performance",
                    "Officer" => "The captain publicly commended your work",
                    "Soldier" => "The men respect you greatly after your recent actions",
                    _ => $"{target} reputation significantly improved"
                };
            }
            else if (delta >= 10)
            {
                return target switch
                {
                    "Lord" => "Your lord's confidence in you is growing",
                    "Officer" => "You've impressed the officers with your recent work",
                    "Soldier" => "Your standing with the men has improved",
                    _ => $"{target} reputation improved"
                };
            }
            else if (delta <= -20)
            {
                return target switch
                {
                    "Lord" => "You've seriously disappointed your lord",
                    "Officer" => "The officers question your competence",
                    "Soldier" => "The men have lost respect for you",
                    _ => $"{target} reputation significantly damaged"
                };
            }
            else // delta <= -10
            {
                return target switch
                {
                    "Lord" => "Your lord's confidence in you has declined",
                    "Officer" => "The officers are disappointed in your recent performance",
                    "Soldier" => "Your standing with the men has suffered",
                    _ => $"{target} reputation declined"
                };
            }
        }
    }
}


