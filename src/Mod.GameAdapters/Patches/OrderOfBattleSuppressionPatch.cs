using System;
using HarmonyLib;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Mod.GameAdapters.Patches
{
    /// <summary>
    /// Prevents the Order of Battle deployment screen from appearing when the player is enlisted.
    /// As an enlisted soldier, the player should not have deployment control and should spawn
    /// automatically without the deployment screen, just like a regular soldier in an army.
    /// </summary>
    [HarmonyPatch(typeof(SandBox.GameComponents.SandboxBattleInitializationModel), "CanPlayerSideDeployWithOrderOfBattleAux")]
    public class OrderOfBattleSuppressionPatch
    {
        /// <summary>
        /// Prefix that runs before CanPlayerSideDeployWithOrderOfBattleAux.
        /// Returns false to prevent Order of Battle screen when player is enlisted.
        /// </summary>
        static bool Prefix(ref bool __result)
        {
            try
            {
                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment?.IsEnlisted == true)
                {
                    // Player is enlisted - skip Order of Battle screen
                    // They should spawn directly into battle as a regular soldier
                    __result = false;
                    ModLogger.LogOnce("oob_suppressed", "Battle", "Order of Battle screen suppressed - enlisted soldiers spawn directly into battle");
                    return false; // Skip original method
                }

                // Player not enlisted - allow normal behavior
                return true; // Continue to original method
            }
            catch (Exception ex)
            {
                ModLogger.Error("OrderOfBattle", $"Error in Order of Battle suppression patch: {ex.Message}");
                return true; // Fail open - allow normal behavior on error
            }
        }
    }
}

