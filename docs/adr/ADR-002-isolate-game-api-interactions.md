# ADR-002: Isolate Game API Interactions in GameAdapters Layer

**Status:** Accepted  
**Date:** 2025-08-25  
**Deciders:** Development Team

## Context

The original codebase mixed TaleWorlds game API calls (Harmony patches, campaign behaviors, UI interactions) directly with business logic. This created several problems:

1. **Brittleness**: Game updates could break business logic
2. **Testing Difficulty**: Cannot unit test logic that requires game runtime
3. **Tight Coupling**: Domain rules tied to specific TaleWorlds implementations
4. **Maintenance Overhead**: API changes required touching multiple unrelated files

## Decision

We will implement a **GameAdapters layer** that isolates all TaleWorlds-specific interactions:

### Structure
```
src/
├── GameAdapters/
│   ├── Patches/                  # All Harmony patches
│   │   ├── SuppressArmyMenuPatch.cs
│   │   ├── BattleParticipationPatch.cs
│   │   └── HidePartyNamePlatePatch.cs
│   └── Behaviors/                # Campaign system integrations (future)
```

### Responsibilities
- **GameAdapters**: Handle TaleWorlds API calls, Harmony patches, UI modifications
- **Features**: Contain pure business logic, calculations, state management
- **Infrastructure**: Bridge between features and GameAdapters when needed

### Patterns
- **Adapter Pattern**: GameAdapters translate between game APIs and domain interfaces
- **Observer Pattern**: GameAdapters listen to game events, notify domain services
- **Command Pattern**: Domain services issue commands, GameAdapters execute them

## Consequences

### Positive
- **Update Resilience**: Game API changes isolated to one layer
- **Testability**: Domain logic can be unit tested without game runtime
- **Maintainability**: Clear separation of concerns
- **Flexibility**: Can swap game adapters for different versions or platforms

### Negative
- **Indirection**: Some operations require more code layers
- **Complexity**: Need to design proper interfaces between layers
- **Learning Curve**: Team must understand adapter patterns

### Neutral
- **Performance**: Minimal impact due to thin adapter layer
- **Code Volume**: Similar amount of code, better organized

## Implementation Guidelines

1. **All Harmony patches** go in `GameAdapters/Patches/`
2. **No direct TaleWorlds API calls** in Features domain layer
3. **Infrastructure services** can interact with GameAdapters
4. **Static Instance patterns** are temporary during migration
5. **TODO comments** mark future dependency injection points

## Rollback Plan

If this approach proves problematic:
1. Move patches back to Features infrastructure
2. Allow direct TaleWorlds calls in infrastructure layer
3. Keep domain layer pure regardless

## Compliance

This decision implements:
- Blueprint Section 3: "Integration Boundary"
- Blueprint Principle: "Respect the platform"
- Blueprint Pattern: "Decision vs Actuation separation"
