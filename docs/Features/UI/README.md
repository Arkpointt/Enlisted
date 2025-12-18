# UI Features Documentation

**Purpose:** Documentation for all user interface systems in the Enlisted mod.

---

## Quick Navigation

| Document | System | Status |
|----------|--------|--------|
| [menu-interface.md](menu-interface.md) | Text menu system (GameMenu) | ✅ Implemented |
| [event-ui.md](event-ui.md) | Modern event screens (Gauntlet) | ✅ Implemented |
| [camp-tent.md](camp-tent.md) | Camp hub features | ✅ Implemented |
| [dialog-system.md](dialog-system.md) | Conversation system | ✅ Implemented |
| [quartermaster.md](quartermaster.md) | Equipment system | ✅ Implemented |
| [news-dispatches.md](news-dispatches.md) | News feed system | ✅ Implemented |

---

## System Overview

### Text Menus (GameMenu)
**Document:** [menu-interface.md](menu-interface.md)  
**Implementation:** `src/Features/Interface/Behaviors/EnlistedMenuBehavior.cs`, `src/Features/Camp/CampMenuHandler.cs`

Classic Bannerlord text menus with modern styling:
- Main menu (`enlisted_status`) - Service status and navigation
- Camp activities (`enlisted_camp_activities`) - Training, tasks, social
- Lance menu (`enlisted_lance`) - Lance roster and relationships
- Medical menu (`enlisted_medical`) - Treatment options
- Camp hub (`enlisted_camp_hub`) - Camp-facing navigation

**Key Features:**
- Modern icons via `LeaveType`
- Hover tooltips
- Culture-appropriate backgrounds
- Escalation track displays
- Dynamic text system

---

### Modern Event Screens (Gauntlet)
**Document:** [event-ui.md](event-ui.md)  
**Implementation:** `src/Features/Lances/UI/LanceLifeEventScreen.cs`, `src/Features/Lances/UI/ModernEventPresenter.cs`

Full-screen Gauntlet UI for Lance Life events:
- Character portraits
- Visual choice buttons
- Live escalation tracking
- Rich text formatting
- Smooth animations

**Key Features:**
- Modern card layout with semi-transparent overlay
- Scene visualization (portraits, images)
- Risk indicators (color-coded choices)
- Advanced visual effects (transitions, hover, particles)
- Cinematic effects (vignette, blur, camera shake)

---

### Camp Features
**Document:** [camp-tent.md](camp-tent.md)  
**Implementation:** `src/Features/CommandTent/Core/RetinueManager.cs`, `src/Features/CommandTent/Core/ServiceRecordManager.cs`

Personal hub for enlisted soldiers:
- Service records (faction-specific and lifetime)
- Pay & pension status
- Discharge actions (final muster)
- Personal retinue management (Tier 4+)
- Companion battle assignments

---

### Dialog System
**Document:** [dialog-system.md](dialog-system.md)  
**Implementation:** `src/Features/Conversations/Behaviors/EnlistedDialogManager.cs`

Centralized conversation manager:
- Enlistment dialogs (kingdom + minor faction variants)
- Status dialogs
- Management dialogs (promotions, equipment)
- Army leader restriction
- Immediate menu activation after enlistment

---

### Equipment System
**Document:** [quartermaster.md](quartermaster.md)  
**Implementation:** `src/Features/Equipment/Behaviors/QuartermasterManager.cs`, `src/Features/Equipment/Behaviors/EquipmentManager.cs`

Enlisted equipment management:
- Persistent Quartermaster Hero NPC (6 archetypes)
- Formation-driven equipment availability
- Culture-appropriate gear
- Tier-unlocked progression
- Relationship discounts (5-15%)
- Enlistment bag check (stow/sell/keep)
- Baggage train stash (fatigue cost by tier)

---

### News Feed System
**Document:** [news-dispatches.md](news-dispatches.md)  
**Implementation:** `src/Features/Interface/Behaviors/EnlistedNewsBehavior.cs`

Two-feed news architecture:
- **Kingdom News** (in `enlisted_status`): Kingdom-wide strategic events
- **Personal News** (in `enlisted_camp_activities`): Your immediate service context

**Key Features:**
- Event-driven generation (not tick-based)
- Priority system (2-day important, 1-day minor)
- Dedupe by StoryKey
- Read-only (mod-safe)
- Battle casualty classification (clean/costly/pyrrhic)

---

## Technical Guides

For building UI:
- **[Gauntlet UI Playbook](../../research/gauntlet-ui-screens-playbook.md)** - How to build Gauntlet screens safely

---

## Integration Patterns

### Menu → Gauntlet Screen
```csharp
// From text menu option
Campaign.Current.GameMenuManager.SetNextMenu("some_menu");
CampManagementScreen.Open(); // Opens Gauntlet overlay
```

### Gauntlet Screen → Menu
```csharp
// From Gauntlet screen close
ScreenManager.PopScreen();
GameMenu.ActivateGameMenu("enlisted_status"); // Return to text menu
```

### Event Presentation
```csharp
// Modern UI (Lance Life Events)
ModernEventPresenter.TryShowWithFallback(eventDef, enlistment, useModernUI: true);

// Basic inquiry popup
InquiryData inquiry = new InquiryData(...);
InformationManager.ShowInquiry(inquiry);
```

### News Integration
```csharp
// Subscribe to campaign events
CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);

// Generate news item
EnlistedNewsBehavior.Instance?.AddNewsItem(newsItem);

// Display in menu
string kingdomNews = EnlistedNewsBehavior.Instance?.BuildKingdomNewsSection(3);
string personalNews = EnlistedNewsBehavior.Instance?.BuildPersonalNewsSection(2);
```

---

## UI Architecture Philosophy

### 1. Two UI Technologies
- **Text Menus (GameMenu):** Simple, stable, vanilla-friendly
- **Gauntlet Screens (ScreenBase):** Rich, visual, custom overlays

### 2. When to Use Each

**Use Text Menus when:**
- Navigation/hub systems
- Lists of options
- Simple status displays
- Vanilla-style interactions

**Use Gauntlet Screens when:**
- Complex layouts needed
- Visual presentation critical
- Interactive elements (drag/drop, tabs)
- Real-time data updates

### 3. Integration Points
- Text menus can launch Gauntlet screens
- Gauntlet screens can return to text menus
- Both can display same data sources
- Keep logic in Behaviors, not in UI

### 4. Modern Styling
All UI uses consistent modern styling:
- Icons via `LeaveType` (text menus) or sprites (Gauntlet)
- Culture-appropriate backgrounds
- Hover tooltips
- Color-coded indicators
- Professional typography

---

## Performance Considerations

### Text Menus
- Lightweight by design
- No rendering overhead
- Fast menu switching
- Text variable updates are cheap

### Gauntlet Screens
- Heavier than text menus
- Load sprite categories explicitly
- Dispose resources properly
- Minimize redraws
- Use lazy loading for portraits

### News System
- Event-driven (not tick-based)
- Bounded history (60 kingdom, 20 personal)
- Priority backlog system
- Read-only (no world-state writes)

---

## Debugging

### Text Menu Issues
```csharp
// Enable menu logging
[CommandLineFunctionality.CommandLineArgumentFunction("enlisted_debug_menus", "enlisted")]
public static string DebugMenus(List<string> args)
{
    EnlistedMenuBehavior.Instance?.EnableDebugLogging();
    return "Menu debug logging enabled";
}
```

### Gauntlet Screen Issues
```csharp
// Check sprite categories loaded
if (_townManagementSpriteCategory == null)
{
    InformationManager.DisplayMessage(new InformationMessage(
        "Failed to load sprite categories", Colors.Red));
}

// Check ViewModel state
InformationManager.DisplayMessage(new InformationMessage(
    $"VM state: {_dataSource?.DebugState()}", Colors.Cyan));
```

### News Feed Issues
```csharp
// Check news generation
[CommandLineFunctionality.CommandLineArgumentFunction("enlisted_debug_news", "enlisted")]
public static string DebugNews(List<string> args)
{
    EnlistedNewsBehavior.Instance?.DumpNewsHistory();
    return "News history dumped to log";
}
```

---

## Related Documentation

- **[Features Index](../index.md)** - All feature documentation
- **[Implementation Roadmap](../../ImplementationPlans/implementation-roadmap.md)** - Future UI work
- **[Gauntlet UI Playbook](../../research/gauntlet-ui-screens-playbook.md)** - Technical guide

---

## Folder History

**December 18, 2025:**
- Consolidated `modern-event-ui.md` + `advanced-visual-effects.md` → `event-ui.md`
- Both documents covered the same system (LanceLifeEventScreen)
- Advanced effects section preserved as part of consolidated doc
- Created this README for navigation

**Files (6 total):**
- ✅ `menu-interface.md` (923 lines) - Text menu system
- ✅ `event-ui.md` (640 lines) - Modern event screens [CONSOLIDATED]
- ✅ `camp-tent.md` (462 lines) - Camp features
- ✅ `dialog-system.md` (326 lines) - Conversation system
- ✅ `quartermaster.md` (213 lines) - Equipment system
- ✅ `news-dispatches.md` (674 lines) - News feed system

---

**Document Maintained By:** Enlisted Development Team  
**Last Updated:** December 18, 2025  
**Status:** Active - All Systems Implemented

