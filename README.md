# Enlisted

Bannerlord mod (1.2.x) targeting .NET Framework 4.7.2.

## Build
- Visual Studio: open `Enlisted.sln`, configuration "Enlisted EDITOR", Build
- CLI: `dotnet build -c "Enlisted EDITOR"`

Output DLL:
`C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\Enlisted\bin\Win64_Shipping_wEditor\Enlisted.dll`

## Module entry & behaviors
- `src/Mod.Entry/SubModule.cs` wires:
  - `DiscoveryBehavior` (under `src/Debugging/Discovery/`) for runtime discovery logging
  - `LordDialogBehavior` for our conversation entries
  - `ApiDiscoveryBehavior` to dump API surface on session launch

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

## Settings (ModuleData/Enlisted/settings.json)
Key flags:
- `LogMenus`, `LogDialogs`, `LogCampaignEvents`
- `DiscoveryStackTraces` – include stack traces on registration logs
- `DiscoveryPlayerOnly` – filter to player-driven contexts
- `LogApiCalls`, `ApiCallDetail`

## Dialog: “Join your army” entry
- Added under the hub token `hero_main_options` with roleplayed lines.
- Player: "My lord, with your leave, I wish to enlist under your banner."
- Noble responses:
  - Different kingdom → deny
  - Same kingdom with army → accept
  - Same kingdom without army → defer

See `docs/BLUEPRINT.md` for architecture and phase details.
