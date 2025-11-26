using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Mod.GameAdapters.Patches
{
	/// <summary>
	/// Backup patches to suppress kingdom decision menus and notifications for enlisted soldiers.
	/// The primary suppression is handled by KingdomDecisionParticipationPatch (IsPlayerParticipant).
	/// These patches provide belt-and-suspenders protection at the menu/notification level.
	/// </summary>
	public static class VotingSuppressionPatches
	{
		private static bool ShouldSuppressPrompts()
		{
			try
			{
				// Skip during character creation when campaign isn't initialized
				if (Campaign.Current == null)
				{
					return false;
				}
				
				var enlistment = EnlistmentBehavior.Instance;
				return enlistment?.IsEmbeddedWithLord() == true;
			}
			catch (Exception ex)
			{
				ModLogger.Error("Voting", $"Error evaluating suppression state: {ex.Message}");
				return false;
			}
		}

		/// <summary>
		/// Backup patch to intercept and suppress kingdom decision menus when the player is enlisted.
		/// Primary suppression is via KingdomDecisionParticipationPatch.IsPlayerParticipant.
		/// </summary>
		[HarmonyPatch(typeof(GameMenu), "ActivateGameMenu")]
		public class KingdomDecisionMenuPatch
		{
			static bool Prefix(string menuId)
			{
				try
				{
					if (!ShouldSuppressPrompts())
					{
						return true;
					}

					if (string.IsNullOrEmpty(menuId))
					{
						return true;
					}

					// Suppress loot menu for low-tier enlisted soldiers
					if (menuId == "encounter_loot")
					{
						var enlistment = EnlistmentBehavior.Instance;
						if (enlistment?.IsEnlisted == true && enlistment.EnlistmentTier < 4)
						{
							ModLogger.Debug("LootRestriction", $"Blocked loot menu for enlisted tier {enlistment.EnlistmentTier}");
							return false;
						}
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
		/// Backup patch to suppress kingdom decision map notifications when the player is enlisted.
		/// </summary>
		[HarmonyPatch(typeof(CampaignInformationManager), "NewMapNoticeAdded")]
		public class KingdomDecisionNotificationPatch
		{
			static bool Prefix(InformationData informationData)
			{
				try
				{
					if (!ShouldSuppressPrompts())
					{
						return true;
					}

					if (informationData != null)
					{
						var notificationType = informationData.GetType();
						if (notificationType.Name == "KingdomDecisionMapNotification" || 
						    notificationType.FullName?.Contains("KingdomDecisionMapNotification") == true)
						{
							ModLogger.Debug("Voting", "Suppressed kingdom decision map notification for embedded enlisted player");
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
	}
}
