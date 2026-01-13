# Readiness System Analysis

**Date:** 2026-01-11  
**Status:** Active Investigation  
**Purpose:** Comprehensive analysis of Readiness metric to inform removal or enhancement decision

---

## Executive Summary

**Readiness is currently a cosmetic/display-only metric with no mechanical gameplay impact.**

Despite documentation claiming it affects "combat effectiveness," "order success rates," and "reputation gains," code analysis reveals these claims are false. Readiness:
- ✅ Degrades daily and from orders/events
- ✅ Displays in UI with color-coded status
- ✅ Generates news messages when changed significantly
- ❌ Does NOT affect combat effectiveness
- ❌ Does NOT modify order success rates
- ❌ Does NOT impact reputation gains
- ❌ Does NOT gate any gameplay decisions

**Recommendation:** Either implement actual mechanics or remove as redundant with Supplies metric.

---

## Current Implementation

### What Readiness Actually Does

#### 1. Daily Degradation (CompanyNeedsManager.cs)
```csharp
// Base degradation: -2 per day
var readinessDegradation = 2;

// Accelerated degradation from long marches
if (isOnLongMarch)
{
    readinessDegradation += 5; // Total: -7 per day
}

needs.SetNeed(CompanyNeed.Readiness, needs.Readiness - readinessDegradation);
```

**Effect:** Readiness naturally declines 2 points daily, 7 points when army is marching without settlement.

#### 2. Modified by Orders and Events

**15 Orders Affect Readiness:**

**T1-T3 Orders (4 total):**
- `order_guard_duty`: +6 success, -8 fail
- `order_camp_patrol`: +5 success, -6 fail
- `order_equipment_check`: +5 success, -6 fail
- `order_sentry`: +6 success, -8 fail

**T4-T6 Orders (4 total):**
- `order_scout_route`: +10 success, -10 fail, -15 critical
- `order_repair_equipment`: +12 success, -10 fail
- `order_lead_patrol`: +10 success (also +6 Morale)
- `order_inspect_defenses`: +14 success, -10 fail

**T7-T9 Orders (5 total):**
- `order_command_squad`: +15 success, -12 fail, -25 critical
- `order_strategic_planning`: +18 success, -15 fail
- `order_interrogate`: +12 success, -8 fail
- `order_inspect_readiness`: +20 success, -15 fail

**2 Events Affect Readiness:**
- `mi_siege_assault_prep` (incidents_siege.json:349): "prepare equipment" option gives +5 Readiness
- `supply_pressure_stage_1_cmd` (pressure_arc_events.json:126): "send foraging parties" option gives -2 Readiness

#### 3. UI Display and Status Text (EnlistedMenuBehavior.cs:2587-2610)

```csharp
private string GetReadinessPhrase(int value)
{
    return value switch
    {
        >= 80 => "Combat Ready",      // Excellent
        >= 60 => "Prepared",           // Good
        >= 40 => "Adequate",           // Fair
        >= 30 => "Sluggish",           // Poor
        _     => "Unprepared"          // Critical
    };
}
```

Displays as:
- "Readiness: 85 (Combat Ready)" ← Green
- "Readiness: 45 (Adequate)" ← Yellow
- "Readiness: 22 (Unprepared)" ← Red

#### 4. News Messages (OrderManager.cs:1319-1421)

**Triggers news when:**
- Change ≥ ±10 points
- Crosses 30% threshold (up or down)
- Crosses 80% threshold (up)

**Message Examples:**
- `< 30`: "Unit readiness critical - Combat effectiveness severely reduced"
- `+15 or more`: "Unit readiness greatly improved"
- `-15 or more`: "Unit readiness declining sharply"

#### 5. Strategic Context Predictions (CompanyNeedsManager.cs:126-177)

Loaded from `strategic_context_config.json`:
- Grand Campaign: Predicts 85 Readiness needed
- Last Stand: Predicts 90 Readiness needed
- Winter Camp: Predicts 55 Readiness needed

**Purpose:** Display-only forecasting shown in Camp Hub "Upcoming" section.

#### 6. Critical Need Warnings (OrderManager.cs:867-876)

When Readiness < 30, displays red popup warning:
> "Warning: Readiness is low (28%). Address soon."

When Readiness < 20, displays critical warning:
> "Warning: Readiness is CRITICAL (18%)! Immediate action required."

---

### What Readiness Does NOT Do

#### ❌ No Combat Effectiveness Impact

**Searched:** All combat code (`src/Features/Combat/`)  
**Result:** Zero references to Readiness

**Claims in Documentation:**
- camp-life-simulation.md:98: "Unit combat effectiveness"
- camp-life-simulation.md:589: "High Readiness (80+): +10% combat effectiveness"
- content-effects-reference.md:270: "Battle effectiveness"

**Reality:** No code applies combat bonuses based on Readiness. Battle AI plan documents (11806-11819) use `health * morale / 100` as "readiness proxy" for unit rotation, but this is NOT the Company Readiness metric.

#### ❌ No Order Success Rate Modification

**Order Success Formula (OrderManager.cs:668-746):**
```csharp
var successChance = 0.6f; // Base 60%

// Modified by player skill vs requirements
// +1% per skill point above, -2% per point below

// Modified by player traits vs requirements
// +2% per trait level above, -3% per level below

// Clamped to 10-95% range
successChance = MathF.Clamp(successChance, 0.1f, 0.95f);
```

**Conclusion:** Success rate depends ONLY on player skills/traits. Readiness is never checked.

**Documentation Claims:**
- camp-life-simulation.md:107: "Reduced order success chance"
- camp-life-simulation.md:135: "Bonuses to order success chance (+10%)"

**Reality:** False. Order success is purely skill-based (SkillCheckHelper.cs).

#### ❌ No Reputation Gain Modification

**Reputation Application (OrderManager.cs:803-822):**
```csharp
if (outcome.Reputation.TryGetValue("lord", out var lordRep))
{
    escalation.ModifyLordReputation(lordRep); // Direct value, no multiplier
}
// Same for officer and soldier reputation
```

**Conclusion:** Reputation changes are applied as flat values from order outcomes. No Readiness multiplier exists.

**Documentation Claims:**
- camp-life-simulation.md:108: "Lower reputation gains"
- camp-life-simulation.md:135: "Reputation gains (+20%)"

**Reality:** False. No code applies Readiness-based reputation modifiers.

#### ❌ No Event Filtering

**Event Delivery (EventDeliveryManager.cs):**
- No Readiness requirements checks
- ContentOrchestrator.cs allows need-based overrides but none configured for Readiness in `orchestrator_overrides.json`

#### ❌ No Gear Restrictions

**Quartermaster Access:**
- Only Supplies < 30 restricts high-tier gear
- Readiness has no effect on equipment availability

#### ❌ No Battle Performance Impact

**Post-Battle Effects:**
- Documentation claims (camp-life-simulation.md:593): "MoraleShock spikes, Readiness may change"
- Reality: MoraleShock is CampLifeBehavior metric (backend). Readiness itself doesn't change from battles unless manually set by event.

---

## Code Evidence Summary

### Files Implementing Readiness

| File | Lines | Purpose |
|------|-------|---------|
| `CompanyNeed.cs` | 12 | Enum definition |
| `CompanyNeedsState.cs` | 17, 49, 64-65, 113, 123, 143 | Property storage, getters/setters |
| `CompanyNeedsManager.cs` | 41-60, 86, 135-172 | Daily degradation, critical checks, predictions |
| `EnlistedMenuBehavior.cs` | 1651, 1656, 2599 | UI display phrases |
| `OrderManager.cs` | 824-877, 1316-1421 | Order outcome effects, news messages |
| `ContentOrchestrator.cs` | 1880 | Need value lookup (unused in practice) |
| `EnlistmentBehavior.cs` | 1826, 1863 | Serialization (save/load) |

**Total Code References:** ~30 locations  
**Functional Impact:** Display, logging, warnings only

### Files NOT Using Readiness

- **Combat System:** 0 references in `/Features/Combat/`
- **Order Success:** SkillCheckHelper.cs uses only skills/traits
- **Reputation:** EscalationManager.cs applies flat values
- **Events:** No Readiness requirements in event filters
- **Quartermaster:** Only Supplies affects gear access

---

## Content Analysis

### Orders Modifying Readiness

**Distribution by Tier:**
- T1-T3: 4 orders (guard duty, patrols, equipment)
- T4-T6: 4 orders (scouting, repairs, inspections)
- T7-T9: 5 orders (command, planning, interrogation)

**Total:** 15/45+ orders (33%) affect Readiness

**Pattern:** Military/tactical orders increase Readiness on success, decrease on failure. Training/combat preparation theme.

### Events Modifying Readiness

**Total:** 2 events
- Siege prep: +5 (prepare equipment carefully)
- Supply crisis: -2 (send foraging parties)

**Observation:** Very few events interact with Readiness compared to other metrics.

---

## Documentation Discrepancies

### False Claims in Documentation

**File:** `docs/Features/Campaign/camp-life-simulation.md`

**Line 98:** "Unit combat effectiveness, training level, tactical sharpness"  
**Reality:** No combat code references Readiness

**Lines 106-109:** "Impact when low (<30): Reduced order success chance, Lower reputation gains, Risk of poor battle performance"  
**Reality:** None of these mechanics exist in code

**Line 135:** "Excellent (80-100): Bonuses to order success chance (+10%), reputation gains (+20%)"  
**Reality:** No multipliers applied anywhere

**Line 589:** "High Readiness (80+): +10% combat effectiveness"  
**Reality:** No pre-battle bonuses based on Readiness

---

**File:** `docs/ANEWFEATURE/content-effects-reference.md`

**Line 270:** "Battle effectiveness"  
**Reality:** No battle system integration

---

### Accurate Documentation

**Lines 100-104 (camp-life-simulation.md):** Daily degradation rates - ✅ Correct  
**Lines 135-144 (camp-life-simulation.md):** Threshold checks and warnings - ✅ Correct  
**Lines 397-407 (camp-life-simulation.md):** Daily degradation processing - ✅ Correct

---

## Historical Context

### Why Readiness Was Created

**Original Intent (inferred from docs):**
1. Company-wide metric parallel to Player Fatigue
2. Represent tactical preparedness and training
3. Affect combat performance and order outcomes
4. Create meaningful choice in activity selection

**What Actually Shipped:**
- UI display and messaging system
- Order/event effects (input side)
- No gameplay mechanics (output side)

**Similar System:** Company Morale (removed earlier)
- Also had no mechanical impact
- Also displayed in UI with color-coding
- Also modified by orders/events
- Removed as redundant

**Related System:** Company Rest (removed 2026-01-11)
- Had minimal mechanical impact
- Was redundant with Player Fatigue
- Removed successfully with no gameplay loss

---

## Decision Framework

### Option 1: Remove Readiness Entirely

**Pros:**
- Eliminates false documentation claims
- Simplifies Company Needs to single metric (Supplies)
- Reduces cognitive load (one less number to track)
- Consistent with Morale/Rest removals
- No gameplay value lost (it does nothing mechanically)

**Cons:**
- Removes thematic flavor ("are we ready for battle?")
- Loses 15 order effects (would need reassignment)
- Removes UI variety (color-coded status)
- Strategic predictions become simpler

**Effort:** Low (similar to Rest removal)
- Remove enum value
- Update 30 code references
- Reassign order/event effects to Supplies or remove
- Update 10+ documentation files
- Add backwards compatibility for saves

---

### Option 2: Implement Actual Mechanics

#### 2A: Combat Effectiveness Modifier

**Implementation:**
```csharp
// In pre-battle setup
var readinessBonus = enlistment.CompanyNeeds.Readiness >= 80 ? 1.1f :
                     enlistment.CompanyNeeds.Readiness < 30 ? 0.9f : 1.0f;

// Apply to player's formation morale or damage output
playerFormation.ApplyMoraleModifier(readinessBonus);
```

**Impact:**
- High Readiness (80+): +10% formation morale/damage
- Low Readiness (<30): -10% formation morale/damage
- Creates actual mechanical reason to maintain Readiness

**Pros:**
- Makes documentation claims true
- Rewards preparation
- Punishes negligence

**Cons:**
- Player has limited control (enlisted, can't choose battles)
- Adds complexity to combat calculations
- May feel unfair if lord picks bad fights

---

#### 2B: Order Success Rate Modifier

**Implementation:**
```csharp
// In OrderManager.EvaluateOrderResult()
var successChance = 0.6f;

// ... existing skill/trait modifiers ...

// Add Readiness modifier
if (enlistment.CompanyNeeds.Readiness >= 80)
{
    successChance += 0.1f; // +10% bonus
}
else if (enlistment.CompanyNeeds.Readiness < 30)
{
    successChance -= 0.1f; // -10% penalty
}
```

**Impact:**
- Excellent Readiness: Easier order success
- Poor Readiness: Harder order success
- Creates feedback loop (low Readiness → fail orders → lower Readiness)

**Pros:**
- Rewards maintaining Readiness
- Makes Readiness meaningful for daily gameplay
- Simple to implement

**Cons:**
- May create negative feedback spiral
- Overlaps with skill-based success (is unit ready OR is player skilled?)
- Could feel punishing

---

#### 2C: Strategic Context Gating

**Implementation:**
```csharp
// In event/order delivery
if (strategicContext == "Grand Campaign" && readiness < 70)
{
    // Restrict high-tier tactical orders
    // Increase risk of "unprepared unit" events
}
```

**Impact:**
- Low Readiness during offensive campaigns triggers warnings/events
- High Readiness unlocks elite orders
- Contextual rather than universal penalty

**Pros:**
- Feels natural (offensive needs preparation)
- Avoids always-on penalties
- Creates prep phase gameplay

**Cons:**
- Complex to implement (many contexts)
- Still limited player control
- May not fire often enough to matter

---

#### 2D: Reputation Gain Multiplier

**Implementation:**
```csharp
// In OrderManager.ApplyOrderOutcome()
var reputationMultiplier = readiness >= 80 ? 1.2f :
                           readiness < 30 ? 0.8f : 1.0f;

lordRep = (int)(lordRep * reputationMultiplier);
```

**Impact:**
- Well-prepared unit: +20% reputation from orders
- Unprepared unit: -20% reputation from orders

**Pros:**
- Indirect benefit (faster rank progression)
- Doesn't affect combat directly
- Rewards preparation

**Cons:**
- Invisible mechanic (hard to communicate)
- May not feel impactful enough
- Overlaps with skill-based reputation gains

---

### Option 3: Convert to Morale/Cohesion Metric

**Concept:** Rename Readiness → Morale, make it represent unit cohesion instead of tactical prep

**Implementation:**
- Keep existing degradation/order effects
- Add new effects: social events affect Morale, combat defeats reduce it
- Mechanical impact: Low Morale (<30) increases desertion risk, reduces soldier reputation gains

**Pros:**
- More thematic (morale matters in military)
- Clear impact (desertion is concrete)
- Different from Supplies (not just resources)

**Cons:**
- Company Morale was already removed (would bring it back)
- Still overlaps with Soldier Reputation metric
- May confuse players (wasn't Morale removed?)

---

### Option 4: Status Quo (Do Nothing)

**Keep Readiness as display-only metric**

**Pros:**
- Zero effort
- No risk of breaking existing content
- Players may not notice it's cosmetic

**Cons:**
- Documentation lies to players
- Wastes development resources (maintained for no reason)
- Creates false choices (why maintain Readiness if it does nothing?)
- Inconsistent with removal of similar systems (Morale, Rest)

---

## Comparison with Other Metrics

| Metric | Mechanical Impact | UI Display | Order Effects | Event Effects | Gating |
|--------|-------------------|------------|---------------|---------------|--------|
| **Readiness** | ❌ None | ✅ Yes | ✅ 15 orders | ✅ 2 events | ❌ No |
| **Supplies** | ✅ Gear restrictions | ✅ Yes | ✅ ~10 orders | ✅ Many events | ✅ QM access |
| **Morale** (removed) | ❌ None | ✅ Yes | ✅ Many | ✅ Many | ❌ No |
| **Rest** (removed) | ⚠️ Minimal | ✅ Yes | ✅ Many | ✅ Many | ❌ No |
| **Player Fatigue** | ✅ Decision gating | ✅ Yes | ✅ All orders | ✅ Some events | ✅ Camp decisions |

**Pattern:** Metrics without mechanical impact were removed (Morale, Rest). Readiness follows same pattern.

---

## Technical Debt

### If Keeping Readiness

**Must Fix:**
1. Update documentation to remove false claims (10+ files)
2. Clarify it's display-only in player-facing docs
3. Consider adding actual mechanics (Options 2A-2D)

**Estimated Effort:** 4-6 hours (docs only) or 2-3 days (implement mechanics)

---

### If Removing Readiness

**Must Do:**
1. Remove enum value (`CompanyNeed.Readiness`)
2. Update ~30 code references
3. Reassign 15 order effects (→ Supplies or remove)
4. Reassign 2 event effects
5. Update 10+ documentation files
6. Add backwards compatibility (load old value, discard)
7. Remove from UI displays
8. Remove from strategic predictions
9. Update content validator schemas

**Estimated Effort:** 1-2 days (based on Rest removal experience)

---

## Recommendations

### Primary Recommendation: Remove Readiness

**Rationale:**
1. **No Gameplay Value:** It does nothing mechanically. Removing it loses zero gameplay.
2. **Precedent:** Morale and Rest were removed for similar reasons. Be consistent.
3. **False Documentation:** Keeping it means maintaining lies to players about its impact.
4. **Cognitive Load:** One less number for players to track with no benefit.
5. **Simplicity:** Company Needs becomes single-metric (Supplies), easier to understand.

**Migration Path:**
- Reassign order effects: Training orders → Player skill XP (already have this), Tactical orders → Supplies (makes sense - prepared = well-supplied)
- Remove or reassign event effects (2 events won't miss it)
- Supplies becomes sole Company Need alongside Player Fatigue (personal stamina budget)

**Result:** Simpler, more honest system with same gameplay depth.

---

### Alternative: Implement Combat Effectiveness Modifier (Option 2A)

**If you want to keep Readiness:**

**Only justification:** You want a metric that specifically represents "are we tactically ready for battle?" separate from "do we have enough food/supplies?"

**Minimum Implementation:**
```csharp
// Pre-battle morale modifier
var readinessModifier = enlistment.CompanyNeeds.Readiness switch
{
    >= 80 => 1.1f,  // +10% morale/damage
    < 30 => 0.9f,   // -10% morale/damage
    _ => 1.0f
};
playerFormation.ApplyMoraleModifier(readinessModifier);
```

**Also add:** Order success rate modifier (Option 2B) to make daily gameplay meaningful.

**Documentation:** Remove false claims about reputation gains and battle performance. Add honest claims about what it actually does now.

**Effort:** 1-2 days implementation + testing

---

## Testing Recommendations

### If Implementing Mechanics

**Test Cases:**
1. High Readiness (85) in battle → Verify morale bonus applied
2. Low Readiness (25) in battle → Verify morale penalty applied
3. High Readiness (85) order success → Verify +10% success chance
4. Low Readiness (25) order success → Verify -10% penalty
5. Strategic context transitions → Verify Readiness impacts change

**Validation:**
- Combat log shows morale modifiers
- Order success rates match expected probabilities
- Player can observe impact of maintaining Readiness

---

### If Removing

**Test Cases:**
1. Old save with Readiness value loads correctly
2. New save doesn't serialize Readiness
3. UI shows only Supplies in Company Needs
4. Orders with old Readiness effects work (reassigned)
5. Strategic predictions still generate (Supplies only)
6. Content validator passes (updated schemas)

**Validation:**
- Build succeeds (0 errors)
- Content validator: 0 errors
- Grep: 0 Readiness references remain
- Old saves compatible (backwards compat)

---

## Next Steps

1. **User Decision:** Remove or implement mechanics?
2. **If Remove:** Follow Rest removal pattern, create removal plan
3. **If Implement:** Choose mechanic (combat effectiveness + order success recommended), prototype, test
4. **Documentation:** Update all files to match reality
5. **Archive This Document:** Move to `/docs/Archive/` once decision made

---

## Files to Update (If Removing)

### Code (9 files)
- `src/Features/Company/CompanyNeed.cs`
- `src/Features/Company/CompanyNeedsState.cs`
- `src/Features/Company/CompanyNeedsManager.cs`
- `src/Features/Interface/Behaviors/EnlistedMenuBehavior.cs`
- `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`
- `src/Features/Orders/Behaviors/OrderManager.cs`
- `src/Features/Content/ContentOrchestrator.cs`
- `src/Features/Content/EventDefinition.cs` (if used)
- `src/Features/Orders/Models/OrderOutcome.cs` (comment update)

### Content (3+ files)
- `ModuleData/Enlisted/Orders/orders_t1_t3.json` (4 orders)
- `ModuleData/Enlisted/Orders/orders_t4_t6.json` (4 orders)
- `ModuleData/Enlisted/Orders/orders_t7_t9.json` (5 orders)
- `ModuleData/Enlisted/Events/incidents_siege.json` (1 event)
- `ModuleData/Enlisted/Events/pressure_arc_events.json` (1 event)
- `ModuleData/Enlisted/Config/strategic_context_config.json` (predictions)

### Documentation (10+ files)
- `docs/BLUEPRINT.md`
- `docs/INDEX.md`
- `docs/Features/Core/core-gameplay.md`
- `docs/Features/Core/enlistment.md`
- `docs/Features/Campaign/camp-life-simulation.md`
- `docs/ANEWFEATURE/content-effects-reference.md`
- `docs/Features/Content/event-system-schemas.md`
- `docs/Features/Content/orders-content.md`
- `docs/Features/Content/writing-style-guide.md`
- And others (grep will find all)

---

## Conclusion

**Readiness is a vestigial system.** Like Morale and Rest before it, Readiness has:
- UI presence ✅
- Order/event effects ✅
- Documentation claims ✅
- **Actual gameplay mechanics ❌**

**Decision Time:** Either make it real or remove it. Keeping it as-is wastes everyone's time and misleads players.

My vote: **Remove it.** Simplify to Supplies + Player Fatigue. Clean, honest, functional.
