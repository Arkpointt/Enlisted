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
- Use **Camp Activities** for action-based skill XP (training/tasks/social) with fatigue costs
- Docs:
  - **[Duties System](Core/duties-system.md)**
  - **[Camp](UI/camp-tent.md)**
  - **[Formation Training](Core/formation-training.md)**
  - **[Menu Interface](UI/menu-interface.md)** (Camp Activities)

### 6) Get paid (Pay Muster) and manage pay tension
Wages accrue daily with modifiers (culture, wartime, lord wealth). Paid at **pay muster** (~12 days).
- When pay is late, **Pay Tension** builds (0-100) with escalating effects
- At 60+ tension, **free desertion** becomes available (no penalties)
- **Battle loot share** compensates T1-T3 soldiers; T4+ get native loot screens
- Docs:
  - **[Pay System](Core/pay-system.md)**
  - **[Enlistment System](Core/enlistment.md)** (Discharge/Final Muster)

### 7) Lance Life Events
Random events shape your military career:
- **Camp events**: Social, training, supply issues
- **Pay tension events**: Grumbling, theft, confrontation, mutiny
- **Loyal path missions**: Help the lord to reduce tension
- **Consequences**: Heat, discipline, reputation, relations
- Docs:
  - **[Lance Life Events](Core/lance-life-events.md)**
  - **[Camp Life Simulation](Gameplay/camp-life-simulation.md)**

---

## Core Systems

The foundational systems that enable military service.

- **[Enlistment System](Core/enlistment.md)** - Core service mechanics, army following, discharge
- **[Pay System](Core/pay-system.md)** - Wages, pay muster, pay tension, battle loot share, tier-gated loot
- **[Lance Life Events](Core/lance-life-events.md)** - Data-driven events for camp life, training, pay tension, narrative
- **[Duties System](Core/duties-system.md)** - Military roles and assignments with skill bonuses
- **[Formation Training](Core/formation-training.md)** - Automatic formation-based skill XP progression
- **[Lance Assignments](Core/lance-assignments.md)** - Lance roster, personas, culture-specific ranks
- **[Camp Fatigue](Core/camp-fatigue.md)** - Daily fatigue system for activities
- **[Companion Management](Core/companion-management.md)** - Companion behavior during enlistment

---

## User Interface

Menu systems and player interaction interfaces.

- **[Menu Interface](UI/menu-interface.md)** - Main enlisted status menu and navigation
- **[Camp](UI/camp-tent.md)** - Service records, personal retinue (Tier 4+), companion management hub ("My Camp" menu)
- **[Dialog System](UI/dialog-system.md)** - Conversation management with lords
- **[Quartermaster](UI/quartermaster.md)** - Equipment selection UI and variant management

---

## Gameplay Features

Additional gameplay mechanics and player choices.

- **[Temporary Leave](Gameplay/temporary-leave.md)** - 14-day leave system with desertion penalties
- **[Troop Selection](Gameplay/troop-selection.md)** - Real troop choice for promotions and formations
- **[Town Access System](Gameplay/town-access-system.md)** - Settlement exploration and access control
- **[Camp Life Simulation](Gameplay/camp-life-simulation.md)** - Condition-driven camp logistics, morale shocks, delayed pay/IOUs, and Quartermaster mood/stockouts (phased plan)
- **[Lance Life](Gameplay/lance-life.md)** - Lance-driven camp stories: drills, scrounging, corruption/contraband, and escalation/condition consequences (data-driven, modular)

---

## Missions

Special mission types available to enlisted players.

- **[Recon Mission](Missions/recon-mission.md)** - Tier 4+ scouting missions with intel gathering, risk/reward mechanics, and enemy encounters

---

## Technical Systems

Low-level systems that ensure stability and prevent issues.

- **[Encounter Safety](Technical/encounter-safety.md)** - Map encounter crash prevention, reserve watchdog, prisoner-state aware activation, besiege-menu fix, and naval battle fixes (ship assignment, captain lookup, troop deployment, AI behavior creation for shipless formations)
- **[Formation Assignment](Technical/formation-assignment.md)** - Battle formation assignment and position teleportation to lord's deployment

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
- Tier 1+: Enlistment, Duties, Formation Training, Camp Activities, Pay System
- Tier 2+: Quartermaster, Duty Requests
- Tier 4+: Native Loot Screens, Personal Retinue, Recon Mission

**By Category:**
- **Progression**: Enlistment, Pay System, Duties, Formation Training
- **Party Management**: Companion Management, Personal Retinue
- **Events**: Lance Life Events, Pay Tension Events, Loyal Path Missions
- **Interaction**: Dialog System, Menu Interface, Town Access, Camp
- **Equipment**: Quartermaster, Tier-Gated Loot
- **Special**: Temporary Leave, Recon Mission
- **Technical/Safety**: Encounter Safety, Formation Assignment
