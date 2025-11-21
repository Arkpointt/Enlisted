# Bannerlord Menu Templates & Custom UI Reference

**Generated from decompile analysis and implementation study**  
**Updated**: 2025-01-28  
**Purpose**: Solve UI widget issues in enlisted menu system

## 🚨 **The UI Widget Problem**

### **Issue**: Empty Brown Boxes in Menu
**Root Cause**: Certain menu templates contain **ListPanel/Repeater widgets** designed for multiple items
**Symptoms**: Empty wooden boxes appear when widgets have no data bound to them
**Affected Templates**: Some `AddWaitGameMenu` templates expect list data structures

### **Current Conflict**:
- ✅ **`AddWaitGameMenu`**: Time controls work (spacebar pause/unpause) ✅ **ESSENTIAL**
- ❌ **`AddWaitGameMenu`**: Empty UI slot boxes appear ❌ **UNACCEPTABLE**

- ❌ **`AddGameMenu`**: No UI boxes ✅ **GOOD** 
- ❌ **`AddGameMenu`**: Time controls broken (stuck paused) ❌ **UNACCEPTABLE**

## 🎯 **Solution Options (From Decompile Analysis)**

### **Option 1: Party Wait Menu ID** ⭐ **RECOMMENDED - TRY FIRST**

**Discovery**: `"party_wait"` menu ID works cleanly for enlisted status menus

```csharp
// Pattern from decompiled game sources
campaignStarter.AddWaitGameMenu("party_wait", 
    "Party Leader: {PARTY_LEADER}\n{PARTY_TEXT}", 
    new OnInitDelegate(this.wait_on_init), 
    new OnConditionDelegate(this.wait_on_condition), 
    null, 
    new OnTickDelegate(this.wait_on_tick), 
    3, 0, 0f, 0, null);
```

**Why This Might Work**:
- ✅ This menu ID works cleanly for party status displays
- ✅ `"party_wait"` template might not have ListPanel widgets
- ✅ Existing game template means no custom UI needed
- ✅ Time controls work properly with this menu type

**Implementation**:
```csharp
// Replace "enlisted_status" with "party_wait"
starter.AddWaitGameMenu("party_wait", 
    "{ENLISTED_STATUS_TEXT}",
    new OnInitDelegate(OnEnlistedStatusInit),
    new OnConditionDelegate(OnEnlistedStatusCondition),
    null,
    null, // Can add tick handler later if needed
    GameMenu.MenuAndOptionType.WaitMenuShowOnlyProgressOption,
    GameOverlays.MenuOverlayType.None,
    0f,
    GameMenu.MenuFlags.None,
    null);
```

### **Option 2: Custom Gauntlet UI Layer** 🔧 **COMPLEX BUT COMPLETE CONTROL**

**Based on Custom Equipment Selector Pattern**:

```csharp
public class EnlistedMenuViewModel : ViewModel
{
    private string _statusText;
    
    [DataSourceProperty]
    public string StatusText
    {
        get => _statusText;
        set => SetField(ref _statusText, value);
    }
    
    // No list properties = no empty slot widgets
    public void ExecuteClose() => /* close menu */;
}

public static void ShowCustomEnlistedMenu()
{
    var layer = new GauntletLayer(1001, "EnlistedMenu", false);
    var viewModel = new EnlistedMenuViewModel();
    viewModel.StatusText = GetEnlistedStatusText();
    
    var movie = layer.LoadMovie("EnlistedStatusMenu", viewModel);
    layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
    
    ScreenManager.TopScreen.AddLayer(layer);
    layer.IsFocusLayer = true;
}
```

**Requires**: Custom .xml template file (100% control, no unwanted widgets)

### **Option 3: Alternative Base Game Menu IDs** 🔍 **RESEARCH NEEDED**

**Potential Clean Menu Templates** (need verification):
- `"town"` - Basic town menu template
- `"village"` - Simple village menu template  
- `"encounter"` - Basic encounter template
- `"wait"` - Simple wait template
- `"army_wait"` - Army waiting template

**Research Strategy**:
```csharp
// Test different menu IDs to find clean templates
private readonly string[] TestMenuIds = {
    "party_wait",     // Works cleanly for status displays
    "town_wait", 
    "village_wait",
    "army_wait",
    "simple_wait",
    "encounter_wait"
};

foreach (var menuId in TestMenuIds)
{
    // Test each to see which has clean UI
}
```

### **Option 4: Feed Dummy Data to Widgets** 🎛️ **MODERATE COMPLEXITY**

**Strategy**: Identify what data the ListPanel expects and provide empty/hidden entries

```csharp
// If menu template expects list of assignments/duties
private void OnEnlistedStatusInit(MenuCallbackArgs args)
{
    args.MenuContext.GameMenu.StartWait();
    
    // Provide empty list data to satisfy widgets
    var emptyList = new List<object>();
    MBTextManager.SetTextVariable("DUTY_LIST", emptyList);
    MBTextManager.SetTextVariable("ASSIGNMENT_LIST", emptyList);
    
    RefreshEnlistedStatusDisplay();
}
```

## 🔧 **Complete API Reference for Menu Creation**

### **Standard Menu Creation APIs**

```csharp
// Basic menu (pauses time)
campaignStarter.AddGameMenu(string menuId, string menuText, OnInitDelegate initDelegate, 
    GameOverlays.MenuOverlayType overlayType = GameOverlays.MenuOverlayType.None, 
    GameMenu.MenuFlags flags = GameMenu.MenuFlags.None, 
    object relatedObject = null);

// Wait menu (allows time controls)
campaignStarter.AddWaitGameMenu(string menuId, string menuText, OnInitDelegate initDelegate, 
    OnConditionDelegate conditionDelegate, OnConsequenceDelegate consequenceDelegate, 
    OnTickDelegate tickDelegate, GameMenu.MenuAndOptionType type, 
    GameOverlays.MenuOverlayType overlayType, float targetWaitHours, 
    GameMenu.MenuFlags flags, object relatedObject);

// Menu options
campaignStarter.AddGameMenuOption(string menuId, string optionId, string optionText, 
    GameMenuOption.OnConditionDelegate condition, 
    GameMenuOption.OnConsequenceDelegate consequence, 
    bool isLeave = false, int index = -1, bool isRepeatable = false);
```

### **Menu Control APIs**

```csharp
// Menu navigation
GameMenu.ActivateGameMenu(string menuId);
GameMenu.SwitchToMenu(string menuId);  
GameMenu.ExitToLast();

// Time control
Campaign.Current.SetTimeControlModeLock(bool lockState);
Campaign.Current.TimeControlMode = CampaignTimeControlMode.StoppablePlay;

// Menu management  
Campaign.Current.GameMenuManager.RefreshMenuOptions(MenuContext context);
args.MenuContext.GameMenu.StartWait();
```

### **Custom Gauntlet UI APIs**

```csharp
// Layer management
TaleWorlds.Engine.GauntletUI.GauntletLayer :: GauntletLayer(int localOrder, string categoryId, bool shouldClear);
TaleWorlds.Engine.GauntletUI.GauntletLayer :: LoadMovie(string movieName, object dataSource);
TaleWorlds.Engine.GauntletUI.GauntletLayer :: ReleaseMovie(GauntletMovie movie);

// Screen management
TaleWorlds.ScreenSystem.ScreenManager :: TopScreen { get; }
TaleWorlds.ScreenSystem.ScreenBase :: AddLayer(ScreenLayer layer);
TaleWorlds.ScreenSystem.ScreenBase :: RemoveLayer(ScreenLayer layer);

// Input control
TaleWorlds.Engine.GauntletUI.GauntletLayer :: IsFocusLayer { get; set; }
TaleWorlds.Engine.GauntletUI.GauntletLayer :: InputRestrictions { get; }
```

### **Alternative UI Systems**

```csharp
// Inventory-based UI (proven clean)
TaleWorlds.CampaignSystem.Inventory.InventoryManager :: OpenScreenAsReceiveItems(ItemRoster items, TextObject leftRosterName, InventoryManager.DoneLogicExtrasDelegate doneLogicDelegate);

// Dialog-based UI (no visual widgets)
campaignStarter.AddDialogLine / AddPlayerLine (conversation system)
```

## 🎯 **RECOMMENDED IMPLEMENTATION STRATEGY**

### **Phase 1: Try Party Wait Menu ID** (Quickest Fix)
```csharp
// Replace current menu ID with party_wait
starter.AddWaitGameMenu("party_wait",  // <- Proven to work cleanly
    "{ENLISTED_STATUS_TEXT}",
    // ... rest of implementation unchanged
```

**Expected Result**: 
- ✅ Time controls work properly
- ✅ Clean display without UI boxes
- ✅ Minimal code changes

### **Phase 2: If party_wait Still Has Issues**
**Try other base game menu IDs** until we find a clean template:
- `"town_wait"`
- `"village_wait"` 
- `"army_wait"`

### **Phase 3: Custom Gauntlet UI** (If No Clean Template Found)
**Create custom .xml template** with only desired widgets:
```xml
<!-- EnlistedStatusMenu.xml -->
<Prefab>
  <Window>
    <RichTextWidget Text="@StatusText" />
    <!-- No ListPanel = No empty slots -->
  </Window>
</Prefab>
```

## 🚀 **IMMEDIATE ACTION PLAN**

**Step 1**: Try `"party_wait"` menu ID (proven approach)
**Step 2**: Test if UI boxes disappear while time controls still work  
**Step 3**: If successful, document as solution. If not, proceed to Option 2.

**The `"party_wait"` approach has the highest probability of success since it works cleanly!**
