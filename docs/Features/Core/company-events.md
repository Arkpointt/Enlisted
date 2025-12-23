# Company Events

**Summary:** Data-driven, role-based event system that delivers camp life moments, training opportunities, reputation shifts, and narrative choices during enlisted service. Events emerge based on player role (Scout, Medic, NCO, etc.) and campaign context, delivered through camp menu, automatic inquiries, or map incidents.

**Status:** ✅ Current  
**Last Updated:** 2025-12-22  
**Related Docs:** [Content System Architecture](../Content/content-system-architecture.md), [Core Gameplay](core-gameplay.md)

---

## Index

- [Overview](#overview)
- [Delivery Channels](#delivery-channels)
- [Role-Based Events](#role-based-events)
- [Event Types](#event-types)
- [Integration](#integration)

---

## Overview

Enlisted v0.9.0 uses a unified event delivery pipeline that is **Role-Based** and **Context-Aware**. Events are no longer tied to a rigid schedule but emerge based on your role in the unit and the current campaign situation.

### Delivery Channels

| Channel | Description |
|---------|-------------|
| `menu` | Player-initiated via the **Camp** activities menu. |
| `inquiry` | Automatic popup for narrative or consequence delivery. |
| `incident` | Native moment events that occur on the map (e.g., Bag Check). |

---

## Event Packs

Events are organized into packs by context and role:

| Pack | Context / Role | Purpose |
|------|----------------|---------|
| `camp_events.json` | General Camp Life | Daily life, social interactions, gambling, storytelling. |
| `events_general.json` | Universal Soldier | General enlisted experiences, muster, daily duties. |
| `events_escalation_thresholds.json` | Crisis Triggers | Scrutiny, Discipline, and Medical threshold events. |
| `events_onboarding.json` | First Enlistment | Guaranteed first-day experiences for new enlisted. |
| `events_promotion.json` | Rank Progression | Promotion opportunities, proving events, leadership tests. |
| `events_training.json` | Training Activities | Training-related events and progression. |
| `events_retinue.json` | Commander (T7+) | Retinue management events for high-rank commanders. |
| `events_duty_*.json` | Role-Specific | Duty events for scouts, medics, engineers, etc. (10 files). |
| `events_pay_*.json` | Payment Context | Pay tension, loyalty, and mutiny events (3 files). |
| `Role/scout_events.json` | Scout Role | Reconnaissance, tracking, intelligence gathering. |
| `incidents_*.json` | Map Incidents | Battle, siege, town, village, leaving, waiting contexts (7 files). |

---

## Role-Based Routing

The system detects your **Primary Role** based on your native Bannerlord traits (Commander, Surgeon, ScoutSkills, etc.) and routes events accordingly.

-   **Priority 1**: Escalation/Crisis events (triggered by high Scrutiny or low Discipline).
-   **Priority 2**: Role-specific events matching your current specialization.
-   **Priority 3**: General Soldier events (fallback).

---

## Event Structure

Each event is defined in JSON and supports complex requirements and outcomes.

### Requirements
Events can be gated by:
-   **Rank**: Minimum and maximum enlistment tiers (T1-T9).
-   **Role**: Specific tags like `scout`, `medic`, `officer`.
-   **Context**: Campaign state tags like `war`, `siege`, `peace`, `camp`, `town`.
-   **Reputation**: Minimum thresholds for Lord, Officer, or Soldier reputation.
-   **Traits/Skills**: Minimum levels in native Bannerlord traits and skills.

### Outcomes
Choices within events impact:
-   **Reputation**: LordReputation, OfficerReputation, SoldierReputation.
-   **Company Needs**: Readiness, Morale, Supplies, Equipment, Rest.
-   **Escalation**: Scrutiny and Discipline levels.
-   **Rewards**: Denars, Renown, Items, and Skill/Trait XP.

---

## Trigger System

### Role-Based Triggering
Events fire at an appropriate frequency (~1-2 per day) during `HourlyTickEvent`. The trigger logic:
1.  Rolls for a chance to fire (e.g., 5% per hour).
2.  Determines the player's role and campaign context.
3.  Filters the available event pool for eligible matches.
4.  Selects a random event from the filtered pool.

### Strategic Context Tags
Events are filtered by the unit's current strategic situation:
-   `coordinated_offensive`: The Grand Campaign (allied hosts gathering).
-   `desperate_defense`: The Last Stand (bleeding realm, every sword needed).
-   `raid_operation`: Harrying the Lands (bleeding the enemy's purse).
-   `siege_operation`: Siege Works (active siege engines and encircling walls).
-   `patrol_peacetime`: Riding the Marches (vigilance against bandits).
-   `garrison_duty`: Watching the walls from within a settlement.
-   `recruitment_drive`: Mustering for War (stockpiling and sharpening blades).
-   `winter_camp`: Seasonal rest and daily duties in the snow.

Legacy tactical tags (`war`, `peace`, `siege`, `town`, `outdoor`, `camp`) remain supported for broad categorization.

---

## Effects Application

When an option is selected, effects are applied through centralized managers:
-   **Trait XP**: Awarded via native `TraitLevelingHelper`.
-   **Reputation**: Modified via `EscalationManager`.
-   **Company Needs**: Modified via `CompanyNeedsManager`.

---

## File Locations

| Path | Purpose |
|------|---------|
| `ModuleData/Enlisted/Events/` | Event JSON definitions (loaded recursively). |
| `ModuleData/Enlisted/Decisions/` | Player-initiated decision definitions. |
| `ModuleData/Enlisted/Orders/` | Chain of command order definitions (3 files by tier). |
| `ModuleData/Languages/enlisted_strings.xml` | Localized text and templates. |
| `src/Features/Content/` | Event system implementation (EventCatalog, EventDeliveryManager, etc.). |

**Current Event Files:**
- `camp_events.json` - General camp life events
- `events_general.json` - Universal soldier events
- `events_escalation_thresholds.json` - Escalation (Scrutiny/Discipline/Medical) threshold events
- `events_onboarding.json` - First-enlistment guaranteed events
- `events_promotion.json` - Promotion and proving events
- `events_training.json` - Training-related events
- `events_retinue.json` - Retinue events (T7+ commanders)
- `events_duty_*.json` - Role-specific duty events (scout, medic, engineer, etc.)
- `events_pay_*.json` - Pay-related events (tension, loyalty, mutiny)
- `events_player_decisions.json` - Player-initiated events (legacy)
- `incidents_*.json` - Map incident events (battle, siege, town, village, leaving, waiting, retinue)
- `muster_events.json` - Muster and recruitment events
- `Role/scout_events.json` - Scout role-specific events

The EventCatalog loads all JSON files from Events/ and Decisions/ directories recursively at startup.

