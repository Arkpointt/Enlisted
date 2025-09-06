# Bannerlord Save System Requirements

**Based on decompiled TaleWorlds.SaveSystem analysis for our duties system implementation**

## ✅ GOOD NEWS: Most Types Already Supported

### Core Container Types Already Defined
**From SaveableCampaignTypeDefiner.cs lines 515-525:**

```csharp
// Our exact dictionary types are ALREADY defined in core system!
base.ConstructContainerDefinition(typeof(Dictionary<Hero, int>));           // Line 516 ✅
base.ConstructContainerDefinition(typeof(Dictionary<IFaction, int>));       // Line 522 ✅  
base.ConstructContainerDefinition(typeof(List<Hero>));                      // Line 400 ✅
base.ConstructContainerDefinition(typeof(List<IFaction>));                  // Line 415 ✅
```

### Core Game Object Types Already Supported
**Equipment and ItemRoster are core types with SaveableProperty support:**

```csharp
// From Hero.cs - Equipment is already properly serializable
[SaveableProperty(210)]
private Equipment _battleEquipment { get; set; }        // Line 573 ✅

[SaveableProperty(220)] 
private Equipment _civilianEquipment { get; set; }      // Line 578 ✅

// ItemRoster is a core serializable type used throughout campaign system ✅
```

## 🎯 What This Means for Our Implementation

### **SIMPLIFIED APPROACH: We DON'T Need Custom SaveDefiner**

Our save implementation can use standard SyncData patterns without custom type definitions:

```csharp
public class EnlistmentBehavior : CampaignBehaviorBase
{
    // All these types are ALREADY supported by core save system
    private Hero _enlistedLord;                                    // ✅ Core type
    private int _enlistmentTier = 1;                              // ✅ Basic type
    private CampaignTime _enlistmentDate;                         // ✅ Core type
    private Dictionary<Hero, int> _lordReputation = new();        // ✅ Already defined in SaveableCampaignTypeDefiner
    private Dictionary<IFaction, int> _factionReputation = new(); // ✅ Already defined in SaveableCampaignTypeDefiner
    private List<IFaction> _vassalageOffersReceived = new();      // ✅ Already defined in SaveableCampaignTypeDefiner
    private Equipment _personalBattleEquipment;                   // ✅ Core type with SaveableProperty support
    private ItemRoster _personalInventory = new ItemRoster();     // ✅ Core serializable type

    public override void SyncData(IDataStore dataStore)
    {
        // Standard SyncData - no custom definer needed!
        dataStore.SyncData("_enlistedLord", ref _enlistedLord);
        dataStore.SyncData("_enlistmentTier", ref _enlistmentTier);
        dataStore.SyncData("_enlistmentDate", ref _enlistmentDate);
        dataStore.SyncData("_lordReputation", ref _lordReputation);           // Works with existing container definition
        dataStore.SyncData("_factionReputation", ref _factionReputation);     // Works with existing container definition
        dataStore.SyncData("_vassalageOffersReceived", ref _vassalageOffersReceived); // Works with existing container definition
        dataStore.SyncData("_personalBattleEquipment", ref _personalBattleEquipment); // Core type
        dataStore.SyncData("_personalInventory", ref _personalInventory);     // Core type
    }
}
```

## 📋 Save System Best Practices (From Decompiled Examples)

### 1. Use SaveableProperty for Properties
```csharp
// Pattern from Hero.cs
[SaveableProperty(210)]
private Equipment _battleEquipment { get; set; }
```

### 2. Use SaveableField for Public Fields  
```csharp
// Pattern from Romance.RomanticState
[SaveableField(0)]
public Hero Person1;

[SaveableField(1)] 
public Hero Person2;
```

### 3. Simple SyncData Implementation
```csharp
// Standard pattern from CampaignBehaviorBase
public override void SyncData(IDataStore dataStore)
{
    // Simple, direct SyncData calls - no try-catch needed
    dataStore.SyncData("fieldName", ref _fieldName);
}
```

### 4. No Exception Handling in SyncData
**Key insight**: Official campaign behaviors don't wrap SyncData in try-catch - let exceptions bubble up for proper error handling.

## ⚠️ When You DO Need Custom SaveDefiner

### Only Required For:
1. **Custom enum types** that aren't in core system
2. **Custom class/struct types** that need serialization
3. **New container combinations** not in SaveableCampaignTypeDefiner

### Our Custom Types Needing SaveDefiner:
```csharp
public class EnlistedSaveDefiner : SaveableTypeDefiner
{
    public EnlistedSaveDefiner() : base(1436500013) // Unique ID
    {
    }
    
    protected override void DefineEnumTypes()
    {
        // Only our custom enums need definition
        base.AddEnumDefinition(typeof(TroopType), 1, null);
        base.AddEnumDefinition(typeof(RetirementChoice), 2, null);
    }
    
    // DefineContainerDefinitions() not needed - our containers already exist!
}
```

## 🎯 CORRECTED Implementation Strategy

### **OPTION A: No SaveDefiner Needed (RECOMMENDED)**
```csharp
// Use only types already supported by core save system
// Replace custom enums with int constants or string IDs
// Result: Simple, robust, no custom save definer required
```

### **OPTION B: Minimal SaveDefiner (If Custom Enums Needed)**
```csharp
// Add SaveDefiner only for custom enums
// Use existing container definitions for dictionaries/lists
// Result: Minimal complexity, custom enum support
```

## 📊 Final Recommendation

### **Use Standard Save Patterns (No Custom SaveDefiner)**

**Reasons:**
- ✅ All our dictionary types already supported
- ✅ Equipment and ItemRoster are core serializable types
- ✅ Simpler implementation with no custom save definitions
- ✅ Lower maintenance burden
- ✅ Follows existing campaign behavior patterns

**Implementation:**
```csharp
public override void SyncData(IDataStore dataStore)
{
    // Version tracking
    dataStore.SyncData("_saveVersion", ref _saveVersion);
    
    // Simple types (always safe)
    dataStore.SyncData("_enlistedLord", ref _enlistedLord);
    dataStore.SyncData("_enlistmentTier", ref _enlistmentTier);
    dataStore.SyncData("_enlistmentXP", ref _enlistmentXP);
    dataStore.SyncData("_enlistmentDate", ref _enlistmentDate);
    
    // Complex types (already supported by core system)
    dataStore.SyncData("_lordReputation", ref _lordReputation);           // Dictionary<Hero, int> ✅
    dataStore.SyncData("_factionReputation", ref _factionReputation);     // Dictionary<IFaction, int> ✅ 
    dataStore.SyncData("_vassalageOffersReceived", ref _vassalageOffersReceived); // List<IFaction> ✅
    dataStore.SyncData("_personalBattleEquipment", ref _personalBattleEquipment); // Equipment ✅
    dataStore.SyncData("_personalInventory", ref _personalInventory);     // ItemRoster ✅
    
    // Post-load validation (separate from SyncData per best practices)
    if (dataStore.IsLoading)
    {
        ValidateLoadedState();
    }
}
```

**Our save implementation is already compatible with Bannerlord save system - no custom SaveDefiner required!**
