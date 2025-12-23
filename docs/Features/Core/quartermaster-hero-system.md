# Quartermaster Hero System

**Summary:** The quartermaster hero system creates persistent NPC quartermasters for each lord, transforming equipment management into character relationships. Quartermasters have unique archetypes (veteran, merchant, bookkeeper, etc.), build reputation with the player, and reflect company supply needs through their dialogue and services.

**Status:** ✅ Current  
**Last Updated:** 2025-12-23  
**Related Docs:** [Quartermaster System](../Equipment/quartermaster-system.md)

---

## Index

- [Overview](#overview)
- [Company Needs Integration](#company-needs-integration)
- [Quartermaster Archetypes](#quartermaster-archetypes)
- [Relationship & Reputation](#relationship--reputation)
- [Provisions System](#provisions-system)
- [Accessing the Quartermaster](#accessing-the-quartermaster)

---

## Overview

- **Persistent NPC**: You deal with the same quartermaster throughout your service with a lord.
- **Archetypes**: Veterans, Merchants, Scoundrels, or Bookkeepers—each with unique dialogue and advice.
- **Context-Aware**: Reacts to your rank, reputation, and the current state of the company's supplies.

---

## Company Needs Integration

In v0.9.0, the Quartermaster's services are directly tied to the unit's **Company Needs** and the broader **Strategic Context**:

- **Strategic Context Awareness**: The Quartermaster's inventory and dialogue change based on the current operation. During a "Grand Campaign," he prioritizes arrow bundles and repair kits. During "Winter Camp," he focuses on warm clothing and luxury provisions.
- **Supplies Threshold**: If the unit's **Supplies** need falls below 30%, the Quartermaster will restrict access to high-tier gear and rations.
- **Equipment Threshold**: If the unit's **Equipment** need is low, the cost of maintenance and repairs increases.
- **Mood**: The Quartermaster’s dialogue reflects the current **Morale** and **Readiness** of the company. During a "Last Stand," his tone becomes grim and urgent.

---

## Quartermaster Archetypes

Each unit is assigned a Quartermaster with a specific personality:
-   **Veteran**: Pragmatic and no-nonsense. Offers survival tips and warnings about unit readiness.
-   **Merchant**: Opportunistic and trade-minded. Focused on costs and logistics strain.
-   **Bookkeeper**: Bureaucratic and procedural. Obsessed with ledgers and supply counts.
-   **Scoundrel**: Sly and creative. Hints at "off-the-books" solutions when supplies are critical.

---

## Relationship & Reputation

Your standing with the Quartermaster is influenced by your broader **Officer Reputation** and direct interactions:

- **Officer Reputation**: High reputation with officers (60+) unlocks better prices and respect from the Quartermaster.
- **Interactions**: Regular equipment maintenance and successful resupply orders improve your personal relationship with the Quartermaster.
- **Benefits**: High trust grants discounts (up to 15%) and access to "reserved" equipment pools.

---

## Provisions System

The Quartermaster manages the unit's rations, which impact player **Morale** and **Fatigue**:

- **Supplemental Rations**: Basic food for 1 day.
- **Officer's Fare**: High-quality food for 2 days; provides an immediate fatigue recovery boost.
- **Commander's Feast**: Top-tier food for 3 days; maximum morale and fatigue benefits.

---

## Accessing the Quartermaster

Access is handled through the **Native Game Menu** system:

1.  **Enlisted Status** (Main Menu) -> **Visit Quartermaster**.
2.  Triggers a native **Conversation** with the Quartermaster NPC.
3.  Options:
    -   "I need equipment" -> Opens the equipment management screen.
    -   "I want to sell some gear" -> Opens the sell screen.
    -   "I need better provisions" -> Opens the rations menu.
    -   "How are the unit's supplies holding up?" -> Provides a report based on **Company Needs**.
    -   "I'll be going" -> Exits the conversation.
