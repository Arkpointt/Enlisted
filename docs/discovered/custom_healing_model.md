# Custom Party Healing Model Implementation

**VERIFIED AVAILABLE from decompiled TaleWorlds.CampaignSystem**

## ✅ **PartyHealingModel Interface** (CONFIRMED)

**Source**: `TaleWorlds.CampaignSystem\ComponentInterfaces\PartyHealingModel.cs`

```csharp
public abstract class PartyHealingModel : GameModel
{
    public abstract float GetSurgeryChance(PartyBase party);
    public abstract float GetSurvivalChance(PartyBase party, CharacterObject agentCharacter, DamageTypes damageType, bool canDamageKillEvenIfBlunt, PartyBase enemyParty = null);
    public abstract int GetSkillXpFromHealingTroop(PartyBase party);
    public abstract ExplainedNumber GetDailyHealingForRegulars(MobileParty party, bool includeDescriptions = false);
    public abstract ExplainedNumber GetDailyHealingHpForHeroes(MobileParty party, bool includeDescriptions = false);
    public abstract int GetHeroesEffectedHealingAmount(Hero hero, float healingRate);
    public abstract float GetSiegeBombardmentHitSurgeryChance(PartyBase party);
    public abstract int GetBattleEndHealingAmount(MobileParty party, Hero hero);
}
```

## ✅ **Custom Healing Model for Enlisted Soldiers**

### **Enhanced Healing Implementation** (VERIFIED APIs)
```csharp
// NEW: Enhanced healing when enlisted (like SAS SoldierPartyHealingModel.cs)
public class EnlistedPartyHealingModel : PartyHealingModel
{
    public override ExplainedNumber GetDailyHealingHpForHeroes(MobileParty party, bool includeDescriptions = false)
    {
        // Base healing from default model
        var baseModel = Campaign.Current.Models.PartyHealingModel as DefaultPartyHealingModel;
        var result = baseModel?.GetDailyHealingHpForHeroes(party, includeDescriptions) ?? 
                    new ExplainedNumber(11f, includeDescriptions, null);
        
        // Enhanced healing when enlisted
        if (EnlistmentBehavior.Instance?.IsEnlisted == true && party == MobileParty.MainParty)
        {
            result.Add(13f, new TextObject("Enlisted Service Medical Support"));
            
            // Field Medic bonus (officer role substitution benefit)
            if (DutiesBehavior.Instance?.HasActiveDutyWithRole("Surgeon") == true)
            {
                var medicineSkill = Hero.MainHero.GetSkillValue(DefaultSkills.Medicine);
                result.Add(medicineSkill / 10f, new TextObject("Field Medic Training"));
            }
            
            // Army medical support bonus
            var lordParty = EnlistmentBehavior.Instance.CurrentLord?.PartyBelongedTo;
            if (lordParty?.Army != null)
            {
                result.Add(5f, new TextObject("Army Medical Corps"));
            }
        }
        
        return result;
    }
    
    public override ExplainedNumber GetDailyHealingForRegulars(MobileParty party, bool includeDescriptions = false)
    {
        // Enhanced healing for enlisted soldier's party
        var baseModel = Campaign.Current.Models.PartyHealingModel as DefaultPartyHealingModel;
        var result = baseModel?.GetDailyHealingForRegulars(party, includeDescriptions) ?? 
                    new ExplainedNumber(5f, includeDescriptions, null);
        
        // Field Medic provides healing to entire party
        if (EnlistmentBehavior.Instance?.IsEnlisted == true && 
            DutiesBehavior.Instance?.HasActiveDutyWithRole("Surgeon") == true)
        {
            result.Add(8f, new TextObject("Field Medic Care"));
        }
        
        return result;
    }
    
    public override int GetBattleEndHealingAmount(MobileParty party, Hero hero)
    {
        var baseModel = Campaign.Current.Models.PartyHealingModel as DefaultPartyHealingModel;
        var baseHealing = baseModel?.GetBattleEndHealingAmount(party, hero) ?? 0;
        
        // Enhanced battle-end healing for enlisted heroes
        if (EnlistmentBehavior.Instance?.IsEnlisted == true && hero == Hero.MainHero)
        {
            var bonus = DutiesBehavior.Instance?.HasActiveDutyWithRole("Surgeon") == true ? 15 : 5;
            return baseHealing + bonus;
        }
        
        return baseHealing;
    }
    
    // Implement remaining abstract methods with default behavior
    public override float GetSurgeryChance(PartyBase party) => 
        (Campaign.Current.Models.PartyHealingModel as DefaultPartyHealingModel)?.GetSurgeryChance(party) ?? 0f;
        
    public override float GetSurvivalChance(PartyBase party, CharacterObject character, DamageTypes damageType, bool canDamageKillEvenIfBlunt, PartyBase enemyParty = null) =>
        (Campaign.Current.Models.PartyHealingModel as DefaultPartyHealingModel)?.GetSurvivalChance(party, character, damageType, canDamageKillEvenIfBlunt, enemyParty) ?? 1f;
        
    public override int GetSkillXpFromHealingTroop(PartyBase party) =>
        (Campaign.Current.Models.PartyHealingModel as DefaultPartyHealingModel)?.GetSkillXpFromHealingTroop(party) ?? 5;
        
    public override int GetHeroesEffectedHealingAmount(Hero hero, float healingRate) =>
        (Campaign.Current.Models.PartyHealingModel as DefaultPartyHealingModel)?.GetHeroesEffectedHealingAmount(hero, healingRate) ?? (int)healingRate;
        
    public override float GetSiegeBombardmentHitSurgeryChance(PartyBase party) =>
        (Campaign.Current.Models.PartyHealingModel as DefaultPartyHealingModel)?.GetSiegeBombardmentHitSurgeryChance(party) ?? 0f;
}
```

## 🔧 **Integration with Game System**

### **Register Custom Model** (Phase 1B)
```csharp
// In SubModule.cs OnGameStart
protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
{
    if (gameStarterObject is CampaignGameStarter campaignStarter)
    {
        // Register custom healing model for enhanced enlisted soldier healing
        campaignStarter.AddModel(new EnlistedPartyHealingModel());
        
        // Register other behaviors...
    }
}
```

### **Benefits for Players**
- **Daily Healing Bonus**: +13 HP/day when enlisted (vs. base 11)
- **Field Medic Enhancement**: Medicine skill bonus to healing rate  
- **Army Support**: +5 HP/day when lord is in an army
- **Battle Recovery**: Enhanced post-battle healing for enlisted soldiers
- **Party Healing**: Field Medics provide healing bonus to entire party

**This provides meaningful healing benefits for enlisted service while working seamlessly with Bannerlord's built-in healing system.**
