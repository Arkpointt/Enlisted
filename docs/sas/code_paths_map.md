# Code Paths Mapping

API signatures mapped to decompiled source locations

| Section | Full.Type | Member | File | Line |
|---------|-----------|---------|------|------|
| CharacterObject | TaleWorlds.CampaignSystem.CharacterObject | BattleEquipments { get; } | CharacterObject.cs | ~300 |
| CharacterObject | TaleWorlds.CampaignSystem.CharacterObject | Culture { get; } | CharacterObject.cs | ~250 |
| CharacterObject | TaleWorlds.CampaignSystem.CharacterObject | Tier { get; } | CharacterObject.cs | ~200 |
| CharacterObject | TaleWorlds.CampaignSystem.CharacterObject | UpgradeTargets { get; } | CharacterObject.cs | ~400 |
| MBEquipmentRoster | TaleWorlds.Core.MBEquipmentRoster | AllEquipments { get; } | MBEquipmentRoster.cs | 32 |
| MBEquipmentRoster | TaleWorlds.Core.MBEquipmentRoster | DefaultEquipment { get; } | MBEquipmentRoster.cs | 46 |
| MBEquipmentRoster | TaleWorlds.Core.MBEquipmentRoster | EquipmentCulture { get; } | MBEquipmentRoster.cs | 171 |
| MBEquipmentRoster | TaleWorlds.Core.MBEquipmentRoster | EquipmentFlags { get; } | MBEquipmentRoster.cs | 16 |
| Equipment | TaleWorlds.Core.Equipment | Equipment() | Equipment.cs | 60 |
| Equipment | TaleWorlds.Core.Equipment | Equipment(bool isCivilian) | Equipment.cs | 67 |
| Equipment | TaleWorlds.Core.Equipment | this[EquipmentIndex index] { get; set; } | Equipment.cs | 100 |
| Equipment | TaleWorlds.Core.Equipment | GetEquipmentFromSlot(EquipmentIndex index) | Equipment.cs | ~200 |
| EquipmentElement | TaleWorlds.Core.EquipmentElement | Item { get; } | EquipmentElement.cs | 48 |
| EquipmentElement | TaleWorlds.Core.EquipmentElement | ItemModifier { get; } | EquipmentElement.cs | 52 |
| EquipmentIndex | TaleWorlds.Core.EquipmentIndex | Weapon0 | EquipmentIndex.cs | 13 |
| EquipmentIndex | TaleWorlds.Core.EquipmentIndex | Head | EquipmentIndex.cs | 31 |
| EquipmentIndex | TaleWorlds.Core.EquipmentIndex | Body | EquipmentIndex.cs | 33 |
| ItemObject | TaleWorlds.Core.ItemObject | Culture { get; } | ItemObject.cs | 201 |
| ItemObject | TaleWorlds.Core.ItemObject | StringId { get; } | ItemObject.cs | ~100 |
| ItemObject | TaleWorlds.Core.ItemObject | ItemType { get; } | ItemObject.cs | ~150 |
| Hero | TaleWorlds.CampaignSystem.Hero | BattleEquipment { get; set; } | Hero.cs | ~800 |
| Hero | TaleWorlds.CampaignSystem.Hero | CivilianEquipment { get; set; } | Hero.cs | ~810 |
| Hero | TaleWorlds.CampaignSystem.Hero | Culture { get; } | Hero.cs | ~200 |
| MBObjectManager | TaleWorlds.ObjectSystem.MBObjectManager | Instance { get; } | MBObjectManager.cs | ~20 |
| MBObjectManager | TaleWorlds.ObjectSystem.MBObjectManager | GetObject<T>(string stringId) | MBObjectManager.cs | ~50 |
| MBObjectManager | TaleWorlds.ObjectSystem.MBObjectManager | GetObjectTypeList<T>() | MBObjectManager.cs | ~80 |
| EquipmentSelectionModel | TaleWorlds.CampaignSystem.ComponentInterfaces.EquipmentSelectionModel | GetEquipmentRostersForHeroComeOfAge(Hero hero, bool isCivilian) | EquipmentSelectionModel.cs | 11 |
| DefaultEquipmentSelectionModel | TaleWorlds.CampaignSystem.GameComponents.DefaultEquipmentSelectionModel | GetEquipmentRostersForHeroComeOfAge(Hero hero, bool isCivilian) | DefaultEquipmentSelectionModel.cs | 14 |

## Key Implementation Paths

### Primary Equipment Access
- **Source**: `MBObjectManager.GetObjectTypeList<CharacterObject>()`
- **Filter**: `character.Culture` and `character.Tier`
- **Extract**: `character.BattleEquipments` collection
- **Items**: `equipment[slot].Item.StringId`

### High-Tier Equipment Access  
- **Source**: `Campaign.Current.Models.EquipmentSelectionModel`
- **Method**: `GetEquipmentRostersForHeroComeOfAge(hero, false)`
- **Extract**: `roster.DefaultEquipment[slot].Item`
- **Usage**: Tier 6+ equipment bonuses

### Equipment Assignment
- **Target**: `Hero.BattleEquipment` or `Hero.CivilianEquipment`
- **Method**: `EquipmentHelper.AssignHeroEquipmentFromEquipment(hero, equipment)`
- **Construction**: `new Equipment(false)` then populate slots

## Missing APIs (Use Alternatives)

- **CharacterObject.All**: Use `MBObjectManager.GetObjectTypeList<CharacterObject>()`
- **Equipment Extensions**: Use direct Equipment object manipulation
- **Roster Helpers**: Implement custom filtering logic using available properties
