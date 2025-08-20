using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using Enlisted.Behaviors;

namespace Enlisted.Patches
{
    /// <summary>
    /// Suppress leave/abandon army options in army menus while enlisted to avoid
    /// conflicting UX with the enlistment system.
    /// </summary>
    [HarmonyPatch]
    public class SuppressArmyMenuPatch
    {
        [HarmonyPatch("TaleWorlds.CampaignSystem.CampaignBehaviors.PlayerArmyWaitBehavior", "wait_menu_army_leave_on_condition")]
        [HarmonyPrefix]
        private static bool SuppressLeaveArmyOption(ref bool __result)
        {
            if (EnlistmentBehavior.Instance?.IsEnlisted == true)
            {
                __result = false;
                return false;
            }
            
            return true;
        }

        [HarmonyPatch("TaleWorlds.CampaignSystem.CampaignBehaviors.PlayerArmyWaitBehavior", "wait_menu_army_abandon_on_condition")]
        [HarmonyPrefix]
        private static bool SuppressAbandonArmyOption(ref bool __result)
        {
            if (EnlistmentBehavior.Instance?.IsEnlisted == true)
            {
                __result = false;
                return false;
            }
            
            return true;
        }

        [HarmonyPatch("TaleWorlds.CampaignSystem.GameModels.DefaultEncounterGameMenuModel", "GetGenericStateMenu")]
        [HarmonyPostfix]
        private static void RedirectArmyMenuWhenEnlisted(ref string __result)
        {
            if (EnlistmentBehavior.Instance?.IsEnlisted == true && 
                (__result == "army_wait" || __result == "army_wait_at_settlement"))
            {
                __result = null; // Stick with current state instead of showing the army wait menu.
            }
        }

        [HarmonyPatch("TaleWorlds.CampaignSystem.CampaignBehaviors.VillageHostileActionCampaignBehavior", "wait_menu_end_raiding_at_army_by_leaving_on_condition")]
        [HarmonyPrefix]
        private static bool SuppressRaidingLeaveArmy(ref bool __result)
        {
            if (EnlistmentBehavior.Instance?.IsEnlisted == true)
            {
                __result = false;
                return false;
            }
            
            return true;
        }

        [HarmonyPatch("TaleWorlds.CampaignSystem.CampaignBehaviors.VillageHostileActionCampaignBehavior", "wait_menu_end_raiding_at_army_by_abandoning_on_condition")]
        [HarmonyPrefix]
        private static bool SuppressRaidingAbandonArmy(ref bool __result)
        {
            if (EnlistmentBehavior.Instance?.IsEnlisted == true)
            {
                __result = false;
                return false;
            }
            
            return true;
        }
    }
}
