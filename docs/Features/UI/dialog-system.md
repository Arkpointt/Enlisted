# Feature Spec: Centralized Dialog System

## Overview
Single conversation manager that handles all military service dialogs instead of scattering them across multiple behavior files.

## Purpose
Prevent dialog conflicts, simplify maintenance, and provide consistent conversation experience across all lords and military interactions.

## Inputs/Outputs

**Inputs:**
- Player conversation attempts with lords
- Current enlistment status 
- Player tier and military standing
- Lord relationship and faction info

**Outputs:**
- Enlistment confirmation and immediate menu activation
- Status check dialogs showing current military info
- Promotion and equipment management conversations
- Retirement and departure dialogs

## Behavior

**Enlistment Flow:**
1. Talk to any lord → "I have something else to discuss" → "I wish to serve in your warband"
2. **Army Leader Check**: If player is leading their own army, special dialog appears:
   - Player: "My lord, I offer you my sword and my loyalty. Will you have me in your ranks?"
   - Lord: "Hold, friend. I see you already command men of your own. A general cannot become a foot soldier while lords still march beneath his banner. Disband your army first, then we may speak of service."
   - Player: "I understand, my lord. I shall see to my army's affairs first."
   - Conversation ends (player cannot proceed with enlistment)
3. Lord responds based on relationship and faction status
4. Player confirms → Immediate enlistment with `IsActive = false` and menu switch
5. No encounter gaps - player goes straight to enlisted status menu

**Status Dialogs:**
- Centralized conditions check enlistment state
- Shared consequences apply changes consistently
- Dialog IDs managed centrally to prevent conflicts

## Technical Implementation

**Files:**
- `EnlistedDialogManager.cs` - Single hub for all military conversations

**Registration Pattern:**
```csharp
public override void RegisterEvents()
{
    // Register all dialog flows in one place
    CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
}

private void OnSessionLaunched(CampaignGameStarter starter)
{
    RegisterEnlistmentDialogs(starter);
    RegisterStatusDialogs(starter);  
    RegisterManagementDialogs(starter);
}
```

**Dialog Structure:**
- Consistent dialog line IDs (no conflicts)
- Shared condition methods (`CanEnlistWithLord`, `IsPlayerEnlisted`, etc.)
- Shared consequence methods (`StartEnlistment`, `ShowStatus`, etc.)

## Edge Cases

**Multiple Dialog Behaviors:**
- Only one behavior can own each dialog ID
- Centralized manager prevents conflicts between features

**Lord State Changes:**
- Handles lord death/capture during conversations
- Updates dialog availability based on current lord status
- Graceful fallback if enlisted lord becomes unavailable

**Invalid Enlistment Attempts:**
- Already enlisted with different lord
- Player is leading their own army (shows roleplay rejection dialog)
- Lord not available for service
- Player in incompatible state (prisoner, etc.)

## Acceptance Criteria

- ✅ All military dialogs work from single manager
- ✅ No dialog ID conflicts between features  
- ✅ Consistent conversation experience across all lords
- ✅ Immediate menu activation after enlistment (no encounter gaps)
- ✅ Status dialogs show current military information accurately
- ✅ Easy to add new conversations (single file to modify)

## Debugging

**Common Issues:**
- **Dialog doesn't appear**: Check dialog conditions return true for current game state
- **"Dialog already registered"**: Another behavior is trying to use same dialog ID
- **Menu doesn't activate**: Check consequence methods are properly applied

**Log Categories:**
- "Dialog" - Dialog registration and conversation flow
- Look for conversation events in `Modules\Enlisted\Debugging\dialog.log`
