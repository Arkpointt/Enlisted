# Enlisted Mod Blueprint
Comprehensive reference for features, architecture, and standards used in this repository.

1. Overview
- Name: Enlisted – Serve as a Soldier
- Game: Mount & Blade II: Bannerlord
- Target framework: .NET Framework 4.7.2
- Entry point: SubModule.cs (TaleWorlds module)
- Build targets: Debug/Release for AnyCPU and x64 (use Debug|x64 for game runtime)

Core idea: The player can enlist under a lord and serve as a soldier. The mod integrates with native systems (armies, dialogs, encounters, UI) to make the experience seamless and stable.

2. Coding Standards (adapted for this repo)
- Follow Microsoft C# conventions.
- Naming
  - Classes/structs/enums/constants: PascalCase (PromotionBehavior, MaxPartySize)
  - Methods/properties/events: PascalCase
  - Parameters/local vars: camelCase
  - Private fields: _camelCase (private bool _isEnlisted)
  - Interfaces: IPascalCase (IArmyService)
- Formatting
  - Allman braces; 4-space indentation; no tabs.
  - One type per file. Keep files under the correct folder by concern.
- Comments
  - XML docs (///) on public/internal types and members exposed outside their file.
  - In-method comments explain why, not what.
- Defensive coding
  - Prefer null-conditional and null-coalescing (?. and ??).
  - Guard game state reads; Bannerlord can return nulls.
- Harmony patch safety
  - One patched game class per file under Patches/.
  - Patch class names: <GameClass>_<Method>_<Prefix|Postfix|Transpiler>.
  - Wrap patch logic in try/catch and log via DebugHelper to avoid game crashes.
- Localization
  - No hardcoded user-facing strings. Use TextObject with string IDs.
- Performance
  - Keep per-tick logic minimal; offload to hourly/daily events where possible.
  - Cache repeated lookups in a scope.

3. Professional Modularity Rules
- Layered dependencies (allow-list)
  - Patches ➜ Services
  - Behaviors ➜ Services
  - Services ➜ Models, Utils (and TaleWorlds APIs)
  - Models, Utils ➜ no upward references
  - Forbidden: Patches ➜ Behaviors; Behaviors ➜ Patches; cross-service concrete coupling
- Service contracts
  - Define interfaces for every service consumed by other layers:
    - IArmyService, IPartyIllusionService, IDialogService, IPromotionRules
  - Behaviors depend on interfaces only. SubModule wires concrete implementations.
- Patch discipline
  - Patches contain no business logic. They only:
    1) detect the game event/state, 2) map to a service call, 3) guard with try/catch and log.
- Reflection boundaries
  - ReflectionHelpers may be used inside Services only (never in Behaviors/Patches).
- Feature boundaries
  - Keep feature logic self-contained: enlistment, wages, promotion, encounters, UI.
  - Cross-feature communication goes through service interfaces or events, not direct calls.
- Observability
  - Centralized logging via DebugHelper with levels (Info/Warning/Error). Minimal per-tick logging.

4. Repository Structure (this project)
- Behaviors/
  - EnlistmentBehavior: Orchestrates enlist/leave life cycle and save/load.
  - PromotionBehavior: Handles rank and promotion tracking via PromotionRules.
  - WageBehavior: Pays daily wages while enlisted.
- Services/
  - ArmyService: Create/join/leave armies; escort AI setup.
  - PartyIllusionService: Hides player party, handles camera follow/restore.
  - DialogService: Registers dialog lines and conditions/actions.
  - PromotionRules: Encapsulates promotion thresholds and logic.
- Patches/
  - BanditEncounterPatch: Prevents hostile encounters while enlisted.
  - BattleParticipationPatch: Auto-joins commander battles on correct side.
  - EngagementPatch: Participation/engagement helpers.
  - HidePartyNamePlatePatch: UI cleanup while enlisted.
  - SuppressArmyMenuPatch: Removes inappropriate army UI while enlisted.
- Models/
  - EnlistmentState, PromotionState: Persistent state for save system.
- Utils/
  - Constants, DebugHelper (logging), ReflectionHelpers (API-compat wrappers).
- Settings.cs: Centralized knobs for wages, toggles, etc.
- SubModule.cs: Module bootstrap; adds CampaignBehaviors and wires services.
- SubModule.xml: Bannerlord module manifest (copied by post-build).

Optional evolution (feature modules)
- If the codebase grows, introduce a Features/ folder with subfolders per feature (Enlistment, Promotion, Wages, Encounters, UI), each hosting its behavior, service(s), patches, and models. Keep public APIs in Services interfaces at the root namespace to avoid circular deps.

5. Game Integration
- Dialog integration: Options appear in lord_talk and hero_main_options with priority 110.
- Army integration: Uses Kingdom.CreateArmy and escort AI for joining/escorting.
- Camera/visibility: Transfers camera to commander; hides player party; restores on discharge.
- Encounters/battles: Watches commander combat state and joins automatically.
- UI: Hides party nameplate and suppresses conflicting screens while enlisted.
- Save/load: EnlistmentState and PromotionState saved via Bannerlord save system.

6. Build & Deploy
- Visual Studio: Build Debug|x64.
- Post-build: Copies Enlisted.dll and SubModule.xml to
  Modules/Enlisted/bin/Win64_Shipping_Client under your Bannerlord installation (see .csproj PostBuildEvent).
- References: .csproj points to TaleWorlds and 0Harmony DLLs in Steam installation; keep paths valid.

7. Review & Testing Checklists
- Review (PR) checklist
  - Layering respected (see section 3) and dependencies are interface-based.
  - Patches are thin and delegate to services; have try/catch + logging.
  - No hardcoded user-facing strings; uses TextObject IDs.
  - XML docs on new public/internal members. No tabs; Allman braces.
  - Per-tick logic kept minimal.
- Functional testing
  - Enlistment: dialog shows, enlist succeeds, wages start, camera/visibility update.
  - Battles: auto-join commander battles on correct side.
  - Discharge: visibility/camera restore; wages stop.
  - Stability: no hostile encounters; no unhandled exceptions in logs.

8. Roadmap
- Promotions: Expand PromotionRules and UI feedback for ranks.
- Advanced battle roles: Formation placement and role assignment.
- Settings: Expose more knobs (wage, visibility toggles, logging level).

9. Contribution Guidelines (summary)
- Feature branches: feature/<name>; create PRs to main/develop as per repo policy.
- Adhere to standards above; run analyzers and fix warnings.
- Keep patches minimal and focused. Prefer services/behaviors for logic.
