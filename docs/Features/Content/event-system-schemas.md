# Event System Schemas

**Summary:** Authoritative JSON schema definitions for events, decisions, and orders. This document specifies the exact field names the parser expects. When in doubt, **this document is the source of truth**.

**Status:** ✅ Current  
**Last Updated:** 2025-12-22 (Phase 6: Map incident contexts)  
**Related Docs:** [Content System Architecture](content-system-architecture.md), [Event Catalog](../../Content/event-catalog-by-system.md)

---

## Critical Rules

1. **Root array must be named `"events"`** - Even for decisions, the parser expects the array to be called `"events"`, not `"decisions"`.

2. **Category determines content type:**
   - `"category": "decision"` → Camp Hub decisions (required for dec_* prefixed items)
   - Other categories → Automatic events

3. **ID prefixes determine delivery mechanism:**
   - `dec_*` → Player-initiated Camp Hub menu items
   - `player_*` → Player-initiated popup inquiries  
   - `decision_*` → Game-triggered popup inquiries
   - Other → Automatic events

4. **Rewards vs Effects:** 
   - `"rewards"` = Player gains (fatigueRelief, gold, skillXp)
   - `"effects"` = State changes (reputation, escalation, hpChange)

---

## File Structure

### Required Root Structure

```json
{
  "schemaVersion": 2,
  "category": "decision",
  "events": [
    { ... event/decision definitions ... }
  ]
}
```

### File Locations

| Content Type | Location | ID Prefix |
|-------------|----------|-----------|
| Camp Hub Decisions | `ModuleData/Enlisted/Decisions/decisions.json` | `dec_*` |
| Player Popup Events | `ModuleData/Enlisted/Events/events_player_decisions.json` | `player_*` |
| Game-Triggered Events | `ModuleData/Enlisted/Events/events_decisions.json` | `decision_*` |
| Automatic Events | `ModuleData/Enlisted/Events/events_*.json` | `evt_*` |
| Map Incidents | `ModuleData/Enlisted/Events/incidents_*.json` | `mi_*` |

---

## Event/Decision Definition

### Core Fields

```json
{
  "id": "dec_rest",
  "category": "decision",
  "titleId": "dec_rest_title",
  "title": "Rest",
  "setupId": "dec_rest_setup", 
  "setup": "Your legs ache from the march...",
  "requirements": { ... },
  "timing": { ... },
  "options": [ ... ]
}
```

| Field | JSON Name | Type | Required | Notes |
|-------|-----------|------|----------|-------|
| ID | `id` | string | ✅ | Must be unique. Prefix determines delivery. |
| Category | `category` | string | ✅ | Must be `"decision"` for Camp Hub decisions |
| Title ID | `titleId` | string | ✅ | XML localization key |
| Title Fallback | `title` | string | ❌ | Shown if localization missing |
| Setup ID | `setupId` | string | ✅ | XML localization key for description |
| Setup Fallback | `setup` | string | ❌ | Shown if localization missing |

---

## Requirements Object

All fields are optional. If omitted, no restriction applies.

### JSON Field Names (Parser Accepts Both)

```json
{
  "requirements": {
    "tier": { "min": 1, "max": 9 },
    "context": "Any",
    "role": "Any",
    "hp_below": 80,
    "minSkills": { "Scouting": 50 },
    "minTraits": { "Honor": 3 },
    "minEscalation": { "Scrutiny": 5 }
  }
}
```

| Field | JSON Names | Type | Range | Notes |
|-------|------------|------|-------|-------|
| Tier Range | `tier.min`, `tier.max` or `minTier`, `maxTier` | int | 1-9 | Enlistment tier |
| Context | `context` | string | See list | Campaign context |
| Role | `role` | string | See list | Player's primary role |
| HP Below | `hp_below` or `hpBelow` | int | 0-100 | % HP threshold |
| Min Skills | `minSkills` | dict | - | Skill name → level |
| Min Traits | `minTraits` | dict | - | Trait name → level |
| Min Escalation | `minEscalation` | dict | - | Track → threshold |
| Onboarding Stage | `onboarding_stage` | int | 1-3 | Onboarding events only |
| Onboarding Track | `onboarding_track` | string | - | green/seasoned/veteran |

### Valid Context Values

**General Event Contexts:**
`"Any"`, `"War"`, `"Peace"`, `"Siege"`, `"Battle"`, `"Town"`, `"Camp"`

**Map Incident Contexts:**
`"leaving_battle"`, `"during_siege"`, `"entering_town"`, `"entering_village"`, `"leaving_settlement"`, `"waiting_in_settlement"`

Map incident contexts are used by `MapIncidentManager` to filter incidents based on player actions (battle end, settlement entry/exit, hourly checks during garrison/siege).

### Valid Role Values  
`"Any"`, `"Soldier"`, `"Scout"`, `"Medic"`, `"Engineer"`, `"Officer"`, `"Operative"`, `"NCO"`

---

## Timing Object

```json
{
  "timing": {
    "cooldown_days": 3,
    "priority": "normal",
    "one_time": false
  }
}
```

| Field | JSON Names | Type | Default | Notes |
|-------|------------|------|---------|-------|
| Cooldown | `cooldown_days` or `cooldownDays` | int | 7 | Days before re-available |
| Priority | `priority` | string | `"normal"` | low/normal/high/critical |
| One-Time | `one_time` or `oneTime` | bool | false | Fire once per playthrough |

---

## Options Array

Each option represents a player choice.

```json
{
  "options": [
    {
      "id": "rest_short",
      "textId": "dec_rest_short",
      "text": "Find a shady spot and rest.",
      "costs": { ... },
      "rewards": { ... },
      "effects": { ... },
      "resultTextId": "dec_rest_short_result",
      "resultText": "You feel refreshed."
    }
  ]
}
```

| Field | JSON Name | Type | Required | Notes |
|-------|-----------|------|----------|-------|
| ID | `id` | string | ✅ | Unique within event |
| Text ID | `textId` | string | ✅ | XML localization key |
| Text Fallback | `text` | string | ❌ | Button text if loc missing |
| Tooltip | `tooltip` | string | ❌ | Hover hint |
| Costs | `costs` | object | ❌ | Resources deducted |
| Rewards | `rewards` | object | ❌ | Resources gained |
| Effects | `effects` | object | ❌ | State changes |
| Result Text ID | `resultTextId` | string | ❌ | XML key for outcome |
| Result Text | `resultText` | string | ❌ | Outcome fallback text |
| Risk | `risk` | string | ❌ | safe/moderate/risky/dangerous |
| Risk Chance | `risk_chance` or `riskChance` | int | ❌ | 0-100, success % |
| Set Flags | `set_flags` or `setFlags` | array | ❌ | Flags to set |
| Clear Flags | `clear_flags` or `clearFlags` | array | ❌ | Flags to clear |
| Chains To | `chains_to` or `chainsTo` | string | ❌ | Follow-up event ID |
| Chain Delay | `chain_delay_hours` or `chainDelayHours` | int | ❌ | Hours before chain fires |

---

## Costs Object

Deducted when option is selected.

```json
{
  "costs": {
    "gold": 30,
    "fatigue": 2,
    "time_hours": 4
  }
}
```

| Field | JSON Names | Type | Notes |
|-------|------------|------|-------|
| Gold | `gold` | int | Deducted from player |
| Fatigue | `fatigue` | int | Added to fatigue |
| Time | `time_hours` or `timeHours` | int | Campaign hours |

---

## Rewards Object

Gained when option is selected. **Used for positive gains.**

```json
{
  "rewards": {
    "gold": 50,
    "fatigueRelief": 3,
    "skillXp": { "OneHanded": 25, "Athletics": 10 }
  }
}
```

| Field | JSON Names | Type | Notes |
|-------|------------|------|-------|
| Gold | `gold` | int | Added to player |
| Fatigue Relief | `fatigueRelief` or `fatigue_relief` | int | Reduces fatigue |
| Skill XP | `skillXp` | dict | Skill name → XP amount |
| Dynamic Skill XP | `dynamicSkillXp` | dict | `"equipped_weapon"` or `"weakest_combat"` → XP |

**⚠️ IMPORTANT:** `fatigueRelief` goes in `rewards`, NOT in `effects`!

---

## Effects Object

State changes when option is selected. Can be positive or negative.

```json
{
  "effects": {
    "soldierRep": 5,
    "officerRep": -3,
    "lordRep": 2,
    "scrutiny": 3,
    "discipline": -2,
    "medicalRisk": -1,
    "hpChange": -10,
    "gold": -50,
    "troopXp": 20
  }
}
```

| Field | JSON Names | Type | Range | Notes |
|-------|------------|------|-------|-------|
| Soldier Rep | `soldierRep` or `soldier_rep` | int | -50 to +50 | |
| Officer Rep | `officerRep` or `officer_rep` | int | -100 to +100 | |
| Lord Rep | `lordRep` or `lord_rep` | int | -100 to +100 | |
| Scrutiny | `scrutiny` | int | delta | Escalation track |
| Discipline | `discipline` | int | delta | Escalation track |
| Medical Risk | `medicalRisk` or `medical_risk` | int | delta | Escalation track |
| HP Change | `hpChange` or `hp_change` | int | delta | **NOT `hp`!** |
| Gold | `gold` | int | delta | Positive = gain |
| Troop Loss | `troopLoss` or `troop_loss` | int | count | Party troops killed |
| Troop Wounded | `troopWounded` or `troop_wounded` | int | count | Party troops wounded |
| Food Loss | `foodLoss` or `food_loss` | int | count | Food items removed |
| Troop XP | `troopXp` or `troop_xp` | int | amount | XP to lord's T1-T3 troops |
| Skill XP | `skillXp` | dict | - | Skill name → XP |
| Trait XP | `traitXp` | dict | - | Trait name → XP |
| Company Needs | `companyNeeds` | dict | - | Need name → delta |
| Apply Wound | `applyWound` or `apply_wound` | string | - | Minor/Serious/Permanent |
| Chain Event | `chainEventId` | string | - | Immediate follow-up |
| Discharge | `triggersDischarge` | string | - | dishonorable/washout/deserter |
| Renown | `renown` | int | delta | Clan renown |

**⚠️ COMMON MISTAKES:**
- Use `hpChange`, not `hp`
- Use `fatigueRelief` in `rewards`, not `effects`
- Use `soldierRep` (camelCase), parser also accepts `soldier_rep`

---

## Company Needs Keys

For `effects.companyNeeds`:

```json
{
  "effects": {
    "companyNeeds": {
      "Readiness": -5,
      "Morale": 3,
      "Supplies": -10,
      "Equipment": 2,
      "Rest": 5
    }
  }
}
```

---

## Risky Options

Options with success/failure outcomes:

```json
{
  "id": "gamble_high",
  "text": "Bet big.",
  "risk": "risky",
  "risk_chance": 50,
  "effects": {
    "soldierRep": 1
  },
  "effects_success": {
    "gold": 100
  },
  "effects_failure": {
    "gold": -100,
    "soldierRep": -2
  },
  "resultText": "You win!",
  "failure_resultText": "You lose everything."
}
```

| Field | Notes |
|-------|-------|
| `effects` | Applied always (before roll) |
| `effects_success` | Applied on success |
| `effects_failure` | Applied on failure |
| `resultText` | Shown on success |
| `failure_resultText` | Shown on failure |

---

## Skill Names (Valid Values)

`OneHanded`, `TwoHanded`, `Polearm`, `Bow`, `Crossbow`, `Throwing`, `Riding`, `Athletics`, `Scouting`, `Tactics`, `Roguery`, `Charm`, `Leadership`, `Trade`, `Steward`, `Medicine`, `Engineering`

---

## Trait Names (Valid Values)

`Honor`, `Valor`, `Mercy`, `Generosity`, `Calculating`

---

## Escalation Track Names

For `requirements.minEscalation` and `effects`:

`Scrutiny`, `Discipline`, `MedicalRisk` (or `medical_risk`), `SoldierReputation`, `LordReputation`, `OfficerReputation`

---

## Complete Decision Example

```json
{
  "schemaVersion": 2,
  "category": "decision",
  "events": [
    {
      "id": "dec_spar",
      "category": "decision",
      "titleId": "dec_spar_title",
      "title": "Spar with a Comrade",
      "setupId": "dec_spar_setup",
      "setup": "A soldier offers to trade blows in the practice yard.",
      "requirements": {
        "tier": { "min": 1, "max": 999 }
      },
      "timing": {
        "cooldown_days": 1,
        "priority": "normal"
      },
      "options": [
        {
          "id": "spar_friendly",
          "textId": "dec_spar_friendly",
          "text": "Keep it friendly.",
          "costs": {
            "fatigue": 2
          },
          "rewards": {
            "skillXp": { "OneHanded": 25, "Athletics": 10 }
          },
          "effects": {
            "soldierRep": 2
          },
          "resultTextId": "dec_spar_friendly_result",
          "resultText": "A good practice session. You both learn something."
        },
        {
          "id": "spar_hard",
          "textId": "dec_spar_hard",
          "text": "Make it count.",
          "costs": {
            "fatigue": 4
          },
          "rewards": {
            "skillXp": { "OneHanded": 40, "Athletics": 20 }
          },
          "effects": {
            "soldierRep": 3,
            "hpChange": -8
          },
          "resultTextId": "dec_spar_hard_result",
          "resultText": "Blood is drawn. Word spreads about this bout."
        }
      ]
    }
  ]
}
```

---

## Validation Checklist

Before committing content:

- [ ] Root array is `"events"` (not `"decisions"`)
- [ ] All items have `"category": "decision"` for Camp Hub decisions
- [ ] ID prefixes match delivery mechanism (`dec_*` for Camp Hub)
- [ ] `fatigueRelief` is in `rewards`, not `effects`
- [ ] HP changes use `hpChange`, not `hp`
- [ ] All string IDs have matching entries in `enlisted_strings.xml`
- [ ] Skill/trait names match valid values exactly (case-sensitive)

---

**End of Document**
