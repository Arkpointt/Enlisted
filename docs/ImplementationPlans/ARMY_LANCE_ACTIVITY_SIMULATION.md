# Army Lance Activity Simulation System

**Status:** Design Phase  
**Category:** Immersion & World-Building Feature  
**Priority:** 🟡 Medium (Should-Have for rich experience)  
**Dependencies:** AI Camp Schedule, Lance Life Simulation, News/Dispatches System  
**Related Docs:** `ai-camp-schedule.md`, `lance-life-simulation.md`, `news-dispatches.md`, `MASTER_IMPLEMENTATION_ROADMAP.md`

---

## Table of Contents

1. [Overview](#overview)
2. [Core Concept](#core-concept)
3. [Design Philosophy](#design-philosophy)
4. [System Architecture](#system-architecture)
5. [Lance Activity Types](#lance-activity-types)
6. [State Simulation](#state-simulation)
7. [Player Impact Scenarios](#player-impact-scenarios)
8. [News & Bulletin Integration](#news--bulletin-integration)
9. [Cover Request System](#cover-request-system)
10. [Rumor & Intelligence System](#rumor--intelligence-system)
11. [Time-of-Day Scheduling](#time-of-day-scheduling)
12. [Technical Implementation](#technical-implementation)
13. [Configuration](#configuration)
14. [Content Creation](#content-creation)
15. [Implementation Phases](#implementation-phases)
16. [Integration Points](#integration-points)
17. [Testing Strategy](#testing-strategy)
18. [Future Enhancements](#future-enhancements)

---

## Overview

The Army Lance Activity Simulation creates the feeling that **you're part of a larger military organization**, not the only lance that matters. Other lances in your army are constantly engaged in activities - patrols, foraging, guard duty, training, maintenance - and these activities affect the player's experience.

**Core Experience:**
- You open the camp bulletin and see: *"Lance 'The Bold Hawks' dispatched on patrol near Marunath."*
- The next day: *"2nd Lance short-handed due to injuries - extra shifts for remaining units."*
- An inquiry arrives: *"Lance 'Stormborn Riders' leader requests your lance cover their guard duty - they're still recovering from last week's skirmish."*
- During a battle: *"3rd Lance delayed - still on foraging detail 2 days out."*

**Key Philosophy:** The player is **along for the ride** in a living military machine. They can influence things (accepting cover requests, checking the bulletin board, being strategic about timing) but they're not micromanaging every lance. The world moves with or without their input.

---

## ⚠️ Important: What This System Does and Doesn't Do

### ✅ This System Simulates: "The Small Stuff"
- **Patrols** going out and sometimes encountering bandits/scouts (small skirmishes)
- **Foraging missions** that might get ambushed by looters
- **Guard duty** accidents and minor incidents
- **Scouting missions** where lances spot enemy activity
- **Tax collection** where villagers resist or bandits attack
- **Training** accidents
- **Disease** and sickness in camp
- **Desertion** and discipline issues

**In short:** The everyday life of soldiers doing routine military work.

### ❌ This System Does NOT Simulate:
- **Full battles** (we use Bannerlord's normal battle system)
- **Large-scale combat** (that's handled by real battles)
- **Army vs Army engagements** (real battles only)

### How It Works Together:
1. Lances do routine work (patrols, foraging, etc.)
2. Sometimes they encounter **small groups of enemies** (5-15 bandits, a few scouts)
3. These small encounters cause casualties (1-5 troops typically)
4. When a **real battle** happens (Bannerlord's battle), the army is weaker due to accumulated casualties from routine operations

**Think of it like this:** Real armies don't just lose men in battles. They lose men to patrol ambushes, foraging accidents, disease, desertion, and a hundred small incidents. That's what we simulate. The big battles are handled by Bannerlord's normal systems.

---

## Core Concept

### The Living Army

Your lord's army isn't just the player's lance. It's composed of **multiple lances** (8-15 typically), each with:
- **Named identity** (e.g., "The Ironwood Guard", "Derthert's Fist")
- **Active assignments** (patrol, foraging, guard duty, training, rest)
- **Current status** (ready, undermanned, injured, sick, exhausted)
- **Location** (in camp, on assignment nearby, detached)

### What Gets Simulated

**Lance Activities:**
- **Wartime:** Patrols, scouting, foraging, requisitioning, picket duty, convoy escort, outpost garrison
- **Peacetime:** Training exercises, recruitment drives, tax collection, bandit suppression, village protection, road patrols
- **Universal:** Guard duty, equipment maintenance, rest and recovery, medical tent, disciplinary actions

**Lance States:**
- **Readiness:** Combat effectiveness (injuries, equipment, morale, rest)
- **Availability:** Can they be deployed? (on assignment, in recovery, under disciplined)
- **Reputation:** Known for reliability, recklessness, skill, or problems

**Lance Events:**
- **Success:** Mission completed, enemy contacts defeated, supplies secured
- **Complications:** Delays, minor injuries, equipment damage, discipline issues
- **Failures:** Ambushed, mission failed, heavy casualties, lance returns undermanned
- **Rare:** Heroic action, desertion, disease outbreak, lance leader killed

### What We DON'T Simulate

**Out of Scope:**
- **Battles themselves** (we use Bannerlord's normal battle system, not simulated combat)
- Individual personalities for every lance member (only player's lance has Persistent Lance Leaders with memory/personality)
- Detailed equipment inventory per NPC lance
- Inter-lance rivalries (unless it becomes a dedicated social feature)
- Player micromanagement of other lances' assignments

**Why:** Keep simulation lightweight and focused on **impact on player**, not complex NPC management.

**Clarification:** This system simulates **routine military life** - patrols, foraging, guard duty, etc. When lances encounter enemies during these activities, they may take small casualties (skirmishes, not full battles). When a **real battle** happens (Bannerlord's normal battle system), the army is affected by these accumulated routine losses.

---

## Design Philosophy

### 1. The Player is Along for the Ride

The player **experiences** a living military organization but doesn't **micromanage** it. Other lances are assigned duties by the army leadership (the lord, marshal, quartermaster). The player:
- ✅ Sees the consequences (cover requests, camp bulletin updates, undermanned conditions)
- ✅ Can make choices when asked (accept/decline cover requests)
- ✅ Can gather intelligence (check bulletin board, talk to lance leaders)
- ❌ Cannot assign other lances to duties (that's the lord's job)
- ❌ Cannot control other lance schedules

**Design Goal:** Create immersion through **observation and reaction**, not control.

---

### 2. Systemic Impact Over Individual Detail

We care about lance activities **when they affect the player:**
- **Cover Requests:** "Can you cover guard duty? Our lance is still recovering."
- **Resource Strain:** "Multiple lances on foraging detail - camp supplies tight this week."
- **Combat Availability:** "3rd Lance still out on patrol - battle will start without them."
- **Morale Effects:** "2nd Lance took heavy losses on that tax collection run - morale is shaken."

If a lance is out on patrol and nothing notable happens, we don't generate spam. We generate content **when it creates player experience**.

---

### 3. Event-Driven, Not Tick-Driven

Following the established pattern (News System, Lance Life Simulation), we:
- ✅ React to campaign events (battles, days passing, army moving)
- ✅ Generate activities during daily processing
- ✅ Cache state changes for bulletin updates
- ❌ Don't scan every frame
- ❌ Don't create events that have no player-facing impact

---

### 4. Integrate with Existing Systems

This system is a **bridge** between existing systems:
- **AI Camp Schedule:** Determines which duties need filling → informs which lances are on what schedule
- **Lance Life Simulation:** Injuries/sickness in player's lance → can happen to other lances too
- **News/Dispatches System:** Primary UI for communicating lance activities to player
- **Cover Request Events:** Existing event infrastructure for player decisions

**Design Goal:** Don't recreate wheels. Extend existing systems to cover "other lances in your army."

---

## System Architecture

### High-Level Overview

```
Lord's Army
    ↓
Army Lance Roster (8-15 simulated lances)
    ↓
    ├─→ Lance Identity (name, tier, size)
    ├─→ Lance Assignment (current duty, time remaining)
    ├─→ Lance State (readiness, availability, status effects)
    └─→ Lance History (recent events, notable outcomes)
        ↓
Daily Processing
    ↓
    ├─→ Advance Assignments (time progress)
    ├─→ Generate Events (based on duty type, context, chance)
    ├─→ Update States (injuries, exhaustion, equipment)
    ├─→ Generate Consequences (cover requests, resource strain, morale)
    └─→ Feed to News System (bulletin updates)
        ↓
Player Experience
    ↓
    ├─→ Camp Bulletin (read about lance activities)
    ├─→ Cover Requests (decision events)
    ├─→ Rumors (in-camp chatter)
    └─→ Systemic Effects (resource availability, combat readiness)
```

---

### Data Model

#### ArmyLanceRoster (per army)

```csharp
class ArmyLanceRoster
{
    MobileParty Army;
    List<SimulatedLance> Lances;
    int DaysSinceFormation;
    ArmyContext Context; // War, Peace, Siege, March
    
    // Aggregate stats
    float AverageReadiness;
    int LancesOnAssignment;
    int LancesInCamp;
    int LancesUndermanned;
}
```

#### SimulatedLance (per NPC lance)

```csharp
class SimulatedLance
{
    string Name; // "The Bold Hawks", "Ironwood Guard"
    int Size; // 8-12 members
    int AverageTier; // 2-5
    Hero Leader; // Optional: generic hero or null
    
    // Current state
    LanceAssignment CurrentAssignment;
    int DaysOnAssignment;
    float Readiness; // 0-100 (combat effectiveness)
    LanceStatus Status; // Ready, Undermanned, Injured, Exhausted, OnAssignment
    
    // History (last 7 days)
    List<LanceEvent> RecentEvents;
    
    // Reputation (accumulated over time)
    int ReliabilityScore; // 0-100 (affects success chances)
    int MoraleScore; // 0-100 (current morale)
}
```

#### LanceAssignment

```csharp
enum LanceAssignmentType
{
    InCamp,          // Available in camp
    GuardDuty,       // Camp perimeter, lord protection
    Patrol,          // Area patrol (2-4 hours away)
    Foraging,        // Food/supply gathering (1-2 days)
    Requisitioning,  // Tax collection, resource procurement (1-3 days)
    Scouting,        // Intelligence gathering (2-5 days)
    ConvoyEscort,    // Protecting supply train (3-7 days)
    OutpostGarrison, // Detached to hold location (7-14 days)
    Recovery,        // Medical tent, rest (1-7 days)
    Training,        // Drilling, equipment practice
    Discipline,      // Confined, punishment duty
}

class LanceAssignment
{
    LanceAssignmentType Type;
    int DaysRemaining;
    Settlement TargetLocation; // If applicable
    string Description; // "Patrol near Marunath"
    float DangerLevel; // 0-100 (affects event chances)
}
```

#### LanceEvent

```csharp
enum LanceEventType
{
    // Success outcomes
    MissionSuccess,      // Routine success
    ExceptionalSuccess,  // Noteworthy achievement
    EnemyContact,        // Brief skirmish with hostiles (not a full battle)
    
    // Neutral outcomes
    MissionIncomplete,   // Partial success
    Delayed,             // Taking longer than expected
    EquipmentDamage,     // Minor wear and tear
    
    // Negative outcomes
    MinorInjuries,       // Some wounded from patrol incidents, lance still functional
    HeavyCasualties,     // Significant losses from ambush/skirmish, undermanned
    MissionFailure,      // Failed objective
    Ambushed,            // Small enemy attack during patrol, fight or retreat
    
    // Rare outcomes
    HeroicAction,        // Lance distinguished itself during routine duty
    LeaderKilled,        // Lance leader died (accident, ambush, disease)
    Desertion,           // Some members fled
    DiseaseOutbreak,     // Sickness spreading
    ValuableIntel,       // Discovered important information during scouting
}

// NOTE: These are ROUTINE ACTIVITY events, not simulated battles.
// "Enemy Contact" = brief skirmish with bandits/scouts during patrol
// "Ambushed" = small enemy group attacks, not a full battle
// Real battles use Bannerlord's normal battle system.

class LanceEvent
{
    int Day;
    LanceEventType Type;
    string Description;
    Settlement Location;
    int CasualtiesCount;
    bool GeneratedNewsItem; // Did we tell player about this?
}
```

---

## Lance Activity Types

### Wartime Activities

**High Frequency (assigned often during campaign):**

1. **Patrol** (2-6 hours, nearby area)
   - **Purpose:** Area denial, early warning, spot bandits/enemy scouts
   - **Danger:** Medium (30% chance of hostile contact per day)
   - **Outcomes:** 
     - Success: Area clear, no issues
     - Contact: Brief skirmish with bandits/enemy scouts (NOT a full battle), casualties possible
     - Ambush: Lance encounters small enemy group, fights or retreats, some injuries
   - **Note:** These are **small encounters** (5-15 enemies), not battles. Think: patrol runs into bandit gang.
   - **News Examples:**
     - *"2nd Lance reports area clear near Jaculan."*
     - *"The Bold Hawks engaged enemy scouts - 2 wounded, enemy driven off."* (skirmish, not battle)
     - *"3rd Lance ambushed on patrol, returning to camp for recovery."* (small ambush, not battle)

2. **Foraging** (1-2 days, medium range)
   - **Purpose:** Gather food, forage from countryside, requisition from villages
   - **Danger:** Low to Medium (20% chance of incident)
   - **Outcomes:**
     - Success: +50-150 food to army
     - Delay: Takes extra day, bad weather or empty villages
     - Conflict: Villagers resist, enemy forces encountered
   - **News Examples:**
     - *"Lance 'Ironwood Guard' returned with supplies from Ortongard."*
     - *"Foraging party delayed - sparse countryside."*
     - *"Supply team encountered Battanian raiders, 1 killed, 3 wounded."*

3. **Scouting** (2-5 days, long range)
   - **Purpose:** Intelligence on enemy positions, settlement status, terrain
   - **Danger:** High (40% chance of enemy contact)
   - **Outcomes:**
     - Success: Intel on enemy army location/strength
     - Spotted: Enemy aware, no intel gained
     - Captured: Lance members taken prisoner
   - **News Examples:**
     - *"Scout lance reports enemy army 3 days march east."*
     - *"Scouting mission compromised - enemy alerted to our presence."*
     - *"2 scouts captured during reconnaissance near Dunglanys."*

4. **Guard Duty** (24 hours, in camp/lord vicinity)
   - **Purpose:** Camp security, lord protection, prisoner watch
   - **Danger:** Very Low (5% chance of incident)
   - **Outcomes:**
     - Routine: Uneventful guard shift
     - Alert: Suspicious activity, false alarm or minor threat
     - Breach: Actual threat (assassin, infiltrator, prisoner escape attempt)
   - **News Examples:**
     - *"Night watch reports suspicious movement - false alarm."*
     - *"Guard lance apprehended deserter attempting to flee camp."*
     - *"Infiltrator stopped by sentries before reaching command tent."*

5. **Convoy Escort** (3-7 days, variable range)
   - **Purpose:** Protect supply wagons, reinforcement columns, prisoners
   - **Danger:** Medium-High (35% chance of ambush per day)
   - **Outcomes:**
     - Success: Convoy arrives intact
     - Harried: Multiple skirmishes, delays, minor losses
     - Ambushed: Major attack, convoy damaged or lost
   - **News Examples:**
     - *"Supply convoy arrived safely, escorted by 'Derthert's Fist'."*
     - *"Escort detail reports repeated attacks on supply route."*
     - *"Convoy ambushed - 4 wagons lost, 6 soldiers killed."*

---

### Peacetime Activities

**Higher Frequency (when not actively campaigning):**

1. **Tax Collection** (2-4 days, fief tour)
   - **Purpose:** Collect taxes from bound villages, maintain lord authority
   - **Danger:** Low-Medium (15% chance of resistance/banditry)
   - **Outcomes:**
     - Success: +500-2000 gold collected
     - Resistance: Villagers hostile, morale impact
     - Ambush: Bandits attack, possible casualties
   - **News Examples:**
     - *"Tax collection complete from Rhotae villages - 1,200 denars secured."*
     - *"Villagers at Varcheg refused levy - tense standoff resolved."*
     - *"Tax collectors ambushed by forest bandits - 2 killed, gold recovered."*

2. **Bandit Suppression** (1-3 days, patrol area)
   - **Purpose:** Clear bandit hideouts, secure roads, improve prosperity
   - **Danger:** Medium (25% chance of significant engagement)
   - **Outcomes:**
     - Success: Hideout cleared, roads safer
     - Escape: Bandits scatter, regroup elsewhere
     - Heavy Resistance: Lance takes casualties
   - **News Examples:**
     - *"'The Bold Hawks' cleared bandit camp near Revyl."*
     - *"Bandit suppression failed - hideout abandoned before arrival."*
     - *"Lance engaged 40+ bandits at forest camp - 5 wounded, mission success."*

3. **Village Protection** (3-7 days, stationed at village)
   - **Purpose:** Deter raids, train militia, maintain order
   - **Danger:** Low (10% chance of raid while present)
   - **Outcomes:**
     - Deterrent: No raids occur, villagers grateful
     - Raid Prevented: Enemy raiders arrive, lance defends
     - Overwhelmed: Lance outnumbered, village raided despite presence
   - **News Examples:**
     - *"Village guard detail at Husn Fulq completing peacefully."*
     - *"Lance defended Syronea from Sea Raider attack - village saved."*
     - *"Large enemy force raided Phycaon despite garrison - 3 soldiers lost."*

4. **Training Exercise** (1-2 days, in camp or nearby)
   - **Purpose:** Improve skills, build cohesion, maintain readiness
   - **Danger:** Very Low (2% chance of training accident)
   - **Outcomes:**
     - Success: Readiness +5-10
     - Accident: Training injury, readiness unchanged
     - Exceptional: Lance morale and readiness boost
   - **News Examples:**
     - *"2nd Lance conducting mounted drills near camp."*
     - *"Training accident: 1 soldier injured during melee practice."*
     - *"'Ironwood Guard' impressed officers during archery trials."*

5. **Recruitment Drive** (4-7 days, settlement tour)
   - **Purpose:** Attract volunteers, build local relations
   - **Danger:** Very Low (1% chance of incident)
   - **Outcomes:**
     - Success: +5-15 troops recruited
     - Poor Response: +1-3 recruits
     - Failure: No recruits, population unwilling
   - **News Examples:**
     - *"Recruitment party returned from Seonon with 12 volunteers."*
     - *"Recruitment effort yielded only 2 recruits - low enthusiasm."*
     - *"No recruits from Dunglanys - villagers suspicious of lord's intentions."*

---

### Universal Activities

**Can happen in any context:**

1. **Recovery** (1-7 days, medical tent/camp)
   - **Trigger:** Lance takes injuries or exhaustion
   - **Outcomes:** Gradual readiness restoration, possible complications
   - **News Examples:**
     - *"3rd Lance still recovering in medical tent - 4 wounded."*
     - *"'The Bold Hawks' returned to duty after 3 days recovery."*
     - *"Infection spread among wounded - 1 death, recovery extended."*

2. **Equipment Maintenance** (1 day, in camp)
   - **Trigger:** Scheduled maintenance or after heavy combat
   - **Outcomes:** Equipment readiness restored, possible supply costs
   - **News Examples:**
     - *"Armorer reports equipment maintenance for 2nd Lance complete."*
     - *"Lance delayed return to duty - extensive weapon repairs needed."*

3. **Disciplinary Confinement** (2-7 days, camp)
   - **Trigger:** Infractions, insubordination, fighting
   - **Outcomes:** Lance unavailable, morale impact
   - **News Examples:**
     - *"'Stormborn Riders' confined to camp - disciplinary action."*
     - *"Confined lance released, readiness restored."*

---

## State Simulation

### Lance Readiness System

**Readiness** represents combat effectiveness (0-100):
- **90-100:** Excellent condition (well-rested, equipped, high morale)
- **70-89:** Good condition (standard operational status)
- **50-69:** Degraded (tired, some injuries, worn equipment)
- **30-49:** Poor (undermanned, exhausted, low morale)
- **0-29:** Critical (barely functional, needs immediate recovery)

**Readiness Degradation:**
- **Combat:** -10 to -40 per battle (based on casualties)
- **Long Assignment:** -5 per day on patrol/foraging/escort
- **Failed Mission:** -10 to -20
- **Poor Conditions:** -5 per day (bad weather, low supplies, siege)

**Readiness Recovery:**
- **In Camp (resting):** +10 per day
- **In Camp (light duty):** +5 per day
- **Medical Tent:** +15 per day (for injured)
- **Good Conditions:** +2 bonus per day (peacetime, supplied, good morale)

---

### Lance Availability States

```csharp
enum LanceStatus
{
    Ready,              // Available for assignment
    OnAssignment,       // Currently deployed
    Undermanned,        // Lost >40% members, needs reinforcement
    Injured,            // In medical tent, recovering
    Exhausted,          // Needs rest before deployment
    Disciplined,        // Confined, unavailable
    Dissolved,          // Wiped out or disbanded
}
```

**State Transitions:**
- Lance on long patrol → returns **Exhausted** (needs 1-2 days rest)
- Lance takes casualties → becomes **Undermanned** (readiness capped at 50 until reinforced)
- Lance fails mission + low morale → **Disciplined** (punishment duty)
- Lance in medical tent → **Injured** (gradual recovery)
- Lance completes recovery → **Ready** (available for assignment)

---

### Event Probability System

Events are generated **probabilistically** based on:
1. **Assignment Danger Level** (set per activity type)
2. **Context Modifiers** (war vs peace, siege, enemy nearby)
3. **Lance Readiness** (lower readiness = higher failure/incident chance)
4. **Reliability Score** (veteran lances more likely to succeed)

**Formula:**
```
base_event_chance = DangerLevel * ContextModifier
adjusted_chance = base_event_chance * (150 - Readiness) / 100
success_modifier = (ReliabilityScore / 100)

roll = random(0-100)
if roll < adjusted_chance:
    generate_event(weighted by event type)
```

**Example:**
- Lance on **Patrol** (30% base danger) in **Wartime** (1.5x modifier) = 45% event chance
- Lance has **60 Readiness** (degraded): 45% × (150-60)/100 = 40.5% adjusted
- Lance has **70 Reliability** (decent): Success outcomes weighted +70%

**Event Type Weights (when event triggers):**
- Success outcomes: 50% base + ReliabilityScore/2
- Neutral outcomes: 30%
- Negative outcomes: 20% - ReliabilityScore/4

---

## Player Impact Scenarios

### Scenario 1: Cover Request Chain (With Real Casualties)

**Day 1 - Before Event:**
- **Lord's Army:** 547 total troops
- **3rd Lance** ("Ironwood Guard"): 10 troops (average Tier 3)
  - Party composition: 150 recruits (T1), 200 soldiers (T2), 150 veterans (T3), 47 elites (T4-5)

**Day 1 - Event Occurs:**
- **3rd Lance** sent on tax collection (danger level: 0.15, multiplier: 0.3)
- Encounters bandits, combat event fires
- Casualties calculated: 3 killed, 4 wounded (7 total from lance of 10)
- **Actual Party Impact:**
  - System selects 3 Tier 3 troops to kill: Party loses 3 veterans permanently
  - System selects 4 Tier 3 troops to wound: 4 veterans moved to wounded roster
  - **New Party Total:** 544 troops (140 ready, 4 wounded)
- News: *"Tax collectors ambushed by forest bandits - 3 killed, 4 wounded."*
- Lance status: **Undermanned (3 remaining)**, **Injured**

**Day 2:**
- Lance enters **Recovery** (medical tent, 7 days)
- Guard Duty schedule now short 1 lance
- **Cover Request Event** fires for player:
  - *"The camp is short-handed with 'Ironwood Guard' in recovery. Their guard shift is uncovered. Can your lance take extra duty?"*
  - **Accept:** Gain +2 Favor with lord, player lance gains +5 Fatigue, duty assignment changed
  - **Decline:** No penalty, another lance covers (if available)

**Day 3-8:**
- **Party strength:** Still 544 (wounded recovering)
- Wounded troops tracked: 4 Tier 3 veterans, recovery day 8
- News: *"'Ironwood Guard' recovering in medical tent - 4 wounded, expected return in 5 days."*

**Day 8:**
- **Wounded Recovery Completes:**
  - 4 wounded veterans return to active duty
  - **Party Total:** 544 troops (all active)
- Ironwood Guard recovers, readiness 55%, status: **Undermanned** (7/10 strength, lost 3 permanent)
- Assigned light duty (training, equipment maintenance)
- News: *"'Ironwood Guard' returned to light duty, still undermanned - awaiting reinforcements."*

**Day 15-20:**
- Lord recruits to replace losses (normal AI behavior)
- New recruits (Tier 1-2) join party over several days
- Eventually Ironwood Guard is back-filled to 10 troops
- News: *"'Ironwood Guard' brought to strength with fresh recruits."*

**Real Mechanical Impact:**
- Army was weaker by 7 troops for duration of recovery (days 1-8)
- Army permanently lost 3 veterans (replaced with recruits later)
- If battle occurred during days 1-8, army would have 544 troops instead of 547
- Wounded troops inaccessible until recovery (can't use in battle)
- Lord must actively recruit to replace permanent losses

---

### Scenario 2: Resource Strain

**Context:** Army on campaign, low on supplies

**Day 1:**
- Lord dispatches **3 lances** on foraging (33% of lances)
- News: *"Multiple foraging parties dispatched - 2nd, 4th, and 6th Lances sent to gather supplies."*

**Day 2:**
- **2nd Lance** returns with minimal supplies (poor countryside)
- **4th Lance** delayed (encountered enemy patrol, evading)
- **6th Lance** not yet returned
- Camp food drops to critical levels
- News: *"Foraging yields poor - supplies remain low. 4th Lance delayed by enemy activity."*
- **Camp activities** may be restricted (no feasting, training limited)

**Day 3:**
- **4th Lance** returns with moderate supplies
- **6th Lance** returns with good supplies + intel on enemy positions
- Food situation improves
- News: *"Supply situation easing as foraging teams return. 6th Lance reports enemy movements near Ortongard."*

**Impact on Player:**
- Felt resource scarcity in camp
- Saw multiple lances engaged in solving problem
- Received bonus intel from successful mission

---

### Scenario 3: Battle Availability

**Context:** Army engaging in major battle

**Pre-Battle:**
- **1st Lance:** Ready (in camp)
- **2nd Lance:** On Patrol (6 hours away, will miss battle)
- **3rd Lance:** Injured (medical tent, unavailable)
- **4th Lance:** Ready (in camp)
- **5th Lance:** Foraging (2 days away, will miss battle)
- **6th Lance:** Ready (in camp)

**Battle Starts:**
- Army has **reduced strength** (2 lances absent, 1 injured)
- **Harder battle** due to missing forces
- News (post-battle): *"Battle fought at reduced strength - 2nd Lance still on patrol, 5th Lance on foraging detail, 3rd Lance injured and unable to fight."*

**Post-Battle:**
- **2nd Lance** returns to find battle already over
- **5th Lance** still absent, misses action entirely
- **3rd Lance** remains in recovery

**Impact on Player:**
- Understands strategic timing matters
- Feels consequences of lance deployments
- Army isn't always at full strength when needed

---

### Scenario 4: Cascade Failure

**Context:** Difficult campaign, bad luck

**Day 1:**
- **2nd Lance** ambushed on patrol: 5 casualties, status **Undermanned**
- News: *"2nd Lance ambushed on patrol near Marunath - heavy casualties."*

**Day 2:**
- **2nd Lance** enters recovery
- **4th Lance** sent to fill patrol gap
- **Player's lance** asked to cover guard duty (Cover Request)

**Day 3:**
- **4th Lance** encounters enemy, takes 2 casualties, returns **Exhausted**
- News: *"4th Lance engaged enemy scouts - 2 wounded. Hostile activity increasing."*

**Day 4:**
- **Both 2nd and 4th** lances in recovery
- Lord forced to use **5th Lance** (low readiness, exhausted from previous assignment)
- **Player's lance** receives **second cover request** (more fatigue accumulation)
- News: *"Camp stretched thin - multiple lances recovering from engagements."*

**Day 5:**
- **5th Lance** fails patrol mission (too exhausted): Missed enemy approach
- Enemy army now closer than expected
- News: *"Patrol failed to intercept enemy scouts - hostile army approaching."*
- **Forced battle** with poor positioning

**Impact on Player:**
- Felt cascading pressure
- Multiple cover requests strain player lance
- Saw system failing under pressure
- Battle conditions worsened due to lance unavailability

---

## News & Bulletin Integration

### Camp Bulletin Display

The **Camp Bulletin** (via News/Dispatches System) is the primary way players learn about lance activities.

**Implementation:**
- Extend **Personal News Feed** (`enlisted_activities` menu) to include lance activity updates
- Create new **"Camp Bulletin" submenu** showing last 7 days of lance activities
- Display in **Army Orders** section of personal feed

**Example Personal News Feed:**

```
--- Army Orders ---
Multiple foraging parties dispatched - supplies needed.
'Ironwood Guard' recovering in medical tent - 4 wounded.
2nd Lance reports area clear near Jaculan.

[Camp Activities menu options...]
```

**Example Camp Bulletin Submenu (new screen):**

```
===== Camp Bulletin =====
[Day 23 of Campaign]

--- Today ---
• 2nd Lance completed patrol near Jaculan - no contact.
• Foraging party returned from Ortongard with supplies.
• 'The Bold Hawks' confined to camp - disciplinary action.

--- Yesterday ---
• Tax collection complete from Rhotae villages - 1,200 denars.
• 4th Lance engaged enemy scouts - 2 wounded, enemy driven off.
• Guard shift rotation: 6th Lance on duty tonight.

--- 2 Days Ago ---
• Scouting mission compromised - enemy alerted to presence.
• 'Ironwood Guard' ambushed during tax collection - 3 killed, 4 wounded.
• Supply convoy delayed - bad weather in mountain passes.

[Older entries...]

[Back to Camp Menu]
```

---

### News Types by Feed

**Personal News Feed (top 2-3 items, immediate relevance):**
- Lance requesting cover from player
- Lances returning from assignment
- Lance casualties affecting camp operations
- Resource strain due to lance activities

**Kingdom News Feed (context, less frequent):**
- Major lance successes (heroic actions, exceptional intel)
- Lance leader deaths
- Lance dissolutions (full wipes)

**Camp Bulletin (comprehensive log, 7-day history):**
- All lance departures and returns
- Mission outcomes
- Status changes
- Duty rotations

---

### News Generation Rules

**Priority Levels:**
- **High (2-day display):** Casualties, cover requests, mission failures, heroic actions
- **Medium (1-day display):** Mission successes, delays, equipment issues
- **Low (bulletin only):** Routine assignments, duty rotations, training

**Anti-Spam:**
- Combine similar events: "Multiple patrols dispatched" instead of 5 separate news items
- Only generate news for **notable** events (failures, big successes, player-impacting)
- Routine successes only appear in bulletin, not main feed

**Deduplication:**
- Use StoryKey: `lance_[name]_[activity]_[day]`
- Update existing items instead of creating duplicates
- Group related events (e.g., "Foraging efforts continue" updates single item)

---

## Cover Request System

### When Cover Requests Trigger

**Conditions:**
1. **Lance unavailable** (injured, exhausted, undermanned)
2. **Duty unfilled** (guard, patrol, critical assignment)
3. **No automatic fill** (other lances also committed or unavailable)
4. **Player lance available** (not on assignment, not exhausted)

**Frequency Cap:** Maximum 1 cover request per 2 days (prevents spam)

---

### Cover Request Event Structure

Uses existing **Inquiry Event** system (like Lance Life Simulation).

**Event Flow:**
```
[Inquiry Popup]
Title: "Cover Request from [Lance Name]"

Body:
"[Lance Leader Name] approaches you.

'Our lance is still recovering from [recent event]. 
We're scheduled for [duty type] but can't fill it. 
Can your boys cover for us? We'd owe you one.'

[Context: Current duty, expected duration, danger level]"

Options:
1. [Accept] "We'll cover your shift."
   → +2 Favor with lord
   → +1 Relationship with requesting lance leader (if tracked)
   → Player lance assigned to duty
   → Player lance gains +5-10 Fatigue
   → News: "[Player Lance] covering [Duty] for [Requesting Lance]."

2. [Negotiate] "We'll do it, but you owe us." (Charisma check)
   → Success: Same as Accept, +1 additional Favor
   → Failure: Same as Accept, no bonus
   
3. [Decline] "Sorry, we can't manage it."
   → No immediate penalty
   → If critical duty: Lord assigns player lance anyway (-1 Favor, +10 Fatigue)
   → If non-critical: Another lance takes it or duty unfilled
   → News: "[Duty] shift unfilled - camp security stretched."
```

**Context Variables:**
- Recent event: "the ambush last week", "heavy casualties in the last battle", "sickness in their ranks"
- Duty type: "guard duty tonight", "patrol tomorrow morning", "escort detail"
- Duration: "8 hours", "2 days", "overnight shift"
- Danger: "routine", "area has seen enemy activity", "could be dangerous"

---

### Favor & Reciprocity System

**Favor Tracking (per lance):**
- Player covers for lance: +1 Favor with that lance
- Player declines cover: -1 Favor (if critical, otherwise neutral)
- Lance covers for player: -1 Favor owed

**Reciprocity Events:**
- If player has +3 Favor with a lance, **that lance may offer to cover for player**
- Reverse cover request: Lance approaches player offering to take a duty
- Player can accept (use favor) or decline (keep favor)

**Benefits:**
- High favor with lances → More offers to help player
- Low favor → Player less likely to receive help
- Adds social dimension to army relationships

---

## Rumor & Intelligence System

### Camp Rumors (Social Chatter)

**Trigger:** Talk to NPCs in camp, visit mess tent, check camp activities

**Rumor Types:**

**Lance Performance:**
- *"I heard [Lance Name] ran into trouble on that patrol. Lost a few men."*
- *"They say [Lance Name] are the best - always bring back results."*
- *"[Lance Leader] is pushing his men too hard. They're exhausted."*

**Upcoming Assignments:**
- *"Word is [Lord] is planning a big foraging push next week."*
- *"Scuttlebutt says we're detaching a lance to garrison [Settlement]."*
- *"Guard duty rotation changing - hope we don't get night shift."*

**Enemy Activity:**
- *"Scouts report enemy forces 2 days east."*
- *"Bandits getting bolder near [Settlement] - patrols hitting resistance."*
- *"Heard the [Enemy Kingdom] is moving an army this way."*

**Internal Drama:**
- *"[Lance A] and [Lance B] got into a fight in the mess - tensions high."*
- *"[Lance Leader] got dressed down by the marshal for failing that mission."*
- *"Some of the boys are grumbling about pay - it's been 11 days since muster."*

**Implementation:**
- Rumors generated from recent lance events (last 3 days)
- Show 2-3 random rumors when player visits social areas
- Rumors provide **hints** about camp state without explicit mechanics exposure

---

### Intelligence Gathering

**Player Action:** "Check with the scouts" / "Talk to returning patrols"

**Information Available:**
- Which lances are on assignment
- Expected return dates
- Recent mission outcomes
- Enemy activity reports
- Resource status

**Use Case:**
- Player planning: "Should I accept this cover request, or is another lance returning soon?"
- Strategic awareness: "Multiple lances out on assignment - army weakened if battle happens now"
- Timing decisions: "Wait 2 days for foraging parties to return before we engage"

---

## Time-of-Day Scheduling

### Integration with AI Camp Schedule

**The AI Camp Schedule system** (from `ai-camp-schedule.md`) tracks 6 time periods per day:
- Dawn (5-8am)
- Morning (8-12pm)
- Afternoon (12-5pm)
- Evening (5-8pm)
- Dusk (8-10pm)
- Night (10-5am)

**Lance assignments align with this:**
- **Guard Duty:** Night/Dawn shifts (player sees who's on watch)
- **Patrol:** Morning/Afternoon departures (visible in camp activities)
- **Foraging/Long Missions:** Multi-day, tracked by return day
- **Training:** Morning/Afternoon (visible in camp)
- **Rest:** Night (lances in camp, available for emergencies)

**Visual Feedback:**
- Camp Activities screen shows "3rd Lance on patrol (returns this evening)"
- Guard Duty screen shows "Current watch: 2nd Lance (until dawn)"
- Bulletin notes departure times: "4th Lance departed at dawn for foraging mission"

---

## Affecting Real Party Composition

### Philosophy: Simulation with Mechanical Weight

**Design Decision:** Make simulated casualties **optionally affect** the lord's actual party troop counts.

**Why This Matters:**
- ✅ **Realism**: Lances "really" going on patrol should have real consequences
- ✅ **Strategic Depth**: Armies weaken over time through attrition, not just battles
- ✅ **Player Impact**: Player sees armies dynamically change strength
- ✅ **Mechanical Integration**: Simulated casualties carry into actual battles

**Configuration:**
```json
"party_integration": {
  "affect_actual_troops": true,
  "casualty_rate_multiplier": 0.5,  // Simulation casualties at 50% of battle rate
  "wounded_recovery_days": 7,
  "apply_to_player_army_only": false  // Also affect enemy armies
}
```

---

### Casualty Application System

When a lance event generates casualties, we apply them to the actual party:

**Step 1: Determine Casualty Count**
```csharp
private int CalculateCasualties(LanceEvent evt, SimulatedLance lance)
{
    // Base casualties from event type
    int baseCasualties = evt.Type switch
    {
        LanceEventType.MinorInjuries => MBRandom.RandomInt(1, 3),
        LanceEventType.HeavyCasualties => MBRandom.RandomInt(3, 6),
        LanceEventType.Ambushed => MBRandom.RandomInt(2, 5),
        LanceEventType.LeaderKilled => 1, // Just the leader
        _ => 0
    };
    
    // Apply multiplier from config
    float multiplier = ModConfig.Instance.CasualtyRateMultiplier;
    int adjustedCasualties = (int)(baseCasualties * multiplier);
    
    // Cap at lance size (can't lose more than you have)
    return Math.Min(adjustedCasualties, lance.Size);
}
```

**Step 2: Select Troops to Affect**
```csharp
private void ApplyCasualtiestoParty(MobileParty party, int totalCasualties, SimulatedLance lance)
{
    if (!ModConfig.Instance.AffectActualTroops)
        return;
        
    // Get troops matching lance tier (±1 tier)
    var eligibleTroops = party.MemberRoster.GetTroopRoster()
        .Where(t => !t.Character.IsHero) // Never kill heroes in simulation
        .Where(t => Math.Abs(t.Character.Tier - lance.AverageTier) <= 1)
        .ToList();
    
    if (!eligibleTroops.Any())
        return;
    
    // Distribute casualties
    int killed = (int)(totalCasualties * 0.4f); // 40% killed
    int wounded = totalCasualties - killed;      // 60% wounded
    
    // Apply killed
    for (int i = 0; i < killed && eligibleTroops.Any(); i++)
    {
        var victim = eligibleTroops.GetRandomElement();
        party.MemberRoster.AddToCounts(victim.Character, -1, false, 0, 0, true);
        
        // Remove from eligible list if depleted
        if (party.MemberRoster.GetTroopCount(victim.Character) == 0)
            eligibleTroops.Remove(victim);
    }
    
    // Apply wounded
    for (int i = 0; i < wounded && eligibleTroops.Any(); i++)
    {
        var victim = eligibleTroops.GetRandomElement();
        
        // Move 1 troop from regular to wounded
        if (party.MemberRoster.GetTroopCount(victim.Character) > 0)
        {
            party.MemberRoster.AddToCounts(victim.Character, -1, false, 0, 0, true);
            party.MemberRoster.AddToCounts(victim.Character, 1, false, 1, 0, true); // Add as wounded
        }
    }
}
```

**Step 3: Track Wounded Recovery**
```csharp
private Dictionary<MobileParty, List<WoundedTroopRecord>> _woundedTroops = new();

private class WoundedTroopRecord
{
    public CharacterObject Character;
    public int Count;
    public int DayWounded;
    public int RecoveryDays;
}

private void ProcessWoundedRecovery()
{
    int currentDay = CampaignTime.Now.GetDayOfYear;
    
    foreach (var (party, woundedList) in _woundedTroops)
    {
        var recovered = woundedList.Where(w => currentDay - w.DayWounded >= w.RecoveryDays).ToList();
        
        foreach (var record in recovered)
        {
            // Move wounded back to active roster
            int woundedCount = party.MemberRoster.GetTroopCount(record.Character);
            int woundedToRecover = Math.Min(record.Count, woundedCount);
            
            if (woundedToRecover > 0)
            {
                // Remove from wounded
                party.MemberRoster.AddToCounts(record.Character, -woundedToRecover, false, 0, 0, true);
                // Add back as healthy
                party.MemberRoster.AddToCounts(record.Character, woundedToRecover, false, 0, 0, true);
                
                woundedList.Remove(record);
            }
        }
    }
}
```

---

### Practical Example: How Routine Operations Affect Real Battles

**Important:** We're NOT simulating battles. We're simulating **everyday military activities** (patrols, foraging, guard duty). When a **real battle** happens (Bannerlord's normal battle), the army is weaker because of casualties from those routine activities.

**Scenario:** Army doing routine operations, then a real battle occurs

**7 Days Before Real Battle:**
- Army: 547 troops ready
- Routine operations: Patrols, guard duty, foraging

**5 Days Before Real Battle:**
- 3rd Lance on routine patrol (not a battle, just patrolling the area)
- Encounters bandits during patrol: Small skirmish, 2 killed, 3 wounded
- Army: 545 troops (542 ready, 3 wounded recovering)
- **Note:** This was a patrol encounter, not a simulated battle

**3 Days Before Real Battle:**
- 5th Lance sent on scouting mission (routine intelligence gathering)
- 6th Lance on foraging duty (collecting supplies)
- Both are just doing their jobs, not fighting battles

**2 Days Before Real Battle:**
- 5th Lance doing routine scouting, spots enemy patrol
- Brief contact: 1 killed, 5 wounded (small skirmish, not a full battle)
- 6th Lance returns successfully with supplies (no incidents)
- Army: 544 troops (536 ready, 8 wounded recovering)
- **Note:** These are patrol/scouting incidents, not battles

**Real Battle Day (Bannerlord's Normal Battle System):**
- **Available Forces:** 536 troops (8 wounded unavailable)
- **Unavailable Lance:** 5th Lance still on scouting mission (10 troops, 2 days away)
- **Effective Battle Strength:** 526 troops (536 minus 10 on assignment)
- **Compare to:** 547 troops if no routine casualties had occurred

**Impact:**
- Army enters **real battle** 21 troops weaker (4% reduction) due to routine operations
- This is realistic: Armies lose men to patrols, accidents, small skirmishes before big battles
- Player sees: "We've been taking losses before the big battle even starts"
- News: *"Battle joined at reduced strength - 3rd Lance recovering, 5th Lance still on scouting duty."*

**Key Point:** We simulate the **small stuff** (patrols losing men to bandits, scouting parties taking casualties, guard duty accidents). When Bannerlord triggers a **real battle**, those losses matter because the army is weaker.

---

### Pros & Cons Analysis

#### Pros ✅

**1. Realism & Immersion**
- Armies feel like living organisms that weaken through routine operations
- Historical realism: armies lost men to disease, accidents, skirmishes more than battles
- Player sees consequences of attrition warfare

**2. Strategic Depth**
- Timing matters: Don't send lances on dangerous missions right before battle
- Risk management: High-reward scouting mission vs keeping lances safe
- Resource management: Weakened armies may need to delay battles to recruit

**3. Mechanical Consistency**
- Simulation events have real weight, not just narrative flavor
- Integrates with Bannerlord's existing casualty system
- Wounded recovery mirrors post-battle recovery mechanics

**4. Dynamic Campaigns**
- Armies get weaker over long campaigns (realistic)
- Lords must actively recruit to maintain strength
- Player sees world consequences beyond direct control

**5. Player Agency Enhanced**
- Cover requests more meaningful (prevent casualties by taking duty yourself)
- Strategic choices: "Should I push for battle now, or wait for lances to recover?"
- Intelligence gathering: Checking bulletin to see army strength before committing

#### Cons ❌

**1. Balance Challenges**
- Could make campaigns harder (player army weakening constantly)
- AI lords may not recruit fast enough to compensate
- Long campaigns could spiral into weakness

**2. Player Frustration Risk**
- Losing troops to "background" events might feel unfair
- "I didn't even fight a battle and I lost 15 troops?"
- Casual players may not enjoy attrition mechanics

**3. Complexity**
- Adds another system that affects party composition
- Harder to track why army strength changed
- Debugging: "Did I lose troops in battle or simulation?"

**4. AI Behavior Uncertainty**
- Bannerlord AI not designed for attrition mechanics
- Lords might make poor decisions with weakened armies
- Enemy armies might become too weak (if enabled for them)

**5. Performance Considerations**
- Daily processing of casualties for all armies
- Tracking wounded recovery across many parties
- Save file size increase (wounded troop records)

#### Mitigations 🛠️

**For Balance:**
- Keep casualty multipliers low (0.3-0.6 range)
- Make it configurable (can disable if too punishing)
- Start with player army only, make enemy application optional
- Wounded recover (most casualties temporary, not permanent)

**For Frustration:**
- Clear news reporting: Player always knows what happened
- Gradual casualties: Not sudden massive losses
- Player agency: Cover requests let player prevent some casualties
- Tutorial/documentation: Explain attrition is part of campaign life

**For Complexity:**
- Configuration presets: "Casual" (low casualties), "Realistic" (higher)
- Debug UI: Show recent simulation casualties
- Clear attribution: News says "lost in patrol" vs "lost in battle"

**For AI:**
- Monitor AI recruiting behavior in playtesting
- Add recruitment boost AI if armies get too weak
- Consider different casualty rates for player vs AI

#### Recommendation 🎯

**Implementation Approach:**

**Phase 1 (MVP):**
- Enable casualty application for **player's army only**
- Use **conservative multipliers** (0.3-0.5 base)
- Wounded recovery system (7 days default)
- Clear news reporting
- **Configurable off** for players who don't want it

**Phase 2 (Post-Testing):**
- Gather player feedback on balance
- Adjust multipliers based on playtest data
- Consider enabling for enemy armies (optional difficulty setting)
- Add intelligence reports: "Enemy army weakened by attrition"

**Phase 3 (Polish):**
- AI recruiting behavior monitoring
- Advanced config options (per-activity multipliers)
- Integration with medical tent capacity limits
- Attrition statistics tracking (for player review)

**Recommended Default:**
```json
"affect_actual_troops": true,  // Enable mechanical weight
"base_casualty_multiplier": 0.4,  // Conservative casualties
"apply_to_enemy_armies": false,  // Player only for balance
"wounded_recovery_days": 7  // Week recovery time
```

This provides **meaningful attrition** without being **punishing**, and players who dislike it can disable it entirely.

---

### Visual Example: Weekly Attrition

**Campaign Week Overview - Lord's Army**

```
Day 1 - Army Forms
├─ Total: 547 troops
├─ 8 lances (10-12 each)
└─ All ready for duty

Day 2 - Routine Operations
├─ 2nd Lance: Patrol (area clear, no casualties)
├─ 4th Lance: Foraging (success, +80 food, no casualties)
├─ 6th Lance: Guard duty (routine, no casualties)
└─ Army: 547 troops (no change)

Day 3 - Minor Incident
├─ 3rd Lance: Tax collection → Bandit ambush
│   ├─ Event: 2 killed, 3 wounded
│   └─ Party: -2 Tier 3 troops (permanent)
│       └─ +3 Tier 3 wounded (recover day 10)
├─ News: "Tax collectors ambushed - 2 killed, 3 wounded"
└─ Army: 545 troops (542 active, 3 wounded)

Day 4 - Increased Activity
├─ 5th Lance: Scouting mission (danger: high)
├─ 7th Lance: Convoy escort
└─ Army: 545 troops (still recovering from day 3)

Day 5 - Second Incident
├─ 5th Lance: Enemy contact during scouting
│   ├─ Event: 1 killed, 4 wounded
│   └─ Party: -1 Tier 4 troop (permanent)
│       └─ +4 Tier 4 wounded (recover day 12)
├─ Cover Request: "5th Lance needs recovery, can you cover patrol?"
├─ News: "Scout lance engaged enemy - 1 killed, 4 wounded"
└─ Army: 544 troops (537 active, 7 wounded)

Day 6 - Resource Strain
├─ Multiple foraging parties dispatched (supplies low)
├─ 3rd Lance still recovering (3 wounded)
├─ 5th Lance enters recovery (4 wounded, 1 killed)
└─ Army: 544 troops (537 active, 7 wounded)

Day 7 - Week Summary
├─ Total Casualties: 3 killed, 7 wounded
├─ Party Strength: 544 / 547 original (0.5% loss)
├─ Wounded Recovery: 7 troops returning over next week
├─ Lord Recruiting: +10 new recruits (Tier 1-2)
│   └─ Replacing veteran losses with fresh troops
└─ Net Army: 554 troops (550 active, 7 wounded)

Day 14 - Two Week Mark
├─ All wounded recovered (day 10, day 12 passed)
├─ Recruits integrated
├─ New casualties from ongoing operations: ~2-5 more
└─ Army: 549-552 troops (cyclical attrition + recruitment)
```

**Analysis:**
- **Weekly attrition:** 0.5-1% of army strength
- **Sustainable:** Lords recruit faster than attrition
- **Impact visible:** Player sees army composition change
- **Strategic:** Timing battles around lance availability matters
- **Recoverable:** Most casualties temporary (wounded recover)

---

### Balance Considerations

**Casualty Rate Tuning:**

The simulation should generate **fewer casualties** than actual battles:
- **Battle casualties:** 10-40% of engaged troops
- **Simulation casualties:** 2-8% of engaged lance (via multiplier)

**Reasoning:**
- Lances are doing routine work (patrols, foraging), not full battles
- Most missions succeed without incident
- High-danger missions (scouting enemy territory) have higher rates
- Keeps armies from degrading too quickly

**Recommended Multipliers by Activity:**
```json
"casualty_multipliers": {
  "guard_duty": 0.1,        // Very rare casualties
  "training": 0.05,          // Training accidents rare
  "patrol": 0.3,             // Some risk
  "foraging": 0.2,           // Low-medium risk
  "scouting": 0.6,           // High risk, deep in enemy territory
  "convoy_escort": 0.5,      // Medium-high risk, ambush likely
  "bandit_suppression": 0.4, // Medium risk
  "tax_collection": 0.3      // Some resistance possible
}
```

---

### Enemy Army Application

**Question:** Should we apply simulation to **enemy armies** too?

**Yes, if:**
- Player wants enemy armies to also weaken through attrition
- Realism > gameplay convenience
- Player is playing a long strategic campaign

**Implementation:**
```csharp
private void ProcessDailyLanceActivities(ArmyLanceRoster roster)
{
    // ... existing code ...
    
    // Only process if enabled for this army type
    bool shouldProcess = roster.Army.LeaderParty.IsPlayerParty ||
                        ModConfig.Instance.ApplyToEnemyArmies;
    
    if (!shouldProcess)
        return;
        
    // Continue with event generation and casualty application
}
```

**Pros:**
- Enemy armies weaken over time (realistic)
- Player benefits from enemy attrition
- Consistent simulation (all armies treated equally)

**Cons:**
- May make campaigns easier (enemy weakens before battles)
- AI lords may not recruit fast enough to compensate
- Could cause balance issues

**Recommendation:** Make it **configurable**, default to **player army only** for MVP, enable for enemies as optional difficulty setting.

---

### Integration with Existing Systems

**With Lance Life Simulation:**
- Player's lance uses **detailed injury system** (Lance Life Simulation)
- NPC lances use **aggregate casualty application** (this system)
- Both feed to same medical tent capacity tracking

**With AI Camp Schedule:**
- Casualties affect lance availability
- Schedule system knows which lances are undermanned
- Triggers cover requests when too many lances unavailable

**With Battle System:**
- Simulated casualties are already applied when battle starts
- Army enters battle with reduced strength
- Post-battle casualties are **additional** to simulation casualties

---

## Technical Implementation

### Core Behavior

```csharp
public class ArmyLanceSimulationBehavior : CampaignBehaviorBase
{
    private static ArmyLanceSimulationBehavior? _instance;
    public static ArmyLanceSimulationBehavior? Instance => _instance;
    
    private Dictionary<MobileParty, ArmyLanceRoster> _armyRosters;
    private List<PendingCoverRequest> _pendingRequests;
    private Dictionary<MobileParty, List<WoundedTroopRecord>> _woundedTroops;
    
    public override void RegisterEvents()
    {
        CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        CampaignEvents.ArmyCreated.AddNonSerializedListener(this, OnArmyCreated);
        CampaignEvents.ArmyDispersed.AddNonSerializedListener(this, OnArmyDispersed);
        CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnBattleEnded);
    }
    
    private void OnDailyTick()
    {
        // Process wounded recovery first
        ProcessWoundedRecovery();
        
        // Then process lance activities
        foreach (var roster in _armyRosters.Values)
        {
            ProcessDailyLanceActivities(roster);
        }
    }
    
    private void ProcessDailyLanceActivities(ArmyLanceRoster roster)
    {
        // 1. Advance assignments (decrease days remaining)
        AdvanceAssignments(roster);
        
        // 2. Generate events for lances on assignment
        GenerateLanceEvents(roster);
        
        // 3. Apply casualties to actual party (NEW)
        ApplySimulatedCasualties(roster);
        
        // 4. Update lance states (readiness, availability)
        UpdateLanceStates(roster);
        
        // 5. Check for cover requests
        CheckForCoverRequests(roster);
        
        // 6. Generate news items
        GenerateNewsItems(roster);
        
        // 7. Assign new duties (if lances available)
        AssignNewDuties(roster);
    }
}
```

---

### Lance Event Generation

```csharp
private void GenerateLanceEvents(ArmyLanceRoster roster)
{
    foreach (var lance in roster.Lances)
    {
        if (lance.Status != LanceStatus.OnAssignment)
            continue;
            
        // Calculate event probability
        float dangerLevel = lance.CurrentAssignment.DangerLevel;
        float contextMod = GetContextModifier(roster.Context);
        float baseChance = dangerLevel * contextMod;
        float adjustedChance = baseChance * (150 - lance.Readiness) / 100f;
        
        // Roll for event
        if (MBRandom.RandomFloat < adjustedChance)
        {
            GenerateEvent(lance, roster);
        }
    }
}

private void GenerateEvent(SimulatedLance lance, ArmyLanceRoster roster)
{
    // Weighted roll for event type
    float successWeight = 50f + (lance.ReliabilityScore / 2f);
    float neutralWeight = 30f;
    float negativeWeight = 20f - (lance.ReliabilityScore / 4f);
    
    LanceEventType eventType = WeightedRandom(successWeight, neutralWeight, negativeWeight);
    
    // Create event
    var lanceEvent = new LanceEvent
    {
        Day = CampaignTime.Now.GetDayOfYear,
        Type = eventType,
        Description = GenerateEventDescription(lance, eventType),
        Location = lance.CurrentAssignment.TargetLocation,
        GeneratedNewsItem = false
    };
    
    // Apply consequences
    ApplyEventConsequences(lance, lanceEvent);
    
    // Add to history
    lance.RecentEvents.Add(lanceEvent);
    
    // Trim old events (keep last 7 days)
    TrimOldEvents(lance);
}
```

---

### Cover Request Generation

```csharp
private void CheckForCoverRequests(ArmyLanceRoster roster)
{
    // Don't spam - max 1 request per 2 days
    if (roster.DaysSinceLastCoverRequest < 2)
        return;
    
    // Find lances that need coverage
    var needsCover = roster.Lances
        .Where(l => l.Status == LanceStatus.Injured || 
                    l.Status == LanceStatus.Undermanned ||
                    l.Status == LanceStatus.Exhausted)
        .Where(l => l.CurrentAssignment.Type != LanceAssignmentType.InCamp)
        .ToList();
    
    if (needsCover.Any())
    {
        var requestingLance = needsCover.GetRandomElement();
        
        // Check if player can cover
        var playerLance = GetPlayerLance();
        if (playerLance != null && CanPlayerCover(playerLance))
        {
            CreateCoverRequestEvent(requestingLance, roster);
            roster.DaysSinceLastCoverRequest = 0;
        }
    }
}

private void CreateCoverRequestEvent(SimulatedLance requestingLance, ArmyLanceRoster roster)
{
    string title = $"Cover Request from {requestingLance.Name}";
    string body = GenerateCoverRequestText(requestingLance);
    
    InformationManager.ShowInquiry(
        new InquiryData(
            title,
            body,
            isAffirmingOptionPositive: true,
            isNegativeOptionPositive: false,
            affirmativeText: "We'll cover your shift",
            negativeText: "Sorry, we can't",
            onAffirmative: () => AcceptCoverRequest(requestingLance, roster),
            onNegative: () => DeclineCoverRequest(requestingLance, roster)
        )
    );
}
```

---

### News Integration

```csharp
private void GenerateNewsItems(ArmyLanceRoster roster)
{
    foreach (var lance in roster.Lances)
    {
        foreach (var evt in lance.RecentEvents.Where(e => !e.GeneratedNewsItem))
        {
            if (ShouldGenerateNews(evt))
            {
                string headline = GenerateHeadline(lance, evt);
                
                // Feed to News System
                EnlistedNewsBehavior.Instance?.AddPersonalNews(
                    headline: headline,
                    day: evt.Day,
                    priority: GetNewsPriority(evt),
                    storyKey: $"lance_{lance.Name}_{evt.Type}_{evt.Day}"
                );
                
                evt.GeneratedNewsItem = true;
            }
        }
    }
}

private bool ShouldGenerateNews(LanceEvent evt)
{
    // Only generate news for notable events
    switch (evt.Type)
    {
        case LanceEventType.MissionSuccess:
            return false; // Routine, bulletin only
            
        case LanceEventType.ExceptionalSuccess:
        case LanceEventType.MinorInjuries:
        case LanceEventType.HeavyCasualties:
        case LanceEventType.MissionFailure:
        case LanceEventType.HeroicAction:
        case LanceEventType.LeaderKilled:
            return true; // Always newsworthy
            
        case LanceEventType.EnemyContact:
        case LanceEventType.Delayed:
            return MBRandom.RandomFloat < 0.5f; // Sometimes newsworthy
            
        default:
            return false;
    }
}
```

---

### Save/Load Support

```csharp
public override void SyncData(IDataStore dataStore)
{
    dataStore.SyncData("_armyLanceRosters", ref _armyRosters);
    dataStore.SyncData("_pendingCoverRequests", ref _pendingRequests);
}
```

**Data Classes** must be:
- Decorated with `[SaveableClass]`
- Use `[SaveableField]` for persisted properties
- Use primitives or Bannerlord-supported types (Hero, Settlement, MobileParty references OK)

---

## Configuration

### Config File

`ModuleData/Enlisted/lance_simulation_config.json`

```json
{
  "army_lance_simulation": {
    "enabled": true,
    "lances_per_army": {
      "min": 8,
      "max": 15,
      "troops_per_lance": 10
    },
    "event_generation": {
      "base_event_chance": 0.3,
      "war_modifier": 1.5,
      "peace_modifier": 0.7,
      "siege_modifier": 2.0
    },
    "party_integration": {
      "affect_actual_troops": true,
      "base_casualty_multiplier": 0.5,
      "wounded_recovery_days": 7,
      "apply_to_enemy_armies": false,
      "never_kill_heroes": true,
      "casualty_multipliers_by_activity": {
        "guard_duty": 0.1,
        "training": 0.05,
        "patrol": 0.3,
        "foraging": 0.2,
        "scouting": 0.6,
        "convoy_escort": 0.5,
        "bandit_suppression": 0.4,
        "tax_collection": 0.3,
        "village_protection": 0.3,
        "outpost_garrison": 0.2
      },
      "wounded_vs_killed_ratio": 0.6
    },
    "cover_requests": {
      "enabled": true,
      "min_days_between": 2,
      "favor_reward": 2,
      "fatigue_cost": 5
    },
    "readiness": {
      "combat_degradation": 20,
      "assignment_degradation_per_day": 5,
      "rest_recovery_per_day": 10,
      "medical_recovery_per_day": 15
    },
    "news_integration": {
      "generate_personal_news": true,
      "generate_kingdom_news": true,
      "bulletin_enabled": true,
      "history_days": 7
    },
    "assignment_durations": {
      "patrol_hours": 6,
      "foraging_days": 2,
      "scouting_days": 4,
      "convoy_escort_days": 5,
      "village_protection_days": 5,
      "recovery_days": 3
    },
    "danger_levels": {
      "guard_duty": 0.05,
      "training": 0.02,
      "patrol": 0.30,
      "foraging": 0.20,
      "scouting": 0.40,
      "convoy_escort": 0.35,
      "bandit_suppression": 0.25,
      "tax_collection": 0.15
    }
  }
}
```

---

### Configuration Options Explained

**party_integration.affect_actual_troops**
- **true**: Simulated casualties affect real party troop counts
- **false**: Simulation is cosmetic only (just news/readiness tracking)
- **Recommendation:** Start with `true` for full immersion

**party_integration.base_casualty_multiplier**
- Controls overall casualty rate (0.5 = 50% of what battle would cause)
- **Range:** 0.1 to 1.0
- **Balance:** Lower = armies stay stronger longer, Higher = more attrition

**party_integration.wounded_recovery_days**
- Days until wounded troops recover and return to active roster
- **Range:** 3-14 days
- **Balance:** Shorter = armies recover faster, Longer = more sustained impact

**party_integration.apply_to_enemy_armies**
- **true**: Enemy armies also lose troops through simulation
- **false**: Only player's army affected (easier campaign)
- **Recommendation:** Start with `false` for balance, enable for hardcore mode

**party_integration.casualty_multipliers_by_activity**
- Per-activity multiplier on top of base multiplier
- High-danger activities (scouting) have higher casualties
- Low-danger activities (guard duty) have minimal casualties

**party_integration.wounded_vs_killed_ratio**
- 0.6 = 60% wounded, 40% killed
- **Range:** 0.5 to 0.8
- **Balance:** Higher ratio = more recoverable losses (less punishing)

---

## Content Creation

### Lance Names Pool

**Cultural Variants** (use Bannerlord name generators):

**Vlandian:**
- "The Iron Lions"
- "Derthert's Fist"
- "The Silver Shields"
- "Rhotae's Riders"

**Battanian:**
- "The Greenwood Warband"
- "Caladog's Rangers"
- "The Oakwood Guard"
- "The Forest Wolves"

**Aserai:**
- "The Desert Swords"
- "Sand Riders"
- "Unqid's Ghazis"
- "The Golden Scorpions"

**Empire:**
- "The Legatus' Cohort"
- "The Scholarii"
- "The Palatine Guard"
- "The Cataphractoi"

**Sturgia:**
- "The Ironborn"
- "Raganvad's Axemen"
- "The Winter Guard"
- "The Frozen Spears"

**Khuzait:**
- "The Windborne"
- "Khan's Riders"
- "The Arrow Storm"
- "The Steppe Hawks"

---

### Event Description Templates

**Success Outcomes:**

```json
{
  "event_type": "MissionSuccess",
  "templates": [
    "{LANCE_NAME} completed {DUTY_TYPE} near {LOCATION} - no incidents.",
    "{DUTY_TYPE} successful - {LANCE_NAME} reports all clear.",
    "{LANCE_NAME} returned from {DUTY_TYPE} - mission complete."
  ]
}
```

**Enemy Contact:**

```json
{
  "event_type": "EnemyContact",
  "templates": [
    "{LANCE_NAME} engaged {ENEMY_TYPE} near {LOCATION} - {CASUALTIES} casualties, enemy driven off.",
    "Skirmish near {LOCATION}: {LANCE_NAME} defeated {ENEMY_COUNT} {ENEMY_TYPE}.",
    "{LANCE_NAME} reports hostile contact during {DUTY_TYPE} - {OUTCOME}."
  ]
}
```

**Failures:**

```json
{
  "event_type": "MissionFailure",
  "templates": [
    "{LANCE_NAME} failed to complete {DUTY_TYPE} - {REASON}.",
    "{DUTY_TYPE} mission unsuccessful - {LANCE_NAME} forced to retreat.",
    "{LANCE_NAME} unable to complete objective at {LOCATION} - {REASON}."
  ]
}
```

**Cover Request Bodies:**

```json
{
  "cover_request_templates": [
    "{LEADER_NAME} approaches you.\n\n'Our lance is still recovering from {RECENT_EVENT}. We're scheduled for {DUTY_TYPE} but can't fill it. Can your boys cover for us? We'd owe you one.'\n\nDuty: {DUTY_TYPE}\nDuration: {DURATION}\nDanger: {DANGER_LEVEL}",
    
    "A messenger from {LANCE_NAME} finds you.\n\n'{LANCE_NAME} is undermanned after {RECENT_EVENT}. Command wants them on {DUTY_TYPE}, but they're not fit. Can you take it? We'll remember the favor.'\n\nDuty: {DUTY_TYPE}\nDuration: {DURATION}",
    
    "{LEADER_NAME} looks exhausted.\n\n'We've been running ragged since {RECENT_EVENT}. Got assigned {DUTY_TYPE} but the men are dead on their feet. Any chance you can cover us? Just this once.'\n\nDuty: {DUTY_TYPE}\nDuration: {DURATION}\nDanger: {DANGER_LEVEL}"
  ]
}
```

---

## Implementation Phases

### Phase 0: Foundation (Week 1)

**Goals:**
- Data structures in place
- Save/load working
- Lance roster generation on army creation

**Tasks:**
1. Create `ArmyLanceSimulationBehavior`
2. Define data classes (`ArmyLanceRoster`, `SimulatedLance`, `LanceAssignment`, `LanceEvent`)
3. Implement `SyncData()` for save/load
4. Generate lance names using Bannerlord name generator
5. Initialize rosters when armies form
6. Clean up rosters when armies disperse

**Testing:**
- Save/load preserves lance rosters
- Lance names are culturally appropriate
- Rosters scale with army size

**Duration:** 3-5 days

---

### Phase 1: Assignment & State Tracking (Week 2)

**Goals:**
- Lances get assigned to duties
- Assignments advance over time
- Lance states update (readiness, availability)

**Tasks:**
1. Implement `AssignNewDuties()` logic
2. Implement `AdvanceAssignments()` (daily tick processing)
3. Implement readiness degradation/recovery
4. Implement lance status transitions
5. Add debug commands to inspect lance states

**Testing:**
- Lances get assigned duties appropriate to context (war/peace)
- Assignments complete and lances return to camp
- Readiness degrades on assignment, recovers in camp
- Lance statuses transition correctly

**Duration:** 4-6 days

---

### Phase 2: Event Generation (Week 3)

**Goals:**
- Events generate based on assignment type and context
- Event outcomes affect lance state
- Event history tracked
- **[NEW]** Casualties applied to actual party rosters

**Tasks:**
1. Implement `GenerateLanceEvents()` with probability system
2. Create event type weights (success/neutral/negative)
3. Implement `ApplyEventConsequences()` (readiness changes, casualties, status changes)
4. **[NEW]** Implement `CalculateCasualties()` (event type → casualty count)
5. **[NEW]** Implement `ApplyCasualiesToParty()` (select troops, apply kills/wounds)
6. **[NEW]** Implement wounded tracking system (`WoundedTroopRecord`)
7. **[NEW]** Implement `ProcessWoundedRecovery()` (daily healing)
8. Create event description templates (20-30 variants)
9. Add event history tracking (last 7 days)
10. **[NEW]** Add configuration flags for casualty system

**Testing:**
- Events fire at expected rates
- Dangerous assignments generate more negative events
- Low readiness lances have worse outcomes
- Event consequences visible in lance state
- **[NEW]** Casualties correctly applied to party rosters
- **[NEW]** Wounded troops recover after configured days
- **[NEW]** Heroes never killed in simulation
- **[NEW]** Config flag `affect_actual_troops` respected

**Duration:** 7-9 days (increased due to casualty system)

---

### Phase 3: News Integration (Week 4)

**Goals:**
- Lance events appear in Personal News feed
- Camp Bulletin shows comprehensive history
- News items properly prioritized

**Tasks:**
1. Implement `GenerateNewsItems()` feeding to `EnlistedNewsBehavior`
2. Create headline templates for each event type
3. Implement news priority system (notable vs routine)
4. Add news deduplication (StoryKey)
5. Create "Camp Bulletin" submenu (7-day history display)

**Testing:**
- News appears in Personal feed
- Only notable events generate news (no spam)
- Bulletin shows comprehensive history
- News items update correctly

**Duration:** 4-6 days

---

### Phase 4: Cover Requests (Week 5)

**Goals:**
- Cover requests fire when lances unavailable
- Player can accept/decline
- Consequences apply correctly

**Tasks:**
1. Implement `CheckForCoverRequests()` logic
2. Create cover request inquiry events
3. Implement accept/decline handlers
4. Add favor tracking system
5. Create 10-15 cover request body templates
6. Implement fatigue costs for player lance

**Testing:**
- Cover requests fire when appropriate
- Requests don't spam (2-day cooldown)
- Accept: Player lance assigned, fatigue applied, favor gained
- Decline: Consequences apply based on criticality
- Favor tracked across multiple requests

**Duration:** 5-7 days

---

### Phase 5: Battle Integration (Week 6)

**Goals:**
- Lance availability affects army strength in battles
- Absent lances noted in post-battle news
- Battle casualties distributed to lances

**Tasks:**
1. Hook `OnBattleEnded` to distribute casualties to lances
2. Calculate army strength based on available lances
3. Generate post-battle news noting absent lances
4. Implement lance casualty distribution (weighted by tier)
5. Update lance states post-battle (readiness, undermanned status)

**Testing:**
- Battles with lances absent show reduced strength
- Post-battle news mentions absent lances
- Casualties distributed realistically
- Lances enter recovery after heavy casualties

**Duration:** 4-6 days

---

### Phase 6: Polish & Balance (Week 7)

**Goals:**
- Event probabilities balanced
- **[NEW]** Casualty rates feel realistic (not too harsh, not trivial)
- News feels natural (not spammy)
- Cover requests feel meaningful (not annoying)
- Player impact clear and significant

**Tasks:**
1. Balance event probabilities (playtest 30-day campaign)
2. **[NEW]** Tune casualty multipliers (test 0.3, 0.5, 0.7 base rates)
3. **[NEW]** Balance wounded vs killed ratio (test 50/50, 60/40, 70/30)
4. **[NEW]** Adjust wounded recovery time (test 5 days, 7 days, 10 days)
5. Adjust readiness degradation/recovery rates
6. Tune cover request frequency
7. Add rumor system (camp chatter)
8. **[NEW]** Test enemy army attrition (optional feature)
9. Create configuration file with all tunable values
10. Write content creation guide
11. **[NEW]** Document recommended config presets (Casual/Normal/Realistic)

**Testing:**
- 30+ day playthrough feels immersive
- **[NEW]** Army loses 5-15% strength over 30 days (attrition feels real but sustainable)
- **[NEW]** Lords recruit enough to offset losses (armies don't spiral to zero)
- **[NEW]** Wounded recovery timing feels natural (not too fast, not too punishing)
- News frequency appropriate (3-5 items per day)
- Cover requests occasional, not constant
- Player sees clear impact of decisions
- **[NEW]** Player understands why army strength changed (attribution clear)

**Duration:** 7-9 days (increased due to casualty balance testing)

---

## Integration Points

### With AI Camp Schedule

**Relationship:**
- AI Camp Schedule manages **player lance** duty assignments
- Army Lance Simulation manages **NPC lance** duty assignments
- Both use same duty types and time periods
- Both respect context (war/peace/siege)

**Integration:**
- Player cover requests **override** AI Camp Schedule temporarily
- Player on duty → AI Camp Schedule restricts camp activities
- NPC lances on duty → visible in camp, affects atmosphere

**Data Sharing:**
- Army context (war/peace/siege)
- Time-of-day system (6 periods)
- Duty type definitions

---

### With Lance Life Simulation

**Relationship:**
- Lance Life Simulation handles **player lance** member states (injuries, death, promotions)
- Army Lance Simulation handles **NPC lance** aggregate states
- Same injury/sickness mechanics, simplified for NPC lances

**Integration:**
- Player lance injuries → can trigger cover requests FROM other lances (reciprocity)
- NPC lance injuries → can trigger cover requests TO player lance
- Both feed medical tent capacity / strain

**Data Sharing:**
- Injury types and severities
- Recovery durations
- Medical tent integration

---

### With News/Dispatches System

**Relationship:**
- News System is the **UI layer**
- Army Lance Simulation is the **data source**
- Personal News feed prioritizes lance activities affecting player
- Camp Bulletin provides comprehensive history

**Integration:**
- Lance events generate news via `EnlistedNewsBehavior.Instance.AddPersonalNews()`
- News priority system handles filtering (high/medium/low priority)
- StoryKey deduplication prevents spam

**Data Sharing:**
- News item structure (headline, day, priority, storyKey)
- Feed type (personal vs kingdom)

---

### With Persistent Lance Leaders

**Relationship:**
- Persistent Lance Leaders manage **player's lance** leader personality/memory
- Army Lance Simulation tracks **NPC lance** leaders (generic)
- Optional: NPC lance leaders can be **simplified versions** (name + reputation, no memory/personality)

**Integration:**
- Player lance has full Persistent Lance Leader (personality, memory, reactions)
- NPC lances have basic leader (name only, no personality system)
- Cover requests mention NPC lance leader by name for immersion

**Data Sharing:**
- Leader names (for cover request text)
- Leader existence (for events mentioning leaders)

---

## Testing Strategy

### Unit Tests

**Lance Generation:**
- Roster generates correct number of lances for army size
- Lance names culturally appropriate
- Lance composition reasonable (size, tier distribution)

**Assignment System:**
- Lances get assigned duties appropriate to context
- Assignments advance correctly (days decrement)
- Lances return to camp when assignment completes

**Event Generation:**
- Events fire at expected probability
- Event type distribution matches weights
- Consequences apply correctly (readiness, casualties, status)

**Casualty Application (NEW):**
- Casualties applied to correct troop tiers (matching lance tier ±1)
- Wounded/killed ratio correct (60/40 default)
- Casualty counts respect multipliers (activity type + base multiplier)
- Heroes never killed in simulation
- Wounded troops tracked for recovery
- Wounded recover after configured days
- Party troop counts update correctly
- System respects `affect_actual_troops` config flag

**Cover Requests:**
- Requests fire only when conditions met
- Requests respect cooldown (no spam)
- Accept/decline consequences apply correctly

---

### Integration Tests

**News Integration:**
- Lance events appear in Personal feed
- News priority system works (notable events prioritized)
- Bulletin displays 7-day history
- No duplicate news items (StoryKey deduplication)

**AI Camp Schedule Integration:**
- Player cover requests override schedule
- Player on duty → camp activities restricted
- NPC lances on duty visible in camp

**Lance Life Simulation Integration:**
- Player lance injuries can trigger NPC cover offers (reciprocity)
- NPC lance injuries trigger player cover requests
- Medical tent capacity shared

**Battle Integration:**
- Army strength reflects available lances
- Absent lances noted in post-battle news
- Casualties distributed to lances
- Post-battle states update correctly

---

### Playtest Scenarios

**Scenario 1: 30-Day Peaceful Campaign**
- Verify lance activities feel natural (patrols, training, tax collection)
- Confirm news frequency appropriate (not spammy)
- Check cover request frequency (occasional, not constant)
- Ensure no crashes or performance issues

**Scenario 2: 30-Day Active War Campaign**
- Verify increased lance activity (more patrols, foraging, scouting)
- Confirm higher casualty rates
- Check cover request pressure increases (cascade scenario)
- Ensure battle integration works (absent lances affect strength)

**Scenario 3: Player Agency Testing**
- Accept multiple cover requests → player lance fatigued
- Decline cover requests → other lances cover or duties unfilled
- Verify favor system works (reciprocity)
- Confirm player choices have visible consequences

**Scenario 4: Casualty Application Validation**
- Army with 500 troops loses troops through simulation over 30 days
- Verify army strength decreases realistically (not too fast, not too slow)
- Check wounded troops recover after configured days
- Verify casualties distributed across appropriate troop tiers
- Confirm heroes never killed
- Test with config `affect_actual_troops: false` (should not affect party)
- Test with various casualty multipliers (0.1, 0.5, 1.0)

**Scenario 5: Enemy Army Attrition (Optional)**
- Enable `apply_to_enemy_armies: true`
- Track enemy army strength over 30-day campaign
- Verify enemy armies weaken through attrition
- Check if AI lords recruit to compensate
- Balance test: Is campaign too easy with enemy attrition?

**Scenario 6: Edge Cases**
- Army forms with very few troops (< 50) → few lances, high pressure
- Army takes massive casualties in battle → multiple lances dissolved, crisis
- Long siege → exhaustion, disease, attrition affects all lances
- Army disperses → roster cleanup works correctly
- Simulation casualties + battle casualties → verify no double-counting

---

## Future Enhancements

### Phase 7+: Advanced Features (Post-MVP)

**Lance Specialization:**
- Designate lances as scouts, shock troops, guards, foragers
- Specialized lances more effective at certain duties
- Player can suggest specialization to lord (influence cost)

**Lance Rivalries & Camaraderie:**
- Track relationships between lances
- Rival lances less likely to help each other (more player cover requests)
- Friendly lances offer help more often
- Events: Lance rivalry brawls, friendly competitions

**Player Lance Reputation:**
- Build reputation through consistent performance
- High reputation → less cover requests (other lances want to prove themselves)
- Low reputation → more cover requests (other lances dump unwanted duties on you)

**Commander Personality Effects:**
- Lord's traits affect lance treatment
- Cruel lords: more dangerous assignments, higher casualties
- Merciful lords: better rest cycles, lower pressure
- Strategic lords: assignments more purposeful

**Lance Equipment Tracking:**
- Track aggregate equipment quality per lance
- Equipment degrades on assignments
- Equipment quality affects success chances
- Quartermaster integration (spend influence to equip lances)

**Advanced Cover Request Options:**
- Negotiate for payment (gold instead of favor)
- Trade duties (swap assignments instead of covering)
- Volunteer for duty (proactive, builds reputation)
- Request cover FOR player lance (reciprocity)

**Lance Leader Personalities (NPC):**
- Generate simple personality traits for NPC lance leaders
- Traits affect request tone (respectful, demanding, pleading)
- Build relationships with NPC leaders
- Leaders remember interactions (simplified version of Persistent Lance Leaders)

**Strategic Intel Depth:**
- Lance scouting generates intel on enemy army composition
- Intel accuracy based on lance success
- Use intel to inform battle tactics
- Intel becomes outdated over time

**Dynamic Assignment System:**
- Lord asks player to recommend lance for assignment
- Player can volunteer for dangerous missions (favor gain)
- Player can suggest safer rotation (morale preservation)

---

## Summary

The **Army Lance Activity Simulation** creates a **living military world** where the player is part of a larger organization. Other lances are constantly engaged in **routine military activities** (patrols, foraging, guard duty, scouting), and these activities have real impact on the player's experience.

**What We Simulate (The Small Stuff):**
- Patrols encountering bandits and enemy scouts (small skirmishes, not battles)
- Foraging missions running into trouble
- Guard duty incidents and accidents
- Scouting missions spotting enemy activity
- Disease, desertion, and everyday military life

**What We DON'T Simulate:**
- Full battles (Bannerlord handles those normally)
- Large-scale combat engagements

**Key Benefits:**
- **Immersion:** You're in a real army, not the only lance that matters
- **Mechanical Weight:** Small encounter casualties **actually affect** party troop counts (configurable)
- **Attrition System:** Armies weaken over time through routine operations and small incidents, not just battles
- **Consequences:** Lance activities affect resource availability, battle readiness, cover requests
- **Choices:** Accept or decline cover requests, manage fatigue, build favor
- **Storytelling:** Camp bulletin tells rich stories of lance adventures (patrol ambushes, foraging successes, guard duty incidents)
- **Integration:** Extends existing systems (AI Schedule, Lance Life, News) without duplication

**Casualty System Features (From Small Encounters):**
- Real troops killed or wounded when lances encounter bandits, looters, or enemy scouts during routine activities
- Casualties from patrol ambushes, foraging incidents, accidents (NOT from simulated battles)
- Wounded troops recover over 3-14 days (configurable)
- Casualty rates much lower than real battles (patrol skirmish: 1-5 casualties, battle: 10-100+ casualties)
- Activity-specific danger levels (scouting deep in enemy territory more dangerous than camp guard duty)
- Heroes never killed in simulation
- Configurable: Can be disabled for cosmetic-only simulation
- Optional: Can apply to enemy armies (hardcore mode)

**Implementation Priority:**
- 🟡 Medium priority (Should-Have for rich experience)
- Can be built **after** core systems (AI Camp Schedule, Lance Life Simulation) are stable
- Builds on existing News System (no new UI required)
- 7-8 weeks implementation for full feature set (includes casualty system)
- MVP possible in 4-5 weeks (Phases 0-3: roster, assignments, events with casualties, news)

**Next Steps:**
1. Review with development team
2. Confirm fits within MASTER_IMPLEMENTATION_ROADMAP timeline
3. Assign to implementation track (post AI Camp Schedule completion)
4. Create detailed task breakdown for Phase 0
5. Begin implementation

---

## Quick Reference: Casualty System Settings

### Recommended Configuration Presets

| Setting | Casual | Normal | Realistic | Hardcore |
|---------|--------|--------|-----------|----------|
| **affect_actual_troops** | false | true | true | true |
| **base_casualty_multiplier** | N/A | 0.3 | 0.5 | 0.7 |
| **wounded_recovery_days** | N/A | 7 | 7 | 10 |
| **apply_to_enemy_armies** | false | false | true | true |
| **wounded_vs_killed_ratio** | N/A | 0.7 | 0.6 | 0.5 |
| **Expected 30-day attrition** | 0% | 5-8% | 10-15% | 15-25% |

### Activity Danger Level Reference

| Activity | Danger | Typical Casualties | Frequency |
|----------|--------|-------------------|-----------|
| Guard Duty | 0.05 | ~0-1 per week | Daily |
| Training | 0.02 | Rare accidents | 2-3/week |
| Patrol | 0.30 | 1-3 per week | Daily |
| Foraging | 0.20 | 1-2 per week | 3-4/week |
| Scouting | 0.40 | 2-5 per mission | 1-2/week |
| Convoy Escort | 0.35 | 2-4 per mission | 1/week |
| Bandit Suppression | 0.25 | 1-3 per mission | 2/week |
| Tax Collection | 0.15 | 1-2 per mission | Peace only |

### Key Design Principles

1. **Configurable**: Every player can tune to their preference
2. **Conservative Default**: Start with lower casualties (0.3-0.4 multiplier)
3. **Recoverable Losses**: Most casualties are wounded (60-70%), not killed
4. **Activity-Appropriate**: Dangerous missions have higher rates
5. **Sustainable**: Lords recruit faster than attrition
6. **Player-Focused**: Enemy attrition is optional (balance consideration)
7. **Clear Attribution**: News always explains why troops were lost

---

**Version:** 1.0  
**Author:** Enlisted Development Team  
**Date:** December 14, 2025  
**Status:** 📋 Design Complete, Ready for Review  
**Features:** Lance Activity Simulation + Real Party Casualty System
