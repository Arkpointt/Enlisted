# Lance Life Simulation System

## Overview

The Lance Life Simulation system creates a dynamic, living environment for lance members within the player's unit. Lance members have their own lives, routines, and challenges that intersect with the player's experience. This system runs in parallel with the AI Camp Schedule and creates opportunities for meaningful interactions, progression, and narrative tension.

**Status**: Design Phase - To be integrated with AI Camp Schedule system  
**Category**: Core Feature  
**Dependencies**: AI Camp Schedule, Lance Life Events, Escalation System, Pay System

## Core Concept

Lance members are not static NPCs - they live, work, get injured, and can die. The player exists within this ecosystem and must navigate both their own career progression and the reality that lance leadership positions only open when the current leader moves on (through promotion, death, or other means).

**Integration with Existing Systems:**
- **AI Camp Schedule**: Lance member availability affects duty assignments and coverage
- **Lance Life Events**: Uses existing event infrastructure (`ModuleData/Enlisted/Events/*.json`)
- **Escalation System**: Injuries/deaths can trigger Heat, Discipline, or Lance Rep changes
- **Pay System**: Lance member welfare tied to pay tension and muster outcomes
- **Menu System**: Lance roster view in `enlisted_lance` menu with relationship indicators

---

## Integration Architecture

### Relationship to Existing Systems

```
AI Camp Schedule (duties/activities)
        ↕
Lance Life Simulation (member states)
        ↓
┌───────┴────────┬───────────────┬──────────────┬──────────────────┐
↓                ↓               ↓              ↓                  ↓
Escalation    Lance Life    Menu System    Pay System    Persistent Lance
(Heat/Disc)   Events JSON   (roster view)  (morale)      Leaders (personality)
```

### Integration with Persistent Lance Leaders System

**Responsibility Division:**

**Lance Life Simulation owns:**
- ✅ Promotion timing and eligibility tracking
- ✅ Creating leader vacancies (when leader leaves/dies/promoted)
- ✅ Escalation paths (promotion/transfer/injury/death/retirement)
- ✅ Integration with AI Camp Schedule for member availability

**Persistent Lance Leaders owns:**
- ✅ Leader personality generation and traits
- ✅ Memory system (tracking last 15 player events)
- ✅ Dynamic reactions and dialogue based on history
- ✅ Replacement personality generation after death

**Integration Flow:**
```csharp
// Lance Life Simulation creates vacancy
LanceLifeSimulation.CreateLeaderVacancy(reason: "battle_casualty");
    ↓
// Triggers Persistent Lance Leaders
PersistentLanceLeaders.OnLeaderVacancy(reason);
    ↓
// Checks if player ready for promotion
IF (LanceLifeSimulation.IsPlayerReadyForLanceLeader()):
    PersistentLanceLeaders.OfferPlayerPromotion();
ELSE:
    var newLeader = PersistentLanceLeaders.GenerateLeader(lord, predecessorInfo);
    PersistentLanceLeaders.TriggerIntroductionEvent(newLeader);
```

**See:** `PERSISTENT_LANCE_LEADERS.md` for leader personality and memory system details.

**Event Delivery**: Lance Life Simulation generates events using the existing `LanceLifeEventsAutomaticBehavior` with delivery channels:
- `channel: "inquiry"` - Cover requests, welfare checks, promotion foreshadowing
- `channel: "incident"` - Injury/death notifications, emergency situations
- `channel: "menu"` - Lance leader interactions, roster inspections

**File Locations**:
- Events: `ModuleData/Enlisted/Events/events_lance_simulation.json` (new)
- State Tracking: `src/Features/Lances/Behaviors/LanceLifeSimulationBehavior.cs` (new)
- Menu Integration: `src/Features/Lances/Behaviors/EnlistedLanceMenuBehavior.cs` (existing)
- AI Schedule Hooks: `src/Features/Camp/AIScheduleBehavior.cs` (existing, add hooks)

---

## System Components

### 1. Lance Member State Tracking

Each lance member maintains a dynamic state that influences their behavior and availability:

#### Health States
- **Healthy**: Fully operational, attending all duties
- **Minor Injury**: Can work but with reduced effectiveness
- **Major Injury**: Cannot perform duties, requires recovery time
- **Incapacitated**: Bedridden, may require medical evacuation
- **Dead**: Removed from roster permanently

#### Activity States
- **On Duty**: Performing assigned camp duties (linked to AI Camp Schedule)
- **Off Duty**: Personal time, recreational activities
- **Sick Bay**: Recovering from injury or illness
- **On Leave**: Temporary absence (personal matters)
- **Detached**: Temporarily assigned elsewhere

#### Career Progression
- **Current Rank**: Private, Corporal, Sergeant, etc.
- **Time in Service**: Affects promotion eligibility
- **Performance Record**: Impacts advancement opportunities
- **Readiness for Promotion**: When eligible for next rank

### 2. Cover Request System

Lance members may approach the player to cover their duties when they cannot fulfill them.

**Event Delivery**: Uses `channel: "inquiry"` (popup inquiry dialog)  
**Frequency**: 2-4 times per in-game week  
**Evaluation**: Every 6 hours (existing cadence from `lance_life_events.automatic.evaluation_cadence_hours`)

#### Request Triggers
- **Personal Emergency**: Family matters, urgent business
- **Illness/Injury**: Not severe enough for sick bay but needs rest
- **Conflicting Duty**: Double-booked on camp schedule
- **Exhaustion**: Overworked, needs relief
- **Special Opportunity**: Chance for advancement, training, etc.

#### Player Decision Points

**Event JSON Example**:
```json
{
  "id": "lance_cover_request_personal",
  "category": "lance_simulation",
  "channel": "inquiry",
  "triggers": {
    "all": ["is_enlisted", "ai_safe"],
    "time_of_day": ["evening"]
  },
  "titleId": "lance_cover_request_title",
  "bodyId": "lance_cover_request_body",
  "title": "A Favor to Ask",
  "body": "{LANCE_MEMBER} approaches during evening mess.\n\n\"{PLAYER_SHORT}, I need a favor. I'm supposed to run messages tomorrow morning, but I've got a conflict. Could you handle it? It's just delivering mail to the officers' tents.\"",
  "options": [
    {
      "id": "accept",
      "textId": "lance_cover_accept",
      "text": "[Accept] \"Sure, I'll take care of it.\"",
      "effects": {
        "lance_reputation": 2,
        "fatigue": 2
      },
      "rewards": {
        "relationship_member": 10
      },
      "resultText": "{LANCE_MEMBER} grins with relief. \"Thanks. I owe you one.\""
    },
    {
      "id": "negotiate",
      "textId": "lance_cover_negotiate",
      "text": "[Negotiate] \"I'll do it if you cover my stable duty next week.\"",
      "effects": {
        "lance_reputation": 0,
        "fatigue": 2
      },
      "flags": ["favor_owed"],
      "resultText": "{LANCE_MEMBER} nods. \"Fair enough. Deal.\""
    },
    {
      "id": "refuse",
      "textId": "lance_cover_refuse",
      "text": "[Refuse] \"Sorry, I've got my own duties to handle.\"",
      "effects": {
        "lance_reputation": -1
      },
      "rewards": {
        "relationship_member": -5
      },
      "resultText": "{LANCE_MEMBER} looks disappointed but nods. \"Understood.\""
    },
    {
      "id": "report",
      "textId": "lance_cover_report",
      "text": "[Report] \"You should request official leave.\"",
      "effects": {
        "lance_reputation": -2,
        "discipline": 0
      },
      "resultText": "{LANCE_MEMBER}'s face hardens. \"Right. Official channels.\""
    }
  ]
}
```

#### Consequences
- **Accepting**: 
  - Player gains additional duty in AI Camp Schedule
  - Builds relationship with lance member
  - May result in favors returned later
  - Risk of fatigue if over-committed
  
- **Refusing**:
  - Lance member may face consequences (disciplinary action, missed opportunity)
  - Relationship strain
  - Lance member may find alternative solution
  - Reputation as "unhelpful" among peers

### 3. Injury and Death System

**Event Delivery**: Uses `channel: "incident"` (native incident notification) for immediate alerts  
**Integration**: Connects to Medical Risk escalation track (0-5 range)  
**Menu Display**: Injured/fallen members shown in `enlisted_lance` menu with welfare options

#### Injury Sources
- **Training Accidents**: Drill mishaps, equipment failures (5% chance from training events)
- **Camp Incidents**: Construction accidents, animal encounters (random daily checks)
- **Disease/Illness**: Infectious outbreaks, chronic conditions (ties to Camp Life Simulation conditions)
- **Combat Wounds**: Post-battle injury checks for lance members in battles
- **Environmental**: Weather exposure, accidents while on duty (AI Schedule context-aware)
- **Random Events**: Falls, food poisoning, etc. (low probability background risk)

#### Injury Probability Modifiers

```csharp
Base Injury Chance = 0.1% per day (0.001)

Modifiers:
+ Training Activity: +5% during active drill
+ Combat: +15% if lance in battle
+ Disease Outbreak: +10% when camp conditions poor
+ Weather: +2% in extreme conditions
+ Fatigue: +1% per 10 camp fatigue above normal
- Medical Staff Quality: -5% with good surgeon
- Rest Days: -50% when not on active duty
```

#### Injury Progression

**Event JSON Example - Injury Occurrence**:
```json
{
  "id": "lance_member_training_injury",
  "category": "lance_simulation",
  "channel": "incident",
  "triggers": {
    "all": ["is_enlisted", "lance_training_active"]
  },
  "titleId": "lance_injury_title",
  "title": "Training Accident",
  "bodyId": "lance_injury_body",
  "body": "During formation drill, {LANCE_MEMBER} takes a hard fall. The sergeant calls for the surgeon.",
  "notification_type": "warning",
  "effects": {
    "member_state": "minor_injury",
    "medical_risk": 2,
    "schedule_remove": true
  },
  "follow_up_event": "lance_injury_recovery",
  "follow_up_delay_hours": 24
}
```

#### Death Mechanics

Lance members can die from:
- **Severe Injuries**: Untreated wounds, complications (Medical Risk ≥ 5)
- **Disease**: Fatal illness, epidemic (Camp Life Simulation outbreak events)
- **Combat**: Post-battle death checks (rare, 0.5-2% depending on battle severity)
- **Accidents**: Rare but possible fatal accidents (0.01% daily base rate)
- **Narrative Events**: Story-driven deaths for dramatic effect (escalation consequences)

**Death Event Example**:
```json
{
  "id": "lance_member_death_illness",
  "category": "lance_simulation",
  "channel": "incident",
  "titleId": "lance_death_title",
  "title": "A Soldier Falls",
  "bodyId": "lance_death_illness_body",
  "body": "Despite the surgeon's efforts, {LANCE_MEMBER} succumbs to the fever. Another empty bunk.",
  "notification_type": "critical",
  "effects": {
    "member_state": "dead",
    "lance_reputation": -5,
    "morale": -10,
    "roster_remove": true
  },
  "flags": ["memorial_pending"],
  "follow_up_event": "lance_memorial_service",
  "follow_up_delay_hours": 48
}
```

**Impact of Death**:
- Permanent roster gap
- Lance Reputation: -5 (grief and loss)
- Camp Morale: -10 (escalating system effect)
- Memorial service event (2 days later)
- Opens promotion opportunity (if Lance Leader dies)
- May trigger investigation if preventable (e.g., neglected medical care)

### 4. Lance Leader Vacancy and Promotion

**Integration Note**: This system integrates with the existing 9-tier progression system (T1-T4 Enlisted, T5-T6 Officer, T7-T9 Commander) documented in `Lance Assignments`.

#### The Promotion Constraint

**Core Rule**: The player cannot be promoted to Lance Leader (typically T4-T5 transition) while the position is occupied.

This creates natural tension and gameplay dynamics:
- Player builds skills and readiness through Duties System and Formation Training
- Player waits for opportunity while gaining Lance Reputation
- System must create vacancy when player is ready
- Integrates with existing tier progression and culture-specific ranks

#### Player Readiness Tracking

**Implementation**: `LanceLifeSimulationBehavior.IsPlayerReadyForLanceLeader()`

The system monitors when the player becomes eligible for Lance Leader:

```csharp
public bool IsPlayerReadyForLanceLeader()
{
    var enlistment = EnlistmentBehavior.Instance;
    if (enlistment == null || !enlistment.IsEnlisted) return false;
    
    // Minimum tier requirement (T4 = Corporal-equivalent)
    if (enlistment.CurrentTier < 4) return false;
    
    // Time in service requirement (90 days minimum)
    if (enlistment.DaysServed < 90) return false;
    
    // Lance Reputation requirement (must be at least Accepted)
    var escalation = EscalationManager.Instance;
    if (escalation?.GetLanceReputation() < 5) return false;
    
    // Discipline requirement (cannot have major infractions)
    if (escalation?.GetDiscipline() >= 5) return false;
    
    // Skills requirement (Leadership or relevant combat skill)
    var hero = Hero.MainHero;
    if (hero.GetSkillValue(DefaultSkills.Leadership) < 50 &&
        hero.GetSkillValue(DefaultSkills.OneHanded) < 75) return false;
    
    return true;
}
```

**Readiness Indicators**:
- Displays in `enlisted_status` menu when ready: "⭐ Ready for Lance Leader promotion"
- Shows in `enlisted_lance` menu: "You are prepared to lead this lance."
- Foreshadowing events begin appearing 1-2 weeks before readiness achieved

When **all criteria met** → Player Status: "Ready for Lance Leader Promotion"  
→ System begins Escalation Mechanism

#### Escalation Mechanism

**Event Delivery**: Uses `channel: "inquiry"` for major escalation milestones  
**Timing**: 1-12 weeks after player readiness achieved  
**Urgency Scaling**: Each week without escalation increases probability by 10%

When the player reaches readiness for Lance Leader but the position is occupied, the system **must** create a vacancy through one of these escalation paths:

**Selection Algorithm**:
```csharp
private EscalationPath SelectEscalationPath(Hero lanceLeader)
{
    var weights = new Dictionary<EscalationPath, float>();
    
    // Base weights from config
    weights[EscalationPath.Promotion] = 0.40f;
    weights[EscalationPath.Transfer] = 0.30f;
    weights[EscalationPath.Injury] = 0.15f;
    weights[EscalationPath.Death] = 0.10f;
    weights[EscalationPath.Retirement] = 0.05f;
    
    // Adjust based on lance leader characteristics
    if (lanceLeader.Age > 45)
        weights[EscalationPath.Retirement] *= 2.0f;
    
    if (IsAtWar(lanceLeader.MapFaction))
    {
        weights[EscalationPath.Injury] *= 1.5f;
        weights[EscalationPath.Death] *= 1.3f;
    }
    
    if (lanceLeader.GetSkillValue(DefaultSkills.Leadership) > 150)
        weights[EscalationPath.Promotion] *= 1.5f;
    
    // Normalize and select
    return WeightedRandom(weights);
}
```

##### Path 1: Lance Leader Promotion (Preferred)
- Lance Leader receives promotion to higher position
- Natural, positive resolution
- Lance Leader transfers to new unit or role
- Creates vacancy organically
- **Timing**: 1-4 weeks after player readiness

**Event Flow**:
```
1. Player achieves readiness
2. System flags Lance Leader for promotion track
3. Lance Leader receives orders/opportunity
4. Promotion ceremony/transition event
5. Lance Leader position vacated
6. Player becomes eligible for promotion
```

##### Path 2: Lance Leader Transfer
- Lance Leader reassigned to different unit
- Lateral move, not necessarily promotion
- May be temporary or permanent
- **Timing**: 2-6 weeks after player readiness

##### Path 3: Lance Leader Injury/Incapacitation
- Lance Leader suffers serious injury
- Requires extended recovery or medical discharge
- More dramatic, potentially darker tone
- **Timing**: 1-8 weeks after player readiness

##### Path 4: Lance Leader Death
- Most dramatic option, use sparingly
- Can be combat, accident, or illness
- Significant narrative weight
- Creates memorial/aftermath events
- **Timing**: 2-12 weeks after player readiness

##### Path 5: Lance Leader Retirement
- Lance Leader completes service term
- Peaceful, dignified exit
- Less dramatic but realistic
- **Timing**: 4-12 weeks after player readiness

#### Escalation Selection Logic

The system selects an escalation path based on:

1. **Narrative Context**: What fits the current story?
2. **Lance Leader Characteristics**: Their age, service time, health
3. **Campaign State**: Wartime, peacetime, training period
4. **Recent Events**: Avoid repetitive patterns
5. **Player Relationship**: With Lance Leader (affects emotional impact)
6. **Random Variation**: Prevent predictability

**Example Decision Tree**:
```
IF Lance Leader is near retirement age → Retirement path (50% chance)
ELSE IF campaign is active combat → Injury/Death path (40% chance)
ELSE IF Lance Leader is high performer → Promotion path (60% chance)
ELSE → Transfer path (30% chance)
```

#### Pre-Vacancy Narrative

Before the vacancy occurs, the system should **foreshadow** the transition:

- **Promotion Path**: Lance Leader mentions opportunities, increased responsibilities
- **Transfer Path**: Rumors of reassignments, new units forming
- **Injury Path**: Lance Leader shows fatigue, takes risks, health concerns
- **Death Path**: Dark omens, dangerous situations, close calls
- **Retirement Path**: Lance Leader talks about service time, future plans

This creates narrative coherence and player anticipation.

### 5. Promotion Ceremony and Transition

Once vacancy is created and player is ready:

#### Announcement Phase
- Commander summons player
- Formal promotion offer
- Player can accept or (rarely) decline
- Briefing on new responsibilities

#### Transition Period
- Player assumes Lance Leader duties
- Learns leadership-specific tasks
- Meets with unit members in new capacity
- Adjusts to AI Camp Schedule with leadership authority

#### New Gameplay
- Can now assign duties to others (within AI Camp Schedule)
- Receives different types of events and challenges
- Gains authority but also accountability
- New relationships dynamics with former peers

---

## Integration with AI Camp Schedule

### Parallel Systems

The Lance Life Simulation runs **in parallel** with the AI Camp Schedule:

```
AI Camp Schedule (What lance members SHOULD be doing)
                    ↕ (continuous interaction)
Lance Life Simulation (What's ACTUALLY happening to lance members)
```

### State Modifications

Lance Life events modify the AI Camp Schedule dynamically:

#### Example 1: Injury During Duty
```
Time: 0800
Scheduled: Pvt. Johnson → Guard Duty (North Post)
Event: Johnson injures ankle on patrol
Result: Johnson removed from current duty
        Johnson added to Sick Bay
        Guard Duty North Post → VACANT (requires coverage)
        System alerts player or assigns replacement
```

#### Example 2: Cover Request Accepted
```
Time: 1200
Scheduled: Player → Personal Time
           Cpl. Martinez → Equipment Maintenance
Event: Martinez requests cover (family visit)
Player accepts
Result: Player schedule updated → Equipment Maintenance (1400-1600)
        Martinez schedule updated → Personal Leave (1400-1600)
        AI Camp Schedule adjusted accordingly
```

#### Example 3: Death Creates Permanent Gap
```
Time: 0600
Event: Sgt. Williams dies (illness)
Result: All future schedule entries for Williams → REMOVED
        Williams' regular duties → VACANT
        System redistributes duties among remaining lance
        Player may receive additional responsibilities
        Opens promotion opportunity for qualified lance member
```

### Schedule Awareness

Lance Life events should consider the AI Camp Schedule:

- **Injuries more likely during active duties**: Guard duty → exposure risks, Training → accident risks
- **Cover requests consider schedule conflicts**: Members ask when they have conflicting commitments
- **Deaths impact schedule coverage**: System must handle sudden gaps
- **Promotions reorganize schedule**: New Lance Leader receives leadership duties

### Duty Coverage Priorities

When lance members are unavailable, the system must assign coverage:

**Priority Order**:
1. **Player (if appropriate)**: Can take on most duties
2. **Off-duty lance members**: Those with free time
3. **Cross-trained personnel**: Those qualified for the duty
4. **Temporary overload**: Assign double duty to someone (with fatigue risk)
5. **Duty skipped**: Mark as UNFULFILLED (with consequences)

---

## Event Types and Examples

### Cover Request Events

#### Event: "A Favor to Ask"
```
Trigger: Lance member has schedule conflict
Condition: Player is available or can rearrange

Cpl. Davis approaches you during evening mess.

Davis: "Hey, I need a favor. I'm supposed to run messages tomorrow 
morning, but Sergeant pulled me for inventory duty at the same time. 
Could you handle the message run? It's easy, just delivering mail 
to the officers' tents."

Options:
1. [Help] "Sure, I'll take care of it."
   → Player assigned message duty tomorrow 0800-1000
   → Davis relationship +10
   → Potential for reciprocal favor later
   
2. [Negotiate] "I'll do it if you cover my stable duty next week."
   → Player assigned message duty tomorrow
   → Davis assigned stable duty (5 days from now)
   → Establishes quid pro quo relationship
   
3. [Refuse] "Sorry, I've got my own duties to handle."
   → Davis must find another solution or face consequences
   → Davis relationship -5
   → Davis may remember refusal
   
4. [Suggest Alternative] "Why don't you ask Johnson? He's off duty."
   → Player facilitates solution without commitment
   → Neutral relationship impact
   → Johnson may cover (if willing)
```

### Injury Events

#### Event: "Training Accident"
```
Trigger: Random during combat drill schedule time
Condition: Lance member performing training duty

During afternoon drill, Pvt. Morrison takes a hard fall while 
practicing cavalry maneuvers. He's clutching his arm and clearly 
in pain.

The drill sergeant looks to you: "Get Morrison to the surgeon. 
Someone needs to cover his watch duty tonight."

Immediate Impact:
- Morrison → Status: Injured (Minor)
- Morrison → Removed from evening watch duty
- Morrison → Assigned to Sick Bay (3-7 days recovery)
- Player or another lance member must cover watch

Follow-up Event (next day):
"Morrison's Recovery"

The surgeon reports Morrison has a sprained wrist. He'll be 
light duty for a week, then full recovery expected.

During this time:
- Morrison cannot perform physical duties
- Morrison assigned to administrative/light tasks
- Schedule automatically adjusts
- Morrison relationship +5 if player covered his duty
```

#### Event: "Serious Illness"
```
Trigger: Random, increased during disease outbreak
Condition: Any lance member

Sgt. Williams hasn't reported for morning formation. You find 
him in his tent, burning with fever and barely conscious.

Options:
1. [Get Surgeon] "I'll get the surgeon immediately!"
   → Fast response, best outcome chance
   → Williams status: Incapacitated
   → Treatment begins immediately
   
2. [Help Self] "Let me get you some water and medicine."
   → Slower response, moderate outcome
   → Williams status: Major Illness
   → Delayed treatment
   
3. [Ignore] "He's probably just tired."
   → Worst outcome, condition worsens
   → Williams may die if severe illness
   → Reputation damage if discovered

Outcome depends on choice and random factors:
- Recovery: Returns to duty in 1-2 weeks
- Prolonged Illness: Extended sick bay (3-6 weeks)
- Death: 5-15% chance if severe and delayed treatment
```

### Death Events

#### Event: "Fatal Accident"
```
Trigger: Rare random event during dangerous duties
Condition: Lance member performing hazardous duty

You hear shouts from the construction site. Pvt. Kowalski was 
helping repair the stable roof when scaffolding collapsed. 

By the time you arrive, the surgeon is already there, but 
shaking his head solemnly. Kowalski didn't survive.

Impact:
- Kowalski permanently removed from roster
- Morale penalty for entire lance (-10)
- Memorial service scheduled (next day)
- Kowalski's duties redistributed
- Investigation may occur (was it preventable?)
- Player may be questioned

Follow-up:
- Memorial event (player can attend/speak)
- Kowalski's possessions handled
- New recruit may arrive (1-2 weeks)
- Lance members discuss the loss (ambient dialogue)
```

#### Event: "Disease Outbreak Death"
```
Trigger: During epidemic event, severe illness
Condition: Lance member critically ill

Despite the surgeon's best efforts, Cpl. Martinez succumbs 
to the fever that's been ravaging the camp. He's the third 
death this week.

Impact:
- Martinez permanently removed from roster
- Morale penalty (-15, cumulative with other deaths)
- Fear/tension increases in camp
- Other lance members may request sick leave
- Medical resources strained
- Potential for player to contract illness

Follow-up:
- Quick burial (prevent contagion)
- Enhanced medical protocols
- Lance members express fear/grief
- Commander may address the situation
```

### Lance Leader Escalation Events

#### Event: "Promotion Opportunity" (Path 1)
```
Trigger: Player achieves Lance Leader readiness
Condition: Current Lance Leader eligible for promotion

Your Lance Leader, Sgt. Bradley, returns from a meeting with 
the Colonel looking pleased.

Bradley: "Good news. I've been selected for promotion to 
Company Sergeant Major. I'll be transferring to HQ staff 
next month. That means this lance is going to need a new 
leader. Keep up the good work—the Colonel's watching."

Timeline:
- Week 1-2: Bradley begins leadership transition
- Week 2-3: Bradley introduces potential successors (including player)
- Week 3-4: Bradley's promotion finalized
- Week 4: Lance Leader position vacant
- Week 4: Player offered promotion to Lance Leader

During transition:
- Bradley delegates more to player
- Player gains informal leadership experience
- Other lance members acknowledge coming change
- Events test player's readiness
```

#### Event: "Combat Casualty" (Path 4)
```
Trigger: Player achieves Lance Leader readiness + combat scenario
Condition: Current Lance Leader in combat situation

The lance returned from patrol this morning. Sgt. Bradley 
didn't make it back. He took enemy fire while covering the 
withdrawal. He saved the lance, but paid the ultimate price.

The camp is in shock. Bradley was respected, experienced, 
and capable. Now the lance needs a new leader, and all eyes 
are turning to you.

Immediate Impact:
- Bradley permanently removed from roster
- Lance morale severely impacted (-20)
- Commander summons player
- Emergency leadership vacuum
- Memorial service scheduled

Timeline:
- Day 1: Death announced, initial grief
- Day 2: Memorial service (player expected to speak)
- Day 3: Commander offers player Lance Leader position
- Day 3-4: Player assumes emergency leadership
- Week 1: Lance adjusts to new leadership
- Week 2+: Gradual morale recovery

This path is most dramatic and creates strong emotional impact.
```

#### Event: "Medical Discharge" (Path 3)
```
Trigger: Player achieves Lance Leader readiness
Condition: Current Lance Leader suffers disabling injury

Sgt. Bradley was injured during a training accident last week. 
The surgeon's report just came through: Bradley's leg injury 
won't heal properly. He's being medically discharged and sent 
home.

Bradley is devastated—he's been in the army for 15 years. 
But he can barely walk now, and won't be fit for duty.

Timeline:
- Week 1: Injury occurs, prognosis uncertain
- Week 2: Medical discharge decision
- Week 3: Bradley processes discharge, says farewells
- Week 4: Bradley departs, position vacant
- Week 4: Player offered Lance Leader position

Emotional tone:
- Sympathy for Bradley's loss
- Lance members affected by seeing leader disabled
- Bittersweet transition
- Bradley may offer player advice before leaving
```

---

## System Parameters and Configuration

### Timing Windows

```json
{
  "injury_check_frequency": "daily",
  "minor_injury_chance": 0.02,
  "major_injury_chance": 0.005,
  "death_chance": 0.001,
  "recovery_time_minor": "3-7 days",
  "recovery_time_major": "2-6 weeks",
  
  "cover_request_frequency": "2-4 per week",
  "cover_request_player_target_chance": 0.40,
  
  "escalation_delay_min": "1 week",
  "escalation_delay_max": "12 weeks",
  "escalation_urgency_increase": "weekly",
  
  "promotion_path_weights": {
    "promotion": 0.40,
    "transfer": 0.30,
    "injury": 0.15,
    "death": 0.10,
    "retirement": 0.05
  }
}
```

### Relationship Tracking

Lance members maintain relationship scores with the player:

- **Friendly (75-100)**: Will cover for player, share information, loyal
- **Cordial (50-74)**: Professional relationship, occasional favors
- **Neutral (25-49)**: Standard military relationship
- **Cold (10-24)**: Distant, unlikely to help, tension
- **Hostile (0-9)**: Active dislike, may cause problems

### Morale System Integration

Lance Life events affect unit morale:

- **Death**: -10 to -20 morale (based on popularity)
- **Injury**: -2 to -5 morale (based on severity)
- **Cover Requests Unfulfilled**: -1 morale (cumulative)
- **Successful Mutual Support**: +2 morale
- **Lance Leader Transition**: -5 to +5 (based on circumstances)

---

## Implementation Phases

### Phase 1: Foundation (Week 1-2)
- Lance member state tracking system
- Health state machine (Healthy → Injured → Dead)
- Basic injury events
- Integration hooks with AI Camp Schedule

### Phase 2: Cover Request System (Week 3-4)
- Cover request event framework
- Player decision tree
- Schedule modification logic
- Relationship impact system

### Phase 3: Injury and Death (Week 5-6)
- Expanded injury types and sources
- Death mechanics and consequences
- Recovery progression system
- Medical facility interactions

### Phase 4: Lance Leader Progression (Week 7-9)
- Player readiness tracking
- Escalation trigger system
- Lance Leader event paths (all 5)
- Promotion ceremony and transition

### Phase 5: Polish and Balance (Week 10-12)
- Event variety and narrative quality
- Probability tuning
- Integration testing with AI Camp Schedule
- Player feedback and iteration

---

## Design Considerations

### Balancing Player Agency vs. Realism

**Challenge**: Players want control, but realistic military life has limited agency.

**Solution**: 
- Give player meaningful choices within realistic constraints
- Cover requests = player agency
- Injury/death = realistic consequences
- Promotion timing = system-driven but narratively justified

### Avoiding Frustration

**Risk**: Player waits too long for Lance Leader promotion.

**Mitigation**:
- Maximum escalation delay: 12 weeks
- Escalation urgency increases weekly
- Foreshadowing events keep player engaged
- Alternative progression paths (skills, relationships) during wait

### Death Frequency

**Risk**: Too many deaths = depressing, too few = lacks impact.

**Balance**:
- Death should be rare (0.1% daily check = ~3% annual mortality)
- More common during combat/epidemic scenarios
- Deaths should feel meaningful, not random
- Memorial events give emotional closure

### Lance Leader Escalation Feels Forced

**Risk**: Player sees through the "system creating vacancy" mechanic.

**Mitigation**:
- Variable timing (1-12 weeks)
- Multiple escalation paths
- Strong narrative justification
- Foreshadowing makes it feel natural
- Escalation reflects Lance Leader's actual characteristics

---

## Narrative Opportunities

### Personal Stories

Lance Life creates organic narrative moments:

- **Rivalry**: Another lance member also wants Lance Leader
- **Mentorship**: Current Lance Leader teaches player
- **Tragedy**: Losing a friend to injury or death
- **Loyalty**: Lance members who cover for player in crisis
- **Betrayal**: Lance member who refuses to help when needed

### Emergent Gameplay

The system creates unscripted stories:

- Player covers for multiple people, becomes exhausted
- Epidemic decimates the lance, survivors bond
- Lance Leader dies, player must lead unprepared
- Player refuses to help someone who later dies
- Lance member player helped returns favor at crucial moment

### Integration with Broader Campaign

Lance Life events can tie into larger story:

- Deaths increase during major battle arcs
- Promotions accelerate during expansion phases
- Cover requests increase during supply shortages
- Lance solidarity affects overall army morale
- Player's leadership of lance prepares for higher command

---

## Technical Architecture

### Core Behavior Class

**File**: `src/Features/Lances/Behaviors/LanceLifeSimulationBehavior.cs` (new)

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;
using TaleWorlds.Core;
using Enlisted.Mod.Core.Logging;
using Enlisted.Features.Escalation;

namespace Enlisted.Features.Lances.Behaviors
{
    /// <summary>
    /// Manages lance member simulation: health, injuries, deaths, and promotion escalation.
    /// Integrates with AI Camp Schedule and Lance Life Events.
    /// </summary>
    public sealed class LanceLifeSimulationBehavior : CampaignBehaviorBase
    {
        private static LanceLifeSimulationBehavior _instance;
        public static LanceLifeSimulationBehavior Instance => _instance;
        
        // Lance member state tracking
        private Dictionary<string, LanceMemberState> _memberStates = new();
        
        // Player promotion escalation
        private bool _playerReadyForLanceLeader;
        private CampaignTime _readinessAchievedTime;
        private EscalationPath? _selectedEscalationPath;
        private CampaignTime _escalationTargetDate;
        private List<string> _escalationForeshadowFlags = new();
        
        // Injury/death tracking
        private Dictionary<string, float> _injuryRiskModifiers = new();
        private int _lastInjuryCheckDay = -1;
        
        public LanceLifeSimulationBehavior()
        {
            _instance = this;
        }
        
        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
            CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(this, OnMissionEnded);
        }
        
        public override void SyncData(IDataStore dataStore)
        {
            try
            {
                dataStore.SyncData("lls_memberStates", ref _memberStates);
                dataStore.SyncData("lls_playerReady", ref _playerReadyForLanceLeader);
                dataStore.SyncData("lls_readinessTime", ref _readinessAchievedTime);
                dataStore.SyncData("lls_escalationPath", ref _selectedEscalationPath);
                dataStore.SyncData("lls_escalationTarget", ref _escalationTargetDate);
                dataStore.SyncData("lls_foreshadowFlags", ref _escalationForeshadowFlags);
                dataStore.SyncData("lls_injuryRisks", ref _injuryRiskModifiers);
                dataStore.SyncData("lls_lastInjuryCheck", ref _lastInjuryCheckDay);
                
                // Safe initialization
                _memberStates ??= new Dictionary<string, LanceMemberState>();
                _escalationForeshadowFlags ??= new List<string>();
                _injuryRiskModifiers ??= new Dictionary<string, float>();
            }
            catch (Exception ex)
            {
                ModLogger.Error("LanceLifeSim", "Error syncing data", ex);
                InitializeDefaults();
            }
        }
        
        private void InitializeDefaults()
        {
            _memberStates = new Dictionary<string, LanceMemberState>();
            _escalationForeshadowFlags = new List<string>();
            _injuryRiskModifiers = new Dictionary<string, float>();
            _lastInjuryCheckDay = -1;
        }
        
        // Public API methods documented in API Reference section
    }
}
```

### Data Structures

```csharp
/// <summary>
/// Persistent state for a single lance member.
/// </summary>
[SaveableStruct(1)]
public struct LanceMemberState
{
    [SaveableField(1)]
    public string MemberId; // Unique identifier
    
    [SaveableField(2)]
    public string Name;
    
    [SaveableField(3)]
    public string Rank;
    
    [SaveableField(4)]
    public HealthState Health;
    
    [SaveableField(5)]
    public ActivityState Activity;
    
    [SaveableField(6)]
    public int RelationshipWithPlayer; // 0-100
    
    [SaveableField(7)]
    public int TimeInService; // days
    
    [SaveableField(8)]
    public bool EligibleForPromotion;
    
    [SaveableField(9)]
    public int DaysSinceInjury; // for recovery tracking
    
    [SaveableField(10)]
    public string ScheduleOverride; // duty_id if covering for someone
    
    [SaveableField(11)]
    public List<string> ActiveFlags; // e.g., "on_leave", "sick_bay", "memorial_attended"
}

public enum HealthState
{
    Healthy = 0,
    MinorInjury = 1,
    MajorInjury = 2,
    Incapacitated = 3,
    Dead = 4
}

public enum ActivityState
{
    OnDuty = 0,
    OffDuty = 1,
    SickBay = 2,
    OnLeave = 3,
    Detached = 4
}

/// <summary>
/// Player promotion escalation tracking.
/// </summary>
[SaveableStruct(2)]
public struct PlayerEscalationState
{
    [SaveableField(1)]
    public bool PlayerReady;
    
    [SaveableField(2)]
    public CampaignTime ReadinessAchievedTime;
    
    [SaveableField(3)]
    public EscalationPath? SelectedPath;
    
    [SaveableField(4)]
    public CampaignTime EscalationTargetDate;
    
    [SaveableField(5)]
    public List<string> ForeshadowFlags; // e.g., "leader_promotion_hint_1"
}

public enum EscalationPath
{
    None = 0,
    Promotion = 1,
    Transfer = 2,
    Injury = 3,
    Death = 4,
    Retirement = 5
}
```

### Event System Integration

**Integration with LanceLifeEventsAutomaticBehavior**:

The Lance Life Simulation uses the existing event infrastructure. Events are defined in JSON and queued through the standard evaluation system.

```csharp
/// <summary>
/// Daily processing of lance member simulation.
/// </summary>
private void OnDailyTick()
{
    try
    {
        if (!IsFeatureEnabled()) return;
        if (!IsEnlisted()) return;
        
        // 1. Check for injuries (random + context-based)
        ProcessInjuryChecks();
        
        // 2. Process recovery for injured members
        ProcessRecoveryProgress();
        
        // 3. Update player readiness for Lance Leader
        UpdatePlayerReadinessTracking();
        
        // 4. Process escalation if player ready
        ProcessLanceLeaderEscalation();
        
        // 5. Clean up dead members from roster
        ProcessRosterCleanup();
    }
    catch (Exception ex)
    {
        ModLogger.Error("LanceLifeSim", "Error in daily tick", ex);
    }
}

/// <summary>
/// Escalation processing - creates vacancy when player ready.
/// </summary>
private void ProcessLanceLeaderEscalation()
{
    if (!_playerReadyForLanceLeader) return;
    if (!IsLanceLeaderPositionOccupied()) return;
    
    var now = CampaignTime.Now;
    
    // Initiate escalation if not started
    if (_selectedEscalationPath == null)
    {
        InitiateEscalation();
        return;
    }
    
    // Check for foreshadowing events
    ProcessForeshadowingEvents();
    
    // Check if target date reached
    if (now >= _escalationTargetDate)
    {
        ExecuteEscalation();
    }
}

/// <summary>
/// Queue escalation event through existing event system.
/// </summary>
private void QueueEscalationEvent(string eventId)
{
    var eventsMgr = LanceLifeEventsAutomaticBehavior.Instance;
    if (eventsMgr == null)
    {
        ModLogger.Warning("LanceLifeSim", "Cannot queue escalation event - events manager not available");
        return;
    }
    
    // Queue event for next safe moment (uses existing safety checks)
    eventsMgr.QueueEventById(eventId);
    ModLogger.Info("LanceLifeSim", $"Queued escalation event: {eventId}");
}
```

**Event JSON Structure for Escalation**:

```json
{
  "id": "lance_leader_promotion_foreshadow",
  "category": "lance_simulation",
  "channel": "inquiry",
  "triggers": {
    "all": ["is_enlisted", "player_ready_lance_leader", "escalation_path_promotion"]
  },
  "titleId": "lance_leader_promo_hint_title",
  "title": "Opportunity Ahead",
  "bodyId": "lance_leader_promo_hint_body",
  "body": "{LANCE_LEADER_RANK} {LANCE_LEADER_SHORT} returns from a meeting with the Colonel looking pleased.\n\n\"Good news. I've been selected for promotion to Company Sergeant Major. I'll be transferring to HQ staff next month. That means this lance is going to need a new leader.\"\n\nHe glances at you. \"Keep up the good work—the Colonel's watching.\"",
  "options": [
    {
      "id": "acknowledge",
      "textId": "lance_leader_promo_ack",
      "text": "[Acknowledge] \"Congratulations, Sergeant. Well deserved.\"",
      "effects": {
        "lance_reputation": 2
      },
      "flags": ["foreshadow_acknowledged"],
      "resultText": "{LANCE_LEADER_SHORT} nods appreciatively. \"We'll see who's ready when the time comes.\""
    }
  ]
}
```

### AI Camp Schedule Hooks

**Integration Points**: `src/Features/Camp/AIScheduleBehavior.cs` (existing, add hooks)

```csharp
/// <summary>
/// Interface for modifying AI Camp Schedule from external systems.
/// </summary>
public interface IScheduleModifier
{
    /// <summary>
    /// Remove a lance member from all scheduled duties in date range.
    /// </summary>
    void RemoveLanceMemberFromSchedule(string memberId, CampaignTime startDate, CampaignTime endDate);
    
    /// <summary>
    /// Assign a replacement to cover a duty.
    /// </summary>
    void AssignCoverageDuty(string replacementId, string dutyId, CampaignTime date);
    
    /// <summary>
    /// Redistribute duties when a member becomes unavailable.
    /// </summary>
    void RedistributeDuties(List<string> availableMemberIds);
    
    /// <summary>
    /// Mark a duty as unfulfilled (no coverage found).
    /// </summary>
    void MarkDutyUnfulfilled(string dutyId, CampaignTime date, string reason);
    
    /// <summary>
    /// Get duties assigned to a specific member for a date.
    /// </summary>
    List<ScheduledDuty> GetMemberDuties(string memberId, CampaignTime date);
}

/// <summary>
/// Hook into AI Schedule from Lance Life Simulation.
/// </summary>
public class LanceLifeScheduleIntegration
{
    private AIScheduleBehavior _scheduleBehavior;
    
    public void OnMemberInjured(string memberId, int estimatedRecoveryDays)
    {
        var startDate = CampaignTime.Now;
        var endDate = CampaignTime.Now + CampaignTime.Days(estimatedRecoveryDays);
        
        // Remove from schedule
        _scheduleBehavior?.RemoveLanceMemberFromSchedule(memberId, startDate, endDate);
        
        // Try to find coverage
        var duties = _scheduleBehavior?.GetMemberDuties(memberId, startDate);
        if (duties != null && duties.Count > 0)
        {
            TryFindCoverage(duties);
        }
        
        ModLogger.Info("LanceLifeSim", $"Member {memberId} removed from schedule for {estimatedRecoveryDays} days");
    }
    
    private void TryFindCoverage(List<ScheduledDuty> unfilledDuties)
    {
        // Priority order for coverage:
        // 1. Player (if appropriate and available)
        // 2. Off-duty lance members
        // 3. Cross-trained personnel
        // 4. Temporary overload (with fatigue risk)
        // 5. Mark as unfulfilled
        
        foreach (var duty in unfilledDuties)
        {
            var coverageFound = TryAssignCoverage(duty);
            if (!coverageFound)
            {
                _scheduleBehavior?.MarkDutyUnfulfilled(duty.DutyId, duty.Date, "No available personnel");
            }
        }
    }
}
```

---

## API Reference

### Public Methods - LanceLifeSimulationBehavior

#### Member State Management

```csharp
/// <summary>
/// Get the current state of a lance member.
/// </summary>
public LanceMemberState? GetMemberState(string memberId);

/// <summary>
/// Update a member's health state.
/// </summary>
public void SetMemberHealth(string memberId, HealthState health);

/// <summary>
/// Update a member's activity state.
/// </summary>
public void SetMemberActivity(string memberId, ActivityState activity);

/// <summary>
/// Get all currently alive lance members.
/// </summary>
public List<LanceMemberState> GetActiveLanceMembers();

/// <summary>
/// Get members currently in sick bay.
/// </summary>
public List<LanceMemberState> GetInjuredMembers();
```

#### Cover Request System

```csharp
/// <summary>
/// Check if a cover request event should fire.
/// </summary>
public bool ShouldGenerateCoverRequest();

/// <summary>
/// Process player accepting a cover request.
/// </summary>
public void ProcessCoverRequestAccepted(string memberId, string dutyId);

/// <summary>
/// Process player refusing a cover request.
/// </summary>
public void ProcessCoverRequestRefused(string memberId);

/// <summary>
/// Check if player has any active cover duties.
/// </summary>
public bool PlayerHasActiveCoverDuties();
```

#### Injury and Death

```csharp
/// <summary>
/// Trigger an injury event for a lance member.
/// </summary>
public void CauseMemberInjury(string memberId, HealthState severity, string source);

/// <summary>
/// Process member death.
/// </summary>
public void ProcessMemberDeath(string memberId, string cause);

/// <summary>
/// Get estimated recovery time for an injured member.
/// </summary>
public int GetEstimatedRecoveryDays(string memberId);

/// <summary>
/// Process one day of recovery for all injured members.
/// </summary>
public void ProcessRecoveryProgress();
```

#### Player Promotion

```csharp
/// <summary>
/// Check if player meets all criteria for Lance Leader.
/// </summary>
public bool IsPlayerReadyForLanceLeader();

/// <summary>
/// Get days since player became ready (for urgency scaling).
/// </summary>
public int GetDaysSinceReadiness();

/// <summary>
/// Check if lance leader position is currently occupied.
/// </summary>
public bool IsLanceLeaderPositionOccupied();

/// <summary>
/// Get the selected escalation path (if any).
/// </summary>
public EscalationPath? GetCurrentEscalationPath();

/// <summary>
/// Force immediate escalation (debug/testing).
/// </summary>
public void ForceEscalation(EscalationPath path);
```

#### Relationship Management

```csharp
/// <summary>
/// Get relationship value between player and lance member.
/// </summary>
public int GetMemberRelationship(string memberId);

/// <summary>
/// Modify relationship value (clamped 0-100).
/// </summary>
public void ModifyMemberRelationship(string memberId, int delta);

/// <summary>
/// Get relationship indicator for UI display.
/// </summary>
public string GetRelationshipIndicator(int relationship);
// Returns: "[+++]", "[++]", "[+]", "[ ]", "[-]", "[--]"
```

### Configuration Properties

**File**: `ModuleData/Enlisted/enlisted_config.json`

```json
{
  "lance_life_simulation": {
    "enabled": true,
    "injury_system": {
      "enabled": true,
      "base_daily_risk": 0.001,
      "training_injury_chance": 0.05,
      "combat_injury_chance": 0.15,
      "recovery_minor_days": [3, 7],
      "recovery_major_days": [14, 42]
    },
    "death_system": {
      "enabled": true,
      "base_daily_risk": 0.0001,
      "combat_death_chance": 0.02,
      "allow_narrative_deaths": true,
      "memorial_delay_hours": 48
    },
    "cover_request_system": {
      "enabled": true,
      "requests_per_week": [2, 4],
      "player_target_chance": 0.40,
      "relationship_impact_accept": 10,
      "relationship_impact_refuse": -5,
      "fatigue_cost": 2
    },
    "promotion_escalation": {
      "enabled": true,
      "min_delay_weeks": 1,
      "max_delay_weeks": 12,
      "urgency_increase_per_week": 0.10,
      "path_weights": {
        "promotion": 0.40,
        "transfer": 0.30,
        "injury": 0.15,
        "death": 0.10,
        "retirement": 0.05
      },
      "foreshadow_events": true,
      "foreshadow_weeks_before": 2
    },
    "readiness_criteria": {
      "min_tier": 4,
      "min_days_served": 90,
      "min_lance_reputation": 5,
      "max_discipline": 4,
      "min_leadership_skill": 50,
      "alt_combat_skill_threshold": 75
    }
  }
}
```

---

## Future Expansion Possibilities

### Advanced Features (Post-Launch)

1. **Lance Dynamics**: Rivalries, friendships, factions within lance
2. **Family System**: Lance members receive letters, family visits
3. **PTSD/Mental Health**: Combat trauma affects lance members
4. **Specializations**: Members develop unique skills over time
5. **Lance Reputation**: Your lance gains reputation in broader army
6. **Multi-Lance Interactions**: Events involving multiple lances
7. **Player Lance Leadership**: Full control over lance duties as leader
8. **Lance Customization**: Player shapes lance culture and priorities

### Narrative Expansions

1. **Lance Origin Stories**: Each member has detailed background
2. **Campaign-Long Arcs**: Lance members' stories span entire game
3. **Divergent Paths**: Lance members can become rivals, allies, or neutral
4. **Legacy System**: Dead lance members remembered, memorials visited
5. **Reunion Events**: Former lance members return in new roles

---

## Success Metrics

### Player Experience Goals

- **Emotional Investment**: Players care about lance members
- **Meaningful Choices**: Cover request decisions matter
- **Natural Progression**: Promotion to Lance Leader feels earned
- **Realistic Consequences**: Injuries/deaths create appropriate tension
- **Integrated Gameplay**: System enhances rather than interrupts flow

### Technical Goals

- **Performance**: Minimal impact on game performance
- **Balance**: Event frequencies feel right, not too common or rare
- **Integration**: Seamless coordination with AI Camp Schedule
- **Scalability**: System handles multiple lances if needed
- **Stability**: No breaks in schedule logic or state management

---

## Conclusion

The Lance Life Simulation system transforms the player's lance from a static roster into a living, breathing group of individuals. By combining:

- **Dynamic state tracking** (health, activity, career)
- **Player interaction** (cover requests, choices)
- **Realistic consequences** (injury, death)
- **Smart escalation** (Lance Leader vacancy creation)
- **AI Camp Schedule integration** (seamless duty management)

...we create a rich, emergent gameplay layer that enhances immersion, creates narrative opportunities, and makes the player's military career feel authentic and consequential.

The system respects player agency while maintaining realism, creates tension without frustration, and opens paths for organic storytelling that goes beyond scripted events.

---

## Related Documentation

### Core Systems
- **[Lance Assignments](lance-assignments.md)** - 9-tier progression system, culture-specific ranks, lance roster structure
- **[Lance Life Events](lance-life-events.md)** - Event system infrastructure, JSON format, delivery channels
- **[Escalation System](../../research/escalation_system.md)** - Heat, Discipline, Lance Rep, Medical Risk tracks
- **[Pay System](pay-system.md)** - Muster ledger, pay tension, morale effects
- **[Duties System](duties-system.md)** - Formation-based roles, skill bonuses, request system

### Integration Points
- **[AI Camp Schedule](ai-camp-schedule.md)** - Daily scheduling system that runs in parallel
- **[Menu Interface](../UI/menu-interface.md)** - `enlisted_lance` menu for roster display
- **[Camp Life Simulation](../Gameplay/camp-life-simulation.md)** - Condition-driven logistics affecting welfare

### Implementation Guides
- **[Event Delivery Guide](../../research/event_delivery_guide.md)** - How events are queued and displayed
- **[Lance Career System](../../research/lance_career_system.md)** - Original design notes for progression

---

## Implementation Checklist

### Phase 1: Foundation (Weeks 1-2)
- [ ] Create `LanceLifeSimulationBehavior.cs` with state tracking
- [ ] Implement `LanceMemberState` struct with save/load
- [ ] Add registration to `SubModule.cs`
- [ ] Create config section in `enlisted_config.json`
- [ ] Verify save/load works without errors

### Phase 2: Injury System (Weeks 3-4)
- [ ] Implement daily injury checks with probability modifiers
- [ ] Create injury event JSONs (`lance_member_training_injury`, etc.)
- [ ] Add recovery progression system
- [ ] Integrate with Medical Risk escalation track
- [ ] Add injured member display to `enlisted_lance` menu

### Phase 3: Death and Memorial (Weeks 5-6)
- [ ] Implement death mechanics with various causes
- [ ] Create death event JSONs with proper notification
- [ ] Add memorial service events (follow-up system)
- [ ] Update roster to show "fallen" members
- [ ] Add welfare option "Honor the Fallen"

### Phase 4: Cover Request System (Weeks 7-8)
- [ ] Implement cover request evaluation logic
- [ ] Create cover request event JSONs
- [ ] Integrate with AI Camp Schedule for duty assignment
- [ ] Add relationship impact tracking
- [ ] Add fatigue cost to player when covering

### Phase 5: Promotion Escalation (Weeks 9-11)
- [ ] Implement player readiness tracking
- [ ] Create escalation path selection algorithm
- [ ] Build foreshadowing event system
- [ ] Create all 5 escalation path event chains
- [ ] Add "Ready for Lance Leader" indicator to menus

### Phase 6: Polish and Balance (Week 12)
- [ ] Tune all probability values
- [ ] Balance relationship impacts
- [ ] Test full escalation flow (readiness → vacancy → promotion)
- [ ] Add debug commands for testing
- [ ] Comprehensive playtesting (30+ in-game days)

---

## Debugging and Testing

### Debug Commands

```csharp
// Force player readiness for Lance Leader
campaign.enlisted.debug.force_lance_leader_ready

// Trigger specific escalation path
campaign.enlisted.debug.force_escalation [promotion|transfer|injury|death|retirement]

// Injure a lance member
campaign.enlisted.debug.injure_member [member_id] [minor|major|incapacitated]

// Kill a lance member (testing)
campaign.enlisted.debug.kill_member [member_id]

// Reset all lance member states
campaign.enlisted.debug.reset_lance_simulation
```

### Log Categories

- `"LanceLifeSim"` - Core simulation events
- `"LanceSchedule"` - AI Schedule integration
- `"LanceEvents"` - Event generation and queueing

### Testing Scenarios

1. **Injury Recovery Flow**:
   - Start game, enlist
   - Force injury on lance member
   - Verify removed from AI Schedule
   - Wait for recovery
   - Verify returns to duty

2. **Cover Request Accept**:
   - Generate cover request event
   - Accept as player
   - Verify duty added to player schedule
   - Verify relationship increase
   - Verify fatigue cost applied

3. **Death and Memorial**:
   - Force death on lance member
   - Verify roster updated
   - Verify memorial event fires 48h later
   - Verify morale/reputation impact

4. **Promotion Escalation (Full Flow)**:
   - Reach T4, 90 days served, Lance Rep 5+
   - Verify readiness indicator appears
   - Wait for escalation to select
   - Verify foreshadowing events
   - Verify final escalation event
   - Verify player offered Lance Leader

---

**Document Version**: 1.0  
**Last Updated**: December 13, 2025  
**Status**: Design Complete - Ready for Implementation  
**Maintained by**: Enlisted Development Team

**Next Steps**: 
1. Review with design team
2. Prototype escalation mechanics
3. Test integration with AI Camp Schedule
4. Create initial event content
5. Iterate based on playtesting feedback
