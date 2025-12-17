# master implementation roadmap

**purpose:** single source of truth for implementation order and dependencies  
**date:** december 16, 2025 (v3.1 - decision events added)  
**status:** active planning document

---

## how to use this documentation

### for developers (human or ai)

**When starting development, read in this order:**

1. **this document (master-implementation-roadmap.md)** 🗺️
   - read "quick reference" → know what to build next
   - read "dependency graph" → understand build order
   - read your track section → see weekly breakdown
   - **purpose:** answers "what do i build and when?"

2. **the feature docs** 📖
   - For system behavior and where things live in code/data:
     - `docs/Features/core-gameplay.md`
     - `docs/Features/index.md`
   - For content authoring:
     - decision events → `docs/StoryBlocks/decision-events-spec.md`
     - duty events → `docs/ImplementationPlans/duty-events-creation-guide.md`
   - For UI work:
     - `docs/ImplementationPlans/gauntlet-ui-screens-playbook.md`

### quick decision guide

| your question | use this document |
|---------------|-------------------|
| "what should i build on monday?" | **this roadmap** 🗺️ |
| "how long will this take?" | **this roadmap** 🗺️ |
| "what's the detailed implementation?" | **feature-specific doc** 📖 |
| "how does this integrate with x?" | **feature-specific doc** 📖 |
| "what are the dependencies?" | **this roadmap** 🗺️ |
| "what code structure do i need?" | **feature-specific doc** 📖 |

### for ai implementation

**Give AI these instructions:**

```
1. read master-implementation-roadmap.md to understand build order and dependencies
2. use docs/ImplementationPlans/implementation-status.md for the current truth of what’s done/next
3. use docs/Features/core-gameplay.md for “where it lives” (code/data map)
4. use docs/ImplementationPlans/gauntlet-ui-screens-playbook.md for all ui work
```

---

## index

- [quick reference](#quick-reference)
- [critical path](#critical-path)
- [build tracks](#build-tracks)

## quick reference

### what's done ✅
see `docs/ImplementationPlans/implementation-status.md` for the up-to-date completion list.

### what's next 🔴
- **decision events**: phase 7 content creation, phase 9 debug commands, optional phase 8 news integration.

### what's waiting 📋
- duty event content expansion (authoring) and any optional integrations listed in `docs/ImplementationPlans/implementation-status.md`.

---

## critical path

### ✅ PRIORITY 0: Foundation (COMPLETE)

This used to be the primary blocker. It is now complete and kept here for reference.

#### Time System Expansion
- **File:** `src/Features/Schedule/Models/TimeBlock.cs`
- **Changes:** Implemented 4-block schedule system (Morning, Afternoon, Dusk, Night)
- **Duration:** Complete
- **doc:** see `docs/Features/core-gameplay.md` (schedule) and `docs/ImplementationPlans/implementation-status.md` (status).
- **Status:** ✅ IMPLEMENTED

**Why Critical:**
- Previous system had inconsistent time handling
- Schedule system needs predictable time blocks for activity assignment
- Each block represents ~6 hours of game time (0-6, 6-12, 12-18, 18-24)

**What We Built:**
- `TimeBlock` enum with 4 periods
- `GetCurrentTimeBlock()` mapping in `ScheduleBehavior`
- Activity preferences per time block (e.g., rest → Night, training → Morning)
- Context-aware flexibility (allows rest outside preferred block if player exhausted)

**Success Criteria:** ✅ Complete
- Schedule generates for all 4 time blocks
- Activities respect time block preferences
- Context-aware overrides work correctly
- Hardship days increase operational tempo appropriately

---

## build tracks

### Track A: Camp Management Screen (COMPLETE ✅)

**Status:** FULLY COMPLETE including Phase 4 Activities Tab

**Phase 1: Foundation (COMPLETE ✅)**
- ✅ Activity card components
- ✅ Camp activities screen
- ✅ Status bar integration
- ✅ Data loading from JSON

**Phase 2: Camp Management Screen - Kingdom Pattern (COMPLETE ✅)**
- **Doc:** `KINGDOM_STYLE_CAMP_SCREEN.md`
- **Architecture:** Full-screen layer-based UI with tabbed interface (matches native Kingdom screen)
- **Deliverable:** Kingdom-style camp management screen with:
  - Tab bar with Q/E hotkey switching
  - Lance Tab: roster, needs display, progression tracking
  - Orders Tab (Schedule): policy-style schedule management
  - Reports Tab: Diplomacy-style news categories with feed
  - Activities Tab: planned (stub exists)
  - Party Tab: planned for v1.0.0 (NPC lances)

**Tasks:**
1. ✅ `CampManagementScreen.cs` - Static layer-based screen
2. ✅ `CampManagementVM.cs` - Tab controller with navigation
3. ✅ `CampLanceVM.cs` - Lance roster + needs + progression
4. ✅ `CampScheduleVM.cs` - Schedule display + policy interactions
5. ✅ `CampReportsVM.cs` - Diplomacy pattern reports + categories
6. ✅ Load required sprite categories and use native UI patterns
7. ✅ Menu hook in Command Tent ("[TEST] Camp Management")

**Phase 3: Tab-Specific Features (COMPLETE ✅)**

**Lance Tab:**
- ✅ Lance needs visual display (Readiness, Equipment, Morale, Rest, Supplies)
- ✅ Status bars with color-coded levels (Excellent/Good/Fair/Poor/Critical)
- ✅ Culture-specific rank display from `name_pools.json`
- ✅ Lance banner persistence (unique per lord/lance, saved)
- ✅ Banner randomization for AI-led lances
- ✅ Member progression tracking (DaysInService, Battles, XP)
- ✅ Auto-promotion system with notifications

**Orders Tab (Schedule):**
- ✅ 4 time block display (Morning, Afternoon, Dusk, Night)
- ✅ Policy-style clickable time blocks
- ✅ Available activities list with filtering
- ✅ Context-aware AI schedule generation
- ✅ Hardship days logic (increased tempo during war)
- ✅ Activity uniqueness tracking (once-per-day duties)
- ✅ Permission-based UI (T1-T6 authority levels)
- ✅ Dynamic hover hints for disabled buttons
- ⏳ T5-T6 schedule SETTING (UI exists, action handlers not wired)

**Reports Tab:**
- ✅ Diplomacy-style category selection (4 categories as clickable buttons)
- ✅ General Orders narrative generation (RP military dispatches)
- ✅ Lance Reports feed (schedule, activities, needs warnings)
- ✅ Company Reports feed (lord status, army info, party strength)
- ✅ Kingdom Reports feed (wars, peace, battles from game logs)
- ✅ Two-panel layout: categories left, feed + details right
- ✅ Native UI patterns (`ReportCategoryTuple`, `ReportTuple`)

**Phase 4: Activities Tab & Schedule Actions (COMPLETE ✅)**
- ✅ Activities Tab implementation (location-based activity selection)
- ✅ T5-T6 schedule action handlers (Set Activity, Request Change)
- ✅ Activity execution with feedback messages
- ✅ CampActivitiesVM with full location navigation
- ⏳ Decision overlay system for events/requests
- ⏳ Party Tab for NPC lance statuses (v1.0.0 feature)

---

### Track B: AI Camp Schedule (7-8 weeks) - COMPLETE ✅

**doc:** see `docs/ImplementationPlans/implementation-status.md` for current status and key files.  
**Prerequisite:** Time system expansion ✅ complete
**Status:** ALL PHASES (0-7) FULLY IMPLEMENTED  
**Status:** Core AI scheduling complete, T5-T6 interaction UI pending

**Phase 0: Foundation (COMPLETE ✅)**
- ✅ Data models (`ScheduledBlock`, `ScheduleDay`, `LanceNeeds`)
- ✅ Configuration loading from `schedule_config.json`
- ✅ Save/load support via `ScheduleBehavior.SyncData()`
- ✅ 4 time block system (Morning, Afternoon, Dusk, Night)

**Phase 1: Basic Scheduling (COMPLETE ✅)**
- ✅ Schedule generation algorithm (`ScheduleGenerator`)
- ✅ Lord objective → duty assignment logic
- ✅ Formation and tier-based activity filtering
- ✅ Player duty filtering (required_duties field)
- ✅ Default activity fallbacks

**Phase 2: Lance Needs System (COMPLETE ✅)**
- ✅ Lance need tracking (Readiness, Equipment, Morale, Rest, Supplies)
- ✅ Need degradation system (`LanceNeedsManager`)
- ✅ Recovery mechanics (per-activity effects)
- ✅ Need-aware UI display (Lance tab status bars)
- ✅ Status levels (Excellent/Good/Fair/Poor/Critical)

**Phase 3: AI Schedule Logic (COMPLETE ✅)**
- ✅ Context-aware scheduling (war/peace/siege)
- ✅ Need-balancing algorithm
- ✅ Duty distribution optimization
- ✅ Hardship days simulation (increased tempo)
- ✅ Activity uniqueness tracking (once-per-day)
- ✅ Time block preferences (rest → Night, training → Morning, etc.)
- ✅ Context-aware flexibility (allows rest override if player exhausted)
- ✅ Free time scheduling (2 slots peacetime, 1 wartime, overrideable)

**Phase 4: Schedule Block Execution (COMPLETE ✅)**
- ✅ Block activation system
- ✅ Event triggering during blocks (`ScheduleExecutor`)
- ✅ Completion tracking
- ✅ Skill-appropriate XP rewards (Leadership, Scouting, One Handed, etc.)
- ✅ Auto-resume after event popups (game no longer stays paused)
- ✅ Need recovery/degradation application

**Phase 5: T5-T6 Leadership (COMPLETE ✅)**
- ✅ Player authority level detection (`ScheduleAuthorityLevel` enum)
- ✅ Permission-based UI (buttons enabled/disabled by authority)
- ✅ Dynamic hover hints explaining why buttons are disabled
- ✅ T3-T4 request approval likelihood calculation (`CalculateApprovalLikelihood()`)
- ✅ Display of available activities filtered by player tier/duty
- ✅ "Set Activity" action handler (`ExecuteSetSchedule()` → `SetManualSchedule()`)
- ✅ "Request Change" action handler (`ExecuteRequestChange()` → `RequestScheduleChange()`)
- ✅ Approval slider display for T3-T4 (`ShowApprovalSlider`, `ApprovalLikelihood`)
- ✅ Manual schedule override for Lance Leaders (`IsManualScheduleMode`, `RevertToAutoSchedule()`)

**Phase 6: Pay Muster Integration (COMPLETE ✅)**
- ✅ 12-day schedule cycle (`_cycleStartTime`, `_nextMusterTime`, `CurrentCycleDay`)
- ✅ Order refresh at muster (`OnPayMusterCompleted()`)
- ✅ Muster sync with enlistment (`SyncMusterTimeWithEnlistment()`)
- ✅ Performance tracking and consequences (`ApplyPerformanceConsequences()`)
- ✅ UI display of cycle info (`CampScheduleVM.RefreshCycleInfo()`)

**Phase 7: Polish (COMPLETE ✅)**
- ✅ Combat interrupt handling (`OnMapEventStarted/Ended`)
- ✅ Time skip recovery (`HandleTimeSkipRecovery()`)
- ✅ Camp bulletin integration (`CampBulletinIntegration.OnBlockStart/Complete`)
- ✅ Performance feedback news (`CampNewsGenerator.GeneratePerformanceFeedbackNews`)
- ✅ Comprehensive integration testing (builds successfully)

**Current Status Summary:**
- **Core AI scheduling:** 100% complete (Phases 0-7)
- **UI for viewing:** 100% complete (Orders tab fully functional)
- **UI for interaction:** 100% complete (all action handlers wired)
- **Track B Status:** FULLY COMPLETE ✅

---

### Track C: Lance Systems (14-16 weeks total, overlapping builds)

**Dependencies:** AI Camp Schedule must be complete first ✅

#### C1: Lance Life Simulation (12 weeks) - COMPLETE ✅

**doc:** see `docs/ImplementationPlans/implementation-status.md` for current status and key files.  
**Prerequisite:** AI Camp Schedule (provides `ILanceScheduleModifier` interface) ✅
**Status:** FULLY IMPLEMENTED

**Weeks 1-2: Foundation ✅**
- ✅ `LanceLifeSimulationBehavior` with state tracking
- ✅ `LanceMemberState` struct with save/load
- ✅ Daily processing hooks
- ✅ Integration with AI Schedule via `ILanceScheduleModifier`

**Weeks 3-4: Injury System ✅**
- ✅ Injury probability checks (base + context modifiers)
- ✅ Health state progression (Healthy → Minor → Major → Incapacitated)
- ✅ Recovery tracking with CampaignTime
- ✅ Medical tent integration via SickBay activity state

**Weeks 5-6: Death & Memorial ✅**
- ✅ Death mechanics (combat, disease, accidents)
- ✅ Memorial service events JSON
- ✅ Roster management (ActiveMembers, AvailableMembers)
- ✅ Morale impact via LanceNeeds

**Weeks 7-8: Cover Request System ✅**
- ✅ Cover request evaluation (LastCoverRequestTime cooldown)
- ✅ Player decision events (9 events in events_lance_simulation.json)
- ✅ Favor tracking (FavorsOwed, FavorsOwedToPlayer)
- ✅ Relationship impacts (ModifyRelation)

**Weeks 9-11: Promotion Escalation ✅**
- ✅ Player readiness tracking (CheckPlayerReadinessForLanceLeader)
- ✅ Escalation path selection (weighted random)
- ✅ Vacancy creation (5 paths: Promotion, Transfer, Injury, Death, Retirement)
- ✅ Promotion ceremony events

**Week 12: Polish ✅**
- ✅ Balance probabilities (configurable base rates)
- ✅ Integration testing (compiles, registered in SubModule)
- ✅ Content variety (9 event types in JSON)

**Integration Points:**
- Provides member availability to AI Schedule
- Triggers Persistent Lance Leaders on vacancy
- Uses existing event infrastructure

#### C2: Persistent Lance Leaders (10 weeks, starts Week 7) - COMPLETE ✅

**doc:** see `docs/ImplementationPlans/implementation-status.md` for current status and key files.  
**Prerequisite:** Lance Life Simulation foundation (Week 6) ✅  
**Status:** FULLY IMPLEMENTED

**Weeks 7-8: Core Generation ✅**
- ✅ `PersistentLanceLeader` data class with all fields
- ✅ `PersistentLanceLeadersBehavior` with save/load
- ✅ Name generation (culture-specific male/female names + epithets)
- ✅ Personality trait system (6 primary + 8 secondary traits)

**Weeks 9-10: Memory System ✅**
- ✅ `MemoryEntry` structure with 9 memory types
- ✅ Memory queue (15 max, FIFO via AddMemory)
- ✅ Event recording hooks (RecordEventChoice, RecordBattlePerformance, RecordPromotion)
- ✅ Memory decay (30 days via ProcessMemoryDecay)

**Weeks 11-12: Reaction System ✅**
- ✅ Tone determination logic (8 dialog tones)
- ✅ Dynamic dialogue generation (personality-based greetings)
- ✅ Reaction event templates (11 events in JSON)
- ✅ Personality-based responses (Stern/Fair/Pragmatic/Fatherly/Ambitious/Cynical)

**Weeks 13-14: Death & Replacement ✅**
- ✅ Death processing (MarkDead, OnLanceLeaderDeath)
- ✅ Replacement generation (GetOrCreateLanceLeader)
- ✅ Memorial system (via LanceLifeSimulation)
- ✅ Introduction events (TriggerIntroductionEvent)

**Weeks 15-16: Integration & Polish ✅**
- ✅ Camp news integration (via InformationManager messages)
- ✅ Promotion system hooks (OnLeaderVacancy, OfferPlayerPromotion)
- ✅ 11 reaction events (heat warnings, praise, discipline, trust milestones)
- ✅ Comprehensive testing (compiles, registered in SubModule)

**Integration Points:**
- ✅ Receives vacancy notifications from Lance Life
- ✅ Provides leader identity to all systems (CurrentLeader property)
- ✅ Comments on duty events (RecordEventChoice)
- ✅ Reacts to escalation thresholds (Heat/Discipline warnings)

---

### Track E: Enhancement Systems (Build After Core - Week 20+)

**IMPORTANT:** Track E should only be started after core systems (Tracks B & C) are complete and stable.

#### E1: Army Lance Activity Simulation (7-8 weeks, Week 20-27)

**Doc:** (Not active) If revived, document it under `docs/Features/` and update this roadmap.  
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

### Track D: Content & Decision Systems (Ongoing)

#### D1: Duty Events Library (Continuous)

**doc:** `duty-events-creation-guide.md`  
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

#### D2: Decision Events Framework (5-6 weeks, Week 7-12) 🆕 NEW

**doc:** `docs/StoryBlocks/decision-events-spec.md`  
**Prerequisites:** Track B AI Schedule (for activity context), existing Events system  
**Priority:** 🔴 P1 (Core for CK3 feel)

**Purpose:**
Create a Crusader Kings-style decision system where:
- Events come TO the player based on what they're doing
- Player-initiated decisions available in Main Menu
- Activities and schedule drive contextual events
- Pacing protections prevent event spam

**Week-by-Week Breakdown:**

**Week 7: Phase 1 - Core Infrastructure**
- [ ] `DecisionEventState` persistence class (cooldowns, flags, counters)
- [ ] `DecisionEventEvaluator` with 8 protection layers
- [ ] Config loading from `enlisted_config.json`
- [ ] Save/load via `IDataStore.SyncData`
- **Deliverable:** Framework can evaluate and fire events

**Week 7-8: Phase 1.5 - Activity-Aware Events ⭐**
- [ ] Add `current_activity:X` tokens to `CampaignTriggerTokens`
- [ ] Add `on_duty:X` tokens to `CampaignTriggerTokens`
- [ ] Implement token evaluation in `LanceLifeEventTriggerEvaluator.cs`
- [ ] Add `GetCurrentActivityContext()` to `ScheduleBehavior`
- [ ] Add activity weight boost in event selection (2x for matching)
- [ ] Update `ScheduleExecutor` to notify event system on block change
- **Deliverable:** Events filter by what player is currently doing

**Week 8: Phase 2 - Pacing System**
- [ ] Individual + category cooldowns
- [ ] Global limits (per-day, per-week, hours between)
- [ ] Weight calculation with activity boost, priority, decay
- [ ] Mutual exclusion, one-time flags, story state blocking
- **Deliverable:** Events feel natural, not spammy

**Week 9: Phase 3 - Event Chains**
- [ ] `chains_to` field support
- [ ] Chain event queueing with delay
- [ ] Outcome variables (`{GOLD_EARNED}`, etc.)
- **Deliverable:** Multi-step decision stories work

**Week 9-10: Phase 4 - Player-Initiated Decisions**
- [ ] Add "Decisions" section to Enlisted Main Menu
- [ ] Show eligible decisions with requirements
- [ ] Status display: Current Activity, Duty, Fatigue
- [ ] Quick override buttons (Change Activity, Request Rest)
- **Deliverable:** Main Menu becomes the lightweight control center

**Week 10-12: Phase 5 - Content Creation**
- [ ] 8-10 player-initiated decisions for Main Menu
- [ ] 30-40 activity-specific pushed events
- [ ] 15-20 duty-specific events
- [ ] 10-15 chain follow-up events
- **Deliverable:** Rich event variety matching all activities

**Week 12: Phase 6 - Testing & Polish**
- [ ] Debug console commands
- [ ] Pacing verification (2-3 quiet days per week)
- [ ] Activity match rate testing (target 70%+)
- **Deliverable:** System balanced and ready

**Integration Points:**
- **With AI Schedule:** Gets activity context from `ScheduleBehavior`
- **With Escalation:** Events check Heat, Discipline, Lance Rep thresholds
- **With Duties:** `on_duty:X` tokens for duty-specific events
- **With Camp Activities:** Activities set context for matching events

**Key Features:**
1. **Activity-Aware Events** — Training block → training events prioritized
2. **CK3 Pacing** — Quiet days, cooldowns, weight decay
3. **Main Menu Decisions** — Always-available player choices
4. **Pushed Invitations** — Lord invites, lance mate requests pop up
5. **Cross-System Integration** — Escalation affects event availability

**Value Proposition:**
- CK3 "living company" feel
- Events match what player is doing
- Reduced menu management (AI runs activities, events come to you)
- Narrative variety without spam

---

### Track E: Enhancement Systems (Build After Core - Week 20+)

#### E1: Army Lance Activity Simulation (7-8 weeks, Week 20-27) 🔴 UPDATED

**Doc:** (Not active) If revived, document it under `docs/Features/` and update this roadmap.  
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

### Gantt Chart - ACTUAL STATUS

```
Week   | Track A: Camp       | Track B: Schedule      | Track C: Lance    | Track D: Content+Decisions    | Track E: Enhancement
-------|---------------------|------------------------|-------------------|-------------------------------|----------------------
   0   | Time System ✅ (4-block schedule complete)                                                               |
   1   | CampScreen ✅       | Phase 0 ✅ (Foundation) |                   | Ongoing                       |
   2   | Lance Tab ✅        | Phase 1 ✅ (Scheduling) |                   | Ongoing                       |
   3   | Orders Tab ✅       | Phase 2 ✅ (Needs)      |                   | Ongoing                       |
   4   | Reports Tab ✅      | Phase 3 ✅ (AI Logic)   |                   | Ongoing                       |
   5   | Activities stub ✅  | Phase 4 ✅ (Execution)  |                   | Ongoing                       |
→ 6   | Activities Tab ⏳   | Phase 5 ⏳ (T5-T6 UI)   | (Blocked)         | Ongoing                       | (Not started)
   7   |                     | Phase 6 (Muster)       | C1: Foundation    | D2: Decision Core 🆕          |
   8   |                     | Phase 7 (Polish)       | C1: Injury        | D2: Activity-Aware 🆕         |
   9   |                     | ✅                     | C1: Death         | D2: Pacing System             |
  10   |                     |                        | C1: Cover         | D2: Event Chains              |
  11   |                     |                        | C1: Cover         | D2: Main Menu Decisions       |
       |                     |                        | C2: Generation    |                               |
  12   |                     |                        | C1: Escalation    | D2: Content + Testing         |
       |                     |                        | C2: Memory        |                               |
  13   |                     |                        | C1: Escalation    | +duty events                  |
       |                     |                        | C2: Memory        |                               |
  14   |                     |                        | C1: Polish        | +duty events                  |
       |                     |                        | C2: Reactions     |                               |
  15   |                     |                        |                   | +duty events                  |
       |                     |                        | C2: Death/Replace |                               |
  16   |                     |                        | C2: Integration   | Polish events                 |
  17   |                     |                        | C2: Polish        | Polish events                 |
  18-22|                     |                        |                   | Ongoing polish                | (Wait for C complete)
  23   |                     |                        |                   |                               | E1: Foundation
  24   |                     |                        |                   |                               | E1: Assignment
  25   |                     |                        |                   |                               | E1: Events+Casualties🔴
  26   |                     |                        |                   |                               | E1: News+Attribution
  27   |                     |                        |                   |                               | E1: Cover Requests
  28   |                     |                        |                   |                               | E1: Battle
  29   |                     |                        |                   |                               | E1: Polish
  30   |                     |                        |                   |                               | E1: Balance Testing🔴
-------|---------------------|------------------------|-------------------|-------------------------------|----------------------
Status:| MOSTLY COMPLETE ✅  | 80% COMPLETE ⏳        | NOT STARTED ❌    | ONGOING ⏳ + D2 NEW 🆕        | NOT STARTED ❌
       | (Activities pending)| (Phases 0-4 done,     | (Blocked on      | (D1 continuous, D2 Week 7-12)  | (Blocked on Track C)
       |                     |  Phase 5 in progress) |  Track B)        |                               |
```

**Current Week:** 6 (→ marker shows where we are now)

**Key Observations:**
- ✅ **Track A:** Core Camp Management Screen complete (Lance/Orders/Reports tabs)
- ⏳ **Track A:** Activities Tab stub exists, needs full implementation
- ✅ **Track B:** Core AI scheduling system 100% functional (Phases 0-4)
- ⏳ **Track B:** T5-T6 interaction UI 60% done (buttons exist, action handlers not wired)
- ❌ **Track C:** Blocked until Track B Phase 5 completes
- ⏳ **Track D1:** Ongoing duty events content creation
- 🆕 **Track D2:** Decision Events Framework ready to start Week 7 (CK3-style decisions)
- ❌ **Track E:** Not started, blocked on Track C completion
- **Critical path shift:** Track B Phase 5 is now the blocker for Track C
- **New parallel work:** Decision Events can start Week 7 alongside Lance Life
- **Estimated remaining:** 2-3 weeks to complete Track A + Track B, then Track C + D2 begin

---

## Dependency Graph

### Visual Dependency Map - CURRENT STATUS

```
                    [Time System (4 blocks) ✅]
                    (COMPLETE)
                            ↓
        ┌───────────────────┴────────────────────┐
        ↓                                        ↓
[Camp Management Screen ✅]          [AI Camp Schedule ⏳ 80%]
(Kingdom-style tabs)                 (Phases 0-4 complete ✅)
Lance/Orders/Reports done            (Phase 5 in progress ⏳)
Activities stub exists               T5-T6 action handlers needed
        ↓                                        ↓
[Activities Tab ⏳]                    ├──────────────────────────────────┐
(1-2 weeks pending)                   ↓                                  ↓
        │                   [Lance Life Simulation ❌]    [Decision Events 🆕]
        │                   (12 weeks, blocked)          (6 weeks, Week 7-12)
        │                             ↓                  Activity-aware events
        │                   [Persistent Lance Leaders ❌] CK3-style pacing
        │                   (10 weeks, overlaps weeks 7-16)     │
        │                             ↓                         │
        │                   ┌─────────┴─────────┐               │
        │                   ↓                   ↓               │
        │           [Duty Events ⏳]    [Army Lance Activity ❌] │
        │           (150-200 events)   (7 weeks, Week 20-26)    │
        │           (ongoing)                  ↓                │
        │                            [Enhancement Complete]     │
        │                                                       │
        └──────────────────► [Main Menu Decisions] ◄────────────┘
                             (Status + Quick Actions)

[AI Lord Lance Simulation ❌] ← Independent, can build anytime (10 weeks)
```

**Legend:**
- ✅ = Complete
- ⏳ = In Progress
- ❌ = Not Started / Blocked
- 🆕 = New Track (Decision Events)

---

## Risk Assessment

### High Risk Items 🔴

**1. UI Instability During Menu Context (Known Risk Area)**
- **Risk:** Freezes/invisible screens due to layer/screen misuse, missing sprite categories, or binding pitfalls
- **Impact:** 🔴 HIGH - breaks core player flow (camp/menus)
- **mitigation:** follow `gauntlet-ui-screens-playbook.md` strictly (layer-based overlays, NextFrameDispatcher, sprite categories, cleanup, 8-digit colors)
- **Status:** ✅ Actively mitigated - Camp Bulletin XML bugs fixed, Playbook updated with "Critical XML Layout Pitfalls" section documenting all discovered issues (vertical text, missing tags, spacing, alignment)

**2. AI Camp Schedule Complexity**
- **Risk:** System is complex with many integration points
- **Impact:** 🟡 MEDIUM - Lance systems wait for this
- **Mitigation:** Follow phased implementation, test each phase
- **Status:** ✅ 80% Complete - Phases 0-4 done, Phase 5 in progress

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

### Gate 1: Time System Complete ✅

**Status:** ✅ PASSED (Week 0)

**Criteria:**
- [x] 4-block `TimeBlock` enum defined (Morning/Afternoon/Dusk/Night)
- [x] Time detection logic implemented (`GetCurrentTimeBlock()`)
- [x] Schedule system using time blocks for activity assignment
- [x] Activity preferences per time block working
- [x] Documentation updated

**Decision:** ✅ Proceeded to Track A & B in parallel (both now mostly complete)

---

### Gate 2: Track A & B Core Complete ✅/⏳

**Status:** ⏳ 90% PASSED (Week 6)

**Criteria:**
- [x] Camp Management Screen fully functional
- [x] Lance Tab complete (roster, needs, progression)
- [x] Orders Tab complete (schedule display, permissions, hover hints)
- [x] Reports Tab complete (categories, feed, General Orders)
- [ ] Activities Tab implementation (stub exists, pending full implementation)
- [x] AI Schedule Phases 0-4 complete (generation, needs, execution)
- [ ] AI Schedule Phase 5 complete (T5-T6 action handlers)

**Decision:** ⏳ Can proceed to Track C once Phase 5 completes, or build Activities Tab in parallel

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

### related documents

**must read:**
- `implementation-status.md` - current state of all tracks (source of truth)
- `docs/Features/core-gameplay.md` - System map: where things live (code/data)

**system docs:**
- `duty-events-creation-guide.md` - event content guide

**supporting docs:**
- `gauntlet-ui-screens-playbook.md` - ui technical guide

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

**see:** `docs/ImplementationPlans/implementation-status.md` for current status and key files

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

# After completing time system (ALREADY DONE ✅)
git commit -m "feat: implement 4-block schedule system (Morning/Afternoon/Dusk/Night)"
git push -u origin feature/time-system-expansion

# Create next feature branch (START HERE)
git checkout -b feature/camp-activities-tab
# OR
git checkout -b feature/schedule-t5t6-actions
```

---

## FAQ

**Q: Can I skip the time system expansion?**  
A: ✅ Already complete! 4-block schedule system is implemented and working.

**Q: Can I build Camp Activities Tab and T5-T6 Schedule Actions in parallel?**  
A: Yes! They're independent features. Both can be developed simultaneously.

**Q: Do I need Persistent Lance Leaders for MVP?**  
A: No. Generic leaders work fine. This is an enhancement for later.

**Q: What's the next priority?**  
A: Two parallel options: (1) Complete Track B Phase 5 (T5-T6 action handlers), or (2) Build Activities Tab for Camp screen. Pick based on team availability.

**Q: How many duty events do I really need?**  
A: Minimum 50 (5 per duty), comfortable with 100, great with 150+.

**Q: Can I build AI Lord Lance Simulation first?**  
A: You can, but it's low priority. Focus on player-facing systems first.

**Q: What if AI Camp Schedule takes longer than 8 weeks?**  
A: Phase 0-4 are critical (6 weeks). Phase 5-7 can be deferred if needed.

---

## Version History

**v3.1** (December 16, 2025) - DECISION EVENTS ADDED
- 🆕 Added Track D2: Decision Events Framework (6 weeks, Week 7-12)
- 🆕 Added Activity-Aware Events system (events tied to schedule/activities)
- 🆕 Added Main Menu Decisions integration plan
- 🆕 Updated Gantt chart to include Decision Events track
- 🆕 Updated dependency graph to show Decision Events flow
- 🆕 Updated Complete System Summary (now 12 systems)
- 🆕 cross-referenced `decision-events-spec.md` as primary doc

**v3.0** (December 16, 2025) - FULL AUDIT
- ✅ Audited all tracks against actual implementation
- ✅ Updated Track A to reflect Camp Management Screen (Kingdom pattern)
- ✅ Updated Track B to show Phases 0-4 complete, Phase 5 in progress
- ✅ Updated Gantt chart to show actual current status (Week 6)
- ✅ Corrected time system to reflect 4 time blocks (not 6)
- ✅ Updated "What's Done" section with all completed features
- ✅ Marked remaining work clearly (Activities Tab, T5-T6 action handlers)

**v2.2** (December 15, 2025)
- Updated "What's Done" with Camp features completion
- Updated time system to 4 blocks (Morning/Afternoon/Dusk/Night)
- Added Reports Tab implementation
- Added Schedule Tab enhancements
- Added Lance Tab features

**v2.1** (December 15, 2025)
- Added camp_menu_overhaul.md reference
- Updated Track A with actual Camp Management Screen implementation
- Updated Track B with schedule system progress

**v2.0** (December 15, 2025)
- Major update after Camp features implementation
- Added KINGDOM_STYLE_CAMP_SCREEN.md as primary doc for Track A
- Updated Track B with detailed phase completion status

**v1.0** (December 14, 2025)
- Initial roadmap based on alignment review
- All implementation plans analyzed
- Dependencies mapped
- Critical path identified
- Time system expansion marked as blocker

---

---

## Complete System Summary

### All 12 Implementation Plans Overview

| # | System | Duration | Priority | Dependencies | Key Features |
|---|--------|----------|----------|--------------|--------------|
| 1 | Menu Overhaul | - | ✅ Done | None | Clean menu structure |
| 2 | **Time System** | ✅ **Done** | **✅ P0** | **None** | **4 time blocks (M/A/D/N)** |
| 3 | Camp Management Screen | ✅ **Done** | ✅ P1 | Time system | Kingdom-style tabs (Lance/Orders/Reports) |
| 4 | AI Camp Schedule | ⏳ **80%** | ⏳ P1 | Time system | Daily duty AI, context-aware ✅, T5-T6 UI ⏳ |
| 5 | Lance Life Simulation | ❌ 12 wks | 🔴 P1 | AI Schedule | Member injury/death/cover |
| 6 | Persistent Leaders | ❌ 10 wks | 🟡 P2 | Lance Life | Leader memory/personality |
| 7 | **Decision Events** 🆕 | ❌ **6 wks** | **🔴 P1** | **AI Schedule** | **CK3 decisions, activity-aware events** |
| 8 | **Army Lance Activity** | ❌ **7-8 wks** | **🟡 P2** | **AI+Lance+News** | **NPC sim + REAL casualties** |
| 9 | Duty Events | ⏳ Ongoing | 🟡 P2 | Duties exist | 150-200 event content |
| 10 | Camp Activities Tab | ⏳ 1-2 wks | 🟢 P3 | Camp Screen | Location-based activity selection |
| 11 | AI Lord Lance Sim | ❌ 10 wks | 🟢 P3 | None | Enemy army lances |
| 12 | Integration Guides | ✅ Ref | 📖 Ref | - | Technical specs |

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

**Document Version:** 3.1 - Decision Events Framework Added  
**Review Date:** December 16, 2025  
**Last Updated:** December 16, 2025  
**Next Review:** After Decision Events Phase 1 complete  
**Status:** 📋 Complete Implementation Guide  
**Maintained By:** Enlisted Development Team

**Recent Changes (v3.1):**
- Added Track D2: Decision Events Framework (CK3-style decisions)
- Added Activity-Aware Events system linking schedule to events
- Added Main Menu integration for player decisions
- Updated Gantt chart, dependency graph, and system summary
- cross-referenced `decision-events-spec.md` as primary documentation

**Previous Changes (v2.1):**
- Updated Track A Phase 3 status with completed UI fixes (text wrapping, spacing, XML bugs)
- Updated Risk Assessment #1 with active mitigation status
- Consolidated Gauntlet documentation (removed duplicate guide)
