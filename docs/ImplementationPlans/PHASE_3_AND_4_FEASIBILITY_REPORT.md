# Phase 3 & 4 Feasibility Report

**Date:** December 15, 2025  
**Investigation:** Native Bannerlord API Decompile Analysis  
**Versions:** Based on Bannerlord 1.2.12 API

---

## Executive Summary

✅ **Phase 3 (Themed Area Screens): FULLY FEASIBLE**  
- Simple to implement with existing Gauntlet UI capabilities
- All required APIs are available and well-documented in native code
- Estimated effort: **4-6 days** (as planned)
- Risk: **Low**

⚠️ **Phase 4 (War Room Command Center): PARTIALLY FEASIBLE WITH LIMITATIONS**  
- Multi-tab interface: **Fully supported** ✅
- Custom layouts and alerts: **Fully supported** ✅
- Interactive camp map view: **Major technical limitation** ⚠️
- Estimated effort: **2-3 weeks** (realistic, but map feature needs redesign)
- Risk: **Medium** (interactive map concept not directly supported)

---

## Phase 3: Themed Area Screens

### Goal (From Roadmap)

Add unique themed backgrounds and visuals for each camp location:
- Themed backgrounds per location
- Dynamic background elements
- Area-specific status displays

### Technical Investigation

#### ✅ Themed Backgrounds: FULLY SUPPORTED

**API Evidence:**

From `TaleWorlds.GauntletUI.BaseTypes.BrushWidget.cs`:
```csharp
public class BrushWidget : Widget
{
    [Editor(false)]
    public Brush Brush
    {
        get { /* ... */ }
        set
        {
            if (this._originalBrush == value) return;
            this._originalBrush = value;
            this._clonedBrush = null;
            this.OnBrushChanged();
            this.OnPropertyChanged<Brush>(value, nameof(Brush));
        }
    }
    
    public new Sprite Sprite
    {
        get => this.ReadOnlyBrush.DefaultStyle.GetLayer("Default").Sprite;
        set => this.Brush.DefaultStyle.GetLayer("Default").Sprite = value;
    }
}
```

**Native Game Usage:**

From `SandBox.GauntletUI\GauntletKingdomScreen.cs`:
```csharp
void IGameStateListener.OnActivate()
{
    this.OnActivate();
    this._kingdomCategory = UIResourceManager.LoadSpriteCategory("ui_kingdom");
    this._clanCategory = UIResourceManager.LoadSpriteCategory("ui_clan");
    this._gauntletLayer = new GauntletLayer("KingdomScreen", 1, true);
    // ... screen setup
}
```

**Implementation Approach for Phase 3:**

**Option A: Color Overlays (Simplest, 1-2 days)**
```xml
<!-- In CampAreaScreen.xml -->
<Widget WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" 
        Sprite="BlankWhiteSquare_9" 
        Color="@LocationBackgroundColor" 
        Alpha="0.3" />
```

```csharp
// In CampAreaVM.cs
public string LocationBackgroundColor
{
    get
    {
        return _locationId switch
        {
            "medical_tent" => "#CC0000AA",      // Reddish tint
            "training_grounds" => "#996633AA",  // Sandy brown
            "lords_tent" => "#0033AAAA",        // Blue/regal
            "camp_fire" => "#FF6600AA",         // Orange glow
            "quartermaster" => "#666666AA",     // Gray/practical
            "personal_quarters" => "#6B4423AA", // Wooden brown
            _ => "#00000000"
        };
    }
}
```

**Verdict:** ✅ **FULLY FEASIBLE** - Trivial to implement with existing XML/ViewModel pattern.

**Option B: Custom Brush System (Medium, 3-4 days)**

Create location-specific brushes in `Brushes/` folder:
```xml
<!-- GUI/Brushes/CampLocations.xml -->
<Brushes>
  <Brush Name="MedicalTentBackground">
    <Layers>
      <Layer Name="Default">
        <Texture Name="medical_tent_bg" />
      </Layer>
    </Layers>
  </Brush>
  <!-- ... other locations -->
</Brushes>
```

Load dynamically:
```csharp
// In CampAreaScreen.cs OnInitialize
private void LoadLocationBrush(string locationId)
{
    var brushName = $"{locationId}_background";
    // Brush is automatically loaded by Gauntlet from Brushes/ folder
}
```

**Verdict:** ✅ **FULLY FEASIBLE** - Native game uses this pattern extensively (Kingdom, Clan screens).

**Option C: Hybrid Approach (Best for Phase 3, 4-6 days)**

- Base color overlay for atmosphere
- Optional decorative sprite elements per location
- Reuse existing game brushes for consistency

```xml
<!-- Example: Training Grounds with decorative elements -->
<Widget WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent">
  <Children>
    <!-- Base color atmosphere -->
    <Widget Sprite="BlankWhiteSquare_9" Color="#996633AA" Alpha="0.3" />
    
    <!-- Decorative elements (reuse existing sprites) -->
    <Widget Sprite="SPGeneral\weaponsmith_1" PositionXOffset="-400" PositionYOffset="200" />
    <Widget Sprite="SPGeneral\armour_1" PositionXOffset="400" PositionYOffset="200" />
  </Children>
</Widget>
```

**Verdict:** ✅ **FULLY FEASIBLE** - Best balance of effort vs. visual impact.

---

#### ✅ Dynamic Background Elements: SUPPORTED

**Particle Systems:**

Gauntlet supports particle effects via `ParticleWidget` (found in decompile), but simpler approaches are more practical:

**Animated Sprites:**
```xml
<Widget Sprite="@SmokeAnimation" IsVisible="@IsCampFireLocation">
  <!-- Animated sprite with frame sequence -->
</Widget>
```

**NPC Silhouettes:**

Reuse existing character sprites with alpha/tint:
```xml
<ListPanel DataSource="{NPCsPresent}">
  <ItemTemplate>
    <Widget Sprite="SPGeneral\figure_silhouette" Alpha="0.4" />
  </ItemTemplate>
</ListPanel>
```

**Verdict:** ✅ **FEASIBLE** - Multiple approaches available, from simple static sprites to animated sequences.

---

#### ✅ Area-Specific Status: ALREADY IMPLEMENTED

This is just ViewModel properties bound to XML:

```csharp
// In CampAreaVM.cs
[DataSourceProperty]
public int WoundedCount { get; set; }  // Medical Tent

[DataSourceProperty]
public string ActiveDrillText { get; set; }  // Training Grounds

[DataSourceProperty]
public MBBindingList<string> PeoplePresent { get; set; }  // Camp Fire
```

```xml
<!-- In CampAreaScreen.xml header -->
<Widget IsVisible="@IsMedicalTent">
  <TextWidget Text="@WoundedCountText" />
</Widget>
```

**Verdict:** ✅ **FULLY FEASIBLE** - Standard ViewModel databinding pattern.

---

### Phase 3 Recommendation

**Status:** ✅ **GREEN LIGHT - PROCEED AS PLANNED**

**Recommended Approach:**
1. **Day 1-2:** Color overlay system with location constants
2. **Day 3-4:** Add 2-3 decorative sprite elements per location (reuse existing)
3. **Day 5:** Area-specific status displays (wounded count, drill info, etc.)
4. **Day 6:** Polish, testing, edge cases

**Technical Risk:** **VERY LOW**  
**API Availability:** **EXCELLENT** - All required APIs present and well-documented  
**Effort Estimate:** **Accurate** - 4-6 days realistic

---

## Phase 4: War Room Command Center

### Goal (From Roadmap)

Full tactical hub with:
- Interactive camp map view ⚠️
- Multi-tab interface (Map, Activities, Lance, Status, Alerts) ✅
- Real-time population tracking ✅
- Alert notifications system ✅
- Integration with AI schedule ✅

### Technical Investigation

#### ✅ Multi-Tab Interface: FULLY SUPPORTED

**API Evidence:**

From `TaleWorlds.GauntletUI.BaseTypes.TabControl.cs`:
```csharp
public class TabControl : Widget
{
    private Widget _activeTab;
    private int _selectedIndex;
    
    public event OnActiveTabChangeEvent OnActiveTabChange;
    
    [DataSourceProperty]
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (_selectedIndex == value) return;
            _selectedIndex = value;
            SetActiveTab(_selectedIndex);
            OnPropertyChanged(value, nameof(SelectedIndex));
        }
    }
    
    private void SetActiveTab(Widget newTab)
    {
        if (ActiveTab == newTab || newTab == null) return;
        if (ActiveTab != null)
            ActiveTab.IsVisible = false;
        ActiveTab = newTab;
        ActiveTab.IsVisible = true;
        SelectedIndex = GetChildIndex(ActiveTab);
    }
}
```

**Native Game Usage:**

Kingdom screen uses tabs extensively (seen in `GauntletKingdomScreen.cs`):
```csharp
protected override void OnFrameTick(float dt)
{
    // ...
    if (DataSource.CanSwitchTabs)
    {
        if (_gauntletLayer.Input.IsHotKeyReleased("SwitchToPreviousTab"))
        {
            DataSource.SelectPreviousCategory();
            UISoundsHelper.PlayUISound("event:/ui/tab");
        }
        else if (_gauntletLayer.Input.IsHotKeyReleased("SwitchToNextTab"))
        {
            DataSource.SelectNextCategory();
            UISoundsHelper.PlayUISound("event:/ui/tab");
        }
    }
}
```

**Implementation for War Room:**

```xml
<!-- WarRoomScreen.xml -->
<TabControl Id="MainTabs" SelectedIndex="@SelectedTabIndex">
  <Children>
    <!-- Tab 1: Map View -->
    <Widget Id="MapTab">
      <!-- Map content here -->
    </Widget>
    
    <!-- Tab 2: Activities -->
    <Widget Id="ActivitiesTab">
      <!-- Reuse existing CampAreaScreen content -->
    </Widget>
    
    <!-- Tab 3: Lance Status -->
    <Widget Id="LanceTab">
      <!-- Lance member cards -->
    </Widget>
    
    <!-- Tab 4: Alerts -->
    <Widget Id="AlertsTab">
      <!-- Alert list -->
    </Widget>
  </Children>
</TabControl>
```

```csharp
// In WarRoomVM.cs
[DataSourceProperty]
public int SelectedTabIndex
{
    get => _selectedTabIndex;
    set
    {
        if (_selectedTabIndex != value)
        {
            _selectedTabIndex = value;
            OnPropertyChangedWithValue(value, nameof(SelectedTabIndex));
            RefreshTabContent();
        }
    }
}

public void SelectPreviousTab()
{
    SelectedTabIndex = Math.Max(0, SelectedTabIndex - 1);
}

public void SelectNextTab()
{
    SelectedTabIndex = Math.Min(3, SelectedTabIndex + 1);
}
```

**Verdict:** ✅ **FULLY SUPPORTED** - Native `TabControl` widget is production-ready and used throughout the game.

---

#### ✅ Real-Time Population Tracking: FULLY FEASIBLE

**Approach:**

Query existing behaviors for lance member locations:

```csharp
// In WarRoomVM.cs
private void RefreshPopulationCounts()
{
    var schedule = AIScheduleBehavior.Instance;
    var lanceLife = LanceLifeSimulationBehavior.Instance;
    
    // Count members at each location
    var membersByLocation = new Dictionary<string, int>();
    
    foreach (var member in GetLanceMembers())
    {
        var currentActivity = schedule?.GetMemberActivity(member);
        var location = currentActivity?.Location ?? "unknown";
        
        if (!membersByLocation.ContainsKey(location))
            membersByLocation[location] = 0;
        membersByLocation[location]++;
    }
    
    // Update UI
    MedicalTentCount = membersByLocation.GetValueOrDefault("medical_tent", 0);
    TrainingGroundsCount = membersByLocation.GetValueOrDefault("training_grounds", 0);
    // ... etc
}
```

**Update Frequency:**

```csharp
protected override void OnFrameTick(float dt)
{
    base.OnFrameTick(dt);
    
    _updateTimer += dt;
    if (_updateTimer >= 1.0f)  // Update every second
    {
        RefreshPopulationCounts();
        _updateTimer = 0f;
    }
}
```

**Verdict:** ✅ **FULLY FEASIBLE** - Standard ViewModel update pattern, integrates with existing systems.

---

#### ✅ Alert Notifications System: FULLY FEASIBLE

**Approach:**

```csharp
public class AlertItemVM : ViewModel
{
    [DataSourceProperty]
    public string AlertText { get; set; }
    
    [DataSourceProperty]
    public string AlertIcon { get; set; }
    
    [DataSourceProperty]
    public string AlertColor { get; set; }
    
    [DataSourceProperty]
    public string AlertTime { get; set; }
}

public class WarRoomVM : ViewModel
{
    [DataSourceProperty]
    public MBBindingList<AlertItemVM> RecentAlerts { get; set; }
    
    public void AddAlert(string text, string icon, string color)
    {
        var alert = new AlertItemVM
        {
            AlertText = text,
            AlertIcon = icon,
            AlertColor = color,
            AlertTime = CampaignTime.Now.ToString()
        };
        
        RecentAlerts.Insert(0, alert);  // Add to top
        
        // Keep only last 10 alerts
        while (RecentAlerts.Count > 10)
            RecentAlerts.RemoveAt(RecentAlerts.Count - 1);
    }
}
```

**Integration Points:**

```csharp
// Subscribe to existing events
LanceLifeSimulationBehavior.Instance.OnMemberInjured += (member) =>
{
    WarRoomVM.Instance?.AddAlert($"{member.Name} was injured", "⚕️", "#CC0000FF");
};

AIScheduleBehavior.Instance.OnDutyAssigned += (member, duty) =>
{
    WarRoomVM.Instance?.AddAlert($"{member.Name} assigned to {duty.Name}", "📋", "#0066CCFF");
};
```

**Verdict:** ✅ **FULLY FEASIBLE** - Standard event subscription + list display pattern.

---

#### ⚠️ Interactive Camp Map View: MAJOR LIMITATION

**Original Phase 4 Vision:**

> "Interactive camp map view with clickable regions showing lance member positions"

**Technical Investigation:**

**Map Rendering in Native Game:**

From `SandBox.View\MapScene` and `TaleWorlds.Engine.Screens.SceneLayer.cs`:

```csharp
public class SceneLayer : ScreenLayer
{
    private SceneView _sceneView;
    
    public SceneLayer(bool clearSceneOnFinalize = true, bool autoToggleSceneView = true)
    {
        // ...
        this._sceneView = SceneView.CreateSceneView();
    }
    
    public void SetScene(Scene scene) => _sceneView.SetScene(scene);
    public void SetCamera(Camera camera) => _sceneView.SetCamera(camera);
    
    public Vec2 WorldPointToScreenPoint(Vec3 position)
    {
        return _sceneView.WorldPointToScreenPoint(position);
    }
}
```

**Key Findings:**

1. **Campaign map uses `SceneLayer`** - A 3D scene rendered in engine, NOT Gauntlet UI
2. **SceneLayer != GauntletLayer** - They are fundamentally different rendering systems
3. **No hybrid scene+UI layer in native code** - Map overlays (nameplates, etc.) are separate Gauntlet layers on top of SceneLayer

**What IS Possible:**

**Option A: Top-Down Camp Illustration (2D Sprite)**

```xml
<!-- WarRoomMapTab.xml -->
<Widget WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent">
  <Children>
    <!-- Static camp layout illustration -->
    <Widget Sprite="CampLayout/camp_overview_map" />
    
    <!-- Clickable hotspots overlaid on illustration -->
    <ButtonWidget Command.Click="ExecuteVisitMedicalTent" 
                  PositionXOffset="-200" PositionYOffset="150"
                  WidthSizePolicy="Fixed" HeightSizePolicy="Fixed"
                  SuggestedWidth="80" SuggestedHeight="80">
      <Children>
        <Widget Sprite="CampIcons/medical_tent_icon" />
        <TextWidget Text="@MedicalTentCount" />
      </Children>
    </ButtonWidget>
    
    <!-- Repeat for other locations -->
  </Children>
</Widget>
```

**Pros:**
- ✅ Fully achievable with Gauntlet UI
- ✅ Can show member counts per location
- ✅ Clickable regions work perfectly
- ✅ 2-3 days to implement

**Cons:**
- ❌ Not a "live" 3D map
- ❌ Static illustration, not actual camp scene
- ❌ Members shown as counts, not animated positions

**Verdict for Option A:** ✅ **FEASIBLE AND RECOMMENDED**

---

**Option B: Member List with Location Labels (Fallback)**

If creating a 2D camp illustration is too much art work:

```xml
<!-- Simple list view grouped by location -->
<ListPanel DataSource="{LocationGroups}">
  <ItemTemplate>
    <Widget>
      <Children>
        <TextWidget Text="@LocationName" />
        <ListPanel DataSource="{MembersHere}">
          <ItemTemplate>
            <Widget>
              <TextWidget Text="@MemberName" />
              <TextWidget Text="@CurrentActivity" />
            </Widget>
          </ItemTemplate>
        </ListPanel>
      </Children>
    </Widget>
  </ItemTemplate>
</ListPanel>
```

**Verdict for Option B:** ✅ **FULLY FEASIBLE** - 1 day to implement, but less impressive.

---

**Option C: ACTUAL 3D Camp Scene (NOT RECOMMENDED)**

**Why This Is Hard:**

1. **Requires SceneLayer + GauntletLayer hybrid** - No native examples of this pattern
2. **Needs 3D camp scene asset** - Would need to create custom scene with entities
3. **Camera control complexity** - Top-down camera in confined space
4. **Member position rendering** - Would need to spawn entities or markers in scene
5. **Performance concerns** - Running a 3D scene for UI is expensive

**Estimated Effort if Attempted:** 3-4 weeks (maybe more)  
**Risk:** **VERY HIGH** - No proven pattern in native code

**Verdict for Option C:** ❌ **NOT RECOMMENDED** - Too complex, no native examples

---

### Phase 4 Recommendation

**Status:** ⚠️ **PROCEED WITH MODIFIED SCOPE**

**What to Keep:**
- ✅ Multi-tab interface (Map, Activities, Lance, Alerts)
- ✅ Real-time population tracking
- ✅ Alert notifications
- ✅ Integration with AI schedule

**What to Modify:**
- ⚠️ **"Interactive camp map"** → **"Top-down camp illustration with clickable hotspots"**
- Use 2D sprite illustration instead of 3D scene
- Show member counts per location instead of animated positions
- Clickable regions navigate to location detail screens

**Revised Implementation Plan:**

**Week 1 (Days 1-5):**
- Multi-tab scaffold (TabControl widget)
- Alert system (event subscription + list display)
- Population tracking (query AI Schedule + Lance Life)
- Tab navigation and keyboard shortcuts

**Week 2 (Days 6-10):**
- Camp illustration design (2D sprite of camp layout)
- Hotspot placement and click handlers
- Member count badges per location
- Tab content integration (reuse existing screens)

**Week 3 (Days 11-15):**
- Polish and visual consistency
- Edge case handling
- Integration testing
- Performance optimization

**Technical Risk:** **MEDIUM → LOW** (with revised scope)  
**Effort Estimate:** **2-3 weeks** (still accurate with modified map approach)

---

## Summary Matrix

| Feature | Feasibility | API Support | Effort | Risk | Recommendation |
|---------|-------------|-------------|--------|------|----------------|
| **Phase 3: Themed Backgrounds** | ✅ Full | Excellent | 1-2 days | Low | ✅ Proceed as planned |
| **Phase 3: Dynamic Elements** | ✅ Full | Good | 2-3 days | Low | ✅ Proceed as planned |
| **Phase 3: Area Status** | ✅ Full | Excellent | 1 day | Low | ✅ Proceed as planned |
| **Phase 4: Multi-Tab UI** | ✅ Full | Excellent | 3-5 days | Low | ✅ Proceed as planned |
| **Phase 4: Real-Time Tracking** | ✅ Full | Good | 2-3 days | Low | ✅ Proceed as planned |
| **Phase 4: Alerts System** | ✅ Full | Good | 2-3 days | Low | ✅ Proceed as planned |
| **Phase 4: 2D Camp Map** | ✅ Full | Good | 3-4 days | Low | ✅ Use this approach |
| **Phase 4: 3D Scene Map** | ❌ Limited | Poor | 3-4 weeks | Very High | ❌ Do not attempt |

---

## Final Recommendations

### Phase 3: GREEN LIGHT ✅

**Proceed exactly as planned.** All technical requirements are supported by native APIs. The implementation patterns are well-established in the game's own UI code (Kingdom, Clan, Crafting screens all use similar techniques).

**No changes needed to roadmap.**

---

### Phase 4: YELLOW LIGHT ⚠️ (Modify Map Concept)

**Proceed with modified "interactive map" scope:**

**Replace:** "3D interactive camp scene with live member positions"  
**With:** "2D illustrated camp layout with clickable location hotspots showing member counts"

**Why This Is Better:**
1. ✅ **Achievable** within 2-3 week timeline
2. ✅ **Low risk** - proven pattern with Gauntlet UI
3. ✅ **Still impressive** - gives tactical overview feeling
4. ✅ **Functional** - provides all the information player needs
5. ✅ **Maintainable** - uses standard UI patterns

**Everything else in Phase 4 is fully supported and feasible.**

---

## Code Examples to Reference

From native decompile, these files provide patterns you can follow:

**For Phase 3 (Themed Backgrounds):**
- `SandBox.GauntletUI\GauntletKingdomScreen.cs` - Loading sprite categories
- `TaleWorlds.GauntletUI\BaseTypes\BrushWidget.cs` - Custom brushes
- Kingdom/Clan/Crafting screen XMLs (in game files) - Background layering

**For Phase 4 (Multi-Tab Interface):**
- `TaleWorlds.GauntletUI\BaseTypes\TabControl.cs` - Tab control implementation
- `SandBox.GauntletUI\GauntletKingdomScreen.cs` - Tab switching keyboard shortcuts
- Kingdom screen (game files) - Multi-tab layout patterns

**For Phase 4 (Alerts/Notifications):**
- Any screen with `MBBindingList<T>` + `ListPanel` - Standard list pattern
- Event subscription patterns from existing behaviors

---

## Questions & Answers

**Q: Can we do animated backgrounds?**  
A: Yes, via sprite sheets or frame sequences. Performance impact should be tested.

**Q: Can we show actual member avatars on the map?**  
A: On 2D illustration, yes - as sprites positioned by code. In 3D scene, no - too complex.

**Q: Can the map be interactive (drag, zoom)?**  
A: Drag: Not natively supported in Gauntlet, would need custom widget. Zoom: Possible but not recommended for Phase 4 scope. Keep it simple with fixed view and clickable hotspots.

**Q: Can we reuse the campaign map?**  
A: No - campaign map is a `SceneLayer` with 3D terrain. Cannot embed in Gauntlet UI without major custom work.

**Q: Should we build Phase 3 before Phase 4?**  
A: Yes, absolutely. Phase 3 provides foundation and visual language for Phase 4. Also, Phase 3 is low-risk and will give you confidence with the UI system before tackling Phase 4's complexity.

---

## Conclusion

Both phases are **technically feasible**, but Phase 4 requires a **scope adjustment** for the "interactive map" feature. The revised approach (2D illustrated layout with hotspots) is more appropriate for the Gauntlet UI framework and achievable within the planned timeline.

**No API blockers were found.** All required functionality is supported by native Bannerlord APIs and has proven usage patterns in the game's existing UI code.

**Proceed with confidence on Phase 3. Proceed with revised map concept on Phase 4.**

