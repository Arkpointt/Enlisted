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
using System.Reflection;

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
			var field = typeof(EnlistmentBehavior).GetField("_enlistedLord", BindingFlags.NonPublic | BindingFlags.Instance);
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
					NextFrameDispatcher.RunNextFrame(() => GameMenu.ActivateGameMenu("enlisted_status"), true);
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

			var offset = main.Position2D - target.Position2D;
			var distance = offset.Length;
			var attachmentName = main.AttachedTo?.LeaderHero?.Name?.ToString() ?? main.AttachedTo?.Name?.ToString() ?? "none";
			var targetName = target.LeaderHero?.Name?.ToString() ?? target.Name?.ToString() ?? "unknown";
			
			// Log only if state changes to reduce spam
			if (main.AttachedTo != target)
			{
				ModLogger.Debug("Following", $"AttachOrEscort check -> target={targetName}, distance={distance:F2}, attachedTo={attachmentName}, mapEvent={(main.Party.MapEvent != null)}");
			}

			// Use natural attachment system for following behavior
			// This provides automatic following and proper army integration
			if (main.AttachedTo != target)
			{
				// Attach to the lord's party if not already attached
				// This enables natural following behavior and army integration
				TryAttach(main, target);
				
				if (main.AttachedTo == target)
				{
					ModLogger.Info("Following", $"Attached player party to {targetName} via engine attachment");
					
					// Clear any previous move orders/clicks to remove the yellow cursor/visuals
					try
					{
						main.Ai.SetMoveModeHold();
					}
					catch { /* Best effort */ }
					
					// CRITICAL: Set camera to follow the lord's party, since we are attached to them
					// The player party is invisible, so following the lord makes more sense visually
					try
					{
						target.Party.SetAsCameraFollowParty();
					}
					catch { /* Best effort */ }
					
					// CRITICAL: Once attached, do NOT issue further move commands.
					return;
				}
			}
			
			// If we are already attached, we don't need to do anything else.
			if (main.AttachedTo == target)
			{
				// Ensure camera stays on lord during attachment
				if (main.Party.MapEvent == null)
				{
					target.Party.SetAsCameraFollowParty();
				}
				return; 
			}

			// Fallback: If attachment failed (reflection issue?), use standard Escort AI.
			// We do NOT use SetMoveGoToPoint (manual trailing) anymore as it causes visual cursor clicking artifacts.
			if (main.Party.MapEvent == null || main.Party.MapEvent.IsFinalized)
			{
				// Use standard Escort AI as fallback
				if (main.TargetParty != target)
				{
					ModLogger.Debug("Following", "Fallback: Setting standard Escort AI (Attachment failed)");
					main.Ai.SetMoveEscortParty(target);
				}
			}
			else
			{
				ModLogger.Debug("Following", "Skipping follow command - player is in active battle");
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
				var attach = typeof(MobileParty).GetMethod("AttachTo", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (attach != null)
				{
					attach.Invoke(main, new object[] { target });
					ModLogger.Debug("Following", $"Reflection attach invoked -> target={target.LeaderHero?.Name?.ToString() ?? target.Name?.ToString() ?? "unknown"}");
					return;
				}

				// If reflection fails, we fall back to Escort AI in the calling method
				ModLogger.Debug("Following", "AttachTo reflection method not found");
			}
			catch (Exception ex)
			{
				ModLogger.Error("Following", $"Attach attempt failed: {ex.Message}");
			}
		}

		// Removed ApplyTrailingOffset to prevent "clicking" artifacts on the map
	}
}
