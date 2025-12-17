# Camp ("Camp")

## Quick Reference

| Feature | Tier | Description |
|---------|------|-------------|
| Service Records | 1+ | View faction-specific and lifetime service statistics |
| Pay & Pension | 1+ | View pending muster pay, next payday, last pay outcome, pension status |
| Discharge Actions | 1+ | Request/Cancel Pending Discharge (Final Muster at next pay muster) |
| Companion Assignments | 4+ | Toggle which companions fight in battles |
| Personal Retinue | 4+ | Command soldiers (5 at T4, 10 at T5, 20 at T6) |

## Table of Contents

- [Overview](#overview)
- [How It Works](#how-it-works)
  - [Service Records](#service-records)
  - [Personal Retinue](#personal-retinue)
  - [Companion Assignments](#companion-assignments)
- [Technical Details](#technical-details)
  - [System Architecture](#system-architecture)
  - [Data Storage](#data-storage)
  - [Key APIs](#key-apis)
- [Configuration](#configuration)
- [Edge Cases](#edge-cases)
- [Debugging](#debugging)

---

## Overview

The **Camp ("Camp")** is the player's personal hub within the enlisted army. It provides access to service records, pay/pension status, discharge actions, retinue management (Tier 4+), and companion assignments.

**Menu Structure:**
```
[Camp]
├── Service Records
│   ├── Current Posting (this enlistment)
│   ├── Faction Records (per-faction history)
│   └── Lifetime Summary (cross-faction totals)
├── Pay & Pension (this enlistment)
│   ├── Pending Muster Pay
│   ├── Next Payday
│   ├── Last Pay Outcome / Pay Muster Pending
│   └── Pension Amount / Pension Paused
├── Discharge
│   ├── Request Discharge (Final Muster)
│   └── Cancel Pending Discharge
├── Personal Retinue (Tier 4+ only)
│   ├── Current Muster
│   ├── Purchase Soldiers
│   ├── Replenish Losses
│   └── Dismiss Soldiers
└── Companion Assignments
    └── Battle Roster (Fight / Stay Back toggle)
```

---

## How It Works

### Service Records

Tracks military service history per faction and lifetime totals.

**Faction-Specific Records:**
- Each faction (kingdom, minor faction, mercenary) has separate records
- Tracks: terms completed, days served, highest rank, battles fought, lords served, enlistments
- Key format: `kingdom_{StringId}`, `minor_{StringId}`, `merc_{StringId}`

**Lifetime Statistics:**
- Total kills across all factions
- Total years served (cumulative)
- Total terms completed
- Total enlistments
- List of all factions served

**Display:**
- Current Posting: Shows current enlistment stats
- Faction Records: Per-faction history
- Lifetime Summary: Cross-faction totals

### Pay & Pension

Camp surfaces the pay-system state so players always know where they stand:
- **Pending Muster Pay**: The current ledger total waiting for pay muster.
- **Next Payday**: When the next pay muster should trigger (may defer if unsafe).
- **Last Pay Outcome**: What happened the last time pay muster resolved.
- **Pay Muster Pending**: Whether a pay muster is queued and waiting for a safe moment.
- **Pension Amount / Paused**: Current pension stipend (if any) and whether it is paused (e.g. during enlistment).

### Discharge Actions (Final Muster)

Discharge is handled as **Pending Discharge → Final Muster**:
- **Request Discharge (Final Muster)** sets pending discharge.
- **Cancel Pending Discharge** clears it.
- Final Muster resolves at the **next pay muster** and is where severance, pension, and gear handling occur.

### Personal Retinue

At Tier 4+, players can command soldiers that fight alongside them in battle.

**Tier-Based Capacity:**
| Tier | Unit Name | Max Soldiers |
|------|-----------|--------------|
| 4 | Lance | 5 |
| 5 | Squad | 10 |
| 6 | Retinue | 20 |

**Soldier Types:**
- **Infantry**: Foot soldiers with sword and shield
- **Archers**: Foot ranged (bows/crossbows)
- **Cavalry**: Mounted melee
- **Horse Archers**: Mounted ranged (not available for all factions)

**Faction Availability:**
- Horse Archers unavailable for: Vlandia, Battania, Sturgia
- Horse Archers available for: Empire, Khuzait, Aserai
- UI grays out unavailable types with tooltip

**Troop Quality:**
- Tier 4-5: Mix of Tier 2-3 troops (40%/60% distribution)
- Tier 6: Mix of Tier 3-4 troops (60%/40% distribution)
- All soldiers are of ONE chosen type (no mixing)

**Gold Costs:**
- **Purchase**: Uses native `GetTroopRecruitmentCost()` formula (~100-400 gold per soldier)
- **Daily Upkeep**: 2 gold per soldier per day
- **Desertion**: One soldier deserts per unpaid day

**Replenishment Systems:**

1. **Trickle System** (Free, Slow):
   - 1 soldier every 2-3 days (randomized)
   - Automatic, no cost
   - Respects tier capacity and party size limits
   - Waits for wounded soldiers to heal or die before filling slots (daily sync)

2. **Instant Requisition** (Costly, Fast):
   - Instant fill of all missing soldiers
   - Cost: `GetTroopRecruitmentCost() × missing soldiers`
   - 14-day cooldown between requisitions

**Party Size Safeguards:**
- Checks `PartyBase.MainParty.PartySizeLimit` before adding soldiers
- Only adds up to available party space
- UI shows party limit vs. tier capacity
- Warns when party limit restricts retinue

**Battle Integration:**
- Retinue soldiers stored in `MobileParty.MainParty.MemberRoster`
- Spawn naturally via native `PartyGroupTroopSupplier`
- Fight in same formation as player (unified squad)
- Casualties tracked by native roster system
- See [Companion Management](../Core/companion-management.md) for formation details

**Retinue Lifecycle:**
| Event | Retinue Behavior |
|-------|------------------|
| On Leave | Stays in player's invisible party |
| Player Captured | **Cleared** - soldiers scattered |
| Enlistment Ends | **Dismissed** - return to army ranks |
| Lord Dies / Army Defeated | **Lost** - scattered in chaos |
| Battle Casualties | Native tracking updates roster |

### Companion Assignments

Manage which companions fight in battles (Tier 4+).

**Settings:**
- **Fight** (default): Companion spawns in battle, faces all risks
- **Stay Back**: Companion doesn't spawn, immune to battle outcomes

**Why "Stay Back" Is Safe:**
- Native battle resolution only processes spawned agents
- "Stay back" companions survive army destruction, player capture, all battle outcomes

**Command Restrictions:**
- Companions **cannot** become formation captains or team generals during enlistment
- Prevents companions from giving tactical orders or appearing as commanders
- Applies to all companions regardless of "Fight" or "Stay Back" setting

**UI:**
- Camp → Companion Assignments
- Shows list of companions with toggle (⚔️ Fight / 🏕️ Stay Back)
- Changes saved immediately

See [Companion Management](../Core/companion-management.md) for full details.

---

## Technical Details

### System Architecture

**File Structure:**
```
src/Features/CommandTent/
├── Core/
│   ├── ServiceRecordManager.cs      # Faction record tracking
│   ├── RetinueManager.cs            # Soldier management
│   ├── RetinueLifecycleHandler.cs   # Event handling (capture, defeat, etc.)
│   └── CompanionAssignmentManager.cs # Companion participation state
├── Data/
│   ├── FactionServiceRecord.cs      # Per-faction data
│   ├── LifetimeServiceRecord.cs     # Cross-faction totals
│   └── RetinueState.cs              # Current retinue state
├── Systems/
│   ├── RetinueTrickleSystem.cs     # Free, slow replenishment
│   ├── RetinueCasualtyTracker.cs   # Battle casualties + daily wounded sync
│   └── ServiceStatisticsSystem.cs   # Kill/battle tracking
└── UI/
    └── CommandTentMenuHandler.cs    # Menu integration
```

**Design Principles:**
- Single Responsibility: Each class handles one aspect
- Event-Driven: Hooks into campaign events
- State Isolation: `RetinueState` is single source of truth
- Graceful Cleanup: `ClearRetinueTroops(reason)` handles all scenarios

### Data Storage

**Persisted Data (SyncData):**

```csharp
// Service Records
private Dictionary<string, FactionServiceRecord> _factionRecords;
private int _lifetimeKills;
private int _lifetimeTermsCompleted;
private int _lifetimeDaysServed;
private List<string> _factionsServed;

// Retinue State
private string _retinueSelectedTypeId;  // "infantry", "archers", etc.
private Dictionary<string, int> _retinueTroopCounts;  // CharacterObject.StringId → count
private int _daysSinceLastTrickle;
private CampaignTime _requisitionCooldownEnd;

// Companion Assignments
private Dictionary<string, bool> _companionBattleParticipation;  // Hero.StringId → fight/stay back
```

**RetinueState Class:**
```csharp
public class RetinueState
{
    public string SelectedTypeId { get; set; }
    public Dictionary<string, int> TroopCounts { get; set; }
    public int DaysSinceLastTrickle { get; set; }
    public CampaignTime RequisitionCooldownEnd { get; set; }
    public int TotalSoldiers => TroopCounts?.Values.Sum() ?? 0;
    public bool HasRetinue => !string.IsNullOrEmpty(SelectedTypeId) && TotalSoldiers > 0;
}
```

### Key APIs

**Faction Detection:**
```csharp
faction.IsMinorFaction     // Minor faction
faction.IsBanditFaction    // Bandit clan
clan.IsClanTypeMercenary   // Mercenary company
faction.IsKingdomFaction   // Major kingdom
```

**Troop Management:**
```csharp
// Add soldiers to player's party
MobileParty.MainParty.MemberRoster.AddToCounts(troopType, count);

// Check party size
int availableSpace = PartyBase.MainParty.PartySizeLimit - PartyBase.MainParty.NumberOfAllMembers;

// Get recruitment cost
Campaign.Current.Models.PartyWageModel.GetTroopRecruitmentCost(troop, Hero.MainHero);

// Deduct gold (use GiveGoldAction to update party treasury visible in UI)
GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, amount);
```

**Formation Control (Battle):**
```csharp
// Team role control
Team.SetPlayerRole(isPlayerGeneral: false, isPlayerSergeant: true);

// Formation ownership
Formation.PlayerOwner = playerAgent;

// Agent formation assignment
agent.Formation = targetFormation;

// Check if agent is from player's party
var origin = agent.Origin as PartyGroupAgentOrigin;
bool isPlayerPartyTroop = origin?.Party == PartyBase.MainParty;
```

**Troop Selection:**
```csharp
// Get faction-appropriate troops by tier
CharacterHelper.GetTroopTree(baseTroop, minTier: 3f, maxTier: 4f);

// Get formation class
character.GetFormationClass();  // Returns FormationClass enum
```

---

## Configuration

**JSON Config:** `ModuleData/Enlisted/command_tent_config.json`

**Key Settings:**
- `capacity_by_tier`: Max soldiers per tier (4: 5, 5: 10, 6: 20)
- `soldier_types`: Type definitions (infantry, archers, cavalry, horse_archers)
- `faction_overrides`: Faction-specific availability and names
- `replenishment`: Trickle and requisition settings
- `economics`: Daily upkeep and desertion settings

**C# Constants:** `src/Features/CommandTent/Core/CommandTentConfig.cs`

**Key Values:**
- `LanceTier = 4`, `SquadTier = 5`, `RetinueTier = 6`
- `DailyUpkeepPerSoldier = 2`
- `TrickleMinDays = 2`, `TrickleMaxDays = 3`
- `RequisitionCooldownDays = 14`

---

## Edge Cases

### Service Records

**Faction Change Mid-Term:**
- Current faction record updated
- New faction record begins fresh
- Lifetime totals continue accumulating

**Minor Faction Without Kingdom:**
- Uses clan StringId as key
- Marked as "minor" type
- Full tracking still applies

### Retinue System

**Zero Soldiers After Casualties:**
- Retinue type preserved
- Player can replenish via trickle or requisition
- No need to re-select type

**Cannot Afford Purchase:**
- UI grays out unaffordable options
- Shows cost next to each option

**Party Full:**
- Retinue purchase UI grayed out
- Message: "Party is full. Dismiss troops or increase party size."
- Trickle skips silently

**Party Partially Full:**
- Purchase/requisition only fills to available space
- UI shows warning: "Party limit restricts retinue size"

**Companions Fill Party:**
- Retinue feature effectively disabled
- UI message: "Your companions fill your party. No room for retinue soldiers."

**Change Soldier Type:**
- Current soldiers must be dismissed first
- Confirmation dialog required
- Dismissed soldiers do NOT return to player

**Naval Battles:**
- Formation assignment skipped (naval DLC handles positioning)
- Cavalry/horse archers fight dismounted
- UI warning: "* Mounted troops fight on foot in naval battles."
- Native reserves system handles ship capacity overflow

### Retinue Lifecycle

**On Leave:**
- Retinue stays in player's invisible party
- No special handling needed

**Player Captured:**
- All retinue cleared from roster
- Message: "Your retinue has scattered. Your soldiers fled when you were captured."

**Enlistment Ends:**
- All retinue dismissed
- Message: "Your soldiers have returned to the army ranks."

**Army Defeated / Lord Dies:**
- All retinue lost
- Message: "In the chaos of defeat, your retinue has scattered."

**Battle Casualties:**
- Pre-battle snapshot of `_retinueState.TroopCounts`
- Post-battle: Compare roster counts to snapshot
- Native `OnTroopKilled()` updates roster automatically
- We sync tracking state to match roster

**Wounded Soldiers:**
- Wounded troops count towards retinue capacity while healing
- Daily sync catches soldiers who die from wounds between battles
- Once wounded soldiers heal or die, trickle can replenish the gap

---

## Debugging

**Log Categories:**
- `"CommandTent"` - General system activity
- `"Retinue"` - Soldier management
- `"Upkeep"` - Daily gold deduction
- `"Trickle"` - Free replenishment
- `"Requisition"` - Instant replenishment
- `"CasualtyTracker"` - Battle casualties and wounded sync

**Key Log Points:**
```csharp
// Service records
ModLogger.Debug("CommandTent", $"Service record updated for {factionId}");

// Retinue management
ModLogger.Info("Retinue", $"Spawning {count} {type} soldiers");
ModLogger.Debug("Retinue", $"Trickle skipped: already at capacity");
ModLogger.Info("Retinue", $"Battle started with {GetRetinueCount()} soldiers");
ModLogger.Debug("Retinue", $"Lost {lost}× {character.Name} in battle");

// Upkeep
ModLogger.Debug("Upkeep", $"Deducted {cost} gold for {soldierCount} soldiers");
ModLogger.Warn("Upkeep", $"Player cannot afford upkeep, desertion triggered");

// Lifecycle
ModLogger.Info("Retinue", $"Cleared {count} retinue troops (reason: {reason})");

// Casualty tracking
ModLogger.Info("CasualtyTracker", $"Battle casualties: {lost} soldiers lost");
ModLogger.Info("CasualtyTracker", $"Wounded casualties: {lost} soldiers succumbed to wounds");
```

**Debug Output Location:**
- `Modules/Enlisted/Debugging/enlisted.log`

**Related Files:**
- `src/Features/CommandTent/Core/RetinueManager.cs`
- `src/Features/CommandTent/Core/ServiceRecordManager.cs`
- `src/Features/CommandTent/Systems/RetinueCasualtyTracker.cs`
- `src/Features/CommandTent/UI/CommandTentMenuHandler.cs`

---

## Related Documentation

- [Companion Management](../Core/companion-management.md) - Companion battle participation details
- [Enlistment System](../Core/enlistment.md) - Tier progression and service state
- [Menu Interface](menu-interface.md) - Parent menu system
- [Pay System](../Core/pay-system.md) - Muster ledger, pay muster, discharge, pensions
