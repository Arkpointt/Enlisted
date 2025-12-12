# Menu Interface System

## Quick Reference

| Menu | Purpose | Access |
|------|---------|--------|
| Enlisted Status | Main service menu | After enlistment, from camp |
| Duty Selection | Choose daily assignment | Enlisted Status → "Report for Duty" |
| Quartermaster | Equipment selection | Enlisted Status → "Visit Quartermaster" |
| Camp ("My Camp") | Service records, pay/pension status, discharge actions, retinue | Enlisted Status → "Camp" |

## Index

### Feature Documentation
- [Overview](#overview) - System purpose and key features
- [Modern UI Styling](#modern-ui-styling) - Icons, tooltips, backgrounds, audio
- [How It Works](#how-it-works) - Menu structure and navigation
  - [Main Enlisted Status Menu](#main-enlisted-status-menu)
  - [Duty Selection Interface](#duty-selection-interface)
  - [Menu Navigation](#menu-navigation)
-  - [Popups & Incidents (not menus)](#popups--incidents-not-menus)
- [Technical Details](#technical-details) - Implementation specifics
  - [Menu Structure](#menu-structure)
  - [Dynamic Text System](#dynamic-text-system)
  - [Tier-Based Availability](#tier-based-availability)
- [Edge Cases](#edge-cases) - Problem scenarios and solutions
  - [Menu State Consistency](#menu-state-consistency)
  - [Tier Progression](#tier-progression)
  - [Duty Selection Persistence](#duty-selection-persistence)
  - [Menu Activation Timing](#menu-activation-timing)
  - [Time Control Preservation](#time-control-preservation)

### Bannerlord API Reference
- [Bannerlord Menu APIs](#bannerlord-menu-apis) - Native API patterns
  - [Menu Types](#menu-types) - MenuAndOptionType values
  - [Time Control Behavior](#time-control-behavior) - Vanilla behavior and preservation
  - [Menu Registration](#menu-registration) - Creating menus
  - [Background and Audio](#background-and-audio) - Visual/audio setup
  - [Text Variables](#text-variables) - Dynamic text system
  - [Menu Navigation](#menu-navigation-api) - Switching between menus
  - [Menu Options](#menu-options) - Adding options with icons
  - [LeaveType Icons](#leavetype-icons) - Icon reference table
  - [Popup Dialogs](#popup-dialogs) - Inquiry dialogs

### Development Reference
- [API Reference](#api-reference) - Enlisted-specific APIs
- [Debugging](#debugging) - Logging and troubleshooting

---

## Overview

Professional military menu interface providing comprehensive service management with organized duty/profession selection, detailed descriptions, and tier-based progression. All menus feature modern styling with icons, tooltips, ambient audio, and culture-appropriate backgrounds.

**Key Features:**
- Modern icons on all menu options via `LeaveType`
- Hover tooltips explaining each option's function and bonuses
- Culture-appropriate background meshes
- Ambient camp audio for immersion
- Clean section organization (Duties vs. Professions)
- Tier-based progression with helpful unlock messages
- Real-time status updates
- Connected daily XP processing

**File:** `src/Features/Interface/Behaviors/EnlistedMenuBehavior.cs`

---

## Modern UI Styling

All menus use a consistent modern styling system for professional appearance.

### Menu Option Icons

Each menu option has a `LeaveType` that displays an appropriate icon:

| Option | LeaveType | Icon Purpose |
|--------|-----------|--------------|
| Master at Arms | `TroopSelection` | Troop management |
| Visit Quartermaster | `Trade` | Equipment/trading |
| My Lord... | `Conversation` | Dialog |
| Visit Settlement | `Submenu` | Navigation |
| Report for Duty | `Manage` | Management |
| Ask for Leave | `Leave` | Exit action |
| Desert the Army | `Escape` | Warning/danger (immediate abandonment) |
| (Discharge via Camp) | `Manage` | Managed separation (Pending Discharge → Final Muster) |

### Tooltips

Every menu option displays a tooltip on hover explaining its function:
- Main menu options show brief descriptions
- Duty options explain skill bonuses and benefits
- Profession options show tier requirements if locked, or bonuses if unlocked

Tooltip strings are localized in `ModuleData/Languages/enlisted_strings.xml`.

### Background and Audio

Menus use `[GameMenuInitializationHandler]` attributes to set:
- **Background Mesh**: Culture-appropriate encounter background from lord's kingdom
- **Ambient Sound**: `event:/map/ambient/node/settlements/2d/camp_army`
- **Panel Sound**: `event:/ui/panels/settlement_camp`

### Text Formatting

- Section headers use em-dashes: `— DUTIES —` instead of ASCII box characters
- Currency displays use inline gold icons: `{GOLD_ICON}`
- Bullet lists use modern markers: `•` for selected, `○` for available

---

## How It Works

### Main Enlisted Status Menu

**Menu ID:** `enlisted_status` (WaitGameMenu)

**Options with Icons and Tooltips:**
| Option | Icon | Tooltip |
|--------|------|---------|
| Master at Arms | TroopSelection | Select your troop type and equipment loadout based on your current tier |
| Visit Quartermaster | Trade | Purchase equipment and manage party supplies |
| My Lord... | Conversation | Speak with nearby lords for quests, news, and relation building |
| Visit Settlement | Submenu | Enter the settlement while your lord is present |
| Report for Duty | Manage | Select your daily duty and profession for bonuses and special abilities |
| Ask commander for leave | Leave | Request temporary leave from service |
| Desert the Army | Escape | WARNING: Immediate abandonment with severe penalties |

**Features:**
- Modern icons on every option for visual clarity
- Tooltips on hover explaining each action
- Culture-appropriate background mesh from lord's kingdom
- Ambient camp audio for immersion
- Professional status display with real-time updates
- Clean navigation to sub-menus
- Status information (tier, XP, days served, etc.)
- Pay is handled via a muster ledger and pay muster (see Pay System doc); discharge is managed from Camp.

### Duty Selection Interface

**Menu ID:** `enlisted_duty_selection` (WaitGameMenu)

**Section Organization:**
- **— DUTIES —** section header with em-dash styling
- **— PROFESSIONS —** section header with em-dash styling
- Visual spacer between sections for clean layout

**Duty Selection (Available T1+) with Icons and Tooltips:**
| Duty | Icon | Tooltip |
|------|------|---------|
| Enlisted | Continue | Standard military service. Train with your formation and earn base wages |
| Forager | Trade | Gather food and supplies for the army. Earn bonus XP and improved wages |
| Sentry | DefendAction | Guard the camp perimeter. Improved detection and bonus lord relations |
| Messenger | Mission | Deliver messages and scout ahead. Bonus riding/athletics XP |
| Pioneer | SiegeAmbush | Build fortifications and siege works. Bonus engineering XP |

**Profession Selection (Available T3+) with Icons and Tooltips:**
| Profession | Icon | Tooltip (Unlocked) | Tooltip (Locked) |
|------------|------|-------------------|------------------|
| Quarterhand | Trade | 15% better trade prices and +50 carry capacity | Requires Tier 3 |
| Field Medic | Manage | Faster healing, bonus medicine XP, morale boost | Requires Tier 3 |
| Siegewright's Aide | SiegeAmbush | Faster siege construction, bonus engineering XP | Requires Tier 3 |
| Drillmaster | OrderTroopsToAttack | Bonus leadership XP and troop morale | Requires Tier 3 |
| Saboteur | Raid | Bonus roguery XP and special mission access | Requires Tier 3 |

**Description System:**
- Top of menu shows detailed descriptions for currently selected duty/profession
- "None" shows simple text when no profession selected
- Rich military context explaining daily activities and skill training

**Checkmark System:**
- Dynamic checkmarks (✓/○) showing current selections
- Updates in real-time when selection changes
- Visual feedback for active assignments

**Tier-Locked Professions:**
- Options below Tier 3 are grayed out (`args.IsEnabled = false`)
- Tooltip shows "Requires Tier 3 to unlock this profession"
- Still visible to show players what they're working toward

### Menu Navigation

**Flow:**
```
Enlisted Status Menu
    ├── Master at Arms → Troop Selection Popup
    ├── Visit Quartermaster → Equipment Selection Menu
    ├── Camp → Service Records / Pay & Pension / Discharge / Retinue / Companions
    ├── My Lord... → Dialog System
    ├── Report for Duty → Duty Selection Menu
    └── Ask commander for leave → Leave Request Dialog
```

### Popups & Incidents (not menus)

Some player-facing UI in Enlisted is **not** a GameMenu. These are inquiry/incident-style popups that fire when it is safe (no battle/encounter/captivity), and they exist to simulate “camp life” without requiring scenes.

Key popups:
- **Enlistment Bag Check** (first enlistment)
  - Fires ~12 in-game hours after enlistment when safe.
  - Doc: **[Enlistment System](../Core/enlistment.md)** (Bag Check section) and **[Quartermaster](quartermaster.md)**
- **Pay Muster** (periodic)
  - Fires when the muster ledger reaches payday and it is safe to show UI.
  - Doc: **[Pay System](../Core/pay-system-rework.md)**
- **Lance Life events** (text-based camp activities)
  - “Viking Conquest style” story popups tied to lance identity and tier gating.
  - Doc: **[Lance Life](../Gameplay/lance-life.md)**
- **Camp Life Simulation hooks** (conditions that influence other UI)
  - Not a single popup by itself; it is the condition layer that can drive future popups and menu availability (Quartermaster mood/stockouts, delayed pay/IOUs, etc.).
  - Doc: **[Camp Life Simulation](../Gameplay/camp-life-simulation.md)**

## Discharge paths (where they live)

There are **two** ways to leave service:

1. **Desert the Army** (from `enlisted_status`)
   - Immediate exit with penalties (crime/relation). This is the “panic button”.

2. **Request Discharge (Final Muster)** (from Camp)
   - Sets a pending discharge state and resolves at the next pay muster (“Final Muster”).
   - This is the intended “retire properly” path and is where pensions/severance are awarded.

**Navigation Features:**
- Back button at top for easy access
- Leave option at bottom of main menu
- Close button in Master at Arms popup
- Consistent menu state across navigation

---

## Technical Details

### Menu Structure

**Main Menu:**
```csharp
// Menu ID: enlisted_status
// Type: WaitGameMenu
// Purpose: Main service management hub
```

**Duty Selection:**
```csharp
// Menu ID: enlisted_duty_selection
// Type: WaitGameMenu
// Purpose: Choose daily assignment
// Features: Section headers, checkmarks, descriptions
```

**Menu Registration:**
- Registered in `EnlistedMenuBehavior.OnSessionLaunched()`
- Menu options added via `AddGameMenuOption()`
- Dynamic text variables set via `SetDynamicMenuText()`

### Dynamic Text System

**Purpose:** Real-time updates showing current selections

**Implementation:**
```csharp
// Set dynamic text for menu options
private void SetDynamicMenuText()
{
    var currentDuty = EnlistmentBehavior.Instance?.SelectedDuty ?? "None";
    var currentProfession = EnlistmentBehavior.Instance?.SelectedProfession ?? "None";
    
    // Update checkmarks based on current selection
    // Format: "✓ Duty Name" or "○ Duty Name"
}
```

**Checkmark Logic:**
- ✓ (checkmark) for currently selected duty/profession
- ○ (circle) for available but not selected
- Updates automatically when selection changes

### Tier-Based Availability

**Duties:**
- All available at Tier 1+
- No restrictions

**Professions:**
- Visible at Tier 1-2 (locked with helpful messages)
- Selectable at Tier 3+
- Tier requirement messages: "Requires Tier 3 or higher"

**Implementation:**
```csharp
// Check tier requirement
bool CanSelectProfession(string professionId)
{
    int requiredTier = GetProfessionTierRequirement(professionId);
    int currentTier = EnlistmentBehavior.Instance?.EnlistmentTier ?? 0;
    return currentTier >= requiredTier;
}
```

**XP Integration:**
- Selected duties/professions connect to daily XP processing
- `EnlistedDutiesBehavior.AssignDuty()` processes selections
- Formation training works with selections
- Duty changes persist properly

---

## Edge Cases

### Menu State Consistency

**Problem:** Menu state can become inconsistent during navigation

**Solution:**
- Real-time refresh maintains menu state consistency
- `SetDynamicMenuText()` called on menu refresh
- Selection changes update immediately

### Tier Progression

**Scenario:** Player promotes to Tier 3 while menu is open

**Handling:**
- Professions unlock automatically
- Menu refresh shows new availability
- No need to close and reopen menu

### Duty Selection Persistence

**Scenario:** Player changes duty, then navigates away and back

**Handling:**
- Selection persists in `EnlistmentBehavior`
- Menu shows correct checkmark on return
- XP processing uses persisted selection

### Menu Activation Timing

**Scenario:** Menu activation during encounter transitions

**Handling:**
- Uses `NextFrameDispatcher` for safe activation
- Prevents timing conflicts with game state transitions
- No crashes or assertion failures

### Time Control Preservation

**Problem:** Vanilla `GameMenu.ActivateGameMenu()` and `SwitchToMenu()` force time to `Stop`, then wait menus call `StartWait()` which sets `UnstoppableFastForward`. This overrides the player's time preference.

**Solution:** Handle time mode conversion once in menu init, never in tick handlers:

1. **In Menu Init:** Call `StartWait()`, then convert unstoppable modes to stoppable equivalents
2. **Unlock Time Control:** Call `SetTimeControlModeLock(false)` to allow player speed changes
3. **No Tick Restoration:** Tick handlers must NOT restore time mode - this fights with user input

**Why No Tick Restoration:** For army members, native code uses `UnstoppableFastForward` when the user clicks fast forward. If tick handlers restore `CapturedTimeMode` whenever they see `UnstoppableFastForward`, the next tick immediately reverts user input, breaking speed controls.

**Correct Pattern:**
```csharp
// In menu init - convert once
args.MenuContext.GameMenu.StartWait();
Campaign.Current.SetTimeControlModeLock(false);
if (Campaign.Current.TimeControlMode == CampaignTimeControlMode.UnstoppableFastForward)
{
    Campaign.Current.TimeControlMode = CampaignTimeControlMode.StoppableFastForward;
}

// In tick handler - do NOT restore time mode
// Just handle menu-specific logic, leave time control alone
```

**Popup Handling:**
- Popup callbacks should NOT call `SafeActivateEnlistedMenu()` - this re-captures time incorrectly
- Cancel actions just close the popup - the menu underneath is already active
- Success actions refresh the menu without re-capturing time

---

## Bannerlord Menu APIs

Reference for Bannerlord's native menu system APIs and patterns.

### Menu Types

**MenuAndOptionType Values:**

| Type | Value | UI Widgets | Time Controls | Usage |
|------|-------|------------|---------------|-------|
| `RegularMenuOption` | 0 | None | Pauses game | Basic text menus |
| `WaitMenuShowProgressAndHoursOption` | 1 | Progress bar + hours | Works | Time-based activities |
| `WaitMenuShowOnlyProgressOption` | 2 | Progress only | Works | Menus with progress display |
| `WaitMenuHideProgressAndHoursOption` | 3 | Clean text only | Works | Army wait, settlement wait |

**Recommended:** Use `WaitMenuHideProgressAndHoursOption` for clean menus without progress widgets.

### Time Control Behavior

**Vanilla Behavior:**
1. `GameMenu.ActivateGameMenu()` sets `Campaign.Current.TimeControlMode = Stop`
2. For wait menus, `StartWait()` then sets `TimeControlMode = UnstoppableFastForward`
3. This overrides player's time preference (paused/playing)

**Time Control Modes:**

| Mode | Description |
|------|-------------|
| `Stop` | Game paused |
| `StoppablePlay` | Normal speed, can be paused |
| `StoppableFastForward` | Fast speed, can be paused |
| `UnstoppablePlay` | Normal speed, cannot be paused |
| `UnstoppableFastForward` | Fast speed, forced by StartWait() |
| `UnstoppableFastForwardForPartyWaitTime` | Party wait variant |

**Preserving Player Time Preference:**

Handle in menu init only (not in tick handlers):
```csharp
private void OnMenuInit(MenuCallbackArgs args)
{
    // Start wait to enable time controls
    args.MenuContext.GameMenu.StartWait();
    
    // Unlock so player can change speed
    Campaign.Current.SetTimeControlModeLock(false);
    
    // Convert unstoppable to stoppable (allows pause button)
    if (Campaign.Current.TimeControlMode == CampaignTimeControlMode.UnstoppableFastForward ||
        Campaign.Current.TimeControlMode == CampaignTimeControlMode.UnstoppableFastForwardForPartyWaitTime)
    {
        Campaign.Current.TimeControlMode = CampaignTimeControlMode.StoppableFastForward;
    }
    else if (Campaign.Current.TimeControlMode == CampaignTimeControlMode.Stop)
    {
        Campaign.Current.TimeControlMode = CampaignTimeControlMode.StoppablePlay;
    }
}

// In tick handler - do NOT modify time control mode
// Native uses UnstoppableFastForward for army members clicking fast forward
// Restoring CapturedTimeMode in tick fights with user input
```

### Menu Registration

```csharp
// Add wait game menu (recommended for time controls)
starter.AddWaitGameMenu("menu_id",
    "Menu Title: {TEXT_VAR}",
    OnMenuInit,
    OnMenuCondition,
    null, // OnConsequenceDelegate
    OnMenuTick,
    GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption,
    GameOverlays.MenuOverlayType.None,
    0f,
    GameMenu.MenuFlags.None,
    null);

// Add regular game menu (pauses game)
starter.AddGameMenu(menuId, menuTitle, menuIntroText, 
    menuFlags, menuBackgroundMeshName);
```

### Background and Audio

```csharp
[GameMenuInitializationHandler("menu_id")]
private static void OnMenuBackgroundInit(MenuCallbackArgs args)
{
    // Set culture-appropriate background
    args.MenuContext.SetBackgroundMeshName(Hero.MainHero.MapFaction.Culture.EncounterBackgroundMesh);
    
    // Set ambient sound
    args.MenuContext.SetAmbientSound("event:/map/ambient/node/settlements/2d/camp_army");
    args.MenuContext.SetPanelSound("event:/ui/panels/settlement_camp");
}
```

### Text Variables

```csharp
// Get menu text and set variables (in init or tick handler)
var text = args.MenuContext.GameMenu.GetText();
text.SetTextVariable("PARTY_LEADER", lordName);
text.SetTextVariable("PARTY_TEXT", statusContent);

// Global text variables
MBTextManager.SetTextVariable("VARIABLE_NAME", value, false);
```

### Menu Navigation {#menu-navigation-api}

```csharp
GameMenu.SwitchToMenu("target_menu_id");  // Switch between menus
GameMenu.ExitToLast();                     // Return to previous menu
args.MenuContext.GameMenu.EndWait();       // End wait menu properly
```

### Menu Options

```csharp
starter.AddGameMenuOption("menu_id", "option_id", "Option Text",
    args =>
    {
        args.optionLeaveType = GameMenuOption.LeaveType.Trade;
        args.Tooltip = new TextObject("Tooltip text");
        return true; // Available
    },
    OnOptionSelected,
    isLeave: false,
    priority: 0);
```

### LeaveType Icons

| LeaveType | Icon Purpose |
|-----------|--------------|
| `Continue` | Continue/default action |
| `TroopSelection` | Troop management |
| `Trade` | Trading/equipment |
| `Conversation` | Dialog |
| `Submenu` | Navigation |
| `Manage` | Management |
| `Leave` | Exit/leave action |
| `Escape` | Warning/danger |
| `Mission` | Combat |
| `DefendAction` | Defense |
| `Raid` | Aggressive action |
| `SiegeAmbush` | Siege-related |
| `OrderTroopsToAttack` | Command troops |

### Popup Dialogs

**ShowInquiry:**
```csharp
InformationManager.ShowInquiry(
    new InquiryData(
        title,
        message,
        isAffirmativeOptionShown: true,
        isNegativeOptionShown: true,
        affirmativeText: "Yes",
        negativeText: "No",
        affirmativeAction: () => { /* on yes */ },
        negativeAction: () => { /* on no */ }),
    pauseGameActiveState: false); // false = don't pause game
```

**ShowMultiSelectionInquiry:**
```csharp
MBInformationManager.ShowMultiSelectionInquiry(
    new MultiSelectionInquiryData(
        title,
        description,
        options,        // List<InquiryElement>
        isExitShown: true,
        minSelectableOptionCount: 1,
        maxSelectableOptionCount: 1,
        affirmativeText: "Select",
        negativeText: "Cancel",
        affirmativeAction: selected => { /* on select */ },
        negativeAction: _ => { /* on cancel */ }),
    pauseGameActiveState: false);
```

**Note:** Popup callbacks should not call menu activation methods that re-capture time state. The underlying menu remains active when the popup closes.

---

## API Reference

Enlisted-specific menu APIs and patterns.

### Time State Management

```csharp
// Capture time state before menu activation (shared across all menus)
QuartermasterManager.CaptureTimeStateBeforeMenuActivation();

// Access shared captured time mode
CampaignTimeControlMode? captured = QuartermasterManager.CapturedTimeMode;
```

### Menu Activation

```csharp
// Safe menu activation (with time preservation)
EnlistedMenuBehavior.SafeActivateEnlistedMenu();

// Direct activation (capture time first)
QuartermasterManager.CaptureTimeStateBeforeMenuActivation();
GameMenu.ActivateGameMenu("enlisted_status");
```

### Selection Management

```csharp
// Get current selection
string currentDuty = EnlistmentBehavior.Instance?.SelectedDuty;
string currentProfession = EnlistmentBehavior.Instance?.SelectedProfession;

// Set selection
EnlistmentBehavior.Instance?.SetDuty(dutyId);
EnlistmentBehavior.Instance?.SetProfession(professionId);
```

---

## Debugging

**Log Categories:**
- `"Interface"` - Menu system activity
- `"Menu"` - Menu navigation and state

**Key Log Points:**
```csharp
// Menu activation
ModLogger.Info("Interface", $"Activating menu: {menuId}");
ModLogger.Debug("Menu", $"Menu state: duty={duty}, profession={profession}");

// Selection changes
ModLogger.Info("Menu", $"Duty changed: {oldDuty} → {newDuty}");
ModLogger.Info("Menu", $"Profession changed: {oldProfession} → {newProfession}");

// Tier checks
ModLogger.Debug("Menu", $"Tier check: required={required}, current={current}, allowed={allowed}");
```

**Common Issues:**

**Professions not appearing:**
- Check tier requirement and availability conditions
- Verify `CanSelectProfession()` returns true
- Check tier progression is working correctly

**Checkmarks not updating:**
- Verify `SetDynamicMenuText()` is called in refresh
- Check selection persistence in `EnlistmentBehavior`
- Ensure menu refresh happens after selection changes

**XP not applying:**
- Ensure selected duties connect to `EnlistedDutiesBehavior.AssignDuty()`
- Check daily tick is processing assignments
- Verify duty/profession IDs match configuration

**Menu doesn't activate:**
- Check `NextFrameDispatcher` is not busy
- Verify encounter state allows menu activation
- Check for timing conflicts with game state transitions

**Debug Output Location:**
- `Modules/Enlisted/Debugging/enlisted.log`

**Related Files:**
- `src/Features/Interface/Behaviors/EnlistedMenuBehavior.cs`
- `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`
- `src/Features/Assignments/Behaviors/EnlistedDutiesBehavior.cs`

---

## Related Documentation

- [Dialog System](dialog-system.md) - Menu activation after enlistment
- [Camp](command-tent.md) - Service records and retinue management
- [Duties System](../Core/duties-system.md) - Duty/profession definitions and XP
