# gauntlet ui screens playbook (enlisted)

this is the **single source of truth** for how we build and maintain gauntlet ui screens in enlisted.

it is written for:
- Humans maintaining the mod
- Future AI agents making UI changes safely (without freezing/crashing Bannerlord)

## index

- [goals](#goals)
- [quick “do this” checklist (ai-safe)](#quick-do-this-checklist-ai-safe)
- [integration styles (pick one)](#integration-styles-pick-one)

## goals

- Keep UI changes **stable** (no freezes, no invisible screens, no input lock-ups).
- Keep UI code **consistent** (same lifecycle, logging, cleanup).
- Make it easy to add/modify screens without rediscovering old pitfalls.

## Quick “Do This” Checklist (AI-safe)

Before you add/change any Gauntlet UI:
- **Decide the integration style**:
  - **Overlay layer on TopScreen** (preferred when opening from game menus): use `ScreenManager.TopScreen.AddLayer(layer)`.
  - **ScreenBase push** (rare): use `ScreenManager.PushScreen(screen)` but **defer to next frame**.
- **Always load required sprite categories** for any sprites/brushes you rely on (see “Sprite Categories”).
- **Never use 6-digit hex colors** in XML or ViewModels. Always `#RRGGBBAA`.
- **Assume bindings happen during `LoadMovie()` even if a widget is hidden**.
- **Always implement cleanup**: release movie, remove layer, lose focus, finalize VM, unload sprite categories, restore time mode.
- Add minimal **diagnostic logging** at open/close + key commands (do not spam per-frame logs).

## Integration Styles (Pick One)

### Style A (Preferred): Layer-based overlay on `TopScreen`

Use this when opening UI from:
- Game menus (camp, quartermaster, etc.)
- Map/menu overlays

Pattern:
- Create `GauntletLayer`
- `LoadMovie(movieName, rootViewModel)`
- Apply input restrictions
- `ScreenManager.TopScreen.AddLayer(layer)`
- Set focus

This is the pattern we currently use for `CampBulletinScreen` and it mirrors native `TownManagement` views.

### Style B: Pushed `ScreenBase`

Use this only when:
- You truly need a full “screen” with its own stack semantics

If you push a screen, **defer it**:
- Native engine updates can crash if you push mid-tick.
- Use `Enlisted.Mod.Entry.NextFrameDispatcher.RunNextFrame(() => ScreenManager.PushScreen(...))`.

## Sprite Categories (Why your UI can be invisible)

Native Gauntlet views frequently do:
- `UIResourceManager.LoadSpriteCategory("ui_town_management")`
- `category.Load()`

If you use sprites/brushes that live in a sprite category but do not load it, you can get:
- missing sprites
- “flat” / blank UI elements
- invisible frames/dividers

**Rule: if your XML uses native `SPGeneral\TownManagement\...` sprites or TownManagement brushes, load `ui_town_management`.**

Common categories we use:
- **`ui_town_management`**: Town management frame/dividers/visuals.
- **`ui_encyclopedia`**: `Encyclopedia.Frame` and other encyclopedia-related visuals.

Where we do this:
- `src/Features/Camp/UI/Bulletin/CampBulletinScreen.cs` loads/unloads sprite categories when opening/closing.

## XML Binding Rules (Most common "it loads but nothing shows" problem)

### 1) Root DataSource

We pass the ViewModel directly into `LoadMovie(movieName, rootVm)`.

That means:
- Root widget should NOT try to do `DataSource="{Something}"` unless you are intentionally switching root context.

When you need nested contexts:
- Use `Widget DataSource="{ChildVm}"` inside the tree.

### 2) Binding happens even when hidden

Gauntlet evaluates bindings for widget properties during `LoadMovie()` regardless of `IsVisible`.

So this can still crash even if the widget is invisible:
- widgets that bind invalid body properties
- image identifier widgets with invalid IDs
- malformed color strings

**Rule: either remove risky widgets entirely or ensure the VM always provides valid values.**

### 3) Colors must be 8-digit

Bannerlord's `Color.ConvertStringToColor()` expects `#RRGGBBAA`.

Bad:
- `#FFFFFF`

Good:
- `#FFFFFFFF`

This applies to:
- XML: `Color=`, `Brush.TextColor=`, etc.
- C# ViewModels: any string color properties

### 4) Match Native Property Names for Perfect Compatibility

**When to do this:** You're replicating a native UI pattern and want bindings to "just work".

**Example:** Native `KingdomWarLogItemVM` uses these exact property names:
- `WarLogText` (main content)
- `WarLogTimeText` (timestamp)
- `Banner` (BannerImageIdentifierVM)

**If you match these names exactly in your VM:**
```csharp
public class ReportItemVM : ViewModel
{
    [DataSourceProperty] public string WarLogText { get; set; }
    [DataSourceProperty] public string WarLogTimeText { get; set; }
    [DataSourceProperty] public BannerImageIdentifierVM Banner { get; set; }
}
```

**Then native XML works without modification:**
```xml
<!-- This is the actual native DiplomacyWarLogElement.xml -->
<RichTextWidget Text="@WarLogText" />
<RichTextWidget Text="@WarLogTimeText" />
<MaskedTextureWidget DataSource="{Banner}" />
```

**Benefit:** Native brushes, layouts, and styles apply automatically. No guesswork on brush names or sizing.

**How to find native property names:**
1. Decompile the relevant native ViewModel (e.g., `KingdomWarLogItemVM`)
2. Look for `[DataSourceProperty]` properties
3. Match those exact names in your VM

**Our implementations using this pattern:**
- `ReportItemVM` matches `KingdomWarLogItemVM` (WarLogText, WarLogTimeText, Banner)
- `ScheduleBlockItemVM` matches `PolicyActiveTuple` (Name, IsSelected, OnSelect)
- `ReportCategoryItemVM` matches native category/tuple patterns

## Input Handling and Closing

### HotKey categories

Most native menu overlays register:
- `GenericPanelGameKeyCategory`

We use this for:
- "Done/Confirm"
- "Exit"

When opening a layer:
- Register the hotkey category if not already registered
- Provide the VM an `InputKeyItemVM` (so the UI can display the key)

### Closing patterns

We use three close paths:
- **Done button** calls VM command (e.g., `ExecuteDone`)
- **ESC** closes the overlay
- VM may request close by setting `Show = false` (native pattern)

Important:
- If campaign ticks are suspended by the menu, "Show=false then tick closes view" may not run reliably.
- In those cases, the VM should call the screen close method directly (we do this in `CampBulletinVM.ExecuteDone()`).

### Command Method Naming: OnSelect() vs ExecuteSelect()

**Native pattern:** Most native tuple ViewModels use `OnSelect()` for click commands.

**Examples:**
- `KingdomWarItemVM.OnSelect()`
- `PolicyItemVM.OnSelect()`
- Native tuples bind: `Command.Click="OnSelect"`

**Our pattern:**
```csharp
public class YourItemVM : ViewModel
{
    private readonly Action<YourItemVM> _onSelect;
    
    public void OnSelect()
    {
        _onSelect?.Invoke(this);
    }
    
    // Optional: Keep for backward compatibility
    public void ExecuteSelect() => OnSelect();
}
```

**XML binding:**
```xml
<ButtonWidget Command.Click="OnSelect" IsSelected="@IsSelected">
  <!-- content -->
</ButtonWidget>
```

**Why OnSelect() instead of ExecuteSelect():**
- Matches native naming convention
- Shorter, cleaner
- Native XML examples use `OnSelect` consistently

**Rule:** For new clickable tuple ViewModels, use `OnSelect()` as the primary method name. Add `ExecuteSelect()` wrapper if needed for backward compatibility.

## Lifecycle and Cleanup (Non-negotiable)

On Close:
- Release the movie: `layer.ReleaseMovie(movieId)`
- Remove layer: `ScreenManager.TopScreen.RemoveLayer(layer)`
- Lose focus: `ScreenManager.TryLoseFocus(layer)`
- Finalize VM: `vm.OnFinalize()`
- Unload sprite categories
- Restore time control mode (if you stopped time)

If you miss any of these:
- input can get stuck
- UI can stay “ghosted”
- memory leaks / references persist across reloads

## Preventing “double open” jitter

Game menu buttons can fire multiple times (native UI transitions can jitter).

Rule:
- add a simple guard (static `_isOpen` bool) in any screen that can be opened from game menus.

## Logging (Performance-friendly)

We want logs that are:
- useful for diagnosing broken UI
- not spammy per-frame

Recommended:
- log **Open**, **Close**, and critical command invocations (`ExecuteDone`, `ExecuteSelect`, etc.)
- log exceptions once with context

Do NOT:
- log every frame
- log every binding refresh

## Using Native UI as Reference (How to “make it look native”)

When you want a screen to look like a native one:
- Find the prefab in the game folder (example: `Modules/SandBox/GUI/Prefabs/TownManagement/TownManagement.xml`)
- Find the brush pack (example: `Modules/SandBox/GUI/Brushes/TownManagement.xml`)
- Copy structure/patterns, then replace bindings with our VM properties

Camp Bulletin currently mirrors:
- `Encyclopedia.Frame` popup
- `Standard.PopupCloseButton`
- TownManagement dividers and "project ring" brushes

## Critical XML Layout Pitfalls

### Problem 1: Vertical Text (Character Wrapping)

**Symptom:** Text displays vertically like "M-e-d-i-c-a-l  T-e-n-t" instead of horizontally.

**Cause:** Using `WidthSizePolicy="StretchToParent"` on a TextWidget inside a constrained panel causes character-by-character wrapping.

**Solution:**
```xml
<!-- BAD: Will wrap vertically -->
<TextWidget WidthSizePolicy="StretchToParent" HeightSizePolicy="CoverChildren" 
            Text="@TitleText" />

<!-- GOOD: Natural horizontal layout -->
<TextWidget WidthSizePolicy="CoverChildren" HeightSizePolicy="CoverChildren" 
            Text="@TitleText" ClipContents="false" />
```

**Rule:** For titles/headers that should never wrap, always use:
- `WidthSizePolicy="CoverChildren"`
- `ClipContents="false"`

### Problem 2: Missing Closing Tags (Cryptic XmlException)

**Symptom:** Exception like "The 'ListPanel' start tag on line 109 does not match the end tag of 'BrushWidget'. Line 409."

**Cause:** A missing `</ListPanel>` or `</Widget>` tag somewhere in between. The error line numbers are misleading - they point to where the parser gives up, not where you forgot the tag.

**Solution:**
1. Count opening vs closing tags: `<ListPanel>` vs `</ListPanel>`, `<Widget>` vs `</Widget>`
2. Use PowerShell to verify:
   ```powershell
   $xml = Get-Content 'path\to\file.xml' -Raw
   [regex]::Matches($xml, '<ListPanel').Count  # Should equal...
   [regex]::Matches($xml, '</ListPanel>').Count
   ```
3. Trace the structure manually from the line mentioned in the error
4. Add comments to mark closings: `</ListPanel> <!-- Close BOTTOM TIER -->`

**Rule:** In complex nested layouts (like multi-tier structures), always add closing comments to track what you're closing.

### Problem 3: Divider Overlapping Text

**Symptom:** Horizontal divider line cuts through title text.

**Cause:** `MarginTop` on divider doesn't account for text height.

**Solution:**
```xml
<!-- BAD: Divider too close -->
<TextWidget MarginTop="6" Text="@Title" />
<Widget MarginTop="8" Sprite="SPGeneral\TownManagement\horizontal_divider" />

<!-- GOOD: Space for text + breathing room -->
<TextWidget MarginTop="6" Text="@Title" />
<Widget MarginTop="46" Sprite="SPGeneral\TownManagement\horizontal_divider" />
```

**Rule:** Dividers after large text need MarginTop ≈ 40-50px (depends on font size).

### Problem 4: Icon Not Aligned with Text (Gold Coin Example)

**Symptom:** Icon and text stack vertically instead of horizontally, or icon too far from text.

**Cause:** ListPanel not using `HorizontalLeftToRight` layout, or missing margins.

**Solution:**
```xml
<!-- BAD: Default vertical stack -->
<ListPanel>
  <Children>
    <TextWidget Text="24" />
    <Widget Sprite="General\Icons\Coin@2x" />
  </Children>
</ListPanel>

<!-- GOOD: Horizontal with spacing -->
<ListPanel StackLayout.LayoutMethod="HorizontalLeftToRight">
  <Children>
    <TextWidget Text="24" />
    <Widget MarginLeft="2" Sprite="General\Icons\Coin@2x" />
  </Children>
</ListPanel>
```

**Rule:** Use `StackLayout.LayoutMethod="HorizontalLeftToRight"` for icon+text combos, add small `MarginLeft` to icon for spacing.

### Problem 5: XML Validates but Game Crashes

**Symptom:** PowerShell `[xml]$x = Get-Content...` succeeds, but game shows XmlException.

**Cause:** Game was running during XML edit - cached old version.

**Solution:**
1. Close game completely (not just to main menu)
2. Close Bannerlord Launcher
3. Relaunch
4. If still fails, verify deployed file vs source: both should have same line count and modification time

**Rule:** Always fully restart game after XML changes. Bannerlord does NOT hot-reload prefabs.

### Problem 6: Buttons Don't Render (List Items Missing)

**Symptom:** You have a `NavigatableListPanel` with `ItemTemplate`, but nothing shows. No errors, just empty space.

**Cause:** The tuple prefab referenced in `ItemTemplate` was not copied to the Bannerlord module folder during build.

**How it happens:**
```xml
<!-- In your panel XML -->
<NavigatableListPanel DataSource="{Items}">
  <ItemTemplate>
    <ReportCategoryTuple />  <!-- This prefab doesn't exist in deployed module! -->
  </ItemTemplate>
</NavigatableListPanel>
```

**Solution:**
1. Create the tuple prefab: `GUI/Prefabs/Camp/ReportCategoryTuple.xml`
2. **CRITICAL:** Add it to `Enlisted.csproj` AfterBuild target:
   ```xml
   <Target Name="AfterBuild">
     <!-- Other copies... -->
     <Copy SourceFiles="GUI\Prefabs\Camp\ReportCategoryTuple.xml" 
           DestinationFolder="$(OutputPath)..\..\GUI\Prefabs\Camp\"/>
   </Target>
   ```
3. Rebuild (new prefabs won't appear without rebuild)
4. Verify file exists: `Modules\Enlisted\GUI\Prefabs\Camp\ReportCategoryTuple.xml`

**Rule:** Every new prefab MUST have a corresponding `<Copy>` in `Enlisted.csproj`, or Gauntlet will silently fail to instantiate items.

**How to verify:**
```powershell
# Check deployed prefabs
dir "C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\Enlisted\GUI\Prefabs\Camp" | findstr "YourTupleName"
```

## How to add a new UI screen (Step-by-step)

1) **Pick style**
   - overlay layer (preferred) vs ScreenBase
2) **Create Screen controller**
   - `Open()` with NextFrameDispatcher if needed
   - load sprite categories required by the UI
3) **Create ViewModel**
   - `DataSourceProperty` on every bound property
   - no null references; safe fallbacks everywhere
4) **Create XML**
   - keep root context simple
   - prefer native brushes over hand-colored rectangles
5) **Integrate**
   - wire menu option to call `Open()`
6) **Verify**
   - open/close repeatedly
   - verify Done + ESC
   - verify it works while in menu context (no tick assumptions)

## Replicating Native UI Patterns

### Pattern 1: Diplomacy-Style Category Selection

**When to use:** You have multiple categories of items and want users to click a category to see its contents.

**Native example:** Kingdom → Diplomacy screen (click "At War" to see list of wars, click a war to see details)

**Our implementation:** Camp Reports tab

**How it works:**
1. **Left panel:** One collapsible section with clickable category buttons inside
2. **Right panel:** Feed of items from selected category + detail view for selected item

**Key components:**
```
PartyHeaderToggleWidget (header that expands/collapses)
  └─ NavigatableListPanel (list of category buttons)
      └─ ItemTemplate with ButtonWidget (clickable categories)
```

**ViewModel pattern:**
```csharp
// Category item
public class CategoryItemVM : ViewModel
{
    [DataSourceProperty] public bool IsSelected { get; set; }
    [DataSourceProperty] public string Name { get; set; }
    
    public void OnSelect() { /* clicked */ }
}

// Parent VM
private string _currentSelectedCategory;
private MBBindingList<ReportItemVM> _rightPanelItems;

public void OnCategorySelect(CategoryItemVM category)
{
    CurrentSelectedCategory = category.Name;
    RefreshRightPanel(); // Populate _rightPanelItems from selected category
}
```

**XML tuple pattern:**
```xml
<ButtonWidget Command.Click="OnSelect" IsSelected="@IsSelected">
  <Children>
    <TextWidget Text="@Name" />
  </Children>
</ButtonWidget>
```

**Critical:** The tuple prefab MUST be copied to module folder in `Enlisted.csproj` AfterBuild target!

### Pattern 2: Log-Style Feed Items (War Log Pattern)

**When to use:** Displaying a scrollable feed of news/events with timestamps.

**Native example:** Kingdom → Diplomacy → Select a war → See recent events

**Our implementation:** Camp Reports feed (right panel after selecting category)

**Key insight:** Native uses specific property names (`WarLogText`, `WarLogTimeText`, `Banner`) - matching these names ensures perfect binding compatibility.

**ViewModel pattern:**
```csharp
public class ReportItemVM : ViewModel
{
    // EXACT native property names for DiplomacyWarLogElement.xml
    [DataSourceProperty] public string WarLogText { get; set; }      // Main text
    [DataSourceProperty] public string WarLogTimeText { get; set; }  // Timestamp
    [DataSourceProperty] public BannerImageIdentifierVM Banner { get; set; }
    [DataSourceProperty] public bool IsSelected { get; set; }
    
    public void OnSelect() { /* clicked */ }
}
```

**XML pattern (exactly matches native DiplomacyWarLogElement.xml):**
```xml
<Widget>
  <ListPanel StackLayout.LayoutMethod="HorizontalLeftToRight">
    <Children>
      <MaskedTextureWidget DataSource="{Banner}" Brush="Kingdom.OtherWars.Faction.Small.Banner" />
      <Widget Sprite="Encyclopedia\subpage_ball" />
      <ListPanel StackLayout.LayoutMethod="VerticalBottomToTop">
        <Children>
          <RichTextWidget Text="@WarLogText" />
          <RichTextWidget Text="@WarLogTimeText" Brush.FontSize="16" />
        </Children>
      </ListPanel>
    </Children>
  </ListPanel>
</Widget>
```

### Pattern 3: Policy-Style Clickable Buttons

**When to use:** List of items users can click to select (policies, activities, schedule blocks).

**Native example:** Kingdom → Policies → Active Policies / Other Policies lists

**Our implementation:** Schedule tab activities, Reports categories

**Key components:**
- `NavigatableListPanel` with `ItemTemplate`
- ButtonWidget with `Command.Click="OnSelect"`
- `IsSelected="@IsSelected"` for visual feedback

**ViewModel pattern:**
```csharp
public class ActivityItemVM : ViewModel
{
    [DataSourceProperty] public string Name { get; set; }
    [DataSourceProperty] public bool IsSelected { get; set; }
    
    public void OnSelect() { _onSelect?.Invoke(this); }
}
```

**XML tuple (matches native PolicyActiveTuple.xml):**
```xml
<Prefab>
  <Window>
    <ButtonWidget DoNotPassEventsToChildren="true" 
                  Command.Click="OnSelect" 
                  IsSelected="@IsSelected"
                  Brush="Kingdom.Policy.Active.Tuple">
      <Children>
        <TextWidget Text="@Name" Brush="Kingdom.PoliciesItem.Text" />
      </Children>
    </ButtonWidget>
  </Window>
</Prefab>
```

### Pattern 4: Collapsible Sections with Inner Lists

**When to use:** Organizing related items under expandable headers (like "At War" / "At Peace" in Diplomacy).

**Native example:** Kingdom → Diplomacy (expand "At War" to see wars list)

**Key widget:** `PartyHeaderToggleWidget`

**XML pattern:**
```xml
<!-- The toggle button (header) -->
<PartyHeaderToggleWidget 
    Brush="Kingdom.Policy.Toggle.Tuple"
    CollapseIndicator="Description\CollapseIndicator"
    ListPanel="..\PathTo\InnerList"
    WidgetToClose="..\PathTo\ParentWidget">
  <Children>
    <ListPanel Id="Description">
      <Children>
        <BrushWidget Id="CollapseIndicator" Brush="Party.Toggle.ExpandIndicator" />
        <RichTextWidget Text="@HeaderText" />
      </Children>
    </ListPanel>
  </Children>
</PartyHeaderToggleWidget>

<!-- The collapsible content -->
<Widget Id="ParentWidget">
  <Children>
    <NavigatableListPanel Id="InnerList" DataSource="{Items}">
      <ItemTemplate>
        <YourTuple />
      </ItemTemplate>
    </NavigatableListPanel>
  </Children>
</Widget>
```

**Critical paths:**
- `CollapseIndicator`: path to the arrow icon widget
- `ListPanel`: path to the NavigatableListPanel to track
- `WidgetToClose`: path to the parent widget to hide/show

### Common Pitfall: Headers vs Buttons

**Problem:** Clicking the toggle header does nothing, inner buttons don't appear.

**Why:** Toggle headers ONLY expand/collapse. The actual clickable items are ButtonWidgets INSIDE the expanded section.

**Solution:**
1. `PartyHeaderToggleWidget` = just the header (not clickable for selection)
2. Inside the collapsible section = `NavigatableListPanel` with `ButtonWidget` items (these are clickable)

**Wrong mental model:** "Click header → select category"
**Correct mental model:** "Click header → expand/collapse → click inner button → select item"

## Reference Implementations in This Repo

- Layer-based overlay (menu-safe):
  - `src/Features/Camp/UI/Bulletin/CampBulletinScreen.cs`
  - `src/Features/Camp/UI/Bulletin/CampBulletinUiTickBehavior.cs`
- Kingdom-style tabbed screen:
  - `src/Features/Camp/UI/Management/CampManagementScreen.cs` (static layer-based)
  - `src/Features/Camp/UI/Management/CampManagementVM.cs` (tab controller)
- Diplomacy pattern (category selection):
  - `src/Features/Camp/UI/Management/Tabs/CampReportsVM.cs`
  - `GUI/Prefabs/Camp/CampReportsPanel.xml`
  - `GUI/Prefabs/Camp/ReportCategoryTuple.xml`
  - `GUI/Prefabs/Camp/ReportTuple.xml`
- Policy pattern (clickable lists):
  - `src/Features/Camp/UI/Management/Tabs/CampScheduleVM.cs`
  - `GUI/Prefabs/Camp/ScheduleActiveTuple.xml`
- ScreenBase pushed screen:
  - `src/Features/Lances/UI/LanceLifeEventScreen.cs`
- Another working overlay pattern:
  - `src/Features/Equipment/UI/QuartermasterEquipmentSelectorBehavior.cs`

## Acceptance Criteria for UI PRs

A UI change is acceptable when:
- It opens reliably from the intended entry point
- It closes reliably via Done + ESC
- It doesn’t crash on load (including with hidden widgets)
- It doesn’t freeze the game/menu
- It cleans up layers/movies/categories (no stuck input)
- It uses native brushes where appropriate (no “weird boxes” unless intentionally designed)


