using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Enlisted.Features.Lances.Events
{
    /// <summary>
    /// In-memory catalog of Lance Life Events loaded from ModuleData/Enlisted/Events/*.json.
    /// Phase 0 delivers the loader + validation only; firing/scheduling is implemented in later phases.
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

        // Phase 4/5: onboarding track filter ("enlisted" | "officer" | "commander"). Optional for non-onboarding events.
        [JsonProperty("track")] public string Track { get; set; } = string.Empty;

        // Phase 6: Tier-Based Narrative Access - who initiates this event
        // Values: "lord", "lance_leader", "lance_mate", "situation"
        // Used to filter events based on player tier (e.g., T1-2 can't get lord invitations)
        [JsonProperty("narrative_source")] public string NarrativeSource { get; set; } = string.Empty;

        // Schema (Phase 5a): optional metadata block (authoring aid)
        [JsonProperty("metadata")] public LanceLifeEventMetadata Metadata { get; set; } = new LanceLifeEventMetadata();

        [JsonProperty("delivery")] public LanceLifeEventDelivery Delivery { get; set; } = new LanceLifeEventDelivery();
        [JsonProperty("triggers")] public LanceLifeEventTriggers Triggers { get; set; } = new LanceLifeEventTriggers();
        [JsonProperty("requirements")] public LanceLifeEventRequirements Requirements { get; set; } = new LanceLifeEventRequirements();
        [JsonProperty("timing")] public LanceLifeEventTiming Timing { get; set; } = new LanceLifeEventTiming();

        // Legacy/engine-flat content (prefer IDs; allow fallback text for early authoring).
        [JsonProperty("titleId")] public string TitleId { get; set; } = string.Empty;
        [JsonProperty("setupId")] public string SetupId { get; set; } = string.Empty;
        [JsonProperty("title")] public string TitleFallback { get; set; } = string.Empty;
        [JsonProperty("setup")] public string SetupFallback { get; set; } = string.Empty;

        // Schema (Phase 5a): nested content block (authoring truth)
        [JsonProperty("content")] public LanceLifeEventContentDefinition Content { get; set; } = new LanceLifeEventContentDefinition();

        // Engine uses a flat Options list; loader normalizes from Content.Options if present.
        [JsonProperty("options")] public List<LanceLifeEventOptionDefinition> Options { get; set; } = new List<LanceLifeEventOptionDefinition>();

        // Phase 5: variant overrides (used primarily by onboarding: first_time/transfer/return).
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

        // Schema-style outcome fields (Phase 5a), normalized into the legacy result fields by loader.
        [JsonProperty("outcome")] public string SchemaOutcome { get; set; } = string.Empty;
        [JsonProperty("outcome_failure")] public string SchemaOutcomeFailure { get; set; } = string.Empty;

        [JsonProperty("risk")] public string Risk { get; set; } = "safe";
        [JsonProperty("success_chance")] public float? SuccessChance { get; set; }
        [JsonProperty("risk_chance")] public int? RiskChance { get; set; }

        [JsonProperty("costs")] public LanceLifeEventCosts Costs { get; set; } = new LanceLifeEventCosts();
        [JsonProperty("rewards")] public LanceLifeEventRewards Rewards { get; set; } = new LanceLifeEventRewards();
        [JsonProperty("effects")] public LanceLifeEventEscalationEffects Effects { get; set; } = new LanceLifeEventEscalationEffects();

        // Phase 5: success/failure effect overrides for risky options (used by some threshold events).
        [JsonProperty("effects_success")] public LanceLifeEventEscalationEffects EffectsSuccess { get; set; }
        [JsonProperty("effects_failure")] public LanceLifeEventEscalationEffects EffectsFailure { get; set; }

        [JsonProperty("injury")] public LanceLifeInjuryRoll Injury { get; set; }
        [JsonProperty("illness")] public LanceLifeIllnessRoll Illness { get; set; }

        // Schema injury format (Phase 5a) normalized into Injury/Illness by loader.
        [JsonProperty("injury_risk")] public LanceLifeInjuryRiskSchema InjuryRisk { get; set; }

        // Phase 3 (Decision Events): Event chains - selecting this option queues another event
        // The chained event will fire after the specified delay (default: 12-36 hours)
        [JsonProperty("chains_to")] public string ChainsTo { get; set; } = string.Empty;

        // Phase 3 (Decision Events): Delay in hours before the chained event fires (0 = use random default)
        [JsonProperty("chain_delay_hours")] public float ChainDelayHours { get; set; }

        // Phase 3 (Decision Events): Story flags to set when this option is selected
        // These can be checked by future events using triggers.all/any/none
        [JsonProperty("set_flags")] public List<string> SetFlags { get; set; } = new List<string>();

        // Phase 3 (Decision Events): Story flags to clear when this option is selected
        [JsonProperty("clear_flags")] public List<string> ClearFlags { get; set; } = new List<string>();

        // Phase 3 (Decision Events): Duration in days for set_flags (0 = permanent until cleared)
        [JsonProperty("flag_duration_days")] public float FlagDurationDays { get; set; }

        // Phase 4/5: onboarding stage advancement control.
        // If true and the event is an onboarding event, selecting this option advances the onboarding stage.
        [JsonProperty("advances_onboarding")] public bool AdvancesOnboarding { get; set; }
    }

    public sealed class LanceLifeEventDelivery
    {
        [JsonProperty("method")] public string Method { get; set; } = "automatic"; // automatic | player_initiated
        [JsonProperty("channel")] public string Channel { get; set; } = string.Empty; // menu | inquiry | incident
        [JsonProperty("incident_trigger")] public string IncidentTrigger { get; set; } = string.Empty; // LeavingBattle | WaitingInSettlement | LeavingEncounter | ...
        [JsonProperty("menu")] public string Menu { get; set; } = string.Empty;
        [JsonProperty("menu_section")] public string MenuSection { get; set; } = string.Empty; // training | tasks | social
    }

    public sealed class LanceLifeEventTriggers
    {
        [JsonProperty("all")] public List<string> All { get; set; } = new List<string>();
        [JsonProperty("any")] public List<string> Any { get; set; } = new List<string>();
        [JsonProperty("time_of_day")] public List<string> TimeOfDay { get; set; } = new List<string>();

        // Phase 3 (Decision Events): blocking flags - event won't fire if any of these flags are active
        [JsonProperty("none")] public List<string> None { get; set; } = new List<string>();

        // Phase 5a: range constraints for escalation tracks
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

        // Phase 3 (Decision Events): max times this event can fire per enlistment term
        [JsonProperty("max_per_term")] public int MaxPerTerm { get; set; }

        // Phase 3 (Decision Events): list of event IDs that cannot fire same day as this event
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

        // Schema: effects.fatigue_relief (legacy mapping; engine also supports rewards.fatigueRelief).
        [JsonProperty("fatigue_relief")] public int FatigueRelief { get; set; }

        // Schema: optional narrative/flag effects (Phase 5a)
        [JsonProperty("formation")] public string Formation { get; set; } = string.Empty;
        [JsonProperty("character_tag")] public string CharacterTag { get; set; } = string.Empty;
        [JsonProperty("loyalty_tag")] public string LoyaltyTag { get; set; } = string.Empty;

        // Phase 7: Promotion and duty assignment effects
        [JsonProperty("promotes")] public bool Promotes { get; set; }
        [JsonProperty("starter_duty")] public string StarterDuty { get; set; } = string.Empty;
    }

    public sealed class LanceLifeEventEscalationRequirements
    {
        [JsonProperty("heat")] public LanceLifeIntRange Heat { get; set; } = new LanceLifeIntRange();
        [JsonProperty("discipline")] public LanceLifeIntRange Discipline { get; set; } = new LanceLifeIntRange();
        [JsonProperty("lance_reputation")] public LanceLifeIntRange LanceReputation { get; set; } = new LanceLifeIntRange();
        [JsonProperty("medical_risk")] public LanceLifeIntRange MedicalRisk { get; set; } = new LanceLifeIntRange();
        
        // Pay tension threshold for pay-related events (Phase 3 Pay System)
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
    }

    public sealed class LanceLifeEventContentDefinition
    {
        [JsonProperty("titleId")] public string TitleId { get; set; } = string.Empty;
        [JsonProperty("setupId")] public string SetupId { get; set; } = string.Empty;

        // Schema allows raw text or localization keys; Phase 5 uses raw text initially.
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
}


