using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;
using Enlisted.Features.Enlistment.Application;
using Enlisted.Core.Logging;
using Enlisted.Core.DependencyInjection;

namespace Enlisted.GameAdapters.Patches
{
    // Harmony Patch
    // Target: Various army wait/raiding menu condition methods
    // Why: Suppress leave/abandon options while enlisted to prevent breaking enlistment contract and conflicting UX
    // Safety: Campaign-only; returns early when not enlisted; modifies only boolean condition results and menu state
    // Notes: Logs at Debug; attribute-based targets; no allocations
    [HarmonyPatch]
    public class SuppressArmyMenuPatch
    {
        /// <summary>
        /// Suppresses the "leave army" option in wait menus when enlisted.
        /// Prevents conflict between game army leave mechanics and enlistment contract.
        /// </summary>
        [HarmonyPatch("TaleWorlds.CampaignSystem.CampaignBehaviors.PlayerArmyWaitBehavior", "wait_menu_army_leave_on_condition")]
        [HarmonyPrefix]
        private static bool SuppressLeaveArmyOption(ref bool __result)
        {
            if (TryGetEnlistmentService(out var enlistmentService) && enlistmentService.IsEnlisted)
            {
                LogDecision("Suppressed army leave option - player is enlisted");
                __result = false;
                return false; // Skip original method execution
            }
            
            return true; // Execute original method
        }

        /// <summary>
        /// Suppresses the "abandon army" option in wait menus when enlisted.
        /// Prevents abandoning army while under active enlistment contract.
        /// </summary>
        [HarmonyPatch("TaleWorlds.CampaignSystem.CampaignBehaviors.PlayerArmyWaitBehavior", "wait_menu_army_abandon_on_condition")]
        [HarmonyPrefix]
        private static bool SuppressAbandonArmyOption(ref bool __result)
        {
            if (TryGetEnlistmentService(out var enlistmentService) && enlistmentService.IsEnlisted)
            {
                LogDecision("Suppressed army abandon option - player is enlisted");
                __result = false;
                return false; // Skip original method execution
            }
            
            return true; // Execute original method
        }

        /// <summary>
        /// Redirects army menu flow when enlisted to maintain narrative consistency.
        /// Prevents showing standard army wait menu which contains options that conflict
        /// with enlistment state. Returns null to preserve current menu context.
        /// </summary>
        [HarmonyPatch("TaleWorlds.CampaignSystem.GameModels.DefaultEncounterGameMenuModel", "GetGenericStateMenu")]
        [HarmonyPostfix]
        private static void RedirectArmyMenuWhenEnlisted(ref string __result)
        {
            if (TryGetEnlistmentService(out var enlistmentService) && enlistmentService.IsEnlisted && 
                (__result == "army_wait" || __result == "army_wait_at_settlement"))
            {
                LogDecision("Redirected army menu - maintaining enlistment narrative flow");
                __result = null; // Maintain current state instead of problematic army wait menu
            }
        }

        /// <summary>
        /// Suppresses the "leave army while raiding" option when enlisted.
        /// Ensures enlisted players cannot break contract during hostile actions.
        /// Maintains contract obligation even during village raids.
        /// </summary>
        [HarmonyPatch("TaleWorlds.CampaignSystem.CampaignBehaviors.VillageHostileActionCampaignBehavior", "wait_menu_end_raiding_at_army_by_leaving_on_condition")]
        [HarmonyPrefix]
        private static bool SuppressRaidingLeaveArmy(ref bool __result)
        {
            if (TryGetEnlistmentService(out var enlistmentService) && enlistmentService.IsEnlisted)
            {
                LogDecision("Suppressed raiding leave option - player is enlisted");
                __result = false;
                return false; // Skip original method execution
            }
            
            return true; // Execute original method
        }

        /// <summary>
        /// Suppresses the "abandon army while raiding" option when enlisted.
        /// Ensures enlisted players cannot abandon during hostile actions.
        /// Prevents contract violation during village raids.
        /// </summary>
        [HarmonyPatch("TaleWorlds.CampaignSystem.CampaignBehaviors.VillageHostileActionCampaignBehavior", "wait_menu_end_raiding_at_army_by_abandoning_on_condition")]
        [HarmonyPrefix]
        private static bool SuppressRaidingAbandonArmy(ref bool __result)
        {
            if (TryGetEnlistmentService(out var enlistmentService) && enlistmentService.IsEnlisted)
            {
                LogDecision("Suppressed raiding abandon option - player is enlisted");
                __result = false;
                return false; // Skip original method execution
            }
            
            return true; // Execute original method
        }

        /// <summary>
        /// Helper method to get enlistment service through dependency injection.
        /// Falls back to static instance during transition period.
        /// </summary>
        private static bool TryGetEnlistmentService(out IEnlistmentService enlistmentService)
        {
            // Try dependency injection first (ADR-004 pattern)
            if (ServiceLocator.TryGetService<IEnlistmentService>(out enlistmentService))
            {
                return true;
            }

            // Fallback to static instance during transition
            if (EnlistmentBehavior.Instance != null)
            {
                enlistmentService = EnlistmentBehavior.Instance;
                return true;
            }

            enlistmentService = null;
            return false;
        }

        /// <summary>
        /// Log patch decisions using centralized logging service.
        /// Falls back to Debug.Print if logging service unavailable.
        /// </summary>
        private static void LogDecision(string decision)
        {
            if (ServiceLocator.TryGetService<ILoggingService>(out var logger))
            {
                logger.LogDebug(LogCategories.GameAdapters, decision);
            }
            else
            {
                // Fallback during transition period
                Debug.Print($"[Enlisted] {decision}");
            }
        }
    }
}
