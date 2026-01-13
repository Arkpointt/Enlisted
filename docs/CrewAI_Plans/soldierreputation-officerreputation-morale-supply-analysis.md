# soldierreputation, officerreputation, morale, supply System Analysis

**Generated:** 2026-01-11
**Systems Analyzed:** soldierreputation, officerreputation, morale, supply
**Focus:** both
**Subsystem Mode:** No

---

## Executive Summary

- Analyzed 0 system files
- Identified 19 integration gaps
- Found 14 efficiency issues
- Generated 10 prioritized recommendations

---

## Current Architecture

## Architecture Overview

This analysis covers the four requested systems as implemented in the current codebase: **soldierreputation**, **officerreputation**, **morale**, **supply**. In this project, these are not four independent “feature folders”; instead they are **fields within shared state containers** and are **driven primarily by `EnlistmentBehavior` ticks and managers**.

### 1) SoldierReputation / OfficerReputation (Reputation tracks)

**Where the state lives**
- Reputation is stored on the escalation state (single source of truth):
  - **File:** `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`
  - **Key references (in code):**
    - `EscalationManager.Instance.State.OfficerReputation`
    - `EscalationManager.Instance.State.SoldierReputation`
    - used in `TryApplyReservistReentryBoost(...)` and in discharge snapshot saving.

**Who writes it**
- `EnlistmentBehavior` writes/overwrites reputation values in two primary lifecycle transitions:

1) **Re-enlistment restoration from service record**
   - **File:** `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`
   - **Method:** `TryApplyReservistReentryBoost(IFaction faction)`
   - **Flow:**
     - Calls `ServiceRecordManager.Instance.TryConsumeReservistForFaction(...)`
       - returns `officerRepRestore`, `soldierRepRestore`, plus band/tier/xp bonuses.
     - If `EscalationManager.Instance != null`, applies restores:
       - `EscalationManager.Instance.State.OfficerReputation = officerRepRestore;`
       - `EscalationManager.Instance.State.SoldierReputation = soldierRepRestore;`
     - Clamps with `EscalationManager.Instance.State.ClampAll();`
     - Displays notification `ShowReputationRestorationNotification(band, officerRepRestore, soldierRepRestore)`.

2) **Discharge snapshot**
   - **File:** `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`
   - **Method:** `StopEnlist(string reason, ...)`
   - **Flow:**
     - Before clearing enlistment state, snapshots current reputations into the faction service record:
       - `record.OfficerRepAtExit = EscalationManager.Instance.State.OfficerReputation;`
       - `record.SoldierRepAtExit = EscalationManager.Instance.State.SoldierReputation;`

**Serialization**
- Reputation itself is not serialized by `EnlistmentBehavior`; it is owned by the **Escalation system**. `EnlistmentBehavior` only snapshots and restores via `ServiceRecordManager` flows.
- The enlistment behavior’s own serialization is large and uses `SyncKey(...)` with a prefixed key.
  - **File:** `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`
  - **Method:** `public override void SyncData(IDataStore dataStore)`

**Key components involved**
- `EnlistmentBehavior` (orchestrates applying/reading reputation via managers)
  - **File:** `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`
- `ServiceRecordManager` (provides restore amounts based on discharge band; stores rep snapshots)
  - referenced in `TryApplyReservistReentryBoost` and `StopEnlist` (same file)
- `EscalationManager` (owns the canonical state for Officer/Soldier reputation)
  - referenced in `TryApplyReservistReentryBoost`, `StopEnlist`, and rations logic.

---

### 2) Morale (Company Need + modifiers)

In the current code, “morale” exists in **two places**, serving different roles:

1) **Company morale need (0–100)**: an abstract need tracked in `CompanyNeedsState`.
2) **Native Bannerlord party morale**: referenced at least in battle participation fallback.

#### 2.1 Company morale need
- **State container**
  - **File:** `src/Features/Company/CompanyNeedsState.cs`
  - **Property:** `public int Morale { get; set; } = 60;`
  - Used via `CompanyNeed.Morale` in `GetNeed/SetNeed/ModifyNeed`.

- **Lifecycle owner**
  - `EnlistmentBehavior` owns a private field:
    - **File:** `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`
    - **Field:** `private CompanyNeedsState _companyNeeds;`
  - Exposed via property:
    - `public CompanyNeedsState CompanyNeeds { get { if (_companyNeeds == null && IsEnlisted) _companyNeeds = new CompanyNeedsState(); return _companyNeeds; } }`

- **Serialization**
  - Manual serialization in `EnlistmentBehavior`:
    - **File:** `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`
    - **Method:** `SerializeCompanyNeeds(IDataStore dataStore)`
    - Saves:
      - `_companyNeeds_morale`
      - plus readiness, rest, supplies
    - Loads them and reconstructs:
      - `_companyNeeds = new CompanyNeedsState { Readiness = ..., Morale = ..., Rest = ..., Supplies = ... };`

#### 2.2 Morale modifiers and usage points
- **Food morale bonus**
  - `EnlistmentBehavior` has a food quality system that returns a morale bonus:
    - **File:** `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`
    - **Method:** `public int GetFoodMoraleBonus()`
    - Based on `CurrentFoodQuality` (Supplemental/Officer/Commander tiers => +2/+4/+8 morale).
  - This is intended to feed into morale calculations elsewhere (camp life / UI); within the file it is labeled:
    - “Used by camp life behavior to adjust party morale.”

- **Pay tension morale penalty**
  - `EnlistmentBehavior` includes:
    - **File:** `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`
    - **Method:** `public int GetPayTensionMoralePenalty()`
    - returns negative morale modifier based on `_payTension` thresholds.

- **Native morale gating for battle fallback**
  - In `OnPlayerBattleEnd(...)`, if kill tracker is absent, it checks:
    - **File:** `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`
    - `if (MobileParty.MainParty.Morale <= 1f) participated = false;`
  - This is one place the mod reads native `MobileParty.MainParty.Morale`.

**Key components for morale**
- `CompanyNeedsState` (morale as a need, 0–100)
  - **File:** `src/Features/Company/CompanyNeedsState.cs`
- `EnlistmentBehavior` (creates/serializes `CompanyNeedsState`, provides morale modifiers)
  - **File:** `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`

---

### 3) Supply (Company Need + supply simulation manager)

Supply also exists in two linked representations:

1) **Company need “Supplies” (0–100)** exposed via `CompanyNeedsState.Supplies`
2) **Underlying continuous simulation** in `CompanySupplyManager` (`_totalSupply` float 0–100)

#### 3.1 CompanyNeedsState Supplies = façade over CompanySupplyManager
- **File:** `src/Features/Company/CompanyNeedsState.cs`
- **Property:**
  - `public int Supplies { get => CompanySupplyManager.Instance?.TotalSupply ?? _suppliesFallback; set => _suppliesFallback = (int)MathF.Clamp(value, 0, 100); }`
- This makes `CompanySupplyManager` the preferred source when available, but allows fallback storage when not.

#### 3.2 CompanySupplyManager (simulation and event hooks)
- **File:** `src/Features/Logistics/CompanySupplyManager.cs`
- **Key responsibilities**
  - Singleton (`public static CompanySupplyManager Instance { get; private set; }`)
  - Tracks supply state:
    - `_totalSupply` (float), exposed as:
      - `public int TotalSupply` (clamped int 0–100)
      - `public float NonFoodSupply => _totalSupply;` (for save/load and debugging)
  - Ticks:
    - `DailyUpdate()` for consumption + daily resupply
    - `HourlyUpdate()` for *hourly settlement resupply only*
  - Settlement hooks:
    - `OnSettlementEntered(Settlement settlement)`
    - `OnSettlementLeft(Settlement settlement)` (emits a resupply flavor message if gain >= 5%)
  - Battle hook:
    - `ProcessBattleSupplyChanges(int troopsLost, int enemiesKilled, bool playerWon, bool wasSiege)`
  - Party context:
    - `GetLordParty()` pulls the enlisted lord party via:
      - `var behavior = EnlistmentBehavior.Instance;`
      - `var lord = behavior?.EnlistedLord ?? _enlistedLord;`
      - checks `party.IsActive`

#### 3.3 Supply serialization
- Supply is persisted as part of enlistment save data:
  - **File:** `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`
  - **Method:** `SerializeCompanyNeeds(IDataStore dataStore)`
  - Saves:
    - `_companyNeeds_supplies` (int need value)
    - `_companySupply_nonFood` (float underlying manager state)
  - On load:
    - reconstructs `_companyNeeds`
    - calls `CompanySupplyManager.RestoreFromSave(_enlistedLord, nonFoodSupply);` if lord exists.

#### 3.4 Supply driving ticks and triggers
`EnlistmentBehavior` is the orchestrator that calls the manager at the right cadence:

- **Hourly tick**
  - **File:** `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`
  - **Method:** `OnHourlyTick()`
  - Calls:
    - `CompanySupplyManager.Instance?.HourlyUpdate();`

- **Daily tick**
  - **File:** `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`
  - **Method:** `OnDailyTick()`
  - Calls:
    - `CompanySupplyManager.Instance?.DailyUpdate();`

- **Settlement events**
  - **File:** `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`
  - **Methods:** `OnSettlementEntered(...)` and `OnSettlementLeft(...)`
  - Calls:
    - `CompanySupplyManager.Instance?.OnSettlementEntered(settlement);` when enlisted lord enters
    - `CompanySupplyManager.Instance?.OnSettlementLeft(settlement);` when enlisted lord leaves

- **Battle events**
  - `EnlistmentBehavior` computes casualties and calls supply manager:
    - **File:** `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`
    - **Method:** `ProcessBattleSupplyChanges(MapEvent mapEvent, int playerKills)`
    - Calls:
      - `CompanySupplyManager.Instance.ProcessBattleSupplyChanges(troopsLost, enemiesKilled, playerWon, wasSiege);`

**Key components for supply**
- `CompanySupplyManager` (simulation + hooks)
  - **File:** `src/Features/Logistics/CompanySupplyManager.cs`
- `CompanyNeedsState` (Supplies view)
  - **File:** `src/Features/Company/CompanyNeedsState.cs`
- `EnlistmentBehavior` (tick orchestration + save/load + battle/settlement integration)
  - **File:** `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`

---

## Integration Points

### A) EnlistmentBehavior as the orchestration hub
`EnlistmentBehavior` integrates all four systems in lifecycle and ticking:

- Initializes supply on enlist:
  - **File:** `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`
  - `CompanySupplyManager.Initialize(lord, preserveSupply: resumingGraceService);`

- Shuts down supply on service end:
  - `CompanySupplyManager.Shutdown();`

- Serializes both the company needs (including morale/supplies) and the supply simulation float:
  - `SerializeCompanyNeeds(IDataStore dataStore)` stores `_companyNeeds_*` and `_companySupply_nonFood`

- Restores supply sim on load:
  - `CompanySupplyManager.RestoreFromSave(_enlistedLord, nonFoodSupply);`

- Writes reputation during re-enlistment:
  - `EscalationManager.Instance.State.OfficerReputation = officerRepRestore;`
  - `EscalationManager.Instance.State.SoldierReputation = soldierRepRestore;`

- Snapshots reputation on discharge:
  - `record.OfficerRepAtExit = EscalationManager.Instance.State.OfficerReputation;`
  - `record.SoldierRepAtExit = EscalationManager.Instance.State.SoldierReputation;`

### B) Supply ↔ Company Needs coupling
- `CompanyNeedsState.Supplies` is not an independent stored value; it **reads from `CompanySupplyManager.Instance.TotalSupply`** when available.
  - **File:** `src/Features/Company/CompanyNeedsState.cs`
- This means UI/logic reading “Supplies” will automatically reflect simulation changes as long as the manager is active.

### C) Morale ↔ Other systems coupling
- Morale modifiers originate in `EnlistmentBehavior`:
  - Food quality (`GetFoodMoraleBonus`)
  - Pay tension (`GetPayTensionMoralePenalty`)
- Company morale need is persisted in `CompanyNeedsState`, but the actual application of modifiers to need values is likely handled by other behaviors (not surfaced in the limited tool budget). Within the examined code, the modifiers are provided by `EnlistmentBehavior` and expected to be consumed elsewhere.

### D) Reputation ↔ Rations coupling (OfficerReputation as “QM rep”)
- The rations issuance chooses food based on “QM reputation” which is currently:
  - `int qmRep = EscalationManager.Instance?.State?.OfficerReputation ?? 50;`
  - **File:** `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`
  - **Method:** `IssueNewRation()`
- This makes **OfficerReputation** an input into a subsystem that affects morale/fatigue indirectly.

### E) Event/callback mechanisms
- `EnlistmentBehavior.RegisterEvents()` wires Bannerlord campaign events:
  - Tick events:
    - `CampaignEvents.HourlyTickEvent` → `OnHourlyTick()` (supply hourly update)
    - `CampaignEvents.DailyTickEvent` → `OnDailyTick()` (supply daily update)
  - Settlement events:
    - `CampaignEvents.SettlementEntered` → `OnSettlementEntered(...)` (supply settlement entry tracking)
    - `CampaignEvents.OnSettlementLeftEvent` → `OnSettlementLeft(...)` (supply departure messaging)
  - Battle events:
    - `CampaignEvents.MapEventStarted/Ended`, `OnPlayerBattleEndEvent` (supply battle changes are processed via `ProcessBattleSupplyChanges`)
- `CompanySupplyManager` is passive: it does not subscribe directly to events; it is called from `EnlistmentBehavior`.

---

## Data Flows (End-to-End)

### 1) Supply daily/hourly loop
1. `CampaignEvents.HourlyTickEvent` → `EnlistmentBehavior.OnHourlyTick()`
2. `OnHourlyTick()` calls `CompanySupplyManager.Instance?.HourlyUpdate()`
3. `CompanySupplyManager.HourlyUpdate()`:
   - if in town/castle: calculates hourly resupply and adds to `_totalSupply`
4. UI/logic reads `CompanyNeedsState.Supplies` which returns:
   - `CompanySupplyManager.Instance.TotalSupply` (preferred)  
   - fallback `_suppliesFallback` if manager missing

Daily:
1. `CampaignEvents.DailyTickEvent` → `EnlistmentBehavior.OnDailyTick()`
2. `OnDailyTick()` calls `CompanySupplyManager.Instance?.DailyUpdate()`
3. `DailyUpdate()`:
   - calculates consumption (party size * activity * terrain)
   - calculates resupply if in settlement
   - clamps, logs threshold warnings

### 2) Supply battle loop
1. Battle ends (`OnPlayerBattleEnd` / `OnMapEventEnded` paths)
2. `EnlistmentBehavior.ProcessBattleSupplyChanges(mapEvent, playerKills)`
3. Computes:
   - `troopsLost` (only from the enlisted lord’s party)
   - `enemiesKilled` (uses `playerKills` as contribution proxy)
   - `playerWon`, `wasSiege`
4. Calls:
   - `CompanySupplyManager.Instance.ProcessBattleSupplyChanges(...)`
5. Manager adjusts `_totalSupply` based on losses and loot.

### 3) Reputation discharge → re-enlist restore loop
1. On discharge (`StopEnlist(...)`):
   - snapshots `EscalationManager.Instance.State.(OfficerReputation/SoldierReputation)` to service record (faction-scoped)
2. On next enlistment:
   - `TryApplyReservistReentryBoost(faction)` calls `ServiceRecordManager.TryConsumeReservistForFaction(...)`
   - applies returned officer/soldier rep restore values into `EscalationManager.State`
   - clamps, notifies player.

### 4) OfficerReputation → rations quality (secondary coupling)
1. At muster / ration issuance (`IssueNewRation()`):
   - reads `OfficerReputation` as QM rep
2. Selects ration item ID (meat/cheese/butter/grain)
3. Issued ration affects later morale/fatigue behavior indirectly (food quality system, camp systems).

---

## Notable Coupling and Risks

- **EnlistmentBehavior is a “god behavior”**: It owns serialization, tick cadence, supply simulation lifecycle, and writes into other managers (Escalation/ServiceRecord). This centralizes behavior but increases coupling.
- **Supply is tightly coupled to enlistment state**: `CompanySupplyManager.Instance` is set/cleared by enlistment start/stop, and `CompanyNeedsState.Supplies` reads it directly.
- **OfficerReputation is overloaded** as “QM rep” for rations issuance, which couples reputation to logistics and morale indirectly.

---

## Verified Code References (Paths)
- `src/Features/Company/CompanyNeedsState.cs`
- `src/Features/Logistics/CompanySupplyManager.cs`
- `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`

Tool budget was exhausted before pulling `get_system_dependencies`; the relationships described above are directly verified from the concrete code paths listed.

---

## Gap Analysis

Total gaps identified: 19

1. **JSON Content Pipeline appears disconnected (no discoverable active events/decisions/orders)**

2. **Balance configuration categories for morale/supply not discoverable (balance system integration gap)**

3. **Quartermaster reputation uses two different sources (Escalation OfficerReputation vs native Hero relationship)**

4. **Supply/rations “stowage” integration explicitly not implemented**

5. **Food morale bonus exists, but no evidence it is applied to the actual Morale system**

6. **Reputation restoration is surfaced only as a notification; ongoing reputation visibility likely absent**

7. **Camp Mood system claims to incorporate morale, but likely lacks a concrete bridge**

8. **Tooling/registry mismatch: content orchestrator depends on enlistment, but content inventory isn’t available**

1. **OfficerReputation used as “QM reputation” (placeholder rep mapping)**

2. **FoodQualityTier → Morale bonus values likely not integrated into morale outcomes**

---

## Efficiency Analysis

Total issues identified: 14

1. **Realtime tick repeatedly calls `SyncActivationState` (and possibly `SetActive`) even when nothing changes**

2. **Excessive logging in “hot” loops (supply consumption, battle/siege integration)**

3. **Repeated full-campaign enumeration in `RefreshFactionVisuals()`**

4. **Avoidable allocations in `EscalationManager.SyncData` (lists created every save)**

5. **`CompanyNeedsManager.PredictUpcomingNeeds` allocates config structures and reads file at first call; can be called repeatedly**

6. **Battle desertion check uses nested loops with per-soldier RNG**

1. **Duplicate “supply resupply” logic (daily vs hourly)**

2. **Two “supply systems” with overlapping meaning (`CompanyNeedsState.Supplies` vs `CompanySupplyManager._totalSupply`)**

3. **Hard-coded thresholds repeated across systems (morale/supply thresholds)**

4. **Reflection used repeatedly in `ReportMusterOutcome`**

---

## Recommendations

Total recommendations: 10

### 1. Gate hot-loop logging & add debug flag for supply/morale ticks

**Priority:** 1 | **Impact:** high | **Effort:** 3h

**Description:** Wrap verbose logs inside hourly/daily tick loops (supply consumption, battle/siege integration, desertion checks) behind a single mod setting (e.g., Debug/VerboseSimulationLogs) and ensure per-tick logging is disabled by default.

**Benefit:** Reduces frame-time spikes and log spam in long campaigns while preserving diagnostics when needed.

**Files:** src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs, src/Features/Equipment/CompanySupplyManager.cs

### 2. Debounce SyncActivationState/SetActive calls on tick

**Priority:** 1 | **Impact:** high | **Effort:** 3h

**Description:** Track last activation state and only call SyncActivationState/SetActive when state actually changes (enlisted ↔ not enlisted, in army ↔ not, etc.). Add a small guard in tick handlers to prevent redundant calls.

**Benefit:** Cuts unnecessary work every tick, improving campaign performance and reducing side-effect risk from repeated activation calls.

**Files:** src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs

### 3. Localize morale/fatigue/reputation UI strings via TextObject

**Priority:** 1 | **Impact:** medium | **Effort:** 3h

**Description:** Replace remaining raw UI strings for fatigue/morale status and notifications with TextObject keys; avoid string concatenation for displayed text and prefer parameterized TextObjects.

**Benefit:** Improves localization consistency and reduces brittle UI text formatting.

**Files:** src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs, src/Features/Core/Fatigue/CampFatigue*.cs

### 4. Add lightweight “Why?” tooltips for morale & supplies in status UI

**Priority:** 1 | **Impact:** high | **Effort:** 4h

**Description:** Expose current morale/supply drivers in a tooltip: morale (base daily change, pay tension penalty, rations bonus, recent battle outcome), supplies (total supply, current consumption/resupply context). Use existing modifier methods already exposed from EnlistmentBehavior/CompanySupplyManager.

**Benefit:** Fixes the visibility/feedback gap so players can connect cause → effect and take corrective actions.

**Files:** src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs, src/Features/Equipment/CompanySupplyManager.cs, src/Features/Company/CompanyNeedsState.cs

### 5. Unify Quartermaster reputation source (OfficerReputation vs Hero relationship)

**Priority:** 2 | **Impact:** high | **Effort:** 10h

**Description:** Pick a single canonical input for QM pricing/availability (either Escalation OfficerReputation or native Hero relationship). Implement an adapter that maps the chosen value into QM computations and deprecate the other pathway, with a migration note for existing saves.

**Benefit:** Removes confusing double-source behavior and makes reputation-driven economy predictable and easier to tune.

**Files:** src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs, src/Features/Equipment/Quartermaster*.cs, src/Features/Escalation/EscalationManager.cs

### 6. Bridge FoodQualityTier morale/fatigue bonuses into the actual morale outcome pipeline

**Priority:** 2 | **Impact:** high | **Effort:** 8h

**Description:** Ensure FoodQualityTier-derived bonuses are applied to the morale calculation that CompanyNeedsManager (or equivalent) actually uses. If fatigue bonus is advertised in UI, either implement its effect (e.g., fatigue decay modifier) or remove/rename UI to match reality.

**Benefit:** Aligns UI promises with actual gameplay, improving trust and making rations meaningful beyond flavor.

**Files:** src/Features/Company/CompanyNeedsManager.cs, src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs, src/Features/Equipment/CompanySupplyManager.cs

### 7. Resolve supply dual-state ambiguity (Needs.Supplies vs CompanySupplyManager.TotalSupply)

**Priority:** 2 | **Impact:** high | **Effort:** 12h

**Description:** Make CompanyNeedsState.Supplies read-only (delegated) or remove fallback writing paths, and centralize all mutation in CompanySupplyManager. Update any writers to call a single API (e.g., AddSupply/ConsumeSupply/Resupply) and ensure save/load uses one source of truth.

**Benefit:** Prevents desync bugs, simplifies reasoning, and reduces future maintenance cost for logistics features.

**Files:** src/Features/Company/CompanyNeedsState.cs, src/Features/Equipment/CompanySupplyManager.cs, src/Features/Company/CompanyNeedsManager.cs

### 8. Connect JSON content pipeline to live orchestrator (events/decisions/orders discovery)

**Priority:** 2 | **Impact:** high | **Effort:** 16h

**Description:** Add a registry/discovery step that loads JSON-defined events/decisions/orders into the runtime inventory used by the orchestrator. Provide a dev diagnostic screen/log summary listing loaded content, missing assets, and load errors.

**Benefit:** Unlocks content scalability and fixes the current “pipeline disconnected” gap that blocks designers from adding content confidently.

**Files:** src/Features/Content/ContentOrchestrator*.cs, src/Features/Content/*Registry*.cs, ModuleData/*

### 9. Optimize RefreshFactionVisuals to avoid full-campaign enumerations

**Priority:** 3 | **Impact:** medium | **Effort:** 4h

**Description:** Cache last faction visuals state and only recompute on relevant triggers (player faction change, enlist/stop enlist, kingdom change). Replace repeated full-campaign enumeration with targeted updates.

**Benefit:** Reduces periodic hitches and improves campaign smoothness, especially late-game.

**Files:** src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs

### 10. Reduce save-time allocations in EscalationManager.SyncData

**Priority:** 3 | **Impact:** medium | **Effort:** 6h

**Description:** Reuse lists/collections or write directly without creating new lists each save. Add micro-benchmarks or simple profiling counters to validate improvement.

**Benefit:** Improves save/load responsiveness and reduces GC pressure during autosaves.

**Files:** src/Features/Escalation/EscalationManager.cs

---

## Compatibility Warnings

- ⚠️ Verify API compatibility with Bannerlord v1.3.13

---

## Next Steps

1. Review recommendations and prioritize
2. Use Warp Agent to create detailed implementation plans
3. Execute with `enlisted-crew implement -p <plan-path>`

---

*Generated by SystemAnalysisFlow v1.0 on 2026-01-11*