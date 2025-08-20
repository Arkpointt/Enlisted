using HarmonyLib;
using SandBox.ViewModelCollection.Nameplate;
using TaleWorlds.CampaignSystem.Party;
using Enlisted.Behaviors;

namespace Enlisted.Patches
{
    /// <summary>
    /// Harmony patch to hide the party nameplate when enlisted.
    /// Modifies the PartyNameplateVM.RefreshBinding method to control visibility
    /// of the UI element showing party member count and banner.
    /// 
    /// This patch uses the modern syntax verified against decompiled references
    /// and is compatible with current Bannerlord versions.
    /// </summary>
    [HarmonyPatch(typeof(PartyNameplateVM), "RefreshBinding")]
    public class HidePartyNamePlatePatch
    {
        /// <summary>
        /// Postfix patch that modifies nameplate visibility based on enlistment status.
        /// Uses modern C# syntax and proper null checking patterns.
        /// </summary>
        /// <param name="__instance">The PartyNameplateVM instance being refreshed</param>
        private static void Postfix(PartyNameplateVM __instance)
        {
            // Only modify the main party's nameplate
            if (__instance.Party == MobileParty.MainParty)
            {
                var enlistmentBehavior = EnlistmentBehavior.Instance;
                
                if (enlistmentBehavior != null && enlistmentBehavior.IsEnlisted)
                {
                    // Hide the nameplate when enlisted - verified properties exist in decompiled refs
                    __instance.IsMainParty = false;
                    __instance.IsVisibleOnMap = false;
                }
                else
                {
                    // Restore normal visibility when not enlisted
                    __instance.IsMainParty = true;
                    // Note: IsVisibleOnMap is controlled by other game systems when not overridden
                }
            }
        }
    }
}