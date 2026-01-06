# Enlisted JSON Schemas

**Canonical Reference:** `docs/Features/Content/event-system-schemas.md`

## CRITICAL: Dual-File Content System

Enlisted uses a **TWO-FILE SYSTEM** for all player-visible text:

1. **JSON files** (`ModuleData/Enlisted/Events/*.json`) - Event structure, effects, logic
2. **XML file** (`ModuleData/Languages/enlisted_strings.xml`) - Localized text strings

**Every event with text MUST have strings in BOTH files.**

### Content Creation Workflow

```
1. Write JSON event with:
   - "titleId": "my_event_title"
   - "title": "Fallback Title"      ← BOTH required!
   - "setupId": "my_event_setup"
   - "setup": "Fallback setup..."   ← BOTH required!
   - Same for option text/tooltips

2. Run sync_strings tool:
   $ python Tools/Validation/sync_event_strings.py
   → Adds missing entries to enlisted_strings.xml

3. Verify with validate_content:
   $ python Tools/Validation/validate_content.py
   → Checks JSON structure AND XML references
```

### Why Both Files?

- **JSON `title`/`setup`** = Fallback if XML missing (dev safety)
- **XML `enlisted_strings.xml`** = Actual displayed text (supports translations)
- Validator warns if JSON references an ID not in XML

### Agent Workflow

When creating content, use these tools in order:
1. `draft_event` or `write_event` → Creates JSON
2. `sync_strings` → Updates XML
3. `validate_content` → Final check

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

## REQUIRED: Valid Categories & Severities

**Using invalid values causes validation ERROR (blocks commit).**

### Valid Categories

| Category | Used For |
|----------|----------|
| `crisis` | Pressure arc events, supply/morale crises |
| `decision` | Player-initiated camp decisions |
| `escalation` | Escalation track events |
| `general` | Generic events |
| `map_incident` | Location-triggered events |
| `medical` | Illness/injury events |
| `muster` | 12-day muster events |
| `onboarding` | Tutorial/early-game events |
| `pay` | Pay tension/mutiny events |
| `promotion` | Rank advancement events |
| `retinue` | T7+ retinue events |
| `role` | Role-specific events (Scout, Medic, etc.) |
| `threshold` | Escalation threshold crossings |
| `training` | Training-related events |
| `universal` | Any-tier events |

### Valid Severities

| Severity | Priority | Used For |
|----------|----------|----------|
| `normal` | Low | Standard events, routine |
| `positive` | Low | Good news, rewards |
| `attention` | Medium | Escalating issues, warnings |
| `urgent` | High | Time-sensitive problems |
| `critical` | Highest | Crises, major consequences |

**Example:** Supply pressure uses escalating severity:
- Stage 1 (Day 3): `"severity": "normal"`
- Stage 2 (Day 5): `"severity": "attention"`
- Crisis (Day 7): `"severity": "critical"`

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

36 opportunities in `camp_opportunities.json`, key fields:
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
| Camp Opportunities | `ModuleData/Enlisted/Decisions/camp_opportunities.json` (36 opps) |
| Medical Decisions | `ModuleData/Enlisted/Decisions/medical_decisions.json` |
| Events | `ModuleData/Enlisted/Events/events_*.json` (8 files) |
| Map Incidents | `ModuleData/Enlisted/Events/incidents_*.json` (7 files) |
| Order Events | `ModuleData/Enlisted/Orders/order_events/*.json` (16 files, 84 events) |
| Orders | `ModuleData/Enlisted/Orders/orders_t1_t3.json` (+ t4_t6, t7_t9) |
| Conditions | `ModuleData/Enlisted/Conditions/condition_defs.json` |
| Injuries | `ModuleData/Enlisted/Content/injuries.json` (14 types) |
| QM Dialogue | `ModuleData/Enlisted/Dialogue/qm_*.json` (4 files) |
| Localization | `ModuleData/Languages/enlisted_strings.xml` |
