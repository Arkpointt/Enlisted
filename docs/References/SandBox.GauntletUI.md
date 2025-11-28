# SandBox.GauntletUI Reference

## Relevance to Enlisted Mod: HIGH

This assembly contains the Gauntlet UI framework for campaign/sandbox mode. We use it for our Quartermaster equipment selection system and Troop Selection popup.

---

## Our Gauntlet UI Usage

### Equipment System UI

| Component | Purpose |
|-----------|---------|
| `QuartermasterEquipmentSelectorVM` | Main equipment grid ViewModel |
| `QuartermasterEquipmentItemVM` | Individual equipment item |
| `QuartermasterEquipmentRowVM` | Equipment row container |
| `TroopSelectionPopupVM` | Troop selection dialog |

### XML Prefabs

Located in `GUI/Prefabs/Equipment/`:
- `QuartermasterEquipmentGrid.xml` - Main equipment selection UI
- `QuartermasterEquipmentCard.xml` - Individual item card
- `QuartermasterEquipmentCardRow.xml` - Row of items
- `TroopSelectionPopup.xml` - Troop selection modal

---

## Key Classes

### Screen Classes

| Class | Purpose | Enlisted Usage |
|-------|---------|----------------|
| `GauntletLayer` | UI layer that hosts Gauntlet movies | Our equipment screens |
| `GauntletPartyScreen` | Party management UI | Study for patterns |
| `GauntletInventoryScreen` | Inventory UI | Study for equipment patterns |

### From TaleWorlds.Library

| Class | Purpose |
|-------|---------|
| `ViewModel` | Base class for ViewModels |
| `MBBindingList<T>` | Observable collection for UI binding |
| `DataSourceProperty` | Attribute for bindable properties |

---

## Gauntlet UI Pattern

### ViewModel Structure

```csharp
public class MyScreenVM : ViewModel
{
    private string _title;
    private MBBindingList<ItemVM> _items;

    [DataSourceProperty]
    public string Title
    {
        get => _title;
        set
        {
            if (_title != value)
            {
                _title = value;
                OnPropertyChanged(nameof(Title));
            }
        }
    }

    [DataSourceProperty]
    public MBBindingList<ItemVM> Items
    {
        get => _items;
        set
        {
            if (_items != value)
            {
                _items = value;
                OnPropertyChanged(nameof(Items));
            }
        }
    }

    // Commands for button clicks
    public void ExecuteSelectItem()
    {
        // Handle selection
    }
}
```

### XML Prefab Structure

```xml
<Prefab>
  <Window>
    <Widget WidthSizePolicy="Fixed" HeightSizePolicy="Fixed" 
            SuggestedWidth="800" SuggestedHeight="600">
      
      <!-- Title -->
      <TextWidget Text="@Title" />
      
      <!-- Item List -->
      <ListPanel DataSource="{Items}">
        <ItemTemplate>
          <QuartermasterEquipmentCard />
        </ItemTemplate>
      </ListPanel>
      
      <!-- Button -->
      <ButtonWidget Command.Click="ExecuteSelectItem">
        <TextWidget Text="Select" />
      </ButtonWidget>
      
    </Widget>
  </Window>
</Prefab>
```

### Behavior (Screen Controller)

```csharp
public class MyScreenBehavior : CampaignBehaviorBase
{
    private GauntletLayer _layer;
    private MyScreenVM _viewModel;

    public void OpenScreen()
    {
        _viewModel = new MyScreenVM();
        _layer = new GauntletLayer(100);
        _layer.LoadMovie("MyScreen", _viewModel);
        
        ScreenManager.TopScreen.AddLayer(_layer);
        _layer.InputRestrictions.SetInputRestrictions();
    }

    public void CloseScreen()
    {
        ScreenManager.TopScreen.RemoveLayer(_layer);
        _layer = null;
        _viewModel = null;
    }
}
```

---

## Data Binding

### Property Binding
```xml
<TextWidget Text="@PropertyName" />
```

### Collection Binding
```xml
<ListPanel DataSource="{CollectionProperty}">
  <ItemTemplate>
    <!-- Template for each item -->
  </ItemTemplate>
</ListPanel>
```

### Command Binding
```xml
<ButtonWidget Command.Click="ExecuteMethodName" />
```

### Visibility Binding
```xml
<Widget IsVisible="@IsVisibleProperty" />
```

---

## Files to Study

| File | Why |
|------|-----|
| `SandBoxGauntletUISubModule.cs` | Module initialization |
| `GauntletPartyScreen.cs` | Party screen pattern |
| `GauntletInventoryScreen.cs` | Inventory screen pattern |

---

## Common Issues

1. **Property not updating**: Ensure `OnPropertyChanged` is called
2. **List not refreshing**: Use `MBBindingList` methods, not direct assignment
3. **Movie not loading**: Check XML path and prefab name match
4. **Layer not showing**: Verify layer priority and input restrictions
5. **Binding errors**: Property names must match exactly (case-sensitive)
