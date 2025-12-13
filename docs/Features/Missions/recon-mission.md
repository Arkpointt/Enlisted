# Feature Spec: Recon Mission System

## Table of Contents

- [Overview](#overview)
- [Purpose](#purpose)
- [Inputs/Outputs](#inputsoutputs)
- [Phase 1: Core Mission System](#phase-1-core-mission-system)
  - [Behavior](#behavior)
  - [Scout Party Management](#scout-party-management)
  - [Enemy Party Spawning and Cleanup](#enemy-party-spawning-and-cleanup)
  - [Technical Implementation](#technical-implementation)
  - [Edge Cases](#edge-cases)
  - [Acceptance Criteria](#acceptance-criteria)
- [Phase 2: Risk/Reward Timer System](#phase-2-riskreward-timer-system)
  - [Behavior](#behavior-1)
  - [Technical Implementation](#technical-implementation-1)
  - [Acceptance Criteria](#acceptance-criteria-1)
- [Phase 3: Discovery and Intel Gathering](#phase-3-discovery-and-intel-gathering)
  - [Behavior](#behavior-2)
  - [Technical Implementation](#technical-implementation-2)
  - [Acceptance Criteria](#acceptance-criteria-2)
- [Phase 4: Evasion and Stealth Mechanics](#phase-4-evasion-and-stealth-mechanics)
  - [Behavior](#behavior-3)
  - [Enemy Party Spawning (Danger Escalation)](#enemy-party-spawning-danger-escalation)
  - [Technical Implementation](#technical-implementation-3)
  - [Acceptance Criteria](#acceptance-criteria-3)
- [Phase 5: Strategic Intel Reporting](#phase-5-strategic-intel-reporting)
  - [Behavior](#behavior-4)
  - [Technical Implementation](#technical-implementation-4)
  - [Acceptance Criteria](#acceptance-criteria-4)
- [Phase 6: Skill Integration](#phase-6-skill-integration)
  - [Behavior](#behavior-5)
  - [Technical Implementation](#technical-implementation-5)
  - [Acceptance Criteria](#acceptance-criteria-5)
- [Configuration](#configuration)
- [Debugging](#debugging)

---

## Overview
A mission system allowing Tier 4+ enlisted players to accept scouting assignments from their lord. The player temporarily detaches from the army to scout ahead, providing a 30% sight bonus to the army while becoming vulnerable to encounters. Features include intel gathering, risk/reward timing, evasion mechanics, and strategic reporting.

## Purpose
Add meaningful gameplay variety for experienced enlisted players. Creates tension between safety (staying with army) and reward (scouting bonuses). Gives players agency and a unique role within the military structure.

## Inputs/Outputs

**Inputs:**
- Player tier (must be 4+ to unlock)
- Army state (patrolling, defending, besieging, gathering)
- Nearby threat levels (enemy parties, settlement threat intensity)
- Player distance from lord during mission
- Player Scouting skill (affects discovery rate)
- Terrain type (affects detection difficulty)
- Time of day (affects visibility)

**Outputs:**
- 30-60% sight bonus for entire army during mission
- XP rewards scaled by mission duration and discoveries
- Intel reports that can influence army behavior
- Gold bonuses for quality intel
- Relation changes with lord based on mission success
- Discovery notifications (enemy positions, caravans, etc.)

---

## Phase 1: Core Mission System

### Behavior

**Mission Trigger (Randomized based on army situation):**

The lord periodically considers sending a scout ahead. Probability calculated hourly:

| Condition | Bonus Chance | Total |
|-----------|--------------|-------|
| Base chance | +5% | 5% |
| High threat nearby (NearbyLandThreatIntensity > AllyIntensity × 0.25) | +15% | 20% |
| Enemy parties within 50 units | +10% | 30% |
| Army patrolling (DefaultBehavior = PatrolAroundPoint) | +7% | 37% |
| Army defending (DefaultBehavior = DefendSettlement) | +5% | 42% |
| Army waiting/gathering (IsWaitingForArmyMembers) | +3% | 45% |
| Pre-siege (ArmyType = Besieger, no active siege) | +20% | 65% |

Cooldown: 6 hours between checks. One mission per campaign (configurable).

**Mission Offer Dialog:**
- Lord approaches player with roleplay-appropriate request
- "The land ahead is unclear to us. Will you ride out and scout what lies beyond?"
- "We need eyes ahead of the column. Are you willing to take the risk?"
- Accept: Mission starts immediately
- Deny: Marked as declined, no penalty

**Mission Start:**
1. Player party released from escort (SetMoveModeHold)
2. Player becomes visible on map (IsVisible = true)
3. Player becomes vulnerable to encounters (IsActive = true, IgnoreByOtherPartiesTill cleared)
4. Army reference stored for sight bonus application
5. Timer starts tracking mission duration
6. **Scout troops assigned to player party** (see Scout Party Management below)

**Distance Tracking (checked every 5 seconds):**

| Distance | State | Effect |
|----------|-------|--------|
| < 20 units | Completion Range | Can complete mission by returning |
| 20-50 units | Normal | Mission continues normally |
| 50-100 units | Warning | Yellow warning message |
| > 100 units | Critical | Red warning, 30 second failure countdown |

**Mission End:**
- **Success**: Player returns within 20 units of lord
- **Failure**: Distance > 100 for 30+ seconds, player captured
- **Cancelled**: Army defeated, army disbands, lord captured/dies
- On success: Re-attach to army (SetMoveEscortParty), IsVisible = false, show results
- On failure/cancelled: Scout troops removed, enemies despawned, player enters grace period (if cancelled)
- **Scout troops removed from player party** (see Scout Party Management below)
- **All spawned enemy parties scheduled for despawn** (30 seconds, or immediately if cancelled)

### Scout Party Management

**Troop Assignment on Mission Start:**
When the mission begins, the lord assigns temporary scout troops to the player's party. These troops are faction-appropriate and match the player's tier.

| Player Tier | Scout Composition |
|-------------|-------------------|
| Tier 4 | 2-3 Tier 3 cavalry (faction appropriate) |
| Tier 5 | 3-4 Tier 3-4 cavalry |
| Tier 6 | 4-5 Tier 4 cavalry + 1 elite scout |

**Implementation:**
```csharp
private List<(CharacterObject, int)> _assignedScouts = new();

private void AssignScoutTroops()
{
    var lordCulture = EnlistmentBehavior.Instance.CurrentLord.Culture;
    var tier = EnlistmentBehavior.Instance.EnlistmentTier;
    
    // Get tier 3-4 cavalry from lord's faction
    var scoutTroops = CharacterHelper.GetTroopTree(lordCulture.EliteBasicTroop, 3f, 4f)
        .Where(t => t.IsMounted)
        .ToList();
    
    if (!scoutTroops.Any()) return;
    
    var count = tier switch
    {
        4 => MBRandom.RandomInt(2, 3),
        5 => MBRandom.RandomInt(3, 4),
        6 => MBRandom.RandomInt(4, 5),
        _ => 2
    };
    
    var selectedTroop = scoutTroops.GetRandomElement();
    MobileParty.MainParty.MemberRoster.AddToCounts(selectedTroop, count);
    _assignedScouts.Add((selectedTroop, count));
    
    ModLogger.Info("Recon", $"Assigned {count}x {selectedTroop.Name} to scout party");
}
```

**Troop Removal on Mission End:**
When the mission ends (success or failure), all assigned scout troops are removed from the player's party and returned to the army.

```csharp
private void RemoveScoutTroops()
{
    foreach (var (troop, count) in _assignedScouts)
    {
        // Remove with negative count
        MobileParty.MainParty.MemberRoster.AddToCounts(troop, -count);
        ModLogger.Info("Recon", $"Returned {count}x {troop.Name} to army");
    }
    _assignedScouts.Clear();
}
```

**Edge Cases:**
- If scout troops die in battle: They're simply not returned (no penalty)
- If player captures prisoners: Prisoners stay with player
- Save/load: `_assignedScouts` list persisted via SyncData

### Enemy Party Spawning and Cleanup

**Spawned Enemy Tracking:**
Enemy parties spawned during the mission are tracked for cleanup.

```csharp
private List<MobileParty> _spawnedEnemyParties = new();
private CampaignTime _despawnScheduledTime = CampaignTime.Never;
```

**Despawn on Mission End:**
When mission ends, all spawned enemy parties are scheduled for despawn after 30 seconds to avoid disrupting gameplay.

```csharp
private void ScheduleEnemyDespawn()
{
    _despawnScheduledTime = CampaignTime.Now + CampaignTime.Seconds(30);
    ModLogger.Info("Recon", $"Scheduled {_spawnedEnemyParties.Count} enemy parties for despawn");
}

// Called in hourly tick or realtime tick
private void CheckDespawnSchedule()
{
    if (_despawnScheduledTime == CampaignTime.Never) return;
    if (CampaignTime.Now < _despawnScheduledTime) return;
    
    foreach (var party in _spawnedEnemyParties.ToList())
    {
        if (party?.IsActive == true)
        {
            DestroyPartyAction.Apply(null, party);
            ModLogger.Debug("Recon", $"Despawned enemy party: {party.Name}");
        }
    }
    _spawnedEnemyParties.Clear();
    _despawnScheduledTime = CampaignTime.Never;
}
```

**Immediate Despawn Conditions:**
Parties are despawned immediately (not delayed) if:
- Player is captured
- Lord dies
- Army disbands
- Player ends enlistment

### Technical Implementation

**Files:**
- `src/Features/Recon/Behaviors/ReconMissionBehavior.cs` - Main behavior (includes army defeat monitoring)
- `src/Features/Recon/Core/ReconConfig.cs` - Configuration constants
- `src/Features/Recon/Core/ReconStateManager.cs` - State machine
- `src/Features/Recon/Core/ReconTriggerCalculator.cs` - Probability logic
- `src/Features/Recon/Systems/ReconPartyManager.cs` - Scout troops & enemy spawning
- `src/Mod.GameAdapters/Patches/ReconSightBonusPatch.cs` - Sight bonus

**Army State Monitoring:**
- Checked every hourly tick and real-time tick during active mission
- Monitors `_reconArmy.LeaderParty.Party.MapEvent` for battle completion
- Validates army reference is still valid (not null, not disbanded)
- Integrates with `EnlistmentBehavior` grace period system

**State Machine:**
```
Inactive → OfferPending → Active → Warning ↔ Active → Completing → Completed
                              ↘ Critical → Failed
                              ↘ Cancelled (any time due to external events)
                                  
Cancelled triggers:
- Army defeated in battle
- Army disbands
- Lord captured/dies
- Player captured
```

**Configuration (ReconConfig.cs):**
```csharp
public static class ReconConfig
{
    // Distance thresholds
    public const float WarningDistance = 50f;
    public const float CriticalDistance = 100f;
    public const float CompletionDistance = 20f;
    
    // Timing
    public const int TriggerCooldownHours = 6;
    public const float DistanceCheckIntervalSeconds = 5f;
    public const int MinimumTier = 4;
    
    // Bonuses
    public const float BaseSightBonus = 0.3f; // 30%
}
```

**Sight Bonus Patch:**
- Harmony postfix on `DefaultMapVisibilityModel.GetPartySpottingRange`
- Check if ReconMissionBehavior.IsReconActive
- Check if party belongs to the stored army
- Add factor: `__result.AddFactor(0.3f, "Recon Scout")`

### Edge Cases

- Lord enters settlement during mission: Mission continues, player can still scout
- Player enters settlement during mission: Warning message, does not auto-fail
- Battle starts while scouting: Player can participate if within range, otherwise warned
- Save/load during mission: State restored, distance rechecked, assigned scouts tracked
- **Army defeated in battle**: See "Army Defeat Handling" below
- Army disbands: Mission auto-cancelled, player returned to normal state, enemies despawned immediately
- Lord captured: Mission auto-cancelled, grace period applies as normal, enemies despawned
- Player captured during scout: Mission fails, normal capture handling, scout troops lost

**Army Defeat Handling:**

When the army is defeated in battle during an active recon mission:

1. **Detection**: System monitors `_reconArmy.LeaderParty.Party.MapEvent` for battle completion
   - Checks if `MapEvent.IsFinalized == true` and `MapEvent.Winner != _reconArmy.LeaderParty.MapFaction`
   - Also checks if army is null or disbanded after battle

2. **Popup Notification**:
   ```
   "The army has been defeated! Your scouting mission is cancelled. 
   Find a lord from your faction and report to them as soon as possible."
   ```
   - Uses `InformationManager.ShowInquiry()` for modal popup
   - Single "OK" button to acknowledge

3. **Mission Termination**:
   - State transitions to `ReconMissionState.Cancelled`
   - Scout troops removed from player party
   - All spawned enemy parties despawned immediately
   - Player remains visible and active (no longer attached to defeated army)
   - Sight bonus removed (army reference cleared)

4. **Player Status**:
   - Player enters grace period (same as enlistment system)
   - Can find any lord from same faction to report
   - Mission marked as cancelled (not failed) - no penalty
   - Intel gathered during mission is lost (not reported)

5. **Recovery**:
   - Player must find a lord from same faction
   - Dialog option: "I was scouting when the army was defeated. I need to report."
   - Lord accepts report, player returns to normal enlisted status
   - No recon mission cooldown (defeat is external event)

**Technical Implementation:**
```csharp
private void CheckArmyDefeat()
{
    if (_reconArmy == null || _reconArmy.LeaderParty == null)
    {
        OnArmyDefeated();
        return;
    }
    
    var mapEvent = _reconArmy.LeaderParty.Party.MapEvent;
    if (mapEvent != null && mapEvent.IsFinalized)
    {
        var winner = mapEvent.Winner;
        var armyFaction = _reconArmy.LeaderParty.MapFaction;
        
        if (winner != armyFaction)
        {
            OnArmyDefeated();
        }
    }
}

private void OnArmyDefeated()
{
    // Show popup
    var message = new TextObject(
        "The army has been defeated! Your scouting mission is cancelled. " +
        "Find a lord from your faction and report to them as soon as possible.");
    
    var inquiry = new InquiryData(
        "Army Defeated",
        message.ToString(),
        true,
        false,
        "OK",
        null,
        () => { },
        null
    );
    
    InformationManager.ShowInquiry(inquiry, true);
    
    // End mission gracefully
    EndMission(ReconMissionState.Cancelled, "Army defeated");
    
    // Enter grace period (via EnlistmentBehavior)
    var enlistment = EnlistmentBehavior.Instance;
    if (enlistment?.CurrentLord != null)
    {
        // Grace period will be handled by enlistment system
        ModLogger.Info("Recon", "Army defeated - mission cancelled, grace period active");
    }
}
```

**Scout Troop Edge Cases:**
- Scout troops die in battle: Not returned to army (acceptable loss)
- No suitable cavalry in faction: Fall back to mounted militia or basic troops
- Player already has max party size: Scouts added anyway (temporary bonus)
- Save/load: `_assignedScouts` list persisted and validated on load

**Enemy Spawn Edge Cases:**
- No enemy kingdom at war: No spawns occur (log warning)
- Spawned party defeats player: Mission fails, party persists normally (becomes regular party)
- Player defeats spawned party: Party destroyed via combat, removed from tracking
- Multiple spawns active: All tracked, all despawn together when mission ends

### Acceptance Criteria

- [ ] Mission triggers based on army state and threat levels
- [ ] Player can accept or deny mission via dialog
- [ ] Player properly detaches and becomes visible
- [ ] Distance tracking works with appropriate warnings
- [ ] Army receives sight bonus during mission
- [ ] Player re-attaches correctly on mission end
- [ ] State persists through save/load
- [ ] Scout troops added to player party on mission start
- [ ] Scout troops removed from player party on mission end
- [ ] Spawned enemy parties tracked and despawned after mission
- [ ] **Army defeat detected and handled gracefully**
- [ ] **Popup notification appears when army is defeated**
- [ ] **Player enters grace period after army defeat**
- [ ] **Mission cancelled (not failed) with no penalty**
- [ ] **Player can report to any same-faction lord to recover**

---

## Phase 2: Risk/Reward Timer System

### Behavior

**Duration Tiers:**
The longer the player stays on recon, the greater the rewards but also the danger.

| Duration | Tier | Sight Bonus | XP Multiplier | Discovery Chance | Risk Level |
|----------|------|-------------|---------------|------------------|------------|
| 0-2 hours | 1 | 30% | 1.0x | Normal | Low |
| 2-6 hours | 2 | 40% | 1.5x | +25% | Medium |
| 6-12 hours | 3 | 50% | 2.0x | +50% | High |
| 12+ hours | 4 | 60% | 3.0x | +100% | Extreme |

**Danger Escalation:**
- Tier 2+: Enemy patrols more likely to notice player
- Tier 3+: Random chance of enemy party spawning near player
- Tier 4: Enemy parties actively hunt for the player

**Player Choice:**
- Player can return at any time to "lock in" current tier rewards
- Staying longer risks failure but multiplies rewards
- Push your luck mechanic creates tension

**Notification System:**
- "You've been scouting for 2 hours. The army's sight has improved to 40%."
- "Danger is increasing. Enemy patrols seem more active."
- "You've gathered excellent intel. Consider returning to report."

### Technical Implementation

**Files:**
- `src/Features/Recon/Systems/ReconTimerSystem.cs`

**Timer Tracking:**
```csharp
public class ReconTimerSystem
{
    private CampaignTime _missionStartTime;
    
    public int CurrentTier => CalculateTier(GetElapsedHours());
    public float CurrentSightBonus => GetSightBonusForTier(CurrentTier);
    public float CurrentXPMultiplier => GetXPMultiplierForTier(CurrentTier);
    
    public event Action<int> OnTierChanged;
}
```

### Acceptance Criteria

- [ ] Timer accurately tracks mission duration
- [ ] Tier changes trigger at correct thresholds
- [ ] Sight bonus scales with tier
- [ ] XP multiplier applies on mission completion
- [ ] Tier change notifications appear
- [ ] Danger escalation affects gameplay

---

## Phase 3: Discovery and Intel Gathering

### Behavior

**Discovery Types:**

| Type | Detection Range | Intel Value | Gameplay Effect |
|------|-----------------|-------------|-----------------|
| Enemy Army | 40 units | High | Warns lord of large force |
| Enemy Party | 30 units | Medium | Shows troop composition |
| Trade Caravan | 35 units | Medium | Potential raid target |
| Undefended Village | 40 units | Medium | Raid opportunity |
| Bandit Camp | 25 units | Low | Bounty opportunity |
| Friendly Force | 50 units | Low | Morale boost |

**Discovery Mechanics:**
- Scan performed every 30 seconds while scouting
- Uses native `MobileParty.StartFindingLocatablesAroundPosition` API
- Discovery radius: 30 + (ScoutingSkill × 0.2) units
- Discovery chance: 5% base + 0.1% per Scouting skill point
- Terrain modifiers: Forest -2%, Night -3%

**Progressive Intel:**
- Like native `ScoutEnemyGarrisonsIssueBehavior`, staying near a target builds intel
- 8 "progress ticks" (1 per minute) to fully scout a target
- Partial intel: "Large enemy force spotted"
- Full intel: "200 soldiers, mostly infantry, led by [Lord]"

**Discovery Notifications:**
- "Your scouts spotted an enemy army to the north!"
- "You've discovered a trade caravan traveling along the road."
- "A bandit camp has been located in the nearby hills."

### Technical Implementation

**Files:**
- `src/Features/Recon/Systems/ReconDiscoverySystem.cs`
- `src/Features/Recon/Models/ReconDiscovery.cs`

**Discovery Data:**
```csharp
public class ReconDiscovery
{
    public DiscoveryType Type { get; set; }
    public IMapPoint Location { get; set; }
    public float EstimatedStrength { get; set; }
    public Hero Leader { get; set; }
    public int IntelProgress { get; set; } // 0-8
    public CampaignTime DiscoveredAt { get; set; }
    public string Description { get; set; }
}
```

**Discovery Scanning:**
```csharp
private void ScanForDiscoveries()
{
    var radius = 30f + (Hero.MainHero.GetSkillValue(DefaultSkills.Scouting) * 0.2f);
    var data = MobileParty.StartFindingLocatablesAroundPosition(
        MobileParty.MainParty.GetPosition2D, radius);
    
    for (var party = MobileParty.FindNextLocatable(ref data); 
         party != null; 
         party = MobileParty.FindNextLocatable(ref data))
    {
        ProcessPotentialDiscovery(party);
    }
}
```

### Acceptance Criteria

- [ ] Discoveries detected based on range and skill
- [ ] Different discovery types provide appropriate intel
- [ ] Progressive intel builds over time near targets
- [ ] Notifications inform player of discoveries
- [ ] Discoveries stored for later reporting

---

## Phase 4: Evasion and Stealth Mechanics

### Behavior

**Detection System:**
Player can be spotted by enemy parties while scouting. Detection based on:

| Factor | Effect on Detection |
|--------|---------------------|
| Terrain: Forest | 30% harder to spot (from native) |
| Time: Night | 50% detection range |
| Movement: Stationary | 25% harder to spot |
| Party Size | Smaller = harder to spot |
| Enemy Scout skill | Higher = larger detection range |

**Detection Formula:**
```
DetectionRange = BaseRange × TerrainMod × TimeMod × MovementMod × PartyMod
BaseRange = 15 units
TerrainMod = 0.7 if forest, 1.0 otherwise
TimeMod = 0.5 if night, 1.0 otherwise
MovementMod = 0.75 if stationary, 1.0 if moving
PartyMod = 1.0 + (PartySize × 0.05)
```

**Chase Mechanics:**
If detected:
1. Enemy party gains "hunt" target on player
2. Enemy pursues for 30 seconds
3. Player must either:
   - Escape to safe distance (50+ units from enemy)
   - Return to army (within 20 units of lord)
   - Fight if caught

### Enemy Party Spawning (Danger Escalation)

**Spawn Triggers (based on Risk Tier from Phase 2):**

| Risk Tier | Spawn Behavior |
|-----------|----------------|
| Tier 1 (0-2 hrs) | No spawns - enemies must already exist on map |
| Tier 2 (2-6 hrs) | 10% chance per hour to spawn 1 scout patrol |
| Tier 3 (6-12 hrs) | 25% chance per hour, can spawn up to 2 patrols |
| Tier 4 (12+ hrs) | 50% chance per hour, patrols actively hunt player |

**Spawned Party Composition:**
Uses enemy faction's tier 3-4 cavalry for fast pursuit:

```csharp
private MobileParty SpawnEnemyScoutParty()
{
    var lordFaction = EnlistmentBehavior.Instance.CurrentLord.MapFaction;
    var enemyKingdom = lordFaction.GetEnemyKingdoms().FirstOrDefault();
    if (enemyKingdom == null) return null;
    
    var enemyCulture = enemyKingdom.Culture;
    var roster = TroopRoster.CreateDummyTroopRoster();
    
    // Get tier 3-4 mounted troops
    var cavalry = CharacterHelper.GetTroopTree(enemyCulture.EliteBasicTroop, 3f, 4f)
        .Where(t => t.IsMounted)
        .ToList();
    
    if (!cavalry.Any()) return null;
    
    var troopCount = MBRandom.RandomInt(4, 8);
    roster.AddToCounts(cavalry.GetRandomElement(), troopCount);
    
    // Spawn near player but not too close
    var playerPos = MobileParty.MainParty.GetPosition2D;
    var spawnOffset = new Vec2(
        MBRandom.RandomFloatRanged(-30, 30),
        MBRandom.RandomFloatRanged(-30, 30));
    var spawnPos = playerPos + spawnOffset;
    
    var party = CustomPartyComponent.CreateCustomPartyWithTroopRoster(
        new CampaignVec2(spawnPos.X, spawnPos.Y),
        1f, null,
        new TextObject("{=enemy_scout}Enemy Scouts"),
        enemyKingdom.Leader.Clan,
        roster,
        TroopRoster.CreateDummyTroopRoster(),
        enemyKingdom.Leader,
        customPartyBaseSpeed: 6f // Fast pursuit
    );
    
    // Make aggressive and hunt player
    party.Aggressiveness = 1f;
    party.SetMoveEngageParty(MobileParty.MainParty);
    
    // Track for cleanup
    _spawnedEnemyParties.Add(party);
    
    ModLogger.Info("Recon", $"Spawned enemy scout: {troopCount} troops from {enemyCulture.Name}");
    return party;
}
```

**Spawn Position Rules:**
- Minimum 20 units from player (gives time to react)
- Maximum 40 units from player (relevant to mission)
- Not directly between player and lord's army
- Prefers road/path positions when available

**Spawned Party Behavior:**
- `SetMoveEngageParty(MainParty)` - actively pursue player
- `Aggressiveness = 1f` - will initiate combat
- If player escapes 50+ units, party returns to patrol behavior
- Parties do NOT pursue into lord's army (fear of larger force)

**Near-Miss Bonus:**
- If player avoids detection while enemy was within 20 units: +50 XP
- Encourages risk-taking and rewards skillful play

**Stealth Tips (shown periodically):**
- "Forests provide cover. Stick to treelines."
- "Night provides darkness. Scout after dusk."
- "Standing still makes you harder to spot."

### Technical Implementation

**Files:**
- `src/Features/Recon/Systems/ReconEvasionSystem.cs`

**Detection Check:**
```csharp
private bool CheckIfDetected(MobileParty enemy)
{
    var distance = GetDistanceTo(enemy);
    var detectionRange = CalculateDetectionRange(enemy);
    
    if (distance < detectionRange)
    {
        // Random roll with skill bonus
        var escapeChance = 0.3f + (Hero.MainHero.GetSkillValue(DefaultSkills.Scouting) * 0.002f);
        if (MBRandom.RandomFloat < escapeChance)
        {
            // Near miss!
            OnNearMiss(enemy);
            return false;
        }
        return true; // Detected
    }
    return false;
}
```

**Events:**
```csharp
public event Action<MobileParty> OnDetectedByEnemy;
public event Action OnEscapedPursuit;
public event Action<MobileParty> OnNearMiss; // Bonus XP
public event Action OnCaughtByEnemy;
```

### Acceptance Criteria

- [ ] Detection system considers all factors (terrain, time, movement)
- [ ] Enemy parties can spot and chase player
- [ ] Player can escape by distance or returning to army
- [ ] Near-miss bonus rewards close calls
- [ ] Stealth tips appear contextually

---

## Phase 5: Strategic Intel Reporting

### Behavior

**Report Dialog:**
When player returns to lord after successful mission, they choose what to report:

**Report Options:**

| Report | Effect | Relation | XP |
|--------|--------|----------|-----|
| "Enemy weak at [Settlement]" | Army prioritizes attacking that settlement | +2 | +100 |
| "Strong force approaching" | Army goes defensive, prepares | +2 | +100 |
| "Safe route found" | Army avoids enemy patrols | +1 | +75 |
| "Valuable caravan spotted" | Intercept opportunity | +1 | +50 |
| "Nothing of note" | No effect | +0 | +25 |

**Report Quality:**
Based on discoveries made and Scouting skill:

| Quality | Requirements | Gold Bonus | Relation |
|---------|--------------|------------|----------|
| Poor | No discoveries | 0 | -1 |
| Standard | 1-2 discoveries | 50 | +0 |
| Good | 3+ discoveries | 150 | +1 |
| Excellent | 5+ discoveries OR enemy army found | 300 | +3 |

**Lord Reaction:**
- Poor: "A wasted effort. I expected better."
- Standard: "Acceptable work, soldier."
- Good: "Well done. This will serve us."
- Excellent: "Outstanding! The army is in your debt."

**Army Behavior Effect:**
Intel reports can influence the army's AI decisions:
- High-value targets get priority score boost
- Defensive intel increases defend behavior score
- This integrates with native `PartyThinkParams` and behavior scoring

### Technical Implementation

**Files:**
- `src/Features/Recon/Systems/ReconIntelReporter.cs`
- `src/Features/Recon/Models/ReconIntelReport.cs`

**Report Data:**
```csharp
public class ReconIntelReport
{
    public ReportType Type { get; set; }
    public IMapPoint Target { get; set; }
    public ReportQuality Quality { get; set; }
    public List<ReconDiscovery> SupportingDiscoveries { get; set; }
    public int GoldReward { get; set; }
    public int RelationChange { get; set; }
    public int XPReward { get; set; }
}
```

**Dialog Structure:**
```csharp
private void ShowReportDialog()
{
    var options = BuildReportOptions(_discoveries);
    
    MBInformationManager.ShowMultiSelectionInquiry(
        new MultiSelectionInquiryData(
            "Scout Report",
            "What will you report to the lord?",
            options,
            true, 1, 1,
            "Report", null,
            OnReportSelected,
            null
        ));
}
```

### Acceptance Criteria

- [ ] Report dialog appears on mission completion
- [ ] Multiple report options based on discoveries
- [ ] Quality calculated from discoveries and skill
- [ ] Lord reacts appropriately to report quality
- [ ] Rewards scaled by quality
- [ ] Army behavior influenced by intel (stretch goal)

---

## Phase 6: Skill Integration

### Behavior

**Scouting Skill Effects:**

| Skill Level | Discovery Radius | Discovery Chance | Detection Avoidance |
|-------------|------------------|------------------|---------------------|
| 0-50 | 30-40 units | 5-10% | Base |
| 50-100 | 40-50 units | 10-15% | +10% |
| 100-150 | 50-60 units | 15-20% | +20% |
| 150-200 | 60-70 units | 20-25% | +30% |
| 200-275 | 70-85 units | 25-35% | +40% |

**Riding Skill Effects:**
- Faster escape speed when being pursued
- Higher skill = more distance gained per check

**Tactics Skill Effects:**
- Better quality intel reports
- +1 quality tier at 100 Tactics
- +2 quality tiers at 200 Tactics

**Roguery Skill Effects (Future):**
- Unlock ability to bribe enemy scouts
- Sabotage missions at high tier

**XP Gains During Recon:**
- Scouting: +5 per discovery, +2 per near-miss
- Tactics: +3 per quality intel report
- Athletics: +1 per hour scouted on foot

### Technical Implementation

**Skill Checks:**
```csharp
private float GetDiscoveryRadius()
{
    var baseRadius = ReconConfig.BaseDiscoveryRadius;
    var scoutingBonus = Hero.MainHero.GetSkillValue(DefaultSkills.Scouting) * 0.2f;
    return baseRadius + scoutingBonus;
}

private float GetEscapeChance()
{
    var baseChance = ReconConfig.BaseEscapeChance;
    var scoutingBonus = Hero.MainHero.GetSkillValue(DefaultSkills.Scouting) * 0.002f;
    return Math.Min(0.8f, baseChance + scoutingBonus);
}
```

### Acceptance Criteria

- [ ] Scouting skill affects discovery radius and chance
- [ ] Riding skill affects escape success
- [ ] Tactics skill affects report quality
- [ ] Skills gain XP from recon activities

---

## Configuration

**recon_config.json (Optional JSON Configuration):**
```json
{
  "schema_version": 1,
  "enabled": true,
  "minimum_tier": 4,
  "one_mission_per_campaign": true,
  "distance_thresholds": {
    "warning": 50,
    "critical": 100,
    "completion": 20
  },
  "trigger_probabilities": {
    "base": 0.05,
    "high_threat": 0.15,
    "enemy_nearby": 0.10,
    "patrolling": 0.07,
    "defending": 0.05,
    "pre_siege": 0.20
  },
  "duration_tiers": [
    { "hours": 2, "sight_bonus": 0.30, "xp_mult": 1.0, "spawn_chance": 0.0 },
    { "hours": 6, "sight_bonus": 0.40, "xp_mult": 1.5, "spawn_chance": 0.10 },
    { "hours": 12, "sight_bonus": 0.50, "xp_mult": 2.0, "spawn_chance": 0.25 },
    { "hours": 999, "sight_bonus": 0.60, "xp_mult": 3.0, "spawn_chance": 0.50 }
  ],
  "scout_troops": {
    "tier_4_count": { "min": 2, "max": 3 },
    "tier_5_count": { "min": 3, "max": 4 },
    "tier_6_count": { "min": 4, "max": 5 },
    "troop_tier_min": 3,
    "troop_tier_max": 4,
    "mounted_only": true
  },
  "enemy_spawns": {
    "despawn_delay_seconds": 30,
    "min_spawn_distance": 20,
    "max_spawn_distance": 40,
    "party_size_min": 4,
    "party_size_max": 8,
    "pursuit_speed": 6.0
  }
}
```

---

## Debugging

**Log Category:** "Recon"

**Key Log Points:**
- Trigger probability calculation with all condition contributions
- State transitions with reason
- Distance checks with current distance
- Discovery events with type and target
- Detection events with enemy and outcome
- Report submission with quality and rewards

**Debug Commands (via ReconDebugHelper):**
```
recon.start           - Force start mission
recon.end [success]   - Force end mission  
recon.state           - Print full state dump
recon.distance        - Print distance to lord
recon.trigger         - Show trigger probability breakdown
recon.discover [type] - Force discovery of type
recon.tier [1-4]      - Set timer tier
recon.detect          - Force enemy detection
recon.scouts          - List assigned scout troops
recon.spawn           - Force spawn enemy patrol
recon.despawn         - Force despawn all spawned enemies
recon.enemies         - List all spawned enemy parties
```

**Common Issues:**

- **Mission not triggering**: Check tier (must be 4+), cooldown, and trigger probability log
- **Sight bonus not applying**: Verify patch is loaded, check army reference
- **Distance not tracking**: Ensure lord party reference is valid
- **Detection too easy/hard**: Adjust terrain/time modifiers in config

---

## Scope boundaries (documentation)
This page documents **shipping behavior** and stable API surfaces for the Recon Mission feature.
Planning/prioritization details belong in `docs/research/` (not here).

