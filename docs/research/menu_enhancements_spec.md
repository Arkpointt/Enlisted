# Menu System Enhancements for Lance Life

This document specifies enhancements to the existing Enlisted menu system to support action-based XP and camp events.

---

## Table of Contents

1. [Current State Analysis](#current-state-analysis)
2. [Required Changes](#required-changes)
3. [Enhanced Menu Structure](#enhanced-menu-structure)
4. [Camp Status Display](#camp-status-display)
5. [Activity Menu (New)](#activity-menu-new)
6. [Duty Menu Enhancements](#duty-menu-enhancements)
7. [Event Integration Points](#event-integration-points)
8. [Implementation Priority](#implementation-priority)

---

## Current State Analysis

### What the Current Menus Do Well

| Menu | Strength |
|------|----------|
| `enlisted_status` | Clean hub with good icons/tooltips |
| `enlisted_duty_selection` | Clear duty/profession selection with tier gating |
| Camp menu | Service records, discharge path |
| Quartermaster | Equipment access |

### What's Missing for Action-Based XP

| Gap | Impact |
|-----|--------|
| No visibility into **available activities** | Player doesn't know what they can do to earn XP |
| No **camp status** display | Player can't see conditions that trigger events |
| No **training event access** | Formation drills have no entry point |
| No **duty event queue** visibility | Player doesn't know when duty events are pending |
| No **activity log** | Player can't see recent XP gains or why |
| Duties show **what you are**, not **what you can do** | Selection-focused, not action-focused |

### The Core Problem

Current flow:
```
Select Duty → Wait → Passive XP (removed) → ???
```

Needed flow:
```
Select Duty → See Available Activities → Do Activities → Earn XP
```

---

## Required Changes

### Summary of Changes

| Menu | Change Type | Purpose |
|------|-------------|---------|
| `enlisted_status` | Enhance | Add camp status line, activity indicator |
| `enlisted_duty_selection` | Enhance | Show pending duty events, last activity |
| **NEW: `enlisted_activities`** | Add | Central hub for training and optional events |
| Camp menu | Enhance | Add activity log, XP breakdown |

### New Menu Option in Main Menu

Add to `enlisted_status`:

```
— Current Options —
• Master at Arms
• Visit Quartermaster
• My Lord...
• Visit Settlement
• Report for Duty
• Ask commander for leave
• Desert the Army

— Add —
• Camp Activities [NEW] ← Training events, optional activities
• (Camp status line in header) [NEW]
```

---

## Enhanced Menu Structure

### Revised Menu Flow

```
Enlisted Status Menu (enlisted_status)
│
│   [Header now shows camp status line]
│   "Camp Status: Supplies adequate • Morale steady • 3 days from town"
│
├── Master at Arms → Troop Selection
├── Visit Quartermaster → Equipment
├── Camp → Service Records / Pay / Discharge / Retinue
├── My Lord... → Dialog
├── Report for Duty → Duty Selection (enhanced)
│       └── Shows: Current duty, pending duty events, duty reputation
│
├── Camp Activities [NEW] → Activity Selection
│       ├── — TRAINING — (formation-based)
│       │   • Join the drill circle (Infantry)
│       │   • Target practice (Archer)
│       │   • Horse rotation (Cavalry)
│       │   └── [shows fatigue cost, XP reward]
│       │
│       ├── — CAMP TASKS — (general)
│       │   • Help at the surgeon's tent
│       │   • Assist the cook
│       │   • Work the forge
│       │   └── [available based on camp conditions]
│       │
│       └── — SOCIAL — 
│           • Join the fire circle
│           • Visit the camp followers
│           └── [available based on morale/location]
│
├── Ask commander for leave → Leave Request
└── Desert the Army → Immediate exit
```

---

## Camp Status Display

### Purpose

Show the player what's happening in camp so they understand:
- Why certain events are available
- What conditions affect their options
- Context for duty events

### Location

Add to the **header text** of `enlisted_status` menu, below the existing status info.

### Current Header (Example)

```
Enlisted under Lord Vlandia
Tier 3 Veteran • Infantry • 45 days served
Current Duty: Scout • Profession: Field Medic
```

### Enhanced Header

```
Enlisted under Lord Vlandia
Tier 3 Veteran • Infantry • 45 days served
Current Duty: Scout • Profession: Field Medic

— Camp Status —
Supplies: Adequate | Morale: Steady | Pay: On time
Days from town: 3 | Recent battle: 2 days ago
[!] Scout duty: Terrain recon available
```

### Camp Status Elements

| Element | Source | Display |
|---------|--------|---------|
| Supplies | `LogisticsStrain` | "Plentiful" / "Adequate" / "Tight" / "Critical" |
| Morale | `MoraleShock` | "High" / "Steady" / "Shaken" / "Low" |
| Pay | `PayTension` | "On time" / "Delayed" / "Uncertain" |
| Days from town | `daysSinceSettlement` | Number |
| Recent battle | `daysSinceBattle` | Number or "None recent" |
| Pending duty event | Duty event queue | "[!] {Duty}: {Event name} available" |

### Implementation

```csharp
private string BuildCampStatusLine()
{
    var sb = new StringBuilder();
    sb.AppendLine();
    sb.AppendLine("— Camp Status —");
    
    // Logistics
    string supplies = GetSupplyStatus(); // "Plentiful" / "Adequate" / etc.
    string morale = GetMoraleStatus();
    string pay = GetPayStatus();
    
    sb.AppendLine($"Supplies: {supplies} | Morale: {morale} | Pay: {pay}");
    
    // Time-based
    int daysFromTown = GetDaysSinceSettlement();
    int daysSinceBattle = GetDaysSinceBattle();
    sb.AppendLine($"Days from town: {daysFromTown} | Recent battle: {FormatBattleRecency(daysSinceBattle)}");
    
    // Pending duty events
    var pendingEvent = GetPendingDutyEvent();
    if (pendingEvent != null)
    {
        sb.AppendLine($"[!] {pendingEvent.DutyName}: {pendingEvent.EventTitle} available");
    }
    
    return sb.ToString();
}
```

---

## Activity Menu (New)

### Menu ID: `enlisted_activities`

### Purpose

Central hub for **player-initiated activities** that grant XP:
- Formation training (replaces passive XP)
- Optional camp tasks
- Social activities

### Menu Structure

```
— CAMP ACTIVITIES —

Your formation training and camp tasks are how you improve your skills.
Select an activity to participate.

— TRAINING ({Formation}) —
  ○ Shield Wall Drill      [+25 Polearm, +20 OneHanded | +2 Fatigue]
  ○ Sparring Circle        [+30 OneHanded | +2 Fatigue]
  ○ March Conditioning     [+25 Athletics | +2 Fatigue]
  
— CAMP TASKS —
  ○ Help the Surgeon       [+20 Medicine | +1 Fatigue] (available)
  ○ Work the Forge         [+20 Smithing | +2 Fatigue] (unavailable - no battle damage)
  ○ Forage for Camp        [+15 Scouting | +2 Fatigue] (available - supplies tight)

— SOCIAL —
  ○ Join the Fire Circle   [+15 Charm | +0 Fatigue, -1 Fatigue relief]
  ○ Drink with the Lads    [+10 Charm, +1 Heat | -2 Fatigue relief] (morale low)

[Back]
```

### Activity Availability Logic

| Activity Type | Availability Rule |
|---------------|-------------------|
| **Training** | Always available if `fatigue < threshold` and `no_combat_recent` (3+ days) |
| **Camp Tasks** | Based on camp conditions (supplies, battle damage, etc.) |
| **Social** | Based on morale, location (in settlement vs. field) |

### Activity Option Format

```csharp
starter.AddGameMenuOption("enlisted_activities", "activity_drill_shield",
    "Shield Wall Drill [+25 Polearm, +20 OneHanded | +2 Fatigue]",
    args =>
    {
        // Check availability
        if (GetFatigue() > MAX_FATIGUE_FOR_TRAINING) 
        {
            args.Tooltip = new TextObject("Too fatigued to train");
            args.IsEnabled = false;
            return true;
        }
        if (GetDaysSinceBattle() < 3)
        {
            args.Tooltip = new TextObject("No time for drill - battle too recent");
            args.IsEnabled = false;
            return true;
        }
        
        args.optionLeaveType = GameMenuOption.LeaveType.OrderTroopsToAttack;
        args.Tooltip = new TextObject("Join formation drill. Trains Polearm and One-Handed combat.");
        return GetPlayerFormation() == Formation.Infantry;
    },
    args => OnTrainingSelected("drill_shield"),
    isLeave: false,
    priority: 1);
```

### Training Selection Flow

```
Player clicks "Shield Wall Drill"
    ↓
Show intensity submenu OR direct inquiry:
    "How hard do you push?"
    • Drill hard (+3 Fatigue, +35 Polearm, +25 OneHanded)
    • Standard pace (+2 Fatigue, +25 Polearm, +20 OneHanded)
    • Take it easy (+1 Fatigue, +15 Polearm, +10 OneHanded)
    ↓
Apply fatigue and XP
    ↓
Return to activities menu (with cooldown now active)
```

### Cooldown Display

After completing an activity, show cooldown:

```
— TRAINING (Infantry) —
  ✓ Shield Wall Drill      [Completed - available in 2 days]
  ○ Sparring Circle        [+30 OneHanded | +2 Fatigue]
  ○ March Conditioning     [+25 Athletics | +2 Fatigue]
```

---

## Duty Menu Enhancements

### Current: `enlisted_duty_selection`

Shows duty selection with checkmarks. Needs to also show:
- Pending duty events
- Recent duty activity
- Duty reputation

### Enhanced Header

```
— DUTY ASSIGNMENT —

Current Duty: Scout (Good standing)
Last duty event: Terrain Recon (2 days ago) — +40 Scouting XP
Pending: Enemy Position Report [!]

Select your duty assignment. Your duty determines what tasks you're 
assigned and what skills you develop.

— DUTIES —
  ...
```

### Duty Event Indicator

If a duty event is pending/available, show it:

```csharp
private string GetDutyEventIndicator(string dutyId)
{
    var pendingEvent = GetPendingDutyEvent(dutyId);
    if (pendingEvent != null)
    {
        return $" [!] {pendingEvent.Title} pending";
    }
    
    var lastEvent = GetLastDutyEvent(dutyId);
    if (lastEvent != null)
    {
        int daysAgo = GetDaysSince(lastEvent.CompletedTime);
        return $" (Last: {lastEvent.Title}, {daysAgo}d ago)";
    }
    
    return "";
}
```

### Duty Option Format (Enhanced)

```
✓ Scout [!]
  "Terrain report pending - battle expected soon"
  Skills: Scouting, Athletics, Tactics
  Standing: Good
  
○ Quartermaster
  Skills: Steward, Trade, Leadership
  Standing: —
```

---

## Event Integration Points

### Where Events Hook Into Menus

| Event Type | Menu Integration |
|------------|------------------|
| **Duty Events** | Indicator in duty menu, can trigger from duty menu |
| **Training Events** | Listed in Activity menu, player initiates |
| **General Events** | Fire via Map Incident / Inquiry (not menu-based) |

### Duty Event Trigger from Menu

If a duty event is pending, player can trigger it from the duty menu:

```csharp
// In duty selection menu, if event is pending
starter.AddGameMenuOption("enlisted_duty_selection", "duty_event_trigger",
    "[!] Begin Terrain Reconnaissance",
    args =>
    {
        var pendingEvent = GetPendingDutyEvent("scout");
        if (pendingEvent == null) return false;
        
        args.optionLeaveType = GameMenuOption.LeaveType.Mission;
        args.Tooltip = new TextObject(pendingEvent.Setup);
        return true;
    },
    args => TriggerDutyEvent("scout"),
    isLeave: false,
    priority: -1); // Show at top
```

### Activity Event Flow

```
Player in Activity Menu
    ↓
Clicks "Help the Surgeon"
    ↓
Show options (inquiry or submenu):
    • Assist with surgery (+30 Medicine, +2 Fatigue)
    • Boil bandages (+15 Medicine, +1 Fatigue)
    • Just observe (+10 Medicine, +0 Fatigue)
    ↓
Apply rewards
    ↓
Return to menu with cooldown active
```

---

## Camp Menu Enhancements

### Add: Activity Log

In the Camp menu (`command_tent`), add a section showing recent XP gains:

```
— ACTIVITY LOG —

Today:
  • Shield Wall Drill: +25 Polearm, +20 OneHanded
  • Terrain Recon (Scout duty): +40 Scouting, +20 Tactics

Yesterday:
  • Sparring Circle: +30 OneHanded
  • Battle (Raiding Party): +45 OneHanded, +30 Athletics

This Week: +180 Polearm, +95 OneHanded, +60 Scouting, +50 Athletics, +20 Tactics
```

### Add: XP Breakdown by Source

```
— SKILL PROGRESS (This Term) —

Source Breakdown:
  Combat:     42%  ████████░░░░░░░░░░░░
  Duty:       35%  ███████░░░░░░░░░░░░░
  Training:   18%  ████░░░░░░░░░░░░░░░░
  Events:      5%  █░░░░░░░░░░░░░░░░░░░

Top Skills Gained:
  Polearm:    +340 XP
  Scouting:   +280 XP
  OneHanded:  +195 XP
  Athletics:  +150 XP
```

---

## UI Text Updates

### New Localization Strings Needed

```xml
<!-- Camp Status -->
<string id="CAMP_STATUS_HEADER" text="— Camp Status —"/>
<string id="SUPPLIES_PLENTIFUL" text="Plentiful"/>
<string id="SUPPLIES_ADEQUATE" text="Adequate"/>
<string id="SUPPLIES_TIGHT" text="Tight"/>
<string id="SUPPLIES_CRITICAL" text="Critical"/>
<string id="MORALE_HIGH" text="High"/>
<string id="MORALE_STEADY" text="Steady"/>
<string id="MORALE_SHAKEN" text="Shaken"/>
<string id="MORALE_LOW" text="Low"/>
<string id="PAY_ONTIME" text="On time"/>
<string id="PAY_DELAYED" text="Delayed"/>
<string id="PAY_UNCERTAIN" text="Uncertain"/>
<string id="DUTY_EVENT_PENDING" text="[!] {DUTY}: {EVENT} available"/>

<!-- Activity Menu -->
<string id="ACTIVITIES_HEADER" text="— CAMP ACTIVITIES —"/>
<string id="ACTIVITIES_INTRO" text="Your formation training and camp tasks are how you improve your skills. Select an activity to participate."/>
<string id="TRAINING_SECTION" text="— TRAINING ({FORMATION}) —"/>
<string id="CAMP_TASKS_SECTION" text="— CAMP TASKS —"/>
<string id="SOCIAL_SECTION" text="— SOCIAL —"/>
<string id="ACTIVITY_COOLDOWN" text="Completed - available in {DAYS} days"/>
<string id="ACTIVITY_TOO_FATIGUED" text="Too fatigued to participate"/>
<string id="ACTIVITY_UNAVAILABLE" text="Not currently available"/>

<!-- Activity Descriptions -->
<string id="DRILL_SHIELD_DESC" text="Join formation drill. Trains Polearm and One-Handed combat."/>
<string id="DRILL_SPARRING_DESC" text="Practice combat in the sparring circle."/>
<string id="TASK_SURGEON_DESC" text="Assist the camp surgeon with wounded soldiers."/>
<string id="TASK_FORGE_DESC" text="Help repair equipment at the forge."/>
<string id="SOCIAL_FIRE_DESC" text="Relax and socialize with your lance mates."/>

<!-- Duty Enhancements -->
<string id="DUTY_STANDING" text="Standing: {STANDING}"/>
<string id="DUTY_LAST_EVENT" text="Last: {EVENT}, {DAYS}d ago"/>
<string id="DUTY_EVENT_TRIGGER" text="[!] Begin {EVENT}"/>
```

---

## Implementation Priority

### Phase 1: Camp Status Display (Quick Win)

**Effort:** Low
**Impact:** High — Gives context for events

1. Add camp status line to `enlisted_status` header
2. Pull data from Camp Life Simulation
3. Show pending duty event indicator

### Phase 2: Activity Menu (Core Feature)

**Effort:** Medium
**Impact:** Critical — This is how players earn training XP

1. Create `enlisted_activities` menu
2. Add formation-based training options
3. Implement cooldowns and fatigue costs
4. Add to main menu

### Phase 3: Duty Menu Enhancements

**Effort:** Low-Medium
**Impact:** Medium — Improves duty event visibility

1. Add pending event indicator to duty options
2. Show duty reputation/standing
3. Allow triggering pending events from menu

### Phase 4: Activity Log (Polish)

**Effort:** Medium
**Impact:** Low-Medium — Nice to have, shows progress

1. Add XP tracking by source
2. Create activity log display in Camp menu
3. Show term totals

---

## Menu File Changes Summary

| File | Changes |
|------|---------|
| `EnlistedMenuBehavior.cs` | Add camp status to header, add Activities option |
| `EnlistedActivitiesBehavior.cs` | **NEW** — Activity menu and training events |
| `EnlistedDutySelectionMenu.cs` | Add event indicators, standing display |
| `CommandTentBehavior.cs` | Add activity log section |
| `enlisted_strings.xml` | New localization strings |

---

## Mockup: Full Enhanced Menu Flow

```
┌─────────────────────────────────────────────────────────────┐
│                    ENLISTED STATUS                          │
├─────────────────────────────────────────────────────────────┤
│ Enlisted under Lord Vlandia                                 │
│ Tier 3 Veteran • Infantry • 45 days served                  │
│ Current Duty: Scout • Profession: Field Medic               │
│                                                             │
│ — Camp Status —                                             │
│ Supplies: Adequate | Morale: Steady | Pay: On time          │
│ Days from town: 3 | Recent battle: 2 days ago               │
│ [!] Scout duty: Terrain recon available                     │
├─────────────────────────────────────────────────────────────┤
│ [⚔] Master at Arms                                          │
│ [💰] Visit Quartermaster                                    │
│ [🏕] Camp                                                    │
│ [💬] My Lord...                                             │
│ [📋] Report for Duty [!]                                    │
│ [🏃] Camp Activities                     ← NEW              │
│ [🚪] Ask commander for leave                                │
│ [⚠] Desert the Army                                        │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼ (Camp Activities)
┌─────────────────────────────────────────────────────────────┐
│                    CAMP ACTIVITIES                          │
├─────────────────────────────────────────────────────────────┤
│ Your training and tasks are how you improve your skills.    │
│                                                             │
│ — TRAINING (Infantry) —                                     │
│ ○ Shield Wall Drill    [+25 Polearm, +20 1H | +2 Fatigue]  │
│ ○ Sparring Circle      [+30 OneHanded | +2 Fatigue]        │
│ ✓ March Conditioning   [Cooldown: 1 day]                   │
│                                                             │
│ — CAMP TASKS —                                              │
│ ○ Help the Surgeon     [+20 Medicine | +1 Fatigue]         │
│ ○ Forage for Camp      [+15 Scouting | +2 Fatigue]         │
│ ✗ Work the Forge       [Unavailable - no repairs needed]   │
│                                                             │
│ — SOCIAL —                                                  │
│ ○ Join the Fire Circle [+15 Charm | Fatigue relief]        │
│                                                             │
│ [←] Back                                                    │
└─────────────────────────────────────────────────────────────┘
```

---

*Document version: 1.0*
*Companion to: Lance Life Events Master Documentation v2*
*Companion to: Menu Interface System*
