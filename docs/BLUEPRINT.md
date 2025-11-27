# Blueprint

Architecture and development standards for the Enlisted military service mod.

## Quick Reference

### Build
- Visual Studio: config "Enlisted EDITOR" → Build
- CLI: `dotnet build -c "Enlisted EDITOR"`
- Output: `<BannerlordInstall>/Modules/Enlisted/`
- Note: Build warnings about locked DLLs are normal when Bannerlord is running

### Dependencies
```xml
<DependedModules>
  <DependedModule Id="Bannerlord.Harmony" />
</DependedModules>
```

### Harmony Initialization
```csharp
_harmony = new Harmony("com.enlisted.mod");
_harmony.PatchAll();
```

### Current Implementation

**15 Harmony Patches** (`src/Mod.GameAdapters/Patches/`):
1. BattleCommandsFilterPatch - Formation-based battle command filtering
2. ClanFinanceEnlistmentIncomePatch - Adds enlistment wages to daily gold tooltip
3. DischargePenaltySuppressionPatch - Prevents relation penalties during discharge
4. DutiesOfficerRolePatches - Officer role integration (Scout, Surgeon, Engineer, Quartermaster)
5. EncounterSuppressionPatch - Suppresses encounters with the lord when not in battle
6. EnlistmentExpenseIsolationPatch - Prevents expense sharing while enlisted
7. HidePartyNamePlatePatch - Hides player nameplate by nulling `PlayerNameplate` on `PartyNameplatesVM`
8. KingdomDecisionParticipationPatch - Blocks kingdom decision prompts
9. LootRestrictionPatch - Tier-based loot restrictions (non-officers blocked from loot)
10. NoHorseSiegePatch - Prevents mounted players from joining sieges
11. OrderOfBattleSuppressionPatch - Skips deployment screen for enlisted soldiers
12. PostDischargeProtectionPatch - Temporary immunity right after discharge
13. SkillSuppressionPatch - Blocks tactics/leadership XP during battles
14. VisibilityEnforcementPatch - Controls party visibility during battles and settlement transitions
15. VotingSuppressionPatch - Prevents voting prompts for enlisted soldiers

**Core Behaviors** (`src/Mod.Entry/SubModule.cs`):
- EnlistmentBehavior - Core service state, lord following, battle participation, veteran retirement system
- EnlistedDialogManager - Centralized dialog handling (enlist, leave, retirement, re-enlistment)
- EnlistedDutiesBehavior - Duties system with formation training
- EnlistedMenuBehavior - Professional menu interface with "Return to Camp" options
- EnlistedEncounterBehavior - Battle participation and encounter management

**Mission Behaviors** (registered via `SubModule.OnMissionBehaviorInitialize`):
- EnlistedKillTrackerBehavior - Tracks player kills during combat missions for XP and faction records

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
│   └── Patches/            # 15 Harmony patches
└── Features/               # Each feature is self-contained
    ├── Enlistment/         # Core service state management + veteran retirement
    ├── Assignments/        # Duties system and XP calculations
    ├── Equipment/          # Equipment management and troop selection
    ├── Ranks/              # Promotion tracking and wage calculation
    ├── Conversations/      # Dialog handling (including retirement dialogs)
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

**Enlistment**: Tracks lord service relationship, veteran status, and grace periods (`src/Features/Enlistment/Behaviors/`)

**Veteran Retirement System**: Per-faction service tracking with:
- `FactionVeteranRecord` class storing tier, kills, cooldowns, term status
- First term (252 days) with full benefits
- Renewal terms (84 days) with discharge/continue bonuses
- 6-month (42 day) faction cooldown after retirement
- Tier preservation for re-enlistment

**Grace Period System**: Unified 14-day grace for:
- Lord killed
- Lord captured
- Army defeated
- Leave expired

**Assignments**: Handles daily duties and benefits (`src/Features/Assignments/Behaviors/`)

**Equipment**: Manages gear access based on rank/culture (`src/Features/Equipment/Behaviors/`)

**Battle Integration**:
- Uses escort AI (`SetMoveEscortParty`) for following lord on the map
- `MobileParty.IsVisible = false` hides 3D party model while following
- Player joins battles via `MapEventSide` assignment when lord engages
- +25 XP awarded per battle participation
- Autosim defeats fall back to vanilla behavior (player chooses Attack/Surrender)
- Formation-based command filtering via `BattleCommandsFilterPatch`

**Note**: Dynamic mission behavior addition (EnlistedKillTrackerBehavior, EnlistedFormationAssignmentBehavior) is disabled due to crash issues when adding behaviors to running missions.

**Settlement Integration**:
- Player automatically follows lord into settlements via native escort behavior
- When lord leaves settlement, player is forced out via `PlayerEncounter.LeaveSettlement()`
- "Return to Camp" menu option added to town/castle menus for manual exit

**Menu Integration**:
- Custom menu with service status, wages, progression
- Retirement dialog flow with benefits explanation
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
- **Avoid** adding MissionBehaviors dynamically during missions (causes crashes)

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

## 6. TaleWorlds API Patterns

**Official API**: https://apidoc.bannerlord.com/v/1.3.5/

**Decompiled Reference**: `C:\Dev\Enlisted\DECOMPILE\TaleWorlds.CampaignSystem\`

Always verify API calls against decompiled code, not external sources.

### 6.1 Image Identifiers

`ImageIdentifierVM` and `ImageIdentifier` are abstract. Use concrete subclasses:

```csharp
new ItemImageIdentifierVM(item, "")
new CharacterImageIdentifierVM(CharacterCode.CreateFrom(troop))
new CharacterImageIdentifier(CharacterCode.CreateFrom(troop))  // For InquiryElement
```

### 6.2 Gauntlet UI

```csharp
var layer = new GauntletLayer("MyLayer", 1001, false);
GauntletMovieIdentifier movie = layer.LoadMovie("MovieName", viewModel);
layer.ReleaseMovie(movie);
```

### 6.3 Kingdom Actions

```csharp
ChangeKingdomAction.ApplyByJoinToKingdom(clan, kingdom, default(CampaignTime), showNotification);
```

### 6.4 Information Messages

```csharp
MBInformationManager.AddQuickInformation(text, duration, character, null, soundId);
```

### 6.5 MobileParty

```csharp
party.SetMoveModeHold();
party.SetMoveEscortParty(targetParty, MobileParty.NavigationType.Default, false);
Vec2 position = party.GetPosition2D;
float weight = party.TotalWeightCarried;
```

### 6.6 Text and Properties

```csharp
TextObject empty = TextObject.GetEmpty();
float strength = army.EstimatedStrength;
```

### 6.7 Menu System

```csharp
GameMenu.MenuOverlayType.None
```

### 6.8 Module Discovery

```csharp
var subModules = Module.CurrentModule.CollectSubModules();
```

### 6.9 Relation Changes

```csharp
ChangeRelationAction.ApplyPlayerRelation(hero, relationChange, affectRelatives, showNotification);
```

### 6.10 Gold Actions

```csharp
GiveGoldAction.ApplyForCharacterToParty(null, MobileParty.MainParty.Party, amount, true);
```

## 7. Development Standards

### 7.1 Code Style

- Consistent style via `.editorconfig`
- Keep classes <500 LOC
- No duplicate logic - utilities in `Mod.Core`
- Human-like comments explaining why, not what

### 7.2 Logging

**Categories**: Enlistment, Duties, Equipment, Patches, Config, Progression, Combat, Compatibility, Performance, Retirement, Leave, SaveLoad

**Levels**:
- **Error**: Critical failures only (primary use)
- **Info**: Essential events (rare - startup confirmation only)
- **Debug**: Development only (disabled by default)

**Strategy**: Silent success - only log failures. Zero performance impact during normal gameplay.

### 7.3 Commenting

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

## 8. Change Management

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

## 9. Workflow

- **Branching**: Trunk-based, short-lived feature branches
- **Commits**: Conventional, descriptive messages
- **Versioning**: Semantic Versioning (SemVer)
- **Releases**: Tagged, packaged to `Modules/Enlisted/`

## 10. Configuration

- **Precedence**: In-game settings > user config > defaults
- **Validation**: Fail fast with actionable errors, bounded ranges enforced
- **Feature flags**: Document defaults, owners, rollback notes

### Configurable Settings (enlisted_config.json)

| Setting | Default | Valid Range | Purpose |
|---------|---------|-------------|---------|
| `gameplay.reserve_troop_threshold` | 100 | 20-500 | Minimum troops for "Wait in Reserve" |
| `gameplay.desertion_grace_period_days` | 14 | 1-90 | Days to find new lord after discharge |
| `gameplay.leave_max_days` | 14 | 1-90 | Maximum days on leave before desertion |
| `finance.show_in_clan_tooltip` | true | bool | Show wages in Daily Gold tooltip |
| `retirement.first_term_days` | 252 | - | Days required for first-term retirement |
| `retirement.renewal_term_days` | 84 | - | Duration of renewal terms |
| `retirement.cooldown_days` | 42 | - | Faction cooldown after retirement |
| `retirement.first_term_gold` | 10000 | - | Gold bonus for first-term retirement |
| `retirement.first_term_reenlist_bonus` | 20000 | - | Gold bonus for re-enlisting after first term |
| `retirement.renewal_discharge_gold` | 5000 | - | Gold for renewal term discharge |
| `retirement.lord_relation_bonus` | 30 | - | Relation gain with retiring lord |
| `retirement.faction_reputation_bonus` | 30 | - | Faction reputation gain |
| `retirement.other_lords_relation_bonus` | 15 | - | Relation gain with other faction lords |
| `retirement.other_lords_min_relation` | 50 | - | Minimum relation required for bonus |

### Supported Cultures

Eight cultures with settlement ownership:
- **Main**: empire, aserai, sturgia, vlandia, battania, khuzait
- **Secondary**: nord (uses Sturgian troops), darshi (uses Aserai troops)

Configuration files support all eight cultures.

## 11. Platform Constraints

- **Runtime**: Target game-supported .NET version
- **Load order**: Declare dependencies in `SubModule.xml`
- **Save-game**: Never remove serialized fields without migration plan
- **Localization**: Store strings in `ModuleData/localization/`

### Save System Considerations

Custom classes cannot be serialized directly via `dataStore.SyncData()`. Use manual serialization:

```csharp
// Instead of: dataStore.SyncData("_records", ref _records);
// Use indexed primitive fields:
int count = _records.Count;
dataStore.SyncData("_records_count", ref count);
for (int i = 0; i < count; i++)
{
    string id = _records[i].Id;
    int value = _records[i].Value;
    dataStore.SyncData($"_records_{i}_id", ref id);
    dataStore.SyncData($"_records_{i}_value", ref value);
}
```

## 12. Critical Patterns

### Party Following (Escort AI)
Use `MobileParty.SetMoveEscortParty(lordParty, NavigationType.Default, false)` for following.
Keep `IsActive = true` but use `IgnoreByOtherPartiesTill()` to prevent random encounters.

### Party Visibility
Set `MobileParty.IsVisible = false` to hide the 3D party model on the campaign map.
The native `MobilePartyVisual` system handles fading automatically.

### Nameplate Hiding
Patch `PartyNameplatesVM.Update()` postfix to call `PlayerNameplate.Clear()` and set `PlayerNameplate = null`.
This mimics the game's own settlement-entry behavior and completely removes the nameplate widget.

### Battle Participation
Set `mainParty.Party.MapEventSide = targetSide` to join an existing `MapEvent`.
Create `PlayerEncounter.Start()` if needed for the native encounter menu.

### Settlement Transitions
Use `PlayerEncounter.LeaveSettlement()` and `PlayerEncounter.Finish(true)` when forcing player out.
Schedule via `NextFrameDispatcher.RunNextFrame()` to avoid timing conflicts.

### Deferred Operations
Use `NextFrameDispatcher.RunNextFrame()` to avoid timing conflicts during encounter exits.
Hooks into `Campaign.Tick()` via `NextFrameDispatcherPatch`.

### Real-Time Processing
Use `CampaignEvents.TickEvent` for continuous state enforcement (works even when paused).

### Campaign Safety
Use `CampaignSafetyGuard` to safely access `Hero.MainHero`, `MobileParty.MainParty`, etc.
Returns null instead of throwing during character creation or uninitialized states.

### Manual Harmony Patching
For patches that reference types not available during character creation (like `PartyNameplateVM`),
use manual patching via `harmony.Patch()` deferred until after campaign initialization.

### Mission Behavior Warning
**Do NOT add MissionBehaviors dynamically** during `AfterMissionStarted` or similar events.
This causes crashes because lifecycle methods get called in unstable states.
Use campaign events instead (OnMapEventEnded, OnPlayerBattleEnd) for battle tracking.

### API Verification

Always verify against decompiled code:
- [ ] Decompiled current game version DLLs
- [ ] Verified static vs instance methods
- [ ] Confirmed parameter types and order
- [ ] Used `AccessTools.Method()` with parameter array
- [ ] Added error handling for missing methods
