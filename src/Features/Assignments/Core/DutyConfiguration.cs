using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Enlisted.Features.Assignments.Core
{
    /// <summary>
    /// Configuration data structures for the duties system.
    /// Maps directly to duties_system.json for complete JSON-driven configuration.
    /// </summary>
    
    [Serializable]
    public class DutiesSystemConfig
    {
        [JsonProperty("schemaVersion")]
        public int SchemaVersion { get; set; } = 1;
        
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = true;
        
        [JsonProperty("duties")]
        public Dictionary<string, DutyDefinition> Duties { get; set; } = new Dictionary<string, DutyDefinition>();
        
        [JsonProperty("duty_slots")]
        public Dictionary<string, int> DutySlots { get; set; } = new Dictionary<string, int>();
        
        [JsonProperty("officer_roles")]
        public Dictionary<string, OfficerRoleDefinition> OfficerRoles { get; set; } = new Dictionary<string, OfficerRoleDefinition>();
        
        [JsonProperty("formation_progression")]
        public FormationProgressionConfig FormationProgression { get; set; } = new FormationProgressionConfig();
        
        [JsonProperty("xp_sources")]
        public Dictionary<string, int> XpSources { get; set; } = new Dictionary<string, int>();
    }
    
    [Serializable]
    public class DutyDefinition
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        
        [JsonProperty("display_name")]
        public string DisplayName { get; set; }
        
        [JsonProperty("description")]
        public string Description { get; set; }
        
        [JsonProperty("min_tier")]
        public int MinTier { get; set; } = 1;
        
        [JsonProperty("max_concurrent")]
        public int MaxConcurrent { get; set; } = 1;
        
        [JsonProperty("target_skill")]
        public string TargetSkill { get; set; }
        
        [JsonProperty("skill_xp_daily")]
        public int SkillXpDaily { get; set; } = 0;
        
        [JsonProperty("officer_role")]
        public string OfficerRole { get; set; }
        
        [JsonProperty("xp_share_multiplier")]
        public float XpShareMultiplier { get; set; } = 0.0f;
        
        [JsonProperty("wage_multiplier")]
        public float WageMultiplier { get; set; } = 1.0f;
        
        [JsonProperty("required_formations")]
        public List<string> RequiredFormations { get; set; } = new List<string>();
        
        [JsonProperty("passive_effects")]
        public Dictionary<string, int> PassiveEffects { get; set; } = new Dictionary<string, int>();
        
        [JsonProperty("special_abilities")]
        public List<string> SpecialAbilities { get; set; } = new List<string>();
        
        [JsonProperty("unlock_conditions")]
        public UnlockConditions UnlockConditions { get; set; } = new UnlockConditions();
    }
    
    [Serializable]
    public class UnlockConditions
    {
        [JsonProperty("relationship_required")]
        public int RelationshipRequired { get; set; } = 0;
        
        [JsonProperty("skill_required")]
        public int SkillRequired { get; set; } = 0;
    }
    
    [Serializable]
    public class OfficerRoleDefinition
    {
        [JsonProperty("harmony_patch_target")]
        public string HarmonyPatchTarget { get; set; }
        
        [JsonProperty("skill_benefits")]
        public List<string> SkillBenefits { get; set; } = new List<string>();
        
        [JsonProperty("party_effects")]
        public List<string> PartyEffects { get; set; } = new List<string>();
        
        [JsonProperty("duties")]
        public List<string> Duties { get; set; } = new List<string>();
    }
    
    [Serializable]
    public class FormationProgressionConfig
    {
        [JsonProperty("selection_tier")]
        public int SelectionTier { get; set; } = 2;
        
        [JsonProperty("auto_detect")]
        public bool AutoDetect { get; set; } = true;
        
        [JsonProperty("allow_switching")]
        public bool AllowSwitching { get; set; } = true;
        
        [JsonProperty("switching_cost_multiplier")]
        public float SwitchingCostMultiplier { get; set; } = 0.5f;
    }
}
