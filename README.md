# Enlisted - Military Service System

**A comprehensive military service mod for Mount & Blade II: Bannerlord**

Bannerlord mod (1.2.x) targeting .NET Framework 4.7.2.

## 🎖️ Overview

Enlisted allows players to **enlist with any lord** and serve in their armies as professional soldiers. Experience authentic military life with a complete, professional military service system.

## ✅ **Current Features (Ready for Players)**

### **Core Military Service**
- **Enlistment System**: Join any lord through diplomatic conversation
- **7-Tier Progression**: Military ranks from Recruit to Master Sergeant over 1+ year service
- **Daily Wages**: 24-150 gold/day progression based on tier and duties
- **Army Integration**: Smart following behavior with army hierarchy awareness
- **Encounter Safety**: Complete crash prevention with engine-level encounter management

### **Professional Menu Interface** 
- **Main Status Menu**: Comprehensive military information with real-time updates
- **Organized Duty Selection**: Clean DUTIES and PROFESSIONS sections with visual spacing
- **Detailed Descriptions**: Full explanations of each military role at top of selection menu
- **Tier-Based Access**: Professions visible at T1, selectable at T3 with progression motivation
- **Clean Navigation**: Streamlined menu order, close buttons, professional presentation

### **Duty & Profession System**
- **5 Daily Duties**: Enlisted (+4 XP non-formation skills), Forager, Sentry, Messenger, Pioneer (available T1+)
- **5 Specialized Professions**: Quartermaster's Aide, Field Medic, Siegewright's Aide, Drillmaster, Saboteur (available T3+)
- **Connected XP Processing**: Menu selections properly activate daily skill training
- **Formation Training**: 50 XP/day to primary skills, 25 XP/day to secondary skills
- **Officer Role Integration**: Professions provide effective party positions with natural skill benefits

### **Equipment & Progression**
- **Master at Arms**: Select from culture-appropriate troops with portraits and close button
- **Equipment Replacement**: Military-style gear replacement (not accumulation)
- **Quartermaster Grid UI**: Individual equipment selection with images and stats
- **Multiple Troop Choices**: 3-6 options per tier with realistic formation-based pricing
- **Retirement Benefits**: Keep final equipment after 1+ year service

### **Battle Integration**
- **Automatic Command Filtering**: Formation-based battle commands with audio cues
- **Army Following**: Smart escort AI with pathfinding safety
- **Battle Participation**: Real-time battle detection and participation
- **Formation Specialization**: 4 formations (Infantry, Archer, Cavalry, Horse Archer) with culture variants

### **Quality of Life**
- **Keyboard Shortcuts**: 'N' for status menu, 'P' for promotions
- **Temporary Leave**: Request leave with proper encounter cleanup
- **Lord Conversations**: Talk to nearby lords with portrait selection
- **Field Medical Treatment**: Healing available anywhere
- **Real-Time Processing**: Works even when game is paused

## 🚀 Quick Start

### Build & Install
- **Visual Studio**: Open `Enlisted.sln`, configuration "Enlisted EDITOR", Build
- **CLI**: `dotnet build -c "Enlisted EDITOR"`

**Output DLL**: 
`<BannerlordInstall>\Modules\Enlisted\bin\Win64_Shipping_wEditor\Enlisted.dll`

**Note**: Build warnings about locked DLL are normal when Bannerlord is running - the build still succeeds.

### In-Game Usage
1. **Talk to any lord** → "I have something else to discuss" → "I wish to serve in your warband"
2. **Choose your formation** on first promotion (Tier 2)
3. **Access enlisted menu** by pressing 'N' key when enlisted  
4. **Manage duties and equipment** through the comprehensive military interface with organized sections
5. **Select duties and professions** with detailed descriptions and tier-based progression
6. **Use Master at Arms** for troop selection with close button support

## ⚙️ Configuration

**7 JSON configuration files** in `ModuleData/Enlisted/` control all system behavior:
- Complete equipment pricing and multiple troop choices
- Duties system with formation specializations  
- Professional localization with multi-language support
- See `ModuleData/Enlisted/README.md` for detailed configuration guide

## 🔧 Technical Architecture

### **Modern Implementation - Verified APIs and Minimal Patches**
- **Package-by-Feature**: Clean modular design with Blueprint compliance
- **Verified APIs**: All critical APIs confirmed in current Bannerlord version
- **Minimal Essential Patches**: Engine properties + targeted battle command filtering for clean implementation  
- **Real-Time Management**: Continuous state enforcement using verified TickEvent
- **Enhanced Healing**: Custom PartyHealingModel integration
- **Configuration-Driven**: No hardcoded values, full JSON customization
- **Professional UI**: Clean menu formatting with organized sections, tier-based progression, detailed descriptions

## Debugging layout and outputs
All logs and discovery artifacts are written to the module’s Debugging folder (one session at a time; cleared on init):
`<BannerlordInstall>\Modules\Enlisted\Debugging\` [[memory:7845841]]

**Note**: Works on any drive (C:, D:, E:, etc.) - automatically detects your Bannerlord installation location

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
- **Engine-Level Encounter Prevention**: Uses `IsActive = false` for complete encounter control (no patches needed)
- **Realistic Economics**: Formation-based equipment pricing (Infantry 75🪙 → Horse Archer 1,969🪙)
- **Schema Versioning**: Future-proof configuration with migration support

### **Debugging & Discovery**  
Development logging in `<BannerlordInstall>\Modules\Enlisted\Debugging\` folder:
- `enlisted.log` – bootstrap and general info
- `discovery.log` – menu opens, settlement entries, session markers  
- `dialog.log` – dialog availability and selections
- `api.log` – API transition notes
- `api_surface.txt` – reflection dump of key public surfaces

**Note**: Works on any drive where Bannerlord is installed (C:, D:, E:, etc.)

**See `docs/CONFIG-VALIDATION.md` for safe configuration loading patterns.**
