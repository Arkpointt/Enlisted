using System;
using System.Collections.Generic;
using System.Linq;
using Enlisted.Features.Content;
using Enlisted.Features.Content.Models;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;
using Enlisted.Mod.Core.SaveSystem;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace Enlisted.Features.Orders.Behaviors
{
    /// <summary>
    /// Handles multi-day order progression with phase-based event injection.
    /// Orders progress through 4 phases per day (Dawn, Midday, Dusk, Night).
    /// At "slot" phases, events may fire based on world state and activity level.
    /// </summary>
    public class OrderProgressionBehavior : CampaignBehaviorBase
    {
        private const string LogCategory = "OrderProgression";

        /// <summary>
        /// Singleton instance for global access.
        /// </summary>
        public static OrderProgressionBehavior Instance { get; private set; }

        // Phase timing constants
        private const int DawnHour = 6;
        private const int MiddayHour = 12;
        private const int DuskHour = 18;
        private const int NightHour = 0;

        // Event chance constants
        private const float SlotBaseChance = 0.15f;      // 15% base chance for slot phases
        private const float HighSlotBaseChance = 0.35f;  // 35% base chance for slot! phases

        // Activity level multipliers
        private static readonly Dictionary<ActivityLevel, float> ActivityMultipliers = new()
        {
            { ActivityLevel.Quiet, 0.3f },
            { ActivityLevel.Routine, 0.6f },
            { ActivityLevel.Active, 1.0f },
            { ActivityLevel.Intense, 1.5f }
        };

        // State tracking
        private int _lastProcessedHour = -1;
        private int _lastProcessedDay = -1;
        private readonly HashSet<string> _recentlyFiredEvents = new();
        private CampaignTime _lastEventCooldownClear = CampaignTime.Zero;

        public OrderProgressionBehavior()
        {
            Instance = this;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            SaveLoadDiagnostics.SafeSyncData(this, dataStore, () =>
            {
                dataStore.SyncData("orderProg_lastHour", ref _lastProcessedHour);
                dataStore.SyncData("orderProg_lastDay", ref _lastProcessedDay);
            });
        }

        /// <summary>
        /// Called every game hour. Checks if we're at a phase transition and processes it.
        /// </summary>
        private void OnHourlyTick()
        {
            // Only process if player is enlisted
            if (EnlistmentBehavior.Instance?.IsEnlisted != true)
            {
                return;
            }

            // Only process if there's an active (accepted) order
            var orderManager = OrderManager.Instance;
            if (orderManager == null || !orderManager.IsOrderActive())
            {
                return;
            }

            var currentHour = (int)CampaignTime.Now.CurrentHourInDay;
            var currentDay = (int)CampaignTime.Now.GetDayOfYear;

            // Check if we're at a phase hour and haven't processed it yet
            if (IsPhaseHour(currentHour) && !HasProcessedPhase(currentHour, currentDay))
            {
                ProcessPhase(currentHour);
                MarkPhaseProcessed(currentHour, currentDay);
            }

            // Clear event cooldowns periodically (every 7 days)
            ClearOldEventCooldowns();
        }

        /// <summary>
        /// Checks if the given hour is a phase transition hour.
        /// </summary>
        private static bool IsPhaseHour(int hour)
        {
            return hour == DawnHour || hour == MiddayHour || hour == DuskHour || hour == NightHour;
        }

        /// <summary>
        /// Checks if we've already processed this phase today.
        /// </summary>
        private bool HasProcessedPhase(int hour, int day)
        {
            return _lastProcessedHour == hour && _lastProcessedDay == day;
        }

        /// <summary>
        /// Marks the current phase as processed.
        /// </summary>
        private void MarkPhaseProcessed(int hour, int day)
        {
            _lastProcessedHour = hour;
            _lastProcessedDay = day;
        }

        /// <summary>
        /// Processes the current phase - may trigger an event based on slot type and world state.
        /// </summary>
        private void ProcessPhase(int hour)
        {
            var phaseName = GetPhaseName(hour);
            var orderManager = OrderManager.Instance;
            var currentOrder = orderManager?.GetCurrentOrder();

            if (currentOrder == null)
            {
                return;
            }

            ModLogger.Debug(LogCategory, $"Processing {phaseName} phase for order: {currentOrder.Id}");

            // Determine if this is a slot phase (for now, treat Midday and Dusk as slot phases)
            var isSlotPhase = hour == MiddayHour || hour == DuskHour;
            var isHighSlot = hour == DuskHour; // Dusk is higher chance

            if (isSlotPhase)
            {
                TryFireOrderEvent(currentOrder, isHighSlot);
            }
        }

        /// <summary>
        /// Attempts to fire an order event based on world state and activity level.
        /// </summary>
        private void TryFireOrderEvent(Models.Order order, bool isHighSlot)
        {
            // Get world situation from orchestrator
            var worldSituation = ContentOrchestrator.Instance?.GetCurrentWorldSituation();
            var activityLevel = worldSituation?.ExpectedActivity ?? ActivityLevel.Routine;

            // Calculate event chance
            var baseChance = isHighSlot ? HighSlotBaseChance : SlotBaseChance;
            var multiplier = ActivityMultipliers.TryGetValue(activityLevel, out var mult) ? mult : 1.0f;
            var finalChance = baseChance * multiplier;

            // Roll for event
            var roll = MBRandom.RandomFloat;
            if (roll > finalChance)
            {
                ModLogger.Debug(LogCategory, $"Event roll failed: {roll:F2} > {finalChance:F2} (activity: {activityLevel})");
                return;
            }

            ModLogger.Debug(LogCategory, $"Event roll succeeded: {roll:F2} <= {finalChance:F2}");

            // Select and fire an event
            var selectedEvent = SelectOrderEvent(order.Id, worldSituation);
            if (selectedEvent != null)
            {
                FireOrderEvent(selectedEvent);
            }
        }

        /// <summary>
        /// Selects an appropriate event for the given order based on world state.
        /// </summary>
        private EventDefinition SelectOrderEvent(string orderId, WorldSituation worldSituation)
        {
            // Get all events for this order type
            var orderEvents = EventCatalog.GetEventsByOrderType(orderId).ToList();

            if (orderEvents.Count == 0)
            {
                ModLogger.Debug(LogCategory, $"No events found for order type: {orderId}");
                return null;
            }

            // Filter by world state requirements
            var eligibleEvents = FilterByWorldState(orderEvents, worldSituation);

            // Exclude recently fired events
            eligibleEvents = eligibleEvents.Where(e => !_recentlyFiredEvents.Contains(e.Id)).ToList();

            if (eligibleEvents.Count == 0)
            {
                ModLogger.Debug(LogCategory, $"No eligible events after filtering for order: {orderId}");
                return null;
            }

            // Random selection from eligible events
            var selectedIndex = MBRandom.RandomInt(eligibleEvents.Count);
            var selected = eligibleEvents[selectedIndex];

            ModLogger.Info(LogCategory, $"Selected order event: {selected.Id} for order: {orderId}");
            return selected;
        }

        /// <summary>
        /// Filters events by world state requirements.
        /// Order events use requirements.WorldState array for context-specific selection.
        /// </summary>
        private static List<EventDefinition> FilterByWorldState(List<EventDefinition> events, WorldSituation worldSituation)
        {
            if (worldSituation == null)
            {
                return events;
            }

            var currentWorldState = GetWorldStateString(worldSituation);

            return events.Where(e =>
            {
                // If no world_state requirements, event is always eligible
                if (e.Requirements?.WorldState == null || e.Requirements.WorldState.Count == 0)
                {
                    return true;
                }

                // Check if current world state matches any of the event's requirements
                var requiredStates = e.Requirements.WorldState
                    .Select(s => s.Trim().ToLowerInvariant())
                    .ToList();

                return requiredStates.Contains(currentWorldState.ToLowerInvariant());
            }).ToList();
        }

        /// <summary>
        /// Converts WorldSituation to a world_state string for matching.
        /// </summary>
        private static string GetWorldStateString(WorldSituation situation)
        {
            return situation.LordIs switch
            {
                LordSituation.PeacetimeGarrison => "peacetime_garrison",
                LordSituation.PeacetimeRecruiting => "peacetime_recruiting",
                LordSituation.WarMarching => "war_marching",
                LordSituation.WarActiveCampaign => "war_active_campaign",
                LordSituation.SiegeAttacking => "siege_attacking",
                LordSituation.SiegeDefending => "siege_defending",
                LordSituation.Defeated => "defeated",
                LordSituation.Captured => "captured",
                _ => "any"
            };
        }

        /// <summary>
        /// Fires the selected order event via EventDeliveryManager.
        /// </summary>
        private void FireOrderEvent(EventDefinition eventDef)
        {
            var deliveryManager = EventDeliveryManager.Instance;
            if (deliveryManager == null)
            {
                ModLogger.Warn(LogCategory, "EventDeliveryManager not available, cannot fire order event");
                return;
            }

            // Queue the event for delivery
            deliveryManager.QueueEvent(eventDef);

            // Record for cooldown
            _recentlyFiredEvents.Add(eventDef.Id);

            // Record with GlobalEventPacer
            GlobalEventPacer.RecordAutoEvent(eventDef.Id, eventDef.Category);

            ModLogger.Info(LogCategory, $"Fired order event: {eventDef.Id}");
        }

        /// <summary>
        /// Gets the phase name for logging.
        /// </summary>
        private static string GetPhaseName(int hour)
        {
            return hour switch
            {
                DawnHour => "Dawn",
                MiddayHour => "Midday",
                DuskHour => "Dusk",
                NightHour => "Night",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Clears old event cooldowns periodically.
        /// </summary>
        private void ClearOldEventCooldowns()
        {
            var daysSinceLastClear = (CampaignTime.Now - _lastEventCooldownClear).ToDays;
            if (daysSinceLastClear >= 7)
            {
                _recentlyFiredEvents.Clear();
                _lastEventCooldownClear = CampaignTime.Now;
                ModLogger.Debug(LogCategory, "Cleared order event cooldowns");
            }
        }
    }
}
