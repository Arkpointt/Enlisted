# Content Orchestrator Implementation Prompts

**Summary:** Copy-paste prompts for each implementation phase of the Content Orchestrator. Use these to guide implementation with your preferred coding assistant.

**Status:** 📋 Reference  
**Last Updated:** 2025-12-30  
**Related Docs:** [Content Orchestrator Plan](content-orchestrator-plan.md), [Order Events Master](order-events-master.md), [BLUEPRINT](../BLUEPRINT.md)

---

## Index

| Phase | Description | Model |
|-------|-------------|-------|
| [Phase 1: Foundation](#phase-1-foundation) | Build core infrastructure | Opus 4 |
| [Phase 2: Selection](#phase-2-content-selection-integration) | Connect to content selection | Opus 4 |
| [Phase 3: Cutover](#phase-3-cutover) | Switch to orchestrator | Sonnet 4 |
| [Phase 4: Orders](#phase-4-orders-integration) | Coordinate order timing | Sonnet 4 |
| [Phase 5: UI](#phase-5-ui-integration-company-report) | Main Menu + Camp Hub UI | Sonnet 4 |
| [Phase 6: Camp Life](#phase-6-camp-life-simulation-living-breathing-world) | Living breathing camp simulation | Opus 4 |
| [Phase 7: Variants](#phase-7-content-variants-post-launch) | Add content variants (JSON) | Sonnet 4 |
| [Phase 8: Progression](#phase-8-progression-system-future) | Organic escalation evolution | Opus 4 |
| [Build & Test](#quick-reference-build--test) | Commands reference | - |

---

## Model Recommendations

| Phase | Complexity | Recommended Model |
|-------|------------|-------------------|
| Phase 1 - Foundation | High (new architecture) | Claude Opus 4 |
| Phase 2 - Selection | Medium-High (integration) | Claude Opus 4 |
| Phase 3 - Cutover | Medium (migration) | Claude Sonnet 4 |
| Phase 4 - Orders | Medium (integration) | Claude Sonnet 4 |
| Phase 5 - UI | Medium (UI changes) | Claude Sonnet 4 |
| Phase 6 - Camp Life | High (living simulation) | Claude Opus 4 |
| Phase 7 - Variants | Low (JSON only) | Claude Sonnet 4 |
| Phase 8 - Progression | High (new system) | Claude Opus 4 |

---

## Phase 1: Foundation

**Goal:** Build core orchestrator infrastructure without changing existing behavior

```
I need you to implement Phase 1 of the Content Orchestrator for my Bannerlord mod.

Read these docs first:
- docs/Features/Content/content-orchestrator-plan.md (full plan, start with Quick Start Guide)
- docs/BLUEPRINT.md (coding standards, API verification, logging)

Phase 1 Tasks:
1. Create ContentOrchestrator.cs class (CampaignBehaviorBase)
2. Create WorldStateAnalyzer.cs static class
3. Create SimulationPressureCalculator.cs static class
4. Create PlayerBehaviorTracker.cs static class
5. Create data models (WorldSituation, SimulationPressure, PlayerPreferences, enums)
6. Wire up daily tick to log analysis (don't affect live system yet)
7. Add comprehensive logging with category "Orchestrator"
8. Register all custom types in EnlistedSaveDefiner.cs
9. Implement SyncData() in ContentOrchestrator for state persistence

File Locations:
- Main classes: src/Features/Content/
- Models: src/Features/Content/Models/

Critical Requirements:
- Add ALL new files to Enlisted.csproj manually (old-style project)
- Verify APIs against local decompile at C:\Dev\Enlisted\Decompile\
- Use ModLogger with category "Orchestrator"
- Follow ReSharper recommendations
- Comments describe current behavior (no "Phase X added" style)

API Verification Checklist (verify in decompile BEFORE implementing):
- MobileParty.CurrentSettlement - property (Settlement?) for garrison detection
- MobileParty.Army - property (Army?) for solo vs army check
- MobileParty.Party.SiegeEvent - property (SiegeEvent?) for siege detection
- FactionManager.IsAtWarAgainstFaction(Kingdom, Kingdom) - method signature
- CampaignTime.Now - property, verify .GetDayOfYear exists
- Hero.MainHero - property (use CampaignSafetyGuard.SafeMainHero for null safety)

Save/Load Requirements (CRITICAL - see BLUEPRINT section 628-651):
1. Register enums in EnlistedSaveDefiner.DefineEnumTypes():
   - WorldSituation enums (LordSituation, WarStance, LifePhase, ActivityLevel, etc.)
2. Register classes in EnlistedSaveDefiner.DefineClassTypes():
   - WorldSituation, SimulationPressure, PlayerPreferences (if persisted as objects)
3. Register containers in EnlistedSaveDefiner.DefineContainerDefinitions():
   - Dictionary<string, int> for behavior tracking
   - Dictionary<string, float> for content engagement
4. Implement SyncData() in ContentOrchestrator:
   - Use SaveLoadDiagnostics.SafeSyncData() wrapper
   - Persist player behavior counts
   - Persist content engagement tracking
   - Persist dampening state (events this week, last week reset)

Acceptance Criteria:
- Orchestrator receives daily ticks when enlisted
- World state analysis logs correctly (LifePhase, ActivityLevel)
- Pressure calculation logs sources and total
- All custom types registered in EnlistedSaveDefiner
- SyncData() implemented with SafeSyncData() wrapper
- Can save and load without "Cannot Create Save" error
- Existing event system still works normally (no changes to live pacing)

Start by reading the orchestrator plan doc, then implement. Show me the files to create.
```

---

## Phase 2: Content Selection Integration

**Goal:** Connect orchestrator to content selection

```
I need you to implement Phase 2 of the Content Orchestrator.

Read these docs first:
- docs/Features/Content/content-orchestrator-plan.md (Phase 2 section)
- docs/Features/Content/content-system-architecture.md (current system)
- docs/Features/Content/event-system-schemas.md (data structures)

Phase 2 Tasks:
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

CRITICAL: Context Mapping Requirements
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

Acceptance Criteria:
- Orchestrator can query EventCatalog with world context
- Fitness scoring considers world situation + player preferences
- Logs show clear comparison: "Old system would pick X, new system picks Y"
- Player preferences begin tracking from choices

Don't break the existing system. Log comparisons only.
```

---

## Phase 3: Cutover & Migration

**Goal:** Switch from schedule-driven event pacing to orchestrator-driven content delivery. This is a MIGRATION - old systems get removed.

```
I need you to implement Phase 3 of the Content Orchestrator - the cutover and migration.

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
```

---

## Phase 4: Orders Integration

**Goal:** Coordinate order timing with orchestrator

```
I need you to implement Phase 4 of the Content Orchestrator - Orders integration.

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
   
5. Implement placeholder text resolution for events:
   - {SERGEANT} → culture-specific NCO (Empire: "Optio", Vlandia: "Sergeant", etc.)
   - {LORD_NAME} → enlisted lord's name
   - {PLAYER_RANK} → culture-specific player rank title
   - {SOLDIER_NAME}, {COMRADE_NAME} → generated soldier names
   - See event-system-schemas.md for full placeholder list
   
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

Acceptance Criteria:
- OrderManager checks CanIssueOrderNow() before issuing
- Orders arrive realistically based on world state
- Order slot events weighted by activity level
- Event text displays with culture-specific NCO/officer titles
- Camp life events fire between orders (orchestrator handles)
- No overwhelming spam from combined systems
```

---

## Phase 5: UI Integration (Quick Decision Center)

**Goal:** Implement the Main Menu Quick Decision Center with KINGDOM, CAMP, YOU sections

```
I need you to implement Phase 5 of the Content Orchestrator - UI Integration.

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

Acceptance Criteria:
- Main Menu shows KINGDOM, CAMP, YOU sections with brief info
- YOU section shows NOW (current state) + AHEAD (forecast)
- Forecast uses culture-appropriate rank names
- Three buttons navigate to ORDERS, DECISIONS, CAMP
- DECISIONS shows dynamically generated camp life activities
- Deep info (rank, tier, records) is in CAMP Hub, not Main Menu
```

---

## Phase 6: Camp Life Simulation (Living Breathing World)

**Goal:** Create a living, breathing military camp that runs independently of player input

**Dependency:** Requires Phases 1-5 (Content Orchestrator) to be complete

```
I need you to implement Phase 6 of the Content Orchestrator - Camp Life Simulation.

Read these docs first:
- docs/AFEATURE/camp-life-simulation.md (COMPLETE SPEC - read the whole thing)
- docs/AFEATURE/content-orchestrator-plan.md (orchestrator you're building on)
- docs/Features/Campaign/camp-life-simulation.md (existing CampLifeBehavior to keep)

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

1. CREATE Data Models:
   - src/Features/Camp/Models/CampOpportunity.cs
   - src/Features/Camp/Models/OpportunityType.cs (enum: Training, Social, Economic, Recovery, Special)
   - src/Features/Camp/Models/CampContext.cs (DayPhase, DaysSinceLastMuster, CurrentMood, RecentEvents)
   - src/Features/Camp/Models/CampMood.cs (enum: Routine, Celebration, Mourning, Tense)
   - src/Features/Camp/Models/DayPhase.cs (enum: Dawn 6-11, Midday 12-5, Dusk 6-9, Night 10-5) - synced with Order phases
   - src/Features/Camp/Models/OpportunityHistory.cs (tracks presentations, engagements, variety)

2. CREATE CampOpportunityGenerator.cs:
   - Location: src/Features/Camp/CampOpportunityGenerator.cs
   - This is the HEART of the living camp simulation
   - GenerateCampLife() method - main entry point
   - AnalyzeCampContext() - builds CampContext from game state
   - CalculateFitness() - scores each opportunity 0-100 using ALL 4 layers
   - DetermineOpportunityBudget() - context-aware (garrison morning=2-3, siege=0-1)
   - Uses ContentOrchestrator.Instance for world state + pressure
   - Uses PlayerBehaviorTracker for learned preferences
   - Implements IsPlayerOnDuty() check

3. CREATE Opportunity Definitions:
   - ModuleData/Enlisted/camp_opportunities.json
   - 25+ opportunities covering all types
   - Natural language descriptions (what's HAPPENING, not game-y options)
   
   GOOD: "Veterans are drilling by the wagons. The sergeant is putting 
          them through their paces. Swords clash in rhythm."
          [Join the drill]
   
   BAD:  "Training Opportunity Available"
          [Start Training]

4. MODIFY EnlistedMenuBehavior.cs:
   - Add BuildCampLifeSection() method
   - Insert CAMP LIFE section into Camp Hub
   - Natural language presentation of what's happening
   - Context-appropriate empty states:
     * On duty: "You're on duty."
     * Marching: "The army is on the march. No time for leisure."
     * Siege: "The siege consumes all attention."

5. IMPLEMENT TIME-OF-DAY AWARENESS:
   Morning (6am-12pm): Training peak, productive tasks, budget 2-3
   Afternoon (12pm-6pm): Duty focus, orders issued, budget 0-1
   Evening (6pm-12am): Social peak, leisure, budget 1-2
   Night (12am-6am): Sleep, guard duty only, budget 0

6. IMPLEMENT WEEKLY RHYTHM:
   Days 1-4: Fresh after muster, more training/gambling
   Days 5-8: Routine settling, balanced
   Days 9-12: Muster approaching, pay tension, economic focus
   Muster Day: No camp life (structured muster sequence)

7. IMPLEMENT LEARNING SYSTEM:
   - Track engagement rates per opportunity type
   - Adapt scoring: +15 for types player engages, -10 for ignored
   - Maintain 70/30 split: 70% learned preference, 30% variety
   - Persist in save system

8. IMPLEMENT ORDER-DECISION TENSION SYSTEM:
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

9. IMPLEMENT PLAYER COMMITMENT TRACKING:
   - When player clicks to join activity, store commitment
   - Only ONE active commitment at a time
   - YOU section updates immediately: "You've agreed to join the {ACTIVITY} tonight."
   - DECISIONS menu greys out committed activity button
   - At scheduled time, activity fires (event/decision)
   - If player is on duty when activity fires, detection check
   - Clear commitment after activity completes or is cancelled
   - Commitment persists across save/load
   
   ```csharp
   public class PlayerCommitments
   {
       public string? ScheduledActivityId { get; set; }
       public CampaignTime? ScheduledTime { get; set; }
       public bool HasCommitment => ScheduledActivityId != null;
   }
   ```

10. IMPLEMENT INFO SECTION CACHING (MainMenuNewsCache.cs):
   - Create MainMenuNewsCache class to store cached section text
   - KINGDOM: Refresh on war/peace/siege events or every 24 hours
   - CAMP: Refresh on time period change (dawn/midday/dusk/night) or every 6 hours
   - YOU: Refresh on player state change (duty status, physical state, new forecast)
   - Use [NEW] tag with Warning color for changed content since last view
   - Don't regenerate on every menu open - cache until trigger fires
   - Call RefreshIfNeeded() when menu opens

Files to Create:
- src/Features/Interface/MainMenuNewsCache.cs
- src/Features/Camp/Models/CampOpportunity.cs
- src/Features/Camp/Models/PlayerCommitments.cs
- src/Features/Camp/Models/OpportunityType.cs
- src/Features/Camp/Models/CampContext.cs
- src/Features/Camp/Models/CampMood.cs
- src/Features/Camp/Models/DayPhase.cs
- src/Features/Camp/Models/OpportunityHistory.cs
- src/Features/Camp/CampOpportunityGenerator.cs
- ModuleData/Enlisted/camp_opportunities.json

Files to Modify:
- src/Features/Interface/Behaviors/EnlistedMenuBehavior.cs
- src/Features/Content/PlayerBehaviorTracker.cs
- ModuleData/Languages/enlisted_strings.xml
- Enlisted.csproj
- src/Mod.Core/SaveSystem/EnlistedSaveDefiner.cs

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

Start by reading the FULL camp-life-simulation.md spec (especially Edge Cases section). Implement in phases:
6A: Models + basic generation
6B: UI integration
6C: Intelligence (player state, variety)
6D: Learning system
6E: Polish (25+ opportunities, descriptions, testing)
```

---

## Phase 7: Content Variants (Post-Launch)

**Goal:** Add contextual variety to high-traffic content

```
I need you to add content variants for the Content Orchestrator.

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
```

---

## Phase 8: Progression System (Future)

**Goal:** Add probabilistic daily progression to escalation tracks (Medical Risk, Discipline, Pay Tension)

**Status:** Schema Ready - Implement when ready for organic escalation evolution

```
I need you to implement the Progression System for escalation tracks.

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

- Each prompt is self-contained but references the main docs
- Always start by reading the referenced docs
- The orchestrator plan has the complete technical specs
- Blueprint has coding standards that must be followed
- Phases 1-3 are critical path; 4-6 can be adjusted based on playtesting
- Phase 7 is post-launch polish
- Phase 8 (Progression System) is future expansion when ready for organic escalation evolution

