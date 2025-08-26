# Testing Guide

## Overview

This document outlines testing strategies for the Enlisted mod following blueprint principles. The Package-by-Feature architecture enables different testing approaches for different layers.

## Testing Strategy by Layer

### Domain Layer (Fully Testable)
**Location**: `src/Features/*/Domain/`
**Approach**: Unit tests with no external dependencies

```csharp
[Test]
public void PromotionRules_CalculatesCorrectXpForTier()
{
    // Given
    int tier = 2;
    
    // When
    int requiredXp = PromotionRules.GetRequiredXpForTier(tier);
    
    // Then
    Assert.AreEqual(900, requiredXp); // 600 * 1.5^1
}

[Test]
public void EnlistmentState_ValidatesCommanderCorrectly()
{
    // Given
    var state = new EnlistmentState();
    var mockCommander = CreateMockHero(alive: true, hasParty: true);
    
    // When
    state.Enlist(mockCommander);
    
    // Then
    Assert.IsTrue(state.IsCommanderValid());
}
```

### Application Layer (Integration Tests)
**Location**: `src/Features/*/Application/`  
**Approach**: Limited integration tests using behavior abstractions

```csharp
[Test]
public void PromotionBehavior_AdvancesTierWhenXpSufficient()
{
    // Given
    var behavior = new PromotionBehavior();
    var mockCampaign = CreateMockCampaign(enlisted: true);
    
    // When
    behavior.AddXp(600); // Enough for first promotion
    
    // Then
    Assert.AreEqual(2, behavior.Tier);
}
```

### Infrastructure Layer (Manual Testing)
**Location**: `src/Features/*/Infrastructure/`
**Approach**: Manual testing due to TaleWorlds dependencies

### GameAdapters Layer (Smoke Testing)
**Location**: `src/GameAdapters/`
**Approach**: In-game smoke tests

## Test Categories

### Unit Tests
- **Target**: Domain logic, calculations, validations
- **No Dependencies**: Pure functions, state objects
- **Fast Execution**: Runs in milliseconds
- **High Coverage**: Aim for 90%+ coverage of domain logic

### Integration Tests  
- **Target**: Application behaviors, service interactions
- **Mock Dependencies**: Abstract TaleWorlds types
- **Medium Speed**: Runs in seconds
- **Critical Paths**: Focus on main user workflows

### Smoke Tests
- **Target**: End-to-end functionality
- **Real Game**: Run in actual Bannerlord
- **Manual Execution**: Following test scripts
- **Release Gates**: Must pass before release

## Test Organization

```
tests/
├── Unit/
│   ├── Domain/
│   │   ├── EnlistmentStateTests.cs
│   │   ├── PromotionRulesTests.cs
│   │   └── PromotionStateTests.cs
│   └── Core/
│       ├── ConfigTests.cs
│       └── UtilsTests.cs
├── Integration/
│   ├── EnlistmentWorkflowTests.cs
│   ├── PromotionWorkflowTests.cs
│   └── WagePaymentTests.cs
└── Smoke/
    └── smoke-test-checklist.md
```

## Smoke Test Checklist

### Enlistment Feature
- [ ] Can start new campaign
- [ ] Can find lord with party  
- [ ] Can enlist via dialog
- [ ] Player party follows commander
- [ ] Army menus are suppressed
- [ ] Can participate in battles
- [ ] Can leave service via dialog

### Promotion Feature
- [ ] Daily XP accumulation works
- [ ] Battle XP awarded correctly
- [ ] Tier advancement at correct thresholds
- [ ] Promotion messages display

### Wages Feature
- [ ] Daily wage payment occurs
- [ ] Gold amount matches configuration
- [ ] No payment when not enlisted

### Integration
- [ ] Save/load preserves state correctly
- [ ] Multiple campaigns work independently
- [ ] Mod loads without conflicts
- [ ] Performance acceptable (>30 FPS)

## Mock Utilities

```csharp
public static class TestHelpers
{
    public static Hero CreateMockHero(bool alive = true, bool hasParty = true)
    {
        var hero = new Mock<Hero>();
        hero.Setup(h => h.IsDead).Returns(!alive);
        
        if (hasParty)
        {
            hero.Setup(h => h.PartyBelongedTo).Returns(CreateMockParty());
        }
        
        return hero.Object;
    }
    
    public static Campaign CreateMockCampaign(bool enlisted = false)
    {
        // Create minimal campaign mock for testing
    }
}
```

## Testing Tools

### Recommended Frameworks
- **NUnit**: Unit testing framework
- **Moq**: Mocking library for interfaces
- **FluentAssertions**: Readable assertion syntax

### IDE Integration
- **Visual Studio**: Built-in test runner
- **ReSharper**: Advanced test features
- **NCrunch**: Continuous test execution

## Continuous Integration

### Build Pipeline Tests
1. **Unit Tests**: Must pass for all commits
2. **Integration Tests**: Must pass for PRs
3. **Smoke Tests**: Manual before release

### Test Reports
- **Coverage Reports**: Track domain logic coverage
- **Performance Tests**: Ensure no regression
- **Compatibility Tests**: Multiple Bannerlord versions

## Future Improvements

1. **Automated Smoke Tests**: UI automation framework
2. **Property-Based Testing**: Fuzz testing for edge cases  
3. **Performance Benchmarks**: Automated performance regression detection
4. **Mutation Testing**: Validate test quality

## Getting Started

1. Install NUnit and Moq packages
2. Create your first domain unit test
3. Run tests from Visual Studio Test Explorer
4. Add new tests for each new feature

Remember: **Test the behavior, not the implementation**. Focus on what the code should do, not how it does it.
