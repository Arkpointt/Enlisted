using HarmonyLib;
using Enlisted.Mod.Core.Config;
using Enlisted.Debugging.Discovery.Infrastructure;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Mod.GameAdapters.Patches
{
	// Harmony Patch
	// Target: TaleWorlds.CampaignSystem.CampaignGameStarter.AddDialogLine(...)
	// Why: Log dialog registrations for discovery
	// Safety: Read-only; gated by settings
	[HarmonyPatch(typeof(CampaignGameStarter), nameof(CampaignGameStarter.AddDialogLine))]
	internal static class CampaignGameStarter_AddDialogLine_Patch
	{
		static void Prefix(
			[HarmonyArgument(0)] string id,
			[HarmonyArgument(1)] string inputToken,
			[HarmonyArgument(4)] ref ConversationSentence.OnConditionDelegate condition,
			[HarmonyArgument(5)] ref ConversationSentence.OnConsequenceDelegate consequence)
		{
			if (!ModConfig.Settings.LogDialogs) return;
			DiscoveryLog.LogDialogRegistration("AddDialogLine", id);
			// Record the dialog token for aggregation
			DiscoveryAggregator.RecordDialogToken(inputToken);
			var originalCond = condition;
			condition = () =>
			{
				var ok = originalCond == null || originalCond();
				if (ok)
				{
					ModLogger.Dialog("INFO", $"available npc_line id={id} token={inputToken}");
					DiscoveryAggregator.RecordDialogToken(inputToken);
				}
				return ok;
			};
			var original = consequence;
			consequence = () =>
			{
				ModLogger.Dialog("INFO", $"selected npc_line id={id} token={inputToken}");
				DiscoveryAggregator.RecordDialogToken(inputToken);
				original?.Invoke();
			};
		}
	}

	// Harmony Patch
	// Target: TaleWorlds.CampaignSystem.CampaignGameStarter.AddPlayerLine(...)
	[HarmonyPatch(typeof(CampaignGameStarter), nameof(CampaignGameStarter.AddPlayerLine))]
	internal static class CampaignGameStarter_AddPlayerLine_Patch
	{
		static void Prefix(
			[HarmonyArgument(0)] string id,
			[HarmonyArgument(1)] string inputToken,
			[HarmonyArgument(4)] ref ConversationSentence.OnConditionDelegate condition,
			[HarmonyArgument(5)] ref ConversationSentence.OnConsequenceDelegate consequence,
			[HarmonyArgument(7)] ref ConversationSentence.OnClickableConditionDelegate clickableCondition)
		{
			if (!ModConfig.Settings.LogDialogs) return;
			DiscoveryLog.LogDialogRegistration("AddPlayerLine", id);
			// Record the dialog token for aggregation
			DiscoveryAggregator.RecordDialogToken(inputToken);
			var originalCond = condition;
			condition = () =>
			{
				var ok = originalCond == null || originalCond();
				if (ok)
				{
					ModLogger.Dialog("INFO", $"available player_line id={id} token={inputToken}");
					DiscoveryAggregator.RecordDialogToken(inputToken);
				}
				return ok;
			};
			var original = consequence;
			consequence = () =>
			{
				ModLogger.Dialog("INFO", $"selected player_line id={id} token={inputToken}");
				DiscoveryAggregator.RecordDialogToken(inputToken);
				original?.Invoke();
			};
		}
	}

	// Harmony Patch
	// Target: TaleWorlds.CampaignSystem.CampaignGameStarter.AddRepeatablePlayerLine(...)
	[HarmonyPatch(typeof(CampaignGameStarter), nameof(CampaignGameStarter.AddRepeatablePlayerLine))]
	internal static class CampaignGameStarter_AddRepeatablePlayerLine_Patch
	{
		static void Prefix(
			[HarmonyArgument(0)] string id,
			[HarmonyArgument(1)] string inputToken,
			[HarmonyArgument(6)] ref ConversationSentence.OnConditionDelegate condition,
			[HarmonyArgument(7)] ref ConversationSentence.OnConsequenceDelegate consequence,
			[HarmonyArgument(9)] ref ConversationSentence.OnClickableConditionDelegate clickableCondition)
		{
			if (!ModConfig.Settings.LogDialogs) return;
			DiscoveryLog.LogDialogRegistration("AddRepeatablePlayerLine", id);
			// Record the dialog token for aggregation
			DiscoveryAggregator.RecordDialogToken(inputToken);
			var originalCond = condition;
			condition = () =>
			{
				var ok = originalCond == null || originalCond();
				if (ok)
				{
					ModLogger.Dialog("INFO", $"available repeatable_player_line id={id} token={inputToken}");
					DiscoveryAggregator.RecordDialogToken(inputToken);
				}
				return ok;
			};
			var original = consequence;
			consequence = () =>
			{
				ModLogger.Dialog("INFO", $"selected repeatable_player_line id={id} token={inputToken}");
				DiscoveryAggregator.RecordDialogToken(inputToken);
				original?.Invoke();
			};
		}
	}
}


