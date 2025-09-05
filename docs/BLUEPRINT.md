# Blueprint

Vanilla starting point for a Bannerlord module.

Repo layout (baseline)
```
Enlisted/
├── Enlisted.sln
├── README.md
├── SubModule.xml
└── src/
    └── Mod.Entry/
        └── SubModule.cs
```

Build
- Visual Studio: config "Enlisted EDITOR" → Build
- CLI: `dotnet build -c "Enlisted EDITOR"`

Next steps
- Add your features under `src/` and reference them in `Enlisted.csproj`.

## Current implementation snapshot

- Entry wiring in `src/Mod.Entry/SubModule.cs` creates a Harmony instance and registers campaign behaviors:
  - `Enlisted.Debugging.Discovery.Application.DiscoveryBehavior` – logs menus, settlements, session
  - `Enlisted.Features.LordDialog.Application.LordDialogBehavior` – dialog entry points
  - `Enlisted.Debugging.Discovery.Application.ApiDiscoveryBehavior` – dumps API surface on session launch
- Discovery lives under `src/Debugging` (not under `Features`). Aggregates are written to the module `Debugging` folder.

### Debugging outputs (session-scoped)

Location: `…/Modules/Enlisted/Debugging/`
- `enlisted.log` – bootstrap, init details
- `discovery.log` – menu opens, settlement events
- `dialog.log` – dialog availability/selection (CampaignGameStarter + DialogFlow)
- `api.log` – menu transition API notes
- `attributed_menus.txt` – unique menu ids
- `dialog_tokens.txt` – unique dialog tokens
- `api_surface.txt` – reflection dump of public surfaces

These files are cleared on init to keep one session at a time.

## 1. Purpose & Scope

This blueprint is the single source of truth for how the Bannerlord mod is organized, built, tested, released, and supported in Visual Studio 2022. It is self-serving: an assistant (or contributor) can follow it without additional instructions.

It defines:

- Minimal root layout and Package-by-Feature structure for C# solutions
- Mod packaging rules (Module folder, SubModule.xml, assets)
- Development, commenting, testing, CI, and release standards
- Platform constraints (Windows, game runtime)
- Change Levels and discretion rules (small, reversible changes first)
- Harmony patching policy for safe extension of TaleWorlds APIs

## 2. Guiding Principles

- Correctness first, speed second. Prefer safe, deterministic behavior over cleverness.
- Determinism over flakiness. Use explicit gates, bounded waits, and idempotent logic.
- Make it observable. Emit structured logs for non-trivial paths.
- Fail closed. On uncertainty, do the safe thing and record why.
- Config over code. Prefer config flags and mod settings to hard-coding behavior.
- Small, reversible changes. Short branches, focused reviews, easy rollbacks.
- Player empathy. Errors and warnings offer next steps.
- Respect the platform. Honor module rules, game API constraints, and save-game safety.

## 3. High-Level Architecture

- Package-by-Feature (C#). Each gameplay feature has its own subtree (domain logic + view-models + assets if relevant).
- Mod Entry & Wiring. A thin entry layer integrates with Bannerlord's module lifecycle (SubModule.xml, load order) and routes to feature modules.
- Separation of Concerns.
  - Decision: feature logic determines what to do (e.g., adjust troops, UI state).
  - Actuation: adapters call TaleWorlds APIs to apply changes safely.
- Integration Boundary. A single "game adapters" layer isolates TaleWorlds types, Harmony patches, and game events from core logic.
- Observability. Centralized logging with stable categories and correlation (e.g., session GUID).

## 4. Project Structure (Visual Studio 2022)

### 4.1 Minimal Root (authoritative)

Only essentials at repo root:

```
BannerlordMod/
├── README.md
├── LICENSE
├── BannerlordMod.sln            # Visual Studio 2022 solution
├── .gitignore
├── .editorconfig
├── .gitattributes               # optional
├── .github/                     # CI workflows, PR/issue templates, CODEOWNERS
└── .aipolicy.yml                # optional, small policy file (text-based)
```

### 4.2 Source Layout (Package-by-Feature)

All implementation lives under `src/`. Each feature keeps its own logic and tests close by.

```
src/
├── Mod.Entry/                   # Thin module entry + packaging glue
│   ├── Properties/
│   └── (entry wiring, module init/shutdown hooks)
│
├── Mod.Core/                    # Shared services, policies, logging, DI
│
├── Mod.GameAdapters/            # TaleWorlds APIs, Harmony patches, event bridges
│   └── Patches/                 # All Harmony patches live here (see 4.2.1)
│
├── Features/                    # Each feature is self-contained
│   ├── Enlistment/
│   │   ├── Core/                # basic rules and validation
│   │   └── Behaviors/           # main enlistment logic and state
│   ├── Assignments/
│   │   ├── Core/                # assignment rules and XP calculations  
│   │   └── Behaviors/           # daily assignment processing
│   ├── Equipment/
│   │   ├── Core/                # gear rules and tier requirements
│   │   ├── Behaviors/           # equipment management and selection
│   │   └── UI/                  # custom gear selector (if needed)
│   ├── Ranks/
│   │   ├── Core/                # promotion rules and tier logic
│   │   └── Behaviors/           # rank tracking and wage calculation
│   ├── Conversations/
│   │   └── Behaviors/           # dialog handling and flows
│   ├── Combat/
│   │   └── Behaviors/           # battle participation and army following
│   └── Interface/
│       └── Behaviors/           # status menus and player interface
│
└── Mod.Config/                  # strongly-typed settings & validation
```

#### 4.2.1 Harmony Patching Policy

**Scope and placement**

- All Harmony patches against TaleWorlds types must be placed in `src/Mod.GameAdapters/Patches/`.
- One class per target patch; name by feature + target (e.g., `EconomyCampaignBehavior_DailyTickPatch.cs`).
- Patch classes must declare scope with `[HarmonyPatch]` attributes.

**When to use Harmony**

- Intercept or extend TaleWorlds methods that are sealed, internal/private, or engine-invoked.
- When critical side effects cannot be reached via public APIs or CampaignBehavior hooks.
- **Officer Role Substitution**: Essential for duties system - patch `MobileParty.EffectiveX` properties to substitute player as party officer for natural skill/perk integration.
- It is acceptable to patch menu/time control, encounter/battle flows, and dispatcher surfaces when required by feature design, provided patches are guarded, observable, and configurable.
- Examples of engine-invoked surfaces: module load/unload lifecycle, campaign daily/hourly ticks, battle/agent updates, menu open/close, economy/party recalculations.

**When not to use Harmony**

- Do not place domain logic in patches; keep orchestration and configuration in feature services.
- Prefer CampaignBehavior, event hooks, and public APIs when straightforward; however, extensive Harmony usage is acceptable where it simplifies integration with engine-invoked behavior. Document assumptions and gate via settings where appropriate.

**Documentation standard (mandatory)**

Every patch must start with a structured header comment:

```csharp
// Harmony Patch
// Target: <Namespace.Type.MethodSignature>
// Why: <brief rationale — what behavior needs adjusting>
// Safety: <scope guards, null checks, campaign-only, constraints>
// Notes: <optional performance or compatibility notes>
```

**Coding conventions (following [Bannerlord Modding best practices](https://docs.bannerlordmodding.lt/modding/harmony/))**

- Prefer Prefix/Postfix; use Transpiler only when behavior cannot be achieved otherwise, and document the IL assumptions (target instruction patterns, invariants).
- **Use HarmonyPriority for patch ordering**: `[HarmonyPriority(999)]` for high priority, lower numbers for lower priority.
- **Specify method signatures for ambiguous matches**: Use `[HarmonyPatch(typeof(Type), "Method", typeof(param1), typeof(param2))]` when multiple overloads exist.
- **Property patching**: For properties, specify `MethodType.Getter` or `MethodType.Setter`: `[HarmonyPatch(typeof(MobileParty), "EffectiveEngineer", MethodType.Getter)]`
- Suffix class names with `Patch`. Use explicit method names `Prefix`, `Postfix`, and `Transpiler` only when necessary.
- Guard all engine objects with null checks and state checks; avoid assumptions about menu/campaign state.
- Respect performance: avoid allocations in per-frame/tick paths; keep reflection minimal and cached if needed.
- Make patches configurable/gated via mod settings where appropriate; fail closed if settings invalid.

**Observability**

- Use the centralized logging service with category `GameAdapters` (or feature category) and include session correlation.
- Avoid spamming logs in high-frequency hooks; log at Info/Warning/Error with budgets.

**Compatibility and load order**

- Declare dependency on `Bannerlord.Harmony` in `SubModule.xml` (runtime provides 0Harmony). See Appendix B.
- Target x64 platform to align with TaleWorlds assemblies.

### 4.3 Module (Game) Packaging Layout

The build outputs must produce the Bannerlord Modules layout (do not keep it in repo; it's a build artifact). Typical target:

```
<BannerlordInstall>/Modules/<YourModName>/
├── SubModule.xml
├── bin/Win64_Shipping_Client/
│   └── YourModName.dll (+ referenced mod DLLs if needed)
├── ModuleData/                  # XML data, config, strings
├── GUI/                         # sprites, prefabs (if used)
└── AssetPackages/               # asset bundles (if used)
```

**Rule:** Author the source in `src/` and `assets/`, then use post-build steps (or a packaging script) to copy DLLs, SubModule.xml, and content into the game's `Modules/<YourModName>/` folder.

### 4.4 Supporting Directories

```
assets/                           # Art, icons, XML templates, localization
docs/                             # ARCHITECTURE.md, RUNBOOK.md, TESTING.md, SECURITY.md, ADRs
tests/                            # Cross-feature tests (optional if colocated)
tools/                            # Packaging helper scripts, VS build tasks
```

If your `.gitignore` mentions `.artifacts/patches/` from earlier drafts, remove it (we don't store patch files).

## 4.5 Campaign Menu & Time-Control Policy (authoritative)

- Menus created via `CampaignGameStarter.AddGameMenu/ AddWaitGameMenu` must not break map time controls.
- Pattern to keep the game unpaused while a menu panel is open (Freelancer/SAS-style):
  1. Finish any active encounter before switching menus: `if (PlayerEncounter.Current != null) PlayerEncounter.Finish(true);`
  2. Drain stray menus: loop `GameMenu.ExitToLast()` a few times until `CurrentMenuContext` is null.
  3. Open your menu via `GameMenu.ActivateGameMenu(id)` or `GameMenu.SwitchToMenu(id)` depending on context.
  4. Set time to `CampaignTimeControlMode.StoppablePlay`.
  5. In the menu `OnInit` handler, call `args.MenuContext.GameMenu.StartWait();` and then `Campaign.Current.GameMenuManager.RefreshMenuOptions(...)`.
- Use `GameOverlays.MenuOverlayType.None` and `GameMenu.MenuFlags.None` unless you intentionally need overlays/flags.
- This ensures the top-left ribbon (spacebar/arrow time controls) works while the panel is expanded and the panel can be collapsed via the chevron without pausing.

### 4.6 Military Service System Architecture

Our military service system follows a modular design where each aspect has clear responsibilities:

#### **Enlistment Feature**
- **What it does**: Tracks who you're serving and manages the basic service relationship
- **Main job**: Keep track of your current lord and make sure you're following them properly
- **Lives in**: `src/Features/Enlistment/Behaviors/`
- **Keep it simple**: Just handle the basics - who, when, and basic state

#### **Assignments Feature** 
- **What it does**: Handles your daily military duties and the benefits you get from them
- **Main job**: Process your role (cook, guard, sergeant, etc.) and give you the right rewards
- **Lives in**: `src/Features/Assignments/Behaviors/`
- **Make it meaningful**: Each job should feel different and worthwhile

#### **Equipment Feature**
- **What it does**: Manages what gear you can access based on your rank and culture
- **Main job**: Make sure you get appropriate equipment that matches your status and faction
- **Lives in**: `src/Features/Equipment/Behaviors/`
- **Progression matters**: Better ranks should unlock cooler gear, but you have to earn it

#### **Battle Integration**
- **Purpose**: Seamless participation in lord's military campaigns
- **Implementation Strategy**:
  - Use escort AI to follow the enlisted lord: `MobileParty.MainParty.Ai.SetMoveEscortParty(lordParty)`
  - When lord is in an army, follow the army leader instead for proper hierarchy
  - Auto-join battles when lord is involved, using reflection with positioning fallback
  - Handle army formation changes gracefully without breaking player experience

#### **Menu Integration**
- **Purpose**: Rich information display and management interface
- **Implementation Strategy**:
  - Custom menu shows service status, wages, progression, and army information
  - Menu guards prevent player-initiated encounters while enlisted
  - Settlement following ensures player stays with lord during town visits
  - Time control management maintains proper game flow

### 4.7 Deferred Operations (assert safety)

- Post-load setup is deferred until there is no active menu or encounter, and a short safety timer elapses. Then re-apply:
  - Escort AI toward commander (or army leader)
  - Visual tracker registration
  - Camera follow to commander party
- Post-battle restore is likewise deferred until encounter/menus clear. Then:
  - Re-hide and deactivate `MainParty`
  - Re-apply escort/camera follow
  - Optionally re-open the enlisted status menu
- Emit clear debug markers when deferred vs applied:
  - "PostLoadSetup deferred/applied"
  - "PendingCameraFollow deferred/applied"
  - "PostBattleRestore deferred/applied"

### 4.8 Camera Follow, Visual Tracking, and Visibility

- Camera follow cadence: reassert follow to `(commanderArmyLeader ?? commander)` frequently to keep camera locked even if the engine resets it.
- Visuals and tracker
  - Keep `MobileParty.MainParty.IsVisible = false` while enlisted.
  - Unregister `MainParty` from `VisualTrackerManager` and register the commander to drive HUD focus; periodically enforce due to engine/UI refreshes.
  - Nameplate & tracker suppression via small Harmony adapters (see ADR-011), with behavior-level enforcement as backup.

### 4.9 Conditional Ignore AI Safety

- To prevent world AI from targeting the hidden `MainParty`, periodically call:
  - `MobileParty.MainParty.IgnoreByOtherPartiesTill(CampaignTime.Now + CampaignTime.Hours(0.5f))`
- Use conditionally: enable while the commander is not in an army; disable when merged into an army to avoid unintended targeting behavior changes.

## 5. Development Standards (C# / VS2022)

- Language/Runtime: Use the game-compatible .NET target (align with current Bannerlord runtime; when unsure, prefer the lowest known compatible target).
- Solution Hygiene: One solution file; each project compiles cleanly; treat warnings as meaningful.
- Style & Linting: Enforce consistent style via `.editorconfig`. Keep namespaces/folders aligned.
- Types & Nullability: Enable nullable reference types where compatible; prefer explicit types on public APIs.
- Logging: Use our centralized ModLogger with feature-specific categories. Structure logs for production troubleshooting and performance monitoring.
- Module/Class Size: Keep classes reasonable (aim <500 LOC). Split by single responsibility when growing.
- No duplicate logic: Cross-feature utilities live in `Mod.Core`; TaleWorlds/Harmony specifics live in `Mod.GameAdapters`.
- Harmony safety: Keep patches minimal, guarded, and reversible; never remove safety checks to "make tests pass."

## 6.3 Production Logging Standards

### Logging Levels and Usage

**Error Level** - Critical failures that need attention (primary use):
```csharp
ModLogger.Error("Config", "Configuration loading failed - using defaults", ex);
ModLogger.Error("Compatibility", "Mod initialization failed - duties unavailable", ex);
ModLogger.Error("Equipment", "Equipment kit application failed", ex);
```

**Info Level** - Minimal essential events (rare use):
```csharp
ModLogger.Info("Init", "Duties system loaded"); // Startup confirmation only
// Avoid info logging for routine operations - use in-game notifications instead
```

**Debug Level** - Disabled by default (development only):
```csharp
// Only when specifically enabled for troubleshooting
// Most operations should be silent for smooth user experience
```

### Performance-Friendly Logging

- **Silent Success Pattern**: Normal operations don't log - use in-game notifications for user feedback
- **Error-Only Strategy**: Only log when something fails or breaks
- **Minimal File I/O**: Typically 0-2 log entries per game session
- **Zero Performance Impact**: No logging overhead during normal gameplay
- **Exception Context**: Always include error context for troubleshooting
- **Session Correlation**: Unique session ID for support issue tracking

### Troubleshooting Categories

- **"Enlistment"** - Core service state and lord relationships
- **"Duties"** - Duties system: troop types, duty assignments, officer roles, configuration loading
- **"Equipment"** - Equipment kits, gear application, culture + troop type + tier matching
- **"Patches"** - Harmony patch success/failure, officer role substitution, XP sharing
- **"Config"** - Configuration loading, JSON parsing, fallback to XML, validation errors
- **"Progression"** - Tier advancement, XP tracking, duty slot unlocking, specialization changes
- **"Combat"** - Battle participation and army integration
- **"Compatibility"** - Game updates, mod conflict detection, API validation failures
- **"Performance"** - Slow operations and optimization opportunities

## 6. Professional Human-Like Commenting Standards

**Goal:** Comments should sound natural and professional, like a colleague explaining their approach.

### 6.1 Human-Like Comment Style

**Good Examples:**
```csharp
// We need to check if the lord is still alive before processing daily benefits
// This prevents the system from trying to pay wages to dead lords
if (_enlistedLord?.IsAlive == true)
{
    ProcessDailyWages();
}

// The tier system works like military ranks - higher tiers unlock better assignments
// We cap it at tier 7 since that represents the highest non-officer rank
while (_enlistmentTier < 7 && _enlistmentXP >= GetTierRequirement(_enlistmentTier + 1))

// Equipment selection is culture-based because different factions have different gear styles
// Empire uses Roman-style equipment, while Vlandia prefers medieval Western gear
var availableGear = GetCultureAppropriateEquipment(lord.Culture, playerTier);
```

**Avoid Robotic Comments:**
```csharp
// BAD: "Execute daily tick processing for enlisted behavior"
// GOOD: "Handle daily military duties like wage payments and skill training"

// BAD: "Validate enlistment state parameters"  
// GOOD: "Make sure we're still properly enlisted before doing anything"

// BAD: "Apply equipment selection algorithm"
// GOOD: "Pick gear that matches the lord's culture and the player's rank"
```

### 6.2 Comment Content Guidelines

**Explain Why, Not What:**
- ✅ **Intent**: "We use escort AI instead of direct attachment because it's more reliable"
- ✅ **Constraints**: "Only process this during campaign mode since battles don't have lords"
- ✅ **Safety**: "Double-check the lord exists since they might have died in battle"
- ✅ **Context**: "This matches how real military promotions work in medieval times"

**Human Context:**
- ✅ "This feels a bit hacky, but it's the only way to detect army disbandment"
- ✅ "We learned this approach from analyzing how the base game handles similar situations"
- ✅ "Players expect this to work like real military service, so we mirror that experience"

**Prohibited:**
- ❌ References to "SAS" or "ServeAsSoldier mod"
- ❌ "This was changed on [date]" or ticket references
- ❌ Overly technical jargon without explanation
- ❌ Comments that just repeat the code

### 6.1 Harmony Patch Commenting Standard

Each Harmony patch must include the header described in 4.2.1. Additionally:

- Indicate whether the patch runs in campaign-only, battle-only, or both.
- List the minimal set of assumptions about object lifetimes (e.g., PartyComponent may be null).
- Note configuration gates (feature flags, settings) and default behavior when disabled.
- Include a brief performance note if the patch is on a frequent path (tick, per-frame, menu draw).

**Modern example following [Bannerlord Modding best practices](https://docs.bannerlordmodding.lt/modding/harmony/)**:

```csharp
// Harmony Patch
// Target: TaleWorlds.CampaignSystem.Party.MobileParty.EffectiveEngineer { get; }
// Why: Make player the effective engineer when assigned to Siegewright's Aide duty for natural skill/perk benefits
// Safety: Campaign-only; checks enlistment state; validates duty assignment; only affects enlisted lord's party
// Notes: Property getter patch; high priority to run before other mods; part of duties system officer role integration

[HarmonyPatch(typeof(MobileParty), "EffectiveEngineer", MethodType.Getter)]
[HarmonyPriority(999)] // High priority - run before other mods
[HarmonyBefore(new string[] { "other.mod.id" })] // Run before conflicting mods if needed
public class DutiesEffectiveEngineerPatch
{
    static bool Prefix(MobileParty __instance, ref Hero __result)
    {
        try
        {
            // Guard: Verify all required objects exist
            if (EnlistmentBehavior.Instance?.IsEnlisted != true || 
                __instance == null || 
                EnlistmentBehavior.Instance.CurrentLord?.PartyBelongedTo != __instance)
            {
                return true; // Use original behavior
            }
            
            // Guard: Verify duty assignment
            if (DutiesBehavior.Instance?.HasActiveDutyWithRole("Engineer") != true)
            {
                return true; // Use original behavior
            }
            
            // Substitute player as effective engineer
            __result = Hero.MainHero;
            return false; // Skip original method - player's Engineering skill now affects party
        }
        catch (Exception ex)
        {
            ModLogger.Error("Patches", $"EffectiveEngineer patch error: {ex.Message}");
            return true; // Fail safe - use original behavior
        }
    }
}
```

Legacy example:
```csharp
// Harmony Patch  
// Target: TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors.EconomyCampaignBehavior.DailyTick()
// Why: Adjust economy tick multipliers for player-configurable scaling
// Safety: Campaign-only; null-check settlements; exits early if settings disabled
// Notes: Logs at Debug when enabled; avoids allocations per tick
```

## 7. Change Levels (with discretion)

### 7.1 Small Changeset (default)

- Small, focused edits (roughly a few lines across 1–3 files).
- Include/update a small test in the same feature when feasible.
- No file-wide rewrites; avoid unrelated formatting.
- Provide a one-sentence intent and expected effect.

### 7.2 Guided Refactor

Use when small changesets aren't sufficient (recurring defects, high complexity, cross-file consistency fixes, algorithmic need for performance).

- Deliver as a sequence of small, reviewable changesets — not a big-bang rewrite.
- Preserve public behavior or provide shims + deprecation notes if behavior changes.

### 7.3 Replacement

- New implementation behind a feature flag (mod setting) and recorded in an ADR.
- Prefer staged enablement; define rollback steps; compare against performance budgets.

**Guardrails (all levels):** Respect game lifecycle, save-game safety, and mod load order. Keep decision vs actuation separation; never remove safety checks to "make tests pass."

## 8. Workflow & Versioning

- Branching: Trunk-based; short-lived feature branches.
- How edits happen: The AI proposes small, focused changesets on your feature branch; you review/approve each changeset, then open a PR to main.
- Commits: Conventional, descriptive messages that capture why and what.
- Versioning: Semantic Versioning (SemVer).
- Releases: Tag releases; package to `Modules/<YourModName>/` and (optionally) Workshop/Nexus with notes.
- Rollbacks: Keep releases reversible (previous DLL + SubModule.xml kept). Feature flags help gate risky changes.

## 9. Testing & CI/CD (policy-level)

- Unit tests: Feature domain calculations, configuration validation, utility classes (avoid TaleWorlds types where possible).
- Integration tests: Limited; test through adapter abstractions. Where not feasible, rely on manual smoke tests defined in `docs/TESTING.md`.
- E2E/Smoke: Launch sequence sanity via in-game checklist.
- Performance checks: Watch frame-time; avoid per-frame heavy allocations/reflection.
- CI gates: build + quick tests + packaging validation (structure, presence of SubModule.xml, DLL output).
- Artifacts on failure: produce concise log summaries to speed diagnosis.

## 10. Configuration & Mod Settings

- Precedence: in-game settings (if provided) > user config files > defaults.
- Validation: validate on load; fail fast with actionable errors (e.g., "Invalid multiplier; expected 0.1–10").
- Feature flags: document default, owner, rollback notes in a small registry (e.g., `Mod.Config`).

## 11. Observability & Support

- Structured logs: stable categories (e.g., `Discovery`, `Dialog`, `Api`).
- Minimal artifacts: keep only what aids diagnosis (e.g., config snapshot, version info).
- Session bundles: clear folder for logs to share when reporting issues.
- Support loop: reproduce → collect bundle → fix → capture learnings in `docs/changes/` or an ADR (if architectural).

Discovery mode defaults: enabled during early phases for observability; may be set to off in `ModuleData/Enlisted/settings.json` for normal play. Optional future: console toggle (e.g., `enlisted.debug menus on`).

## 12. Performance & Reliability Budgets (examples)

- Per-frame work: avoid heavy allocations, reflection loops, or IO.
- Menu/opening hooks: ~≤200 ms perceived delay.
- Campaign tick logic: bounded and amortized; no unbounded loops.
- Save-game safety: persistent data changes are backward-compatible or gated.

(Budgets are living targets; update as features evolve.)

## 13. Platform Constraints (Bannerlord + Windows)

- Runtime compatibility: target the game's supported .NET/runtime; avoid APIs newer than the game provides.
- Module load order: declare dependencies in SubModule.xml; don't assume other mods' presence or order.
- Save-game compatibility: never remove serialized fields without a migration plan.
- Anti-cheat/MP: if relevant, respect multiplayer fairness and server policy.
- Localization: store strings in ModuleData/localization files where appropriate.

## 14. Asset & Data Governance

- Source of truth: author XML data, localization, and assets in `assets/` (and docs where appropriate).
- Packaging: copy into `ModuleData/`, `GUI/`, `AssetPackages/` during build.
- Validation: basic schema checks for XML where feasible; clear in-game error messages on bad data.

## 15. Incident Response & Hotfix Protocol

- Severity: S1 (crash/blocker), S2 (feature broken), S3 (degraded).
- Hotfix: branch from main → focused change(s) → rebuild/retag → replace bin/ DLL in module folder.
- Postmortem: short write-up of cause, detection gap, and guardrail added (store in `docs/changes/`).

## 16. Security & Player Privacy

- No unexpected data collection.
- Minimal logging by default; redact personal paths/IDs if logged.
- Third-party libs: prefer stable, well-known libraries; note licenses in README.

## 17. Discretion & Professional Judgment

These standards set strong defaults without over-prescribing tactics. When evidence suggests a better path, exercise judgment if you:

1. Briefly explain the rationale,
2. Preserve safety and reversibility, and
3. Record significant architectural shifts in an ADR with migration/rollback steps.

## Appendix A — Practical Build & Packaging Rules (non-code)

- Build output location: Configure post-build to copy the compiled DLL(s), SubModule.xml, and content to `<BannerlordInstall>/Modules/<YourModName>/`.
- SubModule.xml hygiene: Declare name, version, dependencies, and load order; keep it minimal and accurate.
- Do not commit build outputs. The `Modules/<YourModName>/` tree is a generated artifact, not part of the repo.
- Keep root clean. Only solution/metadata files and small policy/config files at root. Everything else organized under `src/`, `assets/`, `docs/`, `tools`.

## Appendix B — Harmony Runtime & Packaging Notes

- SubModule.xml dependency (explicit example):

```xml
<DependedModules>
  <DependedModule Id="Bannerlord.Harmony" />
</DependedModules>
```

- Compile-time 0Harmony reference strategy (optional; prefer runtime from Bannerlord.Harmony). If added for IntelliSense, ensure it is not copied:

```xml
<ItemGroup>
  <Reference Include="0Harmony">
    <HintPath>..\..\..\Bannerlord\Modules\Bannerlord.Harmony\bin\Win64_Shipping_Client\0Harmony.dll</HintPath>
    <Private>false</Private>
  </Reference>
</ItemGroup>
```

- Harmony ID stability: create a stable Harmony instance in Mod.Entry and call PatchAll at module load so patches can be unapplied/diagnosed later.

**Modern SubModule initialization following [Bannerlord Modding standards](https://docs.bannerlordmodding.lt/modding/harmony/)**:

```csharp
using HarmonyLib;

public class SubModule : MBSubModuleBase
{
    protected override void OnSubModuleLoad()
    {
        base.OnSubModuleLoad();
        
        try
        {
            Harmony harmony = new Harmony("com.enlisted.mod");
            harmony.PatchAll();
            // Silent success - only log failures
        }
        catch (Exception ex)
        {
            ModLogger.Error("Compatibility", "Harmony initialization failed", ex);
        }
    }
    
    protected override void OnGameStart(Game game, IGameStarter gameStarter)
    {
        base.OnGameStart(game, gameStarter);
        // Behaviors and game models registered here
    }
}
```

- Placement: All patches in `src/Mod.GameAdapters/Patches/`.
- Architecture: Target x64 in the project to align with TaleWorlds assemblies and avoid MSB3270 warnings.
- Logging: Use centralized logging (`GameAdapters` or feature category). Gate Debug logs behind a mod setting to avoid spam in high-frequency hooks.

## Appendix C — Harmony Patch Development & Decompiled References

### Critical Rule: Use Current Game DLL Decompiled References

**NEVER rely on outdated mod source code for method signatures.** Always use decompiled references from the current game version's DLLs.

#### Problem Example
Using outdated mod references (e.g., "ServeAsSoldier" mod) led to:
- Incorrect method signatures (static vs instance methods)
- Wrong parameter types and counts
- Runtime crashes during patch application
- Hours of debugging time

#### Solution Pattern
1. **Decompile current game DLLs** for the exact game version you're targeting
2. **Verify method signatures** in the actual TaleWorlds assemblies
3. **Use `TargetMethod()` with proper reflection** instead of string-based attributes
4. **Add graceful error handling** for missing methods

#### Correct Harmony Patch Structure
```csharp
[HarmonyPatch]
public static class YourPatch
{
    public static MethodBase TargetMethod()
    {
        try
        {
            var type = AccessTools.TypeByName("Full.Type.Name");
            if (type == null)
            {
                LogPatchError("Could not find target type");
                return null;
            }

            var method = AccessTools.Method(type, "MethodName", new[] { typeof(ParamType) });
            if (method == null)
            {
                LogPatchError("Could not find target method");
                return null;
            }

            LogPatchSuccess("Successfully found target method");
            return method;
        }
        catch (Exception ex)
        {
            LogPatchError($"Exception finding patch target: {ex.Message}");
            return null;
        }
    }

    [HarmonyPrefix] // or [HarmonyPostfix]
    private static bool Prefix(/* match exact signature from decompiled code */)
    {
        // Implementation with error handling
    }
}
```

#### Method Signature Examples (Game Version 1.2.12.77991)
- `PlayerArmyWaitBehavior.wait_menu_army_leave_on_condition` → **Instance method**: `bool MethodName(MenuCallbackArgs args)`
- `VillageHostileActionCampaignBehavior.wait_menu_end_raiding_at_army_by_leaving_on_condition` → **Static method**: `bool MethodName(MenuCallbackArgs args)`
- `CampaignEventDispatcher.OnMapEventStarted` → **Instance method**: `void MethodName(MapEvent, PartyBase, PartyBase)`

#### Verification Checklist
- [ ] Decompiled current game version DLLs (not mod source)
- [ ] Verified static vs instance method nature
- [ ] Confirmed exact parameter types and order
- [ ] Used `AccessTools.Method()` with parameter type array
- [ ] Added error handling for missing methods
- [ ] Tested patch application success in logs

#### Debugging Failed Patches
Check logs for patch status messages:
```
[YourModName] PATCH SUCCESS: Successfully found ClassName.MethodName
[YourModName] PATCH ERROR: Could not find target method
```

This prevents crashes and provides clear feedback on what's failing.

