# Enlisted Configuration Files

**Complete configuration system for the Enlisted military service mod**

## 📋 **Configuration Files Overview**

This directory contains **7 JSON configuration files** that control all aspects of the military service system:

### **🔧 Core System Configuration**

#### **1. `settings.json`** - Master Settings  
**Purpose**: Main mod settings and feature toggles  
**Key Settings**:
- `dutiesConfig`: Specifies which duties file to use
- Logging configuration for development/debugging
- SAS behavior settings (attach ranges, IsActive management for encounter prevention)

#### **2. `enlisted_config.json`** - Core Military System
**Purpose**: Base enlistment mechanics and progression  
**Contains**:
- Tier progression XP requirements (T1-T7)
- Wage calculation formulas and multipliers
- Formation definitions with culture-specific names
- Retirement requirements (365 days minimum)

### **⚔️ Military Service Configuration**

#### **3. `duties_system.json`** - Military Duties Framework
**Purpose**: Complete duties system with 9+ military assignments  
**Contains**:
- Individual duty definitions (Runner, Field Medic, Pathfinder, etc.)
- Formation compatibility requirements
- Officer role integration (Engineer, Scout, Quartermaster, Surgeon)
- XP sources and progression mechanics
- Unlock conditions and skill requirements

#### **4. `progression_config.json`** - Advancement System
**Purpose**: Tier progression and advancement mechanics  
**Contains**:
- Detailed tier requirements and benefits
- Wage calculation with bonuses and multipliers
- Formation selection mechanics (triggers at Tier 2)
- Veteran system requirements and benefits
- Special event XP rewards

### **🛡️ Equipment System Configuration**

#### **5. `equipment_pricing.json`** - Economic System
**Purpose**: Realistic military equipment economics  
**Contains**:
- Formation-based pricing multipliers (Infantry 1.0× → Horse Archer 2.5×)
- Culture economic modifiers (Khuzait 0.8× → Vlandia 1.2×)
- Elite troop cost overrides for special units
- Medical treatment settings (5-day/2-day cooldowns)

#### **6. `equipment_kits.json`** - DEPRECATED (Troop Selection System)
**Status**: ❌ **DEPRECATED** - Switched to SAS-style troop selection approach
**New System**: Players select from real Bannerlord troops; equipment extracted from `CharacterObject.BattleEquipments`  
**Contains**:
- Troop selection system configuration
- Promotion notification settings  
- Formation detection rules
- Legacy equipment kits (preserved for reference)

### **🎮 User Interface Configuration**

#### **7. `menu_config.json`** - Menu System
**Purpose**: Complete UI framework for enlisted interface  
**Contains**:
- Menu definitions (enlisted_status, enlisted_troop_selection, enlisted_record)
- Menu option configurations with conditions and tooltips
- Localization keys for multi-language support
- Text variables and icon definitions

## 🔄 **Configuration File Relationships**

```
settings.json (Master)
    ↓ Specifies dutiesConfig
duties_system.json (Military Framework)
    ↓ References formations
enlisted_config.json (Formation Definitions)
    ↓ References equipment
equipment_pricing.json + troop_selection_system (Real Troop Templates)
    ↓ Used by menus
menu_config.json + progression_config.json (UI & Progression)
```

## 🛠️ **Developer Usage**

### **Loading Configuration**
```csharp
// All configs include schema versioning and validation
var settings = LoadConfig<Settings>("settings.json");
var enlisted = LoadConfig<EnlistedConfig>("enlisted_config.json");
var duties = LoadConfig<DutiesConfig>(settings.DutiesConfig); // Dynamic loading
```

### **Key Features**
- **Schema Versioning**: All files support future migrations
- **Validation**: Comprehensive error checking and safe fallbacks  
- **Localization**: Multi-language support with {=key}fallback format
- **Modularity**: Each file handles a specific concern
- **Override System**: Elite troop pricing and special configurations

## ⚙️ **Configuration Examples**

### **Formation Pricing** (equipment_pricing.json)
```json
"formation_multipliers": {
  "infantry": 1.0,      // Base cost
  "archer": 1.3,        // +30% (ranged weapons)
  "cavalry": 2.0,       // +100% (horse equipment)
  "horsearcher": 2.5    // +150% (horse + ranged premium)
}
```

### **Duty Assignment** (duties_system.json)
```json
"field_medic": {
  "min_tier": 3,
  "officer_role": "Surgeon",
  "required_formations": ["infantry", "archer"],
  "special_abilities": ["reduced_medical_cooldown", "enhanced_healing"]
}
```

### **Menu Localization** (menu_config.json)
```json
"title_key": "{=enlisted_status_title}Enlisted Status",
"disabled_tooltips": {
  "not_eligible": "{=retirement_wait}Must serve {DAYS} more days for retirement."
}
```

## 🎯 **Modding Support**

**All configuration files are designed for easy modding**:
- Adjust pricing without recompilation
- Add new duties and formations
- Modify progression requirements  
- Customize culture-specific elements
- Change UI text and localization

**Edit any JSON file and restart the campaign to see changes.**

## 🔧 **Configuration Validation & Loading**

### **Schema Versioning System**
All configuration files include version control for future compatibility:
```json
{
  "schemaVersion": 1,
  "enabled": true,
  // ... configuration content
}
```

### **Safe Configuration Loading** (Blueprint-Compliant)
```csharp
// No unverified APIs - uses relative paths only
public static class ConfigManager
{
    private static string GetConfigPath(string filename)
    {
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
            }
            
            return obj ?? new T();
        }
        catch (Exception ex)
        {
            ModLogger.Error("Config", $"Failed to load {filename}, using defaults", ex);
            return new T();
        }
    }
}
```

### **Comprehensive Validation System**
```csharp
private static void ValidateConfiguration()
{
    // XP requirements strictly ascending (prevents progression bugs)
    for (int i = 1; i < requirements.Length; i++)
    {
        if (requirements[i].XpRequired <= requirements[i-1].XpRequired)
            throw new Exception($"XP requirements not ascending at tier {i}");
    }
    
    // Formation keys standardized
    var validFormations = new[] {"infantry", "archer", "cavalry", "horsearcher"};
    
    // Officer roles valid
    var validOfficerRoles = new[] {"Engineer", "Scout", "Quartermaster", "Surgeon", null};
    
    // Culture IDs verified (empire, aserai, sturgia, vlandia, khuzait, battania)
    var validCultures = new[] {"empire", "aserai", "sturgia", "vlandia", "khuzait", "battania"};
    
    // Pricing multipliers positive (no negatives)
    // Wage multipliers in valid range (0.0-10.0)
}
```

## 📊 **Configuration Fixes Applied**

### **✅ Critical Issues Fixed**
1. **Schema Versioning**: Added to ALL 7 configuration files for future migration support
2. **File Precedence**: Removed duplicate `duties_config_enhanced.json`, specified `duties_system.json` in settings
3. **Safe Loading**: Replaced unverified `ModuleHelper` API with Blueprint-compliant relative paths
4. **Localization Format**: Restored verified `{=key}fallback` format from decompile analysis
5. **Validation Logic**: Added comprehensive configuration validation with error handling

### **✅ APIs Verified from Decompile**
- **Localization System**: `{=key}fallback` format confirmed in `MBTextManager.cs`
- **Custom Healing Model**: `PartyHealingModel` interface confirmed in `ComponentInterfaces`
- **Formation Detection**: `IsRanged && IsMounted` logic verified (SAS-compatible)

### **❌ Unverified APIs Removed**
- **ModuleHelper**: Not found in decompiled code - replaced with safe paths
- **Complex Localization**: Simplified to verified format only

---

**This configuration system provides complete control over the military service experience while maintaining game balance and mod compatibility.**
