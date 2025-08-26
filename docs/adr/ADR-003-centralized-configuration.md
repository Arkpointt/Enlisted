# ADR-003: Centralized Configuration with Feature Flags

**Status:** Accepted  
**Date:** 2025-08-25  
**Deciders:** Development Team

## Context

The original mod had configuration scattered across multiple locations and hardcoded values throughout the codebase. This made it difficult to:

1. **Tune Behavior**: Required code changes for simple parameter adjustments
2. **Toggle Features**: No way to enable/disable features without code modification
3. **Support Users**: Difficult to provide different configurations for different playstyles
4. **Debug Issues**: No way to enable debug features in production builds

## Decision

We will implement a **centralized configuration system** following the blueprint's "Config over Code" principle:

### Core Configuration Class
```csharp
// src/Core/Config/ModSettings.cs
public class ModSettings
{
    // Tunable Parameters
    public int DailyWage { get; set; } = 10;
    
    // Feature Flags  
    public bool EnableDebugLogging { get; set; } = false;
    public bool UseAdvancedPartyHiding { get; set; } = true;
    
    // Validation and Safe Loading
    private static ModSettings ValidateSettings(ModSettings settings);
}
```

### Configuration Precedence
1. **In-game settings** (future implementation)
2. **User config files** (`settings.xml`)
3. **Safe defaults** (hardcoded fallbacks)

### Feature Flag Usage
```csharp
// Enable/disable features at runtime
if (ModSettings.Instance.UseAdvancedPartyHiding)
{
    // Use reflection-based party hiding
}
else 
{
    // Use basic visibility toggle
}
```

## Consequences

### Positive
- **Flexibility**: Users can tune behavior without code changes
- **Safety**: Validation ensures settings are within safe bounds
- **Debuggability**: Debug features can be toggled in production
- **A/B Testing**: Can test different configurations easily
- **Support**: Easier to diagnose issues with known configurations

### Negative
- **Complexity**: Need to handle configuration loading, validation, and defaults
- **Migration**: Existing hardcoded values need to be parameterized
- **Testing**: Must test various configuration combinations

### Neutral
- **Performance**: Minimal impact from configuration lookups
- **File Size**: Small increase due to configuration infrastructure

## Implementation Guidelines

1. **Fail Closed**: Invalid configurations fall back to safe defaults
2. **Validation**: All settings validated on load with actionable error messages
3. **Documentation**: Each setting documented with purpose and valid range
4. **Feature Flags**: Boolean flags for experimental or debug features
5. **Immutable After Load**: Settings don't change during gameplay session

## Configuration Categories

### Gameplay Parameters
- Daily wage amounts
- XP progression rates
- Battle participation thresholds

### Feature Toggles
- Advanced party hiding mechanics
- Debug logging levels
- Experimental features

### Debug Options
- Verbose logging
- Performance monitoring
- State validation checks

## Future Enhancements

1. **In-Game UI**: MCM (Mod Configuration Menu) integration
2. **Hot Reload**: Configuration changes without game restart
3. **Profiles**: Multiple configuration profiles for different playstyles
4. **Telemetry**: Anonymous usage statistics for tuning defaults

## Compliance

This decision implements:
- Blueprint Section 10: "Configuration & Mod Settings"
- Blueprint Principle: "Config over code"
- Blueprint Principle: "Fail closed"
