# camp ui: adapted settlement management pattern

## index

- [analysis: why settlement pattern works here](#analysis-why-settlement-pattern-works-here)
- [proposed hybrid design: \"settlement-style camp screen\"](#proposed-hybrid-design-settlement-style-camp-screen)

## analysis: why settlement pattern works here

### Native Settlement Screen Pattern
**Purpose:** Manage a single settlement with multiple aspects visible at once
- **User Role:** Lord/Administrator viewing owned property
- **Interaction:** Occasional decisions (assign governor, queue project)
- **Information Density:** Medium - distinct sections, not too many options
- **Flow:** Overview -> Select section -> Make decision -> Return to overview

### Our Camp Activities System
**Purpose:** Choose what to do during soldier's downtime
- **User Role:** Soldier/Enlisted member with limited authority
- **Interaction:** Frequent action selection (choose activity, execute, see results)
- **Information Density:** High - 23 activities across 6 locations
- **Flow:** Check status -> Pick location -> Choose activity -> Execute -> See result

## Proposed Hybrid Design: "Settlement-Style Camp Screen"

### Layout Structure (1600x900px)

```
┌──────────────────────────────────────────────────────────────────────────┐
│  CAMP OVERVIEW                        Evening, 18:00              [Done] │
├──────────────────────────────────────────────────────────────────────────┤
│   Reports   Medical   Training   Lord's   Quarter   Quarters   Fire │
│  [Active location highlighted with underline/glow]                      │
├────────────────┬────────────────────────────────────┬──────────────────┤
│                │                                    │                  │
│ SOLDIER STATUS │     ACTIVITIES (Selected Location) │   REPORTS FEED   │
│                │                                    │                  │
│  Fatigue     │  Available Activities (Grid):      │  Recent News  │
│ 12 / 24        │                                    │                  │
│ ▓▓▓▓▓░░░ (50%) │  [  ]  [  ]  [  ]  [  ]  │ WARNING Cover needed │
│                │     1        2        3        4    │  for watch duty │
│  Heat        │                                    │  (2 hours ago)  │
│ 3 / 10         │  [  ]  [  ]  [  ]  [  ]  │                  │
│ ▓▓▓░░░░░ (30%) │     5        6        7        8    │ [x] Training     │
│                │                                    │  completed      │
│ * Lance Rep   │  [Empty if no more activities]      │  (+2 Melee)     │
│ +5             │                                    │  (5 hours ago)  │
│                │  ┌─────────────────────────────┐  │                  │
│  Time        │  │ SELECTED ACTIVITY DETAILS  │  │  Duty alert:  │
│ Evening        │  │                             │  │  Report to      │
│ (18:00)        │  │  Visit Medical Tent      │  │  commander      │
│                │  │                             │  │  (6 hours ago)  │
│ Current Loc:   │  │ Check in with the medic     │  │                  │
│ Medical Tent   │  │ and tend to any wounds.     │  │ [Scroll for     │
│ (2 available)  │  │                             │  │  more news...]  │
│                │  │  Rewards: +1 Health      │  │                  │
│                │  │  Cost: 2 Fatigue         │  │                  │
│                │  │                             │  │                  │
│                │  │      [ EXECUTE ]        │  │                  │
│                │  │                             │  │                  │
│                │  └─────────────────────────────┘  │                  │
│                │                                    │                  │
└────────────────┴────────────────────────────────────┴──────────────────┘
```

## Key Design Decisions

### 1. **Location Navigation: Horizontal Bar (Not Tabs)**
**Why:** Like "Bound Villages" in settlement screen
- All locations visible at once
- Click to switch (no Q/E needed, though we can keep it)
- Current location highlighted
- Shows activity count per location ` Medical (2)`

**Pattern from Settlement:** Top row of icons for quick navigation
**Adaptation:** Locations instead of villages, but same visual language

### 2. **Activity Display: Circular Icon Grid**
**Why:** Like "Projects" grid in settlement screen
- Scan available activities at a glance
- Icons with numbers (not full cards)
- Click icon -> details appear below
- Unavailable activities shown dimmed/locked

**Pattern from Settlement:** Grid of circular project icons
**Adaptation:** Activities instead of buildings, but same interaction

### 3. **Details Panel: Expandable Bottom Section**
**Why:** Like "Current Project" detail view
- Only one activity detailed at a time
- Full description, costs, rewards
- Execute button prominently placed
- Collapses when no activity selected

**Pattern from Settlement:** Selected project shows full details
**Adaptation:** Selected activity shows full details + execute action

### 4. **Status Panel: Always Visible Left Side**
**Why:** Like "Governor/Stats" section
- Player needs constant awareness of fatigue/status
- No clicking to see stats
- Updates in real-time after actions

**Pattern from Settlement:** Stats in left column
**Adaptation:** Player stats instead of settlement stats

### 5. **Reports Feed: Right Panel (Persistent)**
**Why:** NEW addition inspired by "Daily Defaults"
- Shows recent events, alerts, cover requests
- Always visible (not hidden in tab)
- Scrollable feed of recent 10-20 items
- Updates when activities complete

**Pattern from Settlement:** Daily defaults section (but vertical)
**Adaptation:** News feed instead of defaults, more dynamic

## Player Flow Comparison

### Native Settlement (Administrative)
```
1. Enter settlement management
2. View all aspects simultaneously
3. Click section needing attention
4. Make decision (assign governor, queue building)
5. Done -> Exit
```

### Adapted Camp (Action Selection)
```
1. Enter camp screen
2. See: current status + available locations + recent news
3. Click location of interest (or Reports for news details)
4. Scan activity icons -> click one
5. Review details -> Execute
6. See result in news feed
7. Repeat or Done
```

## Advantages Over Tab Design

### Information Architecture
- [x] **No hidden information** - locations always visible
- [x] **Faster navigation** - one click vs tab+scroll
- [x] **Better spatial memory** - locations map to physical camp
- [x] **Persistent context** - player status always visible

### User Experience
- [x] **Familiar to Bannerlord players** - settlement pattern
- [x] **Less cognitive load** - no remembering what's in each tab
- [x] **Better for discovery** - can see activity counts per location
- [x] **News integration** - reports visible without switching tabs

### Implementation
- [x] **Same ViewModels** - `CampLocationTabVM` becomes sections
- [x] **Reuse activity cards** - just shown as icons initially
- [x] **Native patterns** - more widgets we can reference

## Disadvantages vs Tabs

### Screen Real Estate
- WARNING More complex layout management
- WARNING Activity cards can't be as large (icons first, then expand)
- WARNING May feel cramped on smaller resolutions

### Implementation Complexity
- WARNING More sections to coordinate
- WARNING Need icon versions of all activities
- WARNING Details panel collapse/expand logic

## Recommended Approach: "Evolved Settlement Pattern"

### Phase 1: Structure (Keep Current, Add News Panel)
```
Top:    Location buttons (horizontal bar)
Left:   Player status (always visible)
Center: Activity icons -> selected activity details
Right:  Reports feed (scrollable)
```

### Phase 2: Interactions
- Click location button -> center updates with that location's activities
- Activities shown as circular icons (like your screenshot's projects)
- Click activity icon -> details panel expands below
- Execute button in details panel
- Result appears in reports feed

### Phase 3: Polish
- Smooth transitions when switching locations
- Activity icon animations for available/unavailable
- News feed updates with subtle animation
- Location button highlights current selection

## Critical Insight: Two-Stage Interaction

**Settlement Screen:**
1. **Browse** (scan projects) -> 2. **Select** (see details) -> 3. **Confirm** (queue building)

**Camp Screen (Adapted):**
1. **Browse** (scan locations & counts) -> 2. **Focus** (click location, see activity icons) -> 3. **Select** (click activity) -> 4. **Execute** (perform action)

The extra step (Focus on location) is natural because camp has **more granular organization** (6 locations vs settlement's single space). But once focused, the select->execute flow matches the settlement pattern.

## Design Philosophy

**Settlement Screen:** "Here's everything you own, what needs attention?"
**Camp Screen:** "Here's where you are, what can you do right now?"

Both show complete state at once, but camp emphasizes **action selection** over **information review**.

## Recommendation

**Implement the hybrid:**
1. Keep your current ViewModels (they're well-structured)
2. Change XML layout to settlement-style three-panel design
3. Add activity icon display (circular, like projects)
4. Add details panel expansion logic
5. Integrate news feed as persistent right panel

**Result:** Familiar to Bannerlord players, optimized for soldier's action-selection flow, keeps all information visible.

**Files to Update:**
- `GUI/Prefabs/Camp/CampScreen.xml` - new layout structure
- `CampScreenVM.cs` - minimal changes (location switching already works)
- NEW: `ActivityIconVM.cs` - compact representation for grid display
- NEW: `CampNewsItemVM.cs` - already created, just needs XML layout

**Estimated Implementation:** 4-6 hours to adapt XML and add icon display logic.

Would you like me to proceed with implementing this adapted settlement-style design?

