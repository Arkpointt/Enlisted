# TaleWorlds Menu Handling Research

**Purpose**: Understand how TaleWorlds handles menu transitions, activations, and state management to prevent RGL crashes during siege menu transitions.

**Date**: 2025-11-25

## Key Menu APIs

### Core Menu Methods

#### `GameMenu.ActivateGameMenu(string menuId)`
- **Purpose**: Activates a menu, creating a new menu context
- **When to use**: When starting a new menu from scratch
- **Timing**: Should be deferred during state transitions (use `NextFrameDispatcher`)
- **Risks**: Can cause RGL crashes if called during active state transitions (siege menu → encounter menu)

#### `GameMenu.SwitchToMenu(string menuId)`
- **Purpose**: Switches from current menu to another menu
- **When to use**: When transitioning between menus in the same context
- **Timing**: Can be called directly, but deferring is safer during critical transitions
- **Risks**: Less risky than `ActivateGameMenu`, but still can cause issues during siege transitions

#### `GameMenu.ExitToLast()`
- **Purpose**: Exits current menu and returns to previous menu
- **When to use**: When closing a menu to return to the previous state
- **Timing**: Generally safe, but defer during encounter exits
- **Risks**: Can cause issues if called during encounter finishing

### Menu State Detection

#### `Campaign.Current.CurrentMenuContext?.GameMenu?.StringId`
- **Purpose**: Get the current active menu ID
- **Usage**: Check what menu is currently active before making menu changes
- **Example**: `string currentMenu = Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId ?? "";`

#### `Campaign.Current.Models.EncounterGameMenuModel.GetGenericStateMenu()`
- **Purpose**: Get what menu the native system wants to show based on current game state
- **Usage**: Check if native system wants a different menu (like `army_wait`, `menu_siege_strategies`) before activating custom menus
- **Best Practice**: Always check this before activating custom menus to avoid conflicts

## Menu Lifecycle and Timing

### Critical Transition Periods

#### Siege Menu → Encounter Menu Transition
- **Problem**: RGL crash occurs during this transition (`rglSkeleton.cpp:1197`, `time_since_last_update > 0`)
- **Root Cause**: Zero-delta-time updates when menu operations happen during skeleton system updates
- **Solution**: Disable ALL party state changes during siege transitions

#### Encounter Exit → Menu Activation
- **Problem**: Menu activation during encounter finishing can cause assertion failures
- **Solution**: Use `NextFrameDispatcher` with `requireNoEncounter: true` to defer until `PlayerEncounter.Current == null`

### Menu Event Handlers

#### `CampaignEvents.GameMenuOpened`
- **Purpose**: Fired when any menu opens
- **Usage**: Track current menu state, refresh menu options
- **Timing**: Safe to use, fires after menu is fully initialized

#### Menu Tick Handlers (`OnTickDelegate`)
- **Purpose**: Called every frame while menu is active
- **Usage**: Update dynamic menu content, check for state changes
- **Critical**: Must validate `dt.ToSeconds > 0` to prevent zero-delta-time issues
- **Example**:
```csharp
private void OnEnlistedStatusTick(MenuCallbackArgs args, CampaignTime dt)
{
    if (dt.ToSeconds <= 0)
    {
        return; // Prevent zero-delta-time assertion failures
    }
    // Update menu content
}
```

## Best Practices for Menu Operations

### 1. Always Defer Menu Activations During State Transitions

```csharp
// BAD: Direct activation during transition
GameMenu.ActivateGameMenu("enlisted_status");

// GOOD: Deferred activation
NextFrameDispatcher.RunNextFrame(() => 
{
    GameMenu.ActivateGameMenu("enlisted_status");
});
```

### 2. Check Native System Menu Intent

```csharp
// Before activating custom menu, check what native system wants
string genericStateMenu = Campaign.Current.Models.EncounterGameMenuModel.GetGenericStateMenu();

if (genericStateMenu == "enlisted_status" || string.IsNullOrEmpty(genericStateMenu))
{
    // Safe to activate our menu
    GameMenu.ActivateGameMenu("enlisted_status");
}
else
{
    // Native system wants different menu (like army_wait) - respect it
    ModLogger.Debug("Interface", $"Native system wants '{genericStateMenu}' - not activating custom menu");
}
```

### 3. Disable Operations During Siege Transitions

```csharp
// Check for siege state before any menu operations
bool anySiegeEvent = mainParty?.Party?.SiegeEvent != null || lordParty?.Party?.SiegeEvent != null;
bool anyBesiegerCamp = mainParty?.BesiegerCamp != null || lordParty?.BesiegerCamp != null;
bool anyBesiegedSettlement = mainParty?.BesiegedSettlement != null || lordParty?.BesiegedSettlement != null;
bool anySiegeMapEvent = mainParty?.Party?.MapEvent?.IsSiegeAssault == true || 
                        lordParty?.Party?.MapEvent?.IsSiegeAssault == true;
bool playerEncounterSiege = PlayerEncounter.Current != null && PlayerEncounter.EncounteredParty?.SiegeEvent != null;
bool hasPlayerEncounter = PlayerEncounter.Current != null;
bool encounterDuringSiege = hasPlayerEncounter && (anySiegeEvent || anyBesiegerCamp || anyBesiegedSettlement || anySiegeMapEvent);
bool mapEventWaitState = (mainParty?.Party?.MapEvent?.State == MapEventState.Wait || 
                          lordParty?.Party?.MapEvent?.State == MapEventState.Wait) && 
                         (anySiegeEvent || anyBesiegerCamp || anyBesiegedSettlement || anySiegeMapEvent);
string currentMenu = Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId ?? "";
bool inSiegeMenu = currentMenu.Contains("siege") || currentMenu == "encounter";
bool inEncounterMenuWithMapEvent = currentMenu == "encounter" && 
                                    (mainParty?.Party?.MapEvent != null || lordParty?.Party?.MapEvent != null);

bool anySiege = anySiegeEvent || anyBesiegerCamp || anyBesiegedSettlement || anySiegeMapEvent || 
                playerEncounterSiege || encounterDuringSiege || mapEventWaitState || 
                inSiegeMenu || inEncounterMenuWithMapEvent;

if (anySiege)
{
    // COMPLETE disable - no menu operations, no party state changes
    return;
}
```

### 4. Validate Time Delta in Tick Handlers

```csharp
// Always validate time delta to prevent assertion failures
if (dt <= 0 || dt.ToSeconds <= 0)
{
    return; // Skip processing if time delta is invalid
}
```

### 5. Use `NextFrameDispatcher` for Encounter-Related Operations

```csharp
// When finishing encounters, defer menu activation
NextFrameDispatcher.RunNextFrame(() =>
{
    PlayerEncounter.Finish(true);
}, requireNoEncounter: false);

// Defer menu activation until encounter is fully cleared
NextFrameDispatcher.RunNextFrame(() =>
{
    GameMenu.ActivateGameMenu("enlisted_status");
}, requireNoEncounter: true); // Wait until PlayerEncounter.Current == null
```

## Menu Registration Patterns

### Standard Menu Registration

```csharp
starter.AddWaitGameMenu(
    "menu_id",
    "Menu Title Text",
    new OnInitDelegate(OnMenuInit),
    new OnConditionDelegate(OnMenuCondition),
    new OnConsequenceDelegate(OnMenuConsequence), // Optional
    new OnTickDelegate(OnMenuTick), // Optional
    GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption,
    GameOverlays.MenuOverlayType.None,
    0f, // Wait duration (0 = indefinite)
    GameMenu.MenuFlags.None,
    null
);
```

### Menu Option Registration

```csharp
starter.AddGameMenuOption(
    "menu_id",
    "option_id",
    "Option Text",
    new OnConditionDelegate((args) => 
    {
        // Condition to show option
        return true;
    }),
    new OnConsequenceDelegate((args) =>
    {
        // What happens when option is selected
        GameMenu.SwitchToMenu("target_menu");
    }),
    isLeave: false,
    index: 0
);
```

## Common Pitfalls

### 1. Menu Activation During Siege Transitions
- **Problem**: Activating menus during siege menu → encounter menu transition causes RGL crashes
- **Solution**: Check for siege state comprehensively before any menu operations

### 2. Menu Operations During Encounter Finishing
- **Problem**: Menu operations during `PlayerEncounter.Finish()` can cause assertion failures
- **Solution**: Use `NextFrameDispatcher` with `requireNoEncounter: true`

### 3. Zero-Delta-Time in Tick Handlers
- **Problem**: Tick handlers with zero or negative time delta cause assertion failures
- **Solution**: Always validate `dt > 0` at the start of tick handlers

### 4. Ignoring Native System Menu Intent
- **Problem**: Activating custom menus when native system wants different menu (like `army_wait`) causes conflicts
- **Solution**: Always check `GetGenericStateMenu()` before activating custom menus

### 5. Party State Changes During Menu Transitions
- **Problem**: Position snapping, camera operations, and party state changes during menu transitions cause RGL crashes
- **Solution**: Disable ALL party operations during siege transitions

## Menu State Machine

### Typical Menu Flow

```
[No Menu]
    ↓
[Menu Activation Request]
    ↓
[Check Native System Intent]
    ↓
[Check Siege State]
    ↓
[Defer if Needed]
    ↓
[Activate Menu]
    ↓
[Menu Active]
    ↓
[Menu Tick Handler]
    ↓
[Menu Option Selected]
    ↓
[Switch Menu or Exit]
```

### Siege Menu Flow (Problematic)

```
[Siege Menu Active]
    ↓
[User Selects "Attack" or "Lead and Assault"]
    ↓
[Siege State Clears]
    ↓
[Encounter Menu Opens] ← RGL CRASH HERE
    ↓
[MapEvent State: Wait]
    ↓
[Zero-Delta-Time Assertion Failure]
```

## Recommendations for Our Mod

### 1. Comprehensive Siege Detection
- Check ALL siege indicators (SiegeEvent, BesiegerCamp, BesiegedSettlement, MapEvent types)
- Check for PlayerEncounter during siege
- Check for MapEvent in Wait state during siege
- Check for encounter menu with active MapEvent

### 2. Defer All Menu Operations During Sieges
- Use `NextFrameDispatcher` for all menu activations
- Disable party state changes during siege transitions
- Only process deferred actions during sieges (menu transitions need this)

### 3. Respect Native System Intent
- Always check `GetGenericStateMenu()` before activating custom menus
- Switch to native menus when requested (like `army_wait`, `menu_siege_strategies`)
- Don't block native battle menus

### 4. Validate Time Deltas
- Check `dt > 0` in all tick handlers
- Skip processing if time delta is invalid
- Log warnings for zero-delta-time occurrences

### 5. Monitor Menu State
- Track current menu ID in `OnMenuOpened` event
- Check menu state before making changes
- Log menu transitions for debugging

## References

- Official API: https://apidoc.bannerlord.com/v/1.3.4/
- Decompiled Sources: `C:\Dev\Enlisted\DECOMPILE\`
- Menu Documentation: `docs/discovered/menus.md`
- API Discovery: `docs/discovered/api.json`


