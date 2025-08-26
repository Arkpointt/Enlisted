# Enlisted Mod - Blueprint Structure & v2.1.0 Status

This project has been restructured to follow the **Package-by-Feature** blueprint architecture for maintainable Bannerlord mod development.

## ✅ **v2.1.0 COMPLETE - Dependency Injection & Centralized Logging**

**Major Achievement**: Successfully implemented **ADR-004** (Remove Static Singletons) with full **dependency injection** and **centralized logging** throughout the mod.

### 🏗️ **Latest Implementation (v2.1.0)**
- **Dependency Injection System**: Complete DI container with service interfaces
- **Centralized Logging Service**: Structured logging with session correlation
- **Enhanced Configuration**: Logging control and performance monitoring
- **Clean Build**: ✅ All compilation issues resolved (.NET Framework 4.7.2 compatible)
- **Blueprint Compliance**: Full adherence to architectural principles

## Current Structure

```
Enlisted/
├── src/
│   ├── Mod.Entry/                    # Thin entry layer
│   │   └── SubModule.cs             # Module initialization + DI setup
│   │
│   ├── Core/                        # Shared infrastructure
│   │   ├── Config/
│   │   │   └── ModSettings.cs       # Enhanced with logging config
│   │   ├── DependencyInjection/     # 🆕 DI Container
│   │   │   └── ServiceContainer.cs  # Simple service container
│   │   ├── Logging/                 # 🆕 Centralized Logging
│   │   │   └── LoggingService.cs    # Structured logging service
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
│   │   │   │   ├── EnlistmentBehavior.cs    # 🔄 Now uses DI + logging
│   │   │   │   └── IEnlistmentService.cs    # 🆕 Service interface
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
│   │   │       └── PromotionBehavior.cs     # 🔄 Now uses DI + logging
│   │   │
│   │   └── Wages/                   # Wages feature module
│   │       └── Application/
│   │           └── WageBehavior.cs          # 🔄 Now uses DI + logging
│   │
│   └── GameAdapters/                # TaleWorlds API isolation
│       └── Patches/
│           ├── SuppressArmyMenuPatch.cs     # 🔄 Uses DI instead of static
│           ├── BattleParticipationPatch.cs  # 🔄 Centralized logging
│           └── HidePartyNamePlatePatch.cs   # 🔄 DI with fallbacks
│
├── Documentation/                   # Architecture & ADR documentation
├── settings.xml.example            # 🆕 User configuration template
├── SubModule.xml                    # Bannerlord module definition
├── Enlisted.csproj                  # Project file
└── Enlisted.sln                     # Solution file (✅ VERIFIED CORRECT)
```

## Key Blueprint Principles Applied

### ✅ **Package-by-Feature** (v2.0.0)
- Each gameplay feature (Enlistment, Promotion, Wages) has its own self-contained module
- Related domain logic, application orchestration, and infrastructure live together
- Easy to find all code related to a specific feature

### ✅ **Game API Isolation** (v2.0.0)
- All Harmony patches moved to `GameAdapters/` layer
- TaleWorlds-specific code separated from domain logic
- Easier to handle game updates and API changes

### ✅ **Dependency Injection** (v2.1.0)
- Static singleton patterns replaced with proper DI container
- Service interfaces enable testing and reduce coupling
- Graceful fallbacks during transition period

### ✅ **Centralized Logging** (v2.1.0)
- Structured logging with stable categories and session correlation
- Configuration-driven debug and performance logging
- Consistent user message integration

### ✅ **Configuration Over Code** (Enhanced v2.1.0)
- Centralized `ModSettings` with logging configuration
- Feature flags and tunable parameters
- Runtime behavior control through settings

## Migration Status

### ✅ **v2.1.0 COMPLETED**
- **Dependency Injection**: Complete DI container implementation
- **Centralized Logging**: All TODO comments replaced with structured logging
- **Service Interfaces**: Clean abstractions for GameAdapter integration
- **Enhanced Configuration**: Logging control and performance monitoring
- **Build Verification**: ✅ Clean compilation, no errors

### ✅ **v2.0.0 COMPLETED**
- Project structure reorganized to Package-by-Feature
- All Harmony patches moved to GameAdapters layer
- Domain models extracted and properly organized
- Configuration system centralized
- Build system updated and tested

### 🚀 **Future Enhancements**
1. **Complete Static Removal**: Remove transition fallbacks
2. **Advanced Testing**: Unit tests for domain logic
3. **Enhanced Observability**: File-based logging, performance metrics
4. **Advanced DI**: Scoped lifecycles, service decorators

## Build Instructions

1. **Build**: `msbuild Enlisted.sln /p:Configuration=Debug`
2. **Output**: DLL and SubModule.xml automatically copied to game directory
3. **Install**: Files go to `<Bannerlord>/Modules/Enlisted/`
4. **Configuration**: Copy `settings.xml.example` to `settings.xml` for customization

## Configuration Guide

### Example settings.xml
```xml
<ModSettings>
  <DailyWage>10</DailyWage>
  <EnableDebugLogging>false</EnableDebugLogging>
  <LogLevel>1</LogLevel>
  <ShowVerboseMessages>false</ShowVerboseMessages>
  <EnablePerformanceLogging>false</EnablePerformanceLogging>
</ModSettings>
```

### Log Levels
- **0 = Debug**: Most verbose, development diagnostics
- **1 = Info**: Default level, important events
- **2 = Warning**: Only warnings and errors
- **3 = Error**: Only critical errors

This structure follows the blueprint's guidance for **correctness first, speed second** and **small, reversible changes** while achieving **enterprise-grade architecture** with **modern dependency injection** and **observability practices**.
