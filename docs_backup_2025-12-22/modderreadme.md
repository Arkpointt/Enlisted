# Developer Guide

Quick reference for extending or modifying Enlisted.

## Game / API Version

This project targets **Bannerlord v1.3.12+**.

When you need to confirm Bannerlord APIs, prefer the **local native decompile** included with this repository (keep it aligned with the target version). Use the official API docs only as a secondary convenience reference.

## Build
```bash
dotnet build -c "Enlisted RETAIL" /p:Platform=x64
```
Output: `<Bannerlord>/Modules/Enlisted/bin/Win64_Shipping_Client/Enlisted.dll`

## Structure

```
src/
├─ Mod.Entry/            # SubModule + Harmony bootstrap
├─ Mod.Core/             # Logging, settings, helpers
├─ Mod.GameAdapters/     # Harmony patches
└─ Features/
   ├─ Enlistment/        # Service state, battles, retirement
   ├─ Assignments/       # Duties system
   ├─ Equipment/         # Gear management
   ├─ Combat/            # Battle participation, kill tracking
   ├─ Interface/         # Menus
   └─ Conversations/     # Dialog
```

Data and localization live outside `src/`:
- `ModuleData/Enlisted/` (JSON: config + content)
- `ModuleData/Languages/` (XML: translations)

## Key Systems

### EnlistmentBehavior
- Tracks `_enlistedLord`, tier, XP, kills, grace periods
- Real-time ticks for following, daily ticks for wages
- +25 XP per battle, +1 XP per kill
- 252-day first term, 84-day renewals

### Duties
- JSON-defined in `ModuleData/Enlisted/duties_system.json`
- Tier-gated (T1-T6)
- Officer duties use native party role methods

### Equipment
- Culture-specific loadouts via `equipment_kits.json`
- Replaced on promotion, restored on discharge

## Logging

### Mod Logs

**ALL ENLISTED MOD LOGS OUTPUT TO THIS LOCATION:**

`<BannerlordInstall>\Modules\Enlisted\Debugging\`

**Example full path:**

`C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\Enlisted\Debugging\`

**CRITICAL:** The mod writes all logs directly to the `Debugging` subfolder inside the Enlisted module directory. This is NOT the game's ProgramData crash folder and NOT your Documents folder.

**Files created in the Debugging folder:**
- `Session-A_{yyyy-MM-dd_HH-mm-ss}.log` - Current session (newest)
- `Session-B_{yyyy-MM-dd_HH-mm-ss}.log` - Previous session
- `Session-C_{yyyy-MM-dd_HH-mm-ss}.log` - Oldest kept session
- `Conflicts-A_{yyyy-MM-dd_HH-mm-ss}.log` - Current conflicts diagnostics (newest)
- `Conflicts-B_{yyyy-MM-dd_HH-mm-ss}.log` - Previous conflicts
- `Conflicts-C_{yyyy-MM-dd_HH-mm-ss}.log` - Oldest kept conflicts
- `Current_Session_README.txt` - Active log summary and sharing instructions

**Note:** The mod no longer creates `enlisted.log` or `conflicts.log` (legacy filenames). All logs use timestamped Session/Conflicts rotation.

**Session logs contain:** Main activity with category-based verbosity (Enlistment, Orders, Equipment, Combat, etc.)

**Conflicts logs contain:** Mod conflict diagnostics including Harmony patch conflicts, execution order, behaviors, environment info

### Game Crash Logs (Separate Location)

**Bannerlord's crash logs are in a DIFFERENT location:**

`C:\ProgramData\Mount and Blade II Bannerlord\crashes\`

Each crash creates a timestamped folder containing `crash_tags.txt`, `rgl_log_*.txt`, and `dump.dmp`. These are game logs, not Enlisted mod logs.

Configure levels in `settings.json`:
```json
{
  "LogLevels": {
    "Default": "Info",
    "Battle": "Debug",
    "Equipment": "Warn"
  }
}
```

## Adding a Patch

1. Create in `src/Mod.GameAdapters/Patches/`
2. Add to `Enlisted.csproj`
3. Document purpose in header comment

```csharp
/// <summary>
/// Brief purpose. Targets Type.Method.
/// </summary>
[HarmonyPatch(typeof(TargetType), "MethodName")]
public class YourPatch
{
    static bool Prefix(...) { ... }
}
```

## Critical Patterns

**Deferred operations** - Use for encounter transitions:
```csharp
NextFrameDispatcher.RunNextFrame(() => GameMenu.ActivateGameMenu("menu_id"));
```

**Safe hero access** - Null-safe during character creation:
```csharp
var hero = CampaignSafetyGuard.SafeMainHero;
```

**Party following**:
```csharp
party.SetMoveEscortParty(lord, NavigationType.Default, false);
party.IsVisible = false;
```

**Gold transactions** - Use `GiveGoldAction` (not `ChangeHeroGold`):
```csharp
// X WRONG: ChangeHeroGold modifies internal gold not visible in UI
Hero.MainHero.ChangeHeroGold(-amount);

// [x] CORRECT: GiveGoldAction updates party treasury visible in UI
GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, amount);  // Deduct
GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, amount);  // Grant
```

**Equipment slot iteration** - Use numeric loop (not `Enum.GetValues`):
```csharp
// X WRONG: Includes invalid count values, causes IndexOutOfRangeException
foreach (EquipmentIndex slot in Enum.GetValues(typeof(EquipmentIndex))) { ... }

// [x] CORRECT: Iterate valid indices only (0-11)
for (int i = 0; i < (int)EquipmentIndex.NumEquipmentSetSlots; i++)
{
    var slot = (EquipmentIndex)i;
}
```

## Mod Integration

Other mods can hook into Enlisted's systems via public events and properties.

### Events

Subscribe in your `OnGameStart` or `OnCampaignStart`:

```csharp
using Enlisted.Features.Enlistment.Behaviors;

// Subscribe to enlistment events
EnlistmentBehavior.OnEnlisted += (lord) => {
    // Player enlisted with lord
};

EnlistmentBehavior.OnDischarged += (reason) => {
    // Player discharged (reason: "Retirement", "Leave expired", etc.)
};

EnlistmentBehavior.OnPromoted += (tier) => {
    // Player promoted to tier (1-6)
};

EnlistmentBehavior.OnXPGained += (xp, source) => {
    // XP gained (source: "Daily Service", "Battle", "Kill", etc.)
};

EnlistmentBehavior.OnWagePaid += (amount) => {
    // Daily wage paid
};

EnlistmentBehavior.OnLeaveStarted += () => {
    // Player started temporary leave
};

EnlistmentBehavior.OnLeaveEnded += () => {
    // Leave ended (returned or deserted)
};

EnlistmentBehavior.OnGracePeriodStarted += () => {
    // Grace period started (lord died, army defeated)
};
```

### Public Properties

```csharp
var eb = EnlistmentBehavior.Instance;
if (eb?.IsEnlisted == true)
{
    Hero lord = eb.CurrentLord;           // Serving lord
    int tier = eb.EnlistmentTier;         // 1-6
    int xp = eb.EnlistmentXP;             // Total XP
    string duty = eb.SelectedDuty;        // Current duty
    bool onLeave = eb.IsOnLeave;          // On leave?
    bool inGrace = eb.IsInDesertionGracePeriod;
}
```

### Harmony Compatibility

All Enlisted patches use default priority (400). If your mod patches the same methods, use:
- Priority.Low (200) to run after Enlisted
- Priority.High (600) to run before Enlisted

## Docs

| Doc | Purpose |
|-----|---------|
| docs/BLUEPRINT.md | Architecture, patterns |
| docs/Features/*.md | Feature specs |
| docs/research/campaignsystem-apis.md | API notes and pointers (research) |
| ModuleData/Enlisted/README.md | Config schema |

### API Reference

When extending Enlisted, verify Bannerlord API usage against:
- The local native decompile included with this repository (authoritative; matches v1.3.4)
- The official API docs for v1.3.4 as a secondary convenience reference
