using System.Collections.Generic;
using Newtonsoft.Json;

namespace Enlisted.Features.Activities
{
    /// <summary>
    /// JSON schema for ModuleData/Enlisted/Activities/*.json.
    /// Keep these models tolerant; validation/normalization happens in the catalog loader.
    /// </summary>
    public sealed class CampActivitiesDefinitionsJson
    {
        [JsonProperty("schemaVersion")] public int SchemaVersion { get; set; } = 1;
        [JsonProperty("activities")] public List<CampActivityJson> Activities { get; set; } = new List<CampActivityJson>();
    }

    public sealed class CampActivityJson
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("category")] public string Category { get; set; }

        [JsonProperty("textId")] public string TextId { get; set; }
        [JsonProperty("text")] public string Text { get; set; }

        [JsonProperty("hintId")] public string HintId { get; set; }
        [JsonProperty("hint")] public string Hint { get; set; }

        [JsonProperty("minTier")] public int MinTier { get; set; } = 1;

        // Formation types: "infantry", "archer", "cavalry", "horsearcher" (same vocabulary as duties).
        [JsonProperty("formations")] public List<string> Formations { get; set; } = new List<string>();

        // Day parts: "dawn", "day", "dusk", "night" (same vocabulary as CampaignTriggerTokens).
        [JsonProperty("dayParts")] public List<string> DayParts { get; set; } = new List<string>();

        [JsonProperty("fatigueCost")] public int FatigueCost { get; set; } = 0;
        [JsonProperty("fatigueRelief")] public int FatigueRelief { get; set; } = 0;

        [JsonProperty("cooldownDays")] public int CooldownDays { get; set; } = 0;

        [JsonProperty("skillXp")] public Dictionary<string, int> SkillXp { get; set; } = new Dictionary<string, int>();

        // If true, activity is disabled when the player has severe conditions (injury/illness).
        [JsonProperty("blockOnSevereCondition")] public bool BlockOnSevereCondition { get; set; } = true;
    }

    /// <summary>
    /// Normalized, runtime definition used by menus.
    /// </summary>
    public sealed class CampActivityDefinition
    {
        public string Id { get; set; }
        public string Category { get; set; }

        public string TextId { get; set; }
        public string TextFallback { get; set; }

        public string HintId { get; set; }
        public string HintFallback { get; set; }

        public int MinTier { get; set; }
        public List<string> Formations { get; set; } = new List<string>();
        public List<string> DayParts { get; set; } = new List<string>();

        public int FatigueCost { get; set; }
        public int FatigueRelief { get; set; }
        public int CooldownDays { get; set; }

        public Dictionary<string, int> SkillXp { get; set; } = new Dictionary<string, int>();
        public bool BlockOnSevereCondition { get; set; }
    }
}


