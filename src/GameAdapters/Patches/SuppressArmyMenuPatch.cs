using HarmonyLib;
using System;
using System.Reflection;
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
    // Notes: Logs at Debug; uses TargetMethod approach for better error handling; no allocations
    //
    // IMPORTANT: Method signatures verified against game version 1.2.12.77991 decompiled DLLs
    // See ADR-009 and Blueprint Appendix C for Harmony patch development standards
    // Do NOT use outdated mod references - always verify against current game DLLs
    [HarmonyPatch]
    public class SuppressArmyMenuPatch
    {
        private static ILoggingService _logger;

        static SuppressArmyMenuPatch()
        {
            // Try to get logger during static initialization
            ServiceLocator.TryGetService<ILoggingService>(out _logger);
        }

        // Patch for PlayerArmyWaitBehavior.wait_menu_army_leave_on_condition (INSTANCE METHOD)
        // Signature verified: private bool wait_menu_army_leave_on_condition(MenuCallbackArgs args)
        [HarmonyPatch]
        private static class LeaveArmyPatch
        {
            public static MethodBase TargetMethod()
            {
                try
                {
                    var type = AccessTools.TypeByName("TaleWorlds.CampaignSystem.CampaignBehaviors.PlayerArmyWaitBehavior");
                    if (type == null)
                    {
                        LogPatchError("Could not find PlayerArmyWaitBehavior type");
                        return null;
                    }

                    var method = AccessTools.Method(type, "wait_menu_army_leave_on_condition", new[] { typeof(MenuCallbackArgs) });
                    if (method == null)
                    {
                        LogPatchError("Could not find wait_menu_army_leave_on_condition method");
                        return null;
                    }

                    LogPatchSuccess("Successfully found PlayerArmyWaitBehavior.wait_menu_army_leave_on_condition");
                    return method;
                }
                catch (Exception ex)
                {
                    LogPatchError($"Exception finding LeaveArmy patch target: {ex.Message}");
                    return null;
                }
            }

            [HarmonyPrefix]
            private static bool Prefix(ref bool __result, MenuCallbackArgs args)
            {
                if (TryGetEnlistmentService(out var enlistmentService) && enlistmentService.IsEnlisted)
                {
                    LogDecision("Suppressed army leave option - player is enlisted");
                    __result = false;
                    return false; // Skip original method execution
                }
                
                return true; // Execute original method
            }
        }

        // Patch for PlayerArmyWaitBehavior.wait_menu_army_abandon_on_condition (INSTANCE METHOD)
        // Signature verified: private bool wait_menu_army_abandon_on_condition(MenuCallbackArgs args)
        [HarmonyPatch]
        private static class AbandonArmyPatch
        {
            public static MethodBase TargetMethod()
            {
                try
                {
                    var type = AccessTools.TypeByName("TaleWorlds.CampaignSystem.CampaignBehaviors.PlayerArmyWaitBehavior");
                    if (type == null)
                    {
                        LogPatchError("Could not find PlayerArmyWaitBehavior type");
                        return null;
                    }

                    var method = AccessTools.Method(type, "wait_menu_army_abandon_on_condition", new[] { typeof(MenuCallbackArgs) });
                    if (method == null)
                    {
                        LogPatchError("Could not find wait_menu_army_abandon_on_condition method");
                        return null;
                    }

                    LogPatchSuccess("Successfully found PlayerArmyWaitBehavior.wait_menu_army_abandon_on_condition");
                    return method;
                }
                catch (Exception ex)
                {
                    LogPatchError($"Exception finding AbandonArmy patch target: {ex.Message}");
                    return null;
                }
            }

            [HarmonyPrefix]
            private static bool Prefix(ref bool __result, MenuCallbackArgs args)
            {
                if (TryGetEnlistmentService(out var enlistmentService) && enlistmentService.IsEnlisted)
                {
                    LogDecision("Suppressed army abandon option - player is enlisted");
                    __result = false;
                    return false; // Skip original method execution
                }
                
                return true; // Execute original method
            }
        }

        // Patch for VillageHostileActionCampaignBehavior.wait_menu_end_raiding_at_army_by_leaving_on_condition (STATIC METHOD)
        // Signature verified: private static bool wait_menu_end_raiding_at_army_by_leaving_on_condition(MenuCallbackArgs args)
        [HarmonyPatch]
        private static class RaidingLeaveArmyPatch
        {
            public static MethodBase TargetMethod()
            {
                try
                {
                    var type = AccessTools.TypeByName("TaleWorlds.CampaignSystem.CampaignBehaviors.VillageHostileActionCampaignBehavior");
                    if (type == null)
                    {
                        LogPatchError("Could not find VillageHostileActionCampaignBehavior type");
                        return null;
                    }

                    var method = AccessTools.Method(type, "wait_menu_end_raiding_at_army_by_leaving_on_condition", new[] { typeof(MenuCallbackArgs) });
                    if (method == null)
                    {
                        LogPatchError("Could not find wait_menu_end_raiding_at_army_by_leaving_on_condition method");
                        return null;
                    }

                    LogPatchSuccess("Successfully found VillageHostileActionCampaignBehavior.wait_menu_end_raiding_at_army_by_leaving_on_condition");
                    return method;
                }
                catch (Exception ex)
                {
                    LogPatchError($"Exception finding RaidingLeaveArmy patch target: {ex.Message}");
                    return null;
                }
            }

            [HarmonyPrefix]
            private static bool Prefix(ref bool __result, MenuCallbackArgs args)
            {
                if (TryGetEnlistmentService(out var enlistmentService) && enlistmentService.IsEnlisted)
                {
                    LogDecision("Suppressed raiding leave option - player is enlisted");
                    __result = false;
                    return false; // Skip original method execution
                }
                
                return true; // Execute original method
            }
        }

        // Patch for VillageHostileActionCampaignBehavior.wait_menu_end_raiding_at_army_by_abandoning_on_condition (STATIC METHOD)
        // Signature verified: private static bool wait_menu_end_raiding_at_army_by_abandoning_on_condition(MenuCallbackArgs args)
        [HarmonyPatch]
        private static class RaidingAbandonArmyPatch
        {
            public static MethodBase TargetMethod()
            {
                try
                {
                    var type = AccessTools.TypeByName("TaleWorlds.CampaignSystem.CampaignBehaviors.VillageHostileActionCampaignBehavior");
                    if (type == null)
                    {
                        LogPatchError("Could not find VillageHostileActionCampaignBehavior type");
                        return null;
                    }

                    var method = AccessTools.Method(type, "wait_menu_end_raiding_at_army_by_abandoning_on_condition", new[] { typeof(MenuCallbackArgs) });
                    if (method == null)
                    {
                        LogPatchError("Could not find wait_menu_end_raiding_at_army_by_abandoning_on_condition method");
                        return null;
                    }

                    LogPatchSuccess("Successfully found VillageHostileActionCampaignBehavior.wait_menu_end_raiding_at_army_by_abandoning_on_condition");
                    return method;
                }
                catch (Exception ex)
                {
                    LogPatchError($"Exception finding RaidingAbandonArmy patch target: {ex.Message}");
                    return null;
                }
            }

            [HarmonyPrefix]
            private static bool Prefix(ref bool __result, MenuCallbackArgs args)
            {
                if (TryGetEnlistmentService(out var enlistmentService) && enlistmentService.IsEnlisted)
                {
                    LogDecision("Suppressed raiding abandon option - player is enlisted");
                    __result = false;
                    return false; // Skip original method execution
                }
                
                return true; // Execute original method
            }
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
            if (_logger != null)
            {
                _logger.LogDebug(LogCategories.GameAdapters, decision);
            }
            else
            {
                // Fallback during transition period
                Debug.Print($"[Enlisted] {decision}");
            }
        }

        /// <summary>
        /// Log patch success using centralized logging service.
        /// </summary>
        private static void LogPatchSuccess(string message)
        {
            if (_logger != null)
            {
                _logger.LogInfo(LogCategories.GameAdapters, message);
            }
            else
            {
                Debug.Print($"[Enlisted] PATCH SUCCESS: {message}");
            }
        }

        /// <summary>
        /// Log patch errors using centralized logging service.
        /// </summary>
        private static void LogPatchError(string message)
        {
            if (_logger != null)
            {
                _logger.LogError(LogCategories.GameAdapters, message, null);
            }
            else
            {
                Debug.Print($"[Enlisted] PATCH ERROR: {message}");
            }
        }
    }
}
