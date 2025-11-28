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
│   └── Patches/            # 18 patches
└── Features/
    ├── Enlistment/         # Core service state, retirement
    ├── Assignments/        # Duties system
    ├── Equipment/          # Gear management
    ├── Ranks/              # Promotions
    ├── Conversations/      # Dialog
    ├── Combat/             # Battle participation
    └── Interface/          # Menus
```

## Harmony Patches (18)

| Patch | Purpose |
|-------|---------|
| BattleCommandsFilterPatch | Formation-based command filtering |
| ClanFinanceEnlistmentIncomePatch | Adds wages to tooltip |
| DischargePenaltySuppressionPatch | Prevents relation loss on discharge |
| DutiesOfficerRolePatches | Officer role integration |
| EncounterSuppressionPatch | Prevents unwanted encounters |
| EnlistedWaitingPatch | Prevents game pause when lord battles |
| EnlistmentExpenseIsolationPatch | Hides expenses while enlisted |
| HidePartyNamePlatePatch | Hides player nameplate |
| InfluenceMessageSuppressionPatch | Suppresses "0 influence" messages |
| KingdomDecisionParticipationPatch | Blocks kingdom voting |
| LootBlockPatch | Prevents personal loot |
| MercenaryIncomeSuppressionPatch | Hides mercenary income |
| OrderOfBattleSuppressionPatch | Skips deployment screen |
| PostDischargeProtectionPatch | Protects after discharge |
| SkillSuppressionPatch | Blocks tactics/leadership XP |
| StarvationSuppressionPatch | Prevents starvation |
| TownLeaveButtonPatch | Hides Leave button |
| VisibilityEnforcementPatch | Controls party visibility |

## Logging

Logs in `<Bannerlord>\Modules\Enlisted\Debugging\`:
- `enlisted.log` - Main activity
- `conflicts.log` - Mod compatibility

Categories controlled via `settings.json`. Default level: Info. Throttling prevents spam.

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
