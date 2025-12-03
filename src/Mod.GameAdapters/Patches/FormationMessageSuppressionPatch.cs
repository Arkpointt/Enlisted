using Enlisted.Features.Enlistment.Behaviors;
using HarmonyLib;
using TaleWorlds.MountAndBlade;

namespace Enlisted.Mod.GameAdapters.Patches
{
    /// <summary>
    ///     Suppresses formation behavior notification messages for enlisted soldiers.
    ///     The vanilla game shows messages when formation behaviors change, but the soldier
    ///     notification code uses this.ToString() which outputs the full type name like
    ///     "TaleWorlds.MountAndBlade.BehaviorGeneral" instead of a localized message.
    ///     The vanilla code tries to strip "MBModule.Behavior" prefix but the actual classes
    ///     use "TaleWorlds.MountAndBlade" namespace, so the full class name gets displayed:
    ///     "Men! TaleWorlds.MountAndBlade.BehaviorGeneral!"
    ///     Since enlisted soldiers are regular troops following orders (not commanding formations),
    ///     we skip these behavior notifications entirely when the player is enlisted.
    /// </summary>

    // Harmony Patch
    // Target: TaleWorlds.MountAndBlade.BehaviorComponent.OnBehaviorActivated()
    // Why: Suppress broken localization messages for enlisted soldiers
    // Safety: Only affects enlisted players; vanilla behavior preserved for non-enlisted
    [HarmonyPatch(typeof(BehaviorComponent), "OnBehaviorActivated")]
    public class FormationMessageSuppressionPatch
    {
        /// <summary>
        ///     Prefix that skips the behavior activation notifications when player is enlisted.
        ///     Enlisted soldiers are regular troops - they don't need formation command messages
        ///     that are meant for players commanding formations.
        ///     Called by Harmony via reflection.
        /// </summary>
        [HarmonyPrefix]
        public static bool Prefix()
        {
            // Enlisted soldiers are regular troops - suppress formation behavior messages
            if (EnlistmentBehavior.Instance?.IsEnlisted == true)
            {
                return false; // Skip original method entirely
            }

            return true; // Let vanilla run for non-enlisted players
        }
    }
}
