# Camp Background Simulation: Living Company

**Summary:** The Camp Background Simulation creates an autonomous company that lives and breathes without player input. Soldiers get sick, equipment degrades, morale shifts, and incidents occur - all visible through the news feed. This layer provides the context and pressure that makes events meaningful. The player observes camp life; they don't control it.

**Status:** ✅ Implemented  
**Priority:** Phase 5.5 (After Content Orchestrator core, before Camp Life Simulation UI)  
**Last Updated:** 2025-12-30  
**Implementation:** `src/Features/Camp/CompanySimulationBehavior.cs`, `src/Features/Camp/Models/`  
**Related Docs:** [Camp Life Simulation](camp-life-simulation.md), [News Reporting System](../Features/UI/news-reporting-system.md), [Company Needs](../Features/Core/company-needs.md), [Content Orchestrator](content-orchestrator-plan.md)

---

## Index

1. [Core Philosophy](#core-philosophy)
2. [Simulation Systems](#simulation-systems)
3. [Company Roster](#company-roster)
4. [Daily Tick System](#daily-tick-system)
5. [Background Incidents](#background-incidents)
6. [Pulse Events](#pulse-events)
7. [Cascading Pressure](#cascading-pressure)
8. [News Feed Integration](#news-feed-integration)
9. [Data Model](#data-model)
10. [Technical Architecture](#technical-architecture)
11. [Example Days](#example-days)
12. [Configuration](#configuration)
13. [Implementation Tasks](#implementation-tasks)

---

## Core Philosophy

### The Principle

**You are one soldier in a company. The company has its own life.**

Things happen whether you act or not:
- Soldiers get sick, injured, or desert
- Equipment breaks, gets stolen, or wears out
- Morale rises and falls based on conditions
- Small incidents occur constantly
- The company consumes supplies, accumulates fatigue, deals with problems

**You observe this through the news feed.** You don't trigger it. You don't always respond. Sometimes you just watch camp life unfold.

### What This Is NOT

- Not an event system (events are separate, respond to this)
- Not player-driven (runs automatically each day)
- Not always actionable (many incidents are flavor)
- Not a management game (you can't order the company around)

### What This IS

- Background simulation that runs on daily tick
- News generator that shows camp life
- Pressure builder that creates context for events
- Immersion layer that makes the company feel real

---

## Simulation Systems

### Overview

Six systems run each day, in order:

```
Daily Tick (Dawn of each day):
│
├── 1. CONSUMPTION
│   └── Supplies consumed, equipment used
│
├── 2. ROSTER UPDATES
│   └── Sick recover/worsen, wounded heal, deserters leave
│
├── 3. CONDITION CHECKS
│   └── New sickness, injuries, desertion attempts
│
├── 4. INCIDENT ROLLS
│   └── Random camp incidents (0-2 per day)
│
├── 5. PULSE EVALUATION
│   └── Did morale/supplies/rest cross a threshold?
│
└── 6. NEWS GENERATION
    └── Push all results to news feed
```

### System Dependencies

```
Company Needs (existing)     Escalation Tracks (existing)
        │                            │
        ▼                            ▼
┌─────────────────────────────────────────────┐
│       CAMP BACKGROUND SIMULATION            │
│  (new: CompanySimulationBehavior)           │
└─────────────────────────────────────────────┘
        │                            │
        ▼                            ▼
   News Feed                   Content Orchestrator
   (display)                   (event decisions)
```

---

## Orchestrator Integration

### How They Work Together

The **Camp Background Simulation** and **Content Orchestrator** are complementary systems:

| System | Purpose | Output |
|--------|---------|--------|
| **Background Simulation** | Passive simulation, things happening | News items, pressure tracking |
| **Content Orchestrator** | Active decisions, when to engage player | Events, decisions, orders |

```
                    ┌─────────────────────────┐
                    │  CAMP BACKGROUND SIM    │
                    │  (runs every day)       │
                    └───────────┬─────────────┘
                                │
              ┌─────────────────┼─────────────────┐
              │                 │                 │
              ▼                 ▼                 ▼
        ┌──────────┐    ┌──────────────┐   ┌──────────────┐
        │  NEWS    │    │  PRESSURE    │   │   WORLD      │
        │  FEED    │    │  TRACKING    │   │   STATE      │
        └──────────┘    └──────┬───────┘   └──────┬───────┘
                               │                  │
                               ▼                  ▼
                    ┌─────────────────────────────────┐
                    │      CONTENT ORCHESTRATOR       │
                    │  (decides when events fire)     │
                    └─────────────────────────────────┘
                                │
              ┌─────────────────┼─────────────────┐
              │                 │                 │
              ▼                 ▼                 ▼
        ┌──────────┐    ┌──────────────┐   ┌──────────────┐
        │  EVENTS  │    │  CAMP LIFE   │   │   CRISIS     │
        │          │    │  OPPORTUNITIES│   │   EVENTS     │
        └──────────┘    └──────────────┘   └──────────────┘
```

### What Background Simulation Provides to Orchestrator

The orchestrator reads from the background simulation state:

```csharp
// In ContentOrchestrator.AnalyzeWorldState()

var simulation = CompanySimulationBehavior.Instance;

// Roster health affects event selection
float casualtyRate = simulation.Roster.CasualtyRate;
bool hasManySick = simulation.Roster.SickCount > 5;
bool hasDesertions = simulation.Pressure.RecentDesertions > 0;

// Pressure affects urgency
bool supplyPressure = simulation.Pressure.DaysLowSupplies > 2;
bool moralePressure = simulation.Pressure.DaysLowMorale > 2;

// Use in world state for event filtering
worldState["company_stressed"] = supplyPressure || moralePressure;
worldState["high_casualties"] = casualtyRate > 0.2f;
worldState["recent_desertions"] = hasDesertions;
```

### What Orchestrator Does with This

1. **Event Selection:**
   - High casualties → medical events more likely
   - Recent desertions → discipline events more relevant
   - Supply pressure → foraging/scavenging events available

2. **Frequency Adjustment:**
   - Stressed company → more quiet days (men need rest)
   - Healthy company → normal event frequency

3. **Crisis Response:**
   - When pressure hits threshold → orchestrator queues mandatory crisis event
   - Background simulation detects → orchestrator delivers

### Crisis Event Handoff

When pressure becomes critical, background simulation tells orchestrator:

```csharp
// In CompanySimulationBehavior.CheckCrisisTriggers()

if (_pressure.DaysLowSupplies >= 3 && _needs.GetNeed(CompanyNeed.Supplies) < 20)
{
    // Queue crisis with orchestrator - player MUST respond
    _orchestrator.QueueCrisisEvent("evt_supply_crisis");
    
    // Add urgent news
    _news.AddCampNews(
        "The men are starving. Something must be done.",
        "critical",
        "crisis"
    );
}
```

### Camp Life Opportunities

The orchestrator uses simulation state for Camp Life opportunities:

```csharp
// In CampOpportunityGenerator.GenerateOpportunities()

var simulation = CompanySimulationBehavior.Instance;

// If many wounded, offer medical opportunities
if (simulation.Roster.WoundedCount > 10)
{
    opportunities.Add("Help the physician with the wounded");
}

// If recent incident, offer related opportunity
if (simulation.HasRecentIncident("inc_fight_serious"))
{
    opportunities.Add("Mediate between the fighters");
}

// If morale low, offer morale-boosting opportunities
if (_needs.GetNeed(CompanyNeed.Morale) < 40)
{
    opportunities.Add("Tell stories by the fire");
    opportunities.Add("Organize a dice game");
}
```

### Summary: Who Does What

| Responsibility | Background Simulation | Content Orchestrator |
|----------------|----------------------|---------------------|
| Daily news generation | ✅ | — |
| Roster tracking (sick/wounded) | ✅ | — |
| Pressure accumulation | ✅ | — |
| Random incidents | ✅ | — |
| Event selection & timing | — | ✅ |
| Camp Life opportunities | — | ✅ |
| Crisis event delivery | Detects & flags | Delivers to player |
| World state for events | Provides data | Reads data |

**The background simulation creates the CONTEXT. The orchestrator uses that context to decide WHEN and WHAT to show the player.**

---

### Forecast Integration (Player Warnings)

The background simulation feeds the **AHEAD** section of the main menu, warning players about upcoming problems:

```
╔══════════════════════════════════════════════════════════╗
║  _____ YOU _____                                          ║
║  NOW: On guard duty. Day 2 of 3. Tired.                  ║
║  AHEAD: The men are hungry. Rations won't last.          ║  ← FROM SIMULATION
╚══════════════════════════════════════════════════════════╝
```

**What the simulation provides to ForecastGenerator:**

```csharp
// ForecastGenerator reads simulation state for warnings
var sim = CompanySimulationBehavior.Instance;

// Pressure-based warnings (escalating urgency)
if (sim.Pressure.DaysLowSupplies >= 2)
    → "The men are hungry. Supplies won't last." (CRITICAL)

if (sim.Pressure.DaysLowMorale >= 2)
    → "The mood is dark. Something may break." (CRITICAL)

// Health warnings
if (sim.Roster.SickCount > 5)
    → "Fever spreading through camp." (HIGH)

if (sim.Roster.WoundedCount > 20% of total)
    → "Many wounded need care." (HIGH)

// Desertion warning
if (sim.Pressure.RecentDesertions > 0)
    → "Men have been slipping away." (HIGH)
```

**Priority system ensures urgent warnings show first:**

| Priority | Examples | Always Shown? |
|----------|----------|---------------|
| 🔴 Critical | Supply/morale crisis imminent | ✅ Yes |
| 🟠 High | Many sick, desertions, low supplies | ✅ Yes |
| 🟡 Medium | Grumbling, discipline issues | If space |
| 🟢 Low | Incoming order, quiet day | If space |

**This gives the player advance warning to take action** before a crisis event fires.

---

## Company Roster

### Integration with Bannerlord Wounded System

**This is NOT an abstract simulation.** It integrates with the real Bannerlord wounded troop system.

#### Game APIs Used

```csharp
// Reading wounded count from party roster
int woundedCount = party.MemberRoster.TotalWounded;
int totalTroops = party.MemberRoster.TotalManCount;
int fitForDuty = totalTroops - woundedCount;

// Adding wounded troops (when simulation causes injury)
party.MemberRoster.AddToCounts(troopCharacter, count: 0, woundedCount: 1);

// Healing happens via native PartyHealCampaignBehavior
// Base rate: 5 HP/day for regulars (DefaultPartyHealingModel)
// Modified by: Medicine skill, settlement rest, starvation
```

#### What We Control vs. What the Game Controls

| Aspect | Who Controls | Notes |
|--------|--------------|-------|
| **Wounded status** | Game | Real TroopRoster wounded count |
| **Healing rate** | Game (we can modify) | Via custom PartyHealingModel |
| **When to wound** | Us (simulation) | We decide who gets injured |
| **Sickness** | Us (abstract) | Not native - we track separately |
| **Desertion** | Us | Remove troops from roster |
| **Death** | Us + Game | Battle deaths = game; sickness deaths = us |

---

### Roster Categories

The simulation reads from real game data and adds our own tracking layer:

| Category | Source | Description | Can Fight? |
|----------|--------|-------------|------------|
| **Fit for Duty** | `TotalManCount - TotalWounded` | Healthy, ready | ✅ Full |
| **Wounded** | `TotalWounded` (game) | Real wounded troops | ❌ No |
| **Sick** | Our tracking | Not native - overlay | ❌ No |
| **Missing** | Our tracking | Deserted/lost | ❌ No |
| **Dead (Campaign)** | Our tracking | Running death count | ❌ N/A |

**Important:** "Wounded" is the real Bannerlord wounded count. "Sick" is our abstract overlay for non-combat illness that we want to feel different from battle wounds.

**Total Strength** = `party.MemberRoster.TotalManCount`

**Effective Strength** = Fit for Duty (game handles wounded exclusion)

---

### Real Consequences on Party Size

**Everything that kills or removes soldiers ACTUALLY affects the party roster:**

| Event | What Happens | Party Size Change |
|-------|--------------|-------------------|
| **Battle death** | Game removes them | ↓ Real loss |
| **Sickness death** | We call `AddToCounts(troop, -1)` | ↓ Real loss |
| **Confirmed desertion** | We call `AddToCounts(troop, -1)` | ↓ Real loss |
| **Wounded** | Game marks as wounded | Same size, fewer effective |
| **Sick** | Our overlay (SickCount) | Same size (unless they die) |
| **Missing** | Our overlay (MissingCount) | Same size (until confirmed) |

**This matters because:**
- The lord's party gets smaller over a long campaign
- Lord needs to recruit to replace losses
- Low morale / poor conditions = faster attrition
- Good management = soldiers survive

**Example campaign attrition:**
```
Day 1:  127 soldiers
Day 10: 125 soldiers (2 died from fever)
Day 20: 121 soldiers (3 deserted, 1 training accident death)
Day 30: 118 soldiers (2 more sick deaths, 1 deserted)

Lord recruits 15 soldiers at village...

Day 31: 133 soldiers
```

**The lord feels the cost of neglecting the company.**

### Roster Transitions

```
                    ┌──────────────┐
                    │ Fit for Duty │ ← Real: TotalManCount - TotalWounded
                    └──────┬───────┘
                           │
         ┌─────────────────┼─────────────────┐
         │                 │                 │
         ▼                 ▼                 ▼
   ┌───────────┐    ┌───────────┐    ┌───────────┐
   │  Wounded  │    │   Sick    │    │  Missing  │
   │ (GAME)    │    │ (OURS)    │    │ (OURS)    │
   └─────┬─────┘    └─────┬─────┘    └─────┬─────┘
         │                │                │
         │ Game heals     │ Our recovery   │ Permanent
         │ via Medicine   │ check          │ loss
         ▼                ▼                ▼
   ┌───────────┐    ┌───────────┐    ┌───────────┐
   │ Recovered │    │ Recovered │    │  Deserted │
   │ (auto)    │    │ OR Dead   │    │           │
   └───────────┘    └───────────┘    └───────────┘
```

#### How Each Category Works

**Wounded (Game-Controlled):**
- Read from `party.MemberRoster.TotalWounded`
- Healed by native `PartyHealCampaignBehavior`
- Rate affected by Medicine skill, settlement rest
- We can CAUSE wounds (via `AddToCounts` with `woundedCount`)
- We **don't** control healing rate directly (game does)

**Sick (Our Overlay):**
- Tracked in `CompanySimulationSaveData.SickCount`
- Not real wounded troops - separate narrative concept
- We control recovery/death rolls
- Appears in news as different from wounds: "fever" vs "wounds"
- If we want sick to actually be unable to fight, we wound them too

**Missing (Our Tracking):**
- Tracked in `CompanySimulationSaveData.MissingCount`
- Represents desertion - actual troops removed from roster
- `party.MemberRoster.AddToCounts(character, -1)` to remove

**Dead (Our Counter):**
- Running count for campaign statistics
- Battle deaths: read from battle aftermath
- Sickness deaths: our simulation kills them

### Roster Events

| Transition | Implementation | News Example |
|------------|----------------|--------------|
| Fit → Wounded | `AddToCounts(troop, 0, woundedCount: 1)` | "A soldier broke his arm during drill." |
| Fit → Sick | `SickCount++` (our overlay) | "Two soldiers report fever this morning." |
| Wounded → Fit | Automatic (game's Medicine system) | "The wounded are recovering." |
| Sick → Fit | Our recovery roll: `SickCount--` | "The fever broke. He's on his feet." |
| Sick → Dead | Our death roll: `SickCount--; DeadThisCampaign++` | "A soldier died in the night. Fever took him." |
| Fit → Missing | `MissingCount++` (tracked but not removed yet) | "A soldier didn't report for roll call." |
| Missing → Fit | `MissingCount--` (returned) | "The missing soldier returned. Says he got lost." |
| Missing → Gone | `AddToCounts(troop, -1); MissingCount--` | "He's truly gone. Deserted." |
| Any → Dead (combat) | Read from battle aftermath | "We lost three in the fighting." |

**Note:** Wounded recovery is handled by the game's `PartyHealCampaignBehavior` based on Medicine skill. We don't roll for it - we just report when the game heals them.

---

## Daily Tick System

### 1. Consumption Phase

**Supplies:**
```
Daily Supply Consumption = TotalSoldiers × BaseConsumptionRate
BaseConsumptionRate = 0.5 (configurable)

Modifiers:
- Marching: ×1.2
- Combat day: ×1.5
- Resting in camp: ×0.8
- Foraging active: ×0.7
```

**Equipment:**
```
Daily Equipment Degradation = TotalSoldiers × BaseDegradationRate
BaseDegradationRate = 0.2 (configurable)

Modifiers:
- Combat: ×3.0
- Rain/bad weather: ×1.5
- Repair order active: ×0.5
```

**News Generation:**
- If Supplies drops below 50: "Rations are running short."
- If Supplies drops below 25: "Half-rations ordered. Men grumble."
- If Equipment drops below 50: "Much of the gear needs repair."

---

### 2. Roster Update Phase

#### Wounded Recovery (Game-Controlled)

**We don't roll for wounded recovery.** The game handles this via `PartyHealCampaignBehavior`:
- Base: 5 HP/day for regulars
- Modified by: Medicine skill, resting in settlement, starvation
- We can optionally affect via custom `PartyHealingModel` (see Data Model section)

**What we DO:** Track when wounded count changes and generate news.

```csharp
// Check if anyone healed since yesterday
int previousWounded = _lastTickWounded;
int currentWounded = party.MemberRoster.TotalWounded;

if (currentWounded < previousWounded)
{
    int healed = previousWounded - currentWounded;
    AddNews($"{healed} wounded {(healed == 1 ? "soldier has" : "soldiers have")} recovered.");
}

_lastTickWounded = currentWounded;
```

#### Sick Recovery (Our System)

**Sickness is our overlay** - separate from game wounds. We control recovery/death:

```
For each Sick soldier (SickCount):

Recovery Chance = BaseRecoveryChance + Modifiers = 15% per day base

Modifiers:
+ Medical order active: +15%
+ High Rest (>70): +10%
+ High Supplies (>70): +5%
- Low Supplies (<30): -10%
- Low Rest (<30): -5%
- Harsh terrain: -5%

Death Chance = 2% per day base

Modifiers:
+ No medical care: +3%
+ Very low Supplies (<20): +2%
+ Harsh conditions: +2%
- Medical order active: -2%
```

**If sick soldier dies:**
```csharp
SickCount--;
DeadThisCampaign++;

// ACTUALLY REMOVE FROM PARTY ROSTER
var troop = GetRandomTroop(party.MemberRoster);
party.MemberRoster.AddToCounts(troop, count: -1);  // Real removal

AddNews("A soldier died from fever.");
```

**This affects real party size.** The lord's party shrinks. The dead soldier is gone.

#### Missing Resolution

```
For each Missing soldier (MissingCount):

Return Chance = 10% per day (most are gone for good)

After 3 days missing:
  - Confirm as desertion
  - Actually remove from roster: party.MemberRoster.AddToCounts(randomTroop, -1)
  - MissingCount--
  - AddNews("He's not coming back. Deserted.")
```

---

### 3. Condition Check Phase

#### New Sickness (Our Overlay)
```
Sickness Events = Random(0, MaxSicknessPerDay)

MaxSicknessPerDay = BaseSicknessRate × TotalSoldiers / 100 = 2 base

Result: SickCount += sicknessEvents
News: "Two soldiers report fever this morning."
```

Modifiers:
- Low Supplies: ×1.5
- Harsh weather: ×1.5
- Low Rest: ×1.3
- Near settlement (clean water): ×0.5
- High Supplies + Rest: ×0.3

#### New Injury (Real Game Wounds)

**This actually wounds troops using the game API:**

```
Injury Events = Random(0, MaxInjuryPerDay)

MaxInjuryPerDay = BaseInjuryRate × TotalSoldiers / 100 = 1 base

Result: 
  for each injury:
    roster.AddToCounts(randomTroop, count: 0, woundedCount: 1)

News: "A soldier broke his arm during drill."
```

Modifiers:
- Training active: ×1.5
- Marching rough terrain: ×1.3
- Combat yesterday: ×2.0 (delayed wound discovery)
- Resting in camp: ×0.3

**These wounded soldiers will heal naturally via the game's Medicine system.**

#### Desertion (Real Troop Removal)

```
Desertion Events = Random(0, MaxDesertionPerDay)

MaxDesertionPerDay = BaseDesertionRate × TotalSoldiers / 100 = 0.5 base
```

**Two-phase desertion:**

1. **Day 1: Soldier goes missing**
   ```csharp
   MissingCount++;
   AddNews("A soldier didn't report for roll call.");
   // Not removed yet - might return
   ```

2. **Day 3+: Confirmed desertion - ACTUALLY REMOVE**
   ```csharp
   MissingCount--;
   
   // REAL REMOVAL FROM PARTY
   var troop = GetRandomTroop(party.MemberRoster);
   party.MemberRoster.AddToCounts(troop, count: -1);
   
   AddNews("He's not coming back. Deserted.");
   ```

**This affects real party size.** The lord has fewer soldiers.

Modifiers:
- Morale < 30: ×3.0
- Morale < 50: ×1.5
- Pay overdue: ×2.0
- Discipline < 30: ×1.5
- Near friendly territory: ×1.3
- Morale > 70: ×0.2
- Recent victory: ×0.3

---

### 4. Incident Roll Phase

See [Background Incidents](#background-incidents) section.

---

### 5. Pulse Evaluation Phase

See [Pulse Events](#pulse-events) section.

---

### 6. News Generation Phase

All results from phases 1-5 are converted to news items and pushed to the feed.

**Priority Order:**
1. Deaths (always shown first)
2. Serious incidents
3. Roster changes (sickness, recovery)
4. Pulse changes
5. Background incidents (flavor)

**Maximum news items per day:** 5 (excess queued or summarized)

---

## Background Incidents

### Purpose

Small random events that show camp life. Most require no player action. They're flavor that makes the company feel alive.

### Incident Categories

#### Camp Life (40% weight)
| ID | Text | Effect |
|----|------|--------|
| `inc_campfire_song` | "Someone started a song by the fire. Others joined in." | Morale +1 |
| `inc_campfire_story` | "A veteran told stories of old campaigns tonight." | None |
| `inc_dog_adopted` | "A stray dog has attached itself to the company. Men call him 'Scraps'." | None (sets flag) |
| `inc_good_meal` | "The cook found fresh herbs. Tonight's stew was almost good." | Morale +1 |
| `inc_bad_meal` | "The stew was worse than usual. Men pushed it away." | Morale -1 |
| `inc_card_game` | "A card game ran late into the night." | None |
| `inc_dice_game` | "Dice rattled by the supply wagon until the sergeant broke it up." | None |
| `inc_letter_home` | "A merchant passed through. Several men sent letters home." | Morale +1 |
| `inc_rain_night` | "Rain hammered the tents all night. Everything is damp." | Rest -2 |
| `inc_clear_night` | "Clear skies and a bright moon. Good sleeping weather." | Rest +1 |

#### Discipline Issues (25% weight)
| ID | Text | Effect |
|----|------|--------|
| `inc_fight_minor` | "Two soldiers scuffled over something petty. Broken up quickly." | Discipline -1 |
| `inc_fight_serious` | "A real fight broke out. One man has a broken nose." | Discipline -2, +1 Walking Wounded |
| `inc_drunk` | "A soldier was found drunk on duty. Extra punishment assigned." | Discipline -1 |
| `inc_theft_minor` | "Someone stole food from the stores. Sergeant is investigating." | Supplies -1, Discipline -1 |
| `inc_theft_serious` | "Equipment went missing. Suspicion falls on everyone." | Equipment -2, Discipline -2 |
| `inc_insubordination` | "A soldier talked back to an officer. Put on extra duties." | Discipline -1 |
| `inc_punishment` | "A soldier was flogged this morning. The company watched in silence." | Discipline +3, Morale -2 |

#### Social (20% weight)
| ID | Text | Effect |
|----|------|--------|
| `inc_friendship` | "Two soldiers from different tents have become fast friends." | None |
| `inc_rivalry` | "A rivalry has developed between two fire groups." | None (sets flag) |
| `inc_romance` | "Two soldiers are keeping company. The sergeants pretend not to notice." | None |
| `inc_grudge` | "There's bad blood between two men. It may come to blows." | None (sets flag for future fight) |
| `inc_mentor` | "An older soldier has taken one of the recruits under his wing." | None |
| `inc_prayer` | "Some soldiers gathered for quiet prayer before dawn." | Morale +1 |
| `inc_gambling_win` | "Someone won big at dice. Buying drinks for his mates." | Morale +1 |
| `inc_gambling_loss` | "A soldier lost everything gambling. Now owes money." | None (sets flag) |

#### Discovery (10% weight)
| ID | Text | Effect |
|----|------|--------|
| `inc_stream` | "Soldiers found a clean stream nearby. Fresh water." | Supplies +2 |
| `inc_berries` | "Foragers found berry bushes. A small treat for the men." | Morale +1, Supplies +1 |
| `inc_game` | "Hunters brought back a deer. Fresh meat tonight." | Morale +2, Supplies +2 |
| `inc_ruins` | "Scouts found old ruins nearby. Nothing of value." | None |
| `inc_tracks` | "Strange tracks spotted near camp. Probably nothing." | None |

#### Problems (5% weight)
| ID | Text | Effect |
|----|------|--------|
| `inc_horse_lame` | "One of the horses went lame. Slowing the baggage." | Equipment -1 |
| `inc_wheel_broke` | "A wagon wheel broke. Repairs underway." | Equipment -2 |
| `inc_tent_torn` | "A tent ripped in the wind. Men crowding into others." | Rest -1 |
| `inc_well_bad` | "The water source was fouled. Had to move camp." | Rest -2, Supplies -1 |
| `inc_fire_accident` | "A campfire got out of control. Quickly stamped out." | None |

### Incident Generation

```
Daily Incident Count = Random(0, 2)

Selection:
1. Weight by category
2. Filter by current conditions (no "good meal" if Supplies < 20)
3. Check cooldowns (same incident can't fire within 3 days)
4. Apply effects
5. Generate news text
```

### Incident Flags

Some incidents set flags that affect future events or incidents:

| Flag | Set By | Used By |
|------|--------|---------|
| `has_camp_dog` | `inc_dog_adopted` | Dog appears in future events |
| `rivalry_active` | `inc_rivalry` | Increases fight chance |
| `grudge_X_Y` | `inc_grudge` | May trigger `inc_fight_serious` |
| `gambling_debt` | `inc_gambling_loss` | May trigger debt collection event |

---

## Pulse Events

### Purpose

Pulse events mark **threshold crossings** - when a company need or condition moves from one state to another. They provide narrative context for stat changes.

### Thresholds

| Need | Critical (<20) | Low (<40) | Normal (40-70) | Good (>70) | Excellent (>90) |
|------|----------------|-----------|----------------|------------|-----------------|
| Morale | Mutinous | Sullen | Steady | High | Jubilant |
| Supplies | Starving | Short | Adequate | Well-Stocked | Abundant |
| Rest | Exhausted | Tired | Rested | Fresh | Energized |
| Equipment | Broken | Worn | Serviceable | Good | Excellent |
| Readiness | Unfit | Shaky | Ready | Sharp | Elite |

### Pulse News (Threshold Crossings)

| Direction | Need | News Text |
|-----------|------|-----------|
| ↓ Enter Critical | Morale | "The mood is dark. Men speak in whispers. Something may break." |
| ↓ Enter Low | Morale | "Grumbling in the ranks. The men are restless." |
| ↑ Enter Good | Morale | "Spirits are lifting. Men joke and laugh again." |
| ↑ Enter Excellent | Morale | "The company is in fine spirits. Songs echo through camp." |
| ↓ Enter Critical | Supplies | "Starvation rations. Men eye the horses hungrily." |
| ↓ Enter Low | Supplies | "Rations cut. Belts tightened. Men count the days." |
| ↑ Enter Good | Supplies | "The stores are filling. Men eat their fill." |
| ↓ Enter Critical | Rest | "The company staggers. Men sleep standing up." |
| ↓ Enter Low | Rest | "Tired eyes everywhere. The pace is wearing them down." |
| ↑ Enter Good | Rest | "Well-rested and ready. The company looks sharp." |

### Pulse Generation Rules

- Only generate news when **crossing** a threshold (not every day at that level)
- One pulse event per need per day maximum
- Critical threshold crossings always generate news
- Minor crossings may be skipped if other news is more important

---

## Cascading Pressure

### The Build-Up

Background simulation creates **pressure** over time. Pressure builds until an event is required:

```
Pressure Track: LOW SUPPLIES
│
├── Day 1: Supplies drop below 50
│   └── News: "Rations are running short."
│
├── Day 3: Supplies below 40 for 3 days
│   └── News: "Half-rations ordered. Men grumble."
│   └── Effect: Sickness chance +20%
│
├── Day 5: Supplies below 30
│   └── News: "Soldiers eye the baggage train. Hunger gnaws."
│   └── Effect: Desertion chance +50%
│
├── Day 7: Supplies below 20 for 3 days
│   └── NEWS: "The men are starving. Something must be done."
│   └── TRIGGERS: evt_supply_crisis (mandatory event)
│
└── Event gives player options to resolve crisis
```

### Pressure Triggers

| Condition | Days Required | Triggers |
|-----------|---------------|----------|
| Supplies < 20 | 3 days | `evt_supply_crisis` |
| Morale < 20 | 3 days | `evt_morale_collapse` |
| Discipline < 20 | 3 days | `evt_discipline_breakdown` |
| Rest < 15 | 2 days | `evt_exhaustion_crisis` |
| Sick/Injured > 20% of company | Any | `evt_epidemic` |
| Desertion > 5 in 3 days | 3 days | `evt_desertion_wave` |

### Crisis Events

Crisis events are **mandatory** - player must respond. They're triggered by the simulation when pressure exceeds thresholds.

These events live in the normal event system but are queued by `CompanySimulationBehavior` rather than the content orchestrator.

---

## News Feed Integration

### Using Existing EnlistedNewsBehavior

**This integrates with the existing news system** documented in [News Reporting System](../Features/UI/news-reporting-system.md).

```csharp
// In CompanySimulationBehavior, after generating simulation results:

private void PushToNewsFeed(SimulationDayResult result)
{
    var news = EnlistedNewsBehavior.Instance;
    
    // Deaths (always show - Critical severity)
    foreach (var death in result.Deaths)
    {
        news.AddCampNews(
            text: death.NewsText,           // "A soldier died from fever."
            severity: "critical",
            category: "roster"
        );
    }
    
    // Roster changes (Notable severity)
    foreach (var change in result.RosterChanges)
    {
        news.AddCampNews(
            text: change.NewsText,          // "Two soldiers report sick."
            severity: "notable",
            category: "roster"
        );
    }
    
    // Incidents (Minor/Flavor severity)
    foreach (var incident in result.Incidents)
    {
        news.AddCampNews(
            text: incident.NewsText,        // "Dice game by the fire."
            severity: incident.Severity,    // "minor" or "flavor"
            category: "incident"
        );
    }
    
    // Pulse events (threshold crossings)
    foreach (var pulse in result.PulseEvents)
    {
        news.AddCampNews(
            text: pulse.NewsText,           // "Grumbling in the ranks."
            severity: "notable",
            category: "pulse"
        );
    }
    
    // Update company status section
    news.UpdateCompanyStatus(
        fitCount: _roster.FitForDuty,
        totalCount: _roster.TotalSoldiers,
        changeFromYesterday: result.NetStrengthChange,
        statusText: GetPulseText()          // "The men are restless."
    );
}
```

### New API Methods for EnlistedNewsBehavior

Add these methods to the existing `EnlistedNewsBehavior`:

```csharp
/// <summary>
/// Add camp simulation news to the CAMP section of the feed.
/// </summary>
public void AddCampNews(string text, string severity, string category)
{
    var item = new DispatchItem
    {
        Text = text,
        Severity = severity,
        Category = category,
        DayNumber = (int)CampaignTime.Now.ToDays,
        Source = "simulation"
    };
    
    _campFeed.Insert(0, item);
    TrimFeed(_campFeed, MaxCampFeedItems);
}

/// <summary>
/// Update the COMPANY STATUS section shown in the menu.
/// </summary>
public void UpdateCompanyStatus(int fitCount, int totalCount, int changeFromYesterday, string statusText)
{
    _companyFitCount = fitCount;
    _companyTotalCount = totalCount;
    _companyChange = changeFromYesterday;
    _companyStatusText = statusText;
}
```

### Feed Structure

The simulation populates the **CAMP** section of the existing news feed:

```
═══ CAMP ═══
• [Serious] A soldier died in the night. Fever took him.
• [Notable] Two soldiers report sick this morning.
• [Minor] Rain overnight. Tents are damp.
• [Flavor] Someone started a song by the fire.

═══ COMPANY ═══  
• Strength: 106 fit / 127 total (-2 from yesterday)
• Morale: Slipping ↓ (entered Low threshold)
• Supplies: Short (3 days at current consumption)
• Status: "Grumbling in the ranks. Men are restless."
```

### Severity Levels

| Severity | Examples | Display |
|----------|----------|---------|
| Critical | Death, crisis trigger | Always shown, highlighted |
| Serious | Serious injury, desertion, major fight | Always shown |
| Notable | Sickness, recovery, threshold crossing | Shown (may be summarized) |
| Minor | Weather, small incidents | Shown if space |
| Flavor | Songs, stories, discoveries | Shown if space, may skip |

### Summarization

If more than 5 news items in a day:
- Show all Critical/Serious items
- Summarize Notable: "3 soldiers fell ill. 1 recovered."
- Group Minor/Flavor: "Camp was busy with small incidents."

### Related News System Docs

- [News Reporting System](../Features/UI/news-reporting-system.md) - Full news system architecture
- [Event & Order Outcome Queue](../Features/UI/news-reporting-system.md#event--order-outcome-queue-system) - How event results display

---

## Data Model

### CompanyRoster

```csharp
/// <summary>
/// Reads real game data + our overlay tracking.
/// Most data comes from actual TroopRoster - we just add Sick/Missing/Dead.
/// </summary>
public class CompanyRoster
{
    private readonly MobileParty _party;
    
    // Our overlay tracking (saved)
    public int SickCount { get; set; }      // Abstract sickness, separate from wounds
    public int MissingCount { get; set; }   // Deserted but not yet removed
    public int DeadThisCampaign { get; set; }
    
    // Read from game (not saved - read live)
    public int TotalSoldiers => _party.MemberRoster.TotalManCount;
    public int WoundedCount => _party.MemberRoster.TotalWounded;
    public int FitForDuty => TotalSoldiers - WoundedCount - SickCount;
    
    // Calculated
    public float CasualtyRate => (float)(WoundedCount + SickCount) / TotalSoldiers;
    
    /// <summary>
    /// Wound troops using game API. They'll heal via native Medicine system.
    /// </summary>
    public void WoundRandomTroop(int count = 1)
    {
        // Pick random troop type from roster and add as wounded
        var roster = _party.MemberRoster;
        for (int i = 0; i < count; i++)
        {
            var troop = GetRandomHealthyTroop(roster);
            if (troop != null)
            {
                roster.AddToCounts(troop, count: 0, woundedCount: 1);
            }
        }
    }
    
    /// <summary>
    /// Remove troops for desertion. ACTUALLY REMOVES FROM PARTY.
    /// </summary>
    public void RemoveDeserter(int count = 1)
    {
        var roster = _party.MemberRoster;
        for (int i = 0; i < count; i++)
        {
            var troop = GetRandomNonHeroTroop(roster);
            if (troop != null)
            {
                roster.AddToCounts(troop, count: -1);  // REAL REMOVAL
            }
        }
    }
    
    /// <summary>
    /// Kill a sick soldier. ACTUALLY REMOVES FROM PARTY.
    /// </summary>
    public void KillSickSoldier()
    {
        if (SickCount <= 0) return;
        
        SickCount--;
        DeadThisCampaign++;
        
        var roster = _party.MemberRoster;
        var troop = GetRandomNonHeroTroop(roster);
        if (troop != null)
        {
            roster.AddToCounts(troop, count: -1);  // REAL REMOVAL
        }
    }
    
    /// <summary>
    /// Get a random non-hero troop for removal.
    /// Never removes heroes or the lord.
    /// </summary>
    private CharacterObject GetRandomNonHeroTroop(TroopRoster roster)
    {
        // Filter to regular troops only (not heroes)
        var regulars = roster.GetTroopRoster()
            .Where(e => !e.Character.IsHero && e.Number > 0)
            .ToList();
            
        if (regulars.Count == 0) return null;
        
        // Weight by count - more of a troop type = more likely to lose one
        int totalCount = regulars.Sum(e => e.Number);
        int roll = MBRandom.RandomInt(totalCount);
        
        int cumulative = 0;
        foreach (var element in regulars)
        {
            cumulative += element.Number;
            if (roll < cumulative)
                return element.Character;
        }
        
        return regulars[0].Character;
    }
}
```

**Key Point:** We don't duplicate what the game already tracks. We READ wounded count from the real roster and ADD our own overlays (Sick, Missing, Dead counter).

### Optional: Custom Healing Rate Model

If we want to affect how fast wounded troops heal (based on our systems), we can create a custom `PartyHealingModel`:

```csharp
/// <summary>
/// Modifies native healing rate based on our company systems.
/// </summary>
public class EnlistedPartyHealingModel : DefaultPartyHealingModel
{
    public override ExplainedNumber GetDailyHealingForRegulars(
        PartyBase partyBase, 
        bool isPrisoner, 
        bool includeDescriptions = false)
    {
        var result = base.GetDailyHealingForRegulars(partyBase, isPrisoner, includeDescriptions);
        
        if (!isPrisoner && IsEnlistedLordParty(partyBase))
        {
            // Modify based on our systems
            float supplies = CompanyNeedsManager.Instance.GetNeed(CompanyNeed.Supplies);
            float rest = CompanyNeedsManager.Instance.GetNeed(CompanyNeed.Rest);
            
            // Low supplies = slower healing
            if (supplies < 30)
                result.AddFactor(-0.3f, new TextObject("Low supplies"));
            
            // Well-rested = faster healing
            if (rest > 70)
                result.AddFactor(0.2f, new TextObject("Well-rested company"));
            
            // Medical order active = bonus
            if (OrderManager.Instance.HasActiveOrder("order_treat_wounded"))
                result.AddFactor(0.5f, new TextObject("Medical attention"));
        }
        
        return result;
    }
}
```

**Registration:** Add to `SubModule.xml` game models or replace via Harmony.

### CompanyPressure

```csharp
/// <summary>
/// Tracks pressure build-up for crisis triggers.
/// </summary>
[Serializable]
public class CompanyPressure
{
    public int DaysLowSupplies { get; set; }
    public int DaysLowMorale { get; set; }
    public int DaysLowRest { get; set; }
    public int DaysLowDiscipline { get; set; }
    public int RecentDesertions { get; set; }
    public int DaysHighSickness { get; set; }
    
    public void ResetCounter(string counter)
    {
        // Called when condition improves above threshold
    }
}
```

### CampIncident

```csharp
/// <summary>
/// A background incident that occurred.
/// </summary>
public struct CampIncident
{
    public string Id;              // "inc_fight_minor"
    public string Category;        // "discipline", "social", "discovery"
    public string Severity;        // "flavor", "minor", "notable", "serious"
    public string NewsTextId;      // Localization key
    public string NewsTextFallback; // Fallback text
    public Dictionary<string, int> Effects;  // { "Morale": -1, "Discipline": -2 }
}
```

### SimulationDayResult

```csharp
/// <summary>
/// Result of daily simulation tick for news generation.
/// </summary>
public class SimulationDayResult
{
    public List<RosterChange> RosterChanges { get; }      // Who got sick, recovered, died
    public List<CampIncident> Incidents { get; }          // Random incidents
    public List<PulseEvent> PulseEvents { get; }          // Threshold crossings
    public List<string> TriggeredCrises { get; }          // Crisis events to queue
    public Dictionary<string, int> NeedChanges { get; }   // Net changes to company needs
}
```

---

## Technical Architecture

### CompanySimulationBehavior

```csharp
/// <summary>
/// Runs the background company simulation each day.
/// </summary>
public class CompanySimulationBehavior : CampaignBehaviorBase
{
    // State
    private CompanyRoster _roster;
    private CompanyPressure _pressure;
    private HashSet<string> _activeFlags;
    private Dictionary<string, int> _incidentCooldowns;
    
    // Configuration
    private SimulationConfig _config;
    
    // Dependencies
    private CompanyNeedsManager _needs;
    private EscalationManager _escalation;
    private EnlistedNewsBehavior _news;
    private ContentOrchestrator _orchestrator;
    
    public override void RegisterEvents()
    {
        CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, OnBattleEnd);
    }
    
    private void OnDailyTick()
    {
        var result = new SimulationDayResult();
        
        // Phase 1: Consumption
        ProcessConsumption(result);
        
        // Phase 2: Roster Updates
        ProcessRosterRecovery(result);
        
        // Phase 3: Condition Checks
        ProcessNewConditions(result);
        
        // Phase 4: Incidents
        ProcessIncidents(result);
        
        // Phase 5: Pulse
        ProcessPulse(result);
        
        // Phase 6: News - Push to EnlistedNewsBehavior
        GenerateNews(result);
        
        // Check crisis triggers
        CheckCrisisTriggers(result);
    }
    
    /// <summary>
    /// Push simulation results to the news system.
    /// See News Feed Integration section for full API.
    /// </summary>
    private void GenerateNews(SimulationDayResult result)
    {
        // Deaths - always show (critical)
        foreach (var death in result.Deaths)
            _news.AddCampNews(death.NewsText, "critical", "roster");
        
        // Roster changes - notable
        foreach (var change in result.RosterChanges)
            _news.AddCampNews(change.NewsText, "notable", "roster");
        
        // Incidents - minor/flavor
        foreach (var incident in result.Incidents)
            _news.AddCampNews(incident.NewsText, incident.Severity, "incident");
        
        // Pulse events - notable
        foreach (var pulse in result.PulseEvents)
            _news.AddCampNews(pulse.NewsText, "notable", "pulse");
        
        // Update company status
        _news.UpdateCompanyStatus(
            _roster.FitForDuty, 
            _roster.TotalSoldiers,
            result.NetStrengthChange,
            GetPulseText()
        );
    }
    
    private void OnBattleEnd(MapEvent mapEvent)
    {
        // Add casualties to roster based on battle outcome
        ProcessBattleCasualties(mapEvent);
    }
}
```

### Integration Points

```
┌────────────────────────┐
│  CompanyNeedsManager   │ ← Reads current needs
│  (existing)            │ ← Updates needs from simulation
└────────────────────────┘
            │
            ▼
┌────────────────────────┐
│ CompanySimulationBehavior │ ← Runs daily tick
│ (new)                     │ ← Generates incidents
└────────────────────────┘  │ ← Tracks roster
            │               │ ← Builds pressure
            ▼               ▼
┌────────────────────────┐  ┌────────────────────────┐
│  EnlistedNewsBehavior  │  │   ContentOrchestrator  │
│  (existing)            │  │   (existing)           │
│  ← Receives camp news  │  │   ← Receives crisis    │
└────────────────────────┘  │     event triggers     │
                            └────────────────────────┘
```

### Save Data

```csharp
public class CompanySimulationSaveData
{
    public CompanyRoster Roster;
    public CompanyPressure Pressure;
    public List<string> ActiveFlags;
    public Dictionary<string, int> IncidentCooldowns;
    public int LastTickDay;
}
```

---

## Example Days

### Day 14: Normal Camp Day

```
[Dawn Tick Runs]

Consumption:
- Supplies: 52 → 49 (-3 for 127 soldiers)
- Equipment: 61 → 60 (-1 normal degradation)

Roster Updates:
- 1 Sick soldier recovered → Fit
- 1 Walking Wounded fully healed → Fit

New Conditions:
- 0 new sickness (good conditions)
- 0 new injuries (resting in camp)
- 0 desertions (morale steady)

Incidents (rolled 2):
- inc_dice_game: "Dice rattled by the supply wagon..."
- inc_good_meal: "The cook found fresh herbs..."

Pulse:
- Supplies crossed below 50 threshold
  → "Rations are running short."

═══ NEWS OUTPUT ═══

CAMP:
• Rations are running short.
• A soldier recovered from illness. Back on duty.
• A wounded man is fully healed.
• The cook found fresh herbs. Tonight's stew was almost good.

COMPANY:
• Strength: 110 fit / 127 total (+2 from yesterday)
• Supplies: Short (entering Low threshold)
```

---

### Day 21: Bad Day

```
[Dawn Tick Runs]

Consumption:
- Supplies: 28 → 24 (-4, marching)
- Equipment: 45 → 42 (-3, rough terrain)

Roster Updates:
- 1 Sick soldier died (low supplies, no medical care)
- 0 recoveries (conditions too harsh)

New Conditions:
- 2 new sickness (low supplies, harsh terrain)
- 1 new injury (marching accident)
- 1 desertion (morale dropped, near friendly territory)

Incidents (rolled 1):
- inc_theft_minor: "Someone stole food from the stores..."

Pulse:
- Morale crossed below 40 threshold
  → "Grumbling in the ranks. The men are restless."

Pressure:
- DaysLowSupplies: 3 → 4
- RecentDesertions: 2 → 3

═══ NEWS OUTPUT ═══

CAMP:
• [DEATH] A soldier died in the night. Fever took him.
• A soldier slipped away in the darkness. His tent is empty.
• Two soldiers report sick this morning.
• A soldier twisted his ankle on the march.
• Someone stole food from the stores.

COMPANY:
• Strength: 105 fit / 125 total (-5 from yesterday)
• Dead: 15 this campaign (+1)
• Morale: Sullen ↓ (entered Low threshold)
• Supplies: Short (4 days at this rate)
• Status: "Grumbling in the ranks. The men are restless."
```

---

### Day 24: Crisis Triggered

```
[Dawn Tick Runs]

Pressure Check:
- DaysLowSupplies: 7 (threshold: 3 days at <20)
- Supplies currently: 18

CRISIS TRIGGERED: evt_supply_crisis

═══ NEWS OUTPUT ═══

CAMP:
• [CRISIS] The men are starving. Something must be done.
• Hungry eyes watch the officers' tent.
• A soldier collapsed from weakness.

[Crisis event queued - player will be forced to respond]
```

---

## Configuration

### SimulationConfig.json

```json
{
  "consumption": {
    "baseSupplyRate": 0.5,
    "baseEquipmentRate": 0.2,
    "marchingMultiplier": 1.2,
    "combatMultiplier": 1.5,
    "restingMultiplier": 0.8
  },
  "roster": {
    "baseRecoveryChance": 0.15,
    "baseDeathChance": 0.02,
    "baseSicknessRate": 2,
    "baseInjuryRate": 1,
    "baseDesertionRate": 0.5
  },
  "incidents": {
    "minPerDay": 0,
    "maxPerDay": 2,
    "cooldownDays": 3
  },
  "pressure": {
    "supplysCrisisDays": 3,
    "supplyCrisisThreshold": 20,
    "moraleCrisisDays": 3,
    "moraleCrisisThreshold": 20,
    "disciplineCrisisDays": 3,
    "disciplineCrisisThreshold": 20
  },
  "news": {
    "maxItemsPerDay": 5,
    "alwaysShowDeaths": true,
    "summarizeThreshold": 3
  }
}
```

---

## Implementation Tasks

### Phase 1: Core Data Structures ✅
- [x] Create `CompanyRoster` class - `src/Features/Camp/Models/CompanyRoster.cs`
- [x] Create `CompanyPressure` class - `src/Features/Camp/Models/CompanyPressure.cs`
- [x] Create `CampIncident` struct - `src/Features/Camp/Models/CampIncident.cs`
- [x] Create `SimulationDayResult` class - `src/Features/Camp/Models/SimulationDayResult.cs`
- [x] Add save/load support for simulation state - in `CompanySimulationBehavior.SyncData()`
- [x] Create `SimulationConfig` and JSON loader - `ModuleData/Enlisted/simulation_config.json`

### Phase 2: Roster Simulation ✅
- [x] Implement roster initialization (from party size)
- [x] Implement recovery/death rolls
- [x] Implement sickness/injury generation
- [x] Implement desertion logic (two-phase missing → confirmed)
- [x] Hook battle casualties to roster (`OnBattleEnd`)
- [x] Generate roster news items

### Phase 3: Incident System ✅
- [x] Create incident definitions JSON - `simulation_config.json`
- [x] Implement incident selection with weights
- [x] Implement cooldown tracking
- [x] Implement flag system (`_activeFlags`, `SetsFlag`, `RequiresFlag`)
- [x] Generate incident news items

### Phase 4: Consumption & Pressure ✅
- [x] Implement supply consumption (reads from CompanyNeedsManager)
- [x] Implement equipment degradation (via incident effects)
- [x] Implement pressure tracking (`CompanyPressure` class)
- [x] Implement pulse threshold detection (`ProcessPulse()`)
- [x] Generate consumption/pulse news

### Phase 5: Crisis Triggers ✅
- [x] Define crisis event IDs (supply_crisis, morale_collapse, etc.)
- [x] Implement pressure → crisis trigger logic (`CheckCrisisTriggers()`)
- [x] Queue crisis events to orchestrator (`QueueCrisisEvent()`)
- [ ] Create crisis event JSON files - *pending: content authoring*

### Phase 6: News Integration ✅
- [x] Add camp news methods to `EnlistedNewsBehavior` (`AddCampNews()`, `UpdateCompanyStatus()`)
- [x] Implement severity prioritization
- [x] Implement max news cap (5 items/day)
- [x] Update `CampNewsState` with company status fields

### Phase 7: Testing & Tuning
- [ ] Playtest for 30 campaign days
- [ ] Tune rates to feel natural
- [ ] Ensure no news spam
- [ ] Verify crisis triggers at appropriate times

---

## Edge Cases & Special States

### Party Size Edge Cases

#### Empty Party (Lord Only)
```csharp
if (party.MemberRoster.TotalRegulars == 0)
{
    // Skip simulation - no troops to simulate
    // Still show news: "You travel alone."
    return;
}
```

#### Very Small Party (< 10 troops)
- Reduce incident frequency (less people = less drama)
- Desertion more impactful (losing 1 of 5 is 20%)
- Sickness spread reduced

#### Very Large Party (> 200 troops)
- Cap absolute numbers, not percentages
- Max 3 sick per day (not 2% of 500 = 10)
- Summarize news: "Several soldiers fell ill" not individual reports

#### All Troops Wounded/Sick
```csharp
if (roster.FitForDuty <= 0)
{
    // Crisis state - everyone is down
    _orchestrator.QueueCrisisEvent("evt_company_incapacitated");
    
    // No further attrition rolls (can't get worse)
    // Focus on recovery news
}
```

---

### Player State Edge Cases

#### Player is Prisoner
```csharp
if (Hero.MainHero.IsPrisoner)
{
    // Don't simulate lord's old party
    // They continue without player - maybe news about them?
    // Or skip entirely until freed
    return;
}
```

#### Player in Battle/Siege
```csharp
if (party.MapEvent != null || party.SiegeEvent != null)
{
    // Skip daily simulation during active combat
    // Battle casualties handled separately by OnBattleEnd
    return;
}
```

#### Player Changed Lords (Reassignment)
```csharp
// On lord change, reset simulation state
public void OnLordChanged(Hero newLord)
{
    _roster = new CompanyRoster(newLord.PartyBelongedTo);
    _pressure = new CompanyPressure();  // Fresh start
    DeadThisCampaign = 0;  // New company, new count
}
```

---

### Roster Protection

#### Never Remove Heroes
```csharp
private CharacterObject GetRandomNonHeroTroop(TroopRoster roster)
{
    // CRITICAL: Filter out all heroes
    var regulars = roster.GetTroopRoster()
        .Where(e => !e.Character.IsHero && e.Number > 0)
        .ToList();
    
    if (regulars.Count == 0)
    {
        // Only heroes left - cannot remove anyone
        ModLogger.Warn("Orchestrator", "No regular troops to remove - only heroes remain");
        return null;
    }
    
    // ... selection logic
}
```

#### Minimum Troop Count
```csharp
// Don't reduce below a minimum (prevents total wipe)
private const int MinimumTroopCount = 5;

if (roster.TotalRegulars <= MinimumTroopCount)
{
    // No more deaths/desertions
    // Still allow sickness (recoverable)
    // Add news: "The company is dangerously thin."
}
```

---

### State Consistency

#### Negative Number Guards
```csharp
// All decrements must guard against negatives
public void DecrementSick()
{
    SickCount = Math.Max(0, SickCount - 1);
}

public void RemoveDeserter()
{
    if (MissingCount <= 0) return;
    MissingCount--;
    // ... actual removal
}
```

#### Sync with Game Roster
```csharp
// On each tick, verify our counts make sense
private void ValidateState()
{
    int actualTotal = _party.MemberRoster.TotalManCount;
    int actualWounded = _party.MemberRoster.TotalWounded;
    
    // Sick can't exceed total - wounded
    if (SickCount > actualTotal - actualWounded)
    {
        ModLogger.Warn("Orchestrator", 
            $"SickCount {SickCount} exceeds available {actualTotal - actualWounded}, clamping");
        SickCount = Math.Max(0, actualTotal - actualWounded);
    }
    
    // Missing should be resolved, not accumulating forever
    if (MissingCount > 10)
    {
        ModLogger.Warn("Orchestrator", "Too many missing - forcing resolution");
        ResolveMissingAsDeserted();
    }
}
```

---

### Save/Load

#### State Persistence
```csharp
public override void SyncData(IDataStore dataStore)
{
    // Save our overlay data
    dataStore.SyncData("simulation_sick", ref _sickCount);
    dataStore.SyncData("simulation_missing", ref _missingCount);
    dataStore.SyncData("simulation_dead", ref _deadThisCampaign);
    dataStore.SyncData("simulation_pressure", ref _pressure);
    dataStore.SyncData("simulation_flags", ref _activeFlags);
    dataStore.SyncData("simulation_cooldowns", ref _incidentCooldowns);
    dataStore.SyncData("simulation_lastWounded", ref _lastTickWounded);
}
```

#### New Game Initialization
```csharp
private void InitializeForNewGame()
{
    var party = GetEnlistedLordParty();
    if (party == null) return;
    
    _roster = new CompanyRoster(party);
    _pressure = new CompanyPressure();
    _activeFlags = new HashSet<string>();
    _incidentCooldowns = new Dictionary<string, int>();
    _lastTickWounded = party.MemberRoster.TotalWounded;
    DeadThisCampaign = 0;
}
```

#### Save Corruption Recovery
```csharp
// If loaded data is invalid, reset gracefully
if (_sickCount < 0 || _missingCount < 0 || _pressure == null)
{
    ModLogger.Warn("Orchestrator", "Invalid simulation state loaded - resetting");
    InitializeForNewGame();
}
```

---

### Timing Edge Cases

#### Fast Time Skip
```csharp
// If time jumps more than 1 day (fast forward, waiting)
public void OnHourlyTick()
{
    int currentDay = (int)CampaignTime.Now.ToDays;
    if (currentDay > _lastProcessedDay + 1)
    {
        // Missed days - process in batch or skip
        int missedDays = currentDay - _lastProcessedDay - 1;
        
        if (missedDays > 7)
        {
            // Too many to simulate - just apply summary effects
            ApplyBulkTimeskipEffects(missedDays);
        }
        else
        {
            // Simulate each missed day
            for (int i = 0; i < missedDays; i++)
            {
                ProcessDailyTick();
            }
        }
    }
    _lastProcessedDay = currentDay;
}
```

#### Campaign Pause/Resume
```csharp
// Don't simulate during menu, character creation, etc.
if (!Campaign.Current.GameStarted || Campaign.Current.IsPaused)
{
    return;
}
```

---

### Mod Compatibility

#### Other Mods Modifying Roster
```csharp
// Don't assume roster is stable between ticks
// Always read fresh from game API, don't cache

// BAD:
private int _cachedTotalTroops;  // May be stale

// GOOD:
public int TotalTroops => _party.MemberRoster.TotalManCount;  // Live read
```

#### Concurrent Modifications
```csharp
// If another system modified roster same tick, our counts may be off
// Validate before acting

if (roster.TotalRegulars != _expectedCount)
{
    ModLogger.Debug("Orchestrator", "Roster changed externally - resyncing");
    ValidateState();
}
```

---

### Summary: Critical Guards

| Edge Case | Guard |
|-----------|-------|
| Empty party | Skip simulation |
| All incapacitated | Crisis event, stop attrition |
| Player prisoner | Skip simulation |
| Player in battle | Skip simulation |
| Only heroes left | Cannot remove, warn |
| Minimum troops | Stop deaths/desertions |
| Negative counts | Math.Max(0, value) |
| Save corruption | Reset to valid state |
| Time skip | Batch process or summarize |
| Mod conflicts | Read live, validate often |

---

## Future Enhancements

### Named NPCs
Instead of anonymous categories, track a few named soldiers:
- "Finn the Archer" - appears in events
- Build relationships with recurring characters
- Deaths of named soldiers have more impact

### Fire Group System
Assign player to a tent group:
- 6-8 recurring tent-mates
- Shared meals, shared incidents
- Fire group morale separate from company morale

### Seasonal Effects
Weather and season affect simulation:
- Winter: Higher sickness, lower morale
- Summer: Lower sickness, heat exhaustion risk
- Harvest: Easier foraging

### Enemy Pressure
When near enemies:
- Scouts go missing
- Tension incidents increase
- Sleep quality decreases

---

## Related Documentation

- [Camp Life Simulation](camp-life-simulation.md) - Player-facing camp opportunities
- [News Reporting System](../Features/UI/news-reporting-system.md) - News feed display
- [Company Needs](../Features/Core/company-needs.md) - The five needs this affects
- [Escalation System](../Features/Core/escalation-system.md) - Discipline/scrutiny tracks
- [Content Orchestrator](content-orchestrator-plan.md) - Event triggering system
