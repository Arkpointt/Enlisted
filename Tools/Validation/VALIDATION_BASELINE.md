# Content Validation Baseline

This document establishes the **expected/acceptable** state of content validation warnings. Use this as a baseline when running `validate_content.py` - warnings listed here are intentional and do not require fixing.

## Current Baseline (as of 2026-01-03)

**Total Events:** 236  
**Errors:** 0 (must always be zero)  
**Warnings:** 31 (all acceptable - see below)  
**Info Messages:** 89 (variants detection, terminal flags, project stats)

### Latest Update (2026-01-03)
**✅ FIXED:** All 268 missing C# TextObject localization strings have been added to enlisted_strings.xml

### Project Structure (Phase 7)

**C# Files:** All files in `src/` must be in `.csproj`  
**Rogue Files:** None expected in root directory  
**GUI Assets:** All in `.csproj` content includes

### Code Quality (Phase 8)

**Sea Context Detection:** Validates `IsCurrentlyAtSea` usage patterns  
**Whitelisted Files:** `NavalNavigationCapabilityPatch.cs` (diagnostic logging only)  
**Recognized Patterns:** Early-return guards, alternative siege properties, state sync

### C# TextObject Localization (Phase 9)

**Purpose:** Scans C# code for `TextObject("{=string_id}...")` patterns and verifies string IDs exist in XML.  
**Whitelisted Prefixes:** `dbg_`, `test_`, `internal_`, `debug_` (debug strings don't need localization)  
**Whitelisted Files:** `DebugToolsBehavior.cs`, `TestBehavior.cs`  
**Current Status:** 268 missing strings identified (technical debt to fix over time)

### Camp Schedule Descriptions (Phase 9.5)

**Purpose:** Validates `camp_schedule.json` has meaningful descriptions for all phases.  
**Current Status:** All phases have valid descriptions.

---

## Acceptable Warnings

### 1. Complex Multi-Option Events (2 warnings)

**Files:**
- `events_pay_tension.json:pay_tension_loot_the_dead` (5 options)
- `events_pay_tension.json:pay_tension_confrontation` (6 options)

**Warning:** `5-6 options only recommended for onboarding/abort events`

**Why Acceptable:**  
These are critical mutiny/pay crisis decision points. The extra options represent nuanced moral choices (loot the dead: ignore, take necessities, take valuables, organize looting, prevent looting). These events model complex ethical dilemmas that require more than 3-4 options.

**Action:** None. This is intentional design.

---

### 2. Config Files (2 warnings)

**Files:**
- `schema_version.json`
- `camp_opportunities.json`

**Warning:** `No events found in file`

**Why Acceptable:**  
These are configuration/reference files, not event definition files:
- `schema_version.json` - Schema version reference and validation metadata
- `camp_opportunities.json` - Opportunity type definitions (not event instances)

**Action:** None. Validator scans all JSON files; these will always trigger this warning.

---

### 3. Event Chain Terminal Flags (2 warnings)

**Warnings:**
- `Flag 'desertion_plot_joined' referenced by 1 event(s) but never set`
- `Flag 'mutiny_failed_arrested' referenced by 1 event(s) but never set`

**Why Acceptable:**  
These are **terminal flags** for multi-event narrative chains. They're set by C# code during event resolution, not by other events:

- `desertion_plot_joined` - Set when player joins desertion conspiracy (triggers arrest chain)
- `mutiny_failed_arrested` - Set when mutiny attempt fails and player is arrested

The validator only sees JSON data, so it can't detect flags set by game logic.

**Action:** None. This is a validator limitation for flags set by C# code.

---

### 4. Code Quality - IsCurrentlyAtSea Warnings (26 warnings)

**Pattern:** `IsCurrentlyAtSea without full settlement/siege guards`

**Files affected:**
- `EnlistmentBehavior.cs` (17 warnings)
- `EnlistedMenuBehavior.cs` (4 warnings)
- `QuartermasterManager.cs` (2 warnings)
- `RaftStateSuppressionPatch.cs` (2 warnings)
- `PlayerIsAtSeaTagCrashFix.cs`, `CampRoutineProcessor.cs`, `EnlistedDialogManager.cs` (1 each)

**Why Acceptable:**  
These are low-priority usages that don't affect content filtering. Categories:

1. **Harmony Patches** (`RaftStateSuppressionPatch`, `PlayerIsAtSeaTagCrashFix`) - Fix native game bugs, raw value intentional
2. **UI Scene Selection** (`EnlistedMenuBehavior`, `EnlistedDialogManager`) - Chooses conversation scene (visual only)
3. **Position/State Calculations** (`EnlistmentBehavior`, `QuartermasterManager`, `CampRoutineProcessor`) - Low-level game mechanics

**Critical paths already fixed:**
- `WorldStateAnalyzer.cs` - ✅ Uses defensive pattern
- `OrderCatalog.cs` - ✅ Uses defensive pattern  
- `DecisionManager.cs` - ✅ Uses defensive pattern
- `EventRequirementChecker.cs` - ✅ Uses defensive pattern
- `CampOpportunityGenerator.cs` - ✅ Uses defensive pattern
- `ContentOrchestrator.cs` - ✅ Uses defensive pattern
- `EventDeliveryManager.cs` - ✅ Uses defensive pattern
- `OrderManager.cs` - ✅ Uses defensive pattern
- `EnlistedNewsBehavior.cs` - ✅ Uses defensive pattern

**Action:** None. These are acceptable technical debt; fixing would require significant refactoring with minimal gameplay benefit.

---

### 5. C# TextObject Missing Strings (0 warnings) ✅ FIXED

**Pattern:** `TextObject string 'X' not found in enlisted_strings.xml`

**Status:** ✅ **RESOLVED** (2026-01-03)

**What was fixed:**  
Phase 9 (added 2026-01-03) discovered 268 missing localization strings across C# files. These were causing fallback text to display instead of proper localized strings - a bug that was completely invisible before Phase 9 existed.

**How it was fixed:**
1. Created `extract_localization_from_cs.py` tool to scan all C# files for TextObject patterns
2. Extracted 857 unique string IDs with their fallback text
3. Merged 263 new strings into `enlisted_strings.xml` (595 were already present)
4. All missing C# TextObject references now have proper XML localization entries

**Tools created:**
- `Tools/Validation/extract_localization_from_cs.py` - Extracts TextObject strings from C# code
- `Tools/Validation/merge_localization.py` - Intelligently merges strings into XML

**Impact:**  
Reduced warnings from 299 to 31 (90% improvement). All remaining warnings are acceptable per baseline.

---

## Info Messages (89 total)

### Code Quality Info (2 messages)

**Patterns:** 
- `Sea context detection: X issue(s) in Y file(s)`
- `C# TextObject scan: X refs in Y files, Z missing, W debug strings skipped`

**Why Acceptable:**  
Informational summaries of Phase 8 and Phase 9 code quality scan results.

---

### Project Structure (2 messages)

**Pattern:** `All X C# files in src/ are in .csproj` and `Project includes: X compiled files, Y content files, Z documentation files`

**Why Acceptable:**  
These are informational summaries confirming proper project structure.

---

### Variants Detection (17 messages)

**Pattern:** `Unknown top-level fields (new feature or typo?): ['variants']`

**Files:** `events_escalation_thresholds.json` (all threshold events)

**Why Acceptable:**  
The `variants` field is a documented feature for multi-intensity event variants. The validator logs this as "info" (not warning) because it's listed in `validation_extensions.json` as an allowed custom field.

---

### Terminal Flags (22 messages)

**Pattern:** `Flag 'X' set by event(s) but never referenced (terminal flag?)`

**Why Acceptable:**  
These flags mark the **end** of event chains (e.g., `loaned_to_lord`, `desertion_tonight`). They don't trigger other events; they're used by game systems or remain as permanent player state markers.

---

## When to Re-Evaluate This Baseline

Re-run validation and update this document if:

1. **Error count > 0** - Critical schema violations introduced
2. **Warning count changes** - New warnings added or old ones fixed
3. **New event files added** - May introduce new patterns
4. **Schema version updated** - Validation rules may change

---

## Quick Validation Check

```bash
# Run validator and compare to baseline
python Tools/Validation/validate_content.py

# Expected output:
# Total Events: 236
# Errors: 0
# Warnings: 31 (all acceptable)
# Info: 89
```

If you see **different numbers**, investigate the diff before assuming it's acceptable.

**Project errors require immediate attention:**
- `C# file not in .csproj` → Add `<Compile Include="..."/>` 
- `File in .csproj does not exist` → Remove entry or restore file
- `Rogue file in root` → Move to `Tools/` or `docs/`

**New C# TextObject strings (if added):**
- `TextObject string 'X' not found` → Run `python Tools/Validation/extract_localization_from_cs.py` and merge

---

## Validation History

| Date | Events | Errors | Warnings | Notes |
|------|--------|--------|----------|-------|
| 2026-01-03 | 236 | 0 | 31 | Fixed all 268 C# TextObject strings - 90% warning reduction |
| 2026-01-03 | 236 | 0 | 299 | Added Phase 9 C# TextObject validation (268 missing strings identified) |
| 2026-01-03 | 236 | 0 | 31 | Added Phase 8 code quality validation (sea context detection) |
| 2026-01-03 | 236 | 0 | 5 | Added Phase 7 project structure validation |
| 2026-01-03 | 237 | 0 | 6 | Baseline established. Schema v1→v2 migration complete |
