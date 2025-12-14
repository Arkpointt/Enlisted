# Systems and Story Blocks Reference

**Purpose:** This document provides a complete reference of all game systems (Heat, Discipline, Pay Tension, etc.) and the story blocks/events that affect them. Use this to understand system interconnections and ensure new content integrates properly.

**Last Updated:** December 13, 2025  
**Version:** 1.0

---

## Table of Contents

1. [Quick Reference: All Systems](#quick-reference-all-systems)
2. [System Details](#system-details)
   - [Heat (Corruption Attention)](#heat-corruption-attention)
   - [Discipline](#discipline)
   - [Pay Tension](#pay-tension)
   - [Lance Reputation](#lance-reputation)
   - [Medical Risk](#medical-risk)
   - [Fatigue](#fatigue)
3. [Story Block Packs](#story-block-packs)
4. [Event Categories](#event-categories)
5. [Cross-System Effects Matrix](#cross-system-effects-matrix)
6. [Adding New Content](#adding-new-content)

---

## Quick Reference: All Systems

| System | Type | Range | Primary Triggers | Key Effects | Decay Rate |
|--------|------|-------|-----------------|-------------|-----------|
| **Heat** | Escalation | 0-10 | Corruption, bribes, theft | Investigations, confiscation | -1 per 7 days |
| **Discipline** | Escalation | 0-10 | Rule-breaking, infractions | Extra duty, blocked promotion | -1 per 14 days |
| **Pay Tension** | Escalation | 0-100 | Late payday | Desperate actions, mutiny | Varies by events |
| **Lance Reputation** | Social | -50 to +50 | Lance interactions | Support/isolation | ±1 per 14 days to 0 |
| **Medical Risk** | Health | 0-5 | Untreated conditions | Worsening, complications | -1 per day (with rest) |
| **Fatigue** | Resource | 0-30+ | Activities, duties | Performance, availability | Natural recovery |

---

## System Details

### Heat (Corruption Attention)

**What it tracks:** Attention from corrupt activities — skimming supplies, taking bribes, contraband, falsifying records.

**File:** `ModuleData/Enlisted/enlisted_config.json` → `escalation.heat_decay_interval_days`

#### Thresholds

| Level | Heat Value | State | Effect |
|-------|-----------|-------|--------|
| Clean | 0 | No attention | No effects |
| Watched | 1-2 | Mild suspicion | Warning text in events |
| Noticed | 3-4 | Active suspicion | Threshold event: `heat_warning` |
| Hot | 5-6 | Investigation likely | Threshold event: `heat_shakedown` |
| Burning | 7-9 | Active investigation | Threshold event: `heat_audit` |
| Exposed | 10 | Caught | Threshold event: `heat_exposed` |

#### How Heat Increases

| Source | Heat Gained | Example Story Blocks |
|--------|-------------|---------------------|
| Minor corruption | +1 | Looking the other way |
| Moderate corruption | +2 | Adjusting ledger numbers |
| Major corruption | +3-4 | Taking bribes, stealing equipment |
| Witnessed corruption | +1-2 (bonus) | Someone saw you |

#### Story Blocks That Add Heat

**Story Pack: `corruption.json`**
- `corruption.ledger_skim_v1` → "Slip him a little" option: **+2 Heat**

**Event Pack: `events_pay_tension.json`**
- `pay_tension_loot_the_dead` → "Search enemy dead": **+2 Heat**
- `pay_tension_loot_the_dead` → "Search everyone": **+4 Heat**
- `pay_tension_loot_the_dead` → "Take a weapon": **+1 Heat**
- `pay_tension_theft_invitation` → "Join theft": **+3 Heat**
- `pay_tension_theft_invitation` → "Keep watch": **+2 Heat**

#### Story Blocks That Reduce Heat

**Story Pack: `corruption.json`**
- `corruption.ledger_skim_v1` → "Refuse. Pay posted price": **-1 Heat**

**Event Pack: `events_escalation_thresholds.json`**
- `heat_warning` → "clean up act": **-1 Heat**
- `heat_shakedown` → successful clean inspection: **-2 Heat**

#### Threshold Events

| Event ID | Threshold | Description | File |
|----------|-----------|-------------|------|
| `heat_warning` | Heat ≥ 3 | Lance leader warns you privately | `events_escalation_thresholds.json` |
| `heat_shakedown` | Heat ≥ 5 | Surprise kit inspection | `events_escalation_thresholds.json` |
| `heat_audit` | Heat ≥ 7 | Formal audit of supplies | `events_escalation_thresholds.json` |
| `heat_exposed` | Heat = 10 | Caught red-handed, consequences | `events_escalation_thresholds.json` |

#### Decay

- **Passive:** -1 Heat per 7 days with no corrupt choices
- **Active Recovery:**
  - Report corruption: -2 Heat
  - Refuse bribe publicly: -1 Heat
  - Pass audit cleanly: -3 Heat

---

### Discipline

**What it tracks:** Accumulated rule-breaking, insubordination, and duty failures.

**File:** `ModuleData/Enlisted/enlisted_config.json` → `escalation.discipline_decay_interval_days`

#### Thresholds

| Level | Discipline Value | State | Effect |
|-------|-----------------|-------|--------|
| Clean | 0 | Good standing | No effects |
| Minor marks | 1-2 | On notice | Warning text |
| Troubled | 3-4 | Extra duty likely | Threshold event: `discipline_extra_duty` |
| Serious | 5-6 | Hearing possible | Threshold event: `discipline_hearing` |
| Critical | 7-9 | Promotion blocked | Threshold event: `discipline_blocked` |
| Breaking | 10 | Facing discharge | Threshold event: `discipline_discharge` |

#### How Discipline Increases

| Source | Discipline Gained | Example Story Blocks |
|--------|------------------|---------------------|
| Minor infraction | +1 | Late to muster, sloppy work |
| Moderate infraction | +2 | Sleeping on watch, disobeying order |
| Major infraction | +3 | Fighting, desertion attempt, theft |
| Caught by officer | +1 (bonus) | Witnessed by authority |

#### Story Blocks That Add Discipline

**Event Pack: `events_pay_tension.json`**
- `pay_tension_loot_the_dead` → failed looting attempt: **+variable Discipline**
- Multiple pay tension events can trigger discipline through consequences

#### Story Blocks That Reduce Discipline

**Event Pack: `events_escalation_thresholds.json`**
- `discipline_blocked` → "I understand. I'll earn it": **-1 Discipline**
- `discipline_blocked` → successful appeal: **-2 Discipline**
- `discipline_extra_duty` → completing duty: **-1 Discipline**

#### Threshold Events

| Event ID | Threshold | Description | File |
|----------|-----------|-------------|------|
| `discipline_extra_duty` | Discipline ≥ 3 | Assigned unpleasant extra duties | `events_escalation_thresholds.json` |
| `discipline_hearing` | Discipline ≥ 5 | Formal hearing, defend yourself | `events_escalation_thresholds.json` |
| `discipline_blocked` | Discipline ≥ 7 | Promotion blocked until improved | `events_escalation_thresholds.json` |
| `discipline_discharge` | Discipline = 10 | Facing discharge, final chance | `events_escalation_thresholds.json` |

#### Decay

- **Passive:** -1 Discipline per 14 days with no infractions
- **Active Recovery:**
  - Complete extra duty: -1 Discipline
  - Take blame for lance mate: -2 Discipline (but +Lance Rep)
  - Volunteer for dangerous duty: -2 Discipline

---

### Pay Tension

**What it tracks:** Soldier desperation when pay is delayed. Primary driver of mutiny and desertion storylines.

**File:** `ModuleData/Enlisted/enlisted_config.json` → `camp_life.pay_tension_high_threshold` (70)

#### Thresholds

| Level | Pay Tension Value | State | Available Actions |
|-------|------------------|-------|-------------------|
| Content | 0-19 | Pay on time | Standard actions only |
| Grumbling | 20-39 | Complaints begin | Grumbling events unlock |
| Concerned | 40-49 | Looking for solutions | Corrupt/Loyal paths unlock |
| Desperate | 50-69 | Willingness to risk | More extreme options |
| Critical | 70-84 | Desertion talk | Mutiny events possible |
| Explosive | 85-100 | Breaking point | Full mutiny events |

#### How Pay Tension Increases

| Source | Tension Increase | Notes |
|--------|-----------------|-------|
| Payday missed | +automatic | System-driven by finance system |
| Battle without pay | +variable | Compounds frustration |
| Lord relation low | +indirect | Makes tension worse |

#### Story Blocks Triggered by Pay Tension

**Event Pack: `events_pay_tension.json`**

| Event ID | Threshold | Category | Description |
|----------|-----------|----------|-------------|
| `pay_tension_grumbling` | Tension ≥ 20 | Inquiry | Complaints around fire, choose stance |
| `pay_tension_loot_the_dead` | Tension ≥ 50 | Inquiry | Loot battlefield dead for coin |
| `pay_tension_theft_invitation` | Tension ≥ 45 | Inquiry | Invited to steal supplies |
| `pay_tension_confrontation` | Tension ≥ 60 | Inquiry | Delegation to paymaster |
| `pay_tension_mutiny_brewing` | Tension ≥ 85 | Inquiry | Full mutiny event |

**Event Pack: `events_pay_loyal.json`**
- Missions to help lord in exchange for favor/pay
- Available when Pay Tension ≥ 40

**Event Pack: `events_pay_mutiny.json`**
- Final stages of mutiny path
- Available when Pay Tension ≥ 85

#### Effects on Other Systems

Pay Tension events often:
- **Add Heat:** through theft, looting, corruption
- **Add/Remove Discipline:** through insubordination or loyalty
- **Change Lance Reputation:** through solidarity or betrayal

---

### Lance Reputation

**What it tracks:** How your lance mates view you — trust, respect, belonging.

**File:** `ModuleData/Enlisted/enlisted_config.json` → `escalation.lance_rep_decay_interval_days`

#### Thresholds

| Level | Reputation Value | State | Effect |
|-------|-----------------|-------|--------|
| Bonded | +40 to +50 | Family | Lance mates cover for you, warn you, follow lead |
| Trusted | +20 to +39 | Solid | Good event options, help available |
| Accepted | +5 to +19 | Normal | Standard treatment |
| Neutral | -4 to +4 | New/Unknown | No special treatment |
| Disliked | -19 to -5 | Cold | Worse options, no help offered |
| Outcast | -39 to -20 | Isolated | Excluded, blamed first |
| Hated | -50 to -40 | Enemy | Active sabotage, set up to fail |

#### How Lance Reputation Changes

| Source | Reputation Change | Example Story Blocks |
|--------|------------------|---------------------|
| Help lance mate | +2 to +5 | Cover for them, share supplies |
| Reliable duty | +1 | Consistent good work |
| Social bonding | +1 to +3 | Fire circle, drinking, stories |
| Selfish choice | -2 to -5 | Take best gear, blame others |
| Betray lance mate | -5 to -10 | Snitch, let them take fall |
| Cowardice | -3 to -5 | Abandon in danger |

#### Story Blocks That Affect Lance Reputation

**Story Pack: `morale.json`**
- `morale.campfire_song_v1` → leading song builds small reputation
- `morale.after_battle_words_v1` → steadying lance builds trust

**Event Pack: `events_pay_tension.json`**
- `pay_tension_grumbling` → "Join grumbling": **+5 Lance Rep**
- `pay_tension_grumbling` → "Defend lord": **-5 Lance Rep**
- `pay_tension_grumbling` → "Rabble rouse": **+10 Lance Rep**
- `pay_tension_confrontation` → "Join delegation": **+15 Lance Rep**
- `pay_tension_confrontation` → "Lead delegation": **+25 Lance Rep**
- `pay_tension_confrontation` → "Talk them down": **-10 Lance Rep**
- `pay_tension_confrontation` → "Warn officers": **-30 Lance Rep**
- `pay_tension_theft_invitation` → "Report them": **-25 Lance Rep**
- `pay_tension_mutiny_brewing` → "Join mutiny": **+30 Lance Rep**
- `pay_tension_mutiny_brewing` → "Try to stop": **-40 Lance Rep**
- `pay_tension_mutiny_brewing` → "Stand with officers": **-50 Lance Rep**

#### Threshold Events

| Event ID | Threshold | Description | File |
|----------|-----------|-------------|------|
| `lance_trusted` | Rep ≥ +20 | Lance mate shares secret, asks help | `events_escalation_thresholds.json` |
| `lance_bonded` | Rep ≥ +40 | Lance mates cover for mistakes | `events_escalation_thresholds.json` |
| `lance_isolated` | Rep ≤ -20 | Excluded from fire circle | `events_escalation_thresholds.json` |
| `lance_sabotage` | Rep ≤ -40 | Equipment "lost", set up | `events_escalation_thresholds.json` |

#### Decay

- **Passive:** Slowly trends toward 0 (±1 per 14 days)
- **Active Change:**
  - Help wounded lance mate: +3 Rep
  - Share supplies when short: +2 Rep
  - Stand up for lance mate: +3 Rep
  - Betray or inform: -5 to -30 Rep

---

### Medical Risk

**What it tracks:** Danger from untreated or ignored medical conditions.

**File:** `ModuleData/Enlisted/enlisted_config.json` → `escalation.medical_risk_decay_interval_days`

#### Thresholds

| Level | Risk Value | State | Effect |
|-------|-----------|-------|--------|
| None | 0 | Healthy or treated | No effects |
| Mild | 1-2 | Ignorable | Warning text in events |
| Concerning | 3 | Needs attention | Threshold event: `medical_worsening` |
| Serious | 4 | Worsening likely | Threshold event: `medical_complication` |
| Critical | 5 | Emergency | Threshold event: `medical_emergency` |

#### How Medical Risk Increases

| Source | Risk Gained | Example Story Blocks |
|--------|-------------|---------------------|
| Untreated injury (per day) | +1 | Ignored wound |
| Untreated illness (per day) | +1 | Ignored sickness |
| Training while injured | +1 | Pushed too hard |
| Refused treatment | +2 | "I'm fine" when not fine |

#### Story Blocks That Can Cause Medical Risk

**Story Pack: `training.json`**
- `training.lance_drill_night_sparring_v1` → "Lead the drill": **5% injury chance**
  - If injury occurs: sets initial Medical Risk

**Story Pack: `medical.json`**
- Events about treating or ignoring medical situations
- Choosing to skip treatment increases risk

#### Threshold Events

| Event ID | Threshold | Description | File |
|----------|-----------|-------------|------|
| `medical_worsening` | Risk ≥ 3 | Condition severity increases | `events_escalation_thresholds.json` |
| `medical_complication` | Risk ≥ 4 | New complication (infection, fever) | `events_escalation_thresholds.json` |
| `medical_emergency` | Risk = 5 | Collapse, forced bed rest | `events_escalation_thresholds.json` |

#### Decay

- **Passive:** Resets to 0 when condition is treated
- **Active Recovery:**
  - Rest (skip activities): -1 per day
  - Seek treatment: Reset to 0
- **Note:** Does NOT decay while condition persists untreated

---

### Fatigue

**What it tracks:** Player energy for activities. Resource management system.

**File:** Multiple systems reference this

#### Typical Range

- **Normal:** 0-10
- **Tired:** 11-20
- **Exhausted:** 21-30+
- **Probation Cap:** 18 (during probation period)

#### How Fatigue Increases

| Source | Fatigue Cost | Example Story Blocks |
|--------|-------------|---------------------|
| Simple activities | 0-1 | Watching, listening |
| Moderate activities | 1-2 | Helping, training |
| Demanding activities | 2-3 | Leading drills, intense work |
| Duties | Variable | QM duties, scouting, etc. |

#### Story Blocks That Cost Fatigue

**Story Pack: `discipline.json`**
- `discipline.mess_line_fight_v1` → "Step in": **1 Fatigue**
- `discipline.dawn_inspection_v1` → "Polish kit": **2 Fatigue**

**Story Pack: `morale.json`**
- `morale.campfire_song_v1` → "Lead song": **1 Fatigue**

**Story Pack: `logistics.json`**
- `logistics.thin_wagons_v1` → "Forage proper": **2 Fatigue**

**Story Pack: `medical.json`**
- `medical.aid_tent_shift_v1` → "Help": **2 Fatigue**
- `medical.bandage_drill_v1` → "Take seriously": **1 Fatigue**

**Story Pack: `training.json`**
- `training.lance_drill_night_sparring_v1` → "Lead drill": **2 Fatigue**

**Event Pack: `events_pay_tension.json`**
- `pay_tension_loot_the_dead` → various options: **0-1 Fatigue**
- `pay_tension_confrontation` → various options: **1-2 Fatigue**

#### Story Blocks That Restore Fatigue

**Story Pack: `morale.json`**
- `morale.campfire_song_v1` → "Turn in early": **-1 Fatigue**

**Story Pack: `training.json`**
- `training.lance_drill_night_sparring_v1` → "Turn in": **-1 Fatigue**

#### Recovery

- Natural recovery over time
- Resting activities provide fatigue relief
- Some events explicitly restore fatigue

---

## Story Block Packs

Story packs are collections of thematically related stories stored in `ModuleData/Enlisted/StoryPacks/LanceLife/`.

### Available Story Packs

| Pack File | Category | Story Count | Primary Systems Affected |
|-----------|----------|-------------|-------------------------|
| `corruption.json` | corruption | 1 | Heat |
| `discipline.json` | discipline | 2 | Discipline |
| `morale.json` | morale | 2 | Lance Rep, Fatigue |
| `logistics.json` | logistics | 1 | Fatigue |
| `medical.json` | medical | 2 | Medical Risk, Fatigue |
| `training.json` | training | 1 | Fatigue, Medical Risk |
| `escalation_thresholds.json` | threshold | 16 | All escalation systems |

### Story Pack: corruption.json

**Stories:** 1  
**Systems Affected:** Heat

| Story ID | Triggers | Systems Modified |
|----------|----------|-----------------|
| `corruption.ledger_skim_v1` | entered_town | Heat: -1 to +2 |

**Purpose:** Introduces corruption opportunities and heat tracking through quartermaster interactions.

---

### Story Pack: discipline.json

**Stories:** 2  
**Systems Affected:** Discipline, Fatigue

| Story ID | Triggers | Systems Modified |
|----------|----------|-----------------|
| `discipline.mess_line_fight_v1` | day | Fatigue: 0-1 |
| `discipline.dawn_inspection_v1` | dawn | Fatigue: 0-2 |

**Purpose:** Camp discipline situations testing leadership and compliance.

---

### Story Pack: morale.json

**Stories:** 2  
**Systems Affected:** Lance Reputation, Fatigue, XP

| Story ID | Triggers | Systems Modified |
|----------|----------|-----------------|
| `morale.campfire_song_v1` | night | Fatigue: -1 to +1 |
| `morale.after_battle_words_v1` | leaving_battle, night | XP only |

**Purpose:** Social bonding moments that build or maintain lance cohesion.

---

### Story Pack: logistics.json

**Stories:** 1  
**Systems Affected:** Fatigue, XP, Gold

| Story ID | Triggers | Systems Modified |
|----------|----------|-----------------|
| `logistics.thin_wagons_v1` | Always available | Fatigue: 0-2, Gold: 0-50 |

**Purpose:** Resource scarcity problems requiring player solutions.

---

### Story Pack: medical.json

**Stories:** 2  
**Systems Affected:** Fatigue, XP

| Story ID | Triggers | Systems Modified |
|----------|----------|-----------------|
| `medical.aid_tent_shift_v1` | day | Fatigue: 0-2 |
| `medical.bandage_drill_v1` | dusk | Fatigue: 0-1 |

**Purpose:** Medical training and assistance opportunities, prepares for medical risk system.

---

### Story Pack: training.json

**Stories:** 1  
**Systems Affected:** Fatigue, Medical Risk, XP

| Story ID | Triggers | Systems Modified |
|----------|----------|-----------------|
| `training.lance_drill_night_sparring_v1` | night | Fatigue: 0-2, Injury: 5% chance |

**Purpose:** Training opportunities with risk/reward balance.

---

## Event Categories

Events are different from story packs — they're system-triggered responses to conditions.

### Available Event Packs

Located in `ModuleData/Enlisted/Events/`:

| Event File | Category | Event Count | Trigger Type |
|-----------|----------|-------------|-------------|
| `events_pay_tension.json` | pay | 5 | Pay Tension threshold |
| `events_pay_loyal.json` | pay | Multiple | Pay Tension + loyalty path |
| `events_pay_mutiny.json` | pay | Multiple | Pay Tension + mutiny path |
| `events_escalation_thresholds.json` | threshold | 16 | Escalation thresholds |
| `events_duty_*.json` | duty | Many | Duty assignment |
| `events_onboarding.json` | onboarding | Multiple | Tutorial sequence |
| `events_promotion.json` | promotion | Multiple | Promotion opportunities |
| `events_training.json` | training | Multiple | Formation training |
| `events_general.json` | general | Multiple | Various triggers |

### Event Pack: events_pay_tension.json

**Events:** 5  
**Trigger:** Pay Tension thresholds  
**Systems Affected:** Heat, Discipline, Lance Rep, Pay Tension

| Event ID | Threshold | Delivery | Primary Systems |
|----------|-----------|----------|----------------|
| `pay_tension_grumbling` | Tension ≥ 20 | LeavingBattle | Lance Rep |
| `pay_tension_loot_the_dead` | Tension ≥ 50 | LeavingBattle | Heat, Lance Rep |
| `pay_tension_confrontation` | Tension ≥ 60 | LeavingBattle | Lance Rep, Discipline |
| `pay_tension_mutiny_brewing` | Tension ≥ 85 | LeavingBattle | Lance Rep, Discipline |
| `pay_tension_theft_invitation` | Tension ≥ 45 | LeavingBattle | Heat, Lance Rep |

**Purpose:** Progression of desperation as pay remains unpaid, offering corrupt/loyal/desert paths.

---

### Event Pack: events_escalation_thresholds.json

**Events:** 16  
**Trigger:** Escalation threshold reached  
**Systems Affected:** All escalation systems

**Heat Events:**

| Event ID | Threshold | Effect |
|----------|-----------|--------|
| `heat_warning` | Heat ≥ 3 | Warning from lance leader |
| `heat_shakedown` | Heat ≥ 5 | Kit inspection, discovery risk |
| `heat_audit` | Heat ≥ 7 | Formal audit, explain discrepancies |
| `heat_exposed` | Heat = 10 | Caught, face consequences |

**Discipline Events:**

| Event ID | Threshold | Effect |
|----------|-----------|--------|
| `discipline_extra_duty` | Discipline ≥ 3 | Assigned unpleasant duties |
| `discipline_hearing` | Discipline ≥ 5 | Formal hearing, defend self |
| `discipline_blocked` | Discipline ≥ 7 | Promotion blocked |
| `discipline_discharge` | Discipline = 10 | Facing discharge |

**Lance Reputation Events:**

| Event ID | Threshold | Effect |
|----------|-----------|--------|
| `lance_trusted` | Rep ≥ +20 | Lance mate asks for help |
| `lance_bonded` | Rep ≥ +40 | Lance mates cover for you |
| `lance_isolated` | Rep ≤ -20 | Excluded from social events |
| `lance_sabotage` | Rep ≤ -40 | Active sabotage from lance |

**Medical Events:**

| Event ID | Threshold | Effect |
|----------|-----------|--------|
| `medical_worsening` | Risk ≥ 3 | Condition worsens |
| `medical_complication` | Risk ≥ 4 | New complication added |
| `medical_emergency` | Risk = 5 | Collapse, forced rest |

**Purpose:** Automatic consequences for accumulated choices, creating long-term accountability.

---

## Cross-System Effects Matrix

This matrix shows which story blocks affect multiple systems simultaneously.

### Story Blocks with Multi-System Effects

| Story/Event ID | Heat | Discipline | Pay Tension | Lance Rep | Medical | Fatigue |
|---------------|------|-----------|-------------|-----------|---------|---------|
| **corruption.ledger_skim_v1** |  |  |  |  |  |  |
| → Refuse clean | -1 | — | — | — | — | — |
| → Slip a tip | +2 | — | — | — | — | — |
| **pay_tension_grumbling** |  |  |  |  |  |  |
| → Join grumbling | — | — | — | +5 | — | — |
| → Defend lord | — | — | — | -5 | — | — |
| → Rabble rouse | — | — | — | +10 | — | — |
| **pay_tension_loot_the_dead** |  |  |  |  |  |  |
| → Loot enemy | +2 | — | — | — | — | +1 |
| → Loot everyone | +4 | — | — | — | — | +1 |
| → Take weapon | +1 | — | — | — | — | — |
| → Stop others | — | — | — | -10 | — | +1 |
| **pay_tension_confrontation** |  |  |  |  |  |  |
| → Join delegation | — | — | — | +15 | — | +1 |
| → Lead delegation | — | — | — | +25 | — | +2 |
| → Talk them down | — | — | — | -10 | — | +1 |
| → Warn officers | — | — | — | -30 | — | — |
| **pay_tension_theft_invitation** |  |  |  |  |  |  |
| → Join theft | +3 | — | — | — | — | +1 |
| → Keep watch | +2 | — | — | — | — | +1 |
| → Report them | — | — | — | -25 | — | — |
| **pay_tension_mutiny_brewing** |  |  |  |  |  |  |
| → Join mutiny | — | — | — | +30 | — | — |
| → Stop mutiny | — | — | — | -40 | — | +2 |
| → Stand with officers | — | +variable | — | -50 | +0.2 | +2 |
| **training.lance_drill_night_sparring_v1** |  |  |  |  |  |  |
| → Lead drill | — | — | — | — | 5% injury | +2 |
| **discipline.mess_line_fight_v1** |  |  |  |  |  |  |
| → Step in | — | — | — | — | — | +1 |
| **discipline.dawn_inspection_v1** |  |  |  |  |  |  |
| → Polish kit | — | — | — | — | — | +2 |
| **logistics.thin_wagons_v1** |  |  |  |  |  |  |
| → Forage proper | — | — | — | — | — | +2 |
| **medical.aid_tent_shift_v1** |  |  |  |  |  |  |
| → Help | — | — | — | — | — | +2 |
| **morale.campfire_song_v1** |  |  |  |  |  |  |
| → Lead song | — | — | — | — | — | +1 |
| → Turn in | — | — | — | — | — | -1 |

### Key Insights from Matrix

1. **Pay Tension Events** are the primary drivers of Lance Reputation changes
2. **Heat** is primarily affected by corruption and pay tension (looting/theft) events
3. **Discipline** is mostly affected through threshold events and consequences
4. **Fatigue** is affected by nearly every activity-based story
5. **Medical Risk** is rare but comes from training/combat events
6. **Lance Reputation** has the widest swing range (-50 to +50 in single choices)

---

## Adding New Content

### Checklist for New Story Blocks

When adding new stories, consider:

1. **Which systems does this affect?**
   - [ ] Heat (corruption/illegal activity)
   - [ ] Discipline (rule-breaking)
   - [ ] Pay Tension (triggered by pay issues)
   - [ ] Lance Reputation (social interactions)
   - [ ] Medical Risk (injury/illness)
   - [ ] Fatigue (energy cost)

2. **What category does it belong to?**
   - Corruption, Discipline, Morale, Logistics, Medical, Training
   - Or is it an Event (threshold-triggered)?

3. **What are the trigger conditions?**
   - Time of day? (dawn, day, dusk, night)
   - Player state? (is_enlisted, tier_min/max)
   - System thresholds? (pay_tension_min, heat >= X)
   - Location? (entered_town, leaving_battle)

4. **Does it have multi-system effects?**
   - Corruption often adds Heat
   - Betrayal affects Lance Rep AND may add Discipline
   - Physical activities cost Fatigue AND may cause Medical Risk

5. **What's the cooldown?**
   - Story cooldown_days
   - Category cooldown
   - System threshold event cooldown (usually 7 days)

6. **Does it need a threshold event?**
   - If pushing a system past a threshold
   - Add corresponding threshold event if needed

### File Locations

**Story Packs:** `ModuleData/Enlisted/StoryPacks/LanceLife/{category}.json`

**Event Packs:** `ModuleData/Enlisted/Events/events_{category}.json`

**System Config:** `ModuleData/Enlisted/enlisted_config.json`

### Template for New Story Block

```json
{
  "id": "{category}.{name}_v1",
  "category": "{category}",
  "tags": ["tag1", "tag2"],
  
  "titleId": "ll_story_{category}_{name}_title",
  "bodyId": "ll_story_{category}_{name}_body",
  "title": "Story Title",
  "body": "Story description with {PLACEHOLDERS}.",
  
  "tierMin": 2,
  "tierMax": 6,
  "requireFinalLance": true,
  
  "cooldownDays": 7,
  "maxPerTerm": 0,
  
  "triggers": {
    "all": ["trigger_condition"],
    "any": []
  },
  
  "options": [
    {
      "id": "option_id",
      "textId": "ll_story_{category}_{name}_opt_{id}_text",
      "hintId": "ll_story_{category}_{name}_opt_{id}_hint",
      "text": "Option text",
      "hint": "What this does",
      "risk": "safe|risky|corrupt",
      "costs": {
        "fatigue": 0,
        "gold": 0,
        "heat": 0,
        "discipline": 0
      },
      "rewards": {
        "skillXp": { "SkillName": 20 },
        "gold": 0,
        "fatigueRelief": 0
      },
      "effects": {
        "heat": 0,
        "discipline": 0,
        "lance_reputation": 0,
        "medical_risk": 0
      },
      "resultTextId": "",
      "resultText": ""
    }
  ]
}
```

### System Effect Guidelines

**Heat Effects:**
- Minor corruption: +1
- Moderate corruption: +2
- Major corruption: +3-4
- Cleaning up act: -1 to -2

**Discipline Effects:**
- Minor infraction: +1
- Moderate infraction: +2
- Major infraction: +3
- Redemption: -1 to -2

**Lance Reputation Effects:**
- Small positive act: +1 to +3
- Significant help: +5 to +10
- Major betrayal: -10 to -30
- Extreme actions: -50 to +50

**Medical Risk Effects:**
- Training while injured: +1
- Refusing treatment: +2
- Injury occurrence: Set initial risk
- Treatment: Reset to 0

**Fatigue Effects:**
- Simple activity: 0-1
- Moderate activity: 1-2
- Intense activity: 2-3
- Resting: -1

### Testing New Content

After adding new story blocks:

1. **Verify JSON validity:** Use `tools/events/validate_events.py`
2. **Check system thresholds:** Ensure values don't push systems too fast
3. **Test multi-system effects:** Verify combinations work as intended
4. **Check cooldowns:** Ensure story doesn't spam
5. **Verify trigger conditions:** Test that story appears when expected
6. **Update this document:** Add your story to relevant sections

---

## Quick Reference: Finding Story Blocks by System

### "I need stories that affect Heat"
- `corruption.json` → All stories
- `events_pay_tension.json` → Looting, theft events
- `events_escalation_thresholds.json` → Heat threshold events

### "I need stories that affect Discipline"
- `discipline.json` → All stories
- `events_pay_tension.json` → Some confrontation/mutiny events
- `events_escalation_thresholds.json` → Discipline threshold events

### "I need stories that affect Lance Reputation"
- `morale.json` → Social bonding stories
- `events_pay_tension.json` → All pay tension events (biggest swings)
- `events_escalation_thresholds.json` → Lance reputation threshold events

### "I need stories that affect Medical Risk"
- `medical.json` → Treatment stories
- `training.json` → Injury-causing stories
- `events_escalation_thresholds.json` → Medical threshold events

### "I need stories that cost/restore Fatigue"
- ALL story packs have fatigue costs
- `morale.json` → Has fatigue restoration options
- `training.json` → Has fatigue restoration options

---

## Version History

**v1.0** (December 13, 2025)
- Initial comprehensive reference
- All current story packs documented
- All escalation systems mapped
- Cross-system effects matrix created

---

**Maintained by:** Enlisted Development Team  
**Questions?** See `docs/CONTRIBUTING.md` for how to suggest updates to this document.
