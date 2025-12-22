using System;
using System.Collections.Generic;
using System.Linq;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Escalation;
using Enlisted.Mod.Core.Logging;
using Enlisted.Mod.Core.Triggers;
using TaleWorlds.CampaignSystem;

namespace Enlisted.Features.Content
{
    /// <summary>
    /// Manages decision availability, cooldowns, and gate checking.
    /// Provides filtered lists of available decisions for the menu system.
    /// </summary>
    public class DecisionManager : CampaignBehaviorBase
    {
        private const string LogCategory = "DecisionManager";

        public static DecisionManager Instance { get; private set; }

        public override void RegisterEvents()
        {
            Instance = this;
            
            // Initialize the decision catalog (loads from JSON via EventCatalog)
            DecisionCatalog.Initialize();
            
            ModLogger.Info(LogCategory, $"DecisionManager registered with {DecisionCatalog.DecisionCount} decisions " +
                $"({DecisionCatalog.PlayerInitiatedCount} player-initiated, {DecisionCatalog.AutomaticCount} automatic)");
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Decision cooldowns are stored in EscalationState via EventLastFired
            // No additional persistence needed here
        }

        /// <summary>
        /// Gets all player-initiated decisions for a specific menu section that are currently available.
        /// Filters by tier, time of day, cooldown, and other gates.
        /// </summary>
        public IReadOnlyList<DecisionAvailability> GetAvailableDecisionsForSection(string section)
        {
            var decisions = DecisionCatalog.GetDecisionsBySection(section);
            var result = new List<DecisionAvailability>();

            foreach (var decision in decisions)
            {
                var availability = CheckAvailability(decision);
                // Include all decisions so menu can show disabled ones with reason
                result.Add(availability);
            }

            return result;
        }

        /// <summary>
        /// Gets all automatic decisions that should trigger based on current context.
        /// These appear in the Opportunities section.
        /// </summary>
        public IReadOnlyList<DecisionAvailability> GetAvailableOpportunities()
        {
            // Automatic decisions ("Opportunities") require trigger evaluation (activity, flags, weekly_tick, etc.).
            // That logic is intentionally deferred; returning none keeps the menu clean until triggers are implemented.
            return Array.Empty<DecisionAvailability>();
        }

        /// <summary>
        /// Checks all availability gates for a decision and returns detailed status.
        /// </summary>
        public DecisionAvailability CheckAvailability(DecisionDefinition decision)
        {
            var result = new DecisionAvailability
            {
                Decision = decision,
                IsAvailable = true,
                IsVisible = true,
                UnavailableReason = null
            };

            if (decision == null)
            {
                result.IsAvailable = false;
                result.IsVisible = false;
                return result;
            }

            var enlistment = EnlistmentBehavior.Instance;
            var escalation = EscalationManager.Instance;

            // Gate 1: Must be enlisted
            if (enlistment?.IsEnlisted != true)
            {
                result.IsAvailable = false;
                result.IsVisible = false;
                result.UnavailableReason = "Not enlisted";
                return result;
            }

            // Gate 2: Tier requirement
            var playerTier = enlistment.EnlistmentTier;
            var minTier = decision.Requirements?.MinTier;
            var maxTier = decision.Requirements?.MaxTier;

            if (minTier.HasValue && playerTier < minTier.Value)
            {
                result.IsAvailable = false;
                result.UnavailableReason = $"Requires Tier {minTier.Value}+";
                return result;
            }

            if (maxTier.HasValue && playerTier > maxTier.Value)
            {
                result.IsAvailable = false;
                result.IsVisible = false; // Hide decisions above max tier
                result.UnavailableReason = $"Tier too high (max T{maxTier.Value})";
                return result;
            }

            // Gate 3: Time of day (if specified)
            if (decision.TimeOfDay != null && decision.TimeOfDay.Count > 0)
            {
                var currentTimeBlock = CampaignTriggerTrackerBehavior.Instance?.GetTimeBlock() ?? TimeBlock.Morning;
                var currentTimeStr = TimeBlockToString(currentTimeBlock);

                if (!decision.TimeOfDay.Any(t => t.Equals(currentTimeStr, StringComparison.OrdinalIgnoreCase)))
                {
                    result.IsAvailable = false;
                    result.UnavailableReason = $"Available during {string.Join("/", decision.TimeOfDay)}";
                    return result;
                }
            }

            // Gate 4: Cooldown
            var cooldownDays = decision.Timing?.CooldownDays ?? 0;
            if (cooldownDays > 0 && escalation?.State != null)
            {
                if (escalation.State.IsEventOnCooldown(decision.Id, cooldownDays))
                {
                    escalation.State.EventLastFired.TryGetValue(decision.Id, out var lastFired);
                    var daysSince = (CampaignTime.Now - lastFired).ToDays;
                    var daysRemaining = (int)Math.Ceiling(cooldownDays - daysSince);
                    result.IsAvailable = false;
                    result.UnavailableReason = daysRemaining == 1 
                        ? "Available tomorrow" 
                        : $"Available in {daysRemaining} days";
                    return result;
                }
            }

            // Gate 5: One-time check
            if (decision.Timing?.OneTime == true && escalation?.State != null)
            {
                if (escalation.State.HasOneTimeEventFired(decision.Id))
                {
                    result.IsAvailable = false;
                    result.IsVisible = false;
                    result.UnavailableReason = "Already completed";
                    return result;
                }
            }

            // Gate 6: Required flags
            if (decision.RequiredFlags != null && decision.RequiredFlags.Count > 0)
            {
                foreach (var flagName in decision.RequiredFlags)
                {
                    if (!escalation.State.HasFlag(flagName))
                    {
                        result.IsAvailable = false;
                        result.IsVisible = false; // Hide decisions that require missing flags
                        result.UnavailableReason = "Prerequisite not met";
                        return result;
                    }
                }
            }

            // Gate 7: Blocking flags
            if (decision.BlockingFlags != null && decision.BlockingFlags.Count > 0)
            {
                foreach (var flagName in decision.BlockingFlags)
                {
                    if (escalation.State.HasFlag(flagName))
                    {
                        result.IsAvailable = false;
                        result.IsVisible = false; // Hide decisions blocked by flags
                        result.UnavailableReason = "No longer available";
                        return result;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Records that a decision was selected, updating cooldown tracking.
        /// </summary>
        public void RecordDecisionSelected(string decisionId)
        {
            if (string.IsNullOrEmpty(decisionId))
            {
                return;
            }

            var escalation = EscalationManager.Instance;
            if (escalation?.State == null)
            {
                return;
            }

            escalation.State.RecordEventFired(decisionId);

            var decision = DecisionCatalog.GetDecision(decisionId);
            if (decision?.Timing?.OneTime == true)
            {
                escalation.State.RecordOneTimeEventFired(decisionId);
            }

            ModLogger.Info(LogCategory, $"Decision '{decisionId}' selected, cooldown recorded");
        }

        /// <summary>
        /// Gets a summary of available decisions per section for logging.
        /// </summary>
        public Dictionary<string, int> GetAvailableCountsBySection()
        {
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var section in new[] { "training", "social", "camp_life", "logistics", "intel" })
            {
                var available = GetAvailableDecisionsForSection(section)
                    .Count(d => d.IsAvailable);
                counts[section] = available;
            }

            counts["opportunities"] = GetAvailableOpportunities().Count;

            return counts;
        }

        private static string TimeBlockToString(TimeBlock block)
        {
            return block switch
            {
                TimeBlock.Morning => "morning",
                TimeBlock.Afternoon => "afternoon",
                TimeBlock.Dusk => "evening",  // Dusk maps to "evening" in JSON
                TimeBlock.Night => "night",
                _ => "any"
            };
        }
    }

    /// <summary>
    /// Availability status for a single decision.
    /// </summary>
    public class DecisionAvailability
    {
        public DecisionDefinition Decision { get; set; }

        /// <summary>
        /// True if the player can select this decision right now.
        /// </summary>
        public bool IsAvailable { get; set; }

        /// <summary>
        /// True if this decision should appear in the menu (even if disabled).
        /// False to hide completely (e.g., one-time already done, tier too high).
        /// </summary>
        public bool IsVisible { get; set; } = true;

        /// <summary>
        /// Human-readable reason why the decision is unavailable.
        /// Shown as tooltip when disabled.
        /// </summary>
        public string UnavailableReason { get; set; }
    }
}

