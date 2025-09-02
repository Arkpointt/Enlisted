using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Enlisted.Mod.Core.Config;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Mod.GameAdapters.Patches
{
	// Harmony Patch
	// Target (broad): ConversationManager methods that likely fire on dialog option selection
	// We match by common names to avoid signature dependency: ChooseOption/Select*/*Consequence*
	[HarmonyPatch]
	internal static class ConversationManager_SelectionPatch
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			var cmType = AccessTools.TypeByName("TaleWorlds.CampaignSystem.Conversation.ConversationManager");
			if (cmType == null) yield break;
			foreach (var m in AccessTools.GetDeclaredMethods(cmType))
			{
				var n = m.Name;
				if (n.Contains("Consequence") || n.Contains("Choose") || n.Contains("Select"))
				{
					yield return m;
				}
			}
		}

		static void Postfix(MethodBase __originalMethod)
		{
			if (!ModConfig.Settings.LogDialogs) return;
			var name = __originalMethod?.Name ?? "unknown";
			ModLogger.Dialog("INFO", $"Conversation selection hook: {name}");
		}
	}
}


