using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
// Removed: using Enlisted.Features.Camp.UI.Bulletin; (old Bulletin UI deleted)
using Enlisted.Features.Company;
using Enlisted.Features.Conditions;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Equipment.Behaviors;
using Enlisted.Features.Escalation;
using Enlisted.Features.Interface.News.Models;
using Enlisted.Features.Orders.Behaviors;
using Enlisted.Features.Content;
using Enlisted.Mod.Core;
using Enlisted.Mod.Core.Logging;
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
        ///     Public accessor for checking if the player has explicitly visited a settlement.
        ///     Used by GenericStateMenuPatch to prevent auto-opening settlement menus when just paused at a settlement.
        /// </summary>
        public static bool HasExplicitlyVisitedSettlement { get; private set; }

        // Orders accordion state for the main enlisted status menu.
        // We keep this simple: one active order can be expanded/collapsed via a header entry.
        // When a new order arrives, the section auto-expands.
        private bool _ordersCollapsed = true;
        private string _ordersLastSeenOrderId = string.Empty;

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
                        null),
                    new InquiryElement(
                        "force_event",
                        new TextObject("{=Enlisted_Debug_ForceEvent}Force Event Selection").ToString(),
                        null),
                    new InquiryElement(
                        "reset_window",
                        new TextObject("{=Enlisted_Debug_ResetWindow}Reset Event Window").ToString(),
                        null),
                    new InquiryElement(
                        "list_events",
                        new TextObject("{=Enlisted_Debug_ListEvents}List Eligible Events").ToString(),
                        null),
                    new InquiryElement(
                        "clear_cooldowns",
                        new TextObject("{=Enlisted_Debug_ClearCooldowns}Clear Event Cooldowns").ToString(),
                        null),
                    new InquiryElement(
                        "pacing_info",
                        new TextObject("{=Enlisted_Debug_PacingInfo}Show Event Pacing Info").ToString(),
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
                                case "force_event":
                                    Debugging.Behaviors.DebugToolsBehavior.ForceEventSelection();
                                    break;
                                case "reset_window":
                                    Debugging.Behaviors.DebugToolsBehavior.ResetEventWindow();
                                    break;
                                case "list_events":
                                    Debugging.Behaviors.DebugToolsBehavior.ListEligibleEvents();
                                    break;
                                case "clear_cooldowns":
                                    Debugging.Behaviors.DebugToolsBehavior.ClearEventCooldowns();
                                    break;
                                case "pacing_info":
                                    Debugging.Behaviors.DebugToolsBehavior.ShowEventPacingInfo();
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
        public static void OnEnlistedStatusBackgroundInit(MenuCallbackArgs args)
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
        public static void OnDecisionsBackgroundInit(MenuCallbackArgs args)
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
        public static void OnDesertConfirmBackgroundInit(MenuCallbackArgs args)
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

            // Clear captured time state when exiting enlisted menu system entirely.
            // This ensures next time we enter an enlisted menu, we capture the player's fresh time state.
            var wasEnlistedMenu = previousMenu?.StartsWith("enlisted_") == true;
            var isEnlistedMenu = _currentMenuId?.StartsWith("enlisted_") == true;
            if (wasEnlistedMenu && !isEnlistedMenu)
            {
                QuartermasterManager.CapturedTimeMode = null;
                ModLogger.Debug("Interface", "Cleared captured time state - exited enlisted menu system");
            }

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
            RegisterDecisionsMenu(starter);
            RegisterReportsMenu(starter);
            RegisterStatusMenu(starter);

            // Add direct siege battle option to enlisted menu as fallback
            ModLogger.Info("Interface", "Adding emergency siege battle option to enlisted_status menu");
            try
            {
                starter.AddGameMenuOption("enlisted_status", "emergency_siege_battle",
                    "{=Enlisted_Menu_JoinSiege}Join siege battle",
                    IsEmergencySiegeBattleAvailable,
                    OnEmergencySiegeBattleSelected,
                    false, 7);
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("Interface", "E-UI-005", "Failed to add emergency siege battle option", ex);
            }

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

            // Main menu options for enlisted status menu
            // IMPORTANT: Options appear in the order they are added to the menu. Index is secondary.
            // Desired order: Orders → Decisions → Camp → Reports → Status → Debug

            // 1. Orders accordion header (always visible).
            // When a new order arrives, it auto-expands and shows a [NEW] marker for the day.
            starter.AddGameMenuOption("enlisted_status", "enlisted_orders_header",
                "{ORDERS_HEADER_TEXT}",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.WaitQuest;

                    var currentOrder = OrderManager.Instance?.GetCurrentOrder();
                    if (currentOrder == null)
                    {
                        _ordersCollapsed = true;
                        _ordersLastSeenOrderId = string.Empty;
                    }
                    else
                    {
                        var currentId = currentOrder.Id ?? string.Empty;
                        var isNewOrder = string.IsNullOrEmpty(_ordersLastSeenOrderId) ||
                                         !string.Equals(_ordersLastSeenOrderId, currentId, StringComparison.OrdinalIgnoreCase);
                        if (isNewOrder)
                        {
                            _ordersLastSeenOrderId = currentId;
                            _ordersCollapsed = false; // Auto-expand when the order changes
                        }
                    }

                    var headerText = "<span style=\"Link\">ORDERS</span>";
                    if (currentOrder != null)
                    {
                        var daysAgo = (int)(CampaignTime.Now - currentOrder.IssuedTime).ToDays;
                        if (daysAgo == 0)
                        {
                            headerText += " <span style=\"Link\">[NEW]</span>";
                        }

                        args.Tooltip = new TextObject("{=enlisted_orders_tooltip_pending}You have a pending order from {ISSUER}.");
                        args.Tooltip.SetTextVariable("ISSUER", currentOrder.Issuer);
                    }
                    else
                    {
                        args.Tooltip = new TextObject("{=enlisted_orders_tooltip_none}No active orders at this time.");
                    }

                    MBTextManager.SetTextVariable("ORDERS_HEADER_TEXT", headerText);
                    return true;
                },
                ToggleOrdersAccordion,
                false, 1);

            // 1b. Active order row (visible only when expanded and an order exists).
            // Player clicks this row to view details and Accept/Decline.
            starter.AddGameMenuOption("enlisted_status", "enlisted_active_order",
                "{ACTIVE_ORDER_TEXT}",
                args =>
                {
                    var currentOrder = OrderManager.Instance?.GetCurrentOrder();
                    if (currentOrder == null || _ordersCollapsed)
                    {
                        return false;
                    }

                    args.optionLeaveType = GameMenuOption.LeaveType.Mission;

                    var row = $"    {currentOrder.Title}";
                    if (!string.IsNullOrWhiteSpace(currentOrder.Issuer))
                    {
                        row = $"    From {currentOrder.Issuer}: {currentOrder.Title}";
                    }

                    var daysAgo = (int)(CampaignTime.Now - currentOrder.IssuedTime).ToDays;
                    if (daysAgo == 0)
                    {
                        row += " <span style=\"Link\">[NEW]</span>";
                    }

                    MBTextManager.SetTextVariable("ACTIVE_ORDER_TEXT", row);
                    args.Tooltip = new TextObject("View the order details and respond.");
                    return true;
                },
                _ => ShowOrdersMenu(),
                false, 2);

            // 2. Decisions
            starter.AddGameMenuOption("enlisted_status", "enlisted_decisions_entry",
                "{=enlisted_decisions_entry}Decisions",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                    args.Tooltip = new TextObject("{=enlisted_decisions_tooltip}Review and act on decisions.");
                    return true;
                },
                _ =>
                {
                    QuartermasterManager.CaptureTimeStateBeforeMenuActivation();
                    GameMenu.SwitchToMenu("enlisted_decisions");
                },
                false, 3);

            // 3. Camp hub
            starter.AddGameMenuOption("enlisted_status", "enlisted_camp_hub",
                "{=enlisted_camp}Camp",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                    args.Tooltip = new TextObject("{=enlisted_camp_tooltip}Rest, train, manage equipment, and visit the medical tent.");
                    return true;
                },
                _ =>
                {
                    QuartermasterManager.CaptureTimeStateBeforeMenuActivation();
                    GameMenu.SwitchToMenu(CampHubMenuId);
                },
                false, 4);

            // 4. Reports
            starter.AddGameMenuOption("enlisted_status", "enlisted_reports_entry",
                "{=enlisted_reports}Reports",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                    args.Tooltip = new TextObject("{=enlisted_reports_tooltip}View daily brief, company status, and campaign context.");
                    return true;
                },
                _ =>
                {
                    QuartermasterManager.CaptureTimeStateBeforeMenuActivation();
                    GameMenu.SwitchToMenu("enlisted_reports");
                },
                false, 5);

            // 5. Status (detailed)
            starter.AddGameMenuOption("enlisted_status", "enlisted_status_detail",
                "{=enlisted_status_detail}Status",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                    args.Tooltip = new TextObject("{=enlisted_status_detail_tooltip}View detailed rank, reputation, and traits.");
                    return true;
                },
                _ =>
                {
                    QuartermasterManager.CaptureTimeStateBeforeMenuActivation();
                    GameMenu.SwitchToMenu("enlisted_status_detail_view");
                },
                false, 6);

            // 6. Debug tools (QA only): grant gold/XP - at bottom
            starter.AddGameMenuOption("enlisted_status", "enlisted_debug_tools",
                "{=Enlisted_Menu_DebugTools}Debug Tools",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                    args.Tooltip = new TextObject("{=menu_tooltip_debug}Grant gold or enlistment XP for testing.");
                    return true;
                },
                OnDebugToolsSelected,
                false, 7);

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
                false, 100);
#pragma warning restore CS0618

            // NOTE: My Lance and Camp Management have been moved to Camp Hub.
            // These are accessible via Camp → Camp Management.

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
                false, 8);

            // NOTE: Duties, Medical Attention, and Service Records are all accessed via Camp Hub.
            // Keeping the main menu lean - only essential/frequent actions here.

            // === LEAVE OPTIONS (grouped at bottom) ===

            // No "return to duties" option needed - player IS doing duties by being in this menu

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
                    HasExplicitlyVisitedSettlement = false;
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


        private void RegisterDecisionsMenu(CampaignGameStarter starter)
        {
            starter.AddWaitGameMenu("enlisted_decisions",
                "{=Enlisted_Menu_Decisions_Title}— DECISIONS —\n{DECISIONS_STATUS_TEXT}",
                OnDecisionsMenuInit,
                OnDecisionsMenuCondition,
                null,
                OnDecisionsMenuTick,
                GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption);

            // Create 40 decision slots (Back button is now part of the dynamic list)
            for (var i = 0; i < 40; i++)
            {
                var slotIndex = i;
                starter.AddGameMenuOption("enlisted_decisions", $"decision_slot_{i}",
                    $"{{DECISION_SLOT_{i}_TEXT}}",
                    args => IsDecisionSlotAvailable(args, slotIndex),
                    args => OnDecisionSlotSelected(args, slotIndex),
                    false, i + 1);
            }
        }

        private void RegisterReportsMenu(CampaignGameStarter starter)
        {
            starter.AddWaitGameMenu("enlisted_reports",
                "{=enlisted_reports_title}— REPORTS —\n{REPORTS_TEXT}",
                OnReportsMenuInit,
                _ => EnlistmentBehavior.Instance?.IsEnlisted == true,
                null,
                NoopMenuTick,
                GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption);

            starter.AddGameMenuOption("enlisted_reports", "reports_back",
                "{=Enlisted_Menu_BackToStatus}Back",
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
                false, 1);
        }

        private void RegisterStatusMenu(CampaignGameStarter starter)
        {
            starter.AddWaitGameMenu("enlisted_status_detail_view",
                "{=enlisted_status_detail_title}— STATUS —\n{STATUS_DETAIL_TEXT}",
                OnStatusDetailMenuInit,
                _ => EnlistmentBehavior.Instance?.IsEnlisted == true,
                null,
                NoopMenuTick,
                GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption);

            starter.AddGameMenuOption("enlisted_status_detail_view", "status_detail_back",
                "{=Enlisted_Menu_BackToStatus}Back",
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
                false, 1);
        }

        private void RegisterCampHubMenu(CampaignGameStarter starter)
        {
            starter.AddWaitGameMenu(CampHubMenuId,
                "{=enlisted_camp_hub_title}{CAMP_HUB_TEXT}",
                OnCampHubInit,
                args =>
                {
                    _ = args;
                    return EnlistmentBehavior.Instance?.IsEnlisted == true;
                },
                null,
                OnCampHubTick,
                GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption);

            // NOTE: Rest & Recover, Train Skills, and Morale Boost moved to Decisions menu.

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
                false, 1);

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
                false, 2);

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
                false, 3);

            // Visit Quartermaster - blocked when company supply is critically low (below 15%)
            starter.AddGameMenuOption(CampHubMenuId, "camp_hub_quartermaster",
                "{=Enlisted_Menu_VisitQuartermaster}Visit Quartermaster",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Trade;

                    // Supply gate: block equipment access when supplies are critically low (below 15%)
                    const int criticalSupplyThreshold = 15;
                    var companyNeeds = EnlistmentBehavior.Instance?.CompanyNeeds;
                    if (companyNeeds != null && companyNeeds.Supplies < criticalSupplyThreshold)
                    {
                        args.IsEnabled = false;
                        args.Tooltip = new TextObject("{=menu_qm_supply_blocked}Quartermaster unavailable. The company's supplies are critically low. Equipment requisitions are suspended until supply levels recover.");
                        ModLogger.Debug("Interface", $"Quartermaster blocked: Supplies at {companyNeeds.Supplies}% (threshold: {criticalSupplyThreshold}%)");
                        return true;
                    }

                    args.Tooltip = new TextObject("{=menu_tooltip_quartermaster}Purchase equipment for your formation and rank. Newly unlocked items marked [NEW].");
                    return true;
                },
                OnQuartermasterSelected,
                false, 4);

            // Medical Tent
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
                false, 5);

            // My Lord... - conversation with the current lord (Conversation icon)
            starter.AddGameMenuOption(CampHubMenuId, "camp_hub_talk_to_lord",
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
                false, 6);

            // Leave / Discharge / Desert (always shown; eligibility varies)
            starter.AddGameMenuOption(CampHubMenuId, "enlisted_leave_service_entry",
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
                false, 99);

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

        private static void NoopMenuTick(MenuCallbackArgs args, CampaignTime dt)
        {
            _ = args;
            _ = dt;
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

                var sb = new StringBuilder();

                // Add camp news section if there are notable items to report
                var campNews = BuildCampNewsSection(enlistment);
                if (!string.IsNullOrWhiteSpace(campNews))
                {
                    sb.AppendLine(campNews);
                    sb.AppendLine();
                }

                // Add RP atmosphere line based on current context
                var atmosphere = BuildCampAtmosphereLine(lord);
                if (!string.IsNullOrWhiteSpace(atmosphere))
                {
                    sb.AppendLine(atmosphere);
                    sb.AppendLine();
                }

                var lordLine = new TextObject("{=enl_camp_hub_lord_line}Lord: {LORD_NAME}");
                lordLine.SetTextVariable("LORD_NAME", lordName);
                sb.AppendLine(lordLine.ToString());

                var rankLine = new TextObject("{=enl_camp_hub_rank_line}Your Rank: {RANK} (T{TIER})");
                rankLine.SetTextVariable("RANK", rank);
                rankLine.SetTextVariable("TIER", enlistment.EnlistmentTier);
                sb.AppendLine(rankLine.ToString());

                var workLine = new TextObject("{=enl_camp_hub_objective_line}Lord's Work: {OBJECTIVE}");
                workLine.SetTextVariable("OBJECTIVE", objective);
                sb.AppendLine(workLine.ToString());

                return sb.ToString().TrimEnd();
            }
            catch
            {
                return new TextObject("{=enl_camp_hub_unavailable}Service Status unavailable.").ToString();
            }
        }

        /// <summary>
        /// Builds the camp news section containing notable updates for the player.
        /// Includes company health (wounded, sick, casualties), supply status, morale, and upcoming events.
        /// Returns empty string when there's nothing notable to report.
        /// </summary>
        private static string BuildCampNewsSection(EnlistmentBehavior enlistment)
        {
            try
            {
                var newsItems = new List<string>();
                var lord = enlistment?.CurrentLord;
                var lordParty = lord?.PartyBelongedTo;

                // Company health: wounded soldiers currently in the party
                if (lordParty?.MemberRoster != null)
                {
                    var totalWounded = lordParty.MemberRoster.TotalWounded;
                    var totalTroops = lordParty.MemberRoster.TotalManCount;
                    
                    if (totalWounded > 0 && totalTroops > 0)
                    {
                        var woundedPercent = (totalWounded * 100) / totalTroops;
                        if (woundedPercent >= 30)
                        {
                            newsItems.Add($"Many wounded in camp ({totalWounded} soldiers recovering)");
                        }
                        else if (woundedPercent >= 15)
                        {
                            newsItems.Add($"Wounded soldiers in the medical tent ({totalWounded})");
                        }
                        else if (totalWounded >= 5)
                        {
                            newsItems.Add($"{totalWounded} wounded being tended to");
                        }
                    }
                }

                // Casualty and sickness summary since last muster (resets each pay cycle)
                var news = EnlistedNewsBehavior.Instance;
                if (news != null)
                {
                    // Losses since last muster (resets when pay is resolved)
                    var lostSinceMuster = news.LostSinceLastMuster;
                    if (lostSinceMuster > 0)
                    {
                        var lossText = lostSinceMuster == 1 
                            ? "1 soldier lost since last muster" 
                            : $"{lostSinceMuster} soldiers lost since last muster";
                        newsItems.Add($"{lossText}");
                    }
                    
                    // Sickness since last muster
                    var sickSinceMuster = news.SickSinceLastMuster;
                    if (sickSinceMuster >= 5)
                    {
                        newsItems.Add("Sickness spreading through the camp");
                    }
                    else if (sickSinceMuster >= 2)
                    {
                        newsItems.Add("Some soldiers have fallen ill");
                    }
                }

                // Today's snapshot for morale and food status
                var snapshot = news?.GetTodayDailyReportSnapshot();
                if (snapshot != null)
                {
                    // Morale status (only show if not steady/normal)
                    switch (snapshot.Morale)
                    {
                        case MoraleBand.Breaking:
                            newsItems.Add("Morale is dangerously low");
                            break;
                        case MoraleBand.Low:
                            newsItems.Add("Spirits are flagging among the troops");
                            break;
                        case MoraleBand.High:
                            newsItems.Add("The company's spirits are high");
                            break;
                    }
                    
                    // Food status (only show if problematic)
                    switch (snapshot.Food)
                    {
                        case FoodBand.Critical:
                            newsItems.Add("Food stores nearly exhausted");
                            break;
                        case FoodBand.Low:
                            newsItems.Add("Rations are running low");
                            break;
                        case FoodBand.Thin:
                            newsItems.Add("Food supplies are thin");
                            break;
                    }
                }

                // Supply level and quartermaster stock
                var companyNeeds = enlistment?.CompanyNeeds;
                var qm = QuartermasterManager.Instance;

                if (companyNeeds != null && qm != null)
                {
                    var supplyLevel = companyNeeds.Supplies;
                    var outOfStockCount = qm.OutOfStockCount;

                    // Critical supply warning (below 15% - QM is blocked)
                    if (supplyLevel < 15)
                    {
                        newsItems.Add($"Supplies critical ({supplyLevel}%) — Quartermaster closed");
                    }
                    // Low supply with stock shortages
                    else if (supplyLevel < 40 && outOfStockCount > 0)
                    {
                        newsItems.Add($"Supply shortage — {outOfStockCount} items out of stock");
                    }
                    // Moderate supply with some shortages
                    else if (supplyLevel < 60 && outOfStockCount > 0)
                    {
                        newsItems.Add($"Limited quartermaster stock ({outOfStockCount} items unavailable)");
                    }
                }

                // Check days until next muster (only show if within 2 days)
                var nextPayday = enlistment?.NextPaydaySafe ?? CampaignTime.Never;
                if (nextPayday != CampaignTime.Never && nextPayday != CampaignTime.Zero)
                {
                    var daysUntilMuster = (nextPayday - CampaignTime.Now).ToDays;
                    if (daysUntilMuster <= 2 && daysUntilMuster > 0)
                    {
                        var dayText = daysUntilMuster <= 1 ? "tomorrow" : "in 2 days";
                        newsItems.Add($"Muster {dayText}");
                    }
                    else if (daysUntilMuster <= 0 && enlistment?.IsPayMusterPending == true)
                    {
                        newsItems.Add("Muster awaiting your attention");
                    }
                }

                // Check for pay tension (owed backpay)
                var owedBackpay = enlistment?.OwedBackpay ?? 0;
                if (owedBackpay > 0)
                {
                    newsItems.Add($"Owed backpay: {owedBackpay} denars");
                }

                // If no notable news, show a neutral status
                if (newsItems.Count == 0)
                {
                    return "— Camp News —\nAll quiet in camp. No urgent matters.";
                }

                // Build the news section
                var sb = new StringBuilder();
                sb.AppendLine("— Camp News —");
                foreach (var item in newsItems)
                {
                    sb.AppendLine(item);
                }

                return sb.ToString().TrimEnd();
            }
            catch (Exception ex)
            {
                ModLogger.Debug("Interface", $"Error building camp news: {ex.Message}");
                return string.Empty;
            }
        }
        
        private static readonly Random CampAtmoRng = new Random();
        
        private static string BuildCampAtmosphereLine(Hero lord)
        {
            try
            {
                var hour = CampaignTime.Now.GetHourOfDay;
                var party = lord?.PartyBelongedTo;
                
                // Check for recent battle (within 24 hours)
                var news = EnlistedNewsBehavior.Instance;
                if (news != null)
                {
                    if (news.TryGetLastPlayerBattleSummary(out var lastBattleTime, out var playerWon) &&
                        lastBattleTime != CampaignTime.Zero)
                    {
                        var hoursSinceBattle = (CampaignTime.Now - lastBattleTime).ToHours;
                        if (hoursSinceBattle < 24)
                        {
                            return playerWon 
                                ? PickRandom(BattleWonAtmoLines) 
                                : PickRandom(BattleLostAtmoLines);
                        }
                    }
                }
                
                // Context-based atmosphere
                if (party?.Party?.SiegeEvent != null || party?.BesiegerCamp != null)
                {
                    return PickRandom(SiegeAtmoLines);
                }
                
                if (party?.Army != null)
                {
                    return PickRandom(ArmyAtmoLines);
                }
                
                if (party?.CurrentSettlement != null)
                {
                    return PickRandom(SettlementAtmoLines);
                }
                
                // Default: on the march, time-based
                if (hour < 6 || hour >= 20)
                {
                    return PickRandom(NightAtmoLines);
                }
                if (hour < 10)
                {
                    return PickRandom(MorningAtmoLines);
                }
                if (hour >= 17)
                {
                    return PickRandom(EveningAtmoLines);
                }
                
                return PickRandom(DayAtmoLines);
            }
            catch
            {
                return "The company goes about its duties.";
            }
        }
        
        private static string PickRandom(string[] lines)
        {
            return lines[CampAtmoRng.Next(lines.Length)];
        }
        
        // Atmosphere lines for different contexts
        private static readonly string[] BattleWonAtmoLines =
        {
            "Spirits are high after the victory. Men share stories around the fires.",
            "The camp buzzes with the energy of triumph. Wounds are tended, tales told.",
            "Victory songs drift through the camp. The company earned this rest."
        };
        
        private static readonly string[] BattleLostAtmoLines =
        {
            "A somber mood hangs over the camp. The cost was high.",
            "Quiet conversations and grim faces. The company licks its wounds.",
            "Few words are spoken. The men tend to their gear and their thoughts."
        };
        
        private static readonly string[] SiegeAtmoLines =
        {
            "The siege works stretch before the walls. Engineers shout orders.",
            "Smoke rises from siege preparations. The walls loom in the distance.",
            "The camp sprawls around the besieged fortification. Tension fills the air."
        };
        
        private static readonly string[] ArmyAtmoLines =
        {
            "The army camp stretches in every direction. Banners flutter in the wind.",
            "Thousands of soldiers go about their duties. The army is a city on the move.",
            "Lords and their retinues mingle. The gathered host is an impressive sight."
        };
        
        private static readonly string[] SettlementAtmoLines =
        {
            "The company has made camp near the settlement. Soldiers come and go.",
            "Market sounds drift from nearby. A welcome respite from the march.",
            "The settlement provides a backdrop to camp life. Rest comes easier here."
        };
        
        private static readonly string[] NightAtmoLines =
        {
            "The fires burn low as men settle in for the night.",
            "Sentries patrol the perimeter. The camp sleeps under the stars.",
            "Night has fallen. Quiet conversations drift from the watch fires."
        };
        
        private static readonly string[] MorningAtmoLines =
        {
            "Dawn breaks over the camp. Men stir and prepare for the day.",
            "The morning muster begins. Sergeants call out orders.",
            "Cook fires crackle to life. The smell of breakfast fills the air."
        };
        
        private static readonly string[] EveningAtmoLines =
        {
            "The day's march is done. Men gather around the evening fires.",
            "Dusk settles over the camp. The company prepares for night.",
            "Evening rations are distributed. Tired soldiers find their rest."
        };
        
        private static readonly string[] DayAtmoLines =
        {
            "The camp is alive with activity. Soldiers drill and maintain gear.",
            "Midday sun beats down on the tents. The company goes about its work.",
            "Another day on campaign. The rhythms of camp life continue."
        };

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
                // Phase 1: Duties system deleted

                if (lord == null)
                {
                    MBTextManager.SetTextVariable("ENLISTED_STATUS_TEXT",
                        new TextObject("{=Enlisted_Status_ErrorNoLord}Error: No enlisted lord found."));
                    return;
                }

                var statusContent = BuildCompactEnlistedStatusText(enlistment);

                // Set text variables for menu display (lord guaranteed non-null from earlier check)
                var lordName = lord.EncyclopediaLinkWithName?.ToString() ?? lord.Name?.ToString() ?? "Unknown";
                
                // Get player status flavor text from news behavior
                var playerStatus = EnlistedNewsBehavior.Instance?.BuildPlayerStatusLine(enlistment);
                if (string.IsNullOrWhiteSpace(playerStatus))
                {
                    playerStatus = "Ready for duty.";
                }
                var rank = "Unknown";
                try
                {
                    rank = Ranks.RankHelper.GetCurrentRank(enlistment);
                }
                catch
                {
                    /* best-effort */
                }
                
                // Note: The menu template already prefixes the first line with "Lord:".
                // PARTY_LEADER should start with the lord name, not "Lord:" again.
                // We end with a newline so there is a visible gap before the report sections.
                var leaderSummary =
                    $"{lordName}\n" +
                    $"Your Rank: {rank} (T{enlistment.EnlistmentTier})\n" +
                    $"Your Status: {playerStatus}\n";

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

        private static string BuildCompactEnlistedStatusText(EnlistmentBehavior enlistment)
        {
            try
            {
                var sb = new StringBuilder();

                // COMPANY REPORT section: Daily brief with company/player/kingdom context
                var dailyBrief = EnlistedNewsBehavior.Instance?.BuildDailyBriefSection();
                sb.AppendLine("_____ COMPANY REPORT _____");
                sb.AppendLine(!string.IsNullOrWhiteSpace(dailyBrief) ? dailyBrief : "No report available.");
                sb.AppendLine();

                // RECENT ACTIONS section: Personal feed items (battles, orders, reputation changes)
                var personalFeed = EnlistedNewsBehavior.Instance?.GetVisiblePersonalFeedItems(3);
                sb.AppendLine("_____ RECENT ACTIONS _____");
                if (personalFeed == null || personalFeed.Count == 0)
                {
                    sb.AppendLine("• Nothing notable to report.");
                }
                else
                {
                    var wroteAny = false;
                    foreach (var item in personalFeed)
                    {
                        var formattedItem = EnlistedNewsBehavior.FormatDispatchForDisplay(item);
                        if (!string.IsNullOrWhiteSpace(formattedItem))
                        {
                            wroteAny = true;
                            sb.AppendLine($"• {formattedItem}");
                        }
                    }

                    if (!wroteAny)
                    {
                        sb.AppendLine("• Nothing notable to report.");
                    }
                }
                sb.AppendLine();

                // Company status line: Only shown when actionable (high logistics, low morale, or pay tension)
                var companyLine = TryBuildCompanyStatusLine(enlistment);
                if (!string.IsNullOrWhiteSpace(companyLine))
                {
                    sb.AppendLine(companyLine);
                    sb.AppendLine();
                }

                return sb.ToString().TrimEnd();
            }
            catch
            {
                return "Status unavailable.";
            }
        }

        private static string TryBuildCompanyStatusLine(EnlistmentBehavior enlistment)
        {
            try
            {
                var campLife = Camp.CampLifeBehavior.Instance;
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

        // Note: Removed unused utility methods: CalculateServiceDays, GetRankName, GetFormationDisplayInfo, 
        // GetServiceDays, GetRetirementCountdown - kept for reference in git history

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
        /// <summary>
        /// Kept for save compatibility. This handler is obsolete and should not be reachable in normal play.
        /// </summary>
        [Obsolete("Formation is now chosen via proving events, not the Master at Arms menu.")]
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

        // IsMyLanceAvailable removed - lance access now via Visit Camp > Lance location

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
        ///     Fallback handler when quartermaster conversation cannot be opened.
        ///     The GameMenu equipment system was removed in favor of conversation-driven Gauntlet UI.
        /// </summary>
        private void OpenQuartermasterMenuDirectly()
        {
            // The old GameMenu-based quartermaster system has been removed.
            // Equipment access is now conversation-driven with Gauntlet UI.
            // This fallback should not be reached in normal operation.
            ModLogger.ErrorCode("Quartermaster", "E-QM-025", 
                "Cannot open QM: conversation failed and no fallback available");
            InformationManager.DisplayMessage(new InformationMessage(
                new TextObject("{=menu_qm_unavailable}Quartermaster services temporarily unavailable.").ToString()));
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
                    HasExplicitlyVisitedSettlement = true;
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
                    "Pay has been late for too long. You approach your comrades and explain that you can't continue like this.\n\n" +
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
                    ModLogger.ErrorCode("Interface", "E-UI-040", "Cannot desert - EnlistmentBehavior not available");
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
        private HashSet<string> _decisionsPrevTrainingIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private HashSet<string> _decisionsPrevSocialIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private HashSet<string> _decisionsPrevCampLifeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private bool _decisionsPrevCampLifeEnabled;

        private bool _decisionsNewQueued;
        private bool _decisionsNewOpportunities;
        private bool _decisionsNewTraining;
        private bool _decisionsNewSocial;
        private bool _decisionsNewCampLife;

        private CampaignTime? _decisionsNewQueuedSince;
        private CampaignTime? _decisionsNewOpportunitiesSince;
        private CampaignTime? _decisionsNewTrainingSince;
        private CampaignTime? _decisionsNewSocialSince;
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

                if (_decisionsNewTraining && _decisionsNewTrainingSince.HasValue &&
                    now - _decisionsNewTrainingSince.Value > DecisionsNewAutoClearThreshold)
                {
                    _decisionsNewTraining = false;
                    _decisionsNewTrainingSince = null;
                }

                if (_decisionsNewSocial && _decisionsNewSocialSince.HasValue &&
                    now - _decisionsNewSocialSince.Value > DecisionsNewAutoClearThreshold)
                {
                    _decisionsNewSocial = false;
                    _decisionsNewSocialSince = null;
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
                    else if (section == DecisionsMenuSection.Opportunities)
                    {
                        _decisionsNewOpportunities = false;
                        _decisionsNewOpportunitiesSince = null;
                    }
                    else if (section == DecisionsMenuSection.Training)
                    {
                        _decisionsNewTraining = false;
                        _decisionsNewTrainingSince = null;
                    }
                    else if (section == DecisionsMenuSection.Social)
                    {
                        _decisionsNewSocial = false;
                        _decisionsNewSocialSince = null;
                    }
                    else if (section == DecisionsMenuSection.CampLife)
                    {
                        _decisionsNewCampLife = false;
                        _decisionsNewCampLifeSince = null;
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
        /// Initialize decisions menu - loads available decisions and sets up text.
        /// </summary>
        private void OnDecisionsMenuInit(MenuCallbackArgs args)
        {
            try
            {
                EnsureDecisionsAccordionInitialized();

                // Start wait to enable time controls for the wait menu
                args.MenuContext.GameMenu.StartWait();

                // Unlock time control so player can change speed, then restore their prior state
                Campaign.Current.SetTimeControlModeLock(false);

                // Restore captured time using stoppable equivalents, preserving Stop when paused
                var captured = QuartermasterManager.CapturedTimeMode ?? Campaign.Current.TimeControlMode;
                var normalized = QuartermasterManager.NormalizeToStoppable(captured);
                Campaign.Current.TimeControlMode = normalized;

                if (!QuartermasterManager.CapturedTimeMode.HasValue)
                {
                    QuartermasterManager.CapturedTimeMode = normalized;
                }

                var enlistment = EnlistmentBehavior.Instance;
                var decisionManager = DecisionManager.Instance;
                
                var currentQueuedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var currentOpportunityIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var currentTrainingIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var currentSocialIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var currentCampLifeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // Collect current decision IDs per section
                if (decisionManager != null)
                {
                    foreach (var dec in decisionManager.GetAvailableDecisionsForSection("training").Where(d => d.IsVisible))
                        currentTrainingIds.Add(dec.Decision.Id);
                    
                    foreach (var dec in decisionManager.GetAvailableDecisionsForSection("social").Where(d => d.IsVisible))
                        currentSocialIds.Add(dec.Decision.Id);
                    
                    foreach (var dec in decisionManager.GetAvailableDecisionsForSection("camp_life").Where(d => d.IsVisible))
                        currentCampLifeIds.Add(dec.Decision.Id);
                }

                var campLifeEnabledNow = false;
                var mainParty = MobileParty.MainParty;
                if (mainParty?.MemberRoster != null)
                {
                    campLifeEnabledNow = mainParty.MemberRoster.TotalWounded > 0;
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

                    // Detect new training decisions (came off cooldown, tier unlock, etc.)
                    if (!_decisionsNewTraining && currentTrainingIds.Except(_decisionsPrevTrainingIds).Any())
                    {
                        _decisionsNewTraining = true;
                        _decisionsNewTrainingSince ??= CampaignTime.Now;
                    }

                    // Detect new social decisions
                    if (!_decisionsNewSocial && currentSocialIds.Except(_decisionsPrevSocialIds).Any())
                    {
                        _decisionsNewSocial = true;
                        _decisionsNewSocialSince ??= CampaignTime.Now;
                    }

                    // Detect new camp life decisions
                    if (!_decisionsNewCampLife && currentCampLifeIds.Except(_decisionsPrevCampLifeIds).Any())
                    {
                        _decisionsNewCampLife = true;
                        _decisionsNewCampLifeSince ??= CampaignTime.Now;
                    }
                }

                _decisionsSnapshotsInitialized = true;
                _decisionsPrevQueuedIds = currentQueuedIds;
                _decisionsPrevOpportunityIds = currentOpportunityIds;
                _decisionsPrevTrainingIds = currentTrainingIds;
                _decisionsPrevSocialIds = currentSocialIds;
                _decisionsPrevCampLifeIds = currentCampLifeIds;
                _decisionsPrevCampLifeEnabled = campLifeEnabledNow;

                // Auto-clear markers after a while so they don't stick forever if the player ignores them.
                MaybeClearExpiredDecisionsNewFlags();

                // Build comprehensive status text for decision-making context
                var statusText = BuildDecisionsStatusText(enlistment);
                MBTextManager.SetTextVariable("DECISIONS_STATUS_TEXT", statusText);

                _cachedDecisionsMenuEntries = BuildDecisionsMenuEntries();

                // Diagnostic logging to help debug menu issues (using Info so it appears with default log settings)
                ModLogger.Info("Interface", $"Built {_cachedDecisionsMenuEntries.Count} decision menu entries");
                for (var idx = 0; idx < _cachedDecisionsMenuEntries.Count; idx++)
                {
                    var entry = _cachedDecisionsMenuEntries[idx];
                    ModLogger.Info("Interface", $"  [{idx}] {entry?.Id}: {entry?.Text}");
                }

                for (var i = 0; i < 40; i++)
                {
                    var slotText = i < _cachedDecisionsMenuEntries.Count
                        ? (_cachedDecisionsMenuEntries[i]?.Text ?? string.Empty)
                        : string.Empty;

                    // Never allow truly blank rows (they show as "icon-only" entries and look broken).
                    if (i < _cachedDecisionsMenuEntries.Count)
                    {
                        var entry = _cachedDecisionsMenuEntries[i];
                        if (entry != null && string.IsNullOrWhiteSpace(slotText))
                        {
                            slotText = $"    [{entry.Id}]";
                        }
                    }
                    MBTextManager.SetTextVariable($"DECISION_SLOT_{i}_TEXT", slotText);
                }
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("Interface", "E-UI-043", "Failed to initialize decisions menu", ex);
                MBTextManager.SetTextVariable("DECISIONS_STATUS_TEXT", "Error loading decisions.");
            }
        }

        private static string BuildDecisionsStatusText(EnlistmentBehavior enlistment)
        {
            try
            {
                var sb = new StringBuilder();

                // Player status (RP flavor)
                var news = EnlistedNewsBehavior.Instance;
                var playerStatus = news?.BuildPlayerStatusLine(enlistment) ?? "Ready for duty.";
                sb.AppendLine($"Your Status: {playerStatus}");

                // Injuries
                var conditions = PlayerConditionBehavior.Instance;
                var hasInjuries = conditions?.IsEnabled() == true && conditions.State?.HasInjury == true;
                var hasIllness = conditions?.IsEnabled() == true && conditions.State?.HasIllness == true;
                
                if (hasInjuries && hasIllness)
                {
                    sb.AppendLine("Injuries: Wounded and ill");
                }
                else if (hasInjuries)
                {
                    sb.AppendLine("Injuries: Wounded");
                }
                else if (hasIllness)
                {
                    sb.AppendLine("Injuries: Ill");
                }
                else
                {
                    sb.AppendLine("Injuries: None");
                }
                
                sb.AppendLine();

                // Company Status section
                sb.AppendLine("━━━ COMPANY STATUS ━━━");
                
                var companyNeeds = enlistment?.CompanyNeeds;
                var readiness = companyNeeds?.Readiness ?? 0;
                var morale = companyNeeds?.Morale ?? 0;
                var supply = companyNeeds?.Supplies ?? 0;
                sb.AppendLine($"Readiness: {readiness}% | Morale: {morale}% | Supply: {supply}%");

                // Wounded and casualties
                var lord = enlistment?.CurrentLord;
                var lordParty = lord?.PartyBelongedTo;
                var wounded = lordParty?.MemberRoster?.TotalWounded ?? 0;
                var lostSinceMuster = news?.LostSinceLastMuster ?? 0;
                sb.AppendLine($"Wounded: {wounded} soldiers | Lost since muster: {lostSinceMuster}");
                
                sb.AppendLine();

                // Recent Actions section
                sb.AppendLine("━━━ RECENT ACTIONS ━━━");
                var personalFeed = news?.GetVisiblePersonalFeedItems();
                if (personalFeed == null || personalFeed.Count == 0)
                {
                    sb.AppendLine("• Nothing notable to report.");
                }
                else
                {
                    foreach (var item in personalFeed)
                    {
                        var formatted = EnlistedNewsBehavior.FormatDispatchForDisplay(item);
                        if (!string.IsNullOrWhiteSpace(formatted))
                        {
                            sb.AppendLine($"• {formatted}");
                        }
                    }
                }

                return sb.ToString().TrimEnd();
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", "Failed to build decisions status text", ex);
                return "Status unavailable.";
            }
        }

        private List<DecisionsMenuEntry> BuildDecisionsMenuEntries()
        {
            var list = new List<DecisionsMenuEntry>();
            var decisionManager = DecisionManager.Instance;

            // Back button (first entry)
            list.Add(new DecisionsMenuEntry
            {
                Id = "decisions_back",
                Text = "Back",
                IsEnabled = true,
                LeaveType = GameMenuOption.LeaveType.Leave,
                OnSelected = args => OnDecisionsBackSelected(args)
            });

            // Opportunities section (automatic decisions triggered by context)
            var opportunities = decisionManager?.GetAvailableOpportunities() ?? Array.Empty<DecisionAvailability>();
            var hasOpportunities = opportunities.Count > 0;

            list.Add(new DecisionsMenuEntry
            {
                Id = "header_events",
                Text = "<span style=\"Link\">OPPORTUNITIES</span>" + NewTag(_decisionsNewOpportunities),
                IsEnabled = true,
                LeaveType = GameMenuOption.LeaveType.WaitQuest,
                OnSelected = a => ToggleDecisionsSection(DecisionsMenuSection.Opportunities, a)
            });

            if (!_decisionsCollapsedOpportunities)
            {
                if (!hasOpportunities)
                {
                    list.Add(new DecisionsMenuEntry
                    {
                        Id = "events_none",
                        Text = "    (none available)",
                        IsEnabled = false,
                        LeaveType = (GameMenuOption.LeaveType)(-1),
                        Tooltip = new TextObject("{=decisions_no_opportunities}No opportunities at this time. Check back later.")
                    });
                }
                else
                {
                    foreach (var opp in opportunities)
                    {
                        AddDecisionEntry(list, opp);
                    }
                }
            }

            // Training section
            list.Add(new DecisionsMenuEntry
            {
                Id = "header_training",
                Text = "<span style=\"Link\">TRAINING</span>" + NewTag(_decisionsNewTraining),
                IsEnabled = true,
                LeaveType = GameMenuOption.LeaveType.OrderTroopsToAttack,
                OnSelected = a => ToggleDecisionsSection(DecisionsMenuSection.Training, a)
            });

            if (!_decisionsCollapsedTraining)
            {
                AddSectionDecisions(list, decisionManager, "training");
            }

            // Social section
            list.Add(new DecisionsMenuEntry
            {
                Id = "header_social",
                Text = "<span style=\"Link\">SOCIAL</span>" + NewTag(_decisionsNewSocial),
                IsEnabled = true,
                LeaveType = GameMenuOption.LeaveType.Conversation,
                OnSelected = a => ToggleDecisionsSection(DecisionsMenuSection.Social, a)
            });

            if (!_decisionsCollapsedSocial)
            {
                AddSectionDecisions(list, decisionManager, "social");
            }

            // Camp Life section
            list.Add(new DecisionsMenuEntry
            {
                Id = "header_camp",
                Text = "<span style=\"Link\">CAMP LIFE</span>" + NewTag(_decisionsNewCampLife),
                IsEnabled = true,
                LeaveType = GameMenuOption.LeaveType.Manage,
                OnSelected = a => ToggleDecisionsSection(DecisionsMenuSection.CampLife, a)
            });

            if (!_decisionsCollapsedCampLife)
            {
                AddSectionDecisions(list, decisionManager, "camp_life");
            }

            // Logistics section (new for QM-related decisions)
            var logisticsDecisions = decisionManager?.GetAvailableDecisionsForSection("logistics") ?? Array.Empty<DecisionAvailability>();
            var visibleLogistics = logisticsDecisions.Where(d => d.IsVisible).ToList();
            
            if (visibleLogistics.Count > 0)
            {
                list.Add(new DecisionsMenuEntry
                {
                    Id = "header_logistics",
                    Text = "<span style=\"Link\">LOGISTICS</span>",
                    IsEnabled = true,
                    LeaveType = GameMenuOption.LeaveType.Trade,
                    OnSelected = a => ToggleDecisionsSection(DecisionsMenuSection.Queued, a) // Reuse Queued for logistics toggle
                });

                if (!_decisionsCollapsedQueued)
                {
                    foreach (var dec in visibleLogistics)
                    {
                        AddDecisionEntry(list, dec);
                    }
                }
            }

            return list;
        }

        /// <summary>
        /// Adds decisions from a specific section to the menu list.
        /// </summary>
        private void AddSectionDecisions(List<DecisionsMenuEntry> list, DecisionManager decisionManager, string section)
        {
            if (decisionManager == null)
            {
                list.Add(new DecisionsMenuEntry
                {
                    Id = $"section_{section}_none",
                    Text = "    (loading...)",
                    IsEnabled = false,
                    LeaveType = (GameMenuOption.LeaveType)(-1)
                });
                return;
            }

            var decisions = decisionManager.GetAvailableDecisionsForSection(section);
            var visibleDecisions = decisions.Where(d => d.IsVisible).ToList();

            if (visibleDecisions.Count == 0)
            {
                list.Add(new DecisionsMenuEntry
                {
                    Id = $"section_{section}_none",
                    Text = "    (none available)",
                    IsEnabled = false,
                    LeaveType = (GameMenuOption.LeaveType)(-1)
                });
                return;
            }

            foreach (var dec in visibleDecisions)
            {
                AddDecisionEntry(list, dec);
            }
        }

        /// <summary>
        /// Adds a single decision entry to the menu list.
        /// </summary>
        private void AddDecisionEntry(List<DecisionsMenuEntry> list, DecisionAvailability availability)
        {
            var decision = availability.Decision;
            if (decision == null)
            {
                return;
            }

            // Build display text with indent
            var name = GetDecisionDisplayName(decision);
            
            // Log for debugging blank entries
            ModLogger.Info("Interface", $"AddDecisionEntry: id={decision.Id}, titleId={decision.TitleId}, name='{name}'");
            
            if (string.IsNullOrWhiteSpace(name))
            {
                name = decision.Id ?? "Unknown";
                ModLogger.Warn("Interface", $"Decision '{decision.Id}' had blank display name, using ID as fallback");
            }
            var displayText = $"    {name}";

            // Build tooltip
            var tooltipText = GetDecisionTooltip(decision, availability);

            list.Add(new DecisionsMenuEntry
            {
                Id = decision.Id,
                Text = displayText,
                IsEnabled = availability.IsAvailable,
                LeaveType = (GameMenuOption.LeaveType)(-1), // No icon for individual decisions
                Tooltip = new TextObject(tooltipText),
                OnSelected = _ => OnDecisionSelected(decision)
            });
        }

        /// <summary>
        /// Gets a human-readable display name for a decision.
        /// Uses the title from XML or falls back to formatted ID.
        /// </summary>
        private static string GetDecisionDisplayName(DecisionDefinition decision)
        {
            if (decision == null)
            {
                return "[null decision]";
            }

            // Build fallback from ID first (always have something to show)
            var id = decision.Id ?? "Unknown";
            var fallbackName = id;
            
            // Remove common prefixes for cleaner display
            if (fallbackName.StartsWith("player_", StringComparison.OrdinalIgnoreCase))
            {
                fallbackName = fallbackName.Substring(7);
            }
            else if (fallbackName.StartsWith("decision_", StringComparison.OrdinalIgnoreCase))
            {
                fallbackName = fallbackName.Substring(9);
            }

            // Convert underscores to spaces and title case
            var words = fallbackName.Split('_');
            fallbackName = string.Join(" ", words.Select(w => 
                string.IsNullOrEmpty(w) ? w : char.ToUpper(w[0]) + w.Substring(1).ToLower()));

            // Try localized title from XML
            if (!string.IsNullOrEmpty(decision.TitleId))
            {
                try
                {
                    // Use the {=id}fallback format - if XML lookup fails, returns the fallback
                    var textObj = new TextObject($"{{={decision.TitleId}}}{fallbackName}");
                    var resolved = textObj.ToString();
                    
                    // Check if resolution worked (not empty and not just the raw {=...} tag)
                    if (!string.IsNullOrWhiteSpace(resolved) && !resolved.StartsWith("{="))
                    {
                        return resolved;
                    }
                }
                catch
                {
                    // If TextObject throws, fall through to fallback
                }
            }

            // Return fallback - never empty
            return string.IsNullOrWhiteSpace(fallbackName) ? $"[{id}]" : fallbackName;
        }

        /// <summary>
        /// Builds a tooltip for a decision showing effects and availability info.
        /// </summary>
        private static string GetDecisionTooltip(DecisionDefinition decision, DecisionAvailability availability)
        {
            var parts = new List<string>();

            // If unavailable, show reason first
            if (!availability.IsAvailable && !string.IsNullOrEmpty(availability.UnavailableReason))
            {
                parts.Add(availability.UnavailableReason);
            }

            // Try to get localized setup text for description
            if (!string.IsNullOrEmpty(decision.SetupId))
            {
                // No fallback here: if missing, prefer showing nothing over raw IDs.
                var setupText = new TextObject($"{{={decision.SetupId}}}").ToString();
                if (!string.IsNullOrWhiteSpace(setupText) && !setupText.StartsWith("{=") && setupText.Length < 200)
                {
                    parts.Add(setupText);
                }
            }

            // Show cooldown info if relevant
            if (decision.Timing?.CooldownDays > 0 && availability.IsAvailable)
            {
                parts.Add($"Cooldown: {decision.Timing.CooldownDays} days");
            }

            return parts.Count > 0 ? string.Join("\n", parts) : "Select to begin.";
        }

        /// <summary>
        /// Gets the appropriate menu leave type icon for a section.
        /// </summary>
        /// <summary>
        /// Handles when a decision is selected from the menu.
        /// Shows the decision popup using EventDeliveryManager.
        /// </summary>
        private void OnDecisionSelected(DecisionDefinition decision)
        {
            if (decision == null)
            {
                return;
            }

            try
            {
                ModLogger.Info("Interface", $"Decision selected: {decision.Id}");

                // Record that this decision was selected (for cooldown tracking)
                DecisionManager.Instance?.RecordDecisionSelected(decision.Id);

                // Convert to EventDefinition and deliver via EventDeliveryManager
                var eventDef = ConvertDecisionToEvent(decision);
                if (eventDef != null)
                {
                    var deliveryManager = EventDeliveryManager.Instance;
                    if (deliveryManager != null)
                    {
                        deliveryManager.QueueEvent(eventDef);
                    }
                    else
                    {
                        ModLogger.Warn("Interface", "EventDeliveryManager not available, showing simple popup");
                        ShowSimpleDecisionPopup(decision);
                    }
                }
                else
                {
                    ShowSimpleDecisionPopup(decision);
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", $"Error handling decision selection: {decision.Id}", ex);
                InformationManager.DisplayMessage(new InformationMessage($"Error: {ex.Message}"));
            }
        }

        /// <summary>
        /// Converts a DecisionDefinition to EventDefinition for delivery.
        /// </summary>
        private static EventDefinition ConvertDecisionToEvent(DecisionDefinition decision)
        {
            if (decision == null)
            {
                return null;
            }

            return new EventDefinition
            {
                Id = decision.Id,
                TitleId = decision.TitleId,
                SetupId = decision.SetupId,
                Category = decision.Category,
                Requirements = decision.Requirements,
                Timing = decision.Timing,
                Options = decision.Options
            };
        }

        /// <summary>
        /// Shows a simple popup for decisions when EventDeliveryManager isn't available.
        /// </summary>
        private static void ShowSimpleDecisionPopup(DecisionDefinition decision)
        {
            var title = GetDecisionDisplayName(decision);
            var body = !string.IsNullOrEmpty(decision.SetupId)
                ? new TextObject($"{{={decision.SetupId}}}").ToString()
                : "Make your choice.";

            if (body.StartsWith("{="))
            {
                body = $"Decision: {title}";
            }

            var options = new List<InquiryElement>();
            foreach (var opt in decision.Options)
            {
                var optText = !string.IsNullOrEmpty(opt.TextId)
                    ? new TextObject($"{{={opt.TextId}}}").ToString()
                    : opt.Id;

                if (optText.StartsWith("{="))
                {
                    optText = opt.Id;
                }

                options.Add(new InquiryElement(opt.Id, optText, null, true, opt.Tooltip));
            }

            if (options.Count == 0)
            {
                options.Add(new InquiryElement("ok", "Acknowledge", null, true, null));
            }

            var inquiry = new MultiSelectionInquiryData(
                title,
                body,
                options,
                true,  // isExitShown
                1,     // minSelectableOptionCount
                1,     // maxSelectableOptionCount
                "Confirm",
                "Cancel",
                selectedElements =>
                {
                    if (selectedElements != null && selectedElements.Count > 0)
                    {
                        var selectedId = selectedElements[0].Identifier as string;
                        InformationManager.DisplayMessage(new InformationMessage($"Selected: {selectedId}"));
                    }
                },
                null);  // negativeAction

            MBInformationManager.ShowMultiSelectionInquiry(inquiry, true);
        }

        /// <summary>
        /// Condition check for decisions menu (always true when enlisted).
        /// </summary>
        private bool OnDecisionsMenuCondition(MenuCallbackArgs args)
        {
            _ = args;
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
            _ = args;
            QuartermasterManager.CaptureTimeStateBeforeMenuActivation();
            GameMenu.SwitchToMenu("enlisted_status");
        }

        private void OnReportsMenuInit(MenuCallbackArgs args)
        {
            try
            {
                args.MenuContext.GameMenu.StartWait();
                Campaign.Current.SetTimeControlModeLock(false);

                var captured = QuartermasterManager.CapturedTimeMode ?? Campaign.Current.TimeControlMode;
                var normalized = QuartermasterManager.NormalizeToStoppable(captured);
                Campaign.Current.TimeControlMode = normalized;

                MBTextManager.SetTextVariable("REPORTS_TEXT", BuildReportsText());
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("Interface", "E-UI-020", "Error initializing reports menu", ex);
                MBTextManager.SetTextVariable("REPORTS_TEXT", "Reports unavailable.");
            }
        }

        private void OnStatusDetailMenuInit(MenuCallbackArgs args)
        {
            try
            {
                args.MenuContext.GameMenu.StartWait();
                Campaign.Current.SetTimeControlModeLock(false);

                var captured = QuartermasterManager.CapturedTimeMode ?? Campaign.Current.TimeControlMode;
                var normalized = QuartermasterManager.NormalizeToStoppable(captured);
                Campaign.Current.TimeControlMode = normalized;

                MBTextManager.SetTextVariable("STATUS_DETAIL_TEXT", BuildStatusDetailText());
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("Interface", "E-UI-021", "Error initializing status detail menu", ex);
                MBTextManager.SetTextVariable("STATUS_DETAIL_TEXT", "Status details unavailable.");
            }
        }

        private static string BuildReportsText()
        {
            try
            {
                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment?.IsEnlisted != true)
                {
                    return "You are not currently enlisted.";
                }

                var sb = new StringBuilder();

                sb.AppendLine("=== DAILY BRIEF ===");
                var dailyBrief = EnlistedNewsBehavior.Instance?.BuildDailyBriefSection();
                if (!string.IsNullOrWhiteSpace(dailyBrief))
                {
                    sb.AppendLine(dailyBrief);
                }
                else
                {
                    sb.AppendLine("No brief available for today.");
                }
                sb.AppendLine();

                sb.AppendLine("=== RECENT ACTIVITY ===");
                var recentActivity = BuildRecentActivityReport();
                if (!string.IsNullOrWhiteSpace(recentActivity))
                {
                    sb.AppendLine(recentActivity);
                }
                else
                {
                    sb.AppendLine("No recent activity to report.");
                }
                sb.AppendLine();

                sb.AppendLine("=== COMPANY STATUS ===");
                var companyStatus = BuildCompanyStatusReport();
                sb.AppendLine(companyStatus);
                sb.AppendLine();

                sb.AppendLine("=== CAMPAIGN CONTEXT ===");
                var lord = enlistment.CurrentLord;
                var context = Instance?.GetCurrentObjectiveDisplay(lord) ?? "Unknown";
                sb.AppendLine($"Lord's Objective: {context}");

                return sb.ToString().TrimEnd();
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", "Failed to build reports text", ex);
                return "Reports unavailable.";
            }
        }

        private static string BuildStatusDetailText()
        {
            try
            {
                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment?.IsEnlisted != true)
                {
                    return "You are not currently enlisted.";
                }

                var escalation = EscalationManager.Instance;
                var lord = enlistment.CurrentLord;
                var sb = new StringBuilder();

                // Add player status RP flavor line at the top
                var playerStatus = EnlistedNewsBehavior.Instance?.BuildPlayerStatusLine(enlistment);
                if (!string.IsNullOrWhiteSpace(playerStatus))
                {
                    sb.AppendLine(playerStatus);
                    sb.AppendLine();
                }

                var rank = Ranks.RankHelper.GetCurrentRank(enlistment);
                sb.AppendLine($"Rank: {rank} (Tier {enlistment.EnlistmentTier})");
                sb.AppendLine($"Lord: {lord?.Name?.ToString() ?? "Unknown"}");
                sb.AppendLine($"Campaign: {Instance?.GetCurrentObjectiveDisplay(lord) ?? "Unknown"}");
                sb.AppendLine();

                sb.AppendLine("=== REPUTATION ===");
                if (escalation?.State != null)
                {
                    sb.AppendLine($"Lord:     {escalation.State.LordReputation}/100 ({GetReputationLevel(escalation.State.LordReputation)})");
                    sb.AppendLine($"Officers: {escalation.State.OfficerReputation}/100 ({GetReputationLevel(escalation.State.OfficerReputation)})");
                    sb.AppendLine($"Soldiers: {escalation.State.SoldierReputation}/100 ({GetSoldierReputationLevel(escalation.State.SoldierReputation)})");
                }
                else
                {
                    sb.AppendLine("Reputation data unavailable.");
                }
                sb.AppendLine();

                sb.AppendLine("=== ROLE & SPECIALIZATIONS ===");
                var statusManager = Identity.EnlistedStatusManager.Instance;
                if (statusManager != null)
                {
                    var role = statusManager.GetPrimaryRole();
                    var roleDesc = statusManager.GetRoleDescription();
                    sb.AppendLine($"Primary Role: {role}");
                    sb.AppendLine(roleDesc);
                    sb.AppendLine();
                    sb.AppendLine("Specializations:");
                    sb.AppendLine(statusManager.GetAllSpecializations());
                }
                else
                {
                    sb.AppendLine("Role data unavailable.");
                }
                sb.AppendLine();

                sb.AppendLine("=== PERSONALITY TRAITS ===");
                sb.AppendLine(GetPersonalityTraits());

                return sb.ToString().TrimEnd();
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", "Failed to build status detail text", ex);
                return "Status details unavailable.";
            }
        }

        private static string GetReputationLevel(int value)
        {
            if (value >= 80)
            {
                return "Celebrated";
            }
            if (value >= 60)
            {
                return "Trusted";
            }
            if (value >= 40)
            {
                return "Respected";
            }
            if (value >= 20)
            {
                return "Promising";
            }
            if (value >= 0)
            {
                return "Neutral";
            }
            if (value >= -20)
            {
                return "Questionable";
            }
            if (value >= -40)
            {
                return "Disliked";
            }
            if (value >= -60)
            {
                return "Despised";
            }
            if (value >= -80)
            {
                return "Hated";
            }
            return "Enemy";
        }

        private static string GetSoldierReputationLevel(int value)
        {
            if (value >= 40)
            {
                return "Admired";
            }
            if (value >= 20)
            {
                return "Liked";
            }
            if (value >= 0)
            {
                return "Accepted";
            }
            if (value >= -20)
            {
                return "Ignored";
            }
            if (value >= -40)
            {
                return "Disliked";
            }
            return "Despised";
        }

        private static string BuildRecentActivityReport()
        {
            try
            {
                var news = EnlistedNewsBehavior.Instance;
                if (news == null)
                {
                    return "No activity tracking available.";
                }

                var sb = new StringBuilder();
                var hasActivity = false;

                // Show recent order outcomes (last 5 days)
                var recentOrders = news.GetRecentOrderOutcomes(maxDaysOld: 5);
                if (recentOrders.Count > 0)
                {
                    foreach (var order in recentOrders)
                    {
                        int daysAgo = (int)CampaignTime.Now.ToDays - order.DayNumber;
                        string timeStr = daysAgo == 0 ? "today" : daysAgo == 1 ? "yesterday" : $"{daysAgo} days ago";

                        sb.AppendLine($"• {order.OrderTitle} ({timeStr})");
                        sb.AppendLine($"  {order.DetailedSummary}");
                        sb.AppendLine();
                        hasActivity = true;
                    }
                }

                // Show significant reputation changes (last 5 days)
                var recentRep = news.GetRecentReputationChanges(maxDaysOld: 5);
                if (recentRep.Count > 0)
                {
                    foreach (var rep in recentRep)
                    {
                        int daysAgo = (int)CampaignTime.Now.ToDays - rep.DayNumber;
                        string timeStr = daysAgo == 0 ? "today" : daysAgo == 1 ? "yesterday" : $"{daysAgo} days ago";

                        sb.AppendLine($"• {rep.Target} reputation {rep.Delta:+#;-#;0} ({timeStr})");
                        sb.AppendLine($"  {rep.Message}");
                        sb.AppendLine();
                        hasActivity = true;
                    }
                }

                return hasActivity ? sb.ToString().TrimEnd() : string.Empty;
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", "Failed to build recent activity report", ex);
                return "Recent activity unavailable.";
            }
        }

        private static string BuildCompanyStatusReport()
        {
            try
            {
                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment?.IsEnlisted != true || enlistment.CompanyNeeds == null)
                {
                    return new TextObject("{=company_status_unavailable}Company status unavailable.").ToString();
                }

                var needs = enlistment.CompanyNeeds;
                var lord = enlistment.CurrentLord;
                var party = lord?.PartyBelongedTo;
                var sb = new StringBuilder();

                // Determine current conditions for context
                var isMarching = party is { IsMoving: true, CurrentSettlement: null };
                var isInCombat = party?.MapEvent != null;
                var isInSiege = party?.Party?.SiegeEvent != null;
                var isInSettlement = party?.CurrentSettlement != null;
                var isInArmy = party?.Army != null;

                // READINESS: Combat effectiveness and preparation
                sb.AppendLine(BuildReadinessLine(needs.Readiness, isMarching, isInCombat, needs.Morale < 40));
                
                // MORALE: The unit's will to fight
                sb.AppendLine(BuildMoraleLine(needs.Morale, enlistment, isInCombat, isInSiege));
                
                // SUPPLIES: Food and consumables
                sb.AppendLine(BuildSuppliesLine(needs.Supplies, isMarching, isInSiege));
                
                // EQUIPMENT: Maintenance and gear quality
                sb.AppendLine(BuildEquipmentLine(needs.Equipment, isInCombat, isMarching, party));
                
                // REST: Fatigue and recovery
                sb.AppendLine(BuildRestLine(needs.Rest, isMarching, isInSettlement, isInArmy));

                return sb.ToString().TrimEnd();
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", "Failed to build company status report", ex);
                return new TextObject("{=company_status_unavailable}Company status unavailable.").ToString();
            }
        }
        
        private static string BuildReadinessLine(int value, bool isMarching, bool isInCombat, bool lowMorale)
        {
            // Context: What's affecting readiness?
            var context = "";
            if (isInCombat)
            {
                context = new TextObject("{=status_readiness_combat} Battle drains our reserves.").ToString();
            }
            else if (isMarching && lowMorale)
            {
                context = new TextObject("{=status_readiness_march_morale} The long march and low spirits take their toll.").ToString();
            }
            else if (isMarching)
            {
                context = new TextObject("{=status_readiness_march} The march wears on the men.").ToString();
            }
            else if (lowMorale)
            {
                context = new TextObject("{=status_readiness_morale} Low morale saps the company's edge.").ToString();
            }

            // Status description by level
            var status = value switch
            {
                >= 80 => new TextObject("{=status_readiness_excellent}READINESS: The company stands battle-ready, formations tight and weapons sharp.").ToString(),
                >= 60 => new TextObject("{=status_readiness_good}READINESS: The company is prepared for action, though some drills have been skipped.").ToString(),
                >= 40 => new TextObject("{=status_readiness_fair}READINESS: The company can fight, but coordination has slipped.").ToString(),
                >= 20 => new TextObject("{=status_readiness_poor}READINESS: The company is disorganized. Officers bark orders to restore discipline.").ToString(),
                _ => new TextObject("{=status_readiness_critical}READINESS: The company is a shambles. Men mill about confused, barely fit for battle.").ToString()
            };

            return string.IsNullOrEmpty(context) ? status : $"{status}{context}";
        }
        
        private static string BuildMoraleLine(int value, EnlistmentBehavior enlistment, bool isInCombat, bool isInSiege)
        {
            // Context: What's affecting morale?
            var context = "";
            var payTension = enlistment?.PayTension ?? 0;
            
            if (payTension >= 50)
            {
                context = new TextObject("{=status_morale_pay_high} Pay is long overdue and the men are angry.").ToString();
            }
            else if (payTension >= 25)
            {
                context = new TextObject("{=status_morale_pay_low} The men grumble about late wages.").ToString();
            }
            else if (isInSiege)
            {
                context = new TextObject("{=status_morale_siege} The tedium of siege weighs on everyone.").ToString();
            }
            else if (isInCombat)
            {
                context = new TextObject("{=status_morale_combat} Battle tests every man's courage.").ToString();
            }

            var status = value switch
            {
                >= 80 => new TextObject("{=status_morale_excellent}MORALE: Spirits are high. The men sing as they march and talk of glory.").ToString(),
                >= 60 => new TextObject("{=status_morale_good}MORALE: The company's mood is steady. Complaints are few.").ToString(),
                >= 40 => new TextObject("{=status_morale_fair}MORALE: The men are restless. Grumbling spreads around the cookfires.").ToString(),
                >= 20 => new TextObject("{=status_morale_poor}MORALE: The company is unhappy. Fights break out and discipline slips.").ToString(),
                _ => new TextObject("{=status_morale_critical}MORALE: The company is on the edge. Desertion whispers spread through camp.").ToString()
            };

            return string.IsNullOrEmpty(context) ? status : $"{status}{context}";
        }
        
        private static string BuildSuppliesLine(int value, bool isMarching, bool isInSiege)
        {
            // Context: What's affecting supplies?
            var context = "";
            if (isInSiege)
            {
                context = new TextObject("{=status_supplies_siege} Siege rations are stretched thin.").ToString();
            }
            else if (isMarching)
            {
                context = new TextObject("{=status_supplies_march} The march consumes provisions quickly.").ToString();
            }

            var status = value switch
            {
                >= 80 => new TextObject("{=status_supplies_excellent}SUPPLIES: The wagons are well-stocked. Food is plentiful and gear is available.").ToString(),
                >= 60 => new TextObject("{=status_supplies_good}SUPPLIES: Adequate provisions remain. The quartermaster is not worried.").ToString(),
                >= 40 => new TextObject("{=status_supplies_fair}SUPPLIES: Rations are tightening. The quartermaster counts every sack of grain.").ToString(),
                >= 20 => new TextObject("{=status_supplies_poor}SUPPLIES: Food is scarce. Men go hungry and equipment cannot be replaced.").ToString(),
                _ => new TextObject("{=status_supplies_critical}SUPPLIES: The company is starving. Men eye the pack horses with desperation.").ToString()
            };

            return string.IsNullOrEmpty(context) ? status : $"{status}{context}";
        }
        
        private static string BuildEquipmentLine(int value, bool isInCombat, bool isMarching, MobileParty party)
        {
            // Context: What's affecting equipment?
            var context = "";
            if (isInCombat)
            {
                context = new TextObject("{=status_equipment_combat} Battle wears hard on arms and armor.").ToString();
            }
            else if (isMarching)
            {
                // Check terrain for equipment wear context
                var terrainType = Campaign.Current?.MapSceneWrapper?.GetFaceTerrainType(party?.CurrentNavigationFace ?? default);
                if (terrainType == TerrainType.Mountain || terrainType == TerrainType.Desert)
                {
                    context = new TextObject("{=status_equipment_terrain} Rough terrain damages gear faster than usual.").ToString();
                }
            }

            var status = value switch
            {
                >= 80 => new TextObject("{=status_equipment_excellent}EQUIPMENT: Weapons are sharp, armor polished. The armorer has little to do.").ToString(),
                >= 60 => new TextObject("{=status_equipment_good}EQUIPMENT: Gear is serviceable. Minor repairs needed here and there.").ToString(),
                >= 40 => new TextObject("{=status_equipment_fair}EQUIPMENT: The armorer works constantly. Notched blades and dented helms are common.").ToString(),
                >= 20 => new TextObject("{=status_equipment_poor}EQUIPMENT: Gear is failing. Men fight with bent swords and cracked shields.").ToString(),
                _ => new TextObject("{=status_equipment_critical}EQUIPMENT: The company is barely armed. Some men wrap rags around their hands for lack of gloves.").ToString()
            };

            return string.IsNullOrEmpty(context) ? status : $"{status}{context}";
        }
        
        private static string BuildRestLine(int value, bool isMarching, bool isInSettlement, bool isInArmy)
        {
            // Context: What's affecting rest?
            var context = "";
            if (isMarching && isInArmy)
            {
                context = new TextObject("{=status_rest_army_march} Forced marches with the army leave no time for rest.").ToString();
            }
            else if (isMarching)
            {
                context = new TextObject("{=status_rest_march} Days on the road exhaust even the hardiest soldiers.").ToString();
            }
            else if (isInSettlement)
            {
                context = new TextObject("{=status_rest_settlement} The settlement offers a chance to recover.").ToString();
            }

            var status = value switch
            {
                >= 80 => new TextObject("{=status_rest_excellent}REST: The company is well-rested. Men wake refreshed and ready.").ToString(),
                >= 60 => new TextObject("{=status_rest_good}REST: The company has had adequate rest. Some yawning, but nothing serious.").ToString(),
                >= 40 => new TextObject("{=status_rest_fair}REST: Fatigue is setting in. Men doze on their feet during long halts.").ToString(),
                >= 20 => new TextObject("{=status_rest_poor}REST: The company is exhausted. Tempers flare and mistakes multiply.").ToString(),
                _ => new TextObject("{=status_rest_critical}REST: The company is dead on their feet. Men collapse during marches.").ToString()
            };

            return string.IsNullOrEmpty(context) ? status : $"{status}{context}";
        }

        private static string GetPersonalityTraits()
        {
            try
            {
                var hero = Hero.MainHero;
                if (hero == null || Campaign.Current?.DefaultTraits == null)
                {
                    return "No trait data available.";
                }

                var sb = new StringBuilder();
                sb.AppendLine($"Valor: {hero.GetTraitLevel(TaleWorlds.CampaignSystem.CharacterDevelopment.DefaultTraits.Valor)}");
                sb.AppendLine($"Mercy: {hero.GetTraitLevel(TaleWorlds.CampaignSystem.CharacterDevelopment.DefaultTraits.Mercy)}");
                sb.AppendLine($"Generosity: {hero.GetTraitLevel(TaleWorlds.CampaignSystem.CharacterDevelopment.DefaultTraits.Generosity)}");
                sb.AppendLine($"Honor: {hero.GetTraitLevel(TaleWorlds.CampaignSystem.CharacterDevelopment.DefaultTraits.Honor)}");
                sb.AppendLine($"Calculating: {hero.GetTraitLevel(TaleWorlds.CampaignSystem.CharacterDevelopment.DefaultTraits.Calculating)}");

                return sb.ToString();
            }
            catch
            {
                return "Trait data unavailable.";
            }
        }

        private void RegisterLeaveServiceMenu(CampaignGameStarter starter)
        {
            starter.AddWaitGameMenu(LeaveServiceMenuId,
                "{=Enlisted_LeaveService_Title}— LEAVE SERVICE —\n{LEAVE_SERVICE_TEXT}",
                OnLeaveServiceInit,
                _ => EnlistmentBehavior.Instance?.IsEnlisted == true,
                null,
                NoopMenuTick,
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
                // Start wait to enable time controls for the wait menu
                args.MenuContext.GameMenu.StartWait();

                // Unlock time control so player can change speed, then restore their prior state
                Campaign.Current.SetTimeControlModeLock(false);

                // Restore captured time using stoppable equivalents, preserving Stop when paused
                var captured = QuartermasterManager.CapturedTimeMode ?? Campaign.Current.TimeControlMode;
                var normalized = QuartermasterManager.NormalizeToStoppable(captured);
                Campaign.Current.TimeControlMode = normalized;

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

        #region Orders Integration

        private void RefreshEnlistedStatusMenuUi()
        {
            try
            {
                RefreshEnlistedStatusDisplay();

                var menuContext = Campaign.Current?.CurrentMenuContext;
                if (Campaign.Current != null && menuContext?.GameMenu != null)
                {
                    Campaign.Current.GameMenuManager.RefreshMenuOptions(menuContext);
                }
            }
            catch
            {
                // Best-effort only; never let a UI refresh throw from a button callback.
            }
        }

        private void ToggleOrdersAccordion(MenuCallbackArgs args)
        {
            try
            {
                var currentOrder = OrderManager.Instance?.GetCurrentOrder();
                if (currentOrder == null)
                {
                    _ordersCollapsed = true;
                    return;
                }

                _ordersCollapsed = !_ordersCollapsed;

                // Re-render header/report text and then force the menu to re-evaluate option visibility.
                RefreshEnlistedStatusDisplay(args);

                var menuContext = args?.MenuContext ?? Campaign.Current?.CurrentMenuContext;
                if (Campaign.Current != null && menuContext?.GameMenu != null)
                {
                    Campaign.Current.GameMenuManager.RefreshMenuOptions(menuContext);
                }
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("Interface", "E-UI-046", "Failed to toggle orders accordion", ex);
            }
        }

        /// <summary>
        /// Shows the orders menu with current order status and accept/decline options.
        /// </summary>
        private void ShowOrdersMenu()
        {
            try
            {
                var orderManager = OrderManager.Instance;
                var currentOrder = orderManager?.GetCurrentOrder();

                if (currentOrder == null)
                {
                    // No active order
                    InformationManager.ShowInquiry(new InquiryData(
                        titleText: "Orders",
                        text: "No active orders at this time. Check back later.",
                        isAffirmativeOptionShown: true,
                        isNegativeOptionShown: false,
                        affirmativeText: "Continue",
                        negativeText: null,
                        affirmativeAction: null,
                        negativeAction: null
                    ), true);
                    return;
                }

                // Build order display text
                var sb = new StringBuilder();
                sb.AppendLine($"ORDER FROM: {currentOrder.Issuer}");
                sb.AppendLine();
                sb.AppendLine($"TITLE: {currentOrder.Title}");
                sb.AppendLine();
                sb.AppendLine($"OBJECTIVE:");
                sb.AppendLine(currentOrder.Description);
                sb.AppendLine();

                // Show requirements if any
                if (currentOrder.Requirements != null)
                {
                    if (currentOrder.Requirements.MinSkills != null && currentOrder.Requirements.MinSkills.Count > 0)
                    {
                        sb.AppendLine("Required Skills:");
                        foreach (var skill in currentOrder.Requirements.MinSkills)
                        {
                            sb.AppendLine($"  • {skill.Key}: {skill.Value}");
                        }
                        sb.AppendLine();
                    }

                    if (currentOrder.Requirements.MinTraits != null && currentOrder.Requirements.MinTraits.Count > 0)
                    {
                        sb.AppendLine("Required Traits:");
                        foreach (var trait in currentOrder.Requirements.MinTraits)
                        {
                            sb.AppendLine($"  • {trait.Key}: {trait.Value}");
                        }
                        sb.AppendLine();
                    }
                }

                var daysAgo = (int)(CampaignTime.Now - currentOrder.IssuedTime).ToDays;
                string timeStr = daysAgo == 0 ? "today" : daysAgo == 1 ? "yesterday" : $"{daysAgo} days ago";
                sb.AppendLine($"Issued: {timeStr}");
                sb.AppendLine($"Declines: {orderManager.GetDeclineCount()}");

                InformationManager.ShowInquiry(new InquiryData(
                    titleText: "Active Order",
                    text: sb.ToString(),
                    isAffirmativeOptionShown: true,
                    isNegativeOptionShown: true,
                    affirmativeText: "Accept Order",
                    negativeText: "Decline Order",
                    affirmativeAction: () =>
                    {
                        orderManager.AcceptOrder();
                        _ordersCollapsed = true;
                        _menuNeedsRefresh = true;
                        RefreshEnlistedStatusMenuUi();
                    },
                    negativeAction: () =>
                    {
                        // Show decline confirmation
                        InformationManager.ShowInquiry(new InquiryData(
                            titleText: "Decline Order?",
                            text: "Declining orders damages your reputation with your superiors. Continue?",
                            isAffirmativeOptionShown: true,
                            isNegativeOptionShown: true,
                            affirmativeText: "Yes, Decline",
                            negativeText: "Cancel",
                            affirmativeAction: () =>
                            {
                                orderManager.DeclineOrder();
                                _ordersCollapsed = true;
                                _menuNeedsRefresh = true;
                                RefreshEnlistedStatusMenuUi();
                            },
                            negativeAction: null
                        ), true);
                    }
                ), true);
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("Interface", "E-UI-040", "Error showing orders menu", ex);
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
