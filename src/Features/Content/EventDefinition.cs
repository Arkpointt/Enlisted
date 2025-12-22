using System.Collections.Generic;

namespace Enlisted.Features.Content
{
    /// <summary>
    /// Represents a narrative event that can be triggered based on context, role, and escalation state.
    /// Events are loaded from JSON files and presented to the player with multiple choice options.
    /// </summary>
    public class EventDefinition
    {
        /// <summary>
        /// Unique identifier for this event (e.g., "evt_scout_tracks").
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// XML string ID for the event title (localized via enlisted_strings.xml).
        /// </summary>
        public string TitleId { get; set; } = string.Empty;

        /// <summary>
        /// Inline fallback title text if XML localization lookup fails.
        /// </summary>
        public string TitleFallback { get; set; } = string.Empty;

        /// <summary>
        /// XML string ID for the event setup text (localized via enlisted_strings.xml).
        /// </summary>
        public string SetupId { get; set; } = string.Empty;

        /// <summary>
        /// Inline fallback setup text if XML localization lookup fails.
        /// </summary>
        public string SetupFallback { get; set; } = string.Empty;

        /// <summary>
        /// Event category for filtering and selection weighting.
        /// Examples: "escalation", "role", "universal", "muster", "crisis".
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Requirements that determine when this event can fire.
        /// </summary>
        public EventRequirements Requirements { get; set; } = new();

        /// <summary>
        /// Timing constraints for event firing (cooldowns, priority, one-time flags).
        /// </summary>
        public EventTiming Timing { get; set; } = new();

        /// <summary>
        /// Available options the player can choose from when this event fires.
        /// </summary>
        public List<EventOption> Options { get; set; } = [];
    }

    /// <summary>
    /// Defines when an event is eligible to fire based on player state and context.
    /// </summary>
    public class EventRequirements
    {
        /// <summary>
        /// Minimum enlistment tier required (1-9). Null means no minimum.
        /// </summary>
        public int? MinTier { get; set; }

        /// <summary>
        /// Maximum enlistment tier allowed (1-9). Null means no maximum.
        /// </summary>
        public int? MaxTier { get; set; }

        /// <summary>
        /// Campaign context requirement: "War", "Peace", "Siege", "Battle", "Town", "Any".
        /// </summary>
        public string Context { get; set; } = "Any";

        /// <summary>
        /// Player role requirement: "Scout", "Medic", "Engineer", "Officer", "Operative", "NCO", "Soldier", "Any".
        /// </summary>
        public string Role { get; set; } = "Any";

        /// <summary>
        /// Escalation thresholds required to trigger this event.
        /// Key: track name ("Scrutiny", "Discipline", "MedicalRisk"), Value: minimum level.
        /// </summary>
        public Dictionary<string, int> MinEscalation { get; set; } = [];

        /// <summary>
        /// Minimum skill levels required to see this event.
        /// Key: skill name (e.g., "Scouting"), Value: minimum level.
        /// </summary>
        public Dictionary<string, int> MinSkills { get; set; } = [];

        /// <summary>
        /// Minimum trait levels required to see this event.
        /// Key: trait name (e.g., "ScoutSkills"), Value: minimum level.
        /// </summary>
        public Dictionary<string, int> MinTraits { get; set; } = [];
    }

    /// <summary>
    /// Timing and pacing constraints for event firing.
    /// </summary>
    public class EventTiming
    {
        /// <summary>
        /// Minimum days between firing this specific event again.
        /// </summary>
        public int CooldownDays { get; set; } = 7;

        /// <summary>
        /// Event priority for selection: "low", "normal", "high", "critical".
        /// Higher priority events are more likely to be selected when eligible.
        /// </summary>
        public string Priority { get; set; } = "normal";

        /// <summary>
        /// If true, this event can only fire once per playthrough.
        /// </summary>
        public bool OneTime { get; set; }
    }

    /// <summary>
    /// Represents a single choice the player can make when an event fires.
    /// </summary>
    public class EventOption
    {
        /// <summary>
        /// Unique identifier for this option within the event.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// XML string ID for the option button text (localized).
        /// </summary>
        public string TextId { get; set; } = string.Empty;

        /// <summary>
        /// Inline fallback option text if XML localization lookup fails.
        /// </summary>
        public string TextFallback { get; set; } = string.Empty;

        /// <summary>
        /// XML string ID for the result text shown after choosing this option (localized).
        /// </summary>
        public string ResultTextId { get; set; } = string.Empty;

        /// <summary>
        /// Inline fallback result text if XML localization lookup fails.
        /// </summary>
        public string ResultTextFallback { get; set; } = string.Empty;

        /// <summary>
        /// Optional tooltip hint explaining consequences or requirements.
        /// </summary>
        public string Tooltip { get; set; } = string.Empty;

        /// <summary>
        /// Requirements to unlock this option. If not met, option is grayed out.
        /// </summary>
        public EventOptionRequirements Requirements { get; set; }

        /// <summary>
        /// Effects applied when the player chooses this option.
        /// </summary>
        public EventEffects Effects { get; set; } = new();

        /// <summary>
        /// Risk level for UI hints: "safe", "moderate", "risky", "dangerous".
        /// </summary>
        public string Risk { get; set; } = "safe";
    }

    /// <summary>
    /// Requirements that gate access to an event option.
    /// If requirements are not met, the option appears but is disabled.
    /// </summary>
    public class EventOptionRequirements
    {
        /// <summary>
        /// Minimum skill levels required to select this option.
        /// Key: skill name (e.g., "Scouting"), Value: minimum level.
        /// </summary>
        public Dictionary<string, int> MinSkills { get; set; } = [];

        /// <summary>
        /// Minimum trait levels required to select this option.
        /// Key: trait name (e.g., "ScoutSkills"), Value: minimum level.
        /// </summary>
        public Dictionary<string, int> MinTraits { get; set; } = [];

        /// <summary>
        /// Minimum enlistment tier required to select this option.
        /// </summary>
        public int? MinTier { get; set; }

        /// <summary>
        /// Required player role to select this option.
        /// </summary>
        public string Role { get; set; }
    }

    /// <summary>
    /// Effects applied when an event option is chosen.
    /// All effects are optional and can be positive or negative.
    /// </summary>
    public class EventEffects
    {
        /// <summary>
        /// Skill XP awards. Key: skill name, Value: XP amount (can be negative).
        /// </summary>
        public Dictionary<string, int> SkillXp { get; set; } = [];

        /// <summary>
        /// Trait XP awards. Key: trait name, Value: XP amount (can be negative).
        /// </summary>
        public Dictionary<string, int> TraitXp { get; set; } = [];

        /// <summary>
        /// Reputation change with the lord (-100 to +100 delta).
        /// </summary>
        public int? LordRep { get; set; }

        /// <summary>
        /// Reputation change with officers (-100 to +100 delta).
        /// </summary>
        public int? OfficerRep { get; set; }

        /// <summary>
        /// Reputation change with soldiers (-50 to +50 delta).
        /// </summary>
        public int? SoldierRep { get; set; }

        /// <summary>
        /// Scrutiny escalation change (0-10 scale delta).
        /// </summary>
        public int? Scrutiny { get; set; }

        /// <summary>
        /// Discipline escalation change (0-10 scale delta).
        /// </summary>
        public int? Discipline { get; set; }

        /// <summary>
        /// Medical risk escalation change (0-5 scale delta).
        /// </summary>
        public int? MedicalRisk { get; set; }

        /// <summary>
        /// Gold/denar change (positive = gain, negative = loss).
        /// </summary>
        public int? Gold { get; set; }

        /// <summary>
        /// Player HP change (positive = heal, negative = damage).
        /// </summary>
        public int? HpChange { get; set; }

        /// <summary>
        /// Number of troops lost from the party (rare, dramatic events).
        /// </summary>
        public int? TroopLoss { get; set; }

        /// <summary>
        /// Number of troops wounded in the party.
        /// </summary>
        public int? TroopWounded { get; set; }

        /// <summary>
        /// Food items lost from player inventory.
        /// </summary>
        public int? FoodLoss { get; set; }

        /// <summary>
        /// XP awarded to lord's T1-T3 troops (NCO training).
        /// </summary>
        public int? TroopXp { get; set; }

        /// <summary>
        /// Type of wound to apply: "Minor", "Serious", "Permanent", or null.
        /// </summary>
        public string ApplyWound { get; set; }

        /// <summary>
        /// ID of a follow-up event to trigger after this one completes.
        /// </summary>
        public string ChainEventId { get; set; }

        /// <summary>
        /// Company needs modifications.
        /// Key: need name ("Readiness", "Morale", "Supplies", "Equipment", "Rest").
        /// Value: delta to apply.
        /// </summary>
        public Dictionary<string, int> CompanyNeeds { get; set; } = [];

        /// <summary>
        /// Renown change for the player's clan.
        /// </summary>
        public int? Renown { get; set; }
    }
}

