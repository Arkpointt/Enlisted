# Master Implementation Roadmap

**Purpose:** Single source of truth for implementation order and dependencies  
**Date:** December 14, 2025  
**Status:** Active Planning Document

---

## 📚 How to Use This Documentation

### For Developers (Human or AI)

**When starting development, read in this order:**

1. **THIS DOCUMENT (MASTER_IMPLEMENTATION_ROADMAP.md)** 🗺️
   - Read "Quick Reference" → know what to build next
   - Read "Dependency Graph" → understand build order
   - Read your Track section → see weekly breakdown
   - **Purpose:** Answers "What do I build and when?"

2. **THE SPECIFIC FEATURE DOCUMENT** 📖
   - For actual implementation, follow the detailed feature doc:
     - Time System → `camp_schedule_system_analysis.md`
     - AI Camp Schedule → `ai-camp-schedule.md`
     - Lance Life Simulation → `lance-life-simulation.md`
     - Army Lance Activity → `ARMY_LANCE_ACTIVITY_SIMULATION.md`
     - Persistent Leaders → `PERSISTENT_LANCE_LEADERS.md`
     - Camp Hub → `CAMP_ACTIVITIES_MENU_IMPLEMENTATION.md`
     - UI/Screens → `GAUNTLET_CUSTOM_SCREENS_GUIDE.md`
     - Duty Events → `DUTY_EVENTS_CREATION_GUIDE.md`
   - **Purpose:** Detailed week-by-week phases, code examples, data structures

3. **IMPLEMENTATION_ALIGNMENT_REVIEW.md** 📋
   - Reference only when integrating with other systems
   - Check Section 8 for Army Lance Activity integrations
   - Check coordination points when systems touch each other
   - **Purpose:** Answers "How do systems work together?"

### Quick Decision Guide

| Your Question | Use This Document |
|---------------|-------------------|
| "What should I build on Monday?" | **THIS ROADMAP** 🗺️ |
| "How long will this take?" | **THIS ROADMAP** 🗺️ |
| "What's the detailed implementation?" | **Feature-Specific Doc** 📖 |
| "How does this integrate with X?" | **ALIGNMENT_REVIEW.md** 📋 |
| "What are the dependencies?" | **THIS ROADMAP** 🗺️ |
| "What code structure do I need?" | **Feature-Specific Doc** 📖 |

### For AI Implementation

**Give AI these instructions:**

```
1. Read MASTER_IMPLEMENTATION_ROADMAP.md to understand build order and dependencies
2. Follow [FEATURE_NAME].md for detailed week-by-week implementation
3. Reference IMPLEMENTATION_ALIGNMENT_REVIEW.md when integrating with other systems
4. Use GAUNTLET_CUSTOM_SCREENS_GUIDE.md for all UI work
```

---

## Quick Reference

### What's Done ✅
- ✅ Menu restructuring (camp_menu_overhaul.md)
- ✅ Camp Activities Phase 1 (activity cards + screen)

### What's Next 🔴
- 🔴 **Time system expansion (1-2 hours)** ← CRITICAL, BLOCKS EVERYTHING
- 🔷 Camp Activities Phase 2 (camp hub with locations)
- 🔷 AI Camp Schedule (parallel track)

### What's Waiting 📋
- Lance Life Simulation (depends on AI Schedule)
- Persistent Lance Leaders (depends on Lance Life)
- Duty Events content (depends on all above)
- **Army Lance Activity Simulation** (depends on AI Schedule + Lance Life + News)
  - 🔴 **NEW FEATURE:** Real party casualty system
  - Simulates routine operations with actual troop losses
  - Configurable attrition warfare (Casual/Normal/Realistic/Hardcore)
- AI Lord Lance Simulation (independent, can build anytime)

---

## Critical Path

### 🔴 PRIORITY 0: Foundation (1-2 hours) - BLOCKING

**Must complete before ANY other implementation:**

#### Time System Expansion
- **File:** `src/Mod.Core/Triggers/CampaignTriggerTrackerBehavior.cs`
- **Changes:** Expand `DayPart` enum from 4 to 6 periods
- **Duration:** 1-2 hours
- **Doc:** `camp_schedule_system_analysis.md`
- **Blocks:** AI Camp Schedule, Camp Hub Phase 2, Duty Events, Lance Life events

**Why Critical:**
- Currently: 4 periods (Dawn, Day, Dusk, Night) where "Day" = 13 hours
- Needed: 6 periods (Dawn, Morning, Afternoon, Evening, Dusk, Night)
- All downstream systems expect 6 periods for proper event timing
- Quick implementation but blocks everything else

**Implementation Steps:**
1. Expand `DayPart` enum (5 min)
2. Update `GetDayPart()` logic (10 min)
3. Update trigger evaluator (15 min)
4. Test with debug events (30 min)
5. Update event data schema (15 min)

**Success Criteria:**
- Events can filter by all 6 periods
- Time transitions happen at correct hours
- Existing events still work (backwards compatible)

---

## Build Tracks

### Track A: Camp & Activities (6 weeks)

**Phase 1: Foundation (COMPLETE ✅)**
- ✅ Activity card components
- ✅ Camp activities screen
- ✅ Status bar integration
- ✅ Data loading from JSON

**Phase 2: Location Hub (1 week)**
- **Doc:** `CAMP_ACTIVITIES_MENU_IMPLEMENTATION.md` - Phase 2
- **Prerequisite:** Time system expansion complete
- **Duration:** 4-6 days
- **Deliverable:** Camp hub with 6 location buttons, location-filtered area screens

**Tasks:**
1. Add `location` field to activities JSON
2. Create `CampHubScreen` and `CampHubVM`
3. Create `LocationButtonVM` for 6 locations
4. Refactor `CampActivitiesScreen` → `CampAreaScreen` with location filter
5. Update menu hook to open hub instead of direct activities
6. Test navigation flow (hub → area → back)

**Phase 3: Themed Areas (1-2 weeks) - Optional**
- Themed backgrounds per location
- Dynamic visual elements
- Location-specific status displays

**Phase 4: War Room (2-3 weeks) - Future**
- Interactive camp map
- Multi-tab interface
- Real-time alerts

---

### Track B: AI Camp Schedule (7-8 weeks)

**Doc:** `ai-camp-schedule.md`  
**Prerequisite:** Time system expansion complete  
**Can Build:** In parallel with Track A (different features)

**Phase 0: Foundation (Week 1, Days 1-3)**
- Data models (`ScheduledBlock`, `ScheduleDay`, `LanceNeeds`)
- Configuration loading
- Save/load support

**Phase 1: Basic Scheduling (Week 1, Days 4-7)**
- Schedule generation algorithm
- Lord objective → duty assignment logic
- Formation and tier-based filtering

**Phase 2: Lance Needs System (Week 2, Days 1-4)**
- Lance need degradation (Readiness, Equipment, Morale, Rest, Supplies)
- Recovery mechanics
- Need-aware UI display

**Phase 3: AI Schedule Logic (Week 2, Days 5-7)**
- Context-aware scheduling (war/peace/siege)
- Need-balancing algorithm
- Duty distribution optimization

**Phase 4: Schedule Block Execution (Week 3, Days 1-4)**
- Block activation system
- Event triggering during blocks
- Completion tracking

**Phase 5: T5-T6 Leadership (Week 3-4)**
- Player scheduling UI for lance members
- Manual duty assignment
- Trade-off decision making

**Phase 6: Pay Muster Integration (Week 4)**
- 12-day schedule cycle
- Order refresh at muster
- Dynamic adjustment system

**Phase 7: Polish (Week 5)**
- Balance pass
- Edge case handling
- Integration testing

---

### Track C: Lance Systems (14-16 weeks total, overlapping builds)

**Dependencies:** AI Camp Schedule must be complete first

#### C1: Lance Life Simulation (12 weeks)

**Doc:** `lance-life-simulation.md`  
**Prerequisite:** AI Camp Schedule (provides `ILanceScheduleModifier` interface)

**Weeks 1-2: Foundation**
- `LanceLifeSimulationBehavior` with state tracking
- `LanceMemberState` struct with save/load
- Daily processing hooks
- Integration with AI Schedule

**Weeks 3-4: Injury System**
- Injury probability checks
- Health state progression
- Recovery tracking
- Medical tent integration

**Weeks 5-6: Death & Memorial**
- Death mechanics (combat, disease, accidents)
- Memorial service events
- Roster management
- Morale impact

**Weeks 7-8: Cover Request System**
- Cover request evaluation
- Player decision events
- Favor tracking
- Relationship impacts

**Weeks 9-11: Promotion Escalation**
- Player readiness tracking
- Escalation path selection
- Vacancy creation (5 paths)
- Promotion ceremony

**Week 12: Polish**
- Balance probabilities
- Integration testing
- Content variety

**Integration Points:**
- Provides member availability to AI Schedule
- Triggers Persistent Lance Leaders on vacancy
- Uses existing event infrastructure

#### C2: Persistent Lance Leaders (10 weeks, starts Week 7)

**Doc:** `PERSISTENT_LANCE_LEADERS.md`  
**Prerequisite:** Lance Life Simulation foundation (Week 6)  
**Can Overlap:** Starts while Lance Life continues

**Weeks 7-8: Core Generation**
- `PersistentLanceLeader` data class
- `PersistentLanceLeadersBehavior` with save/load
- Name generation from existing pools
- Personality trait system

**Weeks 9-10: Memory System**
- `MemoryEntry` structure
- Memory queue (15 max, FIFO)
- Event recording hooks
- Memory decay (30 days)

**Weeks 11-12: Reaction System**
- Tone determination logic
- Dynamic dialogue generation
- Reaction event templates
- Personality-based responses

**Weeks 13-14: Death & Replacement**
- Death processing
- Replacement generation
- Memorial system
- Introduction events

**Weeks 15-16: Integration & Polish**
- Camp news integration
- Promotion system hooks
- 20+ reaction events
- Comprehensive testing

**Integration Points:**
- Receives vacancy notifications from Lance Life
- Provides leader identity to all systems
- Comments on duty events
- Reacts to escalation thresholds

---

### Track E: Enhancement Systems (Build After Core - Week 20+)

**IMPORTANT:** Track E should only be started after core systems (Tracks B & C) are complete and stable.

#### E1: Army Lance Activity Simulation (7-8 weeks, Week 20-27)

**Doc:** `ARMY_LANCE_ACTIVITY_SIMULATION.md`  
**Prerequisites:** AI Camp Schedule + Lance Life Simulation + News/Dispatches System (must be complete and stable)  
**Priority:** 🟡 Medium (Should-Have for immersion and mechanical depth)

**Purpose:**
Simulate 8-15 NPC lances in player's army with realistic routine operations and optional attrition warfare.

**Core Features:**
1. **Lance Roster Management**
   - Culture-appropriate lance names ("The Bold Hawks", "Ironwood Guard")
   - 8-15 lances per army (based on troop count)
   - Named lance leaders (basic, not full personality)
   - Lance state tracking (readiness 0-100, availability status)

2. **Activity Simulation**
   - **Wartime:** Patrols, scouting, foraging, convoy escort, picket duty
   - **Peacetime:** Training, recruitment, tax collection, bandit suppression, village protection
   - **Universal:** Guard duty, equipment maintenance, recovery, discipline
   - Dynamic assignment based on lord's objectives and army context

3. **🔴 Real Party Casualty System** (Major New Feature)
   - **What It Does:** Simulated casualties from routine operations actually affect party rosters
   - **What It Simulates:**
     - Patrols encountering bandits (5-15 enemies, small skirmishes)
     - Foraging missions ambushed by looters
     - Scouting parties spotted by enemy scouts
     - Guard duty accidents, disease, desertion
     - **NOT full battles** (Bannerlord handles those normally)
   - **How It Works:**
     - Lance on patrol encounters bandits → Event: "2 killed, 3 wounded"
     - System removes 2 troops from party roster (killed)
     - Moves 3 troops to wounded status (recover in 7 days)
     - News reports: "3rd Lance ambushed on patrol - 2 killed, 3 wounded"
     - Army enters real battles weaker due to accumulated patrol losses
   - **Configuration:**
     ```json
     {
       "affect_actual_troops": true,         // Master on/off
       "base_casualty_multiplier": 0.4,      // 40% of battle rates
       "wounded_recovery_days": 7,           // Week recovery
       "apply_to_enemy_armies": false,       // Player only for balance
       "wounded_vs_killed_ratio": 0.7        // 70% wounded, 30% killed
     }
     ```
   - **Presets:**
     - **Casual:** Disabled (narrative only, 0% attrition)
     - **Normal:** 0.3 multiplier, player only (5-8% monthly attrition)
     - **Realistic:** 0.5 multiplier, both armies (10-15% monthly attrition)
     - **Hardcore:** 0.7 multiplier, both armies (15-25% monthly attrition)
   - **Balance:**
     - Conservative defaults (0.3-0.4 multiplier)
     - Most casualties recoverable (70% wounded)
     - Heroes never affected
     - Clear news attribution ("patrol ambush" not "battle")
     - Fully optional (can disable)

4. **Event Generation**
   - Probabilistic based on danger level, context, and lance readiness
   - Event types: Success, Complications (delays, equipment damage), Failures (casualties, mission failed), Rare (heroic, desertion, disease)
   - 20-30 event description templates
   - Activity-specific danger levels (guard 0.05, scouting 0.40)

5. **Cover Request System**
   - NPC lances request player cover when injured/exhausted/undermanned
   - Player can accept (gain favor, fatigue, **prevent real casualties**) or decline
   - Favor tracking system (reciprocity)
   - Maximum 1 request per 2 days (prevents spam)

6. **News & Bulletin Integration**
   - Personal news feed (high-priority lance activities)
   - Camp Bulletin submenu (7-day comprehensive history)
   - Priority-based filtering (casualties always show, routine success bulletin only)
   - StoryKey deduplication

7. **Battle Integration**
   - Lances on assignment miss battles (army fights undermanned)
   - Battle casualties distributed to lances
   - Post-battle news notes absent lances

**Week-by-Week Breakdown:**

**Week 20: Phase 0 - Foundation**
- Data structures (`ArmyLanceRoster`, `SimulatedLance`, `LanceAssignment`, `LanceEvent`)
- Save/load support
- Roster generation on army creation
- Lance name generation (culture-appropriate)
- **Deliverable:** Lance rosters generate and persist

**Week 21: Phase 1 - Assignment & State Tracking**
- Duty assignment logic (based on lord objectives, context)
- Assignment advancement (daily tick, days remaining)
- Readiness degradation (-5 per day on assignment, -20 per combat)
- Readiness recovery (+10 per day resting, +15 in medical tent)
- Lance status transitions (Ready → OnAssignment → Exhausted → Recovery → Ready)
- **Deliverable:** Lances get assigned, states update correctly

**Week 22: Phase 2 - Event Generation + 🔴 Casualty System**
- Probability system (danger level × context × readiness)
- Event type weights (success 50%+reliability, neutral 30%, negative 20%-reliability)
- Event consequence application
- **🔴 Casualty calculation** (event type → casualty count → roster application)
- **🔴 Troop selection** (tier matching, hero exclusion)
- **🔴 Wounded tracking** (move troops to wounded status)
- Event history tracking (last 7 days)
- **Deliverable:** Events fire, casualties apply to party roster

**Week 23: Phase 3 - News Integration**
- News item generation from lance events
- Headline templates (success, enemy contact, casualties, failures)
- Priority system (high: casualties/failures, medium: delays, low: routine)
- Camp Bulletin submenu (7-day history display)
- **🔴 Casualty attribution** ("lost in patrol ambush" vs "battle")
- StoryKey deduplication
- **Deliverable:** Lance activities visible in news and bulletin

**Week 24: Phase 4 - Cover Requests** (optional for MVP)
- Cover request condition evaluation
- Inquiry event creation (10-15 body templates)
- Accept/decline handlers
- Favor tracking per lance
- Fatigue cost application
- **Now more meaningful:** Accepting prevents **real casualties**
- **Deliverable:** Cover requests functional with consequences

**Week 25: Phase 5 - Battle Integration** (optional for MVP)
- Battle casualty distribution to lances
- Army strength calculation (available vs absent lances)
- Post-battle news noting absent lances
- Lance state updates (readiness, undermanned)
- **🔴 Battle casualty vs simulation casualty** tracking
- **Deliverable:** Battles reflect lance availability

**Week 26: Phase 6 - Polish & Balance (Basic)**
- Event probability tuning
- Readiness balance
- Cover request frequency
- Configuration file creation
- **Deliverable:** System balanced for basic play

**Week 27: Phase 6 Extended - 🔴 Casualty Balance Playtesting**
- **CRITICAL:** Playtest each configuration preset
- Casualty rate analysis (30-day campaigns)
- AI recruitment behavior monitoring
- Player feedback collection
- Multiplier adjustments based on data
- Configuration preset refinement
- **Deliverable:** Balanced attrition rates for all presets

**MVP Option (Weeks 20-24, 4-5 weeks):**
- Phases 0-3 + basic casualty system
- Skip cover requests (Phase 4) and battle integration (Phase 5)
- Basic balance only (no extended Week 27 testing)
- Ship with: Roster, assignments, events with casualties, news
- Add Phases 4-5 later if desired

**Full Implementation (Weeks 20-27, 7-8 weeks):**
- All phases including cover requests and battle integration
- Extended balance testing for casualty system
- Complete feature set

**Integration Points:**
- **With AI Camp Schedule:** Uses same duty types and time periods, player cover requests override schedule
- **With Lance Life Simulation:** Same injury mechanics at appropriate detail level, reciprocity system
- **With News/Dispatches:** Lance events feed personal news and bulletin
- **With Bannerlord Core:** Uses existing `MemberRoster` API for casualties, wounded system

**Value Proposition:**
- **Immersion:** Feel part of larger military organization
- **Mechanical Weight:** Simulation has real consequences
- **Strategic Depth:** Army management, timing decisions
- **Historical Realism:** Attrition warfare simulation
- **Player Agency:** Configurable difficulty, can disable
- **Storytelling:** Rich camp bulletin narratives

**Risks & Mitigations:**
- **Risk:** Too punishing for casual players
  - **Mitigation:** Conservative defaults (0.3 multiplier), fully configurable, Casual preset disables
- **Risk:** AI recruitment not keeping up
  - **Mitigation:** Monitor in testing, possible AI boost, player-only default
- **Risk:** Player frustration with background losses
  - **Mitigation:** Clear news reporting, wounded recovery, gradual attrition
- **Risk:** Balance complexity
  - **Mitigation:** Week 27 dedicated to balance, multiple presets, extensive playtesting

**Recommendation:**
- Build after core systems stable (Week 20+)
- Include casualty system in MVP (worth extra week)
- Default to "Normal" preset (0.3 multiplier, player only)
- Make configuration easily accessible
- Dedicate Week 27 to balance playtesting

---

### Track D: Content Creation (Ongoing)

#### Duty Events Library (Continuous)

**Doc:** `DUTY_EVENTS_CREATION_GUIDE.md`  
**Prerequisite:** None (can start creating JSON anytime)  
**Best Time:** During Lance Life + Persistent Leaders development

**Target:** 150-200 duty events total
- 15-20 events per duty role
- 10 duty roles (Quartermaster, Scout, Field Medic, etc.)

**Creation Rate:** 10-20 events per week
**Timeline:** 8-10 weeks of content creation

**Dependencies for Use:**
- Time system (for time-of-day triggers)
- Duties system (already exists)
- Persistent Lance Leaders (for leader reactions) - optional

**Recommendation:** 
- Create event JSON files early (they're just data)
- Test events as systems come online
- Iterate based on playtesting

---

### Track E: Enhancement Systems (Build After Core - Week 20+)

#### E1: Army Lance Activity Simulation (7-8 weeks, Week 20-27) 🔴 UPDATED

**Doc:** `ARMY_LANCE_ACTIVITY_SIMULATION.md`  
**Prerequisites:** AI Camp Schedule + Lance Life Simulation + News/Dispatches System  
**Priority:** 🟡 Medium (Should-Have for immersion)

**Purpose:**
- Simulate NPC lances in player's army
- **🔴 NEW: Real party casualty system** (optional)
- Cover requests from other lances
- Camp bulletin with lance activities
- Resource strain scenarios
- Battle availability impact
- Attrition warfare simulation

**Weeks 20-27: Full Implementation (UPDATED)**
- Week 20: Phase 0 - Foundation (roster generation, save/load)
- Week 21: Phase 1 - Assignment & state tracking
- Week 22: Phase 2 - Event generation with **casualty application system** 🔴
- Week 23: Phase 3 - News integration (bulletin, personal feed, casualty attribution)
- Week 24: Phase 4 - Cover requests (player choices, prevent real casualties)
- Week 25: Phase 5 - Battle integration (availability, real casualty distribution)
- Week 26: Phase 6 - Polish & balance (basic tuning)
- Week 27: Phase 6 Extended - **Casualty balance playtesting** 🔴

**🔴 Casualty System Features:**
- Simulates small encounters (patrols, foraging, NOT full battles)
- Optional: affects actual party troop counts
- Wounded recovery tracking (3-14 days)
- Configurable multipliers (0.3-0.7x battle rates)
- Configuration presets: Casual/Normal/Realistic/Hardcore
- Can be disabled entirely (narrative-only mode)

**MVP Option (Week 20-24, 4-5 weeks):**
- Phases 0-3 + basic casualty system
- Skip cover requests and battle integration initially
- Basic balance (no extended tuning)
- Add Phases 4-5 later

**Value:** Creates feeling of being part of larger military organization + real attrition warfare simulation

**🔴 Major Enhancement:** Now includes optional real party casualty system
- Patrol ambushes apply actual casualties
- Armies weaken through routine operations
- Strategic depth (timing matters, army management)
- Fully configurable (casual to hardcore)

**🔴 Major Features (NEW):**

1. **Real Party Casualty System** (Optional)
   - Simulated casualties **actually affect** party troop counts
   - Simulates small encounters (patrols, foraging, NOT full battles)
   - Typical casualties: 1-5 troops per incident (vs 10-100+ in battles)
   - 60-70% wounded (recoverable in 7 days), 30-40% killed
   - Configurable multipliers: 0.3-0.7x battle casualty rates
   - Heroes never affected
   - Can disable entirely for narrative-only mode

2. **Configuration Presets:**
   - **Casual:** Casualties disabled (narrative only)
   - **Normal:** 0.3 multiplier, player army only, 5-8% monthly attrition
   - **Realistic:** 0.5 multiplier, both armies, 10-15% monthly attrition
   - **Hardcore:** 0.7 multiplier, both armies, 15-25% monthly attrition

3. **Attrition Warfare Simulation:**
   - Armies weaken over campaigns through routine operations
   - Patrol ambushes by bandits
   - Foraging mission attacks
   - Guard duty accidents
   - Disease and desertion
   - Strategic depth: timing matters, army management crucial

**Why This Matters:**
- Realistic: Armies historically lost more to attrition than battles
- Strategic: Can't keep sending lances on dangerous missions
- Immersive: See consequences of routine operations
- Mechanical: Cover requests now prevent **real casualties**
- Player choice: Fully configurable, can disable if too challenging

**Balance Considerations:**
- Requires extensive playtesting (Week 27)
- Conservative defaults (0.3-0.4 multiplier)
- AI lords may need recruitment boost
- Player education needed (tutorial/docs)

**Recommendation:** Build after lance systems stable, adds significant immersion AND mechanical depth

---

### Track F: Optional/Future Systems

#### AI Lord Lance Simulation (10 weeks)

**Doc:** `ai-lord-lance-simulation.md`  
**Dependencies:** None (completely independent)  
**Can Build:** Anytime, in parallel with everything else

**Purpose:**
- Simulate lance structure for enemy/allied armies
- Company status display
- Intelligence gathering feature

**Priority:** 🌟 Low (nice-to-have, not critical path)

**Recommendation:** Build this LAST after core systems are stable and tested.

---

## Timeline Overview

### Gantt Chart (Estimated)

```
Week   | Track A: Camp   | Track B: Schedule | Track C: Lance    | Track D: Content | Track E: Enhancement
-------|-----------------|-------------------|-------------------|------------------|----------------------
   0   | Time Expansion (1-2 hrs) ← CRITICAL BLOCKER                                           |
   1   |                 | Phase 0-1         |                   | Start duty events|
   2   | Phase 2 (Hub)   | Phase 2-3         |                   | +10 events       |
   3   |                 | Phase 4           |                   | +10 events       |
   4   |                 | Phase 5-6         |                   | +15 events       |
   5   |                 | Phase 7 (Polish)  | C1: Foundation    | +15 events       |
   6   | Phase 3         |                   | C1: Injury        | +15 events       |
  (Optional)             |                   | C1: Death         | +15 events       |
   7   | Phase 3         |                   | C1: Cover         | +20 events       |
   8   |                 |                   | C1: Cover         | +20 events       |
       |                 |                   | C2: Generation    |                  |
   9   |                 |                   | C1: Escalation    | +20 events       |
       |                 |                   | C2: Memory        |                  |
  10   |                 |                   | C1: Escalation    | +20 events       |
       |                 |                   | C2: Memory        |                  |
  11   |                 |                   | C1: Polish        | +20 events       |
       |                 |                   | C2: Reactions     |                  |
  12   |                 |                   |                   | +20 events       |
       |                 |                   | C2: Death/Replace |                  |
  13   |                 |                   | C2: Integration   | Polish events    |
  14   |                 |                   | C2: Polish        | Polish events    |
  15-19|                 |                   |                   | Ongoing polish   | (Wait for C complete)
  20   |                 |                   |                   |                  | E1: Foundation
  21   |                 |                   |                   |                  | E1: Assignment
  22   |                 |                   |                   |                  | E1: Events+Casualties🔴
  23   |                 |                   |                   |                  | E1: News+Attribution
  24   |                 |                   |                   |                  | E1: Cover Requests
  25   |                 |                   |                   |                  | E1: Battle
  26   |                 |                   |                   |                  | E1: Polish
  27   |                 |                   |                   |                  | E1: Balance Testing🔴
-------|-----------------|-------------------|-------------------|------------------|----------------------
Total: | 2-3 weeks       | 7-8 weeks         | 14-16 weeks       | Ongoing          | 7-8 weeks (Wk 20-27)🔴
       | (optional ph3-4)| (complete)        | (overlapping)     | (continuous)     | (w/casualty system)
```

**Key Observations:**
- Track A and B can run **in parallel** (different teams/devs)
- Track C must wait for Track B completion (week 5)
- Track C internal overlap: C2 starts week 7 while C1 continues
- Track D is continuous throughout
- Track E starts week 20+ (after core systems stable)
- **Total critical path:** ~27 weeks (6.75 months) with Army Lance Activity
- **MVP critical path:** ~16 weeks (4 months) without enhancements

---

## Dependency Graph

### Visual Dependency Map

```
                    [Time System Expansion]
                    (1-2 hours) 🔴 CRITICAL
                            ↓
        ┌───────────────────┴────────────────────┐
        ↓                                        ↓
[Camp Activities Phase 2]              [AI Camp Schedule]
(1 week)                               (7-8 weeks)
        ↓                                        ↓
[Camp Activities Phase 3]              [Lance Life Simulation]
(1-2 weeks, optional)                  (12 weeks)
                                                ↓
                                    [Persistent Lance Leaders]
                                    (10 weeks, overlaps weeks 7-16)
                                                ↓
                                    ┌───────────┴───────────┐
                                    ↓                       ↓
                            [Duty Events Content]  [Army Lance Activity]
                            (150-200 events)       (7 weeks, Week 20-26)
                            (ongoing)                      ↓
                                                   [Enhancement Complete]

[AI Lord Lance Simulation] ← Independent, can build anytime (10 weeks)
```

---

## Risk Assessment

### High Risk Items 🔴

**1. Time System Expansion Delay**
- **Risk:** Not doing time expansion blocks all other work
- **Impact:** 🔴 CRITICAL - Blocks 4+ systems
- **Mitigation:** Do immediately (only 1-2 hours)
- **Status:** Not started

**2. AI Camp Schedule Complexity**
- **Risk:** System is complex with many integration points
- **Impact:** 🟡 MEDIUM - Lance systems wait for this
- **Mitigation:** Follow phased implementation, test each phase
- **Status:** Well-documented

### Medium Risk Items 🟡

**3. Lance Life + Persistent Leaders Integration**
- **Risk:** Two systems managing lance leaders could conflict
- **Impact:** 🟡 MEDIUM - Could cause confusion
- **Mitigation:** Clear ownership documented (now clarified in this review)
- **Status:** ✅ Resolved

**4. Cover Request System Duplication**
- **Risk:** Camp Activities and Lance Life both designing cover requests
- **Impact:** 🟡 MEDIUM - Wasted effort
- **Mitigation:** Consolidated under Lance Life (fixed in this review)
- **Status:** ✅ Resolved

### Low Risk Items 🟢

**5. Event Content Volume**
- **Risk:** 150-200 duty events is a lot of writing
- **Impact:** 🟢 LOW - Can be done over time
- **Mitigation:** Continuous creation, reuse templates
- **Status:** Manageable

**6. AI Lord Lance Simulation Scope**
- **Risk:** Feature creep, large optional feature
- **Impact:** 🟢 LOW - Completely optional
- **Mitigation:** Build last, can be cut if needed
- **Status:** Low priority

---

## Phase Gates & Decision Points

### Gate 1: Time System Complete ✅/❌

**Criteria:**
- [ ] 6-period enum defined
- [ ] Time detection logic updated
- [ ] Trigger evaluator updated
- [ ] Test events fire correctly
- [ ] Documentation updated

**Decision:** Proceed to Track A & B in parallel

---

### Gate 2: AI Camp Schedule Phase 4 Complete ✅/❌

**Criteria:**
- [ ] Schedule generation works
- [ ] Duty blocks activate correctly
- [ ] Events trigger during blocks
- [ ] Integration with time system verified
- [ ] `ILanceScheduleModifier` interface functional

**Decision:** Proceed to Lance Life Simulation

---

### Gate 3: Lance Life Foundation Complete ✅/❌

**Criteria:**
- [ ] Member state tracking works
- [ ] Injury system functional
- [ ] Integration with AI Schedule verified
- [ ] Save/load tested
- [ ] Ready for Persistent Leaders integration

**Decision:** Begin Persistent Lance Leaders (overlap)

---

### Gate 4: Core Systems Complete ✅/❌

**Criteria:**
- [ ] Camp Hub with locations working
- [ ] AI Camp Schedule fully functional
- [ ] Lance Life Simulation complete
- [ ] Persistent Lance Leaders complete
- [ ] All integration points tested

**Decision:** Focus on content creation and polish

---

## Resource Allocation Recommendations

### If You Have 1 Developer:

**Sequential Build:**
```
Week 0:  Time system expansion
Week 1:  Camp Hub Phase 2
Week 2-8: AI Camp Schedule
Week 9-20: Lance Life Simulation
Week 21-30: Persistent Lance Leaders
Ongoing: Duty Events content
```

**Total:** ~30 weeks (7.5 months) for all core features

---

### If You Have 2 Developers:

**Parallel Build:**
```
Developer A:
Week 0:  Time system expansion
Week 1-3: Camp Hub Phase 2-3
Week 4-11: Duty Events content creation (150-200 events)
Week 12+: Testing and polish

Developer B:
Week 0:  (Support time system)
Week 1-8: AI Camp Schedule
Week 9-20: Lance Life Simulation
Week 21-30: Persistent Lance Leaders
```

**Total:** ~30 weeks (7.5 months) but with more content

---

### If You Have 3+ Developers:

**Parallel Build:**
```
Dev A: Camp Hub (Weeks 0-3)
Dev B: AI Camp Schedule (Weeks 0-8)
Dev C: Content creation (Weeks 1+, then Lance systems Weeks 9+)
```

**Total:** ~20 weeks (5 months) with full feature set

---

## Feature Priority Matrix

### Must-Have (Core Experience) 🔴

| Feature | Priority | Reason |
|---------|----------|--------|
| Time System Expansion | 🔴 P0 | Blocks everything |
| AI Camp Schedule | 🔴 P1 | Core gameplay loop |
| Lance Life Simulation | 🔴 P1 | Core immersion |
| Camp Hub Phase 2 | 🔴 P1 | Major UX improvement |

### Should-Have (Rich Experience) 🟡

| Feature | Priority | Reason |
|---------|----------|--------|
| Persistent Lance Leaders | 🟡 P2 | Adds depth, not critical |
| **Army Lance Activity + Casualties** | 🟡 P2 | **Major immersion & mechanical depth** |
| Camp Hub Phase 3 | 🟡 P2 | Polish, not functional |
| Duty Events (100+) | 🟡 P2 | Good with 50-75, great with 150+ |

### Nice-to-Have (Enhancement) 🟢

| Feature | Priority | Reason |
|---------|----------|--------|
| AI Lord Lance Simulation | 🟢 P3 | Intelligence feature, optional |
| Camp Hub Phase 4 (War Room) | 🟢 P3 | Signature feature but not critical |
| Duty Events (200+) | 🟢 P3 | Diminishing returns after 150 |

---

## Minimum Viable Product (MVP)

**Goal:** Playable, immersive enlisted experience

**Must Include:**
1. ✅ Time system expansion (foundation)
2. ✅ Camp Hub Phase 2 (6 locations)
3. ✅ AI Camp Schedule (through Phase 4)
4. ✅ Lance Life Simulation (through Phase 4 - Cover Requests)
5. ✅ Duty Events library (75-100 events minimum)

**Can Skip:**
- Persistent Lance Leaders (generic leaders OK for MVP)
- Army Lance Activity Simulation (enhancement, not core)
- Camp Hub Phase 3-4 (visual polish)
- Lance Life Phase 5 (Promotion escalation)
- AI Lord Lance Simulation (intelligence feature)
- Additional duty events beyond 100

**MVP Timeline:** 10-12 weeks with focused development

---

## Enhanced Experience (MVP + Army Lance Activity)

**Goal:** Rich immersion with attrition warfare simulation

**Add to MVP:**
6. ✅ Army Lance Activity Simulation (Phases 0-3 minimum)
   - NPC lance roster and assignments
   - Event generation with casualty system
   - News integration and bulletin
   - **Optional:** Cover requests (Phase 4) and battle integration (Phase 5)

**Enhanced Timeline:** 14-17 weeks (MVP + 4-5 weeks for Army Lance Activity)

---

## Testing Strategy

### Unit Testing (Per System)

Each system doc includes testing phases. Follow those.

### Integration Testing (Cross-System)

**Test Suite 1: Time + Events**
- [ ] Events filter correctly by time period
- [ ] Time transitions don't break event queues
- [ ] Cooldowns work across time periods

**Test Suite 2: Schedule + Lance Life**
- [ ] Injured members removed from schedule
- [ ] Schedule gaps filled by available members
- [ ] Cover requests modify schedule correctly
- [ ] Recovery returns members to schedule

**Test Suite 3: Schedule + Camp Hub**
- [ ] "Visit Camp" blocked when on duty
- [ ] "Visit Camp" available during free time
- [ ] Fatigue shared between systems
- [ ] Duty notification timing correct

**Test Suite 4: Lance Life + Persistent Leaders**
- [ ] Vacancy triggers leader replacement
- [ ] Player promotion offers work
- [ ] Leader memory persists across vacancy
- [ ] Introduction events fire correctly

**Test Suite 5: All Systems**
- [ ] Full day cycle (dawn to night)
- [ ] Schedule → duty event → camp activity → lance event
- [ ] Escalation tracks update correctly
- [ ] Save/load preserves all state
- [ ] 30+ day playthrough without crashes

---

## Success Metrics

### Technical Success ✅

- [ ] All systems build without errors
- [ ] No crashes during normal gameplay
- [ ] Save/load works reliably
- [ ] Performance acceptable (no lag)
- [ ] All integration points functional

### Gameplay Success ✅

- [ ] Time system feels natural (not too granular or too coarse)
- [ ] Schedule assignments make sense for context
- [ ] Camp hub navigation intuitive
- [ ] Lance members feel like real people
- [ ] Duty events engaging and varied
- [ ] Player agency preserved throughout

### Content Success ✅

- [ ] 100+ duty events (150-200 target)
- [ ] All 10 duty roles have event coverage
- [ ] Peace and war event variants exist
- [ ] Events don't feel repetitive after 30 days
- [ ] Moral choices have real consequences

---

## Cut Candidates (If Timeline Pressured)

**If you need to ship faster, cut in this order:**

1. **AI Lord Lance Simulation** (saves 10 weeks)
   - Optional intelligence feature
   - No impact on core gameplay
   - Can be added later

2. **Army Lance Activity Simulation** (saves 7-8 weeks)
   - Enhancement, not core feature
   - MVP works without it
   - Can be added post-launch
   - **Note:** Highly recommended to keep if possible (significant value)

3. **Camp Hub Phase 3-4** (saves 3-5 weeks)
   - Visual polish and war room
   - Phase 2 hub is functional
   - Can be added later

4. **Persistent Lance Leaders** (saves 10 weeks)
   - Use generic lance leaders
   - Memory system is enhancement
   - Core gameplay works without it

5. **Lance Life Promotion Escalation** (saves 3 weeks)
   - Player promotion can be simpler
   - Escalation is sophisticated but not required
   - Manual promotion events work

**Absolute Minimum (8-10 weeks):**
- Time system expansion
- Camp Hub Phase 2
- AI Camp Schedule (Phases 0-4)
- Lance Life Simulation (Phases 1-4, skip escalation)
- 50-75 duty events

**Recommended Minimum (14-17 weeks):**
- Absolute Minimum +
- Army Lance Activity Simulation (Phases 0-3 with basic casualty system)
- Why: Adds significant mechanical depth for reasonable time investment

---

## Document Maintenance

### When to Update This Roadmap

**Update triggers:**
- Major system design changes
- Dependency changes
- Timeline adjustments
- Risk mitigation needs
- Feature priority shifts

**Review cadence:**
- Weekly during active development
- After each major phase completion
- When blockers are discovered

### Related Documents

**Must Read:**
- `IMPLEMENTATION_ALIGNMENT_REVIEW.md` - Detailed alignment analysis
- `camp_schedule_system_analysis.md` - Time system expansion steps

**System Docs:**
- `CAMP_ACTIVITIES_MENU_IMPLEMENTATION.md` - Camp hub system
- `ai-camp-schedule.md` - AI scheduling system
- `lance-life-simulation.md` - Lance member simulation
- `PERSISTENT_LANCE_LEADERS.md` - Leader personality system
- `DUTY_EVENTS_CREATION_GUIDE.md` - Event content guide

**Supporting Docs:**
- `LANCE_LIFE_SIMULATION_INTEGRATION_GUIDE.md` - Integration patterns
- `GAUNTLET_CUSTOM_SCREENS_GUIDE.md` - UI technical guide

---

## Quick Start: What To Do Monday Morning

**You're ready to start implementing. Here's your action plan:**

### Step 1: Time System Expansion (Monday Morning, 1-2 hours) 🔴

**File:** `src/Mod.Core/Triggers/CampaignTriggerTrackerBehavior.cs`

1. Find the file (search for `DayPart` enum)
2. Expand enum: Add `Morning`, `Afternoon`, `Evening`
3. Update `GetDayPart()` method with 6-period logic
4. Update trigger evaluator to recognize new periods
5. Test with debug event
6. Commit changes

**See:** `camp_schedule_system_analysis.md` for exact code changes

---

### Step 2: Choose Your Track (Monday Afternoon) 🔷

**Option A: Camp Hub Phase 2 (1 week, user-facing)**
- Immediate visible progress
- Improves player experience now
- Sets foundation for location-based design
- **Start here if:** You want quick wins and better UX

**Option B: AI Camp Schedule (7-8 weeks, core system)**
- Large, complex system
- Enables downstream lance features
- Long development but high value
- **Start here if:** You want to build foundation for lance systems

**Recommendation:** 
- **Solo dev:** Start with Camp Hub Phase 2 (quick win), then AI Schedule
- **Team:** Split - one on Camp Hub, one on AI Schedule

---

### Step 3: Set Up Development Branch

```powershell
# Create feature branch
git checkout -b feature/time-system-expansion

# After completing time system
git commit -m "feat: expand time system to 6 periods (Dawn/Morning/Afternoon/Evening/Dusk/Night)"
git push -u origin feature/time-system-expansion

# Create next feature branch
git checkout -b feature/camp-hub-phase2
# OR
git checkout -b feature/ai-camp-schedule
```

---

## FAQ

**Q: Can I skip the time system expansion?**  
A: No. It's a 1-2 hour task that unblocks 4+ major systems. Do it first.

**Q: Can I build Camp Hub and AI Schedule in parallel?**  
A: Yes! They're independent systems. Perfect for parallel development.

**Q: Do I need Persistent Lance Leaders for MVP?**  
A: No. Generic leaders work fine. This is an enhancement.

**Q: How many duty events do I really need?**  
A: Minimum 50 (5 per duty), comfortable with 100, great with 150+.

**Q: Can I build AI Lord Lance Simulation first?**  
A: You can, but it's low priority. Focus on player-facing systems first.

**Q: What if AI Camp Schedule takes longer than 8 weeks?**  
A: Phase 0-4 are critical (6 weeks). Phase 5-7 can be deferred if needed.

---

## Version History

**v1.0** (December 14, 2025)
- Initial roadmap based on alignment review
- All implementation plans analyzed
- Dependencies mapped
- Critical path identified
- Time system expansion marked as blocker

---

---

## Complete System Summary

### All 11 Implementation Plans Overview

| # | System | Duration | Priority | Dependencies | Key Features |
|---|--------|----------|----------|--------------|--------------|
| 1 | Menu Overhaul | - | ✅ Done | None | Clean menu structure |
| 2 | **Time System** | **1-2 hrs** | **🔴 P0** | **None** | **6 periods - BLOCKS ALL** |
| 3 | Camp Hub Phase 2 | 1 wk | 🔴 P1 | Time system | 6 location navigation |
| 4 | AI Camp Schedule | 7-8 wks | 🔴 P1 | Time system | Daily duty AI, T5-T6 command |
| 5 | Lance Life Simulation | 12 wks | 🔴 P1 | AI Schedule | Member injury/death/cover |
| 6 | Persistent Leaders | 10 wks | 🟡 P2 | Lance Life | Leader memory/personality |
| 7 | **Army Lance Activity** | **7-8 wks** | **🟡 P2** | **AI+Lance+News** | **NPC sim + REAL casualties** |
| 8 | Duty Events | Ongoing | 🟡 P2 | Duties exist | 150-200 event content |
| 9 | Camp Hub Phase 3-4 | 3-5 wks | 🟢 P3 | Phase 2 | Themed areas + war room |
| 10 | AI Lord Lance Sim | 10 wks | 🟢 P3 | None | Enemy army lances |
| 11 | Integration Guides | - | 📖 Ref | - | Technical specs |

---

## Army Lance Activity: Complete Feature Breakdown

### What It Simulates

**Routine Military Operations (The Small Stuff):**
- ✅ Patrols encountering bandits/scouts (5-15 enemies, small skirmishes)
- ✅ Foraging missions ambushed by looters (5-20 enemies)
- ✅ Scouting parties spotted by enemy patrols (brief contacts)
- ✅ Guard duty incidents (infiltrators, deserters, accidents)
- ✅ Tax collection resistance (villagers, bandits)
- ✅ Training accidents (minor injuries)
- ✅ Disease outbreaks in camp
- ✅ Desertion and discipline issues

**What It Does NOT Simulate:**
- ❌ Full battles (Bannerlord handles normally)
- ❌ Large-scale army combat (real battles only)
- ❌ Player-fought encounters

### The Casualty System (NEW)

**Casualties Are REAL:**
- When patrol gets ambushed → actual troops removed from party roster
- 40% killed (permanent), 60% wounded (recover in 7 days default)
- Troops selected by tier (match lance tier ±1)
- Heroes NEVER affected by simulation
- Clear news attribution ("patrol ambush" vs "battle")

**Example:**
```
Starting Army: 547 troops

Week 1 Operations:
- Day 2: Patrol ambush → 2 killed, 3 wounded (545 active, 3 recovering)
- Day 4: Foraging incident → 1 killed, 2 wounded (544 active, 5 recovering)
- Day 6: Guard accident → 0 killed, 1 wounded (544 active, 6 recovering)

Week 2: Real Battle Happens
- Available Forces: 544 troops (3 killed permanently, 6 wounded unavailable)
- Army 3% weaker than start due to routine operations
- Wounded recover over next week (most losses temporary)

Historical Accuracy: Armies lost 60-80% of casualties to disease, 
accidents, desertion, and small skirmishes. Only 20-40% to battles.
This simulates that reality.
```

### Configuration Presets Explained

**Casual (Narrative Only):**
```json
{
  "affect_actual_troops": false  // Disabled, no mechanical impact
}
```
- Events happen, news reports them
- NO actual casualties applied
- Pure storytelling/immersion
- For players who don't want attrition mechanics

**Normal (Light Attrition - RECOMMENDED DEFAULT):**
```json
{
  "affect_actual_troops": true,
  "base_casualty_multiplier": 0.3,     // 30% of battle rates
  "wounded_recovery_days": 7,
  "apply_to_enemy_armies": false,      // Player only
  "wounded_vs_killed_ratio": 0.7       // 70% wounded, 30% killed
}
```
- 5-8% monthly attrition
- Most casualties recoverable
- Player army only (balance)
- Not punishing, adds strategic layer

**Realistic (Historical Simulation):**
```json
{
  "affect_actual_troops": true,
  "base_casualty_multiplier": 0.5,     // 50% of battle rates
  "wounded_recovery_days": 7,
  "apply_to_enemy_armies": true,       // Both armies
  "wounded_vs_killed_ratio": 0.6       // 60% wounded, 40% killed
}
```
- 10-15% monthly attrition
- Both armies affected (enemy weakens too)
- Historical accuracy
- Requires active army management

**Hardcore (Challenge Mode):**
```json
{
  "affect_actual_troops": true,
  "base_casualty_multiplier": 0.7,     // 70% of battle rates
  "wounded_recovery_days": 10,         // Slower recovery
  "apply_to_enemy_armies": true,
  "wounded_vs_killed_ratio": 0.5       // 50/50 wounded/killed
}
```
- 15-25% monthly attrition
- Both armies grind down
- More permanent casualties
- Difficult campaign management

### Implementation Details

**Week 22 (Phase 2): Casualty Application Code**

Core implementation:
```csharp
private void ApplyCasualtiestoParty(MobileParty party, int casualties, SimulatedLance lance)
{
    if (!Config.AffectActualTroops) return;
    
    // Calculate killed vs wounded
    int killed = (int)(casualties * (1 - Config.WoundedVsKilledRatio));
    int wounded = casualties - killed;
    
    // Get eligible troops (match lance tier, exclude heroes)
    var eligible = party.MemberRoster
        .Where(t => !t.Character.IsHero)
        .Where(t => Math.Abs(t.Character.Tier - lance.AverageTier) <= 1);
    
    // Apply killed
    for (int i = 0; i < killed; i++)
        RemoveTroopPermanently(party, eligible.GetRandomElement());
    
    // Apply wounded (move to wounded status)
    for (int i = 0; i < wounded; i++)
        MoveToWounded(party, eligible.GetRandomElement(), Config.WoundedRecoveryDays);
}

private void ProcessWoundedRecovery()
{
    // Daily tick: check all wounded troops
    // If recovery time elapsed, move back to active roster
    // Uses same system as post-battle wounded recovery
}
```

**Week 27 (Phase 6 Extended): Balance Testing**

Required playtests:
1. 30-day Casual (disabled) - verify narrative works, 0% attrition
2. 30-day Normal (0.3) - target 5-8% attrition, feels balanced
3. 30-day Realistic (0.5, both armies) - target 10-15%, challenging but fair
4. 30-day Hardcore (0.7, both armies) - target 15-25%, very challenging

Data to collect:
- Total casualties from simulation vs battles
- Wounded recovery rate
- Army strength over time
- AI recruitment rates
- Player sentiment (too easy, balanced, too hard)

Adjustments based on data:
- Tune multipliers per preset
- Adjust per-activity danger levels
- Modify wounded vs killed ratios
- AI recruitment boost if needed

### Risk Assessment (Casualty System)

**🟡 Medium Risks:**

1. **Balance Difficulty**
   - Risk: Too punishing or too weak
   - Mitigation: Week 27 dedicated testing, configurable
   
2. **Player Frustration**
   - Risk: "Lost troops without fighting?"
   - Mitigation: Clear news, education, optional

3. **AI Behavior**
   - Risk: AI doesn't recruit enough
   - Mitigation: Monitor, possible AI boost, player-only default

**🟢 Low Risks:**

1. **Technical Implementation**
   - Uses existing Bannerlord systems
   - Well-documented patterns
   - Save/load straightforward

2. **Performance**
   - Daily tick processing only
   - 8-15 lances per army max
   - Minimal overhead

**Overall Risk:** 🟡 Medium (manageable with testing)

---

## Final Build Order (Complete)

**The Full 27-Week Journey:**

```
Week 0:   🔴 Time System (1-2 hours) ← START HERE
Week 1:   🔷 Camp Hub Phase 2 AND/OR 🔷 AI Schedule Phase 0-1
Weeks 2-8: 🔷 AI Camp Schedule (Phases 2-7)
Weeks 9-20: 🔶 Lance Life Simulation
Weeks 15-30: 🔶 Persistent Lance Leaders (overlaps with Lance Life)
Weeks 20-27: 🟡 Army Lance Activity (with casualty system)
Throughout: 📝 Duty Events content (150-200 events)
```

**MVP (12-16 weeks):** Stop after Lance Life Phase 4  
**Enhanced (27 weeks):** Include Army Lance Activity  
**Full (30+ weeks):** Add optional features (AI Lord Lance Sim, Camp Phase 3-4)

---

**Document Version:** 2.0 - Updated with Army Lance Activity Casualty System  
**Review Date:** December 14, 2025  
**Last Updated:** December 14, 2025  
**Next Review:** After time system expansion complete  
**Status:** 📋 Complete Implementation Guide  
**Maintained By:** Enlisted Development Team
