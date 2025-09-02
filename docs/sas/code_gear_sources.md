# Code-Based Gear Sources API Reference

Generated from decompiled sources on 2025-09-02 02:58:00 UTC

## CharacterObject (templates)

TaleWorlds.CampaignSystem.CharacterObject :: BattleEquipments { get; }
TaleWorlds.CampaignSystem.CharacterObject :: Culture { get; }
TaleWorlds.CampaignSystem.CharacterObject :: Tier { get; }
TaleWorlds.CampaignSystem.CharacterObject :: UpgradeTargets { get; }

## MBEquipmentRoster (rosters)

TaleWorlds.Core.MBEquipmentRoster :: AllEquipments { get; }
TaleWorlds.Core.MBEquipmentRoster :: DefaultEquipment { get; }
TaleWorlds.Core.MBEquipmentRoster :: EquipmentCulture { get; }
TaleWorlds.Core.MBEquipmentRoster :: EquipmentFlags { get; }
TaleWorlds.Core.MBEquipmentRoster :: HasEquipmentFlags(EquipmentFlags flags)
TaleWorlds.Core.MBEquipmentRoster :: IsEquipmentTemplate()
TaleWorlds.Core.MBEquipmentRoster :: MBEquipmentRoster()

## Roster Extensions (helpers)

// missing in this build: MBEquipmentRosterExtensions.GetAppropriateEquipmentRostersForHero
// missing in this build: MBEquipmentRosterExtensions.IsRosterAppropriateForHeroAsTemplate

## Equipment / Items (Core)

TaleWorlds.Core.Equipment :: Equipment()
TaleWorlds.Core.Equipment :: Equipment(bool isCivilian)
TaleWorlds.Core.Equipment :: Equipment(Equipment equipment)
TaleWorlds.Core.Equipment :: Clone(bool cloneWithoutWeapons)
TaleWorlds.Core.Equipment :: FillFrom(Equipment sourceEquipment, bool useSourceEquipmentType)
TaleWorlds.Core.Equipment :: GetEquipmentFromSlot(EquipmentIndex index)
TaleWorlds.Core.Equipment :: Horse { get; }
TaleWorlds.Core.Equipment :: IsCivilian { get; }
TaleWorlds.Core.Equipment :: IsValid { get; }
TaleWorlds.Core.Equipment :: this[EquipmentIndex index] { get; set; }
TaleWorlds.Core.Equipment :: this[int index] { get; set; }
TaleWorlds.Core.EquipmentElement :: EquipmentElement(ItemObject item, ItemModifier itemModifier, Banner banner, bool isQuestItem)
TaleWorlds.Core.EquipmentElement :: Item { get; }
TaleWorlds.Core.EquipmentElement :: ItemModifier { get; }
TaleWorlds.Core.EquipmentElement :: IsQuestItem { get; }
TaleWorlds.Core.EquipmentIndex :: Body
TaleWorlds.Core.EquipmentIndex :: Cape
TaleWorlds.Core.EquipmentIndex :: Gloves
TaleWorlds.Core.EquipmentIndex :: Head
TaleWorlds.Core.EquipmentIndex :: Horse
TaleWorlds.Core.EquipmentIndex :: HorseHarness
TaleWorlds.Core.EquipmentIndex :: Leg
TaleWorlds.Core.EquipmentIndex :: Weapon0
TaleWorlds.Core.EquipmentIndex :: Weapon1
TaleWorlds.Core.EquipmentIndex :: Weapon2
TaleWorlds.Core.EquipmentIndex :: Weapon3
TaleWorlds.Core.ItemObject :: Culture { get; }
TaleWorlds.Core.ItemObject :: ItemComponent { get; }
TaleWorlds.Core.ItemObject :: ItemType { get; }
TaleWorlds.Core.ItemObject :: Name { get; }
TaleWorlds.Core.ItemObject :: StringId { get; }
TaleWorlds.Core.ItemObject :: Value { get; }

## Hero & Culture

TaleWorlds.CampaignSystem.Hero :: BattleEquipment { get; set; }
TaleWorlds.CampaignSystem.Hero :: CharacterObject { get; }
TaleWorlds.CampaignSystem.Hero :: CivilianEquipment { get; set; }
TaleWorlds.CampaignSystem.Hero :: Culture { get; }
TaleWorlds.Core.BasicCultureObject :: StringId { get; }

## Object Lookup (MBObjectManager)

TaleWorlds.ObjectSystem.MBObjectManager :: GetObject<T>(string stringId)
TaleWorlds.ObjectSystem.MBObjectManager :: GetObjectTypeList<T>()
TaleWorlds.ObjectSystem.MBObjectManager :: Instance { get; }

## Equipment Selection Models

TaleWorlds.CampaignSystem.ComponentInterfaces.EquipmentSelectionModel :: GetEquipmentRostersForCompanion(Hero companionHero, bool isCivilian)
TaleWorlds.CampaignSystem.ComponentInterfaces.EquipmentSelectionModel :: GetEquipmentRostersForDeliveredOffspring(Hero hero)
TaleWorlds.CampaignSystem.ComponentInterfaces.EquipmentSelectionModel :: GetEquipmentRostersForHeroComeOfAge(Hero hero, bool isCivilian)
TaleWorlds.CampaignSystem.ComponentInterfaces.EquipmentSelectionModel :: GetEquipmentRostersForHeroReachesTeenAge(Hero hero)
TaleWorlds.CampaignSystem.ComponentInterfaces.EquipmentSelectionModel :: GetEquipmentRostersForInitialChildrenGeneration(Hero hero)
TaleWorlds.CampaignSystem.GameComponents.DefaultEquipmentSelectionModel :: GetEquipmentRostersForHeroComeOfAge(Hero hero, bool isCivilian)

### Missing/Not Found

- CharacterObject.All static accessor - Use MBObjectManager.GetObjectTypeList<CharacterObject>() instead
- MBEquipmentRosterExtensions.GetAppropriateEquipmentRostersForHero() - Not found in this build
- MBEquipmentRosterExtensions.IsRosterAppropriateForHeroAsTemplate() - Not found in this build
- CharacterObject.CivilianEquipments - Not found, use Hero.CivilianEquipment instead
- CharacterObject.AllEquipments - Not found, use BattleEquipments property
- CharacterObject.FirstBattleEquipment - Not found, use BattleEquipments[0] instead
- CharacterObject.RandomBattleEquipment - Not found, use random selection from BattleEquipments
