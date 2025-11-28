# Temporary Leave System

## Overview
Allows enlisted players to request temporary leave from military service, restoring vanilla gameplay while preserving service data for later return. Leave has a 14-day time limit after which desertion penalties apply.

## Purpose
- Provide flexibility for players who want to pursue independent activities (trading, quests, companion recruitment)
- Maintain service relationship without permanent discharge
- Enforce realistic military leave policies with time limits
- Handle companion and troop management during service transitions

## Inputs/Outputs
- **Inputs**
  - Enlistment state: `EnlistmentBehavior.Instance` (lord, tier, XP)
  - Leave request: Dialog with lord
  - Return request: Dialog with lord when on leave
  - Player party state: companions and recruited troops
- **Outputs**
  - Suspended service: `_isOnLeave = true`, `_leaveStartDate` recorded
  - Vanilla behavior: normal encounters, movement, visibility restored
  - Daily countdown: warnings at 7 days and below
  - Troop transfers: companions/recruits join lord's party on return
  - Dialog integration: return option appears in lord conversations

## Behavior

### Request Leave
- Dialog option: "I would like to request leave from service" when talking to lord
- Confirmation dialog warns about 14-day time limit
- On accept: calls `EnlistmentBehavior.StartTemporaryLeave()`
- Records `_leaveStartDate = CampaignTime.Now`

### Leave State
- Sets `_isOnLeave = true` (disables all enlistment behavior)
- Restores vanilla player state: `IsActive = true`, `IsVisible = true`
- Clears escort AI and encounter state
- Real-time tick skips all enlistment logic
- Status menu shows remaining leave days

### Leave Timer
- 14-day maximum (configurable via `leave_max_days` in `enlisted_config.json`)
- Daily warnings when 7 or fewer days remain
- Warning message: "Your leave expires in {DAYS} days. Return to your lord soon or face desertion penalties."
- At 0 days: desertion penalties applied, service terminated

### Return to Service
- Dialog option: "I wish to return to service" (appears when on leave talking to former lord)
- Transfers any new companions/troops to lord's party
- Restores enlistment behavior: invisible, escort, battle participation
- Clears leave state: `_isOnLeave = false`, `_leaveStartDate` reset

### Transfer Service (While on Leave)
- Dialog option: "I wish to transfer my service to your command" (appears when talking to different lord in same faction)
- Preserves all progression: tier, XP, kills, service date
- Transfers companions/troops to new lord's party
- Clears leave state and begins active service with new lord
- Notification: "You have transferred your service to [Lord]. Your rank and experience have been preserved."

### Desertion Penalties (Leave Expiration)
- Service terminated with dishonorable discharge
- Relation penalty with former lord
- Possible faction reputation loss
- Cannot re-enlist with same faction until cooldown expires

### Retirement vs Leave
- **Leave**: Temporary suspension, 14-day limit, all data preserved, must return
- **Retirement**: Permanent discharge, companions restored to player, full benefits if eligible

## Technical Implementation

### Files Modified
- `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`
  - Added `_isOnLeave` and `_leaveStartDate` state fields
  - Modified `IsEnlisted` property: `_enlistedLord != null && !_isOnLeave`
  - Added `StartTemporaryLeave()` and `ReturnFromLeave()` methods
  - Added `TransferServiceToLord(Hero newLord)` for same-faction transfers
  - Added `CheckLeaveExpiration()` in OnDailyTick
  - Added `TransferPlayerTroopsToLord()` using proper party transfer logic

- `src/Features/Interface/Behaviors/EnlistedMenuBehavior.cs`
  - Shows remaining leave days in status display
  - Calculates: `LeaveMaxDays - (CampaignTime.Now - LeaveStartDate).ToDays`

- `src/Features/Conversations/Behaviors/EnlistedDialogManager.cs`
  - Leave request dialog with lord
  - Return to service dialog option (with original lord)
  - Service transfer dialog option (with different same-faction lord)
  - `CanRequestServiceTransfer()` condition for transfer dialog
  - `OnAcceptServiceTransfer()` consequence handler

- `ModuleData/Enlisted/enlisted_config.json`
  - Added `gameplay.leave_max_days: 14`

### Leave Expiration Check (OnDailyTick)
```csharp
private void CheckLeaveExpiration()
{
    if (!_isOnLeave || _leaveStartDate == default) return;
    
    var config = EnlistedConfig.LoadGameplayConfig();
    var maxLeaveDays = config.LeaveMaxDays;
    var daysOnLeave = (int)(CampaignTime.Now - _leaveStartDate).ToDays;
    var remainingDays = maxLeaveDays - daysOnLeave;
    
    if (daysOnLeave > maxLeaveDays)
    {
        // Apply desertion penalties
        ModLogger.Info("Leave", "Leave expired - applying desertion penalties");
        ApplyDesertionPenalties();
        StopEnlist("Leave expired - desertion", isHonorableDischarge: false);
    }
    else if (remainingDays <= 7 && remainingDays > 0)
    {
        // Daily warning
        var message = $"Your leave expires in {remainingDays} days. Return soon!";
        InformationManager.DisplayMessage(new InformationMessage(message));
    }
}
```

### APIs Used (Verified against 1.3.6 decompile)
- `PlayerEncounter.Current`, `PlayerEncounter.Finish(true)`
- `CampaignTime.Now`, `CampaignTime.ToDays`
- `MemberRoster.AddToCounts()` for troop transfers
- `ChangeRelationAction.ApplyPlayerRelation()` for desertion penalties

## Edge Cases
- **Encounter cleanup**: Proper `PlayerEncounter` state clearing on leave start
- **Save during leave**: Leave state and start date persist correctly
- **Lord death during leave**: Grace period started, player can re-enlist or transfer
- **Leave expires in combat**: Checked only in OnDailyTick, not during active battles
- **Menu display**: Remaining days shown when viewing status while on leave
- **Service transfer during leave**: Player can transfer to different lord in same faction, preserving all progression
- **Transfer to different faction**: Not allowed - transfer dialog only shows for same-faction lords

## Acceptance Criteria
- ✅ Player can request leave via dialog with lord
- ✅ Leave restores vanilla gameplay (movement, encounters, recruitment)
- ✅ 14-day timer enforced with daily warnings at 7 days or less
- ✅ Desertion penalties applied if timer expires
- ✅ Dialog option to return appears when talking to former lord while on leave
- ✅ Service transfer option appears when talking to different lord in same faction
- ✅ Service data (tier, XP, lord relationship) preserved during leave and transfer
- ✅ Remaining leave days displayed in status menu
- ✅ No crashes when entering settlements after leave/return/transfer

## Companion and Troop Management

### Leave Flow
1. **Request Leave**: Player party restored to vanilla state
2. **During Leave**: Player can recruit freely with vanilla behavior
3. **Return from Leave**: New companions/troops transfer to lord's party
4. **Leave Expires**: Desertion - companions return to player, troops stay with lord

### Party Transfer Pattern
```csharp
// Add to lord's party
lordParty.MemberRoster.AddToCounts(character, number, false, 0, 0, true, -1);
// Remove from player party  
playerParty.MemberRoster.AddToCounts(character, -1 * number, false, 0, 0, true, -1);
```
