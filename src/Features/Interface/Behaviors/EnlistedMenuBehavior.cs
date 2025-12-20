using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enlisted.Features.Assignments.Behaviors;
using Enlisted.Features.Assignments.Core;
// Removed: using Enlisted.Features.Camp.UI.Bulletin; (old Bulletin UI deleted)
using Enlisted.Features.Conditions;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Equipment.Behaviors;
using Enlisted.Features.Escalation;
using Enlisted.Features.Lances.Events;
using Enlisted.Features.Lances.Events.Decisions;
using Enlisted.Features.Lances.UI;
using Enlisted.Features.Schedule.Models;
using Enlisted.Mod.Core;
using Enlisted.Mod.Core.Logging;
using Enlisted.Mod.Core.Triggers;
using Enlisted.Mod.Entry;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.Localization;
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
        private const string CampHubMenuId = "enlisted_camp_hub";
        private const string LeaveServiceMenuId = "enlisted_leave_service";

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

        /// <summary>
        ///     Last time we logged an enlisted menu activation, used to avoid log spam
        ///     when activation is retried in quick succession.
        /// </summary>
        private static CampaignTime _lastEnlistedMenuActivationLogTime = CampaignTime.Zero;

        private const float EnlistedMenuActivationLogCooldownSeconds = 1.0f;

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
                            ModLogger.ErrorCode("Interface", "E-UI-001", "Error handling battle menu transition", ex);
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
            
            // Settlement encounters are OK - they happen when armies enter towns/castles
            // We only want to block battle/siege encounters, not peaceful settlement visits
            var isSettlementEncounter = playerEncounter &&
                PlayerEncounter.EncounterSettlement != null &&
                !playerBattle;
            
            // If it's a settlement encounter (not a battle), allow menu activation
            if (isSettlementEncounter && !lordSiegeEvent && !siegeRelatedBattle)
            {
                ModLogger.Debug("Interface", "Allowing menu activation - settlement encounter (not battle)");
                return true;
            }

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
                ModLogger.ErrorCode("Interface", "E-UI-002", "Error in siege battle detection", ex);
                return false;
            }
        }

        private void OnDebugToolsSelected(MenuCallbackArgs args)
        {
            try
            {
                var options = new List<InquiryElement>
                {
                    new InquiryElement(
                        "gold",
                        new TextObject("{=Enlisted_Debug_Gold}Give 1000 Gold").ToString(),
                        null),
                    new InquiryElement(
                        "xp",
                        new TextObject("{=Enlisted_Debug_XP}Give XP to Rank Up").ToString(),
                        null),
                    new InquiryElement(
                        "test_event",
                        new TextObject("{=Enlisted_Debug_TestEvent}Test Onboarding Screen").ToString(),
                        null)
                };

                var inquiry = new MultiSelectionInquiryData(
                    new TextObject("{=Enlisted_Debug_Title}Debug Tools").ToString(),
                    new TextObject("{=Enlisted_Debug_Body}Select a debug action:").ToString(),
                    options,
                    true,
                    1,
                    1,
                    new TextObject("{=str_ok}OK").ToString(),
                    new TextObject("{=str_cancel}Cancel").ToString(),
                    selectedElements =>
                    {
                        if (selectedElements != null && selectedElements.Count > 0)
                        {
                            var selected = selectedElements[0].Identifier as string;
                            switch (selected)
                            {
                                case "gold":
                                    Debugging.Behaviors.DebugToolsBehavior.GiveGold();
                                    break;
                                case "xp":
                                    Debugging.Behaviors.DebugToolsBehavior.GiveEnlistmentXp();
                                    break;
                                case "test_event":
                                    Debugging.Behaviors.DebugToolsBehavior.TestOnboardingScreen();
                                    break;
                            }
                        }
                    },
                    null);

                MBInformationManager.ShowMultiSelectionInquiry(inquiry);
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("Interface", "E-UI-003", "Error opening debug tools", ex);
            }
        }

        private bool IsBaggageTrainAvailable(MenuCallbackArgs args)
        {
            var enlistment = EnlistmentBehavior.Instance;
            return enlistment?.IsEnlisted == true;
        }

        private void OnBaggageTrainSelected(MenuCallbackArgs args)
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null || !enlistment.IsEnlisted)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    new TextObject("{=qm_baggage_unavailable}You must be enlisted to access the baggage train.").ToString(),
                    Colors.Red));
                return;
            }

            if (!enlistment.TryOpenBaggageTrain())
            {
                // failure handled internally (fatigue or other checks)
                return;
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

                var now = CampaignTime.Now;
                if (now - _lastEnlistedMenuActivationLogTime >
                    CampaignTime.Seconds((long)EnlistedMenuActivationLogCooldownSeconds))
                {
                    ModLogger.Info("Menu", "Activating enlisted status menu");
                    _lastEnlistedMenuActivationLogTime = now;
                }

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
        /// Menu background initialization for enlisted_status menu.
        /// Sets culture-appropriate background and ambient audio for modern feel.
        /// Leaves time control untouched so player retains current pause/speed.
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
        }
        
        /// <summary>
        /// Menu background initialization for enlisted_decisions menu.
        /// Uses the same culture-appropriate background as other enlisted menus.
        /// </summary>
        [GameMenuInitializationHandler("enlisted_decisions")]
        private static void OnDecisionsBackgroundInit(MenuCallbackArgs args)
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
        }
        
        /// <summary>
        /// Menu background initialization for enlisted_desert_confirm menu.
        /// Sets ominous background for desertion confirmation.
        /// Leaves time control untouched so player retains current pause/speed.
        /// </summary>
        [GameMenuInitializationHandler("enlisted_desert_confirm")]
        private static void OnDesertConfirmBackgroundInit(MenuCallbackArgs args)
        {
            // Desertion confirmation should feel ominous/decisive, not "normal encounter".
            // Use a known base-game mesh intended for negative outcomes.
            args.MenuContext.SetBackgroundMeshName("encounter_lose");
            args.MenuContext.SetAmbientSound("event:/map/ambient/node/settlements/2d/camp_army");
            // Time control left untouched - respects player's current pause/speed setting
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
                    ModLogger.ErrorCode("Interface", "E-UI-004", "Deferred enlisted menu activation error", ex);
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

                // FALLBACK: Override army_wait and army_wait_at_settlement menus if they somehow appear
                // NOTE: GenericStateMenuPatch now prevents these menus from appearing in the first place
                // when enlisted, but this serves as a defensive fallback in case something bypasses the patch.
                // These are native army menus that would appear when the lord is at settlements or during army operations.
                // Enlisted soldiers should see their custom menu instead, unless in combat/siege.
                
                if (_currentMenuId == "army_wait_at_settlement" || _currentMenuId == "army_wait")
                {
                    // Only override if not in siege or siege-related battle
                    if (!lordSiegeEvent && !siegeRelatedBattle)
                    {
                        // For army_wait, also check that player isn't in a battle/encounter
                        if (_currentMenuId == "army_wait" && (playerBattle || playerEncounter))
                        {
                            // Don't override during battles
                            return;
                        }
                        
                        ModLogger.Warn("Menu", $"FALLBACK: Overriding {_currentMenuId} to enlisted menu (patch may have been bypassed)");
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
            RegisterCampHubMenu(starter);

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
                ModLogger.ErrorCode("Interface", "E-UI-005", "Failed to add emergency siege battle option", ex);
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
                ModLogger.ErrorCode("Interface", "E-UI-006", "Failed to add Return to camp options", ex);
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
                ModLogger.ErrorCode("Interface", "E-UI-007", "Error returning to camp", ex);
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
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("{=enlisted_joining_siege_assault}Joining siege assault...").ToString()));
                }
                // Check if the lord is in a siege-related battle (like sally-outs)
                else if (lordParty?.Party.MapEvent != null)
                {
                    ModLogger.Info("Interface", "Lord is in siege-related battle - attempting to join");
                    // Battle participation is handled by the native game system
                    // The player's army membership automatically includes them in the battle
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("{=enlisted_joining_siege_battle}Joining siege battle...").ToString()));
                }
                else
                {
                    ModLogger.Info("Interface", "No siege situation detected - option should not have been available");
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("{=enlisted_no_siege_battle}No siege battle available.").ToString()));
                }
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("Interface", "E-UI-008", "Error in emergency siege battle", ex);
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
                "{=Enlisted_Menu_Status_Title}Lord: {PARTY_LEADER}\n{PARTY_TEXT}",
                OnEnlistedStatusInit,
                OnEnlistedStatusCondition,
                null, // No consequence for wait menu
                OnEnlistedStatusTick, // Tick handler for real-time updates
                GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption); // Wait menu template that hides progress boxes

            // Main menu options for enlisted status menu with modern icons and localized tooltips

            // Debug tools (QA only): grant gold/XP
            starter.AddGameMenuOption("enlisted_status", "enlisted_debug_tools",
                "{=Enlisted_Menu_DebugTools}Debug Tools",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                    args.Tooltip = new TextObject("{=menu_tooltip_debug}Grant gold or enlistment XP for testing.");
                    return true;
                },
                OnDebugToolsSelected,
                false, 2000);

            // Master at Arms is deprecated. Formation is chosen during the T1→T2 proving event, and equipment is
            // purchased from the Quartermaster based on formation, tier, and culture. We keep the code for save
            // compatibility, but the menu option stays hidden.
#pragma warning disable CS0618 // Intentionally using obsolete method for save compatibility
            starter.AddGameMenuOption("enlisted_status", "enlisted_master_at_arms",
                "{=Enlisted_Menu_MasterAtArms}Master at Arms",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.TroopSelection;
                    args.Tooltip = new TextObject("{=menu_tooltip_master}Select your troop type and equipment loadout based on your current tier.");
                    // Hidden: formation is now chosen via proving event.
                    return false;
                },
                OnMasterAtArmsSelected,
                false, 1);
#pragma warning restore CS0618

            // NOTE: Visit Quartermaster moved to Camp Hub - not needed on main status menu.

            // Camp hub: decisions, camp screen, companions, service records, and leaving-service actions.
            starter.AddGameMenuOption("enlisted_status", "enlisted_camp_hub",
                "{=enlisted_camp}Camp",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                    args.Tooltip = new TextObject("{=enlisted_camp_tooltip}Open the Camp hub (medical tent, reports, and management).");
                    return true;
                },
                _ =>
                {
                    QuartermasterManager.CaptureTimeStateBeforeMenuActivation();
                    GameMenu.SwitchToMenu(CampHubMenuId);
                },
                false, 10);

            // Decisions (react-now layer)
            starter.AddGameMenuOption("enlisted_status", "enlisted_decisions_entry",
                "{=enlisted_decisions_entry}Decisions",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                    var availableCount = DecisionEventBehavior.Instance?.GetAvailablePlayerDecisions()?.Count ?? 0;
                    args.Tooltip = availableCount > 0
                        ? new TextObject("{=enlisted_decisions_tooltip}Review and act on pending decisions.")
                        : new TextObject("{=enlisted_decisions_tooltip_none}No decisions currently available.");
                    return true;
                },
                _ =>
                {
                    QuartermasterManager.CaptureTimeStateBeforeMenuActivation();
                    GameMenu.SwitchToMenu("enlisted_decisions");
                },
                false, 11);

            // NOTE: My Lance and Camp Management have been moved to Camp Hub.
            // These are accessible via Camp → Camp Management.

            // My Lord... - conversation with the current lord (Conversation icon)
            starter.AddGameMenuOption("enlisted_status", "enlisted_talk_to",
                "{=Enlisted_Menu_TalkToLord}My Lord...",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Conversation;
                    var nearbyLords = GetNearbyLordsForConversation();
                    if (nearbyLords.Count == 0)
                    {
                        args.IsEnabled = false;
                        args.Tooltip = new TextObject("{=menu_disabled_no_lords}No lords nearby for conversation.");
                        return true;
                    }
                    args.Tooltip = new TextObject("{=menu_tooltip_talk}Speak with nearby lords for quests, news, and relation building.");
                    return true;
                },
                OnTalkToSelected,
                false, 19);

            // Visit Settlement - towns and castles only (Submenu icon)
            // Hidden when not at a settlement to reduce clutter.
            starter.AddGameMenuOption("enlisted_status", "enlisted_visit_settlement",
                "{VISIT_SETTLEMENT_TEXT}",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                    var enlistment = EnlistmentBehavior.Instance;
                    var lord = enlistment?.CurrentLord;
                    var settlement = lord?.CurrentSettlement;

                    // Hide entirely when not at a town or castle
                    if (settlement == null || (!settlement.IsTown && !settlement.IsCastle))
                    {
                        return false;
                    }

                    args.Tooltip = new TextObject("{=menu_tooltip_visit}Enter the settlement while your lord is present.");
                    var visitText = new TextObject("{=Enlisted_Menu_VisitSettlementNamed}Visit {SETTLEMENT}");
                    visitText.SetTextVariable("SETTLEMENT", settlement.Name);
                    MBTextManager.SetTextVariable("VISIT_SETTLEMENT_TEXT", visitText);
                    return true;
                },
                OnVisitTownSelected,
                false, 18);

            // NOTE: Duties, Medical Attention, and Service Records are all accessed via Camp Hub.
            // Keeping the main menu lean - only essential/frequent actions here.

            // Leave / Discharge / Desert (always shown; eligibility varies)
            starter.AddGameMenuOption("enlisted_status", "enlisted_leave_service_entry",
                "{=enlisted_leave_service_entry}Leave / Discharge / Desert",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    args.Tooltip = new TextObject("{=enlisted_leave_service_tooltip}Leaving actions: request leave, discharge, or desert.");
                    return true;
                },
                _ =>
                {
                    QuartermasterManager.CaptureTimeStateBeforeMenuActivation();
                    GameMenu.SwitchToMenu(LeaveServiceMenuId);
                },
                false, 20);

            // === LEAVE OPTIONS (grouped at bottom) ===

            // No "return to duties" option needed - player IS doing duties by being in this menu

            // Decisions submenu: player-initiated decisions and queued free-time actions.
            RegisterDecisionsMenu(starter);

            // Add desertion confirmation menu
            AddDesertionConfirmMenu(starter);

            // Leave Service submenu (Camp Hub entry point)
            RegisterLeaveServiceMenu(starter);
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
                ModLogger.ErrorCode("Interface", "E-UI-009", "OnSettlementLeftReturnToCamp error", ex);
                // Ensure we don't get stuck in pending state
                _pendingReturnToEnlistedMenu = false;
            }
        }


        /// <summary>
        /// Register the decisions submenu for player-initiated decisions.
        /// </summary>
        private void RegisterDecisionsMenu(CampaignGameStarter starter)
        {
            // Create the decisions submenu (wait menu like duty selection)
            starter.AddWaitGameMenu("enlisted_decisions",
                "{=Enlisted_Menu_Decisions_Title}— DECISIONS —\n{DECISIONS_STATUS_TEXT}",
                OnDecisionsMenuInit,
                OnDecisionsMenuCondition,
                null,
                OnDecisionsMenuTick,
                GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption);

            // Back option (first, like other submenus)
            starter.AddGameMenuOption("enlisted_decisions", "decisions_back",
                "{=Enlisted_Menu_BackToStatus}Back",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    return true;
                },
                OnDecisionsBackSelected,
                false, 1);

            // Dynamic decision slots (up to 10 decisions shown)
            // Scrolling is allowed here; keep plenty of slots so sections can expand.
            for (var i = 0; i < 40; i++)
            {
                var slotIndex = i;
                starter.AddGameMenuOption("enlisted_decisions", $"decision_slot_{i}",
                    $"{{DECISION_SLOT_{i}_TEXT}}",
                    args => IsDecisionSlotAvailable(args, slotIndex),
                    args => OnDecisionSlotSelected(args, slotIndex),
                    false, i + 2);
            }
        }

        private void RegisterCampHubMenu(CampaignGameStarter starter)
        {
            starter.AddWaitGameMenu(CampHubMenuId,
                "{=enlisted_camp_hub_title}— CAMP —\n{CAMP_HUB_TEXT}",
                OnCampHubInit,
                args =>
                {
                    _ = args;
                    return EnlistmentBehavior.Instance?.IsEnlisted == true;
                },
                null,
                OnCampHubTick,
                GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption);

            // Visit Quartermaster - equipment variant selection and management
            starter.AddGameMenuOption(CampHubMenuId, "camp_hub_quartermaster",
                "{=Enlisted_Menu_VisitQuartermaster}Visit Quartermaster",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Trade;
                    args.Tooltip = new TextObject("{=menu_tooltip_quartermaster}Purchase equipment for your formation and rank. Newly unlocked items marked [NEW].");
                    return true;
                },
                OnQuartermasterSelected,
                false, 2);

            // Medical Tent (treatment) - always listed; grey-out when healthy
            starter.AddGameMenuOption(CampHubMenuId, "camp_hub_medical_tent",
                "{=enlisted_camp_medical_tent}Medical Tent",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Manage;

                    var conditions = PlayerConditionBehavior.Instance;
                    if (conditions?.IsEnabled() != true || conditions.State?.HasAnyCondition != true)
                    {
                        args.IsEnabled = false;
                        args.Tooltip = new TextObject("{=menu_disabled_healthy}You are in good health. No treatment needed.");
                        return true;
                    }

                    args.Tooltip = new TextObject("{=menu_tooltip_seek_medical}Visit the surgeon's tent.");
                    return true;
                },
                _ =>
                {
                    QuartermasterManager.CaptureTimeStateBeforeMenuActivation();
                    GameMenu.SwitchToMenu("enlisted_medical");
                },
                false, 3);

            // Reports (Camp Bulletin overlay)
            starter.AddGameMenuOption(CampHubMenuId, "camp_hub_reports",
                "{=enlisted_camp_reports}Reports",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                    args.Tooltip = new TextObject("{=enlisted_camp_reports_tooltip}Open the Camp Bulletin (daily report, archive, and locations).");
                    return true;
                },
                _ =>
                {
                    // Reports moved to Camp Management - open Reports tab (tab 3)
                    Enlisted.Features.Camp.UI.Management.CampManagementScreen.Open(3);
                },
                false, 4);

            // Camp Management (deep config)
            starter.AddGameMenuOption(CampHubMenuId, "camp_hub_camp_management",
                "{=enlisted_camp_management}Camp Management",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Manage;
                    args.Tooltip = new TextObject("{=enlisted_camp_management_tooltip}Orders, reports, army view, and other management.");
                    return true;
                },
                _ =>
                {
                    Enlisted.Features.Camp.UI.Management.CampManagementScreen.Open(1);
                },
                false, 5);

            // Manage Companions
            starter.AddGameMenuOption(CampHubMenuId, "camp_hub_companions",
                "{=enlisted_camp_companions}Manage Companions",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Manage;
                    args.Tooltip = new TextObject("{=enlisted_camp_companions_tooltip}Assign and manage your companions.");
                    return true;
                },
                _ =>
                {
                    QuartermasterManager.CaptureTimeStateBeforeMenuActivation();
                    GameMenu.SwitchToMenu("enlisted_companions");
                },
                false, 7);

            // Service Records
            starter.AddGameMenuOption(CampHubMenuId, "camp_hub_service_records",
                "{=enlisted_camp_records}Service Records",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Manage;
                    args.Tooltip = new TextObject("{=enlisted_camp_records_tooltip}Review your service records and history.");
                    return true;
                },
                _ =>
                {
                    QuartermasterManager.CaptureTimeStateBeforeMenuActivation();
                    GameMenu.SwitchToMenu("enlisted_service_records");
                },
                false, 6);

            // Personal Retinue (T7+: Commander track with recruit training system)
            starter.AddGameMenuOption(CampHubMenuId, "camp_hub_retinue",
                "{=ct_option_retinue}Muster Personal Retinue",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.TroopSelection;
                    var enlistment = EnlistmentBehavior.Instance;
                    
                    // T7+ for new Commander track retinue system (15/25/35 soldiers)
                    if ((enlistment?.EnlistmentTier ?? 1) < 7)
                    {
                        args.IsEnabled = false;
                        args.Tooltip = new TextObject("{=ct_warn_retinue_tier_locked}You must reach Commander rank (Tier 7) to command your own retinue.");
                        return true;
                    }
                    
                    args.Tooltip = new TextObject("{=ct_retinue_tooltip}Manage your personal retinue of soldiers. Recruits trickle in and must be trained through battle.");
                    return true;
                },
                _ =>
                {
                    QuartermasterManager.CaptureTimeStateBeforeMenuActivation();
                    GameMenu.SwitchToMenu("enlisted_retinue");
                },
                false, 8);

            // Back
            starter.AddGameMenuOption(CampHubMenuId, "camp_hub_back",
                "{=enlisted_camp_back}Back",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    return true;
                },
                _ =>
                {
                    QuartermasterManager.CaptureTimeStateBeforeMenuActivation();
                    GameMenu.SwitchToMenu("enlisted_status");
                },
                false, 100);
        }

        private void OnCampHubInit(MenuCallbackArgs args)
        {
            try
            {
                // Start wait to enable time controls for the wait menu
                args.MenuContext.GameMenu.StartWait();

                // Unlock time control so player can change speed, then restore their prior state
                Campaign.Current.SetTimeControlModeLock(false);

                // Restore captured time using stoppable equivalents, preserving Stop when paused
                var captured = QuartermasterManager.CapturedTimeMode ?? Campaign.Current.TimeControlMode;
                var normalized = QuartermasterManager.NormalizeToStoppable(captured);
                Campaign.Current.TimeControlMode = normalized;

                MBTextManager.SetTextVariable("CAMP_HUB_TEXT", BuildCampHubText());
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("Interface", "E-UI-010", "Error initializing Camp hub", ex);
                MBTextManager.SetTextVariable("CAMP_HUB_TEXT", "Camp hub unavailable.");
            }
        }

        private static void OnCampHubTick(MenuCallbackArgs args, CampaignTime dt)
        {
            _ = args;
            _ = dt;
            // Intentionally empty - hub is refreshed on init and re-entry.
        }

        private static string BuildCampHubText()
        {
            try
            {
                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment?.IsEnlisted != true)
                {
                    return new TextObject("{=enl_camp_hub_not_enlisted}You are not currently enlisted.").ToString();
                }

                var lord = enlistment.CurrentLord;
                var lordName = lord?.Name?.ToString() ?? new TextObject("{=enl_ui_unknown}Unknown").ToString();

                var objective = new TextObject("{=enl_ui_unknown}Unknown").ToString();
                try
                {
                    objective = Instance?.GetCurrentObjectiveDisplay(lord) ?? new TextObject("{=enl_ui_unknown}Unknown").ToString();
                }
                catch
                {
                    /* best-effort */
                }

                var rank = new TextObject("{=enl_ui_unknown}Unknown").ToString();
                try
                {
                    rank = Ranks.RankHelper.GetCurrentRank(enlistment);
                }
                catch
                {
                    /* best-effort */
                }

                var decisions = DecisionEventBehavior.Instance?.GetAvailablePlayerDecisions()?.Count ?? 0;

                var sb = new StringBuilder();

                sb.AppendLine(new TextObject("{=enl_camp_hub_title}Service Status").ToString());

                var lordLine = new TextObject("{=enl_camp_hub_lord_line}Lord: {LORD_NAME}");
                lordLine.SetTextVariable("LORD_NAME", lordName);
                sb.AppendLine(lordLine.ToString());

                var fatigueLine = new TextObject("{=enl_camp_hub_fatigue_line}Fatigue: {FAT_CUR}/{FAT_MAX} | {RANK} (T{TIER})");
                fatigueLine.SetTextVariable("FAT_CUR", enlistment.FatigueCurrent);
                fatigueLine.SetTextVariable("FAT_MAX", enlistment.FatigueMax);
                fatigueLine.SetTextVariable("RANK", rank);
                fatigueLine.SetTextVariable("TIER", enlistment.EnlistmentTier);
                sb.AppendLine(fatigueLine.ToString());

                var workLine = new TextObject("{=enl_camp_hub_objective_line}Lord's Work: {OBJECTIVE}");
                workLine.SetTextVariable("OBJECTIVE", objective);
                sb.AppendLine(workLine.ToString());

                var nowLine = new TextObject("{=enl_camp_hub_now_line}Now: {SITUATION} | Decisions: {COUNT}");
                nowLine.SetTextVariable("SITUATION", BuildCurrentSituationLine(enlistment) ?? string.Empty);
                nowLine.SetTextVariable("COUNT", decisions);
                sb.AppendLine(nowLine.ToString());

                return sb.ToString().TrimEnd();
            }
            catch
            {
                return new TextObject("{=enl_camp_hub_unavailable}Service Status unavailable.").ToString();
            }
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

                // Unlock time control so player can change speed, then restore their prior state
                Campaign.Current.SetTimeControlModeLock(false);

                // Restore captured time using stoppable equivalents, preserving Stop when paused
                var captured = QuartermasterManager.CapturedTimeMode ?? Campaign.Current.TimeControlMode;
                var normalized = QuartermasterManager.NormalizeToStoppable(captured);
                Campaign.Current.TimeControlMode = normalized;

                RefreshEnlistedStatusDisplay(args);
                _menuNeedsRefresh = true;
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("Interface", "E-UI-011", "Error initializing enlisted status menu", ex);
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

                var statusContent = BuildCompactEnlistedStatusText(enlistment, lord, duties);

                // Set text variables for menu display (lord guaranteed non-null from earlier check)
                var lordName = lord.EncyclopediaLinkWithName?.ToString() ?? lord.Name?.ToString() ?? "Unknown";
                var leaderSummary = $"{lordName} | Fatigue {enlistment.FatigueCurrent}/{enlistment.FatigueMax}";

                // Get menu context and set text variables
                var menuContext = args?.MenuContext ?? Campaign.Current.CurrentMenuContext;
                if (menuContext != null)
                {
                    var text = menuContext.GameMenu.GetText();
                    text.SetTextVariable("PARTY_LEADER", leaderSummary);
                    text.SetTextVariable("PARTY_TEXT", statusContent);
                }
                else
                {
                    // Fallback for compatibility
                    MBTextManager.SetTextVariable("PARTY_LEADER", leaderSummary);
                    MBTextManager.SetTextVariable("PARTY_TEXT", statusContent);
                }
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("Interface", "E-UI-036", "Error refreshing enlisted status", ex);

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

        private static string BuildCompactEnlistedStatusText(
            EnlistmentBehavior enlistment,
            Hero lord,
            EnlistedDutiesBehavior duties)
        {
            try
            {
                var sb = new StringBuilder();

                var objective = "Unknown";
                try
                {
                    // Reuse existing objective helper (keeps wording consistent with the rest of the system).
                    objective = Instance?.GetCurrentObjectiveDisplay(lord) ?? "Unknown";
                }
                catch
                {
                    /* best-effort */
                }

                // Company status is a lightweight "camp conditions" snapshot. It is intended to give the player a
                // quick read on logistics strain, morale shock, and pay tension while enlisted.
                var companyLine = TryBuildCompanyStatusLine(enlistment);
                if (!string.IsNullOrWhiteSpace(companyLine))
                {
                    sb.AppendLine(companyLine);
                }

                sb.AppendLine($"Lord's Work: {objective}");

                // Compact service status line (kept short to preserve option list space).
                var rank = "Unknown";
                try
                {
                    rank = Ranks.RankHelper.GetCurrentRank(enlistment);
                }
                catch
                {
                    /* best-effort */
                }

                sb.AppendLine($"Service: {rank} (T{enlistment.EnlistmentTier})");
                sb.AppendLine();

                // The main menu uses a short excerpt from today's persisted report (not the full brief). This keeps
                // vertical space stable and avoids menu "jitter". The menu text area is narrow and uses a large
                // font, so long reports would force scrolling. Keep this very short.
                var reportExcerpt = EnlistedNewsBehavior.Instance?.GetLatestDailyReportExcerpt(maxLines: 2, maxChars: 160);
                if (!string.IsNullOrWhiteSpace(reportExcerpt))
                {
                    var flattened = FlattenMenuParagraph(reportExcerpt);
                    sb.AppendLine($"Report: {TruncateForMenu(flattened, 160)}");
                    sb.AppendLine();
                }

                // Schedule section (4 time blocks). This is the "what you're meant to be doing" plan layer.
                AppendScheduleSection(sb);
                sb.AppendLine();

                // Orders section (always shows header; empty when no orders exist).
                AppendOrdersSection(sb);
                sb.AppendLine();

                // "Now" is the reality layer (battle/settlement/forced states can override the schedule).
                sb.AppendLine($"Now: {BuildCurrentSituationLine(enlistment)}");

                return sb.ToString().TrimEnd();
            }
            catch
            {
                return "Status unavailable.";
            }
        }

        private static void AppendScheduleSection(StringBuilder sb)
        {
            try
            {
                sb.AppendLine("Schedule:");

                var tracker = Enlisted.Mod.Core.Triggers.CampaignTriggerTrackerBehavior.Instance;
                var currentBlock = tracker?.GetTimeBlock() ?? Enlisted.Features.Schedule.Models.TimeBlock.Morning;

                AppendScheduleLine(sb, Enlisted.Features.Schedule.Models.TimeBlock.Morning, currentBlock);
                AppendScheduleLine(sb, Enlisted.Features.Schedule.Models.TimeBlock.Afternoon, currentBlock);
                AppendScheduleLine(sb, Enlisted.Features.Schedule.Models.TimeBlock.Dusk, currentBlock);
                AppendScheduleLine(sb, Enlisted.Features.Schedule.Models.TimeBlock.Night, currentBlock);
            }
            catch
            {
                // Best-effort: schedule is informational only; never hard-fail the menu.
            }
        }

        private static void AppendScheduleLine(
            StringBuilder sb,
            Enlisted.Features.Schedule.Models.TimeBlock lineBlock,
            Enlisted.Features.Schedule.Models.TimeBlock currentBlock)
        {
            var label = GetTimeBlockLabel(lineBlock);
            var dutyTitle = TruncateForMenu(GetScheduleBlockTitleOrDefault(lineBlock), 22);

            // Brackets visually mark the active time block.
            if (lineBlock == currentBlock)
            {
                sb.AppendLine($"[{label}: {dutyTitle}]");
            }
            else
            {
                sb.AppendLine($"{label}: {dutyTitle}");
            }
        }

        private static string GetScheduleBlockTitleOrDefault(Enlisted.Features.Schedule.Models.TimeBlock timeBlock)
        {
            try
            {
                var schedule = Enlisted.Features.Schedule.Behaviors.ScheduleBehavior.Instance?.CurrentSchedule;
                var block = schedule?.GetBlock(timeBlock);
                var title = block?.Title;
                return string.IsNullOrWhiteSpace(title) ? "Free Time" : title.Trim();
            }
            catch
            {
                return "Free Time";
            }
        }

        private static string GetTimeBlockLabel(Enlisted.Features.Schedule.Models.TimeBlock timeBlock)
        {
            // Keep labels short to avoid wrapping on narrower resolutions.
            return timeBlock switch
            {
                Enlisted.Features.Schedule.Models.TimeBlock.Morning => "Morning",
                Enlisted.Features.Schedule.Models.TimeBlock.Afternoon => "Afternoon",
                Enlisted.Features.Schedule.Models.TimeBlock.Dusk => "Dusk",
                Enlisted.Features.Schedule.Models.TimeBlock.Night => "Night",
                _ => "Time"
            };
        }

        private static void AppendOrdersSection(StringBuilder sb)
        {
            sb.AppendLine("Orders:");

            // Orders system is intentionally stubbed for now.
            // We keep the header visible (per spec) but do not show "No orders" when empty.
            // Future: integrate with a real order provider / duty event system.
            var orders = GetActiveOrdersForMenu();
            foreach (var orderLine in orders)
            {
                if (!string.IsNullOrWhiteSpace(orderLine))
                {
                    sb.AppendLine($"  • {orderLine.Trim()}");
                }
            }
        }

        private static List<string> GetActiveOrdersForMenu()
        {
            // Placeholder: orders are not implemented yet.
            return new List<string>();
        }

        private static string TruncateForMenu(string text, int maxChars)
        {
            if (string.IsNullOrWhiteSpace(text) || maxChars <= 0)
            {
                return string.Empty;
            }

            var trimmed = text.Trim();
            if (trimmed.Length <= maxChars)
            {
                return trimmed;
            }

            // Use "..." instead of a unicode ellipsis to reduce font/layout quirks across installs.
            if (maxChars <= 3)
            {
                return trimmed.Substring(0, maxChars);
            }

            return trimmed.Substring(0, maxChars - 3).TrimEnd() + "...";
        }

        private static string FlattenMenuParagraph(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            // Collapse line breaks (the Gauntlet menu wraps text anyway). This prevents huge vertical expansion
            // when the underlying report contains deliberate newlines.
            var flattened = text.Replace("\r", " ").Replace("\n", " ");
            while (flattened.Contains("  "))
            {
                flattened = flattened.Replace("  ", " ");
            }

            return flattened.Trim();
        }

        private static string TryBuildCompanyStatusLine(EnlistmentBehavior enlistment)
        {
            try
            {
                var campLife = Enlisted.Features.Camp.CampLifeBehavior.Instance;
                if (campLife == null || !campLife.IsActiveWhileEnlisted())
                {
                    return null;
                }

                // Keep the main menu focused. Only show a Company status line when it provides actionable signal.
                // (This prevents the menu from growing vertically on higher UI-scale setups.)
                if (!campLife.IsLogisticsHigh() && !campLife.IsMoraleLow() && !campLife.IsPayTensionHigh())
                {
                    return null;
                }

                // LogisticsStrain is a "pressure" meter (higher == worse). Display it explicitly to avoid implying
                // we have the full equipment supply simulation shipped already.
                var logisticsStrain = (int)Math.Round(campLife.LogisticsStrain);

                // MoraleShock is an inverse-morale meter; translate to an intuitive "morale %" for UI.
                var moralePct = (int)Math.Round(100f - campLife.MoraleShock);
                moralePct = Math.Max(0, Math.Min(100, moralePct));

                var payTension = enlistment?.PayTension ?? (int)Math.Round(campLife.PayTension);
                var payStatus = payTension >= 60 ? "Pay DUE" : payTension >= 30 ? "Pay Late" : "Pay OK";

                // Keep this short. The menu text area is narrower than it looks, and long status lines will wrap,
                // pushing options off-screen and forcing scroll.
                return $"Company: Log {logisticsStrain}% | Mor {moralePct}% | {payStatus}";
            }
            catch
            {
                return null;
            }
        }

        private static string BuildCurrentSituationLine(EnlistmentBehavior enlistment)
        {
            try
            {
                var main = MobileParty.MainParty;
                if (main?.Party?.MapEvent != null)
                {
                    return Enlisted.Features.Combat.Behaviors.EnlistedEncounterBehavior.IsWaitingInReserve
                        ? "Waiting in reserve during battle."
                        : "Fighting in battle.";
                }

                if (main?.CurrentSettlement != null)
                {
                    return $"In {main.CurrentSettlement.Name}.";
                }

                var encounterSettlement = PlayerEncounter.EncounterSettlement;
                if (encounterSettlement != null && PlayerEncounter.InsideSettlement)
                {
                    return $"Inside {encounterSettlement.Name}.";
                }

                if (main?.IsMoving == true)
                {
                    var target = enlistment?.CurrentLord?.PartyBelongedTo?.TargetSettlement;
                    if (target != null)
                    {
                        return $"Marching toward {target.Name}.";
                    }

                    return "On the march.";
                }

                // Default: in camp / idle on the campaign map.
                return "In camp with the company.";
            }
            catch
            {
                return "In camp.";
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
            return Assignments.Core.ConfigurationManager.GetXpRequiredForTier(currentTier);
        }

        /// <summary>
        ///     Gets display text for total days the player has been enlisted.
        ///     There are no terms anymore - service is indefinite until player leaves.
        /// </summary>
        private string GetDaysEnlistedDisplay(EnlistmentBehavior enlistment)
        {
            try
            {
                if (enlistment?.EnlistmentDate == null || enlistment.EnlistmentDate == CampaignTime.Zero)
                {
                    return "0";
                }

                var daysServed = (int)(CampaignTime.Now - enlistment.EnlistmentDate).ToDays;
                if (daysServed < 0)
                {
                    daysServed = 0;
                }

                return daysServed.ToString();
            }
            catch
            {
                return "0";
            }
        }

        /// <summary>
        ///     Builds a visual progress bar for escalation tracks.
        ///     Uses filled (▓) and empty (░) blocks for ASCII-compatible display.
        /// </summary>
        private static string BuildTrackBar(int current, int max)
        {
            const int barLength = 10;
            var filled = Math.Min(barLength, Math.Max(0, current));
            var empty = barLength - filled;
            return new string('▓', filled) + new string('░', empty);
        }

        /// <summary>
        ///     Gets a warning label for the current Heat level based on threshold events.
        /// </summary>
        private static string GetHeatWarning(int heat)
        {
            return heat switch
            {
                >= 10 => "EXPOSED",
                >= 7 => "Audit",
                >= 5 => "Shakedown",
                >= 3 => "Watched",
                _ => ""
            };
        }

        /// <summary>
        ///     Gets a warning label for the current Discipline level based on threshold events.
        /// </summary>
        private static string GetDisciplineWarning(int discipline)
        {
            return discipline switch
            {
                >= 10 => "DISCHARGE",
                >= 7 => "Blocked",
                >= 5 => "Hearing",
                >= 3 => "Extra Duty",
                _ => ""
            };
        }

        /// <summary>
        ///     Calculates the number of days since the party was last in a settlement.
        ///     Returns 0 if currently in a settlement.
        /// </summary>
        private static int CalculateDaysFromTown(Hero lord)
        {
            try
            {
                if (lord?.PartyBelongedTo?.CurrentSettlement != null)
                {
                    return 0; // Currently in settlement
                }

                var party = lord?.PartyBelongedTo;
                if (party == null)
                {
                    return 0;
                }

                // Check if there's a nearby settlement using correct API (GetPosition2D)
                var partyPos = party.GetPosition2D;
                var nearestSettlement = Settlement.FindFirst(s => 
                    (s.IsTown || s.IsCastle) && 
                    partyPos.DistanceSquared(s.GetPosition2D) < 10f);
                
                if (nearestSettlement != null)
                {
                    return 1; // Near a settlement
                }

                // Estimate based on distance to nearest settlement
                return 3; // Default to 3 days if far from settlements
            }
            catch
            {
                return 0;
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
                ModLogger.ErrorCode("Interface", "E-UI-037", "Error getting formation training description", ex);
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
                ModLogger.ErrorCode("Interface", "E-UI-012", "Error refreshing menu", ex);
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

        /// <summary>
        /// Kept for save compatibility. This option is obsolete and always hidden because formation is chosen via
        /// proving events instead of the Master at Arms menu.
        /// </summary>
        [System.Obsolete("Formation is now chosen via proving events, not the Master at Arms menu.")]
        private bool IsMasterAtArmsAvailable(MenuCallbackArgs args)
        {
            _ = args;
            return false; // Always hidden.
        }

        /// <summary>
        /// Kept for save compatibility. This handler is obsolete and should not be reachable in normal play.
        /// </summary>
        [System.Obsolete("Formation is now chosen via proving events, not the Master at Arms menu.")]
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
                        new TextObject("{=menu_master_unavailable}Master at Arms system is temporarily unavailable.").ToString()));
                    return;
                }

                manager.ShowMasterAtArmsPopup();
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("Interface", "E-UI-013", "Error opening Master at Arms", ex);
            }
        }

        private bool IsQuartermasterAvailable(MenuCallbackArgs args)
        {
            _ = args; // Required by API contract
            return EnlistmentBehavior.Instance?.IsEnlisted == true;
        }

        // IsMyLanceAvailable removed - lance access now via Visit Camp > Lance location

        /// <summary>
        /// Checks if Seek Medical Attention option is available.
        /// Only shows when player has an injury, illness, or exhaustion condition.
        /// </summary>
        private static bool IsSeekMedicalAvailable(MenuCallbackArgs args)
        {
            _ = args;
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment?.IsEnlisted != true)
            {
                return false;
            }

            var cond = PlayerConditionBehavior.Instance;
            if (cond?.IsEnabled() != true)
            {
                return false;
            }

            // Only show if player has an active condition
            if (cond.State?.HasAnyCondition != true)
            {
                return false;
            }

            return true;
        }

        private void OnQuartermasterSelected(MenuCallbackArgs args)
        {
            try
            {
                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment?.IsEnlisted != true)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("{=menu_must_be_enlisted_qm}You must be enlisted to access quartermaster services.").ToString()));
                    ModLogger.Warn("Quartermaster", "Quartermaster open blocked: player not enlisted");
                    return;
                }

                // Try to open conversation with the Quartermaster hero.
                var qm = enlistment.GetOrCreateQuartermaster();
                
                if (qm != null && qm.IsAlive)
                {
                    ModLogger.Info("Quartermaster",
                        $"Opening conversation with quartermaster '{qm.Name}' ({enlistment.QuartermasterArchetype})");

                    // Open conversation with quartermaster Hero
                    // The dialog tree is registered in EnlistedDialogManager
                    CampaignMapConversation.OpenConversation(
                        new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty),
                        new ConversationCharacterData(qm.CharacterObject, qm.PartyBelongedTo?.Party)
                    );
                }
                else
                {
                    // Fallback: Direct to menu if hero creation/conversation fails
                    ModLogger.Warn("Quartermaster", "Quartermaster Hero unavailable, falling back to direct menu");
                    OpenQuartermasterMenuDirectly();
                }
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("Interface", "E-UI-038", "Error opening quartermaster conversation", ex);
                // Fallback to direct menu access
                OpenQuartermasterMenuDirectly();
            }
        }

        /// <summary>
        ///     Opens the quartermaster menu directly (fallback for when Hero conversation fails).
        /// </summary>
        private void OpenQuartermasterMenuDirectly()
        {
            try
            {
                var quartermasterManager = QuartermasterManager.Instance;
                if (quartermasterManager != null)
                {
                    QuartermasterManager.CaptureTimeStateBeforeMenuActivation();
                    GameMenu.ActivateGameMenu("quartermaster_equipment");
                }
                else
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("{=menu_qm_unavailable}Quartermaster services temporarily unavailable.").ToString()));
                    ModLogger.ErrorCode("Quartermaster", "E-QM-001",
                        "Quartermaster menu failed: QuartermasterManager.Instance was null");
                }
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("Quartermaster", "E-QM-002", "Error opening quartermaster menu directly", ex);
                InformationManager.DisplayMessage(new InformationMessage(
                    new TextObject("{=menu_qm_error}Quartermaster system error. Please report this issue.").ToString()));
            }
        }

        private void OnQuartermasterHorsesSelected(MenuCallbackArgs args)
        {
            try
            {
                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment?.IsEnlisted != true)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("{=menu_must_be_enlisted_qm}You must be enlisted to access quartermaster services.").ToString()));
                    ModLogger.Warn("Quartermaster", "Quartermaster (horse/tack) open blocked: player not enlisted");
                    return;
                }

                var quartermasterManager = QuartermasterManager.Instance;
                if (quartermasterManager != null)
                {
                    QuartermasterManager.CaptureTimeStateBeforeMenuActivation();
                    quartermasterManager.SetFilterToHorseAndTack();
                    ModLogger.Info("Quartermaster",
                        $"Opening quartermaster menu (horse/tack filter) (Tier={enlistment.EnlistmentTier}, Troop={TroopSelectionManager.Instance?.LastSelectedTroopId ?? "unknown"})");
                    GameMenu.ActivateGameMenu("quartermaster_equipment");
                }
                else
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("{=menu_qm_unavailable}Quartermaster services temporarily unavailable.").ToString()));
                    ModLogger.ErrorCode("Quartermaster", "E-QM-003",
                        "Quartermaster (horse/tack) open failed: QuartermasterManager.Instance was null");
                }
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("Interface", "E-UI-039", "Error accessing quartermaster horse/tack services", ex);
                InformationManager.DisplayMessage(new InformationMessage(
                    new TextObject("{=menu_qm_error}Quartermaster system error. Please report this issue.").ToString()));
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
                        new TextObject("{=menu_must_be_enlisted_lords}You must be enlisted to speak with lords.").ToString()));
                    return;
                }

                // Find nearby lords for conversation
                var nearbyLords = GetNearbyLordsForConversation();
                if (nearbyLords.Count == 0)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("{=menu_no_lords_available}No lords are available for conversation at this location.").ToString()));
                    return;
                }

                // Show lord selection inquiry
                ShowLordSelectionInquiry(nearbyLords);
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("Interface", "E-UI-014", "Error in Talk to My Lord", ex);
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
                ModLogger.ErrorCode("Interface", "E-UI-015", "Error finding nearby lords", ex);
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
                var unknown = new TextObject("{=enl_ui_unknown}Unknown").ToString();
                foreach (var lord in lords)
                {
                    var name = lord.Name?.ToString() ?? unknown;
                    // 1.3.4 API: ImageIdentifier is now abstract, use CharacterImageIdentifier
                    var portrait = new CharacterImageIdentifier(CharacterCode.CreateFrom(lord.CharacterObject));
                    var description =
                        $"{lord.Clan?.Name?.ToString() ?? unknown}\n{lord.MapFaction?.Name?.ToString() ?? unknown}";

                    options.Add(new InquiryElement(lord, name, portrait, true, description));
                }

                var data = new MultiSelectionInquiryData(
                    titleText: new TextObject("{=enl_ui_select_lord_title}Select lord to speak with").ToString(),
                    descriptionText: string.Empty,
                    inquiryElements: options,
                    isExitShown: true,
                    minSelectableOptionCount: 1,
                    maxSelectableOptionCount: 1,
                    affirmativeText: new TextObject("{=enl_ui_talk}Talk").ToString(),
                    negativeText: new TextObject("{=enl_ui_cancel}Cancel").ToString(),
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
                            ModLogger.ErrorCode("Interface", "E-UI-016", "Error starting lord conversation", ex);
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
                ModLogger.ErrorCode("Interface", "E-UI-017", "Error showing lord selection", ex);
            }
        }

        /// <summary>
        ///     Start conversation with selected lord using verified TaleWorlds APIs.
        ///     Uses different conversation systems for land vs sea to ensure proper scene selection.
        /// </summary>
        private void StartConversationWithLord(Hero lord)
        {
            try
            {
                if (lord?.PartyBelongedTo == null)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("{=menu_lord_not_available}Lord is not available for conversation.").ToString()));
                    return;
                }

                var playerData = new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty);
                var lordData = new ConversationCharacterData(lord.CharacterObject, lord.PartyBelongedTo.Party);

                // At sea: use mission-based conversation with proper ship scene
                // On land: use map conversation (character portraits)
                // This mirrors PlayerEncounter behavior for proper scene selection
                if (MobileParty.MainParty?.IsCurrentlyAtSea == true)
                {
                    // Use Naval DLC's sea conversation scene for proper ship deck visuals
                    const string seaConversationScene = "conversation_scene_sea_multi_agent";
                    ModLogger.Info("Interface", $"Opening sea conversation with {lord.Name} using scene: {seaConversationScene}");
                    CampaignMission.OpenConversationMission(playerData, lordData, seaConversationScene);
                }
                else
                {
                    // Standard land conversation using map conversation system
                    CampaignMapConversation.OpenConversation(playerData, lordData);
                }
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("Interface", "E-UI-018", $"Error opening conversation with {lord?.Name}", ex);
                InformationManager.DisplayMessage(new InformationMessage(
                    new TextObject("{=menu_conversation_error}Unable to start conversation. Please try again.").ToString()));
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

            // Check if lord is in a town or castle AND player hasn't already entered the native town/castle menu
            // When in an army, CurrentSettlement is set but player hasn't visited the town yet (shops, keep, etc.)
            // We check if the current menu is a native settlement menu to determine if they're actually inside
            var currentMenu = Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId ?? "";
            var playerInNativeSettlementMenu = currentMenu == "town" || currentMenu == "castle" ||
                currentMenu.StartsWith("town_") || currentMenu.StartsWith("castle_");
            var canVisit = (settlement.IsTown || settlement.IsCastle) && !playerInNativeSettlementMenu;

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
                ModLogger.ErrorCode("Interface", "E-UI-019", "Error in settlement entered tracking", ex);
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
                        new TextObject("{=menu_lord_not_in_settlement}Your lord is not in a town or castle.").ToString()));
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
                ModLogger.ErrorCode("Interface", "E-UI-020", "VisitTown failed", ex);
                InformationManager.DisplayMessage(new InformationMessage(
                    new TextObject("{=menu_town_interface_error}Couldn't open the town interface.").ToString()));
            }
        }


        private bool IsAskLeaveAvailable(MenuCallbackArgs args)
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment?.IsEnlisted != true)
            {
                return false;
            }

            if (enlistment.IsLeaveOnCooldown(out var daysRemaining))
            {
                args.IsEnabled = false;
                var tooltip = new TextObject("{=Enlisted_Leave_Cooldown_Tooltip}Leave is on cooldown. {DAYS} days remain.");
                tooltip.SetTextVariable("DAYS", daysRemaining);
                args.Tooltip = tooltip;
            }

            return true;
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
                var titleText = new TextObject("{=enl_ui_request_leave_title}Request Leave from Commander").ToString();
                var descriptionText = new TextObject("{=enl_ui_request_leave_desc}Request temporary leave for 14 days. You regain independent movement but forfeit daily wages and duties until you return. Taking leave starts a 7-day cooldown after you come back.").ToString();

                var confirmData = new InquiryData(
                    titleText,
                    descriptionText,
                    isAffirmativeOptionShown: true,
                    isNegativeOptionShown: true,
                    affirmativeText: new TextObject("{=enl_ui_request_leave}Request Leave").ToString(),
                    negativeText: new TextObject("{=enl_ui_cancel}Cancel").ToString(),
                    affirmativeAction: () =>
                    {
                        try
                        {
                            RequestTemporaryLeave();
                        }
                        catch (Exception ex)
                        {
                            ModLogger.ErrorCode("Interface", "E-UI-021", "Error requesting leave", ex);
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
                ModLogger.ErrorCode("Interface", "E-UI-022", "Error in Ask for Leave", ex);
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

                var message = new TextObject("{=enl_ui_leave_granted}Leave granted. You are temporarily released from service. Speak with your lord when ready to return to duty.");
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
                ModLogger.ErrorCode("Interface", "E-UI-023", "Error granting temporary leave", ex);
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
                ModLogger.ErrorCode("Interface", "E-UI-024", "Error opening desertion menu", ex);
            }
        }

        /// <summary>
        ///     Handles free desertion when PayTension >= 60.
        ///     Shows a confirmation dialog then processes the clean desertion.
        /// </summary>
        private void OnFreeDesertionSelected(MenuCallbackArgs args)
        {
            try
            {
                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment?.IsEnlisted != true || enlistment.PayTension < 60)
                {
                    ModLogger.Warn("Interface", "Free desertion attempted but conditions not met");
                    return;
                }

                var lordName = enlistment.CurrentLord?.Name?.ToString() ?? "your lord";
                var confirmText = new TextObject(
                    "Pay has been late for too long. You approach your lance-mates and explain that you can't continue like this.\n\n" +
                    "They nod slowly. \"Can't blame you. No one would. Go — find something better.\"\n\n" +
                    "You can leave now with minimal consequences:\n" +
                    "• -5 relation with {LORD_NAME} (they understand)\n" +
                    "• No crime penalty\n" +
                    "• Keep your equipment\n\n" +
                    "Are you sure you want to leave?");
                confirmText.SetTextVariable("LORD_NAME", lordName);

                InformationManager.ShowInquiry(new InquiryData(
                    new TextObject("{=Enlisted_FreeDesert_Title}Leave Without Penalty").ToString(),
                    confirmText.ToString(),
                    true, true,
                    new TextObject("{=Enlisted_FreeDesert_Confirm}Leave").ToString(),
                    new TextObject("{=Enlisted_FreeDesert_Cancel}Stay").ToString(),
                    () =>
                    {
                        // Process free desertion - no major penalties
                        enlistment.ProcessFreeDesertion();
                        ModLogger.Info("Interface", "Player executed free desertion due to high PayTension");
                    },
                    () =>
                    {
                        // Player chose to stay
                        ModLogger.Debug("Interface", "Player cancelled free desertion");
                    }));
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("Interface", "E-UI-025", "Error in free desertion", ex);
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

            // Back button - returns to Leave Service submenu (Leave icon)
            starter.AddGameMenuOption("enlisted_desert_confirm", "desert_back",
                "{=enl_ui_back}Back",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    return true;
                },
                OnDesertionBackSelected,
                false, 0);

            // Continue with Desertion button - executes the desertion (Escape icon - already set)
            starter.AddGameMenuOption("enlisted_desert_confirm", "desert_confirm",
                "{=enl_ui_desert_confirm}Desert the Army (Accept Penalties)",
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
                ModLogger.ErrorCode("Interface", "E-UI-026", "Error initializing desertion menu", ex);
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
                // Capture time state BEFORE menu switch to preserve player's time control
                QuartermasterManager.CaptureTimeStateBeforeMenuActivation();
                GameMenu.SwitchToMenu(LeaveServiceMenuId);
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("Interface", "E-UI-027", "Error returning from desertion menu", ex);
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
                    ModLogger.ErrorCode("Interface", "E-UI-040", "Cannot desert - EnlistmentBehavior not available", null);
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
                        ModLogger.ErrorCode("Interface", "E-UI-028", "Error exiting menu after desertion", ex);
                    }
                });

                ModLogger.Info("Interface", "Desertion confirmed and executed");
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("Interface", "E-UI-029", "Error confirming desertion", ex);
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
                
                // NOTE: Time mode restoration is handled ONCE in OnEnlistedStatusInit, not here.
                // Previously this tick handler would restore CapturedTimeMode whenever it saw
                // UnstoppableFastForward, but this fought with user input - when the user clicked
                // fast forward (which sets UnstoppableFastForward for army members), the next tick
                // would immediately restore it back to Stop. This caused x3 speed to pause.
                // The fix is to only handle time mode conversion once during menu init.
                
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
                ModLogger.ErrorCode("Interface", "E-UI-030", "Error during enlisted status tick", ex);
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
        ///     Handle Report for Duty selection - open Camp Management Duties tab.
        /// </summary>
        private void OnReportDutySelected(MenuCallbackArgs args)
        {
            try
            {
                // Open Camp Management screen with Duties tab (index 2)
                Enlisted.Features.Camp.UI.Management.CampManagementScreen.Open(2);
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("Interface", "E-UI-031", "Error opening Report for Duty", ex);
            }
        }


        #region Decision Events Menu Handlers

        private sealed class DecisionsMenuEntry
        {
            public string Id { get; set; }
            public string Text { get; set; }
            public TextObject Tooltip { get; set; }
            public GameMenuOption.LeaveType LeaveType { get; set; } = GameMenuOption.LeaveType.Continue;
            public bool IsEnabled { get; set; } = true;
            public bool IsVisible { get; set; } = true;
            public Action<MenuCallbackArgs> OnSelected { get; set; }
        }

        private enum DecisionsMenuSection
        {
            Queued,
            Training,
            Social,
            CampLife,
            Opportunities
        }

        private List<DecisionsMenuEntry> _cachedDecisionsMenuEntries = new List<DecisionsMenuEntry>();

        // Decision menu section collapse state (accordion-style). These persist while the campaign is running.
        private bool _decisionsCollapsedQueued;
        private bool _decisionsCollapsedTraining;
        private bool _decisionsCollapsedSocial;
        private bool _decisionsCollapsedCampLife;
        private bool _decisionsCollapsedOpportunities;

        // Accordion behavior: on the first open, everything starts collapsed. After that, we reopen the last expanded
        // section (if any). Expanding one section collapses the others.
        private bool _decisionsAccordionInitialized;
        private DecisionsMenuSection? _decisionsLastExpandedSection;

        // "New changes" tracking for accordion headers.
        // Note: The GameMenu option list uses RichTextWidget, which supports <span style="...">. We use the built-in
        // "Link" style as a safe, readable highlight color (instead of hard-coded hex colors).
        private bool _decisionsSnapshotsInitialized;
        private HashSet<string> _decisionsPrevQueuedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private HashSet<string> _decisionsPrevOpportunityIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private bool _decisionsPrevCampLifeEnabled;

        private bool _decisionsNewQueued;
        private bool _decisionsNewOpportunities;
        private bool _decisionsNewCampLife;

        private CampaignTime? _decisionsNewQueuedSince;
        private CampaignTime? _decisionsNewOpportunitiesSince;
        private CampaignTime? _decisionsNewCampLifeSince;

        private static readonly CampaignTime DecisionsNewAutoClearThreshold = CampaignTime.Days(1f);

        private static string NewTag(bool hasNew)
        {
            return hasNew ? " <span style=\"Link\">[NEW]</span>" : string.Empty;
        }

        private void MaybeClearExpiredDecisionsNewFlags()
        {
            try
            {
                var now = CampaignTime.Now;

                if (_decisionsNewQueued && _decisionsNewQueuedSince.HasValue &&
                    now - _decisionsNewQueuedSince.Value > DecisionsNewAutoClearThreshold)
                {
                    _decisionsNewQueued = false;
                    _decisionsNewQueuedSince = null;
                }

                if (_decisionsNewOpportunities && _decisionsNewOpportunitiesSince.HasValue &&
                    now - _decisionsNewOpportunitiesSince.Value > DecisionsNewAutoClearThreshold)
                {
                    _decisionsNewOpportunities = false;
                    _decisionsNewOpportunitiesSince = null;
                }

                if (_decisionsNewCampLife && _decisionsNewCampLifeSince.HasValue &&
                    now - _decisionsNewCampLifeSince.Value > DecisionsNewAutoClearThreshold)
                {
                    _decisionsNewCampLife = false;
                    _decisionsNewCampLifeSince = null;
                }
            }
            catch
            {
                // Best-effort only; never let a marker break the menu.
            }
        }

        private void ToggleDecisionsSection(DecisionsMenuSection section, MenuCallbackArgs args)
        {
            try
            {
                // Accordion rules: expanding one section collapses the others. Collapsing the active section leaves
                // everything collapsed.
                var isCollapsed = IsDecisionsSectionCollapsed(section);
                if (isCollapsed)
                {
                    CollapseAllDecisionsSections();
                    SetDecisionsSectionCollapsed(section, collapsed: false);
                    _decisionsLastExpandedSection = section;

                    // Opening a section clears its "new" marker.
                    if (section == DecisionsMenuSection.Queued)
                    {
                        _decisionsNewQueued = false;
                        _decisionsNewQueuedSince = null;
                    }
                    else if (section == DecisionsMenuSection.CampLife)
                    {
                        _decisionsNewCampLife = false;
                        _decisionsNewCampLifeSince = null;
                    }
                    else if (section == DecisionsMenuSection.Opportunities)
                    {
                        _decisionsNewOpportunities = false;
                        _decisionsNewOpportunitiesSince = null;
                    }
                }
                else
                {
                    SetDecisionsSectionCollapsed(section, collapsed: true);
                    if (_decisionsLastExpandedSection.HasValue && _decisionsLastExpandedSection.Value == section)
                    {
                        _decisionsLastExpandedSection = null;
                    }
                }

                // Rebuild menu entries/text and then force the menu to re-evaluate option visibility.
                OnDecisionsMenuInit(args);

                var menuContext = args?.MenuContext ?? Campaign.Current?.CurrentMenuContext;
                if (Campaign.Current != null && menuContext?.GameMenu != null)
                {
                    Campaign.Current.GameMenuManager.RefreshMenuOptions(menuContext);
                }
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("Interface", "E-UI-044", "Failed to toggle decisions section", ex);
            }
        }

        private void EnsureDecisionsAccordionInitialized()
        {
            // First open: everything collapsed.
            if (!_decisionsAccordionInitialized)
            {
                CollapseAllDecisionsSections();
                _decisionsLastExpandedSection = null;
                _decisionsAccordionInitialized = true;
                return;
            }

            // Subsequent opens: reopen to last expanded section (if any).
            if (_decisionsLastExpandedSection.HasValue)
            {
                CollapseAllDecisionsSections();
                SetDecisionsSectionCollapsed(_decisionsLastExpandedSection.Value, collapsed: false);
            }
        }

        private void CollapseAllDecisionsSections()
        {
            _decisionsCollapsedQueued = true;
            _decisionsCollapsedTraining = true;
            _decisionsCollapsedSocial = true;
            _decisionsCollapsedCampLife = true;
            _decisionsCollapsedOpportunities = true;
        }

        private bool IsDecisionsSectionCollapsed(DecisionsMenuSection section)
        {
            switch (section)
            {
                case DecisionsMenuSection.Queued:
                    return _decisionsCollapsedQueued;
                case DecisionsMenuSection.Training:
                    return _decisionsCollapsedTraining;
                case DecisionsMenuSection.Social:
                    return _decisionsCollapsedSocial;
                case DecisionsMenuSection.CampLife:
                    return _decisionsCollapsedCampLife;
                case DecisionsMenuSection.Opportunities:
                    return _decisionsCollapsedOpportunities;
                default:
                    return true;
            }
        }

        private void SetDecisionsSectionCollapsed(DecisionsMenuSection section, bool collapsed)
        {
            switch (section)
            {
                case DecisionsMenuSection.Queued:
                    _decisionsCollapsedQueued = collapsed;
                    break;
                case DecisionsMenuSection.Training:
                    _decisionsCollapsedTraining = collapsed;
                    break;
                case DecisionsMenuSection.Social:
                    _decisionsCollapsedSocial = collapsed;
                    break;
                case DecisionsMenuSection.CampLife:
                    _decisionsCollapsedCampLife = collapsed;
                    break;
                case DecisionsMenuSection.Opportunities:
                    _decisionsCollapsedOpportunities = collapsed;
                    break;
            }
        }

        /// <summary>
        /// Handler for "Pending Decisions" option in enlisted_status menu.
        /// Opens the decisions submenu.
        /// </summary>
        private void OnPendingDecisionsSelected(MenuCallbackArgs args)
        {
            try
            {
                QuartermasterManager.CaptureTimeStateBeforeMenuActivation();
                GameMenu.SwitchToMenu("enlisted_decisions");
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("Interface", "E-UI-042", "Failed to open decisions menu", ex);
            }
        }

        /// <summary>
        /// Initialize decisions menu - loads available decisions and sets up text.
        /// </summary>
        private void OnDecisionsMenuInit(MenuCallbackArgs args)
        {
            try
            {
                EnsureDecisionsAccordionInitialized();

                // Restore time mode preserved from parent menu
                var capturedMode = QuartermasterManager.CapturedTimeMode
                    ?? Campaign.Current?.TimeControlMode
                    ?? CampaignTimeControlMode.Stop;

                if (!QuartermasterManager.CapturedTimeMode.HasValue)
                {
                    QuartermasterManager.CapturedTimeMode = capturedMode;
                }

                if (Campaign.Current != null)
                {
                    Campaign.Current.TimeControlMode = capturedMode;
                }

                var decisionBehavior = DecisionEventBehavior.Instance;
                var enlistment = EnlistmentBehavior.Instance;
                var timeBlock = CampaignTriggerTrackerBehavior.Instance?.GetTimeBlock() ?? TimeBlock.Morning;

                var queued = decisionBehavior?.GetQueuedFreeTimeDecisions() ?? new List<QueuedFreeTimeDecision>();
                var available = decisionBehavior?.GetAvailablePlayerDecisions() ?? new List<LanceLifeEventDefinition>();

                // Remove duplicates for the specific "static menu surfaced" events (we show them as free-time actions).
                var suppressedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "player_organize_dice_game",
                    "player_visit_wounded",
                    "player_petition_lord",
                    "player_write_letter",
                    "player_request_training"
                };
                available = available.Where(d => d != null && !suppressedIds.Contains(d.Id)).ToList();

                // Track changes between menu openings so section headers can show a [NEW] marker. We highlight when
                // new queued decisions appear, when new opportunities appear, and when "Visit the Wounded" becomes
                // available because wounded are present.
                var currentQueuedIds = new HashSet<string>(
                    queued.Where(q => q != null && !string.IsNullOrWhiteSpace(q.Id)).Select(q => q.Id),
                    StringComparer.OrdinalIgnoreCase);

                var currentOpportunityIds = new HashSet<string>(
                    available.Where(d => d != null && !string.IsNullOrWhiteSpace(d.Id)).Select(d => d.Id),
                    StringComparer.OrdinalIgnoreCase);

                var campLifeEnabledNow = false;
                try
                {
                    campLifeEnabledNow = MobileParty.MainParty?.MemberRoster?.TotalWounded > 0;
                }
                catch
                {
                    campLifeEnabledNow = false;
                }

                if (_decisionsSnapshotsInitialized)
                {
                    if (!_decisionsNewQueued && currentQueuedIds.Except(_decisionsPrevQueuedIds).Any())
                    {
                        _decisionsNewQueued = true;
                        _decisionsNewQueuedSince ??= CampaignTime.Now;
                    }

                    if (!_decisionsNewOpportunities && currentOpportunityIds.Except(_decisionsPrevOpportunityIds).Any())
                    {
                        _decisionsNewOpportunities = true;
                        _decisionsNewOpportunitiesSince ??= CampaignTime.Now;
                    }

                    if (!_decisionsNewCampLife && campLifeEnabledNow && !_decisionsPrevCampLifeEnabled)
                    {
                        _decisionsNewCampLife = true;
                        _decisionsNewCampLifeSince ??= CampaignTime.Now;
                    }
                }

                _decisionsSnapshotsInitialized = true;
                _decisionsPrevQueuedIds = currentQueuedIds;
                _decisionsPrevOpportunityIds = currentOpportunityIds;
                _decisionsPrevCampLifeEnabled = campLifeEnabledNow;

                // Auto-clear markers after a while so they don't stick forever if the player ignores them.
                MaybeClearExpiredDecisionsNewFlags();

                // Status text (short and readable; scrolling is allowed on the options list).
                var status = new TextObject("{=enlisted_decisions_status}Time: {TIME} | Fatigue: {FAT}/{MAX} | Queued: {Q}\nSelect an action. It will execute at the next appropriate slot.");
                status.SetTextVariable("TIME", timeBlock.ToString());
                status.SetTextVariable("FAT", enlistment?.FatigueCurrent ?? 0);
                status.SetTextVariable("MAX", enlistment?.FatigueMax ?? 0);
                status.SetTextVariable("Q", queued.Count);
                MBTextManager.SetTextVariable("DECISIONS_STATUS_TEXT", status);

                _cachedDecisionsMenuEntries = BuildDecisionsMenuEntries(enlistment, decisionBehavior, queued, available, timeBlock);

                for (var i = 0; i < 40; i++)
                {
                    var slotText = i < _cachedDecisionsMenuEntries.Count
                        ? (_cachedDecisionsMenuEntries[i]?.Text ?? string.Empty)
                        : string.Empty;
                    MBTextManager.SetTextVariable($"DECISION_SLOT_{i}_TEXT", slotText);
                }
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("Interface", "E-UI-043", "Failed to initialize decisions menu", ex);
                MBTextManager.SetTextVariable("DECISIONS_STATUS_TEXT", "Error loading decisions.");
            }
        }

        private List<DecisionsMenuEntry> BuildDecisionsMenuEntries(
            EnlistmentBehavior enlistment,
            DecisionEventBehavior decisionBehavior,
            IReadOnlyList<QueuedFreeTimeDecision> queued,
            IReadOnlyList<LanceLifeEventDefinition> available,
            TimeBlock timeBlock)
        {
            var list = new List<DecisionsMenuEntry>();

            // Decisions is intentionally a native-looking GameMenu list.
            // Instead of "blank lines" (which make rows taller and look awkward), we use styled header rows (Option A).

            // Scheduled
            list.Add(new DecisionsMenuEntry
            {
                Id = "header_queued",
                Text = "<span style=\"Link\">SCHEDULED</span>" + NewTag(_decisionsNewQueued),
                IsEnabled = true,
                // Header: show a Bannerlord-native menu icon (rendered from LeaveType, not from text).
                LeaveType = GameMenuOption.LeaveType.Wait,
                OnSelected = a => ToggleDecisionsSection(DecisionsMenuSection.Queued, a)
            });

            if (!_decisionsCollapsedQueued)
            {
                if (queued == null || queued.Count == 0)
                {
                    list.Add(new DecisionsMenuEntry
                    {
                        Id = "queued_none",
                        Text = "    (none)",
                        IsEnabled = false,
                        // Non-header: hide icon (using -1 as no sprite exists for it).
                        LeaveType = (GameMenuOption.LeaveType)(-1)
                    });
                }
                else
                {
                    foreach (var q in queued)
                    {
                        if (q == null || string.IsNullOrWhiteSpace(q.Id))
                        {
                            continue;
                        }

                        var windowText = q.Window == FreeTimeDecisionWindow.Training ? "Training" :
                            q.Window == FreeTimeDecisionWindow.Social ? "Social" : "Any";

                        var title = q.Id;
                        if (q.Kind == FreeTimeDecisionKind.Event)
                        {
                            // Try to show the event title (if loaded).
                            var evt = LanceLifeEventRuntime.GetCatalog()?.Events?.FirstOrDefault(e => e.Id == q.Id);
                            title = !string.IsNullOrWhiteSpace(evt?.TitleFallback) ? evt.TitleFallback : q.Id;
                        }

                        list.Add(new DecisionsMenuEntry
                        {
                            Id = $"cancel_{q.Id}",
                            Text = $"    Cancel: {title} (next {windowText})",
                            LeaveType = (GameMenuOption.LeaveType)(-1),
                            Tooltip = new TextObject("{=enlisted_decisions_cancel_tooltip}Remove this queued action."),
                            OnSelected = _ =>
                            {
                                if (decisionBehavior == null)
                                {
                                    return;
                                }

                                if (decisionBehavior.TryCancelQueuedFreeTimeDecision(q.Id, out var msg))
                                {
                                    InformationManager.DisplayMessage(new InformationMessage(msg.ToString()));
                                }
                            }
                        });
                    }
                }
            }

            // Training
            list.Add(new DecisionsMenuEntry
            {
                Id = "header_training",
                Text = "<span style=\"Link\">TRAINING</span>",
                IsEnabled = true,
                // Bannerlord-native menu icon
                LeaveType = GameMenuOption.LeaveType.OrderTroopsToAttack,
                OnSelected = a => ToggleDecisionsSection(DecisionsMenuSection.Training, a)
            });

            if (!_decisionsCollapsedTraining)
            {
                AddQueueEntry(list, decisionBehavior, enlistment,
                    id: "ft_training_formation",
                    text: "Formation Drill",
                    tooltip: "{=enlisted_training_formation_tt}Extra drill time focused on conditioning and formation practice.\nCost: 5 Fatigue",
                    kind: FreeTimeDecisionKind.TrainingAction,
                    window: FreeTimeDecisionWindow.Training,
                    desiredFatigue: 5);

                AddQueueEntry(list, decisionBehavior, enlistment,
                    id: "ft_training_combat",
                    text: "Combat Drill",
                    tooltip: "{=enlisted_training_combat_tt}Extra practice with your equipped weapon.\nCost: 5 Fatigue",
                    kind: FreeTimeDecisionKind.TrainingAction,
                    window: FreeTimeDecisionWindow.Training,
                    desiredFatigue: 5);

                AddQueueEntry(list, decisionBehavior, enlistment,
                    id: "ft_training_specialist",
                    text: "Specialist Practice",
                    tooltip: "{=enlisted_training_specialist_tt}Focused practice on a specialist skill (placeholder until duty-role mapping is added).\nCost: 6 Fatigue",
                    kind: FreeTimeDecisionKind.TrainingAction,
                    window: FreeTimeDecisionWindow.Training,
                    desiredFatigue: 6);
            }

            // Social (Free Time) - event-backed
            list.Add(new DecisionsMenuEntry
            {
                Id = "header_social",
                Text = "<span style=\"Link\">SOCIAL</span>",
                IsEnabled = true,
                // Bannerlord-native menu icon
                LeaveType = GameMenuOption.LeaveType.Conversation,
                OnSelected = a => ToggleDecisionsSection(DecisionsMenuSection.Social, a)
            });

            if (!_decisionsCollapsedSocial)
            {
                AddQueueEntry(list, decisionBehavior, enlistment,
                    id: "player_write_letter",
                    text: "Write a Letter Home",
                    tooltip: "{=enlisted_social_letter_tt}Spend time writing home. Costs fatigue but can steady morale over time.\nCost: 4 Fatigue",
                    kind: FreeTimeDecisionKind.Event,
                    window: FreeTimeDecisionWindow.Social,
                    desiredFatigue: 4);

                AddQueueEntry(list, decisionBehavior, enlistment,
                    id: "player_organize_dice_game",
                    text: "Organize a Dice Game",
                    tooltip: "{=enlisted_social_dice_tt}Pass the evening with your lance mates. Small risks, small rewards.\nCost: 4 Fatigue",
                    kind: FreeTimeDecisionKind.Event,
                    window: FreeTimeDecisionWindow.Social,
                    desiredFatigue: 4);

                AddQueueEntry(list, decisionBehavior, enlistment,
                    id: "player_petition_lord",
                    text: "Petition the Lord",
                    tooltip: "{=enlisted_social_petition_tt}Speak directly with your lord. Tier 3+.\nCost: 5 Fatigue",
                    kind: FreeTimeDecisionKind.Event,
                    window: FreeTimeDecisionWindow.Social,
                    desiredFatigue: 5,
                    extraEnabledCheck: () => (enlistment?.EnlistmentTier ?? 1) >= 3,
                    extraDisabledTooltip: "{=enlisted_social_petition_locked}You must be Tier 3 or higher to petition the lord.");
            }

            // Camp Life (conditional)
            list.Add(new DecisionsMenuEntry
            {
                Id = "header_camp",
                Text = "<span style=\"Link\">CAMP LIFE</span>" + NewTag(_decisionsNewCampLife),
                IsEnabled = true,
                // Bannerlord-native menu icon
                LeaveType = GameMenuOption.LeaveType.Manage,
                OnSelected = a => ToggleDecisionsSection(DecisionsMenuSection.CampLife, a)
            });

            if (!_decisionsCollapsedCampLife)
            {
                AddQueueEntry(list, decisionBehavior, enlistment,
                    id: "player_visit_wounded",
                    text: "Visit the Wounded",
                    tooltip: "{=enlisted_camp_wounded_tt}Only available if your company has wounded.\nCost: 4 Fatigue",
                    kind: FreeTimeDecisionKind.Event,
                    window: FreeTimeDecisionWindow.Social,
                    desiredFatigue: 4,
                    extraEnabledCheck: HasWoundedInCamp,
                    extraDisabledTooltip: "{=enlisted_camp_no_wounded}No wounded in camp right now.");
            }

            // Opportunities (events eligible right now)
            list.Add(new DecisionsMenuEntry
            {
                Id = "header_events",
                Text = "<span style=\"Link\">OPPORTUNITIES</span>" + NewTag(_decisionsNewOpportunities),
                IsEnabled = true,
                // Bannerlord-native menu icon
                LeaveType = GameMenuOption.LeaveType.WaitQuest,
                OnSelected = a => ToggleDecisionsSection(DecisionsMenuSection.Opportunities, a)
            });

            if (!_decisionsCollapsedOpportunities)
            {
                if (available == null || available.Count == 0)
                {
                    list.Add(new DecisionsMenuEntry
                    {
                        Id = "events_none",
                        Text = "    (none)",
                        IsEnabled = false,
                        LeaveType = (GameMenuOption.LeaveType)(-1)
                    });
                }
                else
                {
                    foreach (var evt in available)
                    {
                        if (evt == null)
                        {
                            continue;
                        }

                        list.Add(new DecisionsMenuEntry
                        {
                            Id = $"event_{evt.Id}",
                            Text = !string.IsNullOrWhiteSpace(evt.TitleFallback) ? evt.TitleFallback : evt.Id,
                            LeaveType = (GameMenuOption.LeaveType)(-1),
                            Tooltip = new TextObject(BuildDecisionTooltipStatic(evt)),
                            OnSelected = _ =>
                            {
                                if (decisionBehavior == null)
                                {
                                    return;
                                }

                                // Fire immediately (opportunity-style; not queued).
                                GameMenu.SwitchToMenu("enlisted_status");
                                decisionBehavior.FirePlayerDecision(evt.Id, onEventClosed: () =>
                                {
                                    try
                                    {
                                        if (Campaign.Current != null && EnlistmentBehavior.Instance?.IsEnlisted == true)
                                        {
                                            GameMenu.ActivateGameMenu("enlisted_status");
                                        }
                                    }
                                    catch
                                    {
                                        // ignore restore errors
                                    }
                                });
                            }
                        });
                    }
                }
            }

            return list;

            static bool HasWoundedInCamp()
            {
                try
                {
                    return MobileParty.MainParty?.MemberRoster?.TotalWounded > 0;
                }
                catch
                {
                    return false;
                }
            }
        }

        private static void AddQueueEntry(
            List<DecisionsMenuEntry> list,
            DecisionEventBehavior decisionBehavior,
            EnlistmentBehavior enlistment,
            string id,
            string text,
            string tooltip,
            FreeTimeDecisionKind kind,
            FreeTimeDecisionWindow window,
            int desiredFatigue,
            Func<bool> extraEnabledCheck = null,
            string extraDisabledTooltip = null)
        {
            var entry = new DecisionsMenuEntry
            {
                Id = id,
                Text = "    " + text, // Indent items under headers
                // Non-header: hide icon (using -1 as no sprite exists for it).
                LeaveType = (GameMenuOption.LeaveType)(-1),
                Tooltip = new TextObject(tooltip),
                OnSelected = _ =>
                {
                    if (decisionBehavior == null)
                    {
                        return;
                    }

                    if (decisionBehavior.TryQueueFreeTimeDecision(kind, id, window, desiredFatigue, out var msg))
                    {
                        // Small, non-blocking feedback.
                        InformationManager.DisplayMessage(new InformationMessage(msg.ToString()));
                    }
                    else
                    {
                        InformationManager.DisplayMessage(new InformationMessage(msg.ToString()));
                    }
                }
            };

            // Base fatigue gating.
            if (desiredFatigue > 0 && enlistment != null && enlistment.FatigueCurrent < desiredFatigue)
            {
                entry.IsEnabled = false;
                entry.Tooltip = new TextObject("{=enlisted_decisions_not_enough_fatigue}You are too exhausted for that right now.");
            }

            // Additional conditional gating.
            if (extraEnabledCheck != null && entry.IsEnabled)
            {
                if (!extraEnabledCheck())
                {
                    entry.IsEnabled = false;
                    if (!string.IsNullOrWhiteSpace(extraDisabledTooltip))
                    {
                        entry.Tooltip = new TextObject(extraDisabledTooltip);
                    }
                }
            }

            list.Add(entry);
        }

        private static string BuildDecisionTooltipStatic(LanceLifeEventDefinition decision)
        {
            // Copy of the old tooltip builder, but static (we can't reference instance methods here).
            if (decision == null)
            {
                return string.Empty;
            }

            var lines = new List<string>();

            var setup = !string.IsNullOrWhiteSpace(decision.SetupFallback)
                ? decision.SetupFallback
                : "Make a decision...";

            if (setup.Length > 150)
            {
                setup = setup.Substring(0, 147) + "...";
            }
            lines.Add(setup);
            lines.Add("");
            lines.Add("Status: Ready");

            var firstOption = decision.Options?.FirstOrDefault();
            if (firstOption?.Costs != null)
            {
                var costs = new List<string>();
                if (firstOption.Costs.Gold > 0)
                    costs.Add($"{firstOption.Costs.Gold} gold");
                if (firstOption.Costs.Fatigue > 0)
                    costs.Add($"{firstOption.Costs.Fatigue} fatigue");

                if (costs.Count > 0)
                {
                    lines.Add($"Cost: {string.Join(", ", costs)}");
                }
            }

            return string.Join("\n", lines);
        }

        /// <summary>
        /// Condition check for decisions menu (always true when enlisted).
        /// </summary>
        private bool OnDecisionsMenuCondition(MenuCallbackArgs args)
        {
            return EnlistmentBehavior.Instance?.IsEnlisted == true;
        }

        /// <summary>
        /// Tick handler for decisions menu (intentionally minimal).
        /// </summary>
        private void OnDecisionsMenuTick(MenuCallbackArgs args, CampaignTime dt)
        {
            // Intentionally empty - time mode is handled in menu init
        }

        /// <summary>
        /// Handler for "Back" option in decisions menu.
        /// </summary>
        private void OnDecisionsBackSelected(MenuCallbackArgs args)
        {
            QuartermasterManager.CaptureTimeStateBeforeMenuActivation();
            GameMenu.SwitchToMenu("enlisted_status");
        }

        /// <summary>
        /// Camp Hub submenu: Leave Service.
        /// Contains leaving actions that would otherwise clutter the main enlisted_status menu.
        /// </summary>
        private void RegisterLeaveServiceMenu(CampaignGameStarter starter)
        {
            starter.AddWaitGameMenu(LeaveServiceMenuId,
                "{=Enlisted_LeaveService_Title}— LEAVE SERVICE —\n{LEAVE_SERVICE_TEXT}",
                OnLeaveServiceInit,
                _ => EnlistmentBehavior.Instance?.IsEnlisted == true,
                null,
                (_, __) => { },
                GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption);

            starter.AddGameMenuOption(LeaveServiceMenuId, "leave_service_back",
                "{=Enlisted_LeaveService_Back}Back",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    return true;
                },
                _ =>
                {
                    QuartermasterManager.CaptureTimeStateBeforeMenuActivation();
                    GameMenu.SwitchToMenu("enlisted_status");
                },
                false, 0);

            // Ask for temporary leave (does not discharge/desert)
            starter.AddGameMenuOption(LeaveServiceMenuId, "leave_service_ask_leave",
                "{=Enlisted_Menu_AskLeave}Ask for Leave",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    var enlistment = EnlistmentBehavior.Instance;
                    if (enlistment?.IsOnLeave == true)
                    {
                        args.IsEnabled = false;
                        args.Tooltip = new TextObject("{=menu_disabled_on_leave}You are already on leave.");
                        return true;
                    }

                    // Reuse the existing cooldown messaging.
                    return IsAskLeaveAvailable(args);
                },
                OnAskLeaveSelected,
                false, 1);

            // Leave without penalty (free desertion) - PayTension gated
            starter.AddGameMenuOption(LeaveServiceMenuId, "leave_service_free_desertion",
                "{=Enlisted_Menu_FreeDesert}Leave Without Penalty",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    var enlistment = EnlistmentBehavior.Instance;
                    if ((enlistment?.PayTension ?? 0) < 60)
                    {
                        args.IsEnabled = false;
                        args.Tooltip = new TextObject("{=menu_disabled_pay_ok}Pay must be severely delayed (60+ tension) to leave without penalty.");
                        return true;
                    }
                    args.Tooltip = new TextObject("{=menu_tooltip_free_desert}Pay is too late. You can leave with no penalties — no one would blame you.");
                    return true;
                },
                OnFreeDesertionSelected,
                false, 2);

            // Desert Army (penalty) -> confirm
            starter.AddGameMenuOption(LeaveServiceMenuId, "leave_service_desert_army",
                "{=Enlisted_Menu_DesertArmy}Desert the Army",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Escape;
                    args.Tooltip = new TextObject("{=menu_tooltip_desert}Abandon your post. WARNING: Severe relation and crime penalties!");
                    return IsDesertArmyAvailable(args);
                },
                OnDesertArmySelected,
                false, 3);

            // Request Discharge (final muster) / Cancel (toggle)
            starter.AddGameMenuOption(LeaveServiceMenuId, "leave_service_request_discharge",
                "{LEAVE_SERVICE_DISCHARGE_TEXT}",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                    var enlistment = EnlistmentBehavior.Instance;
                    if (enlistment?.IsEnlisted != true)
                    {
                        return false;
                    }

                    if (enlistment.IsPendingDischarge)
                    {
                        MBTextManager.SetTextVariable("LEAVE_SERVICE_DISCHARGE_TEXT",
                            new TextObject("{=ct_cancel_discharge_short}Cancel Discharge Request"));
                        args.Tooltip = new TextObject("{=ct_cancel_discharge_tooltip}Withdraw your discharge request and remain in service.");
                        return true;
                    }

                    MBTextManager.SetTextVariable("LEAVE_SERVICE_DISCHARGE_TEXT",
                        new TextObject("{=ct_request_discharge_short}Request Discharge"));
                    args.Tooltip = new TextObject("{=ct_discharge_tooltip}Request formal discharge with final pay settlement (resolves at next muster).");
                    return true;
                },
                _ => OnLeaveServiceDischargeSelected(),
                false, 4);
        }

        private void OnLeaveServiceInit(MenuCallbackArgs args)
        {
            try
            {
                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment?.IsEnlisted != true)
                {
                    MBTextManager.SetTextVariable("LEAVE_SERVICE_TEXT",
                        new TextObject("{=Enlisted_Status_NotEnlisted}You are not currently enlisted."));
                    return;
                }

                var sb = new StringBuilder();
                sb.AppendLine("These actions remove you from service.");
                sb.AppendLine();
                sb.AppendLine($"Pay Tension: {enlistment.PayTension}/100");
                sb.AppendLine($"Pending Discharge: {(enlistment.IsPendingDischarge ? "Yes" : "No")}");
                MBTextManager.SetTextVariable("LEAVE_SERVICE_TEXT", sb.ToString().TrimEnd());
            }
            catch
            {
                MBTextManager.SetTextVariable("LEAVE_SERVICE_TEXT", "These actions remove you from service.");
            }
        }

        private void OnLeaveServiceDischargeSelected()
        {
            try
            {
                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment?.IsEnlisted != true)
                {
                    return;
                }

                if (enlistment.IsPendingDischarge)
                {
                    if (enlistment.CancelDischarge())
                    {
                        InformationManager.DisplayMessage(new InformationMessage(
                            new TextObject("{=ct_discharge_cancelled}Pending discharge cancelled.").ToString()));
                        QuartermasterManager.CaptureTimeStateBeforeMenuActivation();
                        GameMenu.SwitchToMenu(LeaveServiceMenuId);
                    }
                    return;
                }

                InformationManager.ShowInquiry(new InquiryData(
                    new TextObject("{=ct_request_discharge_confirm_title}Request Discharge").ToString(),
                    new TextObject("{=ct_request_discharge_confirm_body}Request discharge now? It will resolve at the next pay muster.").ToString(),
                    true,
                    true,
                    new TextObject("{=ct_yes}Yes").ToString(),
                    new TextObject("{=ct_no}No").ToString(),
                    () =>
                    {
                        if (enlistment.RequestDischarge())
                        {
                            InformationManager.DisplayMessage(new InformationMessage(
                                new TextObject("{=ct_discharge_requested}Discharge requested. It will resolve at the next pay muster.").ToString()));
                            QuartermasterManager.CaptureTimeStateBeforeMenuActivation();
                            GameMenu.SwitchToMenu(LeaveServiceMenuId);
                        }
                    },
                    () => { }));
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("Interface", "E-UI-034", "Error in Leave Service discharge option", ex);
            }
        }

        /// <summary>
        /// Check if a decision slot is available and should be shown.
        /// </summary>
        private bool IsDecisionSlotAvailable(MenuCallbackArgs args, int slotIndex)
        {
            if (slotIndex >= _cachedDecisionsMenuEntries.Count)
            {
                return false; // No decision in this slot
            }

            var entry = _cachedDecisionsMenuEntries[slotIndex];
            if (entry == null || !entry.IsVisible)
            {
                return false;
            }

            args.optionLeaveType = entry.LeaveType;
            args.IsEnabled = entry.IsEnabled;
            if (entry.Tooltip != null)
            {
                args.Tooltip = entry.Tooltip;
            }

            return true;
        }

        /// <summary>
        /// Handler when a decision slot is selected.
        /// Opens the full event screen for that decision.
        /// </summary>
        private void OnDecisionSlotSelected(MenuCallbackArgs args, int slotIndex)
        {
            try
            {
                if (slotIndex >= _cachedDecisionsMenuEntries.Count)
                {
                    return;
                }

                var entry = _cachedDecisionsMenuEntries[slotIndex];
                if (entry?.OnSelected == null)
                {
                    return;
                }

                entry.OnSelected(args);
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("Interface", "E-UI-035", $"Failed to select decision slot {slotIndex}", ex);
            }
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
