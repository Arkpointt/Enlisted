# Event Reward Choices

**Summary:** Feature specification for extending the event system to allow players to choose how they are rewarded. Players can select between different reward types (XP, gold, reputation) and customize which skills develop, providing meaningful agency over character progression through events.

**Status:** 📋 Specification  
**Last Updated:** 2025-12-22  
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

### Auto-Selection (Optional Enhancement)

For players who don't want to micromanage, add preference system:

```json
// In enlisted_config.json
"event_preferences": {
  "enabled": true,
  "training_focus": "auto|formation_appropriate|player_choice",
  "gold_vs_reputation": "auto|prefer_gold|prefer_reputation|balanced",
  "risk_tolerance": "auto|cautious|balanced|aggressive"
}
```

**Auto Behavior**:
- `formation_appropriate`: Cavalry → riding, Infantry → melee, Archers → bow
- `prefer_gold`: Always take gold when offered
- `cautious`: Always take safest option
- `auto`: Learn from player's last 5 choices

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

**Handling**: 
- Option A: Pause time, show dialog (breaks immersion)
- Option B: Auto-select using preferences (recommended)
- Option C: Queue for next safe moment

**Recommendation**: Use Option B, log choice in debug category.

### 5. Save/Load During Reward Selection
**Case**: Player saves game while reward dialog is open.

**Handling**: Persist dialog state in save data, restore on load.

## Acceptance Criteria

### Core Functionality
- [ ] Event options can specify `reward_choices` with 2-4 options
- [ ] Reward choice dialog displays after outcome narrative
- [ ] Selected reward is applied correctly (XP, gold, relations, effects)
- [ ] Feedback messages show what player received
- [ ] Conditions (formation, tier, gold) properly filter choices

### Event Coverage
- [ ] All training events offer skill focus choices
- [ ] Social events with gold rewards offer gold/reputation tradeoffs
- [ ] Weapon training offers formation-appropriate weapon selection
- [ ] At least 5 events per category updated with choices

### UX/Polish
- [ ] Tooltips explain tradeoffs clearly
- [ ] Grayed-out options show why they're unavailable
- [ ] Reward amounts are balanced across choices (no trap options)
- [ ] Combat log shows chosen reward clearly

### Optional: Auto-Selection
- [ ] Config setting to enable/disable auto-select
- [ ] Formation-appropriate defaults work correctly
- [ ] Preference learning tracks last 5 choices
- [ ] Auto-select works during AI-unsafe moments (army travel)

### Performance
- [ ] Reward dialog doesn't block time when avoidable
- [ ] No performance impact when auto-select enabled
- [ ] Save/load preserves dialog state if interrupted

## Implementation Notes

### Phase 1: Core Schema & Dialog (Week 1)
1. Extend `EventOptionDefinition` with `RewardChoices` field
2. Create `RewardChoiceDialog` inquiry class
3. Update `EventEffectsApplier` to check for reward choices
4. Implement two-stage application: outcome → reward selection → apply

### Phase 2: Event Updates (Week 2)
1. Update all `events_training.json` events with skill focus choices
2. Update social events in `events_general.json` with gold/rep tradeoffs
3. Update weapon training decisions in `Decisions/decisions.json` with weapon selection
4. Add risk-level choices to 3-5 dangerous events

### Phase 3: Polish & Balance (Week 3)
1. Balance reward amounts (test that choices feel equivalent)
2. Add tooltips explaining tradeoffs
3. Improve feedback messages (show choices clearly in combat log)
4. Playtest and iterate

### Phase 4: Optional Enhancements (Week 4)
1. Implement preference system in config
2. Add auto-selection logic
3. Add preference learning from player choices
4. Test auto-select during army travel

## Examples to Implement

### High-Priority Event Updates

**1. Shield Wall Drill** (`inf_train_shield_wall`)
- Current: Fixed Polearm +25, OneHanded +20, Athletics +15
- New: Choice between Polearm focus (50), Shield focus (50 OneHanded), or Balanced (25/25)

**2. Lord's Hunt** (`decision_lord_hunt_invitation`)
- Current: Fixed Gold +15, Soldier Rep +2
- New: Take gold (25), Build reputation (Soldier +4, Lord +2), or Balanced (12 gold, +2 rep)

**3. Weapon Training** (`player_request_training` in events_player_decisions.json)
- Current: Separate options for each weapon (menu clutter)
- New: Single "Request Training" option → then choose weapon focus

**4. Dangerous Patrol** (new event to create)
- Create new patrol event with risk-level choices
- Demonstrates risk/reward tradeoff mechanic

**5. Evening Dice Game** (`decision_comrade_dice`)
- Current: Fixed gold in/out
- New: After winning, choose: "Keep winnings (gold)" vs "Buy rounds (+4 Soldier Rep)"

**6. Camp Socializing** (general event)
- Current: Generic rep gain
- New: Choose who to spend time with (NCO +3 vs Fellow Soldiers +2 each vs Lord +1)

### Event Distribution Target

| Event Type | Events to Update | Priority |
|------------|-----------------|----------|
| Training (player-initiated) | 12 events | Critical |
| Combat training (automatic) | 8 events | High |
| Social events | 10 events | High |
| Duty-related | 6 events | Medium |
| Dangerous missions | 5 events | Medium |
| Rest/downtime | 4 events | Low |

**Total**: ~45 events updated with meaningful choices

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

