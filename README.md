# Enlisted - Military Service System

**A comprehensive military service mod for Mount & Blade II: Bannerlord**

Bannerlord mod (1.2.x) targeting .NET Framework 4.7.2.

## 🎖️ Overview

Enlisted allows players to **enlist with any lord** and serve in their armies as professional soldiers. Experience authentic military life with:

- **4-Formation Specializations**: Infantry, Archer, Cavalry, Horse Archer (auto-detected from equipment)
- **9+ Military Duties**: From Runner to Field Medic to Pathfinder with real benefits
- **Realistic Equipment System**: Multiple troop choices per tier with formation-based pricing  
- **Officer Role Integration**: Become effective party Engineer, Scout, Quartermaster, or Surgeon
- **Enhanced Healing**: Custom healing model provides medical bonuses for enlisted soldiers
- **Professional Interface**: Complete menu system with equipment and duty management
- **1-Year Service Commitment**: Earn veteran status and retirement benefits

## 🚀 Quick Start

### Build & Install
- **Visual Studio**: Open `Enlisted.sln`, configuration "Enlisted EDITOR", Build
- **CLI**: `dotnet build -c "Enlisted EDITOR"`

**Output DLL**: 
`C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\Enlisted\bin\Win64_Shipping_wEditor\Enlisted.dll`

### In-Game Usage
1. **Talk to any lord** → "I have something else to discuss" → "I wish to serve in your warband"
2. **Choose your formation** on first promotion (Tier 2)
3. **Access enlisted menu** by pressing 'N' key when enlisted  
4. **Manage duties and equipment** through the comprehensive military interface

## ⚙️ Configuration

**7 JSON configuration files** in `ModuleData/Enlisted/` control all system behavior:
- Complete equipment pricing and multiple troop choices
- Duties system with formation specializations  
- Professional localization with multi-language support
- See `ModuleData/Enlisted/README.md` for detailed configuration guide

## 🔧 Technical Architecture

### **Modern Implementation**
- **Package-by-Feature**: Clean modular design with Blueprint compliance
- **Verified APIs**: Uses only decompile-verified Bannerlord APIs  
- **Minimal Patches**: 4-5 targeted Harmony patches (vs. original SAS's 37+)
- **Enhanced Healing**: Custom PartyHealingModel integration
- **Configuration-Driven**: No hardcoded values, full JSON customization

## Debugging layout and outputs
All logs and discovery artifacts are written to the module’s Debugging folder (one session at a time; cleared on init):
`C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\Enlisted\Debugging` [[memory:7845841]]

Files created per session:
- `enlisted.log` – bootstrap and general info
- `discovery.log` – menu opens, settlement entries, session markers
- `dialog.log` – dialog availability and selections (CampaignGameStarter + DialogFlow hooks)
- `api.log` – API transition notes (menu switches etc.)
- `attributed_menus.txt` – unique menu ids observed (aggregated)
- `dialog_tokens.txt` – unique dialog tokens observed (aggregated)
- `api_surface.txt` – reflection dump of key public surfaces

## 📖 Documentation

### **Getting Started**
- **`docs/README-IMPLEMENTATION.md`** - Complete implementation guide and overview
- **`docs/BLUEPRINT.md`** - Architecture standards and development guidelines  
- **`docs/phased-implementation.md`** - Detailed phase-by-phase implementation plan
- **`ModuleData/Enlisted/README.md`** - Configuration files guide (7 JSON files)

### **API Reference**
- **`docs/discovered/engine-signatures.md`** - Verified Bannerlord APIs (enhanced with decompile analysis)
- **`docs/API-VERIFICATION-RESULTS.md`** - Decompile verification results and restored APIs

### **Configuration System**
**7 JSON files** provide complete system customization:
- `settings.json` - Master settings with debug flags and duties file precedence
- `enlisted_config.json` - Core military system configuration
- `duties_system.json` - Military duties and officer roles
- `equipment_pricing.json` - Realistic military economics
- `equipment_kits.json` - Equipment definitions and templates
- `progression_config.json` - Advancement and tier progression
- `menu_config.json` - UI system with localization support

## 🛠️ Development Features

### **Enhanced Capabilities**
- **Professional Localization**: Multi-language support with {=key}fallback format
- **Custom Healing Model**: Enhanced healing bonuses for enlisted soldiers (+13 HP/day)
- **4-Formation System**: Complete specialization system with auto-detection
- **Realistic Economics**: Formation-based equipment pricing (Infantry 75🪙 → Horse Archer 1,969🪙)
- **Schema Versioning**: Future-proof configuration with migration support

### **Debugging & Discovery**
Development logging in `Modules/Enlisted/Debugging/` folder:
- `enlisted.log` – bootstrap and general info
- `discovery.log` – menu opens, settlement entries, session markers  
- `dialog.log` – dialog availability and selections
- `api.log` – API transition notes
- `api_surface.txt` – reflection dump of key public surfaces

**See `docs/CONFIG-VALIDATION.md` for safe configuration loading patterns.**
