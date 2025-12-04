# Companion Management System

## Quick Reference

| Feature | Tier Requirement | Location |
|---------|-----------------|----------|
| Companions in lord's party | Tier 1-3 | Lord's party roster |
| Companions in player's squad | Tier 4+ | Player's party roster |
| Battle participation toggle | Tier 4+ | Command Tent menu |
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

**Implementation:**
- Handled by `EnlistedFormationAssignmentBehavior`
- Assigns all agents from `PartyBase.MainParty` to same formation
- Suppresses general/captain roles via `Team.SetPlayerRole(false, true)`

### Companion Battle Participation

At Tier 4+, players can control which companions fight in battles through the Command Tent menu.

**Settings:**
- **Fight** (default): Companion spawns in battle, faces all risks
- **Stay Back**: Companion remains in roster, doesn't spawn, immune to battle outcomes

**Why "Stay Back" Is Safe:**
- Native battle resolution only processes **spawned agents**
- Troops in roster who never spawn are untouched
- "Stay back" companions survive:
  - Normal battles (not spawned)
  - Army destruction (stay in roster)
  - Player capture (stay in deactivated roster)
  - All battle outcomes (not subject to casualty tracking)

**UI Location:**
- Command Tent → Companion Assignments
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
- `AssignPlayerPartyToFormation()` - Assigns all player party troops to same formation
- `SuppressPlayerCommand()` - Removes generalship, sets sergeant role

**Formation Assignment Flow:**
```
Battle Starts
    ↓
Player party troops spawn via native PartyGroupTroopSupplier
    ↓
EnlistedFormationAssignmentBehavior activates
    ↓
Get player's duty formation (Infantry/Archer/Cavalry/HorseArcher)
    ↓
Assign player agent to that formation
    ↓
Find all agents from player's party → assign to SAME formation
    ↓
Set player as PlayerOwner of formation (squad commands enabled)
    ↓
Suppress general/captain roles (army commands disabled)
```

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

**Persistence:**
- Saved via `SyncData` serialization
- Persists across save/load
- Default: all companions fight (true)

---

## Edge Cases

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

**Key Log Points:**
```csharp
// Companion transfers
ModLogger.Info("Companions", $"Transferred {count} companions to lord's party");
ModLogger.Info("Companions", $"Moved {count} companions to player's squad (Tier 4)");

// Formation assignment
ModLogger.Info("FormationAssignment", $"Assigned enlisted player to {formationClass} formation");
ModLogger.Debug("FormationAssignment", $"Assigned companion {name} to player's formation");
ModLogger.Info("FormationAssignment", $"Command Suppression: Captaincy Stripped={stripped}");

// Battle participation
ModLogger.Debug("Companions", $"Companion {name} marked 'stay back' - removing from battle");
```

**Debug Output Location:**
- `Modules/Enlisted/Debugging/enlisted.log`

**Related Files:**
- `src/Features/Combat/Behaviors/EnlistedFormationAssignmentBehavior.cs`
- `src/Features/CommandTent/Core/CompanionAssignmentManager.cs`
- `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`

---

## Related Documentation

- [Command Tent System](../UI/command-tent.md) - Companion assignments UI
- [Enlistment System](enlistment.md) - Tier progression and service state
- [Formation Training](formation-training.md) - Battle formation details
