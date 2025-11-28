# Enlisted – Military Service System

`Enlisted` replaces the typical "raise an army on day one" start with a professional military career. You enlist under any AI lord, follow their army, draw wages, and climb through six ranks before earning an honorable discharge. Everything—from duties to equipment kits—is configuration driven and logged for troubleshooting.

## 1. Requirements
- Mount & Blade II: Bannerlord v1.3.6 or later
- Bannerlord.Harmony (required dependency)
- Windows with .NET Framework 4.7.2 (for building)

## 2. Installation (Players)
1. Download the latest release (`Enlisted.zip`) from the [Releases](../../releases) page.
2. Extract the contents to `<BannerlordInstall>\Modules\Enlisted\`.
3. Install/enable Bannerlord.Harmony.
4. Enable "Enlisted" in the Bannerlord launcher.

## 3. Installation (Developers)
1. Clone the repository into `C:\Dev\Enlisted\Enlisted` (recommended path used in scripts).
2. Open `Enlisted.sln` in Visual Studio 2022 or run `dotnet build -c "Enlisted EDITOR"`.
3. Output: `<BannerlordInstall>\Modules\Enlisted\bin\Win64_Shipping_wEditor\Enlisted.dll`.
4. Close the Bannerlord launcher if the DLL is locked during copy; rerun the build after the launcher closes.

## 4. How to Use the Mod In-Game
1. **Locate any lord** on the campaign map.
2. Start a conversation → "I have something else to discuss" → "I wish to serve in your warband."
3. Accept enlistment and your clan will hide on the map (no party management required).
4. The Enlisted Status menu appears automatically while following your lord.
5. On promotion to Tier 2 choose a formation (Infantry, Archer, Cavalry, Horse Archer). Culture-appropriate equipment unlocks after each promotion at the quartermaster menu.
6. Use the Duties tab to pick a daily assignment (Enlisted, Forager, Messenger, Sentry, Pioneer). At Tier 3 and higher you can select professions (Quartermaster's Aide, Field Medic, etc.) that grant officer roles inside the lord's party.
7. Request temporary leave through dialogue with your lord if you need to travel independently. You have 14 days to return or face desertion penalties.
8. After 252 days (3 Bannerlord years), you become eligible for retirement. Speak with your lord to discuss options.

### Retirement System
After completing your first full term (252 days), you can:
- **Retire with full benefits**: 10,000 gold + 30 relation with your lord + 30 faction reputation + 15 relation with other faction lords (if they respect you)
- **Re-enlist for 20,000 gold bonus**: Extends service by 1 year (84 days)

After re-enlistment terms:
- **Discharge with 5,000 gold** and start a 6-month cooldown
- **Continue with 5,000 gold bonus** for another year

After the 6-month cooldown, you can re-enlist with the same faction:
- Your tier/rank is preserved
- You must select a new troop type
- 1-year term with 5,000 gold discharge at end

Each faction tracks your veteran status separately.

### Grace Periods (14 Days)
If your service ends unexpectedly, you have 14 days to find a new lord in the same kingdom:
- **Lord killed** → Grace period to find new commander
- **Lord captured** → Grace period while awaiting release
- **Army defeated** → Grace period to regroup
- **Leave expired** → Desertion penalties apply

During grace, you can speak with any lord in your faction to continue service. Your tier, XP, and equipment are preserved. Failing to re-enlist before the timer expires brands you as a deserter with relation penalties.

### Battle Flow
- The mod activates your hidden party when your commander enters a battle and deactivates it when the battle ends.
- You receive +25 XP for each battle you participate in, plus +1 XP per enemy you kill.
- Kills are tracked per faction and preserved when you re-enlist after cooldown.
- Command filtering ensures you only receive the orders relevant to your formation.
- All encounter transitions (sieges, prisoner menus, grace windows) are deferred via the `NextFrameDispatcher` to avoid timing conflicts.

### Equipment
- Promotions unlock culture-specific templates curated from native troop trees.
- Equipment is replaced rather than duplicated; you carry only the kit assigned to your current rank.
- After an honorable discharge you keep the loadout you finished with.

### Leave System
- Request leave through dialogue with your lord
- You have 14 days before being marked as a deserter
- Daily warnings appear when 7 or fewer days remain
- Return to your lord via dialogue to resume service

## 5. Feature Summary
- **Career Progression**: Six tiers (Levy → Household Guard), wages that scale with rank (detailed breakdown in tooltip), battle XP (+25 per battle, +1 per kill), and officer professions that tie directly into native party roles.
- **Veteran Retirement**: Full benefits after 252 days including gold, relations, and re-enlistment bonuses. Per-faction veteran tracking with preserved ranks and kill counts.
- **Financial Isolation**: While enlisted, clan finances show only your military wages - no lord/army income or expenses leak through.
- **Battle Integration**: Automatic participation in both lord battles and army actions, menu suppression for invalid encounters, and army-aware siege handling.
- **Soldier Experience**: No personal loot (spoils go to lord), no starvation (lord provides food), no native Leave button in settlements.
- **Grace Period System**: 14-day windows for lord death/capture/army defeat, with preserved progress and faction loyalty requirements.
- **Duty & Profession System**: JSON-driven duties (T1+) and professions (T3+) that award targeted XP, wages, and officer assignments.
- **Equipment Management**: Master-at-Arms promotion menus, Quartermaster UI, culture-aware kits, and realistic pricing. Personal equipment backed up and restored on service end.
- **Leave System**: 14-day leave timer with daily warnings and desertion penalties for overstaying.
- **Logging & Diagnostics**: Session logs (`enlisted.log`, `discovery.log`, `dialog.log`, `api.log`) plus discovery artifacts. Logs live under `<BannerlordInstall>\Modules\Enlisted\Debugging\`.

## 6. Configuration
All mechanics are data-driven. Edit the JSON files under `ModuleData/Enlisted/`:

| File | Purpose |
| --- | --- |
| `settings.json` | Global toggles, logging options, encounter suppression flags |
| `enlisted_config.json` | Tiers, wages, formations, retirement rules, grace periods |
| `duties_system.json` | Duty and profession definitions plus officer-role bindings |
| `progression_config.json` | Promotion requirements (XP thresholds), battle XP values |
| `equipment_pricing.json` | Formation-specific cost scaling for each tier |
| `equipment_kits.json` | Culture-specific kit definitions per tier |
| `menu_config.json` | Gauntlet/Native menu IDs, strings, and shortcuts |

See `ModuleData/Enlisted/README.md` for the schema and examples.

## 7. Troubleshooting
- If you see repeated "Attack or Surrender" menus after a battle, check `enlisted.log` for encounter suppression messages.
- If a build warns that `Enlisted.dll` is locked, close the Bannerlord launcher or Watchdog tool and rebuild.
- If you crash when entering battle, ensure you have the latest build (mission behavior issues have been fixed).
- If saving fails, ensure you're not saving during critical transitions (battle start/end).
- For reproducible crashes, upload the Bannerlord crash dump plus the matching `enlisted.log` so we can inspect `PlayerEncounter` state.

## 8. Building & Contributing
- Follow the "Installation (Developers)" section to produce a DLL.
- Contributions should respect the package-by-feature layout under `src/Features/`.
- Verify any new API usage against official Bannerlord documentation or decompiled code.
- Keep comments professional and concise; update the relevant doc in `docs/` whenever behavior changes.

## 9. License
MIT License. See `LICENSE` for details.

## 10. Supporting Documentation
- `docs/BLUEPRINT.md` – architecture standards and Harmony policy.
- `docs/Features/*.md` – detailed specs for enlistment, duties, troop selection, temporary leave, etc.
- `docs/discovered/*.md` – API references verified against Bannerlord v1.3.6.

Enlisted delivers a structured, professional soldiering experience inside Bannerlord. Start as a recruit, follow your commander, earn your promotions, and return home as a decorated veteran once your service is complete.
