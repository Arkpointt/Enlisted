# Medical Progression System

**Summary:** CK3-style probabilistic medical progression where conditions can improve or worsen over time based on daily rolls, modified by skills, rest, and treatment. First implementation of the generic Progression System pattern.

**Status:** 📋 Specification (Uses Generic Progression Schema)  
**Last Updated:** 2025-12-29  
**Schema:** See [Progression System Schema](event-system-schemas.md#progression-system-schema-future-foundation)  
**Related Docs:** [Event System Schemas](event-system-schemas.md), [Content System Architecture](content-system-architecture.md)

---

## Overview

The Medical Progression System is the **first implementation** of the generic Progression System pattern. It applies probabilistic daily checks to the Medical Risk escalation track.

**This document focuses on medical-specific details.** For the generic schema (reusable for Discipline, Pay Tension, etc.), see [Progression System Schema](event-system-schemas.md#progression-system-schema-future-foundation).

---

## Medical-Specific Configuration

**File:** `ModuleData/Enlisted/progression_config.json`

```json
{
  "medical_risk": {
    "progressionConfig": {
      "track": "medical_risk",
      "enabled": true,
      
      "tick": {
        "type": "daily",
        "hour": 6,
        "skipDuring": ["battle", "muster"]
      },
      
      "thresholdEvents": {
        "2": "evt_medical_onset",
        "3": "evt_medical_worsening",
        "4": "evt_medical_complication",
        "5": "evt_medical_emergency"
      },
      
      "baseChances": {
        "1": { "improve": 40, "stable": 50, "worsen": 10 },
        "2": { "improve": 25, "stable": 55, "worsen": 20 },
        "3": { "improve": 15, "stable": 50, "worsen": 35 },
        "4": { "improve": 10, "stable": 45, "worsen": 45 },
        "5": { "improve": 5, "stable": 40, "worsen": 55 }
      },
      
      "criticalRolls": {
        "luckyRange": [1, 5],
        "luckyEffect": -2,
        "complicationRange": [96, 100],
        "complicationEffect": 2
      },
      
      "skillModifier": {
        "skill": "Medicine",
        "divisor": 3,
        "maxBonus": 20
      },
      
      "contextModifiers": {
        "resting": { "improve": 10, "worsen": -10 },
        "marching": { "improve": -10, "worsen": 10 },
        "afterBattle": { "improve": -15, "worsen": 15 },
        "treated": { "improve": 25, "worsen": -25 },
        "starving": { "improve": -10, "worsen": 15 },
        "wellFed": { "improve": 5, "worsen": -5 }
      },
      
      "worldStateModifiers": {
        "garrison": { "improve": 10, "worsen": -10, "tickMultiplier": 0.8 },
        "campaign": { "improve": 0, "worsen": 0, "tickMultiplier": 1.0 },
        "siege": { "improve": -15, "worsen": 15, "tickMultiplier": 1.2 }
      },
      
      "clamps": {
        "improveMin": 5,
        "improveMax": 80,
        "worsenMin": 5,
        "worsenMax": 70
      }
    }
  }
}
```

---

## Treatment Decision

Player can visit the surgeon to set the `treated` flag:

```json
{
  "id": "dec_seek_treatment",
  "category": "decision",
  "titleId": "dec_seek_treatment_title",
  "title": "Seek Treatment",
  "setupId": "dec_seek_treatment_setup",
  "setup": "The surgeon's tent. Herbs, poultices, and the smell of blood.",
  "requirements": {
    "escalation": { "medical_risk": { "min": 1 } }
  },
  "timing": {
    "cooldown_days": 1
  },
  "options": [
    {
      "id": "full_treatment",
      "textId": "dec_seek_treatment_full",
      "text": "Submit to the surgeon's care.",
      "tooltipId": "dec_seek_treatment_full_tooltip",
      "tooltip": "+25% recovery chance today. Costs time and dignity.",
      "costs": { "gold": 20 },
      "effects": { 
        "medical_risk": -1,
        "fatigue": 4
      },
      "setFlags": ["treated_today"]
    },
    {
      "id": "quick_treatment",
      "textId": "dec_seek_treatment_quick",
      "text": "Get herbs and go.",
      "tooltipId": "dec_seek_treatment_quick_tooltip",
      "tooltip": "+10% recovery chance. Quick but less effective.",
      "costs": { "gold": 5 },
      "setFlags": ["treated_today_minor"]
    },
    {
      "id": "leave",
      "textId": "dec_seek_treatment_leave",
      "text": "Changed my mind.",
      "tooltipId": "dec_seek_treatment_leave_tooltip",
      "tooltip": "Leave without treatment."
    }
  ]
}
```

---

## Orchestrator Integration

The ContentOrchestrator provides world state modifiers:

```csharp
// MedicalProgressionBehavior.cs
private void OnDailyTick()
{
    if (EscalationState.Instance.MedicalRisk <= 0)
        return;
    
    // Get modifiers from orchestrator
    var modifiers = ContentOrchestrator.Instance?.GetProgressionModifiers("medical_risk")
        ?? new ProgressionModifiers();
    
    // Calculate final probabilities
    var chances = CalculateChances(modifiers);
    
    // Roll and apply
    var result = RollProgression(chances);
    ApplyResult(result);
    
    // Clear daily flags
    EscalationState.Instance.ClearFlag("treated_today");
    EscalationState.Instance.ClearFlag("treated_today_minor");
}
```

---

## YOU Section Integration

Medical state appears in the YOU section:

```
You're wounded - movement impaired. Off duty until you recover. The medic says rest for a few days.
```

```
Feeling poorly. The surgeon gave you herbs. Rest today and hope for the best.
```

---

## Localization Keys

```xml
<!-- Medical Progression Messages -->
<string id="prog_medical_improve" text="Feeling stronger today." />
<string id="prog_medical_stable" text="" />
<string id="prog_medical_worsen" text="Condition worsened overnight." />
<string id="prog_medical_lucky" text="The fever broke. Nearly back to normal." />
<string id="prog_medical_complication" text="Took a bad turn in the night." />

<!-- Treatment Decision -->
<string id="dec_seek_treatment_title" text="Seek Treatment" />
<string id="dec_seek_treatment_setup" text="The surgeon's tent. Herbs, poultices, and the smell of blood." />
<string id="dec_seek_treatment_full" text="Submit to the surgeon's care." />
<string id="dec_seek_treatment_full_tooltip" text="+25% recovery chance today. Costs time and dignity." />
<string id="dec_seek_treatment_quick" text="Get herbs and go." />
<string id="dec_seek_treatment_quick_tooltip" text="+10% recovery chance. Quick but less effective." />
<string id="dec_seek_treatment_leave" text="Changed my mind." />
<string id="dec_seek_treatment_leave_tooltip" text="Leave without treatment." />
```

---

## Edge Cases

| Scenario | Handling |
|----------|----------|
| Medical Risk = 0 | No daily roll. System inactive. |
| Medical Risk = 5 | Worsen has no effect (at max). Optional death risk. |
| Rapid onset (+3 in one event) | Skip intermediate thresholds. Don't retroactively fire events. |
| Save/Load | All state persists. TreatedToday clears on day change. |
| Fast travel | Run missed ticks on arrival (batch roll). |

---

## Acceptance Criteria

- [ ] Daily tick at hour 6 checks MedicalRisk > 0
- [ ] Roll determines recovery/stable/worsen outcome
- [ ] Medicine skill modifies probabilities
- [ ] Rest/march context modifies probabilities
- [ ] `treated_today` flag modifies probabilities
- [ ] Orchestrator provides world state modifiers
- [ ] Threshold crossings fire events via EventDeliveryManager
- [ ] YOU section shows medical state
- [ ] All probabilities configurable in JSON
- [ ] State persists through save/load

---

## Implementation Order

This is a **future feature**. When ready to implement:

1. Create generic `ProgressionBehavior` base class
2. Create `MedicalProgressionBehavior` extending it
3. Add `progression_config.json`
4. Add `dec_seek_treatment` to decisions
5. Add localization keys
6. Wire to ContentOrchestrator for modifiers
7. Test all edge cases

---

**See Also:**
- [Progression System Schema](event-system-schemas.md#progression-system-schema-future-foundation) - Generic pattern
- [Content System Architecture](content-system-architecture.md) - Integration points
