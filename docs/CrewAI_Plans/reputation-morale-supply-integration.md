# Reputation-Morale-Supply Integration

**Summary:** Integrate reputation, morale, supply, and pressure systems to create meaningful feedback loops. High morale amplifies reputation gains (+20%), low supplies trigger reputation penalties, and pressure drives relevant content selection. Expands lord reputation gains to camp events and adds soldier reputation recovery paths.

**Status:** 📋 Planning

**Last Updated:** 2025-01-XX (Initial technical specification)

**Related Docs:** [EscalationManager](../Features/Escalation/escalation-system.md), [CompanyNeedsManager](../Features/Company/company-needs.md), [ContentOrchestrator](../Features/Content/content-system-architecture.md), [SimulationPressureCalculator](../Features/Content/simulation-pressure.md)

---

## Problem Statement

### Current State Issues

Players cannot meaningfully gain or lose reputation through normal gameplay:

| Track | Current Sources | Problem |
|-------|-----------------|---------|  
| **Lord Reputation** | Only order completion bonuses | Camp events (29 total) have no lordRep effects |
| **Soldier Reputation** | 2 events only | Negative soldierRep has no recovery path |
| **Reputation Gains** | Fixed values | Morale state doesn't affect gain amounts |
| **Supply Impact** | Crisis events only | Low supplies don't degrade reputation over time |

### Systems Currently Siloed

```
Current Flow (Disconnected):
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│  Company Needs  │    │   Escalation    │    │    Pressure     │
│  (Morale/Supply)│    │  (Reputation)   │    │   Calculator    │
└────────┬────────┘    └────────┬────────┘    └────────┬────────┘
         │                      │                      │
         ▼                      ▼                      ▼
    Crisis Events         Event Effects         Content Pacing
         │                      │                      │
    (No cross-talk)        (No modifiers)        (Reads but doesn't act)
```

### Goal State

```
Target Flow (Integrated):
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│  Company Needs  │◄──►│   Escalation    │◄──►│    Pressure     │
│  (Morale/Supply)│    │  (Reputation)   │    │   Calculator    │
└────────┬────────┘    └────────┬────────┘    └────────┬────────┘
         │                      │                      │
         ├──────────────────────┼──────────────────────┤
         ▼                      ▼                      ▼
┌─────────────────────────────────────────────────────────────────┐
│                    SystemsIntegrationManager                     │
│  • Daily cross-system effects                                    │
│  • Reputation gain modifiers based on morale                     │
│  • Supply-based reputation decay                                 │
│  • Compound pressure detection                                   │
└─────────────────────────────────────────────────────────────────┘
```

---

## Design Goals

| ID | Goal | Metric |
|----|------|--------|
| G1 | High morale amplifies reputation gains | +20% at morale ≥70 |
| G2 | Low morale diminishes reputation gains | -30% at morale <30 |
| G3 | Low supplies cause reputation decay | -1/day when supplies <20 |
| G4 | Compound pressure creates narrative tension | New pressure source for low rep + low morale |
| G5 | Camp events can affect lord reputation | Add lordRep to 8+ camp events |
| G6 | Soldier reputation has recovery paths | Add 4+ events with +soldierRep |
| G7 | System respects existing constraints | No hard game-over, mod-compatible |

---

## Current System Analysis

### Relevant Files and Their Responsibilities

#### Core System Files

| File | Location | Responsibility |
|------|----------|----------------|
| **EscalationManager.cs** | `src/Features/Escalation/EscalationManager.cs` | Manages all escalation and reputation tracks. Handles passive decay, threshold events, UI notifications, and save/load. |
| **EscalationState.cs** | `src/Features/Escalation/EscalationState.cs` | Data container for escalation values (Scrutiny, Discipline, MedicalRisk, SoldierRep, OfficerRep, LordRep) |
| **CompanyNeedsManager.cs** | `src/Features/Company/CompanyNeedsManager.cs` | Static utility for processing daily degradation, checking critical thresholds, predicting upcoming needs |
| **CompanyNeedsState.cs** | `src/Features/Company/CompanyNeedsState.cs` | Data container for company need values (Supplies, Morale, Rest, Readiness) |
| **SimulationPressureCalculator.cs** | `src/Features/Content/SimulationPressureCalculator.cs` | Aggregates pressure from multiple systems (needs, escalation, world state) to calculate overall simulation pressure |
| **ContentOrchestrator.cs** | `src/Features/Content/ContentOrchestrator.cs` | Central coordinator for content pacing, schedules opportunities, fires crisis events |

### Data Flow Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           DAILY TICK TRIGGER                                 │
│                     (CampaignEvents.DailyTickEvent)                          │
└─────────────────────────────┬───────────────────────────────────────────────┘
                              │
      ┌───────────────────────┼───────────────────────┐
      ▼                       ▼                       ▼
┌─────────────────┐    ┌─────────────────────┐    ┌──────────────────┐
│ EscalationManager │    │CompanySimulationBhv│    │ContentOrchestrator│
│   OnDailyTick()   │    │    OnDailyTick()    │    │   OnDailyTick()   │
└────────┬──────────┘    └──────────┬──────────┘    └────────┬─────────┘
         │                          │                         │
         ▼                          │                         │
┌─────────────────────┐             │                         │
│ ApplyPassiveDecay() │             │                         │
│ - Scrutiny decay    │             │                         │
│ - Discipline decay  │             │                         │
│ - Rep drift to 50   │             │                         │
└─────────────────────┘             │                         │
                                    │                         │
                                    ▼                         │
                        ┌─────────────────────┐               │
                        │ProcessDailyDegrad() │               │
                        │(CompanyNeedsManager)│               │
                        │ - Readiness -2      │               │
                        │ - Morale -1         │               │
                        │ - Rest -4           │               │
                        └──────────┬──────────┘               │
                                   │                          │
                                   ▼                          │
                        ┌─────────────────────┐               │
                        │ Check Critical      │               │
                        │ Needs Thresholds    │               │
                        │ (< 20% = CRITICAL)  │               │
                        └──────────┬──────────┘               │
                                   │                          │
      ┌────────────────────────────┼──────────────────────────┤
      │                            │                          │
      ▼                            ▼                          ▼
┌─────────────────┐     ┌─────────────────┐      ┌───────────────────┐
│Supplies Critical│     │Morale Critical  │      │WorldStateAnalyzer │
│QueueCrisisEvent │     │QueueCrisisEvent │      │AnalyzeSituation() │
└─────────────────┘     └─────────────────┘      └───────────────────┘
```

---

## Proposed Design

### New Data Models

#### SystemsIntegrationConfig

```csharp
// Location: src/Features/Integration/SystemsIntegrationConfig.cs

namespace Enlisted.Features.Integration
{
    /// <summary>
    /// Configuration for cross-system integration mechanics.
    /// Loaded from ModuleData/Enlisted/Config/systems_integration_config.json
    /// </summary>
    public class SystemsIntegrationConfig
    {
        // === Morale-Reputation Modifiers ===
        
        /// <summary>
        /// Morale threshold above which reputation gains are amplified.
        /// Default: 70 (Good+ morale)
        /// </summary>
        public int HighMoraleThreshold { get; set; } = 70;
        
        /// <summary>
        /// Multiplier applied to reputation gains when morale is high.
        /// 1.2 = +20% reputation gains.
        /// </summary>
        public float HighMoraleReputationMultiplier { get; set; } = 1.2f;
        
        /// <summary>
        /// Morale threshold below which reputation gains are diminished.
        /// Default: 30 (Poor morale)
        /// </summary>
        public int LowMoraleThreshold { get; set; } = 30;
        
        /// <summary>
        /// Multiplier applied to reputation gains when morale is low.
        /// 0.7 = -30% reputation gains.
        /// </summary>
        public float LowMoraleReputationMultiplier { get; set; } = 0.7f;
        
        // === Supply-Reputation Effects ===
        
        /// <summary>
        /// Supplies threshold below which daily reputation decay occurs.
        /// Default: 20 (Critical supplies)
        /// </summary>
        public int CriticalSuppliesThreshold { get; set; } = 20;
        
        /// <summary>
        /// Daily soldier reputation loss when supplies are critical.
        /// Applied once per day during daily tick.
        /// </summary>
        public int SupplyShortageReputationPenalty { get; set; } = -1;
        
        /// <summary>
        /// Days of critical supplies before lord notices (affects lord rep).
        /// Default: 3 days
        /// </summary>
        public int DaysBeforeLordNotices { get; set; } = 3;
        
        /// <summary>
        /// Lord reputation penalty after sustained supply shortage.
        /// Applied after DaysBeforeLordNotices consecutive days.
        /// </summary>
        public int SustainedShortageLordPenalty { get; set; } = -2;
        
        // === Compound Pressure Settings ===
        
        /// <summary>
        /// Reputation threshold for "disliked" state that compounds with low morale.
        /// Default: 30
        /// </summary>
        public int DislikedReputationThreshold { get; set; } = 30;
        
        /// <summary>
        /// Morale threshold that compounds with low reputation.
        /// Default: 40
        /// </summary>
        public int DemoralizingMoraleThreshold { get; set; } = 40;
        
        /// <summary>
        /// Additional pressure when both reputation is low AND morale is low.
        /// Default: 20 (significant compound effect)
        /// </summary>
        public int CompoundPressureBonus { get; set; } = 20;
        
        // === Feature Flags ===
        
        /// <summary>
        /// Whether to apply morale modifiers to reputation gains.
        /// </summary>
        public bool EnableMoraleReputationModifiers { get; set; } = true;
        
        /// <summary>
        /// Whether critical supplies cause reputation decay.
        /// </summary>
        public bool EnableSupplyReputationEffects { get; set; } = true;
        
        /// <summary>
        /// Whether to calculate compound pressure from multiple systems.
        /// </summary>
        public bool EnableCompoundPressure { get; set; } = true;
    }
}
```

#### SystemsIntegrationState

```csharp
// Location: src/Features/Integration/SystemsIntegrationState.cs

namespace Enlisted.Features.Integration
{
    /// <summary>
    /// Persistent state for cross-system integration tracking.
    /// Saved/loaded with campaign.
    /// </summary>
    [SaveableClass(300)]
    public class SystemsIntegrationState
    {
        /// <summary>
        /// Consecutive days with critical supplies (<20).
        /// Resets when supplies recover above threshold.
        /// </summary>
        [SaveableProperty(1)]
        public int ConsecutiveLowSupplyDays { get; set; }
        
        /// <summary>
        /// Consecutive days with low morale (<30).
        /// Used for compound effect tracking.
        /// </summary>
        [SaveableProperty(2)]
        public int ConsecutiveLowMoraleDays { get; set; }
        
        /// <summary>
        /// Day number when lord was last notified of supply issues.
        /// Prevents spamming lord reputation penalties.
        /// </summary>
        [SaveableProperty(3)]
        public int LastLordSupplyNotificationDay { get; set; } = -1;
        
        /// <summary>
        /// Total reputation gained this campaign (before modifiers).
        /// Used for analytics and potential achievements.
        /// </summary>
        [SaveableProperty(4)]
        public int TotalReputationGained { get; set; }
        
        /// <summary>
        /// Total reputation lost to supply shortages.
        /// Used for analytics.
        /// </summary>
        [SaveableProperty(5)]
        public int TotalSupplyPenalties { get; set; }
    }
}
```

### Core Manager Class

#### SystemsIntegrationManager (Abbreviated)

```csharp
// Location: src/Features/Integration/SystemsIntegrationManager.cs

namespace Enlisted.Features.Integration
{
    /// <summary>
    /// Manages cross-system integration between reputation, morale, supply, and pressure.
    /// Creates feedback loops: morale affects reputation gains, supplies affect reputation decay,
    /// and compound states create additional pressure.
    /// </summary>
    public class SystemsIntegrationManager : CampaignBehaviorBase
    {
        private const string LogCategory = "SystemsIntegration";
        
        private SystemsIntegrationConfig _config;
        private SystemsIntegrationState _state;
        
        public static SystemsIntegrationManager Instance { get; private set; }
        public SystemsIntegrationState State => _state;
        public SystemsIntegrationConfig Config => _config;
        
        // Key Methods:
        // - OnDailyTick(): Process daily cross-system effects
        // - ProcessSupplyReputationEffects(): Handle supply shortage penalties
        // - ApplyMoraleModifier(int baseDelta): Scale reputation gains by morale
        // - CalculateCompoundPressure(): Detect low rep + low morale
        // - GetMoraleModifierDescription(): UI display of current modifier
    }
}
```

### EscalationManager Extensions

```csharp
// Add to src/Features/Escalation/EscalationManager.cs

/// <summary>
/// Modifies soldier reputation with morale-based scaling.
/// High morale amplifies gains; low morale diminishes them.
/// </summary>
public int ModifySoldierReputationWithMorale(int baseDelta, string reason = null, bool ignoreMoraleModifier = false)
{
    if (!IsEnabled())
        return 0;
    
    int actualDelta = baseDelta;
    
    if (!ignoreMoraleModifier && baseDelta != 0)
    {
        actualDelta = SystemsIntegrationManager.Instance?.ApplyMoraleModifier(baseDelta) ?? baseDelta;
    }
    
    ModifySoldierReputation(actualDelta, reason);
    return actualDelta;
}

// Similar methods for ModifyOfficerReputationWithMorale() and ModifyLordReputationWithMorale()
```

### SimulationPressureCalculator Integration

```csharp
// Modify src/Features/Content/SimulationPressureCalculator.cs

public static SimulationPressure CalculatePressure()
{
    float pressure = 0;
    var sources = new List<string>();

    // ... existing company needs pressure ...
    // ... existing escalation pressure ...
    // ... existing health/location pressure ...
    
    // NEW: Compound pressure from multiple systems
    var integration = SystemsIntegrationManager.Instance;
    if (integration != null)
    {
        var (compoundPressure, compoundSource) = integration.CalculateCompoundPressure();
        if (compoundPressure > 0 && !string.IsNullOrEmpty(compoundSource))
        {
            pressure += compoundPressure;
            sources.Add(compoundSource);
        }
    }
    
    return new SimulationPressure
    {
        Value = Math.Min(100, (int)pressure),
        Sources = sources
    };
}
```

---

## Integration Points

### File Change List

#### New Files to Create

| File | Purpose |
|------|---------|  
| `src/Features/Integration/SystemsIntegrationManager.cs` | Core manager class with daily tick processing |
| `src/Features/Integration/SystemsIntegrationConfig.cs` | Configuration data model |
| `src/Features/Integration/SystemsIntegrationState.cs` | Persistent state data model |
| `ModuleData/Enlisted/Config/systems_integration_config.json` | Default configuration values |

#### C# Files to Modify

| File | Changes |
|------|---------|  
| `src/Features/Escalation/EscalationManager.cs` | Add `*WithMorale()` methods (3 new methods) |
| `src/Features/Content/SimulationPressureCalculator.cs` | Add compound pressure call in `CalculatePressure()` |
| `src/Features/Content/EventEffectApplier.cs` | Route reputation effects through `*WithMorale()` methods |
| `src/Enlisted.csproj` | Add new files to compilation |
| `src/SubModule.cs` | Register `SystemsIntegrationManager` behavior |

#### JSON Content Files to Modify

| File | Changes |
|------|---------|  
| `ModuleData/Enlisted/Decisions/camp_opportunities.json` | Add `lordRep` effects to 8+ events |
| `ModuleData/Enlisted/Events/events_camp.json` | Add `soldierRep` recovery options |
| `ModuleData/Enlisted/Events/events_escalation_thresholds.json` | Add reputation recovery events |

---

## Content Changes Required

### Camp Events Needing lordRep Effects

| Event ID | Current Effects | Proposed Addition | Rationale |
|----------|-----------------|-------------------|-----------|  
| `opp_help_sergeant` | +3 officerRep | +1 lordRep | Officers report to lord |
| `opp_volunteer_duty` | +2 soldierRep | +1 lordRep | Volunteering shows commitment |
| `opp_successful_patrol` | +morale | +2 lordRep | Completing duties impresses |
| `opp_defend_comrade` | +5 soldierRep | +1 lordRep | Loyalty valued by nobility |
| `opp_report_deserter` | +scrutiny | +3 lordRep | Prevents losses to lord |
| `opp_save_supplies` | +supplies | +2 lordRep | Protects lord's investment |
| `opp_train_recruits` | +readiness | +1 lordRep | Improves his army |
| `opp_lead_work_party` | +rest | +1 lordRep | Leadership noted |

### New Events for Soldier Rep Recovery

| Event ID | Trigger | Effects | Description |
|----------|---------|---------|-------------|
| `evt_make_amends` | soldierRep < 20 | +5 soldierRep, -50 gold | Buy drinks, make peace |
| `evt_prove_yourself` | soldierRep < 30 | +8 soldierRep, -10 rest | Volunteer for hard duty |
| `evt_earn_respect` | after battle, soldierRep < 40 | +5 soldierRep | Fighting together heals rifts |
| `evt_comrade_vouches` | random, soldierRep 20-40 | +3 soldierRep | Veteran speaks for you |

---

## Implementation Phases

### Phase 1: Core Infrastructure (3-4 hours)
**Files:** New C# files, csproj updates

1. Create `src/Features/Integration/` folder
2. Implement `SystemsIntegrationConfig.cs` with all properties
3. Implement `SystemsIntegrationState.cs` with SaveSystem attributes
4. Implement `SystemsIntegrationManager.cs` skeleton:
   - Constructor, singleton pattern
   - `RegisterEvents()` with DailyTick
   - `SyncData()` with SafeSyncData
   - `LoadConfig()` with fallback
5. Register behavior in `SubModule.cs`
6. Add files to `Enlisted.csproj`
7. **Test:** Build succeeds, manager initializes on campaign start

### Phase 2: Morale-Reputation Modifier (2-3 hours)
**Files:** EscalationManager.cs, SystemsIntegrationManager.cs

1. Add `ApplyMoraleModifier()` to SystemsIntegrationManager
2. Add `ModifySoldierReputationWithMorale()` to EscalationManager
3. Add `ModifyOfficerReputationWithMorale()` to EscalationManager
4. Add `ModifyLordReputationWithMorale()` to EscalationManager
5. Create `systems_integration_config.json` with defaults
6. **Test:** 
   - Set morale to 80, apply +10 rep → should get +12
   - Set morale to 20, apply +10 rep → should get +7

### Phase 3: Supply-Reputation Effects (2-3 hours)
**Files:** SystemsIntegrationManager.cs

1. Implement `ProcessSupplyReputationEffects()` in daily tick
2. Add consecutive day tracking
3. Add lord notification after threshold days
4. Add analytics tracking (TotalSupplyPenalties)
5. **Test:**
   - Set supplies to 15, advance 1 day → soldierRep -1
   - Advance 3 more days → lordRep -2
   - Set supplies to 50, advance 1 day → counter resets

### Phase 4: Compound Pressure (1-2 hours)
**Files:** SystemsIntegrationManager.cs, SimulationPressureCalculator.cs

1. Implement `CalculateCompoundPressure()` in SystemsIntegrationManager
2. Modify `CalculatePressure()` in SimulationPressureCalculator to call it
3. **Test:**
   - Set soldierRep=25, morale=35 → "Disliked and Demoralized" appears in sources
   - Set soldierRep=50 → compound pressure disappears

### Phase 5: Content Updates (3-4 hours)
**Files:** camp_opportunities.json, events_camp.json, events_escalation_thresholds.json

1. Add `lordRep` effects to 8 camp opportunities
2. Create 4 new soldier rep recovery events
3. Add event requirements for recovery events (e.g., soldierRep < 30)
4. Run content validator
5. **Test:** Play through camp events, verify new effects apply

### Phase 6: EventEffectApplier Integration (2 hours)
**Files:** EventEffectApplier.cs

1. Route `soldierRep`, `officerRep`, `lordRep` effects through `*WithMorale()` methods
2. Add logging for modifier application
3. **Test:** Event with +5 soldierRep shows correct modified value in news

### Phase 7: Documentation & Polish (2 hours)
**Files:** All documentation files listed above

1. Update systems-integration-analysis.md with IMPLEMENTED status
2. Document feedback loop mechanics in core-gameplay.md
3. Add morale modifier info to content-effects-reference.md
4. Add UI hint for morale modifier status

---

## Testing Checklist

### Unit Tests
- [ ] `ApplyMoraleModifier` returns correct values at thresholds
- [ ] Consecutive day tracking increments/resets correctly
- [ ] Compound pressure calculates only when both conditions met
- [ ] Null safety: no crashes when singletons unavailable

### Integration Tests
- [ ] Daily tick processes all effects in correct order
- [ ] Save/load preserves integration state
- [ ] Old saves initialize fresh state without errors
- [ ] Config file changes apply after reload

### Gameplay Tests
- [ ] High morale (+70) visibly increases reputation gains
- [ ] Low morale (<30) visibly reduces reputation gains
- [ ] Critical supplies cause daily reputation loss
- [ ] Lord reputation drops after sustained shortage
- [ ] Camp events now show lordRep effects
- [ ] Soldier rep recovery events appear when rep is low
- [ ] Compound pressure appears in pressure calculator sources

### Performance Tests
- [ ] Daily tick completes in <1ms
- [ ] Morale modifier lookup doesn't cause frame drops
- [ ] No memory leaks from state tracking

---

## Configuration Reference

### systems_integration_config.json

```json
{
  "highMoraleThreshold": 70,
  "highMoraleReputationMultiplier": 1.2,
  "lowMoraleThreshold": 30,
  "lowMoraleReputationMultiplier": 0.7,
  "criticalSuppliesThreshold": 20,
  "supplyShortageReputationPenalty": -1,
  "daysBeforeLordNotices": 3,
  "sustainedShortageLordPenalty": -2,
  "dislikedReputationThreshold": 30,
  "demoralizingMoraleThreshold": 40,
  "compoundPressureBonus": 20,
  "enableMoraleReputationModifiers": true,
  "enableSupplyReputationEffects": true,
  "enableCompoundPressure": true
}
```

---

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Morale modifier feels unfair | Player frustration | Clear UI indicator showing modifier; only affects gains, not losses |
| Supply penalty too harsh | Death spiral | Config tunable; lord penalty has cooldown; supplies recoverable |
| Compound pressure overwhelming | Content spam | Pressure capped at 100; compound is additive, not multiplicative |
| Save compatibility | Crashes on old saves | SafeSyncData wrapper; null state initializes defaults |
| Performance on daily tick | Frame hitches | All lookups O(1); no loops over large collections |

---

## Open Questions

### Design Questions

1. **Morale Modifier Scope**: Should morale affect ALL reputation changes (including penalties), or only gains?
   - **Current Design**: Only affects gains (positive delta)
   - **Rationale**: Prevents exploiting low morale to reduce penalties
   - **Alternative**: Apply to all changes for consistency

2. **Supply Decay Rate**: Is -1
