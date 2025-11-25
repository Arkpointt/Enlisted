using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.Core;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Mod.GameAdapters.Patches
{
		/// <summary>
	/// Harmony patches that prevent enlisted soldiers from being prompted to vote on kingdom decisions.
	/// Enlisted soldiers are not vassals and should not participate in kingdom politics.
	/// </summary>
	public static class VotingSuppressionPatches
	{
		private static bool ShouldSuppressPrompts()
		{
			try
			{
				var enlistment = EnlistmentBehavior.Instance;
				return enlistment?.IsEmbeddedWithLord() == true;
			}
			catch (Exception ex)
			{
				ModLogger.Error("Voting", $"Error evaluating suppression state: {ex.Message}");
				return false;
			}
		}

		// Legacy kingdom decision participation patch removed in Bannerlord 1.2.12; participation is now suppressed
		// via KingdomDecisionParticipationPatch (see separate file).

		/// <summary>
		/// Patch to suppress the faction join menu that appears when joining a kingdom while enlisted.
		/// </summary>
		[HarmonyPatch]
		public class FactionJoinMenuPatch
		{
			static MethodBase TargetMethod()
			{
				try
				{
					// Try to find methods that show faction join menus
					// These are typically in campaign behaviors that handle clan kingdom changes
					var behaviorType = AccessTools.TypeByName("TaleWorlds.CampaignSystem.CampaignBehaviors.DefaultKingdomDecisionCampaignBehavior");
					if (behaviorType != null)
					{
						var method = AccessTools.Method(behaviorType, "OnClanChangedKingdom");
						if (method != null)
						{
							ModLogger.Info("Voting", "Found OnClanChangedKingdom method for menu suppression");
							return method;
						}
					}

					// Try to find menu activation methods
					var menuMethod = AccessTools.Method(typeof(GameMenu), "ActivateGameMenu", new[] { typeof(string) });
					if (menuMethod != null)
					{
						// We'll use a postfix to intercept menu activations
						ModLogger.Info("Voting", "Will use GameMenu.ActivateGameMenu postfix for menu suppression");
						return menuMethod;
					}

					ModLogger.Error("Voting", "Could not find faction join menu method to patch - will use menu interception instead");
					return null;
				}
				catch (Exception ex)
				{
					ModLogger.Error("Voting", $"Exception finding faction join menu method: {ex.Message}");
					return null;
				}
			}

			static bool Prefix()
			{
				try
				{
					// This will be handled by the menu interception patch instead
					return true;
				}
				catch
				{
					return true;
				}
			}
		}

		/// <summary>
		/// Patch to intercept and suppress kingdom decision menus when the player is enlisted.
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
						return true; // Not enlisted or not attached/with army - allow menus
					}

					// Suppress kingdom decision menus for enlisted soldiers who are embedded with their lord
					if (!string.IsNullOrEmpty(menuId))
					{
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

						// Common kingdom decision menu IDs
						if (menuId.Contains("kingdom_decision") || 
						    menuId.Contains("kingdom_policy") ||
						    menuId.Contains("fief_assignment") ||
						    menuId.Contains("kingdom_voting") ||
						    menuId == "kingdom_decision" ||
						    menuId.StartsWith("kingdom_"))
						{
							ModLogger.Debug("Voting", $"Suppressed kingdom decision menu: {menuId}");
							return false; // Prevent menu activation
						}

						// Suppress faction join menus
						if (menuId.Contains("clan_join") || 
						    menuId.Contains("faction_join") ||
						    menuId == "clan_joined_kingdom")
						{
							ModLogger.Debug("Voting", $"Suppressed faction join menu: {menuId}");
							return false; // Prevent menu activation
						}
					}

					return true; // Allow other menus
				}
				catch (Exception ex)
				{
					ModLogger.Error("Voting", $"Error in menu suppression patch: {ex.Message}");
					return true; // Fail open - allow menu on error
				}
			}
		}

		/// <summary>
		/// Patch to suppress kingdom decision prompts through campaign events.
		/// Intercepts the OnClanChangedKingdom event in DefaultCutscenesCampaignBehavior to prevent join kingdom scene notifications.
		/// </summary>
		[HarmonyPatch]
		public class KingdomDecisionEventPatch
		{
			static MethodBase TargetMethod()
			{
				try
				{
					// Based on decompiled code: SandBox/CampaignBehaviors/DefaultCutscenesCampaignBehavior.cs
					// Method signature: OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
					var behaviorType = AccessTools.TypeByName("TaleWorlds.CampaignSystem.CampaignBehaviors.DefaultCutscenesCampaignBehavior");
					if (behaviorType == null)
					{
						// Try SandBox namespace
						behaviorType = AccessTools.TypeByName("SandBox.CampaignBehaviors.DefaultCutscenesCampaignBehavior");
					}

					if (behaviorType == null)
					{
						ModLogger.Error("Voting", "Could not find DefaultCutscenesCampaignBehavior type");
						return null;
					}

					// Try to find the OnClanChangedKingdom method with the correct signature
					// Need to find ChangeKingdomAction.ChangeKingdomActionDetail type first
					var changeKingdomActionType = AccessTools.TypeByName("TaleWorlds.CampaignSystem.Actions.ChangeKingdomAction");
					var detailEnumType = changeKingdomActionType?.GetNestedType("ChangeKingdomActionDetail");

					if (detailEnumType != null)
					{
						var method = AccessTools.Method(behaviorType, "OnClanChangedKingdom", new[] { typeof(Clan), typeof(Kingdom), typeof(Kingdom), detailEnumType, typeof(bool) });
						if (method != null)
						{
							ModLogger.Info("Voting", "Found OnClanChangedKingdom event handler for patching");
							return method;
						}
					}

					// Try without the detail parameter
					var method2 = AccessTools.Method(behaviorType, "OnClanChangedKingdom", new[] { typeof(Clan), typeof(Kingdom), typeof(Kingdom), typeof(bool) });
					if (method2 != null)
					{
						ModLogger.Info("Voting", "Found OnClanChangedKingdom event handler (simplified signature) for patching");
						return method2;
					}

					// Try with just the basic signature
					var method3 = AccessTools.Method(behaviorType, "OnClanChangedKingdom");
					if (method3 != null)
					{
						ModLogger.Info("Voting", "Found OnClanChangedKingdom event handler (basic signature) for patching");
						return method3;
					}

					ModLogger.Error("Voting", "Could not find OnClanChangedKingdom method to patch");
					return null;
				}
				catch (Exception ex)
				{
					ModLogger.Error("Voting", $"Exception finding OnClanChangedKingdom method: {ex.Message}");
					return null;
				}
			}

			static bool Prefix(Clan clan, Kingdom oldKingdom, Kingdom newKingdom)
			{
				try
				{
					// Check if the player's clan is joining a kingdom
					if (clan != Clan.PlayerClan)
					{
						return true; // Not the player's clan - allow normal behavior
					}

					if (!ShouldSuppressPrompts())
					{
						return true;
					}

					string kingdomName = newKingdom?.Name?.ToString() ?? "kingdom";
					ModLogger.Info("Voting", $"Suppressed join kingdom scene notification for embedded enlisted player joining {kingdomName}");
					return false;
				}
				catch (Exception ex)
				{
					ModLogger.Error("Voting", $"Error in kingdom decision event patch: {ex.Message}");
					return true; // Fail open - allow normal behavior on error
				}
			}
		}

		/// <summary>
		/// Patch to suppress kingdom decision map notifications when the player is enlisted.
		/// Based on decompiled DefaultLogsCampaignBehavior which creates KingdomDecisionMapNotification.
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
							return false; // Prevent notification from being added
						}
					}

					return true; // Allow other notifications
				}
				catch (Exception ex)
				{
					ModLogger.Error("Voting", $"Error in kingdom decision notification patch: {ex.Message}");
					return true; // Fail open - allow notification on error
				}
			}
		}
	}
}

