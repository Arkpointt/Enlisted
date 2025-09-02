using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Enlisted.Debugging.Discovery.Infrastructure;
using Enlisted.Mod.Core.Config;
using TaleWorlds.CampaignSystem.GameMenus;
using Enlisted.Mod.Core.Logging;
using Enlisted.Mod.Core.Util;

namespace Enlisted.Mod.GameAdapters.Patches
{
	// Harmony Patch
	// Target: TaleWorlds.CampaignSystem.GameMenus.GameMenu.ActivateGameMenu(string)
	//         TaleWorlds.CampaignSystem.GameMenus.GameMenu.SwitchToMenu(string)
	// Why: Capture runtime menu transitions that may select variant ids (e.g., *_diplomatic)
	// Safety: Read-only; gated by settings
	[HarmonyPatch]
	internal static class GameMenu_TransitionsPatch
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			foreach (var m in AccessTools.GetDeclaredMethods(typeof(GameMenu)))
			{
				if ((m.Name == nameof(GameMenu.ActivateGameMenu) || m.Name == nameof(GameMenu.SwitchToMenu))
					&& m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(string))
				{
					yield return m;
				}
			}
		}

		static void Prefix([HarmonyArgument(0)] string menuId)
		{
			if (!(ModConfig.Settings.LogMenus || ModConfig.Settings.LogApiCalls)) return;
			// Discovery log is gated by player-only if enabled
			if (ModConfig.Settings.LogMenus)
			{
				if (!ModConfig.Settings.DiscoveryPlayerOnly || PlayerContext.IsPlayerContextActive())
				{
					DiscoveryLog.LogMenuOpen("switch", menuId);
				}
			}
			// API log writes regardless of player-only gating when enabled
			if (ModConfig.Settings.LogApiCalls)
			{
				ModLogger.Api("INFO", $"GameMenu.SwitchTo/Activate id={menuId}");
			}
		}
	}
}


