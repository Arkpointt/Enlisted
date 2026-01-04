# Logging Spam Audit - 2025-01-01

## Summary

Audited the mod's logging system to ensure end-user logs are clean and production-friendly. Found and fixed excessive Info-level logging in development/diagnostic code.

## Findings

### ✅ Good: Default Log Level
- Default log level is `Info` (line 47 in ModLogger.cs)
- Debug and Trace logs are OFF by default for end users
- 765 Debug/Trace calls across 87 files are properly gated

### ✅ Good: Anti-Spam Features
- Throttling system prevents repeated messages (5 second default window)
- `LogOnce()` for one-time messages per session
- Exception detail de-duplication
- Session rotation keeps only 3 most recent logs (Session-A, Session-B, Session-C)

### ❌ Fixed: ContentOrchestrator Daily Tick Spam

**Problem:** `ContentOrchestrator.cs` logged extensively at Info level every in-game day, even when orchestrator was disabled.

**Lines affected:**
- Lines 146-147: "Orchestrator Active" and activity level (every day)
- Line 179: "Realistic frequency" when orchestrator disabled (every day, diagnostic mode)
- Lines 187, 201-244: `TestContentSelection()` diagnostic test (8+ log lines per day)

**Fix:**
1. Removed Info-level daily status messages (146-147, 179)
2. Changed `TestContentSelection()` to all Debug-level logging
3. Added early return if Debug logging is disabled (skips expensive selection tests)
4. Added single Debug-level summary line when orchestrator is active

**Impact:** Reduces daily log spam from 10+ lines per day to 0 lines (or 1 debug line if Debug level enabled).

### ❌ Fixed: Combat Log Link Debug Message

**Problem:** `EnlistedCombatLogBehavior.cs` line 293 logged every combat message containing links at Info level.

**Fix:** Changed to Debug level with clarified comment.

**Impact:** Eliminates per-message log spam during battles.

## Unchanged (Appropriate for End Users)

The following Info-level logs were reviewed and deemed appropriate:

### One-Time Initialization (Startup Only)
- SubModule load messages (3 lines)
- Behavior registration messages (39 behaviors, 1 line each)
- Catalog loading (EventCatalog, DecisionCatalog, OrderCatalog)
- System initialization confirmations

### Important User Actions
- Enlistment/discharge events
- Promotions and rank changes
- Order assignment/completion
- Pay day events
- Battle participation
- Medical treatment
- Equipment purchases
- Settlement entry/exit transitions

### Errors and Warnings
- All Error level logging remains unchanged
- Warn level for recoverable issues (appropriate)

## Recommendations

### For End Users
**Current default settings are production-ready:**
- Info level shows important events only
- No spam from frequent ticks or diagnostic code
- Errors and warnings clearly visible
- Session rotation keeps logs manageable

### For Development/Testing
**To enable diagnostic logging, add to `enlisted_config.json`:**
```json
{
  "logging": {
    "levels": {
      "ContentOrchestrator": "Debug",
      "EventPacing": "Debug",
      "Interface": "Debug"
    }
  }
}
```

This enables the diagnostic logs we moved to Debug level without affecting other categories.

### For Troubleshooting Mod Conflicts
**Enable all Debug logging:**
```json
{
  "logging": {
    "default_level": "Debug"
  }
}
```

## Build Verification

✅ Build succeeded with no warnings or errors  
✅ Configuration: Enlisted RETAIL  
✅ Output: `Modules\Enlisted\bin\Win64_Shipping_Client\Enlisted.dll`

## Blueprint Compliance

✅ Logging Standards (Blueprint lines 446-512)
- Uses ModLogger API correctly
- Appropriate categories
- Performance-friendly (throttling, de-duplication)
- Session rotation system active
- Logs to game install: `C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\Enlisted\Debugging\`

✅ Comment Style (Blueprint lines 236-258)
- Comments describe current behavior
- No "Phase X" references
- Professional and natural language

## Conclusion

**Status: ✅ End-User Friendly**

The logging system is now production-ready with:
- Zero daily tick spam in default configuration
- Diagnostic code gated behind Debug level
- Important events clearly logged
- Anti-spam features active
- Session management working correctly

Users will see clean logs showing only significant events (enlistment, battles, pay, promotions, orders) without development noise.
