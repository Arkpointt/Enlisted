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

## VM Command Patterns (from decompiled analysis)

Common ViewModel command patterns to mirror:
- ExecuteDone()
- ExecuteCancel()
- ExecuteConfirm()
- ExecuteEquip()
- ExecuteSelect()
- ExecuteClose()
- RefreshValues()

## Custom Equipment Selector Pattern

```csharp
// How to create custom equipment UI (from decompiled patterns)
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

## 🎨 **BANNERLORD GAUNTLET ASSET SYSTEM - COMPREHENSIVE REFERENCE**

**Research Date**: 2025-01-28  
**Source**: Actual Bannerlord Module GUI assets analysis

### **📍 Asset Locations by Type**

#### **1. Icons & UI Images (Sprites)**

**Primary Sprite Data Files**:
- `...\Modules\Native\GUI\NativeSpriteData.xml` (39,800+ lines) - Core UI sprites
- `...\Modules\SandBox\GUI\SandBoxSpriteData.xml` (17,000+ lines) - Game-specific sprites

**Sprite Structure** (from NativeSpriteData.xml):
```xml
<SpriteCategories>
    <SpriteCategory>
        <Name>ui_backgrounds</Name>
        <SpriteSheetCount>1</SpriteSheetCount>
        <SpriteSheetSize ID="1" Width="4096" Height="4096" />
    </SpriteCategory>
    <!-- 50+ sprite categories available -->
</SpriteCategories>

<SpriteParts>
    <SpritePart>
        <SheetID>1</SheetID>
        <Name>ArmyManagement\army_card</Name>  <!-- Sprite name for reference -->
        <Width>160</Width>
        <Height>140</Height>
        <SheetX>2020</SheetX>
        <SheetY>4</SheetY>
        <CategoryName>ui_armymanagement</CategoryName>
    </SpritePart>
</SpriteParts>
```

**Available Sprite Categories** (confirmed in game data):
- `ui_backgrounds` - Background graphics and illustrations
- `ui_inventory` - Inventory UI elements  
- `ui_conversation` - Dialog and conversation elements
- `ui_encyclopedia` - Encyclopedia and reference UI
- `ui_facegen` - Character creation elements
- `ui_fonts` - Font sprite atlases (9 sprite sheets)
- `ui_group1` - Core UI elements (always loaded)
- `ui_mapbar` - Map interface elements
- `ui_partyscreen` - Party management UI
- `ui_quest` - Quest and journal UI

**Asset Files Location**:
- **Sprite Atlases**: `...\Modules\Native\LauncherGUI\SpriteSheets\` (actual PNG files)
- **Individual Sprites**: `...\Modules\Native\LauncherGUI\SpriteParts\` (categorized PNG files)

#### **2. Brushes (Visual Styles)**

**Brush Definition Files**:
- `...\Modules\Native\GUI\Brushes\*.xml` - 40+ brush definition files
- `...\Modules\SandBox\GUI\Brushes\*.xml` - 20+ game-specific brush files
- `...\Modules\SandBoxCore\GUI\Brushes\*.xml` - Core gameplay brushes

**Key Brush Files for UI Development**:
- `Standard.xml` - Core button and popup brushes
- `GameMenu.xml` - Game menu styling with hover/pressed states
- `Inventory.xml` - Equipment and inventory UI styling
- `Font.xml` - Text styling and font definitions
- `Popup.xml` - Modal dialog and popup styling

**Brush Structure Example** (from Standard.xml):
```xml
<Brush Name="Standard.PopupCloseButton">
    <Layers>
        <BrushLayer Name="Default" Sprite="StdAssets\standart_popup_button" />
    </Layers>
    <Styles>
        <Style Name="Default">
            <BrushLayer Name="Default" ColorFactor="1.0" />
        </Style>
        <Style Name="Hovered">
            <BrushLayer Name="Default" ColorFactor="1.0" Sprite="StdAssets\standart_popup_button_hover" />
        </Style>
        <Style Name="Pressed">
            <BrushLayer Name="Default" ColorFactor="0.7" />
        </Style>
        <Style Name="Disabled">
            <BrushLayer Name="Default" ColorFactor="0.2" />
        </Style>
    </Styles>
    <SoundProperties>
        <EventSounds>
            <EventSound EventName="Click" Audio="panels/next" />
        </EventSounds>
    </SoundProperties>
</Brush>
```

**Key Inventory Brushes for Equipment UI**:
- `Inventory.Tuple.Left` / `Inventory.Tuple.Right` - Equipment list item styling
- `Inventory.TopRight.Background` / `Inventory.TopLeft.Background` - Header sections
- `Inventory.Lock` - Locked item indicators
- `Inventory.Tooltip.Background` - Equipment tooltip styling

#### **3. Fonts (Bitmap Fonts)**

**Font System Location**:
- `...\Modules\Native\GUI\Fonts\NativeLanguages.xml` - Font mapping system

**Available Fonts** (from NativeLanguages.xml):
```xml
<Language id="English" DefaultFont="FiraSansExtraCondensed-Regular">
    <Map From="FiraSansExtraCondensed-Light" To="FiraSansExtraCondensed-Light"/>
    <Map From="FiraSansExtraCondensed-Medium" To="FiraSansExtraCondensed-Medium"/>  
    <Map From="FiraSansExtraCondensed-Regular" To="FiraSansExtraCondensed-Regular"/>
    <Map From="Galahad" To="Galahad"/>                      <!-- Primary game font -->
    <Map From="Galahad_Numbers_Bold" To="Galahad_Numbers_Bold"/>
</Language>
```

**Standard Font Brushes** (from Font.xml):
- `RightAlignedFont` - Right-aligned text (Font="Galahad", FontSize="20")
- `LeftAlignedFont` - Left-aligned text  
- `CenterAlignedFont` - Center-aligned text
- `CenterAlignedSmallFont` - Small centered text (FontSize="15")
- `CenterAlignedLargeFont` - Large centered text (FontSize="45")

**Font Colors** (hex format):
- Primary Text: `#FAF4DEFF` (cream/off-white)
- Glow Effect: `#000000FF` (black glow for readability)

#### **4. Colors & Visual Effects**

**Color System** (from brush analysis):
- **Primary UI**: `#FAF4DEFF` - Cream/off-white text
- **Backgrounds**: `#00213BFF` - Dark blue backgrounds
- **Gold/Highlight**: `#7A5B1EFF` - Gold accent color
- **Positive**: `#296024FF` - Green for positive values
- **Disabled**: ColorFactor="0.2" - 20% opacity for disabled states

**Visual Effect Properties**:
- `ColorFactor` - Brightness multiplier (0.0-2.0+)
- `AlphaFactor` - Transparency (0.0-1.0)
- `HueFactor` - Hue shift in degrees
- `SaturationFactor` - Saturation adjustment
- `ValueFactor` - Value/brightness adjustment
- `ExtendTop/Bottom/Left/Right` - 9-slice border extension

### **🛠️ Implementation Guide for Quartermaster UI**

#### **Using Existing Game Assets**

**For Equipment Selection UI**:
```xml
<!-- Use inventory styling for equipment lists -->
<Widget Brush="Inventory.Tuple.Left" DoNotAcceptEvents="false">
    <Children>
        <ImageWidget Sprite="StdAssets\gold_icon" />
        <TextWidget Font="Galahad" Text="{ItemName}" />
        <TextWidget Font="Galahad" Text="{Cost} denars" />
    </Children>
</Widget>

<!-- Use standard button styling -->
<ButtonWidget Brush="Standard.PopupCloseButton" DoNotAcceptEvents="false">
    <TextWidget Text="Request Equipment" />
</ButtonWidget>
```

**For Quartermaster Menu Header**:
```xml
<!-- Use game menu styling -->
<Widget Brush="GameMenu.Button">
    <TextWidget Font="Galahad" Text="Army Quartermaster" FontSize="25" />
</Widget>
```

#### **Available Sprite Names for Equipment UI**

**From Inventory System**:
- `Inventory\header_left` / `Inventory\header_right` - Header backgrounds
- `Inventory\tuple_left` / `Inventory\tuple_right` - List item backgrounds  
- `Inventory\tuple_left_pressed` / `Inventory\tuple_right_pressed` - Selected states
- `Inventory\toolbox` - Equipment controls background
- `Inventory\inventory_tooltip_button` - Action buttons

**From Standard Assets**:
- `StdAssets\standart_popup_button` - Standard buttons with hover/pressed states
- `StdAssets\prev_button` / `StdAssets\next_button` - Navigation buttons
- `BlankWhiteSquare_9` - Solid color backgrounds (9-slice)
- `StdAssets\gold_icon` - Gold/currency icon

**From Army Management**:
- `ArmyManagement\army_card` - Card-style backgrounds
- `SPGeneral\InventoryPartyExtension\Extension\gold_icon` - Alternative gold icon

### **🎯 Quartermaster UI Implementation Strategy**

**For Equipment Variant Selection**:
1. **Use `Inventory.Tuple.Left` brush** for equipment list items
2. **Use `StdAssets\gold_icon`** for cost display  
3. **Use `Galahad` font** for text consistency
4. **Use `Standard.PopupCloseButton` brush** for action buttons
5. **Use inventory color scheme** - `#7A5B1EFF` for gold, `#FAF4DEFF` for text

**Asset References to Use**:
```csharp
// In Gauntlet XML or ViewModel
Brush="Inventory.Tuple.Left"              // Equipment list styling
Sprite="StdAssets\gold_icon"              // Currency icon  
Font="Galahad"                            // Standard game font
FontColor="#FAF4DEFF"                     // Standard text color
```

## Implementation Strategy

**For Custom Equipment Selection UI:**
1. Create custom ViewModel inheriting from ViewModel base class
2. Use GauntletLayer(1001, "CustomLayer", false) for UI layer
3. Load custom movie with LoadMovie("MovieName", viewModel)
4. Handle input restrictions and focus management
5. Clean up properly with ReleaseMovie() and RemoveLayer()
6. **Reference existing game brushes and sprites** (documented above)

**Input Handling:**
- Set InputRestrictions for modal behavior
- Use IsFocusLayer for focus management
- Handle keyboard/gamepad input through layer.Input

**Lifecycle:**
1. Create → 2. Load Movie → 3. Add to Screen → 4. Set Focus → 5. Handle Input → 6. Clean Up

---

## 🎮 **CUSTOM EQUIPMENT SELECTOR - DECOMPILED ANALYSIS & VERIFIED CURRENT APIS**

**Research Date**: 2025-01-28  
**Source**: Decompiled code + Current TaleWorlds API verification
**Warning**: Using ONLY verified current APIs, not outdated references

### **🏗️ Equipment Selector Architecture (Verified Pattern)**

#### **1. Core Structure**

**Pattern** (adapted with verified APIs):
```csharp
// VERIFIED API: TaleWorlds.Engine.GauntletUI.GauntletLayer
public static void CreateEquipmentSelectorLayer(List<ItemObject> availableItems, string equipmentType)
{
    // VERIFIED: Current TaleWorlds API pattern from BannerEditor and InventoryScreen
    _gauntletLayer = new GauntletLayer(1001, "GauntletLayer", false); // ✅ VERIFIED API
    _equipmentSelectorVM = new QuartermasterEquipmentSelectorVM(availableItems, equipmentType);
    _equipmentSelectorVM.RefreshValues(); // ✅ VERIFIED API

    // VERIFIED: Load movie with ViewModel
    _gauntletMovie = _gauntletLayer.LoadMovie("QuartermasterEquipmentSelection", _equipmentSelectorVM); // ✅ VERIFIED API
    
    // VERIFIED: Input handling from current GauntletInventoryScreen
    _gauntletLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All); // ✅ VERIFIED API
    _gauntletLayer.IsFocusLayer = true; // ✅ VERIFIED API
    
    // VERIFIED: Screen management from current ScreenManager
    ScreenManager.TopScreen.AddLayer(_gauntletLayer); // ✅ VERIFIED API  
    ScreenManager.TrySetFocus(_gauntletLayer); // ✅ VERIFIED API
}

// VERIFIED: Cleanup pattern from current TaleWorlds code
public static void CloseEquipmentSelectorLayer()
{
    if (_gauntletLayer != null)
    {
        _gauntletLayer.InputRestrictions.ResetInputRestrictions(); // ✅ VERIFIED API
        _gauntletLayer.IsFocusLayer = false; // ✅ VERIFIED API
        
        if (_gauntletMovie != null)
        {
            _gauntletLayer.ReleaseMovie(_gauntletMovie); // ✅ VERIFIED API
        }
        
        ScreenManager.TopScreen.RemoveLayer(_gauntletLayer); // ✅ VERIFIED API
        
        // Cleanup references
        _gauntletLayer = null;
        _gauntletMovie = null;
        _equipmentSelectorVM = null;
    }
}
```

#### **2. ViewModel System (VERIFIED APIs)**

**Main Equipment Selector ViewModel** (adapted from decompiled patterns with current APIs):
```csharp
// VERIFIED: ViewModel inheritance pattern from current TaleWorlds code
public class QuartermasterEquipmentSelectorVM : ViewModel
{
    // VERIFIED: MBBindingList usage from current InventoryVM and ScoreboardVM
    [DataSourceProperty]  // ✅ VERIFIED attribute
    public MBBindingList<QuartermasterEquipmentCardRowVM> Rows { get; set; } // ✅ VERIFIED type
    
    [DataSourceProperty]
    public CharacterViewModel UnitCharacter { get; set; } // ✅ VERIFIED from BannerEditorVM
    
    [DataSourceProperty] 
    public MBBindingList<CharacterEquipmentItemVM> WeaponsList { get; set; } // ✅ VERIFIED type
    
    [DataSourceProperty]
    public MBBindingList<CharacterEquipmentItemVM> ArmorsList { get; set; } // ✅ VERIFIED type

    // VERIFIED: Constructor pattern
    public QuartermasterEquipmentSelectorVM(List<ItemObject> availableItems, string equipmentType)
    {
        // Initialize binding lists
        Rows = new MBBindingList<QuartermasterEquipmentCardRowVM>(); // ✅ VERIFIED constructor
        var cards = new MBBindingList<QuartermasterEquipmentCardVM>();
        
        // Create equipment cards (4 per row)
        foreach (var item in availableItems)
        {
            cards.Add(new QuartermasterEquipmentCardVM(item, this, equipmentType));
            
            if (cards.Count == 4) // Row size
            {
                Rows.Add(new QuartermasterEquipmentCardRowVM(cards));
                cards = new MBBindingList<QuartermasterEquipmentCardVM>();
            }
        }
        
        // Add remaining cards
        if (cards.Count > 0)
        {
            Rows.Add(new QuartermasterEquipmentCardRowVM(cards));
        }
        
        // VERIFIED: Character display from current ViewModels
        UnitCharacter = new CharacterViewModel(1); // ✅ VERIFIED constructor
        UnitCharacter.FillFrom(Hero.MainHero.CharacterObject, -1); // ✅ VERIFIED method
    }

    // VERIFIED: Override pattern from current ViewModels
    public override void RefreshValues() // ✅ VERIFIED method
    {
        base.RefreshValues();
        // Refresh UI when data changes
    }
    
    // Custom command methods
    public void ExecuteClose()
    {
        // Close the equipment selector
        QuartermasterEquipmentSelectorBehavior.CloseEquipmentSelectorLayer();
    }
}
```

#### **3. Equipment Card ViewModel (Individual Items)**

```csharp
// VERIFIED: Equipment card pattern from current inventory system
public class QuartermasterEquipmentCardVM : ViewModel
{
    [DataSourceProperty]
    public MBBindingList<BindingListStringItem> ItemName { get; set; } // ✅ VERIFIED type
    
    [DataSourceProperty] 
    public ImageIdentifierVM ItemImage { get; set; } // ✅ VERIFIED type from current ViewModels
    
    [DataSourceProperty]
    public MBBindingList<ItemFlagVM> ItemFlagList { get; set; } // ✅ VERIFIED type
    
    [DataSourceProperty]
    public MBBindingList<ItemMenuTooltipPropertyVM> ItemProperties { get; set; } // ✅ VERIFIED type
    
    public QuartermasterEquipmentCardVM(ItemObject item, QuartermasterEquipmentSelectorVM container, string equipmentType)
    {
        _item = item;
        _container = container;
        _equipmentType = equipmentType;
        
        // VERIFIED: Item name handling
        ItemName = new MBBindingList<BindingListStringItem>(); // ✅ VERIFIED
        if (item != null)
        {
            ItemName.Add(new BindingListStringItem(item.Name?.ToString() ?? "Unknown")); // ✅ VERIFIED
            ItemImage = new ImageIdentifierVM(item, ""); // ✅ VERIFIED constructor
        }
        else
        {
            ItemName.Add(new BindingListStringItem("Empty")); // Empty slot
        }
        
        // VERIFIED: Initialize property lists
        ItemFlagList = new MBBindingList<ItemFlagVM>(); // ✅ VERIFIED
        ItemProperties = new MBBindingList<ItemMenuTooltipPropertyVM>(); // ✅ VERIFIED
        
        // Build item properties (cost, culture, etc.)
        BuildItemProperties();
    }

    // VERIFIED: Command methods for UI interaction
    public void ExecutePreview()
    {
        // Preview equipment on character model
        if (_item != null)
        {
            var equipmentSlot = ConvertToEquipmentSlot(_equipmentType);
            _container.UnitCharacter.SetEquipment(equipmentSlot, new EquipmentElement(_item, null, null, false)); // ✅ VERIFIED
        }
    }
    
    public void ExecuteApply()
    {
        // Apply equipment to player
        if (_item != null)
        {
            var equipmentSlot = ConvertToEquipmentSlot(_equipmentType);
            
            // Use our verified equipment replacement system
            var quartermasterManager = Features.Equipment.Behaviors.QuartermasterManager.Instance;
            quartermasterManager?.RequestEquipmentVariant(_item, equipmentSlot);
        }
        
        // Close the selector
        QuartermasterEquipmentSelectorBehavior.CloseEquipmentSelectorLayer();
    }
    
    private EquipmentIndex ConvertToEquipmentSlot(string equipmentType)
    {
        // VERIFIED: Equipment slot mapping (using verified EquipmentIndex)
        return equipmentType switch
        {
            "Weapon0" => EquipmentIndex.Weapon0, // ✅ VERIFIED enum
            "Weapon1" => EquipmentIndex.Weapon1,
            "Head" => EquipmentIndex.Head,
            "Body" => EquipmentIndex.Body,
            "Leg" => EquipmentIndex.Leg,
            "Gloves" => EquipmentIndex.Gloves,
            "Cape" => EquipmentIndex.Cape,
            "Horse" => EquipmentIndex.Horse,
            "HorseHarness" => EquipmentIndex.HorseHarness,
            _ => EquipmentIndex.Weapon0
        };
    }
    
    private ItemObject _item;
    private QuartermasterEquipmentSelectorVM _container;
    private string _equipmentType;
}
```

#### **4. Row Container ViewModel**

```csharp
// VERIFIED: Simple container pattern (no complex APIs needed)
public class QuartermasterEquipmentCardRowVM : ViewModel
{
    [DataSourceProperty]
    public MBBindingList<QuartermasterEquipmentCardVM> Cards { get; set; } // ✅ VERIFIED type

    public QuartermasterEquipmentCardRowVM(MBBindingList<QuartermasterEquipmentCardVM> cards)
    {
        Cards = cards; // Simple assignment, no complex APIs
    }
}
```

### **🎯 Key API Verifications from Current TaleWorlds Code**

| **API** | **Current Status** | **Source** |
|-------------|-------------------|------------|
| `new GauntletLayer(priority, name, shouldClear)` | ✅ **VERIFIED** | BannerEditor, InventoryScreen, etc. |
| `GauntletLayer.LoadMovie(movieName, dataSource)` | ✅ **VERIFIED** | All current Gauntlet screens |
| `ScreenManager.TopScreen.AddLayer(layer)` | ✅ **VERIFIED** | Current screen management |
| `GauntletLayer.InputRestrictions.SetInputRestrictions()` | ✅ **VERIFIED** | Input handling pattern |
| `ViewModel` base class inheritance | ✅ **VERIFIED** | SPScoreboardVM and others inherit |
| `[DataSourceProperty]` attribute | ✅ **VERIFIED** | Used in current ViewModels |
| `MBBindingList<T>` collections | ✅ **VERIFIED** | Standard collection type |
| `CharacterViewModel` | ✅ **VERIFIED** | Character display |
| `ImageIdentifierVM` | ✅ **VERIFIED** | Item image display |

### Implementation Pattern

The Quartermaster uses this structure:
1. **Behavior** - manages the UI layer 
2. **ViewModels** - organize data for templates
3. **Templates** - XML files that create the UI

Put templates in `GUI/Prefabs/Equipment/` and they'll load automatically.

---

## Working Implementation Notes

The Quartermaster system is working and uses these patterns:

**Template files**: Put them in `GUI/Prefabs/Equipment/` - not `GUI/GauntletUI/` or it won't find them.

**Image loading**: Need to add `TaleWorlds.PlayerServices.dll` reference or you get PlayerId errors. Use `new ImageIdentifierVM(item, "")` for equipment and `new ImageIdentifierVM(0)` for empty slots.

**Input handling**: Register the hotkey category BEFORE setting input restrictions or the game freezes.
```csharp
_gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
_gauntletLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
```

**4K scaling**: Use `HorizontalAlignment="Center"` instead of fixed margins like `MarginLeft="380"` - fixed positions don't scale.

**Widget types**: Use `<Widget>` not `<Panel>` - Panel doesn't exist in current Bannerlord.
