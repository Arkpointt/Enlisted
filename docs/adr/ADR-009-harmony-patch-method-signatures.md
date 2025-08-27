# ADR-009: Harmony Patch Method Signature Verification

## Status
**ACCEPTED** - 2025-08-26

## Context

During development of game integration patches (ADR-008), we encountered critical runtime crashes caused by incorrect Harmony patch method signatures. The patches were targeting methods that either didn't exist or had different signatures than expected, causing the game to crash during mod initialization.

### Root Cause Analysis
1. **Outdated Reference Sources**: Initial patch development relied on decompiled code from older mods (e.g., "ServeAsSoldier") rather than current game DLL references
2. **Incorrect Method Signatures**: 
   - `PlayerArmyWaitBehavior` methods were assumed to be static but are actually instance methods
   - Parameter signatures didn't match actual game code
   - String-based Harmony targeting was fragile and provided poor error feedback
3. **Poor Error Handling**: Failed patches caused complete game crashes instead of graceful degradation

### Impact
- Complete game crashes during mod loading
- Significant debugging time (multiple hours)
- Unreliable patch application
- Poor developer experience

## Decision

**We will establish mandatory practices for Harmony patch development to ensure reliability and maintainability.**

### Mandatory Requirements

1. **Use Current Game DLL Decompiled References Only**
   - Always decompile DLLs from the target game version
   - Never rely on mod source code for method signatures
   - Verify signatures in actual TaleWorlds assemblies

2. **Explicit Method Targeting with Error Handling**
   - Use `TargetMethod()` with `AccessTools.Method()` 
   - Include parameter type arrays for exact matching
   - Implement graceful error handling for missing methods
   - Log patch success/failure for debugging

3. **Signature Verification Process**
   - Document exact method signatures with static/instance nature
   - Verify parameter types and order
   - Test patch application in controlled environment
   - Monitor logs for patch status

### Implementation Pattern

```csharp
[HarmonyPatch]
public static class ExamplePatch
{
    public static MethodBase TargetMethod()
    {
        try
        {
            var type = AccessTools.TypeByName("Full.Type.Name");
            if (type == null)
            {
                LogPatchError("Could not find target type");
                return null;
            }

            var method = AccessTools.Method(type, "MethodName", new[] { typeof(ParamType) });
            if (method == null)
            {
                LogPatchError("Could not find target method");
                return null;
            }

            LogPatchSuccess("Successfully found target method");
            return method;
        }
        catch (Exception ex)
        {
            LogPatchError($"Exception finding patch target: {ex.Message}");
            return null;
        }
    }

    [HarmonyPrefix]
    private static bool Prefix(/* exact signature from decompiled code */)
    {
        try
        {
            // Implementation with error handling
            return true;
        }
        catch (Exception ex)
        {
            LogPatchError($"Exception in patch execution: {ex.Message}");
            return true; // Fail open to prevent game crashes
        }
    }
}
```

## Consequences

### Positive
- **Reliable Patch Application**: Patches either work correctly or fail gracefully
- **Better Debugging**: Clear error messages for failed patches
- **Version Compatibility**: Explicit signature verification catches breaking changes
- **Maintainability**: Self-documenting patch targets and error states
- **Developer Experience**: Faster debugging and more confidence in patches

### Negative
- **Additional Development Time**: Requires decompiling and verifying each target method
- **More Complex Code**: Additional error handling and logging infrastructure
- **Maintenance Overhead**: Must update decompiled references for new game versions

### Mitigation Strategies
- **Centralized Logging**: Standardized error reporting across all patches
- **Documentation**: Maintain current method signature reference in blueprint
- **Testing Protocol**: Verify patch application during build process
- **Graceful Degradation**: Patches fail safely without breaking core functionality

## Implementation

### Completed (2025-08-26)
- [x] Updated all existing patches with proper `TargetMethod()` implementation
- [x] Added comprehensive error handling and logging
- [x] Verified method signatures against game version 1.2.12.77991
- [x] Documented correct signatures in blueprint appendix
- [x] Tested successful patch application

### Next Steps
- [ ] Create automated tests for patch application
- [ ] Establish process for game version updates
- [ ] Document patch testing procedures
- [ ] Create tooling for signature verification

## References
- Game Version: Mount & Blade II Bannerlord v1.2.12.77991
- Harmony Documentation: https://harmony.pardeike.net/
- Related ADRs: ADR-008 (Game Integration Patches)
- Blueprint Section: Appendix C - Harmony Patch Development

## Lessons Learned

1. **Always verify against current game code**, not legacy mod sources
2. **Explicit targeting is more reliable** than string-based attributes
3. **Error handling prevents crashes** and improves debugging experience
4. **Logging is critical** for understanding patch behavior in production
5. **Method nature (static vs instance) is crucial** for correct patching

This ADR establishes practices that prevent similar issues in future development and ensures reliable mod functionality across game updates.
