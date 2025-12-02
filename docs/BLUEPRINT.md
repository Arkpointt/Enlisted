# Blueprint

Architecture and standards for the Enlisted mod.

## Build
- Visual Studio: config "Enlisted EDITOR" → Build
- CLI: `dotnet build -c "Enlisted EDITOR"`
- Output: `<Bannerlord>/Modules/Enlisted/`

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
│   └── Patches/            # 19 patches
└── Features/
    ├── Enlistment/         # Core service state, retirement
    ├── Assignments/        # Duties system
    ├── Equipment/          # Gear management
    ├── Ranks/              # Promotions
    ├── Conversations/      # Dialog
    ├── Combat/             # Battle participation
    └── Interface/          # Menus
```

## Harmony Patches (20)

| Patch | Purpose |
|-------|---------|
| ArmyCohesionExclusionPatch | Compensates for enlisted player's cohesion impact |
| BattleCommandsFilterPatch | Formation-based command filtering |
| ClanFinanceEnlistmentIncomePatch | Adds wages to tooltip |
| DischargePenaltySuppressionPatch | Prevents relation loss on discharge |
| DutiesOfficerRolePatches | Officer role integration |
| EncounterSuppressionPatch | Prevents unwanted encounters |
| EnlistedWaitingPatch | Prevents game pause when lord battles |
| EnlistmentExpenseIsolationPatch | Hides expenses while enlisted |
| FoodConsumptionSuppressionPatch | Skips food consumption when enlisted (lord provides food) |
| HidePartyNamePlatePatch | Hides player nameplate |
| IncidentsSuppressionPatch | Suppresses random incidents (DLC/Naval) |
| InfluenceMessageSuppressionPatch | Suppresses "0 influence" messages |
| LootBlockPatch | Prevents personal loot |
| MercenaryIncomeSuppressionPatch | Suppresses native mercenary income (players receive mod wages only) |
| OrderOfBattleSuppressionPatch | Skips deployment screen |
| PostDischargeProtectionPatch | Protects after discharge |
| SkillSuppressionPatch | Blocks tactics/leadership XP |
| StarvationSuppressionPatch | Prevents starvation (backup for FoodConsumptionSuppressionPatch) |
| TownLeaveButtonPatch | Hides Leave button |
| VisibilityEnforcementPatch | Controls party visibility |

## Logging

Logs in `<Bannerlord>\Modules\Enlisted\Debugging\`:
- `enlisted.log` - Main activity
- `conflicts.log` - Mod compatibility

### Log Levels
- **Error** - Critical failures requiring attention
- **Warn** - Unexpected conditions handled gracefully
- **Info** - Important state changes and events
- **Debug** - Detailed diagnostic information
- **Trace** - Very verbose, for deep debugging

### Debug Categories
| Category | What It Logs |
|----------|--------------|
| Cohesion | Army cohesion compensation for enlisted player |
| Enlistment | Core service state: enlist, discharge, kingdom join/leave |
| Battle | Battle participation, army joining, MapEvent handling |
| Discharge | Kingdom restoration, relation penalty suppression |
| Finance | Mercenary income suppression, wage calculations |
| Food | Food consumption suppression, starvation checks |
| Desertion | Grace period management, desertion penalties |
| SaveLoad | Save/load operations, state restoration |
| Following | Escort AI, position sync, naval following |
| Naval | Sea state sync, naval position updates |
| Siege | Siege state detection, besieger camp sync |
| EncounterGuard | Encounter transitions, menu activation |
| EncounterCleanup | Post-battle cleanup, visibility restoration |
| Equipment | Equipment backup/restore, kit assignment |
| Diagnostics | Party state dumps, debug info |
| Session | Startup diagnostics, version info |
| Config | Configuration loading |

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

## Configuration

All JSON files in `ModuleData/Enlisted/`:

| File | Purpose |
|------|---------|
| settings.json | Logging, encounter settings |
| enlisted_config.json | Tiers, wages, retirement rules |
| duties_system.json | Duty definitions |
| progression_config.json | XP thresholds |
| equipment_pricing.json | Gear costs |
| equipment_kits.json | Culture-specific loadouts |

## Guidelines

- Prefer public APIs over Harmony
- Guard all engine objects with null checks
- Fail closed on errors
- Keep patches narrow and documented
- Use `NextFrameDispatcher` for encounter transitions
