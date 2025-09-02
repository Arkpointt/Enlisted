using HarmonyLib;
using Enlisted.Mod.Core.Config;
using Enlisted.Debugging.Discovery.Infrastructure;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem;

namespace Enlisted.Mod.GameAdapters.Patches
{
	// Harmony Patch
	// Target: TaleWorlds.CampaignSystem.Encounters.PlayerEncounter.DoMeeting()
	// Why: Log meeting kickoff with current menu id and conversation state
	[HarmonyPatch(typeof(PlayerEncounter), nameof(PlayerEncounter.DoMeeting))]
	internal static class PlayerEncounter_DoMeeting_Patch
	{
		static void Postfix()
		{
			if (!ModConfig.Settings.LogMenus) return;
			var id = Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId ?? "unknown";
			DiscoveryLog.LogMenuOpen("meeting", id);
		}
	}
}


