# Content Orchestrator: Sandbox Life Simulator

**Status:** 📋 Specification  
**Priority:** High  
**Complexity:** Major architectural change  
**Created:** 2025-12-24  
**Last Updated:** 2025-12-24  
**Related Docs:** [BLUEPRINT](../BLUEPRINT.md), [Order Progression System](order-progression-system.md), [Content System Architecture](../Features/Content/content-system-architecture.md), [Event System Schemas](../Features/Content/event-system-schemas.md), [News & Reporting System](../Features/UI/news-reporting-system.md)

---

## Executive Summary

Replace the current schedule-driven event pacing system with a **Sandbox Life Simulator** that coordinates all aspects of military life. The orchestrator makes realistic decisions about when content should fire based on what's happening in the world and your lord's situation, not arbitrary timers or dramatic needs.

**Core Principle:** Military life has natural rhythms. Active campaigns are busy. Garrison duty is quiet. The orchestrator respects this reality instead of forcing artificial pacing.

**Design Philosophy:** We simulate a living, breathing military experience. The player is one soldier in Calradia's endless wars. Stories emerge from living that life, not from scripted beats. Boredom is realistic. Crisis is temporary. Most days are routine.

**Alignment with Project Principles:**
- **Emergent identity from choices** - The orchestrator delivers content based on player behavior, not predetermined arcs
- **Realistic military hierarchy** - Event frequency reflects actual military life rhythms
- **Choice-driven narrative** - Player choices create the story; orchestrator provides the canvas
- **Minimal intrusion** - Silence is the default; events earn their moment
- **Native integration** - Uses existing systems (GlobalEventPacer, EventSelector, EventDeliveryManager)

---

## Table of Contents

1. [Quick Start Guide](#quick-start-guide)
2. [Current State & Problems](#current-state--problems)
3. [Migration: What Changes](#migration-what-changes)
4. [Target Architecture](#target-architecture)
5. [Implementation Phases](#implementation-phases)
6. [Technical Specifications](#technical-specifications)
7. [Testing & Validation](#testing--validation)
8. [Sandbox Philosophy](#sandbox-philosophy)
9. [References & Related Docs](#references--related-docs)

---

## Quick Start Guide

**For implementers: Read this first, then proceed to detailed sections.**

### 30-Second Overview
Replace schedule-driven event pacing with world-state-driven content orchestration. Garrison duty becomes quiet, campaigns become busy, sieges become intense. All automatic, all realistic.

### Implementation Order
1. **Week 1 - Foundation:** Create orchestrator infrastructure without changing existing behavior
2. **Week 2 - Selection:** Integrate with content selection and add player behavior tracking
3. **Week 3 - Cutover:** Switch from old system to orchestrator
4. **Week 4 - Orders:** Coordinate order timing with orchestrator
5. **Week 5 - UI:** Add player-facing transparency (Company Report section)

### Critical Requirements
- ✅ Must use local decompile (`C:\Dev\Enlisted\Decompile\`) for API verification
- ✅ Must manually add new .cs files to `Enlisted.csproj`
- ✅ Must use `ModLogger` with category "Orchestrator"
- ✅ Must log to `<BannerlordInstall>\Modules\Enlisted\Debugging\`
- ✅ Must follow ReSharper recommendations
- ✅ Must provide tooltips for all new config options
- ✅ Must write comments describing current behavior (no "Phase X" references)

### Key Files to Create
```
src/Features/Content/
├── ContentOrchestrator.cs            (main coordinator)
├── WorldStateAnalyzer.cs             (world state detection)
├── SimulationPressureCalculator.cs   (pressure calculation)
├── PlayerBehaviorTracker.cs          (preference tracking)
└── Models/
    ├── WorldSituation.cs
    ├── SimulationPressure.cs
    └── PlayerPreferences.cs
```

### Key Files to Modify
```
src/Features/Content/
├── EventPacingManager.cs             (remove schedule logic)
├── GlobalEventPacer.cs               (remove evaluation hours)
└── EventSelector.cs                  (add fitness scoring)

src/Features/Interface/Behaviors/
├── EnlistedNewsBehavior.cs           (add Company Report)
└── EnlistedMenuBehavior.cs           (update header layout)

ModuleData/Enlisted/
└── enlisted_config.json              (add orchestrator config)
```

### Build & Test
```powershell
# 1. Add new files to Enlisted.csproj
# 2. Build
cd C:\Dev\Enlisted\Enlisted
dotnet build -c "Enlisted RETAIL" /p:Platform=x64

# 3. Launch game and check logs
# Log location: <BannerlordInstall>\Modules\Enlisted\Debugging\enlisted.log
# Look for: [Orchestrator] entries on daily tick
```

### Quick Reference Docs
- **[BLUEPRINT.md](../../BLUEPRINT.md)** - Coding standards, API verification, logging
- **[Event System Schemas](event-system-schemas.md)** - JSON data structures
- **[Content System Architecture](content-system-architecture.md)** - Current system overview
- **[UI Systems Master](../UI/ui-systems-master.md)** - UI integration details
- **[News & Reporting System](../UI/news-reporting-system.md)** - Daily Brief system

---

## Current State & Problems

### What Exists Today

| Component | Current Behavior | Problem |
|-----------|------------------|---------|
| **EventPacingManager** | Schedule-driven. Fires events every 3-5 days based on timers. | Events don't respond to world state. Campaign feels same as garrison. |
| **EventSelector** | Weighted random selection from eligible pool. 2× role weight, 1.5× context weight. | Doesn't consider if this event makes sense RIGHT NOW given lord's situation |
| **GlobalEventPacer** | Enforces daily/weekly limits, evaluation hours, quiet day rolls. | Artificially spaces events instead of letting world state drive frequency |
| **MapIncidentManager** | Immediate fire on trigger (battle, settlement). | ✅ Works well - this IS realistic triggering |
| **EscalationManager** | Fires threshold events immediately. | ✅ Good - consequences should be immediate |
| **EventDeliveryManager** | Simple FIFO queue with popup display. | No understanding of simulation pressure or realistic spacing |

### Key Issues

1. **Schedule-driven, not world-driven:** Events fire on timers, not because your lord is in a campaign or siege
2. **No simulation awareness:** System doesn't know if garrison quiet or campaign chaos is appropriate
3. **No player behavior learning:** Previous choices don't influence content preferences
4. **No realistic frequency:** Same pacing whether peacetime garrison or desperate defense
5. **Artificial quiet days:** 15% random chance instead of "it's winter garrison duty, nothing happens"
6. **Evaluation hours feel mechanical:** Real military life doesn't have "event times"

---

## Migration: What Changes

### ❌ Remove: Schedule-Driven Systems

#### 1. EventPacingManager.cs - Schedule Logic
**Location:** `src/Features/Content/EventPacingManager.cs`

**Remove:**
- Lines 100-113: `NextNarrativeEventWindow` checking (fires every 3-5 days on timer)
- Line 116: Scheduled `TryFireEvent()` calls

**Keep:**
- Lines 79-94: Grace period check
- Lines 96-97: Chain event check

**Replace with:** Orchestrator's world-state driven daily tick

---

#### 2. EscalationState Fields
**Location:** `src/Features/Escalation/EscalationState.cs`

**Remove:**
```csharp
public CampaignTime NextNarrativeEventWindow { get; set; }  // Schedule tracking
public CampaignTime LastNarrativeEventTime { get; set; }    // Unused
```

**Keep:**
```csharp
// Safety net tracking (orchestrator uses these)
public CampaignTime LastAutoEventTime { get; set; }
public int AutoEventsToday { get; set; }
public int AutoEventsThisWeek { get; set; }
public bool IsQuietDay { get; set; }  // Set by world state, not random
public Dictionary<string, CampaignTime> CategoryLastFired { get; set; }
public List<ChainEvent> PendingChainEvents { get; set; }
```

---

#### 3. Config Changes
**Location:** `ModuleData/Enlisted/enlisted_config.json`

**BEFORE (Schedule-Driven):**
```json
{
  "decision_events": {
    "pacing": {
      "max_per_day": 2,
      "max_per_week": 8,
      "min_hours_between": 6,
      "event_window_min_days": 3,        // ❌ REMOVE
      "event_window_max_days": 5,        // ❌ REMOVE
      "evaluation_hours": [8, 14, 20],   // ❌ REMOVE
      "allow_quiet_days": true,          // ❌ REMOVE
      "quiet_day_chance": 0.15           // ❌ REMOVE
    }
  }
}
```

**AFTER (World-Driven):**
```json
{
  "decision_events": {
    "pacing": {
      "max_per_day": 2,
      "max_per_week": 8,
      "min_hours_between": 4,
      "per_event_cooldown_days": 7,
      "per_category_cooldown_days": 1
    },
    "orchestrator": {
      "enabled": true,
      "fitness_threshold": 40,
      "log_decisions": true,
      
      "frequency": {
        "peacetime_garrison": 0.14,      // 1 event per week
        "peacetime_recruiting": 0.35,    // 2.5 per week
        "war_marching": 0.5,             // 3.5 per week
        "war_active_campaign": 0.7,      // 5 per week
        "siege_attacking": 0.57,         // 4 per week
        "siege_defending": 1.0,          // 7 per week
        "lord_captured": 0.07            // 0.5 per week
      },
      
      "dampening": {
        "after_busy_week_multiplier": 0.7,
        "after_quiet_week_multiplier": 1.2,
        "after_battle_cooldown_days": 1.5
      },
      
      "pressure_modifiers": {
        "low_supplies": 0.1,
        "wounded_company": 0.1,
        "high_discipline": 0.15,
        "recent_victory": -0.2,
        "just_paid": -0.15
      }
    }
  }
}
```

---

#### 4. GlobalEventPacer Changes
**Location:** `src/Features/Content/GlobalEventPacer.cs`

**Remove:**
- Lines 134-143: Evaluation hours check (artificial "event times")
- Lines 117-132: Random quiet day roll (15% chance)

**Keep:**
- All safety limit checks (daily/weekly, min hours, category cooldowns)

**Change:** Orchestrator sets `IsQuietDay` based on world state, not random roll

---

### ✅ Keep: No Changes Needed

| System | Why It's Good | Action |
|--------|---------------|--------|
| **MapIncidentManager** | Already context-driven (fires after battles) | ✅ Keep as-is |
| **EscalationManager** | Consequences fire immediately | ✅ Keep as-is |
| **EventSelector** | Content selection logic | ✅ Orchestrator calls it |
| **EventDeliveryManager** | Queue system | ✅ Orchestrator uses it |
| **EventRequirementChecker** | Requirement validation | ✅ Orchestrator uses it |

---

### 🔄 Integrate: Coordinate with Orchestrator

#### OrderManager
**Location:** `src/Features/Orders/Behaviors/OrderManager.cs`

**Current:** Issues orders every ~3 days (schedule-based)  
**Has:** Context awareness (siege=1 day, peace=4 days)  
**Needs:** Integration with orchestrator's world state

**Action:** Orchestrator coordinates timing, OrderManager handles selection

---

### Migration Steps

**Phase 1: Add New Systems (Non-Breaking)**
1. Create `ContentOrchestrator.cs` with daily tick
2. Create `WorldStateAnalyzer.cs` to analyze lord situation
3. Create `SimulationPressureCalculator.cs` to track pressure
4. Create `PlayerBehaviorTracker.cs` to learn preferences
5. Add new config section `orchestrator` to `enlisted_config.json`
6. Leave old systems in place initially

**Phase 2: Route Through Orchestrator**
1. Modify `EventPacingManager.OnDailyTick()` to call `ContentOrchestrator.OnDailyTick()`
2. Orchestrator decides if content should fire today
3. If yes, orchestrator calls existing `EventSelector.SelectEvent()`
4. Test side-by-side with old system

**Phase 3: Remove Schedule Logic**
1. Remove `NextNarrativeEventWindow` scheduling from `EventPacingManager`
2. Remove evaluation hours check from `GlobalEventPacer`
3. Remove quiet day random roll from `GlobalEventPacer`
4. Remove schedule-driven config values
5. Orchestrator sets `IsQuietDay` based on world state

**Phase 4: Full Integration**
1. Integrate `OrderManager` timing with orchestrator
2. Route `MapIncidentManager` through orchestrator for coordination
3. All content types compete in same realistic frequency budget
4. Test full system with various world states

---

### Before vs After

**BEFORE (Schedule-Driven):**
```
Day 1: Nothing (too early)
Day 2: Nothing (too early)
Day 3: Event fires (window reached)
Day 4: Nothing (random quiet day)
Day 5: Nothing (too soon)
Day 6: Event fires (window reached)
```
**Problem:** Same pattern whether peacetime garrison or active siege

**AFTER (World-Driven):**
```
Peacetime Garrison:
  Week 1: Day 4 (1 event)
  Week 2: Nothing
  Week 3: Day 2 (1 event)
  Week 4: Nothing
  
Active Siege:
  Day 1: 2 events
  Day 2: 1 event (hit safety limit)
  Day 3: 2 events
  Day 4: 1 event (dampening)
```
**Solution:** Frequency matches reality of situation

---

## Content Types & Coordination

The orchestrator coordinates multiple content delivery systems, each with different timing:

### Content Type Overview

| Content Type | When It Fires | Who Controls | Orchestrator Role |
|--------------|---------------|--------------|-------------------|
| **Order Issuance** | When player has no active order | OrderManager | Provides timing coordination |
| **Order Events** | During order phase slots | OrderProgressionBehavior | Provides world state for weighting |
| **Camp Life Events** | When player is off-duty (no order) | ContentOrchestrator | Direct control via frequency tables |
| **Map Incidents** | After battles, settlements | MapIncidentManager | None (immediate triggers) |
| **Escalation Events** | When thresholds crossed | EscalationManager | None (consequence-driven) |
| **Muster Events** | Every 12 days | MusterBehavior | None (fixed cycle) |

### How Systems Work Together

```
Player Enlisted, No Active Order:
  ├─ Orchestrator checks: Should camp life event fire?
  │   └─ Uses frequency tables based on world state
  ├─ OrderManager checks: Should new order be issued?
  │   └─ Coordinates timing with orchestrator
  └─ Player can use Camp Hub decisions

Player Has Active Order:
  ├─ OrderProgressionBehavior processes phases (4/day)
  │   └─ At slot phases, uses world state to weight event chance
  │   └─ Order events fire contextually during duty
  ├─ Camp life events do NOT fire (player is on duty)
  └─ Order completes → return to "No Active Order" state
```

### Frequency Budget Clarification

**Order Events** (during duty) and **Camp Life Events** (between duties) use DIFFERENT frequency calculations:

**Order Events:**
- Base chance per slot (15% for Slot, 35% for Slot!)
- Modified by activity level (Quiet ×0.3 → Intense ×1.5)
- A 2-day order in siege might have 1-2 events
- A 2-day order in garrison probably has 0 events

**Camp Life Events:**
- Use orchestrator's events-per-week frequency
- Quiet: ~1/week, Intense: ~7/week
- Only fire when player has no active order
- Reduced opportunity during active campaigns (player always on order)

This creates the intended rhythm:
- **Garrison:** Boring orders, occasional camp drama
- **Campaign:** Constant orders, events happen during duty
- **Siege:** Intense orders with frequent events, no downtime

---

## Target Architecture

### Component Overview

```
┌─────────────────────────────────────────────────────────────┐
│              Content Orchestrator (Life Simulator)           │
│        Coordinates realistic military life experience        │
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
                    ┌──────────────────┐
                    │ EventDelivery    │
                    │ Manager (queue)  │
                    └──────────────────┘
```

### Core Components

#### 1. ContentOrchestrator
**Purpose:** Sandbox life simulator coordinator

**Responsibilities:**
- Monitors world state to determine realistic activity level
- Calculates simulation pressure from company/player state
- Determines if events should fire based on situation, not timers
- Learns player preferences to deliver content they enjoy
- Coordinates systems to prevent unrealistic spam
- Respects quiet periods as realistic downtime

**Key Methods:**
```csharp
void OnDailyTick()  // Main simulation loop
WorldSituation AnalyzeCurrentSituation()
float CalculateRealisticFrequency()
SimulationPressure CalculatePressure()
bool ShouldDeliverContent()  // Based on world state, not schedule
EventDefinition SelectRealisticContent()
```

---

#### 2. WorldStateAnalyzer
**Purpose:** Determines what's happening in the world RIGHT NOW

**Responsibilities:**
- Analyzes your lord's current situation (garrison, campaign, siege, defeated)
- Determines kingdom war stance (peacetime, defensive, offensive, desperate)
- Tracks recent world events (battles, sieges, declarations)
- Calculates realistic activity level for current situation
- Provides strategic context (8 strategic contexts)

**Output:**
```csharp
public class WorldSituation
{
    public LordSituation YourLordIs { get; set; }
    public WarStance KingdomStance { get; set; }
    public LifePhase CurrentPhase { get; set; }  // Peacetime, Campaign, Siege, Recovery
    public ActivityLevel ExpectedActivity { get; set; }  // Quiet, Routine, Active, Intense
    public float RealisticEventFrequency { get; set; }  // Events per week
}
```

---

#### 3. SimulationPressureCalculator
**Purpose:** Calculates how much pressure the simulation is putting on the player

**Responsibilities:**
- Tracks pressure sources (low supplies, wounded, failed orders, angry lord, high discipline)
- Tracks pressure releases (pay received, victories, promotions, rest)
- Calculates current simulation pressure (0-100)
- Determines if player needs relief or can handle more
- NOT dramatic tension - realistic life pressure

**Pressure Sources:**
```
Adds Pressure (realistic problems):
  Low supplies: +20
  Wounded company: +15
  Failed last order: +10
  In enemy territory: +15
  Lord angry (low relation): +20
  High discipline: +25
  Wounded player: +15
  
Reduces Pressure (realistic relief):
  Pay received: -15
  Battle won: -10
  Promoted: -20
  Rest day: -10
  In friendly town: -15
  Order success: -10
  
NOT manufactured for drama - emerges from simulation
```

---

#### 4. PlayerBehaviorTracker
**Purpose:** Learns what content the player engages with and enjoys

**Responsibilities:**
- Tracks player behavior patterns (helps comrades, takes risks, follows orders)
- Records content engagement (which events player completes, which they dismiss)
- Builds preference profile (combat vs social, risky vs safe, loyal vs self-serving)
- Informs content selection to deliver what player enjoys
- NOT character arc - just preference learning

**Tracked Patterns:**
```
Behavior Patterns:
  "accepts_all_orders": 15 times
  "helps_comrades": 8 times
  "prioritizes_gold": 6 times
  "avoids_danger": 3 times
  "volunteers_for_duty": 12 times
  
Content Preferences:
  CombatVsSocial: 0.7 (prefers combat content)
  RiskyVsSafe: 0.4 (plays it moderately safe)
  LoyalVsSelfServing: 0.8 (follows orders, helps others)
  
Selection uses this to deliver content player likes
```

---

#### 5. Content Variant System (Future Enhancement)
**Purpose:** Automatic context-aware variant selection

**Responsibilities:**
- No code needed - uses existing `requirements.context` filtering
- Events/decisions can have multiple variants for different situations
- EventRequirementChecker filters variants automatically
- Orchestrator selects best-fitting variant for current world state

**Pattern:**
```json
// Base event (always available)
{
  "id": "dec_rest",
  "requirements": { "tier": { "min": 1 } }
}

// Garrison variant (context-specific)
{
  "id": "dec_rest_garrison",
  "requirements": { 
    "tier": { "min": 1 },
    "context": ["Camp"]
  }
}

// Crisis variant (context-specific)
{
  "id": "dec_rest_exhausted",
  "requirements": { 
    "tier": { "min": 1 },
    "context": ["Siege"]
  }
}
```

**How It Works:**
1. Orchestrator queries eligible content for current world state
2. EventRequirementChecker filters by context (Camp/Siege/etc)
3. Only matching variants pass filter
4. Orchestrator scores and selects best fit
5. Player sees contextually appropriate variant

**Implementation Timeline:**
- **Weeks 1-5:** Orchestrator works with current content (no variants)
- **Week 6+:** Add variants incrementally as JSON additions
- **No code changes:** Variant selection already handled by existing requirement system

---

### Data Flow

```
1. Daily Tick (Main Loop)
   ↓
2. WorldStateAnalyzer.AnalyzeSituation()
   ├─ What's your lord doing? (Garrison, Campaign, Siege, etc.)
   ├─ What's the war status? (Peace, Active War, Crisis)
   └─ Returns: WorldSituation
       ↓
3. SimulationPressureCalculator.CalculatePressure()
   ├─ Check company state (supplies, morale, wounded)
   ├─ Check player state (discipline, reputation, health)
   ├─ Calculate total pressure
   └─ Returns: SimulationPressure (0-100)
       ↓
4. DetermineRealisticFrequency(WorldSituation)
   ├─ Garrison + Peacetime = 1 event/week
   ├─ Campaign + War = 5 events/week
   ├─ Siege Defense = 7 events/week
   └─ Returns: Expected frequency
       ↓
5. ShouldDeliverContentToday()?
   ├─ Roll against realistic frequency
   ├─ Consider recent activity (dampening)
   ├─ Consider pressure level
   ├─ NO → Silence is realistic (most days in garrison)
   └─ YES → Continue
       ↓
6. GetRealisticContent(WorldSituation)
   ├─ Query EventCatalog with world context
   ├─ Filter by tier, role, requirements
   ├─ Filter by context (automatically selects variants)
   ├─ Filter out recently fired content
   └─ Returns: List of realistic candidates (includes best-fit variants)
       ↓
7. SelectBestContent(candidates, PlayerPreferences)
   ├─ Score by player behavior match
   ├─ Score by world situation fit
   ├─ Weight by player engagement history
   └─ Pick winner (or null if nothing fits)
       ↓
8. Check Safety Limits (GlobalEventPacer)
   ├─ Too many today already? → Block
   ├─ Too soon after last? → Block
   └─ OK → Continue
       ↓
9. EventDeliveryManager.QueueEvent()
   ↓
10. Display to player
```

---

## Implementation Phases

### Phase 1: Foundation (Week 1)
**Goal:** Build core orchestrator infrastructure without changing existing behavior

**Tasks:**
1. Create `ContentOrchestrator.cs` class
2. Create `WorldStateAnalyzer.cs` class
3. Create `SimulationPressureCalculator.cs` class
4. Create `PlayerBehaviorTracker.cs` class
5. Create data models (`WorldSituation`, `SimulationPressure`, etc.)
6. Add comprehensive logging
7. Wire up daily tick (log only, don't affect live system)

**Deliverables:**
- Orchestrator receives daily ticks and logs decisions
- World state analysis works correctly
- Existing event system still works normally

**Acceptance Criteria:**
- Can see world situation analysis in logs
- Can see realistic frequency calculations
- Existing pacing system unaffected

---

### Phase 2: Content Selection Integration (Week 2)
**Goal:** Connect orchestrator to content selection

**Tasks:**
1. Integrate with `EventSelector.SelectEvent()`
2. Implement player behavior tracking
3. Add preference-based content scoring
4. Log what WOULD be selected
5. Compare with current system selections

**Deliverables:**
- Orchestrator selects content based on world state
- Logs show comparisons with old system
- Player preferences begin tracking

**Acceptance Criteria:**
- Selection makes sense for world situation
- High-fitness content is contextually appropriate
- Logging shows clear reasoning

---

### Phase 3: Cutover (Week 3)
**Goal:** Switch from old system to orchestrator

**Tasks:**
1. Add feature flag: `use_content_orchestrator`
2. When enabled: orchestrator handles all narrative events
3. Disable `EventPacingManager` scheduled checks
4. Remove evaluation hours from `GlobalEventPacer`
5. Remove quiet day random roll
6. Update config file

**Deliverables:**
- Orchestrator is live and delivering content
- Old schedule-driven logic disabled
- Config reflects new philosophy

**Acceptance Criteria:**
- Content fires based on world state, not timers
- Garrison feels quiet, campaigns feel busy
- Safety limits still prevent spam

---

### Phase 4: Orders Integration (Week 4)
**Goal:** Coordinate order system with orchestrator

**Background:** Orders are now multi-day duty assignments (see [Order Progression System](order-progression-system.md)). The orchestrator needs to understand two separate concepts:
1. **Order Issuance** - When OrderManager assigns a new duty to the player
2. **Order Events** - Things that happen during duty execution (handled by OrderProgressionBehavior)

**Tasks:**
1. Integrate `OrderManager` with orchestrator for **order issuance timing**
   - OrderManager checks with orchestrator before issuing new order
   - Orchestrator provides world state for order selection
   - Order issuance happens every 2-4 days (not competing with event budget)
   
2. Provide `WorldSituation` to `OrderProgressionBehavior` for **order event weighting**
   - Activity level modifies slot event chances during order execution
   - Quiet = ×0.3, Routine = ×0.6, Active = ×1.0, Intense = ×1.5
   - This is separate from narrative event frequency
   
3. Define **non-order time** handling
   - When player has no active order, orchestrator can fire camp life events
   - These use the standard narrative event frequency tables
   - Player can also use Camp Hub decisions during this time
   
4. Test full integration across order lifecycle

**Deliverables:**
- OrderManager coordinates issuance timing with orchestrator
- OrderProgressionBehavior receives world state for event weighting
- Camp life events fire appropriately between orders
- All systems work together smoothly

**Acceptance Criteria:**
- New orders issued every 2-4 days when player has none active
- Order slot events weighted by world state (siege = more events during duty)
- Camp life events fire during non-order time
- No overwhelming spam from combined systems

---

### Phase 5: Refinement & UI Integration (Week 5)
**Goal:** Polish orchestrator and integrate player-facing transparency features

**Tasks:**
1. **Company Report UI** (Player-Facing Transparency)
   - Add `BuildCompanyReportSection()` to `EnlistedNewsBehavior.cs`
   - Add `GetOrchestratorRhythmFlavor()` method to `ContentOrchestrator.cs`
   - Split current Daily Brief into "Company Report" and "Daily News"
   - Update Camp Hub menu to show new layout (see UI Systems doc)
   
2. **Playtest & Tune**
   - Playtest at each tier (T1-T9)
   - Tune frequency tables
   - Tune pressure modifiers
   - Tune dampening values
   
3. **Admin Tools & Documentation**
   - Add admin commands for testing
   - Documentation updates
   - Verify variant filtering works (if any variants added)

**UI Changes (See [UI Systems Master](../UI/ui-systems-master.md)):**

**Current Camp Hub Header:**
```
Lord: [Lord Name]
Your Rank: [Rank Title] (T#)

_____ COMPANY REPORT _____
[Combined daily brief with company + kingdom + player status]

_____ RECENT ACTIONS _____
[Event outcomes]
```

**New Camp Hub Header:**
```
_____ COMPANY REPORT _____
⚙️ CAMP STATUS: [Rhythm] - [Activity Level]

[Orchestrator context: Why this situation, what to expect]

_____ DAILY NEWS _____
[Kingdom macro news, casualties, major events]

_____ RECENT ACTIONS _____
[Event outcomes - unchanged]
```

**Company Report Examples:**

*Garrison (Quiet):*
```
⚙️ CAMP STATUS: Garrison - Quiet

Your lord holds at Pravend with no threats on the horizon. 
The camp has settled into routine. Little disturbs the 
daily rhythm.
```

*Siege Week 2 (High Intensity):*
```
⚔️ CAMP STATUS: Siege Operations - High Intensity

Week 2 of the siege at Pen Cannoc. Every hour brings new 
demands. The pressure never lets up. Your sergeants work 
overtime keeping order among exhausted soldiers.
```

*Campaign (Active):*
```
🏹 CAMP STATUS: Campaign - Active Operations

The army marches to war against Battania. Your sergeants 
manage the daily needs of soldiers on campaign.
```

**Technical Implementation:**

1. **New Method in ContentOrchestrator.cs:**
```csharp
public string GetOrchestratorRhythmFlavor()
{
    var situation = WorldStateAnalyzer.GetCurrentSituation();
    var activityLevel = GetCurrentActivityLevel();
    
    return (situation.Rhythm, activityLevel) switch
    {
        (MilitaryRhythm.Peacetime, ActivityLevel.Quiet) => 
            "Your lord holds at {SETTLEMENT} with no threats on the horizon. The camp has settled into routine. Little disturbs the daily rhythm.",
        (MilitaryRhythm.Siege, ActivityLevel.Intense) => 
            "Week {WEEK} of the siege at {TARGET}. Every hour brings new demands. The pressure never lets up.",
        // ... more combinations
        _ => ""
    };
}
```

2. **New Method in EnlistedNewsBehavior.cs:**
```csharp
public string BuildCompanyReportSection()
{
    if (ContentOrchestrator.Instance == null) 
        return string.Empty;
    
    var rhythmIcon = GetRhythmIcon();
    var rhythmName = GetRhythmName();
    var activityLevel = GetActivityLevelName();
    var flavorText = ContentOrchestrator.Instance.GetOrchestratorRhythmFlavor();
    
    return $"<span style=\"Header\">{rhythmIcon} CAMP STATUS: {rhythmName} - {activityLevel}</span>\n\n{flavorText}";
}
```

3. **Update EnlistedMenuBehavior.cs Camp Hub:**
   - Remove "Lord:" and "Your Rank:" header lines
   - Replace `BuildDailyBriefSection()` with `BuildCompanyReportSection()`
   - Add new `BuildDailyNewsSection()` (kingdom context only)
   - Keep `BuildRecentActionsSection()` unchanged

**Deliverables:**
- Polished, player-tested orchestrator
- Player-facing transparency (Company Report shows orchestrator thinking)
- Admin tools for debugging
- Complete documentation
- Content variant guide (for future additions)

**Acceptance Criteria:**
- Event frequency feels right for all situations
- Content always makes sense
- Silence feels appropriate, not empty
- **Player understands WHY event frequency varies** (via Company Report)
- Company Report aligns with Daily News (no contradictions)
- Context filtering works (variants selected appropriately)

---

### Phase 6: Content Variants (Post-Launch, Incremental)
**Goal:** Add contextual variety to high-traffic content

**When:** After orchestrator is proven (Week 6+)

**Approach:**
1. Identify repetitive events from playtesting
2. Create variants with different `requirements.context`
3. Add as JSON files (no code changes)
4. Test variant selection in different world states

**Priority Order:**
1. Training decisions (high-traffic, easy to vary)
2. Rest/camp decisions (frequent use)
3. Common events (seen repeatedly)
4. Role-specific variants (add depth)

**Example Variants:**
```json
// Training variants by intensity
dec_weapon_drill          → Base (any context)
dec_weapon_drill_light    → Garrison (low pressure)
dec_weapon_drill_intense  → Campaign (high pressure)

// Rest variants by effectiveness
dec_rest                  → Base (any context)
dec_rest_garrison         → Camp (more effective)
dec_rest_exhausted        → Siege (less effective)
```

**No Code Changes Needed:**
- EventRequirementChecker already filters by context
- Orchestrator already scores all eligible content
- Variants compete naturally in selection pool

---

## Technical Specifications

### File Creation Checklist

**CRITICAL:** This project uses old-style .csproj. New files must be manually added.

**New Files to Create:**
1. `src/Features/Content/ContentOrchestrator.cs` → Add to Enlisted.csproj
2. `src/Features/Content/WorldStateAnalyzer.cs` → Add to Enlisted.csproj
3. `src/Features/Content/SimulationPressureCalculator.cs` → Add to Enlisted.csproj
4. `src/Features/Content/PlayerBehaviorTracker.cs` → Add to Enlisted.csproj
5. `src/Features/Content/Models/WorldSituation.cs` → Add to Enlisted.csproj
6. `src/Features/Content/Models/SimulationPressure.cs` → Add to Enlisted.csproj
7. `src/Features/Content/Models/PlayerPreferences.cs` → Add to Enlisted.csproj

**For Each File:**
```xml
<!-- Add to Enlisted.csproj ItemGroup around line 142+ -->
<Compile Include="src\Features\Content\ContentOrchestrator.cs"/>
<Compile Include="src\Features\Content\WorldStateAnalyzer.cs"/>
<!-- ... etc -->
```

---

### API Verification Requirements

**Before implementing, verify these APIs in local decompile:**
- Location: `C:\Dev\Enlisted\Decompile\`
- Target: Bannerlord v1.3.13

**Key APIs to Verify:**
1. `MobileParty.Party.MapEvent` (battle detection)
2. `MobileParty.Party.SiegeEvent` (siege detection)
3. `MobileParty.CurrentSettlement` (garrison detection)
4. `MobileParty.Army` (army status)
5. `FactionManager.IsAtWarAgainstFaction()` (war status)
6. `Campaign.Current.MapSceneWrapper.GetFaceTerrainType()` (terrain context)
7. `CampaignTime.Now` (time checks)

**If API differs from expectation:** Update implementation to match decompile, not assumptions.

---

### Logging Standards

**Log Category:** `"Orchestrator"`

**Log Locations:**
- All logs write to: `<BannerlordInstall>\Modules\Enlisted\Debugging\`
- Example: `C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\Enlisted\Debugging\`

**Required Logging:**
```csharp
using Enlisted.Mod.Core.Logging;

// Decision logging (every daily tick)
ModLogger.Debug("Orchestrator", $"World State: {situation.LifePhase}, Activity: {situation.ExpectedActivity}");
ModLogger.Debug("Orchestrator", $"Pressure: {pressure.Value} from {string.Join(", ", pressure.Sources)}");
ModLogger.Info("Orchestrator", $"Realistic frequency: {frequency:F2} events/week");

// Selection logging
ModLogger.Debug("Orchestrator", $"Eligible candidates: {candidates.Count}");
ModLogger.Info("Orchestrator", $"Selected: {selected.Id} (fitness: {score:F1})");

// Silence logging (important for understanding behavior)
ModLogger.Debug("Orchestrator", $"No content today - {situation.LifePhase} with {situation.ExpectedActivity} activity");

// Block logging
ModLogger.Debug("Orchestrator", $"Blocked {selected.Id}: {blockReason}");
```

**Performance:** Use Debug level for detailed tracing, Info for key decisions. Avoid logging inside tight loops.

---

### Save/Load Integration

**State to Persist:**
```csharp
// In ContentOrchestrator
public override void SyncData(IDataStore dataStore)
{
    SaveLoadDiagnostics.SafeSyncData(this, dataStore, () =>
    {
        // Player behavior tracking
        dataStore.SyncData("orchestrator_behaviorCounts", ref _behaviorCounts);
        dataStore.SyncData("orchestrator_contentEngagement", ref _contentEngagement);
        
        // Recent activity tracking for dampening
        dataStore.SyncData("orchestrator_eventsThisWeek", ref _eventsThisWeek);
        dataStore.SyncData("orchestrator_lastWeekReset", ref _lastWeekReset);
    });
}
```

**CRITICAL:** If using custom Dictionary types, register in `EnlistedSaveDefiner.cs`:
```csharp
// In DefineContainerDefinitions()
ConstructContainerDefinition(typeof(Dictionary<string, int>));
```

---

### Existing System Integration Points

**1. GlobalEventPacer** (`src/Features/Content/GlobalEventPacer.cs`)
- Keep safety limit checks: `CanFireAutoEvent()`, `RecordAutoEvent()`
- Remove: Evaluation hours, quiet day random roll
- Orchestrator sets `state.IsQuietDay` based on world state

**2. EventSelector** (`src/Features/Content/EventSelector.cs`)
- Keep: Requirement checking, role weighting
- Enhance: Add fitness scoring based on world state
- Integration: `SelectEvent()` now receives `WorldSituation` parameter

**3. EventDeliveryManager** (`src/Features/Content/EventDeliveryManager.cs`)
- Keep: FIFO queue, popup display, effect application
- No changes needed - orchestrator uses existing `QueueEvent()`

**4. EscalationManager** (`src/Features/Escalation/EscalationManager.cs`)
- Keep: Threshold events bypass orchestrator (immediate consequences)
- Integration: Orchestrator reads escalation state for pressure calculation

**5. OrderManager** (`src/Features/Orders/Behaviors/OrderManager.cs`)
- Phase 4 integration: Orchestrator coordinates order issuance timing
- Keep: Order selection logic (which order to assign)
- Add: Check with orchestrator before issuing new order
- Note: Order execution is now handled by OrderProgressionBehavior (see Order Progression System)

**5b. OrderProgressionBehavior** (`src/Features/Orders/Behaviors/OrderProgressionBehavior.cs`) - NEW
- Handles 4-phase-per-day order execution
- Receives WorldSituation from orchestrator for event slot weighting
- Processes order phases on hourly tick (6am, 12pm, 6pm, 12am)
- See [Order Progression System](order-progression-system.md) for full specification

**6. EnlistedNewsBehavior** (`src/Features/Interface/Behaviors/EnlistedNewsBehavior.cs`)
- Phase 5: Add `BuildCompanyReportSection()`
- Split: `BuildDailyBriefSection()` into Daily News section
- Integration: Call `ContentOrchestrator.GetOrchestratorRhythmFlavor()`

**7. EnlistmentBehavior** (`src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`)
- Read-only: Check `IsEnlisted` status
- Read-only: Access `EnlistedLord` for world state analysis

**8. CompanyNeedsManager** (`src/Features/Company/Behaviors/CompanyNeedsManager.cs`)
- Read-only: Check Supplies, Morale, Rest for pressure calculation
- No changes needed

---

### Key Classes

**Location:** `src/Features/Content/`  
**Namespace:** `Enlisted.Features.Content`

```csharp
namespace Enlisted.Features.Content
{
    /// <summary>
    /// Coordinates all content delivery based on world state and simulation pressure.
    /// Replaces schedule-driven event pacing with context-aware content selection.
    /// </summary>
    public class ContentOrchestrator : CampaignBehaviorBase
    {
        private const string LogCategory = "Orchestrator";
        
        public static ContentOrchestrator Instance { get; private set; }
        
        // Tracking for dampening
        private int _eventsThisWeek;
        private CampaignTime _lastWeekReset = CampaignTime.Zero;
        
        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }
        
        private void OnDailyTick()
        {
            if (!EnlistmentBehavior.Instance.IsEnlisted) 
            {
                return;
            }
            
            // Reset weekly counter
            if (CampaignTime.Now.GetDayOfYear != _lastWeekReset.GetDayOfYear)
            {
                if ((CampaignTime.Now - _lastWeekReset).ToDays >= 7)
                {
                    _eventsThisWeek = 0;
                    _lastWeekReset = CampaignTime.Now;
                }
            }
            
            // 1. Analyze world situation
            var worldSituation = WorldStateAnalyzer.AnalyzeSituation();
            ModLogger.Debug(LogCategory, $"World State: {worldSituation.LifePhase}, Activity: {worldSituation.ExpectedActivity}");
            
            // 2. Calculate simulation pressure
            var pressure = SimulationPressureCalculator.CalculatePressure();
            ModLogger.Debug(LogCategory, $"Pressure: {pressure.Value} from [{string.Join(", ", pressure.Sources)}]");
            
            // 3. Determine realistic frequency
            var frequency = DetermineRealisticFrequency(worldSituation, pressure);
            ModLogger.Info(LogCategory, $"Realistic frequency: {frequency:F2} events/week");
            
            // 4. Should content fire today?
            if (!ShouldDeliverContent(frequency, worldSituation))
            {
                LogSilence(worldSituation);
                return;
            }
            
            // 5. Get eligible content
            var candidates = GetEligibleContent(worldSituation);
            
            if (candidates.Count == 0)
            {
                ModLogger.Debug(LogCategory, "No eligible content for current situation");
                return;
            }
            
            ModLogger.Debug(LogCategory, $"Eligible candidates: {candidates.Count}");
            
            // 6. Select best fit
            var selected = SelectBestContent(candidates, worldSituation);
            
            if (selected == null)
            {
                ModLogger.Debug(LogCategory, "No content above fitness threshold");
                return;
            }
            
            // 7. Check safety limits
            if (!GlobalEventPacer.CanFireAutoEvent(selected.Id, "narrative", out var reason))
            {
                ModLogger.Debug(LogCategory, $"Blocked {selected.Id}: {reason}");
                return;
            }
            
            // 8. Deliver
            EventDeliveryManager.Instance.QueueEvent(selected);
            GlobalEventPacer.RecordAutoEvent(selected.Id, "narrative");
            PlayerBehaviorTracker.RecordContentDelivered(selected);
            _eventsThisWeek++;
            
            ModLogger.Info(LogCategory, $"Delivered: {selected.Id} (events this week: {_eventsThisWeek})");
        }
        
        private void LogSilence(WorldSituation situation)
        {
            ModLogger.Debug(LogCategory, $"Quiet day - {situation.LifePhase} with {situation.ExpectedActivity} activity");
        }
    }
    
    public static class WorldStateAnalyzer
    {
        public static WorldSituation AnalyzeSituation()
        {
            var lord = EnlistmentBehavior.Instance.EnlistedLord;
            var party = MobileParty.MainParty;
            
            // Analyze what lord is doing
            var lordSituation = DetermineLordSituation(lord, party);
            
            // Analyze kingdom war status
            var warStance = DetermineWarStance(lord.MapFaction);
            
            // Determine life phase
            var lifePhase = DetermineLifePhase(lordSituation, warStance);
            
            // Calculate expected activity level
            var activityLevel = DetermineActivityLevel(lifePhase, lordSituation);
            
            // Map to realistic frequency
            var frequency = MapToRealisticFrequency(lifePhase, activityLevel);
            
            return new WorldSituation
            {
                LordSituation = lordSituation,
                WarStance = warStance,
                LifePhase = lifePhase,
                ExpectedActivity = activityLevel,
                RealisticEventFrequency = frequency
            };
        }
    }
    
    public static class SimulationPressureCalculator
    {
        public static SimulationPressure CalculatePressure()
        {
            float pressure = 0;
            var sources = new List<string>();
            
            // Check company state
            var needs = CompanyNeedsManager.Instance;
            if (needs.Supplies < 30) { pressure += 20; sources.Add("Low Supplies"); }
            if (needs.Rest < 30) { pressure += 15; sources.Add("Exhausted Company"); }
            
            // Check escalation
            var escalation = EscalationManager.Instance.State;
            if (escalation.Discipline > 70) { pressure += 25; sources.Add("High Discipline"); }
            if (escalation.Scrutiny > 70) { pressure += 20; sources.Add("Under Scrutiny"); }
            
            // Check player state
            var hero = Hero.MainHero;
            if (hero.HitPoints < hero.MaxHitPoints * 0.5f) { pressure += 15; sources.Add("Wounded"); }
            
            // Check recent orders
            if (OrderManager.Instance.RecentlyFailed) { pressure += 10; sources.Add("Failed Order"); }
            
            // Check location
            if (IsInEnemyTerritory()) { pressure += 15; sources.Add("Enemy Territory"); }
            
            return new SimulationPressure
            {
                Value = Math.Min(100, pressure),
                Sources = sources
            };
        }
    }
    
    public static class PlayerBehaviorTracker
    {
        private static Dictionary<string, int> _behaviorCounts = new Dictionary<string, int>();
        private static Dictionary<string, int> _contentEngagement = new Dictionary<string, int>();
        
        public static void RecordChoice(string choiceTag)
        {
            if (!_behaviorCounts.ContainsKey(choiceTag))
                _behaviorCounts[choiceTag] = 0;
            
            _behaviorCounts[choiceTag]++;
        }
        
        public static PlayerPreferences GetPreferences()
        {
            return new PlayerPreferences
            {
                CombatVsSocial = CalculateCombatPreference(),
                RiskyVsSafe = CalculateRiskPreference(),
                LoyalVsSelfServing = CalculateLoyaltyPreference()
            };
        }
    }
}
```

### Data Models

**Location:** `src/Features/Content/Models/`  
**Namespace:** `Enlisted.Features.Content.Models`

```csharp
/// <summary>
/// Snapshot of current world state for content selection.
/// Built by WorldStateAnalyzer on each daily tick.
/// </summary>
public class WorldSituation
{
    public LordSituation LordIs { get; set; }  // What the enlisted lord is doing
    public WarStance KingdomStance { get; set; }  // Kingdom's war posture
    public LifePhase CurrentPhase { get; set; }  // Overall military life phase
    public ActivityLevel ExpectedActivity { get; set; }  // Expected event density
    public float RealisticEventFrequency { get; set; }  // Events per week (base)
    
    // Context details for flavor text
    public Settlement CurrentSettlement { get; set; }  // If garrisoned
    public Settlement TargetSettlement { get; set; }  // If marching/sieging
    public int DaysInCurrentPhase { get; set; }  // How long in this phase
    public bool InEnemyTerritory { get; set; }  // Affects pressure
}

/// <summary>
/// Simulation pressure from company/player state.
/// Modifies realistic frequency up or down.
/// </summary>
public class SimulationPressure
{
    public float Value { get; set; }  // 0-100 scale
    public List<string> Sources { get; set; }  // Human-readable reasons
    
    /// <summary>
    /// Converts pressure to frequency modifier.
    /// High pressure = more frequent events.
    /// </summary>
    public float GetFrequencyModifier()
    {
        // 0 pressure = 1.0x (no change)
        // 50 pressure = 1.15x
        // 100 pressure = 1.3x
        return 1.0f + (Value / 100f * 0.3f);
    }
}

/// <summary>
/// Player behavior tracking for content preferences.
/// Learns from choices over time.
/// </summary>
public class PlayerPreferences
{
    public float CombatVsSocial { get; set; }  // 0=social only, 1=combat only, 0.5=balanced
    public float RiskyVsSafe { get; set; }  // 0=always safe, 1=always risky
    public float LoyalVsSelfServing { get; set; }  // 0=selfish, 1=dutiful
    
    // Choice counts for debugging
    public int TotalChoicesMade { get; set; }
    public int CombatChoices { get; set; }
    public int SocialChoices { get; set; }
    public int RiskyChoices { get; set; }
    public int SafeChoices { get; set; }
}

/// <summary>
/// What the enlisted lord is currently doing.
/// Mapped from party/settlement/army state.
/// </summary>
public enum LordSituation
{
    PeacetimeGarrison,      // In settlement, no wars
    PeacetimeRecruiting,    // Moving between villages in peace
    WarMarching,            // Moving during wartime
    WarActiveCampaign,      // In army during wartime
    SiegeAttacking,         // Besieging enemy settlement
    SiegeDefending,         // Defending own settlement
    Defeated,               // Post-battle defeat recovery
    Captured                // Lord is prisoner
}

/// <summary>
/// Overall military life phase.
/// Determines baseline event frequency.
/// </summary>
public enum LifePhase
{
    Peacetime,   // No active wars
    Campaign,    // Active warfare
    Siege,       // Siege operations
    Recovery,    // Post-defeat/injury
    Crisis       // Multiple severe pressures
}

/// <summary>
/// Expected activity level.
/// Maps to concrete event frequency targets.
/// </summary>
public enum ActivityLevel
{
    Quiet,      // ~1 event/week (0.14/day)
    Routine,    // ~3 events/week (0.43/day)
    Active,     // ~5 events/week (0.71/day)
    Intense     // ~7 events/week (1.0/day)
}

/// <summary>
/// Kingdom war posture.
/// Affects transition between life phases.
/// </summary>
public enum WarStance
{
    Peace,          // No active wars
    Defensive,      // Wars declared against us
    Offensive,      // Wars we declared
    MultiWar,       // Fighting multiple factions
    Desperate       // Losing badly
}
```

**Note on Enums:** These are already simple types (int-castable) so they don't need save definer registration unless used in complex containers.

---

### Configuration Schema

**File:** `ModuleData/Enlisted/enlisted_config.json`

**Current Section (to be updated):**
```json
{
  "decision_events": {
    "enabled": true,
    "events_folder": "Events",
    "pacing": {
      "max_per_day": 2,
      "max_per_week": 8,
      "min_hours_between": 6,
      "per_event_cooldown_days": 7,
      "per_category_cooldown_days": 1
    }
  }
}
```

**New Section (Phase 3 cutover):**
```json
{
  "decision_events": {
    "enabled": true,
    "events_folder": "Events",
    
    // Safety limits (prevent spam)
    "pacing": {
      "max_per_day": 2,
      "max_per_week": 8,
      "min_hours_between": 4,
      "per_event_cooldown_days": 7,
      "per_category_cooldown_days": 1
    },
    
    // Orchestrator configuration
    "orchestrator": {
      "enabled": true,
      "log_decisions": true,
      "fitness_threshold": 40,
      
      // World state → daily event probability
      "frequency": {
        "peacetime_garrison": 0.14,       // ~1 event per week
        "peacetime_recruiting": 0.35,     // ~2.5 per week
        "war_marching": 0.5,              // ~3.5 per week
        "war_active_campaign": 0.7,       // ~5 per week
        "siege_attacking": 0.57,          // ~4 per week
        "siege_defending": 1.0,           // ~7 per week (capped by max_per_day)
        "lord_captured": 0.07,            // ~0.5 per week
        "defeated_recovery": 0.21         // ~1.5 per week
      },
      
      // Activity dampening after spikes
      "dampening": {
        "after_busy_week_multiplier": 0.7,     // Reduce after >5 events/week
        "after_quiet_week_multiplier": 1.2,    // Increase after <1 event/week
        "after_battle_cooldown_days": 1.5,     // Quiet period post-battle
        "busy_week_threshold": 5,
        "quiet_week_threshold": 1
      },
      
      // Simulation pressure modifiers (additive to base frequency)
      "pressure": {
        "max_modifier": 0.3,                   // Pressure can add up to 30% more frequency
        "low_supplies_threshold": 30,          // Supplies < 30 adds pressure
        "low_morale_threshold": 30,            // Morale < 30 adds pressure
        "high_scrutiny_threshold": 70,         // Scrutiny > 70 adds pressure
        "high_discipline_threshold": 70        // Discipline > 70 adds pressure
      }
    }
  }
}
```

**Config Loading:** Use existing `EventPacingConfig` class, extend with new `OrchestratorConfig` nested class.

---

### Build & Test Process

**Build Command:**
```powershell
cd C:\Dev\Enlisted\Enlisted
dotnet build -c "Enlisted RETAIL" /p:Platform=x64
```

**Output Location:**
```
<BannerlordInstall>/Modules/Enlisted/bin/Win64_Shipping_Client/
```

**Testing Steps:**
1. Build successfully (no errors)
2. Launch Bannerlord with mod enabled
3. Check log file: `<BannerlordInstall>/Modules/Enlisted/Debugging/enlisted.log`
4. Look for `[Orchestrator]` log entries
5. Verify world state analysis appears on daily tick
6. Test each phase (garrison, campaign, siege)
7. Monitor event frequency over multiple in-game weeks

**Admin Commands (add for testing):**
```csharp
// Toggle orchestrator on/off without rebuild
[CommandLineFunctionality.CommandLineArgumentFunction("toggle_orchestrator", "enlisted")]
public static string ToggleOrchestrator(List<string> args)
{
    ContentOrchestrator.Instance.Enabled = !ContentOrchestrator.Instance.Enabled;
    return $"Orchestrator: {(ContentOrchestrator.Instance.Enabled ? "ON" : "OFF")}";
}

// Force specific world state for testing
[CommandLineFunctionality.CommandLineArgumentFunction("force_world_state", "enlisted")]
public static string ForceWorldState(List<string> args)
{
    // Usage: enlisted.force_world_state siege
    // Temporarily overrides world state detection
}
```

---

### Implementation Dependencies

**Phase 1 requires:**
- ✅ Existing `EnlistmentBehavior` (read lord status)
- ✅ Existing `CompanyNeedsManager` (read company state)
- ✅ Existing `EscalationManager` (read escalation state)
- ✅ Existing `GlobalEventPacer` (safety limits)
- ✅ Bannerlord API: `MobileParty`, `Settlement`, `Kingdom`, `FactionManager`

**Phase 2 requires:**
- ✅ Phase 1 complete
- ✅ Existing `EventCatalog` (content pool)
- ✅ Existing `EventSelector` (selection logic to enhance)
- ✅ Existing `EventRequirementChecker` (eligibility filtering)

**Phase 3 requires:**
- ✅ Phase 1 & 2 complete
- ⚠️ Modify `EventPacingManager` (disable schedule logic)
- ⚠️ Modify `GlobalEventPacer` (remove evaluation hours)
- ⚠️ Update `enlisted_config.json` (new schema)

**Phase 4 requires:**
- ✅ Phase 3 complete
- ⚠️ Modify `OrderManager` (integrate timing)
- ⚠️ Coordinate order/event frequency budget

**Phase 5 requires:**
- ✅ Phase 3 complete
- ⚠️ Modify `EnlistedNewsBehavior` (add Company Report)
- ⚠️ Modify `EnlistedMenuBehavior` (update header layout)
- ✅ Existing color scheme system (for flavor text)

**Legend:**
- ✅ = Read-only integration, no changes needed
- ⚠️ = Requires modification to existing code
```

---

## Testing & Validation

### Unit Tests

1. **WorldStateAnalyzer Tests**
   - Garrison + Peace = Quiet frequency
   - Campaign + War = Active frequency
   - Siege Defense = Intense frequency
   - Lord captured = Almost nothing

2. **SimulationPressureCalculator Tests**
   - Low supplies adds pressure
   - Victory reduces pressure
   - Multiple sources accumulate correctly

3. **RealisticFrequency Tests**
   - Frequency matches world situation
   - Dampening works after busy weeks
   - Pressure modifiers apply correctly

### Integration Tests

1. **Scenario: Peacetime Garrison (4 weeks)**
   - Expected: ~1 event per week
   - Test: Silence is dominant pattern
   - Test: Events are contextually appropriate

2. **Scenario: Active Campaign (2 weeks)**
   - Expected: 5-7 events per week
   - Test: Busy but not overwhelming
   - Test: Battle incidents fire immediately

3. **Scenario: Siege Defense (1 week)**
   - Expected: Multiple events per day
   - Test: Crisis feel
   - Test: Safety limits prevent spam (max 2/day)

4. **Scenario: Transition (Campaign → Garrison)**
   - Expected: Activity drops off naturally
   - Test: Dampening after busy period
   - Test: Return to quiet routine

### Playtest Validation

**T1-T4 Playthrough (6 hours):**
- Track event frequency per week
- Track appropriateness of events
- Track player feedback on pacing

**T5-T6 Playthrough (6 hours):**
- Same tracking
- Verify NCO content fires

**T7-T9 Playthrough (6 hours):**
- Same tracking
- Verify commander content fires

---

## Sandbox Philosophy

### What We're Building

**NOT:** A drama manager that creates story arcs  
**YES:** A life simulator that respects military reality

### Core Principles

1. **World State Drives Everything**
   - Your lord's situation determines activity level
   - War creates activity, peace creates quiet
   - Garrison duty IS boring - embrace it
   - Campaigns have natural rhythms
   - Crisis is temporary, routine is normal

2. **Silence is Often Realistic**
   - Peacetime garrison: Days with nothing happening
   - Winter camps: Weeks of routine
   - Long marches: Uneventful travel
   - After crisis: Natural recovery periods
   - Not "breathing room" - just reality

3. **Pressure from Simulation, Not Drama**
   - Low supplies creates realistic pressure
   - Angry lord creates realistic pressure
   - Failed orders create realistic consequences
   - NOT manufactured for excitement
   - Emerges naturally from living the life

4. **Player Agency Always Preserved**
   - Camp Hub always available
   - Fixed cycles remain fixed (muster, pay)
   - Earned consequences still fire
   - Orchestrator coordinates, never forces
   - Player makes all meaningful choices

5. **Stories Emerge, Not Authored**
   - No predetermined arcs
   - No forced dramatic beats
   - No "character development" tracking
   - Player's choices create their story
   - Simulation provides the canvas

### What Success Looks Like

**Good Simulation:**
- "I spent 3 weeks in garrison. Nothing happened. Then we got mobilized and everything changed."
- "The campaign was brutal. Battle after battle. I barely survived. Then we returned home and it's been quiet."
- "I've been jumping between lords, chasing better pay. Each one's different."
- "Been with the same lord for 200 days. Promoted three times. Survived two sieges. Buried comrades. This feels like a military career."

**Bad Simulation (What We're Avoiding):**
- "Something dramatic happens every week like clockwork."
- "The game made me a hero through scripted events."
- "I can predict when events will fire."
- "Every day feels equally important."

---

## Risk Assessment & Mitigation

| Risk | Impact | Mitigation |
|------|--------|------------|
| **Too few events** | Mod feels inactive | Tunable frequency tables; playtest extensively |
| **Too many events** | Player fatigue | Safety limits prevent spam; dampening after activity spikes |
| **Breaking saves** | Players lose progress | Keep old state fields, migration code, graceful defaults |
| **Performance** | Context building expensive | Cache world state; only recalculate on significant changes |
| **Bugs in selection** | Wrong content fires | Extensive logging; admin commands; unit tests |

---

## Timeline Summary

| Week | Phase | Key Deliverables |
|------|-------|------------------|
| 1 | Foundation | Infrastructure, world state analysis, pressure calculation |
| 2 | Selection | Content selection, player behavior tracking |
| 3 | Cutover | Switch to orchestrator, remove schedule logic |
| 4 | Orders | Integrate order timing |
| 5 | Polish | Playtesting, tuning, documentation |
| 6+ | Variants | Add content variants incrementally (no code changes) |

**Core Implementation:** 5 weeks  
**Content Enhancement:** Ongoing (variants added as needed)

---

## Content Variant System Summary

### Design Principles

**Static Costs/Rewards:**
- All costs, rewards, and effects are fixed in JSON
- No dynamic adjustment of values at runtime
- Transparent to players (tooltips show exact values)
- Moddable (players can edit JSON values)

**Variant Selection:**
- Orchestrator controls WHICH content fires (variant selection)
- JSON controls WHAT happens (costs, rewards, effects)
- Requirements system filters variants by context
- Best-fit variant selected automatically

**No Code Changes:**
- Variants work via existing `requirements.context` filtering
- EventRequirementChecker already handles context matching
- Orchestrator already scores all eligible content
- Add variants as JSON files - system handles rest

### Example: Rest Decision Variants

**Base Event (Always Available):**
```json
{
  "id": "dec_rest",
  "requirements": { "tier": { "min": 1 } },
  "costs": { "fatigue": 2 },
  "rewards": { "fatigueRelief": 5 }
}
```

**Garrison Variant (Better Rest):**
```json
{
  "id": "dec_rest_garrison",
  "requirements": { 
    "tier": { "min": 1 },
    "context": ["Camp"]  // ← Garrison only
  },
  "costs": { "fatigue": 1 },        // ← Cheaper
  "rewards": { "fatigueRelief": 8 }  // ← More effective
}
```

**Crisis Variant (Worse Rest):**
```json
{
  "id": "dec_rest_exhausted",
  "requirements": { 
    "tier": { "min": 1 },
    "context": ["Siege", "Battle"]  // ← Crisis only
  },
  "costs": { "fatigue": 2 },
  "rewards": { "fatigueRelief": 2 }  // ← Less effective
}
```

**Orchestrator Behavior:**
- **Garrison:** Selects `dec_rest_garrison` (best context match)
- **Campaign:** Selects `dec_rest` (base fallback)
- **Siege:** Selects `dec_rest_exhausted` (crisis variant)

**Player Experience:**
- Garrison feels different from campaign feels different from siege
- Same "rest" action, different effectiveness based on situation
- All transparent in tooltips (exact costs/rewards shown)

### Implementation Status

✅ **Weeks 1-5:** Orchestrator handles current content (no variants)  
✅ **Week 6+:** Add variants incrementally (no code changes)  
✅ **Player modding:** Edit JSON to create custom variants

**See:**
- [Event System Schemas - Content Variants Pattern](event-system-schemas.md#content-variants-pattern) - Complete variant documentation
- [Content System Architecture - Variant Selection](content-system-architecture.md#content-variant-selection-future-enhancement) - Technical flow

---

## Next Steps

1. ✅ Review this plan - confirm it matches vision
2. 🔵 Start Phase 1 - build foundation without breaking existing system
3. 🔵 Test world state analysis with logging
4. 🔵 Begin content selection integration

This is a major architectural shift from "pacing algorithm" to "reality simulator." It will make the enlisted experience feel like living a military life in Calradia's endless wars — exactly what the mod is meant to be.

---

## References & Related Docs

### Core Documentation
| Document | Purpose | When to Read |
|----------|---------|--------------|
| [BLUEPRINT.md](../../BLUEPRINT.md) | Project architecture, coding standards, API verification | Before starting implementation |
| [INDEX.md](../../INDEX.md) | Complete documentation catalog | Finding specific systems |
| [Core Gameplay](../Core/core-gameplay.md) | How all systems work together | Understanding project context |

### Content System Docs
| Document | Purpose | When to Read |
|----------|---------|--------------|
| [Content System Architecture](content-system-architecture.md) | Current system overview, components, flow | Phase 1 & 2 - Understanding what exists |
| [Event System Schemas](event-system-schemas.md) | JSON data structures, field definitions | When working with EventCatalog/EventDefinition |
| [Event Catalog by System](../../Content/event-catalog-by-system.md) | All events, decisions, orders | Understanding content scope |

### UI & Reporting Docs
| Document | Purpose | When to Read |
|----------|---------|--------------|
| [UI Systems Master](../UI/ui-systems-master.md) | Camp Hub, menu structure, current header | Phase 5 - UI integration |
| [News & Reporting System](../UI/news-reporting-system.md) | Daily Brief generation, feeds | Phase 5 - Company Report integration |
| [Color Scheme](../UI/color-scheme.md) | Text styling for flavor text | Phase 5 - Formatting output |

### Integration Point Docs
| Document | Purpose | When to Read |
|----------|---------|--------------|
| [Enlistment System](../Core/enlistment.md) | How enlistment works, lord tracking | Phase 1 - World state analysis |
| [Company Needs](../Company/company-needs.md) | Supplies, Morale, Rest mechanics | Phase 1 - Pressure calculation |
| [Escalation System](../Escalation/escalation-system.md) | Reputation, Scrutiny, Discipline | Phase 1 - Pressure calculation |
| [Orders System](../Orders/orders-system.md) | How orders work currently | Phase 4 - Order integration |

### API Reference
| Resource | Purpose | When to Use |
|----------|---------|-------------|
| Local Decompile | Verify Bannerlord API | ALWAYS before using any TaleWorlds API |
| Location | `C:\Dev\Enlisted\Decompile\` | Target: v1.3.13 |
| Key Assemblies | `TaleWorlds.CampaignSystem`, `TaleWorlds.Core` | Party, Settlement, Kingdom APIs |

### Configuration Files
| File | Purpose | Phase |
|------|---------|-------|
| `ModuleData/Enlisted/enlisted_config.json` | Frequency tables, pacing limits | Phase 3 - Cutover |
| `ModuleData/Languages/enlisted_strings.xml` | Localization for flavor text | Phase 5 - UI integration |
| `Enlisted.csproj` | File compilation list | All phases - Adding new files |

### Key Concepts Referenced
- **Emergent Identity:** Player choices create the story, not predetermined arcs
- **Native Integration:** Use existing Bannerlord systems, minimal custom UI
- **Data-Driven Content:** JSON events/decisions with XML localization
- **Realistic Military Hierarchy:** Content reflects actual military life rhythms
- **Performance-Friendly Logging:** All features include diagnostic logging

### Common Issues & Solutions
See [Common Pitfalls](../../BLUEPRINT.md#common-pitfalls) in Blueprint for:
- Old-style .csproj file management
- Equipment slot iteration
- Gold transactions
- API verification process
- ReSharper warning handling
- Tooltip requirements

