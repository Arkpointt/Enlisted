using System.Collections.Generic;
using Newtonsoft.Json;

namespace Enlisted.Features.Lances.Personas
{
    internal enum LancePosition
    {
        Leader = 0,
        Second = 1,
        SeniorVeteran = 2,
        Veteran = 3,
        Soldier = 4,
        Recruit = 5
    }

    internal sealed class LanceRankTitleJson
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("fallback")] public string Fallback { get; set; }
    }

    internal sealed class LanceCulturePersonaPoolJson
    {
        [JsonProperty("male_first")] public List<string> MaleFirst { get; set; } = new List<string>();
        [JsonProperty("female_first")] public List<string> FemaleFirst { get; set; } = new List<string>();
        [JsonProperty("epithets")] public List<string> Epithets { get; set; } = new List<string>();

        [JsonProperty("rank_titles")] public Dictionary<string, LanceRankTitleJson> RankTitles { get; set; } =
            new Dictionary<string, LanceRankTitleJson>();
    }

    internal sealed class LancePersonaNamePoolsJson
    {
        [JsonProperty("schemaVersion")] public int SchemaVersion { get; set; } = 1;

        [JsonProperty("cultures")] public Dictionary<string, LanceCulturePersonaPoolJson> Cultures { get; set; } =
            new Dictionary<string, LanceCulturePersonaPoolJson>();
    }

    internal sealed class LancePersonaMember
    {
        public int SlotIndex { get; set; }
        public LancePosition Position { get; set; }
        public bool IsAlive { get; set; } = true;

        public string RankTitleId { get; set; } = string.Empty;
        public string RankTitleFallback { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;
        public string Epithet { get; set; } = string.Empty;
        
        // Progression tracking for promotions
        public int DaysInService { get; set; } = 0;
        public int BattlesParticipated { get; set; } = 0;
        public int ExperiencePoints { get; set; } = 0;
        
        /// <summary>
        /// Check if this member is eligible for promotion based on time and battles.
        /// Requires 30+ days in service AND 3+ battles for consideration.
        /// </summary>
        public bool IsPromotionEligible => 
            Position != LancePosition.Leader &&
            DaysInService >= 30 && 
            BattlesParticipated >= 3;
    }

    internal sealed class LancePersonaRoster
    {
        public string LanceKey { get; set; } = string.Empty;
        public string LordId { get; set; } = string.Empty;
        public string CultureId { get; set; } = string.Empty;
        public int Seed { get; set; }

        public List<LancePersonaMember> Members { get; set; } = new List<LancePersonaMember>();
    }
}


