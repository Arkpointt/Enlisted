# Quartermaster (and the Baggage Train)

This page documents the full equipment loop: **Quartermaster buy/sell**, plus the **Enlistment Bag Check** and **Baggage Train** stash that keep early service grounded.

## Index
- [Overview](#overview)
- [Access](#access)
- [Enlistment Bag Check (first enlistment)](#enlistment-bag-check-first-enlistment)
- [Baggage Train (stash access)](#baggage-train-stash-access)
- [Quartermaster: How it works](#quartermaster-how-it-works)
- [Discharge & gear handling](#discharge--gear-handling)
- [Technical notes (high-level)](#technical-notes-high-level)
- [Appendix: Future ideas (not implemented)](#appendix-future-ideas-not-implemented)

## Overview
The **Quartermaster** is the enlisted equipment vendor. It lets you **purchase** troop-appropriate weapons, armor, and mounts based on your current **troop identity** (Master at Arms) and **tier**.

Key traits:
- **Purchase-based**: equipment costs denars.
- **No accountability system**: the mod does not track “issued” gear or dock pay for missing items.
- **Buyback**: you can sell gear back to the Quartermaster for a reduced price.

## Access
- **Quartermaster**: Enlisted Status → **Visit Quartermaster**
- **Baggage Train**: Enlisted Status → **Baggage Train**

## Enlistment Bag Check (first enlistment)
About **12 in-game hours** after enlisting (when safe: not in battle/encounter/captivity), the quartermaster runs a bag check.

**Options:**
- **"Stow it all" (50g)**: moves inventory + equipped items into the baggage train stash and charges a **50 denar wagon fee** (clamped to what you can afford).
- **"Sell it all" (60%)**: liquidates inventory + equipped items at **60% value** and gives you denars.
- **"I'm keeping one thing" (Roguery 30+)**: attempts to keep one item (currently picks the highest-value item). If Roguery < 30, it is confiscated.

This is separate from Quartermaster trade: it’s part of the **Enlistment** flow.

## Baggage Train (stash access)
The Baggage Train is the stash created by the bag check.

**Fatigue cost by tier:**
- Tier 1–2: **4 fatigue**
- Tier 3–4: **2 fatigue**
- Tier 5+: **0 fatigue**

## Quartermaster: How it works

### 1) Troop identity drives availability
Your current troop identity comes from **Master at Arms** (Troop Selection). Quartermaster variants are discovered from the troop’s upgrade branch (culture-appropriate loadouts).

### 2) Variant discovery
Quartermaster collects possible variants from troop branch loadouts and builds options per equipment slot:
- Weapons (Weapon0–Weapon3)
- Armor (Head, Body, Gloves, Leg, Cape)
- Mount/Harness (when available)

### 3) Purchasing & equipping
- **Weapons**: placed into the first empty weapon slot (Weapon0–Weapon3). If all weapon slots are full, the item is placed into party inventory.
- **Armor / mount slots**: equipped into the relevant slot; any replaced item is moved into party inventory.

### 4) Selling (buyback)
Quartermaster offers a **Sell Equipment** menu that lists eligible items from your equipment and party inventory (excluding quest-critical items).

Selling removes **one** item and pays the buyback amount.

### 5) Pricing
- **Purchase price**: `item.Value × quartermaster.soldier_tax`
- **Provisioner / Quartermaster role discount**: 15% off quartermaster prices.
- **Buyback price**: `item.Value × quartermaster.buyback_rate`

### 6) Provisioner / Quartermaster role perks
- Expanded equipment availability (culture-wide additions beyond your exact troop kit).
- Supply Management menu access (party supplies / inventory utilities).

## Discharge & gear handling
Discharge is not handled inside Quartermaster. It resolves via **Final Muster** at pay muster (see Pay System). Final Muster determines whether gear is stripped or retained.

## Technical notes (high-level)
- Core logic: `src/Features/Equipment/Behaviors/QuartermasterManager.cs`
- Troop identity source: `src/Features/Equipment/Behaviors/TroopSelectionManager.cs`

## Related docs
- [Troop Selection (Master at Arms)](../Gameplay/troop-selection.md)
- [Enlistment](../Core/enlistment.md)
- [Pay System](../Core/pay-system-rework.md)
- [Camp (My Camp)](command-tent.md)

## Appendix: Future ideas (not implemented)
These are design ideas retained for later work; they are **not implemented** unless you can find code for them:
- “Standard Issue” strict re-issue flow with penalties for missing items
- Under-the-table store / officer stock and fatigue-driven favors (work/drink/roguery actions)
- Stash loss / theft incidents (wagons burning, camp theft, etc.)
