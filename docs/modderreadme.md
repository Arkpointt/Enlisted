# Developer Guide

Quick reference for extending or modifying Enlisted.

## Build
```bash
dotnet build -c "Enlisted EDITOR"
```
Output: `<Bannerlord>/Modules/Enlisted/bin/Win64_Shipping_wEditor/Enlisted.dll`

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

Logs in `Modules/Enlisted/Debugging/`:
- `enlisted.log` - Main log
- `conflicts.log` - Mod compatibility

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

## Docs

| Doc | Purpose |
|-----|---------|
| docs/BLUEPRINT.md | Architecture, patterns |
| docs/Features/*.md | Feature specs |
| ModuleData/Enlisted/README.md | Config schema |
