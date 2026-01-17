# Morale System Removal Plan

**Summary:** Comprehensive plan to safely remove the morale system from the Enlisted mod without breaking other systems. This document maps all morale dependencies, affected systems, and provides a step-by-step removal strategy.

**Status:** ✅ Completed
**Created:** 2026-01-11
**Last Updated:** 2026-01-16 (Morale removal complete; CrewAI tooling removed)
**Related Docs:** [System Integration Analysis](systems-integration-analysis.md), [Company Needs](../Features/Campaign/camp-life-simulation.md)

> **Archive Note (2026-01-16):** The CrewAI tooling referenced in this document has been removed from the project. References to `Tools/CrewAI/`, `sync_systems_db.py`, and the knowledge database are now historical context only.

---

## 🤖 AI Agent Instructions

This document is designed for both human review and AI agent execution. AI agents implementing this plan should:

1. **Execute phases sequentially** - Do not skip ahead
2. **Mark checkboxes as complete** - Update this document as you progress
3. **Preserve the Retinue system** - See Phase 4 for critical preservation details
4. **Test after each phase** - Run build/validation after major changes
5. **Use exact file paths** - All paths are relative to project root `/home/kyle/projects/Enlisted`
6. **Follow the rollback plan** if any phase fails critically
7. **Update this document** with any issues encountered during execution

**Estimated execution time:** 12-16 hours of focused work

---

## Executive Summary

The morale system is one of four Company Needs (Readiness, Morale, Rest, Supplies) tracked in `CompanyNeedsState`. Analysis reveals:

- **19 integration points** across C# code
- **5 JSON config files** with morale references (2 files total, 5 changes needed)
- **0 morale references** in events/orders/decisions content files ✅
- **Critical finding:** Food quality morale bonuses exist but are **never applied** to actual morale values
- **Retinue system** uses `GetRetinueMoraleModifier()` (affects retinue provisioning quality)
- **Database:** 0 morale dependencies, 2 minor schema documentation fixes

**Recommendation:** Safe to remove. No critical gameplay systems depend on morale actually working. Most references are dormant or cosmetic.

**Verification Complete:**
- ✅ All C# files mapped (40 files, 19 integration points)
- ✅ All JSON content verified (0 morale effects in events/orders/decisions)
- ✅ Database checked (no dependencies, schema comment fix only)
- ✅ Retinue system preservation strategy defined
- ✅ Backwards-compatible save/load strategy defined

---

## Table of Contents

1. [Current State Analysis](#current-state-analysis)
2. [Dependency Map](#dependency-map)
3. [Removal Strategy](#removal-strategy)
4. [Step-by-Step Implementation](#step-by-step-implementation)
5. [Testing Checklist](#testing-checklist)
6. [Rollback Plan](#rollback-plan)

---

## Current State Analysis

### What Morale Is

**Location:** `src/Features/Company/CompanyNeed.cs` (enum value)  
**State Container:** `src/Features/Company/CompanyNeedsState.cs`  
**Manager:** `src/Features/Company/CompanyNeedsManager.cs`

```csharp
public enum CompanyNeed
{
    Readiness,
    Morale,      // ← REMOVING THIS
    Rest,
    Supplies
}
```

### Current Morale Mechanics

1. **Storage:** Integer field `Morale { get; set; } = 60;` (0-100 scale)
2. **Serialization:** Saved/loaded in `EnlistmentBehavior.SerializeCompanyNeeds()`
3. **Daily Degradation:** Base -1/day, accelerated if other conditions poor
4. **Modifiers (NOT APPLIED):**
   - Food quality bonus (0/+2/+4/+8) - exists but disconnected
   - Pay tension penalty - exists but disconnected
   - Retinue provisioning modifier (-10 to +10) - **USED** by retinue system

### Key Finding: Food → Morale Disconnection

**File:** `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs` (lines 10746-10760)

```csharp
/// <summary>
/// Gets the morale bonus from current food quality.
/// Used by camp life behavior to adjust party morale.  // ← CLAIM
/// </summary>
public int GetFoodMoraleBonus()
{
    var quality = CurrentFoodQuality;
    return quality switch
    {
        FoodQualityTier.Supplemental => 2,
        FoodQualityTier.Officer => 4,
        FoodQualityTier.Commander => 8,
        _ => 0
    };
}
```

**Reality:** No evidence this is ever called or applied to CompanyNeedsState.Morale. System analysis confirms this is a "phantom integration" - method exists, but pipeline doesn't connect it.

---

## Dependency Map

### 1. Core System Dependencies

#### ✅ **Safe to Remove** - No Functional Impact

| File | Lines | Usage | Impact |
|------|-------|-------|--------|
| `CompanyNeed.cs` | 13-14 | Enum definition | Remove enum value |
| `CompanyNeedsState.cs` | 18-19, 55, 74-75, 127, 158 | Property + accessors | Remove property, update GetOverallHealth() |
| `CompanyNeedsManager.cs` | 41, 47, 57, 66, 70, 75, 103, 155, 173, 179, 187, 200, 268 | Daily degradation, predictions, checks | Remove morale-specific logic |
| `EnlistmentBehavior.cs` | 1826, 1831, 1840, 1853, 1858, 1869, 1889, 1894 | Serialization | Remove save/load keys |

#### ⚠️ **Requires Careful Handling** - Has Side Effects

| File | Lines | Usage | Impact |
|------|-------|-------|--------|
| `EnlistmentBehavior.cs` | 10702-10760 | `GetFoodMoraleBonus()` | **SAFE TO REMOVE** - Never actually used |
| `EnlistmentBehavior.cs` | 10876-10942 | `GetRetinueMoraleModifier()` | **KEEP** - Used by retinue provisioning UI/logic |

#### 🔴 **DO NOT REMOVE** - Critical Dependencies

| System | File | Reason |
|--------|------|--------|
| **Retinue Provisioning** | `EnlistmentBehavior.cs` | `RetinueProvisioningTier` affects morale modifier (-10 to +10). This is **cosmetic/UI only** but removing would break retinue provisioning display |

### 2. JSON Configuration Dependencies

#### Config Files with Morale References

| File | Lines | Content | Action |
|------|-------|---------|--------|
| `orchestrator_overrides.json` | 97-119 | `low_morale` override trigger + light duty | Remove entire `low_morale` section |
| `routine_outcomes.json` | 26-32, 252-258, 620-626, 768-774 | `lowMorale` outcome weights + `moraleChange` effects | Remove `lowMorale` weight profile, remove all `moraleChange` fields |
| `camp_schedule.json` | 10, 110 | Morale mentioned in comments/descriptions | Update comments only |
| `strategic_context_config.json` | (predicted) | Morale in needs_prediction | Remove morale predictions |

#### Content Files with Morale Effects

| Path | Type | Count | Action |
|------|------|-------|--------|
| `Events/events_retinue.json` | Effects | ~10 instances | Remove `"morale": X` effect entries |
| `Orders/order_events/lead_patrol_events.json` | Effects | ~10 instances | Remove `"morale": X` effect entries |
| `Decisions/*` | Effects | Unknown | Search and remove |

### 3. UI/Display Dependencies

| Component | File | Lines | Usage | Action |
|-----------|------|-------|-------|--------|
| **Company Status Display** | `EnlistedMenuBehavior.cs` | 5899-5957 | Morale status text generation | Remove morale section |
| **Needs Forecasting** | `ForecastGenerator.cs` | 128, 156, 160, 163, 166 | Strategic context morale predictions | Remove morale forecasting |
| **Daily Reports** | `EnlistedNewsBehavior.cs` | 1009 | Company report morale mentions | Remove morale from reports |
| **QM Provisions UI** | `QuartermasterProvisionItemVM.cs` | 73, 165 | Food morale bonus display | Remove morale bonus display |
| **Retinue Provisions UI** | `QuartermasterProvisionsVM.cs` | 204, 449, 453, 455, 466 | Retinue morale modifier display | **KEEP** - Still relevant for provisioning quality tiers |

### 4. Dialog/Conversation Dependencies

| File | Lines | Usage | Action |
|------|------|-------|--------|
| `EnlistedDialogManager.cs` | 2334, 2356, 2375, 2384, 2433-2444, 4205 | QM dialogue morale mentions | Search and update/remove morale-specific dialogue |

### 5. Camp/Content System Dependencies

| System | File | Lines | Impact |
|--------|------|-------|--------|
| **Camp Life** | `CampLifeBehavior.cs` | 33, 54, 71, 105-106, 216, 218-219, 228, 231 | Morale checks/modifications | Remove morale logic |
| **Camp Routine** | `CampRoutineProcessor.cs` | 155, 179, 239, 383, 387, 512 | Routine outcome morale changes | Remove morale applications |
| **Camp Simulation** | `CompanySimulationBehavior.cs` | 450-452, 533, 535, 580, 583, 620, 648, 652, 655, 793, 795, 801, 825-830, 898 | Simulation morale tracking | Remove morale from simulation |
| **Camp Mood** | `CampMood.cs` | 5, 18 | Mood calculation includes morale | Remove morale from mood calc |
| **Opportunity Generator** | `CampOpportunityGenerator.cs` | 1677-1684 | Morale-based opportunity filtering | Remove morale filters |
| **Content Orchestrator** | `ContentOrchestrator.cs` | 596, 1881 | Morale in content triggers | Remove morale triggers |
| **Simulation Pressure** | `SimulationPressureCalculator.cs` | 56-61 | Pressure from morale state | Remove morale pressure |

### 6. Order System Dependencies

| File | Lines | Usage | Action |
|------|------|-------|--------|
| `OrderManager.cs` | 1363-1432 | Order outcome morale changes | Remove morale effects from order outcomes |
| `OrderOutcome.cs` | 34 | Morale field in outcome data | Remove morale field |

### 7. Effect Translation Dependencies

| File | Lines | Usage | Action |
|------|------|-------|--------|
| `IncidentEffectTranslator.cs` | 64, 68-71 | Translates JSON `"morale"` effects to CompanyNeedsState | Remove morale effect translation |
| `EventRequirementChecker.cs` | 891, 906 | Checks morale requirements for events | Remove morale requirement checks |

### 8. Localization Dependencies

| File | Type | Action |
|------|------|--------|
| `enlisted_strings.xml` | Localization strings | Search for morale-related string IDs (e.g., `morale_excellent`, `morale_poor`), mark as deprecated or remove |

---

## Removal Strategy

### Phase 1: Preparation & Analysis ✅ COMPLETE

- [x] Map all morale references in C# code
- [x] Map all morale references in JSON configs  
- [x] Identify critical dependencies (Retinue system)
- [x] Verify food morale bonus is disconnected
- [x] Verify database has no morale dependencies

**Status:** Complete - Analysis confirms safe removal with only retinue quality preservation needed.

### Phase 2: Remove Non-Critical References

**Objective:** Remove morale from systems that don't break gameplay

**Files to modify:**
- `src/Features/Company/CompanyNeed.cs`
- `src/Features/Company/CompanyNeedsState.cs`
- `src/Features/Company/CompanyNeedsManager.cs`
- `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`

#### Step 1: Remove from CompanyNeed Enum

**File:** `src/Features/Company/CompanyNeed.cs`

**Action:** Remove `Morale` enum value (line 14)

```csharp
// BEFORE:
public enum CompanyNeed
{
    Readiness,
    Morale,      // ← DELETE THIS LINE
    Rest,
    Supplies
}

// AFTER:
public enum CompanyNeed
{
    Readiness,
    Rest,
    Supplies
}
```

**Update XML comment (line 5-6):** Change "four core needs" to "three core needs"

#### Step 2: Remove from CompanyNeedsState

**File:** `src/Features/Company/CompanyNeedsState.cs`

**Changes required:**

1. **Delete Morale property** (lines 18-19)
2. **Update GetNeed()** (remove line 55, update switch)
3. **Update SetNeed()** (remove lines 74-76, update switch)
4. **Update GetOverallHealth()** (line 158):
   ```csharp
   // BEFORE:
   return (Readiness + Morale + Rest + Supplies) / 4;
   
   // AFTER:
   return (Readiness + Rest + Supplies) / 3;
   ```
5. **Update HasCriticalNeeds()** (remove line 127):
   ```csharp
   // BEFORE:
   return Readiness < CriticalThreshold ||
          Morale < CriticalThreshold ||      // ← DELETE THIS LINE
          Rest < CriticalThreshold ||
          Supplies < CriticalThreshold;
   ```

#### Step 3: Remove from CompanyNeedsManager

**File:** `src/Features/Company/CompanyNeedsManager.cs`

**Changes required:**

1. **ProcessDailyDegradation()** (lines 40-76):
   - Delete `var moraleDegradation = 1;` (line 41)
   - Delete morale degradation logic (lines 47, 57-61, 66, 70, 75)
   
2. **CheckCriticalNeeds()** (line 103):
   ```csharp
   // DELETE this line:
   CheckNeedThreshold(needs.Morale, CompanyNeed.Morale, criticalThresholdHigh, criticalThresholdLow, warnings);
   ```

3. **PredictUpcomingNeeds()** (lines 155, 173, 179, 187, 200):
   - Remove all morale predictions
   - Delete lines that set `predictions[CompanyNeed.Morale]`

4. **CompanyNeedsStateExtensions.GetNeedValue()** (line 268):
   - Remove `CompanyNeed.Morale => needs.Morale,` case

#### Step 4: Remove Serialization (Backwards Compatible)

**File:** `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`

**Method:** `SerializeCompanyNeeds()` (lines 1816-1903)

**Changes:**

1. **Save section** (lines 1820-1843): Remove morale save
   ```csharp
   // DELETE line 1831:
   SyncKey(dataStore, "_companyNeeds_morale", ref morale);
   
   // DELETE line 1840:
   SyncKey(dataStore, "_companyNeeds_morale", ref defaultValue);
   ```

2. **Load section** (lines 1849-1895): Load but discard old morale
   ```csharp
   // KEEP for backwards compatibility, but don't use the value:
   var morale = 60;  // Line 1853
   SyncKey(dataStore, "_companyNeeds_morale", ref morale);  // Line 1858
   // Don't assign morale to _companyNeeds (delete line 1869)
   
   // Update CompanyNeedsState construction (lines 1866-1872):
   _companyNeeds = new CompanyNeedsState
   {
       Readiness = readiness,
       // DELETE: Morale = morale,
       Rest = rest,
       Supplies = supplies
   };
   ```

**⚠️ CRITICAL:** Keep the `SyncKey` call for "_companyNeeds_morale" during loading to drain old saves, but don't use the value.

### Phase 3: Remove Food Quality Morale Bonus

**File:** `EnlistmentBehavior.cs`

```csharp
// REMOVE these methods (lines 10746-10760):
public int GetFoodMoraleBonus()
public int GetFoodFatigueBonus()  // KEEP - still applies fatigue relief
```

**Update food quality comments:**
- Remove morale bonus claims from FoodQualityTier enum comments
- Update `GetFoodQualityInfo()` to only return fatigue bonus

### Phase 4: Handle Retinue System **CAREFULLY**

**DO NOT REMOVE:** `GetRetinueMoraleModifier()` and `RetinueProvisioningTier`

**Why?** This is part of the retinue provisioning **quality tier system**. The "morale modifier" is cosmetic flavor text showing provisioning quality:
- None = -10 (starvation)
- BareMinimum = -5 (grumbling)
- Standard = 0 (neutral)
- GoodFare = +5 (satisfied)
- OfficerQuality = +10 (high morale)

**Action:**
1. Keep the method and enum
2. Rename for clarity: `GetRetinueMoraleModifier()` → `GetRetinueProvisioningQuality()`
3. Update UI to say "Quality: +10" instead of "Morale: +10"
4. Update documentation to clarify this is provisioning quality, not company morale

### Phase 5: Remove JSON Config References

#### orchestrator_overrides.json
```json
// REMOVE entire section (lines 97-119):
"low_morale": {
  "trigger": { "need": "morale", "threshold": 25, "comparison": "lessThan" },
  "override": { ... },
  ...
}
```

#### routine_outcomes.json
```json
// REMOVE outcomeWeights profile (lines 26-32):
"lowMorale": { ... }

// REMOVE all moraleChange fields from activities:
"social": {
  "moraleChange": { ... }  // DELETE THIS
}
"extended_rest": {
  "moraleChange": { ... }  // DELETE THIS
}
"light_duty": {
  "moraleChange": { ... }  // DELETE THIS
}
```

#### strategic_context_config.json
```json
// REMOVE morale from needs_prediction sections:
"needs_prediction": {
  "Readiness": 60,
  "Supplies": 60,
  "Morale": 60,  // DELETE THIS LINE
  "Rest": 60
}
```

### Phase 6: Remove from Content Files

**Run search-and-replace:**

```bash
# Find all morale effect entries in JSON
grep -r '"morale"' ModuleData/Enlisted/

# Files to update:
# - Events/events_retinue.json
# - Orders/order_events/*.json
# - Decisions/*.json
```

**Action:** Remove all `"morale": X` entries from effects objects.

**Example:**
```json
// BEFORE:
"effects": {
  "readiness": 5,
  "morale": 3,     // REMOVE
  "soldier_rep": 2
}

// AFTER:
"effects": {
  "readiness": 5,
  "soldier_rep": 2
}
```

### Phase 7: Remove UI/Display References

#### EnlistedMenuBehavior.cs (lines 5899-5957)
Remove morale status section from Company Status display.

#### ForecastGenerator.cs (lines 128-166)
Remove morale from needs forecasting.

#### QuartermasterProvisionItemVM.cs (lines 73, 165)
Remove morale bonus display from food items.

#### EnlistedNewsBehavior.cs (line 1009)
Remove morale from company reports.

### Phase 8: Remove from Camp/Content Systems

#### CampLifeBehavior.cs
Remove morale checks and modifications.

#### CampRoutineProcessor.cs
Remove morale change applications from routine outcomes.

#### CompanySimulationBehavior.cs
Remove morale tracking from simulation state.

#### CampMood.cs
Update mood calculation to not include morale.

#### ContentOrchestrator.cs
Remove morale from content triggers.

#### SimulationPressureCalculator.cs
Remove morale pressure calculations.

### Phase 9: Remove from Order System

#### OrderManager.cs
Remove morale effects from order outcomes.

#### OrderOutcome.cs
Remove morale field from data model.

### Phase 10: Remove from Effect Translation

#### IncidentEffectTranslator.cs
Remove morale effect translation case.

#### EventRequirementChecker.cs
Remove morale requirement checks.

### Phase 11: ~~Update Database & Schema~~ (REMOVED)

> **Note:** CrewAI tooling was removed from the project (2026-01-16). The database sync script and schema.sql no longer exist. This phase is no longer applicable.

---

## Step-by-Step Implementation

### Pre-Implementation Checklist

- [ ] Create backup branch: `git checkout -b backup-before-morale-removal`
- [ ] Run full validation: `python Tools/Validation/validate_content.py`
- [ ] Run full build: `dotnet build -c "Enlisted RETAIL" /p:Platform=x64`
- [ ] Document current save file version

### Implementation Order

**Day 1: Core System Removal**
1. [ ] Remove from `CompanyNeed.cs` enum
2. [ ] Remove from `CompanyNeedsState.cs` (keep backwards-compat load)
3. [ ] Remove from `CompanyNeedsManager.cs`
4. [ ] Update `EnlistmentBehavior.cs` serialization (load-only for old saves)
5. [ ] Remove `GetFoodMoraleBonus()` method
6. [ ] Build and fix compilation errors

**Day 2: JSON Config Cleanup**
7. [ ] Update `orchestrator_overrides.json`
8. [ ] Update `routine_outcomes.json`
9. [ ] Update `strategic_context_config.json`
10. [ ] Update `camp_schedule.json` comments
11. [ ] Run validation: `python Tools/Validation/validate_content.py`

**Day 3: Content File Cleanup**
12. [ ] Search and remove morale effects from `Events/`
13. [ ] Search and remove morale effects from `Orders/`
14. [ ] Search and remove morale effects from `Decisions/`
15. [ ] Run validation again

**Day 4: UI/Display Cleanup**
16. [ ] Update `EnlistedMenuBehavior.cs`
17. [ ] Update `ForecastGenerator.cs`
18. [ ] Update `QuartermasterProvisionItemVM.cs`
19. [ ] Update `EnlistedNewsBehavior.cs`
20. [ ] Rename retinue methods for clarity

**Day 5: Camp/Content System Cleanup**
21. [ ] Update `CampLifeBehavior.cs`
22. [ ] Update `CampRoutineProcessor.cs`
23. [ ] Update `CompanySimulationBehavior.cs`
24. [ ] Update `CampMood.cs`
25. [ ] Update `ContentOrchestrator.cs`
26. [ ] Update `SimulationPressureCalculator.cs`

**Day 6: Order/Effect System Cleanup**
27. [ ] Update `OrderManager.cs`
28. [ ] Update `OrderOutcome.cs`
29. [ ] Update `IncidentEffectTranslator.cs`
30. [ ] Update `EventRequirementChecker.cs`

**Day 7: Dialog Cleanup**
31. [x] ~~Update `Tools/Validation/sync_systems_db.py`~~ (CrewAI removed 2026-01-16)
32. [x] ~~Update `Tools/CrewAI/database/schema.sql`~~ (CrewAI removed 2026-01-16)
33. [x] ~~Run database sync~~ (CrewAI removed 2026-01-16)
34. [ ] Search and update `EnlistedDialogManager.cs`

**Day 8: Documentation Cleanup**
35. [ ] Update all documentation files
36. [ ] Update `BLUEPRINT.md`
37. [ ] Update `INDEX.md`
38. [ ] Add to `Enlisted.csproj` if new files created

**Day 9: Final Testing**
39. [ ] Full build test
40. [ ] Validation test
41. [ ] Manual gameplay test (see Testing Checklist)

---

## Testing Checklist

### Build Tests
- [ ] Clean build succeeds: `dotnet build -c "Enlisted RETAIL" /p:Platform=x64`
- [ ] No ReSharper warnings about morale references
- [ ] Validation passes: `python Tools/Validation/validate_content.py`

### Save/Load Tests
- [ ] Load old save with morale data → no errors
- [ ] Create new save → morale not serialized
- [ ] Load new save → no morale references

### Core System Tests
- [ ] Enlist with a lord → company needs initialize (Readiness, Rest, Supplies only)
- [ ] Daily tick → needs degrade properly (no morale errors in logs)
- [ ] Company Status menu → displays 3 needs, no morale section
- [ ] Overall health calculation → averages 3 values correctly

### Food/Provisions Tests
- [ ] Purchase officer provisions → fatigue relief applies, no morale claims
- [ ] Food quality display → shows fatigue bonus only
- [ ] Retinue provisioning → quality tiers display correctly (renamed from "morale")

### Content Tests
- [ ] Events fire → no morale effect errors
- [ ] Orders complete → no morale effect errors
- [ ] Decisions execute → no morale effect errors
- [ ] Camp opportunities → no morale filters breaking

### UI Tests
- [ ] Daily Brief → no morale section
- [ ] Company Reports → no morale mentions
- [ ] Camp Hub → no morale display
- [ ] Quartermaster → food items don't show morale bonus

### Dialog Tests
- [ ] Talk to QM → no morale-specific dialogue bugs
- [ ] NCO conversations → no morale mentions (if removed)

---

## Rollback Plan

### If Critical Errors Occur

**Step 1: Immediate Rollback**
```bash
git checkout development
git reset --hard backup-before-morale-removal
```

**Step 2: Identify Issue**
- Check logs for errors
- Identify which system broke
- Determine if partial removal is viable

**Step 3: Selective Rollback**
If only one system broke:
1. Revert only that file/system
2. Add morale back as a "stub" (no functionality, just satisfies dependencies)
3. Continue with rest of removal

### Stub Implementation (Fallback)

If some system absolutely requires morale:

```csharp
// CompanyNeed.cs
public enum CompanyNeed
{
    Readiness,
    Morale,  // DEPRECATED - Stub only, not used
    Rest,
    Supplies
}

// CompanyNeedsState.cs
/// <summary>DEPRECATED - Stub only, always returns 60</summary>
public int Morale { get => 60; set { /* no-op */ } }
```

This allows code to compile without actually tracking morale.

---

## Documentation Updates Required

### Files to Update

1. **BLUEPRINT.md**
   - Remove morale from Company Needs section
   - Update "Key Concepts" to list 3 needs instead of 4

2. **INDEX.md**
   - Update system inventory
   - Remove morale from feature descriptions

3. **docs/Features/Campaign/camp-life-simulation.md**
   - Update Company Needs section
   - Remove morale simulation details

4. **docs/Features/Core/core-gameplay.md**
   - Update Company Needs to 3 values
   - Remove morale descriptions

5. **docs/Features/Equipment/quartermaster-system.md**
   - Remove morale bonus claims from food/provisions

6. **docs/Features/Core/retinue-system.md**
   - Update retinue provisioning to use "quality" instead of "morale"

7. **ModuleData/WARP.md**
   - Update critical rules if morale mentioned

8. **WARP.md (root)**
   - Update system overview

9. **Tools/Validation/sync_systems_db.py**
   - Remove morale keyword from responsibility mapping (line 205)

10. **Tools/CrewAI/database/schema.sql**
   - Remove "morale" from balance_values category comment (line 54)

### New Documentation

Create: `docs/ANEWFEATURE/morale-removal-changelog.md`

Document:
- Why morale was removed
- What replaced it (nothing - simplified to 3 needs)
- How saves are handled
- Breaking changes for mod users

---

## Risk Assessment

### Low Risk ✅

- Core morale system removal
- Food morale bonus removal (never worked)
- JSON config cleanup
- UI display removal
- Save/load backwards compatibility (load-only old keys)

### Medium Risk ⚠️

- Camp simulation morale references (many files)
- Content orchestrator morale triggers
- Dialog system morale mentions
- Effect translation removal

### High Risk 🔴

- **NONE** - No critical dependencies found

### Critical Dependencies 🚨

- **Retinue Provisioning Quality** - Must rename, not remove

---

## Success Criteria

### Completion Checklist

- [ ] All C# morale references removed (except retinue quality)
- [ ] All JSON morale references removed
- [ ] All UI morale displays removed
- [ ] Builds without errors
- [ ] Validation passes
- [ ] Old saves load without errors
- [ ] New saves don't contain morale data
- [ ] Documentation updated
- [ ] Testing checklist completed
- [ ] No morale-related errors in logs

### Performance Metrics

- **Lines of code removed:** ~500-800 estimated
- **Files modified:** ~40 C# files, ~10 JSON files
- **Save file size reduction:** ~50-100 bytes per save
- **Compilation time:** No change expected
- **Runtime performance:** Negligible improvement (one less need to track)

---

## Content Files Verification ✅

**Complete grep search performed on all JSON content files:**

### Results:
- **Total morale references in content:** 2 instances (both in config files)
- **Events:** 0 morale references found
- **Orders:** 0 morale references found  
- **Decisions:** 0 morale references found
- **Config files:** 2 references in `orchestrator_overrides.json` (documented in Phase 5)
- **Routine outcomes:** 3 `moraleChange` fields in `routine_outcomes.json` (documented in Phase 5)

**Verification command used:**
```bash
grep -r "morale" ModuleData/Enlisted/ --include="*.json"
```

**Conclusion:** Content files are clean - only config file changes needed (already documented).

---

## Database Considerations

### Current Database State

**Database:** `Tools/CrewAI/database/enlisted_knowledge.db`

**Tables Checked:**
- `game_systems` - Empty (0 rows)
- `system_dependencies` - Contains 15 behavioral dependencies, **no morale references found**
- `core_systems` - Contains 40 systems including `CompanyNeedsManager`, **no morale-specific systems**

### Schema.sql Updates

**File:** `Tools/CrewAI/database/schema.sql`

**Issue Found:** Line 54 lists "morale" as a balance_values category in SQL comment.

**Action:** Remove "morale" from the category comment (cosmetic documentation fix only - no actual data uses this category).

### Database Sync Script

**File:** `Tools/Validation/sync_systems_db.py`

**Issue Found:** Line 205 contains keyword mapping:
```python
"morale": "morale tracking",
```

This tags any system mentioning "morale" in its summary with "morale tracking" responsibility. After morale removal, this keyword should be deleted to prevent false positives.

**Action Required:**
1. Remove line 205 from `sync_systems_db.py`
2. Run sync after code changes: `python Tools/Validation/sync_systems_db.py`
3. This regenerates `schema.sql` with updated system descriptions

**Database Impact:** Minimal - only affects auto-generated system descriptions in the knowledge base. No manual database queries or updates needed.

---

## Conclusion

The morale system is safe to remove. Analysis confirms:

1. **No database dependencies** - System dependencies table has no morale references
2. **Cosmetic integration** - Most references are UI displays
3. **Disconnected mechanics** - Food bonus never applied
4. **Stub-like existence** - Mentioned but not actively used

**Only critical action:** Preserve retinue provisioning quality system and rename it for clarity.

**Estimated Effort:** 2-3 days full-time work, 1 day testing.

**Risk Level:** Low - No gameplay-breaking dependencies found.

---

**Next Steps:**
1. Review this plan
2. Get user approval
3. Create backup branch
4. Execute Phase 2 onwards
5. Test thoroughly
6. Commit and document changes
