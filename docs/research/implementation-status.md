# implementation status

**last updated:** december 17, 2025  
**build status:** [x] compiles with 0 errors

## index

- [active tracks summary](#active-tracks-summary)
- [track a: camp & activities](#track-a-camp--activities--complete)
- [track b: ai camp schedule](#track-b-ai-camp-schedule--complete)
- [track c: lance life simulation](#track-c-lance-life-simulation--complete)
- [track d2: decision events framework](#track-d2-decision-events-framework)
- [ui status](#ui-status)
- [content inventory](#content-inventory)
- [documentation index](#documentation-index)
- [quick reference: what to read](#quick-reference-what-to-read)

---

## Active Tracks Summary

| Track | Name | Status | Next Step |
|-------|------|--------|-----------|
| **A** | Camp & Activities | [x] Complete | UI polish (optional) |
| **B** | AI Camp Schedule | [x] Complete | Integrated |
| **C** | Lance Life Simulation | [x] Complete | Content creation |
| **D2** | Decision Events | [x] Phase 6 Complete | Phase 7-9 |

---

## Track A: Camp & Activities [x] COMPLETE

| Phase | Status | Description |
|-------|--------|-------------|
| Phase 1: Foundation | [x] | Activity cards, screen, data loading |
| Phase 2: Camp Bulletin | [x] | Settlement-style UI, location activities |
| Phase 3: UI Polish | [x] | Spacing/alignment |
| Phase 3.5: Authority | [x] | Tier + Lance Leader filtering |

**Key Files:**
- `src/Features/Camp/UI/Bulletin/CampBulletinVM.cs`
- `src/Features/Activities/CampActivitiesBehavior.cs`

---

## Track B: AI Camp Schedule [x] COMPLETE

| Phase | Status | Description |
|-------|--------|-------------|
| Phase 0: Foundation | [x] | Data models, config, save/load |
| Phase 1: Basic Scheduling | [x] | Generation algorithm, lord objectives |
| Phase 2: Lance Needs | [x] | Degradation, recovery mechanics |
| Phase 3: AI Logic | [x] | Context-aware scheduling |
| Phase 4: Block Execution | [x] | Activation, completion tracking |
| Phase 4.5: Authority | [x] | 4-level authority, approval system |
| Phase 5: T5-T6 Leadership | [x] | Performance tracking, consequences |
| Phase 6: Pay Muster | [x] | 12-day cycle integration |
| Phase 7: Polish | [x] | Combat interrupts, bulletin integration |

**Key Files:**
- `src/Features/Schedule/Behaviors/ScheduleBehavior.cs`
- `src/Features/Schedule/Core/ScheduleGenerator.cs`
- `ModuleData/Enlisted/schedule_config.json`

---

## Track C: Lance Life Simulation [x] COMPLETE

| Phase | Status | Description |
|-------|--------|-------------|
| Phase 1: Member Tracking | [x] | Health states, activity states |
| Phase 2: Leader System | [x] | Persistent leaders, personalities, memory |
| Phase 3: Events | [x] | Cover requests, injuries, deaths |
| Phase 4: Integration | [x] | UI events, notifications |

**Key Files:**
- `src/Features/Lances/Simulation/LanceLifeSimulationBehavior.cs`
- `src/Features/Lances/Leaders/PersistentLanceLeadersBehavior.cs`

---

## Track D2: Decision Events Framework

### Current Status: Phase 6 Complete

| Phase | Status | Description |
|-------|--------|-------------|
| Phase 1: Core Infrastructure | [x] | State, Evaluator, Behavior, Config |
| Phase 1.5: Activity-Aware | [x] | `current_activity:X`, `on_duty:X` tokens |
| Phase 2: Pacing System | [x] | 8 protection layers built into Phase 1 |
| Phase 3: Event Chains | [x] | `chains_to`, `set_flags`, `clear_flags` |
| Phase 4: Player Menu | [x] | "Pending Decisions" submenu |
| Phase 5: Loot System | IN PROGRESS | Optional, not started |
| Phase 6: Tier-Based Narrative | [x] | `narrative_source`, tier gates |
| Phase 7: Content Creation | IN PROGRESS | Ongoing (14 events created) |
| Phase 8: News Integration | IN PROGRESS | Documented, not started |
| Phase 9: Testing & Debug | IN PROGRESS | Debug commands needed |

### Decision Events Created: 14

| Type | Count | Examples |
|------|-------|----------|
| Automatic (pushed) | 9 | Lord hunt, dice game, training offer |
| Player-initiated | 5 | Organize dice, petition lord, write letter |
| Chain events | 3 | Lance mate favor -> repayment |

### Next Actions

1. **Phase 9 (Testing):** Add debug console commands
2. **Phase 7 (Content):** Create more events
3. **Phase 8 (News):** Integrate with Camp Bulletin (optional)

**Key Files:**
- `src/Features/Lances/Events/Decisions/DecisionEventBehavior.cs`
- `src/Features/Lances/Events/Decisions/DecisionEventEvaluator.cs`
- `ModuleData/Enlisted/Events/events_decisions.json`
- `ModuleData/Enlisted/Events/events_player_decisions.json`
- Full spec: [Story Blocks Master Reference](../StoryBlocks/story-blocks-master-reference.md)

---

## UI Status

### Camp Management Screen

| Tab | Backend | Frontend |
|-----|---------|----------|
| Activities | [x] Complete | [x] Bulletin UI |
| Orders | [x] Complete | IN PROGRESS Needs authority integration |
| Lance | [x] Complete | IN PROGRESS Empty stub |
| Reports | [x] Complete | IN PROGRESS Basic |
| Party | [x] Complete | IN PROGRESS Empty stub |

### Enlisted Main Menu

| Feature | Status |
|---------|--------|
| Status display | [x] Complete |
| News dispatches | [x] Complete |
| Pending Decisions | [x] Complete (Phase 4) |
| Duty selection | [x] Complete |

---

## Content Inventory

### Events

| Category | Count |
|----------|-------|
| Decision | 14 |
| Duty | 50 |
| Threshold | 16 |
| General | 18 |
| Onboarding | 9 |
| Training | 16 |
| Pay | 14 |
| Promotion | 5 |
| **TOTAL** | ~142 |

### Activities

| Location | Count |
|----------|-------|
| training_grounds | 7 |
| mess_tent | 3 |
| quartermaster | 2 |
| medical_tent | 2 |
| **TOTAL** | 14 |

---

## Documentation Index

### Active Docs (Keep Updated)

| File | Purpose |
|------|---------|
| `docs/Features/index.md` | Feature documentation entry point |
| `docs/StoryBlocks/story-blocks-master-reference.md` | Story content reference (single source of truth) |
| `docs/ImplementationPlans/master-implementation-roadmap.md` | Full roadmap |
| `docs/ImplementationPlans/implementation-status.md` | This file |

## Implementation Plans Folder

**Current Structure (2 files):**
- `implementation-status.md` (this file) - What's done, current state
- `implementation-roadmap.md` - What's NOT done, future work

**Guides Moved to Research:**
- `docs/research/duty-events-creation-guide.md` - Content authoring
- `docs/research/gauntlet-ui-screens-playbook.md` - UI technical patterns

**Archived/Deleted Docs:**
Older implementation-plan drafts were consolidated and removed (Dec 18, 2025)

### Reference Docs

| Folder | Contents |
|--------|----------|
| `docs/Features/Core/` | Core system specs |
| `docs/Features/Gameplay/` | Gameplay feature specs |
| `docs/Features/UI/` | UI component specs |
| `docs/Features/Technical/` | Technical implementation |

---

## Quick Reference: What to Read

| If you want to... | Read this |
|-------------------|-----------|
| Understand all features | `docs/Features/index.md` |
| Add story content | `docs/StoryBlocks/story-blocks-master-reference.md` (all events, schema, guidelines) |
| Check current status | `docs/ImplementationPlans/implementation-status.md` (this file) |
| See what's NOT yet implemented | `docs/ImplementationPlans/implementation-roadmap.md` |
| create duty events | `docs/research/duty-events-creation-guide.md` |
| build UI safely | `docs/research/gauntlet-ui-screens-playbook.md` |

