# Enlisted Combat Log

**Status:** Planned  
**Category:** UI / Information Display  
**Related Docs:** [News Reporting System](news-reporting-system.md), [UI Systems Master](ui-systems-master.md), [Color Scheme](color-scheme.md)

---

## Overview

A custom, persistent combat log widget that displays information messages on the right side of the campaign map screen during enlistment. The system suppresses the native bottom-left combat log while enlisted and routes messages to a scrollable, semi-transparent display positioned above the army menu. When not enlisted (left army, prisoner, quit), the native combat log resumes normal operation.

**Key Features:**
- Scrollable message history (last 50 messages)
- Semi-transparent background with persistent text
- Reactive positioning when army menu opens/closes
- Automatic message expiration (5 minute lifetime)
- Color-coded messages matching existing color scheme
- Seamless transition between enlisted/non-enlisted states

---

## Purpose

**Problem:** The native combat log displays messages at the bottom-left of the screen with very short persistence, making it difficult to review routine outcomes, event results, and order notifications during active enlisted gameplay. The messages fade quickly and cannot be scrolled.

**Solution:** Create a dedicated combat log for enlisted soldiers that provides:
1. **Longer message persistence** - Messages stay visible for 5 minutes instead of a few seconds
2. **Scrollable history** - Review up to 50 recent messages at any time
3. **Better positioning** - Right side placement near relevant UI (army menu, status buttons)
4. **Contextual display** - Only active during enlistment, doesn't interfere with solo play

**Benefits:**
- Players can review what happened during busy camp routines
- Event and decision outcomes remain visible for review
- Order status changes don't get lost in action
- Clean integration with enlisted UI ecosystem (news, status menu)

---

## Technical Architecture

### Components

**1. C# ViewModel (`EnlistedCombatLogVM`)**
- Manages message collection and display
- Subscribes to message events
- Handles message expiration and list limits
- Provides positioning data for XML

**2. Gauntlet XML Prefab (`EnlistedCombatLog.xml`)**
- Defines visual layout and positioning
- Scrollable panel with message list
- Transparent/minimal visual design
- Responsive to army menu state

**3. Harmony Patch (`InformationManagerDisplayMessagePatch`)**
- Intercepts `InformationManager.DisplayMessage()` calls
- Routes messages to custom log when enlisted
- Allows native display when not enlisted

**4. Behavior Manager (`EnlistedCombatLogBehavior`)**
- Campaign behavior to manage lifecycle
- Loads/unloads Gauntlet layer
- Handles enlisted state changes

---

## Implementation Details

### File Structure

```
src/Features/Interface/
├── ViewModels/
│   ├── EnlistedCombatLogVM.cs           (Main ViewModel)
│   └── CombatLogMessageVM.cs            (Individual message)
└── Behaviors/
    └── EnlistedCombatLogBehavior.cs     (Manager behavior)

src/Mod.GameAdapters/Patches/
└── InformationManagerDisplayMessagePatch.cs

GUI/Prefabs/Interface/
└── EnlistedCombatLog.xml                (UI layout)
```

### C# ViewModel Structure

**EnlistedCombatLogVM.cs:**
```csharp
public class EnlistedCombatLogVM : ViewModel
{
    // Properties
    public MBBindingList<CombatLogMessageVM> Messages { get; }
    public bool IsVisible { get; set; }
    public float PositionYOffset { get; set; }  // Adjusts when army menu opens
    
    // Configuration
    private const int MaxMessages = 50;
    private const float MessageLifetimeSeconds = 300f;  // 5 minutes
    
    // Methods
    public void AddMessage(InformationMessage message)
    public void Tick(float dt)  // Handle expiration
    public void UpdatePositioning(bool isArmyMenuOpen)
    public void Clear()
}
```

**CombatLogMessageVM.cs:**
```csharp
public class CombatLogMessageVM : ViewModel
{
    public string Text { get; }
    public Color MessageColor { get; }
    public float TimeRemaining { get; set; }
    public CampaignTime CreatedAt { get; }
    
    // Auto-fade support (optional)
    public float AlphaFactor { get; set; }
}
```

### Gauntlet XML Layout

**EnlistedCombatLog.xml:**
```xml
<Prefab>
  <Window>
    <Widget DoNotAcceptEvents="true" 
            WidthSizePolicy="Fixed" 
            HeightSizePolicy="Fixed"
            SuggestedWidth="400" 
            SuggestedHeight="300"
            HorizontalAlignment="Right" 
            VerticalAlignment="Bottom"
            PositionXOffset="-20"
            PositionYOffset="@PositionYOffset"
            IsVisible="@IsVisible">
      <Children>
        
        <!-- Optional semi-transparent background -->
        <Widget WidthSizePolicy="StretchToParent" 
                HeightSizePolicy="StretchToParent"
                Brush="BackgroundBrush"
                Brush.GlobalAlphaFactor="0.3" />
        
        <!-- Scrollable message container -->
        <ScrollablePanel WidthSizePolicy="StretchToParent" 
                        HeightSizePolicy="StretchToParent"
                        AutoScrollToBottom="true"
                        InnerPanel="MessageListPanel">
          <Children>
            
            <!-- Message list -->
            <ListPanel Id="MessageListPanel"
                      DataSource="{Messages}"
                      WidthSizePolicy="StretchToParent"
                      HeightSizePolicy="CoverChildren"
                      StackLayout.LayoutMethod="VerticalBottomToTop">
              <ItemTemplate>
                
                <!-- Individual message -->
                <TextWidget WidthSizePolicy="StretchToParent"
                           HeightSizePolicy="CoverChildren"
                           Text="@Text"
                           Brush="CombatLogMessageBrush"
                           Brush.TextColor="@MessageColor"
                           Brush.GlobalAlphaFactor="@AlphaFactor"
                           MarginBottom="2" />
                
              </ItemTemplate>
            </ListPanel>
            
          </Children>
        </ScrollablePanel>
        
      </Children>
    </Widget>
  </Window>
</Prefab>
```

### Harmony Patch

**InformationManagerDisplayMessagePatch.cs:**
```csharp
using HarmonyLib;
using TaleWorlds.Library;
using Enlisted.Features.Enlistment;
using Enlisted.Features.Interface.Behaviors;

namespace Enlisted.Mod.GameAdapters.Patches
{
    /// <summary>
    /// Intercepts InformationManager.DisplayMessage to route messages to the custom
    /// enlisted combat log when the player is enlisted. Allows native display to resume
    /// when not enlisted (prisoner, left army, etc.).
    /// </summary>
    [HarmonyPatch(typeof(InformationManager), "DisplayMessage")]
    internal class InformationManagerDisplayMessagePatch
    {
        /// <summary>
        /// Prefix that decides whether to show messages in native log or custom log.
        /// Returns false to suppress native display when enlisted.
        /// </summary>
        [HarmonyPrefix]
        public static bool Prefix(InformationMessage message)
        {
            // Not enlisted: use native combat log
            if (!EnlistmentManager.IsEnlisted)
            {
                return true; // Execute original DisplayMessage
            }
            
            // Enlisted: route to custom combat log
            var combatLog = EnlistedCombatLogBehavior.Instance;
            if (combatLog != null)
            {
                combatLog.AddMessage(message);
            }
            
            return false; // Skip original DisplayMessage
        }
    }
}
```

### Campaign Behavior Manager

**EnlistedCombatLogBehavior.cs:**
```csharp
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.Library;
using Enlisted.Features.Enlistment;

namespace Enlisted.Features.Interface.Behaviors
{
    /// <summary>
    /// Manages the enlisted combat log UI layer lifecycle and message routing.
    /// Creates the Gauntlet layer when campaign starts and handles visibility
    /// based on enlistment state.
    /// </summary>
    public class EnlistedCombatLogBehavior : CampaignBehaviorBase
    {
        public static EnlistedCombatLogBehavior Instance { get; private set; }
        
        private GauntletLayer _layer;
        private EnlistedCombatLogVM _dataSource;
        private IGauntletMovie _movie;
        
        public EnlistedCombatLogBehavior()
        {
            Instance = this;
        }
        
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(
                this, OnSessionLaunched);
        }
        
        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            InitializeUI();
        }
        
        private void InitializeUI()
        {
            // Create Gauntlet layer
            _layer = new GauntletLayer(200); // Layer priority
            
            // Create ViewModel
            _dataSource = new EnlistedCombatLogVM();
            
            // Load prefab
            _movie = _layer.LoadMovie("EnlistedCombatLog", _dataSource);
            
            // Add to screen
            var mapScreen = ScreenManager.TopScreen as MapScreen;
            if (mapScreen != null)
            {
                mapScreen.AddLayer(_layer);
            }
        }
        
        /// <summary>
        /// Called by Harmony patch to add messages to the log.
        /// </summary>
        public void AddMessage(InformationMessage message)
        {
            _dataSource?.AddMessage(message);
        }
        
        public override void SyncData(IDataStore dataStore)
        {
            // No save data needed - messages are transient
        }
        
        protected override void OnEndGame()
        {
            _dataSource?.OnFinalize();
            _movie?.Release();
            _layer = null;
            Instance = null;
        }
    }
}
```

---

## Behavior Specification

### Message Flow

**1. Message Generated:**
- Game code calls `InformationManager.DisplayMessage(new InformationMessage(...))`
- Examples: routine outcomes, event results, order status changes

**2. Harmony Patch Intercepts:**
- Checks `EnlistmentManager.IsEnlisted`
- If **enlisted**: routes to `EnlistedCombatLogBehavior.AddMessage()` and returns false
- If **not enlisted**: returns true, native display proceeds normally

**3. Custom Log Adds Message:**
- Creates `CombatLogMessageVM` with text, color, timestamp
- Adds to `Messages` list (MBBindingList)
- If list exceeds 50 messages, removes oldest
- ScrollablePanel auto-scrolls to show new message

**4. Message Persistence:**
- Message stays in list for 5 minutes
- `Tick()` method checks age and removes expired messages
- Optional: fade alpha as message ages (last 30 seconds)

### Positioning Logic

**Base Position:**
- Right side of screen
- Bottom alignment
- 20px margin from screen edge
- Height: 300px, Width: 400px

**Army Menu Detection:**
- Monitor `MapScreen.IsInArmyManagement` property
- When army menu opens: shift log up by army menu height (~150px)
- When army menu closes: return to base position
- Smooth transition using VisualDefinition (0.3s ease)

**Z-Order:**
- Layer priority: 200 (above map, below menus/dialogs)
- Army menu typically at 300+, so log stays behind it

### Visibility Rules

| Player State | Combat Log Visible | Native Log Active |
|-------------|-------------------|-------------------|
| Enlisted | ✓ Custom | ✗ Suppressed |
| Solo (never enlisted) | ✗ Hidden | ✓ Normal |
| Left army | ✗ Hidden | ✓ Normal |
| Prisoner | ✗ Hidden | ✓ Normal |
| Campaign menu open | ✓ Custom | ✗ Suppressed |
| Battle (if enlisted) | ✗ Hidden | ✓ Normal* |

*Note: Battle uses its own message system; patch should detect mission active state

### Message Styling

Messages use the existing color scheme from [color-scheme.md](color-scheme.md):

| Message Type | Color | Use Case |
|-------------|-------|----------|
| Success | `#44AA44` (Bright Green) | Excellent routine outcomes, successful events |
| Positive | `#88CC88` (Light Green) | Good outcomes, minor gains |
| Neutral | `#CCCCCC` (Light Gray) | Normal outcomes, information |
| Warning | `#CCCC44` (Yellow) | Poor outcomes, minor issues |
| Failure | `#DB0808` (Red) | Failed decisions, serious problems |
| Order Status | `#4A9EFF` (Blue) | Order started/completed/failed |

---

## Configuration Options

**Exposed via `ModSettings` (future enhancement):**

```csharp
public class CombatLogSettings
{
    /// <summary>Max number of messages to keep in history.</summary>
    public int MaxMessages { get; set; } = 50;
    
    /// <summary>How long messages stay visible (seconds).</summary>
    public float MessageLifetime { get; set; } = 300f; // 5 minutes
    
    /// <summary>Enable fade effect on old messages.</summary>
    public bool EnableMessageFading { get; set; } = true;
    
    /// <summary>Auto-hide log when not receiving messages.</summary>
    public bool AutoHide { get; set; } = false;
    
    /// <summary>Background opacity (0-1).</summary>
    public float BackgroundAlpha { get; set; } = 0.3f;
    
    /// <summary>Font size for messages.</summary>
    public int FontSize { get; set; } = 16;
}
```

---

## Integration Points

### Existing Systems

**1. News Reporting System:**
- Combat log shows **immediate** feedback (instant)
- News system shows **persistent** records (Recent Activities)
- Both work together: log for "what just happened", news for "what happened today"

**2. Event System:**
- Decision results go to both combat log (instant) and Recent Activities (persistent)
- Regular event outcomes go to Recent Activities only (avoid spam)
- Combat log only shows player-initiated actions or important notifications

**3. Order System:**
- Order status changes (`OrderStarted`, `OrderCompleted`, `OrderFailed`) display in log
- Color-coded: Blue for status, Green for success, Red for failure

**4. Routine System:**
- Routine outcomes send to combat log via `SendCombatLogMessage()`
- Shows flavor text with XP gains/losses
- Color matches outcome quality

### Message Routing Rules

**Send to Combat Log:**
- ✓ Routine activity outcomes
- ✓ Decision results (immediate feedback)
- ✓ Order status changes
- ✓ Equipment condition warnings
- ✓ Supply status alerts
- ✓ Battle participation notifications

**Do NOT Send to Combat Log:**
- ✗ Regular event outcomes (use Recent Activities only)
- ✗ Native game messages (kingdom notifications, etc.) when not relevant
- ✗ Spam-prone notifications (party joined army, etc.)

### Filtering Strategy

Add filtering in `AddMessage()`:

```csharp
public void AddMessage(InformationMessage message)
{
    // Filter out irrelevant native messages
    if (ShouldFilterMessage(message))
        return;
    
    // Add to log
    var logMessage = new CombatLogMessageVM(message);
    Messages.Add(logMessage);
    
    // Enforce limit
    while (Messages.Count > MaxMessages)
    {
        Messages.RemoveAt(0);
    }
}

private bool ShouldFilterMessage(InformationMessage message)
{
    // Example filters:
    // - Ignore generic "Party spotted" messages
    // - Ignore kingdom-wide notifications
    // - Keep only enlisted-relevant messages
    return false; // Customize based on message.Information
}
```

---

## Edge Cases

### 1. Enlistment State Changes

**Scenario:** Player enlists mid-campaign after seeing native log.

**Behavior:**
- Harmony patch activates immediately on enlist
- Native log stops displaying
- Custom log appears empty (no history carried over)
- Future messages route to custom log

**Scenario:** Player leaves army or becomes prisoner.

**Behavior:**
- Harmony patch deactivates on de-enlist
- Custom log hides (visibility = false)
- Native log resumes immediately
- Custom log messages not cleared (available if re-enlist same session)

### 2. Army Menu Interaction

**Scenario:** Player opens army menu while combat log is showing messages.

**Behavior:**
- Log smoothly shifts up to avoid overlap
- Messages remain readable and scrollable
- Closing army menu shifts log back down
- Transition animation: 0.3s ease

**Scenario:** Army menu height varies (more/fewer parties).

**Behavior:**
- Calculate actual army menu height dynamically
- Adjust log position based on measured height
- Ensure minimum 10px gap between log and menu

### 3. Message Volume Spikes

**Scenario:** 20+ messages arrive in rapid succession (mass routine processing).

**Behavior:**
- All messages added to list
- ScrollablePanel auto-scrolls to show newest
- Oldest messages beyond 50-count limit removed immediately
- No performance impact (list capped at 50)

**Scenario:** Player is AFK, messages expire while idle.

**Behavior:**
- `Tick()` removes expired messages silently
- List shrinks naturally over 5 minutes
- Empty list state shows no messages (clean display)

### 4. Screen Resolution Variations

**Scenario:** Player uses non-standard resolution or UI scaling.

**Behavior:**
- Log uses fixed pixel positioning (400x300) to maintain readability
- Right-alignment ensures log stays on screen
- Text wrapping handles narrow widths gracefully
- Test at 1080p, 1440p, 4K

### 5. Battle Transitions

**Scenario:** Player enters battle while enlisted.

**Behavior:**
- Combat log hides during battle (mission active)
- Battle messages use native mission log system
- After battle, enlisted combat log reappears
- Battle-related messages (victory, casualties) may appear in log post-battle

### 6. Save/Load

**Scenario:** Player saves game with messages in log, then loads.

**Behavior:**
- Messages are **not** persisted to save file (transient by design)
- Log appears empty after load
- New messages populate as gameplay resumes
- Rationale: messages are real-time feedback, not historical records

### 7. Mod Conflicts

**Scenario:** Another mod also patches `InformationManager.DisplayMessage()`.

**Behavior:**
- Harmony patch priority determines order
- If conflict detected, log warning to ModLogger
- Fallback: disable suppression, show both native + custom logs
- Document known conflicts in README

---

## Acceptance Criteria

### Core Functionality

- [ ] Custom combat log displays on right side of screen when enlisted
- [ ] Log shows last 50 messages with 5-minute expiration
- [ ] Messages are scrollable using mouse wheel
- [ ] Messages use correct color coding per message type
- [ ] Native combat log is suppressed while enlisted
- [ ] Native combat log resumes when not enlisted

### Positioning & Layout

- [ ] Log positioned 20px from right edge, bottom-aligned
- [ ] Log shifts up when army menu opens (smooth transition)
- [ ] Log shifts down when army menu closes (smooth transition)
- [ ] No overlap with army menu or other UI elements
- [ ] Background is semi-transparent (30% alpha) or invisible
- [ ] Text remains readable against all map backgrounds

### Message Handling

- [ ] Routine outcomes appear in log with flavor text and XP
- [ ] Decision results appear with appropriate success/failure color
- [ ] Order status changes appear with blue coloring
- [ ] Messages older than 5 minutes are automatically removed
- [ ] List never exceeds 50 messages (oldest removed first)
- [ ] Auto-scroll to newest message when message added

### State Transitions

- [ ] Log appears when player enlists
- [ ] Log hides when player leaves army
- [ ] Log hides when player becomes prisoner
- [ ] Log hides during battle/mission
- [ ] Log reappears after battle (if still enlisted)
- [ ] Transitions are smooth and bug-free

### Integration

- [ ] Works alongside news reporting system (no conflicts)
- [ ] Works alongside event system (decisions show in both)
- [ ] Works alongside order system (status changes visible)
- [ ] Works alongside routine system (outcomes displayed)
- [ ] No interference with native game systems

### Performance

- [ ] No FPS impact with 50 messages in log
- [ ] No memory leaks over extended play sessions
- [ ] Smooth scrolling performance
- [ ] Fast message addition (no stutter when adding messages)

### Error Handling

- [ ] Graceful handling if Gauntlet layer fails to load
- [ ] Logs error if prefab file missing
- [ ] Falls back to native log if patch fails
- [ ] No crashes if ViewModel is null

---

## Future Enhancements

### Phase 2 Additions

**1. Message Filtering:**
- Toggle switches in settings to show/hide message types
- "Show Routines", "Show Events", "Show Orders", etc.
- Per-category filtering without code changes

**2. Message History Panel:**
- Button to expand log into full-screen overlay
- Search/filter capability
- Export to text file for bug reports
- Show timestamps with each message

**3. Visual Improvements:**
- Category icons next to messages (sword for combat, tools for routines)
- Fade-in animation for new messages
- Fade-out animation for expiring messages
- Configurable background styles (dark, light, invisible)

**4. Integration with Combat:**
- Special handling for battle start/end notifications
- Casualty reports in combat log post-battle
- Victory/defeat outcomes with loot summary

**5. Notifications:**
- Audio cue for high-priority messages (optional)
- Visual flash or pulse for critical alerts
- Desktop notifications for background play (advanced)

### Configuration Expansion

**Additional settings:**
- Log width/height customization
- Font face and size options
- Message count limit (25/50/100)
- Expiration time (2min/5min/10min/infinite)
- Positioning override (left/right/custom)

---

## Testing Checklist

### Manual Tests

**Basic Display:**
- [ ] Log appears when enlisted
- [ ] Log hides when not enlisted
- [ ] Messages display with correct colors
- [ ] Scrolling works smoothly

**Army Menu Interaction:**
- [ ] Log shifts up when army menu opens
- [ ] Log shifts down when army menu closes
- [ ] No overlap or visual glitches
- [ ] Transitions are smooth

**Message Lifecycle:**
- [ ] New messages appear at bottom
- [ ] Old messages scroll up
- [ ] Messages expire after 5 minutes
- [ ] List caps at 50 messages

**State Changes:**
- [ ] Enlist → log appears
- [ ] Leave army → log hides, native resumes
- [ ] Become prisoner → log hides, native resumes
- [ ] Re-enlist → log reappears
- [ ] Save/load → log clears and resumes

**Integration:**
- [ ] Routine outcomes appear correctly
- [ ] Decision results appear correctly
- [ ] Order notifications appear correctly
- [ ] No duplicate messages in native log

### Stress Tests

- [ ] Add 100+ messages rapidly (should cap at 50)
- [ ] Open/close army menu rapidly (no crashes)
- [ ] Leave and re-enlist repeatedly (no memory leaks)
- [ ] Play for 2+ hours enlisted (no performance degradation)

### Resolution Tests

- [ ] Test at 1920x1080 (standard)
- [ ] Test at 2560x1440 (high-res)
- [ ] Test at 3840x2160 (4K)
- [ ] Test with UI scaling at 80%, 100%, 120%

---

## Implementation Notes

### Development Order

1. **Phase 1: Basic Infrastructure** (2 hours)
   - Create `CombatLogMessageVM` and `EnlistedCombatLogVM`
   - Create `EnlistedCombatLogBehavior` manager
   - Register behavior in `SubModule.OnGameStart()`

2. **Phase 2: Gauntlet UI** (2 hours)
   - Create `EnlistedCombatLog.xml` prefab
   - Set up scrollable panel and message list
   - Test basic display and scrolling

3. **Phase 3: Harmony Patch** (1 hour)
   - Create `InformationManagerDisplayMessagePatch`
   - Test message interception and routing
   - Verify native log suppression

4. **Phase 4: Positioning Logic** (1 hour)
   - Implement army menu detection
   - Add position offset calculations
   - Smooth transition animations

5. **Phase 5: Integration** (1 hour)
   - Connect to routine system
   - Connect to event/decision system
   - Connect to order system

6. **Phase 6: Polish & Testing** (1 hour)
   - Message expiration logic
   - Edge case handling
   - Performance testing

**Total estimated time: ~8 hours**

### Key Classes to Reference

- `TaleWorlds.Library.InformationManager` - Message system
- `TaleWorlds.Library.InformationMessage` - Message data structure
- `TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationVM` - Similar pattern for notifications
- `SandBox.View.Map.MapScreen` - Screen layer management
- `Enlisted.Features.Enlistment.EnlistmentManager` - Enlisted state checks

### Pitfalls to Avoid

1. **Don't store messages in save file** - Makes saves bloated, messages are transient
2. **Don't forget to unsubscribe events** - Memory leaks on campaign end
3. **Don't hard-code positioning** - Use responsive layout where possible
4. **Don't suppress ALL messages** - Battle messages should still work natively
5. **Don't forget null checks** - Instance may not exist during initialization

---

## Related Documentation

- **[News Reporting System](news-reporting-system.md)** - Persistent event display in status menu
- **[UI Systems Master](ui-systems-master.md)** - Overview of all UI systems
- **[Color Scheme](color-scheme.md)** - Message color definitions
- **[Event System](../Content/event-system-schemas.md)** - Decision and event outcomes
- **[Order System](../Content/order-system-master.md)** - Order status notifications
- **[Camp Routine System](../Campaign/camp-routine-schedule-spec.md)** - Routine outcome messages

---

## Changelog

**2025-01-01:**
- Initial specification created
- Documented architecture and implementation approach
- Defined acceptance criteria and testing requirements
