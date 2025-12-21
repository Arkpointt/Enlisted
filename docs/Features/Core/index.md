# Enlisted Documentation Index

**last updated:** December 20, 2025
**Mod Version:** v0.9.0
**Game Target:** v1.3.12

This is the **single entry point** for all Enlisted mod documentation, reflecting the Native Interface & Identity update.

## Index

- [Quick Links](#quick-links)
- [How It All Works (End-to-End)](#how-it-all-works-end-to-end)
- [Core Systems](#core-systems)
- [User Interface](#user-interface)
- [Gameplay Features](#gameplay-features)
- [Technical Systems](#technical-systems)
- [Story Content & Events](#story-content--events)

---

## Quick Links

| What You Need | Where To Go |
|---------------|-------------|
| **Understand the core gameplay** | [Core Gameplay (Consolidated)](core-gameplay.md) |
| **Understand the three pillars** | [Master Implementation Plan](../ImplementationPlans/enlisted-interface-master-plan.md) |
| **Add story content** | [Story Blocks Master Reference](../../StoryBlocks/story-blocks-master-reference.md) |
| **Check implementation status** | [Implementation Status](../ImplementationPlans/enlisted-interface-master-plan.md#implementation-status) |

---

## How It All Works (End-to-End)

### 1) Enlist
Enlist with a lord to begin your career. Your party is hidden, and you follow the lord's movements on the campaign map.
- Doc: **[Enlistment System](enlistment.md)**

### 2) Initial Inspection: Bag Check
About one hour after enlisting, you must stash or liquidate your personal gear to adhere to military regulations.
- Doc: **[Enlistment System](enlistment.md)** (Behavior section)

### 3) Receive Orders
Instead of passive assignments, you receive explicit **Orders** from the chain of command every few days. Success builds your reputation; failure damages it.
- Doc: **[Core Gameplay](core-gameplay.md)** (Orders System section)

### 4) build Your Identity
Your role (Scout, Medic, Officer) emerges dynamically from your native skills and traits. We track your reputation with the Lord, the Officers, and your fellow Soldiers.
- Doc: **[Core Gameplay](core-gameplay.md)** (Emergent Identity section)

### 5) Strategic Context & War Stance
The unit's experience is now strategically aware. Your orders and events reflect the lord's actual campaign plans, whether you're in a "Grand Campaign" offensive or a "Winter Camp" rest period.
- Doc: **[Core Gameplay](core-gameplay.md)** (Strategic Context section)

### 6) Manage Company Needs
The unit's effectiveness is tracked via five core needs: Readiness, Morale, Supplies, Equipment, and Rest. The system now predicts upcoming requirements based on strategic plans.
- Doc: **[Camp Life Simulation](../Gameplay/camp-life-simulation.md)**

### 6) Visit the Quartermaster
The Quartermaster is your primary contact for gear and provisions. Access is gated by your reputation and the unit's supply levels.
- Doc: **[Quartermaster Hero System](quartermaster-hero-system.md)**

---

## Core Systems

The foundational systems that enable the Enlisted experience.

- **[Enlistment System](enlistment.md)** - Service mechanics, army following, and discharge.
- **[Pay System](pay-system.md)** - Wages, pay muster, and pay tension.
- **[Company Events](company-events.md)** - Role-based narrative and social events.
- **[Quartermaster Hero System](quartermaster-hero-system.md)** - Persistent NPC for equipment and logistics.
- **[Core Gameplay](core-gameplay.md)** - Consolidated overview of orders, identity, and progression.

---

## User Interface

Enlisted uses the native Bannerlord Game Menu system for all interactions.

- **[Native Menus](core-gameplay.md#native-game-menu-interface)** - Overview of the menu hub.
- **[Reports & News](core-gameplay.md#native-game-menu-interface)** - Detailed feedback on unit status and player actions.

---

## Gameplay Features

- **[Temporary Leave](../Gameplay/temporary-leave.md)** - Suspend service while preserving progression.
- **[Camp Life Simulation](../Gameplay/camp-life-simulation.md)** - Detailed tracking of unit-wide company needs.
- **[Provisions System](../Gameplay/provisions-system.md)** - Manage morale and fatigue through rations.
- **[Town Access](../Gameplay/town-access-system.md)** - Safe exploration of settlements while enlisted.

---

## Technical Systems

- **[Encounter Safety](../Technical/encounter-safety.md)** - Crash prevention and state management.
- **[Formation Assignment](../Technical/formation-assignment.md)** - Battle deployment logic.

---

## Story Content & Events

- **[Story Blocks Master Reference](../../StoryBlocks/story-blocks-master-reference.md)** - Single source of truth for all story systems and event schemas.
