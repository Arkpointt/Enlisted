## Enlisted — Implementation Plan (Feature-by-Feature)

Links (reference during implementation):
- Bannerlord API 1.2.12: https://apidoc.bannerlord.com/v/1.2.12/
- Harmony modding guide: https://docs.bannerlordmodding.lt/modding/harmony/

Verification rule: For each feature below, verify API usage against the official docs above before coding (symbols like `IDataStore`, `SaveableTypeDefiner`, `CampaignEvents`, `GiveGoldAction`, `ChangeRelationAction`, `MobileParty`, `PlayerEncounter`, `GameMenu`, `CampaignTime`).

### Goals
- Recreate a robust “Serve as a Soldier” experience by blending the best parts of SAS and Freelancer.
- Ship incrementally, one feature at a time, with verifiable acceptance criteria.
- Prefer public APIs; use Harmony patches narrowly where public APIs cannot achieve the behavior.

### Architecture & Conventions
- Package-by-Feature under `src/Features/Enlistment/` (Domain, Application, Presentation optional) and cross-cutting services under `src/Mod.Core/` and adapters/patches under `src/Mod.GameAdapters/`.
- Persistence via `SaveDefiner` and `IDataStore.SyncData`.
- Logging via centralized `LoggingService` with stable categories.
- When Harmony is required: implement under `src/Mod.GameAdapters/Patches/`, document target methods (per blueprint) and guard with settings/feature flags.

### Phased Roadmap (build order)
- Phase 0 — Scaffolding & wiring (foundations)
  1) SubModule registration (ensure both `EnlistmentBehavior` and `EnlistedMenuBehavior` are added in `OnGameStart`/`OnSubModuleLoad` per API) — API: `MBSubModuleBase`, `IGameStarter`, `CampaignGameStarter` (docs)
  2) Logging service categories and config (so subsequent features are observable)

- Phase 1 — Core gameplay loop (minimum viable enlistment)
  1) Feature 1 — Enlistment lifecycle (dialogs + minimal status menu)
  2) Feature 2 — Persistence (save/load enlisted state)
  3) Feature 3 — Commander follow + invisibility + AI safety
  4) Feature 4 — Engagement routing + time-control safety

- Phase 2 — Progression & economy
  5) Feature 5 — Wages/XP/reputation (SAS default; Freelancer optional)
  6) Feature 6 — Promotion + equipment selection (menu → optional Gauntlet)

- Phase 3 — Exits & edge-cases
  7) Feature 7 — Leave and desertion (immediate + timed)
  8) Feature 8 — Commander death/imprisonment & player capture
  9) Feature 9 — Nameplate/tracker suppression (Harmony only if needed)
  10) Feature 10 — Observability polish

- Phase 4 — Post-core enhancements (deferred until core is stable)
  A) SAS menu parity additions (header fields, fuller actions)
  B) Optional Freelancer Soldier Camp submenu (time-boxed actions, paid services)

Note: Each phase is shippable; do not begin the next phase until the previous phase’s acceptance criteria pass.

### Core Data Model (Domain)
- `EnlistmentState`
  - `bool IsEnlisted`
  - `string CommanderId`
  - `int EnlistTier`
  - `int Experience`
  - `CampaignTime EnlistStart`
  - `bool IsOnLeave`
  - `CampaignTime LeaveDeadline`
  - `bool IsDeserter`
  - `List<EquipmentElement> StoredEquipment`
  - `List<ItemObject> StoredItems`
  - `Dictionary<string,int> ReputationByFaction`
  - Methods: `Enlist(Hero)`, `LeaveArmy()`, `ApplySoldierEquipment()`, `StorePlayerEquipment()`, `RestoreStoredLoadout(...)`, extract/restore for save.

### Feature 1 — Enlistment lifecycle (dialogs + status menu)
Intent: Players can enlist/leave via dialog; a minimal hub menu exposes enlisted actions.

APIs (docs):
- `CampaignEvents.OnSessionLaunchedEvent`
- `CampaignGameStarter.AddPlayerLine`, `AddDialogLine`, `AddGameMenu`, `AddWaitGameMenu`, `AddGameMenuOption`
- `GameMenu.ActivateGameMenu`, `GameMenu.SwitchToMenu`
- `Campaign.Current.TimeControlMode` (keep at StoppablePlay while menu open)
- `PlayerEncounter.Finish(bool)`, `GameMenu.ExitToLast()`

Steps:
1) Register enlistment dialogs in `OnSessionLaunched`. Accept path sets commander and calls `EnlistmentState.Enlist(hero)`.
2) Create a single enlisted status menu (SAS-style hub-lite). Prefer `AddWaitGameMenu` when available; in `OnInit`, call `args.MenuContext.GameMenu.StartWait()` and set time to `CampaignTimeControlMode.StoppablePlay` so map speed controls work.
3) Drain any active encounter before switching menus to avoid re-entrancy crashes.

Acceptance:
- Can enlist from lord conversation; status menu opens; map time controls remain usable.
- No duplicate dialog registration after load; re-enlisting prevented while enlisted.

### Feature 2 — Persistence (save/load)
Intent: Persist enlisted state, commander, tier/XP, stored gear, leave timer.

APIs:
- `IDataStore.SyncData`
- Save definer type for `EnlistmentState`
 - `SaveableTypeDefiner` (registers types/versions)

Steps:
1) Add `EnlistmentSaveDefiner` (already present) and sync `EnlistmentState` fields.
2) On load: reapply escort/camera, hide main party, restore trackers (guard commander null/dead).

Behavioral references (for parity):
- SAS: `Test.SyncData(IDataStore)` persists `_following_hero`, `_assigned_role`, `_vassal_offers`, and event keys (e.g., `_deserter_event_party`). Uses a `SaveDefiner : SaveableTypeDefiner`.
- Freelancer: `FreelanceBehavior.SyncData(IDataStore)` persists `freelanceCurrentUnitData` and related state; auxiliary behaviors persist their scoped keys; has `BlocFreelancerSaveDefiner : SaveableTypeDefiner`.

Verification (official docs):
- Confirm `IDataStore`/`SyncData` serialization usage and types: https://apidoc.bannerlord.com/v/1.2.12/
- Confirm save type registration via `SaveableTypeDefiner`: https://apidoc.bannerlord.com/v/1.2.12/

Acceptance:
- Save/load preserves enlisted status and commander; no null refs; menu/escort restored.

### Feature 3 — Commander follow, map invisibility, AI safety
Intent: Attach to commander; hide the main party; prevent world AI from targeting the player’s party.

APIs:
- `MobileParty.MainParty`, `MobileParty.Ai.SetMoveEscortParty(MobileParty)`
- `MobileParty.MainParty.IsVisible`, `IsActive`, `Position2D`
- `PartyBase.SetAsCameraFollowParty()`
- `Campaign.Current.VisualTrackerManager` (optional un/register)
- `MobileParty.MainParty.IgnoreByOtherPartiesTill(CampaignTime)`
 - Optional: `PartyVisualManager` visibility toggles if needed (SAS technique)

Steps:
1) On enlist and ticks: set escort to commander, set camera follow to commander’s `PartyBase`.
2) Hide player party visuals: set `IsVisible=false`. For additional suppression, unregister visual tracking (optional).
3) Set `IsActive=false` while attached so AI does not target the invisible party.
4) Temporarily enable `IsActive=true` when initiating player-owned events (see Feature 4) and restore after.

Acceptance:
- Main party shield/visuals are hidden; camera follows commander; world AI does not chase the player’s party.

Harmony (optional):
- If native nameplate persists, add patch to the PartyVisualManager/Tracker equivalent to unregister or prevent main party nameplate while enlisted.
  - Patch header must document the exact target (use reflection pattern per blueprint) and guard by `IsEnlisted`.

### Feature 4 — Engagement routing and time-control safety
Intent: Join battles/settlements predictably without exposing the main party.

APIs:
- Encounters: `PlayerEncounter` (finish/drain) and event managers for map events/sieges
- Menus/time: `args.MenuContext.GameMenu.StartWait()`, `Campaign.Current.GameMenuManager.RefreshMenuOptions(...)`, `Campaign.Current.TimeControlMode`
 - `GameMenuOption.LeaveType` (informative UI flags)

Steps:
1) Before opening hub/status menu, finish any lingering `PlayerEncounter` and drain `GameMenu.ExitToLast()` a few times.
2) While status/report menus are open, ensure time is `StoppablePlay` and `StartWait()` is called so controls are responsive.
3) When commander engages, either: a) stay attached (Freelancer parity) or b) re-enable `IsActive=true` and route into the event for player participation.

Acceptance:
- No stuck menus; no time-control deadlocks; battles/sieges can be joined consistently.

### Feature 5 — Wages, XP, reputation (Hybrid SAS + Freelancer)
Intent: Provide configurable wage/XP/relation model.

APIs:
- `GiveGoldAction.ApplyBetweenCharacters(...)`
- `ChangeRelationAction.ApplyPlayerRelation(...)`
- `Hero.MainHero.AddSkillXp(...)` (optional role-based XP)

Design:
- Default: SAS-style daily XP → tier advancement with daily wage from level + XP budget (config: `DailyXP`, `XPtoWageRatio`, `MaxWage`).
- Option: Freelancer-style wage formula — `TroopWage + (kills * 2) + Tier` (cap), with bodyguard multiplier.

Steps:
1) Implement `WageAndReputationService` with strategy: `SAS` or `Freelancer` (from settings).
2) `DailyTick`: pay wage, adjust faction/lord relations, add XP, apply role-based skill XP if enabled.

Verification (official docs):
- `GiveGoldAction`, `ChangeRelationAction`, `Hero` XP APIs: https://apidoc.bannerlord.com/v/1.2.12/

Acceptance:
- Wage/XP/relation changes match selected strategy; values visible in status/report.

### Feature 6 — Promotion + equipment selection
Intent: On tier-up, present a promotion path and equipment choices.

APIs:
- Dialogs/menus (as above). For UI selection, use Gauntlet if available, else staged menu options.
- Equipment APIs: `Hero.MainHero.BattleEquipment.FillFrom(...)`, `ItemObject`, `EquipmentIndex`
 - Optional Gauntlet: view-model layer similar to SAS `EquipmentSelectorBehavior.CreateVMLayer(...)`

Steps:
1) On tick/hourly tick, compare `Experience` vs tier thresholds. On tier-up, log and open promotion dialog.
2) Offer categories (weapons/armor/horse) that open an equipment selector.
   - Phase 1: menu-driven list (simple, testable)
   - Phase 2: Gauntlet VM (optional) similar to SAS `EquipmentSelectorBehavior` pattern.
3) Apply equipment using safe `FillFrom` for battle gear; store/restore prior gear in state.

Verification (official docs):
- Equipment and items (`EquipmentElement`, `ItemObject`, `EquipmentIndex`): https://apidoc.bannerlord.com/v/1.2.12/

Acceptance:
- On tier increase, a promotion flow opens and lets the player select gear; result is applied safely.

Addendum (SAS menu parity):
- After core features, expand the Enlisted Menu to mirror SAS layout:
  - Header: leader/objective, enlist time, tier/formation, wage, current/next XP, assignment text.
  - Options: Visit Weaponsmith (open selector), Train with troops, Battle Commands toggle, Talk to..., Show reputation with factions, Ask commander for leave, Go to tavern (when in town), Ask for a different assignment, Abandon party.
- Use wait menus and dynamic enablement; pause time when opening equipment selector.
 - API: `AddWaitGameMenu`, `EnterSettlementAction`, mission controllers for arena/tavern cases (per docs);

### Feature 7 — Leave and desertion
Intent: Support immediate leave (SAS) and timed leave (Freelancer) with penalties.

APIs:
- `CampaignTime`, `CampaignTime.DaysFromNow(...)`
- `ChangeRelationAction.ApplyPlayerRelation(...)`

Steps:
1) Immediate leave option: discharge and restore player gear; re-enable main party; clear enlisted flags.
2) Timed leave option: set `IsOnLeave=true` and `LeaveDeadline=Now+AllowedDays`. If deadline passes without return, set `IsDeserter=true` and apply penalties (relation hits, optional faction crime flag).
3) Block re-enlistment with commander/faction while deserter or for a cooldown.

Verification (official docs):
- `CampaignTime`, `CampaignEvents`, relation actions: https://apidoc.bannerlord.com/v/1.2.12/

Acceptance:
- Immediate leave works; timed leave shows deadline and converts to deserter if late; penalties applied.

### Feature 8 — Commander death/imprisonment & player capture
Intent: Robust detach/recover logic.

APIs:
- Hero state: `Hero.IsAlive`, `Hero.IsPrisoner`
- Party: `MobileParty`

Steps:
1) Guard all tick logic with `Commander != null && Commander.IsAlive && Commander.PartyBelongedTo != null && !Commander.IsPrisoner`.
2) If invalid, gracefully detach (restore `IsActive/IsVisible`, camera back to main party) and notify.
3) While player is captive, damp interactions and temporarily `IgnoreByOtherPartiesTill(...)` upon release.

Verification (official docs):
- `Hero.IsAlive`, `Hero.IsPrisoner`, `MobileParty` guard usage: https://apidoc.bannerlord.com/v/1.2.12/

Acceptance:
- No crashes when commander dies/captured; clear, recoverable state after release.

### Feature 9 — Nameplate and tracker suppression (if needed)
Intent: Ensure main party shield/nameplate doesn’t reveal player while enlisted.

Public APIs first:
- Attempt to unregister from tracker manager via reflection-safe calls.

Harmony (only if required):
- Patch nameplate/visual tracker methods to ignore the main party when `IsEnlisted`.
- Follow blueprint TargetMethod reflection pattern; include header with Target/Why/Safety.

Acceptance:
- No main party plate/shield while enlisted; restored on discharge.

Post-core Feature — Freelancer Soldier Camp (optional)
Intent: Provide an optional “camp” submenu with time-boxed actions akin to Freelancer.

Scope (deferred until main SAS-like menu is complete):
- Add camp actions: scout/train/hunt with hours budget and scheduled completion.
- Add paid services: request new gear/mount; buy beer/medical.
- Add leave-of-absence request with `ALLOWED_LEAVE_DAYS` and deserter handling.

Notes:
- Implement as a separate submenu to avoid cluttering the main status hub.
 - API: `AddGameMenu`, `MBTextManager.SetTextVariable`, `CampaignTime`, `SimpleActionPair`-style scheduler, `GiveGoldAction` for paid services.

### Feature 10 — Observability & safety
Intent: Deterministic behavior; easy diagnosis.

Steps:
1) Log state transitions (enlist, leave, timed leave start, deserter, detach on death/capture).
2) Log encounter drains and menu switches.
3) Prefer early returns with guards; no silent catches.

Acceptance:
- Logs show clear sequence; issues reproducible and diagnosable.

### Harmony Strategy (when public API is insufficient)
- Use `[HarmonyPatch]` classes in `src/Mod.GameAdapters/Patches/`.
- Provide structured header comment per blueprint; prefer Prefix/Postfix; Transpiler only if necessary.
- Resolve targets via `AccessTools` reflection; add try/catch logging around `TargetMethod()` resolution.
- Gate patches behind settings (enable/disable) and `IsEnlisted` checks.
- Reference: Harmony guide (priority, unpatching, late patching) — https://docs.bannerlordmodding.lt/modding/harmony/

Verification (official docs):
- Harmony activation/priority/unpatching patterns: https://docs.bannerlordmodding.lt/modding/harmony/

### Implementation Order (One Feature at a Time)
1) Enlistment lifecycle (dialogs + status menu)
2) Persistence
3) Commander follow + invisibility + AI safety
4) Engagement routing + time-control
5) Wages/XP/reputation (SAS default; Freelancer optional mode)
6) Promotion + equipment selection (menu → optional Gauntlet)
7) Leave and desertion
8) Commander death/imprisonment & player capture
9) Nameplate/tracker suppression (Harmony if required)
10) Observability polish

### Acceptance Matrix (sample)
- Enlist → Status menu opens; time controls work; camera follow commander.
- Save/Load → State restored; no duplicate dialogs; escort/camera/hidden party intact.
- Battle → No AI targeting of hidden party; participation works when enabled.
- Promotion → Tier-up detected; equipment applied.
- Leave (timed) → Deadline shows; deserter if late; penalties applied.

### Notes
- Always verify API usage against Bannerlord 1.2.12 docs: https://apidoc.bannerlord.com/v/1.2.12/
- Keep patches minimal and documented; prefer public APIs first; ensure reversibility.


