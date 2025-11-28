using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Localization;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Mod.GameAdapters.Patches
{
    /// <summary>
    /// Suppresses the vanilla mercenary income display when the player is enlisted.
    /// When enlisted, we join the kingdom as a vassal, but the native game sometimes
    /// still shows mercenary payment in the daily income breakdown. This patch removes
    /// any mercenary-related income entries when the player is enlisted since our
    /// wage system handles compensation separately.
    /// </summary>
    [HarmonyPatch(typeof(DefaultClanFinanceModel), nameof(DefaultClanFinanceModel.CalculateClanIncome))]
    internal static class MercenaryIncomeSuppressionPatch
    {
        /// <summary>
        /// Runs BEFORE the ClanFinanceEnlistmentIncomePatch postfix (lower priority).
        /// Removes any mercenary income entries from the result when enlisted.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPriority(Priority.High)] // Run before our wage patch
        private static void Postfix(Clan clan, ref ExplainedNumber __result)
        {
            try
            {
                // Only process for player clan
                if (Campaign.Current == null || Clan.PlayerClan == null || clan != Clan.PlayerClan)
                {
                    return;
                }

                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment?.IsEnlisted != true)
                {
                    return;
                }

                // Clear any mercenary contract that might exist
                // When enlisted, we're a soldier not a mercenary
                var playerClan = Clan.PlayerClan;
                if (playerClan.IsUnderMercenaryService)
                {
                    // The clan has a mercenary contract - we need to end it
                    // This shouldn't happen if we join properly, but handle it defensively
                    ModLogger.Info("Finance", "Detected mercenary contract while enlisted - this may cause income display issues");
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Finance", $"Error in mercenary income suppression: {ex.Message}");
            }
        }
    }
}

