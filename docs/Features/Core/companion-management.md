# Companion Management System

## Quick Reference

| Feature | Tier Requirement | Location |
|---------|-----------------|----------|
| Companions in lord's party | Tier 1-3 | Lord's party roster |
| Companions in player's squad | Tier 4+ | Player's party roster |
| Battle participation toggle | Tier 4+ | Camp menu |
| Unified squad formation | Tier 4+ | Battle missions |

## Table of Contents

- [Overview](#overview)
- [How It Works](#how-it-works)
  - [Tier-Based Companion Location](#tier-based-companion-location)
  - [Battle Formation Assignment](#battle-formation-assignment)
  - [Companion Battle Participation](#companion-battle-participation)
- [Technical Details](#technical-details)
  - [Companion Transfer Logic](#companion-transfer-logic)
  - [Formation Assignment](#formation-assignment)
  - [Battle Participation State](#battle-participation-state)
- [Edge Cases](#edge-cases)
- [API Reference](#api-reference)
- [Debugging](#debugging)

---

## Overview

The companion management system handles where companions are located during enlistment and whether they participate in battles. The system adapts based on the player's military rank (tier), moving companions from the lord's party to the player's personal squad when the player gains command authority.

**Key Behaviors:**
- **Tier 1-3**: Companions serve in the lord's party, fighting as part of the lord's army
- **Tier 4+**: Companions join the player's personal squad, fighting together in unified formation
- **Battle Participation**: Players can toggle which companions fight vs. stay safe (Tier 4+)

---

## How It Works

### Tier-Based Companion Location

Companion location changes automatically when the player reaches Tier 4:

| Tier | Companion Location | Battle Behavior |
|------|-------------------|-----------------|
| 1-3 | Lord's party | Fight as part of lord's army, no player control |
| 4+ | Player's party | Fight in player's unified squad, player can command |

**Why Tier 4+?**
- Tier 4 unlocks personal command (Lance of 5 soldiers)
- Companions + retinue form the player's "squad"
- Player's invisible party joins battles natively
- Casualties tracked by native roster system

**Transfer Timing:**
- Companions move from lord's party to player's party when player reaches Tier 4
- Transfer happens automatically during promotion
- Companions stay with player for remainder of enlistment

### Battle Formation Assignment

When a battle starts at Tier 4+, the player, companions, and retinue fight together as a unified squad.

**Formation Behavior:**
- All player party troops assigned to the **same formation**
- Formation type based on player's duty (Infantry/Archer/Cavalry/HorseArcher)
- Player can command their own squad (move, charge, hold)
- Player **cannot** command other formations or army
- Player **cannot** access Order of Battle screen

**Command Authority:**
- Player is "Sergeant" (commands own formation only)
- Player is **not** "General" (cannot command entire army)
- Formation ownership: `Formation.PlayerOwner = playerAgent`
- Order controller transferred to lord
- **Companions cannot command** - blocked from becoming captains or generals

**Implementation:**
- Handled by `EnlistedFormationAssignmentBehavior`
- Assigns all agents from `PartyBase.MainParty` to same formation
- Suppresses general/captain roles via `Team.SetPlayerRole(false, true)`

### Companion Battle Participation

At Tier 4+, players can control which companions fight in battles through the Camp menu.

**Settings:**
- **Fight** (default): Companion spawns in battle, faces all risks
- **Stay Back**: Companion remains in roster, doesn't spawn, immune to battle outcomes

**Command Restrictions:**
- Companions **cannot** become formation captains (blocked via Harmony patch)
- Companions **cannot** become team general (blocked via Harmony patch)
- Prevents companions from giving tactical orders or appearing as commanders
- Applies to ALL companions during enlistment, regardless of "Fight" or "Stay Back" setting

**Why "Stay Back" Is Safe:**
- Native battle resolution only processes **spawned agents**
- Troops in roster who never spawn are untouched
- "Stay back" companions survive:
  - Normal battles (not spawned)
  - Army destruction (stay in roster)
  - Player capture (stay in deactivated roster)
  - All battle outcomes (not subject to casualty tracking)

**UI Location:**
- Camp → Companion Assignments
- Shows list of companions with toggle (⚔️ Fight / 🏕️ Stay Back)
- Changes saved immediately (no save button needed)

---

## Technical Details

### Companion Transfer Logic

**On Enlistment Start (Tier 1-3):**
```csharp
// All companions transferred to lord's party
TransferPlayerTroopsToLord();
// Uses MemberRoster.AddToCounts() for transfer
```

**On Tier 4 Promotion:**
```csharp
// Companions moved FROM lord's party TO player's party
TransferCompanionsToPlayer();
// Scans lord's party for IsPlayerCompanion == true
// Moves them to MobileParty.MainParty.MemberRoster
```

**On Retirement/End Service:**
```csharp
// Companions already in player's party (Tier 4+)
// No transfer needed - they stay with player
RestoreCompanionsToPlayer(); // Only needed for Tier 1-3 edge cases
```

### Formation Assignment

**File:** `src/Features/Combat/Behaviors/EnlistedFormationAssignmentBehavior.cs`

**Key Methods:**
- `TryAssignPlayerToFormation()` - Assigns player to duty-based formation
- `TryAssignPlayerPartyToFormation()` - Assigns all player party troops to same formation, teleports if needed
- `TryTeleportPlayerToFormationPosition()` - Teleports player to formation center
- `SuppressPlayerCommand()` - Removes generalship, sets sergeant role
- `IsReinforcementPhase()` - Detects if spawning as reinforcement vs initial deployment

**Formation Assignment Flow:**
```
Battle Starts
    ↓
Player party troops spawn via native PartyGroupTroopSupplier
    ↓
EnlistedFormationAssignmentBehavior activates
    ↓
Get player's duty formation (Infantry/Ranged/Cavalry/HorseArcher)
    ↓
Assign player agent to that formation
    ↓
Detect spawn type (initial vs reinforcement):
  - Initial spawn: teleport if > 5m from formation
  - Reinforcement: always teleport (spawns at map edge)
    ↓
Find all agents from player's party → assign to SAME formation
    ↓
Teleport reassigned squad members to player's position
    ↓
Set player as PlayerOwner of formation (squad commands enabled)
    ↓
Suppress general/captain roles (army commands disabled)
```

**Spawn Detection:**
- Uses `MissionAgentSpawnLogic.IsInitialSpawnOver` to detect reinforcement phase
- Initial spawn: troops deploy near their formation positions
- Reinforcement spawn: troops spawn at map edge and run onto battlefield
- Reinforcements always teleported to formation regardless of distance

### Battle Participation State

**File:** `src/Features/CommandTent/Core/CompanionAssignmentManager.cs`

**State Storage:**
```csharp
// Key: Hero.StringId, Value: true = fight, false = stay back
private Dictionary<string, bool> _companionBattleParticipation;
```

**Spawn Filtering:**
- In `EnlistedFormationAssignmentBehavior.OnAgentBuild()`
- Check `ShouldCompanionFight(hero)` before assigning to formation
- If "stay back": call `agent.FadeOut()` to remove without casualty tracking

**Command Blocking:**
- Harmony patches intercept `Formation.Captain` and `Team.GeneralAgent` setters
- Blocks ALL player companions from being assigned to command roles
- Prevents high-power companions from being selected as generals/captains by vanilla logic
- Ensures "Stay Back" companions don't appear giving tactical announcements

**Persistence:**
- Saved via `SyncData` serialization
- Persists across save/load
- Default: all companions fight (true)

---

## Edge Cases

### Reinforcement Spawn

**Scenario:** Player joins battle as reinforcement (initial troops already deployed)

**Behavior:**
- Player and squad spawn at map edge (native reinforcement behavior)
- System detects reinforcement phase via `MissionAgentSpawnLogic.IsInitialSpawnOver`
- Player and all squad members **always teleported** to formation center
- No distance threshold - reinforcements can spawn anywhere on map edge
- Squad reassigned to player's duty formation (Infantry/Ranged/etc.) if they spawned in wrong formation

### Retinue Type Differs from Duty Formation

**Scenario:** Player duty is Ranged (archer), but retinue soldiers are Infantry type

**Behavior:**
- Game spawns infantry retinue with Infantry formation (their natural troop class)
- System detects retinue soldiers are from player's party
- Retinue reassigned from Infantry to player's Ranged formation
- Retinue teleported to player's position (they spawned at Infantry's location)
- Squad fights together in player's duty formation regardless of troop type

### Army Destroyed

**Scenario:** Player Tier 4+, army is completely destroyed in battle

**Behavior:**
- "Fight" companions: Native handles (killed/captured/escaped)
- "Stay back" companions: **Survive** (stay in roster, weren't spawned)
- Player party released, made visible
- Retinue cleared (separate system)
- Companions remain with player

### Player Captured

**Scenario:** Player is captured during army destruction

**Behavior:**
- "Fight" companions: Captured with player (native handling)
- "Stay back" companions: **Survive** (stay in deactivated roster)
- When player escapes/ransomed: All companions rejoin

### Lord Dies

**Scenario:** Lord dies while player is enlisted

**Behavior:**
- Companions already in player's party (Tier 4+)
- No transfer needed
- Player enters grace period
- Companions stay with player

### Enlistment Ends

**Scenario:** Player retires or ends service

**Behavior:**
- Companions already in player's party (Tier 4+)
- No transfer needed - they stay with player
- "Stay back" state preserved for next enlistment

---

## API Reference

### Companion Detection

```csharp
// Check if hero is player companion
bool isCompanion = hero.IsPlayerCompanion;
// OR
bool isCompanion = hero.Clan == Clan.PlayerClan && !hero.IsHumanPlayerCharacter;

// Find all player companions
var companions = Clan.PlayerClan.Heroes
    .Where(h => h.IsPlayerCompanion && h.IsAlive)
    .ToList();
```

### Companion Location

```csharp
// Where is the companion?
if (hero.PartyBelongedTo != null)
{
    var party = hero.PartyBelongedTo; // In a party
}
else if (hero.PartyBelongedToAsPrisoner != null)
{
    var captorParty = hero.PartyBelongedToAsPrisoner; // Captured
}
else if (hero.CurrentSettlement != null)
{
    var settlement = hero.CurrentSettlement; // In settlement
}
```

### Formation Control

```csharp
// Team role control - determines what player can command
Team.SetPlayerRole(isPlayerGeneral: false, isPlayerSergeant: true);
// (false, true) = Sergeant: commands own formation only

// Formation ownership - who can issue orders
Formation.PlayerOwner = playerAgent; // Player owns this formation

// Agent formation assignment
agent.Formation = targetFormation; // Move agent to formation

// Check if agent is from player's party
var origin = agent.Origin as PartyGroupAgentOrigin;
bool isPlayerPartyTroop = origin?.Party == PartyBase.MainParty;
```

### Battle Participation

```csharp
// Check if companion should fight
bool ShouldCompanionFight(Hero companion)
{
    return _companionBattleParticipation.TryGetValue(companion.StringId, out var fights) 
        ? fights 
        : true; // Default: fight
}

// Remove "stay back" companion from battle
agent.FadeOut(hideInstantly: true, hideMount: true);
```

---

## Debugging

**Log Categories:**
- `"Companions"` - Companion transfers and state changes
- `"FormationAssignment"` - Battle formation assignment
- `"CompanionCommand"` - Companion command blocking (captain/general prevention)

**Key Log Points:**
```csharp
// Companion transfers
ModLogger.Info("Companions", $"Transferred {count} companions to lord's party");
ModLogger.Info("Companions", $"Moved {count} companions to player's squad (Tier 4)");

// Formation assignment
ModLogger.Info("FormationAssignment", $"Assigned enlisted player to {formationClass} formation");
ModLogger.Info("FormationAssignment", $"Teleported player to formation (REINFORCEMENT spawn, was 45.3m away)");
ModLogger.Info("FormationAssignment", $"Player party assignment [REINFORCEMENT]: found=3, teleported=2");
ModLogger.Info("FormationAssignment", $"=== UNIFIED SQUAD FORMED === 3 party members in Ranged formation");
ModLogger.Debug("FormationAssignment", $"Assigned companion {name} to player's squad");
ModLogger.Info("FormationAssignment", $"Command Suppression: Captaincy Stripped={stripped}");

// Battle participation
ModLogger.Debug("Companions", $"Companion {name} marked 'stay back' - removing from battle");

// Command blocking
ModLogger.Debug("CompanionCommand", $"Blocked captain: {name} -> {formation} | Companions cannot command formations while enlisted");
ModLogger.Debug("CompanionCommand", $"Blocked general: {name} -> Team({side}) | Companions cannot be army general while enlisted");
```

**Debug Output Location:**
- `Modules/Enlisted/Debugging/enlisted.log`

**Related Files:**
- `src/Features/Combat/Behaviors/EnlistedFormationAssignmentBehavior.cs`
- `src/Features/CommandTent/Core/CompanionAssignmentManager.cs`
- `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`
- `src/Mod.GameAdapters/Patches/CompanionCaptainBlockPatch.cs` - Command blocking patches

---

## Related Documentation

- [Camp System](../UI/camp-tent.md) - Companion assignments UI
- [Enlistment System](enlistment.md) - Tier progression and service state
- [Formation Training](formation-training.md) - Battle formation details
