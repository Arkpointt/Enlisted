using Enlisted.Features.Camp.UI.Management;
using TaleWorlds.CampaignSystem;

namespace Enlisted.Features.Camp.UI.Bulletin
{
    /// <summary>
    /// Lightweight tick bridge so the Camp UI overlays can close via ESC.
    /// Handles both Camp Bulletin and Camp Management screens.
    /// </summary>
    public sealed class CampBulletinUiTickBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.TickEvent.AddNonSerializedListener(this, OnTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // No persistent state.
        }

        private static void OnTick(float dt)
        {
            // Tick the Camp Bulletin screen (if open)
            if (CampBulletinScreen.IsOpen)
            {
                CampBulletinScreen.Tick();
            }
            
            // Tick the Camp Management screen (if open)
            if (CampManagementScreen.IsOpen)
            {
                CampManagementScreen.Tick();
            }
        }
    }
}


