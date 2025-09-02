# Project File Structure

Updated structure following our human-friendly blueprint design

## Current Structure

```
Enlisted/
├── docs/                           # Documentation and implementation guides
│   ├── phased-implementation.md    # Complete implementation plan
│   ├── BLUEPRINT.md               # Development standards and architecture
│   ├── README-IMPLEMENTATION.md   # Project overview and getting started
│   ├── discovered/                # API research and reference materials
│   └── sas/                      # Equipment and gear system documentation
│
├── src/
│   ├── Features/                  # Each feature is self-contained
│   │   ├── Enlistment/
│   │   │   ├── Core/              # Basic rules and validation
│   │   │   └── Behaviors/         # Main enlistment logic and state
│   │   │       ├── EnlistmentBehavior.cs
│   │   │       └── EncounterGuard.cs
│   │   ├── Assignments/
│   │   │   ├── Core/              # Assignment rules and XP calculations
│   │   │   └── Behaviors/         # Daily assignment processing
│   │   ├── Equipment/
│   │   │   ├── Core/              # Gear rules and tier requirements
│   │   │   ├── Behaviors/         # Equipment management and selection
│   │   │   └── UI/                # Custom gear selector (if needed)
│   │   ├── Ranks/
│   │   │   ├── Core/              # Promotion rules and tier logic
│   │   │   └── Behaviors/         # Rank tracking and wage calculation
│   │   ├── Conversations/
│   │   │   └── Behaviors/         # Dialog handling and flows
│   │   │       └── LordDialogBehavior.cs
│   │   ├── Combat/
│   │   │   └── Behaviors/         # Battle participation and army following
│   │   └── Interface/
│   │       └── Behaviors/         # Status menus and player interface
│   │
│   ├── Mod.Core/                  # Shared services, logging, config
│   ├── Mod.Entry/                 # Module entry point and wiring
│   └── Mod.GameAdapters/          # TaleWorlds APIs and Harmony patches
│       └── Patches/
│
├── ModuleData/                    # Configuration and settings
└── Properties/                    # Assembly information
```

## Ready for Implementation

### Phase 1A: Core Dialog (Week 1)
- Update `src/Features/Conversations/Behaviors/LordDialogBehavior.cs`
- Migrate to `lord_talk_speak_diplomacy_2` submenu

### Phase 1B: State Management (Week 1)  
- Enhance `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`
- Add comprehensive state tracking and daily processing

### Phase 1C: Assignment Framework (Week 1)
- Create `src/Features/Assignments/Core/ServiceTypes.cs`
- Create `src/Features/Assignments/Behaviors/DutyBehavior.cs`

### Phase 2: Equipment & Progression (Weeks 2-3)
- Create `src/Features/Equipment/Behaviors/GearManager.cs`
- Create `src/Features/Ranks/Behaviors/PromotionBehavior.cs`

### Phase 3: Army & Battle Integration (Weeks 4-5)
- Create `src/Features/Combat/Behaviors/BattleFollower.cs`

### Phase 4: Custom Menu System (Week 6)
- Create `src/Features/Interface/Behaviors/StatusMenu.cs`

## Key Changes Made

### Removed Components
- ❌ **Debugging folder** - Will be created by AI during implementation
- ❌ **Old Application folders** - Replaced with human-friendly Behaviors
- ❌ **Test folders** - Simplified structure focusing on main implementation

### Updated Namespaces
- ✅ **Enlisted.Features.Conversations.Behaviors** (was LordDialog.Application)
- ✅ **Enlisted.Features.Enlistment.Behaviors** (was Enlistment.Application)
- ✅ **Cross-references updated** in imports and SubModule

### Human-Friendly Names
- ✅ **Behaviors/** instead of Application/ (what the code actually does)
- ✅ **Core/** instead of Domain/ (simpler, clearer)
- ✅ **Assignments/** instead of Assignment/ (plural feels more natural)
- ✅ **Conversations/** instead of LordDialog/ (broader scope)
- ✅ **Combat/** instead of Battle/ (more descriptive)
- ✅ **Interface/** instead of Menu/ (clearer purpose)

## Ready for AI Implementation

The project structure is now clean, organized, and ready for the AI to begin Phase 1A implementation tomorrow. All debugging components have been removed and will be created fresh during the implementation phases with proper logging integration.
