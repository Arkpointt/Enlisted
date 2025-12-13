# Blueprint

Architecture and standards for the Enlisted mod.

## Build

- Visual Studio: config "Enlisted RETAIL" → Build
- CLI: `dotnet build -c "Enlisted RETAIL" /p:Platform=x64`
- Output: `<Bannerlord>/Modules/Enlisted/bin/Win64_Shipping_Client/`

## Steam Workshop upload (AI checklist)
- Build first: `dotnet build -c "Enlisted RETAIL" /p:Platform=x64` (copies `WorkshopUpdate.xml` and `preview.png` into `Modules/Enlisted`).
- WorkshopUpdate.xml (for updating existing Workshop item) lives at `tools/workshop/`; uses Bannerlord's official format:
  - Root: `<Tasks>` with `<GetItem>` (contains Workshop ID) and `<UpdateItem>` sections
  - Uses `<ModuleFolder>`, `<ChangeNotes>`, `<Tags>` (not ModulePath/PreviewPath)
  - Official tags: Type (`Partial Conversion`), Setting (`Medieval`), Game Mode (`Singleplayer`), Compatible Version (`1.3.10`)
  - Image path points to `Modules\Enlisted\preview.png` (copied on build)
  - Escaped path: `Mount &amp; Blade II Bannerlord`
- Upload via SteamCMD:
  - `& "C:\Dev\steamcmd\steamcmd.exe" +login YOUR_STEAM_USERNAME +workshop_build_item "C:\Dev\Enlisted\Enlisted\tools\workshop\workshop_upload.vdf" +quit`
  - Uses cached credentials if present; otherwise prompts for password/Steam Guard.
- VDF: `tools/workshop/workshop_upload.vdf`
  - `publishedfileid` already set to current Workshop ID (`3621116083`); keep as-is for updates.
  - `contentfolder` points to installed `Modules\Enlisted`; `previewfile` to `tools/workshop/preview.png`.
  - `visibility` defaults to public (`0`); keep in sync with Steam page if changed there.
- Keep description in VDF aligned with current Nexus/Steam text and reporting instructions; rerun SteamCMD after edits.
- The uploader generates `workshop_upload.resolved.vdf` with the preview path resolved to your local repo, so the VDF stays repo-relative.

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
    ├── Interface/          # Main menus (enlisted_status, enlisted_activities)
    ├── Lances/             # Lance assignments, personas, and events
    │   ├── Behaviors/      # LanceStoryBehavior, EnlistedLanceMenuBehavior
    │   ├── Events/         # Lance Life Events system
    │   └── Personas/       # Named lance role personas
    ├── Conditions/         # Player condition system (injury/illness)
    ├── Camp/               # My Camp menu, camp life simulation
    ├── Activities/         # Data-driven camp activities
    └── Escalation/         # Heat/discipline/reputation tracks
```

```
ModuleData/
├── Enlisted/               # JSON config + content (shipping data)
└── Languages/              # XML localization (enlisted_strings*.xml)
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

## Harmony Patches

Patches are intentionally kept narrow and are the exception (prefer public APIs when possible).

Source of truth:
- `src/Mod.GameAdapters/Patches/` (actual patch inventory)
- `Modules/Enlisted/Debugging/Conflicts-*.log` (what ran, in what order, and who else patched the same methods)

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

### Gold Transactions

```csharp
// ❌ WRONG: ChangeHeroGold modifies internal gold not visible in UI
Hero.MainHero.ChangeHeroGold(-amount);

// ✅ CORRECT: GiveGoldAction updates party treasury visible in UI
GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, amount);  // Deduct
GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, amount);  // Grant
```

### Equipment Slot Iteration

```csharp
// ❌ WRONG: Enum.GetValues includes invalid count values (crashes)
foreach (EquipmentIndex slot in Enum.GetValues(typeof(EquipmentIndex))) { ... }

// ✅ CORRECT: Numeric iteration with NumEquipmentSetSlots
for (int i = 0; i < (int)EquipmentIndex.NumEquipmentSetSlots; i++)
{
    var slot = (EquipmentIndex)i;
    // Safe access to equipment[slot]
}
```

### Culture-Specific Ranks (Phase 7)

```csharp
// Get current rank title based on player's tier and lord's culture
var rank = RankHelper.GetCurrentRank(enlistment);

// Get rank for a specific tier
var cultureId = enlistment.EnlistedLord?.Culture?.StringId ?? "mercenary";
var tier3Rank = RankHelper.GetRankTitle(3, cultureId);  // "Immunes" for Empire

// Get next rank for promotion display
var nextRank = RankHelper.GetNextRank(enlistment);
```

### Duty Request System (Phase 7)

```csharp
// Request a duty change (T2+ players)
var result = EnlistedDutiesBehavior.Instance.RequestDutyChange(dutyId);
if (result.Approved)
{
    // Success - duty assigned
}
else
{
    // Denied - show result.Reason (cooldown, reputation, tier, etc.)
}
```

### Pay System

```csharp
// Check pay tension level (0-100)
var tension = EnlistmentBehavior.Instance.PayTension;

// Get morale penalty from pay tension
var moralePenalty = EnlistmentBehavior.Instance.GetPayTensionMoralePenalty();

// Check if free desertion is available (tension >= 60)
if (EnlistmentBehavior.Instance.IsFreeDesertionAvailable)
{
    EnlistmentBehavior.Instance.ProcessFreeDesertion();
}

// Award battle loot share (called post-battle)
var goldEarned = EnlistmentBehavior.Instance.AwardBattleLootShare(mapEvent);

// Get wage breakdown for tooltip
var breakdown = EnlistmentBehavior.Instance.GetWageBreakdown();
```

### Tier-Gated Loot

```csharp
// LootBlockPatch.ShouldBlockLoot() logic:
if (enlistment.EnlistmentTier >= 4)
    return false;  // Allow loot for T4+
return true;       // Block loot for T1-T3 (compensated via gold share)
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
| enlisted_config.json    | Feature flags, wages, retirement rules |
| duties_system.json      | Duty definitions               |
| progression_config.json | 9-tier XP thresholds, culture-specific ranks (T1-4 Enlisted, T5-6 Officer, T7-9 Commander) |
| lances_config.json      | Lance definitions, culture/style mapping |
| equipment_pricing.json  | Gear costs                     |
| equipment_kits.json     | Culture-specific loadouts      |
| Activities/activities.json | Camp Activities menu actions (data-driven XP/fatigue) |
| Conditions/condition_defs.json | Player condition definitions (injury/illness) |
| LancePersonas/name_pools.json | Name pools for lance personas |
| Events/*.json           | Lance Life Events catalog packs |

### Event Packs (Events/*.json)

| Pack | Purpose |
|------|---------|
| events_general.json | General camp and training events |
| events_onboarding.json | New recruit onboarding chain |
| events_training.json | Formation training events |
| events_pay_tension.json | Pay grumbling, theft, confrontation, mutiny |
| events_pay_loyal.json | Loyal path missions (debts, escort, raid) |
| events_pay_mutiny.json | Desertion planning, mutiny resolution chains |

### Culture Ranks (progression_config.json)

Each culture has unique rank names for all 9 tiers:
- **Empire**: Tiro → Miles → Immunes → Principalis → Evocatus → Centurion → Primus Pilus → Tribune → Legate
- **Vlandia**: Peasant → Levy → Footman → Man-at-Arms → Sergeant → Knight Bachelor → Cavalier → Banneret → Castellan
- **Sturgia**: Thrall → Ceorl → Fyrdman → Drengr → Huskarl → Varangian → Champion → Thane → High Warlord
- **Khuzait**: Outsider → Nomad → Noker → Warrior → Veteran → Bahadur → Arban → Zuun → Noyan
- **Battania**: Woodrunner → Clan Warrior → Skirmisher → Raider → Oathsworn → Fian → Highland Champion → Clan Chief → High King's Guard
- **Aserai**: Tribesman → Skirmisher → Footman → Veteran → Guard → Faris → Emir's Chosen → Sheikh → Grand Vizier
- **Mercenary**: Follower → Recruit → Free Sword → Veteran → Blade → Chosen → Captain → Commander → Marshal

## Menu System

| Menu ID | Purpose | Entry Point |
|---------|---------|-------------|
| `enlisted_status` | Main hub - status display, high-level navigation | Party while enlisted |
| `enlisted_activities` | Camp Activities - training, tasks, social (data-driven) | From enlisted_status |
| `enlisted_lance` | My Lance - roster view, player position, relationships | From enlisted_status |
| `enlisted_medical` | Medical Attention - treatment options (when injured/ill) | From enlisted_status |
| `enlisted_duty_selection` | Report for Duty - duty and profession selection | From enlisted_status |
| `command_tent` | My Camp - service records, pay status, retinue | From enlisted_status |
| `enlisted_desert_confirm` | Desertion confirmation with penalty display | From Desert the Army |

### Status Display

The `enlisted_status` menu shows:
- Party Objective, Term Remaining, Rank (Tier)
- Formation, Fatigue, Lance
- Wage (with gold icon)
- **Pay Status** (when PayTension > 0): Grumbling → Tense → Severe → CRITICAL
- **Owed Backpay** (when > 0): Accumulated unpaid wages
- Current XP, Next Level XP

### Leave Options

Grouped at bottom of menu (indices 20+):
- **Ask for Leave** (always) - Request temporary leave
- **Leave Without Penalty** (PayTension >= 60) - Free desertion
- **Desert the Army** (always) - With penalties

## Guidelines

- Prefer public APIs over Harmony
- Guard all engine objects with null checks
- Fail closed on errors
- Keep patches narrow and documented
- Use `NextFrameDispatcher` for encounter transitions
