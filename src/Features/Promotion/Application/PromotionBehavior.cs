using Enlisted.Core.Models;
using Enlisted.Features.Promotion.Domain;
using Enlisted.Features.Enlistment.Application;
using Enlisted.Core.Logging;
using Enlisted.Core.DependencyInjection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace Enlisted.Features.Promotion.Application
{
    /// <summary>
    /// Application orchestrator for promotion feature.
    /// Manages XP accumulation and tier advancement following blueprint patterns.
    /// Coordinates between domain rules and campaign integration.
    /// 
    /// Updated to use centralized logging service replacing TODO comments.
    /// </summary>
    public sealed class PromotionBehavior : CampaignBehaviorBase
    {
        private PromotionState _state = new PromotionState();
        private const int PassiveDailyXp = 5; // Base daily XP for testing
        private ILoggingService _logger;

        public int Tier => _state.Tier;
        public int CurrentXp => _state.CurrentXp;
        public int NextTierXp => _state.NextTierXp;

        public override void RegisterEvents()
        {
            // Initialize logging service
            if (ServiceLocator.TryGetService<ILoggingService>(out _logger))
            {
                _logger.LogInfo(LogCategories.Promotion, "PromotionBehavior initialized");
            }

            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        public override void SyncData(IDataStore data)
        {
            data.SyncData(nameof(_state), ref _state);
            _state ??= new PromotionState();
            _state.EnsureInitialized();

            _logger?.LogInfo(LogCategories.Persistence, "Promotion state loaded from save game. Tier: {0}, XP: {1}/{2}", 
                _state.Tier, _state.CurrentXp, _state.NextTierXp);
        }

        /// <summary>
        /// Grant XP from external systems (e.g., battle results).
        /// Validates enlistment state before awarding experience.
        /// </summary>
        public void AddXp(int amount, string reason = null)
        {
            if (amount <= 0) return;
            
            // Get enlistment service through dependency injection
            if (!TryGetEnlistmentService(out var enlistmentService) || 
                !enlistmentService.IsEnlisted || 
                enlistmentService.Commander == null) 
                return;

            _state.EnsureInitialized();
            _state.AddExperience(amount);
            
            _logger?.LogInfo(LogCategories.Promotion, "Awarded {0} XP for: {1}. Total: {2}/{3}", 
                amount, reason ?? "Unknown", _state.CurrentXp, _state.NextTierXp);
            
            TryPromote();
        }

        private void OnDailyTick()
        {
            // Get enlistment service through dependency injection
            if (!TryGetEnlistmentService(out var enlistmentService) || 
                !enlistmentService.IsEnlisted || 
                enlistmentService.Commander == null) 
                return;

            _state.EnsureInitialized();
            _state.AddExperience(PassiveDailyXp);
            
            _logger?.LogDebug(LogCategories.Promotion, "Daily passive XP awarded: {0}. Total: {1}/{2}", 
                PassiveDailyXp, _state.CurrentXp, _state.NextTierXp);
            
            TryPromote();
        }

        private void TryPromote()
        {
            if (_state.CurrentXp < _state.NextTierXp) return;

            // Use domain rules for tier advancement
            var newTierRequirement = PromotionRules.GetRequiredXpForTier(_state.Tier + 1);
            _state.AdvanceTier(newTierRequirement);

            var message = $"[Enlisted] Promoted to tier {_state.Tier}.";
            _logger?.ShowPlayerMessage(message);
            _logger?.LogInfo(LogCategories.Promotion, "Player promoted to tier {0}. Next requirement: {1} XP", 
                _state.Tier, _state.NextTierXp);
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
