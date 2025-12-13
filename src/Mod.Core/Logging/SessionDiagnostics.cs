using System;
using System.Text;
using Enlisted.Features.Assignments.Core;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace Enlisted.Mod.Core.Logging
{
    /// <summary>
    ///     Logs session startup diagnostics once per game session.
    ///     Provides visibility into mod configuration and state without log spam.
    /// </summary>
    public static class SessionDiagnostics
    {
        /// <summary>
        ///     Mod version for diagnostics. Update this when releasing new versions.
        /// </summary>
        public const string ModVersion = "0.6.0";

        public const string TargetGameVersion = "1.3.10";
        private static bool _hasLoggedStartup;
        private static bool _hasLoggedConfigValues;

        /// <summary>
        ///     Log startup diagnostics once when the game session begins.
        ///     Safe to call multiple times - only logs once per session.
        /// </summary>
        public static void LogStartupDiagnostics()
        {
            if (_hasLoggedStartup)
            {
                return;
            }

            _hasLoggedStartup = true;

            var sb = new StringBuilder();
            sb.AppendLine("=== ENLISTED MOD SESSION START ===");
            sb.AppendLine($"Mod Version: {ModVersion}");
            sb.AppendLine($"Target Game Version: {TargetGameVersion}");
            sb.AppendLine($"Session Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($".NET Runtime: {Environment.Version}");
            sb.AppendLine("===================================");

            ModLogger.Info("Session", sb.ToString());
        }

        /// <summary>
        ///     Log configuration values once when configs are first loaded.
        ///     Helps verify that JSON configs are being read correctly.
        /// </summary>
        public static void LogConfigurationValues()
        {
            if (_hasLoggedConfigValues)
            {
                return;
            }

            _hasLoggedConfigValues = true;

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("--- Configuration Loaded ---");

                // Gameplay config
                var gameplay = ConfigurationManager.LoadGameplayConfig();
                sb.AppendLine($"[Gameplay] reserve_troop_threshold: {gameplay.ReserveTroopThreshold}");
                sb.AppendLine($"[Gameplay] desertion_grace_period_days: {gameplay.DesertionGracePeriodDays}");

                // Finance config
                var finance = ConfigurationManager.LoadFinanceConfig();
                sb.AppendLine($"[Finance] show_in_clan_tooltip: {finance.ShowInClanTooltip}");
                sb.AppendLine($"[Finance] base_wage: {finance.WageFormula.BaseWage}");
                sb.AppendLine($"[Finance] army_bonus_multiplier: {finance.WageFormula.ArmyBonusMultiplier}");

                // Tier progression
                var tierXP = ConfigurationManager.GetTierXpRequirements();
                sb.AppendLine($"[Progression] tier_count: {tierXP.Length}");
                sb.AppendLine(
                    $"[Progression] max_tier_xp: {(tierXP.Length > 0 ? tierXP[tierXP.Length - 1].ToString() : "N/A")}");

                // Lances config
                var lances = ConfigurationManager.LoadLancesConfig();
                sb.AppendLine(
                    $"[Lances] enabled: {lances.LancesEnabled}, selection_count: {lances.LanceSelectionCount}, culture_weighting: {lances.UseCultureWeighting}");

                // Feature flags (end-user friendly; the "what's turned on" answer in one place)
                var campLife = ConfigurationManager.LoadCampLifeConfig();
                sb.AppendLine($"[CampLife] enabled: {campLife?.Enabled == true}");

                var escalation = ConfigurationManager.LoadEscalationConfig();
                sb.AppendLine($"[Escalation] enabled: {escalation?.Enabled == true}");

                var personas = ConfigurationManager.LoadLancePersonasConfig();
                sb.AppendLine($"[LancePersonas] enabled: {personas?.Enabled == true}");

                var cond = ConfigurationManager.LoadPlayerConditionsConfig();
                sb.AppendLine($"[PlayerConditions] enabled: {cond?.Enabled == true}, exhaustion_enabled: {cond?.ExhaustionEnabled == true}");

                var campActivities = ConfigurationManager.LoadCampActivitiesConfig();
                sb.AppendLine($"[CampActivities] enabled: {campActivities?.Enabled == true}, definitions_file: {campActivities?.DefinitionsFile}");

                var ll = ConfigurationManager.LoadLanceLifeConfig();
                sb.AppendLine($"[LanceLife] enabled: {ll?.Enabled == true}, min_tier: {ll?.MinTier}, max_stories_per_week: {ll?.MaxStoriesPerWeek}");

                var llEvents = ConfigurationManager.LoadLanceLifeEventsConfig();
                if (llEvents != null)
                {
                    sb.AppendLine($"[LanceLifeEvents] enabled: {llEvents.Enabled}, folder: {llEvents.EventsFolder}");
                    sb.AppendLine($"[LanceLifeEvents] automatic: {llEvents.Automatic?.Enabled == true}, cadence_hours: {llEvents.Automatic?.EvaluationCadenceHours}, max_per_day: {llEvents.Automatic?.MaxEventsPerDay}");
                    sb.AppendLine($"[LanceLifeEvents] player_initiated: {llEvents.PlayerInitiated?.Enabled == true}, block_training_on_severe_condition: {llEvents.PlayerInitiated?.BlockTrainingOnSevereCondition == true}");
                    sb.AppendLine($"[LanceLifeEvents] onboarding: {llEvents.Onboarding?.Enabled == true}, skip_for_veterans: {llEvents.Onboarding?.SkipForVeterans == true}, stage_count: {llEvents.Onboarding?.StageCount}");
                    sb.AppendLine($"[LanceLifeEvents] incident_channel: {llEvents.IncidentChannel?.Enabled == true}");
                }

                sb.AppendLine("----------------------------");

                ModLogger.Info("Config", sb.ToString());
            }
            catch (Exception ex)
            {
                ModLogger.Error("Config", $"Failed to log configuration values: {ex.Message}");
            }
        }

        /// <summary>
        ///     Log a state transition for debugging purposes.
        ///     Use for important one-time events, not per-tick updates.
        /// </summary>
        public static void LogStateTransition(string system, string fromState, string toState, string details = null)
        {
            var message = $"[{fromState}] -> [{toState}]";
            if (!string.IsNullOrEmpty(details))
            {
                message += $" | {details}";
            }

            ModLogger.Debug(system, message);
        }

        /// <summary>
        ///     Log an important one-time event for troubleshooting.
        /// </summary>
        public static void LogEvent(string system, string eventName, string details = null)
        {
            var message = $"EVENT: {eventName}";
            if (!string.IsNullOrEmpty(details))
            {
                message += $" | {details}";
            }

            ModLogger.Debug(system, message);
        }
    }

    /// <summary>
    ///     Lightweight, user-friendly save/load logging.
    ///     Implemented via two marker behaviors registered first/last so we can log both "begin" and "end"
    ///     without relying on fragile internal save-system hooks.
    /// </summary>
    public static class SaveLoadDiagnostics
    {
        private static readonly object Sync = new object();

        private static int _saveSequence;
        private static int _loadSequence;

        private static DateTime _saveStartUtc;
        private static DateTime _loadStartUtc;

        private static bool _saveInProgress;
        private static bool _loadInProgress;

        public static void OnSaveBegin()
        {
            lock (Sync)
            {
                if (_saveInProgress)
                {
                    return;
                }

                _saveInProgress = true;
                _saveSequence++;
                _saveStartUtc = DateTime.UtcNow;
            }

            var heroName = Hero.MainHero?.Name?.ToString() ?? "Unknown";
            var day = CampaignTime.Now.ToDays;
            ModLogger.Info("SaveLoad", $"Saving game... (#{_saveSequence}, Hero: {heroName}, Day: {day:F1})");
        }

        public static void OnSaveEnd()
        {
            int seq;
            DateTime startUtc;

            lock (Sync)
            {
                if (!_saveInProgress)
                {
                    return;
                }

                _saveInProgress = false;
                seq = _saveSequence;
                startUtc = _saveStartUtc;
            }

            var elapsedMs = (int)Math.Max(0, (DateTime.UtcNow - startUtc).TotalMilliseconds);
            ModLogger.Info("SaveLoad", $"Save finished. (#{seq}, {elapsedMs} ms)");
        }

        public static void OnLoadBegin()
        {
            lock (Sync)
            {
                if (_loadInProgress)
                {
                    return;
                }

                _loadInProgress = true;
                _loadSequence++;
                _loadStartUtc = DateTime.UtcNow;
            }

            ModLogger.Info("SaveLoad", $"Loading save... (#{_loadSequence})");
        }

        public static void OnLoadEnd()
        {
            int seq;
            DateTime startUtc;

            lock (Sync)
            {
                if (!_loadInProgress)
                {
                    return;
                }

                _loadInProgress = false;
                seq = _loadSequence;
                startUtc = _loadStartUtc;
            }

            var elapsedMs = (int)Math.Max(0, (DateTime.UtcNow - startUtc).TotalMilliseconds);
            ModLogger.Info("SaveLoad", $"Load finished. (#{seq}, {elapsedMs} ms)");
        }
    }

    /// <summary>
    ///     Save/load marker behavior. Register one instance at the beginning of the behavior list and one at the end.
    ///     This yields predictable "begin" and "end" callbacks during Save/Load serialization passes.
    /// </summary>
    public sealed class SaveLoadDiagnosticsMarkerBehavior : CampaignBehaviorBase
    {
        public enum Phase
        {
            Begin,
            End
        }

        private readonly Phase _phase;

        public SaveLoadDiagnosticsMarkerBehavior(Phase phase)
        {
            _phase = phase;
        }

        public override void RegisterEvents()
        {
            // No runtime events required.
        }

        public override void SyncData(IDataStore dataStore)
        {
            if (dataStore == null)
            {
                return;
            }

            if (dataStore.IsSaving)
            {
                if (_phase == Phase.Begin)
                {
                    SaveLoadDiagnostics.OnSaveBegin();
                }
                else
                {
                    SaveLoadDiagnostics.OnSaveEnd();
                }
            }
            else if (dataStore.IsLoading)
            {
                if (_phase == Phase.Begin)
                {
                    SaveLoadDiagnostics.OnLoadBegin();
                }
                else
                {
                    SaveLoadDiagnostics.OnLoadEnd();
                }
            }
        }
    }
}
