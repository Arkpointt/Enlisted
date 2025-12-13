# Event Delivery Guide

This document specifies HOW each event type is delivered to the player — automatic vs. player-initiated, inquiry popup vs. menu flow, and what triggers each.

---

## Table of Contents

1. [Overview](#overview)
2. [Delivery Methods](#delivery-methods)
3. [Event Type → Delivery Mapping](#event-type--delivery-mapping)
4. [Automatic Events](#automatic-events)
5. [Player-Initiated Events](#player-initiated-events)
6. [Menu Integration](#menu-integration)
7. [Implementation Patterns](#implementation-patterns)

---

## Overview

### Two Delivery Methods

| Method | Description | Player Action |
|--------|-------------|---------------|
| **Automatic** | System fires event when triggers match | None — popup appears |
| **Player-Initiated** | Player clicks menu option to start event | Click menu option |

### One Presentation Style

**All events use Inquiry Popups** — the standard Bannerlord dialog with options.

We do NOT use:
- Map Incidents (too heavy for most events)
- Scene transitions
- Custom UI

---

## Delivery Methods

### Inquiry Popup (All Events)

```
┌─────────────────────────────────────────────────────────────┐
│ [Event Title]                                               │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│ [Setup text — 2-4 sentences describing the situation]       │
│                                                             │
├─────────────────────────────────────────────────────────────┤
│ ○ Option 1 text                                             │
│ ○ Option 2 text                                             │
│ ○ Option 3 text                                             │
│ ○ Option 4 text                                             │
└─────────────────────────────────────────────────────────────┘
```

Implementation:
```csharp
InformationManager.ShowInquiry(
    new InquiryData(
        titleText: event.Title,
        text: event.Setup,
        isAffirmativeOptionShown: true,
        isNegativeOptionShown: true,
        affirmativeText: event.Options[0].Text,
        negativeText: event.Options[1].Text,
        // ... or use MultiSelectionInquiry for 3+ options
    ),
    pauseGameActiveExchangeCurrentTimeSpeed: true
);
```

For events with 3+ options, use `MultiSelectionInquiryData`.

---

## Event Type → Delivery Mapping

### Complete Mapping Table

| Event Category | Event Type | Delivery | Triggered By | Menu |
|----------------|------------|----------|--------------|------|
| **Onboarding** | Enlisted (3 events) | Automatic | `days_since_enlistment`, `onboarding_stage_X` | None |
| **Onboarding** | Officer (3 events) | Automatic | `days_since_promotion`, `onboarding_stage_X` | None |
| **Onboarding** | Commander (3 events) | Automatic | `days_since_promotion`, `onboarding_stage_X` | None |
| **Duty** | All 50 duty events | Automatic | Duty triggers + `ai_safe` | None (but indicator in Duty Menu) |
| **Training** | Infantry (4 events) | Player-Initiated | Player clicks option | Camp Activities |
| **Training** | Cavalry (4 events) | Player-Initiated | Player clicks option | Camp Activities |
| **Training** | Archer (4 events) | Player-Initiated | Player clicks option | Camp Activities |
| **Training** | Naval (4 events) | Player-Initiated | Player clicks option | Camp Activities |
| **General** | Dawn events (3) | Automatic | `time_of_day: dawn` + `ai_safe` | None |
| **General** | Day events (3) | Automatic | `time_of_day: day` + `ai_safe` | None |
| **General** | Evening events (3) | Automatic | `time_of_day: evening` + `ai_safe` | None |
| **General** | Dusk events (3) | Automatic | `time_of_day: dusk` + `ai_safe` | None |
| **General** | Night events (3) | Automatic | `time_of_day: night` + `ai_safe` | None |
| **General** | Late Night events (3) | Automatic | `time_of_day: late_night` + `ai_safe` | None |
| **Camp Tasks** | Help Surgeon, Forge, Forage | Player-Initiated | Player clicks option | Camp Activities |
| **Social** | Fire Circle, Drinking | Player-Initiated | Player clicks option | Camp Activities |
| **Escalation** | Heat threshold events (4) | Automatic | `heat >= threshold` | None |
| **Escalation** | Discipline events (5) | Automatic | `discipline >= threshold` | None |
| **Escalation** | Lance Rep events (4) | Automatic | `lance_rep >= threshold` | None |
| **Escalation** | Medical Risk events (3) | Automatic | `medical_risk >= threshold` | None |

---

## Automatic Events

### What Makes an Event Automatic

Automatic events fire when:
1. Trigger conditions are met (time, state, flags)
2. `ai_safe` check passes (not in battle, encounter, prisoner)
3. Cooldown has expired
4. Rate limit not exceeded (max events per day/week)

### Automatic Event Flow

```
Game Tick (Hourly/Daily)
    ↓
EventManager.CheckTriggers()
    ↓
For each registered event:
    ├── Check trigger conditions
    ├── Check ai_safe
    ├── Check cooldown
    └── Check rate limit
    ↓
If all pass → Queue event
    ↓
Next safe moment → Fire inquiry popup
    ↓
Player selects option
    ↓
Apply effects (XP, fatigue, escalation, etc.)
    ↓
Set cooldown
```

### Automatic Event Categories

#### Onboarding Events

```csharp
// Triggers
"triggers": {
    "all": ["is_enlisted", "ai_safe", "onboarding_stage_1"],
    "days_since_enlistment": { "max": 1 }
}

// Fires automatically when player enlists
// Uses flag progression: stage_1 → stage_2 → stage_3 → complete
```

#### Duty Events

```csharp
// Triggers
"triggers": {
    "all": ["is_enlisted", "ai_safe", "has_duty:scout"],
    "any": ["before_battle", "daily_tick"],
    "time_of_day": ["morning", "afternoon"]
}

// Fires automatically based on duty + campaign state
// Player sees indicator in Duty Menu but doesn't initiate
```

#### General Events

```csharp
// Triggers
"triggers": {
    "all": ["is_enlisted", "ai_safe"],
    "time_of_day": ["evening", "dusk"]
}

// Fires automatically based on time of day
// Random selection from eligible events
```

#### Escalation Threshold Events

```csharp
// Triggers (internal, not JSON)
// Checked after any event that modifies escalation tracks

if (heat >= 5 && !IsOnCooldown("heat_shakedown"))
{
    QueueEvent("heat_shakedown");
}

// Fires automatically when threshold reached
```

---

## Player-Initiated Events

### What Makes an Event Player-Initiated

Player-initiated events:
1. Listed as menu options
2. Fire when player clicks the option
3. Still show as inquiry popup (same presentation)
4. Menu checks availability (cooldown, fatigue, conditions)

### Player-Initiated Flow

```
Player opens Camp Activities menu
    ↓
Menu shows available options with:
    ├── Current status (available, cooldown, disabled)
    ├── Costs (fatigue)
    └── Rewards (XP)
    ↓
Player clicks option
    ↓
Menu checks:
    ├── Cooldown clear?
    ├── Enough fatigue capacity?
    └── No restricting conditions?
    ↓
If all pass → Fire inquiry popup
    ↓
Player selects option within event
    ↓
Apply effects
    ↓
Return to menu (refreshed)
```

### Player-Initiated Categories

#### Training Events

**Menu:** Camp Activities → Training Section

```csharp
// Menu option
starter.AddGameMenuOption("enlisted_activities", "train_shield_wall",
    "Shield Wall Drill [+25 Polearm, +20 1H | +2 Fatigue]",
    args => {
        // Condition checks
        if (GetPlayerFormation() != Formation.Infantry) return false;
        
        if (IsOnCooldown("inf_train_shield_wall"))
        {
            args.Tooltip = new TextObject($"Cooldown: {GetCooldownDays("inf_train_shield_wall")} days");
            args.IsEnabled = false;
        }
        else if (GetFatigue() > GetMaxFatigue() - 2)
        {
            args.Tooltip = new TextObject("Too fatigued for training");
            args.IsEnabled = false;
        }
        else if (HasRestrictingInjury())
        {
            args.Tooltip = new TextObject("Your injury prevents training");
            args.IsEnabled = false;
        }
        
        return true;
    },
    args => {
        // Fire the event as inquiry popup
        TriggerStoryEvent("inf_train_shield_wall");
    });
```

**Event JSON:**
```json
{
    "id": "inf_train_shield_wall",
    "category": "training",
    "formation": "infantry",
    "delivery": "player_initiated",
    "menu": "enlisted_activities",
    "cooldown_days": 2,
    
    "setup": "The drill sergeant bellows orders...",
    
    "options": [
        {
            "id": "standard",
            "text": "Hold the line, focus on form",
            "risk": "safe",
            "costs": { "fatigue": 2 },
            "rewards": { "xp": { "polearm": 25, "one_handed": 20 } }
        },
        // ... more options
    ]
}
```

#### Camp Tasks

**Menu:** Camp Activities → Camp Tasks Section

```csharp
starter.AddGameMenuOption("enlisted_activities", "task_surgeon",
    "Help the Surgeon [+20 Medicine | +1 Fatigue]",
    args => {
        // Only show if wounded in camp
        if (GetArmyWoundedCount() == 0)
        {
            args.IsEnabled = false;
            args.Tooltip = new TextObject("No wounded to tend");
        }
        return true;
    },
    args => TriggerStoryEvent("task_help_surgeon"));
```

#### Social Events

**Menu:** Camp Activities → Social Section

```csharp
starter.AddGameMenuOption("enlisted_activities", "social_fire",
    "Join the Fire Circle [+15 Charm | −1 Fatigue]",
    args => {
        var time = GetTimeOfDay();
        if (time != TimeOfDay.Evening && time != TimeOfDay.Dusk)
        {
            args.IsEnabled = false;
            args.Tooltip = new TextObject("Only available in evening");
        }
        return true;
    },
    args => TriggerStoryEvent("social_fire_circle"));
```

---

## Menu Integration

### Menu Structure Overview

```
Enlisted Status Menu
├── My Lance                    ← No events, just roster display
├── Camp Activities             ← PLAYER-INITIATED EVENTS HERE
│   ├── Training Section        ← Training events
│   ├── Camp Tasks Section      ← Task events
│   └── Social Section          ← Social events
├── Report for Duty             ← Shows duty event INDICATORS (events are automatic)
├── Seek Medical                ← Treatment options (not story events)
└── Camp                        ← Activity log, XP tracking
```

### Camp Activities Menu — Full Mapping

```csharp
// File: EnlistedActivitiesMenuBehavior.cs

public class EnlistedActivitiesMenuBehavior : CampaignBehaviorBase
{
    private void AddMenuOptions(CampaignGameStarter starter)
    {
        // ═══════════════════════════════════════════════════
        // TRAINING SECTION
        // ═══════════════════════════════════════════════════
        
        // Infantry Training
        AddTrainingOption(starter, "train_shield_wall", "Shield Wall Drill", 
            "inf_train_shield_wall", Formation.Infantry,
            "+25 Polearm, +20 1H", 2);
        
        AddTrainingOption(starter, "train_sparring", "Sparring Circle",
            "inf_train_sparring", Formation.Infantry,
            "+30 OneHanded", 2);
        
        AddTrainingOption(starter, "train_march", "March Conditioning",
            "inf_train_march", Formation.Infantry,
            "+30 Athletics", 3);
        
        AddTrainingOption(starter, "train_twohanded", "Two-Handed Drill",
            "inf_train_twohanded", Formation.Infantry,
            "+25 TwoHanded", 2);
        
        // Cavalry Training
        AddTrainingOption(starter, "train_horse", "Horse Rotation",
            "cav_train_horse", Formation.Cavalry,
            "+30 Riding", 2);
        
        AddTrainingOption(starter, "train_mounted", "Mounted Combat",
            "cav_train_mounted", Formation.Cavalry,
            "+25 Riding, +25 Polearm", 2);
        
        AddTrainingOption(starter, "train_charge", "Charge Practice",
            "cav_train_charge", Formation.Cavalry,
            "+30 Riding, +25 Polearm", 2);
        
        AddTrainingOption(starter, "train_skirmish", "Skirmish Tactics",
            "cav_train_skirmish", Formation.Cavalry,
            "+25 Riding, +25 Throwing", 2);
        
        // Archer Training
        AddTrainingOption(starter, "train_target", "Target Practice",
            "arch_train_target", Formation.Archer,
            "+25 Bow", 1);
        
        AddTrainingOption(starter, "train_volley", "Volley Drill",
            "arch_train_volley", Formation.Archer,
            "+25 Bow, +20 Tactics", 2);
        
        AddTrainingOption(starter, "train_moving", "Moving Targets",
            "arch_train_moving", Formation.Archer,
            "+30 Bow, +20 Scouting", 2);
        
        AddTrainingOption(starter, "train_hunting", "Hunting Party",
            "arch_train_hunting", Formation.Archer,
            "+30 Bow, +30 Scouting", 3);
        
        // Naval Training (only at sea)
        AddNavalTrainingOption(starter, "train_boarding", "Boarding Drill",
            "nav_train_boarding", "+30 Mariner, +25 Athletics", 2);
        
        AddNavalTrainingOption(starter, "train_rigging", "Rigging Practice",
            "nav_train_rigging", "+30 Mariner, +25 Athletics", 2);
        
        AddNavalTrainingOption(starter, "train_navigation", "Navigation Lesson",
            "nav_train_navigation", "+30 Shipmaster", 1);
        
        AddNavalTrainingOption(starter, "train_storm", "Storm Drill",
            "nav_train_storm", "+30 Mariner, +25 Leadership", 2);
        
        // ═══════════════════════════════════════════════════
        // CAMP TASKS SECTION
        // ═══════════════════════════════════════════════════
        
        AddCampTask(starter, "task_surgeon", "Help the Surgeon",
            "task_help_surgeon", "+20 Medicine", 1,
            () => GetArmyWoundedCount() > 0,
            "No wounded to tend");
        
        AddCampTask(starter, "task_forge", "Work the Forge",
            "task_forge_work", "+20 Smithing", 2,
            () => GetDaysSinceBattle() < 3,
            "No repairs needed");
        
        AddCampTask(starter, "task_forage", "Forage for Camp",
            "task_forage", "+15 Scouting, +10 Steward", 2,
            () => GetLogisticsStrain() > 50,
            "Supplies adequate");
        
        // ═══════════════════════════════════════════════════
        // SOCIAL SECTION
        // ═══════════════════════════════════════════════════
        
        AddSocialOption(starter, "social_fire", "Join the Fire Circle",
            "social_fire_circle", "+15 Charm", -1,
            new[] { TimeOfDay.Evening, TimeOfDay.Dusk },
            "Only available in evening");
        
        AddSocialOption(starter, "social_drink", "Drink with the Lads",
            "social_drinking", "+10 Charm, +1 Heat", -2,
            new[] { TimeOfDay.Evening, TimeOfDay.Dusk, TimeOfDay.Night },
            "Only available at night");
    }
    
    // ═══════════════════════════════════════════════════
    // HELPER METHODS
    // ═══════════════════════════════════════════════════
    
    private void AddTrainingOption(CampaignGameStarter starter, 
        string optionId, string displayName, string eventId,
        Formation requiredFormation, string rewardText, int fatigueCost)
    {
        starter.AddGameMenuOption("enlisted_activities", optionId,
            $"{displayName} [{rewardText} | +{fatigueCost} Fatigue]",
            args => {
                // Only show for correct formation
                if (GetPlayerFormation() != requiredFormation) 
                    return false;
                
                // Check availability
                if (IsOnCooldown(eventId))
                {
                    int days = GetCooldownDaysRemaining(eventId);
                    args.Tooltip = new TextObject($"Cooldown: {days} days");
                    args.IsEnabled = false;
                }
                else if (GetCurrentFatigue() > GetMaxFatigue() - fatigueCost)
                {
                    args.Tooltip = new TextObject("Too fatigued");
                    args.IsEnabled = false;
                }
                else if (HasRestrictingInjury())
                {
                    args.Tooltip = new TextObject("Your injury prevents training");
                    args.IsEnabled = false;
                }
                
                return true;
            },
            args => TriggerStoryEvent(eventId));
    }
    
    private void AddCampTask(CampaignGameStarter starter,
        string optionId, string displayName, string eventId,
        string rewardText, int fatigueCost,
        Func<bool> availabilityCheck, string unavailableReason)
    {
        starter.AddGameMenuOption("enlisted_activities", optionId,
            $"{displayName} [{rewardText} | +{fatigueCost} Fatigue]",
            args => {
                if (!availabilityCheck())
                {
                    args.Tooltip = new TextObject(unavailableReason);
                    args.IsEnabled = false;
                }
                else if (IsOnCooldown(eventId))
                {
                    int days = GetCooldownDaysRemaining(eventId);
                    args.Tooltip = new TextObject($"Cooldown: {days} days");
                    args.IsEnabled = false;
                }
                return true;
            },
            args => TriggerStoryEvent(eventId));
    }
    
    private void AddSocialOption(CampaignGameStarter starter,
        string optionId, string displayName, string eventId,
        string rewardText, int fatigueChange,
        TimeOfDay[] validTimes, string timeReason)
    {
        string fatigueText = fatigueChange < 0 
            ? $"{Math.Abs(fatigueChange)} Fatigue relief"
            : $"+{fatigueChange} Fatigue";
        
        starter.AddGameMenuOption("enlisted_activities", optionId,
            $"{displayName} [{rewardText} | {fatigueText}]",
            args => {
                var currentTime = GetTimeOfDay();
                if (!validTimes.Contains(currentTime))
                {
                    args.Tooltip = new TextObject(timeReason);
                    args.IsEnabled = false;
                }
                else if (IsOnCooldown(eventId))
                {
                    int days = GetCooldownDaysRemaining(eventId);
                    args.Tooltip = new TextObject($"Cooldown: {days} days");
                    args.IsEnabled = false;
                }
                return true;
            },
            args => TriggerStoryEvent(eventId));
    }
    
    // ═══════════════════════════════════════════════════
    // EVENT TRIGGERING
    // ═══════════════════════════════════════════════════
    
    private void TriggerStoryEvent(string eventId)
    {
        var storyEvent = StoryPackManager.Instance.GetEvent(eventId);
        if (storyEvent != null)
        {
            // Close menu and show event as inquiry popup
            GameMenu.ExitToLast();
            LanceLifeEventManager.Instance.FireEventImmediately(storyEvent);
        }
        else
        {
            ModLogger.Warn("Activities", $"Event not found: {eventId}");
        }
    }
}
```

### Duty Menu — Event Indicators (Not Triggers)

Duty events are **automatic**, but the Duty Menu shows when one is pending:

```csharp
// In EnlistedDutySelectionBehavior.cs

private string BuildDutyHeader()
{
    var sb = new StringBuilder();
    
    sb.AppendLine($"Current Duty: {GetCurrentDuty()}");
    
    // Show pending event indicator
    var pendingEvent = GetPendingDutyEvent();
    if (pendingEvent != null)
    {
        sb.AppendLine();
        sb.AppendLine($"[!] PENDING: {pendingEvent.Title}");
        sb.AppendLine($"    {pendingEvent.ShortDescription}");
    }
    
    return sb.ToString();
}

// Note: The pending event fires automatically when safe
// The indicator is informational only — player doesn't click to trigger
```

---

## Implementation Patterns

### Pattern 1: Automatic Event Registration

```csharp
// In LanceLifeEventBehavior.cs

public override void RegisterEvents()
{
    // Daily tick for general events
    CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
    
    // Hourly tick for time-sensitive events
    CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
    
    // Battle end for duty events
    CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnBattleEnd);
    
    // Settlement entered
    CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
}

private void OnHourlyTick()
{
    if (!IsEnlisted() || !IsAiSafe()) return;
    
    // Check onboarding events
    CheckOnboardingEvents();
    
    // Check general events (time-based)
    CheckGeneralEvents();
    
    // Check duty events
    CheckDutyEvents();
    
    // Check escalation thresholds
    CheckEscalationThresholds();
}

private void CheckOnboardingEvents()
{
    if (IsOnboardingComplete()) return;
    
    var stage = GetOnboardingStage();
    var track = GetOnboardingTrack();
    var variant = GetOnboardingVariant();
    
    var eventId = $"{track}_onboard_{stage}";
    var storyEvent = StoryPackManager.Instance.GetEvent(eventId, variant);
    
    if (storyEvent != null && MeetsTriggerConditions(storyEvent))
    {
        QueueEvent(storyEvent);
    }
}
```

### Pattern 2: Player-Initiated Event Triggering

```csharp
// In EnlistedActivitiesMenuBehavior.cs

private void TriggerStoryEvent(string eventId)
{
    var storyEvent = StoryPackManager.Instance.GetEvent(eventId);
    
    if (storyEvent == null)
    {
        ModLogger.Warn("Activities", $"Event not found: {eventId}");
        return;
    }
    
    // Close the menu
    GameMenu.ExitToLast();
    
    // Show the event as inquiry popup
    ShowEventInquiry(storyEvent);
}

private void ShowEventInquiry(StoryEvent storyEvent)
{
    // Resolve placeholders in setup text
    string setupText = PlaceholderResolver.Resolve(storyEvent.Setup);
    
    // Build options
    var options = storyEvent.Options
        .Where(o => MeetsOptionConditions(o))
        .Select(o => new InquiryElement(
            o.Id,
            PlaceholderResolver.Resolve(o.Text),
            null,
            true,
            BuildOptionTooltip(o)))
        .ToList();
    
    // Show multi-selection inquiry
    MBInformationManager.ShowMultiSelectionInquiry(
        new MultiSelectionInquiryData(
            storyEvent.Title,
            setupText,
            options,
            isExitShown: false,
            maxSelectableOptionCount: 1,
            affirmativeText: "Select",
            negativeText: null,
            onAffirmativeAction: (selected) => OnOptionSelected(storyEvent, selected),
            onNegativeAction: null
        ),
        pauseGameActiveExchangeCurrentTimeSpeed: true
    );
}

private void OnOptionSelected(StoryEvent storyEvent, List<InquiryElement> selected)
{
    if (selected.Count == 0) return;
    
    var optionId = (string)selected[0].Identifier;
    var option = storyEvent.Options.First(o => o.Id == optionId);
    
    // Show outcome
    string outcomeText = PlaceholderResolver.Resolve(option.Outcome);
    
    InformationManager.ShowInquiry(
        new InquiryData(
            "Outcome",
            outcomeText,
            true, false,
            "Continue", null,
            () => ApplyOptionEffects(storyEvent, option),
            null
        ),
        pauseGameActiveExchangeCurrentTimeSpeed: true
    );
}

private void ApplyOptionEffects(StoryEvent storyEvent, StoryEventOption option)
{
    // Apply XP
    if (option.Rewards?.Xp != null)
    {
        foreach (var (skill, amount) in option.Rewards.Xp)
        {
            ApplySkillXp(skill, amount);
        }
    }
    
    // Apply fatigue
    if (option.Costs?.Fatigue > 0)
    {
        ApplyFatigue(option.Costs.Fatigue);
    }
    
    // Apply escalation
    if (option.Effects != null)
    {
        EscalationManager.Instance.ApplyEffects(option.Effects);
    }
    
    // Set cooldown
    SetCooldown(storyEvent.Id, storyEvent.CooldownDays);
    
    // Return to menu if player-initiated
    if (storyEvent.Delivery == EventDelivery.PlayerInitiated)
    {
        GameMenu.SwitchToMenu("enlisted_activities");
    }
}
```

### Pattern 3: Event JSON with Delivery Metadata

```json
{
    "id": "inf_train_shield_wall",
    "category": "training",
    "delivery": "player_initiated",
    "menu_section": "training",
    "menu_id": "enlisted_activities",
    
    "formation": "infantry",
    "cooldown_days": 2,
    
    "display": {
        "menu_text": "Shield Wall Drill",
        "reward_preview": "+25 Polearm, +20 1H",
        "cost_preview": "+2 Fatigue"
    },
    
    "setup": "The drill sergeant bellows...",
    
    "options": [...]
}
```

```json
{
    "id": "scout_terrain_recon",
    "category": "duty",
    "delivery": "automatic",
    "duty": "scout",
    
    "cooldown_days": 3,
    
    "triggers": {
        "all": ["is_enlisted", "ai_safe", "has_duty:scout"],
        "any": ["before_battle", "daily_tick"]
    },
    
    "setup": "Battle is expected soon...",
    
    "options": [...]
}
```

---

## Summary Tables

### Events by Delivery Method

| Delivery | Count | Event Types |
|----------|-------|-------------|
| **Automatic** | ~75 | Onboarding (9), Duty (50), General (18), Escalation (16) |
| **Player-Initiated** | ~25 | Training (16), Camp Tasks (3-5), Social (2-3) |

### Menu Responsibilities

| Menu | Shows | Triggers Events? |
|------|-------|------------------|
| Enlisted Status | Status header, navigation | No |
| My Lance | Roster, relationships | No |
| **Camp Activities** | Training, tasks, social options | **Yes — player-initiated events** |
| Report for Duty | Duty selection, pending indicators | No (events fire automatically) |
| Seek Medical | Treatment options | No (not story events) |
| Camp | Activity log, XP tracking | No |

### Event Category Quick Reference

| Category | Delivery | Menu | Trigger |
|----------|----------|------|---------|
| Onboarding | Automatic | — | Days since enlistment + stage flags |
| Duty | Automatic | — | Duty type + campaign triggers |
| Training | Player-Initiated | Camp Activities | Player clicks |
| General | Automatic | — | Time of day + conditions |
| Camp Tasks | Player-Initiated | Camp Activities | Player clicks (if available) |
| Social | Player-Initiated | Camp Activities | Player clicks (if time valid) |
| Escalation | Automatic | — | Threshold reached |

---

*Document version: 1.0*
*Integrates with: menu_system_update.md, onboarding_story_pack.md, lance_life_events_content_library.md*
