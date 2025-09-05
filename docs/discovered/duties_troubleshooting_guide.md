# Duties System User-Friendly Logging Guide

**Streamlined logging approach - minimal performance impact with essential troubleshooting capability**

## 🎯 Streamlined Logging Strategy

### What We Log (Minimal)
1. **Critical Errors Only**: Configuration failures, mod conflicts, game update issues
2. **User Feedback**: In-game notifications instead of log entries for normal events
3. **Basic Troubleshooting**: Essential error context for support scenarios

### What We Don't Log (Performance Friendly)
1. **Successful Operations**: Equipment application, duty assignments, officer role activation
2. **Routine Processing**: Daily ticks, hourly updates, menu initialization
3. **Debug Information**: Detailed state tracking, requirement checking, success confirmations

## 📋 Essential Error Logging Only

### Configuration Loading (Silent Success)
```csharp
private void LoadConfiguration()
{
    try
    {
        var json = File.ReadAllText(GetConfigPath());
        _config = JsonConvert.DeserializeObject<DutiesConfig>(json);
        // Silent success - no logging needed
    }
    catch (Exception ex)
    {
        // Only log when something goes wrong
        ModLogger.Error("Config", "Configuration loading failed - using defaults", ex);
    }
}
```

### User Experience (In-Game Notifications)
```csharp
// Troop type selection - user gets notification, not log entry
public void SelectTroopType(string troopType)
{
    _playerTroopType = Enum.Parse<TroopType>(troopType, true);
    ApplyTroopTypeKit();
    
    // User feedback via game UI
    var message = new TextObject("Specialization selected: {TROOP_TYPE}. New equipment issued.");
    message.SetTextVariable("TROOP_TYPE", GetDisplayName());
    InformationManager.AddQuickInformation(message, 0, Hero.MainHero.CharacterObject, "");
    // No logging needed - player sees the result directly
}
```

### Compatibility Monitoring (Error-Only)
```csharp
// Harmony patches - silent success, log only critical failures
protected override void OnSubModuleLoad()
{
    try
    {
        _harmony = new Harmony("com.enlisted.mod");
        _harmony.PatchAll();
        // Silent success - no logging spam
    }
    catch (Exception ex)
    {
        ModLogger.Error("Compatibility", "Mod initialization failed", ex);
    }
}
```

## 📄 Typical Log Output

### Normal Gameplay (Minimal)
```
Location: <Bannerlord>\Modules\Enlisted\Debugging\enlisted.log

2025-01-15 14:23:01.123 [a1b2c3] [INFO] [Init] Enlisted mod loaded

// That's it for normal gameplay! Nearly empty log file.
```

### When Something Breaks
```
2025-01-15 16:45:12.456 [a1b2c3] [ERROR] [Config] duties_config.json not found - using defaults
2025-01-15 17:30:25.789 [a1b2c3] [ERROR] [Equipment] Equipment kit failed: empire_infantry_t5 [FileNotFoundException]
2025-01-15 18:15:33.012 [a1b2c3] [ERROR] [Compatibility] Harmony patch failed - possible game update
```

## 🎯 User Support Benefits

### For End Users
- **Smooth Performance**: Zero logging overhead during normal play
- **Clean Experience**: No notification spam or debug messages  
- **Clear Feedback**: Equipment changes and promotions shown in game UI
- **Problem Indication**: Clear error messages when something breaks

### For Support/Troubleshooting
- **Session Tracking**: Unique session ID for correlating user reports
- **Essential Context**: When errors occur, log shows what failed and why
- **Compatibility Detection**: Identifies game updates or mod conflicts
- **Minimal Data**: Only failed operations logged for privacy and performance

---

## ⚡ Bottom Line: Lightweight & Effective

- **Performance**: Essentially zero impact - only errors logged
- **User Experience**: Smooth gameplay with in-game notifications  
- **Troubleshooting**: Essential error information when needed
- **Support**: Session-correlated logs for rapid issue diagnosis

**Perfect balance: Professional user experience with diagnostic capability when problems occur.**