# Escalation System

This document specifies how repeated player choices accumulate into larger consequences. Escalations create a sense of cause-and-effect without instant punishment.

---

## Table of Contents

1. [Overview](#overview)
2. [Design Principles](#design-principles)
3. [Escalation Tracks](#escalation-tracks)
4. [Track Definitions](#track-definitions)
   - [Corruption / Heat](#corruption--heat)
   - [Discipline](#discipline)
   - [Lance Reputation](#lance-reputation)
   - [Medical](#medical)
5. [Threshold Events](#threshold-events)
6. [Decay and Recovery](#decay-and-recovery)
7. [Event Integration](#event-integration)
8. [UI Visibility](#ui-visibility)
9. [Configuration](#configuration)

---

## Overview

Escalations connect individual event choices to longer-term consequences. When a player repeatedly makes corrupt choices, takes risks, or neglects duties, the system tracks this and eventually triggers consequence events.

**Core Loop:**
```
Player makes choice → Track increments → Threshold reached → Consequence event fires
                                              ↓
                               Track decrements or resets based on outcome
```

**Example:**
```
Player hides supply shortage (QM event)     → Heat +2
Player adjusts ledger again (QM event)      → Heat +2
Player takes bribe from merchant            → Heat +3
                                              ───────
                                              Heat = 7 (threshold: 5)
                                              ↓
                            "Shakedown" event fires — someone noticed
```

---

## Design Principles

### 1. No Instant Hard Fails

Escalations create tension and consequences, never instant game-overs. Even at maximum heat, the consequence is a difficult event — not discharge or death.

### 2. Readable and Predictable

Players should understand why a consequence happened. Events that add to tracks should hint at risk:
- "But ledgers get audited eventually."
- "Someone might have seen that."
- "This won't go unnoticed forever."

### 3. Recovery Paths Exist

Every track has ways to decrease:
- Time decay (slow)
- Clean choices (moderate)
- Specific redemption events (fast)

### 4. Consequences Match Scale

Camp-scale choices → camp-scale consequences. Stealing from the quartermaster doesn't alert the king. It alerts the sergeant.

### 5. Tracks Are Internal

Escalation tracks are Enlisted-internal. They don't modify vanilla reputation, crime, or lord relations.

---

## Escalation Tracks

| Track | Range | What It Measures | Consequence Theme |
|-------|-------|------------------|-------------------|
| **Heat** | 0–10 | Corruption attention | Shakedowns, audits, confiscation |
| **Discipline** | 0–10 | Rule-breaking accumulation | Extra duty, formal hearing, blocked promotion |
| **Lance Reputation** | -50 to +50 | Standing with your unit | Trust, support, isolation |
| **Medical Risk** | 0–5 | Untreated condition danger | Worsening, complications |

---

## Track Definitions

### Corruption / Heat

**What it tracks:** Attention from corrupt activities — skimming supplies, taking bribes, contraband, falsifying records.

**How it increases:**

| Source | Heat Gained | Example |
|--------|-------------|---------|
| Minor corruption | +1 | Looking the other way |
| Moderate corruption | +2 | Adjusting ledger numbers |
| Major corruption | +3 | Taking a bribe, stealing equipment |
| Witnessed corruption | +2 (bonus) | Someone saw you |

**Thresholds:**

| Heat Level | Range | State | Effect |
|------------|-------|-------|--------|
| Clean | 0 | No attention | — |
| Watched | 1–2 | Mild suspicion | Warning text in events |
| Noticed | 3–4 | Active suspicion | "Watched closely" event possible |
| Hot | 5–6 | Investigation likely | Shakedown events trigger |
| Burning | 7–9 | Active investigation | Audit events trigger |
| Exposed | 10 | Caught | Confiscation + discipline event |

**Threshold Events:**

| Threshold | Event | Description |
|-----------|-------|-------------|
| Heat ≥ 3 | `heat_warning` | {LANCE_LEADER_SHORT} gives you a look. "Keep your nose clean." |
| Heat ≥ 5 | `heat_shakedown` | Surprise inspection of your kit. Roll to avoid discovery. |
| Heat ≥ 7 | `heat_audit` | Formal audit of supplies you handled. Explain discrepancies. |
| Heat = 10 | `heat_exposed` | Caught. Face consequences: confiscation, discipline, demotion. |

**Decay:**
- −1 Heat per 7 days with no corrupt choices
- −2 Heat for choosing "report corruption" options
- Reset to 0 if `heat_exposed` event passed (paid the price)

---

### Discipline

**What it tracks:** Accumulated rule-breaking, insubordination, and duty failures.

**How it increases:**

| Source | Discipline Gained | Example |
|--------|-------------------|---------|
| Minor infraction | +1 | Late to muster, sloppy work |
| Moderate infraction | +2 | Sleeping on watch, disobeying order |
| Major infraction | +3 | Fighting, desertion attempt, theft |
| Caught by officer | +1 (bonus) | Witnessed by someone with authority |

**Thresholds:**

| Discipline Level | Range | State | Effect |
|------------------|-------|-------|--------|
| Clean | 0 | Good standing | — |
| Minor marks | 1–2 | On notice | Warning text |
| Troubled | 3–4 | Extra duty likely | Extra duty events trigger |
| Serious | 5–6 | Hearing possible | Formal discipline events |
| Critical | 7–9 | Promotion blocked | Cannot promote until reduced |
| Breaking | 10 | Facing discharge | Discharge hearing event |

**Threshold Events:**

| Threshold | Event | Description |
|-----------|-------|-------------|
| Discipline ≥ 3 | `discipline_extra_duty` | Assigned unpleasant extra duties as punishment. |
| Discipline ≥ 5 | `discipline_hearing` | Formal hearing before {LANCE_LEADER_RANK}. Defend yourself. |
| Discipline ≥ 7 | `discipline_blocked` | Promotion unavailable until discipline improves. |
| Discipline = 10 | `discipline_discharge` | Facing discharge. One chance to argue your case. |

**Decay:**
- −1 Discipline per 14 days with no infractions
- −1 Discipline for completing extra duty without complaint
- −2 Discipline for "cover for lance mate" choices (you take the heat)

---

### Lance Reputation

**What it tracks:** How your lance mates view you — trust, respect, belonging.

**How it changes:**

| Source | Reputation Change | Example |
|--------|-------------------|---------|
| Help a lance mate | +2 to +5 | Cover for them, share supplies |
| Reliable duty performance | +1 | Consistent good work |
| Social bonding | +1 to +3 | Fire circle, drinking, stories |
| Selfish choice | −2 to −5 | Take best gear, blame others |
| Betray lance mate | −5 to −10 | Snitch, let them take fall |
| Cowardice witnessed | −3 to −5 | Abandon them in danger |

**Thresholds:**

| Reputation | Range | State | Effect |
|------------|-------|-------|--------|
| Bonded | +40 to +50 | Family | Lance mates cover for you, warn you, follow your lead |
| Trusted | +20 to +39 | Solid | Good options in events, help available |
| Accepted | +5 to +19 | Normal | Standard treatment |
| Neutral | −4 to +4 | New/Unknown | No special treatment |
| Disliked | −19 to −5 | Cold | Worse options, no help offered |
| Outcast | −39 to −20 | Isolated | Excluded from social events, blamed first |
| Hated | −50 to −40 | Enemy | Active sabotage, set up to fail |

**Threshold Events:**

| Threshold | Event | Description |
|-----------|-------|-------------|
| Rep ≥ +20 | `lance_trusted` | A lance mate shares a secret, asks for help. |
| Rep ≥ +40 | `lance_bonded` | Lance mates warn you about shakedown, cover for mistake. |
| Rep ≤ −20 | `lance_isolated` | Excluded from fire circle. Eat alone. |
| Rep ≤ −40 | `lance_sabotage` | Someone "lost" your equipment. Or set you up. |

**Decay:**
- Slowly trends toward 0 over time (±1 per 14 days)
- Major events can shift dramatically

---

### Medical Risk

**What it tracks:** Danger from untreated or ignored medical conditions.

**How it increases:**

| Source | Risk Gained | Example |
|--------|-------------|---------|
| Untreated injury (per day) | +1 | Ignored wound |
| Untreated illness (per day) | +1 | Ignored sickness |
| Training while injured | +1 | Pushed too hard |
| Refused treatment | +2 | "I'm fine" when not fine |

**Thresholds:**

| Risk Level | Range | State | Effect |
|------------|-------|-------|--------|
| None | 0 | Healthy or treated | — |
| Mild | 1–2 | Ignorable | Warning text |
| Concerning | 3 | Needs attention | Condition may worsen |
| Serious | 4 | Worsening likely | Condition severity increases |
| Critical | 5 | Emergency | Severe consequence event |

**Threshold Events:**

| Threshold | Event | Description |
|-----------|-------|-------------|
| Risk ≥ 3 | `medical_worsening` | Condition gets worse. Minor → Moderate. |
| Risk ≥ 4 | `medical_complication` | New complication. Infection, fever, etc. |
| Risk = 5 | `medical_emergency` | Collapse. Forced bed rest, possible lasting effect. |

**Decay:**
- Resets to 0 when condition is treated
- −1 per day of rest
- Does not decay while condition persists untreated

---

## Threshold Events

Threshold events are special events that fire when a track reaches a certain level. They are:

1. **Mandatory** — Cannot be avoided once triggered
2. **Consequential** — Have real effects (loss, restriction, opportunity)
3. **Resolvable** — Provide paths to reduce the track

### Event Structure

```json
{
  "id": "heat_shakedown",
  "category": "escalation",
  "track": "heat",
  "threshold": 5,
  "cooldown_days": 7,
  "priority": "high",
  
  "triggers": {
    "all": ["is_enlisted", "heat >= 5", "ai_safe"],
    "time_of_day": ["morning", "afternoon"]
  },
  
  "setup": "{SECOND_SHORT} corners you behind the supply wagons. \"Kit inspection. Now. Empty your pack.\"\n\nThis isn't random. Someone talked.",
  
  "options": [
    {
      "id": "comply_clean",
      "text": "Comply — you've got nothing to hide",
      "condition": "contraband_count == 0",
      "risk": "safe",
      "outcome": "Your kit is clean. {SECOND_SHORT} grunts, disappointed. \"Keep it that way.\"",
      "effect": { "heat": -2 }
    },
    {
      "id": "comply_caught",
      "text": "Comply — hope they miss it",
      "condition": "contraband_count > 0",
      "risk": "risky",
      "success_chance": 0.3,
      "outcome_success": "They search quickly, miss the hidden pocket. Lucky.",
      "outcome_failure": "They find it. All of it. {SECOND_SHORT}'s face hardens.",
      "effect_success": { "heat": -1 },
      "effect_failure": { "heat": +2, "discipline": +2, "contraband": "confiscated" }
    },
    {
      "id": "bribe",
      "text": "\"Maybe we can work something out...\"",
      "risk": "corrupt",
      "cost": { "gold": 50 },
      "success_chance": 0.5,
      "outcome_success": "{SECOND_SHORT} pockets the coin. \"Inspection complete. Nothing found.\"",
      "outcome_failure": "\"You trying to bribe me?\" This just got worse.",
      "effect_success": { "heat": -1 },
      "effect_failure": { "heat": +3, "discipline": +3 }
    },
    {
      "id": "run",
      "text": "Bolt — deal with this later",
      "risk": "risky",
      "outcome": "You're gone before they can grab you. But now they know you're guilty.",
      "effect": { "heat": +2, "discipline": +2, "flag": "fugitive_from_inspection" }
    }
  ]
}
```

---

## Decay and Recovery

### Passive Decay

All tracks slowly decay toward neutral over time:

| Track | Decay Rate | Condition |
|-------|------------|-----------|
| Heat | −1 per 7 days | No corrupt choices |
| Discipline | −1 per 14 days | No infractions |
| Lance Rep | ±1 per 14 days | Trends toward 0 |
| Medical Risk | −1 per day | Resting |

### Active Recovery

Players can take actions to reduce tracks faster:

**Heat:**
- Report corruption you witness → −2 Heat
- Refuse bribe publicly → −1 Heat
- Pass audit cleanly → −3 Heat

**Discipline:**
- Complete extra duty → −1 Discipline
- Take blame for lance mate → −2 Discipline (but +Lance Rep)
- Volunteer for dangerous duty → −2 Discipline

**Lance Rep:**
- Help wounded lance mate → +3 Rep
- Share supplies when short → +2 Rep
- Stand up for lance mate → +3 Rep

**Medical:**
- Seek treatment → Reset to 0
- Rest (skip activities) → −1 per day

---

## Event Integration

### Adding Escalation to Events

Events can contribute to escalation tracks via the `effect` field:

```json
{
  "id": "corrupt_option",
  "text": "Adjust the numbers to hide the shortage",
  "risk": "corrupt",
  "effect": {
    "heat": 2,
    "xp": { "roguery": 15, "steward": 20 }
  },
  "flags": ["corruption"],
  "outcome": "A few scratched numbers, a creative total. Nobody's the wiser — for now."
}
```

### Triggering Threshold Events

The escalation manager checks tracks after each event resolves:

```
Event completes
    ↓
Apply effects (including track changes)
    ↓
Check each track against thresholds
    ↓
If threshold reached AND cooldown clear:
    Queue threshold event (fires next safe moment)
```

### Preventing Spam

- Each threshold event has its own cooldown (typically 7 days)
- Only one threshold event can fire per day
- Higher thresholds override lower ones (if Heat = 7, fire `heat_audit`, not `heat_shakedown`)

---

## UI Visibility

### Enlisted Status Header

Show track status when relevant:

```
— YOUR STANDING —
Heat: ░░░░░░░░░░ Clean
Discipline: ██░░░░░░░░ Minor marks (2)
Lance: ████████░░ Trusted (+32)
```

Or simpler text version:

```
— YOUR STANDING —
Discipline: On notice (extra duty possible)
Lance: Trusted — your lance mates have your back
```

### Event Warnings

When an option would increase a track significantly, hint at it:

```
[Adjust the numbers]
Risk: This won't go unnoticed forever. (Heat +2)
```

### Threshold Warnings

Before threshold events fire, show warning in status:

```
[!] You're being watched — keep your head down
```

---

## Configuration

### Feature Flags

```json
{
  "escalation": {
    "enabled": true,
    "tracks": {
      "heat": { "enabled": true, "decay_days": 7 },
      "discipline": { "enabled": true, "decay_days": 14 },
      "lance_reputation": { "enabled": true, "decay_days": 14 },
      "medical_risk": { "enabled": true, "decay_days": 1 }
    },
    "threshold_events": {
      "enabled": true,
      "cooldown_days": 7,
      "max_per_day": 1
    }
  }
}
```

### Tuning Values

```json
{
  "escalation_tuning": {
    "heat": {
      "thresholds": {
        "watched": 3,
        "shakedown": 5,
        "audit": 7,
        "exposed": 10
      },
      "max": 10,
      "decay_rate": 1,
      "decay_interval_days": 7
    },
    "discipline": {
      "thresholds": {
        "extra_duty": 3,
        "hearing": 5,
        "blocked": 7,
        "discharge": 10
      },
      "max": 10,
      "decay_rate": 1,
      "decay_interval_days": 14
    }
  }
}
```

---

## Implementation Checklist

### Phase 4 Requirements (from Master Plan)

- [ ] Heat track with thresholds and events
- [ ] Discipline track with thresholds and events
- [ ] Lance reputation track (simplified)
- [ ] Medical risk track (connects to condition system)
- [ ] Decay system (passive + active)
- [ ] Threshold event triggering
- [ ] UI visibility (status display)
- [ ] Configuration/feature flags

### Threshold Events to Write

**Heat Events:**
- [ ] `heat_warning` (threshold 3)
- [ ] `heat_shakedown` (threshold 5)
- [ ] `heat_audit` (threshold 7)
- [ ] `heat_exposed` (threshold 10)

**Discipline Events:**
- [ ] `discipline_warning` (threshold 2)
- [ ] `discipline_extra_duty` (threshold 3)
- [ ] `discipline_hearing` (threshold 5)
- [ ] `discipline_blocked` (threshold 7)
- [ ] `discipline_discharge` (threshold 10)

**Lance Events:**
- [ ] `lance_trusted` (threshold +20)
- [ ] `lance_bonded` (threshold +40)
- [ ] `lance_isolated` (threshold −20)
- [ ] `lance_sabotage` (threshold −40)

**Medical Events:**
- [ ] `medical_worsening` (threshold 3)
- [ ] `medical_complication` (threshold 4)
- [ ] `medical_emergency` (threshold 5)

---

*Document version: 1.0*
*Part of: Lance Life System*
*Phase: 4 (Escalation tracks + consequences)*
