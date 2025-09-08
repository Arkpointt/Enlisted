# Feature Spec: Automatic Battle Commands Filtering

## Overview
Background system that automatically filters battle commands based on the player's assigned formation type, providing immersive military communication during combat without any user configuration.

## Purpose
Enhance battle immersion by ensuring enlisted soldiers only hear tactical orders relevant to their specific formation role (Infantry, Archer, Cavalry, Horse Archer), creating realistic military command structure and reducing information overload.

## Inputs/Outputs

**Inputs:**
- Player's current formation assignment from troop selection system
- Battle command events from `BehaviorComponent.InformSergeantPlayer()`
- Formation type of the commanding behavior
- Player's enlistment status and lord relationship

**Outputs:**
- Filtered command messages displaying only relevant formation orders
- Audio cues with appropriate horn sounds (attack vs movement)
- Commands attributed to enlisted lord with portrait display
- No configuration required - works automatically in background

## Behavior

**Command Detection:**
1. Battle behavior triggers command (charge, hold position, etc.)
2. System checks if player is enlisted and has assigned formation
3. Command's target formation compared to player's assignment
4. Only matching commands are displayed to player

**Formation Mapping:**
- Infantry commands → Infantry enlisted soldiers only
- Archer commands → Archer enlisted soldiers only  
- Cavalry commands → Cavalry enlisted soldiers only
- Horse Archer commands → Horse Archer enlisted soldiers only

**Audio Integration:**
- Attack commands (Charge, Skirmish, etc.) → Attack horn sound
- Movement commands (Advance, Hold, etc.) → Movement horn sound
- Commands display with enlisted lord's portrait for authenticity

## Technical Implementation

**Files:**
- `BattleCommandsFilterPatch.cs` - Harmony patch for automatic command filtering

**Key Mechanisms:**
```csharp
[HarmonyPatch(typeof(BehaviorComponent), "InformSergeantPlayer")]
[HarmonyPriority(999)]
public class BattleCommandsFilterPatch
{
    static void Postfix(BehaviorComponent __instance)
    {
        // Check enlistment status and formation match
        var playerFormation = EnlistedDutiesBehavior.Instance?.PlayerFormation;
        var commandFormation = GetFormationTypeFromClass(__instance.Formation.PhysicalClass);
        
        // Only show if formations match
        if (commandFormation.ToLower() == playerFormation.ToLower())
        {
            // Display command with lord attribution and audio
        }
    }
}
```

**Integration Points:**
- Uses existing formation detection from `EnlistedDutiesBehavior.PlayerFormation`
- Integrates with troop selection system for automatic assignment
- Works with officer role system for enhanced command context

## Edge Cases

**Formation Not Yet Assigned:**
- Defaults to allowing all commands until formation selected
- No commands shown if player not enlisted
- Graceful handling of missing formation data

**Formation Type Changes:**
- Automatically updates when player selects new troop type
- Works seamlessly with Master at Arms promotion system
- No configuration or restart required

**Battle State Validation:**
- Only processes commands for player team or allies
- Validates behavior component and formation data
- Filters out generic behaviors that aren't tactical commands

**Audio System Issues:**
- Audio playback is optional - command display works without sound
- Graceful fallback if sound events unavailable
- No impact on core functionality if audio fails

## Acceptance Criteria

- ✅ Commands automatically filtered based on player's formation assignment
- ✅ No user configuration or menu options required
- ✅ Integration with existing formation detection system
- ✅ Appropriate audio cues for different command types
- ✅ Commands attributed to enlisted lord with portrait display
- ✅ Works seamlessly with troop selection and promotion systems
- ✅ No performance impact or battle interruption
- ✅ Graceful handling of edge cases and state changes

## Debugging

**Common Issues:**
- **No commands showing**: Check enlistment status and formation assignment
- **All commands showing**: Verify formation detection is working correctly
- **Audio not playing**: Check sound event availability (audio is optional)
- **Wrong commands showing**: Verify formation mapping logic

**Log Categories:**
- "BattleCommands" - Command filtering and display
- "Enlistment" - Enlistment status validation
- "Duties" - Formation assignment tracking

**Testing:**
- Enlist with lord, select formation, enter battle
- Verify only appropriate formation commands display
- Test with different formations (Infantry/Archer/Cavalry/Horse Archer)
- Check audio cues and lord portrait attribution
