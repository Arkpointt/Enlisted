# Quartermaster Equipment & Supply System

**Summary:** This specification describes how company supply levels and unit reputation affect equipment access, pricing, and the Officers Armory. It covers baggage checks, quartermaster pricing/buyback, supply gates, premium gear access, and discharge gear return mechanics.

**Status:** ✅ Complete  
**Last Updated:** 2025-12-22 (Phase 5: UI enhancements with tooltips and upgrade indicators)  
**Related Docs:** [Quartermaster System](quartermaster-system.md), [Quartermaster Conversation Refactor](quartermaster-conversation-refactor.md)

---

## Index

- [Overview](#overview)
- [Baggage Checks & Contraband](#baggage-checks--contraband)
- [Quartermaster Pricing & Buyback](#quartermaster-pricing--buyback)
- [Supply Gate (Company Needs)](#supply-gate-company-needs)
- [Officers Armory (Premium Gear)](#officers-armory-premium-gear)
- [Discharge & Gear Return](#discharge--gear-return)

---

## Overview

The Quartermaster system manages the flow of equipment based on three core factors:
1.  **Company Supplies**: Gates access to new equipment (can you draw from the armory?).
2.  **Officer Reputation**: Affects the gold cost and unlocks the elite armory.
3.  **Soldier Reputation**: Influences unit-wide events and support during inspections.

---

## Baggage Checks & Contraband

Periodic inspections are held to ensure soldiers are not hoarding contraband or selling issued gear.

### Contraband Definition
-   Stolen military equipment (looted from friendly or neutral parties).
-   High-value civilian goods (jewelry, fine silks) found in the field.
-   Illegal items marked as contraband.

### The Inspection Process
Every Pay Muster has a 30% chance to trigger a baggage check. The outcome depends on your **Officer Reputation**:
-   **Trusted (60+)**: The Quartermaster may look the other way, allowing you to keep contraband.
-   **Friendly (35-59)**: You may be offered the chance to pay a "processing fee" (bribe) to keep items.
-   **Neutral or Below (<35)**: Items are confiscated, and you face fines or disciplinary action (increased Scrutiny).

---

## Quartermaster Pricing & Buyback

The cost of equipment is dynamic, reflecting your relationship with the unit's leadership.

### Purchase Discounts
Based on **Officer Reputation**:
-   **Trusted (65+)**: 30% discount.
-   **Close (35-64)**: 20% discount.
-   **Friendly (10-34)**: 10% discount.
-   **Neutral (-10 to 9)**: Standard military pricing.
-   **Hostile (<-10)**: Up to 40% markup (gouging).

### Buyback System
You can sell QM-purchased gear back to the unit. The buyback price is always lower than the purchase price to prevent profit exploits, but high reputation minimizes the loss (ranging from 30% to 65% of town value).

---

## Supply Gate (Company Needs)

Equipment access is strictly gated by the **Supplies** company need.

-   **Critical (<30%)**: The armory is **BLOCKED**. No new equipment can be issued until resupply occurs.
-   **Low (30-49%)**: Equipment is available, but the Quartermaster warns of shortages, and prices may be elevated.
-   **Stable (50%+)**: Full access to all tiered equipment.

---

## Officers Armory (Premium Gear)

Elite gear and specialized loadouts are reserved for those who have proven themselves.

### Access Requirements
-   **Rank**: Tier 5 or higher (Officer).
-   **Officer Reputation**: 60 or higher.
-   **Soldier Reputation**: 50 or higher.

### Benefits
The Officers Armory provides access to:
-   Equipment 1-2 tiers above your current rank.
-   Items with quality modifiers (Fine, Masterwork, Reinforced).
-   Specialized mounts and rare weaponry.

---

## Discharge & Gear Return

Upon leaving service (Honorable Discharge or Retirement), all Quartermaster-purchased items must be returned.
-   **Automated Confiscation**: QM-marked items are removed from your inventory during the discharge process.
-   **Missing Gear**: If issued gear has been sold or lost, the replacement cost is deducted from your final pay or pension.
-   **Looted Gear**: Any items not purchased from the Quartermaster (battlefield loot) are yours to keep.
