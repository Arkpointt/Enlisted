# Quartermaster System

## Quick Reference

| Feature | Purpose | Access |
|---------|---------|--------|
| Equipment Grid | Visual equipment selection | Enlisted Status → "Visit Quartermaster" |
| Equipment Cards | Individual variant selection | Click equipment slot → Grid displays |
| Officer Discounts | 15% cost reduction | Automatic for Quartermaster/Provisioner duties |

## Table of Contents

- [Overview](#overview)
- [How It Works](#how-it-works)
  - [Equipment Selection Flow](#equipment-selection-flow)
  - [Grid Display System](#grid-display-system)
  - [Cost Calculation](#cost-calculation)
- [Technical Details](#technical-details)
  - [System Architecture](#system-architecture)
  - [Equipment Discovery](#equipment-discovery)
  - [UI Templates](#ui-templates)
- [Edge Cases](#edge-cases)
- [API Reference](#api-reference)
- [Debugging](#debugging)

---

## Overview

Grid-based UI system that lets players select individual equipment variants from a visual menu. Replaces basic text-based selection with clickable equipment cards showing weapon stats, costs, and images in a professional 4-column grid layout.

**Key Features:**
- Visual equipment cards with images and stats
- 4-column grid layout (responsive to resolution)
- Individual clickable selection per variant
- Officer discounts (15% for Quartermaster/Provisioner duties)
- Character preview showing equipment changes
- Fallback to conversation-based selection if grid unavailable

**Files:**
- `src/Features/Equipment/Behaviors/QuartermasterManager.cs` - Core logic, cost calculation
- `src/Features/Equipment/UI/QuartermasterEquipmentSelectorBehavior.cs` - Gauntlet UI controller
- `src/Features/Equipment/UI/QuartermasterEquipmentSelectorVm.cs` - Main view model
- `src/Features/Equipment/UI/QuartermasterEquipmentRowVm.cs` - Row container (4 cards)
- `src/Features/Equipment/UI/QuartermasterEquipmentItemVm.cs` - Individual equipment cards

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
2. Each card displays: image, name, stats, cost
3. Character preview updates when hovering over items

**Selection Process:**
1. Player clicks "Select" on any equipment card
2. System validates player has enough gold (with officer discounts)
3. Equipment applied using `EquipmentHelper.AssignHeroEquipmentFromEquipment()`
4. Gold deducted for equipment cost
5. Confirmation message shown
6. UI closes after successful selection

### Grid Display System

**Layout:**
- 4-column grid (responsive to screen resolution)
- Rows contain 4 equipment cards each
- Centered alignment for 4K resolution support
- Character preview panel on side

**Equipment Cards:**
- Individual clickable cards per variant
- Shows: equipment image, name, culture, class, weapon tier, damage stats
- "Select" button (disabled if unaffordable)
- "Preview" button for detailed stats

**Visual Features:**
- Equipment images load via `ImageIdentifierVM`
- Loading spinner while images load
- Cost displayed prominently
- Red text for unaffordable items

### Cost Calculation

**Base Cost:**
- Uses native equipment cost formula
- Based on item tier and quality

**Officer Discounts:**
- **15% discount** for Quartermaster's Aide duty
- **15% discount** for Provisioner duty
- Applied automatically during cost calculation

**Cost Display:**
- Shows base cost and discounted cost (if applicable)
- "Insufficient Funds" status for unaffordable items
- "Select" button disabled when player cannot afford

---

## Technical Details

### System Architecture

**Component Structure:**
```
QuartermasterManager (Core Logic)
    ├── Equipment discovery and filtering
    ├── Cost calculation with discounts
    └── Equipment application

QuartermasterEquipmentSelectorBehavior (UI Controller)
    ├── Gauntlet UI initialization
    ├── Template loading
    └── Input handling

QuartermasterEquipmentSelectorVm (View Model)
    ├── Row organization (4 cards per row)
    ├── Equipment card data binding
    └── Selection handling

QuartermasterEquipmentRowVm (Row Container)
    └── 4 equipment cards per row

QuartermasterEquipmentItemVm (Equipment Card)
    ├── Equipment image and stats
    ├── Cost display
    └── Select/Preview buttons
```

### Equipment Discovery

**Primary Source:**
- Direct from selected troop's `BattleEquipments` collection
- Filtered by tier, formation type, and cultural appropriateness

**Secondary Source:**
- Culture-wide equipment pool for officers
- Additional variants available at higher tiers

**Filtering Logic:**
- By tier: Only equipment appropriate for player's current tier
- By formation: Infantry/Archer/Cavalry/HorseArcher variants
- By culture: Faction-appropriate equipment only
- By slot: Equipment type matches selected slot

### UI Templates

**Template Location:**
```
GUI/Prefabs/Equipment/
├── QuartermasterEquipmentGrid.xml     # Main layout with character preview
├── QuartermasterEquipmentCardRow.xml  # Row container (4 cards)
└── QuartermasterEquipmentCard.xml     # Individual clickable cards
```

**Key Template Features:**
- `HorizontalAlignment="Center"` for 4K resolution scaling
- `ImageIdentifierVM` for equipment images
- `Standard.CircleLoadingWidget` for loading states
- Responsive layout adapts to screen resolution

**Template Requirements:**
- Must be in `GUI/Prefabs/Equipment/` folder
- Use `<Widget>` instead of `<Panel>` (Gauntlet requirement)
- Register hotkeys before `InputRestrictions` to prevent freezing

---

## Edge Cases

### No Equipment Variants Available

**Scenario:** No equipment variants found for selected slot

**Handling:**
- Shows conversation-based fallback menu instead of grid
- Provides simple text selection for basic functionality
- No crash or error - graceful degradation

### Equipment Images Fail to Load

**Scenario:** Image loading fails or times out

**Handling:**
- Shows loading spinner with `Standard.CircleLoadingWidget`
- Graceful fallback if image loading fails
- Equipment card still functional without image
- Error logged but doesn't crash the game

### Insufficient Funds

**Scenario:** Player cannot afford selected equipment

**Handling:**
- "Select" button disabled on unaffordable items
- Cost displayed in red text
- "Insufficient Funds" status message shown
- Player can still preview item stats

### Game Resolution Changes

**Scenario:** Player changes resolution while UI is open

**Handling:**
- Responsive design scales automatically
- `HorizontalAlignment="Center"` maintains centering
- Tested on 1080p, 1440p, and 4K displays
- Grid layout adapts to available space

### Gauntlet Template Loading Fails

**Scenario:** Template files missing or corrupted

**Handling:**
- Automatic fallback to conversation-based selection
- Error logged but doesn't crash the game
- Player can still select equipment via text menu
- System continues to function

### Officer Duty Changes

**Scenario:** Player changes duty while in Quartermaster menu

**Handling:**
- Discounts recalculated on next cost check
- Menu refresh updates prices
- No state corruption

---

## API Reference

### Equipment Discovery

```csharp
// Get available equipment variants for slot
List<EquipmentElement> GetAvailableEquipment(
    EquipmentIndex slotIndex,
    int playerTier,
    FormationClass formationClass,
    CultureObject culture)
{
    // Filter by tier, formation, culture, slot
    // Return list of available variants
}

// Get equipment from troop's battle equipment
EquipmentElement GetEquipmentFromTroop(
    CharacterObject troop,
    EquipmentIndex slotIndex)
{
    // Extract equipment from troop's BattleEquipments
}
```

### Cost Calculation

```csharp
// Calculate equipment cost with discounts
int CalculateEquipmentCost(EquipmentElement equipment, Hero buyer)
{
    // Base cost from native formula
    int baseCost = Campaign.Current.Models.PartyWageModel
        .GetEquipmentCost(equipment.Item, buyer);
    
    // Apply officer discount if applicable
    if (HasOfficerDiscount(buyer))
    {
        baseCost = (int)(baseCost * 0.85); // 15% discount
    }
    
    return baseCost;
}

// Check if player has officer discount
bool HasOfficerDiscount(Hero hero)
{
    // Check for Quartermaster's Aide or Provisioner duty
    return EnlistedDutiesBehavior.Instance?.HasActiveDutyWithRole("Quartermaster") == true ||
           EnlistedDutiesBehavior.Instance?.HasActiveDutyWithRole("Provisioner") == true;
}
```

### Equipment Application

```csharp
// Apply equipment to player
void ApplyEquipment(EquipmentElement equipment, EquipmentIndex slotIndex)
{
    // Use native equipment helper
    EquipmentHelper.AssignHeroEquipmentFromEquipment(
        Hero.MainHero,
        equipment,
        slotIndex);
    
    // Deduct gold
    Hero.MainHero.ChangeHeroGold(-CalculateEquipmentCost(equipment, Hero.MainHero));
}
```

### UI Initialization

```csharp
// Initialize Gauntlet UI
void InitializeQuartermasterUI()
{
    // Register hotkeys BEFORE InputRestrictions
    RegisterHotKeyCategory();
    
    // Load template
    LoadGauntletTemplate("QuartermasterEquipmentGrid");
    
    // Initialize view model
    var vm = new QuartermasterEquipmentSelectorVm();
    SetViewModel(vm);
}

// Register hotkeys
void RegisterHotKeyCategory()
{
    // Prevents input freezing
    // Must be called before InputRestrictions
}
```

---

## Debugging

**Log Categories:**
- `"Quartermaster"` - Core equipment logic
- `"QuartermasterUI"` - Gauntlet UI operations

**Key Log Points:**
```csharp
// Equipment discovery
ModLogger.Debug("Quartermaster", $"Found {count} variants for slot {slotIndex}");
ModLogger.Debug("Quartermaster", $"Filtered by tier {tier}, formation {formation}");

// Cost calculation
ModLogger.Debug("Quartermaster", $"Base cost: {baseCost}, Discount: {discount}, Final: {finalCost}");

// Equipment application
ModLogger.Info("Quartermaster", $"Applied {equipment.Item.Name} to slot {slotIndex} for {cost} gold");

// UI operations
ModLogger.Debug("QuartermasterUI", $"Template loaded: {templateName}");
ModLogger.Debug("QuartermasterUI", $"View model initialized with {rowCount} rows");
```

**Common Issues:**

**"Custom Widget type not found" error:**
- Templates in wrong folder (need `GUI/Prefabs/Equipment/`)
- Check template file paths
- Verify template names match code references

**Game freezes on open:**
- Missing hotkey registration before input restrictions
- Ensure `RegisterHotKeyCategory()` called before `InputRestrictions`
- Check hotkey registration order

**Images don't load:**
- Missing `TaleWorlds.PlayerServices.dll` assembly reference
- Verify assembly reference in project
- Check `ImageIdentifierVM` initialization

**UI not centered on 4K:**
- Using fixed margins instead of center alignment
- Use `HorizontalAlignment="Center"` in templates
- Check template layout properties

**"Panel widget not found" error:**
- Replace `<Panel>` with `<Widget>` in templates
- Gauntlet requires `<Widget>` elements
- Check all template files for panel usage

**Equipment variants not showing:**
- Check tier filtering logic
- Verify formation class matching
- Check culture appropriateness
- Review equipment discovery method

**Debug Output Location:**
- `Modules/Enlisted/Debugging/enlisted.log`

**Related Files:**
- `src/Features/Equipment/Behaviors/QuartermasterManager.cs`
- `src/Features/Equipment/UI/QuartermasterEquipmentSelectorBehavior.cs`
- `src/Features/Equipment/UI/QuartermasterEquipmentSelectorVm.cs`

---

## Related Documentation

- [Menu Interface](menu-interface.md) - Access point for Quartermaster
- [Equipment System](../Equipment/equipment.md) - Equipment management overview
- [Duties System](../Core/duties-system.md) - Officer discounts for duties
