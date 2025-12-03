# Feature Spec: Companion Management System

## Table of Contents

- [Overview](#overview)
- [Purpose](#purpose)
- [Current Implementation Status](#current-implementation-status)
- [Battle Formation Assignment](#battle-formation-assignment)
- [Companion State During Enlistment](#companion-state-during-enlistment)
- [Native APIs and Behavior](#native-apis-and-behavior)
- [Mission Companion Options](#mission-companion-options)
- [Edge Cases and Recovery](#edge-cases-and-recovery)
- [Technical Implementation Plan](#technical-implementation-plan)
- [Configuration](#configuration)
- [Acceptance Criteria](#acceptance-criteria)
- [Debugging](#debugging)
- [Future Considerations](#future-considerations)

---

## Overview

Manages player companion behavior during enlistment and missions. Companions can accompany the player on scouting missions, provide skill bonuses, and face risks including capture and death. The system must handle companion state transitions, recovery scenarios, and integration with the existing enlistment system.

## Purpose

1. Allow companions to meaningfully participate in mission content
2. Provide risk/reward gameplay around companion usage
3. Handle all edge cases (defeat, capture, death) gracefully without breaking game state
4. Integrate with existing enlistment companion transfer system
5. Support future mission content that may involve companions

---

## Current Implementation Status

### What Exists (EnlistmentBehavior.cs)

**On Enlistment Start:**
```csharp
// Companions transferred to lord's party
TransferPlayerTroopsToLord();
// - All non-player troops (including companions) moved to lord party
// - Uses MemberRoster.AddToCounts() for transfer
// - Logs count of companions transferred
```

**On Retirement/End Service:**
```csharp
// Only companions returned (not regular troops)
RestoreCompanionsToPlayer();
// - Scans lord's party for heroes with Clan == PlayerClan
// - Transfers them back to player party
// - Regular troops stay with lord
```

**Current Companion Location During Service:**
- Companions are in the lord's party
- They fight in the lord's battles
- They can be wounded or captured as part of lord's party
- Player has no direct control over them

### Key Insight - Tier-Based Companion Location

Companion location changes based on player's military rank:

| Tier | Companion Location | Reason |
|------|-------------------|--------|
| 1-3 | Lord's party | Player is a common soldier, no command authority |
| 4+ | Player's party | Player has command authority, leads personal squad |

**Why Tier 4+?**
- At Tier 4, player unlocks personal command (Lance of 5 soldiers)
- Companions + retinue form the player's "squad"
- Player's invisible party joins battles natively
- Casualties tracked by native roster system

---

## Battle Formation Assignment

When a battle starts while enlisted, the player and their squad (companions + retinue at Tier 4+) fight together as a cohesive unit. This is handled by the `EnlistedFormationAssignmentBehavior`.

### Design Goals

1. **Unified Squad**: Player, companions, and retinue fight together in the SAME formation
2. **Command Suppression**: Player cannot issue orders to other formations
3. **Formation Control**: Player can command their own squad only
4. **No Order of Battle**: Player does not get to choose the battle deployment
5. **Native Casualty Tracking**: Deaths update player's roster automatically

### Tier-Based Companion Location

| Tier | Companions In | Battle Behavior |
|------|---------------|-----------------|
| 1-3 | Lord's party | Companions fight as part of lord's army |
| 4+ | Player's party | Companions spawn with player, unified squad |

**At Tier 4 promotion:**
```csharp
// Transfer companions FROM lord's party TO player's party
private void TransferCompanionsToPlayer()
{
    var main = MobileParty.MainParty;
    var lordParty = _enlistedLord?.PartyBelongedTo;
    
    foreach (var troop in lordParty.MemberRoster.GetTroopRoster())
    {
        if (troop.Character.IsHero && 
            troop.Character.HeroObject?.IsPlayerCompanion == true)
        {
            // Move companion to player's party
            lordParty.MemberRoster.AddToCounts(troop.Character, -1);
            main.MemberRoster.AddToCounts(troop.Character, 1);
            
            ModLogger.Info("Companions", 
                $"Transferred {troop.Character.Name} to player's squad");
        }
    }
}
```

### Why Player's Party at Tier 4+?

1. **Native Casualty Tracking**: `MapEventSide.OnTroopKilled()` updates player's roster
2. **Native Command Recognition**: `IsPartyUnderPlayerCommand(MainParty)` returns true
3. **No Manual Spawning**: Troops spawn naturally via `PartyGroupTroopSupplier`
4. **Map Safety**: Player party stays `IsVisible = false`, can't be attacked
5. **Unified Squad**: All player party troops grouped together automatically

### Current Implementation (EnlistedFormationAssignmentBehavior)

**What It Does:**
- Assigns player to a formation based on their duty (Infantry, Archer, Cavalry, Horse Archer)
- Strips player's generalship/captaincy to prevent commanding entire army
- Transfers order controller ownership to the lord
- Teleports player to formation position if spawned incorrectly

**Enhancement Needed (Tier 4+):**
- Assign all agents from player's party to SAME formation as player
- Set player as `PlayerOwner` of their formation (allows commanding own squad)

### Formation Assignment Logic

```csharp
// Assign all player party troops to same formation
private void AssignPlayerPartyToFormation(Agent playerAgent, Formation targetFormation)
{
    // 1. Assign player to formation
    playerAgent.Formation = targetFormation;
    
    // 2. Find all agents from player's party and assign to same formation
    foreach (var agent in playerAgent.Team.ActiveAgents)
    {
        var origin = agent.Origin as PartyGroupAgentOrigin;
        if (origin?.Party == PartyBase.MainParty)
        {
            agent.Formation = targetFormation;
            ModLogger.Debug("FormationAssignment", 
                $"Assigned {agent.Character?.Name} (player party) to squad");
        }
    }
    
    // 3. Set player as owner of THIS formation only
    targetFormation.PlayerOwner = playerAgent;
    
    // 4. Suppress general role
    playerAgent.Team.SetPlayerRole(false, true);  // Sergeant only
}
```

### Command Suppression System

**What Gets Suppressed:**
| Command Type | Suppressed? | Notes |
|--------------|-------------|-------|
| Move all formations | Yes | Player is not general |
| Change army formation | Yes | Player is not general |
| Charge/Retreat army | Yes | Player is not general |
| Command own squad | No | Player owns their formation |
| Move own squad | No | Can issue movement orders to squad |
| Charge own squad | No | Can order squad to charge |

### Key APIs

```csharp
// Check if agent is from player's party
var origin = agent.Origin as PartyGroupAgentOrigin;
bool isPlayerPartyTroop = origin?.Party == PartyBase.MainParty;

// Team role control
Team.SetPlayerRole(isPlayerGeneral, isPlayerSergeant)
// false, true = Sergeant (commands own formation only)

// Formation ownership
Formation.PlayerOwner = agent;  // Who can issue orders

// Agent formation assignment
agent.Formation = formation;
```

### Existing Code Location

`src/Features/Combat/Behaviors/EnlistedFormationAssignmentBehavior.cs`

**Key Methods:**
- `TryAssignPlayerToFormation()` - Assigns player to duty-based formation
- `SuppressPlayerCommand()` - Removes generalship from player
- `TryTeleportPlayerToFormationPosition()` - Fixes spawn position issues

### Enhancement Tasks

- [ ] Add tier check - only transfer companions at Tier 4+
- [ ] Add `TransferCompanionsToPlayer()` on Tier 4 promotion
- [ ] Enhance formation assignment to include all player party troops
- [ ] Test casualty tracking for companions in player's party
- [ ] Test casualty tracking for retinue in player's party
- [ ] Test with field, siege, and sally out battles

---

## Companion State During Enlistment

### Tier-Based Flow

```
Player Enlists (Tier 1-3)
    ↓
TransferPlayerTroopsToLord()
    ↓
[Companions in Lord's Party]
    ↓
Player reaches Tier 4
    ↓
TransferCompanionsToPlayer()
    ↓
[Companions in Player's Party - Unified Squad]
    ↓
Player Retires/Ends Service
    ↓
[Companions stay with Player]
```

### Party State Summary

| State | Player Party | Companions Location | Retinue Location |
|-------|--------------|---------------------|------------------|
| Not Enlisted | Active, Visible | With player | N/A |
| Enlisted Tier 1-3 | Active, **Invisible** | Lord's party | N/A |
| Enlisted Tier 4+ | Active, **Invisible** | **Player's party** | **Player's party** |
| On Leave | Active, Visible | With player | Dismissed |
| Retired | Active, Visible | With player | Dismissed |

**Key Point**: Player's party is always `IsVisible = false` when enlisted, so it cannot be attacked on the campaign map. But it's `IsActive = true` so troops participate in battles.

### Companion Properties (from Decompile)

```csharp
// Key properties for companion tracking
Hero.IsPlayerCompanion     // True if companion belongs to player clan
Hero.CompanionOf           // The clan they belong to (Clan.PlayerClan for player's)
Hero.PartyBelongedTo       // Current party they're in
Hero.PartyBelongedToAsPrisoner  // If captured, the party holding them
Hero.Clan                  // Their actual clan membership
```

### Native Companion Actions

```csharp
// Add companion to clan
AddCompanionAction.Apply(clan, hero);

// Remove companion from clan
RemoveCompanionAction.ApplyByFire(clan, hero);      // Fired
RemoveCompanionAction.ApplyByDeath(clan, hero);     // Death
RemoveCompanionAction.ApplyAfterQuest(clan, hero);  // Quest completion

// Add hero to party
AddHeroToPartyAction.Apply(hero, party, showNotification);

// Transfer via roster (current method)
party.MemberRoster.AddToCounts(hero.CharacterObject, count);
```

---

## Native APIs and Behavior

### Companion Detection

```csharp
// Check if a character is a player companion
bool IsPlayerCompanion(Hero hero)
{
    return hero.IsPlayerCompanion;  // Native property
    // OR
    return hero.Clan == Clan.PlayerClan && !hero.IsHumanPlayerCharacter;
}

// Find all player companions
var companions = Clan.PlayerClan.Heroes
    .Where(h => h.IsPlayerCompanion && h.IsAlive)
    .ToList();
```

### Companion Location Tracking

```csharp
// Where is the companion?
if (hero.PartyBelongedTo != null)
{
    // In a party (player, lord, caravan, etc.)
    var party = hero.PartyBelongedTo;
}
else if (hero.PartyBelongedToAsPrisoner != null)
{
    // Captured by enemy
    var captorParty = hero.PartyBelongedToAsPrisoner;
}
else if (hero.CurrentSettlement != null)
{
    // In a settlement (tavern, prison, etc.)
    var settlement = hero.CurrentSettlement;
}
```

### Companion Capture Handling

When a party is defeated, companions can be:
- Captured (become prisoners)
- Escaped (flee to nearest settlement)
- Killed (rare, configurable)

Native handles this automatically during battle resolution.

---

## Mission Companion Options

### Option A: Companions Stay with Lord (Simple)

During recon mission:
- Player goes alone (with temporary scout troops)
- Companions remain in lord's party
- No companion risk during mission
- Simpler implementation

**Pros:**
- No new companion logic needed
- Consistent with current enlistment behavior
- No risk to player's companions

**Cons:**
- Less immersive
- No companion skill benefits during mission
- Companions feel disconnected

### Option B: Player Can Bring Companions (Recommended)

During recon mission:
- Player can choose to bring 0-N companions
- Companions provide skill bonuses (Scouting, Riding, Tactics)
- Companions face same risks as player (capture, death)
- Must handle recovery if mission fails

**Pros:**
- More engaging gameplay
- Risk/reward choice
- Companion skills matter
- More immersive

**Cons:**
- More complex state management
- Must handle companion capture/recovery
- Edge cases with army defeat

### Recommended: Option B with Safeguards

---

## Mission Companion Behavior (Recommended Design)

### Mission Start - Companion Selection

When recon mission starts:

1. **Check Available Companions**
   - Find companions currently in lord's party (transferred during enlistment)
   - Must be: alive, not wounded, not prisoner

2. **Selection Dialog**
   ```
   "You may bring companions on this mission. Who will accompany you?"
   
   [✓] Scouting Expert (Scout skill: 150) - +30% discovery radius
   [✓] Combat Veteran (One-Handed: 180) - Combat support
   [ ] Healer (Medicine: 120) - Faster wound recovery
   
   [Start Mission with 2 companions]
   [Go Alone]
   ```

3. **Transfer Companions**
   - Move selected companions from lord's party to player party
   - Track which companions are on mission: `_missionCompanions`

### During Mission - Companion Benefits

| Companion Skill | Bonus |
|-----------------|-------|
| Scouting 100+ | +20% discovery radius |
| Scouting 200+ | +40% discovery radius |
| Riding 100+ | +10% escape chance |
| Riding 200+ | +20% escape chance |
| Tactics 100+ | +1 intel quality tier |
| Medicine 100+ | Faster wound recovery |
| Combat skills | Better battle outcomes |

### Mission End - Normal

On successful mission completion:
1. Transfer mission companions back to lord's party
2. Apply any XP gains to companions
3. Clear `_missionCompanions` tracking

### Mission End - Army Defeated

When army is defeated during recon:

1. **Detect Defeat**
   - Army reference becomes null or army loses battle

2. **Companion Handling**
   ```
   [Popup: "The army has been defeated!"]
   
   Your companions [Name1, Name2] were with you on the mission.
   They will remain with you until you find a new lord.
   ```

3. **Companions Stay with Player**
   - Do NOT return to defeated/disbanded army
   - Player keeps them during grace period
   - When player re-enlists, companions transfer to new lord

### Mission End - Player Captured

If player is captured during recon:

1. **Companions Captured Too**
   - Companions with player become prisoners
   - Native captivity system handles them

2. **Recovery**
   - Player can ransom/rescue companions
   - Or wait for natural escape
   - Companions rejoin player when freed

### Mission End - Companions Killed

If companion dies during mission:

1. **Native Death Handling**
   ```csharp
   // Native automatically handles:
   RemoveCompanionAction.ApplyByDeath(Clan.PlayerClan, companion);
   ```

2. **Notification**
   ```
   "[Companion Name] was killed during the scouting mission."
   ```

3. **No Recovery**
   - Death is permanent
   - Logged for player reference

---

## Edge Cases and Recovery

### Edge Case: Lord Dies During Mission

```
State: Player on recon, companions with player
Event: Lord is killed

Handling:
1. Mission cancelled (existing logic)
2. Companions remain with player
3. Player enters grace period
4. When re-enlisting, companions transfer to new lord
```

### Edge Case: Army Disbands During Mission

```
State: Player on recon, companions with player
Event: Army disbands

Handling:
1. Mission cancelled (existing logic)  
2. Companions remain with player
3. Grace period begins
4. Same as lord death
```

### Edge Case: Player Defeated in Battle During Mission

```
State: Player in battle during recon
Event: Player party defeated

Handling:
1. Native battle resolution applies
2. Companions may be: captured, escaped, or dead
3. If captured: both player and companions are prisoners
4. Mission fails (existing logic)
5. Captivity system handles escape/ransom
```

### Edge Case: Save/Load During Mission

```
State: Player on recon with companions
Event: Save and reload

Handling:
1. Persist _missionCompanions list via SyncData
2. On load, validate companions still exist and are alive
3. Remove any dead/missing companions from tracking
4. Mission continues normally
```

### Edge Case: Companion Wounded Mid-Mission

```
State: Companion wounded during mission
Event: Battle occurs, companion takes damage

Handling:
1. Native wound system applies
2. Wounded companions can still participate (reduced effectiveness)
3. If critically wounded, may need to retreat to army
4. Medicine skill reduces wound duration
```

### Edge Case: Player Re-Enlists After Grace Period

```
State: Player in grace period with companions
Event: Player enlists with new lord

Handling:
1. TransferPlayerTroopsToLord() runs (existing)
2. All companions (including any from failed mission) go to new lord
3. Clean state restored
```

---

## Technical Implementation Plan

### Phase 1: Companion Tracking Infrastructure

**New Fields (ReconMissionBehavior):**
```csharp
// Companions currently on mission with player
private List<Hero> _missionCompanions = new();

// Track if companions should stay with player after mission
private bool _keepCompanionsOnMissionEnd = false;
```

**SyncData:**
```csharp
// Persist companion tracking
dataStore.SyncData("_missionCompanionIds", ref _missionCompanionIds);
// Restore Hero references on load
```

### Phase 2: Companion Selection UI

**Before Mission Start:**
```csharp
private void ShowCompanionSelectionDialog()
{
    var availableCompanions = GetAvailableMissionCompanions();
    
    if (availableCompanions.Count == 0)
    {
        // No companions available, start alone
        StartMissionAlone();
        return;
    }
    
    // Show multi-selection dialog
    var options = availableCompanions.Select(c => new InquiryElement(
        c.StringId,
        $"{c.Name} ({GetCompanionBonusDescription(c)})",
        new ImageIdentifier(c.CharacterObject)
    )).ToList();
    
    MBInformationManager.ShowMultiSelectionInquiry(...);
}
```

### Phase 3: Companion Transfer During Mission

**Start Mission:**
```csharp
private void TransferCompanionsForMission(List<Hero> selectedCompanions)
{
    var lordParty = EnlistmentBehavior.Instance?.CurrentLord?.PartyBelongedTo;
    var playerParty = MobileParty.MainParty;
    
    foreach (var companion in selectedCompanions)
    {
        // Move from lord to player
        lordParty.MemberRoster.AddToCounts(companion.CharacterObject, -1);
        playerParty.MemberRoster.AddToCounts(companion.CharacterObject, 1);
        _missionCompanions.Add(companion);
    }
    
    ModLogger.Info("Recon", $"Transferred {_missionCompanions.Count} companions for mission");
}
```

**End Mission (Normal):**
```csharp
private void ReturnCompanionsAfterMission()
{
    if (_keepCompanionsOnMissionEnd)
    {
        // Army defeated - companions stay with player
        ModLogger.Info("Recon", "Companions staying with player (army defeated)");
        _missionCompanions.Clear();
        return;
    }
    
    var lordParty = EnlistmentBehavior.Instance?.CurrentLord?.PartyBelongedTo;
    var playerParty = MobileParty.MainParty;
    
    foreach (var companion in _missionCompanions.ToList())
    {
        if (!companion.IsAlive) continue;
        if (companion.PartyBelongedTo != playerParty) continue;
        
        // Move back to lord
        playerParty.MemberRoster.AddToCounts(companion.CharacterObject, -1);
        lordParty.MemberRoster.AddToCounts(companion.CharacterObject, 1);
    }
    
    _missionCompanions.Clear();
    ModLogger.Info("Recon", "Companions returned to lord's party");
}
```

### Phase 4: Skill Bonus Application

```csharp
private float GetCompanionScoutingBonus()
{
    float bonus = 0f;
    foreach (var companion in _missionCompanions)
    {
        var scoutingSkill = companion.GetSkillValue(DefaultSkills.Scouting);
        if (scoutingSkill >= 200)
            bonus += 0.40f;
        else if (scoutingSkill >= 100)
            bonus += 0.20f;
    }
    return bonus;
}

private float GetCompanionEscapeBonus()
{
    float bonus = 0f;
    foreach (var companion in _missionCompanions)
    {
        var ridingSkill = companion.GetSkillValue(DefaultSkills.Riding);
        if (ridingSkill >= 200)
            bonus += 0.20f;
        else if (ridingSkill >= 100)
            bonus += 0.10f;
    }
    return bonus;
}
```

---

## Configuration

**Add to recon_config.json:**
```json
{
  "companions": {
    "enabled": true,
    "max_on_mission": 3,
    "selection_dialog": true,
    "skill_bonuses": {
      "scouting_100": 0.20,
      "scouting_200": 0.40,
      "riding_100": 0.10,
      "riding_200": 0.20,
      "tactics_100": 1,
      "tactics_200": 2
    },
    "keep_on_army_defeat": true,
    "death_chance_in_battle": 0.0
  }
}
```

---

## Acceptance Criteria

### Tier 1-3: Companions in Lord's Party
- [ ] Companions transferred to lord's party on enlistment
- [ ] Companions fight in lord's battles (native behavior)
- [ ] Companion casualties tracked in lord's roster
- [ ] Companion state persists through save/load

### Tier 4+: Companions in Player's Party
- [ ] On Tier 4 promotion, companions transferred TO player's party
- [ ] Player's party stays `IsVisible = false` (map safety)
- [ ] Player's party joins lord's MapEvent (battle participation)
- [ ] Companions spawn naturally via `PartyGroupTroopSupplier`
- [ ] Companion casualties tracked in player's roster (native)
- [ ] Retinue soldiers also in player's party

### Battle Formation Assignment (Tier 4+)
- [ ] Player assigned to formation based on duty
- [ ] All agents from player's party assigned to SAME formation
- [ ] Command suppression prevents player from commanding army
- [ ] Player can issue orders to own squad only
- [ ] Formation assignment works in field, siege, sally out battles
- [ ] Player teleported to formation position if spawned incorrectly

### Edge Cases
- [ ] Army defeated: companions in player's party stay with player
- [ ] Lord dies: companions already with player (Tier 4+)
- [ ] Player captured: companions captured with player (same party)
- [ ] Companion dies in battle: native death handling
- [ ] Companion wounded: native wound handling

### Retirement/Service End
- [ ] Companions already with player at Tier 4+
- [ ] Retinue dismissed on retirement
- [ ] Grace period preserves companion location

---

## Debugging

**Log Categories:** 
- `"Companions"` - Companion transfers and state
- `"FormationAssignment"` - Battle formation assignment

**Key Log Points:**
```csharp
// Companion transfers
ModLogger.Info("Companions", $"Transferred {count} companions to lord's party");
ModLogger.Info("Companions", $"Restored {count} companions to player");

// Formation assignment (battle)
ModLogger.Info("FormationAssignment", $"=== BEHAVIOR ACTIVE === Mission: {mode}, Enlisted: {enlisted}");
ModLogger.Info("FormationAssignment", $"Assigned enlisted player to {formationClass} formation");
ModLogger.Debug("FormationAssignment", $"Assigned companion {name} to player's formation");
ModLogger.Info("FormationAssignment", $"Command Suppression: Captaincy Stripped={stripped}, Role Stripped={role}");
ModLogger.Warn("FormationAssignment", $"Command Suppression Incomplete! Player is still: {roles}");
ModLogger.Info("FormationAssignment", $"Teleported player to formation's current position");
```

**Existing Debug File:**
`src/Features/Combat/Behaviors/EnlistedFormationAssignmentBehavior.cs`

**Debug Commands:**
```
companions.list        - List all player companions and their locations
companions.status      - Show companion locations and party membership
formation.status       - Show player's formation assignment and command state
```

---

## Future Considerations

### Companion Specialization Missions
- Medicine companions for rescue missions
- Scouting companions for recon missions
- Combat companions for assault missions
- Trade companions for supply missions

### Companion Relationships
- Companions may refuse dangerous missions (low relation)
- Successful missions improve companion loyalty
- Failed missions may cause companions to leave

### Companion Equipment
- Mission-specific equipment for companions
- Equipment affects mission bonuses
- Risk of losing equipment on defeat

### Companion Death
- Optional death-on-defeat for hardcore mode
- Funeral/memorial system
- Replacement companion recruitment

### Multi-Companion Interactions
- Companions with good relations work better together
- Rival companions may have conflicts
- Romance companions get bonuses together

