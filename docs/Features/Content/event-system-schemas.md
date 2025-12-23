# Event System Schemas

**Summary:** Authoritative JSON schema definitions for events, decisions, and orders. This document specifies the exact field names the parser expects. When in doubt, **this document is the source of truth**.

**Status:** ✅ Current  
**Last Updated:** 2025-12-23 (Added dialogue schema reference)  
**Related Docs:** [Content System Architecture](content-system-architecture.md), [Event Catalog](../../Content/event-catalog-by-system.md), [Quartermaster System](../Equipment/quartermaster-system.md)

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

## Triggers Object

**CRITICAL:** The `triggers` object controls **when** an event can fire through flag checks and escalation thresholds. This is **separate from** the `requirements` object which controls player eligibility.

⚠️ **Common Bug:** Events with `triggers` but missing validation will fire incorrectly. The system now validates all trigger conditions before event selection.

```json
{
  "triggers": {
    "all": ["is_enlisted", "has_flag:mutiny_joined"],
    "any": [],
    "none": ["has_flag:mutiny_failed"],
    "time_of_day": ["night"],
    "escalation_requirements": {
      "pay_tension_min": 85,
      "scrutiny": 5
    }
  }
}
```

### Trigger Fields

| Field | JSON Name | Type | Purpose |
|-------|-----------|------|---------|
| All Conditions | `all` | array | ALL must be true for event to fire |
| Any Conditions | `any` | array | At least ONE must be true |
| None Conditions | `none` | array | NONE can be true (blocking conditions) |
| Time of Day | `time_of_day` | array | Time blocks when eligible |
| Escalation Requirements | `escalation_requirements` | object | Minimum escalation track values |

### Trigger Condition Strings

**Flag Checks:**
```json
"all": [
  "has_flag:mutiny_joined",        // Player has this flag
  "flag:completed_onboarding"      // Same as has_flag:
]
```

**Generic Conditions:**
```json
"all": [
  "is_enlisted",                   // Player is currently enlisted
  "LeavingBattle"                  // Map incident context (battle ended)
]
```

**Time of Day:**
```json
"time_of_day": ["night", "evening"]  // Only fires during these times
```

### Escalation Requirements

Minimum values for escalation tracks. Event only eligible when threshold reached:

```json
"escalation_requirements": {
  "pay_tension_min": 85,           // Pay tension must be 85+
  "scrutiny": 5,                   // Scrutiny must be 5+
  "discipline": 3,                 // Discipline must be 3+
  "medical_risk": 2                // Medical risk must be 2+
}
```

**Supported Tracks:**
- `pay_tension_min` / `pay_tension` - Pay tension (0-100)
- `scrutiny` - Crime suspicion (0-10)
- `discipline` - Rule-breaking record (0-10)
- `medical_risk` / `medicalrisk` - Health status (0-5)
- `soldierreputation` / `soldier_reputation` - Soldier rep threshold
- `officerreputation` / `officer_reputation` - Officer rep threshold
- `lordreputation` / `lord_reputation` - Lord rep threshold

### Triggers vs Requirements

| Object | Purpose | Validated By | When Checked |
|--------|---------|--------------|--------------|
| **triggers** | Controls when event CAN fire (context, flags, escalation) | EventSelector, MapIncidentManager | Before event enters selection pool |
| **requirements** | Controls player eligibility (tier, role, skills) | EventRequirementChecker | During selection filtering |

**Example: Mutiny Event**
```json
{
  "triggers": {
    "all": ["has_flag:mutiny_joined"],        // Only fires if joined mutiny
    "escalation_requirements": {}
  },
  "requirements": {
    "tier": { "min": 1, "max": 9 }            // Any tier can participate
  }
}
```

### Common Trigger Patterns

**Flag-Gated Chain Events:**
```json
"triggers": {
  "all": ["is_enlisted", "has_flag:event_completed"]
}
```

**High Escalation Events:**
```json
"triggers": {
  "all": ["is_enlisted"],
  "escalation_requirements": {
    "pay_tension_min": 85
  }
}
```

**Time-Specific Events:**
```json
"triggers": {
  "all": ["is_enlisted"],
  "time_of_day": ["night", "evening"]
}
```

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

## Global Event Pacing (enlisted_config.json)

**Location:** `enlisted_config.json` → `decision_events.pacing`

All automatic event timing is config-driven. These limits apply across ALL automatic event sources (EventPacingManager + MapIncidentManager) to ensure players aren't overwhelmed with events.

```json
{
  "decision_events": {
    "pacing": {
      "max_per_day": 2,
      "max_per_week": 8,
      "min_hours_between": 6,
      "event_window_min_days": 3,
      "event_window_max_days": 5,
      "per_event_cooldown_days": 7,
      "per_category_cooldown_days": 1,
      "evaluation_hours": [8, 14, 20],
      "allow_quiet_days": true,
      "quiet_day_chance": 0.15
    }
  }
}
```

### Pacing Fields

| Field | Type | Default | Purpose |
|-------|------|---------|---------|
| `max_per_day` | int | 2 | Maximum automatic events per day across ALL systems |
| `max_per_week` | int | 8 | Maximum automatic events per week |
| `min_hours_between` | int | 6 | Minimum hours between any automatic events |
| `event_window_min_days` | int | 3 | Minimum days between narrative event windows |
| `event_window_max_days` | int | 5 | Maximum days between narrative event windows |
| `per_event_cooldown_days` | int | 7 | Default cooldown before same event can fire again |
| `per_category_cooldown_days` | int | 1 | Cooldown between events of same category (narrative vs map_incident) |
| `evaluation_hours` | array | [8,14,20] | Campaign hours when narrative events can fire (map incidents skip this check) |
| `allow_quiet_days` | bool | true | Whether random quiet days can occur |
| `quiet_day_chance` | float | 0.15 | Chance of no automatic events on a given day (rolled once daily) |

### How Pacing Works

1. **EventPacingManager** checks if current time is within event window (every 3-5 days, config-driven)
2. **MapIncidentManager** fires context incidents on battles, settlements, sieges
3. **GlobalEventPacer** checks ALL limits before any automatic event fires:
   - `evaluation_hours`: Is current hour in allowed list?
   - `max_per_day` / `max_per_week`: Daily/weekly limits reached?
   - `min_hours_between`: Enough time since last event?
   - `per_category_cooldown_days`: Same category fired too recently?
   - `quiet_day_chance`: Is today a quiet day? (rolled once when first event is attempted)
4. Events blocked by any check wait until the condition clears

### Categories

| Category | Source | Evaluation Hours | Purpose |
|----------|--------|------------------|---------|
| `narrative` | EventPacingManager | ✅ Enforced | Paced narrative events (every 3-5 days) |
| `map_incident` | MapIncidentManager | ❌ Skipped | Context-triggered incidents fire immediately |

Map incidents skip `evaluation_hours` check because they're context-triggered (battle just ended, you entered a settlement). It would feel odd to delay the reaction.

Per-category cooldown ensures you don't get a narrative event and a map incident back-to-back.

**Player-selected decisions** (from Camp Hub menu) bypass all pacing since the player explicitly chose them.

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

For `requirements.minEscalation`, `triggers.escalation_requirements`, and `effects`:

**Primary Tracks:**
- `scrutiny` - Crime suspicion (0-10)
- `discipline` - Rule-breaking record (0-10)
- `medical_risk` / `medicalrisk` / `MedicalRisk` - Health status (0-5)
- `pay_tension` / `pay_tension_min` / `paytension` - Pay tension (0-100)

**Reputation Tracks:**
- `soldierreputation` / `soldier_reputation` / `SoldierReputation` - Soldier rep (-50 to +50)
- `officerreputation` / `officer_reputation` / `OfficerReputation` - Officer rep (0-100)
- `lordreputation` / `lord_reputation` / `LordReputation` - Lord rep (0-100)

**Note:** Parser accepts multiple naming variants (camelCase, snake_case, PascalCase) for compatibility.

---

## Text Placeholder Variables

All text fields (`title`, `setup`, `text`, `resultText`, etc.) support placeholder variables that are replaced at runtime with actual game data.

### Common Placeholders

| Category | Variables | Notes |
|----------|-----------|-------|
| **Player** | `{PLAYER_NAME}`, `{PLAYER_RANK}` | Player's name and current rank |
| **NCO/Officers** | `{SERGEANT}`, `{SERGEANT_NAME}`, `{NCO_NAME}`, `{OFFICER_NAME}`, `{CAPTAIN_NAME}` | NCO and officer references |
| **Soldiers** | `{SOLDIER_NAME}`, `{COMRADE_NAME}`, `{VETERAN_1_NAME}`, `{VETERAN_2_NAME}`, `{RECRUIT_NAME}` | Fellow soldiers in your unit |
| **Lord/Faction** | `{LORD_NAME}`, `{LORD_TITLE}`, `{FACTION_NAME}`, `{KINGDOM_NAME}` | Your enlisted lord and faction |
| **Location** | `{SETTLEMENT_NAME}`, `{COMPANY_NAME}` | Current settlement and party name |
| **Rank** | `{NEXT_RANK}`, `{SECOND_RANK}` | Rank progression context |

**Example Usage:**

```json
{
  "setup": "{SERGEANT} pulls you aside. 'Listen, {PLAYER_NAME}, we need someone to scout ahead. {LORD_NAME} wants intel on {SETTLEMENT_NAME} before we march.'",
  "text": "Tell {SERGEANT_NAME} you'll do it.",
  "resultText": "You report back to {SERGEANT}. The information proves valuable."
}
```

All variables use fallback values if data is unavailable (e.g., "the Sergeant" if no NCO name is set). See [Event Catalog - Placeholder Variables](../../Content/event-catalog-by-system.md#placeholder-variables) for the complete list.

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
- [ ] Flag-gated events use `triggers.all` with `has_flag:` conditions
- [ ] Pay tension events use `triggers.escalation_requirements.pay_tension_min`
- [ ] Escalation track names match supported values (case variants accepted)

---

## Related Schemas

### Quartermaster Dialogue Schema

The Quartermaster system uses a dedicated dialogue JSON schema for conversation trees. This follows similar patterns to the event system but with dialogue-specific features:

- **Context-conditional nodes** - Multiple nodes with same ID, different contexts
- **Gate nodes** - RP responses when player doesn't meet requirements
- **Actions** - Trigger UI/system behaviors (open Gauntlet, set flags)

**Location:** `ModuleData/Enlisted/Dialogue/qm_*.json`

**Schema:** Defined in `QMDialogueCatalog.cs` - see [Quartermaster System](../Equipment/quartermaster-system.md) for documentation.

**Key Differences from Events:**
| Feature | Events | Dialogue |
|---------|--------|----------|
| Root Array | `"events"` | `"nodes"` |
| Delivery | Popup inquiry | Conversation flow |
| Branching | `chainsTo` | `next_node` |
| Conditions | `requirements` + `triggers` | `context` + `gate` |
| Actions | `effects` | `action` + `action_data` |

---

**End of Document**
