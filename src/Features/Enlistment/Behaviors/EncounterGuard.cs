using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.Library;
using Enlisted.Mod.Core.Config;
using Enlisted.Mod.Entry;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Features.Enlistment.Behaviors
{
	/// <summary>
	/// Utility class for managing player party attachment and encounter transitions.
	/// Provides methods for attaching the player's party to the lord's party for following,
	/// and handling encounter state transitions safely.
	/// </summary>
	internal static class EncounterGuard
	{
		/// <summary>
		/// Initializes the EncounterGuard system.
		/// Currently a placeholder as encounter prevention is handled by Harmony patches.
		/// </summary>
		public static void Initialize()
		{
			// Encounter prevention is handled by Harmony patches that prevent
			// player-initiated encounters while enlisted
		}

		private static Hero enlistCurrentLord()
		{
			var inst = EnlistmentBehavior.Instance;
			var field = typeof(EnlistmentBehavior).GetField("_enlistedLord", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			return field?.GetValue(inst) as Hero;
		}

		/// <summary>
		/// Safely leaves the current player encounter and activates the enlisted status menu.
		/// Encounter exit and menu activation are deferred to the next frame to prevent
		/// timing conflicts with the game's rendering system during state transitions.
		/// </summary>
		internal static void TryLeaveEncounter()
		{
			try
			{
				if (PlayerEncounter.Current != null)
				{
					// Defer encounter finishing to the next frame to avoid timing conflicts
					// This prevents assertion failures that can occur during encounter exit
					NextFrameDispatcher.RunNextFrame(() =>
					{
						if (PlayerEncounter.Current != null)
						{
							PlayerEncounter.LeaveEncounter = true;
							PlayerEncounter.Finish(true);
						}
					});
					
					// Defer menu activation to the next frame after encounter exit completes
					// This ensures the menu activates cleanly after state transitions
					NextFrameDispatcher.RunNextFrame(() => GameMenu.ActivateGameMenu("enlisted_status"));
				}
			}
			catch
			{
				// Best-effort: if encounter handling fails, the engine will handle cleanup
			}
		}

		/// <summary>
		/// Attaches the player's party to the lord's party by matching positions directly.
		/// Uses direct position assignment instead of AI escort to avoid complications
		/// with the AI system and prevent assertion failures during battle state transitions.
		/// Only positions the party when not in an active battle.
		/// </summary>
		/// <param name="lord">The lord whose party the player should follow.</param>
		public static void TryAttachOrEscort(Hero lord)
		{
			var main = MobileParty.MainParty;
			var target = lord?.PartyBelongedTo;
			if (main == null || target == null)
			{
				return;
			}

			// Use natural attachment system for following behavior
			// This provides automatic following and proper army integration
			// Expense sharing is prevented by EnlistmentExpenseIsolationPatch
			if (main.AttachedTo != target)
			{
				// Attach to the lord's party if not already attached
				// This enables natural following behavior and army integration
				TryAttach(main, target);
			}

			// Use direct position matching as a fallback when not in battle
			// This ensures the player follows the lord during travel
			if (main.Party.MapEvent == null || main.Party.MapEvent.IsFinalized)
			{
				// Match the player's position directly to the lord's position
				// This works in combination with AttachedTo for reliable following
				main.Position2D = target.Position2D;
				ModLogger.Debug("Following", "Position matching for following (with attachment)");
			}
			else
			{
				ModLogger.Debug("Following", "Skipping position matching - player is in active battle");
			}
		}

		private static void TryAttach(MobileParty main, MobileParty target)
		{
			try
			{
				// Use engine attach API if available via reflection.
				if (main.AttachedTo == target)
				{
					return;
				}
				var attach = typeof(MobileParty).GetMethod("AttachTo", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
				if (attach != null)
				{
					attach.Invoke(main, new object[] { target });
				}
			}
			catch
			{
				// Fallback is escort (handled by caller).
			}
		}

		private static void ApplyTrailingOffset(MobileParty main, MobileParty target, float trailDistance)
		{
			try
			{
				// Simple rear offset behind target's forward vector.
				var toMain = (main.Position2D - target.Position2D).Normalized();
				var desired = target.Position2D - toMain * trailDistance;
				main.Ai.SetMoveGoToPoint(desired);
			}
			catch
			{
				// Best-effort positioning; if unavailable, escort alone is fine.
			}
		}
	}
}


