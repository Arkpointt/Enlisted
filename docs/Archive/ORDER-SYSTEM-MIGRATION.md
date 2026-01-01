# Order System Migration: Instant → Progression

**Purpose:** Documents what OLD order resolution code must be removed when implementing the Order Progression System.

**Status:** 📋 Migration Guide  
**Created:** 2025-12-29

---

## Executive Summary

The Order Progression System **completely replaces** the current instant-resolution order mechanism. This is not an addition - it's a replacement.

**Current System:** Accept → Skill Check → Immediate Result  
**New System:** Accept → Multi-Day Progression → Accumulated Consequences

---

## What The Current System Does

**File:** `src/Features/Orders/Behaviors/OrderManager.cs`

### Current Flow:
```
1. Order Issued Every 3-5 Days (timer-based)
   ↓
2. Player Sees Popup: [Accept] [Decline]
   ↓
3. IF Accept → Single Skill Check
   - Success Chance = 60% + (relevant_skill / 3)
   - Roll dice, determine outcome instantly
   ↓
4. Show Result Popup Immediately
   - Success: "You complete the order successfully"
   - Failure: "You failed the order"
   ↓
5. Apply Rewards/Penalties Immediately
   - Reputation changes
   - Company Need changes
   - Gold rewards (T4+)
   - XP gained
   ↓
6. Order Complete → Wait 3-5 days for next order
```

### Current Order Definition Structure:
```json
{
  "id": "order_guard_duty",
  "skills": { "Athletics": 30, "Perception": 25 },
  "baseSuccessChance": 60,
  "resolution": {
    "success": {
      "reputation": { "officer": 8 },
      "companyNeeds": { "Readiness": 5 },
      "skillXp": { "Perception": 15 }
    },
    "failure": {
      "reputation": { "officer": -10 },
      "companyNeeds": { "Readiness": -5 }
    }
  }
}
```

---

## What The New System Does

**Files:** 
- `src/Features/Orders/Behaviors/OrderProgressionBehavior.cs` (NEW)
- `src/Features/Orders/Behaviors/OrderManager.cs` (MODIFY - issuance only)

### New Flow:
```
1. Order Issued (orchestrator-coordinated timing)
   ↓
2. Player Sees Popup: [Accept] [Decline]
   ↓
3. IF Accept → Order Starts Progression
   - No immediate resolution
   - Order has duration (2-8 days)
   - 4 phases per day (6am, 12pm, 6pm, 12am)
   ↓
4. Phases Process Automatically
   - Each phase: Update Recent Activity with status text
   - Slot phases: Orchestrator MAY inject event (15-35% base chance)
   - Events have skill checks (not order itself)
   - Consequences accumulate (fatigue, XP, injury)
   ↓
5. Order Completes After All Phases
   - Show completion summary in Recent Activity
   - Apply accumulated rewards
   - Calculate outcome (perfect/adequate/failed) based on events
   ↓
6. Player Off-Duty → Orchestrator fires camp life events
   - Wait for OrderManager to issue next order (orchestrator timing)
```

### New Order Definition Structure:
```json
{
  "id": "order_guard_duty",
  "duration_days": 2,
  "primary_skill": "Perception",
  "fatigue_per_day": 5,
  "injury_risk": "very_low",
  "blocks": [
    { "phase": 0, "type": "routine", "statusId": "guard_start" },
    { "phase": 1, "type": "routine", "statusId": "guard_midday" },
    { "phase": 2, "type": "slot", "statusId": "guard_evening" },
    { "phase": 3, "type": "slot!", "statusId": "guard_night" }
  ],
  "event_pool": [
    "guard_drunk_soldier",
    "guard_strange_noise",
    "guard_officer_inspection"
  ],
  "resolution": {
    "perfect": { "reputation": { "officer": 8 }, "skillXp": { "Perception": 15 } },
    "adequate": { "reputation": { "officer": 3 }, "skillXp": { "Perception": 8 } },
    "failed": { "reputation": { "officer": -10 } }
  }
}
```

---

## Code To Remove

### 1. OrderManager - Resolution Logic (REMOVE)

**Current Methods to Remove:**
```csharp
// REMOVE: Instant skill check resolution
private OrderOutcome ResolveOrder(OrderDefinition order)
{
    float successChance = CalculateSuccessChance(order);
    bool success = MBRandom.RandomFloat < successChance;
    return success ? OrderOutcome.Success : OrderOutcome.Failure;
}

// REMOVE: Immediate reward application
private void ApplyOrderOutcome(OrderDefinition order, OrderOutcome outcome)
{
    var resolution = order.Resolution[outcome];
    EscalationManager.Instance.ModifyReputation(...);
    CompanyNeedsManager.Instance.ModifyNeed(...);
    // etc.
}

// REMOVE: Result popup display
private void ShowOrderResultPopup(OrderDefinition order, OrderOutcome outcome)
{
    // Popup showing success/failure
}
```

**Keep:**
```csharp
// KEEP: Order selection logic (which order to assign)
private OrderDefinition SelectOrderForTier(int tier, List<string> contextTags)

// KEEP: Order issuance popup (accept/decline)
private void IssueOrderPopup(OrderDefinition order)

// MODIFY: Add orchestrator timing check
private void TryIssueOrder()
{
    if (!ContentOrchestrator.Instance?.CanIssueOrderNow() ?? true)
        return;
    
    // Existing selection logic stays
    var order = SelectOrderForTier(...);
    IssueOrderPopup(order);
}
```

---

### 2. Order JSON Schema (CHANGE)

**Fields to Remove:**
- ❌ `baseSuccessChance` - No instant resolution
- ❌ `skills` (as requirement) - Events have skill checks, not orders
- ❌ Immediate `resolution.success` / `resolution.failure` structure

**Fields to Add:**
- ✅ `duration_days` - How long order runs
- ✅ `blocks` - Phase-by-phase progression with types
- ✅ `event_pool` - Events that can fire during order
- ✅ `fatigue_per_day` - Accumulated fatigue
- ✅ `injury_risk` - Chance of injury during duty
- ✅ `resolution.perfect` / `adequate` / `failed` - Based on event outcomes, not skill check

---

### 3. OrderManager Timing (MODIFY)

**Current Timer System (3-5 days):**
```csharp
// MODIFY: Keep timer for issuing, but coordinate with orchestrator
private void OnDailyTick()
{
    _daysSinceLastOrder++;
    
    if (_daysSinceLastOrder >= _nextOrderDay)
    {
        // ADD: Check with orchestrator
        if (ContentOrchestrator.Instance?.CanIssueOrderNow() ?? true)
        {
            TryIssueOrder();
        }
    }
}
```

**New Behavior:**
- Garrison: Check every day, orchestrator throttles (3-5 days)
- Campaign: Check every day, orchestrator allows (1-2 days)
- Siege: Check every day, orchestrator allows (0.5-1 day)

---

### 4. OrderProgressionBehavior (NEW FILE)

**Create:** `src/Features/Orders/Behaviors/OrderProgressionBehavior.cs`

This is an entirely NEW behavior class that handles:
- Phase progression (hourly tick at 6am, 12pm, 6pm, 12am)
- Status text updates to Recent Activity
- Event injection during slot phases
- Consequence accumulation (fatigue, XP, injury)
- Order completion and outcome calculation

---

## Migration Steps

### Phase A: Create New System (Non-Breaking)
1. Create `OrderProgressionBehavior.cs` (initially disabled)
2. Create new order JSON definitions with `blocks` structure
3. Test progression system in isolation
4. Feature flag: `use_order_progression = false` (default)

### Phase B: Switch Over
1. Enable feature flag: `use_order_progression = true`
2. Modify `OrderManager.TryIssueOrder()`:
   - Remove resolution logic
   - Keep issuance popup
   - On accept: Start progression instead of resolve
3. Disable old resolution methods

### Phase C: Clean Up
1. Remove old resolution methods from OrderManager
2. Remove old JSON fields (`baseSuccessChance`, immediate `resolution`)
3. Archive old orders-system.md → `orders-system-legacy.md`
4. Update all references to point to order-progression-system.md

---

## Acceptance Criteria

**Before (Current System):**
- ✅ Accept order → immediate skill check → immediate result popup
- ✅ Orders resolved instantly
- ✅ 3-5 day wait until next order

**After (New System):**
- ✅ Accept order → progression starts
- ✅ 4 phases per day with status updates
- ✅ Events can fire during slot phases
- ✅ Order completes after 2-8 days
- ✅ Next order issued based on orchestrator timing (garrison slow, siege fast)

---

## Related Documentation

- **Current System:** [Orders System (Legacy)](../Features/Core/orders-system.md)
- **New System:** [Order Progression System](order-progression-system.md)
- **Integration:** [Content Orchestrator Plan - Phase 4](content-orchestrator-plan.md#phase-4-orders-integration-week-4)
- **Content:** [Orders Content](orders-content.md), [Order Events Master](order-events-master.md)

---

**Critical Note:** This is a **replacement**, not an addition. The old instant-resolution code must be removed or disabled to prevent conflicts.
