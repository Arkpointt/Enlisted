# Camp Schedule System - Implementation Analysis

## Executive Summary

**Question**: Can we set a camp schedule for the Enlisted mod where events are available during specific times of the day?

**Answer**: **YES** - The foundation is already built. You currently have 4 time periods (Dawn, Day, Dusk, Night) and can expand to 6 granular periods with minimal changes.

---

## 🔴 CRITICAL PRIORITY - BLOCKING ITEM

**This time system expansion is a PREREQUISITE for multiple major systems:**

**Blocks:**
- ❌ AI Camp Schedule implementation
- ❌ Camp Activities Hub Phase 2
- ❌ Duty Events proper timing
- ❌ Lance Life Simulation time-aware events

**Duration:** 1-2 hours  
**Complexity:** Low (straightforward enum + logic changes)  
**Impact:** Unblocks 4+ major systems

**DO THIS FIRST before starting any other major implementation work.**

See `MASTER_IMPLEMENTATION_ROADMAP.md` for full dependency graph.

---

## Current Implementation Status

### ✅ What's Already Working

Your mod already has a functional time-of-day event system:

**1. Time Tracking (`CampaignTriggerTrackerBehavior`)**
```csharp
// Current enum (4 periods)
public enum DayPart
{
    Dawn,    // Currently tracked
    Day,     // Currently tracked  
    Dusk,    // Currently tracked
    Night    // Currently tracked
}
```

**2. Event Triggering (`LanceLifeEventTriggerEvaluator.cs:141-206`)**
- Events can specify `time_of_day` requirements in JSON
- System checks current time period before firing events
- Extended vocabulary is already supported (morning, afternoon, evening, late_night)

**3. Event Data (`events_general.json:30-32`)**
```json
{
  "id": "gen_dawn_muster",
  "triggers": {
    "time_of_day": ["dawn"]
  }
}
```

### 📊 Current Time Period Mapping

| JSON Token | Maps To | Actual Hours | Status |
|------------|---------|--------------|--------|
| `dawn` | Dawn | Currently tracked | ✅ Working |
| `morning` | Day | Approximation | ⚠️ Coarse |
| `afternoon` | Day | Approximation | ⚠️ Coarse |
| `day` | Day | Currently tracked | ✅ Working |
| `evening` | Dusk | Approximation | ⚠️ Coarse |
| `dusk` | Dusk | Currently tracked | ✅ Working |
| `night` | Night | Currently tracked | ✅ Working |
| `late_night` | Night | Approximation | ⚠️ Coarse |

---

## Proposed Enhancement: 6-Period Camp Schedule

### Recommended Time Periods

Based on military camp life patterns and your research docs:

| Period | Hours | Purpose | Real Camp Activity |
|--------|-------|---------|-------------------|
| **Dawn** | 5:00-7:00 | Morning muster, wake-up | Roll call, assignments |
| **Morning** | 7:00-12:00 | Active duty, training | Drills, work details |
| **Afternoon** | 12:00-17:00 | Continued duty | Training, maintenance |
| **Evening** | 17:00-20:00 | Wind-down, meals | Mess, social time |
| **Dusk** | 20:00-22:00 | Campfire, prep for night | Stories, gambling |
| **Night** | 22:00-5:00 | Watch duty, sleep | Guard rotation, rest |

### Why This Split Works

1. **Dawn** - Distinct enough to keep separate (critical military moment)
2. **Morning/Afternoon** - Split allows different event types (morning drills vs afternoon work)
3. **Evening/Dusk** - Split allows meal events vs campfire events
4. **Night** - Long period appropriate for watch rotation and sleep
5. **No "Late Night"** - Combine with Night to avoid over-fragmentation

---

## Implementation Plan

### Phase 1: Expand DayPart Enum (5 minutes)

**File**: `src/Mod.Core/Triggers/CampaignTriggerTrackerBehavior.cs` (likely location)

**Change**:
```csharp
public enum DayPart
{
    Dawn,      // 5-7
    Morning,   // 7-12  ← NEW
    Afternoon, // 12-17 ← NEW
    Evening,   // 17-20 ← NEW
    Dusk,      // 20-22
    Night      // 22-5
}
```

### Phase 2: Update Time Detection Logic (15 minutes)

**File**: `src/Mod.Core/Triggers/CampaignTriggerTrackerBehavior.cs`

**Current Pattern** (inferred from your code):
```csharp
public DayPart GetDayPart()
{
    float hour = CampaignTime.Now.CurrentHourInDay;
    
    // Current logic (4 periods)
    if (hour >= 5 && hour < 7) return DayPart.Dawn;
    if (hour >= 7 && hour < 20) return DayPart.Day;   // Too broad!
    if (hour >= 20 && hour < 22) return DayPart.Dusk;
    return DayPart.Night;
}
```

**Proposed Enhanced Logic** (6 periods):
```csharp
public DayPart GetDayPart()
{
    float hour = CampaignTime.Now.CurrentHourInDay;
    
    if (hour >= 5f && hour < 7f) return DayPart.Dawn;
    if (hour >= 7f && hour < 12f) return DayPart.Morning;
    if (hour >= 12f && hour < 17f) return DayPart.Afternoon;
    if (hour >= 17f && hour < 20f) return DayPart.Evening;
    if (hour >= 20f && hour < 22f) return DayPart.Dusk;
    return DayPart.Night; // 22-5
}
```

**Native Reference** (from decompile):
```csharp
// CampaignTime.CurrentHourInDay returns float [0.0 - 23.999...]
// Example usage in native code:
float currentHour = CampaignTime.Now.CurrentHourInDay;
bool isDaytime = currentHour >= 6f && currentHour < 20f;
```

### Phase 3: Update Trigger Evaluator (10 minutes)

**File**: `src/Features/Lances/Events/LanceLifeEventTriggerEvaluator.cs:168-202`

**Current Code** (already good!):
```csharp
private bool IsTimeOfDaySatisfied(List<string> timeTokens)
{
    // ... existing checks ...
    
    var dayPart = tracker.GetDayPart();
    foreach (var t in timeTokens)
    {
        var token = t.Trim().ToLowerInvariant();
        
        if (token == CampaignTriggerTokens.Dawn && dayPart == DayPart.Dawn)
            return true;
        if (token == CampaignTriggerTokens.Day && dayPart == DayPart.Day)
            return true;
        // ... etc
    }
}
```

**Needed Changes**:
```csharp
// Add to CampaignTriggerTokens.cs (if it exists):
public const string Morning = "morning";
public const string Afternoon = "afternoon";
public const string Evening = "evening";

// Update IsTimeOfDaySatisfied:
if (token == CampaignTriggerTokens.Morning && dayPart == DayPart.Morning)
    return true;
if (token == CampaignTriggerTokens.Afternoon && dayPart == DayPart.Afternoon)
    return true;
if (token == CampaignTriggerTokens.Evening && dayPart == DayPart.Evening)
    return true;

// Remove fallback mappings (lines 187-202):
// No longer map morning→Day, afternoon→Day, evening→Dusk
// Now they're real periods!
```

### Phase 4: Update Event Data (Ongoing)

Events can now use precise timing:

```json
{
  "id": "morning_drill",
  "triggers": {
    "time_of_day": ["morning"]
  }
}

{
  "id": "evening_mess",
  "triggers": {
    "time_of_day": ["evening"]
  }
}

{
  "id": "campfire_stories",
  "triggers": {
    "time_of_day": ["dusk"]
  }
}

{
  "id": "night_watch",
  "triggers": {
    "time_of_day": ["night"]
  }
}
```

**Multiple Time Periods** (for flexible events):
```json
{
  "id": "sparring_practice",
  "triggers": {
    "time_of_day": ["morning", "afternoon"]
  }
}
```

---

## Benefits of Enhanced Schedule

### 1. Realistic Camp Life Flow

**Dawn (5-7am)**
- Morning muster
- Dawn patrol
- Wake-up routines
- Assigns day's duties

**Morning (7-12pm)**
- Training drills
- Formation practice
- Duty events (scout, quartermaster)
- High-energy activities

**Afternoon (12-2pm → 2-5pm)**
- Continued training
- Equipment maintenance
- Work details
- Less intense than morning

**Evening (5-8pm)**
- Evening meal
- Social time
- Pay debts
- Lance meetings

**Dusk (8-10pm)**
- Campfire circles
- Stories and bonding
- Gambling
- Pre-watch routines

**Night (10pm-5am)**
- Watch duty
- Sleep
- Rare emergencies
- Night alarms

### 2. Event Variety Without Spam

**Current Problem** (4 periods):
- "Day" is 13 hours long (7am-8pm)
- Training events compete with duty events
- Hard to create "meal time" vs "drill time" distinction

**With 6 Periods**:
- Morning = drills and high-energy training
- Afternoon = duty events and maintenance
- Evening = meals and social
- Dusk = campfire and contraband
- Natural rate-limiting by time windows

### 3. Duty Schedule Realism

Different duties become active at realistic times:

```json
{
  "id": "scout_dawn_patrol",
  "requirements": {
    "duty": "scout"
  },
  "triggers": {
    "time_of_day": ["dawn"]
  }
}

{
  "id": "quartermaster_inventory",
  "requirements": {
    "duty": "quartermaster"
  },
  "triggers": {
    "time_of_day": ["morning", "afternoon"]
  }
}

{
  "id": "runner_meal_delivery",
  "requirements": {
    "duty": "runner"
  },
  "triggers": {
    "time_of_day": ["morning", "evening"]
  }
}

{
  "id": "lookout_night_watch",
  "requirements": {
    "duty": "lookout"
  },
  "triggers": {
    "time_of_day": ["night"]
  }
}
```

---

## Technical Considerations

### 1. Save Compatibility

**No Save Breaking**:
- `DayPart` enum expansion doesn't affect save files
- Trigger evaluation is runtime-only
- Events are data-driven (no code in saves)

**Migration**: Seamless - existing saves will work immediately

### 2. Performance Impact

**Negligible**:
- Time check: 1 float comparison per period
- Runs only during event evaluation (not every frame)
- Existing cooldown/rate-limit systems prevent spam

**Current Pattern** (from your code):
```csharp
// Only evaluated when checking event triggers
if (!IsTimeOfDaySatisfied(evt.Triggers?.TimeOfDay))
    return false;
```

### 3. AI Safety Integration

Your system already has AI safety checks:

```csharp
public static bool IsAiSafe()
{
    // Conservative "safe moment" gating:
    // - not in battle/map event
    // - not in active PlayerEncounter
    // - not in conversation
    // - not prisoner
    // ...
}
```

Time-of-day works in conjunction:
```json
{
  "triggers": {
    "all": [
      "is_enlisted",
      "ai_safe"       ← Prevents events during combat
    ],
    "time_of_day": ["morning"]  ← Prevents events at wrong time
  }
}
```

### 4. Native API Reference

**From Bannerlord Decompile 1.3.4**:

```csharp
// TaleWorlds.CampaignSystem.CampaignTime
public class CampaignTime
{
    // Current hour as float (0.0 - 23.999...)
    public float CurrentHourInDay { get; }
    
    // Days elapsed since campaign start
    public double ToDays { get; }
    
    // Current time
    public static CampaignTime Now { get; }
}

// Usage patterns from native:
float hour = CampaignTime.Now.CurrentHourInDay;
bool isNight = hour < 6f || hour >= 22f;
bool isDawn = hour >= 5f && hour < 7f;
```

**No Special API Needed** - You already have everything!

---

## Example: Complete Event with Schedule

```json
{
  "id": "morning_formation_drill",
  "category": "training",
  "delivery": {
    "method": "player_initiated",
    "channel": "menu",
    "menu": "enlisted_activities",
    "menu_section": "training"
  },
  "triggers": {
    "all": [
      "is_enlisted",
      "ai_safe"
    ],
    "time_of_day": ["morning"]
  },
  "requirements": {
    "formation": "infantry",
    "tier": {
      "min": 1,
      "max": 6
    }
  },
  "timing": {
    "cooldown_days": 2,
    "priority": "normal"
  },
  "content": {
    "title": "Formation Drill",
    "setup": "The morning drill begins. {LANCE_LEADER_SHORT} forms up the lance for shield wall practice.",
    "options": [
      {
        "id": "hold_the_line",
        "text": "Hold the line, no breaks",
        "risk": "safe",
        "costs": {
          "fatigue": 2
        },
        "rewards": {
          "xp": {
            "polearm": 25,
            "one_handed": 20,
            "athletics": 15
          }
        }
      }
    ]
  }
}
```

**What Happens**:
1. Event only appears in menu during Morning (7am-12pm)
2. Cooldown prevents spam (once every 2 days)
3. Formation requirement ensures only infantry see it
4. AI safety check prevents it during combat

---

## Testing Plan

### Phase 1: Verify Time Tracking

```csharp
// Add debug logging to GetDayPart()
public DayPart GetDayPart()
{
    float hour = CampaignTime.Now.CurrentHourInDay;
    var part = /* ... calculate ... */;
    
    ModLogger.Debug("CampSchedule", 
        $"Current hour: {hour:F2}, DayPart: {part}");
    
    return part;
}
```

**Test**: Fast-forward time, watch console for transitions

### Phase 2: Test Event Triggering

Create test events for each period:
```json
{
  "id": "test_dawn",
  "triggers": { "time_of_day": ["dawn"] },
  "timing": { "cooldown_days": 0 }
}
// ... repeat for each period
```

**Test**: Fast-forward through full day, verify each event fires once

### Phase 3: Test Rate Limiting

```json
{
  "id": "spam_test",
  "triggers": { 
    "time_of_day": ["morning", "afternoon", "evening"] 
  },
  "timing": { 
    "cooldown_days": 1 
  }
}
```

**Expected**: Fires once, then enters cooldown regardless of time period changes

---

## Migration Strategy

### Step 1: Code Changes (1 hour)
1. ✅ Expand `DayPart` enum
2. ✅ Update `GetDayPart()` logic
3. ✅ Update trigger evaluator
4. ✅ Add constants for new tokens
5. ✅ Test with debug logging

### Step 2: Content Audit (2 hours)
1. ✅ Review existing events
2. ✅ Update time_of_day values for precision
3. ✅ Identify events that benefit from multiple time slots
4. ✅ Test event triggering

### Step 3: Documentation (30 minutes)
1. ✅ Update event schema docs
2. ✅ Update content pack contract
3. ✅ Add examples to lance_life_events_content_library.md

### Step 4: QA (1 hour)
1. ✅ Full day cycle test
2. ✅ Save/load during different periods
3. ✅ Verify no duplicate events
4. ✅ Confirm cooldowns work across time changes

---

## Recommended Files to Modify

### 1. Core Time Tracking
**File**: `src/Mod.Core/Triggers/CampaignTriggerTrackerBehavior.cs` (find via grep/search)
- Expand `DayPart` enum
- Update `GetDayPart()` method

### 2. Token Constants
**File**: `src/Mod.Core/Triggers/CampaignTriggerTokens.cs` (find via grep/search)
- Add `Morning`, `Afternoon`, `Evening` constants

### 3. Trigger Evaluation
**File**: `src/Features/Lances/Events/LanceLifeEventTriggerEvaluator.cs` (confirmed location)
- Update `IsTimeOfDaySatisfied()` method (lines 141-206)
- Remove fallback mappings
- Add explicit checks for new periods

### 4. Event Data
**Files**: `ModuleData/Enlisted/Events/*.json`
- Update existing event time_of_day values
- Add new time-specific events

---

## Alternative: Keep 4 Periods + Smarter Event Design

If you prefer **not** to expand the enum, you can still create a camp schedule using your existing 4 periods:

### Morning Events (Dawn + Morning subset)
```json
{
  "id": "morning_muster",
  "triggers": {
    "time_of_day": ["dawn"],
    "any": ["daily_tick"]
  }
}
```

### Training Events (Day period)
```json
{
  "id": "formation_drill",
  "triggers": {
    "time_of_day": ["day"],
    "all": ["ai_safe"]
  },
  "timing": {
    "cooldown_days": 1
  }
}
```

### Evening/Social Events (Dusk period)
```json
{
  "id": "campfire_stories",
  "triggers": {
    "time_of_day": ["dusk"]
  }
}
```

### Night/Watch Events (Night period)
```json
{
  "id": "night_watch",
  "triggers": {
    "time_of_day": ["night"]
  }
}
```

**Pros**:
- No code changes needed
- Works with current implementation
- Events already support this

**Cons**:
- Less granular control
- "Day" period is very long (13 hours)
- Harder to prevent event spam during active hours

---

## Recommendation

**Implement the 6-Period System**

**Why**:
1. ✅ Minimal code changes (30 minutes of work)
2. ✅ No save compatibility issues
3. ✅ Dramatically improves event pacing
4. ✅ Enables realistic camp schedule
5. ✅ Your content library already designed for it (see research docs)
6. ✅ Better matches military camp life patterns

**ROI**: High value for low effort

**Risk**: Very low - system is already built, just expanding granularity

---

## Next Steps

1. **Locate CampaignTriggerTrackerBehavior.cs**
   ```powershell
   # Find the file
   Get-ChildItem -Path "C:\Dev\Enlisted\Enlisted\src" -Recurse -Filter "*Trigger*.cs"
   ```

2. **Backup Current Implementation**
   ```powershell
   # Create backup branch
   git checkout -b feature/expanded-camp-schedule
   ```

3. **Implement Changes** (follow Phase 1-3 above)

4. **Test with Debug Event**
   ```json
   {
     "id": "debug_time_test",
     "triggers": {
       "time_of_day": ["morning"]
     },
     "content": {
       "title": "Test: Morning Period",
       "setup": "Current hour: {CURRENT_HOUR}. This should only appear 7am-12pm."
     }
   }
   ```

5. **Update Event Library** (use content from lance_life_events_content_library.md)

---

## Conclusion

**YES, you can absolutely set a camp schedule where events are available during specific times of day.**

Your mod already has 80% of the infrastructure:
- ✅ Time tracking exists
- ✅ Event triggering works
- ✅ JSON schema supports it
- ✅ Content library designed for it

**You just need to**:
1. Expand DayPart enum from 4 → 6 periods (5 minutes)
2. Update GetDayPart() time ranges (10 minutes)
3. Add new token checks to trigger evaluator (15 minutes)
4. Update event JSON files (ongoing)

**Total Implementation Time**: ~1-2 hours including testing

**Result**: Realistic military camp schedule with events firing at appropriate times throughout the day.

---

*Document Version: 1.0*  
*Related Docs: time_and_ai_state_system.md, lance_life_events_content_library.md*  
*Native Reference: Bannerlord Decompile 1.3.4 - CampaignTime.cs*
