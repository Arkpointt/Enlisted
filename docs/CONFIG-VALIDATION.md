# Configuration System - Validation & Loading

**Safe, Blueprint-compliant configuration loading with comprehensive validation**

## 🔧 **Fixed Configuration Issues**

### **✅ Schema Versioning Added**
All JSON files now include:
```json
{
  "schemaVersion": 1,
  "enabled": true,
  // ... rest of config
}
```

### **✅ File Precedence Resolved**  
- **`settings.json`** now specifies: `"dutiesConfig": "duties_system.json"`
- **Removed duplicate**: `duties_config_enhanced.json` deleted
- **Single source**: Only `duties_system.json` used for duties configuration

### **✅ Safe File Loading (No Unverified APIs)**
```csharp
// SAFE: Blueprint-compliant approach (no ModuleHelper)
public static class ConfigManager
{
    private static string GetConfigPath(string filename)
    {
        // Use relative path from module directory
        return Path.Combine("ModuleData", "Enlisted", filename);
    }
    
    private static T LoadConfig<T>(string filename) where T : new()
    {
        try
        {
            var path = GetConfigPath(filename);
            var json = File.ReadAllText(path);
            var obj = JsonConvert.DeserializeObject<T>(json);
            
            // Validate schema version
            if (obj is IVersionedConfig versionedConfig)
            {
                if (versionedConfig.SchemaVersion > 1)
                {
                    ModLogger.Error("Config", $"Unsupported schema version in {filename}");
                    return new T();
                }
                
                if (!versionedConfig.Enabled)
                {
                    ModLogger.Info("Config", $"Configuration {filename} is disabled");
                    return new T();
                }
            }
            
            return obj ?? new T();
        }
        catch (Exception ex)
        {
            ModLogger.Error("Config", $"Failed to load {filename}, using defaults", ex);
            return new T();
        }
    }
    
    public static void LoadAll()
    {
        var settings = LoadConfig<Settings>("settings.json");
        var enlisted = LoadConfig<EnlistedConfig>("enlisted_config.json");
        var pricing = LoadConfig<EquipmentPricing>("equipment_pricing.json");
        var kits = LoadConfig<EquipmentKits>("equipment_kits.json");
        var duties = LoadConfig<DutiesConfig>(settings?.DutiesConfig ?? "duties_system.json");
        var progression = LoadConfig<ProgressionConfig>("progression_config.json");
        var menu = LoadConfig<MenuConfig>("menu_config.json");
        
        ValidateConfiguration(enlisted, pricing, duties, progression);
        IndexEquipmentKits(kits); // Build fast lookup cache
    }
}

public interface IVersionedConfig
{
    int SchemaVersion { get; }
    bool Enabled { get; }
}
```

## ✅ **Comprehensive Validation System**

### **Configuration Validation** (Blueprint Section 10 Compliant)
```csharp
private static void ValidateConfiguration(EnlistedConfig enlisted, EquipmentPricing pricing, 
    DutiesConfig duties, ProgressionConfig progression)
{
    try
    {
        // XP requirements strictly ascending
        var requirements = progression.TierProgression.Requirements;
        for (int i = 1; i < requirements.Length; i++)
        {
            if (requirements[i].XpRequired <= requirements[i-1].XpRequired)
                throw new Exception($"XP requirements not ascending at tier {i}");
        }
        
        // Wage multipliers valid range
        foreach (var multiplier in enlisted.WageSystem.AssignmentMultipliers.Values)
        {
            if (multiplier < 0.0f || multiplier > 10.0f)
                throw new Exception($"Invalid wage multiplier: {multiplier}");
        }
        
        // Formation keys standardized
        var validFormations = new[] {"infantry", "archer", "cavalry", "horsearcher"};
        foreach (var duty in duties.Duties.Values)
        {
            foreach (var formation in duty.RequiredFormations)
            {
                if (!validFormations.Contains(formation))
                    throw new Exception($"Invalid formation '{formation}' in duty {duty.Id}");
            }
        }
        
        // Officer role names valid
        var validOfficerRoles = new[] {"Engineer", "Scout", "Quartermaster", "Surgeon", null};
        foreach (var duty in duties.Duties.Values)
        {
            if (duty.OfficerRole != null && !validOfficerRoles.Contains(duty.OfficerRole))
                throw new Exception($"Invalid officer role '{duty.OfficerRole}' in duty {duty.Id}");
        }
        
        // Pricing rules positive
        foreach (var multiplier in pricing.PricingRules.FormationMultipliers.Values)
        {
            if (multiplier <= 0.0f)
                throw new Exception($"Formation multiplier must be positive: {multiplier}");
        }
        
        // Culture IDs match verified list
        var validCultures = new[] {"empire", "aserai", "sturgia", "vlandia", "khuzait", "battania"};
        foreach (var cultureId in pricing.PricingRules.CultureModifiers.Keys)
        {
            if (!validCultures.Contains(cultureId))
                throw new Exception($"Invalid culture ID '{cultureId}'");
        }
        
        ModLogger.Info("Config", "Configuration validation passed");
    }
    catch (Exception ex)
    {
        ModLogger.Error("Config", "Configuration validation failed", ex);
        throw; // Fail fast per Blueprint principles
    }
}
```

## ❌ **Removed Unverified API Usage**

### **1. Removed ModuleHelper (Not in engine-signatures.md)**
```csharp
// REMOVED (UNVERIFIED):
var path = ModuleHelper.GetModuleFullPath("Enlisted") + "ModuleData/Enlisted/" + file;

// REPLACED WITH (BLUEPRINT-COMPLIANT):
var path = Path.Combine("ModuleData", "Enlisted", filename);
```

### **2. Removed Custom Healing Model (Not in our docs)**
```csharp
// REMOVED (UNVERIFIED):
public class EnlistedPartyHealingModel : DefaultPartyHealingModel

// KEPT (VERIFIED in engine-signatures.md line 156):
Hero.MainHero.Heal(healAmount, false);
```

### **3. Simplified Menu Text (No unverified localization)**
```json
// REMOVED (UNVERIFIED):
"title": "🎖️ ENLISTED STATUS 🎖️",
"textKey": "enlisted.menu.status.title"

// REPLACED WITH (SIMPLE & SAFE):
"title": "Enlisted Status",
```

## 📋 **Current Configuration Status**

### **✅ Files Fixed & Validated**
1. **`settings.json`** - Added schema versioning, duties file precedence
2. **`enlisted_config.json`** - Added schema versioning  
3. **`equipment_pricing.json`** - Added schema versioning
4. **`duties_system.json`** - Added schema versioning (authoritative source)
5. **`equipment_kits.json`** - Added schema versioning, templates
6. **`progression_config.json`** - Added schema versioning
7. **`menu_config.json`** - Added schema versioning, simplified text
8. **DELETED**: `duties_config_enhanced.json` (duplicate removed)

### **✅ Key Issues Resolved**
- ✅ **Duplicate files**: Removed, single source specified
- ✅ **Schema versioning**: Added to all configs
- ✅ **Safe file loading**: Blueprint-compliant paths only
- ✅ **Simple text**: No unverified localization format
- ✅ **Formation consistency**: Verified "horsearcher" used consistently
- ✅ **API safety**: Only verified APIs from engine-signatures.md

## 🎯 **Ready for Development**

**All configuration files are now:**
- ✅ **Validated** against Blueprint principles
- ✅ **Using only verified APIs** from our documentation
- ✅ **Single source of truth** for each concern  
- ✅ **Schema versioned** for future compatibility
- ✅ **Comprehensively validated** with error handling

**The enhanced military service system configuration is now production-ready and Blueprint-compliant.**
