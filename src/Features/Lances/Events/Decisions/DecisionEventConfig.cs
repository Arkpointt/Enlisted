using System.Collections.Generic;
using Newtonsoft.Json;

namespace Enlisted.Features.Lances.Events.Decisions
{
    /// <summary>
    /// Configuration for the Decision Events system.
    /// Loaded from enlisted_config.json → decision_events section.
    /// </summary>
    public sealed class DecisionEventConfig
    {
        /// <summary>
        /// Master enable/disable for the entire Decision Events system.
        /// </summary>
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Folder containing decision event JSON files (relative to ModuleData/Enlisted).
        /// </summary>
        [JsonProperty("events_folder")]
        public string EventsFolder { get; set; } = "Events";

        /// <summary>
        /// Global pacing settings to prevent event spam.
        /// </summary>
        [JsonProperty("pacing")]
        public DecisionPacingConfig Pacing { get; set; } = new DecisionPacingConfig();

        /// <summary>
        /// Settings for activity-aware event matching.
        /// </summary>
        [JsonProperty("activity")]
        public DecisionActivityConfig Activity { get; set; } = new DecisionActivityConfig();

        /// <summary>
        /// Settings for player-initiated decisions in the Main Menu.
        /// </summary>
        [JsonProperty("menu")]
        public DecisionMenuConfig Menu { get; set; } = new DecisionMenuConfig();

        /// <summary>
        /// Tier gates for narrative sources.
        /// Controls what rank the player must be to receive events from different sources.
        /// </summary>
        [JsonProperty("tier_gates")]
        public DecisionTierGatesConfig TierGates { get; set; } = new DecisionTierGatesConfig();
    }

    /// <summary>
    /// Global pacing configuration to create CK3-style quiet days.
    /// </summary>
    public sealed class DecisionPacingConfig
    {
        /// <summary>
        /// Maximum events that can fire per day (across all categories).
        /// Default: 2 events per day max.
        /// </summary>
        [JsonProperty("max_per_day")]
        public int MaxPerDay { get; set; } = 2;

        /// <summary>
        /// Maximum events that can fire per week.
        /// Default: 8 events per week (allows ~2-3 quiet days).
        /// </summary>
        [JsonProperty("max_per_week")]
        public int MaxPerWeek { get; set; } = 8;

        /// <summary>
        /// Minimum hours between any two events.
        /// Default: 6 hours (prevents back-to-back popups).
        /// </summary>
        [JsonProperty("min_hours_between")]
        public int MinHoursBetween { get; set; } = 6;

        /// <summary>
        /// Default cooldown in days for events without explicit cooldown.
        /// </summary>
        [JsonProperty("default_cooldown_days")]
        public int DefaultCooldownDays { get; set; } = 3;

        /// <summary>
        /// Default cooldown in days for categories without explicit cooldown.
        /// </summary>
        [JsonProperty("default_category_cooldown_days")]
        public int DefaultCategoryCooldownDays { get; set; } = 1;

        /// <summary>
        /// Hours at which event evaluation happens.
        /// Default: Morning (8), Afternoon (14), Evening (20).
        /// </summary>
        [JsonProperty("evaluation_hours")]
        public List<int> EvaluationHours { get; set; } = new List<int> { 8, 14, 20 };

        /// <summary>
        /// Priority threshold for "must-fire" events that bypass some limits.
        /// Events with priority >= this value only respect per-event cooldowns.
        /// </summary>
        [JsonProperty("high_priority_threshold")]
        public int HighPriorityThreshold { get; set; } = 90;

        /// <summary>
        /// Whether to allow quiet days (skip evaluation randomly).
        /// Default: true (creates natural rhythm).
        /// </summary>
        [JsonProperty("allow_quiet_days")]
        public bool AllowQuietDays { get; set; } = true;

        /// <summary>
        /// Chance per evaluation to skip and create a quiet moment.
        /// Default: 0.15 (15% chance to skip each evaluation).
        /// </summary>
        [JsonProperty("quiet_day_chance")]
        public float QuietDayChance { get; set; } = 0.15f;
    }

    /// <summary>
    /// Configuration for activity-aware event matching.
    /// </summary>
    public sealed class DecisionActivityConfig
    {
        /// <summary>
        /// Whether to enable activity context matching.
        /// When enabled, events tagged with current_activity:X get weight boost.
        /// </summary>
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Weight multiplier for events that match the current activity.
        /// Default: 2.0 (double weight for matching events).
        /// </summary>
        [JsonProperty("activity_match_boost")]
        public float ActivityMatchBoost { get; set; } = 2.0f;

        /// <summary>
        /// Weight multiplier for events that match the player's current duty.
        /// Default: 1.5 (50% weight boost for duty-specific events).
        /// </summary>
        [JsonProperty("duty_match_boost")]
        public float DutyMatchBoost { get; set; } = 1.5f;
    }

    /// <summary>
    /// Configuration for player-initiated decisions in Main Menu.
    /// </summary>
    public sealed class DecisionMenuConfig
    {
        /// <summary>
        /// Whether player-initiated decisions are enabled.
        /// </summary>
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Maximum decisions to show in the Main Menu at once.
        /// </summary>
        [JsonProperty("max_visible_decisions")]
        public int MaxVisibleDecisions { get; set; } = 5;

        /// <summary>
        /// Whether to show decisions that the player can't currently afford/meet requirements.
        /// If true, shows them grayed out with reason.
        /// </summary>
        [JsonProperty("show_unavailable")]
        public bool ShowUnavailable { get; set; } = true;
    }

    /// <summary>
    /// Configuration for tier-based narrative access (Phase 6).
    /// Controls which narrative sources are available at each player tier.
    /// A T1 peasant shouldn't hunt with the lord; these gates enforce that roleplay.
    /// </summary>
    public sealed class DecisionTierGatesConfig
    {
        /// <summary>
        /// Whether tier gating is enabled for narrative sources.
        /// </summary>
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Minimum tier for "lord" narrative source (direct lord invitations).
        /// Events where the Lord personally invites/summons the player.
        /// Default: T5 (Sergeant/Officer tier).
        /// </summary>
        [JsonProperty("lord_invitation")]
        public int LordInvitation { get; set; } = 5;

        /// <summary>
        /// Minimum tier for direct Lord interaction (acknowledgment, brief audiences).
        /// Lower than invitations - Lord may notice good soldiers.
        /// Default: T3 (Veteran tier).
        /// </summary>
        [JsonProperty("lord_direct")]
        public int LordDirect { get; set; } = 3;

        /// <summary>
        /// Minimum tier for "lance_leader" narrative source.
        /// Events where the Lance Leader issues orders or makes requests.
        /// Default: T1 (all enlisted soldiers).
        /// </summary>
        [JsonProperty("lance_leader_orders")]
        public int LanceLeaderOrders { get; set; } = 1;

        /// <summary>
        /// Minimum tier for noble events (hunts, feasts, councils).
        /// Default: T5 (Sergeant/Officer tier).
        /// </summary>
        [JsonProperty("noble_events")]
        public int NobleEvents { get; set; } = 5;

        /// <summary>
        /// Minimum tier for command decisions (affecting others).
        /// Default: T7 (Retinue/Commander tier).
        /// </summary>
        [JsonProperty("command_decisions")]
        public int CommandDecisions { get; set; } = 7;

        /// <summary>
        /// Minimum tier for "lance_mate" narrative source.
        /// Events initiated by fellow lance members.
        /// Default: T1 (all enlisted soldiers).
        /// </summary>
        [JsonProperty("lance_mate")]
        public int LanceMate { get; set; } = 1;

        /// <summary>
        /// Minimum tier for "situation" narrative source.
        /// Environmental or circumstantial events (no specific NPC initiator).
        /// Default: T1 (all enlisted soldiers).
        /// </summary>
        [JsonProperty("situation")]
        public int Situation { get; set; } = 1;

        /// <summary>
        /// Gets the minimum tier required for a given narrative source.
        /// </summary>
        public int GetMinTierForSource(string narrativeSource)
        {
            if (string.IsNullOrEmpty(narrativeSource))
            {
                return 1; // No source specified = available to all
            }

            return narrativeSource.ToLowerInvariant() switch
            {
                "lord" => LordInvitation,
                "lord_invitation" => LordInvitation,
                "lord_direct" => LordDirect,
                "lance_leader" => LanceLeaderOrders,
                "lance_leader_orders" => LanceLeaderOrders,
                "noble" => NobleEvents,
                "noble_events" => NobleEvents,
                "command" => CommandDecisions,
                "command_decisions" => CommandDecisions,
                "lance_mate" => LanceMate,
                "situation" => Situation,
                _ => 1 // Unknown source = available to all
            };
        }
    }
}
