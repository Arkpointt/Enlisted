using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using Enlisted.Behaviors;

namespace Enlisted.Patches
{
    /// <summary>
    /// Harmony patch to suppress army wait menu options when enlisted.
    /// Prevents the "Leave Army" menu from appearing when the player is enlisted
    /// and automatically joined to commander's army for battles.
    /// 
    /// This patch intercepts the army wait menu conditions and prevents
    /// the menu from activating when the player is in enlisted service.
    /// </summary>
    [HarmonyPatch]
    public class SuppressArmyMenuPatch
    {
        /// <summary>
        /// Patches the leave army option condition to prevent it from appearing when enlisted.
        /// This is the main method that prevents the "Leave Army" popup during enlisted service.
        /// </summary>
        [HarmonyPatch("TaleWorlds.CampaignSystem.CampaignBehaviors.PlayerArmyWaitBehavior", "wait_menu_army_leave_on_condition")]
        [HarmonyPrefix]
        private static bool SuppressLeaveArmyOption(ref bool __result)
        {
            // If enlisted, prevent the leave army option from being available
            if (EnlistmentBehavior.Instance?.IsEnlisted == true)
            {
                __result = false;
                return false; // Skip original method
            }
            
            return true; // Allow original method to run
        }

        /// <summary>
        /// Patch to prevent the abandon army option when enlisted.
        /// </summary>
        [HarmonyPatch("TaleWorlds.CampaignSystem.CampaignBehaviors.PlayerArmyWaitBehavior", "wait_menu_army_abandon_on_condition")]
        [HarmonyPrefix]
        private static bool SuppressAbandonArmyOption(ref bool __result)
        {
            // If enlisted, prevent the abandon army option from being available
            if (EnlistmentBehavior.Instance?.IsEnlisted == true)
            {
                __result = false;
                return false; // Skip original method
            }
            
            return true; // Allow original method to run
        }

        /// <summary>
        /// Alternative approach: Patch the EncounterGameMenuModel to redirect army menus when enlisted.
        /// This provides a more comprehensive solution by intercepting at the menu selection level.
        /// </summary>
        [HarmonyPatch("TaleWorlds.CampaignSystem.GameModels.DefaultEncounterGameMenuModel", "GetGenericStateMenu")]
        [HarmonyPostfix]
        private static void RedirectArmyMenuWhenEnlisted(ref string __result)
        {
            // If enlisted and the game wants to show army_wait menu, redirect to a neutral state
            if (EnlistmentBehavior.Instance?.IsEnlisted == true && 
                (__result == "army_wait" || __result == "army_wait_at_settlement"))
            {
                // Instead of showing army menu, continue with normal campaign flow
                __result = null; // This will cause the game to stay in current state
            }
        }

        /// <summary>
        /// Additional safety patch: Prevent army wait menu from showing during village raiding when enlisted.
        /// From the decompiled code, we know these menus also have leave army options.
        /// </summary>
        [HarmonyPatch("TaleWorlds.CampaignSystem.CampaignBehaviors.VillageHostileActionCampaignBehavior", "wait_menu_end_raiding_at_army_by_leaving_on_condition")]
        [HarmonyPrefix]
        private static bool SuppressRaidingLeaveArmy(ref bool __result)
        {
            // If enlisted, don't show the leave army option during raiding either
            if (EnlistmentBehavior.Instance?.IsEnlisted == true)
            {
                __result = false;
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// Additional safety patch for raiding abandon army option.
        /// </summary>
        [HarmonyPatch("TaleWorlds.CampaignSystem.CampaignBehaviors.VillageHostileActionCampaignBehavior", "wait_menu_end_raiding_at_army_by_abandoning_on_condition")]
        [HarmonyPrefix]
        private static bool SuppressRaidingAbandonArmy(ref bool __result)
        {
            // If enlisted, don't show the abandon army option during raiding either
            if (EnlistmentBehavior.Instance?.IsEnlisted == true)
            {
                __result = false;
                return false;
            }
            
            return true;
        }
    }
}