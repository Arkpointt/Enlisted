# Enlisted Systems Overview

## Core Systems

### Content Orchestrator
- Location: `src/Features/Content/ContentOrchestrator.cs`
- Purpose: Central coordinator for content pacing - analyzes world state, pre-schedules opportunities, coordinates all content delivery
- Key methods:
  - `ScheduleOpportunities()` - Pre-schedules 24h ahead at daily tick
  - `GetCurrentPhaseOpportunities()` - Returns locked opportunities for menu
  - `ConsumeOpportunity()` - Marks opportunity as used
  - `CheckForScheduleOverride()` - Need-based/variety overrides
- Related: `WorldStateAnalyzer.cs`, `SimulationPressureCalculator.cs`, `PlayerBehaviorTracker.cs`

### Enlistment System
- Location: `src/Features/Enlistment/`
- Entry point: `EnlistmentBehavior.cs`
- Key properties: `IsEnlisted`, `CurrentLord`, `DaysServed`, `FatigueCurrent`, `CompanyNeeds`
- Tracks: tier (1-9), role, reputation tracks (soldier/officer/lord)

### Camp Opportunity System
- Location: `src/Features/Camp/CampOpportunityGenerator.cs`
- Purpose: Generates candidate opportunities for each DayPhase
- Key method: `GenerateCandidatesForPhase(DayPhase)` - Returns candidates filtered by phase
- JSON source: `ModuleData/Enlisted/Decisions/camp_opportunities.json` (36 opportunities)
- Fitness scoring considers: player preferences, pressure states, variety

### Order Progression System
- Location: `src/Features/Orders/Behaviors/OrderProgressionBehavior.cs`
- Purpose: Multi-day orders with event injection based on activity level
- 17 order types, 84 order events in `ModuleData/Enlisted/Orders/order_events/`
- Activity levels: Quiet (×0.3), Routine (×0.6), Active (×1.0), Intense (×1.5)

### Event Delivery System
- Location: `src/Features/Content/EventDeliveryManager.cs`
- Purpose: Pipeline for queuing and showing events, applying effects
- Related: `EventCatalog.cs`, `EventSelector.cs`, `EventRequirementChecker.cs`

**Effect Application Details:**
- `ApplyEffects()` method at line 518 is the central effect processor
- Reputation changes: `lordRep` (line 613-617), `officerRep` (line 620-624), `soldierRep` (line 627-631)
- Escalation changes: `scrutiny` (635), `discipline` (641), `medicalRisk` (647)
- Gold: line 654-667, HP: line 670-684, CompanyNeeds: line 728-740
- News integration: `NotifyNewsOfEventOutcome()` at line 459 reports significant changes to `EnlistedNewsBehavior`
- Only soldierRep shows inline UI notifications; other rep changes are silent

**Integration Points for Reputation:**
- Primary: `EventDeliveryManager.ApplyEffects()` - processes JSON event effects
- Secondary: `OrderManager.cs` lines 809, 814, 819 - order completion bonuses
- `EscalationManager.cs` line 517-553 - `ApplyPassiveDecay()` daily reputation decay
- `EscalationManager.cs` line 677-697 - `CheckThresholdCrossing()` triggers milestone events

### Escalation System
- Location: `src/Features/Escalation/EscalationManager.cs`
- Purpose: Tracks escalation tracks and reputation systems
- **Escalation tracks (0-10):** Scrutiny (officer attention), Discipline (enforcement level)
- **Escalation tracks (0-5):** MedicalRisk (illness onset threshold at 3+)
- **Reputation tracks:**
  - **SoldierReputation (-50 to +50):** Peer standing, can be negative (hated) or positive (bonded)
  - **LordReputation (0-100):** Trust and loyalty with lord, starts neutral, only goes up
  - **OfficerReputation (0-100):** Perceived potential by NCOs/officers, starts neutral, only goes up
- **Reputation thresholds:** Varies by track - see EscalationState.cs for exact ranges
- Daily passive decay, threshold events trigger at milestones
- Related: `EscalationState.cs`, threshold event JSONs in `Events/`

### Company Needs System
- Location: `src/Features/Company/CompanyNeedsManager.cs`
- Purpose: Manages company need degradation and recovery (0-100 scale)
- **Needs tracks:** Supplies, Morale, Rest, Readiness, Equipment
- **Daily degradation:** Supplies (handled by CompanySupplyManager), Readiness (-2), Morale (-1), Rest (-4)
- **Accelerated degradation:** Long marches (+5 rest/readiness), Low morale (+3 readiness)
- **Status levels:** Excellent (80+), Good (60-79), Fair (40-59), Poor (20-39), Critical (<20)
- **Thresholds:** Critical <20 (crisis events), Low 20-40 (pressure), Normal 40-70, Good >70
- Related: `CompanyNeedsState.cs`, `CompanySimulationBehavior.cs`

### Company Simulation System
- Location: `src/Features/Camp/CompanySimulationBehavior.cs`
- Purpose: Background daily simulation - soldiers get sick, desert, recover
- Tracks: sick count, missing count, casualties, pressure accumulation
- **Pressure tracking:** Days low supplies/morale/rest, days high sickness, recent desertions
- Feeds news system and provides context for orchestrator
- Related: `CompanyRoster.cs`, `CompanyPressure.cs`, `SimulationConfig.json`

### Simulation Pressure System
- Location: `src/Features/Content/SimulationPressureCalculator.cs`
- Purpose: Calculates current pressure (0-100) from multiple sources
- **Pressure sources:**
  - Low supplies (<30): +20 pressure
  - Exhausted company (<30 rest): +15 pressure
  - Low morale (<30): +15 pressure
  - High discipline (>7): +25 pressure
  - High scrutiny (>7): +20 pressure
  - High medical risk (>3): +15 pressure
  - Wounded player (<50% HP): +15 pressure
  - Enemy territory: +15 pressure
  - Reputation-blocked promotion: +5-15 pressure
- Used by orchestrator for content pacing and variety overrides

## Key Enums

### DayPhase (`OrchestratorEnums.cs`)
- `Dawn` (0), `Midday` (1), `Dusk` (2), `Night` (3)
- Used for opportunity scheduling and phase transitions

**⚠️ FUTURE EVOLUTION PLANNED:**
The orchestrator phase system and time-of-day mechanics will receive a major overhaul.
Current implementation (4 phases) is functional but expected to change.
**Design philosophy:** When proposing changes, consider this system is not final.

### ActivityLevel
- `Quiet` - Garrison + peacetime
- `Routine` - Garrison + war, marching
- `Active` - Campaign + war
- `Intense` - Siege, desperate war

### LordSituation
- `PeacetimeGarrison`, `WarMarching`, `WarActiveCampaign`
- `SiegeAttacking`, `SiegeDefending`
- `Defeated`, `Captured`

## File Locations

|| System | Location |
||--------|----------|
|| Content Orchestrator | `src/Features/Content/ContentOrchestrator.cs` |
|| Camp Opportunities | `src/Features/Camp/CampOpportunityGenerator.cs` |
|| Enlistment | `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs` |
|| Orders | `src/Features/Orders/Behaviors/OrderProgressionBehavior.cs` |
|| Escalation Manager | `src/Features/Escalation/EscalationManager.cs` |
|| Company Needs Manager | `src/Features/Company/CompanyNeedsManager.cs` |
|| Company Simulation | `src/Features/Camp/CompanySimulationBehavior.cs` |
|| Pressure Calculator | `src/Features/Content/SimulationPressureCalculator.cs` |
|| Events JSON | `ModuleData/Enlisted/Events/` |
|| Decisions JSON | `ModuleData/Enlisted/Decisions/` |
|| Order Events JSON | `ModuleData/Enlisted/Orders/order_events/` |
|| Config | `ModuleData/Enlisted/Config/` |
