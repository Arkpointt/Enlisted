# Contributing

## Build
```bash
dotnet build -c "Enlisted EDITOR"
```
Output: `<Bannerlord>/Modules/Enlisted/`

## Structure

```
src/
├── Mod.Entry/              # SubModule.cs - Harmony init
├── Mod.Core/               # Logging, config
├── Mod.GameAdapters/       # Harmony patches
└── Features/
    ├── Enlistment/         # Core state, wages, XP
    ├── Assignments/        # Duties
    ├── Equipment/          # Gear
    ├── Combat/             # Battles
    ├── Interface/          # Menus
    ├── Conversations/      # Dialog
    └── Ranks/              # Promotions
```

## Key Files

| Want to... | Look at |
|------------|---------|
| Change enlistment logic | `EnlistmentBehavior.cs` |
| Add menu options | `EnlistedMenuBehavior.cs` |
| Add a duty | `duties_system.json` |
| Change balance | `enlisted_config.json` |
| Add a patch | `src/Mod.GameAdapters/Patches/` |
| Modify dialog | `EnlistedDialogManager.cs` |

## Config Files

All in `ModuleData/Enlisted/`:
- `settings.json` - Logging, encounter settings
- `enlisted_config.json` - Tiers, wages, retirement
- `duties_system.json` - Duty definitions
- `progression_config.json` - XP settings
- `equipment_kits.json` - Loadouts
- `equipment_pricing.json` - Costs

## Debugging

Logs in `Modules/Enlisted/Debugging/`:
- `enlisted.log` - Main activity
- `conflicts.log` - Mod compatibility

## Guidelines

- Fix lint warnings, don't suppress
- Comments explain *why*, not *what*
- Use Harmony only when needed
- Keep changes small and focused
