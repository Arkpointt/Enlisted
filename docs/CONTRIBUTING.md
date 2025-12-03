# Contributing

## Build
```bash
dotnet build -c "Enlisted RETAIL" /p:Platform=x64
```
Output: `<Bannerlord>/Modules/Enlisted/bin/Win64_Shipping_Client/`

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

## API Reference

When working with Bannerlord APIs, refer to the discovered documentation:
- `docs/discovered/engine.md` - Core API signatures (v1.3.4 verified)
- `docs/discovered/menus.md` - Menu system APIs
- `docs/discovered/gauntlet.md` - UI system reference
- `docs/discovered/images.md` - Image system (v1.3.4)
- `docs/discovered/equipment.md` - Equipment APIs
- `docs/discovered/helpers.md` - Helper methods

All discovered docs are updated for Bannerlord v1.3.4 compatibility.

## Guidelines

- Fix lint warnings, don't suppress
- Comments explain *why*, not *what*
- Use Harmony only when needed
- Keep changes small and focused
- Verify APIs against v1.3.4 decompile in `C:\Dev\Enlisted\DECOMPILE\`
