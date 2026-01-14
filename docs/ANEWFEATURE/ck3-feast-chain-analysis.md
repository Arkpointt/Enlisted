# CK3 Feast Chain Event Analysis

**Date:** 2026-01-14  
**Purpose:** Understand how CK3 implements activity-based event chains for application to Enlisted's order-based decision system

---

## Key Findings

### 1. **Activities, Not Random Interrupts**
Feasts are **player-initiated activities** (like your orders). Once the player accepts a feast invitation, events fire **during the activity**, not randomly during normal gameplay.

**Parallel to Enlisted:** Orders are like activities - player accepts duty, events fire **during the duty**, not randomly during camp time.

### 2. **Hidden Setup Events Control Everything**
```
feast_main_live_fowl.0001 = {
    hidden = yes
    
    immediate = {
        # Select characters for specific roles
        random_attending_character = { save_scope_as = fowl_dinner_target }
        random_attending_character = { save_scope_as = fowl_bird_killer }
        
        # Trigger different events to different characters
        activity_host = { trigger_event = feast_main_live_fowl.0003 }
        scope:fowl_dinner_target = { trigger_event = feast_main_live_fowl.0004 }
        scope:fowl_bird_killer = { trigger_event = feast_main_live_fowl.0005 }
    }
}
```

**The Pattern:**
1. **Setup event** (hidden) selects characters and determines outcome
2. **Fires multiple simultaneous events** to different characters with different perspectives
3. **Each character** sees their own version of the same scene

### 3. **Probability Structure**

**Two-Stage Rolls:**

**Stage 1: Event Selection** (which event fires)
- Feasts use weighted triggers and `random_list` for event selection
- Events check requirements (traits, relationships, world state)
- Example: "Live Fowl" event requires certain food options NOT be chosen

**Stage 2: Outcome Rolls** (what happens in the event)
```
random_list = {
    15 = { increase_wounds_effect = { REASON = feast_accident } }  # 15% wounded
    85 = { } # 85% nothing happens
}
```

**Common probability patterns:**
- `random = { chance = 50 }` - 50% chance block executes
- `random_list { 15 = {...} 85 = {...} }` - Weighted outcomes
- `ai_chance = { base = 100 }` - AI decision weighting

### 4. **Chain Event Mechanics**

**Method 1: Immediate Chaining** (same day)
```
after = {
    hidden_effect = {
        trigger_event = {
            id = feast_main_live_fowl.1001
            days = { 10 20 }  # 10-20 days later
        }
    }
}
```

**Method 2: Variable-Based Flags**
```
# Event 1 sets flag
set_variable = {
    name = last_feast_was
    value = flag:live_fowl
}

# Event 2 checks flag
trigger = {
    NOT = { scope:activity.activity_host.var:last_feast_was = flag:live_fowl }
}
```

**Method 3: Scheduled Follow-ups**
- Events schedule follow-up events days/weeks later
- Uses `trigger_event = { id = X days = Y }`
- Follow-up checks if conditions still apply

### 5. **Character-Specific Perspectives**

**Same event, different text for each role:**

**Host sees:**
> "A peacock erupts from the dish! Your guests shriek as [target] dives under the table."

**Target sees:**
> "The dish explodes! A live bird attacks you! Claws rake your face as you scramble away."

**Generic guest sees:**
> "A peacock bursts free! [Target] yelps as it attacks them. [Killer] leaps up and wrings its neck."

**Implementation:**
- Each character gets their own event ID
- Events reference `scope:target`, `scope:killer`, etc. to show other characters
- Outcomes apply to specific characters via scopes

### 6. **Odds and Weights in Practice**

**Trait-Modified Chances:**
```
ai_chance = {
    base = 25
    modifier = {
        has_trait = sadistic
        add = 75  # Now 100% chance
    }
    modifier = {
        has_trait = compassionate
        add = -50  # Now -25% chance (won't pick)
    }
}
```

**Random Character Selection with Filtering:**
```
random_attending_character = {
    limit = {
        has_trait = sadistic
        NOT = { has_trait = compassionate }
    }
    random = {
        chance = 50  # 50% chance this filtered character is picked
        save_scope_as = fowl_bird_killer
    }
}
```

**Outcome Probability Examples from CK3:**
- **15%** - Wound/injury from accident
- **25%** - Minor personality trait gain/loss
- **50%** - Character assignment to role in chain
- **85%** - Nothing happens (safe outcome)
- **100%** - Guaranteed outcome for specific traits

---

## Application to Enlisted

### Your Order Decision Chain Model

**Example: Patrol Duty → "Bodies in the Field" Decision Chain**

**1. Player browses Camp Hub during Patrol duty**
- Sees decision: "Strange Bodies Ahead"
- Description: "Your patrol spots several corpses on the road. Fresh. Could be bandits. Could be a trap."

**2. Player clicks decision → Setup event fires (hidden)**
```csharp
// Setup determines outcome before player sees first event
var roll = MBRandom.RandomFloat;
bool isTrap = roll < 0.5f; // 50% chance it's a trap
bool hasGold = roll >= 0.5f && roll < 0.8f; // 30% chance gold (50-80% range)
bool isEmpty = roll >= 0.8f; // 20% chance nothing

// Save outcome flags
player.SetVariable("patrol_bodies_outcome", isTrap ? "trap" : hasGold ? "gold" : "empty");
```

**3. First event fires with player choice**
```
Event: "Approaching the Bodies"
Desc: "You dismount and approach cautiously. The blood is fresh - hours old at most."

Options:
- "Search the bodies" [Scout 60: Notice tracks] → Chain Event 2a
- "Check the perimeter first" [Perception 40: Spot ambush] → Chain Event 2b  
- "It's a trap. Fall back." → End (safe retreat)
```

**4. Chain Event 2a - Player chose "Search bodies"**
```
IF patrol_bodies_outcome == "trap":
    Event: "Ambush!"
    Desc: "Crossbow bolts! Men burst from the treeline!"
    - Fight (combat) → lose HP, maybe kill bandits, find 30 gold
    - Flee → safe, but lose 5 Officer Rep
    
IF patrol_bodies_outcome == "gold":
    Event: "Hidden Cache"
    Desc: "You find a satchel sewn into the cloak. 50 gold pieces."
    - Keep it (+2 Scrutiny) → +50 gold
    - Report to officer (+8 Officer Rep) → +25 gold (half goes to company)
    
IF patrol_bodies_outcome == "empty":
    Event: "Nothing"
    Desc: "Refugees. Starved or fever. Nothing of value."
    - End (no rewards, time wasted)
```

**5. Skill checks modify outcomes**
```
IF player has Scout 60+:
    // During Event 1, add option:
    - "You spot fresh tracks leading to the trees" [Scout 60] → Detect trap early
    
IF player has Perception 40+ AND chose "Check perimeter":
    // Skip ambush, spot bandits first:
    Event: "You spot movement in the trees before they spring the trap"
    - Set up counter-ambush (+5 Tactics XP, find 50 gold, +10 Officer Rep)
```

### Probability Structure for Enlisted

**Decision → Event Chain Odds:**

| Outcome Type | Probability | Example |
|--------------|-------------|---------|
| **Skill check success** | 40-70% base + skill modifier | Scout 60: 70% base + (skill-60)*0.5% |
| **Minor injury/accident** | 10-20% | Sparring mishap, twisted ankle |
| **Major consequence** | 5-15% | Ambush, capture, troop loss |
| **Windfall** | 15-30% | Find gold, gain bonus XP |
| **Nothing/safe** | 30-50% | Routine completion, no drama |

**Skill Modifiers:**
- Every 10 skill points over requirement: +5% success chance
- Every 10 skill points under requirement: -10% success chance
- Critical success (skill 80+): +10% better rewards
- Critical fail (skill <30 on 60+ check): Double penalty

**Trait Modifiers (like CK3):**
- Bold/Brave: +25% chance to "aggressive" options
- Cautious: -25% chance to "risky" options  
- Lucky: +10% to positive outcomes
- Calculating: Better odds calculation shown to player

---

## Key Design Principles from CK3

1. **Player Always Initiates** - No random interrupts during normal gameplay. Events fire only during activities (feasts, hunts, pilgrimages) or from decisions.

2. **Clear Cause and Effect** - Player knows "I'm at a feast" or "I chose this decision" so events feel contextual, not random.

3. **Multiple Perspectives** - Same event shows differently to host vs. guest vs. target. Your system could show differently to grunt vs. officer.

4. **Probability Transparency** - CK3 often shows odds ("15% chance of injury"). Consider showing odds on risky decisions.

5. **Meaningful Failure** - Bad outcomes aren't just "lose HP." They create story: rivalries, reputational damage, chain consequences.

6. **Variable-Based State** - Use flags/variables to track what happened, enabling follow-up events days/weeks later.

7. **Cooldowns Prevent Repetition** - "last_feast_was" variable prevents same event firing twice in a row.

---

## Recommended Implementation for Enlisted

### Phase 1: Convert Order Events to Decision Chains

**Current:** OrderProgressionBehavior fires events randomly at slot phases
**New:** OrderProgressionBehavior makes decisions available in Camp Hub

```csharp
// During Guard Duty at Dusk phase
if (currentOrder.Id == "order_guard_post" && phase == "Dusk")
{
    // Make "Strange Noise" decision available
    DecisionCatalog.MakeDecisionAvailable("dec_guard_strange_noise");
}

// Player browses Camp Hub, sees:
// "Strange Noise from Perimeter" [Guard Duty]
// Click → Chain starts
```

### Phase 2: Remove Random Event Firing

Delete:
- `EventPacingManager.TryFireEvent()` (line 133-176)
- Keep threshold events (escalation, illness, supply pressure)
- Keep map incidents (contextual)

### Phase 3: Design Decision Chains

Each order gets 3-6 order-specific decisions:
- T1-T3 orders: Simple chains (1-2 events)
- T4-T6 orders: Medium chains (2-3 events)  
- T7-T9 orders: Complex chains (3-4 events with branching)

**Chain Design Template:**
```
Decision: [Situation during order]
  ↓
Event 1: [Setup with choice]
  ↓ (branches based on player choice)
Event 2a: [Consequence A] → Outcome
Event 2b: [Consequence B] → Outcome
Event 2c: [Consequence C] → Outcome
```

**Example chains:**
- Guard Duty → "Drunk Soldier" → Charm check → Success/Fail outcomes
- Patrol → "Bodies in Field" → Investigation → Trap/Gold/Nothing
- Scout Route → "Enemy Tracks" → Follow/Report/Ignore → Intel/Ambush/Safe

---

## Next Steps

1. **Audit current Order Events (84 total)** - Determine which convert to decision chains
2. **Design decision chain structure** - XML/JSON format for chains
3. **Implement chain system** - Support trigger_event with delays, variable-based branching
4. **Remove EventPacingManager.TryFireEvent()** - Delete random narrative event firing
5. **Test chains at 2x speed** - Ensure timing feels good with accelerated time

---

**Conclusion:** CK3's success comes from **activities** (player-initiated contexts) where events fire **during the activity**, creating chains of consequences from player choices. This is exactly what your orders provide - a contextual window where decision chains feel natural and empowering, not random and annoying.
