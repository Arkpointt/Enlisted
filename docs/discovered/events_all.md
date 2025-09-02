# Campaign Events with Exact Signatures

Generated from "C:\Dev\Enlisted\DECOMPILE" on 2025-09-02 00:46:58 UTC

## Session Events

TaleWorlds.CampaignSystem.CampaignEvents :: OnSessionLaunchedEvent<CampaignGameStarter>
TaleWorlds.CampaignSystem.CampaignEvents :: OnAfterSessionLaunchedEvent<CampaignGameStarter>
TaleWorlds.CampaignSystem.CampaignEvents :: OnNewGameCreatedEvent<CampaignGameStarter>
TaleWorlds.CampaignSystem.CampaignEvents :: OnGameLoadedEvent<CampaignGameStarter>

## Menu Events

TaleWorlds.CampaignSystem.CampaignEvents :: BeforeGameMenuOpenedEvent<GameMenuOption>
TaleWorlds.CampaignSystem.CampaignEvents :: GameMenuOpened<MenuCallbackArgs>
TaleWorlds.CampaignSystem.CampaignEvents :: AfterGameMenuOpenedEvent<MenuCallbackArgs>

## Settlement Events

TaleWorlds.CampaignSystem.CampaignEvents :: OnSettlementEntered<MobileParty, Settlement, Hero>
TaleWorlds.CampaignSystem.CampaignEvents :: OnAfterSettlementEntered<MobileParty, Settlement, Hero>
TaleWorlds.CampaignSystem.CampaignEvents :: OnSettlementLeftEvent<MobileParty, Settlement, Hero>

## Party Events

TaleWorlds.CampaignSystem.CampaignEvents :: OnPartyAttachedAnotherParty<MobileParty>
TaleWorlds.CampaignSystem.CampaignEvents :: OnPartyDetachedAnotherParty<MobileParty>
TaleWorlds.CampaignSystem.CampaignEvents :: OnNearbyPartyAddedToPlayerMapEvent<MobileParty>

## Army Events

TaleWorlds.CampaignSystem.CampaignEvents :: OnArmyCreated<Army>
TaleWorlds.CampaignSystem.CampaignEvents :: OnArmyDispersed<Army, Army.ArmyDispersionReason, bool>
TaleWorlds.CampaignSystem.CampaignEvents :: OnPartyJoinedArmyEvent<MobileParty>
TaleWorlds.CampaignSystem.CampaignEvents :: OnPartyRemovedFromArmyEvent<MobileParty>

## Tick Events

TaleWorlds.CampaignSystem.CampaignEvents :: HourlyTickEvent
TaleWorlds.CampaignSystem.CampaignEvents :: DailyTickEvent
TaleWorlds.CampaignSystem.CampaignEvents :: WeeklyTickEvent
TaleWorlds.CampaignSystem.CampaignEvents :: AiHourlyTickEvent<MobileParty, PartyThinkParams>
