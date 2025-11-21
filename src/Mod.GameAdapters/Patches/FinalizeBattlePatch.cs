using HarmonyLib;
using SandBox.CampaignBehaviors;
using TaleWorlds.CampaignSystem.MapEvents;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Mod.GameAdapters.Patches
{
    /// <summary>
    /// Harmony patch that hooks into battle end events to allow custom behavior for enlisted soldiers.
    /// Intercepts DefaultCutscenesCampaignBehavior.OnMapEventEnd to control what happens when battles end.
    /// Currently allows normal behavior but provides a hook for future enhancements.
    /// </summary>
    [HarmonyPatch(typeof(DefaultCutscenesCampaignBehavior), "OnMapEventEnd")]
    public class FinalizeBattlePatch
    {
        /// <summary>
        /// Prefix method that runs before DefaultCutscenesCampaignBehavior.OnMapEventEnd.
        /// Checks if the player is enlisted and allows custom battle end behavior if needed.
        /// </summary>
        /// <param name="mapEvent">The map event (battle) that just ended.</param>
        /// <returns>True to allow normal behavior, false to prevent it.</returns>
        static bool Prefix(MapEvent mapEvent)
        {
            try
            {
                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment?.IsEnlisted == true)
                {
                    // Control battle end behavior when the player is enlisted
                    // This allows customizing what happens when battles end for enlisted soldiers
                    // For now, we allow normal behavior but this provides a hook for future enhancements
                    return true;
                }
                
                return true; // Allow normal behavior when not enlisted
            }
            catch (System.Exception ex)
            {
                ModLogger.Error("FinalizeBattle", "Error in finalize battle patch", ex);
                return true; // Allow normal behavior on error
            }
        }
    }
}
