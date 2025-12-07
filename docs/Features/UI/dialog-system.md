# Dialog System

## Quick Reference

| Feature            | Purpose               | Location                              |
|--------------------|-----------------------|---------------------------------------|
| Enlistment Dialog  | Join lord's warband   | Talk to lord → "I wish to serve"      |
| Status Dialog      | Check current service | Talk to lord → "How goes my service?" |
| Management Dialogs | Promotions, equipment | Context-dependent conversations       |

## Table of Contents

- [Overview](#overview)
- [How It Works](#how-it-works)
    - [Enlistment Flow](#enlistment-flow)
    - [Status Dialogs](#status-dialogs)
    - [Management Dialogs](#management-dialogs)
- [Technical Details](#technical-details)
    - [System Architecture](#system-architecture)
    - [Dialog Registration](#dialog-registration)
    - [Dialog Structure](#dialog-structure)
- [Edge Cases](#edge-cases)
- [API Reference](#api-reference)
- [Debugging](#debugging)

---

## Overview

Centralized conversation manager that handles all military service dialogs. Prevents dialog conflicts, simplifies
maintenance, and provides consistent conversation experience across all lords.

**Key Features:**

- Single manager for all military conversations
- Centralized dialog ID management (no conflicts)
- Shared condition and consequence methods
- Immediate menu activation after enlistment (no encounter gaps)
- Parallel dialog variants for minor faction lords (mercenary/band tone) gated by `lord.Clan?.IsMinorFaction`

**File:** `src/Features/Conversations/Behaviors/EnlistedDialogManager.cs`

---

## How It Works

### Enlistment Flow

**Standard Flow (kingdom lords):**

1. Talk to any lord → "I have something else to discuss" → "I wish to serve in your warband"
2. Lord responds based on relationship and faction status
3. Player confirms → Immediate enlistment with `IsActive = false` and menu switch
4. No encounter gaps - player goes straight to enlisted status menu

**Minor Faction Flow:**

- Uses the same conversation states but with higher-priority mercenary-themed lines when
  `lord.Clan?.IsMinorFaction == true` (works even if they are mercenaries for a kingdom).
- Text themes: contract/payment focus, company camaraderie.
- Retirement/renewal/leave/early-discharge also have minor-faction variants; consequences are shared (same code paths).

**Army Leader Restriction:**
If player is leading their own army, special dialog appears:

- **Player**: "My lord, I offer you my sword and my loyalty. Will you have me in your ranks?"
- **Lord**: "Hold, friend. I see you already command men of your own. A general cannot become a foot soldier while lords
  still march beneath his banner. Disband your army first, then we may speak of service."
- **Player**: "I understand, my lord. I shall see to my army's affairs first."
- Conversation ends (player cannot proceed with enlistment)

### Status Dialogs

**Purpose:** Check current military service status

**Flow:**

- Talk to lord → "How goes my service?"
- Shows current enlistment information
- Displays tier, XP, days served, etc.

**Features:**

- Centralized conditions check enlistment state
- Shared consequences apply changes consistently
- Dialog IDs managed centrally to prevent conflicts

### Management Dialogs

**Types:**

- Promotion notifications
- Equipment management conversations
- Retirement and departure dialogs (kingdom + minor variants)
- Leave/return and early discharge (kingdom + minor variants)

**Behavior:**

- Context-dependent availability
- Consistent with enlistment state
- Integrated with menu system

---

## Technical Details

### System Architecture

**Single Manager Pattern:**

- All military dialogs registered in one place
- Prevents conflicts between multiple behaviors
- Easy to add new conversations (single file to modify)

**Registration Pattern:**

```csharp
public override void RegisterEvents()
{
    CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
}

private void OnSessionLaunched(CampaignGameStarter starter)
{
    RegisterEnlistmentDialogs(starter);
    RegisterStatusDialogs(starter);
    RegisterManagementDialogs(starter);
}
```

### Dialog Registration

**Method:** `RegisterEnlistmentDialogs(CampaignGameStarter starter)`

**Process:**

1. Define dialog line IDs (consistent naming)
2. Register conditions (when dialog appears)
3. Register consequences (what happens when selected)
4. Link to conversation tree

**Example:**

```csharp
starter.AddPlayerLine("enlisted_wish_to_serve",
    "lord_talk_player",
    "enlisted_lord_response",
    new ConversationSentence.OnConditionDelegate(CanEnlistWithLord),
    new ConversationSentence.OnConsequenceDelegate(StartEnlistment));
```

### Dialog Structure

**Consistent Dialog Line IDs:**

- Prefix: `enlisted_` for all military service dialogs
- Format: `enlisted_{action}_{context}`
- Examples: `enlisted_wish_to_serve`, `enlisted_status_check`, `enlisted_request_leave`

**Shared Condition Methods:**

- `CanEnlistWithLord()` - Checks if player can enlist
- `IsPlayerEnlisted()` - Checks current enlistment state
- `IsPlayerOnLeave()` - Checks leave status
- `CanRequestLeave()` - Validates leave eligibility

**Shared Consequence Methods:**

- `StartEnlistment()` - Initiates enlistment process
- `ShowStatus()` - Displays service information
- `RequestLeave()` - Starts leave process
- `EndEnlistment()` - Handles retirement/discharge

---

## Edge Cases

### Multiple Dialog Behaviors

**Problem:** Only one behavior can own each dialog ID

**Solution:** Centralized manager prevents conflicts between features

**Implementation:** All dialogs registered in `EnlistedDialogManager` only

### Lord State Changes

**Scenarios:**

- Lord dies during conversation
- Lord captured during conversation
- Lord changes faction

**Handling:**

- Dialog availability updates based on current lord status
- Graceful fallback if enlisted lord becomes unavailable
- Status dialogs check lord validity before display

### Invalid Enlistment Attempts

**Blocked Scenarios:**

- Already enlisted with different lord
- Player is leading their own army (shows roleplay rejection dialog)
- Lord not available for service
- Player in incompatible state (prisoner, etc.)

**User Experience:**

- Clear rejection messages
- Roleplay-appropriate responses
- No crashes or state corruption

### Menu Activation

**Requirement:** Immediate menu switch after enlistment (no encounter gaps)

**Implementation:**

- Enlistment sets `IsActive = false` immediately
- Menu activation happens in same frame
- No intermediate encounter state

---

## API Reference

### Dialog Registration

```csharp
// Add player line
starter.AddPlayerLine(string playerLineId, string parentId, string nextLineId,
    ConversationSentence.OnConditionDelegate condition,
    ConversationSentence.OnConsequenceDelegate consequence);

// Add dialog line
starter.AddDialogLine(string lineId, string parentId, string nextLineId,
    ConversationSentence.OnConditionDelegate condition,
    ConversationSentence.OnConsequenceDelegate consequence);
```

### Condition Methods

```csharp
// Check if player can enlist
bool CanEnlistWithLord()
{
    return !EnlistmentBehavior.Instance?.IsEnlisted == true &&
           !MobileParty.MainParty?.IsMainPartyWaitingInSettlement == true &&
           Hero.MainHero?.IsPrisoner != true;
}

// Check if player is enlisted
bool IsPlayerEnlisted()
{
    return EnlistmentBehavior.Instance?.IsEnlisted == true;
}

// Check if player is on leave
bool IsPlayerOnLeave()
{
    return EnlistmentBehavior.Instance?.IsOnLeave == true;
}
```

### Consequence Methods

```csharp
// Start enlistment process
void StartEnlistment()
{
    var lord = ConversationManager.OneToOneConversationCharacter as Hero;
    EnlistmentBehavior.Instance?.EnlistWithLord(lord);
    // Menu activation happens automatically
}

// Show service status
void ShowStatus()
{
    // Display current enlistment information
    // Shows tier, XP, days served, etc.
}
```

### Dialog ID Naming Convention

**Format:** `enlisted_{action}_{context}`

**Examples:**

- `enlisted_wish_to_serve` - Enlistment request
- `enlisted_status_check` - Status inquiry
- `enlisted_request_leave` - Leave request
- `enlisted_army_leader_rejection` - Army leader restriction

---

## Debugging

**Log Categories:**

- `"Dialog"` - Dialog registration and conversation flow

**Key Log Points:**

```csharp
// Dialog registration
ModLogger.Info("Dialog", $"Registered enlistment dialog: {dialogId}");

// Conversation flow
ModLogger.Debug("Dialog", $"Player selected: {dialogLineId}");
ModLogger.Debug("Dialog", $"Condition check: {conditionName} = {result}");

// Enlistment
ModLogger.Info("Dialog", $"Enlistment started via dialog with {lord.Name}");
```

**Common Issues:**

**Dialog doesn't appear:**

- Check dialog conditions return true for current game state
- Verify dialog is registered in `OnSessionLaunched`
- Check for conflicting dialog IDs in other behaviors

**"Dialog already registered" error:**

- Another behavior is trying to use same dialog ID
- Ensure all dialogs registered in `EnlistedDialogManager` only
- Check for duplicate registrations

**Menu doesn't activate:**

- Check consequence methods are properly applied
- Verify `IsActive = false` is set during enlistment
- Check menu activation logic in `EnlistmentBehavior`

**Debug Output Location:**

- `Modules/Enlisted/Debugging/dialog.log` (if separate log enabled)
- `Modules/Enlisted/Debugging/enlisted.log` (main log)

**Related Files:**

- `src/Features/Conversations/Behaviors/EnlistedDialogManager.cs`
- `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`
- `src/Features/Interface/Behaviors/EnlistedMenuBehavior.cs`

---

## Related Documentation

- [Menu Interface](menu-interface.md) - Menu activation after enlistment
- [Enlistment System](../Core/enlistment.md) - Service state management
