using HarmonyLib;
using SandBox.ViewModelCollection.Nameplate;
using TaleWorlds.CampaignSystem.Party;
using Enlisted.Features.Enlistment.Application;
using Enlisted.Core.Logging;
using Enlisted.Core.DependencyInjection;

namespace Enlisted.GameAdapters.Patches
{
    // Harmony Patch
    // Target: SandBox.ViewModelCollection.Nameplate.PartyNameplateVM.RefreshBinding()
    // Why: Hide main party nameplate while enlisted to maintain the illusion of being part of the commander’s party
    // Safety: Campaign/UI only; affect only MainParty; restores defaults when not enlisted
    // Notes: Logs at Debug; minimal object access; no allocations
    [HarmonyPatch(typeof(PartyNameplateVM), "RefreshBinding")]
    public class HidePartyNamePlatePatch
    {
        /// <summary>
        /// Intercepts nameplate refresh to hide main party indicators when enlisted.
        /// Modifies UI visibility flags to maintain enlistment illusion.
        /// Only affects main party nameplate, preserves other party UI elements.
        /// </summary>
        private static void Postfix(PartyNameplateVM __instance)
        {
            if (__instance.Party == MobileParty.MainParty)
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
            if (ServiceLocator.TryGetService<ILoggingService>(out var logger))
            {
                logger.LogDebug(LogCategories.GameAdapters, "Hidden main party nameplate to maintain enlistment illusion");
            }
        }
    }
}
