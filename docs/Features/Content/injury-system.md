# Injury System

**Summary:** The injury system applies percentage-based HP loss with narrative descriptions when players are wounded during orders, events, or combat. Injuries range from minor (sprained ankle, 15% HP) to severe (gut wound, 50% HP), with contextual narratives and recovery time tracking. Integrates with the order system and event outcomes to provide immersive, consequence-driven gameplay.

**Status:** ✅ Implemented  
**Last Updated:** 2025-12-31  
**Implementation:** `src/Features/Content/InjurySystem.cs`, `ModuleData/Enlisted/Content/injuries.json`  
**Related Docs:** [Order Progression System](../../AFEATURE/order-progression-system.md), [Event System Schemas](event-system-schemas.md)

---

## Index

1. [Overview](#overview)
2. [Design Philosophy](#design-philosophy)
3. [Injury Definitions](#injury-definitions)
4. [Severity Levels](#severity-levels)
5. [HP Calculation](#hp-calculation)
6. [Integration Points](#integration-points)
7. [Narrative System](#narrative-system)
8. [JSON Schema](#json-schema)
9. [API Reference](#api-reference)
10. [Implementation Notes](#implementation-notes)

---

## Overview

The injury system replaces flat HP loss with contextual, narrative-driven injuries. Instead of generic "-25 HP" damage, players experience "Your ankle rolls on uneven ground. It swells before you can unlace your boot." with appropriate HP percentage loss.

### Core Principles

- **Percentage-based damage** - Injuries scale with player HP (sprained ankle is 15%, not flat 20 HP)
- **Narrative immersion** - Every injury has multiple narrative variations
- **Severity appropriate** - Minor injuries (bruises) do less damage than severe (gut wounds)
- **Context-aware** - Used by orders, events, and combat outcomes
- **Bannerlord RP flavor** - Medieval military language, not generic RPG text

### Key Features

- 15 injury types from minor to severe
- Multiple narrative variations per injury (2-4 options)
- Severity levels: Minor, Moderate, Serious, Severe, Critical
- Recovery day tracking for future medical system integration
- Brief narratives for UI display
- Random selection by severity for contextual application

---

## Design Philosophy

### Why Percentage-Based?

**Problem with flat HP loss:**
- Doesn't scale with player level/HP
- Sprained ankle shouldn't do same damage as gut wound
- No narrative context for what happened

**Solution with injury system:**
- Sprained ankle = 15% HP (minor, recoverable)
- Broken rib = 25% HP (moderate, painful)
- Gut wound = 50% HP (severe, life-threatening)
- Scales naturally with player HP growth

### Narrative First

Injuries are experienced, not just numbers:

```
❌ Before: "You took 25 HP damage from order outcome"
✅ After: "Steel finds your belly. Blood seeps between your fingers."
          [Player loses 50% HP based on gut_wound injury]
```

Players understand what happened and why they're hurt.

---

## Injury Definitions

### Complete Injury Catalog

All injuries defined in `ModuleData/Enlisted/Content/injuries.json`:

| Injury ID | Display Name | HP % | Recovery Days | Severity |
|-----------|--------------|------|---------------|----------|
| `sprained_ankle` | Sprained Ankle | 15% | 3 | Minor |
| `bruised_ribs` | Bruised Ribs | 18% | 4 | Minor |
| `minor_cut` | Minor Cut | 10% | 2 | Minor |
| `twisted_knee` | Twisted Knee | 20% | 5 | Moderate |
| `broken_finger` | Broken Finger | 12% | 7 | Moderate |
| `deep_gash` | Deep Gash | 30% | 8 | Moderate |
| `broken_rib` | Broken Rib | 25% | 10 | Serious |
| `concussion` | Concussion | 28% | 7 | Serious |
| `broken_arm` | Broken Arm | 35% | 14 | Serious |
| `puncture_wound` | Puncture Wound | 40% | 12 | Severe |
| `gut_wound` | Gut Wound | 50% | 10 | Severe |
| `broken_leg` | Broken Leg | 45% | 21 | Severe |
| `crushed_hand` | Crushed Hand | 38% | 18 | Severe |
| `head_trauma` | Head Trauma | 48% | 14 | Critical |
| `near_death` | Near Death | 55% | 30 | Critical |

---

## Severity Levels

Injuries are categorized by severity for contextual selection:

### Minor (10-20% HP)
**Use when:** Training accidents, minor mishaps, low-danger situations
- Sprained Ankle (15%)
- Bruised Ribs (18%)
- Minor Cut (10%)

**Example Usage:**
```json
"outcome": {
  "injury_type": "sprained_ankle",
  "reason": "training_accident"
}
```

### Moderate (20-30% HP)
**Use when:** Failed skill checks, moderate danger, physical labor
- Twisted Knee (20%)
- Deep Gash (30%)
- Broken Finger (12%)

### Serious (25-35% HP)
**Use when:** Combat-adjacent danger, falls, significant failures
- Broken Rib (25%)
- Concussion (28%)
- Broken Arm (35%)

### Severe (35-50% HP)
**Use when:** Direct combat, critical failures, life-threatening situations
- Puncture Wound (40%)
- Gut Wound (50%)
- Broken Leg (45%)
- Crushed Hand (38%)

### Critical (45-55% HP)
**Use when:** Near-death experiences, catastrophic failures
- Head Trauma (48%)
- Near Death (55%)

---

## HP Calculation

### Percentage-Based Formula

```csharp
// Load injury definition
var injuryDef = GetInjuryDefinition(injuryId);

// Calculate damage as percentage of max HP
var maxHp = Hero.MainHero.CharacterObject.MaxHitPoints();
var hpLoss = (int)(maxHp * injuryDef.HpPercentage);

// Apply damage (minimum 1 HP, won't kill player)
var currentHp = Hero.MainHero.HitPoints;
var newHp = Math.Max(1, currentHp - hpLoss);
Hero.MainHero.HitPoints = newHp;
```

### Example Calculation

**Player with 100 max HP:**
- Sprained Ankle (15%) → 15 HP loss
- Gut Wound (50%) → 50 HP loss

**Player with 200 max HP (higher level):**
- Sprained Ankle (15%) → 30 HP loss
- Gut Wound (50%) → 100 HP loss

The injury severity scales naturally with character progression.

---

## Integration Points

### Order Outcomes

Order outcomes can specify `injury_type` instead of `hp_loss`:

```json:ModuleData/Enlisted/Orders/orders_t4_t6.json
{
  "id": "order_scout_route",
  "consequences": {
    "critical_failure": {
      "reputation": { "officer": -15, "lord": -10 },
      "injury_type": "puncture_wound",
      "text": "The ambush caught you. An arrow finds your side. You barely escaped."
    }
  }
}
```

**Code path:**
```csharp
OrderManager.ApplyOrderOutcomeEffects()
  → Checks outcome.InjuryType first
  → InjurySystem.ApplyInjury(injuryType, reason)
  → Displays narrative + applies HP loss
```

### Event Outcomes

Events can trigger injuries through outcome effects:

```json:ModuleData/Enlisted/Events/order_events_scout.json
{
  "id": "order_evt_scout_ambush",
  "options": [
    {
      "id": "fight_back",
      "outcome": {
        "injury_type": "deep_gash",
        "reputation": { "soldier": 5 },
        "text": "You stand and fight. Steel clashes. You take a blade to the arm but hold your ground."
      }
    }
  ]
}
```

### Direct Code Application

Systems can apply injuries programmatically:

```csharp
// Apply specific injury
InjurySystem.ApplyInjury("broken_rib", "combat_incident");

// Apply random injury by severity
var injury = InjurySystem.GetRandomInjury(InjurySeverity.Moderate);
InjurySystem.ApplyInjury(injury.Id, "failed_skill_check");

// Get brief narrative for UI
var briefText = InjurySystem.GetBriefNarrative("sprained_ankle");
// Returns: "Sprained ankle"
```

---

## Narrative System

### Full Narratives

Each injury has 2-4 narrative variations for immersion:

**Sprained Ankle:**
- "Your ankle rolls on uneven ground. It swells before you can unlace your boot."
- "A misstep on the rocky path. Your ankle protests with a sharp pain."

**Gut Wound:**
- "Steel finds your belly. Blood seeps between your fingers."
- "A thrust to the gut. You gasp, the world blurring."

### Brief Narratives

Short forms for UI display and recap feeds:

| Injury | Brief Narrative |
|--------|-----------------|
| Sprained Ankle | "Sprained ankle" |
| Gut Wound | "Gut wound" |
| Broken Rib | "Broken rib" |
| Deep Gash | "Deep gash" |

Used in:
- Order recap feeds
- Recent Activities display
- Daily Brief summaries

---

## JSON Schema

### Injury Definition Structure

```json
{
  "id": "injury_identifier",
  "displayName": "Human-Readable Name",
  "hpPercentage": 0.15,
  "narratives": [
    "First narrative variation...",
    "Second narrative variation...",
    "Third narrative variation (optional)..."
  ],
  "briefNarrative": "Short form for UI",
  "recoveryDays": 7,
  "severity": "Minor | Moderate | Serious | Severe | Critical"
}
```

### Full Example

```json
{
  "id": "broken_rib",
  "displayName": "Broken Rib",
  "hpPercentage": 0.25,
  "narratives": [
    "Something cracks in your chest. Each breath is a knife.",
    "The impact drives air from your lungs. Breathing becomes agony.",
    "A rib snaps like kindling. You taste copper."
  ],
  "briefNarrative": "Broken rib",
  "recoveryDays": 10,
  "severity": "Serious"
}
```

### Severity Enum

```csharp
public enum InjurySeverity
{
    Minor,      // 10-20% HP
    Moderate,   // 20-30% HP
    Serious,    // 25-35% HP
    Severe,     // 35-50% HP
    Critical    // 45-55% HP
}
```

**Note:** Uses existing enum from `PlayerConditionModels.cs` for consistency.

---

## API Reference

### InjurySystem (Static Class)

#### Initialize()
Loads injury definitions from `injuries.json` at game start.

```csharp
// Called from SubModule.OnGameStart()
InjurySystem.Initialize();
```

#### ApplyInjury(string injuryId, string reason)
Applies a specific injury to the player with full narrative.

```csharp
InjurySystem.ApplyInjury("sprained_ankle", "training_accident");
// Displays: "Your ankle rolls on uneven ground..."
// Applies: 15% HP loss
// Logs: Player injured - Sprained Ankle (training_accident): 100 -> 85 HP
```

**Parameters:**
- `injuryId` - Injury identifier from `injuries.json`
- `reason` - Context for logging (e.g., "order_outcome", "combat_incident")

#### GetRandomInjury(InjurySeverity severity)
Returns a random injury definition of the specified severity.

```csharp
var injury = InjurySystem.GetRandomInjury(InjurySeverity.Moderate);
InjurySystem.ApplyInjury(injury.Id, "contextual_danger");
```

#### GetBriefNarrative(string injuryId)
Returns the short narrative form for UI display.

```csharp
var brief = InjurySystem.GetBriefNarrative("gut_wound");
// Returns: "Gut wound"
```

---

## Implementation Notes

### Initialization

Injury definitions are loaded once at game start:

```csharp
// src/Mod.Entry/SubModule.cs
protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
{
    if (game.GameType is Campaign)
    {
        // ... other initialization
        EventCatalog.Initialize();
        InjurySystem.Initialize(); // Load injuries.json
    }
}
```

### File Location

**Definition File:**
```
ModuleData/Enlisted/Content/injuries.json
```

**Implementation:**
```
src/Features/Content/InjurySystem.cs
```

### Error Handling

- Missing `injuries.json` → Warning logged, system disabled
- Invalid injury ID → Warning logged, no injury applied
- JSON parse errors → Error logged with details
- HP calculation always leaves minimum 1 HP (won't kill player)

### Save Compatibility

Injury system is stateless (no save data). Only immediate HP changes are persisted through Bannerlord's native save system.

**Future Enhancement:** Recovery tracking would require save definer registration.

### Performance

- Definitions loaded once at startup (not per-injury)
- Dictionary lookup for O(1) injury retrieval
- Minimal overhead (single calculation, one logging call)

---

## Usage Examples

### In Order Outcomes

```json
{
  "critical_failure": {
    "reputation": { "officer": -15 },
    "company_needs": { "Readiness": -15 },
    "injury_type": "broken_leg",
    "text": "The fall from the watchtower breaks your leg. You'll be off duty for weeks."
  }
}
```

### In Event Options

```json
{
  "id": "climb_wall",
  "text": "Scale the wall yourself",
  "skill_check": { "Athletics": 60 },
  "failure": {
    "injury_type": "broken_arm",
    "text": "Your grip fails. You fall hard. Your arm bends the wrong way."
  }
}
```

### In Code

```csharp
// Combat consequence
if (playerTookDangerousRisk)
{
    var injury = InjurySystem.GetRandomInjury(InjurySeverity.Severe);
    InjurySystem.ApplyInjury(injury.Id, "reckless_combat");
}

// Training accident
if (trainingFailed && MBRandom.RandomFloat < 0.1f)
{
    InjurySystem.ApplyInjury("sprained_ankle", "training_mishap");
}
```

---

## Acceptance Criteria

- [x] 15 injury types defined with appropriate HP percentages
- [x] Multiple narrative variations per injury
- [x] Severity levels (Minor, Moderate, Serious, Severe, Critical)
- [x] Integration with OrderManager (prioritizes injury_type over hp_loss)
- [x] Random injury selection by severity
- [x] Brief narratives for UI display
- [x] Proper logging with context
- [x] Initialization at game start
- [x] Error handling for missing/invalid data
- [x] HP calculation scales with character level
- [x] Bannerlord RP flavor throughout
- [x] Save compatibility (stateless design)
