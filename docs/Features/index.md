# Documentation Index

This is the main documentation entry point for the Enlisted mod.

Use it two ways:
- **If you're playing**: start with **How it all works** (the end-to-end loop).
- **If you're modding/debugging**: use the feature pages under each category for deeper details and file references.

---

## How it all works (end-to-end)

### 1) Enlist
- You enlist with a lord and become an embedded soldier: you follow their movements, join their battles, and your wages accrue into the **muster ledger**.
- Doc: **[Enlistment System](Core/enlistment.md)**

### 2) First enlistment: Bag Check → Baggage Train
About **12 in-game hours** after enlisting (when safe), a bag check runs with choices:
- **Stow it all (50g)**: stash inventory + equipped items into the **baggage train** and pay the wagon fee.
- **Sell it all (60%)**: liquidate gear and receive denars.
- **I'm keeping one thing (Roguery 30+)**: attempt to keep a single item.

This is how the mod prevents “walk in with endgame kit on day one” without deleting your gear.
- Doc: **[Enlistment System](Core/enlistment.md)** (Bag Check section)
- Equipment vending is separate: **[Quartermaster](UI/quartermaster.md)**

### 3) Pick your troop identity (Master at Arms)
Your **troop identity** is what the Quartermaster uses to determine what gear variants you’re allowed to buy.
- Doc: **[Troop Selection](Gameplay/troop-selection.md)**

### 4) Buy / sell gear at the Quartermaster
Quartermaster is purchase + buyback:
- Buy troop-appropriate variants (with pricing multipliers)
- Sell eligible items back at buyback rate
- Doc: **[Quartermaster](UI/quartermaster.md)**

### 5) Day-to-day: Duties, camp actions, and progression
- Pick duties/roles for bonuses and wage modifiers
- Use Camp (“My Camp”) for your service record, retinue (Tier 4+), and discharge actions
- Docs:
  - **[Duties System](Core/duties-system.md)**
  - **[Camp](UI/command-tent.md)**
  - **[Formation Training](Core/formation-training.md)**

### 6) Get paid (Pay Muster) and optionally discharge (Final Muster)
Wages accrue daily, then resolve at a **pay muster** event. Discharge is handled via **Final Muster**.
- Doc: **[Enlistment System](Core/enlistment.md)** (Pay Muster + Discharge/Final Muster)

---

## Core Systems

The foundational systems that enable military service.

- **[Enlistment System](Core/enlistment.md)** - Core service mechanics: muster ledger wages, pay muster incidents, XP, kills, pending discharge + Final Muster, grace periods, army following; naval join safety (sea-state sync before PlayerEncounter) and prisoner/encounter-safe discharge recovery
- **[Companion Management](Core/companion-management.md)** - Companion behavior during enlistment and missions
- **[Duties System](Core/duties-system.md)** - Military roles and assignments with skill bonuses
- **[Formation Training](Core/formation-training.md)** - Automatic formation-based skill XP progression
- **[Implementation Roadmap](Core/implementation-roadmap.md)** - Master phased plan for Camp Life + Lance Life development (source-of-truth and scope control)
- **[Phase 4 Corruption Checklist](Core/corruption-phase-checklist.md)** - Acceptance gate for Heat/Discipline/Corruption escalation and consequences

---

## User Interface

Menu systems and player interaction interfaces.

- **[Menu Interface](UI/menu-interface.md)** - Main enlisted status menu and navigation
- **[Camp](UI/command-tent.md)** - Service records, personal retinue (Tier 4+), companion management hub ("My Camp" menu)
- **[Dialog System](UI/dialog-system.md)** - Conversation management with lords
- **[Quartermaster](UI/quartermaster.md)** - Equipment selection UI and variant management

---

## Gameplay Features

Additional gameplay mechanics and player choices.

- **[Temporary Leave](Gameplay/temporary-leave.md)** - 14-day leave system with desertion penalties
- **[Troop Selection](Gameplay/troop-selection.md)** - Real troop choice for promotions and formations
- **[Town Access System](Gameplay/town-access-system.md)** - Settlement exploration and access control
- **[Camp Life Simulation](Gameplay/camp-life-simulation.md)** - Condition-driven camp logistics, morale shocks, delayed pay/IOUs, and Quartermaster mood/stockouts (phased plan)
- **[Lance Life](Gameplay/lance-life.md)** - Lance-driven camp stories: drills, scrounging, corruption/contraband, and theft with consequences (data-driven, modular)

---

## Missions

Special mission types available to enlisted players.

- **[Recon Mission](Missions/recon-mission.md)** - Tier 4+ scouting missions with intel gathering, risk/reward mechanics, and enemy encounters

---

## Technical Systems

Low-level systems that ensure stability and prevent issues.

- **[Encounter Safety](Technical/encounter-safety.md)** - Map encounter crash prevention, reserve watchdog, prisoner-state aware activation, besiege-menu fix, and naval battle fixes (ship assignment, captain lookup, troop deployment, AI behavior creation for shipless formations)
- **[Formation Assignment](Technical/formation-assignment.md)** - Battle formation assignment and position teleportation to lord's deployment
- **[Night Rest & Fatigue](Technical/night-rest-fatigue.md)** - Night halt logic for lords/armies with optional enlisted fatigue when rest is impossible; phased plan and safeguards

---

## Research / Design Drafts

Working notes and design drafts live under `docs/research`:
- **[Research Index](../research/index.md)** - Entry point for prompts, design drafts, and API notes

## Spec Structure

Most feature pages follow this structure (some pages also include extra debugging/API reference sections):

- **Overview**: One sentence summary of what the feature does
- **Purpose**: Why the feature exists and what problem it solves
- **Inputs/Outputs**: What data flows in and out of the system
- **Behavior**: Detailed description of how the feature works
- **Technical Implementation**: Key files, APIs, and code patterns
- **Edge Cases**: What can go wrong and how it's handled
- **Acceptance Criteria**: Checklist for implementation verification

---

## Quick Reference

**By Tier Requirement:**
- Tier 1+: Enlistment, Duties, Formation Training, Troop Selection, Companion Management, Camp (basic)
- Tier 2+: Quartermaster, Menu Interface
- Tier 4+: Personal Retinue (Camp), Recon Mission

**By Category:**
- **Progression**: Enlistment, Duties, Formation Training, Troop Selection
- **Party Management**: Companion Management, Personal Retinue (Camp)
- **Interaction**: Dialog System, Menu Interface, Town Access, Camp
- **Equipment**: Quartermaster
- **Special**: Temporary Leave, Recon Mission
- **Technical/Safety**: Encounter Safety, Formation Assignment, Night Rest & Fatigue
