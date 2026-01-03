# Camp Fatigue System

**Summary:** Fatigue is a stamina-like resource (0-24 points) that gates intensive enlisted actions, creating meaningful choices about how you spend your time and energy. Players consume fatigue through training, orders, and intensive decisions, then restore it through rest. Fatigue is always visible in the UI and integrates with probation penalties, health systems, and strategic context.

**Status:** ✅ Current  
**Last Updated:** 2025-12-23  
**Related Docs:** [Core Gameplay](core-gameplay.md), [Camp Life Simulation](../Campaign/camp-life-simulation.md)

---

## Index

- [Overview](#overview)
- [Design Philosophy](#design-philosophy)
- [Data & State](#data--state)
- [Fatigue Consumption](#fatigue-consumption)
- [Fatigue Restoration](#fatigue-restoration)
- [Probation Effects](#probation-effects)
- [Health Integration](#health-integration)
- [Strategic Context Effects](#strategic-context-effects)
- [Player Experience](#player-experience)
- [API Reference](#api-reference)
- [Configuration](#configuration)
- [Technical Implementation](#technical-implementation)

---

## Overview

Fatigue is a stamina budget that prevents players from executing all camp activities at once. It creates tension between different goals (training vs rest, risk vs recovery) and forces prioritization of actions.

**Core Concept:** You can't do everything in a day. Training exhausts you, orders drain you, intensive activities cost energy. Rest is not optional—it's a resource you must manage.

**Visibility:** Always displayed in Enlisted Status menu as "Fatigue: 14/24" so players can plan their actions.

**Integration Points:**
- Camp decisions (training, social, economic)
- Orders system (mission intensity)
- Pay Muster options (corruption challenges, side deals)
- Event options (intensive choices)
- Probation system (reduced capacity)
- Medical system (health penalties at critical levels)

---

## Design Philosophy

### Why Fatigue Exists

**Problem Solved:** Without fatigue, players would spam all training decisions every day, trivializing skill progression and removing meaningful choice.

**Solution:** Limited stamina budget forces players to:
1. **Prioritize:** Choose between training, socializing, economic work, or rest
2. **Plan ahead:** Save fatigue for important orders or opportunities
3. **Manage risk:** Low fatigue = vulnerability to health issues
4. **Feel consequences:** Probation, intensive campaigns, poor rest all reduce capacity

**Design Goals:**
- Make rest decisions meaningful (not just time wasters)
- Create trade-offs between different camp activities
- Reflect physical limitations of a soldier's day
- Integrate with health system (exhaustion has consequences)
- Respond to campaign intensity (Grand Campaign = exhausting)

### Player Strategy

**High Fatigue (18-24):** Take intensive actions (training, orders, challenges)  
**Medium Fatigue (10-17):** Standard activities, moderate risk  
**Low Fatigue (5-9):** Avoid intensive actions, consider rest soon  
**Critical Fatigue (0-4):** Rest immediately, health penalty risk

---

## Data & State

**Storage Location:** `EnlistmentBehavior`  
**Fields:**
- `FatigueCurrent`: Current stamina (0 to FatigueMax)
- `FatigueMax`: Maximum capacity (24 default, 18 during probation)

**Defaults:**
- New enlistments: 24/24 (fully rested)
- After probation activation: Current clamped to new max
- After probation ends: Max restored to 24

**Persistence:**
- Serialized in `EnlistmentBehavior.SyncData()`
- Validated on load (clamped to valid ranges)
- Survives save/load cycles

**Validation Rules:**
- `FatigueMax` must be > 0
- `FatigueCurrent` must be <= `FatigueMax`
- Both clamped on load to prevent invalid states

---

## Fatigue Consumption

Fatigue is consumed by intensive actions. Attempts to consume fatigue when current = 0 fail gracefully.

### Camp Decisions (Cost by Category)

**Training (Heavy Cost):**
- `dec_weapon_drill`: 3 fatigue
- `dec_spar`: 4 fatigue
- `dec_endurance`: 5 fatigue
- `dec_study_tactics`: 2 fatigue
- `dec_combat_drill`: 5 fatigue
- `dec_weapon_specialization`: 6 fatigue
- `dec_lead_drill`: 4 fatigue

**Social (Light Cost):**
- `dec_join_men`: 1 fatigue
- `dec_join_drinking`: 2 fatigue
- `dec_seek_officers`: 1 fatigue
- `dec_confront_rival`: 3 fatigue (stressful)

**Economic (Moderate Cost):**
- `dec_side_work`: 3 fatigue
- `dec_gamble_high`: 2 fatigue (mentally taxing)
- `dec_shady_deal`: 2 fatigue

**Risk-Taking (High Cost):**
- `dec_dangerous_wager`: 4 fatigue
- `dec_prove_courage`: 5 fatigue
- `dec_challenge`: 6 fatigue

**Information (Light Cost):**
- `dec_scout_area`: 3 fatigue
- `dec_listen_rumors`: 1 fatigue

**Equipment:**
- `dec_maintain_gear`: 2 fatigue

### Orders System

**Standard Orders:** 3-5 fatigue  
**Intensive Orders:** 6-10 fatigue  
**Strategic Context Modifier:**
- Grand Campaign: +2 fatigue cost
- Last Stand: +2 fatigue cost
- Siege Works: +1 fatigue cost
- Winter Camp: -1 fatigue cost (training focus)

**Examples:**
- Standard patrol: 3 fatigue
- Forced march: 8 fatigue
- Combat mission: 7 fatigue
- Reconnaissance (Scout role): 5 fatigue

### Pay Muster Options

**Corruption Challenge:** 5 fatigue (confronting officers is exhausting)  
**Side Deal:** 3 fatigue (negotiating takes energy)  
**Normal acceptance:** 0 fatigue

### Event Options

Events can have fatigue costs on individual options:

**Example from camp gambling event:**
- Play (intensive): 2 fatigue
- Watch (passive): 0 fatigue
- Report (stressful): 1 fatigue

**Typical Range:** 0-4 fatigue per event option

### Consumption Mechanics

**Attempt to Consume:**
```csharp
bool success = EnlistmentBehavior.Instance.TryConsumeFatigue(amount, "weapon_drill");
```

**Success Conditions:**
- Player is enlisted
- Amount > 0
- Current fatigue >= amount

**On Success:**
- `FatigueCurrent` reduced by amount
- Session log entry created
- Health penalty check triggered
- Returns `true`

**On Failure:**
- No change to current fatigue
- Returns `false`
- Caller should handle gracefully (disable option, show tooltip)

---

## Fatigue Restoration

### Primary Method: Rest Decisions

**dec_rest (Quick Rest):**
- Restores: 2-5 fatigue (varies by option)
- Cooldown: 1 day
- Time cost: Minimal (few hours)
- Options:
  - Short rest: +2 fatigue
  - Rest with men: +1 fatigue, +1 Soldier Rep

**dec_rest_extended (Full Sleep):**
- Restores: 5 fatigue
- Cooldown: 3 days
- Time cost: 8 hours
- Additional benefit: +10 HP
- Best for critical exhaustion recovery

### Secondary Method: Event Rewards

Some events grant `fatigueRelief` as a reward:

**Example structures:**
```json
"rewards": {
  "fatigueRelief": 2
}
```

**Implementation:**
- Adds to Rest company need (not fatigue directly)
- Represents refreshing experiences (good meal, entertainment, rest)
- Typical range: 1-3 points

**Camp events that restore fatigue:**
- Social gatherings (relaxing outcome)
- Light entertainment
- Successful low-stress activities

### Natural Recovery

**Currently Not Implemented:** Fatigue does not restore automatically over time.

**Design Rationale:**
- Forces intentional rest decisions
- Makes rest management a deliberate choice
- Prevents passive recovery from trivializing system

**Future Consideration:** Strategic context could enable slow natural recovery during peaceful periods (1-2 per day during Winter Camp).

### Restoration API

**Restore by Amount:**
```csharp
EnlistmentBehavior.Instance.RestoreFatigue(5, "extended_rest");
```

**Restore to Full:**
```csharp
EnlistmentBehavior.Instance.RestoreFatigue(0, "full_recovery"); // amount=0 means full
```

**Behavior:**
- Clamped to `FatigueMax` (respects probation cap)
- Session log entry created
- Returns void (always succeeds)

---

## Probation Effects

Probation reduces your maximum fatigue capacity, representing restricted freedom and additional stress.

**Normal State:**
- `FatigueMax = 24`
- Full range of activities available

**Probation Active:**
- `FatigueMax = 18` (configurable via `ProbationFatigueCap`)
- `FatigueCurrent` clamped to new max
- ~25% capacity reduction
- Fewer intensive actions available per day

**Activation:**
```csharp
_fatigueMax = Math.Min(_fatigueMax, config.ProbationFatigueCap); // 18
_fatigueCurrent = Math.Min(_fatigueCurrent, _fatigueMax);
```

**When Probation Ends:**
- `FatigueMax` restored to 24
- `FatigueCurrent` unchanged (if was 12/18, becomes 12/24)
- Full capacity available again

**Gameplay Impact:**
- Can't train as frequently during probation
- Must choose between activities more carefully
- Incentivizes completing probation requirements quickly
- Represents supervision and limited autonomy

**Configuration:**
```json
"retirement": {
  "ProbationDays": 12,
  "ProbationFatigueCap": 18
}
```

---

## Health Integration

When fatigue drops to critical levels, health penalties can trigger.

### Health Penalty Check

**Trigger:** Every time fatigue is consumed via `TryConsumeFatigue()`

**Method:** `CheckFatigueHealthPenalty()` (private)

**Thresholds (Estimated):**
- Critical: Fatigue < 5 (20% of capacity)
- Severe: Fatigue < 3 (12% of capacity)

**Possible Consequences:**
- Injury conditions applied
- Illness risk increased
- HP damage
- Medical Risk escalation (+1 to +2)

**Implementation Detail:**
The exact penalty logic is in `EnlistmentBehavior` and integrates with the Medical Conditions system.

**Player Feedback:**
- "You feel dangerously exhausted" (low fatigue tooltip)
- Medical tent option appears if condition develops
- Daily Brief warns about exhaustion risk

**Prevention:**
- Rest before fatigue hits critical levels
- Avoid intensive activities when low (<5)
- Prioritize extended rest if frequently exhausted

---

## Strategic Context Effects

Strategic context affects fatigue costs and recovery, reflecting campaign intensity.

### High-Tempo Operations (Increased Costs)

**Grand Campaign (Coordinated Offensive):**
- Order fatigue costs: +2
- Training fatigue costs: +1
- Represents sustained high-intensity operations

**Last Stand (Desperate Defense):**
- Order fatigue costs: +2
- Social activities: +1 (stress is high)
- Represents crisis conditions

**Siege Works (Active Siege):**
- Order fatigue costs: +1
- Equipment maintenance: +1 (intensive labor)
- Represents grueling siege conditions

### Low-Tempo Operations (Reduced Costs)

**Winter Camp (Seasonal Rest):**
- Training fatigue costs: -1 (training focus, not combat)
- Rest recovery: +1 bonus
- Represents training season with regular sleep

**Garrison Duty (Settlement Defense):**
- Order fatigue costs: -1 (routine duties)
- Rest recovery: +1 bonus
- Represents stable conditions

**Riding the Marches (Peacetime Patrol):**
- Standard costs (no modifier)
- Moderate pace operations

### Recovery Rate Modifiers

**Future Enhancement:** Strategic context could affect natural recovery rate:

- Winter Camp: +2 fatigue per day (natural recovery)
- Garrison Duty: +1 fatigue per day
- Grand Campaign: No natural recovery
- Last Stand: No natural recovery (too intense)

**Current State:** Not yet implemented (all recovery via explicit rest decisions).

---

## Player Experience

### Typical Fatigue Lifecycle

**Day 1 (Fresh, 24/24 Fatigue):**
- Morning: Accept order (costs 5 fatigue → 19/24)
- Afternoon: Weapon drill decision (costs 3 fatigue → 16/24)
- Evening: Social activity (costs 1 fatigue → 15/24)

**Day 2 (Moderate, 15/24 Fatigue):**
- Morning: Standard activities available
- Afternoon: Economic work (costs 3 fatigue → 12/24)
- Evening: Light social or rest

**Day 3 (Tired, 12/24 Fatigue):**
- Morning: Order arrives, declines (saves fatigue)
- Afternoon: Quick rest decision (+2 fatigue → 14/24)
- Evening: Light activities only

**Day 4 (Recovered, 14/24 Fatigue):**
- Back to moderate activity pace
- Can train or accept orders

### Decision-Making Scenarios

**Scenario: Low Fatigue Before Important Order**

Player has 4 fatigue remaining, expects strategic order tomorrow.

**Options:**
1. Rest now (extended rest: +5 fatigue), be ready tomorrow
2. Risk low fatigue, might have to decline order
3. Do light activities (1-2 fatigue cost), rest tomorrow

**Smart Play:** Rest now. Orders are valuable for progression.

**Scenario: Probation with Limited Capacity**

Player on probation (18 max), currently at 10/18 fatigue.

**Options:**
1. Train (costs 5 fatigue → 5/18 remaining)
2. Rest (restores 5 fatigue → 15/18)
3. Social (costs 1 fatigue → 9/18)

**Smart Play:** Prioritize activities that reduce probation duration (completing requirements), rest frequently.

**Scenario: Critical Exhaustion (2/24 Fatigue)**

Player ignored fatigue management, now dangerously low.

**Immediate Action Required:**
1. Use `dec_rest_extended` (priority)
2. Avoid all intensive activities
3. Decline orders if necessary
4. Check for medical conditions

**Consequences if ignored:**
- Health penalties likely
- Injury condition risk
- Medical Risk escalation
- Combat effectiveness reduced

### UI Indicators

**Enlisted Status Menu Display:**
```
Fatigue: 14/24
```

**Tooltip Hints:**
- "Requires 5 fatigue" (decision unavailable)
- "You're too exhausted for this" (0 fatigue)
- "Rest recommended" (fatigue < 5)

**Decision Availability:**
- Grayed out options when fatigue insufficient
- Tooltip shows required amount
- Clear feedback about why unavailable

---

## API Reference

### Consumption

**Method:** `TryConsumeFatigue(int amount, string reason = null)`  
**Returns:** `bool` (true if consumed, false if insufficient)

**Parameters:**
- `amount`: Fatigue points to consume (must be > 0)
- `reason`: Optional log context (e.g., "weapon_drill", "order_patrol")

**Usage Example:**
```csharp
var enlistment = EnlistmentBehavior.Instance;
if (enlistment.TryConsumeFatigue(5, "combat_drill"))
{
    // Execute activity
    ApplyTrainingEffects();
}
else
{
    // Show "insufficient fatigue" message
    InformationManager.DisplayMessage("Too exhausted for this activity.");
}
```

**Behavior:**
- Returns false immediately if not enlisted
- Returns false if amount <= 0
- Reduces `FatigueCurrent` by amount
- Triggers `CheckFatigueHealthPenalty()`
- Logs to session diagnostics

### Restoration

**Method:** `RestoreFatigue(int amount = 0, string reason = null)`  
**Returns:** `void` (always succeeds)

**Parameters:**
- `amount`: Fatigue points to restore (0 = restore to full)
- `reason`: Optional log context

**Usage Example:**
```csharp
// Restore specific amount
EnlistmentBehavior.Instance.RestoreFatigue(5, "rest_decision");

// Restore to full
EnlistmentBehavior.Instance.RestoreFatigue(0, "full_recovery");
```

**Behavior:**
- Adds amount to `FatigueCurrent`
- If amount = 0, sets to `FatigueMax`
- Clamped to `FatigueMax` (respects probation cap)
- Logs to session diagnostics

### Read-Only Properties

**Property:** `FatigueCurrent`  
**Type:** `int`  
**Description:** Current fatigue points (0 to FatigueMax)

**Property:** `FatigueMax`  
**Type:** `int`  
**Description:** Maximum fatigue capacity (24 normal, 18 probation)

**Usage Example:**
```csharp
var enlistment = EnlistmentBehavior.Instance;
var current = enlistment.FatigueCurrent;
var max = enlistment.FatigueMax;
var percentage = (current * 100) / max;

if (percentage < 20)
{
    ShowLowFatigueWarning();
}
```

---

## Configuration

### Baseline Values

**Default Maximum:** 24 points  
**Probation Cap:** 18 points  
**Location:** Hardcoded in `EnlistmentBehavior`

**Rationale for 24:**
- Represents 24 hours in a day
- Intuitive mental model (1 point per hour)
- Allows 3-5 moderate activities per day
- Forces rest every 2-3 days

### Probation Configuration

**File:** `enlisted_config.json` → `retirement` section

```json
"retirement": {
  "ProbationDays": 12,
  "ProbationFatigueCap": 18
}
```

**ProbationFatigueCap:**
- Default: 18 (75% of normal capacity)
- Min recommended: 12 (still allows minimal activity)
- Max recommended: 20 (still feels restrictive)

**Tuning Considerations:**
- Too low (<12): Players can't function
- Too high (>20): Probation doesn't feel meaningful
- Sweet spot: 16-18 (noticeable but manageable)

### Activity Costs

**Currently:** Hardcoded in decision JSON and order definitions

**Example Decision:**
```json
{
  "id": "dec_weapon_drill",
  "costs": {
    "fatigue": 3
  }
}
```

**Example Order:**
```json
{
  "id": "order_patrol",
  "fatigue_cost": 5
}
```

**Future:** Could be moved to configuration for easier tuning.

---

## Technical Implementation

### Storage

**Class:** `EnlistmentBehavior`  
**Namespace:** `Enlisted.Features.Enlistment.Behaviors`

**Fields:**
```csharp
private int _fatigueCurrent = 24;
private int _fatigueMax = 24;
```

**Persistence:**
```csharp
public override void SyncData(IDataStore dataStore)
{
    dataStore.SyncData("enlisted_fatigueCurrent", ref _fatigueCurrent);
    dataStore.SyncData("enlisted_fatigueMax", ref _fatigueMax);
    
    // Validation after load
    _fatigueMax = Math.Max(1, _fatigueMax);
    _fatigueCurrent = Math.Clamp(_fatigueCurrent, 0, _fatigueMax);
}
```

### Consumption Implementation

```csharp
public bool TryConsumeFatigue(int amount, string reason = null)
{
    if (!IsEnlisted || amount <= 0)
        return true; // No-op for invalid state
    
    var newValue = Math.Max(0, _fatigueCurrent - amount);
    if (newValue == _fatigueCurrent)
        return false; // Insufficient fatigue
    
    _fatigueCurrent = newValue;
    
    SessionDiagnostics.LogEvent("Fatigue", "Consumed",
        $"amount={amount}, now={_fatigueCurrent}/{_fatigueMax}, reason={reason ?? "unknown"}");
    
    CheckFatigueHealthPenalty(); // Evaluate health risks
    
    return true;
}
```

### Health Penalty Check

```csharp
private void CheckFatigueHealthPenalty()
{
    // Implementation in EnlistmentBehavior
    // Checks current fatigue against thresholds
    // Applies medical conditions if critical
    // Escalates Medical Risk meter
    
    // Example logic (simplified):
    if (_fatigueCurrent < 5)
    {
        // Risk of exhaustion condition
        var medical = MedicalConditionsManager.Instance;
        medical?.CheckExhaustionRisk();
    }
}
```

### Probation Integration

```csharp
private void ActivateProbation()
{
    var config = ConfigurationManager.LoadRetirementConfig();
    var fatigueCap = Math.Max(1, config?.ProbationFatigueCap ?? 18);
    
    _fatigueMax = Math.Min(_fatigueMax, fatigueCap);
    _fatigueCurrent = Math.Min(_fatigueCurrent, _fatigueMax);
    
    ModLogger.Info("Probation", $"Fatigue cap reduced to {_fatigueMax}");
}

private void ClearProbation(string reason)
{
    _fatigueMax = 24; // Restore to normal
    // Current value unchanged (player must rest to recover)
    
    ModLogger.Info("Probation", $"Fatigue cap restored to {_fatigueMax}");
}
```

### Diagnostics

**Log Category:** "Fatigue"  
**Log Events:** "Consumed", "Restored"  
**Output Location:** `<BannerlordInstall>/Modules/Enlisted/Debugging/Session-A_*.log`

**Example Log Entries:**
```
[Fatigue] Consumed: amount=5, now=19/24, reason=weapon_drill
[Fatigue] Restored: amount=5, now=24/24, reason=rest_extended
[Probation] Fatigue cap reduced to 18
[Fatigue] Consumed: amount=3, now=15/18, reason=order_patrol
```

### Performance

**Cost:** Negligible
- Consumption: 2 arithmetic operations + 1 method call
- Restoration: 2 arithmetic operations + 1 clamp
- Health check: Conditional evaluation, rarely triggers heavy logic

**Memory:** 8 bytes (2 × int32)

**Thread Safety:** Not required (single-threaded campaign tick)
