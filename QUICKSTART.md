# Quick Start Guide - Enlisted Mod Development

## 🚀 **Getting Started**

### Prerequisites
- Visual Studio 2022
- Mount & Blade II: Bannerlord installed
- .NET Framework 4.7.2 SDK

### Build & Run
```bash
# Clone repository
git clone https://github.com/Arkpointt/Enlisted
cd Enlisted

# Build mod
msbuild Enlisted_Blueprint.sln /p:Configuration=Debug

# Files automatically copied to Bannerlord Modules directory
# Launch Bannerlord and enable "Enlisted" mod
```

## 🏗️ **Architecture Overview**

### Package-by-Feature Structure
```
src/
├── Features/           # Self-contained gameplay features
│   ├── Enlistment/    # Contract management, commander tracking
│   ├── Promotion/     # XP system, tier advancement
│   └── Wages/         # Daily payment system
├── GameAdapters/      # TaleWorlds API isolation layer
├── Core/              # Shared infrastructure
└── Mod.Entry/         # Thin initialization layer
```

### Key Services
- **ILoggingService**: Centralized structured logging
- **IEnlistmentService**: Enlistment state abstraction
- **IServiceContainer**: Dependency injection container

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
// Get logger through DI
if (ServiceLocator.TryGetService<ILoggingService>(out var logger))
{
    logger.LogInfo(LogCategories.YourFeature, "Operation completed: {0}", result);
    logger.ShowPlayerMessage("Success!");
}
```

### Using Services
```csharp
// Access enlistment state
if (ServiceLocator.TryGetService<IEnlistmentService>(out var enlistment))
{
    if (enlistment.IsEnlisted)
    {
        // Handle enlisted logic
    }
}
```

## ⚙️ **Configuration**

### User Settings
Create `settings.xml` in `Modules/Enlisted/`:
```xml
<ModSettings>
  <EnableDebugLogging>true</EnableDebugLogging>
  <LogLevel>0</LogLevel>
  <DailyWage>15</DailyWage>
</ModSettings>
```

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

### Manual Testing Checklist
- [ ] Mod loads without errors
- [ ] Can enlist with lords
- [ ] Army menus are suppressed when enlisted
- [ ] Battle participation works
- [ ] Save/load preserves state
- [ ] Daily wages are paid
- [ ] Promotion XP accumulates

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

### Common Issues
- **Services not available**: Check ServiceLocator initialization
- **Static instance access**: Transition period - both DI and static work
- **Build errors**: Ensure all references point to correct Bannerlord installation

### Log Analysis
- Session IDs correlate related log entries
- Categories filter by feature area
- Timestamps help with performance analysis

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
