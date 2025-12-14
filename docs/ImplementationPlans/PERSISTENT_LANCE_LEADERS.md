# Persistent Lance Leaders with Memory System

**Purpose:** Design document for generating unique, culture-appropriate lance leaders for each lord's lance, with persistent memory that tracks and reacts to player choices and events.

**Last Updated:** December 14, 2025  
**Version:** 1.0  
**Status:** Design Phase

---

## Table of Contents

1. [System Overview](#system-overview)
2. [Lance Leader Generation](#lance-leader-generation)
3. [Persistence Architecture](#persistence-architecture)
4. [Memory & Reaction System](#memory--reaction-system)
5. [Personality System](#personality-system)
6. [Death & Replacement](#death--replacement)
7. [Integration Points](#integration-points)
8. [Implementation Guide](#implementation-guide)

---

## System Overview

### Core Concept

**Current State:**
- Lance leaders are generic placeholders
- No memory of player actions
- Same generic dialogue for everyone
- No sense of building relationships

**Desired State:**
- Each lord has a **unique lance leader** for their lance
- Lance leader is **generated once** and persists for entire game
- Lance leader **remembers** player choices, event outcomes, relationship history
- Lance leader **reacts differently** based on past interactions
- Lance leader can **die** and be replaced with newly generated character
- **Culture-appropriate** names, personalities, and behaviors

### Key Features

```
┌────────────────────────────────────────────────────────────┐
│  PERSISTENT LANCE LEADER SYSTEM                             │
├────────────────────────────────────────────────────────────┤
│                                                             │
│  1. GENERATION (per Lord)                                  │
│     • Culture-appropriate name & epithet                   │
│     • Personality traits (stern, fair, pragmatic, etc.)    │
│     • Background story elements                            │
│     • Combat skills & experience                           │
│                                                             │
│  2. MEMORY (tracks recent actions)                         │
│     • Last 10-15 significant player choices                │
│     • Event outcomes (success/failure/corrupt)             │
│     • Relationship changes (+/- Lance Rep)                 │
│     • Heat/Discipline incidents player was involved in     │
│                                                             │
│  3. REACTIONS (dynamic dialogue)                           │
│     • References past events in conversations              │
│     • Tone changes based on relationship                   │
│     • Warnings if Heat/Discipline rising                   │
│     • Praise for consistent good performance               │
│                                                             │
│  4. PERSISTENCE (saved per Lord)                           │
│     • One lance leader per lord                            │
│     • Survives lord changes                                │
│     • Only regenerates on death                            │
│     • Memory persists across saves                         │
│                                                             │
└────────────────────────────────────────────────────────────┘
```

### Comparison to Quartermaster System

**Similar to existing Quartermaster:**
- ✅ Culture-appropriate name generation
- ✅ Persistent character per context (lord's lance)
- ✅ References player relationship state
- ✅ Professional military role

**New Features:**
- ✅ **Memory system** - remembers specific events
- ✅ **Dynamic reactions** - dialogue changes based on history
- ✅ **Personality traits** - affects how they respond
- ✅ **Death mechanics** - can be killed and replaced
- ✅ **Relationship progression** - builds over time

---

## Lance Leader Generation

### Generation Trigger

**Lance leader is generated when:**
1. Player enlists with a new lord for the first time
2. Player re-enlists with a lord (uses existing if already generated)
3. Previous lance leader dies (regenerates new one)

**Not Generated:**
- When player switches duties (same lance leader)
- When player changes formation (same lance leader)
- On save/load (loads existing)

### Character Data Structure

```csharp
public class PersistentLanceLeader
{
    // Identity
    public string LordId { get; set; }              // Hero ID of lord
    public string Name { get; set; }                // e.g., "Marcus Ironarm"
    public string FirstName { get; set; }           // e.g., "Marcus"
    public string Epithet { get; set; }             // e.g., "Ironarm"
    public string Culture { get; set; }             // e.g., "empire"
    public string RankTitle { get; set; }           // e.g., "Decanus"
    public bool IsFemale { get; set; }              // Gender
    
    // Personality
    public PersonalityTrait PrimaryTrait { get; set; }      // Dominant trait
    public PersonalityTrait SecondaryTrait { get; set; }    // Secondary trait
    public int Strictness { get; set; }                     // 0-100 (discipline-focused)
    public int Pragmatism { get; set; }                     // 0-100 (results-focused)
    public int Loyalty { get; set; }                        // 0-100 (lord-focused)
    
    // Background (for flavor)
    public int YearsOfService { get; set; }         // 5-30 years
    public string FormerRole { get; set; }          // "cavalry", "infantry", etc.
    public bool IsVeteran { get; set; }             // Seen major battles
    public List<string> BattleScars { get; set; }   // "missing_finger", "eye_scar"
    
    // Relationship with Player
    public int RelationshipScore { get; set; }      // Mirrors Lance Rep
    public int TrustLevel { get; set; }             // 0-100 (built over time)
    public CampaignTime FirstMetDate { get; set; }  // When player joined
    public int DaysServedUnder { get; set; }        // Time under this leader
    
    // Memory System
    public Queue<MemoryEntry> RecentMemories { get; set; }  // Last 15 events
    public Dictionary<string, int> EventCounts { get; set; } // "corrupt_choice": 3
    public CampaignTime LastInteraction { get; set; }        // Last conversation
    
    // State
    public bool IsAlive { get; set; }               // false if killed
    public CampaignTime DateOfDeath { get; set; }   // When died (if dead)
    public string CauseOfDeath { get; set; }        // "battle", "illness", etc.
}
```

### Generation Algorithm

**Step 1: Determine Culture**
```csharp
public string GetLanceLeaderCulture(Hero lord)
{
    // Use lord's culture as base
    var lordCulture = lord.Culture.StringId;
    
    // 90% same as lord, 10% cosmopolitan (different culture veteran)
    if (MBRandom.RandomFloat < 0.9f)
        return lordCulture;
    
    // 10% chance of veteran from allied culture
    var alliedCultures = GetAlliedCultures(lord.MapFaction);
    return alliedCultures.GetRandomElement().StringId;
}
```

**Step 2: Generate Name**
```csharp
public (string firstName, string epithet, bool isFemale) GenerateName(string culture)
{
    var namePool = LoadNamePool(culture); // from name_pools.json
    
    // Female chance: 20% base, modified by culture
    bool isFemale = MBRandom.RandomFloat < GetFemaleLanceLeaderChance(culture);
    
    var firstNames = isFemale ? namePool.female_first : namePool.male_first;
    var firstName = firstNames.GetRandomElement();
    
    // 70% chance of epithet (veterans earn epithets)
    string epithet = MBRandom.RandomFloat < 0.7f 
        ? namePool.epithets.GetRandomElement() 
        : null;
    
    return (firstName, epithet, isFemale);
}
```

**Step 3: Generate Personality**
```csharp
public (PersonalityTrait primary, PersonalityTrait secondary) GeneratePersonality()
{
    // Primary trait (dominant characteristic)
    var primaryOptions = new[] {
        PersonalityTrait.Stern,      // 20% - strict disciplinarian
        PersonalityTrait.Fair,       // 25% - balanced, just
        PersonalityTrait.Pragmatic,  // 20% - results-focused
        PersonalityTrait.Fatherly,   // 15% - protective, mentoring
        PersonalityTrait.Ambitious,  // 10% - career-driven
        PersonalityTrait.Cynical,    // 10% - world-weary
    };
    
    var primary = WeightedRandom(primaryOptions);
    
    // Secondary trait (modifies primary)
    var secondaryOptions = GetCompatibleSecondaryTraits(primary);
    var secondary = secondaryOptions.GetRandomElement();
    
    return (primary, secondary);
}
```

**Step 4: Generate Background**
```csharp
public LanceLeaderBackground GenerateBackground(string culture)
{
    return new LanceLeaderBackground
    {
        YearsOfService = MBRandom.RandomInt(8, 25),
        FormerRole = GetRandomRole(culture),
        IsVeteran = MBRandom.RandomFloat < 0.6f, // 60% are battle veterans
        BattleScars = GenerateBattleScars(),
        SpecialQuality = GenerateSpecialQuality() // "tactical_genius", "brutal", etc.
    };
}
```

**Step 5: Initialize Attributes**
```csharp
public void InitializeAttributes(PersistentLanceLeader leader)
{
    // Based on personality
    switch (leader.PrimaryTrait)
    {
        case PersonalityTrait.Stern:
            leader.Strictness = MBRandom.RandomInt(70, 95);
            leader.Pragmatism = MBRandom.RandomInt(40, 60);
            leader.Loyalty = MBRandom.RandomInt(60, 80);
            break;
            
        case PersonalityTrait.Pragmatic:
            leader.Strictness = MBRandom.RandomInt(30, 50);
            leader.Pragmatism = MBRandom.RandomInt(75, 95);
            leader.Loyalty = MBRandom.RandomInt(40, 60);
            break;
            
        case PersonalityTrait.Fair:
            leader.Strictness = MBRandom.RandomInt(50, 70);
            leader.Pragmatism = MBRandom.RandomInt(50, 70);
            leader.Loyalty = MBRandom.RandomInt(60, 80);
            break;
            
        // ... other traits
    }
    
    // Initialize memory
    leader.RecentMemories = new Queue<MemoryEntry>(15); // Max 15 memories
    leader.EventCounts = new Dictionary<string, int>();
    leader.RelationshipScore = 0; // Start neutral
    leader.TrustLevel = 25; // Low trust initially
}
```

### Example Generated Leaders

**Example 1: Empire Lance Leader (Stern)**
```
Name: Gaius "One-Eye" Corvus
Rank: Decanus
Culture: Empire
Gender: Male

Personality:
- Primary: Stern (90 Strictness)
- Secondary: Honorable
- Pragmatism: 45
- Loyalty: 75

Background:
- 18 years of service
- Former: Infantry veteran
- Battles: Siege of Onira, Battle of Danustica
- Scars: Lost left eye, sword scar on forearm

Character:
"Gaius is a hard man forged by years of brutal campaigning. He expects 
perfection and has no patience for excuses. But those who earn his respect 
find him unwaveringly loyal. He lost his eye at Onira holding the line 
when others fled."

Dialogue Style:
- Blunt, direct
- References military protocol
- Harsh when disappointed
- Rare but meaningful praise
```

**Example 2: Battanian Lance Leader (Fatherly)**
```
Name: Erwan "Wolfborn" ap Cadell
Rank: Cennaire
Culture: Battania
Gender: Male

Personality:
- Primary: Fatherly (35 Strictness)
- Secondary: Wise
- Pragmatism: 60
- Loyalty: 85

Background:
- 22 years of service
- Former: Scout/Hunter
- Battles: Border conflicts, countless skirmishes
- Scars: Arrow scar on shoulder, wolf bite on leg

Character:
"Erwan is an old wolf who's seen too many young warriors die. He believes 
in teaching rather than punishing. His warriors are fiercely loyal because 
he treats them like family. The wolf that bit him? He killed with his 
bare hands."

Dialogue Style:
- Conversational, uses metaphors
- Offers advice rather than orders
- Disappointed rather than angry
- Warm praise when deserved
```

**Example 3: Khuzait Lance Leader (Pragmatic)**
```
Name: Sübedei "Arrow-Eye"
Rank: Arban-u Darga
Culture: Khuzait
Gender: Male

Personality:
- Primary: Pragmatic (45 Strictness)
- Secondary: Cunning
- Pragmatism: 90
- Loyalty: 55

Background:
- 14 years of service
- Former: Horse Archer, Scout
- Battles: Steppe wars, raids
- Scars: None visible (careful fighter)

Character:
"Sübedei cares about results, not how you get them. Win battles, complete 
missions, stay useful—that's what matters. He has little patience for 
honor or protocol. On the steppe, only the clever survive."

Dialogue Style:
- Efficient, no wasted words
- Judges by results, not methods
- Indifferent to corruption if it works
- Respects competence above all
```

---

## Persistence Architecture

### Storage Model

**Primary Storage:** CampaignBehavior with Dictionary keyed by Lord ID

```csharp
public class PersistentLanceLeadersBehavior : CampaignBehaviorBase
{
    // Main storage: LordId → LanceLeader
    private Dictionary<string, PersistentLanceLeader> _lanceLeadersByLord;
    
    // Quick lookup: Which lord is player currently serving?
    private string _currentLordId;
    
    // Cache for current lance leader
    private PersistentLanceLeader _currentLanceLeader;
    
    public override void SyncData(IDataStore dataStore)
    {
        dataStore.SyncData("persistent_lance_leaders", ref _lanceLeadersByLord);
        dataStore.SyncData("current_lord_id", ref _currentLordId);
        
        // Rebuild cache after load
        if (dataStore.IsLoading && !string.IsNullOrEmpty(_currentLordId))
        {
            _currentLanceLeader = _lanceLeadersByLord.GetValueOrDefault(_currentLordId);
        }
    }
}
```

### Lookup Logic

```csharp
/// <summary>
/// Get or create lance leader for current lord
/// </summary>
public PersistentLanceLeader GetCurrentLanceLeader()
{
    var enlistment = EnlistmentBehavior.Instance;
    if (enlistment == null || !enlistment.IsEnlisted) 
        return null;
    
    var currentLord = enlistment.CurrentLord;
    if (currentLord == null) 
        return null;
    
    return GetOrCreateLanceLeader(currentLord);
}

/// <summary>
/// Get or create lance leader for specific lord
/// </summary>
public PersistentLanceLeader GetOrCreateLanceLeader(Hero lord)
{
    string lordId = lord.StringId;
    
    // Check if we already have a lance leader for this lord
    if (_lanceLeadersByLord.TryGetValue(lordId, out var existing))
    {
        // Check if they're still alive
        if (existing.IsAlive)
            return existing;
        
        // Dead - need to generate replacement
        ModLogger.Info("LanceLeaders", $"Lance leader {existing.Name} is dead. Generating replacement.");
    }
    
    // Generate new lance leader
    var newLeader = GenerateLanceLeader(lord);
    _lanceLeadersByLord[lordId] = newLeader;
    
    ModLogger.Info("LanceLeaders", $"Generated new lance leader: {newLeader.Name} for {lord.Name}");
    
    return newLeader;
}
```

### Re-enlistment Logic

```csharp
/// <summary>
/// Called when player enlists/re-enlists
/// </summary>
public void OnPlayerEnlisted(Hero lord)
{
    _currentLordId = lord.StringId;
    _currentLanceLeader = GetOrCreateLanceLeader(lord);
    
    // Update tracking
    _currentLanceLeader.DaysServedUnder = 0;
    
    // Increment re-enlistment counter if rejoining
    if (_currentLanceLeader.FirstMetDate != CampaignTime.Zero)
    {
        // Returning soldier
        AddMemory(new MemoryEntry
        {
            Type = MemoryType.ReEnlisted,
            Date = CampaignTime.Now,
            Description = "Player re-enlisted after leaving"
        });
    }
    else
    {
        // First time meeting
        _currentLanceLeader.FirstMetDate = CampaignTime.Now;
    }
}

/// <summary>
/// Called when player leaves service
/// </summary>
public void OnPlayerDischarged()
{
    if (_currentLanceLeader != null)
    {
        AddMemory(new MemoryEntry
        {
            Type = MemoryType.Discharged,
            Date = CampaignTime.Now,
            DaysServed = _currentLanceLeader.DaysServedUnder
        });
    }
    
    _currentLordId = null;
    _currentLanceLeader = null;
}
```

---

## Memory & Reaction System

### Memory Entry Structure

```csharp
public class MemoryEntry
{
    public MemoryType Type { get; set; }
    public CampaignTime Date { get; set; }
    public string EventId { get; set; }          // e.g., "qm_corruption_choice"
    public string ChoiceId { get; set; }         // e.g., "take_bribe"
    public int ImpactScore { get; set; }         // -10 to +10
    public string Description { get; set; }      // Human-readable summary
    public Dictionary<string, float> Effects { get; set; } // Heat, Discipline changes
}

public enum MemoryType
{
    DutyEvent,          // Regular duty event choice
    CorruptionChoice,   // Player chose corrupt path
    HeroicAction,       // Player went above and beyond
    Failure,            // Player failed or fumbled
    DisciplineIssue,    // Heat or Discipline gained
    PromotionMoment,    // Player promoted in rank
    BattlePerformance,  // How player did in battle
    ReEnlisted,         // Player returned after leaving
    Discharged          // Player left service
}
```

### Adding Memories

```csharp
/// <summary>
/// Add memory of player event choice
/// </summary>
public void RecordEventChoice(string eventId, string choiceId, int impactScore, Dictionary<string, float> effects)
{
    var leader = GetCurrentLanceLeader();
    if (leader == null) return;
    
    var memory = new MemoryEntry
    {
        Type = DetermineMemoryType(choiceId, effects),
        Date = CampaignTime.Now,
        EventId = eventId,
        ChoiceId = choiceId,
        ImpactScore = impactScore,
        Effects = effects,
        Description = GenerateMemoryDescription(eventId, choiceId)
    };
    
    // Add to queue (max 15, FIFO)
    leader.RecentMemories.Enqueue(memory);
    if (leader.RecentMemories.Count > 15)
        leader.RecentMemories.Dequeue(); // Remove oldest
    
    // Update event count
    string eventKey = $"{eventId}:{choiceId}";
    if (!leader.EventCounts.ContainsKey(eventKey))
        leader.EventCounts[eventKey] = 0;
    leader.EventCounts[eventKey]++;
    
    // Update relationship based on impact
    leader.RelationshipScore += impactScore;
    
    // Build trust over time (positive actions build trust faster)
    if (impactScore > 0)
        leader.TrustLevel = Math.Min(100, leader.TrustLevel + (impactScore / 2));
    else if (impactScore < -3)
        leader.TrustLevel = Math.Max(0, leader.TrustLevel + impactScore);
    
    ModLogger.Info("LanceLeaders", $"{leader.Name} remembers: {memory.Description} (Impact: {impactScore})");
}

private MemoryType DetermineMemoryType(string choiceId, Dictionary<string, float> effects)
{
    // Check for corruption
    if (effects.ContainsKey("heat") && effects["heat"] > 2)
        return MemoryType.CorruptionChoice;
    
    // Check for heroic
    if (effects.ContainsKey("lance_reputation") && effects["lance_reputation"] > 8)
        return MemoryType.HeroicAction;
    
    // Check for discipline issues
    if (effects.ContainsKey("discipline") && effects["discipline"] > 2)
        return MemoryType.DisciplineIssue;
    
    return MemoryType.DutyEvent;
}
```

### Memory Decay

```csharp
/// <summary>
/// Older memories fade, but patterns persist
/// </summary>
public void ProcessMemoryDecay()
{
    var leader = GetCurrentLanceLeader();
    if (leader == null) return;
    
    // Memories older than 30 days start to fade
    var cutoffDate = CampaignTime.Now - CampaignTime.Days(30);
    
    while (leader.RecentMemories.Count > 0 && 
           leader.RecentMemories.Peek().Date < cutoffDate)
    {
        var oldMemory = leader.RecentMemories.Dequeue();
        ModLogger.Debug("LanceLeaders", $"{leader.Name} forgets old memory: {oldMemory.Description}");
    }
    
    // But patterns persist in EventCounts
    // (They remember you're generally corrupt, even if they forget specific instances)
}
```

### Reaction System

```csharp
/// <summary>
/// Generate lance leader reaction to current player state
/// </summary>
public LanceLeaderReaction GetReaction(LanceLeaderContext context)
{
    var leader = GetCurrentLanceLeader();
    if (leader == null) return null;
    
    var reaction = new LanceLeaderReaction
    {
        Tone = DetermineTone(leader, context),
        Opening = GenerateOpening(leader, context),
        ReferencesPastEvent = ShouldReferencePastEvent(leader, context),
        WarningGiven = ShouldGiveWarning(leader, context)
    };
    
    return reaction;
}

private DialogTone DetermineTone(PersistentLanceLeader leader, LanceLeaderContext context)
{
    // Base tone on relationship and personality
    int relationship = leader.RelationshipScore;
    
    // Stern leaders are always formal
    if (leader.PrimaryTrait == PersonalityTrait.Stern)
        return relationship > 50 ? DialogTone.RespectfullyFormal : DialogTone.ColdlyFormal;
    
    // Fatherly leaders warm up quickly
    if (leader.PrimaryTrait == PersonalityTrait.Fatherly)
        return relationship > 30 ? DialogTone.WarmFamilial : DialogTone.PatientlyFormal;
    
    // Pragmatic leaders care about results
    if (leader.PrimaryTrait == PersonalityTrait.Pragmatic)
        return relationship > 40 ? DialogTone.CasuallyProfessional : DialogTone.BusinessLike;
    
    // Fair leaders are balanced
    return relationship > 40 ? DialogTone.FriendlyProfessional : DialogTone.NeutralProfessional;
}

private string GenerateOpening(PersistentLanceLeader leader, LanceLeaderContext context)
{
    // Check for immediate issues first
    if (context.HeatLevel >= 5)
        return GenerateHeatWarningOpening(leader, context);
    
    if (context.DisciplineLevel >= 5)
        return GenerateDisciplineWarningOpening(leader, context);
    
    // Reference recent positive/negative memories
    var recentMemories = leader.RecentMemories.OrderByDescending(m => m.Date).Take(3);
    
    var recentPositive = recentMemories.FirstOrDefault(m => m.ImpactScore > 5);
    if (recentPositive != null && (CampaignTime.Now - recentPositive.Date).ToDays < 7)
    {
        return GeneratePraiseOpening(leader, recentPositive);
    }
    
    var recentNegative = recentMemories.FirstOrDefault(m => m.ImpactScore < -5);
    if (recentNegative != null && (CampaignTime.Now - recentNegative.Date).ToDays < 7)
    {
        return GenerateDisappointmentOpening(leader, recentNegative);
    }
    
    // Default greeting based on relationship
    return GenerateStandardGreeting(leader, context);
}
```

### Dynamic Dialogue Examples

**High Trust, Stern Leader:**
```csharp
// Gaius "One-Eye" (Stern, 80 Relationship, 75 Trust)
Context: Assigning new duty

COLD (Low Rep): 
"You. Quartermaster duty. Don't make me regret this."

WARMING (Mid Rep):
"Quartermaster duty. You've proven yourself capable. Don't disappoint."

TRUSTED (High Rep):
"I'm putting you on quartermaster duty. You've earned my trust. 
I know you'll handle it right."

VETERAN (Very High Rep + Long Service):
"Marcus to {PLAYER_SHORT}. I need someone I can count on for 
quartermaster duty. That's you now. You've earned it."
```

**High Trust, Fatherly Leader:**
```csharp
// Erwan "Wolfborn" (Fatherly, 80 Relationship, 85 Trust)
Context: After player made risky but successful choice

COLD (Low Rep):
"Reckless. You got lucky this time."

WARMING (Mid Rep):
"Bold move. Dangerous, but it worked. Remember: luck runs out."

TRUSTED (High Rep):
"Ha! That's the kind of thinking that keeps warriors alive. 
Risky, but you knew what you were doing."

VETERAN (Very High Rep + Long Service):
"My old wolf heart nearly stopped when I heard what you did. 
But that's why you're one of my best. You know when to take risks."
```

**Pragmatic Leader with Corruption Pattern:**
```csharp
// Sübedei "Arrow-Eye" (Pragmatic, 60 Relationship, Corruption detected)
Context: Player has made 3 corrupt choices recently

DIRECT:
"I notice things go missing around you. Gold finds its way into 
your pockets. I don't care—as long as the job gets done and the 
lord doesn't notice. But if this blows back on the lance, you'll 
answer for it."

vs. SAME LEADER, CLEAN RECORD:
"You do the work, no complaints. That's what matters. Keep it up."
```

---

## Personality System

### Personality Traits

**Primary Traits (Dominant Behavior):**

| Trait | Strictness | Pragmatism | Loyalty | Behavior |
|-------|-----------|------------|---------|----------|
| **Stern** | 70-95 | 40-60 | 60-80 | Demands perfection, harsh discipline, rare praise |
| **Fair** | 50-70 | 50-70 | 60-80 | Balanced, just, rewards merit, punishes fairly |
| **Pragmatic** | 30-50 | 75-95 | 40-60 | Results-focused, flexible on methods, indifferent to honor |
| **Fatherly** | 30-50 | 55-75 | 70-90 | Protective, mentoring, disappointed rather than angry |
| **Ambitious** | 45-65 | 70-90 | 50-70 | Career-driven, political, uses lance for advancement |
| **Cynical** | 40-60 | 65-85 | 30-50 | World-weary, expects worst, dry humor, seen it all |

**Secondary Traits (Modifies Primary):**

| Trait | Effect |
|-------|--------|
| **Honorable** | Won't tolerate corruption, values integrity |
| **Cunning** | Smart, tactical, sees through deception |
| **Brutal** | Harsh punishments, intimidating, feared |
| **Protective** | Shields lance from danger, paternal |
| **Inspiring** | Boosts morale, natural leader |
| **Cautious** | Risk-averse, values safety over glory |
| **Vengeful** | Remembers slights, holds grudges |
| **Wise** | Gives good advice, learned from experience |

### Trait Combinations

**Stern + Honorable:**
```
"By-the-book disciplinarian who expects absolute integrity. 
Will not tolerate rule-breaking or corruption. Respected but feared."

Example: Gaius "One-Eye"
Dialogue: "You took that bribe. Don't insult me by denying it. 
          In my lance, we follow the code. Always. This is your 
          only warning."
```

**Fatherly + Wise:**
```
"Old veteran who's seen everything. Teaches rather than punishes. 
Warriors are loyal unto death."

Example: Erwan "Wolfborn"
Dialogue: "I know what you did. I was young once too. But let me 
          tell you something, pup: shortcuts have a way of leading 
          you off a cliff. Learn from my scars, not your own."
```

**Pragmatic + Cunning:**
```
"Smart operator who cares only about results. Sees through 
deception but doesn't care if it works."

Example: Sübedei "Arrow-Eye"
Dialogue: "Clever. You got what we needed and made some gold on the 
          side. Just remember: the lord's quartermaster is smarter 
          than you think. Don't get caught."
```

**Ambitious + Inspiring:**
```
"Rising star using lance as stepping stone. Charismatic but 
self-serving."

Example: Baldwin "the Bold"
Dialogue: "Excellent work. The commander noticed—I made sure of it. 
          Keep performing like this and we'll both move up. My success 
          is your success."
```

### Reaction Matrix

| Player Action | Stern | Fair | Pragmatic | Fatherly | Ambitious | Cynical |
|---------------|-------|------|-----------|----------|-----------|---------|
| **Corruption** | Furious | Disappointed | Indifferent | Saddened | Opportunistic | Expected |
| **Heroism** | Approving | Proud | Practical | Worried | Exploitative | Surprised |
| **Failure** | Harsh | Constructive | Frustrated | Understanding | Critical | Unsurprised |
| **Success** | Terse praise | Warm praise | Acknowledgment | Joyful | Calculating | Rare praise |
| **Discipline Issue** | Extra duty | Fair punishment | Warning | Disappointed talk | Reputation concern | Dark humor |

---

## Death & Replacement

### Death Triggers

**Lance leaders can die from:**

1. **Battle Deaths** (Combat)
   - During major battles (0.5-2% chance based on battle severity)
   - If player's side loses badly (2-5% chance)
   - Heroic last stand narrative events (rare, story-driven)

2. **Assassination** (Story Events)
   - Enemy infiltrators (war events)
   - Internal conflicts (if lord at low loyalty)
   - Pay tension mutiny events (extreme cases)

3. **Disease** (Rare)
   - Camp outbreak events (0.1% chance during epidemics)
   - Medical risk escalation consequences

4. **Old Age** (Very Rare)
   - If lance leader has 25+ years service and player serves 5+ years under them
   - Natural death event, peaceful passing

5. **Narrative Deaths** (Scripted)
   - Story events where lance leader sacrifice themselves
   - "Cover the retreat" events
   - "Hold the line" last stands

### Death Event Example

```json
{
  "id": "lance_leader_battle_death",
  "category": "lance_simulation",
  "channel": "incident",
  "triggers": {
    "all": ["is_enlisted", "after_battle", "battle_severe_loss"]
  },
  "titleId": "lance_leader_death_title",
  "title": "Your Lance Leader Has Fallen",
  "body": "The word spreads through the camp quickly. {LANCE_LEADER_NAME} fell in the battle—struck down leading a counter-charge to save the retreating infantry.\n\nThe lance is in shock. {LANCE_LEADER_SHORT} was the backbone of this unit. Who will lead you now?",
  "notification_type": "critical",
  "effects": {
    "lance_leader_state": "dead",
    "lance_reputation": -10,
    "morale": -15
  },
  "flags_set": ["lance_leader_dead", "awaiting_replacement"],
  "follow_up_event": "lance_leader_replacement",
  "follow_up_delay_hours": 48
}
```

### Replacement System

```csharp
/// <summary>
/// Called when lance leader dies
/// </summary>
public void OnLanceLeaderDeath(PersistentLanceLeader deceased, string causeOfDeath)
{
    // Mark as dead
    deceased.IsAlive = false;
    deceased.DateOfDeath = CampaignTime.Now;
    deceased.CauseOfDeath = causeOfDeath;
    
    ModLogger.Info("LanceLeaders", $"{deceased.Name} has died: {causeOfDeath}");
    
    // Trigger memorial/reaction events
    TriggerMemorialEvent(deceased);
    
    // Schedule replacement (2-7 days)
    int daysUntilReplacement = MBRandom.RandomInt(2, 8);
    
    CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, () => 
    {
        if (--daysUntilReplacement <= 0)
        {
            GenerateReplacement(deceased);
        }
    });
}

/// <summary>
/// Generate replacement lance leader
/// </summary>
private void GenerateReplacement(PersistentLanceLeader deceased)
{
    var lord = Hero.FindFirst(h => h.StringId == deceased.LordId);
    if (lord == null) return;
    
    // Generate new leader
    var replacement = GenerateLanceLeader(lord);
    
    // Store reference to predecessor
    replacement.PredecessorName = deceased.Name;
    replacement.PredecessorCauseOfDeath = deceased.CauseOfDeath;
    
    // Replace in storage
    _lanceLeadersByLord[deceased.LordId] = replacement;
    _currentLanceLeader = replacement;
    
    // Trigger introduction event
    TriggerReplacementIntroductionEvent(replacement, deceased);
    
    ModLogger.Info("LanceLeaders", $"New lance leader assigned: {replacement.Name}");
}
```

### Replacement Introduction Event

```json
{
  "id": "lance_leader_introduction_after_death",
  "category": "lance_simulation",
  "channel": "inquiry",
  "titleId": "new_lance_leader_title",
  "title": "New Lance Leader",
  "body": "The lord has assigned a new lance leader. {LANCE_LEADER_NAME} arrives at dawn, battle-worn and serious.\n\n\"{PLAYER_SHORT}. I'm {LANCE_LEADER_RANK} {LANCE_LEADER_SHORT}. I know you served under {PREDECESSOR_NAME}. {PREDECESSOR_PRONOUN} was a good soldier.\"\n\nHe pauses, looking over the diminished lance.\n\n\"We've all lost people. But the work continues. I expect discipline and competence. Give me that, and we'll get along fine.\"",
  "options": [
    {
      "id": "respectful",
      "text": "[Respectful] \"Yes, {LANCE_LEADER_RANK}. You can count on me.\"",
      "effects": { "lance_reputation": 2 },
      "outcome": "He nods, appraising. \"We'll see.\""
    },
    {
      "id": "mention_predecessor",
      "text": "[Honor] \"{PREDECESSOR_NAME} was a great leader. Hard to replace.\"",
      "effects": { "lance_reputation": 3 },
      "outcome": "His expression softens slightly. \"Then honor {PREDECESSOR_PRONOUN} by being the soldier {PREDECESSOR_PRONOUN} taught you to be.\""
    },
    {
      "id": "suspicious",
      "text": "[Wary] \"We'll see how you lead before I judge.\"",
      "effects": { "lance_reputation": -2 },
      "outcome": "A hint of steel in his eyes. \"Fair enough. Prove yourself worthy of this lance, and I'll do the same.\""
    }
  ]
}
```

### Memorial System

```csharp
/// <summary>
/// Trigger memorial event for fallen lance leader
/// </summary>
private void TriggerMemorialEvent(PersistentLanceLeader deceased)
{
    // Schedule memorial 24-48 hours after death
    var memorialEvent = new
    {
        deceased.Name,
        deceased.Epithet,
        deceased.YearsOfService,
        DaysServedUnder = deceased.DaysServedUnder,
        PlayerRelationship = deceased.RelationshipScore
    };
    
    // Customize memorial based on relationship
    string memorialTone = deceased.RelationshipScore > 50 
        ? "beloved_leader" 
        : deceased.RelationshipScore < -20 
            ? "distant_leader" 
            : "respected_leader";
    
    // Queue memorial event with customized content
    EventsManager.QueueMemorialEvent(memorialEvent, memorialTone);
}
```

---

## Integration Points

### 1. Duty Events System

**Integration:** Lance leader comments on duty event choices

```csharp
// In PersistentLanceLeadersBehavior
public void OnDutyEventCompleted(string eventId, string choiceId, EventOutcome outcome)
{
    var leader = GetCurrentLanceLeader();
    if (leader == null) return;
    
    // Calculate impact score based on choice
    int impactScore = CalculateImpactScore(outcome);
    
    // Record memory
    RecordEventChoice(eventId, choiceId, impactScore, outcome.Effects);
    
    // Potentially trigger immediate reaction
    if (ShouldTriggerImmediateReaction(outcome, leader))
    {
        TriggerLanceLeaderReactionEvent(leader, eventId, outcome);
    }
}
```

**Example Reaction:**

```json
{
  "id": "lance_leader_reacts_corruption",
  "category": "lance_reaction",
  "channel": "inquiry",
  "triggers": {
    "all": ["is_enlisted", "recent_corrupt_choice", "leader_low_tolerance"]
  },
  "title": "Lance Leader Summons You",
  "body": "{LANCE_LEADER_SHORT} intercepts you after muster. His expression is hard.\n\n\"A word. Privately.\"\n\nOnce alone: \"I know what you did with the quartermaster. You think I don't hear things? I've been doing this for {LEADER_YEARS} years.\"\n\n[Stern Leader, Low Corruption Tolerance]\n\"In my lance, we don't cheat the system. We don't steal. We do our jobs with honor. This is your only warning. Clear?\"",
  "options": [
    {
      "id": "apologize",
      "text": "[Apologetic] \"I'm sorry. It won't happen again.\"",
      "effects": { "heat": -1, "lance_reputation": -3 },
      "outcome": "He watches you for a long moment. \"See that it doesn't.\""
    },
    {
      "id": "defiant",
      "text": "[Defiant] \"Everyone does it. I'm just smarter about it.\"",
      "effects": { "discipline": 2, "lance_reputation": -8, "leader_trust": -20 },
      "outcome": "His face goes cold. \"Get out of my sight. And know this: I'll be watching you.\""
    }
  ]
}
```

### 2. Camp News System

**Integration:** Lance leader mentioned in news dispatches

```csharp
// In EnlistedNewsBehavior
public void GenerateLanceLeaderNews()
{
    var leader = PersistentLanceLeadersBehavior.Instance?.GetCurrentLanceLeader();
    if (leader == null) return;
    
    // Generate news based on leader state
    if (leader.RelationshipScore > 70 && leader.TrustLevel > 80)
    {
        AddNewsItem(new NewsItem
        {
            Category = NewsCategory.Lance,
            Title = "Lance Leader's Commendation",
            Body = $"{leader.RankTitle} {leader.Name} publicly praised your recent performance. " +
                   $"\"One of my best soldiers,\" {PronounHe(leader)} said. Word spreads quickly."
        });
    }
    
    // News about lance leader's past
    if (leader.IsVeteran && MBRandom.RandomFloat < 0.05f)
    {
        AddNewsItem(new NewsItem
        {
            Category = NewsCategory.LanceLore,
            Title = $"Veteran's Tale: {leader.Epithet}",
            Body = GenerateVeteranStory(leader)
        });
    }
}
```

### 3. Escalation System

**Integration:** Lance leader reacts to Heat/Discipline thresholds

```csharp
// In EscalationManager
public void OnThresholdReached(EscalationTrack track, int newLevel)
{
    var leader = PersistentLanceLeadersBehavior.Instance?.GetCurrentLanceLeader();
    if (leader == null) return;
    
    // Heat warning from lance leader (before official warnings)
    if (track == EscalationTrack.Heat && newLevel == 3)
    {
        // Unofficial warning based on leader personality
        if (leader.Strictness > 60)
        {
            TriggerPrivateWarning(leader, "heat", newLevel);
        }
    }
    
    // Discipline disappointment
    if (track == EscalationTrack.Discipline && newLevel == 4)
    {
        if (leader.PrimaryTrait == PersonalityTrait.Fatherly)
        {
            TriggerDisappointedTalk(leader, "discipline");
        }
        else if (leader.PrimaryTrait == PersonalityTrait.Stern)
        {
            TriggerHarshReprimand(leader, "discipline");
        }
    }
}
```

### 4. Promotion System

**Integration:** Lance leader involved in promotion decisions

```csharp
// In promotion events
public bool DeterminePromotionApproval()
{
    var leader = PersistentLanceLeadersBehavior.Instance?.GetCurrentLanceLeader();
    if (leader == null) return true; // Default approve
    
    // Lance leader can block promotion if relationship very bad
    if (leader.RelationshipScore < -30 && leader.TrustLevel < 20)
    {
        // Stern or strict leaders are more likely to block
        if (leader.Strictness > 70 && leader.Discipline > 5)
        {
            TriggerPromotionBlockedEvent(leader);
            return false;
        }
    }
    
    // Lance leader can recommend promotion if relationship very good
    if (leader.RelationshipScore > 60 && leader.TrustLevel > 70)
    {
        TriggerPromotionRecommendationEvent(leader);
        return true; // Helps override other factors
    }
    
    return true;
}
```

### 5. Battle System

**Integration:** Lance leader can be injured/killed in battles

```csharp
// In post-battle processing
public void OnBattleCompleted(Battle battle)
{
    var leader = PersistentLanceLeadersBehavior.Instance?.GetCurrentLanceLeader();
    if (leader == null || !leader.IsAlive) return;
    
    // Check if player's side was in battle
    bool playerSideInBattle = /* ... */;
    if (!playerSideInBattle) return;
    
    // Death chance based on battle severity
    float deathChance = CalculateLanceLeaderDeathChance(battle);
    
    if (MBRandom.RandomFloat < deathChance)
    {
        // Lance leader died in battle
        PersistentLanceLeadersBehavior.Instance.OnLanceLeaderDeath(
            leader, 
            "battle_casualty"
        );
    }
    else if (MBRandom.RandomFloat < deathChance * 5f)
    {
        // Lance leader wounded but survived
        TriggerLanceLeaderWoundedEvent(leader, battle);
    }
}
```

---

## Implementation Guide

### Phase 1: Core Generation (Week 1-2)

**Tasks:**
1. Create `PersistentLanceLeader` data class
2. Create `PersistentLanceLeadersBehavior` with save/load
3. Implement name generation from existing name pools
4. Implement personality trait system
5. Add registration to `SubModule.cs`
6. Test save/load persistence

**Deliverable:** Lance leaders generate and persist across saves

### Phase 2: Memory System (Week 3-4)

**Tasks:**
1. Create `MemoryEntry` structure
2. Implement memory queue (15 max, FIFO)
3. Hook into duty event system
4. Hook into escalation system
5. Implement memory decay (30 days)
6. Add debug logging for memory tracking

**Deliverable:** Lance leaders remember player choices

### Phase 3: Reaction System (Week 5-6)

**Tasks:**
1. Implement tone determination logic
2. Create dynamic dialogue generation
3. Create reaction event templates
4. Hook reactions into existing event system
5. Test all personality combinations
6. Balance reaction frequencies

**Deliverable:** Lance leaders react based on memory/personality

### Phase 4: Death & Replacement (Week 7-8)

**Tasks:**
1. Implement death triggers (battle, events)
2. Create replacement generation system
3. Create memorial event system
4. Create replacement introduction events
5. Test death → replacement → new relationship cycle
6. Balance death probabilities

**Deliverable:** Full death and replacement cycle works

### Phase 5: Integration & Polish (Week 9-10)

**Tasks:**
1. Integrate with camp news system
2. Integrate with promotion system
3. Create 20+ reaction events
4. Add lance leader voice variety
5. Balance all systems
6. Comprehensive playtesting

**Deliverable:** Complete, polished system

---

## Technical Architecture

### File Structure

```
src/Features/Lances/
  Behaviors/
    PersistentLanceLeadersBehavior.cs       (Main behavior)
  Data/
    PersistentLanceLeader.cs                (Data class)
    MemoryEntry.cs                          (Memory structure)
    LanceLeaderPersonality.cs               (Personality enums/logic)
    LanceLeaderReaction.cs                  (Reaction data)
  Generation/
    LanceLeaderGenerator.cs                 (Name/personality generation)
    LanceLeaderDialogueGenerator.cs         (Dynamic dialogue)
  
ModuleData/Enlisted/
  LancePersonas/
    name_pools.json                         (Existing, use as-is)
    personality_templates.json              (New, personality configs)
  Events/
    events_lance_leader_reactions.json      (New, reaction events)
    events_lance_leader_death.json          (New, death/memorial events)
```

### API Reference

```csharp
/// <summary>
/// Main API for interacting with persistent lance leaders
/// </summary>
public class PersistentLanceLeadersBehavior : CampaignBehaviorBase
{
    public static PersistentLanceLeadersBehavior Instance { get; }
    
    // Core Methods
    public PersistentLanceLeader GetCurrentLanceLeader();
    public PersistentLanceLeader GetLanceLeaderForLord(Hero lord);
    public void OnPlayerEnlisted(Hero lord);
    public void OnPlayerDischarged();
    
    // Memory Methods
    public void RecordEventChoice(string eventId, string choiceId, int impactScore, Dictionary<string, float> effects);
    public void RecordBattlePerformance(Battle battle, bool victory, int playerContribution);
    public List<MemoryEntry> GetRecentMemories(int count = 5);
    
    // Reaction Methods
    public LanceLeaderReaction GetReaction(LanceLeaderContext context);
    public bool ShouldTriggerReaction(string eventCategory);
    public void TriggerReactionEvent(PersistentLanceLeader leader, MemoryEntry recentMemory);
    
    // Death Methods
    public void OnLanceLeaderDeath(PersistentLanceLeader leader, string causeOfDeath);
    public void GenerateReplacement(PersistentLanceLeader deceased);
    
    // Utility Methods
    public string GetLeaderName(bool includeRank = true, bool includeEpithet = true);
    public string GetLeaderDialogueTone();
    public int GetRelationshipLevel(); // -100 to +100
    public int GetTrustLevel(); // 0 to 100
}
```

---

## Configuration

### Config File: `personality_templates.json`

```json
{
  "schemaVersion": 1,
  "personality_traits": {
    "stern": {
      "id": "stern",
      "name": "Stern",
      "description": "Strict disciplinarian who demands perfection",
      "strictness_range": [70, 95],
      "pragmatism_range": [40, 60],
      "loyalty_range": [60, 80],
      "compatible_secondary": ["honorable", "brutal", "wise", "cautious"],
      "dialogue_style": "formal_harsh",
      "reaction_modifiers": {
        "corruption_tolerance": -30,
        "failure_tolerance": -20,
        "discipline_importance": 40
      }
    },
    "fair": {
      "id": "fair",
      "name": "Fair",
      "description": "Balanced leader who rewards merit and punishes fairly",
      "strictness_range": [50, 70],
      "pragmatism_range": [50, 70],
      "loyalty_range": [60, 80],
      "compatible_secondary": ["honorable", "wise", "inspiring", "protective"],
      "dialogue_style": "balanced_professional",
      "reaction_modifiers": {
        "corruption_tolerance": -10,
        "failure_tolerance": 0,
        "discipline_importance": 20
      }
    },
    "pragmatic": {
      "id": "pragmatic",
      "name": "Pragmatic",
      "description": "Results-focused leader who cares about outcomes over methods",
      "strictness_range": [30, 50],
      "pragmatism_range": [75, 95],
      "loyalty_range": [40, 60],
      "compatible_secondary": ["cunning", "ambitious", "cautious"],
      "dialogue_style": "businesslike_direct",
      "reaction_modifiers": {
        "corruption_tolerance": 20,
        "failure_tolerance": -30,
        "discipline_importance": -10
      }
    },
    "fatherly": {
      "id": "fatherly",
      "name": "Fatherly",
      "description": "Protective mentor who teaches rather than punishes",
      "strictness_range": [30, 50],
      "pragmatism_range": [55, 75],
      "loyalty_range": [70, 90],
      "compatible_secondary": ["protective", "wise", "inspiring"],
      "dialogue_style": "warm_conversational",
      "reaction_modifiers": {
        "corruption_tolerance": -15,
        "failure_tolerance": 30,
        "discipline_importance": 10
      }
    }
  },
  
  "generation_weights": {
    "stern": 0.20,
    "fair": 0.25,
    "pragmatic": 0.20,
    "fatherly": 0.15,
    "ambitious": 0.10,
    "cynical": 0.10
  },
  
  "female_leader_chance_by_culture": {
    "empire": 0.15,
    "vlandia": 0.10,
    "sturgia": 0.25,
    "battania": 0.30,
    "khuzait": 0.20,
    "aserai": 0.05
  },
  
  "memory_settings": {
    "max_memories": 15,
    "decay_days": 30,
    "significant_event_threshold": 5,
    "reaction_cooldown_days": 3
  }
}
```

---

## Future Enhancements

### Potential Expansions

1. **Lance Leader Conversations**
   - Dedicated conversation menu
   - Ask for advice
   - Hear war stories
   - Get hints about lord's plans

2. **Lance Leader Progression**
   - Leaders can be promoted away
   - Player can become lance leader themselves
   - Maintain relationship even after leader promoted

3. **Multiple Lance Leaders**
   - If player switches between multiple lords frequently
   - Each lord remembers your history
   - Reputation spreads between leaders

4. **Lance Leader Rivalries**
   - Leaders of different lances compete
   - Player caught in politics
   - Can choose sides

5. **Legacy System**
   - Deceased leaders remembered
   - Monuments/memorials
   - "In memory of..." events

6. **Family Connections**
   - Learn about leader's family
   - Meet their relatives
   - Family crisis events

---

## Success Metrics

### Player Experience Goals

- ✅ Lance leader feels like a **real person**
- ✅ Building relationship **matters**
- ✅ Leader **remembers** player actions
- ✅ Reactions feel **appropriate** to situation
- ✅ Death has **emotional weight**
- ✅ Replacement creates **new dynamics**

### Technical Goals

- ✅ **Zero performance impact** (lightweight data structures)
- ✅ **Reliable persistence** (no lost data on save/load)
- ✅ **Scalable** (handles 50+ lords without issue)
- ✅ **Maintainable** (clean code, good logging)

---

## Conclusion

The Persistent Lance Leaders system transforms your direct superior from a generic placeholder into a **living, breathing character** who:

- Has a **unique identity** (name, personality, background)
- **Remembers your choices** and reacts accordingly
- **Builds a relationship** with you over time
- Can **die and be replaced**, creating dramatic moments
- **Integrates seamlessly** with existing systems

This creates a much richer military simulation where **your reputation and relationships matter**. Every choice you make is witnessed and remembered by the one person who matters most in your daily military life: your lance leader.

---

**Document Maintained By:** Enlisted Development Team  
**Questions?** See `docs/CONTRIBUTING.md`  
**Related Docs:**
- `DUTY_EVENTS_CREATION_GUIDE.md`
- `lance-life-simulation.md`
- `ai-camp-schedule.md`

---

**End of Document**
