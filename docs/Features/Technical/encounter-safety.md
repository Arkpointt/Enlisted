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
- Hides native “Abandon army” menu option for enlisted players

**Purpose:**
Keep enlisted players from accidentally entering encounters that would break military service or cause pathfinding crashes, while ensuring they correctly join their Lord's battles without engine conflicts.

**Files:**
- `src/Features/Enlistment/Behaviors/EncounterGuard.cs` - Static utility class for encounter state management
- `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs` - Active battle monitoring and participation logic
- `src/Features/Combat/Behaviors/EnlistedEncounterBehavior.cs` - Wait in Reserve menu and battle wait handling
- `src/Mod.GameAdapters/Patches/HidePartyNamePlatePatch.cs` - UI suppression
- `src/Mod.GameAdapters/Patches/JoinEncounterAutoSelectPatch.cs` - Auto-joins lord's battle
- `src/Mod.GameAdapters/Patches/GenericStateMenuPatch.cs` - Prevents menu stutter during reserve mode
- `src/Mod.GameAdapters/Patches/AbandonArmyBlockPatch.cs` - Removes "Abandon army" options while enlisted
- `src/Mod.GameAdapters/Patches/EncounterSuppressionPatch.cs` - Blocks unwanted encounters, clears stale reserve flags
- `src/Mod.GameAdapters/Patches/PostDischargeProtectionPatch.cs` - Blocks party activation in vulnerable states
- `src/Mod.GameAdapters/Patches/VisibilityEnforcementPatch.cs` - Controls party visibility, allows captivity system control

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
6. `AbandonArmyBlockPatch` hides native “Abandon army” options while enlisted (no change when not enlisted)
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
- `IsWaitingInReserve` flag tracks reserve state
- Player stays in `enlisted_battle_wait` menu until army finishes or player rejoins
- `GenericStateMenuPatch` intercepts `GetGenericStateMenu()` to return `enlisted_battle_wait` when in reserve, preventing native menu systems from switching to `army_wait` (which would cause visual stutter)
- Rejoin removes player from reserve, re-adds them to the MapEvent on lord's side, then activates encounter menu
- Handled by `EnlistedEncounterBehavior` and `GenericStateMenuPatch`

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
- Naval MapEvents: `ForceImmediateBattleJoin` syncs the player's `IsCurrentlyAtSea` state and position to the lord before calling `PlayerEncounter.Start/Init()`. This matches the native requirement (`encounteredBattle.IsNavalMapEvent == MainParty.IsCurrentlyAtSea`) and prevents naval join crashes.

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
- `GenericStateMenuPatch` ensures native menu calls return `enlisted_battle_wait` instead of `army_wait`
- Player remains in `enlisted_battle_wait` menu until army finishes fighting
- Prevents duplicate XP awards and menu flickering during rapid consecutive battles

### Rejoining Battle from Reserve

**Scenario:** Player clicks "Rejoin Battle" while waiting in reserve

**Handling:**
- `IsWaitingInReserve` flag cleared immediately
- Player party re-added to lord's MapEvent on same side (`playerParty.MapEventSide = lordSide`)
- Encounter menu activated via `GameMenu.ActivateGameMenu("encounter")`
- Player can then choose to Attack, Send Troops, or Leave
- If battle ended while waiting, player exits to appropriate post-battle menu

### Lingering PlayerEncounter After Discharge

**Scenario:** Service ends (lord captured, player captured, discharge) but PlayerEncounter remains without a MapEvent

**Handling:**
- `PostDischargeProtectionPatch` blocks party activation while `PlayerEncounter.Current != null`
- This prevents crashes from "Attack or Surrender" encounters created immediately after discharge
- `SchedulePostEncounterVisibilityRestore()` queues visibility restoration
- `RestoreVisibilityAfterEncounter()` waits until PlayerEncounter clears
- **Watchdog timeout**: If encounter persists >5 seconds with no MapEvent and player is discharged (not enlisted, not prisoner), `ForceFinishLingeringEncounter()` clears it
- This prevents permanent stuck state when encounter cleanup fails

**Log Pattern:**
```
[Enlistment] Player in vulnerable state when ending service (MapEvent: False, Encounter: True, Prisoner: False) - deferring activation
[PostDischargeProtection] Prevented party activation - discharged but still in battle state (MapEvent: False, Encounter: True)
[EncounterCleanup] Force finishing lingering PlayerEncounter after discharge (requested at {time})
[Enlistment] Party visibility restored after encounter cleanup
```

### Lord Death While Enlisted

**Scenario:** Lord dies while player is enlisted

**Handling:**
- Encounter safety disabled before retirement processing
- Prevents crashes during emergency service termination
- Grace period begins normally
- State cleanup happens automatically
- Naval safety: if the player is at sea without ships when the lord dies (or any service-ending event), the system always teleports the player to the nearest port before continuing grace/retirement so they cannot get stranded on water.

### Battle Defeat (Autosim)

**Scenario:** Lord's army loses in autosim

**Handling:**
- Player sees encounter menu (Attack/Surrender) after report
- Standard native behavior
- Player resolves outcome manually
- No special handling needed

### Player Captured (Prisoner State)

**Scenario:** Player is taken prisoner after battle defeat or lord capture

**Handling:**
- Mod detects capture via `OnHeroPrisonerTaken` event
- Service ends with grace period (14 days to rejoin same kingdom after escape)
- All map event processing skipped while `Hero.MainHero.IsPrisoner == true`
- Logs show: `Skipping MapEventEnded - player prisoner or cleanup pending`
- Native captivity system takes full control of player state

**Critical - Party Activation During Captivity:**
- Native `PlayerCaptivity.StartCaptivity()` sets `MobileParty.MainParty.IsActive = false`
- `StopEnlist()` now checks `Hero.MainHero.IsPrisoner` before activating the party
- If prisoner, party stays deactivated; `SchedulePostEncounterVisibilityRestore()` waits for captivity to end
- `RestoreVisibilityAfterEncounter()` also checks prisoner state and defers until released
- This prevents the mod from fighting native captivity's party state management

**Important - Prisoner Transfers (Base Game Behavior):**
- While imprisoned, the base game's `TransferPrisonerBarterBehavior` remains active
- When enemy lords meet and interact (dialog/barter), they can trade prisoners
- This includes transferring the player to a different captor
- This is **expected native behavior**, not a mod bug
- The mod correctly stays inactive during captivity, letting native mechanics operate
- Grace period remains valid regardless of which enemy holds the player

**Log Pattern:**
```
[EventSafety] Lord {name} captured - starting grace period
[Battle] Encounter active during lord capture - letting native surrender capture handle the player.
[Enlistment] Service ended: Lord captured (Honorable: False)
[Enlistment] Player in vulnerable state when ending service (MapEvent: False, Encounter: False, Prisoner: True) - deferring activation
[EventSafety] Skipping MapEventEnded - player prisoner or cleanup pending (IsPrisoner=True, CaptureCleanupScheduled=False)
```

### Lord Captured While Player Active

**Scenario:** Lord is captured while player is still fighting or in encounter

**Handling:**
- `TryCapturePlayerAlongsideLord()` checks if player should be captured too
- If player already in encounter (surrender screen), native flow handles capture
- If player was in reserve, reserve state is cleared and player is teleported to safety with protection
- Grace period starts for the kingdom the player was serving
- Cleanup deferred via `SchedulePlayerCaptureCleanup()` until encounter closes

### Lord's Party Disbanded While Player in Reserve

**Scenario:** Lord's party disbands (not captured) while player is waiting in reserve during battle

**Handling:**
- `StopEnlist()` now clears `IsWaitingInReserve` immediately when service ends for ANY reason
- Harmony patches check `IsEnlisted` alongside `IsWaitingInReserve` before blocking
- If reserve flag is stale (not enlisted), patches clear it automatically
- This prevents player getting stuck with invisible/inactive party after party disband
- Player receives grace period and is restored to normal map state

**Log Pattern (when fix triggers):**
```
[Enlistment] Lord's party disbanded (Lord: {name}, Kingdom: {kingdom}) - starting grace period
[Battle] Clearing reserve state during service end
[Enlistment] Party activated and made visible (no active battle state)
```

**Log Pattern (stale flag cleared by patches):**
```
[PostDischargeProtection] Clearing stale reserve flag during activation
```

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
- `"EventSafety"` - Capture/prisoner handling and map event skipping

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

// Capture/prisoner handling
ModLogger.Info("EventSafety", $"Lord {name} captured - starting grace period");
ModLogger.Info("EventSafety", $"Player captured - deferring enlistment teardown until encounter closes");
ModLogger.Info("EventSafety", $"Skipping MapEventEnded - player prisoner or cleanup pending");
ModLogger.Info("EventSafety", $"Finalizing deferred capture cleanup for player");
ModLogger.Info("Battle", $"Encounter active during lord capture - letting native surrender capture handle the player.");
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

**Reserve mode menu stutter (flickering between menus):**
- `GenericStateMenuPatch` should intercept `GetGenericStateMenu()` calls
- Verify patch is registered in `conflicts.log`
- Check `IsWaitingInReserve` is true when in reserve
- If stutter persists, the patch may be missing from `Enlisted.csproj`

**Rejoin battle not working:**
- Log should show: "Player rejoining battle from reserve"
- Player must be re-added to MapEvent before encounter menu activates
- Check `lordParty.Party.MapEventSide` is not null
- Verify `GameMenu.ActivateGameMenu("encounter")` is called

**Party state not persisting:**
- Check state enforcement in `OnTick()`
- Verify state is being reset continuously
- Check for save/load issues
- Review state validation logs

**Repeated "Skipping MapEventEnded" logs while prisoner:**
- This is **expected behavior** when `IsPrisoner=True`
- The mod correctly skips all map event processing during captivity
- Native captivity system handles all prisoner mechanics
- Grace period remains active for rejoining after escape
- Log pattern: `Skipping MapEventEnded - player prisoner or cleanup pending (IsPrisoner=True, CaptureCleanupScheduled=False)`

**Player transferred to different captor:**
- This is **base game behavior**, not a mod bug
- Native `TransferPrisonerBarterBehavior` allows lords to trade prisoners
- When enemy lords meet/dialog, they can exchange prisoners including the player
- The mod correctly stays inactive during captivity
- Grace period is unaffected by captor changes

**Party stuck invisible/inactive after discharge:**
- Check if PlayerEncounter is lingering: log shows `Prevented party activation - discharged but still in battle state (Encounter: True)`
- Watchdog should force-finish after 5 seconds: `Force finishing lingering PlayerEncounter after discharge`
- If still stuck, check if player is prisoner (`IsPrisoner=True`) - native captivity owns state until release
- Log pattern when prisoner blocks activation: `Player in vulnerable state when ending service (Prisoner: True) - deferring activation`
- If stuck after party disband while in reserve, check log for `Clearing reserve state during service end` - this should clear the flag
- If patches see stale reserve flag, they auto-clear it: `Clearing stale reserve flag during activation`

**"Party activated and made visible" but player is prisoner:**
- This was a bug (now fixed) - StopEnlist didn't check prisoner state before activating
- Current code checks `playerIsPrisoner` alongside MapEvent/Encounter before deciding to activate
- If you see this log while prisoner, ensure you have the latest code

**Debug Output Location:**
- `Modules/Enlisted/Debugging/enlisted.log`

**Related Files:**
- `src/Features/Enlistment/Behaviors/EncounterGuard.cs`
- `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`
- `src/Features/Combat/Behaviors/EnlistedEncounterBehavior.cs`
- `src/Mod.GameAdapters/Patches/HidePartyNamePlatePatch.cs`
- `src/Mod.GameAdapters/Patches/JoinEncounterAutoSelectPatch.cs`
- `src/Mod.GameAdapters/Patches/GenericStateMenuPatch.cs`
- `src/Mod.GameAdapters/Patches/EncounterSuppressionPatch.cs`
- `src/Mod.GameAdapters/Patches/PostDischargeProtectionPatch.cs`
- `src/Mod.GameAdapters/Patches/VisibilityEnforcementPatch.cs`

---

## Related Documentation

- [Enlistment System](../Core/enlistment.md) - Service state management
- [Battle System](../Combat/battle-system.md) - Battle participation details
