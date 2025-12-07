# Formation Assignment and Teleportation System

## Quick Reference

| Component              | Behavior                                                                                                     |
|------------------------|--------------------------------------------------------------------------------------------------------------|
| Formation Assignment   | Player automatically assigned to duty-based formation (Infantry/Ranged/Cavalry/Horse Archer) at battle start |
| Position Teleportation | Player and squad teleported to their assigned formation's position (5m behind center)                        |
| Teleport Priority      | Formation position → Lord position → Any allied agent                                                        |
| Retry Window           | 120 attempts (~2 seconds at 60fps) to allow formations to populate                                           |

## Table of Contents

- [Overview](#overview)
- [How It Works](#how-it-works)
    - [Formation Assignment](#formation-assignment)
    - [Position Teleportation](#position-teleportation)
    - [Reference Agent Search](#reference-agent-search)
- [Technical Details](#technical-details)
    - [System Architecture](#system-architecture)
    - [Agent Search Priority](#agent-search-priority)
    - [Spawn Timing Considerations](#spawn-timing-considerations)
- [Edge Cases](#edge-cases)
- [API Reference](#api-reference)
- [Debugging](#debugging)

---

## Overview

Automatically assigns the enlisted player to their designated formation based on duty type (Infantry, Ranged, Cavalry,
Horse Archer) when a battle starts, then teleports the player and their squad to their assigned formation's position to
ensure they spawn with their unit type instead of behind the army.

**Key Features:**

- Automatic formation assignment matching player's duty type
- Teleportation to formation position (e.g., archers teleport to archer formation)
- Player spawns 5m behind formation center to avoid being stuck in the middle
- Squad command authority at Tier 4+ (player controls their own formation)
- Companion command blocking (companions cannot become captains or generals)
- Handles late spawns, reinforcements, and single-team scenarios

**Purpose:**
Fix the issue where the player's map party spawns slightly behind the lord's army when battle starts, causing the player
to appear in the wrong location. This system ensures the player spawns at their assigned formation's position - if
you're an archer, you spawn with the archers; if infantry, you spawn with the infantry.

**Files:**

- `src/Features/Combat/Behaviors/EnlistedFormationAssignmentBehavior.cs` - Mission behavior that handles formation
  assignment and teleportation
- `src/Mod.GameAdapters/Patches/CompanionCaptainBlockPatch.cs` - Harmony patches preventing companions from commanding
  formations or armies

---

## How It Works

### Formation Assignment

**When:** Battle start, deployment finished, agent spawn, or mission tick

**Process:**

1. System checks if player is enlisted and not on leave
2. Retrieves player's formation type from duties system (Infantry/Ranged/Cavalry/Horse Archer)
3. Gets the corresponding formation from player's team
4. Assigns player agent to that formation
5. At Tier 4+, makes formation player-controlled for squad commands
6. Marks position fix as needed to trigger teleportation

**Formation Types:**

- `FormationClass.Infantry` - For "infantry" duty
- `FormationClass.Ranged` - For "ranged" or "archer" duty
- `FormationClass.Cavalry` - For "cavalry" duty
- `FormationClass.HorseArcher` - For "horsearcher" or "horse_archer" duty

**Tier-Based Behavior:**

- **Below Tier 4**: Player joins formation as a soldier, no command authority
- **Tier 4+**: Player becomes sergeant, controls their own formation, companions and retinue join the same formation

**Companion Command Restrictions:**

- Companions **cannot** become formation captains (blocked via Harmony patch)
- Companions **cannot** become team general (blocked via Harmony patch)
- Prevents "Stay Back" companions from appearing and giving tactical orders
- Ensures only lords and non-player heroes can command formations/armies

### Position Teleportation

**When:** After formation assignment is complete, retries for up to 2 seconds

**Process:**

1. System detects position fix is needed (player spawned in wrong location)
2. Gets player's formation class from duties system (Infantry/Ranged/Cavalry/HorseArcher)
3. Gets that formation from the player's team (all allied troops are on the same team)
4. Calculates target position: formation's CachedMedianPosition minus 5m (behind formation)
5. If distance > 10m threshold, teleports player to formation position
6. Sets movement direction and look direction to match formation
7. Forces cache update to ensure position sticks
8. At Tier 4+, also teleports squad members (companions + retinue) to nearby positions

**Teleport Priority:**

1. **Formation Position** - Uses the team's formation matching player's duty (Infantry, Ranged, etc.)
2. **Lord Position** - Fallback if formation not found or has no units
3. **Any Allied Agent** - Last resort fallback

**Teleport Threshold:**

- **10 meters**: If player is more than 10m from target, teleport is triggered
- **5 meters behind**: Player spawns 5m behind formation center to avoid being stuck in the middle

### Formation Position Lookup

**Critical Implementation Detail:** In Bannerlord battles, all allied troops (lord's army + player's party) are on the *
*same team**. There are no separate allied teams to search - everyone fighting together is on one Team object.

**Position Priority:**

**Priority 1 - Formation Position (Primary):**

```csharp
var targetFormation = team.GetFormation(formationClass);
if (targetFormation != null && targetFormation.CountOfUnits > 0)
{
    targetPosition = targetFormation.CachedMedianPosition.GetGroundVec3();
    // Offset 5m behind formation center
    targetPosition += -formationDirection.ToVec3() * 5f;
}
```

- Gets the formation matching player's duty type from the player's team
- Uses `CachedMedianPosition` which represents the center of all units in that formation
- Includes both player party members and lord's army troops
- Position offset 5m behind to avoid spawning in middle of troops

**Priority 2 - Lord Position (Fallback):**

```csharp
// Search all teams on same side for lord agent
foreach (var agent in missionTeam.ActiveAgents)
    if (agent.Character == currentLord.CharacterObject)
        targetPosition = agent.Position;
```

- Used if formation has no units or doesn't exist
- Searches all allied teams for the lord agent
- Falls back to lord's actual position

**Priority 3 - Any Allied Agent (Last Resort):**

```csharp
// Find any non-player-party agent on allied team
foreach (var agent in missionTeam.ActiveAgents)
    if (agent.Origin is NOT PartyGroupAgentOrigin from MainParty)
        targetPosition = agent.Position;
```

- Used only if both formation and lord not found
- Searches for any army troop not from player's party
- Ensures some valid teleport target exists

**Why Formation Position Works:**

The formation's `CachedMedianPosition` includes all units assigned to that formation type:

- Lord's infantry/archers/cavalry/horse archers
- Player's retinue (if same formation type)
- Any reinforcements

This gives the true center of where that unit type is positioned on the battlefield, ensuring players spawn with their
assigned troop type.

---

## Technical Details

### System Architecture

**Mission Behavior:**

- `EnlistedFormationAssignmentBehavior` is added to missions automatically
- Inherits from `MissionBehavior` with `BehaviorType.Other`
- Responds to multiple callbacks: `AfterStart()`, `OnDeploymentFinished()`, `OnAgentBuild()`, `OnMissionTick()`

**State Tracking:**

- `_assignedAgent` - Tracks which agent instance has been assigned (prevents re-assignment)
- `_needsPositionFix` - Flag indicating teleportation is needed
- `_positionFixAttempts` - Counter for retry attempts (max 120)
- `_playerSquadFormation` - Cached reference to player's formation for squad assignment

**Key Callbacks:**

```csharp
AfterStart()          // Initial assignment attempt
OnDeploymentFinished() // Reliable point where agents exist
OnAgentBuild()        // Catches late spawns and reinforcements
OnMissionTick()       // Fallback retry mechanism
```

**Companion Command Blocking:**

The vanilla game assigns heroes as formation captains and team generals based on power level. This would cause player
companions (especially high-power ones) to be selected as commanders, breaking the enlisted soldier experience.

**Harmony Patches:**

- `CompanionCaptainBlockPatch` - Intercepts `Formation.Captain` setter to block companion assignments
- `CompanionGeneralBlockPatch` - Intercepts `Team.GeneralAgent` setter to block companion assignments

**When Active:**

- Only applies when player is enlisted at Tier 4+ (when companions participate in battles)
- Blocks ALL player companions, regardless of "Fight" or "Stay Back" setting
- Ensures lords and non-player heroes can still command normally

**Why This Matters:**

- Prevents companions from giving tactical announcements ("Our plan is to...")
- Prevents companions from appearing as formation captains when set to "Stay Back"
- Maintains the enlisted soldier role - player is a soldier, not a commander

### Teleport Target Priority

The teleport target search uses a three-tier priority system:

1. **Formation Position** - Primary: uses the formation's CachedMedianPosition
2. **Lord Position** - Fallback: finds lord agent and uses their position
3. **Any Allied Agent** - Last resort: uses any non-player-party agent

This priority system ensures the teleportation can work even when:

- Formation has no units yet (falls back to lord)
- Lord hasn't spawned yet (falls back to any army agent)
- Only player party agents are visible initially
- Complex spawn timing scenarios

### Spawn Timing Considerations

**Problem:** Game engine spawns agents in waves:

1. Player's party (player + companions + retinue)
2. Lord's party
3. Other army members
4. Reinforcements

**Solution:**

- Extended retry window from 30 attempts (~0.5s) to 120 attempts (~2s)
- Multiple callback hooks catch spawn at different stages
- Cross-team search finds lord even if on different Team object
- Graceful failure: if lord never found, system stops retrying (no spam)

**Why 120 Attempts:**

- Gives lord time to spawn even if delayed
- Accounts for mission initialization overhead
- Allows for multiple spawn waves
- Still fast enough to feel instant to player

---

## Edge Cases

**Naval Battles:**

- System detects `Mission.Current?.IsNavalBattle == true`
- Skips formation assignment and teleportation
- Naval DLC has its own ship-based spawn system
- Ground-based teleportation would interfere with ship positioning

**Lord Not Found:**

- If lord agent never appears in any team after 120 attempts, teleportation is skipped
- Player remains in assigned formation but at spawn location
- No error spam or crashes, graceful degradation

**Multiple Teams:**

- System searches all teams with matching Side value
- Finds lord even if on different Team object
- Handles complex army structures

**Reinforcements:**

- Detects reinforcement phase via `MissionAgentSpawnLogic.IsInitialSpawnOver`
- Uses 0m teleport threshold (always teleport) for reinforcements
- Reinforcements spawn at map edge, need teleport to reach formation

**Mount Handling:**

- If player is mounted, `TeleportToPosition` automatically teleports mount
- Mount position synchronized with player position
- Rider agents also teleported if present

**Formation Already Assigned:**

- System checks if player already in correct formation
- Still triggers position fix check (may have spawned in wrong location)
- Prevents unnecessary re-assignment

---

## API Reference

**Key APIs Used:**

```csharp
// Formation assignment
playerAgent.Formation = targetFormation;

// Teleportation
playerAgent.TeleportToPosition(targetPosition);
playerAgent.SetMovementDirection(formationDirection);
playerAgent.LookDirection = formationDirection.ToVec3();
playerAgent.ForceUpdateCachedAndFormationValues(true, false);

// Formation control (Tier 4+)
formation.SetControlledByAI(false);
formation.PlayerOwner = playerAgent;
team.SetPlayerRole(isPlayerGeneral: false, isPlayerSergeant: true);

// Agent search
team.ActiveAgents                    // Current team's active agents
Mission.Current.Teams                // All teams in mission
agent.Character == lord.CharacterObject  // Character matching
agent.Origin is PartyGroupAgentOrigin    // Origin type checking

// Spawn phase detection
_spawnLogic.IsInitialSpawnOver      // True after initial deployment
```

**Key Classes:**

- `Agent` - Player and NPC agents in battle
- `Formation` - Battle formations (Infantry, Ranged, Cavalry, Horse Archer)
- `Team` - Battle teams (player's side vs enemy side)
- `MissionAgentSpawnLogic` - Tracks spawn phases

---

## Debugging

**Common Issues:**

**Player not teleporting:**

- Check logs for "Teleported player to X formation" message
- Verify formation has units (log shows unit count)
- Check if battle is naval (teleportation skipped)
- Verify player is enlisted and not on leave

**Player teleports but reverts:**

- Check scripted movement flags (should be None)
- Verify formation AI isn't overriding position
- Check mount synchronization if player is mounted
- Look for spawn logic interference

**Formation assignment fails:**

- Verify duties system has player's formation type set
- Check if formation exists on team
- Verify player agent is active and valid
- Check if already assigned (prevents re-assignment)

**Log Messages (INFO level):**

- `"Teleported player to Infantry formation (90 units)"` - Success, shows formation type and unit count
- `"Teleported player to Lord Debana"` - Fallback to lord position (formation not found)
- `"Teleported player to allied agent"` - Last resort fallback

**Debug Logs (DEBUG level - enable for troubleshooting):**

- `"Teleport search: Looking for X formation"` - Shows search parameters
- `"Found X formation on player's team (N units)"` - Formation found
- `"No X formation found, falling back to lord position"` - Fallback triggered

**Verification Steps:**

1. Player should be in correct formation based on duty type
2. Player should spawn near their formation (within 10m, 5m behind center)
3. At Tier 4+, companions and retinue should join same formation
4. Teleportation should complete within 2 seconds of battle start
5. Position should remain stable (not revert after teleport)

---

## Implementation Notes

**Why Formation Position Instead of Lord:**

The original implementation searched for the lord agent as a teleport reference. However, this had issues:

- Player would spawn at lord's position, not with their assigned unit type
- Archers would spawn near infantry if lord was with infantry
- Didn't reflect the "serve in a formation" fantasy

The new implementation uses the formation's `CachedMedianPosition`:

- Player spawns with their assigned troop type (archers with archers, infantry with infantry)
- Formation position includes all units of that type (lord's army + player's party)
- 5m offset behind formation center prevents spawning stuck in the middle

**Why Same Team, Not Separate Teams:**

In Bannerlord battles, all allied troops are on the **same Team object**. There are typically only 2 teams:

- Team 0: One side (e.g., Defenders)
- Team 1: Other side (e.g., Attackers)

The lord's army and player's party are both on the same team. The formation system groups troops by type (
Infantry/Ranged/Cavalry/HorseArcher) within each team.

**Getting Formation Class Correctly:**

The formation class must come from the duties system, not from `formation.FormationIndex`:

```csharp
// WRONG - can return incorrect values like NumberOfRegularFormations
var formationClass = formation.FormationIndex;

// CORRECT - uses the duties system
var formationString = duties?.PlayerFormation ?? "infantry";
var formationClass = GetFormationClassFromString(formationString);
```

**Squad Teleportation:**

At Tier 4+, the system also teleports squad members (companions + retinue) to positions near the player:

- Grid layout: 3 columns, multiple rows
- 1.5m spacing between squad members
- Positioned behind player slightly
- Faces same direction as formation

This keeps the squad together while maintaining formation appearance.

