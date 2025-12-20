# Story Blocks Master Reference

> **Purpose**: Single source of truth for all story content. Complete catalog organized by trigger type (Duty Events, Map Incidents, Camp Life), with technical schema, and design/writing guidelines.

**Last Updated**: December 18, 2025

**Quick Links:**
- 📐 [Schema Reference](#schema-reference) - Technical docs for creating events
- ✍️ [Design & Writing Guidelines](#event-design--writing-guidelines) - How to write good events

---

## Event Firing Rules & Restrictions

### Duty Events (Scheduled)
- **Trigger**: When a scheduled duty executes (Morning/Afternoon/Dusk/Night blocks)
- **Base Chance**: 20% per duty execution
- **Frequency**: Up to 4 times per day (once per time block)
- **Cooldown**: Per-event (2-10 days depending on event)
- **Restrictions**:
  - Must be enlisted
  - Must not be in battle/encounter
  - Must not be prisoner
  - Tier restrictions apply per event

### Random Map Incidents (One-Time)
- **Trigger**: Native Map Incident system (LeavingBattle, LeavingSettlement, DuringSiege, etc.)
- **Base Chance**: 5-15% per trigger context
- **Frequency**: Global cooldown (8-24 hours between any map incidents)
- **Cooldown**: Per-incident (varies, typically 7-30 days)
- **Restrictions**:
  - Must be enlisted
  - Must not be prisoner
  - Must not be in conversation
  - Not at sea (most incidents)
  - Context-specific (some only fire during siege, after battle, etc.)

### Camp Life Events (Situation-Triggered)
- **Trigger**: Escalation thresholds (Heat, Discipline, Pay Tension, etc.)
- **Base Chance**: 100% when threshold reached
- **Frequency**: Immediate when threshold crossed
- **Cooldown**: Implicit (based on escalation value recovery)
- **Restrictions**:
  - Must be enlisted
  - Specific threshold values required
  - Some require tier minimums

### Safety Systems
- **Global Incident Lock**: Only 1 map incident can be queued at a time
- **Battle Protection**: No incidents fire during active combat
- **Menu Protection**: Incidents deferred while in menus
- **Time Lock**: Campaign time pauses during incident resolution
- **Queue Deferral**: Follow-up incidents wait for safe moments

---

## Static Events (Always Available / Guaranteed)

### Onboarding Events (One-Time)
- **enlist_oath_ceremony** - Fires once at enlistment start
- **enlist_first_duty** - Fires on first schedule assignment
- **enlist_baggage_check** - Deferred 1 hour after enlistment (map incident)

### Promotion Events (Tier Milestones)
- **promotion_to_t2** - Fires at T1→T2 promotion
- **promotion_to_t3_nco** - Fires at T2→T3 promotion (NCO rank)
- **promotion_to_t4_senior_nco** - Fires at T3→T4
- **promotion_to_t5_officer** - Fires at T4→T5 (Officer rank)
- **promotion_to_t6_senior_officer** - Fires at T5→T6

### Pay Muster Events (Scheduled)
- **pay_muster_standard** - Fires every 7-14 days (pay cycle)
- **pay_muster_final** - Fires on discharge/desertion

### Camp Activities (Player-Initiated, Always Available)
- **activity_formation_drill** - Training menu (Cost: 5 Fatigue)
- **activity_combat_drill** - Training menu (Cost: 5 Fatigue)
- **activity_specialist_training** - Training menu (Cost: 6 Fatigue)
- **activity_write_letter** - Social menu (Cost: 2 Fatigue)
- **activity_dice_game** - Social menu (Cost: 1 Fatigue)
- **activity_petition_lord** - Social menu (T3+, Cost: 3 Fatigue)
- **activity_lance_bonding** - Social menu (Cost: 2 Fatigue)

---

## Event Mechanics Reference

### Skill Checks
**How They Work:**
- Player skill vs. difficulty threshold
- Roll formula: `PlayerSkill + Random(0-10) >= Difficulty`
- Success rate examples:
  - Skill 20 vs. Diff 20 = 50-60% success
  - Skill 30 vs. Diff 20 = 90-100% success
  - Skill 10 vs. Diff 30 = 0-10% success

**Difficulty Bands:**
- Easy: 15-20 (most players pass)
- Moderate: 25-30 (50/50 for average player)
- Hard: 35-40 (need specialization)
- Very Hard: 45+ (elite only)

### Escalation Values
**What They Are:**
- Persistent counters that trigger events at thresholds
- Range: 0-10 for most escalations
- Checked every game tick

**Current Escalations:**

| Escalation | Range | Thresholds | Effect |
|---|---|---|---|
| Heat | 0-10 | 3, 5, 7, 10 | Corruption attention/consequences |
| Discipline | 0-10 | 3, 5, 7, 10 | Trouble record/punishment |
| Pay Tension | 0-100 | 40, 60, 80 | Company anger over unpaid wages |
| Lance Reputation | -50 to +50 | -40, -20, +20, +40 | Trust/hostility with lance |
| Medical Risk | 0-5 | 3, 4, 5 | Injury/illness severity |
| Fatigue | 0-10 | 0 (depleted) | Energy for extra actions |

**How They Change:**
- Event choices modify values directly
- Some decay over time (Heat, Discipline)
- Some accumulate passively (Pay Tension if unpaid)
- Crossing thresholds triggers Camp Life Events (guaranteed)

### Cooldowns
**Per-Event Cooldowns:**
- Each event has its own cooldown timer
- Starts after event fires
- Prevents same event from repeating too soon
- Example: `rusty_weapon_found` = 3 days cooldown

**Global Cooldown:**
- Applies to Random Map Incidents only
- Prevents incident spam
- 8-24 hour window between any map incidents
- Does NOT affect Duty Events or Camp Life Events

### Follow-Up Chains
**How They Work:**
1. Player completes Event A (parent)
2. Parent event sets flag + schedules follow-up
3. Game waits for delay period (2hr, 4hr, 1 day, etc.)
4. Follow-up event fires when:
   - Delay period passed
   - Player is safe (no battle, no menu, not captive)
   - Conditions met (if conditional)

**Conditional Chains:**
- Some follow-ups only fire if specific conditions met
- Examples:
  - `sharp_weapons_noticed` - ONLY if battle within 24hr
  - `wagon_wheel_holds` - ONLY if traveling
  - `scout_intel_victory` - ONLY if lord wins battle

**Queue System:**
- Multiple follow-ups can be queued
- Fire one at a time (no overlap)
- Deferred until safe moment

### Tier Gates
**What They Are:**
- Minimum rank requirement to see/trigger event
- Applied at event trigger time

**Tier Bands:**

| Tier | Rank Type | Examples |
|---|---|---|
| T1-T2 | Enlisted | Private, Corporal |
| T3-T4 | NCO | Sergeant, Staff Sergeant |
| T5-T6 | Officer | Lieutenant, Captain |

**How They Work:**
- `T1+` = All players can see
- `T3+` = NCO and Officer only
- `T5-T6` = Officers only
- `T1-T3 only` = Enlisted/NCO, NOT officers

### Context Requirements
**Battle Context:**
- `PostBattle` - Only after battle/encounter
- `DuringBattle` - During active combat
- `NoBattle` - Only when safe on campaign map

**Location Context:**
- `LeavingSettlement` - Town/Castle/Village exit
- `EnteringSettlement` - Town/Castle/Village entry
- `DuringSiege` - Only during active siege
- `Traveling` - Moving on campaign map

**Army Context:**
- `InArmy` - Part of lord's army
- `Independent` - Solo party (not in army)
- `EnemyNearby` - Hostile party in detection range

**Other Context:**
- `Enlisted` - Must be in enlistment
- `NotPrisoner` - Must be free
- `NotInConversation` - No dialog active
- `NotAtSea` - Not on water (most incidents)

---

## INDEX

### 📋 Quick Navigation

**System Reference:**
- [Event Firing Rules](#event-firing-rules--restrictions) - When and how often events trigger
- [Static Events](#static-events-always-available--guaranteed) - Onboarding, promotions, pay, camp activities
- [Event Mechanics](#event-mechanics-reference) - Skill checks, escalations, cooldowns, tier gates
- [📐 Schema Reference](#schema-reference) - Technical docs for creating new events (APPENDIX)
- [✍️ Design & Writing Guidelines](#event-design--writing-guidelines) - How to write good events (APPENDIX)

**Story Content:**
- [Duty Events](#duty-events) - Triggered by scheduled duties (25+ events)
- [Random Map Incidents](#random-map-incidents) - Native-style one-off events (25+ incidents)
- [Camp Life Events](#camp-life-events) - Escalation threshold events (30+ events)

**Planning Tools:**
- [Summary Statistics](#summary-statistics) - Overview and counts
- [Content Gaps](#content-gaps-need-creation) - What needs to be built

---

### 🎖️ [DUTY EVENTS](#duty-events)
*Events that fire when scheduled duties execute (20% chance)*

**Specialist Duty Roles** (Need holder assigned)
- ✅ [Runner Duty](#runner-duty-events) - 3 events (urgent message, coordinate, strategic dispatch)
- ✅ [Scout Duty](#scout-duty-events) - 3 events (enemy position, track enemy, intercept messenger)
- ✅ [Field Medic Duty](#field-medic-duty-events) - 3 events (wounded soldier, camp illness, experimental treatment)
- ⚠️ [Quartermaster Duty](#quartermaster-duty-events) - **NO EVENTS YET** (needs content)
- ⚠️ [Armorer Duty](#armorer-duty-events) - **NO EVENTS YET** (needs content)
- ⚠️ [Engineer Duty](#engineer-duty-events) - **NO EVENTS YET** (needs content)
- ⚠️ [Lookout Duty](#lookout-duty-events) - **NO EVENTS YET** (needs content)
- ⚠️ [Navigator Duty](#navigator-duty-events) - **NO EVENTS YET** (needs content)
- ⚠️ [Boatswain Duty](#boatswain-duty-events) - **NO EVENTS YET** (needs content)
- ⚠️ [Messenger Duty](#messenger-duty-events) - **NO EVENTS YET** (needs content)

**General Schedule Activities** (No duty role required)
- ✅ [Work Detail](#work-detail-events) - 3 events (rusty weapon, wagon wheel, sharpening)
- ✅ [Patrol Duty](#patrol-duty-events) - 2 events (suspicious tracks, lost traveler)
- ✅ [Sentry Duty](#sentry-duty-events) - 2 events (night disturbance, officer inspection)
- ✅ [Training Drill](#training-drill-events) - 2 events (drill excellence, sparring challenge)
- ✅ [Foraging](#foraging-events) - 1 event (hidden cache)

---

### 🗺️ [RANDOM MAP INCIDENTS](#random-map-incidents)
*Native-style one-off events triggered by map actions*

- ✅ [Post-Battle Incidents](#post-battle-incidents) - 4 incidents (sleeping sentry, coin clipping, enlist enemy, honor slain)
- ✅ [Camp Life Incidents](#camp-life-incidents) - 5 incidents (troops fight, donative demand, veteran mentor, job offer, etc.)
- ✅ [Travel Incidents](#travel-incidents) - 3 incidents (ice march, sandstorm, heat/dust)
- ✅ [Settlement Incidents](#settlement-incidents) - 3 incidents (soldier debt, wanted criminal, local hero)
- ✅ [Siege Incidents](#siege-incidents) - 3 incidents (at the breach, water supplies, mining)

---

### ⚠️ [CAMP LIFE EVENTS](#camp-life-events)
*Automatic events triggered by escalation thresholds (100% when threshold reached)*

**Escalation Chains:**
- ✅ [Heat Escalation](#heat-escalation-events) - 4 events (warning→shakedown→audit→exposed)
- ✅ [Discipline Escalation](#discipline-escalation-events) - 4 events (extra duty→hearing→blocked→discharge threat)
- ✅ [Pay Tension Escalation](#pay-tension-escalation-events) - 3 events (desperate→critical→mutiny)
- ✅ [Lance Reputation](#lance-reputation-events) - 4 events (bonded, trusted, isolated, sabotage)
- ✅ [Medical Risk](#medical-risk-events) - 3 events (worsening→complication→emergency)
- ✅ [Fatigue Crisis](#fatigue-crisis-events) - 1 event (exhausted)

---

# DUTY EVENTS

## Runner Duty Events

### Event: runner_urgent_message
**Trigger**: Runner duty holder, army moving/in battle
**Tier Gate**: T1+
**Chance**: 20% on duty execution
**Cooldown**: 3 days

```
incident_runner_urgent_message
├── [Deliver quickly] (Athletics 30+) → Cost: 3 Fatigue
│   ├── SUCCESS: +2 Lance Rep, +20 Athletics XP
│   └── FAILURE: Arrive late, -1 Lance Rep
│
├── [Read the message first] (Roguery 25+) → RISKY
│   ├── SUCCESS: +Intel, +Roguery XP, +Heat (1)
│   └── FAILURE: Caught reading, -2 Lance Rep, +Heat (2)
│
└── [Normal delivery] (Standard) → +10 Athletics XP
```
**Escalation**: NONE
**Follow-up**: NONE

---

### Event: runner_coordinate_messages (NCO)
**Trigger**: Runner duty holder, T3+ only
**Tier Gate**: T3-T6
**Chance**: 15% on duty execution
**Cooldown**: 5 days

```
incident_runner_coordinate_messages
├── [Organize efficiently] (Leadership 25+) → Cost: 2 Fatigue
│   ├── SUCCESS
│   │   ├── Immediate: +20 Leadership XP, -2 Fatigue
│   │   └── Escalation → runner_coordination_praised (IF battle within 12hr)
│   │       └── Outcome: +3 Lance Rep, +Lord Favor
│   └── FAILURE: Messages delayed, -1 Lance Rep
│
├── [Wing it] (Standard) → +10 Leadership XP
│
└── [Delegate to junior] (Safe) → +5 Leadership XP
```
**Escalation**: Conditional (requires battle)
**Follow-up**: YES (12 hours, conditional)

---

### Event: runner_strategic_dispatch (Officer)
**Trigger**: Runner duty holder, T5+ only, during war
**Tier Gate**: T5-T6
**Chance**: 10% on duty execution
**Cooldown**: 7 days

```
incident_runner_strategic_dispatch
├── [Send immediately] (Risky) → 60% success, 40% intercepted
│   ├── SUCCESS: +Lord Favor, strategic advantage
│   └── FAILURE: Enemy intercepts, -Lord Favor, +Heat (2)
│
├── [Wait for confirmation] (Safe) → Delayed but secure
│   └── Outcome: +5 Leadership XP, message arrives safely
│
└── [Encrypt message] (Intelligence 30+) → Cost: 1 Fatigue
    ├── SUCCESS: Secure delivery, +Intelligence XP, +Lord Favor
    └── FAILURE: Encryption flawed, standard delivery
```
**Escalation**: NONE
**Follow-up**: NONE

---

## Scout Duty Events

### Event: scout_enemy_position
**Trigger**: Scout duty holder, enemy nearby
**Tier Gate**: T1+
**Chance**: 25% on duty execution
**Cooldown**: 2 days

```
incident_scout_enemy_position
├── [Get closer] (Scouting 35+) → Cost: 2 Fatigue, RISKY
│   ├── SUCCESS
│   │   ├── Immediate: +25 Scouting XP, accurate intel
│   │   └── Escalation → scout_intel_victory (IF lord wins battle)
│   │       └── Outcome: +3 Lance Rep, +Renown, "Your intel won the day"
│   └── FAILURE: Spotted by enemy, wounded, -2 Fatigue
│
├── [Report from distance] (Scouting 20+) → Safe
│   ├── SUCCESS: +15 Scouting XP, adequate intel
│   └── FAILURE: Wrong count, -1 Lance Rep
│
└── [Guess their numbers] (Coward) → High risk
    └── Outcome: 50% right/50% wrong, -1 Lance Rep if wrong
```
**Escalation**: Conditional (requires lord victory)
**Follow-up**: YES (when battle resolves, conditional)

---

### Event: scout_track_enemy (NCO)
**Trigger**: Scout duty holder, T3+, enemy fleeing/moving
**Tier Gate**: T3-T6
**Chance**: 20% on duty execution
**Cooldown**: 4 days

```
incident_scout_track_enemy
├── [Track them personally] (Scouting 30+, Tactics 25+)
│   ├── SUCCESS
│   │   ├── Immediate: +20 Scouting XP, +15 Tactics XP
│   │   └── Escalation → scout_ambush_opportunity (2 hours later)
│   │       ├── [Set ambush] → Engage enemy (custom battle)
│   │       ├── [Report position] → Lord decides
│   │       └── [Let them go] → -1 Lance Rep
│   └── FAILURE: Lost trail, no follow-up
│
├── [Coordinate lance] (Leadership 25+) → Team effort
│   └── SUCCESS: +Leadership XP, +Scouting XP, adequate tracking
│
└── [Report to lord] (Safe) → Let lord decide
```
**Escalation**: YES (ambush opportunity)
**Follow-up**: YES (2 hours later, if successful)

---

### Event: scout_intercept_messenger (Officer)
**Trigger**: Scout duty holder, T5+, enemy in area
**Tier Gate**: T5-T6
**Chance**: 10% on duty execution
**Cooldown**: 10 days

```
incident_scout_intercept_messenger
├── [Capture messenger] (Tactics 35+, Athletics 30+)
│   ├── SUCCESS
│   │   ├── Immediate: +Enemy intel, +Tactics XP, +Renown
│   │   └── Escalation → scout_decoded_message (4 hours later)
│   │       ├── [Share with lord] → +Lord Favor, strategic advantage
│   │       ├── [Use for profit] → +Gold, +Heat (3), +Roguery XP
│   │       └── [Destroy it] → -Lord Favor (missed opportunity)
│   └── FAILURE: Messenger escapes, enemy alerted
│
├── [Shadow messenger] (Scouting 40+) → Follow to enemy camp
│   ├── SUCCESS: +Intel (enemy location), +Scouting XP
│   └── FAILURE: Lost trail
│
└── [Report sighting] (Standard) → Lord sends patrol
```
**Escalation**: YES (decoded message)
**Follow-up**: YES (4 hours later, if captured)

---

## Field Medic Duty Events

### Event: medic_wounded_soldier
**Trigger**: Field Medic duty holder, post-battle
**Tier Gate**: T1+
**Chance**: 30% after battle
**Cooldown**: 1 day

```
incident_medic_wounded_soldier
├── [Stabilize critical] (Medicine 35+) → Cost: 3 Fatigue
│   ├── SUCCESS
│   │   ├── Immediate: +25 Medicine XP, -3 Fatigue
│   │   └── Escalation → medic_soldier_survives (1 day later)
│   │       └── Outcome: +3 Lance Rep, +Renown, "You saved him"
│   └── FAILURE
│       └── Escalation → medic_soldier_dies (1 day later)
│           └── Outcome: -2 Lance Rep, +Medical Risk (1)
│
├── [Do your best] (Medicine 20+) → Standard
│   ├── SUCCESS: +15 Medicine XP, 50% survival chance
│   └── FAILURE: -1 Lance Rep
│
└── [Triage - send to surgeon] (Safe)
    └── Outcome: Surgeon handles it, +5 Medicine XP
```
**Escalation**: YES (survival/death outcome)
**Follow-up**: YES (1 day later, always)

---

### Event: medic_camp_illness (NCO)
**Trigger**: Field Medic duty holder, T3+, low morale/supplies
**Tier Gate**: T3-T6
**Chance**: 15% on duty execution (camp context)
**Cooldown**: 7 days

```
incident_medic_camp_illness
├── [Quarantine infected] (Medicine 30+) → Cost: Party -5 Morale
│   ├── SUCCESS
│   │   ├── Immediate: +20 Medicine XP, illness contained
│   │   └── Escalation → medic_outbreak_stopped (3 days later)
│   │       └── Outcome: +2 Lance Rep, Party +10 Morale (recovery)
│   └── FAILURE
│       └── Escalation → medic_outbreak_spreads (2 days later)
│           └── Outcome: Party -10 Morale, -5% troops, +Medical Risk (2)
│
├── [Treat symptoms] (Medicine 20+) → Safe but slow
│   └── Outcome: +10 Medicine XP, illness lingers (no crisis)
│
└── [Request lord's surgeon] (Standard)
    └── Outcome: Surgeon helps, +5 Medicine XP
```
**Escalation**: YES (outbreak contained or spreads)
**Follow-up**: YES (2-3 days later, always)

---

### Event: medic_experimental_treatment (Officer)
**Trigger**: Field Medic duty holder, T5+, after major battle
**Tier Gate**: T5-T6
**Chance**: 10% post-battle
**Cooldown**: 14 days

```
incident_medic_experimental_treatment
├── [Try new technique] (Medicine 40+, Intelligence 30+) → RISKY
│   ├── SUCCESS
│   │   ├── Immediate: +30 Medicine XP, +Renown, technique works
│   │   └── Escalation → medic_technique_recognized (7 days later)
│   │       └── Outcome: +Lord Favor, +100 Gold (lord rewards innovation)
│   └── FAILURE: Patient dies, -3 Lance Rep, +Medical Risk (3)
│
├── [Use proven methods] (Medicine 30+) → Safe
│   └── SUCCESS: +20 Medicine XP, patient recovers
│
└── [Consult with surgeon] (Standard)
    └── Outcome: Combined effort, +15 Medicine XP
```
**Escalation**: Conditional (success only)
**Follow-up**: YES (7 days later, if successful)

---

## Work Detail Events (No Specific Duty)

### Event: work_detail_rusty_weapon
**Trigger**: Work Detail schedule activity
**Tier Gate**: T1+
**Chance**: 20% on execution
**Cooldown**: 3 days

```
incident_work_detail_rusty_weapon
├── [Repair properly] (Smithing 30+) → Cost: 2 Fatigue
│   ├── SUCCESS
│   │   ├── Immediate: +15 Smithing XP, -2 Fatigue
│   │   └── Escalation → work_detail_praise (2 hours later)
│   │       └── Outcome: +50 Gold, +1 Lance Rep, "Fine work"
│   └── FAILURE
│       └── Escalation → work_detail_broken_weapon (2 hours later)
│           └── Outcome: -20 Gold, -1 Lance Rep, +5 Fatigue (punishment)
│
├── [Quick patch] (Standard) → +5 Smithing XP
│
└── [Leave for smith] (Safe) → No effects
```
**Escalation**: YES (praise or punishment)
**Follow-up**: YES (2 hours later, always if repaired)

---

### Event: work_detail_wagon_wheel
**Trigger**: Work Detail schedule activity
**Tier Gate**: T1+
**Chance**: 20% on execution
**Cooldown**: 5 days

```
incident_work_detail_wagon_wheel
├── [Reinforce wheel] (Engineering 25+) → Cost: 2 Fatigue
│   ├── SUCCESS
│   │   ├── Immediate: +20 Engineering XP, -2 Fatigue
│   │   └── Escalation → work_detail_wheel_holds (4 hours, IF traveling)
│   │       └── Outcome: +1 Lance Rep, +15 Engineering XP, "Good work"
│   └── FAILURE
│       └── Escalation → work_detail_wheel_breaks (2 hours, IF traveling)
│           └── Outcome: -2 Lance Rep, Party -1 Morale, "Your fault!"
│
├── [Temporary fix] (Standard) → 50% chance breaks later
│
└── [Report to wheelwright] (Safe) → Wheelwright fixes it
```
**Escalation**: Conditional (requires traveling)
**Follow-up**: YES (2-4 hours later, if traveling)

---

### Event: work_detail_sharpening
**Trigger**: Work Detail schedule activity
**Tier Gate**: T1+
**Chance**: 20% on execution
**Cooldown**: 2 days

```
incident_work_detail_sharpening
├── [Do it right] (Smithing 20+) → Cost: 2 Fatigue
│   ├── SUCCESS
│   │   ├── Immediate: +15 Smithing XP, -2 Fatigue
│   │   └── Escalation → work_detail_sharp_weapons (ONLY IF battle in 24hr)
│   │       └── Outcome: +1 Lance Rep, +10 Charm XP, "My blade cut true"
│   └── FAILURE: -2 Fatigue, no follow-up
│
├── [Rush it] (Standard) → +5 Smithing XP
│
└── [Half-ass it] (Roguery 15+)
    ├── SUCCESS: Get away with it, 0 Fatigue
    └── FAILURE: Caught, -1 Lance Rep, +2 Fatigue (punishment)
```
**Escalation**: Conditional (requires battle within 24hr)
**Follow-up**: YES (during/after battle, very conditional)

---

## Patrol Duty Events (No Specific Duty)

### Event: patrol_suspicious_tracks
**Trigger**: Patrol Duty schedule activity
**Tier Gate**: T1+
**Chance**: 20% on execution
**Cooldown**: 4 days

```
incident_patrol_suspicious_tracks
├── [Investigate carefully] (Scouting 35+) → Cost: 2 Fatigue
│   ├── SUCCESS
│   │   ├── Immediate: +20 Scouting XP, -2 Fatigue
│   │   └── Escalation → patrol_bandit_camp_found (1 hour later)
│   │       ├── [Report location] → +2 Lance Rep, +20 Gold reward
│   │       ├── [Set ambush] (Tactics 30+)
│   │       │   ├── SUCCESS: +5 Lance Rep, +50 Gold, +Renown
│   │       │   └── FAILURE: Wounded (Bruised), no rewards
│   │       └── [Ignore] → -1 Lance Rep if discovered
│   └── FAILURE: Got lost, -2 Fatigue, no follow-up
│
├── [Follow at distance] (Scouting 20+)
│   ├── SUCCESS
│   │   ├── Immediate: +10 Scouting XP
│   │   └── Escalation → patrol_refugees_found (1 hour later)
│   │       ├── [Offer passage] (Charm 25+) → +2 Lance Rep, +Charm XP
│   │       ├── [Give food] (Cost: 5g) → +1 Lance Rep, -5 Gold
│   │       └── [Let them go] → No effects
│   └── FAILURE: Lost trail
│
└── [Report to sergeant] (Safe) → +5 Leadership XP
```
**Escalation**: YES (bandit camp OR refugees)
**Follow-up**: YES (1 hour later, branching outcomes)

---

### Event: patrol_lost_traveler
**Trigger**: Patrol Duty schedule activity
**Tier Gate**: T1+
**Chance**: 20% on execution
**Cooldown**: 7 days

```
incident_patrol_lost_traveler
├── [Question him] (Charm 20+ OR Roguery 25+)
│   ├── SUCCESS
│   │   └── Escalation → patrol_spy_revealed (immediate)
│   │       ├── [Chase him] (Athletics 30+)
│   │       │   ├── SUCCESS: +3 Lance Rep, +30 Gold, +Renown
│   │       │   └── FAILURE: He escapes
│   │       ├── [Shoot him] (Bow/Crossbow 40+)
│   │       │   ├── SUCCESS: Kill him, +20 Gold (stolen goods)
│   │       │   └── FAILURE: He escapes
│   │       └── [Let him go] → -1 Lance Rep (failed duty)
│   └── FAILURE: He steals 10 Gold during conversation
│
├── [Escort to camp] (Standard)
│   └── Outcome: He's legitimate, +10 Gold reward, +Charm XP
│
└── [Ignore him] (Safe) → No effects
```
**Escalation**: Conditional (if questioned successfully)
**Follow-up**: YES (immediate, if spy revealed)

---

## Sentry Duty Events (No Specific Duty)

### Event: sentry_night_disturbance
**Trigger**: Sentry Duty schedule activity
**Tier Gate**: T1+
**Chance**: 20% on execution
**Cooldown**: 5 days

```
incident_sentry_night_disturbance
├── [Investigate quietly] (Scouting 25+) → Cost: 1 Fatigue
│   ├── SUCCESS
│   │   └── Escalation → sentry_thief_caught (immediate)
│   │       ├── [Arrest him] → +2 Lance Rep, +10 Gold, +Leadership XP
│   │       ├── [Beat him] (Vigor 25+) → +Roguery XP, -Honor, he flees
│   │       └── [Demand bribe] (Roguery 30+)
│   │           ├── SUCCESS: +30 Gold, +Heat (1), he leaves
│   │           └── FAILURE: He reports you, -2 Lance Rep, +Heat (2)
│   └── FAILURE
│       └── Escalation → sentry_false_alarm (immediate)
│           └── Outcome: Just a dog, no effects
│
├── [Raise alarm] (Standard)
│   ├── 50% Real threat: +1 Lance Rep
│   └── 50% False alarm: -1 Lance Rep (woke everyone)
│
└── [Ignore it] (Risky)
    ├── 30% Nothing happens
    └── 70% Thief steals supplies: -2 Lance Rep
```
**Escalation**: YES (thief caught OR false alarm)
**Follow-up**: YES (immediate, always if investigated)

---

### Event: sentry_officer_inspection
**Trigger**: Sentry Duty schedule activity
**Tier Gate**: T1+
**Chance**: 20% on execution
**Cooldown**: 10 days

```
incident_sentry_officer_inspection
├── [Stand at attention] (Standard) → +5 Leadership XP
│
├── [Engage conversation] (Charm 30+)
│   ├── SUCCESS: +1 Lance Rep, +15 Charm XP (impressed officer)
│   └── FAILURE: No effects (seen as brown-nosing)
│
└── [Act drowsy] (If caught sleeping)
    └── Outcome: -1 Lance Rep, +2 Fatigue (punishment detail)
```
**Escalation**: NONE
**Follow-up**: NONE

---

## Training Drill Events (No Specific Duty)

### Event: training_drill_excellence
**Trigger**: Training Drill schedule activity
**Tier Gate**: T1+
**Chance**: 20% on execution
**Cooldown**: 7 days

```
incident_training_drill_excellence
├── [Thank professionally] (Standard)
│   └── Outcome: +1 Lance Rep, +10 Leadership XP
│
├── [Show off] (Athletics 35+)
│   ├── SUCCESS: +2 Lance Rep, +20 Athletics XP
│   └── FAILURE: Stumble, -1 Lance Rep (embarrassed)
│
└── [Stay humble] (Charm 25+)
    └── Outcome: +15 Charm XP, peers respect you
```
**Escalation**: NONE
**Follow-up**: NONE

---

### Event: training_sparring_challenge
**Trigger**: Training Drill schedule activity
**Tier Gate**: T1+
**Chance**: 20% on execution
**Cooldown**: 5 days

```
incident_training_sparring_challenge
├── [Accept challenge] (One-Handed 30+) → Cost: 2 Fatigue
│   ├── SUCCESS: +2 Lance Rep, +25 One-Handed XP
│   └── FAILURE: -1 Lance Rep, +10 One-Handed XP, Bruised condition
│
├── [Decline politely] (Charm 20+)
│   ├── SUCCESS: +10 Charm XP (avoid fight without shame)
│   └── FAILURE: -2 Lance Rep (seen as coward)
│
└── [Sucker punch] (Roguery 25+)
    ├── SUCCESS: Win dishonorably, -Honor, +Roguery XP
    └── FAILURE: -3 Lance Rep (caught cheating), punished
```
**Escalation**: NONE
**Follow-up**: NONE

---

## Foraging Events (No Specific Duty)

### Event: foraging_hidden_cache
**Trigger**: Foraging schedule activity
**Tier Gate**: T1+
**Chance**: 20% on execution
**Cooldown**: 10 days

```
incident_foraging_hidden_cache
├── [Report to command] (Standard)
│   └── Outcome: +1 Lance Rep, +Steward XP
│
├── [Keep for yourself] (Roguery 20+) → RISKY
│   ├── SUCCESS: +30 Gold, +Food items, +Roguery XP, +Heat (1)
│   └── FAILURE: Caught stealing, -3 Lance Rep, +Heat (3), lose items
│
└── [Share with lance] (Leadership 25+)
    └── Outcome: +2 Lance Rep, +Leadership XP, Party +Morale
```
**Escalation**: NONE
**Follow-up**: NONE

---

# RANDOM MAP INCIDENTS

## Post-Battle Incidents

### Incident: sleeping_sentry (PartyCampLife)
**Trigger**: LeavingBattle
**Tier Gate**: None (any tier)
**Chance**: 10%
**Cooldown**: 7 days

```
incident_sleeping_sentry
├── [Punish him] → -Party Morale
├── [Let it slide] → No effects
└── [Make an example] → -1 troop, +Party Morale
```
**One-time**: YES (no follow-up)

---

### Incident: coin_clipping (PartyCampLife)
**Trigger**: LeavingBattle
**Tier Gate**: T2+
**Chance**: 8%
**Cooldown**: 10 days

```
incident_coin_clipping
├── [Investigate thoroughly] (Roguery 25+)
│   ├── SUCCESS: Find culprit, -1 troop, +10 Gold recovered
│   └── FAILURE: No culprit found, -Party Morale
│
├── [Ignore it] → +Heat (1), problem continues
│
└── [Punish randomly] → -Party Morale, wrong person punished
```
**One-time**: YES

---

### Incident: enlist_wounded_enemy
**Trigger**: LeavingBattle (victory only)
**Tier Gate**: T3+
**Chance**: 5%
**Cooldown**: 14 days

```
incident_enlist_wounded_enemy
├── [Recruit him] (Persuasion 30+)
│   ├── SUCCESS: +1 quality troop, +Charm XP
│   └── FAILURE: He refuses
│
├── [Ransom him] → +Gold (varies by troop tier)
│
└── [Let him go] → +Honor, potential +Relation with enemy lord
```
**One-time**: YES

---

### Incident: honor_slain_foe (PostBattle)
**Trigger**: LeavingBattle
**Tier Gate**: None
**Chance**: 12%
**Cooldown**: 5 days

```
incident_honor_slain_foe
├── [Bury the dead] → +Honor, +Party Morale
├── [Loot bodies] → +Gold, -Honor, +Heat (1)
└── [Leave them] → No effects
```
**One-time**: YES

---

## Camp Life Incidents

### Incident: troops_fight_insult (PartyCampLife)
**Trigger**: LeavingEncounter
**Tier Gate**: None
**Chance**: 15%
**Cooldown**: 3 days

```
incident_troops_fight_insult
├── [Break it up] (Leadership 20+) → +Leadership XP
├── [Side with one] → +Morale for one group, -Morale for other
└── [Punish both] → -Party Morale, +Discipline
```
**One-time**: YES

---

### Incident: donative_demand (PartyCampLife)
**Trigger**: LeavingSettlement
**Tier Gate**: T2+
**Chance**: 10%
**Cooldown**: 10 days

```
incident_donative_demand
├── [Pay them] (Cost: 50g) → -50 Gold, +Party Morale
├── [Refuse] → -Party Morale, potential desertion
└── [Promise later] (Persuasion 25+)
    ├── SUCCESS: Delay payment, no immediate penalty
    └── FAILURE: -Party Morale, -Lance Rep
```
**One-time**: YES

---

### Incident: veteran_mentor (TroopSettlementRelation)
**Trigger**: LeavingVillage
**Tier Gate**: T1-T3 only
**Chance**: 8%
**Cooldown**: 14 days

```
incident_veteran_mentor
├── [Accept training] → +25 Weapon XP, Cost: 2 Fatigue
├── [Politely decline] → No effects
└── [Pay for advanced lessons] (Cost: 20g) → +40 Weapon XP, -20 Gold
```
**One-time**: YES

---

### Incident: job_offer (PartyCampLife)
**Trigger**: LeavingTown
**Tier Gate**: T4+
**Chance**: 5%
**Cooldown**: 20 days

```
incident_job_offer
├── [Let soldier go] → -1 troop, +50 Gold (he pays you)
├── [Refuse offer] → No effects, keep troop
└── [Negotiate better pay] (Persuasion 30+)
    ├── SUCCESS: Keep troop, +Relation with merchant
    └── FAILURE: Troop leaves anyway, no gold
```
**One-time**: YES

---

## Travel Incidents

### Incident: ice_march (HardTravel)
**Trigger**: LeavingEncounter (winter, cold terrain)
**Tier Gate**: None
**Chance**: 12%
**Cooldown**: 5 days

```
incident_ice_march
├── [Push through] (Athletics 25+) → Cost: Party -2 Morale
│   ├── SUCCESS: Make good time, +Athletics XP
│   └── FAILURE: Troops injured, -5% party, +Medical Risk (1)
│
├── [Rest and warm up] → +2 Fatigue, party recovers
│
└── [Find shelter] (Scouting 20+)
    ├── SUCCESS: No penalties, +Scouting XP
    └── FAILURE: No shelter found, -Party Morale
```
**One-time**: YES

---

### Incident: sandstorm_warning (HardTravel)
**Trigger**: LeavingEncounter (desert terrain)
**Tier Gate**: None
**Chance**: 10%
**Cooldown**: 7 days

```
incident_sandstorm_warning
├── [Seek shelter immediately] → Delay travel, no losses
├── [Risk it] (Vigor 30+)
│   ├── SUCCESS: Make it through, +Vigor XP
│   └── FAILURE: Party -Morale, -Supplies
└── [Turn back] → Retreat to last settlement
```
**One-time**: YES

---

### Incident: heat_and_dust (HardTravel)
**Trigger**: LeavingVillage (summer, desert)
**Tier Gate**: None
**Chance**: 15%
**Cooldown**: 3 days

```
incident_heat_and_dust
├── [Ration water] (Steward 20+) → -Party Morale, supplies last
├── [Use extra water] → -Supplies, +Party Morale
└── [Search for water] (Scouting 25+)
    ├── SUCCESS: Find oasis, +Supplies, +Party Morale
    └── FAILURE: No water found, -Party Morale
```
**One-time**: YES

---

## Settlement Incidents

### Incident: soldier_in_debt (TroopSettlementRelation)
**Trigger**: LeavingTown
**Tier Gate**: T2+
**Chance**: 12%
**Cooldown**: 5 days

```
incident_soldier_in_debt
├── [Pay his debt] (Cost: 30g) → -30 Gold, +1 Lance Rep, +Party Morale
├── [Refuse] → -1 Lance Rep, troop may desert later
└── [Negotiate repayment] (Persuasion 25+)
    ├── SUCCESS: Troop agrees to pay back slowly
    └── FAILURE: Troop deserts immediately
```
**One-time**: YES

---

### Incident: wanted_criminal (TroopSettlementRelation)
**Trigger**: LeavingVillage
**Tier Gate**: T1+
**Chance**: 8%
**Cooldown**: 10 days

```
incident_wanted_criminal
├── [Turn him in] → +20 Gold, -1 troop, -Party Morale
├── [Protect him] → +1 Lance Rep, -10 Relation with settlement
└── [Help him flee] (Roguery 25+)
    ├── SUCCESS: He escapes, +1 Lance Rep, +Roguery XP
    └── FAILURE: Both caught, -30 Gold fine, +Heat (2)
```
**One-time**: YES

---

### Incident: local_hero (TroopSettlementRelation)
**Trigger**: LeavingVillage
**Tier Gate**: None
**Chance**: 10%
**Cooldown**: 7 days

```
incident_local_hero
├── [Join celebration] → +Party Morale, +Charm XP, +Settlement Relation
├── [Decline politely] → No effects
└── [Organize bigger feast] (Cost: 50g, Leadership 25+)
    ├── SUCCESS: +Party Morale, +Lance Rep, +Settlement Relation
    └── Outcome: -50 Gold
```
**One-time**: YES

---

## Siege Incidents

### Incident: at_the_breach (Siege)
**Trigger**: DuringSiege (attacker)
**Tier Gate**: T2+
**Chance**: 20%
**Cooldown**: None (per siege)

```
incident_at_the_breach
├── [Lead the assault] (Leadership 30+) → RISKY
│   ├── SUCCESS: Breakthrough, +Renown, +Lord Favor
│   └── FAILURE: Wounded, -Party troops
│
├── [Support the assault] (Standard) → +Leadership XP
│
└── [Hold reserves] (Tactics 25+) → Safer, tactical advantage
```
**One-time**: YES (per siege)

---

### Incident: water_supplies (Siege)
**Trigger**: DuringSiege (attacker)
**Tier Gate**: None
**Chance**: 15%
**Cooldown**: None (per siege)

```
incident_water_supplies
├── [Ration strictly] → -Party Morale, +Supplies last longer
├── [Search for source] (Scouting 30+)
│   ├── SUCCESS: Find well, +Party Morale, +Supplies
│   └── FAILURE: No water found, -Party Morale
└── [Abandon siege] (Persuasion 35+)
    ├── SUCCESS: Convince lord to retreat
    └── FAILURE: Lord refuses, -Lord Favor
```
**One-time**: YES (per siege)

---

### Incident: mining (Siege)
**Trigger**: DuringSiege (attacker)
**Tier Gate**: T3+
**Chance**: 10%
**Cooldown**: None (per siege)

```
incident_mining
├── [Supervise miners] (Engineering 30+)
│   ├── SUCCESS: Tunnel collapses walls, siege advantage
│   └── FAILURE: Tunnel collapses, -Party troops
│
├── [Standard approach] → Engineering progress
│
└── [Abandon plan] → No mining, no risk
```
**One-time**: YES (per siege)

---

# CAMP LIFE EVENTS

## Heat Escalation Events

### Event: heat_warning (Heat = 3)
**Trigger**: Heat threshold reached
**Tier Gate**: None
**Chance**: 100% at threshold
**Cooldown**: Threshold-based

```
heat_warning
├── [Clean up act] → -1 Heat, +Discipline (1)
├── [Ignore warning] → No change
└── [Double down] (Roguery 30+)
    ├── SUCCESS: Continue corrupt activities, +Gold
    └── FAILURE: +1 Heat, caught
```
**Escalation**: Part of Heat chain
**Follow-up**: Leads to heat_shakedown at Heat 5

---

### Event: heat_shakedown (Heat = 5)
**Trigger**: Heat threshold reached
**Tier Gate**: None
**Chance**: 100% at threshold
**Cooldown**: Threshold-based

```
heat_shakedown
├── [Comply with search] → +1 Discipline, 50% chance exposed
│   └── IF exposed: → heat_audit (immediate)
│
├── [Pay off sergeant] (Cost: 100g) → -100 Gold, -2 Heat
│
└── [Create distraction] (Roguery 30+)
    ├── SUCCESS: -1 Heat, +25 Roguery XP
    └── FAILURE: +2 Heat, caught → heat_audit (immediate)
```
**Escalation**: Part of Heat chain
**Follow-up**: Can trigger heat_audit immediately

---

### Event: heat_audit (Heat = 7)
**Trigger**: Heat threshold reached OR caught during shakedown
**Tier Gate**: None
**Chance**: 100% at threshold
**Cooldown**: Threshold-based

```
heat_audit
├── [Confess everything] → -4 Heat, +2 Discipline, -200 Gold (fine)
│
├── [Lie convincingly] (Charm 40+)
│   ├── SUCCESS: -2 Heat, +Charm XP
│   └── FAILURE: → heat_exposed (immediate)
│
└── [Flee / Desert] → Desertion path, end enlistment
```
**Escalation**: Part of Heat chain
**Follow-up**: Can trigger heat_exposed immediately

---

### Event: heat_exposed (Heat = 10)
**Trigger**: Heat threshold reached OR caught lying
**Tier Gate**: None
**Chance**: 100% at threshold
**Cooldown**: Threshold-based

```
heat_exposed
├── [Pay the fine] (Cost: 500g) → -500 Gold, -10 Heat, +5 Discipline
│   └── Outcome: Enlistment continues, but record damaged
│
├── [Accept discharge] → Honorable discharge with penalties
│   └── Outcome: End enlistment, -Renown
│
└── [Resist arrest] → Dishonor discharge
    └── Outcome: End enlistment, permanent reputation loss
```
**Escalation**: Terminal event (ends Heat chain)
**Follow-up**: NONE (chain ends)

---

## Discipline Escalation Events

### Event: discipline_extra_duty (Discipline = 3)
**Trigger**: Discipline threshold reached
**Tier Gate**: None
**Chance**: 100% at threshold
**Cooldown**: Threshold-based

```
discipline_extra_duty
├── [Do the extra duty] → -1 Discipline, +2 Fatigue
│   └── Outcome: Problem resolved
│
├── [Complain to sergeant] (Charm 25+)
│   ├── SUCCESS: Reduced duty, +Charm XP, -1 Discipline
│   └── FAILURE: +1 Discipline, must do duty anyway
│
└── [Skip it] → +2 Discipline, -1 Lance Rep
    └── Escalation: Moves closer to discipline_hearing
```
**Escalation**: Part of Discipline chain
**Follow-up**: Leads to discipline_hearing at Discipline 5

---

### Event: discipline_hearing (Discipline = 5)
**Trigger**: Discipline threshold reached
**Tier Gate**: None
**Chance**: 100% at threshold
**Cooldown**: Threshold-based

```
discipline_hearing
├── [Own your mistakes] → -2 Discipline, +1 Lance Rep
│   └── Outcome: Sergeant respects honesty
│
├── [Make excuses] (Charm 30+)
│   ├── SUCCESS: -1 Discipline, problem minimized
│   └── FAILURE: +1 Discipline, seen as coward
│
└── [Stay silent] → No change, hearing continues
```
**Escalation**: Part of Discipline chain
**Follow-up**: Leads to discipline_blocked at Discipline 7

---

### Event: discipline_blocked (Discipline = 7)
**Trigger**: Discipline threshold reached
**Tier Gate**: None
**Chance**: 100% at threshold
**Cooldown**: Threshold-based

```
discipline_blocked
├── [Accept punishment] → -1 Discipline
│   └── Effect: Promotion blocked for 30 days
│
├── [Appeal to lord] (T3+, Leadership 35+)
│   ├── SUCCESS: -2 Discipline, remove block
│   └── FAILURE: +1 Discipline, block remains
│
└── [Resent authority] → +1 Discipline, -1 Lance Rep
    └── Escalation: Moves toward discipline_discharge_threat
```
**Escalation**: Part of Discipline chain
**Follow-up**: Leads to discipline_discharge_threat at Discipline 10

---

### Event: discipline_discharge_threat (Discipline = 10)
**Trigger**: Discipline threshold reached
**Tier Gate**: None
**Chance**: 100% at threshold
**Cooldown**: Threshold-based

```
discipline_discharge_threat
├── [Beg for mercy] → -3 Discipline, -2 Lance Rep (shamed)
│   └── Outcome: One final chance
│
├── [Request transfer] → Clean discharge
│   └── Outcome: End enlistment, minor penalties
│
└── [Accept fate] → Honorable discharge
    └── Outcome: End enlistment, no major penalties
```
**Escalation**: Terminal event (ends Discipline chain)
**Follow-up**: NONE (chain ends)

---

## Pay Tension Escalation Events

### Event: pay_tension_desperate (Pay Tension = 40)
**Trigger**: Pay Tension threshold reached
**Tier Gate**: None
**Chance**: 100% at threshold
**Cooldown**: Threshold-based

```
pay_tension_desperate
├── [Take a loan] → Starts debt mission chain
│   └── Escalation: → ll_evt_mission_debts (7 days later)
│       ├── [Collect politely] → -10 Pay Tension, +Gold
│       └── [Collect aggressively] → -15 Pay Tension, +Heat (2)
│
├── [Skim from ledger] (Roguery 25+)
│   ├── SUCCESS: +30 Gold, +Heat (2), -5 Pay Tension
│   └── FAILURE: Caught, +Heat (4), -Gold
│
└── [Endure hardship] → +2 Fatigue, +1 Lance Rep, no relief
```
**Escalation**: Part of Pay Tension chain
**Follow-up**: Can trigger debt mission (7 days later)

---

### Event: pay_tension_critical (Pay Tension = 60)
**Trigger**: Pay Tension threshold reached
**Tier Gate**: None
**Chance**: 100% at threshold
**Cooldown**: Threshold-based

```
pay_tension_critical
├── [Desert without penalty] → FREE DESERTION unlocked
│   └── Outcome: Leave service, keep all gear
│
├── [Stay loyal] (Leadership 30+)
│   ├── SUCCESS: -10 Pay Tension, +2 Lance Rep
│   └── FAILURE: No change, tension remains
│
└── [Organize protest] → -5 Pay Tension, +1 Discipline, risk
```
**Escalation**: Part of Pay Tension chain
**Follow-up**: Leads to pay_tension_mutiny at Pay Tension 80

---

### Event: pay_tension_mutiny (Pay Tension = 80)
**Trigger**: Pay Tension threshold reached
**Tier Gate**: None
**Chance**: 100% at threshold
**Cooldown**: Threshold-based

```
pay_tension_mutiny
├── [Join mutiny] → Mutiny mission chain
│   └── Escalation: → ll_evt_mutiny_trial (outcome varies)
│       ├── [Beg mercy] → Possible discharge
│       ├── [Blame others] → +Discipline, +Heat
│       └── [Stand defiant] → Execution OR exile
│
├── [Stay loyal to lord] → -20 Pay Tension, +5 Lance Rep, +Lord Reward
│   └── Outcome: Lord rewards loyalty (gold/promotion)
│
└── [Stay neutral] → +2 Discipline, mutiny continues
```
**Escalation**: Terminal event OR mutiny chain
**Follow-up**: Can trigger mutiny trial

---

## Lance Reputation Events

### Event: lance_bonded (Lance Rep = +40)
**Trigger**: Lance Rep threshold reached
**Tier Gate**: None
**Chance**: 100% at threshold
**Cooldown**: Threshold-based

```
lance_bonded
└── [Continue] → Permanent effect unlocked
    └── Effect: Lance covers for you in combat, +Party Morale bonus
```
**Escalation**: Positive milestone (no chain)
**Follow-up**: NONE (permanent buff)

---

### Event: lance_trusted (Lance Rep = +20)
**Trigger**: Lance Rep threshold reached
**Tier Gate**: None
**Chance**: 100% at threshold
**Cooldown**: Threshold-based

```
lance_trusted
└── [Continue] → Permanent effect unlocked
    └── Effect: Lance supports you in disputes, easier persuasion
```
**Escalation**: Positive milestone (no chain)
**Follow-up**: NONE (permanent buff)

---

### Event: lance_isolated (Lance Rep = -20)
**Trigger**: Lance Rep threshold reached
**Tier Gate**: None
**Chance**: 100% at threshold
**Cooldown**: Threshold-based

```
lance_isolated
├── [Apologize publicly] → +2 Lance Rep, regain trust
│
├── [Win them over] (Charm 35+)
│   ├── SUCCESS: +3 Lance Rep, effort pays off
│   └── FAILURE: No change
│
└── [Ignore them] → No change, isolation continues
```
**Escalation**: Part of Lance Rep chain
**Follow-up**: Leads to lance_sabotage at Lance Rep -40

---

### Event: lance_sabotage (Lance Rep = -40)
**Trigger**: Lance Rep threshold reached
**Tier Gate**: None
**Chance**: 100% at threshold
**Cooldown**: Threshold-based

```
lance_sabotage
├── [Confront them] (Athletics 40+)
│   ├── SUCCESS: +5 Lance Rep (earn respect through strength)
│   └── FAILURE: Beaten, -1 Lance Rep, Wounded
│
├── [Report to leader] → +1 Discipline, +2 Lance Rep
│   └── Outcome: Leader punishes troublemakers
│
└── [Request transfer] → Leave current lance
    └── Outcome: Reassigned, Lance Rep resets to 0
```
**Escalation**: Terminal event (ends negative Lance Rep chain)
**Follow-up**: NONE (chain ends)

---

## Medical Risk Events

### Event: medical_worsening (Medical Risk = 3)
**Trigger**: Medical Risk threshold reached
**Tier Gate**: None
**Chance**: 100% at threshold
**Cooldown**: Threshold-based

```
medical_worsening
├── [Seek treatment] (Cost: 30g) → -30 Gold, -2 Medical Risk
│
├── [Rest] → +3 Fatigue, -1 Medical Risk
│
└── [Ignore it] → +1 Medical Risk
    └── Escalation: Moves toward medical_complication
```
**Escalation**: Part of Medical Risk chain
**Follow-up**: Leads to medical_complication at Medical Risk 4

---

### Event: medical_complication (Medical Risk = 4)
**Trigger**: Medical Risk threshold reached
**Tier Gate**: None
**Chance**: 100% at threshold
**Cooldown**: Threshold-based

```
medical_complication
├── [Expensive treatment] (Cost: 100g) → -100 Gold, -3 Medical Risk
│   └── Outcome: Full recovery
│
├── [Standard treatment] (Cost: 50g) → -50 Gold, -2 Medical Risk
│
└── [Endure] → +1 Medical Risk
    └── Escalation: Moves toward medical_emergency
```
**Escalation**: Part of Medical Risk chain
**Follow-up**: Leads to medical_emergency at Medical Risk 5

---

### Event: medical_emergency (Medical Risk = 5)
**Trigger**: Medical Risk threshold reached
**Tier Gate**: None
**Chance**: 100% at threshold
**Cooldown**: Threshold-based

```
medical_emergency
├── [Emergency surgery] (Cost: 200g) → -200 Gold, -5 Medical Risk
│   └── Outcome: Survive, but permanent debuff (-5 max HP)
│
├── [Accept fate] → Character dies
│   └── Outcome: Game over OR switch to companion
│
└── [Miracle cure] (Medicine 50+, T5+)
    ├── SUCCESS: Full recovery, -5 Medical Risk, no debuff
    └── FAILURE: → Emergency surgery outcome
```
**Escalation**: Terminal event (ends Medical Risk chain)
**Follow-up**: NONE (chain ends)

---

## Fatigue Crisis Events

### Event: fatigue_exhausted (Fatigue = 0)
**Trigger**: Fatigue depleted
**Tier Gate**: None
**Chance**: 100% when depleted
**Cooldown**: Per occurrence

```
fatigue_exhausted
├── [Rest immediately] → Regain 5 Fatigue, miss next activity
│
├── [Push through] (Vigor 30+)
│   ├── SUCCESS: Continue with penalties (-10% combat stats)
│   └── FAILURE: Collapse, Wounded condition
│
└── [Ask for relief] → -1 Lance Rep, regain 3 Fatigue
```
**Escalation**: NONE (immediate resolution)
**Follow-up**: NONE

---

## SUMMARY STATISTICS

**Total Events Documented**: 80+

### Duty Events: 25+ events
- Runner: 3 events (1 chain)
- Scout: 3 events (2 chains)
- Field Medic: 3 events (2 chains)
- Work Detail: 3 events (3 chains)
- Patrol: 2 events (2 chains)
- Sentry: 2 events (1 chain)
- Training: 2 events (0 chains)
- Foraging: 1 event (0 chains)
- *Need content: Quartermaster, Armorer, Engineer, Lookout, Navigator, Boatswain, Messenger*

### Random Map Incidents: 25+ incidents
- Post-Battle: 4 incidents
- Camp Life: 5 incidents
- Travel: 3 incidents
- Settlement: 3 incidents
- Siege: 3 incidents
- *Native has 100+ incidents to draw inspiration from*

### Camp Life Events: 30+ events
- Heat Escalation: 4 events (full chain)
- Discipline Escalation: 4 events (full chain)
- Pay Tension Escalation: 3 events (partial chain)
- Lance Reputation: 4 events (milestone + chain)
- Medical Risk: 3 events (full chain)
- Fatigue: 1 event

### Event Chain Depth
- No chain: 35 events
- 2-event chains: 15 chains
- 3-4 event chains: 8 chains
- 5+ event chains: 3 chains (Heat, Discipline, Pay Tension)

### Conditional Triggers
- Battle required: 5 events
- Travel required: 2 events
- Siege required: 3 events
- Time-delayed: 20+ events

---

## CONTENT GAPS (Need Creation)

### Missing Duty Events
1. **Quartermaster Duty** (0 events - need 3+)
2. **Armorer Duty** (0 events - need 3+)
3. **Engineer Duty** (0 events - need 3+)
4. **Lookout Duty** (0 events - need 3+)
5. **Navigator Duty** (0 events - need 3+)
6. **Boatswain Duty** (0 events - need 3+)
7. **Messenger Duty** (0 events - need 3+)

### Tier Distribution
- T1-T2 (Enlisted): Good coverage
- T3-T4 (NCO): Some gaps, need more leadership events
- T5-T6 (Officer): Limited events, need strategic/command events

### Random Map Incidents
- Need more variety (native has 100+)
- Need naval-specific incidents
- Need more settlement variety incidents

---

## NEXT STEPS

1. **Fill missing duty event content** (7 duties need events)
2. **Create more T5-T6 officer events** (leadership/strategy focused)
3. **Expand random map incident variety** (draw from native incidents)
4. **Test event firing rates** (ensure 20% chance feels right)
5. **Balance rewards/consequences** (XP, Gold, Rep values)
6. **Add conditional chain variety** (more battle/travel/siege conditionals)

---

# APPENDIX

## Schema Reference

### Event Definition Schema

All story blocks (events, map incidents, escalation events) use the **Lance Life Events** schema defined in:
- **Schema File**: `src/Features/Lances/Events/LanceLifeEventCatalog.cs`
- **Event Files**: `ModuleData/Enlisted/Events/*.json`
- **Schema Version**: 1 (see `schema_version.json`)

### Core Schema Structure

```json
{
  "id": "event_unique_id",
  "category": "duty | decision | threshold",
  "delivery": {
    "method": "automatic | player_initiated",
    "channel": "inquiry | incident | menu",
    "schedule_trigger": "on_activity_execution",  // NEW: For duty events
    "activity_trigger": "work_detail",             // Optional: Specific activity
    "incident_trigger": "LeavingBattle",           // For native map incidents
    "menu_section": "training | social | combat"   // For player-initiated
  },
  "triggers": {
    "all": ["is_enlisted", "has_duty:runner"],
    "any": ["daily_tick", "battle_won"],
    "none": ["flag_already_happened"],             // Blocks if flag exists
    "time_of_day": ["morning", "afternoon"],
    "escalation_requirements": {
      "heat": { "min": 3, "max": 7 },
      "discipline": { "min": 5 }
    }
  },
  "requirements": {
    "duty": "runner | scout | field_medic | any",
    "formation": "infantry | cavalry | any",
    "tier": { "min": 1, "max": 6 }
  },
  "timing": {
    "cooldown_days": 3,
    "priority": "normal | high | critical",
    "one_time": false
  },
  "content": {
    "title": "Event Title",
    "setup": "Event description and context...",
    "options": [
      {
        "id": "option_id",
        "text": "Option text",
        "risk": "safe | risky | corrupt",
        "costs": { "fatigue": 2, "gold": 10 },
        "rewards": { "xp": { "athletics": 30 }, "gold": 20 },
        "effects": { "heat": 1, "lance_reputation": 2 },
        "outcome": "What happens when player selects this",
        
        // Event chaining (follow-up events)
        "chains_to": "follow_up_event_id",
        "chain_delay_hours": 2.0,
        
        // Story flags (for conditionals)
        "set_flags": ["flag_name"],
        "clear_flags": ["old_flag"],
        "flag_duration_days": 1.0,  // 0 = permanent
        
        // Injury/illness risks
        "injury_risk": {
          "chance": 10,
          "severity": "minor | moderate | severe",
          "type": "wound | strain | bruise"
        }
      }
    ]
  }
}
```

### Three Event Types

**1. Duty Events** (Schedule-triggered)
```json
{
  "delivery": {
    "method": "automatic",
    "channel": "incident",
    "schedule_trigger": "on_activity_execution"
  },
  "requirements": {
    "duty": "runner"  // Or use activity_trigger instead
  }
}
```

**2. Random Map Incidents** (Native-triggered)
```json
{
  "delivery": {
    "method": "automatic",
    "channel": "incident",
    "incident_trigger": "LeavingBattle"
  }
}
```

**3. Camp Life Events** (Escalation-triggered)
```json
{
  "triggers": {
    "escalation_requirements": {
      "heat": { "min": 5 }
    }
  }
}
```

### Event Chaining (Follow-ups)

Use `chains_to` to create event sequences:

```json
{
  "options": [
    {
      "id": "repair_weapon",
      "chains_to": "weapon_repair_outcome",
      "chain_delay_hours": 2.0,
      "set_flags": ["weapon_repaired"],
      "flag_duration_days": 1.0
    }
  ]
}
```

Follow-up event:
```json
{
  "id": "weapon_repair_outcome",
  "triggers": {
    "all": ["weapon_repaired"]  // Must have flag
  }
}
```

### Conditional Chains

Use flags to make chains conditional:

```json
{
  "options": [
    {
      "id": "reinforce_wheel",
      "chains_to": "wheel_holds_check",
      "chain_delay_hours": 4.0,
      "set_flags": ["wheel_reinforced"],
      "flag_duration_days": 1.0
    }
  ]
}
```

Follow-up with condition:
```json
{
  "id": "wheel_holds_check",
  "triggers": {
    "all": ["wheel_reinforced", "traveling"]  // Only if traveling
  }
}
```

### Configuration Files

**Event Pool Mapping** (connects activities to events):
- **File**: `ModuleData/Enlisted/duty_event_pools.json`
- **Purpose**: Maps schedule activities to event pools with weights
- **Format**: See example below

```json
{
  "activity_id": "work_detail",
  "simple_completion_chance": 0.80,
  "simple_completion": {
    "xp": { "engineering": 10 },
    "fatigue_cost": 1
  },
  "event_pool": [
    { "event_id": "rusty_weapon_found", "weight": 1.0 },
    { "event_id": "wagon_wheel_broken", "weight": 0.8 }
  ]
}
```

### Creating New Events

1. **Choose event type** (duty/map incident/escalation)
2. **Set delivery method** (schedule_trigger, incident_trigger, or escalation_requirements)
3. **Define requirements** (tier, duty, formation)
4. **Write content** (title, setup, options)
5. **Add to event file** (`events_duty_*.json` or `events_general.json`)
6. **Add to mapping** (if duty event, add to `duty_event_pools.json`)
7. **Test cooldowns** (ensure events don't spam)

### Example: New Duty Event

```json
{
  "id": "quartermaster_supply_shortage",
  "category": "duty",
  "delivery": {
    "method": "automatic",
    "channel": "incident",
    "schedule_trigger": "on_activity_execution"
  },
  "triggers": {
    "all": ["is_enlisted", "has_duty:quartermaster"],
    "time_of_day": ["morning"]
  },
  "requirements": {
    "duty": "quartermaster",
    "tier": { "min": 1, "max": 6 }
  },
  "timing": {
    "cooldown_days": 5,
    "priority": "normal"
  },
  "content": {
    "title": "Supply Shortage",
    "setup": "The quartermaster's ledger shows a discrepancy...",
    "options": [
      {
        "id": "investigate",
        "text": "Investigate the shortage",
        "risk": "safe",
        "costs": { "fatigue": 2 },
        "rewards": { "xp": { "steward": 25 } },
        "outcome": "You find the missing supplies."
      }
    ]
  }
}
```

---

## Event Design & Writing Guidelines

### Design Principles

#### 1. Player Agency Matters

**DO:** Give meaningful choices with clear trade-offs  
**DON'T:** Force single-path outcomes or fake choices

**Good Example:**
```
Challenge: Supply shortage before battle
Option A: Requisition from allies (delays battle, reliable)
Option B: Buy at premium (costs gold, immediate)
Option C: "Borrow" without permission (Heat risk, immediate)

All three work. Each has different costs/benefits.
```

**Bad Example:**
```
Challenge: Supply shortage
Option A: Find supplies (only real option)
Option B: Don't find supplies (obviously bad, no one picks this)

Player has no real choice.
```

#### 2. Consequences Must Be Real

**Every choice should have:**
- **Immediate Effect:** XP gain, resource change, time cost
- **System Impact:** Heat/Discipline/Rep/Medical Risk/Fatigue
- **Narrative Weight:** Outcome text that acknowledges choice

**Example:**
```
Choice: "Report corruption in supply chain"
Immediate: +40 Leadership XP, +20 Charm XP
System: Heat -3 (cleaning up), Lance Rep +10 (respected)
Narrative: "The quartermaster is dismissed. The lord thanks you personally. 
           Your lance mates nod with approval—they knew something was wrong."

vs.

Choice: "Ignore corruption, take a cut"
Immediate: +10 Trade XP, +100 Gold
System: Heat +4 (complicit), Lance Rep -15 (sellout)
Narrative: "You pocket the gold. Easy money. But you catch {LANCE_MATE} 
           watching you with cold eyes. They know."
```

#### 3. Context Creates Drama

**Events should reference:**
- Lord's current objective
- Recent battles or losses
- Lance member names (when relevant)
- Army morale and conditions
- Player's rank/tier progression

**Good (Contextual):**
```
"Your lance is already down two men from the last battle. {LANCE_LEADER_SHORT} 
looks exhausted. Now the lord wants us on night patrol again. You're the 
quartermaster—the supplies won't manage themselves. But if you don't go on 
patrol, who will cover your spot?"
```

**Bad (Generic):**
```
"You have duties to perform. Choose what to do."
```

#### 4. Vary Tone and Stakes

**Not every event should be life-or-death.**

**Event Tone Mix (Per Duty):**
- 30% Low Stakes: Routine tasks, skill checks, minor problems
- 50% Medium Stakes: Meaningful choices, reputation impacts, resource trade-offs
- 20% High Stakes: Career-defining moments, major consequences, injury risks

**Examples:**
- **Low Stakes:** "The lance cook burned the stew. Do you help fix it or ignore it?"
- **Medium Stakes:** "The lord's advisor wants a report on morale. Be honest or optimistic?"
- **High Stakes:** "The lord's horse is lame before battle. Blame the stable master or take responsibility?"

---

### Writing Guidelines

#### Voice and Tone

**Style:** Gritty military realism with human moments

**DO:**
- Use military vocabulary naturally (muster, formation, requisition)
- Show physical details (tired eyes, muddy boots, bloody bandages)
- Include soldier banter and dark humor
- Reference weather, time of day, physical conditions
- Keep it brief (2-3 paragraphs max)

**DON'T:**
- Be overly formal or Shakespearean
- Use modern slang or anachronisms
- Write long exposition dumps
- Explain mechanics in-character ("This will cost you 3 fatigue")
- Break immersion with meta-references

**Good Voice:**
```
The supply wagons reek of spoiled grain. You're three days from the nearest 
town and the men are already grumbling about short rations. {LANCE_LEADER_SHORT} 
wants an explanation. You've got two choices: admit someone's been skimming, 
or blame the heat.
```

**Bad Voice:**
```
Greetings, Quartermaster! It appears thy supplies have been compromised by 
nefarious forces! Prithee, wouldst thou investigate this matter posthaste, 
lest the men grow wroth?
```

#### Option Text Format

**Structure:** `[Action Type] "Direct player speech or action description"`

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

#### Outcome Text Guidelines

**Structure:** Consequence → Immediate Result → Future Implication

**Example:**
```
You confront the lance mate. They confess to taking extra rations to sell in 
town. (Consequence)

{LANCE_LEADER_SHORT} commends you for catching the thief, but the rest of the 
lance goes quiet. No one likes a snitch, even when they're right. (Immediate)

You've earned the sergeant's respect—but lost the men's trust. (Future)
```

#### Placeholder Variables

Use these to personalize events:

**Always Available:**
- `{PLAYER_NAME}` - Player's name
- `{LORD_NAME}` - Enlisted lord
- `{LANCE_NAME}` - Player's lance unit
- `{LANCE_LEADER_SHORT}` - Lance leader (short name/rank)
- `{FACTION_NAME}` - Lord's faction

**Context-Specific (use only when trigger guarantees they exist):**
- `{ENEMY_FACTION}`, `{ENEMY_LORD}` - Only for combat/pursuit events
- `{BESIEGED_SETTLEMENT}` - Only during sieges
- `{INJURY_TYPE}`, `{INJURY_LOCATION}` - Only for injury events
- `{SHIP_NAME}`, `{CAPTAIN_NAME}` - Only for naval events

**Rules:**
1. Only use context-specific placeholders when the event trigger guarantees they exist
2. Use 1-2 placeholders per paragraph maximum
3. Write naturally - placeholders should fit speech patterns
4. When in doubt, use generic text over a potentially broken placeholder

