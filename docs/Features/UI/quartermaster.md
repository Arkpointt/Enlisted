# Quartermaster (and the Baggage Train)

This page documents the full equipment loop: **Quartermaster buy/sell**, the **Quartermaster Hero NPC**, plus the **Enlistment Bag Check** and **Baggage Train** stash that keep early service grounded.

## Index
- [Overview](#overview)
- [Quartermaster Hero](#quartermaster-hero)
- [Access](#access)
- [Enlistment Bag Check (first enlistment)](#enlistment-bag-check-first-enlistment)
- [Baggage Train (stash access)](#baggage-train-stash-access)
- [Quartermaster: How it works](#quartermaster-how-it-works)
- [Provisions System](#provisions-system)
- [Relationship System](#relationship-system)
- [Discharge & gear handling](#discharge--gear-handling)
- [Technical notes (high-level)](#technical-notes-high-level)

## Overview
The **Quartermaster** is the enlisted equipment vendor. It lets you **purchase** formation-appropriate weapons, armor, and mounts based on your **formation**, **tier**, and **culture**.

Key traits:
- **Persistent NPC**: Each lord has a unique Quartermaster Hero with personality
- **Three Archetypes**: Veteran, Scoundrel, or Believer - each with unique dialogue
- **Purchase-based**: equipment costs denars
- **Formation-driven**: availability based on your chosen formation (Infantry/Archer/Cavalry/Horse Archer)
- **Culture-appropriate**: gear matches your enlisted lord's culture
- **Tier-unlocked**: higher tiers unlock better equipment
- **Relationship discounts**: Build trust for 5-15% discounts
- **NEW indicators**: newly unlocked items are marked with `[NEW]` after promotion
- **No accountability system**: the mod does not track "issued" gear or dock pay for missing items
- **Buyback**: you can sell gear back to the Quartermaster for a reduced price

## Quartermaster Hero

Each lord has a unique, persistent Quartermaster NPC:

### Archetypes
- **Veteran**: Pragmatic old soldier, practical advice, no-nonsense attitude
- **Scoundrel**: Opportunistic, knows black market contacts, offers "creative" solutions
- **Believer**: Pious and moral, offers spiritual guidance, encourages loyalty

### PayTension-Aware Dialogue
When pay is late (PayTension 40+), the Quartermaster offers archetype-specific advice:
- **Scoundrel**: Black market contacts, opportunities to make coin
- **Believer**: Moral guidance, encouragement to stay faithful
- **Veteran**: Practical survival advice, desertion warnings at 60+ tension

### Relationship Milestones
| Level | Relationship | Discount | Unlocks |
|-------|-------------|----------|---------|
| Stranger | 0-19 | 0% | Basic access |
| Known | 20-39 | 0% | Chat option |
| Trusted | 40-59 | 5% | Black market hints |
| Respected | 60-79 | 10% | Better dialogue |
| Battle Brother | 80-100 | 15% | Special items |

## Access
- **Quartermaster**: Enlisted Status â†’ **Visit Quartermaster**
- **Baggage Train**: Enlisted Status â†’ **Baggage Train**

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
- Tier 1â€“2: **4 fatigue**
- Tier 3â€“4: **2 fatigue**
- Tier 5+: **0 fatigue**

## Quartermaster: How it works

### 1) Formation + Tier + Culture drives availability
Your equipment availability is determined by:
- **Formation**: Chosen during T1â†’T2 proving event (Infantry/Archer/Cavalry/Horse Archer)
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
- **Weapons**: placed into the first empty weapon slot (Weapon0â€“Weapon3). If all weapon slots are full, the item is placed into party inventory.
- **Armor / mount slots**: equipped into the relevant slot; any replaced item is moved into party inventory.

### 5) Selling (buyback)
Quartermaster offers a **Sell Equipment** menu that lists eligible items from your equipment and party inventory (excluding quest-critical items).

Selling removes **one** item and pays the buyback amount.

### 6) Pricing
- **Purchase price**: `item.Value Ã— quartermaster.soldier_tax`
- **Provisioner / Quartermaster role discount**: 15% off quartermaster prices.
- **Buyback price**: `item.Value Ã— quartermaster.buyback_rate`

### 7) Provisioner / Quartermaster duty perks
- Expanded equipment availability (culture-wide additions beyond your exact formation kit).
- Supply Management menu access (party supplies / inventory utilities).

## Promotion & Quartermaster

When promoted, you are prompted to visit the Quartermaster:
- No auto-equip on promotion (unlike the old troop selection system)
- Message displays: "Report to the Quartermaster for your new kit"
- Newly unlocked items are marked with `[NEW]`

## Provisions System

Purchase rations for morale and fatigue benefits:

| Tier | Cost | Duration | Morale | Fatigue |
|------|------|----------|--------|---------|
| Basic Rations | 25g | 3 days | +2 | - |
| Good Fare | 50g | 3 days | +4 | -1/day |
| Officer's Table | 100g | 3 days | +6 | -2/day |

### Retinue Provisioning (T7+)
Commanders with retinues can purchase provisions for their soldiers:

| Tier | Cost/Soldier | Duration | Effect |
|------|--------------|----------|--------|
| Bare Minimum | 1g | 7 days | -2 morale |
| Standard | 2g | 7 days | No modifier |
| Good Fare | 4g | 7 days | +2 morale |
| Officer Quality | 6g | 7 days | +4 morale |

Warning at 2 days remaining; starvation penalties at expiration.

## Relationship System

Build trust with the Quartermaster over time:

**Gaining Relationship:**
- First meeting: +5
- Chatting: +3
- Buying equipment: +1 per purchase
- Helping with PayTension options: +2 to +5

**Discounts by Level:**
- Trusted (40+): 5% off all purchases
- Respected (60+): 10% off all purchases
- Battle Brother (80+): 15% off all purchases

## Discharge & gear handling
Discharge is not handled inside Quartermaster. It resolves via **Final Muster** at pay muster (see Pay System). Final Muster determines whether gear is stripped or retained.

## Technical notes (high-level)

### Core Files
- **Equipment Logic**: `src/Features/Equipment/Behaviors/QuartermasterManager.cs`
- **Quartermaster Hero**: `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs` (Quartermaster section)
- **Dialog System**: `src/Features/Conversations/Behaviors/EnlistedDialogManager.cs`
- **Menu Integration**: `src/Features/Camp/CampMenuHandler.cs` (Desperate Measures, Help the Lord)

### Key Methods
**QuartermasterManager.cs:**
- `GetAvailableEquipmentByFormation(formation, tierCap, culture)` - discovers equipment
- `UpdateNewlyUnlockedItems()` - tracks NEW items after promotion
- `IsNewlyUnlockedItem(item)` - checks if item should show [NEW]
- `AddRationsMenuOptions()` - provisions menu

**EnlistmentBehavior.cs (Quartermaster section):**
- `GetOrCreateQuartermaster()` - gets or creates the QM Hero
- `ModifyQuartermasterRelationship(change)` - adjusts trust level
- `GetQuartermasterDiscount()` - returns discount percentage (0-15)
- `ApplyQuartermasterDiscount(price)` - applies relationship discount
- `PurchaseRations(tier)` - handles rations purchase
- `PurchaseRetinueProvisioning(tier)` - handles retinue provisioning

**EnlistedDialogManager.cs:**
- `AddQuartermasterDialogs(starter)` - registers all QM dialog
- `GetQuartermasterGreeting()` - dynamic greeting based on archetype/tension
- PayTension-aware dialog conditions and consequences

## Related docs
- [Troop Selection (Legacy)](../Gameplay/troop-selection.md)
- [Enlistment](../Core/enlistment.md)
- [Pay System](../Core/pay-system.md)
- [Camp](camp-tent.md)
- [Duties System](../Core/duties-system.md)
- [Camp Life Simulation](../Gameplay/camp-life-simulation.md)
