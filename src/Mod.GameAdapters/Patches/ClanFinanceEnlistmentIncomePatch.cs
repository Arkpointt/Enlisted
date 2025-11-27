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
	/// Completely isolates the player clan's income calculation when enlisted.
	/// When enlisted, the player should ONLY see their enlistment wages, not any
	/// settlement income, party income, or other clan-based income that might leak
	/// from the lord's/army's finances due to party attachment.
	/// </summary>
	[HarmonyPatch(typeof(DefaultClanFinanceModel), nameof(DefaultClanFinanceModel.CalculateClanIncome))]
	internal static class ClanFinanceEnlistmentIncomePatch
	{
		private static TextObject _cachedTooltipLabel;
		private static bool _hasLoggedFirstWage = false;

		/// <summary>
		/// PREFIX: Intercept income calculation BEFORE native code runs.
		/// When enlisted, we completely replace the result with only enlistment wages.
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
				bool isOnLeave = enlistment?.IsOnLeave == true;

				// Only isolate finances when actively enlisted (not on leave)
				if (!isEnlisted && !isInGracePeriod)
				{
					return true; // Run native when not enlisted
				}

				// CRITICAL: Create a fresh ExplainedNumber with ONLY enlistment wages
				// This prevents any lord/army/settlement income from leaking through
				__result = new ExplainedNumber(0f, includeDescriptions, null);

				// Add enlistment wages as the only income source
				if (enlistment?.TryGetProjectedDailyWage(out var wageAmount) == true && wageAmount > 0)
				{
					var financeConfig = ConfigurationManager.LoadFinanceConfig();
					if (_cachedTooltipLabel == null)
					{
						_cachedTooltipLabel = new TextObject(financeConfig.TooltipLabel);
					}
					
					__result.Add(wageAmount, _cachedTooltipLabel, null);
					
					if (!_hasLoggedFirstWage)
					{
						_hasLoggedFirstWage = true;
						ModLogger.Info("Finance", $"Income isolated - showing only enlistment wages: {wageAmount} gold");
					}
				}

				return false; // Skip native - we've completely replaced the result
			}
			catch (Exception ex)
			{
				ModLogger.Error("Finance", $"Error in income isolation prefix: {ex.Message}");
				return true; // Fail open - allow native on error
			}
		}
		
		/// <summary>
		/// Clear cached tooltip label when config is reloaded.
		/// </summary>
		internal static void ClearCache()
		{
			_cachedTooltipLabel = null;
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
	/// </summary>
	[HarmonyPatch(typeof(DefaultClanFinanceModel), nameof(DefaultClanFinanceModel.CalculateClanGoldChange))]
	internal static class ClanFinanceEnlistmentGoldChangePatch
	{
		private static TextObject _cachedTooltipLabel;
		private static bool _hasLoggedFirst = false;

		/// <summary>
		/// PREFIX: Intercept gold change calculation BEFORE native code runs.
		/// When enlisted, show only enlistment wages with zero expenses.
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

				// Create fresh result with only enlistment wages
				__result = new ExplainedNumber(0f, includeDescriptions, null);

				// Add wages when enlisted (not when just captured)
				if (isEnlisted && enlistment?.TryGetProjectedDailyWage(out var wageAmount) == true && wageAmount > 0)
				{
					var financeConfig = ConfigurationManager.LoadFinanceConfig();
					if (_cachedTooltipLabel == null)
					{
						_cachedTooltipLabel = new TextObject(financeConfig.TooltipLabel);
					}
					
					__result.Add(wageAmount, _cachedTooltipLabel, null);
				}

				if (!_hasLoggedFirst)
				{
					_hasLoggedFirst = true;
					string status = isEnlisted ? "enlisted" : (isInGracePeriod ? "grace period" : "captured");
					ModLogger.Info("Finance", $"Gold change isolated ({status}) - only showing enlistment wages");
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
