# Enlisted Mod Debugging Guide

## Log Files Location

All debug logs are stored in: `C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\Enlisted\debugging\`

## Log Files

### 1. `user_log.txt`
- **Purpose**: User-facing messages for troubleshooting
- **Format**: `[Timestamp] [LEVEL] [Category] Message`
- **Levels**: DEBUG, INFO, WARNING, ERROR, EXCEPTION
- **Use**: Check this file first when troubleshooting issues

### 2. `api_calls.txt`
- **Purpose**: Detailed API call documentation (debugging only)
- **Format**: Detailed API call logs with parameters and results
- **Use**: For developers to track API usage and debug integration issues
- **Note**: This will be removed in release builds

### 3. `session_log.txt`
- **Purpose**: Session-specific logs with limited history
- **Format**: Session events and important milestones
- **Use**: Track mod lifecycle and session state

## Session Management

- **3 Sessions Kept**: Only the last 3 session log files are retained
- **Auto-Cleanup**: Old log files are automatically deleted
- **Session ID**: Each session gets a unique timestamp-based ID

## Troubleshooting Steps

### 1. Check if Mod is Loading
Look in `user_log.txt` for:
```
[INFO] [SubModule] Enlisted mod loading...
[INFO] [SubModule] Harmony initialized successfully
[INFO] [SubModule] Game started, registering behaviors...
[INFO] [SubModule] EnlistmentBehavior registered successfully
```

### 2. Check Dialog Registration
Look for:
```
[INFO] [EnlistmentBehavior] Session launched, registering enlistment dialogs...
[INFO] [EnlistmentBehavior] Enlistment dialogs registered successfully
```

### 3. Check Conversation Conditions
When talking to lords, look for:
```
[DEBUG] [EnlistmentBehavior] CanAskToEnlist: All conditions met for [Lord Name], returning true
```

### 4. Common Issues

**Mod Not Loading:**
- Check if Bannerlord.Harmony is enabled
- Verify SubModule.xml is in the correct location
- Check for any ERROR entries in user_log.txt

**No Conversation Options:**
- Check if CanAskToEnlist is being called
- Verify the lord has a party (PartyBelongedTo != null)
- Check if player is already enlisted

**API Call Issues:**
- Review api_calls.txt for failed API calls
- Check parameter types and values
- Verify method signatures match current game version

## Log Categories

- **SubModule**: Module loading and initialization
- **EnlistmentBehavior**: Enlistment system behavior
- **LoggingService**: Logging system itself
- **GameAdapters**: API integration (when implemented)

## Removing Debug Logs for Release

To remove API call logging for release:
1. Remove all `LoggingService.LogApiCall()` calls
2. Remove the `api_calls.txt` file generation
3. Set debug logging to INFO level only

## Support

When reporting issues, include:
1. The relevant section from `user_log.txt`
2. Any ERROR or EXCEPTION entries
3. The game version and mod version
4. Steps to reproduce the issue

