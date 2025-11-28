using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Mod.GameAdapters.Patches
{
    /// <summary>
    /// Prevents enlisted soldiers from participating in kingdom decisions and voting.
    /// Uses a two-layer approach:
    /// 1. Primary: Marks player as non-participant at the logic level
    /// 2. Backup: Blocks kingdom menus and notifications at the UI level
    /// </summary>
    public static class KingdomDecisionParticipationPatch
    {
        private static bool ShouldSuppressParticipation()
        {
            try
            {
                // Skip during character creation when campaign isn't initialized
                if (Campaign.Current == null)
                {
                    return false;
                }
                
                var enlistment = EnlistmentBehavior.Instance;
                return enlistment?.IsEmbeddedWithLord() == true && enlistment?.IsOnLeave != true;
            }
            catch (Exception ex)
            {
                ModLogger.Error("Voting", $"Error evaluating suppression state: {ex.Message}");
                return false;
            }
        }

        #region Primary Patches - Logic Level

        /// <summary>
        /// Marks the player as non-participant in kingdom decisions.
        /// </summary>
        [HarmonyPatch(typeof(KingdomDecision), "get_IsPlayerParticipant")]
        private static class IsPlayerParticipantPatch
        {
            private static void Postfix(ref bool __result)
            {
                if (__result && ShouldSuppressParticipation())
                {
                    __result = false;
                    ModLogger.Debug("Voting", "Suppressed player participation flag while enlisted.");
                }
            }
        }

        /// <summary>
        /// Removes player clan from the list of decision supporters.
        /// </summary>
        [HarmonyPatch(typeof(KingdomDecision), nameof(KingdomDecision.DetermineSupporters))]
        private static class DetermineSupportersPatch
        {
            private static void Postfix(ref IEnumerable<Supporter> __result)
            {
                if (!ShouldSuppressParticipation() || __result == null)
                {
                    return;
                }
                
                // Skip during character creation when player clan isn't initialized
                if (Clan.PlayerClan == null)
                {
                    return;
                }

                __result = __result.Where(s => s?.Clan != Clan.PlayerClan);
            }
        }

        #endregion

        #region Backup Patches - UI Level

        /// <summary>
        /// Blocks kingdom decision menus from appearing when enlisted.
        /// </summary>
        [HarmonyPatch(typeof(GameMenu), "ActivateGameMenu")]
        public class KingdomDecisionMenuPatch
        {
            static bool Prefix(string menuId)
            {
                try
                {
                    if (!ShouldSuppressParticipation())
                    {
                        return true;
                    }

                    if (string.IsNullOrEmpty(menuId))
                    {
                        return true;
                    }

                    // Suppress kingdom decision menus
                    if (menuId.Contains("kingdom_decision") || 
                        menuId.Contains("kingdom_policy") ||
                        menuId.Contains("fief_assignment") ||
                        menuId.Contains("kingdom_voting") ||
                        menuId == "kingdom_decision" ||
                        menuId.StartsWith("kingdom_"))
                    {
                        ModLogger.Debug("Voting", $"Suppressed kingdom decision menu: {menuId}");
                        return false;
                    }

                    // Suppress faction join menus
                    if (menuId.Contains("clan_join") || 
                        menuId.Contains("faction_join") ||
                        menuId == "clan_joined_kingdom")
                    {
                        ModLogger.Debug("Voting", $"Suppressed faction join menu: {menuId}");
                        return false;
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    ModLogger.Error("Voting", $"Error in menu suppression patch: {ex.Message}");
                    return true;
                }
            }
        }

        /// <summary>
        /// Blocks kingdom decision map notifications when enlisted.
        /// </summary>
        [HarmonyPatch(typeof(CampaignInformationManager), "NewMapNoticeAdded")]
        public class KingdomDecisionNotificationPatch
        {
            static bool Prefix(InformationData informationData)
            {
                try
                {
                    if (!ShouldSuppressParticipation())
                    {
                        return true;
                    }

                    if (informationData != null)
                    {
                        var notificationType = informationData.GetType();
                        if (notificationType.Name == "KingdomDecisionMapNotification" || 
                            notificationType.FullName?.Contains("KingdomDecisionMapNotification") == true)
                        {
                            ModLogger.Debug("Voting", "Suppressed kingdom decision map notification");
                            return false;
                        }
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    ModLogger.Error("Voting", $"Error in notification suppression patch: {ex.Message}");
                    return true;
                }
            }
        }

        #endregion
    }
}
