using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Mod.GameAdapters.Patches
{
	/// <summary>
	/// Restricts loot access for enlisted soldiers based on their tier/rank.
	/// Non-officers (tier < 4) get NO loot access.
	/// Officers (tier >= 4) get limited loot based on tier:
	/// - Tier 4: 10% of normal loot
	/// - Tier 5: 20% of normal loot
	/// - Tier 6: 30% of normal loot
	/// 
	/// This enforces realistic military hierarchy where soldiers don't get full loot rights
	/// until they reach officer status, and officers receive limited shares based on rank.
	/// </summary>
	public static class LootRestrictionPatches
	{
		/// <summary>
		/// Blocks the loot menu from appearing for non-officer enlisted soldiers (tier < 4).
		/// Officers (tier >= 4) will see the loot menu but with reduced amounts.
		/// </summary>
		[HarmonyPatch(typeof(PlayerEncounter), "DoLootParty")]
		public class PlayerLootMenuPatch
		{
			static bool Prefix(PlayerEncounter __instance)
			{
				try
				{
					var enlistment = EnlistmentBehavior.Instance;
					if (enlistment?.IsEnlisted != true)
					{
						return true; // Not enlisted - allow normal loot menu
					}

					var tier = enlistment.EnlistmentTier;
					
					// Tier 4+ (officers) get loot menu but with reduced amounts
					// The amount reduction is handled by LootAmountPatch
					if (tier >= 4)
					{
						ModLogger.Debug("LootRestriction", $"Officer tier {tier} - allowing loot menu with reduced amount");
						return true; // Allow loot menu to open
					}
					
					// Non-officers (tier < 4) get NO loot access
					ModLogger.Info("LootRestriction", $"Non-officer tier {tier} - blocking loot menu completely");
					
					// Skip the loot state and move to next encounter state
					// This prevents the loot menu from appearing
					// Use reflection to set the internal _mapEventState field (EncounterState is read-only)
					var mapEventStateField = typeof(PlayerEncounter).GetField("_mapEventState",
						BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
					if (mapEventStateField != null)
					{
						// Switch directly to the inventory phase so the loot menu is skipped
						mapEventStateField.SetValue(__instance, PlayerEncounterState.LootInventory);
					}
					return false; // Block the loot menu
				}
				catch (Exception ex)
				{
					ModLogger.Error("LootRestriction", $"Error in loot menu patch: {ex.Message}", ex);
					return true; // Allow loot on error to prevent breaking gameplay
				}
			}
		}

		/// <summary>
		/// Modifies the loot amount percentage for enlisted officers based on their tier.
		/// This affects both prisoner acquisition and item loot distribution.
		/// </summary>
		[HarmonyPatch]
		public class LootAmountPatch
		{
			/// <summary>
			/// Finds the GiveShareOfLootToParty method in LootCollector to patch.
			/// Uses reflection because the method is internal.
			/// </summary>
			static MethodBase TargetMethod()
			{
				try
				{
					// Find LootCollector class (internal class in TaleWorlds.CampaignSystem.MapEvents)
					var lootCollectorType = AccessTools.TypeByName("TaleWorlds.CampaignSystem.MapEvents.LootCollector");
					if (lootCollectorType == null)
					{
						ModLogger.Error("LootRestriction", "Could not find LootCollector type");
						return null;
					}

					// Find GiveShareOfLootToParty method
					// Signature: internal void GiveShareOfLootToParty(TroopRoster memberRoster, TroopRoster prisonerRoster, ItemRoster itemRoster, PartyBase winnerParty, float lootAmount, MapEvent mapEvent)
					var method = AccessTools.Method(lootCollectorType, "GiveShareOfLootToParty",
						new[] { typeof(TroopRoster), typeof(TroopRoster), typeof(ItemRoster), typeof(PartyBase), typeof(float), typeof(MapEvent) });
					
					if (method != null)
					{
						ModLogger.Info("LootRestriction", "Found GiveShareOfLootToParty method for patching");
						return method;
					}
					
					ModLogger.Error("LootRestriction", "Could not find GiveShareOfLootToParty method to patch");
					return null;
				}
				catch (Exception ex)
				{
					ModLogger.Error("LootRestriction", $"Exception finding GiveShareOfLootToParty method: {ex.Message}", ex);
					return null;
				}
			}

			/// <summary>
			/// Modifies the lootAmount parameter before it's used to determine loot distribution.
			/// Reduces loot amount based on officer tier when player is enlisted.
			/// </summary>
			static void Prefix(ref float lootAmount, PartyBase winnerParty)
			{
				try
				{
					// Only modify loot for the player's party
					if (winnerParty != PartyBase.MainParty)
					{
						return;
					}

					var enlistment = EnlistmentBehavior.Instance;
					if (enlistment?.IsEnlisted != true)
					{
						return; // Not enlisted - use normal loot amount
					}

					var tier = enlistment.EnlistmentTier;
					
					// Non-officers (tier < 4) shouldn't reach here because menu is blocked
					// But if they do, give them nothing
					if (tier < 4)
					{
						lootAmount = 0f;
						ModLogger.Debug("LootRestriction", $"Non-officer tier {tier} - setting loot amount to 0%");
						return;
					}

					// Officers get reduced loot based on tier (max tier is 6)
					float lootPercentage = tier switch
					{
						4 => 0.10f, // Tier 4: 10% of normal loot
						5 => 0.20f, // Tier 5: 20% of normal loot
						>= 6 => 0.30f, // Tier 6 (max): 30% of normal loot
						_ => 0.30f  // Fallback
					};

					// Multiply the original loot amount by the tier percentage
					float originalAmount = lootAmount;
					lootAmount *= lootPercentage;
					
					ModLogger.Info("LootRestriction", $"Officer tier {tier} - reduced loot from {originalAmount:P0} to {lootAmount:P0} ({lootPercentage:P0} of normal)");
				}
				catch (Exception ex)
				{
					ModLogger.Error("LootRestriction", $"Error in loot amount patch: {ex.Message}", ex);
					// Don't modify lootAmount on error - use normal amount to prevent breaking gameplay
				}
			}
		}
	}
}

