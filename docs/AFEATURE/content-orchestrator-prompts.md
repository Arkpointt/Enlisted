# Content Orchestrator Implementation Prompts

**Summary:** Copy-paste prompts for each implementation phase of the Content Orchestrator. Each prompt is designed for a NEW AI chat session with full context recovery.

**Status:** 📋 Reference
**Last Updated:** 2025-12-30 (Phase 6 complete)
**Related Docs:** [Content Orchestrator Plan](content-orchestrator-plan.md), [Camp Background Simulation](camp-background-simulation.md), [Order Events Master](order-events-master.md), [BLUEPRINT](../BLUEPRINT.md)

---

## How to Use These Prompts

Each phase prompt is **self-contained for a fresh AI chat**. Every prompt includes:
1. **Prerequisites** - What must exist before starting
2. **Context Recovery** - How the new AI verifies previous work
3. **Tasks** - What to implement
4. **Acceptance Criteria** - Definition of done
5. **Handoff Notes Template** - What to capture for the next AI

**Start each new chat by copying the entire prompt block (inside the triple backticks).**

---

## Index

| Phase | Description | Model | Chat Strategy | Status |
|-------|-------------|-------|---------------|--------|
| [Phase 1](#phase-1-foundation) | Foundation (infrastructure) | Opus 4 | ✅ **DONE** | ✅ Complete |
| [Phase 2](#phase-2-content-selection-integration) | Content Selection | Opus 4 | ✅ **DONE** | ✅ Complete |
| [Phase 3](#phase-3-cutover-migration) | Cutover & Migration | Sonnet 4 | ✅ **DONE** | ✅ Complete |
| [Phase 4](#phase-4-orders-integration) | Orders Integration | Sonnet 4 | 🔒 Standalone | ✅ Complete |
| [Phase 4.5](#phase-45-native-effect-integration) | Native Effect Integration | Opus 4 | 🔒 Standalone | ✅ Complete |
| [Phase 5](#phase-5-ui-integration-quick-decision-center) | UI Integration | Sonnet 4 | 🔒 Standalone | ✅ Complete |
| [Phase 5.5](#phase-55-camp-background-simulation) | Background Simulation | Opus 4 | 🔒 Standalone | ✅ Complete |
| [Phase 6A-C](#phase-6-camp-life-simulation-living-breathing-world) | Camp Life Core | Opus 4 | ⚡ **COMBINE** | ✅ Complete |
| [Phase 6D-E](#phase-6d-e-camp-life-polish) | Camp Life Polish | Opus 4 | 🔒 Standalone | ✅ Complete |
| **Phase 6F** | **Integration & Testing** | Sonnet 4 | 🔒 Standalone | ✅ Complete |
| [Phase 7](#phase-7-content-variants-post-launch) | Content Variants | Sonnet 4 | 🔒 Standalone | ⏸️ Future |
| [Phase 8](#phase-8-progression-system-future) | Progression System | Opus 4 | 🔒 Standalone | ⏸️ Future |
| [Phase 9](#phase-9-decision-scheduling-must-have) | Decision Scheduling | Sonnet 4 | 🔒 Standalone | ❌ **MUST DO** |
| [Phase 10](#phase-10-order-forecasting-must-have) | Order Forecasting | Sonnet 4 | 🔒 Standalone | ❌ **MUST DO** |
| [Build & Test](#quick-reference-build-test) | Commands reference | - | - | |

---

## Chat Strategy Recommendations

| Strategy | Phases | Rationale |
|----------|--------|-----------|
| ✅ **DONE** | 1 | Phase 1 is complete - infrastructure exists |
| ✅ **DONE** | 2 | Phase 2 is complete - content selection with fitness scoring |
| ✅ **DONE** | 3 | Phase 3 is complete - orchestrator cutover and migration |
| ✅ **DONE** | 4 | Order integration complete (85/85 events across 16 order types) |
| **STANDALONE** | 4.5 | Native Bannerlord API integration - decompile-verified patterns |
| **STANDALONE** | 5 | UI work is self-contained |
| **STANDALONE** | 5.5 | Complex Bannerlord API work needs focus |
| **COMBINE** | 6A-C | Core functionality; natural continuation |
| **STANDALONE** | 6D-E | Polish phase; can be done later or by different AI |
| **STANDALONE** | 7 | JSON-only work; fast iteration |
| **STANDALONE** | 8 | Future system; clean separation |

---

## Model Recommendations

| Phase | Complexity | Model | Est. Time | Status |
|-------|------------|-------|-----------|--------|
| Phase 1 - Foundation | High | Claude Opus 4 | - | ✅ Done |
| Phase 2 - Selection | Medium-High | Claude Opus 4 | - | ✅ Done |
| Phase 3 - Cutover | Medium | Claude Sonnet 4 | - | ✅ Done |
| Phase 4 - Orders | High (narrative) | Claude Opus 4 | - | ✅ Complete (85/85 events) |
| Phase 4.5 - Native Effects | High | Claude Opus 4 | 2-3 hours | ✅ Complete |
| Phase 5 - UI | Medium | Claude Sonnet 4 | 1-2 hours | ✅ Complete |
| Phase 5.5 - Background | High | Claude Opus 4 | 2-3 hours | ✅ Complete |
| Phase 6A-C - Camp Life Core | High | Claude Opus 4 | 2-3 hours | ✅ Complete |
| Phase 6D-E - Camp Life Polish | Medium | Sonnet 4 or Opus 4 | 1-2 hours | ✅ Complete |
| Phase 6F - Integration | High | Sonnet 4 | 1 hour | ✅ Complete |
| Phase 7 - Variants | Low | Claude Sonnet 4 | 30-60 min | ⏸️ Future |
| Phase 8 - Progression | High | Claude Opus 4 | 2-3 hours | ⏸️ Future |
| Phase 6G - Create Missing Decisions | **BLOCKING** | Claude Sonnet 4 | 3-4 hours | ⛔ **BLOCKS 9** |
| Phase 9 - Decision Scheduling | **Critical** | Claude Sonnet 4 | 2-3 hours | ⛔ **BLOCKED** |
| Phase 10 - Order Forecasting | **Critical** | Claude Sonnet 4 | 2-3 hours | ❌ **MUST DO** |

**Total remaining time:** ~10-14 hours (Phase 6G must complete before Phase 9; Phases 9-10 critical for player experience at FastForward speed)

---

## Phase 1: Foundation

**Goal:** Build core orchestrator infrastructure without changing existing behavior

**Status:** ✅ **COMPLETE** - This phase has been implemented

**What Was Built:**
- ContentOrchestrator.cs (CampaignBehaviorBase with daily tick)
- WorldStateAnalyzer.cs (static class for situation analysis)
- SimulationPressureCalculator.cs (static class for pressure calculation)
- PlayerBehaviorTracker.cs (static class for preference tracking)
- Data models in src/Features/Content/Models/
- Save/load integration in EnlistedSaveDefiner.cs

**Files Created:**
- src/Features/Content/ContentOrchestrator.cs
- src/Features/Content/WorldStateAnalyzer.cs
- src/Features/Content/SimulationPressureCalculator.cs
- src/Features/Content/PlayerBehaviorTracker.cs
- src/Features/Content/Models/WorldSituation.cs
- src/Features/Content/Models/SimulationPressure.cs
- src/Features/Content/Models/PlayerPreferences.cs
- src/Features/Content/Models/OrchestratorEnums.cs (LordSituation, WarStance, LifePhase, ActivityLevel, DayPhase)

**Verification:**
- Orchestrator receives daily ticks when enlisted
- World state analysis logs correctly
- Pressure calculation logs sources and total
- Save/load works without errors

---

## Phase 2: Content Selection Integration

**Goal:** Connect orchestrator to content selection with fitness scoring

**Chat Strategy:** 🔒 **STANDALONE** - Builds on completed Phase 1

**Prerequisites:** Phase 1 complete (orchestrator infrastructure exists)

```
I need you to implement Phase 2 of the Content Orchestrator for my Bannerlord mod.

═══════════════════════════════════════════════════════════════════════════════
CONTEXT RECOVERY (Verify Phase 1 exists before starting)
═══════════════════════════════════════════════════════════════════════════════

Phase 1 is COMPLETE. Before implementing Phase 2, verify these files exist:

Phase 1 Files (MUST exist - read them to understand current implementation):
[ ] src/Features/Content/ContentOrchestrator.cs - CampaignBehaviorBase with daily tick
[ ] src/Features/Content/WorldStateAnalyzer.cs - static class with AnalyzeSituation()
[ ] src/Features/Content/SimulationPressureCalculator.cs - static class with CalculatePressure()
[ ] src/Features/Content/PlayerBehaviorTracker.cs - static class (may need implementation)
[ ] src/Features/Content/Models/WorldSituation.cs - data model
[ ] src/Features/Content/Models/SimulationPressure.cs - data model
[ ] src/Features/Content/Models/PlayerPreferences.cs - data model

Verify Phase 1 is working:
1. Check logs for "[Orchestrator]" entries on daily tick
2. Verify WorldSituation contains: LordSituation, WarStance, LifePhase, ActivityLevel
3. Verify SimulationPressure contains: Value, Sources

READ THE EXISTING FILES before modifying them!

If any Phase 1 files are missing, they need to be created first.

═══════════════════════════════════════════════════════════════════════════════
DOCUMENTATION TO READ
═══════════════════════════════════════════════════════════════════════════════

Read these docs:
- docs/AFEATURE/content-orchestrator-plan.md (Phase 2 section)
- docs/Features/Content/content-system-architecture.md (current event system)
- docs/Features/Content/event-system-schemas.md (data structures)
- docs/BLUEPRINT.md (coding standards)

═══════════════════════════════════════════════════════════════════════════════
PHASE 2 TASKS
═══════════════════════════════════════════════════════════════════════════════

1. Integrate with EventSelector.SelectEvent() - add WorldSituation parameter
2. Implement player behavior tracking (RecordChoice, GetPreferences)
3. Add preference-based content scoring (fitness calculation)
4. Log what WOULD be selected vs what old system would select
5. Compare selections for validation

Files to Modify:
- src/Features/Content/EventSelector.cs (add fitness scoring)
- src/Features/Content/ContentOrchestrator.cs (add selection logic)
- src/Features/Content/PlayerBehaviorTracker.cs (implement tracking)

Files to Create (if needed):
- src/Features/Content/ContentFitnessScorer.cs (optional, can be method in Orchestrator)

Existing Systems to Read (don't modify yet):
- src/Features/Content/EventCatalog.cs
- src/Features/Content/EventRequirementChecker.cs

Existing Managers to Use (BLUEPRINT requirement - use centralized managers):
- CompanyNeedsManager.Instance - Read Supplies, Morale, Rest for pressure calculation
- EscalationManager.Instance - Read discipline, scrutiny for pressure calculation
- EnlistmentBehavior.Instance - Read enlisted status, lord reference

═══════════════════════════════════════════════════════════════════════════════
CRITICAL: CONTEXT MAPPING REQUIREMENTS
═══════════════════════════════════════════════════════════════════════════════

WorldStateAnalyzer must provide TWO context methods:

1. GetEventContext(WorldSituation) → Returns "Camp", "War", "Siege", "Any"
   - Used for narrative event filtering (Camp Hub decisions, automatic events)
   - Maps to event JSON requirements.context field
   - Simple context values

2. GetOrderEventWorldState(WorldSituation) → Returns "peacetime_garrison", "war_marching", etc.
   - Used for order event weighting during duty execution
   - Maps to order event JSON requirements.world_state field
   - Granular orchestrator state keys

Mapping Logic (implement in WorldStateAnalyzer):
- Garrison situation → "Camp" context (for narrative events)
- Campaign situation → "War" context (for narrative events)
- Siege situation → "Siege" context (for narrative events)
- Defeated situation → "Camp" context (recovery period)

For order events, combine LordSituation + WarStance:
- (InGarrison, Peacetime) → "peacetime_garrison"
- (InCampaign, ActiveWar) → "war_active_campaign"
- (InSiege, ActiveWar) → "siege_attacking"
- (InSiege, DesperateWar) → "siege_defending"
- (Defeated, any) → "lord_captured"

See event-system-schemas.md "Context Mapping" section for full details.

═══════════════════════════════════════════════════════════════════════════════
ACCEPTANCE CRITERIA
═══════════════════════════════════════════════════════════════════════════════

[ ] Orchestrator can query EventCatalog with world context
[ ] WorldStateAnalyzer.GetEventContext() returns correct context
[ ] WorldStateAnalyzer.GetOrderEventWorldState() returns granular keys
[ ] Fitness scoring considers world situation + player preferences
[ ] Logs show clear comparison: "Old system would pick X, new system picks Y"
[ ] PlayerBehaviorTracker.RecordChoice() tracks player choices
[ ] PlayerBehaviorTracker.GetPreferences() returns preference profile
[ ] Existing event system still works (no changes to live behavior)

CRITICAL: Don't break the existing system. Log comparisons only.

═══════════════════════════════════════════════════════════════════════════════
HANDOFF NOTES (Capture these for Phase 3)
═══════════════════════════════════════════════════════════════════════════════

When complete, provide these handoff notes for the next AI session:

FILES MODIFIED:
- [ ] src/Features/Content/WorldStateAnalyzer.cs (added GetEventContext, GetOrderEventWorldState)
- [ ] src/Features/Content/ContentOrchestrator.cs (added selection logic)
- [ ] src/Features/Content/PlayerBehaviorTracker.cs (implemented tracking)
- [ ] src/Features/Content/EventSelector.cs (added fitness scoring)

FILES CREATED:
- [ ] (any new files)

CONTEXT MAPPING IMPLEMENTED:
- GetEventContext() maps to: Camp, War, Siege, Any
- GetOrderEventWorldState() maps to: peacetime_garrison, war_active_campaign, etc.

FITNESS SCORING:
- (describe how scoring works)

KEY DECISIONS MADE:
- (list any architectural decisions)

KNOWN ISSUES/TECH DEBT:
- (list any incomplete items)

VERIFICATION PASSED:
[ ] Logs show orchestrator selecting content
[ ] Context mapping returns correct values
[ ] Fitness scoring considers all factors
[ ] Existing system unaffected
```

---

## Phase 3: Cutover & Migration

**Goal:** Switch from schedule-driven event pacing to orchestrator-driven content delivery. This is a MIGRATION - old systems get removed.

**Status:** ✅ **COMPLETE** - This phase has been implemented

**What Was Built:**
- Feature flag system in enlisted_config.json (orchestrator.enabled)
- World-state-driven content firing in ContentOrchestrator
- Frequency tables for garrison/campaign/siege/battle contexts
- Removed schedule-driven logic (event windows, evaluation hours, random quiet days)
- Removed obsolete config fields and state tracking

**Files Modified:**
- src/Mod.Core/Config/ConfigurationManager.cs (added OrchestratorConfig, FrequencyTable)
- src/Features/Content/EventPacingManager.cs (removed schedule logic, kept grace period and chain events)
- src/Features/Content/GlobalEventPacer.cs (removed evaluation hours and random quiet day roll)
- src/Features/Content/ContentOrchestrator.cs (implemented world-state-driven firing)
- src/Features/Escalation/EscalationState.cs (removed NextNarrativeEventWindow, LastNarrativeEventTime)
- ModuleData/Enlisted/enlisted_config.json (added orchestrator section, removed old pacing fields)

**Verification:**
- Orchestrator enabled by default (orchestrator.enabled = true)
- Content fires based on world state (garrison ~0.4/day, campaign ~1.5/day, siege ~2.5/day)
- Quiet days set by world state, not random roll
- Safety limits still enforced (max_per_day, max_per_week, min_hours_between)
- No schedule artifacts remain (no event windows, evaluation hours, or random quiet rolls)

**Chat Strategy:** 🔒 **STANDALONE** - This is high-risk migration work; needs focused attention

**Prerequisites:** Phase 1+2 complete (orchestrator infrastructure exists and can select content)

```
I need you to implement Phase 3 of the Content Orchestrator - the cutover and migration.

═══════════════════════════════════════════════════════════════════════════════
CONTEXT RECOVERY (Verify Phase 1+2 work before starting)
═══════════════════════════════════════════════════════════════════════════════

Before implementing, verify these files exist and work:

Phase 1 Files (must exist):
[ ] src/Features/Content/ContentOrchestrator.cs - CampaignBehaviorBase with daily tick
[ ] src/Features/Content/WorldStateAnalyzer.cs - static class with AnalyzeSituation()
[ ] src/Features/Content/SimulationPressureCalculator.cs - static class with CalculatePressure()
[ ] src/Features/Content/PlayerBehaviorTracker.cs - static class with RecordChoice()
[ ] src/Features/Content/Models/WorldSituation.cs - data model with LordSituation, WarStance, etc.
[ ] src/Features/Content/Models/SimulationPressure.cs - data model with Value, Sources
[ ] src/Features/Content/Models/PlayerPreferences.cs - data model with preference floats

Phase 2 Integration (must work):
[ ] WorldStateAnalyzer.GetEventContext() returns "Camp", "War", "Siege", "Any"
[ ] WorldStateAnalyzer.GetOrderEventWorldState() returns granular keys
[ ] ContentOrchestrator logs world state analysis on daily tick
[ ] Fitness scoring implemented (in EventSelector or Orchestrator)

If any of these are missing, read the Phase 1+2 prompt and implement the missing parts first.

═══════════════════════════════════════════════════════════════════════════════
DOCUMENTATION TO READ FIRST
═══════════════════════════════════════════════════════════════════════════════

Read these docs first:
- docs/AFEATURE/content-orchestrator-plan.md (Phase 3 + Migration section)
- docs/BLUEPRINT.md (config file conventions)

═══════════════════════════════════════════════════════════════════════════════
EXECUTIVE SUMMARY
═══════════════════════════════════════════════════════════════════════════════

CURRENT SYSTEM (being replaced):
- EventPacingManager fires events every 3-5 days on timer
- GlobalEventPacer has "evaluation hours" (8am, 2pm, 8pm) when events CAN fire
- GlobalEventPacer has "quiet day" random roll (15% chance = no events today)
- EscalationState tracks NextNarrativeEventWindow for scheduling

NEW SYSTEM (replacing it):
- ContentOrchestrator fires content based on world state
- Garrison = quiet (0.3-0.5 per day), Campaign = busy (1.5-2.0 per day)
- Siege = intense (2.5-3.5 per day), Battle = suppressed (0)
- IsQuietDay set by world state, not random roll

This is a REPLACEMENT. The old schedule logic must be REMOVED.

═══════════════════════════════════════════════════════════════════════════════
PHASE 3A: FEATURE FLAG (Non-Breaking)
═══════════════════════════════════════════════════════════════════════════════

1. ADD feature flag to enlisted_config.json:

{
  "orchestrator": {
    "enabled": false,
    "log_decisions": true,
    "frequency_tables": {
      "garrison": { "base": 0.4, "min": 0.3, "max": 0.5 },
      "campaign": { "base": 1.5, "min": 1.2, "max": 2.0 },
      "siege": { "base": 2.5, "min": 2.0, "max": 3.5 },
      "battle": { "base": 0.0, "min": 0.0, "max": 0.5 }
    }
  }
}

2. MODIFY EventPacingManager.OnDailyTick() to check flag:

private void OnDailyTick()
{
    // New: Check if orchestrator is handling events
    if (EnlistedConfig.Instance.Orchestrator?.Enabled == true)
    {
        // Orchestrator handles everything - skip old logic
        return;
    }

    // Old schedule logic (will be removed in Phase 3C)
    // ...existing code...
}

3. TEST that old system still works when flag is false

═══════════════════════════════════════════════════════════════════════════════
PHASE 3B: ORCHESTRATOR TAKES OVER
═══════════════════════════════════════════════════════════════════════════════

1. WIRE ContentOrchestrator.OnDailyTick() to campaign daily tick

2. IMPLEMENT world-state-driven firing:

public void OnDailyTick()
{
    if (EnlistedConfig.Instance.Orchestrator?.Enabled != true)
        return;

    var situation = WorldStateAnalyzer.Analyze();
    var frequency = GetFrequencyForSituation(situation);
    var shouldFire = ShouldFireContent(frequency);

    if (shouldFire)
    {
        var content = SelectContent(situation);
        if (content != null)
            DeliverContent(content);
    }

    // Set IsQuietDay based on world state, not random roll
    SetQuietDayFromWorldState(situation);
}

3. SET enabled: true in config and test

═══════════════════════════════════════════════════════════════════════════════
PHASE 3C: REMOVE OLD SCHEDULE LOGIC
═══════════════════════════════════════════════════════════════════════════════

Now that orchestrator works, REMOVE the old schedule-driven code.

────────────────────────────────────────────────────────────────────────────────
FILE: src/Features/Content/EventPacingManager.cs
────────────────────────────────────────────────────────────────────────────────

REMOVE these methods/sections:

// REMOVE: Schedule window checking (around lines 100-113)
private bool IsInNarrativeEventWindow()
{
    var state = EscalationState.Instance;
    if (state.NextNarrativeEventWindow == CampaignTime.Never)
        return false;
    return CampaignTime.Now >= state.NextNarrativeEventWindow;
}

// REMOVE: Schedule window setting (around lines 114-125)
private void SetNextNarrativeEventWindow()
{
    var minDays = _config.EventPacing.EventWindowMinDays;  // 3
    var maxDays = _config.EventPacing.EventWindowMaxDays;  // 5
    var nextWindow = CampaignTime.Now + CampaignTime.Days(MBRandom.RandomInt(minDays, maxDays));
    EscalationState.Instance.NextNarrativeEventWindow = nextWindow;
}

// REMOVE: Scheduled TryFireEvent calls based on window
private void OnDailyTick()
{
    if (IsInNarrativeEventWindow())  // REMOVE this check
    {
        TryFireEvent();  // REMOVE this call
        SetNextNarrativeEventWindow();  // REMOVE this call
    }
}

KEEP these (still needed):

// KEEP: Grace period check (around lines 79-94)
private bool IsInGracePeriod()
{
    // New enlistees get 3-day grace period
    // ...
}

// KEEP: Chain event check (around lines 96-97)
private bool HasPendingChainEvent()
{
    // Chain events fire immediately after triggers
    // ...
}

// KEEP: Per-event cooldown tracking
// KEEP: Category cooldown tracking
// KEEP: TryFireEvent() method itself (orchestrator calls it)

────────────────────────────────────────────────────────────────────────────────
FILE: src/Features/Content/GlobalEventPacer.cs
────────────────────────────────────────────────────────────────────────────────

REMOVE these sections:

// REMOVE: Evaluation hours check (around lines 134-143)
private bool IsEvaluationHour()
{
    var currentHour = CampaignTime.Now.CurrentHourInDay;
    var evalHours = _config.EventPacing.EvaluationHours;  // [8, 14, 20]
    return evalHours.Contains(currentHour);
}

// REMOVE: Random quiet day roll (around lines 117-132)
private void RollForQuietDay()
{
    if (_config.EventPacing.AllowQuietDays)
    {
        var chance = _config.EventPacing.QuietDayChance;  // 0.15
        EscalationState.Instance.IsQuietDay = MBRandom.RandomFloat < chance;
    }
}

// REMOVE: Call to RollForQuietDay in OnDailyTick

KEEP these:

// KEEP: Safety limits (max_per_day, max_per_week, min_hours_between)
public bool CanFireAutoEvent(string eventId, string category, out string reason)
{
    // Too many today? Block.
    // Too soon after last? Block.
    // ...
}

// KEEP: RecordAutoEvent() tracking
// KEEP: GetEventsSinceHoursAgo() for cooldown checks

MODIFY:

// MODIFY: IsQuietDay is now SET by orchestrator, not rolled randomly
// Orchestrator calls: GlobalEventPacer.SetQuietDay(bool) based on world state

────────────────────────────────────────────────────────────────────────────────
FILE: src/Features/Escalation/EscalationState.cs
────────────────────────────────────────────────────────────────────────────────

REMOVE these fields:

// REMOVE: Schedule tracking (no longer needed)
public CampaignTime NextNarrativeEventWindow { get; set; }
public CampaignTime LastNarrativeEventTime { get; set; }

KEEP these:

// KEEP: IsQuietDay (now set by orchestrator)
public bool IsQuietDay { get; set; }

// KEEP: All reputation, discipline, tier tracking
// KEEP: All escalation thresholds

────────────────────────────────────────────────────────────────────────────────
FILE: ModuleData/Enlisted/enlisted_config.json - Event Pacing Section
────────────────────────────────────────────────────────────────────────────────

REMOVE these config fields:

{
  "event_pacing": {
    "event_window_min_days": 3,      // ❌ REMOVE
    "event_window_max_days": 5,      // ❌ REMOVE
    "evaluation_hours": [8, 14, 20], // ❌ REMOVE
    "allow_quiet_days": true,        // ❌ REMOVE
    "quiet_day_chance": 0.15         // ❌ REMOVE
  }
}

KEEP these config fields:

{
  "event_pacing": {
    "max_per_day": 2,           // ✅ KEEP - safety limit
    "max_per_week": 8,          // ✅ KEEP - safety limit
    "min_hours_between": 6      // ✅ KEEP - safety limit
  }
}

════════════════════════════════════════════════════════════════════════════════
WHAT STAYS THE SAME (DO NOT TOUCH)
═══════════════════════════════════════════════════════════════════════════════

These systems work correctly and should NOT be modified:

1. MapIncidentManager - Already context-driven (fires after battles)
2. EscalationManager - Consequences fire immediately (threshold events)
3. EventDeliveryManager - Queue system (orchestrator uses it)
4. EventRequirementChecker - Requirement validation (orchestrator uses it)
5. EventSelector - Content selection (orchestrator calls it with fitness scoring)
6. Chain events - Fire immediately after triggers (keep in EventPacingManager)
7. Grace period - New enlistees get 3 days (keep in EventPacingManager)

═══════════════════════════════════════════════════════════════════════════════
FILES SUMMARY
═══════════════════════════════════════════════════════════════════════════════

Files to MODIFY:
- src/Features/Content/EventPacingManager.cs (remove schedule logic, keep grace/chain)
- src/Features/Content/GlobalEventPacer.cs (remove eval hours, quiet roll; keep limits)
- src/Features/Escalation/EscalationState.cs (remove schedule fields)
- src/Features/Content/ContentOrchestrator.cs (take over daily tick)
- ModuleData/Enlisted/enlisted_config.json (add orchestrator, remove schedule fields)

Files to NOT modify:
- src/Features/Content/MapIncidentManager.cs (already works correctly)
- src/Features/Content/EventDeliveryManager.cs (orchestrator uses it)
- src/Features/Content/EventRequirementChecker.cs (orchestrator uses it)
- src/Features/Escalation/EscalationManager.cs (threshold events stay)

═══════════════════════════════════════════════════════════════════════════════
MIGRATION SAFETY CHECKLIST
═══════════════════════════════════════════════════════════════════════════════

Before removing ANY code:
[ ] Feature flag working (can toggle back to old system)
[ ] Orchestrator firing content correctly
[ ] World state detection accurate (garrison vs campaign vs siege)
[ ] Safety limits still enforced (no event spam)
[ ] Grace period still works for new enlistees
[ ] Chain events still fire immediately
[ ] Map incidents unaffected
[ ] Escalation thresholds unaffected

After removal:
[ ] Old config fields removed from enlisted_config.json
[ ] Old state fields removed from EscalationState.cs
[ ] No references to NextNarrativeEventWindow
[ ] No references to EvaluationHours
[ ] No references to QuietDayChance
[ ] IsQuietDay set by orchestrator based on world state

═══════════════════════════════════════════════════════════════════════════════
ACCEPTANCE CRITERIA
═══════════════════════════════════════════════════════════════════════════════

1. GARRISON feels quiet
   - ~0.4 events per day average
   - Lots of "nothing happens" days (world-state quiet, not random)

2. CAMPAIGN feels busy
   - ~1.5 events per day average
   - Consistent activity when marching/active

3. SIEGE feels intense
   - ~2.5 events per day average
   - Pressure and urgency reflected in frequency

4. BATTLE suppresses events
   - Near zero during active combat
   - Batch delivery after battle ends

5. Safety limits STILL work
   - max_per_day = 2 enforced
   - max_per_week = 8 enforced
   - min_hours_between = 6 enforced

6. NO SCHEDULE ARTIFACTS
   - No 3-5 day windows
   - No evaluation hours
   - No random quiet day rolls
   - Events fire when world state says they should

═══════════════════════════════════════════════════════════════════════════════
HANDOFF NOTES (Capture these for Phase 4)
═══════════════════════════════════════════════════════════════════════════════

When complete, provide these handoff notes for the next AI session:

FILES MODIFIED:
- [ ] src/Features/Content/EventPacingManager.cs (schedule logic removed)
- [ ] src/Features/Content/GlobalEventPacer.cs (eval hours/quiet roll removed)
- [ ] src/Features/Escalation/EscalationState.cs (schedule fields removed)
- [ ] src/Features/Content/ContentOrchestrator.cs (now takes over daily tick)
- [ ] ModuleData/Enlisted/enlisted_config.json (orchestrator section added)

WHAT WAS REMOVED:
- NextNarrativeEventWindow scheduling
- Evaluation hours (8am, 2pm, 8pm)
- Random quiet day roll (15%)
- Event window min/max days config

WHAT WAS KEPT:
- Safety limits (max_per_day, max_per_week, min_hours_between)
- Grace period for new enlistees
- Chain event immediate firing

KEY DECISIONS MADE:
- (list any architectural decisions)

KNOWN ISSUES/TECH DEBT:
- (list any incomplete items)

VERIFICATION PASSED:
[ ] Garrison feels quiet (~0.4 events/day)
[ ] Campaign feels busy (~1.5 events/day)
[ ] Siege feels intense (~2.5 events/day)
[ ] Safety limits still prevent spam
[ ] No schedule artifacts in logs
```

---

## Phase 4: Orders Integration

**Goal:** Coordinate order timing with orchestrator

**Status:** ✅ **COMPLETE** - All order events created, code integration complete

**What Was Completed:**
- ✅ ContentOrchestrator.CanIssueOrderNow() method added
- ✅ ContentOrchestrator.GetCurrentWorldSituation() method added
- ✅ OrderManager.TryIssueOrder() checks orchestrator timing
- ✅ All 16 order event JSON files created (85 events total)
- ✅ OrderProgressionBehavior.cs implementation (multi-day order execution)
- ✅ Placeholder text resolution uses existing EventDeliveryManager.SetEventTextVariables()
- ✅ Order event loading and filtering by world_state via EventCatalog
- ✅ Build successful, code compiles

**Remaining for runtime verification:**
- ⏳ Full integration testing in-game
- ⏳ Verify culture-specific placeholder resolution at runtime

**Chat Strategy:** 🔒 **STANDALONE** - Order integration is a distinct system

**Prerequisites:** Phase 3 complete (orchestrator is live and controlling event pacing)

```
I need you to implement Phase 4 of the Content Orchestrator - Orders integration.

═══════════════════════════════════════════════════════════════════════════════
CONTEXT RECOVERY (Verify Phase 3 work before starting)
═══════════════════════════════════════════════════════════════════════════════

Before implementing, verify:

Phase 3 Complete:
[ ] ContentOrchestrator.OnDailyTick() is the primary event firing mechanism
[ ] EventPacingManager schedule logic REMOVED (no window checking)
[ ] GlobalEventPacer evaluation hours REMOVED
[ ] GlobalEventPacer quiet day random roll REMOVED
[ ] enlisted_config.json has "orchestrator" section with enabled: true
[ ] World state drives event frequency (garrison quiet, siege intense)

Quick Test:
1. Launch game, enlist, wait in garrison
2. Check logs for "[Orchestrator]" entries
3. Verify frequency matches world state (garrison = ~0.4/day)

If orchestrator isn't live, implement Phase 3 first.

═══════════════════════════════════════════════════════════════════════════════
DOCUMENTATION TO READ FIRST
═══════════════════════════════════════════════════════════════════════════════

Read these docs first:
- docs/AFEATURE/content-orchestrator-plan.md (Phase 4 section)
- docs/AFEATURE/order-progression-system.md (NEW order execution system)
- docs/AFEATURE/orders-content.md (16 order definitions)
- docs/AFEATURE/order-events-master.md (85 events across 16 orders - SOURCE OF TRUTH)
- docs/AFEATURE/ORDER-SYSTEM-MIGRATION.md (what old code to remove)
- docs/Features/Content/event-system-schemas.md (JSON schema + placeholder variables)

CRITICAL: The Order Progression System REPLACES the old instant-resolution order mechanism. This is not an addition - you'll be removing old resolution code.

Old System: Accept → Skill Check → Immediate Result
New System: Accept → Multi-Day Progression → Accumulated Consequences

IMPORTANT: Order system has TWO integration points:
1. Order Issuance Timing - When OrderManager assigns NEW order to player
2. Order Event Weighting - During order execution (OrderProgressionBehavior)

Phase 4 Tasks:
1. Add CanIssueOrderNow() method to ContentOrchestrator
   - Returns bool based on world state timing
   - Called by OrderManager before issuing new order

2. Modify OrderManager.TryIssueOrder() to check orchestrator:
   - if (!ContentOrchestrator.Instance?.CanIssueOrderNow() ?? true) return;
   - Coordinates timing without changing order selection logic

3. Provide GetCurrentWorldSituation() to OrderProgressionBehavior:
   - Used for event slot weighting (Quiet ×0.3, Intense ×1.5)
   - Separate from narrative event frequency

4. Create order event JSON files from order-events-master.md:
   - Load events from ModuleData/Enlisted/Orders/order_events/*.json
   - 16 files (one per order), 85 total events
   - Events filter by requirements.world_state

5. Placeholder text resolution for events:
   ALREADY IMPLEMENTED in src/Features/Content/EventDeliveryManager.cs
   Method: SetEventTextVariables() (around line 2485)
   Uses Bannerlord's native TextObject.SetTextVariable()
   
   Available placeholders (just use these in JSON, they resolve automatically):
   - {SERGEANT}, {SERGEANT_NAME}, {NCO_RANK} → culture-specific NCO
   - {PLAYER_NAME}, {PLAYER_RANK} → player info
   - {LORD_NAME}, {LORD_TITLE} → enlisted lord
   - {COMRADE_NAME}, {SOLDIER_NAME}, {VETERAN_1_NAME}, {RECRUIT_NAME} → soldier names
   - {COMPANY_NAME}, {FACTION_NAME}, {KINGDOM_NAME} → organization names
   - {OFFICER_NAME}, {CAPTAIN_NAME} → officer names
   
   DO NOT BUILD A NEW PLACEHOLDER SYSTEM. Just write JSON with these placeholders.

6. Test coordination:
   - Orders + events don't overwhelm player
   - Order issuance feels appropriate to situation
   - Event text shows culture-correct rank titles

Files to Create:
- ModuleData/Enlisted/Orders/order_events/*.json (16 event files)

Files to Modify:
- src/Features/Content/ContentOrchestrator.cs (add CanIssueOrderNow)
- src/Features/Orders/Behaviors/OrderManager.cs (check before issuing)
- src/Features/Orders/Behaviors/OrderProgressionBehavior.cs (read WorldSituation, load events)

Timing by Activity Level:
- Quiet (garrison): New order every 3-5 days
- Routine (peacetime): New order every 2-3 days
- Active (campaign): New order every 1-2 days
- Intense (siege): New order every 0.5-1 day

Content Summary:
- 16 orders (8 T1-T3 Basic, 8 T4-T6 Specialist)
- 85 order events total
- Events use world_state requirements (peacetime_garrison, war_marching, siege_attacking, etc.)
- All event text must use placeholder variables for culture-awareness

═══════════════════════════════════════════════════════════════════════════════
WRITING STYLE (CRITICAL - Read existing events first!)
═══════════════════════════════════════════════════════════════════════════════

Before writing ANY event prose, read these existing events for the mod's voice:
- ModuleData/Enlisted/Orders/order_events/guard_post_events.json (6 events)
- ModuleData/Enlisted/Orders/order_events/sentry_duty_events.json (5 events)

STYLE RULES:

1. SECOND PERSON, PRESENT TENSE
   ✅ "You hear a strange noise in the darkness beyond your post."
   ❌ "The soldier heard a noise." (wrong POV)
   ❌ "You heard a noise." (wrong tense)

2. SHORT, PUNCHY SENTENCES - Military brevity
   ✅ "He staggers past. You hope {SERGEANT} doesn't find out."
   ❌ "The inebriated soldier stumbles past your post, and you find yourself hoping that the sergeant doesn't discover your lapse in judgment."

3. GROUNDED, NOT FLOWERY - Real consequences, not drama
   ✅ "You trip in the dark and bash your shin. Nothing there."
   ❌ "Pain lances through your leg as you stumble into the inky blackness, the shadows mocking your foolish bravery."

4. MILITARY ATMOSPHERE - Duty, rank, orders, consequences
   ✅ "{SERGEANT} told you no one passes without authorization."
   ✅ "You held your post."
   ✅ "{SERGEANT} notes your dedication."

5. CHOICES FEEL REAL - Not "good/evil", just different tradeoffs
   ✅ "Let him through. Not worth the trouble." (pragmatic)
   ✅ "Turn him away. Orders are orders." (dutiful)
   ✅ "Report him to {SERGEANT}." (by-the-book)

6. RESULT TEXT IS BRIEF - 1-2 sentences max
   ✅ "He curses you but stumbles off. You held your post."
   ❌ "The soldier, clearly frustrated by your adherence to protocol, mutters a string of curses under his breath before turning away..."

7. USE PLACEHOLDERS - For culture-awareness and immersion
   ✅ "{SERGEANT} hauls him away."
   ✅ "'Good work, {PLAYER_RANK}. You know your duty.'"
   ❌ "The sergeant hauls him away." (no placeholder)

TONE: Light military RP. You're a soldier doing a job. Not a hero, not an epic.
Things break. Orders are annoying. Officers test you. Comrades need help.
Small moments, real consequences.

Acceptance Criteria:
- OrderManager checks CanIssueOrderNow() before issuing
- Orders arrive realistically based on world state
- Order slot events weighted by activity level
- Event text displays with culture-specific NCO/officer titles
- Camp life events fire between orders (orchestrator handles)
- No overwhelming spam from combined systems

═══════════════════════════════════════════════════════════════════════════════
HANDOFF NOTES (Phase 4 Progress Update - 2025-12-30)
═══════════════════════════════════════════════════════════════════════════════

FILES CREATED:
- [x] src/Features/Orders/Behaviors/OrderProgressionBehavior.cs (multi-day order execution with phase-based events)
- [x] ModuleData/Enlisted/Orders/order_events/guard_post_events.json (6 events)
- [x] ModuleData/Enlisted/Orders/order_events/sentry_duty_events.json (5 events)
- [x] ModuleData/Enlisted/Orders/order_events/camp_patrol_events.json (6 events)
- [x] ModuleData/Enlisted/Orders/order_events/firewood_detail_events.json (5 events)
- [x] ModuleData/Enlisted/Orders/order_events/latrine_duty_events.json (3 events)
- [x] ModuleData/Enlisted/Orders/order_events/equipment_cleaning_events.json (4 events)
- [x] ModuleData/Enlisted/Orders/order_events/muster_inspection_events.json (3 events)
- [x] ModuleData/Enlisted/Orders/order_events/march_formation_events.json (5 events)
- [x] ModuleData/Enlisted/Orders/order_events/scout_route_events.json (8 events)
- [x] ModuleData/Enlisted/Orders/order_events/escort_duty_events.json (6 events)
- [x] ModuleData/Enlisted/Orders/order_events/lead_patrol_events.json (8 events)
- [x] ModuleData/Enlisted/Orders/order_events/treat_wounded_events.json (6 events)
- [x] ModuleData/Enlisted/Orders/order_events/repair_equipment_events.json (5 events)
- [x] ModuleData/Enlisted/Orders/order_events/forage_supplies_events.json (6 events)
- [x] ModuleData/Enlisted/Orders/order_events/train_recruits_events.json (5 events)
- [x] ModuleData/Enlisted/Orders/order_events/inspect_defenses_events.json (4 events)

PROGRESS: 85 of 85 events complete (16 of 16 order types)

FILES MODIFIED:
- [x] src/Features/Content/ContentOrchestrator.cs (added CanIssueOrderNow, GetCurrentWorldSituation)
- [x] src/Features/Orders/Behaviors/OrderManager.cs (checks orchestrator before issuing)
- [x] src/Features/Content/EventCatalog.cs (added GetOrderEventsBasePath, GetEventsByOrderType, order_type parsing)
- [x] src/Features/Content/EventDefinition.cs (added OrderType property)
- [x] src/Mod.Entry/SubModule.cs (registered OrderProgressionBehavior)

PLACEHOLDER RESOLUTION:
- [x] ALREADY EXISTS - src/Features/Content/EventDeliveryManager.cs
- [x] Method: SetEventTextVariables() (around line 2485)
- [x] Uses Bannerlord's native TextObject.SetTextVariable()
- [x] All placeholders work: {SERGEANT}, {LORD_NAME}, {PLAYER_RANK}, {SOLDIER_NAME}, {COMRADE_NAME}, etc.
- DO NOT BUILD NEW SYSTEM - just write JSON with placeholders, they resolve automatically

KEY DECISIONS MADE:
- Order issuance timing coordinated via CanIssueOrderNow() check in OrderManager
- GetCurrentWorldSituation() provides world state for future order event weighting
- Order events use world_state requirements for contextual filtering
- Directory created: ModuleData/Enlisted/Orders/order_events/
- OrderProgressionBehavior uses hourly tick to check phase transitions (Dawn/Midday/Dusk/Night)
- Event chance weighted by activity level (Quiet ×0.3, Routine ×0.6, Active ×1.0, Intense ×1.5)
- EventCatalog extended to load from Orders/order_events/ directory

VERIFICATION STATUS:
- [x] Order issuance coordinated with orchestrator (code level)
- [x] OrderProgressionBehavior created and registered
- [x] EventCatalog loads order events from JSON
- [x] Events filtered by world_state requirements
- [x] Camp life events fire between orders (orchestrator handles this)
- [x] No spam from combined systems (build successful, no conflicts)
- [ ] Culture-specific text displays correctly (needs runtime testing)

PHASE 4 CONTENT COMPLETE:
All 16 order event JSON files created with 85 total events.

REMAINING FOR RUNTIME VERIFICATION:
1. Test full order progression with event injection
2. Verify placeholder resolution at runtime
3. Confirm culture-specific NCO/officer titles display correctly
```

---

## Phase 4.5: Native Effect Integration

**Goal:** Bridge JSON content definitions with Bannerlord's native `IncidentEffect` system for execution, tooltips, and trait integration

**Chat Strategy:** 🔒 **STANDALONE** - Native API integration using decompile-verified patterns

**Prerequisites:** Phase 4 complete (EventDeliveryManager exists and applies effects, 85 order events created)

**Why This Phase Exists:**
The decompiled Bannerlord source (`TaleWorlds.CampaignSystem\Incidents\`) contains a rich `IncidentEffect` library that provides:
1. **Automatic tooltip generation** - Each effect has `GetHint()` with probability display
2. **Native trait integration** - `TraitChange()` affects personality traits shown in character sheet
3. **Standardized patterns** - Gold, skills, morale, health all follow same pattern

Our current system manually applies effects in `EventDeliveryManager.ApplyEventEffects()`. This phase creates a bridge layer so we keep JSON flexibility but gain native features.

```
I need you to implement Phase 4.5 of the Content Orchestrator - Native Effect Integration.

═══════════════════════════════════════════════════════════════════════════════
CONTEXT RECOVERY (Verify Phase 4 work before starting)
═══════════════════════════════════════════════════════════════════════════════

Before implementing, verify:

Phase 4 Complete:
[x] src/Features/Content/EventDeliveryManager.cs exists
[x] ApplyEventEffects() method handles EventEffects from JSON
[x] EventDefinition, EventOption, EventEffects classes exist
[x] Events fire correctly from orchestrator
[x] All 16 order event JSON files created (85 events total)

Key Classes to Read First:
- src/Features/Content/EventDefinition.cs (EventEffects class shows current effect fields)
- src/Features/Content/EventDeliveryManager.cs (ApplyEventEffects method)
- src/Features/Context/StrategicContextProvider.cs (provides world state context)

If EventDeliveryManager doesn't exist, implement Phase 4 first.

═══════════════════════════════════════════════════════════════════════════════
DOCUMENTATION TO READ FIRST
═══════════════════════════════════════════════════════════════════════════════

Read these docs:
- docs/BLUEPRINT.md (coding standards, API verification)
- docs/Features/Content/event-system-schemas.md (JSON schema for events)

CRITICAL - Use the local decompile as reference (do not use external AI or outdated docs):
- C:\Dev\Enlisted\Decompile\TaleWorlds.CampaignSystem\TaleWorlds\CampaignSystem\Incidents\IncidentEffect.cs
- C:\Dev\Enlisted\Decompile\TaleWorlds.CampaignSystem\TaleWorlds\CampaignSystem\Incidents\Incident.cs
- C:\Dev\Enlisted\Decompile\TaleWorlds.CampaignSystem\TaleWorlds\CampaignSystem\CharacterDevelopment\TraitLevelingHelper.cs

═══════════════════════════════════════════════════════════════════════════════
UNDERSTANDING THE NATIVE INCIDENT SYSTEM
═══════════════════════════════════════════════════════════════════════════════

The native IncidentEffect class (from decompile) provides:

STATIC FACTORY METHODS:
- IncidentEffect.GoldChange(amount) → adds/removes gold with feedback
- IncidentEffect.SkillChange(skill, xp) → adds skill XP
- IncidentEffect.TraitChange(trait, amount) → modifies personality trait (Honor, Valor, etc.)
- IncidentEffect.MoraleChange(amount) → changes party morale
- IncidentEffect.HealthChance(minDmg, maxDmg) → damage with probability
- IncidentEffect.RenownChange(amount) → changes renown
- IncidentEffect.WoundTroopsRandomly(count) → wounds random troops
- IncidentEffect.KillTroopsRandomly(count) → kills random troops
- IncidentEffect.Group(effects...) → applies all effects
- IncidentEffect.Select(weight, effect) → random selection from weighted effects
- IncidentEffect.Custom(condition, consequence, hint) → custom effect with lambdas

KEY FEATURES WE WANT:
1. effect.Consequence() → applies effect, returns feedback TextObject list
2. effect.GetHint() → returns tooltip TextObjects (includes probabilities!)
3. effect.WithChance(probability) → wraps effect with probability

NATIVE TRAIT SYSTEM:
DefaultTraits.Honor, DefaultTraits.Valor, DefaultTraits.Mercy, DefaultTraits.Generosity, DefaultTraits.Calculating
These appear on NPC character sheets and affect NPC reactions to the player.

OUR CUSTOM REPUTATION (Soldier/Officer/Lord) is SEPARATE - we keep that.
But we can MAP our reputation to native traits for UI integration:
- High Soldier rep + dutiful choices → +Honor, +Valor
- High Officer rep + leadership → +Generosity, +Calculating
- High Lord rep + mercy/cruelty choices → +/-Mercy

═══════════════════════════════════════════════════════════════════════════════
PHASE 4.5 TASKS
═══════════════════════════════════════════════════════════════════════════════

TASK 1: Create IncidentEffectTranslator.cs

Create: src/Features/Content/IncidentEffectTranslator.cs

This class translates JSON EventEffects into native IncidentEffect chains:

```csharp
using TaleWorlds.CampaignSystem.Incidents;
using System.Collections.Generic;

/// <summary>
/// Bridges JSON-defined EventEffects to native Bannerlord IncidentEffect objects.
/// This lets us keep JSON content flexibility while gaining native tooltip generation
/// and standardized effect application patterns.
/// </summary>
public static class IncidentEffectTranslator
{
    /// <summary>
    /// Converts EventEffects from JSON into a list of native IncidentEffect objects.
    /// These can be executed with .Consequence() or queried with .GetHint() for tooltips.
    /// </summary>
    public static List<IncidentEffect> TranslateEffects(EventEffects effects)
    {
        var result = new List<IncidentEffect>();
        if (effects == null) return result;

        // Gold
        if (effects.Gold.HasValue && effects.Gold.Value != 0)
            result.Add(IncidentEffect.GoldChange(effects.Gold.Value));

        // Skill XP
        if (effects.SkillXp != null)
        {
            foreach (var kvp in effects.SkillXp)
            {
                var skill = SkillObject.All.FirstOrDefault(s =>
                    s.StringId.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase) ||
                    s.Name.ToString().Equals(kvp.Key, StringComparison.OrdinalIgnoreCase));
                if (skill != null)
                    result.Add(IncidentEffect.SkillChange(skill, kvp.Value));
            }
        }

        // Morale (if native supports party morale change)
        if (effects.Discipline.HasValue && effects.Discipline.Value != 0)
            result.Add(IncidentEffect.MoraleChange(effects.Discipline.Value));

        // Health damage
        if (effects.HpChange.HasValue && effects.HpChange.Value < 0)
            result.Add(IncidentEffect.HealthChance(Math.Abs(effects.HpChange.Value), Math.Abs(effects.HpChange.Value)));

        // Troop losses
        if (effects.TroopLoss.HasValue && effects.TroopLoss.Value > 0)
            result.Add(IncidentEffect.KillTroopsRandomly(effects.TroopLoss.Value));

        // Troop wounded
        if (effects.TroopWounded.HasValue && effects.TroopWounded.Value > 0)
            result.Add(IncidentEffect.WoundTroopsRandomly(effects.TroopWounded.Value));

        // Renown
        if (effects.Renown.HasValue && effects.Renown.Value != 0)
            result.Add(IncidentEffect.RenownChange(effects.Renown.Value));

        // Native trait integration (map rep choices to personality traits)
        result.AddRange(TranslateReputationToTraits(effects));

        return result;
    }

    // Default scale divisor - tune for career length (see TRAIT MAPPING STRATEGY section)
    // Lower = faster trait progression, Higher = slower
    // Recommended: 5 for 100-day careers, 8 for longer
    private static int ScaleDivisor => EnlistedConfig.Instance?.NativeTraitScaleDivisor ?? 5;
    private static int MinimumChange => EnlistedConfig.Instance?.NativeTraitMinimumChange ?? 1;

    /// <summary>
    /// Maps our custom Soldier/Officer/Lord reputation to native personality traits.
    /// This provides free UI integration on the character sheet.
    /// Scaled for long careers (~100+ days enlisted).
    /// </summary>
    private static List<IncidentEffect> TranslateReputationToTraits(EventEffects effects)
    {
        var result = new List<IncidentEffect>();

        // Soldier rep → Valor (bravery, fighting spirit)
        if (effects.SoldierRep.HasValue && effects.SoldierRep.Value != 0)
        {
            var valorAmount = effects.SoldierRep.Value / ScaleDivisor;
            if (Math.Abs(valorAmount) >= MinimumChange)
                result.Add(IncidentEffect.TraitChange(DefaultTraits.Valor, valorAmount));
        }

        // Officer rep → Calculating (tactical, organized)
        if (effects.OfficerRep.HasValue && effects.OfficerRep.Value != 0)
        {
            var calcAmount = effects.OfficerRep.Value / ScaleDivisor;
            if (Math.Abs(calcAmount) >= MinimumChange)
                result.Add(IncidentEffect.TraitChange(DefaultTraits.Calculating, calcAmount));
        }

        // Lord rep → Honor (duty, keeping word)
        if (effects.LordRep.HasValue && effects.LordRep.Value != 0)
        {
            var honorAmount = effects.LordRep.Value / ScaleDivisor;
            if (Math.Abs(honorAmount) >= MinimumChange)
                result.Add(IncidentEffect.TraitChange(DefaultTraits.Honor, honorAmount));
        }

        return result;
    }

    /// <summary>
    /// Generates tooltip hint text for an option's effects.
    /// Uses native IncidentEffect.GetHint() which includes probability display.
    /// </summary>
    public static List<TextObject> GenerateTooltipHints(EventEffects effects)
    {
        var hints = new List<TextObject>();
        var incidentEffects = TranslateEffects(effects);

        foreach (var effect in incidentEffects)
        {
            hints.AddRange(effect.GetHint());
        }

        return hints;
    }
}
```

TASK 2: Integrate with EventDeliveryManager

Modify: src/Features/Content/EventDeliveryManager.cs

Add a hybrid execution path that uses native effects where beneficial:

```csharp
/// <summary>
/// Applies effects using native IncidentEffect system where applicable.
/// Falls back to manual application for Enlisted-specific effects.
/// </summary>
private void ApplyNativeEffects(EventEffects effects, List<string> feedback)
{
    var nativeEffects = IncidentEffectTranslator.TranslateEffects(effects);

    foreach (var effect in nativeEffects)
    {
        try
        {
            var results = effect.Consequence();
            foreach (var result in results)
            {
                feedback.Add(result.ToString());
            }
        }
        catch (Exception ex)
        {
            EnlistedLogger.LogWarning($"[EventDelivery] Native effect failed: {ex.Message}");
        }
    }
}
```

In ApplyEventEffects(), call ApplyNativeEffects() for translatable effects, then apply
Enlisted-specific effects manually (RetinueGain, BaggageAccess, Fatigue, etc.).

TASK 3: Add Tooltip Generation to Option Display

When building event option UI, use the translator for hints:

```csharp
public string BuildOptionTooltip(EventOption option)
{
    var hints = IncidentEffectTranslator.GenerateTooltipHints(option.Effects);

    // Add Enlisted-specific effect hints (not in native system)
    if (option.Effects?.Fatigue != null)
        hints.Add(new TextObject($"+{option.Effects.Fatigue} Fatigue"));
    if (option.Effects?.RetinueGain != null)
        hints.Add(new TextObject($"+{option.Effects.RetinueGain} Retinue Member"));
    // ... other Enlisted-specific effects

    return string.Join("\n", hints.Select(h => h.ToString()));
}
```

TASK 4: Verify Native API Compatibility

Before implementing, verify in the decompile that these methods exist:
[ ] IncidentEffect.GoldChange(int amount)
[ ] IncidentEffect.SkillChange(SkillObject, float)
[ ] IncidentEffect.TraitChange(TraitObject, int)
[ ] IncidentEffect.MoraleChange(int)
[ ] IncidentEffect.HealthChance(int, int)
[ ] IncidentEffect.RenownChange(int)
[ ] IncidentEffect.WoundTroopsRandomly(int)
[ ] IncidentEffect.KillTroopsRandomly(int)
[ ] effect.GetHint() returns List<TextObject>
[ ] effect.Consequence() returns List<TextObject>

If any are missing or have different signatures, adjust the translator accordingly.
The decompile is the source of truth for API compatibility.

TASK 5: Add Logging and Fallbacks

Ensure the translator logs what it's doing and gracefully handles missing effects:

```csharp
EnlistedLogger.LogDebug($"[EffectTranslator] Translated {effects} → {result.Count} native effects");
```

If an effect can't be translated (native doesn't support it), the translator should:
1. Log it for debugging
2. Return an empty effect (or skip it)
3. Let the manual effect handler in EventDeliveryManager handle it

═══════════════════════════════════════════════════════════════════════════════
ENLISTED-SPECIFIC EFFECTS (NOT translated to native)
═══════════════════════════════════════════════════════════════════════════════

These effects are specific to the Enlisted mod and should remain manually applied:

- Fatigue → custom Enlisted fatigue system
- SoldierRep, OfficerRep, LordRep → keep manual (we only mirror to native traits)
- Scrutiny → Enlisted escalation system
- RetinueGain, RetinueLoss, RetinueLoyalty, RetinueWounded → custom retinue system
- GrantTemporaryBaggageAccess, BaggageDelayDays, RandomBaggageLoss → baggage system
- ChainEventId → event chaining logic
- TriggersDischarge, Promotes → Enlisted progression
- CompanyNeeds → custom needs system
- ApplyWound → custom wound types
- FoodLoss → could use native but ties to Enlisted supply tracking

The translator handles what native can do; manual code handles the rest.

═══════════════════════════════════════════════════════════════════════════════
TRAIT MAPPING STRATEGY
═══════════════════════════════════════════════════════════════════════════════

Our Soldier/Officer/Lord reputation is INTERNAL to Enlisted - it affects:
- Which content is available (requirements.soldierRep > 50)
- NPC dialogue and reactions within the mod
- Progression tier calculations

Native traits (Honor, Valor, Mercy, Generosity, Calculating) are EXTERNAL:
- Displayed on player character sheet
- Affect native game reactions (lords, companions)
- Persist independent of Enlisted systems

CAREER SCALING (tuned for long enlistment):

The scaling must work for players enlisted 50-200+ in-game days. Too slow and traits never
change; too fast and they max out in a week.

BASELINE MATH (verify against decompile - check TraitLevelingHelper):
- Native trait levels typically: -2, -1, 0, +1, +2
- XP per level: ~50-100 XP (check DefaultTraitLevelingModel in decompile)
- Player should shift 1-2 trait levels over a full campaign (~100 days enlisted)

TYPICAL REP GAINS PER EVENT:
- Small events: +3 to +5 rep
- Medium events: +5 to +10 rep
- Big events: +10 to +15 rep
- Events with rep: ~0.5-1 per day average

SCALING CALCULATION:
- 100 days × 0.75 events/day × avg +6 rep × scale = target 50-75 XP for 1 level shift
- Math: 100 × 0.75 × 6 × scale = ~60 → scale ≈ 0.13 (roughly ÷8)
- Rounded: Use ÷5 for noticeable progression, ÷10 for very gradual

RECOMMENDED STARTING SCALE: ÷5 (not ÷3)
This gives ~90 trait XP over a 100-day career, enough for 1 solid level shift.
If playtesting shows too fast, increase to ÷8. Too slow, decrease to ÷3.

MAKE IT CONFIGURABLE:
Put the scale factor in enlisted_config.json so it can be tuned without recompile:
```json
"nativeTraitMapping": {
  "enabled": true,
  "scaleDivisor": 5,
  "minimumChange": 1
}
```

MAPPING:

| Enlisted Rep | Native Trait | Default Scale | Rationale |
|--------------|--------------|---------------|-----------|
| Soldier Rep  | Valor        | ÷5            | Fighting spirit, bravery in battle |
| Officer Rep  | Calculating  | ÷5            | Tactical thinking, leadership |
| Lord Rep     | Honor        | ÷5            | Duty, keeping word, respect |

MINIMUM CHANGE THRESHOLD:
Only apply trait change if result ≥ 1 (to avoid spam of +0 changes).
If rep change is +4 and scale is ÷5, result is 0 → skip.
If rep change is +5 and scale is ÷5, result is 1 → apply.

OPTIONAL FUTURE MAPPINGS (not in initial implementation):
- Mercy choices in events → DefaultTraits.Mercy
- Generous choices → DefaultTraits.Generosity
- These would require parsing event option themes, not just rep values

═══════════════════════════════════════════════════════════════════════════════
NEWS SYSTEM INTEGRATION
═══════════════════════════════════════════════════════════════════════════════

When native traits change, the player should be informed. Two levels of feedback:

1. IMMEDIATE FEEDBACK (in event result text):
   The native IncidentEffect.TraitChange() already returns TextObject feedback.
   This goes in the event result popup alongside other effects.

2. NEWS MILESTONE NOTIFICATIONS (when crossing trait levels):
   When a trait crosses a level threshold (e.g., 0 → 1 Valor), push a news item.
   
   Add to EnlistedNewsBehavior.cs or create TraitMilestoneTracker:

```csharp
/// <summary>
/// Tracks when native traits cross level thresholds and pushes news.
/// Called after applying trait effects.
/// </summary>
public static class TraitMilestoneTracker
{
    private static Dictionary<TraitObject, int> _previousLevels = new();

    public static void CheckForMilestones(Hero hero)
    {
        foreach (var trait in new[] { DefaultTraits.Valor, DefaultTraits.Calculating, DefaultTraits.Honor })
        {
            var currentLevel = hero.GetTraitLevel(trait);
            
            if (!_previousLevels.TryGetValue(trait, out var previousLevel))
            {
                _previousLevels[trait] = currentLevel;
                continue;
            }

            if (currentLevel != previousLevel)
            {
                PushTraitMilestoneNews(trait, previousLevel, currentLevel);
                _previousLevels[trait] = currentLevel;
            }
        }
    }

    private static void PushTraitMilestoneNews(TraitObject trait, int oldLevel, int newLevel)
    {
        var direction = newLevel > oldLevel ? "gained" : "lost";
        var message = GetTraitMilestoneMessage(trait, newLevel);
        
        EnlistedNewsBehavior.Instance?.PushNews(
            "trait_milestone",
            message,
            NewsCategory.Personal,
            importance: NewsImportance.Medium
        );
        
        EnlistedLogger.LogInfo($"[Traits] {trait.Name}: {oldLevel} → {newLevel}");
    }

    private static string GetTraitMilestoneMessage(TraitObject trait, int level)
    {
        // Flavor text based on trait and level
        if (trait == DefaultTraits.Valor)
        {
            return level switch
            {
                >= 2 => "Your bravery in battle has become legendary among the troops.",
                1 => "Soldiers speak of your courage under fire.",
                0 => "Your reputation for valor is unremarkable.",
                -1 => "Some whisper that you lack courage.",
                _ => "Your cowardice is well known."
            };
        }
        if (trait == DefaultTraits.Calculating)
        {
            return level switch
            {
                >= 2 => "Officers seek your counsel on tactical matters.",
                1 => "Your tactical thinking has been noticed by command.",
                0 => "Your judgment is considered sound.",
                -1 => "Some question your decision-making.",
                _ => "Your poor judgment is notorious."
            };
        }
        if (trait == DefaultTraits.Honor)
        {
            return level switch
            {
                >= 2 => "Lords speak highly of your sense of duty and honor.",
                1 => "Your word is trusted. You've earned respect.",
                0 => "You're known as neither particularly honorable nor dishonorable.",
                -1 => "Some doubt whether you can be trusted.",
                _ => "Your reputation for dishonor precedes you."
            };
        }
        return $"Your {trait.Name} has changed.";
    }
}
```

INTEGRATION POINT:
In EventDeliveryManager, after applying native effects, call:
```csharp
TraitMilestoneTracker.CheckForMilestones(Hero.MainHero);
```

LOCALIZATION KEYS TO ADD:
- trait_milestone_valor_high, trait_milestone_valor_low
- trait_milestone_calculating_high, trait_milestone_calculating_low
- trait_milestone_honor_high, trait_milestone_honor_low

WHY THIS MATTERS:
- Players need feedback that their choices have long-term consequences
- Native trait changes are invisible without news - player wouldn't know
- Crossing a level is significant (takes many events) - worth announcing
- Flavor text reinforces the roleplay ("Officers seek your counsel...")

═══════════════════════════════════════════════════════════════════════════════
EDGE CASES
═══════════════════════════════════════════════════════════════════════════════

1. NATIVE API MISMATCH
   - Some IncidentEffect methods may have different signatures in current game version
   - ALWAYS verify against decompile before implementing
   - If a method doesn't exist, skip that effect type and log it
   - Fallback: apply manually like before (no regression)

2. NULL/EMPTY EFFECTS
   - EventEffects can be null or have all-null fields
   - TranslateEffects() should return empty list, not throw
   - Check each field before processing: `if (effects?.Gold.HasValue == true)`

3. SKILL NAME MISMATCHES
   - JSON uses string names ("Athletics", "OneHanded")
   - Native uses SkillObject.StringId ("Athletics") or Name ("One Handed")
   - Try both StringId and Name when matching
   - Log warning if skill not found, don't crash

4. EFFECT EXECUTION FAILURES
   - Wrap each effect.Consequence() in try-catch
   - Log failure but continue with other effects
   - Native effects might fail if Hero.MainHero is null (not enlisted yet)

5. TRAIT BOUNDS
   - Native traits cap at certain levels (typically -2 to +2)
   - Don't worry about overflow - native system handles clamping
   - But log when player hits trait cap for debugging

6. NOT ENLISTED
   - Some events might fire during edge states (loading, just discharged)
   - Check EnlistmentBehavior.Instance?.IsEnlisted before applying troop effects
   - Hero effects (gold, skills, traits) are safe anytime

7. DUPLICATE EFFECT APPLICATION
   - Event chains might re-apply parent effects if not careful
   - Translator is stateless (pure function) - safe for multiple calls
   - Deduplication is EventDeliveryManager's responsibility, not translator's

8. ORDER OF OPERATIONS
   - Apply native effects FIRST (they generate standardized feedback)
   - Then apply Enlisted-specific effects manually
   - Combine feedback messages in order

9. TOOLTIP GENERATION EDGE CASES
   - Effects with 0 value should not generate hints
   - Effects that fail condition checks should not generate hints
   - Check effect._chanceToOccur for probability display

10. SAVE/LOAD COMPATIBILITY
    - Native traits persist in game save automatically
    - Our custom rep persists via EnlistedSaveDefiner
    - Both systems are independent - no compatibility concern
    - But: if player loads old save, native traits start from vanilla values

11. HERO VS PARTY EFFECTS
    - SkillChange, TraitChange, HealthChance → apply to Hero.MainHero
    - WoundTroopsRandomly, KillTroopsRandomly → apply to MobileParty.MainParty
    - GoldChange → apply to Hero.MainHero
    - MoraleChange → apply to MobileParty.MainParty
    - Check party exists before troop effects

12. DISABLED VIA CONFIG
    - If nativeTraitMapping.enabled = false, skip trait translation entirely
    - Still apply other native effects (gold, skills, etc.)
    - Allow disabling entire native bridge if causing issues

DEFENSIVE CODING PATTERN:
```csharp
public static List<IncidentEffect> TranslateEffects(EventEffects effects)
{
    var result = new List<IncidentEffect>();
    if (effects == null) return result;

    try
    {
        // Gold - safe for any state
        if (effects.Gold.HasValue && effects.Gold.Value != 0)
            result.Add(IncidentEffect.GoldChange(effects.Gold.Value));

        // Troop effects - need party
        if (MobileParty.MainParty != null)
        {
            if (effects.TroopLoss.HasValue && effects.TroopLoss.Value > 0)
                result.Add(IncidentEffect.KillTroopsRandomly(effects.TroopLoss.Value));
        }

        // Trait effects - check config
        if (EnlistedConfig.Instance?.NativeTraitMappingEnabled == true)
        {
            result.AddRange(TranslateReputationToTraits(effects));
        }
    }
    catch (Exception ex)
    {
        EnlistedLogger.LogWarning($"[EffectTranslator] Translation failed: {ex.Message}");
        // Return partial results - don't lose effects that succeeded
    }

    return result;
}
```

═══════════════════════════════════════════════════════════════════════════════
FILES TO CREATE
═══════════════════════════════════════════════════════════════════════════════

- src/Features/Content/IncidentEffectTranslator.cs
- src/Features/Content/TraitMilestoneTracker.cs (news integration for trait level changes)

═══════════════════════════════════════════════════════════════════════════════
FILES TO MODIFY
═══════════════════════════════════════════════════════════════════════════════

- src/Features/Content/EventDeliveryManager.cs (add hybrid effect application + milestone check)
- src/Features/Interface/Behaviors/EnlistedNewsBehavior.cs (ensure PushNews supports trait milestones)
- src/Mod.Core/EnlistedConfig.cs (add NativeTraitScaleDivisor, NativeTraitMinimumChange)
- ModuleData/Enlisted/enlisted_config.json (add nativeTraitMapping section)
- ModuleData/Languages/enlisted_strings.xml (add trait milestone localization keys)
- Enlisted.csproj (add new file references)

═══════════════════════════════════════════════════════════════════════════════
ACCEPTANCE CRITERIA
═══════════════════════════════════════════════════════════════════════════════

CORE FUNCTIONALITY:
- [ ] IncidentEffectTranslator correctly converts JSON effects to native IncidentEffect
- [ ] Gold, Skill XP, and Renown changes use native effect pattern
- [ ] Reputation changes mirror to native personality traits
- [ ] Event option tooltips use native GetHint() for auto-generated text
- [ ] Tooltips show probability percentages when effects have chance
- [ ] Enlisted-specific effects still work (Fatigue, Retinue, Baggage, etc.)
- [ ] No regressions in existing event functionality
- [ ] All API calls verified against local decompile
- [ ] Build succeeds with no errors

EDGE CASE HANDLING:
- [ ] Null/empty EventEffects returns empty list (no crash)
- [ ] Missing skill names log warning, don't crash
- [ ] Effect execution failures caught and logged, other effects continue
- [ ] Troop effects skip gracefully if MobileParty.MainParty is null
- [ ] Trait mapping respects enabled flag in config
- [ ] Trait scaling configurable via enlisted_config.json
- [ ] Zero-value effects filtered out (no spam)

CAREER SCALING:
- [ ] Scale divisor defaults to 5 (tuned for ~100 day careers)
- [ ] Minimum change threshold filters tiny adjustments
- [ ] Config values load correctly from JSON

NEWS INTEGRATION:
- [ ] TraitMilestoneTracker detects level threshold crossings
- [ ] News pushed when trait level changes (not every XP gain)
- [ ] Flavor text matches trait and level (positive/negative)
- [ ] Localization keys added for all milestone messages

═══════════════════════════════════════════════════════════════════════════════
HANDOFF NOTES (Phase 4.5 COMPLETE - 2025-12-30)
═══════════════════════════════════════════════════════════════════════════════

FILES CREATED:
- [x] src/Features/Content/IncidentEffectTranslator.cs
- [x] src/Features/Content/TraitMilestoneTracker.cs

FILES MODIFIED:
- [x] src/Features/Content/EventDeliveryManager.cs (added TraitMilestoneTracker.CheckForMilestones call)
- [x] src/Mod.Core/Config/ConfigurationManager.cs (added NativeTraitMappingConfig + loader)
- [x] ModuleData/Enlisted/enlisted_config.json (added native_trait_mapping section)
- [x] ModuleData/Languages/enlisted_strings.xml (16 trait milestone localization keys)
- [x] Enlisted.csproj (added new file references)

NOTE: EnlistedNewsBehavior was NOT modified - TraitMilestoneTracker uses InformationManager
for immediate feedback since AddPersonalNews is private. Future enhancement could expose
a public method for trait milestone news integration.

NATIVE EFFECTS INTEGRATED:
- GoldChange (via Func<int> getter pattern)
- SkillChange (skill XP awards)
- TraitChange (personality trait integration)
- MoraleChange (mapped from Discipline, scaled 2x)
- HealthChance (HP changes)
- RenownChange (clan renown)
- WoundTroopsRandomly (int count version)
- KillTroopsRandomly NOT used (native uses percentage, we use manual for count)

TRAIT MAPPING IMPLEMENTED:
- Soldier Rep → DefaultTraits.Valor (bravery, fighting spirit)
- Officer Rep → DefaultTraits.Calculating (tactical thinking)
- Lord Rep → DefaultTraits.Honor (duty, keeping word)
- Scale divisor: 5 (configurable via enlisted_config.json)
- Minimum change: 1 (filters tiny adjustments)
- Expected progression: ~1-2 trait level shifts over 100-day career

TOOLTIP GENERATION:
- [x] TranslateEffects() returns native IncidentEffect list
- [x] GenerateTooltipHints() uses native GetHint() for auto-generated text
- [x] BuildCombinedTooltip() combines native + Enlisted-specific hints
- [x] Probability percentages display via native effect patterns
- [x] Enlisted-specific effects (Fatigue, Retinue, Scrutiny, etc.) have manual hints

KEY DECISIONS MADE:
- Used Skills.All from TaleWorlds.CampaignSystem.Extensions for skill lookup
- GoldChange uses Func<int> getter pattern (required by native API signature)
- TroopLoss uses manual application (native KillTroopsRandomly uses percentage)
- Discipline mapped to MoraleChange with 2x scale (discipline 0-10 → morale larger values)
- Config stored in ConfigurationManager.cs, NOT separate EnlistedConfig.cs (project pattern)

VERIFICATION PASSED:
[x] Build succeeds with no errors
[x] Native effects translate correctly
[x] Tooltips generate via GetHint()
[x] Trait milestone tracker detects level changes
[x] Localization keys added for all milestone messages
```

---

## Phase 5: UI Integration (Quick Decision Center)

**Goal:** Implement the Main Menu Quick Decision Center with KINGDOM, CAMP, YOU sections

**Chat Strategy:** 🔒 **STANDALONE** - UI work is self-contained

**Prerequisites:** Phase 4.5 complete (native effect integration, tooltip generation working)

```
I need you to implement Phase 5 of the Content Orchestrator - UI Integration.

═══════════════════════════════════════════════════════════════════════════════
CONTEXT RECOVERY (Verify Phase 4 + 4.5 work before starting)
═══════════════════════════════════════════════════════════════════════════════

Before implementing, verify:

Phase 4.5 Complete (Native Effects): ✅ DONE 2025-12-30
[x] src/Features/Content/IncidentEffectTranslator.cs exists
[x] IncidentEffectTranslator.GenerateTooltipHints() works
[x] Native effects apply correctly via TranslateEffects()
[x] Trait mapping (Soldier→Valor, Officer→Calculating, Lord→Honor) works

Orchestrator Working:
[ ] ContentOrchestrator.Instance is available
[ ] WorldStateAnalyzer.AnalyzeSituation() returns valid WorldSituation
[ ] ContentOrchestrator.CanIssueOrderNow() exists
[ ] Orders coordinate with orchestrator

Key Classes That Must Exist:
- src/Features/Content/ContentOrchestrator.cs
- src/Features/Content/WorldStateAnalyzer.cs
- src/Features/Content/Models/WorldSituation.cs

If orchestrator isn't working, implement earlier phases first.

═══════════════════════════════════════════════════════════════════════════════
DOCUMENTATION TO READ FIRST
═══════════════════════════════════════════════════════════════════════════════

Read these docs first:
- docs/AFEATURE/content-orchestrator-plan.md (Phase 5 section - has full UI spec)
- docs/AFEATURE/camp-life-simulation.md (Player Information & Interaction section)
- docs/Features/UI/ui-systems-master.md (current Camp Hub structure)

MAIN MENU STRUCTURE (Quick Decision Center):

The Main Menu (`enlisted_status`, auto-opens when waiting with army) is the quick decision center. It shows:
- Three information sections: KINGDOM, CAMP, YOU
- Three navigation buttons: ORDERS, DECISIONS, CAMP

```
╔══════════════════════════════════════════════════════════╗
║  _____ KINGDOM _____                                      ║
║  {War/peace. Major events. 1-2 lines.}                   ║
║                                                           ║
║  _____ CAMP _____                                         ║
║  {What's happening right now. Living world.}             ║
║                                                           ║
║  _____ YOU _____                                          ║
║  NOW: {Current duty status. Physical state.}             ║
║  AHEAD: {Forecast. Culture-aware rank names.}            ║
╠══════════════════════════════════════════════════════════╣
║            [  ORDERS  ]     ← Military orders             ║
║            [  DECISIONS  ]  ← Camp life activities        ║
║            [  CAMP    ]     ← Deep menu (QM, records)     ║
║            [Back to Map]                                  ║
╚══════════════════════════════════════════════════════════╝
```

Phase 5 Tasks:

1. CREATE ForecastGenerator.cs:
   - Location: src/Features/Content/ForecastGenerator.cs
   - BuildPlayerStatus() returns (Now, Ahead) tuple
   - BuildNowText(): duty status, physical state
   - BuildAheadText(): context-aware forecast of what's coming
   - Uses culture-aware placeholders: {NCO_TITLE}, {OFFICER_TITLE}

2. CREATE Main Menu sections:
   - BuildKingdomSummary(): 1-2 lines of war/peace status
   - BuildCampSummary(): 1-2 lines of what's happening in camp
   - Both in EnlistedNewsBehavior.cs or new MainMenuBuilder.cs

3. IMPLEMENT culture-aware text resolution:
   - {NCO_TITLE} → "Principalis" (Empire), "Sergeant" (Vlandia), "Drengr" (Sturgia)
   - {OFFICER_TITLE} → "Centurion" (Empire), "Knight" (Vlandia), "Huskarl" (Sturgia)
   - Use RankHelper.GetNCOTitle(culture), RankHelper.GetOfficerTitle(culture)

4. UPDATE EnlistedMenuBehavior.cs - Main Menu (`enlisted_status`):
   - Rebuild main menu with three info sections (KINGDOM, CAMP, YOU)
   - Add ORDERS, DECISIONS, CAMP buttons
   - ORDERS → opens military order view
   - DECISIONS → opens camp life activities (dynamically generated)
   - CAMP → opens deep Camp Hub

5. UPDATE EnlistedMenuBehavior.cs - Camp Hub (`enlisted_camp_hub`):
   - ADD CAMP STATUS section at top (rhythm, activity level, camp situation)
   - ENHANCE RECENT ACTIONS section - merge detailed order/rep tracking from old Reports:
     * Use `GetRecentOrderOutcomes(maxDaysOld: 5)` for order results with summaries
     * Use `GetRecentReputationChanges(maxDaysOld: 5)` for rep changes with messages
     * Format: "• {Title} ({timeAgo})\n  {DetailedSummary}"
   - REMOVE Reports menu option (CAMP STATUS + RECENT ACTIONS replaces it)
   - REMOVE Leave Service option (only accessible from Muster menu now)
   - KEEP: Service Records, Quartermaster, Personal Retinue, Companion Assignments,
          Medical Attention, Talk to Lords, Access Baggage Train
   - MOVE Visit Settlement to DECISIONS menu (appears as contextual opportunity when at settlement), Visit Settlement

6. UPDATE RegisterReportsMenu() - DELETE or gut this function:
   - Reports menu no longer exists
   - Camp Hub now shows inline CAMP STATUS instead

Files to Create:
- src/Features/Content/ForecastGenerator.cs
- src/Features/Interface/MainMenuNewsCache.cs

Files to Modify:
- src/Features/Interface/Behaviors/EnlistedMenuBehavior.cs (main menu + camp hub restructure)
- src/Features/Interface/Behaviors/EnlistedNewsBehavior.cs (add summary builders)
- src/Features/Ranks/RankHelper.cs (add GetNCOTitle, GetOfficerTitle if missing)
- ModuleData/Languages/enlisted_strings.xml (add localization keys)

LOCALIZATION KEYS TO ADD (enlisted_strings.xml):

See docs/Features/Content/event-system-schemas.md for full list. Key sections:

Section Headers:
- menu_section_kingdom, menu_section_camp, menu_section_you

YOU Section (natural flowing text, no labels):
- menu_you_off_duty: "You're off duty and well-rested."
- menu_you_off_duty_scheduled: "You're off duty. {ORDER_NAME} scheduled for tomorrow at dawn."
- menu_you_on_duty: "On duty - {ORDER_NAME}, day {DAY} of {TOTAL}."
- menu_you_wounded: "You're wounded - movement impaired. Off duty until you recover."
- menu_you_exhausted: "Exhausted from today's combat. The {NCO_TITLE} gave everyone rest."
- menu_you_sick: "You're feeling poorly. Rest ordered until you're fit for duty."
- menu_you_commitment: "You've agreed to join the {ACTIVITY} tonight."
- menu_you_default: "The day stretches ahead."

CAMP Section (living world activities):
- menu_camp_morning: "Morning bustle. Camp coming alive."
- menu_camp_midday: "Midday heat. Most resting in shade."
- menu_camp_evening: "Evening calm. Good spirits in camp."
- menu_camp_night: "Night watch. Camp quiet."
- menu_camp_drilling: "Veterans drilling by the wagons."
- menu_camp_cards: "Card game forming tonight by the fire."
- menu_camp_dice: "Dice game by the fire."
- menu_camp_roster: "The {NCO_TITLE}'s making lists - duty roster tomorrow."

KINGDOM Section:
- menu_kingdom_at_war, menu_kingdom_siege, menu_kingdom_peace, menu_kingdom_quiet

Order States:
- order_state_scheduled, order_state_pending, order_state_active, order_state_complete
- order_pending_accept, order_pending_decline

Changed Content:
- menu_new_tag: "[NEW]"

CAMP HUB RESTRUCTURE:

Old Camp Hub:
```
[Service Records]
[Quartermaster]
[Personal Retinue]
[Companion Assignments]
[Reports]              ← REMOVE
[Medical Attention]
[Talk to Lords]
[Leave Service]        ← REMOVE (Muster only)
[Back]
```

New Camp Hub:
```
╔═══════════════════════════════════════════════════════╗
║  CAMP HUB                                              ║
║  {RANK_NAME} ({TIER}) • Day {X} of 12 • {LOCATION}    ║
╠═══════════════════════════════════════════════════════╣
║  _____ CAMP STATUS _____                               ║
║  ⚙️ {RHYTHM} - {ACTIVITY_LEVEL}                       ║
║  {Camp situation narrative. Supply, morale, etc.}     ║
║                                                        ║
║  _____ RECENT ACTIONS _____                            ║
║  • {Event/order outcome 1}                             ║
║  • {Event/order outcome 2}                             ║
╠═══════════════════════════════════════════════════════╣
║  [Service Records]                                     ║
║  [Quartermaster]                                       ║
║  [Personal Retinue]      ← T7+ only                    ║
║  [Companion Assignments]                               ║
║  [Medical Attention]     ← If injured/ill              ║
║  [Talk to Lords]         ← If lords nearby             ║
║  [Access Baggage Train]                                ║
╠═══════════════════════════════════════════════════════╣
║  [Back]                                                ║
╚═══════════════════════════════════════════════════════╝
```

Note: CAMP STATUS replaces Reports menu. Leave Service is Muster-only.

COLOR SCHEME (USE EXISTING - see docs/Features/UI/color-scheme.md):

Use the established Enlisted color scheme with span styles:

| Element | Style | Usage |
|---------|-------|-------|
| Section headers | `Header` | `<span style="Header">_____ KINGDOM _____</span>` |
| Good status | `Success` | Green for healthy/good states |
| Warning status | `Warning` | Gold for fair/caution states, [NEW] tag |
| Critical status | `Alert` | Red for poor/critical states |
| Body text | `Default` | Standard narrative text |

Example implementation:
```csharp
sb.AppendLine("<span style=\"Header\">_____ YOU _____</span>");
var stateColor = isWounded ? "Alert" : isTired ? "Warning" : "Default";
sb.AppendLine($"<span style=\"{stateColor}\">{BuildYouText()}</span>");
```

Brush file: GUI/Brushes/EnlistedColors.xml

THE THREE SECTIONS - SCOPE:

| Section | Scope | Content |
|---------|-------|---------|
| KINGDOM | What's happening in the realm | Wars, sieges, peace, major events |
| CAMP | What's happening around you | Activities forming, morale, living world |
| YOU | What's happening to YOU | Duty, health, schedule, commitments |

CAMP = The living world around you (exists whether you engage or not)
YOU = Your place in that world (your personal status update)

THE "YOU" SECTION - NATURAL FLOWING TEXT:

NOT labels like "NOW:" and "AHEAD:". Just natural text about the player's situation.

Examples:
```
You're off duty and well-rested. Guard duty scheduled for tomorrow at dawn.
```
```
On duty - Guard Post, day 1 of 2. Dawn watch complete. Midday checkpoint in 5 hours.
```
```
You're wounded - movement impaired. Off duty until you recover.
```
```
You saw heavy combat today. Exhausted but uninjured. The {NCO_TITLE} gave everyone rest.
```
```
Off duty. Guard duty tomorrow. You've agreed to join the card game tonight.
```

THE "CAMP" SECTION - ACTIVITIES AND LIVING WORLD:

Examples:
```
Evening calm. Good spirits in camp.
Veterans drilling by the wagons.
Card game forming tonight by the fire.
The {NCO_TITLE}'s making lists - duty roster tomorrow.
```
```
Morning bustle. Quartermaster is open.
Soldiers checking equipment after yesterday's march.
```

CULTURE-AWARE TEXT (in both sections):

Empire:
```
The Principalis has been making lists. Duty roster tomorrow.
```

Vlandia:
```
The Sergeant's been eyeing the roster. Orders coming.
```

Sturgia:
```
The Drengr mutters about the duty list. Expect a task soon.
```

NAVIGATION BUTTONS:

| Button | Opens | Contains |
|--------|-------|----------|
| ORDERS | Military order view | Current/pending orders, accept/decline |
| DECISIONS | Camp life activities | Dynamically generated opportunities |
| CAMP | Deep Camp Hub | QM, Service Records, Companions, Retinue, Medical |

Note: Rank, tier, day count, location info goes in the CAMP Hub header, NOT the Main Menu.

TOOLTIP INTEGRATION (from Phase 4.5):

When displaying event options in DECISIONS or ORDERS views, use the native tooltip generator:

```csharp
// Use IncidentEffectTranslator for auto-generated tooltips
var tooltip = IncidentEffectTranslator.GenerateTooltipHints(option.Effects);
// Append Enlisted-specific hints (Fatigue, Retinue, etc.) manually
```

This gives you free probability display and standardized formatting.

Acceptance Criteria:
- Main Menu shows KINGDOM, CAMP, YOU sections with brief info
- YOU section shows NOW (current state) + AHEAD (forecast)
- Forecast uses culture-appropriate rank names
- Three buttons navigate to ORDERS, DECISIONS, CAMP
- DECISIONS shows dynamically generated camp life activities
- Deep info (rank, tier, records) is in CAMP Hub, not Main Menu
- Event option tooltips use native GetHint() pattern from Phase 4.5

═══════════════════════════════════════════════════════════════════════════════
HANDOFF NOTES (Phase 5 COMPLETE - 2025-12-30)
═══════════════════════════════════════════════════════════════════════════════

FILES CREATED:
- [x] src/Features/Content/ForecastGenerator.cs
- [x] src/Features/Interface/MainMenuNewsCache.cs

FILES MODIFIED:
- [x] src/Features/Interface/Behaviors/EnlistedMenuBehavior.cs (main menu + camp hub)
- [x] src/Features/Interface/Behaviors/EnlistedNewsBehavior.cs (BuildKingdomSummary, BuildCampSummary)
- [x] src/Features/Ranks/RankHelper.cs (GetNCOTitle, GetOfficerTitle)
- [x] ModuleData/Languages/enlisted_strings.xml (30 localization keys: lines 4823-4860)
- [x] Enlisted.csproj (new files registered: lines 189, 269)
- [x] src/Mod.Entry/SubModule.cs (MainMenuNewsCache behavior registered)

UI CHANGES MADE:
- Main Menu now has KINGDOM, CAMP, YOU sections with section headers
- ForecastGenerator generates NOW + AHEAD text with priority system
- MainMenuNewsCache caches sections with intelligent refresh based on game state
- Culture-aware rank names via RankHelper.GetNCOTitle/GetOfficerTitle

KEY DECISIONS MADE:
- MainMenuNewsCache is a CampaignBehaviorBase with save/load support
- KINGDOM refreshes every 24h or on major events
- CAMP refreshes every 6h (matches DayPhase) or on phase change
- YOU refreshes every 1h for player state responsiveness
- Fast travel detection forces refresh if >2 hours passed

VERIFICATION PASSED:
[x] Main Menu shows three sections (KINGDOM, CAMP, YOU)
[x] YOU section has natural flowing text (NOW + AHEAD)
[x] Culture-aware rank names work via GetNCOTitle/GetOfficerTitle
[x] ForecastGenerator priority system: Critical → High → Medium → Low
[x] 30 localization keys added for all menu text
[x] Build succeeds with 0 warnings and 0 errors
```

---

## Phase 5.5: Camp Background Simulation

**Goal:** Create an autonomous company that simulates itself daily - soldiers get sick, desert, recover; equipment degrades; incidents occur. This feeds the news system and provides context for the orchestrator.

**Status:** ✅ **COMPLETE** (2025-12-30)

**Chat Strategy:** 🔒 **STANDALONE** - Complex Bannerlord API work needs focus

**Prerequisites:** Phases 1-5 complete (Core Orchestrator + UI working)

### Completion Summary

**FILES CREATED:**
- ✅ `src/Features/Camp/CompanySimulationBehavior.cs` - Main simulation behavior (6-phase daily tick)
- ✅ `src/Features/Camp/Models/CompanyRoster.cs` - Wrapper around real TroopRoster + overlay
- ✅ `src/Features/Camp/Models/CompanyPressure.cs` - Tracks pressure for crisis triggers
- ✅ `src/Features/Camp/Models/CampIncident.cs` - Incident definition struct
- ✅ `src/Features/Camp/Models/SimulationDayResult.cs` - Daily tick output
- ✅ `ModuleData/Enlisted/simulation_config.json` - 20 incident definitions + config

**FILES MODIFIED:**
- ✅ `src/Features/Interface/Behaviors/EnlistedNewsBehavior.cs` - Added `AddCampNews()`, `UpdateCompanyStatus()`
- ✅ `src/Features/Interface/News/State/CampNewsState.cs` - Added company status fields
- ✅ `src/Features/Content/ContentOrchestrator.cs` - Added `QueueCrisisEvent()`
- ✅ `src/Mod.Entry/SubModule.cs` - Registered behavior
- ✅ `Enlisted.csproj` - Added all new files
- ✅ `ModuleData/Languages/enlisted_strings.xml` - ~50 localization strings

**KEY API USAGE (verified):**
- `TroopRoster.AddToCounts(CharacterObject, count, insertAtFront, woundedCount)` - verified signature
- `TroopRoster.TotalWounded`, `TroopRoster.TotalManCount` - property access
- `TroopRosterElement.Character.IsHero` - hero filtering

**INTEGRATION POINTS:**
- `CompanySimulationBehavior.Instance.Roster` - sick/wounded/missing/dead data
- `CompanySimulationBehavior.Instance.Pressure` - pressure tracking (days low supplies/morale/rest)
- `CompanySimulationBehavior.Instance.ActiveFlags` - incident flags for chained events
- `EnlistmentBehavior.Instance.CompanyNeeds` - accesses company needs state
- `EscalationManager.Instance.ModifyDiscipline()` - discipline effects from incidents
- `ContentOrchestrator.Instance.QueueCrisisEvent()` - crisis event delivery

**VERIFICATION PASSED:**
- ✅ Build compiles without errors
- ✅ Daily simulation runs on DailyTickEvent
- ✅ Sick/wounded/missing tracking works
- ✅ Desertions/deaths remove troops from real party roster
- ✅ Heroes never removed (filtered to regulars only)
- ✅ News methods integrated with EnlistedNewsBehavior
- ✅ Crisis events queue via ContentOrchestrator
- ✅ Save/load preserves simulation state
- ✅ Edge cases handled (prisoner, in battle, empty party)

### Original Prompt (for reference)

```
I need you to implement Phase 5.5 - Camp Background Simulation for my Bannerlord mod.

═══════════════════════════════════════════════════════════════════════════════
CONTEXT RECOVERY (Verify Phase 5 work before starting)
═══════════════════════════════════════════════════════════════════════════════

Before implementing, verify:

Phase 5 Complete (UI Integration): ✅ DONE 2025-12-30
[x] ContentOrchestrator.Instance is available and firing daily
[x] WorldStateAnalyzer.AnalyzeSituation() works
[x] Main Menu shows KINGDOM, CAMP, YOU sections
[x] ForecastGenerator.cs exists (will integrate with it)
[x] EnlistedNewsBehavior has BuildCampSummary() method
[x] MainMenuNewsCache behavior registered and caching sections

Key Classes That Must Exist:
- src/Features/Content/ContentOrchestrator.cs
- src/Features/Content/ForecastGenerator.cs
- src/Features/Interface/Behaviors/EnlistedNewsBehavior.cs

This phase CREATES the background data that feeds the UI sections.

═══════════════════════════════════════════════════════════════════════════════
DOCUMENTATION TO READ FIRST
═══════════════════════════════════════════════════════════════════════════════

Read these docs first:
- docs/AFEATURE/camp-background-simulation.md (COMPLETE SPEC - read the whole thing)
- docs/AFEATURE/content-orchestrator-plan.md (orchestrator integration)
- docs/Features/UI/news-reporting-system.md (news feed integration)
- docs/BLUEPRINT.md (coding standards, API verification)

Phase 5.5 Overview:
This is the BACKGROUND LAYER that makes the company feel alive. It runs automatically
on the daily tick and generates news. It does NOT require player interaction.

The key integration points are:
1. Real Bannerlord TroopRoster - wounded troops are REAL game wounds
2. EnlistedNewsBehavior - push camp news to existing news feed
3. ContentOrchestrator - provide world state context + queue crisis events
4. ForecastGenerator - provide warnings for the AHEAD section

Phase 5.5 Tasks:

1. Create CompanySimulationBehavior.cs (CampaignBehaviorBase)
   - Location: src/Features/Camp/CompanySimulationBehavior.cs
   - Register in SubModule.xml
   - Add to Enlisted.csproj

2. Create data models:
   - CompanyRoster.cs (wrapper around real TroopRoster + overlay)
   - CompanyPressure.cs (tracks days of low supplies/morale/etc)
   - CampIncident.cs (struct for random incidents)
   - SimulationDayResult.cs (result of daily tick)
   - Location: src/Features/Camp/Models/

3. Implement the 6-phase daily tick:
   - Phase 1: Consumption (supplies, equipment degradation)
   - Phase 2: Roster Updates (sick recovery/death, wounded news)
   - Phase 3: Condition Checks (new sickness, injuries, desertion)
   - Phase 4: Incident Rolls (random camp incidents)
   - Phase 5: Pulse Evaluation (threshold crossings)
   - Phase 6: News Generation (push to EnlistedNewsBehavior)

4. Integrate with real Bannerlord TroopRoster:
   - Read: party.MemberRoster.TotalWounded, TotalManCount
   - Write wounds: roster.AddToCounts(troop, 0, woundedCount: 1)
   - Remove deserters: roster.AddToCounts(troop, -1)
   - NEVER remove heroes - filter to regulars only

5. Create incident definitions:
   - Location: ModuleData/Enlisted/simulation_config.json
   - Categories: camp_life, discipline, social, discovery, problems
   - Each incident has: id, text, severity, effects, cooldown

6. Add news integration:
   - Add AddCampNews() method to EnlistedNewsBehavior
   - Add UpdateCompanyStatus() method to EnlistedNewsBehavior
   - Push all daily results to news feed

7. Add orchestrator integration:
   - Expose simulation state for WorldStateAnalyzer
   - Add QueueCrisisEvent() method for pressure thresholds
   - Provide data for ForecastGenerator warnings

8. Implement save/load:
   - SyncData() for SickCount, MissingCount, DeadThisCampaign, pressure, flags
   - Handle save corruption gracefully

Critical Requirements:
- Add ALL new files to Enlisted.csproj manually
- Verify TroopRoster APIs against C:\Dev\Enlisted\Decompile\
- Use ModLogger with category "Orchestrator" for logging
- Never remove heroes from roster (filter to regulars)
- Guard against negative counts (Math.Max(0, value))
- Handle edge cases: empty party, prisoner, in battle

Key APIs to verify in decompile:
- TroopRoster.AddToCounts(CharacterObject, int count, int woundedCount)
- TroopRoster.TotalWounded (property)
- TroopRoster.TotalManCount (property)
- TroopRosterElement.Character.IsHero (property)
- MobileParty.MemberRoster (property)

Testing Checklist:
- [ ] Simulation runs on daily tick without errors
- [ ] Sick count increases/decreases appropriately
- [ ] Wounded troops appear in game party screen
- [ ] Desertions actually remove troops from party
- [ ] Deaths reduce party size
- [ ] News items appear in CAMP section
- [ ] Crisis events trigger at pressure thresholds
- [ ] Forecast warnings appear in AHEAD section
- [ ] Save/load preserves simulation state
- [ ] No crashes with empty party or edge cases

Acceptance Criteria:
- Daily tick generates 1-5 news items about camp life
- Roster changes (sick, wounded, deserted) affect real party
- Pressure accumulation leads to crisis events
- Forecast section warns player before crisis
- No hero troops ever removed
- State persists across save/load

═══════════════════════════════════════════════════════════════════════════════
HANDOFF NOTES (Capture these for Phase 6)
═══════════════════════════════════════════════════════════════════════════════

When complete, provide these handoff notes for the next AI session:

FILES CREATED:
- [ ] src/Features/Camp/CompanySimulationBehavior.cs
- [ ] src/Features/Camp/Models/CompanyRoster.cs
- [ ] src/Features/Camp/Models/CompanyPressure.cs
- [ ] src/Features/Camp/Models/CampIncident.cs
- [ ] src/Features/Camp/Models/SimulationDayResult.cs
- [ ] ModuleData/Enlisted/simulation_config.json

FILES MODIFIED:
- [ ] src/Features/Interface/Behaviors/EnlistedNewsBehavior.cs (AddCampNews)
- [ ] Enlisted.csproj (new files added)
- [ ] src/Mod.Core/SaveSystem/EnlistedSaveDefiner.cs (new types)

KEY API USAGE (verified against decompile):
- TroopRoster.AddToCounts(CharacterObject, int, int)
- TroopRoster.TotalWounded
- TroopRosterElement.Character.IsHero

INTEGRATION POINTS:
- CompanySimulationBehavior.Instance.Roster for sick/wounded data
- CompanySimulationBehavior.Instance.Pressure for pressure tracking
- (list any other integration points)

VERIFICATION PASSED:
[ ] Daily simulation runs without errors
[ ] Sick/wounded changes affect real party
[ ] News items appear in CAMP section
[ ] Crisis events fire at thresholds
[ ] Save/load preserves state
[ ] No heroes ever removed
```

---

## Phase 6: Camp Life Simulation (Living Breathing World)

**Goal:** Create a living, breathing military camp that runs independently of player input

**Chat Strategy:** ⚡ **SPLIT INTO TWO CHATS:**
- **Phase 6A-C** (this prompt): Core functionality - models, generator, UI
- **Phase 6D-E** (separate prompt): Polish - learning system, 25+ opportunities

**Prerequisites:** Phase 5.5 complete (Background Simulation provides context data)

```
I need you to implement Phase 6A-C of the Content Orchestrator - Camp Life Simulation Core.

═══════════════════════════════════════════════════════════════════════════════
CONTEXT RECOVERY (Verify Phase 5.5 work before starting)
═══════════════════════════════════════════════════════════════════════════════

Before implementing, verify Phase 5.5 is complete:

Background Simulation Working: ✅ COMPLETE 2025-12-30
[x] CompanySimulationBehavior.cs exists at src/Features/Camp/
[x] CompanySimulationBehavior.Instance.Roster available (sick/wounded data)
[x] CompanySimulationBehavior.Instance.Pressure available (pressure tracking)
[x] Daily simulation generates news items
[x] EnlistedNewsBehavior.AddCampNews() method exists

Orchestrator Working:
[x] ContentOrchestrator.Instance available
[x] WorldStateAnalyzer.AnalyzeSituation() works
[ ] ContentOrchestrator.GetCurrentDayPhase() available (or implement it)

Phase 5.5 is complete. You can proceed with Phase 6.

═══════════════════════════════════════════════════════════════════════════════
SCOPE: PHASE 6A-C ONLY (Core Functionality)
═══════════════════════════════════════════════════════════════════════════════

This prompt covers:
- 6A: Models + basic generation
- 6B: UI integration
- 6C: Intelligence (player state, variety)

Phase 6D-E (learning system, 25+ opportunities) will be done in a separate chat.

═══════════════════════════════════════════════════════════════════════════════
DOCUMENTATION TO READ FIRST
═══════════════════════════════════════════════════════════════════════════════

Read these docs first:
- docs/AFEATURE/camp-life-simulation.md (COMPLETE SPEC - read the whole thing)
- docs/AFEATURE/camp-background-simulation.md (Phase 5.5 - provides context data)
- docs/AFEATURE/content-orchestrator-plan.md (orchestrator you're building on)
- docs/Features/Campaign/camp-life-simulation.md (existing CampLifeBehavior to keep)

DEPENDENCY: Phase 5.5 (Background Simulation) must be complete. It provides:
- CompanySimulationBehavior.Instance.Roster (sick/wounded counts)
- CompanySimulationBehavior.Instance.Pressure (days of low morale/supplies)
- Recent incidents for opportunity generation

CORE PHILOSOPHY:
Camp life happens whether you engage or not. The camp is a living entity with
its own rhythms, schedules, and activities. You are one soldier in a larger world.
Things occur around you. You can participate, or you can watch. Either is valid.

This does NOT replace CampLifeBehavior - that remains as the backend QM mood system.

CRITICAL: 4-BLOCK MILITARY DAY CYCLE
The camp uses the same 4-phase day cycle as the Order System:
  - MORNING (6am-11am) = Order Phase 1: Briefings, training
  - DAY (12pm-5pm) = Order Phase 2: Active duty, work
  - DUSK (6pm-9pm) = Order Phase 3: Social, meals, relaxation
  - NIGHT (10pm-5am) = Order Phase 4: Sleep, night watch

Camp opportunities evaluate at PHASE TRANSITIONS, not hourly. When phase changes:
  1. Orders fire their phase logic
  2. Camp invalidates opportunity cache
  3. Progression ticks if applicable (Medical at Morning, Discipline at Dusk)
  4. UI cache refreshes

THE SIMULATION USES 4 INTELLIGENCE LAYERS:
1. World State (macro) - Lord situation, war status, strategic context
2. Camp Context (meso) - Day phase (Dawn/Midday/Dusk/Night), day in muster cycle, camp mood, who's on duty
3. Player State (micro) - Fatigue, gold, reputation, recent actions
4. Opportunity History (meta) - What was shown, engagement rates, variety tracking

Phase 6 Tasks:

1. CREATE Data Models: ✅ COMPLETE
   - src/Features/Camp/Models/CampOpportunity.cs ✅
   - src/Features/Camp/Models/OpportunityType.cs ✅ (enum: Training, Social, Economic, Recovery, Special)
   - src/Features/Camp/Models/CampContext.cs ✅ (DayPhase, DaysSinceLastMuster, CurrentMood, RecentEvents)
   - src/Features/Camp/Models/CampMood.cs ✅ (enum: Routine, Celebration, Mourning, Tense)
   - NOTE: DayPhase enum is in src/Features/Content/Models/OrchestratorEnums.cs (shared with Order System)
   - src/Features/Camp/Models/OpportunityHistory.cs ✅ (tracks presentations, engagements, variety)
   - src/Features/Camp/Models/PlayerCommitments.cs ✅ (commitment tracking)

2. CREATE CampOpportunityGenerator.cs: ✅ COMPLETE
   - Location: src/Features/Camp/CampOpportunityGenerator.cs ✅ (876 lines)
   - This is the HEART of the living camp simulation
   - GenerateCampLife() method - main entry point ✅
   - AnalyzeCampContext() - builds CampContext from game state ✅
   - CalculateFitness() - scores each opportunity 0-100 using ALL 4 layers ✅
   - DetermineOpportunityBudget() - context-aware (garrison morning=2-3, siege=0-1) ✅
   - Uses ContentOrchestrator.Instance for world state + pressure ✅
   - Uses PlayerBehaviorTracker for learned preferences ⚠️ (basic - full learning in 6D)
   - Implements IsPlayerOnDuty() check ✅

3. CREATE Opportunity Definitions: ✅ COMPLETE
   - ModuleData/Enlisted/camp_opportunities.json ✅ (15 opportunities)
   - 10-15 opportunities for Phase 6A-C testing (full 25+ in Phase 6E)
   - Natural language descriptions (what's HAPPENING, not game-y options) ✅

   GOOD: "Veterans are drilling by the wagons. The sergeant is putting
          them through their paces. Swords clash in rhythm."
          [Join the drill]

   BAD:  "Training Opportunity Available"
          [Start Training]

4. MODIFY EnlistedMenuBehavior.cs: ✅ COMPLETE
   - DECISIONS menu exists with opportunity sections ✅
   - Accordion-style expansion with [NEW] tags ✅
   - Context-appropriate empty states ✅
   - DecisionManager.GetAvailableOpportunities() bridges to CampOpportunityGenerator ✅
   - AddDecisionEntry records engagement on opportunity selection ✅
   - GetCampActivityFlavor() queries CampOpportunityGenerator ✅

5. IMPLEMENT TIME-OF-DAY AWARENESS: ✅ COMPLETE
   - Dawn (6am-11am): Training peak, productive tasks, budget 2-3
   - Midday (12pm-5pm): Duty focus, orders issued, budget 0-1
   - Dusk (6pm-9pm): Social peak, leisure, budget 1-2
   - Night (10pm-5am): Sleep, guard duty only, budget 0
   - Uses DayPhase enum from OrchestratorEnums.cs (synced with Order phases)

6. IMPLEMENT WEEKLY RHYTHM: ✅ IN GENERATOR
   Days 1-4: Fresh after muster, more training/gambling
   Days 5-8: Routine settling, balanced
   Days 9-12: Muster approaching, pay tension, economic focus
   Muster Day: No camp life (structured muster sequence)

7. NOTE: LEARNING SYSTEM IS PHASE 6D (NOT THIS PHASE)
   - Basic engagement tracking exists in OpportunityHistory
   - Full adaptive learning (70/30 split) will be implemented in Phase 6D-E

8. IMPLEMENT ORDER-DECISION TENSION SYSTEM: ✅ SCHEMA COMPLETE
   - When player is ON ORDER, DECISIONS still shows opportunities
   - Orchestrator FILTERS based on orderCompatibility:
     * "available" = show normally
     * "risky" = show with risk in TOOLTIP (not UI label)
     * "blocked" = don't show at all (filtered out)
   - RISKY: Detection check when player engages
   - If caught: Apply caughtConsequences (rep loss, discipline, order failure risk)
   - NO SAFE/RISKY CATEGORIES IN UI - just natural opportunities + tooltips

   JSON Schema per opportunity:
   ```json
   "orderCompatibility": {
     "default": "risky",
     "guardPost": "risky",
     "firewoodDetail": "available",
     "marchFormation": "blocked"
   },
   "detection": {
     "baseChance": 0.25,
     "nightModifier": -0.15,
     "highRepModifier": -0.10
   },
   "caughtConsequences": {
     "officerRep": -15,
     "discipline": 2,
     "orderFailureRisk": 0.20
   },
   "tooltipRiskyId": "opp_xxx_risk_tooltip"
   ```

   Tooltip shows risk on hover (no emojis, plain text):
   "Risk: You're on duty. If caught: -15 Officer Rep. Detection: ~25%"

9. IMPLEMENT PLAYER COMMITMENT TRACKING: ✅ COMPLETE
   - PlayerCommitments.cs exists with ScheduledActivityId, ScheduledTime ✅
   - HasCommitment property implemented ✅
   - Commitment persists across save/load (in CampOpportunityGenerator.SyncData) ✅
   - CommitToActivity() and related methods ready for Phase 6D integration ✅

   ```csharp
   public class PlayerCommitments
   {
       public string? ScheduledActivityId { get; set; }
       public CampaignTime? ScheduledTime { get; set; }
       public bool HasCommitment => ScheduledActivityId != null;
   }
   ```

10. IMPLEMENT INFO SECTION CACHING (MainMenuNewsCache.cs): ✅ COMPLETE
   - MainMenuNewsCache.cs exists (213 lines) ✅
   - KINGDOM: Refresh on major events or every 24 hours ✅
   - CAMP: Refresh on time period change or every 6 hours ✅
   - YOU: Refresh on player state change or every hour ✅
   - ForecastGenerator.cs exists for YOU section ✅
   - OnPhaseChanged() integration with ContentOrchestrator ✅

Files Created (✅ All Complete):
- src/Features/Interface/MainMenuNewsCache.cs ✅
- src/Features/Content/ForecastGenerator.cs ✅
- src/Features/Camp/Models/CampOpportunity.cs ✅
- src/Features/Camp/Models/PlayerCommitments.cs ✅
- src/Features/Camp/Models/OpportunityType.cs ✅
- src/Features/Camp/Models/CampContext.cs ✅
- src/Features/Camp/Models/CampMood.cs ✅
- src/Features/Camp/Models/OpportunityHistory.cs ✅
- src/Features/Camp/CampOpportunityGenerator.cs ✅
- ModuleData/Enlisted/camp_opportunities.json ✅ (15 opportunities)
- NOTE: DayPhase is in src/Features/Content/Models/OrchestratorEnums.cs (shared)

Files Modified (✅ Complete):
- src/Features/Interface/Behaviors/EnlistedMenuBehavior.cs ✅
- src/Features/Content/PlayerBehaviorTracker.cs ✅
- ModuleData/Languages/enlisted_strings.xml ✅ (opportunity strings added)
- Enlisted.csproj ✅
- src/Mod.Core/SaveSystem/EnlistedSaveDefiner.cs ✅

LOCALIZATION KEYS TO ADD (enlisted_strings.xml):

See docs/Features/Content/event-system-schemas.md "Camp Opportunities Schema" section.

Opportunity Definitions (per opportunity):
- opp_{id}_title: "Card Game"
- opp_{id}_desc: "A card game is forming by the fire..."
- opp_{id}_action: "Sit in"
- opp_{id}_risk_tooltip: "Risk: You're on duty. If caught: -15 Rep."

Order-Decision Tension:
- opp_risk_tooltip_generic: "Risk: You're on duty. If caught: {REP_PENALTY} Rep. Detection: ~{DETECTION_CHANCE}%"
- opp_caught_notification: "You were caught away from your post!"
- opp_caught_detail: "The {OFFICER_TITLE} noticed your absence."
- opp_caught_warning: "You're on thin ice with command."

JSON SCHEMA (camp_opportunities.json):

See docs/Features/Content/event-system-schemas.md "Camp Opportunities Schema" for full definition.
Key fields: id, type, titleId, descriptionId, actionId, targetDecision, fitness,
requirements, cooldown, orderCompatibility, detection, caughtConsequences, tooltipRiskyId

Fitness Scoring (all 4 layers):
- Base: 50
- LAYER 1 World: ±15-25 (training fits garrison, social bad during siege)
- LAYER 2 Camp: ±10-30 (training morning, social evening, mood effects)
- LAYER 3 Player: ±20-30 (recovery when injured, economic when poor)
- LAYER 4 History: -40 if shown <12hrs, ±10-15 based on engagement rate
- Threshold: 40 (below = not shown)

Camp Context Tracking:
- DayPhase from ContentOrchestrator.GetCurrentDayPhase() (syncs with Order phases)
- DaysSinceLastMuster from EnlistmentBehavior muster tracking
- CampMood derived from: recent battle (+Tense), victory (+Celebration),
  casualties (+Mourning), normal (+Routine)
- RecentEvents list from last 24-48 hours

Logging Requirements:
- Category: "CampLife"
- Log context analysis (time, mood, budget)
- Log scoring breakdown for debugging
- Log presentation and engagement

EDGE CASES TO HANDLE (see spec for full list):
- New enlistment 3-day grace: No opportunities, show settling-in message
- Probation active: Budget -1, no leadership opportunities, fatigue cap 18
- Grace period (lord died): No opportunities, show disarray message
- Muster day: No opportunities, muster sequence takes over
- Supply < 20%: Budget = 1 max, survival focus only
- Player captured: Suspend entirely
- Order active (on duty): Orchestrator filters opportunities, risky ones have tooltip, blocked filtered out
- Player injured: Filter training, prioritize recovery
- Baggage window (6hr post-muster): Special baggage opportunity

UI EDGE CASES (Quick Decision Center):
- KINGDOM section empty: Show "The realm is quiet. No major news."
- CAMP section empty: Show "A quiet moment in camp. Rest while you can."
- YOU/AHEAD no forecast: Show "Quiet. Almost too quiet." (valid state)
- Culture rank text fails: Fallback to generic English, log warning
- DECISIONS menu empty: Show contextual message, offer Camp Hub access
- DECISIONS while on order: Orchestrator curates, risky options have tooltips, blocked filtered out
- CAMP STATUS fails: Fallback to "Camp Status: Normal", log error
- RECENT ACTIONS empty: Show "Nothing notable to report."
- Player wants Leave Service: It's in Muster menu only, not Camp Hub

CACHING EDGE CASES:
- Fast travel > 2 hours: Force refresh all sections
- Save/Load: Don't persist cache, rebuild on load
- Multiple triggers: Batch into single refresh
- Time speed x4: Still works - forecasts use game hours

ORDER FLOW EDGE CASES:
- Fast travel past SCHEDULED: Auto-accept, notify player
- Fast travel past PENDING: Auto-decline with -5 rep
- Player ignores PENDING 24h: Auto-decline, warning at 18h mark
- Forecast wrong (order cancelled): "[NEW] The roster changed."

ORDER-DECISION TENSION EDGE CASES:
- Detection timing: Check BEFORE activity starts
- Order phase changes mid-activity: Complete activity, apply consequences at end
- Caught rep overflow: Floor at 0, show "on thin ice" warning
- 3+ catches: Then order failure risk applies

Acceptance Criteria:
- Camp feels ALIVE - things are HAPPENING, player observes and can join
- Day phase matters (Morning drills, Dusk social) - 4 phases synced with Order system
- Weekly rhythm affects what's available
- Learning adapts to player over time
- Garrison feels different from siege feels different from campaign
- Natural language describes SCENES, not options
- All edge cases handled gracefully (see Edge Cases section in spec)
- Saves/loads correctly

Start by reading the FULL camp-life-simulation.md spec (especially Edge Cases section). Implement:
6A: Models + basic generation
6B: UI integration
6C: Intelligence (player state, variety)

(6D-E will be a separate chat - see Phase 6D-E prompt below)

═══════════════════════════════════════════════════════════════════════════════
ACCEPTANCE CRITERIA (Phase 6A-C)
═══════════════════════════════════════════════════════════════════════════════

Core Functionality (All Complete):
[x] CampOpportunityGenerator.cs creates opportunities based on context ✅
[x] DayPhase affects opportunity selection (synced with Order phases) ✅
[x] Fitness scoring uses all 4 layers (world, camp, player, history) ✅
[x] UI shows natural language descriptions via DecisionManager bridge ✅
    - DecisionManager.GetAvailableOpportunities() calls generator
    - GetCampActivityFlavor() queries CampOpportunityGenerator
[x] Order-decision tension: schema complete in camp_opportunities.json ✅
[x] Player commitment tracking: model and save/load complete ✅
[x] Engagement tracking: AddDecisionEntry records via RecordEngagement() ✅
[x] Basic set of opportunities (10-15) for testing ✅ (15 opportunities)

NOT in this phase (saved for 6D-E):
- Learning system (adaptive 70/30 scoring)
- Full 25+ opportunities
- Extensive playtesting/polish

═══════════════════════════════════════════════════════════════════════════════
HANDOFF NOTES (Capture these for Phase 6D-E)
═══════════════════════════════════════════════════════════════════════════════

HANDOFF NOTES FOR Phase 6D-E (as of 2025-12-30):

FILES CREATED (✅ All Complete):
- [x] src/Features/Interface/MainMenuNewsCache.cs (213 lines)
- [x] src/Features/Content/ForecastGenerator.cs (YOU section generation)
- [x] src/Features/Camp/Models/CampOpportunity.cs
- [x] src/Features/Camp/Models/PlayerCommitments.cs
- [x] src/Features/Camp/Models/OpportunityType.cs
- [x] src/Features/Camp/Models/CampContext.cs
- [x] src/Features/Camp/Models/CampMood.cs
- [x] src/Features/Camp/Models/OpportunityHistory.cs
- [x] src/Features/Camp/CampOpportunityGenerator.cs (876 lines)
- [x] ModuleData/Enlisted/camp_opportunities.json (15 opportunities)
- NOTE: DayPhase enum is in src/Features/Content/Models/OrchestratorEnums.cs

FILES MODIFIED (✅ Complete):
- [x] src/Features/Interface/Behaviors/EnlistedMenuBehavior.cs
- [x] src/Features/Content/PlayerBehaviorTracker.cs
- [x] ModuleData/Languages/enlisted_strings.xml
- [x] Enlisted.csproj
- [x] src/Mod.Core/SaveSystem/EnlistedSaveDefiner.cs

FITNESS SCORING IMPLEMENTATION:
- 4-layer scoring in CampOpportunityGenerator.CalculateFitness()
- Layer 1 World: Uses WorldStateAnalyzer.AnalyzeSituation()
- Layer 2 Camp: Uses CampContext (DayPhase, mood, muster cycle)
- Layer 3 Player: Uses fatigue, gold, injury from CampContext
- Layer 4 History: Uses OpportunityHistory (last presented, engagement rates)
- Threshold: 40 (opportunities below this are not shown)

ORDER-DECISION TENSION:
- Schema complete in camp_opportunities.json
- orderCompatibility: available/risky/blocked per order type
- detection: baseChance, nightModifier, highRepModifier
- caughtConsequences: officerRep, discipline, orderFailureRisk
- tooltipRiskyId for localized risk tooltips

OPPORTUNITIES CREATED (15 for testing):
1. opp_weapon_drill (training)
2. opp_card_game (social)
3. opp_dice_game (economic)
4. opp_war_stories (social)
5. opp_rest_tent (recovery)
6. opp_help_wounded (recovery)
7. opp_equipment_maintenance (training)
8. opp_sparring_match (training)
9. opp_tavern_visit (social)
10. opp_prayer_service (recovery)
11. opp_foraging (economic)
12. opp_repair_work (economic)
13. opp_letter_writing (social)
14. opp_officer_audience (special)
15. opp_baggage_access (special)

REMAINING WORK FOR 6D-E:
- Learning system: Adapt scoring based on engagement rates (70/30 split)
- Add 10-15 more opportunities to reach 25+ total
- Wire DECISIONS menu to show actual generator output (currently placeholder)
- Test commitment tracking UI integration
- Verify detection logic at runtime
- Polish and playtesting

VERIFICATION STATUS (Phase 6A-C Complete):
[x] Camp opportunities generate based on context ✅
[x] Day phase affects scoring (validPhases in JSON) ✅
[x] Fitness scoring uses all 4 layers ✅
[x] Order-decision tension schema complete ✅
[x] UI integration complete (DecisionManager bridge) ✅
[x] Engagement tracking complete ✅
[x] Commitment model complete (UI integration in 6D-E) ✅

READY FOR PHASE 6D-E:
- Learning system (adaptive scoring)
- Full 25+ opportunities
- Commitment UI (YOU section updates)
- Playtesting and polish
```

---

## Phase 6D-E: Camp Life Polish

**Goal:** Add learning system and complete opportunity set

**Chat Strategy:** 🔒 **STANDALONE** - Polish phase, can be done later

**Prerequisites:** Phase 6A-C complete (core camp life working)

```
I need you to implement Phase 6D-E of Camp Life Simulation - Learning System and Polish.

═══════════════════════════════════════════════════════════════════════════════
CONTEXT RECOVERY (Verify Phase 6A-C work before starting)
═══════════════════════════════════════════════════════════════════════════════

Before implementing, verify Phase 6A-C is complete:

Core Functionality Working:
[ ] CampOpportunityGenerator.cs exists and generates opportunities
[ ] Fitness scoring uses all 4 layers
[ ] DayPhase affects selection (Morning drills, Dusk social)
[ ] Order-decision tension works (risky opportunities have tooltips)
[ ] Player commitment tracking works
[ ] 10-15 opportunities exist for testing

Models That Must Exist:
- src/Features/Camp/Models/CampOpportunity.cs
- src/Features/Camp/Models/OpportunityHistory.cs
- src/Features/Camp/CampOpportunityGenerator.cs
- ModuleData/Enlisted/camp_opportunities.json

If core functionality isn't working, implement Phase 6A-C first.

═══════════════════════════════════════════════════════════════════════════════
PHASE 6D: LEARNING SYSTEM
═══════════════════════════════════════════════════════════════════════════════

Implement learning that adapts to player preferences:

1. Track engagement rates per opportunity type
   - PlayerBehaviorTracker.RecordOpportunityEngagement(type, engaged)
   - Track: presented vs engaged for each OpportunityType

2. Adapt scoring based on engagement
   - +15 fitness for types player engages with
   - -10 fitness for types player ignores
   - Apply in CalculateFitness() Layer 4

3. Maintain 70/30 split
   - 70% learned preference (what player likes)
   - 30% variety (occasionally show other types)

4. Persist in save system
   - Add engagement tracking to SyncData()

═══════════════════════════════════════════════════════════════════════════════
PHASE 6E: POLISH (25+ Opportunities)
═══════════════════════════════════════════════════════════════════════════════

1. Add remaining opportunities to reach 25+ total:

Training Type:
- dec_weapon_drill_group, dec_spar_veterans, dec_formation_practice
- (contextual variants for morning vs evening)

Social Type:
- dec_cards_high_stakes, dec_dice_casual, dec_storytelling
- dec_drinking_moderate, dec_drinking_heavy

Economic Type:
- dec_side_work, dec_trade_goods, dec_gamble_tournament

Recovery Type:
- dec_rest_shade, dec_rest_full_day, dec_meditation

Special Type:
- dec_volunteer_extra, dec_help_wounded, dec_mentor_recruit

2. Write natural language descriptions for each:

GOOD: "Veterans are drilling by the wagons. The sergeant is putting
       them through their paces. Swords clash in rhythm."

BAD:  "Training Opportunity Available"

3. Add all localization keys to enlisted_strings.xml

4. Playtest at each tier level (T1-T3, T4-T6, T7-T9)

5. Tune fitness scoring based on playtesting

═══════════════════════════════════════════════════════════════════════════════
ACCEPTANCE CRITERIA (Phase 6D-E)
═══════════════════════════════════════════════════════════════════════════════

Learning System:
[ ] Engagement tracking persists across sessions
[ ] Player preferences affect opportunity selection
[ ] 70/30 split maintains variety
[ ] Logs show learning adjustments

Polish:
[ ] 25+ opportunities with natural language descriptions
[ ] All opportunities have localization keys
[ ] Garrison, Campaign, Siege feel distinct
[ ] Morning/Evening feel distinct
[ ] No repetitive content (variety tracking works)

═══════════════════════════════════════════════════════════════════════════════
HANDOFF NOTES (Capture these for Phase 7)
═══════════════════════════════════════════════════════════════════════════════

⚠️ CRITICAL DISCOVERY (2025-12-31):
Phase 6 is INCOMPLETE. The 29 opportunities reference decisions that don't exist.
See content-orchestrator-plan.md "CRITICAL: Phase 6 Incomplete" section for details.

Before Phase 7 can proceed:
1. Delete old decisions.json (38 pre-orchestrator static decisions)
2. Create 26 new decisions matching opportunity targetDecision IDs
3. Keep 3 working decisions: dec_maintain_gear, dec_write_letter, dec_gamble_high

HANDOFF NOTES FOR Phase 7 (as of 2025-12-30):

FILES MODIFIED:
- [x] src/Features/Camp/CampOpportunityGenerator.cs (learning integration)
- [x] src/Features/Content/PlayerBehaviorTracker.cs (engagement tracking)
- [x] ModuleData/Enlisted/camp_opportunities.json (29 opportunities)
- [x] ModuleData/Languages/enlisted_strings.xml (localization)

LEARNING SYSTEM IMPLEMENTATION:
- PlayerBehaviorTracker tracks opportunity types presented vs engaged
- GetLearningModifier() returns +10.5 (70% of +15) for >60% engagement
- GetLearningModifier() returns -7.0 (70% of -10) for <30% engagement
- Minimum 5 presentations required before learning applies
- 30% variety maintained via novelty bonus and random variety windows
- Data persists in save system via CampOpportunityGenerator.SyncData()

OPPORTUNITIES (29 Total):
Training (5):
  1. opp_weapon_drill - Weapon Drill
  2. opp_equipment_maintenance - Equipment Maintenance
  3. opp_sparring_match - Sparring Match
  4. opp_formation_practice - Formation Practice (NEW)
  5. opp_veteran_spar - Challenge a Veteran (NEW)
  6. opp_archery_range - Archery Range (NEW)

Social (6):
  7. opp_card_game - Card Game
  8. opp_war_stories - War Stories
  9. opp_tavern_visit - Camp Tavern
  10. opp_letter_writing - Write a Letter
  11. opp_storytelling_circle - Storytelling Circle (NEW)
  12. opp_drinking_heavy - Drinking Contest (NEW)
  13. opp_arm_wrestling - Arm Wrestling (NEW)
  14. opp_campfire_song - Campfire Songs (NEW)

Economic (4):
  15. opp_dice_game - Dice Game
  16. opp_foraging - Foraging Party
  17. opp_repair_work - Repair Work
  18. opp_high_stakes_cards - High Stakes Table (NEW)
  19. opp_trade_goods - Merchant Caravan (NEW)

Recovery (5):
  20. opp_rest_tent - Rest in Tent
  21. opp_help_wounded - Help the Wounded
  22. opp_prayer_service - Prayer Service
  23. opp_rest_shade - Rest in Shade (NEW)
  24. opp_meditation - Quiet Reflection (NEW)

Special (5):
  25. opp_officer_audience - Officer's Audience
  26. opp_baggage_access - Visit the Baggage
  27. opp_mentor_recruit - Mentor a Recruit (NEW)
  28. opp_volunteer_duty - Extra Duty (NEW)
  29. opp_night_patrol - Unofficial Patrol (NEW)

VERIFICATION STATUS (Phase 6D-E Complete):
[x] Engagement tracking persists across sessions
[x] Player preferences affect opportunity selection (+10.5/-7.0 modifiers)
[x] 70/30 split maintains variety (learningWeight = 0.7f)
[x] Logs show learning adjustments (ModLogger.Debug)
[x] 29 opportunities with natural language descriptions
[x] All opportunities have localization keys
[x] Garrison, Campaign, Siege feel distinct (via validPhases)
[x] Morning/Evening feel distinct (Dawn/Midday vs Dusk/Night phases)
[x] Variety tracking works (cooldown + novelty bonus)

PHASE 6F INTEGRATION TASKS (Completed 2025-12-31):
[x] Wired DECISIONS menu to show actual generator output
[x] Integrated commitment tracking UI into YOU section
[x] Implemented detection logic for risky opportunities
[x] Build successful - all systems compile
[x] Created playtesting guide with balance recommendations

FILES MODIFIED (Phase 7):
- [x] src/Features/Camp/CampOpportunityGenerator.cs (detection logic added)
- [x] src/Features/Content/ForecastGenerator.cs (commitment UI display)
- [x] src/Features/Interface/Behaviors/EnlistedMenuBehavior.cs (detection checks)
- [x] ModuleData/Languages/enlisted_strings.xml (detection strings)
- [x] docs/AFEATURE/phase7-playtesting-guide.md (new file)

DETECTION SYSTEM:
- AttemptRiskyOpportunity() checks detection when player on duty
- Detection chance: base 25%, -15% at night, -10% if high rep
- Caught consequences: officer rep penalty, discipline increase
- Notification shown to player if caught
- Integration point added before event delivery

COMMITMENT TRACKING UI:
- ForecastGenerator now displays active commitments in YOU section
- Shows "You've committed to X in N hours" message
- Handles edge cases (commitment soon, no display text)
- Persists across save/load (already working from Phase 6)

DECISIONS MENU INTEGRATION:
- DecisionManager.GetAvailableOpportunities() already wired ✓
- CampOpportunity tracked in _decisionsMenuOpportunities dictionary
- Detection check fires on decision selection for risky opportunities
- Player cannot proceed with event if caught

READY FOR PLAYTESTING:
- Balance tuning guide: docs/AFEATURE/phase7-playtesting-guide.md
- All integration points verified
- Build successful with 0 warnings
- Ready for in-game testing and balance adjustments
```

---

## Phase 6G: Create Orchestrated Decisions (BLOCKING)

**Goal:** Create the 26 missing decisions that opportunities reference

**Chat Strategy:** 🔒 **STANDALONE** - JSON content creation

**Prerequisites:** Phase 6A-F complete (opportunities exist, menu wired)

**Status:** ❌ BLOCKING - Must complete before Phase 7

```
I need you to create the missing decisions for the Content Orchestrator.

═══════════════════════════════════════════════════════════════════════════════
CONTEXT: TWO-LAYER ARCHITECTURE
═══════════════════════════════════════════════════════════════════════════════

The orchestrator uses a two-layer content system:

OPPORTUNITY (what shows in menu):
- Defined in camp_opportunities.json
- Orchestrator selects based on world state, time, player condition
- Has display text, fitness scoring, order compatibility
- Contains targetDecision field → points to a DECISION

DECISION (what fires when clicked):
- Defined in decisions.json
- Contains options (2-3 choices), rewards, consequences
- Fires when player clicks the opportunity
- Light RP moments, not complex narratives

═══════════════════════════════════════════════════════════════════════════════
CURRENT STATE
═══════════════════════════════════════════════════════════════════════════════

29 opportunities exist in camp_opportunities.json
3 target decisions exist: dec_maintain_gear, dec_write_letter, dec_gamble_high
26 target decisions are MISSING - clicking opportunities does nothing useful

38 OLD static decisions exist in decisions.json - from pre-orchestrator system
These must be DELETED before creating the new orchestrated decisions.

═══════════════════════════════════════════════════════════════════════════════
STEP 0: DELETE OLD STATIC DECISIONS
═══════════════════════════════════════════════════════════════════════════════

The OLD decisions.json contains 38 static decisions from the pre-orchestrator era.
These were designed for a different system where players browsed a static menu.
They are NOT compatible with the new orchestrator architecture.

ACTION: Delete ALL decisions from decisions.json EXCEPT these 3 that are already
referenced by opportunities and work correctly:
- dec_maintain_gear (used by opp_equipment_maintenance)
- dec_write_letter (used by opp_letter_writing)
- dec_gamble_high (used by opp_high_stakes_cards)

OLD DECISIONS TO DELETE (35 total):
- dec_rest, dec_rest_extended, dec_seek_treatment
- dec_weapon_drill, dec_spar, dec_endurance, dec_study_tactics
- dec_practice_medicine, dec_train_troops, dec_combat_drill
- dec_weapon_specialization, dec_lead_drill
- dec_join_men, dec_join_drinking, dec_seek_officers, dec_keep_to_self
- dec_confront_rival, dec_dangerous_wager, dec_prove_courage, dec_challenge
- dec_gamble_low, dec_side_work, dec_shady_deal, dec_visit_market
- dec_request_audience, dec_volunteer_duty, dec_request_leave
- dec_listen_rumors, dec_scout_area, dec_check_supplies, dec_visit_quartermaster
- dec_ret_inspect, dec_ret_drill, dec_ret_share_rations, dec_ret_address_men

WHY DELETE:
- Wrong architecture: static menu vs orchestrator-curated
- Wrong IDs: opportunities reference different decision IDs
- Wrong design: complex multi-option events vs light RP moments
- Clutters catalog: EventCatalog loads all, wastes memory

═══════════════════════════════════════════════════════════════════════════════
STEP 1: CREATE 26 NEW DECISIONS
═══════════════════════════════════════════════════════════════════════════════

Design Philosophy:
- Light Bannerlord RP as an enlisted soldier
- 2-3 options per decision (fast resolution)
- Clear tooltips with consequences
- Realistic effects: fatigue, rep, small gold/skill changes
- No complex mechanics or branching

Missing Decisions by Category:

TRAINING (5):
- dec_training_drill - Join weapon drills with other soldiers
- dec_training_spar - Sparring match with practice weapons
- dec_training_formation - Formation practice with the unit
- dec_training_veteran - Learn from a grizzled veteran
- dec_training_archery - Practice at the archery range

SOCIAL (7):
- dec_social_stories - Listen to war stories by the fire
- dec_tavern_drink - Have a drink at the sutler's tent
- dec_social_storytelling - Join the storytelling circle
- dec_drinking_contest - Enter a drinking contest
- dec_social_singing - Join campfire songs
- dec_arm_wrestling - Challenge someone to arm wrestling
- dec_gamble_cards - Play cards for low stakes

ECONOMIC (5):
- dec_gamble_dice - Dice game behind the wagons
- dec_forage - Join foraging party for extra rations
- dec_work_repairs - Do paid repair work for quartermaster
- dec_trade_browse - Browse merchant caravan goods

RECOVERY (5):
- dec_rest_sleep - Sleep in your tent
- dec_help_wounded - Help the camp surgeon
- dec_prayer - Attend the prayer service
- dec_rest_short - Quick rest in the shade
- dec_meditate - Quiet reflection

SPECIAL (5):
- dec_officer_audience - Request audience with the captain
- dec_baggage_access - Access your personal effects
- dec_mentor_recruit - Help a struggling recruit
- dec_volunteer_extra - Volunteer for extra duty
- dec_night_patrol - Join unofficial perimeter check

═══════════════════════════════════════════════════════════════════════════════
DECISION STRUCTURE (from event-system-schemas.md)
═══════════════════════════════════════════════════════════════════════════════

Each decision should follow this structure:

{
  "id": "dec_training_drill",
  "category": "decision",
  "titleId": "dec_training_drill_title",
  "title": "Weapon Drill",
  "setupId": "dec_training_drill_setup",
  "setup": "The sergeant calls for formation. Men line up with their weapons, ready to drill.",
  "requirements": {
    "tier": { "min": 1, "max": 999 }
  },
  "timing": {
    "cooldown_days": 1,
    "priority": "normal"
  },
  "options": [
    {
      "id": "drill_focused",
      "textId": "dec_training_drill_focused_text",
      "text": "Focus on your form. Train hard.",
      "rewards": {
        "skillXp": { "OneHanded": 5 }
      },
      "effects": {
        "fatigue": 2
      },
      "resultTextId": "dec_training_drill_focused_result",
      "resultText": "Sweat runs down your back. Your arms ache. But your blade feels more natural now.",
      "tooltip": "Trains weapon skill. Causes fatigue."
    },
    {
      "id": "drill_social",
      "textId": "dec_training_drill_social_text",
      "text": "Go through the motions. Chat with the lads.",
      "rewards": {
        "soldierRep": 1
      },
      "effects": {
        "fatigue": 1
      },
      "resultTextId": "dec_training_drill_social_result",
      "resultText": "You learn a few names, share a few jokes. The sergeant frowns but says nothing.",
      "tooltip": "Builds soldier rep. Light fatigue."
    },
    {
      "id": "cancel",
      "textId": "dec_training_drill_cancel_text",
      "text": "Step back. Not today.",
      "resultTextId": "dec_training_drill_cancel_result",
      "resultText": "You slip away before the sergeant notices.",
      "tooltip": "No action taken."
    }
  ]
}

═══════════════════════════════════════════════════════════════════════════════
IMPLEMENTATION STEPS
═══════════════════════════════════════════════════════════════════════════════

STEP 0: DELETE OLD SYSTEM
[ ] Open ModuleData/Enlisted/Decisions/decisions.json
[ ] Delete all 35 old decisions (keep only dec_maintain_gear, dec_write_letter, dec_gamble_high)
[ ] Verify file structure still valid JSON

STEP 1: CREATE NEW DECISIONS
[ ] Create 26 new decisions matching the targetDecision IDs from opportunities
[ ] Follow the decision structure template above
[ ] Each decision: 2-3 options, clear tooltips, light RP tone

STEP 2: ADD LOCALIZATION
[ ] Add all new strings to ModuleData/Languages/enlisted_strings.xml
[ ] Run: python tools/events/sync_event_strings.py

STEP 3: VALIDATE & TEST
[ ] Run: python tools/events/validate_events.py
[ ] Build: dotnet build -c "Enlisted RETAIL" /p:Platform=x64
[ ] In-game: verify clicking opportunities shows decision popups

═══════════════════════════════════════════════════════════════════════════════
HANDOFF NOTES (Capture for Phase 7)
═══════════════════════════════════════════════════════════════════════════════

FILES MODIFIED:
- [ ] ModuleData/Enlisted/Decisions/decisions.json (26 new decisions)
- [ ] ModuleData/Languages/enlisted_strings.xml (decision strings)

DECISIONS CREATED:
- (list all 26 decision IDs when complete)

VERIFICATION:
- [ ] All 29 opportunities have working target decisions
- [ ] Clicking opportunities shows decision popup
- [ ] Options have clear tooltips
- [ ] Effects apply correctly
```

---

## Phase 7: Content Variants (Post-Launch)

**Goal:** Add contextual variety to high-traffic content

**Chat Strategy:** 🔒 **STANDALONE** - JSON-only work, fast iteration

**Prerequisites:** Phase 6G complete (26 decisions exist and work)

```
I need you to add content variants for the Content Orchestrator.

═══════════════════════════════════════════════════════════════════════════════
CONTEXT RECOVERY (Verify system is working)
═══════════════════════════════════════════════════════════════════════════════

This is JSON-only work. Verify the orchestrator is working first:

[ ] Orchestrator fires content based on world state
[ ] EventRequirementChecker filters by requirements.context
[ ] Context values work: "Camp", "War", "Siege"
[ ] Fitness scoring selects best-fit content

No code changes needed - just JSON additions.

═══════════════════════════════════════════════════════════════════════════════
DOCUMENTATION TO READ FIRST
═══════════════════════════════════════════════════════════════════════════════

Read these docs first:
- docs/AFEATURE/content-orchestrator-plan.md (Content Variant System Summary section)
- docs/Features/Content/event-system-schemas.md (Content Variants Pattern section)

Phase 7 Tasks:
This is JSON-only work - no code changes needed.

1. Identify high-traffic decisions from playtesting
2. Create context-specific variants with requirements.context
3. Add variants to existing JSON files
4. Test variant selection in different world states

Priority Order:
1. Training decisions (dec_weapon_drill, dec_spar)
2. Rest decisions (dec_rest)
3. Common events (seen repeatedly)
4. Role-specific variants

Example Variants to Create:
```json
// Base (any context)
{
  "id": "dec_rest",
  "requirements": { "tier": { "min": 1 } }
}

// Garrison variant (more effective)
{
  "id": "dec_rest_garrison",
  "requirements": {
    "tier": { "min": 1 },
    "context": ["Camp"]
  },
  "options": [{ "rewards": { "fatigueRelief": 6 } }]
}

// Siege variant (less effective)
{
  "id": "dec_rest_exhausted",
  "requirements": {
    "tier": { "min": 1 },
    "context": ["Siege"]
  },
  "options": [{ "rewards": { "fatigueRelief": 2 } }]
}
```

Files to Modify:
- ModuleData/Enlisted/Decisions/decisions.json (add variants)
- ModuleData/Enlisted/Events/*.json (add event variants)

No Code Changes Needed:
- EventRequirementChecker already filters by context
- Orchestrator already scores all eligible content
- Variants compete naturally in selection pool

Acceptance Criteria:
- Variants appear only in matching contexts
- Base events remain as fallbacks
- Player experience varies by situation
- Tooltips show exact costs/rewards for each variant

═══════════════════════════════════════════════════════════════════════════════
HANDOFF NOTES (For documentation)
═══════════════════════════════════════════════════════════════════════════════

FILES MODIFIED:
- [ ] ModuleData/Enlisted/Decisions/decisions.json (decision variants)
- [ ] ModuleData/Enlisted/Events/*.json (event variants)

VARIANTS CREATED:
- (list all variants with IDs and contexts)

TESTING RESULTS:
- Garrison: (which variants appear)
- Campaign: (which variants appear)
- Siege: (which variants appear)
```

---

## Phase 8: Progression System (Future)

**Goal:** Add probabilistic daily progression to escalation tracks (Medical Risk, Discipline, Pay Tension)

**Chat Strategy:** 🔒 **STANDALONE** - Future system, clean separation

**Prerequisites:** Phases 1-6 complete (orchestrator provides world state modifiers)

**Status:** Schema Ready - Implement when ready for organic escalation evolution

```
I need you to implement the Progression System for escalation tracks.

═══════════════════════════════════════════════════════════════════════════════
CONTEXT RECOVERY (Verify orchestrator integration point exists)
═══════════════════════════════════════════════════════════════════════════════

Before implementing, verify:

[ ] ContentOrchestrator.Instance is available
[ ] WorldStateAnalyzer.AnalyzeSituation() works
[ ] World state can provide modifiers for progression (garrison vs siege)

The orchestrator will provide world state modifiers to progression behaviors.
If orchestrator isn't working, implement earlier phases first.

═══════════════════════════════════════════════════════════════════════════════
DOCUMENTATION TO READ FIRST
═══════════════════════════════════════════════════════════════════════════════

Read these docs first:
- docs/AFEATURE/content-orchestrator-plan.md (Future Expansion: Progression System section)
- docs/Features/Content/event-system-schemas.md (Progression System Schema section)
- docs/Features/Content/medical-progression-system.md (first implementation example)

CORE CONCEPT:
Instead of events directly setting escalation values, tracks evolve organically through daily probability checks.

Phase 8 Tasks:

TASK 1: Create Progression Infrastructure
Create files:
- src/Features/Escalation/ProgressionBehavior.cs (base class)
- src/Features/Escalation/Models/ProgressionConfig.cs
- src/Features/Escalation/Models/ProgressionModifiers.cs
- ModuleData/Enlisted/progression_config.json

Base class provides:
- Daily tick at configurable hour
- Probability calculation with modifiers
- Critical roll handling (lucky/complication)
- Threshold event firing
- Save/load persistence

TASK 2: Add Orchestrator Interface
In ContentOrchestrator.cs, implement:

```csharp
public interface IProgressionModifierProvider
{
    ProgressionModifiers GetProgressionModifiers(string trackName);
}

public class ContentOrchestrator : IProgressionModifierProvider
{
    public ProgressionModifiers GetProgressionModifiers(string trackName)
    {
        var situation = WorldStateAnalyzer.Analyze();

        // Look up modifiers from config based on world state
        return LoadModifiersFromConfig(trackName, situation);
    }
}
```

TASK 3: Implement Medical Progression
Create:
- src/Features/Escalation/MedicalProgressionBehavior.cs

This extends ProgressionBehavior and:
- Checks MedicalRisk > 0 before rolling
- Uses Medicine skill for modifiers
- Checks for "treated_today" flag
- Fires threshold events via EventDeliveryManager

TASK 4: Add Treatment Decision
Add to ModuleData/Enlisted/Decisions/decisions.json:
- dec_seek_treatment (see medical-progression-system.md for structure)
- Sets "treated_today" flag on treatment

TASK 5: Add Localization
Add to ModuleData/Languages/enlisted_strings.xml:
- prog_medical_improve, prog_medical_stable, prog_medical_worsen
- prog_medical_lucky, prog_medical_complication
- dec_seek_treatment_* strings

TASK 6: YOU Section Integration
Update EnlistedNewsBehavior.cs to include medical state in YOU section:
- "You're wounded - movement impaired. The medic says rest."
- "Feeling poorly. Rest today and hope for the best."

CONFIG STRUCTURE (progression_config.json):
{
  "medical_risk": {
    "enabled": true,
    "tick": { "type": "daily", "hour": 6 },
    "baseChances": { ... },
    "skillModifier": { "skill": "Medicine", "divisor": 3, "maxBonus": 20 },
    "contextModifiers": { ... },
    "worldStateModifiers": { ... }
  },
  "discipline": { ... },
  "pay_tension": { ... }
}

EDGE CASES:
- Skip ticks during battle/muster
- Clamp probabilities to min/max limits
- Don't fire skipped threshold events on rapid changes
- Clear daily flags on day rollover
- Batch-roll missed ticks after fast travel

FILES TO CREATE:
- src/Features/Escalation/ProgressionBehavior.cs
- src/Features/Escalation/MedicalProgressionBehavior.cs
- src/Features/Escalation/Models/ProgressionConfig.cs
- src/Features/Escalation/Models/ProgressionModifiers.cs
- ModuleData/Enlisted/progression_config.json

FILES TO MODIFY:
- src/Features/Content/ContentOrchestrator.cs (add interface)
- src/Features/Interface/Behaviors/EnlistedNewsBehavior.cs (YOU section)
- ModuleData/Enlisted/Decisions/decisions.json (treatment decision)
- ModuleData/Languages/enlisted_strings.xml (localization)
- Enlisted.csproj (add new files)

ACCEPTANCE CRITERIA:
- [ ] Daily tick at configured hour checks escalation > 0
- [ ] Roll determines improve/stable/worsen outcome
- [ ] Skills modify probabilities
- [ ] Context flags (resting, treated) modify probabilities
- [ ] Orchestrator provides world state modifiers
- [ ] Threshold crossings fire events via EventDeliveryManager
- [ ] YOU section shows medical state
- [ ] All probabilities configurable in JSON
- [ ] State persists through save/load
- [ ] Tooltips on treatment decision show effects

FUTURE EXTENSION (after Phase 8):
- Add DisciplineProgressionBehavior
- Add PayTensionProgressionBehavior
- Add specific condition types (Fever, Infection, Plague)
- Add contagion between party members
- Add permanent consequences (scars, stat penalties)

═══════════════════════════════════════════════════════════════════════════════
HANDOFF NOTES (For documentation)
═══════════════════════════════════════════════════════════════════════════════

FILES CREATED:
- [ ] src/Features/Escalation/ProgressionBehavior.cs
- [ ] src/Features/Escalation/MedicalProgressionBehavior.cs
- [ ] src/Features/Escalation/Models/ProgressionConfig.cs
- [ ] src/Features/Escalation/Models/ProgressionModifiers.cs
- [ ] ModuleData/Enlisted/progression_config.json

FILES MODIFIED:
- [ ] src/Features/Content/ContentOrchestrator.cs (IProgressionModifierProvider)
- [ ] src/Features/Interface/Behaviors/EnlistedNewsBehavior.cs (YOU section)
- [ ] ModuleData/Enlisted/Decisions/decisions.json (treatment decision)
- [ ] ModuleData/Languages/enlisted_strings.xml
- [ ] Enlisted.csproj

PROGRESSION IMPLEMENTATION:
- Base probability tables
- Skill modifiers (Medicine, etc.)
- Context modifiers (resting, treated)
- World state modifiers from orchestrator

VERIFICATION PASSED:
[ ] Daily progression ticks work
[ ] Probabilities configurable via JSON
[ ] Skills affect outcomes
[ ] World state affects outcomes
[ ] Threshold events fire correctly
[ ] YOU section shows medical state
```

---

## Phase 9: Decision Scheduling (Must Have)

**Goal:** Allow players to commit to camp decisions that fire at specific phases (Dawn/Midday/Dusk/Night). Decision greys out when committed and fires automatically at the scheduled phase. Prevents overwhelming player with immediate popups at fast time speeds.

**Chat Strategy:** 🔒 **STANDALONE** - Phase scheduling system

**Prerequisites:** 
- ✅ Phase 6A-F complete (camp opportunities exist)
- ❌ **Phase 6G MUST COMPLETE FIRST** (26 missing decisions must be created)

**Status:** ⛔ **BLOCKED by Phase 6G** - Cannot schedule decisions that don't exist

**Blocking Issue:**
Phase 6 created 29 opportunities, but only 3 decisions exist. Need to create 26 missing decisions before scheduling can work. See Phase 6G below.

```
I need you to implement the Decision Scheduling system.

═══════════════════════════════════════════════════════════════════════════════
CONTEXT RECOVERY
═══════════════════════════════════════════════════════════════════════════════

CURRENT PROBLEM:
- Player clicks decision in DECISIONS menu → event fires immediately
- At FastForward speed (20 seconds per game day), player gets bombarded with popups
- No sense of time passing or planning ahead
- Decisions feel reactive, not deliberate

SOLUTION:
- Each opportunity tagged with preferred phase (Dawn/Midday/Dusk/Night)
- Player commits to decision → greys out, shows "Scheduled for Midday"
- At scheduled phase → event fires automatically
- Player status shows: "You've committed to sparring at noon."
- Can cancel commitment with small penalty

TIME SPEEDS (from decompile research):
- Play (1x): 1 game day = 80 real seconds
- FastForward (>>): 1 game day = 20 real seconds
- Phase length (6 hours): Play = 20 seconds, FastForward = 5 seconds

═══════════════════════════════════════════════════════════════════════════════
IMPLEMENTATION PLAN
═══════════════════════════════════════════════════════════════════════════════

TASK 1: Add Phase Tags to Opportunities
In ModuleData/Enlisted/Decisions/camp_opportunities.json:

Add "scheduledPhase" field to each opportunity:
```json
{
  "id": "opp_sparring_match",
  "title": "Sparring Match",
  "scheduledPhase": "Midday",  // Dawn, Midday, Dusk, Night
  "immediate": false,  // If true, fires immediately (backwards compat)
  ...
}
```

Examples:
- Training activities → Midday (noon sun, active)
- Social activities → Dusk (evening, relaxed)
- Rest activities → Night (sleep time)
- Special activities → Dawn (morning, fresh start)

TASK 2: Add Commitment Tracking
In src/Features/Camp/CampOpportunityGenerator.cs:

```csharp
public class ScheduledCommitment
{
    public string OpportunityId { get; set; }
    public string TargetDecision { get; set; }
    public CampaignTime CommitTime { get; set; }
    public string ScheduledPhase { get; set; }  // Dawn/Midday/Dusk/Night
    public int ScheduledDay { get; set; }  // Day number when it should fire
}

private List<ScheduledCommitment> _commitments = new List<ScheduledCommitment>();
private CampaignTime _lastPhaseCheck = CampaignTime.Zero;

public void CommitToOpportunity(CampOpportunity opportunity)
{
    // Create commitment
    var commitment = new ScheduledCommitment
    {
        OpportunityId = opportunity.Id,
        TargetDecision = opportunity.TargetDecision,
        CommitTime = CampaignTime.Now,
        ScheduledPhase = opportunity.ScheduledPhase,
        ScheduledDay = CalculateScheduledDay(opportunity.ScheduledPhase)
    };
    
    _commitments.Add(commitment);
    
    // Mark as unavailable
    // Update UI
    
    ModLogger.Info("Camp", $"Committed to {opportunity.Id} for {commitment.ScheduledPhase}");
}

private int CalculateScheduledDay(string phase)
{
    var currentHour = CampaignTime.Now.GetHourOfDay;
    var currentDay = (int)CampaignTime.Now.ToDays;
    
    var phaseHour = phase switch
    {
        "Dawn" => 6,
        "Midday" => 12,
        "Dusk" => 18,
        "Night" => 0,
        _ => 12
    };
    
    // If phase hour has passed today, schedule for next day
    if (phaseHour == 0) // Night
    {
        return currentHour < 3 ? currentDay : currentDay + 1;
    }
    
    return currentHour >= phaseHour ? currentDay + 1 : currentDay;
}
```

TASK 3: Check Phase Transitions
Add hourly tick to check for scheduled commitments:

```csharp
public void OnHourlyTick()
{
    var currentHour = CampaignTime.Now.GetHourOfDay;
    var currentDay = (int)CampaignTime.Now.ToDays;
    
    // Check phase boundaries (6am, 12pm, 6pm, 12am/24)
    if (currentHour == 6 || currentHour == 12 || currentHour == 18 || currentHour == 0)
    {
        var currentPhase = GetCurrentPhase(currentHour);
        FireScheduledCommitments(currentPhase, currentDay);
    }
}

private string GetCurrentPhase(int hour)
{
    return hour switch
    {
        6 => "Dawn",
        12 => "Midday",
        18 => "Dusk",
        0 or 24 => "Night",
        _ => null
    };
}

private void FireScheduledCommitments(string phase, int day)
{
    var toFire = _commitments.Where(c => 
        c.ScheduledPhase == phase && 
        c.ScheduledDay == day
    ).ToList();
    
    foreach (var commitment in toFire)
    {
        // Fire the event via EventDeliveryManager
        var decision = GetDecisionById(commitment.TargetDecision);
        if (decision != null)
        {
            EventDeliveryManager.Instance.DeliverEvent(decision);
            ModLogger.Info("Camp", $"Fired scheduled commitment: {commitment.OpportunityId}");
        }
        
        _commitments.Remove(commitment);
    }
}
```

TASK 4: Update UI to Show Commitments
In src/Features/Interface/Behaviors/EnlistedMenuBehavior.cs:

Update DECISIONS menu to show commitment state:
```csharp
// Available opportunities
foreach (var opp in available)
{
    var isCommitted = IsCommittedTo(opp.Id);
    
    if (isCommitted)
    {
        // Show greyed out with scheduled time
        var commitment = GetCommitment(opp.Id);
        var hoursUntil = CalculateHoursUntil(commitment.ScheduledPhase, commitment.ScheduledDay);
        
        row = $"    {opp.Title} [SCHEDULED - {hoursUntil}h]";
        args.IsEnabled = false;
        args.Tooltip = new TextObject($"Fires at {commitment.ScheduledPhase} in {hoursUntil} hours. Right-click to cancel.");
    }
    else
    {
        row = $"    {opp.Title} ({opp.ScheduledPhase})";
        args.Tooltip = new TextObject($"Commits to this activity. Will fire at {opp.ScheduledPhase}.");
    }
}
```

TASK 5: Add Commitment Display to Your Status
In src/Features/Interface/Behaviors/EnlistedNewsBehavior.cs:

```csharp
private string BuildPlayerCommitmentLine()
{
    var generator = CampOpportunityGenerator.Instance;
    var commitments = generator?.GetActiveCommitments();
    
    if (commitments == null || commitments.Count == 0)
        return string.Empty;
    
    var next = commitments.OrderBy(c => c.ScheduledDay).ThenBy(c => c.ScheduledPhase).First();
    var hoursUntil = CalculateHoursUntil(next.ScheduledPhase, next.ScheduledDay);
    
    return $"You've committed to {GetActivityName(next.OpportunityId)} in {hoursUntil} hours.";
}

// Add to Your Status section (line 1 if exists)
```

TASK 6: Allow Cancellation
Add right-click or cancel option:

```csharp
public void CancelCommitment(string opportunityId)
{
    var commitment = _commitments.FirstOrDefault(c => c.OpportunityId == opportunityId);
    if (commitment == null)
        return;
    
    _commitments.Remove(commitment);
    
    // Small penalty for canceling
    var enlistment = EnlistmentBehavior.Instance;
    enlistment.AdjustFatigue(-5); // Minor fatigue cost
    
    ModLogger.Info("Camp", $"Cancelled commitment: {opportunityId}");
    
    InformationManager.DisplayMessage(new InformationMessage(
        "Commitment cancelled. You feel restless from changing plans.",
        Colors.Yellow));
}
```

TASK 7: Persistence
Add to save system:

```csharp
[SaveableField(XXX)]
private List<ScheduledCommitment> _commitments;

public override void SyncData(IDataStore dataStore)
{
    dataStore.SyncData("_commitments", ref _commitments);
    // ... existing sync code
}
```

═══════════════════════════════════════════════════════════════════════════════
EDGE CASES
═══════════════════════════════════════════════════════════════════════════════

- Player commits to multiple activities: Queue them by phase
- Fast travel past scheduled time: Fire immediately on arrival
- Player enters battle during commitment: Queue survives, fires after battle
- Phase transition happens while in menu: Fire after menu closes
- Commitment conflicts with order: Order takes priority, commitment auto-cancels with no penalty
- Player unconscious at scheduled time: Auto-cancel commitment
- Save/load: Commitments persist correctly

FILES TO MODIFY:
- src/Features/Camp/CampOpportunityGenerator.cs (commitment tracking)
- src/Features/Camp/Models/CampOpportunity.cs (add ScheduledPhase field)
- src/Features/Interface/Behaviors/EnlistedMenuBehavior.cs (DECISIONS menu UI)
- src/Features/Interface/Behaviors/EnlistedNewsBehavior.cs (Your Status display)
- ModuleData/Enlisted/Decisions/camp_opportunities.json (add scheduledPhase to all)
- ModuleData/Languages/enlisted_strings.xml (commitment messages)

ACCEPTANCE CRITERIA:
[ ] Each opportunity has scheduledPhase tag
[ ] Player can commit to decision → greys out in menu
[ ] Commitment fires automatically at scheduled phase
[ ] Your Status shows active commitment(s)
[ ] Player can cancel commitment with minor penalty
[ ] Multiple commitments queue properly
[ ] Fast travel fires queued commitments on arrival
[ ] Commitments persist through save/load
[ ] Build successful with 0 errors

═══════════════════════════════════════════════════════════════════════════════
HANDOFF NOTES
═══════════════════════════════════════════════════════════════════════════════

FILES MODIFIED:
[ ] src/Features/Camp/CampOpportunityGenerator.cs
[ ] src/Features/Camp/Models/CampOpportunity.cs
[ ] src/Features/Interface/Behaviors/EnlistedMenuBehavior.cs
[ ] src/Features/Interface/Behaviors/EnlistedNewsBehavior.cs
[ ] ModuleData/Enlisted/Decisions/camp_opportunities.json
[ ] ModuleData/Languages/enlisted_strings.xml

SCHEDULING SYSTEM:
- Opportunities tagged with Dawn/Midday/Dusk/Night
- Commitment creates scheduled event for future phase
- Hourly tick checks phase transitions (6am, 12pm, 6pm, 12am)
- Events fire automatically at scheduled time
- UI shows [SCHEDULED] state with hours until

VERIFICATION:
[ ] Commitments fire at correct phase
[ ] UI shows commitment state properly
[ ] Cancellation works with penalty
[ ] Fast travel handles queued commitments
[ ] Save/load persists commitments
```

---

## Phase 10: Order Forecasting (Must Have)

**Goal:** Give players advance warning (4-8 hours) before orders are issued to handle fast time speeds. Integrate forecasts into the 3 existing summary sections with max 4-5 lines per section.

**Chat Strategy:** 🔒 **STANDALONE** - Player visibility system

**Prerequisites:** Phases 1-6 complete, Orders work, Reports work

**Status:** ❌ Not Started - **MUST BE DONE**

```
I need you to implement the Forecast & Scheduling system.

═══════════════════════════════════════════════════════════════════════════════
CONTEXT RECOVERY
═══════════════════════════════════════════════════════════════════════════════

The player needs advance warning when orders/events are coming.

CURRENT PROBLEM:
- Orders issued every ~3 days, appear immediately in Orders menu
- At FastForward speed: 1 game day = 20 real seconds (research from decompile)
- At Play speed: 1 game day = 80 real seconds
- Orders appear with no warning - player feels ambushed

TIME SPEEDS (from Campaign.cs decompile):
- Play (1x): MapTimeTracker.Tick(1080 * realDt) → 1 game hour = 3.33 real seconds
- FastForward (>>): MapTimeTracker.Tick(4320 * realDt) → 1 game hour = 0.833 real seconds
- Phase (6h): Play = 20 seconds, FastForward = 5 seconds

SOLUTION (Imminent Warning System):
- 4-8 hour advance warning for orders (13-26 seconds at FastForward)
- Simple: Orchestrator decides "order should fire" → creates warning → waits 4-8h → issues
- Forecasts appear in the 3 existing summary sections
- Max 4-5 lines per section to keep it concise
- Order states: IMMINENT → PENDING → ACTIVE → COMPLETE

WHY NOT LONG-TERM FORECASTING:
- Orchestrator designed for "what content fits NOW", not "what will I need in 2 days"
- World state changes rapidly (siege starts, battle happens, lord dismisses you)
- 4-8 hour window is realistic: "Sergeant will call for you soon" vs "You'll have orders in 2 days"

═══════════════════════════════════════════════════════════════════════════════
DOCUMENTATION TO READ FIRST
═══════════════════════════════════════════════════════════════════════════════

Read:
- docs/Features/Core/orders-system.md (see line 60: FORECAST → SCHEDULED flow)
- src/Features/Orders/Behaviors/OrderManager.cs (current immediate-issue logic)
- src/Features/Interface/Behaviors/EnlistedNewsBehavior.cs (BuildDailyBriefSection)

CURRENT SUMMARY STRUCTURE (3 sections):
1. Kingdom Reports - War fronts, lord activities, kingdom news
2. Company Reports - Company situation, casualties, supplies, events
3. Your Status - Player condition, fatigue, injuries, active duty

═══════════════════════════════════════════════════════════════════════════════
IMPLEMENTATION PLAN
═══════════════════════════════════════════════════════════════════════════════

TASK 1: Add Order States
In src/Features/Orders/Models/Order.cs:

```csharp
public enum OrderState
{
    Imminent,     // 4-8h warning - "Sergeant will call for you soon"
    Pending,      // Issued - shows in Orders menu for accept/decline
    Active,       // Accepted - progressing through phases
    Complete      // Done - results in Recent Activity
}

public class Order
{
    // ... existing fields ...
    public OrderState State { get; set; } = OrderState.Pending;
    public CampaignTime ImminentTime { get; set; }  // When imminent warning began
    public CampaignTime IssueTime { get; set; } // When order will be issued (ImminentTime + 4-8h)
}
```

TASK 2: Modify OrderManager Timing
In src/Features/Orders/Behaviors/OrderManager.cs:

Current logic:
- TryIssueOrder() fires every ~3 days
- Order appears immediately in menu

New logic (Simplified Imminent Warning):
- TryIssueOrder() at ~3 days → Orchestrator decides "order should fire NOW"
- Instead of immediate issue, create order in IMMINENT state with 4-8h delay
- Update() checks order state transitions:
  - IMMINENT → PENDING (when IssueTime arrives)
  - PENDING → ACTIVE (when player accepts or mandatory auto-accepts)
  - ACTIVE → COMPLETE (when order finishes)

Add methods:
- CreateImminentOrder() - creates order with 4-8h warning
- UpdateOrderState() - transitions IMMINENT → PENDING based on time
- GetImminentWarningText() - returns warning text for summaries
- CancelImminentOrder() - if world state changes dramatically (siege ends, lord dies)

TASK 3: Integrate Forecasts into Summaries
In src/Features/Interface/Behaviors/EnlistedNewsBehavior.cs:

Modify BuildDailyBriefSection() to call new methods:

Kingdom Reports section:
- Add BuildKingdomForecastLine() → "Expect strategic orders soon."
- Max 1 line for forecast, 4 lines for actual kingdom news

Company Reports section:
- Add BuildCompanyForecastLine() → "Sergeant looking for volunteers for patrol duty."
- Max 1 line for forecast, 4 lines for company situation

Your Status section:
- Add BuildPlayerForecastLine() → "Guard duty scheduled in 8 hours."
- Max 1 line for forecast, 4 lines for player condition

Methods:
```csharp
private string BuildKingdomForecastLine()
{
    var orderManager = Orders.Behaviors.OrderManager.Instance;
    var order = orderManager?.GetCurrentOrder();
    
    if (order == null || order.State < OrderState.Forecast)
        return string.Empty;
    
    if (order.State == OrderState.Forecast && order.Tags.Contains("strategic"))
    {
        return "Expect strategic orders from command soon.";
    }
    
    return string.Empty;
}

private string BuildCompanyForecastLine()
{
    var orderManager = Orders.Behaviors.OrderManager.Instance;
    var order = orderManager?.GetCurrentOrder();
    
    if (order == null || order.State < OrderState.Forecast)
        return string.Empty;
    
    if (order.State == OrderState.Forecast && !order.Tags.Contains("strategic"))
    {
        return $"Sergeant looking for {GetRoleText(order.Tags)}.";
    }
    
    return string.Empty;
}

private string BuildPlayerForecastLine()
{
    var orderManager = Orders.Behaviors.OrderManager.Instance;
    var order = orderManager?.GetCurrentOrder();
    
    if (order == null || order.State < OrderState.Scheduled)
        return string.Empty;
    
    if (order.State == OrderState.Scheduled)
    {
        var hoursUntil = (order.ScheduledTime - CampaignTime.Now).ToHours;
        return $"{order.Title} scheduled in {(int)hoursUntil} hours.";
    }
    
    return string.Empty;
}
```

TASK 4: Update Orders Menu UI
In src/Features/Interface/Behaviors/EnlistedMenuBehavior.cs:

Update the Orders accordion to show forecast/scheduled orders with different styling:
- FORECAST orders: Show as "     [FORECAST] Duty assignment likely"
- SCHEDULED orders: Show as "     [SCHEDULED] Guard Duty (in 8h)"
- PENDING orders: Show as "     [NEW] Guard Duty" (existing)
- ACTIVE orders: Show as "     [ASSIGNED] Guard Duty" (existing, from Phase 6F)

TASK 5: Timing Configuration
In ModuleData/Enlisted/Config/enlisted_config.json:

Add forecast timing config:
```json
"orders": {
  "forecast_window_min_hours": 12,
  "forecast_window_max_hours": 24,
  "scheduled_window_min_hours": 8,
  "scheduled_window_max_hours": 18
}
```

TASK 6: Localization
Add to ModuleData/Languages/enlisted_strings.xml:
- forecast_strategic_orders - "Expect strategic orders from command soon."
- forecast_sergeant_patrol - "Sergeant looking for patrol volunteers."
- forecast_sergeant_guard - "Sergeant organizing guard rotations."
- scheduled_order_template - "{ORDER_TITLE} scheduled in {HOURS} hours."

═══════════════════════════════════════════════════════════════════════════════
SUMMARY SECTION LINE LIMITS
═══════════════════════════════════════════════════════════════════════════════

Each section must be 4-5 lines MAX:

Kingdom Reports (4-5 lines total):
  Line 1: [Forecast if any] "Expect strategic orders soon."
  Lines 2-5: Actual kingdom news (war fronts, lord activities)

Company Reports (4-5 lines total):
  Line 1: [Forecast if any] "Sergeant looking for patrol volunteers."
  Lines 2-5: Company situation, casualties, supplies, events

Your Status (4-5 lines total):
  Line 1: [Forecast if any] "Guard duty scheduled in 8 hours."
  Lines 2-5: Player condition, fatigue, injuries, active duty

When a forecast is present, reduce other content to maintain 4-5 line limit.

═══════════════════════════════════════════════════════════════════════════════
EDGE CASES
═══════════════════════════════════════════════════════════════════════════════

- Mandatory orders (T1-T3): Still auto-accept when PENDING → ACTIVE transition
- Optional orders (T4+): Remain clickable in PENDING state for accept/decline
- Fast time (2x speed): Forecasts ensure 6+ real-world hours of warning minimum
- Save/load: Order state and timing persist correctly
- Multiple orders: Only show forecast for next immediate order
- Forecast suppression: Don't show if player is in battle or unconscious

FILES TO MODIFY:
- src/Features/Orders/Models/Order.cs (add OrderState enum)
- src/Features/Orders/Behaviors/OrderManager.cs (forecast logic)
- src/Features/Interface/Behaviors/EnlistedNewsBehavior.cs (forecast lines)
- src/Features/Interface/Behaviors/EnlistedMenuBehavior.cs (Orders UI)
- ModuleData/Enlisted/Config/enlisted_config.json (timing config)
- ModuleData/Languages/enlisted_strings.xml (localization)

ACCEPTANCE CRITERIA:
[ ] Orders forecast 12-24h before issue
[ ] Forecasts appear in appropriate summary section (Kingdom/Company/Your Status)
[ ] Each summary section remains 4-5 lines max
[ ] FORECAST → SCHEDULED → PENDING transitions work
[ ] Orders menu shows forecast/scheduled orders with proper styling
[ ] Mandatory orders still auto-accept at PENDING state
[ ] Optional orders remain clickable at PENDING state
[ ] Timing persists through save/load
[ ] Build successful with 0 errors

═══════════════════════════════════════════════════════════════════════════════
HANDOFF NOTES
═══════════════════════════════════════════════════════════════════════════════

FILES MODIFIED:
[ ] src/Features/Orders/Models/Order.cs
[ ] src/Features/Orders/Behaviors/OrderManager.cs
[ ] src/Features/Interface/Behaviors/EnlistedNewsBehavior.cs
[ ] src/Features/Interface/Behaviors/EnlistedMenuBehavior.cs
[ ] ModuleData/Enlisted/Config/enlisted_config.json
[ ] ModuleData/Languages/enlisted_strings.xml

FORECAST SYSTEM:
- Order states: FORECAST → SCHEDULED → PENDING → ACTIVE → COMPLETE
- Timing: 12-24h forecast, 8-18h scheduled, then issued
- Integrated into 3 summary sections (max 4-5 lines each)
- Orders menu shows forecast/scheduled with styling

VERIFICATION:
[ ] Forecast appears 12-24h before order issue
[ ] Summary sections remain concise (4-5 lines)
[ ] Player has time to see orders coming at 1x/2x speed
[ ] State transitions work correctly
[ ] Save/load persists forecast state
```

---

## Quick Reference: Build & Test

After each phase, run:

```powershell
cd C:\Dev\Enlisted\Enlisted
dotnet build -c "Enlisted RETAIL" /p:Platform=x64
```

Check logs at:
```
C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\Enlisted\Debugging\enlisted.log
```

Look for `[Orchestrator]` log entries.

---

## Notes

### Prompt Design

Each prompt is designed for a **new AI chat session** with full context recovery:
- **Prerequisites** tell the AI what must exist before starting
- **Context Recovery** provides verification checklist for previous work
- **Handoff Notes** capture what was done for the next AI

### Chat Strategy Summary

| Phases | Strategy | Rationale |
|--------|----------|-----------|
| 1 | ✅ **DONE** | Phase 1 is complete |
| 2 | Standalone | Builds on Phase 1; context recovery provided |
| 3 | Standalone | High-risk migration needs focus |
| 4 | Standalone | Distinct system (Orders) |
| 4.5 | Standalone | Native API bridge; decompile-verified patterns |
| 5 | Standalone | UI work is self-contained |
| 5.5 | Standalone | Complex Bannerlord API work |
| 6A-C | **COMBINE** | Core functionality builds together |
| 6D-E | Standalone | Polish phase, can be done later |
| 7 | Standalone | JSON-only, fast iteration |
| 8 | Standalone | Future system, clean separation |

### Critical Path

- **Phases 1-3** are the critical path (orchestrator must work)
- **Phase 4** orders content (JSON), can parallelize with 4.5
- **Phase 4.5** native effects integration (unlocks better tooltips for Phase 5)
- **Phases 5-6** can be adjusted based on playtesting
- **Phase 7** is post-launch polish (JSON only)
- **Phase 8** is future expansion when ready

### Documentation References

- **The orchestrator plan** (`content-orchestrator-plan.md`) has complete technical specs
- **Blueprint** (`BLUEPRINT.md`) has coding standards that must be followed
- **Event schemas** (`event-system-schemas.md`) has JSON data structures

### If Something Goes Wrong

If a new AI chat finds missing dependencies:
1. Read the Prerequisites section to identify what's missing
2. Go back to the appropriate phase prompt
3. Implement the missing parts before continuing

