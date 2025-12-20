# Implementation Roadmap - Unimplemented Features

**Last Updated:** December 18, 2025  
**Purpose:** Planning document for features NOT yet implemented  
**For Current Status:** See `implementation-status.md`

## Engineering Standards (applies to everything in this folder)

- Follow **ReSharper** linter/recommendations (see `docs/CONTRIBUTING.md`).
- Use **XML** for:
  - Gauntlet UI prefabs (`GUI/Prefabs/**.xml`)
  - Localization strings (`ModuleData/Languages/enlisted_strings.xml`)
- Keep gameplay/config data in **JSON** (`ModuleData/Enlisted/*.json`) unless there is a specific reason to use XML.

---

## Index

- [Implementation Priority](#implementation-priority)
- [Track E: Army Lance Activity Simulation](#track-e-army-lance-activity-simulation)
- [Track F: AI Lord Lance Simulation](#track-f-ai-lord-lance-simulation)
- [UI Enhancements](#ui-enhancements)
- [Content Expansion](#content-expansion)
- [Optional Enhancements](#optional-enhancements)
- [Timeline Estimates](#timeline-estimates)
- [Dependencies](#dependencies)

---

## Implementation Priority

**HIGH PRIORITY (Do Next):**
1. Duty Events Expansion (65+ events) - Continuous, can start immediately
2. Army Lance Activity Simulation (MVP: 4-5 weeks)

**MEDIUM PRIORITY (After High Priority):**
3. Decision Menu: Camp Life Dashboard (1-2 weeks)
4. Decision Events Content (15+ events) - Continuous

**LOW PRIORITY (Polish/Enhancement):**
5. Army Lance Activity Full (3 more weeks)
6. Camp Management: Party Tab (depends on Track E)
7. Event Content Enhancements

**VERY LOW PRIORITY (Optional):**
8. AI Lord Lance Simulation (10 weeks)
9. Auto-Selection System (1 week)
10. Event Chain Expansion (2-3 weeks)

---

## Track E: Army Lance Activity Simulation

**Status:** Ready to implement - all prerequisites complete  
**Priority:** HIGH (Should-Have for immersion and mechanical depth)  
**Estimated Duration:** 7-8 weeks (MVP: 4-5 weeks)

### Prerequisites (ALL COMPLETE ✅)
- ✅ AI Camp Schedule (`ScheduleBehavior`, `ScheduleGenerator`)
- ✅ Lance Life Simulation (`LanceLifeSimulationBehavior`)
- ✅ News/Dispatches System (`EnlistedNewsBehavior`, `DailyReportGenerator`)
- ✅ Camp Life Conditions (`CampLifeLedger`, situation flags)

### Overview
Simulate 8-15 NPC lances in the player's army with realistic routine operations and optional attrition warfare.

### Core Features

**1. Lance Roster Management**
- Culture-appropriate lance names ("The Bold Hawks", "Ironwood Guard")
- 8-15 lances per army (based on troop count)
- Named lance leaders (basic, not full personality)
- Lance state tracking (readiness 0-100, availability status)

**2. Activity Simulation**
- **Wartime:** Patrols, scouting, foraging, convoy escort, picket duty
- **Peacetime:** Training, recruitment, tax collection, bandit suppression
- **Universal:** Guard duty, equipment maintenance, recovery, discipline
- Dynamic assignment based on lord's objectives and army context

**3. Real Party Casualty System (OPTIONAL - Configurable)**
- **What It Simulates:**
  - Patrols encountering bandits (5-15 enemies)
  - Foraging missions ambushed by looters
  - Scouting parties spotted by enemy scouts
  - Guard duty accidents, disease, desertion
  - **NOT full battles** (handled by native)
- **How It Works:**
  - Lance encounters enemies → System removes killed troops from roster
  - Moves wounded troops to wounded status (7-day recovery)
  - News reports: "3rd Lance ambushed - 2 killed, 3 wounded"
- **Configuration Presets:**
  - Casual: Disabled (0% attrition)
  - Normal: 0.3 multiplier, player only (5-8% monthly)
  - Realistic: 0.5 multiplier, both armies (10-15% monthly)
  - Hardcore: 0.7 multiplier, both armies (15-25% monthly)

**4. Event Generation**
- Probability-based on danger level, context, readiness
- 20-30 event description templates
- Success, complications, failures, rare events

**5. Cover Request System**
- NPC lances request player cover when injured/exhausted
- Accept (gain favor, fatigue, prevent casualties) or decline
- Favor tracking for reciprocity
- Max 1 request per 2 days

**6. News Integration**
- Personal feed (high-priority activities)
- Camp Bulletin (7-day history)
- Casualty attribution ("patrol ambush" vs "battle")
- StoryKey deduplication

**7. Battle Integration**
- Lances on assignment miss battles
- Battle casualties distributed to lances
- Post-battle news notes absent lances

### Implementation Plan

**MVP (4-5 weeks) - Phases 0-3:**

**Week 1: Phase 0 - Foundation**
- Data structures: `ArmyLanceRoster`, `SimulatedLance`, `LanceAssignment`, `LanceEvent`
- Save/load support via `IDataStore.SyncData`
- Roster generation on army creation
- Culture-appropriate name generation
- **File:** `src/Features/Army/Simulation/ArmyLanceSimulationBehavior.cs`
- **Config:** `ModuleData/Enlisted/army_lance_config.json`

**Week 2: Phase 1 - Assignment & State Tracking**
- Duty assignment logic (lord objectives, context)
- Assignment advancement (daily tick)
- Readiness degradation/recovery
- Lance status transitions
- Integration with `ScheduleBehavior` (use same duty types)

**Week 3: Phase 2 - Event Generation**
- Probability system (danger × context × readiness)
- Event type weights and selection
- Event consequence application
- **Optional:** Basic casualty calculation
- Event history tracking (7 days)

**Week 4: Phase 3 - News Integration**
- News item generation from lance events
- Headline templates (success, contact, casualties, failures)
- Priority system integration with `EnlistedNewsBehavior`
- Camp Bulletin submenu hookup
- StoryKey deduplication

**Week 5: Testing & Balance**
- Event probability tuning
- Readiness balance verification
- Integration testing with existing systems
- Basic gameplay testing (30-day campaign)

**Full Implementation (3 more weeks) - Phases 4-5:**

**Week 6: Phase 4 - Cover Requests**
- Condition evaluation for requests
- Inquiry event creation
- Accept/decline handlers with consequences
- Favor tracking system
- Integration with player fatigue system

**Week 7: Phase 5 - Battle Integration**
- Battle casualty distribution to lances
- Army strength calculation (available vs absent)
- Post-battle news noting absent lances
- Lance state updates after battles

**Week 8: Casualty System Balance Testing**
- Playtest all configuration presets
- 30-day campaign analysis per preset
- AI recruitment behavior monitoring
- Multiplier adjustments based on data
- Player feedback collection

### Technical Implementation

**New Files Required:**
```
src/Features/Army/
  Simulation/
    - ArmyLanceSimulationBehavior.cs (main behavior)
    - ArmyLanceRoster.cs (lance roster management)
    - SimulatedLance.cs (lance state tracking)
    - LanceAssignment.cs (assignment logic)
    - LanceEvent.cs (event generation)
    - LanceEventGenerator.cs (event selection)
    - CoverRequestEvaluator.cs (cover request logic)

ModuleData/Enlisted/
  - army_lance_config.json (configuration)
  - Events/events_army_lance.json (event templates)
```

**Integration Points:**
- `ScheduleBehavior` - Use same duty/activity types
- `LanceLifeSimulationBehavior` - Same injury/death mechanics
- `EnlistedNewsBehavior` - Feed lance events to news
- `CampBulletinVM` - Display lance history
- `MemberRoster` API - Apply casualties (if enabled)

### Configuration Schema

```json
{
  "army_lance_simulation": {
    "enabled": true,
    "min_lances": 8,
    "max_lances": 15,
    "lances_per_100_troops": 1.5,
    "event_evaluation_hours": 12,
    "casualty_system": {
      "enabled": true,
      "preset": "normal",
      "base_multiplier": 0.3,
      "wounded_recovery_days": 7,
      "wounded_vs_killed_ratio": 0.7,
      "apply_to_enemy_armies": false
    },
    "cover_requests": {
      "enabled": true,
      "max_per_week": 2,
      "min_hours_between": 48,
      "fatigue_cost": 3
    }
  }
}
```

### Success Criteria

**MVP (Phases 0-3):**
- [ ] Lances generate with appropriate names
- [ ] Assignments match lord objectives
- [ ] Events fire based on assignments
- [ ] News items appear in feed
- [ ] System persists across save/load
- [ ] No performance issues (30-day test)

**Full (Phases 4-5):**
- [ ] Cover requests appear appropriately
- [ ] Accepting/declining has correct consequences
- [ ] Battle integration works correctly
- [ ] Casualty system (if enabled) feels balanced
- [ ] All configuration presets tested

### Risks & Mitigations

**Risk:** Too punishing for casual players
- **Mitigation:** Default to "Normal" (0.3 multiplier, player only), fully configurable, can disable

**Risk:** AI recruitment not keeping up
- **Mitigation:** Player-only default, monitor in testing, possible AI boost if needed

**Risk:** Balance complexity
- **Mitigation:** Week 8 dedicated to balance testing, multiple presets, extensive playtesting

---

## Track F: AI Lord Lance Simulation

**Status:** Can build anytime (no dependencies)  
**Priority:** LOW (Nice-to-Have)  
**Estimated Duration:** 10 weeks

### Overview
Simulate lance structure for enemy/allied armies for intelligence gathering and company status displays.

### Purpose
- View enemy army lance composition (estimates)
- Intelligence quality based on Scouting skill
- "Company Status" displays for other armies
- Integration with Recon Mission (if implemented)

### Features
- Enemy army lance estimates (fuzzy based on intel quality)
- Allied army lance status displays
- Intelligence aging/updating
- Integration with existing scouting mechanics

### Implementation Notes
**Recommendation:** Build this LAST after all core features stable. Pure enhancement, not critical path.

---

## UI Enhancements

### Decision Menu: Camp Life Dashboard

**Status:** Partially specified, not implemented  
**Priority:** MEDIUM  
**Estimated Duration:** 1-2 weeks

**Purpose:** Show "Camp Life first, decisions second" - provide context before choices.

**Features:**

**Dashboard Section (top of `enlisted_decisions`):**
- News Summary: Today's Daily Report excerpt
- Company Events (30 days):
  - Men lost: X
  - Currently wounded: Y / sick: Z
  - Training incidents: N
- Current Pressure: Food/morale/threat bands

**Categorized Decisions (below dashboard):**
- camp_life: shortages, sickness, discipline, pay
- combat: battle prep, volunteering, aftermath
- intel: scouting, screening, patrols
- training: drills, mentorship, sparring
- social: dice, drinks, letters, favors
- logistics: rations, requisitions, equipment

**Implementation:**
- Extend `DecisionEventBehavior` menu generation
- Use existing `CampLifeLedger` for 30-day stats
- Add dashboard section to `enlisted_decisions` menu
- Use existing situation flags and news summaries

**Files to Modify:**
- `src/Features/Lances/Events/Decisions/DecisionEventBehavior.cs`
- `src/Features/Interface/Behaviors/EnlistedMenuBehavior.cs`

---

### Camp Management: Party Tab

**Status:** Stub exists, not implemented  
**Priority:** LOW (depends on Track E)  
**Estimated Duration:** Include with Track E implementation

**Purpose:** View/manage NPC lances in the army (requires Army Lance Activity Simulation).

**Features:**
- List all lances in army
- Lance leader names and statuses
- Readiness levels
- Current assignments
- Wounded/sick/dead counts per lance
- Cover request history

**Dependencies:** Requires Track E (Army Lance Activity) first.

**Recommendation:** Build as part of Track E, not separately.

---

## Content Expansion

### Duty Events Library Expansion

**Current:** ~50 duty-specific events, ~142 total events  
**Target:** 150-200 total events  
**Gap:** 65+ events needed

**Priority Breakdown:**

**HIGH PRIORITY (65 events):**
1. **Duty Events - Role Expansion:**
   - Quartermaster: +10 events (peace/war variants)
   - Scout: +10 events (patrol/recon scenarios)
   - Field Medic: +10 events (triage/treatment)
   - Armorer: +10 events (maintenance/salvage)
   - Runner: +5 events
   - Lookout: +5 events
   - Messenger: +5 events
   - Engineer: +5 events
   - Boatswain: +5 events
   - Navigator: +5 events
   - **Subtotal: 70 events**

**MEDIUM PRIORITY (15 events):**
2. **Decision Events - Social/Politics:**
   - Lance mate interactions: +5
   - Lord interactions: +5
   - Camp politics: +5
   - **Subtotal: 15 events**

**LOW PRIORITY (15 events):**
3. **Formation-Specific Training:**
   - Infantry-specific: +5
   - Cavalry-specific: +5
   - Ranged-specific: +5
   - **Subtotal: 15 events**

**Total Target:** +100 events (allows selection/quality filtering)

### Creation Process

**Using:** `docs/research/duty-events-creation-guide.md` (moved to research)

**Creation Rate:** 10-20 events per week  
**Timeline:** 5-10 weeks of content creation  
**Can be done:** In parallel with feature development

**Quality Checklist per Event:**
- [ ] 3 viable choice paths (safe/risky/corrupt or alternatives)
- [ ] Context-aware (peace/war/siege variants)
- [ ] Balanced XP rewards (30-70 range)
- [ ] Appropriate costs (fatigue 2-6, gold if relevant)
- [ ] Heat/Discipline/Rep impacts proportional
- [ ] Outcome text acknowledges choice
- [ ] Placeholder text used correctly
- [ ] Localization IDs added

---

## Optional Enhancements

### Auto-Selection System for Event Rewards

**Status:** Schema supports it, not implemented  
**Priority:** LOW (QoL)  
**Estimated Duration:** 1 week

**Purpose:** Reduce micromanagement for players who prefer automation.

**Config Addition:**
```json
"event_preferences": {
  "enabled": false,
  "auto_select_rewards": false,
  "training_focus": "formation_appropriate",
  "gold_vs_reputation": "balanced",
  "risk_tolerance": "balanced"
}
```

**Implementation:**
- Extend `LanceLifeRewardChoiceInquiryScreen`
- Add preference tracking (last 5 choices)
- Formation-appropriate defaults
- Log auto-selections in debug

---

### Event Chain System Expansion

**Status:** Basic chains work, complex chains don't  
**Priority:** LOW (content enhancement)  
**Estimated Duration:** 2-3 weeks

**Current Support:**
- ✅ One-step chains (Event A → Event B)
- ❌ Multi-step chains (A → B → C)
- ❌ Branching chains (A → B1 or B2 based on choice)
- ❌ Chain memory (Event C references Event A choice)
- ❌ Delayed triggers (Event B fires 7 days after A)

**Recommendation:** Add after core content library complete (150+ events).

---

### Loot System (Decision Events)

**Status:** Optional, not started  
**Priority:** VERY LOW  
**Estimated Duration:** 1-2 weeks

**Purpose:** Add item rewards to decision events (currently XP/gold/effects only).

**Recommendation:** Low value-add. Events work fine without items.

---

### News Integration for Decision Outcomes

**Status:** Documented, not started  
**Priority:** VERY LOW  
**Estimated Duration:** 1 week

**Purpose:** Decision outcomes automatically generate news items.

**Example:**
```
Player: "Helped lance mate" → Success
News: "Soldier earns praise from {LANCE_LEADER}"
```

**Implementation:** Add optional `news_template` to event outcomes, feed to `EnlistedNewsBehavior`.

---

## Timeline Estimates

### High Priority Path (MVP Focus)

```
Week 1-5:  Duty Events Content (continuous, parallel)
Week 1-5:  Army Lance Activity MVP (Phases 0-3)
Week 6-7:  Decision Menu Dashboard
Week 8-10: Army Lance Activity Full (optional Phases 4-5)
```

**Total:** 10 weeks for high-priority features

### Medium Priority Path (Polish)

```
Week 11-12: Formation-specific event content
Week 13-14: Additional decision events
Week 15:    Auto-selection system (optional)
```

**Total:** 5 more weeks for medium-priority features

### Low Priority Path (Enhancement)

```
Week 16-18: Event chain system expansion
Week 19-28: AI Lord Lance Simulation (optional)
```

**Total:** 13 more weeks for low-priority enhancements

### Complete Roadmap Timeline

**MVP:** 10 weeks  
**Polished:** 15 weeks  
**Complete:** 28 weeks

---

## Dependencies

### Track E (Army Lance Activity) Dependencies
**Prerequisites (ALL COMPLETE):**
- ✅ AI Schedule System
- ✅ Lance Life Simulation
- ✅ News/Dispatches System
- ✅ Camp Life Conditions

**Enables:**
- Camp Management: Party Tab
- Additional lance-related content

### Content Expansion Dependencies
**Prerequisites:**
- ✅ Duty Events Creation Guide (moved to research)
- ✅ Event schemas finalized
- ✅ Reward choice system implemented

**No blockers:** Can start immediately

### UI Enhancements Dependencies
**Decision Menu Dashboard:**
- ✅ `CampLifeLedger` implemented
- ✅ Situation flags implemented
- ✅ News system implemented

**Party Tab:**
- ❌ Track E (Army Lance Activity) must be implemented first

---

## Success Metrics

### Technical Success
- [ ] All systems build without errors
- [ ] No crashes during normal gameplay
- [ ] Save/load works reliably
- [ ] Performance acceptable (no lag during 30-day campaign)
- [ ] All integration points functional

### Content Success
- [ ] 150-200 duty events created
- [ ] All 10 duty roles have 15-20 events each
- [ ] Events feel varied (not repetitive after 30 days)
- [ ] Peace and war variants exist
- [ ] Moral choices have real consequences

### Player Experience Success
- [ ] Army lance simulation feels immersive (if implemented)
- [ ] Decisions feel meaningful
- [ ] Systems feel integrated (not isolated)
- [ ] Military service feels like living in an army
- [ ] Player agency preserved throughout

---

## Testing Strategy

### For Each New Feature

**Unit Testing:**
- Core functionality works in isolation
- Save/load preserves state correctly
- Config changes apply as expected
- Edge cases handled gracefully

**Integration Testing:**
- New feature integrates with existing systems
- No performance degradation
- No mod conflicts (stay read-only where possible)
- Cross-system dependencies work correctly

**Gameplay Testing:**
- Feature feels fun and engaging
- Balance is appropriate (not too easy/hard)
- No exploits or cheese strategies
- Player feedback collected and positive

**Long-term Testing:**
- 30-day playthrough minimum per feature
- Multiple enlistments tested
- Different playstyles tested (aggressive, cautious, corrupt, loyal)
- No issues after extended play

---

## Version History

**v1.0** (December 18, 2025)
- Consolidated from `unimplemented-features.md` and `master-implementation-roadmap.md`
- Removed all implemented features (moved to `implementation-status.md`)
- Focused purely on future work
- Added detailed implementation plans
- Organized by priority

---

## Related Documents

- **[Implementation Status](implementation-status.md)** - What's currently done
- **[Duty Events Creation Guide](../research/duty-events-creation-guide.md)** - How to create duty events
- **[Gauntlet UI Playbook](../research/gauntlet-ui-screens-playbook.md)** - How to build UI safely
- **[Features Index](../Features/index.md)** - Implemented feature specs
- **[Story Blocks Master Reference](../StoryBlocks/story-blocks-master-reference.md)** - Story content authoring (single source of truth)

---

**Document Maintained By:** Enlisted Development Team  
**Next Review:** After completing next high-priority feature  
**Status:** Active Planning Document

