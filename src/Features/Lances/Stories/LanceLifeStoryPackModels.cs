using System.Collections.Generic;
using Newtonsoft.Json;

namespace Enlisted.Features.Lances.Stories
{
    // JSON models for StoryPacks/LanceLife/*.json (Phase 1 contract).
    // Keep these POCOs simple and tolerant: validation happens in the loader.

    internal sealed class LanceLifeStoryPackJson
    {
        [JsonProperty("schemaVersion")] public int SchemaVersion { get; set; } = 1;
        [JsonProperty("packId")] public string PackId { get; set; }
        [JsonProperty("stories")] public List<LanceLifeStoryJson> Stories { get; set; } = new List<LanceLifeStoryJson>();
    }

    internal sealed class LanceLifeStoryJson
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("category")] public string Category { get; set; }
        [JsonProperty("tags")] public List<string> Tags { get; set; } = new List<string>();

        [JsonProperty("titleId")] public string TitleId { get; set; }
        [JsonProperty("bodyId")] public string BodyId { get; set; }
        [JsonProperty("title")] public string Title { get; set; }
        [JsonProperty("body")] public string Body { get; set; }

        [JsonProperty("tierMin")] public int TierMin { get; set; } = 1;
        [JsonProperty("tierMax")] public int TierMax { get; set; } = 999;
        [JsonProperty("requireFinalLance")] public bool RequireFinalLance { get; set; } = true;

        [JsonProperty("cooldownDays")] public int CooldownDays { get; set; } = 7;
        [JsonProperty("maxPerTerm")] public int MaxPerTerm { get; set; } = 0;

        [JsonProperty("triggers")] public LanceLifeTriggersJson Triggers { get; set; } = new LanceLifeTriggersJson();

        [JsonProperty("options")] public List<LanceLifeStoryOptionJson> Options { get; set; } = new List<LanceLifeStoryOptionJson>();
    }

    internal sealed class LanceLifeTriggersJson
    {
        [JsonProperty("all")] public List<string> All { get; set; } = new List<string>();
        [JsonProperty("any")] public List<string> Any { get; set; } = new List<string>();
    }

    internal sealed class LanceLifeStoryOptionJson
    {
        [JsonProperty("id")] public string Id { get; set; }

        [JsonProperty("textId")] public string TextId { get; set; }
        [JsonProperty("hintId")] public string HintId { get; set; }

        [JsonProperty("text")] public string Text { get; set; }
        [JsonProperty("hint")] public string Hint { get; set; }

        [JsonProperty("risk")] public string Risk { get; set; } = "safe";

        [JsonProperty("costs")] public LanceLifeCostsJson Costs { get; set; } = new LanceLifeCostsJson();
        [JsonProperty("rewards")] public LanceLifeRewardsJson Rewards { get; set; } = new LanceLifeRewardsJson();

        // Phase 4: escalation track effects (see docs/research/phase4_escalation_implementation_guide.md).
        // This field is optional; missing effects are treated as 0.
        [JsonProperty("effects")] public LanceLifeEscalationEffectsJson Effects { get; set; } = new LanceLifeEscalationEffectsJson();

        // Phase 5: player condition consequences (injury/illness rolls).
        [JsonProperty("injury")] public LanceLifeInjuryRollJson Injury { get; set; }
        [JsonProperty("illness")] public LanceLifeIllnessRollJson Illness { get; set; }

        [JsonProperty("resultTextId")] public string ResultTextId { get; set; }
        [JsonProperty("resultText")] public string ResultText { get; set; }
    }

    internal sealed class LanceLifeCostsJson
    {
        [JsonProperty("fatigue")] public int Fatigue { get; set; }
        [JsonProperty("gold")] public int Gold { get; set; }
        [JsonProperty("heat")] public int Heat { get; set; }
        [JsonProperty("discipline")] public int Discipline { get; set; }
    }

    internal sealed class LanceLifeRewardsJson
    {
        [JsonProperty("skillXp")] public Dictionary<string, int> SkillXp { get; set; } = new Dictionary<string, int>();
        [JsonProperty("gold")] public int Gold { get; set; }
        [JsonProperty("fatigueRelief")] public int FatigueRelief { get; set; }
    }

    internal sealed class LanceLifeEscalationEffectsJson
    {
        [JsonProperty("heat")] public int Heat { get; set; }
        [JsonProperty("discipline")] public int Discipline { get; set; }
        [JsonProperty("lance_reputation")] public int LanceReputation { get; set; }
        [JsonProperty("medical_risk")] public int MedicalRisk { get; set; }
    }

    internal sealed class LanceLifeInjuryRollJson
    {
        [JsonProperty("chance")] public float Chance { get; set; }
        [JsonProperty("types")] public List<string> Types { get; set; } = new List<string>();
        [JsonProperty("severity_weights")] public Dictionary<string, float> SeverityWeights { get; set; } = new Dictionary<string, float>();
        [JsonProperty("location_weights")] public Dictionary<string, float> LocationWeights { get; set; } = new Dictionary<string, float>();
    }

    internal sealed class LanceLifeIllnessRollJson
    {
        [JsonProperty("chance")] public float Chance { get; set; }
        [JsonProperty("types")] public List<string> Types { get; set; } = new List<string>();
        [JsonProperty("severity_weights")] public Dictionary<string, float> SeverityWeights { get; set; } = new Dictionary<string, float>();
    }
}


