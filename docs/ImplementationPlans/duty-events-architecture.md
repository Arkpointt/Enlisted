# Duty Events System Architecture - Technical Reference

> **Note**: This is a technical deep-dive document. For the complete implementation plan including Main Menu and Decisions menu, see `docs/Features/enlisted-interface-master-plan.md`.

## Overview

The Duty Events System transforms scheduled duties from simple XP-granting notifications into interactive narrative events with skill checks, choices, and chained outcomes. When a player performs a duty (Work Detail, Patrol, etc.), they may encounter an event that requires decisions, and those decisions can trigger follow-up events hours or days later.

## Purpose

Provide meaningful player agency and consequence to daily military duties by:
- Offering skill-based choices during routine activities
- Creating narrative chains that unfold over time
- Rewarding player skills and punishing failures
- Making the "grunt work" of military life interesting

## System Components

### 1. Schedule System (Entry Point)
**Location**: `src/Features/Schedule/Core/ScheduleExecutor.cs`

**Current Behavior**:
- When a scheduled duty executes, shows a simple popup
- Awards XP automatically
- Player clicks "Continue"

**New Behavior**:
- When a scheduled duty executes, checks if it has an associated event
- If event exists, launches full `LanceLifeEventScreen` instead of simple popup
- Player makes choices with consequences
- Outcomes may trigger follow-up events

### 2. Event Definition System
**Location**: `src/Features/Lances/Events/LanceLifeEventCatalog.cs`

**Structure**:
```
LanceLifeEventDefinition
├── Basic Info (id, title, description)
├── Triggers (when event appears)
├── Options (player choices)
│   ├── Skill Checks (success/failure thresholds)
│   ├── Costs (fatigue, gold, etc.)
│   ├── Conditions (requirements to see option)
│   ├── Success Outcome
│   │   ├── Immediate effects (XP, gold, reputation)
│   │   └── Follow-up event (next_event, delay_hours)
│   └── Failure Outcome
│       ├── Immediate effects (penalties)
│       └── Follow-up event (next_event, delay_hours)
└── Cooldowns (prevent spam)
```

### 3. Event Behavior (Execution Engine)
**Location**: `src/Features/Lances/Events/Decisions/DecisionEventBehavior.cs`

**Responsibilities**:
- Fires events (player-initiated or automatic)
- Evaluates skill checks
- Applies outcomes (XP, gold, reputation, etc.)
- Schedules follow-up events with delays

### 4. Event Presenter (UI Layer)
**Location**: `src/Features/Lances/UI/LanceLifeEventScreen.cs`

**Responsibilities**:
- Displays event title, description, and options
- Shows skill check hints (success chance)
- Shows costs/rewards preview
- Handles player selection
- Returns control to game after choice

## Data Flow

### Simple Duty (No Event)
```
[Schedule executes Work Detail]
    ↓
[Simple popup: "You maintain equipment" + XP]
    ↓
[Player clicks Continue]
    ↓
[Duty marked complete]
```

### Duty with Event (New System)
```
[Schedule executes Work Detail]
    ↓
[Check: Does WorkDetail have event mapping?]
    ↓ YES
[Pick event from catalog: "rusty_weapon_found"]
    ↓
[Launch LanceLifeEventScreen]
    ↓
[Player sees 3 options:]
    • Repair it (Smithing 30+)
    • Quick patch (Standard)
    • Leave it (Safe)
    ↓
[Player chooses "Repair it"]
    ↓
[Skill check: Player has Smithing 45 → SUCCESS]
    ↓
[Apply immediate effects: -2 Fatigue, +15 Smithing XP]
    ↓
[Schedule follow-up: "rusty_weapon_outcome_success" in 2 hours]
    ↓
[Close event screen, return to game]
    ↓
[2 hours pass...]
    ↓
[Auto-fire "rusty_weapon_outcome_success"]
    ↓
[New event screen: "Lance Leader notices your work"]
    ↓
[Rewards: +50 Gold, +Reputation]
    ↓
[Player clicks Continue]
    ↓
[Chain complete]
```

## Event Triggering Mechanisms

### 1. Duty-Triggered Events (New)
**Trigger**: Scheduled duty executes
**Mapping**: Duty type → Event ID
**Example**: WorkDetail → "rusty_weapon_found"

**Implementation**:
```csharp
// In ScheduleExecutor.cs
private static void TriggerScheduleEvent(ScheduledBlock block)
{
    // New: Check for event mapping
    var eventId = GetEventIdForDuty(block.BlockType, block.ActivityId);
    
    if (!string.IsNullOrEmpty(eventId))
    {
        // Launch full event screen
        DecisionEventBehavior.Instance?.FireEvent(eventId);
    }
    else
    {
        // Fallback to simple popup
        ShowSimplePopup(block);
    }
}
```

### 2. Player-Initiated Events (Existing)
**Trigger**: Player selects from Decisions menu
**Example**: "Request Better Equipment", "Volunteer for Patrol"

### 3. Automatic Push Events (Existing)
**Trigger**: Daily tick, triggers checked
**Example**: "Lance Leader Summons", "Dispute Breaks Out"

### 4. Chained Events (New/Enhanced)
**Trigger**: Previous event outcome schedules it
**Example**: "rusty_weapon_outcome_success" fires 2 hours after "rusty_weapon_found"

## Implementation Phases

### Phase 1: Event Mapping System
**Goal**: Connect duty types to event IDs

**Tasks**:
1. Create `DutyEventMapping.json` config file
2. Add mapping loader to `ScheduleExecutor`
3. Modify `TriggerScheduleEvent()` to check for mappings
4. Fall back to simple popup if no mapping exists

**Example Config**:
```json
{
  "schemaVersion": 1,
  "mappings": [
    {
      "duty_type": "WorkDetail",
      "event_pool": [
        { "event_id": "rusty_weapon_found", "weight": 1.0 },
        { "event_id": "broken_wagon_wheel", "weight": 0.8 },
        { "event_id": "sharpening_stones", "weight": 0.5 }
      ]
    },
    {
      "duty_type": "PatrolDuty",
      "event_pool": [
        { "event_id": "suspicious_tracks", "weight": 1.0 },
        { "event_id": "lost_traveler", "weight": 0.7 }
      ]
    }
  ]
}
```

**Acceptance Criteria**:
- Duties with mappings launch full event screen
- Duties without mappings use simple popup
- Event selection is weighted/random from pool
- No crashes if config is missing

---

### Phase 2: Follow-Up Event Scheduling
**Goal**: Allow events to schedule delayed outcomes

**Tasks**:
1. Add `next_event` and `next_event_delay_hours` to outcome schema
2. Implement event queue in `DecisionEventBehavior`
3. Add hourly tick check for queued events
4. Fire queued events when time arrives

**Schema Changes**:
```json
{
  "options": [
    {
      "id": "repair_properly",
      "skill_check": { "skill": "Smithing", "difficulty": 30 },
      "success_outcome": {
        "effects": {
          "fatigue": -2,
          "skill_xp": { "Smithing": 15 }
        },
        "next_event": "rusty_weapon_outcome_success",
        "next_event_delay_hours": 2
      }
    }
  ]
}
```

**Implementation**:
```csharp
// In DecisionEventBehavior.cs
private List<ScheduledEvent> _eventQueue = new List<ScheduledEvent>();

public void ScheduleEvent(string eventId, float delayHours)
{
    var fireTime = CampaignTime.Now + CampaignTime.Hours(delayHours);
    _eventQueue.Add(new ScheduledEvent(eventId, fireTime));
}

private void OnHourlyTick()
{
    var now = CampaignTime.Now;
    foreach (var scheduled in _eventQueue.Where(e => e.FireTime <= now))
    {
        FireEvent(scheduled.EventId);
    }
    _eventQueue.RemoveAll(e => e.FireTime <= now);
}
```

**Acceptance Criteria**:
- Events can specify follow-up events in outcomes
- Follow-ups fire after correct delay
- Multiple queued events don't conflict
- Queued events persist through save/load
- Events fire even if player is in different menu

---

### Phase 3: Skill Check Enhancement
**Goal**: Add visual feedback for skill checks

**Tasks**:
1. Calculate player's actual skill level
2. Compare to event's difficulty threshold
3. Show success chance hint ("High", "Moderate", "Low", "Very Low")
4. Color-code options based on success chance

**UI Enhancement**:
```
[Repair it properly] (Smithing 30+)
  Success Chance: High (Your skill: 45)
  Cost: 2 Fatigue
  Success: Quality weapon, +Gold
  Failure: Weapon breaks
```

**Implementation**:
```csharp
private string GetSuccessChanceHint(int playerSkill, int difficulty)
{
    var diff = playerSkill - difficulty;
    if (diff >= 20) return "Very High";
    if (diff >= 10) return "High";
    if (diff >= 0) return "Moderate";
    if (diff >= -10) return "Low";
    return "Very Low";
}
```

**Acceptance Criteria**:
- Options show success chance hints
- Player's actual skill is visible
- Color coding: Green (high), Yellow (moderate), Red (low)
- Works for all skill types (combat, social, crafting)

---

### Phase 4: Event Content Creation
**Goal**: Create 20-30 duty events with chains

**Tasks**:
1. Write event JSON definitions
2. Create follow-up events for outcomes
3. Test all chains end-to-end
4. Balance XP/gold rewards
5. Add localization strings

**Event Categories**:
- Work Detail events (5-7 events)
- Patrol Duty events (4-6 events)
- Sentry Duty events (3-5 events)
- Training Drill events (3-4 events)
- Foraging events (2-3 events)
- Scouting events (2-3 events)

**Acceptance Criteria**:
- Each duty type has 3+ associated events
- Each event has 2-4 player options
- Options have skill checks where appropriate
- Chains are 2-3 events deep maximum
- No dead-end chains (all chains resolve)

---

### Phase 5: Testing & Polish
**Goal**: Ensure stability and balance

**Tasks**:
1. Test all event chains
2. Verify skill check math
3. Balance rewards/penalties
4. Test save/load with queued events
5. Performance profiling
6. Edge case testing (missing data, invalid IDs)

**Test Scenarios**:
- Player with 0 skill attempts skill check
- Player with max skill attempts skill check
- Multiple queued events fire at same time
- Event fires while player in menu
- Event fires during battle
- Save game with queued events
- Load game, verify events fire
- Event catalog missing/corrupted

**Acceptance Criteria**:
- No crashes in any scenario
- Skill checks feel fair
- Rewards are balanced
- Queued events persist correctly
- Performance impact <5ms per frame

---

## Technical Details

### Event Queue Persistence
```csharp
// In DecisionEventBehavior.cs
[SaveableField(1)]
private List<ScheduledEventData> _queuedEvents = new();

public class ScheduledEventData
{
    [SaveableField(1)] public string EventId;
    [SaveableField(2)] public CampaignTime FireTime;
}
```

### Duty-to-Event Mapping
```csharp
// In ScheduleExecutor.cs
private static string PickEventForDuty(ScheduleBlockType dutyType)
{
    var mapping = DutyEventMappingLoader.GetMappingFor(dutyType);
    if (mapping == null || mapping.EventPool.Count == 0)
        return null;
    
    // Weighted random selection
    var totalWeight = mapping.EventPool.Sum(e => e.Weight);
    var roll = Random.NextDouble() * totalWeight;
    
    var cumulative = 0.0;
    foreach (var entry in mapping.EventPool)
    {
        cumulative += entry.Weight;
        if (roll <= cumulative)
            return entry.EventId;
    }
    
    return mapping.EventPool[0].EventId; // fallback
}
```

### Follow-Up Event Scheduling
```csharp
// After player makes choice and outcome is applied
var outcome = option.GetOutcome(wasSuccess);
if (!string.IsNullOrEmpty(outcome.NextEvent))
{
    DecisionEventBehavior.Instance.ScheduleEvent(
        outcome.NextEvent,
        outcome.NextEventDelayHours
    );
}
```

## Edge Cases

### Player Skips Time
**Issue**: Player uses "Wait Here" or fast travel, skipping past queued event times
**Solution**: Check queue immediately when time advances, fire all due events

### Event Fires During Battle
**Issue**: Can't show event screen during combat
**Solution**: Defer event until after battle, add to queue with 0 delay

### Multiple Events Queued at Same Time
**Issue**: Multiple follow-ups fire simultaneously
**Solution**: Queue them as separate entries, fire sequentially with 1-second delays

### Event Definition Missing
**Issue**: Queued event ID doesn't exist in catalog
**Solution**: Log warning, remove from queue, no crash

### Duty Mapping Missing
**Issue**: New duty type added but no event mapping
**Solution**: Fall back to simple popup (current behavior)

## Performance Considerations

- **Event Queue**: O(n) scan per hour tick, max 10-20 entries
- **Mapping Lookup**: Hash map, O(1) lookup
- **Event Selection**: O(n) weighted random, max 5-10 events per pool
- **Impact**: Minimal, <1ms per duty execution

## Dependencies

- `ScheduleExecutor` - Entry point for duty execution
- `DecisionEventBehavior` - Event firing and queue management
- `LanceLifeEventCatalog` - Event definitions
- `LanceLifeEventScreen` - UI presentation
- `EnlistmentBehavior` - Player state (skills, reputation, etc.)

## Related Systems

- Schedule System - Duty assignment and execution
- Camp Activities - Player-initiated actions (Training, Social)
- Decision Events - Player-initiated decisions
- Lance Life Events - Narrative event framework

## Future Enhancements

- **Dynamic difficulty**: Adjust skill checks based on player level
- **Reputation effects**: Event outcomes affect lord/lance reputation
- **Multi-choice chains**: Events with 3+ outcomes leading to different chains
- **Conditional follow-ups**: Next event depends on player state, not just outcome
- **Timed choices**: Player must respond within X hours or default occurs

