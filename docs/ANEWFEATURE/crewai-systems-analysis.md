# Promotion, Reputation, Identity, Orchestrator System Analysis

**Generated:** 2026-01-10
**Systems Analyzed:** Promotion, Reputation, Identity, Orchestrator
**Focus:** integration
**Subsystem Mode:** No

---

## Executive Summary

- Analyzed 1 system files
- Identified 0 integration gaps
- Found 0 efficiency issues
- Generated 10 prioritized recommendations

---

## Current Architecture

## Architecture Overview

### 1) Promotion System

**Purpose / Responsibility**
- Owns the **tier advancement** logic (T1–T9), decides when the player is eligible to advance, and triggers the **proving event** (if available) or **fallback direct promotion**.
- Provides **UI-friendly** helpers for promotion eligibility and progress.

**Key Components (verified paths)**
1. **Behavior: `PromotionBehavior`**
   - **File:** `src/Features/Ranks/Behaviors/PromotionBehavior.cs`
   - **Role:**
     - Registers tick callbacks:
       - `CampaignEvents.OnSessionLaunchedEvent` → `OnSessionLaunched()`
       - `CampaignEvents.HourlyTickEvent` → `OnHourlyTick()`, which calls `CheckForPromotion()`
     - Subscribes to enlistment lifecycle events:
       - `EnlistmentBehavior.OnEnlisted` → `OnNewEnlistmentStarted(Hero lord)`
       - `EnlistmentBehavior.OnDischarged` → `OnEnlistmentEnded(string reason)`
     - Persists promotion tracking:
       - `_lastPromotionCheck`, `_pendingPromotionTier` via `SyncData(IDataStore dataStore)`.

2. **Rule Table: `PromotionRequirements`**
   - **File:** `src/Features/Ranks/Behaviors/PromotionBehavior.cs` (same file)
   - **Role:** Static requirement lookup for non-XP requirements (days, battles, rep thresholds, max discipline, etc.)
   - XP thresholds explicitly sourced from config:
     - `ConfigurationManager.GetTierXpRequirements()` (comment: “single source of truth”)

**Promotion Data Flow**
1. **Hourly Tick** (`CampaignEvents.HourlyTickEvent`)
2. `PromotionBehavior.OnHourlyTick()` sets `_lastPromotionCheck = CampaignTime.Now` and calls `CheckForPromotion()`.
3. `CheckForPromotion()`:
   - Reads enlistment state from `EnlistmentBehavior.Instance` (must be enlisted).
   - Determines next tier (`targetTier = currentTier + 1`) and max tier from config (`ConfigurationManager.GetMaxTier()`).
   - Calls `CanPromote()`:
     - Validates XP threshold (from `ConfigurationManager.GetTierXpRequirements()`).
     - Validates service time (`enlistment.DaysInRank`) and battles survived (`enlistment.BattlesSurvived`).
     - Validates **reputation/discipline** via escalation:
       - Reads `EscalationManager.Instance?.State?.SoldierReputation` and `...Discipline`.
     - Validates leader relation:
       - `enlistment.EnlistedLord.GetRelationWithPlayer()`.
   - If eligible and not declined:
     - Checks `EscalationManager.Instance?.HasDeclinedPromotion(targetTier)`
     - Prevents spam via `_pendingPromotionTier`.
   - Tries to queue proving event:
     - Resolves event id via `GetProvingEventId(currentTier, targetTier)`
     - Looks up event: `Content.EventCatalog.GetEventById(eventId)`
     - Queues via `Content.EventDeliveryManager.Instance.QueueEvent(provingEvent)`
   - If missing event or delivery unavailable → **fallback**:
     - `FallbackDirectPromotion(targetTier, enlistment)`:
       - `enlistment.SetTier(targetTier)`
       - `QuartermasterManager.Instance?.UpdateNewlyUnlockedItems()`
       - Shows promotion notification: `TriggerPromotionNotification(targetTier)`

**Promotion Integration Points**
- **Enlistment lifecycle**: resets pending promotion state on enlistment start/end.
  - `EnlistmentBehavior.OnEnlisted` / `OnDischarged` subscriptions in `PromotionBehavior.RegisterEvents()`.
- **Escalation**: uses escalation state for soldier reputation and discipline gating.
  - `EscalationManager.Instance?.State?.SoldierReputation`, `...Discipline`
  - Uses `EscalationManager.Instance?.HasDeclinedPromotion(targetTier)` to require dialog-request after a decline.
  - Clears decline flags on new enlistment: `EscalationManager.Instance?.ClearAllDeclinedPromotions()`.
- **Content delivery**: queues proving events through `EventDeliveryManager`, consuming content definitions via `EventCatalog`.
- **Quartermaster**: after direct promotion, refreshes unlocks:
  - `QuartermasterManager.Instance?.UpdateNewlyUnlockedItems()`.

**Dependency graph (tool-verified)**
- `PromotionBehavior` depends on `EnlistmentBehavior`
  - (from `get_system_dependencies("PromotionBehavior")`)


---

### 2) Reputation System

**Purpose / Responsibility**
- Reputation in Enlisted is split conceptually across:
  1) **Native relationship** (“QM reputation”) using Bannerlord’s `Hero.GetRelation(...)`.
  2) **Service reputation tracks** (Officer/Soldier) represented in **EscalationState** and used in promotion gating and other systems.
- In the current code slice, there is no standalone `ReputationBehavior`; **reputation behaviors are embedded** into enlistment + escalation systems.

**Key Components (verified paths)**
1. **Behavior: `EnlistmentBehavior` (reputation access + restoration flows)**
   - **File:** `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`
   - Verified snippets:
     - Reputation restoration UI path:
       - `ShowReputationRestorationNotification(string band, int officerRepRestore, int soldierRepRestore)` (found in search results)
     - QM reputation using native relationship:
       - `GetQMReputation()` uses `Hero.MainHero.GetRelation(qm)` and clamps.

2. **State + rules: `EscalationManager` / `EscalationState`**
   - **File:** `src/Features/Escalation/EscalationManager.cs` (identified in search)
   - Promotion reads from escalation:
     - `EscalationManager.Instance?.State?.SoldierReputation`
     - `EscalationManager.Instance?.State?.Discipline`

**Reputation Data Flows**
A) **QM reputation (native relationship)**
1. Consumer calls `EnlistmentBehavior.GetQMReputation()`  
   **File:** `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs` (snippet located by search)
2. Method:
   - Resolves quartermaster hero (`GetOrCreateQuartermaster()`)
   - Reads native relationship: `Hero.MainHero.GetRelation(qm)`
   - Returns clamped value to mod’s intended range.

B) **Soldier/Officer reputation (escalation track)**
1. Reputation values are stored in `EscalationManager.Instance.State`.
2. Promotion checks read from escalation state:
   - In `PromotionBehavior.CanPromote()`:
     - `var soldierRep = escalation.State?.SoldierReputation ?? 0;`
     - `var discipline = escalation.State?.Discipline ?? 0;`

C) **Reputation restoration**
- On re-enlistment, enlistment logic shows a restoration notification:
  - `ShowReputationRestorationNotification(...)` in `EnlistmentBehavior`
  - Likely uses “discharge band” concept and restores Officer/Soldier reputation values (as parameters).

**Reputation Integration Points**
- **Promotion gating**: soldier reputation and discipline are direct constraints on tier advancement.
  - `PromotionBehavior` ↔ `EscalationManager.State`.
- **UI/news**: enlistment behavior shows restoration notifications (and other systems may publish to news feed; not fully inspected here due to tool limits).
- **Core dependencies**: `EnlistmentBehavior` is a central hub depended upon by many systems.

**Dependency graph (tool-verified)**
- `EnlistmentBehavior` depends on:
  - `Hero.MainHero`
  - `Campaign.Current`
- Systems depending on `EnlistmentBehavior` include:
  - `ContentOrchestrator`, `EscalationManager`, `CompanySimulationBehavior`, `CampOpportunityGenerator`, `MusterMenuHandler`, `EnlistedMenuBehavior`, `OrderManager`, `PromotionBehavior`
  - (from `get_system_dependencies("EnlistmentBehavior")`)

> Note: “Reputation tracks (Lord/Officer/Soldier)” are part of the project’s domain model, but the code slice we inspected shows **SoldierReputation** and discipline gating through escalation, and a **QM relationship** through native relations. A dedicated `ReputationBehavior` file was not found in the limited searches performed.


---

### 3) Identity System

**Purpose / Responsibility**
- Provides a **role/identity label** for the enlisted player (Officer/Scout/Medic/Engineer/Operative/NCO/Soldier) derived from **traits and skills**.
- Intended as a **query-only utility** (no events, no persistence), used by UI/status surfaces.

**Key Components (verified paths)**
1. **Behavior: `EnlistedStatusManager`**
   - **File:** `src/Features/Identity/EnlistedStatusManager.cs`
   - **State:** none persisted (`SyncData` no-op)
   - **Events:** none (`RegisterEvents` no-op)

**Identity Data Flow**
1. Caller (UI, menu, report generator) calls:
   - `GetPrimaryRole()`
   - `GetRoleDescription()`
   - `GetAllSpecializations()`
2. Internals:
   - Reads `Hero.MainHero` (null guarded in each method).
   - Evaluates traits in priority order:
     - `DefaultTraits.Commander` (>= 10) → `"Officer"`
     - `DefaultTraits.ScoutSkills` (>= 10) → `"Scout"`
     - `DefaultTraits.Surgery` (>= 10) → `"Medic"`
     - `DefaultTraits.Siegecraft` (>= 10) → `"Engineer"`
     - `DefaultTraits.RogueSkills` (>= 10) → `"Operative"`
     - `DefaultTraits.SergeantCommandSkills` (>= 8) → `"NCO"`
     - Else `"Soldier"`
3. On exceptions:
   - Logs via `ModLogger.Error("Identity", ...)`
   - Returns safe defaults.

**Identity Integration Points**
- Purely reads from Bannerlord hero trait/skill APIs:
  - `hero.GetTraitLevel(...)`, `hero.GetSkillValue(...)`
- No direct coupling to promotion/reputation/orchestrator beyond being a common “status service” the UI can consume.

> Code-style note: this component returns strings and uses string interpolation (e.g., `$"Scout {level}, Scouting {value}"`). If this output is presented in UI, it may violate the project-wide “use TextObject for all UI strings” rule; however, this is the current verified implementation.


---

### 4) Orchestrator System (Content Orchestrator)

**Purpose / Responsibility**
- Central coordinator for **content pacing and scheduling**, providing:
  - **WorldSituation** snapshots to other systems (notably order progression and event selection fitness scoring).
  - **Opportunity pre-scheduling** per phase (Dawn/Midday/Dusk/Night) to prevent opportunities from disappearing as context changes.
  - **Phase transition processing**: expires missed opportunities, fires committed ones.
  - **Medical pressure orchestration**: queues medical opportunities and triggers illness-onset events.
  - **Override system**: need-based schedule overrides and variety injections loaded from JSON.

**Key Components (verified paths)**
1. **Behavior: `ContentOrchestrator`**
   - **File:** `src/Features/Content/ContentOrchestrator.cs`
   - Registers:
     - `CampaignEvents.DailyTickEvent` → `OnDailyTick()`
     - `CampaignEvents.HourlyTickEvent` → `OnHourlyTick()`
   - Persists:
     - `orchestrator_behaviorCounts`, `orchestrator_contentEngagement`
     - Phase state `orchestrator_lastPhase`
     - Medical tracking fields (`orchestrator_lastMedicalCheckDay`, etc.)
     - Uses `SaveLoadDiagnostics.SafeSyncData(...)`
   - Does not persist scheduled opportunities by design:
     - On load: `ForceReschedule()`

2. **State model: `ScheduledOpportunity`**
   - **File:** `src/Features/Content/ContentOrchestrator.cs` (top)
   - Fields:
     - `OpportunityId`, `TargetDecisionId`, `Phase`, `DisplayName`, `NarrativeHint`
     - `Consumed`, `PlayerCommitted`, `FitnessScore`, `SourceOpportunity`
   - Derived state:
     - `IsAvailableToCommit`, `IsScheduledToFire`

3. **World analysis utilities**
   - **World situation model:** `src/Features/Content/Models/WorldSituation.cs` (found by search)
   - **Analyzer:** `src/Features/Content/WorldStateAnalyzer.cs` (found by search)
   - Orchestrator calls `WorldStateAnalyzer.AnalyzeSituation()` and phase helpers like:
     - `WorldStateAnalyzer.GetDayPhaseFromHour(...)`
     - `WorldStateAnalyzer.GetCurrentDayPhase()`

4. **Pacing driver (auto narrative events)**
   - **Behavior:** `src/Features/Content/EventPacingManager.cs`
   - Uses:
     - `GlobalEventPacer.CanFireAutoEvent(...)`
     - `EventSelector.SelectEvent(worldSituation)` where `worldSituation` is obtained from `ContentOrchestrator.Instance?.GetCurrentWorldSituation()`
   - Queues automatic events via `EventDeliveryManager`.

5. **Override types**
   - **File:** `src/Features/Content/OrchestratorOverride.cs` (found by search)
   - Defines `OverrideType` enum (NeedBased, VarietyInjection, etc.)

**Orchestrator Data Flows**
A) **Daily orchestration**
1. `CampaignEvents.DailyTickEvent` → `ContentOrchestrator.OnDailyTick()`
2. Guard:
   - `IsActive()` checks `EnlistmentBehavior.Instance?.IsEnlisted == true`
3. Builds world situation:
   - `worldSituation = WorldStateAnalyzer.AnalyzeSituation()`
4. If orchestrator enabled (`ConfigurationManager.LoadOrchestratorConfig().Enabled == true`):
   - `SetQuietDayFromWorldState(worldSituation)` → updates `GlobalEventPacer.SetQuietDay(isQuiet)`
   - `GenerateForecastData(worldSituation)` (prepares cached data for UI; comments indicate other systems provide caches)
   - `ScheduleOpportunities()`:
     - One schedule per day `_scheduledDay`
     - Cross-day scheduling: late-day schedules tomorrow’s early phases
     - Prevents duplicates via `alreadyScheduledIds`
     - Computes phase “budget” via `DetermineOpportunityBudget(world, phase)` (uses `LordSituation`, phase, probation, supplies)
     - Candidates come from `CampOpportunityGenerator.Instance.GenerateCandidatesForPhase(phase)`
   - `RefreshCampOpportunities(worldSituation)` (legacy note)
   - `RefreshBaggageSimulation(worldSituation)`:
     - Uses `Logistics.BaggageTrainManager.Instance.CalculateEventProbabilities(worldSituation)` (method called inside try/catch)
   - `CheckMedicalPressure(worldSituation)`:
     - Reads pressure from `SimulationPressureCalculator.GetMedicalPressure()`
     - Queues camp medical opportunities via `CampOpportunityGenerator.Instance.QueueMedicalOpportunity(...)`
     - Triggers illness-onset events via `EventCatalog.GetEvent(eventId)` and `EventDeliveryManager.Instance.QueueEvent(eventDef)`
5. Updates tracked player-behavior learning state for save:
   - `_behaviorCounts = PlayerBehaviorTracker.GetBehaviorCountsForSave()`
   - `_contentEngagement = PlayerBehaviorTracker.GetContentEngagementForSave()`

B) **Hourly phase transitions**
1. `CampaignEvents.HourlyTickEvent` → `ContentOrchestrator.OnHourlyTick()`
2. `CheckPhaseTransition()`:
   - Determines phase by hour: `WorldStateAnalyzer.GetDayPhaseFromHour(...)`
   - On change → `OnDayPhaseChanged(newPhase)`:
     - Expires missed opportunities for previous phase:
       - `CleanupMissedOpportunities(previousPhase)` marks uncommitted as `Consumed`
     - Fires committed opportunities:
       - `FireCommittedOpportunities(newPhase)`:
         - Uses `GetOpportunitiesToFireNow()` (filters `PlayerCommitted && !Consumed`)
         - Converts a **Decision** into an **EventDefinition** and queues it:
           - `DecisionCatalog.GetDecision(opp.TargetDecisionId)`
           - Construct `EventDefinition` from decision fields
           - `EventDeliveryManager.Instance?.QueueEvent(eventDef)`
         - Records engagement:
           - `CampOpportunityGenerator.Instance?.RecordEngagement(...)`
     - Notifies camp generator:
       - `CampOpportunityGenerator.Instance?.OnPhaseChanged(newPhase)`

C) **Menu consumption / stable opportunity lists**
- UI should call `GetCurrentPhaseOpportunities()`:
  - Blocks during muster menus: `Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId`
  - Blocks in grace period: `enlistment.DaysServed < 3`
  - Ensures schedule exists: `ScheduleOpportunities()`
  - Returns non-consumed opportunities for current phase.
- When user takes an opportunity/decision:
  - `ConsumeOpportunity(opportunityId)` marks consumed across all phases + tomorrow window
  - `ConsumeOpportunityByDecisionId(decisionId)` fallback
  - `CommitToOpportunity(opportunityId)` marks `PlayerCommitted`, leading to later auto-fire at phase boundary.

D) **Crisis event injection**
- External systems (notably company simulation per comment) can call:
  - `QueueCrisisEvent(string eventId)`:
    - Loads: `EventCatalog.GetEvent(eventId)`
    - Queues: `EventDeliveryManager.Instance.QueueEvent(eventDef)`
    - Bypasses “frequency limits” by being queued directly.

E) **Override system**
- Entry:
  - `CheckForScheduleOverride(DayPhase phase)`
- Reads needs:
  - `var companyNeeds = EnlistmentBehavior.Instance?.CompanyNeeds;`
- Need-based overrides:
  - Uses JSON config loaded from `orchestrator_overrides.json` via `ModulePaths.GetConfigPath(...)`
  - Evaluates need thresholds via `GetNeedValue(CompanyNeedsState needs, string needName)`
- Variety injection:
  - `ShouldInjectVariety()` uses world situation + weekly/min-days constraints and random roll.
  - `SelectVarietyInjection(DayPhase phase)` chooses weighted config and returns `OrchestratorOverride`.

**Orchestrator Integration Points**
- **Enlistment**:
  - Activation gate: only runs when `EnlistmentBehavior.Instance.IsEnlisted`.
  - Reads enlistment attributes for scheduling modifiers:
    - `enlistment.IsOnProbation`
    - `enlistment.CompanyNeeds?.Supplies` (note: uses `CompanyNeedsState` property names in this file)
    - `enlistment.DaysServed` for grace period.
- **Camp system**:
  - Candidate generation: `CampOpportunityGenerator.Instance.GenerateCandidatesForPhase(phase)`
  - Engagement learning: `RecordEngagement(...)`
  - Medical opportunities: `QueueMedicalOpportunity(...)`
  - Phase refresh: `OnPhaseChanged(newPhase)`
- **Decision/Event systems**:
  - Converts `DecisionCatalog.GetDecision(...)` into `EventDefinition` and delivers via `EventDeliveryManager`.
  - Uses `EventCatalog.GetEvent(...)` for crisis and illness events.
- **Global pacing**:
  - `GlobalEventPacer.SetQuietDay(isQuiet)` is set by orchestrator.
  - `EventPacingManager` uses `ContentOrchestrator.Instance?.GetCurrentWorldSituation()` for fitness scoring during event selection.
- **Logistics simulation**:
  - Updates baggage event probabilities via `Logistics.BaggageTrainManager.Instance.CalculateEventProbabilities(worldSituation)`.

**Dependency graph (tool-verified)**
- `ContentOrchestrator` depends on `EnlistmentBehavior` and `EventPacingManager`
- `CampOpportunityGenerator` depends on `ContentOrchestrator`
  - (from `get_system_dependencies("ContentOrchestrator")`)


---

## Integration Points (Cross-System Map)

### Promotion ↔ Reputation/Escalation
- `PromotionBehavior.CanPromote()` requires escalation state for:
  - `SoldierReputation` threshold
  - `Discipline` max constraint  
  (both read via `EscalationManager.Instance?.State`)
- Promotion can be blocked if a promotion was previously declined:
  - `EscalationManager.Instance?.HasDeclinedPromotion(targetTier)`

### Promotion ↔ Enlistment/Tier
- Promotion reads:
  - `EnlistmentBehavior.Instance.EnlistmentTier`, `EnlistmentXP`, `DaysInRank`, `BattlesSurvived`, `EnlistedLord`
- Promotion writes:
  - Tier advancement via `enlistment.SetTier(targetTier)` (fallback path)
- Promotion resets itself on enlistment lifecycle:
  - `EnlistmentBehavior.OnEnlisted`/`OnDischarged` → clears `_pendingPromotionTier`.

### Orchestrator ↔ Content Delivery
- Orchestrator does not “auto fire narrative events” itself (per class header), but it:
  - Auto-fires **committed camp opportunities** by converting decisions into events and calling `EventDeliveryManager.QueueEvent(...)`.
  - Can queue crisis events directly (`QueueCrisisEvent`).
  - Can queue illness onset events directly (medical pressure system).
- Automatic paced narrative events are handled by:
  - `src/Features/Content/EventPacingManager.cs`, which consults orchestrator world situation for fitness scoring.

### Orchestrator ↔ Orders (indirect but explicit in code comments)
- `ContentOrchestrator.GetCurrentWorldSituation()` is explicitly intended for:
  - “OrderProgressionBehavior event weighting” (comment + method name)
- Verified reference exists in `EventPacingManager`:
  - `var worldSituation = ContentOrchestrator.Instance?.GetCurrentWorldSituation();`

### Identity ↔ Everything (light coupling)
- Identity (`EnlistedStatusManager`) is isolated:
  - Only reads `Hero.MainHero` traits/skills and returns role/description strings.
  - Logs errors via `ModLogger`.
  - No persistence, no event registrations.

---

## Event / Callback Mechanisms (System Triggers)

### Promotion
- `CampaignEvents.HourlyTickEvent` → promotion eligibility checks.
- Subscribed enlistment events:
  - `EnlistmentBehavior.OnEnlisted`, `OnDischarged` → resets pending promotion state.

### Orchestrator
- `CampaignEvents.DailyTickEvent` → world analysis, scheduling, forecasts, medical checks.
- `CampaignEvents.HourlyTickEvent` → phase changes → expiry + committed-opportunity firing.

### Reputation
- No dedicated tick behavior found in searches; reputation is accessed through:
  - `EnlistmentBehavior` (native relationship rep, restoration notifications)
  - `EscalationManager.State` (track-based rep used by other systems)

### Identity
- No events; query-only behavior.

---

## High-Level Data Flow Summary (End-to-End)

1. **EnlistmentBehavior** is the central “service context” hub (enlisted state, tier/XP, lord, needs, etc.).
2. **PromotionBehavior** (hourly tick):
   - Reads enlistment tier/XP/days/battles and escalation rep/discipline
   - Queues proving event via content system; or directly sets tier and triggers QM unlock refresh.
3. **Reputation** influences promotion via escalation state (and QM relationship via native relation).
4. **ContentOrchestrator** (daily + hourly):
   - Builds `WorldSituation` from `WorldStateAnalyzer`
   - Pre-schedules camp opportunities per phase (and tomorrow window)
   - On phase transitions, fires committed opportunities by converting decisions into queued events
   - Provides `WorldSituation` to other systems (event selection and order weighting)
   - Manages overrides and medical-pressure triggered content.
5. **Identity (EnlistedStatusManager)** provides player role/status based on traits/skills for UI and reporting.

All file paths and referenced components above are verified from the code excerpts retrieved via `search_codebase` and `Readsource`, and system-level dependency links are verified via `get_system_dependencies`.

---

## Gap Analysis

Total gaps identified: 0

---

## Recommendations

Total recommendations: 10

1. **Add promotion decision telemetry (log + debug overlay hook)**

2. **Guardrails for config integrity (tier XP + max tier validation)**

3. **Debounce/skip promotion checks when no state change is possible**

4. **Make “why can’t I promote?” UI helper explicit and reusable**

1. **Make proving event selection deterministic + fallback reason codes**

2. **Improve persistence robustness for pending promotions**

3. **Add lightweight automated checks (unit-style or integration harness) for tier rules**

1. **Consolidate “single source of truth” comments into enforceable code contracts**

2. **Add clear event subscription lifecycle notes and null-safety**

3. **Micro-optimization: compute target tier and requirement snapshot once per check**

---

## Compatibility Warnings

- ⚠️ Verify API compatibility with Bannerlord v1.3.13

---

## Next Steps

1. Review recommendations and prioritize
2. Use Warp Agent to create detailed implementation plans
3. Execute with `enlisted-crew implement -p <plan-path>`

---

*Generated by SystemAnalysisFlow v1.0 on 2026-01-10*