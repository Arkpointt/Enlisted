# API Helpers - Promotion & Reflection Utilities

**Helper APIs and promotion utilities for the Enlisted military service system**

## 🎯 **SYSTEM CHANGE: SAS Troop Selection Approach**

**ARCHITECTURAL DECISION** (2025-01-28): **Switched from custom equipment kits to SAS-style troop selection**

**Why This Change**:
- ✅ **More Engaging**: Players choose **real Bannerlord troops** ("Imperial Legionary" vs "T3 Infantry Kit")
- ✅ **Simpler**: No maintenance of 40+ custom equipment kits
- ✅ **Authentic**: Uses actual game troop equipment from `CharacterObject.BattleEquipments`
- ✅ **Cultural Immersion**: Each faction's troops feel unique and recognizable

**API Discovery Status: COMPLETE + PHASE 1A/1B IMPLEMENTED**
- ✅ Troop discovery APIs verified and **IMPLEMENTED**
- ✅ Equipment extraction APIs verified and **IMPLEMENTED**  
- ✅ Culture filtering APIs verified and **IMPLEMENTED**
- ✅ Equipment assignment APIs verified and **IMPLEMENTED**
- ✅ **ALL SAS critical APIs verified** in current Bannerlord version and **IN PRODUCTION**

**Implementation Status**: Phase 1A/1B complete with **SAS approach 100% compatible** and **battle crash prevention implemented** using comprehensive lord validation.

## 🛠️ **Troop Selection Helper Methods** 

```csharp
// VERIFIED: Troop discovery API for real Bannerlord troops
MBObjectManager.Instance.GetObjectTypeList<CharacterObject>()

// VERIFIED: Culture filtering for troop selection  
character.Culture.StringId  // "empire", "aserai", "khuzait", etc.
character.Tier              // 1-7 military tiers
character.IsSoldier         // Filter military troops only

// VERIFIED: Equipment extraction from real troops
character.BattleEquipments.FirstOrDefault()  // Get troop's actual equipment

// VERIFIED: Formation detection from troop properties
character.IsRanged && character.IsMounted    // Horse Archer
character.IsMounted                          // Cavalry  
character.IsRanged                           // Archer
// Default: Infantry

// VERIFIED: Primary equipment assignment API
Helpers.EquipmentHelper :: AssignHeroEquipmentFromEquipment(Hero hero, Equipment equipment)

// Equipment selection model methods
TaleWorlds.CampaignSystem.ComponentInterfaces.EquipmentSelectionModel :: GetEquipmentRostersForHeroComeOfAge(Hero hero, bool isCivilian)
TaleWorlds.CampaignSystem.ComponentInterfaces.EquipmentSelectionModel :: GetEquipmentRostersForCompanion(Hero companionHero, bool isCivilian)
TaleWorlds.CampaignSystem.ComponentInterfaces.EquipmentSelectionModel :: GetEquipmentRostersForDeliveredOffspring(Hero hero)
TaleWorlds.CampaignSystem.ComponentInterfaces.EquipmentSelectionModel :: GetEquipmentRostersForHeroReachesTeenAge(Hero hero)
TaleWorlds.CampaignSystem.ComponentInterfaces.EquipmentSelectionModel :: GetEquipmentRostersForInitialChildrenGeneration(Hero hero)
TaleWorlds.CampaignSystem.GameComponents.DefaultEquipmentSelectionModel :: GetEquipmentRostersForHeroComeOfAge(Hero hero, bool isCivilian)
```

## 📋 **Object Manager Methods**

```csharp
// Core object management for equipment and character discovery
TaleWorlds.ObjectSystem.MBObjectManager :: Instance { get; }
TaleWorlds.ObjectSystem.MBObjectManager :: GetObject<T>(string stringId)
TaleWorlds.ObjectSystem.MBObjectManager :: GetObjectTypeList<T>()
```

## 👥 **Character Object Equipment Properties**

```csharp
// Character template analysis
TaleWorlds.CampaignSystem.CharacterObject :: Tier { get; }
TaleWorlds.CampaignSystem.CharacterObject :: UpgradeTargets { get; }
TaleWorlds.CampaignSystem.CharacterObject :: Culture { get; }
TaleWorlds.CampaignSystem.CharacterObject :: IsRanged { get; }
TaleWorlds.CampaignSystem.CharacterObject :: IsMounted { get; }
TaleWorlds.CampaignSystem.CharacterObject :: BattleEquipments { get; }

// Hero equipment access
TaleWorlds.CampaignSystem.Hero :: BattleEquipment { get; set; }
TaleWorlds.CampaignSystem.Hero :: CivilianEquipment { get; set; }
TaleWorlds.CampaignSystem.Hero :: CharacterObject { get; }
```

## ⚙️ **Equipment Management Core**

```csharp
// Equipment object manipulation
TaleWorlds.Core.Equipment :: Equipment()
TaleWorlds.Core.Equipment :: Equipment(bool isCivilian)
TaleWorlds.Core.Equipment :: Equipment(Equipment equipment)
TaleWorlds.Core.Equipment :: Clone(bool cloneWithoutWeapons)
TaleWorlds.Core.Equipment :: FillFrom(Equipment sourceEquipment, bool useSourceEquipmentType)
TaleWorlds.Core.Equipment :: GetEquipmentFromSlot(EquipmentIndex index)
TaleWorlds.Core.Equipment :: AddEquipmentToSlotWithoutAgent(EquipmentIndex index, EquipmentElement element)
```

## 🔍 **Reflection Candidates**

### Battle Participation APIs (May Require Reflection)
```csharp
// These may be internal/private properties requiring reflection
MobileParty.ShouldJoinPlayerBattles { get; set; }  // For battle participation
MobilePartyAi.ClearMoveToParty()                   // For AI state management
MobilePartyAi.SetMoveModeHold()                    // For hold position commands
MobileParty.AttachTo(MobileParty)                  // For party attachment
MobileParty.Detach()                               // For party detachment
```

### Safe Reflection Pattern
```csharp
// Safe reflection usage with fallbacks
private bool TrySetProperty(object target, string propertyName, object value)
{
    try
    {
        var prop = target.GetType().GetProperty(propertyName, 
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (prop != null && prop.CanWrite)
        {
            prop.SetValue(target, value);
            return true;
        }
    }
    catch (Exception ex)
    {
        ModLogger.Error("Reflection", $"Failed to set {propertyName}", ex);
    }
    return false;
}

// Usage for battle participation
private bool TrySetBattleParticipation(bool shouldJoin)
{
    try
    {
        // Primary: Try reflection for ShouldJoinPlayerBattles
        if (TrySetProperty(MobileParty.MainParty, "ShouldJoinPlayerBattles", shouldJoin))
            return true;
            
        // Fallback: Use positioning and escort behavior
        if (shouldJoin)
        {
            var lordParty = EnlistmentBehavior.Instance.CurrentLord?.PartyBelongedTo;
            if (lordParty != null)
                MobileParty.MainParty.Ai.SetMoveEscortParty(lordParty);
        }
        
        return true;
    }
    catch (Exception ex)
    {
        ModLogger.Error("Combat", "Failed to set battle participation", ex);
        return false;
    }
}
```

## 🎖️ **Promotion Helper Implementation**

### Formation Detection
```csharp
// Auto-detect player formation based on equipment (matches SAS logic)
public TroopType DetectPlayerFormation()
{
    var hero = Hero.MainHero.CharacterObject;
    
    if (hero.IsRanged && hero.IsMounted)
        return TroopType.HorseArcher;   // Bow/Crossbow + Horse
    else if (hero.IsMounted)
        return TroopType.Cavalry;       // Melee + Horse  
    else if (hero.IsRanged)
        return TroopType.Archer;        // Bow/Crossbow + No Horse
    else
        return TroopType.Infantry;      // Melee + No Horse (default)
}
```

### Troop Selection Implementation
```csharp
// NEW SYSTEM: Apply equipment from real Bannerlord troop selection
// cultureId comes from EnlistmentBehavior.Instance.CurrentLord.Culture.StringId
public void ApplySelectedTroopEquipment(Hero hero, CharacterObject selectedTroop)
{
    try
    {
        // Extract equipment directly from selected troop's BattleEquipments
        var troopEquipment = selectedTroop.BattleEquipments.FirstOrDefault();
        if (troopEquipment != null)
        {
            EquipmentHelper.AssignHeroEquipmentFromEquipment(hero, troopEquipment);
            
            var message = new TextObject("Promoted to {TROOP_NAME}!");
            message.SetTextVariable("TROOP_NAME", selectedTroop.Name);
            InformationManager.AddQuickInformation(message, 0, hero.CharacterObject, 
                "event:/ui/notification/levelup");
        }
    }
    catch (Exception ex)
    {
        ModLogger.Error("TroopSelection", "Troop equipment assignment failed", ex);
    }
}
```

### 1-Year Progression System
```csharp
// Updated: Realistic 1-year military service progression
public void CheckForPromotion()
{
    var tierRequirements = new int[] { 0, 500, 1500, 3500, 7000, 12000, 18000 }; // 365-day progression
    
    while (_enlistmentTier < 7 && _enlistmentXP >= tierRequirements[_enlistmentTier + 1])
    {
        _enlistmentTier++;
        TriggerTroopSelectionMenu(_enlistmentTier); // SAS-style troop selection
    }
}

private void TriggerPromotion(int newTier)
{
    // Formation selection on first promotion (Tier 2)
    if (newTier == 2 && _playerFormation == TroopType.None)
    {
        TriggerFormationSelectionMenu();
    }
    
    // Trigger troop selection for new tier
    TriggerTroopSelectionMenu(newTier);
    
    // Trigger promotion ceremony
    var tierName = GetTierName(newTier);
    var message = new TextObject("{=promotion_ceremony}Congratulations! Promoted to {TIER}.");
    message.SetTextVariable("TIER", tierName);
    InformationManager.AddQuickInformation(message, 0, Hero.MainHero.CharacterObject, 
        "event:/ui/notification/levelup");
}
```

## 🔄 **Safe API Usage Patterns**

### Equipment Item Validation (VERIFIED Patterns)
```csharp
// Validate items exist before applying - using VERIFIED game item patterns
private bool ValidateEquipmentItem(string itemId)
{
    try
    {
        var item = MBObjectManager.Instance.GetObject<ItemObject>(itemId);
        return item != null;
    }
    catch
    {
        return false;
    }
}

// VERIFIED: Try culture prefix pattern first, fallback to generic items
private string GetValidatedCultureItem(string culture, string itemType, int tier)
{
    // Primary pattern: {culture}_{itemType}_{variant}_t{tier}
    var primaryPattern = $"{culture}_{itemType}_1_t{tier}";
    if (ValidateEquipmentItem(primaryPattern)) return primaryPattern;
    
    // Secondary pattern: try different variants  
    for (int variant = 2; variant <= 5; variant++)
    {
        var variantPattern = $"{culture}_{itemType}_{variant}_t{tier}";
        if (ValidateEquipmentItem(variantPattern)) return variantPattern;
    }
    
    // Fallback: noble variants for high tiers
    if (tier >= 5)
    {
        var noblePattern = $"{culture}_noble_{itemType}_1_t{tier}";
        if (ValidateEquipmentItem(noblePattern)) return noblePattern;
    }
    
    return null; // No valid item found
}

// VERIFIED: Real game examples for each culture
private readonly Dictionary<string, string[]> VerifiedCultureItems = new()
{
    ["aserai"] = new[] { "aserai_sword_1_t2", "aserai_sword_3_t3", "aserai_sword_4_t4", "aserai_sword_5_t4", "aserai_2haxe_1_t3", "long_desert_robe" },
    ["khuzait"] = new[] { "khuzait_sword_1_t2", "khuzait_sword_2_t3", "khuzait_noble_sword_1_t5", "khuzait_civil_coat", "eastern_lamellar_armor" },
    ["vlandia"] = new[] { "vlandia_axe_2_t4", "crossbow_c", "crossbow_e" },
    ["sturgia"] = new[] { "sturgia_axe_4_t4", "sturgia_axe_5_t5", "sturgia_noble_sword_1_t5", "sturgia_noble_sword_2_t5" },
    ["battania"] = new[] { "battania_axe_2_t4", "battania_axe_3_t5", "battania_2haxe_1_t2", "battania_noble_sword_1_t5" },
    ["empire"] = new[] { "empire_sword_1_t2", "empire_sword_2_t3", "empire_sword_6_t5", "imperial_bow_t1" }
};
```

### Culture Object Safety
```csharp
// Safe culture object access
private CultureObject GetCultureSafely(string cultureId)
{
    try
    {
        return MBObjectManager.Instance.GetObject<CultureObject>(cultureId);
    }
    catch (Exception ex)
    {
        ModLogger.Error("Culture", $"Failed to get culture {cultureId}", ex);
        return null;
    }
}
```

**These helper APIs provide safe, robust access to Bannerlord's equipment and character systems with comprehensive error handling and fallback mechanisms.**
