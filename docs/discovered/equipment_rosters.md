# Equipment Rosters Reference

Generated from sandbox_equipment_sets.xml on 2025-09-02 01:30:00 UTC

## Equipment Roster Structure

Equipment rosters define character loadouts with culture-specific gear:

```xml
<EquipmentRoster id="roster_id" culture="Culture.faction_name">
    <EquipmentSet>
        <Equipment slot="Item0" id="Item.weapon_id" />
        <Equipment slot="Body" id="Item.armor_id" />
        <Equipment slot="Leg" id="Item.boots_id" />
        <!-- Additional slots: Item1-3, Head, Gloves, Cape, Horse, HorseHarness -->
    </EquipmentSet>
    <!-- Multiple EquipmentSet entries for variations -->
</EquipmentRoster>
```

## Sample Equipment Rosters by Culture

### Empire Equipment Rosters
- **spc_notable_empire_5**: Basic empire sword + monk robe
- **spc_notable_empire_8**: Tier 4 empire sword + cloth tunic
- **spc_notable_empire_9**: Tier 4 empire sword + tied cloth tunic

### Vlandia Equipment Rosters  
- **spc_wanderer_vlandia_0**: Tier 4 vlandia sword + monk robe

## Equipment Slot Mapping

- **Item0-3**: Weapon slots (Weapon0-3 in EquipmentIndex)
- **Head**: Helmet slot (Head in EquipmentIndex)
- **Body**: Armor slot (Body in EquipmentIndex)
- **Leg**: Boot slot (Leg in EquipmentIndex)
- **Gloves**: Glove slot (Gloves in EquipmentIndex)
- **Cape**: Cape slot (Cape in EquipmentIndex)
- **Horse**: Mount slot (Horse in EquipmentIndex)
- **HorseHarness**: Horse armor slot (HorseHarness in EquipmentIndex)

## Usage in SAS Implementation

```csharp
// Access equipment rosters via EquipmentSelectionModel (Phase 2.2)
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
```

## Equipment Roster Categories

Equipment rosters are filtered by:
- **Culture**: Must match hero's culture
- **Equipment Flags**: IsCombatantTemplate, IsCivilianTemplate, IsMediumTemplate, etc.
- **Age Appropriateness**: IsChildEquipmentTemplate, IsTeenagerEquipmentTemplate
- **Noble Status**: IsNobleTemplate for higher-tier equipment

This system ensures culture-appropriate, tier-appropriate equipment selection for SAS enlisted soldiers.
