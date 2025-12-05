# Formation Assignment and Teleportation System

## Quick Reference

| Component | Behavior |
|-----------|----------|
| Formation Assignment | Player automatically assigned to duty-based formation (Infantry/Ranged/Cavalry/Horse Archer) at battle start |
| Position Teleportation | Player and squad teleported to lord's position to match army deployment |
| Reference Agent Search | Searches for lord agent directly across all allied teams |
| Retry Window | 120 attempts (~2 seconds at 60fps) to allow lord to spawn |

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

Automatically assigns the enlisted player to their designated formation based on duty type (Infantry, Ranged, Cavalry, Horse Archer) when a battle starts, then teleports the player and their squad to the lord's position to ensure they spawn with the army instead of behind it.

**Key Features:**
- Automatic formation assignment matching player's duty type
- Teleportation to lord's position to fix spawn location issues
- Squad command authority at Tier 4+ (player controls their own formation)
- Companion command blocking (companions cannot become captains or generals)
- Handles late spawns, reinforcements, and multi-team scenarios

**Purpose:**
Fix the issue where the player's map party spawns slightly behind the lord's army when battle starts, causing the player to appear in the wrong location (behind formations instead of with them). This system ensures the player spawns at the correct position mimicking being part of the lord's army deployment.

**Files:**
- `src/Features/Combat/Behaviors/EnlistedFormationAssignmentBehavior.cs` - Mission behavior that handles formation assignment and teleportation
- `src/Mod.GameAdapters/Patches/CompanionCaptainBlockPatch.cs` - Harmony patches preventing companions from commanding formations or armies

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
2. Searches for lord agent as teleport reference (see [Reference Agent Search](#reference-agent-search))
3. Calculates distance from player to lord
4. If distance > 10m threshold, teleports player to lord's position
5. Sets movement direction and look direction to match formation
6. Forces cache update to ensure position sticks
7. At Tier 4+, also teleports squad members (companions + retinue) to nearby positions

**Teleport Threshold:**
- **10 meters**: If player is more than 10m from lord, teleport is triggered
- This handles both initial spawn (player party lagged behind) and reinforcement scenarios

### Reference Agent Search

**Critical Implementation Detail:** The system must find the lord agent directly, not just any army agent, because during early spawn only the player's party members may be visible in `team.ActiveAgents`.

**Search Priority:**

**Priority 1 - Direct Lord Match:**
```
Search team.ActiveAgents for agent where:
  agent.Character == currentLord.CharacterObject
```
- Checks all active agents on player's team
- Matches by character object reference
- Most reliable method when lord has spawned

**Priority 2 - Non-Player-Party Agent:**
```
Search team.ActiveAgents for agent where:
  agent.Origin is NOT PartyGroupAgentOrigin from MainParty
  AND agent.Formation != null
```
- Fallback if lord not found in Priority 1
- Looks for any army troop (not from player's party)
- Useful when lord hasn't spawned but other army troops have

**Priority 3 - Cross-Team Lord Search:**
```
Search all Mission.Current.Teams where:
  team.Side == playerTeam.Side (allied teams)
  AND find agent where agent.Character == currentLord.CharacterObject
```
- Searches all allied team objects
- Handles cases where lord is on different Team object
- Ensures lord can be found even if team structure is complex

**Why This Matters:**

The original implementation searched for any non-player-party agent, but during the initial spawn window (first ~0.5-2 seconds), `team.ActiveAgents` may only contain the player's party members:
- Player agent
- Companions (if Tier 4+)
- Retinue soldiers (if Tier 4+)

The lord's army troops may not have spawned yet, or may be in a different collection. By directly searching for the lord's character object, we can find them even if they're in a different team object or spawn slightly later than expected.

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

The vanilla game assigns heroes as formation captains and team generals based on power level. This would cause player companions (especially high-power ones) to be selected as commanders, breaking the enlisted soldier experience.

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

### Agent Search Priority

The reference agent search uses a three-tier priority system to handle various spawn scenarios:

1. **Direct Lord Match** - Most reliable, works when lord has spawned
2. **Non-Player-Party Agent** - Fallback for early spawn scenarios
3. **Cross-Team Search** - Handles complex team structures

This priority system ensures the teleportation can work even when:
- Lord spawns after player's party
- Lord is on different Team object
- Only player party agents are visible initially
- Multiple team objects exist for the same side

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
- Check if lord agent is found (look for "Lord agent not found yet" in logs)
- Verify `EnlistmentBehavior.Instance.CurrentLord` is not null
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

**Log Categories:**
- `FormationAssignment` - Main log category for this system
- `CompanionCommand` - Companion command blocking (captain/general prevention)
- Look for messages starting with "Assigned enlisted player", "Teleported player", "Lord agent not found"
- Look for "Blocked captain" or "Blocked general" messages when debugging companion issues

**Debug Flags:**
- Set log level to Debug for detailed teleportation attempt logs
- Monitor retry attempts (should succeed within 120 attempts)
- Check reference agent search priority (Priority 1, 2, or 3)

**Verification Steps:**
1. Player should be in correct formation based on duty type
2. Player should spawn near lord's position (within 10m)
3. At Tier 4+, companions and retinue should join same formation
4. Teleportation should complete within 2 seconds of battle start
5. Position should remain stable (not revert after teleport)

---

## Implementation Notes

**Why Direct Lord Search:**

The original implementation searched for any non-player-party agent as a teleport reference. However, during the initial spawn window, `team.ActiveAgents` often only contains:
- Player agent
- Player's companions (if Tier 4+)
- Player's retinue soldiers (if Tier 4+)

The lord's army troops may not have spawned yet or may be in a different collection. By directly searching for the lord's character object (`agent.Character == currentLord.CharacterObject`), we can:
- Find the lord even if on different Team object
- Work when only player party is visible
- Handle delayed spawn scenarios
- Ensure consistent reference point

**Retry Window:**

Extended from 30 attempts (~0.5s) to 120 attempts (~2s) because:
- Lord may spawn after player's party
- Mission initialization can delay spawns
- Multiple spawn waves need time
- Cross-team search may need multiple attempts

This ensures teleportation succeeds even in slow-spawn scenarios while remaining fast enough to feel instant.

**Squad Teleportation:**

At Tier 4+, the system also teleports squad members (companions + retinue) to positions near the player:
- Grid layout: 3 columns, multiple rows
- 1.5m spacing between squad members
- Positioned behind player slightly
- Faces same direction as formation

This keeps the squad together while maintaining formation appearance.

