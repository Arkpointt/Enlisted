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
- SAS behavior settings (attach ranges, encounter suppression)

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

#### **6. `equipment_kits.json`** - Gear Definitions
**Purpose**: Complete equipment sets and templates  
**Contains**:
- Pre-defined equipment kits for Empire T1-T3 (examples)
- Kit templates for all formations and cultures
- Culture equipment patterns with tier-based naming
- Fallback equipment for error handling

### **🎮 User Interface Configuration**

#### **7. `menu_config.json`** - Menu System
**Purpose**: Complete UI framework for enlisted interface  
**Contains**:
- Menu definitions (enlisted_status, enlisted_equipment, enlisted_record)
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
equipment_pricing.json + equipment_kits.json (Equipment System)
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

---

**This configuration system provides complete control over the military service experience while maintaining game balance and mod compatibility.**
