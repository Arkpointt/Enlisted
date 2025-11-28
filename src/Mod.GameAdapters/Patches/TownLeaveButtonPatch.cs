using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.GameMenus;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Mod.GameAdapters.Patches
{
    /// <summary>
    /// Harmony patch that hides the native "Leave" button in town/castle menus when the player
    /// is actively enlisted. This prevents accidental departure from settlements.
    /// The button remains visible during grace periods or when on temporary leave.
    /// </summary>
    [HarmonyPatch(typeof(PlayerTownVisitCampaignBehavior), "game_menu_town_town_leave_on_condition")]
    public static class TownLeaveButtonPatch
    {
        /// <summary>
        /// Postfix that runs after the native condition check.
        /// Hides the Leave button if player is actively enlisted (not on leave, not in grace period).
        /// This covers "town", "castle", and "village" main menus.
        /// </summary>
        static void Postfix(ref bool __result, MenuCallbackArgs args)
        {
            try
            {
                // Only modify if native would show the button
                if (!__result)
                {
                    return;
                }

                var enlistment = EnlistmentBehavior.Instance;
                
                // Hide if actively enlisted (not on leave, not in grace period)
                if (enlistment?.IsEnlisted == true && 
                    !enlistment.IsOnLeave && 
                    !enlistment.IsInDesertionGracePeriod)
                {
                    __result = false;
                    ModLogger.Debug("TownLeave", "Hiding native Leave button (town/castle/village menu) - player is actively enlisted");
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("TownLeave", $"Error in TownLeaveButtonPatch: {ex.Message}");
                // Fail open - allow native behavior on error
            }
        }
    }
    
    /// <summary>
    /// Additional patch for the "outside" settlement menus (castle_outside, town_outside).
    /// These use a different condition method from EncounterGameMenuBehavior.
    /// </summary>
    [HarmonyPatch(typeof(EncounterGameMenuBehavior), "game_menu_leave_on_condition")]
    public static class SettlementOutsideLeaveButtonPatch
    {
        /// <summary>
        /// Postfix that hides the Leave button in outside settlement menus when enlisted.
        /// </summary>
        static void Postfix(ref bool __result, MenuCallbackArgs args)
        {
            try
            {
                // Only modify if native would show the button
                if (!__result)
                {
                    return;
                }

                var enlistment = EnlistmentBehavior.Instance;
                
                // Hide if actively enlisted (not on leave, not in grace period)
                if (enlistment?.IsEnlisted == true && 
                    !enlistment.IsOnLeave && 
                    !enlistment.IsInDesertionGracePeriod)
                {
                    __result = false;
                    ModLogger.Debug("TownLeave", "Hiding native Leave button (outside menu) - player is actively enlisted");
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("TownLeave", $"Error in SettlementOutsideLeaveButtonPatch: {ex.Message}");
                // Fail open - allow native behavior on error
            }
        }
    }
}

