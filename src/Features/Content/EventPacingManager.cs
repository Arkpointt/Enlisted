using System;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Escalation;
using Enlisted.Features.Interface.Behaviors;
using Enlisted.Mod.Core.Config;
using Enlisted.Mod.Core.Logging;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace Enlisted.Features.Content
{
    /// <summary>
    /// Controls the pacing of narrative events using config-driven timing.
    /// Registers as a CampaignBehavior and uses the daily tick to check if it's time for a new event.
    /// When the window arrives, selects an event via EventSelector and queues it for delivery.
    /// Coordinates with GlobalEventPacer to enforce max_per_day and min_hours_between limits.
    /// All timing values are config-driven from enlisted_config.json → decision_events.pacing.
    /// </summary>
    public class EventPacingManager : CampaignBehaviorBase
    {
        private const string LogCategory = "EventPacing";

        // Track the last day we ran the tick to avoid double-processing
        private int _lastTickDayNumber = -1;

        // Cached config for pacing window
        private static EventPacingConfig _cachedConfig;

        public static EventPacingManager Instance { get; private set; }

        public EventPacingManager()
        {
            Instance = this;
        }

        private static EventPacingConfig Config => _cachedConfig ??= ConfigurationManager.LoadEventPacingConfig();

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            ModLogger.Info(LogCategory, "Event pacing manager registered for daily tick");
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Pacing state is stored in EscalationState (LastNarrativeEventTime, NextNarrativeEventWindow)
            // No additional sync needed here since EscalationManager handles the state persistence
        }

        /// <summary>
        /// Called once per in-game day. Checks if it's time to fire a narrative event.
        /// </summary>
        private void OnDailyTick()
        {
            try
            {
                // Only run when enlisted
                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment?.IsEnlisted != true)
                {
                    return;
                }

                // Prevent double-tick on same day
                var currentDay = (int)CampaignTime.Now.ToDays;
                if (currentDay == _lastTickDayNumber)
                {
                    return;
                }
                _lastTickDayNumber = currentDay;

                // Get escalation state for pacing timestamps
                var escalationState = EscalationManager.Instance?.State;
                if (escalationState == null)
                {
                    return;
                }

                // Grace period: don't fire events for the first few days after enlistment
                // Give player time to learn the systems before hitting them with narrative events
                // EXCEPT for onboarding events, which ARE the introduction and should fire immediately
                if (enlistment.EnlistmentDate != CampaignTime.Zero)
                {
                    var daysSinceEnlistment = (CampaignTime.Now - enlistment.EnlistmentDate).ToDays;
                    const int gracePeriodDays = 3; // Config: min days before first narrative event

                    // Allow onboarding events during grace period (they're the tutorial/intro)
                    if (daysSinceEnlistment < gracePeriodDays && !escalationState.IsOnboardingActive)
                    {
                        ModLogger.Debug(LogCategory,
                            $"Grace period active: {daysSinceEnlistment:F1}/{gracePeriodDays} days since enlistment");
                        return;
                    }
                }

                // Check for pending chain events first (highest priority)
                CheckPendingChainEvents(escalationState);

                // Initialize pacing window if never set
                if (escalationState.NextNarrativeEventWindow == CampaignTime.Zero)
                {
                    SetNextEventWindow(escalationState);
                    ModLogger.Debug(LogCategory, "Initialized first event window");
                    return;
                }

                // Check if we're past the event window
                if (CampaignTime.Now < escalationState.NextNarrativeEventWindow)
                {
                    var daysUntilWindow = (escalationState.NextNarrativeEventWindow - CampaignTime.Now).ToDays;
                    ModLogger.Debug(LogCategory, $"Not yet in event window (days remaining: {daysUntilWindow:F1})");
                    return;
                }

                // Time for an event - try to select and deliver one
                TryFireEvent(escalationState);
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Error in event pacing daily tick", ex);
            }
        }

        /// <summary>
        /// Checks for and fires any chain events that are ready to be delivered.
        /// Chain events have highest priority and fire before normal paced events.
        /// Also clears pending event hints from the news system when chain events fire.
        /// </summary>
        private void CheckPendingChainEvents(EscalationState escalationState)
        {
            var readyEvents = escalationState.PopReadyChainEvents();

            if (readyEvents.Count == 0)
            {
                return;
            }

            var deliveryManager = EventDeliveryManager.Instance;
            if (deliveryManager == null)
            {
                ModLogger.Warn(LogCategory, "EventDeliveryManager not available, cannot deliver chain events");
                return;
            }

            foreach (var eventId in readyEvents)
            {
                // Clear pending event context from news system when chain event fires
                EnlistedNewsBehavior.Instance?.ClearPendingEvent(eventId);

                var evt = EventCatalog.GetEvent(eventId);
                if (evt != null)
                {
                    ModLogger.Info(LogCategory, $"Firing scheduled chain event: {eventId}");
                    deliveryManager.QueueEvent(evt);
                }
                else
                {
                    ModLogger.Warn(LogCategory, $"Scheduled chain event not found: {eventId}");
                }
            }
        }

        /// <summary>
        /// Attempts to select and fire a narrative event.
        /// Checks GlobalEventPacer limits before firing to prevent event spam.
        /// </summary>
        private void TryFireEvent(EscalationState escalationState)
        {
            // Use "narrative" as the category for per-category cooldown tracking
            const string category = "narrative";

            // Check global pacing limits (max_per_day, min_hours_between, evaluation_hours, category cooldown)
            if (!GlobalEventPacer.CanFireAutoEvent("paced_narrative", category, out var blockReason))
            {
                ModLogger.Debug(LogCategory, $"Blocked by global pacing: {blockReason}");
                return;
            }

            var selectedEvent = EventSelector.SelectEvent();

            if (selectedEvent == null)
            {
                ModLogger.Debug(LogCategory, "No eligible event to fire, extending window");
                // No event available - try again tomorrow
                escalationState.NextNarrativeEventWindow = CampaignTime.DaysFromNow(1);
                return;
            }

            // Queue the event for delivery
            var deliveryManager = EventDeliveryManager.Instance;
            if (deliveryManager == null)
            {
                ModLogger.Warn(LogCategory, "EventDeliveryManager not available, cannot deliver event");
                return;
            }

            deliveryManager.QueueEvent(selectedEvent);

            // Record in global pacer (tracks daily/weekly limits + category cooldown)
            GlobalEventPacer.RecordAutoEvent(selectedEvent.Id, category);

            // Record that this event was fired for cooldown tracking
            escalationState.RecordEventFired(selectedEvent.Id);
            if (selectedEvent.Timing.OneTime)
            {
                escalationState.RecordOneTimeEventFired(selectedEvent.Id);
            }

            // Update pacing timestamps
            escalationState.LastNarrativeEventTime = CampaignTime.Now;
            SetNextEventWindow(escalationState);

            ModLogger.Info(LogCategory, $"Fired event: {selectedEvent.Id}, next window set");
        }

        /// <summary>
        /// Sets the next event window to a random number of days from now (config-driven).
        /// Uses event_window_min_days and event_window_max_days from enlisted_config.json.
        /// </summary>
        private static void SetNextEventWindow(EscalationState escalationState)
        {
            var config = Config;
            var minDays = Math.Max(1, config.EventWindowMinDays);
            var maxDays = Math.Max(minDays, config.EventWindowMaxDays);

            // Random days between min and max (inclusive)
            var days = MBRandom.RandomInt(minDays, maxDays + 1);
            escalationState.NextNarrativeEventWindow = CampaignTime.DaysFromNow(days);

            ModLogger.Debug(LogCategory, $"Next event window: {days} days from now (config: {minDays}-{maxDays})");
        }

        /// <summary>
        /// Gets the number of days until the next event window for diagnostic display.
        /// Returns -1 if pacing is not initialized.
        /// </summary>
        public float GetDaysUntilNextWindow()
        {
            var escalationState = EscalationManager.Instance?.State;
            if (escalationState == null || escalationState.NextNarrativeEventWindow == CampaignTime.Zero)
            {
                return -1f;
            }

            var remaining = (escalationState.NextNarrativeEventWindow - CampaignTime.Now).ToDays;
            return Math.Max(0f, (float)remaining);
        }

        /// <summary>
        /// Forces an immediate event attempt, bypassing the pacing window.
        /// Used for testing and debug commands.
        /// </summary>
        public void ForceEventAttempt()
        {
            var escalationState = EscalationManager.Instance?.State;
            if (escalationState == null)
            {
                ModLogger.Warn(LogCategory, "Cannot force event - EscalationManager not available");
                return;
            }

            ModLogger.Info(LogCategory, "Forcing event attempt (debug)");
            TryFireEvent(escalationState);
        }

        /// <summary>
        /// Resets the pacing window to fire an event on the next daily tick.
        /// Used for testing.
        /// </summary>
        public void ResetPacingWindow()
        {
            var escalationState = EscalationManager.Instance?.State;
            if (escalationState == null)
            {
                return;
            }

            escalationState.NextNarrativeEventWindow = CampaignTime.Now;
            ModLogger.Debug(LogCategory, "Pacing window reset to now (will fire next tick)");
        }
    }
}

