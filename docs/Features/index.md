# enlisted documentation index

**last updated:** december 17, 2025

This is the **single entry point** for all Enlisted mod documentation.

---

## index

- [quick links](#quick-links)
- [how it all works (end-to-end)](#how-it-all-works-end-to-end)
- [core systems](#core-systems)
- [user interface](#user-interface)
- [gameplay features](#gameplay-features)
- [missions](#missions)
- [technical systems](#technical-systems)
- [story content & events](#story-content--events)
- [implementation status](#implementation-status)
- [spec structure](#spec-structure)
- [quick reference](#quick-reference)

## Quick Links

| What You Need | Where To Go |
|---------------|-------------|
| **Understand the core gameplay (recommended)** | [Core Gameplay (Consolidated)](core-gameplay.md) |
| **Understand the mod (legacy index page)** | [How It All Works](#how-it-all-works-end-to-end) (below) |
| **Add story content** | [story systems master](../StoryBlocks/story-systems-master.md) |
| **Add decision events** | [decision events spec](../StoryBlocks/decision-events-spec.md) |
| **Check implementation status** | [implementation status](../ImplementationPlans/implementation-status.md) |
| **See full roadmap** | [master roadmap](../ImplementationPlans/master-implementation-roadmap.md) |

---

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

### 4) Visit the Quartermaster (NPC Hero)
Talk to the quartermaster to access equipment, provisions, and advice:
- **Equipment**: Buy/sell formation-appropriate gear with culture variants
- **Provisions**: Purchase rations for morale and fatigue benefits
- **Retinue Provisioning** (T7+): Feed your personal soldiers
- **Relationship**: Build trust for discounts (5-15% off at high relationship)
- **PayTension Dialogue**: Get archetype-specific advice when pay is late
- Docs: **[Quartermaster Hero System](Core/quartermaster-hero-system.md)**, **[Quartermaster UI](UI/quartermaster.md)**, **[Provisions System](Gameplay/provisions-system.md)**

### 5) Day-to-day: Duties, camp actions, and progression
- Pick duties/roles for bonuses and wage modifiers
- Use Camp (“Camp”) for your service record, retinue (Tier 4+), and discharge actions
- Use **Camp Activities** for action-based skill XP (training/tasks/social) with fatigue costs
- Docs:
  - **[Duties System](Core/duties-system.md)**
  - **[Camp](UI/camp-tent.md)**
  - **[Formation Training](Core/formation-training.md)**
  - **[Menu Interface](UI/menu-interface.md)** (Camp Activities)

### 6) Get paid (Pay Muster) and manage pay tension
Wages accrue daily with modifiers (culture, wartime, lord wealth). Paid at **pay muster** (~12 days).
- When pay is late, **Pay Tension** builds (0-100) with escalating effects
- **Desperate Measures** (40+ tension): Corruption path - bribe clerk, skim supplies, black market, sell equipment
- **Help the Lord** (40+ tension): Loyalty path - collect debts, escort merchants, negotiate loans, raid enemies
- At 60+ tension, **free desertion** becomes available (no penalties)
- **Battle loot share** compensates T1-T3 soldiers; T4+ get native loot screens
- Docs:
  - **[Pay System](Core/pay-system.md)**
  - **[PayTension Action Menus](Gameplay/paytension-action-menus.md)**
  - **[Enlistment System](Core/enlistment.md)** (Discharge/Final Muster)

### 7) Lance Life Events & Character Interactions
Random events and NPC interactions shape your military career:
- **Lance Life Events**: Camp events, training, supply issues, pay tension events
- **Quartermaster Dialogue**: Archetype-specific advice during financial crisis
- **Action Menus**: Choose corruption or loyalty paths when pay is late
- **Consequences**: Heat, discipline, reputation, relations, tension reduction
- Docs:
  - **[Lance Life Events](Core/lance-life-events.md)**
  - **[Camp Life Simulation](Gameplay/camp-life-simulation.md)**
  - **[Quartermaster Hero System](Core/quartermaster-hero-system.md)**
  - **[PayTension Action Menus](Gameplay/paytension-action-menus.md)**

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
- **[Quartermaster Hero System](Core/quartermaster-hero-system.md)** - Persistent NPC quartermaster with personality, relationship system, and PayTension dialogue
- **[Retinue System](Core/retinue-system.md)** - Commander's personal force (T7-T9) with companion management

---

## User Interface

Menu systems and player interaction interfaces.

- **[Menu Interface](UI/menu-interface.md)** - Main enlisted status menu and navigation
- **[Camp](UI/camp-tent.md)** - Service records, personal retinue (Tier 4+), companion management hub ("Camp" menu)
- **[Dialog System](UI/dialog-system.md)** - Conversation management with lords
- **[Quartermaster](UI/quartermaster.md)** - Equipment selection UI and variant management

---

## Gameplay Features

Additional gameplay mechanics and player choices.

- **[Temporary Leave](Gameplay/temporary-leave.md)** - 14-day leave system with desertion penalties
- **[Troop Selection](Gameplay/troop-selection.md)** - Real troop choice for promotions and formations
- **[Town Access System](Gameplay/town-access-system.md)** - Settlement exploration and access control
- **[Camp Life Simulation](Gameplay/camp-life-simulation.md)** - Condition-driven camp logistics, morale shocks, delayed pay/IOUs, and Quartermaster mood/stockouts
- **[Lance Life](Gameplay/lance-life.md)** - Lance-driven camp stories: drills, scrounging, corruption/contraband, and escalation/condition consequences (data-driven, modular)
- **[Provisions System](Gameplay/provisions-system.md)** - Personal rations and retinue provisioning with morale/fatigue benefits
- **[PayTension Action Menus](Gameplay/paytension-action-menus.md)** - Desperate Measures (corruption) and Help the Lord (loyalty) when pay is late

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

## Story Content & Events

For adding or modifying story content:

- **[story systems master](../StoryBlocks/story-systems-master.md)** - consolidated reference for all story systems, escalation mechanics, and content indexes
- **[decision events spec](../StoryBlocks/decision-events-spec.md)** - ck3-style decision events (active development)

## Implementation Status

Track implementation progress:

- **[implementation status](../ImplementationPlans/implementation-status.md)** - current state of all tracks
- **[master roadmap](../ImplementationPlans/master-implementation-roadmap.md)** - full implementation roadmap

## Spec Structure

Feature pages follow this structure:

- **Overview**: One sentence summary
- **Purpose**: Why it exists
- **Inputs/Outputs**: Data flow
- **Behavior**: How it works
- **Edge Cases**: Error handling
- **Acceptance Criteria**: Verification checklist

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
