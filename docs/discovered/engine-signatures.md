# Engine API Signatures

Generated from "C:\Dev\Enlisted\DECOMPILE" on 2025-09-02 00:46:58 UTC

## ⚠️ **CRITICAL SAS DECOMPILE FINDINGS** - **FINAL BREAKTHROUGH**

**Updated**: After deep SAS decompile analysis, we discovered the **complete SAS approach** was different than initially understood:

- **❌ Previous Assumption**: SAS patched encounters for prevention/finishing
- **✅ SAS Reality**: SAS uses **engine properties + immediate menu system** for encounter control
- **✅ Final Solution**: `MobileParty.MainParty.IsActive = false` prevents encounters at engine level (no patches)
- **✅ Critical Timing**: SAS uses `TickEvent` (real-time) not `HourlyTickEvent` (game-time) for continuous enforcement
- **✅ Menu Gap Solution**: SAS shows `party_wait` menu IMMEDIATELY after enlistment (zero gap)

**Revolutionary Impact**: 
- **NO encounter patches needed** - engine properties handle everything
- **Immediate menu system required** - moved from Phase 4 to Phase 1A+
- **Real-time state management** - continuous enforcement even during paused encounters
- **100% API COMPATIBILITY** - All critical SAS APIs verified to exist in current Bannerlord version

## Menus / Encounter (ENHANCED WITH VERIFIED SAS APIS)

TaleWorlds.CampaignSystem.GameMenus.GameMenu :: ActivateGameMenu(string menuId)
TaleWorlds.CampaignSystem.GameMenus.GameMenu :: ExitToLast()
TaleWorlds.CampaignSystem.GameMenus.GameMenu :: SwitchToMenu(string menuId)
TaleWorlds.CampaignSystem.CampaignGameStarter :: AddWaitGameMenu(...) ✅ **SAS CRITICAL - VERIFIED EXISTS**
TaleWorlds.CampaignSystem.Encounters.PlayerEncounter :: DoMeeting()
TaleWorlds.CampaignSystem.Encounters.PlayerEncounter :: Finish(bool forcePlayerOutFromSettlement)
TaleWorlds.CampaignSystem.Encounters.PlayerEncounter :: LeaveEncounter { get; set; }
TaleWorlds.CampaignSystem.Encounters.PlayerEncounter :: Start()

## Dialog Surfaces

TaleWorlds.CampaignSystem.CampaignGameStarter :: AddDialogLine(ConversationSentence dialogLine)
TaleWorlds.CampaignSystem.CampaignGameStarter :: AddDialogLine(string id, string inputToken, string outputToken, string text, ConversationSentence.OnConditionDelegate conditionDelegate, ConversationSentence.OnConsequenceDelegate consequenceDelegate, int priority, ConversationSentence.OnClickableConditionDelegate clickableConditionDelegate)
TaleWorlds.CampaignSystem.CampaignGameStarter :: AddDialogLineMultiAgent(string id, string inputToken, string outputToken, TextObject text, ConversationSentence.OnConditionDelegate conditionDelegate, ConversationSentence.OnConsequenceDelegate consequenceDelegate, int agentIndex, int nextAgentIndex, int priority, ConversationSentence.OnClickableConditionDelegate clickableConditionDelegate)
TaleWorlds.CampaignSystem.CampaignGameStarter :: AddPlayerLine(string id, string inputToken, string outputToken, string text, ConversationSentence.OnConditionDelegate conditionDelegate, ConversationSentence.OnConsequenceDelegate consequenceDelegate, int priority, ConversationSentence.OnClickableConditionDelegate clickableConditionDelegate, ConversationSentence.OnPersuasionOptionDelegate persuasionOptionDelegate)
TaleWorlds.CampaignSystem.CampaignGameStarter :: AddRepeatablePlayerLine(string id, string inputToken, string outputToken, string text, string continueListingRepeatedObjectsText, string continueListingOptionOutputToken, ConversationSentence.OnConditionDelegate conditionDelegate, ConversationSentence.OnConsequenceDelegate consequenceDelegate, int priority, ConversationSentence.OnClickableConditionDelegate clickableConditionDelegate)

## Campaign Events (VERIFIED + SAS CRITICAL ADDITIONS)

TaleWorlds.CampaignSystem.CampaignEvents :: OnSessionLaunchedEvent
TaleWorlds.CampaignSystem.CampaignEvents :: BeforeGameMenuOpenedEvent
TaleWorlds.CampaignSystem.CampaignEvents :: GameMenuOpened
TaleWorlds.CampaignSystem.CampaignEvents :: OnSettlementEntered(MobileParty, Settlement, Hero)
TaleWorlds.CampaignSystem.CampaignEvents :: OnAfterSettlementEntered(MobileParty, Settlement, Hero)
TaleWorlds.CampaignSystem.CampaignEvents :: OnSettlementLeftEvent
TaleWorlds.CampaignSystem.CampaignEvents :: OnPartyAttachedAnotherParty(MobileParty)
TaleWorlds.CampaignSystem.CampaignEvents :: OnPartyDetachedAnotherParty(MobileParty)
TaleWorlds.CampaignSystem.CampaignEvents :: HourlyTickEvent
TaleWorlds.CampaignSystem.CampaignEvents :: TickEvent(float) ✅ **SAS CRITICAL - VERIFIED EXISTS**
TaleWorlds.CampaignSystem.CampaignEvents :: OnNearbyPartyAddedToPlayerMapEvent
TaleWorlds.CampaignSystem.CampaignEvents :: OnArmyCreated(Army)
TaleWorlds.CampaignSystem.CampaignEvents :: OnArmyDispersed(Army, Army.ArmyDispersionReason, bool)
TaleWorlds.CampaignSystem.CampaignEvents :: OnPartyJoinedArmyEvent(MobileParty)
TaleWorlds.CampaignSystem.CampaignEvents :: OnPartyRemovedFromArmyEvent(MobileParty)
TaleWorlds.CampaignSystem.CampaignEvents :: HeroKilledEvent(Hero, Hero, KillCharacterAction.KillCharacterActionDetail, bool)
TaleWorlds.CampaignSystem.CampaignEvents :: HeroPrisonerTaken(Hero, PartyBase)
TaleWorlds.CampaignSystem.CampaignEvents :: HeroPrisonerReleased(Hero, PartyBase, IFaction, EndCaptivityDetail)
TaleWorlds.CampaignSystem.CampaignEvents :: CharacterDefeated(Hero, Hero, bool)

## Army Management

TaleWorlds.CampaignSystem.Army :: AIBehavior { get; set; }
TaleWorlds.CampaignSystem.Army :: ArmyOwner { get; set; }
TaleWorlds.CampaignSystem.Army :: ArmyType { get; set; }
TaleWorlds.CampaignSystem.Army :: Cohesion { get; set; }
TaleWorlds.CampaignSystem.Army :: Kingdom { get; set; }
TaleWorlds.CampaignSystem.Army :: LeaderParty { get; }
TaleWorlds.CampaignSystem.Army :: Name { get; }
TaleWorlds.CampaignSystem.Army :: Parties { get; }
TaleWorlds.CampaignSystem.Army :: TotalStrength { get; }
TaleWorlds.CampaignSystem.Army :: Army(Kingdom kingdom, MobileParty leaderParty, ArmyTypes armyType) ✅ **SAS CRITICAL - VERIFIED EXISTS**
TaleWorlds.CampaignSystem.Army :: AddPartyToMergedParties(MobileParty party) ✅ **SAS CRITICAL - VERIFIED EXISTS**
TaleWorlds.CampaignSystem.Actions.DisbandArmyAction :: ApplyByPlayerTakenPrisoner(Army army)
TaleWorlds.CampaignSystem.Actions.DisbandArmyAction :: ApplyByCohesionDepleted(Army army) ✅ **SAS CRITICAL - VERIFIED EXISTS**
TaleWorlds.CampaignSystem.Actions.GatherArmyAction :: Apply(MobileParty leaderParty, Settlement gatheringSettlement)

## Party / AI (ENHANCED WITH VERIFIED SAS APIS)

TaleWorlds.CampaignSystem.Party.MobileParty :: AttachedTo { get; }
TaleWorlds.CampaignSystem.Party.MobileParty :: IgnoreByOtherPartiesTill(CampaignTime time)
TaleWorlds.CampaignSystem.Party.MobileParty :: IgnoreForHours(float hours)
TaleWorlds.CampaignSystem.Party.MobileParty :: IsVisible { get; set; }
TaleWorlds.CampaignSystem.Party.MobileParty :: IsActive { get; set; } ✅ **SAS CRITICAL - VERIFIED EXISTS**
TaleWorlds.CampaignSystem.Party.MobileParty :: Position2D { get; set; }
TaleWorlds.CampaignSystem.Party.MobilePartyAi :: SetMoveEscortParty(MobileParty mobileParty)
TaleWorlds.CampaignSystem.Party.MobilePartyAi :: SetMoveEngageParty(MobileParty party) ✅ **SAS CRITICAL - VERIFIED EXISTS**
TaleWorlds.CampaignSystem.Party.MobilePartyAi :: SetMoveGoToPoint(Vec2 point)
TaleWorlds.CampaignSystem.Party.MobilePartyAi :: SetMoveGoToSettlement(Settlement settlement)
TaleWorlds.CampaignSystem.Party.MobilePartyAi :: SetMovePatrolAroundSettlement(Settlement settlement)

## Party Officer Roles (VERIFIED FOR DUTIES SYSTEM)

TaleWorlds.CampaignSystem.Party.MobileParty :: EffectiveEngineer { get; }
TaleWorlds.CampaignSystem.Party.MobileParty :: EffectiveQuartermaster { get; }
TaleWorlds.CampaignSystem.Party.MobileParty :: EffectiveScout { get; }
TaleWorlds.CampaignSystem.Party.MobileParty :: EffectiveSurgeon { get; }
TaleWorlds.CampaignSystem.Party.MobileParty :: SetPartyEngineer(Hero hero)
TaleWorlds.CampaignSystem.Party.MobileParty :: SetPartyQuartermaster(Hero hero)
TaleWorlds.CampaignSystem.Party.MobileParty :: SetPartyScout(Hero hero)
TaleWorlds.CampaignSystem.Party.MobileParty :: SetPartySurgeon(Hero hero)

## Settlement Entry

TaleWorlds.CampaignSystem.Actions.EnterSettlementAction :: ApplyForParty(MobileParty party, Settlement settlement)
TaleWorlds.CampaignSystem.Actions.LeaveSettlementAction :: ApplyForParty(MobileParty party)
TaleWorlds.CampaignSystem.Actions.SetPartyAiAction :: GetActionForEngagingParty(MobileParty owner, MobileParty mobileParty)
TaleWorlds.CampaignSystem.Actions.SetPartyAiAction :: GetActionForGoingAroundParty(MobileParty owner, MobileParty mobileParty)
TaleWorlds.CampaignSystem.Actions.SetPartyAiAction :: GetActionForVisitingSettlement(MobileParty owner, Settlement settlement)
TaleWorlds.CampaignSystem.Party.MobileParty :: CurrentSettlement { get; }
TaleWorlds.CampaignSystem.Settlements.Settlement :: HeroesWithoutParty { get; }
TaleWorlds.CampaignSystem.Settlements.Settlement :: IsTown { get; }

## Map Events

TaleWorlds.CampaignSystem.MapEvents.MapEvent :: PlayerMapEvent { get; }

## Hero / Clan / Kingdom

TaleWorlds.CampaignSystem.Hero :: OneToOneConversationHero { get; }
TaleWorlds.CampaignSystem.Hero :: IsLord { get; }
TaleWorlds.CampaignSystem.Hero :: PartyBelongedTo { get; }

## Party Properties

TaleWorlds.CampaignSystem.Party.MobileParty :: ConversationParty { get; }
TaleWorlds.CampaignSystem.Party.MobileParty :: MainParty { get; }

## Structs Used In Signatures (Time/Math)

TaleWorlds.CampaignSystem.CampaignTime :: Now { get; }
TaleWorlds.CampaignSystem.CampaignTime :: Hours(float hours)
TaleWorlds.Library.Vec2 :: Distance(Vec2 other)

## Localization

TaleWorlds.Localization.TextObject :: TextObject(string value, Dictionary<string, object> attributes)
TaleWorlds.Localization.TextObject :: SetTextVariable(string tag, TextObject variable)
TaleWorlds.Localization.TextObject :: SetTextVariable(string tag, string variable)
TaleWorlds.Localization.TextObject :: SetTextVariable(string tag, float variable)
TaleWorlds.Localization.TextObject :: SetTextVariable(string tag, int variable)

## Localization System (VERIFIED AVAILABLE)

TaleWorlds.Localization.MBTextManager :: GetLocalizedText(string text)
TaleWorlds.Localization.LocalizedTextManager :: GetTranslatedText(string languageId, string textId)

## Custom Game Models (VERIFIED AVAILABLE) 

TaleWorlds.CampaignSystem.ComponentInterfaces.PartyHealingModel :: GetDailyHealingHpForHeroes(MobileParty party, bool includeDescriptions)
TaleWorlds.CampaignSystem.ComponentInterfaces.PartyHealingModel :: GetDailyHealingForRegulars(MobileParty party, bool includeDescriptions)
TaleWorlds.CampaignSystem.ComponentInterfaces.PartyHealingModel :: GetBattleEndHealingAmount(MobileParty party, Hero hero)
TaleWorlds.CampaignSystem.GameComponents.DefaultPartyHealingModel :: GetDailyHealingHpForHeroes(MobileParty party, bool includeDescriptions)
TaleWorlds.Localization.TextObject :: ToString()

## API Verification Results (From Decompile Analysis)

### ✅ **VERIFIED AVAILABLE - Restored to Implementation**

#### **1. Localization Key Format** (CONFIRMED in MBTextManager.cs)
```csharp
// VERIFIED: Lines 241-298 in TaleWorlds.Localization\MBTextManager.cs
if (text != null && text.Length > 2 && text[0] == '{' && text[1] == '=')
{
    // Processes {=key}fallback format
    string translatedText = LocalizedTextManager.GetTranslatedText(languageId, keyString);
}

// USAGE:
new TextObject("{=enlisted_status_title}Enlisted Status")
new TextObject("{=field_medic_training}Field Medic Training")
```

#### **2. Custom PartyHealingModel** (CONFIRMED in ComponentInterfaces)
```csharp
// VERIFIED: TaleWorlds.CampaignSystem\ComponentInterfaces\PartyHealingModel.cs
public abstract class PartyHealingModel : GameModel
{
    public abstract ExplainedNumber GetDailyHealingHpForHeroes(MobileParty party, bool includeDescriptions = false);
    public abstract ExplainedNumber GetDailyHealingForRegulars(MobileParty party, bool includeDescriptions = false);
}

// IMPLEMENTATION:
public class EnlistedPartyHealingModel : PartyHealingModel
{
    public override ExplainedNumber GetDailyHealingHpForHeroes(MobileParty party, bool includeDescriptions = false)
    {
        if (EnlistmentBehavior.Instance?.IsEnlisted == true && party == MobileParty.MainParty)
        {
            var result = new ExplainedNumber(24f, includeDescriptions, 
                new TextObject("{=enlisted_base_healing}Enlisted Service Base Healing"));
            
            // Field Medic bonus
            if (DutiesBehavior.Instance?.HasActiveDutyWithRole("Surgeon") == true)
            {
                var medicineSkill = Hero.MainHero.GetSkillValue(DefaultSkills.Medicine);
                result.Add(medicineSkill / 10f, new TextObject("{=field_medic_bonus}Field Medic Training"));
            }
            
            return result;
        }
        
        return new ExplainedNumber(11f, includeDescriptions, null);
    }
}
```

### ❌ **API NOT CONFIRMED**
#### **3. ModuleHelper.GetModuleFullPath** (NOT FOUND in decompiled code)
**Searched**: TaleWorlds.Engine.Utilities, SandBox.ModuleManager, all assemblies  
**Result**: Method not found - kept Blueprint-compliant relative path approach

## Library

TaleWorlds.Library.Vec2 :: Vec2(float x, float y)
TaleWorlds.Library.Vec2 :: X { get; }
TaleWorlds.Library.Vec2 :: Y { get; }
TaleWorlds.Library.Vec2 :: Distance(Vec2 v)
TaleWorlds.Library.Vec2 :: Length { get; }
TaleWorlds.Library.Vec2 :: Normalized()
TaleWorlds.Library.MathF :: Clamp(float value, float minValue, float maxValue)
TaleWorlds.Library.MathF :: Lerp(float valueFrom, float valueTo, float amount, float minimumDifference)
TaleWorlds.Library.MathF :: Max(float a, float b)
TaleWorlds.Library.MathF :: Min(float a, float b)
TaleWorlds.Library.MBRandom :: RandomInt(int min, int max)
TaleWorlds.Library.MBRandom :: RandomInt(int max)

## Actions (Critical for SAS)

TaleWorlds.CampaignSystem.Actions.AddCompanionAction :: Apply(Clan clan, Hero companion)
TaleWorlds.CampaignSystem.Actions.AddHeroToPartyAction :: Apply(Hero hero, MobileParty party, bool showNotification)
TaleWorlds.CampaignSystem.Actions.ChangeRelationAction :: ApplyPlayerRelation(Hero hero, int relationChange, bool showNotification, bool shouldCheckForMarriageOffer)
TaleWorlds.CampaignSystem.Actions.ChangeRelationAction :: ApplyRelationChangeBetweenHeroes(Hero hero1, Hero hero2, int relationChange, bool showNotification)
TaleWorlds.CampaignSystem.Actions.DeclareWarAction :: ApplyByDefault(IFaction faction1, IFaction faction2)
TaleWorlds.CampaignSystem.Actions.GiveGoldAction :: ApplyBetweenCharacters(Hero giverHero, Hero recipientHero, int amount, bool disableNotification)
TaleWorlds.CampaignSystem.Actions.LeaveSettlementAction :: ApplyForParty(MobileParty party)
TaleWorlds.CampaignSystem.Actions.MakePeaceAction :: Apply(IFaction faction1, IFaction faction2, int dailyTributeFrom1To2)
TaleWorlds.CampaignSystem.Actions.RemoveCompanionAction :: ApplyByFire(Clan clan, Hero companion)

## Character Development

TaleWorlds.CampaignSystem.CharacterDevelopment.Hero :: AddSkillXp(SkillObject skill, float xp)
TaleWorlds.CampaignSystem.CharacterDevelopment.Hero :: ChangeHeroGold(int goldChange)
TaleWorlds.CampaignSystem.CharacterDevelopment.Hero :: GetPerkValue(PerkObject perk)
TaleWorlds.CampaignSystem.CharacterDevelopment.Hero :: GetRelation(Hero otherHero)
TaleWorlds.CampaignSystem.CharacterDevelopment.Hero :: GetSkillValue(SkillObject skill)
TaleWorlds.CampaignSystem.CharacterDevelopment.Hero :: Heal(int healAmount, bool addXp)
TaleWorlds.CampaignSystem.CharacterDevelopment.Hero :: Level { get; }
TaleWorlds.CampaignSystem.CharacterDevelopment.HeroDeveloper :: AddFocus(SkillObject skill, int amount, bool checkUnspentFocusPoints)
TaleWorlds.CampaignSystem.CharacterDevelopment.HeroDeveloper :: AddAttribute(CharacterAttribute attribute, int amount, bool checkUnspentAttributePoints)
TaleWorlds.CampaignSystem.CharacterDevelopment.HeroDeveloper :: AddSkillXp(SkillObject skill, float rawXp, bool isAffectedByFocusFactor = true, bool shouldNotify = true)
TaleWorlds.CampaignSystem.Party.MobileParty :: TotalWage { get; }

## Equipment Management

TaleWorlds.Core.Equipment :: GetEquipmentFromSlot(EquipmentIndex index)
TaleWorlds.Core.Equipment :: AddEquipmentToSlotWithoutAgent(EquipmentIndex index, EquipmentElement equipmentElement)
TaleWorlds.CampaignSystem.Roster.ItemRoster :: AddToCounts(ItemObject item, int count)
TaleWorlds.CampaignSystem.Roster.ItemRoster :: Clear()
TaleWorlds.CampaignSystem.Roster.TroopRoster :: AddXpToTroop(int xp, CharacterObject character)

## Notifications & UI

TaleWorlds.Library.InformationManager :: AddQuickInformation(TextObject message, int priority, CharacterObject character, string soundEventPath)
TaleWorlds.Library.InformationManager :: DisplayMessage(InformationMessage message)
TaleWorlds.Library.InformationMessage :: InformationMessage(string message)

## Sound System

TaleWorlds.Engine.SoundEvent :: GetEventIdFromString(string eventPath)
TaleWorlds.Engine.SoundEvent :: PlaySound2D(int soundEventId)

## Text Helpers

TaleWorlds.Core.StringHelpers :: SetCharacterProperties(string tag, CharacterObject character, TextObject textObject, bool includeDetails)

## Campaign Time & Control

TaleWorlds.CampaignSystem.Campaign :: TimeControlMode { get; set; }
TaleWorlds.CampaignSystem.CampaignTime :: Now { get; }
TaleWorlds.CampaignSystem.CampaignTime :: Hours(float hours)
TaleWorlds.CampaignSystem.CampaignTime :: Days(float days)
TaleWorlds.CampaignSystem.CampaignTimeControlMode :: Stop
TaleWorlds.CampaignSystem.CampaignTimeControlMode :: StoppablePlay

## Visual Tracking

TaleWorlds.CampaignSystem.VisualTrackerManager :: RegisterObject(object trackedObject)
TaleWorlds.CampaignSystem.VisualTrackerManager :: RemoveTrackedObject(object trackedObject)

## Equipment & Items (Core)

TaleWorlds.Core.Equipment :: Equipment()
TaleWorlds.Core.Equipment :: Equipment(bool isCivilian)
TaleWorlds.Core.Equipment :: Equipment(Equipment equipment)
TaleWorlds.Core.Equipment :: Clone(bool cloneWithoutWeapons)
TaleWorlds.Core.Equipment :: FillFrom(Equipment sourceEquipment, bool useSourceEquipmentType)
TaleWorlds.Core.Equipment :: GetEquipmentFromSlot(EquipmentIndex index)
TaleWorlds.Core.Equipment :: Horse { get; }
TaleWorlds.Core.Equipment :: IsCivilian { get; }
TaleWorlds.Core.Equipment :: IsValid { get; }
TaleWorlds.Core.Equipment :: this[EquipmentIndex index] { get; set; }
TaleWorlds.Core.Equipment :: this[int index] { get; set; }
TaleWorlds.Core.EquipmentElement :: EquipmentElement(ItemObject item, ItemModifier itemModifier, Banner banner, bool isQuestItem)
TaleWorlds.Core.EquipmentElement :: Item { get; }
TaleWorlds.Core.EquipmentElement :: ItemModifier { get; }
TaleWorlds.Core.EquipmentElement :: IsQuestItem { get; }
TaleWorlds.Core.EquipmentIndex :: Weapon0
TaleWorlds.Core.EquipmentIndex :: Weapon1
TaleWorlds.Core.EquipmentIndex :: Weapon2
TaleWorlds.Core.EquipmentIndex :: Weapon3
TaleWorlds.Core.EquipmentIndex :: Head
TaleWorlds.Core.EquipmentIndex :: Body
TaleWorlds.Core.EquipmentIndex :: Leg
TaleWorlds.Core.EquipmentIndex :: Gloves
TaleWorlds.Core.EquipmentIndex :: Cape
TaleWorlds.Core.EquipmentIndex :: Horse
TaleWorlds.Core.EquipmentIndex :: HorseHarness
TaleWorlds.Core.ItemObject :: ItemComponent { get; }
TaleWorlds.Core.ItemObject :: Name { get; }
TaleWorlds.Core.ItemObject :: StringId { get; }
TaleWorlds.Core.ItemObject :: Value { get; }

## Gear Assignment & Rosters (CampaignSystem)

Helpers.EquipmentHelper :: AssignHeroEquipmentFromEquipment(Hero hero, Equipment equipment)
TaleWorlds.CampaignSystem.CharacterObject :: BattleEquipments { get; }
TaleWorlds.CampaignSystem.CharacterObject :: Tier { get; }
TaleWorlds.CampaignSystem.CharacterObject :: UpgradeTargets { get; }
TaleWorlds.CampaignSystem.Hero :: BattleEquipment { get; set; }
TaleWorlds.CampaignSystem.Hero :: CharacterObject { get; }
TaleWorlds.CampaignSystem.Hero :: CivilianEquipment { get; set; }
TaleWorlds.CampaignSystem.Hero :: Culture { get; }
TaleWorlds.CampaignSystem.Models.EquipmentSelectionModel :: GetEquipmentRostersForHeroComeOfAge(Hero hero, bool isCivilian)
TaleWorlds.CampaignSystem.Party.PartyBase :: ItemRoster { get; }
TaleWorlds.CampaignSystem.Roster.ItemRoster :: AddToCounts(ItemObject item, int count)
TaleWorlds.CampaignSystem.Roster.ItemRoster :: Clear()
TaleWorlds.CampaignSystem.Roster.ItemRoster :: Remove(ItemRosterElement item)
TaleWorlds.Core.MBEquipmentRoster :: DefaultEquipment { get; }

## Custom UI (Gauntlet)

TaleWorlds.Engine.GauntletUI.GauntletLayer :: GauntletLayer(int localOrder, string categoryId, bool shouldClear)
TaleWorlds.Engine.GauntletUI.GauntletLayer :: LoadMovie(string movieName, object dataSource)
TaleWorlds.Engine.GauntletUI.GauntletLayer :: ReleaseMovie(GauntletMovie movie)
TaleWorlds.ScreenSystem.ScreenManager :: TopScreen { get; }
TaleWorlds.ScreenSystem.ScreenBase :: AddLayer(ScreenLayer layer)
TaleWorlds.ScreenSystem.ScreenBase :: RemoveLayer(ScreenLayer layer)

## Inventory UI (SandBox & View)

TaleWorlds.CampaignSystem.Inventory.InventoryManager :: ActivateTradeWithCurrentSettlement()
TaleWorlds.CampaignSystem.Inventory.InventoryManager :: CloseInventoryPresentation(bool fromCancel)
TaleWorlds.CampaignSystem.Inventory.InventoryManager :: CurrentMode { get; }
TaleWorlds.CampaignSystem.Inventory.InventoryManager :: Instance { get; }
TaleWorlds.CampaignSystem.Inventory.InventoryManager :: InventoryLogic { get; }
TaleWorlds.CampaignSystem.Inventory.InventoryManager :: OpenCampaignBattleLootScreen()
TaleWorlds.CampaignSystem.Inventory.InventoryManager :: OpenScreenAsInventory(InventoryManager.DoneLogicExtrasDelegate doneLogicExtrasDelegate)
TaleWorlds.CampaignSystem.Inventory.InventoryManager :: OpenScreenAsInventoryForCraftedItemDecomposition(MobileParty party, CharacterObject character, InventoryManager.DoneLogicExtrasDelegate doneLogicExtrasDelegate)
TaleWorlds.CampaignSystem.Inventory.InventoryManager :: OpenScreenAsInventoryOf(MobileParty party, CharacterObject character)
TaleWorlds.CampaignSystem.Inventory.InventoryManager :: OpenScreenAsInventoryOf(PartyBase rightParty, PartyBase leftParty)
TaleWorlds.CampaignSystem.Inventory.InventoryManager :: OpenScreenAsInventoryOfSubParty(MobileParty rightParty, MobileParty leftParty, InventoryManager.DoneLogicExtrasDelegate doneLogicExtrasDelegate)
TaleWorlds.CampaignSystem.Inventory.InventoryManager :: OpenScreenAsLoot(Dictionary<PartyBase, ItemRoster> itemRostersToLoot)
TaleWorlds.CampaignSystem.Inventory.InventoryManager :: OpenScreenAsReceiveItems(ItemRoster items, TextObject leftRosterName, InventoryManager.DoneLogicExtrasDelegate doneLogicDelegate)
TaleWorlds.CampaignSystem.Inventory.InventoryManager :: OpenScreenAsStash(ItemRoster stash)
TaleWorlds.CampaignSystem.Inventory.InventoryManager :: OpenScreenAsTrade(ItemRoster leftRoster, SettlementComponent settlementComponent, InventoryManager.InventoryCategoryType merchantItemType, InventoryManager.DoneLogicExtrasDelegate doneLogicExtrasDelegate)
TaleWorlds.CampaignSystem.Inventory.InventoryManager :: OpenTradeWithCaravanOrAlleyParty(MobileParty caravan, InventoryManager.InventoryCategoryType merchantItemType)
TaleWorlds.CampaignSystem.Inventory.InventoryManager :: PlayerAcceptTradeOffer()

## Inventory ViewModels (SandBox.ViewModelCollection)

// No specific InventoryVM found - inventory uses InventoryLogic + InventoryState pattern
// Alternative: Use InventoryManager.OpenScreen methods or custom Gauntlet UI

## Conversation Behaviors (SandBox)

SandBox.CampaignBehaviors.StatisticsCampaignBehavior :: OnPartyAttachedAnotherParty(MobileParty mobileParty)

## Settlement / Town ViewModels (optional)

SandBox.ViewModelCollection.Nameplate.SettlementNameplateVM :: Track()
SandBox.ViewModelCollection.Nameplate.SettlementNameplateVM :: Untrack()

## Culture / Items / Equipment (Core)

TaleWorlds.Core.ArmorComponent :: ArmorComponent()
TaleWorlds.Core.BasicCultureObject :: Culture { get; }
TaleWorlds.Core.BasicCultureObject :: StringId { get; }
TaleWorlds.Core.ItemCategory :: StringId { get; }
TaleWorlds.Core.ItemModifier :: StringId { get; }
TaleWorlds.Core.ItemObject :: Culture { get; }
TaleWorlds.Core.ItemObject :: ItemType { get; }
TaleWorlds.Core.MBEquipmentRoster :: DefaultEquipment { get; }
TaleWorlds.Core.MBEquipmentRoster :: EquipmentCulture { get; }
TaleWorlds.Core.WeaponComponentData :: WeaponClass { get; }
TaleWorlds.Core.WeaponComponentData :: AmmoClass { get; }

## Hero / Character / Roster (CampaignSystem)

TaleWorlds.CampaignSystem.CharacterObject :: BattleEquipments { get; }
TaleWorlds.CampaignSystem.CharacterObject :: Culture { get; }
TaleWorlds.CampaignSystem.CharacterObject :: Tier { get; }
TaleWorlds.CampaignSystem.CharacterObject :: UpgradeTargets { get; }
TaleWorlds.CampaignSystem.Hero :: BattleEquipment { get; set; }
TaleWorlds.CampaignSystem.Hero :: CharacterObject { get; }
TaleWorlds.CampaignSystem.Hero :: CivilianEquipment { get; set; }
TaleWorlds.CampaignSystem.Hero :: Culture { get; }
TaleWorlds.CampaignSystem.Hero :: IsAlive { get; }
TaleWorlds.CampaignSystem.Hero :: IsChild { get; }
TaleWorlds.CampaignSystem.Hero :: IsFemale { get; }
TaleWorlds.CampaignSystem.Hero :: IsPrisoner { get; }

## Equipment Selection Models (GameComponents)

TaleWorlds.CampaignSystem.ComponentInterfaces.EquipmentSelectionModel :: GetEquipmentRostersForHeroComeOfAge(Hero hero, bool isCivilian)
TaleWorlds.CampaignSystem.ComponentInterfaces.EquipmentSelectionModel :: GetEquipmentRostersForCompanion(Hero companionHero, bool isCivilian)
TaleWorlds.CampaignSystem.ComponentInterfaces.EquipmentSelectionModel :: GetEquipmentRostersForDeliveredOffspring(Hero hero)
TaleWorlds.CampaignSystem.ComponentInterfaces.EquipmentSelectionModel :: GetEquipmentRostersForHeroReachesTeenAge(Hero hero)
TaleWorlds.CampaignSystem.ComponentInterfaces.EquipmentSelectionModel :: GetEquipmentRostersForInitialChildrenGeneration(Hero hero)
TaleWorlds.CampaignSystem.GameComponents.DefaultEquipmentSelectionModel :: GetEquipmentRostersForHeroComeOfAge(Hero hero, bool isCivilian)

## Object Lookup (ObjectSystem)

TaleWorlds.ObjectSystem.MBObjectManager :: Instance { get; }
TaleWorlds.ObjectSystem.MBObjectManager :: GetObject<T>(string stringId)
TaleWorlds.ObjectSystem.MBObjectManager :: GetObjectTypeList<T>()

## XML Data Access Patterns

### Culture Lookup
```csharp
// Access cultures by ID (from mpcultures.xml)
var empire = MBObjectManager.Instance.GetObject<CultureObject>("empire");
var aserai = MBObjectManager.Instance.GetObject<CultureObject>("aserai");
var vlandia = MBObjectManager.Instance.GetObject<CultureObject>("vlandia");
var sturgia = MBObjectManager.Instance.GetObject<CultureObject>("sturgia");
var khuzait = MBObjectManager.Instance.GetObject<CultureObject>("khuzait");
var battania = MBObjectManager.Instance.GetObject<CultureObject>("battania");
```

### Item Lookup
```csharp
// Access items by string ID (from mpitems.xml)
var empireSword = MBObjectManager.Instance.GetObject<ItemObject>("empire_sword_4_t4");
var vlandiaSword = MBObjectManager.Instance.GetObject<ItemObject>("vlandia_sword_3_t4");
var monkRobe = MBObjectManager.Instance.GetObject<ItemObject>("monk_robe");
var leatherBoots = MBObjectManager.Instance.GetObject<ItemObject>("leather_boots");
```

### Equipment Roster Access
```csharp
// Access equipment rosters (from sandbox_equipment_sets.xml)
var rosters = Campaign.Current.Models.EquipmentSelectionModel
    .GetEquipmentRostersForHeroComeOfAge(hero, false);
foreach (var roster in rosters)
{
    if (roster.EquipmentCulture == hero.Culture)
    {
        var equipment = roster.DefaultEquipment;
        // Use culture-appropriate equipment set
    }
}
```

## Gauntlet / Screen System (for custom selector)

TaleWorlds.Engine.GauntletUI.GauntletLayer :: GauntletLayer(int localOrder, string categoryId, bool shouldClear)
TaleWorlds.Engine.GauntletUI.GauntletLayer :: LoadMovie(string movieName, object dataSource)
TaleWorlds.Engine.GauntletUI.GauntletLayer :: ReleaseMovie(GauntletMovie movie)
TaleWorlds.ScreenSystem.ScreenBase :: AddLayer(ScreenLayer layer)
TaleWorlds.ScreenSystem.ScreenBase :: IsActive { get; }
TaleWorlds.ScreenSystem.ScreenBase :: Layers { get; }
TaleWorlds.ScreenSystem.ScreenBase :: RemoveLayer(ScreenLayer layer)
TaleWorlds.ScreenSystem.ScreenManager :: TopScreen { get; }

## UI Utilities (optional)

TaleWorlds.Library.InformationManager :: AddQuickInformation(TextObject message, int priority, CharacterObject character, string soundEventPath)
TaleWorlds.Library.InformationManager :: DisplayMessage(InformationMessage message)

## Configuration & JSON Support (VERIFIED AVAILABLE)

// VERIFIED: Newtonsoft.Json ships with Bannerlord runtime
// Location: C:\...\Mount & Blade II Bannerlord\bin\Win64_Shipping_Client\Newtonsoft.Json.dll  
using Newtonsoft.Json; 
JsonConvert.DeserializeObject<T>(string json)
JsonConvert.SerializeObject(object obj)

## Character Type Detection (for Troop Types)

TaleWorlds.CampaignSystem.CharacterObject :: IsMounted { get; }
TaleWorlds.CampaignSystem.CharacterObject :: IsRanged { get; }
TaleWorlds.CampaignSystem.CharacterObject :: IsInfantry { get; }
TaleWorlds.CampaignSystem.CharacterObject :: Culture { get; }
TaleWorlds.CampaignSystem.CharacterObject :: Tier { get; }

## Equipment Backup & Restoration (VERIFIED FOR CRITICAL MISSING FEATURES)

// VERIFIED: Equipment cloning for backup system
TaleWorlds.Core.Equipment :: Clone(bool cloneWithoutWeapons)
TaleWorlds.Core.Equipment :: Equipment(Equipment equipment) // Clone constructor

// VERIFIED: ItemRoster management for inventory backup
TaleWorlds.CampaignSystem.Roster.ItemRoster :: ItemRoster()
TaleWorlds.CampaignSystem.Roster.ItemRoster :: AddToCounts(ItemObject item, int number)
TaleWorlds.CampaignSystem.Roster.ItemRoster :: AddToCounts(EquipmentElement element, int number) 
TaleWorlds.CampaignSystem.Roster.ItemRoster :: Clear()
TaleWorlds.CampaignSystem.Roster.ItemRoster :: Remove(ItemRosterElement element)

// VERIFIED: Quest item protection (prevents quest item loss)
TaleWorlds.Core.EquipmentElement :: IsQuestItem { get; }
TaleWorlds.Core.ItemObject :: ItemFlags { get; }
TaleWorlds.Core.ItemFlags :: NotUsableByPlayer
TaleWorlds.Core.ItemFlags :: NonTransferable

// VERIFIED: Equipment visual refresh (UI updates after equipment changes)
TaleWorlds.CampaignSystem.CharacterDevelopment.HeroDeveloper :: UpdateHeroEquipment()
TaleWorlds.CampaignSystem.CampaignEventDispatcher :: OnHeroEquipmentChanged(Hero hero)

## Kingdom Integration (VERIFIED FOR VASSALAGE SYSTEM)

// VERIFIED: Kingdom joining for vassalage offers
TaleWorlds.CampaignSystem.Actions.ChangeKingdomAction :: ApplyByJoinToKingdom(Clan clan, Kingdom newKingdom, bool showNotification = true)
TaleWorlds.CampaignSystem.Actions.ChangeKingdomAction :: ApplyByJoinToKingdomByDefection(Clan clan, Kingdom newKingdom, bool showNotification = true)

// VERIFIED: Settlement ownership for land grants
TaleWorlds.CampaignSystem.Actions.ChangeOwnerOfSettlementAction :: ApplyByGift(Settlement settlement, Hero newOwner)
TaleWorlds.CampaignSystem.Actions.ChangeOwnerOfSettlementAction :: ApplyByDefault(Hero hero, Settlement settlement)

## Save System (VERIFIED - NO CUSTOM SAVEDEFINER NEEDED)

// VERIFIED: Our dictionary types already supported by core save system
// From SaveableCampaignTypeDefiner.cs lines 515-525:
Dictionary<Hero, int>           // ✅ Already defined in core system
Dictionary<IFaction, int>       // ✅ Already defined in core system  
List<Hero>                      // ✅ Already defined in core system
List<IFaction>                  // ✅ Already defined in core system

// VERIFIED: Equipment types are core serializable types
// From Hero.cs lines 573, 578:
Equipment                       // ✅ Core type with [SaveableProperty] support
ItemRoster                      // ✅ Core serializable type

// Standard SyncData pattern (no try-catch needed)
TaleWorlds.CampaignSystem.CampaignBehaviorBase :: SyncData(IDataStore dataStore)
TaleWorlds.CampaignSystem.IDataStore :: SyncData<T>(string key, ref T data)
TaleWorlds.CampaignSystem.IDataStore :: IsLoading { get; }

// Save versioning support
dataStore.SyncData("_saveVersion", ref _saveVersion) // Standard practice for version tracking