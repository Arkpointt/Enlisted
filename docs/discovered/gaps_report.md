# Gaps Report

Generated from "C:\Dev\Enlisted\DECOMPILE" on 2025-09-02 00:46:58 UTC

Expected but missing APIs by category:

## Dialog Surfaces (Missing)

- ConversationManager.AddDialogFlow(DialogFlow, object) — **Status**: Not found in public API, likely internal
- ConversationManager.OpenMapConversation(...) — **Status**: Not found in public API, likely internal
- **Suggested**: Use reflection with BindingFlags.Instance|NonPublic

## Party AI (Missing)

- MobilePartyAi.ClearMoveToParty() — **Status**: Not found in public API, likely internal/private
- MobilePartyAi.SetMoveModeHold() — **Status**: Not found in public API, likely internal/private  
- **Suggested**: Use reflection or find alternative like SetMoveGoToPoint with current position

## Party Attachment (Missing)

- MobileParty.AttachTo(MobileParty) — **Status**: Not found in public API, likely internal
- MobileParty.Detach() — **Status**: Not found in public API, likely internal
- **Suggested**: Use reflection or rely on SetMoveEscortParty for similar behavior

## Battle Participation (Missing)

- MobileParty.ShouldJoinPlayerBattles { get; set; } — **Status**: Not found in public API, likely internal
- **Suggested**: Use reflection with BindingFlags.Instance|NonPublic

## Alternative Types/Files to Search

### For Missing AI Methods
- Search: TaleWorlds.CampaignSystem.Party.PartyAiHelper (if exists)
- Search: TaleWorlds.CampaignSystem.AI.* classes
- Search: TaleWorlds.CampaignSystem.Party.Components.* classes

### For Missing Dialog Methods  
- Search: TaleWorlds.CampaignSystem.Conversation.DialogHelper (if exists)
- Search: TaleWorlds.CampaignSystem.Conversation.Tags.* classes
- Search: TaleWorlds.CampaignSystem.GameComponents.*Conversation* classes

### For Missing Attachment Methods
- Search: TaleWorlds.CampaignSystem.Party.PartyGroupManager (if exists)
- Search: TaleWorlds.CampaignSystem.Actions.*Attach* classes
- Search: TaleWorlds.CampaignSystem.ComponentInterfaces.*Party* interfaces

## Confirmed Available Alternatives

### Instead of ClearMoveToParty()
- Use: MobilePartyAi.SetMoveGoToPoint(currentPosition) to stop movement
- Use: Reflection to find SetMoveModeHold() or similar

### Instead of AttachTo/Detach()
- Use: MobilePartyAi.SetMoveEscortParty() for following behavior
- Monitor: AttachedTo property for current attachment state

### Instead of AddDialogFlow()
- Use: Multiple AddPlayerLine/AddDialogLine calls to build flows
- Use: Existing CampaignGameStarter methods for standard dialog patterns
