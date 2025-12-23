# Docs Audit (Pending / Gaps)

**last updated:** December 20, 2025

This file exists to answer: **“What’s behind, what’s missing, and what is the current source of truth?”** reflecting the v0.9.0 update.

## Index

- [Current Entry Points (Source of Truth)](#current-entry-points-source-of-truth)
- [What We Fixed in the v0.9.0 Pass](#what-we-fixed-in-the-v090-pass)
- [Known Documentation Gaps](#known-documentation-gaps)
- [Next Consolidation Targets](#next-consolidation-targets)

---

## Current Entry Points (Source of Truth)

- **Core Gameplay (Consolidated):** `docs/Features/Core/core-gameplay.md`
- **Master Plan (Architectural Truth):** `docs/ImplementationPlans/enlisted-interface-master-plan.md`
- **Story Blocks (Content Truth):** `docs/StoryBlocks/story-blocks-master-reference.md`
- **Blueprint (Technical Standards):** `docs/blueprint.md`

---

## What We Fixed in the v0.9.0 Pass

- **Terminology Cleanup**
  - Replaced "Lance" with "Unit/Company" across all core documents.
  - Replaced "Lance Reputation" with "Soldier Reputation".
- **Three Pillars Integration**
  - Updated all core loop descriptions to reflect the **Orders System**, **Emergent Identity**, and **Native Interface**.
- **Schema Refactor**
  - Deleted deprecated "Lance Life" schemas.
  - Updated `event-system-schemas.md` to reflect version 2.0 (role-based, trait-driven).
- **Redundancy Removal**
  - Deleted legacy "Action Menus" and "Story Pack Contract" documents.
  - Consolidated scattered gameplay features into clear, indexed specifications.

---

## Known Documentation Gaps

These areas exist in code but are not yet fully documented as first-class pages:

- **Player Conditions (Deep Dive)**
  - Code: `src/Features/Conditions/*`
  - Current: Brief mention in `provisions-system.md` and `core-gameplay.md`.
- **Retinue / Service Records**
  - Code: `src/Features/Retinue/*`
  - Current: Integrated into `retinue-system.md` but needs detail on lifetime records.
- **News Behavior API**
  - Code: `src/Features/Interface/Behaviors/EnlistedNewsBehavior.cs`
  - Current: Mentioned in the master plan but lacks a dedicated spec.

---

## Next Consolidation Targets

To keep documentation lean and maintainable:

- **Medical & Welfare**: Consolidate **Player Conditions** and **Medical Menus** into a single "Medical Care" spec.
- **Unit History**: Consolidate **Service Records** and **Lifetime Records** into a "Service Record" spec.
- **Combat Logic**: Create a "Mission Logic" spec for behaviors beyond formation assignment (e.g., encounter guard, battle handling).
