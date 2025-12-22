using System;
using System.Collections.Generic;
using System.Linq;
using Enlisted.Features.Escalation;
using Enlisted.Mod.Core.Logging;
using TaleWorlds.Core;

namespace Enlisted.Features.Content
{
    /// <summary>
    /// Selects narrative events using a weighted random algorithm.
    /// Filters events by requirements and cooldowns, then weights by role and context match.
    /// Role-matching events get 2x weight, context-matching events get 1.5x weight.
    /// </summary>
    public static class EventSelector
    {
        private const string LogCategory = "EventSelector";

        // Weighting multipliers for event selection
        private const float RoleMatchMultiplier = 2.0f;
        private const float ContextMatchMultiplier = 1.5f;
        private const float HighPriorityMultiplier = 1.5f;
        private const float CriticalPriorityMultiplier = 2.0f;

        /// <summary>
        /// Selects an appropriate event based on current player state, context, and cooldowns.
        /// Returns null if no eligible events are available.
        /// </summary>
        /// <returns>The selected EventDefinition, or null if none available.</returns>
        public static EventDefinition SelectEvent()
        {
            try
            {
                var escalationState = EscalationManager.Instance?.State;
                if (escalationState == null)
                {
                    ModLogger.Warn(LogCategory, "Cannot select event - EscalationManager not available");
                    return null;
                }

                // Get all loaded events
                var allEvents = EventCatalog.GetAllEvents();
                if (allEvents == null || allEvents.Count == 0)
                {
                    ModLogger.Debug(LogCategory, "No events loaded in catalog");
                    return null;
                }

                // Get current player state for filtering
                var playerRole = EventRequirementChecker.GetPlayerRole();
                var currentContext = EventRequirementChecker.GetCurrentContext();

                // Filter to eligible candidates
                var candidates = GetEligibleCandidates(allEvents, escalationState);

                if (candidates.Count == 0)
                {
                    ModLogger.Debug(LogCategory, "No eligible events after filtering");
                    return null;
                }

                // Apply weights and select
                var weightedCandidates = ApplyWeights(candidates, playerRole, currentContext);

                var selectedEvent = WeightedRandomSelect(weightedCandidates);

                if (selectedEvent != null)
                {
                    var selectedWeight = weightedCandidates.FirstOrDefault(w => w.Event == selectedEvent)?.Weight ?? 1f;
                    ModLogger.Info(LogCategory,
                        $"Selected {selectedEvent.Id} (weight: {selectedWeight:F1}, role: {selectedEvent.Requirements.Role}, context: {selectedEvent.Requirements.Context})");
                }

                return selectedEvent;
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Error selecting event", ex);
                return null;
            }
        }

        /// <summary>
        /// Filters events to those meeting all requirements and not on cooldown.
        /// Excludes events with category "decision" since those are handled separately by DecisionManager.
        /// </summary>
        private static List<EventDefinition> GetEligibleCandidates(
            IReadOnlyList<EventDefinition> allEvents,
            EscalationState escalationState)
        {
            var candidates = new List<EventDefinition>();

            foreach (var evt in allEvents)
            {
                // Skip decision events - these are handled by DecisionManager, not the event pacing system
                if (!string.IsNullOrEmpty(evt.Category) &&
                    evt.Category.Equals("decision", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Skip one-time events that have already fired
                if (evt.Timing.OneTime && escalationState.HasOneTimeEventFired(evt.Id))
                {
                    continue;
                }

                // Skip events on cooldown
                if (escalationState.IsEventOnCooldown(evt.Id, evt.Timing.CooldownDays))
                {
                    continue;
                }

                // Check all requirements
                if (!EventRequirementChecker.MeetsRequirements(evt.Requirements))
                {
                    continue;
                }

                candidates.Add(evt);
            }

            ModLogger.Debug(LogCategory, $"Filtered to {candidates.Count} eligible candidates from {allEvents.Count} total events");
            return candidates;
        }

        /// <summary>
        /// Applies weight multipliers based on role match, context match, and priority.
        /// </summary>
        private static List<WeightedEvent> ApplyWeights(
            List<EventDefinition> candidates,
            string playerRole,
            string currentContext)
        {
            var weighted = new List<WeightedEvent>();

            foreach (var evt in candidates)
            {
                var weight = 1.0f;

                // Role match bonus: events targeting player's current role get higher weight
                if (!string.IsNullOrEmpty(evt.Requirements.Role) &&
                    !evt.Requirements.Role.Equals("Any", StringComparison.OrdinalIgnoreCase) &&
                    evt.Requirements.Role.Equals(playerRole, StringComparison.OrdinalIgnoreCase))
                {
                    weight *= RoleMatchMultiplier;
                }

                // Context match bonus: events matching current campaign context get higher weight
                if (!string.IsNullOrEmpty(evt.Requirements.Context) &&
                    !evt.Requirements.Context.Equals("Any", StringComparison.OrdinalIgnoreCase) &&
                    evt.Requirements.Context.Equals(currentContext, StringComparison.OrdinalIgnoreCase))
                {
                    weight *= ContextMatchMultiplier;
                }

                // Priority bonus
                weight *= GetPriorityMultiplier(evt.Timing.Priority);

                weighted.Add(new WeightedEvent { Event = evt, Weight = weight });
            }

            return weighted;
        }

        /// <summary>
        /// Gets the weight multiplier for an event's priority level.
        /// </summary>
        private static float GetPriorityMultiplier(string priority)
        {
            return priority?.ToLowerInvariant() switch
            {
                "high" => HighPriorityMultiplier,
                "critical" => CriticalPriorityMultiplier,
                "low" => 0.5f,
                _ => 1.0f // "normal" or unspecified
            };
        }

        /// <summary>
        /// Performs weighted random selection from the list of candidates.
        /// Events with higher weights have proportionally higher chance of being selected.
        /// </summary>
        private static EventDefinition WeightedRandomSelect(List<WeightedEvent> weightedEvents)
        {
            if (weightedEvents == null || weightedEvents.Count == 0)
            {
                return null;
            }

            // Calculate total weight
            var totalWeight = weightedEvents.Sum(w => w.Weight);
            if (totalWeight <= 0)
            {
                return weightedEvents[0].Event;
            }

            // Roll a random value in the weight range
            var roll = MBRandom.RandomFloat * totalWeight;

            // Walk through candidates until we find the selected one
            var cumulative = 0f;
            foreach (var weighted in weightedEvents)
            {
                cumulative += weighted.Weight;
                if (roll <= cumulative)
                {
                    return weighted.Event;
                }
            }

            // Fallback (should not reach here normally)
            return weightedEvents[weightedEvents.Count - 1].Event;
        }

        /// <summary>
        /// Gets the number of eligible events for diagnostic purposes.
        /// </summary>
        public static int GetEligibleEventCount()
        {
            var escalationState = EscalationManager.Instance?.State;
            if (escalationState == null)
            {
                return 0;
            }

            var allEvents = EventCatalog.GetAllEvents();
            return GetEligibleCandidates(allEvents, escalationState).Count;
        }

        /// <summary>
        /// Internal class for tracking event weights during selection.
        /// </summary>
        private class WeightedEvent
        {
            public EventDefinition Event { get; set; }
            public float Weight { get; set; }
        }
    }
}

