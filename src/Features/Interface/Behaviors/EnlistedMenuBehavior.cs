using System;
using System.Collections.Generic;
using System.Text;
using Enlisted.Features.Assignments.Behaviors;
using Enlisted.Features.Assignments.Core;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Equipment.Behaviors;
using Enlisted.Mod.Core;
using Enlisted.Mod.Core.Logging;
using Enlisted.Mod.Entry;

// 1.3.4 API: ImageIdentifier moved here

namespace Enlisted.Features.Interface.Behaviors
{
    /// <summary>
    ///     Menu system for enlisted military service providing comprehensive status display,
    ///     interactive duty management, and professional military interface.
    ///     This system provides rich, real-time information about military service status including
    ///     detailed progression tracking, army information, duties management, and service records.
    ///     Handles menu creation, state management, and integration with the native game menu system.
    /// </summary>
    public sealed class EnlistedMenuBehavior : CampaignBehaviorBase
    {
        /// <summary>
        ///     Minimum time interval between menu updates, in seconds.
        ///     Updates are limited to once per second to provide real-time feel
        ///     without overwhelming the system with too-frequent refreshes.
        /// </summary>
        private readonly float _updateIntervalSeconds = 1.0f;

        /// <summary>
        ///     Currently active menu ID string.
        ///     Used to track which menu is currently open and determine when to refresh.
        /// </summary>
        private string _currentMenuId = "";

        /// <summary>
        ///     Last campaign time when the menu was updated.
        ///     Used to throttle menu updates to once per second.
        /// </summary>
        private CampaignTime _lastMenuUpdate = CampaignTime.Zero;

        /// <summary>
        ///     Whether the menu needs to be refreshed due to state changes.
        ///     Set to true when enlistment state, duties, or other menu-affecting data changes.
        /// </summary>
        private bool _menuNeedsRefresh;

        /// <summary>
        ///     Tracks if there's a pending return to the enlisted menu after settlement exit.
        ///     Used to defer menu activation until after settlement exit completes.
        /// </summary>
        private bool _pendingReturnToEnlistedMenu;

        /// <summary>
        ///     Campaign time when the player left a settlement.
        ///     Used to delay menu activation after settlement exit to prevent timing conflicts.
        /// </summary>
        private CampaignTime _settlementExitTime = CampaignTime.Zero;


        /// <summary>
        ///     Tracks whether we created a synthetic outside encounter for settlement access.
        ///     Used to clean up encounter state when leaving settlements.
        /// </summary>
        private bool _syntheticOutsideEncounter;

        public EnlistedMenuBehavior()
        {
            Instance = this;
        }

        public static EnlistedMenuBehavior Instance { get; private set; }

        /// <summary>
        ///     Helper method to check if a party is in battle or siege.
        ///     This prevents PlayerSiege assertion failures by ensuring we don't finish encounters during sieges.
        /// </summary>
        private static bool InBattleOrSiege(MobileParty party)
        {
            return party?.Party.MapEvent != null || party?.Party.SiegeEvent != null ||
                   party?.BesiegedSettlement != null;
        }

        // Note: ContainsParty helper removed - was unused utility method for battle side detection

        /// <summary>
        ///     Hourly tick handler that runs once per in-game hour for battle detection.
        ///     Checks if the lord is in battle and exits custom menus to allow native battle menus to appear.
        ///     Battle detection is handled in hourly ticks rather than real-time ticks to avoid
        ///     overwhelming the system with constant checks and to prevent assertion failures.
        /// </summary>
        private void OnHourlyTick()
        {
            if (!EnlistedActivation.EnsureActive())
            {
                return;
            }

            // Skip all processing if the player is not currently enlisted
            // This avoids unnecessary computation when the system isn't active
            var enlistmentBehavior = EnlistmentBehavior.Instance;
            if (enlistmentBehavior?.IsEnlisted != true)
            {
                return;
            }

            // Check if the lord's party is in battle or siege
            var lord = enlistmentBehavior.CurrentLord;
            var lordParty = lord?.PartyBelongedTo;

            if (lordParty != null)
            {
                // Check battle state for both the lord individually and siege-related battles
                // The MapEvent and SiegeEvent properties exist on Party, not directly on MobileParty
                // This is the correct API structure for checking battle state
                var lordInBattle = lordParty.Party.MapEvent != null;
                var lordInSiege = lordParty.Party.SiegeEvent != null;
                var siegeRelatedBattle = IsSiegeRelatedBattle(MobileParty.MainParty, lordParty);

                // Consider both regular battles and sieges as battles for menu management
                var lordInAnyBattle = lordInBattle || lordInSiege || siegeRelatedBattle;

                if (lordInAnyBattle)
                {
                    // Exit custom menus to allow the native system to show appropriate battle menus
                    // The native system will show army_wait, menu_siege_strategies, or other battle menus
                    // CRITICAL: Only exit if we have a valid menu context and it's our custom menu
                    if (_currentMenuId.StartsWith("enlisted_") && Campaign.Current?.CurrentMenuContext != null)
                    {
                        try
                        {
                            // Try to let the native encounter system take over
                            var desiredMenu = Campaign.Current.Models?.EncounterGameMenuModel?.GetGenericStateMenu();
                            if (!string.IsNullOrEmpty(desiredMenu))
                            {
                                ModLogger.Info("Interface",
                                    $"Battle detected - switching to native menu '{desiredMenu}'");
                                GameMenu.SwitchToMenu(desiredMenu);
                            }
                            else
                            {
                                // No specific menu - just return and let native system push its menu
                                ModLogger.Info("Interface",
                                    "Battle detected - letting native system handle menu (no specific menu)");
                            }
                        }
                        catch (Exception ex)
                        {
                            ModLogger.Error("Interface", $"Error handling battle menu transition: {ex.Message}");
                        }
                    }
                }
                // Don't automatically return to the enlisted menu after battles
                // The menu tick handler will check GetGenericStateMenu() and switch back when appropriate
            }
        }

        /// <summary>
        ///     Checks if it's safe to activate the enlisted status menu by verifying there are no
        ///     active battles, sieges, or encounters that would conflict with the menu display.
        ///     This prevents menu activation during critical game state transitions that could cause
        ///     assertion failures or menu conflicts.
        /// </summary>
        /// <returns>True if the menu can be safely activated, false if a conflict exists.</returns>
        public static bool SafeToActivateEnlistedMenu()
        {
            var enlist = EnlistmentBehavior.Instance;
            var main = MobileParty.MainParty;
            var lord = enlist?.CurrentLord?.PartyBelongedTo;

            // Check for conflicts that would prevent menu activation
            // The MapEvent and SiegeEvent properties exist on Party, not directly on MobileParty
            // This is the correct API structure for checking battle state
            var playerBattle = main?.Party.MapEvent != null;
            var playerEncounter = PlayerEncounter.Current != null;
            var lordSiegeEvent = lord?.Party.SiegeEvent != null;
            var siegeRelatedBattle = IsSiegeRelatedBattle(main, lord);

            // If any conflict exists, prevent menu activation
            // This ensures menus don't interfere with battles, sieges, or encounters
            var conflict = playerBattle || playerEncounter || lordSiegeEvent || siegeRelatedBattle;

            if (conflict)
            {
                ModLogger.Debug("Interface",
                    $"Menu activation blocked - battle: {playerBattle}, encounter: {playerEncounter}, siege: {lordSiegeEvent}");
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Detects siege-related battles like sally-outs where formal siege state may be paused.
        /// </summary>
        private static bool IsSiegeRelatedBattle(MobileParty main, MobileParty lord)
        {
            try
            {
                // Check if current battle has siege-related types
                var mapEvent = main?.MapEvent ?? lord?.MapEvent;
                if (mapEvent != null)
                {
                    // Check for siege battle types: SiegeOutside, SiegeAssault, etc.
                    var battleType = mapEvent.EventType.ToString();
                    var mapEventString = mapEvent.ToString();

                    var isSiegeType = battleType.Contains("Siege") ||
                                      mapEventString.Contains("Siege") ||
                                      mapEventString.Contains("SiegeOutside");

                    if (isSiegeType)
                    {
                        ModLogger.Info("Interface",
                            $"SIEGE BATTLE DETECTED: Type='{battleType}', Event='{mapEventString}'");
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", $"Error in siege battle detection: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        ///     Safely activates the enlisted status menu by checking for conflicts and respecting
        ///     the native menu system's state. Checks if battles, sieges, or encounters are active,
        ///     and verifies what menu the native system wants to show before activating.
        /// </summary>
        public static void SafeActivateEnlistedMenu()
        {
            // First check if we can activate (battle/encounter guards prevent activation during conflicts)
            if (!SafeToActivateEnlistedMenu())
            {
                return;
            }

            // Check what menu the game system wants to show based on current campaign state
            // We override army_wait (we provide our own army controls), but respect battle/encounter menus
            try
            {
                var genericStateMenu = Campaign.Current.Models.EncounterGameMenuModel.GetGenericStateMenu();

                // Menus we should NOT override - these are active battle/encounter states
                var isBattleMenu = !string.IsNullOrEmpty(genericStateMenu) &&
                                   (genericStateMenu.Contains("encounter") ||
                                    genericStateMenu.Contains("siege") ||
                                    genericStateMenu.Contains("battle") ||
                                    genericStateMenu.Contains("prisoner") ||
                                    genericStateMenu.Contains("captured"));

                if (isBattleMenu)
                {
                    // Native system wants a battle/encounter menu - respect it
                    ModLogger.Debug("Menu", $"Respecting battle menu '{genericStateMenu}'");
                    return;
                }

                // Override army_wait and other non-battle menus with our enlisted menu
                // This ensures enlisted players always see their status menu when not in combat
                if (!string.IsNullOrEmpty(genericStateMenu) && genericStateMenu != "enlisted_status")
                {
                    ModLogger.Debug("Menu", $"Overriding '{genericStateMenu}' with enlisted_status");
                }

                ModLogger.Info("Menu", "Activating enlisted status menu");
                // Capture time state BEFORE menu activation (vanilla sets Stop, then StartWait sets FastForward)
                QuartermasterManager.CaptureTimeStateBeforeMenuActivation();
                GameMenu.ActivateGameMenu("enlisted_status");
            }
            catch (Exception ex)
            {
                // Fallback to original behavior if GetGenericStateMenu() fails
                // This ensures the menu can still be activated even if the check fails
                ModLogger.Debug("Interface", $"Error checking GetGenericStateMenu, using fallback: {ex.Message}");
                QuartermasterManager.CaptureTimeStateBeforeMenuActivation();
                GameMenu.ActivateGameMenu("enlisted_status");
            }
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.TickEvent.AddNonSerializedListener(this, OnTick);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
            CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, OnMenuOpened);

            // Track lord settlement entry to adjust menu option visibility
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEnteredForButton);

            // Auto-return to enlisted menu when leaving towns/castles
            CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnSettlementLeftReturnToCamp);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Menu behavior has no persistent state - all data comes from other behaviors
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            // Set up global gold icon for inline currency display across all menus
            MBTextManager.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");

            AddEnlistedMenus(starter);
            ModLogger.Info("Interface", "Enlisted menu system initialized with modern UI styling");
        }

        /// <summary>
        ///     Menu background initialization for enlisted_status menu.
        ///     Sets culture-appropriate background and ambient audio for modern feel.
        ///     Resumes time so it continues passing while browsing menus.
        /// </summary>
        [GameMenuInitializationHandler("enlisted_status")]
        private static void OnEnlistedStatusBackgroundInit(MenuCallbackArgs args)
        {
            var enlistment = EnlistmentBehavior.Instance;
            var backgroundMesh = "encounter_looter";

            if (enlistment?.CurrentLord?.Clan?.Kingdom?.Culture?.EncounterBackgroundMesh != null)
            {
                backgroundMesh = enlistment.CurrentLord.Clan.Kingdom.Culture.EncounterBackgroundMesh;
            }
            else if (enlistment?.CurrentLord?.Culture?.EncounterBackgroundMesh != null)
            {
                backgroundMesh = enlistment.CurrentLord.Culture.EncounterBackgroundMesh;
            }

            args.MenuContext.SetBackgroundMeshName(backgroundMesh);
            args.MenuContext.SetAmbientSound("event:/map/ambient/node/settlements/2d/camp_army");
            args.MenuContext.SetPanelSound("event:/ui/panels/settlement_camp");

            // Resume time - native GameMenu.SwitchToMenu() stops time by default,
            // but we want time to continue passing while browsing enlisted menus
            Campaign.Current.TimeControlMode = CampaignTimeControlMode.StoppablePlay;
        }

        /// <summary>
        ///     Menu background initialization for enlisted_duty_selection menu.
        ///     Sets culture-appropriate background and ambient audio.
        ///     Resumes time so it continues passing while browsing menus.
        /// </summary>
        [GameMenuInitializationHandler("enlisted_duty_selection")]
        private static void OnDutySelectionBackgroundInit(MenuCallbackArgs args)
        {
            var enlistment = EnlistmentBehavior.Instance;
            var backgroundMesh = "encounter_looter";

            if (enlistment?.CurrentLord?.Clan?.Kingdom?.Culture?.EncounterBackgroundMesh != null)
            {
                backgroundMesh = enlistment.CurrentLord.Clan.Kingdom.Culture.EncounterBackgroundMesh;
            }
            else if (enlistment?.CurrentLord?.Culture?.EncounterBackgroundMesh != null)
            {
                backgroundMesh = enlistment.CurrentLord.Culture.EncounterBackgroundMesh;
            }

            args.MenuContext.SetBackgroundMeshName(backgroundMesh);
            args.MenuContext.SetAmbientSound("event:/map/ambient/node/settlements/2d/camp_army");
            args.MenuContext.SetPanelSound("event:/ui/panels/settlement_camp");

            // Resume time - native GameMenu.SwitchToMenu() stops time by default
            Campaign.Current.TimeControlMode = CampaignTimeControlMode.StoppablePlay;
        }

        /// <summary>
        ///     Menu background initialization for enlisted_desert_confirm menu.
        ///     Sets ominous background for desertion confirmation.
        ///     Resumes time so it continues passing while browsing menus.
        /// </summary>
        [GameMenuInitializationHandler("enlisted_desert_confirm")]
        private static void OnDesertConfirmBackgroundInit(MenuCallbackArgs args)
        {
            var enlistment = EnlistmentBehavior.Instance;
            var backgroundMesh = "encounter_looter";

            if (enlistment?.CurrentLord?.Clan?.Kingdom?.Culture?.EncounterBackgroundMesh != null)
            {
                backgroundMesh = enlistment.CurrentLord.Clan.Kingdom.Culture.EncounterBackgroundMesh;
            }
            else if (enlistment?.CurrentLord?.Culture?.EncounterBackgroundMesh != null)
            {
                backgroundMesh = enlistment.CurrentLord.Culture.EncounterBackgroundMesh;
            }

            args.MenuContext.SetBackgroundMeshName(backgroundMesh);
            args.MenuContext.SetAmbientSound("event:/map/ambient/node/settlements/2d/camp_army");

            // Resume time - native GameMenu.SwitchToMenu() stops time by default
            Campaign.Current.TimeControlMode = CampaignTimeControlMode.StoppablePlay;
        }

        /// <summary>
        ///     Real-time tick handler that runs every game frame while the player is enlisted.
        ///     Handles menu state updates, menu transitions, and settlement access logic.
        ///     Includes time delta validation to prevent assertion failures, and defers
        ///     heavy processing to hourly ticks to avoid overwhelming the system.
        /// </summary>
        /// <param name="dt">Time elapsed since last frame, in seconds. Must be positive.</param>
        private void OnTick(float dt)
        {
            if (!EnlistedActivation.EnsureActive())
            {
                return;
            }

            // Skip all processing if the player is not currently enlisted
            // This avoids unnecessary computation when the system isn't active
            var enlistmentBehavior = EnlistmentBehavior.Instance;
            if (enlistmentBehavior?.IsEnlisted != true)
            {
                return;
            }

            // Validate time delta to prevent assertion failures
            // Zero-delta-time updates can cause assertion failures in the rendering system
            if (dt <= 0)
            {
                return;
            }

            // Real-time tick handles lightweight menu operations and state updates
            // Heavy processing like battle detection is moved to hourly ticks to avoid
            // overwhelming the system with constant checks

            // Real-time menu updates for dynamic information
            if (CampaignTime.Now - _lastMenuUpdate > CampaignTime.Seconds((long)_updateIntervalSeconds))
            {
                if (_currentMenuId.StartsWith("enlisted_") && _menuNeedsRefresh)
                {
                    RefreshCurrentMenu();
                    _lastMenuUpdate = CampaignTime.Now;
                    _menuNeedsRefresh = false;
                }
            }

            // Only activate menu after settlement exit if the native system allows it
            // Check GetGenericStateMenu() first to see if the native system wants a different menu
            // This prevents conflicts with battle menus that might be starting
            if (_pendingReturnToEnlistedMenu &&
                CampaignTime.Now - _settlementExitTime > CampaignTime.Milliseconds(500))
            {
                try
                {
                    var enlistment = EnlistmentBehavior.Instance;
                    var isEnlisted = enlistment?.IsEnlisted == true;

                    // Check what menu native system wants to show
                    var genericStateMenu = Campaign.Current.Models.EncounterGameMenuModel.GetGenericStateMenu();

                    // Only activate our menu if native system says it's OK (null or our menu)
                    if (isEnlisted && (genericStateMenu == "enlisted_status" || string.IsNullOrEmpty(genericStateMenu)))
                    {
                        // Double-check no active battle or encounter
                        var hasEncounter = PlayerEncounter.Current != null;
                        var inSettlement = MobileParty.MainParty.CurrentSettlement != null;
                        var inBattle = MobileParty.MainParty?.Party.MapEvent != null;

                        if (!hasEncounter && !inSettlement && !inBattle)
                        {
                            ModLogger.Info("Interface",
                                "Deferred menu activation: conditions met, activating enlisted menu");
                            SafeActivateEnlistedMenu();
                        }
                    }
                    else if (!string.IsNullOrEmpty(genericStateMenu))
                    {
                        ModLogger.Debug("Interface",
                            $"Deferred menu activation skipped: native system wants '{genericStateMenu}'");
                    }

                    _pendingReturnToEnlistedMenu = false; // Clear flag regardless of outcome
                }
                catch (Exception ex)
                {
                    ModLogger.Error("Interface", $"Deferred enlisted menu activation error: {ex.Message}");
                    _pendingReturnToEnlistedMenu = false; // Clear flag to prevent endless retries
                }
            }
        }

        private void OnMenuOpened(MenuCallbackArgs args)
        {
            var previousMenu = _currentMenuId;
            _currentMenuId = args.MenuContext.GameMenu.StringId;
            _menuNeedsRefresh = true;

            // Log menu state transition when enlisted for debugging menu transitions
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment?.IsEnlisted == true)
            {
                // Log state transition with previous menu info
                ModLogger.StateChange("Menu",
                    string.IsNullOrEmpty(previousMenu) ? "None" : previousMenu,
                    _currentMenuId);

                // Check all siege/battle conditions to detect state conflicts
                var lord = enlistment.CurrentLord;
                var main = MobileParty.MainParty;
                var playerBattle = main?.Party.MapEvent != null;
                var playerEncounter = PlayerEncounter.Current != null;
                // Check for siege events using the SiegeEvent property on Party
                var lordSiegeEvent = lord?.PartyBelongedTo?.Party.SiegeEvent != null;

                // Check for siege-related battles like sally-outs
                var siegeRelatedBattle = IsSiegeRelatedBattle(main, lord?.PartyBelongedTo);

                ModLogger.Trace("Menu",
                    $"Context: battle={playerBattle}, encounter={playerEncounter}, siege={lordSiegeEvent}");

                if (lordSiegeEvent || siegeRelatedBattle)
                {
                    var battleInfo = siegeRelatedBattle ? " (sally-out)" : "";
                    ModLogger.Debug("Siege", $"Menu '{_currentMenuId}' opened during siege{battleInfo}");

                    if (_currentMenuId == "enlisted_status")
                    {
                        ModLogger.Warn("Menu", "Enlisted menu opened during siege - should have been blocked");
                    }
                }

                // Override army_wait and army_wait_at_settlement menus when enlisted
                // These are native army menus that appear when the lord leaves settlements or during army operations
                // Enlisted soldiers should see their custom menu instead, unless in combat/siege
                if (_currentMenuId == "army_wait" || _currentMenuId == "army_wait_at_settlement")
                {
                    // Don't override during battles or sieges
                    if (!playerBattle && !playerEncounter && !lordSiegeEvent && !siegeRelatedBattle)
                    {
                        // Defer the override to next frame to avoid conflicts with the native menu system
                        NextFrameDispatcher.RunNextFrame(() =>
                        {
                            if (enlistment?.IsEnlisted == true)
                            {
                                SafeActivateEnlistedMenu();
                            }
                        });
                    }
                }
            }
        }

        /// <summary>
        ///     Registers all enlisted menu options and submenus with the game starter.
        ///     Creates the main enlisted status menu, duty selection menu, and return to army options.
        ///     All functionality is consolidated into a single menu system for clarity and simplicity.
        /// </summary>
        private void AddEnlistedMenus(CampaignGameStarter starter)
        {
            AddMainEnlistedStatusMenu(starter);

            // Add direct siege battle option to enlisted menu as fallback
            // This allows players to join siege battles if other methods fail
            ModLogger.Info("Interface", "Adding emergency siege battle option to enlisted_status menu");
            try
            {
                starter.AddGameMenuOption("enlisted_status", "emergency_siege_battle",
                    "{=Enlisted_Menu_JoinSiege}Join siege battle",
                    IsEmergencySiegeBattleAvailable,
                    OnEmergencySiegeBattleSelected,
                    false, 7); // After ask leave option
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", $"Failed to add emergency siege battle option: {ex.Message}");
            }

            // Add "Return to camp" options to native town/castle menus for enlisted players
            AddReturnToCampOptions(starter);
        }

        /// <summary>
        ///     Adds "Return to camp" options to native town and castle menus.
        ///     These allow enlisted players to return to the enlisted status menu from settlements.
        ///     Covers: main menus, outside menus, guard menus, and bribe menus to ensure
        ///     enlisted players always have an exit option even when native Leave buttons are hidden.
        /// </summary>
        private void AddReturnToCampOptions(CampaignGameStarter starter)
        {
            try
            {
                // Town menu (inside town)
                starter.AddGameMenuOption("town", "enlisted_return_to_camp",
                    "{=Enlisted_Menu_ReturnToCamp}Return to camp",
                    IsReturnToCampAvailable,
                    OnReturnToCampSelected,
                    true, 100); // Leave type, high priority to show near bottom

                // Town outside menu
                starter.AddGameMenuOption("town_outside", "enlisted_return_to_camp",
                    "{=Enlisted_Menu_ReturnToCamp}Return to camp",
                    IsReturnToCampAvailable,
                    OnReturnToCampSelected,
                    true, 100);

                // Castle outside menu
                starter.AddGameMenuOption("castle_outside", "enlisted_return_to_camp",
                    "{=Enlisted_Menu_ReturnToCamp}Return to camp",
                    IsReturnToCampAvailable,
                    OnReturnToCampSelected,
                    true, 100);

                // Castle menu (inside castle)
                starter.AddGameMenuOption("castle", "enlisted_return_to_camp",
                    "{=Enlisted_Menu_ReturnToCamp}Return to camp",
                    IsReturnToCampAvailable,
                    OnReturnToCampSelected,
                    true, 100);

                // Castle guard menu (when approaching castle gates)
                // This menu's native "Back" button uses game_menu_leave_on_condition which we patch
                starter.AddGameMenuOption("castle_guard", "enlisted_return_to_camp",
                    "{=Enlisted_Menu_ReturnToCamp}Return to camp",
                    IsReturnToCampAvailable,
                    OnReturnToCampSelected,
                    true, 100);

                // Castle bribe menu (when guards require bribe to enter)
                // This menu's native "Leave" button uses game_menu_leave_on_condition which we patch
                starter.AddGameMenuOption("castle_enter_bribe", "enlisted_return_to_camp",
                    "{=Enlisted_Menu_ReturnToCamp}Return to camp",
                    IsReturnToCampAvailable,
                    OnReturnToCampSelected,
                    true, 100);

                // Town guard menu (when approaching town gates)
                starter.AddGameMenuOption("town_guard", "enlisted_return_to_camp",
                    "{=Enlisted_Menu_ReturnToCamp}Return to camp",
                    IsReturnToCampAvailable,
                    OnReturnToCampSelected,
                    true, 100);

                // Town keep bribe menu (when guards require bribe to enter keep)
                starter.AddGameMenuOption("town_keep_bribe", "enlisted_return_to_camp",
                    "{=Enlisted_Menu_ReturnToCamp}Return to camp",
                    IsReturnToCampAvailable,
                    OnReturnToCampSelected,
                    true, 100);

                ModLogger.Info("Interface",
                    "Added 'Return to camp' options to town/castle menus (including guard and bribe menus)");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", $"Failed to add Return to camp options: {ex.Message}");
            }
        }

        /// <summary>
        ///     Checks if the "Return to camp" option should be available.
        ///     Only shows when player is enlisted.
        /// </summary>
        private bool IsReturnToCampAvailable(MenuCallbackArgs args)
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment?.IsEnlisted != true)
            {
                return false;
            }

            args.optionLeaveType = GameMenuOption.LeaveType.Leave;
            return true;
        }

        /// <summary>
        ///     Handles returning to the enlisted camp from a settlement.
        /// </summary>
        private void OnReturnToCampSelected(MenuCallbackArgs args)
        {
            try
            {
                // Leave the settlement encounter
                if (PlayerEncounter.Current != null)
                {
                    if (PlayerEncounter.InsideSettlement)
                    {
                        PlayerEncounter.LeaveSettlement();
                    }

                    PlayerEncounter.Finish();
                }

                // Return to enlisted status menu
                NextFrameDispatcher.RunNextFrame(() =>
                {
                    SafeActivateEnlistedMenu();
                    ModLogger.Info("Interface", "Returned to camp from settlement");
                });
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", $"Error returning to camp: {ex.Message}");
            }
        }

        // Note: SimpleArmyBattleCondition method removed - was unused utility condition

        /// <summary>
        ///     Check if emergency siege battle option should be available.
        ///     Only show when lord is in a siege battle.
        /// </summary>
        private bool IsEmergencySiegeBattleAvailable(MenuCallbackArgs args)
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment?.IsEnlisted != true)
            {
                return false;
            }

            var lord = enlistment?.CurrentLord;
            var lordParty = lord?.PartyBelongedTo;

            // Check if lord is in a siege battle
            var lordInSiege = lordParty?.Party.SiegeEvent != null;
            var lordInBattle = lordParty?.Party.MapEvent != null;

            // Show option if lord is in siege or siege-related battle
            return lordInSiege || (lordInBattle && IsSiegeRelatedBattle(MobileParty.MainParty, lordParty));
        }

        /// <summary>
        ///     Handle emergency siege battle selection.
        /// </summary>
        private void OnEmergencySiegeBattleSelected(MenuCallbackArgs args)
        {
            try
            {
                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment?.IsEnlisted != true)
                {
                    return;
                }

                // After the check above, enlistment is guaranteed non-null
                var lord = enlistment.CurrentLord;
                var lordParty = lord?.PartyBelongedTo;

                ModLogger.Info("Interface", "EMERGENCY SIEGE BATTLE: Player selected siege battle option");

                // Check what type of siege situation we're in
                if (lordParty?.Party.SiegeEvent != null)
                {
                    ModLogger.Info("Interface", "Lord is in active siege - attempting to join siege assault");
                    // Siege assault participation is handled by the native game system
                    // The player's army membership automatically includes them in the siege
                    InformationManager.DisplayMessage(new InformationMessage("Joining siege assault..."));
                }
                // Check if the lord is in a siege-related battle (like sally-outs)
                else if (lordParty?.Party.MapEvent != null)
                {
                    ModLogger.Info("Interface", "Lord is in siege-related battle - attempting to join");
                    // Battle participation is handled by the native game system
                    // The player's army membership automatically includes them in the battle
                    InformationManager.DisplayMessage(new InformationMessage("Joining siege battle..."));
                }
                else
                {
                    ModLogger.Info("Interface", "No siege situation detected - option should not have been available");
                    InformationManager.DisplayMessage(new InformationMessage("No siege battle available."));
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", $"Error in emergency siege battle: {ex.Message}");
            }
        }

        /// <summary>
        ///     Registers the main enlisted status menu with comprehensive military service information.
        ///     This is a wait menu that displays real-time service status, progression, and army information.
        ///     Includes all menu options for managing military service, equipment, and duties.
        /// </summary>
        private void AddMainEnlistedStatusMenu(CampaignGameStarter starter)
        {
            // Create a wait menu with time controls but hides progress boxes
            // This provides the wait menu functionality (time controls) without showing progress bars
            // NOTE: Use MenuOverlayType.None to avoid showing the empty battle bar when not in combat
            // Create a wait menu with time controls but hides progress boxes
            // Using method group conversion instead of explicit delegate creation
            starter.AddWaitGameMenu("enlisted_status",
                "{=Enlisted_Menu_Status_Title}Party Leader: {PARTY_LEADER}\n{PARTY_TEXT}",
                OnEnlistedStatusInit,
                OnEnlistedStatusCondition,
                null, // No consequence for wait menu
                OnEnlistedStatusTick, // Tick handler for real-time updates
                GameMenu.MenuAndOptionType
                    .WaitMenuHideProgressAndHoursOption); // Wait menu template that hides progress boxes

            // Main menu options for enlisted status menu with modern icons and localized tooltips

            // Master at Arms - allows players to select troop equipment (TroopSelection icon)
            starter.AddGameMenuOption("enlisted_status", "enlisted_master_at_arms",
                "{=Enlisted_Menu_MasterAtArms}Master at Arms",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.TroopSelection;
                    args.Tooltip =
                        new TextObject(
                            "{=menu_tooltip_master}Select your troop type and equipment loadout based on your current tier.");
                    return IsMasterAtArmsAvailable(args);
                },
                OnMasterAtArmsSelected,
                false, 1);

            // Visit Quartermaster - equipment variant selection and management (Trade icon)
            starter.AddGameMenuOption("enlisted_status", "enlisted_quartermaster",
                "{=Enlisted_Menu_VisitQuartermaster}Visit Quartermaster",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Trade;
                    args.Tooltip =
                        new TextObject(
                            "{=menu_tooltip_quartermaster}Request equipment variants and manage party supplies.");
                    return IsQuartermasterAvailable(args);
                },
                OnQuartermasterSelected,
                false, 2);

            // My Lord... - conversation with the current lord (Conversation icon)
            starter.AddGameMenuOption("enlisted_status", "enlisted_talk_to",
                "{=Enlisted_Menu_TalkToLord}My Lord...",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Conversation;
                    args.Tooltip =
                        new TextObject(
                            "{=menu_tooltip_talk}Speak with nearby lords for quests, news, and relation building.");
                    return IsTalkToAvailable(args);
                },
                OnTalkToSelected,
                false, 3);

            // Visit Settlement - towns and castles only (Submenu icon)
            starter.AddGameMenuOption("enlisted_status", "enlisted_visit_settlement",
                "{VISIT_SETTLEMENT_TEXT}",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                    args.Tooltip =
                        new TextObject("{=menu_tooltip_visit}Enter the settlement while your lord is present.");
                    return IsVisitSettlementAvailable(args);
                },
                OnVisitTownSelected,
                false, 4);

            // Report for Duty - duty and profession selection (Manage icon)
            starter.AddGameMenuOption("enlisted_status", "enlisted_report_duty",
                "{=Enlisted_Menu_ReportDuty}Report for Duty",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Manage;
                    args.Tooltip =
                        new TextObject(
                            "{=menu_tooltip_duty}Select your daily duty and profession for bonuses and special abilities.");
                    return IsReportDutyAvailable(args);
                },
                OnReportDutySelected,
                false, 5);

            // Ask commander for leave - moved to bottom (Leave icon)
            starter.AddGameMenuOption("enlisted_status", "enlisted_ask_leave",
                "{=Enlisted_Menu_AskLeave}Ask commander for leave",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    args.Tooltip =
                        new TextObject(
                            "{=menu_tooltip_leave}Request temporary leave from service. You can return when ready.");
                    return IsAskLeaveAvailable(args);
                },
                OnAskLeaveSelected,
                false, 6);

            // Desert Army option - allows player to voluntarily leave with penalties (Escape icon)
            starter.AddGameMenuOption("enlisted_status", "enlisted_desert_army",
                "{=Enlisted_Menu_DesertArmy}Desert the Army",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Escape;
                    args.Tooltip =
                        new TextObject(
                            "{=menu_tooltip_desert}Abandon your post. WARNING: Severe relation and crime penalties!");
                    return IsDesertArmyAvailable(args);
                },
                OnDesertArmySelected,
                false, 7);

            // No "return to duties" option needed - player IS doing duties by being in this menu

            // Add duty selection menu
            AddDutySelectionMenu(starter);

            // Add desertion confirmation menu
            AddDesertionConfirmMenu(starter);
        }

        /// <summary>
        ///     Handles settlement exit by scheduling a deferred return to the enlisted menu.
        ///     When the player leaves a town or castle, this method schedules menu activation
        ///     for the next frame to avoid timing conflicts with other game systems during state transitions.
        /// </summary>
        /// <param name="party">The party that left the settlement.</param>
        /// <param name="settlement">The settlement that was left.</param>
        private void OnSettlementLeftReturnToCamp(MobileParty party, Settlement settlement)
        {
            try
            {
                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment?.IsEnlisted == true)
                {
                    var lordParty = enlistment.CurrentLord?.PartyBelongedTo;
                    // Refresh menu when lord leaves the settlement (so Visit Town disappears)
                    if (party == lordParty && settlement != null &&
                        (settlement.IsTown || settlement.IsVillage || settlement.IsCastle))
                    {
                        // Force full menu re-render by switching to the same menu
                        var menuContext = Campaign.Current?.CurrentMenuContext;
                        var currentMenuId = menuContext?.GameMenu?.StringId;
                        if (currentMenuId == "enlisted_status")
                        {
                            GameMenu.SwitchToMenu("enlisted_status");
                        }
                        else if (menuContext?.GameMenu != null)
                        {
                            Campaign.Current.GameMenuManager.RefreshMenuOptions(menuContext);
                        }
                    }
                }

                if (party != MobileParty.MainParty || enlistment?.IsEnlisted != true)
                {
                    return;
                }

                if (!(settlement?.IsTown == true || settlement?.IsCastle == true))
                {
                    return;
                }

                // CRITICAL: If lord is still in this settlement, don't schedule enlisted menu return
                // With escort AI, the player will be pulled right back in - let them stay in native town menu
                var lordPartyCheck = enlistment.CurrentLord?.PartyBelongedTo;
                if (lordPartyCheck?.CurrentSettlement == settlement)
                {
                    ModLogger.Debug("Interface",
                        "Lord still in settlement - skipping enlisted menu return, using native town menu");
                    return;
                }

                ModLogger.Info("Interface", $"Left {settlement.Name} - scheduling return to enlisted menu");

                // Only finish non-battle encounters when leaving settlements
                // Battle encounters should be preserved to allow battle participation
                if (PlayerEncounter.Current != null)
                {
                    var enlistedLord = EnlistmentBehavior.Instance?.CurrentLord;
                    var lordInBattle = enlistedLord?.PartyBelongedTo?.Party.MapEvent != null;

                    var lordParty = enlistedLord?.PartyBelongedTo;
                    if (!lordInBattle && !InBattleOrSiege(lordParty))
                    {
                        PlayerEncounter.Finish();
                        ModLogger.Debug("Interface", "Finished non-battle encounter on settlement exit");
                    }
                    else
                    {
                        ModLogger.Debug("Interface",
                            "Skipped finishing encounter - lord in battle, preserving vanilla battle menu");
                    }
                }

                // Restore "invisible escort" state if we created a synthetic outside encounter
                // This ensures the player party returns to following mode after leaving the settlement
                if (_syntheticOutsideEncounter)
                {
                    _syntheticOutsideEncounter = false;
                    // Defer IsActive change to next frame to prevent assertion failures
                    // Setting IsActive during settlement exit can interfere with the animation/skeleton update cycle
                    NextFrameDispatcher.RunNextFrame(() =>
                    {
                        if (MobileParty.MainParty != null)
                        {
                            MobileParty.MainParty.IsActive = false;
                        }
                    });
                }

                // Schedule deferred menu activation to avoid timing conflicts
                // This ensures other systems finish processing before we activate our menu
                // A short delay prevents conflicts with settlement exit animations and state transitions
                _pendingReturnToEnlistedMenu = true;
                _settlementExitTime = CampaignTime.Now;

                ModLogger.Debug("Interface", "Deferred enlisted menu return scheduled (0.5s delay)");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", $"OnSettlementLeftReturnToCamp error: {ex}");
                // Ensure we don't get stuck in pending state
                _pendingReturnToEnlistedMenu = false;
            }
        }

        /// <summary>
        ///     Add duty selection menu for choosing duties and professions.
        /// </summary>
        private void AddDutySelectionMenu(CampaignGameStarter starter)
        {
            // Use same wait menu format as main enlisted menu for consistency
            // Using method group conversion instead of explicit delegate creation
            starter.AddWaitGameMenu("enlisted_duty_selection",
                "{=Enlisted_Menu_Duty_Title}Duty Selection: {DUTY_STATUS}\n{DUTY_TEXT}",
                OnDutySelectionInit,
                OnDutySelectionCondition,
                null, // No consequence for wait menu
                OnDutySelectionTick,
                GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption); // Same as main menu

            // BACK OPTION (first, like main menu style) - Leave icon
            starter.AddGameMenuOption("enlisted_duty_selection", "duty_back",
                "{=Enlisted_Menu_BackToStatus}Back to enlisted status",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    return true;
                },
                OnDutyBackSelected,
                false, 1);

            // DUTIES HEADER - cleaner styling with em-dashes
            starter.AddGameMenuOption("enlisted_duty_selection", "duties_header",
                "{=Enlisted_Menu_Header_Duties}— DUTIES —",
                _ => true, // Show but make it a display-only option
                _ =>
                {
                    // Show message when clicked to indicate it's just a header
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject(
                                "{=Enlisted_Message_HeaderInfo_Duties}This is a section header. Select duties below.")
                            .ToString()));
                },
                false, 2);

            // DUTY OPTIONS - Dynamic text based on current selection
            starter.AddGameMenuOption("enlisted_duty_selection", "duty_enlisted",
                "{DUTY_ENLISTED_TEXT}",
                IsDutyEnlistedAvailable,
                OnDutyEnlistedSelected,
                false, 3);

            starter.AddGameMenuOption("enlisted_duty_selection", "duty_forager",
                "{DUTY_FORAGER_TEXT}",
                IsDutyForagerAvailable,
                OnDutyForagerSelected,
                false, 4);

            starter.AddGameMenuOption("enlisted_duty_selection", "duty_sentry",
                "{DUTY_SENTRY_TEXT}",
                IsDutySentryAvailable,
                OnDutySentrySelected,
                false, 5);

            starter.AddGameMenuOption("enlisted_duty_selection", "duty_messenger",
                "{DUTY_MESSENGER_TEXT}",
                IsDutyMessengerAvailable,
                OnDutyMessengerSelected,
                false, 6);

            starter.AddGameMenuOption("enlisted_duty_selection", "duty_pioneer",
                "{DUTY_PIONEER_TEXT}",
                IsDutyPioneerAvailable,
                OnDutyPioneerSelected,
                false, 7);

            // SPACER between duties and professions
            starter.AddGameMenuOption("enlisted_duty_selection", "section_spacer",
                " ",
                _ => true, // Show as visible separator
                _ => { }, // No action when clicked
                true, 8); // Disabled = true makes it gray and non-clickable

            // PROFESSIONS HEADER - cleaner styling with em-dashes
            starter.AddGameMenuOption("enlisted_duty_selection", "professions_header",
                "{=Enlisted_Menu_Header_Professions}— PROFESSIONS —",
                _ => true, // Show but make it a display-only option
                _ =>
                {
                    // Show message when clicked to indicate it's just a header
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject(
                                "{=Enlisted_Message_HeaderInfo_Professions}This is a section header. Select professions below.")
                            .ToString()));
                },
                false, 9);

            // PROFESSION OPTIONS (T3+) - Dynamic text based on current selection
            // Remove "None" profession as requested by user

            starter.AddGameMenuOption("enlisted_duty_selection", "prof_quarterhand",
                "{PROF_QUARTERHAND_TEXT}",
                IsProfQuarterhandAvailable,
                OnProfQuarterhandSelected,
                false, 10);

            starter.AddGameMenuOption("enlisted_duty_selection", "prof_field_medic",
                "{PROF_FIELD_MEDIC_TEXT}",
                IsProfFieldMedicAvailable,
                OnProfFieldMedicSelected,
                false, 11);

            starter.AddGameMenuOption("enlisted_duty_selection", "prof_siegewright",
                "{PROF_SIEGEWRIGHT_TEXT}",
                IsProfSiegewrightAvailable,
                OnProfSiegewrightSelected,
                false, 12);

            starter.AddGameMenuOption("enlisted_duty_selection", "prof_drillmaster",
                "{PROF_DRILLMASTER_TEXT}",
                IsProfDrillmasterAvailable,
                OnProfDrillmasterSelected,
                false, 13);

            starter.AddGameMenuOption("enlisted_duty_selection", "prof_saboteur",
                "{PROF_SABOTEUR_TEXT}",
                IsProfSaboteurAvailable,
                OnProfSaboteurSelected,
                false, 14);
        }

        /// <summary>
        ///     Initialize enlisted status menu with current service information.
        /// </summary>
        private void OnEnlistedStatusInit(MenuCallbackArgs args)
        {
            try
            {
                // Time state is captured by calling code BEFORE menu activation
                // (not here - vanilla has already set Stop by the time init runs)

                // 1.3.4+: Set proper menu background to avoid assertion failure
                // Use the lord's kingdom culture background, or fallback to generic encounter mesh
                var enlistment = EnlistmentBehavior.Instance;
                var backgroundMesh = "encounter_looter"; // Safe fallback

                if (enlistment?.CurrentLord?.Clan?.Kingdom?.Culture != null)
                {
                    backgroundMesh = enlistment.CurrentLord.Clan.Kingdom.Culture.EncounterBackgroundMesh;
                }
                else if (enlistment?.CurrentLord?.Culture != null)
                {
                    backgroundMesh = enlistment.CurrentLord.Culture.EncounterBackgroundMesh;
                }

                args.MenuContext.SetBackgroundMeshName(backgroundMesh);

                // Start wait to enable time controls for the wait menu
                args.MenuContext.GameMenu.StartWait();

                // Set time control mode to allow unpausing (from BLUEPRINT)
                Campaign.Current.SetTimeControlModeLock(false);
                Campaign.Current.TimeControlMode = CampaignTimeControlMode.StoppablePlay;

                RefreshEnlistedStatusDisplay(args);
                _menuNeedsRefresh = true;
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", $"Error initializing enlisted status menu: {ex.Message}");
            }
        }

        /// <summary>
        ///     Refreshes the enlisted status display with current military service information.
        ///     Updates all dynamic text variables used in the menu display, including party leader,
        ///     enlistment details, tier, formation, wages, and XP progression.
        ///     Formats information as "Label : Value" pairs displayed line by line in the menu.
        /// </summary>
        private void RefreshEnlistedStatusDisplay(MenuCallbackArgs args = null)
        {
            try
            {
                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment?.IsEnlisted != true)
                {
                    MBTextManager.SetTextVariable("ENLISTED_STATUS_TEXT",
                        new TextObject("{=Enlisted_Status_NotEnlisted}You are not currently enlisted."));
                    return;
                }

                var lord = enlistment?.CurrentLord;
                var duties = EnlistedDutiesBehavior.Instance;

                if (lord == null)
                {
                    MBTextManager.SetTextVariable("ENLISTED_STATUS_TEXT",
                        new TextObject("{=Enlisted_Status_ErrorNoLord}Error: No enlisted lord found."));
                    return;
                }

                // Build the status content string that will be displayed in the menu
                // Each line follows the format "Label : Value" for consistent presentation
                var statusContent = "";

                try
                {
                    // Construct status display line by line with label:value pairs
                    // Each line contains a descriptive label followed by a colon and the current value

                    // Party objective and enlistment details
                    var objective = GetCurrentObjectiveDisplay(lord);
                    var objectiveLine = new TextObject("{=Enlisted_Objective_Label}Party Objective : {OBJECTIVE}");
                    objectiveLine.SetTextVariable("OBJECTIVE", objective);
                    statusContent += objectiveLine + "\n";

                    // Enlistment time and tier information
                    var enlistmentTime = GetEnlistmentTimeDisplay(enlistment);
                    var timeLine = new TextObject("{=Enlisted_Status_TermRemaining}Term Remaining : {TIME}");
                    timeLine.SetTextVariable("TIME", enlistmentTime);
                    statusContent += timeLine + "\n";

                    // Enlistment tier and formation
                    var tierLine = new TextObject("{=Enlisted_Status_Tier}Enlistment Tier : {TIER}");
                    tierLine.SetTextVariable("TIER", enlistment.EnlistmentTier);
                    statusContent += tierLine + "\n";

                    // Formation display
                    var formationName = "Infantry"; // Default
                    try
                    {
                        if (duties?.IsInitialized == true)
                        {
                            var playerFormation = duties.GetPlayerFormationType();
                            formationName = playerFormation?.ToTitleCase() ?? "Infantry";
                        }
                    }
                    catch
                    {
                        /* Use default */
                    }

                    var formationLine = new TextObject("{=Enlisted_Status_Formation}Formation : {FORMATION}");
                    formationLine.SetTextVariable("FORMATION", formationName);
                    statusContent += formationLine + "\n";

                    // Wage and experience information
                    var dailyWage = CalculateCurrentDailyWage();
                    var wageLine = new TextObject("{=Enlisted_Status_Wage}Wage : {WAGE}");
                    wageLine.SetTextVariable("WAGE", dailyWage);
                    statusContent += wageLine + "<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">\n";

                    // Experience tracking
                    var xpLine = new TextObject("{=Enlisted_Status_XP}Current Experience : {XP}");
                    xpLine.SetTextVariable("XP", enlistment.EnlistmentXP);
                    statusContent += xpLine + "\n";

                    // Next tier experience requirement
                    if (enlistment.EnlistmentTier < 6)
                    {
                        // GetNextTierXpRequirement returns XP needed to promote FROM current tier
                        // So pass current tier, not tier + 1
                        var nextTierXp = GetNextTierXpRequirement(enlistment.EnlistmentTier);
                        var nextXpLine = new TextObject("{=Enlisted_Status_NextXP}Next Level Experience : {XP}");
                        nextXpLine.SetTextVariable("XP", nextTierXp);
                        statusContent += nextXpLine + "\n";
                    }

                    // Formation training description (explains daily skill development)
                    var formationDesc = GetFormationTrainingDescription();
                    statusContent += formationDesc;
                }
                catch
                {
                    // Fallback to simple display on any error (lord guaranteed non-null from earlier check)
                    statusContent = $"Lord : {lord.Name?.ToString() ?? "Unknown"}\n";
                    statusContent += $"Enlistment Tier : {enlistment.EnlistmentTier}\n";
                    statusContent += $"Current Experience : {enlistment.EnlistmentXP}";
                }

                // Set text variables for menu display (lord guaranteed non-null from earlier check)
                var lordName = lord.EncyclopediaLinkWithName?.ToString() ?? lord.Name?.ToString() ?? "Unknown";

                // Get menu context and set text variables
                var menuContext = args?.MenuContext ?? Campaign.Current.CurrentMenuContext;
                if (menuContext != null)
                {
                    var text = menuContext.GameMenu.GetText();
                    text.SetTextVariable("PARTY_LEADER", lordName);
                    text.SetTextVariable("PARTY_TEXT", statusContent);
                }
                else
                {
                    // Fallback for compatibility
                    MBTextManager.SetTextVariable("PARTY_LEADER", lordName);
                    MBTextManager.SetTextVariable("PARTY_TEXT", statusContent);
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", "Error refreshing enlisted status", ex);

                // Error fallback
                var menuContext = args?.MenuContext ?? Campaign.Current.CurrentMenuContext;
                if (menuContext != null)
                {
                    var text = menuContext.GameMenu.GetText();
                    text.SetTextVariable("PARTY_LEADER", "Error");
                    text.SetTextVariable("PARTY_TEXT", "Status information unavailable.");
                }
            }
        }

        // Note: Removed unused utility methods: CalculateServiceDays, GetRankName, GetFormationDisplayInfo, 
        // GetServiceDays, GetRetirementCountdown - kept for reference in git history

        /// <summary>
        ///     Get next tier XP requirement from progression_config.json.
        /// </summary>
        private int GetNextTierXpRequirement(int currentTier)
        {
            // Load from progression_config.json instead of hardcoded values
            return ConfigurationManager.GetXpRequiredForTier(currentTier);
        }

        /// <summary>
        ///     Gets the enlistment time display showing remaining days in the current term.
        ///     Displays countdown for first term or renewal terms.
        /// </summary>
        private string GetEnlistmentTimeDisplay(EnlistmentBehavior enlistment)
        {
            try
            {
                var remainingDays = 0;
                var faction = enlistment.CurrentLord?.MapFaction;
                var isRenewal = false;

                if (faction != null)
                {
                    var record = enlistment.GetFactionVeteranRecord(faction);
                    if (record is { IsInRenewalTerm: true } && record.CurrentTermEnd != CampaignTime.Zero)
                    {
                        isRenewal = true;
                        remainingDays = (int)(record.CurrentTermEnd - CampaignTime.Now).ToDays;
                    }
                }

                if (!isRenewal)
                {
                    // First term calculation
                    var retirementConfig = ConfigurationManager.LoadRetirementConfig();
                    var termEnd = enlistment.EnlistmentDate + CampaignTime.Days(retirementConfig.FirstTermDays);
                    remainingDays = (int)(termEnd - CampaignTime.Now).ToDays;
                }

                if (remainingDays <= 0)
                {
                    return "Term Expired";
                }

                return $"{remainingDays} Days";
            }
            catch
            {
                return "Unknown";
            }
        }

        // Note: Removed unused GetWageDisplay and CalculateBaseDailyWage methods

        /// <summary>
        ///     Gets the formation training description explaining daily skill development.
        ///     Returns a description of what the player does during training based on their formation type.
        /// </summary>
        private string GetFormationTrainingDescription()
        {
            try
            {
                var duties = EnlistedDutiesBehavior.Instance;

                if (duties?.IsInitialized != true)
                {
                    return "You perform basic military duties and training.";
                }

                // Get player's formation and build dynamic description with highlighted skills
                var playerFormation = duties.GetPlayerFormationType();
                return BuildFormationDescriptionWithHighlights(playerFormation, duties);
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", "Error getting formation training description", ex);
                return "You perform basic military duties and training.";
            }
        }

        /// <summary>
        ///     Get formation description with manually highlighted skills and XP amounts.
        /// </summary>
        private string BuildFormationDescriptionWithHighlights(string formation, EnlistedDutiesBehavior _)
        {
            switch (formation.ToLower())
            {
                case "infantry":
                    return
                        "As an Infantryman, you march in formation, drill the shieldwall, and spar in camp, becoming stronger through Athletics, deadly with One-Handed and Two-Handed blades, disciplined with the Polearm, and practiced in Throwing weapons.";

                case "cavalry":
                    return
                        "Serving as a Cavalryman, you ride endless drills to master Riding, lower your Polearm for the charge, cut close with One-Handed steel, practice Two-Handed arms for brute force, and keep your Athletics sharp when dismounted.";

                case "horsearcher":
                    return
                        "As a Horse Archer, you train daily at mounted archery, honing Riding to control your horse, perfecting the draw of the Bow, casting Throwing weapons at the gallop, keeping a One-Handed sword at your side, and building Athletics on foot.";

                case "archer":
                    return
                        "As an Archer, you loose countless shafts with Bow and Crossbow, strengthen your stride through Athletics, and sharpen your edge with a One-Handed blade for when the line closes.";

                default:
                    return "You perform basic military duties and training as assigned.";
            }
        }

        /// <summary>
        ///     Calculate current daily wage with bonuses.
        /// </summary>
        private int CalculateCurrentDailyWage()
        {
            var enlistment = EnlistmentBehavior.Instance;
            var duties = EnlistedDutiesBehavior.Instance;

            if (enlistment?.IsEnlisted != true)
            {
                return 0;
            }

            // Base wage calculation (from progression_config.json logic)
            var baseWage = 10 + Hero.MainHero.Level + enlistment.EnlistmentTier * 5 + enlistment.EnlistmentXP / 200;

            // Duty multiplier
            var dutyMultiplier = duties?.GetCurrentWageMultiplier() ?? 1.0f;

            // Army bonus
            var armyBonus = enlistment.CurrentLord?.PartyBelongedTo?.Army != null ? 1.2f : 1.0f;

            var totalWage = (int)(baseWage * dutyMultiplier * armyBonus);
            return Math.Min(totalWage, 150); // Cap at 150 as per realistic economics
        }

        // Note: Removed unused GetOfficerSkillValue and GetArmyStatusDisplay methods

        /// <summary>
        ///     Get current objective display based on lord's activities.
        /// </summary>
        private string GetCurrentObjectiveDisplay(Hero lord)
        {
            var lordParty = lord?.PartyBelongedTo;
            if (lordParty == null)
            {
                return "";
            }

            if (lordParty.Ai.DoNotMakeNewDecisions)
            {
                return new TextObject("{=Enlisted_Objective_DirectOrders}Following direct orders").ToString();
            }

            if (lordParty.IsActive && lordParty.Party.MapEvent != null)
            {
                var text = new TextObject("{=Enlisted_Objective_Battle}Engaged in battle at {LOCATION}");
                text.SetTextVariable("LOCATION",
                    lordParty.Party.MapEvent.MapEventSettlement?.Name ?? new TextObject("{=menu_field_battle}field"));
                return text.ToString();
            }

            if (lordParty.CurrentSettlement != null)
            {
                var settlement = lordParty.CurrentSettlement;
                var text = new TextObject("{=Enlisted_Objective_Stationed}Stationed at {SETTLEMENT}");
                text.SetTextVariable("SETTLEMENT", settlement.Name);
                return text.ToString();
            }

            if (lordParty.Army != null)
            {
                return new TextObject("{=Enlisted_Objective_Army}Army operations").ToString();
            }

            return new TextObject("{=Enlisted_Objective_Patrol}Patrol duties").ToString();
        }

        // Note: Removed unused GetDynamicStatusMessages, CanPromote, and GetOfficerSkillName methods

        /// <summary>
        ///     Refresh current menu with updated information.
        /// </summary>
        private void RefreshCurrentMenu()
        {
            try
            {
                var menuContext = Campaign.Current?.CurrentMenuContext;
                if (menuContext?.GameMenu != null)
                {
                    // Force menu options to re-evaluate their conditions
                    Campaign.Current.GameMenuManager.RefreshMenuOptions(menuContext);
                    _menuNeedsRefresh = false;
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", $"Error refreshing menu: {ex.Message}");
            }
        }

        #region Menu Condition and Action Methods

        private bool OnEnlistedStatusCondition(MenuCallbackArgs args)
        {
            var isEnlisted = EnlistmentBehavior.Instance?.IsEnlisted == true;

            // Don't refresh in condition methods - they're called too frequently during menu rendering
            // Refresh is handled by the tick method with proper timing validation

            return isEnlisted;
        }

        // Menu Option Conditions and Actions

        private bool IsMasterAtArmsAvailable(MenuCallbackArgs args)
        {
            _ = args; // Required by API contract
            return EnlistmentBehavior.Instance?.IsEnlisted == true;
        }

        private void OnMasterAtArmsSelected(MenuCallbackArgs args)
        {
            try
            {
                // Capture current time state - user may have changed it with spacebar since menu opened
                QuartermasterManager.CaptureTimeStateBeforeMenuActivation();

                var manager = TroopSelectionManager.Instance;
                if (manager == null)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("{=menu_master_unavailable}Master at Arms system is temporarily unavailable.")
                            .ToString()));
                    return;
                }

                manager.ShowMasterAtArmsPopup();
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", $"Error opening Master at Arms: {ex.Message}");
            }
        }

        private bool IsQuartermasterAvailable(MenuCallbackArgs args)
        {
            _ = args; // Required by API contract
            return EnlistmentBehavior.Instance?.IsEnlisted == true;
        }

        private void OnQuartermasterSelected(MenuCallbackArgs args)
        {
            try
            {
                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment?.IsEnlisted != true)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject(
                                "{=menu_must_be_enlisted_qm}You must be enlisted to access quartermaster services.")
                            .ToString()));
                    return;
                }

                // Connect to new Quartermaster system
                var quartermasterManager = QuartermasterManager.Instance;
                if (quartermasterManager != null)
                {
                    // Capture time state BEFORE menu activation - the wait menu tick will restore it
                    QuartermasterManager.CaptureTimeStateBeforeMenuActivation();

                    // Show equipment variants for current troop selection
                    // (wait menu tick handler will restore captured time state)
                    GameMenu.ActivateGameMenu("quartermaster_equipment");
                }
                else
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("{=menu_qm_unavailable}Quartermaster services temporarily unavailable.")
                            .ToString()));
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", "Error accessing quartermaster services", ex);
                InformationManager.DisplayMessage(new InformationMessage(
                    new TextObject("{=menu_qm_error}Quartermaster system error. Please report this issue.")
                        .ToString()));
            }
        }


        private bool IsTalkToAvailable(MenuCallbackArgs args)
        {
            _ = args; // Required by API contract
            return EnlistmentBehavior.Instance?.IsEnlisted == true;
        }

        private void OnTalkToSelected(MenuCallbackArgs args)
        {
            try
            {
                // Capture current time state - user may have changed it with spacebar since menu opened
                QuartermasterManager.CaptureTimeStateBeforeMenuActivation();

                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment?.IsEnlisted != true)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("{=menu_must_be_enlisted_lords}You must be enlisted to speak with lords.")
                            .ToString()));
                    return;
                }

                // Find nearby lords for conversation
                var nearbyLords = GetNearbyLordsForConversation();
                if (nearbyLords.Count == 0)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject(
                                "{=menu_no_lords_available}No lords are available for conversation at this location.")
                            .ToString()));
                    return;
                }

                // Show lord selection inquiry
                ShowLordSelectionInquiry(nearbyLords);
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", $"Error in Talk to My Lord: {ex.Message}");
            }
        }

        /// <summary>
        ///     Find nearby lords available for conversation using current TaleWorlds APIs.
        /// </summary>
        private List<Hero> GetNearbyLordsForConversation()
        {
            var nearbyLords = new List<Hero>();
            try
            {
                var mainParty = MobileParty.MainParty;
                if (mainParty == null)
                {
                    return nearbyLords;
                }

                // Check all mobile parties using verified API
                foreach (var party in MobileParty.All)
                {
                    if (party == null || party == mainParty || !party.IsActive)
                    {
                        continue;
                    }

                    // Check if party is close enough for conversation (same position or very close)
                    // 1.3.4 API: Position2D is now GetPosition2D property
                    var distance = mainParty.GetPosition2D.Distance(party.GetPosition2D);
                    if (distance > 2.0f) // Reasonable conversation distance
                    {
                        continue;
                    }

                    var lord = party.LeaderHero;
                    if (lord is { IsLord: true, IsAlive: true, IsPrisoner: false })
                    {
                        nearbyLords.Add(lord);
                    }
                }

                // Always include your enlisted lord if available
                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment?.CurrentLord != null && !nearbyLords.Contains(enlistment.CurrentLord))
                {
                    nearbyLords.Insert(0, enlistment.CurrentLord); // Put enlisted lord first
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", $"Error finding nearby lords: {ex.Message}");
            }

            return nearbyLords;
        }

        /// <summary>
        ///     Show lord selection inquiry with portraits.
        /// </summary>
        private void ShowLordSelectionInquiry(List<Hero> lords)
        {
            try
            {
                var options = new List<InquiryElement>();
                foreach (var lord in lords)
                {
                    var name = lord.Name?.ToString() ?? "Unknown Lord";
                    // 1.3.4 API: ImageIdentifier is now abstract, use CharacterImageIdentifier
                    var portrait = new CharacterImageIdentifier(CharacterCode.CreateFrom(lord.CharacterObject));
                    var description =
                        $"{lord.Clan?.Name?.ToString() ?? "Unknown Clan"}\n{lord.MapFaction?.Name?.ToString() ?? "Unknown Faction"}";

                    options.Add(new InquiryElement(lord, name, portrait, true, description));
                }

                var data = new MultiSelectionInquiryData(
                    titleText: "Select lord to speak with",
                    descriptionText: string.Empty,
                    inquiryElements: options,
                    isExitShown: true,
                    minSelectableOptionCount: 1,
                    maxSelectableOptionCount: 1,
                    affirmativeText: "Talk",
                    negativeText: "Cancel",
                    affirmativeAction: selected =>
                    {
                        try
                        {
                            // Use pattern matching for cleaner type check
                            if (selected?.FirstOrDefault()?.Identifier is Hero chosenLord)
                            {
                                StartConversationWithLord(chosenLord);
                            }
                        }
                        catch (Exception ex)
                        {
                            ModLogger.Error("Interface", $"Error starting lord conversation: {ex.Message}");
                        }
                    },
                    negativeAction: _ =>
                    {
                        // Cancel action - just close popup, don't affect menu or time state
                        // The enlisted_status menu is already active underneath
                    },
                    soundEventPath: string.Empty);

                MBInformationManager.ShowMultiSelectionInquiry(data);
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", $"Error showing lord selection: {ex.Message}");
            }
        }

        /// <summary>
        ///     Start conversation with selected lord using verified TaleWorlds APIs.
        /// </summary>
        private void StartConversationWithLord(Hero lord)
        {
            try
            {
                if (lord?.PartyBelongedTo == null)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("{=menu_lord_not_available}Lord is not available for conversation.")
                            .ToString()));
                    return;
                }

                // Use the same conversation system our dialogs use
                CampaignMapConversation.OpenConversation(
                    new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty),
                    new ConversationCharacterData(lord.CharacterObject, lord.PartyBelongedTo.Party));
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", $"Error opening conversation with {lord?.Name}: {ex.Message}");
                InformationManager.DisplayMessage(new InformationMessage(
                    new TextObject("{=menu_conversation_error}Unable to start conversation. Please try again.")
                        .ToString()));
            }
        }

        /// <summary>
        ///     Check if Visit Settlement option should be available.
        ///     Supports towns and castles, excludes villages.
        /// </summary>
        private bool IsVisitSettlementAvailable(MenuCallbackArgs args)
        {
            _ = args; // Required by API contract
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment?.IsEnlisted != true)
            {
                return false;
            }

            var lord = enlistment.CurrentLord;
            if (lord?.CurrentSettlement == null)
            {
                return false;
            }

            var settlement = lord.CurrentSettlement;

            // Check if lord is in a town or castle AND player is not already inside the settlement
            // Player is "inside" if they have an active settlement encounter or are in a town/castle menu
            var playerInSettlement = MobileParty.MainParty?.CurrentSettlement != null;
            var canVisit = (settlement.IsTown || settlement.IsCastle) && !playerInSettlement;

            if (canVisit)
            {
                // Set dynamic text based on settlement type
                var visitText = settlement.IsTown ? "Visit Town" : "Visit Castle";
                MBTextManager.SetTextVariable("VISIT_SETTLEMENT_TEXT", visitText);
            }

            return canVisit;
        }

        /// <summary>
        ///     Tracks when the lord enters settlements to adjust menu option visibility.
        ///     Used to show/hide certain menu options based on settlement entry state.
        /// </summary>
        private void OnSettlementEnteredForButton(MobileParty party, Settlement settlement, Hero hero)
        {
            try
            {
                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment?.IsEnlisted == true &&
                    party == enlistment.CurrentLord?.PartyBelongedTo &&
                    (settlement.IsTown || settlement.IsCastle))
                {
                    ModLogger.Debug("Interface", $"Lord entered {settlement.Name} - Visit option available");
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", $"Error in settlement entered tracking: {ex.Message}");
            }
        }

        /// <summary>
        ///     Handles the player selecting "Visit Settlement" from the enlisted status menu.
        ///     Creates a synthetic outside encounter to allow settlement exploration for enlisted soldiers.
        ///     This enables players to visit towns and castles while maintaining their enlisted status.
        /// </summary>
        private void OnVisitTownSelected(MenuCallbackArgs args)
        {
            try
            {
                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment?.IsEnlisted != true)
                {
                    return;
                }

                var lord = enlistment.CurrentLord;
                var settlement = lord?.CurrentSettlement;
                if (settlement == null || (!settlement.IsTown && !settlement.IsCastle))
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("{=menu_lord_not_in_settlement}Your lord is not in a town or castle.")
                            .ToString()));
                    return;
                }

                // If we're already on an outside menu, just reshow it.
                var activeId = Campaign.Current.CurrentMenuContext?.GameMenu?.StringId;
                if (activeId == "town_outside" || activeId == "castle_outside")
                {
                    NextFrameDispatcher.RunNextFrame(() => GameMenu.SwitchToMenu(activeId));
                    return;
                }

                // If an encounter exists and it's already for this settlement, just switch.
                if (PlayerEncounter.Current != null &&
                    PlayerEncounter.EncounterSettlement == settlement)
                {
                    NextFrameDispatcher.RunNextFrame(() =>
                        GameMenu.SwitchToMenu(settlement.IsTown ? "town_outside" : "castle_outside"));
                    return;
                }

                // Clear our own enlisted wait menu off the stack (deferred to next frame)
                if (Campaign.Current.CurrentMenuContext?.GameMenu?.StringId.StartsWith("enlisted_") == true)
                {
                    NextFrameDispatcher.RunNextFrame(() => GameMenu.ExitToLast());
                }

                // BATTLE-AWARE: Only finish non-battle encounters to prevent killing vanilla battle menus
                if (PlayerEncounter.Current != null)
                {
                    var enlistedLord3 = EnlistmentBehavior.Instance?.CurrentLord;
                    var lordInBattle3 = enlistedLord3?.PartyBelongedTo?.Party.MapEvent != null;

                    if (lordInBattle3)
                    {
                        ModLogger.Debug("Interface",
                            "Skipped finishing encounter - lord in battle, preserving vanilla battle menu");
                        return; // Don't create settlement encounter during battles!
                    }

                    // Finish current encounter (if safe), then start a new one next frame
                    NextFrameDispatcher.RunNextFrame(() =>
                    {
                        var lordParty3 = EnlistmentBehavior.Instance?.CurrentLord?.PartyBelongedTo;
                        if (!InBattleOrSiege(lordParty3))
                        {
                            PlayerEncounter.Finish();
                            ModLogger.Debug("Interface", "Finished non-battle encounter before settlement access");
                        }
                        else
                        {
                            ModLogger.Debug("Interface",
                                "SKIPPED finishing encounter - lord in battle/siege, preserving vanilla battle menu");
                        }
                    });
                }

                // TEMPORARILY activate the main party so the engine can attach an encounter.
                var needActivate = !MobileParty.MainParty.IsActive;

                // Start a clean outside encounter for the player at the lord's settlement (deferred)
                NextFrameDispatcher.RunNextFrame(() =>
                {
                    if (needActivate)
                    {
                        MobileParty.MainParty.IsActive = true;
                    }

                    EncounterManager.StartSettlementEncounter(MobileParty.MainParty, settlement);
                    _syntheticOutsideEncounter = true;
                });

                // The engine will have pushed the outside menu; show it explicitly for safety (deferred)
                NextFrameDispatcher.RunNextFrame(() =>
                {
                    GameMenu.SwitchToMenu(settlement.IsTown ? "town_outside" : "castle_outside");
                    ModLogger.Info("Interface", $"Started synthetic outside encounter for {settlement.Name}");
                });
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", $"VisitTown failed: {ex}");
                InformationManager.DisplayMessage(new InformationMessage(
                    new TextObject("{=menu_town_interface_error}Couldn't open the town interface.").ToString()));
            }
        }


        private bool IsAskLeaveAvailable(MenuCallbackArgs args)
        {
            _ = args; // Required by API contract
            return EnlistmentBehavior.Instance?.IsEnlisted == true;
        }

        private void OnAskLeaveSelected(MenuCallbackArgs args)
        {
            try
            {
                // Capture current time state - user may have changed it with spacebar since menu opened
                QuartermasterManager.CaptureTimeStateBeforeMenuActivation();

                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment?.IsEnlisted != true)
                {
                    return;
                }

                // Show leave request confirmation
                var titleText = "Request Leave from Commander";
                var descriptionText =
                    "Request temporary leave from military service. You will regain independent movement but forfeit daily wages and duties until you return.";

                var confirmData = new InquiryData(
                    titleText,
                    descriptionText,
                    isAffirmativeOptionShown: true,
                    isNegativeOptionShown: true,
                    affirmativeText: "Request Leave",
                    negativeText: "Cancel",
                    affirmativeAction: () =>
                    {
                        try
                        {
                            RequestTemporaryLeave();
                        }
                        catch (Exception ex)
                        {
                            ModLogger.Error("Interface", $"Error requesting leave: {ex.Message}");
                        }
                    },
                    negativeAction: () =>
                    {
                        // Cancel action - just close popup, don't affect menu or time state
                        // The enlisted_status menu is already active underneath
                    });

                InformationManager.ShowInquiry(confirmData);
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", $"Error in Ask for Leave: {ex.Message}");
            }
        }

        /// <summary>
        ///     Request temporary leave from service using our established EnlistmentBehavior patterns.
        /// </summary>
        private void RequestTemporaryLeave()
        {
            try
            {
                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment?.IsEnlisted != true)
                {
                    return;
                }

                // Use temporary leave instead of permanent discharge
                enlistment?.StartTemporaryLeave();

                var message =
                    new TextObject(
                        "Leave granted. You are temporarily released from service. Speak with your lord when ready to return to duty.");
                InformationManager.DisplayMessage(new InformationMessage(message.ToString()));

                // Exit menu to campaign map (deferred to next frame)
                NextFrameDispatcher.RunNextFrame(() =>
                {
                    if (Campaign.Current.CurrentMenuContext != null)
                    {
                        GameMenu.ExitToLast();
                    }
                });

                ModLogger.Info("Interface", "Temporary leave granted using proper StopEnlist method");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", $"Error granting temporary leave: {ex.Message}");
            }
        }

        #region Desertion Menu

        /// <summary>
        ///     Condition for showing the "Desert Army" menu option.
        ///     Always available when enlisted - desertion is always an option.
        /// </summary>
        private bool IsDesertArmyAvailable(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Escape;
            return EnlistmentBehavior.Instance?.IsEnlisted == true;
        }

        /// <summary>
        ///     Handler for when the player selects "Desert Army" from the menu.
        ///     Opens the desertion confirmation menu with roleplay explanation.
        /// </summary>
        private void OnDesertArmySelected(MenuCallbackArgs args)
        {
            try
            {
                // Capture time state BEFORE menu switch to preserve player's time control
                QuartermasterManager.CaptureTimeStateBeforeMenuActivation();
                GameMenu.SwitchToMenu("enlisted_desert_confirm");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", $"Error opening desertion menu: {ex.Message}");
            }
        }


        /// <summary>
        ///     Creates the desertion confirmation menu with roleplay-appropriate warning text
        ///     and options to proceed with desertion or return to camp.
        /// </summary>
        private void AddDesertionConfirmMenu(CampaignGameStarter starter)
        {
            // Create the desertion confirmation menu with dramatic RP text
            // Omit default parameter values for cleaner code
            starter.AddGameMenu("enlisted_desert_confirm",
                "{DESERT_WARNING_TEXT}",
                OnDesertionConfirmInit);

            // Back to Camp button - returns to enlisted status menu (Leave icon)
            starter.AddGameMenuOption("enlisted_desert_confirm", "desert_back",
                "Return to Camp",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    return true;
                },
                OnDesertionBackSelected,
                false, 0);

            // Continue with Desertion button - executes the desertion (Escape icon - already set)
            starter.AddGameMenuOption("enlisted_desert_confirm", "desert_confirm",
                "Desert the Army (Accept Penalties)",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Escape;
                    return true;
                },
                OnDesertionConfirmed,
                false, 1);
        }

        /// <summary>
        ///     Initializes the desertion confirmation menu with dramatic roleplay text
        ///     explaining the consequences of desertion.
        /// </summary>
        private void OnDesertionConfirmInit(MenuCallbackArgs args)
        {
            try
            {
                var enlistment = EnlistmentBehavior.Instance;
                var lordName = enlistment?.CurrentLord?.Name?.ToString() ?? "your commander";
                var kingdomName = (enlistment?.CurrentLord?.MapFaction as Kingdom)?.Name?.ToString() ?? "the realm";

                var warningText = new StringBuilder();
                warningText.AppendLine("You stand at a crossroads, contemplating an act of betrayal.");
                warningText.AppendLine();
                warningText.AppendLine(
                    $"To desert {lordName}'s service would mark you as an oath-breaker. Word travels fast among the nobility, and your treachery will not go unnoticed.");
                warningText.AppendLine();
                warningText.AppendLine("The consequences of desertion:");
                warningText.AppendLine(
                    "• Your reputation with ALL lords of {KINGDOM} will be severely damaged (-50 relations)");
                warningText.AppendLine("• You will be branded a criminal in {KINGDOM} (+50 crime rating)");
                warningText.AppendLine("• You may keep the equipment on your back");
                warningText.AppendLine("• You will be free to seek service elsewhere... if anyone will have you");
                warningText.AppendLine();
                warningText.AppendLine("Are you certain you wish to abandon your post?");

                var textObject = new TextObject(warningText.ToString());
                textObject.SetTextVariable("KINGDOM", kingdomName);
                MBTextManager.SetTextVariable("DESERT_WARNING_TEXT", textObject);
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", $"Error initializing desertion menu: {ex.Message}");
                MBTextManager.SetTextVariable("DESERT_WARNING_TEXT",
                    "Are you sure you want to desert? This will have serious consequences.");
            }
        }

        /// <summary>
        ///     Handler for returning to camp from the desertion confirmation menu.
        /// </summary>
        private void OnDesertionBackSelected(MenuCallbackArgs args)
        {
            try
            {
                GameMenu.SwitchToMenu("enlisted_status");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", $"Error returning from desertion menu: {ex.Message}");
            }
        }

        /// <summary>
        ///     Handler for confirming desertion. Calls EnlistmentBehavior.DesertArmy()
        ///     and exits to the campaign map.
        /// </summary>
        private void OnDesertionConfirmed(MenuCallbackArgs args)
        {
            try
            {
                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment == null)
                {
                    ModLogger.Error("Interface", "Cannot desert - EnlistmentBehavior not available");
                    return;
                }

                // Execute the desertion
                enlistment.DesertArmy();

                // Exit to campaign map
                NextFrameDispatcher.RunNextFrame(() =>
                {
                    try
                    {
                        if (Campaign.Current?.CurrentMenuContext != null)
                        {
                            GameMenu.ExitToLast();
                        }
                    }
                    catch (Exception ex)
                    {
                        ModLogger.Error("Interface", $"Error exiting menu after desertion: {ex.Message}");
                    }
                });

                ModLogger.Info("Interface", "Desertion confirmed and executed");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", $"Error confirming desertion: {ex.Message}");
            }
        }

        #endregion

        /// <summary>
        ///     Tick handler for real-time menu updates.
        ///     Called every frame while the enlisted status menu is active to update
        ///     dynamic information and handle menu transitions based on game state.
        ///     Includes time delta validation to prevent assertion failures.
        /// </summary>
        private void OnEnlistedStatusTick(MenuCallbackArgs args, CampaignTime dt)
        {
            try
            {
                // If no time state was captured yet (menu opened via native encounter system),
                // capture current time now so we have a baseline for restoration
                if (!QuartermasterManager.CapturedTimeMode.HasValue && Campaign.Current != null)
                {
                    QuartermasterManager.CapturedTimeMode = Campaign.Current.TimeControlMode;
                }

                // Restore player's time state after StartWait() forced fast-forward
                // Uses shared CapturedTimeMode from QuartermasterManager for consistency across all Enlisted menus
                if (QuartermasterManager.CapturedTimeMode.HasValue && Campaign.Current != null)
                {
                    if (Campaign.Current.TimeControlMode == CampaignTimeControlMode.UnstoppableFastForward ||
                        Campaign.Current.TimeControlMode ==
                        CampaignTimeControlMode.UnstoppableFastForwardForPartyWaitTime)
                    {
                        Campaign.Current.TimeControlMode = QuartermasterManager.CapturedTimeMode.Value;
                    }
                }

                // Validate time delta to prevent assertion failures
                // Zero-delta-time updates can cause assertion failures in the rendering system
                if (dt.ToSeconds <= 0)
                {
                    return;
                }

                // Skip all processing if the player is not currently enlisted
                if (!EnlistmentBehavior.Instance?.IsEnlisted == true)
                {
                    GameMenu.ExitToLast();
                    return;
                }

                // Don't do menu transitions during active encounters
                // This prevents assertion failures that can occur during encounter state transitions
                // When an encounter is active (like paying a bandit), let the native system handle menu transitions
                // We should only check GetGenericStateMenu() for battle menus, not regular encounters
                var hasActiveEncounter = PlayerEncounter.Current != null;

                // If an encounter is active, leave transitions to the native system.
                // We simply keep the menu data fresh so the player still sees updated info.
                if (hasActiveEncounter)
                {
                    if (CampaignTime.Now - _lastMenuUpdate > CampaignTime.Seconds((long)_updateIntervalSeconds))
                    {
                        RefreshEnlistedStatusDisplay(args);
                        _lastMenuUpdate = CampaignTime.Now;
                    }

                    return;
                }

                // No encounter in progress - just refresh the display periodically.
                if (CampaignTime.Now - _lastMenuUpdate > CampaignTime.Seconds((long)_updateIntervalSeconds))
                {
                    RefreshEnlistedStatusDisplay(args);
                    _lastMenuUpdate = CampaignTime.Now;
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", $"Error during enlisted status tick: {ex.Message}");
            }
        }


        /// <summary>
        ///     Check if Report for Duty option should be available.
        /// </summary>
        private bool IsReportDutyAvailable(MenuCallbackArgs args)
        {
            _ = args; // Required by API contract
            var enlistment = EnlistmentBehavior.Instance;
            return enlistment?.IsEnlisted == true;
        }

        /// <summary>
        ///     Handle Report for Duty selection - open duty selection menu.
        /// </summary>
        private void OnReportDutySelected(MenuCallbackArgs args)
        {
            try
            {
                // Capture time state BEFORE menu switch to preserve player's time control
                QuartermasterManager.CaptureTimeStateBeforeMenuActivation();
                GameMenu.SwitchToMenu("enlisted_duty_selection");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", $"Error opening Report for Duty: {ex.Message}");
            }
        }

        /// <summary>
        ///     Initializes the duty selection menu when it's opened.
        ///     Starts the wait menu to enable time controls, same as the main enlisted status menu.
        /// </summary>
        private void OnDutySelectionInit(MenuCallbackArgs args)
        {
            try
            {
                // Time state is captured by calling code BEFORE menu activation
                // (not here - vanilla has already set Stop by the time init runs)

                // 1.3.4+: Set proper menu background to avoid assertion failure
                var enlistment = EnlistmentBehavior.Instance;
                var backgroundMesh = "encounter_looter"; // Safe fallback

                if (enlistment?.CurrentLord?.Clan?.Kingdom?.Culture != null)
                {
                    backgroundMesh = enlistment.CurrentLord.Clan.Kingdom.Culture.EncounterBackgroundMesh;
                }
                else if (enlistment?.CurrentLord?.Culture != null)
                {
                    backgroundMesh = enlistment.CurrentLord.Culture.EncounterBackgroundMesh;
                }

                args.MenuContext.SetBackgroundMeshName(backgroundMesh);

                // Start wait to enable time controls for the wait menu
                args.MenuContext.GameMenu.StartWait();

                // Set time control mode to allow unpausing (same as main menu)
                Campaign.Current.SetTimeControlModeLock(false);
                Campaign.Current.TimeControlMode = CampaignTimeControlMode.StoppablePlay;

                // Initialize dynamic menu text on load
                if (enlistment?.IsEnlisted == true)
                {
                    SetDynamicMenuText(enlistment);
                }

                RefreshDutySelectionDisplay(args);
                _menuNeedsRefresh = true;
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", $"Error initializing duty selection menu: {ex.Message}");
            }
        }

        /// <summary>
        ///     Condition check for duty selection menu (same pattern as main menu).
        /// </summary>
        private bool OnDutySelectionCondition(MenuCallbackArgs args)
        {
            var isEnlisted = EnlistmentBehavior.Instance?.IsEnlisted == true;

            // Don't refresh in condition methods - they're called too frequently during menu rendering
            // Refresh is handled by the tick method with proper timing validation

            return isEnlisted;
        }

        /// <summary>
        ///     Tick handler for duty selection menu with proper timing validation.
        /// </summary>
        private void OnDutySelectionTick(MenuCallbackArgs args, CampaignTime dt)
        {
            try
            {
                // If no time state was captured yet (menu opened via native encounter system),
                // capture current time now so we have a baseline for restoration
                if (!QuartermasterManager.CapturedTimeMode.HasValue && Campaign.Current != null)
                {
                    QuartermasterManager.CapturedTimeMode = Campaign.Current.TimeControlMode;
                }

                // Restore player's time state after StartWait() forced fast-forward
                // Uses shared CapturedTimeMode from QuartermasterManager for consistency across all Enlisted menus
                if (QuartermasterManager.CapturedTimeMode.HasValue && Campaign.Current != null)
                {
                    if (Campaign.Current.TimeControlMode == CampaignTimeControlMode.UnstoppableFastForward ||
                        Campaign.Current.TimeControlMode ==
                        CampaignTimeControlMode.UnstoppableFastForwardForPartyWaitTime)
                    {
                        Campaign.Current.TimeControlMode = QuartermasterManager.CapturedTimeMode.Value;
                    }
                }

                // Validate time delta to prevent assertion failures
                // Zero-delta-time updates can cause assertion failures in the rendering system
                if (dt.ToSeconds <= 0)
                {
                    return; // Skip update if invalid time delta
                }

                // Refresh with timing validation (not every tick)
                if (CampaignTime.Now - _lastMenuUpdate > CampaignTime.Seconds((long)_updateIntervalSeconds))
                {
                    RefreshDutySelectionDisplay(args);
                    _lastMenuUpdate = CampaignTime.Now;
                }

                // Auto-exit if not enlisted
                if (!EnlistmentBehavior.Instance?.IsEnlisted == true)
                {
                    GameMenu.ExitToLast();
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", $"Error during duty selection tick: {ex.Message}");
            }
        }

        /// <summary>
        ///     Refresh duty selection display with dynamic checkmarks for current selections.
        /// </summary>
        private void RefreshDutySelectionDisplay(MenuCallbackArgs args = null)
        {
            try
            {
                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment?.IsEnlisted != true)
                {
                    return;
                }

                // Build the duty selection status content string
                // Formats information as "Label : Value" pairs displayed line by line
                var statusContent = "";

                // Current assignments with descriptions (enlistment guaranteed non-null after check above)
                var currentDuty = GetDutyDisplayName(enlistment.SelectedDuty);
                var currentProfession = enlistment.SelectedProfession == "none"
                    ? "None"
                    : GetProfessionDisplayName(enlistment.SelectedProfession);

                statusContent += $"Current Duty : {currentDuty}\n";
                statusContent += $"Current Profession : {currentProfession}\n\n";

                // Add detailed descriptions for current assignments
                var dutyDescription = GetDutyDescription(enlistment.SelectedDuty);
                var professionDescription = GetProfessionDescription(enlistment.SelectedProfession);

                statusContent += $"DUTY ASSIGNMENT: {dutyDescription}\n\n";
                if (enlistment.SelectedProfession != "none")
                {
                    statusContent += $"PROFESSION: {professionDescription}\n\n";
                }

                // Show the selected profession description instead of instructions
                if (enlistment.SelectedProfession == "none")
                {
                    statusContent += "None";
                }
                else
                {
                    statusContent += GetProfessionDescription(enlistment.SelectedProfession);
                }

                // Set dynamic text variables for menu options with correct checkmarks
                SetDynamicMenuText(enlistment);

                // Use same text variable format as main menu
                var menuContext = args?.MenuContext ?? Campaign.Current.CurrentMenuContext;
                if (menuContext != null)
                {
                    var text = menuContext.GameMenu.GetText();
                    text.SetTextVariable("DUTY_STATUS", "Report for Duty");
                    text.SetTextVariable("DUTY_TEXT", statusContent);
                }
                else
                {
                    // Fallback for compatibility
                    MBTextManager.SetTextVariable("DUTY_STATUS", "Report for Duty");
                    MBTextManager.SetTextVariable("DUTY_TEXT", statusContent);
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", "Error refreshing duty selection display", ex);

                // Error fallback
                var menuContext = args?.MenuContext ?? Campaign.Current.CurrentMenuContext;
                if (menuContext != null)
                {
                    var text = menuContext.GameMenu.GetText();
                    text.SetTextVariable("DUTY_STATUS", "Error");
                    text.SetTextVariable("DUTY_TEXT", "Assignment information unavailable.");
                }
            }
        }

        /// <summary>
        ///     Set dynamic text variables for menu options based on current selections.
        /// </summary>
        private void SetDynamicMenuText(EnlistmentBehavior enlistment)
        {
            var selectedDuty = enlistment.SelectedDuty;
            var selectedProfession = enlistment.SelectedProfession;

            // Helper to format option text
            TextObject FormatOption(string id, string nameText, string currentId)
            {
                var name = new TextObject(nameText);
                var symbol = new TextObject(id == currentId
                    ? "{=Enlisted_Symbol_Selected}✓ {NAME}"
                    : "{=Enlisted_Symbol_Unselected}○ {NAME}");
                symbol.SetTextVariable("NAME", name);
                return symbol;
            }

            // DUTY TEXT VARIABLES
            MBTextManager.SetTextVariable("DUTY_ENLISTED_TEXT",
                FormatOption("enlisted", "{=Enlisted_Duty_Name_Enlisted}Enlisted", selectedDuty));

            MBTextManager.SetTextVariable("DUTY_FORAGER_TEXT",
                FormatOption("forager", "{=Enlisted_Duty_Name_Forager}Forager", selectedDuty));

            MBTextManager.SetTextVariable("DUTY_SENTRY_TEXT",
                FormatOption("sentry", "{=Enlisted_Duty_Name_Sentry}Sentry", selectedDuty));

            MBTextManager.SetTextVariable("DUTY_MESSENGER_TEXT",
                FormatOption("messenger", "{=Enlisted_Duty_Name_Messenger}Messenger", selectedDuty));

            MBTextManager.SetTextVariable("DUTY_PIONEER_TEXT",
                FormatOption("pioneer", "{=Enlisted_Duty_Name_Pioneer}Pioneer", selectedDuty));

            // PROFESSION TEXT VARIABLES
            MBTextManager.SetTextVariable("PROF_QUARTERHAND_TEXT",
                FormatOption("quarterhand", "{=Enlisted_Prof_Name_Quarterhand}Quarterhand", selectedProfession));

            MBTextManager.SetTextVariable("PROF_FIELD_MEDIC_TEXT",
                FormatOption("field_medic", "{=Enlisted_Prof_Name_FieldMedic}Field Medic", selectedProfession));

            MBTextManager.SetTextVariable("PROF_SIEGEWRIGHT_TEXT",
                FormatOption("siegewright_aide", "{=Enlisted_Prof_Name_SiegewrightAide}Siegewright's Aide",
                    selectedProfession));

            MBTextManager.SetTextVariable("PROF_DRILLMASTER_TEXT",
                FormatOption("drillmaster", "{=Enlisted_Prof_Name_Drillmaster}Drillmaster", selectedProfession));

            MBTextManager.SetTextVariable("PROF_SABOTEUR_TEXT",
                FormatOption("saboteur", "{=Enlisted_Prof_Name_Saboteur}Saboteur", selectedProfession));
        }

        #region Duty Selection Conditions and Actions

        // Duty availability conditions with localized tooltips explaining bonuses
        private bool IsDutyEnlistedAvailable(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Continue;
            args.Tooltip =
                new TextObject(
                    "{=duty_tooltip_enlisted}Standard military service. Train with your formation and earn base wages.");
            return true;
        }

        private bool IsDutyForagerAvailable(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Trade;
            args.Tooltip =
                new TextObject(
                    "{=duty_tooltip_forager}Gather food and supplies for the army. Earn bonus XP and improved wages.");
            return true;
        }

        private bool IsDutySentryAvailable(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.DefendAction;
            args.Tooltip =
                new TextObject(
                    "{=duty_tooltip_sentry}Guard the camp perimeter. Improved detection of enemies and bonus relation with your lord.");
            return true;
        }

        private bool IsDutyMessengerAvailable(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Mission;
            args.Tooltip =
                new TextObject(
                    "{=duty_tooltip_messenger}Deliver messages and scout ahead. Bonus riding/athletics XP and faster travel.");
            return true;
        }

        private bool IsDutyPioneerAvailable(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.SiegeAmbush;
            args.Tooltip =
                new TextObject(
                    "{=duty_tooltip_pioneer}Build fortifications and siege works. Bonus engineering XP and siege effectiveness.");
            return true;
        }

        // PROFESSION CONDITIONS with tier requirements shown in localized tooltips
        private const int ProfessionTierRequirement = 3;

        private bool IsProfQuarterhandAvailable(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Trade;
            var tier = EnlistmentBehavior.Instance?.EnlistmentTier ?? 1;
            if (tier < ProfessionTierRequirement)
            {
                args.IsEnabled = false;
                args.Tooltip = new TextObject("{=prof_tooltip_tier_locked}Requires Tier 3 to unlock this profession.");
            }
            else
            {
                args.Tooltip =
                    new TextObject(
                        "{=prof_tooltip_quarterhand}Manage supplies and inventory. 15% better trade prices and +50 carry capacity.");
            }

            return true;
        }

        private bool IsProfFieldMedicAvailable(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Manage;
            var tier = EnlistmentBehavior.Instance?.EnlistmentTier ?? 1;
            if (tier < ProfessionTierRequirement)
            {
                args.IsEnabled = false;
                args.Tooltip = new TextObject("{=prof_tooltip_tier_locked}Requires Tier 3 to unlock this profession.");
            }
            else
            {
                args.Tooltip =
                    new TextObject(
                        "{=prof_tooltip_medic}Tend to wounded soldiers. Faster healing, bonus medicine XP, and morale boost.");
            }

            return true;
        }

        private bool IsProfSiegewrightAvailable(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.SiegeAmbush;
            var tier = EnlistmentBehavior.Instance?.EnlistmentTier ?? 1;
            if (tier < ProfessionTierRequirement)
            {
                args.IsEnabled = false;
                args.Tooltip = new TextObject("{=prof_tooltip_tier_locked}Requires Tier 3 to unlock this profession.");
            }
            else
            {
                args.Tooltip =
                    new TextObject(
                        "{=prof_tooltip_siege}Assist siege engineers. Faster siege construction and bonus engineering XP.");
            }

            return true;
        }

        private bool IsProfDrillmasterAvailable(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.OrderTroopsToAttack;
            var tier = EnlistmentBehavior.Instance?.EnlistmentTier ?? 1;
            if (tier < ProfessionTierRequirement)
            {
                args.IsEnabled = false;
                args.Tooltip = new TextObject("{=prof_tooltip_tier_locked}Requires Tier 3 to unlock this profession.");
            }
            else
            {
                args.Tooltip =
                    new TextObject(
                        "{=prof_tooltip_drill}Train soldiers in formation fighting. Bonus leadership XP and troop morale.");
            }

            return true;
        }

        private bool IsProfSaboteurAvailable(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Raid;
            var tier = EnlistmentBehavior.Instance?.EnlistmentTier ?? 1;
            if (tier < ProfessionTierRequirement)
            {
                args.IsEnabled = false;
                args.Tooltip = new TextObject("{=prof_tooltip_tier_locked}Requires Tier 3 to unlock this profession.");
            }
            else
            {
                args.Tooltip =
                    new TextObject(
                        "{=prof_tooltip_saboteur}Conduct covert operations. Bonus roguery XP and special mission access.");
            }

            return true;
        }

        // DUTY ACTIONS
        private void OnDutyEnlistedSelected(MenuCallbackArgs args)
        {
            SelectDuty("enlisted", "{=Enlisted_Duty_Name_Enlisted}Enlisted");
        }

        private void OnDutyForagerSelected(MenuCallbackArgs args)
        {
            SelectDuty("forager", "{=Enlisted_Duty_Name_Forager}Forager");
        }

        private void OnDutySentrySelected(MenuCallbackArgs args)
        {
            SelectDuty("sentry", "{=Enlisted_Duty_Name_Sentry}Sentry");
        }

        private void OnDutyMessengerSelected(MenuCallbackArgs args)
        {
            SelectDuty("messenger", "{=Enlisted_Duty_Name_Messenger}Messenger");
        }

        private void OnDutyPioneerSelected(MenuCallbackArgs args)
        {
            SelectDuty("pioneer", "{=Enlisted_Duty_Name_Pioneer}Pioneer");
        }

        // PROFESSION ACTIONS (with tier checking)

        private void OnProfQuarterhandSelected(MenuCallbackArgs args)
        {
            SelectProfessionWithTierCheck("quarterhand", "{=Enlisted_Prof_Name_Quarterhand}Quarterhand");
        }

        private void OnProfFieldMedicSelected(MenuCallbackArgs args)
        {
            SelectProfessionWithTierCheck("field_medic", "{=Enlisted_Prof_Name_FieldMedic}Field Medic");
        }

        private void OnProfSiegewrightSelected(MenuCallbackArgs args)
        {
            SelectProfessionWithTierCheck("siegewright_aide",
                "{=Enlisted_Prof_Name_SiegewrightAide}Siegewright's Aide");
        }

        private void OnProfDrillmasterSelected(MenuCallbackArgs args)
        {
            SelectProfessionWithTierCheck("drillmaster", "{=Enlisted_Prof_Name_Drillmaster}Drillmaster");
        }

        private void OnProfSaboteurSelected(MenuCallbackArgs args)
        {
            SelectProfessionWithTierCheck("saboteur", "{=Enlisted_Prof_Name_Saboteur}Saboteur");
        }

        private void OnDutyBackSelected(MenuCallbackArgs args)
        {
            // Capture time state BEFORE menu switch to preserve player's time control
            QuartermasterManager.CaptureTimeStateBeforeMenuActivation();
            GameMenu.SwitchToMenu("enlisted_status");
        }

        #endregion

        #region Duty Selection Helper Methods

        /// <summary>
        ///     Select a new duty and show confirmation.
        /// </summary>
        private void SelectDuty(string dutyId, string dutyName)
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment?.IsEnlisted == true)
            {
                enlistment.SetSelectedDuty(dutyId);

                var message =
                    new TextObject(
                        "{=Enlisted_Message_DutyChanged}Duty changed to {DUTY}. Your new daily skill training has begun.");
                message.SetTextVariable("DUTY", new TextObject(dutyName));
                InformationManager.DisplayMessage(new InformationMessage(message.ToString()));

                GameMenu.SwitchToMenu("enlisted_duty_selection"); // Refresh menu
            }
        }

        /// <summary>
        ///     Select a new profession and show confirmation.
        /// </summary>
        private void SelectProfession(string professionId, string professionName)
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment?.IsEnlisted == true)
            {
                enlistment.SetSelectedProfession(professionId);

                var message =
                    new TextObject(
                        "{=Enlisted_Message_ProfessionChanged}Profession changed to {PROFESSION}. Your specialized training has begun.");
                message.SetTextVariable("PROFESSION", new TextObject(professionName));
                InformationManager.DisplayMessage(new InformationMessage(message.ToString()));

                GameMenu.SwitchToMenu("enlisted_duty_selection"); // Refresh menu
            }
        }

        /// <summary>
        ///     Select profession with tier requirement check.
        /// </summary>
        private void SelectProfessionWithTierCheck(string professionId, string professionName)
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment?.IsEnlisted != true)
            {
                return;
            }

            // Check tier requirement
            if (enlistment.EnlistmentTier < 3)
            {
                var message =
                    new TextObject(
                        "{=Enlisted_Message_Tier3Required}You must reach Tier 3 before selecting professions. Continue your service to unlock specialized roles.");
                InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
                return;
            }

            // Tier 3+, allow selection
            SelectProfession(professionId, professionName);
        }

        /// <summary>
        ///     Get display name for duty ID.
        /// </summary>
        private string GetDutyDisplayName(string dutyId)
        {
            return dutyId switch
            {
                "enlisted" => new TextObject("{=Enlisted_Duty_Name_Enlisted}Enlisted").ToString(),
                "forager" => new TextObject("{=Enlisted_Duty_Name_Forager}Forager").ToString(),
                "sentry" => new TextObject("{=Enlisted_Duty_Name_Sentry}Sentry").ToString(),
                "messenger" => new TextObject("{=Enlisted_Duty_Name_Messenger}Messenger").ToString(),
                "pioneer" => new TextObject("{=Enlisted_Duty_Name_Pioneer}Pioneer").ToString(),
                _ => "Unknown"
            };
        }

        /// <summary>
        ///     Get detailed description for duty ID.
        /// </summary>
        private string GetDutyDescription(string dutyId)
        {
            return dutyId switch
            {
                "enlisted" => new TextObject(
                        "{=Enlisted_Duty_Desc_Enlisted}You handle the everyday soldier work: picket shifts, camp chores, hauling, drill, short patrols. (+4 XP for non-formation skills)")
                    .ToString(),
                "forager" => new TextObject(
                        "{=Enlisted_Duty_Desc_Forager}Work nearby farms/hamlets to keep rations coming—barter, levy, or quietly procure supplies. (Skills: Charm, Roguery, Trade)")
                    .ToString(),
                "sentry" => new TextObject(
                        "{=Enlisted_Duty_Desc_Sentry}Man the picket posts, patrol around the entrenchments and palisade, and call the alarm early. (Skills: Scouting, Tactics)")
                    .ToString(),
                "messenger" => new TextObject(
                        "{=Enlisted_Duty_Desc_Messenger}Run dispatches between the command tent, outposts, and allied banners; get through checkpoints and return with written replies. (Skills: Scouting, Charm, Trade)")
                    .ToString(),
                "pioneer" => new TextObject(
                        "{=Enlisted_Duty_Desc_Pioneer}Cut timber and dig; drain around tents, shore up breastworks, lay corduroy over mud, and keep tools and wagons serviceable. (Skills: Engineering, Steward, Smithing)")
                    .ToString(),
                _ => "Military service duties."
            };
        }

        /// <summary>
        ///     Get display name for profession ID.
        /// </summary>
        private string GetProfessionDisplayName(string professionId)
        {
            return professionId switch
            {
                "none" => "None", // Default but invisible in menu
                "quarterhand" => new TextObject("{=Enlisted_Prof_Name_Quarterhand}Quarterhand").ToString(),
                "field_medic" => new TextObject("{=Enlisted_Prof_Name_FieldMedic}Field Medic").ToString(),
                "siegewright_aide" => new TextObject("{=Enlisted_Prof_Name_SiegewrightAide}Siegewright's Aide")
                    .ToString(),
                "drillmaster" => new TextObject("{=Enlisted_Prof_Name_Drillmaster}Drillmaster").ToString(),
                "saboteur" => new TextObject("{=Enlisted_Prof_Name_Saboteur}Saboteur").ToString(),
                _ => "Unknown"
            };
        }

        /// <summary>
        ///     Get detailed description for profession ID.
        /// </summary>
        private string GetProfessionDescription(string professionId)
        {
            return professionId switch
            {
                "none" => new TextObject("{=Enlisted_Prof_Desc_None}No specialized profession assigned.").ToString(),
                "quarterhand" => new TextObject(
                        "{=Enlisted_Prof_Desc_Quarterhand}Post billet lists, route carts around trenches, book barns/inns, and settle accounts. (Skills: Steward, Trade)")
                    .ToString(),
                "field_medic" => new TextObject(
                        "{=Enlisted_Prof_Desc_FieldMedic}Run the aid tent by the stockade; clean and dress wounds, set bones, and keep salves stocked. (Skill: Medicine)")
                    .ToString(),
                "siegewright_aide" => new TextObject(
                        "{=Enlisted_Prof_Desc_Siegewright}Work the siege park; shape beams, lash ladders and gabions, and patch engines between bombardments. (Skills: Engineering, Smithing)")
                    .ToString(),
                "drillmaster" => new TextObject(
                        "{=Enlisted_Prof_Desc_Drillmaster}Run morning drill on the parade ground; dress ranks, time volleys, rehearse signals, and sharpen maneuvers. (Skills: Leadership, Tactics)")
                    .ToString(),
                "saboteur" => new TextObject(
                        "{=Enlisted_Prof_Desc_Saboteur}Specialized reconnaissance and sabotage operations behind enemy lines. (Skills: Roguery, Engineering, Smithing)")
                    .ToString(),
                _ => new TextObject("{=Enlisted_Prof_Desc_Unknown}Specialized military profession.").ToString()
            };
        }

        #endregion

        // Note: Removed unused Military Styling Helper Methods region (GetFormationSymbol, GetProgressBar)
    }

    /// <summary>
    ///     Extension methods for string formatting.
    /// </summary>
    public static class StringExtensions
    {
        public static string ToTitleCase(this string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            return char.ToUpper(input[0]) + input.Substring(1).ToLower();
        }

        #endregion
    }
}
