using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Enlisted.Mod.Core.Config;
using Enlisted.Debugging.Discovery.Infrastructure;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Mod.GameAdapters.Patches
{
	// Harmony Patch
	// Target: TaleWorlds.CampaignSystem.Conversation.ConversationManager.AddDialogFlow(...)
	//         TaleWorlds.CampaignSystem.Conversation.ConversationManager.OpenMapConversation(...)
	// Why: Some dialogs are registered via dialog flows, and conversations can start via ConversationManager
	[HarmonyPatch]
	internal static class ConversationManager_DiscoveryPatches
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			var cmType = AccessTools.TypeByName("TaleWorlds.CampaignSystem.Conversation.ConversationManager");
			if (cmType == null) yield break;
			foreach (var m in AccessTools.GetDeclaredMethods(cmType))
			{
				if (m.Name == "AddDialogFlow") yield return m;
				if (m.Name == "OpenMapConversation") yield return m;
			}
		}

		static void Postfix(MethodBase __originalMethod, object __instance, params object[] __args)
		{
			if (!ModConfig.Settings.LogDialogs) return;
			var name = __originalMethod?.Name ?? "unknown";
			ModLogger.Dialog("INFO", $"ConversationManager.{name} invoked");
		}
	}
}


