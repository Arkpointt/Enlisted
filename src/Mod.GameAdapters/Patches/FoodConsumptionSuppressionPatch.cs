using System;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Mod.GameAdapters.Patches
{
    /// <summary>
    /// Suppresses food consumption for the player's party when enlisted.
    ///
    /// When enlisted, the player is embedded with their lord's party and shares
    /// the lord's food supplies. The player's party shouldn't consume food separately
    /// because:
    /// 1. Troops were transferred to the lord's party at enlistment start
    /// 2. The player is conceptually eating from the lord's provisions
    /// 3. This prevents food borrowing in armies (which causes influence loss)
    /// 4. This is thematically correct - a soldier doesn't manage their own food logistics
    ///
    /// By returning false from DoesPartyConsumeFood, we completely skip:
    /// - Food consumption calculations
    /// - Starvation checks (also handled by StarvationSuppressionPatch as backup)
    /// - Food borrowing from army members
    /// - Influence loss from food borrowing
    /// </summary>
    [HarmonyPatch(typeof(DefaultMobilePartyFoodConsumptionModel), nameof(DefaultMobilePartyFoodConsumptionModel.DoesPartyConsumeFood))]
    internal static class FoodConsumptionSuppressionPatch
    {
        /// <summary>
        /// Postfix patch that overrides DoesPartyConsumeFood to return false when enlisted.
        /// This completely skips food consumption for the enlisted player's party.
        /// </summary>
        [HarmonyPostfix]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static void Postfix(MobileParty mobileParty, ref bool __result)
        {
            try
            {
                // Only modify for the player's main party
                if (mobileParty != MobileParty.MainParty)
                {
                    return;
                }

                // If already not consuming food, nothing to do
                if (!__result)
                {
                    return;
                }

                var enlistment = EnlistmentBehavior.Instance;

                // When enlisted (not on leave), skip food consumption - lord provides food
                // Note: IsEnlisted already returns false when on leave, so no need to check IsOnLeave separately
                if (enlistment?.IsEnlisted == true)
                {
                    __result = false;
                    ModLogger.Debug("Food", "Enlisted player party skipping food consumption - lord provides supplies");
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Food", $"Error in food consumption suppression patch: {ex.Message}");
                // Fail open - don't modify result on error
            }
        }
    }
}
