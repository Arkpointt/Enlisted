# Content Orchestrator Implementation Prompts

**Summary:** Copy-paste prompts for each implementation phase of the Content Orchestrator. Use these to guide implementation with your preferred coding assistant.

**Status:** 📋 Reference  
**Last Updated:** 2025-12-24  
**Related Docs:** [Content Orchestrator Plan](../Features/Content/content-orchestrator-plan.md), [BLUEPRINT](../BLUEPRINT.md)

---

## Index

| Phase | Description | Model |
|-------|-------------|-------|
| [Phase 1: Foundation](#phase-1-foundation) | Build core infrastructure | Opus 4 |
| [Phase 2: Selection](#phase-2-content-selection-integration) | Connect to content selection | Opus 4 |
| [Phase 3: Cutover](#phase-3-cutover) | Switch to orchestrator | Sonnet 4 |
| [Phase 4: Orders](#phase-4-orders-integration) | Coordinate order timing | Sonnet 4 |
| [Phase 5: UI](#phase-5-ui-integration-company-report) | Add Company Report section | Sonnet 4 |
| [Phase 6: Variants](#phase-6-content-variants-post-launch) | Add content variants (JSON) | Sonnet 4 |
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
| Phase 6 - Variants | Low (JSON only) | Claude Sonnet 4 |

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

File Locations:
- Main classes: src/Features/Content/
- Models: src/Features/Content/Models/

Critical Requirements:
- Add ALL new files to Enlisted.csproj manually (old-style project)
- Verify APIs against local decompile at C:\Dev\Enlisted\Decompile\
- Use ModLogger with category "Orchestrator"
- Follow ReSharper recommendations
- Comments describe current behavior (no "Phase X added" style)

Acceptance Criteria:
- Orchestrator receives daily ticks when enlisted
- World state analysis logs correctly (LifePhase, ActivityLevel)
- Pressure calculation logs sources and total
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

Acceptance Criteria:
- Orchestrator can query EventCatalog with world context
- Fitness scoring considers world situation + player preferences
- Logs show clear comparison: "Old system would pick X, new system picks Y"
- Player preferences begin tracking from choices

Don't break the existing system. Log comparisons only.
```

---

## Phase 3: Cutover

**Goal:** Switch from old system to orchestrator

```
I need you to implement Phase 3 of the Content Orchestrator - the cutover.

Read these docs first:
- docs/Features/Content/content-orchestrator-plan.md (Phase 3 + Migration section)
- docs/BLUEPRINT.md (config file conventions)

Phase 3 Tasks:
1. Add feature flag in enlisted_config.json: orchestrator.enabled
2. When enabled: orchestrator handles all narrative events
3. Disable EventPacingManager scheduled checks (remove NextNarrativeEventWindow logic)
4. Remove evaluation hours check from GlobalEventPacer
5. Remove quiet day random roll from GlobalEventPacer
6. Update enlisted_config.json with new orchestrator config section
7. Set IsQuietDay based on world state, not random roll

Files to Modify:
- src/Features/Content/EventPacingManager.cs (remove schedule logic, keep chain events)
- src/Features/Content/GlobalEventPacer.cs (remove evaluation hours, quiet day roll)
- src/Features/Content/ContentOrchestrator.cs (take over from EventPacingManager)
- src/Features/Escalation/EscalationState.cs (remove unused schedule fields)
- ModuleData/Enlisted/enlisted_config.json (add orchestrator section)

Keep These Behaviors:
- Grace period check (lines 79-94 in EventPacingManager)
- Chain event check (lines 96-97)
- Safety limits (max_per_day, max_per_week, min_hours_between)
- Per-event and per-category cooldowns

Remove These Behaviors:
- NextNarrativeEventWindow checking
- Evaluation hours (8, 14, 20)
- Random quiet day roll (15% chance)
- event_window_min_days / event_window_max_days

Acceptance Criteria:
- Feature flag toggles between old and new system
- When enabled: content fires based on world state
- When disabled: old schedule system works
- Safety limits still prevent spam
- Garrison feels quiet, campaigns feel busy
```

---

## Phase 4: Orders Integration

**Goal:** Coordinate order timing with orchestrator

```
I need you to implement Phase 4 of the Content Orchestrator - Orders integration.

Read these docs first:
- docs/Features/Content/content-orchestrator-plan.md (Phase 4 section)
- docs/Features/Orders/orders-system.md (how orders work)

Phase 4 Tasks:
1. Integrate OrderManager with orchestrator timing
2. Orders compete in same frequency budget as events
3. Orders still have contextual timing (siege = more frequent)
4. Test order + event coordination (don't overwhelm player)
5. Update WorldSituation to inform order timing

Files to Modify:
- src/Features/Orders/Behaviors/OrderManager.cs (integrate timing)
- src/Features/Content/ContentOrchestrator.cs (coordinate orders + events)

Key Considerations:
- Orders should arrive every 5-7 days (modified by situation)
- Siege/campaign = more frequent orders
- Garrison = fewer orders
- Orders + events share frequency budget (don't double-dip)
- Keep order selection and execution logic unchanged

Acceptance Criteria:
- Orders arrive realistically based on world state
- Orders don't flood player during event-heavy periods
- Order frequency feels appropriate for situation
- Both systems coordinate smoothly
```

---

## Phase 5: UI Integration (Company Report)

**Goal:** Add player-facing transparency via Company Report section

```
I need you to implement Phase 5 of the Content Orchestrator - UI Integration.

Read these docs first:
- docs/Features/Content/content-orchestrator-plan.md (Phase 5 section - has full UI spec)
- docs/Features/UI/ui-systems-master.md (Camp Hub structure)
- docs/Features/UI/news-reporting-system.md (Daily Brief system)

Phase 5 Tasks:
1. Add GetOrchestratorRhythmFlavor() method to ContentOrchestrator
2. Add BuildCompanyReportSection() method to EnlistedNewsBehavior
3. Split BuildDailyBriefSection() into:
   - BuildCompanyReportSection() (orchestrator status)
   - BuildDailyNewsSection() (kingdom macro context)
4. Update EnlistedMenuBehavior Camp Hub header:
   - Remove "Lord:" and "Your Rank:" header lines
   - Add COMPANY REPORT section with rhythm flavor
   - Rename existing Daily Brief to DAILY NEWS
   - Keep RECENT ACTIONS unchanged

Files to Modify:
- src/Features/Content/ContentOrchestrator.cs (add GetOrchestratorRhythmFlavor)
- src/Features/Interface/Behaviors/EnlistedNewsBehavior.cs (add BuildCompanyReportSection)
- src/Features/Interface/Behaviors/EnlistedMenuBehavior.cs (update header layout)

Company Report Content:
- Camp Status header: "[Icon] CAMP STATUS: [Rhythm] - [Activity Level]"
- Flavor text explaining WHY (observable world facts)
- NO soldier counts (Daily News has casualties)
- NO stats (Reports submenu has those)

Icons:
- ⚙️ = Quiet (Garrison)
- 🏹 = Active (Campaign)
- ⚔️ = Intense (Siege)
- ⚠️ = Crisis

Example Output:
```
⚙️ CAMP STATUS: Garrison - Quiet

Your lord holds at Pravend with no threats on the horizon. 
The camp has settled into routine. Little disturbs the 
daily rhythm.
```

Acceptance Criteria:
- Player sees Camp Status at top of Camp Hub
- Status matches Daily News (same world state sources)
- Player understands WHY event frequency varies
- Recent Actions section unchanged
```

---

## Phase 6: Content Variants (Post-Launch)

**Goal:** Add contextual variety to high-traffic content

```
I need you to add content variants for the Content Orchestrator.

Read these docs first:
- docs/Features/Content/content-orchestrator-plan.md (Phase 6 section)
- docs/Features/Content/event-system-schemas.md (Content Variants Pattern section)

Phase 6 Tasks:
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

