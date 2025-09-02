using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Enlisted.Debugging.Discovery.Infrastructure;
using Enlisted.Mod.Core.Config;
using Enlisted.Mod.Core.Logging;
using Enlisted.Mod.Core.Util;

namespace Enlisted.Mod.GameAdapters.Patches
{
	// Harmony Patch
	// Target: TaleWorlds.CampaignSystem.GameMenus.GameMenuManager.ActivateGameMenu(string)
	//         TaleWorlds.CampaignSystem.GameMenus.GameMenuManager.SwitchToMenu(string)
	[HarmonyPatch]
	internal static class GameMenuManager_TransitionsPatch
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			var type = AccessTools.TypeByName("TaleWorlds.CampaignSystem.GameMenus.GameMenuManager");
			if (type == null) yield break;
			foreach (var m in AccessTools.GetDeclaredMethods(type))
			{
				if ((m.Name == "ActivateGameMenu" || m.Name == "SwitchToMenu")
					&& m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(string))
				{
					yield return m;
				}
			}
		}

		static void Prefix([HarmonyArgument(0)] string menuId)
		{
			if (string.IsNullOrEmpty(menuId)) return;
			if (ModConfig.Settings.LogMenus)
			{
				if (!ModConfig.Settings.DiscoveryPlayerOnly || PlayerContext.IsPlayerContextActive())
				{
					DiscoveryLog.LogMenuOpen("switch", menuId);
				}
			}
			if (ModConfig.Settings.LogApiCalls)
			{
				ModLogger.Api("INFO", $"GameMenuManager.Switch/Activate id={menuId}");
			}
		}
	}
}


