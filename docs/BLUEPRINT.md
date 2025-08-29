# Bannerlord Mod Engineering Blueprint

**Version:** 1.3 (Living Document)  
**Status:** Authoritative Reference  
**Last Updated:** August 29, 2025

## Changelog

- **2025-08-26:** Added Harmony Patching Policy (Section 4.2.1) and Harmony commenting standard (Section 6.1); noted Harmony packaging/runtime dependency in Appendix B. Clarified SubModule.xml dependency example, 0Harmony reference strategy, engine-invoked examples, and Harmony ID stability.
- **2025-08-25:** Renamed "Patch (default)" → "Small Changeset (default)"; clarified workflow to match approve-changes flow; removed .artifacts/patches/ from structure.
- **2025-08-22:** Initial release for a Mount & Blade II: Bannerlord mod in Visual Studio 2022 with minimal root, Package-by-Feature organization, change levels, and concise commenting standards.

## Quickstart: Generic Bannerlord Mod

This quickstart sets up a minimal, generic Bannerlord mod with Harmony and a clean packaging flow. Replace `YourModName`/`com.yourmodid.mod` with your values.

### Prerequisites
- Visual Studio 2022
- Bannerlord installed (know your `<BannerlordInstall>` path)
- Depends on `Bannerlord.Harmony` (runtime)

### Repository Layout (current project)
```
Enlisted/
├── Enlisted.sln
├── docs/
├── src/
│   ├── Mod.Entry/
│   │   └── SubModule.cs
│   ├── Mod.Core/
│   │   └── Logging/
│   ├── Mod.GameAdapters/
│   │   └── Patches/
│   └── Features/
└── SubModule.xml
```

### Minimal SubModule.cs (project pattern)
```csharp
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem;
using HarmonyLib;

namespace YourModName
{
    public class SubModule : MBSubModuleBase
    {
        private Harmony _harmony;

        protected override void OnSubModuleLoad()
        {
            _harmony = new Harmony("com.yourmodid.mod");
            _harmony.PatchAll();
        }

        protected override void OnGameStart(Game game, IGameStarter starterObj)
        {
            if (starterObj is CampaignGameStarter campaign)
            {
                // Register CampaignBehavior(s) here
                // campaign.AddBehavior(new YourBehavior());
            }
        }
    }
}
```

### SubModule.xml (template)
```xml
<?xml version="1.0" encoding="utf-8"?>
<Module>
  <Name value="YourModName" />
  <Id value="YourModName" />
  <Version value="v1.0.0" />
  <DefaultModule value="false" />

  <SingleplayerModule value="true" />
  <MultiplayerModule value="false" />

  <DependedModules>
    <DependedModule Id="Bannerlord.Harmony" />
  </DependedModules>

  <SubModules>
    <SubModule>
      <Name value="YourMod SubModule" />
      <DLLName value="YourModName.dll" />
      <SubModuleClassType value="YourModName.SubModule" />
      <Tags>
        <Tag key="DedicatedServerType" value="none" />
        <Tag key="IsNoRenderModeElement" value="false" />
      </Tags>
    </SubModule>
  </SubModules>

  <Xmls />
</Module>
```

### Project Settings (csproj snippets)
```xml
<PropertyGroup>
  <TargetFramework>net472</TargetFramework>
  <Platforms>x64</Platforms>
  <PlatformTarget>x64</PlatformTarget>
  <LangVersion>latest</LangVersion>
  <Nullable>disable</Nullable>
</PropertyGroup>
```

Optional reference for IntelliSense (prefer runtime Harmony from Bannerlord.Harmony; do not copy):
```xml
<ItemGroup>
  <Reference Include="0Harmony">
    <HintPath>..\..\..\Bannerlord\Modules\Bannerlord.Harmony\bin\Win64_Shipping_Client\0Harmony.dll</HintPath>
    <Private>false</Private>
  </Reference>
  <Reference Include="TaleWorlds.MountAndBlade">
    <HintPath>..\..\..\Bannerlord\bin\Win64_Shipping_Client\TaleWorlds.MountAndBlade.dll</HintPath>
    <Private>false</Private>
  </Reference>
  <Reference Include="TaleWorlds.CampaignSystem">
    <HintPath>..\..\..\Bannerlord\bin\Win64_Shipping_Client\TaleWorlds.CampaignSystem.dll</HintPath>
    <Private>false</Private>
  </Reference>
  <!-- add other TaleWorlds references as needed, Private=false -->
  </ItemGroup>
```

### Packaging (post-build)
Copy the DLL and `SubModule.xml` into the module folder after build. In this project the `Enlisted EDITOR` configuration already outputs to the module path. Example snippet (if you need a post-build copy):
```xml
<Target Name="PostBuild" AfterTargets="PostBuildEvent">
  <PropertyGroup>
    <ModuleDir>C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\YourModName</ModuleDir>
  </PropertyGroup>
  <MakeDir Directories="$(ModuleDir)\bin\Win64_Shipping_Client" />
  <Copy SourceFiles="$(TargetDir)$(TargetFileName)" DestinationFolder="$(ModuleDir)\bin\Win64_Shipping_Client" />
  <Copy SourceFiles="$(ProjectDir)..\..\SubModule.xml" DestinationFolder="$(ModuleDir)" />
</Target>
```

### Test Run
1. Build in Release/x64.
2. Verify files under `<BannerlordInstall>/Modules/YourModName/`.
3. Enable the mod in the launcher (ensure `Bannerlord.Harmony` loads first).
4. Launch a new campaign to see SubModule hooks active.

### Troubleshooting
- If the mod doesn’t appear, confirm `SubModule.xml` is present and valid.
- If patches fail, check Harmony IDs and ensure x64 target.
- Use a simple logger to confirm `OnSubModuleLoad` and `OnGameStart` are hit.

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
├── Features/                    # One folder per feature (self-contained)
│   ├── EconomyTweaks/
│   │   ├── Domain/              # calculations, rules, models
│   │   ├── Application/         # orchestrators, controllers
│   │   ├── Presentation/        # view-models, UI bindings (if any)
│   │   └── Tests/               # unit tests for this feature
│   ├── PartyManagement/
│   │   ├── Domain/
│   │   ├── Application/
│   │   ├── Presentation/
│   │   └── Tests/
│   └── (add more features here)
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

**Coding conventions**

- Prefer Prefix/Postfix; use Transpiler only when behavior cannot be achieved otherwise, and document the IL assumptions (target instruction patterns, invariants).
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

## 5. Development Standards (C# / VS2022)

- Language/Runtime: Use the game-compatible .NET target (align with current Bannerlord runtime; when unsure, prefer the lowest known compatible target).
- Solution Hygiene: One solution file; each project compiles cleanly; treat warnings as meaningful.
- Style & Linting: Enforce consistent style via `.editorconfig`. Keep namespaces/folders aligned.
- Types & Nullability: Enable nullable reference types where compatible; prefer explicit types on public APIs.
- Logging: Central logging service; avoid `Console.WriteLine`. Logs should be structured and filterable.
- Module/Class Size: Keep classes reasonable (aim <500 LOC). Split by single responsibility when growing.
- No duplicate logic: Cross-feature utilities live in `Mod.Core`; TaleWorlds/Harmony specifics live in `Mod.GameAdapters`.
- Harmony safety: Keep patches minimal, guarded, and reversible; never remove safety checks to "make tests pass."

## 6. Commenting & Documentation in Code (authoritative)

**Goal:** Comments explain intent, constraints, and safety — not change history.

- Allowed: why the approach is used, assumptions (e.g., campaign-only), safety limits, invariants, side effects, known engine quirks.
- Prohibited: "changed this on …", ticket IDs, comments that merely restate code.
- Accuracy: When behavior changes, update or remove nearby comments/docstrings in the same change.
- Pointers: If needed, reference a section in `docs/` or an ADR by title (not an external ticket URL).

### 6.1 Harmony Patch Commenting Standard

Each Harmony patch must include the header described in 4.2.1. Additionally:

- Indicate whether the patch runs in campaign-only, battle-only, or both.
- List the minimal set of assumptions about object lifetimes (e.g., PartyComponent may be null).
- Note configuration gates (feature flags, settings) and default behavior when disabled.
- Include a brief performance note if the patch is on a frequent path (tick, per-frame, menu draw).

Example header:

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

- Structured logs: stable categories (e.g., `EconomyTweaks`, `PartyManagement`, `GameAdapters`).
- Minimal artifacts: keep only what aids diagnosis (e.g., config snapshot, version info).
- Session bundles: clear folder for logs to share when reporting issues.
- Support loop: reproduce → collect bundle → fix → capture learnings in `docs/changes/` or an ADR (if architectural).

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

```csharp
using HarmonyLib;

// e.g., inside SubModule.OnSubModuleLoad or appropriate init hook
var harmony = new Harmony("com.yourmodid.mod");
harmony.PatchAll();
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

