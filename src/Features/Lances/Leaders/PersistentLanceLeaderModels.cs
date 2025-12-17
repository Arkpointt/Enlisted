using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;

namespace Enlisted.Features.Lances.Leaders
{
    /// <summary>
    /// Primary personality traits for lance leaders - dominant behavior pattern.
    /// Track C2: Persistent Lance Leaders.
    /// </summary>
    public enum PersonalityTrait
    {
        /// <summary>Demands perfection, harsh discipline, rare praise (70-95 Strictness)</summary>
        Stern = 0,
        
        /// <summary>Balanced, just, rewards merit, punishes fairly (50-70 Strictness)</summary>
        Fair = 1,
        
        /// <summary>Results-focused, flexible on methods (75-95 Pragmatism)</summary>
        Pragmatic = 2,
        
        /// <summary>Protective, mentoring, disappointed rather than angry (30-50 Strictness)</summary>
        Fatherly = 3,
        
        /// <summary>Career-driven, political, uses lance for advancement (45-65 Strictness)</summary>
        Ambitious = 4,
        
        /// <summary>World-weary, expects worst, dry humor (40-60 Strictness)</summary>
        Cynical = 5
    }

    /// <summary>
    /// Secondary personality traits - modifies primary behavior.
    /// </summary>
    public enum SecondaryTrait
    {
        /// <summary>Won't tolerate corruption, values integrity</summary>
        Honorable = 0,
        
        /// <summary>Smart, tactical, sees through deception</summary>
        Cunning = 1,
        
        /// <summary>Harsh punishments, intimidating, feared</summary>
        Brutal = 2,
        
        /// <summary>Shields lance from danger, paternal</summary>
        Protective = 3,
        
        /// <summary>Boosts morale, natural leader</summary>
        Inspiring = 4,
        
        /// <summary>Risk-averse, values safety over glory</summary>
        Cautious = 5,
        
        /// <summary>Remembers slights, holds grudges</summary>
        Vengeful = 6,
        
        /// <summary>Gives good advice, learned from experience</summary>
        Wise = 7
    }

    /// <summary>
    /// Types of memories the lance leader can store.
    /// </summary>
    public enum MemoryType
    {
        /// <summary>Regular duty event choice</summary>
        DutyEvent = 0,
        
        /// <summary>Player chose corrupt path</summary>
        CorruptionChoice = 1,
        
        /// <summary>Player went above and beyond</summary>
        HeroicAction = 2,
        
        /// <summary>Player failed or fumbled</summary>
        Failure = 3,
        
        /// <summary>Heat or Discipline gained</summary>
        DisciplineIssue = 4,
        
        /// <summary>Player promoted in rank</summary>
        PromotionMoment = 5,
        
        /// <summary>How player did in battle</summary>
        BattlePerformance = 6,
        
        /// <summary>Player returned after leaving</summary>
        ReEnlisted = 7,
        
        /// <summary>Player left service</summary>
        Discharged = 8
    }

    /// <summary>
    /// Dialogue tone for lance leader conversations.
    /// </summary>
    public enum DialogTone
    {
        /// <summary>Cold and formal, low trust</summary>
        ColdlyFormal = 0,
        
        /// <summary>Respectfully formal, moderate trust</summary>
        RespectfullyFormal = 1,
        
        /// <summary>Patient but formal, developing trust</summary>
        PatientlyFormal = 2,
        
        /// <summary>Neutral professional, standard</summary>
        NeutralProfessional = 3,
        
        /// <summary>Casual but professional</summary>
        CasuallyProfessional = 4,
        
        /// <summary>Business-like, pragmatic</summary>
        BusinessLike = 5,
        
        /// <summary>Friendly and professional, high trust</summary>
        FriendlyProfessional = 6,
        
        /// <summary>Warm and familial, very high trust</summary>
        WarmFamilial = 7
    }

    /// <summary>
    /// A single memory entry tracking a player action or event.
    /// </summary>
    public class MemoryEntry
    {
        [SaveableProperty(1)]
        public MemoryType Type { get; set; }

        [SaveableProperty(2)]
        public CampaignTime Date { get; set; }

        [SaveableProperty(3)]
        public string EventId { get; set; }

        [SaveableProperty(4)]
        public string ChoiceId { get; set; }

        [SaveableProperty(5)]
        public int ImpactScore { get; set; }

        [SaveableProperty(6)]
        public string Description { get; set; }

        /// <summary>
        /// Days since this memory was created.
        /// </summary>
        public float DaysSinceCreated => (float)(CampaignTime.Now - Date).ToDays;

        /// <summary>
        /// Create a new memory entry.
        /// </summary>
        public static MemoryEntry Create(MemoryType type, string description, int impactScore = 0)
        {
            return new MemoryEntry
            {
                Type = type,
                Date = CampaignTime.Now,
                Description = description,
                ImpactScore = impactScore
            };
        }

        /// <summary>
        /// Create a memory from an event choice.
        /// </summary>
        public static MemoryEntry FromEventChoice(string eventId, string choiceId, string description, int impactScore)
        {
            return new MemoryEntry
            {
                Type = MemoryType.DutyEvent,
                Date = CampaignTime.Now,
                EventId = eventId,
                ChoiceId = choiceId,
                Description = description,
                ImpactScore = impactScore
            };
        }
    }

    /// <summary>
    /// Background information for a lance leader.
    /// </summary>
    public class LanceLeaderBackground
    {
        [SaveableProperty(1)]
        public int YearsOfService { get; set; }

        [SaveableProperty(2)]
        public string FormerRole { get; set; }

        [SaveableProperty(3)]
        public bool IsVeteran { get; set; }

        [SaveableProperty(4)]
        public List<string> BattleScars { get; set; }

        [SaveableProperty(5)]
        public string SpecialQuality { get; set; }

        public LanceLeaderBackground()
        {
            BattleScars = new List<string>();
        }
    }

    /// <summary>
    /// Complete persistent lance leader data.
    /// </summary>
    public class PersistentLanceLeader
    {
        // Identity
        [SaveableProperty(1)]
        public string LordId { get; set; }

        [SaveableProperty(2)]
        public string Name { get; set; }

        [SaveableProperty(3)]
        public string FirstName { get; set; }

        [SaveableProperty(4)]
        public string Epithet { get; set; }

        [SaveableProperty(5)]
        public string Culture { get; set; }

        [SaveableProperty(6)]
        public string RankTitle { get; set; }

        [SaveableProperty(7)]
        public bool IsFemale { get; set; }

        // Personality
        [SaveableProperty(8)]
        public PersonalityTrait PrimaryTrait { get; set; }

        [SaveableProperty(9)]
        public SecondaryTrait SecondaryTraitValue { get; set; }

        [SaveableProperty(10)]
        public int Strictness { get; set; }

        [SaveableProperty(11)]
        public int Pragmatism { get; set; }

        [SaveableProperty(12)]
        public int Loyalty { get; set; }

        // Background
        [SaveableProperty(13)]
        public LanceLeaderBackground Background { get; set; }

        // Relationship with Player
        [SaveableProperty(14)]
        public int RelationshipScore { get; set; }

        [SaveableProperty(15)]
        public int TrustLevel { get; set; }

        [SaveableProperty(16)]
        public CampaignTime FirstMetDate { get; set; }

        [SaveableProperty(17)]
        public int DaysServedUnder { get; set; }

        // Memory System
        [SaveableProperty(18)]
        public List<MemoryEntry> RecentMemories { get; set; }

        [SaveableProperty(19)]
        public Dictionary<string, int> EventCounts { get; set; }

        [SaveableProperty(20)]
        public CampaignTime LastInteraction { get; set; }

        // State
        [SaveableProperty(21)]
        public bool IsAlive { get; set; }

        [SaveableProperty(22)]
        public CampaignTime DateOfDeath { get; set; }

        [SaveableProperty(23)]
        public string CauseOfDeath { get; set; }

        // Random seed for consistent behavior
        [SaveableProperty(24)]
        public int PersonalitySeed { get; set; }

        public PersistentLanceLeader()
        {
            RecentMemories = new List<MemoryEntry>();
            EventCounts = new Dictionary<string, int>();
            Background = new LanceLeaderBackground();
            IsAlive = true;
            TrustLevel = 25;
        }

        /// <summary>
        /// Get the full display name (with epithet if present).
        /// </summary>
        public string FullName => string.IsNullOrEmpty(Epithet) 
            ? FirstName 
            : $"{FirstName} \"{Epithet}\"";

        /// <summary>
        /// Get the formal title (rank + name).
        /// </summary>
        public string FormalTitle => $"{RankTitle} {FirstName}";

        /// <summary>
        /// Add a memory, maintaining FIFO queue of max 15.
        /// </summary>
        public void AddMemory(MemoryEntry memory)
        {
            RecentMemories.Add(memory);
            
            // Maintain max 15 memories (FIFO)
            while (RecentMemories.Count > 15)
            {
                RecentMemories.RemoveAt(0);
            }

            // Update event count
            if (!string.IsNullOrEmpty(memory.EventId))
            {
                string key = $"{memory.EventId}:{memory.ChoiceId}";
                if (!EventCounts.ContainsKey(key))
                    EventCounts[key] = 0;
                EventCounts[key]++;
            }

            // Update relationship based on impact
            RelationshipScore = Math.Max(-100, Math.Min(100, RelationshipScore + memory.ImpactScore));

            // Build trust over time
            if (memory.ImpactScore > 0)
            {
                TrustLevel = Math.Min(100, TrustLevel + (memory.ImpactScore / 2));
            }
            else if (memory.ImpactScore < -3)
            {
                TrustLevel = Math.Max(0, TrustLevel + memory.ImpactScore);
            }

            LastInteraction = CampaignTime.Now;
        }

        /// <summary>
        /// Process memory decay - remove memories older than 30 days.
        /// </summary>
        public void ProcessMemoryDecay()
        {
            var cutoffDate = CampaignTime.Now - CampaignTime.Days(30);
            RecentMemories.RemoveAll(m => m.Date < cutoffDate);
        }

        /// <summary>
        /// Check if player has a corruption pattern.
        /// </summary>
        public bool HasCorruptionPattern()
        {
            int corruptCount = 0;
            foreach (var kvp in EventCounts)
            {
                if (kvp.Key.Contains("corrupt") || kvp.Key.Contains("bribe") || kvp.Key.Contains("steal"))
                {
                    corruptCount += kvp.Value;
                }
            }
            return corruptCount >= 3;
        }

        /// <summary>
        /// Get recent memories of a specific type.
        /// </summary>
        public IEnumerable<MemoryEntry> GetRecentMemoriesOfType(MemoryType type, int maxDays = 7)
        {
            var cutoff = CampaignTime.Now - CampaignTime.Days(maxDays);
            foreach (var memory in RecentMemories)
            {
                if (memory.Type == type && memory.Date >= cutoff)
                {
                    yield return memory;
                }
            }
        }

        /// <summary>
        /// Mark this leader as dead.
        /// </summary>
        public void MarkDead(string cause)
        {
            IsAlive = false;
            DateOfDeath = CampaignTime.Now;
            CauseOfDeath = cause;
        }

        /// <summary>
        /// Increment days served.
        /// </summary>
        public void IncrementDaysServed()
        {
            DaysServedUnder++;
        }
    }

    /// <summary>
    /// Context for generating lance leader reactions.
    /// </summary>
    public class LanceLeaderContext
    {
        public int HeatLevel { get; set; }
        public int DisciplineLevel { get; set; }
        public int LanceReputation { get; set; }
        public int PlayerTier { get; set; }
        public bool IsInBattle { get; set; }
        public bool IsOnLeave { get; set; }
        public string CurrentDuty { get; set; }
    }

    /// <summary>
    /// Lance leader reaction result.
    /// </summary>
    public class LanceLeaderReaction
    {
        public DialogTone Tone { get; set; }
        public string Opening { get; set; }
        public bool ReferencesPastEvent { get; set; }
        public MemoryEntry ReferencedMemory { get; set; }
        public bool WarningGiven { get; set; }
        public string WarningText { get; set; }
    }

    /// <summary>
    /// Save container definitions for persistent lance leaders.
    /// </summary>
    public class PersistentLanceLeaderSaveDefiner : SaveableTypeDefiner
    {
        public PersistentLanceLeaderSaveDefiner() : base(735320) { }

        protected override void DefineClassTypes()
        {
            AddClassDefinition(typeof(MemoryEntry), 1);
            AddClassDefinition(typeof(LanceLeaderBackground), 2);
            AddClassDefinition(typeof(PersistentLanceLeader), 3);
        }

        protected override void DefineEnumTypes()
        {
            // Enum IDs start at 100 to avoid collision with class IDs (1-3)
            AddEnumDefinition(typeof(PersonalityTrait), 100);
            AddEnumDefinition(typeof(SecondaryTrait), 101);
            AddEnumDefinition(typeof(MemoryType), 102);
            AddEnumDefinition(typeof(DialogTone), 103);
        }

        protected override void DefineContainerDefinitions()
        {
            ConstructContainerDefinition(typeof(List<MemoryEntry>));
            ConstructContainerDefinition(typeof(List<string>));
            ConstructContainerDefinition(typeof(Dictionary<string, int>));
            ConstructContainerDefinition(typeof(Dictionary<string, PersistentLanceLeader>));
        }
    }
}

