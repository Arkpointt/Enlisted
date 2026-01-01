# Enlisted Combat Log

**Status:** ✅ Implemented  
**Category:** UI / Information Display  
**Related Docs:** [News Reporting System](news-reporting-system.md), [UI Systems Master](ui-systems-master.md), [Color Scheme](color-scheme.md)

---

## Overview

A native-styled combat log widget that displays information messages on the right side of the campaign map screen during enlistment. The system suppresses the native bottom-left combat log while enlisted and routes messages to a scrollable, professional display that mimics Bannerlord's native message feed aesthetic.

**Key Features:**
- Native-style text rendering with faction-specific link colors
- Transparent background with shadowed text
- Smart auto-scroll that pauses on user interaction (resumes after 6 seconds idle)
- Inactivity fade (35% opacity after 10 seconds)
- Scrollable message history (last 50 messages)
- Mouse wheel scrolling support
- Automatic message expiration (5 minute lifetime)
- Color-coded messages matching game's color scheme
- Bottom-to-top message ordering (newest at bottom)
- Automatic repositioning when party screen opens
- **Clickable encyclopedia links** with faction-specific colors (heroes: cyan, settlements: cyan, kingdoms: faction banner colors)

---

## Purpose

**Problem:** The native combat log displays messages at the bottom-left of the screen with very short persistence, making it difficult to review routine outcomes, event results, and order notifications during active enlisted gameplay.

**Solution:** A dedicated combat log that provides:
1. **Longer message persistence** - Messages stay visible for 5 minutes instead of seconds
2. **Scrollable history** - Review up to 50 recent messages at any time
3. **Smart behavior** - Auto-scrolls to newest, pauses when you read, fades when idle
4. **Native aesthetic** - Uses game's native styling for seamless integration
5. **Contextual display** - Only active during enlistment

---

## Technical Architecture

### Components

**1. C# ViewModel (`EnlistedCombatLogVM`)**
- Manages message collection with 50-message cap
- Handles message expiration (5 minutes real-time)
- Controls inactivity fade (35% after 10 seconds)
- Provides user interaction tracking
- Manages visibility based on enlistment state

**2. Gauntlet XML Prefab (`EnlistedCombatLog.xml`)**
- Native-style layout with custom brush for link support
- Uses `Enlisted.CombatLog.Text` brush with link styles
- Transparent background (no sprite)
- Native scrollbar with auto-hide
- Bottom-to-top message layout
- Proper clipping and scroll behavior

**3. Custom Brush (`EnlistedCombatLog.xml`)**
- Extends native text styling with link support
- Defines encyclopedia link colors (Hero: teal-green, Settlement: sky-blue, Kingdom: gold)
- Includes hover and click states for interactive feedback
- Matches native text rendering (Galahad font, text glow, outline)

**4. Harmony Patch (`InformationManagerDisplayMessagePatch`)**
- Intercepts `InformationManager.DisplayMessage()` calls
- Routes messages to custom log when enlisted
- Suppresses native display during enlistment
- Allows native display when not enlisted

**5. Behavior Manager (`EnlistedCombatLogBehavior`)**
- Campaign behavior managing UI lifecycle
- Loads/unloads Gauntlet layer
- Tracks scroll position for smart auto-scroll
- Detects manual scrolling vs automatic
- Resumes auto-scroll after 6 seconds idle
- Auto-repositions when party screen opens/closes
- Subscribes to RichTextWidget EventFire events for link clicks
- Applies faction-specific colors to kingdom links via FactionLinkColorizer
- Manages layer suspension during missions and conversations
- Provides `SuspendLayer()`/`ResumeLayer()` public methods for Harmony patches

**6. Faction Link Colorizer (`FactionLinkColorizer`)**
- Intercepts native messages and replaces generic `Link.Kingdom` styles with faction-specific styles
- Uses href (encyclopedia link) to identify faction, not display name (more reliable for localization)
- Maps kingdom StringIds to faction brush styles (Vlandia→red, Sturgia→blue, etc.)
- Preserves all other message formatting including hero/settlement links

**7. Harmony Patches**
- **`CombatLogConversationPatch`**: Hooks into MapScreen's conversation lifecycle to suspend/resume combat log layer
  - Patches `MapScreen.HandleMapConversationInit` (PostFix) to suspend layer when map conversations start
  - Patches `MapScreen.OnMapConversationOver` (PostFix) to resume layer when map conversations end
  - Uses native `ScreenManager.SetSuspendLayer()` approach (same as `GauntletMapBarView`)
- **`QuartermasterConversationScenePatch`**: Ensures correct scene selection (sea vs land) for quartermaster conversations
  - Patches `ConversationManager.OpenMapConversation` (Prefix) to inject lord's party data for scene determination

---

## Implementation Details

### File Structure

```
src/Features/Interface/
├── ViewModels/
│   ├── EnlistedCombatLogVM.cs           (Main ViewModel)
│   └── CombatLogMessageVM.cs            (Individual message)
├── Behaviors/
│   └── EnlistedCombatLogBehavior.cs     (Manager + scroll control + link events + visibility)
└── Utils/
    └── FactionLinkColorizer.cs          (Faction-specific link colors)

src/Mod.GameAdapters/Patches/
├── InformationManagerDisplayMessagePatch.cs    (Message routing)
├── CombatLogConversationPatch.cs               (Conversation visibility)
└── QuartermasterConversationScenePatch.cs      (QM scene selection)

GUI/Prefabs/Interface/
└── EnlistedCombatLog.xml                (Native-style UI)

GUI/Brushes/
└── EnlistedCombatLog.xml                (Custom brush with faction link styles)
```

### Key Features Breakdown

#### 1. Native-Style Text Rendering with Link Support
Uses a custom brush that extends native styling with encyclopedia link colors:
```xml
<RichTextWidget Brush="Enlisted.CombatLog.Text" 
                Brush.GlobalAlphaFactor="@AlphaFactor" />
```

**Enlisted.CombatLog.Text Brush Properties:**
- Font: Galahad
- Base Color: `#FAF4DEFF` (cream/beige)
- Text Glow: `#000000FF` (black)
- Text Outline: `#000000CC` (black with alpha)
- Outline Amount: 0.1
- Font Size: 20
- Link Styles:
  - Hero Links: `#8CDBC4FF` (teal-green) with hover brightness
  - Settlement Links: `#8CBEDEFF` (sky-blue) with hover brightness
  - Kingdom Links: `#EBD89CFF` (gold) with hover brightness

#### 2. Inactivity Fade
```csharp
// Fades to 35% opacity after 10 seconds of no activity
private const float InactivityFadeDelay = 10f;
private const float DimmedAlpha = 0.35f;
private const float FullAlpha = 1.0f;

// In Tick():
_timeSinceLastActivity += dt;
if (_timeSinceLastActivity >= InactivityFadeDelay)
{
    ContainerAlpha = DimmedAlpha;
}

// Resets on:
// - New message arrives
// - User hovers over log
// - User scrolls
```

#### 3. Smart Auto-Scroll
```csharp
// Behavior tracks scroll position every frame
var scrollbar = _scrollablePanel.VerticalScrollbar;
float currentScrollPosition = scrollbar.ValueFloat;

// Detect manual scroll
if (MathF.Abs(currentScrollPosition - _lastScrollPosition) > 0.01f)
{
    _shouldAutoScroll = false;  // Pause auto-scroll
    _timeSinceLastManualScroll = 0f;
}

// Resume after 6 seconds idle
if (_timeSinceLastManualScroll >= AutoScrollResumeDelay)  // 6f
{
    _shouldAutoScroll = true;
}

// Auto-scroll to bottom
if (_shouldAutoScroll)
{
    scrollbar.ValueFloat = scrollbar.MaxValue;
}
```

#### 4. Message Lifecycle
```csharp
// Real-time aging (not game time)
public class CombatLogMessageVM
{
    public DateTime CreatedAt { get; }  // DateTime.UtcNow
    
    public float GetAgeInSeconds()
    {
        return (float)(DateTime.UtcNow - CreatedAt).TotalSeconds;
    }
}

// Expiration check in Tick()
if (age >= MessageLifetimeSeconds)  // 300 seconds
{
    Messages.RemoveAt(i);
}
```

---

## XML Layout Structure

```xml
<Prefab>
  <Window>
    <!-- Main Container (400x280, bottom-right) -->
    <Widget Id="EnlistedCombatLogWidget" 
            DoNotAcceptEvents="false"    <!-- Allows mouse input -->
            SuggestedWidth="400" 
            SuggestedHeight="280"
            AlphaFactor="@ContainerAlpha"  <!-- Inactivity fade -->
            Command.HoverBegin="OnUserInteraction"
            Command.MouseScroll="OnUserInteraction">
      
      <!-- Scrollable Panel Container -->
      <Widget Id="ScrollablePanelContainer" 
              DoNotAcceptEvents="false">  <!-- Enables scroll events -->
        
        <!-- Scrollable Panel (no AutoScrollToBottom, manual control) -->
        <ScrollablePanel Id="ScrollablePanel"
                        InnerPanel="ClipRect\InnerPanel"
                        VerticalScrollbar="..\ScrollbarHolder\VerticalScrollbar">
          
          <!-- Clip Rect (keeps messages in bounds) -->
          <Widget Id="ClipRect" ClipContents="true">
            
            <!-- Message List (VerticalBottomToTop = newest at bottom) -->
            <ListPanel Id="InnerPanel"
                      DataSource="{Messages}"
                      StackLayout.LayoutMethod="VerticalBottomToTop">
              <ItemTemplate>
                
                <!-- Individual Message -->
                <RichTextWidget Id="MessageTextWidget"
                               DoNotAcceptEvents="false"  <!-- Enables link clicks -->
                               Text="@Text"
                               Brush="Enlisted.CombatLog.Text"  <!-- Custom brush with link styles -->
                               Brush.GlobalAlphaFactor="@AlphaFactor"
                               ClipContents="false" />  <!-- Allows shadows to extend -->
                <!-- Note: Links handled via EventFire, not Command.LinkClicked -->
                
              </ItemTemplate>
            </ListPanel>
          </Widget>
        </ScrollablePanel>
        
        <!-- Native-style Scrollbar (auto-hides) -->
        <ScrollbarWidget Id="VerticalScrollbar" 
                        Brush="SPChatlog.Scrollbar.Handle"
                        Color="#A37434FF"  <!-- Brownish track -->
                        AlphaFactor="0.7" />
      </Widget>
    </Widget>
  </Window>
</Prefab>
```

---

## Encyclopedia Linking

### Overview

Native Bannerlord messages already include clickable encyclopedia links for heroes, settlements, and kingdoms. The combat log preserves these native links and enhances them by applying **faction-specific colors** to kingdom names, making it easy to identify which faction is being mentioned at a glance.

**Link Colors:**
- **Heroes**: Cyan/teal (native Link.Hero style)
- **Settlements**: Cyan/teal (native Link.Settlement style)
- **Kingdoms**: Faction banner colors (Vlandia=red, Sturgia=blue, Battania=green, etc.)

### Technical Implementation

**1. Faction Link Colorization (`FactionLinkColorizer`)**

Native messages use a generic `Link.Kingdom` style for all kingdoms (renders as gold/white). The colorizer intercepts messages and replaces this with faction-specific styles:

```csharp
public static string ColorizeFactionLinks(string messageText)
{
    if (!messageText.Contains("Link.Kingdom"))
        return messageText;
    
    // Pattern matches: <a style="Link.Kingdom" href="event:Faction:kingdom_1"><b>Name</b></a>
    var pattern = @"<a style=""Link\.Kingdom"" href=""([^""]+)"">(.+?)</a>";
    
    return Regex.Replace(messageText, pattern, match =>
    {
        var href = match.Groups[1].Value;         // "event:Faction:kingdom_3"
        var content = match.Groups[2].Value;      // "<b>Khuzaits</b>"
        
        // Determine faction style from href (more reliable than display name)
        string factionStyle = GetFactionStyleFromHref(href);
        
        // Replace generic Link.Kingdom with faction-specific style
        return $"<a style=\"{factionStyle}\" href=\"{href}\">{content}</a>";
    });
}

private static string GetFactionStyleFromHref(string href)
{
    var stringId = href.ToLower();
    
    // href contains faction StringId, e.g., "event:Faction:kingdom_3"
    if (stringId.Contains("vlandia")) return "Link.FactionVlandia";
    if (stringId.Contains("sturgia")) return "Link.FactionSturgia";
    if (stringId.Contains("aserai")) return "Link.FactionAserai";
    if (stringId.Contains("khuzait")) return "Link.FactionKhuzait";
    if (stringId.Contains("battania")) return "Link.FactionBattania";
    if (stringId.Contains("empire_s")) return "Link.FactionEmpire_S";
    if (stringId.Contains("empire_w")) return "Link.FactionEmpire_W";
    if (stringId.Contains("north")) return "Link.FactionEmpire_N";
    
    return "Link.Kingdom"; // Fallback
}
```

**Why href matching instead of display name?**
- Native messages use demonyms: "Khuzaits", "Battanians", "northern Empire"
- Kingdom.Name is formal: "Khuzait Khanate", "Battania", "Northern Empire"
- The href ALWAYS contains the faction StringId, regardless of localization
- More reliable across all languages and message formats

**2. Link Click Handling (`EnlistedCombatLogBehavior`)**

Links are handled via the `RichTextWidget.EventFire` event (not `Command.LinkClicked`). This is the low-level event mechanism that RichTextWidget uses internally.

```csharp
// In InitializeUI()
_dataSource.Messages.ListChanged += OnMessagesListChanged;

// When new messages are added
private void OnMessagesListChanged(object sender, ListChangedEventArgs e)
{
    if (e.ListChangedType == ListChangedType.ItemAdded)
    {
        // Schedule subscription for next frame to ensure widgets are created
        _layer.AddLateUpdateAction(new Action<float>(dt => 
            SubscribeToMessageWidget(e.NewIndex)));
    }
}

// Find and subscribe to RichTextWidget
private void SubscribeToMessageWidget(int messageIndex)
{
    var messageWidget = listPanel.GetChild(messageIndex);
    
    // Find the RichTextWidget by type
    foreach (var child in messageWidget.Children)
    {
        if (child is RichTextWidget rtWidget)
        {
            rtWidget.EventFire += OnRichTextWidgetEvent;
            break;
        }
    }
}

// Handle link clicks
private void OnRichTextWidgetEvent(Widget widget, string eventName, object[] args)
{
    if (eventName == "LinkClick" && args.Length > 0 && args[0] is string linkHref)
    {
        // Strip "event:" prefix if present
        var encyclopediaLink = linkHref.StartsWith("event:") 
            ? linkHref.Substring(6) 
            : linkHref;
        
        // Open encyclopedia page
        Campaign.Current.EncyclopediaManager.GoToLink(encyclopediaLink);
        
        // Reset inactivity timer
        _dataSource.OnUserInteraction();
    }
}
```

**3. XML Link Event Wiring**

```xml
<RichTextWidget Id="MessageTextWidget"
                DoNotAcceptEvents="false"  <!-- Must be false to receive events -->
                Text="@Text"
                Brush="Enlisted.CombatLog.Text" />
```

Note: `Command.LinkClicked` is NOT used. RichTextWidget's link events go through `EventFire`, not the normal Gauntlet command system.

**4. Custom Brush Styles**

```xml
<!-- GUI/Brushes/EnlistedCombatLog.xml -->
<Brush Name="Enlisted.CombatLog.Text" Font="Galahad">
  <Styles>
    <Style Name="Default" FontColor="#FAF4DEFF" TextGlowColor="#000000FF" />
    
    <!-- Faction-specific kingdom colors (from spkingdoms.xml, brightened for readability) -->
    <Style Name="Link.FactionVlandia" FontColor="#C14030FF" />        <!-- Red -->
    <Style Name="Link.FactionSturgia" FontColor="#3854A0FF" />        <!-- Blue -->
    <Style Name="Link.FactionAserai" FontColor="#D47838FF" />         <!-- Orange -->
    <Style Name="Link.FactionKhuzait" FontColor="#5BC0B0FF" />        <!-- Teal -->
    <Style Name="Link.FactionBattania" FontColor="#5C7E3AFF" />       <!-- Green -->
    <Style Name="Link.FactionEmpire_N" FontColor="#73447EFF" />       <!-- Purple -->
    <Style Name="Link.FactionEmpire_W" FontColor="#D078A0FF" />       <!-- Pink -->
    <Style Name="Link.FactionEmpire_S" FontColor="#B8A0E8FF" />       <!-- Light Purple -->
    
    <!-- Generic fallback -->
    <Style Name="Link.Kingdom" FontColor="#FAF4DEFF" />               <!-- Cream (default) -->
  </Styles>
</Brush>
```

Each faction style includes `.MouseOver` (brightened) and `.MouseDown` (dimmed) states for visual feedback.

### Link Format

**RichTextWidget-Compatible Markup:**
- Format: `<a style="Link.Type" href="event:{EncyclopediaLink}"><b>Name</b></a>`
- Examples:
  - `<a style="Link.Hero" href="event:Hero:lord_1_1"><b>Derthert</b></a>`
  - `<a style="Link.Settlement" href="event:Settlement:town_V1"><b>Pravend</b></a>`
  - `<a style="Link.Kingdom" href="event:Faction:faction_1"><b>Vlandia</b></a>`

The `event:` prefix is required by Bannerlord's native encyclopedia system and is stripped before passing to `EncyclopediaManager.GoToLink()`.

### Why This Implementation Works

**Key Architectural Insights:**

1. **Native Messages Already Have Links**: Bannerlord's native messages include properly formatted encyclopedia links. No need to scan and add links ourselves - just enhance the existing ones with faction colors.

2. **href-Based Faction Matching**: Display names vary ("Khuzaits" vs "Khuzait Khanate"), but href always contains the faction StringId. Matching on `href.Contains("khuzait")` works reliably across all languages and message formats.

3. **Regex Link Style Replacement**: Find `<a style="Link.Kingdom"...>` tags and replace only the style attribute, preserving all other content including `<b>` tags and display text.

4. **EventFire vs Command.LinkClicked**: RichTextWidget doesn't fire `Command.LinkClicked` for `<a>` tag clicks. It uses the lower-level `EventFire` event with `eventName == "LinkClick"`.

5. **Dynamic Widget Subscription**: Widgets instantiated from `ItemTemplate` aren't available immediately. Using `ListChanged` event + `AddLateUpdateAction` ensures we subscribe after widget creation.

6. **Type-Based Lookup**: Finding widgets by `Id` doesn't work reliably for `ItemTemplate` instances. Iterating children and checking type (`is RichTextWidget`) is more robust.

7. **Brush File Deployment**: Custom brush files need `<CopyToOutputDirectory>Always</CopyToOutputDirectory>` in `.csproj` to ensure they copy on every build (not just when source is newer).

**Previous Issues Resolved:**
- Initially tried adding our own encyclopedia links (unnecessary - native already has them)
- Tried matching by kingdom display name (failed due to demonyms like "Khuzaits")
- Used `PreserveNewest` for brush deployment (didn't update when manually copied)

---

## Behavior Specification

### Message Flow

1. **Game generates message** → `InformationManager.DisplayMessage()`
2. **Harmony patch intercepts** → Checks `EnlistmentBehavior.IsEnlisted`
3. **If enlisted** → Routes to `EnlistedCombatLogBehavior.AddMessage()`
4. **Message added** → Creates `CombatLogMessageVM`, adds to list
5. **Auto-scroll enabled** → New message immediately visible
6. **After 10s idle** → Fades to 35% opacity
7. **After 5 minutes** → Message expires and removed

### User Interaction

| Action | Behavior |
|--------|----------|
| **New message arrives** | Auto-scroll to bottom, reset fade timer, full opacity |
| **User hovers over log** | Reset fade timer, return to full opacity |
| **User scrolls wheel** | Pause auto-scroll for 10s, reset fade timer, full opacity |
| **10s no interaction** | Resume auto-scroll, fade to 35% opacity |
| **User scrolls to bottom** | Treated as manual scroll, pause auto-scroll for 10s |

### Visibility Rules

| Player State | Custom Log | Native Log |
|-------------|-----------|-----------|
| Enlisted | ✓ Visible | ✗ Suppressed |
| Not Enlisted | ✗ Hidden | ✓ Normal |
| In Battle | ✗ Hidden | ✓ Normal |
| In Conversation | ✗ Hidden | ✓ Normal |
| In Tavern/Hall | ✗ Hidden | ✓ Normal |
| Prisoner | ✗ Hidden | ✓ Normal |

**Visibility Management System:**

The combat log uses a dual-detection system to hide during conversations and missions:

1. **Map Conversations (Harmony Patch):**
   - `CombatLogConversationPatch` hooks into `MapScreen.HandleMapConversationInit` and `MapScreen.OnMapConversationOver`
   - Calls `SuspendLayer()` when conversation starts, `ResumeLayer()` when it ends
   - Uses native `ScreenManager.SetSuspendLayer()` (same approach as `GauntletMapBarView`)

2. **Missions/Scenes (Tick Detection):**
   - `ManageLayerVisibility()` checks `Mission.Current != null` every tick
   - Covers battles, taverns, halls, arenas, and all other mission types
   - Automatically suspends layer when entering any mission

3. **Enlistment State:**
   - Always hidden when not enlisted, regardless of other conditions
   - Only visible on campaign map when enlisted and not in conversation/mission

```csharp
// Visibility logic in ManageLayerVisibility():
bool shouldSuspend = !isEnlisted || isInMission || _wasInConversation;

if (shouldSuspend && _isLayerActive)
{
    ScreenManager.SetSuspendLayer(_layer, true);  // Hide
    _isLayerActive = false;
}
else if (!shouldSuspend && !_isLayerActive)
{
    ScreenManager.SetSuspendLayer(_layer, false);  // Show
    _isLayerActive = true;
}
```

**Why This Approach:**
- Native game uses `SetSuspendLayer()` for hiding layers during conversations (`GauntletMapBarView`, `GauntletMapBasicView`)
- Suspending is cleaner than removing/re-adding layers (preserves state, avoids screen reference issues)
- Combining Harmony patches (conversation lifecycle) + tick detection (missions) provides complete coverage

---

## Configuration

**Constants in `EnlistedCombatLogVM`:**
```csharp
private const int MaxMessages = 50;
private const float MessageLifetimeSeconds = 300f;  // 5 minutes
private const float FadeStartSeconds = 270f;        // Start fade at 4.5min
private const float InactivityFadeDelay = 10f;      // Fade after 10s idle
private const float DimmedAlpha = 0.35f;
private const float FullAlpha = 1.0f;
```

**Constants in `EnlistedCombatLogBehavior`:**
```csharp
private const float AutoScrollResumeDelay = 10f;  // Resume scroll after 10s
```

**XML Dimensions:**
- Width: `400px`
- Height: `280px`
- Position: Bottom-right, `MarginRight="30"`, `MarginBottom="80"`
- Font Size: `20` (in Brush.FontSize)

**Brush File:**
- Location: `GUI/Brushes/EnlistedCombatLog.xml`
- Must be included in `.csproj` as `<Content Include="..."/>` for deployment

---

## Message Styling

Messages use existing color scheme with native styling:

| Message Type | Color | Use Case |
|-------------|-------|----------|
| Success | `#44AA44` | Excellent outcomes, successful events |
| Positive | `#88CC88` | Good outcomes, minor gains |
| Neutral | `#CCCCCC` | Normal outcomes, information |
| Warning | `#CCCC44` | Poor outcomes, minor issues |
| Failure | `#DB0808` | Failed decisions, serious problems |
| Order Status | `#4A9EFF` | Order notifications |

All colors rendered with:
- Black text glow (`#000000FF`)
- Black text outline (`#000000CC`)
- Outline amount: `0.1`
- Per-message alpha fading (last 30 seconds of life)

---

## Integration Points

### Existing Systems

**1. News Reporting System:**
- Combat log: Immediate feedback (instant display)
- News system: Persistent records (Recent Activities)
- Complementary, not redundant

**2. Event System:**
- Decision results appear in combat log instantly
- Regular event outcomes go to Recent Activities only

**3. Order System:**
- Order status changes display in log (blue colored)
- Completion/failure notifications immediate

**4. Routine System:**
- Routine outcomes send to combat log
- Shows flavor text with XP gains/losses

---

## Edge Cases

### 1. Rapid Message Spam
**Scenario:** 20+ messages in quick succession  
**Behavior:** All added, auto-scrolls to newest, oldest beyond 50 removed

### 2. User Scrolling Up
**Scenario:** User scrolls to read old messages while new ones arrive  
**Behavior:** Auto-scroll paused, new messages added off-screen, resumes after 10s

### 3. Battle Transitions
**Scenario:** Enter battle while enlisted  
**Behavior:** Log hides, battle uses native system, log reappears after battle

### 4. Save/Load
**Scenario:** Save with messages, then load  
**Behavior:** Messages not persisted (transient), log starts empty

### 5. Screen Resolution
**Scenario:** Non-standard resolution or UI scaling  
**Behavior:** Fixed positioning maintains readability, right-alignment keeps it on-screen

---

## Acceptance Criteria

### Core Functionality
- [x] Custom combat log displays on right side when enlisted
- [x] Shows last 50 messages with 5-minute expiration
- [x] Scrollable with mouse wheel
- [x] Correct color coding per message type
- [x] Native log suppressed while enlisted
- [x] Native log resumes when not enlisted

### Smart Behavior
- [x] Auto-scrolls to newest messages
- [x] Pauses auto-scroll when user manually scrolls
- [x] Resumes auto-scroll after 6 seconds idle
- [x] Fades to 35% after 10 seconds of no activity
- [x] Returns to full opacity on interaction

### Interactive Features
- [x] Hero names are clickable encyclopedia links
- [x] Settlement names are clickable encyclopedia links
- [x] Links open corresponding encyclopedia pages
- [x] Link clicks count as user interaction

### Visual Quality
- [x] Native `ChatLog.Text` brush styling
- [x] Transparent background (no sprite)
- [x] Black text shadows and outlines
- [x] Proper text clipping within bounds
- [x] Native-style scrollbar that auto-hides

### State Transitions
- [x] Appears when player enlists
- [x] Hides when player leaves army
- [x] Hides during battles
- [x] Hides during map conversations (quartermaster, lords, etc.)
- [x] Hides during mission conversations (sea conversations)
- [x] Hides in taverns, halls, and all other missions/scenes
- [x] Smooth transitions using native SetSuspendLayer()

### Performance
- [x] No FPS impact with 50 messages
- [x] Smooth scrolling
- [x] No memory leaks
- [x] Fast message addition

---

## Implementation Issues & Resolutions

### Bug #1: Messages Expiring Immediately

**Root Cause:** Used `CampaignTime` (game time) instead of real-time for age calculation.

**Resolution:** Changed to `DateTime.UtcNow`:
```csharp
// BEFORE (Broken)
public CampaignTime CreatedAt { get; }

// AFTER (Fixed)
public DateTime CreatedAt { get; }
public float GetAgeInSeconds() => (float)(DateTime.UtcNow - CreatedAt).TotalSeconds;
```

### Bug #2: UI Not Rendering

**Root Cause:** XML prefab not deployed to game directory.

**Resolution:** Added to `.csproj`:
```xml
<Content Include="GUI\Prefabs\Interface\EnlistedCombatLog.xml">
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</Content>
```

### Bug #3: Background Visible, Text Unreadable

**Root Cause:** Used `SPGeneral.Text` brush and had a background sprite.

**Resolution:** 
- Removed background widget entirely
- Changed to `ChatLog.Text` brush
- Added explicit black glow/outline for readability

### Bug #4: Scrolling Doesn't Work

**Root Cause:** Layer configured with `DoNotAcceptEvents="true"`, input restrictions not set.

**Resolution:**
```csharp
// In InitializeUI():
_layer.InputRestrictions.SetInputRestrictions(false);

// In XML:
DoNotAcceptEvents="false"  // On main widget and container
```

### Bug #5: Text Overflowing, No Clipping

**Root Cause:** Missing `ClipContents="true"` on container widgets.

**Resolution:** Added to both main container and ClipRect widget.

### Bug #6: Wrong Message Order

**Root Cause:** Used `VerticalTopToBottom`, oldest messages at top.

**Resolution:** Changed to `VerticalBottomToTop` to match native behavior.

### Bug #7: Auto-Scroll Forced on New Messages

**Root Cause:** `AddMessage()` was forcing `_shouldAutoScroll = true` every time a new message arrived, overriding user's manual scroll pause.

**Resolution:** Removed forced auto-scroll from `AddMessage()`. Now respects the 6-second timer after manual scrolling.

### Bug #8: Party Screen Overlap

**Root Cause:** Combat log didn't reposition when party screen opened, causing UI overlap.

**Resolution:** Added `PartyState` detection in `EnlistedCombatLogBehavior.OnTick()`. Log moves up 180px when party screen opens, returns to normal position when closed.

```csharp
// Detect party screen and reposition
bool isPartyScreenOpen = Game.Current?.GameStateManager?.ActiveState is PartyState;
UpdatePositioning(isPartyScreenOpen);

// In ViewModel:
PositionYOffset = isMenuOpen ? -280f : -100f;
```

### Bug #9: Encyclopedia Links Not Clickable (Command.LinkClicked)

**Root Cause:** Used `Command.LinkClicked` binding on `RichTextWidget`, which is not how RichTextWidget fires link events. RichTextWidget uses the low-level `EventFire` mechanism instead.

**Resolution:** Changed to subscribe to `Widget.EventFire` event and check for `eventName == "LinkClick"`.

```csharp
// Wrong approach
<RichTextWidget Command.LinkClicked="OnLinkClick" />

// Correct approach
richTextWidget.EventFire += OnRichTextWidgetEvent;

private void OnRichTextWidgetEvent(Widget widget, string eventName, object[] args)
{
    if (eventName == "LinkClick" && args[0] is string linkHref) { ... }
}
```

### Bug #10: Widget Lookup Failed for ItemTemplate Instances

**Root Cause:** Tried to find `RichTextWidget` by ID using `FindChild("MessageTextWidget", true)`, which doesn't work for widgets instantiated from `ItemTemplate` in a `ListPanel`.

**Resolution:** Changed to type-based lookup, iterating through `messageWidget.Children` and checking `child is RichTextWidget`.

```csharp
// Wrong approach
var richTextWidget = messageWidget.FindChild("MessageTextWidget", true);

// Correct approach
foreach (var child in messageWidget.Children)
{
    if (child is RichTextWidget rtWidget)
    {
        richTextWidget = rtWidget;
        break;
    }
}
```

### Bug #11: Links Not Styled (Rendering in Message Color)

**Root Cause:** `Brush.FontColor="@MessageColor"` binding in XML was overriding all inline styles from `<a style="Link.Hero">` tags.

**Resolution:** Removed the `Brush.FontColor` binding from `RichTextWidget` in XML, allowing `<a>` tag styles to apply their own colors from the brush definition.

```xml
<!-- Wrong approach -->
<RichTextWidget Brush="Enlisted.CombatLog.Text" 
                Brush.FontColor="@MessageColor" />  <!-- Overrides all styles -->

<!-- Correct approach -->
<RichTextWidget Brush="Enlisted.CombatLog.Text" />  <!-- Allows <a> styles to work -->
```

### Bug #12: Custom Brush File Not Deployed

**Root Cause:** Created `GUI/Brushes/EnlistedCombatLog.xml` but didn't add it to `.csproj`, so it wasn't copied to the game directory during build.

**Resolution:** Added `<Content Include="GUI\Brushes\EnlistedCombatLog.xml"/>` to `.csproj` to ensure deployment.

```xml
<ItemGroup>
  <Content Include="GUI\Brushes\EnlistedCombatLog.xml">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```

### Bug #13: Prefab File Not Updated in Game Directory

**Root Cause:** Modified `GUI/Prefabs/Interface/EnlistedCombatLog.xml` to use new brush, but game was running during build, preventing file copy. Game continued using cached old version.

**Resolution:** Manually copied updated prefab to game directory after closing game. Verified `.csproj` had correct `<Content Include>` entry for automatic future deployments.

### Bug #14: Combat Log Visible During Conversations and Missions

**Root Cause:** CampaignBehaviors don't receive MapView lifecycle callbacks (`OnMapConversationStart/Over`). Initial attempts to detect conversations via `ConversationManager.IsConversationFlowActive` or `Mission.Mode` didn't work properly. Tried removing/re-adding layers from `ScreenManager.TopScreen`, but screen reference changes during conversations.

**Resolution:** Created `CombatLogConversationPatch` Harmony patch to hook into the same MapScreen methods that notify native MapViews:
- Patches `MapScreen.HandleMapConversationInit` (PostFix) → calls `SuspendLayer()`
- Patches `MapScreen.OnMapConversationOver` (PostFix) → calls `ResumeLayer()`
- Uses native `ScreenManager.SetSuspendLayer()` (same approach as `GauntletMapBarView`)
- Combined with `Mission.Current != null` check for battles/taverns/halls
- Stored `_ownerScreen` reference instead of using `ScreenManager.TopScreen` (which changes)

**Key Insights:**
1. Native game uses `SetSuspendLayer()` to hide layers during conversations, not remove/add
2. MapViews receive `OnMapConversationStart/Over` callbacks, but custom behaviors need Harmony patches
3. Suspending is cleaner than removing (preserves state, avoids screen reference issues)
4. Combining Harmony patches (conversations) + tick detection (missions) provides complete coverage

---

## Changelog

**2025-01-01 (Evening - Conversation/Mission Visibility):**
- ✅ **Proper visibility management during conversations and missions**
- Created `CombatLogConversationPatch` to hook into MapScreen's conversation lifecycle
- Uses native `ScreenManager.SetSuspendLayer()` approach (same as GauntletMapBarView)
- Patches `MapScreen.HandleMapConversationInit` and `MapScreen.OnMapConversationOver`
- Added `SuspendLayer()` and `ResumeLayer()` public methods to behavior
- Combined Harmony patches (conversations) + tick detection (missions) for complete coverage
- Combat log now properly hides during:
  - Map conversations (quartermaster, lords, etc.)
  - Mission-based conversations (sea conversations)
  - Battles, taverns, halls, arenas
  - Any other mission/scene
- **Key Discovery**: Native MapViews receive `OnMapConversationStart/Over` callbacks, but CampaignBehaviors don't
- **Key Discovery**: Harmony patches on MapScreen methods provide the same lifecycle hooks
- **Key Discovery**: `SetSuspendLayer()` is cleaner than add/remove (preserves state, no screen reference issues)

**2025-01-01 (Afternoon - Faction-Colored Links):**
- ✅ **Faction-specific kingdom colors implemented** - Each kingdom displays in its banner color
- Native messages already contain encyclopedia links (heroes, settlements, kingdoms)
- Created `FactionLinkColorizer` to replace generic `Link.Kingdom` with faction-specific styles
- Uses href (encyclopedia link) to identify faction, not display name (works across all localizations)
- Created custom brush file (`GUI/Brushes/EnlistedCombatLog.xml`) with faction-specific link colors from `spkingdoms.xml`
- Link clicks handled via `RichTextWidget.EventFire` event (not `Command.LinkClicked`)
- Dynamic widget subscription via `ListChanged` event and `AddLateUpdateAction`
- Links count as user interaction (resets inactivity timer)
- **Key Discovery**: Native messages use demonyms ("Khuzaits") not formal names ("Khuzait Khanate")
- **Key Discovery**: href contains faction StringId, making it more reliable for matching than display text
- **Key Discovery**: `.csproj` needs `<CopyToOutputDirectory>Always</CopyToOutputDirectory>` for reliable XML deployment

**2025-01-01 (Late Session):**
- ✅ **Fixed scroll pause behavior** - Manual scrolling now properly pauses auto-scroll
- ✅ **Added party screen repositioning** - Log moves up when party screen opens
- ✅ **Fixed duplicate routine messages** - Removed personal feed from Company Reports section
- ✅ **Reduced auto-scroll resume delay** - Changed from 10s to 6s for better responsiveness
- All scroll and positioning bugs resolved

**2025-01-01 (Initial):**
- ✅ **Feature fully implemented and polished**
- Implemented native `ChatLog.Text` brush styling
- Added inactivity fade (35% after 10s idle)
- Implemented smart auto-scroll with pause on manual scroll
- Added mouse wheel scrolling support
- Fixed all UI rendering and clipping issues
- Removed background for transparent native look
- Achieved professional, native-quality appearance
- Completed all acceptance criteria

---

## Future Enhancements

### Potential Additions

**1. Message Filtering:**
- Toggle switches for message types
- Per-category filtering in settings

**2. Message History Panel:**
- Full-screen overlay for extended history
- Search/filter capability
- Export to text file

**3. Visual Improvements:**
- Category icons next to messages
- Fade-in animation for new messages
- Configurable fade timing

**4. Configuration:**
- Customizable dimensions
- Adjustable message lifetime
- Positioning options

---

## Related Documentation

- **[News Reporting System](news-reporting-system.md)** - Persistent event display
- **[UI Systems Master](ui-systems-master.md)** - UI overview
- **[Color Scheme](color-scheme.md)** - Message colors
- **[Event System](../Content/event-system-schemas.md)** - Event outcomes
- **[Order System](../Content/order-system-master.md)** - Order notifications
