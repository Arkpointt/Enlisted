# Harmony Patches Best Practices Summary

**Following [Bannerlord Modding documentation](https://docs.bannerlordmodding.lt/modding/harmony/) standards**

## ✅ Our Harmony Implementation Status

### 1. Activation Pattern (CORRECT)
```csharp
// src/Mod.Entry/SubModule.cs - Follows standard pattern
protected override void OnSubModuleLoad()
{
    base.OnSubModuleLoad();
    
    try
    {
        Harmony harmony = new Harmony("com.enlisted.mod");
        harmony.PatchAll();
        // Silent success per best practices
    }
    catch (Exception ex)
    {
        ModLogger.Error("Compatibility", "Harmony initialization failed", ex);
    }
}
```

### 2. Property Patching (CORRECT)
```csharp
// Proper MethodType.Getter specification for properties
[HarmonyPatch(typeof(MobileParty), "EffectiveEngineer", MethodType.Getter)]
[HarmonyPriority(999)] // High priority to run before other mods
public class DutiesEffectiveEngineerPatch
```

### 3. Method Overload Handling (CORRECT) 
```csharp
// Specify parameter types to avoid ambiguous match errors
[HarmonyPatch(typeof(HeroDeveloper), "AddSkillXp", typeof(SkillObject), typeof(float), typeof(bool), typeof(bool))]
[HarmonyPriority(500)]
public class DutiesXpSharingPatch
```

### 4. Priority System (IMPLEMENTED)
- **Officer Role Patches**: `[HarmonyPriority(999)]` - High priority (run before other mods)
- **XP Sharing Patch**: `[HarmonyPriority(500)]` - Medium priority (doesn't conflict)

### 5. Error Handling (ROBUST)
```csharp
static bool Prefix(MobileParty __instance, ref Hero __result)
{
    try
    {
        // Comprehensive null checks and state validation
        if (EnlistmentBehavior.Instance?.IsEnlisted != true || 
            __instance == null ||
            EnlistmentBehavior.Instance.CurrentLord?.PartyBelongedTo != __instance)
        {
            return true; // Fail safe - use original behavior
        }
        
        // Patch logic...
    }
    catch (Exception ex)
    {
        ModLogger.Error("Patches", $"Patch error: {ex.Message}");
        return true; // Always fail safe
    }
}
```

## ⚠️ Potential Issues Addressed

### 1. Ambiguous Match Prevention
- ✅ **Specified parameter types** for `HeroDeveloper.AddSkillXp` to avoid multiple overload confusion
- ✅ **Used MethodType.Getter** for property patches to be explicit about target
- ✅ **Clear class names** with descriptive patch purposes

### 2. Model Patching Considerations
- ✅ **No game model patches needed** - our system uses officer role substitution, not model patching
- ✅ **Behaviors in OnGameStart** - patches in OnSubModuleLoad per standards
- ✅ **No static initialization issues** - our patches don't modify core game models

### 3. Mod Compatibility
- ✅ **High priority patches** for officer roles to run before potential conflicts
- ✅ **Defensive guards** that validate state before applying patches
- ✅ **Fail-safe behavior** - always use original behavior if anything fails
- ✅ **Optional before attributes** can be added if specific mod conflicts discovered

### 4. Performance Considerations
- ✅ **Minimal patches** (4-5 vs SAS's 37+)
- ✅ **Guard clauses exit early** when patch not applicable
- ✅ **No allocations in patch code** - simple state checks only
- ✅ **Silent operation** - no logging spam in frequent paths

## 🎯 Patch Implementation Summary

### Required Patches (4)
1. **DutiesEffectiveEngineerPatch** - Engineering role substitution
2. **DutiesEffectiveScoutPatch** - Scouting role substitution  
3. **DutiesEffectiveQuartermasterPatch** - Supply management role substitution
4. **DutiesEffectiveSurgeonPatch** - Medical role substitution

### Optional Patches (1)  
5. **DutiesXpSharingPatch** - Dynamic XP sharing from lord activities

### Best Practices Compliance
- ✅ **Modern activation pattern** in SubModule.cs
- ✅ **Proper priority specification** with HarmonyPriority attributes
- ✅ **Method signature specification** to avoid ambiguous matches  
- ✅ **Robust error handling** with fail-safe behavior
- ✅ **Property getter specification** using MethodType.Getter
- ✅ **Comprehensive guards** for null checks and state validation
- ✅ **Performance-friendly** with early exit conditions and no allocations

## 🎖️ Mod Compatibility

### Harmony ID: "com.enlisted.mod"
### Dependencies: Bannerlord.Harmony (declared in SubModule.xml)
### Load Order: Standard (no special requirements)
### Conflicts: None expected - defensive patches with fail-safe behavior

## 🚨 Critical Fixes Implemented

### Quest Item Protection (CRITICAL)
- ✅ **Equipment backup now skips quest items** using `EquipmentElement.IsQuestItem`
- ✅ **Special items preserved** using `ItemFlags.NotUsableByPlayer` and `ItemFlags.NonTransferable`  
- ✅ **Prevents permanent quest item loss** during enlistment equipment backup

### Dialog Token Compatibility (ROBUSTNESS)
- ✅ **Multi-token registration** for `lord_talk_speak_diplomacy_2`, `lord_politics_request`, `hero_main_options`
- ✅ **Prevents "works on my machine" issues** across different game builds
- ✅ **Graceful fallback system** when primary tokens unavailable

### Save Data Safety (FUTURE-PROOFING)
- ✅ **Save versioning system** with `_saveVersion` field for migration support
- ✅ **State validation after load** prevents corruption from invalid save data
- ✅ **Safe state reset** when save corruption detected

### Equipment Visual Refresh (UX)
- ✅ **Immediate UI updates** using `OnHeroEquipmentChanged` event dispatch
- ✅ **Equipment screen refresh** after backup restoration
- ✅ **Smooth visual transitions** for equipment changes

**Our Harmony implementation follows all Bannerlord modding best practices with critical robustness fixes for maximum compatibility and reliability.**
