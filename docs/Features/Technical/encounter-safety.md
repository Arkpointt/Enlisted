# Encounter Safety System

## Quick Reference

| State | Party Visibility | Encounter Behavior | Battle Participation |
|-------|-----------------|-------------------|---------------------|
| Enlisted (Peace) | Hidden (`IsActive = false`) | No encounters | N/A |
| Enlisted (Battle) | Hidden but Active (`IsActive = true`) | Auto-joins lord's battle | Yes |
| Not Enlisted | Visible (`IsActive = true`) | Normal encounters | Normal |

## Table of Contents

- [Overview](#overview)
- [How It Works](#how-it-works)
  - [During Enlistment (Peace/Travel)](#during-enlistment-peacetravel)
  - [During Service (Battle)](#during-service-battle)
  - [On Retirement](#on-retirement)
- [Technical Details](#technical-details)
  - [System Architecture](#system-architecture)
  - [State Management](#state-management)
  - [Battle Participation](#battle-participation)
- [Edge Cases](#edge-cases)
- [API Reference](#api-reference)
- [Debugging](#debugging)

---

## Overview

Prevents the player from triggering map encounters while enlisted by using the game engine's `IsActive` property and Harmony patches, with enhanced battle participation and cleanup logic.

**Key Features:**
- No map encounters during peaceful service (bandit raids, etc.)
- Automatic battle participation when lord enters combat
- Player party hidden from map systems (UI and logical)
- Smooth transitions between peace and battle states
- Reliable cleanup when returning to normal campaign play

**Purpose:**
Keep enlisted players from accidentally entering encounters that would break military service or cause pathfinding crashes, while ensuring they correctly join their Lord's battles without engine conflicts.

**Files:**
- `src/Features/Enlistment/Behaviors/EncounterGuard.cs` - Static utility class for encounter state management
- `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs` - Active battle monitoring and participation logic
- `src/Features/Combat/Behaviors/EnlistedEncounterBehavior.cs` - Wait in Reserve menu and battle wait handling
- `src/Mod.GameAdapters/Patches/HidePartyNamePlatePatch.cs` - UI suppression
- `src/Mod.GameAdapters/Patches/JoinEncounterAutoSelectPatch.cs` - Auto-joins lord's battle

---

## How It Works

### During Enlistment (Peace/Travel)

**State:** Player party is inactive and hidden

**Process:**
1. Player enlists → `MobileParty.MainParty.IsActive = false`
2. Nameplate hidden via `HidePartyNamePlatePatch`
3. Game engine stops considering player for encounters
4. Player follows lord's army without map interference
5. System continuously enforces state (prevents manual changes)

**Result:**
- No bandit raids or random encounters
- No pathfinding crashes
- Player party invisible on map
- Smooth following behavior

### During Service (Battle)

**State:** Player party is active but visually hidden, participates in battle

**Process:**
1. Lord enters battle (`MapEvent` detected)
2. `EnlistmentBehavior` activates player party (`IsActive = true`) while keeping it visually hidden
3. Player party kept in same army/attachment so vanilla encounter stack automatically collects it
4. `JoinEncounterAutoSelectPatch` intercepts "join_encounter" menu and automatically joins battle on lord's side
5. Player sees standard encounter menu (Attack/Send Troops/Wait) instead of "Help X's Party / Don't get involved"
6. Player participates in battle
7. If the player is a prisoner or capture cleanup is queued, battle handling is skipped to let native captivity finish (prevents crash when captors are defeated by friendlies).

**Battle Types:**

**Manual Battle:**
- Player's troops participate directly in combat
- Standard battle interface
- Player controls their squad

**Autosim (Send Troops):**
- Relies on vanilla autosim (player party isn't injected)
- After seeing report, expect encounter menu (Attack/Surrender) if army loses
- Player always resolves outcome through standard encounter UI

**Wait in Reserve:**
- Available for field battles (not sieges)
- Player sits out the battle while the army fights
- `IsWaitingInReserve` flag prevents menu loops during consecutive battles
- Player stays in `enlisted_battle_wait` menu until army finishes fighting
- Can rejoin battle at any time via menu option
- Handled by `EnlistedEncounterBehavior`

**Result:**
- Player automatically joins lord's battles
- Correct encounter menus (not "Help or Leave")
- Predictable battle behavior
- No manual intervention needed

### On Retirement

**State:** Player party returns to normal visibility and encounter behavior

**Process:**
1. Player leaves service → `MobileParty.MainParty.IsActive = true`
2. Game engine re-enables normal encounter behavior
3. Player returns to normal world interaction
4. Nameplate restored

**Result:**
- Normal encounter behavior restored
- Player party visible on map
- Full campaign map interaction available

---

## Technical Details

### System Architecture

**Component Structure:**
```
EncounterGuard (Static Utility)
    ├── DisableEncounters() - Sets IsActive = false
    ├── EnableEncounters() - Sets IsActive = true
    └── State validation

EnlistmentBehavior (Campaign Behavior)
    ├── OnTick() - Continuous state enforcement
    ├── Battle detection (MapEvent monitoring)
    └── Battle participation logic

HidePartyNamePlatePatch (Harmony Patch)
    └── Suppresses party nameplate UI

JoinEncounterAutoSelectPatch (Harmony Patch)
    └── Auto-joins lord's battle, bypasses choice menu
```

### State Management

**Core Methods:**
```csharp
// Disable encounters during enlistment
public static void DisableEncounters()
{
    MobileParty.MainParty.IsActive = false;
}

// Enable encounters after retirement
public static void EnableEncounters()
{
    MobileParty.MainParty.IsActive = true;
}
```

**Monitoring:**
- Called from `EnlistmentBehavior.OnTick()` for continuous enforcement
- State checked every frame during military service
- Immediate response to enlistment status changes
- Prevents manual state changes by player or other systems

**State Validation:**
- System continuously resets `IsActive = false` during service
- Logs warnings if state doesn't match expected service status
- Recovers automatically on next tick

### Battle Participation

**Battle Detection:**
- Monitors `MapEvent` state for lord's party
- Detects when lord enters combat
- Activates player party for battle participation

**Auto-Join Logic:**
- `JoinEncounterAutoSelectPatch` intercepts encounter menu
- Automatically selects lord's side
- Bypasses "Help or Leave" choice menu
- Routes to standard encounter menu (Attack/Send Troops/Wait)

**Menu Routing:**
- Ensures "Encounter" menu instead of "Help or Leave"
- Standard battle interface for player
- Predictable behavior across all battle types

### Activation Gating (Inactive Mode)

- Global gate: `EnlistedActivation` (default off). Flips on at enlist start, off at discharge; synced on load from `IsEnlisted`.
- Guard pattern: behaviors and patches early-return when inactive; logs once if something runs while inactive.
- Active while enlisted, on leave, or in desertion grace so protections stay on during grace/leave; deactivates only once all three are false.
- Crash guards: currently off while inactive per design; can be whitelisted later if needed.
- Menus: remain registered, but handlers/ticks are inert when inactive.
- Scope: finance/food/XP/formation/influence/encounter/nameplate/visibility/captain/return-to-army/order-of-battle, etc., all guarded.

---

## Edge Cases

### Player Manually Changes Party State

**Scenario:** Player or other system tries to change `IsActive` during service

**Handling:**
- System continuously resets `IsActive = false` during service
- Prevents player from accidentally re-enabling encounters
- State enforced every tick

### Game Engine State Conflicts

**Scenario:** Unexpected `IsActive` changes from game engine

**Handling:**
- Monitor for unexpected `IsActive` changes
- Log warnings if state doesn't match expected service status
- Recover automatically on next tick
- No crashes or state corruption

### Siege Menu Loop

**Scenario:** Player stuck in siege "waiting" phase

**Handling:**
- Fixed by suppressing `IsActive` during siege "waiting" phase
- Only active during actual assault (`MapEvent`)
- Prevents menu loop issues

### Consecutive Battles While in Reserve

**Scenario:** Player is waiting in reserve when army engages in another battle

**Handling:**
- `OnMapEventStarted` checks `IsWaitingInReserve` flag before processing
- If true, skips all battle processing (menu switching, teleport, army setup)
- Player remains in `enlisted_battle_wait` menu until army finishes fighting
- Prevents duplicate XP awards and menu flickering during rapid consecutive battles

### Lord Death While Enlisted

**Scenario:** Lord dies while player is enlisted

**Handling:**
- Encounter safety disabled before retirement processing
- Prevents crashes during emergency service termination
- Grace period begins normally
- State cleanup happens automatically

### Battle Defeat (Autosim)

**Scenario:** Lord's army loses in autosim

**Handling:**
- Player sees encounter menu (Attack/Surrender) after report
- Standard native behavior
- Player resolves outcome manually
- No special handling needed

---

## API Reference

### Encounter State Management

```csharp
// Disable encounters (during enlistment)
public static void DisableEncounters()
{
    MobileParty.MainParty.IsActive = false;
}

// Enable encounters (after retirement)
public static void EnableEncounters()
{
    MobileParty.MainParty.IsActive = true;
}

// Check if encounters are disabled
public static bool AreEncountersDisabled()
{
    return !MobileParty.MainParty.IsActive;
}
```

### Battle Detection

```csharp
// Check if lord is in battle
bool IsLordInBattle()
{
    var lord = EnlistmentBehavior.Instance?.CurrentLord;
    return lord?.PartyBelongedTo?.MapEvent != null;
}

// Get lord's MapEvent
MapEvent GetLordMapEvent()
{
    var lord = EnlistmentBehavior.Instance?.CurrentLord;
    return lord?.PartyBelongedTo?.MapEvent;
}
```

### Party Visibility

```csharp
// Hide party nameplate
// Handled by HidePartyNamePlatePatch
// Suppresses UI display of party nameplate

// Check party visibility state
bool IsPartyVisible()
{
    return MobileParty.MainParty.IsVisible;
}

// Check party active state
bool IsPartyActive()
{
    return MobileParty.MainParty.IsActive;
}
```

### Battle Participation

```csharp
// Activate party for battle (while keeping hidden)
void ActivateForBattle()
{
    // Set IsActive = true for battle participation
    // Keep IsVisible = false for visual hiding
    MobileParty.MainParty.IsActive = true;
}

// Deactivate after battle
void DeactivateAfterBattle()
{
    // Return to inactive state during peace
    MobileParty.MainParty.IsActive = false;
}
```

---

## Debugging

**Log Categories:**
- `"EncounterGuard"` - State management operations
- `"Enlistment"` - Service state changes that affect encounters
- `"Battle"` - Battle join logic and vanilla encounter coordination

**Key Log Points:**
```csharp
// State changes
ModLogger.Info("EncounterGuard", $"Encounters disabled (IsActive = false)");
ModLogger.Info("EncounterGuard", $"Encounters enabled (IsActive = true)");

// Battle detection
ModLogger.Info("Battle", $"Lord entered battle: {mapEvent}");
ModLogger.Debug("Battle", $"Player party activated for battle participation");

// State validation
ModLogger.Warn("EncounterGuard", $"Unexpected IsActive state: {isActive}, expected: {expected}");
ModLogger.Debug("EncounterGuard", $"State reset to expected value");
```

**Common Issues:**

**Encounters still happening:**
- Check `IsActive` is actually false
- Verify `DisableEncounters()` is being called
- Check for conflicting systems changing party state
- Review logs for state validation warnings

**Not joining battles:**
- Check `MapEvent` detection logic in `EnlistmentBehavior`
- Verify battle detection is working correctly
- Check if player party is in same army as lord
- Review battle participation logs

**"Help or Leave" menu appearing:**
- Fixed by `JoinEncounterAutoSelectPatch`
- If still appearing, check patch is registered in logs
- Verify patch is applied correctly
- Check for patch conflicts with other mods

**"Attack or Surrender" after autosim defeat:**
- Expected outcome when lord loses
- Player should resolve encounter manually (Attack/Surrender)
- Matches native behavior
- Not a bug - working as designed

**Reserve mode not persisting across battles:**
- `OnMapEventStarted` checks `IsWaitingInReserve` before processing
- Log shows: "Skipping MapEventStarted - player is waiting in reserve"
- If menu keeps appearing, verify the check is present in `OnMapEventStarted`

**Party state not persisting:**
- Check state enforcement in `OnTick()`
- Verify state is being reset continuously
- Check for save/load issues
- Review state validation logs

**Debug Output Location:**
- `Modules/Enlisted/Debugging/enlisted.log`

**Related Files:**
- `src/Features/Enlistment/Behaviors/EncounterGuard.cs`
- `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`
- `src/Mod.GameAdapters/Patches/HidePartyNamePlatePatch.cs`
- `src/Mod.GameAdapters/Patches/JoinEncounterAutoSelectPatch.cs`

---

## Related Documentation

- [Enlistment System](../Core/enlistment.md) - Service state management
- [Battle System](../Combat/battle-system.md) - Battle participation details
