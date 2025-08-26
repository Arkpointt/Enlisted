# ADR-004: Remove Static Singleton Patterns

**Status:** Proposed  
**Date:** 2025-08-25  
**Deciders:** Development Team

## Context

The current codebase uses static singleton patterns in several places, particularly:

1. **EnlistmentBehavior.Instance**: Static access for Harmony patches
2. **ModSettings.Instance**: Static configuration access
3. **Various Service Classes**: Static methods without state

While these patterns enabled the initial blueprint migration, they create several issues:

1. **Testing Difficulty**: Cannot mock or substitute dependencies
2. **Tight Coupling**: Direct dependencies on concrete implementations
3. **State Management**: Global mutable state is hard to reason about
4. **Concurrency Issues**: Potential race conditions in complex scenarios

## Decision

We will **gradually eliminate static singleton patterns** and replace them with proper dependency injection:

### Phase 1: Interface Extraction (Future)
```csharp
public interface IEnlistmentService
{
    bool IsEnlisted { get; }
    Hero Commander { get; }
}

public interface IConfigurationService  
{
    int DailyWage { get; }
    bool EnableDebugLogging { get; }
}
```

### Phase 2: Constructor Injection (Future)
```csharp
public class SuppressArmyMenuPatch
{
    private readonly IEnlistmentService _enlistmentService;
    
    public SuppressArmyMenuPatch(IEnlistmentService enlistmentService)
    {
        _enlistmentService = enlistmentService;
    }
}
```

### Phase 3: Service Container (Future)
```csharp
// Service registration during mod initialization
container.RegisterSingleton<IEnlistmentService, EnlistmentBehavior>();
container.RegisterSingleton<IConfigurationService, ModSettings>();
```

## Consequences

### Positive
- **Testability**: Can inject mock dependencies for unit tests
- **Flexibility**: Can swap implementations without changing dependents
- **Maintainability**: Clear dependency relationships
- **Debugging**: Easier to trace dependencies and state changes

### Negative
- **Complexity**: Requires understanding of DI patterns
- **Migration Effort**: Significant refactoring of existing code
- **Runtime Overhead**: Small performance cost of container lookups

### Neutral
- **Code Volume**: Similar amount of code, differently organized
- **Build Time**: Minimal impact on compilation

## Migration Strategy

### Immediate (This Release)
- Keep static patterns with TODO comments marking injection points
- Document interfaces that will be extracted
- Ensure code is structured to support future injection

### Phase 1 (Next Release)
- Extract interfaces for major services
- Create simple service container
- Migrate GameAdapters to use injection

### Phase 2 (Future Release)  
- Migrate all behaviors to use injection
- Remove static Instance properties
- Add comprehensive unit tests

### Phase 3 (Future Release)
- Advanced DI features (scoped lifecycles, decorators)
- Integration with Bannerlord's service systems

## Implementation Guidelines

1. **Start with Interfaces**: Define contracts before changing implementations
2. **Gradual Migration**: One service at a time to avoid big-bang changes
3. **Backward Compatibility**: Keep static access during transition
4. **Documentation**: Clear examples of new patterns
5. **Testing**: Unit tests to validate injection works correctly

## Risks and Mitigations

### Risk: Bannerlord Compatibility
- **Mitigation**: Use simple container that doesn't conflict with game systems

### Risk: Performance Impact
- **Mitigation**: Measure performance, optimize container if needed

### Risk: Team Learning Curve  
- **Mitigation**: Provide clear documentation and examples

## Success Criteria

1. All major services use constructor injection
2. Unit tests cover domain logic without game dependencies
3. No static state in domain layer
4. Clear service boundaries and interfaces

## Compliance

This decision aligns with:
- Blueprint Section 7: "Guided Refactor" change level
- Blueprint Principle: "Small, reversible changes"
- Modern C# dependency injection patterns
