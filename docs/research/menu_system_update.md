# Menu System Update — Integrated Lance Life

> **Status: IMPLEMENTED**
> 
> This research doc has been implemented. The shipping documentation is:
> - [Menu Interface](../Features/UI/menu-interface.md) — feature spec with current menu structure
> - [Lance Assignments](../Features/Core/lance-assignments.md) — 9-tier progression with culture-specific ranks
> - [Implementation Roadmap](../Features/Core/implementation-roadmap.md) — overall system status

This document specifies all menu changes needed to support the Lance Life systems: lance career, camp activities, player condition, time-of-day awareness, and action-based XP.

---

## Table of Contents

1. [Overview of Changes](#overview-of-changes)
2. [Updated Menu Structure](#updated-menu-structure)
3. [Enlisted Status Menu (Enhanced)](#enlisted-status-menu-enhanced)
4. [My Lance Menu (New)](#my-lance-menu-new)
5. [Camp Activities Menu (New)](#camp-activities-menu-new)
6. [Duty Selection Menu (Enhanced)](#duty-selection-menu-enhanced)
7. [Medical Menu (New)](#medical-menu-new)
8. [Camp Menu (Enhanced)](#camp-menu-enhanced)
9. [Dynamic Header System](#dynamic-header-system)
10. [Placeholder Resolution](#placeholder-resolution)
11. [Implementation Priority](#implementation-priority)

---

## Overview of Changes

### Current Menu Structure

```
Enlisted Status Menu
├── Master at Arms
├── Visit Quartermaster
├── My Lord...
├── Visit Settlement
├── Report for Duty
├── Ask for Leave
└── Desert the Army
```

### New Menu Structure

```
Enlisted Status Menu (enhanced header with status)
├── Master at Arms
├── Visit Quartermaster
├── My Lance [NEW]                    ← Lance roster, relationships, rank
├── Camp Activities [NEW]             ← Training, tasks, social (action-based XP)
├── Report for Duty (enhanced)        ← Shows pending duty events
├── Seek Medical Attention [NEW]      ← Only when injured/ill
├── Camp (enhanced)                   ← Activity log, XP breakdown added
├── My Lord...
├── Visit Settlement
├── Ask for Leave
└── Desert the Army
```

### What Each New/Enhanced Menu Does

| Menu | Purpose | Key Features |
|------|---------|--------------|
| **My Lance** | See your unit, relationships, rank | Roster, NPC relationships, position in lance |
| **Camp Activities** | Earn XP through actions | Training by formation, camp tasks, social |
| **Report for Duty** | Duty selection + pending events | Enhanced with duty event indicators |
| **Seek Medical** | Treat injuries/illness | Only appears when needed |
| **Camp** | Service records + activity log | Now shows XP breakdown by source |

---

## Updated Menu Structure

### Full Navigation Flow

```
Enlisted Status Menu (enlisted_status)
│
│   [Enhanced Header - see Dynamic Header System]
│
├── [⚔] Master at Arms → Troop Selection Popup
│
├── [💰] Visit Quartermaster → Equipment Menu
│
├── [🛡] My Lance [NEW] → Lance Menu (enlisted_lance)
│       ├── Lance Roster (see all 10 members)
│       ├── Talk to {LANCE_LEADER_RANK} (if available)
│       ├── Talk to {SECOND_RANK} (if available)
│       ├── Check on Wounded (if any wounded)
│       └── [Back]
│
├── [🏃] Camp Activities [NEW] → Activities Menu (enlisted_activities)
│       ├── — TRAINING ({FORMATION}) —
│       │   ├── Formation Drill
│       │   ├── Sparring Circle
│       │   └── [formation-specific options]
│       ├── — CAMP TASKS —
│       │   ├── Help the Surgeon (if available)
│       │   ├── Work the Forge (if available)
│       │   └── Forage for Camp (if available)
│       ├── — SOCIAL —
│       │   ├── Fire Circle (evening/dusk)
│       │   └── [time-appropriate options]
│       └── [Back]
│
├── [📋] Report for Duty → Duty Menu (enlisted_duty_selection) [ENHANCED]
│       ├── [!] Pending Duty Event (if any)
│       ├── — DUTIES —
│       ├── — PROFESSIONS —
│       └── [Back]
│
├── [🏥] Seek Medical Attention [NEW] → Medical Menu (enlisted_medical)
│       [Only shows if injured or ill]
│       ├── Request Treatment
│       ├── Check Condition
│       ├── Rest in Camp
│       └── [Back]
│
├── [🏕] Camp → Camp Menu (enlisted_camp) [ENHANCED]
│       ├── Service Records
│       ├── Activity Log [NEW] ← XP gains, recent events
│       ├── Pay & Pension Status
│       ├── Request Discharge
│       ├── Retinue
│       └── [Back]
│
├── [💬] My Lord... → Dialog System
│
├── [🏘] Visit Settlement → Settlement Menu
│
├── [🚪] Ask for Leave → Leave Dialog
│
└── [⚠] Desert the Army → Desertion Confirmation
```

---

## Enlisted Status Menu (Enhanced)

### Menu ID: `enlisted_status`

### Enhanced Header

The header now shows comprehensive status at a glance:

```
═══════════════════════════════════════════════════════════════
                    ENLISTED STATUS
═══════════════════════════════════════════════════════════════

Serving: {LORD_TITLE} {LORD_NAME} of {FACTION_NAME}
Lance: {LANCE_NAME} — {PLAYER_RANK_TITLE} ({TIER_NAME})
Days Served: {DAYS_ENLISTED} | Formation: {FORMATION}

— YOUR CONDITION —
Health: {HEALTH_BAR} {HEALTH_PERCENT}%
{CONDITION_LINE}
Fatigue: {FATIGUE_CURRENT}/{FATIGUE_MAX}

— CAMP STATUS —
Supplies: {SUPPLY_STATUS} | Morale: {MORALE_STATUS} | Pay: {PAY_STATUS}
Time: {TIME_OF_DAY} | Days from town: {DAYS_FROM_TOWN}
{PENDING_EVENT_LINE}

═══════════════════════════════════════════════════════════════
```

### Condition Line Examples

```
// Healthy
Condition: Healthy

// Injured
Condition: Moderate Injury (Twisted knee) — {RECOVERY_DAYS} days recovery
Fatigue Pool: 75% of normal

// Ill
Condition: Camp Fever (Moderate) — See the surgeon
Health draining: −2% per day

// Multiple
Condition: Minor Injury + Exhaustion (Worn)
```

### Pending Event Line Examples

```
// Duty event pending
[!] Scout duty: Terrain Reconnaissance available

// Training available
[!] Training available — {FORMATION} drills forming

// Medical urgent
[!] Seek medical attention — condition worsening

// Nothing
[line hidden when no pending events]
```

### New Menu Options

```csharp
// My Lance option
starter.AddGameMenuOption("enlisted_status", "option_my_lance",
    "{=LANCE_OPT}My Lance",
    args =>
    {
        args.optionLeaveType = GameMenuOption.LeaveType.Manage;
        args.Tooltip = new TextObject("View your lance, check on your comrades, see your standing");
        return IsEnlisted() && HasLance();
    },
    args => GameMenu.SwitchToMenu("enlisted_lance"),
    false, 2);  // Priority 2, after Quartermaster

// Camp Activities option
starter.AddGameMenuOption("enlisted_status", "option_activities",
    "{=ACTIVITIES_OPT}Camp Activities",
    args =>
    {
        args.optionLeaveType = GameMenuOption.LeaveType.Wait;
        args.Tooltip = new TextObject("Training, camp tasks, and social activities — earn XP through action");
        
        // Show indicator if activities available
        int availableCount = GetAvailableActivityCount();
        if (availableCount > 0)
            args.Tooltip = new TextObject($"{availableCount} activities available");
        
        return IsEnlisted();
    },
    args => GameMenu.SwitchToMenu("enlisted_activities"),
    false, 4);  // After Report for Duty

// Seek Medical option (conditional)
starter.AddGameMenuOption("enlisted_status", "option_medical",
    "{=MEDICAL_OPT}Seek Medical Attention",
    args =>
    {
        args.optionLeaveType = GameMenuOption.LeaveType.Manage;
        
        if (HasUrgentCondition())
            args.Tooltip = new TextObject("[!] Your condition requires attention");
        else
            args.Tooltip = new TextObject("Visit the surgeon's tent");
        
        // Only show if injured, ill, or exhausted
        return IsEnlisted() && (HasInjury() || HasIllness() || HasExhaustion());
    },
    args => GameMenu.SwitchToMenu("enlisted_medical"),
    false, 5);
```

---

## My Lance Menu (New)

### Menu ID: `enlisted_lance`

### Purpose

Show the player their lance — who's in it, their relationships, and their position.

### Header

```
═══════════════════════════════════════════════════════════════
                      {LANCE_NAME}
═══════════════════════════════════════════════════════════════

Your Position: {PLAYER_RANK_TITLE} (Slot {PLAYER_SLOT}/10)
Lance Leader: {LANCE_LEADER_NAME}
Battles Survived: {LANCE_BATTLES} | Fallen: {LANCE_FALLEN_COUNT}

— ROSTER —
```

### Roster Display

```
[1] {LANCE_LEADER_RANK} {LANCE_LEADER_NAME}
    "{LANCE_LEADER_TRAIT}" | Relationship: {REL_INDICATOR}
    
[2] {SECOND_RANK} {SECOND_NAME}
    "{SECOND_TRAIT}" | Relationship: {REL_INDICATOR}

[3] {VETERAN_1_NAME} (Veteran)
    "{VETERAN_1_TRAIT}" | Relationship: {REL_INDICATOR}

[4] {VETERAN_2_NAME} (Veteran)
    Relationship: {REL_INDICATOR}

[5] {SOLDIER_1_NAME}
[6] {SOLDIER_2_NAME}
[7] {SOLDIER_3_NAME}
[8] {SOLDIER_4_NAME}

[9] {RECRUIT_1_NAME} — [WOUNDED: 3 days]
[10] YOU — {PLAYER_RANK_TITLE}
```

### Relationship Indicators

```
[+++] Bonded (75+)
[++ ] Loyal (50-74)
[+  ] Friendly (20-49)
[   ] Neutral (-19 to +19)
[-  ] Unfriendly (-49 to -20)
[-- ] Hostile (-100 to -50)
```

### Menu Options

```csharp
// View full roster
starter.AddGameMenuOption("enlisted_lance", "lance_roster",
    "View Full Roster",
    args => {
        args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
        return true;
    },
    args => ShowLanceRosterPopup(),
    false, 0);

// Talk to leader
starter.AddGameMenuOption("enlisted_lance", "lance_talk_leader",
    "Speak with {LANCE_LEADER_RANK} {LANCE_LEADER_SHORT}",
    args => {
        args.optionLeaveType = GameMenuOption.LeaveType.Conversation;
        args.Tooltip = new TextObject("Have a word with your lance leader");
        return GetLeaderRelationship() > -50; // Not if hostile
    },
    args => StartLanceLeaderConversation(),
    false, 1);

// Talk to second
starter.AddGameMenuOption("enlisted_lance", "lance_talk_second",
    "Speak with {SECOND_RANK} {SECOND_SHORT}",
    args => {
        args.optionLeaveType = GameMenuOption.LeaveType.Conversation;
        return GetSecondRelationship() > -50;
    },
    args => StartSecondConversation(),
    false, 2);

// Check on wounded
starter.AddGameMenuOption("enlisted_lance", "lance_wounded",
    "Check on the wounded",
    args => {
        args.optionLeaveType = GameMenuOption.LeaveType.Manage;
        int woundedCount = GetWoundedLanceMateCount();
        args.Tooltip = new TextObject($"{woundedCount} lance mates recovering from wounds");
        return woundedCount > 0;
    },
    args => ShowWoundedStatus(),
    false, 3);

// Honor the fallen
starter.AddGameMenuOption("enlisted_lance", "lance_fallen",
    "Remember the fallen",
    args => {
        args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
        int fallenCount = GetFallenCount();
        args.Tooltip = new TextObject($"{fallenCount} have fallen serving with {LANCE_NAME}");
        return fallenCount > 0;
    },
    args => ShowFallenRoster(),
    false, 4);

// Back
starter.AddGameMenuOption("enlisted_lance", "lance_back",
    "Back",
    args => {
        args.optionLeaveType = GameMenuOption.LeaveType.Leave;
        return true;
    },
    args => GameMenu.SwitchToMenu("enlisted_status"),
    true, 99);
```

---

## Camp Activities Menu (New)

### Menu ID: `enlisted_activities`

### Purpose

Central hub for **action-based XP**. This is where passive formation training moved to — players must engage to earn.

### Header

```
═══════════════════════════════════════════════════════════════
                    CAMP ACTIVITIES
═══════════════════════════════════════════════════════════════

Time: {TIME_OF_DAY_FULL} | Your Fatigue: {FATIGUE_CURRENT}/{FATIGUE_MAX}
{CONDITION_WARNING}

Your skills improve through action. Select an activity to participate.

═══════════════════════════════════════════════════════════════
```

### Activity Sections

#### Training Section (Formation-Based)

```
— TRAINING ({FORMATION}) —

Available training depends on your formation and time of day.
```

**Infantry Training Options:**
```csharp
starter.AddGameMenuOption("enlisted_activities", "train_shield_wall",
    "Shield Wall Drill [+25 Polearm, +20 1H | +2 Fatigue]",
    args => {
        args.optionLeaveType = GameMenuOption.LeaveType.OrderTroopsToAttack;
        
        if (IsOnCooldown("shield_wall_drill"))
        {
            args.Tooltip = new TextObject($"Cooldown: {GetCooldownDays("shield_wall_drill")} days");
            args.IsEnabled = false;
        }
        else if (GetFatigue() > GetMaxFatigue() - 2)
        {
            args.Tooltip = new TextObject("Too fatigued");
            args.IsEnabled = false;
        }
        else if (HasRestrictingCondition())
        {
            args.Tooltip = new TextObject("Your injury prevents training");
            args.IsEnabled = false;
        }
        else
        {
            args.Tooltip = new TextObject("Join formation drill with your lance");
        }
        
        return GetPlayerFormation() == Formation.Infantry && 
               IsTrainingTime(); // Morning/Afternoon
    },
    args => StartTrainingEvent("shield_wall_drill"),
    false, 10);

starter.AddGameMenuOption("enlisted_activities", "train_sparring",
    "Sparring Circle [+30 OneHanded | +2 Fatigue]",
    // Similar pattern...
    );
```

**Cavalry Training Options:**
```csharp
starter.AddGameMenuOption("enlisted_activities", "train_horse_rotation",
    "Horse Rotation [+25 Riding | +1 Fatigue]",
    args => {
        // Only for cavalry formation
        return GetPlayerFormation() == Formation.Cavalry;
    },
    // ...
    );

starter.AddGameMenuOption("enlisted_activities", "train_mounted_drill",
    "Mounted Drill [+30 Riding, +20 Polearm | +3 Fatigue]",
    // ...
    );
```

**Archer Training Options:**
```csharp
starter.AddGameMenuOption("enlisted_activities", "train_target_practice",
    "Target Practice [+25 Bow | +1 Fatigue]",
    args => {
        return GetPlayerFormation() == Formation.Archer;
    },
    // ...
    );

starter.AddGameMenuOption("enlisted_activities", "train_hunting",
    "Hunting Party [+20 Bow, +15 Scouting | +2 Fatigue]",
    args => {
        // Only if near wilderness and supplies needed
        return GetPlayerFormation() == Formation.Archer && 
               IsNearWilderness() && 
               GetLogisticsStrain() > 30;
    },
    // ...
    );
```

**Naval Training Options:**
```csharp
starter.AddGameMenuOption("enlisted_activities", "train_boarding",
    "Boarding Drill [+30 Mariner, +20 Athletics | +2 Fatigue]",
    args => {
        return IsAtSea() && GetPlayerFormation() == Formation.Naval;
    },
    // ...
    );
```

#### Camp Tasks Section

```
— CAMP TASKS —

Tasks available based on camp conditions and needs.
```

```csharp
// Help surgeon (always available if not injured)
starter.AddGameMenuOption("enlisted_activities", "task_surgeon",
    "Help the Surgeon [+20 Medicine | +1 Fatigue]",
    args => {
        args.optionLeaveType = GameMenuOption.LeaveType.Manage;
        
        bool hasWounded = GetArmyWoundedCount() > 0;
        if (!hasWounded)
        {
            args.Tooltip = new TextObject("No wounded to tend");
            args.IsEnabled = false;
        }
        
        return !HasMedic() || hasWounded; // More valuable if no medic duty
    },
    args => StartTaskEvent("help_surgeon"),
    false, 30);

// Work the forge (after battles)
starter.AddGameMenuOption("enlisted_activities", "task_forge",
    "Work the Forge [+20 Smithing, +15 Engineering | +2 Fatigue]",
    args => {
        bool hasRepairs = GetDaysSinceBattle() < 3;
        if (!hasRepairs)
        {
            args.Tooltip = new TextObject("No repairs needed");
            args.IsEnabled = false;
        }
        return hasRepairs;
    },
    args => StartTaskEvent("forge_work"),
    false, 31);

// Forage (when supplies low)
starter.AddGameMenuOption("enlisted_activities", "task_forage",
    "Forage for Camp [+15 Scouting, +10 Steward | +2 Fatigue]",
    args => {
        bool suppliesLow = GetLogisticsStrain() > 50;
        if (!suppliesLow)
        {
            args.Tooltip = new TextObject("Supplies adequate");
            args.IsEnabled = false;
        }
        return suppliesLow;
    },
    args => StartTaskEvent("forage"),
    false, 32);
```

#### Social Section

```
— SOCIAL —

Social activities available based on time of day.
```

```csharp
// Fire circle (evening/dusk only)
starter.AddGameMenuOption("enlisted_activities", "social_fire",
    "Join the Fire Circle [+15 Charm | −1 Fatigue]",
    args => {
        args.optionLeaveType = GameMenuOption.LeaveType.Wait;
        
        var time = GetTimeOfDay();
        if (time != TimeOfDay.Evening && time != TimeOfDay.Dusk)
        {
            args.Tooltip = new TextObject("Only in the evening");
            args.IsEnabled = false;
        }
        else
        {
            args.Tooltip = new TextObject("Stories and camaraderie around the fire");
        }
        
        return true; // Always show, but may be disabled
    },
    args => StartSocialEvent("fire_circle"),
    false, 40);

// Drink with lance (evening, costs heat or gold)
starter.AddGameMenuOption("enlisted_activities", "social_drink",
    "Drink with the Lads [+10 Charm, +1 Heat | −2 Fatigue]",
    args => {
        var time = GetTimeOfDay();
        bool isSafe = !IsInHostileTerritory();
        
        if (time != TimeOfDay.Evening && time != TimeOfDay.Dusk && time != TimeOfDay.Night)
        {
            args.IsEnabled = false;
        }
        
        return isSafe;
    },
    args => StartSocialEvent("drinking"),
    false, 41);
```

#### Rest Option

```csharp
// Rest (skip activities, recover fatigue)
starter.AddGameMenuOption("enlisted_activities", "rest",
    "Rest Instead [−2 Fatigue, no XP]",
    args => {
        args.optionLeaveType = GameMenuOption.LeaveType.Wait;
        args.Tooltip = new TextObject("Skip activities for today, recover fatigue faster");
        return GetFatigue() > 0;
    },
    args => {
        ApplyFatigueRelief(2);
        ShowMessage("You rest instead of training.");
        GameMenu.SwitchToMenu("enlisted_status");
    },
    false, 50);

// Back
starter.AddGameMenuOption("enlisted_activities", "back",
    "Back",
    args => {
        args.optionLeaveType = GameMenuOption.LeaveType.Leave;
        return true;
    },
    args => GameMenu.SwitchToMenu("enlisted_status"),
    true, 99);
```

---

## Duty Selection Menu (Enhanced)

### Menu ID: `enlisted_duty_selection`

### Enhanced Header

```
═══════════════════════════════════════════════════════════════
                    DUTY ASSIGNMENT
═══════════════════════════════════════════════════════════════

Current Duty: {CURRENT_DUTY} ({DUTY_STANDING})
Last Activity: {LAST_DUTY_EVENT} ({DAYS_AGO} days ago)

{PENDING_DUTY_EVENT_SECTION}

Your duty determines what tasks you're assigned and what events 
you'll see. Duty events are your primary source of skill growth.

═══════════════════════════════════════════════════════════════
```

### Pending Duty Event Section

When a duty event is available:

```
┌─────────────────────────────────────────────────────────────┐
│ [!] PENDING: Terrain Reconnaissance                         │
│                                                             │
│ Battle is expected soon. The captain needs terrain intel.   │
│                                                             │
│ → Begin Reconnaissance                                      │
└─────────────────────────────────────────────────────────────┘
```

```csharp
// Pending duty event trigger
starter.AddGameMenuOption("enlisted_duty_selection", "duty_event_pending",
    "[!] Begin {PENDING_EVENT_TITLE}",
    args => {
        var pending = GetPendingDutyEvent();
        if (pending == null) return false;
        
        args.optionLeaveType = GameMenuOption.LeaveType.Mission;
        args.Tooltip = new TextObject(pending.Setup);
        return true;
    },
    args => TriggerPendingDutyEvent(),
    false, -1);  // Top priority
```

### Enhanced Duty Options

Show reputation and recent activity:

```csharp
starter.AddGameMenuOption("enlisted_duty_selection", "duty_scout",
    "{SCOUT_CHECK} Scout — Scouting, Athletics, Tactics",
    args => {
        args.optionLeaveType = GameMenuOption.LeaveType.Mission;
        
        string standing = GetDutyStanding("scout");
        int lastEvent = GetDaysSinceLastDutyEvent("scout");
        
        args.Tooltip = new TextObject(
            $"Reconnaissance and terrain assessment.\n" +
            $"Standing: {standing}\n" +
            $"Last event: {lastEvent} days ago"
        );
        
        return true;
    },
    args => SelectDuty("scout"),
    false, 10);
```

---

## Medical Menu (New)

### Menu ID: `enlisted_medical`

### Purpose

Treatment and recovery options when injured, ill, or exhausted.

### Header

```
═══════════════════════════════════════════════════════════════
                   MEDICAL ATTENTION
═══════════════════════════════════════════════════════════════

— YOUR CONDITION —
{CONDITION_DETAILS}

Recovery Time: {RECOVERY_ESTIMATE}
Treatment Available: {TREATMENT_STATUS}

═══════════════════════════════════════════════════════════════
```

### Condition Details Examples

```
// Injury
Injury: Twisted Knee (Moderate)
  Location: Left leg
  Health: 78%
  Fatigue Pool: −25%
  Recovery: 5-7 days without treatment, 3-4 with
  Restrictions: No training, light duty only

// Illness
Illness: Camp Fever (Moderate)
  Health: Draining 2% per day
  Fatigue Pool: −25%
  Recovery: Requires treatment
  Warning: May worsen if untreated

// Exhaustion
Exhaustion: Worn
  Fatigue Pool: −25%
  XP Penalty: −10%
  Recovery: 2-3 days rest
```

### Menu Options

```csharp
// Request treatment (from surgeon)
starter.AddGameMenuOption("enlisted_medical", "med_treatment",
    "Request Treatment from Surgeon",
    args => {
        args.optionLeaveType = GameMenuOption.LeaveType.Manage;
        args.Tooltip = new TextObject(
            "Surgeon will treat your condition.\n" +
            "+100% recovery rate, stops worsening.\n" +
            "Cost: +2 Fatigue"
        );
        return HasTreatableCondition();
    },
    args => StartMedicalEvent("surgeon_treatment"),
    false, 0);

// Self-treat (if Field Medic duty)
starter.AddGameMenuOption("enlisted_medical", "med_self_treat",
    "Treat Yourself (Field Medic)",
    args => {
        args.optionLeaveType = GameMenuOption.LeaveType.Manage;
        args.Tooltip = new TextObject(
            "Use your medical knowledge.\n" +
            "+Medicine XP, +50% recovery rate.\n" +
            "Only for minor/moderate conditions."
        );
        return HasDuty("field_medic") && CanSelfTreat();
    },
    args => StartMedicalEvent("self_treatment"),
    false, 1);

// Buy medicine
starter.AddGameMenuOption("enlisted_medical", "med_buy",
    "Purchase Herbal Remedy ({MEDICINE_COST} gold)",
    args => {
        args.optionLeaveType = GameMenuOption.LeaveType.Trade;
        int cost = GetMedicineCost();
        args.Tooltip = new TextObject(
            $"Buy medicine from camp followers.\n" +
            $"+75% recovery rate.\n" +
            $"Cost: {cost} gold"
        );
        return Hero.MainHero.Gold >= cost && IsInSettlementOrHasCampFollowers();
    },
    args => BuyMedicine(),
    false, 2);

// Rest in camp
starter.AddGameMenuOption("enlisted_medical", "med_rest",
    "Rest in Camp (Light Duty)",
    args => {
        args.optionLeaveType = GameMenuOption.LeaveType.Wait;
        args.Tooltip = new TextObject(
            "Skip activities, focus on recovery.\n" +
            "+50% recovery rate.\n" +
            "No duty events while resting."
        );
        return true;
    },
    args => SetRestMode(true),
    false, 3);

// Request bed rest (serious conditions)
starter.AddGameMenuOption("enlisted_medical", "med_bed_rest",
    "Request Bed Rest (Duty Suspended)",
    args => {
        args.optionLeaveType = GameMenuOption.LeaveType.Wait;
        args.Tooltip = new TextObject(
            "Full rest, duty suspended until recovered.\n" +
            "+150% recovery rate.\n" +
            "No activities or duty events."
        );
        return HasSeriousCondition();
    },
    args => RequestBedRest(),
    false, 4);

// Check condition details
starter.AddGameMenuOption("enlisted_medical", "med_status",
    "View Detailed Status",
    args => {
        args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
        return true;
    },
    args => ShowDetailedConditionPopup(),
    false, 5);

// Back
starter.AddGameMenuOption("enlisted_medical", "med_back",
    "Back",
    args => {
        args.optionLeaveType = GameMenuOption.LeaveType.Leave;
        return true;
    },
    args => GameMenu.SwitchToMenu("enlisted_status"),
    true, 99);
```

---

## Camp Menu (Enhanced)

### Menu ID: `enlisted_camp`

### New Option: Activity Log

```csharp
starter.AddGameMenuOption("enlisted_camp", "camp_activity_log",
    "Activity Log",
    args => {
        args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
        args.Tooltip = new TextObject("View your recent activities and XP gains");
        return true;
    },
    args => ShowActivityLogPopup(),
    false, 1);  // After Service Records
```

### Activity Log Popup Content

```
═══════════════════════════════════════════════════════════════
                     ACTIVITY LOG
═══════════════════════════════════════════════════════════════

— TODAY —
• Shield Wall Drill: +25 Polearm, +20 OneHanded
• Terrain Recon (Scout duty): +40 Scouting, +20 Tactics
• Fire Circle: +15 Charm

— YESTERDAY —
• Sparring Circle: +30 OneHanded
• Supply Inventory (Quartermaster duty): +30 Steward, +15 Trade

— THIS WEEK TOTAL —
Combat:     +85 XP  ████████░░░░░░░░░░░░
Duty:      +145 XP  ██████████████░░░░░░
Training:   +75 XP  ███████░░░░░░░░░░░░░
Events:     +30 XP  ███░░░░░░░░░░░░░░░░░

— TOP SKILLS THIS TERM —
Scouting:   +280 XP
Polearm:    +195 XP
OneHanded:  +150 XP
Steward:    +125 XP

═══════════════════════════════════════════════════════════════
```

### New Option: XP Breakdown

```csharp
starter.AddGameMenuOption("enlisted_camp", "camp_xp_breakdown",
    "Skill Progress",
    args => {
        args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
        args.Tooltip = new TextObject("See how your skills have grown this term");
        return true;
    },
    args => ShowXPBreakdownPopup(),
    false, 2);
```

---

## Dynamic Header System

### Building the Header

```csharp
private string BuildEnlistedStatusHeader()
{
    var sb = new StringBuilder();
    
    // Basic info
    sb.AppendLine($"Serving: {GetLordTitle()} {GetLordName()} of {GetFactionName()}");
    sb.AppendLine($"Lance: {GetLanceName()} — {GetPlayerRankTitle()} ({GetTierName()})");
    sb.AppendLine($"Days Served: {GetDaysEnlisted()} | Formation: {GetFormationName()}");
    sb.AppendLine();
    
    // Condition section
    sb.AppendLine("— YOUR CONDITION —");
    sb.AppendLine($"Health: {BuildHealthBar()} {GetHealthPercent()}%");
    
    if (HasCondition())
    {
        sb.AppendLine(BuildConditionLine());
    }
    
    sb.AppendLine($"Fatigue: {GetFatigueCurrent()}/{GetFatigueMax()}");
    sb.AppendLine();
    
    // Camp status section
    sb.AppendLine("— CAMP STATUS —");
    sb.AppendLine($"Supplies: {GetSupplyStatus()} | Morale: {GetMoraleStatus()} | Pay: {GetPayStatus()}");
    sb.AppendLine($"Time: {GetTimeOfDayFull()} | Days from town: {GetDaysFromTown()}");
    
    // Pending events
    string pendingLine = BuildPendingEventLine();
    if (!string.IsNullOrEmpty(pendingLine))
    {
        sb.AppendLine(pendingLine);
    }
    
    return sb.ToString();
}

private string BuildHealthBar()
{
    int health = GetHealthPercent();
    int filled = health / 10;
    int empty = 10 - filled;
    return "[" + new string('█', filled) + new string('░', empty) + "]";
}

private string BuildConditionLine()
{
    var conditions = new List<string>();
    
    if (HasInjury())
    {
        var injury = GetCurrentInjury();
        conditions.Add($"{injury.Severity} Injury ({injury.Type})");
    }
    
    if (HasIllness())
    {
        var illness = GetCurrentIllness();
        conditions.Add($"{illness.Type} ({illness.Severity})");
    }
    
    if (HasExhaustion())
    {
        conditions.Add($"Exhaustion ({GetExhaustionLevel()})");
    }
    
    if (conditions.Count == 0)
        return "Condition: Healthy";
    
    string conditionText = string.Join(" + ", conditions);
    string recovery = GetRecoveryEstimate();
    
    return $"Condition: {conditionText} — {recovery}";
}

private string BuildPendingEventLine()
{
    // Check for pending duty event
    var dutyEvent = GetPendingDutyEvent();
    if (dutyEvent != null)
    {
        return $"[!] {GetCurrentDuty()} duty: {dutyEvent.Title} available";
    }
    
    // Check for available training
    if (IsTrainingTime() && HasAvailableTraining())
    {
        return $"[!] Training available — {GetFormationName()} drills forming";
    }
    
    // Check for medical urgency
    if (HasWorseningCondition())
    {
        return "[!] Seek medical attention — condition worsening";
    }
    
    return null;
}
```

---

## Placeholder Resolution

### Menu-Specific Placeholders

All menus should resolve these placeholders in their text:

```csharp
private void SetMenuPlaceholders(MenuCallbackArgs args)
{
    var text = args.MenuContext.GameMenu.GetText();
    
    // Lord/Faction
    text.SetTextVariable("LORD_NAME", GetLordName());
    text.SetTextVariable("LORD_TITLE", GetLordTitle());
    text.SetTextVariable("FACTION_NAME", GetFactionName());
    
    // Lance
    text.SetTextVariable("LANCE_NAME", GetLanceName());
    text.SetTextVariable("PLAYER_RANK_TITLE", GetPlayerRankTitle());
    text.SetTextVariable("LANCE_LEADER_RANK", GetLanceLeaderRank());
    text.SetTextVariable("LANCE_LEADER_NAME", GetLanceLeaderName());
    text.SetTextVariable("LANCE_LEADER_SHORT", GetLanceLeaderShortName());
    text.SetTextVariable("SECOND_RANK", GetSecondRank());
    text.SetTextVariable("SECOND_NAME", GetSecondName());
    
    // Player state
    text.SetTextVariable("TIER_NAME", GetTierName());
    text.SetTextVariable("FORMATION", GetFormationName());
    text.SetTextVariable("DAYS_ENLISTED", GetDaysEnlisted().ToString());
    text.SetTextVariable("CURRENT_DUTY", GetCurrentDuty());
    
    // Condition
    text.SetTextVariable("HEALTH_PERCENT", GetHealthPercent().ToString());
    text.SetTextVariable("FATIGUE_CURRENT", GetFatigueCurrent().ToString());
    text.SetTextVariable("FATIGUE_MAX", GetFatigueMax().ToString());
    
    // Camp status
    text.SetTextVariable("SUPPLY_STATUS", GetSupplyStatus());
    text.SetTextVariable("MORALE_STATUS", GetMoraleStatus());
    text.SetTextVariable("PAY_STATUS", GetPayStatus());
    text.SetTextVariable("TIME_OF_DAY", GetTimeOfDayName());
    text.SetTextVariable("TIME_OF_DAY_FULL", GetTimeOfDayFull());
    text.SetTextVariable("DAYS_FROM_TOWN", GetDaysFromTown().ToString());
}
```

---

## Implementation Priority

### Phase 1: Enhanced Header (Quick Win)

**Effort:** Low
**Impact:** High

1. Add camp status line to `enlisted_status` header
2. Add condition display
3. Add pending event indicator

### Phase 2: Camp Activities Menu (Critical)

**Effort:** Medium
**Impact:** Critical — This is where XP comes from

1. Create `enlisted_activities` menu
2. Add formation-based training options
3. Add camp task options
4. Add social options
5. Implement cooldowns and fatigue checks

### Phase 3: My Lance Menu (High Value)

**Effort:** Medium
**Impact:** High — Makes lance feel real

1. Create `enlisted_lance` menu
2. Display roster with relationships
3. Add conversation starters with key NPCs
4. Add wounded/fallen tracking

### Phase 4: Medical Menu (Important)

**Effort:** Low-Medium
**Impact:** Medium — Completes condition loop

1. Create `enlisted_medical` menu
2. Add treatment options
3. Add rest/recovery options
4. Link to condition system

### Phase 5: Enhanced Duty Menu (Polish)

**Effort:** Low
**Impact:** Medium

1. Add pending event trigger
2. Add duty standing display
3. Add last activity display

### Phase 6: Activity Log (Polish)

**Effort:** Medium
**Impact:** Low-Medium — Nice visibility

1. Add activity log to Camp menu
2. Track XP by source
3. Show weekly/term totals

---

## File Changes Summary

| File | Changes |
|------|---------|
| `EnlistedMenuBehavior.cs` | Enhanced header, new menu options, placeholders |
| `EnlistedLanceMenuBehavior.cs` | **NEW** — Lance menu |
| `EnlistedActivitiesMenuBehavior.cs` | **NEW** — Activities menu |
| `EnlistedMedicalMenuBehavior.cs` | **NEW** — Medical menu |
| `EnlistedDutySelectionBehavior.cs` | Enhanced with pending events |
| `CommandTentBehavior.cs` | Activity log addition |
| `enlisted_strings.xml` | New localization strings |

---

*Document version: 1.0*
*Integrates: Lance Career, Player Condition, Time/AI State, Action-Based XP*
*Companion to: Menu Interface System*
