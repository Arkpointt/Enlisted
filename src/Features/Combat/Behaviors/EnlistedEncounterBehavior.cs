using System;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;
using Enlisted.Mod.Entry;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace Enlisted.Features.Combat.Behaviors
{
    /// <summary>
    ///     Handles encounter menu extensions and battle participation for enlisted soldiers.
    ///     Adds "Wait in reserve" option for large field battles (100+ troops).
    ///     Native system handles siege menus automatically.
    /// </summary>
    public sealed class EnlistedEncounterBehavior : CampaignBehaviorBase
    {
        public EnlistedEncounterBehavior()
        {
            Instance = this;
        }

        public static EnlistedEncounterBehavior Instance { get; private set; }

        /// <summary>
        ///     Tracks whether the player is currently waiting in reserve during a battle.
        ///     Used to prevent XP awards when entering reserve mode (OnPlayerBattleEnd fires but battle isn't actually over).
        /// </summary>
        public static bool IsWaitingInReserve { get; private set; }


        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // No persistent state needed
        }

        private void OnSessionLaunched(CampaignGameStarter campaignStarter)
        {
            try
            {
                // Reset static state from previous session to prevent stale flag issues
                // If player quit while in reserve mode, flag would persist across save loads
                IsWaitingInReserve = false;

                AddEnlistedEncounterOptions(campaignStarter);
                ModLogger.LogOnce("encounter_behavior_init", "Combat",
                    "Encounter behavior initialized with modern UI styling - battle wait menu and reserve options ready");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Combat", $"Failed to initialize encounter behavior: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Menu background initialization for enlisted_battle_wait menu.
        /// Sets culture-appropriate background and ambient audio for battle wait.
        /// </summary>
        [GameMenuInitializationHandler("enlisted_battle_wait")]
        private static void OnBattleWaitBackgroundInit(MenuCallbackArgs args)
        {
            var lord = EnlistmentBehavior.Instance?.CurrentLord;
            var backgroundMesh = "encounter_looter";
            
            if (lord?.Clan?.Kingdom?.Culture?.EncounterBackgroundMesh != null)
            {
                backgroundMesh = lord.Clan.Kingdom.Culture.EncounterBackgroundMesh;
            }
            else if (lord?.Culture?.EncounterBackgroundMesh != null)
            {
                backgroundMesh = lord.Culture.EncounterBackgroundMesh;
            }
            
            args.MenuContext.SetBackgroundMeshName(backgroundMesh);
            args.MenuContext.SetAmbientSound("event:/map/ambient/node/settlements/2d/camp_army");
        }

        /// <summary>
        ///     Registers military-specific menu options for battles.
        ///     Adds "Wait in reserve" option for large field battles (100+ troops).
        ///     This option appears in the encounter menu when the player is enlisted and their lord is in battle.
        ///     Native system handles siege menus automatically.
        /// </summary>
        private void AddEnlistedEncounterOptions(CampaignGameStarter starter)
        {
            // Add "Wait in reserve" option for large battles (100+ troops)
            // This allows players to stay out of the initial fighting in large battles
            var waitInReserveText = new TextObject("{=combat_wait_reserve}Wait in reserve");

            // Add to the encounter menu for field battles only (Wait icon)
            // Native system handles siege menus automatically
            starter.AddGameMenuOption("encounter", "enlisted_wait_reserve",
                waitInReserveText.ToString(),
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Wait;
                    return IsWaitInReserveAvailable(args);
                },
                OnWaitInReserveSelected,
                false, 1);

            // Native system handles siege menus - no custom siege options needed

            // Add a custom "wait in reserve" menu for battles
            // This menu shows while the player is waiting in reserve and allows them to rejoin
            // Use shorter overload - MenuOverlayType.None is default
            starter.AddWaitGameMenu("enlisted_battle_wait",
                "Waiting in Reserve: {BATTLE_STATUS}",
                OnBattleWaitInit,
                OnBattleWaitCondition,
                null,
                OnBattleWaitTick,
                GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption);

            // Return to battle option (Mission icon for combat)
            var rejoinBattleText = new TextObject("{=combat_rejoin_battle}Rejoin the battle");
            starter.AddGameMenuOption("enlisted_battle_wait", "enlisted_rejoin_battle",
                rejoinBattleText.ToString(),
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Mission;
                    return true;
                },
                OnRejoinBattleSelected,
                false, 1);
        }

        /// <summary>
        ///     Checks if the "Wait in Reserve" option should be available in the encounter menu.
        ///     This option is only available for enlisted soldiers in large battles (100+ troops),
        ///     allowing players to stay out of the initial fighting when the army is large enough.
        /// </summary>
        /// <param name="args">Menu callback arguments containing menu state and context.</param>
        /// <returns>True if the option should be available, false otherwise.</returns>
        private bool IsWaitInReserveAvailable(MenuCallbackArgs args)
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment?.IsEnlisted != true)
            {
                return false;
            }

            var lord = enlistment!.CurrentLord;
            if (lord == null)
            {
                return false;
            }

            var lordParty = lord.PartyBelongedTo;

            // The MapEvent property exists on Party, not directly on MobileParty
            // This is the correct API structure for checking battle state
            if (lordParty?.Party.MapEvent == null)
            {
                return false;
            }

            // CRITICAL: Do NOT allow "Wait in Reserve" during siege battles
            // Siege battles have their own menu system and custom menus cause loops/crashes
            // Only available for field battles (not sieges)
            var isSiegeBattle = lordParty.Party.SiegeEvent != null ||
                                lordParty.Party.MapEvent?.IsSiegeAssault == true ||
                                lordParty.Party.MapEvent?.EventType == MapEvent.BattleTypes.Siege;

            if (isSiegeBattle)
            {
                args.IsEnabled = false;
                args.Tooltip = new TextObject("{=combat_reserve_siege_disabled}Wait in reserve is not available during siege battles");
                ModLogger.Debug("Siege", "Wait in reserve disabled - siege battle detected");
                return false;
            }

            // BUGFIX: Allow waiting in reserve if player morale is too low to fight, regardless of troop count
            if (MobileParty.MainParty.Morale <= 1f)
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Wait;
                args.Tooltip = new TextObject("{=combat_too_demoralized}You are too demoralized to fight.");
                return true;
            }


            args.optionLeaveType = GameMenuOption.LeaveType.Wait;
            return true;
        }

        /// <summary>
        ///     Handles the player selecting "Wait in Reserve" from the encounter menu.
        ///     Exits the current encounter and switches to the battle wait menu where
        ///     the player can monitor the battle and rejoin when ready.
        /// </summary>
        /// <param name="args">Menu callback arguments containing menu state and context.</param>
        private void OnWaitInReserveSelected(MenuCallbackArgs args)
        {
            try
            {
                // CRITICAL: Check if this is a siege battle - do NOT allow wait menu during sieges
                // Siege battles have their own menu system and custom menus cause loops/crashes
                var enlistmentBehavior = EnlistmentBehavior.Instance;
                var lordParty = enlistmentBehavior?.CurrentLord?.PartyBelongedTo;
                var isSiegeBattle = lordParty?.Party.SiegeEvent != null ||
                                    lordParty?.Party.MapEvent?.IsSiegeAssault == true ||
                                    lordParty?.Party.MapEvent?.EventType == MapEvent.BattleTypes.Siege;

                if (isSiegeBattle)
                {
                    ModLogger.Info("Battle",
                        "Prevented wait in reserve during siege battle - native system handles siege menus");
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("{=combat_reserve_siege_disabled_full}Wait in reserve is not available during siege battles.").ToString()));
                    return; // Don't switch menus during sieges
                }

                // CRITICAL: Set waiting in reserve flag BEFORE PlayerEncounter.Finish()
                // This prevents OnPlayerBattleEnd from awarding XP when we're just entering reserve mode
                IsWaitingInReserve = true;

                // Exit the current encounter and switch to the wait menu
                // This allows the player to wait out the battle without participating
                if (PlayerEncounter.Current != null)
                {
                    PlayerEncounter.Finish();
                }

                // Switch to the battle wait menu where the player can monitor the battle
                GameMenu.ActivateGameMenu("enlisted_battle_wait");
                ModLogger.StateChange("Battle", "Fighting", "Reserve",
                    "Player chose to wait in reserve during large battle");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Battle", $"Error entering reserve mode: {ex.Message}");
            }
        }

        /// <summary>
        ///     Initialize battle wait menu.
        /// </summary>
        private void OnBattleWaitInit(MenuCallbackArgs args)
        {
            try
            {
                // 1.3.4+: Set proper menu background - MUST be set before anything else
                // to avoid "temp background" assertion failure
                var backgroundMesh = "encounter_looter"; // Safe fallback
                var lord = EnlistmentBehavior.Instance?.CurrentLord;

                if (lord?.Clan?.Kingdom?.Culture?.EncounterBackgroundMesh != null)
                {
                    backgroundMesh = lord.Clan.Kingdom.Culture.EncounterBackgroundMesh;
                }
                else if (lord?.Culture?.EncounterBackgroundMesh != null)
                {
                    backgroundMesh = lord.Culture.EncounterBackgroundMesh;
                }

                args.MenuContext.SetBackgroundMeshName(backgroundMesh);

                // StartWait() automatically enables time progression for WaitMenuHideProgressAndHoursOption menus
                // Time will continue at normal speed, and player can pause/unpause with spacebar
                args.MenuContext.GameMenu.StartWait();

                ModLogger.Info("Battle",
                    "Started wait in reserve - time will continue at normal speed (can pause with spacebar)");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Battle", $"Error initializing battle wait menu: {ex.Message}");
            }
        }

        /// <summary>
        ///     Condition for battle wait menu.
        /// </summary>
        private bool OnBattleWaitCondition(MenuCallbackArgs args)
        {
            var enlistment = EnlistmentBehavior.Instance;
            var lord = enlistment?.CurrentLord;
            var lordParty = lord?.PartyBelongedTo;

            // Update battle status text
            // CORRECT API: Use Party.MapEvent (not direct on MobileParty)
            if (lordParty?.Party.MapEvent != null)
            {
                var battleStatus = "Your lord is engaged in battle";
                MBTextManager.SetTextVariable("BATTLE_STATUS", battleStatus);
            }
            else
            {
                MBTextManager.SetTextVariable("BATTLE_STATUS", "Battle has ended");
            }

            return enlistment?.IsEnlisted == true;
        }

        /// <summary>
        ///     Tick handler for the battle wait menu that runs while the player is waiting in reserve.
        ///     Monitors battle state, checks if the native system wants to show a different menu,
        ///     and automatically returns the player to battle if troop count drops below 100.
        ///     Includes time delta validation to prevent assertion failures.
        /// </summary>
        /// <param name="args">Menu callback arguments containing menu state and context.</param>
        /// <param name="dt">Time elapsed since last tick, in campaign time. Must be positive.</param>
        private void OnBattleWaitTick(MenuCallbackArgs args, CampaignTime dt)
        {
            try
            {
                // Validate time delta to prevent assertion failures
                // Zero-delta-time updates can cause assertion failures in the rendering system
                if (dt.ToSeconds <= 0)
                {
                    return;
                }

                var enlistment = EnlistmentBehavior.Instance;
                var lord = enlistment?.CurrentLord;
                var lordParty = lord?.PartyBelongedTo;
                var isSiegeBattle = lordParty?.Party.SiegeEvent != null ||
                                    lordParty?.Party.MapEvent?.IsSiegeAssault == true ||
                                    lordParty?.Party.MapEvent?.EventType == MapEvent.BattleTypes.Siege;
                // If isSiegeBattle is true, lordParty is guaranteed to be non-null (all conditions use null-conditional)
                var siegeAssaultStarted = isSiegeBattle && lordParty!.Party.MapEvent != null;

                // If the actual assault has begun (MapEvent active), exit the reserve menu immediately so the native encounter can start
                if (siegeAssaultStarted)
                {
                    args.MenuContext.GameMenu.EndWait();
                    ModLogger.Info("Battle", "Siege assault started - exiting reserve menu for native encounter");
                    NextFrameDispatcher.RunNextFrame(() =>
                    {
                        if (Campaign.Current?.CurrentMenuContext != null)
                        {
                            GameMenu.ExitToLast();
                        }
                    });
                    return;
                }

                // During siege prep (no assault yet) let the native system manage menus without interference
                if (isSiegeBattle)
                {
                    ModLogger.Debug("Battle", "Siege preparation detected - holding reserve menu to avoid conflicts");
                    return;
                }

                // Check what menu the native game system wants to show based on current state
                // The native system may want to show army_wait, menu_siege_strategies, or other menus
                // We should respect this and switch to the native menu to avoid blocking battle flow
                // BUT: Only check once per battle state change, not every tick
                var genericStateMenu = Campaign.Current.Models.EncounterGameMenuModel.GetGenericStateMenu();

                // CRITICAL: During battle, STAY in enlisted_battle_wait - don't switch to ANY menu
                // The native game will try to push us to army_wait or encounter, but switching
                // causes a loop where the game pushes back to encounter because the battle is still active
                // Only allow menu switching when the battle has actually ended (no MapEvent)
                var lordStillInBattle = lordParty?.Party.MapEvent != null;

                if (lordStillInBattle)
                {
                    // Lord is still in battle - stay in reserve, don't switch menus
                    // Switching to army_wait or encounter will cause the game to loop back
                    ModLogger.Debug("Battle", "Staying in reserve - lord still in battle");
                    return;
                }

                // Battle has ended - now we can switch to whatever menu the native system wants
                if (!string.IsNullOrEmpty(genericStateMenu) &&
                    genericStateMenu != "enlisted_battle_wait")
                {
                    // Clear the waiting in reserve flag - battle has ended
                    IsWaitingInReserve = false;
                    args.MenuContext.GameMenu.EndWait();
                    ModLogger.Info("Battle", $"Battle ended - switching to native menu '{genericStateMenu}'");
                    GameMenu.SwitchToMenu(genericStateMenu);
                    return;
                }

                // Check if the battle has ended
                // If so, let the native system decide what menu to show next
                // Don't force a return to the enlisted menu - let GetGenericStateMenu() determine it
                // Note: lord and lordParty are already declared above (lines 300-301)

                // The MapEvent property exists on Party, not directly on MobileParty
                // This is the correct API structure for checking battle state
                if (lordParty?.Party.MapEvent == null && string.IsNullOrEmpty(genericStateMenu))
                {
                    // Clear the waiting in reserve flag - battle has ended
                    IsWaitingInReserve = false;
                    // Battle ended and native system doesn't want a specific menu
                    // Exit and let the menu tick handler or native system decide the next menu
                    args.MenuContext.GameMenu.EndWait();
                    GameMenu.ExitToLast();
                    ModLogger.Info("Battle",
                        "Battle ended - exiting battle wait menu, native system will show appropriate menu");
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Battle", $"Error in battle wait tick: {ex.Message}");
            }
        }

        /// <summary>
        ///     Handle rejoin battle selection.
        /// </summary>
        private void OnRejoinBattleSelected(MenuCallbackArgs args)
        {
            try
            {
                // Clear the waiting in reserve flag - player is rejoining battle
                IsWaitingInReserve = false;

                args.MenuContext.GameMenu.EndWait();

                NextFrameDispatcher.RunNextFrame(() =>
                {
                    var encounterModel = Campaign.Current?.Models?.EncounterGameMenuModel;
                    var desiredMenu = encounterModel?.GetGenericStateMenu();

                    if (!string.IsNullOrEmpty(desiredMenu) && desiredMenu != "enlisted_battle_wait")
                    {
                        ModLogger.Info("Battle", $"Rejoin requested - switching to native menu '{desiredMenu}'");
                        GameMenu.SwitchToMenu(desiredMenu);
                    }
                    else if (PlayerEncounter.Current != null)
                    {
                        ModLogger.Info("Battle", "Rejoin requested - forcing encounter menu");
                        GameMenu.SwitchToMenu("encounter");
                    }
                    else
                    {
                        ModLogger.Debug("Battle", "Rejoin requested but no native menu available - leaving wait menu");
                        GameMenu.ExitToLast();
                    }
                });

                ModLogger.Info("Battle", "Player rejoining battle from reserve");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Battle", $"Error rejoining battle: {ex.Message}");
            }
        }


    }
}
