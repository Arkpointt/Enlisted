# Menu Interface System

## Quick Reference

| Menu | Purpose | Access |
|------|---------|--------|
| Enlisted Status | Main service menu | After enlistment, from camp |
| Duty Selection | Choose daily assignment | Enlisted Status → "Report for Duty" |
| Quartermaster | Equipment selection | Enlisted Status → "Visit Quartermaster" |
| Command Tent | Service records, retinue | Enlisted Status → "Command Tent" |

## Table of Contents

- [Overview](#overview)
- [How It Works](#how-it-works)
  - [Main Enlisted Status Menu](#main-enlisted-status-menu)
  - [Duty Selection Interface](#duty-selection-interface)
  - [Menu Navigation](#menu-navigation)
- [Technical Details](#technical-details)
  - [Menu Structure](#menu-structure)
  - [Dynamic Text System](#dynamic-text-system)
  - [Tier-Based Availability](#tier-based-availability)
- [Edge Cases](#edge-cases)
- [API Reference](#api-reference)
- [Debugging](#debugging)

---

## Overview

Professional military menu interface providing comprehensive service management with organized duty/profession selection, detailed descriptions, and tier-based progression.

**Key Features:**
- Clean section organization (Duties vs. Professions)
- Detailed descriptions of each military role
- Tier-based progression visibility
- Real-time status updates
- Connected daily XP processing

**File:** `src/Features/Interface/Behaviors/EnlistedMenuBehavior.cs`

---

## How It Works

### Main Enlisted Status Menu

**Menu ID:** `enlisted_status` (WaitGameMenu)

**Options:**
- **Master at Arms** - Troop selection with close button support
- **Visit Quartermaster** - Equipment variant selection
- **Command Tent** - Service records, retinue management, companion assignments
- **My Lord...** - Conversation with nearby lords
- **Report for Duty** - Organized duty/profession selection
- **Ask commander for leave** - Positioned at bottom for logical flow

**Features:**
- Professional status display with real-time updates
- Clean navigation to sub-menus
- Status information (tier, XP, days served, etc.)

### Duty Selection Interface

**Menu ID:** `enlisted_duty_selection` (WaitGameMenu)

**Section Organization:**
- **DUTIES** section header with visual separation
- **PROFESSIONS** section header with visual separation
- Visual spacer between sections for clean layout

**Duty Selection (Available T1+):**
- **Enlisted** - General military service (+4 XP for non-formation skills)
- **Forager** - Supply procurement (Charm, Roguery, Trade)
- **Sentry** - Guard duty (Scouting, Tactics)
- **Messenger** - Communications (Scouting, Charm, Trade)
- **Pioneer** - Engineering work (Engineering, Steward, Smithing)

**Profession Selection (Available T3+):**
- **Quartermaster's Aide** - Logistics management (Steward, Trade)
- **Field Medic** - Medical duties (Medicine)
- **Siegewright's Aide** - Siege engineering (Engineering, Smithing)
- **Drillmaster** - Training management (Leadership, Tactics)
- **Saboteur** - Special operations (Roguery, Engineering, Smithing)

**Description System:**
- Top of menu shows detailed descriptions for currently selected duty/profession
- "None" shows simple text when no profession selected
- Rich military context explaining daily activities and skill training

**Checkmark System:**
- Dynamic checkmarks (✓/○) showing current selections
- Updates in real-time when selection changes
- Visual feedback for active assignments

### Menu Navigation

**Flow:**
```
Enlisted Status Menu
    ├── Master at Arms → Troop Selection Popup
    ├── Visit Quartermaster → Equipment Selection Menu
    ├── Command Tent → Service Records / Retinue / Companions
    ├── My Lord... → Dialog System
    ├── Report for Duty → Duty Selection Menu
    └── Ask commander for leave → Leave Request Dialog
```

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

---

## API Reference

### Menu Registration

```csharp
// Add game menu
starter.AddGameMenu(menuId, menuTitle, menuIntroText, 
    menuFlags, menuBackgroundMeshName);

// Add menu option
starter.AddGameMenuOption(menuId, optionId, optionText,
    GameMenuOption.OnConditionDelegate condition,
    GameMenuOption.OnConsequenceDelegate consequence,
    isLeave: false, priority: 0);
```

### Dynamic Text

```csharp
// Set dynamic text variable
GameMenu.SetTextVariable(menuId, variableName, value);

// Example: Set checkmark
string checkmark = isSelected ? "✓" : "○";
GameMenu.SetTextVariable("enlisted_duty_selection", "duty_checkmark", checkmark);
```

### Menu Activation

```csharp
// Activate menu (safe, deferred)
NextFrameDispatcher.RunNextFrame(() => 
    GameMenu.ActivateGameMenu("enlisted_status"));

// Check if menu can be activated
bool CanActivateMenu()
{
    return !NextFrameDispatcher.Busy() && 
           PlayerEncounter.Current == null;
}
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
- [Command Tent](command-tent.md) - Service records and retinue management
- [Duties System](../Core/duties-system.md) - Duty/profession definitions and XP
