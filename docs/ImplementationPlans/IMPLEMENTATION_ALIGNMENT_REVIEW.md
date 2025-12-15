# Implementation Plans - Alignment Review

**Date:** December 14, 2025  
**Purpose:** Comprehensive review of all implementation plans to ensure they align and complement each other  
**Reviewer:** AI Analysis  
**Status:** ✅ ALIGNED with recommendations

---

## Executive Summary

**Overall Assessment:** ✅ **STRONG ALIGNMENT** - All implementation plans are well-integrated and complementary.

**Key Strengths:**
- Clear system boundaries and integration points
- Consistent terminology and concepts across docs
- Well-defined dependencies and build order
- No major conflicts or contradictions found

**Areas Requiring Attention:**
- Some overlapping responsibilities need clarification
- Implementation order dependencies need stricter enforcement
- A few minor terminology inconsistencies

---

## Implementation Plans Inventory

| Plan | Purpose | Duration | Status | Dependencies |
|------|---------|----------|--------|--------------|
| **1. camp_menu_overhaul.md** | Menu restructuring | Completed | ✅ Done | None |
| **2. camp_schedule_system_analysis.md** | Time-of-day expansion (4→6 periods) | 1-2 hours | 📋 Ready | None |
| **3. GAUNTLET_CUSTOM_SCREENS_GUIDE.md** | Technical best practices | Reference | 📖 Guide | None |
| **4. CAMP_ACTIVITIES_MENU_IMPLEMENTATION.md** | Camp Hub with 6 locations | 6 weeks total (Phase 1 done) | 🚧 Phase 2 | Time system |
| **5. DUTY_EVENTS_CREATION_GUIDE.md** | Recurring duty events (150-200 events) | Ongoing | 📋 Content | Duties system, time system |
| **6. lance-life-simulation.md** | Player's lance member simulation | 12 weeks | 📋 Design | AI Camp Schedule |
| **7. LANCE_LIFE_SIMULATION_INTEGRATION_GUIDE.md** | Integration spec between systems | N/A | 📖 Spec | Lance Life + AI Schedule |
| **8. PERSISTENT_LANCE_LEADERS.md** | Unique lance leaders with memory | 10 weeks | 📋 Design | Lance Life, Duty Events |
| **9. ai-camp-schedule.md** | AI daily duty scheduling | 8+ weeks | 📋 Design | Time system |
| **10. ai-lord-lance-simulation.md** | AI lord army lance simulation | 10 weeks | 📋 Design | None (parallel) |
| **11. ARMY_LANCE_ACTIVITY_SIMULATION.md** | NPC lance activity simulation | 7 weeks | 📋 Design | AI Schedule, Lance Life, News System |

---

## Critical Dependencies & Build Order

### Tier 1: Foundation (Must Build First)
```
✅ camp_menu_overhaul.md          (DONE)
    ↓
⚡ camp_schedule_system_analysis   (1-2 hours - DO NEXT)
    ↓
    ├─→ Enables time-aware events for all systems
    └─→ Required by: AI Camp Schedule, Duty Events, Camp Hub
```

### Tier 2: Core Systems (Build in Parallel)
```
🔷 CAMP_ACTIVITIES_MENU (Phase 2)  ←─┐
   └─→ Camp Hub navigation            │ Can build
                                       │ simultaneously
🔷 ai-camp-schedule.md                 │ (different
   └─→ AI duty scheduling           ←─┘ features)
```

### Tier 3: Lance Systems (Depend on Tier 2)
```
AI Camp Schedule (from Tier 2)
    ↓
🔶 lance-life-simulation.md
   └─→ Player's lance member simulation
       ↓
       ├─→ 🔶 PERSISTENT_LANCE_LEADERS.md
       │   └─→ Unique lance leaders with memory
       │
       └─→ 📦 LANCE_LIFE_SIMULATION_INTEGRATION_GUIDE.md
           └─→ Integration spec (reference during build)
```

### Tier 4: Content Layer (Build After Core)
```
Lance Life Simulation (from Tier 3)
    +
Duties System (existing)
    ↓
📝 DUTY_EVENTS_CREATION_GUIDE.md
   └─→ Create 150-200 duty events
```

### Tier 5: Advanced Features (Optional/Later)
```
🌟 ai-lord-lance-simulation.md
   └─→ AI lord army simulation (parallel system, no blocking dependencies)
```

---

## System Integration Matrix

### How Systems Interact

| System A | System B | Integration Type | Status | Notes |
|----------|----------|------------------|--------|-------|
| **Time System** | AI Camp Schedule | ⚡ Required | 📋 Planned | Schedule uses 6 time periods |
| **Time System** | Camp Activities Hub | ⚡ Required | ✅ Partial | Activities filter by time |
| **Time System** | Duty Events | ⚡ Required | 📋 Planned | Events fire at specific times |
| **AI Camp Schedule** | Lance Life Simulation | 🔗 Bidirectional | 📋 Spec exists | Schedule ↔ Member availability |
| **AI Camp Schedule** | Camp Activities Hub | ⚠️ Coordination | 📋 Planned | Duty blocking vs free time |
| **Lance Life Simulation** | Persistent Lance Leaders | 🔗 Integrates | 📋 Planned | Leaders react to member events |
| **Lance Life Simulation** | Duty Events | 🔗 Triggers | 📋 Planned | Events reference member state |
| **Persistent Lance Leaders** | Duty Events | 🔗 Integrates | 📋 Planned | Leaders comment on choices |
| **AI Lord Lance Sim** | All Others | ⚪ Parallel | 📋 Planned | Independent system |

**Legend:**
- ⚡ Required - One system cannot function without the other
- 🔗 Integrates - Systems share data and coordinate
- ⚠️ Coordination - Need to coordinate but not tightly coupled
- ⚪ Parallel - Independent, no integration needed

---

## Alignment Analysis by System

### 1. Time-of-Day System (camp_schedule_system_analysis.md)

**Status:** ✅ **WELL-ALIGNED**

**Current State:**
- 4 time periods (Dawn, Day, Dusk, Night)
- Already implemented in `CampaignTriggerTrackerBehavior`
- Events can filter by time-of-day

**Planned Enhancement:**
- Expand to 6 periods (Dawn, Morning, Afternoon, Evening, Dusk, Night)
- Minimal code changes (30 minutes)
- Enables more precise event timing

**Dependencies:**
- ✅ No dependencies
- ✅ Must be done BEFORE AI Camp Schedule
- ✅ Must be done BEFORE Phase 2 of Camp Activities Hub

**Integration:**
- ✅ `ai-camp-schedule.md` references 6-period system ✓
- ✅ `CAMP_ACTIVITIES_MENU_IMPLEMENTATION.md` references 6-period system ✓
- ✅ `DUTY_EVENTS_CREATION_GUIDE.md` uses time-of-day triggers ✓
- ✅ `lance-life-simulation.md` uses time-of-day for events ✓

**Recommendation:** ✅ **Implement immediately** - This is foundational for everything else.

---

### 2. Camp Activities Hub (CAMP_ACTIVITIES_MENU_IMPLEMENTATION.md)

**Status:** ✅ **WELL-ALIGNED** with minor clarification needed

**Current State:**
- Phase 1 complete (activity cards working)
- Phase 2 next (add location-based hub)

**Alignment Check:**

✅ **With Time System:**
- Activities filter by `time_of_day` field
- Uses 6-period system from `camp_schedule_system_analysis.md`
- Properly integrated

✅ **With AI Camp Schedule:**
- Clear separation documented:
  - AI Schedule = **mandatory duties**
  - Camp Hub = **free time activities**
- Integration point: "Visit Camp" greyed out when player on duty
- Properly coordinated

✅ **With Lance Life Simulation:**
- Location awareness: Shows which lance members are where
- Injury indicators: "Check on Wounded" option when members injured
- Memorial support: "Honor the Fallen" option
- Properly integrated via `LANCE_LIFE_SIMULATION_INTEGRATION_GUIDE.md`

✅ **With Escalation Systems:**
- Status bar shows: Fatigue, Heat, Lance Rep, Medical Risk, Pay Owed
- All existing systems properly referenced

**Minor Issue:**
⚠️ **Phase 2.5 "Duty Cover Request System" overlaps with Lance Life Simulation**
- Camp Activities doc mentions duty swapping (Phase 2.5, marked as future brainstorm)
- Lance Life Simulation already has cover request system designed
- **Resolution:** Phase 2.5 should reference Lance Life's cover system, not duplicate it

**Recommendation:** 
- ✅ Continue with Phase 2 as planned
- 📝 Update Phase 2.5 section to reference `lance-life-simulation.md` cover request system instead of designing new one

---

### 3. AI Camp Schedule (ai-camp-schedule.md)

**Status:** ✅ **WELL-ALIGNED** with one clarification

**Scope:**
- AI assigns daily duties to player
- 6 time periods per day
- Based on lord's objectives and player's tier/formation
- 12-day cycle aligned with pay muster

**Alignment Check:**

✅ **With Time System:**
- Uses 6-period day (Dawn, Morning, Afternoon, Evening, Dusk, Night)
- Matches `camp_schedule_system_analysis.md` exactly
- Properly aligned

✅ **With Camp Activities Hub:**
- Clear delineation: Schedule assigns **duties**, Hub provides **free time activities**
- Integration documented in both files
- Menu gating logic specified
- Properly coordinated

✅ **With Lance Life Simulation:**
- Interface `ILanceScheduleModifier` defined for cross-system communication
- Injury removal from schedule specified
- Cover request integration specified
- Properly designed per `LANCE_LIFE_SIMULATION_INTEGRATION_GUIDE.md`

✅ **With Duties System:**
- Respects existing formation-based duties
- Uses existing duty definitions from `duties_system.json`
- Compatible with duty request system

**T5-T6 Lance Leadership Feature:**
- Schedule gives player control over lance member assignments at higher tiers
- Introduces "Lance Needs" management system
- **Potential Overlap:** This overlaps conceptually with Lance Life Simulation's management aspects

**Clarification Needed:**
⚠️ **"Lance Needs" vs. Lance Life Simulation**
- AI Camp Schedule has "Lance Needs" (Readiness, Equipment, Morale, Rest, Supplies) at T5-T6
- Lance Life Simulation tracks member health and availability
- These are **complementary** but need clear boundaries:
  - **Lance Needs** = aggregate lance resource management (T5-T6 leadership gameplay)
  - **Lance Life** = individual member simulation (all tiers, player as participant)

**Recommendation:** 
- ✅ Systems are complementary, not conflicting
- ✅ Cross-references added to both docs

---

### Issue 5: Army Lance Activity Casualty System Balance ⚠️ NEW

**Feature:**
- Army Lance Activity Simulation now includes optional **real party casualty system**
- Simulated events (patrol ambushes, foraging incidents) can apply actual casualties to party rosters

**Considerations:**

**Pros:**
- ✅ Realistic attrition warfare simulation
- ✅ Strategic depth (timing, army management matter)
- ✅ Mechanical weight (simulation has real consequences)
- ✅ Historical accuracy (armies lost more to attrition than battles)

**Cons:**
- ⚠️ Could be too punishing for casual players
- ⚠️ Requires extensive balance testing
- ⚠️ AI lords may not recruit fast enough
- ⚠️ Adds complexity to party management

**Resolution:**
✅ **Highly configurable system with presets:**

| Preset | Casualties | Multiplier | Attrition | Use Case |
|--------|-----------|------------|-----------|----------|
| Casual | Disabled | N/A | 0% | Narrative only |
| Normal | Enabled | 0.3 | 5-8% | Default, balanced |
| Realistic | Enabled | 0.5 | 10-15% | Historical sim |
| Hardcore | Enabled | 0.7 | 15-25% | Challenge |

**Key Design Decisions:**
- Default to "Normal" (0.3 multiplier, player army only)
- 70% wounded (recoverable), 30% killed
- Heroes never affected
- Can disable entirely if disliked
- Week 27 dedicated to balance playtesting

**Integration:**
- Uses Bannerlord's existing casualty and wounded systems
- Wounded recovery mirrors post-battle mechanics
- Clear news attribution (no confusion with battles)
- Save/load preserves wounded troop records

**Action Required:**
- ✅ Week 27 added to roadmap for balance testing
- ✅ Configuration documentation in Army Lance Activity doc
- 📝 Player education needed (tutorial about attrition)
- 📝 Monitor AI recruitment in playtests

---

### 4. Lance Life Simulation (lance-life-simulation.md)

**Status:** ✅ **WELL-ALIGNED**

**Scope:**
- Player's lance member simulation
- Injuries, deaths, cover requests
- Promotion escalation (Lance Leader vacancy)
- Integration with AI Camp Schedule

**Alignment Check:**

✅ **With AI Camp Schedule:**
- Integration guide exists: `LANCE_LIFE_SIMULATION_INTEGRATION_GUIDE.md`
- Interface `ILanceScheduleModifier` properly specified
- Clear coordination on member availability
- Properly aligned

✅ **With Persistent Lance Leaders:**
- Both systems track lance leader identity
- **Clarification:** Need to specify which system "owns" the current lance leader
  - Lance Life Simulation: Promotion escalation creates vacancy
  - Persistent Lance Leaders: Generates unique leader when assigned
- These work together: Lance Life determines **when** leader changes, Persistent determines **who** the new leader is

✅ **With Escalation Systems:**
- Uses existing Heat, Discipline, Lance Rep, Medical Risk tracks
- Cover requests affect relationships
- Injuries trigger medical events
- Properly integrated

✅ **With Camp Activities:**
- Activities can appear based on injured members
- "Check on Wounded" option specified in both docs
- Properly coordinated

**Recommendation:** 
- ✅ Well-aligned
- 📝 Clarify "lance leader ownership" - Suggest: Lance Life handles **promotion/vacancy**, Persistent Lance Leaders handles **personality/memory**

---

### 5. Persistent Lance Leaders (PERSISTENT_LANCE_LEADERS.md)

**Status:** ✅ **WELL-ALIGNED**

**Scope:**
- Unique, culture-appropriate lance leaders
- Memory system (last 15 events)
- Dynamic reactions based on player history
- Death and replacement mechanics

**Alignment Check:**

✅ **With Lance Life Simulation:**
- Both handle lance leader transitions
- Complementary: Lance Life = vacancy creation, Persistent = personality/identity
- Death mechanics in both (Lance Life can kill leaders, Persistent handles replacement)
- Properly aligned

✅ **With Duty Events:**
- Lance leader comments on duty event choices
- Memory system records duty events
- Reaction events reference player choices
- Integration points well-defined

✅ **With Escalation Systems:**
- Lance leader reacts to Heat/Discipline thresholds
- Personality affects reactions (Stern vs Pragmatic)
- Properly integrated

✅ **With Promotion System:**
- Lance leader can block/recommend promotions
- Relationship affects advancement
- Properly integrated

**Recommendation:** ✅ Well-aligned, no changes needed

---

### 6. Duty Events (DUTY_EVENTS_CREATION_GUIDE.md)

**Status:** ✅ **WELL-ALIGNED**

**Scope:**
- 150-200 recurring duty events (15-20 per duty)
- Fire every 3-5 days while on duty
- Context-aware (peace/war, lord's actions)
- Three-choice philosophy (Safe/Risky/Corrupt)

**Alignment Check:**

✅ **With Time System:**
- Events use `time_of_day` triggers
- References 6-period system
- Properly aligned

✅ **With Duties System:**
- One event library per duty type (10 duties)
- Uses `has_duty:{id}` trigger
- Formation-aware
- Properly aligned

✅ **With Persistent Lance Leaders:**
- Events designed for lance leader reactions
- Memory system integration specified
- Properly coordinated

✅ **With Lance Life Simulation:**
- Events can reference lance member availability
- Cover requests can trigger duty events
- Injury risks specified
- Properly integrated

✅ **With Escalation Systems:**
- All events affect Heat/Discipline/Lance Rep appropriately
- Corruption paths add Heat
- Heroic actions build Lance Rep
- Properly balanced

**Recommendation:** ✅ Well-aligned, no changes needed

---

### 7. AI Lord Lance Simulation (ai-lord-lance-simulation.md)

**Status:** ✅ **WELL-ALIGNED** (Parallel System)

**Scope:**
- Simulate lance structure for ALL lord armies
- Company status (morale, supplies, readiness)
- Casualty distribution
- Intelligence gathering opportunity

**Alignment Check:**

✅ **Independence:**
- This is a **parallel system** that simulates OTHER lords' armies
- Does NOT interact with player's lance directly
- No dependencies on player lance systems
- Can be built independently

✅ **With Existing Systems:**
- Uses existing party troop counts
- Generates names using same pools as player lance
- Displays in separate menu (enemy army inspection)
- No conflicts

✅ **With Menu System:**
- New menu entry for viewing lord armies
- Does not interfere with player camp menus
- Properly separated

**Recommendation:** 
- ✅ Well-aligned
- 💡 This can be built **in parallel** with other systems
- 💡 Low priority - nice-to-have feature, not critical path

---

### 8. Army Lance Activity Simulation (ARMY_LANCE_ACTIVITY_SIMULATION.md)

**Status:** ✅ **WELL-ALIGNED** (Enhancement System - **UPDATED WITH CASUALTY SYSTEM**)

**Scope:**
- Simulate NPC lances within player's army (not player's lance)
- Lance assignments (patrol, foraging, scouting, guard duty, etc.)
- Dynamic events (success, complications, failures, casualties)
- **🔴 NEW: Real party casualty system** - simulated casualties actually affect party rosters
- Cover request system (NPC lances ask player for help)
- News & bulletin integration
- Battle availability impact
- Attrition warfare simulation

**Alignment Check:**

✅ **With AI Camp Schedule:**
- **Clear Division:**
  - AI Camp Schedule = **player lance** duty assignments
  - Army Lance Activity = **NPC lances** duty assignments
- Both use same duty types and time periods
- Player cover requests temporarily override AI Schedule
- Properly coordinated

✅ **With Lance Life Simulation:**
- **Clear Division:**
  - Lance Life Simulation = **player's lance** member states (detailed)
  - Army Lance Activity = **NPC lances** aggregate states (simplified)
- Same injury/recovery mechanics, appropriate detail level
- Reciprocity possible (NPC lances can request player cover, player can request NPC cover)
- Medical tent capacity shared
- Properly integrated

✅ **With News/Dispatches System:**
- Army Lance Activity is **data source**
- News System is **UI layer**
- Personal feed shows high-priority lance activities
- Camp Bulletin shows comprehensive 7-day history
- StoryKey deduplication prevents spam
- Properly integrated

✅ **With Persistent Lance Leaders:**
- Player's lance has full Persistent Lance Leader (personality/memory)
- NPC lances have basic leaders (name + reputation only)
- No personality system for NPC leaders (keeps it lightweight)
- Cover requests mention NPC leader names for immersion
- Clear boundary

✅ **Priority & Dependencies:**
- Medium priority (Should-Have, not Must-Have)
- Must be built **AFTER** core systems:
  - AI Camp Schedule (provides duty framework)
  - Lance Life Simulation (provides mechanics)
  - News/Dispatches System (provides UI)
- Cannot start until these are stable

**🔴 Major Update: Casualty System Added**

**What Changed:**
- System now includes **optional real party casualty system**
- Simulated events (patrol ambushes, foraging incidents) can apply actual casualties to party rosters
- Wounded recovery tracking (3-14 days configurable)
- Configurable casualty multipliers (0.3-0.7x battle rates)
- Configuration presets: Casual (disabled), Normal, Realistic, Hardcore

**Key Features:**
- Simulates **small encounters** (patrols, foraging, not battles)
- Casualties from routine operations (1-5 troops typically)
- 60-70% wounded (recoverable), 30-40% killed
- Activity-specific danger levels
- Heroes never affected
- Fully configurable (can disable entirely)

**Implementation Impact:**
- 7-8 weeks for full feature set (+1 week for balance)
- 4-5 weeks for MVP (roster, assignments, events with casualties, news)
- Extended Phase 6 for balance tuning
- Requires playtesting at multiple difficulty levels

**Alignment Assessment:**
- ✅ Uses existing Bannerlord casualty/wounded systems
- ✅ Configurable (not forced on players)
- ✅ Well-documented with pros/cons analysis
- ⚠️ Requires extensive balance testing
- ⚠️ May need AI recruitment adjustments

**Recommendation:**
- ✅ Well-aligned with existing systems
- 🔴 Priority upgraded to **Medium-High** (enhanced value)
- 📝 Add to roadmap as **Track E: Enhancement Systems** (post-core)
- 🔷 Build after: AI Schedule + Lance Life + News complete (Week 20-27)
- 💡 Adds significant immersion AND mechanical depth
- 🎯 Include casualty system in MVP (worth extra week)

---

## Cross-System Coordination Analysis

### Coordination Point 1: Time-of-Day System

**Used By:**
1. AI Camp Schedule (assigns duties by time block)
2. Camp Activities Hub (filters activities by time)
3. Duty Events (triggers at specific times)
4. Lance Life Simulation (events use time-of-day)

**Status:** ✅ **CONSISTENT** - All docs reference 6-period system

**Implementation Order:**
1. ✅ Expand `DayPart` enum (30 minutes) - **DO FIRST**
2. Then all other systems can reference it

---

### Coordination Point 2: Lance Leader Identity

**Involved Systems:**
1. Lance Life Simulation - Promotion escalation (when leader changes)
2. Persistent Lance Leaders - Leader personality and memory (who the leader is)

**Current State:**
- Both docs mention lance leaders
- Overlap in death mechanics
- **Needs clarification**

**Recommended Division:**

| Aspect | Owned By | Reason |
|--------|----------|--------|
| **Leader vacancy/promotion timing** | Lance Life Simulation | Handles career progression |
| **Leader personality/memory** | Persistent Lance Leaders | Handles character depth |
| **Death triggers (in combat)** | Lance Life Simulation | Part of member simulation |
| **Death consequences (replacement)** | Persistent Lance Leaders | Generates new personalities |
| **Current leader lookup** | Persistent Lance Leaders | Single source of truth |

**Integration Pattern:**
```csharp
// Lance Life Simulation creates vacancy
LanceLifeSimulation.CreateLeaderVacancy(reason);
    ↓
// Persistent Lance Leaders handles replacement
PersistentLanceLeaders.OnLeaderVacancy();
    ↓
// Generates new leader with personality
var newLeader = PersistentLanceLeaders.GenerateLeader(lord);
    ↓
// Player offered promotion OR new leader assigned
if (IsPlayerReady())
    OfferPlayerPromotion();
else
    AssignNewLeader(newLeader);
```

**Action Required:** 📝 Add cross-reference section to both docs clarifying this division

---

### Coordination Point 3: Cover Requests

**Involved Systems:**
1. Lance Life Simulation - Cover request events and tracking
2. Camp Activities Hub - Phase 2.5 mentions duty swapping
3. AI Camp Schedule - Duty reassignment

**Current State:**
- Lance Life Simulation has complete cover request design
- Camp Activities Phase 2.5 is marked "FUTURE - BRAINSTORM"
- No conflict, but potential duplication

**Recommendation:**
✅ **Lance Life Simulation owns cover requests**
- Camp Activities Phase 2.5 should be a reference to Lance Life's system, not a new design
- AI Camp Schedule provides the `ILanceScheduleModifier` interface for duty swapping

**Action Required:** 📝 Update Camp Activities Phase 2.5 to reference Lance Life Simulation's cover request system

---

### Coordination Point 4: Menu Structure

**Involved Systems:**
1. camp_menu_overhaul.md - Defines menu hierarchy
2. CAMP_ACTIVITIES_MENU_IMPLEMENTATION.md - Camp Hub navigation
3. Lance Life Simulation - Lance roster display

**Current State:**
✅ **Clear separation:**

```
enlisted_status (Main Hub)
    ↓
    ├─→ command_tent (Camp)
    │       ├─→ Service Records
    │       ├─→ Pay Status
    │       └─→ Visit Camp → Camp Hub (6 locations)
    │
    └─→ enlisted_lance (My Lance)
            ├─→ Lance Roster (with health indicators)
            ├─→ Lance Activities
            └─→ Talk to Lance Leader
```

**Alignment:** ✅ No conflicts, clear hierarchy

---

### Coordination Point 5: Fatigue Resource

**Shared By:**
1. AI Camp Schedule - Duties cost fatigue
2. Camp Activities Hub - Activities cost fatigue
3. Lance Life Simulation - Cover requests cost fatigue

**Status:** ✅ **PROPERLY SHARED**

**Implementation:**
- Single fatigue pool managed by `EnlistmentBehavior` or `CampFatigueBehavior`
- All systems read/modify same value
- Range: 0-30+ (configurable max)
- All docs reference this consistently

**Alignment:** ✅ No conflicts

---

### Coordination Point 6: Lance Member Simulation

**Involved Systems:**
1. Lance Life Simulation - Player's lance members (health, career, relationships)
2. AI Camp Schedule - Uses member availability for duty assignments
3. Army Lance Activity Simulation - NPC lances in player's army (aggregate)
4. AI Lord Lance Simulation - Other lords' lance members (aggregate only)

**Status:** ✅ **CLEAR BOUNDARIES**

**Scope Division:**

| Aspect | Player's Lance (Lance Life) | NPC Lances in Player Army (Army Lance Activity) | Other Lord Lances (AI Lord) |
|--------|----------------------------|-------------------------------------------------|------------------------------|
| **Detail Level** | Individual member tracking | Aggregate lance state | Aggregate statistics |
| **Health Tracking** | Yes (minor/major/dead) | Yes (readiness 0-100) | No (casualties as numbers) |
| **Relationships** | Yes (per member) | No (lance-level favor) | No |
| **Schedule Integration** | Yes (removes injured) | Yes (lances on duty) | No (not scheduled) |
| **Death Events** | Yes (named events) | Yes (casualties reported) | Yes (statistical) |
| **Purpose** | Player immersion | Army immersion | Intelligence/context |
| **Cover Requests** | Player receives | NPC lances request | N/A |

**Alignment:** ✅ No conflicts - clear scope separation by detail level

---

## Terminology Consistency Check

### ✅ Consistent Terms

| Term | Definition | Used Consistently? |
|------|------------|-------------------|
| **Lance** | 8-12 member unit | ✅ Yes |
| **Lance Leader** | Leader of a lance (T4-T6) | ✅ Yes |
| **Duty** | Assigned military role | ✅ Yes |
| **Activity** | Player-initiated camp action | ✅ Yes |
| **Time Period** | 6 periods per day | ✅ Yes (after expansion) |
| **Escalation Track** | Heat/Discipline/Lance Rep/etc. | ✅ Yes |
| **Cover Request** | Lance member asks player to cover duty | ✅ Yes |
| **Schedule** | AI-assigned daily duties | ✅ Yes |

### ⚠️ Minor Inconsistencies

| Term | Issue | Where | Resolution |
|------|-------|-------|------------|
| **"Lance Needs"** | AI Camp Schedule introduces this for T5-T6 | ai-camp-schedule.md | ✅ OK - different from member health (aggregate resource management) |
| **"Company Status"** | Used in AI Lord Lance Sim, but not player's lance | ai-lord-lance-simulation.md | ✅ OK - specific to that system |
| **"Schedule Board"** vs **"Camp Hub"** | Different UI concepts | ai-camp-schedule.md vs CAMP_ACTIVITIES | ✅ OK - different menus (duty schedule vs free time) |

**Overall:** ✅ Terminology is consistent and clear

---

## Potential Issues & Resolutions

### Issue 1: Lance Leader System Overlap ⚠️

**Problem:**
- Lance Life Simulation handles promotion/vacancy
- Persistent Lance Leaders generates personalities
- Both mention death mechanics
- Could be confusing who owns what

**Resolution:**
✅ **Divide responsibilities clearly:**

**Lance Life Simulation owns:**
- Promotion timing and eligibility
- Creating vacancies (when leader leaves/dies)
- Escalation paths (promotion/transfer/injury/death/retirement)
- Integration with AI Camp Schedule

**Persistent Lance Leaders owns:**
- Leader personality generation
- Memory system (last 15 events)
- Dynamic reactions and dialogue
- Replacement personality after death

**Integration Flow:**
```
Lance Life: "Leader dies in battle"
    ↓
Lance Life: CreateVacancy(reason: "battle_casualty")
    ↓
Persistent Leaders: OnLeaderVacancy(reason)
    ↓
IF player ready:
    Persistent Leaders: OfferPlayerPromotion()
ELSE:
    Persistent Leaders: GenerateReplacementLeader(lord, predecessorInfo)
    ↓
    Persistent Leaders: TriggerIntroductionEvent(newLeader)
```

**Action Required:** 
📝 Add integration section to both docs explaining this handoff

---

### Issue 2: Duty Events vs. AI Schedule Events ⚠️

**Problem:**
- Duty Events guide creates 150-200 duty-specific events (fire every 3-5 days)
- AI Camp Schedule creates "schedule block events" (fire during scheduled duties)
- Are these the same events or different?

**Analysis:**
Looking at the docs more carefully:

**Duty Events (DUTY_EVENTS_CREATION_GUIDE.md):**
- Fire every 3-5 days
- Tied to player's **assigned duty role** (Quartermaster, Scout, etc.)
- Example: "Merchant Negotiation" (Quartermaster duty)
- Use existing `has_duty:{id}` trigger

**AI Schedule Block Events (ai-camp-schedule.md):**
- Fire **during scheduled duty blocks**
- Tied to **specific schedule assignment** (Morning Drill, Guard Duty, Patrol)
- Example: "Sentry encounters rider" (during Guard Duty block)
- Use schedule-specific triggers

**Clarification:**
These are **different event types** with different purposes:

| Aspect | Duty Events | Schedule Block Events |
|--------|-------------|----------------------|
| **Frequency** | Every 3-5 days | During each scheduled block |
| **Scope** | Role-specific (Quartermaster) | Task-specific (Sentry Duty) |
| **Trigger** | `has_duty:quartermaster` | `schedule_block:guard_duty` |
| **Purpose** | Advance duty role skills | Handle specific assignments |
| **Quantity** | 15-20 per duty × 10 duties = 150-200 | Many per block type |

**Resolution:**
✅ **These are complementary, not conflicting:**
- **Duty Events** = Long-term role progression (once per few days)
- **Schedule Block Events** = Daily duty execution (multiple per day)

**Example: Quartermaster player on a typical day:**
```
Morning Block: Guard Duty
    → Schedule Block Event: "Sentry encounters rider" (fires during this block)

Afternoon Block: Free Time
    → Player visits Camp Hub, chooses activities

Evening Block: Inventory Check
    → Duty Event: "Supplier Negotiation" (fires because player is Quartermaster, on 3-day cooldown)

Night Block: Sleep
    → No events (rest block)
```

**Action Required:** 
📝 Add clarification to Duty Events guide explaining relationship to schedule block events

---

### Issue 3: Camp Hub Phase 2.5 Duplication ⚠️

**Problem:**
- Camp Activities Phase 2.5 designs a "Duty Cover Request System"
- Lance Life Simulation already has complete cover request design
- Potential duplication of effort

**Resolution:**
✅ **Lance Life Simulation owns cover requests**

**Camp Activities Phase 2.5 should:**
- Reference Lance Life's cover request system
- NOT design a new system
- Document UI/UX for how cover requests appear in camp context
- Focus on display, not logic

**Action Required:**
📝 Rewrite Camp Activities Phase 2.5 as "UI for Lance Life Cover Requests" instead of new system design

---

### Issue 4: Time System Implementation Order 🔴

**Problem:**
- Time system expansion (4→6 periods) is prerequisite for many systems
- Must be done BEFORE AI Camp Schedule or Camp Activities Phase 2
- Only takes 1-2 hours
- Not called out as critical priority

**Resolution:**
🔴 **CRITICAL PATH ITEM**

**Implementation Order MUST be:**
1. ✅ Expand time system (1-2 hours) ← **DO IMMEDIATELY**
2. Then: AI Camp Schedule OR Camp Activities Phase 2
3. Then: Lance Life Simulation
4. Then: Persistent Lance Leaders
5. Then: Duty Events content creation

**Action Required:**
🔴 Mark time system expansion as **BLOCKING** and **PRIORITY 1**

---

## Recommended Implementation Order

### Critical Path (Must Do in Order)

**Week 0: Foundation**
```
🔴 PRIORITY 1 (1-2 hours):
   └─→ Expand time system (4→6 periods)
       └─→ Unblocks: ALL time-dependent systems
```

**Weeks 1-4: Core Camp Systems**
```
🔷 Track A: Camp Activities Phase 2
   ├─ Week 1: Camp Hub with 6 locations
   ├─ Week 2: Location filtering and navigation
   └─ Weeks 3-4: Polish and themed areas

🔷 Track B: AI Camp Schedule (can build in parallel)
   ├─ Weeks 1-2: Core scheduling engine
   ├─ Week 3: Schedule menu and display
   └─ Week 4: Block events and integration
```

**Weeks 5-10: Lance Systems**
```
🔶 Lance Life Simulation (12 weeks, overlap with below)
   ├─ Weeks 5-6: Foundation + Injury system
   ├─ Weeks 7-8: Cover request system
   ├─ Weeks 9-10: Death and memorial
   └─ Weeks 11-12: Promotion escalation

🔶 Persistent Lance Leaders (10 weeks, can start after Lance Life foundation)
   ├─ Weeks 7-8: Core generation + persistence
   ├─ Weeks 9-10: Memory system
   ├─ Weeks 11-12: Reaction system
   └─ Weeks 13-14: Death & replacement + integration
```

**Ongoing: Content Creation**
```
📝 Duty Events (150-200 events)
   └─ Create as Lance Life and Persistent Leaders are developed
   └─ 10-20 events per week throughout development
```

**Parallel Track: Optional Feature**
```
🌟 AI Lord Lance Simulation (10 weeks)
   └─ Can be built anytime, no dependencies
   └─ Nice-to-have, not critical path
```

---

## Integration Testing Requirements

### Test Matrix

| System A | System B | Integration Test | Passed? |
|----------|----------|-----------------|---------|
| Time System | AI Camp Schedule | Duties assigned by time period | 📋 Pending |
| Time System | Camp Activities | Activities filter by time | 📋 Pending |
| AI Camp Schedule | Lance Life | Injured members removed from schedule | 📋 Pending |
| AI Camp Schedule | Camp Activities | "Visit Camp" blocked during duties | 📋 Pending |
| Lance Life | Persistent Leaders | Vacancy triggers leader replacement | 📋 Pending |
| Lance Life | Camp Activities | "Check on Wounded" appears when needed | 📋 Pending |
| Persistent Leaders | Duty Events | Leader comments on player choices | 📋 Pending |
| All Systems | Escalation | Heat/Discipline/Rep changes correctly | 📋 Pending |

---

## Documentation Quality Assessment

### Strengths ✅

1. **Comprehensive Coverage:**
   - Every major system has detailed implementation plan
   - Integration points clearly documented
   - Technical architecture specified

2. **Consistent Structure:**
   - Most docs follow similar format
   - Clear sections for overview, architecture, implementation
   - Good use of examples and code snippets

3. **Integration Awareness:**
   - Docs cross-reference each other
   - Dependencies identified
   - Integration points specified

4. **Technical Depth:**
   - Code examples provided
   - Data structures defined
   - APIs specified

5. **Realistic Timelines:**
   - Phase breakdowns with week estimates
   - Deliverables clearly defined
   - Testing phases included

### Areas for Improvement 📝

1. **Missing Cross-References:**
   - Some integration points mentioned but not linked
   - Would benefit from explicit "See also" sections

2. **Implementation Order:**
   - Dependencies mentioned but not strictly ordered
   - Critical path not explicitly called out
   - Time system expansion buried in analysis doc

3. **Ownership Clarity:**
   - Some overlapping systems (lance leaders) need clearer boundaries
   - "Who owns what" should be more explicit

4. **Testing Strategy:**
   - Individual testing in each doc
   - Missing **integration testing** strategy across systems
   - Should have master test plan

---

## Recommended Actions

### 🔴 CRITICAL (Do Immediately)

1. **Implement Time System Expansion (1-2 hours)**
   - File: `src/Mod.Core/Triggers/CampaignTriggerTrackerBehavior.cs`
   - Expand `DayPart` enum from 4 to 6 periods
   - Update `GetDayPart()` logic
   - Update trigger evaluator
   - **Blocks:** All time-dependent systems

### 📝 HIGH PRIORITY (Update Docs)

2. **Clarify Lance Leader System Boundaries**
   - Add integration section to `lance-life-simulation.md`
   - Add integration section to `PERSISTENT_LANCE_LEADERS.md`
   - Specify: Lance Life = timing, Persistent = personality

3. **Update Camp Activities Phase 2.5**
   - Rewrite as reference to Lance Life's cover request system
   - Remove duplicate design
   - Focus on UI/UX only

4. **Add Master Implementation Order Doc**
   - Create `IMPLEMENTATION_ROADMAP.md`
   - List all plans in dependency order
   - Mark blocking relationships
   - Define critical path

### 💡 MEDIUM PRIORITY (Enhance)

5. **Create Integration Testing Plan**
   - Document test scenarios for all integration points
   - Define success criteria
   - Create test data/scripts

6. **Add Cross-Reference Sections**
   - Each doc should have "Related Systems" section
   - Explicit links to integration points
   - "See also" recommendations

### 🌟 LOW PRIORITY (Nice-to-Have)

7. **AI Lord Lance Simulation**
   - Can be built anytime
   - No blocking dependencies
   - Consider after core systems stable

---

## Conclusion

### Overall Assessment: ✅ STRONG ALIGNMENT

Your implementation plans are **well-designed and complementary**. The systems work together cohesively and show clear understanding of integration points.

**Key Strengths:**
- ✅ Clear system boundaries
- ✅ Well-defined dependencies
- ✅ Consistent terminology
- ✅ Realistic timelines
- ✅ Integration awareness

**Key Findings:**
1. 🔴 **Time system expansion is critical path** - must do first
2. ⚠️ **Lance leader ownership needs clarification** - add integration sections
3. ⚠️ **Cover request duplication** - consolidate under Lance Life Simulation
4. ✅ **No major conflicts found** - systems complement each other well

**Recommended Next Steps:**
1. Implement time system expansion (1-2 hours)
2. Update docs for lance leader integration
3. Update Camp Activities Phase 2.5
4. Create master roadmap
5. Begin Camp Activities Phase 2 OR AI Camp Schedule (parallel tracks)

**Timeline Impact:**
- No major delays expected
- Some reordering needed for dependencies
- Time system expansion is quick win
- Parallel development tracks can accelerate overall timeline

---

---

## Complete Implementation Order (Final)

**The 27-Week Journey:**

```
WEEK 0: 🔴 Time System Expansion (1-2 hours)
└─→ Blocks: Everything

WEEKS 1-8: 🔷 AI Camp Schedule
├─ Week 1: Foundation & basic scheduling
├─ Week 2: Lance needs system
├─ Week 3-4: Block execution & events
├─ Week 5-6: T5-T6 leadership & pay muster
└─ Week 7-8: Polish & integration

WEEKS 9-20: 🔶 Lance Life Simulation
├─ Week 9-10: Foundation & injury system
├─ Week 11-12: Death & memorial
├─ Week 13-14: Cover request system
├─ Week 15-19: Promotion escalation
└─ Week 20: Polish & balance

WEEKS 15-30: 🔶 Persistent Lance Leaders (overlaps with Lance Life)
├─ Week 15-16: Core generation & persistence
├─ Week 17-18: Memory system
├─ Week 19-20: Reaction system
├─ Week 21-22: Death & replacement
└─ Week 23-24: Integration & polish

WEEKS 20-27: 🟡 Army Lance Activity Simulation (NEW)
├─ Week 20: Foundation (roster, save/load)
├─ Week 21: Assignments & state tracking
├─ Week 22: Event generation + 🔴 CASUALTY APPLICATION
├─ Week 23: News integration + attribution
├─ Week 24: Cover requests (optional for MVP)
├─ Week 25: Battle integration (optional for MVP)
├─ Week 26: Polish & basic balance
└─ Week 27: 🔴 CASUALTY BALANCE PLAYTESTING

THROUGHOUT: 📝 Duty Events Content
└─ Target: 150-200 events, create 10-20 per week
```

**MVP stops at Week 16** (AI Schedule + Lance Life basic)  
**Enhanced stops at Week 27** (adds Army Lance Activity with casualties)  
**Full feature set:** Week 30+ (adds Persistent Leaders complete)

---

## System Feature Comparison

### Lance Simulation Systems Side-by-Side

| Aspect | Player's Lance (Lance Life) | NPC Lances (Army Lance Activity) | Enemy Lances (AI Lord Sim) |
|--------|----------------------------|----------------------------------|----------------------------|
| **Purpose** | Deep player immersion | Army immersion | Intelligence gathering |
| **Detail Level** | Individual members | Aggregate lance state | Aggregate statistics |
| **Health Tracking** | Per member (Healthy/Minor/Major/Dead) | Per lance (Readiness 0-100) | None (casualties as numbers) |
| **Casualties** | Named events, memorials | Real party losses from patrols | Statistical only |
| **Cover Requests** | Player receives from NPCs | NPC lances request from player | N/A |
| **Personality** | Basic relationships | Basic leader names | None |
| **Schedule Integration** | Yes (injured removed from schedule) | Yes (lances on duty) | No |
| **Real Mechanical Impact** | Yes (player member availability) | **YES (actual troop casualties)** 🔴 | No |
| **Configuration** | Injury probabilities | **Casualty multipliers & presets** | N/A |
| **Priority** | Must-Have (P1) | Should-Have (P2) | Nice-to-Have (P3) |
| **Dependencies** | AI Camp Schedule | AI Schedule + Lance Life + News | None |
| **Timeline** | 12 weeks | 7-8 weeks | 10 weeks |

**Key Distinction:**
- **Lance Life** = Your lance, deep simulation, individual tracking
- **Army Lance Activity** = Other lances in YOUR army, aggregate with **real casualties from routine ops**
- **AI Lord Lance** = Other lords' armies, statistics for intelligence

All three are **complementary** with clear boundaries.

---

**Document Version:** 2.0 - Updated with Army Lance Activity Casualty System  
**Review Date:** December 14, 2025  
**Last Updated:** December 14, 2025  
**Next Review:** After time system expansion complete  
**Maintained By:** Enlisted Development Team
