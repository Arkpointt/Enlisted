using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map;
using TaleWorlds.CampaignSystem.Overlay;
using HarmonyLib;
using System.Reflection;
using Enlisted.Features.Enlistment.Domain;
using Enlisted.Mod.Core.Logging;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Actions;
using System.Collections.Generic;

namespace Enlisted.Features.Enlistment.Application
{
	/// <summary>
	/// EnlistmentBehavior (CampaignBehaviorBase)
	/// Purpose: Owns enlistment lifecycle (dialogs, state persistence, commander following, menus) using public Campaign APIs.
	/// APIs (verify with Bannerlord 1.2.12 docs: https://apidoc.bannerlord.com/v/1.2.12/):
	/// - CampaignEvents.* (OnSessionLaunchedEvent, OnGameLoadedEvent, TickEvent)
	/// - CampaignGameStarter.AddPlayerLine/AddDialogLine/AddGameMenu/AddGameMenuOption
	/// - GameMenu.ActivateGameMenu / GameMenu.ExitToLast
	/// - PlayerEncounter.Finish
	/// - PartyBase.SetAsCameraFollowParty
	/// - MobileParty.MainParty / MobileParty.Ai.SetMoveEscortParty
	/// Safety: Campaign-only. All engine calls are null-guarded; menus are drained before switching.
	/// Notes: Hidden MainParty visuals and map tracker suppression are coordinated with Harmony patches in Mod.GameAdapters.
	/// </summary>
	public class EnlistmentBehavior : CampaignBehaviorBase
	{
		public static bool IsPlayerEnlisted;
		public static MobileParty CurrentCommanderParty;
		private EnlistmentState _state;
		private PartySnapshot _snapshot;
		private bool _pendingCameraFollow;
		private PartyBase _pendingFollowParty;
		private MobileParty _trackedCommander;
		private float _escortRefreshTimer;
		private float _cameraFollowTimer;
		private bool _waitMenuActive;
		private bool _commanderInSettlementPrev;
		private float _visualHideTimer;
		private bool _pendingCloseEnlistedMenu;
		private bool _pendingOpenStatusMenu;
		// Deprecated countdown/flag kept previously for auto-close behavior; removed to avoid warnings
		private bool _autoCloseStatusOnInit;
		private static readonly bool UseSettlementWaitMenu = false; // Freelancer parity: do not auto-open wait menu on settlement entry
		private static readonly bool PromptOnSettlementEntry = true; // Pause and prompt when lord enters town/castle
		private bool _isPromptingSettlement;
		private Settlement _pendingSettlementPrompt;
		private bool _pendingEnterSettlement;
		private Settlement _pendingEnterTarget;

		// Diagnostic helpers
		private string GetActiveMenuId()
		{
			try
			{
				var ctx = Campaign.Current?.CurrentMenuContext;
				if (ctx == null)
				{
					return "<none>";
				}
				var gameMenuProp = ctx.GetType().GetProperty("GameMenu", BindingFlags.Public | BindingFlags.Instance);
				var gameMenu = gameMenuProp?.GetValue(ctx);
				if (gameMenu == null)
				{
					return "<context-no-gamemenu>";
				}
				var idProp = gameMenu.GetType().GetProperty("StringId", BindingFlags.Public | BindingFlags.Instance);
				var id = idProp?.GetValue(gameMenu) as string;
				return string.IsNullOrEmpty(id) ? gameMenu.GetType().Name : id;
			}
			catch (Exception ex)
			{
				LoggingService.Exception("EnlistmentBehavior", ex, "GetActiveMenuId");
				return "<error>";
			}
		}

		private void LogState(string stage)
		{
			try
			{
				var menuId = GetActiveMenuId();
				bool hasEncounter = PlayerEncounter.Current != null;
				LoggingService.Info("EnlistmentBehavior", $"STATE[{stage}] menu={menuId} hasEncounter={hasEncounter}");
			}
			catch (Exception ex)
			{
				LoggingService.Exception("EnlistmentBehavior", ex, $"LogState[{stage}]");
			}
		}

		// Open wait menu robustly by draining any active menu and pausing time
		private void OpenWaitMenuSafely()
		{
			try
			{
				int safety = 4;
				while (Campaign.Current?.CurrentMenuContext != null && safety-- > 0)
				{
					try { GameMenu.ExitToLast(); } catch { break; }
				}
				GameMenu.ActivateGameMenu("enlisted_party_wait");
				if (Campaign.Current != null)
				{
					Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
				}
			}
			catch (Exception ex)
			{
				LoggingService.Exception("EnlistmentBehavior", ex, "OpenWaitMenuSafely");
			}
		}

		private sealed class PartySnapshot
		{
			public int Gold;
			public Vec2 MapPos;
		}

		public EnlistmentBehavior()
		{
			_state = new EnlistmentState();
		}

		public override void RegisterEvents()
		{
			CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
			CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
			CampaignEvents.TickEvent.AddNonSerializedListener(this, OnTick);
			// Log when player enters any settlement
			try { CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered); } catch { }
			
			
			
		}

		public override void SyncData(IDataStore dataStore)
		{
			// Primary: let the save system handle our state object if available
			dataStore.SyncData("_enlistmentState", ref _state);
			// Fallback: persist minimal fields to maintain enlisted status across loads
			string commanderId = null; bool isEnlisted = false; CampaignTime enlistTime = default; int enlistTier = 0;
			List<EquipmentElement> storedEq = null; List<ItemObject> storedItems = null;
			if (dataStore.IsSaving)
			{
				try
				{
					_state?.ExtractForSave(out commanderId, out isEnlisted, out enlistTime, out enlistTier);
					if (_state != null)
					{
						var loadout = _state.GetStoredLoadout();
						storedEq = loadout.equipment; storedItems = loadout.items;
					}
					dataStore.SyncData("_enlistment_commanderId", ref commanderId);
					dataStore.SyncData("_enlisted_flag", ref isEnlisted);
					dataStore.SyncData("_enlist_time", ref enlistTime);
					dataStore.SyncData("_enlist_tier", ref enlistTier);
					dataStore.SyncData("_enlist_stored_eq", ref storedEq);
					dataStore.SyncData("_enlist_stored_items", ref storedItems);
				}
				catch { }
			}
			else if (dataStore.IsLoading)
			{
				try
				{
					dataStore.SyncData("_enlistment_commanderId", ref commanderId);
					dataStore.SyncData("_enlisted_flag", ref isEnlisted);
					dataStore.SyncData("_enlist_time", ref enlistTime);
					dataStore.SyncData("_enlist_tier", ref enlistTier);
					dataStore.SyncData("_enlist_stored_eq", ref storedEq);
					dataStore.SyncData("_enlist_stored_items", ref storedItems);
					if (_state == null) _state = new EnlistmentState();
					_state.RestoreFromSave(commanderId, isEnlisted, enlistTime, enlistTier);
					if (storedEq != null || storedItems != null) _state.RestoreStoredLoadout(storedEq, storedItems);
				}
				catch { }
			}
		}

		private void OnSessionLaunched(CampaignGameStarter campaignStarter)
		{
			LoggingService.Info("EnlistmentBehavior", "Session launched, registering enlistment dialogs...");
			
			// Log API call for debugging
			LoggingService.LogApiCall("CampaignGameStarter", "AddPlayerLine", 
				new object[] { "enlistment_start", "lord_talk_speak_diplomatic", "enlistment_ask_to_join" });
			
			AddEnlistmentDialog(campaignStarter);
			AddEnlistmentMenus(campaignStarter);
			
			LoggingService.Info("EnlistmentBehavior", "Enlistment dialogs registered successfully");
		}

		private void OnGameLoaded(CampaignGameStarter campaignStarter)
		{
			// Don't re-register dialogs on game load to avoid duplicates
			LoggingService.Info("EnlistmentBehavior", "Game loaded, dialogs already registered");
			// Re-apply enlistment effects if a save was loaded while enlisted
			try { ReapplyEnlistmentState(); } catch (System.Exception ex) { LoggingService.Exception("EnlistmentBehavior", ex, "ReapplyEnlistmentState on load"); }
		}

		private void OnTick(float dt)
		{
			if (_pendingCameraFollow && _pendingFollowParty != null)
			{
				try
				{
					_pendingFollowParty.SetAsCameraFollowParty();
				}
				catch { }
				finally
				{
					_pendingCameraFollow = false;
					_pendingFollowParty = null;
				}
			}

			// Ensure commander behaviors while enlisted
			if (_state.IsEnlisted && _trackedCommander != null)
			{
				// Enforce hidden main party visuals each tick, since engine/UI may respawn them
				try { MobileParty.MainParty.IsVisible = false; } catch { }
				_visualHideTimer += dt;
				if (_visualHideTimer >= 2.0f)
				{
					_visualHideTimer = 0f;
					try { TryUntrackAndDespawn(MobileParty.MainParty); } catch { }
				}

				// Aggressively refresh escort and camera to prevent manual control and keep follow locked
				_escortRefreshTimer += dt;
				if (_escortRefreshTimer >= 0.2f)
				{
					_escortRefreshTimer = 0f;
					try
					{
						if (MobileParty.MainParty?.Ai != null)
						{
							MobileParty.MainParty.Ai.SetMoveEscortParty(_trackedCommander);
							// Ensure main party is not tracked by the visual tracker (hide shield)
							try
							{
								var vtm = Campaign.Current?.VisualTrackerManager;
								var mUnreg =
									AccessTools.Method(vtm?.GetType(), "UnregisterObject") ??
									AccessTools.Method(vtm?.GetType(), "RemoveTrackedObject") ??
									AccessTools.Method(vtm?.GetType(), "RemoveObject");
								mUnreg?.Invoke(vtm, new object[] { MobileParty.MainParty });
							}
							catch { }
						}
					}
					catch { }
				}

				// Keep camera following the commander steadily
				_cameraFollowTimer += dt;
				if (_cameraFollowTimer >= 0.5f)
				{
					_cameraFollowTimer = 0f;
					try { _trackedCommander.Party.SetAsCameraFollowParty(); } catch { }
				}

				// Settlement handling
				try
				{
					var commanderSettlement = _trackedCommander.CurrentSettlement;
					bool commanderInSettlement = commanderSettlement != null;
					bool playerInEncounter = PlayerEncounter.Current != null;

					if (UseSettlementWaitMenu)
					{
						if (!_commanderInSettlementPrev && commanderInSettlement && !_waitMenuActive && !playerInEncounter)
						{
							OpenWaitMenuSafely();
							// Untrack main party to prevent shield inside settlements
							try
							{
								var vtm = Campaign.Current?.VisualTrackerManager;
								var mUnreg =
									AccessTools.Method(vtm?.GetType(), "UnregisterObject") ??
									AccessTools.Method(vtm?.GetType(), "RemoveTrackedObject") ??
									AccessTools.Method(vtm?.GetType(), "RemoveObject");
								mUnreg?.Invoke(vtm, new object[] { MobileParty.MainParty });
							}
							catch { }
							_waitMenuActive = true;
							LoggingService.Debug("EnlistmentBehavior", $"Activated enlisted_party_wait for settlement: {commanderSettlement?.Name}");
						}
						else if (_commanderInSettlementPrev && !commanderInSettlement && _waitMenuActive)
						{
							try { GameMenu.ExitToLast(); } catch { }
							_waitMenuActive = false;
							LoggingService.Debug("EnlistmentBehavior", "Closed enlisted_party_wait; commander left settlement");
							try { MobileParty.MainParty?.Ai?.SetMoveEscortParty(_trackedCommander); } catch { }
						}
					}
					else
					{
						// Freelancer parity: do not auto-open menus on settlement entry
						// Optional: Pause and prompt when entering towns/castles
						if (PromptOnSettlementEntry && !_commanderInSettlementPrev && commanderInSettlement && !playerInEncounter)
						{
							if ((commanderSettlement?.IsTown ?? false) || (commanderSettlement?.IsCastle ?? false))
							{
								if (!_isPromptingSettlement)
								{
									_isPromptingSettlement = true;
									_pendingSettlementPrompt = commanderSettlement;
									try { Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop; } catch { }
									try { GameMenu.ActivateGameMenu("enlisted_soldier_status"); } catch { }
								}
							}
						}
						// If commander just left a town/castle and player is inside one, leave with commander
						if (_commanderInSettlementPrev && !commanderInSettlement)
						{
							try
							{
								var playerSettlement = MobileParty.MainParty?.CurrentSettlement;
								if (playerSettlement != null && (playerSettlement.IsTown || playerSettlement.IsCastle))
								{
									int drain2 = 5;
									while (Campaign.Current?.CurrentMenuContext != null && drain2-- > 0)
									{
										try { GameMenu.ExitToLast(); } catch { break; }
									}
									TaleWorlds.CampaignSystem.Actions.LeaveSettlementAction.ApplyForParty(MobileParty.MainParty);
									try { MobileParty.MainParty?.Ai?.SetMoveEscortParty(_trackedCommander); } catch { }
									try { Campaign.Current.TimeControlMode = CampaignTimeControlMode.StoppablePlay; } catch { }
								}
							}
							catch { }
						}
						_commanderInSettlementPrev = commanderInSettlement;
					}
				}
				catch (Exception ex)
				{
					LoggingService.Exception("EnlistmentBehavior", ex, "Settlement handling");
				}

				// Deferred enter-settlement execution (run outside of menu callback to avoid crashes)
				if (_pendingEnterSettlement)
				{
					try
					{
						// Drain menus safely now that we're on tick
						int drain = 5;
						while (Campaign.Current?.CurrentMenuContext != null && drain-- > 0)
						{
							try { GameMenu.ExitToLast(); } catch { break; }
						}
						// If already inside, skip
						if (MobileParty.MainParty?.CurrentSettlement != null)
						{
							LoggingService.Warning("EnlistmentBehavior", "DeferredEnter: already inside settlement; skipping");
							_pendingEnterSettlement = false;
							_isPromptingSettlement = false;
							_pendingEnterTarget = null;
						}
						else
						{
							var s = _pendingEnterTarget ?? _trackedCommander?.CurrentSettlement;
							if (s != null && (s.IsTown || s.IsCastle))
							{
								LoggingService.Info("EnlistmentBehavior", $"DeferredEnter: applying EnterSettlement for {s.Name}");
								try { EnterSettlementAction.ApplyForParty(MobileParty.MainParty, s); } catch (System.Exception ex) { LoggingService.Exception("EnlistmentBehavior", ex, "DeferredEnter: ApplyForParty"); }
							}
							else
							{
								LoggingService.Warning("EnlistmentBehavior", "DeferredEnter: no valid settlement target");
							}
							_pendingEnterSettlement = false;
							_isPromptingSettlement = false;
							_pendingEnterTarget = null;
						}
					}
					catch { }
				}

				// Proximity clamp: keep main party within range of commander on the campaign map
				try
				{
					var hasMenu2 = Campaign.Current?.CurrentMenuContext != null;
					var inEncounter2 = PlayerEncounter.Current != null;
					var commanderInSettlement2 = _trackedCommander.CurrentSettlement != null;
					var playerInSettlement = MobileParty.MainParty?.CurrentSettlement != null;
					if (!hasMenu2 && !inEncounter2 && !commanderInSettlement2 && !playerInSettlement)
					{
						var playerPos = MobileParty.MainParty.Position2D;
						var commanderPos = _trackedCommander.Position2D;
						var delta = commanderPos - playerPos;
						if (delta.LengthSquared > 9.0f) // > 3 units squared to avoid rubber-banding
						{
							var dir = delta;
							dir.Normalize();
							// Place player 1 unit behind commander to avoid overlap
							var target = commanderPos - dir * 1.0f;
							MobileParty.MainParty.Position2D = target;
							// Reassert escort
							try { MobileParty.MainParty.Ai.SetMoveEscortParty(_trackedCommander); } catch { }
							LoggingService.Debug("EnlistmentBehavior", $"Proximity clamp to commander. NewPos={target}");
						}
					}
				}
				catch { }
			}

			// If we just enlisted, open our status menu once
			if (_pendingOpenStatusMenu && _state.IsEnlisted)
			{
				try
				{
					// Use ActivateGameMenu because after conversation there may be no active menu
					var hasMenuCtx = Campaign.Current?.CurrentMenuContext != null;
					if (hasMenuCtx)
					{
						LoggingService.Debug("EnlistmentBehavior", "Opening enlisted_soldier_status via SwitchToMenu");
						GameMenu.SwitchToMenu("enlisted_soldier_status");
					}
					else
					{
						LoggingService.Debug("EnlistmentBehavior", "Opening enlisted_soldier_status via ActivateGameMenu");
						GameMenu.ActivateGameMenu("enlisted_soldier_status");
					}
					// menu opened
					LoggingService.Info("EnlistmentBehavior", "Opened enlisted_soldier_status successfully");
				}
				catch
				{
					// open failed
					LoggingService.Warning("EnlistmentBehavior", "Failed to open enlisted_soldier_status (ActivateGameMenu threw)");
				}
				_pendingOpenStatusMenu = false;
				// keep menu open until player leaves
			}

			// Removed auto-close behavior to ensure status menu stays visible

			// Auto-close enlisted menu next tick so time continues (Freelancer-style)
			if (_pendingCloseEnlistedMenu)
			{
				var hasMenu = Campaign.Current?.CurrentMenuContext != null;
				if (hasMenu)
				{
					try 
					{ 
						LoggingService.Debug("EnlistmentBehavior", $"ExitToLast before: activeMenu={GetActiveMenuId()} hasEncounter={(PlayerEncounter.Current!=null)}");
						TaleWorlds.CampaignSystem.GameMenus.GameMenu.ExitToLast(); 
						LoggingService.Debug("EnlistmentBehavior", $"ExitToLast after: activeMenu={GetActiveMenuId()} hasEncounter={(PlayerEncounter.Current!=null)}");
					} 
					catch (Exception ex) 
					{ 
						LoggingService.Exception("EnlistmentBehavior", ex, "ExitToLast in OnTick"); 
					}
				}
				else
				{
					LoggingService.Debug("EnlistmentBehavior", "Skip ExitToLast; no active menu");
				}
				_pendingCloseEnlistedMenu = false;
			}

			// While our enlisted menus are open, force StoppablePlay so spacebar/arrows work
			try
			{
				var menuCtx = Campaign.Current?.CurrentMenuContext;
				if (menuCtx != null)
				{
					var activeId = GetActiveMenuId();
					if (activeId == "enlisted_soldier_status" || activeId == "enlisted_status_report")
					{
						if (Campaign.Current.TimeControlMode == CampaignTimeControlMode.Stop)
						{
							Campaign.Current.TimeControlMode = CampaignTimeControlMode.StoppablePlay;
						}
						// If an encounter is still active, finish it so time controls are not blocked
						if (PlayerEncounter.Current != null)
						{
							LoggingService.Debug("EnlistmentBehavior", "Finishing lingering PlayerEncounter while enlisted menu is open");
							try { PlayerEncounter.Finish(true); } catch { }
							// Drain any leftover menus opened by encounter
							int safetyDrain = 3;
							while (Campaign.Current?.CurrentMenuContext != null && safetyDrain-- > 0)
							{
								try { GameMenu.ExitToLast(); } catch { break; }
							}
							// Re-open our status menu without pausing
							try { GameMenu.ActivateGameMenu("enlisted_soldier_status"); } catch { }
							try { Campaign.Current.TimeControlMode = CampaignTimeControlMode.StoppablePlay; } catch { }
						}
					}
				}
			}
			catch { }
		}
		

		
		


		private void AddEnlistmentDialog(CampaignGameStarter campaignStarter)
		{
			LoggingService.Info("EnlistmentBehavior", "Adding enlistment dialog lines...");
			
			// Add enlistment option to the main conversation menu
			campaignStarter.AddPlayerLine(
				"enlistment_start",
				"hero_main_options",  // Main conversation menu
				"enlistment_ask_to_join",
				"I would like to enlist in your army.",
				new ConversationSentence.OnConditionDelegate(CanAskToEnlist),
				new ConversationSentence.OnConsequenceDelegate(OnEnlistmentRequested),
				ConversationSentence.DefaultPriority + 200,  // High priority
				// clickableCondition: show greyed-out with reason if condition fails
				(out TextObject reason) => {
					var h = Hero.OneToOneConversationHero;
					if (h == null) { reason = new TextObject("No conversed hero."); return false; }
					if (_state.IsEnlisted) { reason = new TextObject("Already enlisted."); return false; }
					if (h.PartyBelongedTo == null) { reason = new TextObject("Target has no party."); return false; }
					reason = new TextObject("Eligible.");
					return true;
				}
			);
			
			
			
			// Lord's response based on reputation
			campaignStarter.AddDialogLine("enlistment_accept", 
				EnlistmentDialogs.DialogIds.AskToJoin, 
				EnlistmentDialogs.DialogIds.Accepted, 
				EnlistmentDialogs.EnlistmentRequest.LordAccept, 
				new ConversationSentence.OnConditionDelegate(ShouldAcceptEnlistment),
				null);
			
			campaignStarter.AddDialogLine("enlistment_decline", 
				EnlistmentDialogs.DialogIds.AskToJoin, 
				"lord_talk_speak_diplomatic", 
				EnlistmentDialogs.EnlistmentRequest.LordDecline, 
				new ConversationSentence.OnConditionDelegate(ShouldDeclineEnlistment),
				null);
			
			// Confirmation dialog - end conversation cleanly
			campaignStarter.AddPlayerLine(EnlistmentDialogs.DialogIds.Confirm, 
				EnlistmentDialogs.DialogIds.Accepted, 
				"close_window", 
				EnlistmentDialogs.EnlistmentRequest.PlayerConfirm, 
				null, 
				new ConversationSentence.OnConsequenceDelegate(OnEnlistmentConfirmed));

			// Add leave conversation options for enlisted soldiers
			campaignStarter.AddPlayerLine(EnlistmentDialogs.DialogIds.AskLeave, 
				"lord_talk_speak_diplomatic", 
				EnlistmentDialogs.DialogIds.LeaveRequest, 
				EnlistmentDialogs.LeaveRequest.PlayerRequest, 
				new ConversationSentence.OnConditionDelegate(CanAskForLeaveDialog),
				new ConversationSentence.OnConsequenceDelegate(OnLeaveRequested));

			// Lord's response to leave request
			campaignStarter.AddDialogLine("enlistment_leave_accept", 
				EnlistmentDialogs.DialogIds.LeaveRequest, 
				EnlistmentDialogs.DialogIds.LeaveAccepted, 
				EnlistmentDialogs.LeaveRequest.CommanderAccept, 
				new ConversationSentence.OnConditionDelegate(ShouldAcceptLeave),
				new ConversationSentence.OnConsequenceDelegate(OnLeaveAccepted));

			campaignStarter.AddDialogLine("enlistment_leave_decline", 
				EnlistmentDialogs.DialogIds.LeaveRequest, 
				"close_window", 
				EnlistmentDialogs.LeaveRequest.CommanderDecline, 
				new ConversationSentence.OnConditionDelegate(ShouldDeclineLeave),
				new ConversationSentence.OnConsequenceDelegate(OnLeaveDeclined));

			// Confirmation for leaving - end conversation cleanly
			campaignStarter.AddPlayerLine(EnlistmentDialogs.DialogIds.LeaveConfirm, 
				EnlistmentDialogs.DialogIds.LeaveAccepted, 
				"close_window", 
				EnlistmentDialogs.LeaveRequest.PlayerConfirm, 
				null, 
				new ConversationSentence.OnConsequenceDelegate(OnLeaveConfirmed));
		}

		// Enlisted status menu (Freelancer-style): shows after successful enlist
		private void AddEnlistmentMenus(CampaignGameStarter campaignStarter)
		{
			// Soldier status / camp menu
			campaignStarter.AddGameMenu(
				"enlisted_soldier_status",
				"{=ENLIST_STATUS}You are serving as a soldier in this army.",
				new OnInitDelegate(OnSoldierStatusInit),
				GameOverlays.MenuOverlayType.None,
				GameMenu.MenuFlags.None);

			// Detailed status report (separate screen like Freelancer's report)
			campaignStarter.AddGameMenu(
				"enlisted_status_report",
				"{=ENLIST_REPORT}You are serving in the army of {ENLIST_FACTION_NAME}. Your lord is {ENLIST_LORD_NAME}.\nYour reputation with {ENLIST_LORD_NAME} is {ENLIST_RELATION}.\nYou have served {ENLIST_DAYS} days as tier {ENLIST_TIER}.",
				new OnInitDelegate(OnStatusReportInit),
				GameOverlays.MenuOverlayType.None,
				GameMenu.MenuFlags.None);

			// SAS-style wait menu while commander is inside a settlement (use standard menu for broad compatibility)
			campaignStarter.AddGameMenu(
				"enlisted_party_wait",
				"{=ENLIST_WAIT}You are waiting with {COMMANDER_NAME}'s party inside the settlement.",
				new OnInitDelegate(OnWaitMenuInit),
				GameOverlays.MenuOverlayType.None,
				GameMenu.MenuFlags.None);

			// Soldier Camp option (placeholder for now)
			campaignStarter.AddGameMenuOption(
				"enlisted_soldier_status",
				"enlisted_soldier_camp",
				"Soldier Camp",
				new GameMenuOption.OnConditionDelegate(OnSoldierStatusCondition),
				new GameMenuOption.OnConsequenceDelegate(OnSoldierStatusConsequence),
				false,
				-1,
				false);

			// Enter settlement with commander (towns/castles only)
			campaignStarter.AddGameMenuOption(
				"enlisted_soldier_status",
				"enlisted_enter_settlement",
				"Enter settlement with your lord",
				new GameMenuOption.OnConditionDelegate(OnEnterSettlementCondition),
				new GameMenuOption.OnConsequenceDelegate(OnEnterSettlementConsequence),
				true,
				-1,
				false);

			// Get Status Report – navigates to detailed report menu
			campaignStarter.AddGameMenuOption(
				"enlisted_soldier_status",
				"enlisted_go_to_report",
				"Get Status Report",
				new GameMenuOption.OnConditionDelegate(OnSoldierStatusCondition),
				new GameMenuOption.OnConsequenceDelegate((MenuCallbackArgs args) => { TryOpenStatusReport(); }),
				true,
				-1,
				false);

			// Leave back to campaign from status menu
			campaignStarter.AddGameMenuOption(
				"enlisted_soldier_status",
				"enlisted_return_campaign",
				"Return to campaign",
				new GameMenuOption.OnConditionDelegate((MenuCallbackArgs args) => true),
				new GameMenuOption.OnConsequenceDelegate((MenuCallbackArgs args) => { _isPromptingSettlement = false; OnReturnToCampaign(args); }),
				true,
				-1,
				false);

			// Back from report to status
			campaignStarter.AddGameMenuOption(
				"enlisted_status_report",
				"enlisted_report_back",
				"Back",
				new GameMenuOption.OnConditionDelegate(OnSoldierStatusCondition),
				new GameMenuOption.OnConsequenceDelegate((MenuCallbackArgs args) => { GameMenu.SwitchToMenu("enlisted_soldier_status"); }),
				true,
				-1,
				false);

			// Leave from report directly to campaign
			campaignStarter.AddGameMenuOption(
				"enlisted_status_report",
				"enlisted_report_leave",
				"Return to campaign",
				new GameMenuOption.OnConditionDelegate((MenuCallbackArgs args) => true),
				new GameMenuOption.OnConsequenceDelegate(OnReturnToCampaign),
				true,
				-1,
				false);

			// Desert the army – reuse leave logic
			campaignStarter.AddGameMenuOption(
				"enlisted_soldier_status",
				"enlisted_desert_army",
				"Desert the army",
				new GameMenuOption.OnConditionDelegate(OnSoldierStatusCondition),
				new GameMenuOption.OnConsequenceDelegate((MenuCallbackArgs args) => { OnLeaveConfirmed(); }),
				false,
				-1,
				false);
		}

		private void TryOpenStatusReport()
		{
			try
			{
				LoggingService.Debug("EnlistmentBehavior", "Switching to enlisted_status_report");
				GameMenu.ActivateGameMenu("enlisted_status_report");
			}
			catch { }
		}

		private void OnWaitMenuInit(MenuCallbackArgs args)
		{
			if (_state.Commander != null)
			{
				args.MenuTitle = new TextObject(EnlistmentDialogs.GetSoldierStatusTitle(_state.Commander.Name.ToString()));
			}
		}

		private void OnStatusReportInit(MenuCallbackArgs args)
		{
			try
			{
				LoggingService.Debug("EnlistmentBehavior", "OnStatusReportInit called");
				var commander = _state.Commander;
				if (commander == null)
				{
					LoggingService.Warning("EnlistmentBehavior", "OnStatusReportInit: commander was null");
					return;
				}

				// Keep ribbon time controls available while report is open
				try { if (Campaign.Current != null) Campaign.Current.TimeControlMode = CampaignTimeControlMode.StoppablePlay; } catch { }
				// Match SAS/Freelancer: mark menu as wait-active so time controls work while menu is shown
				try { args.MenuContext?.GameMenu?.StartWait(); } catch { }

				// Compose report variables (relation-only focus per design)
				var factionName = commander.MapFaction?.Name?.ToString() ?? "Unknown Faction";
				var lordName = commander.Name?.ToString() ?? "Unknown Lord";
				int days = _state.GetDaysServed();
				int tier = _state.EnlistTier;
				int relation = Hero.MainHero != null ? Hero.MainHero.GetRelation(commander) : 0;

				MBTextManager.SetTextVariable("ENLIST_FACTION_NAME", new TextObject(factionName));
				MBTextManager.SetTextVariable("ENLIST_LORD_NAME", new TextObject(lordName));
				MBTextManager.SetTextVariable("ENLIST_DAYS", days);
				MBTextManager.SetTextVariable("ENLIST_TIER", tier);
				MBTextManager.SetTextVariable("ENLIST_RELATION", relation);

				args.MenuTitle = new TextObject("Status Report");
				// Refresh menu options after variables/flags are set
				try { Campaign.Current?.GameMenuManager?.RefreshMenuOptions(Campaign.Current.CurrentMenuContext); } catch { }
			}
			catch (Exception ex)
			{
				LoggingService.Exception("EnlistmentBehavior", ex, "OnStatusReportInit");
			}
		}

		private bool OnWaitMenuCondition(MenuCallbackArgs args)
		{
			// Show only while enlisted and commander is inside a settlement
			var ok = _state.IsEnlisted && _trackedCommander != null && _trackedCommander.CurrentSettlement != null;
			return ok;
		}

		private bool CanAskToEnlist()
		{
			// Check if player is not already enlisted
			if (_state.IsEnlisted)
			{
				return false;
			}

			// Check if talking to a noble with a party (looser than IsLord)
			Hero conversationHero = Hero.OneToOneConversationHero;
			if (conversationHero == null || conversationHero.PartyBelongedTo == null)
			{
				return false;
			}

			return true;
		}

		private bool ShouldAcceptEnlistment()
		{
			Hero conversationHero = Hero.OneToOneConversationHero;
			if (conversationHero == null)
			{
				return false;
			}

			// Check reputation - accept if positive or neutral
			int relation = Hero.MainHero.GetRelation(conversationHero);
			return relation >= 0;
		}

		private bool ShouldDeclineEnlistment()
		{
			Hero conversationHero = Hero.OneToOneConversationHero;
			if (conversationHero == null)
			{
				return false;
			}

			// Decline if negative reputation
			int relation = Hero.MainHero.GetRelation(conversationHero);
			return relation < 0;
		}

		private void OnEnlistmentRequested()
		{
			// Called when player asks to enlist; confirmation path proceeds in OnEnlistmentConfirmed
		}

		private void OnEnlistmentConfirmed()
		{
			try
			{
				LogState("OnEnlistmentConfirmed:entry");
				LoggingService.Info("EnlistmentBehavior", "OnEnlistmentConfirmed called");
				
				Hero conversationHero = Hero.OneToOneConversationHero;
				if (conversationHero?.PartyBelongedTo == null)
				{
					LoggingService.Error("EnlistmentBehavior", "OnEnlistmentConfirmed: Invalid conversation hero or no party");
					return;
				}
				
				var lord = conversationHero;
				var lordMobileParty = lord.PartyBelongedTo; // MobileParty in 1.2.12
				var lordPartyBase = lordMobileParty?.Party;
				if (lordMobileParty == null || lordPartyBase == null)
				{
					InformationManager.DisplayMessage(new InformationMessage("Enlist failed: no valid lord party."));
					return;
				}
				
				// Guards
				if (lord.IsPrisoner || lordMobileParty.IsDisbanding)
				{
					InformationManager.DisplayMessage(new InformationMessage("Enlist failed: lord unavailable."));
					return;
				}
				if (MobileParty.MainParty == null || MobileParty.MainParty.IsDisbanding)
				{
					InformationManager.DisplayMessage(new InformationMessage("Enlist failed: player party invalid."));
					return;
				}

				// A) Finish any active encounter and drain menus BEFORE applying escort/camera/state
				try 
				{ 
					if (PlayerEncounter.Current != null)
					{
						LoggingService.Debug("EnlistmentBehavior", "Finishing active PlayerEncounter prior to enlistment state changes");
						PlayerEncounter.Finish(true);
					}
				}
				catch { }
				try
				{
					int drainSafety = 5;
					while (Campaign.Current?.CurrentMenuContext != null && drainSafety-- > 0)
					{
						GameMenu.ExitToLast();
					}
				}
				catch { }
				try { if (Campaign.Current != null) Campaign.Current.TimeControlMode = CampaignTimeControlMode.StoppablePlay; } catch { }
				_pendingOpenStatusMenu = true;

				// 1) Snapshot player party
				var mainPartyBefore = MobileParty.MainParty;
				_snapshot = SnapshotPlayerParty(mainPartyBefore);
				
				// 2) Escort the commander (Freelancer-style attachment)
				try { MobileParty.MainParty?.Ai?.SetMoveEscortParty(lordMobileParty); } catch { }
				
				// 3) Persist enlistment state and apply equipment
				_state.Enlist(lord);
				_state.StorePlayerEquipment();
				_state.ApplySoldierEquipment();


				// 4) Camera follow and tracking
				_pendingFollowParty = lordPartyBase;
				_pendingCameraFollow = true;
				_trackedCommander = lordMobileParty;
				try { Campaign.Current.VisualTrackerManager.RegisterObject(lordMobileParty); } catch { }
				
				// 5) Hide our main party and despawn visuals
				try { MobileParty.MainParty.IsVisible = false; } catch { }
				try { TryUntrackAndDespawn(MobileParty.MainParty); } catch { }
				
				try { lordPartyBase.SetAsCameraFollowParty(); } catch { }
				
				InformationManager.DisplayMessage(new InformationMessage(
					EnlistmentDialogs.GetEnlistmentSuccessMessage(lord.Name.ToString()),
					Color.FromUint(0xFF00FF00)));
				LoggingService.Info("EnlistmentBehavior", "Enlistment completed successfully");
				
				IsPlayerEnlisted = true;
				CurrentCommanderParty = lordMobileParty;
				
				// Open status once on next tick; do not force-close menus immediately
				_pendingCloseEnlistedMenu = false;
				LogState("OnEnlistmentConfirmed:queued-close");
			}
			catch (Exception ex)
			{
				LoggingService.Exception("EnlistmentBehavior", ex, "Error in OnEnlistmentConfirmed");
				InformationManager.DisplayMessage(new InformationMessage(
					$"Enlistment failed: {ex.Message}", 
					Color.FromUint(0xFFFF0000)));
			}
		}

		private PartySnapshot SnapshotPlayerParty(MobileParty party)
		{
			return new PartySnapshot
			{
				Gold = party.PartyTradeGold,
				MapPos = party.Position2D
			};
		}

		// Unregister tracking and despawn any lingering visuals for a party
		private static void TryUntrackAndDespawn(MobileParty party)
		{
			if (party == null)
			{
				return;
			}
			
			// A) VisualTrackerManager: stop long-distance tracking/highlight
			try
			{
				var vtm = Campaign.Current?.VisualTrackerManager;
				if (vtm != null)
				{
					var mUnreg =
						AccessTools.Method(vtm.GetType(), "UnregisterObject") ??
						AccessTools.Method(vtm.GetType(), "RemoveTrackedObject") ??
						AccessTools.Method(vtm.GetType(), "RemoveObject");
					mUnreg?.Invoke(vtm, new object[] { party });
				}
			}
			catch { }
			
			// B) Party visuals on the map (SandBox side managers vary by version)
			try
			{
				var pvmType =
					  AccessTools.TypeByName("SandBox.View.Map.PartyVisualManager")
					?? AccessTools.TypeByName("SandBox.View.PartyVisualManager")
					?? AccessTools.TypeByName("SandBox.ViewModelCollection.Map.PartyVisualManager");
				
				if (pvmType != null)
				{
					var instProp = pvmType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
					var mgr = instProp?.GetValue(null);
					if (mgr != null)
					{
						var mRemove =
							  AccessTools.Method(pvmType, "RemoveVisualOfParty")
							?? AccessTools.Method(pvmType, "RemoveParty")
							?? AccessTools.Method(pvmType, "DespawnPartyVisual");
						mRemove?.Invoke(mgr, new object[] { party });
					}
				}
			}
			catch { }
			
			// C) Last-resort: sever the visual link from the party itself
			try
			{
				var visualsProp = party.GetType().GetProperty("Visuals", BindingFlags.Public | BindingFlags.Instance);
				var visuals = visualsProp?.GetValue(party);
				if (visuals != null)
				{
					var mSetMapEntity = AccessTools.Method(visuals.GetType(), "SetMapEntity");
					mSetMapEntity?.Invoke(visuals, new object[] { null });
				}
			}
			catch { }
		}

		private void OnSoldierStatusInit(MenuCallbackArgs args)
		{
			if (_state.Commander != null)
			{
				args.MenuTitle = new TextObject(EnlistmentDialogs.GetSoldierStatusTitle(_state.Commander.Name.ToString()));
			}
			LoggingService.Debug("EnlistmentBehavior", "OnSoldierStatusInit called");
			// Keep ribbon time controls available while status menu is open
			try { if (Campaign.Current != null) Campaign.Current.TimeControlMode = CampaignTimeControlMode.StoppablePlay; } catch { }
			// Match SAS/Freelancer: start wait mode so time controls are accepted with menu open
			try { args.MenuContext?.GameMenu?.StartWait(); } catch { }
			try { Campaign.Current?.GameMenuManager?.RefreshMenuOptions(Campaign.Current.CurrentMenuContext); } catch { }
			// If we're prompting for settlement entry, pause again for clarity
			if (_isPromptingSettlement)
			{
				try { Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop; } catch { }
			}
			// Immediately bounce out to campaign if this menu was opened post-enlistment
			if (_autoCloseStatusOnInit)
			{
				_autoCloseStatusOnInit = false;
				try { GameMenu.ExitToLast(); } catch { }
			}
		}

		private bool OnSoldierStatusCondition(MenuCallbackArgs args)
		{
			return _state.IsEnlisted;
		}

		private bool OnEnterSettlementCondition(MenuCallbackArgs args)
		{
			if (!_state.IsEnlisted || _trackedCommander == null)
			{
				return false;
			}
			var s = _pendingSettlementPrompt ?? _trackedCommander.CurrentSettlement;
			bool eligible = s != null && (s.IsTown || s.IsCastle);
			if (!eligible)
			{
				args.IsEnabled = false;
				args.Tooltip = new TextObject("Your lord is not inside a town or castle.");
			}
			// Note: we cannot change menu text via args; keep label static
			return eligible;
		}

		private void OnEnterSettlementConsequence(MenuCallbackArgs args)
		{
			// Schedule deferred entry to avoid running heavy actions inside menu callback
			try
			{
				var s = _pendingSettlementPrompt ?? _trackedCommander?.CurrentSettlement;
				_pendingEnterTarget = s;
				_pendingEnterSettlement = true;
				LoggingService.Info("EnlistmentBehavior", $"EnterSettlement: scheduled deferred entry for {s?.Name}");
			}
			catch (System.Exception ex)
			{
				LoggingService.Exception("EnlistmentBehavior", ex, "OnEnterSettlementConsequence(schedule)");
			}
		}

		private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
		{
			try
			{
				if (party == MobileParty.MainParty)
				{
					var kind = settlement == null ? "<null>" : (settlement.IsTown ? "Town" : settlement.IsCastle ? "Castle" : settlement.IsVillage ? "Village" : settlement.StringId);
					LoggingService.Info("EnlistmentBehavior", $"Player entered settlement: {settlement?.Name} ({kind})");
				}
			}
			catch { }
		}

		private void OnSoldierStatusConsequence(MenuCallbackArgs args)
		{
			// Menu is displayed, no special consequence needed
		}

		private bool CanAskForLeave(MenuCallbackArgs args)
		{
			return _state.IsEnlisted;
		}

		private void OnAskForLeave(MenuCallbackArgs args)
		{
			// Open conversation with commander to ask for leave
			if (_state.Commander != null)
			{
				CampaignMapConversation.OpenConversation(new ConversationCharacterData(
					CharacterObject.PlayerCharacter, 
					PartyBase.MainParty, 
					false, 
					false, 
					false, 
					false, 
					false, 
					false), 
				new ConversationCharacterData(
					_state.Commander.CharacterObject, 
					_state.Commander.PartyBelongedTo.Party, 
					false, 
					false, 
					false, 
					false, 
					false, 
					false));
			}
		}

		private void OnReturnToCampaign(MenuCallbackArgs args)
		{
			GameMenu.ExitToLast();
		}

		// Re-apply escort, visibility and tracking after load if the player is enlisted
		private void ReapplyEnlistmentState()
		{
			if (_state == null || !_state.IsEnlisted)
			{
				return;
			}
			var commander = _state.Commander;
			var commanderParty = commander?.PartyBelongedTo;
			if (commander == null || commanderParty == null)
			{
				LoggingService.Warning("EnlistmentBehavior", "ReapplyEnlistmentState: commander or commander party missing");
				return;
			}
			try { MobileParty.MainParty?.Ai?.SetMoveEscortParty(commanderParty); } catch { }
			try { MobileParty.MainParty.IsVisible = false; } catch { }
			try { TryUntrackAndDespawn(MobileParty.MainParty); } catch { }
			try { commanderParty.Party.SetAsCameraFollowParty(); } catch { }
			try { Campaign.Current?.VisualTrackerManager?.RegisterObject(commanderParty); } catch { }
			_trackedCommander = commanderParty;
			IsPlayerEnlisted = true;
			CurrentCommanderParty = commanderParty;
			_commanderInSettlementPrev = commanderParty.CurrentSettlement != null;
			// If we loaded while commander is already in a town/castle, surface the prompt once
			var cs = commanderParty.CurrentSettlement;
			if (PromptOnSettlementEntry && cs != null && (cs.IsTown || cs.IsCastle))
			{
				if (!_isPromptingSettlement)
				{
					_isPromptingSettlement = true;
					_pendingSettlementPrompt = cs;
					try { Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop; } catch { }
					try { GameMenu.ActivateGameMenu("enlisted_soldier_status"); } catch { }
				}
			}
			LoggingService.Info("EnlistmentBehavior", $"Reapplied enlistment state. Commander={commander.Name}");
		}

		// Leave dialog methods
		private bool CanAskForLeaveDialog()
		{
			// Check if player is enlisted and talking to their commander
			if (!_state.IsEnlisted)
			{
				return false;
			}

			Hero conversationHero = Hero.OneToOneConversationHero;
			if (conversationHero == null)
			{
				return false;
			}

			return conversationHero == _state.Commander;
		}

		private bool ShouldAcceptLeave()
		{
			// Always accept leave requests for now
			// You could add conditions like minimum service time, good behavior, etc.
			return true;
		}

		private bool ShouldDeclineLeave()
		{
			// Decline if conditions aren't met for accepting
			return !ShouldAcceptLeave();
		}

		private void OnLeaveRequested()
		{
			// This is called when player asks for leave
			// The actual leave happens in OnLeaveConfirmed
		}

		private void OnLeaveAccepted()
		{
			// Lord accepts the leave request
		}

		private void OnLeaveDeclined()
		{
			// Lord declines the leave request
			InformationManager.DisplayMessage(new InformationMessage(
				EnlistmentDialogs.Messages.LeaveDenied, 
				Color.FromUint(0xFFFF0000)));
		}

		private void OnLeaveConfirmed()
		{
			// Discharge and restore player's party
			try
			{
				var lord = _state.Commander;
				var lordParty = lord?.PartyBelongedTo;
				
				// Remove hero from lord party if present
				if (lordParty != null && lordParty.MemberRoster.Contains(Hero.MainHero.CharacterObject))
				{
					lordParty.MemberRoster.RemoveTroop(Hero.MainHero.CharacterObject, 1);
				}
				
				_state.LeaveArmy();
				// Restore UI tracking back to player and untrack commander exactly once
				try
				{
					var vtm = Campaign.Current?.VisualTrackerManager;
					vtm?.RegisterObject(MobileParty.MainParty);
					try
					{
						var unreg = AccessTools.Method(vtm?.GetType(), "UnregisterObject")
								?? AccessTools.Method(vtm?.GetType(), "RemoveTrackedObject");
						if (unreg != null && _trackedCommander != null)
						{
							unreg.Invoke(vtm, new object[] { _trackedCommander });
						}
					}
					catch { }
				}
				catch { }
				_trackedCommander = null;
				IsPlayerEnlisted = false;
				CurrentCommanderParty = null;
				// Show player party again and clear escort orders
				try { MobileParty.MainParty.IsVisible = true; } catch { }
				try { MobileParty.MainParty?.Ai?.SetMoveModeHold(); } catch { }
				try { MobileParty.MainParty?.Party?.SetAsCameraFollowParty(); } catch { }
				InformationManager.DisplayMessage(new InformationMessage("You have left their service."));
			}
			catch (Exception ex)
			{
				LoggingService.Exception("EnlistmentBehavior", ex, "Error in OnLeaveConfirmed");
			}
		}

		// Public methods for other systems to interact with enlistment state
		public bool IsEnlisted => _state.IsEnlisted;
		public Hero Commander => _state.Commander;
		public void LeaveArmy() => _state.LeaveArmy();
	}
}
