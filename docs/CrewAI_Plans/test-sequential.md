# test-sequential

Status: Planning  
Last updated: 2026-01-09  
Target game version: Enlisted (Bannerlord mod) RETAIL x64 (current branch)  
Related systems: ContentOrchestrator phase pipeline, OrderProgressionBehavior phase processing, EventDeliveryManager queueing, Decisions (optional), Escalation flags (optional)

## Overview

**Sequential process optimization from Phase 2.5 (practical meaning):**
This feature adds **developer-facing instrumentation** to observe and validate the *effective* ordering of work that happens at **campaign day/phase boundaries** (e.g., hour-based phase changes). “Sequential process optimization” here means:
- treating the boundary as a **logical pipeline** (stages executed in a consistent order),
- ensuring the pipeline is **deterministic** (same inputs → same stage sequence and enqueue order),
- ensuring the pipeline is **idempotent** (save/load or re-entry does not cause the same boundary to apply twice),
- making phase ordering **observable** via structured logs and a rolling trace buffer.

**Player-facing vs dev-only:**
- Core feature is **dev-only instrumentation** gated by a runtime flag (`TestSequentialSettings.Enabled`).
- Optionally, a **camp Decision** (`dec_test_sequential`) can be added to toggle tracing on/off (and strict mode) within a save, without editing config.

**Goals:**
1. **Determinism:** same seed + same day/phase yields the same ordering of subsystem boundary markers and event enqueue order.
2. **Idempotency:** boundary key `(DayOfYear, HourOfDay, DayPhase)` is not processed twice; save/load mid-boundary should not duplicate.
3. **Phase ordering observability:** record and log strict stage ordering and event enqueue producers.

Non-goals:
- No behavioral changes to gameplay logic by default.
- No invasive refactor to introduce a unified coordinator (future work).

## Technical Specification

### Architecture

**Concept: Phase Boundary Trace**
- Implement a **probe** (`SequentialProbe`) invoked from existing chokepoints.
- Probe records an ordered list of **markers** representing stage begin/end/close.
- Probe records **queued events** during the boundary window with a **producer tag**.
- Probe checks for:
  - out-of-order producer beginnings (Orders vs Orchestrator),
  - duplicate processing of same boundary key,
  - nested (re-entrant) boundary begins,
  - FIFO event enqueue ordering by producer.

**Sequential pipeline stages observed (not enforced):**
- Orders phase processing: slot roll → select event → queue event
- ContentOrchestrator boundary pipeline: cleanup → fire committed → notify camp
- EventDeliveryManager queueing: FIFO queue operations (and any immediate delivery risks)

**Phase clock assumptions:**
- Both Orders and Orchestrator may be triggered by hour/phase transitions via `CampaignEvents.HourlyTickEvent`.
- `ContentOrchestrator` tracks last phase internally (`_lastPhase`) and is treated as the **boundary owner** that closes the trace record.
- Boundary identity uses `CampaignTime.Now` and captures day/hour info; the probe treats `(DayOfYear, HourOfDay, DayPhase)` as the idempotency key.

### Exact integration points (methods touched)

Instrument at existing chokepoints (no refactor):

1. `src/Features/Content/ContentOrchestrator.cs`
   - Method: `private void OnDayPhaseChanged(DayPhase newPhase)`
   - Add:
     - `SequentialProbe.BeginBoundary("Orchestrator", newPhase);` at the start
     - stage markers around:
       - `CleanupMissedOpportunities(previousPhase)`
       - `FireCommittedOpportunities(newPhase)`
       - `Camp.CampOpportunityGenerator.Instance?.OnPhaseChanged(newPhase)`
     - `SequentialProbe.CloseBoundaryIfOwner("Orchestrator");` at the end
   - Additionally update the enqueue call in committed-opportunity firing:
     - In `FireCommittedOpportunities(DayPhase phase)`: replace `QueueEvent(eventDef)` with overload `QueueEvent(eventDef, "Orchestrator.Decision")`

   **Spec-level patch (implementation-ready):**
   ```csharp
   using Enlisted.Features.Content.Sequential;

   private void OnDayPhaseChanged(DayPhase newPhase)
   {
       SequentialProbe.BeginBoundary("Orchestrator", newPhase);

       ModLogger.Info(LogCategory, $"Day phase changed to {newPhase}");

       var previousPhase = GetPreviousPhase(newPhase);

       SequentialProbe.BeginBoundary("Orchestrator.Cleanup", newPhase);
       CleanupMissedOpportunities(previousPhase);
       SequentialProbe.EndBoundary("Orchestrator.Cleanup");

       SequentialProbe.BeginBoundary("Orchestrator.FireCommitted", newPhase);
       FireCommittedOpportunities(newPhase);
       SequentialProbe.EndBoundary("Orchestrator.FireCommitted");

       SequentialProbe.BeginBoundary("Camp.NotifyPhase", newPhase);
       Camp.CampOpportunityGenerator.Instance?.OnPhaseChanged(newPhase);
       SequentialProbe.EndBoundary("Camp.NotifyPhase");

       SequentialProbe.CloseBoundaryIfOwner("Orchestrator");
   }
   ```

2. `src/Features/Orders/Behaviors/OrderProgressionBehavior.cs`
   - Method: `private void ProcessPhase(int hour)`
     - Map to `DayPhase` via existing helper (per spec: `GetDayPhaseFromHour(hour)`)
     - Add:
       - `SequentialProbe.BeginBoundary("Orders", phase);` at start
       - `SequentialProbe.EndBoundary("Orders");` at end
   - Method: `FireOrderEvent(EventDefinition eventDef)` (exact name per repository)
     - Replace `deliveryManager.QueueEvent(eventDef);` with `deliveryManager.QueueEvent(eventDef, "Orders.Slot");`

   **Spec-level patch (implementation-ready):**
   ```csharp
   using Enlisted.Features.Content.Sequential;

   private void ProcessPhase(int hour)
   {
       var phase = GetDayPhaseFromHour(hour);
       SequentialProbe.BeginBoundary("Orders", phase);

       // existing logic...

       SequentialProbe.EndBoundary("Orders");
   }
   ```

3. `src/Features/Content/EventDeliveryManager.cs`
   - Method: `public void QueueEvent(EventDefinition evt)` (existing)
   - Add non-breaking overload:

   ```csharp
   public void QueueEvent(EventDefinition evt, string producer)
   {
       SequentialProbe.RecordQueuedEvent(producer ?? "Unknown", evt?.Id ?? "null");
       QueueEvent(evt);
   }
   ```

   - Update call sites in:
     - `src/Features/Content/ContentOrchestrator.cs` (committed opportunities)
     - `src/Features/Orders/Behaviors/OrderProgressionBehavior.cs` (order events)

### Data structures and persistence strategy

**Data structures (new):**
- `SequentialPhaseKey`: stable identity for a boundary snapshot (`DayOfYear`, `HourOfDay`, `DayNumber`)
- `SequentialTraceRecord`: one boundary record
  - `Key`, `Phase`
  - ordered `Markers: List<string>`
  - `QueuedEvents: List<(string Producer, string EventId)>`
  - timing (`UtcStarted`, `UtcEnded`, `DurationMs`)
  - violation tracking (`HasViolation`, `ViolationSummary`)

**Rolling buffer:**
- Maintain in-memory `_history: List<SequentialTraceRecord>` capped to `TestSequentialSettings.MaxRecords`.

**Idempotency tracking:**
- `_processedKeys: HashSet<string>` where key format: `"{DayOfYear}:{HourOfDay}:{phase}"`.

**Persistence (save/load safety):**
- Default: telemetry is **transient** (in-memory only) to avoid save compatibility risk.
- Optional future extension (not required for MVP): serialize last N records + processed key set.
  - If implemented, keep size small and robust to versioning.

### Config toggles and logging outputs

**Settings (new static settings class):**
- `TestSequentialSettings.Enabled` (default `false`)
- `TestSequentialSettings.Strict` (default `false`)
- `TestSequentialSettings.MaxRecords` (default `64`)

**Logging category:**
- `TestSequential`

**Performance logging:**
- Each trace logs total `DurationMs`.
- (Optional enhancement) stage-level durations can be inferred by marker timestamps, but current spec logs only boundary duration.

**Strict mode behavior:**
- When `Strict = true`, violations log as **Error** (no throwing by default).
- When `Strict = false`, violations log as **Warn**.

### Optional content: Decision trigger (toggle)

**New content ID:** `dec_test_sequential` (verified unique; evidence below).

**Behavior:**
- Decision sets/clears escalation flags:
  - `flag_test_sequential_on`
  - `flag_test_sequential_strict`
- A bridge campaign behavior reads flags hourly and updates `TestSequentialSettings`:
  - `TestSequentialFlagBridgeBehavior : CampaignBehaviorBase`
  - Listens to `CampaignEvents.HourlyTickEvent`.

**Decision JSON template:**
- File: `ModuleData/Enlisted/Decisions/decisions.json` (or repository’s canonical decisions file)
- Adds `dec_test_sequential` with options enable/disable/enable_strict.

**Localization:**
- File: `ModuleData/Languages/enlisted_strings.xml`
- Add keys listed under Content IDs.

## Files to Create/Modify

### Create (new files)
1. `src/Features/Content/Sequential/TestSequentialSettings.cs`
   - Must add to `Enlisted.csproj` compile includes.
2. `src/Features/Content/Sequential/SequentialPhaseKey.cs`
   - Must add to `Enlisted.csproj` compile includes.
3. `src/Features/Content/Sequential/SequentialTraceRecord.cs`
   - Must add to `Enlisted.csproj` compile includes.
4. `src/Features/Content/Sequential/SequentialProbe.cs`
   - Must add to `Enlisted.csproj` compile includes.
5. `src/Features/Content/Sequential/TestSequentialFlagBridgeBehavior.cs` *(optional; only if Decision toggle is implemented)*
   - Must add to `Enlisted.csproj` compile includes.

### Modify (existing files)
1. `src/Features/Content/ContentOrchestrator.cs`
   - Instrument `OnDayPhaseChanged(DayPhase newPhase)` with probe markers and close.
   - Update event enqueue call(s) in `FireCommittedOpportunities` to use producer overload.
2. `src/Features/Orders/Behaviors/OrderProgressionBehavior.cs`
   - Instrument `ProcessPhase(int hour)` begin/end.
   - Update order event enqueue call(s) to use producer overload.
3. `src/Features/Content/EventDeliveryManager.cs`
   - Add `QueueEvent(EventDefinition evt, string producer)` overload.
4. `Enlisted.csproj`
   - Add `<Compile Include="..."/>` entries for all new `.cs` files (old-style csproj constraint).

### Optional content files (only if Decision toggle is added)
1. `ModuleData/Enlisted/Decisions/decisions.json`
   - Add decision object for `dec_test_sequential`.
2. `ModuleData/Languages/enlisted_strings.xml`
   - Add localization keys for decision text.

## Content IDs

### New IDs
- Decision:
  - `dec_test_sequential`

### Localization keys (new)
- `dec_test_sequential_title`
- `dec_test_sequential_setup`
- `dec_test_sequential_enable`
- `dec_test_sequential_disable`
- `dec_test_sequential_strict`
- `dec_test_sequential_enabled_result`
- `dec_test_sequential_disabled_result`
- `dec_test_sequential_strict_result`

### Uniqueness evidence
- `dec_test_sequential` — lookup_content_id result: **Not found** (per Technical Specification section “Database-first Content Planning (Verified)”).

## Implementation Checklist

### Phase 1 — Wire hooks (orchestrator events/callouts)
- [ ] Add `using Enlisted.Features.Content.Sequential;` to `ContentOrchestrator.cs`.
- [ ] In `ContentOrchestrator.OnDayPhaseChanged(DayPhase newPhase)`, add:
  - [ ] `SequentialProbe.BeginBoundary("Orchestrator", newPhase);` at entry.
  - [ ] Stage markers around cleanup/fire/notify as per spec.
  - [ ] `SequentialProbe.CloseBoundaryIfOwner("Orchestrator");` at exit.
- [ ] Add `using Enlisted.Features.Content.Sequential;` to `OrderProgressionBehavior.cs`.
- [ ] In `OrderProgressionBehavior.ProcessPhase(int hour)`, add:
  - [ ] `phase = GetDayPhaseFromHour(hour)` (existing helper).
  - [ ] `SequentialProbe.BeginBoundary("Orders", phase);` at entry.
  - [ ] `SequentialProbe.EndBoundary("Orders");` at exit.

### Phase 2 — Implement pipeline + record structures
- [ ] Create `TestSequentialSettings.cs` with fields:
  - [ ] `Enabled` default false
  - [ ] `Strict` default false
  - [ ] `MaxRecords` default 64
- [ ] Create `SequentialPhaseKey.cs` as readonly struct based on `CampaignTime`.
- [ ] Create `SequentialTraceRecord.cs` with:
  - [ ] key + phase
  - [ ] markers list
  - [ ] queued events list
  - [ ] UTC start/end + duration
  - [ ] violation flags
- [ ] Create `SequentialProbe.cs` implementing:
  - [ ] rolling `_history` buffer
  - [ ] `_current` record
  - [ ] `_processedKeys` for idempotency detection
  - [ ] `BeginBoundary`, `EndBoundary`, `CloseBoundaryIfOwner`, `RecordQueuedEvent`
  - [ ] ordering assertions (Orders begin before Orchestrator begin when both present)
  - [ ] violation reporting with strict vs warn behavior

### Phase 3 — Optional JSON decision/event trigger
- [ ] Add decision `dec_test_sequential` to `ModuleData/Enlisted/Decisions/decisions.json` using spec template.
- [ ] Add localization entries to `ModuleData/Languages/enlisted_strings.xml`.
- [ ] Create `TestSequentialFlagBridgeBehavior.cs` reading escalation flags hourly.
- [ ] Register `TestSequentialFlagBridgeBehavior` in the campaign behavior registration location (to be discovered during implementation; likely SubModule or a central registrar).

### Phase 4 — Persistence + save/load safety
- [ ] Confirm MVP telemetry is transient-only (no IDataStore usage in probe).
- [ ] Ensure idempotency check uses boundary key set `_processedKeys` and logs duplicate processing.
- [ ] (If Decision toggle implemented) ensure enabling/disabling via flags survives save/load (Escalation state persistence).

### Phase 5 — Logging + metrics
- [ ] Ensure all probe logs use category `TestSequential`.
- [ ] Ensure each closed trace logs:
  - [ ] boundary key (day/hour)
  - [ ] phase
  - [ ] duration
  - [ ] ordered markers
  - [ ] queued events with producer tags
  - [ ] violation summary when present
- [ ] Verify overhead when disabled is near zero:
  - [ ] early-return `if (!Enabled) return;` in probe methods.

### Phase 6 — csproj update + build
- [ ] Add `<Compile Include="src\Features\Content\Sequential\TestSequentialSettings.cs" />` to `Enlisted.csproj`.
- [ ] Add `<Compile Include="src\Features\Content\Sequential\SequentialPhaseKey.cs" />`.
- [ ] Add `<Compile Include="src\Features\Content\Sequential\SequentialTraceRecord.cs" />`.
- [ ] Add `<Compile Include="src\Features\Content\Sequential\SequentialProbe.cs" />`.
- [ ] Add `<Compile Include="src\Features\Content\Sequential\TestSequentialFlagBridgeBehavior.cs" />` (if created).
- [ ] Build **Enlisted RETAIL x64**.

### Phase 7 — Validation script
- [ ] If any JSON/XML content added or modified, run `Tools/Validation/validate_content.py` and fix reported issues.

### Explicit “done when” checkboxes
- [ ] Compiles in **Enlisted RETAIL x64**.
- [ ] No duplicate execution across save/load for the same `(DayOfYear, HourOfDay, DayPhase)` boundary.
- [ ] Logs show strict stage ordering within each phase boundary (markers in expected sequence when both subsystems run).

## Validation Criteria

### 1) Deterministic ordering test
**Setup:**
- Enable `TestSequentialSettings.Enabled = true` (or via decision `dec_test_sequential`).
- Ensure both Orders processing and at least one committed opportunity can fire on the same boundary.
- Run the same scenario twice with the same seed/save state.

**Expected:**
- For a given boundary key and phase, the trace marker sequence is identical.
- The `Queued=[...]` producer/eventId list order matches across runs.

**Acceptance outputs:**
- Log lines in category `TestSequential` containing:
  - `Trace Day#<n> (DoY=<d>) Hour=<h> Phase=<phase> ... Markers=[...] Queued=[...] OK`
- Expected number of records:
  - **Exactly 1** trace record per observed boundary where orchestrator closes (`CloseBoundaryIfOwner`).

### 2) Idempotency test (save/load mid-phase)
**Setup:**
- Enable tracing.
- Save shortly before a phase boundary; progress into the boundary; save again; reload.
- Repeat reload around the same hour/phase.

**Expected:**
- No warnings/errors about duplicate boundary keys.
- Specifically, probe should **not** report:
  - `Duplicate boundary processing detected: <key>`

**Acceptance outputs:**
- Absence of `Duplicate boundary processing detected` in logs.
- If the underlying engine triggers duplicates, they must be detected and logged; that still satisfies instrumentation but fails idempotency goal.

### 3) Re-entrancy / nested boundary test
**Setup:**
- Enable tracing.
- Trigger situations where queueing events might cause immediate delivery or cascading calls while still inside a boundary.

**Expected:**
- No `Nested boundary begin` violations.

**Acceptance outputs:**
- Absence of `Nested boundary begin` warn/error lines.

### 4) Performance / optimization measurement test
**Setup:**
- Enable tracing for at least 20 boundaries (or a full in-game day).

**Expected:**
- Each trace logs `Duration=<ms>ms`.
- Overhead when enabled is acceptable; when disabled, overhead is near zero.

**Acceptance outputs:**
- Presence of duration in every closed trace log line.
- (Optional target) Median duration does not regress vs baseline by more than an agreed threshold.

### 5) Tier safety test (T3/T5 scenarios)
**Purpose:** Ensure instrumentation does not accidentally alter event selection/assignment behavior.

**Setup:**
- Run scenarios that exercise tier-sensitive logic (T3 and T5).
- Compare outcomes with tracing disabled vs enabled.

**Expected:**
- No tier-inappropriate assignments introduced by this feature (should be purely observational).

**Acceptance outputs:**
- Identical gameplay outcomes (or within expected RNG variance) with tracing disabled vs enabled.
- No new warnings/errors in `TestSequential` logs indicating ordering violations due to instrumentation alone.
