# ADR-010: Robust Logging System

**Status:** Accepted  
**Date:** 2024-12-27  
**Deciders:** Development Team

## Context

The Enlisted mod requires comprehensive logging for both development debugging and end-user support. Previous logging was basic and lacked structured organization, making it difficult to:

- Trace issues across different mod components
- Provide effective support to end users
- Debug complex game world interactions
- Correlate crashes with recent game state
- Maintain performance while providing detailed logging

The blueprint emphasizes "Make it observable" and "Emit structured logs for non-trivial paths" as guiding principles.

## Decision

Implement a robust logging system with three integrated components:

### 1. LoggingService
- **Structured logging** with stable categories (Enlistment, Promotion, Wages, Debugging, etc.)
- **Configurable log levels** (Debug, Info, Warning, Error)
- **Session correlation** with unique session IDs
- **User notifications** for important events
- **Performance logging** for timing information

### 2. CrashDumpService
- **Automatic crash reporting** on unhandled exceptions
- **Rich context** including game version, mod settings, campaign state
- **Debug log integration** from GameWorldDebugger
- **Session correlation** linking crash dumps to debug sessions
- **Automatic cleanup** with configurable retention (3 sessions default)

### 3. GameWorldDebugger
- **Detailed game world capture** for debugging
- **Player tracking** (position, health, party information)
- **Entity monitoring** (nearby parties and entities)
- **Battle state logging** (combat information and agent details)
- **Session-based storage** organized by session ID

### Session-Based Architecture
- **Unified session management** across all services
- **Same session ID** used for correlation
- **Temporal correlation** with millisecond precision timestamps
- **Organized file structure**:
  ```
  ModuleDirectory/
  ├── Debugging/[Session-ID]/
  │   ├── world_state_*.log
  │   ├── player_position_*.log
  │   ├── battle_state_*.log
  │   └── debug_report_*.txt
  └── CrashDumps/
      └── crash_[Session-ID]_*.txt
  ```

### Configuration Integration
- **DebugSettings** in ModSettings for granular control
- **Log levels** configurable via settings.xml
- **Feature-specific enabling** (world state, position tracking, etc.)
- **Retention policies** configurable per environment

## Consequences

### Positive
- **Comprehensive observability** following blueprint principles
- **Structured data** for issue analysis and support
- **Session correlation** enables effective issue tracing
- **Performance control** through configurable logging levels
- **End-user support** with detailed crash context
- **Development efficiency** with detailed game world debugging
- **Privacy protection** with no personal information in logs

### Negative
- **Increased complexity** in service registration and integration
- **Disk space usage** from debug logs and crash dumps
- **Performance overhead** from detailed logging (mitigated by configurable levels)
- **File I/O operations** that could fail in restricted environments

### Neutral
- **Additional configuration** required for optimal operation
- **Learning curve** for new developers to understand logging categories
- **Maintenance burden** for log file cleanup and management

## Compliance

### Blueprint Compliance
- **Observability**: Follows "Make it observable" principle with structured logging
- **Stable Categories**: Implements blueprint requirement for stable logging categories
- **Session Correlation**: Provides correlation support as specified in blueprint
- **Fail Closed**: Graceful degradation if logging fails, doesn't crash the game
- **Config over Code**: Extensive configuration options for different environments

### Architecture Compliance
- **Package-by-Feature**: Logging services in Core layer, used by all features
- **Dependency Injection**: Services registered via ServiceContainer
- **Separation of Concerns**: Logging separated from business logic
- **Error Boundaries**: Logging failures don't affect game functionality

### Integration with Existing ADRs
- **ADR-004**: Uses dependency injection for service registration
- **ADR-003**: Integrates with centralized configuration system
- **ADR-002**: Provides logging for game adapter interactions
- **ADR-005-007**: Supports feature-specific logging categories

## Implementation Notes

### Service Registration
```csharp
// Core services registered in ServiceContainer
container.RegisterSingleton<ICrashDumpService>(...);
container.RegisterSingleton<ILoggingService>(...);
container.RegisterSingleton<IGameWorldDebugger>(...);

// Integration in SubModule.cs
crashDumpService.SetGameWorldDebugger(gameWorldDebugger);
```

### Configuration
```xml
<DebugSettings>
    <EnableWorldStateCapture>true</EnableWorldStateCapture>
    <EnablePositionTracking>true</EnablePositionTracking>
    <EnableBattleLogging>true</EnableBattleLogging>
    <EnableCampaignLogging>true</EnableCampaignLogging>
    <EnableEntityMonitoring>true</EnableEntityMonitoring>
    <EnableVerboseEventLogging>true</EnableVerboseEventLogging>
    <DebugFileRetentionDays>3</DebugFileRetentionDays>
</DebugSettings>
```

### Usage Patterns
```csharp
// Structured logging with categories
_logger.LogInfo(LogCategories.Enlistment, "Player enlisted with {0}", commander.Name);
_logger.LogError(LogCategories.Promotion, "Failed to promote player", exception);

// Game world debugging
_debugger.CaptureWorldState("enlistment_start");
_debugger.LogPlayerPosition("commander_follow");
```

This ADR establishes the foundation for comprehensive observability in the Enlisted mod, enabling effective development, debugging, and end-user support while maintaining performance and following established architectural patterns.
