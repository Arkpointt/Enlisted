# Equipment Reference - Complete Guide

**Equipment system documentation and item categorization for the Enlisted mod**

## 🎯 **SAS Troop Selection System**

**SYSTEM CHANGE**: **Switched from custom equipment kits to SAS-style troop selection for better player experience**

**How It Works**: 
1. **Culture Determined by Lord**: When players enlist with a lord, that lord's culture (`EnlistmentBehavior.Instance.CurrentLord.Culture.StringId`) determines which **real Bannerlord troops** are available
2. **Promotion Notifications**: When XP threshold reached → *"Promotion available! Press 'P' to advance."*  
3. **Real Troop Selection**: Player chooses from actual game troops (*"Imperial Legionary", "Aserai Mameluke", etc.*) 
4. **Authentic Equipment**: Equipment extracted directly from `CharacterObject.BattleEquipments[0]`

**Why This Is Better**:
- ✅ **More Fun**: Real troop identity vs abstract progression  
- ✅ **Immersive**: Players recognize troops from battles
- ✅ **Authentic**: Uses actual game equipment (no custom maintenance)
- ✅ **Cultural**: Each faction's troops feel unique and flavorful

## 🎖️ **Troop Discovery Pipeline**

**NEW APPROACH**: **Extract equipment directly from real Bannerlord troop templates**

**Troop Selection Implementation**:
```csharp
// 1. Get all character templates for culture and tier
var culture = MBObjectManager.Instance.GetObject<CultureObject>(cultureId);
var allTroops = MBObjectManager.Instance.GetObjectTypeList<CharacterObject>();
var availableTroops = allTroops.Where(troop => 
    troop.Culture == culture && 
    troop.Tier == tier &&
    troop.IsSoldier).ToList();

// 2. Player selects from real troop names
// "Imperial Legionary", "Aserai Mameluke", "Battanian Fian Champion"

// 3. Apply selected troop's authentic equipment  
var selectedEquipment = selectedTroop.BattleEquipments.FirstOrDefault();
EquipmentHelper.AssignHeroEquipmentFromEquipment(hero, selectedEquipment);
```

**Benefits**:
- ✅ **No custom item maintenance**: Uses game's existing equipment data
- ✅ **Always accurate**: Equipment matches what that troop actually uses in battles  
- ✅ **Cultural authenticity**: Each culture's troops have their unique gear styles

## ⚔️ **Equipment Replacement System**

**CRITICAL: Equipment REPLACEMENT (Not Accumulation)**

**During Service** (Promotions):
```csharp
// When player gets promoted and selects new troop:
// 1. Previous equipment is REMOVED (turned in to quartermaster)
// 2. New troop's equipment is ASSIGNED (issued by army)
// 3. Player does NOT keep both sets of equipment

EquipmentHelper.AssignHeroEquipmentFromEquipment(hero, newTroopEquipment);
// This REPLACES all equipment slots - player loses old gear
```

**At Retirement Only**:
```csharp
// After 1+ year of service (Tier 5+):
// Player keeps FINAL equipment as retirement benefit
// Example: Retire as "Imperial Cataphract" → Keep elite heavy cavalry gear
```

**Example Equipment Flow**:
1. **Enlist**: Get Imperial Recruit gear (basic sword, leather armor)
2. **Tier 2 Promotion**: **REPLACE** with Imperial Legionary gear (better sword, scale armor) 
3. **Tier 5 Promotion**: **REPLACE** with Imperial Cataphract gear (elite cavalry equipment)
4. **Retire**: **KEEP** final Cataphract gear permanently

**Rationale**: Realistic military service - equipment belongs to the army, not the soldier (except as retirement benefit)

## 🛡️ **Equipment Roster Structure**

Equipment rosters define character loadouts with culture-specific gear:

```xml
<EquipmentRoster id="roster_id" culture="Culture.faction_name">
    <EquipmentSet>
        <Equipment slot="Item0" id="Item.weapon_id" />
        <Equipment slot="Body" id="Item.armor_id" />
        <Equipment slot="Leg" id="Item.boots_id" />
        <!-- Additional slots: Item1-3, Head, Gloves, Cape, Horse, HorseHarness -->
    </EquipmentSet>
</EquipmentRoster>
```

## 🔍 **Item Categories**

### Weapons (by ItemType)
- **OneHandedWeapon** (0) - Swords, maces, axes
- **TwoHandedWeapon** (1) - Two-handed swords, axes, maces  
- **Polearm** (2) - Spears, glaives, pikes
- **Bow** (3) - Bows and hunting bows
- **Crossbow** (4) - Crossbows and siege crossbows
- **Thrown** (5) - Throwing axes, javelins, stones
- **Shield** (6) - All shield types
- **Arrows** (7) - Arrow ammunition
- **Bolts** (8) - Crossbow bolt ammunition

### Armor & Equipment
- **HeadArmor** (9) - Helmets, caps, crowns
- **BodyArmor** (10) - Chest armor, robes, clothing
- **LegArmor** (11) - Boots, shoes, leg armor
- **HandArmor** (12) - Gloves, gauntlets
- **Cape** (13) - Cloaks, capes, back items
- **Horse** (14) - All mount types
- **HorseHarness** (15) - Horse armor and decoration
- **Banner** (22) - Banners and standards
- **ChestArmor** (23) - Additional chest armor category
- **Pistol** (24) - Firearms (if available)
- **Musket** (25) - Long firearms (if available)

## 🎯 **Equipment Slot Mapping**

- **Item0-3**: Weapon slots (Weapon0-3 in EquipmentIndex)
- **Head**: Helmet slot (Head in EquipmentIndex)
- **Body**: Armor slot (Body in EquipmentIndex)
- **Leg**: Boot slot (Leg in EquipmentIndex)
- **Gloves**: Glove slot (Gloves in EquipmentIndex)
- **Cape**: Cape slot (Cape in EquipmentIndex)
- **Horse**: Mount slot (Horse in EquipmentIndex)
- **HorseHarness**: Horse armor slot (HorseHarness in EquipmentIndex)

## 🚀 **Equipment Discovery Pipeline**

### Step-by-Step Implementation

**Step 1**: Enumerate Character Templates
```csharp
var allCharacters = MBObjectManager.Instance.GetObjectTypeList<CharacterObject>();
```

**Step 2**: Filter by Culture and Tier
```csharp
var cultureTemplates = allCharacters.Where(c => 
    c.Culture == targetCulture && c.Tier <= maxTier);
```

**Step 3**: Extract Equipment Collections
```csharp
foreach (var character in cultureTemplates)
{
    foreach (var equipment in character.BattleEquipments)
    {
        // Process each equipment set
    }
}
```

**Step 4**: Map Equipment Slots to Items
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

**Step 5**: High-Tier Equipment Enhancement
```csharp
if (playerTier > 6)
{
    var heroRosters = Campaign.Current.Models.EquipmentSelectionModel
        .GetEquipmentRostersForHeroComeOfAge(hero, false);
    // Extract premium equipment items
}
```

## 📊 **Equipment Categories by Tier & Culture**

### By Tier Level
- **T1-T2**: Basic equipment for low-tier soldiers
- **T3-T4**: Standard equipment for mid-tier soldiers  
- **T5-T6**: Elite equipment for high-tier soldiers
- **T7**: Legendary/Noble equipment for master veterans

### By Culture Focus
- **Empire**: Roman-style heavy infantry gear (legionary equipment)
- **Aserai**: Desert cavalry and archer gear (mameluke equipment)
- **Sturgia**: Nordic infantry and archer gear (huscarl equipment)
- **Vlandia**: Western cavalry and infantry gear (knightly equipment)
- **Khuzait**: Steppe horse archer gear (nomadic equipment)
- **Battania**: Celtic forest fighter gear (clan warrior equipment)

## 🔧 **Implementation Usage**

```csharp
// Access equipment rosters via EquipmentSelectionModel
public List<ItemObject> GetHighTierEquipment(Hero hero, EquipmentIndex slot)
{
    var equipmentRosters = Campaign.Current.Models.EquipmentSelectionModel
        .GetEquipmentRostersForHeroComeOfAge(hero, false);
        
    var availableItems = new List<ItemObject>();
    foreach (var roster in equipmentRosters)
    {
        var item = roster.DefaultEquipment[slot].Item;
        if (item != null && !availableItems.Contains(item))
            availableItems.Add(item);
    }
    
    return availableItems;
}

// Get all available items by tier and culture
public List<ItemObject> GetAvailableItemsByTier(CultureObject culture, int maxTier, EquipmentIndex slot)
{
    var availableItems = new List<ItemObject>();
    var allItems = MBObjectManager.Instance.GetObjectTypeList<ItemObject>();
    
    foreach (var item in allItems)
    {
        if (item.Culture == culture && 
            GetItemTier(item) <= maxTier && 
            IsItemForSlot(item, slot))
        {
            availableItems.Add(item);
        }
    }
    
    return availableItems;
}
```

## 🎮 **Equipment Filtering**

### Filter by Equipment Flags
- **IsCombatantTemplate** - Suitable for military service
- **IsCivilianTemplate** - Civilian clothing only
- **IsMediumTemplate** - Standard military equipment
- **IsChildEquipmentTemplate** - Child-sized equipment
- **IsNobleTemplate** - High-tier noble equipment

### Culture Appropriateness
- Equipment rosters filtered by culture match
- Age appropriateness for adult soldiers
- Noble status for higher-tier equipment
- Military suitability for enlisted service

**This system ensures culture-appropriate, tier-appropriate equipment selection for enlisted soldiers with realistic military progression.**
