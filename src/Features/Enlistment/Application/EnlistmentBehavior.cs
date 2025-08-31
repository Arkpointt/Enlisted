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
using TaleWorlds.CampaignSystem.MapEvents;
using HarmonyLib;
using System.Reflection;
using System.Linq;
using Enlisted.Features.Enlistment.Domain;
using Enlisted.Mod.Core.Logging;
using TaleWorlds.CampaignSystem.Settlements;
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
		private float _aiIgnoreRefreshTimer;
		private bool _waitMenuActive;
		private bool _commanderInSettlementPrev;
		private float _visualHideTimer;
		private bool _pendingCloseEnlistedMenu;
		private bool _pendingOpenStatusMenu;
		private bool _enlistedMenuWasOpenBeforeJoin;
		// Deprecated countdown/flag kept previously for auto-close behavior; removed to avoid warnings
		private bool _autoCloseStatusOnInit;
		private static readonly bool UseSettlementWaitMenu = false; // Freelancer parity: do not auto-open wait menu on settlement entry
		private bool _activatedDueToCommanderBattle;
		private bool _commanderWasInArmy;
		private const bool UseIgnoreSafety = true;
		private const bool AggressiveVisualDespawn = false; // disable frequent visuals.SetMapEntity(null) calls
		private bool _deferPostLoadSetup;
		private bool _pendingPostBattleRestore;
		private float _postLoadSafetyTimer;
		private bool _loggedPendingCameraWait = true;
		private bool _loggedPostLoadWait = true;
		private bool _loggedPostBattleWait = true;

		private bool ShouldUseIgnore()
		{
			// Only use ignore safety when the commander is not in an army; when in an army we rely on army attachment blob
			return UseIgnoreSafety && (_trackedCommander?.Army == null);
		}

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
			// Engagement routing via map event hooks (Freelancer/SAS parity)
			try { CampaignEvents.MapEventStarted.AddNonSerializedListener(this, OnMapEventStarted); } catch { }
			try { CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded); } catch { }
			
			
			
		}

		public override void SyncData(IDataStore dataStore)
		{
			// Primary: let the save system handle our state object if available
			dataStore.SyncData("_enlistmentState", ref _state);
			// Fallback: persist minimal fields to maintain enlisted status across loads
			string commanderId = null; bool isEnlisted = false; CampaignTime enlistTime = default; int enlistTier = 0;
			List<EquipmentElement> storedEq = null; List<ItemObject> storedItems = null; List<int> storedSlots = null; Equipment storedEquipmentSnapshot = null; Equipment storedCivilianEquipmentSnapshot = null; List<EquipmentElement> storedRosterElements = null; List<int> storedRosterCounts = null;
			if (dataStore.IsSaving)
			{
				try
				{
					_state?.ExtractForSave(out commanderId, out isEnlisted, out enlistTime, out enlistTier);
					if (_state != null)
					{
						var loadout = _state.GetStoredLoadout();
						storedEq = loadout.equipment; storedItems = loadout.items; storedSlots = loadout.slots;
						storedEquipmentSnapshot = _state.GetStoredEquipmentSnapshot();
						storedCivilianEquipmentSnapshot = _state.GetStoredCivilianEquipmentSnapshot();
						var roster = _state.GetStoredRosterSnapshot();
						storedRosterElements = roster.elements; storedRosterCounts = roster.counts;
					}
					dataStore.SyncData("_enlistment_commanderId", ref commanderId);
					dataStore.SyncData("_enlisted_flag", ref isEnlisted);
					dataStore.SyncData("_enlist_time", ref enlistTime);
					dataStore.SyncData("_enlist_tier", ref enlistTier);
					dataStore.SyncData("_enlist_stored_eq", ref storedEq);
					dataStore.SyncData("_enlist_stored_items", ref storedItems);
					dataStore.SyncData("_enlist_stored_slots", ref storedSlots);
					dataStore.SyncData("_enlist_stored_equipment_snapshot", ref storedEquipmentSnapshot);
					dataStore.SyncData("_enlist_stored_civilian_equipment_snapshot", ref storedCivilianEquipmentSnapshot);
					dataStore.SyncData("_enlist_stored_roster_elements", ref storedRosterElements);
					dataStore.SyncData("_enlist_stored_roster_counts", ref storedRosterCounts);
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
					dataStore.SyncData("_enlist_stored_slots", ref storedSlots);
					dataStore.SyncData("_enlist_stored_equipment_snapshot", ref storedEquipmentSnapshot);
					dataStore.SyncData("_enlist_stored_civilian_equipment_snapshot", ref storedCivilianEquipmentSnapshot);
					dataStore.SyncData("_enlist_stored_roster_elements", ref storedRosterElements);
					dataStore.SyncData("_enlist_stored_roster_counts", ref storedRosterCounts);
					if (_state == null) _state = new EnlistmentState();
					_state.RestoreFromSave(commanderId, isEnlisted, enlistTime, enlistTier);
					if (storedEq != null || storedItems != null || storedSlots != null) _state.RestoreStoredLoadout(storedEq, storedItems, storedSlots);
					if (storedEquipmentSnapshot != null) _state.SetStoredEquipmentSnapshot(storedEquipmentSnapshot);
					if (storedCivilianEquipmentSnapshot != null) _state.SetStoredCivilianEquipmentSnapshot(storedCivilianEquipmentSnapshot);
					if (storedRosterElements != null || storedRosterCounts != null) _state.SetStoredRosterSnapshot(storedRosterElements, storedRosterCounts);
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
				bool hasMenuNow = Campaign.Current?.CurrentMenuContext != null;
				bool inEncounterNow = PlayerEncounter.Current != null;
				if (!hasMenuNow && !inEncounterNow && _postLoadSafetyTimer <= 0f)
				{
					try { _pendingFollowParty.SetAsCameraFollowParty(); } catch { }
					LoggingService.Debug("EnlistmentBehavior", "PendingCameraFollow applied");
					_pendingCameraFollow = false;
					_pendingFollowParty = null;
					_loggedPendingCameraWait = true;
				}
				else if (_loggedPendingCameraWait)
				{
					LoggingService.Debug("EnlistmentBehavior", $"PendingCameraFollow deferred: hasMenu={hasMenuNow} inEncounter={inEncounterNow} timer={_postLoadSafetyTimer:0.00}");
					_loggedPendingCameraWait = false;
				}
			}

			// Perform deferred post-load setup once it's safe (no menu/encounter)
			if (_deferPostLoadSetup && _state.IsEnlisted && _trackedCommander != null)
			{
				bool hasMenuNow = Campaign.Current?.CurrentMenuContext != null;
				bool inEncounterNow = PlayerEncounter.Current != null;
				if (!hasMenuNow && !inEncounterNow && _postLoadSafetyTimer <= 0f)
				{
					try { MobileParty.MainParty?.Ai?.SetMoveEscortParty(_trackedCommander?.Army?.LeaderParty ?? _trackedCommander); } catch { }
					try { Campaign.Current?.VisualTrackerManager?.RegisterObject(_trackedCommander); } catch { }
					try { ((_trackedCommander?.Army?.LeaderParty ?? _trackedCommander)?.Party)?.SetAsCameraFollowParty(); } catch { }
					_deferPostLoadSetup = false;
					LoggingService.Debug("EnlistmentBehavior", "PostLoadSetup applied");
					_loggedPostLoadWait = true;
				}
				else if (_loggedPostLoadWait)
				{
					LoggingService.Debug("EnlistmentBehavior", $"PostLoadSetup deferred: hasMenu={hasMenuNow} inEncounter={inEncounterNow} timer={_postLoadSafetyTimer:0.00}");
					_loggedPostLoadWait = false;
				}
			}

			// Perform deferred post-battle restore once out of encounter/menus
			if (_pendingPostBattleRestore && _state.IsEnlisted && _trackedCommander != null)
			{
				bool hasMenuNow2 = Campaign.Current?.CurrentMenuContext != null;
				bool inEncounterNow2 = PlayerEncounter.Current != null;
				if (!hasMenuNow2 && !inEncounterNow2 && _postLoadSafetyTimer <= 0f)
				{
					try { MobileParty.MainParty.IsActive = false; } catch { }
					try { MobileParty.MainParty.IsVisible = false; } catch { }
					if (ShouldUseIgnore()) { try { MobileParty.MainParty.IgnoreByOtherPartiesTill(CampaignTime.Now + CampaignTime.Hours(0.5f)); } catch { } }
					try { MobileParty.MainParty?.Ai?.SetMoveEscortParty(_trackedCommander?.Army?.LeaderParty ?? _trackedCommander); } catch { }
					try { _trackedCommander.Party.SetAsCameraFollowParty(); } catch { }
					if (_enlistedMenuWasOpenBeforeJoin)
					{
						_enlistedMenuWasOpenBeforeJoin = false;
						try { GameMenu.ActivateGameMenu("enlisted_soldier_status"); } catch { }
						try { Campaign.Current.TimeControlMode = CampaignTimeControlMode.StoppablePlay; } catch { }
					}
					_pendingPostBattleRestore = false;
					LoggingService.Debug("EnlistmentBehavior", "PostBattleRestore applied");
					_loggedPostBattleWait = true;
				}
				else if (_loggedPostBattleWait)
				{
					LoggingService.Debug("EnlistmentBehavior", $"PostBattleRestore deferred: hasMenu={hasMenuNow2} inEncounter={inEncounterNow2} timer={_postLoadSafetyTimer:0.00}");
					_loggedPostBattleWait = false;
				}
			}

			// Ensure commander behaviors while enlisted
			if (_state.IsEnlisted && _trackedCommander != null)
			{
				// Attach/detach to commander's army to be treated as part of blob (SAS parity)
				try
				{
					var commanderArmy = _trackedCommander.Army;
					if (commanderArmy != null)
					{
						if (MobileParty.MainParty.Army != commanderArmy)
						{
							try { commanderArmy.AddPartyToMergedParties(MobileParty.MainParty); } catch { }
							try { MobileParty.MainParty.Army = commanderArmy; } catch { }
						}
					}
					else
					{
						if (MobileParty.MainParty.Army != null)
						{
							try { MobileParty.MainParty.Army = null; } catch { }
						}
					}
				}
				catch { }

				// Commander defeat/capture safety: if commander is no longer valid, detach and restore player state
				try
				{
					var leader = _trackedCommander.LeaderHero;
					if (leader == null || !leader.IsAlive || leader.IsPrisoner || _trackedCommander.IsDisbanding)
					{
						SafeDetachFromCommander("Your commander was defeated or captured.");
						return;
					}
				}
				catch { }

				// Enforce hidden main party visuals each tick, since engine/UI may respawn them
				try { MobileParty.MainParty.IsVisible = false; } catch { }
				_visualHideTimer += dt;
				if (_visualHideTimer >= 2.0f)
				{
					_visualHideTimer = 0f;
					if (AggressiveVisualDespawn)
					{
						try { TryUntrackAndDespawn(MobileParty.MainParty); } catch { }
					}
				}

				// Aggressively refresh escort and camera to prevent manual control and keep follow locked
				_escortRefreshTimer += dt;
				// Do not reassert escort while the commander is inside a settlement; we want to wait outside
				if (_trackedCommander?.CurrentSettlement == null && _escortRefreshTimer >= 0.1f)
				{
					_escortRefreshTimer = 0f;
					try
					{
						if (MobileParty.MainParty?.Ai != null)
						{
							// Escort-based speed match (engine AI handles following)
							try { MobileParty.MainParty.IsActive = true; } catch { }
							var escortTarget = _trackedCommander?.Army?.LeaderParty ?? _trackedCommander;
							MobileParty.MainParty.Ai.SetMoveEscortParty(escortTarget);
							if (ShouldUseIgnore()) { try { MobileParty.MainParty.IgnoreByOtherPartiesTill(CampaignTime.Now + CampaignTime.Hours(0.5f)); } catch { } }
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

				// Gentle tether every tick: smoothly correct toward a point just behind commander/leader
				try
				{
					var hasMenuTether = Campaign.Current?.CurrentMenuContext != null;
					var inEncounterTether = PlayerEncounter.Current != null;
					var commanderInSettlementTether = _trackedCommander.CurrentSettlement != null;
					var playerInSettlementTether = MobileParty.MainParty?.CurrentSettlement != null;
					if (!hasMenuTether && !inEncounterTether && !commanderInSettlementTether && !playerInSettlementTether)
					{
						var playerPos = MobileParty.MainParty.Position2D;
						var targetParty = (_trackedCommander?.Army?.LeaderParty ?? _trackedCommander);
						var targetPos = targetParty.Position2D;
						var delta = targetPos - playerPos;
						if (delta.LengthSquared > 0.36f) // > 0.6 units
						{
							var dir = delta; if (dir.LengthSquared < 1e-4f) dir = new Vec2(-0.6f, -0.6f); else dir.Normalize();
							var desired = targetPos - dir * 0.5f;
							float alpha = dt * 8f; if (alpha > 1f) alpha = 1f;
							MobileParty.MainParty.Position2D = playerPos + (desired - playerPos) * alpha;
						}
					}
				}
				catch { }

				// Keep camera following the commander (or army leader) steadily
				_cameraFollowTimer += dt;
				if (_cameraFollowTimer >= 0.5f)
				{
					_cameraFollowTimer = 0f;
					try { ((_trackedCommander?.Army?.LeaderParty ?? _trackedCommander)?.Party)?.SetAsCameraFollowParty(); } catch { }
				}

				// Diagnostics: log when commander joins/leaves an army
				try
				{
					bool inArmy = _trackedCommander.Army != null;
					if (inArmy != _commanderWasInArmy)
					{
						_commanderWasInArmy = inArmy;
						var leaderName = _trackedCommander.Army?.LeaderParty?.LeaderHero?.Name?.ToString() ?? "<none>";
						LoggingService.Info("EnlistmentBehavior", $"Commander army state changed. InArmy={inArmy} Leader={leaderName}");
					}
				}
				catch { }

				// Auto-join commander battles (SAS/Freelancer): keep inactive, briefly activate only to let inclusion happen
				try
				{
					bool commanderInBattle = _trackedCommander.MapEvent != null;
					bool playerInBattle = MobileParty.MainParty?.MapEvent != null || PlayerEncounter.Current != null;
					if (commanderInBattle && !playerInBattle)
					{
						try { MobileParty.MainParty.IsActive = true; } catch { }
						// Nudge near commander to ensure inclusion as reinforcement
						try
						{
							var commanderPos = _trackedCommander.Position2D;
							var offset = new Vec2(0.3f, -0.3f);
							MobileParty.MainParty.Position2D = commanderPos + offset;
						}
						catch { }
						_activatedDueToCommanderBattle = true;
					}
					else if (!commanderInBattle && _activatedDueToCommanderBattle)
					{
						// Commander left battle; restore inactive hidden state
						_activatedDueToCommanderBattle = false;
						try { if (_state.IsEnlisted) MobileParty.MainParty.IsActive = false; } catch { }
						if (ShouldUseIgnore()) { try { MobileParty.MainParty.IgnoreByOtherPartiesTill(CampaignTime.Now + CampaignTime.Hours(0.5f)); } catch { } }
					}
				}
				catch { }

				// Settlement handling
				try
				{
					var commanderSettlement = _trackedCommander.CurrentSettlement;
					bool commanderInSettlement = commanderSettlement != null;
					bool playerInEncounter = PlayerEncounter.Current != null;
					bool playerInSettlementNow = MobileParty.MainParty?.CurrentSettlement != null;

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
							try { MobileParty.MainParty?.Ai?.SetMoveEscortParty(_trackedCommander?.Army?.LeaderParty ?? _trackedCommander); } catch { }
						}
					}
					else
					{
						// Freelancer parity: do not auto-open menus on settlement entry
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
									try { TaleWorlds.CampaignSystem.Actions.LeaveSettlementAction.ApplyForParty(MobileParty.MainParty); } catch { }
									try { MobileParty.MainParty?.Ai?.SetMoveEscortParty(_trackedCommander?.Army?.LeaderParty ?? _trackedCommander); } catch { }
									try { Campaign.Current.TimeControlMode = CampaignTimeControlMode.StoppablePlay; } catch { }
								}
							}
							catch { }
						}
						// If commander just entered a town/castle, keep player outside: stop escort, hold position,
						// and force-leave if the player somehow got inside.
						if (!_commanderInSettlementPrev && commanderInSettlement)
						{
							try { MobileParty.MainParty?.Ai?.SetMoveModeHold(); } catch { }
							if (playerInSettlementNow)
							{
								int drain3 = 5;
								while (Campaign.Current?.CurrentMenuContext != null && drain3-- > 0)
								{
									try { GameMenu.ExitToLast(); } catch { break; }
								}
								try { TaleWorlds.CampaignSystem.Actions.LeaveSettlementAction.ApplyForParty(MobileParty.MainParty); } catch { }
								try { Campaign.Current.TimeControlMode = CampaignTimeControlMode.StoppablePlay; } catch { }
							}
						}
						_commanderInSettlementPrev = commanderInSettlement;
					}
				}
				catch (Exception ex)
				{
					LoggingService.Exception("EnlistmentBehavior", ex, "Settlement handling");
				}

				// Periodically refresh AI ignore so world parties do not target the hidden main party
				try
				{
					_aiIgnoreRefreshTimer += dt;
					if (_aiIgnoreRefreshTimer >= 10.0f)
					{
						_aiIgnoreRefreshTimer = 0f;
						if (ShouldUseIgnore()) { try { MobileParty.MainParty.IgnoreByOtherPartiesTill(CampaignTime.Now + CampaignTime.Hours(0.5f)); } catch { } }
					}
				}
				catch { }

				// Removed: deferred enter-settlement execution

				// Recovery clamp: if somehow far away, snap back just behind commander/army leader
				try
				{
					var hasMenu2 = Campaign.Current?.CurrentMenuContext != null;
					var inEncounter2 = PlayerEncounter.Current != null;
					var commanderInSettlement2 = _trackedCommander.CurrentSettlement != null;
					var playerInSettlement = MobileParty.MainParty?.CurrentSettlement != null;
					if (!hasMenu2 && !inEncounter2 && !commanderInSettlement2 && !playerInSettlement)
					{
						var playerPos = MobileParty.MainParty.Position2D;
						var targetParty = (_trackedCommander?.Army?.LeaderParty ?? _trackedCommander);
						var targetPos = targetParty.Position2D;
						var delta = targetPos - playerPos;
						if (delta.LengthSquared > 1.44f) // > 1.2 units away: smooth correction
						{
							var dir = delta; if (dir.LengthSquared < 1e-4f) dir = new Vec2(-0.6f, -0.6f); else dir.Normalize();
							var desired = targetPos - dir * 0.5f;
							float alpha = dt * 5f; if (alpha > 1f) alpha = 1f; // smooth toward desired
							MobileParty.MainParty.Position2D = playerPos + (desired - playerPos) * alpha;
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
						// Do NOT finish encounters while inside a settlement/siege flow; defer instead
						bool insideSettlement = MobileParty.MainParty?.CurrentSettlement != null;
						if (PlayerEncounter.Current != null && !insideSettlement)
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

			// Count down safety timer
			if (_postLoadSafetyTimer > 0f)
			{
				_postLoadSafetyTimer -= dt;
				if (_postLoadSafetyTimer < 0f) _postLoadSafetyTimer = 0f;
			}
		}

		private void SafeDetachFromCommander(string reason)
		{
			try
			{
				_state.LeaveArmy();
				IsPlayerEnlisted = false;
				CurrentCommanderParty = null;
				_trackedCommander = null;
				// Restore player party state
				try { MobileParty.MainParty.IsVisible = true; } catch { }
				try { MobileParty.MainParty.IsActive = true; } catch { }
				try { MobileParty.MainParty?.Ai?.SetMoveModeHold(); } catch { }
				// Clear ignore horizon so normal targeting resumes soon (does nothing if safety disabled)
				if (ShouldUseIgnore()) { try { MobileParty.MainParty.IgnoreByOtherPartiesTill(CampaignTime.Now + CampaignTime.Hours(0.5f)); } catch { } }
				try { PartyBase.MainParty?.SetAsCameraFollowParty(); } catch { }
				if (!string.IsNullOrEmpty(reason))
				{
					InformationManager.DisplayMessage(new InformationMessage(reason, Color.FromUint(0xFFFFA500)));
				}
				try { if (MobileParty.MainParty.Army != null) MobileParty.MainParty.Army = null; } catch { }
			}
			catch (Exception ex)
			{
				LoggingService.Exception("EnlistmentBehavior", ex, "SafeDetachFromCommander");
			}
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
			// Menus moved to EnlistedMenuBehavior
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

		// Map event hooks inspired by Freelancer/SAS patterns to ensure inclusion and cleanup
		private void OnMapEventStarted(MapEvent mapEvent, PartyBase firstParty, PartyBase secondParty)
		{
			try
			{
				if (!_state.IsEnlisted || _trackedCommander == null || mapEvent == null)
				{
					return;
				}
				// Do not attempt to join or manipulate encounters while either the commander or the player is in a settlement
				if (_trackedCommander.CurrentSettlement != null || MobileParty.MainParty?.CurrentSettlement != null)
				{
					return;
				}
				// Debug: dump attacker/defender parties and commander
				try
				{
					var attackerNames = string.Join(", ", (mapEvent.AttackerSide?.Parties ?? new System.Collections.Generic.List<MapEventParty>()).Select(mp => mp?.Party?.Name?.ToString()).Where(s => !string.IsNullOrEmpty(s)));
					var defenderNames = string.Join(", ", (mapEvent.DefenderSide?.Parties ?? new System.Collections.Generic.List<MapEventParty>()).Select(mp => mp?.Party?.Name?.ToString()).Where(s => !string.IsNullOrEmpty(s)));
					LoggingService.Debug("EnlistmentBehavior", $"MapEventStarted sides -> Attacker:[{attackerNames}] Defender:[{defenderNames}] Commander={_trackedCommander?.LeaderHero?.Name}");
				}
				catch { }
				// If commander (or their army parties) are on either side and player not yet in an event, join
				bool commanderInvolved = false;
				PartyBase enemyPartyBase = null;
				try
				{
					var commanderPartyBase = _trackedCommander?.Party;
					// Build a set of commander-related parties (commander + army members)
					var commanderRelated = new System.Collections.Generic.HashSet<PartyBase>();
					if (commanderPartyBase != null) commanderRelated.Add(commanderPartyBase);
					try
					{
						var army = _trackedCommander?.Army;
						if (army?.Parties != null)
						{
							foreach (var ap in army.Parties)
							{
								var pb = ap?.Party;
								if (pb != null) commanderRelated.Add(pb);
							}
						}
					}
					catch { }
					if (commanderPartyBase != null)
					{
						// Attacker side
						bool onAttacker = mapEvent.AttackerSide?.Parties?.Any(mp => mp != null && mp.Party != null && commanderRelated.Contains(mp.Party)) == true;
						bool onDefender = mapEvent.DefenderSide?.Parties?.Any(mp => mp != null && mp.Party != null && commanderRelated.Contains(mp.Party)) == true;
						if (onAttacker)
						{
							commanderInvolved = true;
							// choose an enemy party from defender side
							enemyPartyBase = mapEvent.DefenderSide?.Parties?.FirstOrDefault()?.Party ?? secondParty ?? firstParty;
						}
						else if (onDefender)
						{
							commanderInvolved = true;
							// choose an enemy party from attacker side
							enemyPartyBase = mapEvent.AttackerSide?.Parties?.FirstOrDefault()?.Party ?? firstParty ?? secondParty;
						}
					}
				}
				catch { }
				bool playerAlreadyInvolved = MobileParty.MainParty?.MapEvent != null || PlayerEncounter.Current != null;
				LoggingService.Debug("EnlistmentBehavior", $"OnMapEventStarted: commanderInvolved={commanderInvolved} playerInvolved={playerAlreadyInvolved} enemy={enemyPartyBase?.Name}");
				if ((commanderInvolved || !commanderInvolved) && !playerAlreadyInvolved)
				{
					// If our enlisted menu is open, remember and close it so the encounter window can appear
					try
					{
						var activeId = GetActiveMenuId();
						if (activeId == "enlisted_soldier_status" || activeId == "enlisted_status_report")
						{
							_enlistedMenuWasOpenBeforeJoin = true;
							int drain = 4;
							while (Campaign.Current?.CurrentMenuContext != null && drain-- > 0)
							{
								try { GameMenu.ExitToLast(); } catch { break; }
							}
							try { if (Campaign.Current != null) Campaign.Current.TimeControlMode = CampaignTimeControlMode.StoppablePlay; } catch { }
						}
						else
						{
							_enlistedMenuWasOpenBeforeJoin = false;
						}
					}
					catch { }
					// Ensure engine can include us: briefly set active
					try { MobileParty.MainParty.IsActive = true; } catch { }
					try
					{
						bool nudged = false;
						// Only nudge toward the commander; no nudging toward other friendly parties to avoid drift
						if (commanderInvolved)
						{
							var cpos = _trackedCommander.Position2D;
							MobileParty.MainParty.Position2D = cpos + new Vec2(0.3f, -0.3f);
							LoggingService.Debug("EnlistmentBehavior", "Commander involved; nudged beside commander for inclusion");
							nudged = true;
							if (ShouldUseIgnore()) { try { MobileParty.MainParty.IgnoreByOtherPartiesTill(CampaignTime.Now + CampaignTime.Hours(0.5f)); } catch { } }
						}
						// No friendly-party nudges; if commander not involved we don't move
					}
					catch { }
				}
			}
			catch (System.Exception ex)
			{
				LoggingService.Exception("EnlistmentBehavior", ex, "OnMapEventStarted");
			}
		}

		private void OnMapEventEnded(MapEvent mapEvent)
		{
			try
			{
				if (!_state.IsEnlisted || mapEvent == null)
				{
					return;
				}

				// Ignore unrelated events where neither commander nor player was involved
				bool commanderInvolved = false;
				bool playerInvolved = false;
				try
				{
					var cmdParty = _trackedCommander?.Party;
					if (cmdParty != null)
					{
						commanderInvolved = (mapEvent.AttackerSide?.Parties?.Any(mp => mp?.Party == cmdParty) == true)
							|| (mapEvent.DefenderSide?.Parties?.Any(mp => mp?.Party == cmdParty) == true)
							|| (mapEvent.InvolvedParties?.Any(p => p == cmdParty) == true);
					}
					var playerParty = PartyBase.MainParty;
					playerInvolved = playerParty != null && (mapEvent.InvolvedParties?.Any(p => p == playerParty) == true);
				}
				catch { }

				if (!commanderInvolved && !playerInvolved)
				{
					return;
				}
				// If commander is gone from the event or invalid, detach; otherwise re-hide and resume escort
				bool commanderStillInvolved = false;
				try
				{
					if (mapEvent != null && mapEvent.InvolvedParties != null && _trackedCommander != null)
					{
						commanderStillInvolved = mapEvent.InvolvedParties.Any(p => p == _trackedCommander.Party);
					}
				}
				catch { }
				var commanderLeader = _trackedCommander?.LeaderHero;
				if (!commanderStillInvolved || commanderLeader == null || !commanderLeader.IsAlive || commanderLeader.IsPrisoner)
				{
					SafeDetachFromCommander("You were separated after the battle.");
				}
				else
				{
					// Defer restore to a safe tick to avoid engine asserts during exit/transfer screens
					_pendingPostBattleRestore = true;
					_loggedPostBattleWait = true;
					LoggingService.Debug("EnlistmentBehavior", "PostBattleRestore scheduled (deferred)");
				}
			}
			catch (System.Exception ex)
			{
				LoggingService.Exception("EnlistmentBehavior", ex, "OnMapEventEnded");
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
				// SAS/Freelancer: keep main party inactive by default; rely on tethering
				try { MobileParty.MainParty.IsActive = false; } catch { }
				if (ShouldUseIgnore()) { try { MobileParty.MainParty.IgnoreByOtherPartiesTill(CampaignTime.Now + CampaignTime.Hours(0.5f)); } catch { } }
				if (AggressiveVisualDespawn)
				{
					try { TryUntrackAndDespawn(MobileParty.MainParty); } catch { }
				}
                
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
				// Initialize settlement remembered state
				_commanderInSettlementPrev = lordMobileParty.CurrentSettlement != null;
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
			// Removed: settlement entry prompt pause
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

		// Removed: enter-settlement menu condition and consequence

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
			try { MobileParty.MainParty.IsVisible = false; } catch { }
			// SAS/Freelancer baseline on load
			try { MobileParty.MainParty.IsActive = false; } catch { }
			if (ShouldUseIgnore()) { try { MobileParty.MainParty.IgnoreByOtherPartiesTill(CampaignTime.Now + CampaignTime.Hours(0.5f)); } catch { } }
			// Defer escort/tracker/camera to first safe tick to avoid visual asserts during load
			_deferPostLoadSetup = true;
			// Queue camera follow once menus settle
			_pendingFollowParty = commanderParty.Party;
			_pendingCameraFollow = true;
			_postLoadSafetyTimer = 1.0f;
			LoggingService.Debug("EnlistmentBehavior", "PostLoadSetup deferred: timer=1.00 started");
			_trackedCommander = commanderParty;
			IsPlayerEnlisted = true;
			CurrentCommanderParty = commanderParty;
			_commanderInSettlementPrev = commanderParty.CurrentSettlement != null;
			// Removed: auto-prompt when loading into settlement
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
				try { MobileParty.MainParty.IsActive = true; } catch { }
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
