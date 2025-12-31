# Content Orchestrator: Sandbox Life Simulator

**Status:** ⚠️ In Progress (Phases 1-5.5 Complete, Phase 6A-F Complete, **Phase 6G BLOCKED**)
**Priority:** High
**Complexity:** Major architectural change
**Created:** 2025-12-24
**Last Updated:** 2025-12-31

---

## ⚠️ CRITICAL: Phase 6 Incomplete - Missing Decisions

**Discovery (2025-12-31):** Phase 6 created 29 camp opportunities, but the target decisions they reference don't exist.

### Two-Layer Architecture

```
OPPORTUNITIES (what shows in menu)     DECISIONS (what fires when clicked)
────────────────────────────────────   ────────────────────────────────────
camp_opportunities.json                decisions.json
  opp_weapon_drill                       dec_training_drill ← MISSING
    targetDecision: "dec_training_drill"
  
  opp_card_game                          dec_gamble_cards ← MISSING
    targetDecision: "dec_gamble_cards"
    
  opp_equipment_maintenance              dec_maintain_gear ← EXISTS ✓
    targetDecision: "dec_maintain_gear"
```

**Opportunities:** Orchestrator-curated activities shown in DECISIONS menu. Contains display text, fitness scoring, order compatibility, detection logic.

**Decisions:** The actual event with options, rewards, and result text that fires when player clicks an opportunity.

### Current State

| Status | Count | Details |
|--------|-------|---------|
| ✅ Opportunities exist | 29 | All defined in camp_opportunities.json |
| ✅ Decisions exist | 3 | dec_maintain_gear, dec_write_letter, dec_gamble_high |
| ❌ Decisions missing | 26 | Target decisions never created |
| ❌ Old decisions | 38 | Pre-orchestrator static decisions (DEPRECATED) |

### Required Fix (Phase 6G)

**Step 0: Delete Old System**
- Open `ModuleData/Enlisted/Decisions/decisions.json`
- Delete all 35 old static decisions (from pre-orchestrator era)
- Keep only: `dec_maintain_gear`, `dec_write_letter`, `dec_gamble_high`
- These 35 decisions were designed for static menu browsing, not orchestrator curation

**Step 1: Create New Decisions**
- Create 26 new decisions matching the `targetDecision` IDs from `camp_opportunities.json`
- Design: 2-3 options, light RP moments, clear tooltips
- See Phase 6G prompt in `content-orchestrator-prompts.md` for full structure

**Step 2: Validate**
- Run `python tools/events/validate_events.py`
- Build and test in-game

### Missing Decisions List

**Training (5 missing):**
- dec_training_drill, dec_training_spar, dec_training_formation, dec_training_veteran, dec_training_archery

**Social (7 missing):**
- dec_social_stories, dec_tavern_drink, dec_social_storytelling, dec_drinking_contest, dec_social_singing, dec_arm_wrestling

**Economic (4 missing):**
- dec_gamble_cards, dec_gamble_dice, dec_forage, dec_work_repairs, dec_trade_browse

**Recovery (5 missing):**
- dec_rest_sleep, dec_help_wounded, dec_prayer, dec_rest_short, dec_meditate

**Special (5 missing):**
- dec_officer_audience, dec_baggage_access, dec_mentor_recruit, dec_volunteer_extra, dec_night_patrol
**Related Docs:** [BLUEPRINT](../BLUEPRINT.md), [Order Progression System](order-progression-system.md), [Order Events Master](order-events-master.md), [Orders Content](orders-content.md), [Camp Background Simulation](camp-background-simulation.md), [Content System Architecture](../Features/Content/content-system-architecture.md), [Event System Schemas](../Features/Content/event-system-schemas.md), [News & Reporting System](../Features/UI/news-reporting-system.md)

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
9. [Edge Cases & Special States](#edge-cases--special-states)
10. [References & Related Docs](#references--related-docs)

---

## Quick Start Guide

**For implementers: Read this first, then proceed to detailed sections.**

### 30-Second Overview
Replace schedule-driven event pacing with world-state-driven content orchestration. Garrison duty becomes quiet, campaigns become busy, sieges become intense. All automatic, all realistic.

### Implementation Order
1. ✅ **Week 1 - Foundation:** Create orchestrator infrastructure without changing existing behavior
2. ✅ **Week 2 - Selection:** Integrate with content selection and add player behavior tracking
3. ✅ **Week 3 - Cutover:** Switch from old system to orchestrator
4. **Week 4 - Orders:** Coordinate order timing with orchestrator
4.5. ✅ **Native Effects:** Bridge JSON effects to native IncidentEffect system (tooltips, trait mapping)
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
├── IncidentEffectTranslator.cs       (Phase 4.5 - native effect bridge)
├── TraitMilestoneTracker.cs          (Phase 4.5 - trait level notifications)
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
├── EventSelector.cs                  (add fitness scoring)
└── EventDeliveryManager.cs           (Phase 4.5 - trait milestone check)

src/Features/Interface/Behaviors/
├── EnlistedNewsBehavior.cs           (add Company Report)
└── EnlistedMenuBehavior.cs           (update header layout)

src/Mod.Core/Config/
└── ConfigurationManager.cs           (Phase 4.5 - NativeTraitMappingConfig)

ModuleData/Enlisted/
└── enlisted_config.json              (add orchestrator config, native_trait_mapping)

ModuleData/Languages/
└── enlisted_strings.xml              (Phase 4.5 - trait milestone localization)
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

**✅ Update (2025-12-31):** Added mandatory order support:
- T1-T3 basic duties (guard, patrol, firewood, etc.) are now automatically assigned
- No player choice for mandatory orders - realistic soldier experience
- Optional orders (T4+) still require Accept/Decline
- Orders shown as `[ASSIGNED]` vs `[NEW]` in UI
- Player status displays: "On duty: Guard Duty." when order is active

---

### Migration Steps

**Phase 1: Add New Systems (Non-Breaking)**
1. Create `ContentOrchestrator.cs` with daily tick
2. Create `WorldStateAnalyzer.cs` to analyze lord situation
3. Create `SimulationPressureCalculator.cs` to track pressure
4. Create `PlayerBehaviorTracker.cs` to learn preferences
5. Add new config section `orchestrator` to `enlisted_config.json`
6. Integrate with `CompanySimulationBehavior` for roster/pressure data (see below)
7. Leave old systems in place initially

**Background Simulation Integration:**
The `WorldStateAnalyzer` reads from `CompanySimulationBehavior` ([Camp Background Simulation](camp-background-simulation.md)):
```csharp
// WorldStateAnalyzer.cs
var simulation = CompanySimulationBehavior.Instance;

// Company health context
worldState["high_casualties"] = simulation.Roster.CasualtyRate > 0.2f;
worldState["many_sick"] = simulation.Roster.SickCount > 5;
worldState["recent_desertions"] = simulation.Pressure.RecentDesertions > 0;

// Pressure context (affects event selection)
worldState["supply_pressure"] = simulation.Pressure.DaysLowSupplies > 2;
worldState["morale_pressure"] = simulation.Pressure.DaysLowMorale > 2;
worldState["company_stressed"] = worldState["supply_pressure"] || worldState["morale_pressure"];
```

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

### Military Day Cycle

The orchestrator is built around a **4-phase military day cycle** that mirrors authentic military life. This cycle syncs directly with the Order System.

```
┌────────────────────────────────────────────────────────────────────────────────┐
│                          MILITARY DAY CYCLE                                     │
├────────────────────────────────────────────────────────────────────────────────┤
│                                                                                 │
│  DAWN (6am-11am)         MIDDAY (12pm-5pm)    DUSK (6pm-9pm)     NIGHT (10pm+) │
│  ══════════════          ════════════════     ═══════════════    ═════════════ │
│                                                                                 │
│  Order Phase 1:          Order Phase 2:       Order Phase 3:     Order Phase 4:│
│  - Briefings             - Active duty        - Evening meal     - Night watch │
│  - Morning roll call     - Patrols            - Social time      - Sleep       │
│  - Work assignments      - Training drills    - Card games       - Quiet       │
│  - New orders arrive     - Guard posts        - Drinking         - Order ends  │
│                                                                                 │
│  Camp Activities:        Camp Activities:     Camp Activities:   Camp Limited: │
│  - Training              - Work details       - Gambling         - Night guard │
│  - Equipment check       - Trade/barter       - Storytelling     - Emergencies │
│  - Medical treatment     - Rest (midday)      - Rest (evening)   - Sleeping    │
│                                                                                 │
│  Progression:            Progression:         Progression:       Progression:  │
│  - Medical roll (6am)    - (none)             - Discipline (8pm) - (none)      │
│                                                                                 │
└────────────────────────────────────────────────────────────────────────────────┘
```

**The orchestrator uses DayPhase to:**
1. **Filter opportunities** - Training in morning, social in evening
2. **Time order events** - Briefings at 6am, completions at midnight
3. **Schedule progression rolls** - Medical at dawn, discipline at dusk
4. **Adjust content tone** - Morning is businesslike, evening is relaxed

```csharp
public enum DayPhase
{
    Dawn,     // 6am-11am: Briefings, training, work (Order Phase 1)
    Midday,   // 12pm-5pm: Active duty, patrols, drills (Order Phase 2)
    Dusk,     // 6pm-9pm: Social, meals, relaxation (Order Phase 3)
    Night     // 10pm-5am: Sleep, night watch (Order Phase 4)
}
```

---

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
void OnDailyTick()  // Main daily analysis (runs once at 6am)
void OnDayPhaseChanged(DayPhase newPhase)  // Phase transitions (4x daily)
WorldSituation AnalyzeCurrentSituation()
DayPhase GetCurrentDayPhase()  // Dawn, Midday, Dusk, Night
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
- **Tracks current day phase (Dawn, Midday, Dusk, Night)** - synced with Order System
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
    public DayPhase CurrentDayPhase { get; set; }  // Dawn, Midday, Dusk, Night
}
```

**Day Phase Detection:**
```csharp
public static DayPhase GetCurrentDayPhase()
{
    int hour = (int)CampaignTime.Now.CurrentHourInDay;
    return hour switch
    {
        >= 6 and < 12 => DayPhase.Dawn,      // 6am-11am
        >= 12 and < 18 => DayPhase.Midday,   // 12pm-5pm
        >= 18 and < 22 => DayPhase.Dusk,     // 6pm-9pm
        _ => DayPhase.Night                  // 10pm-5am
    };
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

**Status:** ✅ **COMPLETE**

**Tasks:**
1. ✅ Create `ContentOrchestrator.cs` class
2. ✅ Create `WorldStateAnalyzer.cs` class
3. ✅ Create `SimulationPressureCalculator.cs` class
4. ✅ Create `PlayerBehaviorTracker.cs` class
5. ✅ Create data models (`WorldSituation`, `SimulationPressure`, etc.)
6. ✅ Add comprehensive logging
7. ✅ Wire up daily tick (log only, don't affect live system)

**Deliverables:**
- ✅ Orchestrator receives daily ticks and logs decisions
- ✅ World state analysis works correctly
- ✅ Existing event system still works normally

**Acceptance Criteria:**
- ✅ Can see world situation analysis in logs
- ✅ Can see realistic frequency calculations
- ✅ Existing pacing system unaffected

---

### Phase 2: Content Selection Integration (Week 2)
**Goal:** Connect orchestrator to content selection

**Status:** ✅ **COMPLETE**

**Tasks:**
1. ✅ Integrate with `EventSelector.SelectEvent()` - Added WorldSituation parameter
2. ✅ Implement player behavior tracking - RecordChoice(), GetPreferences() working
3. ✅ Add preference-based content scoring - Fitness scoring in ApplyWeights()
4. ✅ Log what WOULD be selected - TestContentSelection() logs both systems
5. ✅ Compare with current system selections - Comparison logging implemented

**Deliverables:**
- ✅ Orchestrator selects content based on world state
- ✅ Logs show comparisons with old system
- ✅ Player preferences begin tracking

**Acceptance Criteria:**
- ✅ Selection makes sense for world situation
- ✅ High-fitness content is contextually appropriate
- ✅ Logging shows clear reasoning

**Files Modified:**
- `src/Features/Content/EventSelector.cs` - Added WorldSituation parameter, fitness scoring methods
- `src/Features/Content/ContentOrchestrator.cs` - Added TestContentSelection() comparison logging
- `src/Features/Content/WorldStateAnalyzer.cs` - Context mapping methods already present
- `src/Features/Content/PlayerBehaviorTracker.cs` - Tracking methods already present

---

### Phase 3: Cutover (Week 3)
**Goal:** Switch from old system to orchestrator

**Status:** ✅ **COMPLETE**

**Tasks:**
1. ✅ Add feature flag: `orchestrator.enabled` in enlisted_config.json
2. ✅ When enabled: orchestrator handles all narrative events (EventPacingManager returns early)
3. ✅ Disable `EventPacingManager` scheduled checks
4. ✅ Remove evaluation hours from `GlobalEventPacer`
5. ✅ Remove quiet day random roll (replaced with world-state-driven via `SetQuietDay()`)
6. ✅ Update config file

**Deliverables:**
- ✅ Orchestrator is live and delivering content when `orchestrator.enabled = true`
- ✅ Old schedule-driven logic disabled (no NextNarrativeEventWindow, no evaluation_hours)
- ✅ Config reflects new philosophy (frequency_tables by situation)

**Acceptance Criteria:**
- ✅ Content fires based on world state, not timers
- ✅ Garrison feels quiet, campaigns feel busy
- ✅ Safety limits still prevent spam (max_per_day, max_per_week, min_hours_between)

**Files Modified:**
- `src/Features/Content/GlobalEventPacer.cs` - Removed evaluation_hours checks, cleaned up dead code
- `src/Features/Content/EventPacingManager.cs` - Checks orchestrator.enabled and defers
- `src/Features/Content/MapIncidentManager.cs` - Updated to use simplified CanFireAutoEvent
- `src/Features/Content/ContentOrchestrator.cs` - Delivers content based on world state
- `ModuleData/Enlisted/enlisted_config.json` - Added orchestrator section with frequency_tables

---

### Phase 4: Orders Integration (Week 4)
**Goal:** Coordinate order system with orchestrator

**Status:** ✅ **COMPLETE** (2025-12-30)

**CRITICAL:** The Order Progression System **replaces** the current instant-resolution order mechanism. See [Order System Migration](ORDER-SYSTEM-MIGRATION.md) for what old code must be removed.

**Background:** Orders are now multi-day duty assignments (see [Order Progression System](order-progression-system.md)). The orchestrator needs to understand two separate concepts:
1. **Order Issuance** - When OrderManager assigns a new duty to the player
2. **Order Events** - Things that happen during duty execution (handled by OrderProgressionBehavior)

**Content Resources:**
- **16 orders** defined in [Orders Content](orders-content.md)
- **85 order events** cataloged in [Order Events Master](order-events-master.md)
- JSON schema for order events defined in [Event System Schemas](../Features/Content/event-system-schemas.md#order-event-schema)

**Tasks:**
1. ✅ Integrate `OrderManager` with orchestrator for **order issuance timing**
   - OrderManager checks with orchestrator before issuing new order
   - Orchestrator provides world state for order selection
   - Order issuance happens every 2-4 days (not competing with event budget)

2. ✅ Provide `WorldSituation` to `OrderProgressionBehavior` for **order event weighting**
   - Activity level modifies slot event chances during order execution
   - Quiet = ×0.3, Routine = ×0.6, Active = ×1.0, Intense = ×1.5
   - This is separate from narrative event frequency
   - Order events use `requirements.world_state` for context filtering

3. ✅ Load order events from JSON files
   - Files located at `ModuleData/Enlisted/Orders/order_events/*.json`
   - Each order has dedicated event pool file (e.g., `guard_events.json`)
   - Events use `world_state` requirements (e.g., `siege_attacking`, `war_marching`)

4. ✅ Define **non-order time** handling
   - When player has no active order, orchestrator can fire camp life events
   - These use the standard narrative event frequency tables
   - Player can also use Camp Hub decisions during this time

5. ⏳ Test full integration across order lifecycle (runtime testing required)

**Order Event File Structure:**
```json
{
  "schemaVersion": 2,
  "order_type": "order_guard_post",
  "events": [
    {
      "id": "guard_drunk_soldier",
      "order_type": "order_guard_post",
      "requirements": {
        "world_state": ["peacetime_garrison", "war_active_campaign"]
      },
      "options": [ ... ]
    }
  ]
}
```

**Deliverables:**
- ✅ OrderManager coordinates issuance timing with orchestrator
- ✅ OrderProgressionBehavior receives world state for event weighting
- ✅ Order event JSON files created for all 16 orders
- ✅ All event text uses placeholder variables for culture-awareness (`{SERGEANT}`, `{LORD_NAME}`, etc.)
- ✅ Camp life events fire appropriately between orders
- ✅ All systems work together smoothly

**Acceptance Criteria:**
- ✅ New orders issued every 2-4 days when player has none active
- ✅ Order slot events weighted by world state (siege = more events during duty)
- ✅ Order events filter by `world_state` requirement
- ✅ Event text displays with culture-specific NCO/officer titles (Empire: "Optio", Vlandia: "Sergeant", etc.)
- ✅ Camp life events fire during non-order time
- ✅ No overwhelming spam from combined systems

**Files Modified:**
- `src/Features/Content/EventDefinition.cs` - Added WorldState property to EventRequirements
- `src/Features/Content/EventCatalog.cs` - Added world_state JSON parsing in ParseRequirements
- `src/Features/Orders/Behaviors/OrderProgressionBehavior.cs` - Fixed FilterByWorldState to use WorldState property
- `src/Features/Orders/Behaviors/OrderManager.cs` - Integrates with ContentOrchestrator.CanIssueOrderNow()
- `ModuleData/Enlisted/Orders/order_events/*.json` - 16 order event files with world_state requirements

---

### Phase 4.5: Native Effect Integration
**Goal:** Bridge JSON content definitions with Bannerlord's native IncidentEffect system

**Status:** ✅ **COMPLETE** (2025-12-30)

**Tasks:**
1. ✅ Create `IncidentEffectTranslator.cs` - Bridges JSON EventEffects to native IncidentEffect objects
2. ✅ Create `TraitMilestoneTracker.cs` - Tracks when native traits cross level thresholds
3. ✅ Add `NativeTraitMappingConfig` to ConfigurationManager
4. ✅ Integrate trait milestone check into EventDeliveryManager
5. ✅ Add localization keys for trait milestone messages

**Native Effects Integrated:**
- GoldChange, SkillChange, TraitChange, MoraleChange, HealthChance
- RenownChange, WoundTroopsRandomly
- Automatic tooltip generation via native GetHint()

**Trait Mapping:**
| Enlisted Rep | Native Trait | Rationale |
|--------------|--------------|-----------|
| Soldier Rep  | Valor        | Bravery, fighting spirit |
| Officer Rep  | Calculating  | Tactical thinking, leadership |
| Lord Rep     | Honor        | Duty, keeping word |

**Configuration:** `native_trait_mapping` section in enlisted_config.json
- `enabled`: Toggle trait mapping on/off
- `scale_divisor`: Division factor (default 5, tuned for ~100 day careers)
- `minimum_change`: Threshold to prevent spam (default 1)

**Key Files:**
- `src/Features/Content/IncidentEffectTranslator.cs` (NEW)
- `src/Features/Content/TraitMilestoneTracker.cs` (NEW)
- `src/Mod.Core/Config/ConfigurationManager.cs` (MODIFIED)
- `src/Features/Content/EventDeliveryManager.cs` (MODIFIED)
- `ModuleData/Languages/enlisted_strings.xml` (MODIFIED)

---

### Phase 5: Refinement & UI Integration (Week 5)
**Goal:** Polish orchestrator and integrate the Quick Decision Center UI

**Status:** ✅ **COMPLETE** (2025-12-30)

**Tasks:**
1. ✅ **Main Menu UI** (Quick Decision Center)
   - Restructure Main Menu (`enlisted_status`) with KINGDOM, CAMP, YOU sections
   - Add `BuildForecastSection()` to generate NOW + AHEAD text
   - Add `GenerateCampSummary()` for camp activity one-liner
   - Implement culture-aware text resolution for rank names

2. ✅ **Navigation Structure**
   - Main Menu shows info + three buttons: ORDERS, DECISIONS, CAMP
   - DECISIONS opens camp life opportunities (dynamically generated)
   - ORDERS opens military order view
   - CAMP opens deep Camp Hub

3. ✅ **Camp Hub Restructure** (`enlisted_camp_hub`)
   - ADD CAMP STATUS section at top (rhythm, activity level, camp narrative)
   - ADD RECENT ACTIONS section (event/order outcomes)
   - REMOVE Reports menu option (camp status replaces it)
   - REMOVE Leave Service option (only accessible from Muster menu)
   - DELETE RegisterReportsMenu() function or gut it
   - KEEP: Service Records, Quartermaster, Retinue, Companions, Medical, Lords, Baggage

4. ⏳ **Playtest & Tune** (runtime testing required)
   - Playtest at each tier (T1-T9)
   - Tune frequency tables
   - Tune forecast signal timing
   - Verify culture-aware text works for all cultures

**UI Structure:**

**Main Menu (`enlisted_status`) - Quick Decision Center:**
```
╔══════════════════════════════════════════════════════════╗
║  _____ KINGDOM _____                                      ║
║  {War/peace. Major events. 1-2 lines.}                   ║
║                                                           ║
║  _____ CAMP _____                                         ║
║  {What's happening right now. Living world. 1-2 lines.}  ║
║                                                           ║
║  _____ YOU _____                                          ║
║  NOW: {Current duty status. Physical state.}             ║
║  AHEAD: {Forecast of what's coming. Culture-aware.}      ║
╠══════════════════════════════════════════════════════════╣
║            [  ORDERS  ]     ← Military orders             ║
║            [  DECISIONS  ]  ← Camp life activities        ║
║            [  CAMP    ]     ← Deep menu (QM, records)     ║
║            [Back to Map]                                  ║
╚══════════════════════════════════════════════════════════╝
```

**Three Information Sections (cached, stable):**

| Section | Purpose | Refreshes When |
|---------|---------|----------------|
| KINGDOM | Macro strategic context | War/peace, siege events, or every 24h |
| CAMP | Living world + forecasts | Time period changes, or every 6h |
| YOU | NOW + AHEAD personal state | Player state changes |

**Don't regenerate on every menu open.** Cache and refresh on triggers.
Use `[NEW]` tag with Warning color for changed content.

**The YOU Section - NOW + AHEAD:**

NOW shows current state:
- Duty status (on order, off duty)
- Physical state (rested, tired, wounded)

AHEAD shows forecast (context-aware):
- When off duty: Hints about incoming orders, camp events
- When on order: What's coming DURING that order
- Uses culture-appropriate rank names via `{NCO_TITLE}`, `{OFFICER_TITLE}`

**Forecast Signals:**

| Signal | What It Foreshadows | Data Source | Priority |
|--------|---------------------|-------------|----------|
| **Party State Warnings (from Background Simulation)** ||||
| "The men are hungry. Supplies won't last." | Supply crisis imminent | `CompanySimulationBehavior` | 🔴 Critical |
| "The mood is dark. Something may break." | Morale crisis imminent | `CompanySimulationBehavior` | 🔴 Critical |
| "Fever spreading through camp." | Many sick, need medical | `CompanySimulationBehavior` | 🟠 High |
| "Many wounded need care." | High casualties | `CompanySimulationBehavior` | 🟠 High |
| "Men have been slipping away." | Recent desertions | `CompanySimulationBehavior` | 🟠 High |
| "Rations are getting thin." | Supplies low | `CompanyNeedsManager` | 🟠 High |
| "Grumbling in the ranks." | Morale low | `CompanyNeedsManager` | 🟡 Medium |
| "Officers are losing patience." | Discipline low | `EscalationManager` | 🟡 Medium |
| **Order/Event Forecasts** ||||
| "{NCO_TITLE}'s been making lists." | Order in 12-24 hours | `OrderManager` | 🟡 Medium |
| "Pay day approaches." | Muster in 2-3 days | `EnlistmentBehavior` | 🟡 Medium |
| "The men are planning something." | Social event tomorrow | `CampOpportunityGenerator` | 🟢 Low |
| "Quiet. Almost too quiet." | Nothing imminent | Default | 🟢 Low |

**Priority determines display order.** Critical/High warnings always shown first. Player sees the most urgent 1-2 forecasts.

**Technical Implementation:**

1. **New ForecastGenerator.cs:**
```csharp
public class ForecastGenerator
{
    public (string Now, string Ahead) BuildPlayerStatus()
    {
        var now = BuildNowText(); // Duty status, physical state
        var ahead = BuildAheadText(); // Context-aware forecast
        return (now, ahead);
    }

    private string BuildAheadText()
    {
        var forecasts = new List<ForecastItem>();

        // === PARTY STATE WARNINGS (from Background Simulation) ===
        var sim = CompanySimulationBehavior.Instance;

        // Supply warnings - escalating urgency
        if (sim.Pressure.DaysLowSupplies >= 2)
            forecasts.Add(("The men are hungry. Supplies won't last.", Priority.Critical));
        else if (_needs.GetNeed(CompanyNeed.Supplies) < 40)
            forecasts.Add(("Rations are getting thin.", Priority.High));

        // Morale warnings
        if (sim.Pressure.DaysLowMorale >= 2)
            forecasts.Add(("The mood is dark. Something may break.", Priority.Critical));
        else if (_needs.GetNeed(CompanyNeed.Morale) < 40)
            forecasts.Add(("Grumbling in the ranks.", Priority.Medium));

        // Health warnings
        if (sim.Roster.SickCount > 5)
            forecasts.Add(("Fever spreading through camp.", Priority.High));
        if (sim.Roster.WoundedCount > sim.Roster.TotalSoldiers * 0.2f)
            forecasts.Add(("Many wounded need care.", Priority.High));

        // Discipline warnings
        if (_escalation.GetTrack(EscalationTrack.Discipline) < 30)
            forecasts.Add(("Officers are losing patience.", Priority.Medium));

        // Desertion warnings
        if (sim.Pressure.RecentDesertions > 0)
            forecasts.Add(("Men have been slipping away.", Priority.High));

        // === ORDER/EVENT FORECASTS ===

        // Check for incoming orders
        if (OrderManager.Instance?.IsOrderPending(12.Hours()))
            forecasts.Add(("{NCO_TITLE}'s been making lists.", Priority.Medium));

        // Check for upcoming muster
        if (EnlistmentBehavior.Instance?.DaysUntilMuster <= 3)
            forecasts.Add(("Pay day approaches.", Priority.Medium));

        // Check for upcoming camp events
        var upcoming = CampOpportunityGenerator.GetUpcomingOpportunities();
        if (upcoming.Any())
            forecasts.Add(("The men are planning something.", Priority.Low));

        // Default if nothing else
        if (forecasts.Count == 0)
            forecasts.Add(("Quiet. Almost too quiet.", Priority.Low));

        // Sort by priority, take top 2
        var topForecasts = forecasts
            .OrderByDescending(f => f.Priority)
            .Take(2)
            .Select(f => ResolveCultureText(f.Text));

        return string.Join(" ", topForecasts);
    }
}
```

2. **New Method in EnlistedNewsBehavior.cs:**
```csharp
public string BuildCampStatusSection()
{
    if (ContentOrchestrator.Instance == null)
        return string.Empty;

    var rhythmIcon = GetRhythmIcon();
    var rhythmName = GetRhythmName();
    var activityLevel = GetActivityLevelName();
    var flavorText = ContentOrchestrator.Instance.GetOrchestratorRhythmFlavor();

    // Used in Camp Hub header (not Reports menu - that's gone)
    return $"<span style=\"Header\">_____ CAMP STATUS _____</span>\n" +
           $"{rhythmIcon} {rhythmName} - {activityLevel}\n\n{flavorText}";
}
```

3. **Update EnlistedMenuBehavior.cs Camp Hub (`enlisted_camp_hub`):**
   - ADD `BuildCampStatusSection()` at top of Camp Hub text
   - ADD `BuildRecentActionsSection()` after camp status
   - REMOVE RegisterReportsMenu() - Reports menu no longer exists
   - REMOVE Leave Service option - only accessible from Muster
   - Keep remaining Camp Hub options (QM, Records, Companions, etc.)

4. **Update Main Menu (`enlisted_status`):**
   - ADD KINGDOM section (kingdom news summary)
   - ADD CAMP section (camp activity + forecast hints)
   - ADD YOU section (NOW + AHEAD)
   - Kingdom news stays on Main Menu, not Camp Hub

5. **Order State Flow (FORECAST → SCHEDULED → PENDING → ACTIVE):**
   - FORECAST: Signal appears in CAMP/YOU sections 12-24h before
   - SCHEDULED: Appears in ORDERS menu (grayed) 8-18h before
   - PENDING: Appears with [NEW] tag, player accepts/declines
   - ACTIVE: Shows progress (Day X/Y), current slot, next slot
   - COMPLETE: Brief summary with rewards, auto-clears

6. **Info Section Caching:**
```csharp
public class MainMenuNewsCache
{
    // Cached text, only regenerate on triggers
    private string _kingdomText;
    private string _campText;
    private string _youText;

    public void RefreshIfNeeded()
    {
        // KINGDOM: 24h or major event
        // CAMP: time period change or 6h
        // YOU: state change
    }
}
```

7. **DECISIONS Presentation (no SAFE/RISKY labels):**
   - Orchestrator curates what appears based on player state + order status
   - "blocked" options filtered out (don't appear)
   - "risky" options appear normally, TOOLTIP shows risk/consequences
   - No visual categories in UI - just natural opportunities

**Deliverables:**
- Polished, player-tested orchestrator
- Main Menu with KINGDOM, CAMP, YOU sections
- Camp Hub with inline CAMP STATUS (replaces Reports menu)
- Leave Service only from Muster menu
- Admin tools for debugging

**Acceptance Criteria:**
- Event frequency feels right for all situations
- Content always makes sense
- Silence feels appropriate, not empty
- **Player understands camp situation** (via CAMP STATUS in Camp Hub)
- Kingdom news on Main Menu, camp-specific status in Camp Hub
- Context filtering works (variants selected appropriately)

---

### Phase 5.5: Camp Background Simulation
**Goal:** Create autonomous company simulation that provides context data for the orchestrator

**Status:** ✅ **COMPLETE** (2025-12-30)

**Tasks:**
1. ✅ Create `CompanySimulationBehavior.cs` - Autonomous company state simulation
2. ✅ Create `CompanyRosterState.cs` - Tracks sick/wounded/desertions
3. ✅ Create `SimulationPressure.cs` - Pressure tracking for forecasts
4. ✅ Integrate with `ForecastGenerator` for party state warnings

**Key Features:**
- Autonomous sick/wounded tracking (not deterministic from events)
- Pressure accumulation for crisis warnings (days low supplies, days low morale)
- Desertion tracking for forecast signals
- Provides data for "living world" feeling without player input

**Files Created:**
- `src/Features/Company/CompanySimulationBehavior.cs`
- `src/Features/Company/Models/CompanyRosterState.cs`
- `src/Features/Company/Models/SimulationPressure.cs`

**See Also:** [Camp Background Simulation](camp-background-simulation.md)

---

### Phase 6: Camp Life Simulation (Living Breathing World)
**Goal:** Create a living camp that generates contextual opportunities independently of player input

**Status:** ✅ **Phase 6 COMPLETE** (2025-12-30, sea awareness added 2025-12-31)

**Sub-Phases:**
- **6A (Foundation):** Core models, generator, basic fitness scoring ✅
- **6B (UI Integration):** DECISIONS menu, opportunity engagement tracking ✅
- **6C (Intelligence):** Player state modifiers, budget system, cooldowns ✅
- **6D (Learning):** Player behavior adaptation, 70/30 split ✅
- **6E (Polish):** 29 opportunities, edge cases, natural language ✅
- **6F (Sea Awareness):** Location-based filtering (atSea/notAtSea), 4 sea variants ✅

**Key Concepts:**

**4 Intelligence Layers for Fitness Scoring:**
1. **World State (Macro):** Lord situation, war status, strategic context
2. **Camp Context (Meso):** Day phase (synced with orders), camp mood, weekly rhythm
3. **Player State (Micro):** Fatigue, gold, injury, recent actions
4. **History (Meta):** Recent presentations, engagement rates, variety maintenance

**Order-Decision Tension:**
- Opportunities have `orderCompatibility` per order type
- `available` = safe, `risky` = detection check, `blocked` = filtered out
- Risky opportunities show consequences in tooltip
- Detection rolls use `detection.baseChance` + modifiers

**Opportunity Budget:**
- Context determines how many opportunities (0-3)
- Garrison morning: 2-3, Siege: 0-1, Campaign evening: 1-2
- Reduced by probation, low supplies, on-duty status

**Files Created:**
- `src/Features/Camp/CampOpportunityGenerator.cs` (876 lines, +sea filtering)
- `src/Features/Camp/Models/CampOpportunity.cs` (+NotAtSea, +AtSea properties)
- `src/Features/Camp/Models/CampContext.cs`
- `src/Features/Camp/Models/CampMood.cs`
- `src/Features/Camp/Models/OpportunityType.cs`
- `src/Features/Camp/Models/OpportunityHistory.cs`
- `src/Features/Camp/Models/PlayerCommitments.cs`
- `src/Features/Camp/Models/DetectionSettings.cs`
- `src/Features/Camp/Models/CaughtConsequences.cs`
- `src/Features/Interface/MainMenuNewsCache.cs`
- `src/Features/Interface/ForecastGenerator.cs`
- `src/Features/Content/EventDefinition.cs` (+AtSea property)
- `src/Features/Content/EventRequirementChecker.cs` (+MeetsAtSeaRequirement)
- `ModuleData/Enlisted/camp_opportunities.json` (29 land + 4 sea = 33 opportunities)

**See Also:** [Camp Life Simulation](camp-life-simulation.md), [Event System Schemas - Camp Opportunities](../Features/Content/event-system-schemas.md#camp-opportunities-schema-phase-6)

---

### Phase 7: Content Variants (Post-Launch, Incremental)
**Goal:** Add contextual variety to high-traffic content

**Status:** 🟡 **PARTIALLY IMPLEMENTED** (Sea/land variants added 2025-12-31)

**When:** After orchestrator and camp life are proven (Week 7+)

**Approach:**
1. Identify repetitive events from playtesting
2. Create variants with different `requirements.context`
3. Add as JSON files (no code changes)
4. Test variant selection in different world states

**Priority Order:**
1. ✅ **Sea/land variants** (DONE - first implementation)
2. Training decisions (high-traffic, easy to vary)
3. Rest/camp decisions (frequent use)
4. Common events (seen repeatedly)
5. Role-specific variants (add depth)

**Implemented Sea/Land Variants:**
```json
// Land-only opportunities (notAtSea: true)
opp_rest_tent, opp_tavern_visit, opp_foraging, opp_repair_work,
opp_campfire_song, opp_dice_game, opp_archery_range

// Sea-only opportunities (atSea: true)
opp_rest_hammock          → "Your hammock sways with the ship's motion..."
opp_below_deck_drinking   → "The crew is passing around bottles below deck..."
opp_ship_maintenance      → "Rope splicing, caulking, sail mending..."
opp_sea_shanty            → "Sailors on deck are singing work songs..."
```

**Example Future Variants:**
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

**Implementation Pattern:**
- `CampOpportunity` model: Added `NotAtSea` and `AtSea` bool properties
- `CampOpportunityGenerator`: Checks `MobileParty.IsCurrentlyAtSea` for filtering
- `EventDefinition` model: Added `AtSea` bool property for events
- `EventRequirementChecker`: Added `MeetsAtSeaRequirement()` method
- JSON schema: `"atSea": true` or `"notAtSea": true` in requirements
- Variants compete naturally in selection pool based on party location

---

### Phase 8: Progression System (Future)
**Goal:** Generic probabilistic progression pattern for escalation tracks

**Status:** 📋 Schema Ready (Implementation Deferred)

**Concept:**
Instead of deterministic "choice = fixed outcome", progression tracks can:
- **Improve** naturally over time
- **Stay stable** (no change)
- **Worsen** (complications, decay)

Daily probability checks determine outcomes, modified by skills, context, and player actions.

**Applicable Tracks:**
- Medical Risk (daily @ 6am, modified by Medicine skill)
- Discipline (daily @ 8pm, modified by Leadership)
- Pay Tension (daily, modified by Trade)

**See:** [Event System Schemas - Progression System](../Features/Content/event-system-schemas.md#progression-system-schema-future-foundation)

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
- Phase 5: Add `BuildCampStatusSection()` for Camp Hub
- Phase 5: Add `BuildKingdomSummary()`, `BuildCampSummary()` for Main Menu
- Phase 5: Remove Reports menu, remove Leave Service from Camp Hub
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

        private DayPhase _lastPhase = DayPhase.Night;

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, CheckPhaseTransition);
        }

        /// <summary>
        /// Checks if day phase has changed and fires OnDayPhaseChanged if so.
        /// Runs hourly to detect phase boundaries.
        /// </summary>
        private void CheckPhaseTransition()
        {
            var currentPhase = GetCurrentDayPhase();
            if (currentPhase != _lastPhase)
            {
                _lastPhase = currentPhase;
                OnDayPhaseChanged(currentPhase);
            }
        }

        /// <summary>
        /// Fires when military day phase changes (4x per day).
        /// Syncs Orders, Camp Life, and Progression systems.
        /// </summary>
        private void OnDayPhaseChanged(DayPhase newPhase)
        {
            ModLogger.Info("Orchestrator", $"Day phase changed to {newPhase}");

            // Order system handles phase
            OrderProgressionBehavior.Instance?.OnPhaseChanged(newPhase);

            // Camp life regenerates opportunities
            CampLifeManager.Instance?.OnPhaseChanged(newPhase);

            // Progression ticks at specific phases
            if (newPhase == DayPhase.Dawn)
                MedicalProgressionBehavior.Instance?.Tick();
            if (newPhase == DayPhase.Dusk)
                DisciplineProgressionBehavior.Instance?.Tick();

            // Refresh UI caches
            MainMenuNewsCache.Instance?.OnPhaseChanged(newPhase);
        }

        /// <summary>
        /// Main daily analysis. Runs once at 6am.
        /// Analyzes world state and updates expectations.
        /// </summary>
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

        /// <summary>
        /// Maps internal world state to event system context string for filtering.
        /// Used by EventRequirementChecker to filter eligible narrative events.
        /// Returns simplified context values: "Camp", "War", "Siege", "Any".
        /// </summary>
        public static string GetEventContext(WorldSituation situation)
        {
            return situation.LordSituation switch
            {
                LordSituation.InGarrison => "Camp",
                LordSituation.InSiege => "Siege",
                LordSituation.InCampaign => "War",
                LordSituation.Defeated => "Camp",  // Recovery counts as garrison
                _ => "Any"
            };
        }

        /// <summary>
        /// Returns granular world state key for order event weighting.
        /// Order events use detailed world_state requirements (war_marching, siege_attacking, etc.)
        /// for context-specific event selection during duty execution.
        /// </summary>
        public static string GetOrderEventWorldState(WorldSituation situation)
        {
            // Map situation to config frequency key
            return (situation.LordSituation, situation.WarStance) switch
            {
                (LordSituation.InGarrison, WarStance.Peacetime) => "peacetime_garrison",
                (LordSituation.InCampaign, WarStance.Peacetime) => "peacetime_recruiting",
                (LordSituation.InCampaign, WarStance.ActiveWar) => "war_active_campaign",
                (LordSituation.InSiege, WarStance.ActiveWar) => "siege_attacking",
                (LordSituation.InSiege, WarStance.DesperateWar) => "siege_defending",
                (LordSituation.Defeated, _) => "lord_captured",
                _ => "peacetime_garrison"  // Default to safest/quietest
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

    // Military Day Cycle - synced with Order Phases
    public DayPhase CurrentDayPhase { get; set; }  // Dawn, Midday, Dusk, Night
    public int CurrentHour { get; set; }  // 0-23 for precise timing

    // Context details for flavor text
    public Settlement CurrentSettlement { get; set; }  // If garrisoned
    public Settlement TargetSettlement { get; set; }  // If marching/sieging
    public int DaysInCurrentPhase { get; set; }  // How long in this phase
    public bool InEnemyTerritory { get; set; }  // Affects pressure
}

/// <summary>
/// Military day cycle - maps directly to Order System's 4 phases.
/// Content is filtered by DayPhase for appropriate timing.
/// </summary>
public enum DayPhase
{
    Dawn,     // 6am-11am: Briefings, training, work details (Order Phase 1)
    Midday,   // 12pm-5pm: Active duty, patrols, main work (Order Phase 2)
    Dusk,     // 6pm-9pm: Evening meal, social time, relaxation (Order Phase 3)
    Night     // 10pm-5am: Sleep, night watch, quiet (Order Phase 4)
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

## Edge Cases & Special States

The orchestrator must handle these special states that affect content delivery:

### Enlistment State Edge Cases

| State | Orchestrator Behavior |
|-------|----------------------|
| **New enlistment (3-day grace)** | Suppress all narrative events and map incidents. Allow Camp Hub decisions. Return early from `ShouldFireContent()`. |
| **Probation active** | Content still fires but with probation-aware selection. Economic stress events more likely. |
| **Pending discharge** | Reduce content frequency to 25%. Focus on closure/farewell content. |
| **Grace period (lord died)** | Suspend orchestrator entirely. Only critical system events (discharge prompts). Resume on re-enlistment. |

### Combat & Movement Edge Cases

| State | Orchestrator Behavior |
|-------|----------------------|
| **Active battle** | `LordSituation = InBattle`. No content fires. Queue any pending for post-battle. |
| **Army marching** | Check `MobileParty.IsMoving`. Reduce frequency. Context = Campaign. |
| **Player captured** | Suspend orchestrator. No content until release. Clear any pending queue. |
| **Siege (attacking)** | `LordSituation = InSiege`. High-intensity context. Siege-specific content only. |
| **Siege (defending)** | Same as attacking but with desperate tone. Higher pressure scores. |

### Muster & Pay Edge Cases

| State | Orchestrator Behavior |
|-------|----------------------|
| **Muster day** | Orchestrator defers to muster sequence. `MusterMenuHandler` takes priority. No narrative events. |
| **Pay tension high (>60)** | Increase economic event weight. PayTension pressure source active. |
| **Backpay owed** | Similar to high pay tension but longer-term pressure. |

### Supply & Resource Edge Cases

| State | Orchestrator Behavior |
|-------|----------------------|
| **Supply < 30%** | Add supply-related pressure. Increase foraging/supply event weight. |
| **Supply < 20% (critical)** | Force supply crisis context. Block non-survival content. |
| **Supply > 80%** | Reduce supply pressure to 0. Normal content distribution. |

### Integration Edge Cases

| State | Orchestrator Behavior |
|-------|----------------------|
| **Order active** | Check `OrderManager.HasActiveOrder`. Reduce narrative events during duty. Allow duty-specific content. |
| **Multiple content types eligible** | Priority: Muster > Orders > Escalation thresholds > Narrative events. Never fire competing content. |
| **Save mid-content** | Complete current content before save. Don't save partial state. |
| **Load into special state** | Detect state on load. Apply appropriate frequency table. Don't fire content on first tick. |

### UI Caching Edge Cases

| State | Orchestrator Behavior |
|-------|----------------------|
| **Fast travel > 2 hours** | Force refresh all info sections on next menu open. |
| **Save/Load** | Don't persist cache. Rebuild on load from current state. |
| **Multiple triggers fire** | Batch into single refresh. Don't refresh per-trigger. |
| **Time speed x4** | No impact - forecasts use game hours, not real seconds. |

### Order Flow Edge Cases

| State | Orchestrator Behavior |
|-------|----------------------|
| **Fast travel past SCHEDULED** | Auto-accept order. Notify: "You were assigned {ORDER} while traveling." |
| **Fast travel past PENDING** | Auto-decline with -5 rep penalty. Notify: "You missed an order assignment." |
| **Player ignores PENDING 24h** | Auto-decline after 24h. Warning at 18h: "Respond soon or miss the order." |
| **Order cancelled while SCHEDULED** | Remove from menu. CAMP news: "[NEW] The roster changed. {ORDER} cancelled." |
| **Forecast wrong (didn't happen)** | Use soft language in forecasts ("likely", "seems"). Never guarantee. |

### Order-Decision Tension Edge Cases

| State | Orchestrator Behavior |
|-------|----------------------|
| **Detection timing** | Check BEFORE activity starts. Player sees consequence immediately. |
| **Order phase changes mid-activity** | Complete activity. Apply consequences at END if caught. |
| **Caught rep would go negative** | Floor at 0. Show "You're on thin ice with command." |
| **3+ catches during single order** | Then order failure risk applies (20% per catch after third). |

### Fallback Behavior

All edge cases should fail safely:
- Unknown state → Default to "Camp" context with standard frequency
- Null managers → Log warning, disable affected pressure source, continue
- Config load failure → Use hardcoded defaults
- Content selection fails → Return empty, log error, try again next tick
- Info section generation fails → Show fallback text, log error, don't break menu
- Forecast text missing → Show "The day stretches ahead. Time will tell."

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

## Critical Addition: Phases 9-10 - Scheduling & Forecasting

**Status:** ❌ Not Implemented - **MUST HAVE**  
**Priority:** Critical for player experience at fast time speeds  
**Implementation:** See [content-orchestrator-prompts.md](content-orchestrator-prompts.md#phase-9-decision-scheduling-must-have)

**Problem:**
From decompile research (Campaign.cs):
- **Play (1x):** 1 game day = 80 real seconds (1 min 20 sec)
- **FastForward (>>):** 1 game day = 20 real seconds
- At FastForward, orders/events appear with <10 seconds warning
- Player gets bombarded with immediate popups, no time to plan

**Phase 9: Decision Scheduling**
Players commit to camp decisions that fire at scheduled phases:
- Tag opportunities with Dawn/Midday/Dusk/Night
- Click decision → greys out, shows "Scheduled for Midday"
- Event fires automatically at phase boundary
- Player status: "You've committed to sparring at noon"
- Prevents popup spam at fast speeds

**Phase 10: Order Imminent Warnings**
Short-term (4-8 hour) warnings before orders issue:
- Orchestrator decides "order should fire"
- Instead of immediate issue, creates IMMINENT state with 4-8h delay
- Warning appears in summaries: "Sergeant will call for you soon"
- Order issues when delay expires
- Simpler than long-term forecasting (world state changes too fast)

**Integration:**
- Decision commitments and order warnings both appear in summaries
- Max 4-5 lines per section (Kingdom Reports, Company Reports, Your Status)
- Commitments take priority (player's choice)
- Orders can override if critical (siege defense)

**Why It Matters:**
At FastForward speed, 4 hours = 3.3 real seconds, 8 hours = 6.6 real seconds. Even this short warning is **essential** - without it, orders appear instantly with zero reaction time.

---

## Future Expansion: Progression System

**Status:** Schema Ready (Deferred Implementation)
**Schema:** [Progression System Schema](../Features/Content/event-system-schemas.md#progression-system-schema-future-foundation)

After the orchestrator is complete, integrate the **Progression System** for organic escalation track evolution.

### What It Does

Instead of events directly setting escalation values, tracks like Medical Risk, Discipline, and Pay Tension can:
- **Improve** naturally over time (recovery, good behavior)
- **Stay stable** (no change)
- **Worsen** (complications, decay)

Daily probability checks determine outcomes, modified by skills, context, and player actions.

### Orchestrator Integration Points

The orchestrator provides modifiers to progression behaviors:

```csharp
// ContentOrchestrator implements this interface
public interface IProgressionModifierProvider
{
    ProgressionModifiers GetProgressionModifiers(string trackName);
}

// Progression behavior asks for world state modifiers
var modifiers = ContentOrchestrator.Instance.GetProgressionModifiers("medical_risk");
// Returns: { ImproveBonus: 5, WorsenBonus: 0, TickMultiplier: 0.8 }
```

### World State Affects Progression

| World Situation | Medical | Discipline | Pay Tension |
|-----------------|---------|------------|-------------|
| Garrison | +10 improve | +5 improve | No effect |
| Campaign | No effect | No effect | No effect |
| Siege | +15 worsen | -5 improve | +10 worsen |
| After Battle | +15 worsen | +10 improve | No effect |

### Implementation Order

When ready to implement progression:

1. Add `IProgressionModifierProvider` interface to `ContentOrchestrator`
2. Create `ProgressionBehavior` base class
3. Create `MedicalProgressionBehavior` (first implementation)
4. Add `progression_config.json`
5. Wire threshold events through `EventDeliveryManager`
6. Extend to Discipline, Pay Tension, etc.

### Prepared Schema

The schema is already defined in [event-system-schemas.md](../Features/Content/event-system-schemas.md#progression-system-schema-future-foundation), including:
- `progressionConfig` field structure
- Probability tables by severity level
- Skill and context modifiers
- Critical roll ranges
- Clamps and limits
- Localization key patterns

No additional schema work needed - just implement when ready.

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

