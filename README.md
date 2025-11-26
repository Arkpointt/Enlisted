# Enlisted – Military Service System

`Enlisted` replaces the typical “raise an army on day one” start with a professional military career. You enlist under any AI lord, follow their army, draw wages, and climb through seven ranks before earning an honorable discharge. Everything—from duties to equipment kits—is configuration driven and logged for troubleshooting.

## 1. Requirements
- Mount & Blade II: Bannerlord v1.3.5 or later
- Bannerlord.Harmony (required dependency)
- Windows with .NET Framework 4.7.2 (for building)

## 2. Installation (Players)
1. Download the latest release (`Enlisted.zip`) from the [Releases](../../releases) page.
2. Extract the contents to `<BannerlordInstall>\Modules\Enlisted\`.
3. Install/enable Bannerlord.Harmony.
4. Enable “Enlisted” in the Bannerlord launcher.

## 3. Installation (Developers)
1. Clone the repository into `C:\Dev\Enlisted\Enlisted` (recommended path used in scripts).
2. Open `Enlisted.sln` in Visual Studio 2022 or run `dotnet build -c "Enlisted EDITOR"`.
3. Output: `<BannerlordInstall>\Modules\Enlisted\bin\Win64_Shipping_wEditor\Enlisted.dll`.
4. Close the Bannerlord launcher if the DLL is locked during copy; rerun the build after the launcher closes.

## 4. How to Use the Mod In-Game
1. **Locate any lord** on the campaign map.
2. Start a conversation → “I have something else to discuss” → “I wish to serve in your warband.”
3. Accept enlistment and your clan will hide on the map (no party management required).
4. Press `N` for the Enlisted Status menu and `P` to check promotion eligibility.
5. On promotion to Tier 2 choose a formation (Infantry, Archer, Cavalry, Horse Archer). Culture-appropriate equipment unlocks after each promotion at the quartermaster menu.
6. Use the Duties tab to pick a daily assignment (Enlisted, Forager, Messenger, Sentry, Pioneer). At Tier 3 and higher you can select professions (Quartermaster’s Aide, Field Medic, etc.) that grant officer roles inside the lord’s party.
7. Request temporary leave through the status menu if you need to travel independently. Leave automatically ends when the timer expires or you speak to your commander.
8. Losing a battle triggers a 14-day desertion grace period. During grace, you may rejoin any lord from the same kingdom. Your rank, XP, equipment, and enlistment date are restored, and you remain a vassal of the kingdom. A one-day invulnerability shield (`IgnoreByOtherPartiesTill`) starts as soon as captivity ends so hostile lords cannot instantly re-engage you.
9. Completing 252 in-game days of service (roughly three campaign years) marks you for honorable discharge. If you retire after meeting that requirement you keep your final kit permanently and rejoin your original kingdom without penalties.

### Battle Flow
- The mod activates your hidden party when your commander enters a battle and deactivates it when the battle ends, preventing duplicate “Attack/Surrender” menus.
- Command filtering ensures you only receive the orders relevant to your formation.
- All encounter transitions (sieges, prisoner menus, grace windows) are deferred via the `NextFrameDispatcher` to avoid zero-delta race conditions.

### Equipment
- Promotions unlock culture-specific templates curated from native troop trees.
- Equipment is replaced rather than duplicated; you carry only the kit assigned to your current rank.
- After an honorable discharge you keep the loadout you finished with.

### Leave, Capture, and Grace Periods
- Temporary leave parks your clan as visible/active for civilian interactions while preserving the link to your commander.
- If you and your commander are captured the mod defers the tear-down until the native surrender flow closes. The party is made visible, a 24-hour ignore window is applied, and you stay in the kingdom until the 14-day grace timer expires.
- Re-enlisting during grace clears the timer but preserves the original enlistment start date, so progress toward honorable discharge is continuous.

## 5. Feature Summary
- **Career Progression**: Seven tiers, wages that scale with rank, formation-specific XP gains, and officer professions that tie directly into native party roles.
- **Battle Integration**: Automatic participation in both lord battles and army actions, menu suppression for invalid encounters, and army-aware siege handling.
- **Duty & Profession System**: JSON-driven duties (T1+) and professions (T3+) that award targeted XP, wages, and officer assignments.
- **Equipment Management**: Master-at-Arms promotion menus, Quartermaster UI, culture-aware kits, and realistic pricing.
- **Leave & Safety Systems**: Temporary leave workflow, one-day post-capture protection, and encounter suppression that keeps the player from being re-engaged while menus are closing.
- **Logging & Diagnostics**: Session logs (`enlisted.log`, `discovery.log`, `dialog.log`, `api.log`) plus discovery artifacts for menu IDs and dialog tokens. Logs live under `<BannerlordInstall>\Modules\Enlisted\Debugging\`.

## 6. Configuration
All mechanics are data-driven. Edit the JSON files under `ModuleData/Enlisted/`:

| File | Purpose |
| --- | --- |
| `settings.json` | Global toggles, logging options, encounter suppression flags |
| `enlisted_config.json` | Tiers, wages, formation metadata, retirement rules |
| `duties_system.json` | Duty and profession definitions plus officer-role bindings |
| `progression_config.json` | Promotion requirements (XP thresholds, skill gates) |
| `equipment_pricing.json` | Formation-specific cost scaling for each tier |
| `equipment_kits.json` | Culture-specific kit definitions per tier |
| `menu_config.json` | Gauntlet/Native menu IDs, strings, and shortcuts |

See `ModuleData/Enlisted/README.md` for the schema and examples.

## 7. Troubleshooting
- If you see repeated “Attack or Surrender” menus after a battle, check `enlisted.log` for encounter suppression messages.
- If a build warns that `Enlisted.dll` is locked, close the Bannerlord launcher or Watchdog tool and rebuild.
- If you lose access to armies after captivity, confirm the grace period message fired and that `GraceProtection` logged the one-day ignore window.
- For reproducible crashes, upload the Bannerlord crash dump plus the matching `enlisted.log` so we can inspect `PlayerEncounter` state.

## 8. Building & Contributing
- Follow the “Installation (Developers)” section to produce a DLL.
- Contributions should respect the package-by-feature layout under `src/Features/`.
- Verify any new API usage against official Bannerlord documentation or decompiled code.
- Keep comments professional and concise; update the relevant doc in `docs/` whenever behavior changes.

## 9. License
MIT License. See `LICENSE` for details.

## 10. Supporting Documentation
- `docs/BLUEPRINT.md` – architecture standards and Harmony policy.
- `docs/Features/*.md` – detailed specs for enlistment, duties, troop selection, temporary leave, etc.
- `docs/discovered/*.md` – API references verified against Bannerlord v1.3.5.

Enlisted delivers a structured, professional soldiering experience inside Bannerlord. Start as a recruit, follow your commander, earn your promotions, and return home as a decorated veteran once the 252-day tour is complete.
