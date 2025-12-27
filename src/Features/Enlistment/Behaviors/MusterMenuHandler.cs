using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Enlisted.Features.Context;
using Enlisted.Features.Equipment.Behaviors;
using Enlisted.Features.Escalation;
using Enlisted.Features.Identity;
using Enlisted.Features.Interface.Behaviors;
using Enlisted.Features.Logistics;
using Enlisted.Features.Retinue.Core;
using Enlisted.Features.Retinue.Data;
using Enlisted.Mod.Core.Config;
using Enlisted.Mod.Core.Logging;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Enlisted.Features.Enlistment.Behaviors
{
    /// <summary>
    /// Handles the multi-stage muster menu sequence that guides players through pay day,
    /// inspections, period summaries, and transitions to post-muster activities.
    /// Replaces the simple inquiry popup with a comprehensive GameMenu flow.
    /// </summary>
    public sealed class MusterMenuHandler : CampaignBehaviorBase
    {
        private const string LogCategory = "Muster";

        // Menu IDs for the 8 muster stages
        private const string MusterIntroMenuId = "enlisted_muster_intro";
        private const string MusterPayMenuId = "enlisted_muster_pay";
        private const string MusterBaggageMenuId = "enlisted_muster_baggage";
        private const string MusterInspectionMenuId = "enlisted_muster_inspection";
        private const string MusterRecruitMenuId = "enlisted_muster_recruit";
        private const string MusterPromotionRecapMenuId = "enlisted_muster_promotion_recap";
        private const string MusterRetinueMenuId = "enlisted_muster_retinue";
        private const string MusterCompleteMenuId = "enlisted_muster_complete";

        /// <summary>
        /// Singleton instance for accessing the muster menu handler.
        /// </summary>
        public static MusterMenuHandler Instance { get; private set; }

        /// <summary>
        /// Current muster session state. Non-null only while a muster sequence is active.
        /// </summary>
        private MusterSessionState _currentMuster;

        /// <summary>
        /// Flag set when muster is pending but player is in combat.
        /// Muster will trigger when combat ends.
        /// </summary>
        private bool _musterPendingAfterCombat;

        /// <summary>
        /// Flag set when muster was attempted but player was in another menu.
        /// Muster will trigger when player exits to map.
        /// </summary>
        private bool _musterPendingAfterMenu;

        /// <summary>
        /// Tracks the last muster trigger attempt time to prevent rapid retries.
        /// </summary>
        private CampaignTime _lastMusterTriggerAttempt = CampaignTime.Zero;

        /// <summary>
        /// Tracks muster session data including outcomes, progression, and post-muster flags.
        /// Created fresh at the start of each muster and cleared when muster completes.
        /// </summary>
        public class MusterSessionState
        {
            // Flow tracking (essential for save/load)
            /// <summary>Current menu stage ID for resume.</summary>
            public string CurrentStage { get; set; }

            /// <summary>Day muster started.</summary>
            public int MusterDay { get; set; }

            /// <summary>Previous muster for period calculation.</summary>
            public int LastMusterDay { get; set; }

            // Strategic context (for intro flavor text)
            /// <summary>Strategic context from ArmyContextAnalyzer.</summary>
            public string StrategicContext { get; set; }

            // Orders summary
            /// <summary>Count of orders completed this period.</summary>
            public int OrdersCompleted { get; set; }

            /// <summary>Count of orders failed this period.</summary>
            public int OrdersFailed { get; set; }

            /// <summary>Brief descriptions of order outcomes.</summary>
            public List<string> OrderOutcomes { get; set; } = new List<string>();

            // Fatigue (reset at muster start)
            /// <summary>Fatigue level before muster for "restored" message.</summary>
            public int FatigueBeforeMuster { get; set; }

            // Pay stage outcomes
            /// <summary>Amount of pay received.</summary>
            public int PayReceived { get; set; }

            /// <summary>Pay outcome type: "full", "partial", "iou", "corruption".</summary>
            public string PayOutcome { get; set; }

            /// <summary>Ration outcome: "issued", "none", "officer_exempt".</summary>
            public string RationOutcome { get; set; }

            // Event stage outcomes
            /// <summary>Baggage check outcome: "passed", "confiscated", "bribed", "skipped".</summary>
            public string BaggageOutcome { get; set; }

            /// <summary>Inspection outcome: "perfect", "basic", "failed", "skipped".</summary>
            public string InspectionOutcome { get; set; }

            /// <summary>Recruit outcome: "mentored", "ignored", "hazed", "skipped".</summary>
            public string RecruitOutcome { get; set; }

            // Promotion tracking (promotion occurs via PromotionBehavior, recap shows at muster)
            /// <summary>True if tier changed since last muster.</summary>
            public bool PromotionOccurredThisPeriod { get; set; }

            /// <summary>Tier at last muster.</summary>
            public int PreviousTier { get; set; }

            /// <summary>Tier at this muster.</summary>
            public int CurrentTier { get; set; }

            /// <summary>Campaign day when promotion occurred.</summary>
            public int PromotionDay { get; set; }

            // Contraband (needed for baggage stage display)
            /// <summary>Contraband found during baggage check.</summary>
            public bool ContrabandFound { get; set; }

            /// <summary>Current quartermaster reputation.</summary>
            public int QMRep { get; set; }

            // Retinue (T7+ only)
            /// <summary>Current retinue soldier count.</summary>
            public int RetinueStrength { get; set; }

            /// <summary>Maximum retinue capacity.</summary>
            public int RetinueCapacity { get; set; }

            /// <summary>Retinue casualties this period.</summary>
            public int RetinueCasualties { get; set; }

            /// <summary>Names of fallen soldiers this period.</summary>
            public List<string> FallenRetinueNames { get; set; } = new List<string>();

            // Post-muster flags
            /// <summary>If true, open QM conversation after muster.</summary>
            public bool VisitQMAfter { get; set; }

            /// <summary>If true, open retinue recruitment after muster.</summary>
            public bool RecruitRetinueAfter { get; set; }

            /// <summary>If true, request temporary leave after muster.</summary>
            public bool RequestLeaveAfter { get; set; }

            // Escalation tracking (for edge case handling)
            /// <summary>Whether high scrutiny warning should display.</summary>
            public bool HighScrutinyWarning { get; set; }

            /// <summary>Whether discharge threshold was reached during muster.</summary>
            public bool DischargeThresholdReached { get; set; }

            /// <summary>Events queued to fire after muster completes.</summary>
            public List<string> PendingEscalationEvents { get; set; } = new List<string>();

            // Error tracking
            /// <summary>Errors encountered during muster that couldn't halt the flow.</summary>
            public List<string> EncounteredErrors { get; set; } = new List<string>();

            /// <summary>Whether any effects failed to apply.</summary>
            public bool EffectsPartiallyFailed { get; set; }
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            Instance = this;
            ModLogger.Info(LogCategory, "MusterMenuHandler registered");
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Persist muster-related flags for edge case handling
            dataStore.SyncData("_musterPendingAfterCombat", ref _musterPendingAfterCombat);
            dataStore.SyncData("_musterPendingAfterMenu", ref _musterPendingAfterMenu);

            // Muster session state persistence for mid-muster save/load
            var hasMusterInProgress = _currentMuster != null;
            dataStore.SyncData("_hasMusterInProgress", ref hasMusterInProgress);

            if (dataStore.IsSaving)
            {
                // Save current muster state if in progress
                if (_currentMuster != null)
                {
                    var currentStage = _currentMuster.CurrentStage ?? "";
                    var musterDay = _currentMuster.MusterDay;
                    var lastMusterDay = _currentMuster.LastMusterDay;
                    var strategicContext = _currentMuster.StrategicContext ?? "";
                    var payReceived = _currentMuster.PayReceived;
                    var payOutcome = _currentMuster.PayOutcome ?? "";
                    var baggageOutcome = _currentMuster.BaggageOutcome ?? "";
                    var inspectionOutcome = _currentMuster.InspectionOutcome ?? "";
                    var recruitOutcome = _currentMuster.RecruitOutcome ?? "";
                    var promotionOccurred = _currentMuster.PromotionOccurredThisPeriod;
                    var previousTier = _currentMuster.PreviousTier;
                    var currentTier = _currentMuster.CurrentTier;
                    var highScrutiny = _currentMuster.HighScrutinyWarning;
                    var visitQM = _currentMuster.VisitQMAfter;

                    dataStore.SyncData("_muster_currentStage", ref currentStage);
                    dataStore.SyncData("_muster_musterDay", ref musterDay);
                    dataStore.SyncData("_muster_lastMusterDay", ref lastMusterDay);
                    dataStore.SyncData("_muster_strategicContext", ref strategicContext);
                    dataStore.SyncData("_muster_payReceived", ref payReceived);
                    dataStore.SyncData("_muster_payOutcome", ref payOutcome);
                    dataStore.SyncData("_muster_baggageOutcome", ref baggageOutcome);
                    dataStore.SyncData("_muster_inspectionOutcome", ref inspectionOutcome);
                    dataStore.SyncData("_muster_recruitOutcome", ref recruitOutcome);
                    dataStore.SyncData("_muster_promotionOccurred", ref promotionOccurred);
                    dataStore.SyncData("_muster_previousTier", ref previousTier);
                    dataStore.SyncData("_muster_currentTier", ref currentTier);
                    dataStore.SyncData("_muster_highScrutiny", ref highScrutiny);
                    dataStore.SyncData("_muster_visitQM", ref visitQM);

                    ModLogger.Info(LogCategory, $"Saved muster state at stage {currentStage}");
                }
            }
            else
            {
                // Loading - restore muster state if one was in progress
                if (hasMusterInProgress)
                {
                    try
                    {
                        var currentStage = "";
                        var musterDay = 0;
                        var lastMusterDay = 0;
                        var strategicContext = "";
                        var payReceived = 0;
                        var payOutcome = "";
                        var baggageOutcome = "";
                        var inspectionOutcome = "";
                        var recruitOutcome = "";
                        var promotionOccurred = false;
                        var previousTier = 0;
                        var currentTier = 0;
                        var highScrutiny = false;
                        var visitQM = false;

                        dataStore.SyncData("_muster_currentStage", ref currentStage);
                        dataStore.SyncData("_muster_musterDay", ref musterDay);
                        dataStore.SyncData("_muster_lastMusterDay", ref lastMusterDay);
                        dataStore.SyncData("_muster_strategicContext", ref strategicContext);
                        dataStore.SyncData("_muster_payReceived", ref payReceived);
                        dataStore.SyncData("_muster_payOutcome", ref payOutcome);
                        dataStore.SyncData("_muster_baggageOutcome", ref baggageOutcome);
                        dataStore.SyncData("_muster_inspectionOutcome", ref inspectionOutcome);
                        dataStore.SyncData("_muster_recruitOutcome", ref recruitOutcome);
                        dataStore.SyncData("_muster_promotionOccurred", ref promotionOccurred);
                        dataStore.SyncData("_muster_previousTier", ref previousTier);
                        dataStore.SyncData("_muster_currentTier", ref currentTier);
                        dataStore.SyncData("_muster_highScrutiny", ref highScrutiny);
                        dataStore.SyncData("_muster_visitQM", ref visitQM);

                        // Validate restored state
                        if (string.IsNullOrEmpty(currentStage) || !IsValidMusterStage(currentStage))
                        {
                            ModLogger.ErrorCode(LogCategory, "E-MUSTER-003",
                                $"Corrupted muster state on load (stage={currentStage}), aborting muster");
                            _currentMuster = null;
                            EnlistmentBehavior.Instance?.DeferPayMuster();
                        }
                        else
                        {
                            // Restore muster session
                            _currentMuster = new MusterSessionState
                            {
                                CurrentStage = currentStage,
                                MusterDay = musterDay,
                                LastMusterDay = lastMusterDay,
                                StrategicContext = strategicContext,
                                PayReceived = payReceived,
                                PayOutcome = payOutcome,
                                BaggageOutcome = baggageOutcome,
                                InspectionOutcome = inspectionOutcome,
                                RecruitOutcome = recruitOutcome,
                                PromotionOccurredThisPeriod = promotionOccurred,
                                PreviousTier = previousTier,
                                CurrentTier = currentTier,
                                HighScrutinyWarning = highScrutiny,
                                VisitQMAfter = visitQM
                            };

                            ModLogger.Info(LogCategory, $"Restored muster state, will resume at stage {currentStage}");

                            // Queue resume for after load completes
                            _musterPendingAfterMenu = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        ModLogger.ErrorCode(LogCategory, "E-MUSTER-003", "Failed to restore muster state", ex);
                        _currentMuster = null;
                        EnlistmentBehavior.Instance?.DeferPayMuster();
                    }
                }
            }
        }

        /// <summary>
        /// Validates that a stage ID is a known muster menu stage.
        /// </summary>
        private bool IsValidMusterStage(string stageId)
        {
            return stageId == MusterIntroMenuId ||
                   stageId == MusterPayMenuId ||
                   stageId == MusterBaggageMenuId ||
                   stageId == MusterInspectionMenuId ||
                   stageId == MusterRecruitMenuId ||
                   stageId == MusterPromotionRecapMenuId ||
                   stageId == MusterRetinueMenuId ||
                   stageId == MusterCompleteMenuId;
        }

        /// <summary>
        /// Tracks whether muster menu registration succeeded.
        /// If false, BeginMusterSequence will fall back to legacy inquiry.
        /// </summary>
        private bool _menusRegisteredSuccessfully;

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            if (starter == null)
            {
                ModLogger.Error(LogCategory, "OnSessionLaunched called with null CampaignGameStarter");
                _menusRegisteredSuccessfully = false;
                return;
            }

            try
            {
                RegisterMusterMenus(starter);
                _menusRegisteredSuccessfully = true;
                ModLogger.Info(LogCategory, "Muster menus registered successfully");
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode(LogCategory, "E-MUSTER-001", "Failed to register muster menus, will use legacy fallback", ex);
                _menusRegisteredSuccessfully = false;
            }
        }

        /// <summary>
        /// Registers all 8 muster menu stages with the game starter.
        /// Each menu is a wait menu that hides progress bars and shows formatted text.
        /// </summary>
        private void RegisterMusterMenus(CampaignGameStarter starter)
        {
            // 1. Muster Intro Menu
            starter.AddWaitGameMenu(MusterIntroMenuId,
                "{=muster_intro_title}⚔  PAY MUSTER - DAY {MUSTER_DAY}  ⚔\n{MUSTER_INTRO_TEXT}",
                OnMusterIntroInit,
                OnMusterMenuCondition,
                null,
                OnMusterMenuTick,
                GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption);

            // Intro options
            starter.AddGameMenuOption(MusterIntroMenuId, "muster_intro_continue",
                "{=muster_continue}Proceed to Pay Line",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Continue;
                    args.Tooltip = new TextObject("{=muster_continue_tt}Step forward to receive wages.");
                    return true;
                },
                _ => GameMenu.SwitchToMenu(MusterPayMenuId),
                false, 1);

            starter.AddGameMenuOption(MusterIntroMenuId, "muster_intro_qm_after",
                "{=muster_qm_after}Visit Quartermaster After Muster",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Trade;
                    args.Tooltip = new TextObject("{=muster_qm_after_tt}Browse newly refreshed stock after pay.");
                    var supply = CompanySupplyManager.Instance?.TotalSupply ?? 100;
                    return supply >= 15;
                },
                _ =>
                {
                    if (_currentMuster != null)
                    {
                        _currentMuster.VisitQMAfter = true;
                    }
                    GameMenu.SwitchToMenu(MusterPayMenuId);
                },
                false, 2);

            starter.AddGameMenuOption(MusterIntroMenuId, "muster_intro_skip",
                "{=muster_skip}Step Aside (Return Later)",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    args.Tooltip = new TextObject("{=muster_skip_tt}Defer muster. Resumes tomorrow.");
                    return true;
                },
                _ => DeferPayMuster(),
                false, 3);

            // 2. Pay Line Menu
            starter.AddWaitGameMenu(MusterPayMenuId,
                "{=muster_pay_title}💰  PAYMASTER'S LINE  💰\n{MUSTER_PAY_TEXT}",
                OnMusterPayInit,
                OnMusterMenuCondition,
                null,
                OnMusterMenuTick,
                GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption);

            // Pay option 1: Accept Your Pay (standard payment)
            starter.AddGameMenuOption(MusterPayMenuId, "muster_pay_accept",
                "{=muster_pay_accept}Accept Your Pay ({PAY_AMOUNT} denars)",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Continue;
                    args.Tooltip = new TextObject("{=muster_pay_accept_tt}Standard payment. Full wages owed.");
                    return true;
                },
                _ => ResolvePayMusterStandard(),
                false, 1);

            // Pay option 2: Demand a Recount (corruption)
            starter.AddGameMenuOption(MusterPayMenuId, "muster_pay_recount",
                "{=muster_pay_recount}Demand a Recount",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Bribe;
                    args.Tooltip = new TextObject("{=muster_pay_recount_tt}Roguery/Charm check to extract more coin through creative accounting.");
                    return true;
                },
                _ => ResolveCorruptionMuster(),
                false, 2);

            // Pay option 3: Trade Pay for Select Gear (side deal)
            starter.AddGameMenuOption(MusterPayMenuId, "muster_pay_side_deal",
                "{=muster_pay_side_deal}Trade Pay for Select Gear",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Trade;
                    var enlistment = EnlistmentBehavior.Instance;
                    var payout = enlistment?.PendingMusterPay ?? 0;
                    var reducedPay = (int)(payout * 0.6f);
                    MBTextManager.SetTextVariable("REDUCED_PAY", reducedPay);
                    args.Tooltip = new TextObject("{=muster_pay_side_deal_tt}Take {REDUCED_PAY} denars + premium equipment.");
                    return true;
                },
                _ => ResolveSideDealMuster(),
                false, 3);

            // Pay option 4: Accept a Promise of Payment (IOU)
            starter.AddGameMenuOption(MusterPayMenuId, "muster_pay_iou",
                "{=muster_pay_iou}Accept a Promise of Payment",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Manage;
                    args.Tooltip = new TextObject("{=muster_pay_iou_tt}Defer payment 3 days. No pay tension increase.");
                    var enlistment = EnlistmentBehavior.Instance;
                    // Only available if pay tension is high (60+)
                    return enlistment != null && enlistment.PayTension >= 60;
                },
                _ => ResolvePromissoryMuster(),
                false, 4);

            // Pay option 5: Take Your Final Pay (honorable discharge)
            starter.AddGameMenuOption(MusterPayMenuId, "muster_pay_discharge",
                "{=muster_pay_discharge}Take Your Final Pay",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    args.Tooltip = new TextObject("{=muster_pay_discharge_tt}Receive final pay. Service ends. Pension activated.");
                    var enlistment = EnlistmentBehavior.Instance;
                    return enlistment != null && enlistment.IsPendingDischarge;
                },
                _ => FinalizePendingDischarge(),
                false, 5);

            // Pay option 6: Slip Away in the Night (deserter)
            starter.AddGameMenuOption(MusterPayMenuId, "muster_pay_smuggle",
                "{=muster_pay_smuggle}Slip Away in the Night",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    args.Tooltip = new TextObject("{=muster_pay_smuggle_tt}Desert with your gear. Forfeit pension. Hunted as a deserter.");
                    var enlistment = EnlistmentBehavior.Instance;
                    return enlistment != null && enlistment.IsPendingDischarge;
                },
                _ => ResolveSmuggleDischarge(),
                false, 6);

            // 3. Baggage Check Menu
            starter.AddWaitGameMenu(MusterBaggageMenuId,
                "{=muster_baggage_title}⚠️  BAGGAGE CHECK  ⚠️\n{MUSTER_BAGGAGE_TEXT}",
                OnBaggageCheckInit,
                OnMusterMenuCondition,
                null,
                OnMusterMenuTick,
                GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption);

            // Continue option (shown when no contraband confrontation, or after resolution)
            starter.AddGameMenuOption(MusterBaggageMenuId, "muster_baggage_continue",
                "{=muster_baggage_continue}Continue",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Continue;
                    args.Tooltip = new TextObject("{=muster_baggage_continue_tt}Proceed to next stage.");
                    // Only show if no active contraband confrontation
                    return _currentMuster != null && !IsContrabandConfrontationActive();
                },
                _ => GameMenu.SwitchToMenu(MusterInspectionMenuId),
                false, 1);

            // Bribe option (Rep 35-64)
            starter.AddGameMenuOption(MusterBaggageMenuId, "muster_baggage_bribe",
                "{=muster_baggage_bribe}Pay Him Off",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Trade;

                    if (_currentMuster == null || !_currentMuster.ContrabandFound)
                    {
                        return false;
                    }

                    var qmRep = _currentMuster.QMRep;
                    if (qmRep < 35 || qmRep >= 65)
                    {
                        return false;
                    }

                    var enlistment = EnlistmentBehavior.Instance;
                    if (enlistment == null)
                    {
                        return false;
                    }

                    var playerTier = enlistment.EnlistmentTier;
                    var playerRole = EnlistedStatusManager.Instance?.GetPrimaryRole() ?? "Soldier";
                    var contrabandResult = ContrabandChecker.ScanInventory(playerTier, playerRole);

                    if (!contrabandResult.HasContraband)
                    {
                        return false;
                    }

                    var bribeAmount = ContrabandChecker.CalculateBribeAmount(contrabandResult.MostValuable.Value);
                    var playerGold = Hero.MainHero?.Gold ?? 0;

                    if (playerGold < bribeAmount)
                    {
                        args.IsEnabled = false;
                        args.Tooltip = new TextObject($"{{=muster_bribe_blocked}}Requires {bribeAmount} denars.");
                    }
                    else
                    {
                        args.Tooltip = new TextObject($"{{=muster_bribe_tt}}Charm check. 50% success. Failure increases scrutiny.");
                    }

                    return true;
                },
                _ => HandleBribe(),
                false, 2);

            // Smuggle option (Rep 35-64, Roguery 40+)
            starter.AddGameMenuOption(MusterBaggageMenuId, "muster_baggage_smuggle",
                "{=muster_baggage_smuggle}[Roguery 40+] Smuggle It Past",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;

                    if (_currentMuster == null || !_currentMuster.ContrabandFound)
                    {
                        return false;
                    }

                    var qmRep = _currentMuster.QMRep;
                    if (qmRep < 35 || qmRep >= 65)
                    {
                        return false;
                    }

                    var roguery = Hero.MainHero?.GetSkillValue(DefaultSkills.Roguery) ?? 0;
                    if (roguery < 40)
                    {
                        args.IsEnabled = false;
                        args.Tooltip = new TextObject($"{{=muster_smuggle_blocked}}Requires Roguery 40.");
                    }
                    else
                    {
                        args.Tooltip = new TextObject("{=muster_smuggle_tt}Sleight of hand while distracted. 70% success. Risky.");
                    }

                    return true;
                },
                _ => HandleSmuggle(),
                false, 3);

            // Hand over option (Rep 35-64)
            starter.AddGameMenuOption(MusterBaggageMenuId, "muster_baggage_handover",
                "{=muster_baggage_handover}Hand It Over",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Manage;

                    if (_currentMuster == null || !_currentMuster.ContrabandFound)
                    {
                        return false;
                    }

                    var qmRep = _currentMuster.QMRep;
                    if (qmRep < 35 || qmRep >= 65)
                    {
                        return false;
                    }

                    args.Tooltip = new TextObject("{=muster_handover_tt}Accept confiscation. Fine + 2 Scrutiny. Keep dignity.");
                    return true;
                },
                _ => HandleConfiscation(),
                false, 4);

            // Accept confiscation option (Rep <35)
            starter.AddGameMenuOption(MusterBaggageMenuId, "muster_baggage_accept",
                "{=muster_baggage_accept}Accept Confiscation",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Manage;

                    if (_currentMuster == null || !_currentMuster.ContrabandFound)
                    {
                        return false;
                    }

                    var qmRep = _currentMuster.QMRep;
                    if (qmRep >= 35)
                    {
                        return false;
                    }

                    args.Tooltip = new TextObject("{=muster_accept_tt}Item confiscated. Fine + 2 Scrutiny.");
                    return true;
                },
                _ => HandleConfiscation(),
                false, 5);

            // Protest option (Rep <35)
            starter.AddGameMenuOption(MusterBaggageMenuId, "muster_baggage_protest",
                "{=muster_baggage_protest}Protest the Seizure",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;

                    if (_currentMuster == null || !_currentMuster.ContrabandFound)
                    {
                        return false;
                    }

                    var qmRep = _currentMuster.QMRep;
                    if (qmRep >= 35)
                    {
                        return false;
                    }

                    args.Tooltip = new TextObject("{=muster_protest_tt}20% success. Failure adds scrutiny and discipline.");
                    return true;
                },
                _ => HandleProtest(),
                false, 6);

            // 4. Equipment Inspection Menu
            starter.AddWaitGameMenu(MusterInspectionMenuId,
                "{=muster_inspection_title}⚔️  EQUIPMENT INSPECTION  ⚔️\n{MUSTER_INSPECTION_TEXT}",
                OnInspectionInit,
                OnMusterMenuCondition,
                null,
                OnMusterMenuTick,
                GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption);

            // Inspection option 1: Perfect Attention (OneHanded 30+)
            starter.AddGameMenuOption(MusterInspectionMenuId, "muster_inspection_perfect",
                "{=muster_inspection_perfect}[OneHanded 30+] Stand at Perfect Attention",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.OrderTroopsToAttack;

                    // Only show if inspection is active (not skipped)
                    if (_currentMuster == null || !string.IsNullOrEmpty(_currentMuster.InspectionOutcome))
                    {
                        return false;
                    }

                    var skill = GetRelevantMeleeSkill();
                    var skillValue = Hero.MainHero?.GetSkillValue(skill) ?? 0;

                    if (skillValue < 30)
                    {
                        args.IsEnabled = false;
                        args.Tooltip = new TextObject($"{{=muster_inspection_perfect_blocked}}Requires {skill.Name} 30.");
                    }
                    else
                    {
                        args.Tooltip = new TextObject("{=muster_inspection_perfect_tt}Flawless presentation. +10 OneHanded XP, +6 Officer Rep, +3 Soldier Rep.");
                    }

                    return true;
                },
                _ => ResolveInspectionPerfect(),
                false, 1);

            // Inspection option 2: Basic Requirements
            starter.AddGameMenuOption(MusterInspectionMenuId, "muster_inspection_basic",
                "{=muster_inspection_basic}Meet the Basic Requirements",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Manage;

                    // Only show if inspection is active
                    if (_currentMuster == null || !string.IsNullOrEmpty(_currentMuster.InspectionOutcome))
                    {
                        return false;
                    }

                    args.Tooltip = new TextObject("{=muster_inspection_basic_tt}Presentable enough. +5 OneHanded XP, +2 Officer Rep.");
                    return true;
                },
                _ => ResolveInspectionBasic(),
                false, 2);

            // Inspection option 3: Appear Slovenly
            starter.AddGameMenuOption(MusterInspectionMenuId, "muster_inspection_unprepared",
                "{=muster_inspection_unprepared}Appear Slovenly",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;

                    // Only show if inspection is active
                    if (_currentMuster == null || !string.IsNullOrEmpty(_currentMuster.InspectionOutcome))
                    {
                        return false;
                    }

                    args.Tooltip = new TextObject("{=muster_inspection_unprepared_tt}Poor showing. -8 Officer Rep, -4 Soldier Rep, +8 Scrutiny.");
                    return true;
                },
                _ => ResolveInspectionUnprepared(),
                false, 3);

            // Continue option (shown when inspection is skipped or after resolution)
            starter.AddGameMenuOption(MusterInspectionMenuId, "muster_inspection_continue",
                "{=muster_inspection_continue}Continue",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Continue;
                    args.Tooltip = new TextObject("{=muster_inspection_continue_tt}Proceed to next stage.");
                    // Only show if inspection was skipped or already resolved
                    return _currentMuster != null && !string.IsNullOrEmpty(_currentMuster.InspectionOutcome);
                },
                _ => GameMenu.SwitchToMenu(MusterRecruitMenuId),
                false, 4);

            // 5. Green Recruit Menu
            starter.AddWaitGameMenu(MusterRecruitMenuId,
                "{=muster_recruit_title}👥  GREEN RECRUIT  👥\n{MUSTER_RECRUIT_TEXT}",
                OnRecruitInit,
                OnMusterMenuCondition,
                null,
                OnMusterMenuTick,
                GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption);

            // Recruit option 1: Train Him (Leadership 25+)
            starter.AddGameMenuOption(MusterRecruitMenuId, "muster_recruit_train",
                "{=muster_recruit_train}[Leadership 25+] Take Him Aside and Train Him",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Conversation;

                    // Only show if recruit event is active (not skipped)
                    if (_currentMuster == null || !string.IsNullOrEmpty(_currentMuster.RecruitOutcome))
                    {
                        return false;
                    }

                    var leadership = Hero.MainHero?.GetSkillValue(DefaultSkills.Leadership) ?? 0;

                    if (leadership < 25)
                    {
                        args.IsEnabled = false;
                        args.Tooltip = new TextObject("{=muster_recruit_train_blocked}Requires Leadership 25.");
                    }
                    else
                    {
                        args.Tooltip = new TextObject("{=muster_recruit_train_tt}Train the recruit. +15 Leadership XP, +5 Officer Rep, +6 Soldier Rep.");
                    }

                    return true;
                },
                _ => ResolveRecruitTrain(),
                false, 1);

            // Recruit option 2: Let Him Figure It Out
            starter.AddGameMenuOption(MusterRecruitMenuId, "muster_recruit_ignore",
                "{=muster_recruit_ignore}Let Him Figure It Out",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;

                    // Only show if recruit event is active
                    if (_currentMuster == null || !string.IsNullOrEmpty(_currentMuster.RecruitOutcome))
                    {
                        return false;
                    }

                    args.Tooltip = new TextObject("{=muster_recruit_ignore_tt}Ignore the recruit. -2 Soldier Rep.");
                    return true;
                },
                _ => ResolveRecruitIgnore(),
                false, 2);

            // Recruit option 3: Traditional Welcome (hazing)
            starter.AddGameMenuOption(MusterRecruitMenuId, "muster_recruit_haze",
                "{=muster_recruit_haze}Give Him the Traditional Welcome",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;

                    // Only show if recruit event is active
                    if (_currentMuster == null || !string.IsNullOrEmpty(_currentMuster.RecruitOutcome))
                    {
                        return false;
                    }

                    args.Tooltip = new TextObject("{=muster_recruit_haze_tt}Haze the recruit. +4 Soldier Rep, -5 Officer Rep, +8 Discipline.");
                    return true;
                },
                _ => ResolveRecruitHaze(),
                false, 3);

            // Continue option (shown when recruit is skipped or after resolution)
            starter.AddGameMenuOption(MusterRecruitMenuId, "muster_recruit_continue",
                "{=muster_recruit_continue}Continue",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Continue;
                    args.Tooltip = new TextObject("{=muster_recruit_continue_tt}Proceed to next stage.");
                    // Only show if recruit was skipped or already resolved
                    return _currentMuster != null && !string.IsNullOrEmpty(_currentMuster.RecruitOutcome);
                },
                _ => ProceedToNextStageFromRecruit(),
                false, 4);

            // 6. Promotion Recap Menu
            starter.AddWaitGameMenu(MusterPromotionRecapMenuId,
                "{=muster_promotion_title}⭐  PROMOTION ACKNOWLEDGED  ⭐\n{MUSTER_PROMOTION_TEXT}",
                OnPromotionInit,
                OnMusterMenuCondition,
                null,
                OnMusterMenuTick,
                GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption);

            starter.AddGameMenuOption(MusterPromotionRecapMenuId, "muster_promotion_continue",
                "{=muster_promotion_continue}Acknowledge Promotion",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Continue;
                    args.Tooltip = new TextObject("{=muster_promotion_continue_tt}Proceed with muster.");
                    return true;
                },
                _ => ProceedToNextStageFromPromotion(),
                false, 1);

            starter.AddGameMenuOption(MusterPromotionRecapMenuId, "muster_promotion_visit_qm",
                "{=muster_promotion_visit_qm}Visit Quartermaster Now",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Trade;
                    args.Tooltip = new TextObject("{=muster_promotion_visit_qm_tt}Review newly unlocked equipment. Will return to muster after.");
                    return true;
                },
                _ =>
                {
                    if (_currentMuster != null)
                    {
                        _currentMuster.VisitQMAfter = true;
                        ModLogger.Info(LogCategory, "Player flagged QM visit from promotion stage");
                    }
                    ProceedToNextStageFromPromotion();
                },
                false, 2);

            // 7. Retinue Muster Menu (T7+ only)
            starter.AddWaitGameMenu(MusterRetinueMenuId,
                "{=muster_retinue_title}🏴  RETINUE MUSTER  🏴\n{MUSTER_RETINUE_TEXT}",
                OnRetinueInit,
                OnMusterMenuCondition,
                null,
                OnMusterMenuTick,
                GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption);

            starter.AddGameMenuOption(MusterRetinueMenuId, "muster_retinue_continue",
                "{=muster_retinue_continue}Dismiss Retinue",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Continue;
                    args.Tooltip = new TextObject("{=muster_retinue_continue_tt}Proceed to muster summary.");
                    return true;
                },
                _ => GameMenu.SwitchToMenu(MusterCompleteMenuId),
                false, 1);

            starter.AddGameMenuOption(MusterRetinueMenuId, "muster_retinue_recruit",
                "{=muster_retinue_recruit}Seek New Soldiers",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.TroopSelection;
                    args.Tooltip = new TextObject("{=muster_retinue_recruit_tt}Open recruitment after muster. Fill retinue openings.");
                    // Only available if retinue has openings
                    return _currentMuster != null && _currentMuster.RetinueStrength < _currentMuster.RetinueCapacity;
                },
                _ =>
                {
                    if (_currentMuster != null)
                    {
                        _currentMuster.RecruitRetinueAfter = true;
                    }
                    GameMenu.SwitchToMenu(MusterCompleteMenuId);
                },
                false, 2);

            // 8. Muster Complete Menu
            starter.AddWaitGameMenu(MusterCompleteMenuId,
                "{=muster_complete_title}⚔  MUSTER COMPLETE - DAY {MUSTER_DAY}  ⚔\n{MUSTER_COMPLETE_TEXT}",
                OnMusterCompleteInit,
                OnMusterMenuCondition,
                null,
                OnMusterMenuTick,
                GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption);

            starter.AddGameMenuOption(MusterCompleteMenuId, "muster_complete_dismiss",
                "{=muster_complete_dismiss}Dismiss",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    args.Tooltip = new TextObject("{=muster_complete_dismiss_tt}Return to map. Muster complete.");
                    return true;
                },
                _ => CompleteMusterSequence(),
                false, 1);

            starter.AddGameMenuOption(MusterCompleteMenuId, "muster_complete_qm",
                "{=muster_complete_qm}Visit Quartermaster",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Trade;
                    var supply = CompanySupplyManager.Instance?.TotalSupply ?? 100;
                    if (supply < 15)
                    {
                        args.Tooltip = new TextObject("{=muster_complete_qm_blocked_tt}Quartermaster unavailable. Supplies critically low. Equipment requisitions suspended.");
                        args.IsEnabled = false;
                    }
                    else
                    {
                        args.Tooltip = new TextObject("{=muster_complete_qm_tt}Browse newly refreshed equipment stock and access your baggage.");
                    }
                    return true;
                },
                _ =>
                {
                    if (_currentMuster != null)
                    {
                        _currentMuster.VisitQMAfter = true;
                    }
                    CompleteMusterSequence();
                },
                false, 2);

            starter.AddGameMenuOption(MusterCompleteMenuId, "muster_complete_records",
                "{=muster_complete_records}Review Service Record",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Manage;
                    args.Tooltip = new TextObject("{=muster_complete_records_tt}Check your full military history.");
                    return true;
                },
                _ =>
                {
                    CompleteMusterSequence();
                    // TODO: Open service records menu
                },
                false, 3);

            starter.AddGameMenuOption(MusterCompleteMenuId, "muster_complete_leave",
                "{=muster_complete_leave}Request Leave of Absence",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                    args.Tooltip = new TextObject("{=muster_complete_leave_tt}Request leave of absence. 14-day limit.");
                    // Disabled during combat/siege
                    var enlistment = EnlistmentBehavior.Instance;
                    if (enlistment?.EnlistedLord?.PartyBelongedTo?.BesiegerCamp != null ||
                        enlistment?.EnlistedLord?.PartyBelongedTo?.MapEvent != null)
                    {
                        args.IsEnabled = false;
                        args.Tooltip = new TextObject("{=muster_complete_leave_blocked_tt}Cannot request leave during active siege or combat.");
                    }
                    return true;
                },
                _ =>
                {
                    if (_currentMuster != null)
                    {
                        _currentMuster.RequestLeaveAfter = true;
                    }
                    CompleteMusterSequence();
                },
                false, 4);

            ModLogger.Debug(LogCategory, "All 8 muster menu stages registered");
        }

        /// <summary>
        /// Checks if muster can be triggered in the current game state.
        /// Returns a tuple: (canTrigger, deferReason).
        /// </summary>
        private (bool canTrigger, string deferReason) CanTriggerMuster()
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null || !enlistment.IsEnlisted)
            {
                return (false, "Not enlisted");
            }

            // Player in combat
            if (MobileParty.MainParty?.MapEvent != null)
            {
                return (false, "Player in combat");
            }

            // Player captured/prisoner
            if (Hero.MainHero?.IsPrisoner == true)
            {
                return (false, "Player captured");
            }

            // Player in active encounter (siege, battle preparation)
            if (PlayerEncounter.Current != null && !PlayerEncounter.InsideSettlement)
            {
                return (false, "Player in encounter");
            }

            // Player in conversation
            if (Campaign.Current?.ConversationManager?.IsConversationInProgress == true)
            {
                return (false, "Player in conversation");
            }

            // Already in a muster sequence
            if (_currentMuster != null)
            {
                return (false, "Muster already in progress");
            }

            // Check if currently in a game menu that would conflict
            var menuManager = Campaign.Current?.GameMenuManager;
            var nextMenu = menuManager?.NextMenu;
            if (nextMenu != null && !string.IsNullOrEmpty(nextMenu.StringId))
            {
                // Allow muster menus themselves
                if (nextMenu.StringId.StartsWith("enlisted_muster"))
                {
                    return (true, null);
                }

                // Allow from town/settlement menus (muster closes them)
                if (nextMenu.StringId.Contains("town") || nextMenu.StringId.Contains("village") ||
                    nextMenu.StringId.Contains("castle") || nextMenu.StringId.Contains("settlement"))
                {
                    return (true, null);
                }

                // Defer from other menus
                return (false, "Player in menu: " + nextMenu.StringId);
            }

            return (true, null);
        }

        /// <summary>
        /// Checks for pending muster triggers after combat/menu exits.
        /// Called periodically from hourly tick.
        /// </summary>
        public void CheckPendingMusterTrigger()
        {
            if (!_musterPendingAfterCombat && !_musterPendingAfterMenu)
            {
                return;
            }

            var (canTrigger, reason) = CanTriggerMuster();
            if (canTrigger)
            {
                _musterPendingAfterCombat = false;
                _musterPendingAfterMenu = false;
                ModLogger.Info(LogCategory, "Executing deferred muster trigger");
                BeginMusterSequenceInternal();
            }
            else
            {
                ModLogger.Debug(LogCategory, $"Deferred muster still blocked: {reason}");
            }
        }

        /// <summary>
        /// Entry point called from EnlistmentBehavior when muster day arrives.
        /// Creates a new session state, captures context, and switches to the intro menu.
        /// Handles edge cases like combat, capture, and menu states.
        /// </summary>
        public void BeginMusterSequence()
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null || !enlistment.IsEnlisted)
            {
                ModLogger.Warn(LogCategory, "BeginMusterSequence called but player is not enlisted");
                return;
            }

            // If menu registration failed, fall back immediately to legacy system
            if (!_menusRegisteredSuccessfully)
            {
                ModLogger.Warn(LogCategory, "Muster menus not registered, using legacy fallback");
                FallbackToLegacyMuster();
                return;
            }

            // Prevent rapid retry attempts
            if (CampaignTime.Now - _lastMusterTriggerAttempt < CampaignTime.Hours(1))
            {
                ModLogger.Debug(LogCategory, "Muster trigger attempt throttled (< 1 hour since last)");
                return;
            }
            _lastMusterTriggerAttempt = CampaignTime.Now;

            // Check if we can trigger muster in current state
            var (canTrigger, deferReason) = CanTriggerMuster();

            if (!canTrigger)
            {
                // Handle specific defer scenarios
                if (deferReason == "Player in combat")
                {
                    _musterPendingAfterCombat = true;
                    ModLogger.Info(LogCategory, "Muster deferred: player in combat. Will trigger when combat ends.");
                    return;
                }

                if (deferReason == "Player captured")
                {
                    // Skip muster entirely while captured, wages accumulate as backpay
                    ModLogger.Info(LogCategory, "Muster skipped: player captured. Wages will accumulate as backpay.");
                    enlistment.DeferPayMuster();
                    return;
                }

                if (deferReason == "Player in conversation" || deferReason?.StartsWith("Player in menu:") == true)
                {
                    _musterPendingAfterMenu = true;
                    ModLogger.Info(LogCategory, $"Muster deferred: {deferReason}. Will trigger after.");
                    return;
                }

                if (deferReason == "Muster already in progress")
                {
                    ModLogger.Debug(LogCategory, "Muster trigger ignored: already in progress");
                    return;
                }

                // Unknown defer reason, log and defer
                ModLogger.Warn(LogCategory, $"Muster deferred: {deferReason}");
                _musterPendingAfterMenu = true;
                return;
            }

            BeginMusterSequenceInternal();
        }

        /// <summary>
        /// Internal implementation of muster sequence start.
        /// Called after edge case checks pass.
        /// </summary>
        private void BeginMusterSequenceInternal()
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null || !enlistment.IsEnlisted)
            {
                ModLogger.ErrorCode(LogCategory, "E-MUSTER-005", "BeginMusterSequenceInternal called but player not enlisted");
                return;
            }

            try
            {
                var currentDay = Campaign.Current != null ? (int)CampaignTime.Now.ToDays : 0;
                ModLogger.Info(LogCategory, $"Beginning muster sequence on day {currentDay}");

                // Create fresh session state
                _currentMuster = new MusterSessionState
                {
                    CurrentStage = MusterIntroMenuId,
                    MusterDay = currentDay,
                    LastMusterDay = enlistment.LastMusterDay
                };

                // Capture strategic context for flavor text
                var lordParty = enlistment.EnlistedLord?.PartyBelongedTo;
                _currentMuster.StrategicContext = lordParty != null
                    ? ArmyContextAnalyzer.GetLordStrategicContext(lordParty)
                    : "patrol_peacetime";

                // Capture fatigue state before reset
                _currentMuster.FatigueBeforeMuster = enlistment.FatigueCurrent;

                // Check for high scrutiny warning
                var scrutiny = EscalationManager.Instance?.State?.Scrutiny ?? 0;
                if (scrutiny >= 5)
                {
                    _currentMuster.HighScrutinyWarning = true;
                    ModLogger.Debug(LogCategory, $"High scrutiny warning enabled (scrutiny={scrutiny})");
                }

                // Check for promotion this period
                if (enlistment.EnlistmentTier > enlistment.TierAtLastMuster && enlistment.TierAtLastMuster > 0)
                {
                    _currentMuster.PromotionOccurredThisPeriod = true;
                    _currentMuster.PreviousTier = enlistment.TierAtLastMuster;
                    _currentMuster.CurrentTier = enlistment.EnlistmentTier;
                    _currentMuster.PromotionDay = enlistment.DayOfLastPromotion;
                    ModLogger.Info(LogCategory, $"Promotion detected this period: T{_currentMuster.PreviousTier} -> T{_currentMuster.CurrentTier}");
                }

                // Close any current menu before opening muster (handles town/settlement menus)
                var menuManager = Campaign.Current?.GameMenuManager;
                var nextMenu = menuManager?.NextMenu;
                if (nextMenu != null && !nextMenu.StringId.StartsWith("enlisted_muster"))
                {
                    ModLogger.Debug(LogCategory, $"Closing current menu before muster: {nextMenu.StringId}");
                }

                // Switch to the intro menu (time control will be initialized in OnMusterIntroInit)
                GameMenu.ActivateGameMenu(MusterIntroMenuId);
                ModLogger.Debug(LogCategory, $"Muster intro menu activated (context: {_currentMuster.StrategicContext})");
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode(LogCategory, "E-MUSTER-005", "Unhandled exception in BeginMusterSequence", ex);
                // Ensure player isn't stuck - try fallback
                AbortMusterWithFallback("Failed to start muster menu sequence");
            }
        }

        /// <summary>
        /// Falls back to legacy pay muster inquiry system.
        /// Used when menu registration fails or muster sequence encounters critical error.
        /// </summary>
        private void FallbackToLegacyMuster()
        {
            try
            {
                var incidentsBehavior = EnlistedIncidentsBehavior.Instance;
                if (incidentsBehavior != null)
                {
                    ModLogger.Info(LogCategory, "Using legacy pay muster inquiry");
                    incidentsBehavior.TriggerPayMusterIncident();
                }
                else
                {
                    // Last resort: just defer the muster
                    ModLogger.Warn(LogCategory, "Legacy muster system unavailable, deferring");
                    EnlistmentBehavior.Instance?.DeferPayMuster();
                    InformationManager.DisplayMessage(new InformationMessage(
                        "Muster will resume next cycle.", Colors.Yellow));
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Legacy muster fallback failed", ex);
                EnlistmentBehavior.Instance?.DeferPayMuster();
                InformationManager.DisplayMessage(new InformationMessage(
                    "Muster will resume next cycle.", Colors.Yellow));
            }
        }

        /// <summary>
        /// Aborts the current muster sequence with a message and falls back to old system if available.
        /// Ensures player is never stuck.
        /// </summary>
        private void AbortMusterWithFallback(string reason)
        {
            ModLogger.ErrorCode(LogCategory, "E-MUSTER-005", $"Aborting muster: {reason}");

            // Clear state
            _currentMuster = null;
            _musterPendingAfterCombat = false;
            _musterPendingAfterMenu = false;

            // Restore time control if locked
            try
            {
                if (Campaign.Current != null)
                {
                    Campaign.Current.SetTimeControlModeLock(false);
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Failed to unlock time control during abort", ex);
            }

            // Try to exit any active menu
            try
            {
                GameMenu.ExitToLast();
            }
            catch
            {
                // Ignore - might not be in a menu
            }

            // Fall back to old inquiry system
            FallbackToLegacyMuster();
        }

        #region Menu Init Handlers

        /// <summary>
        /// Safely switches to another muster stage with error handling.
        /// If transition fails, jumps to muster complete or aborts.
        /// </summary>
        private void SafeSwitchToMenu(string menuId)
        {
            try
            {
                ModLogger.Debug(LogCategory, $"Switching to menu: {menuId}");
                GameMenu.SwitchToMenu(menuId);
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode(LogCategory, "E-MUSTER-002", $"Stage transition failed to {menuId}", ex);
                _currentMuster?.EncounteredErrors.Add($"Stage transition failed: {menuId}");

                // Try to jump to complete stage if not already there
                if (menuId != MusterCompleteMenuId)
                {
                    try
                    {
                        GameMenu.SwitchToMenu(MusterCompleteMenuId);
                    }
                    catch
                    {
                        AbortMusterWithFallback($"Failed to switch to {menuId} and complete stage");
                    }
                }
                else
                {
                    AbortMusterWithFallback($"Failed to switch to complete stage");
                }
            }
        }

        /// <summary>
        /// Wraps effect application in try-catch, logging errors but continuing muster.
        /// </summary>
        private bool SafeApplyEffect(string effectName, Action effectAction)
        {
            try
            {
                effectAction();
                return true;
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode(LogCategory, "E-MUSTER-004", $"Effect application failed: {effectName}", ex);
                if (_currentMuster != null)
                {
                    _currentMuster.EffectsPartiallyFailed = true;
                    _currentMuster.EncounteredErrors.Add($"Effect failed: {effectName}");
                }
                return false;
            }
        }

        private void OnMusterIntroInit(MenuCallbackArgs args)
        {
            try
            {
                if (_currentMuster == null)
                {
                    ModLogger.Warn(LogCategory, "OnMusterIntroInit called with null session state");
                    return;
                }

                _currentMuster.CurrentStage = MusterIntroMenuId;

                // Reset fatigue to maximum (muster day = rest day)
                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment != null)
                {
                    SafeApplyEffect("FatigueReset", () =>
                    {
                        var fatigueBeforeReset = enlistment.FatigueCurrent;
                        enlistment.RestoreFatigue(0, "Muster rest day");
                        _currentMuster.FatigueBeforeMuster = fatigueBeforeReset;
                        ModLogger.Debug(LogCategory, $"Fatigue restored from {fatigueBeforeReset} to {enlistment.FatigueCurrent}");
                    });
                }

                // Build intro text with null safety
                var introText = BuildIntroTextSafe();
                MBTextManager.SetTextVariable("MUSTER_DAY", _currentMuster.MusterDay.ToString());
                MBTextManager.SetTextVariable("MUSTER_INTRO_TEXT", introText);

                // Initialize time control on first stage
                InitializeMusterTimeControl(args);
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode(LogCategory, "E-MUSTER-002", "OnMusterIntroInit failed", ex);
                AbortMusterWithFallback("Intro stage initialization failed");
            }
        }

        private void OnMusterPayInit(MenuCallbackArgs args)
        {
            try
            {
                if (_currentMuster == null)
                {
                    return;
                }
                _currentMuster.CurrentStage = MusterPayMenuId;

                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment == null)
                {
                    ModLogger.Warn(LogCategory, "OnMusterPayInit: EnlistmentBehavior null");
                    MBTextManager.SetTextVariable("MUSTER_PAY_TEXT", "Pay records unavailable.");
                    MBTextManager.SetTextVariable("PAY_AMOUNT", "0");
                    return;
                }

                // Set pay amount variable for option text
                MBTextManager.SetTextVariable("PAY_AMOUNT", enlistment.PendingMusterPay.ToString());
                MBTextManager.SetTextVariable("MUSTER_PAY_TEXT", BuildPayTextSafe());
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode(LogCategory, "E-MUSTER-002", "OnMusterPayInit failed", ex);
                MBTextManager.SetTextVariable("MUSTER_PAY_TEXT", "Pay records unavailable. Contact paymaster.");
                MBTextManager.SetTextVariable("PAY_AMOUNT", "0");
            }
        }

        /// <summary>
        /// Builds intro text with null safety for all dependent systems.
        /// </summary>
        private string BuildIntroTextSafe()
        {
            try
            {
                return BuildIntroText();
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "BuildIntroText failed, using fallback", ex);
                _currentMuster?.EncounteredErrors.Add("Intro text generation failed");

                var sb = new StringBuilder();
                sb.AppendLine("The sergeants call the muster. Your company assembles in formation.");
                sb.AppendLine();
                sb.AppendLine("_____ YOUR SERVICE RECORD _____");
                sb.AppendLine();

                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment != null)
                {
                    sb.AppendLine($"Rank: {GetRankName(enlistment.EnlistmentTier)} (Tier {enlistment.EnlistmentTier})");
                    sb.AppendLine($"Days Served: {enlistment.DaysServed} days");
                }
                else
                {
                    sb.AppendLine("Service record unavailable.");
                }

                return sb.ToString();
            }
        }

        /// <summary>
        /// Builds pay text with null safety for all dependent systems.
        /// </summary>
        private string BuildPayTextSafe()
        {
            try
            {
                return BuildPayText();
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "BuildPayText failed, using fallback", ex);
                _currentMuster?.EncounteredErrors.Add("Pay text generation failed");

                var sb = new StringBuilder();
                sb.AppendLine("You step forward to the paymaster's table. He opens his ledger.");
                sb.AppendLine();
                sb.AppendLine("_____ PAY STATUS _____");
                sb.AppendLine();

                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment != null)
                {
                    sb.AppendLine($"Wages Owed: {enlistment.PendingMusterPay} denars");
                }
                else
                {
                    sb.AppendLine("Pay records unavailable.");
                }

                return sb.ToString();
            }
        }

        private void OnBaggageCheckInit(MenuCallbackArgs args)
        {
            if (_currentMuster == null)
            {
                return;
            }
            _currentMuster.CurrentStage = MusterBaggageMenuId;

            // Perform the baggage check (30% chance, only if contraband found)
            ProcessBaggageCheck();

            // Set title based on whether contraband was found
            if (_currentMuster.ContrabandFound && _currentMuster.QMRep < 65)
            {
                MBTextManager.SetTextVariable("muster_baggage_title", "⚠️  CONTRABAND DISCOVERED  ⚠️");
            }

            MBTextManager.SetTextVariable("MUSTER_BAGGAGE_TEXT", BuildBaggageText());
        }

        private void OnInspectionInit(MenuCallbackArgs args)
        {
            if (_currentMuster == null)
            {
                return;
            }
            _currentMuster.CurrentStage = MusterInspectionMenuId;

            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null)
            {
                MBTextManager.SetTextVariable("MUSTER_INSPECTION_TEXT", "Enlistment data unavailable.");
                return;
            }

            // Check 12-day cooldown
            var currentDay = (int)CampaignTime.Now.ToDays;
            var daysSinceInspection = currentDay - enlistment.LastInspectionDay;

            if (daysSinceInspection < 12)
            {
                // On cooldown, skip inspection
                _currentMuster.InspectionOutcome = "skipped_cooldown";
                ModLogger.Debug(LogCategory, $"Inspection skipped: cooldown ({daysSinceInspection}/12 days)");
                GameMenu.SwitchToMenu(MusterRecruitMenuId);
                return;
            }

            // Check if player wounded <30%
            var hero = Hero.MainHero;
            if (hero != null)
            {
                var healthPercent = (hero.HitPoints / (float)hero.MaxHitPoints) * 100f;
                if (healthPercent < 30f)
                {
                    // Too wounded, excused from inspection
                    _currentMuster.InspectionOutcome = "excused_wounded";
                    ModLogger.Debug(LogCategory, $"Inspection skipped: player wounded ({healthPercent:F0}%)");
                    GameMenu.SwitchToMenu(MusterRecruitMenuId);
                    return;
                }
            }

            // Display inspection text
            MBTextManager.SetTextVariable("MUSTER_INSPECTION_TEXT", BuildInspectionText());
        }

        private void OnRecruitInit(MenuCallbackArgs args)
        {
            if (_currentMuster == null)
            {
                return;
            }
            _currentMuster.CurrentStage = MusterRecruitMenuId;

            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null)
            {
                MBTextManager.SetTextVariable("MUSTER_RECRUIT_TEXT", "Enlistment data unavailable.");
                return;
            }

            // Check tier requirement (must be T3+)
            if (enlistment.EnlistmentTier < 3)
            {
                // Too junior to mentor
                _currentMuster.RecruitOutcome = "skipped_tier";
                ModLogger.Debug(LogCategory, $"Recruit stage skipped: tier too low (T{enlistment.EnlistmentTier})");
                ProceedToNextStageFromRecruit();
                return;
            }

            // Check 10-day cooldown
            var currentDay = (int)CampaignTime.Now.ToDays;
            var daysSinceRecruit = currentDay - enlistment.LastRecruitDay;

            if (daysSinceRecruit < 10)
            {
                // On cooldown, skip recruit stage
                _currentMuster.RecruitOutcome = "skipped_cooldown";
                ModLogger.Debug(LogCategory, $"Recruit stage skipped: cooldown ({daysSinceRecruit}/10 days)");
                ProceedToNextStageFromRecruit();
                return;
            }

            // Display recruit text
            MBTextManager.SetTextVariable("MUSTER_RECRUIT_TEXT", BuildRecruitText());
        }

        private void OnPromotionInit(MenuCallbackArgs args)
        {
            if (_currentMuster == null)
            {
                return;
            }
            _currentMuster.CurrentStage = MusterPromotionRecapMenuId;

            MBTextManager.SetTextVariable("MUSTER_PROMOTION_TEXT", BuildPromotionText());
        }

        private void OnRetinueInit(MenuCallbackArgs args)
        {
            if (_currentMuster == null)
            {
                return;
            }
            _currentMuster.CurrentStage = MusterRetinueMenuId;

            MBTextManager.SetTextVariable("MUSTER_RETINUE_TEXT", BuildRetinueText());
        }

        private void OnMusterCompleteInit(MenuCallbackArgs args)
        {
            try
            {
                if (_currentMuster == null)
                {
                    return;
                }
                _currentMuster.CurrentStage = MusterCompleteMenuId;

                MBTextManager.SetTextVariable("MUSTER_DAY", _currentMuster.MusterDay.ToString());
                MBTextManager.SetTextVariable("MUSTER_COMPLETE_TEXT", BuildMusterCompleteTextSafe());
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode(LogCategory, "E-MUSTER-002", "OnMusterCompleteInit failed", ex);
                MBTextManager.SetTextVariable("MUSTER_DAY", "?");
                MBTextManager.SetTextVariable("MUSTER_COMPLETE_TEXT", "Muster summary unavailable. You may dismiss.");
            }
        }

        /// <summary>
        /// Builds muster complete text with null safety and edge case handling.
        /// </summary>
        private string BuildMusterCompleteTextSafe()
        {
            try
            {
                var text = BuildMusterCompleteText();

                // Append warnings for edge cases
                var sb = new StringBuilder(text);

                // High scrutiny warning
                if (_currentMuster?.HighScrutinyWarning == true)
                {
                    sb.AppendLine();
                    sb.AppendLine("⚠️ WARNING: You are under heightened scrutiny. Your actions are being watched closely.");
                }

                // Effects partially failed warning
                if (_currentMuster?.EffectsPartiallyFailed == true)
                {
                    sb.AppendLine();
                    sb.AppendLine("⚠️ Some effects may not have applied correctly.");
                }

                // Discharge threshold warning
                if (_currentMuster?.DischargeThresholdReached == true)
                {
                    sb.AppendLine();
                    sb.AppendLine("⚠️ DISCIPLINARY NOTICE: Your conduct will be reviewed by command.");
                }

                // Supply crisis warning
                var supply = CompanySupplyManager.Instance?.TotalSupply ?? 100;
                if (supply < 20)
                {
                    sb.AppendLine();
                    sb.AppendLine("⚠️ CRITICAL SUPPLY: Quartermaster has locked all baggage access.");
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "BuildMusterCompleteText failed, using fallback", ex);
                _currentMuster?.EncounteredErrors.Add("Complete text generation failed");

                var sb = new StringBuilder();
                sb.AppendLine("Muster is dismissed. The sergeants release the company.");
                sb.AppendLine();
                sb.AppendLine("_____ MUSTER OUTCOMES _____");
                sb.AppendLine();

                if (_currentMuster != null)
                {
                    sb.AppendLine($"Pay: {_currentMuster.PayOutcome ?? "unknown"}");
                    sb.AppendLine($"Baggage: {_currentMuster.BaggageOutcome ?? "not checked"}");
                    sb.AppendLine($"Inspection: {_currentMuster.InspectionOutcome ?? "skipped"}");
                }
                else
                {
                    sb.AppendLine("Details unavailable.");
                }

                return sb.ToString();
            }
        }

        #endregion

        #region Menu Flow Control

        private static bool OnMusterMenuCondition(MenuCallbackArgs args)
        {
            // Muster menus are always available when active
            return true;
        }

        private static void OnMusterMenuTick(MenuCallbackArgs args, CampaignTime dt)
        {
            // No-op tick handler for wait menus
        }

        /// <summary>
        /// Resolves standard pay muster (accept your pay).
        /// Calls EnlistmentBehavior.ResolvePayMusterStandard() which handles full/partial/delay logic internally.
        /// Note: ResolvePayMusterStandard calls OnMusterCycleComplete which processes rations and QM stock.
        /// </summary>
        private void ResolvePayMusterStandard()
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null)
            {
                ModLogger.ErrorCode(LogCategory, "E-MUSTER-004", "Cannot resolve standard pay: EnlistmentBehavior null");
                if (_currentMuster != null)
                {
                    _currentMuster.EffectsPartiallyFailed = true;
                    _currentMuster.PayOutcome = "error";
                }
                ProceedToNextStageFromPay();
                return;
            }

            var success = SafeApplyEffect("StandardPay", () =>
            {
                ModLogger.Info(LogCategory, "Resolving standard pay from muster menu");

                // Capture pay details before resolution for muster state tracking
                if (_currentMuster != null)
                {
                    var wages = enlistment.PendingMusterPay;
                    var backpay = enlistment.OwedBackpay;
                    _currentMuster.PayReceived = wages + backpay;
                    _currentMuster.PayOutcome = "full";
                }

                // Call EnlistmentBehavior's standard pay resolution
                enlistment.ResolvePayMusterStandard();

                // Update muster state with actual outcome
                if (_currentMuster != null)
                {
                    _currentMuster.PayOutcome = enlistment.LastPayOutcome;
                }

                ModLogger.Debug(LogCategory, $"Standard pay resolved: {enlistment.LastPayOutcome}");
            });

            if (!success && _currentMuster != null)
            {
                _currentMuster.PayOutcome = "error";
            }

            ProceedToNextStageFromPay();
        }

        /// <summary>
        /// Resolves corruption muster (demand a recount).
        /// Attempts skill check to get 95% payout through illicit channels.
        /// </summary>
        private void ResolveCorruptionMuster()
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null)
            {
                ModLogger.Warn(LogCategory, "Cannot resolve corruption pay: EnlistmentBehavior unavailable");
                ProceedToNextStageFromPay();
                return;
            }

            try
            {
                ModLogger.Info(LogCategory, "Resolving corruption pay from muster menu");

                if (_currentMuster != null)
                {
                    _currentMuster.PayOutcome = "corruption";
                }

                enlistment.ResolveCorruptionMuster();

                if (_currentMuster != null)
                {
                    _currentMuster.PayOutcome = enlistment.LastPayOutcome;
                }

                ModLogger.Debug(LogCategory, $"Corruption pay resolved: {enlistment.LastPayOutcome}");
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode(LogCategory, "E-MUSTER-004", "Failed to resolve corruption pay", ex);
            }

            ProceedToNextStageFromPay();
        }

        /// <summary>
        /// Resolves side deal muster (trade pay for gear).
        /// Takes reduced pay (60%) but opens equipment selection after muster.
        /// </summary>
        private void ResolveSideDealMuster()
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null)
            {
                ModLogger.Warn(LogCategory, "Cannot resolve side deal pay: EnlistmentBehavior unavailable");
                ProceedToNextStageFromPay();
                return;
            }

            try
            {
                ModLogger.Info(LogCategory, "Resolving side deal pay from muster menu");

                if (_currentMuster != null)
                {
                    _currentMuster.PayOutcome = "side_deal";
                    // Flag QM visit after muster for gear selection
                    _currentMuster.VisitQMAfter = true;
                }

                enlistment.ResolveSideDealMuster();

                if (_currentMuster != null)
                {
                    _currentMuster.PayOutcome = enlistment.LastPayOutcome;
                }

                ModLogger.Debug(LogCategory, $"Side deal pay resolved: {enlistment.LastPayOutcome}");
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode(LogCategory, "E-MUSTER-004", "Failed to resolve side deal pay", ex);
            }

            ProceedToNextStageFromPay();
        }

        /// <summary>
        /// Resolves promissory note muster (IOU).
        /// Defers payment 3 days to ease lord's finances.
        /// </summary>
        private void ResolvePromissoryMuster()
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null)
            {
                ModLogger.Warn(LogCategory, "Cannot resolve promissory pay: EnlistmentBehavior unavailable");
                ProceedToNextStageFromPay();
                return;
            }

            try
            {
                ModLogger.Info(LogCategory, "Resolving promissory note from muster menu");

                if (_currentMuster != null)
                {
                    _currentMuster.PayOutcome = "promissory";
                    _currentMuster.PayReceived = 0; // No pay now, deferred
                }

                enlistment.ResolvePromissoryMuster();

                ModLogger.Debug(LogCategory, "Promissory note accepted, payment deferred 3 days");
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode(LogCategory, "E-MUSTER-004", "Failed to resolve promissory note", ex);
            }

            ProceedToNextStageFromPay();
        }

        /// <summary>
        /// Processes final discharge (honorable discharge).
        /// Ends service, pays final wages + severance, activates pension.
        /// ResolvePayMusterStandard() detects IsPendingDischarge internally and calls
        /// the private FinalizePendingDischarge() method which handles the full flow.
        /// </summary>
        private void FinalizePendingDischarge()
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null)
            {
                ModLogger.Warn(LogCategory, "Cannot finalize discharge: EnlistmentBehavior unavailable");
                GameMenu.ExitToLast();
                return;
            }

            try
            {
                ModLogger.Info(LogCategory, "Processing final discharge from muster menu");

                if (_currentMuster != null)
                {
                    _currentMuster.PayOutcome = "final_discharge";
                }

                // ResolvePayMusterStandard detects IsPendingDischarge and handles final muster internally
                // This pays final wages + severance, applies relations, and calls StopEnlist
                enlistment.ResolvePayMusterStandard();

                ModLogger.Info(LogCategory, "Final discharge complete, service ended");

                // Service has ended, clear muster state and exit
                RestoreTimeControlOnExit();
                _currentMuster = null;
                GameMenu.ExitToLast();
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode(LogCategory, "E-MUSTER-004", "Failed to finalize discharge", ex);
                RestoreTimeControlOnExit();
                _currentMuster = null;
                GameMenu.ExitToLast();
            }
        }

        /// <summary>
        /// Resolves smuggle discharge (desertion).
        /// Player keeps all gear but loses pension and faces desertion penalties.
        /// Uses DesertArmy() which handles keeping equipped gear and applying penalties.
        /// </summary>
        private void ResolveSmuggleDischarge()
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null)
            {
                ModLogger.Warn(LogCategory, "Cannot resolve smuggle discharge: EnlistmentBehavior unavailable");
                GameMenu.ExitToLast();
                return;
            }

            try
            {
                ModLogger.Info(LogCategory, "Processing smuggle discharge (deserter) from muster menu");

                if (_currentMuster != null)
                {
                    _currentMuster.PayOutcome = "smuggle_desertion";
                }

                // DesertArmy keeps equipped gear and applies desertion penalties
                // Player forfeits pension but keeps all current equipment
                enlistment.DesertArmy();

                ModLogger.Info(LogCategory, "Smuggle discharge complete, service ended as deserter");

                // Service has ended, clear muster state and exit
                RestoreTimeControlOnExit();
                _currentMuster = null;
                GameMenu.ExitToLast();
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode(LogCategory, "E-MUSTER-004", "Failed to resolve smuggle discharge", ex);
                RestoreTimeControlOnExit();
                _currentMuster = null;
                GameMenu.ExitToLast();
            }
        }

        /// <summary>
        /// Proceeds to next stage after pay resolution.
        /// After pay, automatically processes ration exchange (handled by OnMusterCycleComplete),
        /// then moves to baggage check stage. Baggage check will determine if contraband inspection occurs.
        /// </summary>
        private void ProceedToNextStageFromPay()
        {
            // Ration exchange happens automatically in OnMusterCycleComplete (called by payment resolution)
            // No menu stage for ration exchange - it's automatic

            // Move to baggage check stage
            // The baggage check stage will handle 30% trigger and contraband detection in OnBaggageCheckInit
            GameMenu.SwitchToMenu(MusterBaggageMenuId);
        }

        private void ProceedToNextStageFromRecruit()
        {
            if (_currentMuster == null)
            {
                GameMenu.SwitchToMenu(MusterCompleteMenuId);
                return;
            }

            // If promotion occurred this period, show recap
            if (_currentMuster.PromotionOccurredThisPeriod)
            {
                GameMenu.SwitchToMenu(MusterPromotionRecapMenuId);
            }
            else
            {
                ProceedToNextStageFromPromotion();
            }
        }

        private void ProceedToNextStageFromPromotion()
        {
            var enlistment = EnlistmentBehavior.Instance;
            var retinueMgr = RetinueManager.Instance;

            // If T7+ with retinue (or just granted retinue), show retinue muster
            if (enlistment?.EnlistmentTier >= 7 && retinueMgr != null)
            {
                // Show retinue stage even if retinue not formed yet (T7 promotion case)
                // or if retinue exists
                var hasRetinue = retinueMgr.State?.HasRetinue ?? false;
                var justPromotedToT7 = _currentMuster?.CurrentTier == 7 && _currentMuster?.PromotionOccurredThisPeriod == true;

                if (hasRetinue || justPromotedToT7)
                {
                    GameMenu.SwitchToMenu(MusterRetinueMenuId);
                }
                else
                {
                    GameMenu.SwitchToMenu(MusterCompleteMenuId);
                }
            }
            else
            {
                GameMenu.SwitchToMenu(MusterCompleteMenuId);
            }
        }

        private void DeferPayMuster()
        {
            ModLogger.Info(LogCategory, "Muster deferred by player");

            try
            {
                // Call EnlistmentBehavior.DeferPayMuster() to reschedule (retry tomorrow)
                EnlistmentBehavior.Instance?.DeferPayMuster();
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Failed to defer pay muster", ex);
            }

            _currentMuster = null;
            RestoreTimeControlOnExit();

            try
            {
                GameMenu.ExitToLast();
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Failed to exit menu during defer", ex);
            }
        }

        private void CompleteMusterSequence()
        {
            ModLogger.Info(LogCategory, "Muster sequence completing");

            // Capture post-muster flags before clearing state
            var visitQM = _currentMuster?.VisitQMAfter ?? false;
            var recruitRetinue = _currentMuster?.RecruitRetinueAfter ?? false;
            var requestLeave = _currentMuster?.RequestLeaveAfter ?? false;
            var effectsPartiallyFailed = _currentMuster?.EffectsPartiallyFailed ?? false;
            var pendingEscalationEvents = _currentMuster?.PendingEscalationEvents?.ToList() ?? new List<string>();
            var dischargeThresholdReached = _currentMuster?.DischargeThresholdReached ?? false;
            var encounteredErrors = _currentMuster?.EncounteredErrors?.ToList() ?? new List<string>();

            try
            {
                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment != null)
                {
                    // Create MusterOutcomeRecord for news feed BEFORE OnMusterComplete resets tracking
                    if (_currentMuster != null)
                    {
                        SafeApplyEffect("CreateMusterRecord", () => CreateMusterOutcomeRecord(enlistment));
                    }

                    // Mark muster complete (updates tier/XP tracking, resets XP sources)
                    SafeApplyEffect("MusterComplete", () => enlistment.OnMusterComplete());

                    // Refresh quartermaster stock (with null safety)
                    SafeApplyEffect("QMStockRefresh", () =>
                    {
                        QuartermasterManager.Instance?.RollStockAvailability();
                        ModLogger.Debug(LogCategory, "QM stock availability refreshed");
                    });

                    // Mark newly unlocked items in case promotion occurred this period
                    SafeApplyEffect("QMUnlockedItems", () =>
                    {
                        QuartermasterManager.Instance?.UpdateNewlyUnlockedItems();
                    });
                }

                RestoreTimeControlOnExit();

                // Clear muster state before exiting
                _currentMuster = null;

                GameMenu.ExitToLast();

                // Show warning if some effects failed to apply
                if (effectsPartiallyFailed)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        "Some muster effects may not have applied correctly.", Colors.Yellow));
                }

                // Log any encountered errors
                if (encounteredErrors.Count > 0)
                {
                    ModLogger.Warn(LogCategory, $"Muster completed with {encounteredErrors.Count} errors: {string.Join(", ", encounteredErrors)}");
                }

                // Handle post-muster transitions after menu exits
                if (visitQM)
                {
                    ModLogger.Debug(LogCategory, "Post-muster: opening QM conversation");
                    SafeOpenQuartermaster();
                }
                else if (recruitRetinue)
                {
                    ModLogger.Debug(LogCategory, "Post-muster: opening retinue recruitment");
                    // TODO: Open retinue recruitment screen
                }
                else if (requestLeave)
                {
                    ModLogger.Debug(LogCategory, "Post-muster: requesting temporary leave");
                    // TODO: Call TemporaryLeaveManager.RequestLeave()
                }

                // Queue escalation events that were triggered during muster
                if (pendingEscalationEvents.Count > 0)
                {
                    ModLogger.Info(LogCategory, $"Queuing {pendingEscalationEvents.Count} escalation events for post-muster");
                    // Events will fire on next hourly tick via EscalationManager
                }

                // Handle discharge threshold reached during muster
                if (dischargeThresholdReached)
                {
                    ModLogger.Info(LogCategory, "Discharge threshold reached during muster, queuing discharge event");
                    InformationManager.DisplayMessage(new InformationMessage(
                        "Your conduct has been noted. Expect a summons from command.", Colors.Red));
                }
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode(LogCategory, "E-MUSTER-002", "Failed to complete muster sequence", ex);
                _currentMuster = null;
                RestoreTimeControlOnExit();
                try { GameMenu.ExitToLast(); } catch { /* Ensure we don't get stuck */ }
            }
        }

        /// <summary>
        /// Safely opens QM conversation with error handling.
        /// </summary>
        private void SafeOpenQuartermaster()
        {
            try
            {
                // Check supply level before opening
                var supply = CompanySupplyManager.Instance?.TotalSupply ?? 100;
                if (supply < 15)
                {
                    ModLogger.Warn(LogCategory, "QM blocked due to critical supply shortage");
                    InformationManager.DisplayMessage(new InformationMessage(
                        "Quartermaster unavailable due to critical supply shortage.", Colors.Yellow));
                    return;
                }

                OpenQuartermasterConversation();
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Failed to open QM conversation", ex);
                InformationManager.DisplayMessage(new InformationMessage(
                    "Quartermaster unavailable.", Colors.Yellow));
            }
        }

        /// <summary>
        /// Queues an escalation event to fire after muster completes.
        /// </summary>
        public void QueueEscalationEventForAfterMuster(string eventId)
        {
            if (_currentMuster == null)
            {
                ModLogger.Warn(LogCategory, $"Cannot queue escalation event {eventId}: no muster in progress");
                return;
            }

            _currentMuster.PendingEscalationEvents.Add(eventId);
            ModLogger.Debug(LogCategory, $"Queued escalation event for after muster: {eventId}");
        }

        /// <summary>
        /// Marks that the discharge threshold was reached during muster.
        /// The actual discharge event will fire after muster completes.
        /// </summary>
        public void MarkDischargeThresholdReached()
        {
            if (_currentMuster == null)
            {
                return;
            }
            _currentMuster.DischargeThresholdReached = true;
            ModLogger.Info(LogCategory, "Discharge threshold marked during muster");
        }

        private void CreateMusterOutcomeRecord(EnlistmentBehavior enlistment)
        {
            try
            {
                var newsBehavior = EnlistedNewsBehavior.Instance;
                if (newsBehavior == null)
                {
                    ModLogger.Warn(LogCategory, "Cannot create muster outcome record: EnlistedNewsBehavior unavailable");
                    return;
                }

                var record = new Interface.Behaviors.MusterOutcomeRecord
                {
                    DayNumber = _currentMuster.MusterDay,
                    StrategicContext = _currentMuster.StrategicContext ?? string.Empty,
                    PayOutcome = _currentMuster.PayOutcome ?? "unknown",
                    PayAmount = _currentMuster.PayReceived,
                    RationOutcome = _currentMuster.RationOutcome ?? "unknown",
                    RationItemId = string.Empty, // Not tracked in current muster state
                    QmReputation = _currentMuster.QMRep,
                    SupplyLevel = CompanySupplyManager.Instance?.TotalSupply ?? 0,
                    LostSinceLast = 0, // Not tracked in current muster state
                    SickSinceLast = 0, // Not tracked in current muster state
                    OrdersCompleted = _currentMuster.OrdersCompleted,
                    OrdersFailed = _currentMuster.OrdersFailed,
                    BaggageOutcome = _currentMuster.BaggageOutcome ?? "not_conducted",
                    InspectionOutcome = _currentMuster.InspectionOutcome ?? "skipped",
                    RecruitOutcome = _currentMuster.RecruitOutcome ?? "skipped",
                    PromotionTier = _currentMuster.PromotionOccurredThisPeriod ? _currentMuster.CurrentTier : 0,
                    RetinueStrength = _currentMuster.RetinueStrength,
                    RetinueCasualties = _currentMuster.RetinueCasualties,
                    FallenRetinueNames = _currentMuster.FallenRetinueNames ?? new List<string>(),
                    RationFlavorText = string.Empty // Not currently used
                };

                newsBehavior.AddMusterOutcome(record);
                ModLogger.Info(LogCategory, $"Muster outcome recorded: pay={record.PayOutcome}:{record.PayAmount}, supply={record.SupplyLevel}%");
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Failed to create muster outcome record", ex);
            }
        }

        #endregion

        #region Time Control

        /// <summary>
        /// Initialize time control behavior for muster menus based on user settings.
        /// Called once at the start of the muster sequence (intro stage).
        /// </summary>
        private void InitializeMusterTimeControl(MenuCallbackArgs args)
        {
            var settings = ModConfig.Settings;

            if (settings?.PauseGameDuringMuster == true)
            {
                // Pause mode: force stop and lock controls
                // Player cannot advance time during muster
                if (Campaign.Current != null)
                {
                    Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
                    Campaign.Current.SetTimeControlModeLock(true);
                }
                ModLogger.Debug(LogCategory, "Time control: Paused (locked)");
            }
            else
            {
                // Time-preserving mode: respect player's current speed
                // Player can advance time during muster using speed controls
                QuartermasterManager.CaptureTimeStateBeforeMenuActivation();
                args.MenuContext?.GameMenu?.StartWait();

                if (Campaign.Current != null)
                {
                    Campaign.Current.SetTimeControlModeLock(false);

                    var captured = QuartermasterManager.CapturedTimeMode ?? Campaign.Current.TimeControlMode;
                    var normalized = QuartermasterManager.NormalizeToStoppable(captured);
                    Campaign.Current.TimeControlMode = normalized;
                }
                ModLogger.Debug(LogCategory, $"Time control: Preserved ({QuartermasterManager.CapturedTimeMode})");
            }
        }

        /// <summary>
        /// Restore previous time control state when exiting muster.
        /// Called in CompleteMusterSequence() before exiting.
        /// </summary>
        private void RestoreTimeControlOnExit()
        {
            var settings = ModConfig.Settings;

            if (settings?.PauseGameDuringMuster != true && Campaign.Current != null)
            {
                // Time-preserving mode: restore captured time state
                if (QuartermasterManager.CapturedTimeMode.HasValue)
                {
                    var normalized = QuartermasterManager.NormalizeToStoppable(QuartermasterManager.CapturedTimeMode.Value);
                    Campaign.Current.TimeControlMode = normalized;
                    ModLogger.Debug(LogCategory, $"Time control restored: {normalized}");
                }

                // Clear captured state from QuartermasterManager
                QuartermasterManager.CapturedTimeMode = null;
            }
            else if (settings?.PauseGameDuringMuster == true && Campaign.Current != null)
            {
                // Pause mode: unlock time controls
                Campaign.Current.SetTimeControlModeLock(false);
                ModLogger.Debug(LogCategory, "Time control unlocked on muster exit");
            }
        }

        #endregion

        #region Text Builders

        private string BuildIntroText()
        {
            if (_currentMuster == null)
            {
                return "Muster session unavailable.";
            }

            var sb = new StringBuilder();
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null)
            {
                return "Enlistment data unavailable.";
            }

            // Strategic context flavor text
            var flavorText = BuildStrategicContextFlavor();
            sb.AppendLine(flavorText);
            sb.AppendLine();

            // Service record section
            sb.AppendLine(new TextObject("{=muster_section_service}_____ YOUR SERVICE RECORD _____").ToString());
            sb.AppendLine();
            sb.Append(BuildServiceRecord());
            sb.AppendLine();

            // Orders summary section
            var ordersText = BuildOrdersSummary();
            if (!string.IsNullOrEmpty(ordersText))
            {
                sb.AppendLine(new TextObject("{=muster_section_orders}_____ ORDERS THIS PERIOD _____").ToString());
                sb.AppendLine();
                sb.Append(ordersText);
                sb.AppendLine();
            }

            // Events since last muster section
            var eventsText = BuildPeriodSummary();
            if (!string.IsNullOrEmpty(eventsText))
            {
                sb.AppendLine(new TextObject("{=muster_section_events}_____ EVENTS SINCE LAST MUSTER _____").ToString());
                sb.AppendLine();
                sb.Append(eventsText);
                sb.AppendLine();
            }

            // Status line
            sb.Append(BuildStatusLine());

            return sb.ToString();
        }

        private string BuildStrategicContextFlavor()
        {
            var context = _currentMuster?.StrategicContext ?? "patrol_peacetime";
            var flavorText = GetStrategicContextFlavor(context);

            // Add army name if available
            var enlistment = EnlistmentBehavior.Instance;
            var lordParty = enlistment?.EnlistedLord?.PartyBelongedTo;
            if (lordParty?.Army != null)
            {
                var armyName = lordParty.Army.Name?.ToString();
                if (!string.IsNullOrEmpty(armyName))
                {
                    return $"{armyName}\n\n{flavorText}";
                }
            }

            return flavorText;
        }

        private string BuildServiceRecord()
        {
            var sb = new StringBuilder();
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null)
            {
                return "Service data unavailable.";
            }

            // Rank and tier
            var tier = enlistment.EnlistmentTier;
            var rankName = GetRankName(tier);
            sb.AppendLine($"Rank: {rankName} (Tier {tier})");

            // Days served
            var daysServed = enlistment.DaysServed;
            sb.AppendLine($"Days Served This Term: {daysServed} days");

            // XP progress
            var currentXP = enlistment.EnlistmentXP;
            var xpNeeded = GetXPRequiredForNextTier(tier);
            if (xpNeeded > 0)
            {
                var percentage = (int)((currentXP / (float)xpNeeded) * 100);
                var nextRankName = GetRankName(tier + 1);
                sb.AppendLine($"Experience: {currentXP:N0} / {xpNeeded:N0} XP ({percentage}% to {nextRankName})");
            }
            else
            {
                // Max tier reached
                sb.AppendLine($"Experience: {currentXP:N0} XP (Maximum rank achieved)");
            }

            // Health status
            var healthText = BuildHealthStatus();
            if (!string.IsNullOrEmpty(healthText))
            {
                sb.AppendLine(healthText);
            }

            // Baggage access
            var baggageAccess = BaggageTrainManager.Instance?.GetCurrentAccess() ?? BaggageAccessState.FullAccess;
            sb.AppendLine($"Baggage Access: {GetBaggageAccessText(baggageAccess)} (muster window)");

            // Period XP gain
            var periodXP = currentXP - enlistment.XPAtLastMuster;
            if (periodXP > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"This Period: +{periodXP} XP (battles, training, orders completed)");
            }

            // Fatigue restoration
            var fatigueRestored = enlistment.FatigueCurrent - _currentMuster.FatigueBeforeMuster;
            if (fatigueRestored > 0)
            {
                sb.AppendLine($"Fatigue: Restored to full (muster rest day)");
            }
            else if (_currentMuster.FatigueBeforeMuster >= enlistment.FatigueMax)
            {
                sb.AppendLine("Fatigue: Already well-rested");
            }

            return sb.ToString();
        }

        private string BuildHealthStatus()
        {
            var hero = Hero.MainHero;
            if (hero == null)
            {
                return string.Empty;
            }

            var healthPercent = (int)((hero.HitPoints / (float)hero.MaxHitPoints) * 100);

            if (healthPercent >= 100)
            {
                return "Health Status: Fit for duty";
            }
            else if (healthPercent >= 50)
            {
                return $"Health Status: Wounded ({healthPercent}%) - Light duties";
            }
            else if (healthPercent >= 30)
            {
                return $"Health Status: Badly Wounded ({healthPercent}%) - Limited duties";
            }
            else
            {
                return $"Health Status: Critically Wounded ({healthPercent}%) - Excused from inspection";
            }
        }

        private string BuildOrdersSummary()
        {
            var newsBehavior = Interface.Behaviors.EnlistedNewsBehavior.Instance;
            if (newsBehavior == null)
            {
                return "Order records unavailable.";
            }

            var lastMusterDay = _currentMuster?.LastMusterDay ?? 0;
            if (lastMusterDay == 0)
            {
                return "No orders issued this period.";
            }

            var orders = newsBehavior.GetOrderOutcomesSince(lastMusterDay);
            if (orders == null || orders.Count == 0)
            {
                return "No orders issued this period.";
            }

            var sb = new StringBuilder();
            var completed = 0;
            var failed = 0;

            foreach (var order in orders.Take(5)) // Show max 5 orders
            {
                if (order.Success)
                {
                    completed++;
                    var xpGain = order.BriefSummary?.Contains("+") == true ? " " + ExtractXPFromSummary(order.BriefSummary) : "";
                    sb.AppendLine($"• {order.OrderTitle}: Completed{xpGain}");
                }
                else
                {
                    failed++;
                    sb.AppendLine($"• {order.OrderTitle}: Failed");
                }
            }

            sb.AppendLine();
            sb.AppendLine($"Orders: {completed} completed, {failed} failed");

            // Store counts in session state
            if (_currentMuster != null)
            {
                _currentMuster.OrdersCompleted = completed;
                _currentMuster.OrdersFailed = failed;
            }

            return sb.ToString();
        }

        private string BuildPeriodSummary()
        {
            var newsBehavior = Interface.Behaviors.EnlistedNewsBehavior.Instance;
            if (newsBehavior == null)
            {
                return "Event records unavailable.";
            }

            var lastMusterDay = _currentMuster?.LastMusterDay ?? 0;
            var label = lastMusterDay > 0 ? "Since Last Muster" : "Since Enlistment";

            var feedItems = newsBehavior.GetPersonalFeedSince(lastMusterDay);
            if (feedItems == null || feedItems.Count == 0)
            {
                return "A quiet period. Nothing of note occurred.";
            }

            var sb = new StringBuilder();
            var count = 0;
            var maxEvents = 8;

            foreach (var item in feedItems)
            {
                if (count >= maxEvents)
                {
                    break;
                }

                var headline = FormatFeedItemForMuster(item);
                if (!string.IsNullOrEmpty(headline))
                {
                    // Truncate very long headlines
                    if (headline.Length > 80)
                    {
                        headline = headline.Substring(0, 77) + "...";
                    }

                    sb.AppendLine($"• Day {item.DayCreated}: {headline}");
                    count++;
                }
            }

            if (count == 0)
            {
                return "A quiet period. Nothing of note occurred.";
            }

            return sb.ToString();
        }

        private string BuildStatusLine()
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null)
            {
                return string.Empty;
            }

            var pay = enlistment.PendingMusterPay;
            var supply = CompanySupplyManager.Instance?.TotalSupply ?? 100;

            // Unit status (simplified for now - could be enhanced with actual casualty tracking)
            var battles = CountBattlesThisPeriod();

            return $"[Pay: {pay:N0} denars | Supply: {supply}% | Battles: {battles}]";
        }

        private string BuildPayText()
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null)
            {
                return "Enlistment data unavailable.";
            }

            var sb = new StringBuilder();
            sb.AppendLine(new TextObject("{=muster_pay_intro}You step forward to the paymaster's table. He opens his ledger.").ToString());
            sb.AppendLine();
            sb.AppendLine(new TextObject("{=muster_section_pay}_____ PAY STATUS _____").ToString());
            sb.AppendLine();

            // Wages owed
            var wages = enlistment.PendingMusterPay;
            sb.AppendLine($"Wages Owed:           {wages:N0} denars");

            // Backpay outstanding
            var backpay = enlistment.OwedBackpay;
            sb.AppendLine($"Backpay Outstanding:  {backpay:N0} denars");

            // Lord's treasury status
            var lordGold = enlistment.EnlistedLord?.Gold ?? 0;
            var wealthStatus = GetLordWealthStatus(lordGold);
            sb.AppendLine($"Lord's Treasury:      {wealthStatus}");

            // Special status indicators
            if (enlistment.IsOnProbation)
            {
                sb.AppendLine();
                sb.AppendLine("⚠️  [Probation Rate] - Wages reduced 50%");
            }

            if (enlistment.EnlistedLord?.PartyBelongedTo?.Army != null)
            {
                sb.AppendLine();
                sb.AppendLine("⚔️  [Army Bonus] - Wages +20%");
            }

            if (enlistment.PayTension >= 100)
            {
                sb.AppendLine();
                sb.AppendLine("⚠️  MUTINY RISK - Pay tension at maximum!");
            }
            else if (enlistment.PayTension >= 60)
            {
                sb.AppendLine();
                sb.AppendLine($"⚠️  Pay Tension: {enlistment.PayTension}/100 (High)");
            }

            // Pending discharge note
            if (enlistment.IsPendingDischarge)
            {
                sb.AppendLine();
                sb.AppendLine("📜  Final Muster - Discharge processing");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets human-readable wealth status text for the lord's treasury.
        /// Thresholds match pay-system.md spec: Wealthy >50k, Comfortable >20k,
        /// Struggling >5k, Poor >1k, Broke ≤1k.
        /// </summary>
        private string GetLordWealthStatus(int lordGold)
        {
            if (lordGold > 50000)
            {
                return "Wealthy";
            }
            if (lordGold > 20000)
            {
                return "Comfortable";
            }
            if (lordGold > 5000)
            {
                return "Struggling";
            }
            if (lordGold > 1000)
            {
                return "Poor";
            }
            return "Broke";
        }

        /// <summary>
        /// Processes the baggage check during muster init.
        /// 30% chance to trigger. If contraband found and QM rep < 65, shows options.
        /// Otherwise skips to next stage automatically.
        /// </summary>
        private void ProcessBaggageCheck()
        {
            if (_currentMuster == null)
            {
                return;
            }

            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null)
            {
                _currentMuster.BaggageOutcome = "skipped";
                return;
            }

            // 30% chance to trigger check
            if (MBRandom.RandomFloat > 0.30f)
            {
                _currentMuster.BaggageOutcome = "not_conducted";
                ModLogger.Debug(LogCategory, "Baggage check not triggered (70% roll)");
                return;
            }

            // Scan inventory for contraband
            var playerTier = enlistment.EnlistmentTier;
            var playerRole = EnlistedStatusManager.Instance?.GetPrimaryRole() ?? "Soldier";

            var contrabandResult = ContrabandChecker.ScanInventory(playerTier, playerRole);

            if (!contrabandResult.HasContraband)
            {
                _currentMuster.BaggageOutcome = "clean";
                _currentMuster.ContrabandFound = false;
                ModLogger.Debug(LogCategory, "Baggage check triggered but no contraband found");
                return;
            }

            // Contraband found - check QM reputation
            var qmRep = enlistment.GetQMReputation();
            _currentMuster.QMRep = qmRep;
            _currentMuster.ContrabandFound = true;

            // QM rep 65+ auto-passes (looks away)
            if (qmRep >= 65)
            {
                _currentMuster.BaggageOutcome = "qm_favor";
                ModLogger.Info(LogCategory, $"Contraband found but QM rep {qmRep} >= 65, auto-pass");
                return;
            }

            // Contraband found and QM will confront - store for display
            ModLogger.Info(LogCategory, $"Contraband found: {contrabandResult.MostValuable.Item.Name}, QM rep: {qmRep}");
        }

        private string BuildBaggageText()
        {
            if (_currentMuster == null)
            {
                return "[No baggage check data]";
            }

            var sb = new StringBuilder();

            // Handle skip conditions
            if (_currentMuster.BaggageOutcome == "not_conducted")
            {
                sb.AppendLine("The quartermaster waves you through without inspection.");
                sb.AppendLine();
                sb.AppendLine("'Move along, soldier. We haven't got all day.'");
                sb.AppendLine();
                sb.AppendLine("You're free to proceed.");
                return sb.ToString();
            }

            if (_currentMuster.BaggageOutcome == "clean")
            {
                sb.AppendLine("The quartermaster checks your kit methodically.");
                sb.AppendLine();
                sb.AppendLine("He nods curtly. 'Everything in order. Carry on.'");
                sb.AppendLine();
                sb.AppendLine("Your belongings are returned.");
                return sb.ToString();
            }

            if (_currentMuster.BaggageOutcome == "qm_favor")
            {
                sb.AppendLine("The quartermaster's hand pauses over something in your pack.");
                sb.AppendLine();
                sb.AppendLine("He meets your eye for a moment, then pushes your kit back across the table.");
                sb.AppendLine();
                sb.AppendLine("'I didn't see anything. Next!'");
                sb.AppendLine();
                sb.AppendLine($"[QM Reputation: {_currentMuster.QMRep} - Trusted]");
                return sb.ToString();
            }

            // Contraband found - build confrontation text
            if (_currentMuster.ContrabandFound)
            {
                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment == null)
                {
                    return "Enlistment data unavailable.";
                }

                var playerTier = enlistment.EnlistmentTier;
                var playerRole = EnlistedStatusManager.Instance?.GetPrimaryRole() ?? "Soldier";
                var contrabandResult = ContrabandChecker.ScanInventory(playerTier, playerRole);

                if (contrabandResult.HasContraband)
                {
                    var contraband = contrabandResult.MostValuable;

                    sb.AppendLine("The quartermaster's hand stops in your pack. He pulls out an item");
                    sb.AppendLine("and raises an eyebrow.");
                    sb.AppendLine();
                    sb.AppendLine($"Found: {contraband.Item.Name} (value {contraband.Value} denars)");
                    sb.AppendLine();

                    if (_currentMuster.QMRep >= 35)
                    {
                        sb.AppendLine("\"This doesn't belong to a soldier of your rank,\" he says quietly.");
                        sb.AppendLine("\"I can overlook it... for a price. Or we can do this by the book.\"");
                    }
                    else
                    {
                        sb.AppendLine("\"Contraband,\" he says flatly. \"You know the rules.\"");
                        sb.AppendLine("\"Hand it over. Now.\"");
                    }

                    sb.AppendLine();
                    sb.AppendLine($"[QM Reputation: {_currentMuster.QMRep} - {GetReputationLabel(_currentMuster.QMRep)}]");

                    return sb.ToString();
                }
            }

            // Outcomes from previous choices
            if (!string.IsNullOrEmpty(_currentMuster.BaggageOutcome))
            {
                return GetBaggageOutcomeText(_currentMuster.BaggageOutcome);
            }

            return "Baggage inspection in progress...";
        }

        private string GetReputationLabel(int rep)
        {
            if (rep >= 65)
            {
                return "Trusted";
            }
            if (rep >= 35)
            {
                return "Neutral";
            }
            if (rep >= 0)
            {
                return "Wary";
            }
            return "Hostile";
        }

        private string GetBaggageOutcomeText(string outcome)
        {
            return outcome switch
            {
                "bribed" => "The quartermaster pockets the coin and pushes your pack back.\n\n\"I didn't see anything. Move along.\"",
                "smuggled" => "While he's distracted, you palm the item and slip it back into your pack.\n\nHe finishes his inspection without noticing. Close call.",
                "confiscated" => "He takes the item and drops it into a chest behind him.\n\n\"Don't let it happen again.\"",
                "protested" => "You argued your case. He wasn't impressed.",
                "bribe_failed" => "He takes your gold... then confiscates the item anyway.\n\n\"Nice try. Next!\"",
                "smuggle_failed" => "His hand shoots out and catches yours.\n\n\"Did you think I wouldn't notice? Confiscated. And you just earned yourself extra scrutiny.\"",
                _ => "Baggage check complete."
            };
        }

        private string BuildInspectionText()
        {
            if (_currentMuster == null)
            {
                return "[No inspection data]";
            }

            // Check for skip conditions
            if (_currentMuster.InspectionOutcome == "skipped_cooldown")
            {
                return "The captain walks the inspection line but doesn't single you out today.\n\n'Carry on,' he says, moving past.\n\nYou were inspected recently. Next inspection will occur in a few days.";
            }

            if (_currentMuster.InspectionOutcome == "excused_wounded")
            {
                return "You fall in with the others, but the sergeant pulls you aside.\n\n'You're excused from inspection today. Get yourself healed up.'\n\nYour wounds excuse you from formal inspection.";
            }

            // Active inspection
            var sb = new StringBuilder();
            sb.AppendLine("The company forms ranks for morning inspection. The captain walks");
            sb.AppendLine("the line, inspecting each soldier's equipment and bearing.");
            sb.AppendLine();
            sb.AppendLine("He stops in front of you, looking you up and down with a critical eye.");
            sb.AppendLine();

            // Show skill status
            var skill = GetRelevantMeleeSkill();
            var skillValue = Hero.MainHero?.GetSkillValue(skill) ?? 0;
            sb.AppendLine($"Your {skill.Name} skill: {skillValue}");

            return sb.ToString();
        }

        private string BuildRecruitText()
        {
            if (_currentMuster == null)
            {
                return "[No recruit data]";
            }

            // Check for skip conditions
            if (_currentMuster.RecruitOutcome == "skipped_tier")
            {
                return "A new recruit joins the company at morning muster. The veterans gather around,\noffering advice and hazing in equal measure.\n\nYou're still too junior to take a recruit under your wing. Leave it to the sergeants.";
            }

            if (_currentMuster.RecruitOutcome == "skipped_cooldown")
            {
                return "The company welcomes the new recruits at morning formation.\n\nYou've mentored recently. The sergeants can handle this batch.";
            }

            // Active recruit event
            var sb = new StringBuilder();
            sb.AppendLine("At morning muster, a terrified-looking recruit stands trembling in");
            sb.AppendLine("the front rank. He's barely holding his spear correctly, and his eyes");
            sb.AppendLine("dart nervously at every sound.");
            sb.AppendLine();
            sb.AppendLine("The veterans are already snickering. As a seasoned soldier, you could");
            sb.AppendLine("step in—or let him learn the hard way.");
            sb.AppendLine();

            // Show Leadership status
            var leadership = Hero.MainHero?.GetSkillValue(DefaultSkills.Leadership) ?? 0;
            sb.AppendLine($"Your Leadership skill: {leadership}");

            return sb.ToString();
        }

        private string BuildInspectionResultText(string outcome)
        {
            return outcome switch
            {
                "perfect" => "Your equipment gleams, your stance is flawless. The captain gives a curt nod of approval.\n\n'This is the standard,' he tells the others.\n\nYour fellow soldiers take note, and the officers remember your discipline.",
                "basic" => "You're presentable enough. The captain moves on without comment.\n\nYou've met expectations—nothing more, nothing less.",
                "failed" => "Your equipment is dirty, your bearing slovenly. The captain's face darkens.\n\n'Five laps around camp with full pack,' he orders. 'Then report to the\nquartermaster for extra duties.'\n\nThe other soldiers avoid your gaze as you fall out of formation.",
                _ => "Inspection complete."
            };
        }

        private string BuildRecruitResultText(string outcome)
        {
            return outcome switch
            {
                "mentored" => "You pull the recruit aside after formation and spend the morning drilling him.\n\nBy midday, he's holding his spear properly and his eyes have steadied.\nThe other soldiers notice, and the officers nod approvingly.",
                "ignored" => "You turn away as the veterans close in, laughing.\n\nThe recruit's on his own. The other soldiers notice your indifference.",
                "hazed" => "You join the veterans in the traditional welcome. The recruit learns quickly\nthat this life isn't gentle.\n\nThe men laugh and clap you on the back. The officers pretend not to notice.",
                _ => "Recruit processing complete."
            };
        }

        private string BuildPromotionText()
        {
            if (_currentMuster == null)
            {
                return "[No promotion data]";
            }

            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null)
            {
                return "[Enlistment data unavailable]";
            }

            var sb = new StringBuilder();

            // Formal announcement
            var prevRank = GetRankName(_currentMuster.PreviousTier);
            var currentRank = GetRankName(_currentMuster.CurrentTier);
            var daysSincePromotion = _currentMuster.MusterDay - _currentMuster.PromotionDay;

            sb.AppendLine("The captain addresses the formation. Your promotion is formally");
            sb.AppendLine("recognized before the assembled company.");
            sb.AppendLine();

            // Promotion announcement text
            if (daysSincePromotion == 0)
            {
                sb.AppendLine($"\"Earlier today, this soldier was promoted to {currentRank}");
                sb.AppendLine("for distinguished service. The rank has been earned and noted.\"");
            }
            else if (daysSincePromotion == 1)
            {
                sb.AppendLine($"\"Yesterday, this soldier was promoted to {currentRank}");
                sb.AppendLine("for distinguished service. The rank has been earned and noted.\"");
            }
            else
            {
                sb.AppendLine($"\"On Day {_currentMuster.PromotionDay} of this campaign, this soldier was promoted to {currentRank}");
                sb.AppendLine("for distinguished service. The rank has been earned and noted.\"");
            }

            sb.AppendLine();
            sb.AppendLine("The men salute. Your promotion is now part of the official record.");
            sb.AppendLine();
            sb.AppendLine("_____ PROMOTION DETAILS _____");
            sb.AppendLine();

            // Promotion details
            sb.AppendLine($"Promoted From:       {prevRank} (Tier {_currentMuster.PreviousTier})");
            sb.AppendLine($"Current Rank:        {currentRank} (Tier {_currentMuster.CurrentTier})");

            if (daysSincePromotion == 0)
            {
                sb.AppendLine($"Date of Promotion:   Day {_currentMuster.PromotionDay} (earlier today)");
            }
            else
            {
                sb.AppendLine($"Date of Promotion:   Day {_currentMuster.PromotionDay} ({daysSincePromotion} days ago)");
            }

            sb.AppendLine();

            // Benefits unlocked at promotion
            sb.AppendLine("Benefits Unlocked at Promotion:");

            // Wage increase
            var prevWage = CalculateTierWage(_currentMuster.PreviousTier);
            var currentWage = CalculateTierWage(_currentMuster.CurrentTier);
            sb.AppendLine($"• Wages increased: {prevWage} → {currentWage} denars/day");

            // Equipment tier access
            sb.AppendLine($"• Tier {_currentMuster.CurrentTier} equipment available from Quartermaster");

            // Authority changes
            var authority = GetTierAuthority(_currentMuster.CurrentTier);
            if (!string.IsNullOrEmpty(authority))
            {
                sb.AppendLine($"• New authority: {authority}");
            }

            // Enhanced reputation gains (T4+)
            if (_currentMuster.CurrentTier >= 4 && _currentMuster.PreviousTier < 4)
            {
                sb.AppendLine("• Enhanced reputation gains from successful orders");
            }

            // Special benefits for specific tiers
            if (_currentMuster.CurrentTier >= 7)
            {
                sb.AppendLine("• Officer rations (exempt from standard ration exchange)");
                sb.AppendLine("• Daily baggage access during campaign");
            }

            if (_currentMuster.CurrentTier == 7)
            {
                sb.AppendLine($"• Commander's retinue granted: {RetinueManager.CommanderCapacity1} soldiers");
            }
            else if (_currentMuster.CurrentTier == 8)
            {
                sb.AppendLine($"• Retinue capacity expanded to {RetinueManager.CommanderCapacity2} soldiers");
            }
            else if (_currentMuster.CurrentTier == 9)
            {
                sb.AppendLine($"• Retinue capacity expanded to {RetinueManager.CommanderCapacity3} soldiers");
            }

            sb.AppendLine();

            // New abilities unlocked (camp decisions, etc.)
            // For multi-tier promotions (e.g., T5→T7), show abilities from all gained tiers
            var abilities = new List<string>();
            for (int tier = _currentMuster.PreviousTier + 1; tier <= _currentMuster.CurrentTier; tier++)
            {
                abilities.AddRange(GetTierAbilities(tier));
            }

            if (abilities.Count > 0)
            {
                sb.AppendLine("New Abilities Unlocked:");
                foreach (var ability in abilities)
                {
                    sb.AppendLine($"• {ability}");
                }
                sb.AppendLine();
            }

            // Current XP progress to next rank
            // XP thresholds are cumulative totals, so we need to calculate progress within current tier
            var currentXP = enlistment.EnlistmentXP;
            var currentTierThreshold = GetXPThresholdForTier(_currentMuster.CurrentTier);
            var nextTierThreshold = GetXPThresholdForTier(_currentMuster.CurrentTier + 1);

            if (nextTierThreshold > 0)
            {
                // XP progress = total XP minus current tier threshold
                var xpProgress = currentXP - currentTierThreshold;
                // XP needed for next tier = difference between thresholds
                var xpToNext = nextTierThreshold - currentTierThreshold;
                var percentage = xpToNext > 0 ? (int)((xpProgress / (float)xpToNext) * 100) : 0;
                var nextRank = GetRankName(_currentMuster.CurrentTier + 1);
                sb.AppendLine($"Current XP Progress: {xpProgress:N0} / {xpToNext:N0} XP to {nextRank} ({percentage}%)");
            }
            else
            {
                // Max tier reached
                sb.AppendLine("Current XP Progress: Maximum rank achieved");
                sb.AppendLine();
                sb.AppendLine("You have reached the pinnacle of military service. No further");
                sb.AppendLine("promotion is possible—your place in history is secured.");
            }

            sb.AppendLine();
            sb.AppendLine("The promotion proving event occurred when you earned it. This is a");
            sb.AppendLine("formal acknowledgment for your service record.");

            // Reminder about visiting QM if new equipment available
            if (_currentMuster.CurrentTier < 9) // Not at max tier
            {
                sb.AppendLine();
                sb.AppendLine("Visit the Quartermaster to review newly unlocked equipment.");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Calculates approximate daily wage for a given tier (simplified for display).
        /// </summary>
        private int CalculateTierWage(int tier)
        {
            // Simplified wage calculation: base 10 + 5 per tier
            // This matches the formula in wage_system config
            return 10 + (tier * 5);
        }

        /// <summary>
        /// Gets the authority description for a given tier.
        /// </summary>
        private string GetTierAuthority(int tier)
        {
            return tier switch
            {
                2 => "Select formation assignment",
                3 => "Request extra training sessions",
                4 => "Lead small patrols",
                5 => "Organize squad drills, recommend promotions",
                6 => "Act as NCO, train recruits",
                7 => "Command retinue, halt column for baggage access",
                8 => "Lead independent operations",
                9 => "Strategic planning, major tactical decisions",
                _ => null
            };
        }

        /// <summary>
        /// Gets the list of abilities unlocked at a given tier.
        /// </summary>
        private List<string> GetTierAbilities(int tier)
        {
            var abilities = new List<string>();

            switch (tier)
            {
                case 2:
                    abilities.Add("Formation selection (permanent choice)");
                    break;
                case 3:
                    abilities.Add("Request Extra Training (camp decision)");
                    abilities.Add("Eligible for Green Recruit mentoring events");
                    break;
                case 4:
                    abilities.Add("Organize Squad Drill (camp decision)");
                    abilities.Add("Lead patrol orders");
                    break;
                case 5:
                    abilities.Add("Recommend Comrades for Promotion (social decision)");
                    abilities.Add("Daily baggage access window");
                    break;
                case 6:
                    abilities.Add("Train New Recruits (camp decision)");
                    abilities.Add("Enhanced order authority");
                    break;
                case 7:
                    abilities.Add("Command personal retinue of 20 soldiers");
                    abilities.Add("Halt column for baggage access");
                    abilities.Add("Request emergency supplies");
                    break;
                case 8:
                    abilities.Add("Expanded retinue (30 soldiers)");
                    abilities.Add("Request lord reinforcements");
                    break;
                case 9:
                    abilities.Add("Elite retinue (40 soldiers)");
                    abilities.Add("Influence strategic decisions");
                    break;
            }

            return abilities;
        }

        private string BuildRetinueText()
        {
            var sb = new StringBuilder();

            var retinueMgr = RetinueManager.Instance;
            var enlistment = EnlistmentBehavior.Instance;

            if (retinueMgr == null || enlistment == null)
            {
                sb.AppendLine("[Retinue system unavailable]");
                ModLogger.Warn(LogCategory, "BuildRetinueText: RetinueManager or EnlistmentBehavior unavailable");
                return sb.ToString();
            }

            var state = retinueMgr.State;
            if (state == null || !state.HasRetinue)
            {
                // Edge case: Just promoted to T7 but haven't formed retinue yet
                if (enlistment.EnlistmentTier == 7)
                {
                    sb.AppendLine("You have been granted the authority to command a personal retinue.");
                    sb.AppendLine();
                    sb.AppendLine($"Retinue Capacity: 0 / {RetinueManager.CommanderCapacity1} soldiers");
                    sb.AppendLine();
                    sb.AppendLine("Visit your lord to select your retinue type and begin recruitment.");
                }
                else
                {
                    sb.AppendLine("Your retinue has been lost. All soldiers have fallen.");
                    sb.AppendLine();
                    sb.AppendLine("Visit your lord to rebuild your retinue when ready.");
                }

                return sb.ToString();
            }

            // Standard retinue muster display
            sb.AppendLine("Your soldiers form up for inspection. You walk the line, checking");
            sb.AppendLine("each man's gear and bearing. They salute as you pass.");
            sb.AppendLine();
            sb.AppendLine("_____ RETINUE STRENGTH _____");
            sb.AppendLine();

            // Current strength vs capacity
            var capacity = RetinueManager.GetTierCapacity(enlistment.EnlistmentTier);
            var current = state.TotalSoldiers;

            if (_currentMuster != null)
            {
                _currentMuster.RetinueStrength = current;
                _currentMuster.RetinueCapacity = capacity;
            }

            sb.AppendLine($"Current Strength:    {current} / {capacity} soldiers");

            // Morale (based on RetinueLoyalty)
            var loyalty = state.RetinueLoyalty;
            var moraleDesc = GetMoraleDescription(loyalty);
            var moraleColor = loyalty < 50 ? "⚠️" : "";
            sb.AppendLine($"Morale:              {moraleColor}{moraleDesc} ({loyalty})");

            // Equipment status (simplified for now)
            var equipmentStatus = GetEquipmentStatus(state);
            sb.AppendLine($"Equipment:           {equipmentStatus}");

            // Experience (based on battles participated)
            var expDesc = GetExperienceDescription(state.BattlesParticipated);
            sb.AppendLine($"Experience:          {expDesc} ({state.BattlesParticipated} battles)");

            sb.AppendLine();
            sb.AppendLine("_____ THIS PERIOD _____");
            sb.AppendLine();

            // Query casualties from news feed
            var newsBehavior = EnlistedNewsBehavior.Instance;
            int killed = 0;
            int wounded = 0;
            List<string> fallenNames = new List<string>();

            if (newsBehavior != null && _currentMuster != null)
            {
                // Check personal feed for retinue casualty reports since last muster
                var periodFeed = newsBehavior.GetPersonalFeedSince(_currentMuster.LastMusterDay);

                foreach (var item in periodFeed)
                {
                    // Get the formatted text for pattern matching
                    var headline = FormatFeedItemForMuster(item);
                    if (string.IsNullOrEmpty(headline))
                    {
                        continue;
                    }

                    // Look for retinue casualty entries
                    // Format: "Your retinue suffered X killed and Y wounded" or "Your retinue lost X soldier(s)"
                    if (headline.IndexOf("retinue", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        // Extract killed count
                        var killedMatch = Regex.Match(headline, @"(\d+)\s+killed");
                        if (killedMatch.Success && int.TryParse(killedMatch.Groups[1].Value, out int kCount))
                        {
                            killed += kCount;
                        }
                        else
                        {
                            // Try alternate format: "lost X soldier(s)"
                            var lostMatch = Regex.Match(headline, @"lost\s+(\d+)\s+soldier");
                            if (lostMatch.Success && int.TryParse(lostMatch.Groups[1].Value, out int lCount))
                            {
                                killed += lCount;
                            }
                        }

                        // Extract wounded count
                        var woundedMatch = Regex.Match(headline, @"(\d+)\s+wounded");
                        if (woundedMatch.Success && int.TryParse(woundedMatch.Groups[1].Value, out int wCount))
                        {
                            wounded += wCount;
                        }
                    }

                    // Look for named veteran deaths
                    // Format: "X the Y has fallen in battle"
                    if (headline.IndexOf("has fallen in battle", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        // Extract name from headline
                        var nameMatch = Regex.Match(headline, @"^([^\s]+(?:\s+the\s+[^\s]+)?)\s+has\s+fallen");
                        if (nameMatch.Success)
                        {
                            fallenNames.Add(nameMatch.Groups[1].Value.Trim());
                        }
                    }
                }
            }

            // Store in muster state for retinue recruitment option
            if (_currentMuster != null)
            {
                _currentMuster.RetinueCasualties = killed;
                _currentMuster.FallenRetinueNames = fallenNames;
            }

            // Display casualties in spec format: "X killed, Y wounded"
            if (killed > 0 || wounded > 0)
            {
                var casualtyParts = new List<string>();
                if (killed > 0)
                {
                    casualtyParts.Add($"{killed} killed");
                }
                if (wounded > 0)
                {
                    casualtyParts.Add($"{wounded} wounded");
                }
                sb.AppendLine($"Casualties:          {string.Join(", ", casualtyParts)}");
                sb.AppendLine($"Recruits Added:      0");
                sb.AppendLine($"Desertions:          0");
            }
            else
            {
                sb.AppendLine("Casualties:          None");
                sb.AppendLine($"Recruits Added:      0");
                sb.AppendLine($"Desertions:          0");
                sb.AppendLine();
                sb.AppendLine("Your retinue stands ready. No losses this period.");
            }

            // Show fallen soldiers with names
            if (fallenNames.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("_____ FALLEN THIS PERIOD _____");
                sb.AppendLine();

                foreach (var name in fallenNames)
                {
                    sb.AppendLine($"• {name} - Killed in battle");
                }
            }

            // Show recruitment availability
            var openings = capacity - current;
            if (openings > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"[{openings} recruitment slot{(openings == 1 ? "" : "s")} available]");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets morale description from loyalty value.
        /// </summary>
        private string GetMoraleDescription(int loyalty)
        {
            return loyalty switch
            {
                >= 80 => "Excellent",
                >= 65 => "High",
                >= 50 => "Steady",
                >= 35 => "Shaky",
                >= 20 => "Low",
                _ => "Critical"
            };
        }

        /// <summary>
        /// Gets equipment status description (simplified).
        /// </summary>
        private string GetEquipmentStatus(RetinueState state)
        {
            // Simplified: based on troop tier quality
            // In the future, this could check actual equipment values
            if (state == null || state.TotalSoldiers == 0)
            {
                return "N/A";
            }

            // Basic heuristic: if we have troops, they're equipped
            return "Serviceable";
        }

        /// <summary>
        /// Gets experience description from battles participated.
        /// </summary>
        private string GetExperienceDescription(int battles)
        {
            return battles switch
            {
                >= 20 => "Battle-hardened veterans",
                >= 10 => "Experienced troops",
                >= 5 => "Seasoned soldiers",
                >= 2 => "Tested in combat",
                _ => "Green but willing"
            };
        }

        private string BuildMusterCompleteText()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Muster is dismissed. The sergeants release the company.");
            sb.AppendLine();

            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null || _currentMuster == null)
            {
                sb.AppendLine("[Muster data unavailable]");
                return sb.ToString();
            }

            // Section 1: Muster Outcomes
            sb.AppendLine("_____ MUSTER OUTCOMES _____");
            sb.AppendLine();

            BuildMusterOutcomesSection(sb, enlistment);

            sb.AppendLine();

            // Section 2: Rank & Progression
            sb.AppendLine("_____ RANK & PROGRESSION _____");
            sb.AppendLine();

            BuildRankProgressionSection(sb, enlistment);

            sb.AppendLine();

            // Section 3: Period Summary
            var periodDays = _currentMuster.MusterDay - _currentMuster.LastMusterDay;
            sb.AppendLine($"_____ PERIOD SUMMARY ({periodDays} Days) _____");
            sb.AppendLine();

            BuildPeriodSummarySection(sb, enlistment);

            return sb.ToString();
        }

        private void BuildMusterOutcomesSection(StringBuilder sb, EnlistmentBehavior enlistment)
        {
            // Pay Received
            var payAmount = _currentMuster.PayReceived;
            var payType = _currentMuster.PayOutcome switch
            {
                "full" => "Full Payment",
                "partial" => "Partial Payment",
                "iou" => "IOU - Pay Delayed",
                "corruption" => "Corruption Deal",
                _ => "Unknown"
            };
            sb.AppendLine($"Pay Received:         {payAmount} denars ({payType})");

            // Ration Issued
            var rationText = _currentMuster.RationOutcome switch
            {
                "issued" => GetRationItemName(enlistment),
                "none" => "None - low supply",
                "officer_exempt" => "Officer Exempt",
                _ => "Not processed"
            };
            sb.AppendLine($"Ration Issued:        {rationText}");

            // Baggage Check
            var baggageText = _currentMuster.BaggageOutcome switch
            {
                "passed" => "Passed - No contraband found",
                "clean" => "Passed - Inventory clean",
                "qm_favor" => "Passed (QM favor)",
                "confiscated" => "Failed - Contraband confiscated",
                "bribed" => "Passed (bribe paid)",
                "skipped" => "Not conducted",
                "empty_inventory" => "Nothing to inspect",
                _ => "Not triggered"
            };
            sb.AppendLine($"Baggage Check:        {baggageText}");

            // Equipment Inspection
            var inspectionText = _currentMuster.InspectionOutcome switch
            {
                "perfect" => "Passed with distinction",
                "basic" => "Passed - Adequate condition",
                "failed" => "Failed - Deficiencies noted",
                "excused_wounds" => "Excused (wounds)",
                "skipped" => "Not scheduled",
                _ => "Not conducted"
            };

            if (_currentMuster.InspectionOutcome == "perfect" || _currentMuster.InspectionOutcome == "failed")
            {
                // Add reputation change if applicable
                var repChange = _currentMuster.InspectionOutcome == "perfect" ? "+6 Officer Rep" : "-4 Officer Rep";
                inspectionText += $" ({repChange})";
            }

            sb.AppendLine($"Equipment Inspection: {inspectionText}");

            // Supply Status
            var supplyPct = CompanySupplyManager.Instance?.TotalSupply ?? 0;
            var supplyStatus = supplyPct >= 50 ? "Adequate" : supplyPct >= 20 ? "Low" : "Critical";
            sb.AppendLine($"Supply Status:        {supplyPct}% - {supplyStatus} condition");
        }

        private void BuildRankProgressionSection(StringBuilder sb, EnlistmentBehavior enlistment)
        {
            var currentTier = enlistment.EnlistmentTier;
            var rankName = enlistment.GetRankName(currentTier) ?? $"Tier {currentTier}";

            sb.AppendLine($"Current Rank:         {rankName} (Tier {currentTier})");

            // XP this period - calculate from current XP minus XP at last muster
            var currentXP = enlistment.EnlistmentXP;
            var xpAtLastMuster = enlistment.XPAtLastMuster;
            var xpThisPeriod = Math.Max(0, currentXP - xpAtLastMuster);

            // Get XP requirements from config
            var tierXpRequirements = Mod.Core.Config.ConfigurationManager.GetTierXpRequirements();
            var xpForNextRank = currentTier < tierXpRequirements.Length ? tierXpRequirements[currentTier] : 0;

            if (currentTier >= 9)
            {
                sb.AppendLine($"XP This Period:       +{xpThisPeriod} XP");
                sb.AppendLine($"Total Experience:     {currentXP} XP (Maximum Rank)");
                sb.AppendLine($"Status:               Pinnacle of military service reached");
            }
            else
            {
                var xpPercent = xpForNextRank > 0 ? (int)((float)xpThisPeriod / (xpForNextRank - xpAtLastMuster) * 100) : 0;
                sb.AppendLine($"XP This Period:       +{xpThisPeriod} XP ({xpPercent}%)");

                var nextRankName = enlistment.GetRankName(currentTier + 1) ?? $"Tier {currentTier + 1}";
                var progressPercent = xpForNextRank > 0 ? (int)((float)currentXP / xpForNextRank * 100) : 0;
                var xpNeeded = Math.Max(0, xpForNextRank - currentXP);

                sb.AppendLine($"Total Experience:     {currentXP} / {xpForNextRank} XP to {nextRankName} ({progressPercent}%)");

                // Estimate days to promotion
                var periodDays = Math.Max(1, _currentMuster.MusterDay - _currentMuster.LastMusterDay);
                var dailyRate = xpThisPeriod / (float)periodDays;
                var estimatedDays = dailyRate > 0 ? (int)(xpNeeded / dailyRate) : 999;
                sb.AppendLine($"Status:               {xpNeeded} XP needed (~{estimatedDays} days at current pace)");
            }

            // XP Sources breakdown
            sb.AppendLine();
            sb.AppendLine("XP Sources This Period:");

            var xpSources = enlistment.GetXPSourcesThisPeriod();
            if (xpSources != null && xpSources.Count > 0)
            {
                var topSources = xpSources.OrderByDescending(kvp => kvp.Value).Take(5);
                foreach (var source in topSources)
                {
                    var sourceName = FormatXPSourceName(source.Key);
                    sb.AppendLine($"• {sourceName}: +{source.Value} XP");
                }
            }
            else
            {
                // Fallback: all XP from daily service
                sb.AppendLine($"• Daily service: +{xpThisPeriod} XP");
            }
        }

        private void BuildPeriodSummarySection(StringBuilder sb, EnlistmentBehavior enlistment)
        {
            // Net Gold
            var goldGained = _currentMuster.PayReceived;
            sb.AppendLine($"Net Gold:            +{goldGained:N0} denars ({goldGained} pay)");

            // Reputation Changes
            // Note: We don't track reputation changes across the period currently,
            // so this is a placeholder for future enhancement
            sb.AppendLine($"Reputation Changes:  [Tracked in future update]");

            // Skills Improved
            sb.AppendLine($"Skills Improved:     [Tracked in future update]");

            // Items Acquired
            sb.AppendLine($"Items Acquired:      [Tracked in future update]");

            // Unit Status
            var currentTier = enlistment.EnlistmentTier;
            var retinueCount = RetinueManager.Instance?.State?.TroopCounts?.Values.Sum() ?? 0;
            var casualties = _currentMuster.RetinueCasualties;
            var moraleText = GetMoraleText();

            if (currentTier >= 7 && retinueCount > 0)
            {
                sb.AppendLine($"Unit Status:         {casualties} casualties, morale {moraleText}");
            }
            else
            {
                sb.AppendLine($"Unit Status:         [No retinue under command]");
            }

            // Next Muster
            var paydayInterval = Mod.Core.Config.ConfigurationManager.LoadFinanceConfig()?.PaydayIntervalDays ?? 12;
            var nextMusterDay = _currentMuster.MusterDay + paydayInterval;
            sb.AppendLine($"Next Muster:         Day {nextMusterDay} ({paydayInterval} days)");
        }

        private string GetRationItemName(EnlistmentBehavior enlistment)
        {
            // Try to get the ration item name from the last issued ration
            // This is a best-effort approach; if we can't determine it, show generic text
            var tier = enlistment.EnlistmentTier;
            return tier switch
            {
                >= 5 => "Officer Ration",
                >= 3 => "Field Ration",
                _ => "Basic Ration"
            };
        }

        private string FormatXPSourceName(string sourceKey)
        {
            return sourceKey switch
            {
                "daily_service" => "Daily service",
                "battles_won" => "Battles won",
                "orders_completed" => "Orders completed",
                "training" => "Training",
                "reputation" => "Officer reputation gains",
                "events" => "Event outcomes",
                _ => sourceKey
            };
        }

        private string GetMoraleText()
        {
            var morale = RetinueManager.Instance?.State?.RetinueLoyalty ?? 50;
            return morale switch
            {
                >= 75 => "high",
                >= 50 => "steady",
                >= 25 => "wavering",
                _ => "poor"
            };
        }

        private static string GetStrategicContextFlavor(string context)
        {
            return context switch
            {
                "coordinated_offensive" => "The host prepares for the Grand Campaign. Spirits are high as the sergeants call the muster.",
                "desperate_defense" => "The realm bleeds. Muster is brief—every sword is needed on the line.",
                "siege_operation" => "The walls loom above. Muster proceeds in the muddy siege lines.",
                "raid_operation" => "Smoke rises in the distance. The company assembles between raids.",
                "patrol_peacetime" => "A quiet day on the march. The sergeants call muster at the usual hour.",
                "garrison_duty" => "Within the castle walls, the garrison assembles for pay day.",
                "winter_camp" => "Snow blankets the camp. Men huddle close as the sergeants call muster.",
                "recruitment_drive" => "Fresh faces join the ranks. The company swells as muster is called.",
                _ => "The sergeants call the muster. Your company assembles in formation."
            };
        }

        #endregion

        #region Baggage Check Handlers

        /// <summary>
        /// Returns true if contraband confrontation is currently active (not yet resolved).
        /// </summary>
        private bool IsContrabandConfrontationActive()
        {
            if (_currentMuster == null || !_currentMuster.ContrabandFound)
            {
                return false;
            }

            // If outcome is already set, confrontation is resolved
            if (!string.IsNullOrEmpty(_currentMuster.BaggageOutcome) &&
                _currentMuster.BaggageOutcome != "not_conducted" &&
                _currentMuster.BaggageOutcome != "clean" &&
                _currentMuster.BaggageOutcome != "qm_favor")
            {
                return false;
            }

            // Check if we have active contraband and QM rep is in confrontation range
            return _currentMuster.QMRep < 65;
        }

        /// <summary>
        /// Handles bribe attempt. 50% success based on Charm check.
        /// Success: Keep item, pay bribe, no scrutiny.
        /// Failure: Lose gold AND item, +3 scrutiny.
        /// </summary>
        private void HandleBribe()
        {
            if (_currentMuster == null)
            {
                return;
            }

            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null)
            {
                return;
            }

            var playerTier = enlistment.EnlistmentTier;
            var playerRole = EnlistedStatusManager.Instance?.GetPrimaryRole() ?? "Soldier";
            var contrabandResult = ContrabandChecker.ScanInventory(playerTier, playerRole);

            if (!contrabandResult.HasContraband)
            {
                _currentMuster.BaggageOutcome = "clean";
                GameMenu.SwitchToMenu(MusterInspectionMenuId);
                return;
            }

            var contraband = contrabandResult.MostValuable;
            if (contraband.Item == null)
            {
                _currentMuster.BaggageOutcome = "clean";
                GameMenu.SwitchToMenu(MusterInspectionMenuId);
                return;
            }

            var bribeAmount = ContrabandChecker.CalculateBribeAmount(contraband.Value);

            // Deduct bribe cost
            Hero.MainHero.ChangeHeroGold(-bribeAmount);

            // 50% success chance, modified by Charm
            var charm = Hero.MainHero?.GetSkillValue(DefaultSkills.Charm) ?? 0;
            var successChance = 0.50f + (charm / 1000f); // +0.1 at Charm 100
            var success = MBRandom.RandomFloat < successChance;

            if (success)
            {
                _currentMuster.BaggageOutcome = "bribed";
                ModLogger.Info(LogCategory, $"Bribe successful: {bribeAmount} denars paid, item kept");

                // Give Charm XP
                if (Hero.MainHero != null)
                {
                    Hero.MainHero.AddSkillXp(DefaultSkills.Charm, 10);
                }
            }
            else
            {
                _currentMuster.BaggageOutcome = "bribe_failed";

                // Confiscate item
                ContrabandChecker.ConfiscateItem(contraband.Item);

                // Apply scrutiny and fine
                Escalation.EscalationManager.Instance?.ModifyScrutiny(3, "Failed bribe attempt");
                var fine = ContrabandChecker.CalculateFineAmount(contraband.Value);
                if (Hero.MainHero != null)
                {
                    Hero.MainHero.ChangeHeroGold(-fine);
                }

                ModLogger.Warn(LogCategory, $"Bribe failed: lost {bribeAmount} denars + {contraband.Item.Name} + {fine} denars fine");
            }

            // Refresh display with outcome
            MBTextManager.SetTextVariable("MUSTER_BAGGAGE_TEXT", BuildBaggageText());
            GameMenu.SwitchToMenu(MusterInspectionMenuId);
        }

        /// <summary>
        /// Handles smuggle attempt. 70% success based on Roguery check.
        /// Success: Keep item, +5 Roguery XP.
        /// Failure: Lose item, +5 scrutiny, +3 discipline.
        /// </summary>
        private void HandleSmuggle()
        {
            if (_currentMuster == null)
            {
                return;
            }

            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null)
            {
                return;
            }

            var playerTier = enlistment.EnlistmentTier;
            var playerRole = EnlistedStatusManager.Instance?.GetPrimaryRole() ?? "Soldier";
            var contrabandResult = ContrabandChecker.ScanInventory(playerTier, playerRole);

            if (!contrabandResult.HasContraband)
            {
                _currentMuster.BaggageOutcome = "clean";
                GameMenu.SwitchToMenu(MusterInspectionMenuId);
                return;
            }

            var contraband = contrabandResult.MostValuable;
            if (contraband.Item == null)
            {
                _currentMuster.BaggageOutcome = "clean";
                GameMenu.SwitchToMenu(MusterInspectionMenuId);
                return;
            }

            // 70% success chance, modified by Roguery
            var roguery = Hero.MainHero?.GetSkillValue(DefaultSkills.Roguery) ?? 0;
            var successChance = 0.70f + (roguery / 2000f); // +0.05 at Roguery 100
            var success = MBRandom.RandomFloat < successChance;

            if (success)
            {
                _currentMuster.BaggageOutcome = "smuggled";
                ModLogger.Info(LogCategory, $"Smuggle successful: kept {contraband.Item.Name}");

                // Give Roguery XP
                if (Hero.MainHero != null)
                {
                    Hero.MainHero.AddSkillXp(DefaultSkills.Roguery, 15);
                }
            }
            else
            {
                _currentMuster.BaggageOutcome = "smuggle_failed";

                // Confiscate item
                ContrabandChecker.ConfiscateItem(contraband.Item);

                // Heavy penalties for getting caught
                Escalation.EscalationManager.Instance?.ModifyScrutiny(5, "Caught smuggling contraband");
                Escalation.EscalationManager.Instance?.ModifyDiscipline(3, "Attempted smuggling at muster");

                var fine = ContrabandChecker.CalculateFineAmount(contraband.Value);
                if (Hero.MainHero != null)
                {
                    Hero.MainHero.ChangeHeroGold(-fine);
                }

                ModLogger.Warn(LogCategory, $"Smuggle failed: lost {contraband.Item.Name}, +5 scrutiny, +3 discipline");
            }

            // Refresh display with outcome
            MBTextManager.SetTextVariable("MUSTER_BAGGAGE_TEXT", BuildBaggageText());
            GameMenu.SwitchToMenu(MusterInspectionMenuId);
        }

        /// <summary>
        /// Handles voluntary confiscation or forced acceptance.
        /// Item confiscated, fine applied, +2 scrutiny.
        /// </summary>
        private void HandleConfiscation()
        {
            if (_currentMuster == null)
            {
                return;
            }

            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null)
            {
                return;
            }

            var playerTier = enlistment.EnlistmentTier;
            var playerRole = EnlistedStatusManager.Instance?.GetPrimaryRole() ?? "Soldier";
            var contrabandResult = ContrabandChecker.ScanInventory(playerTier, playerRole);

            if (!contrabandResult.HasContraband)
            {
                _currentMuster.BaggageOutcome = "clean";
                GameMenu.SwitchToMenu(MusterInspectionMenuId);
                return;
            }

            var contraband = contrabandResult.MostValuable;
            if (contraband.Item == null)
            {
                _currentMuster.BaggageOutcome = "clean";
                GameMenu.SwitchToMenu(MusterInspectionMenuId);
                return;
            }

            // Check if item is equipped and unequip if needed
            var isEquipped = IsItemEquipped(contraband.Item);
            if (isEquipped)
            {
                UnequipItem(contraband.Item);
                ModLogger.Debug(LogCategory, $"Unequipped {contraband.Item.Name} before confiscation");
            }

            // Confiscate item
            ContrabandChecker.ConfiscateItem(contraband.Item);

            // Apply scrutiny and fine
            Escalation.EscalationManager.Instance?.ModifyScrutiny(2, "Contraband confiscated at muster");
            var fine = ContrabandChecker.CalculateFineAmount(contraband.Value);
            Hero.MainHero.ChangeHeroGold(-fine);

            _currentMuster.BaggageOutcome = "confiscated";

            ModLogger.Info(LogCategory, $"Confiscation: lost {contraband.Item.Name}, {fine} denars fine, +2 scrutiny");

            // Refresh display with outcome
            MBTextManager.SetTextVariable("MUSTER_BAGGAGE_TEXT", BuildBaggageText());
            GameMenu.SwitchToMenu(MusterInspectionMenuId);
        }

        /// <summary>
        /// Handles protest attempt. 20% success based on Charm.
        /// Success: Keep item, no penalties.
        /// Failure: Confiscate item, +4 scrutiny, +2 discipline.
        /// </summary>
        private void HandleProtest()
        {
            if (_currentMuster == null)
            {
                return;
            }

            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null)
            {
                return;
            }

            var playerTier = enlistment.EnlistmentTier;
            var playerRole = EnlistedStatusManager.Instance?.GetPrimaryRole() ?? "Soldier";
            var contrabandResult = ContrabandChecker.ScanInventory(playerTier, playerRole);

            if (!contrabandResult.HasContraband)
            {
                _currentMuster.BaggageOutcome = "clean";
                GameMenu.SwitchToMenu(MusterInspectionMenuId);
                return;
            }

            var contraband = contrabandResult.MostValuable;
            if (contraband.Item == null)
            {
                _currentMuster.BaggageOutcome = "clean";
                GameMenu.SwitchToMenu(MusterInspectionMenuId);
                return;
            }

            // 20% success chance, slightly modified by Charm
            var charm = Hero.MainHero?.GetSkillValue(DefaultSkills.Charm) ?? 0;
            var successChance = 0.20f + (charm / 2000f); // +0.05 at Charm 100
            var success = MBRandom.RandomFloat < successChance;

            if (success)
            {
                _currentMuster.BaggageOutcome = "protested";
                ModLogger.Info(LogCategory, $"Protest successful: kept {contraband.Item.Name}");

                // Give Charm XP
                if (Hero.MainHero != null)
                {
                    Hero.MainHero.AddSkillXp(DefaultSkills.Charm, 20);
                }
            }
            else
            {
                _currentMuster.BaggageOutcome = "protested";

                // Check if equipped and unequip if needed
                var isEquipped = IsItemEquipped(contraband.Item);
                if (isEquipped)
                {
                    UnequipItem(contraband.Item);
                }

                // Confiscate item
                ContrabandChecker.ConfiscateItem(contraband.Item);

                // Heavy penalties for failed protest
                Escalation.EscalationManager.Instance?.ModifyScrutiny(4, "Failed protest at muster");
                Escalation.EscalationManager.Instance?.ModifyDiscipline(2, "Insubordination at muster");

                var fine = ContrabandChecker.CalculateFineAmount(contraband.Value);
                if (Hero.MainHero != null)
                {
                    Hero.MainHero.ChangeHeroGold(-fine);
                }

                ModLogger.Warn(LogCategory, $"Protest failed: lost {contraband.Item.Name}, +4 scrutiny, +2 discipline");
            }

            // Refresh display with outcome
            MBTextManager.SetTextVariable("MUSTER_BAGGAGE_TEXT", BuildBaggageText());
            GameMenu.SwitchToMenu(MusterInspectionMenuId);
        }

        /// <summary>
        /// Checks if an item is currently equipped by the player.
        /// </summary>
        private bool IsItemEquipped(ItemObject item)
        {
            var hero = Hero.MainHero;
            if (hero == null)
            {
                return false;
            }

            var equipment = hero.BattleEquipment;
            for (int i = 0; i < 12; i++) // 12 equipment slots
            {
                var slot = (EquipmentIndex)i;
                var element = equipment.GetEquipmentFromSlot(slot);
                if (element.Item == item)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Unequips an item from the player's equipment.
        /// </summary>
        private void UnequipItem(ItemObject item)
        {
            var hero = Hero.MainHero;
            if (hero == null)
            {
                return;
            }

            var equipment = hero.BattleEquipment.Clone();
            for (int i = 0; i < 12; i++)
            {
                var slot = (EquipmentIndex)i;
                var element = equipment.GetEquipmentFromSlot(slot);
                if (element.Item == item)
                {
                    equipment[slot] = EquipmentElement.Invalid;
                    hero.BattleEquipment.FillFrom(equipment);
                    ModLogger.Debug(LogCategory, $"Unequipped {item.Name} from slot {slot}");
                    return;
                }
            }
        }

        #endregion

        #region Inspection Handlers

        /// <summary>
        /// Resolves perfect attention inspection (OneHanded 30+).
        /// Awards +10 OneHanded XP, +6 Officer Rep, +3 Soldier Rep.
        /// </summary>
        private void ResolveInspectionPerfect()
        {
            if (_currentMuster == null)
            {
                return;
            }

            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null)
            {
                return;
            }

            var skill = GetRelevantMeleeSkill();
            var hero = Hero.MainHero;

            // Award XP
            hero?.AddSkillXp(skill, 10);

            // Award reputation
            EscalationManager.Instance?.ModifyOfficerReputation(6, "Perfect inspection");
            EscalationManager.Instance?.ModifySoldierReputation(3, "Perfect inspection");

            // Record outcome
            _currentMuster.InspectionOutcome = "perfect";

            // Update cooldown
            var currentDay = (int)CampaignTime.Now.ToDays;
            typeof(EnlistmentBehavior)
                .GetField("_lastInspectionDay", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(enlistment, currentDay);

            ModLogger.Info(LogCategory, $"Inspection passed perfectly: +10 {skill.Name} XP, +6 Officer, +3 Soldier");

            // Refresh display
            MBTextManager.SetTextVariable("MUSTER_INSPECTION_TEXT", BuildInspectionResultText("perfect"));
            GameMenu.SwitchToMenu(MusterRecruitMenuId);
        }

        /// <summary>
        /// Resolves basic inspection (meets minimum standards).
        /// Awards +5 OneHanded XP, +2 Officer Rep.
        /// </summary>
        private void ResolveInspectionBasic()
        {
            if (_currentMuster == null)
            {
                return;
            }

            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null)
            {
                return;
            }

            var skill = GetRelevantMeleeSkill();
            var hero = Hero.MainHero;

            // Award XP
            hero?.AddSkillXp(skill, 5);

            // Award reputation
            EscalationManager.Instance?.ModifyOfficerReputation(2, "Basic inspection");

            // Record outcome
            _currentMuster.InspectionOutcome = "basic";

            // Update cooldown
            var currentDay = (int)CampaignTime.Now.ToDays;
            typeof(EnlistmentBehavior)
                .GetField("_lastInspectionDay", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(enlistment, currentDay);

            ModLogger.Info(LogCategory, $"Inspection passed: +5 {skill.Name} XP, +2 Officer");

            // Refresh display
            MBTextManager.SetTextVariable("MUSTER_INSPECTION_TEXT", BuildInspectionResultText("basic"));
            GameMenu.SwitchToMenu(MusterRecruitMenuId);
        }

        /// <summary>
        /// Resolves failed inspection (not ready).
        /// Applies -8 Officer, -4 Soldier, +8 Scrutiny, +5 Discipline.
        /// </summary>
        private void ResolveInspectionUnprepared()
        {
            if (_currentMuster == null)
            {
                return;
            }

            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null)
            {
                return;
            }

            // Apply penalties
            EscalationManager.Instance?.ModifyOfficerReputation(-8, "Failed inspection");
            EscalationManager.Instance?.ModifySoldierReputation(-4, "Failed inspection");
            EscalationManager.Instance?.ModifyScrutiny(8, "Failed equipment inspection");
            EscalationManager.Instance?.ModifyDiscipline(5, "Slovenly appearance at inspection");

            // Record outcome
            _currentMuster.InspectionOutcome = "failed";

            // Update cooldown
            var currentDay = (int)CampaignTime.Now.ToDays;
            typeof(EnlistmentBehavior)
                .GetField("_lastInspectionDay", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(enlistment, currentDay);

            ModLogger.Warn(LogCategory, "Inspection failed: -8 Officer, -4 Soldier, +8 Scrutiny, +5 Discipline");

            // Refresh display
            MBTextManager.SetTextVariable("MUSTER_INSPECTION_TEXT", BuildInspectionResultText("failed"));
            GameMenu.SwitchToMenu(MusterRecruitMenuId);
        }

        /// <summary>
        /// Gets the relevant melee skill for inspection.
        /// Returns highest melee skill, or Athletics as fallback.
        /// </summary>
        private SkillObject GetRelevantMeleeSkill()
        {
            var hero = Hero.MainHero;
            if (hero == null)
            {
                return DefaultSkills.Athletics;
            }

            var skills = new[]
            {
                (skill: DefaultSkills.OneHanded, value: hero.GetSkillValue(DefaultSkills.OneHanded)),
                (skill: DefaultSkills.TwoHanded, value: hero.GetSkillValue(DefaultSkills.TwoHanded)),
                (skill: DefaultSkills.Polearm, value: hero.GetSkillValue(DefaultSkills.Polearm))
            };

            var best = skills.OrderByDescending(s => s.value).FirstOrDefault();

            // If no melee skills, use Athletics
            return best.value > 0 ? best.skill : DefaultSkills.Athletics;
        }

        #endregion

        #region Recruit Handlers

        /// <summary>
        /// Resolves mentoring a recruit (Leadership 25+).
        /// Awards +15 Leadership XP, +30 Sergeant trait XP, +5 Officer, +6 Soldier, +30 troop XP.
        /// </summary>
        private void ResolveRecruitTrain()
        {
            if (_currentMuster == null)
            {
                return;
            }

            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null)
            {
                return;
            }

            var hero = Hero.MainHero;

            // Award Leadership XP
            hero?.AddSkillXp(DefaultSkills.Leadership, 15);

            // Award Sergeant trait XP (if implemented)
            // TODO: Add trait XP when trait system is implemented

            // Award reputation
            EscalationManager.Instance?.ModifyOfficerReputation(5, "Mentored recruit");
            EscalationManager.Instance?.ModifySoldierReputation(6, "Mentored recruit");

            // Award enlistment XP (representing troop improvement)
            enlistment.AddEnlistmentXP(30, "Recruit training");

            // Record outcome
            _currentMuster.RecruitOutcome = "mentored";

            // Update cooldown
            var currentDay = (int)CampaignTime.Now.ToDays;
            typeof(EnlistmentBehavior)
                .GetField("_lastRecruitDay", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(enlistment, currentDay);

            ModLogger.Info(LogCategory, "Recruit mentored: +15 Leadership XP, +5 Officer, +6 Soldier, +30 XP");

            // Refresh display
            MBTextManager.SetTextVariable("MUSTER_RECRUIT_TEXT", BuildRecruitResultText("mentored"));
            ProceedToNextStageFromRecruit();
        }

        /// <summary>
        /// Resolves ignoring the recruit.
        /// Applies -2 Soldier Rep.
        /// </summary>
        private void ResolveRecruitIgnore()
        {
            if (_currentMuster == null)
            {
                return;
            }

            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null)
            {
                return;
            }

            // Apply penalty
            EscalationManager.Instance?.ModifySoldierReputation(-2, "Ignored recruit");

            // Record outcome
            _currentMuster.RecruitOutcome = "ignored";

            // Update cooldown
            var currentDay = (int)CampaignTime.Now.ToDays;
            typeof(EnlistmentBehavior)
                .GetField("_lastRecruitDay", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(enlistment, currentDay);

            ModLogger.Info(LogCategory, "Recruit ignored: -2 Soldier");

            // Refresh display
            MBTextManager.SetTextVariable("MUSTER_RECRUIT_TEXT", BuildRecruitResultText("ignored"));
            ProceedToNextStageFromRecruit();
        }

        /// <summary>
        /// Resolves hazing the recruit.
        /// Awards +4 Soldier, -5 Officer, +8 Discipline.
        /// </summary>
        private void ResolveRecruitHaze()
        {
            if (_currentMuster == null)
            {
                return;
            }

            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null)
            {
                return;
            }

            // Apply effects
            EscalationManager.Instance?.ModifySoldierReputation(4, "Hazed recruit");
            EscalationManager.Instance?.ModifyOfficerReputation(-5, "Hazed recruit");
            EscalationManager.Instance?.ModifyDiscipline(8, "Hazing recruit at muster");

            // Record outcome
            _currentMuster.RecruitOutcome = "hazed";

            // Update cooldown
            var currentDay = (int)CampaignTime.Now.ToDays;
            typeof(EnlistmentBehavior)
                .GetField("_lastRecruitDay", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(enlistment, currentDay);

            ModLogger.Info(LogCategory, "Recruit hazed: +4 Soldier, -5 Officer, +8 Discipline");

            // Refresh display
            MBTextManager.SetTextVariable("MUSTER_RECRUIT_TEXT", BuildRecruitResultText("hazed"));
            ProceedToNextStageFromRecruit();
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets the localized rank name for a given tier.
        /// Falls back to generic tier names if culture-specific progression config unavailable.
        /// </summary>
        private string GetRankName(int tier)
        {
            // Generic fallback rank names
            return tier switch
            {
                1 => "Follower",
                2 => "Recruit",
                3 => "Free Sword",
                4 => "Sergeant",
                5 => "Veteran",
                6 => "Blade",
                7 => "Captain",
                8 => "Lieutenant",
                9 => "Commander",
                _ => $"Tier {tier}"
            };

            // TODO: Query progression_config.json for culture-specific rank names
        }

        /// <summary>
        /// Gets the XP required to reach the next tier.
        /// Returns 0 if already at max tier.
        /// </summary>
        /// <summary>
        /// Gets the cumulative XP threshold to ENTER a specific tier.
        /// E.g., GetXPThresholdForTier(3) returns 3000 (total XP needed to become T3).
        /// </summary>
        private int GetXPThresholdForTier(int tier)
        {
            // XP thresholds are cumulative totals from progression_config.json
            return tier switch
            {
                <= 1 => 0,      // T1 starts at 0
                2 => 800,       // T2 requires 800 total
                3 => 3000,      // T3 requires 3000 total
                4 => 6000,      // T4 requires 6000 total
                5 => 11000,     // T5 requires 11000 total
                6 => 19000,     // T6 requires 19000 total
                7 => 30000,     // T7 requires 30000 total
                8 => 45000,     // T8 requires 45000 total
                9 => 65000,     // T9 requires 65000 total
                _ => 0
            };
        }

        /// <summary>
        /// Gets the XP needed to advance from current tier to next tier.
        /// Returns 0 if at max tier.
        /// </summary>
        private int GetXPRequiredForNextTier(int currentTier)
        {
            var currentThreshold = GetXPThresholdForTier(currentTier);
            var nextThreshold = GetXPThresholdForTier(currentTier + 1);
            return nextThreshold > currentThreshold ? nextThreshold : 0;
        }

        /// <summary>
        /// Converts baggage access state to human-readable text.
        /// </summary>
        private string GetBaggageAccessText(BaggageAccessState state)
        {
            return state switch
            {
                BaggageAccessState.FullAccess => "Available",
                BaggageAccessState.TemporaryAccess => "Temporary",
                BaggageAccessState.NoAccess => "Unavailable",
                BaggageAccessState.Locked => "Locked",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Extracts XP value from order summary text like "Patrol completed (+20 XP)".
        /// </summary>
        private string ExtractXPFromSummary(string summary)
        {
            if (string.IsNullOrEmpty(summary))
            {
                return "";
            }

            // Look for pattern like "+20 XP" or "+20XP"
            var match = Regex.Match(summary, @"\+\d+\s*XP");
            return match.Success ? $"({match.Value})" : "";
        }

        /// <summary>
        /// Formats a dispatch item for display in the muster period summary.
        /// Simplifies headlines and removes redundant prefixes.
        /// </summary>
        private string FormatFeedItemForMuster(DispatchItem item)
        {
            // DispatchItem is a struct, so check for default values instead
            if (item.DayCreated == 0 && string.IsNullOrEmpty(item.HeadlineKey))
            {
                return string.Empty;
            }

            // Use HeadlineKey directly as it contains the formatted text
            var headline = item.HeadlineKey ?? string.Empty;

            // Remove common prefixes to make it more concise
            headline = headline.Replace("The company ", "");
            headline = headline.Replace("Your lord's army ", "Army ");

            return headline.Trim();
        }

        /// <summary>
        /// Counts battles that occurred since the last muster.
        /// </summary>
        private int CountBattlesThisPeriod()
        {
            var newsBehavior = Interface.Behaviors.EnlistedNewsBehavior.Instance;
            if (newsBehavior == null)
            {
                return 0;
            }

            var lastMusterDay = _currentMuster?.LastMusterDay ?? 0;
            var feedItems = newsBehavior.GetPersonalFeedSince(lastMusterDay);

            if (feedItems == null)
            {
                return 0;
            }

            // Count battle-related feed items
            return feedItems.Count(item =>
                item.Category?.Contains("battle") == true ||
                item.HeadlineKey?.ToLowerInvariant().Contains("battle") == true ||
                item.HeadlineKey?.ToLowerInvariant().Contains("victory") == true ||
                item.HeadlineKey?.ToLowerInvariant().Contains("defeat") == true);
        }

        /// <summary>
        /// Opens the quartermaster conversation after muster.
        /// </summary>
        private void OpenQuartermasterConversation()
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment?.IsEnlisted != true)
            {
                ModLogger.Warn(LogCategory, "Cannot open QM: player not enlisted");
                return;
            }

            var qm = enlistment.GetOrCreateQuartermaster();
            if (qm != null && qm.IsAlive)
            {
                ModLogger.Info(LogCategory, "Opening quartermaster conversation from muster complete menu");
                CampaignMapConversation.OpenConversation(
                    new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty),
                    new ConversationCharacterData(qm.CharacterObject, qm.PartyBelongedTo?.Party));
            }
            else
            {
                ModLogger.Error(LogCategory, "Quartermaster hero unavailable");
                InformationManager.DisplayMessage(new InformationMessage("Quartermaster unavailable."));
            }
        }

        #endregion
    }
}

