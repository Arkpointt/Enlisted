using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Enlisted.Features.Lances.Events
{
    /// <summary>
    /// In-memory catalog of Lance Life Events loaded from ModuleData/Enlisted/Events/*.json.
    /// </summary>
    public sealed class LanceLifeEventCatalog
    {
        public List<LanceLifeEventDefinition> Events { get; } = new List<LanceLifeEventDefinition>();

        public LanceLifeEventDefinition FindById(string eventId)
        {
            if (string.IsNullOrWhiteSpace(eventId))
            {
                return null;
            }

            return Events.Find(e => e != null && string.Equals(e.Id, eventId, StringComparison.OrdinalIgnoreCase));
        }
    }

    public sealed class LanceLifeEventDefinition
    {
        [JsonProperty("id")] public string Id { get; set; } = string.Empty;
        [JsonProperty("category")] public string Category { get; set; } = string.Empty;

        // Onboarding track filter ("enlisted" | "officer" | "commander"). Optional for non-onboarding events.
        [JsonProperty("track")] public string Track { get; set; } = string.Empty;

        // Tier-based narrative access. This field describes who initiates the event.
        // Values: "lord", "lance_leader", "lance_mate", "situation"
        // Used to filter events based on player tier (e.g., T1-2 can't get lord invitations)
        [JsonProperty("narrative_source")] public string NarrativeSource { get; set; } = string.Empty;

        // Schema: optional metadata block (authoring aid).
        [JsonProperty("metadata")] public LanceLifeEventMetadata Metadata { get; set; } = new LanceLifeEventMetadata();

        [JsonProperty("delivery")] public LanceLifeEventDelivery Delivery { get; set; } = new LanceLifeEventDelivery();
        [JsonProperty("triggers")] public LanceLifeEventTriggers Triggers { get; set; } = new LanceLifeEventTriggers();
        [JsonProperty("requirements")] public LanceLifeEventRequirements Requirements { get; set; } = new LanceLifeEventRequirements();
        [JsonProperty("timing")] public LanceLifeEventTiming Timing { get; set; } = new LanceLifeEventTiming();

        // Engine-flat content. Prefer IDs, but allow fallback text for early authoring.
        [JsonProperty("titleId")] public string TitleId { get; set; } = string.Empty;
        [JsonProperty("setupId")] public string SetupId { get; set; } = string.Empty;
        [JsonProperty("title")] public string TitleFallback { get; set; } = string.Empty;
        [JsonProperty("setup")] public string SetupFallback { get; set; } = string.Empty;

        // Schema: nested content block (authoring truth).
        [JsonProperty("content")] public LanceLifeEventContentDefinition Content { get; set; } = new LanceLifeEventContentDefinition();

        // Engine uses a flat Options list; loader normalizes from Content.Options if present.
        [JsonProperty("options")] public List<LanceLifeEventOptionDefinition> Options { get; set; } = new List<LanceLifeEventOptionDefinition>();

        // Variant overrides, used primarily by onboarding (first_time/transfer/return).
        [JsonProperty("variants")]
        public Dictionary<string, LanceLifeEventVariantDefinition> Variants { get; set; } =
            new Dictionary<string, LanceLifeEventVariantDefinition>(StringComparer.OrdinalIgnoreCase);
    }

    public sealed class LanceLifeEventVariantDefinition
    {
        [JsonProperty("setupId")] public string SetupId { get; set; } = string.Empty;
        [JsonProperty("setup")] public string SetupFallback { get; set; } = string.Empty;

        [JsonProperty("options")] public List<LanceLifeEventOptionDefinition> Options { get; set; } = new List<LanceLifeEventOptionDefinition>();
    }

    public sealed class LanceLifeEventOptionDefinition
    {
        [JsonProperty("id")] public string Id { get; set; } = string.Empty;

        [JsonProperty("textId")] public string TextId { get; set; } = string.Empty;
        [JsonProperty("text")] public string TextFallback { get; set; } = string.Empty;
        [JsonProperty("tooltip")] public string Tooltip { get; set; } = string.Empty;
        [JsonProperty("condition")] public string Condition { get; set; } = string.Empty;

        // Some content uses a single outcome; some uses success/failure outcomes. Both are supported.
        [JsonProperty("resultTextId")] public string OutcomeTextId { get; set; } = string.Empty;
        [JsonProperty("resultText")] public string OutcomeTextFallback { get; set; } = string.Empty;
        [JsonProperty("resultSuccessTextId")] public string OutcomeSuccessTextId { get; set; } = string.Empty;
        [JsonProperty("resultFailureTextId")] public string OutcomeFailureTextId { get; set; } = string.Empty;

        // Schema-style outcome fields, normalized into the engine-flat result fields by the loader.
        [JsonProperty("outcome")] public string SchemaOutcome { get; set; } = string.Empty;
        [JsonProperty("outcome_failure")] public string SchemaOutcomeFailure { get; set; } = string.Empty;

        [JsonProperty("risk")] public string Risk { get; set; } = "safe";
        [JsonProperty("success_chance")] public float? SuccessChance { get; set; }
        [JsonProperty("risk_chance")] public int? RiskChance { get; set; }

        [JsonProperty("costs")] public LanceLifeEventCosts Costs { get; set; } = new LanceLifeEventCosts();
        [JsonProperty("rewards")] public LanceLifeEventRewards Rewards { get; set; } = new LanceLifeEventRewards();
        [JsonProperty("effects")] public LanceLifeEventEscalationEffects Effects { get; set; } = new LanceLifeEventEscalationEffects();

        // Success/failure effect overrides for risky options (used by some threshold events).
        [JsonProperty("effects_success")] public LanceLifeEventEscalationEffects EffectsSuccess { get; set; }
        [JsonProperty("effects_failure")] public LanceLifeEventEscalationEffects EffectsFailure { get; set; }

        [JsonProperty("injury")] public LanceLifeInjuryRoll Injury { get; set; }
        [JsonProperty("illness")] public LanceLifeIllnessRoll Illness { get; set; }

        // Schema injury format normalized into Injury/Illness by the loader.
        [JsonProperty("injury_risk")] public LanceLifeInjuryRiskSchema InjuryRisk { get; set; }

        // Event chains. Selecting this option can queue another event.
        // The chained event will fire after the specified delay (default: 12-36 hours)
        [JsonProperty("chains_to")] public string ChainsTo { get; set; } = string.Empty;

        // Delay in hours before the chained event fires (0 = use random default).
        [JsonProperty("chain_delay_hours")] public float ChainDelayHours { get; set; }

        // Story flags to set when this option is selected.
        // These can be checked by future events using triggers.all/any/none
        [JsonProperty("set_flags")] public List<string> SetFlags { get; set; } = new List<string>();

        // Story flags to clear when this option is selected.
        [JsonProperty("clear_flags")] public List<string> ClearFlags { get; set; } = new List<string>();

        // Duration in days for set_flags (0 = permanent until cleared).
        [JsonProperty("flag_duration_days")] public float FlagDurationDays { get; set; }

        // Onboarding stage advancement control.
        // If true and the event is an onboarding event, selecting this option advances the onboarding stage.
        [JsonProperty("advances_onboarding")] public bool AdvancesOnboarding { get; set; }

        // Reward Choices: Optional reward selection dialog shown after event outcome
        // Allows players to customize which skills level up, whether to take gold vs reputation, etc.
        [JsonProperty("reward_choices")] public LanceLifeRewardChoices RewardChoices { get; set; }
    }

    public sealed class LanceLifeEventDelivery
    {
        [JsonProperty("method")] public string Method { get; set; } = "automatic"; // automatic | player_initiated
        [JsonProperty("channel")] public string Channel { get; set; } = string.Empty; // menu | inquiry | incident
        [JsonProperty("incident_trigger")] public string IncidentTrigger { get; set; } = string.Empty; // LeavingBattle | WaitingInSettlement | LeavingEncounter | ...
        
        /// <summary>
        /// Schedule-based trigger for events fired during duty/activity execution.
        /// If set to "on_activity_execution", this event can fire when its associated schedule activity executes.
        /// The Schedule system will roll for this event based on event_pool configuration (typically 20% chance).
        /// Requires: requirements.duty or activity_trigger to be set.
        /// </summary>
        [JsonProperty("schedule_trigger")] public string ScheduleTrigger { get; set; } = string.Empty; // on_activity_execution | empty
        
        /// <summary>
        /// Optional: Specific activity ID that triggers this event (e.g., "work_detail", "patrol_duty").
        /// If empty, uses requirements.duty to determine which duty holder can trigger this event.
        /// Used with schedule_trigger = "on_activity_execution".
        /// </summary>
        [JsonProperty("activity_trigger")] public string ActivityTrigger { get; set; } = string.Empty;
        
        [JsonProperty("menu")] public string Menu { get; set; } = string.Empty;
        [JsonProperty("menu_section")] public string MenuSection { get; set; } = string.Empty; // training | tasks | social
    }

    public sealed class LanceLifeEventTriggers
    {
        [JsonProperty("all")] public List<string> All { get; set; } = new List<string>();
        [JsonProperty("any")] public List<string> Any { get; set; } = new List<string>();
        [JsonProperty("time_of_day")] public List<string> TimeOfDay { get; set; } = new List<string>();

        // Blocking flags. The event won't fire if any of these flags are active.
        [JsonProperty("none")] public List<string> None { get; set; } = new List<string>();

        // Range constraints for escalation tracks.
        [JsonProperty("escalation_requirements")] public LanceLifeEventEscalationRequirements EscalationRequirements { get; set; } =
            new LanceLifeEventEscalationRequirements();
    }

    public sealed class LanceLifeEventRequirements
    {
        [JsonProperty("duty")] public string Duty { get; set; } = "any";
        [JsonProperty("formation")] public string Formation { get; set; } = "any";
        [JsonProperty("tier")] public LanceLifeTierRange Tier { get; set; } = new LanceLifeTierRange();
    }

    public sealed class LanceLifeEventTiming
    {
        [JsonProperty("cooldown_days")] public int CooldownDays { get; set; }
        [JsonProperty("priority")] public string Priority { get; set; } = "normal"; // normal | high | critical
        [JsonProperty("one_time")] public bool OneTime { get; set; }

        // Max times this event can fire per enlistment term.
        [JsonProperty("max_per_term")] public int MaxPerTerm { get; set; }

        // List of event IDs that cannot fire on the same day as this event.
        [JsonProperty("excludes")] public List<string> Excludes { get; set; } = new List<string>();
    }

    public sealed class LanceLifeEventCosts
    {
        [JsonProperty("fatigue")] public int Fatigue { get; set; }
        [JsonProperty("gold")] public int Gold { get; set; }
        [JsonProperty("time_hours")] public int TimeHours { get; set; }

        // Escalation "costs" (treated as deltas when escalation is enabled).
        [JsonProperty("heat")] public int Heat { get; set; }
        [JsonProperty("discipline")] public int Discipline { get; set; }
    }

    public sealed class LanceLifeEventRewards
    {
        // Schema: rewards.xp
        [JsonProperty("xp")]
        public Dictionary<string, int> SchemaXp { get; set; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        [JsonProperty("skillXp")]
        public Dictionary<string, int> SkillXp { get; set; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        [JsonProperty("gold")] public int Gold { get; set; }
        [JsonProperty("fatigueRelief")] public int FatigueRelief { get; set; }
    }

    public sealed class LanceLifeEventEscalationEffects
    {
        [JsonProperty("heat")] public int Heat { get; set; }
        [JsonProperty("discipline")] public int Discipline { get; set; }
        [JsonProperty("lance_reputation")] public int LanceReputation { get; set; }
        [JsonProperty("medical_risk")] public int MedicalRisk { get; set; }

        // Schema: effects.fatigue_relief (compatibility mapping; engine also supports rewards.fatigueRelief).
        [JsonProperty("fatigue_relief")] public int FatigueRelief { get; set; }

        // Optional narrative/flag effects.
        [JsonProperty("formation")] public string Formation { get; set; } = string.Empty;
        [JsonProperty("character_tag")] public string CharacterTag { get; set; } = string.Empty;
        [JsonProperty("loyalty_tag")] public string LoyaltyTag { get; set; } = string.Empty;

        // Promotion and duty assignment effects.
        [JsonProperty("promotes")] public bool Promotes { get; set; }
        [JsonProperty("starter_duty")] public string StarterDuty { get; set; } = string.Empty;
    }

    public sealed class LanceLifeEventEscalationRequirements
    {
        [JsonProperty("heat")] public LanceLifeIntRange Heat { get; set; } = new LanceLifeIntRange();
        [JsonProperty("discipline")] public LanceLifeIntRange Discipline { get; set; } = new LanceLifeIntRange();
        [JsonProperty("lance_reputation")] public LanceLifeIntRange LanceReputation { get; set; } = new LanceLifeIntRange();
        [JsonProperty("medical_risk")] public LanceLifeIntRange MedicalRisk { get; set; } = new LanceLifeIntRange();
        
        // Pay tension threshold for pay-related events.
        [JsonProperty("pay_tension_min")] public int? PayTensionMin { get; set; }
        [JsonProperty("pay_tension_max")] public int? PayTensionMax { get; set; }
    }

    public sealed class LanceLifeIntRange
    {
        [JsonProperty("min")] public int? Min { get; set; }
        [JsonProperty("max")] public int? Max { get; set; }
    }

    public sealed class LanceLifeEventMetadata
    {
        [JsonProperty("content_doc")] public string ContentDoc { get; set; } = string.Empty;
        [JsonProperty("tier_range")] public LanceLifeTierRange TierRange { get; set; } = new LanceLifeTierRange();

        // Guardrail: keep metadata forward-compatible.
        // Authoring can add lightweight hints (e.g., impact tags) without requiring a code change for every new field.
        [JsonExtensionData] public IDictionary<string, JToken> ExtensionData { get; set; } = new Dictionary<string, JToken>();
    }

    public sealed class LanceLifeEventContentDefinition
    {
        [JsonProperty("titleId")] public string TitleId { get; set; } = string.Empty;
        [JsonProperty("setupId")] public string SetupId { get; set; } = string.Empty;

        // Schema allows raw text or localization keys.
        [JsonProperty("title")] public string Title { get; set; } = string.Empty;
        [JsonProperty("setup")] public string Setup { get; set; } = string.Empty;

        [JsonProperty("options")] public List<LanceLifeEventOptionDefinition> Options { get; set; } = new List<LanceLifeEventOptionDefinition>();
    }

    public sealed class LanceLifeTierRange
    {
        [JsonProperty("min")] public int Min { get; set; } = 1;
        [JsonProperty("max")] public int Max { get; set; } = 999;
    }

    public sealed class LanceLifeInjuryRoll
    {
        [JsonProperty("chance")] public float Chance { get; set; }
        [JsonProperty("types")] public List<string> Types { get; set; } = new List<string>();
        [JsonProperty("severity_weights")]
        public Dictionary<string, float> SeverityWeights { get; set; } = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        [JsonProperty("location_weights")]
        public Dictionary<string, float> LocationWeights { get; set; } = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
    }

    public sealed class LanceLifeIllnessRoll
    {
        [JsonProperty("chance")] public float Chance { get; set; }
        [JsonProperty("types")] public List<string> Types { get; set; } = new List<string>();
        [JsonProperty("severity_weights")]
        public Dictionary<string, float> SeverityWeights { get; set; } = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
    }

    public sealed class LanceLifeInjuryRiskSchema
    {
        [JsonProperty("chance")] public int Chance { get; set; }
        [JsonProperty("severity")] public string Severity { get; set; } = string.Empty;
        [JsonProperty("type")] public string Type { get; set; } = string.Empty;
    }

    /// <summary>
    /// Reward choice system - allows players to customize event rewards.
    /// After an event outcome, players can choose between different reward types:
    /// - Skill focus (which skill to level up)
    /// - Compensation (gold vs reputation)
    /// - Weapon specialization (which weapon to train)
    /// - Risk level (safe vs aggressive approaches)
    /// - Rest focus (sleep vs socialize vs study)
    /// </summary>
    public sealed class LanceLifeRewardChoices
    {
        /// <summary>
        /// Type of reward choice:
        /// - skill_focus: Choose which skills to level up (polearm vs one-handed vs balanced)
        /// - compensation: Choose gold vs reputation tradeoffs
        /// - weapon_focus: Choose which weapon to train
        /// - risk_level: Choose risk/reward balance
        /// - rest_focus: Choose how to spend downtime
        /// </summary>
        [JsonProperty("type")] 
        public string Type { get; set; } = "skill_focus";
        
        /// <summary>
        /// Custom prompt text shown in the reward choice dialog.
        /// If empty, a default prompt is generated based on the type.
        /// </summary>
        [JsonProperty("prompt")] 
        public string Prompt { get; set; } = string.Empty;
        
        /// <summary>
        /// Optional: localization key for the prompt
        /// </summary>
        [JsonProperty("promptId")] 
        public string PromptId { get; set; } = string.Empty;
        
        /// <summary>
        /// Auto-selection preference (future enhancement):
        /// - formation: Choose based on player's formation (cavalry → riding, infantry → melee)
        /// - last_choice: Remember player's previous choice
        /// - gold_focus: Always prefer gold rewards
        /// - xp_focus: Always prefer XP rewards
        /// </summary>
        [JsonProperty("auto_select_preference")] 
        public string AutoSelectPreference { get; set; } = string.Empty;
        
        /// <summary>
        /// List of reward options to choose from (2-5 recommended).
        /// </summary>
        [JsonProperty("options")] 
        public List<LanceLifeRewardOption> Options { get; set; } = new List<LanceLifeRewardOption>();
    }

    /// <summary>
    /// Individual reward option within a reward choice dialog.
    /// </summary>
    public sealed class LanceLifeRewardOption
    {
        /// <summary>
        /// Unique ID for this reward option (used for tracking/analytics)
        /// </summary>
        [JsonProperty("id")] 
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// Text shown for this option in the dialog
        /// </summary>
        [JsonProperty("text")] 
        public string Text { get; set; } = string.Empty;
        
        /// <summary>
        /// Optional localization key for the text
        /// </summary>
        [JsonProperty("textId")] 
        public string TextId { get; set; } = string.Empty;
        
        /// <summary>
        /// Optional tooltip explaining the option in more detail
        /// </summary>
        [JsonProperty("tooltip")] 
        public string Tooltip { get; set; } = string.Empty;
        
        /// <summary>
        /// Optional condition that must be met for this option to appear:
        /// - "formation:infantry" - only for infantry players
        /// - "tier >= 3" - requires tier 3+
        /// - "gold >= 50" - requires 50+ gold
        /// </summary>
        [JsonProperty("condition")] 
        public string Condition { get; set; } = string.Empty;
        
        /// <summary>
        /// Optional success chance (for risky reward options).
        /// 0.0-1.0, where 1.0 = guaranteed success, 0.5 = 50% chance, etc.
        /// If present, the option can fail and show failure_outcome instead.
        /// </summary>
        [JsonProperty("success_chance")] 
        public float? SuccessChance { get; set; }
        
        /// <summary>
        /// Rewards granted if this option is selected (and succeeds if risky)
        /// </summary>
        [JsonProperty("rewards")] 
        public LanceLifeEventRewards Rewards { get; set; } = new LanceLifeEventRewards();
        
        /// <summary>
        /// Effects applied if this option is selected (and succeeds if risky)
        /// </summary>
        [JsonProperty("effects")] 
        public LanceLifeEventEscalationEffects Effects { get; set; } = new LanceLifeEventEscalationEffects();
        
        /// <summary>
        /// Optional failure outcome (for risky options with success_chance)
        /// </summary>
        [JsonProperty("failure_outcome")] 
        public LanceLifeRewardFailure Failure { get; set; }
    }

    /// <summary>
    /// Failure outcome for a risky reward option.
    /// </summary>
    public sealed class LanceLifeRewardFailure
    {
        /// <summary>
        /// Text shown if the option fails
        /// </summary>
        [JsonProperty("text")] 
        public string Text { get; set; } = string.Empty;
        
        /// <summary>
        /// Optional localization key for failure text
        /// </summary>
        [JsonProperty("textId")] 
        public string TextId { get; set; } = string.Empty;
        
        /// <summary>
        /// Effects applied on failure (typically negative)
        /// </summary>
        [JsonProperty("effects")] 
        public LanceLifeEventEscalationEffects Effects { get; set; } = new LanceLifeEventEscalationEffects();
    }
}


