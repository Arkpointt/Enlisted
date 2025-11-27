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
- Encounter behavior settings (attach ranges, IsActive management for encounter prevention)

#### **2. `enlisted_config.json`** - Core Military System
**Purpose**: Base enlistment mechanics, progression, and retirement  
**Contains**:
- Tier progression XP requirements (T1-T6)
- Wage calculation formulas and multipliers
- Formation definitions with culture-specific names
- **Retirement system**: First term (252 days), renewal terms (84 days), cooldowns (42 days)
- **Grace period settings**: 14 days for various service interruptions
- **Finance settings** for tooltip display and wage formula configuration
- **Gameplay settings** for combat reserves and desertion mechanics

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
**Purpose**: Tier progression, XP sources, and wage calculation  
**Contains**:
- Tier XP requirements (100, 300, 600, 1000, 2000 for T2-T6)
- **XP sources**: daily service (25), battle participation (25), duty performance (15)
- Wage calculation with base wages and tier multipliers

### **🛡️ Equipment System Configuration**

#### **5. `equipment_pricing.json`** - Economic System
**Purpose**: Realistic military equipment economics  
**Contains**:
- Formation-based pricing multipliers (Infantry 1.0× → Horse Archer 2.5×)
- Culture economic modifiers for all 8 cultures (Khuzait 0.8× → Vlandia 1.2×)
- Nord and Darshi cultures (added in Bannerlord 1.3.4)

#### **6. `equipment_kits.json`** - Troop Selection System
**Purpose**: Real troop templates for equipment extraction  
**Contains**:
- Troop selection system configuration
- Promotion notification settings  
- Formation detection rules
- Equipment is extracted from `CharacterObject.BattleEquipments`

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
enlisted_config.json (Formation Definitions + Retirement)
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
var retirement = EnlistedConfig.LoadRetirementConfig(); // Retirement-specific
```

### **Key Features**
- **Schema Versioning**: All files support future migrations
- **Validation**: Comprehensive error checking and safe fallbacks  
- **Localization**: Multi-language support with {=key}fallback format
- **Modularity**: Each file handles a specific concern

## ⚙️ **Configuration Examples**

### **Retirement System** (enlisted_config.json)
```json
"retirement": {
  "first_term_days": 252,
  "renewal_term_days": 84,
  "cooldown_days": 42,
  "first_term_gold": 10000,
  "first_term_reenlist_bonus": 20000,
  "renewal_discharge_gold": 5000,
  "renewal_continue_bonus": 5000,
  "lord_relation_bonus": 30,
  "faction_reputation_bonus": 30,
  "other_lords_relation_bonus": 15,
  "other_lords_min_relation": 50
}
```
- `first_term_days`: Service required for first retirement (252 = 3 game years)
- `renewal_term_days`: Duration of re-enlistment terms (84 = 1 game year)
- `cooldown_days`: Time before veteran can re-enlist (42 = 6 months)
- `first_term_gold`: Gold bonus for first retirement
- `first_term_reenlist_bonus`: Gold bonus for extending after first term
- `renewal_discharge_gold`: Gold when leaving after renewal term
- Relation bonuses for lord, faction, and other lords

### **XP Sources** (progression_config.json)
```json
"xp_sources": {
  "daily_service_xp": 25,
  "battle_participation_xp": 25,
  "duty_performance_xp": 15,
  "kill_xp_per_kill": 1
}
```
Note: Kill XP tracking is currently disabled due to technical limitations.

### **Grace Period** (enlisted_config.json)
```json
"gameplay": {
  "reserve_troop_threshold": 100,
  "desertion_grace_period_days": 14,
  "leave_max_days": 14
}
```
- `reserve_troop_threshold`: Minimum troops to enable "Wait in Reserve" (20-500)
- `desertion_grace_period_days`: Days to find new lord after discharge (1-90)
- `leave_max_days`: Maximum days on leave before desertion penalties

### **Formation Pricing** (equipment_pricing.json)
```json
"formation_multipliers": {
  "infantry": 1.0,
  "archer": 1.3,
  "cavalry": 2.0,
  "horsearcher": 2.5
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

### **Finance Configuration** (enlisted_config.json)
```json
"finance": {
  "show_in_clan_tooltip": true,
  "tooltip_label": "{=enlisted_wage_income}Enlistment Wages",
  "wage_formula": {
    "base_wage": 10,
    "level_multiplier": 1,
    "tier_multiplier": 5,
    "xp_divisor": 200,
    "army_bonus_multiplier": 1.2
  }
}
```
- `show_in_clan_tooltip`: Display wages in native Daily Gold Change tooltip
- `wage_formula`: Customize calculation: `(base + level×level_mult + tier×tier_mult + xp/xp_div) × army_bonus`

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
- Customize retirement bonuses and timers
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
    
    // Culture IDs verified
    var validCultures = new[] {"empire", "aserai", "sturgia", "vlandia", "khuzait", "battania", "nord", "darshi"};
    
    // Retirement days positive
    // Grace period in valid range (1-90)
}
```

## 📊 **Version Info**

**Mod Version**: 0.4.0  
**Compatible with**: Bannerlord 1.3.5  
**Supported Cultures**: empire, aserai, sturgia, vlandia, khuzait, battania, nord, darshi

## 📝 **Recent Changes (v0.4.0)**

- Added comprehensive veteran retirement system with configurable bonuses
- Unified grace period system (lord killed/captured, army defeated, leave expired)
- Battle participation XP (+25 per battle)
- Leave system with 14-day timer and desertion penalties
- Per-faction veteran record tracking
- Re-enlistment with tier preservation

---

This configuration system provides complete control over the military service experience while maintaining game balance and mod compatibility.
