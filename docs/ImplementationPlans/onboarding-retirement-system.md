# Onboarding & Discharge System Implementation Plan

## Overview

A unified system governing how players enter service (onboarding), leave service (discharge), and re-enter service with the same or different factions. The system uses player level as the primary experience metric and tracks per-faction service history to create meaningful progression and consequences.

---

## Development Guidelines

### API Verification
Use the local native decompile at `C:\Dev\Enlisted\Decompile` as the primary reference for Bannerlord APIs. Do not rely on external documentation or AI assumptions about API behavior. When in doubt, read the decompiled source.

### Code Style
Follow ReSharper linter rules and recommendations. Fix lint warnings rather than suppressing them with pragmas.

### Comments
Write comments in plain, natural English that describe what the code does right now. Avoid:
- Changelog-style framing ("Phase 2 addition", "Updated for new system")
- Legacy references ("Previously this was X, now it's Y")
- Robotic or overly technical language
- Dash-bullet formatting in comments

Good: `// Checks if the player can re-enlist with this faction based on cooldown and discharge history.`

Bad: `// Phase 2: Added re-enlistment check. Previously used FactionVeteranRecord, now uses FactionServiceRecord.`

### After Each Phase
Update all affected comments to reflect the current state of the code. Comments should always describe what the code does now, not what it used to do or when it was changed.

## Current Status Summary

| Feature | Status | Notes |
|---------|--------|-------|
| Lord Death Handling | ✅ Complete | `OnHeroKilled()` triggers 14-day grace period |
| Lord Capture Handling | ✅ Complete | `OnHeroPrisonerTaken()` triggers grace period |
| Grace Period System | ✅ Complete | `StartDesertionGracePeriod()` with saved progression |
| Transfer Service | ✅ Complete | `TransferServiceToLord()` preserves tier/XP |
| Discharge Bands | ✅ Complete | veteran/honorable/washout/deserter/grace tracked |
| Reservist Re-entry | ✅ Complete | `TryConsumeReservistForFaction()` with tier/XP bonuses |
| Faction Cooldown | ✅ Complete | `FactionVeteranRecord.CooldownEnds` |
| Baggage Stash | ✅ Complete | `_baggageStash` ItemRoster exists |
| Discipline Events | ✅ Content exists | Full event chain from 3→5→7→10 threshold |
| Discipline Discharge Event | ⚠️ Event only | Event shows but doesn't trigger actual discharge |
| 90-Day Block | ❌ Missing | No `ReenlistmentBlockedUntil` per faction |
| Experience Tracks | ❌ Missing | No player-level-based starting tier |
| Baggage Faction Tracking | ❌ Missing | No cross-faction transfer prompt |
| Data Consolidation | ❌ Missing | Two parallel records exist |

---

## Native Game Reality

Lords in Bannerlord do NOT retire. They only have these states:
- **Active** - leading their party normally
- **Prisoner** - captured, held by enemy
- **Released/Fugitive** - just freed, temporarily without party
- **Dead** - permanent (old age, battle, execution, wounds)

When a lord's party is destroyed, they become fugitive/released, then the game's `HeroSpawnCampaignBehavior` eventually spawns a new party for them.

For our mod, "player discharge" means the player leaving the lord's service. The lord continues existing (unless dead).

---

## Core Concepts

### Experience Tracks

Player level determines the onboarding experience track:

| Track | Player Level | Starting Tier | Description |
|-------|--------------|---------------|-------------|
| Green | < 10 | T1 | New to military life, full boot camp |
| Seasoned | 10-20 | T2 | Knows the basics, abbreviated onboarding |
| Veteran | 21+ | T3 | Proven fighter, minimal formalities |

### Discharge Bands

How service ends determines future treatment:

| Band | Trigger | Cooldown | Relations | Re-entry Treatment |
|------|---------|----------|-----------|-------------------|
| `veteran` | 200+ days, T4+ | 0 days | +30 lord, +15 faction | Full benefits |
| `honorable` | 100-199 days, neutral+ relations | 0 days | +10 lord, +5 faction | Good standing |
| `washout` | < 100 days OR negative relations | 30 days | -10 all | Probation on return |
| `dishonorable` | Discipline reaches max (10) | 90 days | -20 all | Severe penalties |
| `deserter` | Abandoned service | 90 days | -30 all, crime +30 | Disgraced track |
| `grace` | Lord died/captured | 0 days | None | Preserves status |

### Escalation Discipline Scale

**Note:** Discipline uses a 0-10 scale, not 0-100.

| Threshold | Value | Event |
|-----------|-------|-------|
| Extra Duty | 3 | `discipline_extra_duty` |
| Hearing | 5 | `discipline_hearing` |
| Blocked | 7 | `discipline_blocked` |
| Discharge | 10 | `discipline_discharge` |

---

## Gap Analysis & Implementation Tasks

### Phase 1: Fix Discipline Discharge (HIGH PRIORITY)

**Problem:** The `discipline_discharge` event shows great narrative content but doesn't actually trigger a discharge.

**Solution:** Add `TriggersDischarge` effect to EventEffects system.

```csharp
// Add to EventEffects.cs
public string TriggersDischarge { get; set; }  // "dishonorable", "washout", etc.
```

```csharp
// Add to EventDeliveryManager.ApplyEffects()
if (!string.IsNullOrEmpty(effects.TriggersDischarge))
{
    EnlistmentBehavior.Instance?.StopEnlist(
        $"Discharged: {effects.TriggersDischarge}", 
        isHonorableDischarge: false);
}
```

Update `events_escalation_thresholds.json` options:
- "accept" option: `"triggers_discharge": "dishonorable"`
- "defiant" option: `"triggers_discharge": "dishonorable"`
- "beg" failure: `"triggers_discharge": "dishonorable"`
- "service" failure: `"triggers_discharge": "dishonorable"`

**Tasks:**
- [ ] Add `TriggersDischarge` property to `EventEffects`
- [ ] Add discharge handling to `ApplyEffects()`
- [ ] Update discipline_discharge event options in JSON
- [ ] Set `ReenlistmentBlockedUntil` for 90 days on dishonorable

---

### Phase 2: Add 90-Day Re-enlistment Block (HIGH PRIORITY)

**Problem:** Bad discharges don't block re-enlistment with the faction.

**Solution:** Add block tracking to consolidated record.

```csharp
// Add to FactionServiceRecord.cs
public CampaignTime ReenlistmentBlockedUntil { get; set; } = CampaignTime.Zero;
public string LastDischargeBand { get; set; } = string.Empty;
```

**Logic:**
- On dishonorable/deserter discharge → set block for 90 days
- On washout → set block for 30 days
- On honorable/veteran → no block
- Check block at enlistment start, show message if blocked

**Tasks:**
- [ ] Add `ReenlistmentBlockedUntil` to `FactionServiceRecord`
- [ ] Set block in `StopEnlist()` based on discharge band
- [ ] Check block in `ContinueStartEnlistInternal()`
- [ ] Show UI message when blocked

---

### Phase 3: Consolidate Data Systems (HIGH PRIORITY)

**Problem:** Two parallel record systems tracking overlapping data:
1. `FactionVeteranRecord` in EnlistmentBehavior (FirstTermCompleted, PreservedTier, CooldownEnds, etc.)
2. `FactionServiceRecord` in ServiceRecordManager (HighestTier, TotalDaysServed, etc.)

**Solution:** Merge into single `FactionServiceRecord`.

Add to `FactionServiceRecord`:
```csharp
// Migration from FactionVeteranRecord
public bool FirstTermCompleted { get; set; }
public int PreservedTier { get; set; } = 1;
public CampaignTime CooldownEnds { get; set; } = CampaignTime.Zero;
public CampaignTime CurrentTermEnd { get; set; } = CampaignTime.Zero;
public bool IsInRenewalTerm { get; set; }
public int RenewalTermsCompleted { get; set; }

// New fields
public CampaignTime ReenlistmentBlockedUntil { get; set; } = CampaignTime.Zero;
public string LastDischargeBand { get; set; } = string.Empty;
public int OfficerRepAtExit { get; set; }
public int SoldierRepAtExit { get; set; }
```

**Tasks:**
- [ ] Add fields to `FactionServiceRecord`
- [ ] Update `ServiceRecordManager.SyncData()` to serialize new fields
- [ ] Refactor `EnlistmentBehavior` to use `ServiceRecordManager.Instance.GetOrCreateRecord()`
- [ ] Delete `FactionVeteranRecord` class
- [ ] Delete `_veteranRecords` dictionary from EnlistmentBehavior
- [ ] Add migration logic for existing saves

---

### Phase 4: Experience Track System (MEDIUM PRIORITY)

**Problem:** Player level doesn't affect starting tier.

**Solution:** Add experience track calculation at enlistment.

```csharp
public static string GetExperienceTrack()
{
    var level = Hero.MainHero.Level;
    if (level < 10) return "green";
    if (level <= 20) return "seasoned";
    return "veteran";
}

public static int GetStartingTierForTrack(string track, FactionServiceRecord record)
{
    var experienceTier = track switch
    {
        "green" => 1,
        "seasoned" => 2,
        "veteran" => 3,
        _ => 1
    };
    
    // Previous faction service can boost tier
    var factionTier = record != null && record.HighestTier > 0 
        ? Math.Max(1, record.HighestTier - 2) 
        : 1;
    
    return Math.Max(experienceTier, factionTier);
}
```

**Tasks:**
- [ ] Add `GetExperienceTrack()` method
- [ ] Add `GetStartingTierForTrack()` method
- [ ] Call in `ContinueStartEnlistInternal()` to set initial tier
- [ ] Show track notification to player

---

### Phase 5: Rep Snapshot on Discharge (MEDIUM PRIORITY)

**Problem:** Rep isn't saved per-faction for restoration on re-entry.

**Tasks:**
- [ ] Save `OfficerRepAtExit` and `SoldierRepAtExit` to FactionServiceRecord in `StopEnlist()`
- [ ] Restore partial rep based on discharge band in `TryConsumeReservistForFaction()`

```csharp
// In StopEnlist(), before clearing state:
var record = ServiceRecordManager.Instance.GetOrCreateRecord(faction);
record.OfficerRepAtExit = EscalationManager.Instance?.State.OfficerReputation ?? 0;
record.SoldierRepAtExit = EscalationManager.Instance?.State.SoldierReputation ?? 0;
record.LastDischargeBand = band;
```

---

### Phase 6: Baggage Faction Tracking (LOW PRIORITY)

**Problem:** Baggage stash isn't tagged with faction, no cross-faction transfer prompt.

**Tasks:**
- [ ] Add `_baggageStashFactionId` field
- [ ] Set faction ID when items stashed during bag check
- [ ] Check on enlistment start - if faction differs and stash not empty, show transfer prompt
- [ ] Implement courier/sell options

---

### Phase 7: Onboarding Event Pacing (LOW PRIORITY)

This can be deferred. The current system works without track-based onboarding.

---

## Already Working (No Changes Needed)

### Lord Death Handling
```csharp:7832:7862:src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs
private void OnHeroKilled(Hero victim, Hero killer, ...)
{
    if (IsEnlisted && victim == _enlistedLord)
    {
        // Starts 14-day grace period
        StopEnlist("Lord killed in battle", false, true);
        StartDesertionGracePeriod(lordKingdom);
    }
}
```

### Lord Capture Handling
```csharp:8003:8060:src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs
private void OnHeroPrisonerTaken(PartyBase capturingParty, Hero prisoner)
{
    // Handles both player capture and lord capture
    // Starts grace period for both scenarios
}
```

### Grace Period System
```csharp:3124:3150:src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs
private void StartDesertionGracePeriod(Kingdom kingdom)
{
    // Saves tier, XP, troop selection for restoration
    // Sets 14-day countdown
}
```

### Reservist Re-entry Bonuses
```csharp:333:405:src/Features/Retinue/Core/ServiceRecordManager.cs
public bool TryConsumeReservistForFaction(IFaction faction, out int targetTier, ...)
{
    // Returns tier/XP/relation bonuses based on discharge band
    // "grace" band restores previous tier
    // "veteran" gives T4 + 1000 XP
    // "honorable" gives T3 + 500 XP
    // "washout"/"deserter" gives T1 with probation
}
```

### Discipline Event Chain
Already exists in `events_escalation_thresholds.json`:
- `discipline_extra_duty` (threshold 3)
- `discipline_hearing` (threshold 5)
- `discipline_blocked` (threshold 7)
- `discipline_discharge` (threshold 10) - needs effect wiring

---

## Configuration

Already in `retirement_config.json`, verify these exist:

```json
{
  "CooldownDays": 180,
  "FirstTermDays": 90,
  "RenewalTermDays": 60,
  "ProbationDays": 30
}
```

Add:
```json
{
  "dischargeCooldowns": {
    "washout": 30,
    "dishonorable": 90,
    "deserter": 90
  },
  "experienceTrackLevels": {
    "greenMax": 9,
    "seasonedMax": 20
  }
}
```

---

## Implementation Order

1. **Phase 1: Discipline Discharge Effect** - Wire up the event to actually discharge
2. **Phase 2: 90-Day Block** - Enforce re-enlistment block for bad discharges
3. **Phase 3: Data Consolidation** - Merge FactionVeteranRecord into FactionServiceRecord
4. **Phase 4: Experience Tracks** - Player level affects starting tier
5. **Phase 5: Rep Snapshots** - Save/restore rep per faction
6. **Phase 6: Baggage Tracking** - Cross-faction transfer prompt
7. **Phase 7: Onboarding Pacing** - Future enhancement

---

## Acceptance Criteria

1. **Discipline Discharge Works**: Discipline 10 → event → accept/fail → player actually discharged with "dishonorable" band
2. **90-Day Block Enforced**: After dishonorable discharge, can't re-enlist with that faction for 90 days
3. **Data Consolidated**: Single `FactionServiceRecord` contains all per-faction data
4. **Experience Track Affects Tier**: Level 5 starts T1, Level 15 starts T2, Level 25 starts T3
5. **Faction Memory**: Player who reached T5 returns at T3 (HighestTier - 2)
6. **Rep Restoration**: Veteran returning gets partial rep based on discharge band

---

## Edge Cases (Already Handled)

| Case | Current Behavior |
|------|------------------|
| Lord dies in battle | Grace period started, player can transfer |
| Lord captured | Grace period started, player can transfer or wait |
| Player captured during service | Grace period started after escape |
| Army dispersed | Checks if lord still alive, handles accordingly |
| Lord has no kingdom | Immediate discharge without penalties |
| Grace period expires | Auto-discharge as deserter |
| Same-faction transfer | `TransferServiceToLord()` preserves progression |

---

## Dependencies

- **EventDeliveryManager**: Add `TriggersDischarge` effect
- **ServiceRecordManager**: Serialize new FactionServiceRecord fields
- **EnlistmentBehavior**: Use consolidated records

## Target Version

This project targets **Bannerlord v1.3.11**. Verify all API calls against the local decompile.

## Related Documents

- `docs/Features/Core/core-gameplay.md` - Core mechanics reference
- `docs/StoryBlocks/event-catalog-by-system.md` - Event content index
- `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs` - Main implementation file
- `src/Features/Retinue/Core/ServiceRecordManager.cs` - Service record manager

## Native Reference Files

Key decompiled files for API verification:

| Native File | Purpose |
|-------------|---------|
| `Decompile/TaleWorlds.CampaignSystem/Actions/KillCharacterAction.cs` | Hero death mechanics |
| `Decompile/TaleWorlds.CampaignSystem/Actions/EndCaptivityAction.cs` | Prisoner release mechanics |
| `Decompile/TaleWorlds.CampaignSystem/CampaignBehaviors/HeroSpawnCampaignBehavior.cs` | Lord party reformation |
| `Decompile/TaleWorlds.CampaignSystem/Hero.cs` | Hero states (CharacterStates enum) |

## Phase Completion Checklist

After completing each phase:

- [ ] All new code follows ReSharper recommendations
- [ ] Comments describe current behavior (no changelog language)
- [ ] Existing comments updated if behavior changed
- [ ] Build passes with 0 warnings
- [ ] Tested in-game for the specific scenario
