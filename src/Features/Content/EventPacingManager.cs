using System;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Escalation;
using Enlisted.Features.Interface.Behaviors;
using Enlisted.Mod.Core.Logging;
using TaleWorlds.CampaignSystem;

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

        public static EventPacingManager Instance { get; private set; }

        public EventPacingManager()
        {
            Instance = this;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            ModLogger.Info(LogCategory, "Event pacing manager registered for daily tick");
        }

        public override void SyncData(IDataStore dataStore)
        {
            // No additional sync needed - EscalationManager handles all state persistence
            // Chain events are tracked in EscalationState.PendingChainEvents
        }

        /// <summary>
        /// Called once per in-game day. Checks if it's time to fire a narrative event.
        /// Chain events (scheduled follow-ups) have highest priority and fire first.
        /// Then paced narrative events are attempted based on timing windows.
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

                // NOTE: New enlistment grace period removed - let events flow immediately

                // Check for pending chain events first (highest priority)
                CheckPendingChainEvents(escalationState);

                // Attempt to fire a paced narrative event
                // Uses global pacing limits (max_per_day, min_hours_between, etc.)
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
        /// When orchestrator is enabled, uses world situation for fitness-scored event selection.
        /// </summary>
        private void TryFireEvent(EscalationState escalationState)
        {
            // Use "narrative" as the category for per-category cooldown tracking
            const string category = "narrative";

            // Check global pacing limits (max_per_day, min_hours_between, category cooldown)
            if (!GlobalEventPacer.CanFireAutoEvent("paced_narrative", category, out var blockReason))
            {
                ModLogger.Debug(LogCategory, $"Blocked by global pacing: {blockReason}");
                return;
            }

            // Get world situation from orchestrator for fitness scoring (if available)
            var worldSituation = ContentOrchestrator.Instance?.GetCurrentWorldSituation();
            var selectedEvent = EventSelector.SelectEvent(worldSituation);

            if (selectedEvent == null)
            {
                ModLogger.Debug(LogCategory, "No eligible event to fire");
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

            ModLogger.Info(LogCategory, $"Fired event: {selectedEvent.Id}");
        }

        /// <summary>
        /// Forces an immediate event attempt for testing and debug commands.
        /// Note: With orchestrator enabled, this bypasses world-state-driven pacing.
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
    }
}

