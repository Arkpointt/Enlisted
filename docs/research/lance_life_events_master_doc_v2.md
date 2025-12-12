# Lance Life Events — Master Documentation (v2)

This document is the **single source of truth** for brainstorming, designing, and implementing camp events in the Enlisted mod. It covers triggers, skills, event structure, and all the hooks needed to make events feel grounded in what's actually happening on the campaign map.

**Key Change in v2:** All skill XP is now **action-based**. No passive daily XP. Players gain skills by doing things — drills, duties, events, combat.

---

## Table of Contents

1. [Design Philosophy](#design-philosophy)
2. [Skill Growth Model](#skill-growth-model)
   - [The Three Pillars of XP](#the-three-pillars-of-xp)
   - [Duty System Integration](#duty-system-integration)
   - [Formation Training Events](#formation-training-events)
3. [Trigger System](#trigger-system)
   - [Campaign State Triggers](#campaign-state-triggers)
   - [Camp Condition Triggers](#camp-condition-triggers)
   - [Player State Triggers](#player-state-triggers)
   - [Duty-Based Triggers](#duty-based-triggers)
4. [Skill System](#skill-system)
   - [Naval Skills (War Sails)](#naval-skills-war-sails)
   - [Vanilla Skills by Attribute](#vanilla-skills-by-attribute)
   - [Skill-to-Duty Mapping](#skill-to-duty-mapping)
   - [Skill-to-Event Mapping](#skill-to-event-mapping)
5. [Event Structure](#event-structure)
6. [Escalation System](#escalation-system)
7. [Event Categories](#event-categories)
8. [Sample Events](#sample-events)
9. [Implementation Hooks](#implementation-hooks)
10. [External AI Brainstorm Prompt](#external-ai-brainstorm-prompt)

---

## Design Philosophy

### What We're Building
Viking Conquest / Brytenwalda style camp events: short popup scenarios with 2–4 choices that have mechanical consequences. The "story" is just flavor text wrapping a choice.

### Core Principles

1. **No passive XP** — Skills grow from actions, not time. Every point of XP should trace to something the player did.

2. **Grounded in campaign reality** — Events fire because of what's actually happening (siege, battle, long march), not randomly.

3. **Duties create opportunities** — Your assigned duty determines what events you see and what skills you can train.

4. **Risk spectrum** — Each event offers at least one safe option and one risky option. Corrupt options are optional but tempting.

5. **Small stakes, real consequences** — Camp-scale problems, not kingdom politics. But repeated choices accumulate into reputation and consequences.

### The Core Loop

```
DUTY ASSIGNMENT → DUTY-SPECIFIC EVENTS → PLAYER CHOICES → XP + CONSEQUENCES
       ↑                                                           │
       └───────────────────────────────────────────────────────────┘
                        (Better duties unlock with reputation)
```

---

## Skill Growth Model

### The Three Pillars of XP

All skill XP comes from one of three sources:

#### 1. Combat (Vanilla + Enhanced)
- Using weapons in battle grants XP (vanilla behavior)
- Post-battle events can grant bonus XP based on performance
- Casualties/victories affect available medical/leadership events

#### 2. Duty Performance
- Each duty has associated **duty events** that fire based on triggers
- Completing duty events grants XP in duty-relevant skills
- Higher-stakes duty choices grant more XP but carry risk
- **Duty events replace passive formation training**

#### 3. Camp Events (Lance Life)
- Optional events based on campaign state
- Player chooses whether and how to engage
- Mix of training, logistics, morale, corruption events

### Duty System Integration

The duty system is now the **primary driver** of non-combat skill growth:

#### How It Works

1. **Player selects duties** (limited by tier: 1 slot → 2 → 3)
2. **Duties unlock duty-specific events** that fire based on triggers
3. **Completing duty events grants XP** in relevant skills
4. **Better performance** (risky choices that succeed) grants more XP
5. **Duty reputation** tracks how well you're doing your job

#### Duty → Event Flow

```
Player has Quartermaster duty
    ↓
Army enters town (trigger: entered_town)
    ↓
Quartermaster duty event fires: "Supply Inventory"
    ↓
Player chooses how to handle:
  - Thorough count (+2 Fatigue, +30 Steward XP, +20 Trade XP)
  - Quick count (+1 Fatigue, +15 Steward XP)
  - Delegate to assistant (no fatigue, +15 Leadership XP, risk of errors)
    ↓
XP granted based on choice
```

#### Duty Event Frequency

Each duty has a **minimum event frequency** to ensure active players can progress:

| Duty Type | Events Per Week (Target) | Primary Triggers |
|-----------|--------------------------|------------------|
| Quartermaster | 2–3 | `entered_town`, `siege_ongoing`, `logistics_high` |
| Scout | 2–3 | `marching`, `entered_hostile_territory`, `before_battle` |
| Field Medic | 2–3 | `battle_ended`, `battle_heavy_casualties`, `siege_ongoing` |
| Messenger | 2–3 | `in_army`, `before_battle`, `entered_town` |
| Armorer | 2–3 | `battle_ended`, `siege_ongoing`, `entered_town` |
| Lookout | 2–3 | `at_sea`, `in_hostile_waters`, `night_cycle` |
| etc. | 2–3 | Duty-specific |

### Formation Training Events

**Old system (removed):** Passive daily XP based on formation.

**New system:** Formation determines which **training events** are available, but XP only comes from participating.

#### Training Event Types by Formation

| Formation | Training Events Available |
|-----------|---------------------------|
| **Infantry** | Shield wall drill, formation practice, sparring (sword/polearm), wrestling, march conditioning |
| **Cavalry** | Horse rotation, mounted drill, lance practice, charge formations |
| **Archer** | Target practice, volley drill, bow maintenance, hunting party |
| **Horse Archer** | Mounted archery drill, skirmish practice, horse care |
| **Naval** | Boarding drill, deck combat, rigging practice |

#### Training Event Flow

```
Player is Infantry formation
    ↓
Daily tick + no_combat_extended (3+ days)
    ↓
Training event available: "Formation Drill"
    ↓
Player can engage via:
  - Game Menu option (wait menu): "Join the drill"
  - Or skip it (no XP, but no fatigue cost either)
    ↓
If player engages:
  - Choose intensity (safe/risky options)
  - Gain XP based on choice
  - Pay fatigue cost
```

---

## Trigger System

Events fire based on **observable campaign state** — things we can reliably detect.

### Campaign State Triggers

#### Siege Operations

| Trigger ID | Detection Method | Duty Events | General Events |
|------------|------------------|-------------|----------------|
| `siege_started` | `OnSiegeEventStartedEvent` | Engineer: fortification work | Trench assignment |
| `siege_ongoing` | `BesiegedSettlement != null` | Quartermaster: supply rationing | Siege duties |
| `siege_ended_victory` | `OnSiegeEventEndedEvent` + win | Medic: mass casualty triage | Loot discipline |
| `siege_ended_defeat` | `OnSiegeEventEndedEvent` + loss | — | Retreat events |

#### Battle Operations

| Trigger ID | Detection Method | Duty Events | General Events |
|------------|------------------|-------------|----------------|
| `battle_won` | `MapEventEnded` + victory | Scout: pursuit recommendations | Victory celebration |
| `battle_lost` | `MapEventEnded` + defeat | — | Morale crisis |
| `battle_heavy_casualties` | `MapEventEnded` + casualty % | Medic: triage choices | Burial detail |
| `before_battle` | Enemy party nearby + engagement likely | Scout: terrain report | Pre-battle nerves |
| `no_combat_extended` | Days since battle > 3 | — | Training events available |

#### Settlement Operations

| Trigger ID | Detection Method | Duty Events | General Events |
|------------|------------------|-------------|----------------|
| `entered_town` | `SettlementEntered` (town) | Quartermaster: resupply | Pay muster, tavern |
| `entered_castle` | `SettlementEntered` (castle) | Messenger: dispatch duty | Garrison events |
| `entered_village` | `SettlementEntered` (village) | Scout: local intel | Foraging |
| `days_from_settlement` > X | Days since entry | Quartermaster: rationing | Supply anxiety |

#### Raiding Operations

| Trigger ID | Detection Method | Duty Events | General Events |
|------------|------------------|-------------|----------------|
| `raid_started` | `VillageBeingRaided` | Scout: perimeter watch | — |
| `raid_completed` | `VillageLooted` | Quartermaster: inventory haul | Loot discipline |

#### Army State

| Trigger ID | Detection Method | Duty Events | General Events |
|------------|------------------|-------------|----------------|
| `in_army` | `Army != null` | Messenger: inter-party communication | Rivalry events |
| `army_formed` | `ArmyCreated` | — | New army chaos |
| `army_dispersed` | `ArmyDispersed` | — | Farewell events |

#### Naval Operations (War Sails)

| Trigger ID | Detection Method | Duty Events | General Events |
|------------|------------------|-------------|----------------|
| `at_sea` | Naval travel state | Lookout: watch duty | Deck maintenance |
| `long_voyage` | Days at sea > X | Quartermaster: ration check | Scurvy, boredom |
| `naval_battle` | Naval `MapEventEnded` | Medic: shipboard triage | Boarding aftermath |
| `bad_weather` | Weather/sea state | — | Rigging emergency |
| `in_hostile_waters` | Near enemy territory | Scout/Lookout: threat watch | Tension events |

### Camp Condition Triggers

From the Camp Life Simulation system:

| Trigger ID | Condition | Duty Relevance |
|------------|-----------|----------------|
| `logistics_high` | `LogisticsStrain > 60` | Quartermaster events more frequent |
| `morale_low` | `MoraleShock > 60` | Leadership/Charm events |
| `pay_tension_high` | `PayTension > 50` | Corruption temptations |
| `heat_high` | `ContrabandHeat > 50` | Shakedown risk |

### Player State Triggers

| Trigger ID | Condition | Effect |
|------------|-----------|--------|
| `tier_1` through `tier_6` | Enlisted tier | Gates event availability |
| `provisional_lance` | Lance not finalized | Limited events |
| `final_lance` | Lance finalized | Full event access |
| `fatigue_high` | Fatigue > threshold | Some events unavailable |
| `has_duty_X` | Player has duty X | Duty events available |

### Duty-Based Triggers

These triggers only fire for players with specific duties:

| Trigger ID | Required Duty | When It Fires |
|------------|---------------|---------------|
| `quartermaster_resupply` | Quartermaster | `entered_town` + supplies needed |
| `quartermaster_ration` | Quartermaster | `logistics_high` or `days_from_settlement > 5` |
| `scout_terrain` | Scout | `before_battle` |
| `scout_recon` | Scout | `entered_hostile_territory` |
| `medic_triage` | Field Medic | `battle_heavy_casualties` |
| `medic_routine` | Field Medic | `waiting_in_settlement` (daily) |
| `messenger_dispatch` | Messenger | `in_army` + command communication |
| `armorer_repair` | Armorer | `battle_ended` |
| `lookout_watch` | Lookout | `at_sea` (regular intervals) |

---

## Skill System

### Naval Skills (War Sails)

| Skill | Governed By | Primary Duties | Event Types |
|-------|-------------|----------------|-------------|
| **Mariner** | Endurance & Cunning | Marine, Boarding Specialist | Boarding drills, deck combat |
| **Boatswain** | Control & Social | Boatswain, Deckhand | Rigging, crew management |
| **Shipmaster** | Vigor & Intelligence | Navigator, Helmsman | Navigation, fleet command |

### Vanilla Skills by Attribute

#### VIGOR (Melee)

| Skill | Primary Duties | Training Events | Combat Source |
|-------|----------------|-----------------|---------------|
| **OneHanded** | Guard, Infantry | Sparring, guard duty | Sword/mace combat |
| **TwoHanded** | Shock Trooper | Heavy weapon drill | Greatsword combat |
| **Polearm** | Infantry, Cavalry | Formation drill, lance practice | Spear/lance combat |

#### CONTROL (Ranged)

| Skill | Primary Duties | Training Events | Combat Source |
|-------|----------------|-----------------|---------------|
| **Bow** | Scout, Archer | Target practice, hunting | Bow combat |
| **Crossbow** | Marksman | Maintenance, wall defense | Crossbow combat |
| **Throwing** | Skirmisher | Javelin drill, hunting | Throwing combat |

#### ENDURANCE (Physical)

| Skill | Primary Duties | Training Events | Combat Source |
|-------|----------------|-----------------|---------------|
| **Riding** | Messenger, Cavalry | Horse rotation, mounted drill | Mounted combat |
| **Athletics** | Runner, Infantry | March conditioning, climbing | Foot combat/movement |
| **Smithing** | Armorer | Forge work, repairs | — |

#### CUNNING (Tactical)

| Skill | Primary Duties | Training Events | Combat Source |
|-------|----------------|-----------------|---------------|
| **Scouting** | Scout, Pathfinder | Recon missions, night watch | — |
| **Tactics** | — | Battle planning (officer) | — |
| **Roguery** | — | Theft events (not a duty) | — |

#### SOCIAL (Influence)

| Skill | Primary Duties | Training Events | Combat Source |
|-------|----------------|-----------------|---------------|
| **Charm** | — | Morale events, socializing | — |
| **Leadership** | All officer roles | Rally events, discipline | — |
| **Trade** | Quartermaster | Merchant dealings, haggling | — |

#### INTELLIGENCE (Knowledge)

| Skill | Primary Duties | Training Events | Combat Source |
|-------|----------------|-----------------|---------------|
| **Steward** | Quartermaster | Supply management, rationing | — |
| **Medicine** | Field Medic | Triage, wound care, herbs | — |
| **Engineering** | Engineer | Siege work, construction | — |

### Skill-to-Duty Mapping

Which duties train which skills:

| Duty | Primary Skills | Secondary Skills |
|------|----------------|------------------|
| **Quartermaster** | Steward, Trade | Leadership |
| **Scout** | Scouting, Athletics | Bow (hunting) |
| **Field Medic** | Medicine | Steward (supplies) |
| **Messenger** | Riding, Athletics | Charm |
| **Armorer** | Smithing, Engineering | — |
| **Runner** | Athletics | Scouting |
| **Lookout** | Scouting | Shipmaster (naval) |
| **Engineer** | Engineering | Athletics |
| **Boatswain** | Boatswain | Leadership |
| **Navigator** | Shipmaster, Scouting | — |

### Skill-to-Event Mapping

Quick reference for ensuring skill coverage in events:

| Skill | Duty Events | Training Events | General Events |
|-------|-------------|-----------------|----------------|
| OneHanded | Guard duty combat | Sparring circle | Challenges |
| TwoHanded | — | Heavy weapon drill | Challenges |
| Polearm | — | Formation drill | Shield wall events |
| Bow | Scout hunting | Target practice | Hunting party |
| Crossbow | — | Wall defense prep | — |
| Throwing | — | Javelin drill | Hunting |
| Riding | Messenger dispatch | Horse rotation | — |
| Athletics | Runner errands | March conditioning | Labor events |
| Smithing | Armorer repairs | Forge assistance | — |
| Scouting | Scout recon | Night watch | Foraging |
| Tactics | — | (Officer only) | Planning discussions |
| Roguery | — | — | Theft/contraband |
| Charm | — | — | Social events |
| Leadership | All duty supervision | — | Rally events |
| Trade | Quartermaster deals | — | Merchant events |
| Steward | Quartermaster inventory | — | Rationing events |
| Medicine | Medic triage | — | Wounded care |
| Engineering | Engineer construction | — | Siege labor |
| Mariner | — | Boarding drill | Deck combat |
| Boatswain | Boatswain crew mgmt | Rigging practice | Deck duty |
| Shipmaster | Navigator plotting | — | Helm events |

---

## Event Structure

### Event Template

```
## Event: [Title] (2-4 words)

**Type:** Duty Event / Training Event / General Event

**Duty Required:** [Duty name] or "None"

**Setup** (2-4 sentences)

**Tier gate:** Tier X+ (provisional OK / final lance only)

**Trigger:** [Primary trigger] + [Secondary if needed]

**Options**

| Option | Risk | Cost | Reward | Skills |
|--------|------|------|--------|--------|
| **[Option text]** | Safe/Risky/Corrupt | [Costs] | [Rewards] | [Skills] |

**Escalation hook:** [1 line — consequences of patterns]
```

### Event Types

#### Duty Events
- **Require** the player to have a specific duty
- Fire based on duty-specific triggers
- Primary source of duty-related skill XP
- Higher frequency (2–3 per week per duty)

#### Training Events
- Available based on **formation** (Infantry, Cavalry, Archer, etc.)
- Fire during downtime (`no_combat_extended`, `waiting_in_settlement`)
- Player must **choose to engage** (via game menu)
- Primary source of combat skill XP outside of battle

#### General Events
- Available to all enlisted players
- Fire based on campaign state
- Mix of categories (morale, corruption, logistics, etc.)
- Lower frequency than duty events

### Option Design

| Risk Level | Description | XP Multiplier |
|------------|-------------|---------------|
| **Safe** | No risk, predictable outcome | 1.0x |
| **Risky** | Chance of failure or extra cost | 1.5x (success) / 0.5x (failure) |
| **Corrupt** | Guaranteed heat, best reward | 1.5x + gold/items |

### Reward/Cost Guidelines

#### XP Amounts (Action-Based)

| Action Type | XP Range | Example |
|-------------|----------|---------|
| Quick task | 10–20 | Observation, minor help |
| Standard duty event | 25–40 | Inventory count, basic triage |
| Challenging duty event | 40–60 | Complex negotiation, mass triage |
| Training drill (safe) | 15–25 | Basic drill participation |
| Training drill (risky) | 30–50 | Push yourself, take risks |
| Exceptional performance | 50–75 | Risky choice with great success |

#### Fatigue Costs

| Activity | Fatigue |
|----------|---------|
| Light task | +1 |
| Standard duty | +2 |
| Heavy labor/intense drill | +3–4 |
| Exhausting work | +5 |

#### Gold

| Type | Amount |
|------|--------|
| Small bribe/tip | 15–30 |
| Standard payment | 40–80 |
| Significant deal | 100–200 |

---

## Escalation System

### Heat (Contraband/Corruption)
Builds from: smuggling, theft, bribes, corrupt deals, looking the other way

| Level | Threshold | Consequence |
|-------|-----------|-------------|
| Low | 1–3 | Quartermaster watches you |
| Medium | 4–6 | Random shakedown events |
| High | 7+ | Confiscation, discipline, discharge risk |

### Discipline Risk
Builds from: shirking duty, sleeping on watch, insubordination, failed duty events

| Level | Threshold | Consequence |
|-------|-----------|-------------|
| Low | 1–3 | Worse duty assignments |
| Medium | 4–6 | Extra duty fatigue penalties |
| High | 7+ | Formal punishment, duty removed, promotion blocked |

### Duty Reputation
Tracks how well you perform your assigned duty:

| Level | Effect |
|-------|--------|
| Poor | Duty may be reassigned, fewer choices in duty events |
| Standard | Normal duty event options |
| Good | Bonus XP from duty events, better choices available |
| Excellent | Can request specific duties, officer consideration |

---

## Event Categories

### 1. Duty Events

Events tied to specific duty assignments:

#### Quartermaster Duty Events

| Event | Trigger | Skills |
|-------|---------|--------|
| Supply Inventory | `entered_town` | Steward, Trade |
| Ration Planning | `logistics_high` | Steward, Leadership |
| Merchant Negotiation | `entered_town` | Trade, Charm |
| Equipment Distribution | `after_battle` | Steward, Leadership |
| Stockpile Check | `siege_ongoing` | Steward |

#### Scout Duty Events

| Event | Trigger | Skills |
|-------|---------|--------|
| Terrain Reconnaissance | `before_battle` | Scouting, Athletics |
| Enemy Position Report | `enemy_nearby` | Scouting, Tactics |
| Hunting for Camp | `logistics_high` | Scouting, Bow |
| Night Patrol | `in_hostile_territory` | Scouting, Athletics |
| Trail Assessment | `long_march` | Scouting |

#### Field Medic Duty Events

| Event | Trigger | Skills |
|-------|---------|--------|
| Battle Triage | `battle_heavy_casualties` | Medicine, Leadership |
| Wound Treatment | `battle_ended` | Medicine |
| Sick Call | `waiting_in_settlement` | Medicine, Steward |
| Herb Gathering | `entered_village` | Medicine, Scouting |
| Plague Prevention | `long_voyage` or `siege_ongoing` | Medicine, Steward |

#### Messenger Duty Events

| Event | Trigger | Skills |
|-------|---------|--------|
| Dispatch Run | `in_army` | Riding, Athletics |
| Command Relay | `before_battle` | Riding, Leadership |
| Town Courier | `entered_town` | Riding, Charm |
| Emergency Message | `battle_ongoing` | Riding, Athletics |

#### Armorer Duty Events

| Event | Trigger | Skills |
|-------|---------|--------|
| Post-Battle Repairs | `battle_ended` | Smithing, Engineering |
| Equipment Inspection | `entered_town` | Smithing, Steward |
| Field Repairs | `siege_ongoing` | Smithing, Athletics |
| Weapon Maintenance | `no_combat_extended` | Smithing |

### 2. Training Events

Formation-based training (player must opt in):

#### Infantry Training

| Event | Skills | Fatigue |
|-------|--------|---------|
| Shield Wall Drill | Polearm, OneHanded | +2–3 |
| Formation March | Athletics, Polearm | +2–3 |
| Sparring Circle | OneHanded or TwoHanded | +2 |
| Wrestling Practice | Athletics | +2 |

#### Cavalry Training

| Event | Skills | Fatigue |
|-------|--------|---------|
| Horse Rotation | Riding | +1–2 |
| Mounted Drill | Riding, Polearm | +2–3 |
| Charge Practice | Riding, OneHanded | +2–3 |
| Horse Care | Riding | +1 |

#### Archer Training

| Event | Skills | Fatigue |
|-------|--------|---------|
| Target Practice | Bow or Crossbow | +1–2 |
| Volley Drill | Bow, Tactics | +2 |
| Hunting Party | Bow, Scouting | +2–3 |
| Bow Maintenance | Bow, Engineering | +1 |

#### Naval Training

| Event | Skills | Fatigue |
|-------|--------|---------|
| Boarding Drill | Mariner, Athletics | +2–3 |
| Rigging Practice | Boatswain, Athletics | +2–3 |
| Navigation Lesson | Shipmaster, Scouting | +1–2 |
| Deck Combat | Mariner, OneHanded | +2–3 |

### 3. General Events

Available regardless of duty:

| Category | Example Events |
|----------|----------------|
| Morale | After the Slaughter, Victory Toast, Camp Grumbling |
| Corruption | Quartermaster's Offer, Ledger Game, Unguarded Goods |
| Discipline | Night Watch (non-scout), Cover for Lance Mate, Rivalry |
| Logistics | Empty Wagons (non-QM), Merchant's Shortcut |
| Medical | Help the Surgeon (non-medic), Wounded Lance Mate |

---

## Sample Events

### Duty Event: Supply Inventory (Quartermaster)

**Type:** Duty Event

**Duty Required:** Quartermaster

**Setup**
The army's entered town. Time to count what we've got and what we need. The supply sergeant hands you the ledger. "Get it right."

**Tier gate:** Tier 2+ (provisional OK for basic version)

**Trigger:** `entered_town` + `has_duty_quartermaster`

**Options**

| Option | Risk | Cost | Reward | Skills |
|--------|------|------|--------|--------|
| **Count everything twice** | Safe | +3 Fatigue | +35 Steward XP, +20 Trade XP, accurate count | Steward, Trade |
| **Standard inventory** | Safe | +2 Fatigue | +25 Steward XP, +15 Trade XP | Steward, Trade |
| **Delegate to assistants** | Risky | +1 Fatigue | +20 Leadership XP; 30% chance of errors (reputation hit) | Leadership |
| **Skim a little off the top** | Corrupt | +2 Heat | +25 Steward XP, +30 gold | Steward |

**Escalation hook:** Repeated skimming builds heat; errors from delegation hurt duty reputation.

---

### Duty Event: Terrain Reconnaissance (Scout)

**Type:** Duty Event

**Duty Required:** Scout

**Setup**
Battle's coming. The captain needs to know what's ahead — hills, treelines, places to anchor a flank. You're sent out with orders to report back fast.

**Tier gate:** Tier 3+ (final lance)

**Trigger:** `before_battle` + `has_duty_scout`

**Options**

| Option | Risk | Cost | Reward | Skills |
|--------|------|------|--------|--------|
| **Thorough survey, take your time** | Safe | +3 Fatigue | +40 Scouting XP, +20 Tactics XP, detailed report | Scouting, Tactics |
| **Quick sweep, hit the obvious points** | Safe | +1 Fatigue | +20 Scouting XP, basic report | Scouting |
| **Push deep into risky ground** | Risky | +2 Fatigue, 20% ambush chance | +50 Scouting XP, +25 Tactics XP, excellent intel | Scouting, Tactics |
| **Report what you assume without checking** | Corrupt | +0 Fatigue, +2 Discipline | +10 Scouting XP, bad intel (battle penalty) | Scouting |

**Escalation hook:** Bad intel repeatedly will get you removed from scout duty and discipline consequences.

---

### Duty Event: Battle Triage (Field Medic)

**Type:** Duty Event

**Duty Required:** Field Medic

**Setup**
The fighting's done but the dying isn't. Wounded everywhere — more than you can handle. You have to choose who gets attention first.

**Tier gate:** Tier 3+ (final lance)

**Trigger:** `battle_heavy_casualties` + `has_duty_field_medic`

**Options**

| Option | Risk | Cost | Reward | Skills |
|--------|------|------|--------|--------|
| **Triage by survival chance (efficient)** | Safe | +3 Fatigue | +40 Medicine XP, +20 Leadership XP, best outcomes | Medicine, Leadership |
| **Treat everyone equally (fair)** | Safe | +4 Fatigue | +35 Medicine XP, +15 Charm XP, morale boost | Medicine, Charm |
| **Focus on your lance mates first** | Risky | +2 Fatigue | +25 Medicine XP, lance loyalty; others may die | Medicine |
| **Prioritize those who can pay** | Corrupt | +2 Heat | +30 Medicine XP, +40 gold, reputation damage | Medicine |

**Escalation hook:** Corrupt triage will eventually result in confrontation from families of those who died.

---

### Training Event: Shield Wall Drill (Infantry)

**Type:** Training Event

**Duty Required:** None (Infantry formation)

**Setup**
The sergeant's calling for formation practice. Shields up, spears out, hold the line. You can join or find something else to do.

**Tier gate:** Tier 2+ (provisional OK)

**Trigger:** `no_combat_extended` + `formation_infantry` + `waiting_in_settlement`

**UI System:** Game Menu (wait menu option)

**Options**

| Option | Risk | Cost | Reward | Skills |
|--------|------|------|--------|--------|
| **Drill hard, front rank** | Safe | +3 Fatigue | +30 Polearm XP, +25 OneHanded XP | Polearm, OneHanded |
| **Standard participation** | Safe | +2 Fatigue | +20 Polearm XP, +15 OneHanded XP | Polearm, OneHanded |
| **Push for extra rounds** | Risky | +4 Fatigue, 15% minor injury | +45 Polearm XP, +30 OneHanded XP, +20 Athletics XP | Polearm, OneHanded, Athletics |

**Escalation hook:** None — clean training event.

---

### General Event: The Quartermaster's Offer

**Type:** General Event

**Duty Required:** None

**Setup**
You're waiting for your ration chit when the quartermaster's clerk pulls you aside. "Your lance got shorted last muster. Clerical error." He pauses. "I can fix it. Fifty silver, and your back-pay appears on the ledger."

**Tier gate:** Tier 4+ (final lance)

**Trigger:** `pay_tension_high` + `entered_town`

**Options**

| Option | Risk | Cost | Reward | Skills |
|--------|------|------|--------|--------|
| **Pay the fifty — get what's owed** | Risky | −50 gold | Recover ~80 gold, +20 Trade XP | Trade |
| **Refuse, report him** | Safe | Clerk hostile | +25 Leadership XP, −1 Heat | Leadership |
| **Refuse, but keep quiet** | Safe | None | None | — |
| **Negotiate a better deal** | Risky | −30 gold, +1 Heat | Recover ~80 gold + 20 extra, +30 Trade XP | Trade |

**Escalation hook:** Working with corrupt clerks flags you — audit events become personal.

---

## Implementation Hooks

### Duty Event Registration

```csharp
// In LanceStoryBehavior or DutyEventBehavior

private void RegisterDutyEvent(DutyEventDefinition def)
{
    // Only available if player has the required duty
    if (!PlayerHasDuty(def.RequiredDuty)) return;
    
    // Check trigger conditions
    if (!MeetsTriggerConditions(def.Triggers)) return;
    
    // Fire the event using appropriate UI
    switch (def.UISystem)
    {
        case "map_incident":
            FireAsMapIncident(def);
            break;
        case "game_menu":
            // Option added to menu elsewhere
            break;
        case "inquiry":
            FireAsInquiry(def);
            break;
    }
}
```

### XP Application (No Passive)

```csharp
// REMOVED: Daily passive XP
// private void ApplyDailyFormationTraining() { ... }

// NEW: XP only from completed events
private void ApplyEventRewards(EventOption selectedOption)
{
    foreach (var skillXp in selectedOption.SkillXp)
    {
        Hero.MainHero.AddSkillXp(skillXp.Skill, skillXp.Amount);
        
        // Log for debugging
        LogSkillGain(skillXp.Skill, skillXp.Amount, "Event: " + currentEvent.Id);
    }
}
```

### Duty Event Frequency Tracking

```csharp
private Dictionary<string, int> _dutyEventsThisWeek = new();

private void OnDutyEventCompleted(string dutyId)
{
    _dutyEventsThisWeek[dutyId] = _dutyEventsThisWeek.GetValueOrDefault(dutyId, 0) + 1;
}

private void OnWeeklyTick()
{
    // Check if any duty is underserved
    foreach (var duty in GetPlayerDuties())
    {
        int eventsThisWeek = _dutyEventsThisWeek.GetValueOrDefault(duty.Id, 0);
        if (eventsThisWeek < duty.MinEventsPerWeek)
        {
            // Force a duty event to fire soon
            QueueDutyEvent(duty.Id);
        }
    }
    
    _dutyEventsThisWeek.Clear();
}
```

### Training Event Availability

```csharp
// Training events appear as game menu options
private bool CanShowTrainingEvent(string trainingId, MenuCallbackArgs args)
{
    var training = GetTrainingDefinition(trainingId);
    
    // Must be in correct formation
    if (training.Formation != GetPlayerFormation()) return false;
    
    // Must be in appropriate context (waiting)
    if (!IsWaitingInSettlement()) return false;
    
    // Must have sufficient stamina
    if (GetFatigue() > training.MaxFatigue) return false;
    
    // Cooldown check
    if (IsOnCooldown(trainingId)) return false;
    
    return true;
}
```

---

## External AI Brainstorm Prompt

Use this prompt when brainstorming new events:

---

```
You are brainstorming **camp events** for a Mount & Blade II: Bannerlord mod called **Enlisted** with the **War Sails** naval expansion. These are "Viking Conquest style" popups: short situation, 2–4 choices, immediate consequences.

### Core Principle: No Passive XP
All skill growth comes from ACTIONS:
- Combat (using weapons)
- Duty events (performing assigned duties)
- Training events (opting into drills)
- General events (camp situations)

### Event Types

**Duty Events** — Require specific duty, fire on duty-related triggers
**Training Events** — Based on formation, player opts in via menu
**General Events** — Available to all, based on campaign state

### Available Duties
- Quartermaster (Steward, Trade)
- Scout (Scouting, Bow, Athletics)
- Field Medic (Medicine, Steward)
- Messenger (Riding, Athletics, Charm)
- Armorer (Smithing, Engineering)
- Runner (Athletics, Scouting)
- Lookout (Scouting, Shipmaster — naval)
- Engineer (Engineering, Athletics)
- Boatswain (Boatswain, Leadership — naval)
- Navigator (Shipmaster, Scouting — naval)

### Available Skills

**Naval (War Sails):** Mariner, Boatswain, Shipmaster
**Vigor:** OneHanded, TwoHanded, Polearm
**Control:** Bow, Crossbow, Throwing
**Endurance:** Riding, Athletics, Smithing
**Cunning:** Scouting, Tactics, Roguery
**Social:** Charm, Leadership, Trade
**Intelligence:** Steward, Medicine, Engineering

### Time of Day
Events should specify when they occur:
- **Dawn** (5-7): Morning muster, sick call, watch handoff
- **Morning/Afternoon** (7-17): Training, duty events, work
- **Evening** (17-20): Meals, winding down, lance meetings
- **Dusk** (20-22): Campfire, stories, socializing
- **Night** (22-2): Night watch, quiet events
- **Late Night** (2-5): Emergencies only

### Personalization — Use Placeholders
Make events feel personal:

**Always available:**
- {PLAYER_NAME} — Player's character name
- {LORD_NAME}, {LORD_TITLE} — Enlisted lord
- {FACTION_NAME} — Lord's faction
- {LANCE_NAME} — Player's lance unit
- {SERGEANT_NAME} — Lance sergeant
- {LANCE_MATE_NAME} — Random lance member

**Combat context only (battle/pursuit triggers):**
- {ENEMY_FACTION}, {ENEMY_FACTION_ADJECTIVE}
- {ENEMY_LORD}

**Siege context only:**
- {BESIEGED_SETTLEMENT}, {SIEGE_DAYS}, {DEFENDER_FACTION}

**Naval context only:**
- {SHIP_NAME}, {CAPTAIN_NAME}, {DAYS_AT_SEA}

**Injury context only:**
- {INJURY_TYPE}, {INJURY_LOCATION}, {RECOVERY_DAYS}

**Rules:**
1. Only use context-specific placeholders when the trigger guarantees they exist
2. Use 1-2 placeholders per paragraph maximum
3. Write naturally — placeholders should fit speech patterns

### Injuries and Illness
Risky options can cause injuries:
- **Training injuries:** 5-20% chance for risky options
- **Duty injuries:** 10-25% for dangerous choices
- **Typical severity:** minor (70%), moderate (25%), severe (5%)

### Triggers
[See full trigger list in master doc]

### Event Template

## Event: [Title]

**Type:** Duty Event / Training Event / General Event

**Duty Required:** [Duty] or "None"

**Time of Day:** [dawn / morning / afternoon / evening / dusk / night]

**Setup** (2-4 sentences, use placeholders naturally)

**Tier gate:** Tier X+

**Trigger:** [triggers]

**Options**

| Option | Risk | Cost | Reward | Skills | Injury |
|--------|------|------|--------|--------|--------|
| **[Text with {PLACEHOLDERS}]** | Safe/Risky/Corrupt | [Costs] | [Rewards] | [Skills] | [chance%, severity] |

**Escalation hook:** [1 line]

### XP Guidelines
- Quick task: 10–20 XP
- Standard duty/training: 25–40 XP
- Challenging/risky: 40–60 XP
- Exceptional: 50–75 XP

### Generate Now
Create [N] events:
- [X] Duty events for [specific duties]
- [X] Training events for [formations]
- [X] General events for [time of day]

Ensure:
- All XP comes from player choices, not passive gain
- Events use appropriate placeholders for personalization
- Risky options include injury chances where realistic
- Time of day is specified and makes sense for the event
```

---

*Document version: 2.0*
*Key change: Removed passive XP, integrated duty system*
*For use with: Enlisted mod, War Sails expansion*
