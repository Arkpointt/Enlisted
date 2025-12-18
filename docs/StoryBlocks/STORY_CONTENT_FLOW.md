# Story Content Flow - Complete Player Experience

> **Purpose**: This document shows ALL story content organized by HOW and WHEN the player experiences it in actual gameplay. Use this to identify overlaps, consolidate systems, and plan simplifications.

**Last Updated**: December 18, 2025

---

## Player's Day in the Life

### Morning - 6:00 AM

**Main Menu displays:**
```
═══════════════════════════════════════════
Lord: Derthert | Rank: Man-at-Arms (T2)
Enlisted: 47 days

Schedule:
  [Morning: Training Drill]  ← Current
   Afternoon: Guard Duty
   Dusk: Free Time
   Night: Rest

Orders:
  • Report for Training [Now]
  • Lance Leader wants to see you [Today]

[Camp] [Decisions] [My Lord...] [Leave]
═══════════════════════════════════════════
```

**What happens:**
1. **Schedule auto-executes**: "Training Drill" starts
2. **System rolls for event**: 20% chance
3. **TWO POSSIBLE OUTCOMES**:

   **A. No Event (80% chance)**:
   ```
   [Small notification appears]
   "Training Complete - Gained 15 One-Handed XP"
   [Auto-closes after 3 seconds]
   ```

   **B. Event Fires (20% chance)**:
   ```
   ════════════════════════════════════
           TRAINING DRILL
   ────────────────────────────────────
   The Drill Sergeant singles you out.
   "Look at THIS one! THAT'S how you 
   hold the line!"
   
   [Thank him professionally] (Safe)
     +10 Leadership XP
   
   [Show off] (Athletics 35+)
     Success: +2 Rep, +20 Athletics XP
     Failure: Stumble, -1 Rep
   
   [Stay humble] (Charm 25+)
     +15 Charm XP, peers respect you
   ════════════════════════════════════
   ```

---

### Mid-Morning - 8:00 AM

**Decision Events Check** (Automatic Push System):
- System evaluates available decision events
- Cooldowns, tier gates, pacing limits applied
- If event available: Fires automatically

**Example Event**:
```
════════════════════════════════════════
    LANCE LEADER'S REQUEST
────────────────────────────────────────
{LANCE_LEADER_SHORT} approaches you.

"Listen, I need someone reliable for
a special task. You interested?"

[Accept the assignment] (Standard)
  Unknown consequences, +Reputation

[Ask for details first] (Charm 25+)
  Learn more before committing

[Politely decline] (Safe)
  No consequences

════════════════════════════════════════
```

**Source**: `events_decisions.json` → `"delivery": { "method": "automatic" }`

---

### Afternoon - 12:00 PM

**Main Menu shows updated schedule:**
```
Schedule:
   Morning: Training Drill [Complete]
  [Afternoon: Guard Duty]  ← Current
   Dusk: Free Time
   Night: Rest

Orders:
  • Guard Duty starts now
  • Lance Leader wants to see you [Today]
```

**Guard Duty executes:**
- 20% chance → Full event fires
- 80% chance → Quiet completion

**If event fires**:
```
════════════════════════════════════════
         GUARD DUTY
────────────────────────────────────────
You're posted at the camp perimeter.
Hours pass quietly... then you hear
something in the brush.

[Investigate quietly] (Scouting 25+)
  Success: Catch thief (+2 Rep, +Gold)
  Failure: False alarm

[Raise the alarm] (Standard)
  50% chance real threat or false alarm

[Ignore it] (Risky)
  30% safe, 70% thief steals supplies
════════════════════════════════════════
```

**Source**: Either:
- `events_duty_scout.json` (if has_duty:scout)
- NEW duty-triggered event from schedule mapping

---

### Evening - 6:00 PM (2 PM Real Time Check)

**Decision Events Check #2**:
- Another automatic push evaluation
- Could fire lance mate event, lord event, situation event

**Example - Heat Threshold Reached**:
```
╔══════════════════════════════════════╗
║  [ESCALATION]     THE SHAKEDOWN      ║
╠══════════════════════════════════════╣
║  "Kit inspection! Everyone out, NOW!"║
║                                      ║
║  {SERGEANT_NAME} is tearing through  ║
║  camp. They're looking for YOU.      ║
║                                      ║
║  [Comply - Let them search]          ║
║    50% risk of exposure              ║
║                                      ║
║  [Bribe sergeant] (100 gold)         ║
║    -100 gold, -3 Heat if success     ║
║                                      ║
║  [Create distraction] (Roguery 30+)  ║
║    +25 Roguery, 40% caught           ║
║                                      ║
║  Heat ▓▓▓▓▓░░░░░ 5/10 [SHAKEDOWN]   ║
╚══════════════════════════════════════╝
```

**Source**: `events_escalation_thresholds.json` → Heat threshold = 5

---

### Dusk - 6:00 PM

**Main Menu:**
```
Schedule:
   Morning: Training Drill [Complete]
   Afternoon: Guard Duty [Complete]
  [Dusk: Free Time]  ← Current
   Night: Rest

Orders:
  • Lance Leader wants to see you [Today]
```

**Player opens Decisions Menu:**
```
════════════════════════════════════════
              DECISIONS
────────────────────────────────────────

Events:
  • Lance Leader's Request [!]
  • Organize Dice Game

────────────────────────────────────────

Training (Available Now):
  • Sparring Circle
  • Weapons Practice
  • Archery Range
  • Riding Drills

Social (Available Now):
  • Fire Circle
  • Drink with the Lads
  • Play Dice
  • Tell War Stories

────────────────────────────────────────
[Back]
════════════════════════════════════════
```

**Player clicks "Lance Leader's Request"**:
- Launches event immediately
- Makes choice, gets consequences
- Order clears from main menu

**Player clicks "Sparring Circle"**:
```
════════════════════════════════════════
        SPARRING CIRCLE
────────────────────────────────────────
You head to the training grounds to
practice sword work against partners.

[Standard sparring] (Safe)
  +20 One-Handed XP, -2 Fatigue

[Challenge a veteran] (One-Handed 40+)
  Success: +30 One-Handed XP, +1 Rep
  Failure: Bruised condition, -1 Rep

[Back]
════════════════════════════════════════
```

**Source**: `activities.json` → Category: Training

---

### Night - 10:00 PM

**Schedule executes Rest**:
- Fatigue recovery applied
- No events (Rest is quiet)
- Simple notification: "You rest and recover 5 Fatigue"

---

### Follow-Up Events (Time-Delayed)

**If player made certain choices earlier, follow-up events fire:**

**Example - 2 hours after "Guard Duty" event**:
```
════════════════════════════════════════
     SERGEANT'S COMMENDATION
────────────────────────────────────────
The Sergeant finds you later.

"Good work catching that thief. Here's
your share of what we recovered."

+30 Gold | +1 Reputation

[Continue]
════════════════════════════════════════
```

**Triggered by**: Previous event choice scheduled follow-up

---

## All Story Content by System

### 1. Schedule-Triggered Events

**When**: Schedule block executes (Morning, Afternoon, Dusk, Night)
**Chance**: 10-20% per execution
**Type**: Full multi-choice events

**Current Implementation**:
- Simple popups: `schedule_popup_events.json` (9 events, continue-only)
- Full events: `events_duty_*.json` (50 events, multi-choice)

**Sources**:
```
Training Drill → events_training.json
Guard Duty → events_duty_scout.json (if scout) / NEW guard events
Work Detail → events_duty_engineer.json / NEW work events
Patrol → events_duty_scout.json
Rest → (usually quiet, small XP gain)
Free Time → (player-driven, see Camp Activities)
```

**Example Events**:
- "Formation Drill Excellence" (training)
- "Rusty Weapon Found" (work detail)
- "Night Watch Disturbance" (guard duty)
- "Suspicious Tracks" (patrol)

---

### 2. Decision Events (Automatic Push)

**When**: 8am, 2pm, 8pm checks (3x per day)
**Chance**: Evaluated with cooldowns, tier gates, pacing limits
**Type**: Full multi-choice events

**Source Files**:
- `events_decisions.json` (14 events)
- `events_player_decisions.json` (player-initiated subset)

**Categories**:
```
Lord: Lord invites you (hunting, feast, council)
Lance Leader: Special assignments, training offers
Lance Mate: Dice games, favors, disputes
Situation: Medical emergencies, merchant deals
```

**Pacing**:
- Max 2 per day
- Max 8 per week
- 6-hour minimum gap between events
- Cooldown per event: 7-14 days

**Example Events**:
- "Lord's Hunt Invitation" (lord, T5+)
- "Lance Mate Dice Game" (lance_mate, T1+)
- "Medic Emergency" (situation, T2+)
- "Lance Mate Favor Request" (chain starter)

---

### 3. Escalation Threshold Events

**When**: Heat, Discipline, Rep, Medical thresholds reached
**Chance**: 100% (guaranteed when threshold hit)
**Type**: Full multi-choice events with dramatic UI

**Source File**: `events_escalation_thresholds.json` (16 events)

**Thresholds**:
```
Heat:
  3 → "The Warning"
  5 → "The Shakedown"
  7 → "The Audit"
  10 → "Exposed"

Discipline:
  3 → "Extra Duty"
  5 → "The Hearing"
  7 → "Blocked from Promotion"
  10 → "Discharge Threat"

Lance Reputation:
  +20 → "Trusted"
  +40 → "Bonded"
  -20 → "Isolated"
  -40 → "Sabotage"

Medical Risk:
  3 → "Condition Worsening"
  4 → "Complication"
  5 → "Medical Emergency"
```

**UI**: Uses modern full-screen presentation with character portraits, escalation bars visible

---

### 4. Camp Activities (Player-Initiated)

**When**: Player opens Decisions menu and selects activity
**Chance**: 100% (player-driven)
**Type**: Simple choice or event launch

**Source File**: `activities.json` (18 activities)

**Categories**:
```
Training (Free Time / Night):
  - Formation Drill (+Athletics, +Leadership)
  - Sparring Circle (+One-Handed)
  - Weapons Practice (+Weapon XP)
  - Archery Range (+Bow)
  - Crossbow Range (+Crossbow)
  - Riding Drills (+Riding, +Polearm)
  - Two-Handed Drill (+Two-Handed)
  - Polearm Drill (+Polearm)
  - Throwing Practice (+Throwing)

Social (Free Time / Night):
  - Fire Circle (+Charm, +Fatigue Relief)
  - Drink with the Lads (+Charm, +Fatigue Relief)
  - Play Dice (+Roguery, +Charm)
  - Write a Letter (+Charm, +Leadership)
  - Tell War Stories (+Charm, +Leadership)
  - Rest and Eat (+Fatigue Relief)

Lance (Any Time):
  - Talk to Lance Leader (+Leadership, +Tactics)
  - Check on Lance Mates (+Charm, +Leadership)
  - Help Struggling Soldier (+Leadership, +Charm)
  - Share Rations (+Charm, +Leadership)
  - Settle Dispute (+Charm, +Leadership)
  - Train Recruits (+Leadership, +Athletics)

Tasks (Any Time):
  - Help the Surgeon (+Medicine)
  - Maintain Equipment (+Smithing, +Trade)
```

**Note**: Some activities could trigger follow-up events (e.g., "Play Dice" → gambling event)

---

### 5. Follow-Up Events (Chained)

**When**: Scheduled by previous event outcome (delays: 2 hours to 7 days)
**Chance**: 100% when scheduled
**Type**: Full multi-choice events (or simple outcomes)

**Examples**:
```
"Rusty Weapon Found" → SUCCESS choice
  ↓ (2 hours later)
"Lance Leader Praises" → +50 Gold, +Rep

"Lance Mate Favor" → ACCEPT choice
  ↓ (7 days later)
"Favor Repayment" → +Gold, +Rep

"Guard Duty" → CATCH THIEF choice
  ↓ (1 day later, only if battle occurs)
"Sergeant Commendation" → +Gold, +Rep
```

**Implementation**: Event queue in `DecisionEventBehavior` tracks scheduled events

---

## System Overlap Analysis

### Where Systems Duplicate:

**1. Schedule Popups vs Duty Events**
```
BOTH trigger from schedule execution
BOTH give XP rewards
BOTH are duty-themed

DIFFERENCE:
- Popups: Simple, continue-only, lightweight
- Duty Events: Multi-choice, skill checks, consequences
```

**2. Decision Events vs Camp Activities**
```
BOTH are player-accessible choices
BOTH appear in menus
BOTH give XP/rewards

DIFFERENCE:
- Decision Events: Story-driven, cooldowns, tier-gated
- Activities: Reliable, repeatable, mechanical
```

**3. Duty Events vs Decision Events**
```
BOTH use same JSON schema
BOTH fire through same event system
BOTH have multi-choice with consequences

DIFFERENCE:
- Duty Events: Triggered by assigned duties
- Decision Events: Triggered by automatic push checks
```

---

## Simplification Opportunities

### Option A: Merge Duty + Decision Events
**One unified StoryBlocks event system**:
- All events in `ModuleData/Enlisted/Events/*.json`
- Category field determines trigger type: `"duty"`, `"decision"`, `"escalation"`
- Same evaluation engine, same cooldowns, same pacing

**Delete**:
- `schedule_popup_events.json` (9 simple popups)

**Keep**:
- All StoryBlocks events (142 events)
- Add duty-to-event mapping config

---

### Option B: Schedule → Events → Activities
**Three distinct layers**:
1. **Events**: All narrative moments (duty-triggered + decision-triggered + escalation)
2. **Activities**: Reliable skill grinding (training, social, lance)
3. **Orders**: Notification layer (shows upcoming duties/events)

**Delete**:
- `schedule_popup_events.json`
- Separate "duty events" concept (merge into events)

**Keep**:
- StoryBlocks events system
- Activities system
- Add Orders system (new)

---

### Option C: Full Consolidation
**Two systems total**:
1. **Events**: ALL story content (duty, decision, escalation, activity-triggered)
2. **Orders**: Notification/scheduling layer

**Delete**:
- `schedule_popup_events.json`
- `activities.json` (convert activities into events)

**Keep**:
- StoryBlocks events only
- Add Orders system

---

## Current File Inventory

### Active Systems:
```
ModuleData/Enlisted/
├── Events/                         ← 142 events, PRIMARY SYSTEM
│   ├── events_decisions.json              (14 events)
│   ├── events_player_decisions.json       (subset of above)
│   ├── events_duty_*.json                 (50 events, 10 files)
│   ├── events_escalation_thresholds.json  (16 events)
│   ├── events_general.json                (18 events)
│   ├── events_training.json               (16 events)
│   ├── events_pay_*.json                  (14 events, 3 files)
│   ├── events_promotion.json              (5 events)
│   └── ...
│
├── Activities/
│   └── activities.json             ← 18 activities, ACTIVE
│
├── schedule_popup_events.json      ← 9 popups, REDUNDANT?
│
└── StoryPacks/LanceLife/           ← DEPRECATED (migrate to Events)
```

---

## Recommendations

### What to Keep:
✅ **StoryBlocks Events** (`Events/*.json`) - PRIMARY system, 142 events
✅ **Camp Activities** (`activities.json`) - Reliable player actions, 18 activities
✅ **Escalation system** - Threshold events work well

### What to Delete:
❌ **Schedule Popup Events** (`schedule_popup_events.json`) - Redundant, upgrade to full events
❌ **StoryPacks** (`StoryPacks/LanceLife/`) - Deprecated, already flagged for removal

### What to Create:
➕ **Orders System** - Notification layer for Main Menu
➕ **Duty-to-Event Mapping** - Connect schedule duties to StoryBlocks events

---

## Proposed Flow (Simplified)

```
┌─────────────────────────────────────────────┐
│  MAIN MENU                                   │
│  - Shows Schedule (4 time blocks)            │
│  - Shows Orders (time-sensitive tasks)       │
│  - Navigation to Decisions menu              │
└─────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────┐
│  ORDERS (Notification Layer)                 │
│  - Schedule duties with deadlines            │
│  - Decision events awaiting player           │
│  - Quest tasks                               │
│  Source: Schedule + DecisionEventBehavior    │
└─────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────┐
│  EVENTS (Unified StoryBlocks)                │
│  - Duty-triggered (schedule execution)       │
│  - Decision-triggered (automatic push)       │
│  - Escalation-triggered (thresholds)         │
│  - Activity-triggered (player-initiated)     │
│  Source: Events/*.json (ONE system)          │
└─────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────┐
│  DECISIONS MENU                              │
│  - Events: Available decision events         │
│  - Training: Camp activities (skill XP)      │
│  - Social: Camp activities (fatigue/rep)     │
│  Source: DecisionEventBehavior + activities  │
└─────────────────────────────────────────────┘
```

---

## Next Steps

1. **Decide on consolidation approach** (A, B, or C above)
2. **Map existing content** to new structure
3. **Update master implementation plan** with simplified vision
4. **Create migration plan** for removing redundant systems
5. **Update code** to support unified flow

---

## Questions to Answer

1. **Keep simple popups or upgrade all to events?**
2. **Merge duty events into decision events or keep separate?**
3. **Convert activities to events or keep as separate system?**
4. **How should Orders interact with Events?** (clickable vs informational)
5. **What's the right % chance for schedule → event trigger?** (10%? 20%?)

