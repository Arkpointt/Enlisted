# docs audit (pending / gaps)

**last updated:** 2025-12-17

This file exists to answer: **“What’s behind, what’s missing, and what is the current source of truth?”**

---

## Current entry points (source of truth)

- **Start here (core loop overview):** `docs/Features/core-gameplay.md`
- **Feature index (deep links):** `docs/Features/index.md`
- **status (what’s done / next):** `docs/ImplementationPlans/implementation-status.md`
- **roadmap (dependency / ordering):** `docs/ImplementationPlans/master-implementation-roadmap.md`
- **story systems:** `docs/StoryBlocks/story-systems-master.md`
- **decision events:** `docs/StoryBlocks/decision-events-spec.md`

## index

- [current entry points (source of truth)](#current-entry-points-source-of-truth)
- [what we fixed in this audit pass](#what-we-fixed-in-this-audit-pass)
- [known documentation gaps (still behind)](#known-documentation-gaps-still-behind)
- [next consolidation targets (recommended)](#next-consolidation-targets-recommended)

---

## What we fixed in this audit pass

- **Broken doc links**
  - Removed references to non-existent `pay-system-rework.md` and `implementation-roadmap.md`.
  - Updated the roadmap to stop pointing at old implementation-plan files (which are now deleted).
- **Encoding cleanup (readability)**
  - Fixed common “mojibake” sequences in frequently read gameplay docs and the menu interface doc.
- **New navigation**
  - Added `docs/index.md` as a top-level “start here”.
  - Added `docs/Features/core-gameplay.md` as the high-level consolidated view of the core loop.

---

## Known documentation gaps (still behind)

These areas exist in code but are not documented as first-class pages (or are only mentioned briefly):

- **Schedule system**
  - Code: `src/Features/Schedule/*`
  - Data: `ModuleData/Enlisted/schedule_config.json`
- **Command Tent systems (service record, retinue lifecycle, etc.)**
  - Code: `src/Features/CommandTent/*`
- **Player conditions**
  - Code: `src/Features/Conditions/*`
- **News / interface behaviors**
  - Code: `src/Features/Interface/Behaviors/EnlistedNewsBehavior.cs`
- **Combat behaviors beyond formation assignment**
  - Code: `src/Features/Combat/Behaviors/*`

---

## Next consolidation targets (recommended)

To keep docs “few files, easy to maintain”, the next useful consolidation is:

- **One “Systems Reference” page** for:
  - Schedule
  - Conditions
  - Command Tent (records/retinue)
  - Interface/News
  - Combat behaviors

That page should link into existing feature docs instead of duplicating them.


