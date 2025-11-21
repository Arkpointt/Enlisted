# Temporary Leave System

## Overview
Allows enlisted players to request temporary leave from military service, restoring vanilla gameplay while preserving service data for later return.

## Purpose
- Provide flexibility for players who want to pursue independent activities (trading, quests, companion recruitment)
- Maintain service relationship without permanent discharge
- Handle companion and troop management during service transitions

## Inputs/Outputs
- **Inputs**
  - Enlistment state: `EnlistmentBehavior.Instance` (lord, tier, XP)
  - Leave request: "Ask commander for leave" menu option
  - Return request: Dialog with former lord when on leave
  - Player party state: companions and recruited troops
- **Outputs**
  - Suspended service: `_isOnLeave = true`, `IsEnlisted` returns false
  - Vanilla behavior: normal encounters, movement, visibility restored
  - Troop transfers: companions/recruits join lord's party on return
  - Dialog integration: return option appears in lord conversations

## Behavior
1) **Request Leave**
   - Menu option: "Ask commander for leave" in enlisted_status
   - Confirmation dialog warns about losing wages/duties during leave
   - On accept: calls `EnlistmentBehavior.StartTemporaryLeave()`

2) **Leave State**
   - Sets `_isOnLeave = true` (disables all enlistment behavior)
   - Restores vanilla player state: `IsActive = true`, `IsVisible = true`
   - Clears escort AI and encounter state using `PlayerEncounter.Finish(true)`
   - Real-time tick skips all enlistment logic, ensures vanilla state

3) **Return to Service**
   - Dialog option: "I wish to return to service" (appears only when on leave talking to former lord)
   - Transfers any new companions/troops to lord's party using proper party transfer logic
   - Restores enlistment behavior: invisible, escort, battle participation

4) **Retirement vs Leave**
   - **Leave**: Temporary suspension, all data preserved, can return anytime
   - **Retirement**: Permanent discharge, companions restored to player, regular troops stay with lord

## Technical Implementation
### Files Modified
- `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`
  - Added `_isOnLeave` and `_leaveStartDate` state fields
  - Modified `IsEnlisted` property: `_enlistedLord != null && !_isOnLeave`
  - Added `StartTemporaryLeave()` and `ReturnFromLeave()` methods
  - Added `TransferPlayerTroopsToLord()` using proper party transfer logic
  - Added `RestoreCompanionsToPlayer()` for retirement companion restoration

- `src/Features/Interface/Behaviors/EnlistedMenuBehavior.cs`
  - Renamed "Talk to..." → "My Lord..." 
  - Added nearby lord detection and selection inquiry
  - Added leave request confirmation dialog
  - Added `CampaignMapConversation.OpenConversation()` integration

- `src/Features/Conversations/Behaviors/EnlistedDialogManager.cs`
  - Added "I wish to return to service" player dialog option
  - Added lord response and return consequence handler
  - Added `CanReturnFromLeave()` condition checking

### APIs Used (Verified against 1.2.12 decompile)
- `PlayerEncounter.Current`, `PlayerEncounter.Finish(true)`, `PlayerEncounter.InsideSettlement`, `PlayerEncounter.LeaveSettlement()`
- `MemberRoster.AddToCounts(character, number, false, 0, 0, true, -1)` for troop transfers
- `CampaignMapConversation.OpenConversation()` for lord conversations
- `MultiSelectionInquiryData` with `ImageIdentifier(CharacterCode.CreateFrom())` for portraits

## Edge Cases
- **Encounter cleanup**: `PlayerEncounter.LeaveSettlement()` before `Finish()` prevents assertion crashes
- **Companion ownership**: Only restores companions from `Clan.PlayerClan` on retirement
- **Troop permanence**: Regular recruited troops become permanent military assets (stay with lord)
- **Save/load compatibility**: Leave state persists across game sessions
- **Lord death during leave**: Standard lord death handlers still apply, end service permanently

## Acceptance Criteria
- Player can request leave and regain full vanilla gameplay (movement, encounters, recruitment)
- Dialog option to return appears when talking to former lord while on leave
- Companions and new recruits automatically transfer to lord's party on return
- Service data (tier, XP, lord relationship) preserved during leave
- Companions restored to player on permanent retirement
- No crashes when entering settlements after leave/retirement

## Companion and Troop Management
### Enlistment Flow
1. **Initial Enlistment**: All existing companions and troops transfer to lord's party
2. **During Service**: Player travels alone (invisible, attached to lord)
3. **Temporary Leave**: Player can recruit freely with vanilla behavior
4. **Return from Leave**: New companions/troops transfer to lord's party
5. **Retirement**: Companions return to player, regular troops stay with lord permanently

### Party Transfer Implementation Pattern
```csharp
// Add to lord's party
lordParty.MemberRoster.AddToCounts(character, number, false, 0, 0, true, -1);
// Remove from player party  
playerParty.MemberRoster.AddToCounts(character, -1 * number, false, 0, 0, true, -1);
```

This ensures companions remain available for future adventures while recruited soldiers become permanent military assets.
