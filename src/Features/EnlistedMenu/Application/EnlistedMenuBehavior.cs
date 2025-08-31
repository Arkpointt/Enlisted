using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using Enlisted.Mod.Core.Logging;
using Enlisted.Features.Enlistment.Application;

namespace Enlisted.Features.EnlistedMenu.Application
{
	/// <summary>
	/// EnlistedMenuBehavior (CampaignBehaviorBase)
	/// Purpose: Owns enlisted status/report menus and menu options independently from enlistment lifecycle.
	/// Notes: Uses static state from EnlistmentBehavior to query enlistment and commander.
	/// </summary>
	public class EnlistedMenuBehavior : CampaignBehaviorBase
	{
		public override void RegisterEvents()
		{
			CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		}

		public override void SyncData(IDataStore dataStore)
		{
			// No persistent menu state required currently
		}

		private void OnSessionLaunched(CampaignGameStarter starter)
		{
			try
			{
				RegisterMenus(starter);
				LoggingService.Info("EnlistedMenuBehavior", "Enlisted menus registered.");
			}
			catch (Exception ex)
			{
				LoggingService.Exception("EnlistedMenuBehavior", ex, "OnSessionLaunched");
			}
		}

		private void RegisterMenus(CampaignGameStarter starter)
		{
			starter.AddGameMenu(
				"enlisted_soldier_status",
				"{=ENLIST_STATUS}You are serving as a soldier in this army.",
				new OnInitDelegate(OnSoldierStatusInit),
				GameOverlays.MenuOverlayType.None,
				GameMenu.MenuFlags.None);

			starter.AddGameMenu(
				"enlisted_status_report",
				"{=ENLIST_REPORT}You are serving in the army of {ENLIST_FACTION_NAME}. Your lord is {ENLIST_LORD_NAME}.",
				new OnInitDelegate(OnStatusReportInit),
				GameOverlays.MenuOverlayType.None,
				GameMenu.MenuFlags.None);

			starter.AddGameMenu(
				"enlisted_party_wait",
				"{=ENLIST_WAIT}You are waiting with {COMMANDER_NAME}'s party inside the settlement.",
				new OnInitDelegate(OnWaitMenuInit),
				GameOverlays.MenuOverlayType.None,
				GameMenu.MenuFlags.None);

			// Soldier Camp option (placeholder)
			starter.AddGameMenuOption(
				"enlisted_soldier_status",
				"enlisted_soldier_camp",
				"Soldier Camp",
				new GameMenuOption.OnConditionDelegate(OnSoldierStatusCondition),
				new GameMenuOption.OnConsequenceDelegate(OnSoldierStatusConsequence),
				false,
				-1,
				false);

			// Get Status Report
			starter.AddGameMenuOption(
				"enlisted_soldier_status",
				"enlisted_go_to_report",
				"Get Status Report",
				new GameMenuOption.OnConditionDelegate(OnSoldierStatusCondition),
				new GameMenuOption.OnConsequenceDelegate((MenuCallbackArgs args) => { TryOpenStatusReport(); }),
				true,
				-1,
				false);

			// Return to campaign
			starter.AddGameMenuOption(
				"enlisted_soldier_status",
				"enlisted_return_campaign",
				"Return to campaign",
				new GameMenuOption.OnConditionDelegate((MenuCallbackArgs args) => true),
				new GameMenuOption.OnConsequenceDelegate(OnReturnToCampaign),
				true,
				-1,
				false);

			// Back from report to status
			starter.AddGameMenuOption(
				"enlisted_status_report",
				"enlisted_report_back",
				"Back",
				new GameMenuOption.OnConditionDelegate(OnSoldierStatusCondition),
				new GameMenuOption.OnConsequenceDelegate((MenuCallbackArgs args) => { GameMenu.SwitchToMenu("enlisted_soldier_status"); }),
				true,
				-1,
				false);

			// Leave from report directly to campaign
			starter.AddGameMenuOption(
				"enlisted_status_report",
				"enlisted_report_leave",
				"Return to campaign",
				new GameMenuOption.OnConditionDelegate((MenuCallbackArgs args) => true),
				new GameMenuOption.OnConsequenceDelegate(OnReturnToCampaign),
				true,
				-1,
				false);
		}

		private static bool OnSoldierStatusCondition(MenuCallbackArgs args)
		{
			return EnlistmentBehavior.IsPlayerEnlisted;
		}

		private static void OnSoldierStatusConsequence(MenuCallbackArgs args)
		{
			// no-op placeholder
		}

		private static void OnReturnToCampaign(MenuCallbackArgs args)
		{
			try { GameMenu.ExitToLast(); } catch { }
		}

		private static void TryOpenStatusReport()
		{
			try { GameMenu.ActivateGameMenu("enlisted_status_report"); } catch { }
		}

		private static void OnWaitMenuInit(MenuCallbackArgs args)
		{
			var commander = EnlistmentBehavior.CurrentCommanderParty?.LeaderHero;
			if (commander != null)
			{
				args.MenuTitle = new TextObject($"Waiting with {commander.Name}");
			}
		}

		private static void OnStatusReportInit(MenuCallbackArgs args)
		{
			try
			{
				var commander = EnlistmentBehavior.CurrentCommanderParty?.LeaderHero;
				if (commander == null)
				{
					return;
				}
				var factionName = commander.MapFaction?.Name?.ToString() ?? "Unknown Faction";
				var lordName = commander.Name?.ToString() ?? "Unknown Lord";
				MBTextManager.SetTextVariable("ENLIST_FACTION_NAME", new TextObject(factionName));
				MBTextManager.SetTextVariable("ENLIST_LORD_NAME", new TextObject(lordName));
				// Keep time controls available
				try { if (Campaign.Current != null) Campaign.Current.TimeControlMode = CampaignTimeControlMode.StoppablePlay; } catch { }
				try { args.MenuContext?.GameMenu?.StartWait(); } catch { }
				try { Campaign.Current?.GameMenuManager?.RefreshMenuOptions(Campaign.Current.CurrentMenuContext); } catch { }
			}
			catch { }
		}

		private static void OnSoldierStatusInit(MenuCallbackArgs args)
		{
			var commander = EnlistmentBehavior.CurrentCommanderParty?.LeaderHero;
			if (commander != null)
			{
				args.MenuTitle = new TextObject($"You are serving under {commander.Name}");
			}
			try { if (Campaign.Current != null) Campaign.Current.TimeControlMode = CampaignTimeControlMode.StoppablePlay; } catch { }
			try { args.MenuContext?.GameMenu?.StartWait(); } catch { }
			try { Campaign.Current?.GameMenuManager?.RefreshMenuOptions(Campaign.Current.CurrentMenuContext); } catch { }
		}
	}
}


