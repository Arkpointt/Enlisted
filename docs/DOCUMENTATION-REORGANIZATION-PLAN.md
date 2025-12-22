# Documentation Reorganization - Complete Plan & Execution Guide

**Created:** December 22, 2025  
**Purpose:** Single comprehensive plan to reorganize all Enlisted mod documentation for clarity, usability, and AI/human accessibility

---

## Summary

This plan reorganizes 53+ documentation files to eliminate outdated planning docs, consolidate duplicates, and create a clear, professional structure. The reorganization removes ~15 files, consolidates ~10 more, and establishes a logical hierarchy with Features, Content, and Reference folders. All documents will have summaries and indexes. The Blueprint will become the definitive guide to project architecture and coding standards.

**Key Changes:**
- Delete 5-6 outdated implementation plans (13,546 lines of old planning)
- Consolidate 6 duplicate docs into 3 well-organized docs
- Create 4 new folder categories (Identity, Combat, Content, Reference)
- Rename StoryBlocks → Content (clearer purpose)
- Enhance Blueprint with all coding standards and project conventions
- Ensure every doc has a summary and index at the top

**Estimated Effort:** ~20 hours across 10 phases

---

## Index

1. [Current Problems](#current-problems)
2. [Desired End State](#desired-end-state)
3. [Complete File Inventory](#complete-file-inventory)
4. [Blueprint Enhancement Plan](#blueprint-enhancement-plan)
5. [Documentation Standards](#documentation-standards)
6. [Proposed New Structure](#proposed-new-structure)
7. [Key Decisions & Rationale](#key-decisions--rationale)
8. [Phase-by-Phase Execution Plan](#phase-by-phase-execution-plan)
9. [File Disposition Matrix](#file-disposition-matrix)
10. [Success Criteria](#success-criteria)
11. [Backup & Safety Procedures](#backup--safety-procedures)

---

## Current Problems

### 1. Implementation Plans Folder Contains Outdated Docs

**Issue:** 8 files (13,546 lines) labeled as "plans" but most are completed features
- `macro-schedule-simplification.md` - Status: "✅ COMPLETE - systems deleted"
- `traits-identity-system.md` - Status: "✅ COMPLETE - Code implemented"
- `phase8`, `phase9`, `phase10` - Mix of completed and outdated content
- Planning language ("Phase X will...") persists in docs describing current features

**Impact:** Confuses readers about what's implemented vs. planned

---

### 2. Root Documentation is Duplicated

**Issue:** 3 files with 80% overlapping content
- `blueprint.md` (182 lines) - Technical reference
- `CONTRIBUTING.md` (155 lines) - Development guide
- `modderreadme.md` (239 lines) - Developer guide + API reference

**Impact:** Maintenance burden, inconsistent information, unclear which to follow

---

### 3. Missing Summaries and Indexes

**Issue:** Many docs lack:
- Brief summary at the top explaining purpose
- Table of contents for navigation
- Professional organization

**Impact:** Hard to quickly understand document purpose and navigate content

---

### 4. Blueprint Lacks Coding Standards

**Issue:** Blueprint is currently just technical architecture
- Missing coding preferences (ReSharper, comment style, etc.)
- Missing API verification approach
- Missing tooltip/logging standards
- These standards are scattered across implementation plans

**Impact:** Not a single source of truth for "how we code"

---

### 5. Unclear Folder Organization

**Issue:** Current structure mixes purposes
- StoryBlocks = content catalog + reference material
- Gameplay vs. Core unclear distinction
- Technical folder has content system docs
- Empty Interface folder
- No clear home for Identity, Combat systems

**Impact:** Hard to find information, illogical groupings

---

### 6. Duplicate and Overlapping Docs

**Issue:** Multiple docs cover same topics
- 2 quartermaster implementation plans (1,841 lines total)
- 2 provisions/food docs (1,780 lines total)
- Docs-audit.md duplicates this plan's purpose

**Impact:** Wasted space, confusion about which is current

---

### 7. Inconsistent Naming

**Issue:** Mixed conventions
- `Quartermaster_Master_Implementation.md` (underscores + CamelCase)
- `commander_track_schema.md` (underscores)
- Most use `kebab-case.md` (hyphens)

**Impact:** Looks unprofessional, harder to find files

---

### 8. Status Unclear

**Issue:** Hard to tell what's current vs. historical
- No status indicators on most docs
- No "last updated" dates
- Mix of implementation plans and feature docs

**Impact:** Can't trust if information is current

---

## Desired End State

### 1. Clear Folder Hierarchy

```
docs/
├── README.md                    # Primary entry point, overview of mod and docs
├── BLUEPRINT.md                 # Complete project guide (architecture + standards)
├── DEVELOPER-GUIDE.md           # How to build, modify, extend
├── ROADMAP.md                   # Future plans only
├── INDEX.md                     # Complete doc listing with status
│
├── Features/                    # All gameplay feature documentation
│   ├── Core/                    # Service mechanics (enlistment, pay, events)
│   ├── Identity/                # Traits, reputation, role detection
│   ├── Combat/                  # Training, formations, battle participation
│   ├── Equipment/               # Quartermaster, provisions, supplies
│   ├── Content/                 # Event system architecture
│   ├── Campaign/                # Leave, town access, camp life
│   ├── UI/                      # Interface systems
│   └── Technical/               # Encounter safety, schemas
│
├── Content/                     # Content catalog (events, orders, decisions)
│   ├── content-index.md
│   ├── event-catalog-by-system.md
│   └── map-incidents-catalog.md
│
└── Reference/                   # Technical reference materials
    ├── native-apis.md
    ├── native-skill-xp.md
    ├── native-map-incidents.md
    └── ai-behavior-analysis.md
```

---

### 2. Enhanced Blueprint

**New BLUEPRINT.md structure:**

```markdown
# Enlisted - Project Blueprint

## Summary
Complete guide to the Enlisted mod's architecture, coding standards, 
and development practices. Use this as the single source of truth for 
understanding how this project works and how we write code.

## Index
1. Overview & Philosophy
2. Project Architecture
3. Coding Standards & Practices
4. Build & Deployment
5. Dependencies & API Reference
6. File Structure
7. Key Patterns
8. Configuration
9. Logging & Debugging

## Coding Standards & Practices

### Code Quality
- **Use ReSharper linter** - Fix warnings, don't suppress with pragmas
- **Human comments, not changelogs** - Comments describe current behavior
  - Good: "Checks if player can re-enlist with this faction"
  - Bad: "Phase 2: Added re-enlistment check. Changed from X to Y"
- **No phase/legacy references in code comments**
- **Professional and natural** - Write as a human developer would

### API Verification
- **Always verify against local decompile first**
- Location: `C:\Dev\Enlisted\Decompile\`
- Target version: v1.3.11
- Key assemblies: TaleWorlds.CampaignSystem, Core, Library
- Don't rely on external docs or AI assumptions

### Data File Conventions
- **XML for player-facing text** - `ModuleData/Languages/enlisted_strings.xml`
- **JSON for content/config** - `ModuleData/Enlisted/*.json`
- In code: `TextObject("{=stringId}Fallback")`
- Critical: Fallback fields must follow ID fields in JSON

### Tooltip Best Practices
- Every event option needs a tooltip
- One sentence, under 80 characters
- Explain consequences clearly
- Example: "Accept discharge. 90-day re-enlistment block applies."

### Logging Standards
- All logs: `<BannerlordInstall>/Modules/Enlisted/Debugging/`
- Use: `ModLogger.Info("Category", "message")`
- Categories: Enlistment, Combat, Equipment, Events, etc.
- Log: Actions, errors, state changes

... (rest of blueprint content)
```

---

### 3. Documentation Standards (Applied to ALL Docs)

**Every document must have:**

```markdown
# Document Title

**Summary:** [2-3 sentence overview of what this doc covers and who should read it]

**Status:** ✅ Current | ⚠️ In Progress | 📦 Archived  
**Last Updated:** YYYY-MM-DD  
**Related Docs:** [Links to related documentation]

---

## Index

1. [Section 1](#section-1)
2. [Section 2](#section-2)
...

---

## Section 1
[Content]
```

**Status Icons:**
- ✅ **Current** - Actively maintained, reflects current implementation
- ⚠️ **In Progress** - Feature partially implemented, doc evolving
- 📦 **Archived** - Historical reference, no longer maintained
- ❌ **Deprecated** - Replaced by newer docs, kept for reference only

---

### 4. No Implementation Plans Folder

- All completed features converted to proper feature docs
- Future plans extracted to ROADMAP.md
- Outdated planning docs deleted
- ImplementationPlans/ folder removed entirely

---

### 5. Single Source of Truth

- Blueprint = project architecture + coding standards
- Each topic covered once, cross-referenced appropriately
- Clear entry points (README → specific docs)
- Easy navigation with indexes and links

---

## Complete File Inventory

### Root Files (6 files)

| File | Lines | Status | Issues | Action |
|------|-------|--------|--------|--------|
| `index.md` | 19 | ⚠️ Outdated | References non-existent files | RENAME → INDEX.md, update |
| `blueprint.md` | 182 | ⚠️ Incomplete | Missing coding standards | ENHANCE → BLUEPRINT.md |
| `CONTRIBUTING.md` | 155 | ⚠️ Duplicate | 80% overlap with blueprint | MERGE → DEVELOPER-GUIDE.md |
| `modderreadme.md` | 239 | ⚠️ Duplicate | 80% overlap with above | MERGE → DEVELOPER-GUIDE.md |
| `docs-audit.md` | 64 | ⚠️ Outdated | Superseded by this plan | DELETE |
| *(README.md)* | - | ❌ Missing | No primary entry point | CREATE |

---

### ImplementationPlans Folder (8 files - 13,546 lines)

| File | Lines | Status in File | Actual Status | Action |
|------|-------|---------------|---------------|--------|
| `macro-schedule-simplification.md` | 2,266 | "✅ COMPLETE" | Historical | DELETE |
| `traits-identity-system.md` | 1,940 | "✅ COMPLETE" | Implemented | MOVE → Features/Identity/ |
| `phase10-combat-xp-training.md` | 1,286 | "Phase 10 Complete" | Implemented | CONVERT → Features/Combat/training-system.md |
| `unified-content-system-implementation.md` | 1,134 | "Phase 5 Complete" | Implemented | CONVERT → Features/Content/content-system-architecture.md |
| `onboarding-retirement-system.md` | 687 | "✅ Complete" (17/17) | Implemented | CONVERT → Features/Core/onboarding-discharge-system.md |
| `phase9-logistics-simulation.md` | 1,297 | Mixed | Partially outdated | DELETE (extract relevant to ROADMAP) |
| `phase8-advanced-content-features.md` | 836 | Mixed | Partially outdated | DELETE (extract relevant to ROADMAP) |
| `enlisted-interface-master-plan.md` | 4,100 | Mixed | Mostly complete | EXTRACT → ROADMAP.md (future only) |

**Total:** 13,546 lines → Will become ~500-800 lines of feature docs + ROADMAP

---

### Features Folder (26 files across 7 subfolders)

#### Features/Core (9 files) - ✅ Keep

| File | Status | Action |
|------|--------|--------|
| `index.md` | ✅ Good | Keep, add summary/index |
| `core-gameplay.md` | ✅ Good | Keep, add summary/index |
| `enlistment.md` | ✅ Good | Keep, add summary/index |
| `pay-system.md` | ✅ Good | Keep, add summary/index |
| `company-events.md` | ✅ Good | Keep, add summary/index |
| `quartermaster-hero-system.md` | ✅ Good | Keep, add summary/index |
| `retinue-system.md` | ✅ Good | Keep, add summary/index |
| `companion-management.md` | ✅ Good | Keep, add summary/index |
| `camp-fatigue.md` | ✅ Good | Keep, add summary/index |

---

#### Features/Equipment (5 files) - Consolidate

| File | Lines | Status | Action |
|------|-------|--------|--------|
| `company-supply-simulation.md` | ? | ✅ Good | Keep, add summary/index |
| `quartermaster-dialogue-implementation.md` | 1,143 | ⚠️ Planning | CONSOLIDATE ↓ |
| `Quartermaster_Master_Implementation.md` | 698 | ⚠️ Planning | CONSOLIDATE → `quartermaster-system.md` |
| `quartermaster-equipment-quality.md` | ? | ✅ Good | Keep, add summary/index |
| `player-food-ration-system.md` | 1,702 | ⚠️ Design | CONSOLIDATE ↓ |

**Consolidation:**
1. **Quartermaster docs** (2 files, 1,841 lines) → `quartermaster-system.md` (~400 lines)
2. **Provisions docs** (see Gameplay below)

---

#### Features/Gameplay (5 files) - Reorganize

| File | Lines | Status | Action |
|------|-------|--------|--------|
| `camp-life-simulation.md` | ? | ✅ Good | MOVE → Features/Campaign/ |
| `event-reward-choices.md` | ? | ✅ Good | MOVE → Features/Content/ |
| `provisions-system.md` | 78 | ⚠️ Overlap | CONSOLIDATE with player-food-ration-system → Equipment/provisions-rations-system.md |
| `temporary-leave.md` | ? | ✅ Good | MOVE → Features/Campaign/ |
| `town-access-system.md` | ? | ✅ Good | MOVE → Features/Campaign/ |

---

#### Features/Interface - ❌ Empty

| Status | Action |
|--------|--------|
| Empty folder | DELETE FOLDER |

---

#### Features/Technical (4 files) - Reorganize

| File | Status | Action |
|------|--------|--------|
| `commander_track_schema.md` | ⚠️ Bad naming | RENAME → `commander-track-schema.md`, add summary/index |
| `encounter-safety.md` | ✅ Good | Keep, add summary/index |
| `event-system-schemas.md` | ✅ Good | MOVE → Features/Content/ |
| `formation-assignment.md` | ✅ Good | MOVE → Features/Combat/ |

---

#### Features/UI (2 files) - ✅ Keep

| File | Lines | Status | Action |
|------|-------|--------|--------|
| `README.md` | 80 | ✅ Good | Keep, add summary if missing |
| `ui-systems-master.md` | 1,011 | ✅ Good | Keep, add summary/index |

---

#### Features/Missions (1 file) - Verify

| File | Status | Action |
|------|--------|--------|
| `recon-mission.md` | ❓ Unknown | VERIFY implementation status, then DELETE or CONVERT |

**Verification needed:**
```bash
grep -r "ReconMission\|ScoutMission\|reconnaissance" src/
```

---

### StoryBlocks Folder (5 files) - Reorganize

| File | Purpose | Action |
|------|---------|--------|
| `content-index.md` | Content catalog | KEEP → Content/content-index.md |
| `event-catalog-by-system.md` | Event mechanics | KEEP → Content/event-catalog-by-system.md |
| `map-incidents-warsails.md` | Naval incidents | REVIEW, possibly merge with native catalog |
| `native-map-incidents-catalog.md` | Native reference | MOVE → Reference/native-map-incidents.md |
| `native-skill-xp-and-leveling.md` | Native reference | MOVE → Reference/native-skill-xp.md |

**Folder action:** RENAME `StoryBlocks/` → `Content/`

---

### Research Folder (4 files) - Reorganize

| File | Purpose | Action |
|------|---------|--------|
| `ai-strategic-behavior-analysis-v2.md` | AI analysis (1,499 lines) | MOVE → Reference/ai-behavior-analysis.md |
| `campaignsystem-apis.md` | API reference | MOVE → Reference/native-apis.md |
| `extract_native_map_incidents.py` | Data extraction tool | VERIFY usage, then MOVE to tools/ or DELETE |
| `native-map-incidents.json` | Extracted data | VERIFY usage, then MOVE to tools/ or DELETE |

**Folder action:** DELETE `research/` after moving relevant files

---

## Blueprint Enhancement Plan

### Current Blueprint Content (182 lines)

**Has:**
- Build instructions
- Workshop upload info
- Dependencies
- Structure overview
- Adding new files (critical .csproj note)
- Logging location
- Key patterns (UI, party following, gold, reputation)
- Configuration files
- Menu system

**Missing (found in implementation plans):**
- Coding standards (ReSharper, comment style)
- API verification approach
- Data file conventions (XML vs JSON rules)
- Tooltip best practices
- Logging categories and standards
- Engineering philosophy

---

### Enhanced Blueprint Structure

```markdown
# Enlisted - Project Blueprint

**Summary:** Complete guide to the Enlisted mod's architecture, coding standards, 
and development practices. This is the single source of truth for understanding 
how this project works and how we write code.

**Last Updated:** [Date]  
**Target Game:** Bannerlord v1.3.11  
**Related Docs:** [DEVELOPER-GUIDE.md], [TECHNICAL-REFERENCE.md]

---

## Index

1. [Overview & Philosophy](#overview--philosophy)
2. [Engineering Standards](#engineering-standards)
   - [Code Quality](#code-quality)
   - [API Verification](#api-verification)
   - [Data File Conventions](#data-file-conventions)
   - [Tooltip Best Practices](#tooltip-best-practices)
   - [Logging Standards](#logging-standards)
3. [Project Architecture](#project-architecture)
4. [Build & Deployment](#build--deployment)
5. [Dependencies](#dependencies)
6. [Native Reference (Decompile)](#native-reference-decompile)
7. [File Structure](#file-structure)
8. [Adding New Files](#adding-new-files)
9. [Key Patterns](#key-patterns)
10. [Configuration](#configuration)
11. [Menu System](#menu-system)
12. [Common Pitfalls](#common-pitfalls)

---

## Overview & Philosophy

Enlisted transforms Bannerlord into a soldier career simulator. Players enlist 
with a lord, follow orders, manage reputation, and progress through ranks from 
recruit to commander.

**Design Principles:**
- Emergent identity from choices, not menus
- Native Bannerlord integration (traits, skills, game menus)
- Minimal custom UI (use game's native systems)
- Choice-driven narrative progression
- Realistic military hierarchy and consequences

---

## Engineering Standards

### Code Quality

#### ReSharper Linter
- **Always follow ReSharper recommendations** (available in Rider or Visual Studio)
- Fix warnings, don't suppress with pragmas
- Exception: Only suppress if there's a specific compatibility reason with a comment explaining why

#### Comment Style
**Comments should be factual descriptions of current behavior**, written as a human 
developer would—professional and natural.

✅ **Good:**
```csharp
// Checks if the player can re-enlist with this faction based on cooldown and discharge history.
private bool CanReenlistWithFaction(Kingdom faction)
```

❌ **Bad:**
```csharp
// Phase 2: Added re-enlistment check. Previously used FactionVeteranRecord, now uses FactionServiceRecord.
// Changed from using days to using CampaignTime for better accuracy.
private bool CanReenlistWithFaction(Kingdom faction)
```

**Rules:**
- Describe WHAT the code does NOW, not what it used to do or when it changed
- No "Phase X" references in code comments
- No changelog-style framing ("Added X", "Changed from Y")
- No "legacy" or "migration" mentions in doc comments
- Write professionally and naturally

#### Code Organization
- Reuse existing patterns (e.g., copy OrderCatalog structure for new catalogs)
- Keep related functionality together
- Use clear, descriptive names
- Group related fields/properties/methods

---

### API Verification

**ALWAYS verify against the local native decompile FIRST**

- **Location:** `C:\Dev\Enlisted\Decompile\`
- **Target Version:** v1.3.11
- **Key Assemblies:**
  - `TaleWorlds.CampaignSystem` - Party, Settlement, Campaign behaviors
  - `TaleWorlds.Core` - CharacterObject, ItemObject
  - `TaleWorlds.Library` - Vec2, MBList, utility classes
  - `TaleWorlds.MountAndBlade` - Mission, Agent, combat
  - `SandBox.View` - Menu views, map handlers

**Process:**
1. Check decompile for actual API (authoritative)
2. Use official docs only for quick lookups (secondary)
3. Don't rely on external docs or AI assumptions
4. Verify method signatures, property types, enum values

**Example:**
```
Need: Party position
Check: C:\Dev\Enlisted\Decompile\TaleWorlds.CampaignSystem\TaleWorlds\CampaignSystem\Party\MobileParty.cs
Find: GetPosition2D() → Vec2 (NOT Position2D property)
```

---

### Data File Conventions

#### XML for Player-Facing Text
- **Localization:** `ModuleData/Languages/enlisted_strings.xml`
- **Gauntlet UI:** `GUI/Prefabs/**.xml`
- **In Code:** Use `TextObject("{=stringId}Fallback text")`
- Add string keys to enlisted_strings.xml even if only using English

#### JSON for Content/Config
- **Content:** `ModuleData/Enlisted/Events/*.json`
- **Config:** `ModuleData/Enlisted/*.json`
- **Orders/Decisions:** `ModuleData/Enlisted/Orders/*.json`, `Decisions/*.json`

#### Critical JSON Rule
In JSON, **fallback fields must immediately follow their ID fields** for proper parser association:

✅ **Correct:**
```json
{
  "titleId": "event_title_key",
  "title": "Fallback Title",
  "setupId": "event_setup_key",
  "setup": "Fallback setup text..."
}
```

❌ **Wrong:**
```json
{
  "titleId": "event_title_key",
  "setupId": "event_setup_key",
  "title": "Fallback Title",
  "setup": "Fallback setup text..."
}
```

---

### Tooltip Best Practices

**Every event option and decision must have a tooltip** explaining consequences.

#### Guidelines:
- Tooltips appear on hover in `MultiSelectionInquiryData` popups
- One sentence, under 80 characters
- Explain what happens, requirements, or trade-offs
- Be concise and clear

#### Examples:

**Consequences:**
```json
{"tooltip": "Accept discharge. 90-day re-enlistment block applies."}
{"tooltip": "Desert immediately. Criminal record and faction hostility."}
```

**Skill checks:**
```json
{"tooltip": "Charm check determines outcome."}
{"tooltip": "Requires Leadership 50+ to attempt."}
```

**Training:**
```json
{"tooltip": "Train with the weapon you carry into battle"}
{"tooltip": "Work on the skill that needs most improvement"}
{"tooltip": "Build stamina and footwork"}
```

**Requirements:**
```json
{"tooltip": "Requires Tier 7+ to unlock"}
{"tooltip": "Need Quartermaster reputation 30+ to purchase"}
```

---

### Logging Standards

#### Location
**ALL mod logs output to:**  
`<BannerlordInstall>\Modules\Enlisted\Debugging\`

**Example full path:**  
`C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\Enlisted\Debugging\`

#### Files Created:
- `Session-A_{timestamp}.log` - Current session (newest)
- `Session-B_{timestamp}.log` - Previous session
- `Session-C_{timestamp}.log` - Oldest kept session
- `Conflicts-A_{timestamp}.log` - Current conflicts diagnostics
- `Conflicts-B_{timestamp}.log` - Previous conflicts
- `Conflicts-C_{timestamp}.log` - Oldest kept conflicts
- `Current_Session_README.txt` - Summary of active logs

#### Usage:
```csharp
ModLogger.Info("Category", "message");
ModLogger.Warn("Category", "warning message");
ModLogger.Error("Category", "error message");
```

#### Standard Categories:
| Category | Purpose |
|----------|---------|
| `Enlistment` | Enlist, discharge, service events |
| `Combat` | Battle participation, XP, formations |
| `Equipment` | Quartermaster, gear changes, baggage |
| `Orders` | Order assignment, completion, failure |
| `Events` | Event triggering, delivery, effects |
| `Training` | Troop XP, skill gains |
| `News` | News feed updates |
| `ServiceRecord` | Faction record updates |
| `GracePeriod` | Lord death/capture handling |
| `EventDelivery` | Event processing pipeline |

#### What to Log:
- Actions taken (enlist, discharge, order accepted)
- State changes (reputation changes, tier promotion)
- Errors and warnings
- Migration/compatibility notices
- Validation failures

#### What NOT to Log:
- Normal tick operations (too spammy)
- Successful null checks (clutter)
- Repeated checks every frame

---

## Project Architecture

[... rest of current blueprint content enhanced with above standards ...]

## Build & Deployment

[... current content ...]

## Dependencies

[... current content ...]

## Native Reference (Decompile)

[... current content ...]

## File Structure

[... current content ...]

## Adding New Files

[... current content - this is critical ...]

## Key Patterns

[... current content ...]

## Configuration

[... current content ...]

## Menu System

[... current content ...]

## Common Pitfalls

[NEW SECTION]

### 1. Forgetting to Add Files to .csproj
**Problem:** New .cs files aren't automatically compiled (old-style project)  
**Solution:** Always add `<Compile Include="path\to\file.cs"/>` to Enlisted.csproj

### 2. Using Wrong Gold Transaction Method
**Problem:** `ChangeHeroGold()` doesn't update UI  
**Solution:** Always use `GiveGoldAction.ApplyBetweenCharacters()`

### 3. Wrong Equipment Slot Iteration
**Problem:** `Enum.GetValues(typeof(EquipmentIndex))` causes crashes  
**Solution:** Use numeric loop to `NumEquipmentSetSlots`

### 4. Incorrect JSON Field Order
**Problem:** Fallback text doesn't work  
**Solution:** Fallback fields must immediately follow ID fields

### 5. Not Verifying APIs
**Problem:** Using outdated/incorrect API calls  
**Solution:** Always check decompile before using any native API

---

```

---

## Documentation Standards

### Template for All Documents

```markdown
# Document Title

**Summary:** [2-3 sentences explaining what this document covers, who should 
read it, and when to reference it]

**Status:** ✅ Current | ⚠️ In Progress | 📦 Archived  
**Last Updated:** YYYY-MM-DD  
**Related Docs:** [Link 1], [Link 2]

---

## Index

1. [Overview](#overview)
2. [Main Section 1](#main-section-1)
3. [Main Section 2](#main-section-2)
...

---

## Overview

[Brief introduction to the topic]

---

## Main Section 1

[Content organized logically with clear headings]

### Subsection 1.1

[Details]

---

## Main Section 2

[More content]

---
```

### Status Indicator Guide

| Icon | Status | When to Use |
|------|--------|-------------|
| ✅ | **Current** | Actively maintained, reflects current implementation |
| ⚠️ | **In Progress** | Feature partially implemented, doc evolving |
| 📦 | **Archived** | Historical reference, no longer maintained |
| ❌ | **Deprecated** | Replaced by newer docs, kept for reference only |

---

## Proposed New Structure

```
docs/
│
├── README.md                          # PRIMARY ENTRY POINT
│                                      # Mod overview, quick links to all docs
│
├── BLUEPRINT.md                       # PROJECT GUIDE
│                                      # Architecture + coding standards
│                                      # Single source of truth
│
├── DEVELOPER-GUIDE.md                 # DEVELOPMENT INSTRUCTIONS
│                                      # How to build, modify, extend
│                                      # Merged from CONTRIBUTING + modderreadme
│
├── ROADMAP.md                         # FUTURE PLANS
│                                      # What's coming next
│                                      # Extracted from implementation plans
│
├── INDEX.md                           # COMPLETE DOC LISTING
│                                      # All docs with status indicators
│                                      # Organized by category
│
├── Features/                          # FEATURE DOCUMENTATION
│   ├── README.md                      # Features overview and index
│   │
│   ├── Core/                          # Core gameplay systems
│   │   ├── README.md
│   │   ├── core-gameplay.md
│   │   ├── enlistment.md
│   │   ├── pay-system.md
│   │   ├── company-events.md
│   │   ├── quartermaster-hero-system.md
│   │   ├── retinue-system.md
│   │   ├── companion-management.md
│   │   ├── camp-fatigue.md
│   │   └── onboarding-discharge-system.md  # NEW (from impl plan)
│   │
│   ├── Identity/                      # NEW FOLDER
│   │   ├── README.md                  # NEW
│   │   └── identity-system.md         # From traits-identity-system.md
│   │
│   ├── Combat/                        # NEW FOLDER
│   │   ├── README.md                  # NEW
│   │   ├── training-system.md         # From phase10 doc
│   │   └── formation-assignment.md    # From Technical/
│   │
│   ├── Equipment/                     # Reorganized
│   │   ├── README.md
│   │   ├── company-supply-simulation.md
│   │   ├── quartermaster-system.md    # CONSOLIDATED from 2 files
│   │   ├── quartermaster-equipment-quality.md
│   │   └── provisions-rations-system.md  # CONSOLIDATED from 2 files
│   │
│   ├── Content/                       # NEW FOLDER (system architecture)
│   │   ├── README.md                  # NEW
│   │   ├── content-system-architecture.md  # From unified-content doc
│   │   ├── event-system-schemas.md    # From Technical/
│   │   └── event-reward-choices.md    # From Gameplay/
│   │
│   ├── Campaign/                      # NEW FOLDER (campaign gameplay)
│   │   ├── README.md                  # NEW
│   │   ├── camp-life-simulation.md    # From Gameplay/
│   │   ├── temporary-leave.md         # From Gameplay/
│   │   └── town-access-system.md      # From Gameplay/
│   │
│   ├── UI/                            # Keep as-is
│   │   ├── README.md
│   │   └── ui-systems-master.md
│   │
│   └── Technical/                     # Reduced folder
│       ├── README.md
│       ├── encounter-safety.md
│       └── commander-track-schema.md  # Renamed from underscore version
│
├── Content/                           # RENAMED from StoryBlocks
│   ├── README.md                      # NEW
│   ├── content-index.md               # Master content catalog
│   ├── event-catalog-by-system.md     # Event mechanics
│   └── map-incidents-catalog.md       # Map incidents (possibly merged)
│
└── Reference/                         # NEW FOLDER (technical reference)
    ├── README.md                      # NEW
    ├── native-apis.md                 # From research/campaignsystem-apis.md
    ├── native-skill-xp.md             # From StoryBlocks/
    ├── native-map-incidents.md        # From StoryBlocks/
    └── ai-behavior-analysis.md        # From research/
```

**Summary of Changes:**
- **Deleted:** ImplementationPlans/, research/, Features/Interface/, Features/Gameplay/
- **Created:** 4 new feature folders (Identity, Combat, Content, Campaign)
- **Created:** Reference/ folder
- **Renamed:** StoryBlocks/ → Content/
- **Consolidated:** 6 files into 3
- **Enhanced:** Blueprint with all coding standards
- **Standardized:** All docs get summary + index

---

## Key Decisions & Rationale

### Decision 1: Delete macro-schedule-simplification.md

**Status in file:** "✅ COMPLETE - Code implemented, systems deleted"

**Rationale:**
- Document explicitly states it describes "what was ALREADY IMPLEMENTED"
- Systems discussed (Schedule, Lance, Duties) have been "deleted from the codebase"
- Pure historical planning document with no current value
- Content about completed systems already documented in feature docs

**Action:** DELETE

---

### Decision 2: Convert onboarding-retirement-system.md to Feature Doc

**Status in file:** 17 out of 17 features marked "✅ Complete"

**Rationale:**
- All listed features are implemented
- Contains valuable documentation of onboarding/discharge system
- Written as implementation plan, needs rewrite as feature documentation
- Important system deserving proper documentation

**Action:** CONVERT to `Features/Core/onboarding-discharge-system.md`
- Remove planning/phase language
- Document current implementation
- Extract configuration examples
- Cross-reference with enlistment.md

---

### Decision 3: Consolidate Quartermaster Docs

**Files:** 
- `quartermaster-dialogue-implementation.md` (1,143 lines) - "Implementation Planning"
- `Quartermaster_Master_Implementation.md` (698 lines) - "Master Implementation Plan"

**Rationale:**
- Both are planning docs, not feature documentation
- Heavy overlap in content and purpose
- Both reference the same underlying specs
- Neither reflects current implementation accurately
- Total 1,841 lines can become ~400 line feature doc

**Action:** CONSOLIDATE into `Features/Equipment/quartermaster-system.md`
1. Verify current implementation in code
2. Document actual current behavior
3. Remove all planning language
4. Extract configuration and usage
5. Move future plans to ROADMAP.md
6. Delete both old docs

---

### Decision 4: Consolidate Provisions Docs

**Files:**
- `Features/Gameplay/provisions-system.md` (78 lines) - General overview
- `Features/Equipment/player-food-ration-system.md` (1,702 lines) - Detailed spec

**Rationale:**
- Overlapping content (both discuss personal rations, retinue provisioning, QM access)
- Overview doc says "✅ Good" but detailed doc says "Design Phase" (status confusion)
- Detailed doc contains everything from overview plus much more
- Total 1,780 lines can become ~300-400 line feature doc

**Action:** CONSOLIDATE into `Features/Equipment/provisions-rations-system.md`
1. Use detailed doc as base (more complete)
2. Add any unique content from overview
3. Verify current implementation status
4. Remove planning language if features are implemented
5. Delete both old files

---

### Decision 5: Verify recon-mission.md Status

**File structure:** 6-phase feature spec (detailed planning)

**Rationale:**
- Document written as planning spec, not documentation
- No status indicators
- Unclear what's implemented vs. planned
- Need to check codebase for actual implementation

**Action:** VERIFY implementation first
```bash
grep -r "ReconMission\|ScoutMission\|reconnaissance" src/
```

**Then:**
- If NOT implemented: DELETE or move relevant parts to ROADMAP.md
- If PARTIALLY implemented: Update with status markers, convert implemented parts
- If FULLY implemented: Convert to `Features/Missions/reconnaissance-missions.md`

---

### Decision 6: Keep ai-strategic-behavior-analysis.md

**File:** `research/ai-strategic-behavior-analysis-v2.md` (1,499 lines)

**Rationale:**
- Detailed analysis of Bannerlord AI from decompiled source
- Valuable reference for developers working on AI-related features
- Not planning, but research/analysis
- Not directly actionable but highly informative
- Useful for understanding native game mechanics

**Action:** MOVE to `Reference/ai-behavior-analysis.md`
- Keep as reference material
- Add header noting it's analysis, not implementation
- Valuable for understanding native behavior

---

### Decision 7: Research Tools (Python Script, JSON)

**Files:**
- `research/extract_native_map_incidents.py`
- `research/native-map-incidents.json`

**Rationale:**
- Tools for extracting data from decompile
- May or may not still be used
- Should be in tools/ if still useful, deleted if not

**Action:** VERIFY usage first
1. Is the JSON up to date with current game version?
2. Does the script still run against current decompile?
3. Are map incidents still being extracted/updated?

**If YES (still used):**
- Create `tools/` folder in project root
- Move both files to `tools/data-extraction/`
- Add README explaining purpose and usage

**If NO (outdated/unused):**
- DELETE both files
- Keep `native-map-incidents-catalog.md` in Reference (contains extracted data)

---

### Decision 8: Create Separate Combat Folder

**Rationale:**
- Combat systems are distinct from core enlistment/progression
- Multiple combat-related docs (training, formations, battle participation)
- Combat is a major feature area deserving its own folder
- Keeps Core folder focused on service mechanics
- Easier navigation when properly categorized

**Action:** CREATE `Features/Combat/`
- Move `formation-assignment.md` from Technical/
- Convert `phase10` doc to `training-system.md`
- Add battle participation doc if needed

---

### Decision 9: Create Separate Identity Folder

**Rationale:**
- Identity system (traits, reputation, roles) is substantial (~1,940 lines)
- Distinct from core enlistment mechanics
- Cross-cuts multiple systems (events, orders, combat)
- Deserves its own focus area
- Document is large enough to warrant dedicated folder

**Action:** CREATE `Features/Identity/`
- Move and convert `traits-identity-system.md` to `identity-system.md`
- Add detailed reputation mechanics if needed
- Add role detection details if needed

---

### Decision 10: Enhance Blueprint with Coding Standards

**Rationale:**
- Blueprint currently only covers architecture
- Coding standards scattered across multiple implementation plans
- Need single source of truth for "how we code"
- Standards include: ReSharper, comment style, API verification, tooltips, logging
- These are consistent across all implementation plans (same standards repeated)

**Action:** ENHANCE `BLUEPRINT.md`
1. Extract all "Engineering Standards" sections from implementation plans
2. Consolidate into comprehensive standards section
3. Include: Code quality, API verification, data files, tooltips, logging
4. Make Blueprint the definitive project guide
5. Ensure it's both architecture AND standards

---

## Phase-by-Phase Execution Plan

### Phase 1: Backup & Setup (15 minutes)

**Purpose:** Safety first

**Actions:**
```bash
# 1. Create backup
cp -r docs docs_backup_2025-12-22

# 2. Create git branch
git checkout -b docs/reorganization

# 3. Create execution log
touch docs/reorganization.log
echo "$(date): Started documentation reorganization" >> docs/reorganization.log
```

**Verification:**
- [ ] Backup folder exists
- [ ] On correct git branch
- [ ] Log file created

---

### Phase 2: Critical Deletions (30 minutes)

**Purpose:** Remove clutter immediately

**Actions:**
1. DELETE `ImplementationPlans/macro-schedule-simplification.md`
2. DELETE `ImplementationPlans/phase8-advanced-content-features.md`
3. DELETE `ImplementationPlans/phase9-logistics-simulation.md`
4. DELETE `docs/docs-audit.md`
5. DELETE `Features/Interface/` (empty folder)

**Log each deletion:**
```bash
echo "$(date): Deleted [filename] - [reason]" >> docs/reorganization.log
```

**Commit:**
```bash
git add docs/
git commit -m "docs: phase 2 - remove outdated planning docs and empty folders"
```

**Verification:**
- [ ] 5 items deleted
- [ ] Git commit created
- [ ] Log updated

---

### Phase 3: Root Documentation (2 hours)

**Purpose:** Establish new entry points

**Actions:**

**3.1: CREATE `docs/README.md`** (30 min)
```markdown
# Enlisted - Bannerlord Soldier Career Mod

**Summary:** Transform Bannerlord into a soldier career simulator. Enlist with 
a lord, follow orders, manage reputation, and advance from recruit to commander.

**Version:** v1.0.0  
**Target Game:** Mount & Blade II: Bannerlord v1.3.11  
**Workshop:** [Steam Workshop Link]

---

## Index

1. [What is Enlisted?](#what-is-enlisted)
2. [Documentation](#documentation)
3. [Quick Links](#quick-links)

---

## What is Enlisted?

[Brief description of the mod]

## Documentation

### Getting Started
- **[Core Gameplay Guide](Features/Core/core-gameplay.md)** - Start here
- **[Installation & Setup](BLUEPRINT.md#build--deployment)** - How to install

### For Players
- **[Features Index](Features/README.md)** - All gameplay features
- **[Content Catalog](Content/README.md)** - Events, orders, decisions

### For Developers
- **[Blueprint](BLUEPRINT.md)** - Project guide (architecture + standards)
- **[Developer Guide](DEVELOPER-GUIDE.md)** - How to build and modify
- **[API Reference](Reference/README.md)** - Bannerlord API notes

### Planning & Reference
- **[Roadmap](ROADMAP.md)** - Future features
- **[Complete Index](INDEX.md)** - All documentation

## Quick Links

- **Build:** `dotnet build -c "Enlisted RETAIL" /p:Platform=x64`
- **Logs:** `<Bannerlord>\Modules\Enlisted\Debugging\`
- **Decompile:** `C:\Dev\Enlisted\Decompile\`
```

**3.2: ENHANCE `blueprint.md` → `BLUEPRINT.md`** (60 min)
- Add summary and index at top
- Add entire "Engineering Standards" section (from section above)
- Keep existing architecture content
- Add "Common Pitfalls" section
- Rename file to BLUEPRINT.md

**3.3: CREATE `DEVELOPER-GUIDE.md`** (30 min)
- Merge content from CONTRIBUTING.md and modderreadme.md
- Add summary and index
- Organize into sections: Build, Structure, Adding Files, Debugging, Guidelines
- Remove duplicate content

**3.4: RENAME `index.md` → `INDEX.md`**
- Add summary at top
- Update to list all docs with status indicators
- Group by category (Features, Content, Reference)

**3.5: DELETE old files**
- DELETE `CONTRIBUTING.md`
- DELETE `modderreadme.md`

**Commit:**
```bash
git add docs/
git commit -m "docs: phase 3 - create root documentation and entry points"
```

**Verification:**
- [ ] README.md created
- [ ] BLUEPRINT.md enhanced with standards
- [ ] DEVELOPER-GUIDE.md created
- [ ] INDEX.md updated
- [ ] Old duplicates deleted

---

### Phase 4: Major Consolidations (3 hours)

**Purpose:** Reduce duplicates

**4.1: Consolidate Quartermaster Docs** (60 min)

**Check implementation first:**
```bash
grep -r "Quartermaster" src/Features/Equipment/
ls ModuleData/Enlisted/*quartermaster* 2>/dev/null
```

**Create `Features/Equipment/quartermaster-system.md`:**
```markdown
# Quartermaster System

**Summary:** The Quartermaster manages equipment, provisions, baggage checks, 
and supply access for enlisted soldiers. This document covers the complete 
quartermaster interaction system including reputation-based access and pricing.

**Status:** ✅ Current  
**Last Updated:** [Date]  
**Related Docs:** [company-supply-simulation.md], [provisions-rations-system.md]

---

## Index

1. [Overview](#overview)
2. [Equipment System](#equipment-system)
3. [Provisions Access](#provisions-access)
4. [Baggage Checks](#baggage-checks)
5. [Supply System Integration](#supply-system-integration)
6. [Reputation Effects](#reputation-effects)
7. [Configuration](#configuration)

---

## Overview

[Document current implementation, not planning]

[Extract actual behavior from code and existing docs]

---

[... rest of content ...]
```

**DELETE:**
- `quartermaster-dialogue-implementation.md`
- `Quartermaster_Master_Implementation.md`

---

**4.2: Consolidate Provisions Docs** (60 min)

**Create `Features/Equipment/provisions-rations-system.md`:**
```markdown
# Provisions & Rations System

**Summary:** Complete food system covering issued rations (T1-T6), officer 
provisioning (T7+), and retinue feeding requirements. Tracks how soldiers 
obtain and consume food throughout their service.

**Status:** ✅ Current  
**Last Updated:** [Date]  
**Related Docs:** [quartermaster-system.md], [camp-fatigue.md]

---

## Index

1. [Overview](#overview)
2. [Issued Rations (T1-T6)](#issued-rations-t1-t6)
3. [Officer Provisioning (T7+)](#officer-provisioning-t7)
4. [Ration Exchange System](#ration-exchange-system)
5. [Retinue Feeding](#retinue-feeding)
6. [System Integration](#system-integration)

---

[Merge content from both docs, verify implementation]

---
```

**DELETE:**
- `Features/Gameplay/provisions-system.md`
- `Features/Equipment/player-food-ration-system.md`

---

**4.3: Convert Onboarding Doc** (60 min)

**Create `Features/Core/onboarding-discharge-system.md`:**
```markdown
# Onboarding & Discharge System

**Summary:** Governs how players enter service (onboarding), leave service 
(discharge), and re-enlist with the same or different factions. Tracks service 
history, discharge bands, and re-enlistment eligibility per faction.

**Status:** ✅ Current  
**Last Updated:** [Date]  
**Related Docs:** [enlistment.md], [pay-system.md]

---

## Index

1. [Overview](#overview)
2. [Onboarding Process](#onboarding-process)
3. [Discharge Types](#discharge-types)
4. [Discharge Bands](#discharge-bands)
5. [Re-enlistment Rules](#re-enlistment-rules)
6. [Service Records](#service-records)
7. [Grace Period System](#grace-period-system)

---

[Extract from onboarding-retirement-system.md, remove planning language]

---
```

**DELETE:**
- `ImplementationPlans/onboarding-retirement-system.md`

**Commit:**
```bash
git add docs/
git commit -m "docs: phase 4 - consolidate quartermaster, provisions, and onboarding docs"
```

**Verification:**
- [ ] 3 new consolidated docs created
- [ ] 5 old files deleted
- [ ] All new docs have summary + index

---

### Phase 5: Folder Restructuring (30 minutes)

**Purpose:** Create new organization

**Actions:**

**5.1: Create new folders:**
```bash
mkdir -p docs/Features/Identity
mkdir -p docs/Features/Combat
mkdir -p docs/Features/Content
mkdir -p docs/Features/Campaign
mkdir -p docs/Reference
```

**5.2: Rename StoryBlocks:**
```bash
mv docs/StoryBlocks docs/Content
```

**5.3: Create README files:**
- `Features/Identity/README.md`
- `Features/Combat/README.md`
- `Features/Content/README.md`
- `Features/Campaign/README.md`
- `Reference/README.md`
- `Content/README.md` (update after rename)

Each README should follow template:
```markdown
# [Folder Name]

**Summary:** [What this folder contains and when to use it]

---

## Index

- [File 1](file1.md) - Brief description
- [File 2](file2.md) - Brief description
...

---
```

**Commit:**
```bash
git add docs/
git commit -m "docs: phase 5 - create new folder structure"
```

**Verification:**
- [ ] 6 new folders created
- [ ] StoryBlocks renamed to Content
- [ ] 6 README files created

---

### Phase 6: Move Files (2 hours)

**Purpose:** Reorganize into new structure

**Actions:**

**6.1: ImplementationPlans → Features** (30 min)
```bash
# Move and convert trait doc
mv docs/ImplementationPlans/traits-identity-system.md docs/Features/Identity/identity-system.md

# Convert phase10 doc (create new, extract content, delete old)
# [Manual conversion needed - remove planning language]
# Result: docs/Features/Combat/training-system.md
rm docs/ImplementationPlans/phase10-combat-xp-training.md

# Convert unified content doc
# [Manual conversion needed]
# Result: docs/Features/Content/content-system-architecture.md
rm docs/ImplementationPlans/unified-content-system-implementation.md
```

**6.2: Extract ROADMAP** (30 min)
```bash
# Extract future plans from enlisted-interface-master-plan.md
# Create: docs/ROADMAP.md
# Delete: docs/ImplementationPlans/enlisted-interface-master-plan.md
```

**6.3: Delete ImplementationPlans folder** (5 min)
```bash
rmdir docs/ImplementationPlans
```

**6.4: Reorganize Technical folder** (15 min)
```bash
# Move to Combat
mv docs/Features/Technical/formation-assignment.md docs/Features/Combat/

# Move to Content
mv docs/Features/Technical/event-system-schemas.md docs/Features/Content/

# Rename remaining file
mv docs/Features/Technical/commander_track_schema.md docs/Features/Technical/commander-track-schema.md
```

**6.5: Reorganize Gameplay folder** (20 min)
```bash
# Move to Content
mv docs/Features/Gameplay/event-reward-choices.md docs/Features/Content/

# Move to Campaign
mv docs/Features/Gameplay/camp-life-simulation.md docs/Features/Campaign/
mv docs/Features/Gameplay/temporary-leave.md docs/Features/Campaign/
mv docs/Features/Gameplay/town-access-system.md docs/Features/Campaign/

# Delete Gameplay folder (now empty)
rmdir docs/Features/Gameplay
```

**6.6: Move research files** (20 min)
```bash
# Move to Reference
mv docs/research/ai-strategic-behavior-analysis-v2.md docs/Reference/ai-behavior-analysis.md
mv docs/research/campaignsystem-apis.md docs/Reference/native-apis.md

# Move StoryBlocks reference files
mv docs/Content/native-skill-xp-and-leveling.md docs/Reference/native-skill-xp.md
mv docs/Content/native-map-incidents-catalog.md docs/Reference/native-map-incidents.md

# Handle tools - verify first
# If still used: create tools/ folder and move
# If not used: delete

# Delete research folder
rm -rf docs/research
```

**Commit:**
```bash
git add docs/
git commit -m "docs: phase 6 - move files to new structure"
```

**Verification:**
- [ ] Identity folder populated
- [ ] Combat folder populated
- [ ] Content folder populated
- [ ] Campaign folder populated
- [ ] Reference folder populated
- [ ] ImplementationPlans folder deleted
- [ ] Gameplay folder deleted
- [ ] research folder deleted

---

### Phase 7: Add Summaries & Indexes (3 hours)

**Purpose:** Standardize all documentation

**Actions:**

For EACH document in docs/, add to the top:
```markdown
# [Title]

**Summary:** [2-3 sentence overview]

**Status:** ✅ Current | ⚠️ In Progress | 📦 Archived  
**Last Updated:** [Date]  
**Related Docs:** [Links]

---

## Index

1. [Section 1](#section-1)
2. [Section 2](#section-2)
...

---
```

**Priority order:**
1. Root docs (README, BLUEPRINT, DEVELOPER-GUIDE, ROADMAP, INDEX) - 1 hour
2. Features/Core docs - 30 min
3. Features/Identity, Combat, Content, Campaign - 45 min
4. Features/Equipment, UI, Technical - 30 min
5. Content folder docs - 15 min
6. Reference folder docs - 15 min

**Commit after each folder:**
```bash
git add docs/
git commit -m "docs: phase 7 - add summaries and indexes to [folder]"
```

**Verification:**
- [ ] All docs have summaries
- [ ] All docs have indexes
- [ ] All docs have status indicators
- [ ] All docs have last updated dates

---

### Phase 8: Update Cross-References (2 hours)

**Purpose:** Fix all broken links

**Actions:**

**8.1: Global find-replace** (30 min)
```bash
# Update ImplementationPlans references
find docs/ -type f -name "*.md" -exec sed -i 's|ImplementationPlans/traits-identity-system|Features/Identity/identity-system|g' {} +
# [Continue for all moved files]

# Update StoryBlocks references
find docs/ -type f -name "*.md" -exec sed -i 's|StoryBlocks/|Content/|g' {} +

# Update research references
find docs/ -type f -name "*.md" -exec sed -i 's|research/campaignsystem-apis|Reference/native-apis|g' {} +
```

**8.2: Update index files** (30 min)
- Update `INDEX.md` with complete new structure
- Update all folder README files with current file lists
- Update `Features/README.md` with new subfolders

**8.3: Check source code references** (30 min)
```bash
# Search for doc references in code
grep -r "docs/ImplementationPlans" src/
grep -r "docs/StoryBlocks" src/
grep -r "docs/research" src/

# Update any found references
```

**8.4: Manual link verification** (30 min)
- Open each major doc and click all internal links
- Fix any broken links found

**Commit:**
```bash
git add docs/
git commit -m "docs: phase 8 - update all cross-references and fix broken links"
```

**Verification:**
- [ ] No ImplementationPlans references remain
- [ ] No StoryBlocks references remain  
- [ ] No research/ references remain
- [ ] All links tested and working

---

### Phase 9: Verify Implementation Status (1-2 hours)

**Purpose:** Ensure docs reflect reality

**Actions:**

**9.1: Verify recon-mission.md** (30 min)
```bash
# Check if implemented
grep -r "ReconMission\|ScoutMission\|reconnaissance" src/

# Based on findings:
# - If NOT implemented: DELETE or extract to ROADMAP
# - If implemented: Convert to feature doc with summary/index
# - If partial: Update with clear status markers
```

**9.2: Review converted docs** (60 min)
- Read through identity-system.md
- Read through training-system.md
- Read through content-system-architecture.md
- Ensure all removed planning language
- Ensure all describe current implementation
- Add notes if features are partial

**Commit:**
```bash
git add docs/
git commit -m "docs: phase 9 - verify implementation status and update accordingly"
```

**Verification:**
- [ ] Recon mission status determined
- [ ] All converted docs reviewed
- [ ] Docs accurately reflect implementation

---

### Phase 10: Final Review & Cleanup (1 hour)

**Purpose:** Polish and verify completion

**Actions:**

**10.1: Test navigation** (20 min)
- Start at README.md
- Navigate to each major section
- Verify all links work
- Verify organization is clear

**10.2: Check Success Criteria** (20 min)
- [ ] No ImplementationPlans folder
- [ ] Clear folder structure (Features, Content, Reference)
- [ ] Single README.md entry point
- [ ] No duplicates
- [ ] All files use kebab-case
- [ ] No broken links
- [ ] All docs have summaries and indexes
- [ ] All docs have status headers
- [ ] Easy navigation (each folder has README)
- [ ] Can understand and recreate mod from docs

**10.3: Update root INDEX.md** (10 min)
- Generate complete listing of all docs
- Add status icons
- Group by folder

**10.4: Final commit** (10 min)
```bash
git add docs/
git commit -m "docs: reorganization complete - final cleanup and verification"
```

**Verification:**
- [ ] All success criteria met
- [ ] Navigation tested
- [ ] INDEX.md complete and accurate

---

## File Disposition Matrix

Quick reference for what happens to each file:

| Current Location | Action | New Location | Priority |
|-----------------|--------|--------------|----------|
| **Root** |
| `index.md` | RENAME | `INDEX.md` | High |
| `blueprint.md` | ENHANCE+RENAME | `BLUEPRINT.md` | High |
| `CONTRIBUTING.md` | MERGE | `DEVELOPER-GUIDE.md` | High |
| `modderreadme.md` | MERGE | `DEVELOPER-GUIDE.md` | High |
| `docs-audit.md` | DELETE | N/A | High |
| *(none)* | CREATE | `README.md` | High |
| *(none)* | CREATE | `ROADMAP.md` | High |
| **ImplementationPlans** |
| `macro-schedule-simplification.md` | DELETE | N/A | High |
| `phase8-advanced-content-features.md` | DELETE | N/A | High |
| `phase9-logistics-simulation.md` | DELETE | N/A | High |
| `traits-identity-system.md` | MOVE+CONVERT | `Features/Identity/identity-system.md` | High |
| `phase10-combat-xp-training.md` | CONVERT | `Features/Combat/training-system.md` | High |
| `unified-content-system-implementation.md` | CONVERT | `Features/Content/content-system-architecture.md` | High |
| `onboarding-retirement-system.md` | CONVERT | `Features/Core/onboarding-discharge-system.md` | High |
| `enlisted-interface-master-plan.md` | EXTRACT | `ROADMAP.md` | High |
| *(folder)* | DELETE | N/A | High |
| **Features/Core** |
| `*.md` (9 files) | ADD SUMMARY | Same location | Medium |
| **Features/Equipment** |
| `quartermaster-dialogue-implementation.md` | CONSOLIDATE ↓ | - | High |
| `Quartermaster_Master_Implementation.md` | CONSOLIDATE → | `quartermaster-system.md` | High |
| `company-supply-simulation.md` | ADD SUMMARY | Same location | Medium |
| `quartermaster-equipment-quality.md` | ADD SUMMARY | Same location | Medium |
| **Features/Gameplay** |
| `provisions-system.md` | CONSOLIDATE → | `Equipment/provisions-rations-system.md` | High |
| `camp-life-simulation.md` | MOVE | `Features/Campaign/` | Medium |
| `event-reward-choices.md` | MOVE | `Features/Content/` | Medium |
| `temporary-leave.md` | MOVE | `Features/Campaign/` | Medium |
| `town-access-system.md` | MOVE | `Features/Campaign/` | Medium |
| *(folder)* | DELETE | N/A | Medium |
| **Features/Interface** |
| *(empty folder)* | DELETE | N/A | High |
| **Features/Technical** |
| `commander_track_schema.md` | RENAME | `commander-track-schema.md` | Medium |
| `encounter-safety.md` | ADD SUMMARY | Same location | Medium |
| `event-system-schemas.md` | MOVE | `Features/Content/` | Medium |
| `formation-assignment.md` | MOVE | `Features/Combat/` | Medium |
| **Features/UI** |
| `README.md` | ADD SUMMARY | Same location | Low |
| `ui-systems-master.md` | ADD SUMMARY | Same location | Low |
| **Features/Missions** |
| `recon-mission.md` | VERIFY FIRST | TBD | Medium |
| **StoryBlocks** |
| *(folder)* | RENAME | `Content/` | Medium |
| `content-index.md` | MOVE | `Content/` | Low |
| `event-catalog-by-system.md` | MOVE | `Content/` | Low |
| `map-incidents-warsails.md` | REVIEW | `Content/` or merge | Low |
| `native-map-incidents-catalog.md` | MOVE | `Reference/native-map-incidents.md` | Low |
| `native-skill-xp-and-leveling.md` | MOVE | `Reference/native-skill-xp.md` | Low |
| **research** |
| `ai-strategic-behavior-analysis-v2.md` | MOVE | `Reference/ai-behavior-analysis.md` | Low |
| `campaignsystem-apis.md` | MOVE | `Reference/native-apis.md` | Low |
| `extract_native_map_incidents.py` | VERIFY | `tools/` or DELETE | Low |
| `native-map-incidents.json` | VERIFY | `tools/` or DELETE | Low |
| *(folder)* | DELETE | N/A | Low |

---

## Success Criteria

### The reorganization is complete when:

- [ ] **No ImplementationPlans folder exists**
- [ ] **Clear folder hierarchy** (Features, Content, Reference)
- [ ] **Single entry point** (README.md directs to everything)
- [ ] **Enhanced Blueprint** (architecture + all coding standards)
- [ ] **No duplicates** (each topic covered once)
- [ ] **Consistent naming** (all kebab-case, no underscores/CamelCase)
- [ ] **No broken links** (all cross-references work)
- [ ] **All docs have summaries** (2-3 sentence overview at top)
- [ ] **All docs have indexes** (table of contents)
- [ ] **Status indicators** (every doc has status header)
- [ ] **Easy navigation** (each folder has README)
- [ ] **Recreation possible** (another developer can rebuild from docs)
- [ ] **AI-friendly** (structure is logical and discoverable)
- [ ] **Professional** (consistent formatting, clear organization)

### Quality Checks:

- [ ] **Docs accurately reflect implementation** (verified against code)
- [ ] **No planning language** (docs describe current state)
- [ ] **No phase references** (removed from all docs)
- [ ] **Appropriate level of detail** (neither too brief nor too verbose)
- [ ] **Examples provided** (where helpful)
- [ ] **Related docs linked** (easy to find related information)

---

## Backup & Safety Procedures

### Before Starting:

```bash
# 1. Create backup
cp -r docs docs_backup_2025-12-22

# 2. Create git branch
git checkout -b docs/reorganization

# 3. Create execution log
touch docs/reorganization.log
echo "$(date): Documentation reorganization started" >> docs/reorganization.log
```

### During Execution:

**After each phase:**
```bash
# Log progress
echo "$(date): Completed Phase X - [description]" >> docs/reorganization.log

# Commit changes
git add docs/
git commit -m "docs: phase X - [description]"
```

### If Problems Occur:

**Option 1: Rollback last phase**
```bash
git reset --hard HEAD~1
```

**Option 2: Rollback all changes**
```bash
git checkout main
git branch -D docs/reorganization
rm -rf docs/
cp -r docs_backup_2025-12-22 docs/
```

**Option 3: Start fresh**
```bash
rm -rf docs/
cp -r docs_backup_2025-12-22 docs/
git checkout -b docs/reorganization-v2
# Start again with lessons learned
```

### After Completion:

**Before deleting backup:**
1. Test all navigation thoroughly
2. Have another person review
3. Wait 24-48 hours to ensure no issues
4. Run full test suite (if applicable)

**Then:**
```bash
# Merge to main
git checkout main
git merge docs/reorganization

# Delete backup
rm -rf docs_backup_2025-12-22

# Delete branch
git branch -D docs/reorganization
```

---

## Execution Checklist

**Before Starting:**
- [ ] Read entire plan
- [ ] Understand all decisions
- [ ] Have backup created
- [ ] On git branch
- [ ] Have log file

**During Execution:**
- [ ] Complete phases in order
- [ ] Don't skip verification steps
- [ ] Commit after each phase
- [ ] Update log after each phase
- [ ] Test links after moving files
- [ ] Verify docs reflect implementation

**After Completion:**
- [ ] All success criteria met
- [ ] All quality checks passed
- [ ] Navigation tested
- [ ] Another person reviewed (if possible)
- [ ] Waited 24-48 hours before deleting backup
- [ ] Merged to main
- [ ] Backup deleted
- [ ] Branch deleted

---

**Total Estimated Time:** ~20 hours

**Recommended Approach:** Execute 1-2 phases per session, with breaks between to avoid mistakes.

---

**Ready to begin? Start with Phase 1 (Backup & Setup).**

