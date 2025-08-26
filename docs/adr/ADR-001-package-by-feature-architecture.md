# ADR-001: Adopt Package-by-Feature Architecture

**Status:** Accepted  
**Date:** 2025-08-25  
**Deciders:** Development Team

## Context

The Enlisted mod was originally organized using a traditional layered architecture (Behaviors/, Models/, Patches/, Services/, Utils/). As the mod grew in complexity with multiple features (enlistment, promotion, wages), this structure created several maintenance challenges:

1. **Scattered Feature Logic**: Related functionality was split across multiple folders
2. **High Coupling**: Changes to one feature often required touching multiple layers
3. **Game API Exposure**: TaleWorlds-specific code was mixed with domain logic
4. **Testing Difficulties**: Domain logic was tightly coupled to game infrastructure
5. **Team Confusion**: Developers needed to navigate multiple folders for single features

## Decision

We will restructure the mod to use **Package-by-Feature** architecture as defined in the engineering blueprint:

```
src/
├── Mod.Entry/                    # Thin entry layer
├── Core/                         # Shared infrastructure  
├── Features/                     # Self-contained feature modules
│   ├── Enlistment/
│   │   ├── Domain/              # Business logic
│   │   ├── Application/         # Campaign integration
│   │   └── Infrastructure/      # TaleWorlds API interactions
│   ├── Promotion/
│   └── Wages/
└── GameAdapters/                 # Harmony patches isolated
```

## Consequences

### Positive
- **Feature Cohesion**: All related code lives together
- **Easier Testing**: Domain logic separated from game dependencies
- **Reduced Coupling**: Clear boundaries between features
- **Game Update Resilience**: TaleWorlds API changes isolated to GameAdapters
- **Team Productivity**: Developers work within well-defined feature boundaries

### Negative
- **Migration Effort**: One-time cost to restructure existing code
- **Learning Curve**: Team must adapt to new organization patterns
- **Potential Over-Engineering**: May be overkill for very simple features

### Neutral
- **File Count**: Similar number of files, just organized differently
- **Build Process**: Minimal changes to compilation

## Migration Strategy

1. **Phase 1**: Create new structure alongside existing (✅ Complete)
2. **Phase 2**: Move and refactor code to new locations (✅ Complete)  
3. **Phase 3**: Remove old structure and update references (✅ Complete)
4. **Phase 4**: Add tests for domain logic (🔄 Future)
5. **Phase 5**: Implement dependency injection (🔄 Future)

## Compliance

This decision aligns with:
- Blueprint Section 4.2: "Package-by-Feature (C#)"
- Blueprint Section 3: "Separation of Concerns"
- Blueprint Principle: "Make it observable"
