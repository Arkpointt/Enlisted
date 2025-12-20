# Enlisted UI Systems - Master Reference

**Document Purpose:** Complete reference for all user interface systems in the Enlisted mod.

**Last Updated:** December 18, 2025  
**Status:** All Systems Implemented

---

## Master Index

### Quick Navigation by System

| System | Type | Purpose | Access Point |
|--------|------|---------|--------------|
| [Text Menus](#text-menu-system) | GameMenu | Core navigation and status displays | Throughout mod |
| [Modern Event UI](#modern-event-ui) | Gauntlet | Rich event screens with visuals | Lance Life events |
| [Camp Hub](#camp-hub-system) | GameMenu | Personal service management | Enlisted Status → Camp |
| [Dialog System](#dialog-system) | Conversation | Lord interactions and enlistment | Talk to lords |
| [Quartermaster](#quartermaster-system) | Equipment | Formation-based gear system | Camp → Quartermaster |
| [News Feeds](#news-and-dispatches) | Text Display | Kingdom and personal news | Status menus |

### Quick Navigation by Feature

**Service Management:**
- [Service Records](#service-records) - Track your military history
- [Pay & Pension](#pay-and-pension) - Wage and pension status
- [Discharge Actions](#discharge-actions) - Proper retirement path
- [Personal Retinue](#personal-retinue) - Command soldiers (T7+)
- [Companion Assignments](#companion-assignments) - Battle roster management

**Activities and Duties:**
- [Duty Selection](#duty-selection-interface) - Choose your military role
- [Camp Activities](#camp-activities-menu) - Training, tasks, social

**Equipment:**
- [Quartermaster](#quartermaster-hero) - Persistent NPC vendor
- [Baggage Train](#baggage-train) - Equipment stash
- [Provisions](#provisions-system) - Rations and supplies

**Information:**
- [Kingdom News](#kingdom-news-feed) - Strategic events
- [Personal News](#personal-news-feed) - Your immediate context
- [Lance Menu](#my-lance-menu) - Lance roster and relationships

---

## Overview

The Enlisted mod uses two complementary UI technologies to create a professional military service experience:

### 1. Text Menus (GameMenu)
Classic Bannerlord text menus with modern styling for core navigation and management. Clean, stable, and vanilla-friendly.

**Used For:**
- Main status displays
- Navigation hubs
- List-based selections
- Simple status information

**Features:**
- Modern icons via `LeaveType`
- Hover tooltips
- Culture-appropriate backgrounds
- Ambient audio
- Professional typography

### 2. Gauntlet Screens (ScreenBase)
Full-screen custom UI overlays for rich visual experiences requiring complex layouts.

**Used For:**
- Event presentation (Lance Life)
- Complex management interfaces
- Visual storytelling
- Real-time data displays

**Features:**
- Character portraits
- Visual choice buttons
- Live escalation tracking
- Rich text formatting
- Smooth animations

---

## Text Menu System

### Main Enlisted Status Menu

**Menu ID:** `enlisted_status` (WaitGameMenu)

The primary service management hub. Displays your current service status with escalation tracks and provides navigation to all major systems.

**Status Display Includes:**
- Party Objective (following lord/on leave)
- Army composition (if in army)
- Days Enlisted
- Rank and Tier
- Formation
- Fatigue level
- **Escalation Tracks:**
  - Heat with visual bar `▓▓▓░░░░░░░ 3/10 [Watched]`
  - Discipline with visual bar and warnings
  - Lance Reputation with numeric value `+15 (Trusted)`
- Time of day and days from town
- Camp snapshot (when Camp Life active)
- Pay Status (shows tension when pay is late)
- Owed Backpay (if applicable)
- Kingdom News section (top 3 headlines)

**Navigation Options:**

| Option | Icon | Purpose | Condition |
|--------|------|---------|-----------|
| Debug Tools | Submenu | QA helpers | Always (dev/QA) |
| Camp | Submenu | Camp hub access | Always (while enlisted) |
| Decisions | Submenu | Pending decisions | Always |
| My Lord... | Conversation | Speak with nearby lords | Lord nearby |
| Visit Settlement | Submenu | Enter settlement | In settlement |
| Leave / Discharge / Desert | Leave | Departure actions | Always |

**Modern Styling:**
- Culture-appropriate background from lord's kingdom
- Ambient camp audio: `event:/map/ambient/node/settlements/2d/camp_army`
- Professional typography with section headers
- Color-coded escalation indicators
- Tooltips explaining each option

**Escalation Track Thresholds:**

| Track | Threshold | Warning Label | Effect |
|-------|-----------|---------------|--------|
| Heat | 3 | Watched | Minor suspicion |
| Heat | 5 | Shakedown | Kit inspections |
| Heat | 7 | Audit | Investigation |
| Heat | 10 | EXPOSED | Critical - discharge risk |
| Discipline | 3 | Extra Duty | Additional work |
| Discipline | 5 | Hearing | Formal review |
| Discipline | 7 | Blocked | Promotion blocked |
| Discipline | 10 | DISCHARGE | Forced discharge imminent |

**File:** `src/Features/Interface/Behaviors/EnlistedMenuBehavior.cs`

---

### My Lance Menu

**Menu ID:** `enlisted_lance` (WaitGameMenu)

View your lance position, see the full roster with relationship indicators, interact with lance mates, and track welfare.

**Header Display:**
- Lance name with decorative border
- Your position and rank
- Days served with lance
- Lance Reputation with visual bar
- Heat and Discipline (if > 0)
- Condensed roster (Leader and Second)

**Menu Options:**

| Section | Option | Icon | Description |
|---------|--------|------|-------------|
| **Roster** | View Full Roster | TroopSelection | All 10 slots with relationship indicators |
| **Interactions** | Talk to Lance Leader | Conversation | Speak with leader, relationship-based dialogue |
| | Talk to Second | Conversation | Speak with second-in-command |
| **Welfare** | Check on the Wounded | Manage | Visit wounded (+1 Lance Rep) |
| | Honor the Fallen | Mission | Remember fallen (+2 Lance Rep) |
| **Info** | View Lance History | Submenu | Battle record, achievements |
| | Return to Camp | Leave | Return to main menu |

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
- Culture-specific rank titles (e.g., "Miles" for Empire T2)
- Slot position based on tier (higher tier = earlier slot)
- Named persona members (if enabled)
- Wounded/fallen tracking
- Leader relationship affects dialogue tone

**File:** `src/Features/Lances/Behaviors/EnlistedLanceMenuBehavior.cs`

---

### Camp Activities Menu

**Menu ID:** `enlisted_camp_activities` (WaitGameMenu)

Organized menu for player-initiated activities. Earn XP, manage fatigue, and build relationships through structured camp actions.

**Access:** Camp → "Camp Activities"

**Menu Header (RP-flavored):**
> The camp stirs with activity. Soldiers drill, fires crackle, and the smell of cooking fills the air. What will you do with your time?

**Display Includes:**
- Personal News section (top 2 headlines)
- Activity categories with section headers
- Activity options with tooltips
- Disabled activities shown greyed out with reasons

**Categories:**

| Category | Icon | Activities |
|----------|------|------------|
| — TRAINING — | OrderTroopsToAttack | Combat drills, sparring, weapons practice |
| — CAMP TASKS — | Manage | Help surgeon, forge work, foraging |
| — SOCIAL — | Conversation | Fire circle, drinking, dice, letters |
| — LANCE — | TroopSelection | Talk to leader, check on mates, help soldiers |

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

**File:** `src/Features/Camp/CampMenuHandler.cs`

---

### Medical Attention Menu

**Menu ID:** `enlisted_medical` (WaitGameMenu)

Seek treatment when injured, ill, or exhausted. Only accessible when player has an active condition.

**Options:**

| Option | Icon | Description |
|--------|------|-------------|
| Request Treatment from Surgeon | Manage | Full treatment, costs 2 fatigue |
| Treat Yourself (Field Medic) | Manage | Self-treatment if Field Medic duty, grants Medicine XP |
| Purchase Herbal Remedy | Trade | Costs 50 gold |
| Rest in Camp | Wait | Light recovery option |
| View Detailed Status | Submenu | Full condition breakdown popup |
| Back | Leave | Return to Enlisted Status |

**File:** `src/Features/Conditions/EnlistedMedicalMenuBehavior.cs`

---

## Camp Hub System

**Menu ID:** `enlisted_camp_hub` (WaitGameMenu)

The Camp hub is your personal military service center. Provides camp-facing navigation to all service management features.

**Access:** Enlisted Status → "Camp"

**Menu Structure:**
```
[Camp]
├── Service Records
│   ├── Current Posting (this enlistment)
│   ├── Faction Records (per-faction history)
│   └── Lifetime Summary (cross-faction totals)
├── Pay & Pension
│   ├── Pending Muster Pay
│   ├── Next Payday
│   ├── Last Pay Outcome
│   └── Pension Amount / Status
├── Discharge
│   ├── Request Discharge (Final Muster)
│   └── Cancel Pending Discharge
├── Personal Retinue (Tier 7+ only - Commander ranks)
│   ├── Current Muster
│   ├── Purchase Soldiers
│   ├── Replenish Losses
│   └── Dismiss Soldiers
└── Companion Assignments
    └── Battle Roster (Fight / Stay Back toggle)
```

---

### Service Records

Tracks military service history per faction and lifetime totals.

**Faction-Specific Records:**
- Each faction (kingdom, minor faction, mercenary) has separate records
- Tracks: terms completed, days served, highest rank, battles fought, lords served, enlistments
- Key format: `kingdom_{StringId}`, `minor_{StringId}`, `merc_{StringId}`

**Lifetime Statistics:**
- Total kills across all factions
- Total years served (cumulative)
- Total terms completed
- Total enlistments
- List of all factions served

**Display:**
- Current Posting: Shows current enlistment stats
- Faction Records: Per-faction history
- Lifetime Summary: Cross-faction totals

---

### Pay and Pension

Surfaces the pay-system state so players always know where they stand.

**Information Displayed:**
- **Pending Muster Pay:** Current ledger total waiting for pay muster
- **Next Payday:** When the next pay muster should trigger (may defer if unsafe)
- **Last Pay Outcome:** What happened the last time pay muster resolved
- **Pay Muster Pending:** Whether a pay muster is queued and waiting for a safe moment
- **Pension Amount:** Current pension stipend (if any)
- **Pension Status:** Whether pension is paused (e.g., during enlistment)

---

### Discharge Actions

Discharge is handled as **Pending Discharge → Final Muster**:

**Request Discharge (Final Muster):**
- Sets pending discharge flag
- Resolves at the next pay muster
- Final Muster is where severance, pension, and gear handling occur

**Cancel Pending Discharge:**
- Clears the pending discharge flag
- Allows you to continue service

**Note:** This is the proper retirement path. For immediate exit, use "Desert the Army" from the main menu (with penalties).

---

### Personal Retinue

At Tier 7+ (Commander ranks), players can command soldiers that fight alongside them in battle.

**Tier-Based Capacity:**

| Tier | Unit Name | Max Soldiers |
|------|-----------|--------------|
| 7 | Commander (Regular) | 20 |
| 8 | Commander (Veteran) | 30 |
| 9 | Commander (Elite) | 40 |

**Soldier Types:**
- **Infantry:** Foot soldiers with sword and shield
- **Archers:** Foot ranged (bows/crossbows)
- **Cavalry:** Mounted melee
- **Horse Archers:** Mounted ranged (not available for all factions)

**Faction Availability:**
- Horse Archers unavailable for: Vlandia, Battania, Sturgia
- Horse Archers available for: Empire, Khuzait, Aserai
- UI grays out unavailable types with tooltip

**Troop Quality:**
- Tier 7: Mix of Tier 2-3 troops (40%/60% distribution)
- Tier 8: Mix of Tier 3-4 troops (50%/50% distribution)
- Tier 9: Mix of Tier 3-4 troops (40%/60% distribution)
- All soldiers are of ONE chosen type (no mixing)

**Gold Costs:**
- **Purchase:** Uses native `GetTroopRecruitmentCost()` formula (~100-400 gold per soldier)
- **Daily Upkeep:** 2 gold per soldier per day
- **Desertion:** One soldier deserts per unpaid day

**Replenishment Systems:**

1. **Trickle System (Free, Slow):**
   - 1 soldier every 2-3 days (randomized)
   - Automatic, no cost
   - Respects tier capacity and party size limits
   - Waits for wounded soldiers to heal or die before filling slots

2. **Instant Requisition (Costly, Fast):**
   - Instant fill of all missing soldiers
   - Cost: `GetTroopRecruitmentCost() × missing soldiers`
   - 14-day cooldown between requisitions

**Party Size Safeguards:**
- Checks `PartyBase.MainParty.PartySizeLimit` before adding soldiers
- Only adds up to available party space
- UI shows party limit vs. tier capacity
- Warns when party limit restricts retinue

**Battle Integration:**
- Retinue soldiers stored in `MobileParty.MainParty.MemberRoster`
- Spawn naturally via native `PartyGroupTroopSupplier`
- Fight in same formation as player (unified squad)
- Casualties tracked by native roster system

**Retinue Lifecycle:**

| Event | Retinue Behavior |
|-------|------------------|
| On Leave | Stays in player's invisible party |
| Player Captured | **Cleared** - soldiers scattered |
| Enlistment Ends | **Dismissed** - return to army ranks |
| Lord Dies / Army Defeated | **Lost** - scattered in chaos |
| Battle Casualties | Native tracking updates roster |

---

### Companion Assignments

Manage which companions fight in battles (Tier 4+).

**Settings:**
- **Fight (default):** Companion spawns in battle, faces all risks
- **Stay Back:** Companion doesn't spawn, immune to battle outcomes

**Why "Stay Back" Is Safe:**
- Native battle resolution only processes spawned agents
- "Stay back" companions survive army destruction, player capture, all battle outcomes

**Command Restrictions:**
- Companions **cannot** become formation captains or team generals during enlistment
- Prevents companions from giving tactical orders or appearing as commanders
- Applies to all companions regardless of "Fight" or "Stay Back" setting

**UI:**
- Camp → Companion Assignments
- Shows list of companions with toggle (Fight / Stay Back)
- Changes saved immediately

See [Companion Management](../Core/companion-management.md) for full details.

---

## Duty Selection Interface

**Location:** Camp Management Screen → Duties Tab (Gauntlet UI)

**Access:** Enlisted Status → "Camp Management" → Duties tab OR Enlisted Status → "Report for Duty"

**Implementation:** Orders-screen style interface with persistent duty assignments. Left panel shows available duties, right panel shows selected duty details. Data-driven from `duties_system.json`.

**Duty Assignment Flow:**
- **T1 Players:** Auto-assigned "Runner" duty, can change duties freely (no approval needed)
- **T2+ Players:** Must **request** duty changes through lance leader approval

**Duty Request System (T2+):**

| UI Element | Meaning |
|------------|---------|
| "Request Assignment" button | Duty available for request |
| "On Cooldown" button (disabled) | Request denied, must wait X days |
| "Locked" | Tier or formation requirement not met |
| "(Current)" badge in list | Currently active duty |

Request approval requires:
- 14-day cooldown between requests
- Minimum 10 lance reputation
- Meeting the duty's tier requirement
- Duty compatible with player's formation

**Persistence:**
- Duties persist across sessions - your assigned duty stays active until you successfully request a change
- Current duty clearly displayed at top of panel with transfer availability status
- Similar to giving orders to troops - select, assign, and it stays

**Duty Selection (Available T1+; data-driven)**

| Duty | Min Tier | Formations |
|------|----------|------------|
| Runner | 1 | Infantry |
| Quartermaster | 1 | Infantry |
| Field Medic | 1 | Infantry |
| Armorer | 1 | Infantry |
| Engineer | 2 | Infantry |
| Scout | 1 | Archer / Cavalry / Horse Archer |
| Lookout | 1 | Archer |
| Messenger | 1 | Cavalry / Horse Archer |
| Boatswain | 1 | Naval (War Sails only) |
| Navigator | 2 | Naval (War Sails only) |

**Starter Duties (Auto-assigned at T2):**

| Formation | Starter Duty |
|-----------|--------------|
| Infantry | Runner |
| Archer | Lookout |
| Cavalry | Messenger |
| Horse Archer | Scout |
| Naval | Boatswain |

**Tier Locking:**
- Duties with tier requirements above player's current tier show culture-specific rank
- Locked duties are grayed out but visible (shows progression path)
- Tooltips explain unlock requirements

**Details Panel:**
- Right panel shows full duty information when selected
- Duty title, description, effects (skill XP, wage modifiers, special abilities)
- Requirements display (tier, formation, special conditions)
- Status-aware button text ("Request Assignment" / "Current Duty" / "On Cooldown")

---

## Quartermaster System

### Quartermaster Hero

Each lord has a unique, persistent Quartermaster NPC with personality and relationship tracking.

**Archetypes:**
- **Veteran:** Pragmatic old soldier, practical advice, no-nonsense attitude
- **Merchant:** Trade-minded and opportunistic, treats the armory like a market
- **Bookkeeper:** Bureaucratic clerk type, obsessed with forms and ledgers
- **Scoundrel:** Opportunistic, knows black market contacts, offers "creative" solutions
- **Believer:** Pious and moral, offers spiritual guidance, encourages loyalty
- **Eccentric:** Superstitious and odd, speaks in omens and strange observations

**PayTension-Aware Dialogue:**
When pay is late (PayTension 40+), the Quartermaster offers archetype-specific advice:
- **Scoundrel:** Black market contacts, opportunities to make coin
- **Believer:** Moral guidance, encouragement to stay faithful
- **Veteran:** Practical survival advice, desertion warnings at 60+ tension

**Relationship Milestones:**

| Level | Relationship | Discount | Unlocks |
|-------|-------------|----------|---------|
| Stranger | 0-19 | 0% | Basic access |
| Known | 20-39 | 0% | Chat option |
| Trusted | 40-59 | 5% | Black market hints |
| Respected | 60-79 | 10% | Better dialogue |
| Battle Brother | 80-100 | 15% | Special items |

**Gaining Relationship:**
- First meeting: +5
- Chatting: +3
- Buying equipment: +1 per purchase
- Helping with PayTension options: +2 to +5

---

### Equipment System

**Access:** Enlisted Status → "Visit Quartermaster"

The Quartermaster provides formation-appropriate weapons, armor, and mounts based on your formation, tier, and culture.

**Key Features:**
- **Purchase-based:** Equipment costs denars
- **Formation-driven:** Availability based on your chosen formation (Infantry/Archer/Cavalry/Horse Archer)
- **Culture-appropriate:** Gear matches your enlisted lord's culture
- **Tier-unlocked:** Higher tiers unlock better equipment
- **Relationship discounts:** Build trust for 5-15% discounts
- **NEW indicators:** Newly unlocked items marked with `[NEW]` after promotion
- **Buyback:** Sell gear back to Quartermaster for reduced price

**Equipment Discovery:**
Quartermaster dynamically discovers available equipment by:
- Scanning troops matching your formation and culture
- Filtering to troops at or below your tier
- Collecting all equipment from those troops

**Purchasing & Equipping:**
- **Weapons:** Placed into first empty weapon slot (Weapon0–Weapon3)
- **Armor/mount slots:** Equipped into relevant slot; any replaced item moved to party inventory

**Pricing:**
- **Purchase price:** `item.Value × quartermaster.soldier_tax`
- **Provisioner/Quartermaster duty discount:** 15% off quartermaster prices
- **Relationship discount:** 0–15% off based on relationship milestones
- **Buyback price:** `item.Value × quartermaster.buyback_rate`

**Promotion & Quartermaster:**
When promoted, you are prompted to visit the Quartermaster:
- No auto-equip on promotion
- Message displays: "Report to the Quartermaster for your new kit"
- Newly unlocked items are marked with `[NEW]`

---

### Baggage Train

**Access:** Enlisted Status → "Baggage Train"

The Baggage Train is the equipment stash created by the bag check.

**Fatigue Cost by Tier:**
- Tier 1–2: **4 fatigue**
- Tier 3–6: **2 fatigue**
- Tier 7+: **0 fatigue** (Commander ranks)

**Enlistment Bag Check (First Enlistment):**
About 1 in-game hour after enlisting (when safe), the quartermaster runs a bag check.

**Options:**
- **"Stow it all" (50g):** Moves inventory + equipped items into baggage train, charges 50 denar wagon fee (clamped to what you can afford)
- **"Sell it all" (60%):** Liquidates inventory + equipped items at 60% value
- **"I'm keeping one thing" (Roguery 30+):** Attempts to keep one item (highest-value). If Roguery < 30, it is confiscated

---

### Provisions System

Purchase rations for morale and fatigue benefits.

**Personal Provisions:**

| Tier | Base Cost | Duration | Morale | Fatigue |
|------|-----------|----------|--------|---------|
| Supplemental Rations | 10g | 1 day | +2 | - |
| Officer's Fare | 30g | 2 days | +4 | +2 (immediate) |
| Commander's Feast | 75g | 3 days | +8 | +5 (immediate) |

**Retinue Provisioning (T7+):**

| Tier | Base Cost/Soldier | Duration | Effect |
|------|-------------------|----------|--------|
| Bare Minimum | 2g | 7 days | -5 morale |
| Standard | 5g | 7 days | No modifier |
| Good Fare | 10g | 7 days | +5 morale |
| Officer Quality | 20g | 7 days | +10 morale |

Warning at 2 days remaining; starvation penalties at expiration.

---

## Dialog System

Centralized conversation manager handling all military service dialogs.

**Key Features:**
- Single manager for all military conversations
- Centralized dialog ID management (no conflicts)
- Shared condition and consequence methods
- Immediate menu activation after enlistment (no encounter gaps)
- Parallel dialog variants for minor faction lords

**File:** `src/Features/Conversations/Behaviors/EnlistedDialogManager.cs`

---

### Enlistment Flow

**Standard Flow (Kingdom Lords):**
1. Talk to any lord → "I have something else to discuss" → "I wish to serve in your warband"
2. Lord responds based on relationship and faction status
3. Player confirms → Immediate enlistment with menu switch
4. No encounter gaps - player goes straight to enlisted status menu

**Minor Faction Flow:**
- Uses same conversation states but with higher-priority mercenary-themed lines
- Text themes: contract/payment focus, company camaraderie
- Leave and early-discharge have minor-faction variants
- Retirement/discharge requests via **Camp → Request Discharge**

**Army Leader Restriction:**
If player is leading their own army, special dialog appears:
- **Player:** "My lord, I offer you my sword and my loyalty. Will you have me in your ranks?"
- **Lord:** "Hold, friend. I see you already command men of your own. A general cannot become a foot soldier while lords still march beneath his banner. Disband your army first, then we may speak of service."
- **Player:** "I understand, my lord. I shall see to my army's affairs first."
- Conversation ends (player cannot proceed with enlistment)

---

### Status Dialogs

**Purpose:** Check current military service status

**Flow:**
- Talk to lord → "How goes my service?"
- Shows current enlistment information
- Displays tier, XP, days served, etc.

---

### Management Dialogs

**Types:**
- Promotion notifications
- Equipment management conversations
- Discharge guidance (kingdom + minor variants)
- Leave/return and early discharge (kingdom + minor variants)

**Behavior:**
- Context-dependent availability
- Consistent with enlistment state
- Integrated with menu system

---

## Modern Event UI

**Status:** ✅ IMPLEMENTED  
**Files:** `src/Features/Lances/UI/LanceLifeEventScreen.cs`, `src/Features/Lances/UI/ModernEventPresenter.cs`

This system replaces basic inquiry popups with stunning custom Gauntlet screens for Lance Life events.

**Visual Features:**
- Full-screen event cards with modern design
- Character portraits or scene images
- Visual choice buttons with icons and hover effects
- Live escalation track displays (Heat, Discipline, Lance Rep)
- Rich text formatting with colors
- Smooth animations and transitions

---

### Modern Card Layout

- **Semi-transparent overlay** with blur effect
- **Centered event card** (1100x750px) with rounded corners
- **Decorative header bar** with category badge and title
- **Professional typography** with size hierarchy

---

### Scene Visualization

- **Character portraits** for interpersonal events (3D character tableau)
- **Scene images** for situational events (training, camp, etc.)
- **Character name plates** with semi-transparent backgrounds
- **Dynamic character selection** (lance leader, companions, etc.)

---

### Choice Presentation

- **Card-style buttons** with colored accent bars
- **Risk indicators** (green for safe, yellow/orange/red for risky)
- **Icons** for each choice type (shield, warning, coin, XP, etc.)
- **Live cost/reward display** in button corners
- **Disabled state overlays** with clear reason text

---

### Escalation Tracking

- **Progress bars** for Heat, Discipline, and Lance Rep
- **Color-coded bars** (red for heat, blue for discipline, green for rep)
- **Live values** updating after each choice
- **Visual thresholds** showing dangerous levels

---

### Rich Text Formatting

- **Colored text** for emphasis (gold for rewards, red for costs)
- **Inline markers** (text labels for gold/warnings)
- **Paragraph spacing** for readability
- **Scrollable story text** for long narratives

---

### Usage

**Replace Inquiry Popups:**

Old way (basic popup):
```csharp
LanceLifeEventInquiryPresenter.TryShow(eventDef, enlistment);
```

New way (modern UI):
```csharp
ModernEventPresenter.TryShow(eventDef, enlistment);
```

Safe way (with fallback):
```csharp
// Tries modern UI first, falls back to inquiry if it fails
ModernEventPresenter.TryShowWithFallback(eventDef, enlistment, useModernUI: true);
```

**Direct Screen Opening:**
```csharp
// Open the screen directly from anywhere
LanceLifeEventScreen.Open(eventDef, enlistment, onClosed: () =>
{
    // Called when the screen closes
    Console.WriteLine("Event completed!");
});
```

---

### Category Colors

| Category | Color | Hex |
|----------|-------|-----|
| Duty | Blue | `#4488FF` |
| Social | Green | `#44FF88` |
| Combat | Red | `#FF4444` |
| Training | Yellow | `#FFAA44` |
| Lance | Purple | `#AA44FF` |
| Discipline | Orange | `#FF8844` |

---

### Risk Indicators

| Risk Level | Color | Hex |
|------------|-------|-----|
| Safe | Green | `#44FF44` |
| Minor Risk | Yellow | `#FFFF44` |
| Moderate Risk | Orange | `#FFAA44` |
| High Risk | Red | `#FF4444` |

---

## News and Dispatches

**Status:** ✅ IMPLEMENTED  
**File:** `src/Features/Interface/Behaviors/EnlistedNewsBehavior.cs`

A lightweight dispatch board shown inside the custom enlisted menus that reports real in-game campaign events in a diegetic "scribe/messenger" tone.

**Purpose:**
- Make enlisted service feel embedded in a living kingdom
- Provide believable military "sitreps" without rewriting Bannerlord AI/economy
- Keep costs low and stability high by using event-driven generation

**Design Goals:**
- **Believable, not random:** Headlines follow campaign events
- **Low overhead:** Event-driven ingestion + 2-day bulletin snapshot
- **Rate-limited:** Avoid spam; publish top items, roll up the rest
- **Localization-friendly:** All text uses placeholders
- **Mod-safe:** No invasive patches; read-only

---

### Two-Feed Architecture

The news system is split into two separate feeds based on relevance and context.

---

### Kingdom News Feed

**Location:** `enlisted_status` menu (main enlisted status screen)

**Integration:** Appended to the main status text (after core status info, before escalation tracks)

**Scope:** Kingdom-wide strategic events
- Wars, major battles, settlements falling
- Lord captures/executions
- Politics (clan defections, fief grants)

**Display:** Top 3–5 headlines as a "Kingdom News" section

**Example:**
```
Party Objective : Following Lord Derthert
Army : Derthert (8 parties, 547 men)
Days Enlisted : 23
Rank : Man-at-Arms (Tier 2)
Formation : Infantry
Fatigue : 12/100

--- Kingdom News ---
War declared: Vlandia vs Battania.
Victory: Lord Aldric defeated Lord Calatild near Marunath.
Ormidore has fallen.
```

---

### Personal News Feed

**Location:** `enlisted_camp_activities` menu (camp activities screen)

**Integration:** Prepended to the top of the menu text (before camp activities list)

**Scope:** Your immediate service context
- Personal mentions (your party's participation in battles/sieges)
- Your lord's army movements (forming, dispersing, regrouping)
- Your immediate unit (retinue casualties, camp conditions)
- Direct orders or changes to your posting

**Display:** Top 2–3 items, more immediate/actionable than kingdom news

**Example:**
```
--- Army Orders ---
Host forming under Lord Derthert's banner.
Our forces helped secure victory at Marunath.

[Camp Activities menu options follow below...]
```

---

### News Prioritization & Display Duration

**Priority System:**
- **Important news** (wars, major battles, sieges, executions, army movements): shows for **2 days minimum**
- **Minor news** (village raids, prisoner events, player battle participation): shows for **1 day minimum**
- News items are **"sticky"** - once displayed, they stay visible for their full duration even if new news arrives
- After minimum duration expires, news ages out naturally as newer items appear

**Display Selection Logic:**
1. Already-shown items within their minimum display window (sticky)
2. Higher importance items (2-day news over 1-day news)
3. Most recent items among same priority

---

### News Categories

**Kingdom News Reports:**
- Wars and diplomacy (declarations, peace, alliances)
- Major battles (kingdom vs kingdom clashes)
- Settlements (sieges, captures, ownership changes)
- Lord fates (captures, executions, escapes)
- Politics (clan defections, succession, fief grants)

**Personal News Reports:**
- Personal participation (your party's role in battles/sieges)
- Your lord's army (forming, dispersing, moving)
- Your unit (retinue casualties, camp conditions)
- Direct orders (reassignments, special duties)
- Army-local events (skirmishes, foraging, discipline)

---

### Anti-Spam Rules

**Kingdom News:**
- Only generate while enlisted
- Only include items where involved heroes/parties belong to your lord's kingdom
- **Filters out bandit/looter battles** - only kingdom vs kingdom conflicts
- Dedupe by `StoryKey` - updates existing items instead of duplicates
- Display shows top 3 items (configurable)
- Feed maintains max 60 items in history (trimmed automatically)

**Personal News:**
- Only generate while enlisted
- Focus on player party, player lord, and player's army exclusively
- **Filters out bandit/looter battles** - only significant kingdom battles
- Display shows top 2 items (configurable)
- Feed maintains max 20 items in history (trimmed automatically)
- Army movement detection uses day-to-day comparison to avoid duplicate messages

---

### Localization (Current Implementation)

**Known Issue:**
Bannerlord's `MBTextManager.GetLocalizedText()` has special behavior for English that prevents XML-based localization from working correctly. For English language, it immediately returns fallback text without checking the translation database.

**Current Workaround:**
The system uses **hardcoded text templates** in C# code:
- Templates defined in `GetNewsTemplate()` method in `EnlistedNewsBehavior.cs`
- Placeholders (`{WINNER}`, `{LOSER}`, `{PLACE}`, etc.) processed correctly via `TextObject.SetTextVariable()`
- XML localization strings remain in `enlisted_strings.xml` for future use

**Core Placeholder Dictionary:**
- **Hero/lord:** `{LORD}`, `{WINNER}`, `{LOSER}`, `{CAPTOR}`, `{EXECUTOR}`, `{LEADER}`
- **Kingdom/faction:** `{KINGDOM}`, `{KINGDOM_A}`, `{KINGDOM_B}`, `{CLAN}`, `{FACTION}`
- **Location:** `{SETTLEMENT}`, `{PLACE}`, `{REGION}`, `{ROAD}`, `{TOWN}`, `{CASTLE}`, `{VILLAGE}`
- **Military:** `{ARMY}`, `{PARTIES}`, `{MEN}`, `{COUNT}`, `{LOSSES}`
- **Time:** `{DAY}`, `{DAYS}`, `{AGE}`
- **Culture:** `{CULTURE}`

---

### Battle Classification

**Winner Determination:**
- Uses `mapEvent.HasWinner` + winner side/result flags
- If no winner: classify as "inconclusive" / "contact broken"

**Loss Rate Calculation:**
```
lossRate = losses / max(1, initialStrength)
```

**Classification Labels:**
- **Clean victory:** winner lossRate < 0.20
- **Costly victory:** winner lossRate 0.20–0.45
- **Pyrrhic victory:** winner lossRate ≥ 0.45 AND loser lossRate ≥ 0.55
- **Mutual ruin:** both sides lossRate ≥ 0.60

**Templates:**
- `"Victory: {WINNER} defeated {LOSER} near {PLACE}."`
- `"Costly victory: {WINNER} drove off {LOSER} near {PLACE}."`
- `"Pyrrhic victory: {WINNER} defeated {LOSER} near {PLACE}."`
- `"Bloodbath near {PLACE}: both sides were mauled."`

---

## Technical Reference

### UI Architecture Philosophy

**1. Two UI Technologies:**
- **Text Menus (GameMenu):** Simple, stable, vanilla-friendly
- **Gauntlet Screens (ScreenBase):** Rich, visual, custom overlays

**2. When to Use Each:**

**Use Text Menus when:**
- Navigation/hub systems
- Lists of options
- Simple status displays
- Vanilla-style interactions

**Use Gauntlet Screens when:**
- Complex layouts needed
- Visual presentation critical
- Interactive elements (drag/drop, tabs)
- Real-time data updates

**3. Integration Points:**
- Text menus can launch Gauntlet screens
- Gauntlet screens can return to text menus
- Both can display same data sources
- Keep logic in Behaviors, not in UI

**4. Modern Styling:**
All UI uses consistent modern styling:
- Icons via `LeaveType` (text menus) or sprites (Gauntlet)
- Culture-appropriate backgrounds
- Hover tooltips
- Color-coded indicators
- Professional typography

---

### Performance Considerations

**Text Menus:**
- Lightweight by design
- No rendering overhead
- Fast menu switching
- Text variable updates are cheap

**Gauntlet Screens:**
- Heavier than text menus
- Load sprite categories explicitly
- Dispose resources properly
- Minimize redraws
- Use lazy loading for portraits

**News System:**
- Event-driven (not tick-based)
- Bounded history (60 kingdom, 20 personal)
- Priority backlog system
- Read-only (no world-state writes)

---

### GameMenu Text Width (No-Scroll Rule)

Bannerlord's GameMenu UI wraps text based on UI Scale and fixed layout:

- **Do not rely on long separator lines** (ASCII/Unicode). They can wrap and render as broken fragments.
- Keep the **main menu report excerpt short** (prefer single paragraph, capped length).
- **Avoid heavy indentation**; it increases wrapping risk in proportional fonts.
- If a line can be long, **truncate** with `...` to keep menu within visible area (no scrolling).

---

### Time Control Preservation

**Problem:** Vanilla `GameMenu.ActivateGameMenu()` and `SwitchToMenu()` force time to `Stop`, then wait menus call `StartWait()` which sets `UnstoppableFastForward`. This overrides player's time preference.

**Solution:** Handle time mode conversion once in menu init, never in tick handlers:

1. **In Menu Init:** Call `StartWait()`, then convert unstoppable modes to stoppable equivalents
2. **Unlock Time Control:** Call `SetTimeControlModeLock(false)` to allow player speed changes
3. **No Tick Restoration:** Tick handlers must NOT restore time mode - this fights with user input

**Why No Tick Restoration:** For army members, native code uses `UnstoppableFastForward` when user clicks fast forward. If tick handlers restore `CapturedTimeMode` whenever they see `UnstoppableFastForward`, the next tick immediately reverts user input, breaking speed controls.

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

---

## Bannerlord API Reference

### Menu Types

**MenuAndOptionType Values:**

| Type | Value | UI Widgets | Time Controls | Usage |
|------|-------|------------|---------------|-------|
| `RegularMenuOption` | 0 | None | Pauses game | Basic text menus |
| `WaitMenuShowProgressAndHoursOption` | 1 | Progress bar + hours | Works | Time-based activities |
| `WaitMenuShowOnlyProgressOption` | 2 | Progress only | Works | Menus with progress display |
| `WaitMenuHideProgressAndHoursOption` | 3 | Clean text only | Works | Army wait, settlement wait |

**Recommended:** Use `WaitMenuHideProgressAndHoursOption` for clean menus without progress widgets.

---

### Time Control Modes

| Mode | Description |
|------|-------------|
| `Stop` | Game paused |
| `StoppablePlay` | Normal speed, can be paused |
| `StoppableFastForward` | Fast speed, can be paused |
| `UnstoppablePlay` | Normal speed, cannot be paused |
| `UnstoppableFastForward` | Fast speed, forced by StartWait() |
| `UnstoppableFastForwardForPartyWaitTime` | Party wait variant |

---

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

---

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

---

### Text Variables

```csharp
// Get menu text and set variables (in init or tick handler)
var text = args.MenuContext.GameMenu.GetText();
text.SetTextVariable("PARTY_LEADER", lordName);
text.SetTextVariable("PARTY_TEXT", statusContent);

// Global text variables
MBTextManager.SetTextVariable("VARIABLE_NAME", value, false);
```

---

### Menu Navigation

```csharp
GameMenu.SwitchToMenu("target_menu_id");  // Switch between menus
GameMenu.ExitToLast();                     // Return to previous menu
args.MenuContext.GameMenu.EndWait();       // End wait menu properly
```

---

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

---

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

---

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

---

## Debugging

### Log Categories

- `"Interface"` - Menu system activity
- `"Menu"` - Menu navigation and state
- `"Dialog"` - Dialog registration and conversation flow
- `"CommandTent"` - General system activity
- `"Retinue"` - Soldier management
- `"Upkeep"` - Daily gold deduction
- `"Trickle"` - Free replenishment
- `"Requisition"` - Instant replenishment
- `"CasualtyTracker"` - Battle casualties and wounded sync

---

### Key Log Points

```csharp
// Menu activation
ModLogger.Info("Interface", $"Activating menu: {menuId}");
ModLogger.Debug("Menu", $"Menu state: duty={duty}");

// Selection changes
ModLogger.Info("Menu", $"Duty changed: {oldDuty} -> {newDuty}");

// Tier checks
ModLogger.Debug("Menu", $"Tier check: required={required}, current={current}, allowed={allowed}");

// Dialog registration
ModLogger.Info("Dialog", $"Registered enlistment dialog: {dialogId}");

// Conversation flow
ModLogger.Debug("Dialog", $"Player selected: {dialogLineId}");

// Service records
ModLogger.Debug("CommandTent", $"Service record updated for {factionId}");

// Retinue management
ModLogger.Info("Retinue", $"Spawning {count} {type} soldiers");
ModLogger.Debug("Retinue", $"Trickle skipped: already at capacity");

// Lifecycle
ModLogger.Info("Retinue", $"Cleared {count} retinue troops (reason: {reason})");
```

---

### Debug Output Location

- `Modules/Enlisted/Debugging/enlisted.log`

---

### Common Issues

**Duties greyed out unexpectedly:**
- Check formation compatibility (`IsDutyCompatibleWithFormation()`)
- Check tier requirement vs player's current tier
- Verify expansion is active for naval duties (War Sails)

**Current duty not showing:**
- Verify `CampDutiesVM.RefreshValues()` is called when tab opens
- Check duty persistence in `EnlistedDutiesBehavior.ActiveDuties`
- Ensure UI updates after duty assignment

**XP not applying:**
- Ensure selected duties connect to `EnlistedDutiesBehavior.AssignDuty()`
- Check daily tick is processing assignments
- Verify duty IDs match configuration

**Menu doesn't activate:**
- Check `NextFrameDispatcher` is not busy
- Verify encounter state allows menu activation
- Check for timing conflicts with game state transitions

**Dialog doesn't appear:**
- Check dialog conditions return true for current game state
- Verify dialog is registered in `OnSessionLaunched`
- Check for conflicting dialog IDs in other behaviors

**"Dialog already registered" error:**
- Another behavior is trying to use same dialog ID
- Ensure all dialogs registered in `EnlistedDialogManager` only
- Check for duplicate registrations

---

## Related Files

### Text Menu System
- `src/Features/Interface/Behaviors/EnlistedMenuBehavior.cs` - Main enlisted status menu
- `src/Features/Lances/Behaviors/EnlistedLanceMenuBehavior.cs` - My Lance menu
- `src/Features/Conditions/EnlistedMedicalMenuBehavior.cs` - Medical Attention menu
- `src/Features/Camp/CampMenuHandler.cs` - Camp menu and activities

### Modern Event UI
- `src/Features/Lances/UI/LanceLifeEventScreen.cs` - Main Gauntlet screen
- `src/Features/Lances/UI/ModernEventPresenter.cs` - Presentation logic
- `src/Features/Lances/UI/LanceLifeEventVM.cs` - ViewModel
- `src/Features/Lances/UI/LanceLifeEventScreenWidget.xml` - UI layout

### Camp Hub System
- `src/Features/CommandTent/Core/ServiceRecordManager.cs` - Faction record tracking
- `src/Features/CommandTent/Core/RetinueManager.cs` - Soldier management
- `src/Features/CommandTent/Core/RetinueLifecycleHandler.cs` - Event handling
- `src/Features/CommandTent/Core/CompanionAssignmentManager.cs` - Companion participation state
- `src/Features/CommandTent/UI/CommandTentMenuHandler.cs` - Menu integration

### Dialog System
- `src/Features/Conversations/Behaviors/EnlistedDialogManager.cs` - Centralized dialog manager

### Quartermaster System
- `src/Features/Equipment/Behaviors/QuartermasterManager.cs` - Equipment logic
- `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs` - Quartermaster Hero

### News System
- `src/Features/Interface/Behaviors/EnlistedNewsBehavior.cs` - News generation and display

### Core Systems
- `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs` - Core enlistment state
- `src/Features/Assignments/Behaviors/EnlistedDutiesBehavior.cs` - Duty system

---

## Related Documentation

### Core Features
- [Enlistment System](../Core/enlistment.md) - Service state management and tier progression
- [Lance Assignments](../Core/lance-assignments.md) - 9-tier progression and culture-specific ranks
- [Duties System](../Core/duties-system.md) - Duty definitions and XP
- [Pay System](../Core/pay-system.md) - Muster ledger, pay muster, discharge, pensions
- [Companion Management](../Core/companion-management.md) - Companion battle participation details

### Gameplay Features
- [Camp Life Simulation](../Gameplay/camp-life-simulation.md) - Camp conditions and integrations
- [Troop Selection (Legacy)](../Gameplay/troop-selection.md) - Old troop selection system

### Technical Guides
- [Gauntlet UI Playbook](../../research/gauntlet-ui-screens-playbook.md) - How to build Gauntlet screens safely

---

## Menu Backgrounds Reference

Enlisted's game-menus use **menu background meshes** (not `.png` images) via:

```csharp
args.MenuContext.SetBackgroundMeshName("<mesh_name>");
```

### Where This Mod Sets Menu Backgrounds

**Enlisted status / duty menus:**
- Choose the current lord/kingdom culture's encounter background mesh, with fallback
- **File:** `src/Features/Interface/Behaviors/EnlistedMenuBehavior.cs`
- **Fallback:** `encounter_looter`
- **Typical source:** `CultureObject.EncounterBackgroundMesh`

**Camp menus:**
- Fixed "meeting" background
- **File:** `src/Features/Camp/CampMenuHandler.cs`
- **Mesh:** `encounter_meeting`

### Culture Encounter Background Meshes (Vanilla)

From Bannerlord's culture XML (`Modules/SandBoxCore/ModuleData/spcultures.xml`):

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

### Other Background Meshes (Observed)

Used by native menu handlers:

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

### Notes

- **This repo does not ship menu background textures:** Only `preview.png` exists in-repo; menu backdrops are engine meshes
- **Mesh availability depends on game install / DLC:** If a mesh name is missing, it may render incorrectly or fall back
- **Culture-driven menus** will change depending on the lord/kingdom the player is serving

---

**Document Maintained By:** Enlisted Development Team  
**Last Updated:** December 18, 2025  
**Status:** Active - All Systems Implemented  
**File:** `docs/Features/UI/ui-systems-master.md`

