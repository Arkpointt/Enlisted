using System;
using Enlisted.Features.Camp;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Equipment.Behaviors;
using Enlisted.Features.Interface.Behaviors;
using Enlisted.Mod.Core.Logging;
using Enlisted.Mod.Entry;
// Dialogue regarding assignments is being updated to reflect the new order system.
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Enlisted.Features.Conversations.Behaviors
{
    /// <summary>
    ///     Centralized dialog manager for all enlisted military service conversations.
    ///     This system provides a single hub for all enlisted dialogs to prevent conflicts
    ///     and maintain consistent conversation flows. Uses the diplomatic submenu for
    ///     professional integration with the game's conversation system.
    ///     The menu system is handled by EnlistedMenuBehavior.cs, which provides the main enlisted status menu
    ///     and duty/profession selection interface.
    /// </summary>
    public sealed class EnlistedDialogManager : CampaignBehaviorBase
    {
        public EnlistedDialogManager()
        {
            Instance = this;
        }

        public static EnlistedDialogManager Instance { get; private set; }

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
            AddQuartermasterDialogs(starter);
            // The menu system is handled by EnlistedMenuBehavior.cs, which provides the main enlisted status menu
        }

        /// <summary>
        ///     Add all enlisted dialog flows through centralized management.
        ///     Uses diplomatic submenu (lord_talk_speak_diplomacy_2) for professional integration.
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

                ModLogger.Info("DialogManager",
                    "All enlisted dialog flows registered successfully - enlistment dialogs should appear in lord conversations");
            }
            catch (Exception ex)
            {
                ModLogger.Error("DialogManager", "Failed to register enlisted dialogs", ex);
            }
        }

        /// <summary>
        ///     Add main entry point for enlisted services through diplomatic submenu.
        ///     CORRECTED: Restore working player-initiated dialog structure.
        /// </summary>
        private void AddMainEnlistedEntry(CampaignGameStarter starter)
        {
            // FIXED: Player initiates military service discussion (was working!)
            starter.AddPlayerLine(
                "enlisted_diplomatic_entry",
                "lord_talk_speak_diplomacy_2",
                "enlisted_main_hub",
                GetLocalizedText(
                        "{=enlisted_diplomatic_entry}My lord, I seek to speak with you about bearing arms in your service.")
                    .ToString(),
                IsValidLordForMilitaryService,
                null,
                110);

            // Lord recognizes player serves another faction (roleplay rejection)
            starter.AddDialogLine(
                "enlisted_different_faction_rejection",
                "enlisted_main_hub",
                "close_window",
                GetLocalizedText(
                        "{=enlisted_different_faction_rejection}Hold a moment... I recognize your bearing. You serve another lord, do you not? A soldier cannot serve two masters. Return to your commander, or settle your affairs with them first before approaching me.")
                    .ToString(),
                IsOnLeaveWithDifferentFaction,
                null,
                120); // Higher priority - check this FIRST

            // Lord responds to player's request (normal flow)
            starter.AddDialogLine(
                "enlisted_main_hub_response",
                "enlisted_main_hub",
                "enlisted_service_options",
                GetLocalizedText("{=enlisted_main_hub_response}Speak freely. What brings a warrior to my hall?")
                    .ToString(),
                null,
                null,
                110);
        }

        /// <summary>
        ///     Add enlistment conversation flow.
        /// </summary>
        private void AddEnlistmentDialogs(CampaignGameStarter starter)
        {
            var enlistmentText =
                GetLocalizedText(
                    "{=enlisted_request_service}I offer you my sword and my loyalty, my lord. Will you have me in your ranks?");
            // Option to request enlistment (standard kingdom lords)
            starter.AddPlayerLine(
                "enlisted_request_service",
                "enlisted_service_options",
                "enlisted_enlistment_response",
                enlistmentText.ToString(),
                CanRequestStandardEnlistment,
                null,
                110);

            // Option to request enlistment (minor faction lords - mercenary tone)
            starter.AddPlayerLine(
                "enlisted_minor_request_service",
                "enlisted_service_options",
                "enlisted_minor_enlistment_response",
                GetLocalizedText(
                        "{=enlisted_minor_request_service}I hear your company takes on capable fighters. I can handle myself in a scrap - interested?")
                    .ToString(),
                CanRequestMinorFactionEnlistment,
                null,
                111); // Higher priority than standard

            // Minor faction lord's response to enlistment request
            starter.AddDialogLine(
                "enlisted_minor_enlistment_accepted",
                "enlisted_minor_enlistment_response",
                "close_window",
                GetLocalizedText(
                        "{=enlisted_minor_enlistment_accepted}You look useful. We pay fair coin for good work, and dead men don't collect. Fall in with the lads and keep your blade sharp.")
                    .ToString(),
                null,
                OnAcceptEnlistment,
                111);

            // Army leader attempting to enlist - shows when player commands their own army
            // Higher priority (109) so it appears when condition is met, blocking standard enlistment
            starter.AddPlayerLine(
                "enlisted_request_service_army_leader",
                "enlisted_service_options",
                "enlisted_army_leader_rejection",
                GetLocalizedText(
                        "{=enlisted_request_service_army_leader}My lord, I offer you my sword and my loyalty. Will you have me in your ranks?")
                    .ToString(),
                IsPlayerLeadingArmy,
                null,
                109);

            // Lord's response to army leader - cannot enlist while commanding an army
            starter.AddDialogLine(
                "enlisted_army_leader_rejection_response",
                "enlisted_army_leader_rejection",
                "enlisted_army_leader_acknowledge",
                GetLocalizedText(
                        "{=enlisted_army_leader_rejection}Hold, friend. I see you already command men of your own. A general cannot become a foot soldier while lords still march beneath his banner. Disband your army first, then we may speak of service.")
                    .ToString(),
                null,
                null,
                109);

            // Player acknowledges they must disband army first
            starter.AddPlayerLine(
                "enlisted_army_leader_acknowledge",
                "enlisted_army_leader_acknowledge",
                "lord_pretalk",
                GetLocalizedText(
                        "{=enlisted_army_leader_acknowledge}I understand, my lord. I shall see to my army's affairs first.")
                    .ToString(),
                null,
                null,
                109);

            // Grace-specific enlistment option (covers lord killed, captured, or army defeated)
            starter.AddPlayerLine(
                "enlisted_request_service_grace",
                "enlisted_service_options",
                "enlisted_enlistment_response_grace",
                GetLocalizedText(
                        "{=enlisted_request_service_grace}My lord, fate has taken my commander from me. I am a soldier without a banner, but my oath to this kingdom remains. Will you take me under your command?")
                    .ToString(),
                CanRequestGraceTransfer,
                null,
                110);

            // Option to return from temporary leave - kingdom lords
            starter.AddPlayerLine(
                "enlisted_return_from_leave",
                "enlisted_service_options",
                "enlisted_return_response",
                GetLocalizedText(
                        "{=enlisted_return_from_leave}I have rested enough, my lord. My blade grows restless. I am ready to return to the front.")
                    .ToString(),
                CanReturnFromLeave,
                null,
                111);

            // Option to return from temporary leave - minor faction lords
            starter.AddPlayerLine(
                "enlisted_minor_return_from_leave",
                "enlisted_service_options",
                "enlisted_minor_return_response",
                GetLocalizedText(
                        "{=enlisted_minor_return_from_leave}I've had my rest. Ready to get back to work, captain.")
                    .ToString(),
                CanReturnFromMinorFactionLeave,
                null,
                112); // Higher priority

            // Lord's response to return from leave request - kingdom lords
            starter.AddDialogLine(
                "enlisted_return_accepted",
                "enlisted_return_response",
                "close_window",
                GetLocalizedText(
                        "{=enlisted_return_accepted}Good to see you standing tall again. The men will be glad to have you back. Fall in with your company.")
                    .ToString(),
                null,
                OnReturnFromLeave,
                111);

            // Minor faction lord's response to return from leave
            starter.AddDialogLine(
                "enlisted_minor_return_accepted",
                "enlisted_minor_return_response",
                "close_window",
                GetLocalizedText(
                        "{=enlisted_minor_return_accepted}Good timing. We've got work lined up and could use another blade. Fall in with the lads.")
                    .ToString(),
                null,
                OnReturnFromLeave,
                112);

            // Option to transfer service to a different lord (while on leave or grace period)
            starter.AddPlayerLine(
                "enlisted_request_transfer",
                "enlisted_service_options",
                "enlisted_transfer_response",
                GetLocalizedText(
                        "{=enlisted_request_transfer}My lord, I currently serve another of this realm, but I believe my talents would be better suited under your banner. Would you accept my transfer?")
                    .ToString(),
                CanRequestServiceTransfer,
                null,
                112);

            // Lord's response to service transfer request
            starter.AddDialogLine(
                "enlisted_transfer_accepted",
                "enlisted_transfer_response",
                "close_window",
                GetLocalizedText(
                        "{=enlisted_transfer_accepted}A soldier who knows his worth. Very well, I shall send word to your former commander. From this day, you march with me.")
                    .ToString(),
                null,
                OnAcceptServiceTransfer,
                112);

            // ===== Promotion Request Dialog (T6→T7) =====
            // Player can request T7 promotion if they meet all requirements
            starter.AddPlayerLine(
                "enlisted_request_promotion_t7",
                "enlisted_service_options",
                "enlisted_promotion_t7_response",
                GetLocalizedText(
                        "{=enlisted_request_promotion_t7}My lord, I have proven myself in your service. I believe I am ready to accept the responsibilities of command.")
                    .ToString(),
                CanRequestCommanderPromotion,
                null,
                108); // High priority - shows when eligible

            // Lord evaluates player and offers promotion
            starter.AddDialogLine(
                "enlisted_promotion_t7_offer",
                "enlisted_promotion_t7_response",
                "close_window",
                GetLocalizedText(
                        "{=enlisted_promotion_t7_offer}You have proven yourself worthy. The rank of commander is yours, along with twenty soldiers to train and lead. Make them into warriors worthy of our banner.")
                    .ToString(),
                null,
                OnAcceptCommanderPromotion,
                110);

            // Lord's response to enlistment request
            starter.AddDialogLine(
                "enlisted_lord_accepts",
                "enlisted_enlistment_response",
                "enlisted_service_terms",
                GetLocalizedText(
                        "{=enlisted_lord_accepts}I see the steel in your eyes. You'll do. But know this - I expect loyalty, discipline, and courage. Here are my terms...")
                    .ToString(),
                null,
                null,
                110);

            // Lord's response to grace re-assignment
            starter.AddDialogLine(
                "enlisted_lord_accepts_grace",
                "enlisted_enlistment_response_grace",
                "enlisted_service_terms",
                GetLocalizedText(
                        "{=enlisted_lord_accepts_grace}A soldier who seeks a new banner rather than deserting? That speaks well of your character. I welcome you to my company. These are my terms...")
                    .ToString(),
                null,
                null,
                110);

            // Service terms and confirmation (3-year initial term as per user request)
            starter.AddDialogLine(
                "enlisted_service_terms_details",
                "enlisted_service_terms",
                "enlisted_confirm_service",
                GetLocalizedText(
                        "{=enlisted_service_terms_details}You will march when I say march, fight when I say fight, and hold the line when all seems lost. Your term is three years. In return, you shall have daily wages and a place by the fire. Do we have an accord?")
                    .ToString(),
                null,
                null,
                110);

            // Player accepts service
            starter.AddPlayerLine(
                "enlisted_accept_service",
                "enlisted_confirm_service",
                "close_window",
                GetLocalizedText("{=enlisted_accept_service}You have my oath, my lord. Point me at the enemy.")
                    .ToString(),
                null,
                OnAcceptEnlistment,
                110);

            // Player declines service
            starter.AddPlayerLine(
                "enlisted_decline_service",
                "enlisted_confirm_service",
                "lord_pretalk",
                GetLocalizedText(
                        "{=enlisted_decline_service}Your offer is generous, my lord, but I must think on it. Perhaps another time.")
                    .ToString(),
                null,
                null,
                110);
        }

        /// <summary>
        ///     Add retirement conversation flow with full benefits explanation.
        /// </summary>
        private void AddRetirementDialogs(CampaignGameStarter starter)
        {
            // Retirement/discharge is requested via Camp -> Leave Service -> Request Discharge (Pending Discharge -> Final Muster).
            // Conversations only provide guidance so we don't maintain a second conflicting retirement system here.
            starter.AddPlayerLine(
                "enlisted_discuss_discharge_guidance",
                "enlisted_service_options",
                "enlisted_discharge_guidance",
                GetLocalizedText(
                        "{=enlisted_discuss_discharge_guidance}My lord, if I wished to leave your service, how would I do so properly?")
                    .ToString(),
                CanDiscussDischargeGuidance,
                null,
                109);

            starter.AddDialogLine(
                "enlisted_discharge_guidance_text",
                "enlisted_discharge_guidance",
                "close_window",
                GetLocalizedText(
                        "{=enlisted_discharge_guidance_text}If you wish to leave my service, do it properly. Make camp and request discharge. Your final pay and discharge papers will be handled at the next muster.")
                    .ToString(),
                null,
                null,
                110);

#if false
            // First-term retirement discussion (available after 3 years) - kingdom lords
            starter.AddPlayerLine(
                "enlisted_discuss_retirement",
                "enlisted_service_options",
                "enlisted_retirement_benefits",
                GetLocalizedText(
                        "{=enlisted_discuss_retirement}My lord, I have served faithfully these many months. The time has come to discuss my future.")
                    .ToString(),
                CanDiscussFirstTermRetirement,
                null,
                109); // Higher priority than regular retirement

            // First-term retirement discussion - minor faction lords (mercenary tone)
            starter.AddPlayerLine(
                "enlisted_minor_discuss_retirement",
                "enlisted_service_options",
                "enlisted_minor_retirement_benefits",
                GetLocalizedText(
                        "{=enlisted_minor_discuss_retirement}Captain, I've been riding with the company long enough. Time we settled accounts.")
                    .ToString(),
                CanDiscussMinorFactionRetirement,
                null,
                110); // Higher priority than kingdom version

            // Minor faction lord explains benefits (mercenary tone)
            starter.AddDialogLine(
                "enlisted_minor_retirement_benefits_explanation",
                "enlisted_minor_retirement_benefits",
                "enlisted_minor_retirement_choice",
                GetLocalizedText(
                        "{=enlisted_minor_retirement_benefits}You've earned your share and then some. Take your coin now and walk free, or stick around another year - I'll sweeten the pot. Your call.")
                    .ToString(),
                null,
                null,
                110);

            // Player accepts retirement from minor faction (with retinue - goes to troop choice)
            starter.AddPlayerLine(
                "enlisted_minor_accept_retirement_with_retinue",
                "enlisted_minor_retirement_choice",
                "enlisted_minor_retinue_ask",
                GetLocalizedText(
                        "{=enlisted_minor_accept_retirement}I'll take my cut now. It's been good riding with you.")
                    .ToString(),
                HasRetinueForRetirement,
                null,
                111); // Higher priority when has retinue

            // Player accepts retirement from minor faction (no retinue - direct farewell)
            starter.AddPlayerLine(
                "enlisted_minor_accept_retirement_no_retinue",
                "enlisted_minor_retirement_choice",
                "enlisted_minor_retirement_farewell",
                GetLocalizedText(
                        "{=enlisted_minor_accept_retirement}I'll take my cut now. It's been good riding with you.")
                    .ToString(),
                () => !HasRetinueForRetirement(),
                OnAcceptFirstTermRetirement,
                110);

            // Minor faction captain asks about the troops
            starter.AddDialogLine(
                "enlisted_minor_retinue_ask_text",
                "enlisted_minor_retinue_ask",
                "enlisted_minor_retinue_choice",
                GetLocalizedText(
                        "{=enlisted_minor_retinue_ask}Your lads have been asking around. Word is they'd rather follow you than draw army pay. Your call, fighter.")
                    .ToString(),
                null,
                null,
                110);

            // Player keeps troops (minor faction)
            starter.AddPlayerLine(
                "enlisted_minor_keep_troops",
                "enlisted_minor_retinue_choice",
                "enlisted_minor_farewell_with_troops",
                GetLocalizedText(
                        "{=enlisted_minor_keep_troops}Tell them to pack their gear. We ride together.")
                    .ToString(),
                null,
                OnKeepTroopsRetirement,
                110);

            // Player releases troops (minor faction)
            starter.AddPlayerLine(
                "enlisted_minor_release_troops",
                "enlisted_minor_retinue_choice",
                "enlisted_minor_retirement_farewell",
                GetLocalizedText(
                        "{=enlisted_minor_release_troops}They're good fighters. The company needs them more than I do.")
                    .ToString(),
                null,
                OnAcceptFirstTermRetirement,
                110);

            // Minor faction farewell when keeping troops
            starter.AddDialogLine(
                "enlisted_minor_farewell_with_troops_text",
                "enlisted_minor_farewell_with_troops",
                "close_window",
                GetRetinueFarewellText(isMinorFaction: true),
                null,
                OnFinalizeRetirementWithTroops,
                110);

            // Player accepts re-enlistment with minor faction (1-year term)
            starter.AddPlayerLine(
                "enlisted_minor_accept_reenlist",
                "enlisted_minor_retirement_choice",
                "enlisted_minor_reenlist_confirmed",
                GetLocalizedText(
                        "{=enlisted_minor_accept_reenlist}Coin's good, company's better. I'll stick around for that bonus.")
                    .ToString(),
                null,
                OnAcceptFirstTermReenlistBonus,
                110);

            // Player needs time to decide (minor faction)
            starter.AddPlayerLine(
                "enlisted_minor_retirement_later",
                "enlisted_minor_retirement_choice",
                "close_window",
                GetLocalizedText(
                        "{=enlisted_minor_retirement_later}Let me think on it. I'll find you when I've decided.")
                    .ToString(),
                null,
                null,
                110);

            // Minor faction lord's farewell on retirement
            starter.AddDialogLine(
                "enlisted_minor_retirement_farewell_text",
                "enlisted_minor_retirement_farewell",
                "close_window",
                GetLocalizedText(
                        "{=enlisted_minor_retirement_farewell}Fair winds, fighter. You've done right by the company. If you ever need work again, you know where to find us.")
                    .ToString(),
                null,
                null,
                110);

            // Minor faction lord confirms re-enlistment
            starter.AddDialogLine(
                "enlisted_minor_reenlist_confirmed",
                "enlisted_minor_reenlist_confirmed",
                "close_window",
                GetLocalizedText(
                        "{=enlisted_minor_reenlist_confirmed}Smart choice. The lads will be glad to have you. Your bonus is guaranteed when the year's up. Now let's find some trouble.")
                    .ToString(),
                null,
                null,
                110);

            // Lord explains benefits (mentions 1-year re-enlistment term) - kingdom lords
            starter.AddDialogLine(
                "enlisted_retirement_benefits_explanation",
                "enlisted_retirement_benefits",
                "enlisted_retirement_choice",
                GetLocalizedText(
                        "{=enlisted_retirement_benefits_explanation}You have served with honor, and I do not forget loyalty. Retire now and you leave with gold in your pocket, my personal letter of recommendation, and the respect of this kingdom. Or... stay another year, and I shall double your severance. The choice is yours.")
                    .ToString(),
                null,
                null,
                110);

            // Player accepts retirement with benefits (with retinue - goes to troop choice)
            starter.AddPlayerLine(
                "enlisted_accept_retirement_with_retinue",
                "enlisted_retirement_choice",
                "enlisted_retinue_ask",
                GetLocalizedText(
                        "{=enlisted_accept_retirement}I thank you for everything, my lord. It has been an honor to serve under your banner.")
                    .ToString(),
                HasRetinueForRetirement,
                null,
                111); // Higher priority when has retinue

            // Player accepts retirement with benefits (no retinue - direct farewell)
            starter.AddPlayerLine(
                "enlisted_accept_retirement_no_retinue",
                "enlisted_retirement_choice",
                "enlisted_retirement_farewell",
                GetLocalizedText(
                        "{=enlisted_accept_retirement}I thank you for everything, my lord. It has been an honor to serve under your banner.")
                    .ToString(),
                () => !HasRetinueForRetirement(),
                OnAcceptFirstTermRetirement,
                110);

            // Kingdom lord asks about the troops
            starter.AddDialogLine(
                "enlisted_retinue_ask_text",
                "enlisted_retinue_ask",
                "enlisted_retinue_choice",
                GetRetinueAskText(),
                null,
                null,
                110);

            // Player keeps troops (kingdom)
            starter.AddPlayerLine(
                "enlisted_keep_troops",
                "enlisted_retinue_choice",
                "enlisted_farewell_with_troops",
                GetLocalizedText(
                        "{=enlisted_keep_troops}My men have chosen to march with me, my lord. We've bled together - that bond does not break so easily.")
                    .ToString(),
                null,
                OnKeepTroopsRetirement,
                110);

            // Player releases troops (kingdom)
            starter.AddPlayerLine(
                "enlisted_release_troops",
                "enlisted_retinue_choice",
                "enlisted_retirement_farewell",
                GetLocalizedText(
                        "{=enlisted_release_troops}Let them return to the army. They signed on to serve the kingdom, not one man.")
                    .ToString(),
                null,
                OnAcceptFirstTermRetirement,
                110);

            // Kingdom farewell when keeping troops
            starter.AddDialogLine(
                "enlisted_farewell_with_troops_text",
                "enlisted_farewell_with_troops",
                "close_window",
                GetRetinueFarewellText(isMinorFaction: false),
                null,
                OnFinalizeRetirementWithTroops,
                110);

            // Player accepts re-enlistment bonus (1-year term)
            starter.AddPlayerLine(
                "enlisted_accept_reenlist_bonus",
                "enlisted_retirement_choice",
                "enlisted_reenlist_confirmed",
                GetLocalizedText(
                        "{=enlisted_accept_reenlist_bonus}The battlefield is the only home I know, my lord. I shall stay and earn that gold.")
                    .ToString(),
                null,
                OnAcceptFirstTermReenlistBonus,
                110);

            // Player needs time to decide
            starter.AddPlayerLine(
                "enlisted_retirement_later",
                "enlisted_retirement_choice",
                "close_window",
                GetLocalizedText(
                        "{=enlisted_retirement_later}This is not a decision to make lightly. Give me time to consider, my lord.")
                    .ToString(),
                null,
                null,
                110);

            // Lord's farewell on retirement
            starter.AddDialogLine(
                "enlisted_retirement_farewell_text",
                "enlisted_retirement_farewell",
                "close_window",
                GetLocalizedText(
                        "{=enlisted_retirement_farewell}Go well, soldier. You have earned your peace. Should you ever wish to return, my door remains open.")
                    .ToString(),
                null,
                null,
                110);

            // Lord confirms re-enlistment (1-year term)
            starter.AddDialogLine(
                "enlisted_reenlist_bonus_confirmed",
                "enlisted_reenlist_confirmed",
                "close_window",
                GetLocalizedText(
                        "{=enlisted_reenlist_bonus_confirmed}Ha! I knew you had more fight in you. Your gold will be waiting when the year is done. Now get back to your post.")
                    .ToString(),
                null,
                null,
                110);

            // Renewal term complete dialog (after 1-year renewal) - kingdom lords
            starter.AddPlayerLine(
                "enlisted_renewal_complete",
                "enlisted_service_options",
                "enlisted_renewal_options",
                GetLocalizedText(
                    "{=enlisted_renewal_complete}My lord, another year has passed. What becomes of me now?").ToString(),
                CanDiscussRenewalTermEnd,
                null,
                108);

            // Renewal term complete dialog - minor faction lords
            starter.AddPlayerLine(
                "enlisted_minor_renewal_complete",
                "enlisted_service_options",
                "enlisted_minor_renewal_options",
                GetLocalizedText(
                    "{=enlisted_minor_renewal_complete}Captain, the year's up. What's the score?").ToString(),
                CanDiscussMinorFactionRenewalEnd,
                null,
                109); // Higher priority

            // Minor faction lord explains renewal options
            starter.AddDialogLine(
                "enlisted_minor_renewal_options_explanation",
                "enlisted_minor_renewal_options",
                "enlisted_minor_renewal_choice",
                GetLocalizedText(
                        "{=enlisted_minor_renewal_options}Another year done, and you're still breathing. Take your gold and go free, or sign on for another round - same deal as before. What'll it be?")
                    .ToString(),
                null,
                null,
                110);

            // Player accepts renewal discharge from minor faction (with retinue - goes to troop choice)
            starter.AddPlayerLine(
                "enlisted_minor_accept_renewal_discharge_with_retinue",
                "enlisted_minor_renewal_choice",
                "enlisted_minor_renewal_retinue_ask",
                GetLocalizedText(
                        "{=enlisted_minor_renewal_discharge}I'll take my share. Time to see what else is out there.")
                    .ToString(),
                HasRetinueForRetirement,
                null,
                111); // Higher priority when has retinue

            // Player accepts renewal discharge from minor faction (no retinue - direct farewell)
            starter.AddPlayerLine(
                "enlisted_minor_accept_renewal_discharge_no_retinue",
                "enlisted_minor_renewal_choice",
                "enlisted_minor_renewal_farewell",
                GetLocalizedText(
                        "{=enlisted_minor_renewal_discharge}I'll take my share. Time to see what else is out there.")
                    .ToString(),
                () => !HasRetinueForRetirement(),
                OnAcceptRenewalDischarge,
                110);

            // Minor faction captain asks about troops on renewal discharge
            starter.AddDialogLine(
                "enlisted_minor_renewal_retinue_ask_text",
                "enlisted_minor_renewal_retinue_ask",
                "enlisted_minor_renewal_retinue_choice",
                GetLocalizedText(
                        "{=enlisted_minor_retinue_ask}Your lads have been asking around. Word is they'd rather follow you than draw army pay. Your call, fighter.")
                    .ToString(),
                null,
                null,
                110);

            // Player keeps troops on renewal discharge (minor faction)
            starter.AddPlayerLine(
                "enlisted_minor_renewal_keep_troops",
                "enlisted_minor_renewal_retinue_choice",
                "enlisted_minor_renewal_farewell_with_troops",
                GetLocalizedText(
                        "{=enlisted_minor_keep_troops}Tell them to pack their gear. We ride together.")
                    .ToString(),
                null,
                OnKeepTroopsRetirement,
                110);

            // Player releases troops on renewal discharge (minor faction)
            starter.AddPlayerLine(
                "enlisted_minor_renewal_release_troops",
                "enlisted_minor_renewal_retinue_choice",
                "enlisted_minor_renewal_farewell",
                GetLocalizedText(
                        "{=enlisted_minor_release_troops}They're good fighters. The company needs them more than I do.")
                    .ToString(),
                null,
                OnAcceptRenewalDischarge,
                110);

            // Minor faction farewell when keeping troops on renewal
            starter.AddDialogLine(
                "enlisted_minor_renewal_farewell_with_troops_text",
                "enlisted_minor_renewal_farewell_with_troops",
                "close_window",
                GetRetinueFarewellText(isMinorFaction: true),
                null,
                OnFinalizeRenewalWithTroops,
                110);

            // Player continues with minor faction (another 1-year term)
            starter.AddPlayerLine(
                "enlisted_minor_continue_service",
                "enlisted_minor_renewal_choice",
                "enlisted_minor_continue_confirmed",
                GetLocalizedText(
                        "{=enlisted_minor_continue_service}The road's been good to me here. I'm in for another year.")
                    .ToString(),
                null,
                OnContinueService,
                110);

            // Minor faction lord's farewell on renewal discharge
            starter.AddDialogLine(
                "enlisted_minor_renewal_farewell_text",
                "enlisted_minor_renewal_farewell",
                "close_window",
                GetLocalizedText(
                        "{=enlisted_minor_renewal_farewell}Your coin's counted and your horse is yours. Good luck out there - and if you hear of any good contracts, send word.")
                    .ToString(),
                null,
                null,
                110);

            // Minor faction lord confirms continued service
            starter.AddDialogLine(
                "enlisted_minor_continue_confirmed",
                "enlisted_minor_continue_confirmed",
                "close_window",
                GetLocalizedText(
                        "{=enlisted_minor_continue_confirmed}Good man. The company's better for having you. Same terms, same coin - let's get back to work.")
                    .ToString(),
                null,
                null,
                110);

            // Lord explains renewal options (1-year terms) - kingdom lords
            starter.AddDialogLine(
                "enlisted_renewal_options_explanation",
                "enlisted_renewal_options",
                "enlisted_renewal_choice",
                GetLocalizedText(
                        "{=enlisted_renewal_options}You have proven yourself time and again. Take your discharge with five thousand gold and my blessing, or stay another year with a bonus to match. What say you?")
                    .ToString(),
                null,
                null,
                110);

            // Player accepts renewal discharge (with retinue - goes to troop choice)
            starter.AddPlayerLine(
                "enlisted_accept_renewal_discharge_with_retinue",
                "enlisted_renewal_choice",
                "enlisted_renewal_retinue_ask",
                GetLocalizedText(
                        "{=enlisted_renewal_discharge}It is time I sought my own path, my lord. I thank you for the opportunity.")
                    .ToString(),
                HasRetinueForRetirement,
                null,
                111); // Higher priority when has retinue

            // Player accepts renewal discharge (no retinue - direct farewell)
            starter.AddPlayerLine(
                "enlisted_accept_renewal_discharge_no_retinue",
                "enlisted_renewal_choice",
                "enlisted_renewal_farewell",
                GetLocalizedText(
                        "{=enlisted_renewal_discharge}It is time I sought my own path, my lord. I thank you for the opportunity.")
                    .ToString(),
                () => !HasRetinueForRetirement(),
                OnAcceptRenewalDischarge,
                110);

            // Kingdom lord asks about troops on renewal discharge
            starter.AddDialogLine(
                "enlisted_renewal_retinue_ask_text",
                "enlisted_renewal_retinue_ask",
                "enlisted_renewal_retinue_choice",
                GetRetinueAskText(),
                null,
                null,
                110);

            // Player keeps troops on renewal discharge (kingdom)
            starter.AddPlayerLine(
                "enlisted_renewal_keep_troops",
                "enlisted_renewal_retinue_choice",
                "enlisted_renewal_farewell_with_troops",
                GetLocalizedText(
                        "{=enlisted_keep_troops}My men have chosen to march with me, my lord. We've bled together - that bond does not break so easily.")
                    .ToString(),
                null,
                OnKeepTroopsRetirement,
                110);

            // Player releases troops on renewal discharge (kingdom)
            starter.AddPlayerLine(
                "enlisted_renewal_release_troops",
                "enlisted_renewal_retinue_choice",
                "enlisted_renewal_farewell",
                GetLocalizedText(
                        "{=enlisted_release_troops}Let them return to the army. They signed on to serve the kingdom, not one man.")
                    .ToString(),
                null,
                OnAcceptRenewalDischarge,
                110);

            // Kingdom farewell when keeping troops on renewal
            starter.AddDialogLine(
                "enlisted_renewal_farewell_with_troops_text",
                "enlisted_renewal_farewell_with_troops",
                "close_window",
                GetRetinueFarewellText(isMinorFaction: false),
                null,
                OnFinalizeRenewalWithTroops,
                110);

            // Player continues service (another 1-year term)
            starter.AddPlayerLine(
                "enlisted_continue_service",
                "enlisted_renewal_choice",
                "enlisted_continue_confirmed",
                GetLocalizedText(
                        "{=enlisted_continue_service}Why would I leave now? We have enemies yet to crush. Count me in for another year.")
                    .ToString(),
                null,
                OnContinueService,
                110);

            // Lord's farewell on renewal discharge
            starter.AddDialogLine(
                "enlisted_renewal_farewell_text",
                "enlisted_renewal_farewell",
                "close_window",
                GetLocalizedText(
                        "{=enlisted_renewal_farewell}Then this is farewell, for now. You have been a fine soldier. May the winds carry you to fortune.")
                    .ToString(),
                null,
                null,
                110);

            // Lord confirms continued service
            starter.AddDialogLine(
                "enlisted_continue_service_confirmed",
                "enlisted_continue_confirmed",
                "close_window",
                GetLocalizedText(
                        "{=enlisted_continue_confirmed}Ha! That's the spirit! I knew I could count on you. Another year, then. Try not to get yourself killed.")
                    .ToString(),
                null,
                null,
                110);

            // Post-cooldown re-enlistment option (veteran returning)
            starter.AddPlayerLine(
                "enlisted_reenlist_after_cooldown",
                "enlisted_service_options",
                "enlisted_veteran_welcome",
                GetLocalizedText(
                        "{=enlisted_reenlist_cooldown}My lord, you may remember me - I served this realm before. The quiet life does not suit me. Will you have me back?")
                    .ToString(),
                CanReEnlistAfterCooldown,
                null,
                107);

            // Lord welcomes back veteran (1-year term for veterans)
            starter.AddDialogLine(
                "enlisted_veteran_welcome_back",
                "enlisted_veteran_welcome",
                "enlisted_veteran_confirm",
                GetLocalizedText(
                        "{=enlisted_veteran_welcome}I remember you! A good soldier returns to the fold. Your old rank awaits, though you will need to choose your specialty anew. Serve one year and you leave with gold in hand.")
                    .ToString(),
                null,
                null,
                110);

            // Veteran confirms re-enlistment
            starter.AddPlayerLine(
                "enlisted_veteran_accept",
                "enlisted_veteran_confirm",
                "enlisted_veteran_accepted",
                GetLocalizedText(
                        "{=enlisted_veteran_accept}My sword arm is strong as ever, my lord. Where do you need me?")
                    .ToString(),
                null,
                OnVeteranReEnlist,
                110);

            // Veteran decides not to re-enlist
            starter.AddPlayerLine(
                "enlisted_veteran_decline",
                "enlisted_veteran_confirm",
                "close_window",
                GetLocalizedText(
                        "{=enlisted_veteran_decline}Forgive me, my lord. The call to arms is strong, but I am not yet ready. Perhaps another day.")
                    .ToString(),
                null,
                null,
                110);

            // Lord confirms veteran re-enlistment
            starter.AddDialogLine(
                "enlisted_veteran_accepted_text",
                "enlisted_veteran_accepted",
                "close_window",
                GetLocalizedText(
                        "{=enlisted_veteran_accepted}Good to have you back in the ranks. Report to the Master at Arms to choose your role, then find your company.")
                    .ToString(),
                null,
                null,
                110);

            // Simple early discharge (for those not eligible for full retirement) - kingdom lords
            starter.AddPlayerLine(
                "enlisted_request_early_discharge",
                "enlisted_service_options",
                "enlisted_early_discharge_response",
                GetLocalizedText(
                        "{=enlisted_early_discharge}My lord, I must ask to be released from my oath. I cannot continue.")
                    .ToString(),
                CanRequestEarlyDischarge,
                null,
                115); // Lower priority - shows when not eligible for full retirement

            // Simple early discharge - minor faction lords
            starter.AddPlayerLine(
                "enlisted_minor_request_early_discharge",
                "enlisted_service_options",
                "enlisted_minor_early_discharge_response",
                GetLocalizedText(
                        "{=enlisted_minor_early_discharge}Captain, I need to break contract early. Something's come up.")
                    .ToString(),
                CanRequestMinorFactionEarlyDischarge,
                null,
                116); // Higher priority

            // Lord grants early discharge (no benefits) - kingdom lords
            starter.AddDialogLine(
                "enlisted_early_discharge_granted",
                "enlisted_early_discharge_response",
                "close_window",
                GetLocalizedText(
                        "{=enlisted_early_discharge_granted}I release you from your oath. You leave without the honors of a full term, but you are free. Go, and think carefully before you take up arms again.")
                    .ToString(),
                null,
                OnGrantEarlyDischarge,
                110);

            // Minor faction lord grants early discharge
            starter.AddDialogLine(
                "enlisted_minor_early_discharge_granted",
                "enlisted_minor_early_discharge_response",
                "close_window",
                GetLocalizedText(
                        "{=enlisted_minor_early_discharge_granted}Contract's done then. No hard feelings - everyone's got their reasons. Don't expect a warm welcome if you come crawling back soon, though.")
                    .ToString(),
                null,
                OnGrantEarlyDischarge,
                110);
#endif

            // Exit option - always available as fallback from service options
            // Uses default priority (100) - shows last among service options (others use 110-115)
            starter.AddPlayerLine(
                "enlisted_service_nevermind",
                "enlisted_service_options",
                "close_window",
                GetLocalizedText(
                        "{=enlisted_service_nevermind}Forgive me, my lord. I spoke out of turn. I have nothing to discuss at this time.")
                    .ToString(),
                null,
                null);
        }

        // ========================================================================
        // QUARTERMASTER HERO DIALOG SYSTEM
        // Dialog tree for interaction with persistent quartermaster NPC
        // ========================================================================

        /// <summary>
        ///     Add quartermaster NPC dialog tree.
        ///     Player opens conversation via "Visit Quartermaster" menu option.
        /// </summary>
        private void AddQuartermasterDialogs(CampaignGameStarter starter)
        {
            try
            {
                // ========================================
                // QUARTERMASTER GREETING (Entry Point)
                // ========================================

                // First meeting greeting - introduction (uses dynamic text)
                starter.AddDialogLine(
                    "qm_greeting_first",
                    "start",
                    "qm_hub",
                    "{=qm_greeting_dynamic}{QM_GREETING}",
                    () => IsQuartermasterConversation() && !HasMetQuartermaster() && SetQuartermasterGreetingText(),
                    OnQuartermasterFirstMeeting,
                    200); // High priority for first meeting

                // Returning visit greeting - archetype + mood based (uses dynamic text)
                starter.AddDialogLine(
                    "qm_greeting_return",
                    "start",
                    "qm_hub",
                    "{=qm_greeting_dynamic}{QM_GREETING}",
                    () => IsQuartermasterConversation() && HasMetQuartermaster() && SetQuartermasterGreetingText(),
                    null,
                    199); // Slightly lower priority for returning visits

                // ========================================
                // QUARTERMASTER HUB (Main Options)
                // ========================================

                // Option 1: Browse Equipment → Opens existing menu system
                starter.AddPlayerLine(
                    "qm_request_equipment",
                    "qm_hub",
                    "qm_equipment_response",
                    "{=qm_request_equipment}I need equipment.",
                    null,
                    null);

                starter.AddDialogLine(
                    "qm_equipment_response",
                    "qm_equipment_response",
                    "close_window",
                    "{=qm_equipment_response}Let me see what I've got in stock for you.",
                    null,
                    OnQuartermasterEquipmentRequest);

                // Option 2: Sell Equipment → Opens existing returns menu
                starter.AddPlayerLine(
                    "qm_sell_equipment",
                    "qm_hub",
                    "qm_sell_response",
                    "{=qm_sell_equipment}I want to sell some equipment.",
                    CanSellEquipment,
                    null,
                    99);

                starter.AddDialogLine(
                    "qm_sell_response",
                    "qm_sell_response",
                    "close_window",
                    "{=qm_sell_response}[Quartermaster responds to sell request]",
                    null,
                    OnQuartermasterSellRequest);

                // Option 3: Provisions → Opens rations menu
                starter.AddPlayerLine(
                    "qm_request_provisions",
                    "qm_hub",
                    "qm_provisions_response",
                    "{=qm_request_provisions}I need better provisions.",
                    null,
                    null,
                    97);

                starter.AddDialogLine(
                    "qm_provisions_response",
                    "qm_provisions_response",
                    "close_window",
                    "{=qm_provisions_response}Let me show you what provisions I have available.",
                    null,
                    OnQuartermasterProvisionsRequest);

                // ========================================
                // PAYTENSION DIALOG OPTIONS
                // ========================================

                // Scoundrel Black Market (tension 40+)
                starter.AddPlayerLine(
                    "qm_black_market",
                    "qm_hub",
                    "qm_black_market_response",
                    "{=qm_black_market}I need... alternative supplies.",
                    IsScoundrelBlackMarketAvailable,
                    null,
                    96);

                starter.AddDialogLine(
                    "qm_black_market_response",
                    "qm_black_market_response",
                    "qm_hub",
                    "{=qm_black_market_response}Looking for opportunities? I might know some people who can help... for a price.",
                    null,
                    OnQuartermasterBlackMarket);

                // Believer Moral Guidance (tension 60+)
                starter.AddPlayerLine(
                    "qm_moral_guidance",
                    "qm_hub",
                    "qm_moral_guidance_response",
                    "{=qm_moral_guidance}The men are losing hope. Any guidance?",
                    IsBelieverGuidanceAvailable,
                    null,
                    95);

                starter.AddDialogLine(
                    "qm_moral_guidance_response",
                    "qm_moral_guidance_response",
                    "qm_hub",
                    "{=qm_moral_guidance_response}These are trying times, but faith endures. Remember why you serve.",
                    null,
                    OnQuartermasterMoralGuidance);

                // Veteran Survival Advice (tension 80+)
                starter.AddPlayerLine(
                    "qm_survival_advice",
                    "qm_hub",
                    "qm_survival_advice_response",
                    "{=qm_survival_advice}Should I... consider my options?",
                    IsVeteranAdviceAvailable,
                    null,
                    94);

                starter.AddDialogLine(
                    "qm_survival_advice_response",
                    "qm_survival_advice_response",
                    "qm_hub",
                    "{=qm_survival_advice_response}I've seen armies fall apart before. Keep your head down and watch for opportunities.",
                    null,
                    OnQuartermasterSurvivalAdvice);

                // Help the Lord suggestion (any archetype, tension 40+)
                starter.AddPlayerLine(
                    "qm_help_lord",
                    "qm_hub",
                    "qm_help_lord_response",
                    "{=qm_help_lord}Is there anything I can do to help with... the situation?",
                    IsHelpLordAvailable,
                    null,
                    93);

                starter.AddDialogLine(
                    "qm_help_lord_response",
                    "qm_help_lord_response",
                    "qm_hub",
                    "{=qm_help_lord_response}There are ways a loyal soldier could help. Collect debts, escort merchants, that sort of thing.",
                    null,
                    OnQuartermasterHelpLordSuggestion);

                // Option 4: Chat (Relationship building) - first time or high relationship
                starter.AddPlayerLine(
                    "qm_chat",
                    "qm_hub",
                    "qm_chat_response",
                    "{=qm_chat}How did you end up as quartermaster?",
                    CanChatWithQuartermaster,
                    null,
                    98);

                starter.AddDialogLine(
                    "qm_chat_response",
                    "qm_chat_response",
                    "qm_hub",
                    "{=qm_chat_response}It's a long story. Let's just say I ended up here after some... interesting experiences.",
                    null,
                    OnQuartermasterChat);

                // Option 4: Leave
                starter.AddPlayerLine(
                    "qm_leave",
                    "qm_hub",
                    "close_window",
                    "{=qm_leave}I'll be going.",
                    null,
                    null,
                    50);

                ModLogger.Info("DialogManager", "Quartermaster dialog tree registered");
            }
            catch (Exception ex)
            {
                ModLogger.Error("DialogManager", "Failed to register quartermaster dialogs", ex);
            }
        }

        #region Quartermaster Dialog Conditions

        /// <summary>
        ///     Sets the QM_GREETING text variable for dynamic greeting display.
        ///     Returns true always (used in condition chain).
        /// </summary>
        private bool SetQuartermasterGreetingText()
        {
            MBTextManager.SetTextVariable("QM_GREETING", GetQuartermasterGreeting());
            return true;
        }

        /// <summary>
        ///     Checks if the current conversation is with the quartermaster hero.
        /// </summary>
        private bool IsQuartermasterConversation()
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null || !enlistment.IsEnlisted)
            {
                return false;
            }

            var qm = enlistment.QuartermasterHero;
            return qm != null && qm.IsAlive && Hero.OneToOneConversationHero == qm;
        }

        /// <summary>
        ///     Checks if the player has met the current quartermaster before.
        /// </summary>
        private bool HasMetQuartermaster()
        {
            return EnlistmentBehavior.Instance?.HasMetQuartermaster == true;
        }

        /// <summary>
        ///     Checks if the player can sell equipment (has sellable items).
        /// </summary>
        private bool CanSellEquipment()
        {
            // Always available - the menu will show what can be sold
            return true;
        }

        /// <summary>
        ///     Checks if the player can chat with the quartermaster.
        ///     Available on first meeting or at high relationship (40+).
        /// </summary>
        private bool CanChatWithQuartermaster()
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null)
            {
                return false;
            }

            // Chat available: first time (not met), or high relationship
            return !enlistment.HasMetQuartermaster || enlistment.QuartermasterRelationship >= 40;
        }

        // ========================================
        // PAYTENSION DIALOG CONDITIONS
        // ========================================

        /// <summary>
        ///     Checks if Scoundrel black market option is available.
        ///     Requires: Scoundrel archetype + PayTension 40+ + relationship 20+
        /// </summary>
        private bool IsScoundrelBlackMarketAvailable()
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null)
            {
                return false;
            }

            return enlistment.QuartermasterArchetype == "scoundrel"
                && enlistment.PayTension >= 40
                && enlistment.QuartermasterRelationship >= 20;
        }

        /// <summary>
        ///     Checks if Believer moral guidance option is available.
        ///     Requires: Believer archetype + PayTension 60+
        /// </summary>
        private bool IsBelieverGuidanceAvailable()
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null)
            {
                return false;
            }

            return enlistment.QuartermasterArchetype == "believer"
                && enlistment.PayTension >= 60;
        }

        /// <summary>
        ///     Checks if Veteran survival advice option is available.
        ///     Requires: Veteran archetype + PayTension 80+
        /// </summary>
        private bool IsVeteranAdviceAvailable()
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null)
            {
                return false;
            }

            return enlistment.QuartermasterArchetype == "veteran"
                && enlistment.PayTension >= 80;
        }

        /// <summary>
        ///     Checks if "Help the Lord" suggestion is available.
        ///     Requires: PayTension 40+ (any archetype)
        /// </summary>
        private bool IsHelpLordAvailable()
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null)
            {
                return false;
            }

            return enlistment.PayTension >= 40;
        }

        #endregion

        #region Quartermaster Dialog Text Generation

        /// <summary>
        ///     Gets the quartermaster's greeting text based on archetype, mood, and first meeting status.
        ///     Called dynamically when the greeting dialog is displayed.
        /// </summary>
        public string GetQuartermasterGreeting()
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null)
            {
                return "What do you need, soldier?";
            }

            var archetype = enlistment.QuartermasterArchetype;
            var isFirstMeeting = !enlistment.HasMetQuartermaster;
            var mood = GetQuartermasterMood();
            var qmName = enlistment.QuartermasterHero?.Name?.ToString() ?? "Quartermaster";

            if (isFirstMeeting)
            {
                return GetFirstMeetingGreeting(archetype, qmName);
            }

            return GetReturningGreeting(archetype, mood);
        }

        /// <summary>
        ///     Gets the first-meeting introduction based on archetype.
        ///     Uses {QM_NAME} placeholder for quartermaster's actual name.
        /// </summary>
        private string GetFirstMeetingGreeting(string archetype, string qmName)
        {
            var text = archetype switch
            {
                "veteran" => new TextObject("So you're the new {PLAYER_RANK}. Name's {QM_NAME}. I run the baggage train. You need gear, you come to me. Clear?"),
                "merchant" => new TextObject("Ah, fresh meat! I'm {QM_NAME}, quartermaster. Everything here has a price, soldier. Even loyalty."),
                "bookkeeper" => new TextObject("New arrival. {QM_NAME}, quartermaster. Please familiarize yourself with Form 14-C for equipment requisitions."),
                "scoundrel" => new TextObject("Well, well. Another mouth to feed. I'm {QM_NAME}. Need anything... special?"),
                "believer" => new TextObject("Welcome, soldier. I am {QM_NAME}. The Lord provides for those who serve with honor. What do you seek?"),
                "eccentric" => new TextObject("Your name has a lucky number of letters. I'm {QM_NAME}. The stars say you'll need good armor."),
                _ => new TextObject("I'm {QM_NAME}, quartermaster. What do you need?")
            };
            
            text.SetTextVariable("QM_NAME", qmName);
            return text.ToString();
        }

        /// <summary>
        ///     Gets the returning visit greeting based on archetype, mood, and PayTension level.
        ///     Now includes tension-aware greetings at 40+, 60+, and 80+ thresholds.
        /// </summary>
        private string GetReturningGreeting(string archetype, string mood)
        {
            // Check PayTension for tension-aware greetings
            var tension = EnlistmentBehavior.Instance?.PayTension ?? 0;

            // Critical tension (80+) - all archetypes show concern
            if (tension >= 80)
            {
                return GetCriticalTensionGreeting(archetype);
            }

            // High tension (60+) - archetype-specific warnings
            if (tension >= 60)
            {
                return GetHighTensionGreeting(archetype);
            }

            // Moderate tension (40+) - subtle hints
            if (tension >= 40)
            {
                return GetModerateTensionGreeting(archetype);
            }

            // Normal greetings based on mood
            return (archetype, mood) switch
            {
                ("veteran", "content") => "Back again? What do you need?",
                ("veteran", "stressed") => "Busy day. Make it quick.",
                ("veteran", "grim") => "Supply's tight. What?",
                ("merchant", "content") => "Good to see you. What can I sell you today?",
                ("merchant", "stressed") => "Prices are up. Supply issues. What do you need?",
                ("merchant", "grim") => "Not a good time for shopping. Make it fast.",
                ("bookkeeper", "content") => "Ah, soldier. Equipment requisition? Let me find the form.",
                ("bookkeeper", "stressed") => "The inventory is a mess. What is it?",
                ("bookkeeper", "grim") => "Everything is behind schedule. What?",
                ("scoundrel", "content") => "My favorite customer. What are you looking for today?",
                ("scoundrel", "stressed") => "Things are getting tight around here. Need something?",
                ("scoundrel", "grim") => "Bad times, friend. Very bad times. What do you need?",
                ("believer", "content") => "The Lord's blessings upon you. How may I serve?",
                ("believer", "stressed") => "Trying times test our faith. What do you need?",
                ("believer", "grim") => "Pray for us all, soldier. What can I do for you?",
                ("eccentric", "content") => "The ravens spoke of your coming. What do you seek?",
                ("eccentric", "stressed") => "The stars are misaligned today. Be careful what you ask for.",
                ("eccentric", "grim") => "Dark omens everywhere. What?",
                _ => "What do you need, soldier?"
            };
        }

        /// <summary>
        ///     Gets greeting when PayTension is 80+ (critical).
        ///     All archetypes express serious concern about the situation.
        /// </summary>
        private string GetCriticalTensionGreeting(string archetype)
        {
            return archetype switch
            {
                "veteran" => "You're still here? Half the company's talking desertion. The lord better pay up soon or there won't be anyone left to pay.",
                "merchant" => "Ah, friend. These are dark days. Even my best suppliers won't extend credit anymore. The whole operation is falling apart.",
                "bookkeeper" => "The accounts... they're a disaster. Unpaid wages everywhere. I don't know how much longer we can hold things together.",
                "scoundrel" => "Looking to disappear? I know some people... for a price. This whole army is one bad battle away from scattering like leaves.",
                "believer" => "The men's faith is being tested beyond measure. Many are losing hope. Pray with me, soldier, for we are in dire straits.",
                "eccentric" => "The stars scream of betrayal and abandonment. The wheel of fortune has turned against us. Do you hear the whispers of the damned?",
                _ => "Things are very bad. What do you need?"
            };
        }

        /// <summary>
        ///     Gets greeting when PayTension is 60-79 (high).
        ///     Archetypes provide specific warnings and hints.
        /// </summary>
        private string GetHighTensionGreeting(string archetype)
        {
            return archetype switch
            {
                "veteran" => "The men are angry. Real angry. I've seen mutinies start from less than this. Watch your back.",
                "merchant" => "Payment's been... irregular. My credit with suppliers is stretched thin. We may need to discuss... alternatives.",
                "bookkeeper" => "The ledgers don't balance. Too many 'delayed' entries. This is unsustainable.",
                "scoundrel" => "Desperate times, friend. People are getting creative about finding coin. If you need anything... unofficial...",
                "believer" => "The men's spirits are breaking. Some are losing their way. Perhaps if you could help the lord find coin, their faith would be restored.",
                "eccentric" => "I dreamed of empty coffers and marching boots heading home. An ill omen. Very ill.",
                _ => "Times are getting hard. What do you need?"
            };
        }

        /// <summary>
        ///     Gets greeting when PayTension is 40-59 (moderate).
        ///     Subtle hints that something is wrong.
        /// </summary>
        private string GetModerateTensionGreeting(string archetype)
        {
            return archetype switch
            {
                "veteran" => "The lads are grumbling about pay. Nothing serious yet, but keep your ears open.",
                "merchant" => "Cash flow is getting tight. Might be some opportunities for a resourceful soldier, if you take my meaning.",
                "bookkeeper" => "There are some... irregularities in the pay records. I'm sure it will be sorted soon.",
                "scoundrel" => "Heard some interesting rumors about the lord's finances. Nothing that concerns you, of course... unless you want it to.",
                "believer" => "Some of the men are wavering. Material concerns are clouding their devotion. Perhaps you could set an example.",
                "eccentric" => "The coins in my pouch keep spinning counterclockwise. That usually means someone isn't getting paid.",
                _ => "What brings you here today?"
            };
        }

        /// <summary>
        ///     Gets the quartermaster's response to equipment request based on archetype.
        ///     Note: Reserved for future dynamic dialog enhancement.
        /// </summary>
        [Obsolete("Reserved for future dynamic dialog. Currently using static localized strings.")]
        public string GetEquipmentResponseLine()
        {
            var archetype = EnlistmentBehavior.Instance?.QuartermasterArchetype ?? "veteran";

            return archetype switch
            {
                "veteran" => "Equipment requisition. Let me see what I've got.",
                "merchant" => "Looking to upgrade? Smart. Follow me to the armory.",
                "bookkeeper" => "Equipment requisition approved. Please review the inventory.",
                "scoundrel" => "Official channels or... off the books?",
                "believer" => "The armory is open. May you choose wisely.",
                "eccentric" => "The stars favor shields today. Or swords. Maybe both.",
                _ => "This way."
            };
        }

        /// <summary>
        ///     Gets the quartermaster's response to sell request based on mood.
        /// </summary>
        public string GetSellResponseLine()
        {
            var mood = GetQuartermasterMood();

            return mood switch
            {
                "content" => "Let's see what you've got. I'll give you fair value.",
                "stressed" => "Selling? Fine. Prices are low right now.",
                "grim" => "Not buying much today. Supply issues.",
                _ => "What are you selling?"
            };
        }

        /// <summary>
        ///     Gets the quartermaster's response to provisions request based on archetype.
        ///     Note: Reserved for future dynamic dialog enhancement.
        /// </summary>
        [Obsolete("Reserved for future dynamic dialog. Currently using static localized strings.")]
        public string GetProvisionsResponseLine()
        {
            var archetype = EnlistmentBehavior.Instance?.QuartermasterArchetype ?? "veteran";

            return archetype switch
            {
                "veteran" => "Hungry? Smart soldier keeps his belly full. Let me show you what I've got.",
                "merchant" => "Looking to upgrade from hardtack? I've got options. Quality costs, mind you.",
                "bookkeeper" => "Provisions requisition. I'll need to check the allocation limits for your rank.",
                "scoundrel" => "Want something better than the slop they serve the regulars? I might know a source.",
                "believer" => "The body is a temple. You're wise to nourish it properly. What do you need?",
                "eccentric" => "The ravens say the meat is fresh today. The bread too. Maybe. Let me show you.",
                _ => "Looking for better rations? Follow me."
            };
        }

        /// <summary>
        ///     Gets the quartermaster's backstory based on archetype.
        ///     Uses {LORD_NAME} placeholder for dynamic lord reference.
        ///     Note: Reserved for future dynamic dialog enhancement.
        /// </summary>
        [Obsolete("Reserved for future dynamic dialog. Currently using static localized strings.")]
        public string GetQuartermasterBackstory()
        {
            var enlistment = EnlistmentBehavior.Instance;
            var archetype = enlistment?.QuartermasterArchetype ?? "veteran";

            var text = archetype switch
            {
                "veteran" => new TextObject("Lost my sword arm at Pendraic. {LORD_NAME} gave me this posting. Beats starving. Now I make sure other soldiers don't go hungry."),
                "merchant" => new TextObject("I ran caravans for twenty years. Profitable, but dangerous. This is safer and steadier. Plus, I know every trader from here to Zeonica."),
                "bookkeeper" => new TextObject("I clerked for the imperial bureaucracy for fifteen years. When the war came, they needed someone who could count. Here I am."),
                "scoundrel" => new TextObject("Let's just say I had some... disagreements... with the city guard. {LORD_NAME} offered me a choice: noose or supply wagon. Easy choice."),
                "believer" => new TextObject("I served as a priest before the war. But faith without works is dead. Now I serve by ensuring the faithful are fed and armed."),
                "eccentric" => new TextObject("The stars told me I'd manage supplies for a great army. Twelve years ago. On a Tuesday. In the rain. And here I am, just as foretold."),
                _ => new TextObject("I've been doing this for years. It's honest work.")
            };
            
            // Direct unit name variables are being updated.
            if (enlistment != null)
            {
                // Dynamic name replacement will be updated to support the new unit structure.
            }
            
            return text.ToString();
        }

        // ========================================
        // PAYTENSION DIALOG TEXT GENERATION
        // Reserved for future dynamic dialog enhancement.
        // Currently using static localized strings in dialog registration.
        // ========================================

        /// <summary>
        ///     Gets the Scoundrel's black market response.
        ///     Note: Reserved for future dynamic dialog enhancement.
        /// </summary>
        [Obsolete("Reserved for future dynamic dialog. Currently using static localized strings.")]
        public string GetBlackMarketResponseLine()
        {
            var tension = EnlistmentBehavior.Instance?.PayTension ?? 0;

            if (tension >= 80)
            {
                return "Alternative supplies? Ha! Half the camp is already selling what they can carry. " +
                       "I know some merchants who don't ask questions... for a cut, of course. " +
                       "Smuggled goods, looted gear, you name it. Times like these, everyone's a little flexible with the rules.";
            }
            else if (tension >= 60)
            {
                return "Looking to make some coin on the side? I might know some people. " +
                       "There's always a market for... surplus equipment. Weapons that fell off wagons, rations that weren't counted right. " +
                       "Just don't get caught. The lord's too broke to pay bribes right now.";
            }
            else
            {
                return "Interested in opportunities, eh? Smart. " +
                       "I can point you toward some traders who pay premium for certain... acquisitions. " +
                       "Nothing too serious yet, but if things get worse, we might need to be more creative.";
            }
        }

        /// <summary>
        ///     Gets the Believer's moral guidance response.
        ///     Note: Reserved for future dynamic dialog enhancement.
        /// </summary>
        [Obsolete("Reserved for future dynamic dialog. Currently using static localized strings.")]
        public string GetMoralGuidanceResponseLine()
        {
            var tension = EnlistmentBehavior.Instance?.PayTension ?? 0;

            if (tension >= 80)
            {
                return "These are the darkest of times, my child. The men's faith is being tested beyond measure. " +
                       "Some have already abandoned their posts - and their honor. But you are still here. That means something. " +
                       "The heavens see our suffering. They see the lord's broken promises. Justice will come - one way or another. " +
                       "Until then, hold to your principles. That is all any of us can do.";
            }
            else
            {
                return "I see the doubt in your eyes. You are not alone. Many question why we suffer when we have served faithfully. " +
                       "Remember: material wealth fades, but honor endures. If the lord cannot pay, perhaps there are other ways to serve - " +
                       "helping him recover his fortune. The faithful are rewarded, in this life or the next.";
            }
        }

        /// <summary>
        ///     Gets the Veteran's survival advice response.
        ///     Note: Reserved for future dynamic dialog enhancement.
        /// </summary>
        [Obsolete("Reserved for future dynamic dialog. Currently using static localized strings.")]
        public string GetSurvivalAdviceResponseLine()
        {
            var enlistment = EnlistmentBehavior.Instance;
            var freeDesertion = enlistment?.IsFreeDesertionAvailable == true;

            if (freeDesertion)
            {
                return "Listen to me carefully. I've seen this before. The lord's broke, the men are starving, and the whole thing's falling apart. " +
                       "At this point? No one would blame you for walking away. The lord broke his end of the deal first. " +
                       "If you do decide to leave... do it at night. Head for the nearest village. Don't look back. " +
                       "But if you stay... stay for the right reasons. Not because you can't leave.";
            }
            else
            {
                return "Things are bad, I won't lie to you. But we've been in tough spots before. " +
                       "Keep your head down, don't volunteer for anything stupid, and watch your purse. " +
                       "Some men are getting desperate - and desperate men do foolish things. " +
                       "If it gets worse... well, we'll talk then.";
            }
        }

        /// <summary>
        ///     Gets the Help the Lord suggestion response based on archetype.
        ///     Uses {LORD_NAME} placeholder for dynamic lord reference.
        ///     Note: Reserved for future dynamic dialog enhancement.
        /// </summary>
        [Obsolete("Reserved for future dynamic dialog. Currently using static localized strings.")]
        public string GetHelpLordResponseLine()
        {
            var enlistment = EnlistmentBehavior.Instance;
            var archetype = enlistment?.QuartermasterArchetype ?? "veteran";

            var text = archetype switch
            {
                "veteran" => new TextObject("You want to help? Good soldier. {LORD_NAME} needs gold, and fast. " +
                             "There's always bounty work, escort jobs, that sort of thing. Traders sometimes pay for protection. " +
                             "If we're at war, enemy supply wagons are fair game. Every denar helps."),

                "merchant" => new TextObject("Ah, thinking like a businessman! There are merchants who owe {LORD_NAME} money. " +
                              "Perhaps you could... encourage timely payment. Also, I hear there's profit in captured goods - " +
                              "just bring them to me and I'll handle the sale. Discretely."),

                "bookkeeper" => new TextObject("According to my records, there are several outstanding debts owed to {LORD_NAME}. " +
                                "If someone were to... collect these debts... the accounts would balance better. " +
                                "I can provide you with a list of debtors, if you're interested."),

                "scoundrel" => new TextObject("Help {LORD_NAME}? Ha! Plenty of ways, if you're not too squeamish. " +
                               "Enemy caravans, shakedowns, a little light robbery... all perfectly acceptable in wartime. " +
                               "Bring me the goods and I'll fence them. We split the profit - and the lord gets his cut."),

                "believer" => new TextObject("Your loyalty is commendable. Perhaps you could collect donations from the faithful? " +
                              "Or help {LORD_NAME} recover what is rightfully owed. " +
                              "There's also the matter of enemy supplies - liberating them serves a higher purpose."),

                "eccentric" => new TextObject("The stars aligned last night to show me a vision. A merchant caravan, heavy with gold, " +
                               "passing through dangerous territory. Perhaps they need protection? Perhaps they don't survive the journey? " +
                               "Either way, there's opportunity for the bold."),

                _ => new TextObject("The lord needs gold. Find work that pays and bring back what you can.")
            };
            
            // Direct unit name variables are being updated.
            if (enlistment != null)
            {
                // Dynamic name replacement will be updated to support the new unit structure.
            }
            
            return text.ToString();
        }

        /// <summary>
        ///     Gets the current quartermaster mood based on camp conditions.
        /// </summary>
        private string GetQuartermasterMood()
        {
            // Try to get mood from CampLifeBehavior if available
            var campLife = CampLifeBehavior.Instance;
            if (campLife != null && campLife.IsActiveWhileEnlisted())
            {
                // Map QuartermasterMoodTier enum to mood strings
                return campLife.QuartermasterMoodTier switch
                {
                    QuartermasterMoodTier.Fine => "content",
                    QuartermasterMoodTier.Tense => "stressed",
                    QuartermasterMoodTier.Sour => "grim",
                    QuartermasterMoodTier.Predatory => "grim",
                    _ => "content"
                };
            }

            // Fallback: Use pay tension as mood indicator
            var payTension = EnlistmentBehavior.Instance?.PayTension ?? 0;
            if (payTension < 20)
            {
                return "content";
            }
            if (payTension < 50)
            {
                return "stressed";
            }
            return "grim";
        }

        #endregion

        #region Quartermaster Dialog Consequences

        /// <summary>
        ///     Called on first meeting with the quartermaster.
        ///     Marks the quartermaster as met.
        /// </summary>
        private void OnQuartermasterFirstMeeting()
        {
            EnlistmentBehavior.Instance?.MarkQuartermasterMet();
            ModLogger.Info("Quartermaster", "First meeting with quartermaster completed");
        }

        /// <summary>
        ///     Called when player requests equipment.
        ///     Opens the quartermaster equipment menu.
        /// </summary>
        private void OnQuartermasterEquipmentRequest()
        {
            try
            {
                // Increase relationship slightly for interaction
                EnlistmentBehavior.Instance?.ModifyQuartermasterRelationship(1);

                // Open equipment menu
                QuartermasterManager.CaptureTimeStateBeforeMenuActivation();
                GameMenu.ActivateGameMenu("quartermaster_equipment");

                ModLogger.Info("Quartermaster", "Opened equipment menu from dialog");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Quartermaster", "Failed to open equipment menu from dialog", ex);
            }
        }

        /// <summary>
        ///     Called when player wants to sell equipment.
        ///     Opens the quartermaster returns menu.
        /// </summary>
        private void OnQuartermasterSellRequest()
        {
            try
            {
                // Increase relationship for interaction
                EnlistmentBehavior.Instance?.ModifyQuartermasterRelationship(1);

                // Open sell menu
                QuartermasterManager.CaptureTimeStateBeforeMenuActivation();
                GameMenu.ActivateGameMenu("quartermaster_returns");

                ModLogger.Info("Quartermaster", "Opened sell menu from dialog");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Quartermaster", "Failed to open sell menu from dialog", ex);
            }
        }

        /// <summary>
        ///     Called when player chats with the quartermaster.
        ///     Increases relationship.
        /// </summary>
        private void OnQuartermasterChat()
        {
            // Increase relationship for chatting
            EnlistmentBehavior.Instance?.ModifyQuartermasterRelationship(5);
            ModLogger.Info("Quartermaster", "Chat completed, relationship increased");
        }

        /// <summary>
        ///     Called when player requests provisions.
        ///     Opens the rations purchase menu.
        /// </summary>
        private void OnQuartermasterProvisionsRequest()
        {
            try
            {
                // Open rations menu
                QuartermasterManager.CaptureTimeStateBeforeMenuActivation();
                GameMenu.ActivateGameMenu("quartermaster_rations");

                ModLogger.Info("Quartermaster", "Opened rations menu from dialog");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Quartermaster", "Failed to open rations menu from dialog", ex);
            }
        }

        // ========================================
        // PAYTENSION DIALOG CONSEQUENCES
        // ========================================

        /// <summary>
        ///     Called when player asks Scoundrel for black market options.
        ///     Provides hints about illicit ways to acquire coin/supplies.
        /// </summary>
        private void OnQuartermasterBlackMarket()
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null)
            {
                return;
            }

            // Increase relationship for showing trust
            enlistment.ModifyQuartermasterRelationship(3);

            // Display the black market response text
            var tension = enlistment.PayTension;
            InformationManager.DisplayMessage(new InformationMessage(
                "The quartermaster shares some... alternative supply channels.",
                Colors.Magenta));

            ModLogger.Info("Quartermaster", $"Black market dialog triggered (tension={tension})");
        }

        /// <summary>
        ///     Called when player asks Believer for moral guidance.
        ///     Provides morale boost and spiritual encouragement.
        /// </summary>
        private void OnQuartermasterMoralGuidance()
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null)
            {
                return;
            }

            // Increase relationship for seeking guidance
            enlistment.ModifyQuartermasterRelationship(5);

            // Small fatigue relief from spiritual comfort
            if (enlistment.FatigueCurrent < 24)
            {
                enlistment.RestoreFatigue(2, "moral_guidance");
            }

            InformationManager.DisplayMessage(new InformationMessage(
                "The quartermaster's words bring some comfort. (+2 fatigue recovery)",
                Colors.Green));

            ModLogger.Info("Quartermaster", "Moral guidance dialog triggered");
        }

        /// <summary>
        ///     Called when player asks Veteran for survival advice.
        ///     Provides practical advice about desertion and survival.
        /// </summary>
        private void OnQuartermasterSurvivalAdvice()
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null)
            {
                return;
            }

            // Increase relationship for confiding
            enlistment.ModifyQuartermasterRelationship(2);

            // Check if free desertion is available
            if (enlistment.IsFreeDesertionAvailable)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    "The veteran nods knowingly. 'If you need to leave... no one would blame you. The Lord's broken his contract with us.'",
                    Colors.Yellow));
            }
            else
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    "The veteran shares tips for surviving hard times. 'Keep your head down. Watch for opportunities.'",
                    Colors.Yellow));
            }

            ModLogger.Info("Quartermaster", $"Survival advice dialog triggered (freeDesertion={enlistment.IsFreeDesertionAvailable})");
        }

        /// <summary>
        ///     Called when player asks how to help the lord.
        ///     Provides archetype-specific suggestions for reducing PayTension.
        /// </summary>
        private void OnQuartermasterHelpLordSuggestion()
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null)
            {
                return;
            }

            var archetype = enlistment.QuartermasterArchetype;
            var suggestion = archetype switch
            {
                "veteran" => "The lord needs gold, and fast. Look for bounty work, escort missions, anything that pays quick.",
                "merchant" => "I know some merchants who owe the lord money. Perhaps you could... encourage them to pay up.",
                "bookkeeper" => "There are discrepancies in the accounts. If you find evidence of embezzlement, the lord could recover funds.",
                "scoundrel" => "Plenty of ways to make coin if you're not too squeamish. Raid enemy supply lines, shake down travelers...",
                "believer" => "The faithful should help their lord in times of need. Perhaps donations could be collected, or enemy coffers... liberated.",
                "eccentric" => "The stars suggest a merchant caravan will pass by soon with more gold than guards. Just saying.",
                _ => "The lord needs coin. Find work that pays."
            };

            // Increase relationship for showing loyalty
            enlistment.ModifyQuartermasterRelationship(3);

            InformationManager.DisplayMessage(new InformationMessage(
                $"Quartermaster advice: \"{suggestion}\"",
                Colors.Cyan));

            ModLogger.Info("Quartermaster", "Help lord suggestion dialog triggered");
        }

        #endregion

        #region Utility Methods

        /// <summary>
        ///     Get localized text with fallback support.
        /// </summary>
        private TextObject GetLocalizedText(string key)
        {
            return new TextObject(key);
        }

#if false
        /// <summary>
        ///     Cleans up the enlisted menu after discharge/retirement.
        ///     Called deferred (next frame) after retirement dialog closes to ensure
        ///     the player isn't stuck on an empty menu with no options.
        /// </summary>
        private static void CleanupEnlistedMenuAfterDischarge()
        {
            try
            {
                // Only exit if we're still on the enlisted menu and not enlisted
                if (EnlistmentBehavior.Instance?.IsEnlisted != true)
                {
                    ModLogger.Info("DialogManager", "Cleaning up enlisted menu after discharge");
                    GameMenu.ExitToLast();
                }
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("DialogManager", "E-DIALOG-003", "Error cleaning up menu after discharge", ex);
            }
        }
#endif

        #endregion

        #region Shared Dialog Conditions

        /// <summary>
        ///     Checks if the player is on leave and talking to a lord from a DIFFERENT faction.
        ///     Used to trigger the roleplay rejection dialog ("You cannot serve two masters").
        /// </summary>
        private bool IsOnLeaveWithDifferentFaction()
        {
            var enlistment = EnlistmentBehavior.Instance;
            var lord = Hero.OneToOneConversationHero;

            if (enlistment?.IsOnLeave != true || lord == null)
            {
                return false;
            }

            // If talking to current lord, this is "return from leave" - not a rejection
            if (enlistment.CurrentLord == lord)
            {
                return false;
            }

            // Check if different faction
            var currentLordKingdom = enlistment.CurrentLord?.MapFaction as Kingdom;
            var targetLordKingdom = lord.MapFaction as Kingdom;

            // Different faction (or one has no kingdom) = rejection
            if (currentLordKingdom == null || targetLordKingdom != currentLordKingdom)
            {
                ModLogger.Debug("DialogManager",
                    $"Triggering faction rejection - on leave from {enlistment.CurrentLord?.Name}, talking to {lord.Name} (different faction)");
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Checks if the conversation partner is a valid lord for military service discussions.
        ///     This shared condition prevents dialog conflicts and ensures consistency.
        ///     CRITICAL: Prevents enlistment dialog from appearing if player is already enlisted, on leave, or in grace period.
        ///     Player must be fully discharged before they can enlist with another lord/kingdom.
        /// </summary>
        private bool IsValidLordForMilitaryService()
        {
            var lord = Hero.OneToOneConversationHero;
            if (lord == null || !lord.IsLord || !lord.IsAlive)
            {
                return false;
            }

            var enlistment = EnlistmentBehavior.Instance;

            // Allow dialog when talking to current lord (for retirement, duties discussion, etc.)
            // Block dialog with OTHER lords when actively enlisted - player can't enlist elsewhere
            if (enlistment?.IsEnlisted == true && enlistment?.IsOnLeave != true)
            {
                if (enlistment?.CurrentLord != lord)
                {
                    ModLogger.Debug("DialogManager",
                        $"Dialog hidden - player is actively enlisted with {enlistment?.CurrentLord?.Name}");
                    return false;
                }

                // Allow - player talking to their own lord (retirement, early discharge, etc.)
                ModLogger.Debug("DialogManager", $"Dialog shown - player talking to their enlisted lord {lord.Name}");
                return true;
            }

            // When on leave, ALLOW dialog with all lords - we handle same vs different faction in the dialog itself
            // Same faction lords can accept transfers, different faction lords give roleplay rejection
            if (enlistment?.IsOnLeave == true)
            {
                if (enlistment.CurrentLord == lord)
                {
                    // Allow - player can return from leave with this lord
                    ModLogger.Debug("DialogManager",
                        $"Dialog shown - player on leave, talking to their lord {lord.Name}");
                    return true;
                }

                // Allow dialog with ALL lords when on leave - rejection handled in dialog flow
                // This prevents "Missing dialog state" and gives roleplay rejection for different factions
                ModLogger.Debug("DialogManager",
                    $"Dialog shown - player on leave, talking to {lord.Name} (will check faction in dialog)");
                return true;
            }

            if (enlistment?.IsInDesertionGracePeriod == true)
            {
                // During grace period, player can only rejoin the same kingdom they served
                // Don't show enlistment dialog for different kingdoms
                var lordKingdom = lord.MapFaction as Kingdom;
                if (lordKingdom != enlistment.PendingDesertionKingdom)
                {
                    ModLogger.Debug("DialogManager",
                        $"Dialog hidden - player in grace period, can only rejoin {enlistment.PendingDesertionKingdom?.Name}");
                    return false;
                }
                // Allow dialog if same kingdom - they can rejoin during grace period
            }

            return true;
        }

        /// <summary>
        ///     Checks if the conversation partner is a minor faction lord.
        ///     Minor faction lords (mercenary companies, bandits, etc.) use different dialog text
        ///     even when they're serving as mercenaries for a kingdom. Uses Clan.IsMinorFaction
        ///     which remains true regardless of kingdom affiliation.
        /// </summary>
        private bool IsMinorFactionLord()
        {
            var lord = Hero.OneToOneConversationHero;
            return lord?.Clan?.IsMinorFaction == true;
        }

        /// <summary>
        ///     Checks if the player can request enlistment with a minor faction lord.
        ///     Same logic as standard enlistment but only for minor faction lords.
        /// </summary>
        private bool CanRequestMinorFactionEnlistment()
        {
            if (!IsMinorFactionLord())
            {
                return false;
            }

            var enlistment = EnlistmentBehavior.Instance;

            if (enlistment?.IsEnlisted == true || enlistment?.IsOnLeave == true)
            {
                return false;
            }

            if (enlistment?.IsInDesertionGracePeriod == true)
            {
                return false;
            }

            var lord = Hero.OneToOneConversationHero;
            if (lord == null || !lord.IsLord)
            {
                return false;
            }

            return enlistment != null && enlistment.CanEnlistWithParty(lord, out _);
        }

        /// <summary>
        ///     Checks if the player can request standard kingdom enlistment (not minor faction).
        ///     Minor faction lords use CanRequestMinorFactionEnlistment instead.
        /// </summary>
        private bool CanRequestStandardEnlistment()
        {
            // Minor faction lords use separate dialog
            if (IsMinorFactionLord())
            {
                return false;
            }

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

            return enlistment?.CanEnlistWithParty(lord, out _) == true;
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

            return enlistment.CanEnlistWithParty(lord, out _);
        }

        /// <summary>
        ///     Checks if the player is currently leading their own army.
        ///     Used to show army leader rejection dialog instead of standard enlistment.
        /// </summary>
        private bool IsPlayerLeadingArmy()
        {
            var enlistment = EnlistmentBehavior.Instance;

            // Don't show if already enlisted or on leave
            if (enlistment?.IsEnlisted == true || enlistment?.IsOnLeave == true)
            {
                return false;
            }

            // Don't show during grace period (different dialog flow)
            if (enlistment?.IsInDesertionGracePeriod == true)
            {
                return false;
            }

            var lord = Hero.OneToOneConversationHero;
            if (lord == null || !lord.IsLord)
            {
                return false;
            }

            // Check if player is leading their own army
            var main = MobileParty.MainParty;
            return main?.Army != null && main.Army.LeaderParty == main;
        }

#if false
        /// <summary>
        ///     Checks if the player can discuss first-term retirement (after 252 days).
        ///     Must be enlisted with current lord, have completed minimum service,
        ///     and not be in a grace period or already in a renewal term.
        ///     Only for kingdom lords - minor factions use CanDiscussMinorFactionRetirement.
        /// </summary>
        private bool CanDiscussFirstTermRetirement()
        {
            // Minor faction lords use separate dialog
            if (IsMinorFactionLord())
            {
                return false;
            }

            var enlistment = EnlistmentBehavior.Instance;
            var lord = Hero.OneToOneConversationHero;

            // Must be enlisted with this specific lord and eligible for retirement
            // Also explicitly exclude grace period for clarity (IsEnlisted is false during grace anyway)
            return enlistment?.IsEnlisted == true &&
                   enlistment.CurrentLord == lord &&
                   enlistment.IsEligibleForRetirement &&
                   !enlistment.IsInDesertionGracePeriod;
        }
#endif

        private bool CanDiscussDischargeGuidance()
        {
            var enlistment = EnlistmentBehavior.Instance;
            var lord = Hero.OneToOneConversationHero;

            return enlistment?.IsEnlisted == true &&
                   enlistment.CurrentLord == lord &&
                   !enlistment.IsInDesertionGracePeriod;
        }

#if false
        /// <summary>
        ///     Checks if the player can discuss retirement with a minor faction lord.
        ///     Same eligibility as kingdom lords but uses mercenary-themed dialog.
        /// </summary>
        private bool CanDiscussMinorFactionRetirement()
        {
            if (!IsMinorFactionLord())
            {
                return false;
            }

            var enlistment = EnlistmentBehavior.Instance;
            var lord = Hero.OneToOneConversationHero;

            return enlistment?.IsEnlisted == true &&
                   enlistment.CurrentLord == lord &&
                   enlistment.IsEligibleForRetirement &&
                   !enlistment.IsInDesertionGracePeriod;
        }
#endif

#if false
        /// <summary>
        ///     Checks if the player can discuss renewal term completion.
        ///     Only for kingdom lords - minor factions use CanDiscussMinorFactionRenewalEnd.
        /// </summary>
        private bool CanDiscussRenewalTermEnd()
        {
            // Minor faction lords use separate dialog
            if (IsMinorFactionLord())
            {
                return false;
            }

            var enlistment = EnlistmentBehavior.Instance;
            var lord = Hero.OneToOneConversationHero;

            return enlistment?.IsEnlisted == true &&
                   enlistment.CurrentLord == lord &&
                   enlistment.IsInRenewalTerm &&
                   enlistment.IsRenewalTermComplete;
        }
#endif

#if false
        /// <summary>
        ///     Checks if the player can discuss renewal term completion with a minor faction lord.
        /// </summary>
        private bool CanDiscussMinorFactionRenewalEnd()
        {
            if (!IsMinorFactionLord())
            {
                return false;
            }

            var enlistment = EnlistmentBehavior.Instance;
            var lord = Hero.OneToOneConversationHero;

            return enlistment?.IsEnlisted == true &&
                   enlistment.CurrentLord == lord &&
                   enlistment.IsInRenewalTerm &&
                   enlistment.IsRenewalTermComplete;
        }
#endif

#if false
        /// <summary>
        ///     Checks if the player can re-enlist after cooldown (veteran return).
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
            return kingdom != null && enlistment?.CanReEnlistAfterCooldown(kingdom) == true;
        }
#endif

#if false
        /// <summary>
        ///     Checks if the player can request early discharge (before full term).
        ///     Shows only when not eligible for full retirement benefits.
        ///     Only for kingdom lords - minor factions use CanRequestMinorFactionEarlyDischarge.
        /// </summary>
        private bool CanRequestEarlyDischarge()
        {
            // Minor faction lords use separate dialog
            if (IsMinorFactionLord())
            {
                return false;
            }

            var enlistment = EnlistmentBehavior.Instance;
            var lord = Hero.OneToOneConversationHero;

            // Must be enlisted with this lord but NOT eligible for full retirement
            return enlistment?.IsEnlisted == true &&
                   enlistment.CurrentLord == lord &&
                   !enlistment.IsEligibleForRetirement &&
                   !enlistment.IsRenewalTermComplete;
        }
#endif

#if false
        /// <summary>
        ///     Checks if the player can request early discharge from a minor faction.
        /// </summary>
        private bool CanRequestMinorFactionEarlyDischarge()
        {
            if (!IsMinorFactionLord())
            {
                return false;
            }

            var enlistment = EnlistmentBehavior.Instance;
            var lord = Hero.OneToOneConversationHero;

            return enlistment?.IsEnlisted == true &&
                   enlistment.CurrentLord == lord &&
                   !enlistment.IsEligibleForRetirement &&
                   !enlistment.IsRenewalTermComplete;
        }
#endif

        /// <summary>
        ///     Checks if the player can return from temporary leave.
        ///     Only for kingdom lords - minor factions use CanReturnFromMinorFactionLeave.
        /// </summary>
        private bool CanReturnFromLeave()
        {
            // Minor faction lords use separate dialog
            if (IsMinorFactionLord())
            {
                return false;
            }

            var enlistment = EnlistmentBehavior.Instance;
            var lord = Hero.OneToOneConversationHero;

            // Must be on leave and talking to the same lord you served
            return enlistment?.IsOnLeave == true &&
                   enlistment.CurrentLord == lord;
        }

        /// <summary>
        ///     Checks if the player can return from leave with a minor faction lord.
        /// </summary>
        private bool CanReturnFromMinorFactionLeave()
        {
            if (!IsMinorFactionLord())
            {
                return false;
            }

            var enlistment = EnlistmentBehavior.Instance;
            var lord = Hero.OneToOneConversationHero;

            return enlistment?.IsOnLeave == true &&
                   enlistment.CurrentLord == lord;
        }

        /// <summary>
        ///     Checks if the player can request to transfer service to a different lord.
        ///     Available ONLY when on leave, talking to a different lord in the same faction.
        ///     Note: Grace period transfers are handled separately by CanRequestGraceTransfer().
        /// </summary>
        private bool CanRequestServiceTransfer()
        {
            var enlistment = EnlistmentBehavior.Instance;
            var lord = Hero.OneToOneConversationHero;

            if (lord == null || !lord.IsLord || !lord.IsAlive)
            {
                return false;
            }

            // Only show this dialog when on leave (NOT in grace period)
            // Grace period has its own dedicated dialog via CanRequestGraceTransfer()
            if (enlistment?.IsOnLeave != true)
            {
                return false;
            }

            // Must be talking to a DIFFERENT lord (not current lord - that's "return from leave")
            if (enlistment.CurrentLord == lord)
            {
                return false;
            }

            // Must be same faction/kingdom
            var currentLordKingdom = enlistment.CurrentLord?.MapFaction as Kingdom;
            var targetLordKingdom = lord.MapFaction as Kingdom;

            if (currentLordKingdom == null || targetLordKingdom != currentLordKingdom)
            {
                return false;
            }

            // Check if the lord can accept service
            return enlistment.CanEnlistWithParty(lord, out _);
        }

        /// <summary>
        ///     Check if player can request T7 (Commander) promotion.
        ///     Player must be T6, enlisted with the lord they're talking to, and meet all T7 requirements.
        /// </summary>
        private bool CanRequestCommanderPromotion()
        {
            var enlistment = EnlistmentBehavior.Instance;
            var lord = Hero.OneToOneConversationHero;

            if (lord == null || !lord.IsLord || !lord.IsAlive)
            {
                return false;
            }

            // Must be enlisted with this specific lord
            if (enlistment?.IsEnlisted != true || enlistment.CurrentLord != lord)
            {
                return false;
            }

            // Must be exactly Tier 6 (not already T7+)
            if (enlistment.EnlistmentTier != 6)
            {
                return false;
            }

            // Must meet all T7 promotion requirements
            var promotionBehavior = Ranks.Behaviors.PromotionBehavior.Instance;
            if (promotionBehavior == null)
            {
                return false;
            }

            var (canPromote, _) = promotionBehavior.CanPromote();
            return canPromote;
        }

        #endregion

        #region Shared Dialog Consequences

        /// <summary>
        ///     Handles the consequence of accepting enlistment.
        ///     Centralized to ensure consistent behavior across all enlistment paths.
        ///     CRITICAL: The entire enlistment process is deferred to the next frame because
        ///     StartEnlist() calls PlayerEncounter.LeaveSettlement/Finish which crashes if
        ///     the conversation mission is still active (MissionLocationLogic.OnRemoveBehavior fails).
        /// </summary>
        private void OnAcceptEnlistment()
        {
            try
            {
                var lord = Hero.OneToOneConversationHero;
                if (lord == null)
                {
                    ModLogger.LogOnce("dialog_accept_enlistment_no_conversation_hero", "DialogManager",
                        "[E-DIALOG-001] No conversation hero found during enlistment acceptance", LogLevel.Error);
                    return;
                }

                if (EnlistmentBehavior.Instance == null)
                {
                    ModLogger.LogOnce("dialog_accept_enlistment_enlistment_instance_null", "DialogManager",
                        "[E-DIALOG-002] EnlistmentBehavior.Instance is null during enlistment", LogLevel.Error);
                    return;
                }

                ModLogger.Info("DialogManager", $"Player accepting enlistment with lord: {lord.Name}");

                // Capture lord reference before conversation ends (Hero.OneToOneConversationHero will be null next frame)
                var capturedLord = lord;

                // CRITICAL: Defer the entire enlistment to next frame to let the conversation mission
                // finalize first. Calling StartEnlist() during the dialog consequence crashes because
                // it tries to end the PlayerEncounter while MissionLocationLogic is still active.
                NextFrameDispatcher.RunNextFrame(() =>
                {
                    try
                    {
                        if (EnlistmentBehavior.Instance == null)
                        {
                            ModLogger.LogOnce("dialog_deferred_enlistment_instance_null", "DialogManager",
                                "[E-DIALOG-004] EnlistmentBehavior.Instance became null before deferred enlistment", LogLevel.Error);
                            return;
                        }

                        EnlistmentBehavior.Instance.StartEnlist(capturedLord);

                        // Activate the enlisted status menu after enlistment completes
                        NextFrameDispatcher.RunNextFrame(EnlistedMenuBehavior.SafeActivateEnlistedMenu);
                        ModLogger.Debug("DialogManager",
                            "Scheduled enlisted_status menu activation - preventing encounter gap");

                        // Professional notification
                        var message =
                            GetLocalizedText("{=enlisted_success_notification}You have enlisted in {LORD_NAME}'s service.");
                        message.SetTextVariable("LORD_NAME", capturedLord.Name);
                        InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
                    }
                    catch (Exception ex)
                    {
                        ModLogger.Error("DialogManager", "Error in deferred enlistment", ex);
                    }
                });
            }
            catch (Exception ex)
            {
                ModLogger.Error("DialogManager", "Error during enlistment acceptance", ex);
            }
        }

#if false
        /// <summary>
        ///     Handles first-term retirement with full benefits.
        ///     Schedules menu cleanup after dialog closes to prevent empty menu state.
        /// </summary>
        private void OnAcceptFirstTermRetirement()
        {
            try
            {
                EnlistmentBehavior.Instance?.ProcessFirstTermRetirement();
                ModLogger.Info("DialogManager", "First-term retirement processed");
                
                // Defer menu cleanup until after dialog closes - prevents stuck menu with no options
                NextFrameDispatcher.RunNextFrame(CleanupEnlistedMenuAfterDischarge);
            }
            catch (Exception ex)
            {
                ModLogger.Error("DialogManager", "Error during first-term retirement", ex);
            }
        }

        /// <summary>
        ///     Called when player chooses to keep their troops during retirement.
        ///     Sets the retention flag - actual processing happens in OnFinalizeRetirementWithTroops.
        /// </summary>
        private void OnKeepTroopsRetirement()
        {
            EnlistmentBehavior.RetainTroopsOnRetirement = true;
            ModLogger.Info("DialogManager", "Player chose to keep retinue troops on retirement");
        }

        /// <summary>
        ///     Finalizes retirement when player kept troops. Processes retirement,
        ///     adds starter supplies (grain), and cleans up.
        /// </summary>
        private void OnFinalizeRetirementWithTroops()
        {
            try
            {
                // Process the retirement with troop retention flag set
                EnlistmentBehavior.Instance?.ProcessFirstTermRetirement();
                
                // Add starter supplies so they don't starve immediately
                AddRetirementSupplies();
                
                ModLogger.Info("DialogManager", "First-term retirement with troops processed");
                
                // Defer menu cleanup until after dialog closes
                NextFrameDispatcher.RunNextFrame(CleanupEnlistedMenuAfterDischarge);
            }
            catch (Exception ex)
            {
                ModLogger.Error("DialogManager", "Error during retirement with troops", ex);
            }
        }
#endif

#if false
        /// <summary>
        ///     Adds 10 grain to player inventory as retirement supplies.
        ///     Prevents immediate starvation when leaving the army's supply train.
        /// </summary>
        private static void AddRetirementSupplies()
        {
            try
            {
                var party = MobileParty.MainParty;
                if (party == null)
                {
                    return;
                }

                var grain = MBObjectManager.Instance.GetObject<ItemObject>("grain");
                if (grain != null)
                {
                    party.ItemRoster.AddToCounts(grain, 10);
                    ModLogger.Info("DialogManager", "Added 10 grain as retirement supplies");
                }
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("DialogManager", "E-DIALOG-004", "Failed to add retirement supplies", ex);
            }
        }
#endif

#if false
        /// <summary>
        ///     Checks if player has a retinue that can be kept on retirement.
        ///     Used to determine which dialog branch to show.
        /// </summary>
        private static bool HasRetinueForRetirement()
        {
            var manager = ServiceRecordManager.Instance?.RetinueManager;
            return manager?.State?.HasRetinue == true;
        }

        /// <summary>
        ///     Gets the lord's question text about the troops, with unit name variable.
        /// </summary>
        private static string GetRetinueAskText()
        {
            var unitName = GetRetinueUnitName();
            var text = new TextObject(
                "{=enlisted_retinue_ask}Your {UNIT_NAME} has served you well these many months. What of your men? They may return to the ranks, or... if they wish to follow you into the unknown, I shall not stop them.");
            text.SetTextVariable("UNIT_NAME", unitName);
            return text.ToString();
        }

        /// <summary>
        ///     Gets the farewell text when player keeps their troops.
        /// </summary>
        private static string GetRetinueFarewellText(bool isMinorFaction)
        {
            var manager = ServiceRecordManager.Instance?.RetinueManager;
            var count = manager?.State?.TotalSoldiers ?? 0;

            TextObject text = new TextObject(
                isMinorFaction
                    ? "{=enlisted_minor_farewell_troops}Ha! Loyal to the end, that lot. Your {COUNT} have decided to stay with you instead of signing back on. Here's some grain for the road - you'll need it. Don't be a stranger."
                    : "{=enlisted_farewell_troops}So be it. Your {COUNT} soldiers have decided to stay with you rather than re-enlist. I've had the quartermaster set aside provisions for your journey. Fare well, soldier.");
            text.SetTextVariable("COUNT", count);
            return text.ToString();
        }
#endif

#if false
        /// <summary>
        ///     Gets the unit name based on player's tier (Lance, Squad, or Retinue).
        /// </summary>
        private static string GetRetinueUnitName()
        {
            var tier = EnlistmentBehavior.Instance?.EnlistmentTier ?? 4;
            return tier switch
            {
                >= 6 => "retinue",
                5 => "squad",
                _ => "lance"
            };
        }
#endif

#if false
        /// <summary>
        ///     Handles first-term re-enlistment with 20,000 gold bonus.
        /// </summary>
        private void OnAcceptFirstTermReenlistBonus()
        {
            try
            {
                var config = AssignmentsConfig.LoadRetirementConfig();
                EnlistmentBehavior.Instance?.StartRenewalTerm(config.FirstTermReenlistBonus);
                ModLogger.Info("DialogManager",
                    $"First-term re-enlistment with {config.FirstTermReenlistBonus}g bonus");
            }
            catch (Exception ex)
            {
                ModLogger.Error("DialogManager", "Error during first-term re-enlistment", ex);
            }
        }

        /// <summary>
        ///     Handles renewal term discharge with 5,000 gold.
        ///     Schedules menu cleanup after dialog closes to prevent empty menu state.
        /// </summary>
        private void OnAcceptRenewalDischarge()
        {
            try
            {
                EnlistmentBehavior.Instance?.ProcessRenewalRetirement();
                ModLogger.Info("DialogManager", "Renewal discharge processed");
                
                // Defer menu cleanup until after dialog closes - prevents stuck menu with no options
                NextFrameDispatcher.RunNextFrame(CleanupEnlistedMenuAfterDischarge);
            }
            catch (Exception ex)
            {
                ModLogger.Error("DialogManager", "Error during renewal discharge", ex);
            }
        }

        /// <summary>
        ///     Finalizes renewal discharge when player kept troops. Processes discharge,
        ///     adds starter supplies (grain), and cleans up.
        /// </summary>
        private void OnFinalizeRenewalWithTroops()
        {
            try
            {
                // Process the renewal discharge with troop retention flag set
                EnlistmentBehavior.Instance?.ProcessRenewalRetirement();
                
                // Add starter supplies so they don't starve immediately
                AddRetirementSupplies();
                
                ModLogger.Info("DialogManager", "Renewal discharge with troops processed");
                
                // Defer menu cleanup until after dialog closes
                NextFrameDispatcher.RunNextFrame(CleanupEnlistedMenuAfterDischarge);
            }
            catch (Exception ex)
            {
                ModLogger.Error("DialogManager", "Error during renewal discharge with troops", ex);
            }
        }
#endif

#if false
        /// <summary>
        ///     Handles continuing service with 5,000 gold bonus.
        /// </summary>
        private void OnContinueService()
        {
            try
            {
                var config = AssignmentsConfig.LoadRetirementConfig();
                EnlistmentBehavior.Instance?.StartRenewalTerm(config.RenewalContinueBonus);
                ModLogger.Info("DialogManager", $"Service continued with {config.RenewalContinueBonus}g bonus");
            }
            catch (Exception ex)
            {
                ModLogger.Error("DialogManager", "Error continuing service", ex);
            }
        }

        /// <summary>
        ///     Handles veteran re-enlistment after cooldown.
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
                    NextFrameDispatcher.RunNextFrame(EnlistedMenuBehavior.SafeActivateEnlistedMenu);
                }

                ModLogger.Info("DialogManager", "Veteran re-enlistment processed");
            }
            catch (Exception ex)
            {
                ModLogger.Error("DialogManager", "Error during veteran re-enlistment", ex);
            }
        }

        /// <summary>
        ///     Handles early discharge (before full term, no benefits).
        ///     Schedules menu cleanup after dialog closes to prevent empty menu state.
        /// </summary>
        private void OnGrantEarlyDischarge()
        {
            try
            {
                if (EnlistmentBehavior.Instance?.IsEnlisted == true)
                {
                    var lordName = EnlistmentBehavior.Instance.CurrentLord?.Name?.ToString() ?? "Unknown Lord";
                    ModLogger.Info("DialogManager", $"Player early discharge from service with: {lordName}");

                    // Early discharge is not honorable (default false)
                    EnlistmentBehavior.Instance.StopEnlist("Early discharge through dialog");

                    var message =
                        GetLocalizedText(
                            "{=enlisted_early_discharge_notification}You have been discharged from service without full benefits.");
                    InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
                    
                    // Defer menu cleanup until after dialog closes - prevents stuck menu with no options
                    NextFrameDispatcher.RunNextFrame(CleanupEnlistedMenuAfterDischarge);
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("DialogManager", "Error during early discharge", ex);
            }
        }
#endif

        /// <summary>
        ///     Handles the consequence of returning from temporary leave.
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
                    var returnMessage =
                        GetLocalizedText(
                            "{=enlisted_return_notification}You have returned to active military service.");
                    InformationManager.DisplayMessage(new InformationMessage(returnMessage.ToString()));
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("DialogManager", "Error during return from leave", ex);
            }
        }

        /// <summary>
        ///     Handles the consequence of transferring service to a new lord.
        ///     Called when player accepts transfer while on leave or in grace period.
        /// </summary>
        private void OnAcceptServiceTransfer()
        {
            try
            {
                var newLord = Hero.OneToOneConversationHero;
                var enlistment = EnlistmentBehavior.Instance;

                if (newLord == null || enlistment == null)
                {
                    ModLogger.LogOnce("dialog_transfer_service_missing_inputs", "DialogManager",
                        "[E-DIALOG-003] Cannot transfer service - missing lord or enlistment instance", LogLevel.Error);
                    return;
                }

                var previousLordName = enlistment.CurrentLord?.Name?.ToString() ?? "your previous commander";
                var newLordName = newLord.Name?.ToString() ?? "Unknown Lord";

                ModLogger.Info("DialogManager",
                    $"Player transferring service from {previousLordName} to {newLordName}");

                // Perform the transfer
                enlistment.TransferServiceToLord(newLord);

                // Professional notification
                var message =
                    GetLocalizedText(
                        "{=enlisted_transfer_notification}You have transferred your service to {LORD_NAME}. Your rank and experience have been preserved.");
                message.SetTextVariable("LORD_NAME", newLord.Name);
                InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
            }
            catch (Exception ex)
            {
                ModLogger.Error("DialogManager", "Error during service transfer", ex);
            }
        }

        /// <summary>
        ///     Handles the consequence of accepting T7 Commander promotion via dialog.
        ///     Triggers the T6→T7 promotion event which grants retinue and promotes player.
        /// </summary>
        private void OnAcceptCommanderPromotion()
        {
            try
            {
                var enlistment = EnlistmentBehavior.Instance;

                if (enlistment == null)
                {
                    ModLogger.Error("DialogManager", "EnlistmentBehavior.Instance is null during commander promotion acceptance");
                    return;
                }

                if (enlistment.EnlistmentTier != 6)
                {
                    ModLogger.Warn("DialogManager", $"Player not T6 when accepting commander promotion (tier={enlistment.EnlistmentTier})");
                    return;
                }

                ModLogger.Info("DialogManager", "Player accepting T7 Commander promotion via dialog");

                // Defer to next frame to let conversation close first
                NextFrameDispatcher.RunNextFrame(() =>
                {
                    try
                    {
                        // High-ranking commissions are processed directly through the promotion system.
                        ModLogger.Info("DialogManager", "Processing commander promotion");
                        
                        var enlistmentCheck = EnlistmentBehavior.Instance;
                        if (enlistmentCheck != null && enlistmentCheck.EnlistmentTier == 6)
                        {
                            enlistmentCheck.SetTier(7);
                            QuartermasterManager.Instance?.UpdateNewlyUnlockedItems();
                            
                            var message = new TextObject("{=promotion_t7_notification}You have been promoted to Commander. Twenty recruits await your command.");
                            InformationManager.DisplayMessage(new InformationMessage(message.ToString(), Colors.Green));
                        }
                    }
                    catch (Exception ex)
                    {
                        ModLogger.Error("DialogManager", "Error during deferred commander promotion", ex);
                    }
                });
            }
            catch (Exception ex)
            {
                ModLogger.Error("DialogManager", "Error accepting commander promotion", ex);
            }
        }

        #endregion
    }
}
