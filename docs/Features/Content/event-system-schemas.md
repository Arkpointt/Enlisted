# Event System Schemas

**Summary:** Authoritative JSON schema definitions for events, decisions, and orders. This document specifies the exact field names the parser expects. When in doubt, **this document is the source of truth**.

**Status:** ✅ Current  
**Last Updated:** 2025-12-24 (Removed outcome popup system; resultText now displays in Recent Activities; reward_choices still use popups)  
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
   - `"rewards"` = Player gains for **sub-choices only** (reward_choices options)
   - `"effects"` = State changes and rewards for **main options** (reputation, escalation, hpChange, skillXp)
   - **⚠️ CRITICAL:** For main event options, use `effects.skillXp`. Only use `rewards.skillXp` in sub-choice options!

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

| Content Type | Location | ID Prefix | Delivery |
|-------------|----------|-----------|----------|
| Camp Hub Decisions | `ModuleData/Enlisted/Decisions/decisions.json` | `dec_*` | Inline menu |
| Player Popup Events | `ModuleData/Enlisted/Events/events_player_decisions.json` | `player_*` | Popup inquiry |
| Game-Triggered Events | `ModuleData/Enlisted/Events/events_decisions.json` | `decision_*` | Popup inquiry |
| Automatic Events | `ModuleData/Enlisted/Events/events_*.json` | `evt_*` | Popup inquiry |
| Map Incidents | `ModuleData/Enlisted/Events/incidents_*.json` | `mi_*` | Popup inquiry |
| **Muster Menu Stages** | *See note below* | `evt_muster_*` | **GameMenu stage** |

**Note on Muster Events:** Some events (`evt_muster_inspection`, `evt_muster_new_recruit`, `evt_baggage_*`) are delivered as GameMenu stages during the muster sequence rather than popup inquiries. The JSON definitions remain in `events_*.json` files, but `MusterMenuHandler` converts them to menu text instead of using `MultiSelectionInquiryData`. Effects still apply via `EventDeliveryManager.ApplyEffects()`. See [Muster Menu System](../Core/muster-menu-revamp.md).

---

## Event/Decision Definition

### Core Fields

```json
{
  "id": "dec_rest",
  "category": "decision",
  "severity": "normal",
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
| Severity | `severity` | string | ❌ | News priority/color: `"normal"`, `"positive"`, `"attention"`, `"urgent"`, `"critical"` (defaults to `"normal"`) |
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
| Context | `context` | string or array | See list | Campaign context(s) |
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

**Context as Array:**
```json
{
  "requirements": {
    "context": ["Camp", "Town"]  // Eligible in EITHER camp or town
  }
}
```

### Valid Role Values  
`"Any"`, `"Soldier"`, `"Scout"`, `"Medic"`, `"Engineer"`, `"Officer"`, `"Operative"`, `"NCO"`

---

## Content Variants Pattern

**Purpose:** Provide contextual variety by creating multiple versions of the same decision/event that fire in different situations.

### How Variants Work

The system automatically selects the best-fitting variant based on `requirements.context`:

1. Player enters situation (e.g., garrison, siege, campaign)
2. Orchestrator queries eligible content
3. EventRequirementChecker filters by context
4. Only matching variants pass filter
5. Orchestrator scores and selects best fit

**No code changes needed** - variants filter themselves via requirements.

### Variant Naming Convention

```
<base_id>                → Base version (any context)
<base_id>_<context>      → Context-specific variant
<base_id>_<intensity>    → Intensity variant (light/intense)
```

**Examples:**
- `dec_rest` → Base
- `dec_rest_garrison` → Garrison-specific
- `dec_rest_exhausted` → Siege-specific
- `dec_weapon_drill` → Base
- `dec_weapon_drill_light` → Low-intensity
- `dec_weapon_drill_intense` → High-intensity

### Basic Variant Example

```json
{
  "schemaVersion": 2,
  "category": "decision",
  "events": [
    {
      "id": "dec_rest",
      "category": "decision",
      "title": "Rest",
      "setup": "You need to rest.",
      "requirements": {
        "tier": { "min": 1 }
      },
      "options": [
        {
          "id": "rest",
          "text": "Find a spot and rest.",
          "costs": { "fatigue": 2 },
          "rewards": { "fatigueRelief": 5 }
        }
      ]
    },
    {
      "id": "dec_rest_garrison",
      "category": "decision",
      "title": "Rest (Garrison)",
      "setup": "You have time for proper rest.",
      "requirements": {
        "tier": { "min": 1 },
        "context": ["Camp"]  // ← Only in garrison
      },
      "options": [
        {
          "id": "rest_proper",
          "text": "Get proper rest in camp.",
          "costs": { "fatigue": 1 },  // ← Cheaper
          "rewards": { "fatigueRelief": 8 }  // ← More effective
        }
      ]
    },
    {
      "id": "dec_rest_exhausted",
      "category": "decision",
      "title": "Rest (Exhausted)",
      "setup": "You try to rest amid chaos.",
      "requirements": {
        "tier": { "min": 1 },
        "context": ["Siege", "Battle"]  // ← Only during crisis
      },
      "options": [
        {
          "id": "rest_crisis",
          "text": "Catch what rest you can.",
          "costs": { "fatigue": 2 },
          "rewards": { "fatigueRelief": 2 }  // ← Less effective
        }
      ]
    }
  ]
}
```

### Multi-Intensity Variant Example

```json
{
  "schemaVersion": 2,
  "category": "decision",
  "events": [
    {
      "id": "dec_weapon_drill",
      "category": "decision",
      "title": "Weapon Training",
      "requirements": { "tier": { "min": 1 } },
      "costs": { "fatigue": 2 },
      "effects": { "skillXp": { "OneHanded": 25 } }
    },
    {
      "id": "dec_weapon_drill_light",
      "category": "decision",
      "title": "Light Weapon Drill",
      "requirements": { 
        "tier": { "min": 1 },
        "context": ["Camp", "Town"]  // ← Garrison contexts
      },
      "costs": { "fatigue": 1 },  // ← Cheaper
      "effects": { "skillXp": { "OneHanded": 15 } }  // ← Less XP
    },
    {
      "id": "dec_weapon_drill_intense",
      "category": "decision",
      "title": "Intense Weapon Drill",
      "requirements": { 
        "tier": { "min": 3 },  // ← Higher tier required
        "context": ["War"]  // ← Campaign context
      },
      "costs": { "fatigue": 5 },  // ← More expensive
      "effects": { 
        "skillXp": { "OneHanded": 60 },  // ← Much more XP
        "hpChange": -5  // ← Risk of injury
      }
    }
  ]
}
```

### Tier + Context Variant Example

```json
{
  "schemaVersion": 2,
  "events": [
    {
      "id": "evt_patrol",
      "title": "Patrol Duty",
      "requirements": { "tier": { "min": 1, "max": 3 } },
      "setup": "Standard patrol assignment."
    },
    {
      "id": "evt_patrol_veteran",
      "title": "Veteran Patrol",
      "requirements": { 
        "tier": { "min": 4 },
        "context": ["War"]
      },
      "setup": "You lead a patrol in enemy territory."
    },
    {
      "id": "evt_patrol_garrison",
      "title": "Garrison Patrol",
      "requirements": { 
        "tier": { "min": 1, "max": 3 },
        "context": ["Camp", "Town"]
      },
      "setup": "Routine patrol around camp."
    }
  ]
}
```

### Role + Context Variant Example

```json
{
  "schemaVersion": 2,
  "events": [
    {
      "id": "evt_scout_mission",
      "title": "Scout Mission",
      "requirements": { 
        "role": "Scout",
        "tier": { "min": 2 }
      }
    },
    {
      "id": "evt_scout_mission_dangerous",
      "title": "Deep Reconnaissance",
      "requirements": { 
        "role": "Scout",
        "tier": { "min": 4 },
        "context": ["War"]  // ← Only during war
      }
    }
  ]
}
```

### Variant Selection Behavior

**Scenario 1: Garrison (Camp context)**
```
Eligible content:
  dec_rest           ✓ (context: Any)
  dec_rest_garrison  ✓ (context: ["Camp"])
  dec_rest_exhausted ✗ (context: ["Siege", "Battle"])

Orchestrator scores both eligible variants
Selects: dec_rest_garrison (better fit for garrison)
Player sees: Garrison-specific rest option
```

**Scenario 2: Siege Defense (Siege context)**
```
Eligible content:
  dec_rest           ✓ (context: Any)
  dec_rest_garrison  ✗ (context: ["Camp"])
  dec_rest_exhausted ✓ (context: ["Siege", "Battle"])

Orchestrator scores both eligible variants
Selects: dec_rest_exhausted (crisis-appropriate)
Player sees: Exhausted rest option
```

**Scenario 3: Campaign March (War context)**
```
Eligible content:
  dec_rest           ✓ (context: Any)
  dec_rest_garrison  ✗ (context: ["Camp"])
  dec_rest_exhausted ✗ (context: ["Siege", "Battle"])

Only base version eligible
Selects: dec_rest (fallback to base)
Player sees: Normal rest option
```

### Variant Best Practices

**DO:**
- Create base version with `"context": "Any"` as fallback
- Use specific contexts for variants (`["Camp"]`, `["Siege"]`)
- Vary costs/rewards to reflect context appropriately
- Keep variant count reasonable (2-3 per base event)
- Name variants descriptively (`_garrison`, `_exhausted`, `_intense`)

**DON'T:**
- Create variants without a base version (always have fallback)
- Overlap contexts too much (causes selection confusion)
- Make all events have variants (add incrementally)
- Change event logic between variants (keep structure similar)

### Implementation Timeline

**Phase 1-5 (Weeks 1-5):** Orchestrator built with current content (no variants)  
**Phase 6+ (Week 6+):** Add variants incrementally as JSON additions

**Priority for Variants:**
1. High-traffic decisions (training, rest, social)
2. Repetitive events (seen too often in playtesting)
3. Role-specific content (add depth)
4. Tier-specific content (progression feel)

**See:** [Content Orchestrator Plan - Phase 6](content-orchestrator-plan.md#phase-6-content-variants-post-launch-incremental) for variant strategy.

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

### Current Config Structure

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

### Pacing Fields (Current)

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

### How Pacing Works (Current)

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

## Future: Orchestrator Config Structure

**Status:** 📋 Specification - See [Content Orchestrator Plan](../../Features/Content/content-orchestrator-plan.md)

The orchestrator will replace schedule-driven config with world-state driven frequency tables:

### Future Config Structure

```json
{
  "decision_events": {
    "pacing": {
      "max_per_day": 2,
      "max_per_week": 8,
      "min_hours_between": 4,
      "per_event_cooldown_days": 7,
      "per_category_cooldown_days": 1
    },
    "orchestrator": {
      "enabled": true,
      "fitness_threshold": 40,
      "log_decisions": true,
      
      "frequency": {
        "peacetime_garrison": 0.14,      // 1 event per week
        "peacetime_recruiting": 0.35,    // 2.5 per week
        "war_marching": 0.5,             // 3.5 per week
        "war_active_campaign": 0.7,      // 5 per week
        "siege_attacking": 0.57,         // 4 per week
        "siege_defending": 1.0,          // 7 per week
        "lord_captured": 0.07            // 0.5 per week
      },
      
      "dampening": {
        "after_busy_week_multiplier": 0.7,
        "after_quiet_week_multiplier": 1.2,
        "after_battle_cooldown_days": 1.5
      },
      
      "pressure_modifiers": {
        "low_supplies": 0.1,
        "wounded_company": 0.1,
        "high_discipline": 0.15,
        "recent_victory": -0.2,
        "just_paid": -0.15
      }
    }
  }
}
```

### Changes from Current

**Removed Fields:**
- `event_window_min_days`, `event_window_max_days` - Schedule-driven pacing
- `evaluation_hours` - Artificial "event times"
- `allow_quiet_days`, `quiet_day_chance` - Random rolls replaced by world-state determination

**Kept Fields (Safety Limits):**
- `max_per_day`, `max_per_week` - Prevent spam
- `min_hours_between` - Minimum spacing
- `per_event_cooldown_days` - Prevent repetition
- `per_category_cooldown_days` - Prevent category spam

**Added Fields:**
- `orchestrator.frequency` - World situation → realistic event frequency mappings
- `orchestrator.dampening` - Activity-based frequency adjustments
- `orchestrator.pressure_modifiers` - Simulation pressure effects on frequency

### Frequency Table Rationale

| Situation | Events/Week | Daily Chance | Why |
|-----------|-------------|--------------|-----|
| Peacetime Garrison | 1.0 | 14% | Boring is realistic. Garrison duty is routine. |
| War Marching | 3.5 | 50% | Normal campaign tempo. |
| Active Campaign | 5.0 | 70% | High activity during active operations. |
| Siege Defense | 7.0 | 100% | Crisis situation. Something every day. |

**Philosophy:** The world determines realism. Garrison should feel quiet. Sieges should feel chaotic. The simulation doesn't manufacture pacing—it reflects reality.

**Grace Period:** 3-day grace period after enlistment (unchanged)

**See:** [Content Orchestrator Plan](content-orchestrator-plan.md) for complete specification and implementation timeline.

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
| Tooltip | `tooltip` | string | ✅ | Cannot be null. Factual, brief description |
| Costs | `costs` | object | ❌ | Resources deducted |
| Rewards | `rewards` | object | ❌ | Resources gained |
| Effects | `effects` | object | ❌ | State changes |
| Result Text ID | `resultTextId` | string | ❌ | XML key for outcome narrative |
| Result Text | `resultText` | string | ❌ | Outcome narrative (displayed in Recent Activities) |
| Risk | `risk` | string | ❌ | safe/moderate/risky/dangerous |
| Risk Chance | `risk_chance` or `riskChance` | int | ❌ | 0-100, success % |
| Set Flags | `set_flags` or `setFlags` | array | ❌ | Flags to set |
| Clear Flags | `clear_flags` or `clearFlags` | array | ❌ | Flags to clear |
| Chains To | `chains_to` or `chainsTo` | string | ❌ | Follow-up event ID |
| Chain Delay | `chain_delay_hours` or `chainDelayHours` | int | ❌ | Hours before chain fires |

---

## Tooltip Guidelines

**Tooltips cannot be null.** Every option must have a factual, concise, brief description.

**Format:** Action + consequences + restrictions (under 80 characters)

**Examples:**

```json
// Simple actions
{"tooltip": "Trains equipped weapon"}
{"tooltip": "Build stamina and footwork"}

// Stat/reputation changes
{"tooltip": "Harsh welcome. +5 Officer rep. -3 Retinue Loyalty."}
{"tooltip": "Risky move. +10 Courage. -5 Discipline. Injury chance."}

// Consequences
{"tooltip": "Accept discharge. 90-day re-enlistment block applies."}
{"tooltip": "Desert immediately. Criminal record and faction hostility."}

// Requirements
{"tooltip": "Requires Leadership 50+ to attempt."}
{"tooltip": "Greyed out: Company Morale must be below 50"}
```

---

## Result Text & Event Outcomes

**Purpose:** The `resultText` field provides narrative feedback after an event option is chosen.

**Display System:** Event outcomes are **NOT shown as popups** (the outcome popup system was removed as dead code). Instead, they appear in the **Recent Activities** section of the status menu using a queue system.

**What Still Uses Popups:**
- Initial event setup and option selection (unchanged)
- Multi-phase/chain events (each phase shows normally)
- `reward_choices` sub-popups (training focus, etc.) - These still work!
- Only the outcome narrative after choosing goes to Recent Activities

**Queue Behavior:**
- Only ONE event outcome displays at a time
- Each outcome shows for 1 day in Recent Activities
- Multiple events on the same day queue automatically (FIFO)
- Players check their status menu to see what happened

**Writing Guidelines:**
- Write immersive, present-tense narrative
- Show immediate consequences and atmosphere
- Keep it 1-3 sentences (under 200 characters)
- Use sensory details and character reactions
- Supports placeholder variables (e.g., `{PLAYER_NAME}`, `{SERGEANT}`)

**Examples:**

```json
{
  "resultText": "You wait on the bank with the others, watching the pioneers work. They know their business, but the current is strong and the footing treacherous. It will take time."
}

{
  "resultText": "You wade into the freezing water, gasping at the cold. Your boots fill instantly. Someone shoves a rope into your hands and you haul, muscles burning, until something finally gives."
}

{
  "resultText": "The dice tumble. You win the pot. {COMRADE_NAME} curses under his breath and walks away."
}
```

**Risky Options:** Use `failure_resultText` for the failure narrative when `risk` is set:

```json
{
  "resultText": "Your training pays off. The blow glances off harmlessly.",
  "failure_resultText": "You're too slow. Blood runs down your arm. {SERGEANT} calls for the medic."
}
```

**Display Format in Recent Activities:**
```
• Event Title: Result narrative text here
```

**Important:** The `resultText` field is still required even though outcomes don't show as popups. It's displayed in Recent Activities and provides crucial narrative feedback.

**See Also:** [News & Reporting System - Event Outcome Queue](../UI/news-reporting-system.md#event-outcome-queue-system)

---

## Costs Object

Deducted when option is selected. Automatically displayed to player in yellow.

```json
{
  "costs": {
    "gold": 30,
    "fatigue": 2,
    "time_hours": 4
  }
}
// Player sees: "Cost: -30 gold, +2 fatigue, 4 hours"
```

| Field | JSON Names | Type | Notes |
|-------|------------|------|-------|
| Gold | `gold` | int | Deducted from player |
| Fatigue | `fatigue` | int | Added to fatigue |
| Time | `time_hours` or `timeHours` | int | Campaign hours |

---

## Rewards Object

**⚠️ ONLY USED IN SUB-CHOICE OPTIONS (reward_choices).** For main event options, use `effects` instead!

Gained when sub-choice option is selected. Used for positive gains in reward_choices blocks. Automatically displayed to player in cyan.

```json
{
  "reward_choices": {
    "type": "skill_focus",
    "prompt": "What do you focus on?",
    "options": [
      {
        "id": "focus_weapon",
        "text": "Weapon training",
        "rewards": {
          "skillXp": { "OneHanded": 50 }
        }
      }
    ]
  }
}
// Player sees: "+50 OneHanded XP"
```

| Field | JSON Names | Type | Notes |
|-------|------------|------|-------|
| Gold | `gold` | int | Added to player |
| Fatigue Relief | `fatigueRelief` or `fatigue_relief` | int | Reduces fatigue |
| Skill XP | `skillXp` | dict | Skill name → XP amount (SUB-CHOICES ONLY) |
| Dynamic Skill XP | `dynamicSkillXp` | dict | `"equipped_weapon"` or `"weakest_combat"` → XP (SUB-CHOICES ONLY) |

**⚠️ CRITICAL NOTES:** 
- Use `rewards` ONLY in sub-choice options inside `reward_choices` blocks
- For main event options, put `skillXp` in `effects` instead
- `fatigueRelief` always goes in `rewards` (or nowhere if not a sub-choice)

---

## Effects Object

State changes when option is selected. Can be positive or negative.

**⚠️ AUTOMATIC FEEDBACK:** All effects are automatically displayed to the player in the combat log with color coding. You don't need to add any special fields - just define the effects and the player will see them.

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
// Player sees: "+5 Soldier Reputation, -3 Officer Reputation, +2 Lord Reputation, -10 HP, -50 gold, +20 Troop XP"
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
| Skill XP | `skillXp` | dict | - | Skill name → XP (USE THIS for main options!) |
| Dynamic Skill XP | `dynamicSkillXp` or `dynamic_skill_xp` | dict | - | `"equipped_weapon"` or `"weakest_combat"` → XP |
| Trait XP | `traitXp` | dict | - | Trait name → XP |
| Company Needs | `companyNeeds` | dict | - | Need name → delta |
| Apply Wound | `applyWound` or `apply_wound` | string | - | Minor/Serious/Permanent |
| Chain Event | `chainEventId` | string | - | Immediate follow-up |
| Discharge | `triggersDischarge` | string | - | dishonorable/washout/deserter |
| Renown | `renown` | int | delta | Clan renown |

**⚠️ COMMON MISTAKES:**
- Use `hpChange`, not `hp`
- Use `skillXp` in `effects` for main options, NOT in `rewards`
- Use `fatigueRelief` in `rewards` (sub-choices only)
- Use `soldierRep` (camelCase), parser also accepts `soldier_rep`

**✅ PLAYER FEEDBACK:**
- All effects, costs, and rewards are automatically shown to the player in combat log
- Effects = Green messages (e.g., `+5 Soldier Reputation, +25 XP`)
- Costs = Yellow messages (e.g., `Cost: -30 gold, +2 fatigue`)
- Rewards = Cyan messages (e.g., `+50 gold, -3 fatigue`)
- No special fields needed - just define the effects and they'll be displayed

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
          "effects": {
            "skillXp": { "OneHanded": 25, "Athletics": 10 },
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
          "effects": {
            "skillXp": { "OneHanded": 40, "Athletics": 20 },
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
- [ ] **`skillXp` is in `effects` for main options** (NOT in `rewards`)
- [ ] `rewards` only used in sub-choice options (reward_choices)
- [ ] HP changes use `hpChange`, not `hp`
- [ ] All string IDs have matching entries in `enlisted_strings.xml`
- [ ] Skill/trait names match valid values exactly (case-sensitive)
- [ ] Flag-gated events use `triggers.all` with `has_flag:` conditions
- [ ] Pay tension events use `triggers.escalation_requirements.pay_tension_min`
- [ ] Escalation track names match supported values (case variants accepted)

**Localization Status:** All existing events have complete XML localization as of Dec 2025. For new content, run `python tools/events/sync_event_strings.py` to automatically extract and add missing strings.

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

## Severity Field (News Priority System)

**Added:** December 2025 (Baggage Train feature)

### Purpose

The optional `severity` field controls how events appear in the news feed:
1. **Color coding** - Visual importance in GameMenu displays
2. **Display duration** - How long the event stays visible
3. **Priority** - Whether it can be replaced by less important events

### Values

| Severity | Color | Duration | Use For |
|----------|-------|----------|---------|
| `"normal"` | Default (cream) | 6 hours | Routine updates, standard events |
| `"positive"` | Success (green) | 6 hours | Good news, opportunities, achievements |
| `"attention"` | Warning (gold) | 12 hours | Needs attention, warnings, delays |
| `"urgent"` | Alert (red) | 24 hours | Problems, losses, dangers, raids |
| `"critical"` | Alert (red) | 48 hours | Immediate threats, lockdowns, crises |

### Behavior

**Display Duration:**
- Events persist in personal feed for their duration
- Cannot be replaced by lower-severity events
- Automatically expire after duration elapses

**Color Rendering:**
- Maps to existing `EnlistedColors.xml` styles
- Displayed in Camp Hub RECENT ACTIONS section
- Also affects Daily Brief mentions

**Priority Replacement:**
```
Critical (48h) → Cannot be replaced by anything
Urgent (24h)   → Can only be replaced by Critical or another Urgent
Attention (12h)→ Can be replaced by Urgent or Critical
Normal/Positive (6h) → Can be replaced by anything higher
```

### Examples

**Baggage Train Events:**
```json
{
  "id": "evt_baggage_raided",
  "severity": "urgent",
  "title": "Raiders Hit the Baggage Train",
  ...
}
```

**Training Events:**
```json
{
  "id": "evt_training_success",
  "severity": "positive",
  "title": "Training Session Complete",
  ...
}
```

**Default (No Severity):**
```json
{
  "id": "evt_routine_patrol",
  "title": "Routine Patrol",
  // No severity field = defaults to "normal"
  ...
}
```

### Implementation Notes

- **Optional field** - Existing events without severity default to `"normal"`
- **Backward compatible** - No changes needed to existing event files
- **Case insensitive** - Parser accepts `"Normal"`, `"NORMAL"`, `"normal"`
- **Invalid values** - Unknown severities default to `"normal"` with warning log

---

**End of Document**
