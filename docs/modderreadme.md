# Developer Guide

Quick reference for extending or modifying Enlisted.

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

Location: `<BannerlordInstall>\Modules\Enlisted\Debugging\`

- Session logs rotate across three files:
  - `Session-A_{yyyy-MM-dd_HH-mm-ss}.log` (newest)
  - `Session-B_{...}.log`
  - `Session-C_{...}.log` (oldest kept)
- Conflicts logs rotate similarly:
  - `Conflicts-A_{yyyy-MM-dd_HH-mm-ss}.log` (newest)
  - `Conflicts-B_{...}.log`
  - `Conflicts-C_{...}.log` (oldest kept)
- `Current_Session_README.txt` summarizes Session/Conflicts A/B/C and how to share logs.
- `enlisted.log` - Legacy name (redirected to session rotation); main log with category-based verbosity control
- `conflicts.log` - Mod conflict diagnostics:
  - Harmony patch conflict detection (identifies mods sharing patches)
  - Patch execution order analysis
  - Registered behaviors list
  - Environment and module information
  - Categorized patch inventory

### Game Crash Logs

Location: `C:\ProgramData\Mount and Blade II Bannerlord\crashes\`

Each crash creates a timestamped folder containing `crash_tags.txt`, `rgl_log_*.txt`, and `dump.dmp`.

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
    string prof = eb.SelectedProfession;  // Profession track
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
| docs/discovered/*.md | Bannerlord API reference (v1.3.4 verified) |
| ModuleData/Enlisted/README.md | Config schema |

### API Reference

When extending Enlisted, refer to the discovered API documentation:
- `docs/discovered/engine.md` - Core API signatures
- `docs/discovered/menus.md` - Menu system
- `docs/discovered/gauntlet.md` - UI system
- `docs/discovered/equipment.md` - Equipment APIs
- `docs/discovered/helpers.md` - Helper methods

All discovered docs are updated for Bannerlord v1.3.4 compatibility and include indexes for easy navigation.
