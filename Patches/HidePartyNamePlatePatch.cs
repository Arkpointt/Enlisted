using HarmonyLib;
using SandBox.ViewModelCollection.Nameplate;
using TaleWorlds.CampaignSystem.Party;
using Enlisted.Behaviors;

namespace Enlisted.Patches
{
    /// <summary>
    /// Hides the main party nameplate UI when enlisted to reinforce the illusion that
    /// the player is part of the commander's party.
    /// </summary>
    [HarmonyPatch(typeof(PartyNameplateVM), "RefreshBinding")]
    public class HidePartyNamePlatePatch
    {
        private static void Postfix(PartyNameplateVM __instance)
        {
            if (__instance.Party == MobileParty.MainParty)
            {
                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment != null && enlistment.IsEnlisted)
                {
                    __instance.IsMainParty = false;
                    __instance.IsVisibleOnMap = false;
                }
                else
                {
                    __instance.IsMainParty = true;
                }
            }
        }
    }
}
