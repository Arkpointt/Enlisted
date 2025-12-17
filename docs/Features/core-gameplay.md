# Enlisted – Core Gameplay (Consolidated)

**last updated:** 2025-12-17  
**Goal:** One maintained document for the mod’s core gameplay loop and the systems that implement it.

This file replaces the older split docs under:
- `docs/Features/Core/*.md`
- `docs/Features/Gameplay/*.md`

Those pages now link here to avoid drift.

---

## index

- [overview](#overview)
- [system map (code + data)](#system-map-code--data)
- [enlistment](#enlistment)
- [duties + formation training](#duties--formation-training)
- [pay system (muster ledger, pay tension, ious)](#pay-system-muster-ledger-pay-tension-ious)
- [lance identity (assignments, ranks, personas)](#lance-identity-assignments-ranks-personas)
- [events (lance life events + storypacks) + decision events](#events-lance-life-events--storypacks--decision-events)
- [companions + retinue](#companions--retinue)
- [fatigue + conditions](#fatigue--conditions)
- [town access + temporary leave](#town-access--temporary-leave)
- [content authoring (quick rules)](#content-authoring-quick-rules)
- [acceptance checklist (core loop)](#acceptance-checklist-core-loop)

## Overview

Enlisted turns Bannerlord into a “soldier career” loop:

1) **Enlist** with a lord (kingdom or minor faction).  
2) **Live the routine**: duties, camp actions, fatigue, conditions, lance identity, events.  
3) **Fight as a squad**: player + companions (+ retinue at higher tiers) are assigned into one formation.  
4) **Get paid** via the muster ledger + pay muster, and deal with pay tension.  
5) **Progress** through tiers via proving events (not just raw XP).  
6) **Leave** via temporary leave, transfer, managed discharge (Final Muster), or desertion.

---

## System map (code + data)

### Core code areas

- **Enlistment + state**: `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`
- **Pay muster / incidents**: `src/Features/Enlistment/Behaviors/EnlistedIncidentsBehavior.cs`
- **Duties + formation training**: `src/Features/Assignments/Behaviors/EnlistedDutiesBehavior.cs`
- **Promotion / proving events**: `src/Features/Ranks/Behaviors/PromotionBehavior.cs`
- **Lances (identity + UI + stories)**: `src/Features/Lances/*`
- **Events (lance life + decisions)**: `src/Features/Lances/Events/*`
- **Camp + camp UI**: `src/Features/Camp/*`
- **Command tent systems (service record, retinue lifecycle, etc.)**: `src/Features/CommandTent/*`
- **Combat formation assignment**: `src/Features/Combat/Behaviors/EnlistedFormationAssignmentBehavior.cs`
- **Town access / menus**: `src/Features/Interface/Behaviors/EnlistedMenuBehavior.cs`
- **Quartermaster + troop selection popup**: `src/Features/Equipment/*`
- **Player conditions (injury/illness/etc.)**: `src/Features/Conditions/*`

### Core data sources (ModuleData)

- **Config**: `ModuleData/Enlisted/enlisted_config.json`
- **Progression**: `ModuleData/Enlisted/progression_config.json`
- **Duties**: `ModuleData/Enlisted/duties_system.json`
- **Schedule**: `ModuleData/Enlisted/schedule_config.json`
- **Activities**: `ModuleData/Enlisted/Activities/activities.json`
- **Events catalog**: `ModuleData/Enlisted/Events/*.json`
- **Decision events**: `ModuleData/Enlisted/Events/events_decisions.json`, `events_player_decisions.json`
- **Story packs (legacy-but-shipping layer)**: `ModuleData/Enlisted/StoryPacks/LanceLife/*.json`
- **Lances**: `ModuleData/Enlisted/lances_config.json`, `LancePersonas/name_pools.json`

---

## Enlistment

### What it does

- Lets the player **enter service** under a lord and follow their movements.
- Hides the player party on the campaign map while enlisted.
- Handles fragile campaign states safely: battles, encounters, captivity, lord death/capture, army disbanding.
- Supports **minor faction enlistment** (war stance mirroring + desertion cooldowns).

### Key gameplay beats

- **Bag check** happens after enlistment (deferred) and prevents “walk in with endgame kit” abuse.
- **Tier progression** is gated by proving events at key promotions.
- **Managed discharge** is requested via Camp; it resolves at pay muster (Final Muster path).

### Where it lives

- `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`
- `src/Features/Enlistment/Behaviors/EnlistedIncidentsBehavior.cs`
- `src/Features/Conversations/Behaviors/EnlistedDialogManager.cs`

---

## Duties + formation training

### What it does

- Duty system is **data-driven** and gives:
  - daily skill XP
  - wage multiplier surface for the pay system
  - tokens for event triggers (e.g., `has_duty:{id}`)
- Formation training is the “automatic daily XP by formation” layer and is configured in `duties_system.json`.

### Where it lives

- `src/Features/Assignments/Behaviors/EnlistedDutiesBehavior.cs`
- `ModuleData/Enlisted/duties_system.json`

---

## Pay system (muster ledger, pay tension, IOUs)

### What it does

- Wages accrue into a **muster ledger** and are paid out at pay muster events.
- When pay is late/disrupted, **Pay Tension** rises and unlocks pressure valves:
  - “Desperate Measures” (corruption)
  - “Help the Lord” (loyalty)
  - free desertion at high tension
- Supports an **IOU/promissory** path (retry sooner without losing owed backpay).

### Where it lives

- Pay state is owned by enlistment behavior: `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`
- Pay muster UI/options: `src/Features/Enlistment/Behaviors/EnlistedIncidentsBehavior.cs`

---

## Lance identity (assignments, ranks, personas)

### What it does

- Gives the player a lance identity early, then finalizes it later.
- Tracks culture-specific rank titles and (optional) text-only “personas” for story flavor.

### Where it lives

- `src/Features/Assignments/Core/LanceRegistry.cs`
- `src/Features/Lances/Personas/*`
- `ModuleData/Enlisted/lances_config.json`

---

## Events (Lance Life Events + StoryPacks) + Decision Events

### Two “content layers” exist (both shipping)

- **Events Catalog**: `ModuleData/Enlisted/Events/*.json`
  - supports automatic delivery and player-initiated delivery
  - supports inquiry / menu / incident delivery channels
- **StoryPacks**: `ModuleData/Enlisted/StoryPacks/LanceLife/*.json`
  - a legacy-but-shipping daily-tick story layer
  - governed by a strict authoring contract (schema, localization, validation)

### Decision Events

Decision Events are the CK3-like “decisions” system used for player-initiated and pushed decisions.

- **spec**: `docs/StoryBlocks/decision-events-spec.md`
- **Code**: `src/Features/Lances/Events/Decisions/*`
- **Data**: `ModuleData/Enlisted/Events/events_decisions.json`, `events_player_decisions.json`

---

## Companions + retinue

### Companions

- Companions stay with the player during enlistment.
- They can be toggled “fight” vs “stay back” for battles.

### Retinue (command track)

- Higher tiers grant a personal force with lifecycle handling, casualty tracking, and provisioning integrations.

### Where it lives

- Companion settings / service record helpers: `src/Features/CommandTent/Core/*`
- Retinue lifecycle and systems: `src/Features/CommandTent/*`

---

## Fatigue + conditions

### Fatigue

Fatigue is a stamina-like budget consumed/restored by certain actions and options (activities, some event options, etc.).

### Player conditions

Injury/illness/exhaustion (and treatment gating) live under the Conditions feature area and are used by some event outcomes.

### Where it lives

- Conditions: `src/Features/Conditions/*`

---

## Town access + temporary leave

### Town access

The town/castle access system provides safe settlement exploration while the player party is normally hidden/inactive during enlistment.

### Temporary leave

Leave temporarily suspends active service with a hard time limit; expiring leave is treated as abandonment (penalties apply).

---

## Content authoring (quick rules)

- **add/modify decision events**: follow `docs/StoryBlocks/decision-events-spec.md`.
- **Add/modify non-decision events**: edit `ModuleData/Enlisted/Events/*.json` and corresponding localization in `ModuleData/Languages/enlisted_strings.xml`.
- **Add/modify story packs**: follow the authoring contract at `docs/Features/Core/story-pack-contract.md`.
- **Keep trigger vocabulary stable**: trigger tokens are code-defined and must not drift.

---

## Acceptance checklist (core loop)

- Enlist/leave/discharge flows do not crash across:
  - battle entry/exit
  - encounters
  - captivity
  - lord death/capture / army disband
  - naval travel/battles (when applicable)
- Pay muster pays, IOU path works, pay tension gates menus correctly.
- Duties apply benefits and expose stable trigger tokens for events.
- Events/decisions load, validate, and fail-safe skip invalid content.


