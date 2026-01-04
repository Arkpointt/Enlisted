# Orchestrator Opportunity Unification Spec

**Summary:** Architectural redesign to unify the ContentOrchestrator and CampOpportunityGenerator into a single coherent system where the Orchestrator owns opportunity scheduling. Opportunities are pre-scheduled 24 hours ahead, locked once generated, and hinted through narrative text.

**Status:** ✅ **IMPLEMENTED & BUG FIXES APPLIED**  
**Created:** 2026-01-03  
**Implemented:** 2026-01-03  
**Bug Fixes:** 2026-01-03, 2026-01-04, 2026-01-03 (Commitment Model)  
**Related Docs:** [Content System Architecture](Features/Content/content-system-architecture.md), [Camp Simulation System](Features/Campaign/camp-simulation-system.md)

---

## Recent Bug Fixes

### 2026-01-04: Phase-Aware Scheduling & Duplicate Prevention

**Change:** Fixed opportunity scheduling to properly generate candidates for each phase and prevent duplicates.

**Issues Fixed:**
1. **Wrong phase candidates** - Orchestrator was calling `GenerateCampLife()` which filtered by CURRENT phase, not target phase. Result: Only current phase got candidates, all other phases got zero.
2. **Duplicate opportunities** - Opportunities with `validPhases: ["Dusk", "Night"]` were scheduled for BOTH phases, showing "War Stories" and "War Stories (Night)" simultaneously.
3. **Past-phase commits** - Clicking a Dawn opportunity during Midday would "commit" it for future firing (wrong - Dawn is PAST, not future).

**Changes Made:**
- `CampOpportunityGenerator.cs`: Added `GenerateCandidatesForPhase(DayPhase targetPhase)` that overrides context phase to target phase
- `ContentOrchestrator.cs`: Updated `GenerateCandidatesForPhase()` to use new generator method
- `ContentOrchestrator.cs`: Changed `scheduledGuaranteedIds` → `alreadyScheduledIds` to track ALL opportunities, preventing any opportunity from appearing in multiple phases
- `ContentOrchestrator.cs`: Removed "guaranteed opportunity" special logic (now all opportunities compete on fitness)
- `EnlistedMenuBehavior.cs`: Added `IsPhaseFuture(scheduledPhase, currentPhase)` helper for proper phase comparison
- `EnlistedMenuBehavior.cs`: Updated commit logic to only commit if `!isCurrentPhase && isFuturePhase` (not past phases)
- `camp_opportunities.json`: Removed all `"immediate": true` flags

**Result:** 
- ✅ Each opportunity appears only once per day in its first valid phase
- ✅ All phases get proper candidates (not just current phase)
- ✅ Past-phase opportunities fire immediately (no commit)
- ✅ Build succeeded with 0 warnings, 0 errors

---

### 2026-01-03: Commitment Model Implementation

**Change:** Implemented player commitment model for forecasted opportunities.

**How It Works:**
1. **All today's opportunities visible** - Menu shows opportunities for all phases, not just current
2. **Click to commit** - Clicking a future-phase opportunity schedules it (greys out)
3. **Auto-fire on phase** - When the phase arrives, committed opportunities fire automatically
4. **Immediate for current/past phase** - Current or past-phase opportunities fire immediately when clicked
5. **Missed opportunities disappear** - Uncommitted opportunities vanish when their phase passes

**User Experience:**
- See "Card Game (Dusk)" in menu during Dawn
- Click to schedule → Shows "Scheduled for Dusk" message, option greys out
- At Dusk phase transition → Event popup fires automatically
- If not clicked by Dusk → Opportunity just disappears (missed it)
- Click Dawn opportunity during Midday → Fires immediately (Dawn was past)

**Changes Made:**
- `ContentOrchestrator.cs`: Added `PlayerCommitted` to `ScheduledOpportunity`, added `CommitToOpportunity()`, `GetAllTodaysOpportunities()`, `GetOpportunitiesToFireNow()`, `CleanupMissedOpportunities()`, `FireCommittedOpportunities()`
- `EnlistedMenuBehavior.cs`: Updated `GetCurrentDecisions()` to show all today's opportunities, `OnMainMenuDecisionSlotSelected()` to commit vs fire based on phase
- `DecisionManager.cs`: Added `ScheduledOpportunity` property to `DecisionAvailability`
- `CampOpportunityGenerator.cs`: Added baggage access filter (only show when access available)

**Result:** Players can now plan ahead, see what's coming, and commit to activities. All opportunities compete fairly through the orchestrator. Build succeeded with 0 warnings, 0 errors.

---

### 2026-01-04: Decision/Accordion Integration & Deprecation Cleanup

**Issues Fixed:**
1. **Decisions firing as popups instead of appearing in accordion** - `MapIncidentManager` was selecting decision-category events because they had `context="Any"` (default). Fixed by filtering out "decision" and "onboarding" category events in `TryDeliverIncident()`.
2. **Deprecation warnings for `GenerateCampLife()` calls** - Two call sites were still using the deprecated method directly instead of going through the Orchestrator.

**Changes Made:**
- `MapIncidentManager.cs`: Added filter to exclude "decision" and "onboarding" category events from map incidents
- `CampOpportunityGenerator.cs`: `GetUpcomingHints()` now uses only cached opportunities (no regeneration)
- `EnlistedNewsBehavior.cs`: `GetCampActivityFlavor()` now uses `ContentOrchestrator.GetCurrentPhaseOpportunities()`

**Result:** Build succeeded with 0 warnings, 0 errors.

### 2026-01-03: Initial Bug Fixes

**Issues Fixed:**
1. **Decision tree changing when lord leaves castle** - Menu was calling deprecated `GenerateCampLife()` which used current context instead of locked schedule
2. **Decisions not disappearing after selection** - Consumption failing for logistics decisions, menu refresh too narrow

**Changes Made:**
- `EnlistedMenuBehavior.cs`: Replaced direct generator calls with Orchestrator queries, added decision ID fallback consumption
- `ContentOrchestrator.cs`: Added `ConsumeOpportunityByDecisionId()` method for fallback consumption
- `EventDeliveryManager.cs`: Expanded menu refresh scope to all `enlisted_*` menus

**Result:** Build succeeded with 0 errors. All testing checklist items verified.

---

## Problem Statement

### The Bug

When the player is in a castle and the lord leaves, all decisions/opportunities vanish from the menu mid-session. This creates a jarring UX where content the player was about to interact with disappears.

### Root Cause

Two systems are in conflict:

**System 1: CampOpportunityGenerator**
- Generates opportunities on-demand when menu requests them
- Caches by phase, invalidates on phase change
- Has `PlayerCommitments` for player-scheduled activities

**System 2: ContentOrchestrator**
- Documented as the central coordinator for content pacing
- Supposed to "own" content delivery
- Currently only provides world state analysis, doesn't control opportunity lifecycle

**The Conflict:**
- Menu init triggers `DecisionManager.GetAvailableOpportunities()`
- This calls `CampOpportunityGenerator.GenerateCampLife()`
- Generator calculates budget based on **current context** (lord situation, phase)
- If context changed (lord left, phase changed), budget may be 0
- Result: opportunities that WERE visible disappear

### Why It Feels Wrong

The docs state: "The orchestrator does NOT fire narrative events directly - it provides world state analysis..."

But this creates a system where:
- Content appears/disappears based on instantaneous context
- No stability or predictability for the player
- No immersive foreshadowing of upcoming activities
- The Orchestrator doesn't truly "orchestrate" - it just analyzes

---

## Proposed Architecture

### Core Principle: Orchestrator Owns Opportunity Lifecycle

The ContentOrchestrator should:
1. **Pre-schedule** opportunities 24 hours ahead (on daily tick)
2. **Lock** opportunities once scheduled (no regeneration)
3. **Hint** upcoming opportunities through narrative text
4. **Fire** opportunities at scheduled phase boundaries
5. **Clear** only after firing or phase completion

### Data Flow

```
Daily Tick (6am)
    ↓
ContentOrchestrator.ScheduleOpportunities()
    ↓
For each phase in next 24h:
    ├─ Analyze context for that phase (predicted)
    ├─ Call CampOpportunityGenerator.GenerateCandidates(predictedContext)
    ├─ Score and select top N
    └─ Store in _scheduledOpportunities[phase]
    ↓
_scheduledOpportunities is now LOCKED for the day
    ↓
BuildNarrativeHints() → inject into Company Reports
    ↓
At phase boundary:
    ├─ Fire scheduled opportunities for this phase
    └─ Mark as Fired (remove from menu)
    ↓
Menu displays current phase's scheduled opportunities
    ↓
Player interacts → opportunity consumed, removed
    ↓
Next daily tick → schedule next day
```

### New Data Model

```csharp
// ContentOrchestrator.cs

/// <summary>
/// Pre-scheduled opportunities for each phase. Generated once per day.
/// Locked until consumed, fired, or day ends.
/// </summary>
private Dictionary<DayPhase, List<ScheduledOpportunity>> _scheduledOpportunities;

/// <summary>
/// Day the current schedule was generated. Used to detect new day.
/// </summary>
private int _scheduledDay = -1;

/// <summary>
/// A single scheduled opportunity.
/// </summary>
public class ScheduledOpportunity
{
    /// <summary>Opportunity definition ID (e.g., "opp_card_game").</summary>
    public string OpportunityId { get; set; }
    
    /// <summary>Target decision to fire (e.g., "dec_gamble_cards").</summary>
    public string TargetDecisionId { get; set; }
    
    /// <summary>Phase when this opportunity is available.</summary>
    public DayPhase Phase { get; set; }
    
    /// <summary>Display name for menu.</summary>
    public string DisplayName { get; set; }
    
    /// <summary>Narrative hint for Daily Brief (e.g., "A card game is forming"). Sourced from opportunity JSON hint field. Automatically categorized as camp rumor or personal hint.</summary>
    public string NarrativeHint { get; set; }
    
    /// <summary>True if player has engaged with this opportunity.</summary>
    public bool Consumed { get; set; }
    
    /// <summary>Fitness score when generated (for debugging).</summary>
    public float FitnessScore { get; set; }
}
```

### Key Methods

```csharp
// ContentOrchestrator.cs

/// <summary>
/// Called on daily tick. Schedules opportunities for next 24 hours.
/// </summary>
private void ScheduleOpportunities()
{
    var currentDay = (int)CampaignTime.Now.ToDays;
    
    // Only schedule once per day
    if (_scheduledDay == currentDay)
        return;
    
    _scheduledDay = currentDay;
    _scheduledOpportunities = new Dictionary<DayPhase, List<ScheduledOpportunity>>();
    
    // Get current world situation
    var worldSituation = WorldStateAnalyzer.AnalyzeSituation();
    
    // Schedule for each phase
    foreach (DayPhase phase in Enum.GetValues(typeof(DayPhase)))
    {
        var scheduled = SchedulePhaseOpportunities(phase, worldSituation);
        _scheduledOpportunities[phase] = scheduled;
        
        ModLogger.Debug(LogCategory, 
            $"Scheduled {scheduled.Count} opportunities for {phase}: " +
            $"[{string.Join(", ", scheduled.Select(s => s.OpportunityId))}]");
    }
}

/// <summary>
/// Schedules opportunities for a specific phase.
/// </summary>
private List<ScheduledOpportunity> SchedulePhaseOpportunities(
    DayPhase phase, 
    WorldSituation worldSituation)
{
    var generator = CampOpportunityGenerator.Instance;
    if (generator == null)
        return new List<ScheduledOpportunity>();
    
    // Create predicted context for this phase
    var predictedContext = PredictContextForPhase(phase, worldSituation);
    
    // Get budget for this phase
    int budget = generator.DetermineOpportunityBudget(worldSituation, predictedContext);
    
    if (budget <= 0)
        return new List<ScheduledOpportunity>();
    
    // Generate candidates
    var candidates = generator.GenerateCandidatesForPhase(phase, worldSituation, predictedContext);
    
    // Select top N
    var selected = candidates
        .OrderByDescending(c => c.FitnessScore)
        .Take(budget)
        .Select(c => new ScheduledOpportunity
        {
            OpportunityId = c.Id,
            TargetDecisionId = c.TargetDecisionId,
            Phase = phase,
            DisplayName = GetOpportunityDisplayName(c),
            NarrativeHint = GetLocalizedHint(c), // Reads from opportunity JSON hint/hintId
            FitnessScore = c.FitnessScore
        })
        .ToList();
    
    return selected;
}

/// <summary>
/// Gets opportunities for the current phase. Returns locked list, no regeneration.
/// </summary>
public IReadOnlyList<ScheduledOpportunity> GetCurrentPhaseOpportunities()
{
    var currentPhase = WorldStateAnalyzer.GetCurrentDayPhase();
    
    if (_scheduledOpportunities == null || 
        !_scheduledOpportunities.TryGetValue(currentPhase, out var opportunities))
    {
        return new List<ScheduledOpportunity>();
    }
    
    // Return only non-consumed opportunities
    return opportunities.Where(o => !o.Consumed).ToList();
}

/// <summary>
/// Gets narrative hints for upcoming opportunities (for Company Reports).
/// </summary>
public IEnumerable<string> GetUpcomingHints()
{
    if (_scheduledOpportunities == null)
        yield break;
    
    var currentPhase = WorldStateAnalyzer.GetCurrentDayPhase();
    
    // Get hints for current and next phases
    foreach (var phase in GetPhasesAhead(currentPhase, 2))
    {
        if (_scheduledOpportunities.TryGetValue(phase, out var opportunities))
        {
            foreach (var opp in opportunities.Where(o => !o.Consumed))
            {
                if (!string.IsNullOrEmpty(opp.NarrativeHint))
                    yield return opp.NarrativeHint;
            }
        }
    }
}

/// <summary>
/// Marks an opportunity as consumed (player interacted with it).
/// </summary>
public void ConsumeOpportunity(string opportunityId)
{
    if (_scheduledOpportunities == null)
        return;
    
    foreach (var phase in _scheduledOpportunities.Values)
    {
        var opp = phase.FirstOrDefault(o => o.OpportunityId == opportunityId);
        if (opp != null)
        {
            opp.Consumed = true;
            ModLogger.Info(LogCategory, $"Opportunity consumed: {opportunityId}");
            return;
        }
    }
}
```

### Narrative Hint Integration

Hints are split into two categories for optimal UX:

**Camp Rumors** → Company Reports (with Rumor styling)
- Social activities others are doing (card games, dice, drills, etc.)
- Styled with `<span style="Rumor">` (muted lavender #B8A8D8) for visual distinction
- Examples: "Torgan mentioned a card game tonight." "The veterans are running dice."

**Personal Hints** → Your Status section
- Player-specific needs (medical care, rest, personal activities)
- Default text styling
- Examples: "Your condition needs attention." "Your hammock awaits."

```csharp
// In EnlistedNewsBehavior.BuildDailyBriefSection()

// Company Reports section
var campRumors = BuildCampRumorsLine(); // Filters for social hints, applies Rumor styling
if (!string.IsNullOrWhiteSpace(campRumors))
{
    companyParts.Add(campRumors);
}

// Your Status section  
var personalHints = BuildPersonalHintsLine(); // Filters for player-specific hints
if (!string.IsNullOrWhiteSpace(personalHints))
{
    playerParts.Add(personalHints);
}
```

**Example Output:**
> The company is in fair spirits. Supplies hold steady.
> 
> *A card game is forming by the wagons this evening.*
> *The veterans mentioned sparring practice at dawn.*

### Menu Integration Changes

```csharp
// EnlistedMenuBehavior.cs - simplified

// REMOVE: _cachedMainMenuDecisions and its cache clearing
// INSTEAD: Just ask Orchestrator for current phase opportunities

private List<DecisionAvailability> GetCurrentDecisions()
{
    var result = new List<DecisionAvailability>();
    
    // Get locked opportunities from Orchestrator
    var opportunities = ContentOrchestrator.Instance?.GetCurrentPhaseOpportunities();
    if (opportunities != null)
    {
        foreach (var opp in opportunities)
        {
            result.Add(ConvertToDecisionAvailability(opp));
        }
    }
    
    // Add logistics decisions (baggage access, etc.) - always available
    var logistics = DecisionManager.Instance?
        .GetAvailableDecisionsForSection("logistics")
        .Where(d => d.IsVisible && d.IsAvailable);
    if (logistics != null)
    {
        result.AddRange(logistics);
    }
    
    return result;
}
```

### CampOpportunityGenerator Role Change

The CampOpportunityGenerator becomes a **candidate generator** rather than the owner of opportunity state:

```csharp
// CampOpportunityGenerator.cs

/// <summary>
/// Generates candidate opportunities for a specific phase.
/// Called by Orchestrator during daily scheduling.
/// Does NOT cache - Orchestrator owns the cached state.
/// </summary>
public List<CampOpportunity> GenerateCandidatesForPhase(
    DayPhase phase,
    WorldSituation worldSituation,
    CampContext predictedContext)
{
    // ... existing candidate generation logic ...
    // ... fitness scoring ...
    // Returns scored candidates, Orchestrator decides what to schedule
}

// GenerateCampLife() is now `internal` - only Orchestrator calls it
// The Orchestrator owns the opportunity lifecycle
```

---

## Narrative Hint Examples

Hints are phase-appropriate and immersive, using dynamic placeholders for personalization:

| Opportunity | Phase | Category | Hint Example |
|-------------|-------|----------|--------------|
| opp_weapon_drill | Dawn | Camp Rumor | "{VETERAN_1_NAME} mentioned morning drill." |
| opp_sparring | Dawn | Camp Rumor | "{COMRADE_NAME} is looking for a sparring partner." |
| opp_card_game | Dusk | Camp Rumor | "{SOLDIER_NAME} is running a card game tonight." |
| opp_seek_medical_care | Any | Personal | "Your condition needs attention." |
| opp_rest_hammock | Night | Personal | "Your hammock awaits." |
| opp_card_game | Dusk | "A card game is forming by the wagons this evening." |
| opp_campfire_stories | Dusk | "Men are gathering around the fire for stories." |
| opp_dice_game | Night | "Dice are rattling somewhere in camp tonight." |
| opp_rest | Any | "You could use some rest." |

---

## Migration Path

### Phase 1: Add Orchestrator Scheduling (Non-Breaking) ✅ COMPLETE
1. ✅ Add `_scheduledOpportunities` and scheduling methods to ContentOrchestrator
2. ✅ Add `GetCurrentPhaseOpportunities()` method
3. ✅ Add narrative hint generation (reads from `hint`/`hintId` JSON fields)
4. ✅ Update opportunity JSON schema to include `hint`/`hintId` fields (see [event-system-schemas.md](Features/Content/event-system-schemas.md#narrative-hints-orchestrator-pre-scheduling))
5. ✅ Keep existing `GenerateCampLife()` working for backward compatibility

### Phase 2: Wire Menu to Orchestrator ✅ COMPLETE
1. ✅ Change menu to call `ContentOrchestrator.GetCurrentPhaseOpportunities()`
2. ✅ Remove menu's own cache (`_cachedMainMenuDecisions`)
3. ✅ Add `ConsumeOpportunity()` call when player selects an opportunity

### Phase 3: Deprecate Old Flow ✅ COMPLETE
1. ✅ Make `CampOpportunityGenerator.GenerateCampLife()` internal (only Orchestrator calls it)
2. ✅ Remove `DecisionManager.GetAvailableOpportunities()` (replaced by Orchestrator)
3. ✅ Update docs to reflect new architecture
4. ✅ Migrate remaining callers: `GetUpcomingHints()` and `GetCampActivityFlavor()` now use Orchestrator
5. ✅ Filter decisions/onboarding from MapIncidentManager to prevent accordion bypass

### Phase 4: Enhance Scheduling
1. Add weather/event awareness to scheduling
2. Add player preference learning to scheduling
3. Add "locked in" vs "tentative" hint styling

---

## Benefits

1. **No jarring disappearance** - opportunities locked once scheduled
2. **Immersive foreshadowing** - hints in narrative text
3. **Clear ownership** - Orchestrator owns content lifecycle
4. **Predictable UX** - quiet phases are intentional and communicated
5. **Single source of truth** - no conflicting caches
6. **Player can plan** - knows what's coming via hints

---

## Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| Predictions may be wrong (siege starts mid-day) | Accept stale opportunities for current day; next day corrects |
| Too many hints clutter narrative | Limit to 2 hints, only for current/next phase |
| Save/load complexity | Scheduled opportunities don't persist; regenerate on load |
| Performance of daily scheduling | Only runs once at 6am; candidates already calculated efficiently |
| Existing opportunities lack hint field | Backward compatible: opportunities without hints work fine, just no foreshadowing |

---

## Testing Checklist

- [x] Opportunities persist when lord leaves settlement
- [x] Opportunities persist when phase changes mid-view
- [x] Hints appear in Company Reports
- [x] Consumed opportunities disappear correctly
- [x] Quiet phases show no opportunities (intentional)
- [x] New day generates fresh schedule
- [x] Save/load regenerates schedule correctly

## Logging & Verification

**Enable Debug Logging:** Set `ModLogger` to Debug level for category `"Orchestrator"` to see detailed scheduling.

**Daily Schedule Log (6am):**
```
═══ Daily Opportunity Schedule ═══
Context: PeacetimeGarrison, Activity=Routine, Phase=Dawn
  Dawn: 3 opportunities (1 guaranteed) → [opp_weapon_drill(65), opp_sparring(58), opp_baggage_access*]
  Midday: 2 opportunities (0 guaranteed) → [opp_mentor_recruit(72), opp_help_wounded(64)]
  Dusk: 3 opportunities (0 guaranteed) → [opp_card_game(78), opp_tavern_drink(71), opp_stories(65)]
  Night: 1 opportunities (0 guaranteed) → [opp_rest(55)]
Schedule complete: 9 total (1 guaranteed, 8 fitness-selected)
══════════════════════════════════
```

**Menu Query Log:**
```
GetCurrentPhaseOpportunities: 3 available for Dawn
Menu querying Orchestrator: 3 opportunities available
GetCurrentDecisions: 4 total decisions ready for menu display
```

**Consumption Log:**
```
✓ Opportunity consumed: opp_card_game (phase=Dusk, fitness=78.0)
  Recorded engagement for learning system (type=Social)
```

**Hints Log:**
```
GetUpcomingHints: 2 hints provided: [opp_card_game (Dusk), opp_stories (Dusk)]
BuildCampRumorsLine: 2 camp rumors displayed
```

---

## Files to Modify

| File | Changes |
|------|---------|
| `src/Features/Content/ContentOrchestrator.cs` | Add scheduling system, hint generation |
| `src/Features/Camp/CampOpportunityGenerator.cs` | Expose candidate generation, deprecate lifecycle ownership |
| `src/Features/Interface/Behaviors/EnlistedMenuBehavior.cs` | Remove cache, use Orchestrator |
| `src/Features/Content/DecisionManager.cs` | Deprecate `GetAvailableOpportunities()` |
| `src/Features/Interface/Behaviors/EnlistedNewsBehavior.cs` | Add hint integration |

---

**End of Specification**
