# Medical Progression System

**Summary:** CK3-style probabilistic medical progression where conditions can improve or worsen over time based on daily rolls, modified by skills, rest, and treatment. Replaces deterministic "choice = outcome" with organic uncertainty.

**Status:** 📋 Specification  
**Last Updated:** 2025-12-23  
**Related Docs:** [Event System Schemas](event-system-schemas.md), [Event Catalog](../../Content/event-catalog-by-system.md)

---

## Index

1. [Overview](#overview)
2. [Purpose](#purpose)
3. [Inputs](#inputs)
4. [Outputs](#outputs)
5. [Behavior](#behavior)
6. [Probability Tables](#probability-tables)
7. [Modifiers](#modifiers)
8. [Event Integration](#event-integration)
9. [Edge Cases](#edge-cases)
10. [Future Expansion](#future-expansion)
11. [Acceptance Criteria](#acceptance-criteria)

---

## Overview

The Medical Progression System adds daily probabilistic checks to the Medical Risk escalation track. Instead of player choices directly setting Medical Risk values, the system simulates organic disease/injury progression where conditions can:

- **Recover naturally** (body fights it off)
- **Remain stable** (no change)
- **Worsen** (complications develop)

This creates tension and unpredictability similar to Crusader Kings 3's health system, where even "safe" choices don't guarantee safety and high Medicine skill genuinely matters.

---

## Purpose

### Problems with Current System

1. **Deterministic** - "See surgeon" always equals -2 Medical Risk. No uncertainty.
2. **Puzzle-like** - Players learn the "right" answer and always pick it.
3. **No skill incentive** - Medicine skill doesn't affect recovery.
4. **Unrealistic** - Real illness doesn't respond to single choices.
5. **Low stakes** - Once you pick "surgeon," you're guaranteed to improve.

### Goals

1. **Organic uncertainty** - Conditions can worsen overnight even if resting
2. **Skill matters** - High Medicine genuinely protects you
3. **Multiple treatments** - May need to see surgeon repeatedly
4. **Narrative richness** - "It got worse" or "Lucky recovery" feel realistic
5. **Tension** - Even careful players face risk

---

## Inputs

### State Tracked

| Input | Source | Purpose |
|-------|--------|---------|
| `MedicalRisk` | EscalationState | Current severity (0-5) |
| `Medicine` skill | Hero.MainHero | Recovery/worsen modifier |
| `IsResting` | Context check | Resting in camp vs marching |
| `RecentBattle` | News tracking | Battle in last 24 hours |
| `TreatedToday` | Flag | Saw surgeon this day |
| `DaysSinceOnset` | Counter | How long condition has persisted |

### Configuration (enlisted_config.json)

```json
{
  "medical_progression": {
    "enabled": true,
    "check_hour": 6,
    "base_recovery_chance": 25,
    "base_stable_chance": 55,
    "base_worsen_chance": 20,
    "medicine_skill_divisor": 3,
    "resting_bonus": 10,
    "march_penalty": 10,
    "battle_penalty": 15,
    "treatment_bonus": 25,
    "critical_death_enabled": false,
    "critical_death_chance": 5
  }
}
```

---

## Outputs

### Daily Roll Results

| Result | Effect | News Message |
|--------|--------|--------------|
| **Recovery** | -1 Medical Risk | "Feeling stronger today." |
| **Stable** | No change | (no message) |
| **Worsen** | +1 Medical Risk | "Condition worsened overnight." |
| **Lucky Recovery** | -2 Medical Risk | "Fever broke. Nearly back to normal." |
| **Complication** | +2 Medical Risk | "Took a bad turn in the night." |

### Threshold Events

When Medical Risk crosses thresholds (2, 3, 4, 5), the existing threshold events fire:

- `medical_onset` (Med Risk 2)
- `medical_worsening` (Med Risk 3)
- `medical_complication` (Med Risk 4)
- `medical_emergency` (Med Risk 5)

---

## Behavior

### Daily Tick (Hour 6)

```
IF MedicalRisk > 0:
    1. Calculate modifiers (skill, rest, treatment, etc.)
    2. Adjust base probabilities
    3. Roll d100
    4. Apply result (+1, -1, or 0 to MedicalRisk)
    5. Check for threshold crossings → queue events
    6. Clear daily flags (TreatedToday)
    7. Post news message if changed
```

### Probability Calculation

```
RecoveryChance = BaseRecovery + SkillBonus + RestBonus + TreatmentBonus - BattlePenalty
WorsenChance = BaseWorsen - SkillReduction - RestReduction - TreatmentReduction + BattlePenalty
StableChance = 100 - RecoveryChance - WorsenChance
```

### Critical Rolls (Optional)

- Roll 01-05: **Lucky Recovery** (-2 instead of -1)
- Roll 96-100: **Complication** (+2 instead of +1)

These represent unusually good or bad luck that can dramatically shift the situation.

---

## Probability Tables

### Base Probabilities by Severity

| Medical Risk | Recovery | Stable | Worsen | Notes |
|--------------|----------|--------|--------|-------|
| 1 (minor) | 40% | 50% | 10% | Usually resolves on its own |
| 2 (unwell) | 25% | 55% | 20% | Could go either way |
| 3 (worsening) | 15% | 50% | 35% | Trending bad without treatment |
| 4 (serious) | 10% | 45% | 45% | Dangerous, needs intervention |
| 5 (critical) | 5% | 40% | 55% | Life-threatening |

### Why Severity Affects Odds

Higher Medical Risk = harder to recover naturally. This creates urgency at higher levels while allowing minor issues to often resolve themselves.

---

## Modifiers

### Skill Modifiers

| Medicine Skill | Recovery Bonus | Worsen Reduction |
|----------------|----------------|------------------|
| 0-29 | +0% | -0% |
| 30-59 | +5% | -5% |
| 60-89 | +10% | -10% |
| 90-119 | +15% | -12% |
| 120+ | +20% | -15% |

Formula: `SkillBonus = Medicine / 3` (capped at +20%)

### Context Modifiers

| Condition | Recovery | Worsen | Notes |
|-----------|----------|--------|-------|
| Resting in camp | +10% | -10% | Not on march, in settlement |
| On the march | -10% | +10% | Party is moving |
| After battle (24h) | -15% | +15% | Body stressed from combat |
| Saw surgeon today | +25% | -25% | TreatedToday flag |
| Starving | -10% | +15% | Party food < 0 |
| Well-fed | +5% | -5% | Party food > 10 days |

### Stacking

All modifiers stack additively. Final probabilities are clamped:
- Recovery: 5% minimum, 80% maximum
- Worsen: 5% minimum, 70% maximum
- Stable: whatever remains

---

## Event Integration

### Threshold Events (Existing)

The existing medical threshold events continue to fire when thresholds are crossed. The difference is that thresholds can now be crossed by **daily progression**, not just player event choices.

| Event | Trigger | Notes |
|-------|---------|-------|
| `medical_onset` | MedRisk crosses to 2 | First warning |
| `medical_worsening` | MedRisk crosses to 3 | Getting serious |
| `medical_complication` | MedRisk crosses to 4 | Complications |
| `medical_emergency` | MedRisk crosses to 5 | Life-threatening |

### New Decision: Seek Treatment

A Camp Hub decision to visit the surgeon and set the `TreatedToday` flag:

```json
{
  "id": "dec_seek_treatment",
  "category": "decision",
  "title": "Seek Treatment",
  "setup": "The surgeon's tent. Herbs, poultices, and the smell of blood.",
  "requirements": {
    "min_escalation": { "medical_risk": 1 }
  },
  "timing": {
    "cooldown_days": 1
  },
  "options": [
    {
      "id": "full_treatment",
      "text": "Submit to the surgeon's care.",
      "tooltip": "+25% recovery chance today. Costs time and dignity.",
      "costs": { "gold": 20, "time_hours": 4 },
      "effects": { "medical_risk": -1 },
      "set_flags": ["treated_today"]
    },
    {
      "id": "quick_treatment",
      "text": "Get herbs and go.",
      "tooltip": "+10% recovery chance. Quick but less effective.",
      "costs": { "gold": 5, "time_hours": 1 },
      "set_flags": ["treated_today_minor"]
    },
    {
      "id": "leave",
      "text": "Changed my mind.",
      "tooltip": "Leave without treatment."
    }
  ]
}
```

### New Events: Progression Notifications

Optional lightweight events to notify player of daily changes:

**Lucky Recovery (MedRisk drops by 2)**
```json
{
  "id": "evt_medical_lucky_recovery",
  "title": "Fever Broke",
  "setup": "You wake feeling... better. Much better. The fever broke in the night. {COMRADE_NAME} looks relieved.",
  "options": [
    { "id": "relief", "text": "Thank the gods.", "effects": {} }
  ]
}
```

**Complication (MedRisk rises by 2)**
```json
{
  "id": "evt_medical_complication_daily",
  "title": "Bad Turn",
  "setup": "The night was hard. Sweating, shaking. Whatever this is, it's getting worse. Fast.",
  "options": [
    { "id": "worry", "text": "This isn't good.", "effects": {} }
  ]
}
```

---

## Edge Cases

### Medical Risk at 0

No daily roll. System only activates when MedicalRisk > 0.

### Medical Risk at 5 (Maximum)

Worsen rolls have no effect (already at max). Optional: enable death risk at this level.

### Death (Optional)

If `critical_death_enabled: true` and MedicalRisk = 5:
- 5% chance per day of death event
- Death event offers last-ditch options (miracle, sacrifice, etc.)
- Not recommended for most playthroughs

### Rapid Onset

If player gains +3 Medical Risk in one event (serious injury), they skip intermediate thresholds. The daily system doesn't retroactively fire skipped events.

### Multiple Treatments

Player can "Seek Treatment" every day. Each treatment:
- Gives -1 Medical Risk immediately
- Sets +25% recovery bonus for that day's roll
- Costs gold/time

Aggressive treatment (daily surgeon visits) can stabilize even critical conditions.

### Save/Load

All state is persisted in EscalationState:
- `MedicalRisk` (existing)
- `TreatedToday` flag (clear on day change)
- `DaysSinceOnset` counter (optional, for narrative)

---

## Future Expansion

### Specific Conditions

Instead of generic "Medical Risk," track specific conditions:

| Condition | Base Worsen | Base Recovery | Notes |
|-----------|-------------|---------------|-------|
| Fever | 25% | 20% | Common, moderate danger |
| Infection | 35% | 10% | Serious, needs treatment |
| Plague | 50% | 5% | Deadly, rare |
| Battle Wound | 20% | 15% | Heals slowly but steadily |
| Exhaustion | 15% | 30% | Recovers with rest |

### Contagion

- Party members can spread illness
- Quarantine decision to reduce spread
- Affects Company Needs (Morale, Readiness)

### Permanent Consequences

- Critical conditions that survive may leave scars
- -5 to max HP, or skill penalties
- Creates long-term stakes

### Surgeon Quality

- Different lords have different surgeon quality
- Affects treatment bonuses
- Reason to care about which lord you serve

### Herbal Remedies

- Player can gather herbs (Scouting skill)
- Self-treatment option that doesn't require gold
- Medicine skill affects effectiveness

---

## Acceptance Criteria

### Core System

- [ ] Daily tick at hour 6 checks MedicalRisk > 0
- [ ] Roll determines recovery/stable/worsen outcome
- [ ] Medicine skill modifies probabilities
- [ ] Rest/march context modifies probabilities
- [ ] Treatment flag modifies probabilities
- [ ] MedicalRisk changes trigger threshold events
- [ ] News system reports significant changes

### Configuration

- [ ] All probabilities configurable in enlisted_config.json
- [ ] System can be disabled via config flag
- [ ] Skill divisor configurable
- [ ] All modifier values configurable

### Integration

- [ ] Existing medical threshold events still fire
- [ ] Player status shows current Medical Risk
- [ ] Seek Treatment decision available in Camp Hub
- [ ] Daily progression logged for debugging

### Edge Cases

- [ ] No roll when MedicalRisk = 0
- [ ] No worsen effect when MedicalRisk = 5
- [ ] TreatedToday flag clears at day change
- [ ] State persists through save/load

---

**End of Document**

