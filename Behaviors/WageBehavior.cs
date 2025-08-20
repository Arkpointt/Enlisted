using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Library;

namespace Enlisted.Behaviors
{
    /// <summary>
    /// Handles enlisted wage payment using direct gold transfer approach.
    /// This matches the successful implementation from the original ServeAsSoldier mod.
    /// The wages are paid directly via GiveGoldAction rather than trying to integrate 
    /// with the complex finance display system.
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
            // No persistent fields needed
        }

        /// <summary>
        /// Daily tick handler that pays wages when enlisted.
        /// Uses the same direct approach as the original ServeAsSoldier mod.
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
            GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, wage, false);
            
            // Optional: Show a discrete message (less spammy than the current one)
            // InformationManager.DisplayMessage(new InformationMessage(
            //     $"[Enlisted] Daily wage: {wage} denars"));
        }
    }
}