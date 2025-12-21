using System;
using System.IO;
using EnlistedConfig = Enlisted.Mod.Core.Config.ConfigurationManager;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Escalation;
using Enlisted.Mod.Core.Logging;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Enlisted.Features.Conditions
{
    /// <summary>
    /// Manages the player condition system, including injuries, illnesses, and exhaustion.
    ///
    /// This system is feature-flagged and only active while the player is enlisted. 
    /// It uses safe persistence by storing only primitives and strings, and integrates 
    /// with the MedicalRisk escalation track where untreated conditions increase risk 
    /// and treatment resets it.
    /// </summary>
    public sealed class PlayerConditionBehavior : CampaignBehaviorBase
    {
        private const string LogCategory = "PlayerConditions";

        private readonly PlayerConditionState _state = new PlayerConditionState();
        private PlayerConditionDefinitionsJson _cachedDefs;
        private int _lastDailyTickDayNumber = -1;

        public static PlayerConditionBehavior Instance { get; private set; }

        public PlayerConditionBehavior()
        {
            Instance = this;
        }

        public PlayerConditionState State => _state;

        public bool IsEnabled()
        {
            return EnlistedConfig.LoadPlayerConditionsConfig()?.Enabled == true;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            SaveLoadDiagnostics.SafeSyncData(this, dataStore, () =>
            {
                // Injury
                var injSeverity = (int)_state.CurrentInjury;
                var injType = _state.InjuryType ?? string.Empty;
                var injDays = _state.InjuryDaysRemaining;

                // Illness
                var illSeverity = (int)_state.CurrentIllness;
                var illType = _state.IllnessType ?? string.Empty;
                var illDays = _state.IllnessDaysRemaining;

                // Exhaustion
                var exhaustion = (int)_state.Exhaustion;

                // Treatment
                var underCare = _state.UnderMedicalCare;
                var recoveryMult = _state.RecoveryRateModifier;

                dataStore.SyncData("pc_injSeverity", ref injSeverity);
                dataStore.SyncData("pc_injType", ref injType);
                dataStore.SyncData("pc_injDays", ref injDays);

                dataStore.SyncData("pc_illSeverity", ref illSeverity);
                dataStore.SyncData("pc_illType", ref illType);
                dataStore.SyncData("pc_illDays", ref illDays);

                dataStore.SyncData("pc_exhaust", ref exhaustion);
                dataStore.SyncData("pc_underCare", ref underCare);
                dataStore.SyncData("pc_recoveryMult", ref recoveryMult);

                if (dataStore.IsLoading)
                {
                    _state.CurrentInjury = (InjurySeverity)ClampEnum(injSeverity, 0, 4);
                    _state.InjuryType = injType ?? string.Empty;
                    _state.InjuryDaysRemaining = Math.Max(0, injDays);

                    _state.CurrentIllness = (IllnessSeverity)ClampEnum(illSeverity, 0, 4);
                    _state.IllnessType = illType ?? string.Empty;
                    _state.IllnessDaysRemaining = Math.Max(0, illDays);

                    _state.Exhaustion = (ExhaustionLevel)ClampEnum(exhaustion, 0, 4);

                    _state.UnderMedicalCare = underCare;
                    _state.RecoveryRateModifier = Math.Max(0.1f, recoveryMult);

                    NormalizeState();
                }
            });
        }

        private void NormalizeState()
        {
            if (_state.InjuryDaysRemaining <= 0)
            {
                _state.CurrentInjury = InjurySeverity.None;
                _state.InjuryType = string.Empty;
            }

            if (_state.IllnessDaysRemaining <= 0)
            {
                _state.CurrentIllness = IllnessSeverity.None;
                _state.IllnessType = string.Empty;
            }

            if (!_state.HasAnyCondition)
            {
                _state.ClearTreatment();
            }
        }

        private void OnDailyTick()
        {
            try
            {
                if (!IsEnabled())
                {
                    return;
                }

                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment?.IsEnlisted != true)
                {
                    return;
                }

                var dayNumber = (int)CampaignTime.Now.ToDays;
                if (dayNumber == _lastDailyTickDayNumber)
                {
                    return;
                }
                _lastDailyTickDayNumber = dayNumber;

                ApplyDailyRecoveryAndRisk();
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Daily tick failed", ex);
            }
        }

        private void ApplyDailyRecoveryAndRisk()
        {
            if (!_state.HasAnyCondition)
            {
                return;
            }

            var recoveryMult = Math.Max(0.1f, _state.RecoveryRateModifier);
            var recoveryTicks = _state.UnderMedicalCare ? Math.Max(1, (int)Math.Round(recoveryMult)) : 1;

            if (_state.HasInjury)
            {
                _state.InjuryDaysRemaining = Math.Max(0, _state.InjuryDaysRemaining - recoveryTicks);
            }

            if (_state.HasIllness)
            {
                _state.IllnessDaysRemaining = Math.Max(0, _state.IllnessDaysRemaining - recoveryTicks);
            }

            // Medical risk rises slowly when conditions are untreated. Treatment resets it (see ApplyTreatment).
            if ((_state.HasInjury || _state.HasIllness) && !_state.UnderMedicalCare)
            {
                EscalationManager.Instance?.ModifyMedicalRisk(1, "condition_untreated");
            }

            var recoveredAny = false;
            if (_state.InjuryDaysRemaining <= 0 && _state.CurrentInjury != InjurySeverity.None)
            {
                recoveredAny = true;
            }
            if (_state.IllnessDaysRemaining <= 0 && _state.CurrentIllness != IllnessSeverity.None)
            {
                recoveredAny = true;
            }

            NormalizeState();

            if (recoveredAny)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    new TextObject("{=cond_recovered}You’ve recovered.").ToString(),
                    Colors.Green));
            }
        }

        public bool CanTrain()
        {
            if (!IsEnabled())
            {
                return true;
            }

            if (_state.CurrentInjury >= InjurySeverity.Severe)
            {
                return false;
            }

            if (_state.CurrentIllness >= IllnessSeverity.Severe)
            {
                return false;
            }

            if (EnlistedConfig.LoadPlayerConditionsConfig()?.ExhaustionEnabled == true &&
                _state.Exhaustion >= ExhaustionLevel.Broken)
            {
                return false;
            }

            return true;
        }

        public void ApplyTreatment(float recoveryMultiplier, string reason)
        {
            if (!IsEnabled())
            {
                return;
            }

            if (!_state.HasAnyCondition)
            {
                return;
            }

            _state.UnderMedicalCare = true;
            _state.RecoveryRateModifier = Math.Max(1.0f, recoveryMultiplier);

            // Treatment resets medical risk pressure.
            EscalationManager.Instance?.ResetMedicalRisk(reason ?? "treatment");

            InformationManager.DisplayMessage(new InformationMessage(
                new TextObject("{=cond_treatment_applied}Treatment applied.").ToString(),
                Colors.White));
        }

        public void TryApplyInjury(string injuryType, InjurySeverity severity, int days, string reason)
        {
            if (!IsEnabled())
            {
                return;
            }

            if (severity == InjurySeverity.None || days <= 0)
            {
                return;
            }

            // If already injured, upgrade severity or extend.
            if (_state.HasInjury)
            {
                if (severity > _state.CurrentInjury)
                {
                    _state.CurrentInjury = severity;
                    _state.InjuryType = injuryType ?? string.Empty;
                    _state.InjuryDaysRemaining = Math.Max(_state.InjuryDaysRemaining, days);
                }
                else
                {
                    _state.InjuryDaysRemaining = Math.Max(_state.InjuryDaysRemaining, days);
                }
            }
            else
            {
                _state.CurrentInjury = severity;
                _state.InjuryType = injuryType ?? string.Empty;
                _state.InjuryDaysRemaining = days;
            }

            _state.ClearTreatment(); // new injury cancels current treatment until reapplied

            var label = GetInjuryLabel(_state.InjuryType);
            var severityText = GetInjurySeverityLabel(severity);
            var msg = new TextObject("{=cond_injury_gained}You suffered a {SEVERITY} injury: {INJURY}.");
            msg.SetTextVariable("SEVERITY", severityText);
            msg.SetTextVariable("INJURY", label);

            InformationManager.DisplayMessage(new InformationMessage(msg.ToString(), Colors.Red));
            ModLogger.Info(LogCategory, $"Injury applied ({severity} {injuryType}, days={days}, why={reason})");
        }

        public void TryApplyIllness(string illnessType, IllnessSeverity severity, int days, string reason)
        {
            if (!IsEnabled())
            {
                return;
            }

            if (severity == IllnessSeverity.None || days <= 0)
            {
                return;
            }

            if (_state.HasIllness)
            {
                if (severity > _state.CurrentIllness)
                {
                    _state.CurrentIllness = severity;
                    _state.IllnessType = illnessType ?? string.Empty;
                    _state.IllnessDaysRemaining = Math.Max(_state.IllnessDaysRemaining, days);
                }
                else
                {
                    _state.IllnessDaysRemaining = Math.Max(_state.IllnessDaysRemaining, days);
                }
            }
            else
            {
                _state.CurrentIllness = severity;
                _state.IllnessType = illnessType ?? string.Empty;
                _state.IllnessDaysRemaining = days;
            }

            _state.ClearTreatment();

            var label = GetIllnessLabel(_state.IllnessType);
            var severityText = GetIllnessSeverityLabel(severity);
            var msg = new TextObject("{=cond_illness_gained}You fell ill ({SEVERITY}): {ILLNESS}.");
            msg.SetTextVariable("SEVERITY", severityText);
            msg.SetTextVariable("ILLNESS", label);
            InformationManager.DisplayMessage(new InformationMessage(msg.ToString(), Colors.Red));
            ModLogger.Info(LogCategory, $"Illness applied ({severity} {illnessType}, days={days}, why={reason})");
        }

        internal PlayerConditionDefinitionsJson LoadDefinitions()
        {
            if (_cachedDefs != null)
            {
                return _cachedDefs;
            }

            try
            {
                var cfg = EnlistedConfig.LoadPlayerConditionsConfig() ?? new Mod.Core.Config.PlayerConditionsConfig();
                var rel = cfg.DefinitionsFile ?? "Conditions\\condition_defs.json";
                var path = Path.Combine(EnlistedConfig.GetModuleDataPathForConsumers(), rel);
                if (!File.Exists(path))
                {
                    ModLogger.Warn(LogCategory, $"Condition definitions missing at: {path}");
                    return null;
                }

                var json = File.ReadAllText(path);
                var parsed = JsonConvert.DeserializeObject<PlayerConditionDefinitionsJson>(json);
                if (parsed?.SchemaVersion != 1)
                {
                    ModLogger.Warn(LogCategory, "Condition definitions schema mismatch (expected 1)");
                    return null;
                }

                _cachedDefs = parsed;
                return _cachedDefs;
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Failed to load condition definitions", ex);
                return null;
            }
        }

        public int GetBaseRecoveryDaysForInjury(string injuryType, InjurySeverity severity)
        {
            var defs = LoadDefinitions();
            if (defs?.Injuries == null || string.IsNullOrWhiteSpace(injuryType) || severity == InjurySeverity.None)
            {
                return 0;
            }

            if (!defs.Injuries.TryGetValue(injuryType, out var def) || def?.BaseRecoveryDays == null)
            {
                return 0;
            }

            return severity switch
            {
                InjurySeverity.Minor => def.BaseRecoveryDays.Minor,
                InjurySeverity.Moderate => def.BaseRecoveryDays.Moderate,
                InjurySeverity.Severe => def.BaseRecoveryDays.Severe,
                InjurySeverity.Critical => Math.Max(def.BaseRecoveryDays.Critical, def.BaseRecoveryDays.Severe),
                _ => 0
            };
        }

        public int GetBaseRecoveryDaysForIllness(string illnessType, IllnessSeverity severity)
        {
            var defs = LoadDefinitions();
            if (defs?.Illnesses == null || string.IsNullOrWhiteSpace(illnessType) || severity == IllnessSeverity.None)
            {
                return 0;
            }

            if (!defs.Illnesses.TryGetValue(illnessType, out var def) || def?.BaseRecoveryDays == null)
            {
                return 0;
            }

            return severity switch
            {
                IllnessSeverity.Mild => def.BaseRecoveryDays.Mild,
                IllnessSeverity.Moderate => def.BaseRecoveryDays.Moderate,
                IllnessSeverity.Severe => def.BaseRecoveryDays.Severe,
                IllnessSeverity.Critical => Math.Max(def.BaseRecoveryDays.Critical, def.BaseRecoveryDays.Severe),
                _ => 0
            };
        }

        private TextObject GetInjuryLabel(string injuryType)
        {
            var defs = LoadDefinitions();
            if (defs?.Injuries == null || string.IsNullOrWhiteSpace(injuryType) ||
                !defs.Injuries.TryGetValue(injuryType, out var def) || def == null)
            {
                return new TextObject("{=cond_injury_generic}Injury");
            }

            var embeddedFallback = def.DisplayNameFallback ?? "Injury";
            return new TextObject("{=" + (def.DisplayNameId ?? string.Empty) + "}" + embeddedFallback);
        }

        private TextObject GetIllnessLabel(string illnessType)
        {
            var defs = LoadDefinitions();
            if (defs?.Illnesses == null || string.IsNullOrWhiteSpace(illnessType) ||
                !defs.Illnesses.TryGetValue(illnessType, out var def) || def == null)
            {
                return new TextObject("{=cond_illness_generic}Illness");
            }

            var embeddedFallback = def.DisplayNameFallback ?? "Illness";
            return new TextObject("{=" + (def.DisplayNameId ?? string.Empty) + "}" + embeddedFallback);
        }

        private static TextObject GetInjurySeverityLabel(InjurySeverity severity)
        {
            return severity switch
            {
                InjurySeverity.Minor => new TextObject("{=cond_sev_injury_minor}minor"),
                InjurySeverity.Moderate => new TextObject("{=cond_sev_injury_moderate}moderate"),
                InjurySeverity.Severe => new TextObject("{=cond_sev_injury_severe}severe"),
                InjurySeverity.Critical => new TextObject("{=cond_sev_injury_critical}critical"),
                _ => new TextObject(string.Empty)
            };
        }

        private static TextObject GetIllnessSeverityLabel(IllnessSeverity severity)
        {
            return severity switch
            {
                IllnessSeverity.Mild => new TextObject("{=cond_sev_ill_mild}mild"),
                IllnessSeverity.Moderate => new TextObject("{=cond_sev_ill_moderate}moderate"),
                IllnessSeverity.Severe => new TextObject("{=cond_sev_ill_severe}severe"),
                IllnessSeverity.Critical => new TextObject("{=cond_sev_ill_critical}critical"),
                _ => new TextObject(string.Empty)
            };
        }

        private static int ClampEnum(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }
            return value > max ? max : value;
        }
    }
}


