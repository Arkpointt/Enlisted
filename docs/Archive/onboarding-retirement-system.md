# Onboarding & Discharge System Implementation Plan

## Overview

A unified system governing how players enter service (onboarding), leave service (discharge), and re-enter service with the same or different factions. The system uses player level as the primary experience metric and tracks per-faction service history to create meaningful progression and consequences.

---

## Engineering Standards

**Follow these while implementing all phases** (same as unified content system):

### Code Quality
- **Follow ReSharper linter/recommendations.** Fix warnings; don't suppress them with pragmas.
- **Comments should be factual descriptions of current behavior.** Write them as a human developer would—professional and natural. Don't use "Phase" references, changelog-style framing ("Added X", "Changed from Y"), or mention legacy/migration in doc comments.
- Reuse existing patterns from the codebase.

Good: `// Checks if the player can re-enlist with this faction based on cooldown and discharge history.`

Bad: `// Phase 2: Added re-enlistment check. Previously used FactionVeteranRecord, now uses FactionServiceRecord.`

### API Verification
- **Use the local native decompile** to verify Bannerlord APIs before using them.
- Decompile location: `C:\Dev\Enlisted\Decompile\`
- Key namespaces: `TaleWorlds.CampaignSystem`, `TaleWorlds.Core`, `TaleWorlds.Library`
- Don't rely on external docs or AI assumptions - verify in decompile first.

Key files for this system:
- `TaleWorlds.CampaignSystem/Actions/KillCharacterAction.cs` - Hero death mechanics
- `TaleWorlds.CampaignSystem/Actions/EndCaptivityAction.cs` - Prisoner release mechanics
- `TaleWorlds.CampaignSystem/CampaignBehaviors/HeroSpawnCampaignBehavior.cs` - Lord party reformation
- `TaleWorlds.CampaignSystem/Hero.cs` - Hero states (CharacterStates enum)

### Data Files
- **XML** for player-facing text (localization via `ModuleData/Languages/enlisted_strings.xml`)
- **JSON** for content data (events, decisions in `ModuleData/Enlisted/Events/`)
- In code, use `TextObject("{=stringId}Fallback")` for localized strings.
- **CRITICAL:** In JSON, fallback fields (`title`, `setup`, `text`, `resultText`) must immediately follow their ID fields (`titleId`, `setupId`, `textId`, `resultTextId`) for proper parser association.

### Tooltip Best Practices
- **Every event option should have a tooltip** explaining consequences
- Tooltips appear on hover in `MultiSelectionInquiryData` popups via `hint` parameter
- Keep tooltips concise (one sentence, under 80 characters)
- For discharge events, explain:
  - Discharge band consequences (cooldown, reputation)
  - What happens to equipment/baggage
  - Re-enlistment implications

**Example tooltip patterns:**
```json
{
  "tooltip": "Accept discharge. 90-day re-enlistment block applies."
  "tooltip": "Plead your case. Charm check determines outcome."
  "tooltip": "Desert immediately. Criminal record and faction hostility."
}
```

### Logging
- All logs go to: `<BannerlordInstall>/Modules/Enlisted/Debugging/`
- Use: `ModLogger.Info("Category", "message")`
- Categories for this system:
  - `"Enlistment"` - enlist/discharge events
  - `"ServiceRecord"` - faction record updates
  - `"GracePeriod"` - lord death/capture handling
- Log: discharge band, cooldown dates, experience track, errors

### Build
```bash
dotnet build -c "Enlisted RETAIL" /p:Platform=x64
```
Output: `Modules/Enlisted/bin/Win64_Shipping_Client/`

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
| Faction Cooldown | ✅ Complete | `FactionServiceRecord.CooldownEnds` |
| Baggage Stash | ✅ Complete | `_baggageStash` ItemRoster exists |
| Discipline Events | ✅ Complete | Full event chain from 3→5→7→10 threshold |
| Discipline Discharge Event | ✅ Complete | `triggers_discharge` effect calls `StopEnlist()` |
| 90-Day Block | ✅ Complete | `ReenlistmentBlockedUntil` checked in `StartEnlist()` |
| Risky Option System | ✅ Complete | `EffectsSuccess`/`EffectsFailure` with `RiskChance` roll |
| Data Consolidation | ✅ Complete | Unified `FactionServiceRecord` in `ServiceRecordManager` |
| Experience Tracks | ✅ Complete | `ExperienceTrackHelper` sets starting tier 1-3 based on level |
| Rep Snapshot on Discharge | ✅ Complete | Officer/Soldier rep saved per faction and restored on re-entry |
| Baggage Faction Tracking | ✅ Complete | Cross-faction transfer prompt with courier/sell/abandon options |
| Onboarding Event Pacing | ✅ Complete | Track-based onboarding events with stage 1→2→3→complete progression |

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

| Track | Player Level | Starting Tier | Training XP | Description |
|-------|--------------|---------------|-------------|-------------|
| Green | < 10 | T1 | +20% | New to military life, learning quickly |
| Seasoned | 10-20 | T2 | Normal | Knows the basics, steady progression |
| Veteran | 21+ | T3 | -10% | Proven fighter, diminishing returns from drills |

**Training XP Modifier**: Used by training decisions. New soldiers benefit most from training events, while experienced veterans gain more from combat. See [Training System](../Features/Combat/training-system.md) for details.

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

### Phase 1: Fix Discipline Discharge ✅ COMPLETE

**Problem:** The `discipline_discharge` event shows great narrative content but doesn't actually trigger a discharge.

**Solution:** Added `TriggersDischarge` effect to EventEffects system with risky option support.

**Implementation:**

| File | Changes |
|------|---------|
| `EventDefinition.cs` | Added `TriggersDischarge`, `EffectsSuccess`, `EffectsFailure`, `RiskChance` properties |
| `EventCatalog.cs` | Added parsing for `triggers_discharge`, `effects_success`, `effects_failure`, `risk_chance` |
| `EventDeliveryManager.cs` | Added `ApplyDischargeEffect()`, `SetReenlistmentBlock()`, risky option resolution |
| `events_escalation_thresholds.json` | Updated all 4 `discipline_discharge` options with tooltips and `triggers_discharge` |

**Risky Option Behavior:**
- "accept" → immediate dishonorable discharge
- "defiant" → dishonorable discharge with extra reputation penalty  
- "beg" → 25% chance to stay (penal detail), 75% failure → discharge
- "service" → 40% chance to stay (demotion), 60% failure → discharge

**Tasks:**
- [x] Add `TriggersDischarge` property to `EventEffects`
- [x] Add discharge handling to `ApplyEffects()`
- [x] Update discipline_discharge event options in JSON with tooltips
- [x] Set `ReenlistmentBlockedUntil` for 90 days on dishonorable
- [x] Add risky option system with `EffectsSuccess`/`EffectsFailure`

---

### Phase 2: Add 90-Day Re-enlistment Block ✅ COMPLETE

**Problem:** Bad discharges don't block re-enlistment with the faction.

**Solution:** Added block tracking to `FactionServiceRecord` and check in `StartEnlist()`.

**Implementation:**

| File | Changes |
|------|---------|
| `FactionServiceRecord.cs` | Added `ReenlistmentBlockedUntil`, `LastDischargeBand`, `OfficerRepAtExit`, `SoldierRepAtExit` |
| `ServiceRecordManager.cs` | Updated serialization for all new fields |
| `EventDeliveryManager.cs` | Sets block when `triggers_discharge` effect fires |
| `EnlistmentBehavior.cs` | Added `CanEnlistWithFaction()` check in `StartEnlist()` |

**Block Durations:**
- Dishonorable/Deserter: 90 days
- Washout: 30 days
- Honorable/Veteran: No block

**Tasks:**
- [x] Add `ReenlistmentBlockedUntil` to `FactionServiceRecord`
- [x] Set block in discharge logic based on band
- [x] Check block in `StartEnlist()` via `CanEnlistWithFaction()`
- [x] Show UI message when blocked

---

### Phase 3: Consolidate Data Systems ✅ COMPLETE

**Problem:** Two parallel record systems tracking overlapping data:
1. `FactionVeteranRecord` in EnlistmentBehavior (FirstTermCompleted, PreservedTier, CooldownEnds, etc.)
2. `FactionServiceRecord` in ServiceRecordManager (HighestTier, TotalDaysServed, etc.)

**Solution:** Merged into single `FactionServiceRecord` managed by `ServiceRecordManager`.

**Implementation:**

| File | Changes |
|------|---------|
| `FactionServiceRecord.cs` | Added all fields from `FactionVeteranRecord` |
| `ServiceRecordManager.cs` | Serializes all consolidated fields |
| `EnlistmentBehavior.cs` | `GetFactionVeteranRecord()` now returns `FactionServiceRecord` via `ServiceRecordManager` |
| `EnlistmentBehavior.cs` | Removed `_veteranRecords` dictionary and `FactionVeteranRecord` class |

**Unified FactionServiceRecord fields:**
```csharp
// Service history
public int TermsCompleted, TotalDaysServed, HighestTier, BattlesFought, LordsServed, Enlistments, TotalKills;

// Re-enlistment control
public CampaignTime ReenlistmentBlockedUntil;
public string LastDischargeBand;
public int OfficerRepAtExit, SoldierRepAtExit;

// Term tracking
public bool FirstTermCompleted;
public int PreservedTier;
public CampaignTime CooldownEnds, CurrentTermEnd;
public bool IsInRenewalTerm;
public int RenewalTermsCompleted;
```

**Tasks:**
- [x] Add fields to `FactionServiceRecord`
- [x] Update `ServiceRecordManager.SyncData()` to serialize new fields
- [x] Refactor `EnlistmentBehavior` to use `ServiceRecordManager.Instance.GetOrCreateRecord()`
- [x] Delete `FactionVeteranRecord` class
- [x] Delete `_veteranRecords` dictionary from EnlistmentBehavior

---

### Phase 4: Experience Track System ✅ COMPLETE

**Problem:** Player level doesn't affect starting tier.

**Solution:** Added experience track calculation at enlistment via `ExperienceTrackHelper.cs`.

**Implementation:**

| File | Changes |
|------|---------|
| `ExperienceTrackHelper.cs` | NEW - `GetExperienceTrack()`, `GetStartingTierForTrack()`, `GetTrainingXpModifier()` |
| `EnlistmentBehavior.cs` | Calls helper in `ContinueStartEnlistInternal()`, shows track notification |
| `Enlisted.csproj` | Added compile include for new helper |

**Experience Tracks:**

| Track | Player Level | Starting Tier | Training XP |
|-------|--------------|---------------|-------------|
| Green | 1-9 | T1 | +20% |
| Seasoned | 10-20 | T2 | Normal |
| Veteran | 21+ | T3 | -10% |

**Starting Tier Calculation:**
- Base tier from experience track (1-3)
- Faction history bonus: `HighestTier - 2` (only if no bad discharge)
- Result capped at T3 (higher tiers require reservist bonus from veteran/honorable discharge)
- Notification shown after reservist boost applied (displays final tier)

**Edge Cases Handled:**
- Bad discharge (washout/dishonorable/deserter) prevents faction history bonus
- Tier capped at 3 to prevent starting too high from experience track alone
- Notification timing fixed to show after all tier adjustments

**Tasks:**
- [x] Add `GetExperienceTrack()` method to `ExperienceTrackHelper.cs`
- [x] Add `GetStartingTierForTrack()` method with tier cap and bad discharge check
- [x] Add `GetTrainingXpModifier()` method (for Phase 10)
- [x] Call in `ContinueStartEnlistInternal()` to set initial tier
- [x] Show track notification to player after all tier calculations

---

### Phase 5: Rep Snapshot on Discharge ✅ COMPLETE

**Problem:** Rep isn't saved per-faction for restoration on re-entry.

**Solution:** Implemented reputation snapshot system that saves and restores Officer and Soldier reputation based on discharge band.

**Implementation:**

| File | Changes |
|------|---------|
| `EnlistmentBehavior.cs` | Added rep save in `StopEnlist()`, added `ShowReputationRestorationNotification()` |
| `ServiceRecordManager.cs` | Updated `TryConsumeReservistForFaction()` with rep restoration calculation |
| `EnlistmentBehavior.cs` | Modified `TryApplyReservistReentryBoost()` to apply restored rep to `EscalationManager` |

**Reputation Restoration by Discharge Band:**

| Band | Officer Rep Restore | Soldier Rep Restore |
|------|---------------------|---------------------|
| veteran | 75% (3/4) | 75% (3/4) |
| honorable | 50% (1/2) | 50% (1/2) |
| grace | 100% (full) | 100% (full) |
| washout | 0% | 0% |
| deserter | 0% | 0% |

**Key Features:**
- Reputation saved at start of `StopEnlist()` before any state clearing
- Separate reputation tracking per faction via `FactionServiceRecord`
- Player notification on re-entry showing restored reputation
- Values clamped to valid ranges after restoration (Officer: 0-100, Soldier: -50 to 50)
- Grace period discharge grants full restoration (lord died/captured, not player's fault)

**Tasks:**
- [x] Save `OfficerRepAtExit` and `SoldierRepAtExit` to FactionServiceRecord in `StopEnlist()`
- [x] Calculate restoration percentages in `TryConsumeReservistForFaction()`
- [x] Apply restored rep in `TryApplyReservistReentryBoost()`
- [x] Show notification to player about restored reputation
- [x] Add `ClampAll()` call to prevent range overflow
- [x] Test build successful

---

### Phase 6: Baggage Faction Tracking ✅ COMPLETE

**Problem:** Baggage stash isn't tagged with faction, no cross-faction transfer prompt.

**Solution:** Baggage stash now tracks which faction it belongs to. When enlisting with a different faction, player gets a prompt to handle their old belongings.

**Tasks:**
- [x] Add `_baggageStashFactionId` field
- [x] Add `_baggageCourierArrivalTime` and `_pendingCourierBaggage` for delivery tracking
- [x] Set faction ID when items stashed during bag check
- [x] Check on enlistment start - if faction differs and stash not empty, show transfer prompt
- [x] Implement courier option (50g base + 5% of value, 3-day delivery delay)
- [x] Implement sell option (40% of value, immediate gold)
- [x] Implement abandon option (items lost)
- [x] Add `ProcessCourierArrival()` hourly check for delivery
- [x] Update `ReturnBaggageStashToPlayerInventory()` to also return pending courier items on discharge
- [x] Add serialization for all new fields in `SyncData()`
- [x] Add validation in `ValidateLoadedState()`
- [x] Handle edge cases (see below)
- [x] Integrate courier arrival with news system (`PostPersonalDispatchText`)
- [x] Build successful, 0 warnings

**Implementation Details:**
- Cross-faction check runs in `StartEnlist()` before bag check
- Courier cost: 50g base + 5% of item total value
- Remote sell rate: 40% of item value (worse than normal 60%)
- Courier delivery: 3 in-game days
- On discharge: pending courier baggage delivered immediately (don't strand items)
- Faction ID cleared when baggage returned to player
- Courier arrival posts to personal news feed (category: "logistics")

**Edge Cases Handled:**

| Edge Case | Solution |
|-----------|----------|
| Grace period same-kingdom re-enlistment | Skip prompt if baggage faction matches `_pendingDesertionKingdom` |
| Dialog cancelled by player | Shows "Enlistment cancelled" message, aborts enlistment |
| Null/empty selection | Explicit check with user feedback before switch statement |
| Courier already in transit | Merge new items into existing `_pendingCourierBaggage` instead of replacing |
| Courier arrives while prisoner | Defer delivery until `Hero.MainHero.IsPrisoner == false` |
| Courier arrives with no party | Defer delivery until party roster available |
| Legacy saves without faction tag | `HasCrossFactionBaggage()` returns false for null/empty faction ID |
| Faction no longer exists | `ResolveFactionName()` returns null, displays "unknown faction" |
| Discharge during courier transit | Items delivered immediately via `ReturnBaggageStashToPlayerInventory()` |

---

### Phase 7: Onboarding Event Pacing ✅ COMPLETE

**Problem:** New enlistees have no structured introduction to their role based on experience level.

**Solution:** Onboarding state tracking system that fires introductory events based on the player's experience track.

**Tasks:**
- [x] Add `OnboardingStage`, `OnboardingTrack`, and `OnboardingStartTime` to `EscalationState`
- [x] Add helper methods: `IsOnboardingActive`, `IsOnboardingStage()`, `AdvanceOnboardingStage()`, `InitializeOnboarding()`, `ResetOnboarding()`
- [x] Add serialization for onboarding state in `EscalationManager.SyncData()`
- [x] Initialize onboarding on enlistment start (using `ExperienceTrackHelper.GetExperienceTrack()`)
- [x] Reset onboarding on discharge
- [x] Add `OnboardingStage` and `OnboardingTrack` to `EventRequirements`
- [x] Add `MeetsOnboardingRequirements()` check in `EventRequirementChecker`
- [x] Add `AdvancesOnboarding` flag to `EventOption`
- [x] Handle onboarding advancement in `EventDeliveryManager.ApplyFlagChanges()`
- [x] Build successful, 0 warnings

**Implementation Details:**
- Experience tracks: "green" (level 1-9), "seasoned" (level 10-20), "veteran" (level 21+)
- Onboarding stages: 1, 2, 3 (0 = complete/not started)
- Events with `OnboardingStage` requirement only fire when player is at that stage
- Events with `OnboardingTrack` requirement only fire for matching track
- Options with `AdvancesOnboarding: true` advance the stage (1→2→3→complete)
- Onboarding state is reset on discharge, not preserved during grace period
- Existing `events_onboarding.json` content already has track-based events authored

**Edge Cases Handled:**

| Edge Case | Solution |
|-----------|----------|
| Old saves without onboarding data | Default `OnboardingStage = 0` means inactive |
| Corrupted state (active stage, empty track) | `ValidateOnboardingState()` resets to complete |
| Out-of-range stage value | Validation resets to 0 |
| Player not enlisted but onboarding events queried | `MeetsOnboardingRequirements` checks `IsEnlisted` first |
| Grace period re-enlistment | Onboarding not re-initialized; starts fresh next full enlistment |
| Lord dies mid-onboarding | Reset on discharge, not resumed on grace rejoin |
| Null EscalationManager or State | Null checks throughout with safe fallbacks |

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

1. **Phase 1: Discipline Discharge Effect** ✅ Complete - Event triggers actual discharge via `TriggersDischarge`
2. **Phase 2: 90-Day Block** ✅ Complete - Re-enlistment block enforced in `StartEnlist()`
3. **Phase 3: Data Consolidation** ✅ Complete - Unified `FactionServiceRecord` in `ServiceRecordManager`
4. **Phase 4: Experience Tracks** ✅ Complete - `ExperienceTrackHelper` sets starting tier based on player level
5. **Phase 5: Rep Snapshots** ✅ Complete - Save/restore Officer and Soldier reputation per faction
6. **Phase 6: Baggage Tracking** ✅ Complete - Cross-faction transfer with courier/sell/abandon options
7. **Phase 7: Onboarding Pacing** ✅ Complete - Track-based onboarding events with stage progression

---

## Acceptance Criteria

1. ✅ **Discipline Discharge Works**: Discipline 10 → event → accept/fail → player actually discharged with "dishonorable" band
2. ✅ **90-Day Block Enforced**: After dishonorable discharge, can't re-enlist with that faction for 90 days
3. ✅ **Data Consolidated**: Single `FactionServiceRecord` contains all per-faction data
4. ✅ **Experience Track Affects Tier**: Level 5 starts T1, Level 15 starts T2, Level 25 starts T3
5. ✅ **Faction Memory**: Player who reached T5 with good discharge returns at T3 (HighestTier - 2, capped at T3)
6. ✅ **Rep Restoration**: Veteran returning gets partial rep based on discharge band (75% for veteran, 50% for honorable, 100% for grace)
7. ✅ **Cross-Faction Baggage**: Player with baggage from faction A sees transfer prompt when enlisting with faction B (courier/sell/abandon)
8. ✅ **Onboarding Events**: New enlistees see track-based onboarding events (stage 1→2→3→complete) with `AdvancesOnboarding` options

---

## Edge Cases (Already Handled)

| Case | Current Behavior |
|------|------------------|
| Lord dies in battle | Grace period started, player can transfer |
| Lord captured | Grace period started, player can transfer or wait |
| Player captured during service | Grace period started after escape |
| Army dispersed | Checks if lord still alive, handles accordingly |
| Lord has no kingdom | Immediate discharge without penalties |
| Grace period expires | Auto-discharge as deserter with 90-day block |
| Same-faction transfer | `TransferServiceToLord()` preserves progression |
| Bad discharge faction bonus | Washout/dishonorable/deserter prevents faction tier bonus |
| High faction tier cap | Experience track caps at T3; higher tiers need reservist bonus |
| Notification timing | Shown after reservist boost to display final tier |
| Event discharge when not enlisted | Logs warning, returns safely |
| Event discharge with null EnlistmentBehavior | Null check, logs warning |
| RiskChance = 0 or 100 | Treated as non-risky option (no roll) |
| Faction null when checking block | Returns true (allow enlistment) |
| ServiceRecordManager.Instance null | Null propagation prevents crash |
| Muster/smuggle discharge | `RecordReservist()` sets block consistently |
| Rep save with dead lord | Reputation saved before `_enlistedLord` cleared |
| Rep overflow on restore | `ClampAll()` ensures values stay within valid range |
| No previous service | `TryConsumeReservistForFaction()` returns false safely |
| Cross-faction enlistment | Each faction has separate reputation record |
| Negative soldier rep | Integer division handles negatives correctly |
| Zero reputation | Restoration correctly returns 0, no notification shown |
| Baggage grace period same-kingdom | Skip transfer prompt if baggage matches grace kingdom |
| Baggage dialog cancelled | User feedback and enlistment abort |
| Baggage courier in transit + new transfer | Items merged into existing courier |
| Baggage courier arrives while prisoner | Delivery deferred until released |
| Baggage courier arrives with no party | Delivery deferred until party available |
| Baggage from destroyed faction | Displays "unknown faction" in prompt |

---

## Dependencies

- **EventDeliveryManager**: ✅ `TriggersDischarge` effect implemented via `ApplyDischargeEffect()`
- **ServiceRecordManager**: ✅ Serializes all `FactionServiceRecord` fields, centralized `SetReenlistmentBlock()`
- **EnlistmentBehavior**: ✅ Uses consolidated records via `ServiceRecordManager.GetOrCreateRecord()`
- **ExperienceTrackHelper**: ✅ Shared experience track methods for Phase 4 and Phase 10

### Cross-Phase Dependencies

| This Phase | Depends On | Shared Code |
|------------|------------|-------------|
| Phase 4 (Experience Tracks) | None | `ExperienceTrackHelper.GetExperienceTrack()` |
| Phase 10D (Training Modifiers) | Phase 4 | `ExperienceTrackHelper.GetTrainingXpModifier()` |

**Implementation Note:** Phase 4 created `ExperienceTrackHelper.cs` with `GetExperienceTrack()`, `GetStartingTierForTrack()`, and `GetTrainingXpModifier()`. Phase 10 can directly use these methods.

## Target Version

This project targets **Bannerlord v1.3.11**. Verify all API calls against the local decompile.

## Related Documents

- [Content System Architecture](../Features/Content/content-system-architecture.md) - Engineering standards, JSON/XML patterns
- [Core Gameplay](../Features/Core/core-gameplay.md) - Core mechanics reference
- [Event Catalog](../Content/event-catalog-by-system.md) - Event content index
- [Training System](../Features/Combat/training-system.md) - Training system (shares experience track code)
- [UI Systems](../Features/UI/ui-systems-master.md) - UI systems and localization reference

### Source Files

| File | Purpose |
|------|---------|
| `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs` | Main enlistment logic, `ContinueStartEnlistInternal()`, `ShowExperienceTrackNotification()` |
| `src/Features/Retinue/Core/ServiceRecordManager.cs` | Unified faction record storage, serialization, `RecordReservist()`, `SetReenlistmentBlock()` |
| `src/Features/Retinue/Data/FactionServiceRecord.cs` | Consolidated per-faction data: `ReenlistmentBlockedUntil`, `LastDischargeBand`, term tracking |
| `src/Features/Content/ExperienceTrackHelper.cs` | Experience track calculation, starting tier, training XP modifier |
| `src/Features/Content/EventDeliveryManager.cs` | Event delivery, `ApplyDischargeEffect()`, risky option resolution |
| `src/Features/Content/EventCatalog.cs` | JSON parsing for `triggers_discharge`, `effects_success/failure` |
| `src/Features/Content/EventDefinition.cs` | `EventEffects.TriggersDischarge` and related properties |
| `ModuleData/Enlisted/Events/events_escalation_thresholds.json` | Discipline discharge event with `triggers_discharge` |

### Key Implementation Details

**Re-enlistment Block Flow:**
All discharge paths flow through `ServiceRecordManager.RecordReservist()`, which:
1. Records reservist snapshot data for re-entry bonuses
2. Sets `FactionServiceRecord.LastDischargeBand`
3. Calls `SetReenlistmentBlock()` to set `ReenlistmentBlockedUntil` based on band:
   - `dishonorable` / `deserter`: 90 days
   - `washout`: 30 days
   - `veteran` / `honorable` / `grace`: no block

This ensures consistent blocking whether discharge comes from:
- Event options with `triggers_discharge`
- Muster pay resolution
- Smuggle discharge (deserter)
- Grace period expiry

## Native Reference Files

Key decompiled files for API verification:

| Native File | Purpose |
|-------------|---------|
| `Decompile/TaleWorlds.CampaignSystem/Actions/KillCharacterAction.cs` | Hero death mechanics |
| `Decompile/TaleWorlds.CampaignSystem/Actions/EndCaptivityAction.cs` | Prisoner release mechanics |
| `Decompile/TaleWorlds.CampaignSystem/CampaignBehaviors/HeroSpawnCampaignBehavior.cs` | Lord party reformation |
| `Decompile/TaleWorlds.CampaignSystem/Hero.cs` | Hero states (CharacterStates enum) |

## Phase 5 Edge Cases & Safety

The reputation restoration system handles these edge cases:

**Null Safety:**
- All accesses protected with null-conditional operators
- Graceful degradation if EscalationManager or ServiceRecordManager unavailable

**Grace Period with Dead Lord:**
- Reputation saved at START of `StopEnlist()` before `_enlistedLord` cleared
- Works correctly even when lord is dead/captured

**Reputation Range Safety:**
- `ClampAll()` called after restoration to prevent overflow
- Officer: 0-100, Soldier: -50 to 50

**Cross-Faction Enlistment:**
- Each faction maintains separate reputation records
- Vlandia rep doesn't affect Battania rep

**Negative Reputation:**
- Integer division handles negative values correctly
- Example: -40 * 75% = -30 (correct)

**First-Time Enlistment:**
- `TryConsumeReservistForFaction()` returns false if no prior service
- Player starts with default neutral reputation

**Multiple Discharges:**
- Each discharge overwrites saved reputation (desired behavior)
- Most recent service reputation is what matters

**Exception Handling:**
- Both save and restore wrapped in try-catch
- Failures logged as warnings, discharge continues normally

## Phase Completion Checklist

After completing each phase:

- [x] All new code follows ReSharper recommendations
- [x] Comments describe current behavior (no changelog language)
- [x] Existing comments updated if behavior changed
- [x] Build passes with 0 warnings
- [x] Tested in-game for the specific scenario
