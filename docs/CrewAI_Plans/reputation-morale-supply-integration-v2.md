# Reputation-Morale-Supply Integration

**Summary:** Cross-system integration creating meaningful feedback loops between reputation tracks (Soldier: -50 to +50, Lord/Officer: 0-100), company needs (0-100), and pressure systems. Implements gradient-based content weighting, reputation-influenced need recovery, morale-influenced reputation decay, pressure arc narrative events, and synergy effects when multiple systems are stressed.

**Status:** ⚙️ Partial Implementation (Phase 3 - Supply Pressure Arc completed)

**Last Updated:** 2026-01-06 (Supply pressure arc events implemented and documented)

**Related Docs:** 
- [Company Events - Pressure Arcs](../Features/Core/company-events.md#pressure-arc-events) ✅ Implemented
- [Company Supply Simulation](../Features/Equipment/company-supply-simulation.md#supply-pressure-arc-events) ✅ Implemented
- [Test Implementation Plan (Archived)](../Archive/supply-pressure-arc-test-IMPLEMENTED.md)
- [Systems Integration Analysis](../ANEWFEATURE/systems-integration-analysis.md)
- [Architecture Improvement Report](../ANEWFEATURE/architecture-improvement-report.md)
- [Content System Architecture](../Features/Content/content-system-architecture.md)
- [Camp Simulation System](../Features/Campaign/camp-simulation-system.md)
- [Game Design Principles](../../Tools/CrewAI/knowledge/game-design-principles.md)

---

## Implementation Progress

### ✅ Completed (2026-01-06)

**Supply Pressure Arc Events:**
- 9 tier-variant events (3 stages × 3 tiers) implemented and tested
- Fires at Day 3, 5, 7 of sustained low supplies
- `CompanySimulationBehavior.CheckPressureArcEvents()` method active
- Full documentation dispersed to permanent feature docs

**Files Modified:**
- `src/Features/Camp/CompanySimulationBehavior.cs` - Added pressure arc event triggering
- `ModuleData/Enlisted/Events/pressure_arc_events.json` - 9 supply events created
- `ModuleData/Languages/enlisted_strings.xml` - Localization added

**Documentation Updated:**
- [Company Events](../Features/Core/company-events.md#pressure-arc-events)
- [Company Supply Simulation](../Features/Equipment/company-supply-simulation.md#supply-pressure-arc-events)

### ⏳ Remaining Work

- Phases 1, 2, 4 (Quick Wins, Gradient Fitness, Synergy & Polish)
- Morale and rest pressure arcs (18 more events)
- Positive arcs for high morale/supplies (8 events)
- Synergy-aware forecasts

---

## Problem Statement

### The Gap

Enlisted has **7 core tracking systems** that operate largely in isolation:
- **Company Needs** (0-100): Supplies, Morale, Rest, Readiness, Equipment
- **Reputation Tracks**: Soldier (-50 to +50), Officer (0-100), Lord (0-100)
- **Escalation Tracks** (0-10): Scrutiny, Discipline, Medical Risk (0-5)
- **Pressure Tracking**: Days low supplies/morale/rest, sustained pressure counters

### Current Problems

1. **Binary Thresholds Only**: Company needs trigger content at fixed thresholds (<20, <30) rather than continuous gradients. Morale at 35 vs 29 feels identical until you cross 30.

2. **No Cross-System Effects**: Low morale doesn't accelerate reputation decay. High reputation doesn't provide recovery bonuses. Systems feel disconnected.

3. **Pressure Arcs Invisible**: `CompanyPressure.DaysLowSupplies` is tracked but doesn't trigger progressive narrative events. Players hit crises without warning.

4. **No Synergy Effects**: Multiple simultaneous low needs don't compound. Having low supplies AND low morale feels like two separate problems rather than a spiraling crisis.

5. **Reputation Only Gates**: Reputation gates content access but doesn't weight fitness scoring. High soldier rep doesn't make social opportunities appear more often.

### Why It Matters

Players expect their actions to have ripple effects. A leader with high morale should see their reputation grow organically. A company with supplies AND morale both critically low should feel like everything is falling apart—narratively, not just numerically.

---

## Tier-Aware Player Experience

**Reference:** [Game Design Principles](../../Tools/CrewAI/knowledge/game-design-principles.md)

This feature MUST account for the three distinct player experiences in Enlisted:

### T1-T4: Enlisted Track (Grunt)
**"Things happen TO you"**

- Player witnesses company problems with limited agency to fix them
- Pressure arcs manifest as rumors, observations, peer complaints
- Player verbs: "Witness", "Report", "Participate", "Survive"
- Example: Supply crisis → "You hear fighting over scraps at the cook's fire"

### T5-T6: Officer Track (NCO)
**"You handle your squad"**

- Player has authority over their squad during crises
- Pressure arcs involve squad-level decisions
- Player verbs: "Investigate", "Mentor", "Handle", "Lead small group"
- Example: Supply crisis → "Two of YOUR soldiers are fighting over rations. Others watch you."

### T7-T9: Commander Track
**"You command troops"**

- Player makes company-wide decisions during crises
- Pressure arcs involve resource allocation, discipline, petitioning lord
- Player verbs: "Order", "Allocate", "Discipline", "Petition", "Command"
- Example: Supply crisis → "Fighting in YOUR retinue. Discipline is breaking down."

### Tier-Variant Pressure Arc Events

Each pressure arc stage needs **tier-appropriate variants**:

| Stage | T1-T4 (Grunt) | T5-T6 (NCO) | T7+ (Commander) |
|-------|---------------|-------------|-----------------|
| Day 3 | "Rations are thin. You see men eyeing each other's bowls." | "Your squad grumbles about portions. They look to you." | "Supply officer reports rationing. Your retinue needs guidance." |
| Day 5 | "A fight breaks out. Do you step in or get the NCO?" | "Two of YOUR men fighting. The squad watches how you handle it." | "Fights breaking out. Time for harsh discipline or shared sacrifice." |
| Day 7 | "You hear whispers of desertion. Report it or stay quiet?" | "One of your men is planning to desert. Confront him or let him go?" | "Desertions imminent. Petition the lord for resupply or make hard choices." |

### Design Checklist (Per Event)

Before implementing pressure arc events, verify:

- [ ] **Tier variants exist** - Same event has T1-T4, T5-T6, T7+ versions
- [ ] **Player agency matches tier** - Grunt observes, NCO handles squad, Commander decides
- [ ] **Soldier-eye view** - "Thin gruel" not "Supply level: 35%"
- [ ] **Memorable moment** - Would player tell a friend about this?
- [ ] **Positive arcs too** - Not just crises, also high-morale celebrations

### Positive Arcs (Often Forgotten)

Don't only track bad things. High morale/supplies should also trigger events:

| Stage | T1-T4 (Grunt) | T5-T6 (NCO) | T7+ (Commander) |
|-------|---------------|-------------|-----------------|
| Day 3 | "Men in good spirits. Someone starts a song." | "Your squad is in fine form. Pride swells." | "Retinue morale excellent. Veterans approach with requests." |
| Day 5 | "Roaring campfire. War stories flow freely." | "Your men boast about your leadership." | "Peak cohesion. Retinue ready for anything." |
| Day 7 | "Peak morale. Bonus event: Veteran offers mentoring." | "Peak morale. Squad bonds. Skill training opportunity." | "Peak morale. Retinue loyalty event. Named veteran emerges." |

---

## Current State Analysis

### Verified File Locations

All references below have been verified to exist:

| System | File | Purpose |
|--------|------|----------|
| Camp Opportunities | `src/Features/Camp/CampOpportunityGenerator.cs` | Generates candidate opportunities with fitness scoring |
| Company Needs | `src/Features/Company/CompanyNeedsManager.cs` | Static manager for need degradation/recovery (0-100 scale) |
| Escalation/Reputation | `src/Features/Escalation/EscalationManager.cs` | Manages reputation tracks (Soldier: -50 to +50, Lord/Officer: 0-100) and escalation (0-10) |
| Company Simulation | `src/Features/Camp/CompanySimulationBehavior.cs` | Daily background simulation, tracks pressure counters |
| Pressure Calculation | `src/Features/Content/SimulationPressureCalculator.cs` | Calculates instant pressure (0-100) from multiple sources |
| Event Delivery | `src/Features/Content/EventDeliveryManager.cs` | Central effect processor (line 518: ApplyEffects()) |

### Verified Opportunity IDs

**36 camp opportunities** exist in `ModuleData/Enlisted/Decisions/camp_opportunities.json`:
- Social: `opp_card_game`, `opp_dice_game`, `opp_war_stories`, `opp_campfire_song`, `opp_storytelling_circle`, `opp_tavern_visit`
- Recovery: `opp_rest_tent`, `opp_rest_shade`, `opp_rest_hammock`, `opp_preventive_rest`
- Economic/Supply: `opp_foraging`, `opp_trade_goods`, `opp_baggage_access`
- Training: `opp_weapon_drill`, `opp_formation_practice`, `opp_archery_range`, `opp_sparring_match`, `opp_veteran_spar`
- Medical: `opp_help_wounded`, `opp_seek_medical_care`, `opp_urgent_medical`
- Duty: `opp_volunteer_duty`, `opp_night_patrol`, `opp_equipment_maintenance`, `opp_repair_work`, `opp_ship_maintenance`
- Personal: `opp_letter_writing`, `opp_prayer_service`, `opp_meditation`, `opp_officer_audience`, `opp_mentor_recruit`
- Risky: `opp_drinking_heavy`, `opp_high_stakes_cards`, `opp_arm_wrestling`, `opp_below_deck_drinking`, `opp_sea_shanty`

### Existing Pressure Arc Events

**490 event IDs** exist across multiple event files, including relevant threshold events:
- Escalation thresholds (84 IDs in `events_escalation_thresholds.json`)
- Pay tension events (27 IDs in `events_pay_tension.json`)
- Illness onset events (28 IDs in `illness_onset.json`)
- Incident events across contexts (battle, siege, town, village, waiting)

**Note:** New pressure arc events will need to be created as part of implementation (e.g., `supply_pressure_stage_1`, `morale_pressure_stage_2`, etc.)

---

## Proposed Design

### Goal 1: Gradient-Based Content Weighting

**What:** Opportunity fitness varies smoothly with need values, not just at thresholds.

**Implementation:**
- Add `CalculateNeedGradientModifier()` method to `src/Features/Camp/CampOpportunityGenerator.cs`
- Calculates continuous 0-1 gradients based on need deficits
- Boost opportunities tagged as relevant to low needs
- Example: supplies 45 → small boost to foraging, supplies 15 → large boost

**Data Requirements:**
- Add `Tags` field to `CampOpportunity` model (e.g., `["supplies", "outdoor", "group"]`)
- Add `NeedsRelevance` dict (e.g., `{"Supplies": 0.8, "Morale": 0.1}`)
- Update existing 36 opportunities in `camp_opportunities.json` with tags

### Goal 2: Reputation ↔ Needs Feedback Loops

**What:** Low morale accelerates reputation decay, high reputation grants recovery bonuses.

**Implementation A: Morale → Reputation Decay**
- Modify `EscalationManager.ApplyPassiveDecay()` in `src/Features/Escalation/EscalationManager.cs`
- Check morale level, apply multiplier to soldier reputation decay interval
- Morale < 30 → decay 50% faster (e.g., 14 days → 7 days)
- Already uses daily tick pattern, save-compatible

**Implementation B: Reputation → Need Recovery**
- Add `ApplyReputationRecoveryBonus()` method to `src/Features/Company/CompanyNeedsManager.cs`
- High Officer Rep (+30+) → +1 morale/day (better leadership)
- High Lord Rep (+30+) → +1 supply/day (priority logistics)
- High Soldier Rep (+30+) → +1 readiness/day (peer support)
- Call after `ProcessDailyDegradation()`

### Goal 3: Pressure Arc Events

**What:** Progressive narrative events for sustained pressure (Day 1 → Day 3 → Day 5 → Crisis).

**Implementation:**
- Add `CheckPressureArcEvents()` method to `src/Features/Camp/CompanySimulationBehavior.cs`
- Use existing `CompanyPressure.DaysLowSupplies`, `DaysLowMorale`, `DaysLowRest` counters
- Fire events at thresholds (3 days, 5 days, 7 days)
- Queue events through `EventDeliveryManager.Instance.QueueEvent()`

**Content Requirements (NEW events to create):**

Pressure arc events require **tier variants** (see Tier-Aware Player Experience section above).

```json
// ModuleData/Enlisted/Events/pressure_arc_events.json
{
  "events": [
    // Supply pressure - T1-T4 (observing)
    {"id": "supply_pressure_stage_1_grunt", "requirements": {"tier": {"max": 4}}, ...},
    {"id": "supply_pressure_stage_2_grunt", "requirements": {"tier": {"max": 4}}, ...},
    {"id": "supply_crisis_grunt", "requirements": {"tier": {"max": 4}}, ...},
    
    // Supply pressure - T5-T6 (squad leadership)
    {"id": "supply_pressure_stage_1_nco", "requirements": {"tier": {"min": 5, "max": 6}}, ...},
    {"id": "supply_pressure_stage_2_nco", "requirements": {"tier": {"min": 5, "max": 6}}, ...},
    {"id": "supply_crisis_nco", "requirements": {"tier": {"min": 5, "max": 6}}, ...},
    
    // Supply pressure - T7+ (command decisions)
    {"id": "supply_pressure_stage_1_cmd", "requirements": {"tier": {"min": 7}}, ...},
    {"id": "supply_pressure_stage_2_cmd", "requirements": {"tier": {"min": 7}}, ...},
    {"id": "supply_crisis_cmd", "requirements": {"tier": {"min": 7}}, ...},
    
    // Similar tier variants for morale_pressure_* and rest_pressure_*
    // Plus POSITIVE arcs: morale_high_stage_1/2/3 with tier variants
  ]
}
```

**Event Count:** ~27 pressure arc events (3 stages × 3 tracks × 3 tiers = 27 minimum, plus positive arcs)

### Goal 4: Synergy Effects

**What:** Multiple low systems compound pressure and content selection.

**Implementation:**
- Refactor `SimulationPressureCalculator.CalculatePressure()` in `src/Features/Content/SimulationPressureCalculator.cs`
- Count needs below threshold (e.g., < 40)
- Apply synergy bonus: 2 low needs → +10 pressure, 3 low needs → +20 pressure
- Add synergy multiplier to fitness calculations

### Goal 5: Player Visibility

**What:** Surface cross-system effects through forecasts and Daily Brief.

**Implementation:**
- Add synergy-aware forecast generation to `EnlistedNewsBehavior.cs`
- Detect multiple low needs, add appropriate forecast messages
- Example: 3 low needs → "Everything is falling apart. Something must give."

---

## Data Models

### New: CrossSystemConfig (JSON)

```json
// ModuleData/Enlisted/Config/cross_system_config.json
{
  "gradientThresholds": {
    "supplyGradientStart": 50,
    "moraleGradientStart": 60,
    "restGradientStart": 50
  },
  "reputationModifiers": {
    "moraleDecayThreshold": 30,
    "moraleDecayMultiplier": 0.5,
    "reputationRecoveryBonusThreshold": 30,
    "reputationRecoveryBonusAmount": 1
  },
  "pressureArcThresholds": {
    "stage1Days": 3,
    "stage2Days": 5,
    "crisisDays": 7
  },
  "synergySettings": {
    "lowNeedsThreshold": 40,
    "synergyBonusPerAdditionalLowNeed": 10,
    "synergiesEnabledAtCount": 2
  }
}
```

### New: CrossSystemConfig C# Class

```csharp
// src/Features/Content/Models/CrossSystemConfig.cs
public class CrossSystemConfig
{
    public int SupplyGradientStart { get; set; } = 50;
    public int MoraleGradientStart { get; set; } = 60;
    public int RestGradientStart { get; set; } = 50;
    
    public int MoraleDecayThreshold { get; set; } = 30;
    public float MoraleDecayMultiplier { get; set; } = 0.5f;
    
    public int ReputationRecoveryBonusThreshold { get; set; } = 30;
    public int ReputationRecoveryBonusAmount { get; set; } = 1;
    
    public int PressureArcStage1Days { get; set; } = 3;
    public int PressureArcStage2Days { get; set; } = 5;
    public int PressureArcCrisisDays { get; set; } = 7;
    
    public int LowNeedsThreshold { get; set; } = 40;
    public int SynergyBonusPerAdditionalLowNeed { get; set; } = 10;
    public int SynergiesEnabledAtCount { get; set; } = 2;
}
```

### New: NeedGradientModifiers Struct

```csharp
// src/Features/Camp/Models/NeedGradientModifiers.cs
public struct NeedGradientModifiers
{
    public float SupplyDeficit { get; set; }      // 0-1 gradient
    public float MoraleDeficit { get; set; }      // 0-1 gradient
    public float RestDeficit { get; set; }        // 0-1 gradient
    public float SynergyMultiplier { get; set; }  // 1.0 = no synergy, 1.5+ = active
}
```

### Extended: CampOpportunity Model

```csharp
// Add to existing CampOpportunity.cs
public List<string> Tags { get; set; } = new List<string>();
public Dictionary<string, float> NeedsRelevance { get; set; } = new Dictionary<string, float>();
public Dictionary<string, float> ReputationRelevance { get; set; } = new Dictionary<string, float>();
```

---

## Integration Points

### Primary Files to Modify

| File | Changes | Risk |
|------|---------|------|
| `src/Features/Camp/CampOpportunityGenerator.cs` | Add `CalculateNeedGradientModifier()` method, integrate into `CalculateFitness()` | Low - additive scoring |
| `src/Features/Company/CompanyNeedsManager.cs` | Add `ApplyReputationRecoveryBonus()` method | Low - additive bonuses |
| `src/Features/Escalation/EscalationManager.cs` | Modify `ApplyPassiveDecay()` to check morale | Low - affect decay rate only |
| `src/Features/Camp/CompanySimulationBehavior.cs` | Add `CheckPressureArcEvents()`, call from `OnDailyTick()` | Low - event queueing |
| `src/Features/Content/SimulationPressureCalculator.cs` | Refactor `CalculatePressure()` for gradients + synergies | Medium - changes pressure calculations |
| `src/Features/Interface/Behaviors/EnlistedNewsBehavior.cs` | Add synergy-aware forecast generation | Low - additive forecasts |
| `ModuleData/Enlisted/Decisions/camp_opportunities.json` | Add tags and relevance weights to 36 opportunities | Low - schema extension |

### New Files to Create

1. `src/Features/Content/Models/CrossSystemConfig.cs` - Configuration class
2. `src/Features/Camp/Models/NeedGradientModifiers.cs` - Gradient modifiers struct
3. `src/Features/Content/Services/PressureStateService.cs` - Unified pressure access (optional, Phase 4)
4. `ModuleData/Enlisted/Config/cross_system_config.json` - Configuration file
5. `ModuleData/Enlisted/Events/pressure_arc_events.json` - 8 new pressure arc events

### Daily Tick Flow

```
Daily Tick (6:00 AM)
    │
    ├─── CompanySimulationBehavior.OnDailyTick()
    │       ├── UpdatePressureTracking()
    │       ├── CheckPressureArcEvents()  ← NEW
    │       └── Existing simulation...
    │
    ├─── EscalationManager.OnDailyTick()
    │       └── ApplyPassiveDecay()  ← MODIFIED (morale affects decay rate)
    │
    ├─── CompanyNeedsManager (via ScheduleBehavior)
    │       ├── ProcessDailyDegradation()
    │       └── ApplyReputationRecoveryBonus()  ← NEW
    │
    └─── ContentOrchestrator.OnDailyTick()
            └── ScheduleOpportunities()
                    └── CampOpportunityGenerator.GenerateCandidatesForPhase()
                            └── CalculateFitness()  ← MODIFIED (gradient modifiers)
```

---

## Implementation Phases

### Phase 1: Quick Wins (1-2 days)

**Goal:** Immediate cross-system effects with minimal code changes.

| Task | File | Effort |
|------|------|--------|
| Add morale → reputation decay modifier | `EscalationManager.cs` | 2 hours |
| Add reputation → need recovery bonuses | `CompanyNeedsManager.cs` | 4 hours |
| Create `CrossSystemConfig` class and JSON | New files | 2 hours |
| Add `LoadCrossSystemConfig()` to ConfigurationManager | `ConfigurationManager.cs` | 1 hour |

**Deliverable:** Morale and reputation now affect each other.

### Phase 2: Gradient Fitness (2-3 days)

**Goal:** Content selection responds smoothly to need levels.

| Task | File | Effort |
|------|------|--------|
| Create `NeedGradientModifiers` struct | New file | 1 hour |
| Add `CalculateNeedGradientModifier()` | `CampOpportunityGenerator.cs` | 4 hours |
| Add `Tags` and `NeedsRelevance` to CampOpportunity | `CampOpportunity.cs` | 1 hour |
| Tag 20 key opportunities with relevance | `camp_opportunities.json` | 4 hours |
| Integrate gradient layer into `CalculateFitness()` | `CampOpportunityGenerator.cs` | 2 hours |

**Deliverable:** Low supplies → more foraging opportunities appear naturally.

### Phase 3: Pressure Arcs (4-5 days) - ⚙️ PARTIALLY COMPLETE

**Goal:** Progressive narrative events for sustained pressure, with tier-appropriate player agency.

| Task | File | Status | Effort |
|------|------|--------|--------|
| Add `CheckPressureArcEvents()` to simulation | `CompanySimulationBehavior.cs` | ✅ Done | 4 hours |
| Create **supply** tier-variant pressure arc events (JSON) | `pressure_arc_events.json` | ✅ Done (9 events) | 6 hours |
| Create **morale** tier-variant pressure arc events (JSON) | `pressure_arc_events.json` | ⏳ Remaining | 6 hours |
| Create **rest** tier-variant pressure arc events (JSON) | `pressure_arc_events.json` | ⏳ Remaining | 4 hours |
| Create positive arc events (high morale) | `positive_arc_events.json` | ⏳ Remaining | 8 hours |
| Add synergy-aware forecasts | `EnlistedNewsBehavior.cs` | ⏳ Remaining | 3 hours |

**Content scope:** 
- ✅ **Completed:** 9 supply pressure arc events (3 stages × 3 tiers)
- ⏳ **Remaining:** ~26 events (18 morale/rest arcs + 8 positive arcs)

**Deliverable:** 
- ✅ Day 3/5/7 supply shortage → tier-appropriate narrative events
- ⏳ Morale and rest pressure arcs
- ⏳ Positive arcs for high morale/supplies

**See:** [Company Events - Pressure Arcs](../Features/Core/company-events.md#pressure-arc-events) for implemented supply arc details.

### Phase 4: Synergy & Polish (2 days)

**Goal:** Multiple low systems compound, full documentation.

| Task | File | Effort |
|------|------|--------|
| Refactor `CalculatePressure()` for synergies | `SimulationPressureCalculator.cs` | 4 hours |
| Add reputation-based fitness modifier | `CampOpportunityGenerator.cs` | 3 hours |
| Create `PressureStateService` (optional) | New file | 4 hours |
| Write documentation | New doc | 3 hours |
| Testing and tuning | All | 4 hours |

**Deliverable:** Complete cross-system integration with documentation.

---

## Testing Strategy

### Unit Test Scenarios

1. **Gradient Calculations**
   - Supplies 50 → gradient 0.0
   - Supplies 25 → gradient 0.5
   - Supplies 0 → gradient 1.0

2. **Morale Decay Modifier**
   - Morale 50+ → normal decay (14 days)
   - Morale 29 → accelerated decay (7 days)
   - Verify decay interval calculation

3. **Reputation Recovery Bonuses**
   - Officer Rep 29 → no bonus
   - Officer Rep 30 → +1 morale/day
   - Verify bonuses stack correctly

4. **Pressure Arc Event Firing**
   - DaysLowSupplies = 2 → no event
   - DaysLowSupplies = 3 → stage 1 event fires
   - DaysLowSupplies = 4 → no duplicate event
   - DaysLowSupplies = 5 → stage 2 event fires

5. **Synergy Detection**
   - 1 low need → no synergy
   - 2 low needs → synergy active, +10 pressure
   - 3 low needs → synergy active, +20 pressure

### Integration Test Scenarios

1. **Full Pressure Arc Playthrough**
   - Start with supplies < 30
   - Observe Day 3, 5, 7 events fire
   - Verify pressure increases
   - Verify opportunities shift to supply-related

2. **Reputation-Morale Feedback Loop**
   - High officer rep → morale recovery bonus
   - Observe morale stabilizes faster
   - Let morale drop → reputation decay accelerates

3. **Multiple Low Needs Synergy**
   - Reduce supplies, morale, rest all to 35
   - Verify combined pressure > sum of individual pressures
   - Verify synergy forecasts appear

### Save/Load Testing

1. **Save with low morale, load, verify decay continues**
2. **Save with high rep bonuses active, load, verify bonuses apply**
3. **Save mid-pressure arc (Day 4), load, verify Day 5 event fires**

---

## Open Questions

1. **Tuning Values**
   - What's the right gradient boost range? Currently 0-20, may need adjustment.
   - Should synergy multiplier scale exponentially or linearly?
   - Are pressure arc day thresholds (3/5/7) appropriate?

2. **Content Design**
   - Which opportunities should have highest supply relevance?
   - Should some opportunities have NEGATIVE relevance (e.g., drinking makes morale worse)?
   - How many pressure arc events are needed for each need type?

3. **Player Communication**
   - Should UI show "Morale affecting reputation" somewhere?
   - Should Daily Brief explicitly mention synergies?
   - Do we need a "System Status" screen showing all cross-system effects?

4. **Escalation Interaction**
   - Should high discipline also affect reputation decay?
   - Should scrutiny affect morale recovery rates?
   - Medical risk synergy with rest/morale?

5. **Phase System Coupling**
   - The orchestrator phase system is marked for future overhaul.
   - Should gradient modifiers be phase-aware? (e.g., rest opportunities boost more at night)
   - How to design for future phase system changes?

---

## Dependencies and Risks

### Technical Dependencies

- **No new external libraries** - uses existing game systems
- **Configuration loading** - requires ConfigurationManager extension
- **Event delivery** - relies on EventDeliveryManager pipeline
- **.NET 4.7.2 constraints** - no modern C# features (records, file-scoped namespaces)

### Integration Risks

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Pressure calculation changes affect content pacing | Medium | Medium | Careful tuning, beta testing |
| Gradient modifiers unbalance opportunity selection | Medium | Low | Config-driven thresholds, easy to adjust |
| Pressure arc events fire too frequently | Low | Medium | Use exact day matching, track last fired |
| Save compatibility issues | Low | High | No new persistent state, everything recalculated |
| Performance impact on fitness calculation | Low | Low | Lightweight calculations, cached config |

### Content Dependencies

- **36 opportunities need tagging** - requires content review and categorization
- **8 pressure arc events need writing** - requires narrative design
- **Forecast text needs localization keys** - requires string table entries

---

## Success Criteria

### Functional Success

- [ ] Morale < 30 accelerates soldier reputation decay
- [ ] High reputation (+30) grants +1 need recovery per day
- [ ] Opportunities fitness varies continuously with need levels (no binary jumps)
- [ ] Pressure arc events fire at Days 3, 5, 7 of sustained low needs
- [ ] Multiple low needs compound pressure (visible in calculations)
- [ ] Synergy forecasts appear when 2+ needs are low
- [ ] All changes are save/load compatible

### Player Experience Success

- [ ] Players notice content shifts smoothly as needs change
- [ ] Players see "death spiral" tension when multiple systems are low
- [ ] Players get warning events before crises (time to react)
- [ ] High reputation feels rewarding through recovery bonuses
- [ ] Low morale has visible consequences beyond content gating
- [ ] **T1-T4 players** witness and report crises (appropriate agency)
- [ ] **T5-T6 players** handle squad-level problems (NCO authority)
- [ ] **T7+ players** make command decisions (retinue leadership)
- [ ] Positive arcs feel as impactful as negative spirals
- [ ] Players would tell a friend about crisis moments

### Technical Success

- [ ] No performance degradation in fitness calculations
- [ ] Configuration values are easily tunable without code changes
- [ ] All file paths and event IDs are verified before implementation
- [ ] Code follows existing patterns (singleton managers, SyncData persistence)
- [ ] Documentation is complete and accurate

---

## Future Extensions

### Potential Phase 5+ Features

1. **Cascade Effects**
   - Event effects that trigger secondary effects based on state changes
   - Example: Supplies restored → morale boost if supplies were critical

2. **Reputation-Weighted Content**
   - High soldier rep → social opportunities appear more frequently
   - High officer rep → leadership opportunities prioritized

3. **Equipment Quality Integration**
   - Low equipment quality affects pressure and morale
   - Quartermaster system cross-system effects

4. **Long-Term Pressure Arcs**
   - Track "weeks of hardship" for major narrative milestones
   - "Breaking point" events after sustained multi-system stress

5. **Positive Feedback Loops**
   - "High spirits" when morale + supplies + reputation all high
   - Bonus content and opportunities during peak performance

---

## Summary

This feature creates **meaningful feedback loops** between Enlisted's core tracking systems:

1. **Morale affects reputation decay** - Low morale accelerates soldier rep decay
2. **Reputation affects recovery** - High rep grants bonus need recovery
3. **Needs affect content gradient** - Low needs smoothly boost relevant opportunities
4. **Sustained pressure creates narrative arcs** - Progressive events before crises
5. **Multiple low needs compound** - Synergy effects create "death spiral" tension

All changes follow existing patterns (singleton managers, SyncData persistence, daily tick processing) and maintain save compatibility. Configuration is externalized to JSON for easy tuning.

**Estimated Total Effort:** 10-14 days (increased for tier-variant content)
**Risk Level:** Low-Medium (mostly additive changes, well-isolated)
**Player Impact:** High (tier-appropriate agency during crises, memorable "tell a friend" moments)

### Tier-Aware Design Verification

Before implementation, verify each pressure arc event against the [Game Design Principles](../../Tools/CrewAI/knowledge/game-design-principles.md):

- [ ] T1-T4 events have "witness/report/participate" verbs
- [ ] T5-T6 events have "investigate/mentor/handle squad" verbs  
- [ ] T7+ events have "order/allocate/command" verbs
- [ ] Soldier-eye view (narrative, not numbers)
- [ ] Positive arcs exist (not just death spirals)
- [ ] Each event passes the "tell a friend" test

---

## Verification Registry (Reference)

**All file paths and IDs below have been verified to exist as of 2026-01-08:**

### Verified C# Files
- `src/Features/Camp/CampOpportunityGenerator.cs` ✅
- `src/Features/Company/CompanyNeedsManager.cs` ✅
- `src/Features/Escalation/EscalationManager.cs` ✅
- `src/Features/Camp/CompanySimulationBehavior.cs` ✅
- `src/Features/Content/EventDeliveryManager.cs` ✅
- `src/Features/Content/SimulationPressureCalculator.cs` ✅

### Verified JSON Folders
- `ModuleData/Enlisted/Decisions/` - 148 IDs (36 camp opportunities)
- `ModuleData/Enlisted/Events/` - 490 IDs (15 files)
- `ModuleData/Enlisted/Orders/order_events/` - 327 IDs (16 files)

### Sample Verified Opportunity IDs
- Social: `opp_card_game`, `opp_war_stories`, `opp_campfire_song`, `opp_tavern_visit`
- Recovery: `opp_rest_tent`, `opp_rest_shade`, `opp_preventive_rest`
- Economic: `opp_foraging`, `opp_trade_goods`, `opp_baggage_access`
- Training: `opp_weapon_drill`, `opp_formation_practice`, `opp_archery_range`
- Medical: `opp_help_wounded`, `opp_seek_medical_care`, `opp_urgent_medical`

**Note:** New event IDs (`supply_pressure_stage_1`, etc.) will be created during implementation and are NOT in the current codebase.

---

**End of Planning Document**
