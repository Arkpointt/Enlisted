# Quartermaster System

## Quick Reference

| Feature              | Purpose                                   | Access                                  |
|----------------------|-------------------------------------------|-----------------------------------------|
| Equipment Grid       | Visual equipment selection                | Enlisted Status → "Visit Quartermaster" |
| Equipment Cards      | Individual variant selection              | Click equipment slot → Grid displays    |
| Free Gear            | All equipment issued free                 | Automatic                               |
| Slot-Specific Limits | Armor = 1, Weapons = 2                    | Anti-abuse measure                      |
| Real-Time Updates    | Buttons and player model update instantly | No menu exit needed                     |
| Inventory Overflow   | Weapons go to pack when slots full        | Automatic                               |
| Accountability       | Charged for missing gear on troop change  | Automatic                               |

## Table of Contents

- [Overview](#overview)
- [How It Works](#how-it-works)
    - [Equipment Selection Flow](#equipment-selection-flow)
    - [Slot-Specific Item Limits](#slot-specific-item-limits)
    - [Equipment Accountability](#equipment-accountability)
    - [Retirement](#retirement)
- [Technical Details](#technical-details)
    - [System Architecture](#system-architecture)
    - [Equipment Discovery](#equipment-discovery)
    - [Real-Time UI Updates](#real-time-ui-updates)
    - [UI Templates](#ui-templates)
- [Edge Cases](#edge-cases)
- [API Reference](#api-reference)
- [Debugging](#debugging)

---

## Overview

Grid-based UI system that lets players select individual equipment variants from a visual menu. All gear is issued free,
but soldiers are held accountable for missing equipment when changing troop types.

**Key Features:**

- Visual equipment cards with images and stats
- 4-column grid layout (responsive to resolution)
- All equipment free (military issue)
- Slot-specific limits (armor = 1, weapons = 2)
- Real-time button and player model updates without leaving menu
- Accountability check on troop change (charged for missing gear)
- Retirement reward: keep military gear + get personal belongings back

**Files:**

- `src/Features/Equipment/Behaviors/QuartermasterManager.cs` - Core logic, variant options, item limits
- `src/Features/Equipment/Behaviors/TroopSelectionManager.cs` - Equipment tracking and accountability
- `src/Features/Equipment/UI/QuartermasterEquipmentSelectorBehavior.cs` - Gauntlet UI controller
- `src/Features/Equipment/UI/QuartermasterEquipmentSelectorVm.cs` - Main view model, real-time refresh
- `src/Features/Equipment/UI/QuartermasterEquipmentItemVm.cs` - Individual equipment cards

**Shared Time State:**
`QuartermasterManager.CapturedTimeMode` is a public static property used by all Enlisted wait menus to preserve player's
time control preference (pause/play) when navigating between menus.

---

## How It Works

### Equipment Selection Flow

**Entry:**

1. Player accesses through enlisted menu "Visit Quartermaster"
2. System checks available equipment variants for player's tier using branch-based collection

**Slot Selection:**

1. Player chooses equipment slot (Primary Weapon, Helmet, Body Armor, etc.)
2. System filters available variants by slot type
3. Slots with only one available option still show the issued item so players can review what they have even when no
   alternates exist.

**Grid Display:**

1. Shows 4-column grid of available equipment variants
2. Each card displays: image, name, stats, status
3. Player model preview updates in real-time when equipment changes

**Status Indicators:**

- "Free" - Available to obtain
- "Get Another" - Weapons/consumables (can get a second when you have 1)
- "Equipped / Already issued" - Armor at limit (1)
- "Limit (2)" - Weapons at limit (already have 2)

**Selection Process:**

1. Player clicks "Select" on any equipment card
2. System checks slot-specific item limit
3. Equipment applied and tracked for accountability
4. Button states and player model update instantly (no menu exit needed)
5. Menu stays open for additional selections

### Slot-Specific Item Limits

Different equipment types have different limits based on realistic military constraints:

| Slot Type                                           | Limit | Behavior                                                   |
|-----------------------------------------------------|-------|------------------------------------------------------------|
| **Armor** (Head, Body, Legs, Gloves, Cape)          | 1     | Greys out when equipped, shows "Equipped / Already issued" |
| **Shields**                                         | 1     | Same as armor                                              |
| **Weapons** (Swords, Axes, Maces, Spears)           | 2     | Shows "Get Another" at 1, greys out at 2 with "Limit (2)"  |
| **Ranged** (Bows, Crossbows)                        | 2     | Same as weapons                                            |
| **Consumables** (Arrows, Bolts, Javelins, Throwing) | 2     | Same as weapons                                            |

The limit counts items across **both equipment slots and party inventory**.

Armor/shields use an "already issued" message at 1; weapons and consumables hit the "Limit (2)" state after two of the
same item are owned.

### Inventory Overflow (Weapons)

When a soldier's weapon slots are full (all 4 slots occupied) and they requisition a new weapon:

1. Weapon is added to party inventory instead of replacing an equipped item
2. Message shown: "{ITEM_NAME} stowed in your pack. Hands full."
3. Item still counts toward the 2-item limit

This allows soldiers to stock up on weapons while keeping their current loadout intact. Items in inventory can be
equipped manually through the normal inventory screen.

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
    ├── Equipment discovery via branch-based collection
    ├── Variant option building with slot-specific limits
    ├── GetItemLimit(slot) - returns 1 for armor/shields, 2 for weapons
    └── Item limit checking per slot type

TroopSelectionManager (Accountability)
    ├── IssuedItemRecord tracking
    ├── Missing equipment detection
    └── Gold deduction

QuartermasterEquipmentSelectorBehavior (UI Controller)
    ├── Gauntlet UI initialization
    └── Template loading

QuartermasterEquipmentSelectorVm (View Model)
    ├── Row organization (4 cards per row)
    ├── RecalculateAllVariantStates() - real-time button updates
    ├── RefreshCharacterModel() - real-time player preview
    └── Selection handling (menu stays open)

QuartermasterEquipmentItemVm (Equipment Card)
    ├── IsEnabled based on IsAtLimit only
    ├── Status display (Free, Get Another, Equipped, Limit)
    └── GetVariant() for state access
```

### Equipment Discovery

Equipment variants are discovered using branch-based collection from the player's troop upgrade tree.

**Branch-Based Collection:**

- Builds the troop upgrade path from culture's BasicTroop/EliteBasicTroop to the player's selected troop
- Collects equipment from ALL troops in that branch at the player's tier
- Falls back to all tiers if exact tier has no variants
- Items are NOT filtered by culture (allows cross-culture items that appear in troop loadouts)

**Equipment Categories:**

| Category | Menu Option               | Slots                                 |
|----------|---------------------------|---------------------------------------|
| Weapons  | "Request weapon variants" | Weapon0, Weapon1, Weapon2, Weapon3    |
| Armor    | "Request armor variants"  | Body, Head, Leg (boots), Gloves, Cape |

### Real-Time UI Updates

The UI updates instantly without requiring the player to exit and re-enter the menu:

**Button State Updates:**

- `RecalculateAllVariantStates(hero)` is called on every refresh
- Iterates through all variant cards and recalculates `IsAtLimit` and `CanAfford`
- `OnPropertyChanged(nameof(IsEnabled))` triggers UI binding update

**Player Model Updates:**

- `RefreshCharacterModel(hero)` is called after equipment changes
- Calls `UnitCharacter.FillFrom(hero.CharacterObject)` to reload equipment
- `OnPropertyChanged(nameof(UnitCharacter))` triggers 3D preview update

**Menu Persistence:**

- Menu no longer closes after each requisition
- Player can requisition multiple items in sequence
- Exit by clicking Close/Back button

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

### Item Limit Reached (Armor)

**Scenario:** Player tries to get armor they're already wearing

**Handling:**

- Card shows "Equipped / Already issued" status
- Select button disabled (greyed out)
- Limit is 1 for all armor slots

### Item Limit Reached (Weapons)

**Scenario:** Player tries to get a third copy of a weapon

**Handling:**

- Card shows "Limit (2)" status
- Select button disabled (greyed out)
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

### Slot-Specific Item Limits

```csharp
// Get the limit for a specific equipment slot
int GetItemLimit(EquipmentIndex slot)
// Returns 1 for armor/shields, 2 for weapons/consumables

// Check if player has reached limit for item in slot
bool IsAtLimit = GetPlayerItemCount(item) >= GetItemLimit(slot);
```

### Equipment Tracking

```csharp
// Record issued equipment for accountability
TroopSelectionManager.RecordIssuedEquipment()

// Check for missing equipment and calculate debt
List<(string name, int value)> CheckMissingEquipment(out int totalDebt)

// Clear tracking (retirement or full discharge)
TroopSelectionManager.ClearIssuedEquipment()
```

### Real-Time Updates

```csharp
// Recalculate all variant button states
void RecalculateAllVariantStates(Hero hero)

// Refresh character model preview
void RefreshCharacterModel(Hero hero)

// Get the underlying variant for state updates
EquipmentVariantOption GetVariant()
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
- `"QuartermasterUI"` - UI operations
- `"Equipment"` - Tracking and accountability

**Key Log Points:**

```csharp
// Item issued to equipment slot
ModLogger.Info("Quartermaster", $"Equipment issued: {item.Name} to slot {slot} (replaced {previous})");

// Item added to inventory (slots full)
ModLogger.Info("Quartermaster", $"Weapon slots full - {item.Name} added to inventory");

// Item limit reached
ModLogger.Info("Quartermaster", $"Item limit reached: {item.Name} (count: {count}, limit: {limit})");

// Accountability check
ModLogger.Info("Equipment", $"Missing equipment check: {missingCount} items, {totalDebt} gold debt");

// Character model refresh
ModLogger.Info("QuartermasterUI", "Character model refreshed after equipment change");
```

**Debug Output Location:**

- `Modules/Enlisted/Debugging/enlisted.log`

---

## Related Documentation

- [Menu Interface](menu-interface.md) - Access point for Quartermaster
- [Equipment Reference](../../discovered/equipment.md) - Equipment system research
