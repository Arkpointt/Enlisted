using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace Enlisted.Behaviors
{
    /// <summary>
    /// Pays a fixed daily wage to the player while enlisted.
    /// Uses GiveGoldAction for simplicity and reliability.
    /// </summary>
    public class WageBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            // Register for daily tick to handle wage payments
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        public override void SyncData(IDataStore data)
        {
            // Stateless behavior; nothing to sync.
        }

        /// <summary>
        /// Daily handler that checks enlistment and transfers gold.
        /// </summary>
        private void OnDailyTick()
        {
            // Get enlistment status
            var enlistmentBehavior = Campaign.Current?.GetCampaignBehavior<EnlistmentBehavior>();
            if (enlistmentBehavior == null || !enlistmentBehavior.IsEnlisted || enlistmentBehavior.Commander == null)
            {
                return;
            }

            // Get the daily wage amount
            int wage = Settings.Instance.DailyWage;
            if (wage <= 0)
            {
                return;
            }

            // Pay the wage directly using GiveGoldAction - this is the proven approach
            TaleWorlds.CampaignSystem.Actions.GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, wage, false);
            // Optional: Consider a subtle message or event log entry if needed.
        }
    }
}
