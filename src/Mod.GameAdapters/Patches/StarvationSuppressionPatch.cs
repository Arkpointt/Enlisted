using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Mod.GameAdapters.Patches
{
    /// <summary>
    /// Suppresses starvation penalties for the player's party when enlisted.
    /// When enlisted, the player is embedded with their lord's party and shares
    /// the lord's food supplies. The player's invisible party shouldn't be subject
    /// to separate starvation checks.
    /// </summary>
    [HarmonyPatch(typeof(PartyBase), nameof(PartyBase.IsStarving), MethodType.Getter)]
    public static class StarvationSuppressionPatch
    {
        /// <summary>
        /// Postfix that overrides IsStarving to return false when the player is enlisted.
        /// The lord handles all food supplies - the soldier doesn't starve separately.
        /// </summary>
        static void Postfix(PartyBase __instance, ref bool __result)
        {
            try
            {
                // Only modify for the player's main party
                if (__instance != PartyBase.MainParty)
                {
                    return;
                }

                // If already not starving, nothing to do
                if (!__result)
                {
                    return;
                }

                var enlistment = EnlistmentBehavior.Instance;

                // When enlisted (not on leave), suppress starvation - lord provides food
                // Note: IsEnlisted already returns false when on leave, so no need to check IsOnLeave separately
                if (enlistment?.IsEnlisted == true)
                {
                    __result = false;
                    // Only log occasionally to avoid spam
                    if (CampaignTime.Now.GetDayOfSeason % 7 == 0)
                    {
                        ModLogger.Debug("Starvation", "Suppressed starvation for enlisted player - lord provides food");
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Starvation", $"Error in starvation suppression patch: {ex.Message}");
                // Fail open - don't modify result on error
            }
        }
    }
}

