using HarmonyLib;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using Enlisted.Features.Enlistment.Application;
using Enlisted.Mod.Core.Config;

namespace Enlisted.Mod.GameAdapters.Patches
{
	// Harmony Patch
	// Target: TaleWorlds.CampaignSystem.Encounters.PlayerEncounter.DoMeeting()
	// Why: While enlisted (party-only), prevent player-initiated encounter UI unless the enlisted lord leads the map event.
	// Safety: Campaign-only; checks enlistment flag; allows through when suppress setting off; no changes beyond early-return.
	[HarmonyPatch(typeof(PlayerEncounter), nameof(PlayerEncounter.DoMeeting))]
	internal static class Encounter_DoMeetingGuardPatch
	{
		static bool Prefix()
		{
			// Feature gate
			if (ModConfig.Settings?.SAS?.SuppressPlayerEncounter != true)
			{
				return true;
			}
			var enlist = EnlistmentBehavior.Instance;
			if (enlist == null || !enlist.IsEnlisted)
			{
				return true;
			}
			var lordPartyBase = enlist.CurrentLord?.PartyBelongedTo?.Party;
			var playerEvent = MapEvent.PlayerMapEvent;
			if (playerEvent == null || lordPartyBase == null)
			{
				return true;
			}

			var attackerLeader = playerEvent.AttackerSide?.LeaderParty;
			var defenderLeader = playerEvent.DefenderSide?.LeaderParty;
			var lordLeads = (attackerLeader == lordPartyBase) || (defenderLeader == lordPartyBase);
			if (!lordLeads)
			{
				// Cancel the meeting; the guard in enlistment will keep us attached.
				PlayerEncounter.LeaveEncounter = true;
				return false;
			}
			return true;
		}
	}
}


