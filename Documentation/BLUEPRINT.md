# Bannerlord Mod Engineering Blueprint

**Version:** 1.1 (Living Document)  
**Status:** Authoritative Reference  
**Last Updated:** August 25, 2025

## Changelog

- **2025-08-25:** Renamed "Patch (default)" → "Small Changeset (default)"; clarified workflow to match approve-changes flow; removed .artifacts/patches/ from structure.
- **2025-08-22:** Initial release for a Mount & Blade II: Bannerlord mod in Visual Studio 2022 with minimal root, Package-by-Feature organization, change levels, and concise commenting standards.

## 1. Purpose & Scope

This blueprint is the single source of truth for how the Bannerlord mod is organized, built, tested, released, and supported in Visual Studio 2022. It is self-serving: an assistant (or contributor) can follow it without additional instructions.

It defines:

- Minimal root layout and Package-by-Feature structure for C# solutions
- Mod packaging rules (Module folder, SubModule.xml, assets)
- Development, commenting, testing, CI, and release standards
- Platform constraints (Windows, game runtime)
- Change Levels and discretion rules (small, reversible changes first)

## 2. Guiding Principles

- **Correctness first, speed second.** Prefer safe, deterministic behavior over cleverness.
- **Determinism over flakiness.** Use explicit gates, bounded waits, and idempotent logic.
- **Make it observable.** Emit structured logs for non-trivial paths.
- **Fail closed.** On uncertainty, do the safe thing and record why.
- **Config over code.** Prefer config flags and mod settings to hard-coding behavior.
- **Small, reversible changes.** Short branches, focused reviews, easy rollbacks.
- **Player empathy.** Errors and warnings offer next steps.
- **Respect the platform.** Honor module rules, game API constraints, and save-game safety.

## 3. High-Level Architecture

- **Package-by-Feature (C#).** Each gameplay feature has its own subtree (domain logic + view-models + assets if relevant).
- **Mod Entry & Wiring.** A thin entry layer integrates with Bannerlord's module lifecycle (SubModule.xml, load order) and routes to feature modules.
- **Separation of Concerns.**
  - **Decision:** feature logic determines what to do (e.g., adjust troops, UI state).
  - **Actuation:** adapters call TaleWorlds APIs to apply changes safely.
- **Integration Boundary.** A single "game adapters" layer isolates TaleWorlds types, Harmony patches (if used), and game events from core logic.
- **Observability.** Centralized logging with stable categories and correlation (e.g., session GUID).

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

- **Language/Runtime:** Use the game-compatible .NET target (align with current Bannerlord runtime; when unsure, prefer the lowest known compatible target).
- **Solution Hygiene:** One solution file; each project compiles cleanly; treat warnings as meaningful.
- **Style & Linting:** Enforce consistent style via `.editorconfig`. Keep namespaces/folders aligned.
- **Types & Nullability:** Enable nullable reference types where compatible; prefer explicit types on public APIs.
- **Logging:** Central logging service; avoid `Console.WriteLine`. Logs should be structured and filterable.
- **Module/Class Size:** Keep classes reasonable (aim <500 LOC). Split by single responsibility when growing.
- **No duplicate logic:** Cross-feature utilities live in `Mod.Core`; TaleWorlds/Harmony specifics live in `Mod.GameAdapters`.

## 6. Commenting & Documentation in Code (authoritative)

**Goal:** Comments explain intent, constraints, and safety — not change history.

- **Allowed:** why the approach is used, assumptions (e.g., campaign-only), safety limits, invariants, side effects, known engine quirks.
- **Prohibited:** "changed this on …", ticket IDs, comments that merely restate code.
- **Accuracy:** When behavior changes, update or remove nearby comments/docstrings in the same change.
- **Pointers:** If needed, reference a section in `docs/` or an ADR by title (not an external ticket URL).

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

- **Branching:** Trunk-based; short-lived feature branches.
- **How edits happen:** The AI proposes small, focused changesets on your feature branch; you review/approve each changeset, then open a PR to main.
- **Commits:** Conventional, descriptive messages that capture why and what.
- **Versioning:** Semantic Versioning (SemVer).
- **Releases:** Tag releases; package to `Modules/<YourModName>/` and (optionally) Workshop/Nexus with notes.
- **Rollbacks:** Keep releases reversible (previous DLL + SubModule.xml kept). Feature flags help gate risky changes.

## 9. Testing & CI/CD (policy-level)

- **Unit tests:** Feature domain calculations, configuration validation, utility classes (avoid TaleWorlds types where possible).
- **Integration tests:** Limited; test through adapter abstractions. Where not feasible, rely on manual smoke tests defined in `docs/TESTING.md`.
- **E2E/Smoke:** Launch sequence sanity via in-game checklist.
- **Performance checks:** Watch frame-time; avoid per-frame heavy allocations/reflection.
- **CI gates:** build + quick tests + packaging validation (structure, presence of SubModule.xml, DLL output).
- **Artifacts on failure:** produce concise log summaries to speed diagnosis.

## 10. Configuration & Mod Settings

- **Precedence:** in-game settings (if provided) > user config files > defaults.
- **Validation:** validate on load; fail fast with actionable errors (e.g., "Invalid multiplier; expected 0.1–10").
- **Feature flags:** document default, owner, rollback notes in a small registry (e.g., `Mod.Config`).

## 11. Observability & Support

- **Structured logs:** stable categories (e.g., `EconomyTweaks`, `PartyManagement`, `Adapters`).
- **Minimal artifacts:** keep only what aids diagnosis (e.g., config snapshot, version info).
- **Session bundles:** clear folder for logs to share when reporting issues.
- **Support loop:** reproduce → collect bundle → fix → capture learnings in `docs/changes/` or an ADR (if architectural).

## 12. Performance & Reliability Budgets (examples)

- **Per-frame work:** avoid heavy allocations, reflection loops, or IO.
- **Menu/opening hooks:** ~≤200 ms perceived delay.
- **Campaign tick logic:** bounded and amortized; no unbounded loops.
- **Save-game safety:** persistent data changes are backward-compatible or gated.

(Budgets are living targets; update as features evolve.)

## 13. Platform Constraints (Bannerlord + Windows)

- **Runtime compatibility:** target the game's supported .NET/runtime; avoid APIs newer than the game provides.
- **Module load order:** declare dependencies in SubModule.xml; don't assume other mods' presence or order.
- **Save-game compatibility:** never remove serialized fields without a migration plan.
- **Anti-cheat/MP:** if relevant, respect multiplayer fairness and server policy.
- **Localization:** store strings in ModuleData/localization files where appropriate.

## 14. Asset & Data Governance

- **Source of truth:** author XML data, localization, and assets in `assets/` (and docs where appropriate).
- **Packaging:** copy into `ModuleData/`, `GUI/`, `AssetPackages/` during build.
- **Validation:** basic schema checks for XML where feasible; clear in-game error messages on bad data.

## 15. Incident Response & Hotfix Protocol

- **Severity:** S1 (crash/blocker), S2 (feature broken), S3 (degraded).
- **Hotfix:** branch from main → focused change(s) → rebuild/retag → replace bin/ DLL in module folder.
- **Postmortem:** short write-up of cause, detection gap, and guardrail added (store in `docs/changes/`).

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

- **Build output location:** Configure post-build to copy the compiled DLL(s), SubModule.xml, and content to `<BannerlordInstall>/Modules/<YourModName>/`.
- **SubModule.xml hygiene:** Declare name, version, dependencies, and load order; keep it minimal and accurate.
- **Do not commit build outputs.** The `Modules/<YourModName>/` tree is a generated artifact, not part of the repo.
- **Keep root clean.** Only solution/metadata files and small policy/config files at root. Everything else organized under `src/`, `assets/`, `docs/`, `tools/`.

