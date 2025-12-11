using System;
using System.Collections.Generic;
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
    /// Currently handles the enlistment bag-check incident.
    /// </summary>
    public sealed class EnlistedIncidentsBehavior : CampaignBehaviorBase
    {
        private Incident _bagCheckIncident;
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
            RegisterBagCheckIncident();
        }

        private void RegisterBagCheckIncident()
        {
            try
            {
                // Trigger flag is unused for manual invocation, but set a generic trigger
                _bagCheckIncident = Game.Current.ObjectManager.RegisterPresumedObject(new Incident("incident_enlisted_bag_check"));
                _bagCheckIncident.Initialize(
                    "{=qm_bagcheck_title}Enlistment Bag Check",
                    "{=qm_bagcheck_body}The quartermaster lifts his quill. \"You can’t march in that finery. Regimental rules. Everything goes in the wagons or my ledger. If the wagons burn, so does your past life. How do you want this written, soldier?\"",
                    IncidentsCampaignBehaviour.IncidentTrigger.EnteringTown,
                    IncidentsCampaignBehaviour.IncidentType.TroopSettlementRelation,
                    CampaignTime.Days(365f),
                    _ => true);

                _bagCheckIncident.AddOption("{=qm_stow_all}\"Stow it all\" (50g)", new List<IncidentEffect>());
                _bagCheckIncident.AddOption("{=qm_sell_all}\"Sell it all\" (60%)", new List<IncidentEffect>());
                _bagCheckIncident.AddOption("{=qm_smuggle_one}\"I'm keeping one thing\" (Roguery 30+)", new List<IncidentEffect>());

                ModLogger.Info("Incident", "Registered enlistment bag-check incident.");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Incident", $"Failed to register bag-check incident: {ex.Message}");
            }
        }

        public void TriggerBagCheckIncident()
        {
            try
            {
                if (_bagCheckIncident == null)
                {
                    RegisterBagCheckIncident();
                }

                if (_bagCheckIncident == null)
                {
                    ModLogger.Warn("Incident", "Bag-check incident unavailable; falling back to direct handling.");
                    EnlistmentBehavior.Instance?.ShowBagCheckInquiryFallback();
                    return;
                }

                // Prefer the native incident flow (map incident), fall back to inquiry if unavailable
                if (!TryStartIncidentNative(_bagCheckIncident))
                {
                    EnlistmentBehavior.Instance?.ShowBagCheckInquiryFallback();
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Incident", $"Error triggering bag-check incident: {ex.Message}");
                EnlistmentBehavior.Instance?.ShowBagCheckInquiryFallback();
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
                ModLogger.Info("Incident", "Started bag-check via native incident UI (NextIncident).");
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

