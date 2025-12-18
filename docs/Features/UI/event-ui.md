# Event UI System

**Status:** ✅ IMPLEMENTED  
**Files:** `src/Features/Lances/UI/LanceLifeEventScreen.cs`, `src/Features/Lances/UI/ModernEventPresenter.cs`

## Index

- [Overview](#overview)
- [Visual Features](#visual-features)
- [Usage](#usage)
- [Configuration](#configuration)
- [Advanced Visual Effects](#advanced-visual-effects)
  - [Animated Transitions](#animated-transitions)
  - [Hover Effects](#hover-effects)
  - [Particle Systems](#particle-systems)
  - [Character Portraits](#character-portraits)
  - [Cinematic Effects](#cinematic-effects)
  - [Environmental Effects](#environmental-effects)
- [Technical Details](#technical-details)
- [Performance Optimization](#performance-optimization)

---

## Overview

This system replaces basic inquiry popups with **stunning custom Gauntlet screens** that provide a rich, immersive experience for Lance Life events. Instead of plain text popups, players see:

- **Full-screen event cards** with modern design
- **Character portraits** or scene images
- **Visual choice buttons** with icons and hover effects
- **Live escalation track displays** showing Heat, Discipline, and Lance Rep
- **Rich text formatting** with colors and styling
- **Smooth animations** and transitions

---

## Visual Features

### 1. Modern Card Layout
- **Semi-transparent overlay** with blur effect
- **Centered event card** (1100x750px) with rounded corners
- **Decorative header bar** with category badge and title
- **Professional typography** with size hierarchy

### 2. Scene Visualization
- **Character portraits** for interpersonal events (3D character tableau)
- **Scene images** for situational events (training, camp, etc.)
- **Character name plates** with semi-transparent backgrounds
- **Dynamic character selection** (lance leader, companions, etc.)

### 3. Choice Presentation
- **Card-style buttons** with colored accent bars
- **Risk indicators** (green for safe, yellow/orange/red for risky)
- **Icons** for each choice type (shield, warning, coin, XP, etc.)
- **Live cost/reward display** in button corners
- **Disabled state overlays** with clear reason text

### 4. Escalation Tracking
- **Progress bars** for Heat, Discipline, and Lance Rep
- **Color-coded bars** (red for heat, blue for discipline, green for rep)
- **Live values** updating after each choice
- **Visual thresholds** showing dangerous levels

### 5. Rich Text Formatting
- **Colored text** for emphasis (gold for rewards, red for costs)
- **Inline markers** (text labels for gold/warnings)
- **Paragraph spacing** for readability
- **Scrollable story text** for long narratives

---

## Usage

### Replace Inquiry Popups

**Old way (basic popup):**
```csharp
LanceLifeEventInquiryPresenter.TryShow(eventDef, enlistment);
```

**New way (modern UI):**
```csharp
ModernEventPresenter.TryShow(eventDef, enlistment);
```

**Safe way (with fallback):**
```csharp
// Tries modern UI first, falls back to inquiry if it fails
ModernEventPresenter.TryShowWithFallback(eventDef, enlistment, useModernUI: true);
```

### Direct Screen Opening

```csharp
// Open the screen directly from anywhere
LanceLifeEventScreen.Open(eventDef, enlistment, onClosed: () =>
{
    // Called when the screen closes
    Console.WriteLine("Event completed!");
});
```

---

## Configuration

Enable/disable modern UI in your configuration:

```json
{
    "lance_life": {
        "enabled": true,
        "use_modern_ui": true,
        "fallback_to_inquiry": true
    }
}
```

### Visual Customization

#### Category Colors

Events are color-coded by category:

| Category | Color | Hex |
|----------|-------|-----|
| Duty | Blue | `#4488FF` |
| Social | Green | `#44FF88` |
| Combat | Red | `#FF4444` |
| Training | Yellow | `#FFAA44` |
| Lance | Purple | `#AA44FF` |
| Discipline | Orange | `#FF8844` |

#### Risk Indicators

Choices display colored accents based on risk:

| Risk Level | Color | Hex |
|------------|-------|-----|
| Safe | Green | `#44FF44` |
| Minor Risk | Yellow | `#FFFF44` |
| Moderate Risk | Orange | `#FFAA44` |
| High Risk | Red | `#FF4444` |

---

## Advanced Visual Effects

Beyond the modern card layout, you can add **stunning visual effects** to make your events even more immersive and engaging.

### Animated Transitions

#### Fade-In Animation

Add smooth fade-in when the screen appears:

```xml
<BrushWidget Id="EventCard" WidthSizePolicy="Fixed" HeightSizePolicy="Fixed" 
             SuggestedWidth="1100" SuggestedHeight="750" 
             AlphaFactor="0">
    <Children>
        <!-- Card contents -->
    </Children>
    <!-- Fade in over 0.3 seconds -->
    <AnimationState Name="FadeIn">
        <AnimationTrack Parameter="AlphaFactor" StartKey="0" EndKey="1" Duration="0.3" />
    </AnimationState>
</BrushWidget>
```

#### Slide-In Animation

Make the card slide up from the bottom:

```xml
<BrushWidget Id="EventCard" PositionYOffset="200">
    <AnimationState Name="SlideUp">
        <AnimationTrack Parameter="PositionYOffset" StartKey="200" EndKey="0" Duration="0.4" Easing="QuadOut" />
        <AnimationTrack Parameter="AlphaFactor" StartKey="0" EndKey="1" Duration="0.4" />
    </AnimationState>
</BrushWidget>
```

#### Staggered Choice Buttons

Animate choices appearing one by one:

```xml
<ButtonWidget DelayedAlphaFactor="0" DelayTime="@DelayTime">
    <AnimationState Name="StaggerIn">
        <AnimationTrack Parameter="AlphaFactor" StartKey="0" EndKey="1" Duration="0.2" />
        <AnimationTrack Parameter="PositionXOffset" StartKey="-50" EndKey="0" Duration="0.2" />
    </AnimationState>
</ButtonWidget>
```

In ViewModel:
```csharp
// Set staggered delay for each choice
choice.DelayTime = index * 0.1f; // 100ms between each
```

### Hover Effects

#### Glowing Buttons

Add glow effect on hover:

```xml
<ButtonWidget UpdateChildrenStates="true">
    <Children>
        <BrushWidget WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent">
            <VisualState State="Default">
                <Animation Name="Default">
                    <AnimationTrack Parameter="Color" Value="#FFFFFF" />
                </Animation>
            </VisualState>
            <VisualState State="Hovered">
                <Animation Name="Hover">
                    <AnimationTrack Parameter="Color" Value="#FFFFAA" Duration="0.15" />
                    <AnimationTrack Parameter="Brightness" StartKey="1.0" EndKey="1.3" Duration="0.15" />
                </Animation>
            </VisualState>
        </BrushWidget>
    </Children>
</ButtonWidget>
```

#### Scale on Hover

Make buttons slightly larger when hovered:

```xml
<ButtonWidget ScaleFactor="1.0">
    <VisualState State="Hovered">
        <Animation Name="ScaleUp">
            <AnimationTrack Parameter="ScaleFactor" StartKey="1.0" EndKey="1.05" Duration="0.1" />
        </Animation>
    </VisualState>
</ButtonWidget>
```

#### Shadow Effect

Add dynamic shadow to buttons:

```xml
<BrushWidget WidthSizePolicy="Fixed" HeightSizePolicy="Fixed" 
             SuggestedWidth="350" SuggestedHeight="80"
             Brush="Shadow.Blur" Color="#00000080" 
             PositionXOffset="0" PositionYOffset="5">
    <VisualState State="Default">
        <Animation Name="Default">
            <AnimationTrack Parameter="AlphaFactor" Value="0.3" />
        </Animation>
    </VisualState>
    <VisualState State="Hovered">
        <Animation Name="HoverShadow">
            <AnimationTrack Parameter="AlphaFactor" StartKey="0.3" EndKey="0.6" Duration="0.15" />
            <AnimationTrack Parameter="PositionYOffset" StartKey="5" EndKey="8" Duration="0.15" />
        </Animation>
    </VisualState>
</BrushWidget>
```

### Particle Systems

#### Background Particle Effect

Add floating particles for atmospheric effect:

```xml
<ParticleWidget WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent">
    <ParticleSystem>
        <Emitter Rate="20" Lifetime="5.0" ParticleLifetime="8.0">
            <Particle Sprite="General\circle_glow" 
                      Size="10" 
                      Color="#FFFFFF40"
                      VelocityX="-20~20" 
                      VelocityY="-50~-30"
                      FadeIn="1.0" 
                      FadeOut="2.0" />
        </Emitter>
    </ParticleSystem>
</ParticleWidget>
```

**Note:** Bannerlord doesn't have native particle widgets, so this would require custom implementation via `Widget` extensions.

#### Choice Selection Effect

Burst of particles when clicking a choice:

```csharp
// In choice button click handler
public void OnChoiceSelected(int choiceIndex)
{
    // Trigger particle burst at button position
    PlayParticleBurst(buttonPosition, count: 30, color: choiceColor);
    
    // Then process choice as normal
    ProcessChoice(choiceIndex);
}
```

### Character Portraits

#### 3D Character Tableau

Display character portraits using Bannerlord's tableau system:

```csharp
public class LanceLifeEventVM : ViewModel
{
    [DataSourceProperty]
    public ImageIdentifierVM CharacterPortrait { get; set; }
    
    private void SetCharacterPortrait(Hero hero)
    {
        var charCode = CampaignUIHelper.GetCharacterCode(hero.CharacterObject);
        CharacterPortrait = new ImageIdentifierVM(charCode);
    }
}
```

In XML:
```xml
<ImageWidget WidthSizePolicy="Fixed" HeightSizePolicy="Fixed" 
             SuggestedWidth="400" SuggestedHeight="400"
             ImageId="@CharacterPortrait"
             ImageTypeCode="0" />
```

#### Portrait with Nameplate

Add character name below portrait:

```xml
<Widget WidthSizePolicy="Fixed" HeightSizePolicy="CoverChildren" 
        SuggestedWidth="400">
    <Children>
        <!-- Portrait -->
        <ImageWidget ImageId="@CharacterPortrait" 
                     SuggestedWidth="400" SuggestedHeight="400" />
        
        <!-- Nameplate -->
        <BrushWidget WidthSizePolicy="StretchToParent" HeightSizePolicy="Fixed" 
                     SuggestedHeight="60" VerticalAlignment="Bottom"
                     Brush="Header.Background.Dark" AlphaFactor="0.8">
            <Children>
                <TextWidget WidthSizePolicy="StretchToParent" 
                           HeightSizePolicy="StretchToParent"
                           Text="@CharacterName" 
                           Brush="Header.Text.Large"
                           HorizontalAlignment="Center" 
                           VerticalAlignment="Center" />
            </Children>
        </BrushWidget>
    </Children>
</Widget>
```

### Cinematic Effects

#### Vignette Effect

Add edge darkening for focus:

```xml
<BrushWidget WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent"
             Brush="Vignette.Radial" Color="#00000080" IsEnabled="false" />
```

**Note:** You may need to create a custom brush with radial gradient.

#### Depth of Field (Blur Background)

Blur the background behind the event card:

```xml
<BrushWidget Id="BackgroundBlur" 
             WidthSizePolicy="StretchToParent" 
             HeightSizePolicy="StretchToParent"
             Brush="FullscreenBlur" 
             AlphaFactor="0.7" />
```

#### Camera Shake (Code-Side)

Add subtle camera shake for dramatic moments:

```csharp
private void PlayCameraShake(float intensity, float duration)
{
    // Simple camera shake using widget position offset
    float elapsed = 0f;
    while (elapsed < duration)
    {
        float shake = intensity * (1f - elapsed / duration);
        RootWidget.PositionXOffset = MBRandom.RandomFloatRanged(-shake, shake);
        RootWidget.PositionYOffset = MBRandom.RandomFloatRanged(-shake, shake);
        
        elapsed += Time.DeltaTime;
        yield return null;
    }
    
    // Reset position
    RootWidget.PositionXOffset = 0;
    RootWidget.PositionYOffset = 0;
}
```

### Environmental Effects

#### Weather Overlay

Add rain/snow overlay for outdoor events:

```xml
<Widget Id="WeatherOverlay" IsVisible="@IsOutdoorEvent">
    <Children>
        <!-- Rain streaks -->
        <BrushWidget WidthSizePolicy="StretchToParent" 
                     HeightSizePolicy="StretchToParent"
                     Brush="Weather.Rain" 
                     AlphaFactor="0.3"
                     PositionYOffset="@RainOffset">
            <AnimationState Name="RainFall" Loop="true">
                <AnimationTrack Parameter="PositionYOffset" 
                               StartKey="-1080" EndKey="0" 
                               Duration="1.0" />
            </AnimationState>
        </BrushWidget>
    </Children>
</Widget>
```

#### Time of Day Tint

Adjust card color based on time of day:

```csharp
private Color GetTimeOfDayTint()
{
    var hour = CampaignTime.Now.GetHourOfDay;
    
    if (hour >= 6 && hour < 12)
        return new Color(1.0f, 1.0f, 0.9f); // Morning (warm)
    else if (hour >= 12 && hour < 18)
        return new Color(1.0f, 1.0f, 1.0f); // Day (neutral)
    else if (hour >= 18 && hour < 20)
        return new Color(1.0f, 0.8f, 0.6f); // Evening (orange)
    else
        return new Color(0.6f, 0.7f, 0.9f); // Night (blue)
}
```

Apply in XML:
```xml
<BrushWidget Color="@TimeOfDayTint" AlphaFactor="0.2" />
```

#### Battle Smoke Effect

Add smoke/dust for combat events:

```xml
<Widget IsVisible="@IsCombatEvent">
    <Children>
        <BrushWidget WidthSizePolicy="StretchToParent" 
                     HeightSizePolicy="StretchToParent"
                     Brush="Effects.Smoke" 
                     AlphaFactor="0.15"
                     PositionXOffset="@SmokeOffset">
            <AnimationState Name="SmokeDrift" Loop="true">
                <AnimationTrack Parameter="PositionXOffset" 
                               StartKey="-100" EndKey="100" 
                               Duration="8.0" />
                <AnimationTrack Parameter="AlphaFactor" 
                               StartKey="0.05" EndKey="0.25" 
                               Duration="4.0" Loop="true" />
            </AnimationState>
        </BrushWidget>
    </Children>
</Widget>
```

---

## Technical Details

### File Structure

```
src/Features/Lances/UI/
├── LanceLifeEventScreen.cs         [Main Gauntlet screen]
├── ModernEventPresenter.cs         [Presentation logic]
├── LanceLifeEventVM.cs             [ViewModel]
└── LanceLifeEventScreenWidget.xml  [UI layout]
```

### Screen Lifecycle

1. **Opening:**
   - `LanceLifeEventScreen.Open()` called
   - Naval visual crash guard activated
   - Screen pushed to ScreenManager
   - GauntletLayer and Movie loaded
   - ViewModel initialized with event data

2. **Display:**
   - User sees event card with choices
   - Escalation tracks show current values
   - Choices display costs/rewards

3. **Choice Selection:**
   - User clicks a choice button
   - ViewModel validates choice (fatigue, conditions)
   - Choice consequence applied
   - Escalation tracks update

4. **Closing:**
   - Screen fades out
   - GauntletLayer and Movie released
   - Screen popped from ScreenManager
   - Naval visual crash guard deactivated
   - Callback invoked

### Naval DLC Crash Guard

The screen includes a thread-safe crash guard for the Naval DLC:

```csharp
public static bool IsNavalVisualCrashGuardActive => 
    Volatile.Read(ref _navalVisualCrashGuardState) != 0;
```

This prevents crashes when background threads update party visuals while the screen is opening.

### Sprite Categories

The screen explicitly loads required sprite categories:

```csharp
_townManagementSpriteCategory = UIResourceManager.SpriteData.SpriteCategories["ui_townmanagement"];
_encyclopediaSpriteCategory = UIResourceManager.SpriteData.SpriteCategories["ui_encyclopedia"];
```

This ensures divider sprites and UI elements render correctly.

---

## Performance Optimization

### Efficient Rendering

**1. Lazy Loading:**
```csharp
// Only load character portraits when needed
if (_dataSource.ShowCharacterPortrait)
{
    _dataSource.LoadCharacterPortrait(hero);
}
```

**2. Minimize Redraws:**
```csharp
// Batch property updates
_dataSource.BeginUpdate();
_dataSource.Heat = newHeat;
_dataSource.Discipline = newDiscipline;
_dataSource.LanceRep = newRep;
_dataSource.EndUpdate();
```

**3. Disable Hidden Widgets:**
```xml
<Widget IsVisible="@ShowAdvancedEffects" IsEnabled="@ShowAdvancedEffects">
    <!-- Expensive effects only when visible -->
</Widget>
```

### Animation Performance

**1. Use Hardware Acceleration:**
```xml
<!-- Prefer AlphaFactor over Color changes -->
<AnimationTrack Parameter="AlphaFactor" /> <!-- Fast -->
<!-- NOT: -->
<AnimationTrack Parameter="Color" /> <!-- Slower -->
```

**2. Limit Particle Count:**
```csharp
// Scale particle count based on performance setting
int particleCount = GraphicsSettings.High ? 50 : 20;
```

**3. Reduce Animation Complexity:**
```xml
<!-- Simple linear animations perform better -->
<AnimationTrack Easing="Linear" /> <!-- Fast -->
<!-- NOT: -->
<AnimationTrack Easing="ElasticOut" /> <!-- Slower -->
```

### Memory Management

**1. Dispose Resources:**
```csharp
protected override void OnFinalize()
{
    _dataSource?.OnFinalize();
    _gauntletLayer = null;
    _townManagementSpriteCategory = null;
    _encyclopediaSpriteCategory = null;
    
    base.OnFinalize();
}
```

**2. Reuse ViewModels:**
```csharp
// Pool ViewModels for repeated events
private static readonly ObjectPool<LanceLifeEventVM> _vmPool = 
    new ObjectPool<LanceLifeEventVM>(() => new LanceLifeEventVM());
```

**3. Avoid Memory Leaks:**
```csharp
// Always unsubscribe from events
_dataSource.PropertyChanged -= OnPropertyChanged;
```

---

## Best Practices

### Visual Design

1. **Keep it readable:** High contrast text, clear fonts, appropriate sizes
2. **Be consistent:** Use the same colors/styles for same element types
3. **Don't overdo it:** Too many effects can be distracting
4. **Test on different resolutions:** Ensure UI scales correctly
5. **Accessibility:** Provide text alternatives for visual indicators

### Performance

1. **Profile first:** Measure before optimizing
2. **Start simple:** Add effects incrementally
3. **Provide options:** Let players disable heavy effects
4. **Test on low-end:** Ensure performance on minimum spec

### Code Quality

1. **Comment complex animations:** Explain what and why
2. **Use descriptive names:** `FadeInCardAnimation` not `Anim1`
3. **Extract reusable components:** Don't duplicate animation XML
4. **Handle errors gracefully:** Fallback if effects fail to load

---

## Related Documentation

- **[Menu Interface](menu-interface.md)** - Text menu system
- **[News Dispatches](news-dispatches.md)** - News feed integration
- **[Gauntlet UI Playbook](../../research/gauntlet-ui-screens-playbook.md)** - Technical guide for building Gauntlet UI

---

## Acceptance Criteria

### Basic Functionality
- [x] Event cards display with modern styling
- [x] Character portraits render correctly
- [x] Choices display with costs/rewards
- [x] Escalation tracks update in real-time
- [x] Screen closes properly after choice

### Visual Polish
- [x] Fade-in animation on open
- [x] Hover effects on buttons
- [x] Risk indicators color-coded
- [x] Category badges with colors
- [x] Smooth transitions throughout

### Performance
- [x] No frame drops on event open
- [x] Animations run smoothly (60 FPS)
- [x] Memory properly released on close
- [x] Works on minimum spec hardware

### Stability
- [x] Naval DLC crash guard functional
- [x] Sprite categories load correctly
- [x] No crashes on repeated opens
- [x] Fallback to inquiry if screen fails

---

**Document Maintained By:** Enlisted Development Team  
**Last Updated:** December 18, 2025  
**Status:** Active - Implementation Complete

