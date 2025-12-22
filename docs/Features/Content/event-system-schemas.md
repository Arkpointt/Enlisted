# Event System Schemas

**Summary:** Comprehensive JSON schema definitions for the event and orders system in v0.9.0. This document specifies the structure for events, orders, escalation, reputation, company needs, promotions, trigger conditions, and validation rules used throughout the content delivery pipeline.

**Status:** ✅ Current  
**Last Updated:** 2025-12-22  
**Related Docs:** [Content System Architecture](content-system-architecture.md), [Event Catalog](../../Content/event-catalog-by-system.md)

---

## Index

- [Schema Version](#schema-version)
- [Event Schema](#event-schema)
- [Order Schema](#order-schema)
- [Escalation & Reputation Schema](#escalation--reputation-schema)
- [Company Needs Schema](#company-needs-schema)
- [Promotion Schema](#promotion-schema)
- [Trigger Conditions](#trigger-conditions)
- [Common Types & Valid Names](#common-types--valid-names)
- [Validation Rules](#validation-rules)

---

## Schema Version

All data files must include a schema version for validation and migration support:

```json
{
  "schemaVersion": 2,
  "...": "..."
}
```

---

## Event Schema

Events are narrative moments or player-initiated activities that shape your military career.

### Structure
- **Metadata**: Tier range, primary role, and campaign context.
- **Delivery**: Automatic (triggered by conditions) or Player-Initiated (via menu).
- **Triggers**: Complex logical conditions (all/any) and escalation requirements.
- **Requirements**: Skill, trait, and reputation gates.
- **Content**: Localized text and branching options with varied outcomes.

---

## Order Schema

Orders are mission-driven directives issued by the chain of command. They differ from events in their frequency and the direct "Accept/Decline" flow.

### Structure
- **Issuer**: Auto-determined by rank or specific (Sergeant, Captain, Lord).
- **Frequency**: Base days modified by campaign tempo (e.g., faster during war).
- **Consequences**: Explicit Success, Failure, and Decline outcomes affecting reputation and company needs.
- **Strategic Tags**: Used to match orders to the current **Strategic Context** (e.g., `assault`, `scout`, `preparation`). Inappropriate tags for a context will filter out orders.

---

## Escalation & Reputation Schema

Tracks the persistent standing of the player across three reputation tracks and two escalation meters.

- **Reputation**: Lord, Officer, and Soldier (-100 to +100).
- **Escalation**: Scrutiny and Discipline (0 to 100).
- **Thresholds**: Defined levels (e.g., "Trusted", "Disliked") that trigger specific events.

---

## Company Needs Schema

Defines the five core metrics for the unit's state.

- **Meters**: Supplies, Readiness, Morale, Rest, Equipment (0 to 100).
- **Decay**: Daily consumption rates based on activity and context.
- **Warnings**: Localized messages triggered when needs fall below 30%.

---

## Promotion Schema

Governs the transition between the nine enlistment tiers.

- **Eligibility**: Minimum days in rank, XP, battles, and reputation thresholds.
- **Proving Event**: A mandatory mission or choice required to finalize the promotion.
- **Ceremony**: Narrative text and rewards (gold, relation) awarded upon completion.

---

## Trigger Conditions

Valid condition strings used across the system:
- **Status**: `is_enlisted`, `ai_safe`.
- **Location**: `in_settlement`, `not_in_settlement`.
- **Rank**: `tier_min:{n}`, `tier_max:{n}`.
- **Role**: `has_role:scout`, `has_role:medic`, etc.
- **Traits**: `has_trait_min:{trait}:{value}`.
- **Needs**: `company_need_min:{need}:{value}`.

---

## Common Types & Valid Names

### Skill Names
`OneHanded`, `TwoHanded`, `Polearm`, `Bow`, `Crossbow`, `Throwing`, `Riding`, `Athletics`, `Scouting`, `Tactics`, `Roguery`, `Charm`, `Leadership`, `Trade`, `Steward`, `Medicine`, `Engineering`.

### Trait Names
`Honor`, `Valor`, `Mercy`, `Generosity`, `Calculating`, `Commander`, `Surgeon`, `Scout`, `Rogue`, `Engineer`.

---

## Validation Rules

1.  **Unique IDs**: All event and order IDs must be globally unique.
2.  **Reputation Range**: Values must be within -100 to 100.
3.  **Need Range**: Company needs must be within 0 to 100.
4.  **Logical Ranges**: Tier and level ranges must satisfy `min <= max`.
5.  **Linked Events**: All referenced event IDs in follow-up triggers must exist.
