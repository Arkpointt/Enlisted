using System;
using System.Collections.Generic;
using System.Linq;
using Enlisted.Features.Assignments.Core;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;
using TaleWorlds.CampaignSystem;

namespace Enlisted.Features.Lances.Events.Decisions
{
    /// <summary>
    /// Decision Events behavior: CK3-style event delivery system.
    /// 
    /// Evaluates eligible decision events on specific hours, respects pacing limits,
    /// and fires events through the existing Lance Life Events infrastructure.
    /// 
    /// This behavior handles the "pushed" automatic decision events.
    /// Player-initiated decisions are surfaced through the Main Menu (Phase 4).
    /// </summary>
    public sealed class DecisionEventBehavior : CampaignBehaviorBase
    {
        private const string LogCategory = "DecisionEvents";

        public static DecisionEventBehavior Instance { get; private set; }

        private readonly DecisionEventEvaluator _evaluator = new DecisionEventEvaluator();

        // Persisted state (cooldowns, flags, counters)
        private DecisionEventState _state = new DecisionEventState();

        /// <summary>
        /// Public accessor for decision event state (used by menu tooltips).
        /// </summary>
        public DecisionEventState State => _state;

        // Cached config (reloaded periodically)
        private DecisionEventConfig _config;
        private int _lastConfigLoadHour = -1;

        // Queued event for firing at safe moment
        private string _queuedEventId = string.Empty;
        private int _queuedAtHour = -1;

        // Tracking
        private int _lastEvaluationHour = -1;
        private bool _initialized;

        public override void RegisterEvents()
        {
            Instance = this;

            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Sync the state object
            _state ??= new DecisionEventState();
            _state.SyncData(dataStore);

            // Sync queue state
            dataStore.SyncData("de_queuedEventId", ref _queuedEventId);
            dataStore.SyncData("de_queuedAtHour", ref _queuedAtHour);
            dataStore.SyncData("de_lastEvalHour", ref _lastEvaluationHour);
            dataStore.SyncData("de_initialized", ref _initialized);

            if (dataStore.IsLoading)
            {
                _queuedEventId ??= string.Empty;
                if (_queuedAtHour < -1)
                {
                    _queuedAtHour = -1;
                }
            }
        }

        /// <summary>
        /// Gets available player-initiated decisions for the Main Menu.
        /// </summary>
        public List<LanceLifeEventDefinition> GetAvailablePlayerDecisions()
        {
            var config = GetConfig();
            if (config == null || !config.Enabled || !config.Menu.Enabled)
            {
                return new List<LanceLifeEventDefinition>();
            }

            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment?.IsEnlisted != true)
            {
                return new List<LanceLifeEventDefinition>();
            }

            var catalog = LanceLifeEventRuntime.GetCatalog();
            var allEvents = catalog?.Events ?? new List<LanceLifeEventDefinition>();

            return _evaluator.GetAvailablePlayerDecisions(allEvents, _state, config, enlistment);
        }

        /// <summary>
        /// Fires a player-initiated decision. Called from Main Menu.
        /// </summary>
        public bool FirePlayerDecision(string eventId)
        {
            var config = GetConfig();
            if (config == null || !config.Enabled)
            {
                return false;
            }

            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment?.IsEnlisted != true)
            {
                return false;
            }

            var catalog = LanceLifeEventRuntime.GetCatalog();
            var evt = catalog?.Events?.FirstOrDefault(e => e.Id == eventId);

            if (evt == null)
            {
                ModLogger.Warn(LogCategory, $"Player decision not found: {eventId}");
                return false;
            }

            // Fire through existing infrastructure
            return FireEvent(evt, enlistment);
        }

        /// <summary>
        /// Sets a story flag with optional expiry.
        /// </summary>
        public void SetFlag(string flag, float expiryDays = 0f)
        {
            _state.SetFlag(flag, expiryDays);
            ModLogger.Debug(LogCategory, $"Flag set: {flag} (expires in {expiryDays} days)");
        }

        /// <summary>
        /// Clears a story flag.
        /// </summary>
        public void ClearFlag(string flag)
        {
            _state.ClearFlag(flag);
            ModLogger.Debug(LogCategory, $"Flag cleared: {flag}");
        }

        /// <summary>
        /// Queues a chain event to fire after a delay.
        /// </summary>
        public void QueueChainEvent(string eventId, float delayHours = 0f)
        {
            _state.QueueChainEvent(eventId, delayHours);
            ModLogger.Debug(LogCategory, $"Chain event queued: {eventId} (delay {delayHours}h)");
        }

        /// <summary>
        /// Resets per-term counters. Called when player re-enlists.
        /// </summary>
        public void OnPlayerReenlisted()
        {
            _state.ResetTermCounters();
            ModLogger.Debug(LogCategory, "Per-term counters reset for new enlistment");
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

                // Try to fire queued event first
                if (!string.IsNullOrEmpty(_queuedEventId))
                {
                    TryFireQueued(enlistment);
                    return;
                }

                // Check if this is an evaluation hour
                var currentHour = (int)CampaignTime.Now.CurrentHourInDay;
                var config = GetConfig();

                if (!IsEvaluationHour(currentHour, config))
                {
                    return;
                }

                // Don't re-evaluate same hour
                var hourNumber = GetHourNumber();
                if (_lastEvaluationHour == hourNumber)
                {
                    return;
                }

                _lastEvaluationHour = hourNumber;

                // Try to select and queue an event
                TryEvaluateAndQueue(enlistment, config);

                // If we queued something and it's safe to fire, fire now
                if (!string.IsNullOrEmpty(_queuedEventId))
                {
                    TryFireQueued(enlistment);
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Decision Events hourly tick failed", ex);
            }
        }

        private void OnDailyTick()
        {
            try
            {
                // Reset daily counter
                _state.ResetDailyCounter();

                // Reset weekly counter if week changed
                _state.ResetWeeklyCounter();

                // Expire old flags
                _state.ExpireFlags();

                ModLogger.Debug(LogCategory, $"Daily reset complete. Fired today: {_state.FiredToday}, this week: {_state.FiredThisWeek}");
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Decision Events daily tick failed", ex);
            }
        }

        private bool IsEnabled()
        {
            var config = GetConfig();
            return config?.Enabled == true;
        }

        private bool IsEvaluationHour(int currentHour, DecisionEventConfig config)
        {
            if (config?.Pacing?.EvaluationHours == null || config.Pacing.EvaluationHours.Count == 0)
            {
                // Default: 8 AM, 2 PM, 8 PM
                return currentHour == 8 || currentHour == 14 || currentHour == 20;
            }

            return config.Pacing.EvaluationHours.Contains((int)currentHour);
        }

        private void TryEvaluateAndQueue(EnlistmentBehavior enlistment, DecisionEventConfig config)
        {
            var catalog = LanceLifeEventRuntime.GetCatalog();
            var allEvents = catalog?.Events ?? new List<LanceLifeEventDefinition>();

            // Filter to decision events only
            var decisionEvents = allEvents
                .Where(e => e.Category == "decision" && e.Delivery?.Method == "automatic")
                .ToList();

            if (decisionEvents.Count == 0)
            {
                ModLogger.Debug(LogCategory, "No decision events in catalog");
                return;
            }

            // Select an event using the evaluator
            var selected = _evaluator.SelectEvent(decisionEvents, _state, config, enlistment);

            if (selected == null)
            {
                ModLogger.Debug(LogCategory, "No eligible decision event selected");
                return;
            }

            // Queue for firing
            _queuedEventId = selected.Id;
            _queuedAtHour = GetHourNumber();

            ModLogger.Info(LogCategory, $"Queued decision event: {selected.Id}");
        }

        private void TryFireQueued(EnlistmentBehavior enlistment)
        {
            if (string.IsNullOrEmpty(_queuedEventId))
            {
                return;
            }

            // Check timeout (24 hours max queue time)
            var hoursSinceQueued = GetHourNumber() - _queuedAtHour;
            if (hoursSinceQueued > 24)
            {
                ModLogger.Warn(LogCategory, $"Dropping queued decision event due to timeout: {_queuedEventId}");
                ClearQueue();
                return;
            }

            // Check if safe to fire (not in combat, not in dialogue, etc.)
            if (!IsSafeToShowEvent())
            {
                return;
            }

            // Find the event
            var catalog = LanceLifeEventRuntime.GetCatalog();
            var evt = catalog?.Events?.FirstOrDefault(e => e.Id == _queuedEventId);

            if (evt == null)
            {
                ModLogger.Warn(LogCategory, $"Queued event not found in catalog: {_queuedEventId}");
                ClearQueue();
                return;
            }

            // Fire it
            if (FireEvent(evt, enlistment))
            {
                ModLogger.Info(LogCategory, $"Fired decision event: {evt.Id}");
            }

            ClearQueue();
        }

        private bool FireEvent(LanceLifeEventDefinition evt, EnlistmentBehavior enlistment)
        {
            try
            {
                // Use the modern event presenter (matches existing infrastructure)
                var shown = Enlisted.Features.Lances.UI.ModernEventPresenter.TryShow(evt, enlistment);

                if (shown)
                {
                    // Record in state for cooldown and pacing tracking
                    _state.RecordEventFired(
                        evt.Id,
                        evt.Category,
                        evt.Timing?.OneTime == true,
                        hasMaxPerTerm: (evt.Timing?.MaxPerTerm ?? 0) > 0
                    );
                }

                return shown;
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, $"Failed to fire decision event: {evt.Id}", ex);
                return false;
            }
        }

        private void ClearQueue()
        {
            _queuedEventId = string.Empty;
            _queuedAtHour = -1;
        }

        private bool IsSafeToShowEvent()
        {
            // Check various unsafe conditions
            if (Campaign.Current == null)
            {
                return false;
            }

            // Not safe during combat
            if (TaleWorlds.CampaignSystem.Encounters.PlayerEncounter.Current != null)
            {
                return false;
            }

            // Not safe during menus (conversation, trade, etc.)
            if (Campaign.Current.ConversationManager?.IsConversationInProgress == true)
            {
                return false;
            }

            // Not safe if game is paused for other reasons
            if (Campaign.Current.TimeControlMode == CampaignTimeControlMode.Stop ||
                Campaign.Current.TimeControlMode == CampaignTimeControlMode.UnstoppableFastForwardForPartyWaitTime)
            {
                return false;
            }

            return true;
        }

        private DecisionEventConfig GetConfig()
        {
            var currentHour = GetHourNumber();

            // Reload config every hour
            if (_config == null || currentHour != _lastConfigLoadHour)
            {
                _config = ConfigurationManager.LoadDecisionEventsConfig() ?? new DecisionEventConfig();
                _lastConfigLoadHour = currentHour;
            }

            return _config;
        }

        private static int GetHourNumber()
        {
            return (int)Math.Floor(CampaignTime.Now.ToHours);
        }
    }
}

