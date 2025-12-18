using System;
using System.Collections.Generic;
using System.Linq;
using Enlisted.Features.Assignments.Behaviors;
using Enlisted.Features.Assignments.Core;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;
using TaleWorlds.CampaignSystem;

namespace Enlisted.Features.Lances.Events
{
    /// <summary>
    /// Phase 2: automatic event scheduler.
    ///
    /// Evaluates eligible automatic events on a coarse cadence, queues one, and fires it at the next ai_safe moment.
    /// Persistence is safe: only primitives and strings are stored (count + keys).
    /// </summary>
    public sealed class LanceLifeEventsAutomaticBehavior : CampaignBehaviorBase
    {
        private const string LogCategory = "LanceLifeEvents";

        private readonly LanceLifeEventTriggerEvaluator _triggerEvaluator = new LanceLifeEventTriggerEvaluator();

        // Persisted queue
        private string _queuedEventId = string.Empty;
        private int _queuedAtHour = -1;

        // Persisted per-event state
        // NOTE: cooldown + one-time state is shared via LanceLifeEventsStateBehavior.

        // Persisted global rate-limits
        private int _lastAutoFireHour = -1;
        private int _autoFiresToday;
        private int _autoFiresDayNumber = -1;
        private int _lastEvaluationHour = -1;
        private int _lastAutoFireDayNumber = -1; // Day number when last event fired (for day-based cooldown)

        private LanceLifeEventCatalog _catalog;

        public override void RegisterEvents()
        {
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            SaveLoadDiagnostics.SafeSyncData(this, dataStore, () =>
            {
                dataStore.SyncData("ll_evt_q_id", ref _queuedEventId);
                dataStore.SyncData("ll_evt_q_hour", ref _queuedAtHour);

                dataStore.SyncData("ll_evt_lastFireHour", ref _lastAutoFireHour);
                dataStore.SyncData("ll_evt_firesToday", ref _autoFiresToday);
                dataStore.SyncData("ll_evt_firesDay", ref _autoFiresDayNumber);
                dataStore.SyncData("ll_evt_lastEvalHour", ref _lastEvaluationHour);
                dataStore.SyncData("ll_evt_lastFireDay", ref _lastAutoFireDayNumber);

                if (dataStore.IsLoading)
                {
                    NormalizeLoadedState();
                }
            });
        }

        private void NormalizeLoadedState()
        {
            if (string.IsNullOrWhiteSpace(_queuedEventId))
            {
                _queuedEventId = string.Empty;
                _queuedAtHour = -1;
            }

            if (_autoFiresToday < 0)
            {
                _autoFiresToday = 0;
            }
            if (_autoFiresDayNumber < -1)
            {
                _autoFiresDayNumber = -1;
            }
            if (_lastAutoFireHour < -1)
            {
                _lastAutoFireHour = -1;
            }
            if (_lastEvaluationHour < -1)
            {
                _lastEvaluationHour = -1;
            }
            if (_lastAutoFireDayNumber < -1)
            {
                _lastAutoFireDayNumber = -1;
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

                // Ensure we reset daily rate limit counter when the day changes.
                var today = GetDayNumber();
                if (_autoFiresDayNumber != today)
                {
                    _autoFiresDayNumber = today;
                    _autoFiresToday = 0;
                }

                // If something is queued, try to fire it at a safe moment first.
                if (!string.IsNullOrWhiteSpace(_queuedEventId))
                {
                    TryFireQueued(enlistment);
                    return;
                }

                // If nothing queued, consider evaluating and queueing a new event.
                TryEvaluateAndQueue(enlistment);

                // If we queued and it's safe, fire immediately.
                if (!string.IsNullOrWhiteSpace(_queuedEventId))
                {
                    TryFireQueued(enlistment);
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Automatic scheduler hourly tick failed", ex);
            }
        }

        private bool IsEnabled()
        {
            var cfg = ConfigurationManager.LoadLanceLifeEventsConfig() ?? new LanceLifeEventsConfig();
            if (!cfg.Enabled)
            {
                return false;
            }

            return cfg.Automatic?.Enabled == true;
        }

        private void TryEvaluateAndQueue(EnlistmentBehavior enlistment)
        {
            var cfg = ConfigurationManager.LoadLanceLifeEventsConfig() ?? new LanceLifeEventsConfig();
            var auto = cfg.Automatic ?? new LanceLifeEventsAutomaticConfig();

            var nowHour = GetHourNumber();
            var cadence = Math.Max(1, auto.EvaluationCadenceHours);
            if (_lastEvaluationHour >= 0 && nowHour - _lastEvaluationHour < cadence)
            {
                return;
            }
            _lastEvaluationHour = nowHour;

            // Enlistment grace period: don't fire events for the first N days after enlisting
            // This gives players time to learn systems and get oriented before events start
            var graceDays = Math.Max(0, auto.EnlistmentGraceDays);
            if (graceDays > 0)
            {
                var daysSinceEnlistment = LanceLifeOnboardingBehavior.Instance?.DaysSinceEnlistment ?? int.MaxValue;
                if (daysSinceEnlistment < graceDays)
                {
                    return; // Too soon - player is still in grace period
                }
            }

            // Global rate limits
            var maxPerDay = Math.Max(0, auto.MaxEventsPerDay);
            if (maxPerDay > 0 && _autoFiresToday >= maxPerDay)
            {
                return;
            }

            // Day-based cooldown: minimum days between ANY automatic events (hard floor)
            var nowDay = GetDayNumber();
            var minDaysBetween = Math.Max(0, auto.MinDaysBetweenEvents);
            if (_lastAutoFireDayNumber >= 0 && nowDay - _lastAutoFireDayNumber < minDaysBetween)
            {
                return; // Too soon - minimum cooldown not met
            }
            
            // Target interval: events prefer to fire around this interval (soft target)
            // If we haven't reached target interval, only fire high-priority events
            var targetDays = Math.Max(minDaysBetween, auto.TargetDaysBetweenEvents);
            var daysSinceLastEvent = _lastAutoFireDayNumber >= 0 ? nowDay - _lastAutoFireDayNumber : targetDays;
            var isAtTargetInterval = daysSinceLastEvent >= targetDays;

            EnsureCatalogLoaded();
            if (_catalog == null || _catalog.Events.Count == 0)
            {
                return;
            }

            var picked = PickBestEligibleAutomaticEvent(enlistment, isAtTargetInterval);
            if (picked == null)
            {
                return;
            }

            _queuedEventId = picked.Id;
            _queuedAtHour = nowHour;
            ModLogger.Info(LogCategory, $"Queued automatic Lance Life Event: {picked.Id} (category={picked.Category}, targetReached={isAtTargetInterval})");
        }

        private void TryFireQueued(EnlistmentBehavior enlistment)
        {
            var cfg = ConfigurationManager.LoadLanceLifeEventsConfig() ?? new LanceLifeEventsConfig();
            var auto = cfg.Automatic ?? new LanceLifeEventsAutomaticConfig();

            var nowHour = GetHourNumber();
            var timeout = Math.Max(1, auto.QueueTimeoutHours);
            if (_queuedAtHour >= 0 && nowHour - _queuedAtHour > timeout)
            {
                ModLogger.Warn(LogCategory, $"Dropping queued event due to timeout: {_queuedEventId}");
                _queuedEventId = string.Empty;
                _queuedAtHour = -1;
                return;
            }

            if (!LanceLifeEventTriggerEvaluator.IsAiSafe())
            {
                return;
            }

            // Don't fire if another event popup is already showing
            if (LanceLifeEventInquiryPresenter.IsEventShowing || UI.ModernEventPresenter.IsEventShowing)
            {
                return;
            }

            EnsureCatalogLoaded();
            var evt = _catalog?.FindById(_queuedEventId);
            if (evt == null)
            {
                ModLogger.Warn(LogCategory, $"Dropping queued event (missing in catalog): {_queuedEventId}");
                _queuedEventId = string.Empty;
                _queuedAtHour = -1;
                return;
            }

            if (!IsEventEligibleNow(evt, enlistment))
            {
                // If it became ineligible (requirements changed), drop it instead of stalling the queue forever.
                ModLogger.Info(LogCategory, $"Dropping queued event (no longer eligible): {evt.Id}");
                _queuedEventId = string.Empty;
                _queuedAtHour = -1;
                return;
            }

            // Use modern UI for onboarding and escalation events, fallback to basic popup for others
            bool useModernUI = ShouldUseModernUI(evt);
            bool shown;
            
            if (useModernUI)
            {
                shown = UI.ModernEventPresenter.TryShowWithFallback(evt, enlistment, useModernUI: true);
            }
            else
            {
                shown = LanceLifeEventInquiryPresenter.TryShow(evt, enlistment);
            }
            
            if (!shown)
            {
                return;
            }

            MarkEventFired(evt);
            _queuedEventId = string.Empty;
            _queuedAtHour = -1;
        }

        private void EnsureCatalogLoaded()
        {
            if (_catalog != null)
            {
                return;
            }

            _catalog = LanceLifeEventCatalogLoader.LoadCatalog();
        }

        /// <summary>
        /// Pick the best eligible automatic event.
        /// If we're between min and target intervals (3-7 days), only high/critical priority events fire.
        /// Once target interval (7 days) is reached, normal priority events can also fire.
        /// </summary>
        private LanceLifeEventDefinition PickBestEligibleAutomaticEvent(EnlistmentBehavior enlistment, bool isAtTargetInterval)
        {
            var events = _catalog?.Events;
            if (events == null || events.Count == 0)
            {
                return null;
            }

            // Priority order per plan: onboarding → threshold → duty → general.
            var onboarding = LanceLifeOnboardingBehavior.Instance;
            var hasOnboardingContent = events.Any(e => string.Equals(e?.Category, "onboarding", StringComparison.OrdinalIgnoreCase));
            var cfg = ConfigurationManager.LoadLanceLifeEventsConfig();
            var incidentChannelEnabled = cfg?.IncidentChannel?.Enabled == true;

            // Between min and target interval: only high/critical priority events can fire
            // This prevents event spam while still allowing urgent events through
            bool IsHighPriority(LanceLifeEventDefinition e)
            {
                var priority = e?.Timing?.Priority?.ToLowerInvariant() ?? "normal";
                return priority == "high" || priority == "critical";
            }

            // Phase 4 rule: onboarding takes priority until complete.
            // Safety: if we don't have onboarding content yet (pre-Phase 5), do not block other categories.
            if (onboarding?.IsEnabled() == true && !onboarding.IsComplete && hasOnboardingContent)
            {
                var onboardingCandidates = events
                    .Where(e => e != null &&
                                string.Equals(e.Delivery?.Method, "automatic", StringComparison.OrdinalIgnoreCase) &&
                                string.Equals(e.Category, "onboarding", StringComparison.OrdinalIgnoreCase))
                    .Where(e => !incidentChannelEnabled ||
                                !string.Equals(e.Delivery?.Channel, "incident", StringComparison.OrdinalIgnoreCase))
                    .Where(e => IsEventEligibleNow(e, enlistment))
                    .Where(e => isAtTargetInterval || IsHighPriority(e)) // Respect target interval for normal priority
                    .ToList();

                return PickDeterministic(onboardingCandidates);
            }

            var orderedCategories = new[] { "onboarding", "threshold", "duty", "general" };
            foreach (var cat in orderedCategories)
            {
                var candidates = events
                    .Where(e => e != null &&
                                string.Equals(e.Delivery?.Method, "automatic", StringComparison.OrdinalIgnoreCase) &&
                                string.Equals(e.Category, cat, StringComparison.OrdinalIgnoreCase))
                    .Where(e => !incidentChannelEnabled ||
                                !string.Equals(e.Delivery?.Channel, "incident", StringComparison.OrdinalIgnoreCase))
                    .Where(e => IsEventEligibleNow(e, enlistment))
                    .Where(e => isAtTargetInterval || IsHighPriority(e)) // Respect target interval for normal priority
                    .ToList();

                var picked = PickDeterministic(candidates);
                if (picked != null)
                {
                    return picked;
                }
            }

            // Fallback: any other automatic category (still respect priority rules).
            var other = events
                .Where(e => e != null && string.Equals(e.Delivery?.Method, "automatic", StringComparison.OrdinalIgnoreCase))
                .Where(e => !incidentChannelEnabled ||
                            !string.Equals(e.Delivery?.Channel, "incident", StringComparison.OrdinalIgnoreCase))
                .Where(e => IsEventEligibleNow(e, enlistment))
                .Where(e => isAtTargetInterval || IsHighPriority(e)) // Respect target interval for normal priority
                .ToList();

            return PickDeterministic(other);
        }

        private LanceLifeEventDefinition PickDeterministic(List<LanceLifeEventDefinition> candidates)
        {
            if (candidates == null || candidates.Count == 0)
            {
                return null;
            }

            candidates.Sort((a, b) => string.Compare(a?.Id, b?.Id, StringComparison.OrdinalIgnoreCase));

            // Deterministic pseudo-random selection seeded by day + enlisted tier + count.
            var seed = unchecked(GetDayNumber() * 397) ^ (EnlistmentBehavior.Instance?.EnlistmentTier ?? 1) ^ candidates.Count;
            var r = new Random(seed);
            return candidates[r.Next(candidates.Count)];
        }

        private bool IsEventEligibleNow(LanceLifeEventDefinition evt, EnlistmentBehavior enlistment)
        {
            if (evt == null || enlistment == null)
            {
                return false;
            }

            // One-time
            var state = LanceLifeEventsStateBehavior.Instance;
            if (evt.Timing?.OneTime == true && state?.IsOneTimeFired(evt.Id) == true)
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

            // Formation requirement (from duties behavior)
            var reqFormation = (evt.Requirements?.Formation ?? "any").Trim();
            if (!string.IsNullOrWhiteSpace(reqFormation) && !string.Equals(reqFormation, "any", StringComparison.OrdinalIgnoreCase))
            {
                var formation = EnlistedDutiesBehavior.Instance?.PlayerFormation ?? string.Empty;
                if (!string.Equals(formation, reqFormation, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            // Duty requirement (from duties behavior)
            var reqDuty = (evt.Requirements?.Duty ?? "any").Trim();
            if (!string.IsNullOrWhiteSpace(reqDuty) && !string.Equals(reqDuty, "any", StringComparison.OrdinalIgnoreCase))
            {
                var duties = EnlistedDutiesBehavior.Instance?.ActiveDuties ?? new List<string>();
                if (!duties.Any(d => string.Equals(d, reqDuty, StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }
            }

            // Triggers
            if (!_triggerEvaluator.AreTriggersSatisfied(evt, enlistment))
            {
                return false;
            }

            // Cooldown
            var cooldownDays = Math.Max(0, evt.Timing?.CooldownDays ?? 0);
            if (cooldownDays > 0 && state != null)
            {
                var today = GetDayNumber();
                if (state.TryGetCooldownDaysRemaining(evt.Id, cooldownDays, today, out _))
                {
                    return false;
                }
            }

            return true;
        }

        private void MarkEventFired(LanceLifeEventDefinition evt)
        {
            if (evt == null)
            {
                return;
            }

            var today = GetDayNumber();
            LanceLifeEventsStateBehavior.Instance?.MarkFired(evt);

            var nowHour = GetHourNumber();
            _lastAutoFireHour = nowHour;
            _lastAutoFireDayNumber = today; // Track day for day-based cooldown

            if (_autoFiresDayNumber != today)
            {
                _autoFiresDayNumber = today;
                _autoFiresToday = 0;
            }

            _autoFiresToday++;
            
            ModLogger.Debug(LogCategory, $"Event fired - next eligible after {Math.Max(0, ConfigurationManager.LoadLanceLifeEventsConfig()?.Automatic?.MinDaysBetweenEvents ?? 3)} days");
        }

        private static int GetDayNumber()
        {
            return (int)Math.Floor(CampaignTime.Now.ToDays);
        }

        private static int GetHourNumber()
        {
            return (int)Math.Floor(CampaignTime.Now.ToDays * 24f);
        }

        /// <summary>
        /// Determine if this event should use modern UI.
        /// Onboarding and escalation events get modern UI for maximum impact.
        /// </summary>
        private static bool ShouldUseModernUI(LanceLifeEventDefinition evt)
        {
            if (evt == null)
            {
                return false;
            }

            var category = evt.Category?.ToLowerInvariant() ?? "";
            
            // Use modern UI for:
            // 1. Onboarding events (first impressions matter!)
            // 2. Escalation events (dramatic threshold moments)
            return category == "onboarding" || category == "escalation";
        }
    }
}


