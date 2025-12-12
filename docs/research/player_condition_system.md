# Player Condition System — Injuries, Illness, and Recovery

This document specifies the player condition system for Enlisted. Military service is dangerous — players can get hurt in training, fall sick on campaign, and suffer injuries during duty. This creates meaningful consequences and a medical care loop.

---

## Table of Contents

1. [Design Philosophy](#design-philosophy)
2. [Condition Types](#condition-types)
3. [How Conditions Are Acquired](#how-conditions-are-acquired)
4. [Condition Effects](#condition-effects)
5. [Recovery and Medical Care](#recovery-and-medical-care)
6. [Integration with Events](#integration-with-events)
7. [UI Integration](#ui-integration)
8. [Sample Injury Events](#sample-injury-events)
9. [Balance Guidelines](#balance-guidelines)

---

## Design Philosophy

### Core Principles

1. **Military service is dangerous** — Training accidents happen. Campaigns breed disease. Duty carries risk.

2. **Consequences create stakes** — Getting hurt matters. It affects what you can do and pushes you toward recovery.

3. **Medical care is a loop** — Injuries create a reason to seek the surgeon, which creates its own events and XP opportunities.

4. **Light RP, not simulation** — We're not tracking specific wounds. We're tracking *condition states* that affect gameplay.

5. **Punishing but fair** — Bad luck happens, but players can mitigate risk through choices. Risky options are riskier.

### The Condition Loop

```
Risk Activity → Injury/Illness Check → Condition Applied
                                              ↓
                                      Condition Effects
                                      (reduced capacity)
                                              ↓
                         Seek Medical Care ← or → Tough It Out
                                ↓                      ↓
                         Faster Recovery          Slower Recovery
                         (+Medicine XP)           (Risk of Worsening)
```

---

## Condition Types

### Overview

| Condition Type | Source | Primary Effect | Recovery |
|----------------|--------|----------------|----------|
| **Injury** | Training, duty, combat, accidents | Health damage, activity restrictions | Surgeon, rest |
| **Illness** | Campaign conditions, exposure | Fatigue pool reduction, health drain | Surgeon, rest, time |
| **Exhaustion** | Overwork, fatigue debt | Fatigue pool reduction, performance penalty | Rest only |

### Injury Severity Levels

| Severity | Health Lost | Duration | Activity Impact |
|----------|-------------|----------|-----------------|
| **Minor** | 5–15% | 2–4 days | Warning only |
| **Moderate** | 15–30% | 4–7 days | Training restricted |
| **Severe** | 30–50% | 7–14 days | Most activities restricted |
| **Critical** | 50%+ | 14+ days | Bed rest required, duty suspended |

### Illness Severity Levels

| Severity | Fatigue Pool | Health Drain | Duration |
|----------|--------------|--------------|----------|
| **Mild** | −10% max | None | 2–5 days |
| **Moderate** | −25% max | 1–2% per day | 5–10 days |
| **Severe** | −50% max | 3–5% per day | 10–20 days |
| **Critical** | −75% max | 5–10% per day | Until treated |

### Exhaustion Levels

| Level | Fatigue Pool | Performance | Recovery |
|-------|--------------|-------------|----------|
| **Tired** | −10% max | None | 1 day rest |
| **Worn** | −25% max | −10% XP gain | 2–3 days rest |
| **Depleted** | −50% max | −25% XP gain, injury risk+  | 3–5 days rest |
| **Broken** | −75% max | Cannot train, duty restricted | 5+ days rest |

---

## How Conditions Are Acquired

### Training Injuries

Every training event has an **injury chance** based on intensity:

| Training Intensity | Injury Chance | Typical Severity |
|--------------------|---------------|------------------|
| Light / Easy | 0–2% | Minor |
| Standard | 3–5% | Minor–Moderate |
| Hard / Risky | 10–20% | Minor–Severe |
| Reckless | 25–40% | Moderate–Severe |

**Example:**
```
Shield Wall Drill (Hard intensity)
- 15% chance of injury
- If injured: 70% Minor, 25% Moderate, 5% Severe
- Injury types: Twisted ankle, shield bash to face, spear cut
```

### Duty Injuries

Duty events can cause injuries based on choices:

| Duty | Risk Scenario | Injury Chance |
|------|---------------|---------------|
| Scout | Push into dangerous territory | 15–25% |
| Messenger | Ride hard through rough terrain | 10–20% |
| Armorer | Forge accident | 5–10% |
| Field Medic | Exposure to disease | Illness, not injury |
| Quartermaster | Heavy lifting, cart accident | 5–10% |

### Campaign Illness

Illness chance increases based on campaign conditions:

| Condition | Illness Check | Severity Modifier |
|-----------|---------------|-------------------|
| Days from town > 7 | Daily 2% chance | +1 severity tier |
| Siege ongoing | Daily 3% chance | +1 severity tier |
| Long voyage (naval) | Daily 2% chance | Scurvy risk |
| After battle (wounds) | 5% if wounded | Infection risk |
| Logistics critical | Daily 4% chance | +1 severity tier |
| Morale broken | Daily 2% chance | (Stress illness) |

### Exhaustion Accumulation

Exhaustion builds when fatigue is not managed:

| Trigger | Exhaustion Gain |
|---------|-----------------|
| End day at max fatigue | +1 exhaustion level |
| Work through high fatigue (>80%) | +1 exhaustion level |
| No rest for 3+ days | +1 exhaustion level |
| Recover from severe injury | Start at Worn |

### Combat Injuries (Post-Battle)

After battles where the player participated:

| Battle Outcome | Injury Chance | Severity |
|----------------|---------------|----------|
| Victory, no knockdown | 5% | Minor |
| Victory, knocked down | 30% | Minor–Moderate |
| Defeat, escaped | 40% | Moderate–Severe |
| Defeat, captured then freed | 60% | Moderate–Severe |

---

## Condition Effects

### On Fatigue Pool

| Condition | Fatigue Pool Modifier |
|-----------|----------------------|
| Minor injury | No change |
| Moderate injury | −10% max fatigue |
| Severe injury | −25% max fatigue |
| Critical injury | −50% max fatigue |
| Mild illness | −10% max fatigue |
| Moderate illness | −25% max fatigue |
| Severe/Critical illness | −50/75% max fatigue |

### On Activities

| Condition | Training | Duty | Combat |
|-----------|----------|------|--------|
| Minor injury | Allowed (warning) | Allowed | Allowed |
| Moderate injury | Restricted | Light duty only | Penalized |
| Severe injury | Forbidden | Suspended | Forbidden |
| Critical injury | Forbidden | Suspended | Forbidden |
| Mild illness | Allowed | Allowed | Allowed |
| Moderate illness | Restricted | Light duty only | Penalized |
| Severe+ illness | Forbidden | Suspended | Forbidden |

### On Performance

| Condition | XP Gain | Combat Effectiveness |
|-----------|---------|---------------------|
| Minor injury | Normal | −5% |
| Moderate injury | −10% | −15% |
| Severe injury | N/A | N/A |
| Mild illness | Normal | −5% |
| Moderate illness | −15% | −20% |
| Exhaustion (per level) | −10% per level | −5% per level |

### On Health

| Condition | Health Effect |
|-----------|---------------|
| Injury | Immediate damage (based on severity) |
| Moderate+ illness | Daily health drain until treated |
| Critical illness | Accelerating health drain |
| Untreated infection | Injury worsens over time |

---

## Recovery and Medical Care

### Natural Recovery (No Treatment)

| Condition | Daily Recovery | Risk |
|-----------|----------------|------|
| Minor injury | 10–15% | None |
| Moderate injury | 5–10% | 5% worsen |
| Severe injury | 2–5% | 10% worsen |
| Critical injury | 0–2% | 20% worsen daily |
| Mild illness | 10–15% | 5% worsen |
| Moderate illness | 3–5% | 10% worsen |
| Severe+ illness | 0% | Worsens without treatment |
| Exhaustion | 1 level per full rest day | None |

### Medical Treatment

**Surgeon Visit (Duty Event or Menu Option)**

Creates a medical event where player can seek treatment:

| Treatment | Cost | Effect | Medicine XP (Surgeon) |
|-----------|------|--------|----------------------|
| Basic treatment | Free (enlisted) | +50% recovery rate | — |
| Thorough treatment | Fatigue (+2) | +100% recovery rate, stops worsening | — |
| Herbal remedy | Gold (25–50) | +75% recovery rate, +15% resist worsen | — |
| Surgery (severe) | Gold (50–100), Fatigue (+3) | Required for critical injuries | — |

**Field Medic Duty Bonus**

If player has Field Medic duty:
- Can self-treat minor/moderate conditions
- +25% recovery rate baseline
- Access to "Treat yourself" event

**Rest**

| Rest Type | Recovery Bonus | Requirements |
|-----------|----------------|--------------|
| Light duty | +25% | Duty set to light |
| Camp rest | +50% | No duty, stay in camp |
| Settlement rest | +75% | In town/castle |
| Hospital rest | +100% | In settlement with surgeon |

### Treatment Events

**"Visit the Surgeon" (Menu Option or Duty Event)**

```
## Event: Surgeon's Tent

**Setup**
You report to the surgeon's tent. The physician looks you over, 
noting your {CONDITION}. "I can help, but you'll need to follow 
instructions."

**Options**

| Option | Risk | Cost | Effect |
|--------|------|------|--------|
| Accept treatment | Safe | +2 Fatigue | +100% recovery, stops worsening |
| Ask for medicine to go | Safe | 30 gold | +75% recovery, can continue duty |
| Tough it out | Risky | None | No bonus, 10% worsen chance |
| Request bed rest | Safe | Duty suspended | +150% recovery, lose duty events |
```

---

## Integration with Events

### Injury Chance in Event Options

Every risky option should include injury chance:

```json
{
  "id": "drill_hard",
  "text": "Push yourself to the limit",
  "risk": "risky",
  "costs": {
    "fatigue": 4
  },
  "rewards": {
    "skill_xp": { "Polearm": 45, "Athletics": 30 }
  },
  "injury_chance": 0.15,
  "injury_severity_weights": {
    "minor": 0.70,
    "moderate": 0.25,
    "severe": 0.05
  }
}
```

### Illness Triggers in Events

Some events can directly cause illness:

```json
{
  "id": "medic_plague_ward",
  "text": "Tend the plague cases personally",
  "risk": "risky",
  "costs": {
    "fatigue": 3
  },
  "rewards": {
    "skill_xp": { "Medicine": 60 }
  },
  "illness_chance": 0.20,
  "illness_type": "camp_fever",
  "illness_severity_weights": {
    "mild": 0.50,
    "moderate": 0.40,
    "severe": 0.10
  }
}
```

### Condition-Gated Events

Some events only appear when conditions exist:

```json
{
  "id": "surgeon_checkup",
  "title": "Surgeon's Rounds",
  "triggers": {
    "any": ["has_injury", "has_illness"]
  },
  "setup": "The camp surgeon is making rounds. He notices you favoring your {INJURY_LOCATION}...",
  "options": [...]
}
```

### Condition-Restricted Events

Some events are blocked by conditions:

```json
{
  "id": "drill_shield_wall",
  "requirements": {
    "max_injury_severity": "minor",
    "max_illness_severity": "mild",
    "max_exhaustion": "worn"
  }
}
```

---

## UI Integration

### Status Display Enhancement

Add to `enlisted_status` header:

```
Enlisted under Lord Vlandia
Tier 3 Veteran • Infantry • 45 days served
Current Duty: Scout • Profession: Field Medic

— Your Condition —
Health: 78% [████████░░] 
Condition: Moderate Injury (Twisted knee) — 4 days recovery
Fatigue Pool: 75% of normal

— Camp Status —
...
```

### Condition Indicators

| Condition | Icon | Color |
|-----------|------|-------|
| Healthy | ✓ | Green |
| Minor injury | ⚠ | Yellow |
| Moderate injury | ⚠ | Orange |
| Severe+ injury | ✖ | Red |
| Illness | 🤒 | Yellow–Red |
| Exhaustion | 😫 | Gray–Orange |

### Activity Menu Restrictions

Show why activities are restricted:

```
— TRAINING (Infantry) —
✗ Shield Wall Drill    [Unavailable - Moderate injury]
○ Light Stretching     [+10 Athletics | +1 Fatigue] (safe for injured)
✗ March Conditioning   [Unavailable - Moderate injury]

— MEDICAL —
○ Visit the Surgeon    [Seek treatment for your injury]
○ Rest in Camp         [Skip activities, faster recovery]
```

### Medical Menu Option

Add to main menu when injured/ill:

```
[🏥] Seek Medical Attention  ← Only shows when has condition
```

Or always available:

```
[🏥] Medical / Condition     ← Shows status and treatment options
```

---

## Sample Injury Events

### Training Accident

```
## Event: Training Accident

**Type:** Consequence Event (fires when injury roll succeeds)

**Setup**
The drill goes wrong. Your foot catches on uneven ground and you 
go down hard. Pain shoots through your {INJURY_LOCATION}.

**Severity: Minor**
"A bad bruise, nothing more. You'll be sore for a few days."
→ −10% health, 2–3 day recovery

**Severity: Moderate**  
"The surgeon clicks his tongue. 'Twisted it proper. Stay off it.'"
→ −20% health, 4–6 day recovery, training restricted

**Severity: Severe**
"You hear something pop. The surgeon's face tells you it's bad."
→ −35% health, 8–12 day recovery, duty suspended
```

### Campaign Fever

```
## Event: Camp Fever

**Type:** Illness Event (fires on illness roll during campaign)

**Setup**
You wake shivering despite the heat. Your head pounds and your 
stomach churns. The familiar signs of camp fever.

**Options**

| Option | Risk | Effect |
|--------|------|--------|
| Report to surgeon immediately | Safe | +50% recovery, duty suspended today |
| Push through, hide it | Risky | 30% chance to worsen, keep duty |
| Ask lance mates for help | Safe | They cover for you, +25% recovery |
| Request light duty | Safe | Reduced duty events, +40% recovery |

**Escalation:** Hiding illness repeatedly can lead to severe illness 
or spreading it to lance mates (morale event).
```

### Forge Burn (Armorer Duty)

```
## Event: Hot Iron

**Type:** Duty Injury Event

**Duty Required:** Armorer

**Setup**
The iron slips. Before you can react, searing pain across your 
forearm. You bite back a curse as the skin blisters.

**Severity determined by previous choice in duty event:**
- "Work carefully" → Minor burn (5% health, 2 days)
- "Work quickly" → Moderate burn (15% health, 5 days)
- "Rush the job" → Severe burn (25% health, 8 days, duty restricted)

**Follow-up options:**

| Option | Effect |
|--------|--------|
| Get it treated properly | Normal recovery |
| Wrap it yourself | −1 day recovery, 10% infection chance |
| Ignore it | 25% infection chance, may worsen |
```

### Scout Ambush Injury

```
## Event: Ambush

**Type:** Duty Injury Event

**Duty Required:** Scout

**Setup**
You pushed too deep. Enemy scouts spotted you first. Arrows fly — 
you take one in the {LOCATION} before you can escape.

**Severity:** Moderate–Severe (arrow wound)

**Options:**

| Option | Risk | Effect |
|--------|------|--------|
| Get back to camp for treatment | Safe | Standard recovery, report delivered late |
| Pull it out, keep scouting | Risky | 30% worsen (infection), mission complete |
| Signal for help | Safe | Rescue comes, mission failed |

**Consequence:** Arrow wounds that aren't properly treated have 
high infection chance. Infection upgrades injury by 1 severity level.
```

---

## Illness Types

### Campaign Illnesses

| Illness | Source | Symptoms | Special |
|---------|--------|----------|---------|
| **Camp Fever** | Poor sanitation, siege | Fatigue drain, health drain | Common |
| **Flux** | Bad water, bad food | Severe fatigue drain | Spreads |
| **Wound Rot** | Untreated injury | Injury worsens | Requires surgery |
| **Lung Sickness** | Cold/wet conditions | Fatigue drain, coughing | Long recovery |
| **Scurvy** | Long voyage (naval) | Fatigue drain, weakness | Needs citrus/port |
| **Battle Shock** | Heavy combat | Morale penalty, fatigue | Rest cures |

### Illness Acquisition Table

| Trigger | Illness | Base Chance | Modifiers |
|---------|---------|-------------|-----------|
| Siege > 7 days | Camp Fever | 3%/day | +1% per 3 days |
| Logistics Critical | Flux | 4%/day | — |
| Untreated Moderate+ Injury | Wound Rot | 10%/day | — |
| Campaign in winter | Lung Sickness | 2%/day | +2% if wounded |
| Voyage > 14 days | Scurvy | 5%/day | −3% if supplies good |
| Battle > 20% casualties | Battle Shock | 15% once | +10% if knocked down |

---

## Balance Guidelines

### Injury Frequency Targets

| Activity Level | Injuries per Month |
|----------------|--------------------|
| Safe choices only | 0–1 minor |
| Standard play | 1–2 minor, occasional moderate |
| Risky play | 2–3 minor, 1 moderate, rare severe |
| Reckless play | Frequent moderate, occasional severe |

### Illness Frequency Targets

| Campaign Type | Illness Chance |
|---------------|----------------|
| Normal campaign | 1 mild per 20 days |
| Extended field campaign | 1 mild per 10 days, moderate possible |
| Siege | 1 illness per 7–10 days |
| Long voyage | Scurvy almost certain without port |

### Recovery Time Targets

| Severity | With Treatment | Without |
|----------|----------------|---------|
| Minor | 1–2 days | 2–4 days |
| Moderate | 3–5 days | 5–8 days |
| Severe | 6–10 days | 10–14+ days (risk) |
| Critical | 10–14 days | Worsens |

### Lethality

**Injuries should not directly kill the player** except in extreme circumstances (critical injury + no treatment + bad luck).

**Illness can be lethal** if completely ignored at severe+ levels.

**Health floor:** Player should rarely drop below 20% from non-combat conditions. Below that, force medical events.

---

## Implementation Data Structures

### Player Condition State

```csharp
public class PlayerConditionState
{
    // Injury
    public InjurySeverity? CurrentInjury { get; set; }
    public string InjuryType { get; set; }  // "twisted_knee", "arrow_wound", etc.
    public string InjuryLocation { get; set; }  // "leg", "arm", "torso"
    public float InjuryRecoveryProgress { get; set; }  // 0.0 to 1.0
    public int InjuryDaysRemaining { get; set; }
    public bool InjuryInfected { get; set; }
    
    // Illness
    public IllnessSeverity? CurrentIllness { get; set; }
    public string IllnessType { get; set; }  // "camp_fever", "flux", etc.
    public float IllnessRecoveryProgress { get; set; }
    public int IllnessDaysRemaining { get; set; }
    
    // Exhaustion
    public ExhaustionLevel Exhaustion { get; set; }
    
    // Treatment
    public bool UnderMedicalCare { get; set; }
    public float RecoveryRateModifier { get; set; }  // From treatment
    
    // Computed
    public float FatiguePoolModifier => CalculateFatiguePoolModifier();
    public float XPGainModifier => CalculateXPModifier();
    public bool CanTrain => CanPerformActivity(ActivityType.Training);
    public bool CanPerformDuty => CanPerformActivity(ActivityType.Duty);
}

public enum InjurySeverity { Minor, Moderate, Severe, Critical }
public enum IllnessSeverity { Mild, Moderate, Severe, Critical }
public enum ExhaustionLevel { None, Tired, Worn, Depleted, Broken }
```

### Injury Definition

```json
{
  "injuries": {
    "twisted_knee": {
      "display_name": "Twisted Knee",
      "locations": ["leg"],
      "base_recovery_days": { "minor": 3, "moderate": 6, "severe": 12 },
      "health_damage": { "minor": 0.10, "moderate": 0.20, "severe": 0.35 },
      "infection_risk": 0.0,
      "activity_restrictions": {
        "minor": [],
        "moderate": ["training_hard", "running"],
        "severe": ["training", "duty_active", "combat"]
      }
    },
    "arrow_wound": {
      "display_name": "Arrow Wound",
      "locations": ["arm", "leg", "torso"],
      "base_recovery_days": { "minor": 4, "moderate": 8, "severe": 14 },
      "health_damage": { "minor": 0.15, "moderate": 0.25, "severe": 0.40 },
      "infection_risk": 0.25,
      "requires_surgery_at": "severe",
      "activity_restrictions": {
        "minor": [],
        "moderate": ["training", "duty_physical"],
        "severe": ["training", "duty", "combat"]
      }
    }
  }
}
```

---

## Event JSON with Injury

### Standard Format

```json
{
  "id": "scout_deep_recon",
  "options": [
    {
      "id": "push_deep",
      "text": "Push into dangerous territory",
      "risk": "risky",
      "costs": { "fatigue": 2 },
      "rewards": { "skill_xp": { "Scouting": 50 } },
      "injury": {
        "chance": 0.20,
        "types": ["arrow_wound", "fall_injury", "blade_cut"],
        "severity_weights": { "minor": 0.40, "moderate": 0.45, "severe": 0.15 },
        "location_weights": { "arm": 0.3, "leg": 0.4, "torso": 0.3 }
      }
    }
  ]
}
```

### Training Event with Scaling Risk

```json
{
  "id": "sparring_circle",
  "options": [
    {
      "id": "spar_light",
      "text": "Spar carefully",
      "costs": { "fatigue": 1 },
      "rewards": { "skill_xp": { "OneHanded": 15 } },
      "injury": null
    },
    {
      "id": "spar_standard",
      "text": "Fight normally",
      "costs": { "fatigue": 2 },
      "rewards": { "skill_xp": { "OneHanded": 25 } },
      "injury": {
        "chance": 0.05,
        "types": ["bruise", "cut"],
        "severity_weights": { "minor": 0.90, "moderate": 0.10 }
      }
    },
    {
      "id": "spar_hard",
      "text": "Go all out",
      "costs": { "fatigue": 3 },
      "rewards": { "skill_xp": { "OneHanded": 40 } },
      "injury": {
        "chance": 0.15,
        "types": ["bruise", "cut", "sprain"],
        "severity_weights": { "minor": 0.60, "moderate": 0.35, "severe": 0.05 }
      }
    }
  ]
}
```

---

*Document version: 1.0*
*Companion to: Lance Life Events Master Documentation v2*
*Companion to: Menu Enhancements Spec*
