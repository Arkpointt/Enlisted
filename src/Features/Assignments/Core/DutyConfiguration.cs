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
        
        [JsonProperty("selection_system")]
        public SelectionSystemConfig SelectionSystem { get; set; } = new SelectionSystemConfig();
        
        [JsonProperty("duty_slots")]
        public Dictionary<string, int> DutySlots { get; set; } = new Dictionary<string, int>();
        
        [JsonProperty("officer_roles")]
        public Dictionary<string, OfficerRoleDefinition> OfficerRoles { get; set; } = new Dictionary<string, OfficerRoleDefinition>();
        
        [JsonProperty("formation_progression")]
        public FormationProgressionConfig FormationProgression { get; set; } = new FormationProgressionConfig();
        
        [JsonProperty("xp_sources")]
        public Dictionary<string, int> XpSources { get; set; } = new Dictionary<string, int>();
        
        [JsonProperty("formation_training")]
        public FormationTrainingConfig FormationTraining { get; set; } = new FormationTrainingConfig();
    }
    
    [Serializable]
    public class SelectionSystemConfig
    {
        [JsonProperty("duty_selection")]
        public SelectionConfig DutySelection { get; set; } = new SelectionConfig();
    }
    
    [Serializable]
    public class SelectionConfig
    {
        [JsonProperty("description")]
        public string Description { get; set; }
        
        [JsonProperty("max_selections")]
        public int MaxSelections { get; set; } = 1;
        
        [JsonProperty("available_from_tier")]
        public int AvailableFromTier { get; set; } = 1;
        
        [JsonProperty("default_selection")]
        public string DefaultSelection { get; set; }
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
        public int SkillXpDaily { get; set; }
        
        [JsonProperty("officer_role")]
        public string OfficerRole { get; set; }
        
        [JsonProperty("xp_share_multiplier")]
        public float XpShareMultiplier { get; set; }
        
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
        
        [JsonProperty("multi_skill_xp")]
        public Dictionary<string, int> MultiSkillXp { get; set; } = new Dictionary<string, int>();

        // Phase 4.5: Event prefix used by Phase 5 content conversion verification (e.g., "qm_" for quartermaster).
        [JsonProperty("event_prefix")]
        public string EventPrefix { get; set; }

        // Phase 4.5: Optional DLC gate. Example: "war_sails" (NavalDLC).
        [JsonProperty("requires_expansion")]
        public string RequiresExpansion { get; set; }
    }
    
    [Serializable]
    public class UnlockConditions
    {
        [JsonProperty("relationship_required")]
        public int RelationshipRequired { get; set; }
        
        [JsonProperty("skill_required")]
        public int SkillRequired { get; set; }
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
    
    [Serializable]
    public class FormationTrainingConfig
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = true;
        
        [JsonProperty("description")]
        public string Description { get; set; } = "Formation-based military training";
        
        [JsonProperty("formations")]
        public Dictionary<string, FormationSkillConfig> Formations { get; set; } = new Dictionary<string, FormationSkillConfig>();
    }
    
    [Serializable]
    public class FormationSkillConfig
    {
        [JsonProperty("description")]
        public string Description { get; set; }
        
        [JsonProperty("skills")]
        public Dictionary<string, int> Skills { get; set; } = new Dictionary<string, int>();
    }
}
