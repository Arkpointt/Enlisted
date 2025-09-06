# Equipment Selection Implementation Pipeline

Step-by-step implementation plan using verified decompiled APIs

## Step 1: Enumerate Character Templates
```csharp
var allCharacters = MBObjectManager.Instance.GetObjectTypeList<CharacterObject>();
```
**Purpose**: Get all available troop templates in the game for equipment extraction.

## Step 2: Filter by Culture and Tier
```csharp
var cultureTemplates = allCharacters.Where(c => 
    c.Culture == targetCulture && c.Tier <= maxTier);
```
**Purpose**: Only consider troops appropriate for the enlisted lord's culture and player's rank.

## Step 3: Extract Equipment Collections
```csharp
foreach (var character in cultureTemplates)
{
    foreach (var equipment in character.BattleEquipments)
    {
        // Process each equipment set
    }
}
```
**Purpose**: Access the equipment sets that each qualifying troop template uses.

## Step 4: Map Equipment Slots to Items
```csharp
for (EquipmentIndex slot = EquipmentIndex.Weapon0; slot <= EquipmentIndex.HorseHarness; slot++)
{
    var item = equipment[slot].Item;
    if (item != null)
    {
        itemsBySlot[slot] = item.StringId;
    }
}
```
**Purpose**: Create a mapping of equipment slots to specific item IDs for each equipment set.

## Step 5: Enrich with High-Tier Rosters
```csharp
if (playerTier > 6)
{
    var heroRosters = Campaign.Current.Models.EquipmentSelectionModel
        .GetEquipmentRostersForHeroComeOfAge(hero, false);
    foreach (var roster in heroRosters)
    {
        // Extract premium equipment items
    }
}
```
**Purpose**: Add elite equipment options for high-ranking enlisted soldiers.

## Step 6: Apply Equipment Filters
```csharp
var validRosters = heroRosters.Where(r => 
    r.EquipmentCulture == targetCulture && 
    r.HasEquipmentFlags(EquipmentFlags.IsMediumTemplate));
```
**Purpose**: Ensure equipment is culturally appropriate and suitable for military service.

## Step 7: Deduplicate and Organize
```csharp
var uniqueItems = new HashSet<string>();
var organizedGear = new Dictionary<EquipmentIndex, List<string>>();
```
**Purpose**: Remove duplicate items and organize by equipment slot for efficient selection.

## Step 8: Build Runtime Index
```csharp
var gearIndex = new Dictionary<string, Dictionary<int, List<EquipmentSet>>>();
gearIndex[cultureId][tier] = equipmentSets;
```
**Purpose**: Create the final data structure for fast equipment lookup during promotion and selection.

## Fallback Strategies

### If BattleEquipments is Empty
**Fallback**: Use `Hero.BattleEquipment` from actual hero characters
**Implementation**: Filter heroes by culture and use their current equipment

### If EquipmentSelectionModel Unavailable
**Fallback**: Rely on CharacterObject equipment only
**Implementation**: Use tier-based filtering and string pattern matching

### If Culture Property Missing
**Fallback**: Use item StringId pattern matching
**Implementation**: Check for culture prefixes (empire_, aserai_, vlandia_, etc.)

## Expected Output Structure

```json
{
  "empire": {
    "1": [
      {
        "source": "CharacterObject.BattleEquipments",
        "sourceTroop": "empire_recruit", 
        "itemsBySlot": {
          "Weapon0": "imperial_sword_t1",
          "Head": "imperial_leather_cap",
          "Body": "imperial_padded_armor"
        }
      }
    ],
    "2": [ /* tier 2 equipment sets */ ],
    "3": [ /* tier 3 equipment sets */ ]
  }
}
```

## Implementation Priority

1. **Primary**: CharacterObject.BattleEquipments for standard military equipment
2. **Secondary**: EquipmentSelectionModel for high-tier bonus equipment  
3. **Tertiary**: Manual item filtering by StringId patterns as ultimate fallback

This pipeline ensures we can build a comprehensive equipment selection system using only verified APIs from our decompiled sources.