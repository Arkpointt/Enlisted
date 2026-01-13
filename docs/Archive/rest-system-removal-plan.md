# Company Rest System Removal Plan

**Date:** 2026-01-11  
**Status:** Research Complete, Ready for Execution  
**Related:** [morale-system-removal-plan.md](morale-system-removal-plan.md)

## Executive Summary

Company Rest is a redundant metric in the Company Needs system. Player Fatigue (0-24 budget) already handles player stamina for activities. Company Rest does nothing meaningful - it degrades daily, shows in UI, but has no actual gameplay impact. This plan removes it, leaving a 2-need system: **Readiness** and **Supplies**.

## Critical Distinction

**DO NOT CONFUSE:**
- **Player Fatigue** (0-24): Player's personal stamina budget that gates camp decisions and has health penalties. **KEEP THIS - IT WORKS!**
- **Company Rest** (0-100): Company-wide metric that degrades but does nothing. **REMOVE THIS - IT'S USELESS!**

## Research Findings

**Code Impact:** 18 C# files  
**Config Impact:** 10+ JSON/XML files  
**Doc Impact:** Multiple documentation files

**Files with Rest References:**
- `CompanyNeed.cs` - Enum definition
- `CompanyNeedsState.cs` - Rest property
- `CompanyNeedsManager.cs` - Degradation logic (10 references)
- `EnlistmentBehavior.cs` - Serialization (5 references)
- `EnlistedMenuBehavior.cs` - UI display (5 references)
- `CampScheduleManager.cs` - Exhausted override check
- `OrderManager.cs` - Need change messages (5 references)
- `EventDeliveryManager.cs` - Effect application (4 references)
- Plus 10 more files with 1-3 references each

---

## Phase 1: Core System Removal

### 1.1 Update CompanyNeed Enum
**File:** `src/Features/Company/CompanyNeed.cs`

**Current:**
```csharp
public enum CompanyNeed
{
    Readiness,
    Rest,
    Supplies
}
```

**Change to:**
```csharp
/// <summary>
/// The two core needs that must be balanced for company health and effectiveness.
/// Represents the enlisted lord's party needs (readiness, supplies).
/// Each need ranges from 0-100, with thresholds at 30 (Poor) and 20 (Critical).
/// Note: Rest was removed 2026-01-11 (redundant with player fatigue system).
/// </summary>
public enum CompanyNeed
{
    /// <summary>Combat readiness and training level (0-100)</summary>
    Readiness,
    
    /// <summary>Food, water, and supplies (0-100)</summary>
    Supplies
}
```

### 1.2 Update CompanyNeedsState Class
**File:** `src/Features/Company/CompanyNeedsState.cs`

**Changes:**
- Remove `Rest` property (line 19)
- Update `GetOverallHealth()`: change `/ 3` to `/ 2` (line 150)
- Update `HasCriticalNeeds()`: remove `Rest < CriticalThreshold` check (line 120)
- Update `GetNeed()` switch statement: remove Rest case (line 52)
- Update `SetNeed()` switch statement: remove Rest case (line 70)
- Update `GetMostCriticalNeed()`: will naturally exclude Rest after enum change

### 1.3 Update CompanyNeedsManager
**File:** `src/Features/Company/CompanyNeedsManager.cs`

**Line 41:** Remove `var restDegradation = 4;`  
**Line 50:** Remove `restDegradation += 5;` from long march  
**Line 57:** Remove `var oldRest = needs.Rest;`  
**Line 60:** Remove `needs.SetNeed(CompanyNeed.Rest, needs.Rest - restDegradation);`  
**Line 64:** Remove Rest logging  
**Line 91:** Remove `CheckNeedThreshold(needs.Rest, CompanyNeed.Rest, ...)` call  
**Line 142:** Remove `predictions[CompanyNeed.Rest] = 60;` default  
**Line 159:** Remove `predictions[CompanyNeed.Rest] = needsPrediction["Rest"]?.Value<int>() ?? 60;`  
**Line 164:** Remove Rest from debug log  
**Line 171:** Remove `predictions[CompanyNeed.Rest] = 60;` fallback  
**Line 183:** Remove `predictions[CompanyNeed.Rest] = 60;` error fallback  
**Line 250:** Remove Rest case from `GetNeedValue()` extension method

### 1.4 Update EnlistmentBehavior Serialization
**File:** `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`

**Find serialization section** (search for `companyReadiness` or `SyncData` with needs):

**Add backwards compatibility:**
```csharp
// Load old Rest value but discard it (backwards compatibility)
int oldCompanyRest = 60;
dataStore.SyncData("companyRest", ref oldCompanyRest);
// Value loaded but not used - Rest metric removed 2026-01-11
```

**Update initialization** to only set Readiness and Supplies (remove Rest initialization)

---

## Phase 2: Schedule System Cleanup

### 2.1 Remove Exhausted Override from CampScheduleManager
**File:** `src/Features/Camp/CampScheduleManager.cs`

**Lines 449-458:** Delete entire exhausted check block:
```csharp
// DELETE THIS:
var exhausted = pressureOverrides["exhausted"];
if (exhausted != null)
{
    var threshold = exhausted["threshold"]?.Value<int>() ?? 30;
    if (needs.Rest < threshold)
    {
        ApplyPressureEffect(schedule, exhausted);
    }
}
```

### 2.2 Update camp_schedule.json
**File:** `ModuleData/Enlisted/Config/camp_schedule.json`

**Remove from pressureOverrides:**
```json
"exhausted": {
  "threshold": 30,
  "effect": "boost_recovery",
  "description": "Rest becomes priority"
},
```

**Update all skippedWhen arrays** - remove `"exhausted"` entries:
- Dawn slot1: `["exhausted", "siege", "marching"]` → `["siege", "marching"]`
- Dawn slot2: `["exhausted", "marching", "siege", "routine", "quiet"]` → `["marching", "siege", "routine", "quiet"]`
- Night slot2: `["exhausted", "routine", "quiet", "marching", "siege"]` → `["routine", "quiet", "marching", "siege"]`

**Update boostedWhen arrays** - remove `"exhausted"` entries:
- Night slot1: `["exhausted", "marching"]` → `["marching"]`

### 2.3 Check orchestrator_overrides.json
**File:** `ModuleData/Enlisted/Config/orchestrator_overrides.json`

Search for any rest/exhausted overrides. If found, remove them.

---

## Phase 3: UI and Display Cleanup

### 3.1 Update EnlistedMenuBehavior
**File:** `src/Features/Interface/Behaviors/EnlistedMenuBehavior.cs`

**Line 1662-1670:** Remove exhaustion display from Company Status:
```csharp
// DELETE THIS BLOCK:
if (companyNeeds.Rest < 40)
{
    var exhaustedText = new TextObject("{=status_men_exhausted}men exhausted").ToString();
    var fatigueText = new TextObject("{=status_fatigue_showing}fatigue showing").ToString();
    var restPhrase = companyNeeds.Rest < 20 
        ? $"<span style=\"Alert\">{exhaustedText}</span>" 
        : $"<span style=\"Warning\">{fatigueText}</span>";
    needsParts.Add(restPhrase);
}
```

**Line 2488-2499:** Remove Rest from camp narrative:
```csharp
// DELETE THIS BLOCK:
if (companyNeeds.Rest < 20)
{
    detailParts.Add("<span style=\"Alert\">exhaustion</span> weighs on everyone");
}
else if (companyNeeds.Rest < 40 && activityLevel == Content.Models.ActivityLevel.Intense)
{
    detailParts.Add("<span style=\"Warning\">men push through fatigue</span>");
}
else if (companyNeeds.Rest < 40)
{
    detailParts.Add("<span style=\"Warning\">fatigue</span> visible in the ranks");
}
```

**Search for other Rest display code** and remove.

### 3.2 Update MusterMenuHandler
**File:** `src/Features/Enlistment/Behaviors/MusterMenuHandler.cs`

**Line 1272:** Check if Rest is displayed in muster reports. If so, remove it.

### 3.3 Update EnlistedNewsBehavior
**File:** `src/Features/Interface/Behaviors/EnlistedNewsBehavior.cs`

Search for Rest threshold warnings and remove them.

---

## Phase 4: Content System Updates

### 4.1 Update OrderManager
**File:** `src/Features/Orders/Behaviors/OrderManager.cs`

**Lines 1363, 1379, 1393, 1409, 1423:** Remove Rest from `GetCompanyNeedChangeMessage()` switch statements.

Find all occurrences of:
```csharp
case CompanyNeed.Rest:
    // ... message generation
    break;
```

Delete these cases.

### 4.2 Update EventDeliveryManager
**File:** `src/Features/Content/EventDeliveryManager.cs`

**Lines 2376-2377, 2417-2418:** Remove Rest effect application logic.

### 4.3 Update CompanySimulationBehavior
**File:** `src/Features/Camp/CompanySimulationBehavior.cs`

**Lines 342, 345, 577, 610, 775, 802:** Remove any Rest pressure tracking or checks.

### 4.4 Update CampRoutineProcessor
**File:** `src/Features/Camp/CampRoutineProcessor.cs`

**Lines 234, 517:** Check for Rest effects in routine outcomes. Remove if found.

### 4.5 Search JSON Content Files
**Search:** `grep -r "\"Rest\"" ModuleData/Enlisted/Events/ ModuleData/Enlisted/Orders/ ModuleData/Enlisted/Decisions/`

For each occurrence in `companyNeeds` effects:
- Remove `"Rest": X` entries
- Update effect descriptions if they mention rest/exhaustion

**Known files with Rest effects:**
- `incidents_siege.json` (lines 55, 397)
- `incidents_leaving.json` (lines 36, 58)
- `incidents_town.json` (lines 38, 56, 269)
- `camp_decisions.json` (line 535)

---

## Phase 5: Config and Schema Updates

### 5.1 Update strategic_context_config.json
**File:** `ModuleData/Enlisted/Config/strategic_context_config.json`

**Remove `"Rest": X` from all 8 contexts:**
- coordinated_offensive (line 24)
- desperate_defense (line 37)
- raid_operation (line 50)
- siege_operation (line 63)
- patrol_peacetime (line 76)
- garrison_duty (line 89)
- recruitment_drive (line 102)
- winter_camp (line 115)

### 5.2 Update routine_outcomes.json
**File:** `ModuleData/Enlisted/Config/routine_outcomes.json`

**Line 378:** Check for Rest effects. Remove if found.

**IMPORTANT:** DO NOT remove `fatigue` references - those are PLAYER fatigue (0-24 system) which we're keeping!

### 5.3 Update simulation_config.json
**File:** `ModuleData/Enlisted/Config/simulation_config.json`

**Lines 120, 147, 156:** Check for Rest references. Remove if found.

---

## Phase 6: Documentation Updates

### 6.1 Update Core Documentation

**BLUEPRINT.md:**
- Change "3 Company Needs" → "2 Company Needs"
- Update Company Needs line: `Readiness, Supply, Rest` → `Readiness, Supply`

**core-gameplay.md:**
- Remove Rest from Company Needs section
- Clarify: "Company Needs (Readiness, Supply) track company effectiveness. Player Fatigue (0-24) gates your personal activities."

**camp-life-simulation.md:**
- Update header: "Three Company Needs" → "Two Company Needs"
- Remove entire Rest section (if not already removed)
- Update architecture diagrams
- Clarify player fatigue is separate

**camp-routine-schedule-spec.md:**
- Remove all `exhausted` override references (already partially done)
- Update deviation rules table
- Remove Rest from pressure effects

**INDEX.md:**
- Update Company Needs descriptions to show 2 needs

**enlistment.md:**
- Update Company Needs references (already partially done)

### 6.2 Add Deprecation Notice to BLUEPRINT.md

Update the "Deprecated Systems" section (add after morale entry):

```markdown
### Company Rest (Removed 2026-01-11)
**Status:** Fully removed  
**Reason:** Redundant with player fatigue system. Company Rest degraded daily and displayed in UI but had no actual gameplay impact.  
**Distinction:** Player Fatigue (0-24 budget) still exists and works correctly - it gates camp decisions and has health penalties.  
**What was removed:**
- CompanyNeed.Rest enum value
- CompanyNeedsState.Rest property
- All Rest degradation logic
- Rest UI displays
- Rest from strategic predictions
- "Exhausted" schedule override (checked needs.Rest < 30)
- Rest effects from events/orders
```

---

## Phase 7: Validation

### 7.1 Build Test
```bash
dotnet build -c "Enlisted RETAIL" /p:Platform=x64
```

**Expected:** 0 errors (warnings OK)  
**Watch for:** Missing case statements, null reference errors

### 7.2 Content Validation
```bash
python3 Tools/Validation/validate_content.py
```

**Expected:** 0 errors  
**Watch for:** Invalid companyNeeds references in JSON

### 7.3 Grep Verification
```bash
# Should find ZERO results in src/ (except comments):
grep -r "CompanyNeed\.Rest" src/

# Should find ZERO results in ModuleData/ (except backup files):
grep -r "\"Rest\"" ModuleData/Enlisted/Config/
grep -r "\"Rest\"" ModuleData/Enlisted/Events/
grep -r "\"Rest\"" ModuleData/Enlisted/Orders/
```

### 7.4 Save Compatibility Test
1. Load a save with old 3-need system
2. Verify it loads without errors
3. Check that Readiness and Supplies load correctly
4. Verify old Rest value is silently discarded

---

## Backwards Compatibility Strategy

Same pattern as morale removal:

```csharp
// In EnlistmentBehavior.SyncData()
int oldCompanyRest = 60;
dataStore.SyncData("companyRest", ref oldCompanyRest);
// Value loaded from old saves but immediately discarded
// Rest metric removed 2026-01-11 - redundant with player fatigue system
```

Old saves will load successfully. The Rest value loads but is never used.

---

## Final Result

**Before:**
- Company Needs: Readiness, Rest, Supplies (3 metrics)
- Player Fatigue: 0-24 budget

**After:**
- Company Needs: Readiness, Supplies (2 metrics)
- Player Fatigue: 0-24 budget (unchanged)

**Benefits:**
- Simpler system (2 needs instead of 3)
- Less confusion (no more "company rest" vs "player fatigue")
- Less redundancy (Rest did nothing gameplay-wise)
- Cleaner UI (fewer meaningless numbers)

---

## Completion Checklist

### Code Changes
- [ ] CompanyNeed.cs - Remove Rest from enum
- [ ] CompanyNeedsState.cs - Remove Rest property, update methods
- [ ] CompanyNeedsManager.cs - Remove Rest degradation and predictions
- [ ] EnlistmentBehavior.cs - Add backwards compatibility serialization
- [ ] CampScheduleManager.cs - Remove exhausted override check
- [ ] EnlistedMenuBehavior.cs - Remove Rest from UI displays
- [ ] OrderManager.cs - Remove Rest from message switches
- [ ] EventDeliveryManager.cs - Remove Rest effect application
- [ ] All other files - Remove remaining Rest references

### Config Changes
- [ ] camp_schedule.json - Remove exhausted override
- [ ] strategic_context_config.json - Remove Rest from all 8 contexts
- [ ] orchestrator_overrides.json - Check for rest overrides
- [ ] routine_outcomes.json - Remove Rest effects (keep fatigue!)
- [ ] simulation_config.json - Remove Rest references

### JSON Content
- [ ] incidents_siege.json - Remove Rest effects
- [ ] incidents_leaving.json - Remove Rest effects
- [ ] incidents_town.json - Remove Rest effects
- [ ] camp_decisions.json - Remove Rest effects
- [ ] Search all other JSON for Rest in companyNeeds

### Documentation
- [ ] BLUEPRINT.md - Update to 2 needs, add deprecation notice
- [ ] INDEX.md - Update Company Needs descriptions
- [ ] core-gameplay.md - Remove Rest, clarify player fatigue
- [ ] enlistment.md - Update need references
- [ ] camp-life-simulation.md - Update to 2 needs
- [ ] camp-routine-schedule-spec.md - Remove exhausted override
- [ ] All other docs - Update need counts

### Validation
- [ ] Build succeeds (0 errors)
- [ ] Content validator passes (0 errors)
- [ ] Grep finds no Rest references
- [ ] Old saves load correctly

---

**Status:** Ready for execution  
**Estimated effort:** 2-3 hours  
**Risk level:** Low (same pattern as successful morale removal)
