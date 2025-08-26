# Changelog

All notable changes to the Enlisted mod are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.1.0] - 2025-08-25 - Dependency Injection & Centralized Logging

### 🏗️ **MAJOR FEATURES**
- **Dependency Injection System**: Replaced static singleton patterns with proper DI container
- **Centralized Logging Service**: Structured logging with session correlation and stable categories
- **Service Interfaces**: Clean abstractions for GameAdapter integration
- **Enhanced Configuration**: Logging control, performance monitoring, debug options

### ✨ **Added**
- **ILoggingService**: Centralized logging with debug, info, warning, error levels
- **IServiceContainer**: Simple dependency injection without external dependencies
- **IEnlistmentService**: Interface abstraction for enlistment state access
- **LogCategories**: Stable logging categories for observability
- **Session Correlation**: Unique session IDs for support and debugging
- **Performance Logging**: Optional frame-time impact monitoring
- **Configuration Options**: Debug logging, verbose messages, log level thresholds
- **Example Settings**: settings.xml.example for user customization

### 🔄 **Changed**
- **All Behaviors**: Now use dependency injection and centralized logging
- **All GameAdapter Patches**: Use service interfaces instead of static instances
- **SubModule Initialization**: Registers services and initializes DI container
- **ModSettings**: Enhanced with logging configuration options
- **Error Handling**: Improved with structured error reporting

### 🗑️ **Deprecated**
- **Static Instance Access**: EnlistmentBehavior.Instance (kept for transition)
- **Direct InformationManager Calls**: Replaced with logging service
- **TODO Logging Comments**: All replaced with structured logging

### 🐛 **Fixed**
- **Missing Using Statements**: LoggingService properly references ModSettings
- **Log Level Filtering**: Respects user-configured log level thresholds
- **Service Resolution**: Graceful fallbacks when DI unavailable
- **Compilation Errors**: Fixed LogCategories parameter type in BattleParticipationPatch
- **Framework Compatibility**: Replaced range operator with Substring for .NET Framework 4.7.2
- **Service Registration**: Fixed DI container registration in SubModule

### 📚 **Documentation**
- **ADR-004-IMPLEMENTATION-COMPLETE.md**: Detailed implementation summary with build verification
- **settings.xml.example**: User configuration template
- **Enhanced Code Comments**: Blueprint-compliant intent-focused documentation
- **README_BLUEPRINT.md**: Updated with v2.1.0 completion status

### 🎯 **Blueprint Compliance**
- ✅ **ADR-004**: Static singleton patterns replaced with dependency injection
- ✅ **Centralized Logging**: Structured, observable logging throughout
- ✅ **Config over Code**: Logging behavior controlled by configuration
- ✅ **Make it Observable**: Session correlation and stable categories
- ✅ **Fail Closed**: Safe fallbacks when services unavailable
- ✅ **Clean Build**: All compilation issues resolved, framework compatible

---

## [2.0.0] - 2025-08-25 - Blueprint Architecture Migration

### 🏗️ **BREAKING CHANGES**
- **Complete restructure** to Package-by-Feature architecture
- **New build system** using `Enlisted_Blueprint.csproj` and `Enlisted_Blueprint.sln`
- **Moved configuration** from `Settings.cs` to `src/Core/Config/ModSettings.cs`

### ✨ **Added**
- **Package-by-Feature structure** following engineering blueprint
- **GameAdapters layer** for TaleWorlds API isolation
- **Centralized configuration** with feature flags and validation
- **Comprehensive documentation** (ADRs, Architecture Guide, Testing Guide)
- **Enhanced error handling** with safe fallbacks

### 🔄 **Changed**
- **All source files** moved to blueprint structure under `src/`
- **Harmony patches** isolated in `src/GameAdapters/Patches/`
- **Domain models** separated from infrastructure concerns
- **Service layer** reorganized by feature boundaries
- **Build output** unchanged - still works with existing Bannerlord installation

### 🗑️ **Removed**
- **Old flat structure** (Behaviors/, Models/, Patches/, Services/, Utils/)
- **ServiceLocator pattern** (replaced with direct service calls)
- **Duplicate files** and legacy project structure
- **Unused interface abstractions** that added complexity without value

### 🐛 **Fixed**
- **Compilation errors** from missing dependencies
- **Static singleton references** (temporary solution with TODO markers)
- **Project structure inconsistencies**

### 📚 **Documentation**
- **ADR-001**: Package-by-Feature Architecture adoption
- **ADR-002**: Game API isolation strategy  
- **ADR-003**: Centralized configuration approach
- **ADR-004**: Static singleton removal plan
- **ARCHITECTURE.md**: Comprehensive architecture guide
- **TESTING.md**: Testing strategy and guidelines
- **README_BLUEPRINT.md**: Migration summary and structure overview

---

## [1.x.x] - Previous Releases

### Functional Features (Preserved in 2.x)
- ✅ **Enlistment system** - Join lord armies as soldier
- ✅ **Army menu suppression** - Prevent conflicting UX while enlisted
- ✅ **Battle participation** - Auto-join commander battles  
- ✅ **Party illusion** - Hide player party, follow commander
- ✅ **Promotion system** - XP accumulation and tier advancement
- ✅ **Wage payment** - Daily gold payment while enlisted
- ✅ **Save/load compatibility** - Persistent state across sessions
- ✅ **Settlement following** - Auto-enter/exit with commander

---

## Configuration Guide

### Settings File Location
Place `settings.xml` in your `<Bannerlord>/Modules/Enlisted/` directory.

### Example Configuration
```xml
<ModSettings>
  <DailyWage>10</DailyWage>
  <EnableDebugLogging>false</EnableDebugLogging>
  <LogLevel>1</LogLevel>
  <ShowVerboseMessages>false</ShowVerboseMessages>
</ModSettings>
```

### Log Levels
- **0 = Debug**: Most verbose, shows all operations
- **1 = Info**: Default level, shows important events
- **2 = Warning**: Shows only warnings and errors
- **3 = Error**: Shows only critical errors

---

## Support & Troubleshooting

### Debug Mode
Set `EnableDebugLogging=true` and `LogLevel=0` for detailed diagnostics.

### Performance Issues
Enable `EnablePerformanceLogging=true` to monitor frame-time impact.

### Support Information
Session IDs in logs help with issue reporting and troubleshooting.

---

## Version Numbering

- **Major version** (2.0): Breaking architectural changes
- **Minor version** (2.1): New features, backward compatible
- **Patch version** (2.0.1): Bug fixes, no new features
