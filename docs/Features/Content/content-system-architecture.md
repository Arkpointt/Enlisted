# Content System Architecture

**Summary:** The unified content system manages all narrative content (events, decisions, orders, map incidents) through a world-state driven orchestration pipeline. The Content Orchestrator analyzes your lord's situation and coordinates content delivery to match military reality: garrison duty is quiet, campaigns are busy, sieges are intense. All content uses JSON definitions, XML localization, requirement checking, and native Bannerlord effect integration.

**Status:** ✅ **IMPLEMENTED** - ContentOrchestrator, OrderProgressionBehavior, 85 order events active  
**Last Updated:** 2026-01-01 (Native API usage for travel context detection)  
**Implementation:** `src/Features/Content/ContentOrchestrator.cs`, `src/Features/Orders/Behaviors/OrderProgressionBehavior.cs`, `src/Features/Content/WorldStateAnalyzer.cs`  
**Related Docs:** [Event Catalog](../../Content/event-catalog-by-system.md), [Training System](../Combat/training-system.md), [Order Progression System](../../AFEATURE/order-progression-system.md), [Content Orchestrator Plan](../../AFEATURE/content-orchestrator-plan.md)

**RECENT CHANGES (2026-01-01):**
- **Travel context detection** now uses native `party.IsCurrentlyAtSea` property directly (removed reflection overhead)
- **WorldStateAnalyzer** simplified - no more reflection-based Warsails DLC detection
- **OrderCatalog** uses direct property access for sea/land context variant selection
- **Result**: Cleaner, faster, more maintainable code verified against native decompile

---

## ✅ World-State Orchestration (IMPLEMENTED 2025-12-30)

The content system uses **world-state driven orchestration** instead of schedule-based pacing. The Content Orchestrator analyzes your lord's situation (garrison/campaign/siege), war status, and company condition to provide contextually appropriate content.

**Core Architecture (All Implemented):**
- **ContentOrchestrator** - Analyzes world state, calculates activity levels, coordinates all systems ✅
- **WorldStateAnalyzer** - Detects lord situation, war stance, determines context ✅
- **SimulationPressureCalculator** - Tracks company condition and pressure sources ✅
- **PlayerBehaviorTracker** - Learns player preferences for better content selection ✅
- **OrderProgressionBehavior** - Multi-day orders with phase progression and event injection ✅
- **EventSelector, EventDeliveryManager, EventRequirementChecker** - Reused with world-state awareness ✅

**Implementation Benefits:**
- Content frequency matches simulation reality (not arbitrary timers)
- Activity levels drive order event frequency (quiet/routine/active/intense)
- 85 order events fire contextually during 16 different order types
- Player behavior learning improves content selection
- Native Bannerlord effects integrated (traits, skills, morale, health)
- Camp opportunities dynamically generated based on context

---

## Index

1. [Overview](#overview)
2. [Content Orchestrator](#content-orchestrator)
3. [Content Types](#content-types)
4. [System Architecture](#system-architecture)
5. [Content Pipeline](#content-pipeline)
6. [Requirement System](#requirement-system)
7. [Effect System](#effect-system)
8. [Native Effect Integration](#native-effect-integration)
9. [Delivery Contexts](#delivery-contexts)
10. [JSON Structure](#json-structure)
11. [Localization](#localization)
12. [Implementation Files](#implementation-files)

---

## Overview

The content system provides a unified pipeline for delivering narrative content:

**Content Types:**
- **Orders** - Military directives from chain of command (order_* prefix)
- **Decisions** - Player-initiated Camp Hub menu actions (dec_* prefix, inline)
- **Player Events** - Player-initiated popup events (player_* prefix, inquiry popup)
- **Events** - Context-triggered narrative moments (automatic)
- **Map Incidents** - Native Bannerlord incident integration
- **Content Variants** - Context-specific versions of events/decisions (future enhancement)

**Core Responsibilities:**
- Load content from JSON files at startup
- Check requirements before delivery
- Apply effects after player choices
- Integrate with escalation, reputation, and progression
- Provide news feed and UI feedback
- Select best-fitting content variant for current situation (orchestrator)

**Design Philosophy:**
- Data-driven (JSON + XML)
- Extensible (new content without code changes)
- Integrated (shares state with enlistment/QM systems)
- Validated (requirement checks prevent invalid delivery)
- Context-aware (variants selected automatically via requirements)
- World-state driven (content frequency matches simulation reality)

---

## Content Orchestrator

The Content Orchestrator replaced schedule-driven event pacing with intelligent world-state analysis. Instead of firing events on timers, the system analyzes your lord's actual situation and coordinates content delivery to match military reality.

### Core Components

**ContentOrchestrator** (`src/Features/Content/ContentOrchestrator.cs`)
- CampaignBehaviorBase that receives daily tick at 6am
- Analyzes world state and calculates activity level
- Provides activity levels to OrderProgressionBehavior for order event frequency
- Coordinates forecast generation for player information
- Updates camp opportunity generation based on context

**WorldStateAnalyzer** (`src/Features/Content/WorldStateAnalyzer.cs`)
- Static class that analyzes current game state
- Determines LordSituation: InGarrison, InCampaign, InSiege, InBattle, Defeated
- Determines WarStance: Peacetime, ActiveWar, DesperateWar, MultiWarStrained
- Determines LifePhase: Routine, Strained, Crisis based on company needs
- Calculates ActivityLevel: Quiet, Routine, Active, Intense
- Maps contexts for event filtering (Camp/War/Siege/Any)

**SimulationPressureCalculator** (`src/Features/Content/SimulationPressureCalculator.cs`)
- Static class that tracks realistic pressure sources
- Adds pressure from: low supplies, wounded company, failed orders, enemy territory, angry lord, high discipline
- Reduces pressure from: pay received, battle victory, promotions, rest, friendly territory
- Returns 0-100 pressure value for orchestrator decisions
- Pressure emerges from simulation, not manufactured for drama

**PlayerBehaviorTracker** (`src/Features/Content/PlayerBehaviorTracker.cs`)
- Static class that learns player preferences
- Tracks behavior patterns: helps comrades, takes risks, follows orders, prioritizes gold
- Builds preference profile: CombatVsSocial, RiskyVsSafe, LoyalVsSelfServing
- Informs content selection to deliver what player engages with
- Saves/loads with campaign data

### Activity Level System

The orchestrator provides activity levels that modify order event frequency:

| Activity Level | Lord Situation | Order Event Modifier | Description |
|----------------|---------------|---------------------|-------------|
| **Quiet** | Garrison + Peacetime | ×0.3 | Long garrison duty, routine days, little happens |
| **Routine** | Garrison + War, Marching | ×0.6 | Normal military operations, moderate activity |
| **Active** | Campaign + War | ×1.0 | Active campaigning, regular events during orders |
| **Intense** | Siege, Desperate War | ×1.5 | Crisis situations, high-frequency events |

Activity levels are provided to `OrderProgressionBehavior` which fires events during duty execution. This is the PRIMARY content delivery mechanism - order events happen while you're on duty, based on how busy military life is.

### Content Model (Implemented)

The orchestrator coordinates three content tracks:

**1. Order Events (Automatic, During Duty)**
- Fire during order execution based on activity level
- 85 order events across 16 order types
- Weighted by world_state requirements (siege_attacking, war_marching, etc.)
- Frequency controlled by activity modifiers (Quiet = rare, Intense = frequent)

**2. Camp Decisions (Player-Initiated)**
- Available in DECISIONS menu when player chooses
- Dynamically generated opportunities based on world state
- 29 camp opportunities with fitness scoring
- Player controls timing completely

**3. Map Incidents (Context-Triggered)**
- Fire on specific triggers (battles, settlements, encounters)
- Already context-driven, unchanged by orchestrator
- Immediate delivery, no frequency gating

### World State Data Flow

```
Daily Tick (6am) → ContentOrchestrator.OnDailyTick()
  ↓
WorldStateAnalyzer.AnalyzeSituation()
  ├─ Detect lord's situation (garrison/campaign/siege)
  ├─ Analyze war status (peace/active/desperate)
  ├─ Check company condition (supplies/morale/fatigue)
  └─ Return: WorldSituation
  ↓
CalculateActivityLevel(WorldSituation)
  ├─ Garrison + Peacetime = Quiet
  ├─ Campaign + War = Active
  ├─ Siege = Intense
  └─ Return: ActivityLevel (Quiet/Routine/Active/Intense)
  ↓
ProvideToOrderSystem()
  └─ OrderProgressionBehavior uses activity level for event slots
  ↓
GenerateForecasts()
  └─ Create NOW + AHEAD text for main menu display
  ↓
UpdateCampOpportunities()
  └─ Refresh DECISIONS menu based on current context
```

### Configuration

Orchestrator settings in `ModuleData/Enlisted/enlisted_config.json`:

```json
{
  "orchestrator": {
    "enabled": true,
    "activity_modifiers": {
      "Quiet": 0.3,
      "Routine": 0.6,
      "Active": 1.0,
      "Intense": 1.5
    },
    "order_scheduling": {
      "warning_hours_24": 24,
      "warning_hours_8": 8,
      "warning_hours_2": 2
    }
  }
}
```

**Note:** Order scheduling (Phases 9-10) is not yet implemented. Warning system will provide advance notice of upcoming orders for fast-forward playability.

### What This Replaced

**Old System (Removed):**
- Schedule-driven event windows (fire every 3-5 days on timer)
- Evaluation hours (specific times when events could fire)
- Random quiet day rolls (15% chance to suppress events)
- NextNarrativeEventWindow tracking in EscalationState

**New System (Current):**
- World-state analysis drives all decisions
- Activity level matches simulation reality
- Content frequency emerges from context
- No arbitrary timing restrictions

---

## Camp Routine Orchestration

**Added:** 2025-12-31  
**Implementation:** `src/Features/Content/ContentOrchestrator.cs` (override system), `src/Features/Camp/CampRoutineProcessor.cs` (automatic processing)  
**Config Files:** `orchestrator_overrides.json`, `routine_outcomes.json`

The orchestrator extends beyond order event coordination to actively manage the camp's daily routine. It serves as the "brain" that responds to company needs and injects variety into camp life.

### Schedule Override System

The ContentOrchestrator can override the normal camp schedule (`CampScheduleManager`) when company conditions demand it:

**Need-Based Overrides (Immediate Response):**
```
Supplies < 30  → Foraging Duty (replaces training)
Rest < 20      → Extended Rest (skips formations)
Readiness < 40 → Emergency Drill (extra training)
Morale < 25    → Light Duty (morale recovery)
```

**Flow:**
```
OnPhaseChanged()
    ↓
CheckForScheduleOverride(phase)
    ↓
┌──────────────────────┬───────────────────────┐
│ Critical Need?       │ Variety Due?          │
└──────────────────────┴───────────────────────┘
    ↓                       ↓
NeedBasedOverride      VarietyInjection
    ↓                       ↓
    └───────────┬───────────┘
                ↓
        OrchestratorOverride
                ↓
CampScheduleManager.ApplyOverride()
```

**Implementation:**
- `ContentOrchestrator.CheckForScheduleOverride()` - Checks triggers, returns override
- `ContentOrchestrator.ShouldInjectVariety()` - Tracks timing for variety
- `ContentOrchestrator.SelectVarietyInjection()` - Picks variety activity
- `OrchestratorOverride` model - Data structure for overrides

### Variety Injection System

Periodically (every 3-5 days), the orchestrator breaks up routine with special assignments:

**Variety Activities:**
- Patrol Duty - Perimeter patrols
- Scouting Assignment - Area reconnaissance  
- Guard Rotation - Night watch duty
- Supply Escort - Wagon escort duty
- Camp Fortification - Defensive improvements
- Equipment Inspection - Full gear check
- Messenger Duty - Dispatch carrying

**Selection Criteria:**
- Weighted random based on phase preference
- Skips during Intense activity or siege
- Maximum 2 injections per week
- Minimum 3 days between injections

**Config:**
```json
{
  "varietySettings": {
    "minDaysBetweenInjections": 3,
    "maxDaysBetweenInjections": 5,
    "injectionChancePerDay": 0.35,
    "maxInjectionsPerWeek": 2,
    "skipDuringIntense": true,
    "skipDuringSiege": true
  }
}
```

### Automatic Routine Processing

When a phase completes, `CampRoutineProcessor` automatically processes scheduled activities:

**Processing Flow:**
```
Phase Transition
    ↓
CampOpportunityGenerator.ProcessCompletedPhaseRoutine()
    ↓
CampRoutineProcessor.ProcessPhaseTransition(phase, schedule)
    ↓
For each scheduled slot:
    ↓
RollOutcomeType()
    ├─ Excellent (10%) → +50% XP, bonus effects
    ├─ Good (25%) → +20% XP
    ├─ Normal (40%) → Base XP
    ├─ Poor (18%) → -50% XP
    └─ Mishap (7%) → -80% XP, potential injury
    ↓
ApplyEffects()
    ├─ Hero.AddSkillXp()
    ├─ Hero.ChangeHeroGold()
    ├─ CompanyNeeds.ModifyNeed()
    └─ ApplyCondition() (on mishap)
    ↓
┌───────────────────┬────────────────────┐
│ Combat Log        │ News Feed          │
│ (immediate)       │ (summary)          │
└───────────────────┴────────────────────┘
```

**Outcome Modifiers:**
- Player skills → Better training outcomes
- Fatigue → More mishaps when exhausted
- Morale → Poor outcomes when morale low
- Equipment → Training penalties with poor gear
- Weather/context → Siege = harsh conditions

**Example Outcomes:**
```
[Green] Sharp focus today. Movements feel natural. (+22 OneHanded XP)
[Gray] Another day of practice. (+10 OneHanded XP)
[Yellow] Sluggish today. Heat didn't help. (+5 OneHanded XP)
[Red] Twisted ankle during drill. (minor_injury)
```

**Config Files:**
- `routine_outcomes.json` - Activity outcome tables, XP ranges, flavor text
- `orchestrator_overrides.json` - Override triggers, variety pool
- `camp_schedule.json` - Baseline schedules

**Player Control:**
- Player commitments override automatic processing
- Scheduled tag appears on matching opportunities
- Routine only processes when player has no commitment

**Integration:**
- Combat log messages with color-coded outcomes
- News feed integration via `EnlistedNewsBehavior.AddRoutineOutcome()`
- No popups for routine outcomes (summary only)
- Decision opportunities still fire for meaningful choices

**Related Docs:** [Camp Routine Schedule](../../Campaign/camp-routine-schedule-spec.md), [Event System Schemas](event-system-schemas.md#camp-routine-configs)

---

## Content Types

### Orders

Military directives from the chain of command that the player must complete:

**Characteristics:**
- Issued by superiors (lord, officer, sergeant)
- Have completion criteria and time limits
- Reward completion, penalize failure
- Track completion in service record

**Examples:**
- Scouting duty (reconnaissance mission)
- Supply run (acquire provisions)
- Patrol duty (area security)
- Guard duty (static defense)

### Decisions

Player-initiated actions from the Camp Hub menu:

**Characteristics:**
- Player chooses when to attempt (full timing control)
- Delivered as inline menu selections (not popups)
- Use `dec_` ID prefix for Camp Hub decisions
- Loaded from `ModuleData/Enlisted/Decisions/` folder
- Often have skill checks or costs
- Provide progression and customization
- Can be daily or limited-use

**Examples:**
- Training (skill improvement)
- Social interactions (reputation building)
- Commerce (buy/sell/gamble)
- Risky actions (calculated risks)

### Events

Context-triggered narrative moments:

**Characteristics:**
- Fire based on context (camp, march, battle, muster)
- Check requirements (tier, role, reputation, traits)
- Provide choices with consequences
- Drive narrative and character development

**Examples:**
- Escalation thresholds (discipline warnings)
- Role-specific events (scout encounters)
- Universal events (camp life, social)
- Crisis events (supply shortages, mutiny)

### Map Incidents

Integration with native Bannerlord map incidents:

**Characteristics:**
- Use native incident system
- Trigger when conditions met
- Leverage existing map mechanics
- Provide Enlisted-specific outcomes

**Examples:**
- Encounter while leaving battle
- Events during siege
- Town/village entry incidents
- Waiting/resting incidents

---

## System Architecture

### Component Hierarchy

```
Content System
├── EventCatalog         (JSON loader, content registry)
├── EventDeliveryManager (Pipeline, effects, feedback)
├── EventRequirementChecker (Validation, gating)
├── EventSelector        (Weighted event selection)
├── EventPacingManager   (3-5 day paced narrative events)
├── MapIncidentManager   (Context-based incidents: battles, settlements)
├── GlobalEventPacer     (Enforces max_per_day, min_hours_between limits)
├── DecisionCatalog      (Decision-specific loading)
└── OrderCatalog         (Order-specific loading)
```

### Global Event Pacing (Current System)

All automatic events are coordinated through `GlobalEventPacer` to prevent spam. **All timing is config-driven** from `enlisted_config.json` → `decision_events.pacing`.

```
                    ┌─────────────────────────────────────────────────┐
                    │              GlobalEventPacer                   │
                    │  Enforces (all config-driven):                  │
                    │    • max_per_day / max_per_week                 │
                    │    • min_hours_between                          │
                    │    • evaluation_hours (narrative only)          │
                    │    • per_category_cooldown_days                 │
                    │    • quiet_day_chance (random skip)             │
                    └──────────────────────┬──────────────────────────┘
                                           │
              ┌────────────────────────────┼────────────────────────────┐
              │                            │                            │
     EventPacingManager           MapIncidentManager            (Chain Events)
    category: "narrative"       category: "map_incident"         (immediate)
    (event_window_min/max)       (skips evaluation_hours)
    (follows eval hours)
              │                            │                            │
              └────────────────────────────┼────────────────────────────┘
                                           │
                                           ▼
                               EventDeliveryManager
                                 (queues and shows)
```

**Config Location:** `enlisted_config.json` → `decision_events.pacing`

**What's NOT gated:**
- Player-selected decisions (Camp Hub menu) - player chose to see it
- Chain events from previous choices - immediate follow-up
- Debug/test event triggers

**Grace Period After Enlistment:**
- **3-day grace period** after enlisting with a lord
- No narrative events or map incidents fire during this period
- Gives player time to learn systems and get oriented
- Hardcoded in EventPacingManager and MapIncidentManager

See [Event System Schemas - Global Event Pacing](event-system-schemas.md#global-event-pacing-enlisted_configjson) for full config reference.

---

## Future: Content Orchestrator Architecture

**Status:** 📋 Specification - See [Content Orchestrator Plan](content-orchestrator-plan.md)

The orchestrator will replace schedule-driven pacing with **world-state driven simulation:**

```
┌─────────────────────────────────────────────────────────────┐
│          Content Orchestrator (Sandbox Life Simulator)       │
│     Matches frequency to lord's situation, not timers        │
└─────────────────────────────────────────────────────────────┘
                              │
            ┌─────────────────┼─────────────────────────┐
            ▼                 ▼                         ▼
    ┌──────────────┐  ┌──────────────────┐  ┌──────────────────┐
    │   World      │  │   Simulation     │  │   Player         │
    │   State      │  │   Pressure       │  │   Behavior       │
    │   Analyzer   │  │   Calculator     │  │   Tracker        │
    └──────────────┘  └──────────────────┘  └──────────────────┘
            │                 │                         │
            └─────────────────┼─────────────────────────┘
                              ▼
                    ┌──────────────────────┐
                    │ Realistic Frequency  │
                    │ & Content Selection  │
                    └──────────────────────┘
                              ▼
                    ┌────────────────────────┐
                    │   GlobalEventPacer     │
                    │   (Safety Limits Only) │
                    └────────────────────────┘
                              ▼
                    ┌──────────────────┐
                    │ EventDelivery    │
                    │ Manager (queue)  │
                    └──────────────────┘
```

### Key Changes

**Replaces:**
- `EventPacingManager` schedule logic (event windows, evaluation hours)
- Random quiet day rolls (15% chance)
- Fixed 3-5 day event cycles

**Adds:**
- **WorldStateAnalyzer** - Determines lord's situation (garrison, campaign, siege)
- **SimulationPressureCalculator** - Tracks realistic pressure sources
- **PlayerBehaviorTracker** - Learns player preferences

**Keeps:**
- `EventSelector` (content selection logic)
- `EventDeliveryManager` (queue and display)
- `EventRequirementChecker` (validation)
- `MapIncidentManager` (already context-driven)
- `GlobalEventPacer` (safety limits only - max/day, min hours between)

### World-State Driven Frequency

| Lord Situation | Realistic Frequency | Why |
|----------------|---------------------|-----|
| Peacetime Garrison | 1 event/week | Boring is realistic |
| War Marching | 3.5 events/week | Normal tempo |
| Active Campaign | 5 events/week | High tempo |
| Siege Defense | 7 events/week | Crisis situation |
| Lord Captured | 0.5 events/week | Special state |

**Philosophy:** Garrison duty IS boring. Campaigns ARE busy. The simulation doesn't manufacture drama—it reflects reality.

**See:** [Content Orchestrator Plan](content-orchestrator-plan.md) for complete architecture and 5-week implementation timeline.

### Data Flow

```
1. Content Loading (Startup)
   ├── Load JSON files from ModuleData/Enlisted/Events/
   ├── Parse event/decision/order definitions
   ├── Validate structure and IDs
   └── Register in catalogs

2. Content Delivery (Runtime)
   ├── Context trigger (camp menu, daily tick, battle end, etc.)
   ├── Query available content for context
   ├── Check requirements for each candidate
   ├── Select content (weighted random or player choice)
   └── Present to player

3. Player Response
   ├── Player selects option
   ├── Apply immediate effects (gold, rep, XP, etc.)
   ├── Apply delayed effects (orders, flags, timers)
   └── Display feedback (combat log messages with color coding)

4. Effect Resolution
   ├── Update escalation state (Scrutiny, Discipline, Medical)
   ├── Update reputation (Officer, Soldier, QM, Lord)
   ├── Award XP and trait progress
   ├── Modify resources (gold, supplies, food)
   ├── Set flags for follow-up events
   └── Display consolidated effect summary in combat log
```

---

## Content Pipeline

### Event Delivery Flow

```csharp
// 1. Context determines what content can fire
string context = "camp"; // or "march", "muster", "battle", etc.

// 2. Get candidate events for context
var candidates = EventCatalog.GetEventsForContext(context);

// 3. Filter by requirements (includes context filtering for variants)
var validEvents = candidates.Where(e => 
    EventRequirementChecker.MeetsRequirements(e.Requirements));
// Note: If event has variants, only context-matching variants pass this filter

// 4. Select event (weighted random or priority)
var selectedEvent = SelectEvent(validEvents);
// If multiple variants eligible, best-fit is selected

// 5. Present to player
EventDeliveryManager.DeliverEvent(selectedEvent);

// 6. Player chooses option
var chosenOption = await GetPlayerChoice(selectedEvent.Options);

// 7. Apply effects
EventDeliveryManager.ApplyEffects(chosenOption.Effects);

// 8. Display feedback (combat log message with all effects)
// Example: "+25 OneHanded XP, +5 Soldier Reputation, -30 gold"
// Automatically shown in green/yellow/cyan based on effect type
```

### Content Variant Selection (Future Enhancement)

When multiple variants exist for the same base event/decision:

```csharp
// Example: Player in garrison, checking for rest decision
var context = GetCurrentContext(); // "Camp"

// Step 1: Query all rest-related content
var candidates = EventCatalog.GetAllEvents().Where(e => e.Id.StartsWith("dec_rest"));
// Returns: dec_rest, dec_rest_garrison, dec_rest_exhausted

// Step 2: Filter by requirements (including context)
var eligible = candidates.Where(e => 
    EventRequirementChecker.MeetsRequirements(e.Requirements));
// dec_rest:           ✓ (context: Any)
// dec_rest_garrison:  ✓ (context: ["Camp"]) ← Matches!
// dec_rest_exhausted: ✗ (context: ["Siege", "Battle"])

// Step 3: Orchestrator scores eligible variants
// dec_rest: score 40 (base match)
// dec_rest_garrison: score 65 (context match bonus)

// Step 4: Select best-fit variant
var selected = SelectBestContent(eligible);
// Returns: dec_rest_garrison (better fit for garrison context)

// Player sees: Garrison-specific rest option with better fatigue relief
```

**Key Point:** Variant selection happens automatically via existing requirement system. No new code needed - just add JSON variants with appropriate `requirements.context` values.

**See:** [Event System Schemas - Content Variants Pattern](event-system-schemas.md#content-variants-pattern) for complete variant documentation.

### Decision Delivery Flow

**Camp Hub Decisions (dec_* prefix):**
```csharp
// 1. Player opens Camp Hub (C key)
ShowCampMenu();

// 2. Player navigates to category (Training, Social, etc.)
// 3. Get available decisions for that category
var decisions = DecisionCatalog.GetAvailableDecisions();

// 3. Filter by requirements (tier, role, cooldowns, etc.)
var validDecisions = decisions.Where(d => 
    MeetsDecisionRequirements(d));

// 4. Display in menu with costs/tooltips
DisplayDecisionMenu(validDecisions);

// 5. Player selects decision
var chosen = await GetPlayerDecisionChoice();

// 6. Deduct costs (gold, fatigue, etc.)
ApplyCosts(chosen.Costs);

// 7. Execute decision (may have sub-choices)
ExecuteDecision(chosen);

// 8. Apply rewards
ApplyRewards(chosen.Rewards);
```

---

## Requirement System

### Requirement Types

**Player State:**
```json
{
  "tier_min": 3,
  "tier_max": 6,
  "role": "Scout",
  "is_enlisted": true
}
```

**Reputation:**
```json
{
  "officer_rep_min": 40,
  "soldier_rep_min": -10,
  "qm_rep_min": 30
}
```

**Skills:**
```json
{
  "skill_checks": {
    "Scouting": 50,
    "Athletics": 30
  }
}
```

**Context:**
```json
{
  "context": ["camp", "march"],
  "time_of_day": "night",
  "in_settlement": false
}
```

**Flags:**
```json
{
  "required_flags": ["completed_onboarding"],
  "forbidden_flags": ["on_leave"]
}
```

**Onboarding:**
```json
{
  "onboarding_stage": 2,
  "onboarding_track": "green"
}
```

### Requirement Checking

```csharp
public bool MeetsRequirements(EventRequirements req)
{
    // Tier check
    if (!CheckTierRange(req.TierMin, req.TierMax))
        return false;
    
    // Role check
    if (req.Role != null && !HasRole(req.Role))
        return false;
    
    // Reputation checks
    if (!CheckReputationRequirements(req))
        return false;
    
    // Skill checks
    if (!CheckSkillRequirements(req.SkillChecks))
        return false;
    
    // Context checks
    if (!CheckContextRequirements(req.Context, req.TimeOfDay, req.InSettlement))
        return false;
    
    // Flag checks
    if (!CheckFlagRequirements(req.RequiredFlags, req.ForbiddenFlags))
        return false;
    
    // Onboarding checks
    if (!CheckOnboardingRequirements(req.OnboardingStage, req.OnboardingTrack))
        return false;
    
    return true;
}
```

---

## Effect System

### Effect Categories

**Resources:**
```json
{
  "gold": 100,
  "supplies": -5,
  "food": 3,
  "fatigue": -2
}
```

**Reputation:**
```json
{
  "officer_rep": 10,
  "soldier_rep": -5,
  "qm_rep": 3,
  "soldierRep": 5
}
```

**Escalation:**
```json
{
  "scrutiny": 5,
  "discipline": 2,
  "medical": -3
}
```

**XP & Traits:**
```json
{
  "reward_choices": [
    {
      "Scouting": 25,
      "Leadership": 15,
      "trait_xp": {
        "ScoutSkills": 10
      }
    }
  ]
}
```

**Flags & State:**
```json
{
  "set_flags": ["completed_scout_training"],
  "clear_flags": ["awaiting_assignment"],
  "advances_onboarding": true
}
```

**Retinue Effects (T7+ Commanders):**
```json
{
  "retinueGain": 3,
  "retinueLoss": 2,
  "retinueWounded": 5,
  "retinueLoyalty": 10
}
```

**Special Effects:**
```json
{
  "triggers_discharge": true,
  "starts_order": "order_patrol_duty",
  "unlocks_decision": "decision_advanced_training"
}
```

### Retinue Effects (T7+ Commanders)

Special effects for managing the Commander's retinue:

| Effect | Type | Target | When Used |
|--------|------|--------|-----------|
| `retinueGain` | int | Player's retinue | Add soldiers (volunteers, transfers, reinforcements) |
| `retinueLoss` | int | Player's retinue | Remove soldiers (casualties, desertion) |
| `retinueWounded` | int | Player's retinue | Wound soldiers (accidents, illness) |
| `retinueLoyalty` | int | Loyalty track | Modify retinue morale (-100 to +100) |

**Implementation:**

Retinue effects operate on the player's personal retinue (T7+ commanders only):
- Effects check `RetinueManager.Instance?.State?.HasRetinue` before applying
- Modify `MobileParty.MainParty.MemberRoster` for actual soldiers
- Update `RetinueState.TroopCounts` to maintain sync
- Display contextual notifications for player feedback
- Only affect retinue soldiers, not lord's troops or companions

**Loyalty Tracking:**

`retinueLoyalty` modifies the retinue's loyalty track (0-100):
- Low loyalty (<30): Triggers grumbling events, performance warnings
- High loyalty (>80): Triggers devotion events, combat bonuses
- Appears in Daily Brief when notable
- Used as gating condition for some events

**Example Usage in Events:**

```json
{
  "option_id": "share_rations",
  "text": "Buy extra food and share with your men.",
  "costs": { "gold": 30 },
  "effects": {
    "retinueLoyalty": 8,
    "soldierRep": 5
  }
}
```

**See:** [Retinue System Documentation](../Core/retinue-system.md) for complete retinue mechanics.

### Risky Options

Options with success/failure outcomes:

```json
{
  "option_id": "risky_action",
  "risk_chance": 0.60,
  "effects_success": {
    "gold": 100,
    "officer_rep": 10
  },
  "effects_failure": {
    "scrutiny": 10,
    "officer_rep": -5
  }
}
```

---

## Native Effect Integration

The content system integrates with Bannerlord's native IncidentEffect system to apply effects that work seamlessly with the base game's mechanics, UI, and tooltips.

### IncidentEffectTranslator

**Location:** `src/Features/Content/IncidentEffectTranslator.cs`

The translator bridges JSON event effects to native Bannerlord `IncidentEffect` objects that the game's engine understands. This provides:
- Automatic tooltip generation via native `GetHint()` method
- Integration with native UI feedback systems
- Proper effect application using Bannerlord's action system
- Support for multiple effects in a single event outcome

**Supported Native Effects:**

| Effect Type | Native Class | Applied Via | Example |
|-------------|--------------|-------------|---------|
| Gold change | `GoldIncidentEffect` | `GiveGoldAction` | Pay, rewards, gambling |
| Skill XP | `SkillIncidentEffect` | `Hero.AddSkillXp()` | Training, experience gains |
| Trait level | `TraitIncidentEffect` | `Hero.SetTraitLevel()` | Reputation mapping to traits |
| Morale | `MoraleIncidentEffect` | `MobileParty.Morale` | Company morale shifts |
| Health | `HealthIncidentEffect` | `Hero.HitPoints` | Injuries, recovery |
| Renown | `RenownIncidentEffect` | `Hero.AddRenown()` | Glory, reputation |
| Wound troops | `WoundTroopsRandomlyIncidentEffect` | `MobileParty.MemberRoster` | Battle casualties |

**Translator Process:**

```
Event fires → Player chooses option
  ↓
EventDeliveryManager.ApplyEffects(option.effects)
  ↓
IncidentEffectTranslator.TranslateEffects(effectsJson)
  ├─ Parse JSON effects
  ├─ Create native IncidentEffect objects
  ├─ Native tooltips generated automatically
  └─ Return: List<IncidentEffect>
  ↓
Native Bannerlord applies effects
  └─ UI feedback, sound effects, visual indicators
```

### Trait Mapping System

The mod maps Enlisted reputation tracks to native Bannerlord traits, allowing reputation to affect the base game's diplomacy, influence, and NPC reactions.

**Mapping Configuration:**

| Enlisted Track | Native Trait | Rationale | Milestone Notification |
|----------------|--------------|-----------|------------------------|
| Soldier Rep | Valor | Bravery, combat spirit | "Your courage is noticed among the ranks." |
| Officer Rep | Calculating | Tactical thinking, leadership | "The officers see you as a thinker." |
| Lord Rep | Honor | Duty, keeping your word | "Your lord trusts your word." |

**Configuration:** `ModuleData/Enlisted/enlisted_config.json` → `native_trait_mapping`

```json
{
  "native_trait_mapping": {
    "enabled": true,
    "scale_divisor": 5,
    "minimum_change": 1,
    "soldier_to_valor": true,
    "officer_to_calculating": true,
    "lord_to_honor": true
  }
}
```

**Scale Divisor:** Divides reputation changes before applying to traits (default 5). Tuned for ~100 day careers where cumulative reputation builds meaningful trait levels.

**Minimum Change:** Threshold to prevent spam (default 1). Changes below this threshold are ignored.

### TraitMilestoneTracker

**Location:** `src/Features/Content/TraitMilestoneTracker.cs`

Tracks when native traits cross level thresholds and displays milestone notifications to give players feedback on their reputation's impact on the wider game world.

**Tracked Milestones:**
- Trait level increases (Level 0 → 1, 1 → 2, etc.)
- Significant threshold crosses (0-25-50-75-100 ranges)
- Persistent per-campaign tracking

**Notification Examples:**
- Valor +1: "Your courage is noticed among the ranks."
- Calculating +1: "The officers see you as a thinker."
- Honor +1: "Your lord trusts your word."

**Integration Point:** `EventDeliveryManager.ApplyEffects()` checks for trait milestones after applying all effects, displays notification if threshold crossed.

### Benefits of Native Integration

**For Players:**
- Familiar tooltips that match base game style
- Effects integrate with existing game systems
- Reputation affects NPC reactions and diplomacy
- No need to learn mod-specific mechanics

**For Modders:**
- Content creators can use native effect types
- Automatic tooltip generation (no manual text needed)
- Effects work correctly with other mods
- Future-proof against game updates

**For Developers:**
- Leverage Bannerlord's tested action system
- Native UI feedback (sound, visuals, messages)
- Proper save/load handling via native effects
- Reduced custom code maintenance

---

## Delivery Contexts

### Context Types

| Context | When | Example Events |
|---------|------|----------------|
| `"camp"` | In camp, daily | Gambling, storytelling, brawls |
| `"march"` | Party moving | Patrol encounters, terrain challenges |
| `"muster"` | Every 12 days | Pay events, inspections, promotions |
| `"leaving_battle"` | After player battle ends | Looting, wounded comrades, battlefield finds |
| `"during_siege"` | Hourly while besieging | Water rationing, desertion, disease |
| `"entering_town"` | Opening town/castle menu | Tavern encounters, merchants, old friends |
| `"entering_village"` | Opening village menu | Local gratitude, rumors, recruitment |
| `"leaving_settlement"` | Leaving any settlement | Hangovers, farewells, stolen items |
| `"waiting_in_settlement"` | Hourly in town/castle garrison | Opportunities, encounters, trouble brewing |

### Context-Specific Behavior

**Camp Events:**
- Fire during daily tick
- Check fatigue, time of day
- Integrate with camp menu decisions

**Muster Events:**
- Fire on 12-day cycle as part of multi-stage muster menu sequence
- Integrate with pay, rations, baggage checks
- Delivered as GameMenu stages (not popups) via `MusterMenuHandler`
- Events include: inspection, recruit mentoring, baggage checks
- See [Muster Menu System](../Core/muster-menu-revamp.md) for complete flow

**Map Incidents:**
- Fire based on map actions (battle end, settlement entry/exit, hourly while stationed)
- Use MapIncidentManager to deliver context-specific incidents
- Contexts: leaving_battle, during_siege, entering_town, entering_village, leaving_settlement, waiting_in_settlement
- Have individual cooldowns (1-12 hours) and probability checks

### Player Status Notifications

**Medical Risk Status Line:**

The player status line (shown in Enlisted Status menu and Daily Brief) displays Medical Risk warnings:

| Medical Risk | Status Message |
|--------------|----------------|
| 0-1 | (normal status messages) |
| 2 | "Something's off. Tired. Aching. Watch it." |
| 3 | "The ache is constant now. Rest or surgeon." |
| 4-5 | "Fever won't break. Surgeon's tent calls." |

**Priority Order:**
1. Recent battle aftermath
2. Active injuries (PlayerConditionBehavior)
3. Active illness (PlayerConditionBehavior)
4. **Medical Risk escalation** (brewing sickness)
5. Fatigue levels
6. Good condition (tier/time flavor)

This gives players early warning before threshold events fire, allowing proactive treatment.

---

## JSON Structure

### Event Definition

```json
{
  "event_id": "evt_example",
  "titleId": "evt_example_title",
  "title": "Example Event",
  "setupId": "evt_example_setup",
  "setup": "Event description goes here.",
  "requirements": {
    "tier_min": 2,
    "context": ["camp"],
    "role": "Scout"
  },
  "options": [
    {
      "option_id": "option_1",
      "textId": "evt_example_opt1",
      "text": "First choice",
      "tooltip": "Explains consequences",
      "effects": {
        "gold": 50,
        "officer_rep": 5
      },
      "resultTextId": "evt_example_opt1_result",
      "resultText": "Result of first choice."
    },
    {
      "option_id": "option_2",
      "textId": "evt_example_opt2",
      "text": "Second choice",
      "tooltip": "Explains consequences",
      "risk_chance": 0.50,
      "effects_success": {
        "gold": 100
      },
      "effects_failure": {
        "scrutiny": 5
      },
      "resultTextId": "evt_example_opt2_result",
      "resultText": "Result of second choice."
    }
  ]
}
```

### Decision Definition

```json
{
  "decision_id": "decision_training",
  "nameId": "decision_training_name",
  "name": "Training",
  "descriptionId": "decision_training_desc",
  "description": "Improve your skills through practice.",
  "category": "training",
  "cost": {
    "gold": 0,
    "fatigue": 1
  },
  "requirements": {
    "tier_min": 1
  },
  "sub_choices": [
    {
      "id": "weapon_training",
      "textId": "decision_training_weapon",
      "text": "Weapon practice",
      "tooltip": "Train with equipped weapon",
      "reward_choices": [
        {
          "equipped_weapon_skill": 20,
          "applies_training_modifier": true
        }
      ]
    }
  ]
}
```

---

## Localization

### XML String Structure

All player-facing text is defined in `ModuleData/Languages/enlisted_strings.xml`:

```xml
<strings>
  <!-- Event title -->
  <string id="evt_example_title" text="Example Event" />
  
  <!-- Event setup -->
  <string id="evt_example_setup" text="Event description goes here." />
  
  <!-- Option text -->
  <string id="evt_example_opt1" text="First choice" />
  
  <!-- Result text -->
  <string id="evt_example_opt1_result" text="Result of first choice." />
</strings>
```

### TextObject Usage

```csharp
// In code, use TextObject with string IDs
var title = new TextObject("{=evt_example_title}Example Event");

// Fallback text is used if localization missing
// (useful during development)
```

### Placeholder Variables

All text fields (titles, descriptions, option text, result text) support placeholder variables that are dynamically replaced at runtime with game data:

```json
{
  "setup": "{SERGEANT} pulls you aside. 'Listen, {PLAYER_NAME}, we need someone to scout ahead.'",
  "text": "Tell {SERGEANT_NAME} you'll do it.",
  "resultText": "You report back. {LORD_NAME} seems pleased with the intel."
}
```

**Common Variables:**
- Player: `{PLAYER_NAME}`, `{PLAYER_RANK}`
- NCO/Officers: `{SERGEANT}`, `{OFFICER_NAME}`, `{CAPTAIN_NAME}`
- Soldiers: `{SOLDIER_NAME}`, `{VETERAN_1_NAME}`, `{RECRUIT_NAME}`
- Lord/Faction: `{LORD_NAME}`, `{FACTION_NAME}`, `{KINGDOM_NAME}`
- Location: `{SETTLEMENT_NAME}`, `{COMPANY_NAME}`

All variables automatically fall back to reasonable defaults if data is unavailable (e.g., "the Sergeant" if no NCO assigned). This ensures events work in all game states.

**See:** [Event Catalog - Placeholder Variables](../../Content/event-catalog-by-system.md#placeholder-variables) for complete list.

### Fallback Strategy

JSON files include fallback text for development:

```json
{
  "titleId": "evt_example_title",
  "title": "Example Event",  // ← Fallback if ID not found
}
```

**Production:** String IDs resolve from XML  
**Development:** Fallback text displays if XML missing

---

## Implementation Files

### Core System

| File | Purpose |
|------|---------|
| `src/Features/Content/EventCatalog.cs` | JSON loading, content registry |
| `src/Features/Content/EventDeliveryManager.cs` | Delivery pipeline, effects |
| `src/Features/Content/EventRequirementChecker.cs` | Requirement validation |
| `src/Features/Content/EventDefinition.cs` | Content data structures |
| `src/Features/Content/EventPacingManager.cs` | 3-5 day narrative event pacing |
| `src/Features/Content/MapIncidentManager.cs` | Map incident triggers and delivery |
| `src/Features/Content/GlobalEventPacer.cs` | Global pacing limits across all auto events |

### Subsystems

| File | Purpose |
|------|---------|
| `src/Features/Content/DecisionCatalog.cs` | Decision-specific loading |
| `src/Features/Content/RewardChoices.cs` | XP and reward handling |
| `src/Features/Content/ExperienceTrackHelper.cs` | Training modifiers |

### Content Files

| File | Content Type |
|------|--------------|
| `ModuleData/Enlisted/Events/events_escalation.json` | Escalation threshold events |
| `ModuleData/Enlisted/Events/events_crisis.json` | Crisis events |
| `ModuleData/Enlisted/Events/events_role_*.json` | Role-specific events |
| `ModuleData/Enlisted/Events/events_camp_life.json` | Universal camp events |
| `ModuleData/Enlisted/Events/events_training.json` | Training-related events |
| `ModuleData/Enlisted/Events/events_decisions.json` | Automatic decision events (game-triggered popups) |
| `ModuleData/Enlisted/Events/events_player_decisions.json` | Player-initiated event popups (player_* prefix) |
| `ModuleData/Enlisted/Events/incidents_battle.json` | Map incidents: leaving_battle (11 incidents) |
| `ModuleData/Enlisted/Events/incidents_siege.json` | Map incidents: during_siege (10 incidents) |
| `ModuleData/Enlisted/Events/incidents_town.json` | Map incidents: entering_town (8 incidents) |
| `ModuleData/Enlisted/Events/incidents_village.json` | Map incidents: entering_village (6 incidents) |
| `ModuleData/Enlisted/Events/incidents_leaving.json` | Map incidents: leaving_settlement (6 incidents) |
| `ModuleData/Enlisted/Events/incidents_waiting.json` | Map incidents: waiting_in_settlement (4 incidents) |
| `ModuleData/Enlisted/Events/events_retinue.json` | Retinue narrative events (10) + post-battle volunteers (T7+) |
| `ModuleData/Enlisted/Events/incidents_retinue.json` | Retinue post-battle incidents (6, T7+) |
| `ModuleData/Enlisted/Decisions/decisions.json` | 38 Camp Hub decisions (dec_* prefix, inline menu) including 4 retinue decisions (T7+) |

### Localization

| File | Purpose |
|------|---------|
| `ModuleData/Languages/enlisted_strings.xml` | All player-facing text |

---

## Player Feedback System

All content systems provide immediate visual feedback when effects are applied.

### Combat Log Messages

**Decisions and Events:**
- Effects displayed as consolidated message: `+25 OneHanded XP, +5 Soldier Reputation, -30 gold`
- Costs shown separately: `Cost: -30 gold, +2 fatigue`
- Rewards shown separately: `+50 gold, -3 fatigue`

**Orders:**
- Effects displayed with "Order:" prefix: `Order: +30 Leadership XP, +3 Officer Reputation, +100 gold`

### Color Coding

| Message Type | Color | Example |
|--------------|-------|---------|
| Effects Applied | Green | `+25 OneHanded XP, +5 Soldier Reputation` |
| Costs Paid | Yellow | `Cost: -30 gold, +2 fatigue` |
| Rewards Received | Cyan | `+50 gold, -3 fatigue` |
| Warnings/Failures | Red | `You are badly wounded!` |

### Implementation

**Effect Aggregation:**
- All effects from a single action are collected into a list
- Messages are formatted with proper +/- signs
- Multiple effects are comma-separated in a single message
- Empty effect lists are not displayed (no message spam)

**Technical Details:**
- Uses `InformationManager.DisplayMessage()` with `Colors.*` constants
- Messages appear in the combat log (bottom-left of screen)
- All effects are still logged to debug file for troubleshooting
- Message format: `<effect type>: <comma-separated effects>`

**Example Flow:**
```csharp
// Player selects "Spar Hard" decision
var feedbackMessages = new List<string>();

// Collect effects
feedbackMessages.Add("+40 OneHanded XP");
feedbackMessages.Add("+20 Athletics XP");
feedbackMessages.Add("+3 Soldier Reputation");
feedbackMessages.Add("-8 HP");

// Display to player
var message = string.Join(", ", feedbackMessages);
InformationManager.DisplayMessage(new InformationMessage(message, Colors.Green));
// Output: "+40 OneHanded XP, +20 Athletics XP, +3 Soldier Reputation, -8 HP"
```

This ensures players always know exactly what happened when they make a choice.

---

**End of Document**

