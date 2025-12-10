# Blueprint

Architecture and standards for the Enlisted mod.

## Build

- Visual Studio: config "Enlisted RETAIL" → Build
- CLI: `dotnet build -c "Enlisted RETAIL" /p:Platform=x64`
- Output: `<Bannerlord>/Modules/Enlisted/bin/Win64_Shipping_Client/`

## Steam Workshop upload (AI checklist)
- Build first: `dotnet build -c "Enlisted RETAIL" /p:Platform=x64` (copies `WorkshopCreate.xml` into `Modules/Enlisted`).
- WorkshopCreate.xml (game-version tag file) lives at `tools/workshop/WorkshopCreate.xml`; tags: `Singleplayer`, `Gameplay`, `Overhaul`, `v1.3.10`. Escaped path: `Mount &amp; Blade II Bannerlord`.
- Upload via SteamCMD:
  - `& "C:\Dev\steamcmd\steamcmd.exe" +login YOUR_STEAM_USERNAME +workshop_build_item "C:\Dev\Enlisted\Enlisted\tools\workshop\workshop_upload.vdf" +quit`
  - Uses cached credentials if present; otherwise prompts for password/Steam Guard.
- VDF: `tools/workshop/workshop_upload.vdf`
  - `publishedfileid` already set to current Workshop ID (`3621116083`); keep as-is for updates.
  - `contentfolder` points to installed `Modules\Enlisted`; `previewfile` to `tools/workshop/preview.png`.
  - `visibility` defaults to public (`0`); keep in sync with Steam page if changed there.
- Keep description in VDF aligned with current Nexus/Steam text and reporting instructions; rerun SteamCMD after edits.

## Dependencies

```xml
<DependedModules>
  <DependedModule Id="Bannerlord.Harmony" />
</DependedModules>
```

## Structure

```
src/
├── Mod.Entry/              # SubModule + Harmony init
├── Mod.Core/               # Logging, config, helpers
├── Mod.GameAdapters/       # Harmony patches
│   └── Patches/            # 25 patches
└── Features/
    ├── Enlistment/         # Core service state, retirement
    ├── Assignments/        # Duties system
    ├── Equipment/          # Gear management
    ├── Ranks/              # Promotions
    ├── Conversations/      # Dialog
    ├── Combat/             # Battle participation
    └── Interface/          # Menus
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

## Harmony Patches (25)

| Patch                            | Purpose                                                              |
|----------------------------------|----------------------------------------------------------------------|
| ArmyCohesionExclusionPatch       | Compensates for enlisted player's cohesion impact                    |
| ArmyDispersedMenuPatch           | Handles army dispersal menu for enlisted players                     |
| ClanFinanceEnlistmentIncomePatch | Adds wages to tooltip                                                |
| DischargePenaltySuppressionPatch | Prevents relation loss on discharge                                  |
| DutiesOfficerRolePatches         | Officer role integration                                             |
| EncounterSuppressionPatch        | Prevents unwanted encounters                                         |
| EnlistedWaitingPatch             | Overrides ComputeIsWaiting() to allow time flow when enlisted        |
| EnlistmentExpenseIsolationPatch  | Hides expenses while enlisted                                        |
| FoodSystemPatches                | Skips food consumption when enlisted (lord provides food)            |
| FormationMessageSuppressionPatch | Suppresses formation command messages                                |
| HidePartyNamePlatePatch          | Hides player nameplate                                               |
| IncidentsSuppressionPatch        | Suppresses random incidents (DLC/Naval)                              |
| InfluenceMessageSuppressionPatch | Suppresses "0 influence" messages                                    |
| JoinEncounterAutoSelectPatch     | Auto-joins lord's battle, bypasses "Help or Don't get involved" menu |
| LootBlockPatch                   | Prevents personal loot                                               |
| MercenaryIncomeSuppressionPatch  | Suppresses native mercenary income (players receive mod wages only)  |
| NavalBattleShipAssignmentPatch   | Naval crash fixes: ship assignment, captain lookup, troop deployment, AI behavior creation, and mission cleanup |
| NavalShipExclusionPatch          | Prevents lord from using enlisted player's ships for sea travel      |
| OrderOfBattleSuppressionPatch    | Skips deployment screen                                              |
| PlayerEncounterFinishSafetyPatch | Prevents crash when both mod and native try to finish encounter      |
| PostDischargeProtectionPatch     | Protects after discharge                                             |
| PrisonerActionBlockPatch         | Prevents enlisted soldiers from executing/releasing prisoner lords   |
| ReturnToArmySuppressionPatch     | Suppresses return to army messages                                   |
| SkillSuppressionPatch            | Blocks tactics/leadership XP                                         |
| TownLeaveButtonPatch             | Hides Leave button                                                   |
| VisibilityEnforcementPatch       | Controls party visibility                                            |

## Logging

### Mod Logs

Location: `<BannerlordInstall>\Modules\Enlisted\Debugging\`

- Session logs rotate across three files for easy human reading:
  - `Session-A_{yyyy-MM-dd_HH-mm-ss}.log` (newest)
  - `Session-B_{...}.log`
  - `Session-C_{...}.log` (oldest kept)
- `current_session.txt` points to the active session file.
- Conflicts logs rotate similarly:
  - `Conflicts-A_{yyyy-MM-dd_HH-mm-ss}.log` (newest)
  - `Conflicts-B_{...}.log`
  - `Conflicts-C_{...}.log` (oldest kept)
- `Current_Session_README.txt` summarizes Session/Conflicts A/B/C and how to share logs.
- `enlisted.log` - Legacy name (redirected to session rotation); throttled, category-based
- `conflicts.log` - Comprehensive mod conflict diagnostics:
  - Harmony patch conflict detection (identifies which mods patch the same methods)
  - Patch execution order and priority analysis
  - Registered campaign behaviors inventory
  - Environment info (game version, mod version, OS, CLR version)
  - Loaded modules enumeration
  - Categorized patch list by purpose (Army/Party, Encounter, Kingdom/Clan, Finance, UI/Menu, Combat, Other)
  - Tracks both main Harmony instance (startup patches) and deferred instance (campaign-start patches)
  - Combined conflict summary across all patches

### Game Crash Logs

Location: `C:\ProgramData\Mount and Blade II Bannerlord\crashes\`

Each crash creates a timestamped folder containing `crash_tags.txt`, `rgl_log_*.txt`, and `dump.dmp`.

### Log Levels

- **Error** - Critical failures requiring attention
- **Warn** - Unexpected conditions handled gracefully
- **Info** - Important state changes and events
- **Debug** - Detailed diagnostic information
- **Trace** - Very verbose, for deep debugging

### Debug Categories

| Category            | What It Logs                                              |
|---------------------|-----------------------------------------------------------|
| Cohesion            | Army cohesion compensation for enlisted player            |
| Enlistment          | Core service state: enlist, discharge, kingdom join/leave |
| Battle              | Battle participation, army joining, MapEvent handling     |
| Discharge           | Kingdom restoration, relation penalty suppression         |
| Finance             | Mercenary income suppression, wage calculations           |
| Food                | Food consumption suppression, starvation checks           |
| Desertion           | Grace period management, desertion penalties              |
| SaveLoad            | Save/load operations, state restoration                   |
| Following           | Escort AI, position sync, naval following                 |
| Naval               | Sea state sync, naval position updates                    |
| Siege               | Siege state detection, besieger camp sync                 |
| Mission             | Mission behavior initialization, mode detection           |
| FormationAssignment | Player formation assignment, position teleporting         |
| EncounterGuard      | Encounter transitions, menu activation                    |
| EncounterCleanup    | Post-battle cleanup, visibility restoration               |
| Equipment           | Equipment backup/restore, kit assignment                  |
| Diagnostics         | Party state dumps, debug info                             |
| Session             | Startup diagnostics, version info                         |
| Config              | Configuration loading                                     |

Categories controlled via `settings.json`. Default level: Info. Throttling prevents spam (5s default).

## Key Patterns

### Party Following

```csharp
party.SetMoveEscortParty(lordParty, NavigationType.Default, false);
party.IsVisible = false;  // Hide on map
```

### Battle Participation

```csharp
mainParty.Party.MapEventSide = targetSide;  // Join battle
```

### Deferred Operations

```csharp
NextFrameDispatcher.RunNextFrame(() => { ... });  // Avoid timing conflicts
```

### Campaign Safety

```csharp
var hero = CampaignSafetyGuard.SafeMainHero;  // Null-safe during char creation
```

### Wait Menu Time Control

Wait menus use `StartWait()` to enable time controls (play/pause/fast-forward buttons). Native `StartWait()` forces `UnstoppableFastForward` mode. Handle time mode conversion once in menu init, never in tick handlers:

```csharp
// In menu init - convert unstoppable to stoppable (allows pause)
args.MenuContext.GameMenu.StartWait();
Campaign.Current.SetTimeControlModeLock(false);
if (Campaign.Current.TimeControlMode == CampaignTimeControlMode.UnstoppableFastForward)
{
    Campaign.Current.TimeControlMode = CampaignTimeControlMode.StoppableFastForward;
}

// In tick handler - do NOT restore time mode here
// Native sets UnstoppableFastForward when user clicks fast forward (for army members)
// Restoring in tick fights with user input and causes speed controls to break
```

For army members, native uses `UnstoppableFastForward` when fast-forwarding. Per-tick restoration of `CapturedTimeMode` will fight with user input and break speed controls.

## Configuration

All JSON files in `ModuleData/Enlisted/`:

| File                    | Purpose                        |
|-------------------------|--------------------------------|
| settings.json           | Logging, encounter settings    |
| enlisted_config.json    | Tiers, wages, retirement rules |
| duties_system.json      | Duty definitions               |
| progression_config.json | XP thresholds                  |
| equipment_pricing.json  | Gear costs                     |
| equipment_kits.json     | Culture-specific loadouts      |

## Guidelines

- Prefer public APIs over Harmony
- Guard all engine objects with null checks
- Fail closed on errors
- Keep patches narrow and documented
- Use `NextFrameDispatcher` for encounter transitions
