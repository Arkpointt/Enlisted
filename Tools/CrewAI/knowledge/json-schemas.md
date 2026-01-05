# Enlisted JSON Schemas

**Canonical Reference:** `docs/Features/Content/event-system-schemas.md`

## File Structure

All event/decision files use this root structure:
```json
{
  "schemaVersion": 2,
  "category": "decision",
  "events": [ ... ]
}
```

**CRITICAL:** Root array must be named `"events"` even for decisions.

## Event/Decision Schema

```json
{
  "id": "dec_rest",
  "category": "decision",
  "severity": "normal",
  "titleId": "dec_rest_title",
  "title": "Rest",
  "setupId": "dec_rest_setup",
  "setup": "Your legs ache...",
  "requirements": { ... },
  "timing": { ... },
  "options": [ ... ]
}
```

## ID Prefixes & Delivery

| Prefix | Delivery | Used For |
|--------|----------|----------|
| `dec_*` | Camp Hub inline menu | Player-initiated decisions |
| `player_*` | Popup inquiry | Player-initiated events |
| `decision_*` | Popup inquiry | Game-triggered events |
| `evt_*` | Popup inquiry | Automatic events |
| `mi_*` | Popup inquiry | Map incidents |

## Requirements Object

```json
{
  "requirements": {
    "tier": { "min": 1, "max": 9 },
    "context": ["Camp", "War"],
    "role": "Scout",
    "notAtSea": true,
    "hasAnyCondition": true,
    "minSkills": { "Scouting": 50 }
  }
}
```

**Valid contexts:** `Any`, `War`, `Peace`, `Siege`, `Battle`, `Town`, `Camp`
**Valid roles:** `Any`, `Soldier`, `Scout`, `Medic`, `Engineer`, `Officer`, `NCO`

**Common skill names:**
- Combat: `OneHanded`, `TwoHanded`, `Polearm`, `Bow`, `Crossbow`, `Throwing`, `Athletics`
- Leadership: `Leadership`, `Tactics`, `Steward`, `Charm`
- Specialist: `Scouting`, `Medicine`, `Engineering`, `Trade`, `Roguery`

## Effects vs Rewards

- `effects` = State changes for **main options** (reputation, escalation, skillXp)
- `rewards` = Player gains for **sub-choices only**

**CRITICAL:** For main options, use `effects.skillXp`. Only use `rewards.skillXp` in sub-choice options.

### Common Effect Types

**Resources:**
```json
{
  "gold": 100,
  "hpChange": -10,
  "fatigueChange": 2
}
```

**Reputation:**
```json
{
  "officerRep": 10,
  "soldierRep": -5,
  "lordRep": 3
}
```

**Escalation:**
```json
{
  "scrutiny": 5,
  "discipline": 2,
  "medicalRisk": -3
}
```

**Skills:**
```json
{
  "skillXp": {
    "OneHanded": 25,
    "Leadership": 15,
    "Scouting": 30
  }
}
```

**Medical:**
```json
{
  "applyInjury": "twisted_knee",
  "applyIllness": "camp_fever",
  "beginTreatment": true,
  "medicalRisk": -5
}
```

**Company Needs:**
```json
{
  "companyNeeds": {
    "supplies": 10,
    "morale": -5,
    "readiness": 3
  }
}
```

## Option Schema

```json
{
  "id": "option_1",
  "textId": "dec_rest_opt1",
  "text": "Rest here.",
  "tooltip": "Recover fatigue.",
  "costs": { "fatigue": 2 },
  "effects": {
    "gold": 50,
    "officer_rep": 5,
    "skillXp": { "OneHanded": 25 }
  },
  "resultTextId": "dec_rest_opt1_result",
  "resultText": "You feel rested."
}
```

## Risky Options

```json
{
  "risk_chance": 0.60,
  "effects_success": { "gold": 100 },
  "effects_failure": { "scrutiny": 10 }
}
```

## Medical System

**Conditions:** `ModuleData/Enlisted/Conditions/condition_defs.json`
**Injuries:** `ModuleData/Enlisted/Content/injuries.json`

### Injury Types
- `twisted_knee`, `blade_cut`, `arrow_wound` (in condition_defs.json)
- 14 injury variants with severity levels in injuries.json

### Illness Types
- Land: `camp_fever`, `flux`
- Maritime: `ship_fever`, `scurvy`

### Severity Levels
- Injuries: `minor`, `moderate`, `severe`, `critical`
- Illnesses: `mild`, `moderate`, `severe`, `critical`

**Medical Risk:** 0-5 escalation track (triggers illness onset at 3+)

## Camp Opportunities

29 opportunities in `camp_opportunities.json`, key fields:
```json
{
  "id": "opp_weapon_drill",
  "type": "training",
  "targetDecision": "dec_training_drill",
  "validPhases": ["Dawn", "Midday"],
  "baseFitness": 55,
  "orderCompatibility": { "guard_duty": "risky" },
  "notAtSea": true
}
```

**Opportunity Types:** `training`, `social`, `economic`, `recovery`
**Order Compatibility:** `available`, `risky`, `blocked`

## File Locations

| Content | Location |
|---------|----------|
| Camp Hub Decisions | `ModuleData/Enlisted/Decisions/decisions.json` |
| Camp Opportunities | `ModuleData/Enlisted/Decisions/camp_opportunities.json` (29 opps) |
| Medical Decisions | `ModuleData/Enlisted/Decisions/medical_decisions.json` |
| Events | `ModuleData/Enlisted/Events/events_*.json` (8 files) |
| Map Incidents | `ModuleData/Enlisted/Events/incidents_*.json` (7 files) |
| Order Events | `ModuleData/Enlisted/Orders/order_events/*.json` (16 files, 84 events) |
| Orders | `ModuleData/Enlisted/Orders/orders_t1_t3.json` (+ t4_t6, t7_t9) |
| Conditions | `ModuleData/Enlisted/Conditions/condition_defs.json` |
| Injuries | `ModuleData/Enlisted/Content/injuries.json` (14 types) |
| QM Dialogue | `ModuleData/Enlisted/Dialogue/qm_*.json` (4 files) |
| Localization | `ModuleData/Languages/enlisted_strings.xml` |
