# Lance Career System — Enlisted Module

This document specifies the lance structure, career progression, NPC generation, and persistence for the Enlisted mod. This system is **only active while enlisted** — when you're not serving, none of this exists.

---

## Table of Contents

1. [Overview](#overview)
2. [Lance Structure](#lance-structure)
3. [Culture Ranks](#culture-ranks)
4. [Player Progression](#player-progression)
5. [Lance Generation](#lance-generation)
6. [NPC Generation](#npc-generation)
7. [Relationships](#relationships)
8. [Casualties and Replacement](#casualties-and-replacement)
9. [Persistence](#persistence)
10. [Events Integration](#events-integration)
11. [Placeholders](#placeholders)
12. [Implementation Reference](#implementation-reference)

---

## Overview

### The Core Journey

```
ENLIST with Lord
    ↓
Assigned to a lance as lowest rank (Tier 1)
    ↓
Rise through the ranks (Tier 1 → 6)
    ↓
Eventually lead the lance (Tier 6)
    ↓
DISCHARGE / DESERT / TERM ENDS
    ↓
Lance continues without you (persists with lord)
```

### Key Principles

1. **Enlisted-only** — This system only runs while player is enlisted. Not enlisted = no lance.

2. **Per-lord persistence** — Each lord's lances are generated once per campaign and persist.

3. **Culture-appropriate** — Ranks, names, and flavor match the lord's culture.

4. **10-person lance** — Fixed structure with defined hierarchy.

5. **You start at the bottom** — Every enlistment begins as a recruit, rising through merit.

---

## Lance Structure

### The 10-Person Lance

Every lance has exactly 10 soldiers in a fixed hierarchy:

```
LANCE (10 soldiers)
│
├── [Slot 1] LANCE LEADER
│       Position: Commander of the lance
│       Culture Rank: Sergeant / Decanus / Arban-u Darga / etc.
│
├── [Slot 2] SECOND-IN-COMMAND
│       Position: Deputy, handles discipline and logistics
│       Culture Rank: Corporal / Tesserarius / etc.
│
├── [Slots 3-4] VETERANS (2 soldiers)
│       Position: Experienced fighters, mentors
│       Culture Rank: Senior Soldier titles
│
├── [Slots 5-8] SOLDIERS (4 soldiers)
│       Position: Core of the lance
│       Culture Rank: Standard soldier titles
│
└── [Slots 9-10] RECRUITS (2 soldiers)
        Position: Newest members, proving themselves
        Culture Rank: Recruit / Tiro / etc.
```

### Slot Mapping to Player Tier

| Player Tier | Slot Occupied | Position |
|-------------|---------------|----------|
| 1 | 9 or 10 | Recruit |
| 2 | 5–8 | Soldier |
| 3 | 4 | Veteran |
| 4 | 3 | Senior Veteran |
| 5 | 2 | Second-in-Command |
| 6 | 1 | Lance Leader |

---

## Culture Ranks

Each culture has appropriate rank titles for each position. These should pull from existing `progression_config.json` tier names where possible.

### Empire (Roman/Byzantine)

| Position | Rank Title | Latin Feel |
|----------|------------|------------|
| Lance Leader | Decanus | Commander of 10 |
| Second-in-Command | Tesserarius | Watch commander |
| Senior Veteran | Veteranus Primus | First veteran |
| Veteran | Veteranus | Veteran |
| Soldier | Miles | Soldier |
| Senior Soldier | Miles Gregarius | Line soldier |
| Recruit | Tiro | Recruit |

**Lance Name:** "Contubernium" (tent-group) or legion-style names

### Vlandia (Feudal Western)

| Position | Rank Title | Feudal Feel |
|----------|------------|-------------|
| Lance Leader | Sergeant | Man-at-arms leader |
| Second-in-Command | Corporal | Second |
| Senior Veteran | Senior Man-at-Arms | Experienced fighter |
| Veteran | Man-at-Arms | Professional soldier |
| Soldier | Soldier | Common soldier |
| Recruit | Recruit | New blood |

**Lance Name:** "Lance" or heraldic names

### Sturgia (Norse/Slavic)

| Position | Rank Title | Norse Feel |
|----------|------------|------------|
| Lance Leader | Húskarl-Leader | Household warrior chief |
| Second-in-Command | Second Húskarl | Deputy |
| Senior Veteran | Veteran Drengr | Proven warrior |
| Veteran | Drengr | Young warrior |
| Soldier | Karl | Freeman warrior |
| Recruit | Thrall-Born / Unproven | New warrior |

**Lance Name:** "Shield-Band" or animal/nature names

### Battania (Celtic)

| Position | Rank Title | Celtic Feel |
|----------|------------|-------------|
| Lance Leader | Cennaire | Head of ten |
| Second-in-Command | Tánaise | Heir/Second |
| Senior Veteran | Laoch Mór | Great warrior |
| Veteran | Laoch | Warrior |
| Soldier | Fénnid | Band member |
| Recruit | Dalta | Foster-son/Learner |

**Lance Name:** "Fianna" (war-band) or nature names

### Khuzait (Mongol/Steppe)

| Position | Rank Title | Mongol Feel |
|----------|------------|-------------|
| Lance Leader | Arban-u Darga | Commander of 10 |
| Second-in-Command | Baghatur | Hero/Champion |
| Senior Veteran | Akh Duu | Elder brother |
| Veteran | Duu | Younger brother |
| Soldier | Cherbi | Guard |
| Recruit | Koke | Blue (young) |

**Lance Name:** "Arban" (unit of 10) or sky/horse names

### Aserai (Arabian/Desert)

| Position | Rank Title | Arabic Feel |
|----------|------------|-------------|
| Lance Leader | Arif | Corporal (leader of 10) |
| Second-in-Command | Naqib | Deputy |
| Senior Veteran | Muqatil Kabir | Senior fighter |
| Veteran | Muqatil | Fighter |
| Soldier | Jundi | Soldier |
| Recruit | Mubdi | Beginner |

**Lance Name:** "Firqa" (squad) or desert/honor names

### Universal Rank Mapping

For code purposes, map to universal positions:

```csharp
public enum LancePosition
{
    Leader,         // Slot 1 — Sergeant/Decanus/Arban-u Darga
    Second,         // Slot 2 — Corporal/Tesserarius/Baghatur  
    SeniorVeteran,  // Slot 3
    Veteran,        // Slot 4
    Soldier,        // Slots 5-8
    Recruit         // Slots 9-10
}
```

### Rank Title Resolution

```csharp
public string GetRankTitle(string cultureId, LancePosition position)
{
    var rankTable = GetCultureRanks(cultureId);
    return rankTable[position];
}

// Example usage:
// GetRankTitle("empire", LancePosition.Leader) → "Decanus"
// GetRankTitle("vlandia", LancePosition.Leader) → "Sergeant"
// GetRankTitle("khuzait", LancePosition.Second) → "Baghatur"
```

---

## Player Progression

### Tier to Position Mapping

| Tier | Position | What Changes |
|------|----------|--------------|
| 1 | Recruit | You're the new blood. Everyone above you. |
| 2 | Soldier | Part of the core. Recruits below you. |
| 3 | Veteran | Respected. Asked your opinion. |
| 4 | Senior Veteran | Lead small tasks. Mentor others. |
| 5 | Second-in-Command | You're the Corporal/Tesserarius. Handle discipline. |
| 6 | Lance Leader | You ARE the Sergeant/Decanus. You lead the lance. |

### Promotion Events

When player reaches a new tier, a promotion event fires:

**Tier 1 → 2 (Recruit → Soldier):**
```
{LANCE_LEADER_RANK} {LANCE_LEADER_NAME} nods at you after muster. 
"You're past your proving days, {PLAYER_NAME}. Welcome to {LANCE_NAME}. 
Properly, this time."
```

**Tier 4 → 5 (Senior Veteran → Second-in-Command):**
```
{LANCE_LEADER_RANK} {LANCE_LEADER_NAME} pulls you aside.

"{SECOND_NAME} is transferring to the Third Company. I need a new 
{SECOND_RANK}. You've earned it."

Options:
• "I'm ready, {LANCE_LEADER_RANK}." → Accept
• "I'd be honored." → Accept with bonus
• "Not yet." → Decline, stay Senior Veteran
```

**Tier 5 → 6 (Second → Lance Leader):**
```
The captain summons you.

"{LANCE_LEADER_NAME} is retiring. Twenty years is enough for any 
soldier. {LANCE_NAME} needs a new {LANCE_LEADER_RANK}."

He looks at you. "That's you, {PLAYER_NAME}. Don't disappoint me."

→ You become Lance Leader
→ {LANCE_LEADER_NAME} leaves the lance
→ You now command 9 soldiers
```

### What Changes at Each Rank

| Rank | New Events | New Options | New Responsibilities |
|------|------------|-------------|---------------------|
| Recruit | Hazing, worst duties | Limited | None |
| Soldier | Standard events | Standard | None |
| Veteran | Mentor events | Advise options | Guide recruits |
| Senior Veteran | Leadership preview | "As senior man..." | Small tasks |
| Second | Discipline events | Assign duties | Handle problems |
| Leader | Command events | All leadership | Entire lance |

---

## Lance Generation

### When Lances Are Generated

```
Player enlists with Lord X
    ↓
Check: Does Lord X have lance data?
    ↓
NO → Generate lances for this lord
     (One per formation type: infantry, cavalry, archer, etc.)
     Store in campaign save
    ↓
YES → Load existing lance data
    ↓
Assign player to appropriate lance based on formation choice
```

### Generation Seed

Each lord's lances are deterministic per campaign:

```csharp
int GetLanceSeed(Hero lord)
{
    // Same lord + same campaign = same names
    // Same lord + different campaign = different names
    return lord.StringId.GetHashCode() ^ Campaign.Current.UniqueGameId.GetHashCode();
}
```

### Lance Name Generation

```csharp
public string GenerateLanceName(string cultureId, string roleHint, int seed)
{
    var rng = new Random(seed);
    var templates = GetCultureNameTemplates(cultureId);
    
    // Pick a pattern for this culture
    var pattern = Pick(templates.Patterns, rng);
    
    // Fill in the pattern
    return pattern
        .Replace("{Adjective}", Pick(templates.Adjectives, rng))
        .Replace("{Noun}", Pick(templates.Nouns, rng))
        .Replace("{Ordinal}", Pick(templates.Ordinals, rng))
        .Replace("{Animal}", Pick(templates.Animals, rng))
        .Replace("{Place}", Pick(templates.Places, rng));
}
```

### Culture Name Templates

**Empire:**
```json
{
  "patterns": [
    "The {Ordinal} Cohort",
    "The {Adjective} {Noun}",
    "Emperor's {Adjective} {Noun}",
    "The {Noun} of {Place}"
  ],
  "ordinals": ["1st", "2nd", "3rd", "4th", "5th", "7th", "9th"],
  "adjectives": ["Iron", "Jade", "Golden", "Crimson", "Obsidian", "Imperial"],
  "nouns": ["Cohort", "Column", "Guard", "Sentinels", "Spears", "Eagles"],
  "places": ["Lycaron", "Charas", "Epicrotea", "Jalmarys"]
}
```

**Vlandia:**
```json
{
  "patterns": [
    "The {Adjective} {Noun}",
    "The {Adjective} {Noun}s",
    "The {Noun} of {Heraldry}"
  ],
  "adjectives": ["Black", "White", "Red", "Broken", "Gilded", "Iron", "Scarlet"],
  "nouns": ["Lance", "Pennant", "Chevron", "Gauntlet", "Spur", "Banner", "Shield"],
  "heraldry": ["the Crossed Swords", "the Iron Gate", "the Golden Field"]
}
```

**Sturgia:**
```json
{
  "patterns": [
    "The {Animal}-{Suffix}",
    "The {Adjective} {Noun}s",
    "{Animal}'s {Noun}"
  ],
  "animals": ["Wolf", "Bear", "Raven", "Stag", "Boar", "Elk"],
  "suffixes": ["Skins", "Biters", "Feeders", "Runners", "Bloods"],
  "adjectives": ["Frost", "Storm", "Blood", "Iron", "Black", "Winter"],
  "nouns": ["Axes", "Shields", "Spears", "Claws", "Hunters"]
}
```

**Battania:**
```json
{
  "patterns": [
    "The {Nature} {Noun}s",
    "{Animal} {Noun}s",
    "Children of the {Nature}"
  ],
  "nature": ["Oak", "Ash", "Thorn", "Mist", "Stone", "Moon", "Storm"],
  "animals": ["Stag", "Wolf", "Raven", "Hound", "Boar", "Hawk"],
  "nouns": ["Spears", "Shields", "Runners", "Hunters", "Wardens", "Blades"]
}
```

**Khuzait:**
```json
{
  "patterns": [
    "The {Adjective} {Noun}",
    "{Sky} {Noun}s",
    "The {Noun} of the {Nature}"
  ],
  "adjectives": ["Lightning", "Swift", "Silent", "Endless", "Golden"],
  "sky": ["Wind", "Storm", "Cloud", "Thunder", "Sky"],
  "nouns": ["Riders", "Hooves", "Arrows", "Lancers", "Wolves", "Hawks"],
  "nature": ["Steppe", "Plains", "East Wind", "Open Sky"]
}
```

**Aserai:**
```json
{
  "patterns": [
    "The {Adjective} {Noun}s",
    "{Noun}s of the {Element}",
    "The {Possessive} {Noun}"
  ],
  "adjectives": ["Golden", "Silver", "Desert", "Burning", "Silent", "Swift"],
  "nouns": ["Crescents", "Blades", "Falcons", "Shadows", "Spears"],
  "possessives": ["Sultan's", "Emir's"],
  "elements": ["Sun", "Sand", "Flame", "Stars"]
}
```

---

## NPC Generation

### Name Pools by Culture

**Empire:**
```json
{
  "male_first": ["Marcus", "Lucius", "Gaius", "Tiberius", "Quintus", "Servius", "Decimus", "Publius", "Aulus", "Gnaeus", "Sextus", "Caius"],
  "female_first": ["Livia", "Julia", "Claudia", "Flavia", "Valeria", "Cassia", "Lucia", "Tertia", "Serena", "Caia", "Aquila", "Prisca"],
  "cognomen": ["Avitus", "Crassus", "Felix", "Longinus", "Maximus", "Nerva", "Priscus", "Rufus", "Severus", "Varro", "Crax", "Falco"],
  "epithets": ["the Wall", "Iron-Arm", "the Grim", "the Old", "One-Eye", "the Younger", "Twice-Wounded"]
}
```

**Vlandia:**
```json
{
  "male_first": ["Aldric", "Baldwin", "Conrad", "Edmund", "Geoffrey", "Hugh", "Lambert", "Morcar", "Osric", "Raymond", "Roland", "William", "Robert", "Richard"],
  "female_first": ["Adela", "Beatrice", "Eleanor", "Giselle", "Helena", "Ingrid", "Mathilde", "Rosamund", "Sybil", "Margaret", "Alice"],
  "surnames": ["of Galend", "of Pravend", "of Sargot", "of Jaculan"],
  "epithets": ["the Red", "the Black", "the Younger", "One-Eye", "Ironfist", "the Lame", "the Bold", "the Quiet"]
}
```

**Sturgia:**
```json
{
  "male_first": ["Arik", "Boris", "Dimitri", "Fyodor", "Grigori", "Ivan", "Kaspar", "Mikhail", "Oleg", "Sigurd", "Valdur", "Yaroslav", "Erik", "Ragnar"],
  "female_first": ["Anja", "Darya", "Helga", "Irina", "Katya", "Ludmila", "Nadia", "Olga", "Svana", "Yrsa", "Freya", "Astrid"],
  "patronymic_male": ["Borisovich", "Ivanovich", "Olegsyn", "Valdurson"],
  "epithets": ["Bear-Hand", "Frost-Born", "the Grim", "Storm-Eye", "Wolf-Slayer", "the Silent", "Blood-Axe"]
}
```

**Battania:**
```json
{
  "male_first": ["Aeron", "Brennus", "Cadan", "Drest", "Erwan", "Findan", "Gawain", "Luern", "Morcant", "Nevin", "Talwyn", "Cael", "Brynn"],
  "female_first": ["Aeryn", "Brigid", "Caela", "Deirdre", "Efa", "Fiona", "Gwyneth", "Moira", "Niamh", "Seren", "Rhiannon"],
  "clan_prefix": ["ap", "fen"],
  "epithets": ["of the Oak", "Wolfborn", "Storm-Singer", "the Painted", "Thornhand", "Stag-Runner", "the Wild"]
}
```

**Khuzait:**
```json
{
  "male_first": ["Arban", "Batu", "Chagatai", "Dayan", "Erden", "Gerel", "Huchar", "Jochi", "Mongke", "Nokhor", "Sübedei", "Temur", "Borte"],
  "female_first": ["Altani", "Borte", "Erdene", "Gerel", "Khulan", "Mandukhai", "Naran", "Oyuun", "Saran", "Yesui"],
  "epithets": ["the Swift", "Horse-Master", "Arrow-Eye", "Wind-Rider", "the Fearless", "Storm-Born", "Eagle-Eye"]
}
```

**Aserai:**
```json
{
  "male_first": ["Adnan", "Bahir", "Farid", "Hassan", "Ibrahim", "Jalal", "Karim", "Malik", "Nasir", "Omar", "Rashid", "Salim", "Tariq", "Yasir", "Zahir"],
  "female_first": ["Amira", "Dalila", "Fatima", "Jamila", "Layla", "Nadia", "Rashida", "Samira", "Yasmina", "Zahra"],
  "ibn": ["ibn Adnan", "ibn Hassan", "ibn Omar", "ibn Rashid"],
  "epithets": ["al-Sayf", "the Lion", "Sun-Blessed", "Sand-Walker", "the Hawk", "the Patient"]
}
```

### NPC Generation by Position

**Lance Leader (Slot 1):**
```csharp
LanceMember GenerateLanceLeader(string cultureId, int seed)
{
    var rng = new Random(seed);
    var names = GetNamePool(cultureId);
    var ranks = GetCultureRanks(cultureId);
    
    string firstName = Pick(names.MaleFirst, rng); // 80% male for leaders historically
    string epithet = Pick(names.Epithets, rng);
    
    return new LanceMember
    {
        Id = $"leader_{seed}",
        Position = LancePosition.Leader,
        RankTitle = ranks[LancePosition.Leader],  // "Decanus", "Sergeant", etc.
        FullName = $"{ranks[LancePosition.Leader]} {firstName} {epithet}",  // "Decanus Marcus the Wall"
        ShortName = firstName,  // "Marcus"
        
        // Personality
        Traits = PickTraits(LeaderTraitPool, 2, rng),
        BackstoryHook = PickBackstory(LeaderBackstoryPool, rng),
        
        // State
        IsAlive = true,
        RelationshipWithPlayer = 0
    };
}

var LeaderTraitPool = [
    "gruff but fair",
    "hard as nails", 
    "quiet professional",
    "old soldier",
    "by-the-book",
    "pragmatic survivor",
    "father figure",
    "bitter veteran"
];

var LeaderBackstoryPool = [
    "Twenty years in. Turned down officer twice.",
    "Used to command fifty. Prefers ten.",
    "Broke a noble's son's arm once. Still here somehow.",
    "Doesn't talk about Pendraic.",
    "They say he killed a champion in single combat.",
    "His whole original lance died. He didn't."
];
```

**Second-in-Command (Slot 2):**
```csharp
LanceMember GenerateSecond(string cultureId, int seed)
{
    var rng = new Random(seed);
    var names = GetNamePool(cultureId);
    var ranks = GetCultureRanks(cultureId);
    
    bool isFemale = rng.NextDouble() < 0.3;  // 30% female
    string firstName = isFemale 
        ? Pick(names.FemaleFirst, rng) 
        : Pick(names.MaleFirst, rng);
    
    return new LanceMember
    {
        Id = $"second_{seed}",
        Position = LancePosition.Second,
        RankTitle = ranks[LancePosition.Second],  // "Tesserarius", "Corporal", etc.
        FullName = $"{ranks[LancePosition.Second]} {firstName}",  // "Corporal Lucia"
        ShortName = firstName,
        
        Traits = PickTraits(SecondTraitPool, 2, rng),
        BackstoryHook = PickBackstory(SecondBackstoryPool, rng),
        
        IsAlive = true,
        RelationshipWithPlayer = 0
    };
}

var SecondTraitPool = [
    "ambitious",
    "loyal to the leader",
    "by-the-book disciplinarian",
    "practical",
    "resentful",
    "competent but cold"
];

var SecondBackstoryPool = [
    "Waiting for the old man to retire.",
    "Passed over twice. Won't happen again.",
    "The leader's right hand. Completely loyal.",
    "Handles the logistics. Hates it.",
    "Used to lead their own lance. Lost it."
];
```

**Veterans (Slots 3-4):**
```csharp
LanceMember GenerateVeteran(string cultureId, int seed, int index)
{
    var rng = new Random(seed);
    var names = GetNamePool(cultureId);
    
    bool isFemale = rng.NextDouble() < 0.25;
    string firstName = isFemale 
        ? Pick(names.FemaleFirst, rng) 
        : Pick(names.MaleFirst, rng);
    
    // Veterans often have epithets
    bool hasEpithet = rng.NextDouble() < 0.6;
    string fullName = hasEpithet 
        ? $"{firstName} {Pick(names.Epithets, rng)}"  // "Borcha One-Eye"
        : firstName;
    
    return new LanceMember
    {
        Id = $"veteran_{seed}_{index}",
        Position = LancePosition.Veteran,
        RankTitle = null,  // Veterans don't have formal rank titles
        FullName = fullName,
        ShortName = firstName,
        
        Traits = PickTraits(VeteranTraitPool, 1, rng),  // Just one defining trait
        BackstoryHook = PickBackstory(VeteranBackstoryPool, rng),
        
        IsAlive = true,
        RelationshipWithPlayer = 0
    };
}

var VeteranTraitPool = [
    "cynical survivor",
    "mentor figure",
    "quiet and deadly",
    "joker",
    "religious",
    "functional drunk",
    "haunted"
];
```

**Soldiers (Slots 5-8):**
```csharp
LanceMember GenerateSoldier(string cultureId, int seed, int index)
{
    var rng = new Random(seed);
    var names = GetNamePool(cultureId);
    
    bool isFemale = rng.NextDouble() < 0.2;
    string firstName = isFemale 
        ? Pick(names.FemaleFirst, rng) 
        : Pick(names.MaleFirst, rng);
    
    return new LanceMember
    {
        Id = $"soldier_{seed}_{index}",
        Position = LancePosition.Soldier,
        FullName = firstName,  // Just "Gaius"
        ShortName = firstName,
        
        // Soldiers have no deep personality — just names
        Traits = null,
        BackstoryHook = null,
        
        IsAlive = true,
        RelationshipWithPlayer = 0
    };
}
```

**Recruits (Slots 9-10):**
```csharp
LanceMember GenerateRecruit(string cultureId, int seed, int index)
{
    var rng = new Random(seed);
    
    // Recruits often don't have full names yet — nicknames
    bool hasRealName = rng.NextDouble() < 0.5;
    
    if (hasRealName)
    {
        var names = GetNamePool(cultureId);
        string firstName = Pick(names.MaleFirst, rng);  // 90% male recruits
        return new LanceMember
        {
            Id = $"recruit_{seed}_{index}",
            Position = LancePosition.Recruit,
            FullName = firstName,
            ShortName = firstName,
            IsAlive = true,
            RelationshipWithPlayer = 0
        };
    }
    else
    {
        // Nickname only
        string nickname = Pick(RecruitNicknames, rng);
        return new LanceMember
        {
            Id = $"recruit_{seed}_{index}",
            Position = LancePosition.Recruit,
            FullName = nickname,  // "The Baker's Son"
            ShortName = nickname,
            IsAlive = true,
            RelationshipWithPlayer = 0
        };
    }
}

var RecruitNicknames = [
    "the Baker's Son",
    "the Blacksmith's Boy",
    "the Farmer's Lad",
    "the Mute",
    "Skinny",
    "the Kid",
    "Mouse",
    "the Runaway",
    "Twitchy",
    "the Orphan"
];
```

---

## Relationships

### Relationship Score

Each named NPC has a relationship with the player:

| Score Range | Status | Description |
|-------------|--------|-------------|
| −100 to −50 | Hostile | Actively dislikes you, may sabotage |
| −49 to −20 | Unfriendly | Cold, unhelpful |
| −19 to +19 | Neutral | Professional, does their job |
| +20 to +49 | Friendly | Helpful, shares information |
| +50 to +74 | Loyal | Covers for you, follows your lead |
| +75 to +100 | Bonded | Personal loyalty, deep trust |

### Building Relationships

**Event Choices:**
```
Help {VETERAN_1_NAME} with a task      → +15 relationship
Cover for {SOLDIER_NAME}'s mistake     → +20 with soldier, −5 with leader
Share rations when supplies low        → +10 with everyone
Report {CORPORAL_NAME}'s skimming      → −30 with corporal, +10 with leader
Stand up for {RECRUIT_NAME}            → +25 with recruit
```

**Time-Based:**
```
Survive a battle together              → +5 with all
Serve together 30 days                 → +10 baseline with all
Complete a term with the lance         → +15 with all surviving
```

**Honor Modifier:**
```
High honor (25+)    → Relationship gains +25%
Low honor (−25)     → Relationship gains −25%, some NPCs won't bond
```

### Relationship Effects

| Level | Effect |
|-------|--------|
| Friendly (20+) | They warn you about shakedowns, share info |
| Friendly (35+) | They cover for you (reduce heat/discipline gains) |
| Loyal (50+) | They back you in confrontations |
| Loyal (65+) | They follow your lead in group decisions |
| Bonded (80+) | Personal events unlock, deep backstory revealed |

---

## Casualties and Replacement

### After Battle Rolls

| Battle Result | Wound Chance | Death Chance |
|---------------|--------------|--------------|
| Easy victory | 5% per soldier | 1% |
| Hard victory | 15% per soldier | 5% |
| Pyrrhic victory | 30% per soldier | 15% |
| Defeat (escaped) | 40% per soldier | 25% |

### Casualty Priority

Who gets hit (weighted):
1. **Recruits** — Most likely (inexperienced)
2. **Soldiers** — Next most likely
3. **Veterans** — Survive more often
4. **Second/Leader** — Plot armor unless dramatic

### Wound Recovery

| Severity | Recovery Time | Risk |
|----------|---------------|------|
| Light | 2–4 days | None |
| Moderate | 5–10 days | 10% becomes severe |
| Severe | 10–20 days | 15% death if untreated |

### Death Notification

```
## Event: Losses

**Trigger:** After battle with lance casualties

**Setup**
{LANCE_LEADER_RANK} {LANCE_LEADER_NAME} gathers {LANCE_NAME} after 
the fighting. The count comes up short.

Wounded:
{WOUNDED_LIST}

Killed:
{KILLED_LIST}

{LANCE_LEADER_SHORT} removes his helm. "We drink to them tonight. 
They were {LANCE_NAME}."

**Options**
| Option | Effect |
|--------|--------|
| Say their names | +5 relationship with all, +10 Leadership XP |
| Stay silent | No effect |
| "They died well." | +10 Leadership XP |
```

### Replacement

When the lance has vacancies:
- New soldiers assigned when army enters settlement
- Recruits are unnamed initially ("the new one")
- After 7 days, they get names
- Relationships start at 0

```
{LANCE_LEADER_SHORT} points to a nervous-looking youth. "Fresh meat. 
Try not to get him killed on his first day, {PLAYER_NAME}."
```

---

## Persistence

### What Saves (Per Lord)

```csharp
public class LordLanceData : ISerializable
{
    public string LordId { get; set; }
    public int GenerationSeed { get; set; }
    
    public Dictionary<string, Lance> Lances { get; set; }  // roleHint → Lance
    
    // Player history with this lord
    public bool PlayerHasServedBefore { get; set; }
    public int PlayerPreviousTier { get; set; }
    public bool PlayerLeftInGoodStanding { get; set; }
    public string PlayerPreviousLanceId { get; set; }
}
```

### What Saves (Per Lance)

```csharp
public class Lance : ISerializable
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string LordId { get; set; }
    public string RoleHint { get; set; }
    
    public List<LanceMember> Members { get; set; }
    
    // Stats
    public int BattlesSurvived { get; set; }
    public int TotalDeaths { get; set; }
    public List<string> FallenNames { get; set; }  // Honor roll
    
    // Player tracking
    public int PlayerSlotIndex { get; set; }  // -1 if player not in this lance
}
```

### What Saves (Per Member)

```csharp
public class LanceMember : ISerializable
{
    public string Id { get; set; }
    public LancePosition Position { get; set; }
    public string RankTitle { get; set; }
    public string FullName { get; set; }
    public string ShortName { get; set; }
    
    public string[] Traits { get; set; }
    public string BackstoryHook { get; set; }
    
    public bool IsPlayer { get; set; }
    public bool IsAlive { get; set; }
    public bool IsWounded { get; set; }
    public int WoundDaysRemaining { get; set; }
    
    public int RelationshipWithPlayer { get; set; }
}
```

### Rejoining the Same Lord

**Left in Good Standing:**
```
"{PLAYER_NAME}. Back for more?"

- Start at Tier 2 (recognized, not raw recruit)
- Relationships with survivors = 50% of previous
- Same lance if still exists
- Position based on current tier
```

**Left in Bad Standing (deserted):**
```
{LANCE_LEADER_SHORT} stares at you. "You've got nerve."

- Start at Tier 1 (must prove yourself again)
- Relationships start negative (−20 with all)
- Must earn trust back
```

---

## Events Integration

### Lance-Aware Placeholders

```
{LANCE_NAME}           → "The Iron Column"
{LANCE_LEADER_RANK}    → "Decanus" / "Sergeant" / etc.
{LANCE_LEADER_NAME}    → "Decanus Marcus the Wall"
{LANCE_LEADER_SHORT}   → "Marcus"
{SECOND_RANK}          → "Tesserarius" / "Corporal" / etc.
{SECOND_NAME}          → "Corporal Lucia"
{SECOND_SHORT}         → "Lucia"
{VETERAN_1_NAME}       → "Borcha One-Eye"
{VETERAN_2_NAME}       → "Ingrid"
{SOLDIER_NAME}         → Random living soldier name
{RECRUIT_NAME}         → Random recruit name
{LANCE_MATE_NAME}      → Random member (not player, not leader)
{WOUNDED_MEMBER}       → Name of wounded member (if any)
{FALLEN_MEMBER}        → Name of recently killed (if any)
```

### Position-Based Event Gating

```json
{
  "id": "assign_duty_to_recruit",
  "requires_position": ["Second", "Leader"],
  "setup": "A recruit needs assigning to a task..."
}

{
  "id": "hazing_event",
  "requires_position": ["Recruit"],
  "setup": "The veterans have something planned for you..."
}

{
  "id": "leadership_dilemma",
  "requires_position": ["Leader"],
  "setup": "As {LANCE_LEADER_RANK}, the decision falls to you..."
}
```

### Example Event with Lance Context

```
## Event: Dawn Muster

**Time:** Dawn
**Position:** Any

**Setup**
{LANCE_LEADER_RANK} {LANCE_LEADER_SHORT} counts heads in the grey 
light. "{LANCE_NAME}, all present?"

{SECOND_SHORT} nods. "All here, {LANCE_LEADER_RANK}. Though 
{LANCE_MATE_NAME} looks like death warmed over."

{LANCE_LEADER_SHORT} grunts. "They'll live." His eyes find you. 
"{PLAYER_NAME}. Water detail. Move."

**Options**
| Option | Risk | Effect |
|--------|------|--------|
| "Yes, {LANCE_LEADER_RANK}." | Safe | +5 relationship with leader |
| Get moving without a word | Safe | No effect |
| "Again? I did it yesterday." | Risky | −10 with leader, might get worse duty |
| [Veteran+] "Put the new blood on it." | Safe | Recruit does it, +5 with leader |
```

---

## Implementation Reference

### Data Structures Summary

```csharp
// Position enum
public enum LancePosition
{
    Leader,         // Slot 1
    Second,         // Slot 2
    SeniorVeteran,  // Slot 3
    Veteran,        // Slot 4
    Soldier,        // Slots 5-8
    Recruit         // Slots 9-10
}

// Culture rank lookup
public interface ICultureRanks
{
    string GetRankTitle(string cultureId, LancePosition position);
    string GetLanceTerminology(string cultureId);  // "Lance", "Contubernium", "Arban"
}

// Name generation
public interface INameGenerator
{
    string GenerateLanceName(string cultureId, string roleHint, int seed);
    LanceMember GenerateMember(string cultureId, LancePosition position, int seed);
}

// Lance management
public interface ILanceManager
{
    Lance GetOrCreateLance(Hero lord, string roleHint);
    void AssignPlayerToLance(Lance lance, int tier);
    void ProcessBattleCasualties(Lance lance, BattleResult result);
    void PromotePlayer(Lance lance, int newTier);
    void HandleMemberDeath(Lance lance, LanceMember member);
    void ReplaceMember(Lance lance, int slotIndex);
}
```

### Config File Reference

```json
// lance_career_config.json
{
  "culture_ranks": {
    "empire": {
      "leader": "Decanus",
      "second": "Tesserarius",
      "veteran": "Veteranus",
      "soldier": "Miles",
      "recruit": "Tiro",
      "lance_term": "Contubernium"
    },
    "vlandia": {
      "leader": "Sergeant",
      "second": "Corporal",
      "veteran": "Man-at-Arms",
      "soldier": "Soldier",
      "recruit": "Recruit",
      "lance_term": "Lance"
    }
    // ... other cultures
  },
  
  "name_templates": {
    // ... as defined above
  },
  
  "name_pools": {
    // ... as defined above
  },
  
  "casualty_rates": {
    "easy_victory": { "wound": 0.05, "death": 0.01 },
    "hard_victory": { "wound": 0.15, "death": 0.05 },
    "pyrrhic": { "wound": 0.30, "death": 0.15 },
    "defeat": { "wound": 0.40, "death": 0.25 }
  },
  
  "rejoin_rules": {
    "good_standing_start_tier": 2,
    "good_standing_relationship_retention": 0.5,
    "bad_standing_start_tier": 1,
    "bad_standing_relationship_start": -20
  }
}
```

---

*Document version: 1.0*
*Module: Enlisted*
*Companion to: Lance Life Events Master Documentation v2*
