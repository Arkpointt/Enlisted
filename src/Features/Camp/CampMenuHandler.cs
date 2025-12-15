using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Enlisted.Features.Assignments.Behaviors;
using Enlisted.Features.Camp.UI;
using Enlisted.Features.Camp.UI.Hub;
using Enlisted.Features.CommandTent.Core;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Equipment.Behaviors;
using Enlisted.Features.Interface.Behaviors;
using Enlisted.Mod.Core.Logging;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using EnlistedConfig = Enlisted.Features.Assignments.Core.ConfigurationManager;

namespace Enlisted.Features.Camp
{
    /// <summary>
    /// Handles the Camp menu system for service records display.
    /// Provides menus for viewing current posting, faction history, and lifetime statistics.
        /// Integrates with the existing enlisted status menu by adding a "Camp" option.
    /// </summary>
    public sealed class CampMenuHandler : CampaignBehaviorBase
    {
        private const string LogCategory = "Camp";

        // Menu IDs
        private const string CommandTentMenuId = "command_tent";
        private const string ServiceRecordsMenuId = "command_tent_service_records";
        private const string CurrentPostingMenuId = "command_tent_current_posting";
        private const string FactionRecordsMenuId = "command_tent_faction_records";
        private const string FactionDetailMenuId = "command_tent_faction_detail";
        private const string LifetimeSummaryMenuId = "command_tent_lifetime_summary";

        // Retinue Menu IDs
        private const string RetinueMenuId = "command_tent_retinue";
        private const string RetinuePurchaseMenuId = "command_tent_retinue_purchase";
        private const string RetinueDismissMenuId = "command_tent_retinue_dismiss";
        private const string RetinueRequisitionMenuId = "command_tent_retinue_requisition";


        // Ensure Camp dialogs never pause the campaign clock.
        private static bool ShouldPauseDuringCommandTentInquiry() => false;

        #region Wait Menu Handlers (enables spacebar time control like Quartermaster menus)
        
        /// <summary>
        /// Wait condition - always returns true since we control exit via menu options.
        /// </summary>
        private static bool CommandTentWaitCondition(MenuCallbackArgs args) => true;
        
        /// <summary>
        /// Wait consequence - empty since we handle exit via menu options.
        /// </summary>
        private static void CommandTentWaitConsequence(MenuCallbackArgs args)
        {
            // No consequence needed - we never let progress reach 100%
        }
        
        /// <summary>
        /// Wait tick handler for Camp menus.
        /// NOTE: Time mode restoration is handled ONCE during menu init, not here.
        /// Previously this tick handler would restore CapturedTimeMode whenever it saw
        /// UnstoppableFastForward, but this fought with user input - when the user clicked
        /// fast forward, the next tick would immediately restore it. This caused x3 speed to pause.
        /// </summary>
        private static void CommandTentWaitTick(MenuCallbackArgs args, CampaignTime dt)
        {
            // Intentionally empty - time mode is handled in menu init, not per-tick
            // The old code here fought with user speed input and caused pausing issues
        }
        
        /// <summary>
        /// Switch to a menu while preserving the current time control mode.
        /// Uses the shared CapturedTimeMode from QuartermasterManager.
        /// </summary>
        private static void SwitchToMenuPreserveTime(string menuId)
        {
            // Preserve the player's time mode from the moment they first entered Camp.
            // Do not overwrite it on subsequent hops between submenus so time never gets paused/resumed unexpectedly.
            var capturedMode = QuartermasterManager.CapturedTimeMode
                               ?? Campaign.Current?.TimeControlMode
                               ?? CampaignTimeControlMode.Stop;

            if (!QuartermasterManager.CapturedTimeMode.HasValue)
            {
                QuartermasterManager.CapturedTimeMode = capturedMode;
            }

            GameMenu.SwitchToMenu(menuId);

            if (Campaign.Current != null)
            {
                Campaign.Current.TimeControlMode = capturedMode;
            }
        }
        
        #endregion

        // Companion Assignment Menu IDs
        private const string CompanionAssignmentsMenuId = "command_tent_companions";

        // Phase 8: PayTension Action Menu IDs
        private const string DesperateMeasuresMenuId = "command_tent_desperate";
        private const string HelpTheLordMenuId = "command_tent_help_lord";

        // Track selected faction for detail view
        private string _selectedFactionKey;

        public static CampMenuHandler Instance { get; private set; }

        public CampMenuHandler()
        {
            Instance = this;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // No persistent state needed for menu handler
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            try
            {
                // Set up inline icons for use in menu text (Bannerlord's rich text system)
                SetupInlineIcons();
                
                AddCommandTentMenus(starter);
                ModLogger.Info(LogCategory, "Camp menus registered successfully");
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, $"Failed to register camp menus: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Sets up inline icon variables for use in menu text.
        /// Uses Bannerlord's native img tag system for inline sprites.
        /// </summary>
        private static void SetupInlineIcons()
        {
            // Gold/denar icon - displays coin sprite inline with text
            MBTextManager.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
            
            // Influence icon - displays influence sprite inline
            MBTextManager.SetTextVariable("INFLUENCE_ICON", "{=!}<img src=\"General\\Icons\\Influence@2x\" extend=\"7\">");
        }

        /// <summary>
        /// Registers all camp menus and options with the game.
        /// </summary>
        private void AddCommandTentMenus(CampaignGameStarter starter)
        {
            // Add "Camp" option to enlisted_status menu (entry point)
            AddCommandTentOptionToEnlistedMenu(starter);

            // Main camp menu
            AddMainCommandTentMenu(starter);

            // Service Records submenu
            AddServiceRecordsMenu(starter);

            // Current Posting display
            AddCurrentPostingMenu(starter);

            // Faction Records list
            AddFactionRecordsMenu(starter);

            // Faction Detail view
            AddFactionDetailMenu(starter);

            // Lifetime Summary display
            AddLifetimeSummaryMenu(starter);

            // Retinue menus (Phase 4)
            AddRetinueMenu(starter);
            AddRetinuePurchaseMenu(starter);
            AddRetinueDismissMenu(starter);

            // Requisition menu (Phase 5)
            AddRetinueRequisitionMenu(starter);

            // Companion Assignments menu (Phase 8)
            AddCompanionAssignmentsMenu(starter);

            // Phase 8: PayTension Action Menus
            AddDesperateMeasuresMenu(starter);
            AddHelpTheLordMenu(starter);
        }

        #region Enlisted Status Integration

        /// <summary>
        /// Adds the Camp option to the main enlisted status menu.
        /// </summary>
        private void AddCommandTentOptionToEnlistedMenu(CampaignGameStarter starter)
        {
            try
            {
                starter.AddGameMenuOption(
                    "enlisted_status",
                    "enlisted_command_tent",
                    "{=ct_menu_enter}Camp",
                    IsCommandTentAvailable,
                    OnCommandTentSelected,
                    false,
                    4); // Position after Camp Activities (keeps camp-related options grouped)

                ModLogger.Debug(LogCategory, "Added Camp option to enlisted_status menu");
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, $"Failed to add Camp option: {ex.Message}");
            }
        }


        /// <summary>
        /// Checks if the Camp option should be available (player must be enlisted).
        /// Only shows when enlisted (menu is only visible when enlisted anyway).
        /// </summary>
        private bool IsCommandTentAvailable(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
            args.Tooltip = new TextObject("{=ct_menu_tooltip}Access your personal camp area.");
            return EnlistmentBehavior.Instance?.IsEnlisted == true;
        }

        /// <summary>
        /// Opens the Camp main menu.
        /// </summary>
        private void OnCommandTentSelected(MenuCallbackArgs args)
        {
            try
            {
                // Capture time mode before SwitchToMenu changes it
                if (Campaign.Current != null)
                {
                    QuartermasterManager.CapturedTimeMode = Campaign.Current.TimeControlMode;
                }

                // If we're inside a settlement encounter, finish it first so the engine
                // doesn't immediately re-enter the town/castle menu when we switch.
                var encounterSettlement = PlayerEncounter.EncounterSettlement;
                if (encounterSettlement != null)
                {
                    var lordParty = EnlistmentBehavior.Instance?.CurrentLord?.PartyBelongedTo;
                    var inBattleOrSiege = lordParty?.Party.MapEvent != null ||
                                          lordParty?.Party.SiegeEvent != null ||
                                          lordParty?.BesiegedSettlement != null;

                    if (!inBattleOrSiege)
                    {
                        PlayerEncounter.Finish();
                        ModLogger.Info(LogCategory,
                            $"Finished settlement encounter ({encounterSettlement.Name}) before opening Camp");
                    }
                }

                SwitchToMenuPreserveTime(CommandTentMenuId);
                ModLogger.Debug(LogCategory, "Player entered Camp");
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, $"Failed to switch to Camp menu: {ex.Message}");
            }
        }

        #endregion

        #region Main Camp Menu

        /// <summary>
        /// Creates the main Camp menu with introduction text.
        /// </summary>
        private void AddMainCommandTentMenu(CampaignGameStarter starter)
        {
            // Use wait menu with hidden progress to allow spacebar time control (like Quartermaster)
            starter.AddWaitGameMenu(
                CommandTentMenuId,
                "{CT_MAIN_TEXT}",
                OnCommandTentInit,
                CommandTentWaitCondition,
                CommandTentWaitConsequence,
                CommandTentWaitTick,
                GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption);

            // Service Records option - Manage icon (scroll/quill)
            starter.AddGameMenuOption(
                CommandTentMenuId,
                "ct_service_records",
                "Review Service Records",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Manage;
                    return true;
                },
                _ => SwitchToMenuPreserveTime(LifetimeSummaryMenuId),
                false,
                1);

            // Seek Medical Attention (moved from main enlisted menu into Camp)
            starter.AddGameMenuOption(
                CommandTentMenuId,
                "ct_seek_medical",
                "{=Enlisted_Menu_SeekMedical}Seek Medical Attention",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Manage;
                    var conditions = Conditions.PlayerConditionBehavior.Instance;
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
                false,
                2);

            // Camp Activities - Visual Screen (MODERN UI)
            starter.AddGameMenuOption(
                CommandTentMenuId,
                "ct_camp_activities_visual",
                "🏕️ Visit Camp",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                    var activitiesBehavior = Features.Activities.CampActivitiesBehavior.Instance;
                    if (activitiesBehavior?.IsEnabled() != true)
                    {
                        args.IsEnabled = false;
                        args.Tooltip = new TextObject("{=menu_disabled_activities}Camp activities system is disabled.");
                        return true;
                    }
                    var count = activitiesBehavior.GetAvailableActivityCountForCurrentContext();
                    args.Tooltip = count > 0
                        ? new TextObject("Modern card-based activities menu. ({COUNT} available)").SetTextVariable("COUNT", count)
                        : new TextObject("Modern activities interface - No activities available at this time.");
                    return true;
                },
                _ => 
                {
                    // Phase 2: Open the new Camp Hub screen (with 6 location buttons)
                    CampHubScreen.Open(() =>
                    {
                        // Return to camp menu when closed
                        SwitchToMenuPreserveTime(CommandTentMenuId);
                    });
                },
                false,
                3);


            // ========================================
            // PAYTENSION ACTION MENUS (Phase 8)
            // ========================================

            // Desperate Measures (corruption path) - only visible at tension 40+
            starter.AddGameMenuOption(
                CommandTentMenuId,
                "ct_desperate_measures",
                "{=ct_desperate_measures}Desperate Measures...",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;
                    var enlistment = EnlistmentBehavior.Instance;
                    var tension = enlistment?.PayTension ?? 0;
                    
                    if (tension < 40)
                    {
                        return false; // Hide completely below threshold
                    }
                    
                    args.Tooltip = new TextObject("{=ct_desperate_tooltip}When times are hard, some turn to... creative solutions.");
                    return true;
                },
                _ => SwitchToMenuPreserveTime(DesperateMeasuresMenuId),
                false,
                7);

            // Help the Lord (loyalty path) - only visible at tension 40+
            starter.AddGameMenuOption(
                CommandTentMenuId,
                "ct_help_lord",
                "{=ct_help_lord}Help the Lord with Finances",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Mission;
                    var enlistment = EnlistmentBehavior.Instance;
                    var tension = enlistment?.PayTension ?? 0;
                    
                    if (tension < 40)
                    {
                        return false; // Hide completely below threshold
                    }
                    
                    args.Tooltip = new TextObject("{=ct_help_lord_tooltip}Volunteer for missions that might help the lord pay his debts.");
                    return true;
                },
                _ => SwitchToMenuPreserveTime(HelpTheLordMenuId),
                false,
                8);

            // Baggage Train (stash access) - Submenu icon
            starter.AddGameMenuOption(
                CommandTentMenuId,
                "ct_baggage_train",
                "{=enlisted_baggage_train}Visit Baggage Train",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                    args.Tooltip = new TextObject("{=qm_baggage_tooltip}Access your stored belongings (fatigue cost by rank).");
                    return true;
                },
                _ => EnlistmentBehavior.Instance?.TryOpenBaggageTrain(),
                false,
                9);

            // Request Discharge (Final Muster) / Cancel (toggle)
            starter.AddGameMenuOption(
                CommandTentMenuId,
                "ct_request_discharge",
                "{CT_DISCHARGE_OPTION_TEXT}",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                    var enlistment = EnlistmentBehavior.Instance;
                    if (enlistment?.IsPendingDischarge == true)
                    {
                        MBTextManager.SetTextVariable("CT_DISCHARGE_OPTION_TEXT",
                            new TextObject("{=ct_cancel_discharge_short}Cancel Discharge Request"));
                        args.Tooltip = new TextObject("{=ct_cancel_discharge_tooltip}Withdraw your discharge request and remain in service.");
                        return true;
                    }

                    MBTextManager.SetTextVariable("CT_DISCHARGE_OPTION_TEXT",
                        new TextObject("{=ct_request_discharge_short}Request Discharge"));
                    args.Tooltip = new TextObject("{=ct_discharge_tooltip}Request formal discharge with final pay settlement (resolves at next muster).");
                    return true;
                },
                _ =>
                {
                    var enlistment = EnlistmentBehavior.Instance;
                    if (enlistment == null)
                    {
                        return;
                    }

                    if (enlistment.IsPendingDischarge)
                    {
                        if (enlistment.CancelDischarge())
                        {
                            InformationManager.DisplayMessage(new InformationMessage(
                                new TextObject("{=ct_discharge_cancelled}Pending discharge cancelled.").ToString()));
                            SwitchToMenuPreserveTime(CommandTentMenuId);
                        }
                        return;
                    }

                    ShowRequestDischargeConfirmPopup();
                },
                false,
                4);

            // Personal Retinue option - TroopSelection icon (soldiers)
            starter.AddGameMenuOption(
                CommandTentMenuId,
                "ct_retinue",
                "{=ct_option_retinue}Muster Personal Retinue",
                IsRetinueAvailable,
                _ => SwitchToMenuPreserveTime(RetinueMenuId),
                false,
                3);

            // Companion Assignments option - Conversation icon (speech)
            starter.AddGameMenuOption(
                CommandTentMenuId,
                "ct_companions",
                "{=ct_option_companions}Companion Assignments",
                IsCompanionAssignmentsAvailable,
                _ => SwitchToMenuPreserveTime(CompanionAssignmentsMenuId),
                false,
                3);

            // Back to camp option - Leave icon (door)
            starter.AddGameMenuOption(
                CommandTentMenuId,
                "ct_back",
                "{=ct_option_back}Return to Camp",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    return true;
                },
                _ => SwitchToMenuPreserveTime("enlisted_status"),
                true,
                100);
        }

        /// <summary>
        /// Initializes the main Camp menu.
        /// </summary>
        private void OnCommandTentInit(MenuCallbackArgs args)
        {
            // Refresh inline icons in case they were cleared
            SetupInlineIcons();
            MBTextManager.SetTextVariable("CT_MAIN_TEXT", BuildCampMainMenuText());
            ModLogger.Debug(LogCategory, "Camp menu initialized");
        }

        /// <summary>
        /// Builds the main Camp menu text (overview + news + camp bulletin).
        /// </summary>
        private static string BuildCampMainMenuText()
        {
            try
            {
                var enlistment = EnlistmentBehavior.Instance;
                var svc = ServiceRecordManager.Instance;
                var campLife = CampLifeBehavior.Instance;
                var cond = Conditions.PlayerConditionBehavior.Instance;
                var news = EnlistedNewsBehavior.Instance;

                var sb = new StringBuilder();
                sb.AppendLine(new TextObject("{=ct_menu_intro}Maps and tallies cover the makeshift table. Your small corner of the army's camp.").ToString());
                sb.AppendLine();

                // News / what's happening (keep this screen focused on "what's going on")
                var personalNews = news?.BuildPersonalNewsSection(2);
                if (!string.IsNullOrWhiteSpace(personalNews))
                {
                    sb.AppendLine(personalNews.TrimEnd());
                }

                var kingdomNews = news?.BuildKingdomNewsSection(2);
                if (!string.IsNullOrWhiteSpace(kingdomNews))
                {
                    sb.AppendLine(kingdomNews.TrimEnd());
                }

                // Compact personal overview (few key stats only)
                sb.AppendLine();
                sb.AppendLine("— Overview —");
                sb.AppendLine();

                if (enlistment != null)
                {
                    sb.AppendLine($"Rank Tier: {enlistment.EnlistmentTier}");
                    sb.AppendLine($"Days Served: {(int)enlistment.DaysServed}");
                    sb.AppendLine($"Fatigue: {enlistment.FatigueCurrent}/{enlistment.FatigueMax}");
                }

                if (cond?.IsEnabled() == true && cond.State?.HasAnyCondition == true)
                {
                    var parts = new List<string>();
                    if (cond.State.HasInjury)
                    {
                        parts.Add($"Injured ({cond.State.InjuryDaysRemaining}d)");
                    }
                    if (cond.State.HasIllness)
                    {
                        parts.Add($"Ill ({cond.State.IllnessDaysRemaining}d)");
                    }
                    if (parts.Count > 0)
                    {
                        sb.AppendLine($"Condition: {string.Join(", ", parts)}");
                    }
                }

                // Keep term details in Service Records (avoid clutter here)
                if (svc != null && svc.CurrentTermBattles > 0)
                {
                    sb.AppendLine($"This Term: {svc.CurrentTermBattles} battles, {svc.CurrentTermKills} kills");
                }

                // Camp bulletin / news
                sb.AppendLine();
                sb.AppendLine("— Camp Bulletin —");
                sb.AppendLine();

                if (campLife != null && campLife.IsActiveWhileEnlisted())
                {
                    sb.AppendLine($"Quartermaster Mood: {campLife.QuartermasterMoodTier}");

                    // Short, readable "news" lines driven by the meters.
                    if (campLife.IsLogisticsHigh())
                    {
                        sb.AppendLine("Supplies are tight. Quartermasters are getting sharp-eyed about requisitions.");
                    }

                    if (campLife.IsMoraleLow())
                    {
                        sb.AppendLine("The men are weary after recent fighting. Tempers are short around the fires.");
                    }

                    if (campLife.IsPayTensionHigh())
                    {
                        sb.AppendLine("Pay is late. Grumbling is turning into open talk.");
                    }

                    if (!campLife.IsLogisticsHigh() && !campLife.IsMoraleLow() && !campLife.IsPayTensionHigh())
                    {
                        sb.AppendLine("Routine holds. Drill, rations, and watch rotations grind on.");
                    }
                }
                else
                {
                    sb.AppendLine("Camp routines continue. (CampLife is disabled.)");
                }

                return sb.ToString();
            }
            catch
            {
                return new TextObject("{=ct_menu_intro}Maps and tallies cover the makeshift table. Your small corner of the army's camp.").ToString();
            }
        }

        /// <summary>
        /// Confirmation popup for requesting discharge (final muster next payday).
        /// </summary>
        private static void ShowRequestDischargeConfirmPopup()
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment?.IsEnlisted != true)
            {
                return;
            }

            var daysServed = (int)enlistment.DaysServed;
            var pendingPay = enlistment.PendingMusterPay;

            // Predict discharge "band" using the same thresholds as FinalizePendingDischarge().
            var cfg = EnlistedConfig.LoadRetirementConfig();
            var band = daysServed >= 200 ? "veteran" : daysServed >= 100 ? "honorable" : "washout";

            var lord = enlistment.CurrentLord;
            if ((band == "honorable" || band == "veteran") && lord != null && Hero.MainHero.GetRelation(lord) < 0)
            {
                band = "washout";
            }

            var lordRelation = 0;
            var factionLeaderRelation = 0;
            var severance = 0;

            switch (band)
            {
                case "veteran":
                    lordRelation = 30;
                    factionLeaderRelation = 15;
                    severance = Math.Max(0, cfg?.SeveranceVeteran ?? 3000);
                    break;
                case "honorable":
                    lordRelation = 10;
                    factionLeaderRelation = 5;
                    severance = Math.Max(0, cfg?.SeveranceHonorable ?? 3000);
                    break;
                default:
                    lordRelation = -10;
                    factionLeaderRelation = -10;
                    severance = 0;
                    break;
            }

            var payout = Math.Max(0, pendingPay) + Math.Max(0, severance);

            var sb = new StringBuilder();
            sb.AppendLine("Request discharge?");
            sb.AppendLine();
            sb.AppendLine("This will take effect at your next pay muster.");
            sb.AppendLine();
            sb.AppendLine("— Expected Outcome —");
            sb.AppendLine($"Discharge quality: {band}");
            sb.AppendLine($"Final payout (estimate): {payout} (Pending pay {pendingPay} + Severance {severance})");
            sb.AppendLine();
            sb.AppendLine("— Relationship Impact (estimate) —");
            sb.AppendLine($"Lord: {(lordRelation >= 0 ? "+" : "")}{lordRelation}");
            sb.AppendLine($"Faction leader: {(factionLeaderRelation >= 0 ? "+" : "")}{factionLeaderRelation}");
            sb.AppendLine();
            sb.AppendLine("Note: If your relation with your lord is negative at discharge, honorable/veteran exit is not granted.");

            InformationManager.ShowInquiry(
                new InquiryData(
                    new TextObject("{=ct_request_discharge_confirm_title}Request Discharge").ToString(),
                    sb.ToString(),
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
                            SwitchToMenuPreserveTime(CommandTentMenuId);
                        }
                    },
                    () => { }),
                false);
        }

        /// <summary>
        /// Resolves an activity text ID to localized text.
        /// </summary>
        private static string ResolveActivityText(string textId, string fallback)
        {
            if (string.IsNullOrEmpty(textId))
            {
                return fallback;
            }

            try
            {
                var t = new TextObject("{=" + textId + "}" + fallback);
                return t.ToString();
            }
            catch
            {
                return fallback;
            }
        }

        #endregion


        #region Menu Background and Audio
        
        /// <summary>
        /// Menu background and audio initialization for Camp menus.
        /// Sets military-themed background and ambient audio for immersion.
        /// Resumes time so it continues passing while browsing menus.
        /// </summary>
        [GameMenuInitializationHandler(CommandTentMenuId)]
        [GameMenuInitializationHandler(ServiceRecordsMenuId)]
        [GameMenuInitializationHandler(CurrentPostingMenuId)]
        [GameMenuInitializationHandler(FactionRecordsMenuId)]
        [GameMenuInitializationHandler(FactionDetailMenuId)]
        [GameMenuInitializationHandler(LifetimeSummaryMenuId)]
        [GameMenuInitializationHandler(RetinueMenuId)]
        [GameMenuInitializationHandler(RetinuePurchaseMenuId)]
        [GameMenuInitializationHandler(RetinueDismissMenuId)]
        [GameMenuInitializationHandler(RetinueRequisitionMenuId)]
        [GameMenuInitializationHandler(CompanionAssignmentsMenuId)]
        public static void CommandTentMenuBackgroundInit(MenuCallbackArgs args)
        {
            // Use a military meeting/camp background
            args.MenuContext.SetBackgroundMeshName("encounter_meeting");
            
            // Add ambient audio for the camp atmosphere
            args.MenuContext.SetAmbientSound("event:/map/ambient/node/settlements/2d/keep");
            
            // NOTE: We intentionally do NOT call StartWait() here. Camp menus rely on
            // the wait-menu type for layout/options, but we keep time control unchanged and
            // preserve the player's captured time mode when hopping between submenus.
        }

        /// <summary>
        /// Checks if retinue option is available (Tier 4+ only).
        /// Uses TroopSelection icon for soldier management.
        /// </summary>
        private bool IsRetinueAvailable(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.TroopSelection;
            var enlistment = EnlistmentBehavior.Instance;
            if ((enlistment?.EnlistmentTier ?? 1) < 4)
            {
                args.IsEnabled = false;
                args.Tooltip = new TextObject("{=ct_warn_tier_locked}You must reach Tier 4 to command soldiers.");
                return true;
            }
            args.Tooltip = new TextObject("{=ct_retinue_tooltip}Recruit and manage your personal retinue of soldiers.");
            return true;
        }

        #endregion

        #region Service Records Menu

        /// <summary>
        /// Creates the Service Records submenu with options for different record views.
        /// </summary>
        private void AddServiceRecordsMenu(CampaignGameStarter starter)
        {
            // Keep the menu registration minimal; service records now go straight to LifetimeSummary
            starter.AddWaitGameMenu(
                ServiceRecordsMenuId,
                "{=ct_records_intro}Your service history and military records.",
                OnServiceRecordsInit,
                CommandTentWaitCondition,
                CommandTentWaitConsequence,
                CommandTentWaitTick,
                GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption);
        }

        private void OnServiceRecordsInit(MenuCallbackArgs args)
        {
            _ = args;
            ModLogger.Debug(LogCategory, "Service Records menu initialized");
        }

        #endregion

        #region Current Posting Menu

        /// <summary>
        /// Creates the Current Posting display showing current enlistment details.
        /// </summary>
        private void AddCurrentPostingMenu(CampaignGameStarter starter)
        {
            starter.AddWaitGameMenu(
                CurrentPostingMenuId,
                "{CURRENT_POSTING_TEXT}",
                OnCurrentPostingInit,
                CommandTentWaitCondition,
                CommandTentWaitConsequence,
                CommandTentWaitTick,
                GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption);

            // Back option
            starter.AddGameMenuOption(
                CurrentPostingMenuId,
                "ct_posting_back",
                "{=ct_back_records}Back to Service Records",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    return true;
                },
                _ => SwitchToMenuPreserveTime(ServiceRecordsMenuId),
                true,
                100);
        }

        /// <summary>
        /// Initializes the Current Posting display with live data.
        /// </summary>
        private void OnCurrentPostingInit(MenuCallbackArgs args)
        {
            _ = args;
            try
            {
                var text = BuildCurrentPostingText();
                MBTextManager.SetTextVariable("CURRENT_POSTING_TEXT", text);
                ModLogger.Debug(LogCategory, "Current Posting display initialized");
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, $"Error initializing Current Posting: {ex.Message}");
                MBTextManager.SetTextVariable("CURRENT_POSTING_TEXT", "Error loading current posting data.");
            }
        }

        /// <summary>
        /// Builds the formatted text for current posting display.
        /// Uses clean, modern formatting without ASCII art.
        /// </summary>
        private string BuildCurrentPostingText()
        {
            var sb = new StringBuilder();
            var enlistment = EnlistmentBehavior.Instance;
            var recordManager = ServiceRecordManager.Instance;

            if (enlistment?.IsEnlisted != true)
            {
                return "You are not currently enlisted.";
            }

            var lord = enlistment.CurrentLord;
            var faction = lord?.Clan?.MapFaction;
            var tier = enlistment.EnlistmentTier;
            var daysServed = (int)enlistment.DaysServed;
            var daysRemaining = GetDaysRemaining(enlistment);
            var factionName = faction?.Name?.ToString() ?? "Unknown";
            var lordName = lord?.Name?.ToString() ?? "Unknown";
            var rankName = GetRankName(tier);
            var termBattles = recordManager?.CurrentTermBattles ?? 0;
            var termKills = recordManager?.CurrentTermKills ?? 0;
            var pendingPay = enlistment.PendingMusterPay;
            var nextPay = enlistment.NextPaydaySafe;
            var daysToPay = Math.Max(0, (float)(nextPay - CampaignTime.Now).ToDays);
            var lastPay = enlistment.LastPayOutcome;
            var musterQueued = enlistment.IsPayMusterPending;
            var pensionDaily = GetPensionDaily(enlistment);
            var pensionStatus = GetPensionStatus(enlistment);

            sb.AppendLine();
            sb.AppendLine("— Current Service Record —");
            sb.AppendLine();
            sb.AppendLine($"Posting: Army of {factionName}");
            sb.AppendLine($"Commander: {lordName}");
            sb.AppendLine($"Rank: {rankName} (Tier {tier})");
            sb.AppendLine();
            sb.AppendLine($"Days Served: {daysServed}");
            sb.AppendLine($"Contract: {daysRemaining}");
            sb.AppendLine();
            sb.AppendLine("— Pay Muster —");
            sb.AppendLine();
            sb.AppendLine($"Pending Muster Pay: {pendingPay} denars");
            sb.AppendLine($"Next Payday: in {daysToPay:F1} days");
            sb.AppendLine($"Last Outcome: {lastPay}");
            if (musterQueued)
            {
                sb.AppendLine("Status: Muster queued");
            }
            if (enlistment.IsPendingDischarge)
            {
                sb.AppendLine("Status: Discharge pending (will resolve at next pay muster)");
            }
            sb.AppendLine();
            sb.AppendLine("— Pension —");
            sb.AppendLine();
            sb.AppendLine($"Daily Pension: {pensionDaily} denars");
            sb.AppendLine($"Status: {pensionStatus}");
            sb.AppendLine();
            sb.AppendLine("— This Term —");
            sb.AppendLine();
            sb.AppendLine($"Battles Fought: {termBattles}");
            sb.AppendLine($"Enemies Slain: {termKills}");

            return sb.ToString();
        }

        private static int GetPensionDaily(EnlistmentBehavior enlistment)
        {
            return enlistment?.PensionAmountPerDay ?? 0;
        }

        private static string GetPensionStatus(EnlistmentBehavior enlistment)
        {
            if (enlistment == null)
            {
                return "Unknown";
            }

            if (enlistment.IsPensionPaused)
            {
                return "Paused";
            }

            return "Active";
        }

        /// <summary>
        /// Calculates days remaining in current term based on term type (first term vs renewal).
        /// </summary>
        private static string GetDaysRemaining(EnlistmentBehavior enlistment)
        {
            try
            {
                var remainingDays = 0;
                var faction = enlistment.CurrentLord?.MapFaction;

                if (faction != null)
                {
                    // Check if in renewal term first
                    var record = enlistment.GetFactionVeteranRecord(faction);
                    if (record is { IsInRenewalTerm: true } && record.CurrentTermEnd != CampaignTime.Zero)
                    {
                        remainingDays = (int)(record.CurrentTermEnd - CampaignTime.Now).ToDays;
                    }
                    else
                    {
                        // First term calculation
                        var retirementConfig = EnlistedConfig.LoadRetirementConfig();
                        var termEnd = enlistment.EnlistmentDate + CampaignTime.Days(retirementConfig.FirstTermDays);
                        remainingDays = (int)(termEnd - CampaignTime.Now).ToDays;
                    }
                }

                if (remainingDays <= 0)
                {
                    return "Term complete";
                }

                return $"{remainingDays} days";
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, $"Error calculating days remaining: {ex.Message}");
                return "Unknown";
            }
        }

        #endregion

        #region Faction Records Menu

        /// <summary>
        /// Creates the Faction Records list showing all factions served.
        /// </summary>
        private void AddFactionRecordsMenu(CampaignGameStarter starter)
        {
            starter.AddWaitGameMenu(
                FactionRecordsMenuId,
                "{FACTION_RECORDS_TEXT}",
                OnFactionRecordsInit,
                CommandTentWaitCondition,
                CommandTentWaitConsequence,
                CommandTentWaitTick,
                GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption);

            // Dynamic faction options will be added during init
            // For now, add a static back option
            starter.AddGameMenuOption(
                FactionRecordsMenuId,
                "ct_factions_back",
                "{=ct_back_records}Back to Service Records",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    return true;
                },
                _ => SwitchToMenuPreserveTime(ServiceRecordsMenuId),
                true,
                100);
        }

        /// <summary>
        /// Initializes the Faction Records list.
        /// </summary>
        private void OnFactionRecordsInit(MenuCallbackArgs args)
        {
            _ = args;
            try
            {
                var text = BuildFactionRecordsListText();
                MBTextManager.SetTextVariable("FACTION_RECORDS_TEXT", text);
                ModLogger.Debug(LogCategory, "Faction Records list initialized");
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, $"Error initializing Faction Records: {ex.Message}");
                MBTextManager.SetTextVariable("FACTION_RECORDS_TEXT", "Error loading faction records.");
            }
        }

        /// <summary>
        /// Builds the faction records list text showing all factions served with summary stats.
        /// Uses clean, modern formatting.
        /// </summary>
        private string BuildFactionRecordsListText()
        {
            var sb = new StringBuilder();
            var recordManager = ServiceRecordManager.Instance;
            var records = recordManager?.GetAllRecords();

            sb.AppendLine();
            sb.AppendLine("— Faction Service Records —");
            sb.AppendLine();

            if (records == null || records.Count == 0)
            {
                sb.AppendLine("No faction service records found.");
                sb.AppendLine();
                sb.AppendLine("Enlist with a lord to begin building your military service history.");
            }
            else
            {
                foreach (var record in records.Values.OrderByDescending(r => r.TotalDaysServed))
                {
                    var factionType = FormatFactionType(record.FactionType);
                    sb.AppendLine($"• {record.FactionDisplayName}");
                    sb.AppendLine($"  {factionType} — {record.TermsCompleted} terms, {record.TotalDaysServed} days");
                    sb.AppendLine($"  Kills: {record.TotalKills}");
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Formats the faction type for display.
        /// </summary>
        private static string FormatFactionType(string factionType)
        {
            return factionType switch
            {
                "kingdom" => "Kingdom",
                "minor" => "Minor Faction",
                "merc" => "Mercenary Company",
                "clan" => "Noble Clan",
                "bandit" => "Bandit Clan",
                _ => "Unknown"
            };
        }

        #endregion

        #region Faction Detail Menu

        /// <summary>
        /// Creates the Faction Detail view for a specific faction.
        /// </summary>
        private void AddFactionDetailMenu(CampaignGameStarter starter)
        {
            starter.AddWaitGameMenu(
                FactionDetailMenuId,
                "{FACTION_DETAIL_TEXT}",
                OnFactionDetailInit,
                CommandTentWaitCondition,
                CommandTentWaitConsequence,
                CommandTentWaitTick,
                GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption);

            // Back option
            starter.AddGameMenuOption(
                FactionDetailMenuId,
                "ct_detail_back",
                "{=ct_back_factions}Back to Faction Records",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    return true;
                },
                _ => SwitchToMenuPreserveTime(FactionRecordsMenuId),
                true,
                100);
        }

        /// <summary>
        /// Initializes the Faction Detail display.
        /// </summary>
        private void OnFactionDetailInit(MenuCallbackArgs args)
        {
            try
            {
                var text = BuildFactionDetailText(_selectedFactionKey);
                MBTextManager.SetTextVariable("FACTION_DETAIL_TEXT", text);
                ModLogger.Debug(LogCategory, $"Faction Detail initialized for: {_selectedFactionKey}");
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, $"Error initializing Faction Detail: {ex.Message}");
                MBTextManager.SetTextVariable("FACTION_DETAIL_TEXT", "Error loading faction detail.");
            }
        }

        /// <summary>
        /// Builds detailed faction record text.
        /// Uses clean, modern formatting.
        /// </summary>
        private string BuildFactionDetailText(string factionKey)
        {
            var sb = new StringBuilder();
            var recordManager = ServiceRecordManager.Instance;
            var record = recordManager?.GetRecord(factionKey);

            if (record == null)
            {
                return "No record found for this faction.";
            }

            var highestRank = GetRankName(record.HighestTier);

            sb.AppendLine();
            sb.AppendLine($"— {record.FactionDisplayName} —");
            sb.AppendLine();
            sb.AppendLine($"Enlistments: {record.Enlistments}");
            sb.AppendLine($"Terms Completed: {record.TermsCompleted}");
            sb.AppendLine($"Days Served: {record.TotalDaysServed}");
            sb.AppendLine($"Highest Rank: {highestRank} (Tier {record.HighestTier})");
            sb.AppendLine();
            sb.AppendLine("— Combat Record —");
            sb.AppendLine();
            sb.AppendLine($"Battles Fought: {record.BattlesFought}");
            sb.AppendLine($"Enemies Slain: {record.TotalKills}");
            sb.AppendLine($"Lords Served: {record.LordsServed}");

            return sb.ToString();
        }

        /// <summary>
        /// Sets the selected faction for detail view and navigates to detail menu.
        /// </summary>
        public void ViewFactionDetail(string factionKey)
        {
            _selectedFactionKey = factionKey;
            SwitchToMenuPreserveTime(FactionDetailMenuId);
        }

        #endregion

        #region Lifetime Summary Menu

        /// <summary>
        /// Creates the Lifetime Summary display showing cross-faction totals.
        /// </summary>
        private void AddLifetimeSummaryMenu(CampaignGameStarter starter)
        {
            starter.AddWaitGameMenu(
                LifetimeSummaryMenuId,
                "{LIFETIME_SUMMARY_TEXT}",
                OnLifetimeSummaryInit,
                CommandTentWaitCondition,
                CommandTentWaitConsequence,
                CommandTentWaitTick,
                GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption);

            // Back option
            starter.AddGameMenuOption(
                LifetimeSummaryMenuId,
                "ct_lifetime_back",
                "{=ct_back_records}Back to Camp",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    return true;
                },
                _ => SwitchToMenuPreserveTime(CommandTentMenuId),
                true,
                100);
        }

        /// <summary>
        /// Initializes the Lifetime Summary display.
        /// </summary>
        private void OnLifetimeSummaryInit(MenuCallbackArgs args)
        {
            _ = args;
            try
            {
                var text = BuildLifetimeSummaryText();
                MBTextManager.SetTextVariable("LIFETIME_SUMMARY_TEXT", text);
                ModLogger.Debug(LogCategory, "Lifetime Summary initialized");
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, $"Error initializing Lifetime Summary: {ex.Message}");
                MBTextManager.SetTextVariable("LIFETIME_SUMMARY_TEXT", "Error loading lifetime summary.");
            }
        }

        /// <summary>
        /// Builds the lifetime summary text showing cross-faction statistics.
        /// Uses clean, modern formatting.
        /// </summary>
        private string BuildLifetimeSummaryText()
        {
            var sb = new StringBuilder();
            var recordManager = ServiceRecordManager.Instance;
            var lifetime = recordManager?.LifetimeRecord;

            sb.AppendLine();
            sb.AppendLine("— Lifetime Service Summary —");
            sb.AppendLine();

            if (lifetime == null)
            {
                sb.AppendLine("No lifetime service records found.");
                sb.AppendLine();
                sb.AppendLine("Your military career has not yet begun.");
            }
            else
            {
                // Calculate years and months from total days
                var (years, months) = lifetime.GetServiceDuration();
                var timeString = years > 0
                    ? $"{years} year{(years > 1 ? "s" : "")}, {months} month{(months != 1 ? "s" : "")}"
                    : $"{months} month{(months != 1 ? "s" : "")}";

                if (years == 0 && months == 0)
                {
                    timeString = $"{lifetime.TotalDaysServed} days";
                }

                sb.AppendLine($"Time in Service: {timeString}");
                sb.AppendLine($"Total Enlistments: {lifetime.TotalEnlistments}");
                sb.AppendLine($"Terms Completed: {lifetime.TermsCompleted}");
                sb.AppendLine();

                // Current service snapshot (replaces the removed Activity Log + XP Breakdown menus)
                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment?.IsEnlisted == true)
                {
                    sb.AppendLine("— Current Enlistment —");
                    sb.AppendLine();
                    sb.AppendLine($"Tier: {enlistment.EnlistmentTier}");
                    sb.AppendLine($"Service XP: {enlistment.EnlistmentXP}");
                    sb.AppendLine($"Days Served: {(int)enlistment.DaysServed}");
                    sb.AppendLine($"Fatigue: {enlistment.FatigueCurrent}/{enlistment.FatigueMax}");

                    if (enlistment.EnlistmentTier < 6)
                    {
                        var nextTierXp = Enlisted.Features.Assignments.Core.ConfigurationManager.GetXpRequiredForTier(enlistment.EnlistmentTier);
                        sb.AppendLine($"Next Tier Requirement: {nextTierXp} XP");
                    }
                    else
                    {
                        sb.AppendLine("Next Tier Requirement: (Max tier)");
                    }

                    if (enlistment.IsPendingDischarge)
                    {
                        sb.AppendLine("Discharge: Pending (resolves at next pay muster)");
                    }

                    if (enlistment.PendingMusterPay > 0)
                    {
                        sb.AppendLine($"Pending Pay (next muster): {enlistment.PendingMusterPay}");
                    }

                    if (enlistment.OwedBackpay > 0)
                    {
                        sb.AppendLine($"Owed Backpay: {enlistment.OwedBackpay}");
                    }

                    sb.AppendLine();
                    sb.AppendLine("— XP Sources —");
                    sb.AppendLine();
                    sb.AppendLine("• Battles: XP for participating in combat");
                    sb.AppendLine("• Duties: Daily XP for assigned tasks");
                    sb.AppendLine("• Activities: Skill XP from camp and lance activities");
                    sb.AppendLine("• Service: Passive XP for time served");
                    sb.AppendLine();

                    sb.AppendLine("— This Term —");
                    sb.AppendLine();
                    sb.AppendLine($"Battles: {recordManager.CurrentTermBattles}");
                    sb.AppendLine($"Kills: {recordManager.CurrentTermKills}");
                    sb.AppendLine();
                }

                // List factions served
                if (lifetime.FactionsServed != null && lifetime.FactionsServed.Count > 0)
                {
                    sb.AppendLine("— Factions Served —");
                    sb.AppendLine();

                    var allRecords = recordManager.GetAllRecords();
                    foreach (var factionId in lifetime.FactionsServed)
                    {
                        if (allRecords.TryGetValue(factionId, out var record))
                        {
                            var terms = record.TermsCompleted > 0
                                ? $"{record.TermsCompleted} term{(record.TermsCompleted > 1 ? "s" : "")}"
                                : "in progress";
                            sb.AppendLine($"• {record.FactionDisplayName} ({terms})");
                        }
                    }
                    sb.AppendLine();
                }

                sb.AppendLine("— Combat Statistics —");
                sb.AppendLine();
                sb.AppendLine($"Total Battles: {lifetime.TotalBattlesFought}");
                sb.AppendLine($"Enemies Slain: {lifetime.LifetimeKills}");
            }

            return sb.ToString();
        }

        #endregion

        #region Retinue Menu

        /// <summary>
        /// Creates the Personal Retinue submenu showing current muster status and options.
        /// </summary>
        private void AddRetinueMenu(CampaignGameStarter starter)
        {
            starter.AddWaitGameMenu(
                RetinueMenuId,
                "{RETINUE_STATUS_TEXT}",
                OnRetinueMenuInit,
                CommandTentWaitCondition,
                CommandTentWaitConsequence,
                CommandTentWaitTick,
                GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption);

            // Current Muster option - ManageGarrison icon (soldiers/shield)
            starter.AddGameMenuOption(
                RetinueMenuId,
                "ct_retinue_muster",
                "{=ct_retinue_current_muster}View Current Muster",
                args =>
                {
                    var manager = RetinueManager.Instance;
                    var hasRetinue = manager?.State?.HasRetinue == true;
                    args.IsEnabled = hasRetinue;
                    // Hint refreshes when enabled; clear guidance when empty.
                    args.Tooltip = hasRetinue
                        ? new TextObject("{=ct_retinue_muster_hint}Select to refresh the menu and see updated retinue data.")
                        : new TextObject("{=ct_retinue_no_soldiers}You have no soldiers mustered.");
                    args.optionLeaveType = GameMenuOption.LeaveType.ManageGarrison;
                    return true;
                },
                _ =>
                {
                    // Refresh the menu to show current muster breakdown
                    SwitchToMenuPreserveTime(RetinueMenuId);
                },
                false,
                1);

            // Purchase Soldiers option - Recruit icon
            starter.AddGameMenuOption(
                RetinueMenuId,
                "ct_retinue_purchase",
                "{=ct_retinue_purchase}Purchase Soldiers",
                args =>
                {
                    var manager = RetinueManager.Instance;
                    var enlistment = EnlistmentBehavior.Instance;

                    if (manager != null && enlistment != null)
                    {
                        var tierCapacity = RetinueManager.GetTierCapacity(enlistment.EnlistmentTier);
                        var currentSoldiers = manager.State?.TotalSoldiers ?? 0;
                        var partySpace = GetAvailablePartySpace();

                        if (currentSoldiers >= tierCapacity)
                        {
                            args.IsEnabled = false;
                            args.Tooltip = new TextObject("{=ct_warn_full_retinue}Your retinue is at full strength.");
                        }
                        else if (partySpace <= 0)
                        {
                            args.IsEnabled = false;
                            args.Tooltip = new TextObject("{=ct_warn_party_full}Your party is full. Dismiss troops or increase party size.");
                        }
                    }

                    args.optionLeaveType = GameMenuOption.LeaveType.Recruit;
                    return true;
                },
                _ => SwitchToMenuPreserveTime(RetinuePurchaseMenuId),
                false,
                2);

            // Requisition Soldiers option - Trade icon (coins)
            starter.AddGameMenuOption(
                RetinueMenuId,
                "ct_retinue_requisition",
                "{REQUISITION_OPTION_TEXT}",
                IsRequisitionAvailable,
                _ => SwitchToMenuPreserveTime(RetinueRequisitionMenuId),
                false,
                3);

            // Dismiss Soldiers option - DonateTroops icon
            starter.AddGameMenuOption(
                RetinueMenuId,
                "ct_retinue_dismiss",
                "{=ct_retinue_dismiss}Dismiss Soldiers",
                args =>
                {
                    var manager = RetinueManager.Instance;
                    var hasRetinue = manager?.State?.HasRetinue == true;
                    args.IsEnabled = hasRetinue;
                    if (!hasRetinue)
                    {
                        args.Tooltip = new TextObject("{=ct_retinue_no_soldiers}You have no soldiers to dismiss.");
                    }
                    args.optionLeaveType = GameMenuOption.LeaveType.DonateTroops;
                    return true;
                },
                _ => SwitchToMenuPreserveTime(RetinueDismissMenuId),
                false,
                4);

            // Back option
            starter.AddGameMenuOption(
                RetinueMenuId,
                "ct_retinue_back",
                "{=ct_back_tent}Back to Camp",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    return true;
                },
                _ => SwitchToMenuPreserveTime(CommandTentMenuId),
                true,
                100);
        }

        /// <summary>
        /// Initializes the Retinue menu with current status.
        /// </summary>
        private void OnRetinueMenuInit(MenuCallbackArgs args)
        {
            _ = args;
            try
            {
                var text = BuildRetinueStatusText();
                MBTextManager.SetTextVariable("RETINUE_STATUS_TEXT", text);

                // Set requisition option text with cost display
                SetRequisitionOptionText();

                ModLogger.Debug(LogCategory, "Retinue menu initialized");
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, $"Error initializing Retinue menu: {ex.Message}");
                MBTextManager.SetTextVariable("RETINUE_STATUS_TEXT", "Error loading retinue status.");
            }
        }

        /// <summary>
        /// Sets the requisition menu option text with cost and cooldown info.
        /// Uses inline gold icon for currency display.
        /// </summary>
        private static void SetRequisitionOptionText()
        {
            var manager = RetinueManager.Instance;
            if (manager == null)
            {
                MBTextManager.SetTextVariable("REQUISITION_OPTION_TEXT", "Requisition Men");
                return;
            }

            var cost = manager.CalculateRequisitionCost();
            var missing = manager.GetMissingSoldierCount();
            var cooldownDays = manager.GetRequisitionCooldownDays();

            string optionText;
            if (cooldownDays > 0)
            {
                optionText = $"Requisition Men ({cooldownDays}d cooldown)";
            }
            else if (missing <= 0)
            {
                optionText = "Requisition Men (at capacity)";
            }
            else
            {
                // Use inline gold icon for cost display
                optionText = $"Requisition Men ({cost}{{GOLD_ICON}})";
            }

            MBTextManager.SetTextVariable("REQUISITION_OPTION_TEXT", optionText);
        }

        /// <summary>
        /// Builds the retinue status text showing current muster and capacity.
        /// Uses clean formatting with inline gold icons for currency.
        /// </summary>
        private string BuildRetinueStatusText()
        {
            var sb = new StringBuilder();
            var enlistment = EnlistmentBehavior.Instance;
            var manager = RetinueManager.Instance;

            if (enlistment?.IsEnlisted != true)
            {
                return "You are not currently enlisted.";
            }

            var tier = enlistment.EnlistmentTier;
            var rankName = GetRankName(tier);
            var unitName = GetUnitNameForTier(tier);
            var tierCapacity = RetinueManager.GetTierCapacity(tier);
            var currentSoldiers = manager?.State?.TotalSoldiers ?? 0;
            var selectedType = manager?.State?.SelectedTypeId;
            var partySpace = GetAvailablePartySpace();
            var dailyUpkeep = currentSoldiers * 2;
            var partyLimit = PartyBase.MainParty?.PartySizeLimit ?? 0;
            var currentMembers = PartyBase.MainParty?.NumberOfAllMembers ?? 0;

            sb.AppendLine();
            sb.AppendLine("— Personal Retinue —");
            sb.AppendLine();
            sb.AppendLine($"Rank: {rankName} (Tier {tier})");
            sb.AppendLine($"Command Limit: {unitName}");
            sb.AppendLine();
            sb.AppendLine("— Current Muster —");
            sb.AppendLine();

            if (string.IsNullOrEmpty(selectedType))
            {
                sb.AppendLine("No soldiers mustered.");
                sb.AppendLine("Select a soldier type to begin.");
            }
            else
            {
                var typeName = GetSoldierTypeName(selectedType, enlistment.CurrentLord?.Culture);
                sb.AppendLine($"Type: {typeName}");
                sb.AppendLine($"Soldiers: {currentSoldiers} / {tierCapacity}");
                sb.AppendLine($"Daily Upkeep: {dailyUpkeep}{{GOLD_ICON}}");
            }

            sb.AppendLine();
            sb.AppendLine("— Party Capacity —");
            sb.AppendLine();
            sb.AppendLine($"Party Limit: {partyLimit}");
            sb.AppendLine($"Current Members: {currentMembers}");
            sb.AppendLine($"Available Space: {partySpace}");

            if (partySpace < tierCapacity - currentSoldiers)
            {
                sb.AppendLine();
                sb.AppendLine("Party size limits your retinue.");
            }

            return sb.ToString();
        }

        #endregion

        #region Retinue Purchase Menu

        /// <summary>
        /// Creates the soldier type selection and purchase menu.
        /// </summary>
        private void AddRetinuePurchaseMenu(CampaignGameStarter starter)
        {
            starter.AddWaitGameMenu(
                RetinuePurchaseMenuId,
                "{RETINUE_PURCHASE_TEXT}",
                OnRetinuePurchaseInit,
                CommandTentWaitCondition,
                CommandTentWaitConsequence,
                CommandTentWaitTick,
                GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption);

            // Infantry option - Men-at-Arms
            starter.AddGameMenuOption(
                RetinuePurchaseMenuId,
                "ct_purchase_infantry",
                "{INFANTRY_OPTION_TEXT}",
                args => IsSoldierTypeAvailable(args, "infantry"),
                _ => OnSoldierTypePurchase("infantry"),
                false,
                1);

            // Archers option - Bowmen
            starter.AddGameMenuOption(
                RetinuePurchaseMenuId,
                "ct_purchase_archers",
                "{ARCHERS_OPTION_TEXT}",
                args => IsSoldierTypeAvailable(args, "archers"),
                _ => OnSoldierTypePurchase("archers"),
                false,
                2);

            // Cavalry option - Mounted Lancers
            starter.AddGameMenuOption(
                RetinuePurchaseMenuId,
                "ct_purchase_cavalry",
                "{CAVALRY_OPTION_TEXT}",
                args => IsSoldierTypeAvailable(args, "cavalry"),
                _ => OnSoldierTypePurchase("cavalry"),
                false,
                3);

            // Horse Archers option - Mounted Bowmen (faction restricted)
            starter.AddGameMenuOption(
                RetinuePurchaseMenuId,
                "ct_purchase_horse_archers",
                "{HORSE_ARCHERS_OPTION_TEXT}",
                args => IsSoldierTypeAvailable(args, "horse_archers"),
                _ => OnSoldierTypePurchase("horse_archers"),
                false,
                4);

            // Back option
            starter.AddGameMenuOption(
                RetinuePurchaseMenuId,
                "ct_purchase_back",
                "{=ct_back_retinue}Back to Retinue",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    return true;
                },
                _ => SwitchToMenuPreserveTime(RetinueMenuId),
                true,
                100);
        }

        /// <summary>
        /// Initializes the purchase menu. Shows gold, soldier options, and naval dismount note for mounted types.
        /// Uses clean formatting with inline gold icons.
        /// </summary>
        private void OnRetinuePurchaseInit(MenuCallbackArgs args)
        {
            _ = args;
            try
            {
                var enlistment = EnlistmentBehavior.Instance;
                var culture = enlistment?.CurrentLord?.Culture;
                var tier = enlistment?.EnlistmentTier ?? 4;
                var playerGold = Hero.MainHero?.Gold ?? 0;

                var sb = new StringBuilder();
                sb.AppendLine();
                sb.AppendLine("— Purchase Soldiers —");
                sb.AppendLine();
                sb.AppendLine("Select the type of soldiers to muster.");
                sb.AppendLine();
                sb.AppendLine($"Your Gold: {playerGold}{{GOLD_ICON}}");
                sb.AppendLine();
                sb.AppendLine("* Mounted troops fight on foot in naval battles.");

                MBTextManager.SetTextVariable("RETINUE_PURCHASE_TEXT", sb.ToString());

                // Set option text variables with costs and gold icons
                SetSoldierTypeOptionText("infantry", culture, tier);
                SetSoldierTypeOptionText("archers", culture, tier);
                SetSoldierTypeOptionText("cavalry", culture, tier);
                SetSoldierTypeOptionText("horse_archers", culture, tier);

                ModLogger.Debug(LogCategory, "Retinue purchase menu initialized");
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, $"Error initializing purchase menu: {ex.Message}");
                MBTextManager.SetTextVariable("RETINUE_PURCHASE_TEXT", "Error loading purchase options.");
            }
        }

        /// <summary>
        /// Sets the menu option text for a soldier type with inline gold icon.
        /// Mounted types marked with * for naval warning.
        /// </summary>
        private void SetSoldierTypeOptionText(string typeId, CultureObject culture, int playerTier)
        {
            var typeName = GetSoldierTypeName(typeId, culture);
            var cost = CalculateRecruitmentCost(typeId, culture, playerTier);
            var variableName = typeId.ToUpperInvariant() + "_OPTION_TEXT";

            // Use inline gold icon for cost display
            var optionText = $"{typeName} ({cost}{{GOLD_ICON}})";

            if (IsMountedType(typeId))
            {
                optionText += " *";
            }

            MBTextManager.SetTextVariable(variableName, optionText);
        }

        /// <summary>
        /// Returns true for cavalry/horse_archers which fight dismounted in naval battles.
        /// </summary>
        private static bool IsMountedType(string typeId)
        {
            return typeId is "cavalry" or "horse_archers";
        }

        /// <summary>
        /// Gets a human-readable display name for a formation/soldier type.
        /// Used for tooltip messages explaining formation restrictions.
        /// </summary>
        private static string GetFormationDisplayName(string typeId)
        {
            return typeId?.ToLowerInvariant() switch
            {
                "infantry" => "infantry",
                "archers" or "archer" or "ranged" => "archers",
                "cavalry" => "cavalry",
                "horse_archers" or "horsearcher" or "horse_archer" => "horse archers",
                _ => typeId ?? "soldiers"
            };
        }

        /// <summary>
        /// Validates soldier type availability. Checks player formation match, faction restrictions, affordability, 
        /// and shows naval tooltip for mounted types.
        /// Players can only recruit soldiers matching their own troop type (infantry can only lead infantry, etc.)
        /// </summary>
        private bool IsSoldierTypeAvailable(MenuCallbackArgs args, string typeId)
        {
            var enlistment = EnlistmentBehavior.Instance;
            var culture = enlistment?.CurrentLord?.Culture;

            if (culture == null)
            {
                args.IsEnabled = false;
                return true;
            }

            // Formation match check - players can only recruit soldiers matching their own formation type
            // An infantry soldier leads infantry, a cavalryman leads cavalry, etc.
            var duties = EnlistedDutiesBehavior.Instance;
            var playerFormation = duties?.PlayerFormation?.ToLowerInvariant() ?? "infantry";
            
            // Map player formation to retinue type for comparison
            var playerRetinueType = playerFormation switch
            {
                "archer" or "ranged" => "archers",
                "cavalry" => "cavalry",
                "horsearcher" or "horse_archer" => "horse_archers",
                _ => "infantry"
            };

            if (!typeId.Equals(playerRetinueType, StringComparison.OrdinalIgnoreCase))
            {
                args.IsEnabled = false;
                var formationDisplayName = GetFormationDisplayName(playerRetinueType);
                var requestedDisplayName = GetFormationDisplayName(typeId);
                var tooltip = new TextObject("{=ct_warn_formation_mismatch}As a {PLAYER_TYPE}, you can only command {PLAYER_TYPE} soldiers. You cannot lead {REQUESTED_TYPE}.");
                tooltip.SetTextVariable("PLAYER_TYPE", formationDisplayName);
                tooltip.SetTextVariable("REQUESTED_TYPE", requestedDisplayName);
                args.Tooltip = tooltip;
                return true;
            }

            // Faction restriction check (horse archers not available for some factions)
            if (!RetinueManager.IsSoldierTypeAvailable(typeId, culture))
            {
                args.IsEnabled = false;
                var factionName = culture.Name?.ToString() ?? "This faction";
                var tooltip = new TextObject("{=ct_warn_faction_unavailable}{FACTION_NAME} does not field mounted archers.");
                tooltip.SetTextVariable("FACTION_NAME", factionName);
                args.Tooltip = tooltip;
                return true;
            }

            // Affordability check
            var cost = CalculateRecruitmentCost(typeId, culture, enlistment.EnlistmentTier);
            var playerGold = Hero.MainHero?.Gold ?? 0;

            if (playerGold < cost)
            {
                args.IsEnabled = false;
                var tooltip = new TextObject("{=ct_warn_cannot_afford}You cannot afford this ({COST} denars required).");
                tooltip.SetTextVariable("COST", cost);
                args.Tooltip = tooltip;
                return true;
            }

            // Type change requires dismissing current retinue
            var manager = RetinueManager.Instance;
            if (manager?.State?.HasRetinue == true && manager.State.SelectedTypeId != typeId)
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
            }
            else
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Recruit;
            }

            // Naval dismount info tooltip for mounted types
            if (IsMountedType(typeId))
            {
                var tooltipId = typeId == "cavalry" 
                    ? "{=ct_naval_cavalry_tooltip}Cavalry will dismount and fight as infantry during naval engagements. Their horses cannot be brought aboard ships."
                    : "{=ct_naval_horse_archer_tooltip}Horse archers will dismount and fight as foot archers during naval engagements. Their horses cannot be brought aboard ships.";
                args.Tooltip = new TextObject(tooltipId);
            }

            return true;
        }

        /// <summary>
        /// Handles soldier type purchase selection.
        /// </summary>
        private void OnSoldierTypePurchase(string typeId)
        {
            var manager = RetinueManager.Instance;
            var enlistment = EnlistmentBehavior.Instance;

            if (manager == null || enlistment == null)
            {
                ModLogger.Warn(LogCategory, "Cannot purchase: manager or enlistment null");
                return;
            }

            var culture = enlistment.CurrentLord?.Culture;
            if (culture == null)
            {
                ModLogger.Warn(LogCategory, "Cannot purchase: no culture");
                return;
            }

            // Check if changing type with existing retinue
            if (manager.State?.HasRetinue == true && manager.State.SelectedTypeId != typeId)
            {
                // Show confirmation to dismiss existing retinue
                ShowTypeChangeConfirmation(typeId);
                return;
            }

            // Calculate how many soldiers to purchase
            var tierCapacity = RetinueManager.GetTierCapacity(enlistment.EnlistmentTier);
            var currentSoldiers = manager.State?.TotalSoldiers ?? 0;
            var partySpace = GetAvailablePartySpace();
            var canPurchase = Math.Min(tierCapacity - currentSoldiers, partySpace);

            if (canPurchase <= 0)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    "Cannot purchase soldiers: at capacity or party full.", Colors.Red));
                return;
            }

            // Calculate total cost
            var costPerSoldier = CalculateRecruitmentCost(typeId, culture, enlistment.EnlistmentTier);
            var playerGold = Hero.MainHero?.Gold ?? 0;
            var maxAffordable = playerGold / Math.Max(1, costPerSoldier);
            var purchaseCount = Math.Min(canPurchase, maxAffordable);

            if (purchaseCount <= 0)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    "You cannot afford any soldiers.", Colors.Red));
                return;
            }

            // Show purchase confirmation
            ShowPurchaseConfirmation(typeId, purchaseCount, costPerSoldier * purchaseCount);
        }

        /// <summary>
        /// Shows purchase confirmation dialog.
        /// </summary>
        private void ShowPurchaseConfirmation(string typeId, int count, int totalCost)
        {
            var enlistment = EnlistmentBehavior.Instance;
            var culture = enlistment?.CurrentLord?.Culture;
            var typeName = GetSoldierTypeName(typeId, culture);

            var title = new TextObject("{=ct_purchase_confirm_title}Confirm Purchase");
            var message = new TextObject("{=ct_purchase_confirm_msg}Purchase {COUNT} {TYPE_NAME} for {COST} denars?\n\nDaily upkeep: {UPKEEP} denars");
            message.SetTextVariable("COUNT", count);
            message.SetTextVariable("TYPE_NAME", typeName);
            message.SetTextVariable("COST", totalCost);
            message.SetTextVariable("UPKEEP", count * 2);

            // pauseGameActiveState = false so dialogs don't freeze game time
            var pauseGameActiveState = ShouldPauseDuringCommandTentInquiry();
            InformationManager.ShowInquiry(
                new InquiryData(
                    title.ToString(),
                    message.ToString(),
                    true,
                    true,
                    new TextObject("{=ct_confirm_yes}Purchase").ToString(),
                    new TextObject("{=ct_confirm_no}Cancel").ToString(),
                    () => ExecutePurchase(typeId, count, totalCost),
                    () => SwitchToMenuPreserveTime(RetinuePurchaseMenuId)),
                pauseGameActiveState);
        }

        /// <summary>
        /// Executes the actual purchase of soldiers.
        /// </summary>
        private void ExecutePurchase(string typeId, int count, int totalCost)
        {
            var manager = RetinueManager.Instance;

            if (manager == null)
            {
                ModLogger.Error(LogCategory, "ExecutePurchase: manager null");
                return;
            }

            // Deduct gold using GiveGoldAction (properly affects party treasury and updates UI)
            if (totalCost > 0 && Hero.MainHero != null)
            {
                GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, totalCost);
            }

            // Add soldiers
            if (manager.TryAddSoldiers(count, typeId, out var actuallyAdded, out var message))
            {
                ModLogger.Info(LogCategory, $"Purchased {actuallyAdded} {typeId} soldiers for {totalCost} gold");

                var successMsg = new TextObject("{=ct_purchase_success}{COUNT} soldiers have been mustered to your retinue.");
                successMsg.SetTextVariable("COUNT", actuallyAdded);
                InformationManager.DisplayMessage(new InformationMessage(successMsg.ToString(), Colors.Green));
            }
            else
            {
                // Refund gold if failed
                if (totalCost > 0 && Hero.MainHero != null)
                {
                    GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, totalCost);
                }
                ModLogger.Warn(LogCategory, $"Purchase failed: {message}");
                InformationManager.DisplayMessage(new InformationMessage(message, Colors.Red));
            }

            // Return to retinue menu
            SwitchToMenuPreserveTime(RetinueMenuId);
        }

        /// <summary>
        /// Shows confirmation dialog when changing soldier type with existing retinue.
        /// </summary>
        private void ShowTypeChangeConfirmation(string newTypeId)
        {
            var manager = RetinueManager.Instance;
            var currentCount = manager?.State?.TotalSoldiers ?? 0;

            var title = new TextObject("{=ct_type_change_title}Change Soldier Type");
            var message = new TextObject("{=ct_type_change_msg}You currently have {COUNT} soldiers. Changing type will dismiss them. Continue?");
            message.SetTextVariable("COUNT", currentCount);

            // pauseGameActiveState = false so dialogs don't freeze game time
            var pauseGameActiveState = ShouldPauseDuringCommandTentInquiry();
            InformationManager.ShowInquiry(
                new InquiryData(
                    title.ToString(),
                    message.ToString(),
                    true,
                    true,
                    new TextObject("{=ct_confirm_yes}Dismiss & Continue").ToString(),
                    new TextObject("{=ct_confirm_no}Cancel").ToString(),
                    () =>
                    {
                        // Dismiss current retinue
                        manager?.ClearRetinueTroops("type_change");
                        ModLogger.Info(LogCategory, "Dismissed retinue for type change");

                        var dismissMsg = new TextObject("{=ct_type_change_dismiss}Your current soldiers have been dismissed to make way for new recruits.");
                        InformationManager.DisplayMessage(new InformationMessage(dismissMsg.ToString()));

                        // Now proceed with new type purchase
                        OnSoldierTypePurchase(newTypeId);
                    },
                    () => SwitchToMenuPreserveTime(RetinuePurchaseMenuId)),
                pauseGameActiveState);
        }

        #endregion

        #region Retinue Dismiss Menu

        /// <summary>
        /// Creates the dismiss soldiers confirmation menu.
        /// </summary>
        private void AddRetinueDismissMenu(CampaignGameStarter starter)
        {
            starter.AddWaitGameMenu(
                RetinueDismissMenuId,
                "{RETINUE_DISMISS_TEXT}",
                OnRetinueDismissInit,
                CommandTentWaitCondition,
                CommandTentWaitConsequence,
                CommandTentWaitTick,
                GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption);

            // Confirm dismiss option
            starter.AddGameMenuOption(
                RetinueDismissMenuId,
                "ct_dismiss_confirm",
                "{=ct_dismiss_confirm_btn}Dismiss all soldiers",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Surrender;
                    return true;
                },
                _ => ExecuteDismiss(),
                false,
                1);

            // Cancel option
            starter.AddGameMenuOption(
                RetinueDismissMenuId,
                "ct_dismiss_cancel",
                "{=ct_dismiss_cancel}Keep my soldiers",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    return true;
                },
                _ => SwitchToMenuPreserveTime(RetinueMenuId),
                true,
                100);
        }

        /// <summary>
        /// Initializes the dismiss menu with clean formatting.
        /// </summary>
        private void OnRetinueDismissInit(MenuCallbackArgs args)
        {
            _ = args;
            try
            {
                var manager = RetinueManager.Instance;
                var currentCount = manager?.State?.TotalSoldiers ?? 0;

                var sb = new StringBuilder();
                sb.AppendLine();
                sb.AppendLine("— Dismiss Soldiers —");
                sb.AppendLine();
                sb.AppendLine("Are you certain?");
                sb.AppendLine();
                sb.AppendLine($"Your {currentCount} soldiers will return to the army ranks.");
                sb.AppendLine();
                sb.AppendLine("This action cannot be undone.");

                MBTextManager.SetTextVariable("RETINUE_DISMISS_TEXT", sb.ToString());
                ModLogger.Debug(LogCategory, "Dismiss menu initialized");
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, $"Error initializing dismiss menu: {ex.Message}");
                MBTextManager.SetTextVariable("RETINUE_DISMISS_TEXT", "Error loading dismiss options.");
            }
        }

        /// <summary>
        /// Executes the dismissal of all retinue soldiers.
        /// </summary>
        private void ExecuteDismiss()
        {
            var manager = RetinueManager.Instance;
            var dismissedCount = manager?.State?.TotalSoldiers ?? 0;

            manager?.ClearRetinueTroops("player_dismiss");

            ModLogger.Info(LogCategory, $"Player dismissed {dismissedCount} soldiers");

            var msg = new TextObject("{=ct_dismiss_success}Your soldiers have been dismissed.");
            InformationManager.DisplayMessage(new InformationMessage(msg.ToString()));

            SwitchToMenuPreserveTime(RetinueMenuId);
        }

        #endregion

        #region Requisition Menu

        /// <summary>
        /// Checks if the requisition menu option should be available.
        /// Shows disabled state with tooltip when on cooldown or at capacity.
        /// </summary>
        private static bool IsRequisitionAvailable(MenuCallbackArgs args)
        {
            var manager = RetinueManager.Instance;

            // Must have a retinue type selected
            if (manager?.State?.HasTypeSelected != true)
            {
                args.IsEnabled = false;
                args.Tooltip = new TextObject("{=ct_requisition_no_type}You must select a soldier type first.");
                args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                return true;
            }

            // Check cooldown
            if (!manager.IsRequisitionAvailable())
            {
                args.IsEnabled = false;
                var days = manager.GetRequisitionCooldownDays();
                var tooltip = new TextObject("{=ct_requisition_cooldown}Requisition on cooldown: {DAYS} days remaining.");
                tooltip.SetTextVariable("DAYS", days);
                args.Tooltip = tooltip;
                args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                return true;
            }

            // Check if already at capacity
            var missing = manager.GetMissingSoldierCount();
            if (missing <= 0)
            {
                args.IsEnabled = false;
                args.Tooltip = new TextObject("{=ct_warn_full_retinue}Your retinue is at full strength.");
                args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                return true;
            }

            // Check affordability
            var cost = manager.CalculateRequisitionCost();
            var playerGold = Hero.MainHero?.Gold ?? 0;
            if (playerGold < cost)
            {
                args.IsEnabled = false;
                var tooltip = new TextObject("{=ct_warn_cannot_afford}You cannot afford this ({COST} denars required).");
                tooltip.SetTextVariable("COST", cost);
                args.Tooltip = tooltip;
                args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                return true;
            }

            args.optionLeaveType = GameMenuOption.LeaveType.Trade;
            return true;
        }

        /// <summary>
        /// Creates the Requisition confirmation menu with cost breakdown.
        /// </summary>
        private void AddRetinueRequisitionMenu(CampaignGameStarter starter)
        {
            starter.AddWaitGameMenu(
                RetinueRequisitionMenuId,
                "{REQUISITION_MENU_TEXT}",
                OnRetinueRequisitionInit,
                CommandTentWaitCondition,
                CommandTentWaitConsequence,
                CommandTentWaitTick,
                GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption);

            // Confirm requisition option
            starter.AddGameMenuOption(
                RetinueRequisitionMenuId,
                "ct_requisition_confirm",
                "{REQUISITION_CONFIRM_TEXT}",
                args =>
                {
                    var manager = RetinueManager.Instance;
                    if (manager == null || !manager.CanRequisition(out _))
                    {
                        args.IsEnabled = false;
                        return true;
                    }

                    args.optionLeaveType = GameMenuOption.LeaveType.Trade;
                    return true;
                },
                _ => ExecuteRequisition(),
                false,
                1);

            // Cancel option
            starter.AddGameMenuOption(
                RetinueRequisitionMenuId,
                "ct_requisition_cancel",
                "{=ct_requisition_cancel}Return without requisition",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    return true;
                },
                _ => SwitchToMenuPreserveTime(RetinueMenuId),
                true,
                100);
        }

        /// <summary>
        /// Initializes the requisition menu with cost breakdown.
        /// Uses clean formatting with inline gold icons.
        /// </summary>
        private static void OnRetinueRequisitionInit(MenuCallbackArgs args)
        {
            _ = args;
            try
            {
                var manager = RetinueManager.Instance;
                var missing = manager?.GetMissingSoldierCount() ?? 0;
                var cost = manager?.CalculateRequisitionCost() ?? 0;
                var perSoldier = missing > 0 ? cost / missing : 0;
                var playerGold = Hero.MainHero?.Gold ?? 0;
                var cooldownDays = manager?.GetRequisitionCooldownDays() ?? 0;

                var sb = new StringBuilder();
                sb.AppendLine();
                sb.AppendLine("— Requisition Men —");
                sb.AppendLine();
                sb.AppendLine("A word to the right quartermaster, a few coins changing hands, and fresh soldiers report for duty.");
                sb.AppendLine();
                sb.AppendLine($"Missing Soldiers: {missing}");
                sb.AppendLine($"Cost per Soldier: {perSoldier}{{GOLD_ICON}}");
                sb.AppendLine($"Total Cost: {cost}{{GOLD_ICON}}");
                sb.AppendLine();
                sb.AppendLine($"Your Gold: {playerGold}{{GOLD_ICON}}");
                sb.AppendLine();

                var cooldownLine = cooldownDays > 0
                    ? $"Cooldown: {cooldownDays} days remaining"
                    : "Requisition available now";
                sb.AppendLine(cooldownLine);

                sb.AppendLine();
                sb.AppendLine("After requisition: 14 day cooldown");

                MBTextManager.SetTextVariable("REQUISITION_MENU_TEXT", sb.ToString());

                // Set confirm button text with gold icon
                var confirmText = $"Requisition {missing} soldiers ({cost}{{GOLD_ICON}})";
                MBTextManager.SetTextVariable("REQUISITION_CONFIRM_TEXT", confirmText);

                ModLogger.Debug(LogCategory, "Requisition menu initialized");
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, $"Error initializing requisition menu: {ex.Message}");
                MBTextManager.SetTextVariable("REQUISITION_MENU_TEXT", "Error loading requisition details.");
                MBTextManager.SetTextVariable("REQUISITION_CONFIRM_TEXT", "Requisition");
            }
        }

        /// <summary>
        /// Executes the instant requisition of soldiers.
        /// </summary>
        private void ExecuteRequisition()
        {
            var manager = RetinueManager.Instance;
            if (manager == null)
            {
                ModLogger.Error(LogCategory, "ExecuteRequisition: manager null");
                SwitchToMenuPreserveTime(RetinueMenuId);
                return;
            }

            if (manager.TryRequisition(out var message))
            {
                var successMsg = new TextObject("{=ct_requisition_success}{MESSAGE}");
                successMsg.SetTextVariable("MESSAGE", message);
                InformationManager.DisplayMessage(new InformationMessage(successMsg.ToString(), Colors.Green));

                ModLogger.Info(LogCategory, $"Requisition complete: {message}");
            }
            else
            {
                InformationManager.DisplayMessage(new InformationMessage(message, Colors.Red));
                ModLogger.Warn(LogCategory, $"Requisition failed: {message}");
            }

            SwitchToMenuPreserveTime(RetinueMenuId);
        }

        #endregion

        #region Companion Assignments Menu

        /// <summary>
        /// Checks if companion assignments option is available (Tier 4+ and has companions).
        /// Uses Conversation icon for companion management.
        /// </summary>
        private static bool IsCompanionAssignmentsAvailable(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Conversation;
            var enlistment = EnlistmentBehavior.Instance;
            if ((enlistment?.EnlistmentTier ?? 1) < 4)
            {
                args.IsEnabled = false;
                args.Tooltip = new TextObject("{=ct_companions_tier_locked}You must reach Tier 4 to manage companion assignments.");
                return true;
            }
            var manager = CompanionAssignmentManager.Instance;
            var companions = manager?.GetAssignableCompanions() ?? new List<Hero>();
            if (companions.Count == 0)
            {
                args.IsEnabled = false;
                args.Tooltip = new TextObject("{=ct_companions_none}No companions in your command.");
                return true;
            }
            args.Tooltip = new TextObject("{=ct_companions_tooltip}Assign companions to roles in your retinue.");
            return true;
        }

        /// <summary>
        /// Creates the Companion Assignments submenu showing each companion with toggle options.
        /// </summary>
        private void AddCompanionAssignmentsMenu(CampaignGameStarter starter)
        {
            starter.AddWaitGameMenu(
                CompanionAssignmentsMenuId,
                "{COMPANION_ASSIGNMENTS_TEXT}",
                OnCompanionAssignmentsInit,
                CommandTentWaitCondition,
                CommandTentWaitConsequence,
                CommandTentWaitTick,
                GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption);

            // Dynamic companion options are added via menu init text since GameMenu doesn't support 
            // truly dynamic options. Instead we show the list in the description and use toggle buttons.
            // Add 8 companion slots (should cover most parties)
            for (var i = 0; i < 8; i++)
            {
                var index = i;
                starter.AddGameMenuOption(
                    CompanionAssignmentsMenuId,
                    $"ct_companion_{i}",
                    $"{{COMPANION_{i}_TEXT}}",
                    args => IsCompanionSlotVisible(args, index),
                    _ => OnCompanionToggle(index),
                    false,
                    i + 1);
            }

            // Back option
            starter.AddGameMenuOption(
                CompanionAssignmentsMenuId,
                "ct_companions_back",
                "{=ct_back_tent}Back to Camp",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    return true;
                },
                _ => SwitchToMenuPreserveTime(CommandTentMenuId),
                true,
                100);
        }

        // Cache companions for the current menu session to maintain consistent indices
        private List<Hero> _cachedCompanions;

        /// <summary>
        /// Initializes the companion assignments menu with current companion list.
        /// Uses clean formatting with status indicators.
        /// </summary>
        private void OnCompanionAssignmentsInit(MenuCallbackArgs args)
        {
            _ = args;
            try
            {
                var manager = CompanionAssignmentManager.Instance;
                _cachedCompanions = manager?.GetAssignableCompanions() ?? new List<Hero>();

                var sb = new StringBuilder();
                sb.AppendLine();
                sb.AppendLine("— Companion Assignments —");
                sb.AppendLine();
                sb.AppendLine("Companions set to 'Stay Back' will not spawn in battle.");
                sb.AppendLine("They remain safe, immune to death, wounds, or capture.");
                sb.AppendLine();

                if (_cachedCompanions.Count == 0 || manager == null)
                {
                    sb.AppendLine("No companions in your command.");
                }
                else
                {
                    var fightCount = manager.GetFightingCompanionCount();
                    var stayBackCount = manager.GetStayBackCompanionCount();
                    sb.AppendLine($"Fighting: {fightCount}  |  Staying Back: {stayBackCount}");
                }

                MBTextManager.SetTextVariable("COMPANION_ASSIGNMENTS_TEXT", sb.ToString());

                // Set text variables for each companion slot with clear status indicators
                for (var i = 0; i < 8; i++)
                {
                    if (i < _cachedCompanions.Count && manager != null)
                    {
                        var companion = _cachedCompanions[i];
                        var shouldFight = manager.ShouldCompanionFight(companion);
                        var statusText = shouldFight ? "[Fight]" : "[Stay Back]";
                        MBTextManager.SetTextVariable($"COMPANION_{i}_TEXT", $"{companion.Name} {statusText}");
                    }
                    else
                    {
                        MBTextManager.SetTextVariable($"COMPANION_{i}_TEXT", string.Empty);
                    }
                }

                ModLogger.Debug(LogCategory, $"Companion assignments menu initialized with {_cachedCompanions.Count} companions");
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, $"Error initializing companion assignments: {ex.Message}");
                MBTextManager.SetTextVariable("COMPANION_ASSIGNMENTS_TEXT", "Error loading companion data.");
            }
        }

        /// <summary>
        /// Checks if a companion slot should be visible based on cached companion list.
        /// </summary>
        private bool IsCompanionSlotVisible(MenuCallbackArgs args, int index)
        {
            if (_cachedCompanions == null || index >= _cachedCompanions.Count)
            {
                return false;
            }

            args.optionLeaveType = GameMenuOption.LeaveType.Continue;
            return true;
        }

        /// <summary>
        /// Toggles a companion's battle participation when their menu option is selected.
        /// </summary>
        private void OnCompanionToggle(int index)
        {
            if (_cachedCompanions == null || index >= _cachedCompanions.Count)
            {
                return;
            }

            var companion = _cachedCompanions[index];
            var manager = CompanionAssignmentManager.Instance;
            if (manager == null || companion == null)
            {
                return;
            }

            manager.ToggleCompanionParticipation(companion);
            var newStatus = manager.ShouldCompanionFight(companion) ? "Fight" : "Stay Back";
            
            var message = new TextObject("{=ct_companion_toggled}{COMPANION_NAME} set to: {STATUS}");
            message.SetTextVariable("COMPANION_NAME", companion.Name);
            message.SetTextVariable("STATUS", newStatus);
            InformationManager.DisplayMessage(new InformationMessage(message.ToString()));

            ModLogger.Info(LogCategory, $"Toggled {companion.Name} to {newStatus}");

            // Refresh the menu
            SwitchToMenuPreserveTime(CompanionAssignmentsMenuId);
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Gets the rank name for a given tier.
        /// </summary>
        private static string GetRankName(int tier)
        {
            return tier switch
            {
                1 => "Levy",
                2 => "Footman",
                3 => "Serjeant",
                4 => "Man-at-Arms",
                5 => "Banner Serjeant",
                6 => "Household Guard",
                _ => $"Tier {tier}"
            };
        }

        /// <summary>
        /// Gets the unit name for a tier (lance/squad/retinue).
        /// </summary>
        private static string GetUnitNameForTier(int tier)
        {
            return tier switch
            {
                >= 6 => "Retinue of Twenty",
                5 => "Squad of Ten",
                4 => "Lance of Five",
                _ => "None"
            };
        }

        /// <summary>
        /// Gets the display name for a soldier type, with faction-specific overrides.
        /// </summary>
        private static string GetSoldierTypeName(string typeId, CultureObject culture)
        {
            var cultureId = culture?.StringId?.ToLowerInvariant() ?? "";

            return typeId switch
            {
                "infantry" => "Men-at-Arms",
                "archers" => cultureId switch
                {
                    "vlandia" => "Crossbowmen",
                    "battania" => "Fian Archers",
                    _ => "Bowmen"
                },
                "cavalry" => "Mounted Lancers",
                "horse_archers" => cultureId switch
                {
                    "khuzait" => "Steppe Riders",
                    "aserai" => "Mameluke Cavalry",
                    _ => "Mounted Bowmen"
                },
                _ => typeId
            };
        }

        /// <summary>
        /// Gets available party space for new soldiers.
        /// </summary>
        private static int GetAvailablePartySpace()
        {
            var party = PartyBase.MainParty;
            if (party == null)
            {
                return 0;
            }

            return Math.Max(0, party.PartySizeLimit - party.NumberOfAllMembers);
        }

        /// <summary>
        /// Calculates the recruitment cost for one soldier of the given type.
        /// Uses the native PartyWageModel.GetTroopRecruitmentCost formula.
        /// </summary>
        private static int CalculateRecruitmentCost(string typeId, CultureObject culture, int playerTier)
        {
            // Get a representative troop to calculate cost
            var troops = RetinueManager.GetAvailableTroops(typeId, culture, playerTier);
            if (troops == null || troops.Count == 0)
            {
                // Fallback cost based on type
                return typeId switch
                {
                    "cavalry" or "horse_archers" => 200, // Mounted troops cost more
                    _ => 100
                };
            }

            // Calculate average cost across available troops
            var totalCost = 0;
            var count = 0;

            foreach (var troop in troops)
            {
                var cost = Campaign.Current?.Models?.PartyWageModel?
                    .GetTroopRecruitmentCost(troop, Hero.MainHero)
                    .RoundedResultNumber ?? 100;
                totalCost += cost;
                count++;
            }

            return count > 0 ? totalCost / count : 100;
        }

        #endregion

        #region Phase 8: PayTension Action Menus

        /// <summary>
        /// Creates the Desperate Measures menu - corruption path options.
        /// Options unlock at different PayTension thresholds.
        /// </summary>
        private void AddDesperateMeasuresMenu(CampaignGameStarter starter)
        {
            starter.AddWaitGameMenu(
                DesperateMeasuresMenuId,
                "{=dm_menu_intro}Desperate Measures\n\nWhen legitimate channels fail, some turn to darker paths. " +
                "Choose carefully - your reputation and honor are at stake.\n\n{DESPERATE_STATUS}",
                OnDesperateMeasuresInit,
                CommandTentWaitCondition,
                CommandTentWaitConsequence,
                CommandTentWaitTick,
                GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption);

            // Option 1: Bribe Paymaster's Clerk (40+ tension)
            starter.AddGameMenuOption(
                DesperateMeasuresMenuId,
                "dm_bribe_clerk",
                "{=dm_bribe_clerk}Bribe the Paymaster's Clerk (50{GOLD_ICON})",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;
                    var enlistment = EnlistmentBehavior.Instance;
                    var tension = enlistment?.PayTension ?? 0;
                    
                    if (tension < 40) return false;
                    
                    if (Hero.MainHero.Gold < 50)
                    {
                        args.IsEnabled = false;
                        args.Tooltip = new TextObject("{=dm_no_gold}You don't have enough gold.");
                    }
                    else
                    {
                        args.Tooltip = new TextObject("{=dm_bribe_tooltip}Slip the clerk some coin to 'adjust' the pay records. Gain 20 gold but risk getting caught.");
                    }
                    return true;
                },
                _ => OnBribeClerk(),
                false, 1);

            // Option 2: Skim Supplies (40+ tension, Quartermaster or Armorer duty)
            starter.AddGameMenuOption(
                DesperateMeasuresMenuId,
                "dm_skim_supplies",
                "{=dm_skim_supplies}Skim Supplies from the Baggage Train",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;
                    var enlistment = EnlistmentBehavior.Instance;
                    var tension = enlistment?.PayTension ?? 0;
                    var duty = enlistment?.SelectedDuty ?? "";
                    
                    if (tension < 40) return false;
                    
                    var isSupplyDuty = duty == "quartermaster" || duty == "armorer";
                    if (!isSupplyDuty)
                    {
                        args.IsEnabled = false;
                        args.Tooltip = new TextObject("{=dm_wrong_duty}Only available for Quartermaster or Armorer duties.");
                        return true;
                    }
                    
                    args.Tooltip = new TextObject("{=dm_skim_tooltip}Divert some supplies for personal gain. Gain 30 gold worth of goods.");
                    return true;
                },
                _ => OnSkimSupplies(),
                false, 2);

            // Option 3: Find Black Market (50+ tension)
            starter.AddGameMenuOption(
                DesperateMeasuresMenuId,
                "dm_black_market",
                "{=dm_black_market}Find the Black Market",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Trade;
                    var enlistment = EnlistmentBehavior.Instance;
                    var tension = enlistment?.PayTension ?? 0;
                    
                    if (tension < 50) return false;
                    
                    args.Tooltip = new TextObject("{=dm_black_market_tooltip}Seek out illicit traders. Buy or sell contraband for profit.");
                    return true;
                },
                _ => OnFindBlackMarket(),
                false, 3);

            // Option 4: Sell Your Gear (60+ tension)
            starter.AddGameMenuOption(
                DesperateMeasuresMenuId,
                "dm_sell_gear",
                "{=dm_sell_gear}Sell Your Issued Equipment",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Trade;
                    var enlistment = EnlistmentBehavior.Instance;
                    var tension = enlistment?.PayTension ?? 0;
                    
                    if (tension < 60) return false;
                    
                    args.Tooltip = new TextObject("{=dm_sell_gear_tooltip}Sell equipment you were issued. Risky - you'll need to replace it or face punishment.");
                    return true;
                },
                _ => OnSellIssuedGear(),
                false, 4);

            // Option 5: Listen to Desertion Talk (70+ tension)
            starter.AddGameMenuOption(
                DesperateMeasuresMenuId,
                "dm_desertion_talk",
                "{=dm_desertion_talk}Listen to Desertion Talk",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Escape;
                    var enlistment = EnlistmentBehavior.Instance;
                    var tension = enlistment?.PayTension ?? 0;
                    
                    if (tension < 70) return false;
                    
                    args.Tooltip = new TextObject("{=dm_desertion_tooltip}Some soldiers are planning to slip away. Hear what they have to say.");
                    return true;
                },
                _ => OnDesertionTalk(),
                false, 5);

            // Back option
            starter.AddGameMenuOption(
                DesperateMeasuresMenuId,
                "dm_back",
                "{=dm_back}Return to Camp",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    return true;
                },
                _ => SwitchToMenuPreserveTime(CommandTentMenuId),
                true, 99);
        }

        /// <summary>
        /// Initializes the Desperate Measures menu.
        /// </summary>
        private void OnDesperateMeasuresInit(MenuCallbackArgs args)
        {
            var enlistment = EnlistmentBehavior.Instance;
            var tension = enlistment?.PayTension ?? 0;
            
            var status = $"Current PayTension: {tension}/100\n";
            status += tension >= 70 ? "The situation is critical. Desperate times call for desperate measures."
                    : tension >= 50 ? "The men are angry. Options are limited."
                    : "Things are getting difficult. Some are already bending the rules.";
            
            MBTextManager.SetTextVariable("DESPERATE_STATUS", status);
        }

        /// <summary>
        /// Creates the Help the Lord menu - loyalty path missions.
        /// Missions help reduce PayTension through legitimate means.
        /// </summary>
        private void AddHelpTheLordMenu(CampaignGameStarter starter)
        {
            starter.AddWaitGameMenu(
                HelpTheLordMenuId,
                "{=hlm_menu_intro}Help the Lord\n\nThe lord's coffers are running low, but loyal soldiers can help. " +
                "Volunteer for missions that bring coin to the treasury.\n\n{HELP_LORD_STATUS}",
                OnHelpTheLordInit,
                CommandTentWaitCondition,
                CommandTentWaitConsequence,
                CommandTentWaitTick,
                GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption);

            // Option 1: Collect Debts (40+ tension)
            starter.AddGameMenuOption(
                HelpTheLordMenuId,
                "hlm_collect_debts",
                "{=hlm_collect_debts}Volunteer to Collect Debts",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Mission;
                    var enlistment = EnlistmentBehavior.Instance;
                    var tension = enlistment?.PayTension ?? 0;
                    
                    if (tension < 40) return false;
                    
                    args.Tooltip = new TextObject("{=hlm_debts_tooltip}Visit merchants who owe the lord money. Persuade them to pay. (-10 PayTension on success)");
                    return true;
                },
                _ => OnCollectDebts(),
                false, 1);

            // Option 2: Escort Merchant (50+ tension)
            starter.AddGameMenuOption(
                HelpTheLordMenuId,
                "hlm_escort_merchant",
                "{=hlm_escort_merchant}Escort a Merchant Caravan",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Mission;
                    var enlistment = EnlistmentBehavior.Instance;
                    var tension = enlistment?.PayTension ?? 0;
                    
                    if (tension < 50) return false;
                    
                    args.Tooltip = new TextObject("{=hlm_escort_tooltip}Guard a friendly merchant through dangerous territory. The lord takes a cut. (-15 PayTension)");
                    return true;
                },
                _ => OnEscortMerchant(),
                false, 2);

            // Option 3: Negotiate Loan (60+ tension, requires Trade/Charm skill)
            starter.AddGameMenuOption(
                HelpTheLordMenuId,
                "hlm_negotiate_loan",
                "{=hlm_negotiate_loan}Negotiate a Loan for the Lord",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Conversation;
                    var enlistment = EnlistmentBehavior.Instance;
                    var tension = enlistment?.PayTension ?? 0;
                    
                    if (tension < 60) return false;
                    
                    var tradeSkill = Hero.MainHero?.GetSkillValue(DefaultSkills.Trade) ?? 0;
                    if (tradeSkill < 50)
                    {
                        args.IsEnabled = false;
                        args.Tooltip = new TextObject("{=hlm_loan_no_skill}Requires Trade skill 50+. Your skill: {SKILL}").SetTextVariable("SKILL", tradeSkill);
                        return true;
                    }
                    
                    args.Tooltip = new TextObject("{=hlm_loan_tooltip}Use your trade connections to secure a loan. (-20 PayTension, requires Trade 50+)");
                    return true;
                },
                _ => OnNegotiateLoan(),
                false, 3);

            // Option 4: Volunteer for Raid (70+ tension, at war only)
            starter.AddGameMenuOption(
                HelpTheLordMenuId,
                "hlm_volunteer_raid",
                "{=hlm_volunteer_raid}Volunteer for a Raid",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;
                    var enlistment = EnlistmentBehavior.Instance;
                    var tension = enlistment?.PayTension ?? 0;
                    
                    if (tension < 70) return false;
                    
                    // Check if lord is at war with anyone - simplified check
                    var lordKingdom = enlistment?.CurrentLord?.MapFaction;
                    var atWar = lordKingdom != null && Campaign.Current?.Factions?
                        .Any(f => f != lordKingdom && lordKingdom.IsAtWarWith(f)) == true;
                    
                    if (!atWar)
                    {
                        args.IsEnabled = false;
                        args.Tooltip = new TextObject("{=hlm_no_war}Only available when the lord is at war.");
                        return true;
                    }
                    
                    args.Tooltip = new TextObject("{=hlm_raid_tooltip}Lead a raid on enemy territory. Dangerous but lucrative. (-25 PayTension, risk of injury)");
                    return true;
                },
                _ => OnVolunteerRaid(),
                false, 4);

            // Back option
            starter.AddGameMenuOption(
                HelpTheLordMenuId,
                "hlm_back",
                "{=hlm_back}Return to Camp",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    return true;
                },
                _ => SwitchToMenuPreserveTime(CommandTentMenuId),
                true, 99);
        }

        /// <summary>
        /// Initializes the Help the Lord menu.
        /// </summary>
        private void OnHelpTheLordInit(MenuCallbackArgs args)
        {
            var enlistment = EnlistmentBehavior.Instance;
            var tension = enlistment?.PayTension ?? 0;
            var lordName = enlistment?.CurrentLord?.Name?.ToString() ?? "the lord";
            
            var status = $"Current PayTension: {tension}/100\n";
            status += $"Lord {lordName} needs your help to restore the treasury.";
            
            MBTextManager.SetTextVariable("HELP_LORD_STATUS", status);
        }

        // ========================================
        // DESPERATE MEASURES CONSEQUENCES
        // ========================================

        private void OnBribeClerk()
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null) return;

            // Pay the bribe
            Hero.MainHero.ChangeHeroGold(-50);

            // Roll for success/caught
            var successChance = 70; // 70% chance of success
            var roll = MBRandom.RandomInt(100);

            if (roll < successChance)
            {
                // Success - gain more than you paid
                Hero.MainHero.ChangeHeroGold(70);
                InformationManager.DisplayMessage(new InformationMessage(
                    new TextObject("{=dm_bribe_success}The clerk adjusts the records in your favor. You gain 20 gold net.").ToString(),
                    Colors.Green));
                ModLogger.Info(LogCategory, "Bribe clerk succeeded");
            }
            else
            {
                // Caught - lose the bribe and relationship
                enlistment.ModifyQuartermasterRelationship(-10);
                InformationManager.DisplayMessage(new InformationMessage(
                    new TextObject("{=dm_bribe_caught}The clerk takes your money... then reports you. Your reputation suffers.").ToString(),
                    Colors.Red));
                ModLogger.Info(LogCategory, "Bribe clerk failed - caught");
            }

            SwitchToMenuPreserveTime(DesperateMeasuresMenuId);
        }

        private void OnSkimSupplies()
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null) return;

            // Gain supplies (as gold equivalent)
            Hero.MainHero.ChangeHeroGold(30);
            
            InformationManager.DisplayMessage(new InformationMessage(
                new TextObject("{=dm_skim_success}You quietly divert some supplies for yourself. +30 gold worth of goods.").ToString(),
                Colors.Yellow));

            // Small reputation cost if quartermaster relationship is low
            if (enlistment.QuartermasterRelationship < 40)
            {
                enlistment.ModifyQuartermasterRelationship(-5);
                InformationManager.DisplayMessage(new InformationMessage(
                    new TextObject("{=dm_skim_suspicious}The quartermaster seems suspicious...").ToString(),
                    Colors.Gray));
            }

            ModLogger.Info(LogCategory, "Skimmed supplies");
            SwitchToMenuPreserveTime(DesperateMeasuresMenuId);
        }

        private void OnFindBlackMarket()
        {
            InformationManager.DisplayMessage(new InformationMessage(
                new TextObject("{=dm_black_market_contact}You make contact with some... entrepreneurial traders. They'll be around the camp from time to time.").ToString(),
                Colors.Magenta));

            ModLogger.Info(LogCategory, "Found black market");
            SwitchToMenuPreserveTime(DesperateMeasuresMenuId);
        }

        private void OnSellIssuedGear()
        {
            // Gain gold based on current tier
            var enlistment = EnlistmentBehavior.Instance;
            var tier = enlistment?.EnlistmentTier ?? 1;
            var goldGain = tier * 25; // 25-225 gold based on tier

            Hero.MainHero.ChangeHeroGold(goldGain);

            InformationManager.DisplayMessage(new InformationMessage(
                new TextObject("{=dm_sell_gear_success}You sell some of your issued equipment for {GOLD} gold. You'll need to replace it...")
                    .SetTextVariable("GOLD", goldGain).ToString(),
                Colors.Yellow));

            ModLogger.Info(LogCategory, $"Sold issued gear for {goldGain}g");
            SwitchToMenuPreserveTime(DesperateMeasuresMenuId);
        }

        private void OnDesertionTalk()
        {
            var enlistment = EnlistmentBehavior.Instance;
            var freeDesertion = enlistment?.IsFreeDesertionAvailable == true;

            if (freeDesertion)
            {
                InformationManager.ShowInquiry(new InquiryData(
                    new TextObject("{=dm_desertion_title}Desertion Talk").ToString(),
                    new TextObject("{=dm_desertion_body}Several soldiers are planning to slip away tonight. They invite you to join them.\n\n\"The lord's broken his word. We've earned our freedom. No one would blame you for coming with us.\"\n\nWill you desert with them?").ToString(),
                    true, true,
                    new TextObject("{=dm_desertion_accept}Desert Now").ToString(),
                    new TextObject("{=dm_desertion_decline}Stay").ToString(),
                    () => enlistment?.ProcessFreeDesertion(),
                    null));
            }
            else
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    new TextObject("{=dm_desertion_not_ready}You listen to the deserters' plans, but decide the risk isn't worth it... yet.").ToString(),
                    Colors.Gray));
            }

            ModLogger.Info(LogCategory, $"Desertion talk (freeDesertion={freeDesertion})");
        }

        // ========================================
        // HELP THE LORD CONSEQUENCES
        // ========================================

        private void OnCollectDebts()
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null) return;

            // Simple skill check based on Charm
            var charm = Hero.MainHero?.GetSkillValue(DefaultSkills.Charm) ?? 0;
            var successChance = 50 + (charm / 5); // 50-90% based on charm
            var roll = MBRandom.RandomInt(100);

            if (roll < successChance)
            {
                // Reduce PayTension
                ReducePayTension(10);
                InformationManager.DisplayMessage(new InformationMessage(
                    new TextObject("{=hlm_debts_success}You successfully collect the debts. The lord is pleased. (-10 PayTension)").ToString(),
                    Colors.Green));
            }
            else
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    new TextObject("{=hlm_debts_fail}The merchants refuse to pay. Perhaps they need more... persuasion.").ToString(),
                    Colors.Yellow));
            }

            ModLogger.Info(LogCategory, "Collect debts mission");
            SwitchToMenuPreserveTime(HelpTheLordMenuId);
        }

        private void OnEscortMerchant()
        {
            // Always succeeds but costs fatigue
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null) return;

            // Cost fatigue
            enlistment.TryConsumeFatigue(4, "escort_mission");

            // Reduce PayTension
            ReducePayTension(15);

            InformationManager.DisplayMessage(new InformationMessage(
                new TextObject("{=hlm_escort_success}You escort the merchant safely. The lord's coffers grow. (-15 PayTension, -4 fatigue)").ToString(),
                Colors.Green));

            ModLogger.Info(LogCategory, "Escort merchant mission");
            SwitchToMenuPreserveTime(HelpTheLordMenuId);
        }

        private void OnNegotiateLoan()
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null) return;

            // Skill check
            var trade = Hero.MainHero?.GetSkillValue(DefaultSkills.Trade) ?? 0;
            var successChance = trade; // Direct skill percentage
            var roll = MBRandom.RandomInt(100);

            if (roll < successChance)
            {
                ReducePayTension(20);
                InformationManager.DisplayMessage(new InformationMessage(
                    new TextObject("{=hlm_loan_success}You secure a favorable loan for the lord. The treasury is replenished. (-20 PayTension)").ToString(),
                    Colors.Green));
            }
            else
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    new TextObject("{=hlm_loan_fail}The bankers aren't interested. Perhaps another approach...").ToString(),
                    Colors.Yellow));
            }

            ModLogger.Info(LogCategory, "Negotiate loan mission");
            SwitchToMenuPreserveTime(HelpTheLordMenuId);
        }

        private void OnVolunteerRaid()
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null) return;

            // Risky mission - chance of injury
            var combatSkill = (Hero.MainHero?.GetSkillValue(DefaultSkills.OneHanded) ?? 0) +
                             (Hero.MainHero?.GetSkillValue(DefaultSkills.TwoHanded) ?? 0);
            var injuryChance = Math.Max(10, 50 - (combatSkill / 10)); // 10-50% based on skill
            var roll = MBRandom.RandomInt(100);

            if (roll < injuryChance)
            {
                // Injured
                var damage = MBRandom.RandomInt(20, 50);
                Hero.MainHero.HitPoints = Math.Max(1, Hero.MainHero.HitPoints - damage);
                InformationManager.DisplayMessage(new InformationMessage(
                    new TextObject("{=hlm_raid_wounded}The raid succeeds but you're wounded! (-{DAMAGE} HP, -25 PayTension)")
                        .SetTextVariable("DAMAGE", damage).ToString(),
                    Colors.Yellow));
            }
            else
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    new TextObject("{=hlm_raid_success}The raid is a complete success! Valuable loot is captured. (-25 PayTension)").ToString(),
                    Colors.Green));
            }

            // Always reduces tension significantly
            ReducePayTension(25);

            // Gain some gold personally
            Hero.MainHero.ChangeHeroGold(50);

            ModLogger.Info(LogCategory, "Volunteer raid mission");
            SwitchToMenuPreserveTime(HelpTheLordMenuId);
        }

        /// <summary>
        /// Reduces the current PayTension by the specified amount.
        /// </summary>
        private static void ReducePayTension(int amount)
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null) return;

            enlistment.ReducePayTension(amount);
        }

        #endregion
    }
}

