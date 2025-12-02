using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Mod.GameAdapters.Patches
{
    /// <summary>
    /// Suppresses native mercenary income when the player is enlisted.
    ///
    /// When enlisting, players join a kingdom as mercenaries (using ApplyByJoinFactionAsMercenary)
    /// with awardMultiplier=0 to prevent native mercenary wages. However, the game recalculates
    /// MercenaryAwardMultiplier every 30 days in ClanVariablesCampaignBehavior.DailyTickClan,
    /// which would restore native mercenary income.
    ///
    /// This patch intercepts the AddMercenaryIncome method to completely suppress mercenary
    /// income when enlisted, ensuring players only receive wages from the mod's system.
    /// </summary>
    [HarmonyPatch(typeof(DefaultClanFinanceModel), "AddMercenaryIncome")]
    internal static class MercenaryIncomeSuppressionPatch
    {
        /// <summary>
        /// Prefix patch that skips native mercenary income calculation when the player is enlisted.
        /// Returns false to skip the original method entirely when enlisted.
        /// </summary>
        [HarmonyPrefix]
        private static bool Prefix(Clan clan, ref ExplainedNumber goldChange, bool applyWithdrawals)
        {
            try
            {
                // Only intercept for player clan
                if (Campaign.Current == null || Clan.PlayerClan == null || clan != Clan.PlayerClan)
                {
                    return true; // Allow normal processing for other clans
                }

                var enlistment = EnlistmentBehavior.Instance;
                var isEnlisted = enlistment?.IsEnlisted == true;
                var isUnderMercenaryService = Clan.PlayerClan.IsUnderMercenaryService;
                var awardMultiplier = Clan.PlayerClan.MercenaryAwardMultiplier;

                if (ModLogger.IsEnabled("Finance", LogLevel.Debug))
                {
                    ModLogger.Debug("Finance", $"MercenaryIncome check: isEnlisted={isEnlisted}, isMercenary={isUnderMercenaryService}, awardMultiplier={awardMultiplier}, applyWithdrawals={applyWithdrawals}, currentGold={goldChange.ResultNumber}");
                }

                if (!isEnlisted)
                {
                    return true; // Not enlisted - allow normal mercenary income
                }

                // Player is enlisted - suppress native mercenary income entirely
                // The mod's wage system (ClanFinanceEnlistmentIncomePatch) handles compensation
                ModLogger.Debug("Finance", "Suppressed native mercenary income - enlisted soldiers receive mod wages only");
                return false; // Skip the original AddMercenaryIncome method
            }
            catch (Exception ex)
            {
                ModLogger.Error("Finance", "Error in mercenary income suppression", ex);
                return true; // Fail open - allow normal behavior on error
            }
        }
    }

    /// <summary>
    /// Additional patch to prevent the MercenaryAwardMultiplier from being recalculated
    /// every 30 days while enlisted. This ensures the multiplier stays at 0.
    ///
    /// Without this patch, ClanVariablesCampaignBehavior.DailyTickClan would recalculate
    /// the multiplier based on clan strength, potentially giving non-zero values.
    /// </summary>
    [HarmonyPatch(typeof(Clan), nameof(Clan.MercenaryAwardMultiplier), MethodType.Setter)]
    internal static class MercenaryAwardMultiplierSuppressionPatch
    {
        /// <summary>
        /// Prefix patch that prevents setting MercenaryAwardMultiplier to non-zero values
        /// when the player clan is enlisted.
        /// </summary>
        [HarmonyPrefix]
        private static bool Prefix(Clan __instance, ref int value)
        {
            try
            {
                // Safety checks for early game initialization
                // Campaign.Current and Clan.PlayerClan can be null during load
                if (Campaign.Current == null)
                {
                    return true;
                }

                // PlayerClan can be null during early initialization
                var playerClan = Clan.PlayerClan;
                if (playerClan == null || __instance != playerClan)
                {
                    return true; // Allow normal processing for other clans or during init
                }

                // EnlistmentBehavior may not be registered yet during game load
                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment == null)
                {
                    return true; // Behavior not ready yet
                }

                var isEnlisted = enlistment.IsEnlisted;

                if (ModLogger.IsEnabled("Finance", LogLevel.Debug))
                {
                    ModLogger.Debug("Finance", $"MercenaryAwardMultiplier setter: isEnlisted={isEnlisted}, newValue={value}");
                }

                if (!isEnlisted)
                {
                    return true; // Not enlisted - allow normal multiplier changes
                }

                // Player is enlisted - force multiplier to 0
                if (value != 0)
                {
                    if (ModLogger.IsEnabled("Finance", LogLevel.Debug))
                    {
                        ModLogger.Debug("Finance", $"Blocked MercenaryAwardMultiplier change to {value} while enlisted - forcing to 0");
                    }
                    value = 0;
                }
                return true; // Continue with the (now zeroed) value
            }
            catch (Exception ex)
            {
                ModLogger.Error("Finance", "Error in mercenary multiplier suppression", ex);
                return true; // Fail open - allow normal behavior on error
            }
        }
    }
}

