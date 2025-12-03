# Feature Spec: Command Tent System

## Table of Contents

- [Overview](#overview)
- [Purpose](#purpose)
- [Tier Requirements](#tier-requirements)
- [Menu Structure](#menu-structure)
- [Service Records System](#service-records-system)
  - [Faction-Specific Records](#faction-specific-records)
  - [Cross-Faction Statistics](#cross-faction-statistics)
  - [Minor Faction Support](#minor-faction-support)
  - [Display Format](#display-format)
- [Personal Retinue System](#personal-retinue-system)
  - [Tier-Based Unlocks](#tier-based-unlocks)
  - [Soldier Types](#soldier-types)
  - [Faction Availability](#faction-availability)
  - [Troop Quality & Tier Distribution](#troop-quality--tier-distribution)
  - [Gold Costs](#gold-costs)
  - [Replenishment Systems](#replenishment-systems)
  - [Party Size Safeguards](#party-size-safeguards)
  - [Battle Integration](#battle-integration)
  - [Retinue Lifecycle & State Management](#retinue-lifecycle--state-management)
  - [Leadership Notification](#leadership-notification)
- [Companion Management](#companion-management)
- [Technical Implementation](#technical-implementation)
  - [Modular Architecture](#modular-architecture)
  - [Data Storage](#data-storage)
  - [Debugging System](#debugging-system)
  - [Key APIs](#key-apis)
- [Implementation Phases](#implementation-phases)
- [Configuration](#configuration)
- [Edge Cases](#edge-cases)
- [Acceptance Criteria](#acceptance-criteria)
- [Future Considerations](#future-considerations)
- [Localization (XML Strings)](#localization-xml-strings)

---

## Overview

The **Command Tent** is the player's personal command space within the enlisted army. It serves as the central hub for reviewing faction-specific service history, managing a personal retinue of soldiers (at Tier 4+), and coordinating companion assignments.

---

## Purpose

1. **Service Tracking**: Maintain per-faction military records including battles, kills, terms served
2. **Leadership Reward**: Give Tier 4+ players command of soldiers as a tangible combat benefit
3. **Immersion**: Period-appropriate interface for military administrative tasks
4. **Progression**: Clear advancement path from individual soldier → commanding 5 → 10 → 20 men
5. **Gold Sink**: Create meaningful economic decisions around purchasing and maintaining troops

---

## Tier Requirements

| Feature | Minimum Tier | Notes |
|---------|--------------|-------|
| Access Command Tent | 1 | Basic menu access |
| View Service Records | 1 | All statistics visible |
| Manage Companions | 1 | Battle participation toggles |
| Request Lance (5 soldiers) | 4 | First leadership unlock |
| Request Squad (10 soldiers) | 5 | Expanded command |
| Request Retinue (20 soldiers) | 6 | Full personal company |

---

## Menu Structure

```
[Command Tent]
├── Service Records
│   ├── Current Posting (this enlistment)
│   ├── Faction Records (per-faction history)
│   └── Lifetime Summary (cross-faction totals)
├── Personal Retinue (Tier 4+ only)
│   ├── Current Muster (view current soldiers)
│   ├── Purchase Soldiers (buy new troops)
│   ├── Replenish Losses (replace casualties)
│   └── Dismiss Soldiers
└── Companion Assignments
    ├── Battle Roster
    └── Mission Roster (Future)
```

---

## Service Records System

### Faction-Specific Records

Each faction (kingdom, minor faction, mercenary company) maintains **separate** service records.

**Per-Faction Statistics:**
| Statistic | Description | Persists |
|-----------|-------------|----------|
| Terms Completed | Number of full enlistment terms served | Yes |
| Days Served | Total days in this faction's service | Yes |
| Highest Rank | Peak tier achieved with this faction | Yes |
| Battles Fought | Battles participated in for this faction | Yes |
| Lords Served | Number of different lords served | Yes |
| Enlistments | Times enlisted with this faction | Yes |

**Faction Record Key**: Uses `Kingdom.StringId`, `Clan.StringId` (for minor factions), or special key for mercenary companies.

### Cross-Faction Statistics

Some statistics are tracked globally across all factions:

| Statistic | Description | Scope |
|-----------|-------------|-------|
| Total Kills | All enemies slain while enlisted | Lifetime |
| Total Years Served | Cumulative service time (all factions) | Lifetime |
| Total Terms Completed | All enlistment terms finished | Lifetime |
| Total Enlistments | Times enlisted (any faction) | Lifetime |
| Factions Served | List of all factions served | Lifetime |

### Minor Faction Support

Minor factions are handled distinctly:

- **Minor Kingdoms**: Clans with `IsMinorFaction = true`
- **Mercenary Companies**: Clans with `IsClanTypeMercenary = true`
- **Bandit Factions**: Clans with `IsBanditFaction = true` (if supported)

**Detection Logic:**
```csharp
// Determine faction type for record keeping
string GetFactionRecordKey(IFaction faction)
{
    if (faction is Kingdom kingdom)
        return $"kingdom_{kingdom.StringId}";
    
    if (faction is Clan clan)
    {
        if (clan.IsMinorFaction)
            return $"minor_{clan.StringId}";
        if (clan.IsClanTypeMercenary)
            return $"merc_{clan.StringId}";
        return $"clan_{clan.StringId}";
    }
    
    return $"unknown_{faction.StringId}";
}
```

### Display Format

**Current Posting:**
```
═══════════════════════════════════════════
         CURRENT SERVICE RECORD
═══════════════════════════════════════════

  Posting: Army of King Derthert
  Lord: Count Aldric of Charas
  Rank: Sergeant (Tier 4)
  
  Days Served: 47
  Contract Remaining: 13 days
  
───────────────────────────────────────────
            THIS TERM
───────────────────────────────────────────
  
  Battles: 12 (9 victories)
  Enemies Slain: 67
  
═══════════════════════════════════════════
```

**Faction Record (Example - Vlandia):**
```
═══════════════════════════════════════════
       SERVICE RECORD: VLANDIA
═══════════════════════════════════════════

  Total Enlistments: 3
  Terms Completed: 2
  Days Served: 523
  Highest Rank: Veteran (Tier 5)
  
  Battles Fought: 45
  Lords Served: 4
  
═══════════════════════════════════════════
```

**Lifetime Summary:**
```
═══════════════════════════════════════════
         LIFETIME SERVICE SUMMARY
═══════════════════════════════════════════

  Years in Service: 4 years, 2 months
  Total Enlistments: 7
  Terms Completed: 5
  
  Factions Served:
    • Kingdom of Vlandia (3 terms)
    • Aserai Sultanate (1 term)
    • Company of the Golden Boar (1 term)
  
  Total Enemies Slain: 312
  
═══════════════════════════════════════════
```

---

## Personal Retinue System

At Tier 4, the player has proven themselves capable of leading men in battle. They receive a **leadership notification** and can request soldiers to follow them.

### Tier-Based Unlocks

| Tier | Unit Name | Max Soldiers | Historical Flavor |
|------|-----------|--------------|-------------------|
| 4 | Lance | 5 | "A lance of five soldiers" |
| 5 | Squad | 10 | "A squad of ten soldiers" |
| 6 | Retinue | 20 | "A retinue of twenty soldiers" |

**Note**: Companions do NOT count toward these limits. A Tier 6 player can have 20 soldiers + companions.

### Soldier Types

The player may choose **ONE type** of soldier at a time. Changing types requires dismissing current soldiers. **All soldiers will be of the chosen type** - no mixing.

| Type | Description | FormationClass | Menu Text |
|------|-------------|----------------|-----------|
| **Infantry** | Foot soldiers, shields, melee | `FormationClass.Infantry` | "Men-at-Arms" |
| **Archers** | Foot ranged, bows/crossbows | `FormationClass.Ranged` | "Bowmen" |
| **Cavalry** | Mounted melee | `FormationClass.Cavalry` | "Mounted Lancers" |
| **Horse Archers** | Mounted ranged | `FormationClass.HorseArcher` | "Mounted Bowmen" |

### Faction Availability

Not all factions have all troop types. **Horse Archers are unavailable** for some factions:

| Faction | Infantry | Archers | Cavalry | Horse Archers |
|---------|:--------:|:-------:|:-------:|:-------------:|
| **Empire** | ✓ | ✓ | ✓ | ✓ |
| **Vlandia** | ✓ | ✓ (Crossbows) | ✓ | ❌ |
| **Battania** | ✓ | ✓ (Fians) | ✓ | ❌ |
| **Sturgia** | ✓ | ✓ | ✓ | ❌ |
| **Khuzait** | ✓ | ✓ | ✓ | ✓ |
| **Aserai** | ✓ | ✓ | ✓ | ✓ |

**UI Behavior**: Unavailable types are **grayed out** with tooltip: "{FACTION_NAME} does not field mounted archers."

### Troop Quality & Tier Distribution

Soldiers are **faction-appropriate troops** with quality based on player tier. Within each type, soldiers are a **mix of tiers** (not all the same quality).

#### Troop Tiers by Player Tier

| Player Tier | Troop Tiers | Reasoning |
|-------------|-------------|-----------|
| 4 (Lance) | Tier 2-3 | Junior leader, commands basic soldiers |
| 5 (Squad) | Tier 2-3 | Same quality, larger quantity |
| 6 (Retinue) | Tier 3-4 | Veteran leader, elite soldiers |

#### Tier Distribution (Quality Mix)

When player requests soldiers, they get a **mix of tiers** within their type:

| Type | Tier Distribution | Example (5 soldiers) |
|------|-------------------|----------------------|
| Infantry | 40% T2, 60% T3 | 2 Footmen + 3 Veterans |
| Archers | 40% T2, 60% T3 | 2 Archers + 3 Veteran Archers |
| Cavalry | 100% T3 | 5 Cavalry (no T2 cavalry typically) |
| Horse Archers | 30% T2, 70% T3 | 1 Nomad + 4 Horse Archers |

**Note**: Distribution is configurable per faction and player tier in JSON config.

#### Faction-Specific Quality Overrides

Some factions have exceptional troops that skew the distribution:

| Faction | Type | Override | Reason |
|---------|------|----------|--------|
| Battania | Archers | 80% T3, 20% T4 | Fians are elite |
| Khuzait | Horse Archers | 40% T2, 60% T3 | Abundant horsemen |
| Vlandia | Cavalry | 20% T2, 80% T3 | Strong cavalry tradition |

#### Technical Implementation

```csharp
// Get troops filtered by tier and formation class
public List<CharacterObject> GetRetinueTroops(
    CultureObject culture, 
    FormationClass formationClass,
    int playerTier)
{
    // Get tier range from config
    var tierConfig = Config.GetTierDistribution(playerTier, formationClass);
    
    // Get all troops from faction's tree
    var allTroops = CharacterHelper.GetTroopTree(culture.BasicTroop);
    
    // Filter by formation class and tier
    return allTroops
        .Where(t => t.GetFormationClass() == formationClass)
        .Where(t => tierConfig.ContainsKey(t.Tier))
        .ToList();
}

// Generate roster with tier distribution
public TroopRoster GenerateRetinue(int count, string typeId, CultureObject culture, int playerTier)
{
    var roster = TroopRoster.CreateDummyTroopRoster();
    var distribution = Config.GetTierDistribution(playerTier, typeId, culture);
    
    foreach (var (tier, percent) in distribution)
    {
        int tierCount = (int)Math.Round(count * (percent / 100.0));
        var troops = GetTroopsForTier(culture, typeId, tier);
        
        for (int i = 0; i < tierCount; i++)
        {
            roster.AddToCounts(troops.GetRandomElement(), 1);
        }
    }
    
    return roster;
}
```

### Gold Costs

**Purchase Cost:**
- Uses native recruitment cost formula: `PartyWageModel.GetTroopRecruitmentCost(troop, buyerHero)`
- Tier 3 troops: ~100-200 gold
- Tier 4 troops: ~200-400 gold
- Mounted troops cost more (horse included)

**Daily Upkeep:**
- **2 gold per soldier per day** (flat rate, regardless of type)
- Charged automatically from player gold
- If player cannot pay, soldiers desert (one per unpaid day)

### Replenishment Systems

The player has two ways to replenish lost soldiers, both with **overfill protection** to ensure the player never exceeds their tier-based capacity.

#### Trickle System (Free, Slow)

Soldiers are **automatically assigned** to the player's retinue over time at no cost.

| Parameter | Value | Notes |
|-----------|-------|-------|
| Rate | 1 soldier per 2-3 days | Configurable |
| Cost | Free | No gold required |
| Trigger | Daily tick | Automatic, background |
| Overfill Protection | Yes | Only adds if below capacity |

**How it works:**
```csharp
// Daily tick handler
private void OnDailyTick()
{
    if (!CanTrickleReplenish()) return;
    
    // Random 2-3 day interval
    if (_daysSinceLastTrickle < TrickleIntervalDays) 
    {
        _daysSinceLastTrickle++;
        return;
    }
    
    _daysSinceLastTrickle = 0;
    int currentCount = GetCurrentSoldierCount();
    int maxCapacity = GetTierCapacity(_currentTier);
    
    if (currentCount >= maxCapacity) 
    {
        ModLogger.Debug("Retinue", "Trickle skipped: already at capacity");
        return;
    }
    
    AddSoldier(1);
    ModLogger.Info("Retinue", $"Trickle added 1 soldier ({currentCount + 1}/{maxCapacity})");
    
    // Notify player
    var msg = new TextObject("{=retinue_trickle}A new soldier has reported for duty.");
    MBInformationManager.AddQuickInformation(msg);
}
```

**Trickle Conditions:**
- Player is enlisted at Tier 4+
- Player has an active retinue type selected
- Current soldier count < tier capacity
- Trickle interval has elapsed

#### Instant Requisition (Costly, Fast)

The player can "bribe" or "requisition" to **instantly fill** all missing soldier slots.

| Parameter | Value | Notes |
|-----------|-------|-------|
| Speed | Instant | Immediate fill |
| Cost | Native recruitment cost × missing soldiers | Uses `GetTroopRecruitmentCost()` |
| Cooldown | 14 days | Cannot spam requisition |
| Overfill Protection | Yes | Only fills to capacity |

**Cost Calculation:**
```csharp
// Calculate total requisition cost
public int CalculateRequisitionCost()
{
    int currentCount = GetCurrentSoldierCount();
    int maxCapacity = GetTierCapacity(_currentTier);
    int missing = maxCapacity - currentCount;
    
    if (missing <= 0) return 0;
    
    // Get the troop type cost
    var troop = GetSelectedTroopType();
    int perSoldierCost = Campaign.Current.Models.PartyWageModel
        .GetTroopRecruitmentCost(troop, Hero.MainHero);
    
    return perSoldierCost * missing;
}

// Execute requisition
public bool TryRequisition()
{
    if (!CanRequisition(out string reason))
    {
        ModLogger.Warn("Retinue", $"Requisition blocked: {reason}");
        return false;
    }
    
    int cost = CalculateRequisitionCost();
    int toAdd = GetMissingSoldierCount();
    
    // Deduct gold
    Hero.MainHero.ChangeHeroGold(-cost);
    
    // Add soldiers
    AddSoldiers(toAdd);
    
    // Start cooldown
    _requisitionCooldownEnd = CampaignTime.Now + CampaignTime.Days(RequisitionCooldownDays);
    
    ModLogger.ActionResult("Retinue", "Requisition", true, 
        $"Added {toAdd} soldiers for {cost} gold");
    
    return true;
}
```

**Requisition Conditions:**
- Player is enlisted at Tier 4+
- Player has an active retinue type selected
- Cooldown has elapsed (14 days since last requisition)
- Player has enough gold
- Current soldier count < tier capacity

**UI Display:**
```
[Instant Requisition]
Fill missing soldiers immediately.

Missing: 8 soldiers
Cost: 1,600 gold (200 gold each)
Your Gold: 5,230

Cooldown: Available NOW
    - or -
Cooldown: 6 days remaining
```

#### Overfill Protection

Both systems enforce the same capacity logic:

```csharp
// Capacity check used by both trickle and requisition
private int GetSafeAddCount(int requested)
{
    int current = GetCurrentSoldierCount();
    int maxCapacity = GetTierCapacity(_currentTier);
    int available = maxCapacity - current;
    
    // Never exceed capacity
    int toAdd = Math.Min(requested, available);
    
    if (toAdd < requested)
    {
        ModLogger.Debug("Retinue", 
            $"Overfill protection: requested {requested}, adding {toAdd} (capacity: {maxCapacity})");
    }
    
    return toAdd;
}
```

### Party Size Safeguards

The native game enforces **party size limits** based on clan tier, Leadership skill, and perks. We must ensure the player's party can accommodate retinue soldiers.

#### Native Party Size Formula

```
Base: 20
+ Clan Tier Bonus: 15 per tier (25 if clan leader)
+ Steward Skill: varies
+ Leadership Perks: varies
```

**Typical party sizes by clan tier:**

| Clan Tier | Non-Leader Bonus | Estimated Total |
|-----------|------------------|-----------------|
| 1 | +15 | ~35-45 |
| 2 | +30 | ~50-60 |
| 3 | +45 | ~65-75 |
| 4+ | +60+ | ~80+ |

#### What We Need (Max Case)

At Tier 6 with full retinue:
- Player character: 1
- Companions: ~3-5
- Retinue soldiers: 20
- **Total: ~25-26 slots needed**

Even a Clan Tier 1 player (~35 party limit) has enough room.

#### Safeguard Implementation

Before adding soldiers, check available party space:

```csharp
// Check party size before adding soldiers
public bool TryAddSoldiers(int count, out int actuallyAdded, out string message)
{
    var party = PartyBase.MainParty;
    int partyLimit = party.PartySizeLimit;
    int currentMembers = party.NumberOfAllMembers;
    int availableSpace = partyLimit - currentMembers;
    
    // Apply tier capacity limit first
    int tierCapacity = GetTierCapacity(_currentTier);
    int currentSoldiers = GetCurrentSoldierCount();
    int tierAvailable = tierCapacity - currentSoldiers;
    
    // Take the more restrictive limit
    int maxCanAdd = Math.Min(availableSpace, tierAvailable);
    actuallyAdded = Math.Min(count, maxCanAdd);
    
    if (actuallyAdded <= 0)
    {
        if (availableSpace <= 0)
            message = "Party is full. Dismiss troops or increase party size.";
        else if (tierAvailable <= 0)
            message = "Retinue is at full capacity for your rank.";
        else
            message = "Cannot add soldiers.";
        
        ModLogger.Warn("Retinue", $"Add blocked: {message}");
        return false;
    }
    
    if (actuallyAdded < count)
    {
        message = $"Party limit reached. Only {actuallyAdded} soldiers assigned.";
        ModLogger.Info("Retinue", message);
    }
    else
    {
        message = $"{actuallyAdded} soldiers assigned to your retinue.";
    }
    
    // Actually add to roster
    var troopType = GetSelectedTroopType();
    MobileParty.MainParty.MemberRoster.AddToCounts(troopType, actuallyAdded);
    
    return true;
}
```

#### UI Feedback

When party size limits the retinue:

```
[Request Soldiers]
Your party size limit prevents a full retinue.

Party Limit: 42
Current Members: 35
Available Space: 7

Tier Capacity: 10 soldiers
Can Request: 7 soldiers (party limited)

[Request 7 Soldiers] [Cancel]
```

#### Edge Case: Companions Fill Party

If companions + player fill the party before any retinue can be added:

```csharp
// In RetinueManager
public bool CanHaveRetinue(out string reason)
{
    if (_currentTier < CommandTentConfig.LanceTier)
    {
        reason = "Requires Tier 4 or higher.";
        return false;
    }
    
    var party = PartyBase.MainParty;
    int availableSpace = party.PartySizeLimit - party.NumberOfAllMembers;
    
    if (availableSpace <= 0)
    {
        reason = "No party space available. Your companions fill your party.";
        return false;
    }
    
    reason = null;
    return true;
}
```

### Battle Integration - Unified Squad Formation

**Design Principle**: Player, companions, and retinue soldiers all fight together as ONE unified squad. The player does NOT have control over army order of battle or other formations.

#### Player Party Approach (Tier 4+)

At Tier 4, companions and retinue soldiers are kept in the **player's party** instead of the lord's party. This is the cleanest approach because:

1. **Native casualty tracking** - Deaths update player's roster automatically
2. **Native "under player command"** - `IsPartyUnderPlayerCommand(MainParty)` returns true
3. **No manual spawn/despawn** - Troops spawn naturally from party roster
4. **Invisible party safety** - Player party stays `IsVisible = false`, can't be attacked on map

#### How It Works

1. **Campaign Map State**:
   - Player party: `IsActive = true` (for battles), `IsVisible = false` (hidden)
   - Companions and retinue soldiers are in player's `MemberRoster`
   - Player party escorts lord (existing behavior)
   - Party cannot be attacked separately (invisible)

2. **When Battle Starts**:
   - Player party automatically joins lord's MapEvent
   - Troops from player's roster supplied via native `PartyGroupTroopSupplier`
   - All troops placed in SAME formation (unified squad)

3. **Formation Assignment**:
   - Player, companions, AND retinue all in same formation
   - Formation type based on player's duty (Infantry/Archer/Cavalry/HorseArcher)
   - Player can command their squad only

4. **Casualties**:
   - Native `MapEventSide.OnTroopKilled()` handles deaths
   - Player's roster automatically updated
   - Dead retinue soldiers removed from count
   - Dead companions handled by native companion death system

5. **Command Authority**: 
   - Player can command their own squad (move, charge, hold)
   - Player CANNOT command other formations or army
   - Player does NOT see order of battle screen

#### Tier-Based Party Management

| Tier | Companions | Retinue | Location |
|------|------------|---------|----------|
| 1-3 | In lord's party | N/A | Lord's roster |
| 4+ | In player's party | In player's party | Player's roster |

**On reaching Tier 4:**
```csharp
// Reverse of TransferPlayerTroopsToLord() - move companions TO player
private void TransferCompanionsToPlayer()
{
    var main = MobileParty.MainParty;
    var lordParty = _enlistedLord?.PartyBelongedTo;
    
    // Find player companions in lord's party and move them back
    foreach (var troop in lordParty.MemberRoster.GetTroopRoster())
    {
        if (troop.Character.IsHero && 
            troop.Character.HeroObject?.IsPlayerCompanion == true)
        {
            lordParty.MemberRoster.AddToCounts(troop.Character, -1);
            main.MemberRoster.AddToCounts(troop.Character, 1);
        }
    }
}
```

#### Command Suppression

The mod suppresses normal player command authority:

| What | Suppressed? | Notes |
|------|-------------|-------|
| Order of Battle screen | Yes | Player is not general |
| Army-wide orders | Yes | Player is not general |
| Other formation commands | Yes | Player only controls own squad |
| Own squad movement | **No** | Player can move their squad |
| Own squad charge/hold | **No** | Player can issue squad orders |

**Technical:**
```csharp
// Player is "Sergeant" - can command own formation only
Team.SetPlayerRole(isPlayerGeneral: false, isPlayerSergeant: true);

// Player owns their formation (allows issuing orders to squad)
playerFormation.PlayerOwner = playerAgent;

// Order controller transferred to lord (removes army-wide command UI)
Team.PlayerOrderController.Owner = lordAgent;
```

#### Formation Assignment Flow

```
Battle Starts
    ↓
Player party troops spawn via native PartyGroupTroopSupplier
(companions + retinue already in player's MemberRoster)
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
    ↓
Teleport player to formation position if needed
```

#### Formation Assignment Implementation

**Method**: Enhance `EnlistedFormationAssignmentBehavior` to assign all player party troops

```csharp
// Find and assign all troops from player's party to same formation
private void AssignPlayerPartyToFormation(Agent playerAgent, Formation targetFormation)
{
    foreach (var agent in playerAgent.Team.ActiveAgents)
    {
        // Check if this agent is from player's party
        var origin = agent.Origin as PartyGroupAgentOrigin;
        if (origin?.Party == PartyBase.MainParty)
        {
            agent.Formation = targetFormation;
            ModLogger.Debug("FormationAssignment", 
                $"Assigned {agent.Character?.Name} (player party) to squad");
        }
    }
    
    // Set player as formation owner for command UI
    targetFormation.PlayerOwner = playerAgent;
}
```

#### Why This Approach?

1. **Native Casualty Tracking**: Deaths automatically update player's roster
2. **Native Command Recognition**: `IsPartyUnderPlayerCommand(MainParty)` = true
3. **No Custom Spawning**: Troops spawn naturally from roster
4. **Map Safety**: Invisible party can't be attacked
5. **Unified Squad**: All player party troops fight together
6. **Historical Accuracy**: NCO commanding their immediate men

### Retinue Lifecycle & State Management

The retinue system tracks soldiers through various game states. This section defines behavior for all scenarios.

#### State Tracking

```csharp
public class RetinueState
{
    // What type the player chose ("infantry", "archers", etc.)
    public string SelectedTypeId { get; set; }
    
    // Tracked troops: CharacterObject.StringId → count
    // Allows us to distinguish retinue from other troops
    public Dictionary<string, int> TroopCounts { get; set; }
    
    // Replenishment tracking
    public int DaysSinceLastTrickle { get; set; }
    public CampaignTime RequisitionCooldownEnd { get; set; }
}
```

#### Distinguishing Party Members

```csharp
// PLAYER - The hero themselves
bool isPlayer = character == Hero.MainHero.CharacterObject;

// COMPANIONS - Heroes belonging to player's clan
bool isCompanion = character.IsHero && 
                   character.HeroObject?.IsPlayerCompanion == true;

// RETINUE - Regular troops we track
bool isRetinue = _retinueState.TroopCounts.ContainsKey(character.StringId);
```

#### Lifecycle Events Summary

| Event | Companions | Retinue | Notes |
|-------|:----------:|:-------:|-------|
| **On Leave** | Stay with player | Stay with player | Player party remains invisible |
| **Return from Leave** | Still there | Still there | No transfer needed |
| **Leave Expires (Desertion)** | Stay with player | **LOST** | Deserters can't command |
| **Player Captured** | Native handling | **LOST** | Scattered in capture |
| **Enlistment Ends** | Stay with player | **DISMISSED** | Return to army ranks |
| **Lord Dies** | Stay with player | **LOST** | Chaos of defeat |
| **Army Defeated** | Stay with player | **LOST** | Scattered in defeat |
| **Battle Victory** | Survivors remain | Survivors remain | Native casualty tracking |
| **Battle Defeat** | Survivors remain | Survivors remain | Native casualty tracking |
| **Cannot Pay Upkeep** | Unaffected | 1 deserts/day | Daily tick check |
| **Change Soldier Type** | Unaffected | **DISMISSED** | Must dismiss first |

#### Detailed Event Handling

**On Leave:**
```csharp
private void OnLeaveStarted()
{
    // NO special handling needed for retinue
    // Player party stays invisible, retinue stays in MemberRoster
    _isOnLeave = true;
    
    ModLogger.Info("Retinue", 
        $"Leave started: {GetRetinueCount()} retinue, {GetCompanionCount()} companions");
}
```

**Player Captured:**
```csharp
private void OnPlayerCaptured()
{
    // Retinue scatters when player is captured
    ClearRetinueTroops("capture");
    
    var msg = new TextObject("{=ct_capture_retinue_lost}" +
        "Your retinue has scattered. Your soldiers fled when you were captured.");
    InformationManager.DisplayMessage(new InformationMessage(msg.ToString()));
}
```

**Enlistment Ends:**
```csharp
private void OnEnlistmentEnd(bool isHonorableDischarge)
{
    // Retinue returns to army (dismissed)
    ClearRetinueTroops("enlistment_end");
    
    var msg = new TextObject("{=ct_enlist_end_retinue}" +
        "Your soldiers have returned to the army ranks.");
    InformationManager.DisplayMessage(new InformationMessage(msg.ToString()));
    
    // Companions handled by existing RestoreCompanionsToPlayer()
}
```

**Army Defeated / Lord Dies:**
```csharp
private void OnArmyDefeatedOrLordDied()
{
    // Retinue lost in chaos of defeat
    ClearRetinueTroops("army_defeat");
    
    var msg = new TextObject("{=ct_defeat_retinue_lost}" +
        "In the chaos of defeat, your retinue has scattered.");
    InformationManager.DisplayMessage(new InformationMessage(msg.ToString()));
}
```

**Core Cleanup Method:**
```csharp
/// <summary>
/// Removes all retinue troops from player's roster and clears state.
/// Called on capture, enlistment end, army defeat, or type change.
/// </summary>
/// <param name="reason">Logging reason for debugging</param>
private void ClearRetinueTroops(string reason)
{
    var main = MobileParty.MainParty;
    if (main == null) return;
    
    int totalCleared = 0;
    
    foreach (var (troopId, count) in _retinueState.TroopCounts.ToList())
    {
        if (count <= 0) continue;
        
        var character = CharacterObject.Find(troopId);
        if (character != null)
        {
            main.MemberRoster.AddToCounts(character, -count);
            totalCleared += count;
            
            ModLogger.Debug("Retinue", $"Cleared {count}× {character.Name}");
        }
    }
    
    // Reset state
    _retinueState.TroopCounts.Clear();
    _retinueState.SelectedTypeId = null;
    _retinueState.DaysSinceLastTrickle = 0;
    
    ModLogger.Info("Retinue", $"Cleared {totalCleared} retinue troops (reason: {reason})");
}
```

#### Casualty Tracking (Battle)

```csharp
/// <summary>
/// Called before battle to snapshot retinue counts.
/// </summary>
private void OnBattleStarted()
{
    _preBattleCounts = new Dictionary<string, int>(_retinueState.TroopCounts);
    ModLogger.Debug("Retinue", $"Battle started with {GetRetinueCount()} soldiers");
}

/// <summary>
/// Called after battle to reconcile casualties.
/// Native roster updates handle deaths; we just sync our tracking.
/// </summary>
private void OnBattleEnded()
{
    var roster = MobileParty.MainParty?.MemberRoster;
    if (roster == null) return;
    
    int casualties = 0;
    
    foreach (var (troopId, preBattleCount) in _preBattleCounts)
    {
        var character = CharacterObject.Find(troopId);
        if (character == null) continue;
        
        int currentCount = roster.GetTroopCount(character);
        int lost = preBattleCount - currentCount;
        
        if (lost > 0)
        {
            casualties += lost;
            _retinueState.TroopCounts[troopId] = currentCount;
            ModLogger.Debug("Retinue", $"Lost {lost}× {character.Name} in battle");
        }
    }
    
    // Remove entries with 0 troops
    _retinueState.TroopCounts = _retinueState.TroopCounts
        .Where(kv => kv.Value > 0)
        .ToDictionary(kv => kv.Key, kv => kv.Value);
    
    if (casualties > 0)
    {
        ModLogger.Info("Retinue", 
            $"Battle ended: {casualties} casualties, {GetRetinueCount()} remaining");
    }
}
```

#### Helper Methods

```csharp
/// <summary>
/// Gets total retinue soldier count.
/// </summary>
public int GetRetinueCount()
{
    return _retinueState.TroopCounts?.Values.Sum() ?? 0;
}

/// <summary>
/// Gets companion count from player's party.
/// </summary>
public int GetCompanionCount()
{
    return MobileParty.MainParty?.MemberRoster
        .GetTroopRoster()
        .Count(e => e.Character.IsHero && 
                    e.Character.HeroObject?.IsPlayerCompanion == true) ?? 0;
}

/// <summary>
/// Logs full party breakdown for debugging.
/// </summary>
public void LogPartyBreakdown()
{
    var roster = MobileParty.MainParty?.MemberRoster;
    if (roster == null) return;
    
    int total = roster.TotalManCount;
    int companions = GetCompanionCount();
    int retinue = GetRetinueCount();
    int other = total - 1 - companions - retinue; // -1 for player
    
    ModLogger.Debug("Party", 
        $"Breakdown: Player=1, Companions={companions}, " +
        $"Retinue={retinue}, Other={other}, Total={total}");
}
```

### Leadership Notification

When player reaches **Tier 4**, display a dialog:

> **"Promotion to Leadership"**
>
> Your service has not gone unnoticed. You've been granted the authority to command a small lance of soldiers in battle. 
>
> Visit the Command Tent to request men be assigned to your command. Know that you'll be responsible for their welfare—each soldier in your care will cost 2 denars per day in upkeep.
>
> [Understood]

---

## Companion Management

Companions can be assigned to:
- **Battle Roster**: Companions who join the player in combat
- **Mission Roster**: (Future) Companions available for special missions

See [Companion Management](../Core/companion-management.md) for full details.

---

## Technical Implementation

### Modular Architecture

The Command Tent system is split into **modular components** for easy maintenance:

```
src/Features/CommandTent/
├── Behaviors/
│   └── CommandTentBehavior.cs       # Main campaign behavior, event hooks
├── Core/
│   ├── CommandTentConfig.cs         # Configuration constants
│   ├── ServiceRecordManager.cs      # Faction record tracking
│   ├── RetinueManager.cs            # Soldier management (add/remove/track)
│   └── RetinueLifecycleHandler.cs   # Leave, capture, enlistment end handling
├── Data/
│   ├── FactionServiceRecord.cs      # Per-faction data class
│   ├── LifetimeServiceRecord.cs     # Cross-faction totals
│   └── RetinueState.cs              # Current retinue state (troops, type, cooldowns)
├── Systems/
│   ├── RetinueTrickleSystem.cs      # Free, slow replenishment (daily tick)
│   ├── RetinueUpkeepSystem.cs       # Daily gold deduction, desertion
│   ├── RetinueCasualtyTracker.cs    # Battle casualty reconciliation
│   └── ServiceStatisticsSystem.cs   # Kill/battle tracking
└── UI/
    └── CommandTentMenuHandler.cs    # Menu integration
```

**Key Design Principles:**
- **Single Responsibility**: Each class handles one aspect (lifecycle vs. upkeep vs. trickle)
- **Event-Driven**: Hooks into existing campaign events (capture, enlistment end, battle end)
- **State Isolation**: `RetinueState` is the single source of truth for retinue data
- **Logging**: Every action logs to `ModLogger` for debugging
- **Graceful Cleanup**: `ClearRetinueTroops(reason)` handles all cleanup scenarios

### Data Storage

**Persisted Data (SyncData):**
```csharp
// ═══════════════════════════════════════════════════════════════
// SERVICE RECORDS
// ═══════════════════════════════════════════════════════════════

// Faction-specific records (keyed by faction ID)
private Dictionary<string, FactionServiceRecord> _factionRecords;

// Cross-faction lifetime totals
private int _lifetimeKills;
private int _lifetimeTermsCompleted;
private int _lifetimeDaysServed;
private List<string> _factionsServed;

// Current term tracking
private int _currentTermBattles;
private int _currentTermKills;

// ═══════════════════════════════════════════════════════════════
// RETINUE STATE
// ═══════════════════════════════════════════════════════════════

// What soldier type the player chose ("infantry", "archers", etc.)
private string _retinueSelectedTypeId;

// Which specific CharacterObjects we added and how many
// Key = CharacterObject.StringId, Value = count
// This allows us to distinguish retinue from other troops
private Dictionary<string, int> _retinueTroopCounts;

// Replenishment tracking
private int _daysSinceLastTrickle;
private CampaignTime _requisitionCooldownEnd;

// Battle casualty tracking (transient, not saved)
[NonSerialized]
private Dictionary<string, int> _preBattleCounts;
```

**RetinueState Helper Class:**
```csharp
/// <summary>
/// Encapsulates retinue state for cleaner access and serialization.
/// </summary>
public class RetinueState
{
    public string SelectedTypeId { get; set; }
    public Dictionary<string, int> TroopCounts { get; set; } = new();
    public int DaysSinceLastTrickle { get; set; }
    public CampaignTime RequisitionCooldownEnd { get; set; }
    
    public int TotalSoldiers => TroopCounts?.Values.Sum() ?? 0;
    
    public bool HasRetinue => !string.IsNullOrEmpty(SelectedTypeId) && TotalSoldiers > 0;
    
    public void Clear()
    {
        TroopCounts?.Clear();
        SelectedTypeId = null;
        DaysSinceLastTrickle = 0;
    }
}
```

**FactionServiceRecord Class:**
```csharp
public class FactionServiceRecord
{
    public string FactionId { get; set; }
    public string FactionType { get; set; }  // "kingdom", "minor", "merc"
    public int TermsCompleted { get; set; }
    public int TotalDaysServed { get; set; }
    public int HighestTier { get; set; }
    public int BattlesFought { get; set; }
    public int LordsServed { get; set; }
    public int Enlistments { get; set; }
}
```

### Debugging System

Lightweight logging for diagnostics:

```csharp
// Log categories for Command Tent
ModLogger.Debug("CommandTent", $"Service record updated for {factionId}");
ModLogger.Debug("Retinue", $"Spawning {count} {type} soldiers");
ModLogger.Debug("Upkeep", $"Deducted {cost} gold for {soldierCount} soldiers");
ModLogger.StateChange("Retinue", "Empty", "Lance", "Player purchased 5 infantry");

// Error tracking
ModLogger.Error("Retinue", $"Failed to spawn soldiers: {ex.Message}");
ModLogger.Warn("Upkeep", $"Player cannot afford upkeep, desertion triggered");
```

**Debug Output Location**: `Modules/Enlisted/Debugging/` (per memory [[memory:7862710]])

### Key APIs

**Formation Control (Battle):**
```csharp
// Team role control - determines what player can command
Team.SetPlayerRole(isPlayerGeneral, isPlayerSergeant)
// (false, true) = Sergeant: commands own formation only
// (false, false) = No command authority  
// (true, false) = General: commands all formations

// Formation ownership - who can issue orders to a formation
Formation.PlayerOwner = agent;    // Agent that owns formation (for order UI)
Formation.Captain = agent;        // Formation leader (affects morale)

// Agent formation assignment
agent.Formation = targetFormation;  // Move agent to specific formation

// Order controller - who has the command interface
Team.PlayerOrderController.Owner = lordAgent;  // Transfer command UI to lord
```

**Faction Detection:**
```csharp
// Check faction type
faction.IsMinorFaction     // Minor faction
faction.IsBanditFaction    // Bandit clan
clan.IsClanTypeMercenary   // Mercenary company
faction.IsKingdomFaction   // Major kingdom
```

**Troop Spawning:**
```csharp
// Get faction-appropriate troops by tier
CharacterHelper.GetTroopTree(baseTroop, minTier: 3f, maxTier: 4f)

// Get formation class for troop type
character.GetFormationClass()  // Returns FormationClass enum
character.IsInfantry
character.IsMounted
character.IsRanged
```

**Gold Management:**
```csharp
// Recruitment cost
Campaign.Current.Models.PartyWageModel.GetTroopRecruitmentCost(troop, Hero.MainHero)

// Daily wage (for reference)
Campaign.Current.Models.PartyWageModel.GetCharacterWage(character)

// Deduct gold
Hero.MainHero.ChangeHeroGold(-amount);
```

---

## Implementation Phases

### Phase 1: Service Records Foundation
**Goal**: Faction-specific record tracking and display

1. Create `FactionServiceRecord` data class
2. Create `ServiceRecordManager` for CRUD operations
3. Implement faction key generation (kingdom/minor/merc detection)
4. Add SyncData serialization for records dictionary
5. Hook into enlistment events to update records
6. Basic logging for record updates

**Deliverables:**
- [ ] FactionServiceRecord.cs
- [ ] LifetimeServiceRecord.cs  
- [ ] ServiceRecordManager.cs
- [ ] SyncData integration
- [ ] Logging for all record changes

### Phase 2: Service Records UI
**Goal**: Display records in Command Tent menu

1. Create CommandTentMenuHandler
2. Add "Service Records" menu branch
3. Implement Current Posting display
4. Implement Faction Records list/detail view
5. Implement Lifetime Summary display

**Deliverables:**
- [ ] CommandTentMenuHandler.cs
- [ ] Menu integration with Enlisted Menu
- [ ] Current/Faction/Lifetime record displays

### Phase 3: Retinue Core System (Add to Player Party)
**Goal**: Basic soldier management with party-based tracking

1. Create `RetinueState` data class
2. Create `RetinueManager` for soldier tracking
3. Implement **tier-based capacity checks** (no renown requirement)
4. Implement **party size safeguard checks** (respect native party limit)
5. Implement purchase logic - add soldiers to `MainParty.MemberRoster`
6. Implement dismiss logic - remove from roster
7. Add SyncData serialization for retinue state
8. Create Tier 4 leadership notification

**Key Logic:**
```csharp
// Add soldiers to player's party roster
MobileParty.MainParty.MemberRoster.AddToCounts(troopType, count);

// Check party size before adding
int availableSpace = PartyBase.MainParty.PartySizeLimit - PartyBase.MainParty.NumberOfAllMembers;
int toAdd = Math.Min(requested, availableSpace);
```

**Deliverables:**
- [ ] RetinueState.cs (type, count serialization)
- [ ] RetinueManager.cs
- [ ] Party size safeguard implementation
- [ ] Purchase/dismiss functionality (roster-based)
- [ ] Tier 4 notification dialog

### Phase 4: Retinue Purchase UI
**Goal**: Menu interface for soldier management

1. Add "Personal Retinue" menu branch
2. Implement soldier type selection (4 types)
3. Show **party size limit** in UI (available slots vs. tier capacity)
4. Implement initial purchase flow with gold cost display
5. Implement dismissal confirmation

**Deliverables:**
- [ ] Retinue menu screens
- [ ] Purchase/dismiss UI
- [ ] Party size display in UI
- [ ] Gold cost calculations

### Phase 5: Replenishment Systems
**Goal**: Trickle (free/slow) and Requisition (costly/instant) replenishment

**5A: Trickle System**
1. Add trickle state tracking (`_daysSinceLastTrickle`)
2. Hook into daily tick event
3. Implement 2-3 day randomized interval
4. Add 1 soldier per tick (to roster) with overfill protection
5. Add subtle notification on trickle

**5B: Instant Requisition System**
1. Add cooldown state tracking (`_requisitionCooldownEnd`)
2. Calculate cost: `GetTroopRecruitmentCost() × missing soldiers`
3. Implement instant fill logic with overfill protection
4. Implement 14-day cooldown enforcement
5. Add menu option and confirmation UI

**5C: Overfill Protection**
Both systems use shared capacity check:
```csharp
int tierCapacity = GetTierCapacity(_currentTier);
int partySpace = PartyBase.MainParty.PartySizeLimit - PartyBase.MainParty.NumberOfAllMembers;
int maxCanAdd = Math.Min(tierCapacity - currentCount, partySpace);
```

**Deliverables:**
- [ ] Trickle system (daily tick integration)
- [ ] Requisition system with cooldown
- [ ] Overfill protection (shared logic)
- [ ] Replenishment UI (requisition menu, cooldown display)
- [ ] Trickle and requisition notifications

### Phase 6: Daily Upkeep & Desertion
**Goal**: Automatic gold deduction and desertion on unpaid upkeep

1. Create `RetinueUpkeepSystem`
2. Hook into daily tick event (after trickle, before other systems)
3. Implement upkeep deduction (2 gold/soldier/day)
4. Implement desertion when player cannot pay
5. Remove deserted soldier from `MainParty.MemberRoster`
6. Add notifications for upkeep charged / soldier deserted

**Deliverables:**
- [ ] RetinueUpkeepSystem.cs
- [ ] Daily tick integration
- [ ] Desertion logic (remove from roster)
- [ ] Upkeep/desertion notifications

### Phase 7: Battle Formation Assignment
**Goal**: Ensure player, companions, and retinue fight as unified squad

Since retinue is in player's `MemberRoster`, troops spawn automatically via native `PartyGroupTroopSupplier`. This phase ensures **unified formation assignment**.

1. Enhance `EnlistedFormationAssignmentBehavior`:
   - Find all agents originating from `PartyBase.MainParty`
   - Assign them to same formation as player
   - Set player as `PlayerOwner` of that formation
2. Verify command suppression (player = sergeant, not general)
3. Test casualty tracking (native handles this automatically)
4. Verify roster updates post-battle

**Key Code:**
```csharp
foreach (var agent in Team.ActiveAgents)
{
    var origin = agent.Origin as PartyGroupAgentOrigin;
    if (origin?.Party == PartyBase.MainParty)
    {
        agent.Formation = playerFormation;
    }
}
```

**Deliverables:**
- [ ] Enhanced EnlistedFormationAssignmentBehavior
- [ ] Unified squad formation assignment
- [ ] Command suppression verification
- [ ] Casualty tracking verification (native)

### Phase 8: Polish and Edge Cases
**Goal**: Handle all edge cases, final polish

1. Handle **player capture** (clear retinue from roster)
2. Handle **army defeat** (clear retinue from roster)
3. Handle **enlistment end** (clear retinue from roster)
4. Handle **type change** (dismiss first, then purchase new)
5. Handle **tier demotion** (if implemented - dismiss retinue)
6. Handle **companions fill party** (UI warning, limited retinue)
7. Comprehensive logging review
8. Performance optimization

**Deliverables:**
- [ ] Edge case handling
- [ ] Final logging pass
- [ ] Performance review

---

## Configuration

The Command Tent system uses both **C# constants** (for code) and **JSON config** (for modder customization).

### JSON Config: `ModuleData/Enlisted/command_tent_config.json`

```json
{
  "schemaVersion": 1,
  "enabled": true,
  
  "capacity_by_tier": {
    "4": { 
      "name": "Lance", 
      "max_soldiers": 5,
      "tier_distribution": {
        "infantry": { "2": 40, "3": 60 },
        "archers": { "2": 40, "3": 60 },
        "cavalry": { "3": 100 },
        "horse_archers": { "2": 30, "3": 70 }
      }
    },
    "5": { 
      "name": "Squad", 
      "max_soldiers": 10,
      "tier_distribution": {
        "infantry": { "2": 30, "3": 70 },
        "archers": { "2": 30, "3": 70 },
        "cavalry": { "2": 20, "3": 80 },
        "horse_archers": { "2": 20, "3": 80 }
      }
    },
    "6": { 
      "name": "Retinue", 
      "max_soldiers": 20,
      "tier_distribution": {
        "infantry": { "3": 60, "4": 40 },
        "archers": { "3": 60, "4": 40 },
        "cavalry": { "3": 50, "4": 50 },
        "horse_archers": { "3": 50, "4": 50 }
      }
    }
  },
  
  "soldier_types": {
    "infantry": {
      "id": "infantry",
      "display_key": "{=ct_type_infantry}Men-at-Arms",
      "description_key": "{=ct_type_infantry_desc}Foot soldiers with sword and shield.",
      "formation_class": "Infantry",
      "icon": "🗡️",
      "available_all_factions": true
    },
    "archers": {
      "id": "archers",
      "display_key": "{=ct_type_archers}Bowmen",
      "description_key": "{=ct_type_archers_desc}Skilled archers to loose volleys.",
      "formation_class": "Ranged",
      "icon": "🏹",
      "available_all_factions": true
    },
    "cavalry": {
      "id": "cavalry",
      "display_key": "{=ct_type_cavalry}Mounted Lancers",
      "description_key": "{=ct_type_cavalry_desc}Horsemen with lance and sword.",
      "formation_class": "Cavalry",
      "icon": "🐎",
      "available_all_factions": true
    },
    "horse_archers": {
      "id": "horse_archers",
      "display_key": "{=ct_type_horse_archers}Mounted Bowmen",
      "description_key": "{=ct_type_horse_archers_desc}Riders who loose arrows.",
      "formation_class": "HorseArcher",
      "icon": "🏇",
      "available_all_factions": false,
      "faction_whitelist": ["empire", "khuzait", "aserai"]
    }
  },
  
  "faction_overrides": {
    "vlandia": {
      "archers_display_key": "{=ct_type_crossbowmen}Crossbowmen",
      "archers_description_key": "{=ct_type_crossbowmen_desc}Armored crossbowmen.",
      "unavailable_types": ["horse_archers"]
    },
    "battania": {
      "archers_display_key": "{=ct_type_fians}Fian Archers",
      "archers_description_key": "{=ct_type_fians_desc}Legendary woodland archers.",
      "unavailable_types": ["horse_archers"],
      "tier_distribution_override": {
        "archers": { "3": 80, "4": 20 }
      }
    },
    "sturgia": {
      "unavailable_types": ["horse_archers"]
    },
    "khuzait": {
      "horse_archers_display_key": "{=ct_type_khuzait_ha}Steppe Riders",
      "tier_distribution_override": {
        "horse_archers": { "2": 40, "3": 60 }
      }
    },
    "aserai": {
      "horse_archers_display_key": "{=ct_type_mameluke}Mameluke Cavalry"
    }
  },
  
  "replenishment": {
    "trickle": {
      "enabled": true,
      "min_days": 2,
      "max_days": 3,
      "soldiers_per_tick": 1
    },
    "requisition": {
      "enabled": true,
      "cooldown_days": 14,
      "cost_multiplier": 1.0
    }
  },
  
  "economics": {
    "daily_upkeep_per_soldier": 2,
    "desertion_enabled": true
  }
}
```

### C# Constants: `CommandTentConfig.cs`

```csharp
public static class CommandTentConfig
{
    // ─────────────────────────────────────────────────────────────
    // Tier unlock thresholds
    // ─────────────────────────────────────────────────────────────
    public const int LanceTier = 4;      // Unlocks 5 soldiers
    public const int SquadTier = 5;      // Unlocks 10 soldiers
    public const int RetinueTier = 6;    // Unlocks 20 soldiers
    
    // ─────────────────────────────────────────────────────────────
    // Capacity by tier (purely tier-based, no renown requirement)
    // ─────────────────────────────────────────────────────────────
    public const int LanceCapacity = 5;
    public const int SquadCapacity = 10;
    public const int RetinueCapacity = 20;
    
    // ─────────────────────────────────────────────────────────────
    // Economics - Daily Upkeep
    // ─────────────────────────────────────────────────────────────
    public const int DailyUpkeepPerSoldier = 2;  // Gold per soldier per day
    
    // ─────────────────────────────────────────────────────────────
    // Replenishment - Trickle System (free, slow)
    // ─────────────────────────────────────────────────────────────
    public const int TrickleMinDays = 2;         // Minimum days between trickle
    public const int TrickleMaxDays = 3;         // Maximum days between trickle
    public const int TrickleSoldiersPerTick = 1; // Soldiers added per trickle
    
    // ─────────────────────────────────────────────────────────────
    // Replenishment - Instant Requisition (costly, fast)
    // ─────────────────────────────────────────────────────────────
    public const int RequisitionCooldownDays = 14; // Days between requisitions
    // Cost = GetTroopRecruitmentCost() × missing soldiers (no flat rate)
    
    // ─────────────────────────────────────────────────────────────
    // Party Size Safety
    // ─────────────────────────────────────────────────────────────
    // No fixed values - uses native PartyBase.PartySizeLimit
    // Minimum recommended party size at Tier 4: ~35 (Clan Tier 1)
    // This comfortably fits 20 retinue + 5 companions + player
    
    // ─────────────────────────────────────────────────────────────
    // Logging categories
    // ─────────────────────────────────────────────────────────────
    public const string LogCategory = "CommandTent";
    public const string RetinueLogCategory = "Retinue";
    public const string UpkeepLogCategory = "Upkeep";
    public const string TrickleLogCategory = "Trickle";
    public const string RequisitionLogCategory = "Requisition";
}
```

---

## Edge Cases

### Service Records

#### Faction Change Mid-Term
- If player's army changes faction mid-enlistment (rare):
  - Current faction record is updated
  - New faction record begins fresh
  - Lifetime totals continue accumulating

#### Minor Faction Without Kingdom
- Minor factions that aren't attached to a kingdom:
  - Use clan StringId as key
  - Mark as "minor" type in record
  - Full tracking still applies

#### Mercenary Company Service
- Mercenary clans (`IsClanTypeMercenary`):
  - Tracked separately from kingdoms
  - Use "merc_" prefix for record key
  - Same statistics tracked

### Retinue System

#### Zero Soldiers After Casualties
- If all soldiers die in battle:
  - Retinue type preserved
  - Player can replenish via trickle (free) or requisition (costly)
  - No need to re-select type

#### Cannot Afford Initial Purchase
- Gray out soldier type options player cannot afford
- Show cost next to each option

#### Tier Demotion (If Implemented)
- If player tier drops below 4:
  - Soldiers removed from roster
  - Message: "Your rank no longer permits personal command."

#### Quick Save/Load Mid-Battle
- Soldiers are in `MemberRoster`, which persists via native save
- On load, soldiers remain in roster and respawn normally

### Retinue Lifecycle

#### On Leave with Retinue
- Retinue stays in player's invisible party
- Player can move freely, retinue cannot be attacked
- On return from leave, retinue is intact
- If leave expires (desertion), retinue is lost

#### Player Captured with Retinue
- All retinue troops are immediately cleared from roster
- `ClearRetinueTroops("capture")` called
- Player notified: "Your retinue has scattered"
- Companions handled by native capture system

#### Enlistment Ends with Retinue
- All retinue troops dismissed ("return to army")
- `ClearRetinueTroops("enlistment_end")` called
- Player notified: "Your soldiers have returned to the ranks"
- Companions stay with player (existing behavior)

#### Lord Dies / Army Defeated with Retinue
- All retinue troops lost in chaos
- `ClearRetinueTroops("army_defeat")` called
- Player notified: "In the chaos of defeat, your retinue has scattered"
- Player enters grace period (existing behavior)
- Companions stay with player

#### Change Soldier Type with Existing Retinue
- Current soldiers must be dismissed first
- Confirmation dialog required
- After dismissal, player can select new type
- Dismissed soldiers do NOT return to player

#### Casualty Tracking After Battle
- Pre-battle: Snapshot `_retinueState.TroopCounts`
- Post-battle: Compare roster counts to snapshot
- Native `OnTroopKilled()` updates roster automatically
- We sync our tracking state to match roster
- Dead soldiers removed from tracking dictionary

### Replenishment Systems

#### Trickle While At Capacity
- If player is already at tier capacity:
  - Trickle check skipped silently (no notification)
  - Logged as debug: "Trickle skipped: already at capacity"

#### Requisition While At Capacity
- If player is already at tier capacity:
  - Requisition button grayed out
  - Message: "Your retinue is at full strength."

#### Requisition On Cooldown
- If requisition cooldown hasn't elapsed:
  - Requisition button grayed out
  - Show: "Cooldown: X days remaining"
  - Trickle still operates normally

#### Cannot Afford Requisition
- If player lacks gold for full requisition:
  - Show cost breakdown and player's gold
  - Requisition button grayed out
  - Player can wait for trickle instead

### Party Size Limits

#### Party Full - Cannot Add Any Soldiers
- If `PartySizeLimit - NumberOfAllMembers <= 0`:
  - Gray out entire retinue purchase UI
  - Message: "Party is full. Dismiss troops or increase party size."
  - Trickle skips silently (logged as debug)
  - Requisition disabled

#### Party Partially Full - Limited Retinue
- If available space < tier capacity:
  - Show warning in UI: "Party limit restricts retinue size"
  - Purchase/requisition only fills to available space
  - Example: Tier 6 (20 capacity) but only 7 party slots → can only have 7 soldiers

#### Companions Fill Party
- If companions + player = party limit:
  - Retinue feature effectively disabled for this player
  - UI message: "Your companions fill your party. No room for retinue soldiers."
  - Consider: Player may need to reduce companion count to use retinue

#### Party Limit Increases Mid-Enlistment
- If player gains clan tier or skills that increase party size:
  - Trickle will automatically fill new space over time
  - Requisition can instantly fill if off cooldown

#### Party Limit Decreases (Edge Case)
- If something reduces party limit below current roster size:
  - Native game handles this (some troops may be "over limit")
  - We don't forcibly remove soldiers, let native handle it

---

## Acceptance Criteria

### Service Records
- [ ] Faction records created automatically on first enlistment
- [ ] Minor factions tracked separately from major kingdoms
- [ ] Mercenary companies tracked with "merc_" prefix
- [ ] Cross-faction kills accumulate correctly
- [ ] Lifetime totals update on every enlistment
- [ ] All records persist across save/load
- [ ] UI displays current, faction, and lifetime records

### Personal Retinue - Core
- [ ] Tier 4 unlocks Lance (5 soldiers)
- [ ] Tier 5 unlocks Squad (10 soldiers)
- [ ] Tier 6 unlocks Retinue (20 soldiers)
- [ ] **Capacity is tier-based only (no renown requirement)**
- [ ] Four soldier types available (Infantry, Archers, Cavalry, Horse Archers)
- [ ] Soldiers stored in player's `MemberRoster` (not lord's)
- [ ] Leadership notification appears at Tier 4

### Faction Availability
- [ ] Horse Archers grayed out for Vlandia, Battania, Sturgia
- [ ] Horse Archers available for Empire, Khuzait, Aserai
- [ ] Unavailable types show tooltip: "{FACTION} does not field mounted archers."
- [ ] Faction-specific display names (Crossbowmen for Vlandia, Fians for Battania)
- [ ] Settings loaded from JSON config

### Troop Quality & Tier Distribution
- [ ] Tier 4 players get Tier 2-3 troops (40%/60% distribution)
- [ ] Tier 5 players get Tier 2-3 troops (30%/70% distribution)
- [ ] Tier 6 players get Tier 3-4 troops (60%/40% distribution)
- [ ] All soldiers are of ONE chosen type (no mixing Infantry + Archers)
- [ ] Tier distribution is a quality mix WITHIN the chosen type
- [ ] Faction overrides work (Battanian archers skew higher tier)
- [ ] Distribution configurable in JSON config
- [ ] Fallback to adjacent tier if specific tier unavailable for faction

### Replenishment Systems
- [ ] **Trickle System**: 1 soldier every 2-3 days (free, automatic)
- [ ] Trickle respects tier capacity (no overfill)
- [ ] Trickle respects party size limit (no overfill)
- [ ] Trickle notification when soldier added
- [ ] **Requisition System**: Instant fill for gold cost
- [ ] Requisition cost = `GetTroopRecruitmentCost()` × missing soldiers
- [ ] Requisition has 14-day cooldown
- [ ] Requisition respects tier capacity (no overfill)
- [ ] Requisition respects party size limit (no overfill)
- [ ] Requisition shows cooldown status in UI
- [ ] Both systems skip silently when at capacity

### Party Size Safeguards
- [ ] Check `PartySizeLimit` before adding soldiers
- [ ] Only add soldiers up to available party space
- [ ] UI shows party size limit vs. tier capacity
- [ ] UI warns when party limit restricts retinue
- [ ] Gray out retinue options when party is full
- [ ] Handle companions-fill-party edge case gracefully

### Gold Economics
- [ ] Purchase costs match native recruitment formula
- [ ] Daily upkeep of 2 gold per soldier
- [ ] One soldier deserts per unpaid day
- [ ] Desertion removes soldier from `MemberRoster`

### Retinue Lifecycle & State Management
- [ ] `RetinueState` tracks: `SelectedTypeId`, `TroopCounts`, `DaysSinceLastTrickle`, `RequisitionCooldownEnd`
- [ ] Retinue troops distinguished from companions via `TroopCounts` dictionary
- [ ] **On Leave**: Retinue stays in player's invisible party
- [ ] **Player Captured**: All retinue cleared, player notified
- [ ] **Enlistment Ends**: All retinue dismissed, player notified
- [ ] **Lord Dies/Army Defeated**: All retinue lost, player notified
- [ ] **Type Change**: Existing retinue dismissed before new type selected
- [ ] **Battle Casualties**: Pre-battle snapshot compared to post-battle roster
- [ ] Native casualty tracking updates roster; we sync our state
- [ ] `ClearRetinueTroops(reason)` handles all cleanup scenarios
- [ ] All lifecycle events logged with reason codes
- [ ] State persists via `SyncData` serialization

### Unified Squad Formation (Battle)
- [ ] Player assigned to formation based on duty (Infantry/Archer/Cavalry/HorseArcher)
- [ ] Player companions assigned to SAME formation as player
- [ ] Retinue soldiers assigned to SAME formation as player
- [ ] All player party agents in unified squad
- [ ] Player can issue orders to their own squad
- [ ] Player CANNOT issue orders to other formations
- [ ] Player CANNOT access Order of Battle screen
- [ ] Command authority transferred to lord (not player)
- [ ] Player set as PlayerOwner of their formation only
- [ ] Formation assignment works in field, siege, and sally out battles
- [ ] Native casualty tracking updates roster automatically

### Debugging
- [ ] All record updates logged
- [ ] All retinue actions logged (purchase, dismiss, replenish)
- [ ] Trickle events logged (added or skipped)
- [ ] Requisition events logged (success, blocked with reason)
- [ ] Party size checks logged
- [ ] Errors logged with full context
- [ ] State changes logged with old→new values
- [ ] Logs output to Debugging folder

---

## Future Considerations

### Retinue Veterancy
- Track battles survived per retinue
- Veterans gain slight stat bonuses
- Named veterans possible ("Old Marcus")

### Retinue Naming
- Allow players to name their retinue
- "The Iron Shields" (Infantry)
- "Whitecliff Archers" (Bowmen)

### Mixed Retinue (Tier 7+?)
- Allow split composition at higher tiers
- E.g., 10 infantry + 5 archers

### Faction-Specific Troop Names
- Empire: "Legionaries"
- Battania: "Fian Runners"  
- Khuzait: "Kheshig"

### Service Medals/Awards
- Visual achievements for service milestones
- "Veteran of 100 Battles"
- "Three-Term Serviceman"

---

## Localization (XML Strings)

Add these to `ModuleData/Languages/enlisted_strings.xml`:

```xml
<!-- ═══════════════════════════════════════════════════════════════ -->
<!-- COMMAND TENT - Main Menu -->
<!-- ═══════════════════════════════════════════════════════════════ -->
<string id="ct_title" text="Command Tent" />
<string id="ct_menu_intro" text="The canvas flaps in the breeze. Maps and tallies cover a makeshift table. Your small corner of the army's camp." />
<string id="ct_option_records" text="Review service records" />
<string id="ct_option_retinue" text="Muster personal retinue" />
<string id="ct_option_companions" text="Companion assignments" />
<string id="ct_option_back" text="Return to camp" />

<!-- ═══════════════════════════════════════════════════════════════ -->
<!-- COMMAND TENT - Soldier Types (Base) -->
<!-- ═══════════════════════════════════════════════════════════════ -->
<string id="ct_type_infantry" text="Men-at-Arms" />
<string id="ct_type_infantry_desc" text="Foot soldiers with sword and shield. Steady in the line." />
<string id="ct_type_archers" text="Bowmen" />
<string id="ct_type_archers_desc" text="Skilled archers to loose volleys upon your command." />
<string id="ct_type_cavalry" text="Mounted Lancers" />
<string id="ct_type_cavalry_desc" text="Horsemen with lance and sword. Swift and deadly." />
<string id="ct_type_horse_archers" text="Mounted Bowmen" />
<string id="ct_type_horse_archers_desc" text="Riders who loose arrows from the saddle." />

<!-- ═══════════════════════════════════════════════════════════════ -->
<!-- COMMAND TENT - Faction-Specific Troop Names -->
<!-- ═══════════════════════════════════════════════════════════════ -->
<string id="ct_type_crossbowmen" text="Crossbowmen" />
<string id="ct_type_crossbowmen_desc" text="Armored crossbowmen with deadly bolts." />
<string id="ct_type_fians" text="Fian Archers" />
<string id="ct_type_fians_desc" text="Legendary woodland archers of Battania." />
<string id="ct_type_khuzait_ha" text="Steppe Riders" />
<string id="ct_type_khuzait_ha_desc" text="Masters of mounted archery from the steppes." />
<string id="ct_type_mameluke" text="Mameluke Cavalry" />
<string id="ct_type_mameluke_desc" text="Elite desert horsemen trained in bow and lance." />

<!-- ═══════════════════════════════════════════════════════════════ -->
<!-- COMMAND TENT - Retinue Management -->
<!-- ═══════════════════════════════════════════════════════════════ -->
<string id="ct_retinue_title" text="Personal Retinue" />
<string id="ct_retinue_intro" text="As a {RANK_NAME}, you may command a {UNIT_NAME} of men." />
<string id="ct_retinue_muster" text="Current Muster: {CURRENT}/{CAPACITY} soldiers" />
<string id="ct_retinue_upkeep" text="Daily Upkeep: {COST} denars" />
<string id="ct_retinue_none" text="No soldiers mustered. Choose a type to begin." />
<string id="ct_unit_lance" text="lance of five" />
<string id="ct_unit_squad" text="squad of ten" />
<string id="ct_unit_retinue" text="retinue of twenty" />

<!-- ═══════════════════════════════════════════════════════════════ -->
<!-- COMMAND TENT - Replenishment -->
<!-- ═══════════════════════════════════════════════════════════════ -->
<string id="ct_requisition_title" text="Requisition Men" />
<string id="ct_requisition_intro" text="A word to the right quartermaster, a few coins changing hands, and fresh soldiers report for duty." />
<string id="ct_requisition_cost" text="Cost: {COST} denars ({PER_SOLDIER} each)" />
<string id="ct_requisition_available" text="Available NOW" />
<string id="ct_requisition_cooldown" text="Cooldown: {DAYS} days remaining" />
<string id="ct_requisition_success" text="{COUNT} soldiers have reported for duty." />
<string id="ct_trickle_added" text="A new soldier has reported for duty." />

<!-- ═══════════════════════════════════════════════════════════════ -->
<!-- COMMAND TENT - Warnings & Errors -->
<!-- ═══════════════════════════════════════════════════════════════ -->
<string id="ct_warn_tier_locked" text="You must reach Tier 4 to command soldiers." />
<string id="ct_warn_party_full" text="Your party is full. Dismiss troops or increase party size." />
<string id="ct_warn_party_limited" text="Party size limits your retinue to {COUNT} soldiers." />
<string id="ct_warn_cannot_afford" text="You cannot afford this ({COST} denars required)." />
<string id="ct_warn_faction_unavailable" text="{FACTION_NAME} does not field mounted archers." />
<string id="ct_warn_full_retinue" text="Your retinue is at full strength." />

<!-- ═══════════════════════════════════════════════════════════════ -->
<!-- COMMAND TENT - Leadership Notification (Tier 4) -->
<!-- ═══════════════════════════════════════════════════════════════ -->
<string id="ct_leadership_title" text="Promotion to Leadership" />
<string id="ct_leadership_message" text="Your service has not gone unnoticed. You've been granted the authority to command a small lance of soldiers in battle.\n\nVisit the Command Tent to request men be assigned to your command. Know that you'll be responsible for their welfare—each soldier in your care will cost 2 denars per day in upkeep." />
<string id="ct_leadership_acknowledge" text="Understood" />

<!-- ═══════════════════════════════════════════════════════════════ -->
<!-- COMMAND TENT - Upkeep & Desertion -->
<!-- ═══════════════════════════════════════════════════════════════ -->
<string id="ct_upkeep_charged" text="Retinue upkeep: {COST} denars deducted." />
<string id="ct_upkeep_desertion" text="Unable to pay upkeep. A soldier has deserted." />
<string id="ct_dismiss_confirm" text="Are you certain? Your {COUNT} soldiers will return to the ranks." />
<string id="ct_dismiss_success" text="Your soldiers have been dismissed." />

<!-- ═══════════════════════════════════════════════════════════════ -->
<!-- COMMAND TENT - Retinue Lifecycle Events -->
<!-- ═══════════════════════════════════════════════════════════════ -->
<string id="ct_capture_retinue_lost" text="Your retinue has scattered. Your soldiers fled when you were captured." />
<string id="ct_desert_retinue_lost" text="Your retinue has abandoned you. Deserters cannot command men." />
<string id="ct_enlist_end_retinue" text="Your soldiers have returned to the army ranks. Serve again to command new men." />
<string id="ct_defeat_retinue_lost" text="In the chaos of defeat, your retinue has scattered." />
<string id="ct_lord_died_retinue" text="With your lord fallen, your retinue has scattered to the winds." />
<string id="ct_type_change_dismiss" text="Your current soldiers have been dismissed to make way for new recruits." />
```

---

## Related Documents

- [Companion Management](../Core/companion-management.md) - Companion battle/mission assignments
- [Recon Mission](../Missions/recon-mission.md) - Retinue integration with missions
- [Menu Interface](./menu-interface.md) - Parent menu system
- [Enlistment System](../Core/enlistment.md) - Tier and service tracking
