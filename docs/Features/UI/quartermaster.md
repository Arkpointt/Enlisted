# Quartermaster System

## Quick Reference

| Feature | Purpose | Access |
|---------|---------|--------|
| Equipment Grid | Visual equipment selection | Enlisted Status → "Visit Quartermaster" |
| Equipment Cards | Individual variant selection | Click equipment slot → Grid displays |
| Free Gear | All equipment issued free | Automatic |
| Item Limit | Max 2 of each item type (equipment + inventory) | Anti-abuse measure |
| Inventory Overflow | Weapons go to pack when slots full | Automatic |
| Accountability | Charged for missing gear on troop change | Automatic |

## Table of Contents

- [Overview](#overview)
- [How It Works](#how-it-works)
  - [Equipment Selection Flow](#equipment-selection-flow)
  - [Item Limits](#item-limits)
  - [Equipment Accountability](#equipment-accountability)
  - [Retirement](#retirement)
- [Technical Details](#technical-details)
  - [System Architecture](#system-architecture)
  - [Equipment Discovery](#equipment-discovery)
  - [UI Templates](#ui-templates)
- [Edge Cases](#edge-cases)
- [API Reference](#api-reference)
- [Debugging](#debugging)

---

## Overview

Grid-based UI system that lets players select individual equipment variants from a visual menu. All gear is issued free, but soldiers are held accountable for missing equipment when changing troop types.

**Key Features:**
- Visual equipment cards with images and stats
- 4-column grid layout (responsive to resolution)
- All equipment free (military issue)
- Max 2 of each item type (anti-abuse)
- Accountability check on troop change (charged for missing gear)
- Retirement reward: keep military gear + get personal belongings back

**Files:**
- `src/Features/Equipment/Behaviors/QuartermasterManager.cs` - Core logic, variant options, shared time state
- `src/Features/Equipment/Behaviors/TroopSelectionManager.cs` - Equipment tracking and accountability
- `src/Features/Equipment/UI/QuartermasterEquipmentSelectorBehavior.cs` - Gauntlet UI controller
- `src/Features/Equipment/UI/QuartermasterEquipmentSelectorVm.cs` - Main view model
- `src/Features/Equipment/UI/QuartermasterEquipmentItemVm.cs` - Individual equipment cards

**Shared Time State:**
`QuartermasterManager.CapturedTimeMode` is a public static property used by all Enlisted wait menus to preserve player's time control preference (pause/play) when navigating between menus.

---

## How It Works

### Equipment Selection Flow

**Entry:**
1. Player accesses through enlisted menu "Visit Quartermaster"
2. System checks available equipment variants for player's tier and culture

**Slot Selection:**
1. Player chooses equipment slot (Primary Weapon, Helmet, Body Armor, etc.)
2. System filters available variants by slot type

**Grid Display:**
1. Shows 4-column grid of available equipment variants
2. Each card displays: image, name, stats, status
3. Character preview updates when hovering over items

**Status Indicators:**
- "Free" - Available to obtain
- "Get Another" - Weapons/consumables (can get a second)
- "Equipped" - Already wearing this item
- "Limit (2)" - Already have 2 of this item type

**Selection Process:**
1. Player clicks "Select" on any equipment card
2. System checks item limit (max 2 per type)
3. Equipment applied and tracked for accountability
4. Confirmation message shown

### Item Limits

Soldiers can hold up to 2 of each item type to prevent abuse. The limit counts items across **both equipment slots and party inventory**.

**Limit Rules:**
- Weapons: Max 2 of the same weapon (equipped + inventory combined)
- Ammo/Consumables: Max 2 stacks
- Armor: Single slot per type (replaces existing)

When limit reached, card shows "Limit (2)" and selection is blocked.

### Inventory Overflow (Weapons)

When a soldier's weapon slots are full (all 4 slots occupied) and they requisition a new weapon:

1. Weapon is added to party inventory instead of replacing an equipped item
2. Message shown: "{ITEM_NAME} stowed in your pack. Hands full."
3. Item still counts toward the 2-item limit

This allows soldiers to stock up on weapons while keeping their current loadout intact. Items in inventory can be equipped manually through the normal inventory screen.

### Equipment Accountability

When changing troop type (via Master at Arms):
1. System checks what gear was issued vs what player has
2. Missing items = gold deducted from pay
3. Player notified of missing items and deduction
4. New troop's equipment issued and tracked

This encourages soldiers to take care of their gear.

### Retirement

**Honorable Discharge (Retirement):**
- Player keeps ALL military equipment they're wearing
- Personal belongings (backed up at enlistment) returned to inventory
- No accountability check - keeping gear is the retirement reward

**Regular Discharge:**
- Accountability check runs (charged for missing gear)
- Military equipment removed
- Personal equipment restored (replaces military gear)

---

## Technical Details

### System Architecture

```
QuartermasterManager (Core Logic)
    ├── Equipment discovery and filtering
    ├── Variant option building
    └── Item limit checking

TroopSelectionManager (Accountability)
    ├── IssuedItemRecord tracking
    ├── Missing equipment detection
    └── Gold deduction

QuartermasterEquipmentSelectorBehavior (UI Controller)
    ├── Gauntlet UI initialization
    └── Template loading

QuartermasterEquipmentSelectorVm (View Model)
    ├── Row organization (4 cards per row)
    └── Selection handling

QuartermasterEquipmentItemVm (Equipment Card)
    ├── Status display (Free, Equipped, Limit)
    └── Select button logic
```

### Equipment Discovery

Equipment variants are discovered using branch-based collection, which traverses the player's entire troop upgrade tree to find all available options.

**Branch-Based Collection:**
- Builds the troop upgrade path from culture's BasicTroop/EliteBasicTroop to the player's selected troop
- Collects equipment from ALL troops in that branch at the player's tier
- Falls back to all tiers if exact tier has no variants

**Equipment Categories:**

| Category | Menu Option | Slots |
|----------|-------------|-------|
| Weapons | "Request weapon variants" | Weapon0, Weapon1, Weapon2, Weapon3 |
| Armor | "Request armor variants" | Body, Head, Leg (boots), Gloves, Cape |

### UI Templates

**Template Location:**
```
GUI/Prefabs/Equipment/
├── QuartermasterEquipmentGrid.xml
├── QuartermasterEquipmentCardRow.xml
└── QuartermasterEquipmentCard.xml
```

---

## Edge Cases

### Item Limit Reached

**Scenario:** Player tries to get a third copy of an item

**Handling:**
- Card shows "Limit (2)" status
- Select button disabled
- Quartermaster dialogue: "Two's the limit, soldier. Army regs."

### Weapon Slots Full

**Scenario:** Player with all 4 weapon slots occupied requests another weapon

**Handling:**
- Weapon added to party inventory (not equipped)
- Message: "{ITEM_NAME} stowed in your pack. Hands full."
- Still counts toward 2-item limit
- Player can manually equip from inventory later

### Missing Equipment on Troop Change

**Scenario:** Player sold or lost issued gear

**Handling:**
- Gold deducted equal to item value
- Popup shows missing items and total deduction
- If insufficient gold, debt is noted in log

### Retirement with Missing Gear

**Scenario:** Player retires but lost some equipment

**Handling:**
- No accountability check for retirement
- Player keeps whatever they have
- Personal belongings returned to inventory

---

## API Reference

### Equipment Tracking

```csharp
// Record issued equipment for accountability
TroopSelectionManager.RecordIssuedEquipment()

// Check for missing equipment and calculate debt
List<(string name, int value)> CheckMissingEquipment(out int totalDebt)

// Clear tracking (retirement or full discharge)
TroopSelectionManager.ClearIssuedEquipment()
```

### Item Limit Checking

```csharp
// Check if player has reached limit for item type
bool IsAtLimit = GetPlayerItemCount(item) >= MaxItemsPerType; // MaxItemsPerType = 2

// Get current count of item across equipment AND inventory
int GetPlayerItemCount(Hero hero, string itemStringId)
// Checks: BattleEquipment, CivilianEquipment, PartyBase.MainParty.ItemRoster
```

### Inventory Overflow

```csharp
// When weapon slots are full, adds to inventory instead
PartyBase.MainParty.ItemRoster.AddToCounts(new EquipmentElement(item), 1);
```

### Retirement Equipment

```csharp
// Add backed-up gear to inventory (player keeps military equipment)
EquipmentManager.RestorePersonalEquipmentToInventory()

// Standard restoration (replaces military gear with personal)
EquipmentManager.RestorePersonalEquipment()
```

---

## Debugging

**Log Categories:**
- `"Quartermaster"` - Equipment logic
- `"Equipment"` - Tracking and accountability

**Key Log Points:**
```csharp
// Item issued to equipment slot
ModLogger.Info("Quartermaster", $"Equipment issued: {item.Name} to slot {slot} (replaced {previous})");

// Item added to inventory (slots full)
ModLogger.Info("Quartermaster", $"Weapon slots full - {item.Name} added to inventory");

// Item limit reached
ModLogger.Info("Quartermaster", $"Item limit reached: {item.Name} (count: {count})");

// Accountability check
ModLogger.Info("Equipment", $"Missing equipment check: {missingCount} items, {totalDebt} gold debt");

// Retirement
ModLogger.Info("Equipment", "Retirement reward: keeping military gear, personal items to inventory");
```

**Debug Output Location:**
- `Modules/Enlisted/Debugging/enlisted.log`

---

## Related Documentation

- [Menu Interface](menu-interface.md) - Access point for Quartermaster
- [Equipment System](../Equipment/equipment.md) - Equipment management overview
