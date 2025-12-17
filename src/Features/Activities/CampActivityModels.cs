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

        // Phase 2: Camp Hub - Location field for spatial navigation
        [JsonProperty("location")] public string Location { get; set; }

        [JsonProperty("textId")] public string TextId { get; set; }
        [JsonProperty("text")] public string Text { get; set; }

        [JsonProperty("hintId")] public string HintId { get; set; }
        [JsonProperty("hint")] public string Hint { get; set; }

        [JsonProperty("minTier")] public int MinTier { get; set; } = 1;
        [JsonProperty("maxTier")] public int MaxTier { get; set; } // 0 = no limit
        [JsonProperty("requiresLanceLeader")] public bool RequiresLanceLeader { get; set; }

        // Formation types: "infantry", "archer", "cavalry", "horsearcher" (same vocabulary as duties).
        [JsonProperty("formations")] public List<string> Formations { get; set; } = new List<string>();

        // Day parts: "dawn", "morning", "afternoon", "evening", "dusk", "night" (6-period system).
        [JsonProperty("dayParts")] public List<string> DayParts { get; set; } = new List<string>();

        [JsonProperty("fatigueCost")] public int FatigueCost { get; set; }
        [JsonProperty("fatigueRelief")] public int FatigueRelief { get; set; }

        [JsonProperty("cooldownDays")] public int CooldownDays { get; set; }

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

        // Phase 2: Camp Hub - Location for spatial navigation
        // Valid values: "medical_tent", "training_grounds", "lords_tent", 
        //               "quartermaster", "personal_quarters", "camp_fire"
        public string Location { get; set; }

        public string TextId { get; set; }
        public string TextFallback { get; set; }

        public string HintId { get; set; }
        public string HintFallback { get; set; }

        public int MinTier { get; set; }
        public int MaxTier { get; set; }
        public bool RequiresLanceLeader { get; set; }
        public List<string> Formations { get; set; } = new List<string>();
        public List<string> DayParts { get; set; } = new List<string>();

        public int FatigueCost { get; set; }
        public int FatigueRelief { get; set; }
        public int CooldownDays { get; set; }

        public Dictionary<string, int> SkillXp { get; set; } = new Dictionary<string, int>();
        public bool BlockOnSevereCondition { get; set; }
    }
}


