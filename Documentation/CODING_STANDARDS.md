# Enlisted Mod - Coding Standards & Best Practices

## 1. Project Overview
This document outlines the official coding standards for the **Enlisted** mod project. Our mod allows players to serve as soldiers under lords in Mount & Blade II: Bannerlord, creating an immersive subordinate military experience.

### Core Philosophy
Our code must be:
- **Readable**: Code written for humans first, computers second
- **Maintainable**: Easy to debug, modify, and extend without breaking functionality
- **Stable**: Defensive coding practices to minimize crashes for end-users
- **Bannerlord-Compatible**: Seamlessly integrate with existing game systems

## 2. C# Coding Conventions

### 2.1. Naming Conventions
| Type | Style | Example |
|------|-------|---------|
| Class, Struct, Enum | PascalCase | `public class EnlistmentBehavior` |
| Interface | IPascalCase | `public interface IArmyService` |
| Public Members | PascalCase | `public void EnlistWithCommander()` |
| Method Parameters | camelCase | `void SetCommander(Hero commanderHero)` |
| Local Variables | camelCase | `var playerParty = MobileParty.MainParty;` |
| Private Fields | _camelCase | `private bool _isEnlisted;` |
| Constants | PascalCase | `public const int MaxEnlistmentDuration = 365;` |

**Critical**: The `_camelCase` convention for private fields prevents ambiguity with local variables and aligns with our existing codebase.

### 2.2. Formatting & Layout
**Braces**: Use Allman style (each brace gets its own line)
```csharp
// Correct
if (EnlistmentState.IsEnlisted)
{
    ArmyService.FollowCommander();
}
```

**Indentation**: 4 spaces per level (already configured in `.editorconfig`)

**Spacing**: Single space after commas and control flow keywords

### 2.3. Documentation Requirements
**XML Documentation is Mandatory** for all public/internal members:

```csharp
/// <summary>
/// Enlists the player with the specified commander, integrating them into the commander's army.
/// </summary>
/// <param name="commander">The hero to serve under</param>
/// <returns>True if enlistment succeeded, false otherwise</returns>
public bool EnlistWithCommander(Hero commander)
{
    // Implementation
}
```

## 3. Enlisted-Specific Architectural Patterns

### 3.1. Service-Oriented Architecture
Our project follows a clean service pattern:

```
Behaviors/     # Orchestrators - coordinate services and handle game events
Services/      # Business logic - specific functionality implementations  
Patches/       # Harmony patches - minimal game integration
Models/        # Data structures - save/load compatible
Utils/         # Utilities - helpers, constants, reflection
```

**Rule**: Behaviors orchestrate, Services implement, Patches integrate.

### 3.2. Harmony Patching Standards
**File Organization**: Each patched class gets its own file
- File: `HidePartyNamePlatePatch.cs` 
- Target: `SandBox.ViewModelCollection.Nameplate.PartyNameplateVM`

**Naming Convention**: `TargetClass_MethodName_PatchType`
```csharp
[HarmonyPatch(typeof(PartyNameplateVM), "RefreshBinding")]
public class HidePartyNamePlatePatch
{
    private static void Postfix(PartyNameplateVM __instance)
    {
        // Patch logic here
    }
}
```

**Safety First**: Always wrap patch logic in try-catch
```csharp
private static void Postfix(PartyNameplateVM __instance)
{
    try
    {
        // Patch logic
    }
    catch (Exception ex)
    {
        // Log but don't crash the game
        InformationManager.DisplayMessage(new InformationMessage($"[Enlisted] Patch error: {ex.Message}"));
    }
}
```

### 3.3. Save System Integration
**Use SaveableField Correctly**: Only on private fields, never properties
```csharp
public class EnlistmentState
{
    [SaveableField(1)] private bool _isEnlisted;
    [SaveableField(2)] private Hero _commander;
    
    // Properties expose the fields
    public bool IsEnlisted 
    { 
        get => _isEnlisted; 
        set => _isEnlisted = value; 
    }
}
```

**Save Type Definer**: Required for custom types
```csharp
public class EnlistedSaveDefiner : SaveableTypeDefiner
{
    public EnlistedSaveDefiner() : base(580669) { } // Unique ID

    protected override void DefineClassTypes()
    {
        AddClassDefinition(typeof(EnlistmentState), 1);
        AddClassDefinition(typeof(PromotionState), 2);
    }
}
```

## 4. Bannerlord-Specific Best Practices

### 4.1. Null Safety & Defensive Coding
**Mandatory Null Checks**: Bannerlord state can be unpredictable
```csharp
// Always check for null
var commander = Hero.OneToOneConversationHero;
if (commander?.PartyBelongedTo != null && !commander.IsDead)
{
    // Safe to proceed
}

// Use null-conditional operators
var commanderName = EnlistmentState.Commander?.Name?.ToString() ?? "Unknown";
```

### 4.2. Performance Guidelines
**Avoid Heavy Logic in OnTick**: Tick methods must be lightweight
```csharp
public override void OnDailyTick()
{
    // Good - daily operations
    if (EnlistmentState.IsEnlisted)
    {
        WageService.PayDailyWage();
    }
}

public override void OnHourlyTick()
{
    // Acceptable - hourly checks
    PromotionService.CheckForPromotion();
}

// Never do heavy operations in OnTick() - called every frame!
```

**Cache Frequently Used Values**:
```csharp
// Cache expensive lookups
private MobileParty _cachedMainParty;
public MobileParty MainParty => _cachedMainParty ??= MobileParty.MainParty;
```

### 4.3. Localization Support
**Never use hardcoded strings** for user-facing text:
```csharp
// Wrong
InformationManager.DisplayMessage(new InformationMessage("You have enlisted!"));

// Correct (following our current pattern)
private const string MESSAGE_PREFIX = "[Enlisted]";
InformationManager.DisplayMessage(new InformationMessage($"{MESSAGE_PREFIX} Now serving under {commanderName}"));

// Future: Full localization support
var message = new TextObject("{=Enlisted_Enlist_Success}Now serving under {COMMANDER}");
message.SetTextVariable("COMMANDER", commanderName);
```

### 4.4. Reflection Safety
**Use Reflection Helpers**: For API compatibility across game versions
```csharp
// Use our ReflectionHelpers utility
try
{
    ReflectionHelpers.SetEscortAction(party, target);
}
catch (Exception ex)
{
    // Fallback to known API
    party?.Ai?.SetMoveEscortParty(target);
    DebugHelper.LogReflectionFailure("SetEscortAction", ex);
}
```

## 5. Project Structure Standards

### 5.1. Directory Organization
Our current structure (maintain this):
```
Enlisted/
??? Behaviors/          # CampaignBehaviors (orchestrators)
??? Services/           # Business logic implementations
??? Patches/            # Harmony patches for game integration
??? Models/             # Data structures with save/load support
??? Utils/              # Utilities, constants, helpers
??? Documentation/      # Technical documentation
??? SubModule.cs       # Mod entry point
??? SubModule.xml      # Bannerlord mod configuration
??? Settings.cs        # User configuration
```

### 5.2. Class Responsibilities
**Behaviors**: Orchestrate systems, handle game events
```csharp
public class EnlistmentBehavior : CampaignBehaviorBase
{
    private readonly ArmyService _armyService;
    private readonly DialogService _dialogService;
    
    // Orchestrates but doesn't implement business logic
}
```

**Services**: Implement specific functionality
```csharp
public class ArmyService
{
    // Handles army creation, joining, leaving
    public bool CreateArmyForCommander(Hero commander) { }
    public bool JoinCommanderArmy(Hero commander) { }
}
```

**Models**: Data only, minimal logic
```csharp
public class EnlistmentState
{
    // Save/load data
    // Simple state validation
    // No business logic
}
```

## 6. Quality Assurance

### 6.1. Error Handling Patterns
**Service Layer Error Handling**:
```csharp
public bool EnlistWithCommander(Hero commander)
{
    try
    {
        if (!ValidateCommander(commander))
        {
            return false;
        }
        
        // Business logic
        return true;
    }
    catch (Exception ex)
    {
        DebugHelper.LogError($"Enlistment failed: {ex.Message}");
        return false;
    }
}
```

**Patch Error Handling**: Never let patches crash the game
```csharp
[HarmonyPrefix]
private static bool PreventCrash(/*parameters*/)
{
    try
    {
        // Patch logic
        return true;
    }
    catch
    {
        // Log error but continue game execution
        return true; // Let original method run
    }
}
```

### 6.2. Testing Standards
**Manual Testing Checklist** (maintain current standards):
1. Core enlistment flow
2. Save/load preservation
3. Battle participation
4. Edge cases (commander death, army disbanding)

**Code Review Focus**:
- Null safety
- Performance impact
- Save compatibility
- Error handling

## 7. Version Control & Collaboration

### 7.1. Git Workflow
**Branches**:
- `main`: Stable, production-ready
- `develop`: Development integration
- `feature/*`: New features
- `hotfix/*`: Critical fixes

**Commit Messages**: Follow conventional commits
```
feat: Add promotion system for enlisted soldiers
fix: Prevent crash when commander dies during battle
docs: Update API documentation for ArmyService
```

### 7.2. Code Review Requirements
Before merging:
- [ ] Follows naming conventions
- [ ] Includes XML documentation
- [ ] Has proper error handling
- [ ] Maintains save compatibility
- [ ] No performance regressions

## 8. Development Tools

### 8.1. Required Configuration
- **IDE**: Visual Studio 2022
- **Extensions**: 
  - CodeMaid (code cleanup)
  - SonarLint (code analysis)
- **Configuration**: `.editorconfig` enforces formatting

### 8.2. Build Configuration
**Target Framework**: .NET Framework 4.7.2 (matches project)
**Language Version**: Latest (C# 13.0 features available)
**Platform**: AnyCPU with x64 configurations

### 8.3. Dependencies
**Game References**: All TaleWorlds.* assemblies from Bannerlord installation
**Harmony**: 2.2.2 (compile-time only, runtime from Bannerlord.Harmony)
**No External Dependencies**: Keep mod self-contained

## 9. Performance & Compatibility

### 9.1. Performance Targets
- Zero impact on game startup time
- Minimal memory allocation in tick methods  
- Cache expensive reflection calls
- Efficient state checking

### 9.2. Compatibility Requirements
- Support latest Bannerlord stable version
- Graceful degradation for API changes
- Safe mod unloading (no memory leaks)
- Save file compatibility across versions

## 10. Future Considerations

### 10.1. Planned Enhancements
- Full localization system (TextObject integration)
- MCM (Mod Configuration Menu) integration
- Advanced promotion systems
- Formation positioning

### 10.2. Architecture Evolution
- Maintain service-oriented pattern
- Consider dependency injection for complex features
- Plan for modular expansion (rank systems, equipment standards)

---

**Last Updated**: [Current Date]  
**Status**: Production Implementation  
**Version**: 1.0.0

This document reflects the current high-quality standards achieved in the Enlisted project and provides guidance for future development and contributors.