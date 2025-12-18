# Enlisted Interface & Events - Master Implementation Plan

## Vision Statement

Transform the Enlisted mod's interface from static menus and simple notifications into a dynamic, narrative-driven experience where:
- The **Main Menu** shows your current status, daily schedule, and time-sensitive orders at a glance
- **Duties** trigger interactive events with meaningful choices and consequences
- **Decisions** become a hub for gameplay events, training, and social activities
- Player choices cascade into follow-up events that unfold over hours or days
- Skills matter - your character build determines success in narrative moments

This creates a "Viking Conquest meets CK3" experience where being an enlisted soldier feels alive and consequential.

---

## The Three Pillars

### Pillar 1: Main Menu Redesign
**Goal**: Show player status, schedule, and urgent orders prominently

**Current State**: Basic status info buried in text blocks

**Target State**: 
```
═══════════════════════════════════════════════════════════
Lord: Derthert
Rank: Man-at-Arms (T2) | Enlisted: 47 days
Lord's Work: Besieging Aserai

Daily Report:
Your lance joined the siege. Supplies are running low.

Schedule:
  [Morning: Training Drill]  ← Current
   Afternoon: Guard Duty
   Dusk: Free Time
   Night: Rest

───────────────────────────────────────────────────────────
Orders:
  • Report to Quartermaster [Expires: Tonight]
  • Assist with Siege Prep [Expires: Tomorrow]
───────────────────────────────────────────────────────────

[Camp] [Decisions] [My Lord...] [Leave Service]
═══════════════════════════════════════════════════════════
```

### Pillar 2: Decisions Menu Redesign
**Goal**: Organize gameplay events, training, and social activities clearly

**Current State**: Generic "decisions" list, camp activities elsewhere

**Target State**:
```
═══════════════════════════════════════════════════════════
                      DECISIONS
═══════════════════════════════════════════════════════════

Events:
  • Lance Leader Summons You [Urgent]
  • Dispute Between Lance Mates

───────────────────────────────────────────────────────────

Training (Free Time / Night):
  • Sparring Circle
  • Weapons Practice
  • Archery Range
  • Riding Drills

Social (Free Time / Night):
  • Fire Circle
  • Drink with the Lads
  • Play Dice
  • Tell War Stories

───────────────────────────────────────────────────────────
[Back]
═══════════════════════════════════════════════════════════
```

### Pillar 3: Duty Events System
**Goal**: Transform simple duty notifications into interactive narrative events

**Current State**: "You maintain equipment" → [Continue] → +15 XP

**Target State**:
```
[Morning: Work Detail executes]
    ↓
════════════════════════════════════
        WORK DETAIL
────────────────────────────────────
You find a rusted sword that needs
attention. What do you do?

[Repair it properly] (Smithing 30+)
  Success: Quality weapon, +Gold
  Failure: Weapon breaks, -Gold

[Quick patch job] (Standard)
  Adequate repair, safe choice

[Leave it for the smith]
  No risk, no reward
════════════════════════════════════
    ↓
[Player chooses "Repair properly"]
[Skill Check: SUCCESS]
    ↓
[2 hours later...]
════════════════════════════════════
    LANCE LEADER'S NOTICE
────────────────────────────────────
"Fine craftsmanship, soldier!"

+50 Gold | +Reputation | +Smithing XP
════════════════════════════════════
```

---

## System Architecture

### Component Map

```
Main Menu (enlisted_status)
├── Shows: Schedule + Orders
├── Links to: Camp, Decisions, Conversations, Leave
└── Auto-refreshes: Hourly (schedule highlighting)

Decisions Menu (enlisted_decisions)
├── Events Section (automatic/pushed events)
├── Training Section (player-initiated, time-gated)
├── Social Section (player-initiated, time-gated)
└── Links to: Full event screens

Schedule System
├── Assigns duties to 4 time blocks
├── Executes duties at block start
├── Triggers: Duty Event OR Simple Popup
└── Awards: XP, updates schedule state

Duty Events System
├── Maps: Duty Type → Event Pool
├── Picks: Random event from pool
├── Launches: Full event screen (3+ options)
├── Evaluates: Skill checks (success/failure)
├── Schedules: Follow-up events (delayed)
└── Fires: Queued events at specified time

Orders System (NEW)
├── Sources: Duties, Quests, Leader commands
├── Displays: In Main Menu Orders section
├── Expires: Time-based (Tonight, Tomorrow, etc.)
└── Notifies: Player of urgent tasks
```

---

## Implementation Roadmap

### Phase 1: Main Menu Redesign (4-6 hours)
**Goal**: Update main menu layout with Schedule and Orders sections

**Tasks**:
1. Modify `BuildCompactEnlistedStatusText()` method
   - Reorganize header (Lord, Rank, Days Enlisted)
   - Keep Daily Report section
   - Add Schedule section (4 time blocks)
   - Add Orders section (header + dynamic list)
   - Add separator lines

2. Create helper methods:
   - `BuildScheduleSection()` - format schedule display
   - `BuildOrdersSection()` - format orders display (stub for now)
   - `GetTimeBlockDisplayName()` - format time block names
   - `HighlightCurrentTimeBlock()` - add visual highlighting

3. Integrate with Schedule system:
   - Call `EnlistedDutiesBehavior.GetPlayerSchedule()`
   - Get current time block from `CampaignTriggerTrackerBehavior.GetTimeBlock()`
   - Map schedule data to 4 blocks (Morning/Afternoon/Dusk/Night)
   - Show "Free Time" for unassigned blocks

**Files**:
- `src/Features/Interface/Behaviors/EnlistedMenuBehavior.cs`
  - Method: `BuildCompactEnlistedStatusText()`
  - New: `BuildScheduleSection()`
  - New: `BuildOrdersSection()`

**Acceptance Criteria**:
- Main menu displays with new layout
- Schedule section shows 4 time blocks with duties
- Current time block is highlighted
- Orders section shows header (empty for now)
- Menu auto-refreshes on hourly tick
- No crashes or visual glitches

---

### Phase 2: Decisions Menu Reorganization (3-4 hours)
**Goal**: Restructure Decisions menu with Events, Training, Social sections

**Tasks**:
1. Update `OnDecisionsMenuInit()`:
   - Split decisions into categories
   - Add "Events" section header for pushed events
   - Add "Training" section header + activities
   - Add "Social" section header + activities
   - Add separators between sections

2. Create section builders:
   - `BuildEventsSection()` - pushed/automatic events
   - `BuildTrainingSection()` - training activities from catalog
   - `BuildSocialSection()` - social activities from catalog

3. Filter activities by time:
   - Training/Social only available during Free Time or Night
   - Gray out options when wrong time of day
   - Show availability hint in tooltip

4. Migrate activities:
   - Keep Training activities from `activities.json`
   - Keep Social activities from `activities.json`
   - Remove "tasks" activities (will become duty events)

**Files**:
- `src/Features/Interface/Behaviors/EnlistedMenuBehavior.cs`
  - Method: `OnDecisionsMenuInit()`
  - New: `BuildEventsSection()`
  - New: `BuildTrainingSection()`
  - New: `BuildSocialSection()`
- `ModuleData/Enlisted/Activities/activities.json`
  - Keep: training.* and social.* activities
  - Remove: tasks.* activities (converted to events)

**Acceptance Criteria**:
- Decisions menu has 3 clear sections
- Events section shows pushed decision events
- Training section shows available training activities
- Social section shows available social activities
- Time restrictions enforced (Free Time / Night only)
- Tooltips show availability windows
- Separator lines between sections

---

### Phase 3: Duty-to-Event Mapping (4-5 hours)
**Goal**: Connect scheduled duties to event pools instead of simple popups

**Tasks**:
1. Create `DutyEventMapping.json`:
   - Map duty types to event pools
   - Assign weights for random selection
   - Support fallback to simple popup if no mapping

2. Create mapping loader:
   - `DutyEventMappingLoader.cs` - loads and caches mappings
   - `TryGetEventForDuty()` - picks event from pool
   - Weighted random selection

3. Modify `ScheduleExecutor.TriggerScheduleEvent()`:
   - Check for duty-to-event mapping
   - If mapping exists: Launch full event screen
   - If no mapping: Fall back to simple popup (current behavior)
   - Log which path was taken

4. Create initial mappings:
   - WorkDetail → rusty_weapon_found (weight 1.0)
   - PatrolDuty → suspicious_tracks (weight 1.0)
   - SentryDuty → night_watch_disturbance (weight 1.0)

**New Files**:
- `ModuleData/Enlisted/Events/duty_event_mappings.json`
- `src/Features/Schedule/Events/DutyEventMappingLoader.cs`

**Modified Files**:
- `src/Features/Schedule/Core/ScheduleExecutor.cs`

**Example Config**:
```json
{
  "schemaVersion": 1,
  "mappings": [
    {
      "duty_type": "WorkDetail",
      "event_pool": [
        { "event_id": "rusty_weapon_found", "weight": 1.0 },
        { "event_id": "broken_wagon_wheel", "weight": 0.8 }
      ],
      "fallback_to_popup": true
    }
  ]
}
```

**Acceptance Criteria**:
- Mapping config loads successfully
- Duties with mappings launch full event screens
- Duties without mappings use simple popups (backward compatible)
- Weighted selection works correctly
- No crashes if config missing
- Logging shows which path taken

---

### Phase 4: Event Chaining System (6-8 hours)
**Goal**: Enable events to schedule follow-up events with delays

**Tasks**:
1. Extend event schema:
   - Add `next_event` to success/failure outcomes
   - Add `next_event_delay_hours` to outcomes
   - Support conditional next events (based on game state)

2. Implement event queue:
   - `_eventQueue` in `DecisionEventBehavior`
   - `ScheduleEvent(eventId, delayHours)` method
   - Hourly tick checks and fires due events
   - Persist queue through save/load

3. Update event outcome handler:
   - After applying effects, check for next_event
   - If present, schedule it with delay
   - Support both success and failure follow-ups

4. Create follow-up events:
   - `rusty_weapon_outcome_success` - Lance leader praises
   - `rusty_weapon_outcome_failure` - Quartermaster angry
   - `wagon_wheel_holds` - Wheel holds during travel
   - `wagon_wheel_breaks` - Wheel breaks during travel

**Modified Files**:
- `src/Features/Lances/Events/LanceLifeEventCatalog.cs` - extend schema
- `src/Features/Lances/Events/Decisions/DecisionEventBehavior.cs` - add queue

**Schema Example**:
```json
{
  "options": [
    {
      "id": "repair_properly",
      "skill_check": { "skill": "Smithing", "difficulty": 30 },
      "success_outcome": {
        "effects": { "fatigue": -2, "skill_xp": { "Smithing": 15 } },
        "next_event": "rusty_weapon_outcome_success",
        "next_event_delay_hours": 2
      },
      "failure_outcome": {
        "effects": { "fatigue": -2 },
        "next_event": "rusty_weapon_outcome_failure",
        "next_event_delay_hours": 2
      }
    }
  ]
}
```

**Acceptance Criteria**:
- Events can specify follow-up events
- Follow-ups fire after correct delay
- Queue persists through save/load
- Multiple queued events don't conflict
- Events fire even if player in different menu
- Logging shows chain execution

---

### Phase 5: Event Content Creation (10-15 hours)
**Goal**: Create 10-15 duty events with full chains

**Tasks**:
1. Write event JSON for each duty type:
   - Work Detail: 3 events
   - Patrol Duty: 2 events
   - Sentry Duty: 2 events
   - Training Drill: 2 events
   - Foraging: 1 event

2. Write follow-up events:
   - Success outcomes (praise, rewards)
   - Failure outcomes (punishment, penalties)
   - Neutral outcomes (acknowledgment)

3. Balance rewards:
   - XP: 15-25 for successes
   - Gold: 20-50 for successes, -10 to -30 for failures
   - Reputation: +1 to +3 for good, -1 to -3 for bad
   - Skill thresholds: 20-40 range

4. Test all chains:
   - Verify each chain completes
   - Test skill check math
   - Verify delays work correctly
   - Check for dead ends

**New Files**:
- `ModuleData/Enlisted/Events/duty_events_work_detail.json`
- `ModuleData/Enlisted/Events/duty_events_patrol.json`
- `ModuleData/Enlisted/Events/duty_events_sentry.json`
- `ModuleData/Enlisted/Events/duty_events_training.json`
- `ModuleData/Enlisted/Events/duty_events_foraging.json`

**Reference**: `docs/Features/Events/duty-events-catalog.md` for event designs

**Acceptance Criteria**:
- 10+ events created and working
- Each event has 3+ options
- Chains tested end-to-end
- Rewards balanced
- No dead-end chains
- All skill checks functional

---

### Phase 6: Orders System Foundation (6-8 hours)
**Goal**: Create Orders system to show time-sensitive tasks in main menu

**Tasks**:
1. Define Orders data structure:
   ```csharp
   public class EnlistedOrder
   {
       public string Id;
       public string Description;
       public CampaignTime ExpirationTime;
       public int Priority;
       public string SourceSystem; // "duty", "event", "quest"
   }
   ```

2. Create `EnlistedOrdersBehavior`:
   - Tracks active orders
   - Provides `GetActiveOrders()` method
   - Removes expired orders
   - Persists through save/load

3. Integrate order sources:
   - Duty system: Generate order for scheduled duty 1 hour before
   - Event system: Generate order when event becomes available
   - (Future) Quest system integration

4. Update Main Menu Orders section:
   - Call `EnlistedOrdersBehavior.GetActiveOrders()`
   - Format as bullet list with expiration times
   - Show "Orders:" header even when empty
   - Make orders clickable to navigate to relevant menu

**New Files**:
- `src/Features/Orders/Models/EnlistedOrder.cs`
- `src/Features/Orders/Behaviors/EnlistedOrdersBehavior.cs`
- `src/Features/Orders/Providers/DutyOrdersProvider.cs`
- `src/Features/Orders/Providers/EventOrdersProvider.cs`

**Modified Files**:
- `src/Features/Interface/Behaviors/EnlistedMenuBehavior.cs` - integrate orders display

**Acceptance Criteria**:
- Orders appear in main menu
- Orders expire correctly
- Empty state shows header only
- Orders sorted by priority/expiration
- Clicking order navigates to relevant menu
- Orders persist through save/load

---

### Phase 7: Polish & Testing (4-6 hours)
**Goal**: Refine UI, add localization, fix bugs

**Tasks**:
1. UI Polish:
   - Adjust spacing and separators
   - Refine time block highlighting
   - Improve order expiration formatting
   - Test with various text lengths

2. Localization:
   - Add all new strings to `enlisted_strings.xml`
   - Replace hardcoded text with TextObject
   - Test with long translations

3. Performance:
   - Profile menu refresh times (<50ms target)
   - Optimize event queue scanning
   - Cache schedule/orders data

4. Testing:
   - Test all event chains end-to-end
   - Test save/load with queued events
   - Test with missing/corrupted data
   - Test during battles/sieges
   - Test with rapid time progression

5. Documentation:
   - Update code comments
   - Add XML documentation
   - Update feature specs with final details

**Acceptance Criteria**:
- All text localized
- Menu refresh <50ms
- No crashes in any scenario
- All event chains work
- Save/load preserves state
- Code well-documented

---

## Complete Event Flow Examples

### Example 1: Simple Duty (No Event)
```
1. Time: 06:00 (Morning block starts)
2. Schedule executes: "Training Drill"
3. No event mapping exists for TrainingDrill
4. Simple popup: "Your lance leader notices your improved form"
5. Player clicks [Continue]
6. Awards: +15 One-Handed XP
7. Duty marked complete
```

### Example 2: Duty with Event Chain
```
1. Time: 12:00 (Afternoon block starts)
2. Schedule executes: "Work Detail"
3. Event mapping found: WorkDetail → "rusty_weapon_found"
4. Full event screen opens:
   ═══════════════════════════════════
           WORK DETAIL
   ───────────────────────────────────
   You find a rusted sword...
   
   [Repair it] (Smithing 30+)
   [Quick patch]
   [Leave it]
   ═══════════════════════════════════

5. Player chooses [Repair it]
6. Skill check: Player has Smithing 45 → SUCCESS
7. Immediate effects: -2 Fatigue, +15 Smithing XP
8. Schedule follow-up: "rusty_weapon_outcome_success" at 14:00
9. Event screen closes, return to game

10. Time advances: 14:00 (2 hours later)
11. Queued event fires: "rusty_weapon_outcome_success"
12. Event screen opens:
    ═══════════════════════════════════
        LANCE LEADER'S NOTICE
    ───────────────────────────────────
    "Fine craftsmanship, soldier!"
    
    +50 Gold | +Reputation
    
    [Continue]
    ═══════════════════════════════════

13. Player clicks [Continue]
14. Apply rewards: +50 Gold, +1 Reputation
15. Chain complete
```

### Example 3: Decision Event → Duty Chain
```
1. Player opens Decisions menu
2. Events section shows: "Lance Leader Summons You"
3. Player selects event
4. Event screen opens:
   ═══════════════════════════════════
       LANCE LEADER'S SUMMONS
   ───────────────────────────────────
   "I need a volunteer for dangerous
   patrol work. Interested?"
   
   [Accept] (Courage)
   [Decline politely]
   ═══════════════════════════════════

5. Player chooses [Accept]
6. Event outcome: +Reputation, assigns "Dangerous Patrol" duty tomorrow

7. Next day, Morning block: "Dangerous Patrol" executes
8. Event mapping: DangerousPatrol → "ambush_encounter"
9. Full event screen for ambush encounter
10. Chain continues based on player choices...
```

---

## Data File Organization

```
ModuleData/Enlisted/
├── Activities/
│   └── activities.json (Training & Social only)
├── Events/
│   ├── duty_event_mappings.json (Duty → Event pools)
│   ├── duty_events_work_detail.json
│   ├── duty_events_patrol.json
│   ├── duty_events_sentry.json
│   ├── duty_events_training.json
│   └── duty_events_foraging.json
└── Strings/
    └── enlisted_strings.xml (All localized text)
```

---

## Key Design Principles

### 1. Backward Compatibility
- System works even if mappings/events are missing
- Falls back to simple popups gracefully
- Existing saves continue to work

### 2. Modularity
- Each system can be enabled/disabled independently
- Events are data-driven (no hardcoded content)
- Easy to add new events without code changes

### 3. Player Agency
- Always offer meaningful choices
- Skills determine success, not RNG alone
- Consequences are clear from tooltips

### 4. Narrative Cohesion
- Events feel connected to military life
- Chains create story arcs over time
- Outcomes reference previous choices

### 5. Performance
- Menu refresh <50ms
- Event queue scan <1ms per hour tick
- Minimal save file bloat

---

## Testing Checklist

### Main Menu
- [ ] Schedule displays all 4 time blocks
- [ ] Current time block highlighted correctly
- [ ] Schedule updates on hourly tick
- [ ] Orders section shows/hides correctly
- [ ] All text fits without overflow

### Decisions Menu
- [ ] Events section shows pushed events
- [ ] Training section shows activities
- [ ] Social section shows activities
- [ ] Time restrictions enforced
- [ ] Separators display correctly

### Duty Events
- [ ] Duty triggers mapped event correctly
- [ ] Fallback to popup works
- [ ] Event screen displays all options
- [ ] Skill checks calculated correctly
- [ ] Success/failure outcomes apply

### Event Chains
- [ ] Follow-up events schedule correctly
- [ ] Queued events fire at right time
- [ ] Multiple queued events work
- [ ] Chains persist through save/load
- [ ] Chains work during battles/menus

### Orders System
- [ ] Orders appear when created
- [ ] Orders expire correctly
- [ ] Empty state shows header
- [ ] Clicking orders navigates correctly
- [ ] Orders persist through save/load

---

## Success Metrics

- **Player Engagement**: Duty completion rate increases by 30%
- **Event Interaction**: 80%+ of triggered events result in player choice (not ignored)
- **Chain Completion**: 70%+ of event chains complete (no early exits)
- **Performance**: Menu refresh <50ms, event queue <1ms/tick
- **Stability**: No crashes related to events/menus in 10+ hour playthroughs

---

## Future Enhancements (Post-Launch)

### Phase 8+: Advanced Features
- **Conditional Chains**: Next event depends on player state, not just outcome
- **Multi-NPC Events**: Events involving multiple characters (companions, lords)
- **Reputation Effects**: Event outcomes affect specific NPC relationships
- **Dynamic Difficulty**: Skill checks adjust based on player level
- **Timed Choices**: Player must respond within X hours or default occurs
- **Event Modding API**: Allow community to create custom duty events

---

## Related Documentation

- **Event Story Catalog**: `docs/Features/Events/duty-events-catalog.md` - All event content and chains
- **Technical Architecture**: `docs/Features/Events/duty-events-architecture.md` - Deep dive on event system
- **Menu Layout Spec**: `docs/Features/Interface/main-menu-layout.md` - Detailed menu structure

---

## Implementation Timeline

**Total Estimated Effort**: 40-55 hours

- **Phase 1** (Main Menu): 4-6 hours
- **Phase 2** (Decisions Menu): 3-4 hours
- **Phase 3** (Duty Mapping): 4-5 hours
- **Phase 4** (Event Chains): 6-8 hours
- **Phase 5** (Content Creation): 10-15 hours
- **Phase 6** (Orders System): 6-8 hours
- **Phase 7** (Polish): 4-6 hours

**Recommended Approach**: Implement in order, test after each phase, deploy incrementally.

---

## Summary

This master plan unifies three major systems:
1. **Main Menu Redesign** - Show status, schedule, and orders clearly
2. **Decisions Menu** - Organize events, training, and social activities
3. **Duty Events** - Transform duties into interactive narrative chains

Together, they create a cohesive, engaging experience where being an enlisted soldier feels dynamic and consequential. The player's character build matters, choices have weight, and the daily grind becomes interesting through narrative depth and meaningful agency.

