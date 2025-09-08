# Feature Spec: Encounter Safety System

## Overview
Prevents the player from triggering map encounters while enlisted by using the game engine's `IsActive` property instead of complex patches, with enhanced encounter cleanup for temporary leave system.

## Purpose
Keep enlisted players from accidentally entering encounters (like bandit raids) that would break military service or cause pathfinding crashes when the game tries to find a player party that shouldn't be on the map.

## Inputs/Outputs

**Inputs:**
- Enlistment status changes (join/leave service)
- Real-time party state monitoring
- Lord army status and position

**Outputs:**
- Player party visibility controlled (`IsActive = true/false`)
- Encounter prevention during military service  
- Smooth transitions in/out of service
- No pathfinding crashes or "ghost party" bugs

## Behavior

**During Enlistment:**
1. Player enlists → `MobileParty.MainParty.IsActive = false`
2. Game engine stops considering player for encounters  
3. Player follows lord's army without map interference
4. Continuous monitoring maintains state

**During Service:**
- Player party invisible to encounter system
- No bandit raids, caravan interactions, or settlement encounters
- Player experiences world through lord's army instead

**On Retirement:**
1. Player leaves service → `MobileParty.MainParty.IsActive = true`
2. Game engine re-enables normal encounter behavior
3. Player returns to normal world interaction

## Technical Implementation

**Files:**
- `EncounterGuard.cs` - Static utility class for encounter state management

**Core Methods:**
```csharp
public static void DisableEncounters()
{
    MobileParty.MainParty.IsActive = false;
}

public static void EnableEncounters()  
{
    MobileParty.MainParty.IsActive = true;
}
```

**Monitoring:**
- Called from `EnlistmentBehavior.OnTick()` for continuous enforcement
- State checked every frame during military service
- Immediate response to enlistment status changes

## Edge Cases

**Player Manually Changes Party State:**
- System continuously resets `IsActive = false` during service
- Prevents player from accidentally re-enabling encounters

**Game Engine State Conflicts:**
- Monitor for unexpected `IsActive` changes
- Log warnings if state doesn't match expected service status
- Recover automatically on next tick

**Save/Load During Service:**
- `IsActive` state persists correctly through save/load
- No additional serialization needed (game handles it)

**Lord Death While Enlisted:**
- Encounter safety disabled before retirement processing
- Prevents crashes during emergency service termination

## Acceptance Criteria

- ✅ No map encounters during military service
- ✅ No pathfinding crashes when player follows lord
- ✅ Smooth enlistment transitions (no visual glitches)  
- ✅ Player party properly hidden from map systems
- ✅ Normal encounter behavior restored after retirement
- ✅ State maintained correctly through save/load
- ✅ Works in all map situations (settlements, battles, travel)

## Debugging

**Common Issues:**
- **Encounters still happening**: Check `IsActive` is actually false, monitor continuous setting
- **Player stuck invisible**: Verify encounter re-enabling on retirement/desertion
- **Pathfinding crashes**: Usually means `IsActive` not set properly during state transitions

**Testing:**
- Check `MobileParty.MainParty.IsActive` value in debugger
- Look for encounter system logs trying to access inactive party
- Verify state changes during enlist/retire operations

**Log Categories:**  
- "EncounterGuard" - State management operations
- "Enlistment" - Service state changes that affect encounters
