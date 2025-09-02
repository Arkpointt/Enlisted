# Item Categories & Equipment Reference

Generated from Bannerlord XML files on 2025-09-02 01:30:00 UTC

## Item Types (from mpitems.xml)

### Weapon Types
- **OneHandedWeapon** - Swords, axes, maces
- **TwoHandedWeapon** - Two-handed swords, axes, polearms
- **Polearm** - Spears, pikes, halberds
- **Bow** - Bows and crossbows
- **Crossbow** - Crossbows specifically
- **Thrown** - Throwing weapons (javelins, throwing axes)
- **Arrows** - Arrow ammunition
- **Bolts** - Crossbow bolt ammunition

### Armor Types
- **HeadArmor** - Helmets, caps, crowns
- **BodyArmor** - Chest armor, robes, tunics
- **LegArmor** - Leg armor, boots, shoes
- **HandArmor** - Gloves, gauntlets
- **Cape** - Capes, cloaks

### Mount Types
- **Horse** - Horses, camels, mules
- **HorseHarness** - Horse armor and saddles

## Item Properties Structure

```xml
<Item id="item_string_id" 
      name="{=localization_key}Display Name"
      culture="Culture.faction_name"
      value="gold_value"
      weight="weight_in_kg"
      Type="ItemType">
    <ItemComponent>
        <Armor head_armor="protection_value" ... />
        <!-- or -->
        <Weapon weapon_class="WeaponClass" ... />
    </ItemComponent>
    <Flags Civilian="true/false" UseTeamColor="true/false" />
</Item>
```

## Culture-Specific Item Patterns

### Empire Items
- **Prefix**: `empire_` (e.g., `empire_sword_1_t2`, `empire_sword_4_t4`)
- **Tiers**: `_t2`, `_t4` indicate tier levels
- **Style**: Roman-inspired equipment

### Aserai Items  
- **Prefix**: `aserai_` (e.g., `mp_aserai_civil_c_head`)
- **Style**: Desert/Middle Eastern equipment
- **Special**: Keffiyeh, turbans, desert robes

### Vlandia Items
- **Prefix**: `vlandia_` (e.g., `vlandia_sword_3_t4`)
- **Style**: Western European medieval equipment

## Item Categories for Equipment Selection

### By Tier (T1-T6)
- **T1-T2**: Basic equipment for low-tier soldiers
- **T3-T4**: Standard equipment for mid-tier soldiers  
- **T5-T6**: Elite equipment for high-tier soldiers

### By Culture
- **Culture.empire**: Roman-style heavy infantry gear
- **Culture.aserai**: Desert cavalry and archer gear
- **Culture.sturgia**: Nordic infantry and archer gear
- **Culture.vlandia**: Western cavalry and infantry gear
- **Culture.khuzait**: Steppe horse archer gear
- **Culture.battania**: Celtic forest fighter gear

## Usage in SAS Equipment System

```csharp
// Get culture-appropriate items by tier (Phase 2.2)
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

private int GetItemTier(ItemObject item)
{
    // Extract tier from item ID (e.g., "_t4" = tier 4)
    if (item.StringId.Contains("_t"))
    {
        var tierPart = item.StringId.Substring(item.StringId.LastIndexOf("_t") + 2);
        if (int.TryParse(tierPart, out int tier))
            return tier;
    }
    return 1; // Default to tier 1
}
```
