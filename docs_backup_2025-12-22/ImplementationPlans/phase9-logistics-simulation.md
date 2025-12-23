# Phase 9: Logistics, Map Simulation & News Context Implementation

**Status**: ✅ Complete  
**Last Updated**: December 22, 2025  
**Prerequisites**: Phase 8 Complete (Advanced Content Features), Unified Content System  
**Target Game Version**: Bannerlord v1.3.11

---

## Engineering Standards

**Follow these while implementing all phases:**

### Code Quality
- **Follow ReSharper linter/recommendations.** Fix warnings; don't suppress them with pragmas.
- **Comments should be factual descriptions of current behavior.** Write them as a human developer would—professional and natural. Don't use "Phase" references, changelog-style framing ("Added X", "Changed from Y"), or mention legacy/migration in doc comments. Just describe what the code does.
- Reuse existing patterns in the codebase.

### API Verification
- **Use the local native decompile** to verify Bannerlord APIs before using them.
- Decompile location: `C:\Dev\Enlisted\Decompile\`
- Key namespaces: `TaleWorlds.CampaignSystem`, `TaleWorlds.Core`, `TaleWorlds.Library`
- Don't rely on external docs or AI assumptions - verify in decompile first.

### Data Files
- **XML** for player-facing text (localization via `ModuleData/Languages/enlisted_strings.xml`)
- **JSON** for content data (events, decisions, orders in `ModuleData/Enlisted/`)
- In code, use `TextObject("{=stringId}Fallback")` for localized strings.

### Logging
- All logs go to: `<BannerlordInstall>/Modules/Enlisted/Debugging/`
- Use: `ModLogger.Info("Category", "message")`
- Log content loading counts, state changes, errors.

### Build
```bash
dotnet build -c "Enlisted RETAIL" /p:Platform=x64
```
Output: `Modules/Enlisted/bin/Win64_Shipping_Client/`

---

## Overview

This document covers five logistics, simulation, and player context systems that bring realistic military life to the Enlisted experience. These systems interact with each other and with the existing content system.

| Phase | Feature | Complexity | Dependencies | Priority | Status |
|-------|---------|------------|--------------|----------|--------|
| **9A** | Supply Simulation | Medium | CompanyNeedsState | HIGH | ✅ Complete |
| **9B** | Map Incidents | Medium | EnlistedIncidentsBehavior, EventCatalog | HIGH | ✅ Complete |
| **9C** | Ration Exchange | Medium | Muster system, Item tracking | MEDIUM | ✅ Complete |
| **9D** | Baggage Check | High | QM reputation, Item system | LOW | ✅ Complete |
| **9E** | Event Context in News | Medium | EnlistedNewsBehavior, EventDeliveryManager | HIGH | ✅ Complete |

**Why these matter:**
- Supply Simulation: ✅ Now uses hybrid 40/60 model (food observed, non-food simulated)
- Map Incidents: ✅ Events fire during battle end, settlement entry/exit, and siege operations
- Ration Exchange: ✅ Issues/reclaims rations at muster based on QM rep and supply levels
- Baggage Check: ✅ Random contraband inspection at muster with QM rep-based outcomes
- Event Context in News: ✅ Event outcomes appear in Personal Feed, pending chain events show in Daily Brief

---

## Table of Contents

1. [Phase 9A: Supply Simulation](#phase-9a-supply-simulation)
2. [Phase 9B: Map Incidents](#phase-9b-map-incidents)
3. [Phase 9C: Ration Exchange](#phase-9c-ration-exchange)
4. [Phase 9D: Baggage Check](#phase-9d-baggage-check)
5. [Phase 9E: Event Context in News](#phase-9e-event-context-in-news)
6. [Implementation Order](#implementation-order)
7. [Testing Checklist](#testing-checklist)

---

## Phase 9A: Supply Simulation

### What It Does

Implements the hybrid 40/60 supply model where:
- **40% Food Component**: Observed from lord's party food (vanilla system, read-only)
- **60% Non-Food Component**: Simulated (ammunition, repair materials, camp supplies)

Currently, `LanceNeedsState.Supplies` just degrades over time with no replenishment. This phase makes it dynamic and responsive to actual gameplay.

### Current State

From `LanceNeedsState.cs`:
- `Supplies` property exists (0-100)
- Degrades daily but never replenishes
- Below 30% blocks equipment menu (already implemented)
- No observation of lord's actual food situation

### Scope: Company (Enlisted Lord's Party)

Supply simulation is scoped to the **Company** (the enlisted lord's party), not the full army:
- If the lord is attached to an army, we still observe only the lord's party food
- Vanilla army food sharing may affect the lord's food days (we observe this naturally)
- We do not aggregate or switch context based on army membership

### Implementation Plan

#### 9A-1: Create CompanySupplyManager Class

New file: `src/Features/Logistics/CompanySupplyManager.cs`

```csharp
public class CompanySupplyManager
{
    private float _nonFoodSupply = 60.0f;
    
    public int TotalSupply
    {
        get
        {
            float food = CalculateFoodComponent();
            float nonFood = _nonFoodSupply;
            return (int)Math.Min(100, food + nonFood);
        }
    }
    
    // Observe lord's food (0-40% contribution)
    private float CalculateFoodComponent()
    {
        MobileParty lordParty = GetLordParty();
        if (lordParty == null) return 40.0f;
        
        int daysOfFood = lordParty.GetNumDaysForFoodToLast();
        
        if (lordParty.Party.IsStarving) return 0.0f;
        if (daysOfFood >= 10) return 40.0f;
        if (daysOfFood >= 7) return 35.0f;
        if (daysOfFood >= 5) return 30.0f;
        if (daysOfFood >= 3) return 20.0f;
        if (daysOfFood >= 1) return 10.0f;
        return 5.0f;
    }
    
    // Daily update for non-food supplies
    public void DailyUpdate()
    {
        float consumption = CalculateNonFoodConsumption();
        float resupply = CalculateNonFoodResupply();
        _nonFoodSupply = Math.Max(0, Math.Min(60, _nonFoodSupply - consumption + resupply));
    }
}
```

#### 9A-2: Consumption Calculation

Base rate: 1.5% per day, modified by:

| Factor | Modifier |
|--------|----------|
| Company size (100 troops baseline) | troops/100 |
| Resting in settlement | 0.3x |
| Traveling | 1.0x |
| Patrolling | 1.2x |
| Raiding | 1.5x |
| Besieging | 2.5x |
| Being besieged | 1.8x |
| Desert terrain | 1.2x |
| Mountain terrain | 1.3x |
| Snow terrain | 1.4x |

#### 9A-3: Resupply Calculation

| Source | Non-Food Gain |
|--------|---------------|
| In town/castle | +3% per day |
| Wealthy settlement (>5000 prosperity) | +4% per day |
| Owned settlement | +5% per day |
| Player foraging duty | +2 to +5% |
| Post-battle loot | +1 to +6% based on enemy count/faction |
| Player purchases | Variable (150-500g for 5-20%) |

#### 9A-4: Integration with LanceNeedsState

Modify `LanceNeedsState.cs`:
- Replace simple degradation with `CompanySupplyManager.TotalSupply`
- Hook `CompanySupplyManager.DailyUpdate()` into daily tick
- Add save/load for `_nonFoodSupply` value

#### 9A-5: Battle Supply Changes

After battles:
- Casualties: -1% per 10 troops lost
- Defeat penalty: -5% if battle lost
- Siege assault: Additional -3%
- Loot: +1% per 25 enemies killed (modified by faction quality)

### Files Modified

| File | Changes |
|------|---------|
| `src/Features/Logistics/CompanySupplyManager.cs` | NEW - Core supply logic with hybrid 40/60 model |
| `src/Features/Company/CompanyNeedsState.cs` | Supplies property now reads from CompanySupplyManager |
| `src/Features/Company/CompanyNeedsManager.cs` | Removed supplies degradation (handled by supply manager) |
| `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs` | Initialize/Shutdown hooks, DailyUpdate, battle events, save/load |
| `Enlisted.csproj` | Added CompanySupplyManager.cs to compilation |

### Success Criteria

- [x] Supply % reflects lord's actual food days (observed, not modified)
- [x] Non-food component degrades based on activity/terrain
- [x] Settlement resupply works (+3-5% per day in town)
- [x] Battle losses/loot affect supply correctly
- [x] Supply persists across save/load
- [x] Below 30% still blocks equipment menu

---

## Phase 9B: Map Incidents

### What It Does

Delivers events during map travel based on game context:
- **LeavingBattle**: Events after combat (looting, wounded, first kill)
- **DuringSiege**: Events during siege operations
- **EnteringTown/Village**: Settlement arrival events
- **LeavingSettlement**: Departure events
- **WaitingInSettlement**: Garrison events

### Current State

From `EnlistedIncidentsBehavior.cs`:
- Can register and trigger custom incidents
- Currently only handles bag-check and pay muster
- Uses native `Incident` system with `MapState.NextIncident`
- Has fallback to `MultiSelectionInquiryData` for reliability

From content index: 45 map incidents defined but not implemented:
- 11 LeavingBattle
- 10 DuringSiege
- 8 EnteringTown
- 6 EnteringVillage
- 6 LeavingSettlement
- 4 WaitingInSettlement

### Implementation Plan

#### 9B-1: Create MapIncidentManager Class

New file: `src/Features/Content/MapIncidentManager.cs`

Responsibilities:
- Register for map events (battle end, settlement enter/leave, hourly during siege)
- Filter incidents by context using `EventRequirementChecker`
- Use weighted selection via `EventSelector`
- Respect cooldowns via `EventPacingManager`
- Deliver via `EventDeliveryManager`

```csharp
public class MapIncidentManager
{
    private readonly EventCatalog _catalog;
    private readonly EventRequirementChecker _requirementChecker;
    private readonly EventSelector _selector;
    
    public void RegisterEvents()
    {
        CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, OnBattleEnd);
        CampaignEvents.OnSettlementEnteredEvent.AddNonSerializedListener(this, OnSettlementEntered);
        CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnSettlementLeft);
        CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
    }
    
    private void OnBattleEnd(MapEvent mapEvent)
    {
        if (!EnlistmentBehavior.Instance?.IsEnlisted == true) return;
        
        TryDeliverIncident("leaving_battle");
    }
    
    private void TryDeliverIncident(string context)
    {
        var candidates = _catalog.GetEventsForContext(context);
        var eligible = candidates.Where(e => _requirementChecker.MeetsAllRequirements(e));
        var selected = _selector.SelectWeighted(eligible);
        
        if (selected != null)
        {
            EventDeliveryManager.Instance?.QueueEvent(selected);
        }
    }
}
```

#### 9B-2: Add Context Filtering to EventCatalog

Extend event loading to include map incident context:

```csharp
public IEnumerable<EventDefinition> GetEventsForContext(string context)
{
    return _events.Values.Where(e => e.Context == context);
}
```

#### 9B-3: Create Map Incident JSON Files

Create JSON files for each context:
- `ModuleData/Enlisted/Events/incidents_battle.json`
- `ModuleData/Enlisted/Events/incidents_siege.json`
- `ModuleData/Enlisted/Events/incidents_settlement.json`

Sample structure from content index:

```json
{
  "events": [
    {
      "id": "mi_loot_decision",
      "context": "leaving_battle",
      "titleId": "mi_loot_title",
      "title": "Dead Man's Purse",
      "setupId": "mi_loot_setup",
      "setup": "Gold on a corpse. Officers aren't looking.",
      "options": [
        {
          "id": "take",
          "textId": "mi_loot_take",
          "text": "Take the gold",
          "effects": { "gold": 25, "scrutiny": 2 }
        },
        {
          "id": "leave",
          "textId": "mi_loot_leave",
          "text": "Leave it be",
          "effects": { "honor": 5 }
        },
        {
          "id": "report",
          "textId": "mi_loot_report",
          "text": "Report the find",
          "effects": { "officerRep": 5, "gold": 5 }
        }
      ]
    }
  ]
}
```

#### 9B-4: Siege Hourly Check

During siege, check hourly for siege-specific incidents:

```csharp
private void OnHourlyTick()
{
    if (!EnlistmentBehavior.Instance?.IsEnlisted == true) return;
    
    var lordParty = GetLordParty();
    if (lordParty?.BesiegerCamp != null)
    {
        // 10% chance per hour to trigger siege incident
        if (MBRandom.RandomFloat < 0.10f)
        {
            TryDeliverIncident("during_siege");
        }
    }
}
```

#### 9B-5: Pacing Integration

Use existing `EventPacingManager` but with shorter cooldowns for map incidents:
- Battle incidents: 1 battle cooldown (not time-based)
- Settlement incidents: 12 hour cooldown
- Siege incidents: 4 hour cooldown

### Files Modified

| File | Changes |
|------|---------|
| `src/Features/Content/MapIncidentManager.cs` | ✅ NEW - Map incident delivery with context-based filtering and cooldowns |
| `src/Features/Content/EventCatalog.cs` | ✅ Added GetEventsForContext method for context filtering |
| `src/Mod.Entry/SubModule.cs` | ✅ Register MapIncidentManager as campaign behavior |
| `ModuleData/Enlisted/Events/incidents_battle.json` | ✅ NEW - 3 battle incidents (loot, wounded enemy, battlefield find) |
| `ModuleData/Enlisted/Events/incidents_settlement.json` | ✅ NEW - 4 settlement incidents (tavern, message, hangover, gratitude) |
| `Enlisted.csproj` | ✅ Added MapIncidentManager.cs to compilation |

### Success Criteria

- [x] Battle end triggers LeavingBattle incident selection
- [x] Entering town/village triggers appropriate incidents
- [x] Siege events fire periodically during siege (10% per hour)
- [x] Incidents respect cooldowns (battle: 1hr, settlement: 12hr, siege: 4hr)
- [x] Incidents use existing event popup system via EventDeliveryManager
- [x] Effects apply correctly (gold, rep, skill XP, traits)
- [x] Weighted selection with priority modifiers
- [x] Integration with EventRequirementChecker, EscalationManager
- [x] Build successful with no errors or warnings

---

## Phase 9C: Ration Exchange System

### What It Does

At each 12-day muster, the quartermaster:
1. Reclaims the previously issued ration (if still exists)
2. Issues a new ration (quality based on QM reputation)
3. OR issues nothing if company supplies are low

This creates real food scarcity where players can't hoard military rations.

### Current State: COMPLETE

Fully implemented with muster news reporting.

### Scope: T1-T6 (Enlisted/NCO/Officer Track)

T7+ commanders don't receive issued rations; they purchase provisioning for their personal retinue.

### Implementation Plan

#### 9C-1: Add Issued Ration Tracking

In `EnlistmentBehavior.cs`:

```csharp
// Track issued rations for reclamation
private List<IssuedRationRecord> _issuedRations = new();

private class IssuedRationRecord
{
    public string ItemId { get; set; }
    public int Amount { get; set; }
    public CampaignTime IssuedAt { get; set; }
}
```

#### 9C-2: Process Ration Exchange at Muster

```csharp
private void ProcessMusterFoodRation()
{
    if (GetPlayerTier() >= 7) return; // Commanders don't get issued rations
    
    int companySupply = LanceNeedsState.Supplies;
    bool rationAvailable = DetermineRationAvailability(companySupply);
    
    if (!rationAvailable)
    {
        // No rations this muster - don't reclaim either
        DisplayNoRationsMessage(companySupply);
        return;
    }
    
    // Reclaim old rations (from inventory AND stowage)
    ReclaimIssuedRations();
    
    // Issue new ration based on QM rep
    IssueNewRation();
}

private bool DetermineRationAvailability(int supply)
{
    if (supply >= 70) return true;          // Always
    if (supply >= 50) return MBRandom.RandomFloat < 0.80f;  // 80%
    if (supply >= 30) return MBRandom.RandomFloat < 0.50f;  // 50%
    return false;  // Never below 30%
}
```

#### 9C-3: Food Quality by QM Reputation

```csharp
private ItemObject GetFoodItemForReputation(int qmRep)
{
    if (qmRep >= 80) return GetItem("meat");
    if (qmRep >= 50) return GetItem("cheese");
    if (qmRep >= 20) return GetItem("butter");
    return GetItem("grain"); // Default
}
```

| QM Rep | Food Item | Value | Flavor |
|--------|-----------|-------|--------|
| 80+ | Meat | 30g | "The quartermaster favors you." |
| 50-79 | Cheese | 50g | "The quartermaster is being generous." |
| 20-49 | Butter | 40g | "Better than grain, at least." |
| -9 to 19 | Grain | 10g | "Standard rations." |
| < -10 | Grain | 10g | "Moldy and barely edible." |

#### 9C-4: Reclaim From All Storage Locations

```csharp
private void ReclaimIssuedRations()
{
    foreach (var record in _issuedRations)
    {
        var item = GetItem(record.ItemId);
        int toReclaim = record.Amount;
        
        // Check main inventory first
        int inInventory = MobileParty.MainParty.ItemRoster.GetItemNumber(item);
        int fromInventory = Math.Min(toReclaim, inInventory);
        if (fromInventory > 0)
        {
            MobileParty.MainParty.ItemRoster.AddToCounts(item, -fromInventory);
            toReclaim -= fromInventory;
        }
        
        // Check stowage (if implemented)
        if (toReclaim > 0 && StowageManager != null)
        {
            int inStowage = StowageManager.GetItemCount(item);
            int fromStowage = Math.Min(toReclaim, inStowage);
            if (fromStowage > 0)
            {
                StowageManager.RemoveItem(item, fromStowage);
            }
        }
        
        ModLogger.Info("Ration", $"Reclaimed {record.ItemId}: {record.Amount - toReclaim} recovered");
    }
    
    _issuedRations.Clear();
}
```

#### 9C-5: Discharge Reclamation

When player leaves service (discharge, desertion, lord death):
- Reclaim all issued rations
- Personal food (bought/foraged) is kept
- Add to `OnServiceEnded()` flow

#### 9C-6: Save/Load

Add `_issuedRations` to save data sync.

### Files Modified

| File | Changes |
|------|---------|
| `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs` | ✅ Added IssuedRationRecord class, _issuedRations tracking field, SerializeIssuedRations() method, ProcessMusterFoodRation(), DetermineRationAvailability(), DisplayNoRationsMessage(), ReclaimIssuedRations(), IssueNewRation(), GetFoodItemForReputation(), GetRationFlavorText(), ReportMusterOutcome(), ParsePayOutcome() methods; integrated into OnMusterCycleComplete() and StopEnlist() |
| `src/Features/Interface/Behaviors/EnlistedNewsBehavior.cs` | ✅ Added MusterOutcomeRecord class, _musterOutcomes tracking field, AddMusterOutcome(), GetLastMusterOutcome(), GetLastMusterSummary(), BuildMusterHeadline(), GetRationDisplayName(), AddToPersonalFeed() methods for muster news reporting |

### Success Criteria

- [x] Player receives 1 food item at each muster (T1-T6)
- [x] Food quality matches QM reputation
- [x] Old rations reclaimed at next muster (tracked items only)
- [x] No ration issued when company supply < 30%
- [x] 50% chance when supply 30-49%
- [x] 80% chance when supply 50-69%
- [x] Personal food (bought) is never reclaimed
- [x] Rations reclaimed on discharge
- [x] Save/load preserves issued rations tracking
- [x] Muster outcome reported to news system with pay, ration, supply, and unit status
- [x] Muster headlines appear in personal feed for camp menu display

---

## Phase 9D: Baggage Check System ✅

### What It Does

At muster, the quartermaster randomly inspects player inventory (30% chance) for contraband items:
- **Tier violations**: Items above player tier + 1
- **Role violations**: Ranged weapons for non-ranged roles, cavalry lances for infantry
- **Luxury items**: High-value trade goods (jewelry, velvet, silk, etc.)

Quest items and food are always protected from inspection.

### Implementation

#### ContrabandChecker Class

Location: `src/Features/Logistics/ContrabandChecker.cs`

Static utility class that scans player inventory for contraband:

```csharp
// Core scanning method
public static ContrabandCheckResult ScanInventory(int playerTier, string playerRole)

// Violation checks
private static Tuple<string, string> CheckTierViolation(ItemObject item, int playerTier)
private static Tuple<string, string> CheckRoleViolation(ItemObject item, string playerRole)
private static Tuple<string, string> CheckLuxuryViolation(ItemObject item)
private static bool IsQuestItem(ItemObject item)  // Protected from confiscation

// Financial calculations
public static int CalculateBribeAmount(int itemValue)  // 50-75% of value
public static int CalculateFineAmount(int itemValue)   // 25-50% of value

// Item removal
public static bool ConfiscateItem(ItemObject item)
```

**Tier Violation Rules:**
- Player tier 1 → allowed items T1-T2
- Player tier 2 → allowed items T1-T3
- Player tier N → allowed items T1-(N+1)

**Role Violation Rules:**
- Crossbows/Bows → require ranged role (Scout, Archer, Marksman)
- Lance weapons → require cavalry role or Officer/NCO

#### Muster Integration

Location: `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`

Called from `OnMusterCycleComplete()` after ration exchange:

```csharp
private void ProcessBaggageCheck()
{
    // Skip first muster after grace period transfer
    if (_isGracePeriodReenlistment) { ... return; }
    
    // Skip if already inspected today
    if (Math.Abs(currentDay - _lastBaggageCheckDay) < 0.5f) return;
    
    // 30% random chance
    if (MBRandom.RandomFloat > 0.30f) return;
    
    // Scan and trigger event based on QM rep
    var result = ContrabandChecker.ScanInventory(_enlistmentTier, playerRole);
    if (!result.HasContraband) return;
    
    TriggerBaggageCheckEvent(GetQMReputation(), result);
}
```

#### Event Outcomes

| QM Rep | Event ID | Outcome |
|--------|----------|---------|
| 65+ | `evt_baggage_lookaway` | QM ignores contraband, no penalty |
| 35-64 | `evt_baggage_bribe` | Bribe opportunity + smuggle option |
| < 35 | `evt_baggage_confiscate` | Confiscation with fine and Scrutiny |

**Bribe Option (Charm Skill Check):**
- Charm 60+: Auto-success
- Charm 30-59: Scaled roll (20% at 30, 80% at 59)
- Charm < 30: Auto-fail
- Cost: 50-75% of item value

**Smuggle Option (Roguery 40+ Required):**
- 70% success chance (30% discovery risk)
- Success: Keep item, +15 Roguery XP
- Failure: +4 Scrutiny, -10 QM rep, item confiscated

**Confiscation Penalties:**
- Most valuable contraband item removed
- Fine: 25-50% of item value
- +2 Scrutiny (standard)
- +3 Scrutiny if player can't afford full fine
- +3 Scrutiny, +1 Discipline if player is broke

#### Edge Cases Handled

- **Empty inventory**: No inspection needed
- **No contraband found**: Silent skip
- **Multiple contraband items**: Confiscates most valuable
- **Quest items**: Never flagged as contraband
- **Grace period re-enlistment**: Skip first muster
- **Insufficient gold for bribe**: Fails to confiscation
- **Insufficient gold for fine**: Partial payment + extra Scrutiny
- **Save/Load**: `_lastBaggageCheckDay` and `_isGracePeriodReenlistment` persisted

### Files Modified

| File | Changes |
|------|---------|
| `src/Features/Logistics/ContrabandChecker.cs` | NEW - Contraband detection logic |
| `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs` | Baggage check trigger, handler methods, save/load |
| `src/Features/Content/EventDeliveryManager.cs` | Baggage event outcome handlers, Charm skill check |
| `ModuleData/Enlisted/Events/muster_events.json` | 3 baggage check events |
| `Enlisted.csproj` | Added ContrabandChecker.cs to compilation |

### Success Criteria ✅

- [x] 30% chance of inspection at muster
- [x] Contraband detected based on tier/role/luxury rules
- [x] QM rep 65+: Look away (no penalty)
- [x] QM rep 35-64: Bribe opportunity with Charm check
- [x] QM rep < 35: Confiscation with fine and Scrutiny
- [x] Charm skill affects bribe success (60+ auto-success, <30 auto-fail, 30-59 roll)
- [x] Smuggle option for Roguery 40+ with 30% discovery risk
- [x] Smuggle failure applies double penalty (+4 Scrutiny, -10 QM rep)
- [x] Contraband items properly removed from inventory
- [x] Fines deducted from player gold
- [x] Quest items protected from confiscation
- [x] Save/Load persistence for inspection tracking
- [x] Build successful with 0 warnings, 0 errors

---

## Phase 9E: Event Context in News

### What It Does

Surfaces event outcomes, pending chain events, and situational context in the Camp News system so players understand what's happening around them. Without this, events fire and apply effects but the player has no lasting record or situational awareness.

### Current State

From `EnlistedNewsBehavior.cs`:
- **Personal Feed** (`_personalFeed`): List of `DispatchItem` for personal events
- **Daily Brief** (Company/Unit/Kingdom lines): Generated once per day
- **Order Outcome Tracking**: `_orderOutcomes` list exists for duty results
- **Reputation Change Tracking**: `_reputationChanges` list exists
- `AddPersonalNews()` method adds items to personal feed

**Missing:**
- No event outcome tracking when events fire
- No display of pending chain events (e.g., "a comrade owes you money")
- No flag-based context hints (e.g., "you're known for generosity")
- No supply/ration context in daily brief

### Implementation Plan

#### 9E-1: Add Event Outcome Tracking

New tracking class in `EnlistedNewsBehavior.cs`:

```csharp
private List<EventOutcomeRecord> _eventOutcomes = new();

public class EventOutcomeRecord
{
    public string EventId { get; set; }
    public string EventTitle { get; set; }
    public string OptionChosen { get; set; }
    public string OutcomeSummary { get; set; }
    public int DayNumber { get; set; }
    public Dictionary<string, int> EffectsApplied { get; set; }
}
```

#### 9E-2: Notify News System from EventDeliveryManager

In `EventDeliveryManager.ApplyEffects()`, after effects are applied:

```csharp
private void NotifyNewsOfEventOutcome(EventOption option)
{
    if (EnlistedNewsBehavior.Instance == null) return;
    if (_currentEvent == null) return;
    
    var outcome = new EventOutcomeRecord
    {
        EventId = _currentEvent.Id,
        EventTitle = ResolveText(_currentEvent.TitleId, _currentEvent.TitleFallback),
        OptionChosen = ResolveText(option.TextId, option.TextFallback),
        OutcomeSummary = BuildOutcomeSummary(option),
        DayNumber = (int)CampaignTime.Now.ToDays,
        EffectsApplied = BuildEffectsSummary(option.Effects)
    };
    
    EnlistedNewsBehavior.Instance.AddEventOutcome(outcome);
}

private string BuildOutcomeSummary(EventOption option)
{
    var parts = new List<string>();
    var effects = option.Effects;
    
    if (effects?.Gold > 0) parts.Add($"+{effects.Gold} gold");
    if (effects?.Gold < 0) parts.Add($"{effects.Gold} gold");
    if (effects?.Xp != null)
    {
        foreach (var xp in effects.Xp)
            parts.Add($"+{xp.Value} {xp.Key} XP");
    }
    if (effects?.SoldierRep != 0) 
        parts.Add($"{effects.SoldierRep:+#;-#;0} Soldier Rep");
    if (effects?.OfficerRep != 0) 
        parts.Add($"{effects.OfficerRep:+#;-#;0} Officer Rep");
    if (effects?.Scrutiny != 0) 
        parts.Add($"{effects.Scrutiny:+#;-#;0} Scrutiny");
    
    return parts.Count > 0 ? $"({string.Join(", ", parts)})" : "";
}
```

#### 9E-3: Add Pending Chain Event Tracking

Track scheduled chain events for display:

```csharp
private List<PendingEventRecord> _pendingEvents = new();

public class PendingEventRecord
{
    public string SourceEventId { get; set; }
    public string ChainEventId { get; set; }
    public string ContextHint { get; set; }
    public int ScheduledDay { get; set; }
    public int CreatedDay { get; set; }
}

public void AddPendingEvent(string sourceId, string chainId, string hint, int delayDays)
{
    _pendingEvents.Add(new PendingEventRecord
    {
        SourceEventId = sourceId,
        ChainEventId = chainId,
        ContextHint = hint,
        CreatedDay = (int)CampaignTime.Now.ToDays,
        ScheduledDay = (int)CampaignTime.Now.ToDays + delayDays
    });
}

public void ClearPendingEvent(string chainEventId)
{
    _pendingEvents.RemoveAll(p => p.ChainEventId == chainEventId);
}
```

#### 9E-4: Extend Daily Brief with Event Context

Modify `BuildDailyBriefSection()` to include:

```csharp
public string BuildDailyBriefSection()
{
    var parts = new List<string>();
    
    // Existing: Company movement/objective
    if (!string.IsNullOrWhiteSpace(_dailyBriefCompany))
        parts.Add(_dailyBriefCompany);
    
    // NEW: Supply context
    var supplyContext = BuildSupplyContextLine();
    if (!string.IsNullOrWhiteSpace(supplyContext))
        parts.Add(supplyContext);
    
    // Existing: Casualty report
    var casualtyLine = BuildCasualtyReportLine();
    if (!string.IsNullOrWhiteSpace(casualtyLine))
        parts.Add(casualtyLine);
    
    // NEW: Recent event aftermath
    var recentEventLine = BuildRecentEventLine();
    if (!string.IsNullOrWhiteSpace(recentEventLine))
        parts.Add(recentEventLine);
    
    // NEW: Pending events context
    var pendingLine = BuildPendingEventsLine();
    if (!string.IsNullOrWhiteSpace(pendingLine))
        parts.Add(pendingLine);
    
    // NEW: Flag-based context
    var flagContext = BuildFlagContextLine();
    if (!string.IsNullOrWhiteSpace(flagContext))
        parts.Add(flagContext);
    
    // Existing: Unit status
    if (!string.IsNullOrWhiteSpace(_dailyBriefUnit))
        parts.Add(_dailyBriefUnit);
    
    // Existing: Kingdom news
    if (!string.IsNullOrWhiteSpace(_dailyBriefKingdom))
        parts.Add(_dailyBriefKingdom);
    
    return string.Join(" ", parts);
}
```

#### 9E-5: Supply Context Line

```csharp
private string BuildSupplyContextLine()
{
    var supply = LanceNeedsState.Instance?.Supplies ?? 100;
    
    if (supply < 30)
        return "The company is nearly out of supplies. Equipment changes are restricted.";
    if (supply < 50)
        return "Supplies are running thin. The quartermaster looks worried.";
    if (supply < 70)
        return "Supplies are holding, but careful management is needed.";
    
    return ""; // Good supply, no need to mention
}
```

#### 9E-6: Recent Event Aftermath Line

Show context from events in the last 24 hours:

```csharp
private string BuildRecentEventLine()
{
    var currentDay = (int)CampaignTime.Now.ToDays;
    var recentEvents = _eventOutcomes
        .Where(e => currentDay - e.DayNumber <= 1)
        .OrderByDescending(e => e.DayNumber)
        .Take(1)
        .ToList();
    
    if (recentEvents.Count == 0) return "";
    
    var recent = recentEvents[0];
    
    // Context-specific aftermath text
    if (recent.EventId.Contains("dice") || recent.EventId.Contains("gambling"))
        return "The men still talk about your dice game.";
    if (recent.EventId.Contains("training"))
        return "Yesterday's training session left your arms sore, but your skills improved.";
    if (recent.EventId.Contains("hunt"))
        return "Fresh game from the hunt has improved everyone's mood.";
    if (recent.EventId.Contains("lend") || recent.EventId.Contains("loan"))
        return "A comrade owes you a debt.";
    
    return "";
}
```

#### 9E-7: Pending Events Context Line

```csharp
private string BuildPendingEventsLine()
{
    if (_pendingEvents.Count == 0) return "";
    
    var currentDay = (int)CampaignTime.Now.ToDays;
    var lines = new List<string>();
    
    foreach (var pending in _pendingEvents)
    {
        int daysRemaining = pending.ScheduledDay - currentDay;
        
        if (pending.ChainEventId.Contains("repay"))
        {
            if (daysRemaining <= 1)
                lines.Add("A comrade promised to repay you today.");
            else
                lines.Add($"A comrade owes you money. It's been {currentDay - pending.CreatedDay} days.");
        }
        else if (pending.ChainEventId.Contains("gratitude"))
        {
            lines.Add("Someone remembers your kindness.");
        }
        else if (!string.IsNullOrEmpty(pending.ContextHint))
        {
            lines.Add(pending.ContextHint);
        }
    }
    
    return lines.Count > 0 ? lines[0] : ""; // Show most relevant
}
```

#### 9E-8: Flag-Based Context Line

Use escalation flags to add personality context:

```csharp
private string BuildFlagContextLine()
{
    var state = EscalationManager.Instance?.State;
    if (state == null) return "";
    
    var lines = new List<string>();
    
    if (state.HasFlag("has_helped_comrade"))
        lines.Add("You're known for helping your comrades.");
    if (state.HasFlag("dice_winner"))
        lines.Add("Your luck at dice is remembered.");
    if (state.HasFlag("shared_winnings"))
        lines.Add("The men appreciate your generosity.");
    if (state.HasFlag("officer_attention"))
        lines.Add("Officers have taken notice of you lately.");
    if (state.HasFlag("training_focused"))
        lines.Add("Your dedication to training has been noted.");
    
    // Return one random context to avoid spam
    if (lines.Count > 0)
        return lines[MBRandom.RandomInt(lines.Count)];
    
    return "";
}
```

#### 9E-9: Personal Feed Event Headlines

Add formatted headlines for event outcomes:

```csharp
public void AddEventOutcome(EventOutcomeRecord outcome)
{
    var headline = BuildEventHeadline(outcome);
    var placeholders = new Dictionary<string, string>
    {
        { "EVENT_TITLE", outcome.EventTitle },
        { "OPTION", outcome.OptionChosen },
        { "EFFECTS", outcome.OutcomeSummary }
    };
    
    AddPersonalNews("event", headline, placeholders, outcome.EventId, 2);
}

private string BuildEventHeadline(EventOutcomeRecord outcome)
{
    // Context-specific headlines
    if (outcome.EventId.Contains("dice"))
        return "Dice game in camp — {EFFECTS}";
    if (outcome.EventId.Contains("training"))
        return "Training session — {EFFECTS}";
    if (outcome.EventId.Contains("hunt"))
        return "Hunt with the lord — {EFFECTS}";
    if (outcome.EventId.Contains("lend"))
        return "Lent money to a comrade";
    if (outcome.EventId.Contains("repay"))
        return "Debt repaid — {EFFECTS}";
    if (outcome.EventId.Contains("scrutiny"))
        return "Unwanted attention — {EFFECTS}";
    if (outcome.EventId.Contains("discipline"))
        return "Disciplinary matter — {EFFECTS}";
    
    // Generic fallback
    return "{EVENT_TITLE} — {EFFECTS}";
}
```

### Sample Headlines Reference

**Event Outcome Headlines (Personal Feed):**

| Event Type | Headline Format | Example |
|------------|-----------------|---------|
| Dice Game | "Dice game in camp — {EFFECTS}" | "Dice game in camp — (+15 gold)" |
| Training | "Training session — {EFFECTS}" | "Training session — (+12 Polearm XP)" |
| Hunt | "Hunt with the lord — {EFFECTS}" | "Hunt with the lord — (+20 gold, +5 Lord Rep)" |
| Loan Extended | "Lent money to a comrade" | "Lent money to a comrade" |
| Loan Repaid | "Debt repaid — {EFFECTS}" | "Debt repaid — (+75 gold)" |
| Scrutiny Event | "Unwanted attention — {EFFECTS}" | "Unwanted attention — (+2 Scrutiny)" |
| Discipline | "Disciplinary matter — {EFFECTS}" | "Disciplinary matter — (+1 Discipline)" |

**Daily Brief Context Lines:**

| Situation | Example Line |
|-----------|--------------|
| Low Supply (<50%) | "Supplies are running thin. The quartermaster looks worried." |
| Critical Supply (<30%) | "The company is nearly out of supplies. Equipment changes are restricted." |
| No Rations | "No rations were issued at last muster. Personal food reserves are critical." |
| Post-Training | "Yesterday's training session left your arms sore, but your skills improved." |
| Post-Hunt | "Fresh game from the hunt has improved everyone's mood." |
| Post-Dice | "The men still talk about your dice game." |
| Pending Loan | "A comrade owes you money. It's been 5 days." |
| Pending Gratitude | "Someone remembers your kindness." |
| Flag: Generous | "You're known for helping your comrades." |
| Flag: Lucky | "Your luck at dice is remembered." |
| Flag: Officer Notice | "Officers have taken notice of you lately." |

**Sample Complete Daily Brief (Before vs After):**

**Before (current):**
```
The company marches northeast toward Sargoth. We've lost 3 soldiers 
since last muster. You feel rested and ready.
```

**After (with event context):**
```
The company marches northeast toward Sargoth. Supplies are holding 
steady at 68%. We've lost 3 soldiers since last muster. Yesterday's 
training session with the weapon master left your arms sore, but 
your polearm work has improved noticeably. The men still talk about 
your dice game win — some admire your luck, others your generosity 
when you bought rounds. A comrade owes you 50 denars; he promised 
to repay by week's end. You feel rested and ready.
```

**Sample RECENT ACTIONS Section (Enhanced):**

```
_____ RECENT ACTIONS _____
• Dice game in camp — kept winnings (+15 gold) (Day 24)
• Training session — focused on Polearms (+12 Polearm XP) (Day 23)
• Lent money to a comrade (50 gold, repayment pending) (Day 22)
• Hunt with Lord Derthert — took modest share (+20 gold, +5 Lord Rep) (Day 21)
• Completed guard duty (Day 20)
```

### Files Modified

| File | Changes |
|------|---------|
| `src/Features/Interface/Behaviors/EnlistedNewsBehavior.cs` | ✅ Added EventOutcomeRecord, PendingEventRecord classes; _eventOutcomes, _pendingEvents fields; AddEventOutcome(), AddPendingEvent(), ClearPendingEvent(), CleanupStalePendingEvents() methods; BuildSupplyContextLine(), BuildRecentEventLine(), BuildPendingEventsLine(), BuildFlagContextLine(), BuildEventHeadline() methods; save/load helpers; edge case handling (delay validation, capacity limits, stale cleanup) |
| `src/Features/Content/EventDeliveryManager.cs` | ✅ Added NotifyNewsOfEventOutcome(), NotifyNewsOfPendingChainEvent(), BuildOutcomeSummary(), BuildEffectsDictionary(), BuildChainEventContextHint() methods |
| `src/Features/Content/EventPacingManager.cs` | ✅ Added ClearPendingEvent call when chain events fire |

### Success Criteria

- [x] Event outcomes appear in Personal Feed after event resolution
- [x] Headlines show relevant effects (gold, XP, rep changes)
- [x] Pending chain events show context hints in Daily Brief
- [x] Supply context appears when supply < 70%
- [x] Recent event aftermath shows for 24 hours
- [x] Flag-based context adds personality hints
- [x] Daily Brief flows naturally as a paragraph
- [x] Save/load preserves event outcome and pending event tracking

### Edge Cases Handled

| Edge Case | Solution |
|-----------|----------|
| Zero/negative delay days | `AddPendingEvent()` validates `delayDays >= 1`, skips if too short |
| Pending events overflow | Capped at 10 entries, oldest removed when exceeded |
| Stale pending events | Daily cleanup removes events more than 7 days overdue |
| Negative day display | `daysSince` clamped to minimum 1 in pending events line |
| Very old pending events | Skipped in display if more than 7 days overdue |
| Null singletons | Null-conditional checks on all manager instances |
| Null effects | Returns empty string/dictionary, doesn't crash |
| Duplicate events | Same-day duplicates prevented in AddEventOutcome |
| Duplicate pending events | Existing pending event with same chain ID replaced |

---

## Implementation Order

### Recommended Sequence

```
Phase 9A: Supply Simulation     [3-4 days] ─┐  ✅ Complete
                                            ├─► Phase 9C depends on Supply %
Phase 9B: Map Incidents         [2-3 days] ─┤  ✅ Complete
                                            │
Phase 9E: Event Context in News [2-3 days] ─┘  ✅ Complete

Phase 9C: Ration Exchange       [2-3 days] ─── ✅ Complete

Phase 9D: Baggage Check         [3-4 days] ─── ✅ Complete
```

### Why This Order

1. **Supply Simulation (9A)**: ✅ Foundation for ration availability and supply context in news
2. **Map Incidents (9B)**: ✅ Independent, expands content delivery; news integration benefits from 9E
3. **Event Context in News (9E)**: ✅ Enhances player awareness of all events
4. **Ration Exchange (9C)**: ✅ Depends on Supply % from 9A; ration news benefits from 9E
5. **Baggage Check (9D)**: ✅ Complex but complete; contraband inspection at muster

### Dependencies

```
LanceNeedsState.Supplies ──► Supply Simulation (9A)
                               │
                               ├──► Supply Context in News (9E)
                               │
                               ▼
                           Ration Exchange (9C) ◄── Muster System
                               │
                               ▼
                           Baggage Check (9D) ◄── Contraband Rules

EventDeliveryManager ──► Map Incidents (9B) ──┐
EventCatalog                │                 │
EventSelector               ▼                 │
                        Settlement/Battle     │
                            Hooks             │
                                              ▼
EnlistedNewsBehavior ◄───────────────────── Event Context (9E)
EscalationState (flags)                       │
Chain Events                                  │
                                              ▼
                                        Daily Brief
                                        Personal Feed
                                        RECENT ACTIONS
```

---

## Testing Checklist

### Phase 9A: Supply Simulation ✅ Code Complete

- [x] Start at 100% supply (60% non-food + 40% food when lord has 10+ days)
- [x] In settlement: Supply stays stable/increases (+3-5% resupply, 0.3x consumption)
- [x] Traveling: Supply slowly degrades (1.5% base × size × terrain)
- [x] Siege: Supply degrades faster (2.5x activity multiplier)
- [x] Battle defeat: -5% penalty
- [x] Battle loot: +1-6% based on enemy count (capped at 6%)
- [x] Lord with 10+ days food: Full food component (40%)
- [x] Lord starving: Food component = 0%
- [x] Below 30% supply: Equipment menu blocked (existing logic)
- [x] Save/load preserves non-food supply value
- [x] Edge case: FoodChange >= 0 handled (returns 40%)
- [x] Edge case: Grace period re-enlistment preserves supply
- [x] Edge case: Lord captured/dies gracefully handled

### Phase 9B: Map Incidents ✅ Code Complete

- [x] Win battle → LeavingBattle incident chance
- [x] Enter town → EnteringTown incident chance
- [x] Enter village → EnteringVillage incident chance
- [x] During siege → Hourly siege incident chance (10%)
- [x] Cooldowns prevent spam (battle: 1hr, settlement: 12hr, siege: 4hr)
- [x] Context filtering works (battle events don't fire in town)
- [x] Effects apply correctly (gold, rep, XP, traits)
- [x] Weighted selection with priority modifiers
- [x] Integration with existing event systems

### Phase 9C: Ration Exchange ✅ Code Complete

- [x] Muster day: Player receives 1 food item
- [x] QM rep 80+: Receive meat
- [x] QM rep 50-79: Receive cheese
- [x] QM rep 20-49: Receive butter
- [x] QM rep < 20: Receive grain
- [x] Supply 70%+: Always get ration
- [x] Supply 50-69%: 80% chance
- [x] Supply 30-49%: 50% chance
- [x] Supply < 30%: No ration
- [x] Old ration reclaimed from inventory
- [x] Old ration reclaimed from stowage (tracked items only - personal food never reclaimed)
- [x] Personal food not reclaimed (only tracked issued rations are reclaimed)
- [x] Rations reclaimed on discharge
- [x] T7+ commanders don't receive rations
- [x] Save/load preserves issued rations tracking

### Phase 9D: Baggage Check ✅ Code Complete

- [x] 30% chance of inspection at muster
- [x] Contraband detected by tier/role/luxury rules
- [x] Quest items protected from confiscation
- [x] QM rep 65+: Look away (no penalty)
- [x] QM rep 35-64: Bribe opportunity (Charm skill check)
- [x] QM rep < 35: Confiscation with fine
- [x] Charm 60+ auto-success, <30 auto-fail, 30-59 scaled roll
- [x] Smuggle option for Roguery 40+ with 30% discovery risk
- [x] Smuggle failure = double penalty (+4 Scrutiny, -10 QM rep)
- [x] Insufficient gold handling (partial payment, extra Scrutiny)
- [x] Save/load persistence for inspection tracking

### Phase 9E: Event Context in News ✅ Code Complete

- [x] Event outcomes appear in Personal Feed after event resolution
- [x] Headlines include effect summaries (+gold, +XP, +rep)
- [x] Pending chain events display in Daily Brief
- [x] Supply context line appears when supply < 70%
- [x] Supply context line shows critical warning < 30%
- [x] Recent event aftermath shows for 24 hours after event
- [x] Flag-based context adds personality hints (generous, lucky, noticed)
- [x] Daily Brief reads as natural flowing paragraph
- [x] RECENT ACTIONS section shows event outcomes with day numbers
- [x] Loan pending shows "comrade owes you money" context
- [x] Loan repaid clears pending event and shows outcome
- [x] Save/load preserves event outcomes list
- [x] Save/load preserves pending events list
- [x] No duplicate entries for same event

---

## Reference Documents

- `docs/Features/Equipment/company-supply-simulation.md` - Full supply system design (implemented)
- `docs/Features/Equipment/player-food-ration-system.md` - Ration exchange design
- `docs/StoryBlocks/content-index.md` - Map incident definitions (45 incidents)
- `src/Features/Logistics/CompanySupplyManager.cs` - Supply simulation (9A implementation)
- `src/Features/Content/MapIncidentManager.cs` - Map incident delivery (9B implementation)
- `src/Features/Logistics/ContrabandChecker.cs` - Contraband detection (9D implementation)
- `src/Features/Company/CompanyNeedsState.cs` - Company needs tracking
- `src/Features/Content/EventCatalog.cs` - Event loading with context filtering
- `src/Features/Content/EventDeliveryManager.cs` - Event popup system
- `src/Features/Interface/Behaviors/EnlistedNewsBehavior.cs` - News system (Daily Brief, Personal Feed)
- `src/Features/Interface/News/State/CampNewsState.cs` - Persisted news state
- `src/Features/Escalation/EscalationState.cs` - Flag system for context hints
- `ModuleData/Enlisted/Events/incidents_battle.json` - Battle map incidents (3 samples)
- `ModuleData/Enlisted/Events/incidents_settlement.json` - Settlement map incidents (4 samples)
- `ModuleData/Enlisted/Events/muster_events.json` - Muster events including baggage check (9D)

---

## What's Next: Phase 10

Phase 9 is complete. The next phase covers combat XP and training enhancements.

**See:** [`phase10-combat-xp-training.md`](phase10-combat-xp-training.md)

| Phase | Feature | Description | Priority |
|-------|---------|-------------|----------|
| **10A** | Weapon-Aware Training | Dynamic skill XP based on equipped weapon | MEDIUM |
| **10B** | Robust Troop Training | NCO drill uses lord's party, validates XP gains | MEDIUM |
| **10C** | XP Feedback in News | Skill progress hints in Daily Brief | LOW |
| **10D** | Experience Track Modifiers | Green +20% / Veteran -10% training XP | MEDIUM |

**Key Integration:** Phase 10 uses Phase 9E's news system to report training outcomes and skill progress.

**Cross-Dependencies:**
- Phase 10A uses Phase 8's `reward_choices` for weapon focus selection
- Phase 10B reports training to Phase 9E's Personal Feed
- Phase 10D shares experience track code with Onboarding System (Phase 4)

---

**Status**: Phase 9 complete (all sub-phases: 9A, 9B, 9C, 9D, 9E).

