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

## Adding New Files

**CRITICAL**: This project uses an old-style `.csproj` with explicit file includes. New `.cs` files are NOT automatically compiled.

When adding a new source file:
1. Create the `.cs` file in the appropriate location
2. **Manually add it to `Enlisted.csproj`** in the `<ItemGroup>` with `<Compile Include="..."/>` entries
3. Build and verify the file is included

Example - adding a new patch:
```xml
<Compile Include="src\Mod.GameAdapters\Patches\YourNewPatch.cs"/>
```

If you forget this step, the file will exist but won't be compiled, and your code won't run.

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

### Mod Logs (Enlisted)

Full path: `<BannerlordInstall>\Modules\Enlisted\Debugging\`

Example: `C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\Enlisted\Debugging\`

Files:
- `enlisted.log` - Main activity log with category-based levels
- `conflicts.log` - Comprehensive mod conflict diagnostics:
  - Detects Harmony patch conflicts (other mods patching same methods)
  - Shows patch execution order and priorities
  - Lists all registered campaign behaviors
  - Environment info (game version, mod version, OS, runtime)
  - Loaded modules enumeration
  - Categorized patch list (Army/Party, Encounter, Finance, UI/Menu, Combat, etc.)
  - Tracks both main and deferred Harmony instances

### Game Crash Logs (Bannerlord)

Full path: `C:\ProgramData\Mount and Blade II Bannerlord\crashes\`

Each crash creates a timestamped folder (e.g., `2025-12-08_03.41.58\`) containing:
- `crash_tags.txt` - Module versions and system info
- `rgl_log_*.txt` - Engine logs (large, check near end for crash context)
- `rgl_log_errors_*.txt` - Engine error summary
- `dump.dmp` - Memory dump for debugging
- `module_list.txt` - Active modules at crash time

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
