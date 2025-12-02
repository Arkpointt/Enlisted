using System;
using HarmonyLib;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Assignments.Core;
using Enlisted.Mod.Core.Logging;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace Enlisted.Mod.GameAdapters.Patches
{
	/// <summary>
	/// Helper class to cache tooltip labels for wage breakdown display.
	/// </summary>
	internal static class WageTooltipLabels
	{
		public static TextObject BasePay;
		public static TextObject LevelBonus;
		public static TextObject TierBonus;
		public static TextObject ServiceBonus;
		public static TextObject ArmyBonus;
		public static TextObject DutyBonus;

		public static void EnsureInitialized()
		{
			if (BasePay == null)
			{
				BasePay = new TextObject("{=enlisted_base_pay}Soldier's Pay");
				LevelBonus = new TextObject("{=enlisted_level_bonus}Combat Exp");
				TierBonus = new TextObject("{=enlisted_tier_bonus}Rank Pay");
				ServiceBonus = new TextObject("{=enlisted_service_bonus}Service Seniority");
				ArmyBonus = new TextObject("{=enlisted_army_bonus}Army Campaign Bonus");
				DutyBonus = new TextObject("{=enlisted_duty_bonus}Duty Assignment");
			}
		}

		public static void ClearCache()
		{
			BasePay = null;
			LevelBonus = null;
			TierBonus = null;
			ServiceBonus = null;
			ArmyBonus = null;
			DutyBonus = null;
		}
	}

	/// <summary>
	/// Completely isolates the player clan's income calculation when enlisted.
	/// When enlisted, the player should ONLY see their enlistment wages, not any
	/// settlement income, party income, or other clan-based income that might leak
	/// from the lord's/army's finances due to party attachment.
	/// Shows a detailed breakdown of wage components in the tooltip.
	/// </summary>
	[HarmonyPatch(typeof(DefaultClanFinanceModel), nameof(DefaultClanFinanceModel.CalculateClanIncome))]
	internal static class ClanFinanceEnlistmentIncomePatch
	{
		private static bool _hasLoggedFirstWage = false;

		/// <summary>
		/// PREFIX: Intercept income calculation BEFORE native code runs.
		/// Matches signature: public override ExplainedNumber CalculateClanIncome(Clan clan, bool includeDescriptions = false, bool applyWithdrawals = false, bool includeDetails = false)
		/// </summary>
		private static bool Prefix(Clan clan, bool includeDescriptions, bool applyWithdrawals, bool includeDetails, ref ExplainedNumber __result)
		{
			try
			{
				// Skip during character creation
				if (Campaign.Current == null || Clan.PlayerClan == null)
				{
					return true; // Run native
				}

				// Only intercept for player clan
				if (clan != Clan.PlayerClan)
				{
					return true; // Run native for other clans
				}

				var enlistment = EnlistmentBehavior.Instance;
				bool isEnlisted = enlistment?.IsEnlisted == true;
				bool isInGracePeriod = enlistment?.IsInDesertionGracePeriod == true;
				var mainHero = Hero.MainHero;
				bool playerCaptured = mainHero?.IsPrisoner == true;

				// Only isolate finances when actively enlisted (not on leave) or captured
				if (!isEnlisted && !isInGracePeriod && !playerCaptured)
				{
					return true; // Run native when not enlisted
				}

				// CRITICAL: Create a fresh ExplainedNumber with ONLY enlistment wages
				// If captured or in grace period, this starts at 0 and stays at 0 (no wages added)
				__result = new ExplainedNumber(0f, includeDescriptions, null);

				// Add detailed wage breakdown
				if (isEnlisted)
				{
					AddWageBreakdownToResult(enlistment, ref __result);
				}

				return false; // Skip native
			}
			catch (Exception ex)
			{
				ModLogger.Error("Finance", $"Error in income isolation prefix: {ex.Message}");
				return true; // Fail open
			}
		}

		/// <summary>
		/// Adds each wage component as a separate line in the tooltip.
		/// </summary>
		internal static void AddWageBreakdownToResult(EnlistmentBehavior enlistment, ref ExplainedNumber result)
		{
			var breakdown = enlistment.GetWageBreakdown();
			if (breakdown.Total <= 0) return;

			WageTooltipLabels.EnsureInitialized();

			// Add each component as a separate tooltip line
			if (breakdown.BasePay > 0)
			{
				result.Add(breakdown.BasePay, WageTooltipLabels.BasePay, null);
			}

			if (breakdown.LevelBonus > 0)
			{
				result.Add(breakdown.LevelBonus, WageTooltipLabels.LevelBonus, null);
			}

			if (breakdown.TierBonus > 0)
			{
				result.Add(breakdown.TierBonus, WageTooltipLabels.TierBonus, null);
			}

			if (breakdown.ServiceBonus > 0)
			{
				result.Add(breakdown.ServiceBonus, WageTooltipLabels.ServiceBonus, null);
			}

			if (breakdown.ArmyBonus > 0)
			{
				result.Add(breakdown.ArmyBonus, WageTooltipLabels.ArmyBonus, null);
			}

			if (breakdown.DutyBonus > 0)
			{
				// Show duty name if available
				if (!string.IsNullOrEmpty(breakdown.ActiveDuty))
				{
					var dutyLabel = new TextObject("{=enlisted_duty_bonus_named}{DUTY} Bonus");
					dutyLabel.SetTextVariable("DUTY", breakdown.ActiveDuty);
					result.Add(breakdown.DutyBonus, dutyLabel, null);
				}
				else
				{
					result.Add(breakdown.DutyBonus, WageTooltipLabels.DutyBonus, null);
				}
			}

			if (!_hasLoggedFirstWage)
			{
				_hasLoggedFirstWage = true;
				ModLogger.Info("Finance", $"Wage breakdown - Base:{breakdown.BasePay} Level:{breakdown.LevelBonus} Tier:{breakdown.TierBonus} Service:{breakdown.ServiceBonus} Army:{breakdown.ArmyBonus} Duty:{breakdown.DutyBonus} = {breakdown.Total}");
			}
		}

		/// <summary>
		/// Clear cached tooltip labels when config is reloaded.
		/// </summary>
		internal static void ClearCache()
		{
			WageTooltipLabels.ClearCache();
		}
	}

	/// <summary>
	/// Completely isolates the player clan's expense calculation when enlisted.
	/// When enlisted, the player should have ZERO expenses - the lord pays for everything.
	/// This prevents any army/party/garrison expenses from appearing in the player's finances.
	/// </summary>
	[HarmonyPatch(typeof(DefaultClanFinanceModel), nameof(DefaultClanFinanceModel.CalculateClanExpenses))]
	internal static class ClanFinanceEnlistmentExpensePatch
	{
		private static bool _hasLoggedFirst = false;

		/// <summary>
		/// PREFIX: Intercept expense calculation BEFORE native code runs.
		/// Matches signature: public override ExplainedNumber CalculateClanExpenses(Clan clan, bool includeDescriptions = false, bool applyWithdrawals = false, bool includeDetails = false)
		/// </summary>
		private static bool Prefix(Clan clan, bool includeDescriptions, bool applyWithdrawals, bool includeDetails, ref ExplainedNumber __result)
		{
			try
			{
				if (Campaign.Current == null || Clan.PlayerClan == null) return true;
				if (clan != Clan.PlayerClan) return true;

				var enlistment = EnlistmentBehavior.Instance;
				bool isEnlisted = enlistment?.IsEnlisted == true;
				bool isInGracePeriod = enlistment?.IsInDesertionGracePeriod == true;
				var mainHero = Hero.MainHero;
				bool playerCaptured = mainHero?.IsPrisoner == true;

				// Isolate expenses when enlisted, in grace period, or captured
				if (!isEnlisted && !isInGracePeriod && !playerCaptured)
				{
					return true;
				}

				// CRITICAL: Return zero expenses when enlisted
				__result = new ExplainedNumber(0f, includeDescriptions, null);

				if (!_hasLoggedFirst)
				{
					_hasLoggedFirst = true;
					ModLogger.Info("Finance", "Expenses isolated - showing zero expenses while enlisted");
				}

				return false; // Skip native
			}
			catch (Exception ex)
			{
				ModLogger.Error("Finance", $"Error in expense isolation prefix: {ex.Message}");
				return true;
			}
		}
	}

	/// <summary>
	/// Forces the daily gold change calculation to use the public CalculateClanIncome/Expenses methods
	/// instead of the private Internal methods. This is required because:
	/// 1. Native CalculateClanGoldChange bypasses public overrides (calling Internal directly), which ignores our Income/Expense patches.
	/// 2. We need to combine Income + Expenses manually to preserve the detailed wage breakdown tooltips from Income.
	///
	/// This patch ensures both functionality (wage appears) and compatibility (other mods patching Income/Expenses will work).
	/// </summary>
	[HarmonyPatch(typeof(DefaultClanFinanceModel), nameof(DefaultClanFinanceModel.CalculateClanGoldChange))]
	internal static class ClanFinanceEnlistmentGoldChangePatch
	{
		private static bool _hasLoggedFirst = false;
		private static TextObject _expensesText;

		private static void EnsureInitialized()
		{
			if (_expensesText == null)
			{
				_expensesText = GameTexts.FindText("str_expenses", null);
			}
		}

		private static bool Prefix(DefaultClanFinanceModel __instance, Clan clan, bool includeDescriptions, bool applyWithdrawals, bool includeDetails, ref ExplainedNumber __result)
		{
			try
			{
				if (Campaign.Current == null || Clan.PlayerClan == null) return true;
				if (clan != Clan.PlayerClan) return true;

				var enlistment = EnlistmentBehavior.Instance;
				bool isEnlisted = enlistment?.IsEnlisted == true;

				// Only redirect when enlisted. When not enlisted, use native logic.
				if (!isEnlisted)
				{
					return true;
				}

				// 1. Calculate Income using the PUBLIC method (triggers our patch and others)
				// This returns an ExplainedNumber with the full wage breakdown (if enlisted)
				ExplainedNumber income = __instance.CalculateClanIncome(clan, includeDescriptions, applyWithdrawals, includeDetails);

				// 2. Calculate Expenses using the PUBLIC method (triggers our patch and others)
				// This returns 0 (if enlisted)
				ExplainedNumber expenses = __instance.CalculateClanExpenses(clan, includeDescriptions, applyWithdrawals, includeDetails);

				// 3. Combine them
				// We start with Income to preserve its tooltips.
				__result = income;

				// Add expenses (if any) to the result.
				if (Math.Abs(expenses.ResultNumber) > 0.001f)
				{
					EnsureInitialized();
					__result.Add(expenses.ResultNumber, _expensesText, null);
				}

				if (!_hasLoggedFirst)
				{
					_hasLoggedFirst = true;
					ModLogger.Info("Finance", $"GoldChange redirected - Income:{income.ResultNumber} Expenses:{expenses.ResultNumber}");
				}

				return false; // Skip native implementation (which calls Internal)
			}
			catch (Exception ex)
			{
				ModLogger.Error("Finance", $"Error in GoldChange redirection: {ex.Message}");
				return true; // Fail open
			}
		}
	}
}
