# Enlisted Mod - Blueprint Structure

This project has been restructured to follow the **Package-by-Feature** blueprint architecture for maintainable Bannerlord mod development.

## Current Structure

```
Enlisted/
├── src/
│   ├── Mod.Entry/                    # Thin entry layer
│   │   └── SubModule.cs             # Module initialization
│   │
│   ├── Core/                        # Shared infrastructure
│   │   ├── Config/
│   │   │   └── ModSettings.cs       # Centralized configuration
│   │   ├── Constants.cs             # Application constants
│   │   ├── Persistence/
│   │   │   └── SaveDefiner.cs       # Save game schema
│   │   └── Utils/
│   │       └── DebugHelper.cs       # Shared utilities
│   │
│   ├── Features/                    # Package-by-Feature organization
│   │   ├── Enlistment/              # Enlistment feature module
│   │   │   ├── Domain/
│   │   │   │   └── EnlistmentState.cs
│   │   │   ├── Application/
│   │   │   │   └── EnlistmentBehavior.cs
│   │   │   └── Infrastructure/
│   │   │       ├── ArmyIntegrationService.cs
│   │   │       ├── PartyIllusionService.cs
│   │   │       └── DialogService.cs
│   │   │
│   │   ├── Promotion/               # Promotion feature module
│   │   │   ├── Domain/
│   │   │   │   ├── PromotionState.cs
│   │   │   │   └── PromotionRules.cs
│   │   │   └── Application/
│   │   │       └── PromotionBehavior.cs
│   │   │
│   │   └── Wages/                   # Wages feature module
│   │       └── Application/
│   │           └── WageBehavior.cs
│   │
│   └── GameAdapters/                # TaleWorlds API isolation
│       └── Patches/
│           ├── SuppressArmyMenuPatch.cs
│           ├── BattleParticipationPatch.cs
│           └── HidePartyNamePlatePatch.cs
│
├── Documentation/
│   └── BLUEPRINT.md                 # Architecture guide
│
├── SubModule.xml                    # Bannerlord module definition
├── Enlisted_Blueprint.csproj        # Updated project file
└── Enlisted_Blueprint.sln           # Solution file
```

## Key Blueprint Principles Applied

### ✅ **Package-by-Feature**
- Each gameplay feature (Enlistment, Promotion, Wages) has its own self-contained module
- Related domain logic, application orchestration, and infrastructure live together
- Easy to find all code related to a specific feature

### ✅ **Separation of Concerns**
- **Domain**: Pure business logic and state (EnlistmentState, PromotionRules)
- **Application**: Campaign integration and orchestration (Behaviors)
- **Infrastructure**: TaleWorlds API interactions (Services)
- **GameAdapters**: Harmony patches isolated from domain logic

### ✅ **Game API Isolation**
- All Harmony patches moved to `GameAdapters/` layer
- TaleWorlds-specific code separated from domain logic
- Easier to handle game updates and API changes

### ✅ **Configuration Over Code**
- Centralized `ModSettings` with validation
- Feature flags and tunable parameters
- Safe loading with fallback to defaults

### ✅ **Observability Ready**
- TODO comments mark where structured logging will be added
- Debug helpers for validation and error reporting
- Preparation for centralized logging service

## Migration Status

### ✅ **Completed**
- Project structure reorganized to Package-by-Feature
- All Harmony patches moved to GameAdapters layer
- Domain models extracted and properly organized
- Configuration system centralized
- Build system updated and tested

### 🔄 **Future Improvements** (marked with TODOs)
1. **Dependency Injection**: Remove static Instance patterns
2. **Centralized Logging**: Replace InformationManager calls with structured logging
3. **Testing Framework**: Add unit tests for domain logic
4. **Advanced Configuration**: In-game settings UI
5. **Performance Monitoring**: Add metrics for frame-time budgets

## Build Instructions

1. **Build**: `msbuild Enlisted_Blueprint.sln /p:Configuration=Debug`
2. **Output**: DLL and SubModule.xml automatically copied to game directory
3. **Install**: Files go to `<Bannerlord>/Modules/Enlisted/`

## Development Workflow

### Adding New Features
1. Create feature folder under `src/Features/`
2. Add Domain models for business logic
3. Add Application behavior for campaign integration  
4. Add Infrastructure services for TaleWorlds API interaction
5. Add GameAdapter patches if needed

### Making Changes
- **Small changesets**: Focus on single concern per edit
- **Test early**: Build after each logical change
- **Document intent**: Comments explain why, not what
- **Config over code**: Use settings for tunable behavior

This structure follows the blueprint's guidance for **correctness first, speed second** and **small, reversible changes** while preparing for future growth and maintainability.
