using System.Collections.Generic;

namespace Enlisted.Features.Camp.Models
{
    /// <summary>
    /// Represents a camp life opportunity that the player can engage with.
    /// Opportunities are generated based on context and presented with natural language descriptions.
    /// </summary>
    public class CampOpportunity
    {
        /// <summary>Unique identifier for this opportunity (e.g., "opp_card_game").</summary>
        public string Id { get; set; }

        /// <summary>Category of opportunity for filtering and learning.</summary>
        public OpportunityType Type { get; set; }

        /// <summary>Localization key for the title.</summary>
        public string TitleId { get; set; }

        /// <summary>Fallback title text if localization fails.</summary>
        public string TitleFallback { get; set; }

        /// <summary>Localization key for the description.</summary>
        public string DescriptionId { get; set; }

        /// <summary>Fallback description text (the scene-setting natural language).</summary>
        public string DescriptionFallback { get; set; }

        /// <summary>Localization key for the action button text.</summary>
        public string ActionId { get; set; }

        /// <summary>Fallback action text (e.g., "Join the drill", "Sit in").</summary>
        public string ActionFallback { get; set; }

        /// <summary>Decision ID this opportunity triggers when engaged.</summary>
        public string TargetDecisionId { get; set; }

        /// <summary>Fitness score (0-100) calculated during generation.</summary>
        public float FitnessScore { get; set; }

        /// <summary>Required flags for this opportunity to appear.</summary>
        public List<string> RequiredFlags { get; set; } = new List<string>();

        /// <summary>Flags that block this opportunity from appearing.</summary>
        public List<string> BlockedByFlags { get; set; } = new List<string>();

        /// <summary>Minimum tier required.</summary>
        public int MinTier { get; set; } = 1;

        /// <summary>Maximum tier allowed (0 = no limit).</summary>
        public int MaxTier { get; set; }

        /// <summary>Cooldown in hours before this opportunity can appear again.</summary>
        public int CooldownHours { get; set; } = 12;

        /// <summary>Base fitness score before modifiers.</summary>
        public int BaseFitness { get; set; } = 50;

        /// <summary>Day phases when this opportunity is available.</summary>
        public List<string> ValidPhases { get; set; } = new List<string>();

        /// <summary>
        /// Phase when this activity should fire (Dawn/Midday/Dusk/Night).
        /// If null, uses the first valid phase or Midday as default.
        /// </summary>
        public string ScheduledPhase { get; set; }

        /// <summary>
        /// If true, this opportunity fires immediately when clicked instead of scheduling.
        /// Used for urgent or time-sensitive activities.
        /// </summary>
        public bool Immediate { get; set; }

        /// <summary>
        /// Gets the effective scheduled phase, falling back to first valid phase or Midday.
        /// </summary>
        public string GetEffectiveScheduledPhase()
        {
            if (!string.IsNullOrEmpty(ScheduledPhase))
            {
                return ScheduledPhase;
            }

            if (ValidPhases != null && ValidPhases.Count > 0)
            {
                return ValidPhases[0];
            }

            return "Midday";
        }

        /// <summary>Order compatibility settings for order-decision tension.</summary>
        public Dictionary<string, string> OrderCompatibility { get; set; } = new Dictionary<string, string>();

        /// <summary>Detection settings when player is on duty.</summary>
        public DetectionSettings Detection { get; set; }

        /// <summary>Consequences when caught doing this while on duty.</summary>
        public CaughtConsequences CaughtConsequences { get; set; }

        /// <summary>Localization key for the risk tooltip when on duty.</summary>
        public string TooltipRiskyId { get; set; }

        /// <summary>Fallback risk tooltip text.</summary>
        public string TooltipRiskyFallback { get; set; }

        /// <summary>Scheduled time if this is a timed activity (e.g., "tonight").</summary>
        public string ScheduledTime { get; set; }

        /// <summary>If true, this opportunity only appears on land (not at sea).</summary>
        public bool NotAtSea { get; set; }

        /// <summary>If true, this opportunity only appears at sea (not on land).</summary>
        public bool AtSea { get; set; }

        /// <summary>Gets the resolved title text (localized or fallback).</summary>
        public string GetTitle()
        {
            // TODO: Integrate with localization system
            return TitleFallback ?? Id;
        }

        /// <summary>Gets the resolved description text.</summary>
        public string GetDescription()
        {
            return DescriptionFallback ?? "";
        }

        /// <summary>Gets the resolved action text.</summary>
        public string GetAction()
        {
            return ActionFallback ?? "Engage";
        }

        /// <summary>
        /// Gets the order compatibility for a specific order type.
        /// Returns "available", "risky", or "blocked".
        /// </summary>
        public string GetOrderCompatibility(string orderType)
        {
            if (string.IsNullOrEmpty(orderType))
            {
                return "available";
            }

            if (OrderCompatibility.TryGetValue(orderType, out var compat))
            {
                return compat;
            }

            return OrderCompatibility.TryGetValue("default", out var defaultCompat)
                ? defaultCompat
                : "available";
        }
    }

    /// <summary>
    /// Detection settings for risky opportunities while on duty.
    /// </summary>
    public class DetectionSettings
    {
        /// <summary>Base chance of being caught (0.0 - 1.0).</summary>
        public float BaseChance { get; set; } = 0.25f;

        /// <summary>Modifier applied at night (usually negative = harder to detect).</summary>
        public float NightModifier { get; set; } = -0.15f;

        /// <summary>Modifier for high officer reputation (negative = less likely).</summary>
        public float HighRepModifier { get; set; } = -0.10f;
    }

    /// <summary>
    /// Consequences when caught doing a risky activity while on duty.
    /// </summary>
    public class CaughtConsequences
    {
        /// <summary>Officer reputation penalty.</summary>
        public int OfficerRep { get; set; } = -15;

        /// <summary>Discipline track increase.</summary>
        public int Discipline { get; set; } = 2;

        /// <summary>Chance that the current order fails due to absence.</summary>
        public float OrderFailureRisk { get; set; } = 0.20f;
    }
}
