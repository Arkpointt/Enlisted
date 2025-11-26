using System;
using System.Text;
using Enlisted.Features.Assignments.Core;

namespace Enlisted.Mod.Core.Logging
{
    /// <summary>
    /// Logs session startup diagnostics once per game session.
    /// Provides visibility into mod configuration and state without log spam.
    /// </summary>
    public static class SessionDiagnostics
    {
        private static bool _hasLoggedStartup = false;
        private static bool _hasLoggedConfigValues = false;
        
        /// <summary>
        /// Mod version for diagnostics. Update this when releasing new versions.
        /// </summary>
        public const string ModVersion = "0.3.0";
        public const string TargetGameVersion = "1.3.5";
        
        /// <summary>
        /// Log startup diagnostics once when the game session begins.
        /// Safe to call multiple times - only logs once per session.
        /// </summary>
        public static void LogStartupDiagnostics()
        {
            if (_hasLoggedStartup) return;
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
        /// Log configuration values once when configs are first loaded.
        /// Helps verify that JSON configs are being read correctly.
        /// </summary>
        public static void LogConfigurationValues()
        {
            if (_hasLoggedConfigValues) return;
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
                var tierXP = ConfigurationManager.GetTierXPRequirements();
                sb.AppendLine($"[Progression] tier_count: {tierXP.Length}");
                sb.AppendLine($"[Progression] max_tier_xp: {(tierXP.Length > 0 ? tierXP[tierXP.Length - 1].ToString() : "N/A")}");
                
                sb.AppendLine("----------------------------");
                
                ModLogger.Info("Config", sb.ToString());
            }
            catch (Exception ex)
            {
                ModLogger.Error("Config", $"Failed to log configuration values: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Log a state transition for debugging purposes.
        /// Use for important one-time events, not per-tick updates.
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
        /// Log an important one-time event for troubleshooting.
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
        
        /// <summary>
        /// Log API usage for 1.3.4 migration verification.
        /// Call this once per API pattern to verify new APIs are working.
        /// </summary>
        public static void LogApiUsage(string apiName, bool success, string details = null)
        {
            var status = success ? "OK" : "FAILED";
            var message = $"API [{apiName}]: {status}";
            if (!string.IsNullOrEmpty(details))
            {
                message += $" | {details}";
            }
            ModLogger.Api(success ? "DEBUG" : "ERROR", message);
        }
    }
}

