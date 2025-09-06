# Feature Spec: Quartermaster System

## Overview
Grid UI system that lets players select individual equipment variants from a visual menu, similar to an inventory screen but focused on military equipment procurement.

## Purpose
Replace basic text-based equipment selection with individual clickable equipment cards. Shows weapon stats, costs, and images in a professional 4-column grid layout.

## Inputs/Outputs

**Inputs:**
- Current equipped item in selected slot (weapon, helmet, etc.)
- Available equipment variants for player's troop type and culture
- Player's current gold amount
- Officer role status (for discount calculation)

**Outputs:**  
- Equipment applied to player using `EquipmentHelper.AssignHeroEquipmentFromEquipment()`
- Gold deducted for equipment cost
- UI closed after successful selection
- Status messages for feedback ("Equipment applied", "Insufficient funds", etc.)

## Behavior

1. **Entry**: Player accesses through enlisted menu "Visit Quartermaster"
2. **Slot Selection**: Choose equipment slot (Primary Weapon, Helmet, etc.)  
3. **Grid Display**: Shows 4-column grid of available equipment variants
4. **Individual Selection**: Click "Select" on any equipment card
5. **Cost Check**: Validates player has enough gold (with officer discounts)
6. **Equipment Applied**: Swaps equipment and deducts cost
7. **Feedback**: Shows confirmation message and closes UI

**Equipment Variants Source:**
- Primary: Direct from selected troop's `BattleEquipments` collection
- Secondary: Culture-wide equipment pool for officers
- Filtering: By tier, formation type, and cultural appropriateness

## Technical Implementation

**Files:**
- `QuartermasterManager.cs` - Core logic, cost calculation, equipment discovery
- `QuartermasterEquipmentSelectorBehavior.cs` - Gauntlet UI controller  
- `QuartermasterEquipmentSelectorVM.cs` - Main view model with row organization
- `QuartermasterEquipmentRowVM.cs` - Container for 4 cards per row
- `QuartermasterEquipmentItemVM.cs` - Individual equipment cards with stats

**Templates:**
```
GUI/Prefabs/Equipment/
├── QuartermasterEquipmentGrid.xml     # Main layout with character preview  
├── QuartermasterEquipmentCardRow.xml  # Row container (4 cards)
└── QuartermasterEquipmentCard.xml     # Individual clickable cards
```

**Key APIs:**
- `ImageIdentifierVM(item, "")` for equipment images (requires `TaleWorlds.PlayerServices.dll`)
- `HorizontalAlignment="Center"` for 4K resolution scaling
- `RegisterHotKeyCategory()` before `InputRestrictions` to prevent freezing

## Edge Cases

**No Equipment Variants Available:**
- Shows conversation-based fallback menu instead of grid
- Provides simple text selection for basic functionality

**Equipment Images Fail to Load:**  
- Shows loading spinner with Standard.CircleLoadingWidget
- Graceful fallback if image loading fails

**Insufficient Funds:**
- Disables "Select" button on unaffordable items  
- Shows cost in red text with "Insufficient Funds" status

**Game Resolution Changes:**
- Responsive design scales automatically with `HorizontalAlignment="Center"`
- Tested on 1080p, 1440p, and 4K displays

**Gauntlet Template Loading Fails:**
- Automatic fallback to conversation-based selection
- Error logged but doesn't crash the game

## Acceptance Criteria

- ✅ Individual equipment clicking (each variant has separate Select/Preview buttons)
- ✅ Equipment images load and display properly  
- ✅ 4K resolution support with proper centering
- ✅ Rich weapon details (Culture, Class, Weapon Tier, damage stats)
- ✅ Cost calculation with officer discounts (15% for Quartermaster/provisioner)
- ✅ No crashes, assertion errors, or input freezing
- ✅ ESC key closes UI properly
- ✅ Character preview shows equipment changes
- ✅ Fallback system works if Gauntlet fails

## Debugging

**Common Issues:**
- **"Custom Widget type not found"**: Templates in wrong folder (need `GUI/Prefabs/Equipment/`)
- **Game freezes on open**: Missing hotkey registration before input restrictions
- **Images don't load**: Missing `TaleWorlds.PlayerServices.dll` assembly reference
- **UI not centered on 4K**: Using fixed margins instead of center alignment  
- **"Panel widget not found"**: Replace `<Panel>` with `<Widget>` in templates

**Log Categories:**
- "Quartermaster" - Core equipment logic
- "QuartermasterUI" - Gauntlet UI operations
- Look in `Modules\Enlisted\Debugging\enlisted.log` for errors
