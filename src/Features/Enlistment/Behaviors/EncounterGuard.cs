using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.Library;
using Enlisted.Mod.Core.Config;

namespace Enlisted.Features.Enlistment.Behaviors
{
	/// <summary>
	/// Keeps player party attached/escorting to avoid initiating encounters while enlisted.
	/// Soft guard: closes stray player-initiated encounter menus and restores attach.
	/// </summary>
	internal static class EncounterGuard
	{
		public static void Initialize()
		{
			// Soft guard removed per design; Harmony guard will handle DoMeeting.
		}

		// Soft guard removed

		private static Hero enlistCurrentLord()
		{
			var inst = EnlistmentBehavior.Instance;
			var field = typeof(EnlistmentBehavior).GetField("_enlistedLord", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			return field?.GetValue(inst) as Hero;
		}

		internal static void TryLeaveEncounter()
		{
			try
			{
				if (PlayerEncounter.Current != null)
				{
					PlayerEncounter.LeaveEncounter = true;
					PlayerEncounter.Finish(true);
					// Transition to a neutral holding menu to keep time control sane
					GameMenu.ActivateGameMenu("party_wait");
				}
			}
			catch
			{
				// Best-effort: engine owns these flows.
			}
		}

		public static void TryAttachOrEscort(Hero lord)
		{
			var main = MobileParty.MainParty;
			var target = lord?.PartyBelongedTo;
			if (main == null || target == null)
			{
				return;
			}

			var sas = ModConfig.Settings?.SAS;
			if (sas?.AttachWhenClose == true)
			{
				var distance = main.Position2D.Distance(target.Position2D);
				if (distance <= (float)(sas.AttachRange))
				{
					TryAttach(main, target);
					return;
				}
			}

			// Not close enough or attach disabled → escort and trail.
			EnlistmentBehavior.ApplyEscort(lord);
			ApplyTrailingOffset(main, target, (float)(sas?.TrailDistance ?? 1.2));
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


