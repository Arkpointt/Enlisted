using HarmonyLib;
using SandBox.ViewModelCollection.Nameplate;
using TaleWorlds.CampaignSystem.Party;
using Enlisted.Features.Enlistment.Application;
using Enlisted.Core.Logging;
using Enlisted.Core.DependencyInjection;

namespace Enlisted.GameAdapters.Patches
{
    /// <summary>
    /// Game adapter that hides main party nameplate UI when enlisted.
    /// Isolates TaleWorlds UI system interactions from domain logic per blueprint.
    /// 
    /// Updated to use dependency injection (ADR-004) and centralized logging.
    /// Reinforces the party illusion by suppressing main party visual indicators
    /// in the game's nameplate system. This prevents the UI from contradicting
    /// the narrative that the player is part of the commander's party.
    /// </summary>
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
