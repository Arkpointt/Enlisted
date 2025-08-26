using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using Enlisted.Core.Config;
using Enlisted.Features.Enlistment.Application;
using Enlisted.Core.Logging;
using Enlisted.Core.DependencyInjection;

namespace Enlisted.Features.Wages.Application
{
    /// <summary>
    /// Application orchestrator for wage payment feature.
    /// Handles daily wage payments to enlisted players following blueprint patterns.
    /// Integrates with configuration system for wage amounts.
    /// 
    /// Updated to use centralized logging service and dependency injection.
    /// </summary>
    public class WageBehavior : CampaignBehaviorBase
    {
        private ILoggingService _logger;
        private ModSettings _settings;

        public override void RegisterEvents()
        {
            // Initialize services through dependency injection
            if (ServiceLocator.TryGetService<ILoggingService>(out _logger))
            {
                _logger.LogInfo(LogCategories.Wages, "WageBehavior initialized");
            }

            if (ServiceLocator.TryGetService<ModSettings>(out _settings))
            {
                _logger?.LogDebug(LogCategories.Wages, "Configuration loaded - Daily wage: {0}", _settings.DailyWage);
            }
            else
            {
                // Fallback to static instance
                _settings = ModSettings.Instance;
            }

            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        public override void SyncData(IDataStore data)
        {
            // Stateless behavior; nothing to sync
            _logger?.LogDebug(LogCategories.Persistence, "WageBehavior sync data - no state to persist");
        }

        /// <summary>
        /// Daily wage payment handler.
        /// Validates enlistment status and applies wage using game's gold system.
        /// </summary>
        private void OnDailyTick()
        {
            // Get enlistment service through dependency injection
            if (!TryGetEnlistmentService(out var enlistmentService) || 
                !enlistmentService.IsEnlisted || 
                enlistmentService.Commander == null)
            {
                return;
            }

            // Get configured wage amount
            int wage = _settings?.DailyWage ?? 10;
            if (wage <= 0)
            {
                _logger?.LogDebug(LogCategories.Wages, "Daily wage is 0 or negative, skipping payment");
                return;
            }

            try
            {
                // Apply wage payment using TaleWorlds action system
                TaleWorlds.CampaignSystem.Actions.GiveGoldAction.ApplyBetweenCharacters(
                    null, Hero.MainHero, wage, false);

                _logger?.LogInfo(LogCategories.Wages, "Daily wage paid: {0} gold to {1}", 
                    wage, Hero.MainHero.Name?.ToString() ?? "Player");

                // Show subtle message to player if verbose messages enabled
                if (_settings?.ShowVerboseMessages == true)
                {
                    _logger?.ShowPlayerMessage($"Daily wage: {wage} gold received");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(LogCategories.Wages, "Failed to pay daily wage", ex);
            }
        }

        /// <summary>
        /// Helper method to get enlistment service through dependency injection.
        /// Falls back to static instance during transition period.
        /// </summary>
        private bool TryGetEnlistmentService(out IEnlistmentService enlistmentService)
        {
            // Try dependency injection first (ADR-004 pattern)
            if (ServiceLocator.TryGetService<IEnlistmentService>(out enlistmentService))
            {
                return true;
            }

            // Fallback to static instance during transition
            if (EnlistmentBehavior.Instance != null)
            {
                enlistmentService = EnlistmentBehavior.Instance;
                return true;
            }

            enlistmentService = null;
            return false;
        }
    }
}
