# Blueprint

Architecture and development standards for the Enlisted military service mod.

## Quick Reference

### Build
- Visual Studio: config "Enlisted EDITOR" → Build
- CLI: `dotnet build -c "Enlisted EDITOR"`
- Note: Build warnings about locked DLLs are normal when Bannerlord is running

### Current Implementation

**13 Harmony Patches** (`src/Mod.GameAdapters/Patches/`):
1. BattleCommandsFilterPatch - Formation-based battle command filtering
2. ClanFinanceEnlistmentIncomePatch - Adds enlistment wages to daily gold tooltip
3. DischargePenaltySuppressionPatch - Prevents relation penalties during discharge
4. DutiesOfficerRolePatches - Officer role integration (Scout, Surgeon, Engineer, Quartermaster)
5. EncounterSuppressionPatch - Suppresses encounters with the lord when not in battle
6. EnlistmentExpenseIsolationPatch - Prevents expense sharing when attached to a lord
7. HidePartyNamePlatePatch - Hides the enlisted party nameplate while invisible
8. KingdomDecisionParticipationPatch - Blocks kingdom decision prompts
9. LootRestrictionPatch - Tier-based loot restrictions
10. NoHorseSiegePatch - Prevents mounted players from joining sieges
11. PostDischargeProtectionPatch - Temporary immunity right after discharge
12. VisibilityEnforcementPatch - Keeps party hidden/active in sync with service state
13. VotingSuppressionPatch - Prevents voting prompts for enlisted soldiers

**Core Behaviors** (`src/Mod.Entry/SubModule.cs`):
- EnlistmentBehavior - Core service state and lord relationships
- EnlistedDialogManager - Centralized dialog handling
- EnlistedDutiesBehavior - Duties system with formation training
- EnlistedMenuBehavior - Professional menu interface
- EnlistedEncounterBehavior - Battle participation and encounter management

**Debug Logs** (`<BannerlordInstall>\Modules\Enlisted\Debugging\`):
- `enlisted.log` - Main mod activity and errors
- `conflicts.log` - Mod compatibility diagnostics
- `dialog.log` - Conversation system events
- `discovery.log` - Troop/equipment discovery
- `api.log` - API interaction logging
- Session-scoped (cleared on init)

## 1. Purpose & Scope

Single source of truth for mod architecture, standards, and practices. Defines:
- Package-by-Feature structure for C# solutions
- Mod packaging rules and build process
- Development, commenting, and testing standards
- Platform constraints (Windows, game runtime)
- Harmony patching policy

## 2. Guiding Principles

- **Correctness first** - Prefer safe, deterministic behavior over cleverness
- **Determinism** - Use explicit gates, bounded waits, idempotent logic
- **Observability** - Structured logs for non-trivial paths
- **Fail closed** - On uncertainty, do the safe thing and record why
- **Config over code** - Prefer config flags to hard-coding behavior
- **Small, reversible changes** - Short branches, focused reviews
- **Player empathy** - Errors offer next steps
- **Respect the platform** - Honor module rules, API constraints, save-game safety

## 3. Architecture

### 3.1 Package-by-Feature Structure

Each feature is self-contained with related code grouped together:

```
src/
├── Mod.Entry/              # Module entry + Harmony initialization
├── Mod.Core/               # Shared services, logging, config
├── Mod.GameAdapters/       # TaleWorlds APIs, Harmony patches
│   └── Patches/            # 13 Harmony patches
└── Features/               # Each feature is self-contained
    ├── Enlistment/         # Core service state management
    ├── Assignments/        # Duties system and XP calculations
    ├── Equipment/          # Equipment management and troop selection
    ├── Ranks/              # Promotion tracking and wage calculation
    ├── Conversations/      # Dialog handling
    ├── Combat/             # Battle participation
    └── Interface/          # Menu system
```

### 3.2 Integration Boundary

Single "game adapters" layer isolates TaleWorlds types, Harmony patches, and game events from core logic.

### 3.3 Separation of Concerns

- **Decision**: Feature logic determines what to do
- **Actuation**: Adapters call TaleWorlds APIs safely
- **Observation**: Centralized logging with stable categories

## 4. Project Structure

### 4.1 Root Layout

```
Enlisted/
├── Enlisted.sln
├── README.md
├── SubModule.xml
├── .editorconfig
├── .gitignore
└── src/                    # All implementation
```

### 4.2 Source Organization

- **Mod.Entry**: Thin module entry and Harmony initialization
- **Mod.Core**: Shared services, logging, configuration
- **Mod.GameAdapters**: TaleWorlds APIs, Harmony patches, event bridges
- **Features**: Self-contained feature modules (Package-by-Feature)

### 4.3 Module Packaging

Build outputs to: `<BannerlordInstall>/Modules/Enlisted/`

```
Modules/Enlisted/
├── SubModule.xml
├── bin/Win64_Shipping_Client/Enlisted.dll
├── ModuleData/             # JSON config files
└── GUI/Prefabs/            # UI templates
```

**Rule**: Build artifacts only - never commit to repo.

### 4.4 Military Service Architecture

**Enlistment**: Tracks lord service relationship and basic state (`src/Features/Enlistment/Behaviors/`)

**Assignments**: Handles daily duties and benefits (`src/Features/Assignments/Behaviors/`)

**Equipment**: Manages gear access based on rank/culture (`src/Features/Equipment/Behaviors/`)

**Battle Integration**:
- Uses `IsActive = false` to hide player party and prevent encounters
- `EnlistedEncounterBehavior` manages battle participation
- Autosim defeats fall back to vanilla behavior (player chooses Attack/Surrender after the result)
- Formation-based command filtering

**Menu Integration**:
- Custom menu with service status, wages, progression
- Organized duty/profession selection
- Tier-based access with detailed descriptions

## 5. Harmony Patching Policy

### 5.1 Placement & Structure

- All patches in `src/Mod.GameAdapters/Patches/`
- One class per target patch
- Use `[HarmonyPatch]` attributes

### 5.2 When to Use Harmony

- Intercept sealed, internal/private, or engine-invoked methods
- Critical side effects unreachable via public APIs
- Examples: Loot restrictions, expense isolation, voting suppression, visibility/nameplate enforcement

### 5.3 When NOT to Use Harmony

- Prefer public APIs when available
- Use `IsActive = false` engine property for encounter prevention (no patches needed)
- Keep domain logic in features, not patches

### 5.4 Documentation Standard

Every patch must include:

```csharp
/// <summary>
/// [Brief purpose]
/// Targets [Namespace.Type.Method] to [what it does].
/// Safety: [scope guards, null checks, constraints].
/// </summary>
[HarmonyPatch(typeof(TargetType), "MethodName")]
public class YourPatch
{
    static bool Prefix(...) { /* implementation */ }
}
```

### 5.5 Reflection Pattern (Obfuscated APIs)

When APIs may be obfuscated, use `TargetMethod()`:

```csharp
[HarmonyPatch]
public class YourPatch
{
    static MethodBase TargetMethod()
    {
        var type = AccessTools.TypeByName("Full.Type.Name");
        var method = AccessTools.Method(type, "MethodName", new[] { typeof(ParamType) });
        return method;
    }
    
    static bool Prefix(...) { /* implementation */ }
}
```

### 5.6 Coding Conventions

- Prefer Prefix/Postfix over Transpiler
- Guard all engine objects with null/state checks
- Avoid allocations in per-frame/tick paths
- Make patches configurable via mod settings where appropriate
- Fail closed - fallback to normal behavior on errors

## 6. Development Standards

### 6.1 Code Style

- Consistent style via `.editorconfig`
- Keep classes <500 LOC
- No duplicate logic - utilities in `Mod.Core`
- Human-like comments explaining why, not what

### 6.2 Logging

**Categories**: Enlistment, Duties, Equipment, Patches, Config, Progression, Combat, Compatibility, Performance

**Levels**:
- **Error**: Critical failures only (primary use)
- **Info**: Essential events (rare - startup confirmation only)
- **Debug**: Development only (disabled by default)

**Strategy**: Silent success - only log failures. Zero performance impact during normal gameplay.

### 6.3 Commenting

Write natural, professional comments:

**Good**:
```csharp
// We check if the lord is alive before processing wages
// This prevents paying dead lords
if (_enlistedLord?.IsAlive == true) { ProcessWages(); }
```

**Avoid**:
```csharp
// Execute daily wage processing // Too robotic
```

## 7. Change Management

### Small Changes (default)
- Few lines across 1-3 files
- Include small tests when feasible
- One-sentence intent and expected effect

### Guided Refactor
- Sequence of small, reviewable changesets
- Preserve public behavior or provide shims

### Replacement
- New implementation behind feature flag
- Documented in ADR
- Define rollback steps

**Guardrails**: Respect game lifecycle, save-game safety, mod load order. Never remove safety checks.

## 8. Workflow

- **Branching**: Trunk-based, short-lived feature branches
- **Commits**: Conventional, descriptive messages
- **Versioning**: Semantic Versioning (SemVer)
- **Releases**: Tagged, packaged to `Modules/Enlisted/`

## 9. Configuration

- **Precedence**: In-game settings > user config > defaults
- **Validation**: Fail fast with actionable errors
- **Feature flags**: Document defaults, owners, rollback notes

## 10. Platform Constraints

- **Runtime**: Target game-supported .NET version
- **Load order**: Declare dependencies in `SubModule.xml`
- **Save-game**: Never remove serialized fields without migration plan
- **Localization**: Store strings in `ModuleData/localization/`

## 11. Critical Patterns

### Encounter Prevention
Use `MobileParty.MainParty.IsActive = false` engine property (more robust than patches).

### Deferred Operations
Use `NextFrameDispatcher.RunNextFrame()` to avoid timing conflicts during encounter exits. Hooks into `Campaign.Tick()` via `NextFrameDispatcherPatch`.

### Real-Time Processing
Use `CampaignEvents.TickEvent` for continuous state enforcement (works even when paused).

### Natural Attachment
Use `AttachedTo` property for party following. Expense isolation handled via Harmony patch.

## Appendix A: Build & Packaging

Build output location: `<BannerlordInstall>/Modules/Enlisted/`

**SubModule.xml** requirements:
```xml
<DependedModules>
  <DependedModule Id="Bannerlord.Harmony" />
</DependedModules>
```

**Harmony initialization** (`SubModule.cs`):
```csharp
_harmony = new Harmony("com.enlisted.mod");
_harmony.PatchAll(); // Auto-discovers all [HarmonyPatch] attributes
```

## Appendix B: Decompiled References

**Critical Rule**: Always use decompiled TaleWorlds code, never outdated mod source.

**Verification Checklist**:
- [ ] Decompiled current game version DLLs
- [ ] Verified static vs instance methods
- [ ] Confirmed parameter types and order
- [ ] Used `AccessTools.Method()` with parameter array
- [ ] Added error handling for missing methods

**Reference Location**: `C:\Dev\Enlisted\DECOMPILE\TaleWorlds.CampaignSystem\`

**Official API**: https://apidoc.bannerlord.com/v/1.2.12/
