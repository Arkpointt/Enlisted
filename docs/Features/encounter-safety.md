# Feature Spec: Encounter Safety System

## Overview
Prevents the player from triggering map encounters while enlisted by using the game engine's `IsActive` property and Harmony patches, with enhanced battle participation and cleanup logic.

## Purpose
Keep enlisted players from accidentally entering encounters (like bandit raids) that would break military service or cause pathfinding crashes, while ensuring they *do* correctly join their Lord's battles without engine conflicts.

## Inputs/Outputs

**Inputs:**
- Enlistment status changes (join/leave service)
- Real-time party state monitoring
- Lord army status and position
- **MapEvent State**: Monitoring active battles involving the Lord

**Outputs:**
- Player party visibility controlled (`IsActive = true/false`)
- Encounter prevention during peaceful service
- **Controlled Engagement**: Keep the player party attached/active so native battle collection scoops it up
- **Battle Menu Routing**: Ensuring "Encounter" menu instead of "Help or Leave"
- Reliable cleanup when returning to normal campaign play

## Behavior

**During Enlistment (Peace/Travel):**
1. Player enlists → `MobileParty.MainParty.IsActive = false`
2. Nameplate hidden via `HidePartyNamePlatePatch`
3. Game engine stops considering player for encounters  
4. Player follows lord's army without map interference

**During Service (Battle):**
1. Lord enters battle (`MapEvent` detected)
2. `EnlistmentBehavior` activates player party (`IsActive = true`) while keeping it visually hidden
3. Player party is kept in the same army/attachment so the vanilla encounter stack automatically collects it
4. **JoinEncounterAutoSelectPatch** intercepts the "join_encounter" menu and automatically joins the battle on the lord's side
5. Player sees the standard encounter menu (Attack/Send Troops/Wait) instead of "Help X's Party / Don't get involved"
6. Player participates in battle

**Autosim vs Manual Battle Handling:**
- **Manual Battle**: Player's troops participate directly in combat
- **Autosim (Send Troops)**: We now rely on vanilla autosim (player party isn't injected). After seeing the report, expect the encounter menu (Attack/Surrender) if the army loses.
- **Why This Matters**: The player always resolves the outcome through the standard encounter UI, keeping behavior predictable.

**On Retirement:**
1. Player leaves service → `MobileParty.MainParty.IsActive = true`
2. Game engine re-enables normal encounter behavior
3. Player returns to normal world interaction

## Technical Implementation

**Files:**
- `EncounterGuard.cs` - Static utility class for encounter state management
- `EnlistmentBehavior.cs` - Active battle monitoring and participation logic
- `HidePartyNamePlatePatch.cs` - UI suppression
- `JoinEncounterAutoSelectPatch.cs` - Auto-joins lord's battle, bypasses choice menu

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

**Siege Menu Loop:**
- Fixed by suppressing `IsActive` during siege "waiting" phase
- Only active during actual assault (`MapEvent`)

**Lord Death While Enlisted:**
- Encounter safety disabled before retirement processing
- Prevents crashes during emergency service termination

## Acceptance Criteria

- ✅ No map encounters during military service (Peace/Travel)
- ✅ **Correct Encounters** during War (Battle menus work)
- ✅ No pathfinding crashes when player follows lord
- ✅ Smooth enlistment transitions (no visual glitches)  
- ✅ Player party properly hidden from map systems (UI & Logical)
- ✅ Normal encounter behavior restored after retirement
- ✅ State maintained correctly through save/load
- ✅ Works in all map situations (settlements, battles, travel)

## Debugging

**Common Issues:**
- **Encounters still happening**: Check `IsActive` is actually false
- **Not joining battles**: Check `MapEvent` detection logic in `EnlistmentBehavior`
- **"Help or Leave" menu**: Fixed by `JoinEncounterAutoSelectPatch` - if still appearing, check patch is registered in logs
- **"Attack or Surrender" after autosim defeat**: Expected outcome when the lord loses. The player should resolve the encounter manually (Attack/Surrender) to match native behavior.

**Log Categories:**  
- "EncounterGuard" - State management operations
- "Enlistment" - Service state changes that affect encounters
- "Battle" - Battle join logic and vanilla encounter coordination
