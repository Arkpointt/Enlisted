using System.Collections.Generic;

namespace Enlisted.Features.Lances.Stories
{
    /// <summary>
    /// Normalized, validated story catalog produced by the pack loader.
    /// This is what the runtime behavior consumes.
    /// </summary>
    internal sealed class LanceLifeStoryCatalog
    {
        public List<LanceLifeStoryDefinition> Stories { get; } = new List<LanceLifeStoryDefinition>();
    }

    internal sealed class LanceLifeStoryDefinition
    {
        public string PackId { get; set; }
        public string Id { get; set; }
        public string Category { get; set; }
        public List<string> Tags { get; set; } = new List<string>();

        public string TitleId { get; set; }
        public string BodyId { get; set; }
        public string TitleFallback { get; set; }
        public string BodyFallback { get; set; }

        public int TierMin { get; set; }
        public int TierMax { get; set; }
        public bool RequireFinalLance { get; set; }

        public int CooldownDays { get; set; }
        public int MaxPerTerm { get; set; }

        public List<string> TriggerAll { get; set; } = new List<string>();
        public List<string> TriggerAny { get; set; } = new List<string>();

        public List<LanceLifeOptionDefinition> Options { get; set; } = new List<LanceLifeOptionDefinition>();
    }

    internal sealed class LanceLifeOptionDefinition
    {
        public string Id { get; set; }

        public string TextId { get; set; }
        public string HintId { get; set; }
        public string TextFallback { get; set; }
        public string HintFallback { get; set; }

        public string Risk { get; set; }

        public int CostFatigue { get; set; }
        public int CostGold { get; set; }
        public int CostHeat { get; set; }
        public int CostDiscipline { get; set; }

        // Phase 4 escalation track effects (optional; 0 means no change).
        public int EffectHeat { get; set; }
        public int EffectDiscipline { get; set; }
        public int EffectLanceReputation { get; set; }
        public int EffectMedicalRisk { get; set; }

        // Phase 5 player conditions (optional)
        public float InjuryChance { get; set; }
        public List<string> InjuryTypes { get; set; } = new List<string>();
        public Dictionary<string, float> InjurySeverityWeights { get; set; } = new Dictionary<string, float>();

        public float IllnessChance { get; set; }
        public List<string> IllnessTypes { get; set; } = new List<string>();
        public Dictionary<string, float> IllnessSeverityWeights { get; set; } = new Dictionary<string, float>();

        public Dictionary<string, int> RewardSkillXp { get; set; } = new Dictionary<string, int>();
        public int RewardGold { get; set; }
        public int RewardFatigueRelief { get; set; }

        public string ResultTextId { get; set; }
        public string ResultTextFallback { get; set; }
    }
}


