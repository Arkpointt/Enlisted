# SandBox.ViewModelCollection Reference

## Relevance to Enlisted Mod: MEDIUM

This assembly contains ViewModels for the campaign UI - the data binding layer between game state and UI display. We patch `PartyNameplateVM` to hide the player's nameplate on the map.

---

## Key Classes and Systems

### Map ViewModels

| Class | Purpose | Enlisted Usage |
|-------|---------|----------------|
| `PartyNameplateVM` | Party nameplate on map | We patch to hide player nameplate |
| `MapNavigationVM` | Map navigation UI | Not directly used |
| `MapVM` | Main map ViewModel | Map state |

### Menu ViewModels

| Class | Purpose | Enlisted Usage |
|-------|---------|----------------|
| `MenuVM` | Game menu ViewModel | Menu display |
| `GameMenuItemVM` | Menu option ViewModel | Option display |

### Party/Character ViewModels

| Class | Purpose | Enlisted Usage |
|-------|---------|----------------|
| `PartyVM` | Party screen ViewModel | Party management UI |
| `CharacterVM` | Character ViewModel | Character display |

---

## Our PartyNameplateVM Patch

We patch to hide the player's party nameplate when enlisted:

```csharp
[HarmonyPatch(typeof(PartyNameplateVM), "DetermineIsVisibleOnMap")]
[HarmonyPostfix]
static void DetermineIsVisiblePostfix(PartyNameplateVM __instance)
{
    if (EnlistmentBehavior.Instance?.IsEnlisted != true)
        return;
        
    if (__instance.Party == PartyBase.MainParty)
    {
        // Hide the nameplate
        __instance.IsVisibleOnMap = false;
    }
}
```

---

## ViewModel Pattern

ViewModels use property change notification:

```csharp
public class SomeVM : ViewModel
{
    private bool _isVisible;
    
    [DataSourceProperty]
    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible != value)
            {
                _isVisible = value;
                OnPropertyChanged(nameof(IsVisible));
            }
        }
    }
}
```

---

## Files to Study

| File | Why |
|------|-----|
| `PartyNameplateVM.cs` | Nameplate ViewModel we patch |
| `MenuVM.cs` | Menu display |
| `MapVM.cs` | Map UI coordination |

---

## Nameplate Properties

`PartyNameplateVM` has these key properties:

| Property | Purpose |
|----------|---------|
| `IsVisibleOnMap` | Whether nameplate shows |
| `Party` | The party this represents |
| `IsMainParty` | If this is player's party |
| `Name` | Display name |
| `TroopCount` | Troop count display |

---

## Potential Future Use

If we add custom UI elements, we'd create ViewModels following this pattern:
- Inherit from `ViewModel`
- Use `[DataSourceProperty]` attributes
- Call `OnPropertyChanged` when values change
- Bind to XML prefabs

