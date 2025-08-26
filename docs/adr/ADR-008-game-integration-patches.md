# ADR-008: Game Integration Patches

**Status:** Accepted  
**Date:** 2025-01-12  
**Deciders:** Development Team

## Context

The Enlisted mod features require deep integration with Mount & Blade II: Bannerlord's existing systems. The game doesn't provide official APIs for many required behaviors (hiding player parties, suppressing menu options, detecting battle participation).

Integration challenges identified:
1. **Menu System Conflicts**: Game's army menus offer "leave army" options that break enlistment contracts
2. **Party Visibility**: No official API to hide player party nameplates for immersion
3. **Battle Detection**: Need to hook into battle events for promotion XP awards
4. **UI State Management**: Must prevent conflicting player actions during enlistment

## Decision

We implement **Harmony-based Game Patches** to integrate with TaleWorlds systems safely:

### Patch Categories

#### 1. Army Menu Suppression (SuppressArmyMenuPatch)
**Purpose**: Prevent contract-breaking menu options during enlistment
**Targets**:
- `PlayerArmyWaitBehavior.wait_menu_army_leave_on_condition`
- `PlayerArmyWaitBehavior.wait_menu_army_abandon_on_condition`
- `VillageHostileActionCampaignBehavior` raiding menu conditions
- `DefaultEncounterGameMenuModel.GetGenericStateMenu`

**Strategy**: Prefix patches that return `false` to suppress problematic menu options

#### 2. Party Nameplate Hiding (HidePartyNamePlatePatch)
**Purpose**: Hide player party nameplate for enlisted soldier immersion
**Targets**:
- `PartyNameplateVM.RefreshBinding` 
- Map overlay rendering systems

**Strategy**: Postfix patches that conditionally hide nameplate based on enlistment status

#### 3. Battle Participation Detection (BattleParticipationPatch)
**Purpose**: Award promotion XP for battle participation
**Targets**:
- Battle end events
- Campaign battle result processing

**Strategy**: Postfix patches that trigger XP awards after battle completion

### Patch Design Principles

#### Safety First
- **Early Returns**: Check enlistment status before any modifications
- **Original Preservation**: Use Prefix `false` return to skip, not replace original logic
- **Null Checks**: Validate all game objects before accessing properties
- **Exception Handling**: Wrap patch logic in try-catch blocks

#### Performance Considerations
- **Minimal Allocations**: Avoid creating objects in patch methods
- **Fast Path Checks**: Quick enlistment status validation
- **Targeted Patches**: Only patch specific methods, not entire classes
- **Logging Control**: Debug logging only when explicitly enabled

#### Maintainability
- **Clear Documentation**: Each patch explains its purpose and target
- **Attribute-Based Targeting**: Use HarmonyPatch attributes for clarity
- **Centralized Logic**: Shared helper methods for enlistment status checks
- **Version Resilience**: Target stable game APIs where possible

## Implementation Details

### Patch Structure Pattern
```csharp
[HarmonyPatch("TargetClass", "TargetMethod")]
[HarmonyPrefix/Postfix]
private static bool/void PatchMethod(ref bool __result)
{
    // 1. Quick enlistment status check
    if (!TryGetEnlistmentService(out var service) || !service.IsEnlisted)
        return true; // Execute original
    
    // 2. Apply modification
    LogDecision("Patch action taken");
    __result = false; // Suppress original behavior
    return false; // Skip original execution
}
```

### Service Integration
- **Dependency Injection**: Prefer ServiceLocator for service resolution
- **Fallback Strategy**: Support static instance access during transition
- **Logging Integration**: Use centralized logging service when available

### Error Handling Strategy
- **Fail-Safe**: Patches should never crash the game
- **Graceful Degradation**: Fall back to original behavior on errors
- **Debug Information**: Log patch failures for troubleshooting

## Consequences

### Positive
- **Deep Integration**: Access to game systems not exposed in official APIs
- **Immersive Experience**: Seamless integration with existing game UI/UX
- **Flexible Control**: Can modify any game behavior as needed
- **Version Compatibility**: Harmony provides some resilience to game updates

### Negative
- **Update Fragility**: Game updates may break patches
- **Debugging Complexity**: Harder to troubleshoot when patches interact
- **Performance Risk**: Poorly written patches can impact game performance
- **Compatibility Issues**: May conflict with other mods using same patch points

### Neutral
- **Development Overhead**: Requires understanding game internals
- **Testing Requirements**: Must test patches across different game scenarios

## Compatibility Considerations

### Game Version Targeting
- **Target**: Mount & Blade II: Bannerlord 1.2.x stable branch
- **Testing**: Verify patches on each game update
- **Fallback**: Graceful degradation when patch targets change

### Mod Compatibility
- **Patch Conflicts**: Document known conflicting mods
- **Load Order**: Recommend loading after major overhaul mods
- **Shared Targets**: Coordinate with other mod developers when possible

## Risk Mitigation

### Patch Failure Handling
```csharp
try
{
    // Patch logic
}
catch (Exception ex)
{
    Logger.LogError("Patch failed safely", ex);
    return true; // Execute original method
}
```

### Testing Strategy
- **Automation**: Unit tests for patch logic where possible
- **Integration**: Manual testing of patch interactions
- **Regression**: Verify patches after each game update

## Future Considerations

- **Official API Adoption**: Replace patches with official APIs when available
- **Patch Reduction**: Minimize patch count through better design
- **Community Coordination**: Share patch strategies with other mod developers
- **Performance Optimization**: Profile patch impact and optimize hotpaths

## Compliance

This decision implements:
- Blueprint Section 5.1: "External System Integration"
- Blueprint Section 3.4: "Fail-Safe Patch Design"
- Blueprint Section 4.4: "Performance-Conscious Implementation"
