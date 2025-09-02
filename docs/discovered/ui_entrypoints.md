# UI Entry Points

Generated from "C:\Dev\Enlisted\DECOMPILE" on 2025-09-02 01:20:00 UTC

## Inventory Screen Openers

| Type::Method | Parameters | Notes |
|---|---|---|
| InventoryManager::OpenScreenAsInventory | (DoneLogicExtrasDelegate) | Basic inventory screen |
| InventoryManager::OpenScreenAsInventoryOf | (MobileParty, CharacterObject) | Party inventory view |
| InventoryManager::OpenScreenAsInventoryOf | (PartyBase, PartyBase) | Two-party inventory |
| InventoryManager::OpenScreenAsInventoryOfSubParty | (MobileParty, MobileParty, DoneLogicExtrasDelegate) | Sub-party management |
| InventoryManager::OpenScreenAsLoot | (Dictionary<PartyBase, ItemRoster>) | Battle loot screen |
| InventoryManager::OpenScreenAsReceiveItems | (ItemRoster, TextObject, DoneLogicExtrasDelegate) | Item receiving screen |
| InventoryManager::OpenScreenAsStash | (ItemRoster) | Stash management |
| InventoryManager::OpenScreenAsTrade | (ItemRoster, SettlementComponent, InventoryCategoryType, DoneLogicExtrasDelegate) | Trading screen |
| InventoryManager::OpenTradeWithCaravanOrAlleyParty | (MobileParty, InventoryCategoryType) | Caravan trading |

## Custom UI Creation (Gauntlet Alternative)

| Type::Method | Parameters | Notes |
|---|---|---|
| GauntletLayer::GauntletLayer | (int, string, bool) | Custom UI layer creation |
| GauntletLayer::LoadMovie | (string, object) | Load UI with data source |
| ScreenManager::TopScreen | { get; } | Current screen access |
| ScreenBase::AddLayer | (ScreenLayer) | Add UI layer to screen |

## Screen Management

| Type::Method | Parameters | Notes |
|---|---|---|
| MapScreen::Instance | { get; } | Main map screen access |
| InventoryManager::CloseInventoryPresentation | (bool) | Close inventory screen |

## Implementation Strategy

**For SAS Equipment Selection:**
1. **Option A**: Use `InventoryManager.OpenScreenAsInventoryOf()` for equipment management
2. **Option B**: Create custom Gauntlet UI like original SAS (more control)
3. **Option C**: Use dialog-based equipment selection (simplest, most compatible)

**Recommended Approach**: Start with dialog-based selection (Phase 2), add custom Gauntlet UI in Phase 4 for enhanced experience.
