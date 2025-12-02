using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Naval;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Mod.GameAdapters.Patches
{
    /// <summary>
    /// Prevents lords from using enlisted player's ships for sea travel.
    /// When a player is enlisted, their ships should not contribute to the lord's
    /// naval navigation capability - preventing unwanted ship damage and repair costs.
    /// </summary>
    [HarmonyPatch]
    public class NavalShipExclusionPatch
    {
        /// <summary>
        /// Target the NavalPartyNavigationModel.HasNavalNavigationCapability method.
        /// This requires targeting by name since it's in an external DLC assembly.
        /// </summary>
        [HarmonyPatch("NavalDLC.GameComponents.NavalPartyNavigationModel", "HasNavalNavigationCapability")]
        [HarmonyPostfix]
        static void Postfix(MobileParty mobileParty, ref bool __result)
        {
            try
            {
                if (!CampaignSafetyGuard.IsCampaignReady)
                    return;

                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment?.IsEnlisted != true)
                    return;

                // If checking the lord's party and result is true due to attached parties
                var lordParty = enlistment.EnlistedLord?.PartyBelongedTo;
                if (lordParty == null || mobileParty != lordParty)
                    return;

                // Check if the lord's own party has ships (not from attached parties)
                if (((List<Ship>)lordParty.Ships).Count > 0)
                    return; // Lord has own ships - keep result as true

                // Lord has no ships but result is true - must be from attached parties
                // Check if it's ONLY from the player's ships
                var mainParty = MobileParty.MainParty;
                if (mainParty == null || ((List<Ship>)mainParty.Ships).Count == 0)
                    return; // Player has no ships - not the cause

                // Check if any OTHER attached party has ships
                bool otherPartyHasShips = lordParty.AttachedParties
                    .Where(p => p != mainParty)
                    .Any(p => ((List<Ship>)p.Ships).Count > 0);

                if (!otherPartyHasShips)
                {
                    // Only player has ships - prevent lord from using them
                    __result = false;
                    ModLogger.LogOnce("naval_ship_exclusion", "Naval",
                        "Prevented lord from using enlisted player's ships for sea travel");
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("NavalShipExclusion", $"Error in naval ship exclusion patch: {ex.Message}");
            }
        }
    }
}
