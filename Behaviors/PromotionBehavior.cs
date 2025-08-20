using Enlisted.Models;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace Enlisted.Behaviors
{
    /// <summary>
    /// Core promotion engine. Phase 1: XP-only + daily trickle + AddXp API.
    /// </summary>
    public sealed class PromotionBehavior : CampaignBehaviorBase
    {
        private PromotionState _state = new PromotionState();
        private const int PassiveDailyXp = 5; // lets you test quickly before wiring battles

        public int Tier => _state.Tier;
        public int CurrentXp => _state.CurrentXp;
        public int NextTierXp => _state.NextTierXp;

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        public override void SyncData(IDataStore data)
        {
            data.SyncData(nameof(_state), ref _state);
            _state ??= new PromotionState();
            _state.EnsureInitialized();
        }

        // Public API for any system to grant XP (Phase 2 will call this from battle tracker)
        public void AddXp(int amount, string reason = null)
        {
            if (amount <= 0) return;
            var enlist = Campaign.Current?.GetCampaignBehavior<EnlistmentBehavior>();
            if (enlist == null || !enlist.IsEnlisted || enlist.Commander == null) return;

            _state.EnsureInitialized();
            _state.CurrentXp += amount;
            TryPromote();
        }

        private void OnDailyTick()
        {
            var enlist = Campaign.Current?.GetCampaignBehavior<EnlistmentBehavior>();
            if (enlist == null || !enlist.IsEnlisted || enlist.Commander == null) return;

            _state.EnsureInitialized();
            _state.CurrentXp += PassiveDailyXp;
            TryPromote();
        }

        private void TryPromote()
        {
            if (_state.CurrentXp < _state.NextTierXp) return;

            _state.CurrentXp -= _state.NextTierXp;
            _state.Tier++;
            _state.NextTierXp = PromotionRules.GetRequiredXpForTier(_state.Tier);

            InformationManager.DisplayMessage(
                new InformationMessage($"[Enlisted] Promoted to tier {_state.Tier}.", Colors.Green));
        }
    }
}
