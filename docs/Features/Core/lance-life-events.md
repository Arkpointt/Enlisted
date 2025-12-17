# Lance Life Events

Data-driven event system for camp life, training, pay tension, and narrative moments during enlisted service.

## Overview

Enlisted uses a unified event delivery pipeline supporting:
- **Automatic events** - Evaluated on tick, queued, delivered at safe moments
- **Player-initiated events** - Menu options in Camp Activities

### Delivery Channels

| Channel | Description |
|---------|-------------|
| `menu` | Player-initiated via Camp Activities menu |
| `inquiry` | Automatic popup (`InquiryData` / `MultiSelectionInquiryData`) |
| `incident` | Native moment events via `MapState.NextIncident` |

### Design Principles

- **Content is data** - Events defined in JSON, text in XML
- **Safe persistence** - Only primitives via `IDataStore.SyncData`
- **Feature flags** - Each subsystem can be disabled independently

---

## Event Packs

| Pack | Purpose | Trigger |
|------|---------|---------|
| `events_general.json` | Camp life, social, random encounters | Various |
| `events_onboarding.json` | New recruit orientation chain | Stage progression |
| `events_training.json` | Formation training activities | Player-initiated |
| `events_pay_tension.json` | Pay complaints, theft, confrontation | Post-battle (LeavingBattle) |
| `events_pay_loyal.json` | Help the lord missions | Player-initiated |
| `events_pay_mutiny.json` | Desertion/mutiny chains | Flag-triggered |

---

## Event Structure

Each event has:

| Field | Purpose |
|-------|---------|
| `id` | Unique identifier |
| `category` | Grouping (pay, training, camp, etc.) |
| `delivery` | Method, channel, trigger |
| `triggers` | Conditions (all/any, time of day, escalation requirements) |
| `requirements` | Tier, duty, formation constraints |
| `timing` | Cooldown, priority, one-time flag |
| `content` | Title, setup text, options with outcomes |

### Option Structure

| Field | Purpose |
|-------|---------|
| `id` | Option identifier |
| `text` | Button text |
| `tooltip` | Hover description |
| `condition` | Optional condition (e.g., "tier >= 5") |
| `risk` | "safe" or "risky" |
| `risk_chance` | Failure probability (0.0 - 1.0) |
| `costs` | Fatigue, gold, time |
| `rewards` | XP, gold, items |
| `effects` | Heat, discipline, lance reputation |
| `outcome` | Success result text |
| `outcome_failure` | Failure result text (risky options) |
| `flags_set` | Flags to set on selection |
| `flags_clear` | Flags to clear on selection |

---

## Trigger System

### Trigger Tokens

| Token | Meaning |
|-------|---------|
| `is_enlisted` | Player is currently enlisted |
| `LeavingBattle` | Just finished a battle |
| `WaitingInSettlement` | In town/village wait menu |
| `LeavingEncounter` | Conversation just ended |
| `Dawn`, `Day`, `Dusk`, `Night` | Time of day |
| `PayTensionHigh` | Pay tension ≥ 40 |
| `HeatHigh` | Heat escalation high |

### Escalation Requirements

Events can require escalation values:

```json
"escalation_requirements": {
  "pay_tension_min": 40,
  "pay_tension_max": 80,
  "heat_min": 0,
  "heat_max": 100
}
```

---

## Delivery Flow

### Automatic Events

```
Hourly/Daily Tick
    ↓
Evaluate triggers + requirements
    ↓
Check ai_safe (no battle/encounter/prisoner)
    ↓
Queue eligible event
    ↓
Show inquiry popup when safe
```

**Priority Order:**
1. Onboarding (until complete)
2. Escalation threshold events
3. Duty events
4. Pay tension events
5. General events

### Player-Initiated Events

```
Camp Activities Menu
    ↓
Check availability (cooldown, fatigue, formation)
    ↓
Player clicks option
    ↓
Show inquiry popup immediately
    ↓
Apply effects, return to menu
```

---

## Onboarding

New recruits go through a 3-stage onboarding:

| Stage | Event | Purpose |
|-------|-------|---------|
| 1 | First night in camp | Lance introduction |
| 2 | Finding your place | Choose formation (T1->T2) |
| 3 | Meeting the sergeant | Duty orientation |

### Track Selection

| Tier | Track |
|------|-------|
| T1-T4 | Enlisted |
| T5-T6 | Officer |
| T7-T9 | Commander |

### Variant Selection

| Variant | Condition |
|---------|-----------|
| `first_time` | Never enlisted before |
| `transfer` | Different lord than last time |
| `return` | Same lord as before |

---

## Effects

When an option is selected, effects are applied:

| Effect | Application |
|--------|-------------|
| `xp` | Skill XP via `Hero.AddSkillXp()` |
| `gold` | Via `GiveGoldAction` |
| `fatigue` | Deduct from daily capacity |
| `heat` | Add to escalation track |
| `discipline` | Add to escalation track |
| `lance_reputation` | Modify internal reputation |
| `relation` | Modify lord relation |
| `injury_risk` | Roll for injury |
| `illness_risk` | Roll for illness |

---

## Persistence

All state persists via `IDataStore.SyncData`:

| State | Purpose |
|-------|---------|
| `_cooldowns` | Per-event last-fired timestamps |
| `_oneTimeFired` | Events that can only fire once |
| `_onboardingStage` | Current onboarding stage |
| `_onboardingTrack` | Enlisted/officer/commander |
| `_onboardingVariant` | first_time/transfer/return |

---

## Configuration

### Feature Flags

In `enlisted_config.json`:

```json
{
  "lance_life_events": {
    "enabled": true,
    "automatic": { "enabled": true },
    "player_initiated": { "enabled": true },
    "onboarding": { "enabled": true, "skip_for_veterans": false }
  }
}
```

### Disabling Events

- Set `enabled: false` to disable the entire system
- Use category flags to disable specific event types
- Events with unmet triggers simply don't fire

---

## File Locations

| Path | Purpose |
|------|---------|
| `ModuleData/Enlisted/Events/*.json` | Event pack definitions |
| `ModuleData/Languages/enlisted_strings.xml` | Localized text |
| `src/Features/Lances/Events/` | Event engine code |
| `src/Features/Lances/Events/LanceLifeEventCatalog.cs` | Data models |
| `src/Features/Lances/Events/LanceLifeEventTriggerEvaluator.cs` | Trigger logic |
| `src/Features/Lances/Events/LanceLifeEventsIncidentBehavior.cs` | Incident delivery |
| `src/Features/Lances/Events/LanceLifeEventInquiryPresenter.cs` | Inquiry popups |

---

## API Reference

### LanceLifeEventCatalogLoader

```csharp
// Load all event packs
var catalog = LanceLifeEventCatalogLoader.LoadCatalog();

// Access events
foreach (var evt in catalog.Events)
{
    // Process event
}
```

### LanceLifeEventTriggerEvaluator

```csharp
// Check if event can fire
bool canFire = evaluator.Evaluate(eventDefinition, enlistment);
```

### LanceLifeEventEffectsApplier

```csharp
// Apply selected option effects
LanceLifeEventEffectsApplier.Apply(eventDef, selectedOption, enlistment);
```

