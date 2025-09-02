# Promotion & Equipment Helper APIs

Generated from "C:\Dev\Enlisted\DECOMPILE" on 2025-09-02 01:15:00 UTC

## Equipment Helper Methods

Helpers.EquipmentHelper :: AssignHeroEquipmentFromEquipment(Hero hero, Equipment equipment)

## Equipment Selection Model Methods

TaleWorlds.CampaignSystem.ComponentInterfaces.EquipmentSelectionModel :: GetEquipmentRostersForHeroComeOfAge(Hero hero, bool isCivilian)
TaleWorlds.CampaignSystem.ComponentInterfaces.EquipmentSelectionModel :: GetEquipmentRostersForCompanion(Hero companionHero, bool isCivilian)
TaleWorlds.CampaignSystem.ComponentInterfaces.EquipmentSelectionModel :: GetEquipmentRostersForDeliveredOffspring(Hero hero)
TaleWorlds.CampaignSystem.ComponentInterfaces.EquipmentSelectionModel :: GetEquipmentRostersForHeroReachesTeenAge(Hero hero)
TaleWorlds.CampaignSystem.ComponentInterfaces.EquipmentSelectionModel :: GetEquipmentRostersForInitialChildrenGeneration(Hero hero)
TaleWorlds.CampaignSystem.GameComponents.DefaultEquipmentSelectionModel :: GetEquipmentRostersForHeroComeOfAge(Hero hero, bool isCivilian)

## Object Manager Methods

TaleWorlds.ObjectSystem.MBObjectManager :: Instance { get; }
TaleWorlds.ObjectSystem.MBObjectManager :: GetObject<T>(string stringId)
TaleWorlds.ObjectSystem.MBObjectManager :: GetObjectTypeList<T>()

## Character Object Equipment Properties

TaleWorlds.CampaignSystem.CharacterObject :: Tier { get; }
TaleWorlds.CampaignSystem.CharacterObject :: UpgradeTargets { get; }

## Hero Equipment Accessors

TaleWorlds.CampaignSystem.Hero :: BattleEquipment { get; set; }
TaleWorlds.CampaignSystem.Hero :: CivilianEquipment { get; set; }
TaleWorlds.CampaignSystem.Hero :: CharacterObject { get; }

## Equipment Management Core

TaleWorlds.Core.Equipment :: Equipment()
TaleWorlds.Core.Equipment :: Equipment(bool isCivilian)
TaleWorlds.Core.Equipment :: Equipment(Equipment equipment)
TaleWorlds.Core.Equipment :: Clone(bool cloneWithoutWeapons)
TaleWorlds.Core.Equipment :: FillFrom(Equipment sourceEquipment, bool useSourceEquipmentType)
TaleWorlds.Core.Equipment :: this[EquipmentIndex index] { get; set; }

## Equipment Element Construction

TaleWorlds.Core.EquipmentElement :: EquipmentElement(ItemObject item, ItemModifier itemModifier, Banner banner, bool isQuestItem)
TaleWorlds.Core.EquipmentElement :: Item { get; }
TaleWorlds.Core.EquipmentElement :: ItemModifier { get; }

## Item Roster Management

TaleWorlds.CampaignSystem.Roster.ItemRoster :: AddToCounts(ItemObject item, int count)
TaleWorlds.CampaignSystem.Roster.ItemRoster :: Clear()
TaleWorlds.CampaignSystem.Roster.ItemRoster :: Remove(ItemRosterElement item)
TaleWorlds.CampaignSystem.Party.PartyBase :: ItemRoster { get; }

## Item Object Properties

TaleWorlds.Core.ItemObject :: ItemComponent { get; }
TaleWorlds.Core.ItemObject :: Name { get; }
TaleWorlds.Core.ItemObject :: StringId { get; }
TaleWorlds.Core.ItemObject :: Value { get; }

## Equipment Slot Enumeration

TaleWorlds.Core.EquipmentIndex :: Weapon0 (0)
TaleWorlds.Core.EquipmentIndex :: Weapon1 (1)
TaleWorlds.Core.EquipmentIndex :: Weapon2 (2)
TaleWorlds.Core.EquipmentIndex :: Weapon3 (3)
TaleWorlds.Core.EquipmentIndex :: Head (5)
TaleWorlds.Core.EquipmentIndex :: Body (6)
TaleWorlds.Core.EquipmentIndex :: Leg (7)
TaleWorlds.Core.EquipmentIndex :: Gloves (8)
TaleWorlds.Core.EquipmentIndex :: Cape (9)
TaleWorlds.Core.EquipmentIndex :: Horse (10)
TaleWorlds.Core.EquipmentIndex :: HorseHarness (11)

## Equipment Selection & Availability

TaleWorlds.CampaignSystem.CharacterObject :: BattleEquipments { get; }
TaleWorlds.CampaignSystem.Hero :: Culture { get; }
TaleWorlds.CampaignSystem.Models.EquipmentSelectionModel :: GetEquipmentRostersForHeroComeOfAge(Hero hero, bool isCivilian)
TaleWorlds.Core.MBEquipmentRoster :: DefaultEquipment { get; }

## Custom UI Framework (Gauntlet)

TaleWorlds.Engine.GauntletUI.GauntletLayer :: GauntletLayer(int localOrder, string categoryId, bool shouldClear)
TaleWorlds.Engine.GauntletUI.GauntletLayer :: LoadMovie(string movieName, object dataSource)
TaleWorlds.Engine.GauntletUI.GauntletLayer :: ReleaseMovie(GauntletMovie movie)
TaleWorlds.ScreenSystem.ScreenManager :: TopScreen { get; }
TaleWorlds.ScreenSystem.ScreenBase :: AddLayer(ScreenLayer layer)
TaleWorlds.ScreenSystem.ScreenBase :: RemoveLayer(ScreenLayer layer)

## SAS Equipment Selection Pattern

```csharp
// How SAS implements GetAvailableGear (from Test.cs:2190)
public static List<ItemObject> GetAvaliableGear(List<EquipmentIndex> indexes)
{
    List<CharacterObject> validTroopSets = new List<CharacterObject>();
    List<ItemObject> gear = new List<ItemObject>();
    
    // Get troops from lord's culture up to player's tier
    foreach (CharacterObject troop in GetTroopsList(followingHero.Culture))
    {
        if (troop.Tier <= EnlistTier)
            validTroopSets.Add(troop);
    }
    
    // Extract equipment from valid troop sets
    foreach (CharacterObject troop in validTroopSets)
    {
        foreach (Equipment equipment in troop.BattleEquipments)
        {
            foreach (EquipmentIndex index in indexes)
            {
                if (!gear.Contains(equipment[index].Item) && equipment[index].Item != null)
                    gear.Add(equipment[index].Item);
            }
        }
    }
    
    // High-tier bonus: access to hero equipment rosters
    if (EnlistTier > 6)
    {
        foreach (MBEquipmentRoster template in Campaign.Current.Models.EquipmentSelectionModel.GetEquipmentRostersForHeroComeOfAge(followingHero, false))
        {
            foreach (EquipmentIndex index in indexes)
            {
                if (!gear.Contains(template.DefaultEquipment[index].Item) && template.DefaultEquipment[index].Item != null)
                    gear.Add(template.DefaultEquipment[index].Item);
            }
        }
    }
    
    return gear;
}
```

## Missing/Not Found

- EquipmentHelper.AssignHeroEquipmentFromEquipmentRoster() - Not found, SAS implements custom GetAvaliableGear instead
- EquipmentHelper.GetRandomEquipmentFromEquipmentRoster() - Not found, SAS uses direct Equipment iteration
- SandBox.Inventory.InventoryManager.OpenScreen() methods - Not found, SAS uses custom Gauntlet UI
- GiveItemAction/TakeItemAction - Not found in Actions directory

## Implementation Strategy

SAS doesn't rely on missing helper methods - it implements its own equipment selection logic using:
1. **CharacterObject.BattleEquipments** to get troop equipment sets
2. **EquipmentSelectionModel.GetEquipmentRostersForHeroComeOfAge()** for high-tier equipment
3. **Custom Gauntlet UI** for equipment selection interface
4. **Direct Equipment object manipulation** for gear swapping
