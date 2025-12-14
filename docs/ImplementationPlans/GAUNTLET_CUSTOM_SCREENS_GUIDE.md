# Gauntlet Custom Screen Development Guide

**Critical lessons learned from implementing the Lance Life Event screen system.**

---

## Table of Contents

1. [Critical Issue: Color Format Requirements](#critical-issue-color-format-requirements)
2. [Best Practices for Custom Gauntlet Screens](#best-practices-for-custom-gauntlet-screens)
3. [Common Pitfalls and Solutions](#common-pitfalls-and-solutions)
4. [Implementation Reference](#implementation-reference)
5. [Pre-Deployment Checklist](#pre-deployment-checklist)

---

## Critical Issue: Color Format Requirements

### ⚠️ THE PROBLEM

**Mount & Blade II's `TaleWorlds.Library.Color.ConvertStringToColor()` requires 8-digit hex color strings (`#RRGGBBAA`).**

Using 6-digit hex colors (e.g., `#4488FF`) will cause:
```
ArgumentOutOfRangeException: Index and length must refer to a location within the string. 
Parameter name: length
```

This crash occurs during `GauntletLayer.LoadMovie()` and can be **extremely difficult to debug** because:
- The error message doesn't mention colors
- The crash happens during XML parsing, not at property assignment
- ViewModel initialization may complete successfully before the crash
- Standard debugging shows all properties as valid

### Why This Happens

The native `Color.ConvertStringToColor()` function **always** expects exactly 9 characters (`#` + 8 hex digits) with **no validation or fallback**:

```csharp
// Decompiled from TaleWorlds.Library.Color
public static Color ConvertStringToColor(string colorString)
{
    byte r = Convert.ToByte(colorString.Substring(1, 2), 16);  // Characters 1-2
    byte g = Convert.ToByte(colorString.Substring(3, 2), 16);  // Characters 3-4
    byte b = Convert.ToByte(colorString.Substring(5, 2), 16);  // Characters 5-6
    byte a = Convert.ToByte(colorString.Substring(7, 2), 16);  // ⚠️ CRASHES if string is only 7 chars!
    return new Color(r, g, b, a);
}
```

The function attempts `Substring(7, 2)` to extract the alpha channel, which throws `ArgumentOutOfRangeException` on 6-digit color strings.

### ✅ Solution: Always Use 8-Digit Colors

**CORRECT:**
```csharp
CategoryColor = "#4488FFFF";  // Full opacity (FF = 255)
TextColor = "#888888FF";      // Full opacity
RiskColor = "#DD3333FF";      // Full opacity
TransparentColor = "#00000080";  // 50% opacity (80 = 128)
```

**WRONG (WILL CRASH):**
```csharp
CategoryColor = "#4488FF";    // Missing alpha - CRASH!
TextColor = "#888888";        // Missing alpha - CRASH!
```

### Where Colors Must Be 8-Digit

#### 1. C# ViewModels

All color properties in ViewModels must return 8-digit hex strings:

```csharp
// LanceLifeEventVM.cs
private string GetCategoryColor(string category)
{
    return category?.ToLowerInvariant() switch
    {
        "combat" => "#DD0000FF",      // Red with full alpha
        "social" => "#44AA44FF",      // Green with full alpha
        "duty" => "#4488FFFF",        // Blue with full alpha
        "training" => "#FFAA33FF",    // Orange with full alpha
        "onboarding" => "#4488FFFF",  // Blue with full alpha
        _ => "#FFFFFFFF"              // White with full alpha (default)
    };
}

// EventChoiceVM.cs
public string TextColor
{
    get => _textColor;
    set
    {
        if (_textColor != value)
        {
            _textColor = value;
            OnPropertyChangedWithValue(value, nameof(TextColor));
        }
    }
}

// In initialization:
TextColor = IsEnabled ? "#FFFFFFFF" : "#888888FF";  // Always 8-digit!
```

#### 2. XML UI Files

All hardcoded color values in XML attributes must be 8-digit:

```xml
<!-- LanceLifeEventScreen.xml -->
<RichTextWidget 
    Brush.TextColor="#FFEECCFF"    <!-- ✅ 8-digit -->
    Text="@EventTitle" />

<Widget 
    Color="#FF6644FF"              <!-- ✅ 8-digit -->
    Sprite="StdAssets\progressbar_fill" />

<!-- WRONG - Will crash! -->
<RichTextWidget 
    Brush.TextColor="#FFEECC"      <!-- ❌ 6-digit - CRASH! -->
    Text="@EventTitle" />
```

#### 3. All Color-Related XML Attributes

These attributes require 8-digit colors:
- `Color="#RRGGBBAAFF"`
- `Brush.TextColor="#RRGGBBAAFF"`
- `Brush.Color="#RRGGBBAAFF"`
- `Sprite.Color="#RRGGBBAAFF"`
- Any custom widget property that binds to a color

### Quick Fix for Existing Code

Search your entire codebase for 6-digit colors and append "FF":

**Regex search:** `#[0-9A-Fa-f]{6}(?![0-9A-Fa-f])`

Convert each match:
- `#4488FF` → `#4488FFFF`
- `#FFFFFF` → `#FFFFFFFF`
- `#DD3333` → `#DD3333FF`

---

## Best Practices for Custom Gauntlet Screens

### 1. Defer Screen Pushing to Next Frame

**Always** defer `ScreenManager.PushScreen()` calls to avoid crashes during native visual updates:

```csharp
public static void Open(LanceLifeEventDefinition eventDef, EnlistmentBehavior enlistment, Action onClosed = null)
{
    // ✅ CORRECT: Defer to next frame
    NextFrameDispatcher.RunNextFrame(() =>
    {
        if (Campaign.Current == null)
        {
            onClosed?.Invoke();
            return;
        }
        
        var screen = new LanceLifeEventScreen(eventDef, enlistment, onClosed);
        ScreenManager.PushScreen(screen);
    });
}

// ❌ WRONG: Immediate push can crash
public static void Open(LanceLifeEventDefinition eventDef)
{
    var screen = new LanceLifeEventScreen(eventDef);
    ScreenManager.PushScreen(screen);  // May crash during NavalMobilePartyVisual.UpdateEntityPosition!
}
```

**Why:** Native game systems (e.g., `NavalMobilePartyVisual.UpdateEntityPosition`, party movement updates) can crash if screens are pushed mid-tick. The game's visual update systems don't expect the screen stack to change during their execution.

### 2. Validate Campaign State in Deferred Actions

Always check that `Campaign.Current` is still valid when deferred actions execute:

```csharp
NextFrameDispatcher.RunNextFrame(() =>
{
    // ✅ Campaign might have ended between queuing and execution
    if (Campaign.Current == null)
    {
        ModLogger.Warn("UI", "Cannot display screen - campaign session ended");
        onClosed?.Invoke();
        return;
    }
    
    // Safe to proceed
    var screen = new MyCustomScreen();
    ScreenManager.PushScreen(screen);
});
```

**Why:** The player might exit to main menu or load a save between action queuing and execution. Always validate runtime state.

### 3. Widget Property Binding

**Gauntlet binds ALL widget properties during `LoadMovie()`, regardless of `IsVisible` status.**

This is a critical behavior that's not obvious:

❌ **WRONG (causes binding errors):**
```xml
<!-- CharacterTableauWidget will try to bind BodyProperties during LoadMovie,
     even though IsVisible=false! -->
<CharacterTableauWidget 
    IsVisible="@ShowCharacter"
    BodyProperties="@BodyProperties"    <!-- Binds IMMEDIATELY, crashes if invalid -->
    EquipmentCode="@EquipmentCode" />   <!-- Binds IMMEDIATELY, crashes if invalid -->
```

**The Problem:** Gauntlet's XML parser reads and binds ALL properties during `LoadMovie()`, before checking visibility. If `BodyProperties` contains invalid data (empty string, "null" literal, or malformed XML), it will crash even though `IsVisible="false"`.

✅ **SOLUTION OPTIONS:**

**Option A: Remove the widget entirely if not needed (RECOMMENDED)**
```xml
<!-- Simply don't include widgets you're not using -->
<ImageWidget Sprite="@SceneImagePath" />
<!-- CharacterTableauWidget removed entirely -->
```

**Option B: Always provide valid data in ViewModel**
```csharp
// Even if not showing character, initialize to valid values
if (ShowCharacter)
{
    BodyProperties = character.BodyProperties.ToString();  // Valid body properties
    EquipmentCode = character.Equipment.ToString();
    CharStringId = character.StringId;
}
else
{
    // Must still provide valid data for binding, even though not visible
    BodyProperties = Hero.MainHero.BodyProperties.ToString();
    EquipmentCode = "default_equipment";
    CharStringId = "player";
}
```

**Note:** Option A is preferred for the Lance Life Event screen since we currently don't need character display. The widget was removed to avoid binding issues entirely.

### 4. String Safety in ViewModels

Always use null-conditional operators and provide fallbacks:

```csharp
// ✅ CORRECT: Safe string handling
EventTitle = _event.TitleFallback ?? "Lance Activity";
CategoryText = FormatCategory(_event.Category ?? "general");
TimeLocationText = GetTimeLocationText() ?? "Unknown";

// Substring safety
var preview = text?.Length > 50 
    ? text.Substring(0, Math.Min(50, text.Length)) 
    : text ?? "";

// Safe length checks
var textLength = text?.Length ?? 0;
if (textLength > 0)
{
    // Safe to use text
}
```

**Common String Issues:**
- `text.Substring(0, 50)` crashes if `text` is null or shorter than 50 characters
- `text.Contains("...")` crashes if `text` is null
- `text.ToLowerInvariant()` crashes if `text` is null

**Use `?.` operator everywhere!**

### 5. Screen Cleanup

Always implement comprehensive cleanup in `OnFinalize()`:

```csharp
protected override void OnFinalize()
{
    base.OnFinalize();
    
    // Clean up ViewModel (important for event handlers)
    if (_dataSource != null)
    {
        _dataSource.OnFinalize();
        _dataSource = null;
    }
    
    // Release movie (releases UI resources)
    if (_gauntletMovie != null)
    {
        _gauntletLayer?.ReleaseMovie(_gauntletMovie);
        _gauntletMovie = null;
    }
    
    // Remove and dispose layer
    if (_gauntletLayer != null)
    {
        RemoveLayer(_gauntletLayer);
        _gauntletLayer = null;
    }
}

// Also implement a manual close method for user-initiated closes
private void CloseScreen()
{
    if (_closing) return;
    _closing = true;

    // Clean up
    if (_gauntletMovie != null)
    {
        _gauntletLayer?.ReleaseMovie(_gauntletMovie);
        _gauntletMovie = null;
    }

    if (_gauntletLayer != null)
    {
        RemoveLayer(_gauntletLayer);
        _gauntletLayer = null;
    }

    _dataSource?.OnFinalize();
    _dataSource = null;

    // Pop screen and notify
    ScreenManager.PopScreen();
    _onClosed?.Invoke();
}
```

### 6. Input Handling and Game Pause

Set up input restrictions properly to pause game and capture input:

```csharp
protected override void OnInitialize()
{
    base.OnInitialize();
    
    // ... create ViewModel, layer, movie ...
    
    // Pause game time when screen is open
    if (Campaign.Current != null)
    {
        Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
    }
    
    // Capture all input (modal screen)
    _gauntletLayer.InputRestrictions.SetInputRestrictions(
        isMouseVisible: true,
        InputUsageMask.All);
}

// Handle ESC key to close (if allowed)
protected override void OnFrameTick(float dt)
{
    base.OnFrameTick(dt);
    
    if (_gauntletLayer.Input.IsKeyReleased(InputKey.Escape) 
        && _dataSource.CanClose   // Respect forced choice events
        && !_closing)
    {
        CloseScreen();
    }
}
```

### 7. Exception Handling in Initialization

Always wrap `OnInitialize()` in try-catch with proper cleanup:

```csharp
protected override void OnInitialize()
{
    base.OnInitialize();

    try
    {
        // Create ViewModel
        _dataSource = new LanceLifeEventVM(_event, _enlistment, CloseScreen);

        // Create Gauntlet layer and load UI
        _gauntletLayer = new GauntletLayer("GauntletLayer", 200);
        _gauntletMovie = _gauntletLayer.LoadMovie("LanceLifeEventScreen", _dataSource);

        if (_gauntletMovie == null)
        {
            throw new Exception("Failed to load event UI - XML file may be missing or invalid");
        }

        // ... rest of initialization ...
    }
    catch (Exception ex)
    {
        ModLogger.Error("LanceLifeUI", $"Failed to display lance event: {ex.Message}", ex);
        
        // Clean up partial initialization
        if (_gauntletMovie != null && _gauntletLayer != null)
        {
            _gauntletLayer.ReleaseMovie(_gauntletMovie);
        }
        _gauntletMovie = null;
        _gauntletLayer = null;
        _dataSource = null;

        // Notify caller and close
        _onClosed?.Invoke();
        
        // Pop broken screen
        NextFrameDispatcher.RunNextFrame(() =>
        {
            try { ScreenManager.PopScreen(); } catch { }
        });

        throw; // Re-throw so screen manager knows initialization failed
    }
}
```

---

## Common Pitfalls and Solutions

### ❌ Pitfall 1: Using 6-Digit Hex Colors

**Symptom:** 
```
ArgumentOutOfRangeException: Index and length must refer to a location within the string.
Parameter name: length
```
Occurs during `LoadMovie()`, often with no clear indication it's color-related.

**Solution:** Always use 8-digit colors (`#RRGGBBAAFF`)

**How to Find:**
1. Search for `Color=` or `Brush.TextColor=` in XML files
2. Search for color return statements in C# ViewModels
3. Use regex: `#[0-9A-Fa-f]{6}(?![0-9A-Fa-f])`

---

### ❌ Pitfall 2: Pushing Screen Immediately

**Symptom:** Random crashes during:
- `NavalMobilePartyVisual.UpdateEntityPosition`
- Party movement updates
- Other native visual systems

**Solution:** Always defer screen push:
```csharp
NextFrameDispatcher.RunNextFrame(() => {
    ScreenManager.PushScreen(screen);
});
```

---

### ❌ Pitfall 3: Including Unused Widgets with Invalid Data

**Symptom:** Binding errors or crashes during `LoadMovie()` even with `IsVisible=false`

**Example:**
```xml
<!-- This WILL crash if BodyProperties is invalid, even though IsVisible=false -->
<CharacterTableauWidget 
    IsVisible="false"
    BodyProperties="@BodyProperties" />
```

**Solution:** 
- **Option A (Preferred):** Remove unused widgets entirely
- **Option B:** Always provide valid data in ViewModel, even for hidden widgets

---

### ❌ Pitfall 4: Not Validating Campaign State

**Symptom:** 
```
NullReferenceException: Object reference not set to an instance of an object
```
Occurs when accessing `Campaign.Current` after player exits campaign.

**Solution:**
```csharp
NextFrameDispatcher.RunNextFrame(() =>
{
    if (Campaign.Current == null)
    {
        onClosed?.Invoke();
        return;
    }
    // Safe to proceed
});
```

---

### ❌ Pitfall 5: Incomplete Cleanup

**Symptom:** 
- Memory leaks
- Ghost UI layers persisting after close
- Crashes when reopening screens
- Input still captured after screen closes

**Solution:** Implement comprehensive cleanup in both `OnFinalize()` and `CloseScreen()`:
```csharp
// Release movie
_gauntletLayer?.ReleaseMovie(_gauntletMovie);
// Remove layer
RemoveLayer(_gauntletLayer);
// Finalize ViewModel
_dataSource?.OnFinalize();
// Null everything
_gauntletMovie = null;
_gauntletLayer = null;
_dataSource = null;
```

---

## Implementation Reference

### XML Prefab Structure

**Minimum required structure:**

```xml
<Prefab>
    <Window>
        <Widget WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent">
            <Children>
                <!-- Your UI content here -->
            </Children>
        </Widget>
    </Window>
</Prefab>
```

**Full example with backdrop and centered card:**

```xml
<Prefab>
    <Window>
        <!-- Full screen semi-transparent overlay -->
        <Widget WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" 
                Sprite="BlankWhiteSquare_9" Color="#00000099">
            <Children>
                <!-- Centered card -->
                <BrushWidget WidthSizePolicy="Fixed" HeightSizePolicy="Fixed" 
                           SuggestedWidth="1100" SuggestedHeight="750" 
                           HorizontalAlignment="Center" VerticalAlignment="Center" 
                           Brush="Encyclopedia.Frame">
                    <Children>
                        <!-- Card content -->
                        <RichTextWidget Text="@EventTitle" 
                                      Brush.TextColor="#FFEECCFF" />
                    </Children>
                </BrushWidget>
            </Children>
        </Widget>
    </Window>
</Prefab>
```

### File Deployment

Gauntlet XML files must be copied to the game's module directory during build:

```xml
<!-- Enlisted.csproj -->
<Target Name="AfterBuild">
    <Copy 
        SourceFiles="GUI\Prefabs\Events\LanceLifeEventScreen.xml" 
        DestinationFiles="$(GameModulePath)\GUI\Prefabs\Events\LanceLifeEventScreen.xml" 
        SkipUnchangedFiles="true" />
    <Copy 
        SourceFiles="GUI\Prefabs\Events\EventChoiceButton.xml" 
        DestinationFiles="$(GameModulePath)\GUI\Prefabs\Events\EventChoiceButton.xml" 
        SkipUnchangedFiles="true" />
</Target>
```

**Verify deployment location:**
```
<Game Directory>\Modules\Enlisted\GUI\Prefabs\Events\LanceLifeEventScreen.xml
```

### ViewModel Requirements

**Base class:**

```csharp
using TaleWorlds.Library;

public class LanceLifeEventVM : ViewModel
{
    private string _eventTitle;
    
    [DataSourceProperty]
    public string EventTitle
    {
        get => _eventTitle;
        set
        {
            if (_eventTitle != value)
            {
                _eventTitle = value;
                OnPropertyChangedWithValue(value, nameof(EventTitle));
            }
        }
    }
    
    // For collections
    private MBBindingList<EventChoiceVM> _choiceOptions;
    
    [DataSourceProperty]
    public MBBindingList<EventChoiceVM> ChoiceOptions
    {
        get => _choiceOptions;
        set
        {
            if (_choiceOptions != value)
            {
                _choiceOptions = value;
                OnPropertyChanged(nameof(ChoiceOptions));
            }
        }
    }
    
    // Cleanup
    public override void OnFinalize()
    {
        base.OnFinalize();
        // Clean up event handlers, subscriptions, etc.
    }
}
```

**Property change notifications:**

```csharp
// For value types and strings
OnPropertyChangedWithValue(value, nameof(PropertyName));

// For collections and reference types
OnPropertyChanged(nameof(PropertyName));
```

### Screen Implementation Template

```csharp
using System;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Engine.Screens;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.InputSystem;
using TaleWorlds.ScreenSystem;

public class CustomScreen : ScreenBase
{
    private readonly Action _onClosed;
    private GauntletLayer _gauntletLayer;
    private GauntletMovieIdentifier _gauntletMovie;
    private CustomScreenVM _dataSource;
    private bool _closing;

    public CustomScreen(Action onClosed = null)
    {
        _onClosed = onClosed;
    }

    protected override void OnInitialize()
    {
        base.OnInitialize();

        try
        {
            // Create ViewModel
            _dataSource = new CustomScreenVM(CloseScreen);

            // Create Gauntlet layer and load UI
            _gauntletLayer = new GauntletLayer("GauntletLayer", 200);
            _gauntletMovie = _gauntletLayer.LoadMovie("CustomScreen", _dataSource);

            if (_gauntletMovie == null)
            {
                throw new Exception("Failed to load UI - XML may be missing");
            }

            // Add layer
            AddLayer(_gauntletLayer);

            // Set input restrictions
            _gauntletLayer.InputRestrictions.SetInputRestrictions(
                isMouseVisible: true,
                InputUsageMask.All);

            // Pause game if needed
            if (Campaign.Current != null)
            {
                Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
            }
        }
        catch (Exception ex)
        {
            // Clean up and close
            if (_gauntletMovie != null && _gauntletLayer != null)
            {
                _gauntletLayer.ReleaseMovie(_gauntletMovie);
            }
            _gauntletMovie = null;
            _gauntletLayer = null;
            _dataSource = null;
            _onClosed?.Invoke();
            
            NextFrameDispatcher.RunNextFrame(() =>
            {
                try { ScreenManager.PopScreen(); } catch { }
            });

            throw;
        }
    }

    protected override void OnFrameTick(float dt)
    {
        base.OnFrameTick(dt);

        // Handle ESC key
        if (_gauntletLayer.Input.IsKeyReleased(InputKey.Escape) && !_closing)
        {
            CloseScreen();
        }
    }

    private void CloseScreen()
    {
        if (_closing) return;
        _closing = true;

        // Cleanup
        if (_gauntletMovie != null)
        {
            _gauntletLayer?.ReleaseMovie(_gauntletMovie);
            _gauntletMovie = null;
        }

        if (_gauntletLayer != null)
        {
            RemoveLayer(_gauntletLayer);
            _gauntletLayer = null;
        }

        _dataSource?.OnFinalize();
        _dataSource = null;

        ScreenManager.PopScreen();
        _onClosed?.Invoke();
    }

    protected override void OnFinalize()
    {
        base.OnFinalize();

        if (_dataSource != null)
        {
            _dataSource.OnFinalize();
            _dataSource = null;
        }

        if (_gauntletMovie != null)
        {
            _gauntletLayer?.ReleaseMovie(_gauntletMovie);
            _gauntletMovie = null;
        }
    }

    public static void Open(Action onClosed = null)
    {
        NextFrameDispatcher.RunNextFrame(() =>
        {
            if (Campaign.Current == null)
            {
                onClosed?.Invoke();
                return;
            }

            var screen = new CustomScreen(onClosed);
            ScreenManager.PushScreen(screen);
        });
    }
}
```

### Logging Best Practices

Keep logs meaningful for users/developers, not verbose internal traces:

```csharp
// ✅ GOOD: Meaningful user-facing logs
ModLogger.Info("LanceLifeUI", $"Lance event '{eventTitle}' displayed");
ModLogger.Warn("LanceLifeUI", "Could not display character portrait, using scene image instead");
ModLogger.Error("LanceLifeUI", $"Failed to display lance event: {ex.Message}", ex);

// ❌ BAD: Excessive internal details
ModLogger.Debug("LanceLifeUI", "OnInitialize: Creating ViewModel...");
ModLogger.Debug("LanceLifeUI", "OnInitialize: Creating Gauntlet layer...");
ModLogger.Debug("LanceLifeUI", "OnInitialize: Loading movie...");
ModLogger.Debug("LanceLifeUI", "OnInitialize: Movie loaded, adding layer...");
```

**Guidelines:**
- **Info**: User-visible actions (screen opened, choice selected)
- **Warn**: Fallback behaviors (using default instead of custom)
- **Error**: Actual failures (screen failed to open, invalid data)
- **Debug**: Use sparingly, only for complex troubleshooting

---

## Pre-Deployment Checklist

Before deploying a custom Gauntlet screen, verify:

### Critical Requirements
- [ ] **All colors are 8-digit format** (`#RRGGBBAAFF`) in both C# and XML
- [ ] **Screen push is deferred** to next frame using `NextFrameDispatcher`
- [ ] **Campaign state is validated** in all deferred actions
- [ ] **LoadMovie() return value is checked** and null case handled

### Code Safety
- [ ] **All ViewModel properties have null safety** (`??` operator used)
- [ ] **Proper cleanup implemented** in `OnFinalize()` and `CloseScreen()`
- [ ] **Exception handling** wraps `OnInitialize()` with cleanup
- [ ] **Unused widgets removed** from XML (or valid data always provided)

### Functionality
- [ ] **Input restrictions set correctly** (mouse visible, input captured)
- [ ] **Game pause implemented** if modal screen
- [ ] **ESC key handler** respects `CanClose` flag
- [ ] **XML files deployed** to game module directory

### Polish
- [ ] **Meaningful logging** in place (not excessive debug logs)
- [ ] **Error messages user-friendly** (not technical internals)
- [ ] **Memory cleanup** complete (no leaks or ghost layers)

---

## Enlisted Implementation Files

**Custom Gauntlet Screen:**
- **Screen:** `src/Features/Lances/UI/LanceLifeEventScreen.cs`
- **ViewModel:** `src/Features/Lances/UI/LanceLifeEventVM.cs`
- **Choice ViewModel:** `src/Features/Lances/UI/EventChoiceVM.cs`
- **XML Layout:** `GUI/Prefabs/Events/LanceLifeEventScreen.xml`
- **Choice Button XML:** `GUI/Prefabs/Events/EventChoiceButton.xml`

**Supporting Systems:**
- **Presenter:** `src/Features/Lances/UI/ModernEventPresenter.cs`
- **NextFrameDispatcher:** `src/Mod.Entry/NextFrameDispatcher.cs`

---

## Summary

Following these guidelines will ensure your custom Gauntlet screens work reliably without crashes or binding errors. The most critical takeaway:

> **Always use 8-digit hex colors (`#RRGGBBAAFF`) everywhere in Gauntlet UI code.**

This single issue caused hours of debugging and can be completely avoided by following the color format requirement. Combined with proper deferred screen pushing and comprehensive cleanup, your custom screens will be stable and maintainable.
