# Event Notification Window

**Summary:** Non-blocking side notification window for events that appears on the right side of the screen with interactive options and 20-second auto-dismiss functionality.

**Status:** 📋 Planning  
**Last Updated:** 2026-01-14  
**Related Docs:** [Content System Architecture](../Features/Content/content-system-architecture.md), [Event Delivery Manager](../../src/Features/Content/EventDeliveryManager.cs), [Enlisted Combat Log](../Features/UI/enlisted-combat-log.md)

---

## Executive Summary

Currently, events are delivered via modal `MultiSelectionInquiryData` popups that block gameplay and require immediate player response. This feature replaces them with a **side notification window** that:

1. **Appears on the right side** of the screen (non-blocking)
2. **Shows event info + options** in a compact, native-styled popup
3. **Auto-dismisses after 20 seconds** if player doesn't interact
4. **Allows gameplay to continue** while notification is visible
5. **Queues multiple events** if they arrive simultaneously

---

## Index

1. [Current System Limitations](#current-system-limitations)
2. [Design Goals](#design-goals)
3. [Visual Design](#visual-design)
4. [Interaction Model](#interaction-model)
5. [Technical Architecture](#technical-architecture)
6. [Implementation Tasks](#implementation-tasks)

---

## Current System Limitations

### How Events Work Today

Events are delivered via `EventDeliveryManager.ShowEventPopup()`:

```csharp
var inquiry = new MultiSelectionInquiryData(
    titleText: titleText,
    descriptionText: descriptionText,
    inquiryElements: options,
    isExitShown: false, // Force player to choose
    ...
);
MBInformationManager.ShowMultiSelectionInquiry(inquiry, true);
```

**Problems:**
- ❌ **Blocks gameplay** - Modal dialog, must respond immediately
- ❌ **Center screen** - Interrupts visual focus
- ❌ **No timeout** - Must click to dismiss (can't ignore)
- ❌ **Breaks flow** - Jarring interruption during travel/combat prep
- ❌ **No queuing control** - Multiple events spam popups

### Pain Points

1. **During travel:** Event pops up while watching map movement → breaks immersion
2. **During menus:** Event pops up while in QM/baggage → forces exit
3. **Event spam:** Multiple order events fire rapid succession → player fatigue
4. **No urgency control:** Minor events feel as intrusive as critical ones

---

## Design Goals

### Core Principles

1. **Non-intrusive** - Gameplay continues while notification is visible
2. **Player agency** - Can engage or ignore based on priority
3. **Visual hierarchy** - Urgent events more prominent than routine
4. **Minimal disruption** - Side placement doesn't block main UI
5. **Natural timeout** - Auto-dismiss if ignored (not forgotten/stuck)

### User Experience Flow

```
Event triggers → Side window appears (right side, animated slide-in)
                ↓
Player sees event title + description + options
                ↓
        ┌───────┴───────┐
        ↓               ↓
   Click option    Ignore/wait
        ↓               ↓
   Execute event   Auto-dismiss (20s)
        ↓               ↓
   Window closes   Event discarded or re-queued
```

---

## Visual Design

### Layout Mockup

```
┌────────────────────────────────────────┐
│  [Severity Border - colored left edge] │
│                                         │
│  ⚠ EVENT TITLE                         │
│  ────────────────────────────────      │
│                                         │
│  Event description text goes here.     │
│  Provides context about what           │
│  happened and why it matters.          │
│                                         │
│  ┌──────────────────────────────────┐ │
│  │  [Option 1 Text]                 │ │
│  └──────────────────────────────────┘ │
│  ┌──────────────────────────────────┐ │
│  │  [Option 2 Text]                 │ │
│  └──────────────────────────────────┘ │
│  ┌──────────────────────────────────┐ │
│  │  [Option 3 Text - Disabled]      │ │
│  └──────────────────────────────────┘ │
│                                         │
│  [████████████████░░░] 15s remaining   │
└────────────────────────────────────────┘
```

### Positioning

- **Horizontal:** Right side, 50px margin from screen edge
- **Vertical:** Center-aligned (adjusts for party screen if open)
- **Z-Order:** Above map, below inquiry popups
- **Size:** 400px width, dynamic height (max 500px)

### Animation

**✅ VERIFIED: Gauntlet Capabilities**
- Right-side positioning: `HorizontalAlignment="Right"`
- Dynamic positioning: `PositionXOffset="@PositionXOffset"` (animated in Tick)
- Fade effects: `AlphaFactor="@AlphaFactor"` (smooth transitions)
- Visibility control: `IsVisible="@IsVisible"`
- Button clicks: `ButtonWidget` with `Command.Click="ExecuteOption"`
- Timer updates: ViewModel `Tick(float dt)` method

**Slide-in (manual animation in Tick):**
```csharp
// Animate from off-screen to final position
if (_isSliding)
{
    _slideProgress += dt / _slideDuration; // 0.3s
    PositionXOffset = MathF.Lerp(400f, 0f, EaseOutCubic(_slideProgress));
}
```

**Fade-out:**
- Starts at 17 seconds
- AlphaFactor: 1.0 → 0.4 over 3 seconds
- Still clickable while fading

**Slide-out:**
- Animate PositionXOffset from 0 → 400px
- Duration: 0.2s

---

## Interaction Model

### Severity Levels

Events use `severity` field from JSON to determine visual treatment:

| Severity | Border Color | Auto-Dismiss | Sound | Urgency |
|----------|-------------|--------------|-------|---------|
| **Normal** | Gray `#888888` | 20s | Soft chime | Low |
| **Positive** | Green `#44FF44` | 20s | Pleasant bell | Low |
| **Attention** | Yellow `#FFDD44` | 20s | Alert tone | Medium |
| **Urgent** | Orange `#FFAA44` | 25s | Urgent beep | High |
| **Critical** | Red `#FF4444` | 30s | Alarm | Critical |

### Timer Behavior

```csharp
// Countdown starts immediately on show
_elapsedTime = 0f;
_lifetime = GetLifetimeForSeverity(event.Severity); // 20-30s

// Tick every frame
_elapsedTime += dt;
TimeBarWidth = (1f - (_elapsedTime / _lifetime)) * 360f;
TimeRemainingText = $"{(int)(_lifetime - _elapsedTime)}s";

// Fade starts at 85% of lifetime
if (_elapsedTime >= _lifetime * 0.85f)
{
    AlphaFactor = Lerp(1f, 0.4f, fadeProgress);
}

// Auto-dismiss at 100%
if (_elapsedTime >= _lifetime)
{
    Dismiss();
}
```

### Option Requirements

Options use existing requirement system from `EventDeliveryManager`:
- Checks tier, role, skill, trait requirements
- Disabled options shown with gray overlay + tooltip explaining why
- Enabled options highlight on hover

### Queue Management

```csharp
// Only one notification visible at a time
if (_currentNotification != null)
{
    _pendingQueue.Enqueue(evt);
    return;
}

// Show next after dismiss/option selected
if (_pendingQueue.Count > 0)
{
    var next = _pendingQueue.Dequeue();
    ShowNotification(next);
}
```

**Queue Priority:**
- Critical severity moves to front
- Urgent bumps ahead of normal
- FIFO within same severity

---

## Technical Architecture

### Component Structure

```
EnlistedEventNotificationBehavior (CampaignBehaviorBase)
├── Manages Gauntlet layer lifecycle
├── Handles event queue
└── Coordinates with EventDeliveryManager

EnlistedEventNotificationVM (ViewModel)
├── Event display data (title, description, options)
├── Timer state (elapsed, remaining, bar width, alpha)
├── Severity styling (border color, lifetime)
└── Option list (with enabled/disabled state)

EventNotificationOptionVM (ViewModel)
├── Option text + tooltip
├── Enabled state
├── Click handler
└── Requirement checking
```

### File Locations

```
src/Features/Interface/
├── Behaviors/
│   └── EnlistedEventNotificationBehavior.cs  [NEW]
├── ViewModels/
│   ├── EnlistedEventNotificationVM.cs        [NEW]
│   └── EventNotificationOptionVM.cs          [NEW]

GUI/Prefabs/Interface/
└── EnlistedEventNotification.xml             [NEW]

GUI/Brushes/
└── EnlistedEventNotification.xml             [NEW]
```

### Verified Infrastructure References

Based on existing combat log implementation:
- **Pattern:** `EnlistedCombatLogBehavior` + `EnlistedCombatLogVM`
- **Layer management:** `GauntletLayer` with priority (use 999 to be below menus)
- **Tick handling:** `OnTick(float dt)` → `_dataSource.Tick(dt)`
- **Positioning:** `PositionXOffset` and `PositionYOffset` for animations
- **Fade:** `AlphaFactor` bound to ViewModel property
- **Buttons:** `ButtonWidget` with `Command.Click` binding (see QuartermasterEquipmentCard.xml)
- **Timer bar:** Fake with `Widget` width binding (no native ProgressBar widget)

### Integration Points

**EventDeliveryManager modifications:**

```csharp
// OLD: Modal inquiry
private void ShowEventPopup(EventDefinition evt)
{
    var inquiry = new MultiSelectionInquiryData(...);
    MBInformationManager.ShowMultiSelectionInquiry(inquiry);
}

// NEW: Side notification
private void ShowEventPopup(EventDefinition evt)
{
    EnlistedEventNotificationBehavior.Instance?.QueueEvent(evt);
}
```

**Callback handling:**

```csharp
public void OnOptionSelected(EventDefinition evt, EventOption option)
{
    // Call existing EventDeliveryManager to process the option
    // Must queue event with pre-selected option since OnOptionSelected is private
    EventDeliveryManager.Instance.ProcessEventOption(evt, option);
}

public void OnDismissed(EventDefinition evt)
{
    // Log ignored event
    ModLogger.Info("EventNotification", $"Event {evt.Id} auto-dismissed");
    
    // Optional: Re-queue critical events
    if (evt.Severity == "critical")
    {
        _retryQueue.Enqueue(evt); // Show again in 5 minutes
    }
}
```

**Note:** `EventDeliveryManager.OnOptionSelected()` is private. Need to either:
1. Add public `ProcessEventOption(evt, option)` method to EventDeliveryManager
2. Or replicate the logic in notification behavior

**Recommended:** Add to EventDeliveryManager:
```csharp
public void ProcessEventOption(EventDefinition evt, EventOption option)
{
    _currentEvent = evt;
    var selectedElement = new InquiryElement(option, option.TextFallback, null, 
        true, option.Tooltip);
    OnOptionSelected(new List<InquiryElement> { selectedElement });
}
```

---

## Blueprint Compliance

### Critical Requirements (from BLUEPRINT.md)

**MUST follow these standards:**

1. ✅ **Target Bannerlord v1.3.13** - APIs verified against decompile
2. ⚠️ **Add new files to Enlisted.csproj** - Phase 3 task
   ```xml
   <Compile Include="src\Features\Interface\Behaviors\EnlistedEventNotificationBehavior.cs"/>
   <Compile Include="src\Features\Interface\ViewModels\EnlistedEventNotificationVM.cs"/>
   <Compile Include="src\Features\Interface\ViewModels\EventNotificationOptionVM.cs"/>
   <Content Include="GUI\Prefabs\Interface\EnlistedEventNotification.xml">
     <CopyToOutputDirectory>Always</CopyToOutputDirectory>
   </Content>
   <Content Include="GUI\Brushes\EnlistedEventNotification.xml">
     <CopyToOutputDirectory>Always</CopyToOutputDirectory>
   </Content>
   ```
3. ⚠️ **Use ModLogger with error codes** - Format: `E-NOTIF-001`, `W-NOTIF-001`
   ```csharp
   ModLogger.Info("EventNotification", "Showing side popup: {evt.Id}");
   ModLogger.WarnCode("EventNotification", "W-NOTIF-001", "Event has no valid options");
   ModLogger.ErrorCode("EventNotification", "E-NOTIF-001", "Failed to initialize layer");
   ```
4. ⚠️ **Code Standards**
   - Private fields: `_camelCase` (e.g., `_dataSource`, `_layer`)
   - Braces on all `if/for/while/foreach` (even single-line)
   - No unused imports/variables/methods
   - Comments describe current behavior (not "Phase X added...")
5. ⚠️ **Before Committing**
   ```powershell
   python Tools/Validation/validate_content.py
   dotnet build -c "Enlisted RETAIL" /p:Platform=x64
   ```

---

## Implementation Tasks

### Phase 1: Core UI (2-3 hours)

- [x] Research existing system
- [ ] Create `EnlistedEventNotificationVM.cs`
  - Event display properties
  - Timer logic + countdown
  - Fade alpha calculation
  - Option list management
- [ ] Create `EventNotificationOptionVM.cs`
  - Option text, enabled state
  - Click command binding
  - Tooltip generation
- [ ] Create `EnlistedEventNotification.xml` prefab
  - Side panel layout
  - Severity border styling
  - Option buttons with hover states
  - Timer bar + text display
- [ ] Create `EnlistedEventNotification.xml` brush
  - Title/description text styles
  - Option button states (normal/hover/disabled)
  - Timer text styling

### Phase 2: Behavior Logic (2 hours)

- [ ] Create `EnlistedEventNotificationBehavior.cs`
  - Gauntlet layer initialization
  - Event queue management
  - Priority sorting
  - Tick handling for timer
  - Callback wiring to EventDeliveryManager

### Phase 3: Integration (1 hour)

- [ ] Add `ProcessEventOption()` to EventDeliveryManager
  - Public method to process option selection from notification
  - Wraps private `OnOptionSelected()` logic
  - Sets `_currentEvent` context
- [ ] Modify `EventDeliveryManager.ShowEventPopup()`
  - Replace inquiry with notification behavior call
  - Keep existing logic as fallback (if notification disabled)
- [ ] Add new files to `Enlisted.csproj`
- [ ] Deploy prefab/brush to game directory

### Phase 4: Testing (1-2 hours)

**Functional Testing:**
- [ ] Test single event delivery
- [ ] Test multiple events (queue)
- [ ] Test auto-dismiss at 20s
- [ ] Test option selection (all paths)
- [ ] Test disabled options (requirement failures)
- [ ] Test severity levels (colors, timers)
- [ ] Test during combat/travel/menus
- [ ] Test save/load (queue clears)

**Blueprint Compliance Validation:**
- [ ] All new files added to `Enlisted.csproj`
- [ ] Private fields use `_camelCase` naming
- [ ] All control statements have braces
- [ ] ModLogger used with error codes (E-NOTIF-*, W-NOTIF-*)
- [ ] No unused imports/variables
- [ ] No ReSharper warnings
- [ ] Validation passes: `python Tools/Validation/validate_content.py`
- [ ] Build succeeds: `dotnet build -c "Enlisted RETAIL" /p:Platform=x64`

### Phase 5: Polish (1 hour)

- [ ] Add slide-in/out animations
- [ ] Add sound effects per severity
- [ ] Tune fade timing
- [ ] Adjust positioning for party screen overlap
- [ ] Add config options (enable/disable, timer length)

### Phase 6: Debug Testing Button (30 min)

- [ ] Add "[DEBUG] Test Notification" button to enlisted_status menu
  - Position at bottom of decision list
  - Only visible when mod is in debug mode
  - Triggers sample event with 3 test options
- [ ] Create test event definition in EventCatalog
  - ID: `debug_notification_test`
  - Multiple options to test enabled/disabled states
  - Test severity levels (cycle through normal/urgent/critical)
- [ ] Use existing `EnableDebugTools` from `settings.json`
  - Already exists: `ModSettings.EnableDebugTools`
  - No new config needed

**Total Estimated Time:** 7.5-9.5 hours

---

## Configuration Options

Add to `interface_config.json`:

```json
{
  "eventNotifications": {
    "enabled": true,
    "defaultLifetime": 20.0,
    "fadeStartPercent": 0.85,
    "enableSounds": true,
    "severityLifetimes": {
      "normal": 20.0,
      "positive": 20.0,
      "attention": 20.0,
      "urgent": 25.0,
      "critical": 30.0
    },
    "maxQueueSize": 10,
    "retryIgnoredCritical": false
  }
}
```

---

## Future Enhancements

### Post-MVP Features

1. **Notification history** - Log of recent events (last 10) accessible from camp menu
2. **Snooze option** - "Remind me in 5 minutes" button for non-urgent events
3. **Stacking mode** - Show multiple notifications simultaneously (max 3)
4. **Custom positioning** - Player preference for left/right/center
5. **Hotkey engagement** - Press key to select first option
6. **Urgency pulse** - Critical events pulse border/glow
7. **Context awareness** - Hide during combat, show after battle ends

### Integration with Other Systems

- **Camp Opportunities:** Use notification window for scheduled opportunity reminders
- **Order Events:** Reduce interruption from frequent order event spam
- **Injury/Illness:** Show medical events with special medical icon
- **Promotion:** Flash special border for promotion proving events

---

## Success Metrics

### How We'll Know It Works

1. **Player feedback:** "Events feel less intrusive"
2. **Engagement rate:** Players click options vs. ignore
3. **Critical event completion:** 90%+ critical events get responses (not ignored)
4. **Spam reduction:** Fewer complaints about event interruptions
5. **Completion rates:** Compare option selection rates before/after

### Acceptance Criteria

- [ ] Notification appears on right side within 0.3s of event trigger
- [ ] Timer counts down accurately from 20s
- [ ] Fade animation starts at 17s
- [ ] Auto-dismiss removes notification at 20s
- [ ] Option clicks execute correct event logic
- [ ] Disabled options show correct tooltips
- [ ] Queue handles 10+ events without slowdown
- [ ] No memory leaks after 100+ events
- [ ] Notification hidden during conversations/missions
- [ ] Party screen opening doesn't overlap notification

---

## Risk Assessment

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| **Performance** - Tick overhead from timer | Low | Medium | Only tick when visible, cache calculations |
| **UX** - Players miss critical events | Medium | High | Longer timer for critical (30s), retry option |
| **UI Overlap** - Party screen collision | Medium | Low | Detect party screen, adjust Y position |
| **Save/Load** - Queue persistence | Low | Low | Queue is transient, events re-trigger on conditions |
| **Animation jank** - Slide animations stutter | Low | Medium | Use native easing, test on low-end hardware |

---

## Technical Feasibility

### ✅ Confirmed Working (from combat log)
- Right-side positioning with `HorizontalAlignment="Right"`
- Dynamic offset animation via `PositionXOffset="@Property"`
- Fade effects via `AlphaFactor="@Property"`  
- ViewModel Tick updates at ~60fps
- Button clicks with `Command.Click`
- GauntletLayer priority system
- Multiple widgets in single layer

### 🎯 **CRITICAL DISCOVERY: Native Inquiry Timeout**

From `TaleWorlds.Library.InquiryData`:
```csharp
public readonly float ExpireTime;  // Line 16
public readonly Action TimeoutAction;  // Line 24

public InquiryData(
    string titleText,
    string text,
    ...
    float expireTime = 0.0f,
    Action timeoutAction = null,
    ...)
```

**Native inquiries ALREADY support auto-timeout!** We could potentially:
1. Use native `InquiryData` with `expireTime = 20f` 
2. OR build custom Gauntlet popup (more control over positioning)

**Decision:** Build custom - gives us right-side positioning and full control

### ⚠️ Limitations & Workarounds
- **No built-in slide animations:** Manually animate PositionXOffset in Tick
- **No ProgressBar widget:** Use fixed-width Widget with dynamic Width binding
- **No built-in easing:** Implement EaseOutCubic/EaseInCubic helpers
- **Layer suspend during conversations:** Use same pattern as combat log (CombatLogConversationPatch)

### 🔧 Required Custom Code
```csharp
// Easing function for smooth animations
private static float EaseOutCubic(float t) => 1f - MathF.Pow(1f - t, 3f);

// Timer bar width calculation (no native progress bar)
TimeBarWidth = 360f * (1f - (_elapsedTime / _lifetime));
```

---

## Open Questions

1. **Should ignored events re-queue?** 
   - Pro: Player doesn't miss important content
   - Con: Could be annoying if player genuinely wants to skip
   - **Decision:** Only retry critical severity, max 1 retry

2. **Sound effects - per event or per severity?**
   - **Decision:** Per severity, keeps audio manageable

3. **Multiple notifications at once?**
   - **Decision:** MVP = queue only, future enhancement = stacking

4. **Config option to disable entirely?**
   - **Decision:** Yes, fallback to old inquiry system

5. **Persist across save/load?**
   - **Decision:** No, queue is transient (events will re-trigger from conditions)

---

## Debug Testing Button

### Implementation

Add to `EnlistedMenuBehavior.RefreshMainMenuDecisionSlots()`:

```csharp
private void RefreshMainMenuDecisionSlots()
{
    // ... existing decision slots ...
    
    // Add debug test button at bottom (only if debug mode enabled)
    if (IsDebugNotificationsEnabled())
    {
        var debugOption = new GameMenuOption(
            optionId: "debug_test_notification",
            optionText: "[DEBUG] Test Event Notification",
            optionType: GameMenuOption.LeaveType.Continue,
            onConsequence: args =>
            {
                TriggerDebugNotification();
                return true;
            },
            isEnabled: true,
            tooltip: "Fire a test notification to preview the system"
        );
        
        MenuContext.AddGameMenuOption(debugOption);
    }
}

private bool IsDebugNotificationsEnabled()
{
    // Use existing debug tools flag from settings.json
    return ModSettings.Instance?.EnableDebugTools ?? false;
}

private void TriggerDebugNotification()
{
    // Cycle through severity levels
    _debugSeverityIndex = (_debugSeverityIndex + 1) % 5;
    var severities = new[] { "normal", "positive", "attention", "urgent", "critical" };
    
    var testEvent = new EventDefinition
    {
        Id = "debug_notification_test",
        TitleFallback = $"Test Notification ({severities[_debugSeverityIndex]})",
        SetupFallback = "This is a debug test notification. Choose an option or wait 20 seconds.",
        Severity = severities[_debugSeverityIndex],
        Options = new List<EventOption>
        {
            new EventOption
            {
                Id = "test_option_1",
                TextFallback = "Option 1 (Enabled)",
                Tooltip = "This option is always available",
                Effects = new EventEffects { Gold = 50 }
            },
            new EventOption
            {
                Id = "test_option_2",
                TextFallback = "Option 2 (Skill Check)",
                Tooltip = "Requires Leadership 100",
                Requirements = new EventOptionRequirements { MinSkills = new Dictionary<string, int> { { "Leadership", 100 } } },
                Effects = new EventEffects { SkillXp = new Dictionary<string, int> { { "Leadership", 50 } } }
            },
            new EventOption
            {
                Id = "test_option_3",
                TextFallback = "Option 3 (Dismiss)",
                Tooltip = "Close without action",
                Effects = null
            }
        }
    };
    
    EnlistedEventNotificationBehavior.Instance?.QueueEvent(testEvent);
    
    InformationManager.DisplayMessage(
        new InformationMessage($"Triggered {severities[_debugSeverityIndex]} test notification", Colors.Cyan));
}
```

### Config File (Already Exists!)

`ModuleData/Enlisted/Config/settings.json`:

```json
{
  "EnableDebugTools": true  // SET TO FALSE FOR RELEASE
}
```

**No changes needed** - the project already has this debug flag!

---

## References

- [Enlisted Combat Log](../Features/UI/enlisted-combat-log.md) - Similar side UI pattern
- [Event System Schemas](../Features/Content/event-system-schemas.md) - Event JSON structure
- [Content System Architecture](../Features/Content/content-system-architecture.md) - Event delivery flow
- [UI Systems Master](../Features/UI/ui-systems-master.md) - Gauntlet patterns
