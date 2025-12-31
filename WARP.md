# WARP.md

This file provides guidance to WARP (warp.dev) when working with code in this repository.

## Repository overview

Enlisted is a C# Bannerlord mod targeting **Mount & Blade II: Bannerlord v1.3.13** that turns the game into a soldier career simulator: the player enlists with a lord, follows orders, progresses through ranks, and eventually commands their own retinue.

The project uses an old-style `.csproj` with **explicit file includes** and integrates tightly with Bannerlord native APIs and data formats.

## Documentation entrypoints

Read these before doing any non-trivial work:

- `docs/BLUEPRINT.md` – Single source of truth for architecture, coding standards, constraints (target game version, build configuration, logging, JSON/XML conventions).
- `docs/INDEX.md` – Complete documentation index and feature lookup table.
- `docs/DEVELOPER-GUIDE.md` – Practical build, structure, and integration guide.
- `docs/Features/Core/core-gameplay.md` – High-level description of all major gameplay systems and how they interact.
- `docs/Content/content-index.md` and `docs/Content/event-catalog-by-system.md` – Catalog and grouping of all events, decisions, orders, and incidents.

Use `docs/INDEX.md` as the main navigator: most feature docs are under `docs/Features/**`, content catalogs under `docs/Content/**`, and technical/native references under `docs/Reference/**`.

## Build and validation commands

All commands assume the project root: `Enlisted/` (where `Enlisted.csproj` and `Enlisted.sln` live).

### Build the mod DLL

Configuration and platform are fixed for the mod; the build also runs the `AfterBuild` copy steps that deploy into the Bannerlord `Modules/Enlisted` folder.

```powershell path=null start=null
# From project root
dotnet build -c "Enlisted RETAIL" /p:Platform=x64
```

The compiled DLL and copied assets end up under the configured Bannerlord install path (see `Enlisted.csproj` `OutputPath`).

### Validate narrative/content data

The content validator enforces the JSON schema and cross-file rules described in `docs/Features/Technical/conflict-detection-system.md` and the content docs.

```powershell path=null start=null
# Standard validation
python tools/events/validate_events.py

# Strict mode (treat warnings as failures; preferred for CI / pre-commit)
python tools/events/validate_events.py --strict

# Sync missing XML localization strings from JSON files
python tools/events/sync_event_strings.py

# Check for missing strings without modifying files
python tools/events/sync_event_strings.py --check
```

If you touch any of `ModuleData/Enlisted/Events/*.json`, `ModuleData/Enlisted/Decisions/*.json`, or `ModuleData/Enlisted/Orders/*.json`, run the validator before building or submitting changes.

### Optional: Qodana / static analysis

If Qodana is configured in your environment, BLUEPRINT expects periodic scans:

```powershell path=null start=null
qodana scan --show-report
```

Run this before large refactors or feature branches to catch code-quality regressions alongside ReSharper/Rider warnings.

## Logs and diagnostics

The mod has its own logging pipeline; this is the primary place to look when debugging behavior.

- **Enlisted mod logs:** `<BannerlordInstall>\Modules\Enlisted\Debugging\`
  - Rotating `Session-A_*.log`, `Session-B_*.log`, `Session-C_*.log` (gameplay/session logs).
  - Rotating `Conflicts-A_*.log`, `Conflicts-B_*.log`, `Conflicts-C_*.log` (patch/mod conflict diagnostics).
  - `Current_Session_README.txt` summarises the active session and which files to share.
- **Bannerlord crash dumps (engine-level):** `C:\ProgramData\Mount and Blade II Bannerlord\crashes\`.

Logging is configured via `ModuleData/Enlisted/settings.json` and written through `Mod.Core.Logging.ModLogger`.

## Environment & API constraints

Key constraints and expectations (see `docs/BLUEPRINT.md` for details):

- Target **game version is fixed** at Bannerlord **v1.3.13**; do not assume APIs from later game versions.
- Verify Bannerlord APIs against the **local decompile** at `C:\Dev\Enlisted\Decompile\` rather than online docs.
- New C# files **must be added explicitly** to `Enlisted.csproj` via `<Compile Include="..." />` or they will not be compiled.
- **ReSharper/Rider warnings are treated as the linter:** Always fix warnings before committing. Never suppress unless there's a documented compatibility reason (e.g., namespace conflicts). Run Qodana scans before major refactors.
- **Tooltips cannot be null:** Every event option, decision, order, and UI element must have a factual, concise tooltip.
- **JSON field ordering:** Fallback fields (`title`, `setup`, `text`) must immediately follow their ID fields (`titleId`, `setupId`, `textId`).

## High-level architecture

### C# project layout

The primary project is `Enlisted.csproj` (targeting .NET Framework 4.7.2). Source code is organized as follows (see `docs/BLUEPRINT.md` and `docs/DEVELOPER-GUIDE.md` for details and current statuses):

- `src/Mod.Entry/`
  - SubModule entry point and Harmony initialization.
- `src/Mod.Core/`
  - Configuration loading (`ModSettings`, `ModConfig`, `ConfigurationManager`).
  - Logging and diagnostics (`ModLogger`, `ModConflictDiagnostics`, `SessionDiagnostics`).
  - Save system support (`EnlistedSaveDefiner`) and utility/helpers (`PlayerContext`, `CampaignSafetyGuard`, etc.).
- `src/Mod.GameAdapters/`
  - Harmony patches that adapt/guard native Bannerlord behavior (encounters, parties, UI, DLC edge cases, etc.).
- `src/Features/**`
  - Feature modules grouped by gameplay concern; each has its own behaviors, managers, and data models. The important ones conceptually are:
    - **Enlistment** – Core enlisted state machine and lifecycle (join/leave service, contracts, grace periods, XP and term tracking).
    - **Orders** – Chain-of-command order system (multi-day order lifecycle, progression, consequences).
  - **Content** – JSON-driven narrative engine: event/decision/order catalogs, pacing, delivery, and the Content Orchestrator (AI-driven dynamic content selection and pacing system).
    - **Identity** – Trait- and reputation-based identity/role detection and soldier reputation tracking.
    - **Escalation** – Discipline/scrutiny/medical-risk tracks and associated threshold events.
    - **Company** – Company-wide needs (Readiness, Morale, Supplies, Equipment, Rest) and their managers.
    - **Context** – Strategic context and war stance analysis (Grand Campaign, Last Stand, Winter Camp, etc.).
  - **Interface** – Native `GameMenu`-based UI hub (Camp Hub with accordion-style decision sections, 8-stage muster system every 12 days, reports, news/daily brief).
    - **Equipment** – Quartermaster, equipment management, baggage/contraband checks, provisions and pricing.
    - **Ranks** – Promotion logic and culture-specific rank naming.
    - **Conversations** – Dynamic, data-driven dialogs (e.g., quartermaster conversations).
    - **Combat** – Formation assignment, battle participation hooks, training XP hooks.
    - **Conditions** – Player medical/injury/illness state and related menus.
    - **Retinue** – Commander-tier retinue and named veterans; service record tracking.
    - **Camp** – Camp life activities, fatigue, and opportunity generation.

The `.csproj` `ItemGroup` lists are the authoritative map of which classes compile into the DLL; check there when locating or adding code.

### Data & content pipeline

The mod relies heavily on external data for tuning and narrative content:

- **Config & content JSON (`ModuleData/Enlisted/`)**
  - `enlisted_config.json` – Master tuning/feature flags (wages, retirement, event pacing, camp life, escalation, conditions, etc.).
  - `progression_config.json` – Tier XP thresholds, culture-specific rank titles, wage system details.
  - `Config/*.json` – Additional tuning for equipment, retinue, baggage, simulation, strategic context, etc.
  - `Events/*.json` – Narrative events, automatic decisions, map incidents; use the schemas in `docs/Features/Content/event-system-schemas.md`.
  - `Decisions/decisions.json` – Camp Hub player-initiated decisions (see `ModuleData/Enlisted/Decisions/README.md`).
  - `Orders/*.json` – Tiered order definitions driving the orders system.
- **Localization XML (`ModuleData/Languages/`)**
  - `enlisted_strings.xml`, `language_data.xml` and per-language folders, as documented in `ModuleData/Languages/README.md`.
  - Follow the rules there for placeholders, entities, and adding languages.
- **GUI XML (`GUI/Brushes/**`, `GUI/Prefabs/**`)**
  - Gauntlet UI definitions for quartermaster screens and other complex interfaces; see UI docs in `docs/Features/UI/ui-systems-master.md`.

`Enlisted.csproj` `AfterBuild` copies JSON, XML, and GUI content into the Bannerlord `Modules/Enlisted` folder (see the `AfterBuild` target for directory layout and file lists).

## Working on features and content

### When editing or adding C# code

- Place new classes in the appropriate `src/` subfolder to match existing feature boundaries.
- Add the new file path to the `<Compile Include="..." />` ItemGroup in `Enlisted.csproj`.
- Run a build (`dotnet build -c "Enlisted RETAIL" /p:Platform=x64`) and fix any ReSharper/compiler issues.
- When touching Bannerlord APIs, cross-check signatures and expected behavior in the `C:\Dev\Enlisted\Decompile\` tree rather than guessing.

### When editing or adding JSON content

- **Critical JSON rules:**
  - Fallback fields (`title`, `setup`, `text`) must **immediately follow** their ID fields (`titleId`, `setupId`, `textId`)
  - **Tooltips cannot be null** – every option must have a factual, concise tooltip (1 sentence, under 80 chars)
  - Always include fallback text in JSON, even with XML localization (serves as safety net and source of truth)
- Follow field ordering and fallback rules from `docs/BLUEPRINT.md` and `docs/Features/Content/event-system-schemas.md`.
- Keep narrative/content changes consistent with the catalogs in `docs/Content/content-index.md` and `docs/Content/event-catalog-by-system.md`.
- **Before committing:**
  ```powershell
  # Validate JSON schema and cross-file rules
  python tools/events/validate_events.py --strict
  
  # Sync any missing XML localization strings
  python tools/events/sync_event_strings.py
  ```

### When working with UI

- Prefer using the existing native `GameMenu`-based patterns and Gauntlet prefabs defined in `GUI/Prefabs/**`.
- Use `docs/Features/UI/ui-systems-master.md` and the UI sections of `docs/BLUEPRINT.md` as the reference for which menus, ViewModels, and XML prefabs are authoritative.

## How to get oriented for a new task

Depending on what you want to change:

- **Gameplay/system behavior:**
  - Start at `docs/Features/Core/core-gameplay.md`, then drill into the relevant `docs/Features/**` document (pay, promotion, retinue, orders, camp life, etc.).
  - Locate the corresponding manager/behavior in `src/Features/**` using the "System Map" sections in those docs.
- **Narrative/content:**
  - Use `docs/Content/content-index.md` or `docs/Content/event-catalog-by-system.md` to find the event/decision/order IDs.
  - Edit the appropriate JSON under `ModuleData/Enlisted/**` and associated localization entries under `ModuleData/Languages/**`, then run the validator.
  - Use `sync_event_strings.py` to automatically extract missing XML strings from JSON files.
- **Muster system (pay day ceremony):**
  - 8-stage `GameMenu` sequence occurring every 12 days
  - Stages: Intro → Pay Line → Baggage Check → Inspection → Recruit → Promotion Recap → Retinue → Complete
  - See `docs/Features/Core/muster-system.md` for complete flow and integration points
- **Integration with other mods or Bannerlord systems:**
  - Consult `docs/Reference/native-apis.md` and the Harmony patch list in `docs/Features/Technical/conflict-detection-system.md` / conflicts logs to understand what Enlisted already patches or depends on.

## Code quality checklist

Before committing code, verify:

- [ ] All ReSharper/Rider warnings addressed (never suppress without documented reason)
- [ ] Braces used for all single-line control statements
- [ ] No unused `using` directives, variables, parameters, or methods
- [ ] No redundant namespace qualifiers (use `using` statements)
- [ ] No redundant default parameter values in method calls
- [ ] Comments describe **current behavior** (not "Phase X added..." or changelog-style framing)
- [ ] New files added to `Enlisted.csproj` with `<Compile Include="..."/>`
- [ ] Tooltips provided for all event options (cannot be null)
- [ ] JSON fallback fields immediately follow ID fields
- [ ] Build succeeds: `dotnet build -c "Enlisted RETAIL" /p:Platform=x64`
- [ ] Events validated: `python tools/events/validate_events.py --strict`
