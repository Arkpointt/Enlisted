using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Localization;
using TaleWorlds.Library;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Assignments.Behaviors;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Features.Conversations.Behaviors
{
    /// <summary>
    /// Centralized dialog manager for all enlisted military service conversations.
    /// 
    /// This system provides a single hub for all enlisted dialogs to prevent conflicts
    /// and maintain consistent conversation flows. Uses the diplomatic submenu for 
    /// professional integration with the game's conversation system.
    /// </summary>
    public sealed class EnlistedDialogManager : CampaignBehaviorBase
    {
        public static EnlistedDialogManager Instance { get; private set; }

        public EnlistedDialogManager()
        {
            Instance = this;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Dialog manager has no persistent state
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            AddEnlistedDialogs(starter);
            AddBasicEnlistedMenu(starter);  // SAS CRITICAL: Add basic menu for immediate activation
        }

        /// <summary>
        /// Add all enlisted dialog flows through centralized management.
        /// Uses diplomatic submenu (lord_talk_speak_diplomacy_2) for professional integration.
        /// </summary>
        private void AddEnlistedDialogs(CampaignGameStarter starter)
        {
            try
            {
                // Professional entry point through diplomatic channels
                AddMainEnlistedEntry(starter);
                
                // Enlistment conversation flow
                AddEnlistmentDialogs(starter);
                
                // Service management dialogs
                AddServiceDialogs(starter);
                
                // Duties management dialogs
                AddDutiesDialogs(starter);
                
                // Retirement conversation flow
                AddRetirementDialogs(starter);

                ModLogger.Info("DialogManager", "Enlisted dialogs registered successfully");
            }
            catch (Exception ex)
            {
                ModLogger.Error("DialogManager", "Failed to register enlisted dialogs", ex);
            }
        }

        #region Main Entry Point

        /// <summary>
        /// Main entry point through "I have something else to discuss" -> diplomatic submenu.
        /// This provides professional integration without conflicting with other mods.
        /// </summary>
        private void AddMainEnlistedEntry(CampaignGameStarter starter)
        {
            starter.AddPlayerLine(
                "enlisted_diplomatic_entry",
                "lord_talk_speak_diplomacy_2",
                "enlisted_main_hub",
                GetLocalizedText("{=enlisted_diplomatic_entry}I wish to discuss military service.").ToString(),
                IsValidLordForMilitaryService,
                null,
                110);

            starter.AddDialogLine(
                "enlisted_main_hub_response",
                "enlisted_main_hub",
                "enlisted_service_options",
                GetLocalizedText("{=enlisted_main_hub_response}What military matters do you wish to discuss?").ToString(),
                null,
                null,
                110);
        }

        #endregion

        #region Enlistment Dialogs

        private void AddEnlistmentDialogs(CampaignGameStarter starter)
        {
            // Option to request enlistment
            starter.AddPlayerLine(
                "enlisted_request_service",
                "enlisted_service_options",
                "enlisted_enlistment_response",
                GetLocalizedText("{=enlisted_request_service}I wish to serve in your warband.").ToString(),
                CanRequestEnlistment,
                null,
                110);

            // Lord's response to enlistment request
            starter.AddDialogLine(
                "enlisted_lord_accepts",
                "enlisted_enlistment_response",
                "enlisted_service_terms",
                GetLocalizedText("{=enlisted_lord_accepts}Very well. You may serve under my command. These are the terms of service...").ToString(),
                null,
                null,
                110);

            // Service terms and confirmation
            starter.AddDialogLine(
                "enlisted_service_terms_details",
                "enlisted_service_terms",
                "enlisted_confirm_service",
                GetLocalizedText("{=enlisted_service_terms_details}You will follow my orders, share in our victories, and receive daily wages. Do you accept these terms?").ToString(),
                null,
                null,
                110);

            // Player accepts service
            starter.AddPlayerLine(
                "enlisted_accept_service",
                "enlisted_confirm_service",
                "close_window",
                GetLocalizedText("{=enlisted_accept_service}I accept. We march together.").ToString(),
                null,
                OnAcceptEnlistment,
                110);

            // Player declines service
            starter.AddPlayerLine(
                "enlisted_decline_service",
                "enlisted_confirm_service",
                "lord_pretalk",
                GetLocalizedText("{=enlisted_decline_service}I need more time to consider.").ToString(),
                null,
                null,
                110);
        }

        #endregion

        #region Service Management Dialogs

        private void AddServiceDialogs(CampaignGameStarter starter)
        {
            // Service status option (when already enlisted)
            starter.AddPlayerLine(
                "enlisted_check_service",
                "enlisted_service_options",
                "enlisted_service_status",
                GetLocalizedText("{=enlisted_check_service}I wish to discuss my current service.").ToString(),
                IsAlreadyEnlisted,
                null,
                110);

            // Lord responds with current service status
            starter.AddDialogLine(
                "enlisted_service_status_response",
                "enlisted_service_status",
                "enlisted_service_management",
                GetLocalizedText("{=enlisted_service_status_response}You serve with honor. What do you wish to discuss about your service?").ToString(),
                null,
                null,
                110);

            // Service management options will be expanded in Phase 1B
            starter.AddPlayerLine(
                "enlisted_service_continue",
                "enlisted_service_management",
                "lord_pretalk",
                GetLocalizedText("{=enlisted_service_continue}I will continue my duties.").ToString(),
                null,
                null,
                110);
        }

        #endregion

        #region Retirement Dialogs

        private void AddRetirementDialogs(CampaignGameStarter starter)
        {
            // Request retirement option
            starter.AddPlayerLine(
                "enlisted_request_retirement",
                "enlisted_service_management",
                "enlisted_retirement_response",
                GetLocalizedText("{=enlisted_request_retirement}I wish to request discharge from service.").ToString(),
                CanRequestRetirement,
                null,
                110);

            // Lord's response to retirement request
            starter.AddDialogLine(
                "enlisted_retirement_granted",
                "enlisted_retirement_response",
                "close_window",
                GetLocalizedText("{=enlisted_retirement_granted}Your service has been honorable. You are discharged with our gratitude.").ToString(),
                null,
                OnGrantRetirement,
                110);
        }

        #endregion

        #region Shared Dialog Conditions

        /// <summary>
        /// Checks if the conversation partner is a valid lord for military service discussions.
        /// This shared condition prevents dialog conflicts and ensures consistency.
        /// </summary>
        private bool IsValidLordForMilitaryService()
        {
            var lord = Hero.OneToOneConversationHero;
            return lord != null && lord.IsLord && lord.IsAlive;
        }

        /// <summary>
        /// Checks if the player can request enlistment with the current lord.
        /// </summary>
        private bool CanRequestEnlistment()
        {
            if (EnlistmentBehavior.Instance?.IsEnlisted == true)
            {
                return false;
            }

            var lord = Hero.OneToOneConversationHero;
            if (lord == null || !lord.IsLord)
            {
                return false;
            }

            TextObject reason;
            return EnlistmentBehavior.Instance?.CanEnlistWithParty(lord, out reason) == true;
        }

        /// <summary>
        /// Checks if the player is currently enlisted and can discuss service matters.
        /// </summary>
        private bool IsAlreadyEnlisted()
        {
            if (EnlistmentBehavior.Instance?.IsEnlisted != true)
            {
                return false;
            }

            var lord = Hero.OneToOneConversationHero;
            return lord != null && lord == EnlistmentBehavior.Instance.CurrentLord;
        }

        /// <summary>
        /// Checks if the player can request retirement from current service.
        /// </summary>
        private bool CanRequestRetirement()
        {
            // For Phase 1A, simple check. Will be enhanced in Phase 1B with service duration requirements
            return EnlistmentBehavior.Instance?.IsEnlisted == true;
        }

        #endregion

        #region Shared Dialog Consequences

        /// <summary>
        /// Handles the consequence of accepting enlistment.
        /// Centralized to ensure consistent behavior across all enlistment paths.
        /// </summary>
        private void OnAcceptEnlistment()
        {
            try
            {
                var lord = Hero.OneToOneConversationHero;
                if (lord == null)
                {
                    ModLogger.Error("DialogManager", "No conversation hero found during enlistment acceptance");
                    return;
                }

                if (EnlistmentBehavior.Instance == null)
                {
                    ModLogger.Error("DialogManager", "EnlistmentBehavior.Instance is null during enlistment");
                    return;
                }

                ModLogger.Info("DialogManager", $"Player accepting enlistment with lord: {lord.Name}");
                EnlistmentBehavior.Instance.StartEnlist(lord);
                
                // SAS CRITICAL: Immediately clear all menus to prevent encounter gaps
                while (Campaign.Current.CurrentMenuContext != null)
                {
                    GameMenu.ExitToLast();
                }
                ModLogger.Debug("DialogSAS", "Cleared all menus after enlistment - preventing encounter gap");
                
                // SAS CRITICAL: Activate enlisted menu immediately (MISSING STEP!)
                GameMenu.ActivateGameMenu("enlisted_status");
                ModLogger.Debug("DialogSAS", "Activated enlisted_status menu - zero gap implementation");
                
                // Professional notification
                var message = GetLocalizedText("{=enlisted_success_notification}You have enlisted in {LORD_NAME}'s service.");
                message.SetTextVariable("LORD_NAME", lord.Name);
                InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
            }
            catch (Exception ex)
            {
                ModLogger.Error("DialogManager", "Error during enlistment acceptance", ex);
            }
        }

        /// <summary>
        /// Handles the consequence of granted retirement.
        /// Centralized to ensure proper cleanup and notifications.
        /// </summary>
        private void OnGrantRetirement()
        {
            try
            {
                if (EnlistmentBehavior.Instance?.IsEnlisted == true)
                {
                    var lordName = EnlistmentBehavior.Instance.CurrentLord?.Name?.ToString() ?? "Unknown Lord";
                    ModLogger.Info("DialogManager", $"Player retiring from service with: {lordName}");
                    
                    EnlistmentBehavior.Instance.StopEnlist("Retired through dialog");
                    
                    // Professional notification
                    var retireMessage = GetLocalizedText("{=enlisted_retirement_notification}You have been honorably discharged from military service.");
                    InformationManager.DisplayMessage(new InformationMessage(retireMessage.ToString()));
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("DialogManager", "Error during retirement processing", ex);
            }
        }

        #endregion

        #region Basic Enlisted Menu (SAS Critical Pattern)

        /// <summary>
        /// SAS CRITICAL: Add basic enlisted menu for immediate activation after enlistment.
        /// This prevents the encounter gap that causes the encounter window to appear.
        /// </summary>
        private void AddBasicEnlistedMenu(CampaignGameStarter starter)
        {
            try
            {
                // SAS APPROACH: Use AddWaitGameMenu (doesn't pause game) for continuous flow
                starter.AddWaitGameMenu("enlisted_status",
                    "Enlisted Service Status\n{ENLISTED_STATUS_TEXT}",
                    OnEnlistedStatusInit,
                    OnEnlistedStatusCondition,
                    null, // No consequence for wait menu
                    OnEnlistedStatusTick,
                    GameMenu.MenuAndOptionType.WaitMenuShowOnlyProgressOption,
                    0, // No overlay
                    1f, // Target hours (SAS uses small value for immediate)
                    GameMenu.MenuFlags.None,
                    null);

                // Basic menu option - continue service
                starter.AddGameMenuOption("enlisted_status", "continue_service",
                    GetLocalizedText("{=enlisted_continue_service}Continue service").ToString(),
                    (MenuCallbackArgs args) => {
                        args.optionLeaveType = GameMenuOption.LeaveType.Continue;
                        return true;
                    },
                    (MenuCallbackArgs args) => {
                        // Stay in enlisted status - SAS pattern
                    },
                    false, -1, false, null);

                ModLogger.Debug("DialogSAS", "Basic enlisted menu registered successfully");
            }
            catch (Exception ex)
            {
                ModLogger.Error("DialogManager", "Failed to register basic enlisted menu", ex);
            }
        }

        /// <summary>
        /// Initialize enlisted status menu with current service information.
        /// </summary>
        private void OnEnlistedStatusInit(MenuCallbackArgs args)
        {
            try
            {
                // Build enlisted status text
                var statusText = "";
                var enlistment = EnlistmentBehavior.Instance;
                
                if (enlistment?.IsEnlisted == true && enlistment.CurrentLord != null)
                {
                    // Phase 1B: Enhanced status display with progression data
                    statusText = $"Lord: {enlistment.CurrentLord.Name}\n";
                    statusText += $"Faction: {enlistment.CurrentLord.MapFaction?.Name?.ToString() ?? "Unknown"}\n";
                    statusText += $"Rank: Tier {enlistment.EnlistmentTier}/7\n";
                    statusText += $"Experience: {enlistment.EnlistmentXP} XP\n";
                    
                    // Service duration calculation (public property access)
                    var serviceDays = (CampaignTime.Now - CampaignTime.Zero).ToDays;
                    statusText += $"Service Duration: {serviceDays:F0} days\n";
                    
                    statusText += "\nFollowing your lord's commands...";
                }
                else
                {
                    statusText = "No active enlistment";
                }

                MBTextManager.SetTextVariable("ENLISTED_STATUS_TEXT", statusText);
                ModLogger.Debug("MenuSAS", "Enlisted status menu initialized");
            }
            catch (Exception ex)
            {
                ModLogger.Error("DialogManager", "Error initializing enlisted status menu", ex);
                MBTextManager.SetTextVariable("ENLISTED_STATUS_TEXT", "Service information unavailable");
            }
        }

        /// <summary>
        /// Condition for showing enlisted status menu.
        /// </summary>
        private bool OnEnlistedStatusCondition(MenuCallbackArgs args)
        {
            return EnlistmentBehavior.Instance?.IsEnlisted == true;
        }

        /// <summary>
        /// SAS pattern: Menu tick handler for continuous updates.
        /// </summary>
        private void OnEnlistedStatusTick(MenuCallbackArgs args, CampaignTime dt)
        {
            try
            {
                // SAS CRITICAL: If not enlisted anymore, exit menu
                if (EnlistmentBehavior.Instance?.IsEnlisted != true)
                {
                    GameMenu.ExitToLast();
                    return;
                }

                // Refresh status text periodically  
                OnEnlistedStatusInit(args);
            }
            catch (Exception ex)
            {
                ModLogger.Error("DialogManager", "Error in enlisted status tick", ex);
            }
        }

        #endregion

        #region Localization Helpers

        /// <summary>
        /// Get localized text using the verified {=key}fallback format.
        /// Provides fallback text for development and ensures professional localization support.
        /// </summary>
        private TextObject GetLocalizedText(string keyAndFallback)
        {
            return new TextObject(keyAndFallback);
        }

        #endregion
        
        #region Duties Management Dialogs
        
        /// <summary>
        /// Add duties management dialogs for assignment and officer role interactions.
        /// </summary>
        private void AddDutiesDialogs(CampaignGameStarter starter)
        {
            try
            {
                // Entry point for duties discussion
                starter.AddPlayerLine(
                    "enlisted_duties_entry",
                    "enlisted_service_options", 
                    "enlisted_duties_main",
                    GetLocalizedText("{=enlisted_duties_entry}I would like to discuss my duties.").ToString(),
                    IsEnlistedAndHasDutiesSystem,
                    null,
                    100);

                starter.AddDialogLine(
                    "enlisted_duties_main_response",
                    "enlisted_duties_main",
                    "enlisted_duties_options",
                    GetLocalizedText("{=enlisted_duties_main_response}What aspect of your duties do you wish to discuss?").ToString(),
                    null,
                    null,
                    100);

                // View current duties
                starter.AddPlayerLine(
                    "enlisted_duties_view_current",
                    "enlisted_duties_options",
                    "enlisted_duties_current_display", 
                    GetLocalizedText("{=enlisted_duties_view_current}What are my current duties?").ToString(),
                    null,
                    ShowCurrentDuties,
                    100);

                starter.AddDialogLine(
                    "enlisted_duties_current_response",
                    "enlisted_duties_current_display",
                    "enlisted_duties_options",
                    "{CURRENT_DUTIES_TEXT}",
                    null,
                    null,
                    100);

                // Back to service options
                starter.AddPlayerLine(
                    "enlisted_duties_back",
                    "enlisted_duties_options",
                    "enlisted_service_options",
                    GetLocalizedText("{=enlisted_duties_back}That will be all regarding duties.").ToString(),
                    null,
                    null,
                    100);

                ModLogger.Info("DialogManager", "Duties dialogs registered successfully");
            }
            catch (Exception ex)
            {
                ModLogger.Error("DialogManager", "Failed to register duties dialogs", ex);
            }
        }
        
        private bool IsEnlistedAndHasDutiesSystem()
        {
            return EnlistmentBehavior.Instance?.IsEnlisted == true &&
                   EnlistedDutiesBehavior.Instance?.IsInitialized == true;
        }
        
        private void ShowCurrentDuties()
        {
            try
            {
                var duties = EnlistedDutiesBehavior.Instance;
                if (duties?.IsInitialized != true)
                {
                    MBTextManager.SetTextVariable("CURRENT_DUTIES_TEXT", "No duties system available.");
                    return;
                }
                
                var activeDuties = duties.ActiveDuties;
                if (activeDuties.Count == 0)
                {
                    MBTextManager.SetTextVariable("CURRENT_DUTIES_TEXT", "You have no current duties assigned.");
                    return;
                }
                
                var dutiesText = $"You are currently assigned to {activeDuties.Count} duties:\n";
                dutiesText += string.Join(", ", activeDuties);
                
                MBTextManager.SetTextVariable("CURRENT_DUTIES_TEXT", dutiesText);
            }
            catch (Exception ex)
            {
                ModLogger.Error("DialogManager", "Error showing current duties", ex);
                MBTextManager.SetTextVariable("CURRENT_DUTIES_TEXT", "Error retrieving duty information.");
            }
        }
        
        #endregion
    }
}
