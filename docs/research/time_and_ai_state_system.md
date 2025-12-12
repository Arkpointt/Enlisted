# Time-of-Day and AI-State Event System

This document specifies how events integrate with the campaign's time cycle and the lord's AI behavior. Events should feel natural — dawn events at dawn, night events at night — and never break immersion by firing when the army is mid-charge.

---

## Table of Contents

1. [Design Philosophy](#design-philosophy)
2. [Time-of-Day System](#time-of-day-system)
3. [AI State Detection](#ai-state-detection)
4. [Safety Checks](#safety-checks)
5. [Time-Based Event Categories](#time-based-event-categories)
6. [AI-State Event Categories](#ai-state-event-categories)
7. [Combined Trigger Examples](#combined-trigger-examples)
8. [Implementation Reference](#implementation-reference)

---

## Design Philosophy

### Core Principles

1. **Events respect the fiction** — Dawn events fire at dawn, not midnight. Night watch happens at night.

2. **Never break the AI** — If the lord is chasing an enemy or about to siege, no popups. The player is along for the ride.

3. **Safe windows only** — Events fire during lulls: patrolling, waiting, camped, marching without contact.

4. **Time creates variety** — Different events for different times of day makes camp life feel real.

5. **AI state creates context** — "Patrolling" feels different from "racing to siege." Events should reflect that.

### The Safety Hierarchy

```
CAN EVENT FIRE?
    │
    ▼
1. Is player safe? (not prisoner, not in battle, not in conversation)
    │ NO → Block
    ▼ YES
2. Is lord AI safe? (not engaging, not pursuing, not in siege assault)
    │ NO → Block
    ▼ YES
3. Is time-of-day appropriate for this event?
    │ NO → Block
    ▼ YES
4. Are other conditions met? (triggers, cooldowns, etc.)
    │ NO → Block
    ▼ YES
FIRE EVENT
```

---

## Time-of-Day System

### Time Periods

Bannerlord's day cycle divided into event-relevant periods:

| Period | Hours | Description | Event Types |
|--------|-------|-------------|-------------|
| **Dawn** | 5:00–7:00 | Sun rising, camp waking | Morning muster, wake-up events |
| **Morning** | 7:00–12:00 | Active duty time | Training, duty events, work |
| **Afternoon** | 12:00–17:00 | Continued duty | Training, duty events, work |
| **Evening** | 17:00–20:00 | Duties winding down | Social events, meals |
| **Dusk** | 20:00–22:00 | Sun setting, fires lit | Campfire events, stories |
| **Night** | 22:00–2:00 | Most asleep, watches set | Night watch, quiet events |
| **Late Night** | 2:00–5:00 | Deep night, skeleton watch | Rare events, emergencies only |

### Time Detection

```csharp
public enum TimeOfDay
{
    Dawn,       // 5-7
    Morning,    // 7-12
    Afternoon,  // 12-17
    Evening,    // 17-20
    Dusk,       // 20-22
    Night,      // 22-2
    LateNight   // 2-5
}

public static TimeOfDay GetCurrentTimeOfDay()
{
    float hour = CampaignTime.Now.CurrentHourInDay;
    
    if (hour >= 5 && hour < 7) return TimeOfDay.Dawn;
    if (hour >= 7 && hour < 12) return TimeOfDay.Morning;
    if (hour >= 12 && hour < 17) return TimeOfDay.Afternoon;
    if (hour >= 17 && hour < 20) return TimeOfDay.Evening;
    if (hour >= 20 && hour < 22) return TimeOfDay.Dusk;
    if (hour >= 22 || hour < 2) return TimeOfDay.Night;
    return TimeOfDay.LateNight; // 2-5
}
```

### Time-Gated Events

Events specify when they can fire:

```json
{
  "id": "dawn_muster",
  "time_of_day": ["dawn"],
  "title": "Morning Muster",
  "setup": "The sergeant's voice cuts through the morning mist..."
}

{
  "id": "night_watch",
  "time_of_day": ["night", "late_night"],
  "title": "Night Watch",
  "setup": "Your turn on watch. The camp sleeps around you..."
}

{
  "id": "sparring_circle",
  "time_of_day": ["morning", "afternoon"],
  "title": "Sparring Circle",
  "setup": "A circle forms near the cook fires..."
}

{
  "id": "campfire_stories",
  "time_of_day": ["dusk", "evening"],
  "title": "Fire Circle",
  "setup": "The fire crackles as soldiers gather..."
}
```

---

## AI State Detection

### Lord AI Behavior States

Bannerlord's `MobileParty` exposes AI behavior:

| AI State | Field | Meaning | Events OK? |
|----------|-------|---------|------------|
| **Patrolling** | `DefaultBehavior == PatrolAroundPoint` | Routine patrol | ✅ Yes |
| **Escorting** | `DefaultBehavior == EscortParty` | Escorting someone | ⚠️ Caution |
| **Going to Settlement** | `DefaultBehavior == GoToSettlement` | Traveling to town/castle | ✅ Yes (if not rushed) |
| **Engaging** | `ShortTermBehavior == EngageParty` | Actively fighting | ❌ No |
| **Pursuing** | `TargetParty != null && hostile` | Chasing enemy | ❌ No (if close) |
| **Fleeing** | `DefaultBehavior == FleeToPoint` | Running away | ❌ No |
| **Besieging** | `BesiegedSettlement != null` | Siege in progress | ⚠️ Special rules |
| **Defending** | `DefaultBehavior == DefendSettlement` | Defending a settlement | ⚠️ Caution |
| **Raiding** | Raiding a village | Raid in progress | ⚠️ Special rules |

### AI State Categories

Group AI states into event-safety categories:

```csharp
public enum ArmyActivityState
{
    // Safe for most events
    Idle,           // Camped, no target
    Patrolling,     // Routine patrol
    Traveling,      // Going somewhere, no urgency
    
    // Limited events OK
    Besieging,      // Siege in progress (siege events OK)
    Waiting,        // Waiting in settlement
    
    // No events
    Engaging,       // In combat or about to be
    Pursuing,       // Chasing enemy (close)
    Fleeing,        // Running away
    Raiding,        // Mid-raid
    
    // Unknown/Other
    Unknown
}
```

### AI State Detection Logic

```csharp
public static ArmyActivityState GetLordActivityState(MobileParty lordParty)
{
    // Check for active engagement first (highest priority block)
    if (IsActivelyEngaging(lordParty))
        return ArmyActivityState.Engaging;
    
    // Check for close pursuit
    if (IsPursuingEnemy(lordParty))
        return ArmyActivityState.Pursuing;
    
    // Check for fleeing
    if (lordParty.DefaultBehavior == AiBehavior.FleeToPoint ||
        lordParty.DefaultBehavior == AiBehavior.FleeToGate)
        return ArmyActivityState.Fleeing;
    
    // Check for siege
    if (lordParty.BesiegedSettlement != null)
        return ArmyActivityState.Besieging;
    
    // Check for raiding
    if (IsRaidingVillage(lordParty))
        return ArmyActivityState.Raiding;
    
    // Check for waiting in settlement
    if (lordParty.CurrentSettlement != null)
        return ArmyActivityState.Waiting;
    
    // Check for patrolling
    if (lordParty.DefaultBehavior == AiBehavior.PatrolAroundPoint)
        return ArmyActivityState.Patrolling;
    
    // Check for traveling
    if (lordParty.DefaultBehavior == AiBehavior.GoToSettlement ||
        lordParty.DefaultBehavior == AiBehavior.GoToPoint)
        return ArmyActivityState.Traveling;
    
    // Default to idle if no clear behavior
    if (lordParty.TargetParty == null && lordParty.TargetSettlement == null)
        return ArmyActivityState.Idle;
    
    return ArmyActivityState.Unknown;
}

private static bool IsActivelyEngaging(MobileParty lordParty)
{
    // Short-term engage behavior
    if (lordParty.ShortTermBehavior == AiBehavior.EngageParty)
        return true;
    
    // Check army's short-term behavior too
    if (lordParty.Army?.LeaderParty?.ShortTermBehavior == AiBehavior.EngageParty)
        return true;
    
    return false;
}

private static bool IsPursuingEnemy(MobileParty lordParty)
{
    // Check direct target
    var targetParty = lordParty.TargetParty;
    if (targetParty != null)
    {
        bool isHostile = FactionManager.IsAtWarAgainstFaction(
            lordParty.MapFaction, targetParty.MapFaction);
        
        if (isHostile)
        {
            float distance = lordParty.GetPosition2D.Distance(targetParty.GetPosition2D);
            // Within engagement range = actively pursuing
            if (distance < 15f)
                return true;
        }
    }
    
    // Check army target
    if (lordParty.Army != null)
    {
        var armyTarget = lordParty.Army.AiBehaviorObject as MobileParty;
        if (armyTarget != null)
        {
            bool isHostile = FactionManager.IsAtWarAgainstFaction(
                lordParty.MapFaction, armyTarget.MapFaction);
            
            if (isHostile)
            {
                var armyLeader = lordParty.Army.LeaderParty;
                float distance = armyLeader.GetPosition2D.Distance(armyTarget.GetPosition2D);
                if (distance < 20f)
                    return true;
            }
        }
    }
    
    return false;
}

private static bool IsRaidingVillage(MobileParty lordParty)
{
    // Check if targeting a village for raid
    if (lordParty.DefaultBehavior == AiBehavior.RaidSettlement)
        return true;
    
    // Check if army is raiding
    if (lordParty.Army?.AiBehaviorObject is Settlement settlement)
    {
        if (settlement.IsVillage && settlement.IsUnderRaid)
            return true;
    }
    
    return false;
}
```

### Army vs Party State

When the lord is in an army, check both:

```csharp
public static ArmyActivityState GetEffectiveActivityState(MobileParty lordParty)
{
    var partyState = GetLordActivityState(lordParty);
    
    // If in army, also check army state
    if (lordParty.Army != null)
    {
        var armyLeader = lordParty.Army.LeaderParty;
        var armyState = GetLordActivityState(armyLeader);
        
        // Return the more restrictive state
        return GetMoreRestrictiveState(partyState, armyState);
    }
    
    return partyState;
}

private static ArmyActivityState GetMoreRestrictiveState(
    ArmyActivityState a, ArmyActivityState b)
{
    // Priority order (most restrictive first)
    var priority = new[] {
        ArmyActivityState.Engaging,
        ArmyActivityState.Pursuing,
        ArmyActivityState.Fleeing,
        ArmyActivityState.Raiding,
        ArmyActivityState.Besieging,
        ArmyActivityState.Waiting,
        ArmyActivityState.Traveling,
        ArmyActivityState.Patrolling,
        ArmyActivityState.Idle
    };
    
    int priorityA = Array.IndexOf(priority, a);
    int priorityB = Array.IndexOf(priority, b);
    
    return priorityA < priorityB ? a : b;
}
```

---

## Safety Checks

### Master Safety Check

Before firing any event:

```csharp
public static bool CanFireEvent(string eventId, MobileParty lordParty)
{
    // 1. Player safety checks
    if (Hero.MainHero.IsPrisoner) return false;
    if (Campaign.Current.ConversationManager.IsConversationFlowActive) return false;
    if (PlayerEncounter.Current != null) return false;
    if (MapState.AtMenu) return false;  // Already in a menu
    
    // 2. Lord AI safety checks
    var activityState = GetEffectiveActivityState(lordParty);
    if (!IsActivityStateSafeForEvents(activityState, eventId))
        return false;
    
    // 3. Time-of-day check
    var eventDef = GetEventDefinition(eventId);
    if (!IsTimeOfDayValid(eventDef))
        return false;
    
    // 4. Event-specific checks (cooldowns, triggers, etc.)
    if (!MeetsEventRequirements(eventDef))
        return false;
    
    return true;
}

private static bool IsActivityStateSafeForEvents(
    ArmyActivityState state, string eventId)
{
    var eventDef = GetEventDefinition(eventId);
    
    switch (state)
    {
        case ArmyActivityState.Engaging:
        case ArmyActivityState.Pursuing:
        case ArmyActivityState.Fleeing:
            // Never fire events during active combat/chase/flee
            return false;
        
        case ArmyActivityState.Raiding:
            // Only raid-specific events
            return eventDef.AllowedDuringRaid;
        
        case ArmyActivityState.Besieging:
            // Only siege-specific events
            return eventDef.AllowedDuringSiege;
        
        case ArmyActivityState.Waiting:
        case ArmyActivityState.Patrolling:
        case ArmyActivityState.Traveling:
        case ArmyActivityState.Idle:
            // Most events OK
            return true;
        
        default:
            return false;
    }
}
```

### Event-Specific Safety Flags

Events can declare special permissions:

```json
{
  "id": "siege_trench_duty",
  "allowed_during_siege": true,
  "allowed_during_raid": false,
  "allowed_activity_states": ["besieging", "idle"],
  "blocked_activity_states": ["engaging", "pursuing", "fleeing"]
}

{
  "id": "night_watch",
  "allowed_during_siege": true,
  "allowed_during_raid": false,
  "allowed_activity_states": ["patrolling", "idle", "besieging", "traveling"]
}
```

---

## Time-Based Event Categories

### Dawn Events (5:00–7:00)

The camp wakes up. Morning routines, musters, early duty.

| Event | Type | Description |
|-------|------|-------------|
| Morning Muster | Duty | Roll call, day's orders |
| Dawn Patrol | Scout Duty | Early reconnaissance |
| Breakfast Duty | General | Help the cook |
| Morning Drill | Training | Early formation practice |
| Sick Call | Medical | Report to surgeon |
| Watch Handoff | General | Night watch ends |

**Sample Event:**

```
## Event: Morning Muster

**Time:** Dawn (5:00–7:00)
**AI States:** Idle, Patrolling, Traveling, Besieging

**Setup**
The sergeant's voice cuts through the morning mist. "On your feet! 
Muster in five!" Around you, soldiers groan and reach for boots.

**Options**

| Option | Risk | Cost | Reward | Skills |
|--------|------|------|--------|--------|
| Fall in quickly, look sharp | Safe | +1 Fatigue | +15 Leadership XP, noticed | Leadership |
| Fall in with the rest | Safe | +0 Fatigue | — | — |
| Help a struggling lance mate | Safe | +1 Fatigue | +15 Charm XP, +rep with lance | Charm |
| Oversleep (risky) | Risky | +1 Discipline | Extra rest | — |
```

### Morning/Afternoon Events (7:00–17:00)

Peak activity time. Training, duty events, work.

| Event | Type | Description |
|-------|------|-------------|
| Formation Drill | Training | Combat training |
| Sparring Circle | Training | One-on-one practice |
| Duty Events | Duty | All duty-specific events |
| Work Details | General | Labor assignments |
| Quartermaster Visit | Menu | Equipment access |
| Messenger Runs | Messenger Duty | Dispatch delivery |

### Evening Events (17:00–20:00)

Duties ending, social time begins.

| Event | Type | Description |
|-------|------|-------------|
| Evening Meal | General | Gather to eat |
| Pay Your Debts | Social | Gambling, IOUs |
| Lance Meeting | Social | Unit discussions |
| Equipment Check | General | Gear maintenance |
| Tavern (if in town) | Social | Drinking, socializing |

**Sample Event:**

```
## Event: Evening Mess

**Time:** Evening (17:00–20:00)
**AI States:** Idle, Patrolling, Waiting, Besieging

**Setup**
The cook bangs the pot. Evening meal. The line forms quickly — 
those at the back get the dregs.

**Options**

| Option | Risk | Cost | Reward | Skills |
|--------|------|------|--------|--------|
| Get in line early | Safe | None | Better portion, -1 Fatigue | — |
| Help serve | Safe | +1 Fatigue | +15 Steward XP, cook remembers | Steward |
| Trade for better food | Risky | −15 gold | Better portion, +10 Trade XP | Trade |
| Skip the line (bully) | Corrupt | +1 Discipline | Best portion, −rep with lance | — |
```

### Dusk Events (20:00–22:00)

Fire circles, stories, quiet social time.

| Event | Type | Description |
|-------|------|-------------|
| Campfire Circle | Social | Stories, bonding |
| Evening Drink | Social/Morale | Contraband possible |
| Letters Home | General | Writing, reflection |
| Gambling Circle | Social | Cards, dice |
| Night Watch Prep | General | Preparing for watch |

**Sample Event:**

```
## Event: Fire Circle

**Time:** Dusk (20:00–22:00)
**AI States:** Idle, Patrolling, Waiting, Besieging

**Setup**
The fire crackles. Soldiers gather, passing a skin of something. 
An old veteran starts a story about a battle years past.

**Options**

| Option | Risk | Cost | Reward | Skills |
|--------|------|------|--------|--------|
| Listen and learn | Safe | None | +15 Tactics XP (story), -1 Fatigue | Tactics |
| Share your own story | Risky | None | +20 Charm XP if good, embarrassment if bad | Charm |
| Accept a drink | Risky | +1 Heat | +15 Charm XP, -2 Fatigue, +bond | Charm |
| Turn in early | Safe | None | -1 Fatigue | — |
```

### Night Events (22:00–2:00)

Most asleep. Watch duty, quiet moments, rare emergencies.

| Event | Type | Description |
|-------|------|-------------|
| Night Watch | Duty/General | Guard duty |
| Can't Sleep | General | Restlessness, reflection |
| Night Alarm | Emergency | Something detected |
| Sneak Out | Corrupt | Contraband run |
| Quiet Moment | General | Solitude, thinking |

**Sample Event:**

```
## Event: Night Watch

**Time:** Night (22:00–2:00)
**AI States:** Idle, Patrolling, Traveling, Besieging
**Duty Bonus:** Scout, Lookout (enhanced options)

**Setup**
Your watch. The camp sleeps around you, fires burning low. 
The darkness beyond the perimeter is absolute.

**Options**

| Option | Risk | Cost | Reward | Skills |
|--------|------|------|--------|--------|
| Stay alert, walk the line | Safe | +2 Fatigue | +25 Scouting XP | Scouting |
| Find a spot, rest your eyes | Risky | +1 Fatigue | 20% caught → +2 Discipline | — |
| Practice with your weapon (quiet) | Safe | +2 Fatigue | +20 (weapon) XP | Weapon |
| [Scout] Extend patrol beyond perimeter | Risky | +2 Fatigue | +35 Scouting XP, +15 Athletics | Scouting, Athletics |

**Injury Chance:**
- "Find a spot" if caught: +1 Discipline, possible extra duty fatigue
- "Extend patrol": 10% chance minor injury (stumble in dark)
```

### Late Night Events (2:00–5:00)

Rare events only. Deep night, skeleton watch.

| Event | Type | Description |
|-------|------|-------------|
| Something's Wrong | Emergency | Detected threat |
| Sleepwalker | General | Someone wandering |
| Late Watch Relief | General | Watch changeover |

---

## AI-State Event Categories

### Patrolling Events

When the lord is on routine patrol, camp life is normal:

| Event Type | Available |
|------------|-----------|
| All training events | ✅ |
| All duty events | ✅ |
| Social events | ✅ |
| Time-based events | ✅ |

### Traveling Events

When moving to a destination without urgency:

| Event Type | Available |
|------------|-----------|
| Training events | ⚠️ Limited (marching drill only) |
| Duty events | ✅ Most |
| Social events | ⚠️ Limited |
| March-specific events | ✅ |

**March Events:**
- Marching songs
- Trail hazards
- "Are we there yet" grumbling
- Foraging along the way

### Besieging Events

During a siege, special siege events available:

| Event Type | Available |
|------------|-----------|
| Training events | ⚠️ Limited |
| Duty events | ✅ (Siege variants) |
| Siege-specific events | ✅ |
| Social events | ⚠️ Limited (evening) |

**Siege Events:**
- Trench duty (dawn/morning)
- Wall watch (any time)
- Siege engine work (morning/afternoon)
- Night assault prep (night)
- Sortie response (emergency)

### Waiting (In Settlement) Events

When camped in/near a settlement:

| Event Type | Available |
|------------|-----------|
| All training events | ✅ |
| All duty events | ✅ |
| Town-specific events | ✅ |
| Social events | ✅ (enhanced) |

**Settlement Events:**
- Tavern visits (evening/night)
- Market access
- Quartermaster restock
- Local recruitment
- Town patrol duty

### Blocked States

No events during:
- **Engaging** — Active combat
- **Pursuing** — Chasing enemy within 15 units
- **Fleeing** — Running away
- **Raiding** (mostly) — Only raid-completion events after

---

## Combined Trigger Examples

### Full Event Definition with Time and AI State

```json
{
  "id": "dawn_muster",
  "title": "Morning Muster",
  "type": "general_event",
  
  "time_of_day": ["dawn"],
  
  "allowed_activity_states": ["idle", "patrolling", "traveling", "besieging", "waiting"],
  "blocked_activity_states": ["engaging", "pursuing", "fleeing", "raiding"],
  
  "triggers": {
    "all": ["is_enlisted", "not_on_leave"]
  },
  
  "cooldown_days": 2,
  
  "setup": "The sergeant's voice cuts through the morning mist...",
  "options": [...]
}
```

### Night Watch with Duty Bonus

```json
{
  "id": "night_watch_assignment",
  "title": "Night Watch",
  "type": "general_event",
  
  "time_of_day": ["night"],
  
  "allowed_activity_states": ["idle", "patrolling", "besieging", "traveling"],
  
  "duty_bonus": {
    "scout": {
      "extra_options": ["extend_patrol"],
      "xp_multiplier": 1.25
    },
    "lookout": {
      "extra_options": ["signal_watch"],
      "xp_multiplier": 1.25
    }
  },
  
  "cooldown_days": 3,
  
  "setup": "Your watch. The camp sleeps around you...",
  "options": [...]
}
```

### Siege-Specific Dawn Event

```json
{
  "id": "siege_dawn_assault_prep",
  "title": "Assault Preparation",
  "type": "siege_event",
  
  "time_of_day": ["dawn"],
  
  "allowed_activity_states": ["besieging"],
  "required_activity_states": ["besieging"],
  
  "triggers": {
    "all": ["siege_ongoing", "siege_day_count > 3"]
  },
  
  "setup": "Word spreads through camp before dawn — today's the day. Ladders are being moved forward...",
  "options": [
    {
      "id": "volunteer_first_wave",
      "text": "Volunteer for the first wave",
      "risk": "risky",
      "costs": { "fatigue": 2 },
      "rewards": { "skill_xp": { "Athletics": 30, "Leadership": 20 } },
      "injury": {
        "chance": 0.35,
        "severity_weights": { "minor": 0.30, "moderate": 0.50, "severe": 0.20 }
      },
      "combat_flag": true
    },
    {
      "id": "support_wave",
      "text": "Join the support wave",
      "risk": "safe",
      "costs": { "fatigue": 2 },
      "rewards": { "skill_xp": { "Athletics": 20 } },
      "injury": {
        "chance": 0.15,
        "severity_weights": { "minor": 0.60, "moderate": 0.35, "severe": 0.05 }
      }
    },
    {
      "id": "siege_engine_crew",
      "text": "Man the siege engines",
      "risk": "safe",
      "costs": { "fatigue": 2 },
      "rewards": { "skill_xp": { "Engineering": 35 } },
      "injury": {
        "chance": 0.10,
        "severity_weights": { "minor": 0.80, "moderate": 0.20 }
      }
    }
  ]
}
```

---

## Implementation Reference

### Hourly Tick Event Check

```csharp
private void OnHourlyTick()
{
    if (!IsEnlisted()) return;
    
    var lordParty = GetEnlistedLordParty();
    if (lordParty == null) return;
    
    // Get current state
    var timeOfDay = GetCurrentTimeOfDay();
    var activityState = GetEffectiveActivityState(lordParty);
    
    // Log state for debugging
    LogDebug($"Hourly tick: Time={timeOfDay}, Activity={activityState}");
    
    // Don't even check events during unsafe states
    if (IsBlockedState(activityState))
    {
        LogDebug("Activity state blocked, skipping event check");
        return;
    }
    
    // Check for time-triggered events
    CheckTimeBasedEvents(timeOfDay, activityState);
}

private bool IsBlockedState(ArmyActivityState state)
{
    return state == ArmyActivityState.Engaging ||
           state == ArmyActivityState.Pursuing ||
           state == ArmyActivityState.Fleeing;
}
```

### Event Firing with Full Checks

```csharp
private void TryFireEvent(EventDefinition eventDef, MobileParty lordParty)
{
    // Time check
    var currentTime = GetCurrentTimeOfDay();
    if (!eventDef.TimeOfDay.Contains(currentTime))
    {
        LogDebug($"Event {eventDef.Id} blocked: wrong time ({currentTime})");
        return;
    }
    
    // Activity state check
    var activityState = GetEffectiveActivityState(lordParty);
    if (eventDef.BlockedActivityStates.Contains(activityState))
    {
        LogDebug($"Event {eventDef.Id} blocked: activity state ({activityState})");
        return;
    }
    
    if (eventDef.RequiredActivityStates.Any() && 
        !eventDef.RequiredActivityStates.Contains(activityState))
    {
        LogDebug($"Event {eventDef.Id} blocked: requires specific activity state");
        return;
    }
    
    // All other checks...
    if (!CanFireEvent(eventDef.Id, lordParty))
        return;
    
    // Fire the event
    FireEvent(eventDef);
}
```

### Continuous Safety Monitor

```csharp
// Called frequently to abort events if situation changes
private void OnMapUpdate()
{
    if (!IsEventInProgress()) return;
    
    var lordParty = GetEnlistedLordParty();
    var activityState = GetEffectiveActivityState(lordParty);
    
    // If we've entered combat/pursuit, abort any pending event
    if (IsBlockedState(activityState))
    {
        AbortPendingEvent("Army entering combat");
    }
}
```

---

## Event JSON Schema (Updated)

```json
{
  "id": "string",
  "title": "string",
  "type": "duty_event | training_event | general_event | siege_event | naval_event",
  
  "time_of_day": ["dawn", "morning", "afternoon", "evening", "dusk", "night", "late_night"],
  
  "allowed_activity_states": ["idle", "patrolling", "traveling", "waiting", "besieging"],
  "blocked_activity_states": ["engaging", "pursuing", "fleeing", "raiding"],
  "required_activity_states": [],
  
  "allowed_during_siege": false,
  "allowed_during_raid": false,
  
  "duty_required": "string | null",
  "duty_bonus": {
    "duty_id": {
      "extra_options": ["option_ids"],
      "xp_multiplier": 1.25
    }
  },
  
  "tier_min": 1,
  "tier_max": 6,
  "require_final_lance": false,
  
  "triggers": {
    "any": ["trigger_ids"],
    "all": ["trigger_ids"]
  },
  
  "cooldown_days": 3,
  "max_per_term": 10,
  
  "setup": "string",
  "options": [
    {
      "id": "string",
      "text": "string",
      "risk": "safe | risky | corrupt",
      "costs": { "fatigue": 0, "gold": 0, "heat": 0, "discipline": 0 },
      "rewards": { "skill_xp": {}, "gold": 0, "fatigue_relief": 0 },
      "injury": {
        "chance": 0.0,
        "types": ["injury_type_ids"],
        "severity_weights": { "minor": 0, "moderate": 0, "severe": 0 }
      },
      "illness": {
        "chance": 0.0,
        "type": "illness_id",
        "severity_weights": {}
      }
    }
  ]
}
```

---

*Document version: 1.0*
*Companion to: Lance Life Events Master Documentation v2*
*Companion to: Player Condition System*
