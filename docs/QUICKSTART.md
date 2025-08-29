# Quick Start Guide - Enlisted Mod Development

## 🚀 **Getting Started**

### Prerequisites
- Visual Studio 2022
- Mount & Blade II: Bannerlord installed
- .NET Framework 4.7.2 SDK

### Build & Run
```powershell
# Clone repository
git clone https://github.com/Arkpointt/Enlisted
cd Enlisted

# Build mod (Editor config copies to Modules path)
dotnet build Enlisted.sln -c "Enlisted EDITOR"

# Output: C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\Enlisted\bin\Win64_Shipping_wEditor\Enlisted.dll
# Launch Bannerlord and enable the "Enlisted" mod
```

## 🏗️ **Architecture Overview**

### Package-by-Feature Structure (current)
```
src/
├── Features/
│   └── Enlistment/
│       ├── Application/
│       └── Domain/
├── Mod.GameAdapters/  # Harmony patches and adapters
│   └── Patches/
├── Mod.Core/          # Shared infrastructure (logging)
└── Mod.Entry/         # Thin initialization layer
```

### Key Services (current)
- Centralized logging via `LoggingService` in `src/Mod.Core/Logging/LoggingService.cs`

## 🔧 **Development Patterns**

### Adding New Features
1. **Create feature folder**: `src/Features/YourFeature/`
2. **Add layers as needed**:
   - `Domain/` - Business logic, calculations
   - `Application/` - Campaign integration
   - `Infrastructure/` - TaleWorlds services
3. **Register with DI**: Add to SubModule service registration

### Using Logging
```csharp
using Enlisted.Mod.Core.Logging;

LoggingService.Info("Category", "Operation completed");
```

### Enlistment State Access
Use `EnlistmentBehavior.IsPlayerEnlisted` and `EnlistmentBehavior.CurrentCommanderParty` for UI adapters/patches.

## ⚙️ **Configuration**

### User Settings
Currently no external settings are required.

### Development Settings
```xml
<ModSettings>
  <EnableDebugLogging>true</EnableDebugLogging>
  <EnablePerformanceLogging>true</EnablePerformanceLogging>
  <ShowVerboseMessages>true</ShowVerboseMessages>
  <LogLevel>0</LogLevel>
</ModSettings>
```

## 🧪 **Testing**

### Unit Tests (Future)
- Domain logic: Pure functions, no TaleWorlds dependencies
- Application logic: Mock TaleWorlds services
- Integration: Limited, focus on critical paths

### Manual Testing Checklist (current)
- [ ] Mod loads without errors
- [ ] Can enlist with lords
- [ ] Main party nameplate/shield hidden while enlisted
- [ ] Map tracker follows commander; MainParty is not tracked
- [ ] Camera follows commander; escort is maintained
- [ ] Save/load preserves enlistment state

## 📝 **Code Standards**

### Comments
- Explain **intent** and **constraints**, not implementation
- Reference ADRs for architectural decisions
- Update comments when behavior changes

### Logging Categories
Use stable categories from `LogCategories`:
```csharp
LogCategories.Enlistment    // Contract operations
LogCategories.Promotion     // XP and tier advancement  
LogCategories.Wages         // Payment processing
LogCategories.GameAdapters  // Harmony patch decisions
LogCategories.Performance   // Timing and optimization
```

### Error Handling
```csharp
try
{
    // Risky operation
}
catch (Exception ex)
{
    logger?.LogError(LogCategories.YourFeature, "Operation failed", ex);
    // Graceful degradation
}
```

## 🐛 **Debugging**

### Enable Debug Mode
1. Set `EnableDebugLogging=true` in settings.xml
2. Set `LogLevel=0` for maximum verbosity
3. Check debug output window in Visual Studio

### Robust Logging System
The mod includes a comprehensive logging system for both development and end-user support:

#### Structured Logging
```csharp
// Categories for organized logging
LogCategories.Enlistment    // Contract operations
LogCategories.Promotion     // XP and tier advancement  
LogCategories.Wages         // Payment processing
LogCategories.GameAdapters  // Harmony patch decisions
LogCategories.Performance   // Timing and optimization
LogCategories.Debugging     // Game world state capture
LogCategories.CrashDump     // Crash reporting
```

#### Session-Based Debugging
Enable detailed game world monitoring:

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

#### Log File Locations
- **Debug Logs**: `Modules/Enlisted/Debugging/[Session-ID]/`
  - `world_state_*.log` - Game world snapshots
  - `player_position_*.log` - Player location tracking
  - `battle_state_*.log` - Battle-specific information
  - `campaign_state_*.log` - Campaign progress
  - `nearby_entities_*.log` - Nearby parties and entities
  - `game_event_*.log` - Game event timeline
  - `debug_report_*.txt` - Comprehensive session reports

- **Crash Dumps**: `Modules/Enlisted/CrashDumps/`
  - `crash_[Session-ID]_*.txt` - Detailed crash reports with context
  - Includes recent debug logs, game state, and exception details

#### Session Correlation
- **Same Session ID** used across all log files
- **Easy tracing** from debug logs to crash reports
- **Temporal correlation** with millisecond precision timestamps
- **Automatic cleanup** keeps only 3 most recent sessions

### Error Handling
```csharp
try
{
    // Risky operation
}
catch (Exception ex)
{
    logger?.LogError(LogCategories.YourFeature, "Operation failed", ex);
    // Automatic crash dump created with debug context
    // Graceful degradation
}
```

### Common Issues
- **Services not available**: Check ServiceLocator initialization
- **Static instance access**: Transition period - both DI and static work
- **Build errors**: Ensure all references point to correct Bannerlord installation
- **Log file access**: Check file permissions in module directory

### Log Analysis
- **Session IDs** correlate related log entries across all files
- **Categories** filter by feature area
- **Timestamps** help with performance analysis and issue tracing
- **Debug files** provide detailed game state for troubleshooting
- **Crash dumps** include comprehensive context for support

### End User Support
When users report issues:
1. **Ask for Session ID** from any log file
2. **Request crash dump** if available
3. **Check debug logs** for the same session ID
4. **Correlate timestamps** to understand issue sequence

## 📚 **References**

- **Architecture**: `docs/ARCHITECTURE.md`
- **Testing**: `docs/TESTING.md`
- **ADRs**: `docs/adr/` - Architectural decisions
- **Blueprint**: `docs/BLUEPRINT.md` - Engineering standards
- **Changelog**: `CHANGELOG.md` - Version history

## 🤝 **Contributing**

1. Follow blueprint Package-by-Feature patterns
2. Use dependency injection for services
3. Add structured logging to new features
4. Update tests and documentation
5. Small, focused commits with clear messages

## 🆘 **Getting Help**

- **Architecture Questions**: Check ADRs and architecture docs
- **Bug Reports**: Include session ID from logs
- **Feature Requests**: Follow blueprint feature structure
- **Performance Issues**: Enable performance logging for diagnostics

## Quickstart

### Install & Build
- Open solution in Visual Studio 2022
- Build Enlisted (x64). Post-build copies the DLL into the game Modules folder

### Gameplay
- Talk to a lord and choose "I wish to enlist" to start service
- Your party is visually hidden; you follow your commander
- When your commander enters a settlement, the game normally pauses. While enlisted, time auto-resumes after a brief delay. This is intentional and avoids menu re-entrancy.
- Use the dialog option "I'd like to leave your service" to discharge

### Logs
- Logs are written to `Modules/Enlisted/debugging/` via `LoggingService`

### Troubleshooting
- If menus behave oddly, ensure no other mod drives settlement menus
- If time does not resume while enlisted, another mod may force pause; disable that behavior
