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
				LevelBonus = new TextObject("{=enlisted_level_bonus}Combat Experience");
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
		/// When enlisted, we completely replace the result with a detailed wage breakdown.
		/// This prevents the lord's castle income and other finances from appearing.
		/// </summary>
		private static bool Prefix(Clan clan, bool includeDescriptions, ref ExplainedNumber __result)
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

				// Only isolate finances when actively enlisted (not on leave)
				if (!isEnlisted && !isInGracePeriod)
				{
					return true; // Run native when not enlisted
				}

				// CRITICAL: Create a fresh ExplainedNumber with ONLY enlistment wages
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
		/// When enlisted, return zero expenses.
		/// </summary>
		private static bool Prefix(Clan clan, bool includeDescriptions, ref ExplainedNumber __result)
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

				// Isolate expenses when enlisted, in grace period, or captured
				if (!isEnlisted && !isInGracePeriod && !playerCaptured)
				{
					return true; // Run native when not enlisted
				}

				// CRITICAL: Return zero expenses when enlisted
				// The lord pays for food, troops, and all other expenses
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
				return true; // Fail open
			}
		}
	}
	
	/// <summary>
	/// Isolates the combined gold change calculation (income - expenses) when enlisted.
	/// This is the main method called by the UI to show daily gold change.
	/// Shows a detailed breakdown of all wage components.
	/// </summary>
	[HarmonyPatch(typeof(DefaultClanFinanceModel), nameof(DefaultClanFinanceModel.CalculateClanGoldChange))]
	internal static class ClanFinanceEnlistmentGoldChangePatch
	{
		private static bool _hasLoggedFirst = false;

		/// <summary>
		/// PREFIX: Intercept gold change calculation BEFORE native code runs.
		/// When enlisted, show detailed wage breakdown with zero expenses.
		/// </summary>
		private static bool Prefix(Clan clan, bool includeDescriptions, ref ExplainedNumber __result)
		{
			try
			{
				// Skip during character creation
				if (Campaign.Current == null || Clan.PlayerClan == null)
				{
					return true;
				}
				
				if (clan != Clan.PlayerClan)
				{
					return true;
				}

				var enlistment = EnlistmentBehavior.Instance;
				bool isEnlisted = enlistment?.IsEnlisted == true;
				bool isInGracePeriod = enlistment?.IsInDesertionGracePeriod == true;
				var mainHero = Hero.MainHero;
				bool playerCaptured = mainHero?.IsPrisoner == true;

				if (!isEnlisted && !isInGracePeriod && !playerCaptured)
				{
					return true;
				}

				// Create fresh result - we'll add detailed breakdown
				__result = new ExplainedNumber(0f, includeDescriptions, null);

				// Add detailed wage breakdown when enlisted (not when just captured)
				if (isEnlisted)
				{
					ClanFinanceEnlistmentIncomePatch.AddWageBreakdownToResult(enlistment, ref __result);
				}

				if (!_hasLoggedFirst)
				{
					_hasLoggedFirst = true;
					string status = isEnlisted ? "enlisted" : (isInGracePeriod ? "grace period" : "captured");
					ModLogger.Info("Finance", $"Gold change isolated ({status}) - showing detailed wage breakdown");
				}

				return false;
			}
			catch (Exception ex)
			{
				ModLogger.Error("Finance", $"Error in gold change isolation prefix: {ex.Message}");
				return true;
			}
		}
	}
}
