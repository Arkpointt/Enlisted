using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Mod.GameAdapters.Patches
{
	/// <summary>
	/// Static flag to track when we're in the middle of a discharge operation.
	/// This prevents relation penalties from being applied during kingdom restoration.
	/// </summary>
	public static class DischargeState
	{
		/// <summary>
		/// Flag indicating if we're currently discharging the player from enlistment.
		/// When true, relation penalties will be suppressed.
		/// </summary>
		public static bool IsDischarging { get; set; } = false;
	}

	/// <summary>
	/// Harmony patch that intercepts ChangeRelationAction to prevent relation penalties
	/// during enlistment discharge. This suppresses the -20 relation penalty that would
	/// normally be applied when leaving a kingdom during discharge.
	/// </summary>
	[HarmonyPatch(typeof(ChangeRelationAction), "ApplyRelationChangeBetweenHeroes")]
	public class DischargeRelationPenaltyPatch
	{
		static bool Prefix(Hero hero, Hero gainedRelationWith, int relationChange, bool showQuickNotification)
		{
			try
			{
				// Only suppress negative relation changes for the player during discharge
				if (hero != Hero.MainHero && gainedRelationWith != Hero.MainHero)
				{
					return true; // Not involving the player - allow normal behavior
				}

				// Check if we're discharging and this is a negative relation change
				// The typical penalty when leaving a kingdom is -20 per clan leader
				if (DischargeState.IsDischarging && relationChange < 0)
				{
					// Suppress the relation penalty during discharge
					// This prevents the -20 penalty that would normally apply when leaving a kingdom
					// The flag is set in DischargeHelper.RestoreKingdomWithoutPenalties before calling
					// ChangeKingdomAction methods, so any relation changes during that time are suppressed
					var otherHero = hero == Hero.MainHero ? gainedRelationWith : hero;
					ModLogger.Info("Discharge", $"Suppressed relation penalty of {relationChange} during discharge (OtherHero: {otherHero?.Name?.ToString() ?? "null"})");
					return false; // Suppress the penalty
				}

				return true; // Allow normal relation changes
			}
			catch (Exception ex)
			{
				ModLogger.Error("Discharge", $"Error in relation penalty suppression: {ex.Message}");
				return true; // Fail open - allow normal behavior
			}
		}
	}

	/// <summary>
	/// Helper class to manage discharge state and prevent relation penalties.
	/// Used by EnlistmentBehavior when restoring the player's original kingdom.
	/// </summary>
	public static class DischargeHelper
	{
		/// <summary>
		/// Execute a kingdom change action without applying relation penalties.
		/// Used when discharging the player from enlistment to restore their original kingdom.
		/// </summary>
		public static void RestoreKingdomWithoutPenalties(Clan clan, Kingdom targetKingdom)
		{
			try
			{
				// Mark that we're discharging to suppress penalties
				DischargeState.IsDischarging = true;
				ModLogger.Debug("Discharge", "Beginning discharge operation - relation penalties will be suppressed");

				try
				{
					var currentKingdom = clan.Kingdom;

					if (currentKingdom != null && targetKingdom == null)
					{
						// Leave kingdom to become independent
						ChangeKingdomAction.ApplyByLeaveKingdom(clan, false);
						ModLogger.Info("Discharge", "Restored player clan to independent status without penalties");
					}
				else if (currentKingdom != targetKingdom && targetKingdom != null)
				{
					// Join a different kingdom (1.3.4 API: added CampaignTime parameter)
					ChangeKingdomAction.ApplyByJoinToKingdom(clan, targetKingdom, default(CampaignTime), false);
					string kingdomName = targetKingdom.Name?.ToString() ?? "kingdom";
					ModLogger.Info("Discharge", $"Restored player clan to {kingdomName} without penalties");
				}
					// If kingdoms match, no action needed
				}
				finally
				{
					// Always clear the discharge flag, even if an error occurs
					DischargeState.IsDischarging = false;
					ModLogger.Debug("Discharge", "Discharge operation complete - relation penalties restored");
				}
			}
			catch (Exception ex)
			{
				ModLogger.Error("Discharge", $"Error restoring kingdom without penalties: {ex.Message}");
				DischargeState.IsDischarging = false; // Ensure flag is cleared
				throw; // Re-throw to allow error handling in caller
			}
		}
	}
}

