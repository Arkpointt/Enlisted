# Medical Risk & Illness System

**Summary:** Medical risk tracking and illness onset system integrated with the Content Orchestrator. Medical risk accumulates from injuries, exhaustion, and harsh conditions, triggering context-aware illness onset events when thresholds are reached. Includes maritime vs land illness types, treatment tracking, and condition worsening mechanics.

**Status:** ✅ Implemented  
**Last Updated:** 2026-01-01  
**Implementation:** `src/Features/Content/ContentOrchestrator.cs` (CheckMedicalPressure), `src/Features/Conditions/PlayerConditionBehavior.cs`, `src/Features/Content/EventDeliveryManager.cs`  
**Related Docs:** [Injury & Illness System](injury-system.md), [Event System Schemas](event-system-schemas.md), [Content System Architecture](content-system-architecture.md)

---

## Overview

The medical risk system tracks player health status through a 0-5 escalation track. Medical risk increases from injuries, exhaustion, and harsh conditions, then triggers illness onset events when thresholds are reached. The Content Orchestrator monitors medical pressure daily and queues context-appropriate illness events (maritime vs land).

**Key Features:**
- Medical risk escalation track (0-5)
- Daily orchestrator checks with probability calculation
- Context-aware illness types (ship fever at sea, camp fever on land)
- Treatment system (resets risk, begins recovery)
- Condition worsening for untreated illnesses/injuries
- Maritime content variants with nautical flavor

---

## Medical Risk Escalation

Medical risk is an escalation track that accumulates from various sources and triggers illness onset when thresholds are reached.

**Risk Sources:**
- Injuries (each injury adds risk based on severity)
- Low HP (risk increases as HP drops)
- Exhaustion (low fatigue adds risk)
- Harsh conditions (siege, harsh terrain)
- Failed orders with injury risk
- Untreated existing conditions

**Risk Levels:**
| Risk Level | Status | Illness Trigger Chance |
|------------|--------|------------------------|
| 0 | Healthy | 0% |
| 1-2 | At Risk | 5-10% |
| 3 | Minor Illness Threshold | 15% |
| 4 | Moderate Illness Threshold | 20% |
| 5+ | Severe Illness Threshold | 25%+ |

**Risk Reduction:**
- Medical treatment (immediate -2 to -5 risk)
- Rest and recovery (gradual reduction)
- Successful rest decisions (-1 risk)
- Time passing while healthy

---

## Orchestrator Integration

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

The Content Orchestrator's `CheckMedicalPressure()` method runs daily at dawn to analyze medical risk and trigger appropriate responses.

**Daily Check Flow:**
1. Get medical pressure analysis from `SimulationPressureCalculator.GetMedicalPressure()`
2. Track consecutive high-pressure days
3. Queue medical opportunities if needed (treatment, preventive rest)
4. Check if illness onset should trigger
5. Calculate probability with modifiers (fatigue, siege, consecutive days)
6. Roll for illness and queue appropriate event (land or sea variant)

**Illness Onset Probability Calculation:**
```csharp
// Base: 5% per risk level
var baseChance = medicalRisk * 0.05f;  // Risk 3 = 15%, Risk 4 = 20%, Risk 5 = 25%

// Fatigue modifier
if (fatigue <= 8)
    baseChance += 0.10f;  // +10% if exhausted

// Siege modifier
if (inSiege)
    baseChance += 0.12f;  // +12% during siege

// Consecutive pressure modifier
baseChance += consecutiveHighPressureDays * 0.05f;  // +5% per day

// Cap at 50%
baseChance = Math.Min(baseChance, 0.50f);
```

**Example Scenario:**
- Medical risk: 4 (from injury + low HP)
- Currently in siege: +12%
- 2 consecutive high-pressure days: +10%
- Final chance: 20% + 12% + 10% = 42% illness onset

**Cooldowns:**
- 7-day cooldown between illness onset events
- Prevents illness spam during sustained campaigns
- Still allows condition worsening events for existing illnesses

---

## Maritime Context Selection

The orchestrator automatically selects maritime vs land illness events based on party travel context.

**Event Selection Logic:**
```csharp
// Detect travel context
var isAtSea = party.IsCurrentlyAtSea;
var contextSuffix = isAtSea ? "_sea" : "";

// Select base event by risk level
var baseEventId = medicalRisk >= 5 ? "illness_onset_severe" 
                : medicalRisk >= 4 ? "illness_onset_moderate"
                : "illness_onset_minor";

// Try context variant first, fall back to base
var eventId = baseEventId + contextSuffix;
var eventDef = EventCatalog.GetEvent(eventId);
if (eventDef == null && isAtSea)
{
    // Sea variant missing, use land version
    eventId = baseEventId;
    eventDef = EventCatalog.GetEvent(eventId);
}
```

**Available Events:**
| Base Event | Sea Variant | Triggered By |
|------------|-------------|--------------|
| `illness_onset_minor` | `illness_onset_minor_sea` | Medical Risk 3 |
| `illness_onset_moderate` | `illness_onset_moderate_sea` | Medical Risk 4 |
| `illness_onset_severe` | `illness_onset_severe_sea` | Medical Risk 5+ |

**Sea Event Features:**
- Nautical setting (ship's hold, mainmast, sick bay)
- Maritime characters (ship's surgeon, old hand with grog)
- Sea-appropriate remedies (lime juice, salt air, rum)
- Ship-specific dangers (fever-induced near-overboard)

---

## Medical Opportunities

When medical pressure is detected, the orchestrator queues contextual opportunities to the Camp Opportunity Generator.

**Opportunity Types:**
| Pressure Level | Condition | Opportunity Queued |
|----------------|-----------|-------------------|
| Critical | Severe+ condition | `opp_urgent_medical` |
| High | Untreated condition | `opp_seek_medical_care` |
| Moderate+ | High risk, no condition yet | `opp_preventive_rest` |

**Opportunity Behavior:**
- Opportunities appear in DECISIONS menu as player-initiated actions
- One medical opportunity queued per day (prevents spam)
- Opportunities are filtered by condition state requirements (automatically disappear when player recovers)
- Integrate with existing Camp Opportunity system

**Example Flow:**
1. Player has untreated moderate illness (3 days)
2. Morning orchestrator check: `opp_seek_medical_care` queued
3. Player opens DECISIONS → "Seek Medical Care" appears
4. Player chooses surgeon treatment
5. Treatment begins, medical risk resets, opportunity cleared

---

## Treatment System

Treatment decisions set the `UnderMedicalCare` flag and apply recovery benefits.

**Treatment Effects (via `EventDeliveryManager.ApplyBeginTreatment()`):**
```csharp
public void ApplyBeginTreatment(float recoveryMultiplier, string reason)
{
    conditions.State.UnderMedicalCare = true;
    conditions.State.RecoveryMultiplier = recoveryMultiplier;
    escalationManager.SetEscalation("MedicalRisk", 0);  // Reset risk
    ModLogger.Info($"Treatment begun: {reason}, multiplier={recoveryMultiplier}");
}
```

**Treatment Decisions:**
| Decision | Context | Cost | Effect |
|----------|---------|------|--------|
| `dec_medical_surgeon` / `dec_medical_surgeon_sea` | Any condition | 100g | +20 HP, -5 risk, begins treatment |
| `dec_medical_emergency` / `dec_medical_emergency_sea` | Severe+ condition | 200g | +30 HP, -10 risk, begins treatment |
| `dec_medical_rest` / `dec_medical_rest_sea` | Any condition | Free | +10 HP, -1 risk, passive recovery |
| `dec_medical_herbal` / `dec_medical_grog_sea` | Any condition | 30g | 50% success, +15 HP or +5 HP |

**Recovery Multiplier:**
- Default: 1.0x (normal recovery)
- Professional treatment: 1.5-2.0x (from config)
- Emergency care: 2.5x (rapid recovery)

---

## Condition Worsening

Untreated conditions trigger worsening events after 3+ days without medical care.

**Worsening Events:**
| Event | Trigger Condition | Narrative Theme |
|-------|-------------------|-----------------|
| `untreated_condition_worsening` | Land illness/injury | Camp surgeon urges treatment |
| `untreated_condition_worsening_sea` | Maritime illness | Ship conditions worsen illness |

**Illness-Type Matching:**
```json
{
  "id": "untreated_condition_worsening_sea",
  "triggers": {
    "all": ["has_untreated_condition", "has_maritime_illness"]
  }
}
```

This ensures:
- Ship fever shows nautical worsening, even if player lands
- Camp fever shows camp worsening, even if player boards ship
- Narrative consistency with illness origin

**Worsening Outcome:**
60% chance to increase severity:
- Minor → Moderate
- Moderate → Severe  
- Severe → Critical
- Critical → (cannot worsen further, remains critical)

**Prevention:**
- Seek treatment before 3 days untreated
- Treatment resets days-untreated counter
- Medical risk drops, preventing further escalation

---

## Implementation Files

| File | Purpose |
|------|---------|
| `src/Features/Content/ContentOrchestrator.cs` | `CheckMedicalPressure()`, `CheckIllnessOnsetTriggers()`, medical opportunity queuing |
| `src/Features/Conditions/PlayerConditionBehavior.cs` | Condition state tracking, recovery processing, treatment flags |
| `src/Features/Content/EventDeliveryManager.cs` | `ApplyIllnessOnset()`, `ApplyBeginTreatment()`, `ApplyWorsenCondition()`, context-aware illness selection |
| `src/Features/Content/EventRequirementChecker.cs` | `CheckHasUntreatedCondition()`, `CheckHasMaritimeIllness()`, `CheckHasLandIllness()` |
| `src/Features/Escalation/EscalationManager.cs` | Medical risk escalation tracking |
| `ModuleData/Enlisted/Conditions/condition_defs.json` | Illness definitions (camp_fever, flux, ship_fever, scurvy) |
| `ModuleData/Enlisted/Events/illness_onset.json` | 7 illness onset events (3 land, 3 sea, 2 worsening) |
| `ModuleData/Enlisted/Decisions/medical_decisions.json` | 8 medical decisions (4 land, 4 sea) |

---

**End of Document**

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
