# Menu Interface System

## Terminology (to avoid confusion)

This project uses **two UI technologies**, and they are easy to mix up:

- **Text menus (GameMenu / WaitGameMenu)**: classic Bannerlord menu screens with menu IDs like `enlisted_status`.
- **Gauntlet screens (full-screen UI overlays)**: custom UI layers like `CampManagementScreen`.

We use these names consistently in docs:

- **Main Menu (text)**: `enlisted_status`
- **Camp Menu (text)**: `enlisted_camp_hub` (opened from Main Menu → “Camp”)
- **Camp Bulletin (Gauntlet overlay)**: opened via a “Reports” option inside the Camp Menu
- **Camp Management Screen (Gauntlet)**: `CampManagementScreen` (deep configuration screen)

## Quick Reference

| Menu ID | Purpose | Access |
|---------|---------|--------|
| `enlisted_status` | Main service menu - status with escalation tracks (Heat/Discipline/Lance Rep), navigation | After enlistment |
| `enlisted_lance` | My Lance - roster with relationship indicators, interactions, welfare tracking | Enlisted Status -> "My Lance" |
| `enlisted_activities` | Camp Activities - organized by category (Training/Tasks/Social/Lance) | Enlisted Status -> "Camp" -> "Camp Activities" |
| `enlisted_duty_selection` | Duty Selection - request-based assignment (T2+) | Camp Management -> "Duties" (primary entry) |
| `enlisted_medical` | Medical Attention - treatment options | Enlisted Status -> "Seek Medical Attention" (when injured/ill) |
| `enlisted_camp_hub` | Camp Menu (text) - camp-facing navigation (medical, reports, records, camp activities entry points) | Enlisted Status -> "Camp" |
| Quartermaster | Equipment selection (formation+tier+culture based) | Enlisted Status -> "Visit Quartermaster" |

## Index

### Feature Documentation
- [Overview](#overview) - System purpose and key features
- [Modern UI Styling](#modern-ui-styling) - Icons, tooltips, backgrounds, audio
- [How It Works](#how-it-works) - Menu structure and navigation
  - [Main Enlisted Status Menu](#main-enlisted-status-menu)
  - [Duty Selection Interface](#duty-selection-interface)
  - [Menu Navigation](#menu-navigation)
  - [Popups & Incidents (not menus)](#popups--incidents-not-menus)
- [Technical Details](#technical-details) - Implementation specifics
  - [Menu Structure](#menu-structure)
  - [Dynamic Text System](#dynamic-text-system)
  - [Tier-Based Availability](#tier-based-availability)
- [Edge Cases](#edge-cases) - Problem scenarios and solutions
  - [Menu State Consistency](#menu-state-consistency)
  - [Tier Progression](#tier-progression)
  - [Duty Selection Persistence](#duty-selection-persistence)
  - [Menu Activation Timing](#menu-activation-timing)
  - [Time Control Preservation](#time-control-preservation)

### Bannerlord API Reference
- [Bannerlord Menu APIs](#bannerlord-menu-apis) - Native API patterns
  - [Menu Types](#menu-types) - MenuAndOptionType values
  - [Time Control Behavior](#time-control-behavior) - Vanilla behavior and preservation
  - [Menu Registration](#menu-registration) - Creating menus
  - [Background and Audio](#background-and-audio) - Visual/audio setup
  - [Text Variables](#text-variables) - Dynamic text system
  - [Menu Navigation](#menu-navigation-api) - Switching between menus
  - [Menu Options](#menu-options) - Adding options with icons
  - [LeaveType Icons](#leavetype-icons) - Icon reference table
  - [Popup Dialogs](#popup-dialogs) - Inquiry dialogs

### Development Reference
- [API Reference](#api-reference) - Enlisted-specific APIs
- [Debugging](#debugging) - Logging and troubleshooting

---

## Overview

Professional military menu interface providing comprehensive service management with organized duty selection, detailed descriptions, and tier-based progression. All menus feature modern styling with icons, tooltips, ambient audio, and culture-appropriate backgrounds.

**Key Features:**
- Modern icons on all menu options via `LeaveType`
- Hover tooltips explaining each option's function and bonuses
- Culture-appropriate background meshes
- Ambient camp audio for immersion
- Formation-aware duty display (incompatible duties greyed out with tooltip)
- Tier-based progression with helpful unlock messages
- Real-time status updates
- **Escalation Tracks** in status header: Heat, Discipline, Lance Reputation with visual bars and threshold warnings
- Camp status + time-of-day context in the enlisted header (days from town, camp snapshot)
- Camp Activities menu driven by JSON (XP + fatigue, with condition gating)
- **Lance Menu** with relationship indicators, leader/second interactions, wounded/fallen welfare tracking
- Main menu navigation entries are **always shown**; inaccessible entries are **greyed out with tooltips** explaining why.

**File:** `src/Features/Interface/Behaviors/EnlistedMenuBehavior.cs`

---

## Modern UI Styling

All menus use a consistent modern styling system for professional appearance.

### Menu Option Icons

Each menu option has a `LeaveType` that displays an appropriate icon:

| Option | LeaveType | Icon Purpose |
|--------|-----------|--------------|
| Visit Quartermaster | `Trade` | Equipment/trading |
| My Lord... | `Conversation` | Dialog |
| Visit Settlement | `Submenu` | Navigation |
| Duties | `Manage` | Management (duty request/assignment; primary entry lives in Camp Management) |
| Ask for Leave | `Leave` | Exit action |
| Desert the Army | `Escape` | Warning/danger (immediate abandonment) |
| (Discharge via Camp) | `Manage` | Managed separation (Pending Discharge -> Final Muster) |

### Tooltips

Every menu option displays a tooltip on hover explaining its function:
- Main menu options show brief descriptions
- Duty options explain skill bonuses and benefits
- Formation-incompatible duties show which formations are required
- Tier-locked duties show culture-specific rank requirements

Tooltip strings are localized in `ModuleData/Languages/enlisted_strings.xml`.

### Background and Audio

Menus use `[GameMenuInitializationHandler]` attributes to set:
- **Background Mesh**: Culture-appropriate encounter background from lord's kingdom
- **Ambient Sound**: `event:/map/ambient/node/settlements/2d/camp_army`
- **Panel Sound**: `event:/ui/panels/settlement_camp`

### Text Formatting

- Section headers use em-dashes: `— DUTIES —` instead of ASCII box characters
- Currency displays use inline gold icons: `{GOLD_ICON}`
- Bullet lists use simple markers: `[x]` for selected, `[ ]` for available

---

## How It Works

### Main Enlisted Status Menu

**Menu ID:** `enlisted_status` (WaitGameMenu)

**Options with Icons and Tooltips:**
| Option | Icon | Tooltip | Condition |
|--------|------|---------|-----------|
| Visit Quartermaster | Trade | Purchase equipment for your formation and rank | Always |
| My Lance | Manage | View your lance roster and relationships | Always |
| Camp | Manage | Open the Camp Menu (text): medical, reports, records, and entry points to camp activities/screens | While enlisted (greyed if not enlisted) |
| My Lord... | Conversation | Speak with nearby lords for quests and news | Lord nearby |
| Visit Settlement | Submenu | Enter the settlement while your lord is present | In settlement |
| Seek Medical Attention | Manage | Visit the surgeon's tent | When injured/ill |
| — LEAVE OPTIONS — | | | |
| Ask for Leave | Leave | Request temporary leave from service | Always |
| Leave Without Penalty | Leave | Pay is too late — leave with minimal consequences | PayTension ≥ 60 |
| Desert the Army | Escape | Abandon your post (severe penalties) | Always |

**Note:** “Camp Management” is the deep configuration surface (Gauntlet). “Camp” here means the **Camp Menu (text)** (`enlisted_camp_hub`).

**Features:**
- Modern icons on every option for visual clarity
- Tooltips on hover explaining each action
- Culture-appropriate background mesh from lord's kingdom
- Ambient camp audio for immersion
- Professional status display with real-time updates
- Clean navigation to sub-menus
- Status information (tier, XP, days served, etc.)
- Status header includes:
  - **Escalation Tracks:**
    - **Heat** with visual bar `▓▓▓░░░░░░░ 3/10 [Watched]` and threshold warnings
    - **Discipline** with visual bar and threshold warnings
    - **Lance Rep** with numeric value and status text `+15 (Trusted)`
  - Time-of-day (Dawn/Day/Dusk/Night)
  - Days since last town entry
  - Camp snapshot status line (Supplies/Morale/Pay) when Camp Life is active
  - **Pay Status** - Shows tension level when pay is late (Grumbling -> Tense -> Severe -> CRITICAL)
  - **Owed Backpay** - Shows accumulated unpaid wages with gold icon
- Pay is handled via a muster ledger and pay muster (see Pay System doc); discharge is managed from Camp.
- When PayTension ≥ 60, the "Leave Without Penalty" option appears (free desertion).

**Escalation Track Thresholds:**
| Track | Threshold | Warning Label | Effect |
|-------|-----------|---------------|--------|
| Heat | 3 | Watched | Minor suspicion |
| Heat | 5 | Shakedown | Kit inspections begin |
| Heat | 7 | Audit | Serious investigation |
| Heat | 10 | EXPOSED | Critical - may face discharge |
| Discipline | 3 | Extra Duty | Additional work assigned |
| Discipline | 5 | Hearing | Formal review |
| Discipline | 7 | Blocked | Promotion blocked |
| Discipline | 10 | DISCHARGE | Forced discharge imminent |

### Duty Selection Interface

**Menu ID:** `enlisted_duty_selection` (WaitGameMenu)

**Implementation:** Fully data-driven from `duties_system.json`. The menu dynamically generates up to 10 duty slots, showing all duties with formation-incompatible ones greyed out. Includes tier gating, expansion checks, and duty request flow.

**Duty Assignment Flow:**
- **T1 Players**: Auto-assigned "Runner" duty, cannot change duties
- **T2+ Players**: Must **request** duty changes through lance leader approval

**Duty Request System (T2+):**

| Menu Display | Meaning |
|--------------|---------|
| `[Request Transfer]` | Duty available for request |
| `[Cooldown: Xd]` | Request denied, must wait X days |
| `[Requires {Rank}]` | Tier requirement not met (shows culture-specific rank) |
| `[{formations} only]` | Greyed out, duty requires different formation |
| `(Current)` | Currently active duty |

Request approval requires:
- 14-day cooldown between requests
- Minimum 10 lance reputation
- Meeting the duty's tier requirement
- Duty compatible with player's formation

**Formation-Based Display:**
- All duties are shown in the menu regardless of player's formation
- Incompatible duties are greyed out with a tooltip showing required formations
- Example: A cavalry soldier sees "[infantry only]" on the Armorer duty

**Duty Selection (Available T1+; data-driven)**

Duties are defined in `ModuleData/Enlisted/duties_system.json`. The menu uses `EnlistedDutiesBehavior.GetAllDuties()` to show all duties, with `IsDutyCompatibleWithFormation()` to determine which are selectable. The canonical duty IDs (shipping) are:

| Duty | Min Tier | Notes |
|------|----------|------|
| Runner | 1 | Infantry (starter duty) |
| Quartermaster | 1 | Infantry |
| Field Medic | 1 | Infantry |
| Armorer | 1 | Infantry |
| Engineer | 2 | Infantry |
| Scout | 1 | Archer / Cavalry / Horse Archer |
| Lookout | 1 | Archer (starter duty) |
| Messenger | 1 | Cavalry / Horse Archer (starter duty) |
| Boatswain | 1 | Naval (War Sails only, starter duty) |
| Navigator | 2 | Naval (War Sails only) |

**Starter Duties (Auto-assigned at T2):**
| Formation | Starter Duty |
|-----------|--------------|
| Infantry | Runner |
| Archer | Lookout |
| Cavalry | Messenger |
| Horse Archer | Scout |
| Naval | Boatswain |

**Formation-Based Filtering:**
- Infantry: Runner, Quartermaster, Field Medic, Armorer, Engineer
- Archer: Scout, Lookout
- Cavalry: Scout, Messenger
- Horse Archer: Scout, Messenger
- Naval: Boatswain, Navigator (only when War Sails expansion detected)

**Tier Locking:**
- Duties with tier requirements above player's current tier show culture-specific rank (e.g., `[Requires Immunes]` for Empire)
- Locked duties are grayed out but visible (shows progression path)
- Tooltips explain unlock requirements

**Note:** The progression system supports 9 tiers with culture-specific rank names. See [Lance Assignments](../Core/lance-assignments.md) for the full tier/track breakdown (T1-4 Enlisted, T5-6 Officer, T7-9 Commander).

**Description System:**
- Top of menu shows detailed description for currently selected duty
- Rich military context explaining daily activities and skill training

**Checkmark System:**
- Dynamic markers (`[x]`/`[ ]`) showing current selection
- Updates in real-time when selection changes
- Visual feedback for active assignment

### Menu Navigation

**Flow:**
- Enlisted Status (`enlisted_status`)
  - Visit Quartermaster -> equipment selection (formation + tier + culture)
  - My Lance (`enlisted_lance`) -> roster / relationships
  - Camp (`enlisted_camp_hub`) -> Camp Menu (text)
    - Camp Activities (`enlisted_activities`) -> activity selection (data-driven)
    - Medical Tent (`enlisted_medical`) -> treatment options (when injured/ill)
    - Reports -> Camp Bulletin (Gauntlet overlay)
    - Camp Management (Gauntlet) -> deep configure/manage surface
      - Duties -> duty selection (request system; `enlisted_duty_selection` or camp management panel routing)
  - Seek Medical Attention (`enlisted_medical`) -> treatment options (when injured/ill)
  - My Lord... -> dialog system
  - Visit Settlement -> town/castle menu
  - Ask for Leave -> leave request dialog

### My Lance Menu

**Menu ID:** `enlisted_lance` (WaitGameMenu)

**Purpose:** View your position in your lance, see the roster with relationship indicators, interact with lance mates, and track welfare.

**Header Display:**
- Lance name with decorative border
- Your position and rank
- Days served with lance
- **Escalation Tracks:** Lance Reputation (with visual bar), Heat (if > 0), Discipline (if > 0)
- Condensed roster showing Leader and Second with relationship indicators

**Options:**
| Section | Option | Icon | Description |
|---------|--------|------|-------------|
| **Roster** | View Full Roster | TroopSelection | All 10 slots with relationship indicators `[+++]/[++]/[+]/[ ]/[-]/[--]` |
| **Interactions** | Talk to Lance Leader | Conversation | Speak with the leader, shows relationship status and flavor dialogue |
| | Talk to Second | Conversation | Speak with the second-in-command |
| **Welfare** | Check on the Wounded | Manage | Visit wounded lance mates (+1 Lance Rep) |
| | Honor the Fallen | Mission | Remember fallen comrades (+2 Lance Rep) |
| **Info** | View Lance History | Submenu | Battle record, reputation, achievements, strength |
| | Return to Camp | Leave | Return to Enlisted Status |

**Relationship Indicators:**
| Indicator | Meaning | Rep Range |
|-----------|---------|-----------|
| `[+++]` | Bonded | ≥ +40 |
| `[++]` | Trusted | +20 to +39 |
| `[+]` | Friendly | +5 to +19 |
| `[ ]` | Neutral | -5 to +4 |
| `[-]` | Wary | -20 to -6 |
| `[--]` | Hostile | ≤ -21 |

**Features:**
- Displays culture-specific rank title (e.g., "Miles" for Empire T2, "Levy" for Vlandia T2)
- Shows your slot position based on tier (higher tier = earlier slot)
- Days served with current lance
- Named persona members (if persona system enabled)
- Wounded/fallen tracking with welfare actions
- Leader relationship affects dialogue tone

**File:** `src/Features/Lances/Behaviors/EnlistedLanceMenuBehavior.cs`

### Medical Attention Menu

**Menu ID:** `enlisted_medical` (WaitGameMenu)

**Purpose:** Seek treatment when injured, ill, or exhausted. Only accessible when player has an active condition.

**Options:**
| Option | Icon | Description |
|--------|------|-------------|
| Request Treatment from Surgeon | Manage | Full treatment, costs 2 fatigue |
| Treat Yourself (Field Medic) | Manage | Self-treatment if you have Field Medic duty, grants Medicine XP |
| Purchase Herbal Remedy | Trade | Costs 50 gold |
| Rest in Camp | Wait | Light recovery option |
| View Detailed Status | Submenu | Full condition breakdown popup |
| Back | Leave | Return to Enlisted Status |

**File:** `src/Features/Conditions/EnlistedMedicalMenuBehavior.cs`

### Camp Activities Menu

**Menu ID:** `enlisted_activities` (WaitGameMenu)

**Purpose:** Organized menu for player-initiated activities. Earn XP, manage fatigue, and build relationships through structured camp actions. Activities are organized by category with section headers.

**Access:** Camp -> "Camp Activities"

**Menu Header (RP-flavored):**
> The camp stirs with activity. Soldiers drill, fires crackle, and the smell of cooking fills the air. What will you do with your time?

**Categories:**

| Category | Icon | Purpose |
|----------|------|---------|
| — TRAINING — | OrderTroopsToAttack | Combat drills, sparring, weapons practice |
| — CAMP TASKS — | Manage | Help surgeon, forge work, foraging, maintenance |
| — SOCIAL — | Conversation | Fire circle, drinking, dice, letter writing |
| — LANCE — | TroopSelection | Talk to leader, check on mates, help struggling soldiers |

**Activity Options:**
- Each activity shows as a clickable menu item under its category header
- Disabled activities show in the menu but are greyed out with tooltip explaining why
- Tooltips show: skill XP rewards, fatigue cost/relief, and any restrictions

**Activity Gating:**
| Condition | Display |
|-----------|---------|
| Tier requirement | "[Requires Tier X]" |
| Formation mismatch | "[Cavalry/Archer only]" |
| Wrong time of day | "[Available: Dawn/Day]" |
| On cooldown | "[Cooldown: X days]" |
| Too fatigued | "[Too fatigued]" |

**Success Messages (RP-flavored by category):**
- Training: "You complete the training. Your muscles ache, but you feel stronger."
- Tasks: "You finish the task. The camp runs a little smoother for your effort."
- Social: "Time passes pleasantly. The bonds of camaraderie strengthen."
- Lance: "You spend time with your lance mates. They appreciate your attention."

**Data Source:** `ModuleData/Enlisted/Activities/activities.json`

**File:** `src/Features/Camp/CampMenuHandler.cs` (AddCampActivitiesMenu region)

### Popups & Incidents (not menus)

Some player-facing UI in Enlisted is **not** a GameMenu. These are inquiry/incident-style popups that fire when it is safe (no battle/encounter/captivity), and they exist to simulate “camp life” without requiring scenes.

Key popups:
- **Enlistment Bag Check** (first enlistment)
  - Fires ~12 in-game hours after enlistment when safe.
  - Doc: **[Enlistment System](../Core/enlistment.md)** (Bag Check section) and **[Quartermaster](quartermaster.md)**
- **Pay Muster** (periodic)
  - Fires when the muster ledger reaches payday and it is safe to show UI.
  - Doc: **[Pay System](../Core/pay-system.md)**
- **Lance Life (Stories + Events)**
  - **Lance Life Stories**: daily-tick “story pack” popups from `ModuleData/Enlisted/StoryPacks/LanceLife/*.json` (inquiry UI).
  - **Lance Life Events**: the 109-event catalog under `ModuleData/Enlisted/Events/*.json` with delivery channels:
    - `channel: "menu"` (player-initiated training surfaced in Camp Activities)
    - `channel: "inquiry"` (automatic events shown as inquiry popups)
    - `channel: "incident"` (automatic “moment” events shown via native incidents)
  - Doc: **[lance life](../Gameplay/lance-life.md)** and **[master roadmap](../../ImplementationPlans/master-implementation-roadmap.md)**
- **Camp Life Simulation hooks** (conditions that influence other UI)
  - Not a single popup by itself; it is the condition layer that can drive future popups and menu availability (Quartermaster mood/stockouts, delayed pay/IOUs, etc.).
  - Doc: **[Camp Life Simulation](../Gameplay/camp-life-simulation.md)**

## Discharge paths (where they live)

There are **two** ways to leave service:

1. **Desert the Army** (from `enlisted_status`)
   - Immediate exit with penalties (crime/relation). This is the “panic button”.

2. **Request Discharge (Final Muster)** (from Camp)
   - Sets a pending discharge state and resolves at the next pay muster (“Final Muster”).
   - This is the intended “retire properly” path and is where pensions/severance are awarded.

**Navigation Features:**
- Back button at top for easy access
- Leave option at bottom of main menu
- Close button in Master at Arms popup
- Consistent menu state across navigation

---

## Technical Details

### Menu Structure

**Main Menu:**
```csharp
// Menu ID: enlisted_status
// Type: WaitGameMenu
// Purpose: Main service management hub
```

**Duty Selection:**
```csharp
// Menu ID: enlisted_duty_selection
// Type: WaitGameMenu
// Purpose: Choose daily assignment
// Features: Section headers, checkmarks, descriptions
```

**Menu Registration:**
- Registered in `EnlistedMenuBehavior.OnSessionLaunched()`
- Menu options added via `AddGameMenuOption()`
- Dynamic text variables set via `SetDynamicMenuText()`

### Dynamic Text System

**Purpose:** Real-time updates showing current selections

**Implementation:**
```csharp
// Set dynamic text for menu options
private void SetDynamicMenuText()
{
    var currentDuty = EnlistmentBehavior.Instance?.SelectedDuty ?? "None";
    
    // Update checkmarks based on current selection
    // Format: "[x] Duty Name" or "[ ] Duty Name"
}
```

**Checkmark Logic:**
- `[x]` for currently selected duty
- `[ ]` for available but not selected
- Updates automatically when selection changes

### Tier-Based Availability

**Duties:**
- All duties shown in menu
- Tier-locked duties greyed out with culture-specific rank requirement
- Formation-incompatible duties greyed out with formation requirement

**Implementation:**
```csharp
// Check duty availability
bool IsDutySlotAvailable(DutyDefinition duty)
{
    // Check formation compatibility first
    if (!IsDutyCompatibleWithFormation(duty))
        return false; // Show greyed with "[{formations} only]"
    
    // Check tier requirement
    if (duty.MinTier > currentTier)
        return false; // Show greyed with "[Requires {Rank}]"
    
    return true;
}
```

**XP Integration:**
- Selected duties connect to daily XP processing
- `EnlistedDutiesBehavior.AssignDuty()` processes selections
- Formation training works with selections
- Duty changes persist properly

---

## Edge Cases

### Menu State Consistency

**Problem:** Menu state can become inconsistent during navigation

**Solution:**
- Real-time refresh maintains menu state consistency
- `SetDynamicMenuText()` called on menu refresh
- Selection changes update immediately

### Tier Progression

**Scenario:** Player promotes while menu is open

**Handling:**
- Tier-locked duties unlock automatically
- Menu refresh shows new availability
- No need to close and reopen menu

### Duty Selection Persistence

**Scenario:** Player changes duty, then navigates away and back

**Handling:**
- Selection persists in `EnlistmentBehavior`
- Menu shows correct checkmark on return
- XP processing uses persisted selection

### Menu Activation Timing

**Scenario:** Menu activation during encounter transitions

**Handling:**
- Uses `NextFrameDispatcher` for safe activation
- Prevents timing conflicts with game state transitions
- No crashes or assertion failures

### Time Control Preservation

**Problem:** Vanilla `GameMenu.ActivateGameMenu()` and `SwitchToMenu()` force time to `Stop`, then wait menus call `StartWait()` which sets `UnstoppableFastForward`. This overrides the player's time preference.

**Solution:** Handle time mode conversion once in menu init, never in tick handlers:

1. **In Menu Init:** Call `StartWait()`, then convert unstoppable modes to stoppable equivalents
2. **Unlock Time Control:** Call `SetTimeControlModeLock(false)` to allow player speed changes
3. **No Tick Restoration:** Tick handlers must NOT restore time mode - this fights with user input

**Why No Tick Restoration:** For army members, native code uses `UnstoppableFastForward` when the user clicks fast forward. If tick handlers restore `CapturedTimeMode` whenever they see `UnstoppableFastForward`, the next tick immediately reverts user input, breaking speed controls.

**Correct Pattern:**
```csharp
// In menu init - convert once
args.MenuContext.GameMenu.StartWait();
Campaign.Current.SetTimeControlModeLock(false);
if (Campaign.Current.TimeControlMode == CampaignTimeControlMode.UnstoppableFastForward)
{
    Campaign.Current.TimeControlMode = CampaignTimeControlMode.StoppableFastForward;
}

// In tick handler - do NOT restore time mode
// Just handle menu-specific logic, leave time control alone
```

**Popup Handling:**
- Popup callbacks should NOT call `SafeActivateEnlistedMenu()` - this re-captures time incorrectly
- Cancel actions just close the popup - the menu underneath is already active
- Success actions refresh the menu without re-capturing time

---

## Bannerlord Menu APIs

Reference for Bannerlord's native menu system APIs and patterns.

### Menu Types

**MenuAndOptionType Values:**

| Type | Value | UI Widgets | Time Controls | Usage |
|------|-------|------------|---------------|-------|
| `RegularMenuOption` | 0 | None | Pauses game | Basic text menus |
| `WaitMenuShowProgressAndHoursOption` | 1 | Progress bar + hours | Works | Time-based activities |
| `WaitMenuShowOnlyProgressOption` | 2 | Progress only | Works | Menus with progress display |
| `WaitMenuHideProgressAndHoursOption` | 3 | Clean text only | Works | Army wait, settlement wait |

**Recommended:** Use `WaitMenuHideProgressAndHoursOption` for clean menus without progress widgets.

### Time Control Behavior

**Vanilla Behavior:**
1. `GameMenu.ActivateGameMenu()` sets `Campaign.Current.TimeControlMode = Stop`
2. For wait menus, `StartWait()` then sets `TimeControlMode = UnstoppableFastForward`
3. This overrides player's time preference (paused/playing)

**Time Control Modes:**

| Mode | Description |
|------|-------------|
| `Stop` | Game paused |
| `StoppablePlay` | Normal speed, can be paused |
| `StoppableFastForward` | Fast speed, can be paused |
| `UnstoppablePlay` | Normal speed, cannot be paused |
| `UnstoppableFastForward` | Fast speed, forced by StartWait() |
| `UnstoppableFastForwardForPartyWaitTime` | Party wait variant |

**Preserving Player Time Preference:**

Handle in menu init only (not in tick handlers):
```csharp
private void OnMenuInit(MenuCallbackArgs args)
{
    // Start wait to enable time controls
    args.MenuContext.GameMenu.StartWait();
    
    // Unlock so player can change speed
    Campaign.Current.SetTimeControlModeLock(false);
    
    // Convert unstoppable to stoppable (allows pause button)
    if (Campaign.Current.TimeControlMode == CampaignTimeControlMode.UnstoppableFastForward ||
        Campaign.Current.TimeControlMode == CampaignTimeControlMode.UnstoppableFastForwardForPartyWaitTime)
    {
        Campaign.Current.TimeControlMode = CampaignTimeControlMode.StoppableFastForward;
    }
    else if (Campaign.Current.TimeControlMode == CampaignTimeControlMode.Stop)
    {
        Campaign.Current.TimeControlMode = CampaignTimeControlMode.StoppablePlay;
    }
}

// In tick handler - do NOT modify time control mode
// Native uses UnstoppableFastForward for army members clicking fast forward
// Restoring CapturedTimeMode in tick fights with user input
```

### Menu Registration

```csharp
// Add wait game menu (recommended for time controls)
starter.AddWaitGameMenu("menu_id",
    "Menu Title: {TEXT_VAR}",
    OnMenuInit,
    OnMenuCondition,
    null, // OnConsequenceDelegate
    OnMenuTick,
    GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption,
    GameOverlays.MenuOverlayType.None,
    0f,
    GameMenu.MenuFlags.None,
    null);

// Add regular game menu (pauses game)
starter.AddGameMenu(menuId, menuTitle, menuIntroText, 
    menuFlags, menuBackgroundMeshName);
```

### Background and Audio

```csharp
[GameMenuInitializationHandler("menu_id")]
private static void OnMenuBackgroundInit(MenuCallbackArgs args)
{
    // Set culture-appropriate background
    args.MenuContext.SetBackgroundMeshName(Hero.MainHero.MapFaction.Culture.EncounterBackgroundMesh);
    
    // Set ambient sound
    args.MenuContext.SetAmbientSound("event:/map/ambient/node/settlements/2d/camp_army");
    args.MenuContext.SetPanelSound("event:/ui/panels/settlement_camp");
}
```

### Text Variables

```csharp
// Get menu text and set variables (in init or tick handler)
var text = args.MenuContext.GameMenu.GetText();
text.SetTextVariable("PARTY_LEADER", lordName);
text.SetTextVariable("PARTY_TEXT", statusContent);

// Global text variables
MBTextManager.SetTextVariable("VARIABLE_NAME", value, false);
```

### Menu Navigation {#menu-navigation-api}

```csharp
GameMenu.SwitchToMenu("target_menu_id");  // Switch between menus
GameMenu.ExitToLast();                     // Return to previous menu
args.MenuContext.GameMenu.EndWait();       // End wait menu properly
```

### Menu Options

```csharp
starter.AddGameMenuOption("menu_id", "option_id", "Option Text",
    args =>
    {
        args.optionLeaveType = GameMenuOption.LeaveType.Trade;
        args.Tooltip = new TextObject("Tooltip text");
        return true; // Available
    },
    OnOptionSelected,
    isLeave: false,
    priority: 0);
```

### LeaveType Icons

| LeaveType | Icon Purpose |
|-----------|--------------|
| `Continue` | Continue/default action |
| `TroopSelection` | Troop management |
| `Trade` | Trading/equipment |
| `Conversation` | Dialog |
| `Submenu` | Navigation |
| `Manage` | Management |
| `Leave` | Exit/leave action |
| `Escape` | Warning/danger |
| `Mission` | Combat |
| `DefendAction` | Defense |
| `Raid` | Aggressive action |
| `SiegeAmbush` | Siege-related |
| `OrderTroopsToAttack` | Command troops |

### Popup Dialogs

**ShowInquiry:**
```csharp
InformationManager.ShowInquiry(
    new InquiryData(
        title,
        message,
        isAffirmativeOptionShown: true,
        isNegativeOptionShown: true,
        affirmativeText: "Yes",
        negativeText: "No",
        affirmativeAction: () => { /* on yes */ },
        negativeAction: () => { /* on no */ }),
    pauseGameActiveState: false); // false = don't pause game
```

**ShowMultiSelectionInquiry:**
```csharp
MBInformationManager.ShowMultiSelectionInquiry(
    new MultiSelectionInquiryData(
        title,
        description,
        options,        // List<InquiryElement>
        isExitShown: true,
        minSelectableOptionCount: 1,
        maxSelectableOptionCount: 1,
        affirmativeText: "Select",
        negativeText: "Cancel",
        affirmativeAction: selected => { /* on select */ },
        negativeAction: _ => { /* on cancel */ }),
    pauseGameActiveState: false);
```

**Note:** Popup callbacks should not call menu activation methods that re-capture time state. The underlying menu remains active when the popup closes.

---

## API Reference

Enlisted-specific menu APIs and patterns.

### Time State Management

```csharp
// Capture time state before menu activation (shared across all menus)
QuartermasterManager.CaptureTimeStateBeforeMenuActivation();

// Access shared captured time mode
CampaignTimeControlMode? captured = QuartermasterManager.CapturedTimeMode;
```

### Menu Activation

```csharp
// Safe menu activation (with time preservation)
EnlistedMenuBehavior.SafeActivateEnlistedMenu();

// Direct activation (capture time first)
QuartermasterManager.CaptureTimeStateBeforeMenuActivation();
GameMenu.ActivateGameMenu("enlisted_status");
```

### Selection Management

```csharp
// Get current selection
string currentDuty = EnlistmentBehavior.Instance?.SelectedDuty;

// Set duty
EnlistmentBehavior.Instance?.SetSelectedDuty(dutyId);
```

---

## Debugging

**Log Categories:**
- `"Interface"` - Menu system activity
- `"Menu"` - Menu navigation and state

**Key Log Points:**
```csharp
// Menu activation
ModLogger.Info("Interface", $"Activating menu: {menuId}");
ModLogger.Debug("Menu", $"Menu state: duty={duty}");

// Selection changes
ModLogger.Info("Menu", $"Duty changed: {oldDuty} -> {newDuty}");

// Tier checks
ModLogger.Debug("Menu", $"Tier check: required={required}, current={current}, allowed={allowed}");
```

**Common Issues:**

**Duties greyed out unexpectedly:**
- Check formation compatibility (`IsDutyCompatibleWithFormation()`)
- Check tier requirement vs player's current tier
- Verify expansion is active for naval duties (War Sails)

**Checkmarks not updating:**
- Verify `SetDynamicMenuText()` is called in refresh
- Check selection persistence in `EnlistmentBehavior`
- Ensure menu refresh happens after selection changes

**XP not applying:**
- Ensure selected duties connect to `EnlistedDutiesBehavior.AssignDuty()`
- Check daily tick is processing assignments
- Verify duty IDs match configuration

**Menu doesn't activate:**
- Check `NextFrameDispatcher` is not busy
- Verify encounter state allows menu activation
- Check for timing conflicts with game state transitions

**Debug Output Location:**
- `Modules/Enlisted/Debugging/enlisted.log`

**Related Files:**
- `src/Features/Interface/Behaviors/EnlistedMenuBehavior.cs` - Main enlisted status menu
- `src/Features/Lances/Behaviors/EnlistedLanceMenuBehavior.cs` - My Lance menu
- `src/Features/Conditions/EnlistedMedicalMenuBehavior.cs` - Medical Attention menu
- `src/Features/Camp/CampMenuHandler.cs` - Camp menu
- `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs` - Core enlistment state
- `src/Features/Assignments/Behaviors/EnlistedDutiesBehavior.cs` - Duty system

---

## Menu Backgrounds (Reference)

Enlisted’s game-menus use **menu background meshes** (not `.png` images inside this repo) via:

- `args.MenuContext.SetBackgroundMeshName("<mesh_name>");`

### Where this mod sets menu backgrounds

- **Enlisted status / duty menus**: choose the current lord/kingdom culture’s encounter background mesh, with a fallback.
  - **File**: `src/Features/Interface/Behaviors/EnlistedMenuBehavior.cs`
  - **Fallback**: `encounter_looter`
  - **Typical source**: `CultureObject.EncounterBackgroundMesh`

- **Camp menus**: fixed “meeting” background.
  - **File**: `src/Features/Camp/CampMenuHandler.cs`
  - **Mesh**: `encounter_meeting`

### Culture encounter background meshes (vanilla)

These come from Bannerlord’s culture XML (`Modules/SandBoxCore/ModuleData/spcultures.xml`), attribute:
`encounter_background_mesh="..."`

- `encounter_aserai`
- `encounter_battania`
- `encounter_empire`
- `encounter_khuzait`
- `encounter_sturgia`
- `encounter_vlandia`
- `encounter_looter`
- `encounter_desert_bandit`
- `encounter_forest_bandit`
- `encounter_mountain_bandit`
- `encounter_shore_bandit`

### Other background meshes used by base-game menus (observed)

These are mesh names used by other native menu handlers (useful as “known good” candidates):

- `encounter_caravan`
- `encounter_peasant`
- `encounter_lose`
- `encounter_naval`
- `gui_bg_lord_khuzait`
- `town_blockade`
- `wait_ambush`
- `wait_besieging`
- `wait_captive_at_sea_female`, `wait_captive_at_sea_male`
- `wait_captive_female`, `wait_captive_male`
- `wait_prisoner_female`, `wait_prisoner_male`

### Notes / gotchas

- **This repo does not ship menu background textures**: currently only `preview.png` exists in-repo; menu backdrops are engine meshes.
- **Mesh availability depends on game install / DLC**: if a mesh name is missing in the user’s install, it may render incorrectly or fall back.
- **Culture-driven menus** will change depending on the lord/kingdom the player is serving.

---

## Related Documentation

- [Dialog System](dialog-system.md) - Menu activation after enlistment
- [Camp](camp-tent.md) - Service records and retinue management
- [Duties System](../Core/duties-system.md) - Duty definitions and XP
- [Lance Assignments](../Core/lance-assignments.md) - 9-tier progression and culture-specific ranks
- [Camp Life Simulation](../Gameplay/camp-life-simulation.md) - Camp conditions and integrations
