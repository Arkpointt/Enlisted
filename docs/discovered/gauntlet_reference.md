# Gauntlet UI Reference

Generated from "C:\Dev\Enlisted\DECOMPILE" on 2025-09-02 01:25:00 UTC

## Screen Stack Operations

TaleWorlds.ScreenSystem.ScreenManager :: TopScreen { get; }
TaleWorlds.ScreenSystem.ScreenBase :: AddLayer(ScreenLayer layer)
TaleWorlds.ScreenSystem.ScreenBase :: RemoveLayer(ScreenLayer layer)
TaleWorlds.ScreenSystem.ScreenBase :: IsActive { get; }
TaleWorlds.ScreenSystem.ScreenBase :: Layers { get; }

## Gauntlet Layer Creation

TaleWorlds.Engine.GauntletUI.GauntletLayer :: GauntletLayer(int localOrder, string categoryId, bool shouldClear)
TaleWorlds.Engine.GauntletUI.GauntletLayer :: LoadMovie(string movieName, object dataSource)
TaleWorlds.Engine.GauntletUI.GauntletLayer :: ReleaseMovie(GauntletMovie movie)
TaleWorlds.Engine.GauntletUI.GauntletLayer :: IsFocusLayer { get; set; }
TaleWorlds.Engine.GauntletUI.GauntletLayer :: InputRestrictions { get; }

## VM Command Patterns (from SAS analysis)

Common ViewModel command patterns to mirror:
- ExecuteDone()
- ExecuteCancel()
- ExecuteConfirm()
- ExecuteEquip()
- ExecuteSelect()
- ExecuteClose()
- RefreshValues()

## SAS Custom Equipment Selector Pattern

```csharp
// How SAS creates custom equipment UI (from EquipmentSelectorBehavior.cs)
public static void CreateVMLayer(List<ItemObject> list, string equipmentType)
{
    layer = new GauntletLayer(1001, "GauntletLayer", false);
    equipmentSelectorVM = new SASEquipmentSelectorVM(list, equipmentType);
    equipmentSelectorVM.RefreshValues();
    gauntletMovie = layer.LoadMovie("SASEquipmentSelection", equipmentSelectorVM);
    layer.InputRestrictions.SetInputRestrictions(true, 7);
    ScreenManager.TopScreen.AddLayer(layer);
    layer.IsFocusLayer = true;
    ScreenManager.TrySetFocus(layer);
}

public static void DeleteVMLayer()
{
    layer.InputRestrictions.ResetInputRestrictions();
    layer.IsFocusLayer = false;
    layer.ReleaseMovie(gauntletMovie);
    ScreenManager.TopScreen.RemoveLayer(layer);
    layer = null;
    gauntletMovie = null;
    equipmentSelectorVM = null;
}
```

## Implementation Strategy

**For Custom Equipment Selection UI:**
1. Create custom ViewModel inheriting from ViewModel base class
2. Use GauntletLayer(1001, "CustomLayer", false) for UI layer
3. Load custom movie with LoadMovie("MovieName", viewModel)
4. Handle input restrictions and focus management
5. Clean up properly with ReleaseMovie() and RemoveLayer()

**Input Handling:**
- Set InputRestrictions for modal behavior
- Use IsFocusLayer for focus management
- Handle keyboard/gamepad input through layer.Input

**Lifecycle:**
1. Create → 2. Load Movie → 3. Add to Screen → 4. Set Focus → 5. Handle Input → 6. Clean Up
