# Camp Life Simulation: Intelligent Living World

**Summary:** The Camp Life Simulation creates a living, breathing military camp that runs independently of player input. The orchestrator intelligently generates contextual opportunities based on world state, time, player condition, and recent history. Players are informed of what's happening and can choose to engage or take their own initiative.

**Status:** 📋 Specification  
**Priority:** Phase 6 (After Content Orchestrator Phases 1-5)  
**Last Updated:** 2025-12-29  
**Dependency:** Requires [Content Orchestrator](content-orchestrator-plan.md) to be implemented first  
**Related Docs:** [Content Orchestrator Plan](content-orchestrator-plan.md), [Current Camp Life](../Features/Campaign/camp-life-simulation.md), [UI Systems Master](../Features/UI/ui-systems-master.md), [Progression System Schema](../Features/Content/event-system-schemas.md#progression-system-schema-future-foundation)

---

## Index

1. [Core Vision](#core-vision)
2. [Relationship to Existing Systems](#relationship-to-existing-systems)
3. [Intelligence Layers](#intelligence-layers)
4. [Camp Opportunity Generation](#camp-opportunity-generation)
5. [Player Information & Interaction](#player-information--interaction)
6. [Context-Aware Scheduling](#context-aware-scheduling)
7. [Learning System](#learning-system)
8. [Integration with Orders](#integration-with-orders)
9. [Technical Architecture](#technical-architecture)
10. [Implementation Tasks](#implementation-tasks)
11. [Edge Cases & Special States](#edge-cases--special-states)
12. [Examples](#examples)
13. [Future Enhancements](#future-enhancements)

---

## Core Vision

### The Philosophy

**Camp life happens whether you engage or not.** The camp is a living entity with its own rhythms, schedules, and activities. You are one soldier in a larger world. Things occur around you. You can participate, or you can watch. Either is valid.

**Key Principles:**
- Camp runs automatically based on intelligent simulation
- Player receives information about what's happening
- Player chooses when and how to engage
- Nothing forces player interaction
- Camp feels alive, responsive, and realistic

---

### The Dual-Track System

**Track 1: Living Camp (Orchestrator-Generated)**
- Camp generates its own opportunities
- Based on world state, time, context, player state
- Appears in "CAMP LIFE" section when Camp Hub opened
- 0-3 opportunities at any given time
- Changes based on what's realistic right now

**Track 2: Player Initiative (Category Selection)**
- Player can ignore camp life and take own action
- Browse categories for specific intent
- Orchestrator picks contextually appropriate decision
- Always available as backup option

**Both tracks use the same orchestrator intelligence** - just different presentation.

---

## Relationship to Existing Systems

This feature **extends** existing systems rather than replacing them:

### CampLifeBehavior (Existing - Keep As-Is)
**Location:** `src/Features/Camp/CampLifeBehavior.cs`

The existing CampLifeBehavior continues to:
- Track backend stress meters (LogisticsStrain, MoraleShock, PayTension, TerritoryPressure)
- Derive QuartermasterMoodTier for dynamic pricing
- Respond to campaign events (battles, village raids, resupply)

This remains the **backend simulation layer** for Quartermaster mood.

### Content Orchestrator (Required Dependency)
**Location:** `src/Features/Content/ContentOrchestrator.cs` (created in Phases 1-5)

The Content Orchestrator provides:
- `WorldStateAnalyzer` - analyzes lord situation, war stance, activity level
- `SimulationPressureCalculator` - calculates pressure from company/player state
- `PlayerBehaviorTracker` - learns player preferences
- World-state-driven frequency calculations

Camp Life Simulation uses these components directly.

### New: CampOpportunityGenerator
**Location:** `src/Features/Camp/CampOpportunityGenerator.cs` (created in Phase 6)

New class that:
- Queries ContentOrchestrator for world state
- Tracks camp context (mood, time, recent events)
- Generates candidate opportunities based on what's happening
- Scores opportunities for fitness
- Returns 0-3 opportunities for display

---

## Intelligence Layers

### The Orchestrator Thinks In Layers

When generating camp opportunities, the system analyzes:

```
Layer 1: World State (Macro Context)
  ├─ Lord situation (garrison, campaign, siege)
  ├─ Kingdom war status (peace, active war, desperate)
  ├─ Recent world events (battles, sieges)
  └─ Strategic context (offensive, defensive, recovery)

Layer 2: Camp Context (Meso Context)
  ├─ Day Phase (Dawn, Midday, Dusk, Night) - synced with Order System
  ├─ Day of week (pay coming? muster soon?)
  ├─ Recent camp activity (training yesterday? fight last night?)
  ├─ Current orders status (who's on duty? who's off?)
  └─ Camp mood (celebration? mourning? tense?)

Layer 3: Player State (Micro Context)
  ├─ Physical condition (fatigue, injury, health)
  ├─ Economic state (gold, debts, needs)
  ├─ Social standing (reputation levels)
  ├─ Recent actions (what did player do yesterday?)
  └─ Pending matters (owe debt? promise made? chain events?)

Layer 4: Opportunity History (Meta Context)
  ├─ When was this type last offered?
  ├─ Did player engage or ignore it?
  ├─ What does player tend to choose?
  ├─ Variety maintenance (don't repeat too much)
  └─ Novelty injection (introduce new things periodically)
```

**Result:** Camp opportunities that feel natural, varied, and contextually appropriate.

---

## Camp Opportunity Generation

### The Core Algorithm

```csharp
public List<CampOpportunity> GenerateCampLife()
{
    // Step 0: Check if player can receive opportunities
    if (IsPlayerOnDuty())
        return new List<CampOpportunity>(); // On duty, no opportunities
    
    // Step 1: Analyze all context layers
    var worldState = ContentOrchestrator.Instance.GetCurrentWorldSituation();
    var campContext = AnalyzeCampContext();
    var playerState = AnalyzePlayerState();
    var history = GetOpportunityHistory();
    
    // Step 2: Determine opportunity budget
    int maxOpportunities = DetermineOpportunityBudget(worldState, campContext);
    // Garrison morning: 2-3 opportunities
    // Siege afternoon: 0-1 opportunities
    // Campaign evening: 1-2 opportunities
    
    // Step 3: Generate candidate opportunities
    var candidates = GenerateCandidates(worldState, campContext, playerState);
    
    // Step 4: Score each candidate
    foreach (var candidate in candidates)
    {
        candidate.Score = CalculateFitness(candidate, worldState, campContext, 
                                           playerState, history);
    }
    
    // Step 5: Select best fitting opportunities
    var selected = SelectTopN(candidates, maxOpportunities);
    
    // Step 6: Record what was presented
    RecordOpportunityPresentation(selected);
    
    return selected;
}
```

---

### Opportunity Types

**Training Opportunities:**
- Veterans drilling
- Sergeant running combat practice
- Weapon maintenance session
- Formation practice
- Physical conditioning group

**Social Opportunities:**
- Card game starting
- Men gathering around fire
- Someone telling war stories
- Comrade needs help
- Shared meal time

**Economic Opportunities:**
- Quartermaster needs help
- Smithy looking for labor
- Someone selling something
- Bet being offered
- Work detail forming

**Recovery Opportunities:**
- Medical tent open
- Quiet spot available
- Hot meal being served
- Time to rest before next duty

**Special/Situational:**
- Lord is holding audience
- Prisoner exchange happening
- Battlefield work available (after battle)
- Town leave possible (at settlement)
- Baggage train arrived

---

### Fitness Scoring (Intelligence)

**Each opportunity gets scored 0-100:**

```csharp
float CalculateFitness(CampOpportunity opp, WorldSituation world, 
                       CampContext camp, PlayerState player, OpportunityHistory hist)
{
    float score = 50f; // Base score
    
    // World State Modifiers
    if (opp.Type == OpportunityType.Training && world.LordSituation == LordSituation.InGarrison)
        score += 15; // Training fits garrison
    if (opp.Type == OpportunityType.Social && world.LordSituation == LordSituation.InSiege)
        score -= 20; // Frivolous during siege
    if (opp.Type == OpportunityType.Recovery && world.ExpectedActivity == ActivityLevel.Intense)
        score += 25; // Recovery valuable during intense periods
    
    // Day Phase Modifiers (synced with Order System phases)
    if (opp.Type == OpportunityType.Training && camp.DayPhase == DayPhase.Dawn)
        score += 10; // Dawn is for training (Order Phase 1)
    if (opp.Type == OpportunityType.Social && camp.DayPhase == DayPhase.Dusk)
        score += 15; // Dusk/evening is for socializing (Order Phase 3)
    if (opp.Type == OpportunityType.Economic && camp.DayPhase == DayPhase.Night)
        score -= 30; // Trade at night is odd (Order Phase 4)
    
    // Player State Modifiers
    if (opp.Type == OpportunityType.Training && player.Fatigue > 70)
        score -= 25; // Too tired to train
    if (opp.Type == OpportunityType.Recovery && player.IsInjured)
        score += 30; // Recovery critical when injured
    if (opp.Type == OpportunityType.Economic && player.Gold < 50)
        score += 20; // Economic opportunities valuable when poor
    
    // History Modifiers (Variety & Learning)
    if (hist.LastPresentedHoursAgo(opp.Type) < 12)
        score -= 40; // Don't repeat too soon
    if (hist.PlayerEngagementRate(opp.Type) > 0.7f)
        score += 15; // Player likes this type
    if (hist.PlayerEngagementRate(opp.Type) < 0.2f)
        score -= 10; // Player tends to ignore this
    if (hist.TimesSeen(opp.Type) == 0)
        score += 5; // Small novelty bonus
    
    // Clamp to 0-100
    return Math.Clamp(score, 0f, 100f);
}
```

**Opportunities below threshold (40) are not shown.**

---

### Opportunity Budget (How Many?)

**The system intelligently decides how many opportunities to show:**

```
Garrison + Morning (Phase 1) + Off-Duty:
  → 2-3 opportunities (camp is active, lots happening)

Siege + Afternoon + Off-Duty:
  → 0-1 opportunities (camp is tense, focused, less leisure)

Campaign + Evening + Off-Duty:
  → 1-2 opportunities (some downtime, but still busy)

Any + Any + On-Duty:
  → 0 opportunities (player is busy with order)

Recent Event (< 2 hours):
  → -1 to budget (just had something, quiet now)

Nothing in 2 days:
  → +1 to budget (inject activity)
```

**Result:** Camp feels realistically busy or quiet based on context.

---

## Player Information & Interaction

### Menu Structure

Two menus work together:

| Menu | Access | Purpose |
|------|--------|---------|
| **Main Menu** | `enlisted_status` (auto-opens when waiting) | Quick info + navigation to Orders/Decisions/Camp |
| **Camp Hub** | Click [CAMP] from Main Menu | Deep interaction (QM, Records, Companions, etc.) |

---

### Main Menu Layout (Quick Decision Center)

```
╔══════════════════════════════════════════════════════════╗
║  ENLISTED STATUS                                          ║
╠══════════════════════════════════════════════════════════╣
║                                                           ║
║  _____ KINGDOM _____                                      ║
║  Vlandia at war with Battania. Siege at Jaculan.         ║
║                                                           ║
║  _____ CAMP _____                                         ║
║  Evening calm. Good spirits in camp.                     ║
║  Veterans drilling by the wagons.                        ║
║  Card game forming tonight by the fire.                  ║
║  The {NCO_TITLE}'s making lists - duty roster tomorrow.  ║
║                                                           ║
║  _____ YOU _____                                          ║
║  You're off duty and well-rested. Guard duty scheduled   ║
║  for tomorrow at dawn. You've agreed to join the card    ║
║  game tonight.                                            ║
║                                                           ║
╠══════════════════════════════════════════════════════════╣
║                                                           ║
║      [  ORDERS  ]                                         ║
║                                                           ║
║      [  DECISIONS  ]                                      ║
║                                                           ║
║      [  CAMP  ]                                           ║
║                                                           ║
║      [Back to Map]                                        ║
║                                                           ║
╚══════════════════════════════════════════════════════════╝
```

**Three information sections (natural flowing text, cached):**

| Section | Scope | Example Content |
|---------|-------|-----------------|
| **KINGDOM** | What's happening in the realm | Wars, sieges, peace treaties, major battles |
| **CAMP** | What's happening around you in camp | Activities forming, morale, events, living world |
| **YOU** | What's happening to YOU personally | Your duty schedule, health, commitments, recent actions |

**CAMP = the living world around you (exists whether you engage or not)**
**YOU = your place in that world (your status, your schedule, your choices)**

### YOU Section Examples

The YOU section is natural flowing text that updates with your personal situation:

**Off duty, healthy, nothing scheduled:**
```
You're off duty and well-rested. The day stretches ahead.
```

**Scheduled for duty tomorrow:**
```
You're off duty and well-rested. Guard duty scheduled for tomorrow at dawn.
```

**Made a commitment:**
```
You're off duty. Guard duty tomorrow at dawn. You've agreed to join the card game tonight.
```

**On duty:**
```
On duty - Guard Post, day 1 of 2. Dawn watch complete, all quiet. Midday checkpoint in 5 hours.
```

**Wounded:**
```
You're wounded - movement impaired. Off duty until you recover. The medic says rest for a few days.
```

**Saw combat today:**
```
You saw heavy combat today. Exhausted but uninjured. Off duty - the {NCO_TITLE} is giving everyone rest.
```

**Sick:**
```
You're feeling poorly. The medic suspects camp fever. Rest ordered until you're fit for duty.
```

**Risk situation (committed to activity but has duty):**
```
On duty - Guard Post. You agreed to the dice game tonight. Risky - you'll need to slip away from your post.
```

**Three navigation buttons (open separate menus):**
- **[ORDERS]** - Opens orders menu (see pending, accept/decline, view active)
- **[DECISIONS]** - Opens camp activities menu (join training, card game, etc.)
- **[CAMP]** - Opens deep Camp Hub (QM, Records, Companions, Medical)

### Player Commits to Activity

When player clicks to join an activity (e.g., card game tonight):

1. **YOU section updates immediately:**
```
You're off duty. Guard duty tomorrow. You've agreed to join the card game tonight.
```

2. **DECISIONS menu removes/greys that option:**
```
╔══════════════════════════════════════════════════════════╗
║  CAMP ACTIVITIES                                          ║
╠══════════════════════════════════════════════════════════╣
║                                                           ║
║  Veterans drilling by the wagons...                      ║
║      [Join the drill]                                     ║
║                                                           ║
║  Card game forming tonight by the fire.                  ║
║      [Attending tonight]  ← GREYED/DISABLED               ║
║                                                           ║
╚══════════════════════════════════════════════════════════╝
```

3. **Activity fires at scheduled time** (e.g., evening tick):
   - Player gets the event popup or decision fires
   - If player is on duty at that time, detection check happens
   - YOU section updates: "At the card game. Guard duty in 2 hours..."

4. **After activity completes:**
   - YOU section clears commitment
   - Activity removed from DECISIONS (cooldown starts)
   - Next opportunity cycle generates new options

**Commitment tracking:**
```csharp
public class PlayerCommitments
{
    public string? ScheduledActivityId { get; set; }
    public CampaignTime? ScheduledTime { get; set; }
    
    public bool HasCommitment => ScheduledActivityId != null;
    
    public void CommitTo(string activityId, CampaignTime when)
    {
        ScheduledActivityId = activityId;
        ScheduledTime = when;
        RefreshYouSection(); // Update player status immediately
    }
    
    public void ClearCommitment()
    {
        ScheduledActivityId = null;
        ScheduledTime = null;
    }
}
```

### Info Section Caching

Sections are **cached and stable** - they don't flicker on every menu open:

| Section | Scope | Refreshes When | Stable For |
|---------|-------|----------------|------------|
| **KINGDOM** | Realm news | War/peace, siege start/end, major battle | Days |
| **CAMP** | Camp activity | Day phase changes (Dawn/Midday/Dusk/Night) | Per phase (4x daily) |
| **YOU** | Personal status | State changes (duty, health, commitments) | Until state changes |

**Refresh Triggers:**
- KINGDOM: War declared, peace made, siege starts/ends, lord captured
- CAMP: Day phase changes (Dawn→Midday→Dusk→Night), major camp event, activity starts/ends
- YOU: Duty status changes, health changes, player makes commitment, order scheduled

**Highlight changes:**
```
_____ CAMP _____
Evening calm. Dice game by the fire.
[NEW] The {NCO_TITLE}'s preparing duty roster - orders tomorrow.
```

Use `<span style="Warning">[NEW]</span>` for changed content to draw attention without constant flicker.

### Color Scheme (Use Existing)

All text uses the existing Enlisted color scheme (see `docs/Features/UI/color-scheme.md`):

| Element | Style | Color | Example |
|---------|-------|-------|---------|
| Section headers | `Header` | Cyan | `<span style="Header">_____ KINGDOM _____</span>` |
| Labels | `Label` | Teal | `<span style="Label">NOW:</span>` |
| Good status | `Success` | Green | Supplies well-stocked |
| Warning status | `Warning` | Gold | Fair conditions |
| Critical status | `Alert` | Red | Low supplies, wounded |
| Body text | `Default` | Cream | Standard narrative text |
| Collapsible headers | `Link` | Native | `<span style="Link">ORDERS</span>` |

**Implementation:**
```csharp
// Section header
sb.AppendLine("<span style=\"Header\">_____ KINGDOM _____</span>");

// Label with value
sb.AppendLine($"<span style=\"Label\">NOW:</span> {nowText}");

// Status-colored value
var stateColor = isWounded ? "Alert" : isTired ? "Warning" : "Default";
sb.AppendLine($"<span style=\"{stateColor}\">{stateDescription}</span>");
```

**Brush file:** `GUI/Brushes/EnlistedColors.xml`

---

### The "YOU" Section: NOW + AHEAD

**NOW** shows current state:
- Duty status (on order, off duty)
- Physical state (rested, tired, wounded)
- Recent notable action

**AHEAD** shows forecast (culture-aware, context-aware):
- When off duty: Hints about incoming orders, camp events, pending matters
- When on order: What's coming DURING that order (checkpoints, complications)
- Uses culture-appropriate rank names: `{NCO_TITLE}`, `{OFFICER_TITLE}`

**Forecast Examples:**

| Signal | What It Foreshadows |
|--------|---------------------|
| "{NCO_TITLE}'s been making lists." | Order coming in 12-24 hours |
| "The men are planning something." | Social event tomorrow |
| "{OFFICER_TITLE} looks worried." | Supply issues brewing |
| "Scouts returned with grim faces." | Battle likely soon |
| "Pay day approaches." | Muster in 2-3 days |
| "Quiet. Almost too quiet." | Nothing imminent |

---

### DECISIONS Menu (Camp Life Activities)

When player clicks [DECISIONS], it expands to show orchestrator-curated opportunities:

**Off Duty:**
```
[  DECISIONS  ] ▼

    Veterans are drilling by the wagons. The {NCO_TITLE}
    is putting them through their paces.
        [Join the drill]
    
    A card game is forming by the fire. Stakes look good.
        [Sit in]
    
    The gates of Pravend stand open. The town awaits.
        [Enter Pravend]
```

**On Duty (orchestrator filters to relevant options):**
```
[  DECISIONS  ] ▼

    The soldier next to you looks bored.
        [Strike up conversation]
    
    Laughter drifts from the card game...
        [Sneak away]
```

**Risky options show consequences via TOOLTIP on hover:**
```
┌─────────────────────────────────────────┐
│ Risk: You're on guard duty.            │
│ If caught: -15 Officer Rep             │
│ Detection chance: ~25%                  │
└─────────────────────────────────────────┘
```

**No SAFE/RISKY categories in UI.** The orchestrator curates what to show. Tooltips explain consequences. Player sees natural opportunities, not risk buckets.

**Orchestrator filtering logic:**
- Day phase (Dawn/Midday/Dusk/Night - syncs with Order phases)
- World state (garrison, campaign, siege)
- Player condition (fatigue, injuries)
- Current duty status (on order vs off duty)
- What's actually happening in camp
- Player preferences (learned over time)
- Location (at settlement = "Enter {Settlement}" appears)

---

### ORDERS Menu (Military Activities)

When player clicks [ORDERS], it expands to show order status:

**No Active Orders (order scheduled for tomorrow):**
```
[  ORDERS  ] ▼

    No active orders.
    
    SCHEDULED:
    Guard Post - tomorrow at dawn
    (The {NCO_TITLE} has you down for watch duty.)
```

**Order Pending (needs response):**
```
[  ORDERS  ] ▼

    PENDING:
    Guard Post [NEW]
    "Report to the eastern perimeter for watch duty."
    
        [Accept Order]
        [Decline]
```

**Order Active:**
```
[  ORDERS  ] ▼

    ACTIVE:
    Guard Post (Day 1/2)
    Standing watch at the eastern perimeter.
    
    Current: Dawn watch - all quiet.
    Next: Midday checkpoint in 5 hours.
    
    SCHEDULED:
    Patrol Duty - in 3 days
```

### Order State Flow

Orders progress through states with player visibility at each stage:

```
FORECAST → SCHEDULED → PENDING → ACTIVE → COMPLETE
(CAMP/YOU)  (ORDERS)   (ORDERS)  (ORDERS)  (ORDERS)
```

| State | Where Shown | Player Action |
|-------|-------------|---------------|
| **Forecast** | CAMP section + YOU AHEAD | See it coming (12-24h before) |
| **Scheduled** | ORDERS (grayed) | Preview (8-18h before) |
| **Pending** | ORDERS [NEW] | Accept or Decline |
| **Active** | ORDERS + YOU NOW | Track progress |
| **Complete** | ORDERS (brief summary) | View rewards, auto-clears |

**Forecast signals appear in CAMP section:**
```
_____ CAMP _____
Evening calm. Dice game by the fire.
The {NCO_TITLE}'s making lists - guard duty by morning.
```

**Then moves to SCHEDULED in ORDERS 8-18 hours before activation.**

---

### CAMP Menu (Deep Interaction)

When player clicks [CAMP], they enter the full Camp Hub:

```
╔══════════════════════════════════════════════════════════╗
║  CAMP HUB                                                 ║
║  {RANK_NAME} ({TIER}) • Day {X} of 12 • {LOCATION}       ║
╠══════════════════════════════════════════════════════════╣
║                                                           ║
║  _____ CAMP STATUS _____                                  ║
║  ⚙️ {RHYTHM} - {ACTIVITY_LEVEL}                          ║
║                                                           ║
║  {Camp situation narrative. What's happening here.}      ║
║  {Supply state, morale, company condition.}              ║
║  {Recent camp events. What just happened.}               ║
║                                                           ║
║  _____ RECENT ACTIONS _____                               ║
║  • {Event/order outcome 1}                                ║
║  • {Event/order outcome 2}                                ║
║                                                           ║
╠══════════════════════════════════════════════════════════╣
║                                                           ║
║  [Service Records]                                        ║
║  [Quartermaster]                                          ║
║  [Personal Retinue]        ← T7+ only                     ║
║  [Companion Assignments]                                  ║
║  [Medical Attention]       ← If injured/ill               ║
║  [Talk to Lords]           ← If lords nearby              ║
║  [Access Baggage Train]                                   ║
║                                                           ║
╠══════════════════════════════════════════════════════════╣
║            [Back]                                         ║
╚══════════════════════════════════════════════════════════╝
```

**Visit Settlement** appears in DECISIONS menu (not Camp Hub) when at a settlement - it's a contextual opportunity, not a static button.

**Camp Hub focuses on CAMP, not kingdom.**
- CAMP STATUS: Military rhythm, activity level, camp-specific situation
- RECENT ACTIONS: **Full detail** - order outcomes, event results, reputation changes (merged from old Reports)
- No kingdom news (that's on Main Menu)
- No Leave Service (only accessible from Muster menu)
- No Reports menu (CAMP STATUS + RECENT ACTIONS replaces it)

**RECENT ACTIONS shows (last 5 days):**
```
_____ RECENT ACTIONS _____
• Guard Duty (yesterday)
  Completed without incident. +15 discipline XP.
• Soldier reputation +5 (2 days ago)
  Helped veteran repair armor.
• Battle at Pen Cannoc (3 days ago)
  Victory. Your lord's forces prevailed.
```

Uses existing `GetRecentOrderOutcomes()` and `GetRecentReputationChanges()` from `EnlistedNewsBehavior.cs`.

**This is for deep interaction** - browsing, equipment, records, companions. Player rank/tier/day info shown here.

---

### Information Presentation (Natural Language)

**BAD (Game-y):**
```
Training Opportunity Available
  [Start Training]
```

**GOOD (Immersive):**
```
Veterans are drilling by the wagons. The sergeant 
is putting them through their paces. Swords clash 
in rhythm.

   [Join the drill]
```

**Each opportunity includes:**
1. **What's happening** (2-3 sentences, scene-setting)
2. **Action verb** (Join, Help, Sit in, Check on, etc.)
3. **Implied consequence** (natural, no stat spoilers)

---

### Smart Information Filtering

**What player sees depends on context:**

**Garrison, Morning, Rested:**
```
CAMP LIFE:

Veterans are drilling by the wagons...
   [Join the drill]
   
The quartermaster's men are organizing supplies...
   [Help them]
```

**Garrison, Morning, Exhausted:**
```
CAMP LIFE:

You spot a quiet corner away from the bustle. 
A good place to rest undisturbed.
   [Rest there]
   
(Training opportunities filtered out - too tired)
```

**Siege, Evening, Exhausted:**
```
CAMP LIFE:

Someone found ale. The men are drinking. Tomorrow's 
assault weighs on everyone.
   [Join them]
   
(Only stress relief shown - everything else inappropriate)
```

---

## Context-Aware Scheduling

### Time of Day Cycles

**Morning (6am - 12pm):**
- **Activity Peak:** Training, work, productive tasks
- **Opportunity Budget:** High (2-3)
- **Mood:** Alert, energetic, purposeful
- **Typical:**
  - Morning muster (mandatory event)
  - Training opportunities common
  - Work details forming
  - Productive social (help comrades)

**Afternoon (12pm - 6pm):**
- **Activity Peak:** Duty assignments, orders issued
- **Opportunity Budget:** Low (0-1)
- **Mood:** Focused, business-like
- **Typical:**
  - Orders issued during this window
  - Fewer leisure opportunities
  - Meal time (social opportunity)
  - Preparation activities

**Evening (6pm - 12am):**
- **Activity Peak:** Social, leisure, recovery
- **Opportunity Budget:** Medium (1-2)
- **Mood:** Relaxed, social, tired
- **Typical:**
  - Card games, drinking, fire circles
  - Storytelling, bonding
  - Personal time activities
  - Light training or rest

**Night (12am - 6am):**
- **Activity Peak:** Sleep, guard duty, rare events
- **Opportunity Budget:** Very Low (0)
- **Mood:** Quiet, dark, dangerous
- **Typical:**
  - Most soldiers sleeping
  - Guard rotations (orders, not opportunities)
  - Rare special events (night incidents)
  - Emergency situations only

---

### Weekly Rhythm

**Days 1-4 (Early Week):**
- Fresh after muster/pay
- Higher energy
- More training opportunities
- More gambling/spending

**Days 5-8 (Mid Week):**
- Routine settling in
- Balanced opportunities
- Normal camp rhythm
- Work opportunities increase

**Days 9-12 (Muster Approaching):**
- Pay tension rising
- Economic opportunities prominent
- Preparation for muster
- Social opportunities about upcoming pay

**Muster Day (Every 12 Days):**
- Mandatory muster sequence
- No camp life opportunities (structured event)
- Resets weekly rhythm

---

### World State Schedules (4-Block System)

All schedules follow the **4-phase military day cycle**, synced with the Order System.

**Garrison Schedule (Structured):**
```
MORNING (6am-11am) - Order Phase 1
  → Training opportunities (drills, equipment)
  → Medical progression tick (6am)
  → New orders arrive
  → 2-3 opportunities available

DAY (12pm-5pm) - Order Phase 2
  → Work details, trade opportunities
  → Orders in ACTIVE state
  → 1-2 opportunities available

DUSK (6pm-9pm) - Order Phase 3
  → Social peak (gambling, drinking, stories)
  → Discipline progression tick (8pm)
  → 2-3 opportunities available

NIGHT (10pm-5am) - Order Phase 4
  → Night watch, limited activity
  → Orders complete
  → 0-1 opportunities available
```

**Campaign Schedule (Fluid):**
```
MORNING (6am-11am) - Order Phase 1
  → Break camp, begin march
  → Medical progression tick
  → Limited opportunities (0-1)

DAY (12pm-5pm) - Order Phase 2
  → Marching, midday halt
  → Orders in ACTIVE state
  → 0-1 opportunities (meal break)

DUSK (6pm-9pm) - Order Phase 3
  → Make camp, set pickets
  → Brief social time
  → 1-2 opportunities available

NIGHT (10pm-5am) - Order Phase 4
  → Sleep rotation
  → Orders complete
  → 0 opportunities (exhausted)
```

**Siege Schedule (Chaotic):**
```
MORNING (6am-11am) - Order Phase 1
  → Stand to, constant duty
  → Medical progression tick
  → 0-1 opportunities (stress-focused)

DAY (12pm-5pm) - Order Phase 2
  → Continuous duty rotation
  → No structured breaks
  → 0 opportunities

DUSK (6pm-9pm) - Order Phase 3
  → Brief respite if lucky
  → Discipline progression tick
  → 0-1 opportunities

NIGHT (10pm-5am) - Order Phase 4
  → Night watch, high alert
  → Orders complete
  → 0 opportunities (survival mode)
```

---

## Learning System

### Player Behavior Tracking

**What the system learns:**

```csharp
public class PlayerBehaviorProfile
{
    // Engagement patterns
    public Dictionary<string, float> OpportunityEngagementRate { get; set; }
    // training: 0.8 (engages 80% of time presented)
    // social: 0.3 (often ignores)
    // economic: 0.9 (always takes)
    
    // Choice patterns within opportunities
    public Dictionary<string, int> DecisionChoicePreferences { get; set; }
    // dec_weapon_drill_option_intense: 12 times chosen
    // dec_spar_option_friendly: 2 times chosen
    
    // Day phase patterns (syncs with Order System)
    public Dictionary<DayPhase, float> ActiveTimes { get; set; }
    // Morning: 0.9 (very active, Order Phase 1)
    // Dusk: 0.3 (less active, Order Phase 3)
    
    // Risk tolerance
    public float RiskPreference { get; set; } // 0.0 = safe, 1.0 = risky
    
    // Social preference
    public float SocialPreference { get; set; } // 0.0 = loner, 1.0 = social butterfly
}
```

**How learning affects opportunities:**

```
IF player frequently engages training (80%):
  → Show training opportunities more often
  → Prioritize training when multiple options
  
IF player rarely engages social (20%):
  → Show social opportunities less often
  → Don't waste slots on ignored content
  
IF player always picks risky gambling options:
  → Show higher-stakes gambling
  → Filter out low-stakes safe options
  
IF player helps comrades frequently:
  → Generate more help opportunities
  → Show social connections deepening
```

**Balance:** Learning influences but doesn't dominate. Variety and context still matter.

---

### Adaptive Difficulty

**The system adjusts to player skill:**

```
IF player succeeds at skill checks consistently:
  → Offer harder training opportunities
  → Increase stakes on risky options
  → Present leadership opportunities (higher tier feel)
  
IF player struggles with skill checks:
  → Offer basic/review training
  → Lower-stakes gambling
  → Supportive rather than challenging content
  
IF player is new (T1-T2):
  → Simple, explanatory opportunities
  → Lower consequences
  → Teaching moments
  
IF player is veteran (T7+):
  → Complex situations
  → Leadership scenarios
  → Higher stakes, bigger rewards
```

---

## Integration with Orders

### Order State Affects Camp Life

**When Player Has Active Order:**
```
CAMP LIFE:
  [Empty - you're on duty]
  
Order status shown in Recent Actions instead:
  • Guard Post: Evening watch (Phase 6/8)
```

**No camp opportunities during orders.** Player is busy.

**Note:** Check order state via OrderManager. If Order Progression System is implemented, use `OrderProgressionBehavior.Instance?.HasActiveOrder`. Otherwise use existing OrderManager mechanism.

---

**When Player Is Off-Duty:**
```
CAMP LIFE:

Veterans are drilling...
   [Join the drill]
   
Card game starting...
   [Sit in]
```

**Camp opportunities appear.** Player has free time.

---

### Order Issuance Integration

**Orders can be issued AS camp life:**

**Current (Popup):**
```
[Popup appears]
The sergeant finds you. "Guard post. Two days."
[Accept] [Decline]
```

**With Camp Life (Natural):**
```
CAMP LIFE:

The sergeant is looking for you. He has orders.
   [See what he wants]
   
   → Triggers order issuance event
```

**OR orders can interrupt camp life** (popup still works for urgent/immediate orders)

---

### Order Context Informs Camp Life

**After completing order:**
```
Just completed: Guard Duty (success)

CAMP LIFE:

The sergeant nods approvingly. "Good work out there."
Some of the veterans take notice.
   [Join them] → Social opportunity, bonus rep chance
```

**After failing order:**
```
Just completed: Patrol (failed - fell behind)

CAMP LIFE:

The camp feels tense. You avoid eye contact with 
the sergeant. The men give you space.
   [Find somewhere quiet] → Recovery opportunity only
   
(Social opportunities filtered out - shame context)
```

---

## Technical Architecture

### Core Components

```
CampOpportunityGenerator (new class)
├── GenerateCampLife()
│   ├── Uses ContentOrchestrator.GetCurrentWorldSituation()
│   ├── AnalyzeCampContext()
│   └── AnalyzePlayerState()
│
├── ContextAnalyzer
│   ├── AnalyzeCampContext() → CampContext
│   ├── GetDayPhase() → DayPhase  (synced with Order System)
│   └── GetCampMood() → CampMood
│
├── OpportunityScorer
│   ├── CalculateFitness()
│   └── DetermineOpportunityBudget()
│
└── OpportunityHistory
    ├── RecordOpportunityPresented()
    ├── RecordOpportunityEngaged()
    ├── RecordOpportunityIgnored()
    └── GetEngagementRate()
```

---

### Data Structures

**CampOpportunity:**
```csharp
public class CampOpportunity
{
    public string Id { get; set; }
    public OpportunityType Type { get; set; } // Training, Social, Economic, etc.
    public string DescriptionTextId { get; set; } // XML localization ID
    public string DescriptionFallback { get; set; } // Fallback text
    public string ActionVerb { get; set; } // "Join", "Help", "Sit in", etc.
    public string TargetDecisionId { get; set; } // Which decision this triggers
    public float FitnessScore { get; set; } // 0-100
    public List<string> RequiredFlags { get; set; }
    public List<string> BlockedByFlags { get; set; }
}

public enum OpportunityType
{
    Training,
    Social,
    Economic,
    Recovery,
    Special
}
```

**CampContext:**
```csharp
public class CampContext
{
    public DayPhase DayPhase { get; set; }  // Synced with Order System phases
    public int DaysSinceLastMuster { get; set; }
    public CampMood CurrentMood { get; set; } // Routine, Celebration, Mourning, Tense
    public int SoldiersOnDuty { get; set; }
    public int SoldiersOffDuty { get; set; }
    public List<RecentCampEvent> RecentEvents { get; set; }
}

// Matches Order System phases
public enum DayPhase
{
    Morning,    // 6am - 11am (Order Phase 1)
    Day,        // 12pm - 5pm (Order Phase 2)
    Dusk,       // 6pm - 9pm (Order Phase 3)
    Night       // 10pm - 5am (Order Phase 4)
}

// DEPRECATED: Use DayPhase instead
public enum TimeOfDay
{
    Dawn,       // 6am - 12pm (maps to DayPhase.Dawn)
    Afternoon,  // 12pm - 6pm
    Evening,    // 6pm - 12am
    Night       // 12am - 6am
}

public enum CampMood
{
    Routine,     // Normal operations
    Celebration, // After victory, pay day
    Mourning,    // After defeat, casualties
    Tense        // Before battle, siege, low morale
}
```

**OpportunityHistory:**
```csharp
public class OpportunityHistory
{
    public Dictionary<string, CampaignTime> LastPresented { get; set; }
    public Dictionary<string, int> TimesSeen { get; set; }
    public Dictionary<string, int> TimesEngaged { get; set; }
    public Dictionary<string, int> TimesIgnored { get; set; }
    
    public float GetEngagementRate(string opportunityType)
    {
        int seen = TimesSeen.GetValueOrDefault(opportunityType, 0);
        int engaged = TimesEngaged.GetValueOrDefault(opportunityType, 0);
        return seen > 0 ? (float)engaged / seen : 0.5f;
    }
    
    public float LastPresentedHoursAgo(string opportunityType)
    {
        if (!LastPresented.TryGetValue(opportunityType, out var time))
            return 999f;
        return (float)(CampaignTime.Now - time).ToHours;
    }
}
```

**MainMenuNewsCache (caching for stable UI):**
```csharp
public class MainMenuNewsCache
{
    private string _kingdomText;
    private float _kingdomCacheTime;
    
    private string _campText;
    private DayPhase _campCacheDayPhase;
    
    private string _youNowText;
    private string _youAheadText;
    private float _youCacheTime;
    
    public void RefreshIfNeeded()
    {
        // KINGDOM: Refresh on major events or once per day
        if (HasMajorKingdomEvent() || _kingdomCachePhase != DayPhase.Dawn && CurrentDayPhase() == DayPhase.Dawn)
            RefreshKingdom();
        
        // CAMP: Refresh when day phase changes (synced with Order phases)
        if (CurrentDayPhase() != _campCacheDayPhase)
            RefreshCamp();
        
        // YOU: Refresh when player state changes
        if (PlayerStateChanged())
            RefreshYou();
    }
    
    public string GetKingdomSection() => _kingdomText;
    public string GetCampSection() => _campText;
    public (string Now, string Ahead) GetYouSection() => (_youNowText, _youAheadText);
}
```

**Refresh Triggers:**

| Section | Triggers |
|---------|----------|
| KINGDOM | War declared/ended, siege start/end, major battle, lord captured, or 24h timeout |
| CAMP | Time period change (dawn/midday/dusk/night), major camp event, or 6h timeout |
| YOU | Duty status change, physical state change, new order scheduled, forecast change |

**Highlight new content:**
```csharp
// When content changed since last view
if (campNewsChanged)
    newContent = $"<span style=\"Warning\">[NEW]</span> {newContent}";
```

---

### Files to Create

| File | Purpose |
|------|---------|
| `src/Features/Camp/CampOpportunityGenerator.cs` | Main opportunity generation logic |
| `src/Features/Camp/Models/CampOpportunity.cs` | Opportunity data model |
| `src/Features/Camp/Models/OpportunityType.cs` | Type enum |
| `src/Features/Camp/Models/CampContext.cs` | Camp context tracking |
| `src/Features/Camp/Models/CampMood.cs` | Mood enum |
| `src/Features/Camp/Models/DayPhase.cs` | Day phase enum (syncs with Order phases) |
| `src/Features/Camp/Models/OpportunityHistory.cs` | Tracks what was shown when |
| `src/Features/Interface/MainMenuNewsCache.cs` | Cached news sections (KINGDOM/CAMP/YOU) |
| `src/Features/Interface/ForecastGenerator.cs` | Generates player status text |
| `src/Features/Camp/Models/PlayerCommitments.cs` | Tracks player's scheduled activities |
| `ModuleData/Enlisted/camp_opportunities.json` | Opportunity definitions (see schema below) |

**Schema Reference:** See [Event System Schemas](../Features/Content/event-system-schemas.md) for the full Camp Opportunities JSON schema and all required localization keys.

### Files to Modify

| File | Changes |
|------|---------|
| `src/Features/Interface/Behaviors/EnlistedMenuBehavior.cs` | Add CAMP LIFE section |
| `src/Features/Content/PlayerBehaviorTracker.cs` | Add opportunity tracking |
| `Enlisted.csproj` | Add new files |
| `ModuleData/Languages/enlisted_strings.xml` | Add opportunity strings |
| `src/Mod.Core/SaveSystem/EnlistedSaveDefiner.cs` | Register new types |

---

### Update Loop (4-Block System)

Camp life evaluates at **phase transitions**, not hourly. This syncs with the Order System.

```csharp
// Camp life updates when day phase changes (4 times per day)
private void OnDayPhaseChanged(DayPhase newPhase)
{
    if (!EnlistmentBehavior.Instance.IsEnlisted)
        return;
    
    // Invalidate cache - new phase means new opportunities
    _cachedOpportunities = null;
    _currentPhase = newPhase;
    
    // Log phase transition
    ModLogger.Log("CampLife", $"Phase changed to {newPhase}");
    
    // Trigger progression checks if applicable
    if (newPhase == DayPhase.Dawn)
        MedicalProgressionBehavior.Instance?.Tick();
    if (newPhase == DayPhase.Dusk)
        DisciplineProgressionBehavior.Instance?.Tick();
}

// When player opens Camp Hub or DECISIONS menu
public List<CampOpportunity> GetCurrentCampLife()
{
    // Check if on duty
    if (IsPlayerOnDuty())
        return new List<CampOpportunity>(); // Empty - on duty
    
    // Use cached if still same phase
    if (_cachedOpportunities != null && 
        ContentOrchestrator.GetCurrentDayPhase() == _currentPhase)
        return _cachedOpportunities;
    
    // Generate fresh opportunities for this phase
    _cachedOpportunities = GenerateCampLife(_currentPhase);
    
    return _cachedOpportunities;
}
```

**Phase Transition Timing:**
| Phase | Triggers At | What Happens |
|-------|-------------|--------------|
| Morning | 6am | Orders Phase 1, medical tick, new opportunities |
| Day | 12pm | Orders Phase 2, midday opportunities |
| Dusk | 6pm | Orders Phase 3, discipline tick, social opportunities |
| Night | 10pm | Orders Phase 4, limited opportunities |

---

## Implementation Tasks

### Phase 6A: Foundation (1-2 days)
**Goal:** Basic camp life generation without learning

**Tasks:**
1. Create all model classes (CampOpportunity, CampContext, OpportunityHistory, enums)
2. Create `CampOpportunityGenerator.cs` with basic generation
3. Implement `AnalyzeCampContext()` using CampaignTime
4. Add simple fitness scoring (world state + time only)
5. Create `camp_opportunities.json` with 15-20 initial opportunities
6. Add all files to Enlisted.csproj
7. Register types in EnlistedSaveDefiner

**Deliverables:**
- Camp opportunities generate based on context
- Simple context filtering works (garrison vs siege)

**Acceptance Criteria:**
- Garrison shows 2-3 opportunities during morning
- Siege shows 0-1 opportunities
- Day phase affects what appears (Dawn, Midday, Dusk, Night)

---

### Phase 6B: UI Integration (1-2 days)
**Goal:** Display opportunities in Camp Hub

**Tasks:**
1. Add `BuildCampLifeSection()` to EnlistedMenuBehavior
2. Add CAMP LIFE section to Camp Hub menu
3. Handle on-duty case ("You're on duty")
4. Handle no-opportunities case with context message
5. Add menu options that trigger opportunity decisions
6. Add localization strings

**Deliverables:**
- CAMP LIFE section visible in Camp Hub
- Clicking opportunity triggers correct decision

**Acceptance Criteria:**
- Opportunities display with natural language
- Action buttons work
- Empty states handled gracefully

---

### Phase 6C: Intelligence (1-2 days)
**Goal:** Add player state analysis and smart filtering

**Tasks:**
1. Implement player condition modifiers to scoring
2. Implement opportunity budget system
3. Add variety tracking (don't repeat too soon)
4. Implement OpportunityHistory persistence
5. Test across different player states

**Deliverables:**
- Opportunities respond to player fatigue, gold, injuries
- Budget adjusts based on recent activity
- Variety maintained over multiple days

**Acceptance Criteria:**
- Exhausted player sees rest opportunities prioritized
- Poor player sees economic opportunities more
- Same opportunity doesn't repeat within 12 hours

---

### Phase 6D: Learning (1-2 days)
**Goal:** Track player behavior and adapt

**Tasks:**
1. Extend PlayerBehaviorTracker with opportunity-specific tracking
2. Record opportunity presentations and engagement
3. Add learning modifiers to fitness scoring
4. Persist behavior profile in save system
5. Test learning over multiple sessions

**Deliverables:**
- System tracks what player engages with
- Opportunities adapt to player preferences
- Learning persists across save/load

**Acceptance Criteria:**
- Player who ignores social sees less social over time
- Player who loves training sees more training
- System maintains 70/30 split (learned vs variety)
- Behavior profile saves and loads correctly

---

### Phase 6E: Polish (1-2 days)
**Goal:** Natural language, edge cases, integration

**Tasks:**
1. Write contextual description text for all opportunities (25+)
2. Handle edge cases (no opportunities available, etc.)
3. Integrate with order issuance (natural presentation option)
4. Add admin commands for testing
5. Playtest and tune fitness scoring

**Deliverables:**
- All opportunities have immersive descriptions
- Edge cases handled gracefully
- Testing tools available

**Acceptance Criteria:**
- Descriptions feel natural and varied
- Empty camp life shows appropriate message
- Can force specific opportunities for testing

---

## Future Expansion: Progression Integration

**Status:** Schema Ready (Deferred Implementation)  
**Schema:** [Progression System Schema](../Features/Content/event-system-schemas.md#progression-system-schema-future-foundation)

Camp life decisions can trigger or modify **Progression System** tracks.

### How Camp Life Affects Progression

| Camp Activity | Progression Effect |
|---------------|-------------------|
| Rest decision | Sets `resting` context flag → +10% recovery, -10% worsen |
| Seek treatment | Sets `treated_today` flag → +25% recovery, -25% worsen |
| Training | May set `overexerted` flag → +5% worsen if injured |
| Night duty | Sets `sleep_deprived` → -10% recovery next day |
| Heavy drinking | Sets `hungover` → -15% recovery next day |

### Decision Effects on Progression

Camp decisions can set context flags that modify the next day's progression roll:

```json
{
  "id": "dec_rest_day",
  "options": [
    {
      "id": "full_rest",
      "setFlags": ["resting"],
      "effects": { "fatigue": -6 }
    }
  ]
}
```

The MedicalProgressionBehavior checks for `resting` flag:

```csharp
if (PlayerFlags.HasFlag("resting"))
{
    modifiers.ImproveBonus += config.ContextModifiers["resting"].Improve;
    modifiers.WorsenBonus += config.ContextModifiers["resting"].Worsen;
}
```

### Progression Affects Camp Decisions

The orchestrator considers progression state when selecting opportunities:

```csharp
// In FitnessCalculator
if (EscalationManager.Instance.State.MedicalRisk >= 2)
{
    // Boost recovery-related opportunities
    if (opportunity.Type == OpportunityType.Recovery)
        score += 30;
    
    // Reduce physically demanding opportunities
    if (opportunity.Type == OpportunityType.Training)
        score -= 40;
}
```

### Camp Life Decisions with Progression Consequences

Some camp decisions have probabilistic outcomes via progression:

```json
{
  "id": "dec_sparring_match",
  "options": [
    {
      "id": "fight",
      "setFlags": ["sparring_today"],
      "progressionRisk": {
        "track": "medical_risk",
        "chance": 15,
        "onRoll": +1
      }
    }
  ]
}
```

Instead of deterministic "+1 medical risk", there's a 15% chance of injury.

### Implementation Order

This is a **future feature**. After the Progression System (Phase 8) is implemented:

1. Add `setFlags` field to decision options
2. Add `progressionRisk` field to decision options
3. Update FitnessCalculator to consider progression state
4. Test recovery decision → progression modifier flow

---

## Edge Cases & Special States

### Enlistment State Edge Cases

| Scenario | Camp Life Behavior |
|----------|-------------------|
| **New enlistment (first 3 days)** | 3-day grace period. No camp opportunities generated. Shows: "You're still finding your place. The camp will open up once you've settled in." Player can still access "Take Your Own Action" categories for basic decisions. |
| **Probation active** | Opportunity budget reduced by 1. No leadership or high-stakes opportunities. Fatigue cap affects what player can engage with (18 max instead of 24). Shows probation reminder in status. |
| **Pending discharge** | No camp opportunities. Shows: "Your thoughts are elsewhere. Discharge awaits." Only "Handle Affairs" category available. |
| **Grace period (lord died/captured)** | No camp opportunities. Shows: "The company is in disarray. Your lord is gone." Player can only access basic rest. Muster will fire soon for discharge/transfer. Preserves behavior profile for when player re-enlists. |

### Combat & Movement Edge Cases

| Scenario | Camp Life Behavior |
|----------|-------------------|
| **Active battle** | Camp Hub inaccessible. No opportunities. Resume normal generation after battle ends. |
| **Army marching** | Opportunity budget = 0. Shows: "The army is on the march." Only rest and basic affairs available. |
| **Siege (attacking)** | Opportunity budget reduced. Focus on stress relief and rest. Training/social limited. |
| **Siege (defending)** | Opportunity budget = 0-1. Only recovery opportunities. Shows: "The walls demand constant vigilance." |
| **Player captured** | Camp life suspended entirely. No opportunities. Resume on release with stale cache cleared. |

### Muster & Pay Edge Cases

| Scenario | Camp Life Behavior |
|----------|-------------------|
| **Muster day** | No camp opportunities during muster sequence. Camp Hub shows muster status instead. Resume after muster complete. |
| **6-hour baggage window (post-muster)** | Special baggage opportunity appears: "The baggage train is accessible. Good time to sort your gear." Disappears after 6 hours. |
| **Pay tension high** | Economic opportunities get priority boost. Stress relief opportunities appear more. Shows subtle warning in camp status. |
| **Backpay owed** | Economic and gambling opportunities score higher. Social opportunities with "commiserate" theme appear. |

### Supply & Resource Edge Cases

| Scenario | Camp Life Behavior |
|----------|-------------------|
| **Supply < 30%** | Opportunity budget -1. Foraging/supply opportunities prioritized. Warning in camp status. |
| **Supply < 20% (critical)** | Opportunity budget = 1 max. Only survival-focused opportunities. Shows: "Supplies are critically low. The quartermaster has locked all non-essential access." |
| **Low gold (< 50)** | Economic opportunities score +20. Gambling opportunities filtered (can't afford stakes). Work opportunities prioritized. |
| **Critical fatigue (< 5)** | Only rest opportunities shown. All others filtered. Shows: "You're dangerously exhausted. Rest is the only option." |

### Medical Condition Edge Cases

| Scenario | Camp Life Behavior |
|----------|-------------------|
| **Player injured** | Training opportunities filtered out. Recovery opportunities score +30. Medical tent opportunity always available. |
| **Player ill** | Social opportunities filtered (contagious). Rest and medical opportunities only. Shows: "You're feeling poorly. Best to rest." |
| **Medical Risk escalating (2+)** | Medical opportunity appears with priority. Warning in camp status: "Something feels off." |

### Cross-Faction & Re-enlistment Edge Cases

| Scenario | Camp Life Behavior |
|----------|-------------------|
| **Same faction re-enlistment** | Behavior profile preserved. Learning continues from previous service. |
| **Different faction re-enlistment** | Behavior profile reset (new army, new culture). Fresh learning period. |
| **Veteran re-enlistment (T4 start)** | Skip basic opportunities. Show higher-tier opportunities immediately. |
| **Bad discharge re-enlistment** | Probation rules apply. Limited opportunities. Profile reset. |

### Technical Edge Cases

| Scenario | Handling |
|----------|----------|
| **OpportunityHistory corrupted on load** | Reset to defaults. Log warning. Fresh generation with no learning bias. |
| **CampOpportunityGenerator null** | Graceful fallback to "Take Your Own Action" only. Log error. |
| **No opportunities pass threshold (all < 40)** | Show contextual empty message (varies by world state). Don't show "nothing happening" generically. |
| **Cache expired mid-menu** | Regenerate on next menu access. Don't regenerate while player is viewing current opportunities. |
| **Opportunity engaged but underlying decision on cooldown** | Check cooldown before engaging. Show "Already done recently" and remove from display. |
| **Save during Camp Hub open** | Cache serialized. Resume from same opportunities on load. Don't regenerate immediately. |
| **Multiple opportunities trigger same decision** | Dedup by target decision ID. Only show most contextually appropriate wrapper. |

### Time Control Edge Cases

| Scenario | Handling |
|----------|----------|
| **Time paused externally** | Camp Hub still accessible. Opportunities frozen (don't expire). |
| **Fast forward active** | Camp Hub access pauses time (matches muster behavior). Prevents missing opportunities. |
| **Hour changed while Camp Hub open** | Keep current opportunities. Don't refresh mid-session. Mark cache as stale for next open. |

### Order Interaction Edge Cases

| Scenario | Camp Life Behavior |
|----------|-------------------|
| **Order just completed (success)** | Social opportunities score +15. Recognition opportunity may appear. |
| **Order just completed (failure)** | Social opportunities filtered. Recovery focus. Shows: "The camp feels tense." |
| **Order active but paused** | Still considered "on duty." No camp opportunities. |
| **Order about to be issued** | If orchestrator knows order is imminent (via OrderManager), reduce opportunity budget. Don't compete with order presentation. |

### UI Edge Cases (Quick Decision Center)

| Scenario | Handling |
|----------|----------|
| **KINGDOM section empty** | Show: "The realm is quiet. No major news." Never show blank section. |
| **CAMP section nothing happening** | Show: "A quiet moment in camp. Rest while you can." Never show blank section. |
| **YOU/AHEAD no forecast** | Show: "Quiet. Almost too quiet." This is a valid forecast - nothing imminent. |
| **Culture rank text fails** | Fallback to generic English: "Sergeant", "Captain". Log warning but don't break display. |
| **DECISIONS menu empty** | Show: "Nothing demands your attention right now. The camp is quiet." Option to access Camp Hub for "Take Your Own Action" categories. |
| **DECISIONS clicked while on order** | Show order-appropriate opportunities only (rest during march, check equipment). Or show: "You're on duty. Focus on the task at hand." with order status. |
| **Opportunities expire while viewing** | Don't remove mid-session. Mark stale and regenerate on next DECISIONS open. |
| **Player wants Leave Service** | Camp Hub shows no Leave option. If player is confused, tooltip on other options can say "Leave Service available at Muster." |
| **CAMP STATUS fails to generate** | Fallback to simple: "⚙️ Camp Status: Normal". Log error. Don't break Camp Hub. |
| **RECENT ACTIONS empty** | Show: "Nothing notable to report." with subtle styling. |
| **Lord dead/captured, player needs to leave** | Grace period shows: "The company is in disarray." Muster will fire within 24 hours for discharge/transfer. Player waits for Muster. |
| **Player ignores Muster for days** | Muster system handles this - it's a blocking event that forces resolution. Not a Camp Hub concern. |

### Caching Edge Cases

| Scenario | Handling |
|----------|----------|
| **Menu opens during refresh trigger** | Call `RefreshIfNeeded()` before render. Stale text never shown. |
| **Fast travel skips 6+ hours** | Force refresh all sections on next menu open if time gap > 2 hours. |
| **Save/Load cycle** | Don't persist cache. Rebuild on load. Fresh generation with current state. |
| **Alt-tab/pause for 30 min real time** | No issue - cache based on game time, not real time. |
| **Time speed max (x4)** | Forecasts still valid - they're based on game hours not real seconds. |
| **Multiple triggers fire at once** | Refresh once, not per trigger. Batch trigger checks. |

```csharp
// Fast travel protection
public void RefreshIfNeeded()
{
    var hoursSinceLastCheck = (CampaignTime.Now - _lastCheckTime).ToHours;
    if (hoursSinceLastCheck > 2)
    {
        ForceRefreshAll(); // Time jumped - rebuild everything
        return;
    }
    // Normal trigger-based refresh...
}
```

### Order Flow Edge Cases

| Scenario | Handling |
|----------|----------|
| **Fast travel past SCHEDULED** | Auto-accept if player wasn't available. Notification: "You were assigned Guard Duty while traveling." |
| **Fast travel past PENDING** | Auto-decline with minor rep penalty (-5). Notification: "You missed an order assignment." |
| **Multiple orders scheduled** | Queue system. Only one SCHEDULED at a time. Others wait. |
| **Order cancelled while SCHEDULED** | Remove from ORDERS menu. CAMP news shows: "[NEW] The roster changed. Guard duty cancelled." |
| **Player ignores PENDING 24h+** | Auto-decline after 24 hours. Warning in YOU section 6h before: "AHEAD: You need to respond to the order soon." |
| **Forecast wrong (order didn't come)** | Use soft language: "likely", "seems like". Never guarantee. |

**Order state transitions:**
```
FORECAST (12-24h out) → SCHEDULED (8-18h out) → PENDING (requires response) → ACTIVE → COMPLETE
     CAMP/YOU section        ORDERS menu           ORDERS menu [NEW]         ORDERS     ORDERS
```

### Order-Decision Tension Edge Cases

| Scenario | Handling |
|----------|----------|
| **Detection check timing** | Check happens BEFORE activity starts, not during. Player sees consequence immediately. |
| **Order phase changes mid-activity** | Complete current activity. Apply consequences if caught at END. Don't interrupt. |
| **Caught consequences exceed rep** | Floor at 0. YOU section shows: "You're on thin ice with command." |
| **Order fails due to repeated absences** | Cap at 3 catches before order failure risk triggers. Single catch = rep loss only. |
| **Same activity risky for one order, safe for another** | Tooltip always shows CURRENT context. Dynamically generated per situation. |
| **Player clicks risky option, gets caught** | Immediate notification. Activity still happens (you got caught, not prevented). Consequences apply. |

**Catch handling:**
```csharp
// When player selects risky activity
float detectionChance = GetDetectionChance(opportunity, currentOrder);
bool caught = Random.Range(0f, 1f) < detectionChance;

if (caught)
{
    ApplyConsequences(opportunity.caughtConsequences);
    ShowNotification("You were caught away from your post!");
}

// Activity proceeds either way - they're caught, not stopped
ExecuteActivity(opportunity);
```

### Forecast Priority Edge Cases

| Scenario | Priority | Handling |
|----------|----------|----------|
| **Order + Crisis + Muster all pending** | Order wins | Show order forecast. Crisis/Muster shown on next refresh after order resolves. |
| **Multiple camp activities available** | Highest scored | Show top opportunity in forecast. Others visible in DECISIONS. |
| **Crisis brewing but no immediate threat** | Lower priority | Use subtle language: "Something feels off." Don't alarm unnecessarily. |
| **Nothing to forecast** | Default text | "The day stretches ahead. Time will tell." Valid forecast - nothing imminent. |

**Forecast priority implementation:**
```csharp
var forecasts = new List<(string text, int priority)>
{
    (GetOrderForecast(), 100),        // Orders always top
    (GetCrisisWarning(), 90),          // Supply/morale crisis
    (GetMusterForecast(), 80),         // Pay day coming
    (GetCampActivityForecast(), 50),   // Card game, drill, etc.
    (GetDefaultForecast(), 0)          // "Quiet. Almost too quiet."
};

return forecasts.Where(f => !string.IsNullOrEmpty(f.text))
                .OrderByDescending(f => f.priority)
                .First().text;
```

### Commitment Edge Cases

| Scenario | Handling |
|----------|----------|
| **Player commits then fast-travels** | Check if scheduled time passed. If so, auto-fire or cancel with note. |
| **Player commits then gets wounded** | Activity still fires but with modified outcomes (can't participate fully). |
| **Player commits then order is issued** | Commitment stays. Risk tooltip appears if conflict. Player decides. |
| **Player commits to two things** | Only one commitment at a time. Second replaces first with confirmation. |
| **Activity cancelled (e.g., rain)** | Clear commitment. YOU section: "Card game cancelled due to weather." |
| **Save/Load with commitment** | Commitment persists in save. Fires on load if time passed. |
| **Player tries to re-commit** | Button greyed out. Tooltip: "You're already attending." |

### Localization Edge Cases

| Scenario | Handling |
|----------|----------|
| **Culture token `{NCO_TITLE}` missing** | Fallback to "the NCO". Log warning. |
| **Culture token returns empty** | Use generic English equivalent. Never show empty string. |
| **Tooltip text > 100 chars** | Truncate with "..." at word boundary. |
| **[NEW] tag in non-English** | Localization key `str_new_tag`. Translate to `[NEU]`, `[NOUVEAU]`, etc. |
| **Forecast text too long for UI** | Cap at 60 chars per line. Wrap gracefully. Test all cultures. |

---

## Examples

### Example 1: Garrison Morning (Active Player)

**Context:**
- World: Garrison, peacetime
- Time: 9am (morning, post-muster)
- Player: Rested, decent gold, decent rep
- History: Engaged training yesterday, ignored social 2 days ago

**Orchestrator Generates:**
1. Training opportunity (score: 85)
   - Morning = +10
   - Garrison = +15
   - Player rested = +5
   - Engaged training yesterday = -5 (variety)
   - Final: 85

2. Economic opportunity (score: 45)
   - Morning = +5
   - Player has gold = -10
   - Rarely engages economic = -10
   - Final: 45

3. Social opportunity (score: 65)
   - Morning = -5
   - Ignored 2 days ago = -10
   - Variety boost = +10
   - Final: 65

**Player Sees:**
```
CAMP LIFE:

Veterans are drilling by the wagons. The sergeant 
is putting them through their paces.
   [Join the drill]

A few soldiers are lounging by the fires, swapping 
stories about last campaign.
   [Join them]
```
Top 2 shown (training + social). Economic filtered out (below threshold).

---

### Example 2: Siege Evening (Exhausted Player)

**Context:**
- World: Siege (day 5), desperate defense
- Time: 8pm (evening, brief respite)
- Player: Exhausted (80 fatigue), injured (minor), low gold
- History: Just completed grueling order

**Orchestrator Generates:**
1. Rest opportunity (score: 95)
   - Evening = +10
   - Player exhausted = +30
   - Player injured = +20
   - Siege = +15 (recovery critical)
   - Final: 95

2. Stress relief opportunity (score: 70)
   - Evening = +15
   - Siege = +25 (stress high)
   - Social = +10
   - Player exhausted = -10 (can't join revelry)
   - Final: 70

3. Training opportunity (score: 15)
   - Evening = -10
   - Player exhausted = -25
   - Siege = -10 (wrong tone)
   - Final: 15 (filtered out)

**Player Sees:**
```
CAMP LIFE:

You find a spot away from the noise. Your body 
aches. Sleep will come fast.
   [Rest]
```
Only rest shown. Everything else inappropriate for context.

---

### Example 3: No Opportunities (Campaign March)

**Context:**
- World: Campaign, active march
- Time: 2pm (afternoon)
- Player: On the move with army
- History: Lots of recent activity

**Orchestrator Generates:**
- Opportunity budget: 0 (marching, no free time)

**Player Sees:**
```
CAMP LIFE:

The army is on the march. No time for leisure.

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

TAKE YOUR OWN ACTION:
[TRAINING] ▼ (grayed - marching)
[REST & RECOVER] ▼
[HANDLE AFFAIRS] ▼ (limited options)
```

Camp life empty. Player can still rest or handle affairs via categories, but options limited.

---

### Example 4: Learning Adaptation (Week 1 vs Week 4)

**Week 1 (No learning data):**
```
CAMP LIFE:
  Training opportunity (shown)
  Social opportunity (shown)
  Economic opportunity (shown)
  
(Equal presentation, no preferences known)
```

**Week 4 (After learning):**
```
Player always engages training: 15/15 times
Player sometimes engages social: 4/10 times
Player never engages economic: 0/12 times

CAMP LIFE:
  Training opportunity (shown often, score +15)
  Social opportunity (shown sometimes, score +5)
  Economic opportunity (rarely shown, score -10)
  
(Adapted to player preferences)
```

---

## Order-Decision Tension System

When player is ON ORDER, DECISIONS still shows opportunities - but the orchestrator curates what appears and tooltips reveal consequences. No categories in the UI.

### How It Works

1. **Orchestrator filters** opportunities based on current order
2. **Compatible options** appear normally
3. **Risky options** appear with consequences in tooltip
4. **Blocked options** don't appear at all (filtered out)

### DECISIONS Display (During Order)

```
[  DECISIONS  ] ▼

    The soldier next to you looks bored.
        [Strike up conversation]
    
    Laughter drifts from the card game...
        [Sneak away]
```

**No SAFE/RISKY labels.** Player sees natural opportunities.

**Hover tooltip on "Sneak away" reveals risk:**
```
┌─────────────────────────────────────────┐
│ Risk: You're on guard duty.            │
│ If caught: -15 Officer Rep             │
│ Detection chance: ~25%                  │
└─────────────────────────────────────────┘
```

**Blocked options (like visiting quartermaster while on guard) simply don't appear.**

### JSON Schema (Localized)

**Camp Opportunity Definition:**
```json
{
  "id": "opp_card_game",
  "titleId": "opp_card_game_title",
  "title": "Evening Card Game",
  "descriptionId": "opp_card_game_desc",
  "description": "A card game is forming by the fire. Stakes look good.",
  "actionId": "opp_card_game_action",
  "action": "Sit in",
  
  "orderCompatibility": {
    "default": "risky",
    "guardPost": "risky",
    "campPatrol": "risky",
    "firewoodDetail": "available",
    "sentryDuty": "risky",
    "marchFormation": "blocked",
    "equipmentCleaning": "available"
  },
  
  "detection": {
    "baseChance": 0.25,
    "nightModifier": -0.15,
    "highRepModifier": -0.10
  },
  
  "caughtConsequences": {
    "officerRep": -15,
    "discipline": 2,
    "orderFailureRisk": 0.20
  },
  
  "tooltipRiskyId": "opp_card_game_risk_tooltip",
  "tooltipRisky": "Risk: You're on guard duty. If caught: -15 Officer Rep. Detection: ~25%"
}
```

**Order Compatibility Values:**
- `"available"` - No risk, appears normally
- `"risky"` - Appears with risk tooltip, detection check on engage
- `"blocked"` - Filtered out by orchestrator (doesn't appear)

### Localization Keys

Add to `ModuleData/Languages/enlisted_strings.xml`:

```xml
<!-- Order-Decision Tension - Tooltips -->
<string id="opp_risk_tooltip_generic" text="Risk: You're on duty. If caught: {REP_PENALTY} Officer Rep. Detection: ~{DETECTION_CHANCE}%" />
<string id="opp_caught_notification" text="You were caught away from your post!" />
<string id="opp_caught_detail" text="{OFFICER_TITLE} noticed your absence. This won't be forgotten." />
```

**No category labels in UI.** Tooltips provide risk info. Blocked options don't appear.

### Detection Logic

```csharp
public bool AttemptRiskyOpportunity(CampOpportunity opp, ActiveOrder order)
{
    var detection = opp.Detection;
    float chance = detection.BaseChance;
    
    // Day phase modifier (Night = Order Phase 4)
    if (CurrentDayPhase() == DayPhase.Night)
        chance += detection.NightModifier;
    
    // Reputation modifier
    if (EscalationManager.Instance.State.OfficerReputation > 70)
        chance += detection.HighRepModifier;
    
    // Order-specific modifiers (some orders have stricter oversight)
    chance *= order.Definition.OversightMultiplier;
    
    // Roll
    if (MBRandom.RandomFloat < chance)
    {
        // Caught!
        ApplyCaughtConsequences(opp.CaughtConsequences);
        return false;
    }
    
    // Got away with it
    return true;
}
```

### Consequences When Caught

| Consequence | Typical Value | Description |
|-------------|---------------|-------------|
| `officerRep` | -10 to -20 | Officers don't like soldiers abandoning duty |
| `discipline` | +1 to +3 | Adds to discipline track (scrutiny increases) |
| `orderFailureRisk` | 0.10 to 0.30 | Chance current order fails due to absence |
| `fatigueBonus` | 0 | No rest bonus if caught |

---

## Future Enhancements

**Phase 7+ (Post-Launch):**

1. **Camp Schedules Variation**
   - Different schedules per culture/lord
   - Special events (festivals, executions, arrivals)
   - Weather affecting activities

2. **Multi-Step Camp Events**
   - Opportunities that chain (join game → win → invited to high-stakes game)
   - Long-running camp situations (feud developing over days)

3. **Social Network Simulation**
   - NPCs with schedules
   - Build relationships with specific soldiers
   - Reputation affects who invites you

4. **Camp Mood System**
   - Victory celebration mode
   - Post-defeat mourning
   - Pre-battle tension
   - Affects all opportunity generation

---

**End of Document**
