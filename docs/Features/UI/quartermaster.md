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

## Overview
The **Quartermaster** is the enlisted equipment vendor. It lets you **purchase** formation-appropriate weapons, armor, and mounts based on your **formation**, **tier**, and **culture**.

Key traits:
- **Purchase-based**: equipment costs denars.
- **Formation-driven**: availability based on your chosen formation (Infantry/Archer/Cavalry/Horse Archer).
- **Culture-appropriate**: gear matches your enlisted lord's culture.
- **Tier-unlocked**: higher tiers unlock better equipment.
- **NEW indicators**: newly unlocked items are marked with `[NEW]` after promotion.
- **No accountability system**: the mod does not track "issued" gear or dock pay for missing items.
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

This is separate from Quartermaster trade: it's part of the **Enlistment** flow.

## Baggage Train (stash access)
The Baggage Train is the stash created by the bag check.

**Fatigue cost by tier:**
- Tier 1–2: **4 fatigue**
- Tier 3–4: **2 fatigue**
- Tier 5+: **0 fatigue**

## Quartermaster: How it works

### 1) Formation + Tier + Culture drives availability
Your equipment availability is determined by:
- **Formation**: Chosen during T1→T2 proving event (Infantry/Archer/Cavalry/Horse Archer)
- **Tier**: Equipment up to your current tier is available
- **Culture**: Your enlisted lord's culture determines the gear aesthetics

### 2) Equipment discovery
Quartermaster dynamically discovers available equipment by:
- Scanning troops matching your formation and culture
- Filtering to troops at or below your tier
- Collecting all equipment from those troops

### 3) NEW item tracking
After a promotion:
- System detects newly available items for your tier
- These items are marked with `[NEW]` in the selection menu
- Markers clear after viewing the item selection

### 4) Purchasing & equipping
- **Weapons**: placed into the first empty weapon slot (Weapon0–Weapon3). If all weapon slots are full, the item is placed into party inventory.
- **Armor / mount slots**: equipped into the relevant slot; any replaced item is moved into party inventory.

### 5) Selling (buyback)
Quartermaster offers a **Sell Equipment** menu that lists eligible items from your equipment and party inventory (excluding quest-critical items).

Selling removes **one** item and pays the buyback amount.

### 6) Pricing
- **Purchase price**: `item.Value × quartermaster.soldier_tax`
- **Provisioner / Quartermaster role discount**: 15% off quartermaster prices.
- **Buyback price**: `item.Value × quartermaster.buyback_rate`

### 7) Provisioner / Quartermaster duty perks
- Expanded equipment availability (culture-wide additions beyond your exact formation kit).
- Supply Management menu access (party supplies / inventory utilities).

## Promotion & Quartermaster

When promoted, you are prompted to visit the Quartermaster:
- No auto-equip on promotion (unlike the old troop selection system)
- Message displays: "Report to the Quartermaster for your new kit"
- Newly unlocked items are marked with `[NEW]`

## Discharge & gear handling
Discharge is not handled inside Quartermaster. It resolves via **Final Muster** at pay muster (see Pay System). Final Muster determines whether gear is stripped or retained.

## Technical notes (high-level)
- Core logic: `src/Features/Equipment/Behaviors/QuartermasterManager.cs`
- Key methods:
  - `GetAvailableEquipmentByFormation(formation, tierCap, culture)` - discovers equipment
  - `UpdateNewlyUnlockedItems()` - tracks NEW items after promotion
  - `IsNewlyUnlockedItem(item)` - checks if item should show [NEW]
  - `ClearNewlyUnlockedMarkers()` - clears markers after viewing

## Related docs
- [Troop Selection (Legacy)](../Gameplay/troop-selection.md)
- [Enlistment](../Core/enlistment.md)
- [Pay System](../Core/pay-system-rework.md)
- [Camp (My Camp)](camp-tent.md)
- [Duties System](../Core/duties-system.md)
