# Camp Hub System - Phased Implementation Guide

**Immersive Spatial Camp Navigation**

**Status**: Phase 1 Complete ✅ | Phase 2 In Design  
**Vision**: Transform camp into a spatial hub with themed location screens  
**End Goal**: Full War Room Command Center with location-based navigation

---

## Table of Contents

1. [Vision Overview](#vision-overview)
2. [Architecture: Camp Hub System](#architecture-camp-hub-system)
3. [Phase 1: Camp Activities Foundation](#phase-1-camp-activities-foundation-complete)
4. [Phase 2: Camp Hub & Location System](#phase-2-camp-hub--location-system-next)
5. [Phase 3: Themed Area Screens](#phase-3-themed-area-screens)
6. [Phase 4: War Room Command Center](#phase-4-war-room-command-center)
7. [Technical Foundation](#technical-foundation)
8. [Testing Strategy](#testing-strategy)

---

## Vision Overview

### The New Camp Hub Concept

Instead of a flat activity list, the camp becomes a **spatial hub** where you navigate to different physical locations:

```
Game Menu → "Camp" → Camp Hub Screen
                      ↓
         ┌───────────────────────────────────────┐
         │     SELECT CAMP LOCATION:             │
         │                                       │
         │  [Medical Tent]  [Training Grounds]   │
         │  [Lord's Tent]   [Quartermaster]      │
         │  [Personal Quarters]  [Camp Fire]     │
         └───────────────────────────────────────┘
                      ↓
              [Click "Medical Tent"]
                      ↓
         Medical Tent Detail Screen
         (Shows activities available here)
```

### Why This Is Better

1. **Immersion**: Feels like navigating a real camp
2. **Organization**: Activities grouped by physical location
3. **Context**: Understand where you are and what's happening there
4. **Themed Experiences**: Each location can have unique visuals/backgrounds
5. **Scalability**: Easy to add new locations or activities
6. **Discovery**: Players explore the camp naturally

### Camp Locations (6 Core Areas)

#### **Medical Tent** 🏥
- Rest and Eat
- Help the Surgeon
- Recover from injuries
- Treatment for illness/wounds

#### **Training Grounds** ⚔️
- Weapon training
- Formation drills
- Stand Watch
- Sparring practice

#### **Lord's Tent** 🎪
- Talk to Lance Leader
- Strategic meetings
- Mission briefings
- Receive orders

#### **Quartermaster** 📦
- Maintain Equipment
- Forage for Camp
- Manage supplies
- Pack animals care
- Equipment logistics

#### **Personal Quarters** 🛏️
- Write a Letter
- Personal time
- Study/read
- Rest and sleep
- Organize gear

#### **Camp Fire** 🔥
- Fire Circle
- Drink with the Lads
- Storytelling
- Share Rations
- Social meals
- Dice games

---

## Architecture: Camp Hub System

### Three-Layer Structure

```
Layer 1: Game Menu
   └─ "Visit Camp" option
      ↓
Layer 2: Camp Hub Screen (NEW!)
   └─ 8 location buttons (2x4 grid)
   └─ Each shows: Icon, Name, Available Activities Count
      ↓
Layer 3: Area Detail Screen (Existing CampActivitiesScreen, filtered)
   └─ Shows activities for selected location
   └─ Themed background per location
   └─ Back button returns to hub
```

### Data Flow

```csharp
// Activities are tagged by location in JSON
{
  "id": "rest_and_eat",
  "category": "social",
  "location": "medical_tent",  // NEW!
  "title": "Rest and Eat",
  // ... rest of definition
}

// Hub screen queries by location
var medicalActivities = allActivities
    .Where(a => a.Location == "medical_tent")
    .ToList();

// Area screen receives filtered list
CampAreaScreen.Open("medical_tent", onClosed);
```

### File Structure

```
src/Features/Camp/UI/
├── Hub/
│   ├── CampHubScreen.cs              (NEW - Main hub with 8 locations)
│   ├── CampHubVM.cs                  (NEW - Hub ViewModel)
│   └── LocationButtonVM.cs           (NEW - Individual location button)
├── Areas/
│   ├── CampAreaScreen.cs             (REFACTOR - Was CampActivitiesScreen)
│   ├── CampAreaVM.cs                 (REFACTOR - Was CampActivitiesVM)
│   └── ActivityCardVM.cs             (EXISTING - No changes needed)
└── Common/
    └── ActivityCardRowVM.cs          (EXISTING - For grid layout)

GUI/Prefabs/Camp/
├── Hub/
│   ├── CampHubScreen.xml             (NEW - Hub layout)
│   └── LocationButton.xml            (NEW - Location card component)
├── Areas/
│   ├── CampAreaScreen.xml            (EXISTING - Current screen)
│   └── CampActivityCard.xml          (EXISTING - Activity card)
└── Backgrounds/
    ├── medical_tent_bg.xml           (FUTURE - Themed backgrounds)
    ├── training_grounds_bg.xml
    └── ... (one per location)

ModuleData/Enlisted/Activities/
└── activities.json                   (UPDATE - Add "location" field)
```

---

## System Alignment: Integration with Existing Gameplay

### Relationship to AI Camp Schedule

**CRITICAL DISTINCTION:**

The Camp Hub provides access to **player-initiated activities** during **free time**, while the AI Camp Schedule assigns **mandatory duties** based on Lord's orders.

```
┌─────────────────────────────────────────────────────┐
│           AI CAMP SCHEDULE (Mandatory)              │
├─────────────────────────────────────────────────────┤
│ Lord → Lance Leader → Player Duty Assignment        │
│                                                     │
│ Example Day Schedule:                               │
│  • Dawn (5-7):     Morning Muster                  │
│  • Morning (7-12): Sentry Duty - North Gate        │
│  • Afternoon (12-17): FREE TIME ← Camp Hub         │
│  • Evening (17-20): Patrol - Eastern Road          │
│  • Dusk (20-22):   FREE TIME ← Camp Hub            │
│  • Night (22-5):   Watch Rotation (2nd Watch)      │
└─────────────────────────────────────────────────────┘
```

### Menu State: Duty vs Free Time

**Game Menu "Visit Camp" Option:**

```csharp
// In CampMenuHandler.cs
private bool IsOnDuty()
{
    var schedule = AIScheduleBehavior.Instance;
    if (schedule == null) return false; // AI Schedule not implemented yet
    
    return schedule.HasAssignedDuty(CampaignTime.Now);
}

private void AddCampMenuOption(MenuCallbackArgs args)
{
    bool onDuty = IsOnDuty();
    
    args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
    
    if (onDuty)
    {
        // GREYED OUT - Player has assigned duty
        args.MenuContext.AddGameMenuOption(
            "enlisted_camp",
            "camp_visit_on_duty",  // Special greyed-out option
            "{=camp_on_duty}Visit Camp (On Duty)",
            null,
            null,
            false,  // isLeave = false
            -1,     // index
            false   // isRepeatable
        );
        
        // Tooltip explains why disabled
        MBTextManager.SetTextVariable("ON_DUTY_REASON", 
            GetCurrentDutyDescription());
    }
    else
    {
        // AVAILABLE - Player has free time
        args.MenuContext.AddGameMenuOption(
            "enlisted_camp",
            "camp_visit",
            "{=camp_visit}Visit Camp",
            args2 => CanVisitCamp(),
            args2 => OnVisitCamp(),
            true,   // isLeave = false
            -1      // index
        );
    }
}

private string GetCurrentDutyDescription()
{
    var schedule = AIScheduleBehavior.Instance;
    var currentDuty = schedule?.GetCurrentDuty();
    
    if (currentDuty == null) 
        return "You have assigned duties right now.";
    
    return $"You're assigned to {currentDuty.Name}. Report at {currentDuty.Location}.";
}
```

**Visual Feedback:**

```
┌───────────────────────────────────────────────┐
│  ENLISTED CAMP MENU                           │
├───────────────────────────────────────────────┤
│                                               │
│  [Check Schedule]                             │
│  [Talk to Lance Leader]                       │
│  [Visit Camp] ← GREYED OUT when on duty      │
│    └─ Tooltip: "Assigned to Sentry Duty"     │
│  [Rest in Tent]                               │
│  [Leave]                                      │
│                                               │
└───────────────────────────────────────────────┘

When FREE TIME:
│  [Visit Camp] ← ACTIVE, can click            │
│    └─ Opens Camp Hub Screen                   │
```

**How They Work Together:**

1. **AI Schedule Blocks Access:**
   - When player has assigned duty → "Visit Camp" greyed out
   - Menu tooltip shows: "You're assigned to [Duty Name]"
   - Player must complete duty or wait for free time
   
2. **Free Time Opens Hub:**
   - During unassigned time blocks → "Visit Camp" active
   - Clicking opens Camp Hub with 6 locations
   - Player can freely explore and do activities
   
3. **Duty Notifications:**
   - At duty start time → Notification: "Report to [Location] for [Duty]"
   - If player in Camp Hub when duty starts → Auto-closes hub, returns to game menu
   - Warning 15 minutes before duty: "Your [Duty] assignment begins soon"
   
4. **Fatigue Shared:**
   - Both systems use same Fatigue resource (0-30+)
   - If player does high-fatigue activity → affects duty performance
   - If player exhausted from duties → fewer camp activities available
   
5. **Location Awareness:**
   - Hub shows which lance members are where (from AI Schedule)
   - Example: "Training Grounds (3 members present)" because they're on drill duty

### Time-of-Day Integration

**6-Period System (Already Implemented):**

| Period | Hours | Camp Hub Activities | AI Schedule Duties |
|--------|-------|---------------------|-------------------|
| Dawn | 5-7 | Morning muster activities | Mandatory roll call |
| Morning | 7-12 | Training, medical visits | Assigned work details |
| Afternoon | 12-17 | FREE TIME (peak availability) | Often unassigned |
| Evening | 17-20 | Meals, social activities | Evening duties |
| Dusk | 20-22 | Camp fire, social time | Often unassigned |
| Night | 22-5 | Watch duty, sleep | Guard rotations |

**Activity Availability:**
- Activities have `"time_of_day": ["morning", "afternoon"]` field (existing)
- Camp Hub filters by current time period
- Example: "Camp Fire" activities only available during Dusk/Night

### Lance Life Simulation Integration

**Location as Social Space:**

Camp locations are where lance members **actually are** (tracked by AI Schedule):

```csharp
// Camp Hub can show:
"Medical Tent (2 members present)"  
  → Shows: Wilhelm (recovering), Hans (helping surgeon)

"Camp Fire (5 members present)"
  → Shows: Active social event, opportunity to join

"Training Grounds (Empty)"
  → Most are on other duties right now
```

**Cover Request Integration:**
- Lance member asks: "Can you cover my sentry duty?"
- If player accepts → Activity added to their AI Schedule
- If player refuses → Handled via Lance Life event system

**Location-Based Events:**
- Lance Life events can specify location: `"location": "camp_fire"`
- When player visits Camp Fire via Hub → triggers appropriate social events
- Injury events → Medical Tent becomes relevant location

### Escalation Systems Integration

**Status Bar Shows Real-Time State:**

All 6 escalation systems are displayed in both Hub and Area screens:

| System | Range | Affects Camp Hub How |
|--------|-------|---------------------|
| **Fatigue** | 0-30+ | Blocks high-cost activities |
| **Heat** | 0-10 | Unlocks/blocks corruption activities |
| **Discipline** | 0-10 | Some activities unavailable if high |
| **Pay Tension** | 0-100 | Desperate activities appear |
| **Lance Rep** | -50 to +50 | Social activities affected |
| **Medical Risk** | 0-5 | Medical Tent activities prioritized |

**Activity Effects:**
- Activities can modify any system (e.g., "Drink with Lads" → +Fatigue, +Lance Rep)
- Hub respects thresholds (e.g., can't train if Fatigue > 20)

### Camp Conditions Integration

**Quartermaster Mood Affects Hub:**

```
Camp Conditions (from Camp Life Simulation):
  • LogisticsStrain: 65/100 → "Supply issues"
  • MoraleShock: 40/100 → "Recent battle losses"
  • PayTension: 80/100 → "Pay is late!"

Camp Hub Response:
  • Quartermaster activities show "Sour Mood" warning
  • "Forage for Camp" appears as urgent option
  • Pay-related events more frequent
```

### Location Mapping to Systems

**How 6 Locations Align:**

| Location | AI Duties | Lance Members | Events | Camp Conditions |
|----------|-----------|---------------|--------|----------------|
| **Medical Tent** | Aid tent shift | Injured/sick | Injury notifications | Medical Risk |
| **Training Grounds** | Drills, sparring | On training duty | Training accidents | Readiness |
| **Lord's Tent** | Reports, briefings | Lance leader | Orders, promotions | Territory Pressure |
| **Quartermaster** | Supply details | On supply duty | Shortages, requisitions | Logistics Strain |
| **Personal Quarters** | Rest blocks | Resting soldiers | Letters, study | Fatigue recovery |
| **Camp Fire** | Off-duty social | Free time | Stories, gambling | Morale Shock |

### Event Delivery Channels

**Camp Hub Triggers Events Via:**

1. **"inquiry"** (popup dialog): 
   - Triggered when entering location with active event
   - Example: Click Camp Fire → "Join the story circle?" popup

2. **"menu"** (in-area screen):
   - Activity cards show as options in Area Detail Screen
   - Example: Medical Tent shows "Help the Surgeon" card

3. **"incident"** (notification):
   - Background events notify player to visit location
   - Example: "Wilhelm is injured. Visit Medical Tent."

### Data Model Alignment

**Activity Definition (Updated):**

```json
{
  "id": "fire_circle",
  "category": "social",
  "location": "camp_fire",           // NEW: Maps to 6 hub locations
  "time_of_day": ["evening", "dusk"], // EXISTING: Uses 6-period system
  "fatigue_cost": 0,                  // EXISTING: Shared fatigue resource
  "cooldown_days": 2,                 // EXISTING: Prevents spam
  "minTier": 1,                       // EXISTING: Rank requirements
  "effects": {
    "lance_reputation": 2,            // EXISTING: Escalation systems
    "fatigue": 0
  },
  "delivery": {
    "channel": "menu"                 // EXISTING: Event delivery
  }
}
```

### Key Design Decisions

✅ **Camp Hub is Player-Initiated** - Does not replace AI Schedule  
✅ **Respects Time-of-Day** - Uses existing 6-period system  
✅ **Shared Fatigue Resource** - Both duties and activities use same pool  
✅ **Location = Physical Space** - Where lance members actually are  
✅ **Event Integration** - Uses existing delivery channels  
✅ **Escalation Aware** - All activities affect/respect existing systems

### What Camp Hub Does NOT Do

❌ **Does NOT assign duties** - That's AI Camp Schedule's job  
❌ **Does NOT replace AI Schedule** - Complementary system  
❌ **Does NOT bypass escalation** - All rules still apply  
❌ **Does NOT create new time system** - Uses existing 6 periods  
❌ **Does NOT invent new stats** - Uses existing systems

---

## Phase 1: Camp Activities Foundation (COMPLETE ✅)

### What Was Built

✅ **Activity Card Component**
- Modern, clean card design with colored accent bars
- Category badges, rewards/costs display
- Availability status with color coding
- Responsive 3-column grid layout

✅ **Camp Activities Screen**
- Full-screen modal with header, content area, status bar
- Scrollable grid of activity cards (3 per row)
- Real-time status bar (Fatigue, Heat, Lance Rep, Pay Owed)
- Smooth opening/closing, ESC key support

✅ **Data Integration**
- Loads activities from JSON catalog
- Calculates availability based on fatigue, time, conditions
- Progress bar bindings for visual meters
- Row-based layout for responsive grid

✅ **Current Files**
```
src/Features/Camp/UI/
├── ActivityCardVM.cs              ✅ Complete
├── ActivityCardRowVM.cs           ✅ Complete
├── CampActivitiesVM.cs            ✅ Complete
└── CampActivitiesScreen.cs        ✅ Complete

GUI/Prefabs/Camp/
├── CampActivitiesScreen.xml       ✅ Complete
└── CampActivityCard.xml           ✅ Complete
```

### What's Working

- Opens from camp menu via "Camp Activities" option
- Displays all activities in clean grid
- Shows real-time player status
- Cards are color-coded by category
- Availability logic works correctly
- 4K-ready with larger fonts and proper spacing

### Known Issues

- Cards could be smaller (currently 420×240px)
- No location grouping yet
- No themed backgrounds
- Shows all activities at once (can be overwhelming)

---

## Phase 2: Camp Hub & Location System (NEXT)

**Goal**: Transform flat activity list into spatial camp hub with location navigation.

**Estimated Effort**: 4-6 days  
**Complexity**: Medium (refactoring existing + new hub screen)  
**Value**: Very High (major UX improvement, sets foundation for Phase 3-4)

### What It Looks Like

```
╔═══════════════════════════════════════════════════════════════╗
║  🏕️ CAMP OVERVIEW                        🕐 Evening, 18:45   ║
╠═══════════════════════════════════════════════════════════════╣
║                                                               ║
║  Where do you want to go?                                    ║
║                                                               ║
║  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      ║
║  │  🏥 MEDICAL  │  │ ⚔️ TRAINING  │  │  🎪 LORD'S   │      ║
║  │     TENT     │  │   GROUNDS    │  │     TENT     │      ║
║  │              │  │              │  │              │      ║
║  │ 3 Activities │  │ 5 Activities │  │ 2 Activities │      ║
║  │ [2 Available]│  │ [2 Available]│  │ [1 Available]│      ║
║  └──────────────┘  └──────────────┘  └──────────────┘      ║
║                                                               ║
║  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      ║
║  │📦 QUARTER-   │  │ 🛏️ PERSONAL  │  │  🔥 CAMP     │      ║
║  │   MASTER     │  │   QUARTERS   │  │     FIRE     │      ║
║  │              │  │              │  │              │      ║
║  │ 4 Activities │  │ 3 Activities │  │ 4 Activities │      ║
║  │ [3 Available]│  │ [2 Available]│  │ [3 Available]│      ║
║  └──────────────┘  └──────────────┘  └──────────────┘      ║
║                                                               ║
║  ┌─Status──────────────────────────────────────────┐        ║
║  │ Fatigue: 15/24 │ Heat: 0/10 │ Lance Rep: +3    │        ║
║  └────────────────────────────────────────────────┘        ║
╚═══════════════════════════════════════════════════════════════╝
```

### Features

✅ **Location Buttons (Smaller Cards)**
- 6 location cards in clean 2×3 grid
- Each card: 380×220px (larger than 8-location version)
- Icon, name, total activities, available count
- Color-coded by location type
- Hover effect + click to open area

✅ **Quick Status Bar**
- Same status bar as Phase 1
- Always visible for context

✅ **Refactored Area Screen**
- Existing `CampActivitiesScreen` becomes `CampAreaScreen`
- Accepts location filter parameter
- Shows header with location name + back button
- Only displays activities for that location
- Can apply themed background

✅ **Data Model Update**
- Add `"location"` field to activities JSON
- Activities catalog loader reads location field
- Hub queries activities grouped by location

### Implementation Steps

#### Step 1: Update Data Model (Day 1)

**File**: `ModuleData/Enlisted/Activities/activities.json` (MODIFY)

```json
{
  "id": "rest_and_eat",
  "category": "social",
  "location": "medical_tent",  // ADD THIS
  "title": "Rest and Eat",
  "hint": "Take time to eat a proper meal and rest your legs",
  // ... rest unchanged
}
```

**File**: `src/Features/Activities/CampActivityDefinition.cs` (MODIFY)

```csharp
public class CampActivityDefinition
{
    public string Id { get; set; }
    public string Category { get; set; }
    public string Location { get; set; }  // ADD THIS
    // ... rest unchanged
}
```

**Migration Strategy:**
```csharp
// In loader, default to "general" if location missing
activity.Location = jsonActivity.Location ?? "general";
```

#### Step 2: Create Hub Screen Components (Day 2-3)

**File**: `src/Features/Camp/UI/Hub/LocationButtonVM.cs` (NEW)

```csharp
using TaleWorlds.Library;

namespace Enlisted.Features.Camp.UI.Hub
{
    public class LocationButtonVM : ViewModel
    {
        private readonly string _locationId;
        private readonly Action<string> _onSelected;
        
        private string _locationName;
        private string _locationIcon;
        private string _locationColor;
        private int _totalActivities;
        private int _availableActivities;
        private string _availabilityText;
        
        public LocationButtonVM(string locationId, int total, int available, Action<string> onSelected)
        {
            _locationId = locationId;
            _onSelected = onSelected;
            
            LocationName = GetLocationName(locationId);
            LocationIcon = GetLocationIcon(locationId);
            LocationColor = GetLocationColor(locationId);
            TotalActivities = total;
            AvailableActivities = available;
            AvailabilityText = $"{available} Available";
        }
        
        [DataSourceProperty]
        public string LocationName
        {
            get => _locationName;
            set
            {
                if (_locationName != value)
                {
                    _locationName = value;
                    OnPropertyChangedWithValue(value, nameof(LocationName));
                }
            }
        }
        
        [DataSourceProperty]
        public string LocationIcon
        {
            get => _locationIcon;
            set
            {
                if (_locationIcon != value)
                {
                    _locationIcon = value;
                    OnPropertyChangedWithValue(value, nameof(LocationIcon));
                }
            }
        }
        
        [DataSourceProperty]
        public string LocationColor
        {
            get => _locationColor;
            set
            {
                if (_locationColor != value)
                {
                    _locationColor = value;
                    OnPropertyChangedWithValue(value, nameof(LocationColor));
                }
            }
        }
        
        [DataSourceProperty]
        public int TotalActivities
        {
            get => _totalActivities;
            set
            {
                if (_totalActivities != value)
                {
                    _totalActivities = value;
                    OnPropertyChangedWithValue(value, nameof(TotalActivities));
                }
            }
        }
        
        [DataSourceProperty]
        public int AvailableActivities
        {
            get => _availableActivities;
            set
            {
                if (_availableActivities != value)
                {
                    _availableActivities = value;
                    OnPropertyChangedWithValue(value, nameof(AvailableActivities));
                }
            }
        }
        
        [DataSourceProperty]
        public string AvailabilityText
        {
            get => _availabilityText;
            set
            {
                if (_availabilityText != value)
                {
                    _availabilityText = value;
                    OnPropertyChangedWithValue(value, nameof(AvailabilityText));
                }
            }
        }
        
        public void ExecuteSelect()
        {
            _onSelected?.Invoke(_locationId);
        }
        
        private string GetLocationName(string id)
        {
            return id switch
            {
                "medical_tent" => "Medical Tent",
                "training_grounds" => "Training Grounds",
                "lords_tent" => "Lord's Tent",
                "quartermaster" => "Quartermaster",
                "personal_quarters" => "Personal Quarters",
                "camp_fire" => "Camp Fire",
                _ => "Unknown"
            };
        }
        
        private string GetLocationIcon(string id)
        {
            return id switch
            {
                "medical_tent" => "🏥",
                "training_grounds" => "⚔️",
                "lords_tent" => "🎪",
                "quartermaster" => "📦",
                "personal_quarters" => "🛏️",
                "camp_fire" => "🔥",
                _ => "📍"
            };
        }
        
        private string GetLocationColor(string id)
        {
            return id switch
            {
                "medical_tent" => "#DD0000FF",      // Red
                "training_grounds" => "#FFAA33FF",   // Orange
                "lords_tent" => "#4444AAFF",         // Blue
                "quartermaster" => "#44AA44FF",      // Green
                "personal_quarters" => "#AA44AAFF",  // Purple
                "camp_fire" => "#FF6622FF",          // Bright Orange
                _ => "#FFFFFFFF"                     // White
            };
        }
    }
}
```

**File**: `src/Features/Camp/UI/Hub/CampHubVM.cs` (NEW)

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Enlisted.Features.Activities;
using Enlisted.Features.Camp.UI.Areas;
using Enlisted.Features.Enlistment.Behaviors;
using TaleWorlds.Library;

namespace Enlisted.Features.Camp.UI.Hub
{
    public class CampHubVM : ViewModel
    {
        private readonly Action _onClose;
        private MBBindingList<LocationButtonVM> _locations;
        private string _headerTitle;
        
        // Status bar (reuse from Phase 1)
        private int _fatigue;
        private string _fatigueText;
        private int _heat;
        private string _heatText;
        private int _lanceRep;
        private string _lanceRepText;
        
        public CampHubVM(Action onClose)
        {
            _onClose = onClose;
            Locations = new MBBindingList<LocationButtonVM>();
            HeaderTitle = "🏕️ CAMP OVERVIEW";
            
            RefreshLocations();
            RefreshStatusBar();
        }
        
        [DataSourceProperty]
        public MBBindingList<LocationButtonVM> Locations
        {
            get => _locations;
            set
            {
                if (_locations != value)
                {
                    _locations = value;
                    OnPropertyChangedWithValue(value, nameof(Locations));
                }
            }
        }
        
        [DataSourceProperty]
        public string HeaderTitle
        {
            get => _headerTitle;
            set
            {
                if (_headerTitle != value)
                {
                    _headerTitle = value;
                    OnPropertyChangedWithValue(value, nameof(HeaderTitle));
                }
            }
        }
        
        // Status bar properties (same as Phase 1)
        [DataSourceProperty]
        public int Fatigue { get => _fatigue; set { /* ... */ } }
        
        [DataSourceProperty]
        public string FatigueText { get => _fatigueText; set { /* ... */ } }
        
        // ... (other status properties)
        
        public void ExecuteClose()
        {
            _onClose?.Invoke();
        }
        
        private void RefreshLocations()
        {
            Locations.Clear();
            
            var allActivities = CampActivitiesBehavior.Instance?.GetAllActivities() ?? new List<CampActivityDefinition>();
            var locationIds = new[]
            {
                "medical_tent", "training_grounds", "lords_tent",
                "quartermaster", "personal_quarters", "camp_fire"
            };
            
            foreach (var locationId in locationIds)
            {
                var activitiesAtLocation = allActivities.Where(a => a.Location == locationId).ToList();
                var availableCount = activitiesAtLocation.Count(a => IsActivityAvailable(a));
                
                var button = new LocationButtonVM(
                    locationId,
                    activitiesAtLocation.Count,
                    availableCount,
                    OnLocationSelected
                );
                
                Locations.Add(button);
            }
        }
        
        private void OnLocationSelected(string locationId)
        {
            // Open area detail screen for this location
            CampAreaScreen.Open(locationId, () =>
            {
                // Return to hub when area screen closes
                RefreshLocations(); // Refresh counts in case activities were performed
            });
        }
        
        private bool IsActivityAvailable(CampActivityDefinition activity)
        {
            // Reuse availability logic from Phase 1
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null) return false;
            
            // Check fatigue cost
            if (enlistment.FatigueCurrent + activity.FatigueCost > enlistment.FatigueMax)
                return false;
            
            // Check time of day
            if (activity.DayParts != null && activity.DayParts.Any())
            {
                var currentDayPart = CampaignTriggerTrackerBehavior.GetDayPart();
                if (!activity.DayParts.Contains(currentDayPart))
                    return false;
            }
            
            // Check cooldown (simplified)
            // TODO: Full cooldown tracking
            
            return true;
        }
        
        private void RefreshStatusBar()
        {
            // Same as Phase 1 implementation
            var enlistment = EnlistmentBehavior.Instance;
            Fatigue = (int)(200.0f * (enlistment?.FatigueCurrent ?? 0) / Math.Max(1, enlistment?.FatigueMax ?? 24));
            FatigueText = $"{enlistment?.FatigueCurrent ?? 0} / {enlistment?.FatigueMax ?? 24}";
            // ... etc
        }
    }
}
```

**File**: `src/Features/Camp/UI/Hub/CampHubScreen.cs` (NEW)

```csharp
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Engine.Screens;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace Enlisted.Features.Camp.UI.Hub
{
    public class CampHubScreen : ScreenBase
    {
        private GauntletLayer _gauntletLayer;
        private CampHubVM _dataSource;
        private readonly Action _onClosed;
        
        public CampHubScreen(Action onClosed = null)
        {
            _onClosed = onClosed;
        }
        
        protected override void OnInitialize()
        {
            base.OnInitialize();
            
            _dataSource = new CampHubVM(CloseScreen);
            
            _gauntletLayer = new GauntletLayer(100, "GauntletLayer", false);
            _gauntletLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
            _gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
            
            _gauntletLayer.LoadMovie("CampHubScreen", _dataSource);
            
            AddLayer(_gauntletLayer);
            _gauntletLayer.IsFocusLayer = true;
            ScreenManager.TrySetFocus(_gauntletLayer);
            
            if (Campaign.Current != null)
            {
                Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
            }
        }
        
        protected override void OnFrameTick(float dt)
        {
            base.OnFrameTick(dt);
            
            if (_gauntletLayer?.Input.IsHotKeyReleased("Exit") == true)
            {
                CloseScreen();
            }
        }
        
        protected override void OnFinalize()
        {
            base.OnFinalize();
            
            if (Campaign.Current != null)
            {
                Campaign.Current.TimeControlMode = CampaignTimeControlMode.UnstoppablePlay;
            }
            
            RemoveLayer(_gauntletLayer);
            _gauntletLayer = null;
            _dataSource = null;
        }
        
        private void CloseScreen()
        {
            ScreenManager.PopScreen();
            _onClosed?.Invoke();
        }
        
        public static void Open(Action onClosed = null)
        {
            NextFrameDispatcher.RunNextFrame(() =>
            {
                if (Campaign.Current == null) return;
                var screen = new CampHubScreen(onClosed);
                ScreenManager.PushScreen(screen);
            });
        }
    }
}
```

#### Step 3: Create Hub XML Layout (Day 3)

**File**: `GUI/Prefabs/Camp/Hub/CampHubScreen.xml` (NEW)

```xml
<Prefab>
	<Constants>
		<Constant Name="Encyclopedia.Frame.Width" BrushLayer="Default" BrushName="Encyclopedia.Frame" BrushValueType="Width" />
		<Constant Name="Encyclopedia.Frame.Height" BrushLayer="Default" BrushName="Encyclopedia.Frame" BrushValueType="Height" />
	</Constants>
	<Window>
		<!-- Full screen semi-transparent overlay -->
		<Widget WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" Sprite="BlankWhiteSquare_9" Color="#00000099">
			<Children>

				<!-- Main panel (optimized for 6 locations) -->
				<BrushWidget Id="MainPanel" WidthSizePolicy="Fixed" HeightSizePolicy="Fixed" SuggestedWidth="1300" SuggestedHeight="820" HorizontalAlignment="Center" VerticalAlignment="Center" Brush="Encyclopedia.Frame">
					<Children>
						
						<!-- Header bar -->
						<Widget WidthSizePolicy="StretchToParent" HeightSizePolicy="Fixed" SuggestedHeight="90" VerticalAlignment="Top" Sprite="StdAssets\tabbar_popup">
							<Children>
								<RichTextWidget WidthSizePolicy="CoverChildren" HeightSizePolicy="CoverChildren" HorizontalAlignment="Center" VerticalAlignment="Center" MarginTop="8" Brush="Recruitment.Popup.Title.Text" Brush.FontSize="40" Brush.TextColor="#FFEECCFF" Text="@HeaderTitle" />
							</Children>
						</Widget>

						<!-- Location buttons grid (2x3 = 6 locations) -->
						<Widget WidthSizePolicy="StretchToParent" HeightSizePolicy="Fixed" SuggestedHeight="560" MarginTop="110" MarginLeft="50" MarginRight="50" VerticalAlignment="Top">
							<Children>
								<ListPanel DataSource="{Locations}" WidthSizePolicy="StretchToParent" HeightSizePolicy="CoverChildren" StackLayout.LayoutMethod="VerticalBottomToTop">
									<ItemTemplate>
										<!-- Location button row (3 per row) -->
										<Widget WidthSizePolicy="StretchToParent" HeightSizePolicy="Fixed" SuggestedHeight="250" MarginBottom="20">
											<Children>
												<ListPanel DataSource="{Cards}" WidthSizePolicy="CoverChildren" HeightSizePolicy="CoverChildren" HorizontalAlignment="Center" StackLayout.LayoutMethod="HorizontalLeftToRight">
													<ItemTemplate>
														<!-- Single location button (larger cards for 6 locations) -->
														<ButtonWidget Command.Click="ExecuteSelect" WidthSizePolicy="Fixed" HeightSizePolicy="Fixed" SuggestedWidth="360" SuggestedHeight="220" MarginRight="30" Brush="Encyclopedia.SubPage.Element" UpdateChildrenStates="true">
															<Children>
																<!-- Darker background -->
																<Widget WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" Sprite="BlankWhiteSquare_9" Color="#00000060" />
																
																<!-- Top colored bar -->
																<Widget WidthSizePolicy="StretchToParent" HeightSizePolicy="Fixed" SuggestedHeight="4" VerticalAlignment="Top" Color="@LocationColor" Sprite="BlankWhiteSquare_9" />
																
																<!-- Content -->
																<Widget WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" MarginLeft="20" MarginRight="20" MarginTop="20" MarginBottom="20">
																	<Children>
																		<!-- Icon (large, centered top) -->
																		<RichTextWidget WidthSizePolicy="CoverChildren" HeightSizePolicy="CoverChildren" HorizontalAlignment="Center" VerticalAlignment="Top" Brush="Recruitment.Popup.Title.Text" Brush.FontSize="48" Text="@LocationIcon" />
																		
																		<!-- Name -->
																		<RichTextWidget WidthSizePolicy="CoverChildren" HeightSizePolicy="CoverChildren" MarginTop="75" HorizontalAlignment="Center" VerticalAlignment="Top" Brush="Recruitment.Popup.Title.Text" Brush.FontSize="22" Brush.TextColor="#FFFFFFFF" Text="@LocationName" />
																		
																		<!-- Activity count -->
																		<RichTextWidget WidthSizePolicy="CoverChildren" HeightSizePolicy="CoverChildren" MarginTop="110" HorizontalAlignment="Center" VerticalAlignment="Top" Brush="SPGeneral.MediumText" Brush.FontSize="15" Brush.TextColor="#CCCCCCFF" Text="@TotalActivities Activities" />
																		
																		<!-- Available count (highlighted) -->
																		<Widget WidthSizePolicy="Fixed" HeightSizePolicy="Fixed" SuggestedWidth="180" SuggestedHeight="35" HorizontalAlignment="Center" VerticalAlignment="Bottom" Sprite="General\Notifications\notification_general" Color="@LocationColor">
																			<Children>
																				<RichTextWidget WidthSizePolicy="CoverChildren" HeightSizePolicy="CoverChildren" HorizontalAlignment="Center" VerticalAlignment="Center" Brush="SPGeneral.MediumText" Brush.FontSize="16" Brush.TextColor="#FFFFFFFF" Text="@AvailabilityText" />
																			</Children>
																		</Widget>
																	</Children>
																</Widget>
															</Children>
														</ButtonWidget>
													</ItemTemplate>
												</ListPanel>
											</Children>
										</Widget>
									</ItemTemplate>
								</ListPanel>
							</Children>
						</Widget>

						<!-- Bottom status bar (same as Phase 1) -->
						<Widget WidthSizePolicy="StretchToParent" HeightSizePolicy="Fixed" SuggestedHeight="70" VerticalAlignment="Bottom" MarginBottom="15" MarginLeft="40" MarginRight="40">
							<Children>
								<BrushWidget WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" Brush="Encyclopedia.SubPage.Element">
									<Children>
										<!-- Status bar content (reuse from Phase 1) -->
									</Children>
								</BrushWidget>
							</Children>
						</Widget>

						<!-- Close button -->
						<ButtonWidget Command.Click="ExecuteClose" HeightSizePolicy="Fixed" WidthSizePolicy="Fixed" SuggestedHeight="55" SuggestedWidth="55" VerticalAlignment="Center" HorizontalAlignment="Center" PositionYOffset="-355" PositionXOffset="615" Brush="Popup.CloseButton" />

					</Children>
				</BrushWidget>

			</Children>
		</Widget>
	</Window>
</Prefab>
```

#### Step 4: Refactor Area Screen (Day 4)

**File**: `src/Features/Camp/UI/Areas/CampAreaScreen.cs` (REFACTOR from CampActivitiesScreen)

```csharp
// Change to accept location filter
public class CampAreaScreen : ScreenBase
{
    private readonly string _locationId;
    private readonly Action _onClosed;
    // ...
    
    public CampAreaScreen(string locationId, Action onClosed = null)
    {
        _locationId = locationId;
        _onClosed = onClosed;
    }
    
    protected override void OnInitialize()
    {
        base.OnInitialize();
        
        // Pass location filter to ViewModel
        _dataSource = new CampAreaVM(_locationId, CloseScreen);
        
        // ... rest unchanged
    }
    
    public static void Open(string locationId, Action onClosed = null)
    {
        NextFrameDispatcher.RunNextFrame(() =>
        {
            if (Campaign.Current == null) return;
            var screen = new CampAreaScreen(locationId, onClosed);
            ScreenManager.PushScreen(screen);
        });
    }
}
```

**File**: `src/Features/Camp/UI/Areas/CampAreaVM.cs` (REFACTOR from CampActivitiesVM)

```csharp
public class CampAreaVM : ViewModel
{
    private readonly string _locationId;
    // ...
    
    public CampAreaVM(string locationId, Action onClose)
    {
        _locationId = locationId;
        _onClose = onClose;
        
        // Set header based on location
        HeaderTitle = GetLocationHeaderTitle(locationId);
        
        // ... rest of initialization
    }
    
    private void RefreshActivities()
    {
        Activities.Clear();
        
        var allActivities = _activitiesBehavior?.GetAllActivities() ?? new List<CampActivityDefinition>();
        
        // FILTER BY LOCATION
        var activitiesAtLocation = allActivities
            .Where(a => a.Location == _locationId)
            .ToList();
        
        // ... rest of loading logic (same as Phase 1)
    }
    
    private string GetLocationHeaderTitle(string locationId)
    {
        return locationId switch
        {
            "medical_tent" => "🏥 MEDICAL TENT",
            "training_grounds" => "⚔️ TRAINING GROUNDS",
            "lords_tent" => "🎪 LORD'S TENT",
            "quartermaster" => "📦 QUARTERMASTER",
            "personal_quarters" => "🛏️ PERSONAL QUARTERS",
            "camp_fire" => "🔥 CAMP FIRE",
            _ => "⚔ CAMP ACTIVITIES"
        };
    }
}
```

#### Step 5: Hook into Camp Menu (Day 5)

**File**: `src/Features/Camp/CampMenuHandler.cs` (MODIFY)

```csharp
// REPLACE old "Camp Activities" option with hub opener
public void AddCampHubOption(CampaignGameStarter campaignStarter)
{
    campaignStarter.AddGameMenuOption(
        menuId: "command_tent",
        optionId: "command_tent_camp_hub",
        optionText: "Visit Camp",  // Changed from "Camp Activities"
        condition: args =>
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
            return EnlistmentBehavior.Instance?.IsEnlisted ?? false;
        },
        consequence: args =>
        {
            CampHubScreen.Open(() =>
            {
                GameMenu.ActivateGameMenu("command_tent");
            });
        },
        isLeave: false,
        index: 1
    );
}
```

#### Step 6: Update Project Files (Day 5)

**File**: `Enlisted.csproj` (ADD NEW FILES)

```xml
<ItemGroup>
  <!-- Hub -->
  <Compile Include="src\Features\Camp\UI\Hub\CampHubScreen.cs"/>
  <Compile Include="src\Features\Camp\UI\Hub\CampHubVM.cs"/>
  <Compile Include="src\Features\Camp\UI\Hub\LocationButtonVM.cs"/>
  
  <!-- Areas (refactored) -->
  <Compile Include="src\Features\Camp\UI\Areas\CampAreaScreen.cs"/>
  <Compile Include="src\Features\Camp\UI\Areas\CampAreaVM.cs"/>
  <Compile Include="src\Features\Camp\UI\Areas\ActivityCardVM.cs"/>
  <Compile Include="src\Features\Camp\UI\Areas\ActivityCardRowVM.cs"/>
  
  <!-- XML -->
  <Content Include="GUI\Prefabs\Camp\Hub\CampHubScreen.xml"/>
  <Content Include="GUI\Prefabs\Camp\Areas\CampAreaScreen.xml"/>
  <Content Include="GUI\Prefabs\Camp\Areas\CampActivityCard.xml"/>
</ItemGroup>

<!-- After build tasks -->
<Target Name="AfterBuild">
  <MakeDir Directories="$(OutputPath)..\..\GUI\Prefabs\Camp\Hub\"/>
  <MakeDir Directories="$(OutputPath)..\..\GUI\Prefabs\Camp\Areas\"/>
  
  <Copy SourceFiles="GUI\Prefabs\Camp\Hub\CampHubScreen.xml" DestinationFolder="$(OutputPath)..\..\GUI\Prefabs\Camp\Hub\"/>
  <Copy SourceFiles="GUI\Prefabs\Camp\Areas\CampAreaScreen.xml" DestinationFolder="$(OutputPath)..\..\GUI\Prefabs\Camp\Areas\"/>
  <Copy SourceFiles="GUI\Prefabs\Camp\Areas\CampActivityCard.xml" DestinationFolder="$(OutputPath)..\..\GUI\Prefabs\Camp\Areas\"/>
</Target>
```

#### Step 7: Data Migration (Day 6)

**Update all activities in JSON with location field:**

```json
// Example migrations
{
  "id": "rest_and_eat",
  "location": "medical_tent",
  // ...
}

{
  "id": "fire_circle",
  "location": "camp_fire",
  // ...
}

{
  "id": "weapon_training_v1",
  "location": "training_grounds",
  // ...
}
```

**Mapping Guide (Aligned with Existing Events):**

From your existing event/story packs:

**Medical Tent:**
- `rest_and_eat` (existing activity)
- `medical.aid_tent_shift_v1` (from story pack)
- `medical.bandage_drill_v1` (from story pack)
- `help_surgeon` (activity)
- Any `medical_risk` threshold events

**Camp Fire:**
- `morale.campfire_song_v1` (from story pack) 
- `fire_circle` (existing activity)
- `drink_with_lads` (existing activity)
- `morale.after_battle_words_v1` (if in camp)
- Dice games, storytelling activities

**Training Grounds:**
- `training.lance_drill_night_sparring_v1` (from story pack)
- `weapon_training_*` (activities)
- `formation_drill` (activity)
- `stand_watch` (duty/activity hybrid)

**Lord's Tent:**
- `talk_to_leader` (lance leader conversations)
- Lance Life events with `channel: "menu"` at leader location
- Promotion notifications
- Order briefings

**Quartermaster:**
- `logistics.thin_wagons_v1` (from story pack)
- `maintain_equipment` (activity)
- `forage` (activity)
- Existing Quartermaster menu activities
- Supply shortage events

**Personal Quarters:**
- `write_letter` (existing activity)
- Study/skill development activities
- Rest/fatigue recovery
- Personal reflection events

**Note:** Many existing events don't have a `location` field yet - this is added in Phase 2 data migration.

### Success Criteria

✅ **Hub Screen**
- Opens from camp menu
- Shows 8 location buttons
- Displays activity counts
- Status bar works

✅ **Location Navigation**
- Clicking location opens filtered area screen
- Area screen shows only activities for that location
- Back button returns to hub
- Counts update after activities

✅ **Visual Design**
- Location buttons are clean and readable
- Colors are distinct per location
- Layout works on 1080p and 4K

✅ **Data Integration**
- All activities have location field
- Filtering by location works correctly
- No activities lost or duplicated

---

## Phase 2.5: Duty Cover Request System (FUTURE - BRAINSTORM)

**Goal**: Allow players to swap duties with lance mates to access Camp during assigned duty times.

**Status**: 🤔 Conceptual - For future brainstorming  
**Estimated Effort**: 2-3 weeks (requires AI Schedule + Lance Life integration)  
**Dependencies**: AI Camp Schedule (Phase from AI Schedule doc), Lance Life Simulation

### Concept: Swapping Duties with Lance Mates

**Problem:** Player has assigned duty but wants to visit camp.  
**Solution:** Request a lance mate to cover your duty in exchange for favors.

### Game Flow

```
┌─────────────────────────────────────────────────┐
│ 1. PLAYER ON DUTY (Current State)              │
├─────────────────────────────────────────────────┤
│ Morning: Sentry Duty - North Gate              │
│ Status: ASSIGNED (cannot visit camp)            │
└─────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────┐
│ 2. REQUEST COVER (New Action)                  │
├─────────────────────────────────────────────────┤
│ Menu Option: [Request Cover for Duty]          │
│                                                 │
│ Opens dialog with available lance mates:       │
│  • Wilhelm (Off Duty) - Relationship: +15      │
│  • Hans (Off Duty) - Relationship: -5          │
│  • Friedrich (On Different Duty) - Unavailable │
└─────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────┐
│ 3. NEGOTIATION EVENT                            │
├─────────────────────────────────────────────────┤
│ "{LANCE_MATE}, can you cover my sentry duty?"  │
│                                                 │
│ Options depend on relationship:                │
│  • [Ask as favor] (Relationship ≥ +10)         │
│    → Costs Lance Rep, owes you nothing         │
│  • [Offer to trade] (Any relationship)         │
│    → You cover their duty later (favor owed)   │
│  • [Bribe with gold] (Low relationship)        │
│    → Costs 50-100 gold                          │
│  • [Pull rank] (T5+ only)                      │
│    → Costs Discipline, damages relationship    │
└─────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────┐
│ 4. OUTCOME                                      │
├─────────────────────────────────────────────────┤
│ SUCCESS:                                        │
│  • Your duty assigned to lance mate             │
│  • You gain FREE TIME for this block           │
│  • "Visit Camp" becomes available               │
│  • Consequences tracked (favor owed, etc.)     │
│                                                 │
│ FAILURE:                                        │
│  • Lance mate refuses                           │
│  • Still on duty (menu greyed out)             │
│  • May damage relationship                      │
└─────────────────────────────────────────────────┘
```

### Lance Mate Requests (Reverse Flow)

**Lance mates can also ask player to cover:**

```
Event: "lance_cover_request_player" (already in Lance Life Simulation design)

Wilhelm approaches:
"Hey, remember when I covered your sentry duty? I need you to take 
 my patrol this afternoon."

Options:
 [Accept] - Clears favor, adds duty to player schedule
 [Refuse] - Damages relationship, keeps favor owed
 [Negotiate] - Try to delay or modify terms
```

### Integration with Existing Systems

**AI Camp Schedule:**
- `SwapDutyAssignment(player, lanceMate, dutyBlock)` method
- Updates both character schedules
- Notifies both of assignment change
- Validates swap is allowed (not critical duties)

**Lance Life Simulation:**
- Cover requests affect lance relationships (already designed)
- Refusals trigger resentment events
- Successful trades build trust
- This is **already in the Lance Life design** as "Cover Request System"

**Escalation Systems:**
- Bribing = +Heat
- Pulling rank = +Discipline
- Repeated requests without reciprocating = -Lance Rep

**Favor Tracking:**
```csharp
public class DutyCoverTracker
{
    private Dictionary<string, List<FavorOweRecord>> _favorsOwed;
    
    public class FavorOweRecord
    {
        public string DebtorId { get; set; }      // Who owes
        public string CreditorId { get; set; }    // Who is owed
        public string DutyType { get; set; }      // What duty
        public CampaignTime Date { get; set; }    // When incurred
    }
    
    public void AddFavor(string debtor, string creditor, string dutyType);
    public void CollectFavor(string creditor, string debtor);
}
```

### Balance Considerations

**Frequency Limits:**
- Max 1-2 cover requests per week
- Can't request cover for same duty type repeatedly
- Lance mates refuse if player never reciprocates

**Consequences:**
- Favors MUST be repaid or relationship tanks
- Using rank authority damages morale
- Bribing creates corruption paper trail

**Edge Cases:**
- Lance mate injured during covered duty → Player partially blamed
- Critical duties (Lord's direct orders) → Cover requests refused
- No available lance mates → Option greyed out

### Why This is Phase 2.5 (Not Immediate)

**Prerequisites:**
1. AI Camp Schedule must be fully implemented first
2. Lance Life Simulation needs to track member states
3. Cover Request events already designed in Lance Life doc
4. This bridges two major systems - do it carefully

**Brainstorm Later:**
- Implementation details once both systems are stable
- UI/UX for selecting lance mates
- Event text and negotiation options
- Testing how duty swaps affect gameplay balance

---

## Phase 3: Themed Area Screens

**Goal**: Add unique themed backgrounds and visuals for each camp location.

**Estimated Effort**: 1-2 weeks  
**Dependencies**: Phase 2 complete  
**Value**: High (immersion, visual polish)

### Features

✅ **Themed Backgrounds**
- Medical Tent: Reddish tint, medical supplies visible
- Training Grounds: Dusty/sandy brown, weapon racks
- Lord's Tent: Blue/regal, maps and banners
- Camp Fire: Warm orange glow, shadows
- Each area feels distinct

✅ **Dynamic Background Elements**
- NPC silhouettes in background
- Environmental particles (smoke from fire, dust from training)
- Time-of-day lighting changes

✅ **Area-Specific Status**
- Medical Tent shows wounded count
- Training Grounds shows active drills
- Camp Fire shows who's gathered

### Technical Approach

**Option A: Color Overlays (Simple)**
```xml
<!-- In CampAreaScreen.xml -->
<Widget WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" 
        Sprite="BlankWhiteSquare_9" 
        Color="@LocationBackgroundColor" />
```

**Option B: Custom Background Sprites (Complex)**
```xml
<!-- Load location-specific sprite -->
<Widget WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" 
        Sprite="@LocationBackgroundSprite" />
```

**Option C: Hybrid (Best)**
- Base background sprite for all
- Location-specific color overlay
- Optional decorative elements per location

---

## Phase 4: War Room Command Center

**Goal**: Full tactical hub with map view, multi-tabs, alerts system.

**Estimated Effort**: 2-3 weeks  
**Dependencies**: Phase 1-3 complete  
**Value**: Very High (signature feature)

### See Original Document

Full Phase 4 details remain unchanged from original implementation guide. This builds on top of the hub system by adding:

- Interactive camp map view
- Multi-tab interface (Map, Activities, Lance, Status, Alerts)
- Real-time population tracking
- Alert notifications system
- Integration with AI schedule

---

## Technical Foundation

### Key Patterns from Phase 1

#### 1. Color Format (8-Digit Hex)
```csharp
LocationColor = "#DD0000FF";  // ALWAYS 8 digits!
```

#### 2. NextFrame Dispatch
```csharp
NextFrameDispatcher.RunNextFrame(() => {
    ScreenManager.PushScreen(screen);
});
```

#### 3. ViewModel Properties
```csharp
[DataSourceProperty]
public string MyProperty
{
    get => _myProperty;
    set
    {
        if (_myProperty != value)
        {
            _myProperty = value;
            OnPropertyChangedWithValue(value, nameof(MyProperty));
        }
    }
}
```

### Data Model Updates

**CampActivityDefinition** now includes:
```csharp
public string Location { get; set; }  // NEW in Phase 2
```

**Default Locations:**
```csharp
public static class CampLocations
{
    public const string MedicalTent = "medical_tent";
    public const string TrainingGrounds = "training_grounds";
    public const string LordsTent = "lords_tent";
    public const string Quartermaster = "quartermaster";
    public const string PersonalQuarters = "personal_quarters";
    public const string CampFire = "camp_fire";
}
```

---

## Testing Strategy

### Phase 2 Testing

**Hub Screen Tests:**
1. [ ] Hub opens from camp menu
2. [ ] 8 location buttons display
3. [ ] Activity counts are correct
4. [ ] Available counts update in real-time
5. [ ] Clicking location opens area screen
6. [ ] Status bar displays correctly

**Area Screen Tests:**
1. [ ] Area screen shows correct location header
2. [ ] Only activities for that location display
3. [ ] Back button returns to hub (not game menu)
4. [ ] Hub refreshes after activity execution
5. [ ] All Phase 1 functionality still works

**Data Migration Tests:**
1. [ ] All activities have location field
2. [ ] No activities lost during migration
3. [ ] Activities appear in correct locations
4. [ ] Unlabeled activities default to "general"

---

## Development Timeline

### Updated Timeline

- **Phase 1 (Complete)**: 2 weeks ✅
- **Phase 2 (Hub System)**: +1 week (4-6 days)
- **Phase 3 (Themed Screens)**: +1-2 weeks
- **Phase 4 (War Room)**: +2-3 weeks

**Total MVP (Phases 1-2)**: 3 weeks  
**Total Great (Phases 1-3)**: 5-6 weeks  
**Total Amazing (All Phases)**: 8-10 weeks

---

## Next Steps (Phase 2)

1. ✅ **Design Review**: Approved camp hub concept
2. **Data Migration**: Add `location` field to all activities
3. **Create Hub Components**: `CampHubScreen`, `CampHubVM`, `LocationButtonVM`
4. **Create Hub XML**: `CampHubScreen.xml`
5. **Refactor Area Screen**: Add location filter parameter
6. **Update Menu Hook**: Change to open hub instead of direct activities
7. **Test Integration**: Verify hub → area → back flow
8. **Polish**: Hover effects, sounds, visual feedback

---

---

## Implementation Checklist: System Alignment

### ⚠️ Important: AI Schedule Not Yet Implemented

**Current State:**
- AI Camp Schedule is **designed** but not yet implemented in code
- For Phase 2, "Visit Camp" will be **always available** (no duty blocking)
- Activities limited only by: time-of-day, fatigue, cooldowns, rank

**When to Add Duty Blocking:**
- Wait until AI Camp Schedule is fully implemented
- Then add `IsOnDuty()` check to `CampMenuHandler.cs`
- Menu option will grey out when player has assigned duties
- This is a **clean integration point** - won't affect Phase 2 implementation

**Code Pattern (For Future):**
```csharp
// In CampMenuHandler.cs
private bool IsOnDuty()
{
    // TODO: Add when AI Schedule is implemented
    var schedule = AIScheduleBehavior.Instance;
    if (schedule == null) return false; // Not implemented yet
    
    return schedule.HasAssignedDuty(CampaignTime.Now);
}
```

---

Before implementing Phase 2, ensure alignment with existing systems:

### Data Layer
- [ ] Activities.json includes `"location"` field (6 values)
- [ ] Activities.json respects `"time_of_day"` field (6 periods: dawn, morning, afternoon, evening, dusk, night)
- [ ] Activities include `"fatigue_cost"` field (shared resource with AI Schedule)
- [ ] Activities can affect escalation systems (heat, discipline, lance_rep, medical_risk)
- [ ] Activities respect `"minTier"` and `"maxTier"` (rank requirements)
- [ ] Activities include `"cooldown_days"` to prevent spam

### Behavior Layer
- [ ] `CampHubVM` queries `AIScheduleBehavior` to check if player has assigned duties
- [ ] `CampHubVM` queries `LanceLifeSimulationBehavior` to show lance member locations
- [ ] `CampHubVM` respects `CampaignTriggerTrackerBehavior.GetDayPart()` for time filtering
- [ ] `CampAreaVM` checks `EnlistmentBehavior.FatigueCurrent` before showing high-cost activities
- [ ] `CampAreaVM` checks `EscalationManager` state for Heat/Discipline/LanceRep thresholds
- [ ] `CampAreaVM` checks `CampConditionsBehavior` for logistics/morale state

### Event Integration
- [ ] Activity execution triggers existing event system (not new event pipeline)
- [ ] Cover requests from Lance Life can add activities to player schedule
- [ ] Injury events make Medical Tent location relevant
- [ ] Pay Tension events can unlock desperate activities at certain locations

### UI Layer
- [ ] Status bar shows: Fatigue, Heat, Discipline, Lance Rep, Medical Risk, Pay Owed (all existing systems)
- [ ] Location buttons show lance member count (from AI Schedule tracking)
- [ ] Area screens show time-appropriate activities (from time-of-day system)
- [ ] Unavailability reasons match system constraints ("Too tired" = Fatigue > threshold, not arbitrary)

### Time System
- [ ] Hub respects 6-period day (Dawn, Morning, Afternoon, Evening, Dusk, Night)
- [ ] Activities filtered by current period match their `time_of_day` field
- [ ] Time display shows current period + hour (e.g., "Evening, 18:45")

### Fatigue Management
- [ ] Fatigue is **shared** between AI Schedule duties and Camp Activities
- [ ] High-fatigue activities (2-3) are blocked when Fatigue > 20
- [ ] Rest activities (negative fatigue) are available at Personal Quarters
- [ ] Fatigue recovery aligns with existing camp life simulation

### Location Logic
- [ ] Each location maps to specific duty types in AI Schedule
- [ ] Lance members "present" at location based on current duty assignment
- [ ] Location-specific events trigger when visiting via hub
- [ ] Quartermaster location integrates with existing Quartermaster menu system

### Backwards Compatibility
- [ ] Existing activities without `"location"` default to "general" or first available location
- [ ] Existing event delivery channels ("inquiry", "menu", "incident") continue to work
- [ ] Existing escalation threshold events not disrupted by hub system
- [ ] Existing AI Camp Schedule (if implemented) takes precedence over hub

---

**Ready to start Phase 2! Let's build the hub system! 🏕️**
