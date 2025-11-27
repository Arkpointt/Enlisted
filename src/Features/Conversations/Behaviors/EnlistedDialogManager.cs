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
            var enlistmentText = GetLocalizedText("{=enlisted_request_service}I wish to serve in your warband.");
            // Option to request enlistment (standard)
            starter.AddPlayerLine(
                "enlisted_request_service",
                "enlisted_service_options",
                "enlisted_enlistment_response",
                enlistmentText.ToString(),
                CanRequestStandardEnlistment,
                null,
                110);

            // Grace-specific enlistment option (covers lord killed, captured, or army defeated)
            starter.AddPlayerLine(
                "enlisted_request_service_grace",
                "enlisted_service_options",
                "enlisted_enlistment_response_grace",
                GetLocalizedText("{=enlisted_request_service_grace}My previous commander is no longer available. I wish to continue serving under your banner.").ToString(),
                CanRequestGraceTransfer,
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

            // Option to transfer service to a different lord (while on leave or grace period)
            starter.AddPlayerLine(
                "enlisted_request_transfer",
                "enlisted_service_options",
                "enlisted_transfer_response",
                GetLocalizedText("{=enlisted_request_transfer}I wish to transfer my service to your command.").ToString(),
                CanRequestServiceTransfer,
                null,
                112);

            // Lord's response to service transfer request
            starter.AddDialogLine(
                "enlisted_transfer_accepted",
                "enlisted_transfer_response",
                "close_window",
                GetLocalizedText("{=enlisted_transfer_accepted}Very well. Your prior commander will be informed. Report for duty immediately.").ToString(),
                null,
                OnAcceptServiceTransfer,
                112);

            // Lord's response to enlistment request
            starter.AddDialogLine(
                "enlisted_lord_accepts",
                "enlisted_enlistment_response",
                "enlisted_service_terms",
                GetLocalizedText("{=enlisted_lord_accepts}Very well. You may serve under my command. These are the terms of service...").ToString(),
                null,
                null,
                110);

            // Lord's response to grace re-assignment
            starter.AddDialogLine(
                "enlisted_lord_accepts_grace",
                "enlisted_enlistment_response_grace",
                "enlisted_service_terms",
                GetLocalizedText("{=enlisted_lord_accepts_grace}Our kingdom appreciates your resolve. You may join my banner and resume your duties. These are the terms...").ToString(),
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
        /// Add retirement conversation flow with full benefits explanation.
        /// </summary>
        private void AddRetirementDialogs(CampaignGameStarter starter)
        {
            // First-term retirement discussion (available after 252 days)
            starter.AddPlayerLine(
                "enlisted_discuss_retirement",
                "enlisted_service_options",
                "enlisted_retirement_benefits",
                GetLocalizedText("{=enlisted_discuss_retirement}I've served my term. I wish to discuss my retirement.").ToString(),
                CanDiscussFirstTermRetirement,
                null,
                109); // Higher priority than regular retirement
            
            // Lord explains benefits
            starter.AddDialogLine(
                "enlisted_retirement_benefits_explanation",
                "enlisted_retirement_benefits",
                "enlisted_retirement_choice",
                GetLocalizedText("{=enlisted_retirement_benefits_explanation}You've served with honor. You may retire with full benefits:\n- 10,000 gold severance\n- My personal recommendation (+30 relation)\n- Recognition from our kingdom (+30 reputation)\n- Letters to my fellow lords (+15 with those who respect you)\n\nAlternatively, I can offer 20,000 gold to extend your service one more year.").ToString(),
                null,
                null,
                110);
            
            // Player accepts retirement with benefits
            starter.AddPlayerLine(
                "enlisted_accept_retirement",
                "enlisted_retirement_choice",
                "enlisted_retirement_farewell",
                GetLocalizedText("{=enlisted_accept_retirement}I accept retirement with benefits.").ToString(),
                null,
                OnAcceptFirstTermRetirement,
                110);
            
            // Player accepts re-enlistment bonus
            starter.AddPlayerLine(
                "enlisted_accept_reenlist_bonus",
                "enlisted_retirement_choice",
                "enlisted_reenlist_confirmed",
                GetLocalizedText("{=enlisted_accept_reenlist_bonus}I'll take the 20,000 gold bonus and continue serving.").ToString(),
                null,
                OnAcceptFirstTermReenlistBonus,
                110);
            
            // Player needs time to decide
            starter.AddPlayerLine(
                "enlisted_retirement_later",
                "enlisted_retirement_choice",
                "close_window",
                GetLocalizedText("{=enlisted_retirement_later}I need more time to decide.").ToString(),
                null,
                null,
                110);
            
            // Lord's farewell on retirement
            starter.AddDialogLine(
                "enlisted_retirement_farewell_text",
                "enlisted_retirement_farewell",
                "close_window",
                GetLocalizedText("{=enlisted_retirement_farewell}Farewell, soldier. You've earned your rest. May we meet again in better times.").ToString(),
                null,
                null,
                110);
            
            // Lord confirms re-enlistment
            starter.AddDialogLine(
                "enlisted_reenlist_bonus_confirmed",
                "enlisted_reenlist_confirmed",
                "close_window",
                GetLocalizedText("{=enlisted_reenlist_bonus_confirmed}Excellent! Your loyalty is noted. Report back in one year for your discharge bonus, or speak with me to continue further.").ToString(),
                null,
                null,
                110);
            
            // Renewal term complete dialog (after 1-year renewal)
            starter.AddPlayerLine(
                "enlisted_renewal_complete",
                "enlisted_service_options",
                "enlisted_renewal_options",
                GetLocalizedText("{=enlisted_renewal_complete}My term has ended. I wish to discuss my options.").ToString(),
                CanDiscussRenewalTermEnd,
                null,
                108);
            
            // Lord explains renewal options
            starter.AddDialogLine(
                "enlisted_renewal_options_explanation",
                "enlisted_renewal_options",
                "enlisted_renewal_choice",
                GetLocalizedText("{=enlisted_renewal_options}Your term is complete. You may:\n- Retire with 5,000 gold discharge bonus\n- Continue serving with a 5,000 gold re-enlistment bonus for another year").ToString(),
                null,
                null,
                110);
            
            // Player accepts renewal discharge
            starter.AddPlayerLine(
                "enlisted_accept_renewal_discharge",
                "enlisted_renewal_choice",
                "enlisted_renewal_farewell",
                GetLocalizedText("{=enlisted_renewal_discharge}I'll take the discharge and my 5,000 gold.").ToString(),
                null,
                OnAcceptRenewalDischarge,
                110);
            
            // Player continues service
            starter.AddPlayerLine(
                "enlisted_continue_service",
                "enlisted_renewal_choice",
                "enlisted_continue_confirmed",
                GetLocalizedText("{=enlisted_continue_service}I'll continue serving for another year and take the 5,000 gold bonus.").ToString(),
                null,
                OnContinueService,
                110);
            
            // Lord's farewell on renewal discharge
            starter.AddDialogLine(
                "enlisted_renewal_farewell_text",
                "enlisted_renewal_farewell",
                "close_window",
                GetLocalizedText("{=enlisted_renewal_farewell}Very well. Your service has been valued. You may return after a period of rest if you wish to serve again.").ToString(),
                null,
                null,
                110);
            
            // Lord confirms continued service
            starter.AddDialogLine(
                "enlisted_continue_service_confirmed",
                "enlisted_continue_confirmed",
                "close_window",
                GetLocalizedText("{=enlisted_continue_confirmed}Your dedication is admirable. See you in a year, or speak with me again when your term ends.").ToString(),
                null,
                null,
                110);
            
            // Post-cooldown re-enlistment option
            starter.AddPlayerLine(
                "enlisted_reenlist_after_cooldown",
                "enlisted_service_options",
                "enlisted_veteran_welcome",
                GetLocalizedText("{=enlisted_reenlist_cooldown}I wish to return to service. I've served this kingdom before.").ToString(),
                CanReEnlistAfterCooldown,
                null,
                107);
            
            // Lord welcomes back veteran
            starter.AddDialogLine(
                "enlisted_veteran_welcome_back",
                "enlisted_veteran_welcome",
                "enlisted_veteran_confirm",
                GetLocalizedText("{=enlisted_veteran_welcome}Ah, a veteran returns! Your rank will be restored, though you'll need to select your troop type again. Your term will be one year, with 5,000 gold discharge at the end.").ToString(),
                null,
                null,
                110);
            
            // Veteran confirms re-enlistment
            starter.AddPlayerLine(
                "enlisted_veteran_accept",
                "enlisted_veteran_confirm",
                "enlisted_veteran_accepted",
                GetLocalizedText("{=enlisted_veteran_accept}I'm ready to serve again.").ToString(),
                null,
                OnVeteranReEnlist,
                110);
            
            // Veteran decides not to re-enlist
            starter.AddPlayerLine(
                "enlisted_veteran_decline",
                "enlisted_veteran_confirm",
                "close_window",
                GetLocalizedText("{=enlisted_veteran_decline}On second thought, not yet.").ToString(),
                null,
                null,
                110);
            
            // Lord confirms veteran re-enlistment
            starter.AddDialogLine(
                "enlisted_veteran_accepted_text",
                "enlisted_veteran_accepted",
                "close_window",
                GetLocalizedText("{=enlisted_veteran_accepted}Welcome back, soldier. Select your troop type at the Master at Arms and report for duty.").ToString(),
                null,
                null,
                110);
            
            // Simple early discharge (for those not eligible for full retirement)
            starter.AddPlayerLine(
                "enlisted_request_early_discharge",
                "enlisted_service_options",
                "enlisted_early_discharge_response",
                GetLocalizedText("{=enlisted_early_discharge}I wish to request discharge from service.").ToString(),
                CanRequestEarlyDischarge,
                null,
                115); // Lower priority - shows when not eligible for full retirement
            
            // Lord grants early discharge (no benefits)
            starter.AddDialogLine(
                "enlisted_early_discharge_granted",
                "enlisted_early_discharge_response",
                "close_window",
                GetLocalizedText("{=enlisted_early_discharge_granted}Your service ends today. You leave without the benefits of a full term, but you are free to go.").ToString(),
                null,
                OnGrantEarlyDischarge,
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
            
            // CRITICAL: Prevent enlistment dialog if already enlisted (and not on leave)
            // Player must be fully discharged before they can enlist with another lord/kingdom
            if (enlistment?.IsEnlisted == true && enlistment?.IsOnLeave != true)
            {
                ModLogger.Debug("DialogManager", $"Dialog hidden - player is actively enlisted with {enlistment.CurrentLord?.Name}");
                return false;
            }
            
            // When on leave, ALLOW dialog with same lord (return from leave) OR same-faction lords (transfer service)
            if (enlistment?.IsOnLeave == true)
            {
                if (enlistment.CurrentLord == lord)
                {
                    // Allow - player can return from leave with this lord
                    ModLogger.Debug("DialogManager", $"Dialog shown - player on leave, talking to their lord {lord.Name}");
                    return true;
                }
                
                // Allow dialog with other lords in the same faction for service transfer
                var currentLordKingdom = enlistment.CurrentLord?.MapFaction as Kingdom;
                var targetLordKingdom = lord.MapFaction as Kingdom;
                if (currentLordKingdom != null && targetLordKingdom == currentLordKingdom)
                {
                    ModLogger.Debug("DialogManager", $"Dialog shown - player on leave, can transfer to {lord.Name} (same faction)");
                    return true;
                }
                
                // Block - player can't enlist with different faction lord while on leave
                ModLogger.Debug("DialogManager", $"Dialog hidden - player is on leave from {enlistment.CurrentLord?.Name}, talking to different faction lord");
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
        private bool CanRequestStandardEnlistment()
        {
            var enlistment = EnlistmentBehavior.Instance;
            
            if (enlistment?.IsEnlisted == true || enlistment?.IsOnLeave == true)
            {
                return false;
            }

            if (enlistment?.IsInDesertionGracePeriod == true)
            {
                return false; // hide standard line when in grace period
            }

            var lord = Hero.OneToOneConversationHero;
            if (lord == null || !lord.IsLord)
            {
                return false;
            }

            TextObject reason;
            return enlistment?.CanEnlistWithParty(lord, out reason) == true;
        }

        private bool CanRequestGraceTransfer()
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment?.IsInDesertionGracePeriod != true || enlistment.IsEnlisted || enlistment.IsOnLeave)
            {
                return false;
            }

            var lord = Hero.OneToOneConversationHero;
            if (lord == null || !lord.IsLord)
            {
                return false;
            }

            var lordKingdom = lord.MapFaction as Kingdom;
            if (lordKingdom == null || lordKingdom != enlistment.PendingDesertionKingdom)
            {
                return false;
            }

            TextObject reason;
            return enlistment.CanEnlistWithParty(lord, out reason);
        }

        /// <summary>
        /// Checks if the player can discuss first-term retirement (after 252 days).
        /// Must be enlisted with current lord and have completed minimum service.
        /// </summary>
        private bool CanDiscussFirstTermRetirement()
        {
            var enlistment = EnlistmentBehavior.Instance;
            var lord = Hero.OneToOneConversationHero;
            
            // Must be enlisted with this specific lord and eligible for retirement
            return enlistment?.IsEnlisted == true && 
                   enlistment.CurrentLord == lord &&
                   enlistment.IsEligibleForRetirement;
        }
        
        /// <summary>
        /// Checks if the player can discuss renewal term completion.
        /// </summary>
        private bool CanDiscussRenewalTermEnd()
        {
            var enlistment = EnlistmentBehavior.Instance;
            var lord = Hero.OneToOneConversationHero;
            
            return enlistment?.IsEnlisted == true && 
                   enlistment.CurrentLord == lord &&
                   enlistment.IsInRenewalTerm &&
                   enlistment.IsRenewalTermComplete;
        }
        
        /// <summary>
        /// Checks if the player can re-enlist after cooldown (veteran return).
        /// </summary>
        private bool CanReEnlistAfterCooldown()
        {
            var enlistment = EnlistmentBehavior.Instance;
            var lord = Hero.OneToOneConversationHero;
            
            if (enlistment?.IsEnlisted == true || enlistment?.IsOnLeave == true)
            {
                return false;
            }
            
            var kingdom = lord?.MapFaction as Kingdom;
            return kingdom != null && enlistment.CanReEnlistAfterCooldown(kingdom);
        }
        
        /// <summary>
        /// Checks if the player can request early discharge (before full term).
        /// Shows only when not eligible for full retirement benefits.
        /// </summary>
        private bool CanRequestEarlyDischarge()
        {
            var enlistment = EnlistmentBehavior.Instance;
            var lord = Hero.OneToOneConversationHero;
            
            // Must be enlisted with this lord but NOT eligible for full retirement
            return enlistment?.IsEnlisted == true && 
                   enlistment.CurrentLord == lord &&
                   !enlistment.IsEligibleForRetirement &&
                   !enlistment.IsRenewalTermComplete;
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

        /// <summary>
        /// Checks if the player can request to transfer service to a different lord.
        /// Available when on leave or in grace period, talking to a different lord in the same faction.
        /// </summary>
        private bool CanRequestServiceTransfer()
        {
            var enlistment = EnlistmentBehavior.Instance;
            var lord = Hero.OneToOneConversationHero;
            
            if (lord == null || !lord.IsLord || !lord.IsAlive)
            {
                return false;
            }
            
            // Check if player is on leave - can transfer to different lord in same faction
            if (enlistment?.IsOnLeave == true)
            {
                // Must be talking to a DIFFERENT lord (not current lord - that's "return from leave")
                if (enlistment.CurrentLord == lord)
                {
                    return false;
                }
                
                // Must be same faction/kingdom
                var currentLordKingdom = enlistment.CurrentLord?.MapFaction as Kingdom;
                var targetLordKingdom = lord.MapFaction as Kingdom;
                
                if (currentLordKingdom != null && targetLordKingdom == currentLordKingdom)
                {
                    // Check if the lord can accept service
                    TextObject reason;
                    if (enlistment.CanEnlistWithParty(lord, out reason))
                    {
                        return true;
                    }
                }
                
                return false;
            }
            
            // Check if player is in grace period - can also transfer to any lord in the pending kingdom
            if (enlistment?.IsInDesertionGracePeriod == true && !enlistment.IsEnlisted)
            {
                var lordKingdom = lord.MapFaction as Kingdom;
                if (lordKingdom != null && lordKingdom == enlistment.PendingDesertionKingdom)
                {
                    // Check if the lord can accept service
                    TextObject reason;
                    if (enlistment.CanEnlistWithParty(lord, out reason))
                    {
                        return true;
                    }
                }
            }
            
            return false;
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
        /// Handles first-term retirement with full benefits.
        /// </summary>
        private void OnAcceptFirstTermRetirement()
        {
            try
            {
                EnlistmentBehavior.Instance?.ProcessFirstTermRetirement();
                ModLogger.Info("DialogManager", "First-term retirement processed");
            }
            catch (Exception ex)
            {
                ModLogger.Error("DialogManager", "Error during first-term retirement", ex);
            }
        }
        
        /// <summary>
        /// Handles first-term re-enlistment with 20,000 gold bonus.
        /// </summary>
        private void OnAcceptFirstTermReenlistBonus()
        {
            try
            {
                var config = Enlisted.Features.Assignments.Core.ConfigurationManager.LoadRetirementConfig();
                EnlistmentBehavior.Instance?.StartRenewalTerm(config.FirstTermReenlistBonus);
                ModLogger.Info("DialogManager", $"First-term re-enlistment with {config.FirstTermReenlistBonus}g bonus");
            }
            catch (Exception ex)
            {
                ModLogger.Error("DialogManager", "Error during first-term re-enlistment", ex);
            }
        }
        
        /// <summary>
        /// Handles renewal term discharge with 5,000 gold.
        /// </summary>
        private void OnAcceptRenewalDischarge()
        {
            try
            {
                EnlistmentBehavior.Instance?.ProcessRenewalRetirement();
                ModLogger.Info("DialogManager", "Renewal discharge processed");
            }
            catch (Exception ex)
            {
                ModLogger.Error("DialogManager", "Error during renewal discharge", ex);
            }
        }
        
        /// <summary>
        /// Handles continuing service with 5,000 gold bonus.
        /// </summary>
        private void OnContinueService()
        {
            try
            {
                var config = Enlisted.Features.Assignments.Core.ConfigurationManager.LoadRetirementConfig();
                EnlistmentBehavior.Instance?.StartRenewalTerm(config.RenewalContinueBonus);
                ModLogger.Info("DialogManager", $"Service continued with {config.RenewalContinueBonus}g bonus");
            }
            catch (Exception ex)
            {
                ModLogger.Error("DialogManager", "Error continuing service", ex);
            }
        }
        
        /// <summary>
        /// Handles veteran re-enlistment after cooldown.
        /// </summary>
        private void OnVeteranReEnlist()
        {
            try
            {
                var lord = Hero.OneToOneConversationHero;
                if (lord != null)
                {
                    EnlistmentBehavior.Instance?.ReEnlistAfterCooldown(lord);
                    
                    // Redirect to troop selection
                    NextFrameDispatcher.RunNextFrame(() => EnlistedMenuBehavior.SafeActivateEnlistedMenu());
                }
                ModLogger.Info("DialogManager", "Veteran re-enlistment processed");
            }
            catch (Exception ex)
            {
                ModLogger.Error("DialogManager", "Error during veteran re-enlistment", ex);
            }
        }
        
        /// <summary>
        /// Handles early discharge (before full term, no benefits).
        /// </summary>
        private void OnGrantEarlyDischarge()
        {
            try
            {
                if (EnlistmentBehavior.Instance?.IsEnlisted == true)
                {
                    var lordName = EnlistmentBehavior.Instance.CurrentLord?.Name?.ToString() ?? "Unknown Lord";
                    ModLogger.Info("DialogManager", $"Player early discharge from service with: {lordName}");

                    EnlistmentBehavior.Instance.StopEnlist("Early discharge through dialog", isHonorableDischarge: false);

                    var message = GetLocalizedText("{=enlisted_early_discharge_notification}You have been discharged from service without full benefits.");
                    InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("DialogManager", "Error during early discharge", ex);
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

        /// <summary>
        /// Handles the consequence of transferring service to a new lord.
        /// Called when player accepts transfer while on leave or in grace period.
        /// </summary>
        private void OnAcceptServiceTransfer()
        {
            try
            {
                var newLord = Hero.OneToOneConversationHero;
                var enlistment = EnlistmentBehavior.Instance;
                
                if (newLord == null || enlistment == null)
                {
                    ModLogger.Error("DialogManager", "Cannot transfer service - missing lord or enlistment instance");
                    return;
                }
                
                var previousLordName = enlistment.CurrentLord?.Name?.ToString() ?? "your previous commander";
                var newLordName = newLord.Name?.ToString() ?? "Unknown Lord";
                
                ModLogger.Info("DialogManager", $"Player transferring service from {previousLordName} to {newLordName}");
                
                // Perform the transfer
                enlistment.TransferServiceToLord(newLord);
                
                // Professional notification
                var message = GetLocalizedText("{=enlisted_transfer_notification}You have transferred your service to {LORD_NAME}. Your rank and experience have been preserved.");
                message.SetTextVariable("LORD_NAME", newLord.Name);
                InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
            }
            catch (Exception ex)
            {
                ModLogger.Error("DialogManager", "Error during service transfer", ex);
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