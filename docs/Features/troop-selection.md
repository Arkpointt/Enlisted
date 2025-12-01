# Feature Spec: Troop Selection System

## Overview
On promotion, players choose from real Bannerlord troops and receive their equipment, rather than getting predefined equipment kits.

## Purpose
Provide authentic military progression where you become actual troop types (Imperial Legionary, Aserai Mameluke) and inherit their gear, making promotions feel meaningful and immersive.

## Inputs/Outputs

**Inputs:**
- Player promotion trigger (XP threshold reached)
- Current enlisted lord's culture
- Player's new tier level
- Available troops in the game matching culture and tier

**Outputs:**
- Equipment completely replaced with selected troop's gear
- Player gains identity of chosen troop type
- Visual feedback showing equipment changes
- Progression message confirming new rank

## Behavior

1. **Promotion Trigger**: Player reaches XP threshold → "Promotion!" popup notification
2. **Troop Menu**: Shows real troops filtered by culture and tier
3. **Selection**: Player picks troop (Imperial Legionary, Battanian Fian, etc.)
4. **Equipment Copy**: Player gets exact equipment from `CharacterObject.BattleEquipments[0]`
5. **Application**: Uses `EquipmentHelper.AssignHeroEquipmentFromEquipment()` to apply safely
6. **Feedback**: Character model updates immediately, confirmation message shown

**Troop Filtering Logic:**
```csharp
// Find troops matching player's situation
var availableTroops = MBObjectManager.Instance.GetObjectTypeList<CharacterObject>()
    .Where(troop => troop.Culture == lordCulture)
    .Where(troop => troop.GetBattleTier() == playerTier)
    .Where(troop => troop.Occupation == Occupation.Soldier);
```

## Technical Implementation

**Files:**
- `TroopSelectionManager.cs` - Handles troop filtering and selection UI
- `EquipmentManager.cs` - Applies equipment from selected troops
- `PromotionBehavior.cs` - Triggers selection on tier advancement

**Key APIs:**
- `MBObjectManager.Instance.GetObjectTypeList<CharacterObject>()` - Get all game troops
- `CharacterObject.BattleEquipments` - Access troop equipment variants
- `EquipmentHelper.AssignHeroEquipmentFromEquipment()` - Apply equipment safely

## Edge Cases

**No Troops Available for Tier:**
- Expand tier search range (+1/-1 from exact tier)
- Fallback to basic equipment if no troops found
- Log warning but don't crash

**Equipment Assignment Fails:**
- Rollback to previous equipment state
- Show error message to player
- Log details for debugging

**Lord Culture Changes:**
- Update available troop pool when lord changes
- Handle culture transitions during service

**Multiple Equipment Sets on Troop:**
- Use first equipment set (`BattleEquipments[0]`)
- Future: Could allow selection between variants

## Acceptance Criteria

- ✅ Players see real troop names in promotion menu
- ✅ Equipment comes from actual game troops, not custom kits
- ✅ Filtering works correctly by culture and tier
- ✅ Equipment applies immediately with visual update
- ✅ No crashes during equipment assignment
- ✅ Works for all cultures (Empire, Aserai, Khuzait, Vlandia, Sturgia, Battania)
- ✅ Handles edge cases gracefully (missing troops, failed assignment)

## Debugging

**Common Issues:**
- **No troops in menu**: Check culture filtering and tier ranges
- **Equipment not applying**: Verify `EquipmentHelper` call succeeds
- **Wrong equipment**: Check which `BattleEquipments` index is being used

**Log Categories:**
- "TroopSelection" - Troop filtering and menu operations
- "Equipment" - Equipment application and management
- "Promotion" - Promotion triggers and tier advancement
