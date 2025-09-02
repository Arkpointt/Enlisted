# APIs We Must Not Use

Generated from "C:\Dev\Enlisted\DECOMPILE" on 2025-09-02 00:46:58 UTC

APIs that should be avoided for SAS enlistment to prevent conflicts:

## Army Management APIs (Avoid - Conflicts with Enlistment)

TaleWorlds.CampaignSystem.Army :: CreateArmy(Hero, Clan, Clan) — reason: Creates persistent army structures that conflict with party-only enlistment
TaleWorlds.CampaignSystem.Army :: DisbandArmy() — reason: Should not manage armies directly
TaleWorlds.CampaignSystem.Actions.GatherArmyAction :: Apply(...) — reason: Army formation conflicts with escort behavior
TaleWorlds.CampaignSystem.Actions.DisbandArmyAction :: Apply(...) — reason: Army management is outside our scope

## Party Leadership Changes (Avoid - Breaks Enlistment)

TaleWorlds.CampaignSystem.Party.MobileParty :: ChangePartyLeader(Hero) — reason: Would break enlistment relationship
TaleWorlds.CampaignSystem.Actions.ChangePlayerCharacterAction :: Apply(...) — reason: Character switching incompatible with enlistment

## Settlement Ownership (Avoid - Too High Level)

TaleWorlds.CampaignSystem.Actions.ChangeOwnerOfSettlementAction :: ApplyByDefault(...) — reason: Settlement management outside mod scope
TaleWorlds.CampaignSystem.Actions.ChangeRulingClanAction :: Apply(...) — reason: Political changes outside mod scope

## Faction/Kingdom Changes (Avoid - Too Broad)

TaleWorlds.CampaignSystem.Actions.ChangeKingdomAction :: ApplyByJoinToKingdom(...) — reason: Kingdom membership changes too broad for enlistment
TaleWorlds.CampaignSystem.Actions.DeclareWarAction :: Apply(...) — reason: Diplomatic actions outside mod scope
