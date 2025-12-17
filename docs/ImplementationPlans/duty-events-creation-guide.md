# duty events creation guide

**purpose:** guide for creating engaging, recurring duty-based events that fire every 3-5 days while player performs their assigned duty role. events adapt to peace/wartime conditions and the lord's current actions, advancing player skills and adding drama without becoming annoying.

**last updated:** december 14, 2025  
**version:** 1.0  
**status:** active design document

---

## index

1. [System Overview](#system-overview)
2. [Event Timing & Frequency](#event-timing--frequency)
3. [Core Event Structure](#core-event-structure)
4. [Context-Aware Events](#context-aware-events)
5. [Duty-Specific Event Libraries](#duty-specific-event-libraries)
6. [Event Design Principles](#event-design-principles)
7. [Writing Guidelines](#writing-guidelines)
8. [Implementation Examples](#implementation-examples)

---

## System Overview

### What Are Duty Events?

Duty events are **mini-stories** that occur while the player is performing their assigned duty role (Quartermaster, Scout, Field Medic, etc.). They:

- **Fire automatically** every 3-5 days while on duty
- **Reflect current situation** (peace vs war, lord's actions)
- **Advance player progression** (skills, relationships, character)
- **Create meaningful choices** with real consequences
- **Stay engaging** without becoming repetitive or annoying

### Relationship to Schedule Block Events

**Important Distinction:**

**Duty Events (this guide):**
- Frequency: Every 3-5 days
- Scope: Role-specific (tied to player's **assigned duty role** like Quartermaster)
- Trigger: `has_duty:quartermaster`
- Purpose: Advance duty role skills and career progression
- Quantity: 15-20 per duty × 10 duties = 150-200 total

**Schedule Block Events (from AI Camp Schedule system):**
- Frequency: During each scheduled duty block (multiple per day)
- Scope: Task-specific (tied to **specific scheduled assignment** like "Guard Duty - North Gate")
- Trigger: `schedule_block:guard_duty`
- Purpose: Handle specific daily assignments
- Quantity: Many per block type

**Example: Quartermaster player on typical day:**
```
Morning Block: Guard Duty (from AI Schedule)
    → Schedule Block Event: "Sentry encounters rider" (fires during this block)

Afternoon Block: Free Time
    → Player visits Camp Hub, chooses activities

Evening Block: Inventory Check (from AI Schedule)
    → Duty Event: "Supplier Negotiation" (fires because player is Quartermaster, on 3-day cooldown)
```

**These systems are complementary:**
- Schedule Block Events = immediate daily tasks
- Duty Events = ongoing role development

**See:** `docs/Features/core-gameplay.md` (Schedule section) for where the schedule system lives in code/data.

### Integration with Existing Systems

```
Player Has Active Duty (e.g., Quartermaster)
        ↓
Every 3-5 Days Check
        ↓
Evaluate Context (War? Peace? Lord's Action?)
        ↓
Select Appropriate Event from Duty Pool
        ↓
Fire Event via Inquiry Channel
        ↓
Player Makes Choice
        ↓
Apply Consequences (XP, Heat, Discipline, Rep, etc.)
```

**Key Systems:**
- **Duties System** (`duties_system.json`) - 10 duty roles with skill focuses
- **Lance Life Events** - Existing event infrastructure
- **Escalation Tracks** - Heat, Discipline, Lance Rep, Medical Risk
- **Lord AI State** - Patrolling, Besieging, Resting, Traveling, At War, At Peace
- **Camp Schedule** - Context for when/where events occur

---

## Event Timing & Frequency

### Core Timing Rules

**Base Frequency:** Every **3 days minimum**, up to **5 days maximum**

```json
{
  "timing": {
    "cooldown_days": 3,
    "max_cooldown_days": 5,
    "priority": "normal",
    "one_time": false,
    "rate_limit": {
      "max_per_week": 2,
      "category_cooldown_days": 1
    }
  }
}
```

### Why 3-5 Days?

- **Not too frequent:** Avoids feeling spammy or annoying
- **Not too rare:** Keeps duty role feeling active and meaningful
- **Natural rhythm:** Aligns with pay muster cycle (12 days) = ~3 events per cycle
- **Allows variety:** 10+ events per duty × 3-5 day rotation = months before repetition

### Timing Modifiers

| Condition | Effect | Example |
|-----------|--------|---------|
| **Wartime** | Events fire more frequently (3 days) | Combat-related duties see more action |
| **Peacetime** | Events fire less frequently (4-5 days) | Training and logistics-focused |
| **Lord Preparing Battle** | Events fire next day (urgent) | "Scout enemy positions NOW" |
| **Lord Resting in Town** | Events delayed (5+ days) | Minimal duty activity |
| **High Lance Rep** | More interesting events | Get chosen for important tasks |
| **High Discipline** | More oversight events | Officers checking on you |

### Smart Cooldown System

```csharp
public int GetNextDutyEventCooldown(string dutyId, Hero player, MobileParty army)
{
    int baseCooldown = 3; // minimum
    
    // Context modifiers
    if (IsAtWar(army.MapFaction))
        baseCooldown = 3; // Frequent events during war
    else if (IsLordResting(army))
        baseCooldown = 5; // Rare events when resting
    else
        baseCooldown = 4; // Normal peacetime
    
    // Add small random variation (±1 day)
    int jitter = MBRandom.RandomInt(-1, 2);
    return Math.Clamp(baseCooldown + jitter, 3, 5);
}
```

**Result:** Events feel **organic and context-aware** rather than rigidly timed.

---

## Core Event Structure

### Duty Event Template

All duty events follow this JSON structure:

```json
{
  "id": "{duty}_{scenario}_{variant}",
  "category": "duty",
  "metadata": {
    "tier_range": { "min": 1, "max": 6 },
    "content_doc": "docs/ImplementationPlans/duty-events-creation-guide.md"
  },
  
  "delivery": {
    "method": "automatic",
    "channel": "inquiry",
    "incident_trigger": null,
    "menu": null
  },
  
  "triggers": {
    "all": [
      "is_enlisted",
      "ai_safe",
      "has_duty:{duty_id}"
    ],
    "any": [
      "weekly_tick",
      "entered_settlement",
      "before_battle"
    ],
    "time_of_day": ["morning", "afternoon", "dusk"],
    "escalation_requirements": {}
  },
  
  "requirements": {
    "duty": "{duty_id}",
    "formation": "any",
    "tier": { "min": 1, "max": 6 }
  },
  
  "timing": {
    "cooldown_days": 3,
    "priority": "normal",
    "one_time": false,
    "rate_limit": {
      "max_per_week": 2,
      "category_cooldown_days": 1
    }
  },
  
  "content": {
    "titleId": "ll_evt_{duty}_{scenario}_title",
    "setupId": "ll_evt_{duty}_{scenario}_setup",
    "title": "Brief Event Title",
    "setup": "Event description with context. Shows situation and asks for player decision.",
    
    "options": [
      {
        "id": "option_safe",
        "textId": "ll_evt_{duty}_{scenario}_opt_safe_text",
        "text": "Safe/By-the-book option",
        "tooltip": "What this choice means",
        "risk": "safe",
        "costs": {
          "fatigue": 2,
          "gold": 0
        },
        "rewards": {
          "xp": {
            "primary_skill": 30,
            "secondary_skill": 15
          },
          "gold": 0,
          "relation": {}
        },
        "effects": {
          "heat": 0,
          "discipline": 0,
          "lance_reputation": 2,
          "medical_risk": 0
        },
        "outcome": "Result text showing what happens"
      },
      {
        "id": "option_risky",
        "textId": "ll_evt_{duty}_{scenario}_opt_risky_text",
        "text": "Risky/Clever option",
        "tooltip": "Higher reward but has risk",
        "risk": "risky",
        "risk_chance": 0.3,
        "costs": {
          "fatigue": 3,
          "gold": 0
        },
        "rewards": {
          "xp": {
            "primary_skill": 50,
            "secondary_skill": 30
          },
          "gold": 0,
          "relation": {}
        },
        "effects": {
          "heat": 0,
          "discipline": 0,
          "lance_reputation": 5,
          "medical_risk": 0
        },
        "outcome": "Success result",
        "outcome_failure": "Failure result (if risky)",
        "injury_risk": {
          "chance": 0.1,
          "severity": "minor"
        }
      },
      {
        "id": "option_corrupt",
        "textId": "ll_evt_{duty}_{scenario}_opt_corrupt_text",
        "text": "Corrupt/Shortcut option",
        "tooltip": "Easy way out but consequences",
        "risk": "corrupt",
        "costs": {
          "fatigue": 1,
          "gold": 0
        },
        "rewards": {
          "xp": {
            "primary_skill": 10
          },
          "gold": 20
        },
        "effects": {
          "heat": 2,
          "discipline": 0,
          "lance_reputation": -5,
          "medical_risk": 0
        },
        "outcome": "Short-term gain, long-term problems"
      }
    ]
  }
}
```

### Three-Choice Philosophy

**Every duty event should offer three paths:**

1. **Safe/Professional** - By-the-book approach
   - ✅ Reliable XP gains
   - ✅ Small rep boost
   - ✅ No negative effects
   - ⚠️ Lower XP rewards
   - ⚠️ No material gains

2. **Risky/Clever** - Smart but dangerous approach
   - ✅ High XP rewards
   - ✅ Significant rep boost
   - ✅ Potential for great outcomes
   - ⚠️ Risk of failure (20-40% chance)
   - ⚠️ Higher fatigue cost
   - ⚠️ Possible injury

3. **Corrupt/Shortcut** - Easy way with consequences
   - ✅ Low effort (less fatigue)
   - ✅ Material gains (gold, items)
   - ⚠️ Adds Heat
   - ⚠️ Damages reputation
   - ⚠️ Minimal skill growth
   - ⚠️ Long-term problems

This structure ensures **player agency** while making consequences **clear and meaningful**.

---

## Context-Aware Events

### Event Variants by Context

Every duty should have multiple event variants that trigger based on the current situation:

### Lord's Action Contexts

**The lord's current state determines which event variants are available:**

| Lord State | Event Focus | Duty Priorities | Example Event |
|------------|-------------|-----------------|---------------|
| **Patrolling** | Border security, reconnaissance | Scout, Lookout, Messenger | "Spot enemy scouts on patrol" |
| **Besieging** | Siege operations, supply lines | Engineer, Quartermaster, Field Medic | "Manage supplies during long siege" |
| **Preparing Battle** | Combat readiness, intelligence | Scout, Armorer, Field Medic | "Gather last-minute intel on enemy" |
| **Traveling** | Movement, foraging, navigation | Navigator, Runner, Quartermaster | "Find route through difficult terrain" |
| **Resting (Town)** | Resupply, politics, diplomacy | Quartermaster, Messenger | "Negotiate with town merchants" |
| **Fleeing** | Survival, rear guard, morale | Scout, Runner, Field Medic | "Cover retreating wounded" |
| **At Peace** | Training, maintenance, politics | All duties (training focus) | "Teach new recruits your trade" |
| **At War** | Combat support, urgent tasks | All duties (combat focus) | "Urgent repairs before battle" |

### Peace vs War Event Variations

**Same duty role, different context = different events**

**Example: Quartermaster Duty**

**PEACETIME EVENT:**
```json
{
  "id": "qm_negotiate_supplier_peace",
  "triggers": {
    "all": ["is_enlisted", "has_duty:quartermaster", "at_peace"],
    "any": ["entered_settlement"]
  },
  "setup": "The local merchant is charging premium prices for grain. Your lord wants a fair deal, but the merchant knows we're not desperate.",
  "options": [
    {
      "text": "Negotiate professionally for fair price",
      "xp": { "trade": 40, "charm": 20 },
      "effects": { "heat": 0 }
    },
    {
      "text": "Use lord's name to pressure him",
      "xp": { "charm": 30, "intimidation": 20 },
      "effects": { "heat": 1, "discipline": 0 }
    },
    {
      "text": "Accept inflated prices (take cut for yourself)",
      "xp": { "trade": 10 },
      "rewards": { "gold": 50 },
      "effects": { "heat": 3, "lance_reputation": -10 }
    }
  ]
}
```

**WARTIME EVENT (Same Duty):**
```json
{
  "id": "qm_emergency_supplies_war",
  "triggers": {
    "all": ["is_enlisted", "has_duty:quartermaster", "at_war"],
    "any": ["before_battle", "army_moving"]
  },
  "setup": "Battle is tomorrow and you're short 40% on arrows. The armorer says he can't produce more in time. You need to find supplies fast or soldiers will go into battle under-equipped.",
  "options": [
    {
      "text": "Requisition from nearby allied force (takes time)",
      "xp": { "leadership": 40, "charm": 30 },
      "costs": { "time_hours": 6 },
      "effects": { "lance_reputation": 5 }
    },
    {
      "text": "Pay merchants triple to work through night",
      "xp": { "trade": 30 },
      "costs": { "gold": 300 },
      "effects": { "lance_reputation": 10 }
    },
    {
      "text": "\"Borrow\" from supply wagons of another unit",
      "xp": { "stealth": 40, "trade": 10 },
      "effects": { "heat": 4, "lance_reputation": -5 },
      "outcome": "Your lance is supplied. Someone else isn't. You don't want to think about that."
    }
  ]
}
```

**Notice the differences:**
- **Peacetime:** Slower pace, negotiation focus, relationship-building
- **Wartime:** Urgent pressure, moral dilemmas, higher stakes
- **Same duty, different challenges** = Keeps role fresh

---

## Duty-Specific Event Libraries

### 1. Quartermaster Duty Events

**Primary Skills:** Steward, Trade  
**Event Focus:** Supply management, logistics, negotiation, resource allocation

**Event Pool (10+ events minimum):**

#### Peace Events

**QM-P1: Supplier Negotiation**
- **Context:** Entered settlement, at peace
- **Challenge:** Merchant overcharging for supplies
- **Choices:** Negotiate / Intimidate / Skim funds
- **Skills:** Trade 40, Charm 20

**QM-P2: Inventory Discrepancy**
- **Context:** Weekly tick, in camp
- **Challenge:** Supplies don't match ledger
- **Choices:** Thorough audit / Adjust books / Blame someone else
- **Skills:** Steward 40, Trade 15

**QM-P3: Equipment Upgrade Request**
- **Context:** Lance member approaches
- **Challenge:** Soldier wants better equipment, budget is tight
- **Choices:** Approve request / Deny request / Find compromise
- **Skills:** Steward 30, Leadership 20

**QM-P4: Merchant Opportunity**
- **Context:** Entered settlement
- **Challenge:** Merchant offers deal on bulk goods
- **Choices:** Investigate deal / Accept / Negotiate better price
- **Skills:** Trade 50, Steward 20

#### War Events

**QM-W1: Emergency Resupply**
- **Context:** Before battle, at war
- **Challenge:** Critical shortage of combat supplies
- **Choices:** Requisition from allies / Pay premium / "Borrow" from others
- **Skills:** Leadership 40, Trade 30

**QM-W2: Looting Decision**
- **Context:** After battle victory
- **Challenge:** Battlefield has supplies, but taking them is questionable
- **Choices:** Official seizure / Quick grab / Leave it
- **Skills:** Steward 30, Ethics check
- **Heat Risk:** +2-4 depending on choice

**QM-W3: Rationing Crisis**
- **Context:** Army moving, supplies low
- **Challenge:** Need to ration food, soldiers won't like it
- **Choices:** Fair rationing / Favor your lance / Black market
- **Skills:** Leadership 40, Steward 25

**QM-W4: Supply Line Under Threat**
- **Context:** Patrolling, enemy nearby
- **Challenge:** Supply wagons need escort through dangerous area
- **Choices:** Request cavalry escort / Risk it / Take longer safe route
- **Skills:** Tactics 30, Leadership 25

#### Siege Events

**QM-S1: Long Siege Supplies**
- **Context:** Lord besieging
- **Challenge:** Supplies running low during extended siege
- **Choices:** Forage aggressively / Reduce rations / Request lord's support
- **Skills:** Steward 50, Leadership 30

**QM-S2: Defender Sortie**
- **Context:** Lord besieging
- **Challenge:** Defenders raid supply depot
- **Choices:** Sound alarm / Grab valuables first / Fight back
- **Skills:** Tactics 30, Athletics 20
- **Injury Risk:** 15%

### 2. Scout Duty Events

**Primary Skills:** Scouting, Tactics  
**Event Focus:** Reconnaissance, terrain, enemy intel, danger assessment

**Event Pool:**

#### Peace Events

**SC-P1: Terrain Mapping**
- **Context:** Army traveling, at peace
- **Challenge:** Chart best route through unfamiliar terrain
- **Choices:** Thorough survey / Ask locals / Quick estimate
- **Skills:** Scouting 40, Engineering 15

**SC-P2: Wildlife Encounter**
- **Context:** On patrol, forest/hills
- **Challenge:** Large predator or dangerous animal nearby
- **Choices:** Track and report / Avoid area / Attempt kill
- **Skills:** Scouting 35, Bow 30
- **Injury Risk:** 10% if fight

**SC-P3: Lost Patrol**
- **Context:** Patrol duty
- **Challenge:** Junior scout got separated, need to find them
- **Choices:** Systematic search / Quick backtrack / Wait at rendezvous
- **Skills:** Scouting 45, Leadership 20

#### War Events

**SC-W1: Enemy Scouts Spotted**
- **Context:** Patrolling, at war
- **Challenge:** Enemy scouts observing your position
- **Choices:** Report immediately / Shadow them / Capture one
- **Skills:** Scouting 50, Stealth 30, Tactics 25

**SC-W2: Ambush Site**
- **Context:** Before battle, army moving
- **Challenge:** Discover likely ambush position ahead
- **Choices:** Report and suggest counter / Investigate closer / Mark and avoid
- **Skills:** Tactics 50, Scouting 40

**SC-W3: Night Reconnaissance**
- **Context:** Enemy nearby, night
- **Challenge:** Need to scout enemy camp in darkness
- **Choices:** Cautious approach / Bold close look / Use diversion
- **Skills:** Scouting 50, Stealth 40
- **Injury Risk:** 25%
- **Capture Risk:** 15%

**SC-W4: Friendly Force Contact**
- **Context:** Patrolling, army moving
- **Challenge:** Spot what might be friendly or enemy force
- **Choices:** Approach carefully / Signal / Observe from distance
- **Skills:** Scouting 40, Tactics 30

### 3. Field Medic Duty Events

**Primary Skills:** Medicine, Athletics  
**Event Focus:** Treating wounded, triage, medical ethics, disease prevention

**Event Pool:**

#### Peace Events

**MED-P1: Training Accident**
- **Context:** Training day, in camp
- **Challenge:** Soldier injured during drill, needs treatment
- **Choices:** Basic treatment / Thorough exam / Send to surgeon
- **Skills:** Medicine 40, Athletics 15

**MED-P2: Illness Outbreak**
- **Context:** Weekly tick, in camp or settlement
- **Challenge:** Multiple soldiers showing symptoms, might spread
- **Choices:** Quarantine / Treat individually / Preventative measures
- **Skills:** Medicine 50, Leadership 25

**MED-P3: Medical Supply Check**
- **Context:** Weekly tick
- **Challenge:** Inventory supplies, some are degraded
- **Choices:** Replace all / Salvage what's good / Request emergency funds
- **Skills:** Medicine 35, Steward 20

#### War Events

**MED-W1: Battlefield Triage**
- **Context:** After battle
- **Challenge:** Many wounded, must prioritize who to treat first
- **Choices:** Save most critical / Treat most saveable / Favor your lance
- **Skills:** Medicine 60, Leadership 30
- **Moral Weight:** Heavy

**MED-W2: Wounded Enemy**
- **Context:** After battle
- **Challenge:** Enemy soldier badly wounded, begging for help
- **Choices:** Treat him / Minimal care / Refuse (save supplies)
- **Skills:** Medicine 40, Ethics check
- **Lance Rep:** +10 (treat) or -5 (refuse)

**MED-W3: Emergency Surgery**
- **Context:** After battle, urgent
- **Challenge:** Lance mate needs immediate surgery, no surgeon present
- **Choices:** Attempt surgery / Stabilize only / Find surgeon fast
- **Skills:** Medicine 70, Athletics 20
- **Failure Risk:** 30% (patient dies)
- **Success:** +30 Lance Rep, saved a life

**MED-W4: Disease in Wartime**
- **Context:** Army moving, at war
- **Challenge:** Soldier has contagious disease, army can't stop
- **Choices:** Quarantine in wagon / Send home / Keep fighting
- **Skills:** Medicine 50, Leadership 20

### 4. Armorer Duty Events

**Primary Skills:** Smithing, Engineering  
**Event Focus:** Equipment maintenance, repairs, crafting, quality control

**Event Pool:**

#### Peace Events

**ARM-P1: Equipment Maintenance**
- **Context:** Weekly tick, in camp
- **Challenge:** Lance equipment needs routine maintenance
- **Choices:** Thorough overhaul / Quick repairs / Focus on priorities
- **Skills:** Smithing 40, Engineering 20

**ARM-P2: Forge Accident**
- **Context:** At forge
- **Challenge:** Fire flares up, someone might get burned
- **Choices:** Emergency response / Evacuate / Control it yourself
- **Skills:** Athletics 30, Engineering 25
- **Injury Risk:** 20%

**ARM-P3: Custom Request**
- **Context:** Lance member wants modification
- **Challenge:** Soldier wants custom work on their gear
- **Choices:** Do quality work / Rush it / Charge them gold
- **Skills:** Smithing 50, Charm 15

#### War Events

**ARM-W1: Pre-Battle Rush**
- **Context:** Before battle, urgent
- **Challenge:** Dozen soldiers need repairs before battle in 2 hours
- **Choices:** Rush all / Prioritize critical / Call for help
- **Skills:** Smithing 50, Leadership 25

**ARM-W2: Equipment Failure Analysis**
- **Context:** After battle
- **Challenge:** Multiple shields broke during combat, might be faulty
- **Choices:** Investigate thoroughly / Quick patch / Blame supplier
- **Skills:** Engineering 45, Smithing 30

**ARM-W3: Salvage Operation**
- **Context:** After battle victory
- **Challenge:** Battlefield has enemy equipment, some valuable
- **Choices:** Official salvage / Quick grab / Leave it
- **Skills:** Smithing 40, Trade 25
- **Heat Risk:** +2 if unofficial

### 5. Scout (Formation) Duty Events

*See Scout Duty above - same role*

### 6. Runner Duty Events

**Primary Skills:** Athletics, Scouting  
**Event Focus:** Delivering messages, errands, camp navigation, endurance

**Event Pool:**

#### Peace Events

**RUN-P1: Message Delivery**
- **Context:** In camp or settlement
- **Challenge:** Deliver message to officer in different section
- **Choices:** Direct route / Check message / Ask for tip
- **Skills:** Athletics 30, Charm 10

**RUN-P2: Lost Package**
- **Context:** Weekly tick
- **Challenge:** Package you were carrying has gone missing
- **Choices:** Search thoroughly / Blame someone / Cover it up
- **Skills:** Scouting 30, Charm 20
- **Heat Risk:** +2 if cover up

**RUN-P3: Urgent Errand**
- **Context:** Officer summons
- **Challenge:** Fetch item from town quickly, officer waiting
- **Choices:** Sprint there / Delegate / Shortcut through camp
- **Skills:** Athletics 45, Leadership 15

#### War Events

**RUN-W1: Battlefield Message**
- **Context:** During battle
- **Challenge:** Carry orders to unit under fire
- **Choices:** Run fast / Stay low and careful / Find safer route
- **Skills:** Athletics 60, Tactics 30
- **Injury Risk:** 30%

**RUN-W2: Intercepted Message**
- **Context:** Army moving, at war
- **Challenge:** Found enemy message on dead rider
- **Choices:** Report immediately / Read it first / Pocket it
- **Skills:** Athletics 30, Tactics 40
- **Heat Risk:** +3 if pocket

**RUN-W3: Exhaustion Test**
- **Context:** Army marching hard
- **Challenge:** Multiple urgent runs, you're exhausted
- **Choices:** Push through / Ask for relief / Half-ass it
- **Skills:** Athletics 50, Leadership 20
- **Medical Risk:** +1 if push

### 7. Engineer Duty Events

**Primary Skills:** Engineering, Athletics  
**Event Focus:** Construction, fortifications, siege engines, problem-solving

**Event Pool:**

#### Peace Events

**ENG-P1: Fortification Project**
- **Context:** In camp, at peace
- **Challenge:** Build camp fortifications, limited materials
- **Choices:** Efficient design / Robust but slow / Quick and dirty
- **Skills:** Engineering 50, Leadership 25

**ENG-P2: Bridge Repair**
- **Context:** Army traveling
- **Challenge:** Bridge damaged, need to repair for army passage
- **Choices:** Thorough rebuild / Quick fix / Find alternate route
- **Skills:** Engineering 55, Athletics 25

**ENG-P3: Tool Inventory**
- **Context:** Weekly tick
- **Challenge:** Tools are worn out, need replacements
- **Choices:** Request new tools / Improvise repairs / Borrow from town
- **Skills:** Engineering 35, Steward 20

#### Siege Events

**ENG-S1: Siege Engine Construction**
- **Context:** Lord besieging
- **Challenge:** Build trebuchet with limited materials
- **Choices:** Optimal design / Fast assembly / Experimental design
- **Skills:** Engineering 60, Mathematics 30

**ENG-S2: Undermining Operation**
- **Context:** Lord besieging
- **Challenge:** Dig tunnel under walls, risk of collapse
- **Choices:** Cautious digging / Aggressive pace / Shore up supports
- **Skills:** Engineering 55, Athletics 30
- **Injury Risk:** 25%

**ENG-S3: Siege Engine Sabotage**
- **Context:** Lord besieging
- **Challenge:** Defenders damaged your trebuchet overnight
- **Choices:** Repair quickly / Investigate saboteur / Build new one
- **Skills:** Engineering 50, Tactics 25

### 8. Lookout Duty Events

**Primary Skills:** Scouting, Bow, Athletics  
**Event Focus:** Watch duty, observation, signal fires, camp security

**Event Pool:**

#### Peace Events

**LOOK-P1: Watch Duty Boredom**
- **Context:** On watch, long shift
- **Challenge:** Nothing happening, fighting to stay alert
- **Choices:** Stay focused / Distract yourself safely / Slack off
- **Skills:** Athletics 30, Discipline check

**LOOK-P2: Signal Fire Training**
- **Context:** Training day
- **Challenge:** Practice signal fire protocols
- **Choices:** Take it seriously / Minimal effort / Show off
- **Skills:** Scouting 35, Leadership 15

**LOOK-P3: Wildlife Spotted**
- **Context:** On watch
- **Challenge:** Large animal approaching camp
- **Choices:** Sound alarm / Just watch / Try to scare off
- **Skills:** Scouting 30, Bow 20

#### War Events

**LOOK-W1: Enemy Scouts at Distance**
- **Context:** On watch, at war
- **Challenge:** Spot enemy scouts observing your position
- **Choices:** Alert command / Track their movement / Sound general alarm
- **Skills:** Scouting 50, Tactics 30

**LOOK-W2: Night Attack Warning**
- **Context:** Night watch, enemy nearby
- **Challenge:** Movement in darkness, might be attack
- **Choices:** Sound alarm immediately / Verify first / Light signal fire
- **Skills:** Scouting 55, Bow 30, Tactics 25

**LOOK-W3: Friendly Fire Risk**
- **Context:** On watch, chaotic situation
- **Challenge:** Approaching group, unclear if friendly or hostile
- **Choices:** Challenge them / Sound alarm / Hold position
- **Skills:** Scouting 40, Tactics 35

### 9. Messenger (Cavalry) Duty Events

**Primary Skills:** Riding, Athletics, Charm  
**Event Focus:** Urgent dispatches, diplomacy, navigation, speed

**Event Pool:**

#### Peace Events

**MSG-P1: Diplomatic Delivery**
- **Context:** Entered settlement
- **Challenge:** Deliver diplomatic message to neutral lord
- **Choices:** Formal protocol / Personal approach / Rush delivery
- **Skills:** Charm 40, Leadership 25

**MSG-P2: Ambiguous Orders**
- **Context:** Received message to deliver
- **Challenge:** Orders seem contradictory, unclear what to do
- **Choices:** Ask for clarification / Use judgment / Deliver as-is
- **Skills:** Leadership 35, Charm 20

**MSG-P3: Message for Enemy?**
- **Context:** Army moving
- **Challenge:** Asked to carry message to someone who might be spy
- **Choices:** Report suspicions / Deliver message / Read it first
- **Skills:** Tactics 30, Charm 20
- **Heat Risk:** +2 if read message

#### War Events

**MSG-W1: Under Fire Delivery**
- **Context:** During battle or near battle
- **Challenge:** Must deliver orders to unit under enemy fire
- **Choices:** Ride fast / Use cover / Find alternate route
- **Skills:** Riding 60, Tactics 35
- **Injury Risk:** 35%

**MSG-W2: Captured Enemy Dispatch**
- **Context:** After battle
- **Challenge:** Find orders on enemy messenger, valuable intel
- **Choices:** Report immediately / Read first / Pocket for reward
- **Skills:** Tactics 45, Charm 25
- **Heat Risk:** +3 if pocket

**MSG-W3: Exhausted Horse**
- **Context:** Urgent delivery needed
- **Challenge:** Horse is exhausted, but message is urgent
- **Choices:** Push horse (risk injury) / Switch mounts / Delay
- **Skills:** Riding 45, Animal Care 25

### 10. Boatswain Duty Events (Naval)

**Primary Skills:** Athletics, Leadership  
**Event Focus:** Ship operations, crew management, deck work, weather

**Event Pool:**

#### Peace Events

**BOAT-P1: Crew Discipline**
- **Context:** At sea, routine sail
- **Challenge:** Sailor slacking on duties
- **Choices:** Report to officer / Handle it yourself / Ignore it
- **Skills:** Leadership 35, Athletics 20

**BOAT-P2: Deck Maintenance**
- **Context:** At sea or port
- **Challenge:** Ship needs maintenance, crew is tired
- **Choices:** Thorough work / Minimum viable / Motivate crew
- **Skills:** Athletics 35, Leadership 30

**BOAT-P3: Supply Loading**
- **Context:** In port
- **Challenge:** Supervise cargo loading, time pressure
- **Choices:** Fast but risky / Slow and careful / Delegate fully
- **Skills:** Athletics 30, Leadership 25

#### War Events

**BOAT-W1: Storm in Combat**
- **Context:** Naval battle, bad weather
- **Challenge:** Manage crew during storm and battle
- **Choices:** Priority on sailing / Priority on combat / Balance both
- **Skills:** Athletics 50, Leadership 45

**BOAT-W2: Boarding Action**
- **Context:** Naval battle
- **Challenge:** Enemy attempting to board, coordinate defense
- **Choices:** Lead defense / Organize crew / Cut grapples
- **Skills:** Leadership 45, One-Handed 35
- **Injury Risk:** 40%

**BOAT-W3: Ship Damage Control**
- **Context:** After naval battle
- **Challenge:** Ship taking water, need emergency repairs
- **Choices:** Quick patch / Organize repair crew / Abandon cargo
- **Skills:** Athletics 55, Engineering 25

### 11. Navigator Duty Events (Naval)

**Primary Skills:** Scouting, Engineering  
**Event Focus:** Navigation, weather reading, course plotting, astronomy

**Event Pool:**

#### Peace Events

**NAV-P1: Course Plotting**
- **Context:** At sea, routine sail
- **Challenge:** Plot optimal course to destination
- **Choices:** Safe coastal / Direct open sea / Experimental route
- **Skills:** Scouting 50, Engineering 30

**NAV-P2: Weather Prediction**
- **Context:** At sea
- **Challenge:** Storm signs appearing, captain asks advice
- **Choices:** Warn to change course / Say it'll pass / Unsure, be honest
- **Skills:** Scouting 45, Leadership 20

**NAV-P3: Star Navigation Training**
- **Context:** Night at sea
- **Challenge:** Teaching junior sailor navigation
- **Choices:** Thorough lesson / Quick basics / Dismiss them
- **Skills:** Scouting 40, Leadership 25

#### War Events

**NAV-W1: Enemy Fleet Evasion**
- **Context:** At war, enemy fleet spotted
- **Challenge:** Plot escape route quickly
- **Choices:** Bold dash / Cautious coast route / Island hiding
- **Skills:** Scouting 60, Tactics 40

**NAV-W2: Night Approach**
- **Context:** Naval warfare
- **Challenge:** Navigate to enemy position in darkness, no lights
- **Choices:** Dead reckoning / Star navigation / Follow coastline
- **Skills:** Scouting 65, Engineering 35

**NAV-W3: Uncharted Waters**
- **Context:** War, pursuing enemy
- **Challenge:** Following enemy into unknown waters
- **Choices:** Follow cautiously / Return to known waters / Full speed ahead
- **Skills:** Scouting 55, Tactics 30
- **Ship Damage Risk:** 25%

---

## Event Design Principles

### 1. Player Agency Matters

**DO:** Give meaningful choices with clear trade-offs  
**DON'T:** Force single-path outcomes or fake choices

**Example (Good):**
```
Challenge: Supply shortage before battle
Option A: Requisition from allies (delays battle, reliable)
Option B: Buy at premium (costs gold, immediate)
Option C: "Borrow" without permission (Heat risk, immediate)

All three work. Each has different costs/benefits.
```

**Example (Bad):**
```
Challenge: Supply shortage
Option A: Find supplies (only real option)
Option B: Don't find supplies (obviously bad, no one picks this)

Player has no real choice.
```

### 2. Consequences Must Be Real

**Every choice should have:**
- **Immediate Effect:** XP gain, resource change, time cost
- **System Impact:** Heat/Discipline/Rep/Medical Risk/Fatigue
- **Narrative Weight:** Outcome text that acknowledges choice

**Example:**
```
Choice: "Report corruption in supply chain"
Immediate: +40 Leadership XP, +20 Charm XP
System: Heat -3 (cleaning up), Lance Rep +10 (respected)
Narrative: "The quartermaster is dismissed. The lord thanks you 
           personally. Your lance mates nod with approval—they knew 
           something was wrong."

vs.

Choice: "Ignore corruption, take a cut"
Immediate: +10 Trade XP, +100 Gold
System: Heat +4 (complicit), Lance Rep -15 (sellout)
Narrative: "You pocket the gold. Easy money. But you catch {LANCE_MATE} 
           watching you with cold eyes. They know."
```

### 3. Context Creates Drama

**Events should reference:**
- Lord's current objective
- Recent battles or losses
- Lance member names (when relevant)
- Army morale and conditions
- Player's rank/tier progression

**Example (Contextual):**
```
"Your lance is already down two men from the last battle. {LANCE_LEADER_SHORT} 
looks exhausted. Now the lord wants us on night patrol again. You're 
the quartermaster—the supplies won't manage themselves. But if you don't 
go on patrol, who will cover your spot?"
```

vs.

**Example (Generic):**
```
"You have duties to perform. Choose what to do."
```

### 4. Vary Tone and Stakes

**Not every event should be life-or-death.**

**Event Tone Mix (Per Duty):**
- 30% **Low Stakes**: Routine tasks, skill checks, minor problems
- 40% **Medium Stakes**: Meaningful choices, moderate consequences
- 20% **High Stakes**: Moral dilemmas, major consequences, danger
- 10% **Character Moments**: Social interactions, personality choices

**Low Stakes Example:**
```
"The forge is running low on coal. You need to requisition more from 
the quartermaster. He's in a bad mood today."

[Polite request] [Trade favors] [Pull rank]
```

**High Stakes Example:**
```
"The soldier on your operating table is dying. You're not a trained 
surgeon, but the real surgeon won't make it in time. If you try the 
surgery yourself, you might save him... or kill him faster."

[Attempt surgery] [Stabilize only] [Comfort him as he dies]
```

### 5. Reward Creativity and Role-Play

**Include options that let players express character:**

- Honorable choice (for players building reputation)
- Pragmatic choice (for players focused on efficiency)
- Rebellious choice (for players pushing boundaries)
- Corrupt choice (for players on Heat path)

**Example:**
```
Challenge: Lord orders you to falsify supply records to hide waste

Honorable: "I won't do it. I'll report the waste honestly."
  → +30 Lance Rep, -10 Lord Relation, +Heat 0

Pragmatic: "I'll present the facts diplomatically, minimize the damage."
  → +15 Lance Rep, +0 Lord Relation, +Heat 0

Rebellious: "I'll tell him exactly how incompetent his logistics are."
  → +10 Lance Rep, -20 Lord Relation, +Discipline 2

Corrupt: "I'll falsify the records and skim extra for myself."
  → -20 Lance Rep, +10 Lord Relation, +Heat 4, +Gold 150
```

### 6. Use Placeholder System

**Leverage existing placeholder system for immersion:**

```json
{
  "setup": "{LANCE_LEADER_SHORT} pulls you aside during evening muster. 
           '{PLAYER_NAME}, we have a problem. {LANCE_MATE_1} hasn't been 
           seen since this morning. {LORD_SHORT_TITLE} is asking questions.'",
  
  "outcome": "You find {LANCE_MATE_1} passed out drunk behind the supply 
              wagons. {LANCE_LEADER_SHORT} looks at you expectantly. What you 
              do next will matter."
}
```

**Available Placeholders:**
- `{PLAYER_NAME}` - Player's character name
- `{PLAYER_SHORT}` - Short form (first name or nickname)
- `{LORD_NAME}` - Lord's full name
- `{LORD_SHORT}` - Lord's title + surname
- `{LORD_SHORT_TITLE}` - Just the title
- `{LANCE_NAME}` - Lance unit name
- `{LANCE_LEADER_NAME}` - Lance leader full name
- `{LANCE_LEADER_SHORT}` - Lance leader short form
- `{LANCE_MATE}` / `{LANCE_MATE_1}` - Random lance member
- `{CULTURE}` - Player's culture (Vlandian, Battanian, etc.)

### 7. Fail Forward When Possible

**Risky choices should have interesting failures, not game-overs.**

**Example (Good Failure):**
```
Risk: Attempt field surgery (30% fail rate)

Success: "Your hands shake but hold steady. Against all odds, you save him. 
          {LANCE_MATE} will live to fight another day. +60 Medicine XP, 
          +30 Lance Rep"

Failure: "You try your best, but it's not enough. {LANCE_MATE} dies on 
          the table. His last words: 'Not your fault.' The surgeon arrives 
          three minutes too late. +20 Medicine XP, -10 Lance Rep (they 
          know you tried), +Medical Risk 1 (guilt)"
```

**Example (Bad Failure):**
```
Risk: Attempt field surgery

Success: Patient lives
Failure: Patient dies, -100 Lance Rep, +10 Discipline, kicked from lance

[Too punishing, discourages risk-taking]
```

---

## Writing Guidelines

### Voice and Tone

**Style:** Gritty military realism with human moments

**DO:**
- ✅ Use military vocabulary naturally (muster, formation, requisition)
- ✅ Show physical details (tired eyes, muddy boots, bloody bandages)
- ✅ Include soldier banter and dark humor
- ✅ Reference weather, time of day, physical conditions
- ✅ Keep it brief (2-3 paragraphs max)

**DON'T:**
- ❌ Be overly formal or Shakespearean
- ❌ Use modern slang or anachronisms
- ❌ Write long exposition dumps
- ❌ Explain mechanics in-character ("This will cost you 3 fatigue")
- ❌ Break immersion with meta-references

**Example (Good Voice):**
```
The supply wagons reek of spoiled grain. You're three days from the 
nearest town and the men are already grumbling about short rations. 
{LANCE_LEADER_SHORT} wants an explanation. You've got two choices: 
admit someone's been skimming, or blame the heat.
```

**Example (Bad Voice):**
```
Greetings, Quartermaster! It appears thy supplies have been compromised 
by nefarious forces! Prithee, wouldst thou investigate this matter 
posthaste, lest the men grow wroth?
```

### Option Text Format

**Option Text Structure:**
```
[Action Type] "Direct player speech or action description"
```

**Action Types:**
- `[Report]`, `[Investigate]`, `[Help]`, `[Refuse]`
- `[Negotiate]`, `[Intimidate]`, `[Fight]`, `[Flee]`
- `[Safe]`, `[Risky]`, `[Corrupt]`
- `[Accept]`, `[Decline]`, `[Suggest Alternative]`

**Examples:**
```
"[Investigate] Check the wagons personally before reporting"
"[Report] Tell {LANCE_LEADER_SHORT} immediately"
"[Blame] \"It's the suppliers' fault. They sold us bad grain.\""
"[Admit] \"Someone's been skimming. I'll find out who.\""
```

### Outcome Text Guidelines

**Structure:** Consequence → Immediate Result → Future Implication

**Example:**
```
You pull {LANCE_MATE} from the collapsed trench. His leg is badly 
broken, but he'll live. {LANCE_LEADER_SHORT} claps your shoulder. 
"Good work." The others will remember this.

[Immediate: Saved life]
[Result: Recognition from leader]
[Future: Reputation boost]
```

### Length Guidelines

| Section | Length | Notes |
|---------|--------|-------|
| **Event Title** | 3-5 words | "Emergency Surgery", "Enemy Scouts", "Supply Theft" |
| **Setup Text** | 2-3 paragraphs | Set scene, present problem, end with question/tension |
| **Option Text** | 5-12 words | Clear action + brief context |
| **Tooltip** | 1 sentence | Explain mechanical effect or risk |
| **Outcome Text** | 1-2 paragraphs | Show result, apply consequences, hint at future |
| **Outcome Failure** | 1-2 paragraphs | Show what went wrong, silver lining if possible |

---

## Implementation Examples

### Complete Event: Quartermaster Peace Event

```json
{
  "id": "qm_merchant_premium_peace",
  "category": "duty",
  "metadata": {
    "tier_range": { "min": 1, "max": 6 },
    "content_doc": "docs/ImplementationPlans/duty-events-creation-guide.md",
    "author": "Enlisted Team",
    "date_created": "2025-12-14"
  },
  
  "delivery": {
    "method": "automatic",
    "channel": "inquiry",
    "incident_trigger": null,
    "menu": null,
    "menu_section": null
  },
  
  "triggers": {
    "all": [
      "is_enlisted",
      "ai_safe",
      "has_duty:quartermaster"
    ],
    "any": [
      "entered_settlement",
      "weekly_tick"
    ],
    "time_of_day": ["morning", "afternoon"],
    "escalation_requirements": {
      "at_peace": true
    }
  },
  
  "requirements": {
    "duty": "quartermaster",
    "formation": "any",
    "tier": { "min": 1, "max": 6 }
  },
  
  "timing": {
    "cooldown_days": 3,
    "priority": "normal",
    "one_time": false,
    "rate_limit": {
      "max_per_week": 2,
      "category_cooldown_days": 1
    }
  },
  
  "content": {
    "titleId": "ll_evt_qm_merchant_premium_peace_title",
    "setupId": "ll_evt_qm_merchant_premium_peace_setup",
    "title": "Merchant Negotiation",
    "setup": "The local grain merchant greets you with a calculated smile. His prices are twenty percent above market rate, and he knows it.\n\n\"Supply and demand, friend. Everyone needs to eat.\" He leans back, confident. Your lord expects a good deal, but this man knows you have limited options.\n\nThe wagons are half-empty. You need this grain.",
    
    "options": [
      {
        "id": "negotiate_professionally",
        "textId": "ll_evt_qm_merchant_premium_peace_opt_negotiate_text",
        "text": "[Negotiate] \"Let's talk about a fair price for both of us.\"",
        "tooltip": "Professional negotiation, build relationship",
        "condition": null,
        "risk": "safe",
        "risk_chance": null,
        
        "costs": {
          "fatigue": 2,
          "gold": 0,
          "time_hours": 0
        },
        
        "rewards": {
          "xp": {
            "trade": 40,
            "charm": 20
          },
          "gold": 0,
          "relation": {
            "merchant": 5
          },
          "items": []
        },
        
        "effects": {
          "heat": 0,
          "discipline": 0,
          "lance_reputation": 2,
          "medical_risk": 0,
          "fatigue_relief": 0
        },
        
        "flags_set": ["merchant_relation_fair"],
        "flags_clear": [],
        
        "resultTextId": "ll_evt_qm_merchant_premium_peace_opt_negotiate_outcome",
        "outcome": "You spend an hour talking through the numbers. He respects your knowledge of trade routes and seasonal prices. Eventually he agrees to twelve percent above market.\n\nNot perfect, but fair. {LANCE_LEADER_SHORT} nods approval when you report the deal. \"Good work. We'll remember this merchant.\"",
        "outcome_failure": null,
        "injury_risk": null,
        "triggers_event": null,
        "advances_onboarding": false
      },
      
      {
        "id": "use_lords_name",
        "textId": "ll_evt_qm_merchant_premium_peace_opt_pressure_text",
        "text": "[Pressure] \"{LORD_SHORT_TITLE} won't appreciate you gouging his army.\"",
        "tooltip": "Use lord's authority to force better price",
        "condition": null,
        "risk": "risky",
        "risk_chance": 0.25,
        
        "costs": {
          "fatigue": 1,
          "gold": 0,
          "time_hours": 0
        },
        
        "rewards": {
          "xp": {
            "charm": 30,
            "intimidation": 20
          },
          "gold": 0,
          "relation": {
            "merchant": -10
          },
          "items": []
        },
        
        "effects": {
          "heat": 1,
          "discipline": 0,
          "lance_reputation": 0,
          "medical_risk": 0,
          "fatigue_relief": 0
        },
        
        "flags_set": ["merchant_intimidated"],
        "flags_clear": [],
        
        "resultTextId": "ll_evt_qm_merchant_premium_peace_opt_pressure_outcome",
        "outcome": "His smile vanishes. He's afraid of offending your lord, but he'll remember this.\n\n\"Five percent above market. That's my final offer.\" He practically spits the words.\n\nYou got a better deal, but you've made an enemy. Word gets around in merchant circles.",
        
        "outcome_failure": "His expression hardens. \"Lord {LORD_SHORT} is a fine man, but he doesn't control MY shop. Twenty-five percent above market now. Take it or leave it.\"\n\nYou pushed too hard. The price went up instead of down. {LANCE_LEADER_SHORT} is not pleased with your report.",
        
        "injury_risk": null,
        "triggers_event": null,
        "advances_onboarding": false
      },
      
      {
        "id": "skim_the_difference",
        "textId": "ll_evt_qm_merchant_premium_peace_opt_corrupt_text",
        "text": "[Corrupt] \"Agree to his price, pad the ledger, split the difference.\"",
        "tooltip": "Corruption: Easy profit, but Heat risk",
        "condition": null,
        "risk": "corrupt",
        "risk_chance": null,
        
        "costs": {
          "fatigue": 1,
          "gold": 0,
          "time_hours": 0
        },
        
        "rewards": {
          "xp": {
            "trade": 10,
            "roguery": 20
          },
          "gold": 75,
          "relation": {
            "merchant": 10
          },
          "items": []
        },
        
        "effects": {
          "heat": 3,
          "discipline": 0,
          "lance_reputation": -10,
          "medical_risk": 0,
          "fatigue_relief": 0
        },
        
        "flags_set": ["merchant_corrupt_deal", "corruption_active"],
        "flags_clear": [],
        
        "resultTextId": "ll_evt_qm_merchant_premium_peace_opt_corrupt_outcome",
        "outcome": "The merchant understands immediately. \"A man who knows how business really works.\" He counts out your share—seventy-five gold.\n\nEasy money. The ledger shows the inflated price, your lord pays it, everyone's happy. Except {LANCE_MATE} saw you pocketing the gold. Their expression is unreadable.\n\nYou're in the game now. But games have consequences.",
        
        "outcome_failure": null,
        "injury_risk": null,
        "triggers_event": null,
        "advances_onboarding": false
      },
      
      {
        "id": "find_alternative_supplier",
        "textId": "ll_evt_qm_merchant_premium_peace_opt_alternative_text",
        "text": "[Alternative] \"I'll find another supplier. Good day.\"",
        "tooltip": "Walk away, find better deal elsewhere",
        "condition": null,
        "risk": "safe",
        "risk_chance": null,
        
        "costs": {
          "fatigue": 3,
          "gold": 0,
          "time_hours": 4
        },
        
        "rewards": {
          "xp": {
            "trade": 50,
            "leadership": 25
          },
          "gold": 0,
          "relation": {},
          "items": []
        },
        
        "effects": {
          "heat": 0,
          "discipline": 0,
          "lance_reputation": 5,
          "medical_risk": 0,
          "fatigue_relief": 0
        },
        
        "flags_set": ["found_better_supplier"],
        "flags_clear": [],
        
        "resultTextId": "ll_evt_qm_merchant_premium_peace_opt_alternative_outcome",
        "outcome": "You spend the afternoon talking to other merchants and farmers. It takes time and effort, but you find a miller on the town's edge willing to sell at fair market price.\n\nHe's grateful for the army's business. The first merchant watches from his shop door, fuming.\n\n{LANCE_LEADER_SHORT} is impressed. \"Initiative. Good. That's how you advance.\"",
        
        "outcome_failure": null,
        "injury_risk": null,
        "triggers_event": null,
        "advances_onboarding": false
      }
    ]
  }
}
```

### Complete Event: Scout Wartime Event

```json
{
  "id": "scout_enemy_recon_war",
  "category": "duty",
  "metadata": {
    "tier_range": { "min": 2, "max": 6 },
    "content_doc": "docs/ImplementationPlans/duty-events-creation-guide.md",
    "author": "Enlisted Team",
    "date_created": "2025-12-14"
  },
  
  "delivery": {
    "method": "automatic",
    "channel": "inquiry",
    "incident_trigger": null,
    "menu": null,
    "menu_section": null
  },
  
  "triggers": {
    "all": [
      "is_enlisted",
      "ai_safe",
      "has_duty:scout"
    ],
    "any": [
      "before_battle",
      "army_moving",
      "lord_patrolling"
    ],
    "time_of_day": ["dawn", "dusk"],
    "escalation_requirements": {
      "at_war": true
    }
  },
  
  "requirements": {
    "duty": "scout",
    "formation": "any",
    "tier": { "min": 2, "max": 6 }
  },
  
  "timing": {
    "cooldown_days": 3,
    "priority": "high",
    "one_time": false,
    "rate_limit": {
      "max_per_week": 3,
      "category_cooldown_days": 1
    }
  },
  
  "content": {
    "titleId": "ll_evt_scout_enemy_recon_war_title",
    "setupId": "ll_evt_scout_enemy_recon_war_setup",
    "title": "Enemy Camp Spotted",
    "setup": "{LANCE_LEADER_SHORT} wakes you before dawn. \"Scout duty. Enemy army is close—we need to know their strength and position before the lord makes his move.\"\n\nYou ride out alone as grey light touches the hills. Three hours later, you spot them: enemy camp in a valley, maybe two hundred men. Sentries on the ridges. Cooking fires just starting.\n\nYou could report what you've seen now. Or you could get closer and learn more. The lord needs good intelligence, but getting caught would be... problematic.",
    
    "options": [
      {
        "id": "observe_from_distance",
        "textId": "ll_evt_scout_enemy_recon_war_opt_safe_text",
        "text": "[Observe] Watch from safe distance, estimate numbers and position",
        "tooltip": "Safe approach, reliable intel",
        "condition": null,
        "risk": "safe",
        "risk_chance": null,
        
        "costs": {
          "fatigue": 3,
          "gold": 0,
          "time_hours": 0
        },
        
        "rewards": {
          "xp": {
            "scouting": 40,
            "tactics": 25
          },
          "gold": 0,
          "relation": {},
          "items": []
        },
        
        "effects": {
          "heat": 0,
          "discipline": 0,
          "lance_reputation": 2,
          "medical_risk": 0,
          "fatigue_relief": 0
        },
        
        "flags_set": ["scout_enemy_report_safe"],
        "flags_clear": [],
        
        "resultTextId": "ll_evt_scout_enemy_recon_war_opt_safe_outcome",
        "outcome": "You watch for an hour, counting tents and tracking patrol patterns. Two hundred men, maybe slightly more. Infantry-heavy, light cavalry screen. Three supply wagons.\n\nWhen you report to {LORD_SHORT_TITLE}, he nods. \"Good work. Now I know what we're facing.\" Simple. Professional. The kind of work that keeps armies alive.",
        
        "outcome_failure": null,
        "injury_risk": null,
        "triggers_event": null,
        "advances_onboarding": false
      },
      
      {
        "id": "infiltrate_for_detail",
        "textId": "ll_evt_scout_enemy_recon_war_opt_risky_text",
        "text": "[Infiltrate] Sneak closer to identify officers and troop quality",
        "tooltip": "High risk, excellent intel if successful",
        "condition": null,
        "risk": "risky",
        "risk_chance": 0.35,
        
        "costs": {
          "fatigue": 5,
          "gold": 0,
          "time_hours": 0
        },
        
        "rewards": {
          "xp": {
            "scouting": 70,
            "tactics": 40,
            "stealth": 30
          },
          "gold": 0,
          "relation": {
            "lord": 15
          },
          "items": []
        },
        
        "effects": {
          "heat": 0,
          "discipline": 0,
          "lance_reputation": 10,
          "medical_risk": 0,
          "fatigue_relief": 0
        },
        
        "flags_set": ["scout_enemy_report_detailed", "hero_reputation"],
        "flags_clear": [],
        
        "resultTextId": "ll_evt_scout_enemy_recon_war_opt_risky_outcome",
        "outcome": "You circle downwind and approach through a dry streambed. Heart hammering, you get close enough to see their standards: experienced troops, well-equipped. You recognize their commander from the banners—a veteran known for aggressive tactics.\n\nWhen you report, {LORD_SHORT_TITLE} actually stands to hear the full account. \"This changes everything. Because of you, we know what we're truly facing.\" {LANCE_LEADER_SHORT} grips your shoulder. The lance watches with respect.",
        
        "outcome_failure": "You're halfway to their perimeter when a sentry spots movement. Shouts. You run.\n\nArrows hiss past. Your horse takes one in the flank but keeps running. You make it back, barely, with basic intel and a wounded mount. {LANCE_LEADER_SHORT} is pale. \"Don't ever scare me like that again.\" Your horse will need treatment.",
        
        "injury_risk": {
          "chance": 0.15,
          "severity": "minor",
          "description": "Arrow graze or fall while fleeing"
        },
        
        "triggers_event": null,
        "advances_onboarding": false
      },
      
      {
        "id": "capture_enemy_scout",
        "textId": "ll_evt_scout_enemy_recon_war_opt_aggressive_text",
        "text": "[Ambush] Try to capture their scout for interrogation",
        "tooltip": "Very risky, could get prisoner with information",
        "condition": null,
        "risk": "risky",
        "risk_chance": 0.45,
        
        "costs": {
          "fatigue": 6,
          "gold": 0,
          "time_hours": 0
        },
        
        "rewards": {
          "xp": {
            "scouting": 50,
            "tactics": 45,
            "stealth": 35,
            "one_handed": 30
          },
          "gold": 0,
          "relation": {
            "lord": 20
          },
          "items": []
        },
        
        "effects": {
          "heat": 0,
          "discipline": 0,
          "lance_reputation": 15,
          "medical_risk": 0,
          "fatigue_relief": 0
        },
        
        "flags_set": ["scout_captured_prisoner", "interrogation_available", "hero_reputation"],
        "flags_clear": [],
        
        "resultTextId": "ll_evt_scout_enemy_recon_war_opt_aggressive_outcome",
        "outcome": "You wait in cover near their patrol route. When their scout passes, you move fast—blade to throat, hand over mouth. He struggles but you control him.\n\nBack at camp, the interrogation reveals their battle plan. {LORD_SHORT_TITLE} is stunned. \"By God, you've given us victory before the battle even starts.\" The prisoner is secured, and you're a hero.",
        
        "outcome_failure": "You grab their scout but he's stronger than expected. He breaks free, shouting. You flee as their patrol converges. An arrow catches your shoulder.\n\nYou escape, but they know we're watching now. {LORD_SHORT_TITLE} is not pleased. \"Bold, but foolish.\" You need the surgeon's attention.",
        
        "injury_risk": {
          "chance": 0.25,
          "severity": "moderate",
          "description": "Combat wounds from failed capture"
        },
        
        "triggers_event": null,
        "advances_onboarding": false
      },
      
      {
        "id": "return_without_risk",
        "textId": "ll_evt_scout_enemy_recon_war_opt_cautious_text",
        "text": "[Retreat] Report their position but not their strength",
        "tooltip": "Minimal risk, minimal intel",
        "condition": null,
        "risk": "safe",
        "risk_chance": null,
        
        "costs": {
          "fatigue": 2,
          "gold": 0,
          "time_hours": 0
        },
        
        "rewards": {
          "xp": {
            "scouting": 20,
            "tactics": 10
          },
          "gold": 0,
          "relation": {},
          "items": []
        },
        
        "effects": {
          "heat": 0,
          "discipline": 1,
          "lance_reputation": -5,
          "medical_risk": 0,
          "fatigue_relief": 0
        },
        
        "flags_set": ["scout_incomplete_report"],
        "flags_clear": [],
        
        "resultTextId": "ll_evt_scout_enemy_recon_war_opt_cautious_outcome",
        "outcome": "You mark the position and ride back without getting closer. When you report, {LORD_SHORT_TITLE} frowns. \"That's all? You didn't even try to count their strength?\"\n\n{LANCE_LEADER_SHORT} looks disappointed. This wasn't good enough. You're a scout—you're supposed to take calculated risks.",
        
        "outcome_failure": null,
        "injury_risk": null,
        "triggers_event": null,
        "advances_onboarding": false
      }
    ]
  }
}
```

---

## Event Pool Size Recommendations

### Per Duty Role

**Minimum Viable Pool:** 10 events per duty
**Recommended Pool:** 15-20 events per duty
**Optimal Pool:** 25+ events per duty

**Breakdown:**
- 40% Peace events (routine, training, social)
- 40% War events (combat support, urgency, danger)
- 10% Siege-specific events (if applicable)
- 10% Special/Rare events (high stakes, character moments)

**Total Events Needed:**
- 10 duties × 15 events average = **150 events minimum**
- 10 duties × 20 events average = **200 events target**

### Event Rotation Math

**With 15 events per duty:**
- Cooldown: 3-5 days average (4 days)
- Events per 12-day cycle: 3 events
- Repetition time: 15 events ÷ 3 per cycle = 5 cycles = 60 days

**Result:** Player won't see same event repeated for **~2 in-game months**

**With 20 events per duty:**
- Repetition time: 20 events ÷ 3 per cycle = 6.6 cycles = 80 days

**Result:** Player won't see same event repeated for **~2.5 in-game months**

---

## Testing & Balance

### Playtesting Checklist

When creating new duty events:

- [ ] Event fires within 3-5 days when on duty
- [ ] Event respects context (peace/war, lord action)
- [ ] All three choice paths are viable
- [ ] XP rewards feel appropriate for effort
- [ ] Heat/Discipline/Rep changes are proportional
- [ ] Outcome text acknowledges player choice
- [ ] Failure outcomes (if risky) are interesting
- [ ] Event doesn't repeat too frequently
- [ ] Placeholder text resolves correctly
- [ ] Event advances player progression

### Balance Guidelines

**XP Rewards:**
- Safe choice: 30-40 primary skill, 15-20 secondary
- Risky choice (success): 50-70 primary, 30-40 secondary
- Corrupt choice: 10-20 primary (minimal learning)

**Fatigue Costs:**
- Simple event: 2-3 fatigue
- Complex event: 3-5 fatigue
- Dangerous event: 5-6 fatigue

**Heat Increases:**
- Minor corruption: +1-2 Heat
- Major corruption: +3-4 Heat
- Extreme corruption: +5+ Heat

**Lance Reputation:**
- Small positive act: +2-5 Rep
- Significant help: +5-10 Rep
- Heroic action: +10-15 Rep
- Minor betrayal: -5-10 Rep
- Major betrayal: -15-25 Rep

**Gold Rewards:**
- Honest work: 0-50 gold
- Skilled negotiation: 50-100 gold
- Corruption: 75-200 gold (comes with Heat)

---

## File Organization

### Directory Structure

```
ModuleData/Enlisted/Events/
  events_duty_quartermaster.json       (15-20 events)
  events_duty_scout.json               (15-20 events)
  events_duty_field_medic.json         (15-20 events)
  events_duty_armorer.json             (15-20 events)
  events_duty_runner.json              (15-20 events)
  events_duty_engineer.json            (15-20 events)
  events_duty_lookout.json             (15-20 events)
  events_duty_messenger.json           (15-20 events)
  events_duty_boatswain.json           (15-20 events, naval DLC)
  events_duty_navigator.json           (15-20 events, naval DLC)
```

### Naming Conventions

**Event IDs:**
```
{duty_abbreviation}_{context}_{scenario}_{variant}

Examples:
qm_peace_merchant_negotiation
qm_war_emergency_supplies
sc_war_enemy_recon_night
med_war_triage_moral_choice
arm_siege_sabotage_response
```

**Localization Keys:**
```
ll_evt_{duty}_{scenario}_title
ll_evt_{duty}_{scenario}_setup
ll_evt_{duty}_{scenario}_opt_{choice}_text
ll_evt_{duty}_{scenario}_opt_{choice}_outcome

Examples:
ll_evt_qm_merchant_negotiation_title
ll_evt_qm_merchant_negotiation_setup
ll_evt_qm_merchant_negotiation_opt_negotiate_text
ll_evt_qm_merchant_negotiation_opt_negotiate_outcome
```

---

## Expansion Ideas

### Future Enhancement Paths

**Chain Events:**
- Event A outcome affects Event B availability
- Example: Corrupt quartermaster deal leads to investigation event later
- Tracked via flags system

**Tier-Specific Events:**
- T1-T2: Learning the ropes, simple choices
- T3-T4: Competence tests, meaningful responsibility
- T5-T6: Leadership challenges, managing others

**Relationship-Based Events:**
- Events that reference past choices with specific lance members
- Build on established relationships
- Payoffs for long-term play

**Seasonal/Calendar Events:**
- Winter supply challenges
- Festival/holiday events in peacetime
- Anniversary of major battles

**Multi-Stage Events:**
- Events that span multiple sessions
- "Part 1: Problem arises" → "Part 2: Deal with consequences" (3-5 days later)

---

## Contributing

### Adding New Events

1. **Choose duty and context** (peace/war/siege)
2. **Identify skill focus** from duty definition
3. **Create dilemma with stakes**
4. **Write three viable choices** (safe/risky/corrupt or alternative)
5. **Balance XP and effects**
6. **Write engaging outcome text**
7. **Test cooldown timing**
8. **Submit for review**

### Review Criteria

Events should be:
- ✅ **Context-aware** (references lord actions, army state)
- ✅ **Choice-driven** (player agency matters)
- ✅ **Balanced** (XP/Heat/Rep appropriate)
- ✅ **Immersive** (good writing, military voice)
- ✅ **Progression-focused** (advances skills and character)
- ✅ **Fun** (not annoying, not repetitive)

---

## Conclusion

Duty events are the **heartbeat of the military life simulation**. They:

- Bring duty roles to life with regular, contextual events
- Reward player expertise in their chosen specialty
- Create meaningful progression through skills and relationships
- Adapt to the changing fortunes of war and peace
- Provide drama without being intrusive

**With 150-200 well-crafted duty events, the player will:**
- Experience their duty role as a living, breathing part of army life
- Face new challenges every 3-5 days that matter
- Build character through choices over months of service
- Never feel like they're grinding—just living as a soldier

**Design Philosophy:**
> "Every duty event should feel like a moment that matters—a choice that defines who your soldier is becoming, told through the gritty reality of military life."

---

**Document Maintained By:** Enlisted Development Team  
**Questions?** See `docs/CONTRIBUTING.md`  
**Last Review:** December 14, 2025

---

## Quick Reference Card

### Event Creation Checklist

```
□ Event ID follows naming convention
□ Triggers include has_duty:{duty_id}
□ Context specified (peace/war/siege)
□ Cooldown set to 3-5 days
□ Three viable choice paths
□ XP rewards balanced (30-70 range)
□ Fatigue costs reasonable (2-6 range)
□ Heat/Discipline/Rep appropriate
□ Outcome text written for all paths
□ Failure text written for risky paths
□ Placeholders used correctly
□ Localization IDs added
□ Tested in-game
□ Reviewed by team
```

### XP Quick Reference

| Choice Type | Primary XP | Secondary XP |
|-------------|------------|--------------|
| Safe | 30-40 | 15-20 |
| Risky (success) | 50-70 | 30-40 |
| Risky (failure) | 10-20 | 5-10 |
| Corrupt | 10-20 | 0-10 |
| Heroic | 60-80 | 40-50 |

### Heat Quick Reference

| Action | Heat Change |
|--------|-------------|
| Corruption (minor) | +1 to +2 |
| Corruption (major) | +3 to +4 |
| Corruption (extreme) | +5+ |
| Report corruption | -2 to -3 |
| Clean record | -1 (passive decay) |

---

**End of Guide**
