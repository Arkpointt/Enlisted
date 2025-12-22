# Traits & Identity System — Implementation Study

**Created**: December 20, 2025  
**Status**: ✅ COMPLETE - Code implemented, systems deleted  
**Purpose**: Document how to replace Lance/Formation systems with native trait integration for emergent identity

> **Note**: This document describes what was ALREADY IMPLEMENTED. The Identity system (`EnlistedStatusManager`, `TraitHelper`) is live. The Lance and Formation systems have been deleted. See `StoryBlocks/storyblocks-v2-implementation.md` for event conversion guidance.

---

## Table of Contents

1. [Overview](#overview)
2. [Current System Analysis](#current-system-analysis)
3. [Native Trait System Study](#native-trait-system-study)
4. [Proposed Architecture](#proposed-architecture)
5. [Identity Formation Model](#identity-formation-model)
6. [Event Integration](#event-integration)
7. [Implementation Roadmap](#implementation-roadmap)
8. [Migration Plan](#migration-plan)
9. [Technical Reference](#technical-reference)

---

## Overview

### Problem Statement

Current identity systems (Lances and Formations) add complexity without meaningful player value:
- **Lance System**: ~3000 lines of code for sub-unit identity that players rarely notice
- **Formation System**: ~1500 lines for manual role selection that creates early-game lock-in
- **Result**: Complex systems, shallow identity, limited replayability

### Solution Vision

**Replace system-based identity with choice-based identity** using native Bannerlord traits:
- Player identity emerges from event choices, not menu selections
- Native personality traits (Mercy, Valor, Honor, etc.) track moral/behavioral patterns
- Custom reputation tracks (Lord, Officers, Soldiers) handle social standing
- Rank progression (T1-T9) remains as experience/access gating
- High replayability through trait combinations affecting available content

### Key Insight

Bannerlord already has a robust trait system that tracks personality and skills. We don't need to build our own - just integrate with what exists and let player choices drive trait changes.

---

## Current System Analysis

### Systems to Delete

#### 1. Lance System (~3000 lines)
**Files to Remove:**
```
src/Features/Lances/
├── Behaviors/
│   ├── EnlistedLanceMenuBehavior.cs
│   └── LanceStoryBehavior.cs
├── Leaders/
│   ├── PersistentLanceLeadersBehavior.cs
│   └── PersistentLanceLeaderModels.cs
├── Simulation/
│   ├── LanceLifeSimulationBehavior.cs
│   └── LanceMemberState.cs
├── Core/
│   └── LanceRegistry.cs
└── Config/
    └── lances_config.json

docs/Features/Core/lance-assignments.md
docs/Features/Gameplay/lance-life.md
```

**What This Removes:**
- Lance selection at T2 (inquiry menu)
- Lance roster generation (8-12 NPC members)
- Lance reputation track (-50 to +50)
- Lance member simulation (injuries, deaths)
- Lance-based event filtering
- Named NPC system for lances
- Cultural lance styles (Legion, Feudal, Tribal, etc.)

**Why Delete:**
- Players don't care which lance they're in
- Identity is just a label, not gameplay
- Content filtering can use traits instead
- NPC simulation adds complexity without payoff

#### 2. Formation System (~1500 lines)
**Files to Modify/Remove:**
```
src/Features/Assignments/
├── Behaviors/EnlistedDutiesBehavior.cs (PlayerFormation property)
└── Core/ConfigurationManager.cs (formation configs)

ModuleData/Enlisted/Events/events_promotion.json
└── promotion_t1_t2_finding_your_place (formation choice event)

docs/Features/Core/duties-system.md (being replaced anyway)
```

**What This Removes:**
- Formation choice at T2 proving event
- Manual formation selection (Infantry/Cavalry/Archer/Horse Archer)
- Formation-based activity weighting
- Formation-based event filtering
- Formation-locked progression

**Why Delete:**
- Creates early-game lock-in
- Players may want to change role as they progress
- Equipment access gates by rank anyway
- Natural playstyle detection possible through equipment checks (optional)

### Systems to Keep

#### 1. Rank Progression (T1-T9) ✅
**Keep As-Is:**
```
src/Features/Ranks/Behaviors/PromotionBehavior.cs
docs/Features/Core/lance-assignments.md (rank table section)
```

**Why Keep:**
- Clear progression milestone
- Natural experience gating
- Culture-specific rank names add flavor
- Equipment access tied to rank (quartermaster)
- Already implemented and working

#### 2. Escalation System ✅
**Keep and Expand:**
```
src/Features/Escalation/
├── EscalationManager.cs
├── EscalationState.cs
└── EscalationThresholds.cs
```

**Current Tracks:**
- Scrutiny (0-10): Camp crime, contraband
- Discipline (0-10): Rule-breaking, insubordination
- Lance Reputation (-50 to +50): Peer standing
- Medical Risk (0-5): Injury/illness severity

**Rename/Repurpose:**
- Lance Reputation → **Soldier Reputation** (peer respect)

**Add New Tracks:**
- **Lord Reputation** (-50 to +50): Trust, loyalty, competence in lord's eyes
- **Officer Reputation** (-50 to +50): Potential, promise as seen by NCOs/officers

#### 3. Schedule Activities System ✅
**Keep As-Is:**
```
src/Features/Schedule/
docs/Features/Core/schedule-activities-system.md
```

**Why Keep:**
- Creates daily variety (Patrol, Sentry, Work Detail, etc.)
- Context-aware assignment (siege increases Siege Work)
- Skill-gated specialist activities
- Triggers narrative events (20% chance)
- Already designed to replace duties

#### 4. Event System ✅
**Keep and Enhance:**
```
src/Features/Lances/Events/ (rename to src/Features/Events/)
├── LanceLifeEventBehavior.cs
├── LanceLifeEventCatalog.cs
├── LanceLifeEventEffectsApplier.cs (ADD TRAIT SUPPORT HERE)
└── LanceLifeEventTriggerEvaluator.cs (UPDATE FILTERS)

ModuleData/Enlisted/Events/*.json (80+ events)
```

**Why Keep:**
- Core narrative delivery system
- Proven event execution pipeline
- Good variety of content types
- Just needs trait integration

#### 5. Player Conditions ✅
**Keep As-Is:**
```
src/Features/Conditions/
├── PlayerConditionBehavior.cs
└── PlayerConditionModels.cs
```

**Why Keep:**
- Injury/illness consequences
- Treatment requirements
- Training gating
- Adds meaningful stakes to risky choices

---

## Native Trait System Study

### API Reference (v1.3.12)

#### Trait Categories

**Personality Traits** (Visible, -2 to +2):
```csharp
DefaultTraits.Mercy        // Aversion to suffering, helping strangers/enemies
DefaultTraits.Valor        // Risking life for glory/wealth/cause
DefaultTraits.Honor        // Keeping commitments, respecting law
DefaultTraits.Generosity   // Loyalty to kin/servants, gratitude
DefaultTraits.Calculating  // Controlling emotions for long-term gain
```

**Hidden Skill Traits** (Background, 0-20):
```csharp
DefaultTraits.Commander             // Leadership capability
DefaultTraits.Surgeon               // Medical skill proficiency
DefaultTraits.SergeantCommandSkills // NCO capability
DefaultTraits.EngineerSkills        // Engineering/siege warfare
DefaultTraits.RogueSkills           // Criminal/stealth abilities
DefaultTraits.ScoutSkills           // Scouting/reconnaissance
DefaultTraits.Trader                // Trade/barter skill
DefaultTraits.Blacksmith            // Crafting skill
DefaultTraits.Thug                  // Gang enforcement specialty
DefaultTraits.Smuggler              // Contraband specialty
```

**Political Traits** (Background, 0-20):
```csharp
DefaultTraits.Egalitarian
DefaultTraits.Oligarchic
DefaultTraits.Authoritarian
```

**Special Traits**:
```csharp
DefaultTraits.NavalSoldier          // Naval combat experience
DefaultTraits.Frequency             // Internal use
```

#### Core API Methods

**Reading Traits:**
```csharp
// Get trait level
int mercyLevel = Hero.MainHero.GetTraitLevel(DefaultTraits.Mercy);
// Returns: -2 (Cruel) to +2 (Merciful) for personality traits
// Returns: 0-20 for hidden traits

// Check if trait is positive
bool isMerciful = Hero.MainHero.GetTraitLevel(DefaultTraits.Mercy) > 0;

// Get trait XP progress
int traitXp = Campaign.Current.PlayerTraitDeveloper.GetPropertyValue(
    (PropertyObject)DefaultTraits.Mercy
);
```

**Changing Traits:**
```csharp
// Method 1: Use native helper (recommended - includes logging)
TraitLevelingHelper.OnIncidentResolved(DefaultTraits.Honor, 50);

// Method 2: Direct XP change (for custom scenarios)
Campaign.Current.PlayerTraitDeveloper.AddPropertyValue(
    (PropertyObject)DefaultTraits.Mercy, 
    100
);

// Method 3: Direct level set (rare - use for debugging only)
Hero.MainHero.SetTraitLevel(DefaultTraits.Valor, 2);
```

**Event Notifications:**
```csharp
// Native fires this when trait level changes:
CampaignEventDispatcher.Instance.OnPlayerTraitChanged(trait, previousLevel);

// Shows notification banner in-game automatically
```

#### Native Trait XP Thresholds

**Personality Traits** (-2 to +2):
```
Level -2: -1000 XP
Level -1: -500 XP
Level  0: 0 XP (default)
Level +1: +500 XP
Level +2: +1000 XP
```

**Hidden Traits** (0-20):
```
Each level requires ~100 XP
Level 5: 500 XP (noticeable)
Level 10: 1000 XP (significant)
Level 15: 1500 XP (major)
Level 20: 2000 XP (mastery)
```

#### Native Trait Triggers (Examples)

Bannerlord already triggers trait changes for:

```csharp
// Battle
TraitLevelingHelper.OnBattleWon(mapEvent, contribution);
// → +5 to +20 Valor if outnumbered 9:1+

// Hostile Actions
TraitLevelingHelper.OnVillageRaided();
// → -30 Mercy

TraitLevelingHelper.OnLordExecuted();
// → -1000 Honor (massive penalty, instant level change)

// Party Management
TraitLevelingHelper.OnPartyTreatedWell();
// → +20 Generosity

TraitLevelingHelper.OnPartyStarved();
// → -20 Generosity

// Prisoner Management
TraitLevelingHelper.OnLordFreed(lord);
// → +20 Calculating

// Quests
TraitLevelingHelper.OnIssueSolvedThroughQuest(hero, trait, xp);
TraitLevelingHelper.OnIssueFailed(hero, effectedTraits);
TraitLevelingHelper.OnIssueSolvedThroughBetrayal(hero, effectedTraits);

// Siege Aftermath
TraitLevelingHelper.OnSiegeAftermathApplied(settlement, aftermathType, traits);
// → Show Mercy (+Mercy), Pillage (-Mercy), etc.

// Persuasion
TraitLevelingHelper.OnPersuasionDefection(lord);
// → +20 Calculating
```

### Integration Strategy

Use `TraitLevelingHelper.OnIncidentResolved(trait, xp)` for all enlisted events:

```csharp
// In event effects applier
private void ApplyTraitEffect(string traitId, int value)
{
    var trait = GetTraitFromId(traitId);
    if (trait != null)
    {
        TraitLevelingHelper.OnIncidentResolved(trait, value);
        ModLogger.Info("Events", $"Trait {traitId} changed by {value}");
    }
}

private TraitObject GetTraitFromId(string traitId)
{
    return traitId.ToLower() switch
    {
        "mercy" => DefaultTraits.Mercy,
        "valor" => DefaultTraits.Valor,
        "honor" => DefaultTraits.Honor,
        "generosity" => DefaultTraits.Generosity,
        "calculating" => DefaultTraits.Calculating,
        "commander" => DefaultTraits.Commander,
        "surgeon" => DefaultTraits.Surgeon,
        "sergeant" => DefaultTraits.SergeantCommandSkills,
        "engineer" => DefaultTraits.EngineerSkills,
        "rogue" => DefaultTraits.RogueSkills,
        "scout" => DefaultTraits.ScoutSkills,
        "trader" => DefaultTraits.Trader,
        "blacksmith" => DefaultTraits.Blacksmith,
        "thug" => DefaultTraits.Thug,
        "smuggler" => DefaultTraits.Smuggler,
        _ => null
    };
}
```

---

## Proposed Architecture

### Identity Components Matrix

| Component | Type | Range | Purpose | Source |
|-----------|------|-------|---------|--------|
| **Rank** | Progression | T1-T9 | Experience level, access gating | Existing |
| **Mercy** | Personality | -2 to +2 | Compassion vs. cruelty | Native trait |
| **Valor** | Personality | -2 to +2 | Courage vs. caution | Native trait |
| **Honor** | Personality | -2 to +2 | Integrity vs. pragmatism | Native trait |
| **Generosity** | Personality | -2 to +2 | Loyalty vs. selfishness | Native trait |
| **Calculating** | Personality | -2 to +2 | Strategic vs. impulsive | Native trait |
| **Commander** | Hidden Skill | 0-20 | Leadership capability | Native trait |
| **Surgeon** | Hidden Skill | 0-20 | Medical proficiency | Native trait |
| **Sergeant** | Hidden Skill | 0-20 | NCO capability | Native trait |
| **Engineer** | Hidden Skill | 0-20 | Siege/construction | Native trait |
| **Rogue** | Hidden Skill | 0-20 | Criminal/stealth | Native trait |
| **Scout** | Hidden Skill | 0-20 | Reconnaissance | Native trait |
| **Trader** | Hidden Skill | 0-20 | Commerce | Native trait |
| **Smuggler** | Hidden Skill | 0-20 | Contraband specialty | Native trait |
| **Thug** | Hidden Skill | 0-20 | Enforcement specialty | Native trait |
| **Soldier Rep** | Social | -50 to +50 | Peer respect | Escalation (renamed) |
| **Lord Rep** | Social | -50 to +50 | Trust/loyalty with lord | Escalation (new) |
| **Officer Rep** | Social | -50 to +50 | Perceived potential | Escalation (new) |
| **Scrutiny** | Escalation | 0-10 | Camp crime level | Escalation |
| **Discipline** | Escalation | 0-10 | Rule-breaking level | Escalation |
| **Medical Risk** | Condition | 0-5 | Injury/illness severity | Escalation |
| **Context** | Situational | Various | Army state flags | Existing |

### Content Routing Logic

Events filter based on combinations of:

```csharp
public class EventRequirements
{
    // Rank
    public int? TierMin { get; set; }
    public int? TierMax { get; set; }
    
    // Native Personality Traits (-2 to +2)
    public int? MercyMin { get; set; }
    public int? MercyMax { get; set; }
    public int? ValorMin { get; set; }
    public int? HonorMin { get; set; }
    public int? GenerosityMin { get; set; }
    public int? CalculatingMin { get; set; }
    
    // Native Hidden Traits (0-20)
    public int? CommanderMin { get; set; }
    public int? SurgeonMin { get; set; }
    public int? SergeantMin { get; set; }
    public int? EngineerMin { get; set; }
    public int? RogueMin { get; set; }
    public int? ScoutMin { get; set; }
    public int? SmugglerMin { get; set; }
    public int? ThugMin { get; set; }
    
    // Custom Reputation
    public int? SoldierReputationMin { get; set; }
    public int? LordReputationMin { get; set; }
    public int? OfficerReputationMin { get; set; }
    
    // Escalation
    public int? ScrutinyMin { get; set; }
    public int? DisciplineMin { get; set; }
    
    // Context
    public bool? IsAtWar { get; set; }
    public bool? IsSieging { get; set; }
    public bool? InTown { get; set; }
    public bool? BattleRecent { get; set; }
}
```

### Example Identity Profiles

**Honorable Soldier:**
```
Rank: T5 (Veteran)
Honor: +2, Valor: +1, Mercy: +1
Commander: 8, Sergeant: 5
Lord Rep: 45/100, Officer Rep: 40/100, Soldier Rep: 30/100
Scrutiny: 0, Discipline: 2

Available Content:
→ Leadership training events
→ Officer promotion track
→ Special assignments from lord
→ Mentor younger soldiers
```

**Pragmatic Rogue:**
```
Rank: T4 (Professional)
Honor: -1, Calculating: +2, Mercy: 0
Rogue: 12, Scout: 8, Smuggler: 6
Lord Rep: 20, Officer Rep: 10, Soldier Rep: 35
Scrutiny: 6, Discipline: 5

Available Content:
→ Covert operations
→ Smuggling side jobs
→ Criminal contacts
→ High-risk, high-reward missions
```

**Merciful Medic:**
```
Rank: T6 (NCO)
Mercy: +2, Generosity: +1, Honor: +1
Surgeon: 15, Commander: 5
Lord Rep: 30, Officer Rep: 35, Soldier Rep: 50
Scrutiny: 1, Discipline: 1

Available Content:
→ Medical crisis events
→ Triage decision events
→ Mentoring/teaching
→ Battlefield triage authority
```

**Calculating Commander:**
```
Rank: T7 (Commander)
Calculating: +2, Valor: 0, Honor: 0
Commander: 18, Sergeant: 15, Scout: 10
Lord Rep: 50, Officer Rep: 50, Soldier Rep: 20
Scrutiny: 3, Discipline: 8

Available Content:
→ Tactical command decisions
→ Political maneuvering
→ Disciplinary authority
→ High-stakes strategic choices
```

---

## Identity Formation Model

### Player Journey Example

#### T1: Recruit
**Starting State:**
```
Rank: T1
All Traits: 0
All Reputations: 0
Scrutiny: 0, Discipline: 0
```

**Early Events:**
```
Event: "Witness Theft"
Choice: Report thief
→ +50 Honor XP, +10 Lord Rep, -5 Soldier Rep
→ Outcome: Honest path started

Event: "Dangerous Patrol"
Choice: Volunteer to lead
→ +30 Valor XP, +10 Officer Rep, +5 Soldier Rep
→ Outcome: Brave reputation forming

Event: "Injured Comrade"
Choice: Help carry them back
→ +40 Mercy XP, +15 Soldier Rep, -5 Officer Rep (shows weakness)
→ Outcome: Compassionate but not pragmatic
```

**End of T1:**
```
Rank: T1 (ready for T2)
Honor: +1 (500 XP reached)
Valor: 0 (30/500 XP)
Mercy: 0 (40/500 XP)
Lord Rep: 10
Officer Rep: 5
Soldier Rep: 15
```

#### T2: Tested
**Promotion Event:**
```
No formation choice - just narrative scene
"You've proven yourself in battle. The sergeant acknowledges you with a nod."
→ Promotes to T2
→ Unlock: More complex event choices
```

**Character Development:**
```
Event: "Corrupt Quartermaster"
Choice: Report corruption
→ +100 Honor XP (reaches level +2), +20 Lord Rep, -10 Soldier Rep
→ Outcome: Strongly honorable path locked in
→ Unlocks: "Lord's Trusted" event chain

Event: "Battle Crisis"
Choice: Hold the line despite losses
→ +80 Valor XP (reaches level +1), +15 Officer Rep
→ Outcome: Brave but not reckless

Event: "Smuggler Offer"
Disabled: Requires Honor ≤ 0 OR Rogue ≥ 5
→ Player's Honor +2 prevents access to criminal content
```

**End of T2:**
```
Rank: T2
Honor: +2 (level up!)
Valor: +1 (level up!)
Mercy: 0
Lord Rep: 30
Officer Rep: 20
Soldier Rep: 5 (unpopular with soldiers due to reporting)
```

#### T4: Professional
**Specialist Path Emergence:**
```
Event: "Field Surgery Crisis"
Disabled: Requires Medicine 40+ OR Surgeon 5+
→ Player has Medicine 35 and Surgeon 0
→ Not qualified yet

Event: "Scout Deep Territory"
Requires: Valor +1, Scouting 50+
→ Player qualifies (Valor +1, Scouting 55)
→ Success: +150 Scout trait XP (reaches level 2)
→ Unlocks: Reconnaissance specialist content

Event: "Officer Recommendation"
Requires: Honor +1, Lord Rep 30+, Officer Rep 20+
→ Player qualifies
→ +200 Sergeant trait XP (reaches level 2)
→ +100 Commander trait XP (reaches level 1)
→ Unlocks: NCO leadership events
```

**Branching Path:**
```
Player now qualifies for TWO specialist tracks:
1. Scout Specialist (Scout 2, Valor +1, Scouting 55)
   → Reconnaissance missions
   → Stealth operations
   → Intelligence gathering
   
2. NCO Track (Sergeant 2, Commander 1, Honor +2)
   → Squad leadership
   → Training duties
   → Officer candidate path

Both available simultaneously!
```

**End of T4:**
```
Rank: T4
Honor: +2, Valor: +1, Mercy: 0
Scout: 2, Sergeant: 2, Commander: 1
Lord Rep: 45, Officer Rep: 35, Soldier Rep: 10
```

#### T7: Commander
**Multiple Paths Converge:**
```
Path 1: Officer Track
Requires: Commander 10+, Sergeant 8+, Honor +1, Officer Rep 40+
→ Formal leadership authority
→ Command decisions affect reputation
→ Disciplinary authority events

Path 2: Specialist Scout
Requires: Scout 12+, Valor +1, Scouting 80+
→ Deep reconnaissance missions
→ Elite operations
→ Intelligence network access

Path 3: Pragmatic Political
Requires: Calculating +2, Commander 8+, Lord Rep 50+
→ Strategic influence events
→ Political maneuvering
→ Lord's advisor track
```

**Example Event (Multiple Paths):**
```
Event: "Deserter in Ranks"
Available to all T7+ players, but outcomes vary:

Officer Track:
  Option: "Court-martial by the book"
  Requires: Honor +1, Sergeant 8+
  → +Discipline, +Lord Rep, -Soldier Rep
  
Scout Track:
  Option: "Use him as double agent"
  Requires: Scout 10+, Calculating +1
  → +Scout XP, +Lord Rep, +Scrutiny
  
Merciful Leader:
  Option: "Second chance with demotion"
  Requires: Mercy +1, Commander 8+
  → -Discipline, -Lord Rep, +Soldier Rep
```

### Identity Emergent Properties

**Trait Combinations Create Unique Identities:**

| Trait Combo | Identity | Content Access |
|-------------|----------|----------------|
| Honor +2, Valor +1, Commander 10+ | **Paladin** | Noble officer, mentorship, moral authority |
| Honor -2, Calculating +2, Rogue 15+ | **Spymaster** | Espionage, manipulation, covert ops |
| Mercy +2, Surgeon 15+, Generosity +1 | **Healer** | Medical authority, triage, humanitarian |
| Valor +2, Scout 12+, Calculating 0 | **Hero** | Reckless courage, glory-seeking, fame |
| Calculating +2, Commander 15+, Honor 0 | **Strategist** | Tactical brilliance, cold decisions |
| Mercy -2, Thug 10+, Valor +1 | **Enforcer** | Intimidation, violence, discipline |
| Honor +1, Trader 12+, Generosity -1 | **Quartermaster** | Supply chains, logistics, profit |

**Conflicting Traits Create Complex Characters:**
```
Example: Honor +1, Smuggler 8, Calculating +2
→ "Reluctant Smuggler": Honorable person forced into gray area by pragmatism
→ Unlocks: Moral dilemma events, redemption arcs, complex choices
```

---

## Event Integration

### Event JSON Schema Update

**New Schema (with trait support):**

```json
{
  "id": "event_example",
  "category": "duty",
  "delivery": {
    "method": "automatic",
    "channel": "inquiry"
  },
  "triggers": {
    "all": ["ai_safe"]
  },
  "requirements": {
    "tier": { "min": 3, "max": 6 },
    "traits": {
      "mercy": { "min": 1 },
      "honor": { "min": 0 },
      "commander": { "min": 5 }
    },
    "reputation": {
      "soldier": { "min": 20 },
      "lord": { "min": 30 }
    },
    "escalation": {
      "scrutiny": { "max": 5 },
      "discipline": { "max": 6 }
    },
    "context": {
      "at_war": true,
      "battle_recent": true
    }
  },
  "content": {
    "title": "Wounded Prisoners",
    "setup": "After the battle, you find enemy wounded. Your men want to finish them. The lord expects efficiency, but these men are helpless.",
    "options": [
      {
        "id": "treat_them",
        "text": "Treat their wounds. They're defeated, not animals.",
        "tooltip": "Compassionate choice. Officers may see this as wasteful.",
        "effects": {
          "traits": {
            "mercy": 80,
            "surgeon": 50
          },
          "reputation": {
            "soldier": 10,
            "officer": -5,
            "lord": -5
          },
          "escalation": {
            "discipline": -1
          }
        },
        "outcome": "You bandage the wounded enemies. Your men grumble, but some look at you with new respect. The officers shake their heads at your softness."
      },
      {
        "id": "execute_them",
        "text": "They're a liability. End it quickly.",
        "tooltip": "Pragmatic but cruel. Your reputation will suffer.",
        "effects": {
          "traits": {
            "mercy": -80,
            "calculating": 40
          },
          "reputation": {
            "soldier": -15,
            "officer": 5,
            "lord": 5
          },
          "escalation": {
            "discipline": 2
          }
        },
        "outcome": "The deed is done. Efficient, but the screams haunt you. Your men avoid your eyes. The officers nod approvingly at your cold efficiency."
      },
      {
        "id": "leave_them",
        "text": "Leave them. Not my problem.",
        "tooltip": "Neutral choice. No strong reaction either way.",
        "effects": {
          "traits": {
            "mercy": -20,
            "honor": -20
          },
          "reputation": {
            "soldier": -5
          }
        },
        "outcome": "You walk away. The wounded enemy soldiers lie in the mud. Your conscience is clear, if empty."
      }
    ]
  },
  "timing": {
    "priority": "normal",
    "cooldown_days": 30
  }
}
```

### Effect Types Reference

**Trait Effects:**
```json
"effects": {
  "traits": {
    "mercy": 50,           // +50 XP toward Mercy
    "honor": -80,          // -80 XP toward Honor (moving toward Devious)
    "valor": 100,          // +100 XP toward Valor
    "commander": 150,      // +150 XP toward Commander (hidden trait)
    "scout": 100,          // +100 XP toward Scout
    "rogue": 200,          // +200 XP toward Rogue
    "smuggler": 150        // +150 XP toward Smuggler
  }
}
```

**Reputation Effects:**
```json
"effects": {
  "reputation": {
    "soldier": 15,    // +15 to Soldier Reputation (peer respect)
    "lord": -10,      // -10 to Lord Reputation (disapproval)
    "officer": 20     // +20 to Officer Reputation (impress NCOs)
  }
}
```

**Escalation Effects:**
```json
"effects": {
  "escalation": {
    "scrutiny": 3,          // +3 Scrutiny (camp crime)
    "discipline": -2,   // -2 Discipline (improved behavior)
    "medical_risk": 1   // +1 Medical Risk (injury worsens)
  }
}
```

**Combined Example:**
```json
"effects": {
  "traits": {
    "honor": 100,
    "commander": 50
  },
  "reputation": {
    "lord": 20,
    "officer": 15,
    "soldier": -10
  },
  "escalation": {
    "scrutiny": -2,
    "discipline": -1
  },
  "xp": {
    "leadership": 30,
    "steward": 15
  },
  "gold": 50,
  "fatigue": 2
}
```

### Event Requirement Filters

**Trait Requirements:**
```json
"requirements": {
  "traits": {
    "mercy": { "min": 1 },           // Requires Merciful (level +1 or +2)
    "honor": { "min": -1, "max": 1 }, // Requires not strongly honorable/devious
    "valor": { "min": 0 },            // Requires not cowardly
    "commander": { "min": 8 },        // Requires Commander 8+ (hidden trait)
    "scout": { "min": 10 },           // Requires Scout 10+ (specialist)
    "rogue": { "min": 5, "max": 15 }  // Requires some but not max Rogue
  }
}
```

**Reputation Requirements:**
```json
"requirements": {
  "reputation": {
    "soldier": { "min": 30 },      // Soldiers must respect you
    "lord": { "min": 40 },         // Lord must trust you
    "officer": { "min": 25 }       // Officers see potential
  }
}
```

**Combined Complex Filter:**
```json
"requirements": {
  "tier": { "min": 5, "max": 7 },
  "traits": {
    "honor": { "min": 1 },
    "commander": { "min": 10 },
    "sergeant": { "min": 8 }
  },
  "reputation": {
    "lord": { "min": 40 },
    "officer": { "min": 35 }
  },
  "escalation": {
    "scrutiny": { "max": 3 },
    "discipline": { "max": 5 }
  },
  "context": {
    "at_war": true,
    "in_army": true
  }
}
```

This event only fires for:
- T5-T7 veterans
- Honorable leaders with command skills
- Trusted by lord and officers
- Clean record (low scrutiny/discipline)
- During wartime in army context

### Content Routing Examples

**Early Game (T1-T3):**
```json
{
  "id": "early_choice_theft",
  "requirements": {
    "tier": { "max": 3 },
    "traits": {} // No trait requirements - establishing identity
  },
  "effects": {
    "traits": {
      "honor": 50  // Small XP toward personality formation
    }
  }
}
```

**Mid Game (T4-T6) - Branching:**
```json
{
  "id": "specialist_medical",
  "requirements": {
    "tier": { "min": 4 },
    "traits": {
      "mercy": { "min": 1 },      // Established merciful
      "surgeon": { "min": 5 }      // Started medical path
    }
  },
  "effects": {
    "traits": {
      "surgeon": 200,              // Major boost to specialist skill
      "mercy": 50
    }
  }
}
```

**Late Game (T7+) - Multiple Paths:**
```json
{
  "id": "commander_deserter",
  "requirements": {
    "tier": { "min": 7 }
    // No other requirements - all T7+ see event
  },
  "options": [
    {
      "id": "by_the_book",
      "text": "Court-martial. The law is the law.",
      "requires": {
        "traits": {
          "honor": { "min": 1 },
          "sergeant": { "min": 8 }
        }
      }
      // Only honorable NCOs see this option
    },
    {
      "id": "double_agent",
      "text": "Use him to feed false intel.",
      "requires": {
        "traits": {
          "scout": { "min": 10 },
          "calculating": { "min": 1 }
        }
      }
      // Only calculating scouts see this option
    },
    {
      "id": "second_chance",
      "text": "Demote and give second chance.",
      "requires": {
        "traits": {
          "mercy": { "min": 1 },
          "commander": { "min": 8 }
        }
      }
      // Only merciful leaders see this option
    },
    {
      "id": "ignore_it",
      "text": "Not worth my time.",
      // Always available - neutral/lazy option
    }
  ]
}
```

---

## Implementation Roadmap

### Phase 1: Foundation (Week 1)

**1.1: Expand Escalation System**
```csharp
// File: src/Features/Escalation/EscalationState.cs
// Add new reputation tracks

[Serializable]
public sealed class EscalationState
{
    // EXISTING
    public int Scrutiny { get; set; }
    public int Discipline { get; set; }
    public int MedicalRisk { get; set; }
    
    // RENAMED (was LanceReputation)
    public int SoldierReputation { get; set; }
    
    // NEW
    public int LordReputation { get; set; }
    public int OfficerReputation { get; set; }
    
    // Constants
    public const int ReputationMin = -50;
    public const int ReputationMax = 50;
}
```

**1.2: Create Trait Integration Layer**
```csharp
// File: src/Features/Traits/TraitIntegrationHelper.cs
// New file to wrap native trait API

using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace Enlisted.Features.Traits
{
    public static class TraitIntegrationHelper
    {
        // Get trait from string ID
        public static TraitObject GetTrait(string traitId)
        {
            return traitId.ToLower() switch
            {
                "mercy" => DefaultTraits.Mercy,
                "valor" => DefaultTraits.Valor,
                "honor" => DefaultTraits.Honor,
                "generosity" => DefaultTraits.Generosity,
                "calculating" => DefaultTraits.Calculating,
                "commander" => DefaultTraits.Commander,
                "surgeon" => DefaultTraits.Surgeon,
                "sergeant" => DefaultTraits.SergeantCommandSkills,
                "engineer" => DefaultTraits.EngineerSkills,
                "rogue" => DefaultTraits.RogueSkills,
                "scout" => DefaultTraits.ScoutSkills,
                "trader" => DefaultTraits.Trader,
                "blacksmith" => DefaultTraits.Blacksmith,
                "thug" => DefaultTraits.Thug,
                "smuggler" => DefaultTraits.Smuggler,
                _ => null
            };
        }
        
        // Apply trait XP change
        public static void ApplyTraitXP(string traitId, int xpAmount)
        {
            var trait = GetTrait(traitId);
            if (trait == null)
            {
                ModLogger.Warn("Traits", $"Unknown trait ID: {traitId}");
                return;
            }
            
            int previousLevel = Hero.MainHero.GetTraitLevel(trait);
            TraitLevelingHelper.OnIncidentResolved(trait, xpAmount);
            int newLevel = Hero.MainHero.GetTraitLevel(trait);
            
            if (previousLevel != newLevel)
            {
                ModLogger.Info("Traits", 
                    $"Trait {traitId} changed: {previousLevel} → {newLevel}");
            }
        }
        
        // Check trait requirement
        public static bool MeetsTraitRequirement(string traitId, int? min, int? max)
        {
            var trait = GetTrait(traitId);
            if (trait == null) return true; // Unknown trait = no restriction
            
            int level = Hero.MainHero.GetTraitLevel(trait);
            
            if (min.HasValue && level < min.Value) return false;
            if (max.HasValue && level > max.Value) return false;
            
            return true;
        }
    }
}
```

**1.3: Update Event Effects Applier**
```csharp
// File: src/Features/Lances/Events/LanceLifeEventEffectsApplier.cs
// Add trait effect support

private void ApplyTraitEffects(Dictionary<string, int> traitEffects)
{
    if (traitEffects == null || traitEffects.Count == 0)
        return;
    
    foreach (var effect in traitEffects)
    {
        TraitIntegrationHelper.ApplyTraitXP(effect.Key, effect.Value);
    }
}

private void ApplyReputationEffects(Dictionary<string, int> reputationEffects)
{
    if (reputationEffects == null || reputationEffects.Count == 0)
        return;
    
    var escalation = EscalationManager.Instance;
    if (escalation == null) return;
    
    foreach (var effect in reputationEffects)
    {
        switch (effect.Key.ToLower())
        {
            case "soldier":
                escalation.State.SoldierReputation += effect.Value;
                break;
            case "lord":
                escalation.State.LordReputation += effect.Value;
                break;
            case "officer":
                escalation.State.OfficerReputation += effect.Value;
                break;
        }
    }
    
    escalation.State.ClampAll();
}
```

### Phase 2: Event System Updates (Week 1-2)

**2.1: Update Event Models**
```csharp
// File: src/Features/Lances/Events/Models/EventModels.cs
// Add trait/reputation support to event schema

public class EventRequirements
{
    // ... existing fields ...
    
    public Dictionary<string, TraitRequirement> Traits { get; set; }
    public Dictionary<string, IntRange> Reputation { get; set; }
}

public class TraitRequirement
{
    public int? Min { get; set; }
    public int? Max { get; set; }
}

public class EventEffects
{
    // ... existing fields ...
    
    public Dictionary<string, int> Traits { get; set; }
    public Dictionary<string, int> Reputation { get; set; }
}
```

**2.2: Update Event Trigger Evaluator**
```csharp
// File: src/Features/Lances/Events/LanceLifeEventTriggerEvaluator.cs
// Add trait requirement checking

private bool MeetsTraitRequirements(EventRequirements requirements)
{
    if (requirements.Traits == null || requirements.Traits.Count == 0)
        return true;
    
    foreach (var req in requirements.Traits)
    {
        if (!TraitIntegrationHelper.MeetsTraitRequirement(
            req.Key, req.Value.Min, req.Value.Max))
        {
            return false;
        }
    }
    
    return true;
}

private bool MeetsReputationRequirements(EventRequirements requirements)
{
    if (requirements.Reputation == null || requirements.Reputation.Count == 0)
        return true;
    
    var escalation = EscalationManager.Instance;
    if (escalation == null) return false;
    
    foreach (var req in requirements.Reputation)
    {
        int currentValue = req.Key.ToLower() switch
        {
            "soldier" => escalation.State.SoldierReputation,
            "lord" => escalation.State.LordReputation,
            "officer" => escalation.State.OfficerReputation,
            _ => 0
        };
        
        if (req.Value.Min.HasValue && currentValue < req.Value.Min.Value)
            return false;
        if (req.Value.Max.HasValue && currentValue > req.Value.Max.Value)
            return false;
    }
    
    return true;
}
```

**2.3: Update Event JSON Schema**
- Add trait/reputation fields to all event JSONs
- Start with 5-10 high-impact events as proof-of-concept
- Document schema changes in story-blocks-master-reference.md

### Phase 3: Lance/Formation Removal (Week 2)

**3.1: Delete Lance System**
```bash
# Remove entire Lances feature directory
rm -rf src/Features/Lances/Behaviors/
rm -rf src/Features/Lances/Leaders/
rm -rf src/Features/Lances/Simulation/
rm -rf src/Features/Lances/Core/LanceRegistry.cs

# Remove lance configs
rm ModuleData/Enlisted/lances_config.json
rm ModuleData/Enlisted/LancePersonas/

# Remove docs
rm docs/Features/Core/lance-assignments.md
rm docs/Features/Gameplay/lance-life.md

# Keep Events directory but rename
mv src/Features/Lances/Events/ src/Features/Events/
mv src/Features/Lances/UI/ src/Features/Events/UI/
```

**3.2: Remove Formation System**
```csharp
// File: src/Features/Assignments/Behaviors/EnlistedDutiesBehavior.cs
// Remove PlayerFormation property

// DELETE:
public string PlayerFormation { get; private set; }

// DELETE all formation-related methods:
public void SetFormation(string formation) { }
private void ValidateFormation() { }
```

**3.3: Remove Formation Choice Event**
```json
// File: ModuleData/Enlisted/Events/events_promotion.json
// DELETE: promotion_t1_t2_finding_your_place event
// REPLACE with simple narrative promotion event

{
  "id": "promotion_t1_t2_recognition",
  "category": "promotion",
  "content": {
    "title": "Recognition",
    "setup": "The sergeant nods at you after morning formation. 'You've proven yourself. Welcome to the company, soldier.'",
    "options": [
      {
        "id": "accept",
        "text": "Thank you, sergeant.",
        "effects": {
          "promotes": true,
          "traits": {
            "valor": 20  // Small boost for surviving to T2
          }
        }
      }
    ]
  }
}
```

**3.4: Update Schedule Activities**
```csharp
// File: src/Features/Schedule/Core/ActivityAssignmentLogic.cs
// Remove formation-based weighting

// DELETE:
private Dictionary<string, int> GetFormationWeights(string formation) { }

// REPLACE with rank/trait-based weighting:
private Dictionary<string, int> GetActivityWeights()
{
    var weights = new Dictionary<string, int>();
    
    // Base weights for all activities
    weights["patrol"] = 20;
    weights["sentry"] = 15;
    weights["work_detail"] = 15;
    weights["training"] = 10;
    
    // Boost specialist activities based on traits/skills
    if (Hero.MainHero.GetTraitLevel(DefaultTraits.Surgeon) >= 5)
        weights["medical_duty"] = 20;
    
    if (Hero.MainHero.GetSkillValue(DefaultSkills.Engineering) >= 40)
        weights["siege_work"] = 25;
    
    if (Hero.MainHero.GetTraitLevel(DefaultTraits.ScoutSkills) >= 8)
        weights["scout_mission"] = 30;
    
    // Context modifiers
    if (IsAtWar())
        weights["patrol"] *= 2;
    
    if (IsSieging())
        weights["siege_work"] *= 3;
    
    return weights;
}
```

### Phase 4: Content Updates (Week 3)

**4.1: Update Existing Events**

Priority events to update with trait/reputation:

1. **All Promotion Events** (T1→T2, T2→T3, etc.)
   - Add trait XP rewards based on choices
   - Add reputation changes

2. **High-Impact Moral Choice Events**
   - Corruption events → Honor/Calculating
   - Violence events → Mercy/Valor
   - Loyalty events → Generosity/Honor

3. **Specialist Events**
   - Medical events → Surgeon trait
   - Scout events → Scout trait
   - Criminal events → Rogue/Smuggler traits

Example conversion:

**BEFORE:**
```json
{
  "id": "duty_runner_ambush",
  "requirements": {
    "duty": "runner"
  },
  "effects": {
    "xp": { "athletics": 20 }
  }
}
```

**AFTER:**
```json
{
  "id": "runner_ambush_trait_aware",
  "requirements": {
    "tier": { "min": 2 },
    "context": {
      "at_war": true
    }
  },
  "options": [
    {
      "id": "fight",
      "text": "Draw sword and fight",
      "requires": {
        "traits": {
          "valor": { "min": 0 }
        }
      },
      "effects": {
        "traits": {
          "valor": 50
        },
        "reputation": {
          "officer": 10,
          "soldier": 15
        },
        "xp": { "onehanded": 30 }
      }
    },
    {
      "id": "run",
      "text": "Outrun them - message is critical",
      "requires": {
        "traits": {
          "calculating": { "min": 0 }
        }
      },
      "effects": {
        "traits": {
          "calculating": 30
        },
        "reputation": {
          "lord": 15,
          "soldier": -10
        },
        "xp": { "athletics": 40 }
      }
    }
  ]
}
```

**4.2: Create New Trait-Driven Events**

Design 10-15 new events that showcase trait system:

1. **"Trust Test"** (requires Honor +1, Lord Rep 30+)
   - Lord gives you sensitive task
   - Options show different trait paths

2. **"Criminal Contact"** (requires Rogue 5+, Scrutiny 5+)
   - Underworld figure approaches
   - Can start criminal specialist path

3. **"Medical Crisis"** (requires Surgeon 8+, Mercy +1)
   - Triage decision during battle
   - Mercy vs. Calculating conflict

4. **"Leadership Challenge"** (requires Commander 10+, T7+)
   - Subordinate questions your order
   - Different approaches based on traits

5. **"Scout Deep"** (requires Scout 10+, Valor +1)
   - High-risk reconnaissance
   - Can fail if not qualified

### Phase 5: UI Updates (Week 3-4)

**5.1: Update Camp Status Panel**
```csharp
// File: src/Features/Camp/UI/Management/Tabs/CampReportsVM.cs
// Show reputation and traits in status

private void RefreshReputationDisplay()
{
    var escalation = EscalationManager.Instance;
    
    // Reputation bars
    SoldierReputation = escalation.State.SoldierReputation;
    LordReputation = escalation.State.LordReputation;
    OfficerReputation = escalation.State.OfficerReputation;
    
    // Personality traits (visible)
    MercyLevel = Hero.MainHero.GetTraitLevel(DefaultTraits.Mercy);
    ValorLevel = Hero.MainHero.GetTraitLevel(DefaultTraits.Valor);
    HonorLevel = Hero.MainHero.GetTraitLevel(DefaultTraits.Honor);
    GenerosityLevel = Hero.MainHero.GetTraitLevel(DefaultTraits.Generosity);
    CalculatingLevel = Hero.MainHero.GetTraitLevel(DefaultTraits.Calculating);
    
    // Specialist traits (hidden, show if > 0)
    var specialistTraits = new List<string>();
    if (Hero.MainHero.GetTraitLevel(DefaultTraits.Commander) >= 5)
        specialistTraits.Add("Commander");
    if (Hero.MainHero.GetTraitLevel(DefaultTraits.Surgeon) >= 5)
        specialistTraits.Add("Surgeon");
    if (Hero.MainHero.GetTraitLevel(DefaultTraits.ScoutSkills) >= 5)
        specialistTraits.Add("Scout");
    // ... etc
    
    SpecialistText = specialistTraits.Count > 0 
        ? string.Join(", ", specialistTraits)
        : "None";
}
```

**5.2: Add Reputation Tooltips**
```
Soldier Reputation: 35/100 (Promising)
"Your fellow soldiers respect you, though some think you're too strict."

Lord Reputation: 45/100 (Respected)
"Your lord trusts you with important tasks."

Officer Reputation: 20/100 (Promising)
"The officers see potential but want to see more leadership."

Soldier Reputation: -35/100 (Disliked)
"Most of the men avoid you. Your harsh methods have made you unpopular."

Officer Reputation: -65/100 (Despised)
"The officers consider you a liability. One more failure and you're done."
```

**5.3: Add Trait Display in Character Screen**

Native character screen already shows personality traits!
Just make sure trait changes are logged:

```csharp
// Enlisted events automatically trigger native notifications
// No additional UI work needed for trait display
```

### Phase 6: Balance & Testing (Week 4)

**6.1: Trait XP Tuning**

Test progression rates:
- Early events (T1-T3): 20-50 XP per choice
- Mid events (T4-T6): 50-150 XP per choice
- Late events (T7+): 100-300 XP per choice

Target: Player reaches trait level ±1 by T3, ±2 by T6

**6.2: Reputation Tuning**

Test reputation progression:
- Small choices: ±5 rep
- Medium choices: ±10 rep
- Major choices: ±15-20 rep
- Critical choices: ±25-30 rep

Target: 
- Soldier Rep 30+ by T4 (if popular path)
- Lord Rep 40+ by T6 (if loyal path)
- Officer Rep 35+ by T5 (if officer path)

**6.3: Content Access Testing**

Verify event gating works:
- Specialist events unlock at appropriate trait levels
- High-tier events require multiple criteria
- No dead-end paths (always some content available)

**6.4: Migration Testing**

Test with existing saves:
- Lance Reputation converts to Soldier Reputation
- Formation setting ignored (removed)
- Events continue to work
- No crashes or data loss

---

## Migration Plan

### Save Compatibility

**Save Data Changes:**
```csharp
// File: src/Features/Escalation/EscalationManager.cs
// SyncData migration

public override void SyncData(IDataStore dataStore)
{
    // ... existing sync ...
    
    // MIGRATION: Rename lance_rep to soldier_rep
    if (dataStore.IsLoading)
    {
        // Try old key first
        int oldLanceRep = 0;
        dataStore.SyncData("esc_lance_rep", ref oldLanceRep);
        
        if (oldLanceRep != 0)
        {
            _state.SoldierReputation = oldLanceRep;
            ModLogger.Info("Migration", 
                $"Converted LanceReputation ({oldLanceRep}) to SoldierReputation");
        }
    }
    
    // New keys
    var soldierRep = _state.SoldierReputation;
    var lordRep = _state.LordReputation;
    var officerRep = _state.OfficerReputation;
    
    dataStore.SyncData("esc_soldier_rep", ref soldierRep);
    dataStore.SyncData("esc_lord_rep", ref lordRep);
    dataStore.SyncData("esc_officer_rep", ref officerRep);
    
    _state.SoldierReputation = soldierRep;
    _state.LordReputation = lordRep;
    _state.OfficerReputation = officerRep;
}
```

**Lance/Formation Removal:**
```csharp
// File: src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs
// Remove lance/formation data on load

public override void SyncData(IDataStore dataStore)
{
    // ... existing sync ...
    
    if (dataStore.IsLoading)
    {
        // Clear obsolete data (don't save it back)
        string ignoredLanceId = "";
        string ignoredFormation = "";
        
        dataStore.SyncData("lance_id", ref ignoredLanceId);
        dataStore.SyncData("player_formation", ref ignoredFormation);
        
        if (!string.IsNullOrEmpty(ignoredLanceId))
        {
            ModLogger.Info("Migration", 
                $"Removed obsolete lance assignment: {ignoredLanceId}");
        }
        
        if (!string.IsNullOrEmpty(ignoredFormation))
        {
            ModLogger.Info("Migration", 
                $"Removed obsolete formation setting: {ignoredFormation}");
        }
    }
}
```

### Event Migration

**Update Event IDs:**
```
OLD: duty_runner_ambush (requires duty: runner)
NEW: activity_runner_ambush (no requirement or tier-based)

OLD: lance_drinking_contest (requires lance_style: tribal)
NEW: camp_drinking_contest (requires soldier_rep: 20+)

OLD: formation_infantry_drill (requires formation: infantry)
NEW: activity_drill (context-based, available to all)
```

**Event File Updates:**
```bash
# Backup existing events
cp -r ModuleData/Enlisted/Events/ ModuleData/Enlisted/Events_backup/

# Update event JSONs with new schema
# Manual process - update 80+ events gradually
# Priority: High-impact events first
```

### User Communication

**Changelog Entry:**
```
## v0.9.0 - Identity System Overhaul

### BREAKING CHANGES
- **Removed Lance System**: Lance assignments and lance reputation removed
- **Removed Formation System**: Manual formation selection removed
- **Save Compatibility**: Old saves will load, but lance/formation data is discarded

### NEW FEATURES
- **Native Trait Integration**: Player choices now affect personality traits (Mercy, Valor, Honor, Generosity, Calculating)
- **Specialist Paths**: Hidden skill traits unlock specialist content (Commander, Surgeon, Scout, Rogue, etc.)
- **Reputation System Expanded**: Track reputation with Lord, Officers, and fellow Soldiers separately
- **Emergent Identity**: Your character identity emerges naturally from choices, not menu selections
- **Better Replayability**: Different trait combinations unlock different content paths

### MIGRATION NOTES
- Existing Lance Reputation converts to Soldier Reputation
- Formation setting is ignored (activities assign dynamically)
- All events remain functional
- Some events now have trait/reputation requirements
```

---

## Technical Reference

### Class Hierarchy Changes

**BEFORE:**
```
EnlistedDutiesBehavior
├── PlayerFormation (string)
├── CurrentDuty (Duty object)
└── SetFormation(string)

LanceRegistry
├── GetLances()
├── AssignLance(Hero)
└── GetLanceById(string)

EscalationState
├── Scrutiny
├── Discipline
├── LanceReputation
└── MedicalRisk
```

**AFTER:**
```
EscalationState (expanded)
├── Scrutiny
├── Discipline
├── SoldierReputation (renamed from LanceReputation)
├── LordReputation (new)
├── OfficerReputation (new)
└── MedicalRisk

TraitIntegrationHelper (new)
├── GetTrait(string)
├── ApplyTraitXP(string, int)
└── MeetsTraitRequirement(string, int?, int?)

// LanceRegistry deleted
// PlayerFormation property deleted
```

### Event Schema Comparison

**OLD Schema (Lance/Formation):**
```json
{
  "requirements": {
    "duty": "runner",
    "formation": "infantry",
    "lance_style": "legion"
  },
  "effects": {
    "lance_reputation": 10,
    "xp": { "athletics": 20 }
  }
}
```

**NEW Schema (Trait/Reputation):**
```json
{
  "requirements": {
    "tier": { "min": 2 },
    "traits": {
      "valor": { "min": 0 },
      "honor": { "min": -1 }
    },
    "reputation": {
      "soldier": { "min": 20 }
    }
  },
  "effects": {
    "traits": {
      "valor": 50,
      "honor": 30
    },
    "reputation": {
      "soldier": 10,
      "lord": 5
    },
    "xp": { "athletics": 20 }
  }
}
```

### File Structure Changes

**DELETED:**
```
src/Features/Lances/
├── Behaviors/EnlistedLanceMenuBehavior.cs
├── Behaviors/LanceStoryBehavior.cs
├── Leaders/PersistentLanceLeadersBehavior.cs
├── Leaders/PersistentLanceLeaderModels.cs
├── Simulation/LanceLifeSimulationBehavior.cs
├── Simulation/LanceMemberState.cs
└── Core/LanceRegistry.cs

docs/Features/Core/lance-assignments.md
docs/Features/Gameplay/lance-life.md
```

**RENAMED:**
```
src/Features/Lances/Events/ → src/Features/Events/
src/Features/Lances/UI/ → src/Features/Events/UI/
```

**NEW:**
```
src/Features/Traits/
└── TraitIntegrationHelper.cs

docs/ImplementationPlans/
└── traits-identity-system.md (this document)
```

**MODIFIED:**
```
src/Features/Escalation/
├── EscalationState.cs (add Lord/Officer reputation)
└── EscalationManager.cs (migration logic)

src/Features/Events/
├── LanceLifeEventEffectsApplier.cs (trait/reputation support)
├── LanceLifeEventTriggerEvaluator.cs (trait requirement checking)
└── Models/EventModels.cs (schema updates)

src/Features/Assignments/
└── Behaviors/EnlistedDutiesBehavior.cs (remove PlayerFormation)

src/Features/Schedule/
└── Core/ActivityAssignmentLogic.cs (remove formation weighting)
```

### API Surface Changes

**REMOVED:**
```csharp
// EnlistedDutiesBehavior
public string PlayerFormation { get; }
public void SetFormation(string formation);

// LanceRegistry
public static LanceDefinition GetLanceById(string id);
public static List<LanceDefinition> GetLances();

// EscalationState
public int LanceReputation { get; set; }
```

**ADDED:**
```csharp
// EscalationState
public int SoldierReputation { get; set; }
public int LordReputation { get; set; }
public int OfficerReputation { get; set; }

// TraitIntegrationHelper
public static TraitObject GetTrait(string traitId);
public static void ApplyTraitXP(string traitId, int xpAmount);
public static bool MeetsTraitRequirement(string traitId, int? min, int? max);
```

---

## Success Metrics

### Development Metrics

**Code Reduction:**
- Lance system deletion: ~3000 lines
- Formation system deletion: ~1500 lines
- Trait integration: +500 lines
- **Net reduction: ~4000 lines**

**System Simplification:**
- Removed: 2 major systems (Lance, Formation)
- Added: 1 small integration layer (Trait wrapper)
- Expanded: 1 existing system (Escalation)
- **Net: 2 systems removed, 0 new systems**

### Player Experience Metrics

**Identity Formation:**
- Target: 80% of players reach at least trait level ±1 by T3
- Target: 50% of players reach trait level ±2 by T6
- Target: Players see 2-3 different specialist paths unlock by T6

**Content Access:**
- Target: 100% of events remain accessible (no dead ends)
- Target: High-tier events require 3+ criteria (meaningful gating)
- Target: Replayability increases (5+ distinct builds possible)

**Clarity:**
- Target: 90% of players understand trait system (survey/feedback)
- Target: Reputation tooltips used frequently (telemetry)
- Target: Reduced confusion about "what formation am I?" (support tickets)

---

## Conclusion

This implementation study documents the complete replacement of Lance and Formation systems with native trait integration. The result is:

1. **Simpler**: ~4000 fewer lines of code, 2 fewer systems
2. **Deeper**: Emergent identity from choices, not menus
3. **More Replayable**: Trait combinations create unique paths
4. **Native Integration**: Uses proven Bannerlord systems
5. **Authentic**: Identity emerges from play, not selection

The transition preserves all existing content while enabling richer, choice-driven character development. Player identity becomes a living story, not a static label.

**Next Steps:**
1. Review this document with team
2. Begin Phase 1 implementation (Foundation)
3. Create proof-of-concept with 5 trait-aware events
4. Test progression rates and balance
5. Full rollout across all 80+ events

