using System;
using HarmonyLib;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Localization;
using System.Reflection;

namespace Enlisted.Mod.GameAdapters.Patches
{
	/// <summary>
	/// Injects enlistment wages into the vanilla clan finance calculation so the daily gold change UI
	/// shows service income instead of relying on a custom log line.
	/// </summary>
	[HarmonyPatch]
	internal static class ClanFinanceEnlistmentIncomePatch
	{
		private static readonly TextObject EnlistmentWageText = new TextObject("{=enlisted_wage_income}Enlistment Wages");

		static MethodBase TargetMethod()
		{
			return AccessTools.Method(
				typeof(DefaultClanFinanceModel),
				"CalculateClanIncomeInternal",
				new[] { typeof(Clan), typeof(ExplainedNumber).MakeByRefType(), typeof(bool), typeof(bool) });
		}

		private static void Postfix(Clan clan, ref ExplainedNumber goldChange)
		{
			try
			{
				if (clan != Clan.PlayerClan)
				{
					return;
				}

				var enlistment = EnlistmentBehavior.Instance;
				if (enlistment?.TryGetProjectedDailyWage(out var wageAmount) == true && wageAmount > 0)
				{
					goldChange.Add(wageAmount, EnlistmentWageText, null);
					// Note: Wage amount is visible in game UI - no DEBUG log needed here
					// This method is called frequently by the finance calculation system
				}
			}
			catch (Exception ex)
			{
				ModLogger.Error("Finance", $"Failed to append enlistment wages to clan income: {ex.Message}");
			}
		}
	}
}

