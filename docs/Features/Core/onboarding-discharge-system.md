# Onboarding & Discharge System

**Summary:** The onboarding and discharge system governs how players enter service, progress through experience tracks, build faction service records, and leave service with consequences that affect future re-enlistment. The system uses player level to determine starting experience track (green/seasoned/veteran) and discharge band (veteran/honorable/washout/dishonorable/deserter/grace) to control re-enlistment cooldowns and relationship impacts.

**Status:** ✅ Current  
**Last Updated:** 2025-12-22  
**Related Docs:** [Enlistment](enlistment.md), [Retinue System](retinue-system.md)

---

## Index

1. [Overview](#overview)
2. [Experience Tracks](#experience-tracks)
3. [Onboarding Events](#onboarding-events)
4. [Service Records](#service-records)
5. [Discharge Bands](#discharge-bands)
6. [Re-Enlistment System](#re-enlistment-system)
7. [Lord Death & Grace Period](#lord-death--grace-period)
8. [Discipline Discharge](#discipline-discharge)
9. [Baggage Cross-Faction Transfer](#baggage-cross-faction-transfer)
10. [Reputation Restoration](#reputation-restoration)
11. [Implementation Details](#implementation-details)

---

## Overview

The system manages the complete lifecycle of military service:

**On Enlistment:**
- Determine experience track based on player level (green/seasoned/veteran)
- Set starting tier (T1-T3) and training XP modifier
- Apply faction history bonuses if returning reservist
- Initialize onboarding event stage
- Handle cross-faction baggage transfer if needed

**During Service:**
- Deliver track-specific onboarding events (stage 1→2→3→complete)
- Track service metrics (days served, battles fought, highest tier reached)
- Monitor discipline escalation
- Maintain per-faction service record

**On Discharge:**
- Determine discharge band based on service length, relations, and circumstances
- Save officer and soldier reputation snapshots
- Set re-enlistment cooldown (0-90 days)
- Reclaim quartermaster-issued equipment
- Record reservist status for future benefits

**On Re-Enlistment:**
- Check re-enlistment block and cooldown
- Apply reservist bonuses (tier boost, XP grant, reputation restoration)
- Restore partial reputation based on discharge band

---

## Experience Tracks

### Track Determination

Player level at enlistment determines the experience track:

| Track | Player Level | Description |
|-------|--------------|-------------|
| **Green** | 1-9 | New to military life |
| **Seasoned** | 10-20 | Knows the basics |
| **Veteran** | 21+ | Proven fighter |

### Starting Tier by Track

Base starting tier is determined by experience track:

| Track | Base Tier | Training XP Modifier |
|-------|-----------|---------------------|
| Green | T1 | +20% (learns quickly) |
| Seasoned | T2 | Normal |
| Veteran | T3 | -10% (diminishing returns) |

**Faction History Bonus:**
- If returning to same faction with good service record: `HighestTier - 2` (capped at T3)
- Bad discharge (washout/dishonorable/deserter): No bonus
- Grace period discharge: Full tier restoration

**Example Calculations:**

```
Level 5 Player, First Enlistment:
  Track: Green → Base Tier: T1 → Final: T1

Level 15 Player, First Enlistment:
  Track: Seasoned → Base Tier: T2 → Final: T2

Level 25 Player, First Enlistment:
  Track: Veteran → Base Tier: T3 → Final: T3

Level 25 Player, Returning (Reached T5 Previously, Honorable Discharge):
  Track: Veteran → Base Tier: T3 → Faction Bonus: T5 - 2 = T3 → Final: T3

Level 25 Player, Returning as Reservist (Veteran Discharge):
  Track: Veteran → Base Tier: T3 → Reservist Bonus: T4 → Final: T4
```

### Training XP Modifier

The training XP modifier affects experience gains from training events and drills:

```csharp
public static float GetTrainingXpModifier(string experienceTrack)
{
    return experienceTrack switch
    {
        "green" => 1.2f,      // +20% from training
        "seasoned" => 1.0f,   // Normal
        "veteran" => 0.9f,    // -10% (diminishing returns)
        _ => 1.0f
    };
}
```

**Design Rationale:**
- New soldiers benefit most from structured training
- Experienced veterans learn more from combat than drills
- Encourages diverse playstyles based on player level

---

## Onboarding Events

### Stage Progression

New enlistees receive track-specific onboarding events that fire in sequence:

```
Stage 1 → Stage 2 → Stage 3 → Complete
```

Each stage delivers events tailored to the player's experience track (green/seasoned/veteran).

### Onboarding State

The system tracks three pieces of onboarding state:

| Field | Type | Purpose |
|-------|------|---------|
| `OnboardingStage` | int | Current stage (0 = complete/inactive, 1-3 = active) |
| `OnboardingTrack` | string | Experience track ("green", "seasoned", "veteran") |
| `OnboardingStartTime` | CampaignTime | When onboarding began (for pacing) |

### Event Requirements

Onboarding events use these requirements to gate delivery:

```json
{
  "requirements": {
    "onboarding_stage": 1,
    "onboarding_track": "green"
  }
}
```

Only events matching the current stage and track will fire.

### Stage Advancement

Events advance to the next stage via the `advances_onboarding` flag:

```json
{
  "option_id": "accept",
  "advances_onboarding": true
}
```

When selected, the option increments `OnboardingStage` (1→2, 2→3, 3→complete).

### Onboarding Lifecycle

**Initialization:**
```csharp
// At enlistment start
string track = ExperienceTrackHelper.GetExperienceTrack(Hero.MainHero.Level);
EscalationManager.Instance.InitializeOnboarding(track);
```

**During Service:**
- Event system checks `MeetsOnboardingRequirements()` before delivery
- Events fire in stage order (won't skip stages)
- Player choices advance the stage

**Termination:**
- Stage reaches 0 (complete) or player discharges
- Reset on discharge (not preserved during grace period)
- Re-initialized on next full enlistment

### Example Event Flow

**Green Track (Level 5 Player):**

```
Stage 1: "First Day in Camp"
  - Meet your squad mates
  - Learn about equipment
  - Option: [Advance]

Stage 2: "First Duty Assignment"
  - Quartermaster explains duties
  - Discipline system introduction
  - Option: [Advance]

Stage 3: "First Muster"
  - Pay and ration system explained
  - Reputation basics
  - Option: [Advance] → Complete

Onboarding Complete
```

**Veteran Track (Level 25 Player):**

```
Stage 1: "Old Soldier, New Commander"
  - Brief tactical discussion
  - Respect earned quickly
  - Option: [Advance]

Stage 2: "Reputation Precedes You"
  - Other soldiers know your name
  - Expectations are high
  - Option: [Advance]

Stage 3: "Officer Material"
  - Lord mentions promotion track
  - Leadership opportunities
  - Option: [Advance] → Complete

Onboarding Complete
```

---

## Service Records

### Faction Service Record

Each faction maintains a persistent service record tracking:

**Service History:**
```csharp
public int TermsCompleted;      // Number of completed terms
public int TotalDaysServed;     // Total days across all terms
public int HighestTier;         // Highest rank reached
public int BattlesFought;       // Battles participated in
public int LordsServed;         // Number of different lords served
public int Enlistments;         // Total enlistments with faction
public int TotalKills;          // Combat kills
```

**Re-Enlistment Control:**
```csharp
public CampaignTime ReenlistmentBlockedUntil; // Block date
public string LastDischargeBand;               // veteran/honorable/etc.
public int OfficerRepAtExit;                   // Officer rep snapshot
public int SoldierRepAtExit;                   // Soldier rep snapshot
```

**Term Tracking:**
```csharp
public bool FirstTermCompleted;    // First term milestone
public int PreservedTier;          // Tier saved for grace period
public CampaignTime CooldownEnds;  // Re-enlistment cooldown
public CampaignTime CurrentTermEnd; // Current term expiry
public bool IsInRenewalTerm;       // Renewal vs. first term
public int RenewalTermsCompleted;  // Renewal count
```

### Record Updates

**On Enlistment Start:**
```csharp
record.Enlistments++;
record.LordsServed++; // If new lord
```

**During Service:**
```csharp
record.TotalDaysServed++; // Daily
record.BattlesFought++;   // Per battle
record.TotalKills += kills; // Combat
record.HighestTier = Math.Max(record.HighestTier, currentTier);
```

**On Discharge:**
```csharp
record.LastDischargeBand = dischargeBand;
record.OfficerRepAtExit = currentOfficerRep;
record.SoldierRepAtExit = currentSoldierRep;
record.ReenlistmentBlockedUntil = CalculateBlockEnd(dischargeBand);
record.CooldownEnds = CampaignTime.DaysFromNow(180);
```

---

## Discharge Bands

### Band Determination

Discharge band is calculated based on service metrics and circumstances:

| Band | Trigger | Cooldown | Relations | Description |
|------|---------|----------|-----------|-------------|
| **veteran** | 200+ days, T4+ | 0 days | +30 lord, +15 faction | Outstanding service |
| **honorable** | 100-199 days, neutral+ relations | 0 days | +10 lord, +5 faction | Good service |
| **washout** | < 100 days OR negative relations | 30 days | -10 all | Didn't work out |
| **dishonorable** | Discipline = 10 (discharge event) | 90 days | -20 all | Severe misconduct |
| **deserter** | Abandoned service | 90 days | -30 all, crime +30 | Disgraced |
| **grace** | Lord died/captured | 0 days | None | Not player's fault |

### Band-Specific Effects

**Veteran:**
- Best re-entry benefits (T4 start, +1000 XP, 75% rep restoration)
- +30 relationship with enlisted lord
- +15 relationship with faction nobles
- No re-enlistment block
- Full faction history bonus

**Honorable:**
- Good re-entry benefits (T3 start, +500 XP, 50% rep restoration)
- +10 relationship with enlisted lord
- +5 relationship with faction nobles
- No re-enlistment block
- Full faction history bonus

**Washout:**
- Poor re-entry (T1 start, probation status)
- -10 relationship with enlisted lord and faction
- 30-day re-enlistment block
- No faction history bonus

**Dishonorable:**
- Severe penalties (T1 start, probation, scrutiny)
- -20 relationship with enlisted lord and faction
- 90-day re-enlistment block
- No faction history bonus
- Mentioned in service record (reputation hit)

**Deserter:**
- Worst penalties (T1 start, criminal record)
- -30 relationship with all faction nobles
- +30 crime rating (may be arrested on sight)
- 90-day re-enlistment block
- No faction history bonus

**Grace:**
- Special case (lord died or captured)
- No relationship changes
- No re-enlistment block
- Full tier/XP restoration (not player's fault)
- 100% reputation restoration
- Same-faction transfer preserves all progress

### Relationship Changes

Applied to:

| Target | Veteran | Honorable | Washout | Dishonorable | Deserter |
|--------|---------|-----------|---------|--------------|----------|
| **Enlisted Lord** | +30 | +10 | -10 | -20 | -30 |
| **Faction Lords** | +15 | +5 | -10 | -20 | -30 |
| **Crime Rating** | 0 | 0 | 0 | 0 | +30 |

---

## Re-Enlistment System

### Re-Enlistment Check

Before allowing enlistment, the system checks:

```csharp
public bool CanEnlistWithFaction(IFaction faction)
{
    var record = ServiceRecordManager.Instance.GetOrCreateRecord(faction);
    
    // Check re-enlistment block
    if (record.ReenlistmentBlockedUntil != null && 
        CampaignTime.Now < record.ReenlistmentBlockedUntil)
    {
        int daysRemaining = (int)(record.ReenlistmentBlockedUntil - CampaignTime.Now).ToDays;
        ShowBlockedMessage(faction, daysRemaining);
        return false;
    }
    
    return true;
}
```

### Block Durations

| Discharge Band | Block Duration | Can Re-Enlist After |
|----------------|----------------|---------------------|
| veteran | 0 days | Immediately |
| honorable | 0 days | Immediately |
| washout | 30 days | 1 month |
| dishonorable | 90 days | 3 months |
| deserter | 90 days | 3 months |
| grace | 0 days | Immediately |

### Reservist Re-Entry

If the player has a service record with the faction, they receive reservist bonuses:

**Veteran Discharge:**
```
Starting Tier: T4 (HighestTier, capped at T4)
XP Grant: +1000 XP
Reputation Restore: 75% of saved rep
Message: "The quartermaster welcomes you back warmly."
```

**Honorable Discharge:**
```
Starting Tier: T3 (HighestTier - 1, capped at T3)
XP Grant: +500 XP
Reputation Restore: 50% of saved rep
Message: "You're remembered as a reliable soldier."
```

**Grace Period (Same Faction):**
```
Starting Tier: Preserved tier (full restoration)
XP Grant: Full XP restoration
Reputation Restore: 100% of saved rep
Message: "Your previous service is fully recognized."
```

**Washout/Dishonorable/Deserter:**
```
Starting Tier: T1 (probation)
XP Grant: 0
Reputation Restore: 0%
Probation: Active (close scrutiny)
Message: "You're being given another chance. Don't waste it."
```

### Reputation Restoration

Saved reputation is partially restored based on discharge band:

| Band | Officer Rep Restore | Soldier Rep Restore |
|------|---------------------|---------------------|
| veteran | 75% (3/4) | 75% (3/4) |
| honorable | 50% (1/2) | 50% (1/2) |
| grace | 100% (full) | 100% (full) |
| washout | 0% | 0% |
| dishonorable | 0% | 0% |
| deserter | 0% | 0% |

**Example Calculation:**
```
Previous Service:
  Officer Rep at Exit: 80
  Soldier Rep at Exit: 40
  Discharge Band: honorable

Re-Enlistment:
  Officer Rep Restored: 80 × 50% = 40
  Soldier Rep Restored: 40 × 50% = 20
  
Message: "Your previous reputation with officers (40) and soldiers (20) is partially restored."
```

---

## Lord Death & Grace Period

### Grace Period Activation

When the enlisted lord dies or is captured, a **14-day grace period** begins:

```csharp
private void OnHeroKilled(Hero victim, Hero killer, ...)
{
    if (IsEnlisted && victim == _enlistedLord)
    {
        StopEnlist("Lord killed in battle", preserveState: true, isGrace: true);
        StartDesertionGracePeriod(lordKingdom);
    }
}
```

### Grace Period State

During the grace period, the system preserves:

```csharp
private void StartDesertionGracePeriod(Kingdom kingdom)
{
    _pendingDesertionKingdom = kingdom;
    _pendingDesertionDay = CampaignTime.Now;
    _pendingDesertionTier = _playerTier;
    _pendingDesertionXp = _progressionXp;
    _pendingDesertionTroopSelection = _currentTroopSelection;
    _pendingDesertionGraceDays = 14;
}
```

### Grace Period Options

**Within 14 Days:**

1. **Transfer to Another Lord (Same Faction)**
   - Full tier/XP preservation
   - No discharge recorded
   - No cooldown
   - Continue service seamlessly

2. **Wait for Lord Release**
   - If captured, lord may be released/escape
   - Grace period continues
   - Can wait out capture

3. **Leave Service**
   - Voluntary discharge
   - Grace band applied (no penalties)
   - 100% reputation restoration on return
   - Can re-enlist immediately

**After 14 Days:**
- Automatic discharge as "deserter"
- 90-day re-enlistment block
- -30 relationship penalties
- +30 crime rating

### Grace Period Transfer

```
Event: "Your Lord Has Fallen"

Your lord, [Lord Name], has been killed in battle. You are no longer 
bound to service.

You have 14 days to find a new lord in the [Kingdom] or you will be 
marked as a deserter.

Options:
  [1] Seek Another Lord (transfer service, preserves progress)
  [2] Leave Service (grace discharge, no penalties)
  [3] Wait and See (continue grace period)
```

---

## Discipline Discharge

### Discipline Scale

The discipline system uses a 0-10 scale with escalation thresholds:

| Threshold | Value | Event | Effect |
|-----------|-------|-------|--------|
| Extra Duty | 3 | `discipline_extra_duty` | Warning, extra chores |
| Hearing | 5 | `discipline_hearing` | Formal reprimand |
| Blocked | 7 | `discipline_blocked` | Duties restricted |
| Discharge | 10 | `discipline_discharge` | Kicked out |

### Discharge Event

When discipline reaches 10, the `discipline_discharge` event fires:

```
Event: "Disciplinary Discharge"

You are summoned before the commander. Your conduct has been 
unacceptable, and you are being discharged from service.

Options:
  [1] Accept Discharge → Immediate dishonorable discharge
  [2] Plead Your Case (Charm 50+) → 25% stay (penal detail), 75% discharge
  [3] Offer Extended Service → 40% stay (demotion to T1), 60% discharge
  [4] Defiantly Refuse → Discharge + extra reputation penalty
```

### Risky Options

Options with success/failure outcomes use the risky option system:

```json
{
  "option_id": "plead",
  "risk_chance": 0.25,
  "effects_success": {
    "discipline": -3,
    "officer_rep": -10,
    "text": "The commander reluctantly agrees to one more chance."
  },
  "effects_failure": {
    "triggers_discharge": true,
    "officer_rep": -20,
    "text": "Your plea falls on deaf ears."
  }
}
```

### Discharge Effect

When an option has `triggers_discharge: true`, the system:

1. Calls `StopEnlist()` with discharge band "dishonorable"
2. Sets `ReenlistmentBlockedUntil` to 90 days from now
3. Applies -20 relationship penalty to lord and faction
4. Saves service record with discharge band
5. Ends enlistment immediately

---

## Baggage Cross-Faction Transfer

### Cross-Faction Detection

When enlisting with a new faction, the system checks for cross-faction baggage:

```csharp
private bool HasCrossFactionBaggage(IFaction newFaction)
{
    if (_baggageStash.Count == 0) return false;
    
    string baggageFaction = _baggageStashFactionId;
    string newFactionId = newFaction.StringId;
    
    // Special case: grace period within same kingdom
    if (_desertionGracePeriodActive && 
        _pendingDesertionKingdom == newFaction.MapFaction as Kingdom)
    {
        return false; // Skip prompt
    }
    
    return !string.IsNullOrEmpty(baggageFaction) && 
           baggageFaction != newFactionId;
}
```

### Transfer Prompt

If cross-faction baggage detected:

```
Event: "Baggage from Previous Service"

You have equipment stored with your previous faction ([Old Faction]).

What do you want to do with it?

Options:
  [1] Arrange Courier Delivery
      Cost: 50g + 5% of item value
      Delivery: 3 in-game days
      
  [2] Sell Remotely
      Value: 40% of item worth (remote sale penalty)
      Gold received immediately
      
  [3] Abandon It
      Items lost permanently
      No cost
      
  [4] Cancel Enlistment
      Return to considering options
```

### Courier System

**Courier Delivery:**
```csharp
_pendingCourierBaggage = _baggageStash.ToList();
_baggageCourierArrivalTime = CampaignTime.HoursFromNow(72); // 3 days
_baggageStash.Clear();

int courierCost = 50 + (int)(totalValue * 0.05f);
GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, courierCost);
```

**Courier Arrival:**
```csharp
// Hourly check
if (_pendingCourierBaggage != null && 
    CampaignTime.Now >= _baggageCourierArrivalTime)
{
    DeliverCourierBaggage();
    PostPersonalDispatchText(
        "Baggage Delivered",
        "Your belongings from previous service have arrived.",
        "logistics"
    );
}
```

**Edge Cases:**
- Courier arrives while player is prisoner: Defer delivery
- Courier arrives while party is null: Defer delivery
- Player discharges during transit: Deliver immediately
- Multiple cross-faction enlistments: Merge items into existing courier

### Remote Sale

```csharp
int saleValue = (int)(totalValue * 0.4f); // 40% of value
GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, saleValue);
_baggageStash.Clear();
_baggageStashFactionId = null;
```

**Note:** Remote sale rate (40%) is worse than normal baggage sale (60%) to incentivize courier or in-person retrieval.

---

## Reputation Restoration

### Reputation Snapshot

At discharge, the system saves current reputation levels:

```csharp
private void StopEnlist(string reason, bool preserveState = false, bool isGrace = false)
{
    // Save reputation BEFORE clearing state
    var escalationState = EscalationManager.Instance?.State;
    if (escalationState != null)
    {
        record.OfficerRepAtExit = escalationState.OfficerRep;
        record.SoldierRepAtExit = escalationState.SoldierRep;
    }
    
    // Continue with discharge...
}
```

### Restoration Calculation

At re-enlistment, reputation is partially restored:

```csharp
public bool TryConsumeReservistForFaction(IFaction faction, 
    out int restoredOfficerRep, 
    out int restoredSoldierRep)
{
    var record = GetOrCreateRecord(faction);
    
    float restorePercent = record.LastDischargeBand switch
    {
        "veteran" => 0.75f,   // 75%
        "honorable" => 0.50f, // 50%
        "grace" => 1.00f,     // 100%
        _ => 0.0f             // 0%
    };
    
    restoredOfficerRep = (int)(record.OfficerRepAtExit * restorePercent);
    restoredSoldierRep = (int)(record.SoldierRepAtExit * restorePercent);
    
    return true;
}
```

### Restoration Application

```csharp
private void TryApplyReservistReentryBoost(IFaction faction)
{
    if (ServiceRecordManager.Instance.TryConsumeReservistForFaction(
        faction, 
        out int officerRep, 
        out int soldierRep))
    {
        var escalationState = EscalationManager.Instance?.State;
        if (escalationState != null && (officerRep > 0 || soldierRep != 0))
        {
            escalationState.OfficerRep += officerRep;
            escalationState.SoldierRep += soldierRep;
            escalationState.ClampAll(); // Ensure valid ranges
            
            ShowReputationRestorationNotification(officerRep, soldierRep);
        }
    }
}
```

### Reputation Display

```
Notification: "Service Record Recognized"

Your previous service with [Faction] is remembered.

Officer Reputation Restored: +40 (from 80, 50% of previous)
Soldier Reputation Restored: +20 (from 40, 50% of previous)

Your standing with the company reflects your past service.
```

---

## Implementation Details

### Data Persistence

All service record data is saved in the campaign save file via:

```csharp
public override void SyncData(IDataStore dataStore)
{
    dataStore.SyncData("enlisted_faction_service_records", ref _factionServiceRecords);
}
```

The `FactionServiceRecord` class is serializable and persists across save/load.

### State Validation

On load, the system validates state integrity:

```csharp
private void ValidateLoadedState()
{
    // Validate onboarding state
    if (State.OnboardingStage > 3 || State.OnboardingStage < 0)
        State.OnboardingStage = 0;
    
    if (State.OnboardingStage > 0 && string.IsNullOrEmpty(State.OnboardingTrack))
        State.OnboardingStage = 0; // Reset corrupted state
    
    // Validate reputation ranges
    State.ClampAll();
}
```

### Event Integration

The system integrates with the event delivery system via:

- `MeetsOnboardingRequirements()` in `EventRequirementChecker`
- `ApplyFlagChanges()` in `EventDeliveryManager` for `advances_onboarding`
- `ApplyDischargeEffect()` for `triggers_discharge` flag
- Risky option resolution for success/failure branches

### Source Files

| File | Purpose |
|------|---------|
| `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs` | Main enlistment logic, grace period, baggage transfer |
| `src/Features/Retinue/Core/ServiceRecordManager.cs` | Faction service record storage and serialization |
| `src/Features/Retinue/Data/FactionServiceRecord.cs` | Per-faction data structure |
| `src/Features/Content/ExperienceTrackHelper.cs` | Experience track calculation, starting tier |
| `src/Features/Content/EventDeliveryManager.cs` | Event delivery, discharge effects, risky options |
| `src/Features/Escalation/EscalationManager.cs` | Onboarding state, reputation management |
| `ModuleData/Enlisted/Events/events_onboarding.json` | Track-specific onboarding events |
| `ModuleData/Enlisted/Events/events_escalation_thresholds.json` | Discipline discharge event |

---

**End of Document**

