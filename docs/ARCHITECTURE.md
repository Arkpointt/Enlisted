# Architecture Guide

## Overview

This document describes the architecture of the Enlisted mod following the Package-by-Feature blueprint. The design prioritizes maintainability, testability, and resilience to game updates.

## High-Level Architecture

```
┌─────────────────────────────────────────────────┐
│                 Bannerlord Game                 │
│                                                 │
│  ┌─────────────┐  ┌──────────────────────────┐  │
│  │ Campaign    │  │ UI Systems              │  │
│  │ System      │  │ (Menus, Nameplates)     │  │
│  └─────────────┘  └──────────────────────────┘  │
└─────────────────────────┬───────────────────────┘
                          │ TaleWorlds APIs
┌─────────────────────────▼───────────────────────┐
│               GameAdapters Layer                │
│                                                 │
│  ┌─────────────┐  ┌──────────────────────────┐  │
│  │ Harmony     │  │ Campaign Event          │  │
│  │ Patches     │  │ Handlers                │  │
│  └─────────────┘  └──────────────────────────┘  │
└─────────────────────────┬───────────────────────┘
                          │ Domain Events
┌─────────────────────────▼───────────────────────┐
│                Feature Modules                  │
│                                                 │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────┐  │
│  │ Enlistment  │  │ Promotion   │  │ Wages   │  │
│  │ Feature     │  │ Feature     │  │ Feature │  │
│  └─────────────┘  └─────────────┘  └─────────┘  │
└─────────────────────────┬───────────────────────┘
                          │ Shared Services
┌─────────────────────────▼───────────────────────┐
│                 Core Layer                      │
│                                                 │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────┐  │
│  │ Config      │  │ Persistence │  │ Utils   │  │
│  │ System      │  │ (Save/Load) │  │         │  │
│  └─────────────┘  └─────────────┘  └─────────┘  │
└─────────────────────────────────────────────────┘
```

## Layer Responsibilities

### 1. GameAdapters Layer
**Purpose**: Isolate all TaleWorlds-specific interactions

**Components**:
- **Harmony Patches**: Intercept game methods
- **Event Handlers**: Bridge game events to domain events
- **UI Adapters**: Modify game UI elements

**Principles**:
- No business logic - only translation between game and domain
- Fail safely - don't crash the game
- Minimal state - stateless where possible

### 2. Feature Modules
**Purpose**: Self-contained gameplay features

**Structure** (per feature):
```
Feature/
├── Domain/          # Business logic, rules, calculations
├── Application/     # Campaign integration, orchestration  
└── Infrastructure/  # TaleWorlds service integration
```

**Examples**:
- **Enlistment**: Contract management, commander tracking
- **Promotion**: XP calculation, tier advancement
- **Wages**: Payment scheduling, amount calculation

### 3. Core Layer
**Purpose**: Shared infrastructure and utilities

**Components**:
- **Configuration**: Centralized settings management
- **Persistence**: Save/load game state
- **Utilities**: Shared helper functions
- **Constants**: Application-wide constants

## Domain-Driven Design Patterns

### Domain Models
```csharp
// Pure business logic, no game dependencies
public class EnlistmentState
{
    public bool IsEnlisted { get; }
    public Hero Commander { get; }
    
    public void Enlist(Hero commander) { /* business rules */ }
    public bool IsCommanderValid() { /* validation logic */ }
}
```

### Application Services
```csharp
// Orchestrate domain and infrastructure
public class EnlistmentBehavior : CampaignBehaviorBase
{
    private EnlistmentState _state;
    
    public void OnEnlist() 
    {
        // Domain decision
        _state.Enlist(commander);
        
        // Infrastructure actuation  
        ArmyIntegrationService.JoinArmy(commander);
        PartyIllusionService.HidePlayerParty(commander);
    }
}
```

### Infrastructure Services
```csharp
// Handle TaleWorlds API interactions
public static class ArmyIntegrationService
{
    public static bool TryJoinCommandersArmy(Hero commander)
    {
        // TaleWorlds-specific implementation
        var main = MobileParty.MainParty;
        main.Ai.SetMoveEscortParty(commander.PartyBelongedTo);
        return true;
    }
}
```

## Event Flow

### 1. Player Enlists
```
Player Dialog → GameAdapter → Domain Decision → Infrastructure Action
     ↓              ↓              ↓                    ↓
"I want to    Dialog        EnlistmentState.     ArmyIntegration.
 enlist"      Service       Enlist()             JoinArmy()
                            
                            PromotionState.      PartyIllusion.
                            Reset()              HideParty()
```

### 2. Daily Tick
```
Game Timer → GameAdapter → Application Logic → Infrastructure Updates
     ↓           ↓              ↓                      ↓
Campaign     WageBehavior   Calculate daily       GiveGoldAction.
DailyTick    .OnDailyTick   wage amount          Apply()

             PromotionBehavior Calculate XP       Display promotion
             .OnDailyTick      progression        message
```

### 3. Battle Participation
```
Game Event → GameAdapter → Domain Check → Infrastructure Action
     ↓           ↓             ↓               ↓
MapEvent     BattleParticip-  Check if        EncounterManager.
Started      ationPatch       enlisted        StartEncounter()
```

## Data Flow

### Configuration Flow
```
settings.xml → ModSettings.Load() → Feature Services → Game Actions
```

### State Persistence
```
Domain State → SaveableFields → Bannerlord Save → Load → Domain State
```

### Error Handling
```
Exception → Log (TODO) → Safe Fallback → User Notification
```

## Dependency Rules

### 1. Dependency Direction
- GameAdapters → Features → Core
- Features cannot depend on GameAdapters
- Core has no dependencies

### 2. Interface Boundaries
- Features expose interfaces
- GameAdapters implement adapters
- Core provides utilities

### 3. Static Dependencies (Temporary)
- Static singletons marked with TODO
- Future: Constructor injection
- Present: Controlled global state

## Testing Strategy

### Unit Testing
- **Domain Layer**: Pure unit tests, no mocks needed
- **Application Layer**: Test with mocked infrastructure
- **Infrastructure Layer**: Integration tests or manual testing

### Smoke Testing
- **GameAdapters**: In-game testing required
- **End-to-End**: Full feature workflows
- **Performance**: Frame rate impact validation

## Performance Considerations

### Per-Frame Operations
- Keep tick handlers lightweight
- Avoid reflection in hot paths
- Cache expensive calculations

### Memory Management
- Minimize allocations in frequent operations
- Use object pooling for temporary objects
- Clean up event subscriptions

### Save Game Impact
- Keep serialized state minimal
- Version save data for compatibility
- Test save/load performance

## Security Considerations

### Input Validation
- Validate configuration values
- Sanitize file paths
- Bounds checking on arrays

### Error Boundaries
- Catch exceptions in GameAdapters
- Graceful degradation
- Don't crash the game

## Extension Points

### Adding New Features
1. Create feature folder structure
2. Define domain models
3. Implement application orchestration
4. Add infrastructure services
5. Create GameAdapter patches if needed

### Modifying Existing Features
1. Start with domain logic changes
2. Update application orchestration
3. Modify infrastructure if needed
4. Test end-to-end

### Integration with Other Mods
- Use TaleWorlds events where possible
- Avoid conflicting Harmony patches
- Design for graceful coexistence

## Future Architecture Evolution

### Phase 1: Dependency Injection
- Extract service interfaces
- Implement service container
- Remove static dependencies

### Phase 2: Event-Driven Architecture
- Replace direct calls with events
- Decouple feature interactions
- Enable feature plugins

### Phase 3: Configuration UI
- In-game settings menu
- Real-time configuration updates
- User preference profiles

This architecture supports the blueprint's core principles while providing a solid foundation for future growth and maintenance.
