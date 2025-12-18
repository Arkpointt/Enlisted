using System;
using System.Collections.Generic;
using System.Linq;
using Enlisted.Features.Enlistment.Behaviors;
using EnlistedConfig = Enlisted.Features.Assignments.Core.ConfigurationManager;
using Enlisted.Mod.Core.Logging;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Incidents;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace Enlisted.Features.Lances.Events
{
    /// <summary>
    /// Delivery channel system: native Bannerlord "Incidents" alongside inquiry popups.
    ///
    /// - inquiry channel uses MBInformationManager.ShowMultiSelectionInquiry (handled elsewhere)
    /// - incident channel uses MapState.NextIncident (native incident UI)
    ///
    /// We do NOT rely on IncidentsCampaignBehaviour.TryInvokeIncident because Enlisted suppresses native random incidents while enlisted.
    /// Instead, we set MapState.NextIncident directly (same technique used by EnlistedIncidentsBehavior for bag-check).
    /// </summary>
    public sealed class LanceLifeEventsIncidentBehavior : CampaignBehaviorBase
    {
        private const string LogCategory = "LanceLifeEvents";

        public static LanceLifeEventsIncidentBehavior Instance { get; private set; }

        private LanceLifeEventCatalog _catalog;
        private readonly Dictionary<string, Incident> _incidentsByEventId = new Dictionary<string, Incident>(StringComparer.OrdinalIgnoreCase);

        // Pending incident event if we couldn't start it at the moment trigger (not safe, MapState missing, another incident pending).
        private string _pendingEventId = string.Empty;
        private int _pendingAtHour = -1;

        public LanceLifeEventsIncidentBehavior()
        {
            Instance = this;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
            CampaignEvents.ConversationEnded.AddNonSerializedListener(this, OnConversationEnded);
        }

        public override void SyncData(IDataStore dataStore)
        {
            SaveLoadDiagnostics.SafeSyncData(this, dataStore, () =>
            {
                // Safe persistence: strings/ints only.
                dataStore.SyncData("ll_inc_pendingEventId", ref _pendingEventId);
                dataStore.SyncData("ll_inc_pendingAtHour", ref _pendingAtHour);
            });
        }

        private static bool IsEnabled()
        {
            var cfg = EnlistedConfig.LoadLanceLifeEventsConfig();
            if (cfg?.Enabled != true)
            {
                return false;
            }

            return cfg.IncidentChannel?.Enabled == true;
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            try
            {
                if (!IsEnabled())
                {
                    return;
                }

                EnsureCatalogLoaded();
                RegisterIncidentsForCatalog();
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Failed to register incident channel events", ex);
            }
        }

        private void OnMapEventEnded(MapEvent mapEvent)
        {
            try
            {
                if (!IsEnabled())
                {
                    return;
                }

                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment?.IsEnlisted != true)
                {
                    return;
                }

                if (mapEvent == null || !mapEvent.IsPlayerMapEvent)
                {
                    return;
                }

                TryTriggerIncidentMoment("LeavingBattle", enlistment);
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Incident channel LeavingBattle hook failed", ex);
            }
        }

        private void OnHourlyTick()
        {
            try
            {
                if (!IsEnabled())
                {
                    return;
                }

                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment?.IsEnlisted != true)
                {
                    return;
                }

                // Retry pending incidents (best effort)
                TryFirePending(enlistment);

                var ctx = Campaign.Current?.CurrentMenuContext;
                var menuId = ctx?.GameMenu?.StringId ?? string.Empty;
                if (string.Equals(menuId, "town_wait_menus", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(menuId, "village_wait_menus", StringComparison.OrdinalIgnoreCase))
                {
                    TryTriggerIncidentMoment("WaitingInSettlement", enlistment);
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Incident channel HourlyTick hook failed", ex);
            }
        }

        private void OnConversationEnded(IEnumerable<CharacterObject> conversationCharacters)
        {
            try
            {
                if (!IsEnabled())
                {
                    return;
                }

                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment?.IsEnlisted != true)
                {
                    return;
                }

                // Best-effort proxy for "leaving encounter"
                if (PlayerEncounter.Current == null || !PlayerEncounter.LeaveEncounter)
                {
                    return;
                }

                TryTriggerIncidentMoment("LeavingEncounter", enlistment);
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Incident channel LeavingEncounter hook failed", ex);
            }
        }

        private void EnsureCatalogLoaded()
        {
            if (_catalog != null)
            {
                return;
            }

            _catalog = LanceLifeEventCatalogLoader.LoadCatalog();
        }

        private void RegisterIncidentsForCatalog()
        {
            if (_catalog?.Events == null || _catalog.Events.Count == 0)
            {
                return;
            }

            var incidentEvents = _catalog.Events
                .Where(e => e != null &&
                            string.Equals(e.Delivery?.Channel, "incident", StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(e.Delivery?.Method, "automatic", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (incidentEvents.Count == 0)
            {
                return;
            }

            foreach (var evt in incidentEvents)
            {
                if (evt == null || string.IsNullOrWhiteSpace(evt.Id))
                {
                    continue;
                }

                if (_incidentsByEventId.ContainsKey(evt.Id))
                {
                    continue;
                }

                var incident = TryCreateIncident(evt);
                if (incident != null)
                {
                    _incidentsByEventId[evt.Id] = incident;
                }
            }

            ModLogger.Info(LogCategory, $"Incident channel registered: {_incidentsByEventId.Count} incident-backed events.");
        }

        private static Incident TryCreateIncident(LanceLifeEventDefinition evt)
        {
            try
            {
                if (evt == null)
                {
                    return null;
                }

                // Register presumed object so it can be referenced by MapState.NextIncident.
                var incident = Game.Current?.ObjectManager?.RegisterPresumedObject(new Incident(evt.Id));
                if (incident == null)
                {
                    return null;
                }

                var trigger = ParseIncidentTrigger(evt.Delivery?.IncidentTrigger);

                // Title/body are TextObject-tagged strings so they localize and support placeholders.
                var title = BuildTextObjectTaggedString(evt.TitleId, evt.TitleFallback, "{=ll_default_title}Lance Activity");
                var body = BuildTextObjectTaggedString(evt.SetupId, evt.SetupFallback, string.Empty);

                incident.Initialize(
                    title,
                    body,
                    trigger,
                    IncidentsCampaignBehaviour.IncidentType.TroopSettlementRelation,
                    CampaignTime.Days(365f),
                    _ => true);

                foreach (var opt in evt.Options ?? new List<LanceLifeEventOptionDefinition>())
                {
                    if (opt == null || string.IsNullOrWhiteSpace(opt.Id))
                    {
                        continue;
                    }

                    var optionText = BuildTextObjectTaggedString(opt.TextId, opt.TextFallback, "{=ll_default_continue}Continue");
                    incident.AddOption(optionText, new List<IncidentEffect>
                    {
                        IncidentEffect.Custom(
                            condition: () => IsOptionSelectable(opt, EnlistmentBehavior.Instance),
                            consequence: () =>
                            {
                                var txt = LanceLifeEventEffectsApplier.ApplyAndGetResultText(evt, opt, EnlistmentBehavior.Instance, showResultMessage: false);
                                LanceLifeEventsStateBehavior.Instance?.MarkFired(evt);

                                if (string.IsNullOrWhiteSpace(txt))
                                {
                                    return new List<TextObject>();
                                }

                                return new List<TextObject> { new TextObject(txt) };
                            },
                            hint: _ => new List<TextObject>())
                    });
                }

                return incident;
            }
            catch (Exception ex)
            {
                ModLogger.Warn(LogCategory, $"Failed to create incident for event {evt?.Id}: {ex.Message}");
                return null;
            }
        }

        private void TryTriggerIncidentMoment(string triggerName, EnlistmentBehavior enlistment)
        {
            if (string.IsNullOrWhiteSpace(triggerName) || enlistment == null)
            {
                return;
            }

            EnsureCatalogLoaded();
            RegisterIncidentsForCatalog();

            // If there is already a pending incident, don't stack.
            if (!string.IsNullOrWhiteSpace(_pendingEventId))
            {
                return;
            }

            var candidates = _catalog?.Events?
                .Where(e => e != null &&
                            string.Equals(e.Delivery?.Channel, "incident", StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(e.Delivery?.Method, "automatic", StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(e.Delivery?.IncidentTrigger, triggerName, StringComparison.OrdinalIgnoreCase))
                .ToList() ?? new List<LanceLifeEventDefinition>();

            if (candidates.Count == 0)
            {
                return;
            }

            candidates.Sort((a, b) => string.Compare(a?.Id, b?.Id, StringComparison.OrdinalIgnoreCase));

            // Deterministic pick: stable for the day.
            var seed = unchecked((int)Math.Floor(CampaignTime.Now.ToDays) * 397) ^ candidates.Count;
            var r = new Random(seed);
            var picked = candidates[r.Next(candidates.Count)];
            if (picked == null)
            {
                return;
            }

            if (!IsIncidentEventEligible(picked, enlistment))
            {
                return;
            }

            // Try start immediately; otherwise queue.
            if (!TryStartIncidentNow(picked.Id))
            {
                _pendingEventId = picked.Id;
                _pendingAtHour = (int)Math.Floor(CampaignTime.Now.ToDays * 24f);
                ModLogger.Info(LogCategory, $"Queued incident-channel event: {_pendingEventId} (trigger={triggerName})");
            }
        }

        private void TryFirePending(EnlistmentBehavior enlistment)
        {
            if (string.IsNullOrWhiteSpace(_pendingEventId))
            {
                return;
            }

            // Drop stale pending after 24h
            var nowHour = (int)Math.Floor(CampaignTime.Now.ToDays * 24f);
            if (_pendingAtHour >= 0 && nowHour - _pendingAtHour > 24)
            {
                ModLogger.Warn(LogCategory, $"Dropping pending incident due to timeout: {_pendingEventId}");
                _pendingEventId = string.Empty;
                _pendingAtHour = -1;
                return;
            }

            EnsureCatalogLoaded();
            var evt = _catalog?.FindById(_pendingEventId);
            if (evt == null || !IsIncidentEventEligible(evt, enlistment))
            {
                _pendingEventId = string.Empty;
                _pendingAtHour = -1;
                return;
            }

            if (TryStartIncidentNow(_pendingEventId))
            {
                _pendingEventId = string.Empty;
                _pendingAtHour = -1;
            }
        }

        private bool TryStartIncidentNow(string eventId)
        {
            if (string.IsNullOrWhiteSpace(eventId))
            {
                return false;
            }

            if (!LanceLifeEventTriggerEvaluator.IsAiSafe())
            {
                return false;
            }

            // Don't fire if another event popup is already showing
            if (LanceLifeEventInquiryPresenter.IsEventShowing)
            {
                return false;
            }

            var mapState = GameStateManager.Current?.LastOrDefault<MapState>();
            if (mapState == null)
            {
                return false;
            }

            if (mapState.NextIncident != null)
            {
                return false;
            }

            if (!_incidentsByEventId.TryGetValue(eventId, out var incident) || incident == null)
            {
                return false;
            }

            mapState.NextIncident = incident;
            ModLogger.Info(LogCategory, $"Started incident-channel event via MapState.NextIncident: {eventId}");
            return true;
        }

        private bool IsIncidentEventEligible(LanceLifeEventDefinition evt, EnlistmentBehavior enlistment)
        {
            if (evt == null || enlistment == null)
            {
                return false;
            }

            // One-time
            if (evt.Timing?.OneTime == true && LanceLifeEventsStateBehavior.Instance?.IsOneTimeFired(evt.Id) == true)
            {
                return false;
            }

            // Tier range
            var tier = enlistment.EnlistmentTier;
            var minTier = Math.Max(1, evt.Requirements?.Tier?.Min ?? 1);
            var maxTier = Math.Max(minTier, evt.Requirements?.Tier?.Max ?? 999);
            if (tier < minTier || tier > maxTier)
            {
                return false;
            }

            // Track filter (onboarding events must match player's current onboarding track)
            var evtTrack = (evt.Track ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(evtTrack))
            {
                var playerTrack = LanceLifeOnboardingBehavior.Instance?.Track ?? string.Empty;
                if (!string.Equals(evtTrack, playerTrack, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            // Cooldown
            var cooldownDays = Math.Max(0, evt.Timing?.CooldownDays ?? 0);
            if (cooldownDays > 0 && LanceLifeEventsStateBehavior.Instance != null)
            {
                var day = (int)Math.Floor(CampaignTime.Now.ToDays);
                if (LanceLifeEventsStateBehavior.Instance.TryGetCooldownDaysRemaining(evt.Id, cooldownDays, day, out _))
                {
                    return false;
                }
            }

            // Reuse the same trigger evaluator as inquiries.
            var eval = new LanceLifeEventTriggerEvaluator();
            return eval.AreTriggersSatisfied(evt, enlistment);
        }

        private static bool IsOptionSelectable(LanceLifeEventOptionDefinition opt, EnlistmentBehavior enlistment)
        {
            if (opt == null)
            {
                return false;
            }

            // Gold gating
            if (opt.Costs?.Gold > 0 && Hero.MainHero != null && Hero.MainHero.Gold < opt.Costs.Gold)
            {
                return false;
            }

            // Fatigue gating
            if (opt.Costs?.Fatigue > 0 && enlistment != null && enlistment.FatigueCurrent < opt.Costs.Fatigue)
            {
                return false;
            }

            return true;
        }

        private static string BuildTextObjectTaggedString(string id, string fallback, string defaultText)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                var txt = !string.IsNullOrWhiteSpace(fallback) ? fallback : (defaultText ?? string.Empty);

                // If the default text is already a TextObject tag (e.g. "{=some_id}Continue"),
                // keep it as-is so it can localize through the XML string table.
                if (!string.IsNullOrWhiteSpace(txt) && txt.StartsWith("{=", StringComparison.Ordinal))
                {
                    return txt;
                }
                return "{=!}" + txt;
            }

            var embeddedFallback = !string.IsNullOrWhiteSpace(fallback) ? fallback : (defaultText ?? string.Empty);
            return "{=" + id + "}" + embeddedFallback;
        }

        private static IncidentsCampaignBehaviour.IncidentTrigger ParseIncidentTrigger(string trigger)
        {
            if (string.IsNullOrWhiteSpace(trigger))
            {
                return IncidentsCampaignBehaviour.IncidentTrigger.EnteringTown;
            }

            if (Enum.TryParse(trigger.Trim(), ignoreCase: true, out IncidentsCampaignBehaviour.IncidentTrigger parsed))
            {
                return parsed;
            }

            return IncidentsCampaignBehaviour.IncidentTrigger.EnteringTown;
        }
    }
}


