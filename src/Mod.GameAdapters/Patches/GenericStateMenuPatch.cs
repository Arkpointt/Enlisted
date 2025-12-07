using System;
using System.Diagnostics.CodeAnalysis;
using Enlisted.Features.Combat.Behaviors;
using Enlisted.Mod.Core;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Mod.GameAdapters.Patches
{
    /// <summary>
    ///     Patches the native GetGenericStateMenu() to return our custom menu when player is in reserve.
    ///     Root cause: When in reserve mode, the native system thinks player should be in "army_wait"
    ///     because mainParty.AttachedTo != null. Various native systems call GetGenericStateMenu() and
    ///     switch menus accordingly, causing visual stutter as menus flip between army_wait and enlisted_battle_wait.
    ///     Fix: If player is waiting in reserve, override the result to return "enlisted_battle_wait".
    ///     This prevents ALL native systems from trying to switch away from our menu.
    /// </summary>
    [HarmonyPatch(typeof(DefaultEncounterGameMenuModel), nameof(DefaultEncounterGameMenuModel.GetGenericStateMenu))]
    [SuppressMessage("ReSharper", "UnusedType.Global", Justification = "Harmony patch - applied via attribute")]
    public class GenericStateMenuPatch
    {
        /// <summary>
        ///     Postfix that overrides the result when player is waiting in reserve.
        ///     Returns "enlisted_battle_wait" instead of "army_wait" to prevent menu switching stutter.
        /// </summary>
        [HarmonyPostfix]
        [SuppressMessage("ReSharper", "InconsistentNaming",
            Justification = "Harmony convention: __result is a special injected parameter")]
        [SuppressMessage("CodeQuality", "IDE0051", Justification = "Called by Harmony via reflection")]
        [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Called by Harmony via reflection")]
        public static void Postfix(ref string __result)
        {
            try
            {
                // Check activation gate first - mod may be disabled
                if (!EnlistedActivation.EnsureActive())
                {
                    return;
                }

                // Only intercept when player is waiting in reserve
                if (!EnlistedEncounterBehavior.IsWaitingInReserve)
                {
                    return;
                }

                // When in reserve, the native system returns "army_wait" because the player is attached
                // to the army leader. Override this to return our custom menu instead.
                // This prevents ALL native menu switching systems from interfering.
                if (__result == "army_wait" || __result == "army_wait_at_settlement" || __result == "encounter")
                {
                    __result = "enlisted_battle_wait";
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("GenericStateMenuPatch", $"Error in GetGenericStateMenu patch: {ex.Message}");
            }
        }
    }
}
