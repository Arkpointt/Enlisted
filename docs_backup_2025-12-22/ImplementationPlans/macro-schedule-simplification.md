# Macro Schedule & Interface Simplification

**Created**: December 20, 2025  
**Status**: ✅ COMPLETE - Code implemented, systems deleted  
**Related**: traits-identity-system.md, storyblocks-v2-implementation.md

> **Note**: This document describes what was ALREADY IMPLEMENTED. The Schedule, Lance, and Duties systems have been deleted from the codebase. The Orders system, Role-based events, and Native Game Menu are now live. See `StoryBlocks/storyblocks-v2-implementation.md` for event conversion guidance.

---

## Table of Contents

1. [Problem Statement](#problem-statement)
2. [Current System Analysis](#current-system-analysis)
3. [Proposed Solutions](#proposed-solutions)
4. [Recommendation](#recommendation)
5. [Implementation Plan](#implementation-plan)
6. [Migration Strategy](#migration-strategy)

---

## Problem Statement

### Current Complexity

**Camp Management Window** (5 tabs):
- Lance Tab (roster, members) → DELETING (per traits doc)
- Schedule/Orders Tab → TOO GRANULAR?
- Duties Tab → DELETING (per traits doc)
- Reports Tab → KEEP (stats/info useful)
- Army Tab → CONTEXT INFO (useful)

**Schedule System** (Very Granular):
- 4 time blocks per day (Morning/Afternoon/Dusk/Night)
- Activities auto-assigned based on formation/context
- ~10-15 activity types with weighted assignment
- Daily schedule generation and execution
- Schedule popup events tied to specific time blocks
- ~1500+ lines of scheduling infrastructure

### Issues

1. **Too Much Micromanagement**: Player sees 4 schedule blocks every day
2. **Limited Player Agency**: Activities are auto-assigned, not chosen
3. **Unnecessary Complexity**: Lots of code for minimal gameplay value
4. **Interface Bloat**: 5-tab Camp Management screen for diminishing returns
5. **Misalignment with Trait System**: Granular schedule doesn't support emergent identity

### Questions

With Lances and Duties being removed, what purpose does the Camp Management screen serve?

If identity emerges from choices (not systems), why do we need scheduled activities?

---

## Current System Analysis

### Camp Management Window

**File Structure:**
```
src/Features/Camp/UI/Management/
├── CampManagementVM.cs (main controller)
├── CampManagementScreen.cs
└── Tabs/
    ├── CampLanceVM.cs → DELETE (lance system)
    ├── CampScheduleVM.cs → SIMPLIFY/DELETE?
    ├── CampDutiesVM.cs → DELETE (duty system)
    ├── CampReportsVM.cs → KEEP (useful stats)
    └── CampArmyVM.cs → KEEP (context info)
```

**Lines of Code:**
- Camp Management UI: ~800 lines
- Lance Tab: ~300 lines (DELETING)
- Schedule Tab: ~250 lines
- Duties Tab: ~400 lines (DELETING)
- Reports Tab: ~200 lines (KEEP)
- Army Tab: ~150 lines (KEEP)

**Total Deletable**: ~950 lines if we remove schedule

### Schedule System

**File Structure:**
```
src/Features/Schedule/
├── Behaviors/ScheduleBehavior.cs (~400 lines)
├── Core/
│   ├── ScheduleGenerator.cs (~500 lines)
│   ├── ScheduleExecutor.cs (~300 lines)
│   └── ArmyStateAnalyzer.cs (~200 lines - KEEP for context)
├── Models/
│   ├── DailySchedule.cs
│   ├── TimeBlock.cs
│   ├── ScheduledBlock.cs
│   └── ScheduleBlockType.cs
├── Config/
│   ├── ScheduleConfig.cs
│   └── ScheduleActivityDefinition.cs
└── Events/
    └── SchedulePopupEventCatalog.cs (~300 lines)
```

**Total Lines**: ~1700 lines

**What Schedule Does:**
1. Generates daily 4-block schedule based on:
   - Player formation (DELETING)
   - Context (siege, war, peace)
   - Skill gates (Medicine 40+, Engineering 40+, etc.)
   - Variety control (avoid repetition)

2. Executes schedule blocks:
   - Awards passive XP for activities
   - 20% chance to fire schedule events
   - Updates time blocks visually

3. Provides structure:
   - Morning: Primary assignment
   - Afternoon: Secondary assignment
   - Dusk: Free time or assignment
   - Night: Rest

### What Would Be Lost

If we delete the entire schedule system:
- **4-block daily structure** (Morning/Afternoon/Dusk/Night)
- **Scheduled activities** (Patrol, Sentry, Work Detail, etc.)
- **Passive XP** from executing activities
- **Schedule-triggered events** (20% chance per block)
- **Visual schedule display** (see tomorrow's assignments)
- **Time-of-day flavor** ("morning drill", "night watch")

**Key Question**: Is this structure necessary for good gameplay?

---

## Proposed Solutions

### Option 1: Delete Schedule Entirely (Maximum Simplification)

**Replace With**: Pure event-driven gameplay

**How It Works:**
```
NO SCHEDULE AT ALL

Events fire based on:
- Context (war, siege, peace, town)
- Time passing (hourly tick, daily tick)
- Player actions (camp menu, decisions)
- Escalation thresholds (Scrutiny 7+, Discipline 8+)
- Random chance (natural event frequency)

Example:
- War context → Higher chance of combat-related events
- Siege context → Siege events fire more often
- Town context → Social/rest events
- Random: "A fellow soldier challenges you to dice" (can fire anytime)
```

**Implementation:**
```csharp
// Delete entire Schedule system
// Events fire from various triggers:

// 1. Hourly/Daily ticks
CampaignEvents.HourlyTickEvent.AddListener(this, OnHourlyTick);
CampaignEvents.DailyTickEvent.AddListener(this, OnDailyTick);

// 2. Context-aware random events
if (IsAtWar() && RandomChance(10%))
{
    FireEvent("patrol_ambush");
}

// 3. Player-initiated from camp menu
"Camp" → [Rest] [Train] [Socialize] [Talk to Lord]
```

**Pros:**
- Maximum simplification (~1700 lines deleted)
- No scheduling UI needed
- Pure event-driven narrative
- Player agency through camp menu choices
- Events feel organic, not scheduled

**Cons:**
- Lose time-of-day flavor ("morning drill")
- Lose structure (some players like routines)
- May feel too random/chaotic
- Harder to balance event frequency

**Net Code Change**: -1700 lines (delete Schedule), +200 lines (enhanced event triggers)

---

### Option 2: Campaign-Level Status (Contextual Simplification)

**Replace With**: Macro "Status" instead of schedule

**How It Works:**
```
CAMPAIGN STATUS (not daily schedule)

Current Status:
- Service: Man-at-Arms (T4), Serving Lord Derthert
- Campaign: Besieging Aserai Castle
- Role: Based on traits (Scout, Officer, Medic, Soldier)
- Standing: Lord 45, Officer 35, Soldier 25

Events fire based on:
- Campaign status (siege, war, march, town)
- Your role (determined by highest traits)
- Context + role = event pool
```

**Example Status Display:**
```
═══════════════════════════════════════════════════════════
ENLISTED STATUS

Rank: Man-at-Arms (T4)
Lord: Derthert of Vlandia
Campaign: Besieging Charax (Day 8 of 12)
Role: Scout (Scout 12, Valor +1)

Reputation:
  Lord:     ████████░░ 45/100 "Respected"
  Officers: ███████░░░ 35/100 "Promising"
  Soldiers: ████░░░░░░ 20/100 "Promising"

Traits: Honorable, Brave, Calculating
Specializations: Scout, Sergeant

[Camp Activities] [Decisions] [My Lord...] [Leave Service]
═══════════════════════════════════════════════════════════
```

**Implementation:**
```csharp
// Replace Schedule with simpler Status tracking
public class EnlistedStatus
{
    // Context
    public string CurrentCampaign { get; set; } // "Besieging Charax"
    public string LordObjective { get; set; } // "Siege", "War", "Peace"
    
    // Role (derived from traits)
    public string PrimaryRole { get; set; } // "Scout", "Officer", "Medic"
    public string RoleDescription { get; set; } // "Scout 12, Valor +1"
    
    // No schedule needed!
}

// Events fire based on status
if (Status.LordObjective == "Siege" && HasRole("Scout"))
{
    // Scout events available during siege
    EventPool = GetEvents("siege", "scout");
}
```

**Pros:**
- Much simpler than full schedule (~1500 lines deleted)
- Provides context without micromanagement
- Role ties directly to trait system
- Status is meaningful and readable
- Still has structure (campaign phases)

**Cons:**
- Lose daily variety (no "tomorrow's schedule")
- Lose time-of-day events ("night watch")
- Less granular than current system

**Net Code Change**: -1500 lines (delete Schedule), +300 lines (Status system)

---

### Option 3: Weekly "Assignment" (Medium Simplification)

**Replace With**: Weekly assignment instead of daily blocks

**How It Works:**
```
WEEKLY ASSIGNMENT (not daily schedule)

This Week: Scout Duty (6 days remaining)
Next Week: Training Duty (auto-assigned)

Events fire during your assignment:
- Scout events during Scout Duty week
- Medical events during Medical Duty week
- Combat events during Patrol Duty week

Assignment changes weekly based on:
- Context (siege → more siege work)
- Skills (Medicine 40+ → eligible for Medical)
- Randomness (variety over months)
```

**Example Display:**
```
═══════════════════════════════════════════════════════════
CURRENT ASSIGNMENT

Week 8 of Campaign Season
Assignment: Scout Duty (6 days remaining)
Next Assignment: Training Duty

Current Mission: Reconnaissance near enemy lines
Recent Events: Suspicious tracks (2 days ago)

[Camp Activities] [Decisions] [Report to Sergeant]
═══════════════════════════════════════════════════════════
```

**Implementation:**
```csharp
// Simplified weekly schedule
public class WeeklyAssignment
{
    public string CurrentAssignment { get; set; } // "Scout Duty"
    public int DaysRemaining { get; set; } // 6
    public string NextAssignment { get; set; } // "Training Duty"
    
    // Generate once per week
    public void GenerateNextWeek()
    {
        // Similar logic to current schedule, but weekly
        CurrentAssignment = NextAssignment;
        NextAssignment = GenerateAssignment(context, skills, traits);
        DaysRemaining = 7;
    }
}
```

**Pros:**
- Still has structure (weekly assignments)
- Much less micromanagement than daily
- Keeps "duty" flavor without duty system
- Easier to understand than 4 blocks per day

**Cons:**
- Still need scheduling system (~800 lines)
- Weekly might be too long (feels static)
- Less variety than daily changes

**Net Code Change**: -900 lines (simplify schedule logic)

---

### Option 4: Role-Based Auto Events (Trait Integration)

**Replace With**: Events auto-route based on your role (from traits)

**How It Works:**
```
NO SCHEDULE - JUST YOUR ROLE

Your Role: Determined by highest traits
- Commander 15+ → Officer role → Leadership events
- Scout 12+ → Scout role → Reconnaissance events
- Surgeon 10+ → Medic role → Medical events
- Rogue 10+ → Operative role → Covert events
- None → Soldier role → Generic events

Events auto-select from your role's pool:
- Officer: tactical decisions, discipline, mentoring
- Scout: reconnaissance, intelligence, exploration
- Medic: triage, treatment, medical crisis
- Operative: espionage, smuggling, covert ops
- Soldier: patrol, sentry, drill, work detail

Context modifies frequency:
- Siege → more siege events for your role
- War → more combat events
- Peace → more social/training events
```

**Example Status:**
```
═══════════════════════════════════════════════════════════
YOUR ROLE

Primary: Scout (Scout 12, Valor +1)
Secondary: Officer Candidate (Sergeant 8, Honor +2)

Current Context: Besieging Charax
Recent Activity: Deep reconnaissance (yesterday)

Available Actions:
[Scout Enemy Position] (role-based)
[Train Soldiers] (secondary role)
[Rest & Recover] (always available)
[Speak with Lord] (context)

═══════════════════════════════════════════════════════════
```

**Implementation:**
```csharp
// Auto-determine role from traits
public string DetermineRole()
{
    var traits = new Dictionary<string, int>
    {
        ["Commander"] = Hero.MainHero.GetTraitLevel(DefaultTraits.Commander),
        ["Surgeon"] = Hero.MainHero.GetTraitLevel(DefaultTraits.Surgeon),
        ["Scout"] = Hero.MainHero.GetTraitLevel(DefaultTraits.ScoutSkills),
        ["Rogue"] = Hero.MainHero.GetTraitLevel(DefaultTraits.RogueSkills),
        ["Sergeant"] = Hero.MainHero.GetTraitLevel(DefaultTraits.SergeantCommandSkills)
    };
    
    var highest = traits.OrderByDescending(x => x.Value).First();
    
    if (highest.Value >= 10) return highest.Key;
    return "Soldier"; // Default role
}

// Events filter by role automatically
var role = DetermineRole();
var contextEvents = GetEventsForRoleAndContext(role, currentContext);
```

**Pros:**
- Directly integrates with trait system
- No schedule system needed (~1700 lines deleted)
- Role-based content feels organic
- Player agency through role development (via traits)
- Very simple to understand

**Cons:**
- Lose schedule structure entirely
- No "tomorrow's plan" preview
- May feel less like military service

**Net Code Change**: -1700 lines (delete Schedule), +300 lines (role detection)

---

## Recommendation

### Primary: Option 4 (Role-Based) + Native Game Menu Interface

**Combine the best of both:**

1. **Delete Schedule System** entirely (~1700 lines)
2. **Delete Camp Management Window** entirely (custom UI, all tabs)
3. **Use Native Game Menu** (accordion-style, text-based, proven system)
4. **Events auto-route** based on role (from traits) + context

**Result:**
```
NATIVE GAME MENU (replaces everything custom)

ENLISTED
  You serve Lord Derthert as Man-at-Arms (T4)
  
  > Camp
      Rest & Recover
      Train Skills
      Socialize
      [Back]
  
  > Reports
      Service Record
      Reputation
      Recent Events
      [Back]
  
  > Decisions
      Scout Enemy Position (available)
      Medical Supplies Request (available)
      [Back]
  
  > Status
      Rank: Man-at-Arms (T4)
      Lord: Derthert of Vlandia
      Campaign: Besieging Charax (Day 8)
      
      Role: Scout (Scout 12, Valor +1)
      
      Reputation:
        Lord:     45/100 (Respected)
        Officers: 35/100 (Promising)
        Soldiers: 20/100 (Promising)
      
      Personality: Honorable, Brave, Calculating
      [Back]
  
  Talk to my lord...
  Leave Service
  [Leave]

NO CUSTOM UI SCREENS
NO CAMP MANAGEMENT WINDOW
NO SCHEDULE TAB
NO DUTIES TAB
NO LANCE TAB
```

**Native Game Menu Benefits:**
- **Zero custom UI code** (uses native menus)
- **Accordion-style** collapsible sections (native support)
- **Text-based** (fast, simple, moddable)
- **Proven system** (used throughout Bannerlord)
- **Keyboard navigation** (built-in)
- **Easy to maintain** (just menu definitions)

**Event Routing:**
```csharp
// No schedule needed - events fire based on role + context

// Hourly/Daily checks
void OnHourlyTick()
{
    var role = DetermineRole(); // "Scout", "Officer", "Medic", etc.
    var context = GetContext(); // "Siege", "War", "Peace", "Town"
    
    // Random event chance (context-modified)
    if (ShouldFireRandomEvent(role, context))
    {
        var eventPool = GetEventsForRoleAndContext(role, context);
        var selectedEvent = ChooseEvent(eventPool);
        FireEvent(selectedEvent);
    }
    
    // Escalation events (always check)
    CheckEscalationThresholds();
}

// Example event pools
Scout Role + Siege Context:
  → "scout_enemy_weakness" (find vulnerability)
  → "scout_deep_reconnaissance" (high risk)
  → "scout_intelligence_report" (lord interaction)

Officer Role + War Context:
  → "officer_tactical_decision" (command choice)
  → "officer_discipline_problem" (handle deserter)
  → "officer_training_request" (mentor soldiers)

Medic Role + Battle Context:
  → "medic_triage_crisis" (save one of three)
  → "medic_field_surgery" (risky procedure)
  → "medic_supplies_shortage" (resource management)
```

### Why This Approach

1. **Maximum Simplification**: ~1700 lines deleted (entire Schedule)
2. **Trait Integration**: Role derives from trait system (unified design)
3. **Context-Aware**: Events still respond to siege/war/peace
4. **Player Agency**: Choose role by developing traits
5. **Less Micromanagement**: No daily schedule to review
6. **Emergent Gameplay**: Role + context + traits = organic content
7. **Authentic**: Military service isn't scheduled minute-by-minute

### What Players Experience

**Before** (Current):
1. Open Camp Management (5 tabs)
2. Check Schedule tab (see tomorrow's 4 blocks)
3. Check Duties tab (maybe change duty)
4. Check Lance tab (see roster)
5. Check Reports tab (see stats)
6. Events fire from schedule (20% per block)

**After** (Recommended):
1. Press hotkey for Enlisted Status (simple overlay)
2. See: Role, Context, Reputation, Recent Events
3. [Camp] menu for player-initiated actions
4. [Decisions] menu for active events
5. Events fire naturally based on role + context

**Result**: Simpler, faster, more focused

---

## Implementation Plan

### Phase 1: Foundation (Week 1)

**1.1: Create Status System**
```csharp
// File: src/Features/Status/EnlistedStatusManager.cs
public class EnlistedStatusManager : CampaignBehaviorBase
{
    private EnlistedStatusState _status;
    
    // Determine role from traits
    public string GetPrimaryRole()
    {
        var hero = Hero.MainHero;
        
        if (hero.GetTraitLevel(DefaultTraits.Commander) >= 10)
            return "Officer";
        if (hero.GetTraitLevel(DefaultTraits.ScoutSkills) >= 10)
            return "Scout";
        if (hero.GetTraitLevel(DefaultTraits.Surgeon) >= 10)
            return "Medic";
        if (hero.GetTraitLevel(DefaultTraits.RogueSkills) >= 10)
            return "Operative";
        if (hero.GetTraitLevel(DefaultTraits.SergeantCommandSkills) >= 8)
            return "NCO";
        
        return "Soldier"; // Default
    }
    
    // Get role description
    public string GetRoleDescription()
    {
        var role = GetPrimaryRole();
        var hero = Hero.MainHero;
        
        return role switch
        {
            "Officer" => $"Commander {hero.GetTraitLevel(DefaultTraits.Commander)}",
            "Scout" => $"Scout {hero.GetTraitLevel(DefaultTraits.ScoutSkills)}",
            "Medic" => $"Surgeon {hero.GetTraitLevel(DefaultTraits.Surgeon)}",
            "Operative" => $"Rogue {hero.GetTraitLevel(DefaultTraits.RogueSkills)}",
            "NCO" => $"Sergeant {hero.GetTraitLevel(DefaultTraits.SergeantCommandSkills)}",
            _ => "Enlisted Soldier"
        };
    }
    
    // Get current context
    public string GetCampaignContext()
    {
        var enlistment = EnlistmentBehavior.Instance;
        var lord = enlistment?.CurrentLord;
        
        if (lord?.CurrentSettlement?.IsUnderSiege == true)
            return "Besieging " + lord.CurrentSettlement.Name;
        
        if (lord?.MapFaction?.IsAtWarWith(Hero.MainHero.MapFaction) == true)
            return "At war";
        
        if (lord?.CurrentSettlement?.IsTown == true)
            return "In town";
        
        return "On campaign";
    }
}
```

**1.2: Create Simple Status UI**
```csharp
// File: src/Features/Status/EnlistedStatusVM.cs
public class EnlistedStatusVM : ViewModel
{
    private string _rankText;
    private string _lordText;
    private string _campaignText;
    private string _roleText;
    private string _roleDescriptionText;
    
    // Reputation bars
    private int _lordReputation;
    private int _officerReputation;
    private int _soldierReputation;
    
    // Trait summary
    private string _personalityText; // "Honorable, Brave, Calculating"
    private string _specializationsText; // "Scout, Sergeant"
    
    public void Refresh()
    {
        var status = EnlistedStatusManager.Instance;
        var enlistment = EnlistmentBehavior.Instance;
        var escalation = EscalationManager.Instance;
        
        RankText = $"{enlistment.CurrentRankTitle} (T{enlistment.EnlistmentTier})";
        LordText = enlistment.CurrentLord?.Name?.ToString() ?? "None";
        CampaignText = status.GetCampaignContext();
        RoleText = status.GetPrimaryRole();
        RoleDescriptionText = status.GetRoleDescription();
        
        LordReputation = escalation.State.LordReputation;
        OfficerReputation = escalation.State.OfficerReputation;
        SoldierReputation = escalation.State.SoldierReputation;
        
        PersonalityText = GetPersonalityTraits();
        SpecializationsText = GetSpecializations();
    }
}
```

**1.3: Enhanced Event Trigger System**
```csharp
// File: src/Features/Events/RoleBasedEventTrigger.cs
public class RoleBasedEventTrigger : CampaignBehaviorBase
{
    public override void RegisterEvents()
    {
        CampaignEvents.HourlyTickEvent.AddListener(this, OnHourlyTick);
        CampaignEvents.DailyTickEvent.AddListener(this, OnDailyTick);
    }
    
    private void OnHourlyTick()
    {
        // Escalation events (always check)
        CheckEscalationEvents();
        
        // Random role-based events (5% chance per hour = ~1-2 per day)
        if (MBRandom.RandomFloat < 0.05f)
        {
            TryFireRoleBasedEvent();
        }
    }
    
    private void TryFireRoleBasedEvent()
    {
        var status = EnlistedStatusManager.Instance;
        var role = status.GetPrimaryRole();
        var context = status.GetCampaignContext();
        
        // Get event pool for role + context
        var eventPool = GetEventPoolForRoleAndContext(role, context);
        
        if (eventPool.Count == 0)
        {
            // Fallback to generic soldier events
            eventPool = GetEventPoolForRole("Soldier", context);
        }
        
        // Filter by requirements (tier, traits, reputation)
        var eligibleEvents = FilterEventsByRequirements(eventPool);
        
        if (eligibleEvents.Count > 0)
        {
            var selectedEvent = eligibleEvents.GetRandomElement();
            FireEvent(selectedEvent);
        }
    }
    
    private List<EventDefinition> GetEventPoolForRoleAndContext(
        string role, 
        string context)
    {
        // Load events tagged with role + context
        var events = EventCatalog.GetEvents();
        
        return events.Where(e => 
            e.Tags.Contains(role.ToLower()) && 
            e.Tags.Contains(GetContextTag(context))
        ).ToList();
    }
}
```

### Phase 2: Delete Schedule System (Week 1)

**2.1: Remove Schedule Files**
```bash
# Delete entire Schedule system
rm -rf src/Features/Schedule/Behaviors/
rm -rf src/Features/Schedule/Core/ScheduleGenerator.cs
rm -rf src/Features/Schedule/Core/ScheduleExecutor.cs
rm -rf src/Features/Schedule/Models/DailySchedule.cs
rm -rf src/Features/Schedule/Models/TimeBlock.cs
rm -rf src/Features/Schedule/Models/ScheduledBlock.cs
rm -rf src/Features/Schedule/Events/

# Keep ArmyStateAnalyzer for context detection
# Keep only: src/Features/Schedule/Core/ArmyStateAnalyzer.cs
# Rename to: src/Features/Context/ArmyContextAnalyzer.cs
```

**2.2: Update Event Tags**

Old events had:
```json
{
  "id": "duty_patrol_ambush",
  "triggers": {
    "schedule": ["morning", "afternoon"]
  }
}
```

New events have:
```json
{
  "id": "scout_ambush",
  "tags": ["scout", "war", "outdoor"],
  "triggers": {
    "role_based": true
  }
}
```

### Phase 3: Simplify Camp Management (Week 2)

**3.1: Remove Tabs**
```csharp
// File: src/Features/Camp/UI/Management/CampManagementVM.cs

// DELETE:
private CampLanceVM _lance; // Lance system deleted
private CampScheduleVM _schedule; // Schedule deleted
private CampDutiesVM _duties; // Duties deleted

// KEEP:
private CampReportsVM _reports; // Stats still useful
private CampArmyVM _army; // Context info useful
```

**3.2: Replace with Native Game Menu**
```csharp
// File: src/Features/Interface/EnlistedMenuBehavior.cs

public void AddEnlistedGameMenus(CampaignGameStarter campaignGameStarter)
{
    // Main Enlisted Menu (entry point)
    campaignGameStarter.AddGameMenu(
        "enlisted_main",
        "You serve {LORD_NAME} as {RANK} (T{TIER})\n\nCurrent Campaign: {CAMPAIGN_CONTEXT}",
        OnEnlistedMenuInit);
    
    // Camp submenu (accordion-style)
    campaignGameStarter.AddGameMenuOption(
        "enlisted_main",
        "enlisted_camp",
        "Camp",
        args => 
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
            return true;
        },
        args => GameMenu.SwitchToMenu("enlisted_camp"));
    
    campaignGameStarter.AddGameMenu(
        "enlisted_camp",
        "What would you like to do?",
        null);
    
    campaignGameStarter.AddGameMenuOption(
        "enlisted_camp",
        "camp_rest",
        "Rest & Recover",
        args => true,
        args => OnRestSelected());
    
    campaignGameStarter.AddGameMenuOption(
        "enlisted_camp",
        "camp_train",
        "Train Skills",
        args => true,
        args => OnTrainSelected());
    
    campaignGameStarter.AddGameMenuOption(
        "enlisted_camp",
        "camp_socialize",
        "Socialize",
        args => true,
        args => OnSocializeSelected());
    
    campaignGameStarter.AddGameMenuOption(
        "enlisted_camp",
        "camp_back",
        "Back",
        args => 
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Leave;
            return true;
        },
        args => GameMenu.SwitchToMenu("enlisted_main"));
    
    // Reports submenu
    campaignGameStarter.AddGameMenuOption(
        "enlisted_main",
        "enlisted_reports",
        "Reports",
        args => 
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
            return true;
        },
        args => GameMenu.SwitchToMenu("enlisted_reports"));
    
    campaignGameStarter.AddGameMenu(
        "enlisted_reports",
        GetReportsText(),
        null);
    
    campaignGameStarter.AddGameMenuOption(
        "enlisted_reports",
        "reports_back",
        "Back",
        args => 
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Leave;
            return true;
        },
        args => GameMenu.SwitchToMenu("enlisted_main"));
    
    // Decisions submenu (active events)
    campaignGameStarter.AddGameMenuOption(
        "enlisted_main",
        "enlisted_decisions",
        "Decisions",
        args => 
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
            args.IsEnabled = HasActiveDecisions();
            args.Tooltip = HasActiveDecisions() 
                ? TextObject.Empty 
                : new TextObject("No pending decisions");
            return true;
        },
        args => GameMenu.SwitchToMenu("enlisted_decisions"));
    
    // Status submenu (text-based display)
    campaignGameStarter.AddGameMenuOption(
        "enlisted_main",
        "enlisted_status",
        "Status",
        args => 
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
            return true;
        },
        args => GameMenu.SwitchToMenu("enlisted_status"));
    
    campaignGameStarter.AddGameMenu(
        "enlisted_status",
        GetStatusText(),
        null);
    
    campaignGameStarter.AddGameMenuOption(
        "enlisted_status",
        "status_back",
        "Back",
        args => 
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Leave;
            return true;
        },
        args => GameMenu.SwitchToMenu("enlisted_main"));
    
    // Talk to lord
    campaignGameStarter.AddGameMenuOption(
        "enlisted_main",
        "talk_to_lord",
        "Talk to my lord...",
        args => 
        {
            args.IsEnabled = IsLordAvailable();
            return true;
        },
        args => StartLordConversation());
    
    // Leave service
    campaignGameStarter.AddGameMenuOption(
        "enlisted_main",
        "leave_service",
        "Leave Service",
        args => 
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Leave;
            return true;
        },
        args => ShowLeaveConfirmation());
}

private string GetStatusText()
{
    var status = EnlistedStatusManager.Instance;
    var enlistment = EnlistmentBehavior.Instance;
    var escalation = EscalationManager.Instance;
    
    return $@"Rank: {enlistment.CurrentRankTitle} (T{enlistment.EnlistmentTier})
Lord: {enlistment.CurrentLord?.Name}
Campaign: {status.GetCampaignContext()}

Your Role: {status.GetPrimaryRole()}
  {status.GetRoleDescription()}

Reputation:
  Lord:     {escalation.State.LordReputation}/100 ({GetReputationLevel(escalation.State.LordReputation)})
  Officers: {escalation.State.OfficerReputation}/100 ({GetReputationLevel(escalation.State.OfficerReputation)})
  Soldiers: {escalation.State.SoldierReputation}/100 ({GetReputationLevel(escalation.State.SoldierReputation)})

Personality: {GetPersonalityTraits()}
Specializations: {GetSpecializations()}";
}

private string GetReputationLevel(int value)
{
    // -100 to +100 scale
    if (value >= 80) return "Celebrated";
    if (value >= 60) return "Trusted";
    if (value >= 40) return "Respected";
    if (value >= 20) return "Promising";
    if (value >= 0) return "Neutral";
    if (value >= -20) return "Questionable";
    if (value >= -40) return "Disliked";
    if (value >= -60) return "Despised";
    if (value >= -80) return "Hated";
    return "Enemy";
}

private string GetReportsText()
{
    var service = ServiceRecordManager.Instance;
    
    return $@"Service Record:
  Days Served: {service.CurrentTerm.DaysServed}
  Battles: {service.CurrentTerm.BattlesWon + service.CurrentTerm.BattlesLost}
  Victories: {service.CurrentTerm.BattlesWon}
  Kills: {service.CurrentTerm.TotalKills}
  
Recent Events:
  {GetRecentEventsText()}";
}
```

### Phase 4: Tag All Events (Week 2-3)

**4.1: Event Tagging Schema**

Add tags to all 80+ events:

**Role Tags:**
- `scout` - Reconnaissance, intelligence
- `officer` - Leadership, command
- `medic` - Medical, triage
- `operative` - Covert, criminal
- `nco` - Training, mentoring
- `soldier` - Generic (fallback)

**Context Tags:**
- `war` - Active warfare
- `siege` - Siege operations
- `peace` - Peacetime
- `town` - In settlement
- `outdoor` - Field/campaign
- `camp` - Camp/bivouac

**Example Conversion:**
```json
// OLD (schedule-based)
{
  "id": "duty_patrol_ambush",
  "requirements": {
    "duty": "patrol",
    "formation": "infantry"
  },
  "triggers": {
    "schedule": ["morning", "afternoon"]
  }
}

// NEW (role-based)
{
  "id": "scout_patrol_ambush",
  "tags": ["scout", "soldier", "war", "outdoor"],
  "requirements": {
    "tier": { "min": 2 },
    "context": {
      "at_war": true
    }
  },
  "triggers": {
    "role_based": true
  }
}
```

### Phase 5: Balance Event Frequency (Week 3-4)

**5.1: Tune Event Rates**

Target: 1-2 events per day (random)

```csharp
// Hourly tick (5% chance = ~1.2 events/day)
const float EventChancePerHour = 0.05f;

// Context modifiers
if (context == "Siege") EventChancePerHour *= 1.5f; // More during siege
if (context == "Peace") EventChancePerHour *= 0.5f; // Less in peace
if (role == "Officer") EventChancePerHour *= 1.2f; // Officers get more events
```

**5.2: Verify Coverage**

Ensure events available for all role + context combinations:

| Role | Siege | War | Peace | Town |
|------|-------|-----|-------|------|
| Scout | ✓ (10+) | ✓ (15+) | ✓ (5+) | ✓ (3+) |
| Officer | ✓ (8+) | ✓ (12+) | ✓ (10+) | ✓ (5+) |
| Medic | ✓ (12+) | ✓ (10+) | ✓ (5+) | ✓ (8+) |
| Operative | ✓ (5+) | ✓ (8+) | ✓ (10+) | ✓ (15+) |
| NCO | ✓ (10+) | ✓ (10+) | ✓ (12+) | ✓ (8+) |
| Soldier | ✓ (15+) | ✓ (20+) | ✓ (10+) | ✓ (10+) |

### Phase 6: Testing & Polish (Week 4)

**6.1: Test Event Frequency**
- Average 1-2 events per day
- No long dry spells (> 3 days without event)
- Appropriate event variety for role

**6.2: Test Role Transitions**
- Scout → Officer (change traits)
- Soldier → Medic (level Surgeon trait)
- Events adapt to new role

**6.3: Test Context Changes**
- Peace → War (event pool shifts)
- Field → Siege (siege events fire)
- Campaign → Town (social events increase)

---

## Migration Strategy

### Save Compatibility

**Schedule Data Removal:**
```csharp
// File: src/Features/Status/EnlistedStatusManager.cs

public override void SyncData(IDataStore dataStore)
{
    if (dataStore.IsLoading)
    {
        // Discard old schedule data (don't save back)
        DailySchedule ignoredSchedule = null;
        TimeBlock ignoredTimeBlock = TimeBlock.Morning;
        
        dataStore.SyncData("daily_schedule", ref ignoredSchedule);
        dataStore.SyncData("current_time_block", ref ignoredTimeBlock);
        
        ModLogger.Info("Migration", "Removed obsolete schedule data");
    }
    
    // No new schedule data to save
    // Status is derived from traits/context (not saved)
}
```

### Event Migration

**Update Event Files:**
- Add role/context tags to all events
- Remove schedule triggers
- Update requirements (remove formation/duty checks)

**Priority Events:**
- Core events (20 highest-impact)
- Specialist events (medical, scout, officer)
- Generic soldier events (fallback pool)

### UI Migration

**Delete Entire Custom UI:**
```bash
# Remove all Camp Management UI files
rm -rf src/Features/Camp/UI/Management/
rm -rf GUI/Prefabs/Camp/

# Files deleted:
src/Features/Camp/UI/Management/
├── CampManagementVM.cs
├── CampManagementScreen.cs
└── Tabs/
    ├── CampLanceVM.cs
    ├── CampScheduleVM.cs
    ├── CampDutiesVM.cs
    ├── CampReportsVM.cs
    ├── CampArmyVM.cs
    ├── ActivityItemVM.cs
    └── ScheduleBlockItemVM.cs

GUI/Prefabs/Camp/
├── CampManagement.xml
├── CampLancePanel.xml
├── CampSchedulePanel.xml
├── CampDutiesPanel.xml
├── CampReportsPanel.xml
└── CampArmyPanel.xml
```

**Replace With:**
- Native Game Menu definitions (pure code, no UI files)
- Text-based menus (accordion-style navigation)
- Menu generators (dynamic text based on state)

### User Communication

**Changelog:**
```
## v2.1.0 - Macro Simplification & Native Menu System

### MAJOR CHANGES
- **Removed Schedule System**: No more daily 4-block schedule
- **Removed Camp Management Window**: Entire custom UI deleted
- **Native Game Menu**: All interfaces now use native text-based menus
- **New Role System**: Your role (Scout, Officer, Medic, etc.) derives from traits
- **Event Simplification**: Events fire naturally based on role + context

### NEW INTERFACE
The "Enlisted" option in your party screen now opens a simple text menu:
  > Camp (rest, train, socialize)
  > Reports (service record, reputation)
  > Decisions (active events/orders)
  > Status (role, reputation, traits)
  Talk to my lord...
  Leave Service

No more multi-tab management screens - just simple menus!

### WHAT THIS MEANS
- **No Daily Schedule**: Events happen organically, not on a schedule
- **Role-Based Content**: Develop traits to access specialist events
- **Less Micromanagement**: Focus on choices, not scheduling
- **Native Menus**: Familiar text-based interface (like settlements)
- **Faster Interface**: No custom UI loading, instant navigation

### MIGRATION
- Old schedule data discarded (save compatible)
- Old Camp Management shortcuts removed
- Events now route by role (from your traits)
- All features accessible via native menus
```

---

## Code Metrics

### Deletion Summary

**Schedule System**: -1700 lines
- ScheduleGenerator: -500
- ScheduleExecutor: -300
- ScheduleBehavior: -400
- Models: -300
- Event catalog: -200

**Camp Management (All Custom UI)**: -1800 lines
- CampManagementVM: -200 (main controller)
- CampManagementScreen: -150
- Lance tab VM: -300
- Schedule tab VM: -250
- Duties tab VM: -400
- Reports tab VM: -200 (replace with native menu)
- Army tab VM: -150 (replace with native menu)
- XML UI files: -150

**Total Deleted**: -3500 lines

### Addition Summary

**Status System**: +150 lines
- EnlistedStatusManager: +100 (simpler - no UI)
- Status text generation: +50

**Native Game Menus**: +300 lines
- Menu definitions: +200
- Menu text generators: +100

**Role-Based Events**: +200 lines
- RoleBasedEventTrigger: +150
- Event tagging logic: +50

**Total Added**: +650 lines

### Net Change

**-2850 lines** (81% reduction)

**More importantly:**
- **Zero custom UI** (all native Game Menu)
- **No ViewModels** (no MVVM overhead)
- **No XML UI files** (pure text menus)
- **Easier to maintain** (simple menu definitions)
- **Faster development** (no UI debugging)

---

## Native Game Menu Visual Example

### How It Actually Looks In-Game

```
╔════════════════════════════════════════════════════════╗
║                    ENLISTED                            ║
╠════════════════════════════════════════════════════════╣
║                                                        ║
║  You serve Lord Derthert as Man-at-Arms (T4)          ║
║                                                        ║
║  Current Campaign: Besieging Charax (Day 8)           ║
║                                                        ║
╠════════════════════════════════════════════════════════╣
║                                                        ║
║  > Camp                                                ║
║  > Reports                                             ║
║  > Decisions (2 available)                             ║
║  > Status                                              ║
║                                                        ║
║  Talk to my lord...                                    ║
║  Leave Service                                         ║
║                                                        ║
║  [Leave]                                               ║
║                                                        ║
╚════════════════════════════════════════════════════════╝
```

**User selects "Camp":**
```
╔════════════════════════════════════════════════════════╗
║                      CAMP                              ║
╠════════════════════════════════════════════════════════╣
║                                                        ║
║  What would you like to do?                            ║
║                                                        ║
╠════════════════════════════════════════════════════════╣
║                                                        ║
║  Rest & Recover                                        ║
║  Train Skills                                          ║
║  Socialize                                             ║
║                                                        ║
║  [Back]                                                ║
║                                                        ║
╚════════════════════════════════════════════════════════╝
```

**User selects "Status":**
```
╔════════════════════════════════════════════════════════╗
║                     STATUS                             ║
╠════════════════════════════════════════════════════════╣
║                                                        ║
║  Rank: Man-at-Arms (T4)                                ║
║  Lord: Derthert of Vlandia                             ║
║  Campaign: Besieging Charax (Day 8)                    ║
║                                                        ║
║  Your Role: Scout                                      ║
║    Scout 12, Valor +1, Scouting 75                     ║
║    Duties: Reconnaissance, intelligence gathering      ║
║                                                        ║
║  Reputation:                                           ║
║    Lord:     45/100 (Respected)                        ║
║    Officers: 35/100 (Promising)                        ║
║    Soldiers: 20/100 (Promising)                        ║
║                                                        ║
║  Personality: Honorable, Brave, Calculating            ║
║  Specializations: Scout, Sergeant                      ║
║                                                        ║
║  [Back]                                                ║
║                                                        ║
╚════════════════════════════════════════════════════════╝
```

**User selects "Decisions":**
```
╔════════════════════════════════════════════════════════╗
║                   DECISIONS                            ║
╠════════════════════════════════════════════════════════╣
║                                                        ║
║  Pending Events:                                       ║
║                                                        ║
╠════════════════════════════════════════════════════════╣
║                                                        ║
║  Scout Enemy Position                                  ║
║    Your expertise is needed for reconnaissance         ║
║                                                        ║
║  Medical Supplies Request                              ║
║    The surgeon needs help managing supplies            ║
║                                                        ║
║  [Back]                                                ║
║                                                        ║
╚════════════════════════════════════════════════════════╝
```

### Benefits of Native Game Menu

**Player Benefits:**
- **Familiar Interface**: Same as native settlements/encounters
- **Fast Navigation**: Text-based, keyboard-friendly
- **Clear Hierarchy**: Accordion-style organization
- **No Loading**: Instant menu transitions
- **Readable**: Large, clear text (not cramped UI)

**Developer Benefits:**
- **No Custom UI**: Zero ViewModels, no XML, no UI debugging
- **Easy to Modify**: Just change menu text/options
- **Quick Iteration**: No UI compilation/testing
- **Proven System**: Uses native infrastructure
- **Maintainable**: Simple menu definitions

**Modder Benefits:**
- **Easy to Extend**: Add new menu options via code
- **No UI Conflicts**: Text menus don't clash with UI mods
- **Simple Format**: Menu definitions are straightforward

---

## Orders System

### Overview

**Orders** are quick, reactive, rank-compliant, and trait-compliant tasks from the player's chain of command (Sergeant/Lieutenant/Captain/Lord). Unlike regular events (which are organic situations), orders are explicit directives with clear expectations and consequences for declining or failing.

### What Already Exists vs What's New

#### Already Exists (Leverage These Systems)

✅ **Company Needs Tracking** (`CompanyNeedsState`)
- Readiness, Equipment, Morale, Rest, Supplies (0-100)
- Daily degradation logic in `CompanyNeedsManager.ProcessDailyDegradation()`
- Activity recovery/degradation in `CompanyNeedsManager.ProcessActivityRecovery()`
- Critical threshold warnings
- **Note:** Renamed from `LanceNeedsState` to `CompanyNeedsState` for semantic clarity (Lance system deleted)

✅ **Escalation Tracking** (`EscalationState`)
- Scrutiny (0-10)
- Discipline (0-10)
- LanceReputation (-50 to +50) - being renamed to SoldierReputation
- MedicalRisk (0-5)

✅ **Event System**
- 80+ existing events
- JSON-based event definitions with requirements/triggers/effects
- Event catalog loading (`EventCatalogLoader`)
- Event effect application system

✅ **Skill & Trait XP**
- Native Bannerlord skill system (0-300 scale)
- Native trait system (0-20 scale for hidden, -2 to +2 for personality)
- Trait integration framework (from traits-identity-system.md)

✅ **Equipment System**
- Quartermaster access (`QuartermasterManager`)
- QM reputation affects pricing
- Supply gates equipment access (supplies <30% = blocked)
- Buyback system

#### What's NEW for Orders

✨ **Orders Management System** (Completely New)
- `OrderManager` behavior to track active orders
- Order selection logic (every ~3 days, context/rank/role-based)
- Accept/Decline/Execute flow with consequences
- Decline tracking (5+ refusals = discharge risk)
- Order urgency and expiration

✨ **Expanded Reputation System**
```csharp
public sealed class EscalationState
{
    // Existing (keep)
    public int Scrutiny { get; set; }
    public int Discipline { get; set; }
    public int MedicalRisk { get; set; }
    
    // RENAMED
    public int SoldierReputation { get; set; } // -50 to +50 (was LanceReputation)
    
    // NEW
    public int LordReputation { get; set; } // 0 to 100
    public int OfficerReputation { get; set; } // 0 to 100
}
```

✨ **Order-Driven Company Need Changes** (New Pattern)
- Player choices directly affect company needs
- Success/failure branches with different magnitudes
- Larger impact than passive schedule activities
- Strategic orders can have unit-wide consequences

✨ **Main Menu Order Display**
- Prominent order notification with urgency
- "Respond to Order" menu option
- Order details (from, objective, deadline)
- Visual distinction from regular events

✨ **Order Catalog**
- 30-40 rank-specific order definitions
- Tier-gated (T1-T2, T3-T4, T5-T6, T7-T8, T9)
- Trait-gated requirements (Scout trait for scout orders, etc.)
- Context-tagged (siege, war, peace, town)
- Role-tagged (scout, officer, medic, operative, nco, soldier)

### Implementation Scope

**New Code Required:**
- `OrderManager.cs` (~300 lines) - Order lifecycle management
- `OrderCatalog.cs` (~150 lines) - Order loading, filtering, selection
- Order JSON definitions (~30-40 orders × 50 lines = ~1500 lines)
- Main menu integration (~100 lines) - Order display
- **Total New:** ~2050 lines

**Modified Existing Code:**
- `EscalationState.cs` - Add LordReputation, OfficerReputation fields (~20 lines)
- Event effects applier - Support `company_needs` in order consequences (~50 lines)
- `EnlistedMenuBehavior.cs` - Add order display section (~100 lines)
- **Total Modified:** ~170 lines

**Deleted Code (from this doc + traits doc):**
- Schedule system: -1700 lines
- Duties system: -800 lines
- Lance system: -1200 lines
- **Total Deleted:** -3700 lines

**Net Code Change:** +2220 new - 3700 deleted = **-1480 lines overall**

### Order Frequency

**Base Rate**: Every 3 days

**Context Modifiers:**
```
Peace/Town:        Every 4-5 days  (fewer orders)
Campaign/March:    Every 3 days    (baseline)
War/Skirmish:      Every 2 days    (more activity)
Siege/Battle:      Every 1-2 days  (high tempo)
```

**Rank Modifiers:**
```
T1-T3 (Enlisted):  Every 2-3 days  (frequent group tasks)
T4-T6 (NCO):       Every 3-4 days  (independent missions)
T7-T9 (Officers):  Every 4-5 days  (strategic planning)
```

**Rationale**: 
- Not overwhelming (1-3 orders per week)
- Context feels reactive (more orders during siege)
- Rank progression feels authentic (officers get strategic missions, not daily busywork)
- Leaves room for random events (orders + events = ~2-3 interactions per day total)

### Order Consequences

#### Declining an Order

**Immediate Effects:**
- Discipline +2 (refusing orders)
- Officer Reputation -10 to -20 (disappointing superiors)
- Soldier Reputation -5 (seen as unreliable)

**Repeated Declines** (3+ in short period):
- Scrutiny +1 (marked as problem soldier)
- Lord Reputation -10 (word reaches lord)
- Possible discharge event at 5+ declines

**Mitigating Factors:**
- High Lord/Officer reputation = less penalty
- Valid excuse (low health, recent injury) = reduced penalty
- Honorable trait = larger penalty (breaking code)
- Calculating trait = smaller penalty (pragmatic choice)

#### Failing an Order (Accepted but Failed)

**Immediate Effects:**
- Officer Reputation -5 to -15 (depending on severity)
- Soldier Reputation -5 (if others witnessed)
- Potential Discipline +1 (if gross negligence)

**Mission-Critical Failures:**
- Scrutiny +1 (compromised operation)
- Lord Reputation -5 to -10
- Possible court-martial event

**Partial Success:**
- Reduced penalties (50%)
- "Tried your best" outcome

**Mitigating Factors:**
- High skill/trait in relevant area = less penalty
- Bad luck vs. bad judgment distinction
- Valor +1 trait can convert failure into "brave attempt"

#### Success Rewards

**Standard Rewards:**
- Officer Reputation +5 to +15
- Soldier Reputation +3 to +5 (if witnessed)
- Skill XP (relevant skills, 25-100 XP)
- Trait XP (relevant traits, 50-150 XP)

**Exceptional Success:**
- Lord Reputation +5 to +10
- Discipline -1 (proven reliable)
- Scrutiny -1 (covering up past issues)
- Bonus denars (50-200)
- Equipment unlock (special gear access)

**Critical Success** (rare):
- Promotion consideration (flag for next tier event)
- Commendation (special recognition)
- Lord conversation (personal praise)

### Order Types by Rank

#### T1-T2: Group Orders (Learning the Ropes)

**Characteristics:**
- Always with group (never alone)
- Led by NCO/Officer
- Simple, clear objectives
- Can't really "fail" (you're supervised)

**Example Order:**
```json
{
  "order_id": "t1_group_patrol",
  "title": "Patrol Duty",
  "from": "Sergeant",
  "description": "Join the morning patrol. Follow orders, stay alert, don't wander off.",
  "requirements": {
    "tier_min": 1,
    "tier_max": 2
  },
  "consequences": {
    "decline": {
      "discipline": 2,
      "officer_reputation": -10,
      "text": "The sergeant marks you down as unreliable."
    },
    "success": {
      "officer_reputation": 5,
      "soldier_reputation": 3,
      "athletics_xp": 25,
      "text": "A solid day's work. The sergeant nods approvingly."
    }
  }
}
```

#### T3-T4: Solo Capable (Trusted Hands)

**Characteristics:**
- Can work alone or with small group
- Require basic skills
- More responsibility
- Real failure consequences

**Example Order:**
```json
{
  "order_id": "t4_scout_position",
  "title": "Reconnaissance",
  "from": "Captain",
  "description": "Scout the ridge to the east. Report enemy strength and position.",
  "requirements": {
    "tier_min": 4,
    "tier_max": 6,
    "scouting_min": 60,
    "scout_trait_min": 5
  },
  "consequences": {
    "decline": {
      "discipline": 2,
      "officer_reputation": -20,
      "text": "The captain's jaw tightens. 'Find someone else who can handle it.'"
    },
    "success": {
      "officer_reputation": 15,
      "lord_reputation": 5,
      "scouting_xp": 80,
      "tactics_xp": 40,
      "scout_trait_xp": 100,
      "text": "Excellent work. Your report helps the captain plan the assault."
    },
    "failure": {
      "officer_reputation": -15,
      "scrutiny": 2,
      "text": "You were spotted. Had to retreat without intel. The captain is displeased."
    }
  }
}
```

#### T5-T6: Specialist/Team Leader (NCO Authority)

**Characteristics:**
- Lead small teams (3-5 soldiers)
- Specialist work (medical, engineering, etc.)
- High skill requirements
- Reputation matters

**Example Order:**
```json
{
  "order_id": "t6_field_surgery",
  "title": "Emergency Surgery",
  "from": "Chief Surgeon",
  "description": "Soldier with gut wound. Need you to assist—or lead if I'm unavailable.",
  "requirements": {
    "tier_min": 5,
    "tier_max": 7,
    "medicine_min": 100,
    "surgeon_trait_min": 12
  },
  "consequences": {
    "decline": {
      "discipline": 1,
      "officer_reputation": -15,
      "text": "'Understood. I'll find someone else.' (The soldier may not make it.)"
    },
    "success": {
      "officer_reputation": 20,
      "soldier_reputation": 15,
      "lord_reputation": 5,
      "medicine_xp": 150,
      "surgeon_trait_xp": 200,
      "text": "The man lives. Your skill saved him. You're a gifted surgeon."
    },
    "failure": {
      "officer_reputation": -10,
      "soldier_reputation": -10,
      "text": "He didn't make it. You did your best, but it wasn't enough."
    }
  }
}
```

#### T7-T8: Tactical Command (Junior Officer)

**Characteristics:**
- Command 10-20 soldiers
- Tactical decisions
- Coordinate with other units
- Lord may observe

**Example Order:**
```json
{
  "order_id": "t7_flank_assault",
  "title": "Tactical: Flank Assault",
  "from": "Lord Derthert",
  "description": "Take your men and hit their left flank when you hear the charge. Timing is critical.",
  "requirements": {
    "tier_min": 7,
    "tier_max": 8,
    "leadership_min": 120,
    "tactics_min": 100,
    "commander_trait_min": 10
  },
  "consequences": {
    "decline": {
      "lord_reputation": -30,
      "officer_reputation": -25,
      "text": "Your lord looks you in the eye. 'I see.' (You've lost his confidence.)"
    },
    "success": {
      "lord_reputation": 20,
      "officer_reputation": 15,
      "leadership_xp": 200,
      "tactics_xp": 150,
      "commander_trait_xp": 250,
      "denars": 200,
      "text": "Perfectly executed. The enemy broke. Your lord claps you on the shoulder."
    },
    "failure": {
      "lord_reputation": -20,
      "officer_reputation": -10,
      "scrutiny": 1,
      "text": "Your timing was off. The flank failed. Men died. Your lord says nothing."
    }
  }
}
```

#### T9: Strategic Operations (Senior Officer)

**Characteristics:**
- Lead your retinue (personal troops)
- Strategic missions
- Operate independently
- Directly serve lord

**Example Order:**
```json
{
  "order_id": "t9_intelligence_operation",
  "title": "Strategic: Deep Reconnaissance",
  "from": "Lord Derthert",
  "description": "Take your retinue behind enemy lines. I need their supply routes and troop movements.",
  "requirements": {
    "tier_min": 9,
    "scouting_min": 150,
    "tactics_min": 140,
    "leadership_min": 160,
    "scout_trait_min": 15,
    "commander_trait_min": 15
  },
  "consequences": {
    "decline": {
      "lord_reputation": -50,
      "text": "Your lord's expression hardens. 'Perhaps I've placed too much trust in you.'"
    },
    "success": {
      "lord_reputation": 35,
      "officer_reputation": 25,
      "scouting_xp": 300,
      "tactics_xp": 250,
      "leadership_xp": 200,
      "scout_trait_xp": 400,
      "commander_trait_xp": 350,
      "denars": 500,
      "renown": 10,
      "text": "Mission success. Your intelligence wins the campaign. Your lord rewards you publicly."
    },
    "failure": {
      "lord_reputation": -30,
      "scrutiny": 3,
      "text": "Ambushed. Lost men. Intelligence incomplete. A costly failure."
    }
  }
}
```

### Main Menu Display

Orders appear prominently on the `enlisted_status` main menu:

```
╔════════════════════════════════════════════════════════╗
║                    ENLISTED                            ║
╠════════════════════════════════════════════════════════╣
║                                                        ║
║  You serve Lord Derthert as Champion (T7)             ║
║  Current Campaign: Besieging Charax (Day 12)          ║
║                                                        ║
║  ━━━ ORDERS ━━━                                       ║
║  ⚠️  Lead Patrol (URGENT - respond within 2 days)     ║
║      "Take four men and secure the supply route."     ║
║      - From: Lieutenant Harrad                         ║
║                                                        ║
║  ━━━ REPORTS ━━━                                      ║
║  • Enemy sighted near eastern ridge (yesterday)       ║
║  • Supply caravan arrived (3 days ago)                ║
║                                                        ║
╠════════════════════════════════════════════════════════╣
║                                                        ║
║  > Camp                                                ║
║  > Reports                                             ║
║  > Decisions                                           ║
║  > Status                                              ║
║                                                        ║
║  Respond to Order (⚠️ available)                      ║
║  Talk to my lord...                                    ║
║  Leave Service                                         ║
║                                                        ║
╚════════════════════════════════════════════════════════╝
```

### Order Flow

1. **Order Arrives**: Fires every ~3 days based on rank/context
2. **Main Menu Display**: Shows order prominently with urgency indicator
3. **Player Interaction**: Selects "Respond to Order"
4. **Event Dialog**: Full order details with options:
   - [Accept] - Take the mission
   - [Decline] - Refuse (with reputation consequences)
   - [Ask for Details] - Learn more about requirements/risks
5. **Execution**: 
   - Simple orders: Resolve immediately with skill checks
   - Complex orders: May trigger sub-events or time passage
6. **Resolution**: Success/Failure determined by:
   - Skill levels (Scouting, Medicine, Leadership, etc.)
   - Trait levels (Scout, Surgeon, Commander, etc.)
   - Random factors (10-20% variance)
   - Player choices during execution
7. **Consequences Applied**: Reputation changes, XP gains, denars awarded

### Orders vs Regular Events

**Orders = From Chain of Command:**
- Always from superior (Sergeant/Lieutenant/Captain/Lord)
- Have explicit expectations
- Declining has **reputation consequences**
- Rank-compliant (appropriate to your tier)
- Trait-gated (need specialist skills for specialist orders)
- Displayed on main menu with urgency

**Regular Events = Organic Situations:**
- Happen to you (ambush, social encounter, discovery)
- No "decline" penalty (it's a situation, not an order)
- Role-based (your traits attract certain events)
- More narrative freedom
- Fire based on hourly/daily ticks + role + context

**Example Distinction:**

```
ORDER: "Scout the enemy camp" (from Lieutenant)
→ You can decline, but it damages reputation
→ Explicit success/failure conditions
→ Rewards for completion
→ Appears on main menu

EVENT: "You spot suspicious tracks while on patrol"
→ No penalty for ignoring
→ Opportunity, not obligation
→ Emergent from your role
→ Fires organically during gameplay
```

### Order Effects on Company Needs (NEW Integration)

Orders leverage the **existing** `CompanyNeedsState` system (renamed from `LanceNeedsState`) but with a **new pattern**: player-driven, branching consequences based on success/failure.

#### Order Impact by Type

**Patrol/Reconnaissance Orders:**
```json
{
  "order_id": "t4_scout_position",
  "consequences": {
    "success": {
      "company_needs": {
        "readiness": 5,      // Good intel improves unit readiness
        "morale": 3          // Successful mission boosts morale
      },
      "escalation": {
        "scrutiny": -1           // Reduced enemy awareness
      }
    },
    "failure": {
      "company_needs": {
        "readiness": -10,    // Bad intel hurts readiness
        "morale": -5         // Failed mission damages morale
      },
      "escalation": {
        "scrutiny": 2            // Compromised, enemy aware
      }
    }
  }
}
```

**Supply/Logistics Orders:**
```json
{
  "order_id": "t2_supply_run",
  "consequences": {
    "success": {
      "company_needs": {
        "supplies": 15,      // Direct supply benefit
        "morale": 5          // Well-fed troops are happy
      }
    },
    "failure": {
      "company_needs": {
        "supplies": -10,     // Lost supplies
        "morale": -10,       // Hungry troops are angry
        "equipment": -5      // Lost gear in chaos
      },
      "escalation": {
        "discipline": 1      // Logistics failure
      }
    }
  }
}
```

**Combat Leadership Orders (T5+):**
```json
{
  "order_id": "t5_lead_patrol",
  "consequences": {
    "success": {
      "company_needs": {
        "readiness": 8,
        "morale": 10,
        "rest": -5           // Patrols are tiring
      },
      "escalation": {
        "scrutiny": -1,
        "discipline": -1     // Tight operation
      }
    },
    "failure": {
      "company_needs": {
        "readiness": -15,    // Unit shaken
        "morale": -15,       // Lost confidence in you
        "equipment": -10,    // Man injured
        "rest": -10          // Exhausted from botched patrol
      },
      "escalation": {
        "scrutiny": 2,
        "discipline": 1      // Sloppy operation
      }
    }
  }
}
```

**Tactical Command Orders (T7-T8):**
```json
{
  "order_id": "t7_flank_assault",
  "consequences": {
    "success": {
      "company_needs": {
        "readiness": 15,     // Tactical victory boosts confidence
        "morale": 20,        // Decisive win
        "equipment": -10,    // Combat wear
        "rest": -15,         // Assault is exhausting
        "supplies": -10      // Combat consumption
      },
      "escalation": {
        "scrutiny": -2           // Enemy demoralized
      }
    },
    "failure": {
      "company_needs": {
        "readiness": -25,    // Unit shaken
        "morale": -30,       // Men died, poor tactics
        "equipment": -20,    // Heavy losses
        "rest": -20,         // Exhausted
        "supplies": -15      // Heavy consumption, no gain
      },
      "escalation": {
        "scrutiny": 1,
        "discipline": 1      // Questioning command
      }
    }
  }
}
```

#### Impact Magnitude by Rank

**Small Orders (T1-T3):** ±3 to ±8 need points
- Learning phase, not mission-critical yet
- Failure hurts but isn't catastrophic

**Medium Orders (T4-T6):** ±5 to ±15 need points
- NCO responsibility, actions matter
- Supply runs can save/doom the unit

**Large Orders (T7-T9):** ±10 to ±30 need points
- Command significant forces
- Tactical failures are devastating
- Strategic successes change campaigns

#### Order Impact Matrix

| Order Type | Success | Failure | Scrutiny | Discipline |
|-----------|---------|---------|------|------------|
| **Patrol/Scout** | +Readiness +Morale | -Readiness -Morale | Success:-1 Fail:+2 | - |
| **Supply Run** | +Supplies +Morale | -Supplies -Morale -Equipment | Fail:+1 | Fail:+1 |
| **Equipment Maint** | +Equipment +Readiness | -Equipment -Supplies | - | - |
| **Combat Patrol** | +Readiness +Morale -Rest | -Readiness -Morale -Equipment -Rest | Success:-1 Fail:+2 | Success:-1 Fail:+1 |
| **Medical** | +Morale +Readiness -Supplies | -Morale -Readiness -Supplies | - | Fail:+1 |
| **Tactical Assault** | +Readiness +Morale (large costs) | -ALL (major losses) | Success:-2 Fail:+1 | Fail:+1 |
| **Strategic Op** | +Readiness +Morale (minor costs) | -ALL (catastrophic) | Success:-3 Fail:+3 | Success:-2 |

### Order Selection Logic

```csharp
// File: src/Features/Orders/OrderSelectionManager.cs

public Order SelectOrder()
{
    var tier = EnlistmentBehavior.Instance.EnlistmentTier;
    var context = GetCampaignContext(); // "Siege", "War", "Peace", etc.
    var primaryRole = DetermineRole(); // "Scout", "Medic", "Officer", etc.
    
    // Get orders matching tier range
    var eligibleOrders = OrderCatalog.GetOrders()
        .Where(o => tier >= o.Requirements.TierMin && tier <= o.Requirements.TierMax)
        .ToList();
    
    // Filter by skill/trait requirements
    eligibleOrders = eligibleOrders
        .Where(o => MeetsSkillRequirements(o) && MeetsTraitRequirements(o))
        .ToList();
    
    // Prefer orders matching role + context
    var contextOrders = eligibleOrders
        .Where(o => o.Tags.Contains(context.ToLower()))
        .ToList();
    
    var roleOrders = contextOrders
        .Where(o => o.Tags.Contains(primaryRole.ToLower()))
        .ToList();
    
    // Priority: Role + Context > Context > Role > Any Eligible
    if (roleOrders.Count > 0) return roleOrders.GetRandomElement();
    if (contextOrders.Count > 0) return contextOrders.GetRandomElement();
    if (eligibleOrders.Count > 0) return eligibleOrders.GetRandomElement();
    
    return null; // No eligible orders
}
```

### Skill & Trait XP Integration

**Orders award both Skill XP (0-300 scale) and Trait XP (0-20 scale):**

```json
{
  "consequences": {
    "success": {
      "skill_xp": {
        "scouting": 80,
        "tactics": 40
      },
      "trait_xp": {
        "scout": 100,
        "commander": 50
      },
      "reputation": {
        "officer": 15,
        "lord": 5
      },
      "company_needs": {
        "readiness": 5,
        "morale": 3
      },
      "escalation": {
        "scrutiny": -1
      },
      "denars": 100
    }
  }
}
```

**This creates a progression loop:**
1. Do scout orders → Gain Scouting skill XP + Scout trait XP
2. Higher skill + trait → Unlock better scout orders
3. Better orders → More XP → Elite specialist
4. Elite specialist → Strategic-level orders from lord

### Implementing Company Need Effects

**Modify the existing event effect applier to support `company_needs` in order consequences:**

```csharp
// File: src/Features/Orders/OrderEffectApplier.cs

public void ApplyOrderEffects(OrderConsequences consequences)
{
    // Existing: Apply skill XP, trait XP, reputation, denars
    ApplySkillXP(consequences.SkillXP);
    ApplyTraitXP(consequences.TraitXP);
    ApplyReputation(consequences.Reputation);
    ApplyDenars(consequences.Denars);
    
    // NEW: Apply company need changes (uses CompanyNeedsState system)
    if (consequences.CompanyNeeds != null)
    {
        // Access CompanyNeedsState via ScheduleBehavior property
        var needs = ScheduleBehavior.Instance?.CompanyNeeds;
        if (needs == null)
        {
            ModLogger.Warn("Orders", "Cannot apply company need effects: CompanyNeeds is null");
            return;
        }
        
        foreach (var need in consequences.CompanyNeeds)
        {
            // Use CompanyNeed enum (renamed from LanceNeed for clarity)
            if (Enum.TryParse<CompanyNeed>(need.Key, true, out var needType))
            {
                int oldValue = needs.GetNeedValue(needType);
                needs.SetNeed(needType, oldValue + need.Value);
                
                ModLogger.Info("Orders", 
                    $"Order effect: {needType} {oldValue} -> {needs.GetNeedValue(needType)} ({need.Value:+#;-#;0})");
            }
        }
        
        // Check for critical thresholds using static CompanyNeedsManager
        var warnings = CompanyNeedsManager.CheckCriticalNeeds(needs);
        foreach (var warning in warnings)
        {
            InformationManager.DisplayMessage(new InformationMessage(
                warning.Value,
                warning.Key == CompanyNeed.Supplies || warning.Key == CompanyNeed.Morale 
                    ? Colors.Red 
                    : Colors.Yellow));
        }
    }
    
    // NEW: Apply escalation changes
    if (consequences.Escalation != null)
    {
        var escalation = EscalationManager.Instance;
        if (escalation != null)
        {
            if (consequences.Escalation.ContainsKey("scrutiny"))
            {
                escalation.ModifyScrutiny(consequences.Escalation["scrutiny"]);
            }
            if (consequences.Escalation.ContainsKey("discipline"))
            {
                escalation.ModifyDiscipline(consequences.Escalation["discipline"]);
            }
        }
    }
}

// Note: CompanyNeedsManager.CheckCriticalNeeds() is a static method in
// src/Features/Company/CompanyNeedsManager.cs (renamed from LanceNeedsManager)
```

**JSON Schema for Order Consequences:**

```json
{
  "consequences": {
    "success": {
      "skill_xp": { "scouting": 80, "tactics": 40 },
      "trait_xp": { "scout": 100, "commander": 50 },
      "reputation": { "officer": 15, "lord": 5, "soldier": 3 },
      "company_needs": { "readiness": 5, "morale": 3, "rest": -5 },
      "escalation": { "scrutiny": -1, "discipline": -1 },
      "denars": 100,
      "renown": 5
    },
    "failure": {
      "reputation": { "officer": -15, "soldier": -5 },
      "company_needs": { "readiness": -10, "morale": -5 },
      "escalation": { "scrutiny": 2, "discipline": 1 }
    },
    "decline": {
      "reputation": { "officer": -20 },
      "escalation": { "discipline": 2 }
    }
  }
}
```

---

## Conclusion

This simplification aligns perfectly with the trait-based identity system:

1. **Traits determine role** (Scout, Officer, Medic, etc.)
2. **Role + context determine events** (Scout during siege → reconnaissance events)
3. **Orders provide structure** (clear directives from chain of command)
4. **Skills + Traits gate progression** (need both practical experience and specialization)
5. **No micromanagement** (no daily schedule to review)
6. **Emergent gameplay** (role emerges from trait choices)
7. **Massive simplification** (-2850 lines, 81% reduction)
8. **Zero custom UI** (pure native Game Menu)

**Result**: Simpler, faster, more focused on choices and consequences rather than scheduling and management.

The player experience becomes:
- Build traits through choices
- Your role emerges naturally
- Events match your role and context
- Orders provide clear missions from superiors
- Skills + Traits unlock better orders
- Pure narrative focus
- Simple text-based interface (native Game Menu)

**This is the cleanest possible implementation**: No custom UI, no schedule micromanagement, no complex systems. Just traits, roles, skills, orders, events, and choices.

