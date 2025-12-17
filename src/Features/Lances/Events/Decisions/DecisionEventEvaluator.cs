using System;
using System.Collections.Generic;
using System.Linq;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Schedule.Behaviors;
using Enlisted.Mod.Core.Logging;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace Enlisted.Features.Lances.Events.Decisions
{
    /// <summary>
    /// Evaluates and selects Decision Events using 8 protection layers
    /// to create CK3-style pacing (quiet days, diverse events, no spam).
    /// </summary>
    public sealed class DecisionEventEvaluator
    {
        private const string LogCategory = "DecisionEvents";

        private readonly LanceLifeEventTriggerEvaluator _triggerEvaluator = new LanceLifeEventTriggerEvaluator();
        private readonly Random _random = new Random();

        /// <summary>
        /// Selects a decision event to fire based on current context and pacing rules.
        /// Returns null if no event should fire (quiet moment, limits reached, etc.).
        /// </summary>
        public LanceLifeEventDefinition SelectEvent(
            IReadOnlyList<LanceLifeEventDefinition> allEvents,
            DecisionEventState state,
            DecisionEventConfig config,
            EnlistmentBehavior enlistment)
        {
            if (allEvents == null || allEvents.Count == 0)
            {
                return null;
            }

            // Check global limits first (Protection Layer 3-4)
            if (!PassesGlobalLimits(state, config))
            {
                ModLogger.Debug(LogCategory, "Skipped: Global limits reached");
                return null;
            }

            // Optional quiet day chance (CK3 rhythm)
            if (config.Pacing.AllowQuietDays && ShouldBeQuiet(config))
            {
                ModLogger.Debug(LogCategory, "Skipped: Quiet moment");
                return null;
            }

            // Filter to eligible events (Protection Layers 1-2, 5-8)
            var eligible = FilterToEligible(allEvents, state, config, enlistment);

            if (eligible.Count == 0)
            {
                ModLogger.Debug(LogCategory, "No eligible events after filtering");
                return null;
            }

            // Check for chain events first (they get priority)
            var chainEventId = state.GetNextChainEvent();
            if (!string.IsNullOrEmpty(chainEventId))
            {
                var chainEvent = eligible.FirstOrDefault(e => e.Id == chainEventId);
                if (chainEvent != null)
                {
                    ModLogger.Debug(LogCategory, $"Firing chain event: {chainEventId}");
                    return chainEvent;
                }
            }

            // Group by priority tier
            var maxPriority = eligible.Max(e => GetPriorityValue(e));
            var topTier = eligible
                .Where(e => GetPriorityValue(e) >= maxPriority - 10)
                .ToList();

            // Calculate weights for top tier
            var weighted = topTier
                .Select(e => new WeightedEvent
                {
                    Event = e,
                    Weight = CalculateWeight(e, config, enlistment)
                })
                .Where(w => w.Weight > 0)
                .ToList();

            if (weighted.Count == 0)
            {
                return null;
            }

            // Weighted random selection
            var selected = WeightedRandom(weighted);
            
            if (selected != null)
            {
                ModLogger.Debug(LogCategory, $"Selected event: {selected.Id} (from {weighted.Count} candidates)");
            }

            return selected;
        }

        /// <summary>
        /// Gets all player-initiated decisions that are currently available.
        /// These are shown in the Main Menu's Decisions section.
        /// </summary>
        public List<LanceLifeEventDefinition> GetAvailablePlayerDecisions(
            IReadOnlyList<LanceLifeEventDefinition> allEvents,
            DecisionEventState state,
            DecisionEventConfig config,
            EnlistmentBehavior enlistment)
        {
            if (allEvents == null || allEvents.Count == 0)
            {
                return new List<LanceLifeEventDefinition>();
            }

            // Filter to player-initiated events only
            var playerInitiated = allEvents
                .Where(e => e.Delivery?.Method == "player_initiated")
                .ToList();

            // Check each event's requirements (but not global limits - those only apply to auto-fire)
            var available = new List<LanceLifeEventDefinition>();

            foreach (var evt in playerInitiated)
            {
                if (PassesEventChecks(evt, state, config, enlistment))
                {
                    available.Add(evt);
                }
            }

            // Sort by priority (highest first)
            available.Sort((a, b) => GetPriorityValue(b).CompareTo(GetPriorityValue(a)));

            // Limit to configured max
            if (config.Menu.MaxVisibleDecisions > 0 && available.Count > config.Menu.MaxVisibleDecisions)
            {
                available = available.Take(config.Menu.MaxVisibleDecisions).ToList();
            }

            return available;
        }

        private List<LanceLifeEventDefinition> FilterToEligible(
            IReadOnlyList<LanceLifeEventDefinition> allEvents,
            DecisionEventState state,
            DecisionEventConfig config,
            EnlistmentBehavior enlistment)
        {
            var eligible = new List<LanceLifeEventDefinition>();

            foreach (var evt in allEvents)
            {
                // Only consider automatic events for auto-fire evaluation
                if (evt.Delivery?.Method != "automatic")
                {
                    continue;
                }

                // Only consider decision category events
                if (evt.Category != "decision")
                {
                    continue;
                }

                if (PassesEventChecks(evt, state, config, enlistment))
                {
                    eligible.Add(evt);
                }
            }

            return eligible;
        }

        private bool PassesEventChecks(
            LanceLifeEventDefinition evt,
            DecisionEventState state,
            DecisionEventConfig config,
            EnlistmentBehavior enlistment)
        {
            // Protection Layer 1: Individual cooldown
            if (!PassesIndividualCooldown(evt, state, config))
            {
                return false;
            }

            // Protection Layer 2: Category cooldown
            if (!PassesCategoryCooldown(evt, state, config))
            {
                return false;
            }

            // Protection Layer 5: One-time check
            if (!PassesOneTimeCheck(evt, state))
            {
                return false;
            }

            // Protection Layer 6: Max-per-term check
            if (!PassesMaxPerTermCheck(evt, state))
            {
                return false;
            }

            // Protection Layer 7: Mutual exclusion
            if (!PassesMutualExclusionCheck(evt, state))
            {
                return false;
            }

            // Protection Layer 8: Story flag blocking
            if (!PassesStoryFlagCheck(evt, state))
            {
                return false;
            }

            // Protection Layer 9 (Phase 6): Tier-based narrative access
            // A T1 peasant won't be invited hunting by the Lord
            if (!PassesNarrativeSourceCheck(evt, config, enlistment))
            {
                return false;
            }

            // Protection Layer 10: Formation-specific events
            // A cavalry soldier shouldn't see infantry-only training events
            if (!PassesFormationCheck(evt, enlistment))
            {
                return false;
            }

            // Standard trigger evaluation (escalation, time of day, tokens, etc.)
            if (!_triggerEvaluator.AreTriggersSatisfied(evt, enlistment))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Protection Layer 10: Formation requirement check.
        /// Events with requirements.formation set to a specific formation (infantry, cavalry, etc.)
        /// will only show to players in that formation.
        /// </summary>
        private bool PassesFormationCheck(LanceLifeEventDefinition evt, EnlistmentBehavior enlistment)
        {
            var reqFormation = evt.Requirements?.Formation ?? "any";
            if (string.IsNullOrWhiteSpace(reqFormation) || 
                string.Equals(reqFormation, "any", StringComparison.OrdinalIgnoreCase))
            {
                return true; // No formation requirement or "any" - all formations welcome
            }

            // Get player's current formation from duties behavior
            var playerFormation = Features.Assignments.Behaviors.EnlistedDutiesBehavior.Instance?.PlayerFormation ?? string.Empty;
            
            if (string.IsNullOrWhiteSpace(playerFormation))
            {
                // No formation assigned yet - allow general events only
                ModLogger.Debug(LogCategory, $"Event {evt.Id} requires formation '{reqFormation}' but player has no formation assigned");
                return false;
            }

            var matches = string.Equals(playerFormation, reqFormation, StringComparison.OrdinalIgnoreCase);
            if (!matches)
            {
                ModLogger.Debug(LogCategory, $"Event {evt.Id} requires formation '{reqFormation}' but player is '{playerFormation}'");
            }
            return matches;
        }

        // Protection Layer 1: Individual event cooldown
        private bool PassesIndividualCooldown(
            LanceLifeEventDefinition evt,
            DecisionEventState state,
            DecisionEventConfig config)
        {
            var cooldownDays = evt.Timing?.CooldownDays ?? config.Pacing.DefaultCooldownDays;
            if (cooldownDays <= 0)
            {
                return true;
            }

            var daysSince = state.GetDaysSinceEventFired(evt.Id);
            return daysSince >= cooldownDays;
        }

        // Protection Layer 2: Category cooldown
        private bool PassesCategoryCooldown(
            LanceLifeEventDefinition evt,
            DecisionEventState state,
            DecisionEventConfig config)
        {
            var category = evt.Category;
            if (string.IsNullOrEmpty(category))
            {
                return true;
            }

            var cooldownDays = config.Pacing.DefaultCategoryCooldownDays;
            if (cooldownDays <= 0)
            {
                return true;
            }

            var daysSince = state.GetDaysSinceCategoryFired(category);
            return daysSince >= cooldownDays;
        }

        // Protection Layers 3-4: Global limits (per-day, per-week, min-hours-between)
        private bool PassesGlobalLimits(DecisionEventState state, DecisionEventConfig config)
        {
            // Layer 3: Max per day
            if (state.FiredToday >= config.Pacing.MaxPerDay)
            {
                return false;
            }

            // Layer 4: Max per week
            if (state.FiredThisWeek >= config.Pacing.MaxPerWeek)
            {
                return false;
            }

            // Layer 4b: Min hours between events
            var hoursSince = state.HoursSinceLastEvent();
            if (hoursSince < config.Pacing.MinHoursBetween)
            {
                return false;
            }

            return true;
        }

        // Protection Layer 5: One-time events
        private bool PassesOneTimeCheck(LanceLifeEventDefinition evt, DecisionEventState state)
        {
            if (evt.Timing?.OneTime != true)
            {
                return true;
            }

            return !state.HasFiredOneTime(evt.Id);
        }

        // Protection Layer 6: Max-per-term
        private bool PassesMaxPerTermCheck(LanceLifeEventDefinition evt, DecisionEventState state)
        {
            var maxPerTerm = evt.Timing?.MaxPerTerm ?? 0;

            if (maxPerTerm <= 0)
            {
                return true; // 0 = unlimited
            }

            var firedCount = state.GetFiredThisTerm(evt.Id);
            return firedCount < maxPerTerm;
        }

        // Protection Layer 7: Mutual exclusion
        // Events with excludes list can't fire on same day as excluded events
        private bool PassesMutualExclusionCheck(LanceLifeEventDefinition evt, DecisionEventState state)
        {
            var excludes = evt.Timing?.Excludes;
            if (excludes == null || excludes.Count == 0)
            {
                return true;
            }

            foreach (var excludedId in excludes)
            {
                // Check if the excluded event fired today (within 1 day)
                var daysSince = state.GetDaysSinceEventFired(excludedId);
                if (daysSince < 1)
                {
                    return false; // Mutually excluded event fired today
                }
            }

            return true;
        }

        // Protection Layer 8: Story flag blocking
        // Uses triggers.none list - event blocked if ANY of these flags are active
        private bool PassesStoryFlagCheck(LanceLifeEventDefinition evt, DecisionEventState state)
        {
            // New schema: triggers.none contains flags that block this event
            var noneTriggers = evt.Triggers?.None;
            if (noneTriggers != null && noneTriggers.Count > 0)
            {
                foreach (var flag in noneTriggers)
                {
                    if (state.HasActiveFlag(flag))
                    {
                        return false; // Blocked by active flag
                    }
                }
            }

            // Legacy support: also check triggers.all for "not:flag:X" patterns
            var allTriggers = evt.Triggers?.All;
            if (allTriggers != null)
            {
                foreach (var trigger in allTriggers)
                {
                    if (trigger.StartsWith("not:flag:", StringComparison.OrdinalIgnoreCase))
                    {
                        var flagName = trigger.Substring(9);
                        if (state.HasActiveFlag(flagName))
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        // Protection Layer 9 (Phase 6): Tier-based narrative access
        // Ensures roleplay authenticity: a T1 peasant doesn't get invited to hunt with the Lord.
        // Uses the narrative_source field on events + tier_gates config.
        private bool PassesNarrativeSourceCheck(
            LanceLifeEventDefinition evt,
            DecisionEventConfig config,
            EnlistmentBehavior enlistment)
        {
            // If tier gating is disabled, allow all events
            if (config?.TierGates?.Enabled != true)
            {
                return true;
            }

            // No narrative source specified = available to all tiers
            var narrativeSource = evt.NarrativeSource;
            if (string.IsNullOrEmpty(narrativeSource))
            {
                return true;
            }

            // Get the minimum tier required for this narrative source
            var minTierForSource = config.TierGates.GetMinTierForSource(narrativeSource);

            // Get player's current tier
            var playerTier = enlistment?.EnlistmentTier ?? 1;

            // Check if player meets the tier requirement
            if (playerTier < minTierForSource)
            {
                ModLogger.Debug(LogCategory, $"Blocked by narrative source '{narrativeSource}': player T{playerTier} < required T{minTierForSource} for {evt.Id}");
                return false;
            }

            return true;
        }

        private bool ShouldBeQuiet(DecisionEventConfig config)
        {
            return _random.NextDouble() < config.Pacing.QuietDayChance;
        }

        private int GetPriorityValue(LanceLifeEventDefinition evt)
        {
            // Map string priority to numeric value
            var priority = evt.Timing?.Priority?.ToLowerInvariant() ?? "normal";
            return priority switch
            {
                "critical" => 100,
                "high" => 75,
                "normal" => 50,
                "low" => 25,
                _ => 50
            };
        }

        private int CalculateWeight(
            LanceLifeEventDefinition evt,
            DecisionEventConfig config,
            EnlistmentBehavior enlistment)
        {
            // Base weight (default 100)
            var weight = 100;

            // Activity context boost
            if (config.Activity.Enabled)
            {
                weight = ApplyActivityBoost(evt, weight, config);
            }

            // Game state modifier (traveling, camped, pre-battle, etc.)
            weight = ApplyGameStateModifier(evt, weight);

            // Ensure minimum weight
            return Math.Max(weight, 1);
        }

        private int ApplyActivityBoost(
            LanceLifeEventDefinition evt,
            int baseWeight,
            DecisionEventConfig config)
        {
            var triggers = evt.Triggers?.All;
            if (triggers == null || triggers.Count == 0)
            {
                return baseWeight;
            }

            // Check for current_activity:X tokens
            var currentActivity = GetCurrentActivity();
            if (!string.IsNullOrEmpty(currentActivity))
            {
                var activityToken = $"current_activity:{currentActivity}";
                if (triggers.Any(t => t.Equals(activityToken, StringComparison.OrdinalIgnoreCase)))
                {
                    return (int)(baseWeight * config.Activity.ActivityMatchBoost);
                }
            }

            // Check for on_duty:X tokens
            var currentDuty = GetCurrentDuty();
            if (!string.IsNullOrEmpty(currentDuty))
            {
                var dutyToken = $"on_duty:{currentDuty}";
                if (triggers.Any(t => t.Equals(dutyToken, StringComparison.OrdinalIgnoreCase)))
                {
                    return (int)(baseWeight * config.Activity.DutyMatchBoost);
                }
            }

            return baseWeight;
        }

        private int ApplyGameStateModifier(LanceLifeEventDefinition evt, int baseWeight)
        {
            // Get lord's current activity/state
            var mainParty = Campaign.Current?.MainParty;
            if (mainParty == null)
            {
                return baseWeight;
            }

            var modifier = 1.0f;

            // Traveling = fewer events (busy moving)
            if (mainParty.DefaultBehavior == AiBehavior.GoToSettlement ||
                mainParty.DefaultBehavior == AiBehavior.GoAroundParty)
            {
                modifier = 0.7f;
            }
            // In town/settlement = more events (downtime)
            else if (mainParty.CurrentSettlement != null)
            {
                modifier = 1.3f;
            }
            // In army and besieging = fewer events
            else if (mainParty.Army != null && mainParty.BesiegedSettlement != null)
            {
                modifier = 0.8f;
            }
            // Post-battle = more events (aftermath)
            // (would need to track recent battle - simplified for now)

            return (int)(baseWeight * modifier);
        }

        private string GetCurrentActivity()
        {
            var schedule = ScheduleBehavior.Instance;
            if (schedule == null)
            {
                return string.Empty;
            }

            // Get current scheduled block
            var currentBlock = schedule.GetCurrentActiveBlock();
            return currentBlock?.ActivityId ?? string.Empty;
        }

        private string GetCurrentDuty()
        {
            var enlistment = EnlistmentBehavior.Instance;
            // SelectedDuty is the duty ID (e.g., "scout", "field_medic")
            return enlistment?.SelectedDuty ?? string.Empty;
        }

        private LanceLifeEventDefinition WeightedRandom(List<WeightedEvent> weighted)
        {
            if (weighted.Count == 0)
            {
                return null;
            }

            var totalWeight = weighted.Sum(w => w.Weight);
            if (totalWeight <= 0)
            {
                return null;
            }

            var roll = _random.Next(totalWeight);
            var cumulative = 0;

            foreach (var item in weighted)
            {
                cumulative += item.Weight;
                if (roll < cumulative)
                {
                    return item.Event;
                }
            }

            // Fallback to last item
            return weighted[weighted.Count - 1].Event;
        }

        private sealed class WeightedEvent
        {
            public LanceLifeEventDefinition Event { get; set; }
            public int Weight { get; set; }
        }
    }
}
