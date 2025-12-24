using System;
using System.Collections.Generic;
using Enlisted.Features.Camp;
using Enlisted.Mod.Core.Logging;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Incidents;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.CampaignSystem.GameState;
using System.Linq;

namespace Enlisted.Features.Enlistment.Behaviors
{
    /// <summary>
    /// Registers and triggers custom incidents used by Enlisted.
    /// Handles pay muster incident. Bag check uses the narrative event system instead.
    /// </summary>
    public sealed class EnlistedIncidentsBehavior : CampaignBehaviorBase
    {
        private Incident _payMusterIncident;
        
        public static EnlistedIncidentsBehavior Instance { get; private set; }

        public EnlistedIncidentsBehavior()
        {
            Instance = this;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // No state to sync; incidents are registered per session
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            RegisterPayMusterIncident();
        }

        private void RegisterPayMusterIncident()
        {
            try
            {
                _payMusterIncident =
                    Game.Current.ObjectManager.RegisterPresumedObject(new Incident("incident_enlisted_pay_muster"));
                _payMusterIncident.Initialize(
                    "{=enlisted_pay_muster_title}Pay Muster",
                    "{=enlisted_pay_muster_body}The paymaster calls the muster. Step forward to receive your pay.",
                    IncidentsCampaignBehaviour.IncidentTrigger.EnteringTown,
                    IncidentsCampaignBehaviour.IncidentType.TroopSettlementRelation,
                    CampaignTime.Days(365f),
                    _ => true);

                _payMusterIncident.AddOption("{=enlisted_pay_standard}Accept your pay", new List<IncidentEffect>());

                ModLogger.Info("Incident", "Registered pay muster incident.");
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("Incident", "E-INCIDENT-002", "Failed to register pay muster incident", ex);
            }
        }

        public void TriggerPayMusterIncident()
        {
            try
            {
                // For now, use inquiry to ensure callbacks fire consistently.
                ShowPayMusterInquiryFallback();
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("Incident", "E-INCIDENT-004", "Error triggering pay muster incident", ex);
                ShowPayMusterInquiryFallback();
            }
        }

        private void ShowPayMusterInquiryFallback()
        {
            try
            {
                var enlistment = EnlistmentBehavior.Instance;
                var dischargePending = enlistment?.IsPendingDischarge == true;
                var pendingPay = enlistment?.PendingMusterPay ?? 0;
                ModLogger.Info("Pay",
                    $"Pay muster prompt opened (PendingPay={pendingPay}, DischargePending={dischargePending})");

                var campLife = CampLifeBehavior.Instance;
                var payDisrupted = campLife?.IsPayTensionHigh() == true;

                var options = new List<InquiryElement>();

                options.Add(new InquiryElement(
                    dischargePending ? "final_muster" : "standard",
                    dischargePending
                        ? new TextObject("{=enlisted_final_muster}Resolve Final Muster").ToString()
                        : new TextObject("{=enlisted_pay_standard}Accept your pay").ToString(),
                    null,
                    true,
                    dischargePending
                        ? new TextObject("{=enlisted_final_muster_hint}Process your discharge now.").ToString()
                        : new TextObject("{=enlisted_pay_standard_hint}Take your accumulated muster pay.").ToString()));

                // Promissory note (IOU) when pay tension is high.
                // This keeps pay disruption internal: we simply defer payout and retry soon.
                if (!dischargePending && payDisrupted)
                {
                    options.Add(new InquiryElement(
                        "iou",
                        new TextObject("{=enlisted_pay_iou}Accept a promissory note (IOU)").ToString(),
                        null,
                        true,
                        new TextObject("{=enlisted_pay_iou_hint}No coin today. Your pay remains owed and will be resolved at a later muster.").ToString()));
                }

                // Corruption Challenge (Option 2)
                options.Add(new InquiryElement(
                    "corruption",
                    new TextObject("{=enlisted_pay_corruption}Demand a Recount").ToString(),
                    null,
                    true,
                    new TextObject("{=enlisted_pay_corruption_hint}Roguery/Charm check; better payout on success.").ToString()));

                // Side Deal (Option 3)
                options.Add(new InquiryElement(
                    "side_deal",
                    new TextObject("{=enlisted_pay_sidedeal}Side Deal for Select Gear").ToString(),
                    null,
                    true,
                    new TextObject("{=enlisted_pay_sidedeal_hint}Trade most pay for a select gear pick.").ToString()));

                if (dischargePending)
                {
                    options.Add(new InquiryElement(
                        "smuggle",
                        new TextObject("{=enlisted_final_muster_smuggle}Smuggle Out (Deserter)").ToString(),
                        null,
                        true,
                        new TextObject("{=enlisted_final_muster_smuggle_hint}Keep all gear but take deserter penalties.").ToString()));
                }

                var title = new TextObject("{=enlisted_pay_muster_title}Pay Muster");
                var body = payDisrupted
                    ? new TextObject("{=enlisted_pay_muster_body_tense}The paymaster calls the muster, but the strongbox is light.\n\n\"Not today,\" he mutters. \"Name on the slate. Coin later.\"")
                    : new TextObject("{=enlisted_pay_muster_body}The paymaster calls the muster. Step forward to receive your pay.");

                var inquiry = new MultiSelectionInquiryData(
                    title.ToString(),
                    body.ToString(),
                    options,
                    false,
                    1,
                    1,
                    new TextObject("{=qm_continue}Continue").ToString(),
                    new TextObject("{=str_cancel}Cancel").ToString(),
                    selection =>
                    {
                        var choice = selection?.FirstOrDefault()?.Identifier as string;
                        ModLogger.Info("Pay", $"Pay muster choice selected: {choice ?? "null"}");
                        if (choice == "standard" || choice == "final_muster")
                        {
                            EnlistmentBehavior.Instance?.ResolvePayMusterStandard();
                        }
                        else if (choice == "corruption")
                        {
                            EnlistmentBehavior.Instance?.ResolveCorruptionMuster();
                        }
                        else if (choice == "side_deal")
                        {
                            EnlistmentBehavior.Instance?.ResolveSideDealMuster();
                        }
                        else if (choice == "iou")
                        {
                            EnlistmentBehavior.Instance?.ResolvePromissoryMuster();
                        }
                        else if (choice == "smuggle")
                        {
                            EnlistmentBehavior.Instance?.ResolveSmuggleDischarge();
                        }
                    },
                    _ => { EnlistmentBehavior.Instance?.DeferPayMuster(); });

                MBInformationManager.ShowMultiSelectionInquiry(inquiry);
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("Incident", "E-INCIDENT-005", "Pay muster inquiry failed", ex);
            }
        }

        /// <summary>
        ///     Attempts to trigger the incident through the native map incident flow (MapState.NextIncident),
        ///     mirroring how IncidentsCampaignBehaviour.InvokeIncident assigns incidents.
        ///     Falls back to false if unavailable.
        /// </summary>
        private bool TryStartIncidentNative(Incident incident)
        {
            try
            {
                var mapState = GameStateManager.Current?.LastOrDefault<MapState>();
                if (mapState == null)
                {
                    ModLogger.Warn("Incident", "MapState not available; falling back to inquiry.");
                    return false;
                }

                mapState.NextIncident = incident;
                ModLogger.Info("Incident", "Started incident via native map UI (NextIncident).");
                return true;
            }
            catch (Exception ex)
            {
                ModLogger.Warn("Incident", $"Failed to start native incident: {ex.Message}");
                return false;
            }
        }
    }
}

