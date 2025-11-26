using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Mod.GameAdapters.Patches
{
	/// <summary>
	/// Restricts loot access for enlisted soldiers based on their tier/rank.
	/// 
	/// Non-officers (tier &lt; 4) get NO loot access - the loot menu is completely blocked.
	/// Officers (tier &gt;= 4) get full access to the loot selection screen.
	/// 
	/// This enforces realistic military hierarchy where regular soldiers don't receive
	/// personal loot rights until they reach officer status.
	/// 
	/// Note: The loot quantity reduction feature (officers getting reduced percentages)
	/// was removed in Bannerlord 1.3.4 because the LootCollector.GiveShareOfLootToParty 
	/// method no longer exists. The loot system was refactored to use MapEventParty rosters.
	/// </summary>
	public static class LootRestrictionPatches
	{
		/// <summary>
		/// Blocks the loot menu from appearing for non-officer enlisted soldiers (tier &lt; 4).
		/// Officers (tier &gt;= 4) get normal loot access.
		/// 
		/// Targets: PlayerEncounter.DoLootParty (private void)
		/// This method handles the transition to the loot party screen after battle.
		/// Safety: Campaign-only; checks enlistment state; fails open on error.
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
					
					// Officers (tier 4+) get full loot access
					if (tier >= 4)
					{
						ModLogger.Debug("LootRestriction", $"Officer tier {tier} - allowing loot menu");
						return true; // Allow loot menu to open
					}
					
					// Non-officers (tier < 4) get NO loot access
					ModLogger.Info("LootRestriction", $"Non-officer tier {tier} - blocking loot menu");
					
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
	}
}

