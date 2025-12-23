# Baggage Train Availability System

**Summary:** The Baggage Train system gates player access to their personal stowage based on realistic military logistics. The baggage train marches separately from the fighting column, creating natural access windows during camp, settlement stays, and muster events. This transforms inventory management from a passive storage feature into an active gameplay element with strategic considerations.

**Status:** 📋 Specification  
**Last Updated:** 2025-12-23  
**Related Docs:** [Quartermaster System](quartermaster-system.md), [Company Supply Simulation](company-supply-simulation.md), [Provisions & Rations](provisions-rations-system.md)

---

## Index

1. [Overview](#overview)
2. [Design Philosophy](#design-philosophy)
3. [Access States](#access-states)
4. [Access Conditions](#access-conditions)
5. [Emergency Access System](#emergency-access-system)
6. [Baggage Train Events](#baggage-train-events)
7. [Rank-Based Access](#rank-based-access)
8. [Integration Points](#integration-points)
9. [Data Structures](#data-structures)
10. [Configuration](#configuration)
11. [Implementation Plan](#implementation-plan)
12. [Testing Checklist](#testing-checklist)

---

## Overview

### Current State (Before This Feature)

The mod already has a baggage stash system (`_baggageStash` in `EnlistmentBehavior`):
- Dedicated `ItemRoster` for player's stored items
- Cross-faction transfer mechanics (courier system)
- Bag checks at muster for contraband
- Fatigue gating (can't access when exhausted)
- Accessed via `InventoryScreenHelper.OpenScreenAsStash()`

**Problem:** The baggage stash is always accessible via the Camp Hub menu whenever the player isn't fatigued. This doesn't reflect realistic military logistics and misses gameplay opportunities.

### Proposed State (After This Feature)

Access to the baggage train becomes conditional based on:
- **Location:** In settlement vs. on the march
- **Activity:** Resting vs. actively moving
- **Timing:** Muster days, periodic "caught up" windows
- **Rank:** Higher ranks gain more flexible access
- **Context:** Strategic situation (siege, pursuit, garrison)

---

## Design Philosophy

### Core Concept: "The Baggage Train Marches Separate"

In medieval armies, the baggage train (supply wagons, personal belongings, camp followers) marched separately from the fighting column, typically at the rear. Soldiers couldn't just walk back mid-march to grab spare equipment. This creates:

1. **Preparation Pressure:** Pack what you need before leaving settlement
2. **Access Windows:** Plan activities around when baggage is accessible
3. **Risk/Reward:** Store valuables vs. keep them on person
4. **Narrative Immersion:** Feel like part of a real military column

### Gameplay Goals

| Goal | Implementation |
|------|----------------|
| **Strategic Preparation** | Can't grab gear mid-battle; must prepare beforehand |
| **Natural Pacing** | Access during rest periods feels earned |
| **Rank Progression** | Officers have privileges lowly soldiers don't |
| **Event Hooks** | Baggage delays, raids, and theft create drama |
| **No Frustration** | Emergency access exists with reputation cost |

---

## Access States

### BaggageAccessState Enum

```csharp
public enum BaggageAccessState
{
    /// <summary>Full, unrestricted access to baggage stash.</summary>
    FullAccess,
    
    /// <summary>Brief access window (baggage caught up). Auto-expires.</summary>
    TemporaryAccess,
    
    /// <summary>No access - baggage train is behind the column.</summary>
    NoAccess,
    
    /// <summary>Locked by QM - supply crisis or contraband lockdown.</summary>
    Locked
}
```

### State Transitions

```
┌─────────────────┐
│   FullAccess    │ ← In settlement, during muster, army encamped
│   (unlimited)   │
└────────┬────────┘
         │ Leave settlement / army begins march
         ▼
┌─────────────────┐
│    NoAccess     │ ← On the march, in combat, forced march
│ (baggage behind)│
└────────┬────────┘
         │ Baggage "catches up" event (periodic)
         ▼
┌─────────────────┐
│ TemporaryAccess │ ← Brief window before march resumes
│   (expires)     │
└────────┬────────┘
         │ Window expires OR army moves
         ▼
┌─────────────────┐
│    NoAccess     │ ← Back to march state
└─────────────────┘

Special override:
┌─────────────────┐
│     Locked      │ ← Supply < 20% OR contraband investigation
│ (QM authority)  │
└─────────────────┘
```

---

## Access Conditions

### When Baggage IS Accessible (FullAccess)

| Condition | Detection Method | Notes |
|-----------|------------------|-------|
| **In Settlement** | `party.CurrentSettlement != null` | Town, castle, or village |
| **At Muster** | `_payMusterPending == true` or within 6 hours after | Natural logistics pause |
| **Army Encamped** | `!party.IsMoving && party.Army != null` | Army halted for rest |
| **During Rest Activity** | Player in rest menu | Explicitly resting |
| **Garrison Duty Context** | `GetLordStrategicContext() == "garrison_duty"` | Stationary defense |

### When Baggage is NOT Accessible (NoAccess)

| Condition | Detection Method | Notes |
|-----------|------------------|-------|
| **On the March** | `party.IsMoving && party.CurrentSettlement == null` | Moving on campaign map |
| **Active Battle** | `party.MapEvent != null` | In combat encounter |
| **Forced March/Pursuit** | Party speed > 1.5× base OR chasing target | High-speed movement |
| **Active Siege** | `party.BesiegerCamp != null` or `party.SiegeEvent != null` | Focus on siege operations |
| **Recent Battle** | Within 2 hours of `MapEventEnded` | Reorganizing after combat |

### When Baggage is Locked (Locked)

| Condition | Detection Method | Notes |
|-----------|------------------|-------|
| **Critical Supply** | `CompanyNeeds.Supplies < 20` | QM has locked down all storage |
| **Contraband Investigation** | `_bagCheckInProgress == true` | Ongoing inspection |
| **Lord's Orders** | Future: Order flag blocking access | Mission requires travel light |

### Temporary Access Windows

Periodic "baggage caught up" events create brief access windows on campaign:

| Trigger | Duration | Cooldown |
|---------|----------|----------|
| **Automatic (daily check)** | 4 game hours | 18-30 hours |
| **After Skirmish** | 2 game hours | 8 hours |
| **Night Halt** | Until dawn or movement | Once per night |
| **Rank T7+ Request** | 2 game hours | 12 hours |

---

## Emergency Access System

### Begging the Quartermaster

When access is blocked (NoAccess), players can request emergency access with consequences:

```
[Ask QM for baggage access] (visible when NoAccess)

QM: "The wagons are a quarter-mile back. We're not stopping 
     the whole column because you forgot your spare bowstrings."

Options:
  → "It's urgent. I'll make it quick." 
      [-5 QM Rep, grants TemporaryAccess (2 hours)]
      
  → "You're right. Forget it." 
      [No penalty, no access]
      
  → [QM Rep 50+] "Come on, you know I'm good for it." 
      [No penalty, grants TemporaryAccess (2 hours)]
      
  → [T7+ Commander] "Have the wagons brought up."
      [-3 Officer Rep (inconvenience), grants FullAccess until movement]
```

### Emergency Access Limits

- Can only request emergency access once per 12-hour period
- Each request (except high rep/rank versions) costs -5 QM Rep
- Repeated requests in short period: additional -2 Soldier Rep (seen as needy)

---

## Baggage Train Events

### Event Category: Baggage Train (`events_baggage.json`)

#### Baggage Caught Up (Positive)

| Event ID | Trigger | Description |
|----------|---------|-------------|
| `evt_baggage_arrived` | Daily tick when on march, ~25% chance | Wagons catch up during a halt |

```json
{
  "id": "evt_baggage_arrived",
  "titleId": "evt_baggage_arrived_title",
  "title": "Baggage Train Arrives",
  "setupId": "evt_baggage_arrived_setup",
  "setup": "The sergeant calls a brief halt. The baggage wagons have caught up to the column, their ox-drivers looking exhausted but relieved.",
  "category": "logistics",
  "context": ["march", "campaign"],
  "tier_min": 1,
  "options": [
    {
      "id": "access_baggage",
      "textId": "evt_baggage_arrived_opt_access",
      "text": "Good. I need to get something from my kit.",
      "effects": { "grant_temporary_baggage_access": 4 }
    },
    {
      "id": "no_need",
      "textId": "evt_baggage_arrived_opt_skip",
      "text": "I'm fine. Let them rest.",
      "effects": { "soldier_rep": 2 }
    }
  ]
}
```

#### Baggage Delayed (Negative)

| Event ID | Trigger | Description |
|----------|---------|-------------|
| `evt_baggage_delayed` | Bad weather, mountains, enemy territory | Wagons fall behind |
| `evt_baggage_raided` | Enemy territory + low scout coverage | Raiders hit supply train |
| `evt_baggage_stuck` | River crossing, rough terrain | Physical obstacle |

```json
{
  "id": "evt_baggage_delayed",
  "titleId": "evt_baggage_delayed_title", 
  "title": "Baggage Train Delayed",
  "setupId": "evt_baggage_delayed_setup",
  "setup": "Word comes down the column: the baggage wagons are stuck in the mud two leagues back. It'll be a day or more before they catch up.",
  "category": "logistics",
  "context": ["march", "rain", "mountains"],
  "tier_min": 1,
  "options": [
    {
      "id": "wait",
      "textId": "evt_baggage_delayed_opt_wait",
      "text": "Nothing to be done. We press on.",
      "effects": { "baggage_delay_days": 1 }
    },
    {
      "id": "volunteer_help",
      "textId": "evt_baggage_delayed_opt_help",
      "text": "I'll go back and help dig them out.",
      "tooltip": "Costs 15 Fatigue, may reduce delay",
      "requirements": { "fatigue_min": 20 },
      "effects": { 
        "fatigue": -15,
        "baggage_delay_days": -1,
        "qm_rep": 5,
        "athletics_xp": 20
      }
    }
  ]
}
```

#### Baggage Raided (Combat/Loss)

```json
{
  "id": "evt_baggage_raided",
  "titleId": "evt_baggage_raided_title",
  "title": "Raiders Hit the Baggage Train",
  "setupId": "evt_baggage_raided_setup",
  "setup": "Shouts from the rear of the column. Enemy raiders are attacking the supply wagons!",
  "category": "combat",
  "context": ["enemy_territory", "war"],
  "tier_min": 2,
  "options": [
    {
      "id": "rush_to_defend",
      "textId": "evt_baggage_raided_opt_defend",
      "text": "Rush to defend the wagons!",
      "tooltip": "Combat encounter. Risk injury. Save your belongings.",
      "effects": { "trigger_baggage_defense_combat": true }
    },
    {
      "id": "stay_formation",
      "textId": "evt_baggage_raided_opt_stay",
      "text": "Stay in formation. The rearguard will handle it.",
      "tooltip": "May lose some personal items, but no personal risk.",
      "effects": { 
        "random_baggage_loss": 2,
        "officer_rep": 2
      }
    },
    {
      "id": "order_counterattack",
      "textId": "evt_baggage_raided_opt_order",
      "text": "[T7+ Commander] Order a counterattack!",
      "requirements": { "tier_min": 7 },
      "tooltip": "Use your authority. Full defense, risk to soldiers.",
      "effects": { 
        "trigger_baggage_defense_combat": true,
        "officer_rep": -3,
        "leadership_xp": 30
      }
    }
  ]
}
```

#### Theft from Baggage (Social)

```json
{
  "id": "evt_baggage_theft",
  "titleId": "evt_baggage_theft_title",
  "title": "Missing from Your Kit",
  "setupId": "evt_baggage_theft_setup",
  "setup": "When you finally get access to your stowed belongings, something's missing. That {ITEM_NAME} you stored - gone. Someone's been through your things.",
  "category": "social",
  "context": ["camp"],
  "tier_min": 1,
  "requirements": { "soldier_rep_max": 40, "baggage_has_items": true },
  "options": [
    {
      "id": "let_it_go",
      "textId": "evt_baggage_theft_opt_ignore",
      "text": "Could have been anyone. Let it go.",
      "effects": { "random_baggage_loss": 1 }
    },
    {
      "id": "investigate",
      "textId": "evt_baggage_theft_opt_investigate",
      "text": "Ask around. Someone saw something.",
      "tooltip": "Charm check to identify thief",
      "effects": { 
        "skill_check": { "skill": "Charm", "threshold": 40 },
        "success": { "recover_item": true, "soldier_rep": -5 },
        "failure": { "random_baggage_loss": 1, "soldier_rep": -3 }
      }
    },
    {
      "id": "report_nco",
      "textId": "evt_baggage_theft_opt_report",
      "text": "Report it to the sergeant.",
      "tooltip": "Official channels. Might make enemies.",
      "effects": { 
        "random_baggage_loss": 1,
        "officer_rep": 3,
        "soldier_rep": -8
      }
    }
  ]
}
```

---

## Rank-Based Access

### Access Privileges by Tier

| Tier | Rank | Access Privileges |
|------|------|-------------------|
| **T1-T2** | Recruit/Soldier | Muster + Settlement only. No emergency access. |
| **T3-T4** | Veteran/Corporal | Above + Camp rest. Emergency access available (costs rep). |
| **T5-T6** | NCO | Above + Daily "caught up" window. Reduced emergency cost. |
| **T7-T9** | Officer/Commander | Above + Can request column halt (1/day). Free emergency access. |

### Tier-Gated Features

```csharp
public bool CanRequestEmergencyAccess()
{
    // T1-T2: Cannot request emergency access at all
    if (_enlistmentTier < 3) return false;
    
    // T3+: Can request (with varying costs)
    return true;
}

public int GetEmergencyAccessRepCost()
{
    if (_enlistmentTier >= 7) return 0;      // Commander: free
    if (_enlistmentTier >= 5) return 2;      // NCO: minimal cost
    return 5;                                  // Enlisted: full cost
}

public bool CanHaltColumn()
{
    // Only T7+ commanders can order the column to halt
    return _enlistmentTier >= 7;
}
```

---

## Integration Points

### Existing Systems to Modify

#### 1. EnlistmentBehavior.cs

| Location | Change |
|----------|--------|
| `_baggageStash` field | Add `_lastBaggageAccessTime`, `_baggageDelayUntil`, `_temporaryAccessExpires` |
| `TryOpenBaggageTrain()` | Add access state check before `OpenScreenAsStash()` |
| `OnHourlyTick()` | Add baggage access state updates |
| `OnDailyTick()` | Add baggage "caught up" event check |
| `SyncData()` | Persist new baggage state fields |

#### 2. EnlistedMenuBehavior.cs

| Location | Change |
|----------|--------|
| Camp Hub menu | Modify baggage option condition to check `BaggageTrainManager.GetAccessState()` |
| Tooltip text | Show WHY access is blocked ("Baggage train is behind the column") |
| New option | Add "Request Baggage Access" when NoAccess state |

#### 3. ArmyContextAnalyzer.cs

| Location | Change |
|----------|--------|
| `GetLordStrategicContext()` | Already returns contexts we need |
| New method | `IsArmyMoving()`, `IsInForcedMarch()` for access detection |

#### 4. CompanySupplyManager.cs

| Location | Change |
|----------|--------|
| Add method | `ShouldLockBaggage()` - returns true when supply < 20% |

#### 5. QuartermasterManager.cs

| Location | Change |
|----------|--------|
| Conversation | Add emergency access request dialogue branch |

### New Files to Create

| File | Purpose |
|------|---------|
| `src/Features/Logistics/BaggageTrainManager.cs` | Core manager for access state, events, timing |
| `src/Features/Logistics/BaggageAccessState.cs` | Enum definition |
| `ModuleData/Enlisted/Events/events_baggage.json` | Baggage train events |
| `ModuleData/Enlisted/baggage_config.json` | Timing, costs, thresholds |
| `ModuleData/Languages/enlisted_baggage_strings.xml` | Localization |

---

## Data Structures

### BaggageTrainManager State

```csharp
public class BaggageTrainManager : CampaignBehaviorBase
{
    public static BaggageTrainManager Instance { get; private set; }
    
    // Core state
    private BaggageAccessState _currentState = BaggageAccessState.FullAccess;
    private CampaignTime _temporaryAccessExpires = CampaignTime.Zero;
    private CampaignTime _baggageDelayedUntil = CampaignTime.Zero;
    private CampaignTime _lastEmergencyRequest = CampaignTime.Zero;
    private int _emergencyRequestsToday = 0;
    
    // Tracking
    private bool _baggageCaughtUpEventFiredToday = false;
    private CampaignTime _lastStateChangeTime = CampaignTime.Zero;
    
    // Public API
    public BaggageAccessState GetCurrentAccess() { ... }
    public bool TryRequestEmergencyAccess(out string failReason) { ... }
    public void GrantTemporaryAccess(int hours) { ... }
    public void ApplyBaggageDelay(int days) { ... }
}
```

### Configuration Schema (baggage_config.json)

```json
{
  "access_windows": {
    "temporary_access_hours": 4,
    "night_halt_grants_access": true,
    "muster_grants_access": true,
    "settlement_always_access": true
  },
  "timing": {
    "caught_up_check_hours": 24,
    "caught_up_chance_percent": 25,
    "min_cooldown_hours": 18,
    "max_cooldown_hours": 30
  },
  "emergency_access": {
    "base_qm_rep_cost": 5,
    "nco_qm_rep_cost": 2,
    "officer_qm_rep_cost": 0,
    "cooldown_hours": 12,
    "high_rep_threshold": 50,
    "spam_penalty_soldier_rep": 2
  },
  "rank_gates": {
    "emergency_request_min_tier": 3,
    "column_halt_min_tier": 7,
    "daily_access_window_min_tier": 5
  },
  "lockdown": {
    "supply_threshold_percent": 20,
    "contraband_investigation_blocks": true
  },
  "events": {
    "delay_event_chance_bad_weather": 15,
    "delay_event_chance_mountains": 10,
    "raid_event_chance_enemy_territory": 8,
    "theft_event_chance_low_rep": 5
  }
}
```

---

## Implementation Plan

### Phase 1: Core Infrastructure (Est. 8 hours)

**Goal:** Create the manager class and access state system.

#### Tasks

1. **Create `BaggageAccessState.cs`** (enum)
   - Define 4 states: FullAccess, TemporaryAccess, NoAccess, Locked

2. **Create `BaggageTrainManager.cs`**
   - Singleton pattern matching existing managers
   - `GetCurrentAccess()` - evaluates all conditions
   - `GrantTemporaryAccess(hours)` - sets timed window
   - `ApplyBaggageDelay(days)` - delays baggage arrival
   - Hook into hourly/daily tick for state updates

3. **Add to EnlistmentBehavior.cs**
   - Instantiate manager on enlistment
   - Persist state in `SyncData()`
   - Call manager updates in tick handlers

4. **Create baggage_config.json**
   - All timing and threshold values
   - Load via `ConfigurationManager`

5. **Add to Enlisted.csproj**
   - Add new file entries

#### Acceptance Criteria
- [ ] `BaggageTrainManager.GetCurrentAccess()` returns correct state based on conditions
- [ ] State persists across save/load
- [ ] Config values load correctly

---

### Phase 2: Menu Integration (Est. 6 hours)

**Goal:** Modify Camp Hub to respect baggage access state.

#### Tasks

1. **Modify `EnlistedMenuBehavior.cs`**
   - Find existing baggage stash menu option
   - Add condition check: `BaggageTrainManager.Instance?.GetCurrentAccess()`
   - Set appropriate tooltip for each blocked state
   - Disable option when NoAccess or Locked

2. **Modify `EnlistmentBehavior.TryOpenBaggageTrain()`**
   - Add access check before opening screen
   - Display appropriate message if blocked

3. **Add "Request Baggage Access" option**
   - Visible only when NoAccess
   - Gated by tier (T3+)
   - Shows cost in tooltip
   - Calls `TryRequestEmergencyAccess()`

4. **Add localization strings**
   - `{=baggage_blocked_march}The baggage train is behind the column.`
   - `{=baggage_blocked_locked}The quartermaster has locked down all storage.`
   - `{=baggage_blocked_battle}No time for that now!`
   - `{=baggage_request_cost}Request access (-{COST} QM Rep)`

#### Acceptance Criteria
- [ ] Baggage option disabled when on march
- [ ] Tooltip explains WHY access is blocked
- [ ] Emergency request option appears for T3+
- [ ] High rep (50+) can request without cost

---

### Phase 3: Automatic Access Windows (Est. 4 hours)

**Goal:** Implement periodic "baggage caught up" events.

#### Tasks

1. **Add hourly/daily checks in BaggageTrainManager**
   - Check if on march (NoAccess state)
   - Roll for "caught up" event based on config chance
   - Grant TemporaryAccess if successful
   - Respect cooldown between events

2. **Add night halt detection**
   - If hour >= 20 or hour < 6 AND party not moving
   - Grant TemporaryAccess until party moves

3. **Add muster integration**
   - Hook into `OnMusterCycleComplete()`
   - Grant FullAccess for duration of muster

4. **Display notification**
   - "The baggage wagons have caught up. You can access your belongings."
   - Use `InformationManager.DisplayMessage()`

#### Acceptance Criteria
- [ ] ~25% chance per day on march to get temporary access
- [ ] Night halts grant access
- [ ] Muster always grants access
- [ ] Notifications display correctly

---

### Phase 4: Baggage Events (Est. 10 hours)

**Goal:** Create the JSON events and integrate with event system.

#### Tasks

1. **Create `events_baggage.json`**
   - `evt_baggage_arrived` - positive access window event
   - `evt_baggage_delayed` - delay by 1+ days
   - `evt_baggage_raided` - combat/loss event
   - `evt_baggage_theft` - item loss event
   - `evt_baggage_stuck` - terrain obstacle

2. **Add event triggers in BaggageTrainManager**
   - Hook into daily tick for event checks
   - Weather/terrain detection for delay events
   - Enemy territory detection for raid events
   - Low soldier rep detection for theft events

3. **Implement event effects**
   - `grant_temporary_baggage_access` - new effect type
   - `baggage_delay_days` - delays baggage arrival
   - `random_baggage_loss` - removes random item from stash
   - `trigger_baggage_defense_combat` - starts combat encounter

4. **Add localization strings**
   - All event titles, setups, options, tooltips

5. **Update event-catalog-by-system.md**
   - Add Baggage Train Events section

#### Acceptance Criteria
- [ ] Events fire at appropriate times
- [ ] Delay events block access for specified duration
- [ ] Raid events trigger combat or item loss
- [ ] Theft events remove items from stash

---

### Phase 5: Rank Progression (Est. 4 hours)

**Goal:** Implement tier-gated access privileges.

#### Tasks

1. **Modify emergency access**
   - T1-T2: Cannot request at all
   - T3-T4: Full cost (-5 QM Rep)
   - T5-T6: Reduced cost (-2 QM Rep)
   - T7+: Free (no cost)

2. **Add daily access window for T5+**
   - NCOs get guaranteed access window each day
   - Separate from random "caught up" events

3. **Add column halt for T7+**
   - Commanders can order full access once per day
   - Costs Officer Rep (-3) for inconvenience
   - Conversation option with QM

4. **Add to Daily Brief**
   - "The baggage train is behind the column" when NoAccess
   - "Your belongings are accessible in camp" when FullAccess

#### Acceptance Criteria
- [ ] T1-T2 cannot request emergency access
- [ ] T5+ get daily access window
- [ ] T7+ can halt column
- [ ] Costs scale appropriately by rank

---

### Phase 6: News & Reports Integration (Est. 3 hours)

**Goal:** Add baggage status to Daily Brief and news system.

#### Tasks

1. **Add to Daily Brief (`EnlistedNewsBehavior.cs`)**
   - `BuildBaggageStatusLine()` method
   - "The supply wagons are half a league behind" when delayed
   - "Your belongings are accessible" when FullAccess
   - Only show when notable (delayed or recently arrived)

2. **Add dispatch items for baggage events**
   - Personal feed: "Baggage train raided" 
   - Personal feed: "Supplies delayed by weather"

3. **Add to Company Status Report**
   - Include baggage status in Supplies section context

#### Acceptance Criteria
- [ ] Daily Brief mentions baggage when relevant
- [ ] Personal feed shows baggage events
- [ ] Reports integrate baggage status

---

### Phase 7: System Integration & Edge Cases (Est. 8 hours)

**Goal:** Handle all cross-system edge cases documented in Edge Cases Matrix.

#### Tasks

1. **Leave System Integration**
   - Detect `IsOnLeave` state and grant FullAccess
   - Handle leave expiration gracefully
   - Clear delay state when leave starts

2. **Grace Period Integration**
   - Detect `IsInDesertionGracePeriod` state
   - Grant FullAccess during grace period
   - Preserve baggage delay on grace → re-enlist (same faction)
   - Clear delay on grace → cross-faction transfer

3. **Combat/Reserve Integration**
   - Check `MapEvent` for active battle
   - Check `EnlistedEncounterBehavior.IsWaitingInReserve`
   - Implement 2-hour post-battle cooldown

4. **Capture/Prisoner Integration**
   - Freeze baggage delay timer during captivity
   - Resume delay countdown on release
   - Defer events during captivity (already implemented for courier)

5. **Siege Integration**
   - Defender (inside settlement) = FullAccess
   - Attacker (besieging) = NoAccess or Limited
   - Assault in progress = NoAccess

6. **Discharge Integration**
   - Add `HandleBaggageOnDischarge()` method
   - Deserter: Forfeit all baggage
   - Dishonorable: Reclaim QM items from baggage
   - Other discharges: Keep baggage

7. **Priority-Based Access Resolution**
   - Implement priority order in `GetCurrentAccess()`
   - Document priority order in code comments

8. **Performance Optimization**
   - Cache access state with dirty flag
   - Invalidate on: tick, settlement change, battle state, muster
   - Avoid per-frame recalculation

#### Acceptance Criteria
- [ ] Access correct during leave
- [ ] Access correct during grace period
- [ ] Access blocked during combat
- [ ] Access blocked during reserve mode
- [ ] Delay frozen during captivity
- [ ] Siege states handled correctly
- [ ] Discharge types handle baggage appropriately
- [ ] Priority resolution works when multiple conditions apply
- [ ] No crashes on any state transition

---

### Phase 8: Retinue & Promotion Integration (Est. 3 hours)

**Goal:** Ensure baggage system works with T7+ Commander features.

#### Tasks

1. **T7 Promotion Interaction**
   - If baggage delayed during promotion, queue formation selection
   - Don't block promotion due to baggage state
   - Show notification that formation selection deferred

2. **Retinue Equipment (Future)**
   - Consider requiring baggage access for retinue gear management
   - Stub out integration point for future feature

3. **Commander Access Privileges**
   - Implement "halt column" ability for T7+
   - Add QM conversation option for commanders

#### Acceptance Criteria
- [ ] T7 promotion works regardless of baggage state
- [ ] Formation selection queued if needed
- [ ] T7+ can halt column for access

---

### Phase 9: Polish & QA (Est. 4 hours)

**Goal:** Final polish and comprehensive testing.

#### Tasks

1. **QM Conversation Branch**
   - "What about my belongings?" query
   - QM explains current baggage status
   - Context-aware responses (delayed, accessible, locked)

2. **Notification Polish**
   - Clear messages when access state changes
   - Tooltips explain current state
   - Daily Brief includes baggage status when relevant

3. **Save/Load Verification**
   - Verify all state fields persist correctly
   - Test load during various states (delayed, temp access, locked)
   - Verify cooldowns resume correctly

4. **Documentation Update**
   - Update Content Catalog with new events
   - Update Core Gameplay with baggage references
   - Ensure all new localization strings documented

#### Acceptance Criteria
- [ ] QM can explain baggage status
- [ ] All notifications clear and helpful
- [ ] Save/load works for all states
- [ ] Documentation complete

---

## Edge Cases Matrix

### System Integration Edge Cases

Based on analysis of the codebase, these systems interact with baggage access:

#### 1. Leave System (`_isOnLeave`)

| Scenario | Baggage Access | Reasoning |
|----------|----------------|-----------|
| Player on leave | **FullAccess** | Player is visible on map, party is active |
| Leave expires → desertion | Access at moment of expiry, then N/A | Clear before penalties apply |
| Leave → became vassal | **FullAccess** | Honorable transition |

**Implementation Note:** Leave check in `GetCurrentAccess()`:
```csharp
// Player on leave has full access (party is visible/active)
if (EnlistmentBehavior.Instance?.IsOnLeave == true)
    return BaggageAccessState.FullAccess;
```

#### 2. Grace Period (Lord Death/Capture)

| Scenario | Baggage Access | Duration |
|----------|----------------|----------|
| Grace period active | **FullAccess** | 14 days |
| Grace → rejoin same faction | Baggage preserved | Seamless transfer |
| Grace → join different faction | Cross-faction transfer prompt | Standard transfer flow |
| Grace → expires (desertion) | Baggage forfeited? | Edge case to decide |

**Design Decision Needed:** Should deserters forfeit their baggage? Options:
- A) Forfeit all baggage (harsh but realistic)
- B) Keep baggage but pay crime rating penalty
- C) Baggage left with previous faction (recoverable later)

#### 3. Capture/Prisoner State

| Scenario | Baggage Access | Notes |
|----------|----------------|-------|
| Player captured | **NoAccess** (implicit) | Native captivity system handles |
| Lord captured | **FullAccess** (grace) | Grace period starts |
| Player freed from captivity | Depends on state | Check grace period status |
| Courier arrives during captivity | Deferred | Already implemented |

**Implementation Note:** Prisoner state already defers courier delivery:
```csharp
// In ProcessCourierArrival():
if (Hero.MainHero?.IsPrisoner == true)
{
    // Defer delivery until player is free
    return;
}
```

#### 4. Combat & Battle Reserve

| Scenario | Baggage Access | Reasoning |
|----------|----------------|-----------|
| In active battle | **NoAccess** | Can't access baggage mid-combat |
| Wait in reserve | **NoAccess** | Still in battle encounter |
| Post-battle (2 hours) | **NoAccess** | Reorganizing |
| After post-battle period | Normal evaluation | Resume normal rules |

**Implementation Note:** Check `IsWaitingInReserve` from `EnlistedEncounterBehavior`:
```csharp
if (EnlistedEncounterBehavior.IsWaitingInReserve)
    return BaggageAccessState.NoAccess;
```

#### 5. Retinue System (T7+)

| Scenario | Baggage Access | Integration Needed |
|----------|----------------|-------------------|
| Retinue cleared (capture) | Normal evaluation | No special handling |
| Retinue cleared (discharge) | N/A | Enlistment ended |
| T7 promotion + baggage delayed | Event conflict possible | Queue formation selection |
| Retinue requisition | Requires access? | Consider adding requirement |

**Enhancement Opportunity:** Could require baggage access for retinue equipment management (future feature).

#### 6. Town/Settlement Access

| Scenario | Baggage Access | Notes |
|----------|----------------|-------|
| In town (synthetic encounter) | **FullAccess** | Settlement = access |
| In castle | **FullAccess** | Settlement = access |
| In village | **FullAccess** | Settlement = access |
| Settlement under siege | Depends on side | Defender = FullAccess, Attacker = siege rules |

**Implementation Note:** Use existing `party.CurrentSettlement` check.

#### 7. Discharge Scenarios

| Discharge Type | Baggage Handling | Reasoning |
|----------------|------------------|-----------|
| Veteran | Keep all | Outstanding service |
| Honorable | Keep all | Good service |
| Washout | Keep all | Completed service |
| Dishonorable | QM-issued reclaimed | Misconduct |
| Deserter | All forfeited | Abandoned post |
| Grace (lord death) | Keep all | Not player's fault |

**Implementation Note:** Already has `ReclaimQmIssuedEquipment()` at discharge. Extend for baggage:
```csharp
private void HandleBaggageOnDischarge(string dischargeBand)
{
    switch (dischargeBand)
    {
        case "deserter":
            // Forfeit all baggage
            _baggageStash.Clear();
            ModLogger.Info("Discharge", "Baggage forfeited due to desertion");
            break;
        case "dishonorable":
            // Reclaim QM-issued items from baggage
            ReclaimQmIssuedFromBaggage();
            break;
        // Other bands: keep all baggage
    }
}
```

#### 8. Siege Operations

| Scenario | Baggage Access | Reasoning |
|----------|----------------|-----------|
| Besieging (attacker) | **Limited** or NoAccess | Siege focus, baggage train protected |
| Besieged (defender) | **FullAccess** | Inside settlement |
| Assault in progress | **NoAccess** | Combat |
| Post-siege (victory) | **FullAccess** | Settled in |

#### 9. Baggage Delay During State Changes

| State Change | Delay Persists? | Handling |
|--------------|-----------------|----------|
| Enlistment → Leave | No | Cancel delay, restore access |
| Leave → Return to service | Yes (if still active) | Resume delay countdown |
| Active → Captured | Freeze delay | Resume on release |
| Grace period → Re-enlist | Transfer delay to new lord? | Edge case to decide |
| Discharge | Clear delay | Baggage becomes personal |

### Cross-System Conflict Resolution

When multiple conditions apply, use this priority order:

1. **Captivity** (highest) - Native system takes over, no menu access at all
2. **Combat/Battle** - No access during active encounters
3. **Locked (QM)** - Supply crisis or investigation overrides other states
4. **Leave** - Full access (player is active)
5. **Grace Period** - Full access (player is active)
6. **Settlement** - Full access
7. **Muster** - Full access window
8. **March State** - No access / temporary access windows
9. **Default** (lowest) - Normal access

**Implementation:**
```csharp
public BaggageAccessState GetCurrentAccess()
{
    // Priority 1: Captivity (implicit - no menu access while prisoner)
    if (Hero.MainHero?.IsPrisoner == true)
        return BaggageAccessState.NoAccess;
    
    // Priority 2: Combat/Battle
    var party = MobileParty.MainParty;
    if (party?.MapEvent != null || EnlistedEncounterBehavior.IsWaitingInReserve)
        return BaggageAccessState.NoAccess;
    
    // Priority 3: Locked (supply crisis)
    if (CompanySupplyManager.Instance?.TotalSupply < 20 || _bagCheckInProgress)
        return BaggageAccessState.Locked;
    
    // Priority 4-5: Leave or Grace Period (player active on map)
    if (EnlistmentBehavior.Instance?.IsOnLeave == true || 
        EnlistmentBehavior.Instance?.IsInDesertionGracePeriod == true)
        return BaggageAccessState.FullAccess;
    
    // Priority 6: Settlement
    if (party?.CurrentSettlement != null)
        return BaggageAccessState.FullAccess;
    
    // Priority 7: Muster window
    if (_musterAccessActive)
        return BaggageAccessState.FullAccess;
    
    // Priority 8: Temporary access
    if (_temporaryAccessExpires > CampaignTime.Now)
        return BaggageAccessState.TemporaryAccess;
    
    // Priority 9: Check march state
    if (party?.IsMoving == true && party.CurrentSettlement == null)
        return BaggageAccessState.NoAccess;
    
    // Default: Army encamped/halted
    return BaggageAccessState.FullAccess;
}
```

---

## Testing Checklist

### Unit Tests

- [ ] `GetCurrentAccess()` returns FullAccess in settlement
- [ ] `GetCurrentAccess()` returns NoAccess on march
- [ ] `GetCurrentAccess()` returns Locked when supply < 20%
- [ ] Temporary access expires after duration
- [ ] Delay extends NoAccess period
- [ ] Config values load correctly

### Integration Tests

- [ ] Menu option disabled when NoAccess
- [ ] Menu option enabled when FullAccess
- [ ] Emergency request grants TemporaryAccess
- [ ] Emergency request costs QM Rep (T3-T4)
- [ ] Emergency request free for T7+
- [ ] Muster grants FullAccess
- [ ] Night halt grants TemporaryAccess

### Gameplay Tests

- [ ] Play through a campaign cycle checking access at each phase
- [ ] Verify baggage "caught up" events fire ~25% of march days
- [ ] Verify delay events extend NoAccess period
- [ ] Verify theft events remove items
- [ ] Verify raid events trigger combat
- [ ] Verify cross-faction transfer still works
- [ ] Verify save/load preserves baggage state

### Edge Case Tests

**Leave System:**
- [ ] Baggage accessible during leave
- [ ] Leave expires → baggage cleared (if deserter forfeiture enabled)
- [ ] Leave → vassal promotion → baggage preserved

**Grace Period:**
- [ ] Baggage accessible during grace period
- [ ] Grace → rejoin same faction → baggage preserved
- [ ] Grace → cross-faction → transfer prompt appears
- [ ] Grace expires → baggage handling (per design decision)

**Combat:**
- [ ] Baggage blocked during battle
- [ ] Baggage blocked during reserve mode
- [ ] Baggage accessible 2+ hours after battle
- [ ] No baggage events fire during combat

**Capture:**
- [ ] Courier deferred during captivity
- [ ] Baggage state frozen during captivity
- [ ] Access restored after release

**Retinue:**
- [ ] T7 promotion works with delayed baggage
- [ ] Retinue clear doesn't affect baggage state

**Siege:**
- [ ] Defender in siege has access (inside settlement)
- [ ] Attacker in siege has limited access
- [ ] Siege assault blocks access

**Discharge:**
- [ ] Veteran discharge keeps baggage
- [ ] Deserter discharge forfeits baggage
- [ ] Dishonorable discharge reclaims QM items from baggage

---

## Summary

### Files to Create

| File | Lines (Est.) | Purpose |
|------|--------------|---------|
| `src/Features/Logistics/BaggageAccessState.cs` | 20 | Enum definition |
| `src/Features/Logistics/BaggageTrainManager.cs` | 500 | Core manager with edge case handling |
| `ModuleData/Enlisted/baggage_config.json` | 50 | Configuration |
| `ModuleData/Enlisted/Events/events_baggage.json` | 200 | Events |
| `ModuleData/Languages/enlisted_baggage_strings.xml` | 80 | Localization (expanded) |

### Files to Modify

| File | Changes |
|------|---------|
| `EnlistmentBehavior.cs` | Add manager init, persist state, hook ticks, discharge handling |
| `EnlistedMenuBehavior.cs` | Add access checks, new menu options |
| `QuartermasterManager.cs` | Add emergency access conversation, baggage status query |
| `EnlistedNewsBehavior.cs` | Add baggage status to Daily Brief |
| `EnlistedEncounterBehavior.cs` | Add reserve mode checks for baggage |
| `RetinueRecruitmentGrant.cs` | Queue formation selection if baggage delayed |
| `Enlisted.csproj` | Add new file entries |
| `Content/event-catalog-by-system.md` | Document new events |

### Estimated Total Effort

| Phase | Hours |
|-------|-------|
| Phase 1: Core Infrastructure | 8 |
| Phase 2: Menu Integration | 6 |
| Phase 3: Automatic Access Windows | 4 |
| Phase 4: Baggage Events | 10 |
| Phase 5: Rank Progression | 4 |
| Phase 6: News Integration | 3 |
| Phase 7: System Integration & Edge Cases | 8 |
| Phase 8: Retinue & Promotion Integration | 3 |
| Phase 9: Polish & QA | 4 |
| **Total** | **50 hours** |

### Risk Assessment

| Risk | Impact | Mitigation |
|------|--------|------------|
| Combat state detection edge cases | Medium | Comprehensive unit tests for all MapEvent states |
| Save/load during temporary access | Low | Validate state on load, expire stale windows |
| Cross-system priority conflicts | Medium | Document priority order, central resolution logic |
| Performance with frequent state checks | Low | Cache with dirty flag, throttle recalculation |
| Native system interactions (captivity) | Low | Defer to native, don't interfere with prisoner state |

---

**End of Document**

