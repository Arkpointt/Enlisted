# Contributing to Enlisted

## Quick Start

1. Clone the repo
2. Open `Enlisted.sln` in Visual Studio
3. Build: `dotnet build -c "Enlisted EDITOR"` (or `"Enlisted RETAIL"` for release)
4. Output goes to `<Bannerlord>/Modules/Enlisted/`

## Codebase Map

```
src/
├── Mod.Entry/                    # Entry point
│   └── SubModule.cs              # Harmony init, behavior registration
│
├── Mod.Core/                     # Shared utilities
│   ├── Config/                   # ModSettings, ModConfig
│   └── Logging/                  # ModLogger, conflict diagnostics
│
├── Mod.GameAdapters/             # Game integration layer
│   └── Patches/                  # All Harmony patches
│
└── Features/                     # Main functionality (package-by-feature)
    ├── Enlistment/               # Core service state
    │   └── EnlistmentBehavior    # Lord attachment, wages, XP, tiers
    ├── Assignments/              # Duty system
    │   └── EnlistedDutiesBehavior
    ├── Equipment/                # Gear management
    │   └── QuartermasterManager, TroopSelectionManager
    ├── Combat/                   # Battle handling
    │   └── EnlistedEncounterBehavior
    ├── Interface/                # Menus and input
    │   └── EnlistedMenuBehavior
    ├── Conversations/            # Dialog system
    │   └── EnlistedDialogManager
    └── Ranks/                    # Promotion logic
        └── PromotionBehavior
```

## Key Entry Points

| Want to... | Look at |
|------------|---------|
| Change enlistment logic | `EnlistmentBehavior.cs` |
| Add/modify menu options | `EnlistedMenuBehavior.cs` |
| Add a new duty | `EnlistedDutiesBehavior.cs` + `duties_system.json` |
| Change tier/wage balance | `enlisted_config.json`, `progression_config.json` |
| Add a Harmony patch | `src/Mod.GameAdapters/Patches/` |
| Modify dialog options | `EnlistedDialogManager.cs` |

## Config Files

All in `ModuleData/Enlisted/`:

| File | Purpose |
|------|---------|
| `enlisted_config.json` | Tier requirements, wages, tier names |
| `duties_system.json` | Duty definitions and slots per tier |
| `progression_config.json` | XP and progression settings |
| `equipment_kits.json` | Equipment loadouts by culture/tier |
| `equipment_pricing.json` | Equipment costs |
| `menu_config.json` | Menu text and options |
| `settings.json` | General mod settings |

## Harmony Patches

All patches live in `src/Mod.GameAdapters/Patches/`. Each patch has a single responsibility:

| Patch | Purpose |
|-------|---------|
| `BattleCommandsFilterPatch` | Formation-based command filtering |
| `ClanFinanceEnlistmentIncomePatch` | Adds wages to daily gold tooltip |
| `DischargePenaltySuppressionPatch` | Prevents relation penalties on discharge |
| `DutiesOfficerRolePatches` | Officer role integration |
| `EncounterSuppressionPatch` | Suppresses encounters with lord |
| `EnlistedWaitingPatch` | Prevents game pausing when lord enters battle |
| `EnlistmentExpenseIsolationPatch` | Prevents expense sharing with lord |
| `HidePartyNamePlatePatch` | Hides party nameplate while enlisted |
| `InfluenceMessageSuppressionPatch` | Suppresses "gained 0 influence" messages |
| `KingdomDecisionParticipationPatch` | Blocks kingdom decision prompts |
| `LootBlockPatch` | Blocks loot access for enlisted soldiers |
| `MercenaryIncomeSuppressionPatch` | Suppresses mercenary income display |
| `OrderOfBattleSuppressionPatch` | Skips deployment screen for enlisted |
| `SkillSuppressionPatch` | Blocks tactics/leadership XP during battles |
| `StarvationSuppressionPatch` | Prevents starvation while enlisted |
| `TownLeaveButtonPatch` | Hides native Leave button in settlements |
| `VisibilityEnforcementPatch` | Controls party visibility during service |

## Guidelines

- **Code style**: Follow `.editorconfig`, fix lint warnings (don't suppress them)
- **Comments**: Write naturally, explain *why* not *what*
- **Patches**: Only use Harmony when public API isn't available
- **Logging**: Keep it lightweight, errors only in production paths
- **PRs**: Small, focused changes preferred

## Debugging

Logs output to `Modules/Enlisted/Debugging/`:

| Log | Purpose |
|-----|---------|
| `enlisted.log` | Main mod activity and errors |
| `conflicts.log` | Mod compatibility diagnostics |
| `dialog.log` | Conversation system events |
| `discovery.log` | Troop/equipment discovery |
| `api.log` | API interaction logging |

Logs are cleared each session.

## Architecture Details

See `docs/BLUEPRINT.md` for full architecture documentation, including:
- Guiding principles
- Harmony patching policy
- Platform constraints
- Critical patterns (encounter prevention, deferred operations)

