using HarmonyLib;
using System;
using System.Reflection;
using SandBox.ViewModelCollection.Nameplate;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;
using Enlisted.Features.Enlistment.Application;
using Enlisted.Core.Logging;
using Enlisted.Core.DependencyInjection;

namespace Enlisted.GameAdapters.Patches
{
    // Harmony Patch
    // Target: SandBox.ViewModelCollection.Nameplate.PartyNameplateVM.RefreshBinding()
    // Why: Hide main party nameplate while enlisted to maintain the illusion of being part of the commander's party
    // Safety: Campaign/UI only; affect only MainParty; restores defaults when not enlisted
    // Notes: Logs at Debug; minimal object access; no allocations
    [HarmonyPatch]
    public class HidePartyNamePlatePatch
    {
        private static ILoggingService _logger;

        static HidePartyNamePlatePatch()
        {
            // Try to get logger during static initialization
            ServiceLocator.TryGetService<ILoggingService>(out _logger);
        }

        public static MethodBase TargetMethod()
        {
            try
            {
                var type = typeof(PartyNameplateVM);
                if (type == null)
                {
                    LogPatchError("Could not find PartyNameplateVM type");
                    return null;
                }

                var method = AccessTools.Method(type, "RefreshBinding");
                if (method == null)
                {
                    LogPatchError("Could not find RefreshBinding method");
                    return null;
                }

                LogPatchSuccess("Successfully found PartyNameplateVM.RefreshBinding");
                return method;
            }
            catch (Exception ex)
            {
                LogPatchError($"Exception finding HidePartyNamePlate patch target: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Intercepts nameplate refresh to hide main party indicators when enlisted.
        /// Modifies UI visibility flags to maintain enlistment illusion.
        /// Only affects main party nameplate, preserves other party UI elements.
        /// </summary>
        [HarmonyPostfix]
        private static void Postfix(PartyNameplateVM __instance)
        {
            try
            {
                if (__instance?.Party == MobileParty.MainParty)
                {
                    // Get enlistment service through dependency injection
                    if (TryGetEnlistmentService(out var enlistmentService) && enlistmentService.IsEnlisted)
                    {
                        // Hide main party indicators to maintain illusion
                        __instance.IsMainParty = false;
                        __instance.IsVisibleOnMap = false;
                        
                        LogNameplateHiding();
                    }
                    else
                    {
                        // Restore normal main party UI when not enlisted
                        __instance.IsMainParty = true;
                    }
                }
            }
            catch (Exception ex)
            {
                LogPatchError($"Exception in HidePartyNamePlate patch: {ex.Message}");
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
        /// Log nameplate hiding using centralized logging service.
        /// </summary>
        private static void LogNameplateHiding()
        {
            if (_logger != null)
            {
                _logger.LogDebug(LogCategories.GameAdapters, "Hidden main party nameplate to maintain enlistment illusion");
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
