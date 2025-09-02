using HarmonyLib;
using Enlisted.Mod.Core.Config;
using Enlisted.Debugging.Discovery.Infrastructure;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using System.Collections.Generic;
using System.Reflection;

namespace Enlisted.Mod.GameAdapters.Patches
{
	// Harmony Patch
	// Target: TaleWorlds.CampaignSystem.CampaignGameStarter.AddGameMenu(...) and AddWaitGameMenu(...)
	// Why: Log menu registrations (id and registrar) for discovery
	// Safety: Read-only; gated by settings; campaign-only via type
	[HarmonyPatch]
	internal static class CampaignGameStarter_AddGameMenu_Patch
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			foreach (var m in AccessTools.GetDeclaredMethods(typeof(CampaignGameStarter)))
			{
				if (m.Name == nameof(CampaignGameStarter.AddGameMenu)) yield return m;
				if (m.Name == nameof(CampaignGameStarter.AddWaitGameMenu)) yield return m;
			}
		}

		static void Prefix([HarmonyArgument(0)] string menuId)
		{
			if (!ModConfig.Settings.LogMenus) return;
			DiscoveryLog.LogMenuRegistration("AddGameMenu", menuId);
		}
	}

	// Harmony Patch
	// Target: TaleWorlds.CampaignSystem.CampaignGameStarter.AddGameMenuOption(...)
	// Why: Log option registrations by menu
	// Safety: Read-only; gated by settings
	[HarmonyPatch]
	internal static class CampaignGameStarter_AddGameMenuOption_Patch
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			foreach (var m in AccessTools.GetDeclaredMethods(typeof(CampaignGameStarter)))
			{
				if (m.Name == nameof(CampaignGameStarter.AddGameMenuOption)) yield return m;
			}
		}

		static void Prefix(
			[HarmonyArgument(0)] string menuId,
			[HarmonyArgument(1)] string optionId,
			[HarmonyArgument(3)] ref GameMenuOption.OnConditionDelegate condition,
			[HarmonyArgument(4)] ref GameMenuOption.OnConsequenceDelegate consequence)
		{
			if (!ModConfig.Settings.LogMenus) return;
			if (!string.IsNullOrEmpty(optionId))
			{
				DiscoveryLog.LogMenuOptionRegistration(menuId, optionId);

				// Wrap delegates to log runtime selection
				var originalConsequence = consequence;
				consequence = (MenuCallbackArgs args) =>
				{
					DiscoveryLog.LogMenuOpen("selected", optionId);
					originalConsequence?.Invoke(args);
				};
			}
			else
			{
				DiscoveryLog.LogMenuRegistration("AddGameMenuOption", menuId);
			}
		}
	}
}


