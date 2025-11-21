using HarmonyLib;
using SandBox.Missions.MissionLogics;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Mod.GameAdapters.Patches
{
    /// <summary>
    /// Harmony patch that prevents enlisted soldiers from leaving battles early.
    /// Intercepts MissionFightHandler.OnEndMissionRequest to prevent mission abandonment
    /// when the player is enlisted, ensuring they cannot abandon their lord during battles.
    /// </summary>
    [HarmonyPatch(typeof(MissionFightHandler), "OnEndMissionRequest")]
    public class MissionFightEndPatch
    {
        /// <summary>
        /// Prefix method that runs before MissionFightHandler.OnEndMissionRequest.
        /// Prevents enlisted players from leaving battles early by blocking mission end requests.
        /// </summary>
        /// <param name="canPlayerLeave">Output parameter indicating whether the player can leave.</param>
        /// <returns>False to prevent mission end, true to allow it.</returns>
        static bool Prefix(out bool canPlayerLeave)
        {
            canPlayerLeave = true;
            
            try
            {
                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment?.IsEnlisted == true)
                {
                    // Prevent leaving missions when enlisted
                    // This ensures enlisted soldiers cannot abandon their lord during battles
                    canPlayerLeave = false;
                    return false; // Prevent mission end
                }
                
                return true; // Allow normal behavior when not enlisted
            }
            catch (System.Exception ex)
            {
                ModLogger.Error("MissionFightEnd", "Error in mission fight end patch", ex);
                return true; // Allow normal behavior on error
            }
        }
    }
}
