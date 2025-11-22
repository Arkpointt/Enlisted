using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Localization;
using TaleWorlds.Library;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Assignments.Behaviors;
using Enlisted.Features.Interface.Behaviors;
using Enlisted.Mod.Core.Logging;
using Enlisted.Mod.Entry;

namespace Enlisted.Features.Conversations.Behaviors
{
    /// <summary>
    /// Centralized dialog manager for all enlisted military service conversations.
    /// 
    /// This system provides a single hub for all enlisted dialogs to prevent conflicts
    /// and maintain consistent conversation flows. Uses the diplomatic submenu for 
    /// professional integration with the game's conversation system.
    /// 
    /// The menu system is handled by EnlistedMenuBehavior.cs, which provides the main enlisted status menu
    /// and duty/profession selection interface.
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
            // The menu system is handled by EnlistedMenuBehavior.cs, which provides the main enlisted status menu
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
                
                // Retirement conversation flow
                AddRetirementDialogs(starter);
                
                ModLogger.Info("DialogManager", "All enlisted dialog flows registered successfully - enlistment dialogs should appear in lord conversations");
            }
            catch (Exception ex)
            {
                ModLogger.Error("DialogManager", "Failed to register enlisted dialogs", ex);
            }
        }

        /// <summary>
        /// Add main entry point for enlisted services through diplomatic submenu.
        /// CORRECTED: Restore working player-initiated dialog structure.
        /// </summary>
        private void AddMainEnlistedEntry(CampaignGameStarter starter)
        {
            // FIXED: Player initiates military service discussion (was working!)
            starter.AddPlayerLine(
                "enlisted_diplomatic_entry",
                "lord_talk_speak_diplomacy_2",
                "enlisted_main_hub",
                GetLocalizedText("{=enlisted_diplomatic_entry}I wish to discuss military service.").ToString(),
                IsValidLordForMilitaryService,
                null,
                110);

            // Lord responds to player's request
            starter.AddDialogLine(
                "enlisted_main_hub_response",
                "enlisted_main_hub",
                "enlisted_service_options",
                GetLocalizedText("{=enlisted_main_hub_response}What military matters do you wish to discuss?").ToString(),
                null,
                null,
                110);
        }

        /// <summary>
        /// Add enlistment conversation flow.
        /// </summary>
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

            // Option to return from temporary leave
            starter.AddPlayerLine(
                "enlisted_return_from_leave",
                "enlisted_service_options",
                "enlisted_return_response",
                GetLocalizedText("{=enlisted_return_from_leave}I wish to return to service.").ToString(),
                CanReturnFromLeave,
                null,
                111);

            // Lord's response to return from leave request
            starter.AddDialogLine(
                "enlisted_return_accepted",
                "enlisted_return_response",
                "close_window",
                GetLocalizedText("{=enlisted_return_accepted}Welcome back to service. Resume your duties.").ToString(),
                null,
                OnReturnFromLeave,
                111);

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

        /// <summary>
        /// Add retirement conversation flow.
        /// </summary>
        private void AddRetirementDialogs(CampaignGameStarter starter)
        {
            // Request retirement option
            starter.AddPlayerLine(
                "enlisted_request_retirement",
                "enlisted_service_options",
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

        #region Shared Dialog Conditions

        /// <summary>
        /// Checks if the conversation partner is a valid lord for military service discussions.
        /// This shared condition prevents dialog conflicts and ensures consistency.
        /// CRITICAL: Prevents enlistment dialog from appearing if player is already enlisted, on leave, or in grace period.
        /// Player must be fully discharged before they can enlist with another lord/kingdom.
        /// </summary>
        private bool IsValidLordForMilitaryService()
        {
            var lord = Hero.OneToOneConversationHero;
            if (lord == null || !lord.IsLord || !lord.IsAlive)
            {
                return false;
            }
            
            var enlistment = EnlistmentBehavior.Instance;
            
            // CRITICAL: Prevent enlistment dialog if already enlisted, on leave, or in grace period
            // Player must be fully discharged before they can enlist with another lord/kingdom
            if (enlistment?.IsEnlisted == true)
            {
                ModLogger.Debug("DialogManager", $"Dialog hidden - player is already enlisted with {enlistment.CurrentLord?.Name}");
                return false;
            }
            
            if (enlistment?.IsOnLeave == true)
            {
                ModLogger.Debug("DialogManager", $"Dialog hidden - player is on temporary leave from {enlistment.CurrentLord?.Name}");
                return false;
            }
            
            if (enlistment?.IsInDesertionGracePeriod == true)
            {
                // During grace period, player can only rejoin the same kingdom they served
                // Don't show enlistment dialog for different kingdoms
                var lordKingdom = lord.MapFaction as Kingdom;
                if (lordKingdom != enlistment.PendingDesertionKingdom)
                {
                    ModLogger.Debug("DialogManager", $"Dialog hidden - player in grace period, can only rejoin {enlistment.PendingDesertionKingdom?.Name}");
                    return false;
                }
                // Allow dialog if same kingdom - they can rejoin during grace period
            }
            
            return true;
        }

        /// <summary>
        /// Checks if the player can request enlistment with the current lord.
        /// </summary>
        private bool CanRequestEnlistment()
        {
            var enlistment = EnlistmentBehavior.Instance;
            
            // Don't show initial enlistment if already enlisted or on leave
            if (enlistment?.IsEnlisted == true || enlistment?.IsOnLeave == true)
            {
                return false;
            }

            var lord = Hero.OneToOneConversationHero;
            if (lord == null || !lord.IsLord)
            {
                return false;
            }

            TextObject reason;
            return enlistment?.CanEnlistWithParty(lord, out reason) == true;
        }

        /// <summary>
        /// Checks if the player can request retirement from current service.
        /// </summary>
        private bool CanRequestRetirement()
        {
            // Simple check for now - can be enhanced with service duration requirements if needed
            return EnlistmentBehavior.Instance?.IsEnlisted == true;
        }

        /// <summary>
        /// Checks if the player can return from temporary leave.
        /// </summary>
        private bool CanReturnFromLeave()
        {
            var enlistment = EnlistmentBehavior.Instance;
            var lord = Hero.OneToOneConversationHero;
            
            // Must be on leave and talking to the same lord you served
            return enlistment?.IsOnLeave == true && 
                   enlistment.CurrentLord == lord;
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

                // Activate the enlisted status menu, deferred to the next frame to prevent timing conflicts
                // This ensures the menu activates cleanly after the conversation ends and prevents
                // any gaps that could cause encounter menus to appear
                NextFrameDispatcher.RunNextFrame(() => EnlistedMenuBehavior.SafeActivateEnlistedMenu());
                ModLogger.Debug("DialogManager", "Scheduled enlisted_status menu activation - preventing encounter gap");

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

        /// <summary>
        /// Handles the consequence of returning from temporary leave.
        /// </summary>
        private void OnReturnFromLeave()
        {
            try
            {
                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment?.IsOnLeave == true)
                {
                    var lordName = enlistment.CurrentLord?.Name?.ToString() ?? "Unknown Lord";
                    ModLogger.Info("DialogManager", $"Player returning from leave to service with: {lordName}");

                    enlistment.ReturnFromLeave();

                    // Professional notification
                    var returnMessage = GetLocalizedText("{=enlisted_return_notification}You have returned to active military service.");
                    InformationManager.DisplayMessage(new InformationMessage(returnMessage.ToString()));
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("DialogManager", "Error during return from leave", ex);
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Get localized text with fallback support.
        /// </summary>
        private TextObject GetLocalizedText(string key)
        {
            return new TextObject(key);
        }

        #endregion
    }
}