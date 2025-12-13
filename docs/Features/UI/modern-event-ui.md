# Modern Event UI System

## Overview

This system replaces basic inquiry popups with **stunning custom Gauntlet screens** that provide a rich, immersive experience for Lance Life events. Instead of plain text popups, players see:

- **Full-screen event cards** with modern design
- **Character portraits** or scene images
- **Visual choice buttons** with icons and hover effects
- **Live escalation track displays** showing Heat, Discipline, and Lance Rep
- **Rich text formatting** with colors and styling
- **Smooth animations** and transitions

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
- **Inline icons** (🪙 for gold, ⚠ for warnings)
- **Paragraph spacing** for readability
- **Scrollable story text** for long narratives

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

### Configuration

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

## Visual Customization

### Category Colors

Events are color-coded by category:

| Category | Color | Hex |
|----------|-------|-----|
| Duty | Blue | `#4488FF` |
| Training | Green | `#44FF88` |
| Social | Purple | `#FF88FF` |
| Escalation | Red | `#FF4444` |
| Task | Orange | `#FFAA44` |
| Default | Gray | `#888888` |

### Risk Colors

Choice buttons have colored accent bars based on risk:

| Risk Level | Color | Hex |
|------------|-------|-----|
| Safe | Green | `#44AA44` |
| Low Risk | Yellow | `#FFDD44` |
| Medium Risk | Orange | `#FFAA33` |
| High Risk | Red | `#DD3333` |

### Icons

Choice buttons automatically select appropriate icons:

| Icon | Used For | Path |
|------|----------|------|
| ✓ Check | Safe choices (>90% success) | `General\\Icons\\icon_check` |
| ⚠ Warning | Risky choices (<70% success) | `General\\Icons\\icon_warning` |
| 🪙 Coin | Choices with gold cost/reward | `General\\Icons\\icon_coin` |
| ⭐ XP | Choices granting skill XP | `General\\Icons\\icon_experience` |
| → Arrow | Default/neutral choices | `General\\Icons\\icon_arrow_right` |

## Scene Images

The system automatically selects scene backgrounds based on event category:

| Category | Background |
|----------|------------|
| Training | Training field |
| Task | Camp scene |
| Escalation | Conflict scene |
| Social | Fire circle / tavern |
| Duty | Military camp |
| Default | Generic camp |

### Custom Scene Images

Add custom backgrounds in your event definition:

```json
{
    "id": "custom_event",
    "scene_image": "SPGeneral\\MapBar\\custom_scene",
    "show_character": false
}
```

Or force character display:

```json
{
    "id": "interpersonal_event",
    "show_character": true,
    "character_type": "lance_leader"
}
```

## Character Display

### Automatic Character Selection

The system automatically shows relevant characters:

| Event Category | Character Shown |
|----------------|-----------------|
| Duty | Lance leader |
| Onboarding | Lance leader |
| Social | Random lance mate |
| Escalation | Authority figure |
| Training | Training instructor |

### Character Stances

Characters are shown in appropriate stances:

- **Stance 0**: Casual/conversational
- **Stance 1**: Formal/military
- **Stance 2**: Aggressive/warning
- **Stance 3**: Friendly/welcoming

Configure stance in event definition:

```json
{
    "id": "event_id",
    "character_stance": 1
}
```

## Escalation Track Display

The bottom status bar shows live escalation values:

### Heat (Red Bar)
- Range: 0-10
- Thresholds: 5 (Shakedown), 7 (Caught), 9 (Court-martial)
- Color: `#FF6644`

### Discipline (Blue Bar)
- Range: 0-10
- Thresholds: 4 (Warning), 7 (Flogging), 9 (Discharge)
- Color: `#4488FF`

### Lance Reputation (Green Bar)
- Range: -10 to +10
- Displayed normalized: 0-180px width
- Color: `#44FF88`

### Fatigue
- Displayed as: `Current / Max`
- Updates after each choice
- Warning when approaching max

## Advanced Customization

### Custom Prefab Modifications

Edit `GUI/Prefabs/Events/LanceLifeEventScreen.xml` to customize:

- **Layout dimensions**: Change `SuggestedWidth`/`SuggestedHeight`
- **Colors**: Modify `Color` attributes
- **Fonts**: Change `Brush.FontSize` values
- **Spacing**: Adjust `Margin*` properties

### Custom Choice Button Styles

Edit `GUI/Prefabs/Events/EventChoiceButton.xml` to:

- Change button heights
- Modify hover effects
- Adjust icon sizes
- Customize text positioning

### Animation Effects

Add animation states in the prefab:

```xml
<BrushWidget Brush="Custom.Animated.Brush" DoNotPassEventsToChildren="true">
    <Animation Name="FadeIn" Duration="0.3" />
</BrushWidget>
```

## Performance

### Optimization Features

1. **Lazy loading**: Character tableaus only loaded when needed
2. **Texture pooling**: Scene images cached and reused
3. **Efficient layout**: Fixed-size widgets prevent reflow
4. **Smart updates**: Only changed properties trigger re-renders

### Best Practices

- Keep story text under 500 characters for single-screen display
- Limit choices to 4-6 for optimal visual presentation
- Use character display sparingly (more expensive than images)
- Cache repeated event definitions

## Troubleshooting

### Screen Not Appearing

**Check:**
1. Is `Campaign.Current` null? Screen requires active campaign
2. Is another event already showing? Only one at a time
3. Is the prefab XML valid? Check console for parse errors
4. Are required assets missing? Verify sprite paths

**Solutions:**
```csharp
// Add debug logging
ModLogger.Info("EventUI", $"Attempting to show event: {eventDef.Id}");

// Check if screen opens
if (!ModernEventPresenter.TryShow(evt, enlistment))
{
    ModLogger.Warn("EventUI", "Failed to show modern UI, using fallback");
    LanceLifeEventInquiryPresenter.TryShow(evt, enlistment);
}
```

### Character Not Rendering

**Check:**
1. Is `ShowCharacter` true in ViewModel?
2. Does the character have valid `BodyProperties`?
3. Is `EquipmentCode` properly calculated?
4. Is the character's ID valid?

**Debug:**
```csharp
ModLogger.Debug("EventUI", $"Character: {hero.Name}, Valid: {hero != null}");
ModLogger.Debug("EventUI", $"BodyProps: {hero?.BodyProperties != null}");
```

### Incorrect Colors/Styling

**Check:**
1. Are hex colors properly formatted? (e.g., `#RRGGBB` or `#RRGGBBAA`)
2. Are brush names valid? Check against `Brushes.xml` in native assets
3. Are sprites loading? Verify paths in console

**Fix:**
```csharp
// Use safe color defaults
var color = GetCategoryColor(category) ?? "#888888";
```

### Choices Not Responding

**Check:**
1. Is `IsEnabled` true in ViewModel?
2. Is the button's `Command.Click` bound correctly?
3. Are input restrictions properly set?

**Debug:**
```csharp
ModLogger.Debug("EventUI", $"Choice enabled: {choice.IsEnabled}, Available: {!choice.IsDisabled}");
```

## Migration Guide

### From Basic Inquiries

**Step 1**: Replace presenter calls
```csharp
// Old
LanceLifeEventInquiryPresenter.TryShow(evt, enlistment);

// New
ModernEventPresenter.TryShow(evt, enlistment);
```

**Step 2**: Update event definitions (optional)
```json
{
    "id": "event_id",
    "use_modern_ui": true,
    "scene_image": "path/to/background",
    "character_stance": 0
}
```

**Step 3**: Test with fallback
```csharp
ModernEventPresenter.TryShowWithFallback(evt, enlistment, useModernUI: true);
```

### Gradual Rollout

Enable modern UI for specific categories:

```csharp
public static bool ShouldUseModernUI(LanceLifeEventDefinition evt)
{
    var modernCategories = new[] { "duty", "escalation", "training" };
    return modernCategories.Contains(evt.Category?.ToLowerInvariant());
}

// Usage
if (ShouldUseModernUI(evt))
    ModernEventPresenter.TryShow(evt, enlistment);
else
    LanceLifeEventInquiryPresenter.TryShow(evt, enlistment);
```

## Future Enhancements

### Planned Features

1. **Animated transitions** between choices and outcomes
2. **Sound effects** for choice selection and outcome reveals
3. **Particle effects** for dramatic moments (escalation thresholds)
4. **Dynamic backgrounds** that change based on time of day
5. **Split-screen comparisons** for "before and after" choices
6. **Mini-cutscenes** for major story moments
7. **Achievement notifications** integrated into event screens

### Experimental Features

Enable in configuration:
```json
{
    "experimental_ui": {
        "animated_backgrounds": true,
        "character_animations": true,
        "particle_effects": false,
        "sound_effects": true
    }
}
```

## Examples

### Complete Event Flow

```csharp
// 1. Load event from JSON
var evt = LanceLifeStoryPackLoader.GetEvent("duty_scout_recon");

// 2. Check conditions
if (IsEventEligible(evt))
{
    // 3. Show modern UI
    ModernEventPresenter.TryShow(evt, enlistment);
    
    // 4. Effects applied automatically on choice selection
    // 5. Screen closes, game resumes
}
```

### Custom Event Screen

```csharp
// Create custom event programmatically
var customEvent = new LanceLifeEventDefinition
{
    Id = "special_cutscene",
    Category = "story",
    TitleFallback = "A Fateful Decision",
    SetupFallback = "The battle is lost. Your lord lies wounded...",
    Options = new List<LanceLifeEventOptionDefinition>
    {
        new LanceLifeEventOptionDefinition
        {
            TextFallback = "Stand and fight to the death",
            Risk = "risky",
            Effects = new LanceLifeEventEffects { LanceReputation = 5 }
        },
        new LanceLifeEventOptionDefinition
        {
            TextFallback = "Retreat with survivors",
            Risk = "safe",
            Effects = new LanceLifeEventEffects { LanceReputation = -2 }
        }
    }
};

// Show it
LanceLifeEventScreen.Open(customEvent, enlistment);
```

## Comparison: Old vs New

### Basic Inquiry (Old)
```
┌─────────────────────────┐
│ Event Title             │
├─────────────────────────┤
│ Plain text story...     │
│                         │
│ ○ Choice 1              │
│ ○ Choice 2              │
└─────────────────────────┘
```

### Modern Screen (New)
```
╔═══════════════════════════════════════════════════════════╗
║  [DUTY]         The Suspicious Merchant          Evening  ║
╠═══════════════╦═══════════════════════════════════════════╣
║               ║  A merchant approaches privately...       ║
║  [Character]  ║                                           ║
║   Portrait    ║  "I have a proposition. 50 denars for    ║
║     or        ║   your discretion..."                     ║
║   Scene       ║                                           ║
║   Image       ║  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  ║
║               ║  — Choose Your Action —                   ║
║  [Lance       ║                                           ║
║   Leader]     ║  [✓] Accept bribe      +50🪙 | +2 Heat   ║
║               ║  [⚠] Report superior   +Honor | -rel     ║
║               ║  [→] Decline politely  No effects        ║
║               ║  [!] Threaten (-50🪙)  +Intimidate [⚠]   ║
╠═══════════════╩═══════════════════════════════════════════╣
║ Heat ▓▓▓▓░░░░░░ 4/10  │  Discipline ▓▓▓▓▓░░░░░ 5/10  │ 7/10 ║
╚═══════════════════════════════════════════════════════════╝
```

## Conclusion

The modern event UI system provides a **dramatically improved visual experience** while maintaining full compatibility with existing event JSON definitions. Players get:

- **Better immersion** through visual storytelling
- **Clearer choices** with visual cost/reward indicators
- **Live feedback** on escalation tracks
- **Professional presentation** matching AAA game standards

Simply replace `LanceLifeEventInquiryPresenter.TryShow()` with `ModernEventPresenter.TryShow()` to upgrade your events!

---

**Files:**
- `GUI/Prefabs/Events/LanceLifeEventScreen.xml` - Main screen layout
- `GUI/Prefabs/Events/EventChoiceButton.xml` - Choice button template
- `src/Features/Lances/UI/LanceLifeEventVM.cs` - Main ViewModel
- `src/Features/Lances/UI/EventChoiceVM.cs` - Choice ViewModel
- `src/Features/Lances/UI/LanceLifeEventScreen.cs` - Screen class
- `src/Features/Lances/UI/ModernEventPresenter.cs` - Integration helper
