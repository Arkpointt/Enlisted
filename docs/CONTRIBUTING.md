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
- Session logs rotate across three files:
  - `Session-A_{yyyy-MM-dd_HH-mm-ss}.log` (newest)
  - `Session-B_{...}.log`
  - `Session-C_{...}.log` (oldest kept)
- Conflicts logs rotate similarly:
  - `Conflicts-A_{yyyy-MM-dd_HH-mm-ss}.log` (newest)
  - `Conflicts-B_{...}.log`
  - `Conflicts-C_{...}.log` (oldest kept)
- `Current_Session_README.txt` summarizes Session/Conflicts A/B/C and how to share logs.
- `enlisted.log` - Legacy name (redirected to session rotation); main activity log with category-based levels
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

When working with Bannerlord APIs, verify usage against:
- The local native decompile included with this repository (authoritative)
- The official API docs for the project's target version (currently **v1.3.12+**) when needed for quick lookups

### Native Decompile Location

The decompiled Bannerlord source is located at:
```
C:\Dev\Enlisted\Decompile\
```

Key decompiled assemblies:
| Assembly | Location | Contents |
|----------|----------|----------|
| TaleWorlds.CampaignSystem | `TaleWorlds.CampaignSystem\TaleWorlds\` | Party, Settlement, Campaign behaviors |
| TaleWorlds.Core | `TaleWorlds.Core\` | Basic types, CharacterObject, ItemObject |
| TaleWorlds.Library | `TaleWorlds.Library\` | Vec2, MBList, utility classes |
| TaleWorlds.MountAndBlade | `TaleWorlds.MountAndBlade\TaleWorlds\` | Mission, Agent, combat |
| SandBox.View | `SandBox.View\` | Menu views, map handlers |
| NavalDLC | `NavalDLC\` | War Sails expansion APIs |

**Example**: To find the correct property for party position:
```
C:\Dev\Enlisted\Decompile 1.3.4\TaleWorlds.CampaignSystem\TaleWorlds\CampaignSystem\Party\MobileParty.cs
```
Key APIs:
- `MobileParty.GetPosition2D` -> `Vec2` (not `Position2D`)
- `Settlement.GetPosition2D` -> `Vec2`
- `MobileParty.Position` -> `CampaignVec2`

## Guidelines

- **ReSharper is the linter**: follow ReSharper warnings/recommendations (Rider/VS). Fix issues; do not “paper over” them.
- **No blanket suppression**: don’t disable warnings with pragmas or wide suppressions unless we have a very specific compatibility reason and a comment explaining why.
- **XML for UI + localization**:
  - **Gauntlet UI layout** lives in XML prefabs under `GUI/Prefabs/**.xml` (and shipped to `Modules/Enlisted/GUI/Prefabs/**.xml`).
  - **Localized strings** live in `ModuleData/Languages/enlisted_strings.xml`.
  - In code, prefer `TextObject("{=some_key}Fallback text")` and add the same key to `enlisted_strings.xml` (even if English fallback is used today).
  - **Gameplay/config data** remains JSON (`ModuleData/Enlisted/*.json`) unless there is a specific reason to use XML.
- Fix lint warnings, don't suppress
- Comments explain *why*, not *what*
- Use Harmony only when needed
- Keep changes small and focused
- Verify APIs against the official docs and your local decompile/reference project (path is developer-specific)
