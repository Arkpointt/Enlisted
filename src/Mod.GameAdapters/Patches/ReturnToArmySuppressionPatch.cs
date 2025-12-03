using System;
using System.Diagnostics.CodeAnalysis;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Mod.GameAdapters.Patches
{
    /// <summary>
    ///     Harmony patch that suppresses the "Return to Army" menu option in settlements.
    ///     This option is redundant for enlisted players as the mod provides its own "Return to camp" option.
    /// </summary>
    // ReSharper disable once UnusedType.Global - Harmony patch class discovered via reflection
    [SuppressMessage("ReSharper", "UnusedType.Global", Justification = "Harmony patch class discovered via reflection")]
    [HarmonyPatch(typeof(PlayerTownVisitCampaignBehavior), "game_menu_return_to_army_on_condition")]
    public static class ReturnToArmySuppressionPatch
    {
        /// <summary>
        ///     Postfix that runs after the native condition check.
        ///     Hides the "Return to Army" option if player is enlisted.
        /// </summary>
        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Called by Harmony via reflection")]
        [SuppressMessage("ReSharper", "InconsistentNaming",
            Justification = "Harmony convention: __result is a special injected parameter")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local",
            Justification = "Harmony requires matching method signature")]
        private static void Postfix(ref bool __result, MenuCallbackArgs args)
        {
            try
            {
                // Only modify if native would show the button
                if (!__result)
                {
                    return;
                }

                var enlistment = EnlistmentBehavior.Instance;

                // Hide if enlisted (regardless of leave state, as we want to use the mod's Return to Camp)
                // The user specifically said it's redundant because they have "Return to Camp".
                if (enlistment?.IsEnlisted == true)
                {
                    __result = false;
                    ModLogger.Debug("Interface",
                        "Hiding native 'Return to Army' button - redundant for enlisted player");
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", $"Error in ReturnToArmySuppressionPatch: {ex.Message}");
                // Fail open - allow native behavior on error
            }
        }
    }
}
