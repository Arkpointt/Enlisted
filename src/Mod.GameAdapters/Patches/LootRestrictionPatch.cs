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
	/// Blocks all loot access for enlisted soldiers regardless of rank.
	/// 
	/// As a soldier in someone else's army, the player does not receive personal loot.
	/// All spoils of war go to the lord. The soldier is compensated through wages instead.
	/// 
	/// Targets: PlayerEncounter.DoLootParty (private void)
	/// This method handles the transition to the loot party screen after battle.
	/// Safety: Campaign-only; checks enlistment state; fails open on error.
	/// </summary>
	[HarmonyPatch(typeof(PlayerEncounter), "DoLootParty")]
	public static class LootRestrictionPatch
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
				
				// Allow loot when on leave or in grace period - player is operating independently
				if (enlistment.IsOnLeave || enlistment.IsInDesertionGracePeriod)
				{
					return true;
				}

				// Actively serving enlisted soldiers get NO loot - spoils go to the lord
				ModLogger.Info("LootRestriction", "Blocking loot menu - enlisted soldiers don't receive personal loot");
				
				// Skip ALL loot states (party, inventory, ships/figureheads) and go directly to End
				// This prevents crashes from figurehead/banner loot screens in inconsistent states
				var mapEventStateField = typeof(PlayerEncounter).GetField("_mapEventState",
					BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
				if (mapEventStateField != null)
				{
					mapEventStateField.SetValue(__instance, PlayerEncounterState.End);
				}
				return false; // Block the loot menu
			}
			catch (Exception ex)
			{
				ModLogger.Error("LootRestriction", $"Error in loot restriction patch: {ex.Message}", ex);
				return true; // Allow loot on error to prevent breaking gameplay
			}
		}
	}
}

