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
- JSON source: `ModuleData/Enlisted/Decisions/camp_opportunities.json` (29 opportunities)
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

| System | Location |
|--------|----------|
| Content Orchestrator | `src/Features/Content/ContentOrchestrator.cs` |
| Camp Opportunities | `src/Features/Camp/CampOpportunityGenerator.cs` |
| Enlistment | `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs` |
| Orders | `src/Features/Orders/Behaviors/OrderProgressionBehavior.cs` |
| Events JSON | `ModuleData/Enlisted/Events/` |
| Decisions JSON | `ModuleData/Enlisted/Decisions/` |
| Order Events JSON | `ModuleData/Enlisted/Orders/order_events/` |
| Config | `ModuleData/Enlisted/Config/` |
