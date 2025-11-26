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
	/// Injects enlistment wages into the vanilla clan finance calculation so the daily gold change UI
	/// shows service income. Patches the public CalculateClanIncome method for API stability.
	/// Configurable via enlisted_config.json finance section.
	/// </summary>
	[HarmonyPatch(typeof(DefaultClanFinanceModel), nameof(DefaultClanFinanceModel.CalculateClanIncome))]
	internal static class ClanFinanceEnlistmentIncomePatch
	{
		private static TextObject _cachedTooltipLabel;
		private static bool _hasLoggedFirstWage = false;

		private static void Postfix(Clan clan, ref ExplainedNumber __result)
		{
			try
			{
				// Skip during character creation when campaign isn't initialized
				if (Campaign.Current == null || Clan.PlayerClan == null)
				{
					return;
				}
				
				if (clan != Clan.PlayerClan)
				{
					return;
				}

				// Check if tooltip display is enabled in config
				var financeConfig = ConfigurationManager.LoadFinanceConfig();
				if (!financeConfig.ShowInClanTooltip)
				{
					return;
				}

				var enlistment = EnlistmentBehavior.Instance;
				if (enlistment?.TryGetProjectedDailyWage(out var wageAmount) == true && wageAmount > 0)
				{
					// Use configured tooltip label (cached for performance)
					if (_cachedTooltipLabel == null)
					{
						_cachedTooltipLabel = new TextObject(financeConfig.TooltipLabel);
					}
					
					__result.Add(wageAmount, _cachedTooltipLabel, null);
					
					// Log first successful wage integration for diagnostics (one-time only)
					if (!_hasLoggedFirstWage)
					{
						_hasLoggedFirstWage = true;
						Enlisted.Mod.Core.Logging.SessionDiagnostics.LogEvent("Finance", "WageIntegration",
							$"First wage added to tooltip: {wageAmount} gold");
					}
				}
			}
			catch (Exception ex)
			{
				ModLogger.Error("Finance", $"Failed to append enlistment wages to clan income: {ex.Message}");
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
}

