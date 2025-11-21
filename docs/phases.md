# Implementation Overview

Last updated: 2025-01-26

## What We Built

Military service system for Bannerlord where you can enlist with lords and progress through ranks.

**Core features:**
- Enlist with any lord by talking to them
- Get promoted through 7 tiers over a year of service  
- Choose real Bannerlord troops and get their equipment
- Military duties system with roles like Quartermaster, Scout, Field Medic
- Grid UI for individual equipment selection
- Works on 4K and different resolutions
- No crashes or game freezing

## Implementation Timeline

### Phase 1: Core Systems ✅ Complete
- **Dialog System**: Centralized conversation management in `EnlistedDialogManager.cs`  
- **Enlistment**: Join lords, follow their armies, handle edge cases safely
- **Duties System**: JSON-configured military roles with real benefits

### Phase 2: Equipment & UI ✅ Complete  
- **Troop Selection**: Pick real troops, get their equipment on promotion with close button support
- **Quartermaster Grid UI**: Individual equipment clicking with images and stats
- **Enhanced Menu System**: Professional duty/profession selection with organized sections, detailed descriptions
- **Battle Commands**: Automatic formation-based command filtering with audio cues
- **Formation Training**: Daily skill XP based on military specialization (Infantry, Cavalry, Archer, Horse Archer)
- **Menu Interface Refinements**: Clean section headers, tier-based profession access, streamlined navigation

## Major Breakthrough: Grid UI

**Date**: 2025-09-06

We figured out how to create working Gauntlet grid UIs for Bannerlord:
- Templates go in `GUI/Prefabs/{FeatureName}/` (not custom paths)
- Need `TaleWorlds.PlayerServices.dll` for equipment images
- Register hotkeys BEFORE input restrictions or game freezes  
- Use `<Widget>` not `<Panel>` (Panel is deprecated)
- Use center alignment for 4K support, not fixed margins

**Result**: Working equipment selection with individual clickable cards in a grid layout.

## Major Enhancement: Formation Training System

**Date**: 2025-09-08

We implemented a formation-based skill training system that provides automatic daily skill progression:
- **JSON configuration** for all formation skill XP amounts (easy balancing)
- **Modern Bannerlord API** using `Hero.MainHero.AddSkillXp(skill, amount)`
- **Formation detection** based on chosen troop type, not equipment analysis
- **Leave integration** - training continues even during temporary leave
- **Immersive descriptions** explaining why each skill is trained
- **Build process fix** ensuring `duties_system.json` copies to game folder

**Result**: Authentic military specialization with daily skill progression that feels natural and balanced.

## Key Technical Decisions

**Real Troops vs Equipment Kits**: Chose real troops because it's more immersive and uses existing game data.

**Encounter Safety**: Use `IsActive = false` instead of complex patches - simpler and more reliable.

**Real-Time Processing**: Use `TickEvent` not `HourlyTickEvent` so military service works even when game is paused.

**Centralized Dialogs**: One dialog manager instead of scattered conversations prevents conflicts.

**JSON Configuration**: Military duties and formation training are configurable without recompiling the mod.

**Formation Training**: Use Hero.AddSkillXp API (not HeroDeveloper) for reliable skill progression that works with the native game systems.

## Future Development Roadmap

### Phase 3A: Enhanced Battle Integration ✅ COMPLETE
**Goal**: Better battle participation and army coordination

**Implemented features:**
- ✅ Real-time battle detection and participation
- ✅ Army-aware battle logic (individual vs army battles)
- ✅ Battle encounter menu options ("Wait in Reserve", etc.)

**Technical approach:**
- ✅ Real-time lordParty.MapEvent monitoring for battle detection
- ✅ Proper IsActive toggling for encounter menu triggering  
- ✅ Army hierarchy awareness without complex pathfinding

### Phase 3B: Town Access System ✅ COMPLETE
**Goal**: Full settlement exploration for enlisted soldiers

**Implemented features:**
- ✅ Synthetic outside encounter creation for invisible parties
- ✅ Dynamic settlement access (towns and castles)
- ✅ Complete encounter state management and cleanup

**Technical approach:**
- ✅ AI-guided synthetic encounter approach using StartSettlementEncounter
- ✅ town_outside/castle_outside as safe entry points
- ✅ Comprehensive state tracking and restoration

### Phase 4: Extended Equipment System  
**Goal**: Complete equipment management across all slots

**Planned features:**
- Helmet Quartermaster (extend grid UI to helmets)
- Armor Quartermaster (body armor, gloves, boots)
- Mount Quartermaster (horses and horse armor)
- Enhanced tooltips with detailed stat comparisons

**Technical approach:**
- Reuse existing Quartermaster UI patterns
- Extend `QuartermasterManager` for additional equipment slots
- Copy template structure for helmet/armor variants

### Phase 5: Advanced Military Features
**Goal**: Veteran progression and special systems

**Planned features:**
- Retirement benefits and equipment keeping
- Veteran status with special privileges  
- Military events and random encounters
- Advanced progression tracking and service records
- Optional vassalage offers for distinguished veterans

**Technical approach:**
- Extend existing progression system
- Add veteran-specific dialogs and benefits
- Service history tracking and display

### Phase 6: Polish and Quality of Life
**Goal**: Enhanced user experience and edge case handling

**Planned features:**
- Save/load testing and edge case fixes
- Enhanced UI animations and feedback
- Comprehensive error handling
- Performance optimization
- Additional configuration options

## Current Status

**Complete**: Full military service system with comprehensive menu interface, battle integration, and town access is working and ready for players.

**Latest Enhancement**: Town Access System (Phase 3B) - synthetic outside encounter solution enabling full settlement exploration without crashes.

**Major Breakthroughs**:
- **Phase 3A**: Army-aware battle participation with real-time detection
- **Phase 3B**: AI-guided town access solution solving fundamental encounter type issues

**Next recommended phase**: Phase 4 (Extended Equipment System) - builds on the complete military service foundation.

See `docs/Features/` for detailed feature specifications and implementation guidance.
