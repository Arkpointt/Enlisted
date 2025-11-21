using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Mod.GameAdapters.Patches
{
	/// <summary>
	/// Harmony patch that prevents enlisted soldiers from leaving battles at the encounter menu level.
	/// Intercepts MapEventHelper.CanLeaveBattle to prevent battle retreat options from appearing
	/// in the encounter menu when the player is enlisted, ensuring enlisted soldiers cannot abandon
	/// their lord during battles. This complements MissionFightEndPatch which prevents leaving during
	/// actual combat missions.
	/// </summary>
	[HarmonyPatch(typeof(MapEventHelper), "CanLeaveBattle")]
	public class BattleLeavePreventionPatch
	{
		/// <summary>
		/// Prefix method that runs before MapEventHelper.CanLeaveBattle.
		/// Prevents enlisted players from leaving battles by returning false when they are enlisted.
		/// This blocks the "retreat" option from appearing in encounter menus during battles.
		/// </summary>
		/// <param name="mobileParty">The mobile party attempting to leave the battle.</param>
		/// <param name="__result">Output parameter indicating whether the party can leave the battle.</param>
		/// <returns>False to skip the original method and use our result, true to allow normal behavior.</returns>
		static bool Prefix(MobileParty mobileParty, ref bool __result)
		{
			// Default to allowing normal behavior - only intervene for enlisted players
			__result = true;

			try
			{
				// Only affect the main party (player)
				if (mobileParty != MobileParty.MainParty)
				{
					return true; // Allow normal behavior for AI parties
				}

				var enlistment = EnlistmentBehavior.Instance;
				if (enlistment?.IsEnlisted != true)
				{
					return true; // Allow normal behavior when not enlisted
				}

				// Prevent leaving battles when enlisted
				// Enlisted soldiers serve their lord and cannot retreat from battles
				__result = false;
				ModLogger.Debug("BattleLeave", "Prevented battle leave option for enlisted player");
				return false; // Skip original method and use our result
			}
			catch (System.Exception ex)
			{
				ModLogger.Error("BattleLeave", "Error in battle leave prevention patch", ex);
				// On error, allow normal behavior to prevent breaking battles
				__result = true;
				return true; // Allow normal behavior on error
			}
		}
	}
}

