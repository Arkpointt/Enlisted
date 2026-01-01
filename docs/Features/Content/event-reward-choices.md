# Event Reward Choices

**Summary:** System allowing players to choose how they are rewarded after events. Players can select between different reward types (XP, gold, reputation) and customize which skills develop, providing meaningful agency over character progression through events.

**Status:** ⚠️ **CODED BUT NOT CONTENT-IMPLEMENTED** - System exists, no events use it yet  
**Implementation:** `EventDeliveryManager.ShowSubChoicePopup()`, `RewardChoices` parsing active  
**Blocker:** None - can add `reward_choices` blocks to any event JSON at any time  
**Last Updated:** 2025-12-31  
**Related Docs:** [Content System Architecture](content-system-architecture.md), [Event System Schemas](event-system-schemas.md)

---

## Overview

Extends the Enlisted Events system to give players meaningful agency over how they're rewarded for event outcomes. Instead of receiving predetermined rewards, players can choose between different reward types (XP vs gold vs reputation) and customize which skills they develop.

## Purpose

**Problem**: Current events offer predetermined rewards. Players choose scenarios but cannot control *how* they benefit - which skills level up, whether to take gold or reputation, etc. This limits player agency and character build diversity.

**Solution**: Add post-event reward selection dialogs that let players customize outcomes based on their playstyle and character build goals.

## Inputs/Outputs

### Inputs
- Event option selected by player
- Base reward pool from event definition
- Player preferences (optional: auto-select based on formation/build)

### Outputs
- Customized rewards applied to player
- Optional: Preference learning for future auto-selection
- Feedback messages showing chosen rewards

## Behavior

### Core Mechanic: Reward Selection Dialog

After an event succeeds, if it offers flexible rewards, show a secondary choice:

```
Event outcome plays out...
↓
[If event has flexible rewards]
↓
"How do you want to benefit from this?"
↓
Present 2-4 reward options
↓
Apply selected reward
```

### Reward Choice Types

#### 1. Skill Focus Choices
**Use Case**: Training events, combat events, physical activities

**Example: Shield Wall Drill**
```json
"reward_choices": {
  "type": "skill_focus",
  "pool_xp": 60,
  "options": [
    {
      "id": "focus_polearm",
      "text": "Focus on polearm technique",
      "rewards": { "skillXp": { "Polearm": 50, "Athletics": 10 } }
    },
    {
      "id": "focus_defense",
      "text": "Focus on shield work",
      "rewards": { "skillXp": { "OneHanded": 50, "Athletics": 10 } }
    },
    {
      "id": "balanced",
      "text": "Train everything equally",
      "rewards": { "skillXp": { "Polearm": 25, "OneHanded": 25, "Athletics": 10 } }
    }
  ]
}
```

**Player Impact**: 
- Infantry players focus on polearm/shield
- Versatile players spread XP
- Supports character build diversity

#### 2. Gold vs. Reputation Tradeoffs
**Use Case**: Social events, lord interactions, successful missions

**Example: Lord's Hunt**
```json
"reward_choices": {
  "type": "compensation",
  "options": [
    {
      "id": "take_gold",
      "text": "Accept your share of the catch (+25 gold)",
      "rewards": { "gold": 25 }
    },
    {
      "id": "build_reputation",
      "text": "Decline payment to build goodwill (+3 Soldier Rep, +2 Lord Relation)",
      "rewards": { 
        "effects": { "soldierRep": 3 },
        "relation": { "lord": 2 }
      }
    },
    {
      "id": "partial",
      "text": "Take modest payment (+12 gold, +1 Soldier Rep)",
      "rewards": { 
        "gold": 12,
        "effects": { "soldierRep": 1 }
      }
    }
  ]
}
```

**Player Impact**:
- Poor players prioritize gold
- Ambitious players invest in reputation
- Middle-ground option for balanced approach

#### 3. Weapon/Formation Specialization
**Use Case**: Combat training, weapon drills

**Example: Weapon Training Request**
```json
"reward_choices": {
  "type": "weapon_focus",
  "text": "Which weapon do you want to train?",
  "options": [
    {
      "id": "one_handed",
      "text": "One-Handed Weapons (+40 OneHanded)",
      "condition": null,
      "rewards": { "skillXp": { "OneHanded": 40 } }
    },
    {
      "id": "two_handed",
      "text": "Two-Handed Weapons (+40 TwoHanded)",
      "condition": null,
      "rewards": { "skillXp": { "TwoHanded": 40 } }
    },
    {
      "id": "polearm",
      "text": "Polearms (+40 Polearm)",
      "condition": null,
      "rewards": { "skillXp": { "Polearm": 40 } }
    },
    {
      "id": "bow",
      "text": "Archery (+40 Bow)",
      "condition": "formation:ranged",
      "rewards": { "skillXp": { "Bow": 40 } }
    },
    {
      "id": "crossbow",
      "text": "Crossbow (+40 Crossbow)",
      "condition": "formation:ranged",
      "rewards": { "skillXp": { "Crossbow": 40 } }
    }
  ]
}
```

**Player Impact**:
- Cavalry players choose different weapons than infantry
- Archers focus on ranged skills
- Formation-appropriate options automatically available

#### 4. Risk Level Selection
**Use Case**: Dangerous missions, patrols, raids

**Example: Dangerous Patrol**
```json
"reward_choices": {
  "type": "risk_level",
  "text": "How aggressive should you be on patrol?",
  "options": [
    {
      "id": "cautious",
      "text": "Play it safe (90% success, modest rewards)",
      "success_chance": 0.9,
      "rewards": { 
        "xp": { "enlisted": 15 },
        "effects": { "soldierRep": 1 }
      }
    },
    {
      "id": "balanced",
      "text": "Balanced approach (70% success, good rewards)",
      "success_chance": 0.7,
      "rewards": { 
        "xp": { "enlisted": 30 },
        "effects": { "soldierRep": 2 }
      },
      "failure": {
        "effects": { "medical_risk": 1 }
      }
    },
    {
      "id": "aggressive",
      "text": "Take big risks (50% success, excellent rewards)",
      "success_chance": 0.5,
      "rewards": { 
        "xp": { "enlisted": 60 },
        "gold": 30,
        "effects": { "soldierRep": 5 }
      },
      "failure": {
        "effects": { 
          "medical_risk": 3,
          "scrutiny": 2 
        }
      }
    }
  ]
}
```

**Player Impact**:
- Conservative players take safe options
- Aggressive players gamble for big rewards
- Risk tolerance becomes part of playstyle

#### 5. Fatigue Management Choices
**Use Case**: Rest events, downtime activities

**Example: Evening Off**
```json
"reward_choices": {
  "type": "rest_focus",
  "text": "How do you want to spend your evening?",
  "options": [
    {
      "id": "full_rest",
      "text": "Get a full night's sleep (-4 Fatigue)",
      "rewards": { "fatigue_relief": 4 }
    },
    {
      "id": "socialize",
      "text": "Socialize with your comrades (-2 Fatigue, +2 Soldier Rep)",
      "rewards": { 
        "fatigue_relief": 2,
        "effects": { "soldierRep": 2 }
      }
    },
    {
      "id": "study",
      "text": "Study tactics and strategy (-1 Fatigue, +20 Tactics XP)",
      "rewards": { 
        "fatigue_relief": 1,
        "skillXp": { "Tactics": 20 }
      }
    }
  ]
}
```

**Player Impact**:
- Exhausted players prioritize rest
- Social players build relationships
- Tactician builds develop planning skills

### Implementation Schema

Add to event option definition:

```json
{
  "id": "option_id",
  "text": "Initial choice text",
  "resultText": "Outcome narrative",
  
  "reward_choices": {
    "type": "skill_focus|compensation|weapon_focus|risk_level|rest_focus",
    "prompt": "Optional custom prompt text",
    "auto_select_preference": "formation|last_choice|gold_focus|xp_focus",
    "options": [
      {
        "id": "choice_id",
        "text": "Choice description",
        "tooltip": "Optional detailed explanation",
        "condition": "formation:infantry|tier >= 3|gold >= 50",
        "success_chance": 0.7,
        "rewards": { /* standard reward structure */ },
        "failure": { /* failure outcome if success_chance present */ }
      }
    ]
  }
}
```

### Auto-Selection (Future Enhancement)

**Status:** Not implemented in code or config

For players who don't want to micromanage, a future preference system could auto-select rewards:

```json
// Proposed: enlisted_config.json
"event_preferences": {
  "enabled": true,
  "training_focus": "auto|formation_appropriate|player_choice",
  "gold_vs_reputation": "auto|prefer_gold|prefer_reputation|balanced",
  "risk_tolerance": "auto|cautious|balanced|aggressive"
}
```

**Proposed Behavior**:
- `formation_appropriate`: Cavalry → riding, Infantry → melee, Archers → bow
- `prefer_gold`: Always take gold when offered
- `cautious`: Always take safest option
- `auto`: Learn from player's last 5 choices

This is **not required** for initial content implementation - manual selection works fine.

### UI Flow

#### Option 1: Inquiry Dialog (Current System)
```
[Event Title]
[Narrative setup]

> [Initial Option A]
> [Initial Option B]
↓ Player selects Option A
↓
[Outcome narrative plays]
↓
[Reward Choice Title]
"How do you want to benefit?"

> [Reward Option 1: +50 Polearm]
> [Reward Option 2: +50 OneHanded]  
> [Reward Option 3: Balanced (+25/+25)]
↓ Player selects reward
↓
[Apply rewards, show feedback]
```

#### Option 2: Inline Choice (Streamlined)
```
[Event Title]
[Narrative setup]

> [Hold the line (Focus Polearm)]
> [Hold the line (Focus Shield Work)]
> [Hold the line (Balanced)]
> [Take it easy]
↓
[Apply outcome immediately]
```

**Recommendation**: Use Option 1 (two-stage dialog) for complex events, Option 2 (inline) for simple training menus.

## Edge Cases

### 1. Insufficient Resources for Choice
**Case**: Player chooses "aggressive" option requiring 3 fatigue, but only has 2 remaining.

**Handling**: Gray out options with unmet requirements, show red tooltip explaining why.

### 2. Conditional Options Not Available
**Case**: Archer-only option shown to infantry player.

**Handling**: Filter options by condition before presenting. If no options remain, fall back to default reward.

### 3. Auto-Selection with No Preference Learned
**Case**: Player has auto-select enabled but hasn't made manual choices yet.

**Handling**: Use formation-appropriate defaults as fallback.

### 4. Reward Choice During AI Fast-Forward
**Case**: Event fires during army travel (unstoppable fast-forward).

**Handling Options**: 
- Option A: Pause time, show dialog (current behavior for all events)
- Option B: Auto-select using preferences (requires future enhancement)
- Option C: Queue for next safe moment (requires queue system)

**Current Behavior**: System pauses for player input (same as all inquiry events).

### 5. Save/Load During Reward Selection
**Case**: Player saves game while reward dialog is open.

**Handling**: Persist dialog state in save data, restore on load.

## Acceptance Criteria (For Content Implementation)

### Core Functionality
- [x] Event options can specify `reward_choices` with 2-4 options (code ready)
- [x] Reward choice dialog displays after outcome narrative (code ready)
- [x] Selected reward is applied correctly (code ready)
- [ ] At least 10 events updated with `reward_choices` blocks
- [ ] Playtesting confirms choices feel meaningful

### Event Coverage (Content Work Required)
- [ ] Training events offer skill focus choices
- [ ] Social events offer gold/reputation tradeoffs
- [ ] Weapon training offers formation-appropriate selection
- [ ] Order events include risk-level or rest focus choices

### UX/Polish
- [ ] Tooltips explain tradeoffs clearly
- [ ] Reward amounts are balanced across choices (no trap options)
- [ ] Feedback messages show chosen rewards in combat log

### Future Enhancements (Not Required)
- [ ] Auto-selection preferences system
- [ ] Formation-appropriate defaults
- [ ] Preference learning from player choices

## Implementation Status

### Code (✅ Complete)
- `RewardChoices` class exists in `EventDefinition.cs`
- `EventCatalog.ParseRewardChoices()` parses JSON `reward_choices` blocks
- `EventDeliveryManager.ShowSubChoicePopup()` displays the selection UI
- System is fully functional and ready to use

### Content (❌ Not Started)
- **No events currently use `reward_choices`** in their JSON definitions
- All event JSONs use fixed rewards (traditional system)
- This document serves as the specification for future content work

### To Implement
Add `reward_choices` blocks to event option definitions in:
- `ModuleData/Enlisted/Events/*.json` - Camp events, training, general
- `ModuleData/Enlisted/Orders/*.json` - Order events during duty
- `ModuleData/Enlisted/Decisions/*.json` - Player-initiated decisions

The system will activate automatically once JSON content includes `reward_choices` blocks.

## Proposed Event Updates

These are high-priority candidates for adding `reward_choices` to existing events:

**1. Training Events** (`ModuleData/Enlisted/Events/events_training_*.json`)
- Add skill focus choices to all training events
- Example: Shield wall drill → choose Polearm focus vs Shield focus vs Balanced

**2. Player Decisions** (`ModuleData/Enlisted/Decisions/*.json`)
- Add gold/reputation tradeoffs to social decisions
- Add weapon selection to training requests

**3. Order Events** (`ModuleData/Enlisted/Orders/*.json`)
- Add risk-level choices to dangerous duty events
- Add rest vs socialization choices to downtime events

**4. General Camp Events** (`ModuleData/Enlisted/Events/events_general.json`)
- Add reward customization to social interaction events
- Add NPC relationship targeting (who to befriend)


## Success Metrics

### Player Agency
- 80%+ of events offer at least 2 reward choices
- Players use all reward options (not dominated by one "best" choice)
- Different formations choose different rewards (cavalry ≠ infantry choices)

### Engagement
- Player feedback indicates choices feel meaningful
- Build diversity increases (not everyone focuses same skills)
- No complaints about "automatic" or "on-rails" progression

### Balance
- No single reward choice dominates (>60% pickrate) in balanced events
- Gold vs reputation choices picked roughly 50/50 (adjusted for player wealth)
- Risk-level choices spread across all 3 tiers

## Future Enhancements

### Multi-Step Chains
Events that remember choices and reference them later:
```
Day 1: Train with Comrade A or B?
Day 7: Comrade [A/B] remembers training, offers special mission
```

### Reputation-Gated Rewards
High reputation unlocks better reward options:
```
Soldier Rep < 10: Standard rewards
Soldier Rep 10-20: +1 bonus option (slightly better)
Soldier Rep 20+: +2 bonus options (best rewards)
```

### Lord Favor System
Track which rewards players choose (gold vs reputation):
```
Lord notices: "You've declined payment 3 times - you're loyal."
Unlocks: Special quest chain
```

### Companion Preferences
NPCs react to your choices:
```
Always taking gold → Fellow soldiers see you as greedy
Always building reputation → Seen as ambitious/political
Balanced → Seen as pragmatic
```

