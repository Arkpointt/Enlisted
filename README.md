# Enlisted - Military Service System

**A comprehensive military service mod for Mount & Blade II: Bannerlord**

[![Version](https://img.shields.io/badge/version-v2.1.0-blue.svg)](SubModule.xml)
[![Bannerlord](https://img.shields.io/badge/Bannerlord-1.2.x-green.svg)](https://www.taleworlds.com/)
[![.NET](https://img.shields.io/badge/.NET-Framework%204.7.2-purple.svg)](https://dotnet.microsoft.com/)

## 🎖️ Overview

**Enlisted** transforms Bannerlord's campaign experience by allowing players to enlist with any lord and serve as a professional soldier in their armies. Instead of leading your own warband from day one, you start as a lowly recruit and work your way through 7 military tiers, earning wages, gaining skills, and participating in your lord's campaigns.

This mod provides a complete alternative playstyle focused on **military service, skill development, and authentic soldiering experience** rather than immediate lordship.

## ✨ Key Features

### 🎯 **Core Military Service**
- **Join Any Lord**: Enlist through diplomatic conversation - no quests or prerequisites
- **7-Tier Career Progression**: Start as Recruit (Tier 1) and advance to Master Sergeant (Tier 7) over 1+ year of service
- **Realistic Wages**: Earn 24-150 gold/day based on rank, duties, and formation specialization
- **Army Integration**: Automatically follow your lord's army with smart pathfinding and encounter management
- **Crash Prevention**: Engine-level encounter management prevents map crashes and battle issues

### ⚔️ **Battle & Combat**
- **Real-Time Battle Participation**: Automatically detect and join your lord's battles
- **Formation-Based Commands**: Automatic command filtering based on your chosen formation (Infantry/Archer/Cavalry/Horse Archer)
- **4 Formation Specializations**: Each with culture-specific variants and appropriate equipment access
- **Army-Aware Battles**: Properly handle both individual lord battles and large army engagements

### 🎖️ **Duty & Profession System**
- **5 Daily Duties** (Available T1+): Enlisted, Forager, Sentry, Messenger, Pioneer
  - Each provides specific skill XP bonuses and role-play opportunities
  - Formation training gives 50 XP/day to primary skills, 25 XP/day to secondary skills
- **5 Specialized Professions** (Available T3+): Quartermaster's Aide, Field Medic, Siegewright's Aide, Drillmaster, Saboteur
  - Professions grant officer roles in your lord's party (Scout, Surgeon, Engineer, Quartermaster)
  - Your skills directly affect party performance (healing rates, movement speed, siege speed, carry capacity)

### 🛡️ **Equipment & Progression**
- **Master at Arms**: Select from culture-appropriate troop templates when promoted
- **Quartermaster Grid UI**: Professional equipment selection interface with individual item stats and images
- **Multiple Troop Choices**: 3-6 equipment variants per tier with realistic formation-based pricing
- **Equipment Replacement**: Military-style gear replacement (not accumulation) - realistic service progression
- **Retirement Benefits**: Keep your final service equipment permanently after 1+ year of service

### 🎮 **Professional Interface**
- **Main Status Menu** (Press 'N'): Comprehensive military information with real-time updates
- **Organized Duty Selection**: Clean DUTIES and PROFESSIONS sections with visual spacing and detailed descriptions
- **Tier-Based Access**: Professions visible early but unlockable at higher tiers to motivate progression
- **Keyboard Shortcuts**: 'N' for status menu, 'P' for promotion checks
- **Settlement Access**: Visit towns and castles while enlisted with proper encounter management

### 🌟 **Quality of Life**
- **Temporary Leave System**: Request leave with proper encounter cleanup and automatic menu restoration
- **Lord Conversations**: Talk to nearby lords with portrait selection and faction information
- **Field Medical Treatment**: Enhanced healing available anywhere (+13 HP/day bonus for enlisted soldiers)
- **Real-Time Processing**: All systems work even when the game is paused
- **Configuration-Driven**: 7 JSON files control all aspects - no hardcoded values

## 🚀 Quick Start

### Installation

#### For Players
1. Download the latest release from the [Releases](../../releases) page (or build from source)
2. Extract to your Bannerlord Modules folder: `<BannerlordInstall>\Modules\Enlisted\`
3. Ensure [Bannerlord.Harmony](https://www.nexusmods.com/mountandblade2bannerlord/mods/2006) is installed (required dependency)
4. Enable the mod in Bannerlord's launcher

#### For Developers

**Build Requirements:**
- Visual Studio 2022
- Mount & Blade II: Bannerlord installed
- Bannerlord SDK (included with game)
- Bannerlord.Harmony mod installed

**Build Steps:**
- **Visual Studio**: Open `Enlisted.sln`, select configuration "Enlisted EDITOR", then Build
- **Command Line**: `dotnet build -c "Enlisted EDITOR"`

**Output Location:**  
`<BannerlordInstall>\Modules\Enlisted\bin\Win64_Shipping_wEditor\Enlisted.dll`

> **Note**: Build warnings about locked DLL are normal when Bannerlord is running - the build still succeeds.

### Getting Started In-Game

1. **Find Any Lord**: Talk to any lord on the campaign map
2. **Start Conversation**: Select "I have something else to discuss" → "I wish to serve in your warband"
3. **Begin Service**: Accept the terms and start as a Tier 1 Recruit
4. **Choose Formation**: On your first promotion (Tier 2), select your military formation:
   - **Infantry**: Front-line melee combat
   - **Archer**: Ranged foot combat  
   - **Cavalry**: Mounted melee combat
   - **Horse Archer**: Elite mounted ranged combat
5. **Access Menu**: Press **'N'** key anytime when enlisted to open the military status menu
6. **Manage Service**: 
   - Select duties and professions for skill training and bonuses
   - Visit quartermaster for equipment upgrades
   - Request temporary leave or retirement when ready

## ⚙️ Configuration

The mod is **fully configurable** through 7 JSON files located in `ModuleData/Enlisted/`:

| File | Purpose |
|------|---------|
| `settings.json` | Master settings, debug flags, and encounter behavior |
| `enlisted_config.json` | Core military system (tiers, wages, formations, retirement) |
| `duties_system.json` | Military duties and profession definitions with officer roles |
| `progression_config.json` | Tier advancement requirements and benefits |
| `equipment_pricing.json` | Realistic formation-based equipment economics |
| `equipment_kits.json` | Equipment definitions and troop template assignments |
| `menu_config.json` | UI system with professional localization support |

**All values are customizable** - no hardcoded mechanics. See [`ModuleData/Enlisted/README.md`](ModuleData/Enlisted/README.md) for detailed configuration guide.

## 🔧 Technical Architecture

### **Modern Implementation Philosophy**

- **✅ Package-by-Feature Architecture**: Clean modular design with each feature in its own namespace
- **✅ Verified APIs Only**: All critical APIs confirmed against official Bannerlord decompiled code (v1.2.12)
- **✅ Minimal Essential Patches**: Only engine-level properties + targeted battle command filtering - no heavy patching
- **✅ Configuration-Driven**: Zero hardcoded values - everything customizable via JSON
- **✅ Real-Time State Management**: Continuous state enforcement using verified TickEvent hooks
- **✅ Crash Prevention**: Engine-level encounter management using `IsActive = false` - no workarounds needed
- **✅ Professional Logging**: Performance-friendly error logging for troubleshooting and mod conflict detection

### **Code Organization**

```
src/
├── Features/           # Feature modules (Package-by-Feature)
│   ├── Enlistment/    # Core service state and lord relationship
│   ├── Duties/        # Configuration-driven duties system
│   ├── Equipment/     # Troop selection and equipment management
│   ├── Combat/        # Battle participation and encounter handling
│   ├── Conversations/ # Dialog system integration
│   └── Interface/     # Menu system and UI
├── Mod.Core/          # Shared utilities (logging, config, context)
├── Mod.Entry/         # Module entry point and behavior registration
└── Mod.GameAdapters/  # Harmony patches (minimal, targeted)
```

**See [`docs/BLUEPRINT.md`](docs/BLUEPRINT.md) for complete architecture standards and development guidelines.**

## 📋 Requirements

### **Game Requirements**
- **Mount & Blade II: Bannerlord** version 1.2.x
- **Bannerlord.Harmony** mod (required dependency)
  - Download: [Nexus Mods](https://www.nexusmods.com/mountandblade2bannerlord/mods/2006) or [GitHub](https://github.com/Bannerlord-Modding/Bannerlord.Harmony)

### **System Requirements**
- Windows (mod uses .NET Framework 4.7.2)
- Visual Studio 2022 (for building from source)

### **Mod Compatibility**
- Compatible with most campaign mods
- Safe save/load - all data persists correctly
- Defensive programming prevents conflicts with other mods
- See logs in `Debugging\` folder if issues occur

## 📖 Documentation

### **Getting Started Guides**
- **[`docs/readme.md`](docs/readme.md)** - Complete implementation overview and feature documentation
- **[`docs/BLUEPRINT.md`](docs/BLUEPRINT.md)** - Architecture standards, development guidelines, and code organization
- **[`ModuleData/Enlisted/README.md`](ModuleData/Enlisted/README.md)** - Complete configuration system guide (all 7 JSON files)

### **Feature Documentation**
Detailed feature specs located in `docs/Features/`:
- `enlistment.md` - Core enlistment mechanics and progression
- `duties-system.md` - Military duties and profession system
- `troop-selection.md` - Master at Arms troop selection system
- `quartermaster.md` - Equipment variant selection and management
- `formation-training.md` - Skill development and XP systems
- `battle-commands.md` - Battle integration and command filtering
- `temporary-leave.md` - Leave system and encounter management
- `town-access-system.md` - Settlement access for enlisted soldiers
- `menu-interface.md` - Professional menu system and UI design
- `dialog-system.md` - Conversation flows and dialog integration

### **API Reference**
- **[`docs/discovered/engine.md`](docs/discovered/engine.md)** - Verified Bannerlord API signatures
- **[`docs/discovered/equipment.md`](docs/discovered/equipment.md)** - Equipment system APIs
- **[`docs/discovered/gauntlet.md`](docs/discovered/gauntlet.md)** - UI system APIs

### **Changelog**
See **[`docs/CHANGELOG.md`](docs/CHANGELOG.md)** for detailed version history and implementation phases.

## 🛠️ Development & Debugging

### **Logging System**
All debug outputs are written to `<BannerlordInstall>\Modules\Enlisted\Debugging\` (automatically detected on any drive):

**Session Logs** (cleared on init):
- `enlisted.log` - Bootstrap, initialization, and critical errors
- `discovery.log` - Menu opens, settlement entries, session markers
- `dialog.log` - Dialog availability and selection events
- `api.log` - API transition notes and menu switches

**Discovery Artifacts** (aggregated):
- `attributed_menus.txt` - Unique menu IDs observed
- `dialog_tokens.txt` - Unique dialog tokens discovered
- `api_surface.txt` - Reflection dump of key public surfaces

**Note**: Logging is performance-friendly - only errors and critical events by default.

### **Development Features**
- **Multi-Language Support**: Professional localization with `{=key}fallback` format
- **Enhanced Healing Model**: Custom PartyHealingModel provides +13 HP/day for enlisted soldiers
- **Schema Versioning**: Future-proof configuration with automatic migration support
- **Mod Conflict Detection**: Monitoring and logging for interference from other mods
- **Game Update Detection**: Automatic API validation to detect breaking changes

### **Current Status**

**✅ Completed Phases:**
- Phase 1: Core enlistment system and dialog integration
- Phase 2: Menu system, equipment management, and troop selection
- Phase 3: Battle integration, settlement access, and army-aware systems

**⏳ Planned Features:**
- Phase 5: Advanced military features (veteran progression, service records)
- Phase 6: Polish and optimization (animations, edge cases, performance tuning)

See [`docs/readme.md`](docs/readme.md) for complete implementation status.

## 📄 License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.

## 🤝 Contributing

Contributions are welcome! This mod follows a **Package-by-Feature** architecture with clean separation of concerns:

1. **Feature Development**: Add new features under `src/Features/` following existing patterns
2. **API Verification**: All APIs must be verified against official Bannerlord decompiled code
3. **Minimal Patching**: Use Harmony patches sparingly - prefer verified public APIs
4. **Configuration-Driven**: New mechanics should be configurable via JSON when possible
5. **Documentation**: Update relevant docs in `docs/` when adding features

**See [`docs/BLUEPRINT.md`](docs/BLUEPRINT.md) for detailed contribution guidelines and architecture standards.**

---

**Experience Bannerlord from a new perspective - start your military career as a common soldier and work your way through the ranks!** ⚔️
