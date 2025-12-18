# Event Timing & Frequency Analysis

## Executive Summary

After investigating the event system, I found several timing/frequency issues that could lead to event spam and poor player experience:

1. **Config/Code Mismatch**: Config uses `min_hours_between_events` but code expects `min_days_between_events` and `target_days_between_events`
2. **Too Frequent Evaluation**: Events evaluate every 1 hour (wasteful, should be 6+ hours)
3. **No Enlistment Grace Period**: Events can fire immediately after enlisting (within first day if eligible)
4. **Lance Mate Events Fire Too Early**: Tier gate is 1, so they can appear right after enlistment

---

## Current Configuration

### Lance Life Events Config (`enlisted_config.json`)
```json
"lance_life_events": {
    "enabled": true,
    "events_folder": "Events",
    "automatic": {
        "enabled": true,
        "evaluation_cadence_hours": 1,           // ❌ TOO FREQUENT (every hour!)
        "max_events_per_day": 1,                 // ✓ Good
        "min_hours_between_events": 12,          // ❌ NOT USED BY CODE
        "queue_timeout_hours": 24                // ✓ Good
    },
    "onboarding": {
        "enabled": true,
        "skip_for_veterans": true,
        "stage_count": 3                         // ✓ Good
    }
}
```

### What the Code Actually Uses
From `LanceLifeEventsAutomaticConfig.cs`:
```csharp
public int EvaluationCadenceHours { get; set; } = 6;         // Default is 6, config overrides to 1
public int MaxEventsPerDay { get; set; } = 1;                // ✓ Matches config
public int MinDaysBetweenEvents { get; set; } = 3;           // ❌ NOT IN CONFIG (using default)
public int TargetDaysBetweenEvents { get; set; } = 7;        // ❌ NOT IN CONFIG (using default)
public int QueueTimeoutHours { get; set; } = 24;             // ✓ Matches config
```

### Decision Events Config (For Comparison - Better Pacing)
```json
"decision_events": {
    "pacing": {
        "max_per_day": 2,                        // More permissive (2 vs 1)
        "max_per_week": 8,                       // Has weekly limit
        "min_hours_between": 6,                  // 6 hours minimum
        "default_cooldown_days": 3,
        "default_category_cooldown_days": 1,
        "evaluation_hours": [8, 14, 20],         // Only 3 times per day!
        "allow_quiet_days": true,
        "quiet_day_chance": 0.15                 // 15% chance to skip
    },
    "tier_gates": {
        "lance_mate": 1,                         // ❌ Can fire right away
        "lance_leader_orders": 1,                // ❌ Can fire right away
        "lord_direct": 3,                        // ✓ Requires T3
        "lord_invitation": 5                     // ✓ Requires T5
    }
}
```

---

## Current Event Timing Logic

### Automatic Event Flow (from `LanceLifeEventsAutomaticBehavior.cs`)

```
Every Hour:
  ├─ If event queued → Try to fire it (if AI safe, no other popup showing)
  └─ If no queue → Evaluate if should queue new event:
      ├─ Check evaluation cadence (1 hour currently)
      ├─ Check max events per day (1 event)
      ├─ Check min days between events (3 days default)
      ├─ Check target days between events (7 days default)
      │   └─ Between 3-7 days: Only HIGH/CRITICAL priority events
      │   └─ After 7 days: Any priority can fire
      └─ Pick best eligible event (onboarding → threshold → duty → general)
```

### Priority System
- **Between 3-7 days**: Only events with `priority: "high"` or `priority: "critical"` can fire
- **After 7 days**: All priorities can fire (`priority: "normal"` included)
- **Onboarding priority**: Onboarding events take precedence until onboarding complete

### Onboarding Stages
- **Stage 1**: First events after enlisting (tracked by `onboarding_stage_1` trigger)
- **Stage 2**: Middle events (tracked by `onboarding_stage_2` trigger)
- **Stage 3**: Final onboarding (tracked by `onboarding_stage_3` trigger)
- **Complete**: Normal events start (tracked by `onboarding_complete` trigger)

Veterans (2+ enlistments) skip onboarding entirely (`skip_for_veterans: true`).

---

## Problems Identified

### 1. Config/Code Mismatch ❌
**Problem**: Config specifies `min_hours_between_events: 12` but code uses `MinDaysBetweenEvents` (days, not hours).

**Impact**: The 12-hour config setting is **completely ignored**. Code falls back to defaults (3 days min, 7 days target).

**Evidence**:
- Config: `"min_hours_between_events": 12`
- Code property: `MinDaysBetweenEvents` (completely different name/unit)

---

### 2. Too Frequent Evaluation ❌
**Problem**: Events evaluate **every 1 hour**.

**Impact**: 
- Wastes CPU cycles checking eligibility 24 times per day
- Increases chance of events firing at awkward times
- Decision Events use a smarter approach (only 3 times per day at set hours)

**Recommendation**: Increase to **6 hours** (4 times per day) minimum.

---

### 3. No Enlistment Grace Period ❌
**Problem**: Events can fire as soon as day 3 of enlistment if eligible (since `MinDaysBetweenEvents = 3`).

**Impact**: Player gets hit with events before they've even settled in:
- Day 1: Enlist, onboarding Stage 1 event fires
- Day 2-3: Onboarding Stage 2 could fire
- Day 4-7: Onboarding Stage 3, then completion
- Day 7+: Normal events start

For veterans who skip onboarding, they could see a general event on **Day 3** (too soon!).

**What Players Experience**:
- Lance mate asking for help on Day 3
- Training events appearing before they know the systems
- Decision events about camp life before they've experienced camp

---

### 4. Lance Mate Events Fire Too Early ❌
**Problem**: `tier_gates.lance_mate: 1` means lance mate events can fire at T1 (recruit), right after enlistment.

**Impact**: 
- "Your lance mate asks you for help with X" events appear when player is brand new
- Breaks immersion - why would established soldiers ask a green recruit for help?

**Recommendation**: Increase lance mate gate to T2 (minimum) or add explicit "days since enlistment" filters.

---

### 5. Per-Event Cooldowns Are Short ❌
**Example from `events_general.json`**:
```json
"timing": {
    "cooldown_days": 3,
    "priority": "normal",
    "one_time": false
}
```

**Problem**: Most events have 3-day cooldowns. Combined with the 3-day global minimum, events can repeat quickly.

**Impact**: Same event could appear on Day 3, Day 6, Day 9, etc. (feels repetitive).

---

## Recommended Solutions

### 1. Fix Config/Code Mismatch ✅

**Add missing config properties to `enlisted_config.json`:**
```json
"automatic": {
    "enabled": true,
    "evaluation_cadence_hours": 6,              // ✅ Every 6 hours (4x per day)
    "max_events_per_day": 1,                    // ✅ Keep at 1
    "min_days_between_events": 5,               // ✅ NEW: Minimum 5 days between ANY events
    "target_days_between_events": 10,           // ✅ NEW: Target 10 days (events prefer this interval)
    "queue_timeout_hours": 24                   // ✅ Keep at 24
}
```

**Remove obsolete property:**
- Remove `"min_hours_between_events": 12` (not used by code)

**Rationale**: 
- 5 days minimum prevents spam (up from 3 days)
- 10 days target creates natural rhythm (up from 7 days)
- Between days 5-10: Only high-priority events fire
- After day 10: Normal events can fire
- With max 1 per day, player sees ~3 events per month (good pacing)

---

### 2. Add Enlistment Grace Period ✅

**Add new config property:**
```json
"automatic": {
    // ... existing properties ...
    "enlistment_grace_days": 7                  // ✅ NEW: No events for first 7 days
}
```

**Code change in `LanceLifeEventsAutomaticBehavior.cs`:**
```csharp
private void TryEvaluateAndQueue(EnlistmentBehavior enlistment)
{
    var cfg = ConfigurationManager.LoadLanceLifeEventsConfig() ?? new LanceLifeEventsConfig();
    var auto = cfg.Automatic ?? new LanceLifeEventsAutomaticConfig();
    
    // NEW: Enlistment grace period
    var graceDays = Math.Max(0, auto.EnlistmentGraceDays);
    if (graceDays > 0)
    {
        var daysSinceEnlistment = LanceLifeOnboardingBehavior.Instance?.DaysSinceEnlistment ?? 0;
        if (daysSinceEnlistment < graceDays)
        {
            return; // Too soon - player is still getting oriented
        }
    }
    
    // ... rest of existing code ...
}
```

**Rationale**:
- Gives players a week to learn systems before events fire
- Onboarding events can still fire (they bypass this check if needed)
- Veterans get 7 quiet days to reorient themselves

---

### 3. Adjust Tier Gates ✅

**Update `decision_events.tier_gates` in `enlisted_config.json`:**
```json
"tier_gates": {
    "enabled": true,
    "lance_mate": 2,                            // ✅ UP FROM 1: Need T2 for lance mate requests
    "lance_leader_orders": 1,                   // ✅ Keep at 1 (direct orders from leader)
    "lord_direct": 3,                           // ✅ Keep at 3
    "lord_invitation": 5,                       // ✅ Keep at 5
    "situation": 1                              // ✅ Keep at 1 (environmental events)
}
```

**Rationale**:
- Lance mate requests should require T2 (veteran) - you need to prove yourself first
- Lance leader orders can stay T1 (they give orders to everyone)
- Situation events stay T1 (they're environmental, not social)

---

### 4. Increase Per-Event Cooldowns ✅

**Update event cooldowns in event JSON files:**

**General Events** (currently 3 days → 7 days):
```json
"timing": {
    "cooldown_days": 7,                         // ✅ UP FROM 3
    "priority": "normal"
}
```

**Duty Events** (currently 3 days → 5 days):
```json
"timing": {
    "cooldown_days": 5,                         // ✅ UP FROM 3
    "priority": "normal"
}
```

**High-Priority Events** (keep shorter cooldowns):
```json
"timing": {
    "cooldown_days": 3,                         // ✅ Keep short for urgent events
    "priority": "high"
}
```

**Rationale**:
- General events feel fresh when they appear every 1-2 weeks, not twice per week
- Duty events can be more frequent (job-related content)
- High-priority events keep short cooldowns (they're meant to fire when conditions met)

---

### 5. Add Explicit "Days Since Enlistment" Filters ✅

**Update events that shouldn't fire early:**

**Example: Lance mate requesting help**
```json
"triggers": {
    "all": [
        "is_enlisted",
        "ai_safe",
        "days_since_enlistment >= 14"           // ✅ NEW: Wait 2 weeks after enlisting
    ]
}
```

**Example: Camp politics event**
```json
"triggers": {
    "all": [
        "is_enlisted",
        "ai_safe",
        "days_since_enlistment >= 7"            // ✅ NEW: Wait 1 week after enlisting
    ]
}
```

**Categories that should have enlistment filters:**
- **Lance mate requests**: `>= 14 days` (need to establish yourself first)
- **Camp politics**: `>= 7 days` (need to learn the lay of the land)
- **Training challenges**: `>= 3 days` (need basic orientation first)
- **Situation events**: No filter (can happen anytime)
- **Duty events**: `>= 7 days` (need to prove competence first)

**Onboarding events**: Use `onboarding_stage_X` triggers (already implemented correctly).

---

## Recommended Timeline (First Enlistment)

```
Day 1:   Enlist, bag check
Days 2-3: Quiet (grace period)
Day 4:   Onboarding Stage 1 event fires (welcome to the lance)
Days 5-6: Quiet
Day 7:   Onboarding Stage 2 event fires (learning the ropes)
Days 8-9: Quiet (grace period ends)
Day 10:  Onboarding Stage 3 event fires (first real challenge)
Days 11-14: Quiet
Day 15+: First normal event can fire (minimum 5 days from last event)
Day 20+: Second normal event fires
Day 30+: Third normal event fires
```

**Result**: ~1 event per week on average (feels like CK3/narrative games, not an event grinder).

---

## Recommended Timeline (Veteran Re-enlistment)

```
Day 1:   Enlist (onboarding skipped for veterans)
Days 2-7: Grace period (no events)
Day 8+:  First event can fire (minimum 5 days between events)
Day 13+: Second event fires
Day 23+: Third event fires
```

**Result**: Even veterans get a breather before events start.

---

## Implementation Priority

### Priority 1: Critical Fixes (Do Now)
1. ✅ Fix config/code mismatch: Add `min_days_between_events` and `target_days_between_events` to config
2. ✅ Change `evaluation_cadence_hours` from `1` to `6`
3. ✅ Change `min_days_between_events` to `5` (up from 3)
4. ✅ Change `target_days_between_events` to `10` (up from 7)
5. ✅ Add `enlistment_grace_days: 7` to config + implement in code

### Priority 2: Balance Improvements (Next)
1. ✅ Adjust `tier_gates.lance_mate` to `2` (up from 1)
2. ✅ Increase per-event cooldowns (3 → 7 days for general, 3 → 5 days for duty)

### Priority 3: Event Content Pass (Later)
1. ✅ Add `days_since_enlistment` filters to specific events
2. ✅ Review all lance mate events for appropriate timing
3. ✅ Review all general events for appropriate timing

---

## Testing Checklist

After implementing changes:

1. ☐ New character, first enlistment:
   - Day 1-7: No events except onboarding
   - Day 8+: Onboarding events only
   - Day 15+: First normal event
   
2. ☐ Veteran re-enlistment:
   - Day 1-7: No events at all (grace period)
   - Day 8+: First event can fire
   
3. ☐ Event frequency:
   - Should see ~1 event per 1-2 weeks
   - No events back-to-back within 5 days
   - High-priority events can fire between 5-10 days
   
4. ☐ Lance mate events:
   - Should not fire before T2 (tier gate)
   - Should not fire before 14 days of enlistment
   
5. ☐ Evaluation frequency:
   - Should only evaluate 4 times per day (every 6 hours)
   - Check logs for evaluation spam

---

## Additional Notes

### Why Not Just Copy Decision Events?
Decision Events have better pacing but serve a different purpose:
- **Decision Events**: Player-facing "big decisions" (2/day, 8/week max)
- **Lance Life Events**: Narrative flavor/atmosphere (1/day, slower cadence)

Both systems should coexist with appropriate pacing for their roles.

### Why Not Disable Events Entirely Early?
Some events (onboarding, situation) should be able to fire early:
- Onboarding events teach the player
- Situation events create atmosphere ("you see X in camp")
- Social events (lance mate requests) should wait

The grace period + tier gates + per-event filters give us fine-grained control.

---

## Conclusion

Current state: Events can fire **too often** (every 3 days) and **too early** (day 3 of enlistment).

Recommended state: Events fire **every 5-10 days** with a **7-day grace period** after enlisting.

This creates a CK3-style narrative rhythm: quiet days for player-driven action, punctuated by meaningful events that don't overwhelm.

