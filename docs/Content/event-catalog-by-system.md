# Enlisted Content System

**Summary:** Single source of truth for all narrative content including Events (74), Map Incidents (51), Orders (17), and Decisions (34). This comprehensive catalog organizes content by system and role, providing IDs, descriptions, requirements, effects, and implementation status for all content in the mod.

**Status:** ✅ Current  
**Last Updated:** 2025-12-23 (Added: Retinue system - 11 events, 6 incidents, 4 decisions for T7+ commanders)  
**Related Docs:** [Content System Architecture](../Features/Content/content-system-architecture.md), [Event System Schemas](../Features/Content/event-system-schemas.md), [Retinue System](../Features/Core/retinue-system.md)

---

## Content Summary

| Category | Count | Breakdown |
|----------|-------|-----------|
| **Orders** | 17 | 6 T1-T3, 6 T4-T6, 5 T7-T9 |
| **Decisions** | 38 | Player-initiated Camp Hub menu actions (dec_* prefix) |
| **Events** | 80+ | Narrative events across multiple files (camp, general, duty roles, pay, promotion, training, retinue, etc.) |
| **Map Incidents** | 51 | 11 Battle, 10 Siege, 8 Town, 6 Village, 6 Leaving, 4 Waiting, 6 Retinue (T7+) |

**Total**: 186+ content pieces across all systems.

**Note:** Event count includes all event definitions from Events/ directory. Many events have multiple options, and the system loads content recursively from all JSON files. The actual number of player-facing choices is significantly higher when counting all event options.

---

## Index

| Section | What It Covers |
|---------|----------------|
| [The Four Content Types](#the-four-content-types) | Overview of Orders, Decisions, Events, Map Incidents |
| [Systems Affected](#systems-affected) | Reputation, Escalation, Company Needs, Skills |
| [Progression Philosophy](#progression-philosophy) | 3-year target, XP scale, variety |
| [1. Orders](#1-orders) | Military directives by tier |
| [2. Decisions](#2-decisions) | Player-initiated choices with costs, cooldowns, risks |
| [3. Events](#3-events) | Context/state triggered situations |
| [4. Map Incidents](#4-map-incidents) | Battle, siege, settlement triggers |
| [Event Design Principles](#event-design-principles) | Trade-offs, skill-gating, tier shaping |
| [JSON + XML Localization](#json--xml-localization) | Order and Event structure with translation |
| [Delivery Mechanisms](#delivery-mechanisms) | How each content type is displayed |
| [Placeholder Variables](#placeholder-variables) | Text variables for events |
| [File Organization](#file-organization) | JSON folder structure |

---

## The Four Content Types

| Type | Trigger | Player Control | Frequency |
|------|---------|----------------|-----------|
| **Orders** | System-assigned | Accept/Decline | Every 3-5 days (config) |
| **Decisions** | Player-initiated | Full control | Always available |
| **Events** | Context/state thresholds | Respond | 0-1 per day (config) |
| **Map Incidents** | Map actions (battle, settlement) | Respond | On trigger |

**Global pacing limits** (all config-driven via `enlisted_config.json` → `decision_events.pacing`):
- **Event window:** Orders/Events fire every 3-5 days (`event_window_min_days`, `event_window_max_days`)
- **Daily/weekly limits:** Max 2 automatic events per day, 8 per week (`max_per_day`, `max_per_week`)
- **Minimum spacing:** 6 hours between any automatic events (`min_hours_between`)
- **Evaluation hours:** Orders/Events only fire at hours 8, 14, 20 by default (`evaluation_hours`)
- **Map incidents:** Fire immediately when triggered (ignore evaluation hours)
- **Quiet days:** 15% chance of no events on a given day (`quiet_day_chance`)
- **Category cooldowns:** 1 day between narrative and map incident (`per_category_cooldown_days`)

**What bypasses pacing:** Player-initiated decisions (Camp Hub menu) and chain events from previous choices.

---

## Systems Affected

All content can affect these systems:

### Reputation Tracks
| Track | Range | What It Measures | Tension |
|-------|-------|------------------|---------|
| **Lord Rep** | 0-100 | Your value to the lord | Loyalty vs. independence |
| **Officer Rep** | 0-100 | Your competence to officers | Following rules vs. shortcuts |
| **Soldier Rep** | -50 to +50 | Your popularity with troops | Comrades vs. command |

These tracks often **conflict**. What pleases the lord may anger your comrades.

### Quartermaster Relationship (Separate)
| Track | Range | What It Affects |
|-------|-------|-----------------|
| **QM Rep** | -50 to +100 | Equipment pricing, food quality, baggage check outcomes |

The Quartermaster is a specific NPC with a personal relationship (`Hero.GetRelation(quartermaster)`). High QM Rep = 30% discount on equipment, looks away during baggage checks. Low QM Rep = 40% markup, no mercy.

### Escalation Tracks
| Track | Range | What Happens |
|-------|-------|--------------|
| **Scrutiny** | 0-10 | Suspicion of crimes. Threshold events at 2, 4, 6, 8, 10. |
| **Discipline** | 0-10 | Rule-breaking record. Threshold events at 2, 4, 6, 8, 10. |
| **Medical Risk** | 0-5 | Health status. Threshold events at 2, 3, 4, 5. |

### Company Needs (0-100)
| Need | Affects |
|------|---------|
| **Readiness** | Combat effectiveness |
| **Morale** | Men's will to serve |
| **Supplies** | Food, consumables |
| **Rest** | Recovery from fatigue |
| **Equipment** | Gear condition |

Orders affect Company Needs. Low needs trigger warnings and affect gameplay.

### Party Consequences (Direct)

Some events can affect the actual party, not just abstract numbers:

| Effect | API | When Used |
|--------|-----|-----------|
| **Troop Loss** | `MemberRoster.RemoveTroop()` | Ambush, desertion, failed orders, disease |
| **Troop Wounded** | `MemberRoster.AddToCounts(wounded)` | Battle aftermath, accidents, illness |
| **Food Loss** | `ItemRoster.AddToCounts(food, -N)` | Spoilage, theft, rationing failure |
| **Item Loss** | `ItemRoster.AddToCounts(item, -N)` | Theft, breakage, confiscation |
| **Gold Loss** | `Hero.ChangeHeroGold(-N)` | Fines, gambling, theft |

These are rarer, higher-stakes consequences. Use sparingly for dramatic moments.

### Internal Simulation (CampLife)

These meters run behind the scenes and drive Quartermaster mood/pricing:

| Meter | What It Tracks |
|-------|----------------|
| **LogisticsStrain** | Days from town + raids + battle frequency |
| **MoraleShock** | Recent battle impact (spikes, then decays) |
| **PayTension** | Unpaid wages, backpay issues |
| **TerritoryPressure** | Villages looted (placeholder) |

Players don't see these directly. They combine to set `QuartermasterMoodTier` (Fine → Tense → Sour → Predatory).

### Skills & Traits
| Type | How Awarded |
|------|-------------|
| **Skills** (Scouting, Medicine, etc.) | `hero.AddSkillXp(skill, amount)` |
| **Personality Traits** (Mercy, Valor, Honor, Generosity, Calculating) | `TraitLevelingHelper.OnIncidentResolved(trait, xp)` |
| **Role Traits** (ScoutSkills, Surgeon, Commander, etc.) | Determines player Role |

---

## Progression Philosophy

### 3-Year Well-Rounded Target
Players who engage actively for 3 in-game years should emerge with:
- Primary specialty: Skill 80-100
- Secondary skills: Skill 50-70 in 2-3 areas
- Mid-game ready, not overpowered

### XP Scale
| Action Type | Skill XP | Trait XP |
|-------------|----------|----------|
| Safe/passive choice | 5-10 | ±5-10 |
| Active participation | 15-25 | ±15-20 |
| Risk-taking | 25-40 | ±20-30 |
| Exceptional achievement | 50-75 | ±30-50 |

### Spread Through Variety
Every activity touches 2-3 skills. No single-skill grinding.

---

## Skill Checks

All content uses skill checks to modify outcomes. Higher skills = better results.

### Universal Formula

```
Success Chance = Base% + (Relevant Skill / 3)
```

| Skill Level | Modifier | Effective Chance (Base 50%) |
|-------------|----------|----------------------------|
| 0 | +0% | 50% |
| 30 | +10% | 60% |
| 60 | +20% | 70% |
| 90 | +30% | 80% |
| 120 | +40% | 90% |
| 150+ | +50% | 100% (+ bonus outcome) |

### Check Types by Content

| Content Type | Primary Check | What It Affects |
|--------------|---------------|-----------------|
| **Orders** | Relevant skill | Success vs. failure |
| **Decisions** | Relevant skill | Outcome quality, injury avoidance |
| **Events** | Option-specific | Unlocks options, modifies results |
| **Map Incidents** | Situation-based | Available choices, consequence severity |

### Skill Applications

| Skill | When Checked | Example |
|-------|--------------|---------|
| **Athletics** | Physical tasks, endurance, escaping | Guard duty, patrol, fleeing combat |
| **Scouting** | Spotting, tracking, navigation | Scout orders, ambush detection, route finding |
| **Medicine** | Treating wounds, preventing death | Triage events, saving injured troops |
| **Tactics** | Combat planning, reading situations | Lead patrol, battle aftermath |
| **Leadership** | Commanding, inspiring, training | Squad orders, NCO training, morale events |
| **Engineering** | Building, assessing structures | Siege work, fortification inspection |
| **Steward** | Supply management, logistics | Forage orders, supply events |
| **Charm** | Persuasion, negotiation | Talking down conflicts, officer relations |
| **Roguery** | Deception, crime, black market | Shady deals, avoiding scrutiny |
| **Combat Skills** | Fighting, sparring | Sparring decisions, combat events |

### Threshold Effects

Some skills unlock options or change outcomes at thresholds:

| Threshold | Effect |
|-----------|--------|
| **Skill 30+** | Basic options unlocked |
| **Skill 60+** | Good options, reduced risk |
| **Skill 90+** | Best options, significant risk reduction |
| **Skill 120+** | Bonus outcomes possible |

### Example: Sparring Decision

```
Base injury chance: 25%
Athletics 60: -5% injury (20%)
Combat skill 90: -10% injury (10%)
Combat skill 120: Bonus "Impressive Victory" outcome
```

### Example: Scout Route Order

```
Base success: 60%
Scouting 60: +20% (80% success)
Scouting 90: +30% (90% success)
On failure with Scouting 30+: Partial intel, no ambush
On failure with Scouting <30: Ambush risk, troop loss possible
```

---

## 1. ORDERS

Military directives from the chain of command.

### Mechanics
- **Frequency**: Every 3-5 days
- **Issuer**: Sergeant (T1-3), Captain (T4-6), Lord (T7-9)
- **Must respond**: Accept or Decline
- **Decline penalty**: Officer Rep loss, possible Discipline gain

### Order Success
Based on relevant skills + random factor:
```
Success Chance = Base 60% + (Relevant Skill / 3)
```
Skill 60 = 80% success. Skill 120 = 100% success with bonus.

### Order Failure & Decline

| Outcome | What Happens |
|---------|--------------|
| **Success** | Rewards listed per order. XP, Rep, possibly gold or renown. |
| **Failure** | Reduced/no rewards. Officer Rep penalty (-5 to -15). Company Need may suffer. |
| **Decline** | Officer Rep loss (-8 to -15). Discipline +1. Repeated declines stack. |

Failure isn't catastrophic—you tried and it didn't work. Decline is a choice not to serve.

### Orders by Tier

**T1-T3: Basic Soldier**
| Order | Skills Touched | On Success |
|-------|---------------|------------|
| Guard Duty | Athletics, Perception | +Officer Rep, +Readiness |
| Camp Patrol | Athletics, Scouting | +Officer Rep, +Soldier Rep |
| Firewood Collection | Athletics | +Soldier Rep, +Morale |
| Equipment Inspection | Crafting | +Officer Rep, +Equipment |
| Muster Inspection | — | +Officer Rep, +Lord Rep |
| Sentry Post | Athletics, Perception | +Officer Rep, +Readiness |

**T4-T6: Specialist**
| Order | Skill Req | Skills Touched | On Success |
|-------|-----------|---------------|------------|
| Scout Route | Scouting 40 | Scouting, Athletics, Tactics | +Lord Rep, +Officer Rep, +50 denars |
| Treat Wounded | Medicine 40 | Medicine, Athletics | +Officer Rep, +Soldier Rep, +Morale |
| Equipment Repair | Crafting 50 | Crafting, Engineering | +Officer Rep, +Equipment |
| Forage Supplies | Scouting 30 | Scouting, Athletics | +Officer Rep, +Supplies |
| Lead Patrol | Tactics 50 | Tactics, Leadership, Scouting | +Lord Rep, +75 denars |
| Inspect Defenses | Engineering 40 | Engineering, Tactics | +Lord Rep, +Readiness |

**T7-T9: Leadership**
| Order | Skill Req | Skills Touched | On Success |
|-------|-----------|---------------|------------|
| Command Squad | Leadership 80, Tactics 70 | Leadership, Tactics | +All Reps, +150 denars, +5 Renown |
| Strategic Planning | Tactics 100 | Tactics, Leadership | +Lord Rep, +200 denars, +10 Renown |
| Coordinate Supply | Steward 80 | Steward, Leadership | +Lord Rep, +Supplies, +120 denars |
| Interrogate Prisoner | Charm 60 | Charm, Roguery | +Lord Rep, +100 denars |
| Inspect Readiness | Leadership 100 | Leadership, Tactics | +All Reps, +Readiness, +Morale |

---

## 2. DECISIONS

Player-initiated choices from the Camp Hub menu. These have costs, cooldowns, and risks.

**Current Count:** 38 decisions loaded from `ModuleData/Enlisted/Decisions/decisions.json`

### Cost Types

| Cost Type | What It Means |
|-----------|---------------|
| **Time** | Hours pass, miss other opportunities |
| **Rest** | Fatigue drain |
| **Gold** | Denars spent |
| **HP** | Health point loss (injury) |
| **Medical Risk** | Increases illness/complication chance |

### Injury Severity Scale

| Severity | HP Loss | Additional Effect |
|----------|---------|-------------------|
| Scratch | -5 HP | None |
| Bruised | -10 HP | None |
| Hurt | -15 HP | -5 Rest |
| Injured | -20 HP | +1 Medical Risk |
| Wounded | -30 HP | Native wound applied, +2 Medical Risk |
| Serious | -50 HP | Native wound, +3 Medical Risk, bed rest |

---

### Self-Care Decisions

| Decision | Cost | Cooldown | Gate | Outcomes |
|----------|------|----------|------|----------|
| **Rest** | 4 hrs | None | Rest < 80 | +20 Rest |
| **Extended Rest** | 8 hrs | None | Rest < 50 | +40 Rest |
| **Seek Treatment** | 20-50g | 3 days | Medical Risk > 0 | -Medical Risk, +HP |

### Training Decisions

| Decision | Cost | Cooldown | Injury Risk | Outcomes |
|----------|------|----------|-------------|----------|
| **Weapon Drill** | 2 hrs, -15 Rest | None | 5% | +Combat XP. Injury: -5 HP |
| **Sparring Match** | 2 hrs, -20 Rest | 2 days | 25% | Win: +XP, +Rep. Injury: -15 to -35 HP |
| **Endurance Training** | 3 hrs, -25 Rest | None | 10% | +Athletics XP. Injury: -10 HP |
| **Study Tactics** | 3 hrs, -5 Rest | None | 0% | +Tactics XP |
| **Practice Medicine** | 2 hrs, -10 Rest | None | 0% | +Medicine XP (needs wounded in camp) |

### Social Decisions

| Decision | Cost | Cooldown | Injury Risk | Outcomes |
|----------|------|----------|-------------|----------|
| **Join the Men** | 2 hrs, -5 Rest | 1 day | 5% | +Soldier Rep. Brawl: -10 HP, +Scrutiny |
| **Join the Men (drinking)** | 3 hrs, -10 Rest, 5g | 1 day | 15% | +Soldier Rep. Brawl: -15 HP. Sick: +Medical Risk |
| **Seek Officers** | 1 hr | 2 days | 0% | +Officer Rep opportunity |
| **Keep to Yourself** | 1 hr | None | 0% | +5 Rest, no rep change |
| **Write Letter Home** | 1 hr | 7 days | 0% | Flavor, potential callback event |
| **Confront Rival** | 1 hr | 3 days | 40% | Resolve: ±Rep. Fight: -20 HP, +Discipline |

### Economic Decisions

| Decision | Cost | Cooldown | Injury Risk | Outcomes |
|----------|------|----------|-------------|----------|
| **Gamble (Low)** | 2 hrs, 10g stake | 1 day | 5% | Win: +10g. Lose: -10g. Fight: -10 HP, +Scrutiny |
| **Gamble (High)** | 2 hrs, 50g stake | 1 day | 15% | Win: +50g. Lose: -50g. Fight: -20 HP, +Scrutiny |
| **Side Work** | 4 hrs, -15 Rest | 3 days | 10% | +15-30g. Accident: -15 HP, +Medical Risk |
| **Shady Deal** | 2 hrs | 5 days | 20% | +Gold, +Scrutiny. Betrayed: -25 HP, lose gold |
| **Visit Market** | 1 hr | None | 0% | Buy/sell access (in town) |

### Career Decisions

| Decision | Cost | Cooldown | Gate | Outcomes |
|----------|------|----------|------|----------|
| **Request Audience** | Uses favor | 7 days | Lord Rep 30+ | Speak with lord, ±Lord Rep |
| **Volunteer for Duty** | Commits to order | Until complete | No active order | Extra order, +Officer Rep |
| **Request Leave** | -10 Officer Rep | 14 days | 30+ days service | Time off in town |

### Information Decisions

| Decision | Cost | Cooldown | Injury Risk | Outcomes |
|----------|------|----------|-------------|----------|
| **Listen to Rumors** | 1 hr | 2 days | 0% | Learn world state info |
| **Scout Surroundings** | 3 hrs, -10 Rest | 1 day | 15% | +Scouting XP, intel. Ambush: -25 HP |
| **Check Supply Situation** | None | None | 0% | See company needs |

### Equipment Decisions

| Decision | Cost | Cooldown | Gate | Outcomes |
|----------|------|----------|------|----------|
| **Maintain Gear** | 2 hrs, -5 Rest | 3 days | — | +Equipment condition |
| **Visit Quartermaster** | 1 hr, variable gold | None | — | Request/buy gear |

### Risk-Taking Decisions

| Decision | Cost | Cooldown | Injury Risk | Outcomes |
|----------|------|----------|-------------|----------|
| **Accept Dangerous Wager** | Varies | 5 days | 50% | Big reward or -30 HP, wound |
| **Prove Your Courage** | — | 7 days | 35% | +Valor, +Rep or -25 HP |
| **Challenge Someone** | — | 7 days | 60% | Win: +Rep, +Valor. Lose: -30 HP, -Rep |

### NCO Training (T4-T6 only)

Train the lord's T1-T3 troops, giving them XP. Requires Tier 4+ and Leadership 40+.

| Decision | Cost | Cooldown | Injury Risk | Outcomes |
|----------|------|----------|-------------|----------|
| **Train the Men** | 3 hrs, -10 Rest | 2 days | 20% | Troop XP, Leadership XP, chain events |

**Outcome Distribution:**
- 40% Good progress (Troop XP +15)
- 15% Excellent session (Troop XP +30, bonus rep)
- 25% Slow going (Troop XP +8)
- 10% Minor accident (1 troop wounded)
- 5% Player injured (-15 HP)
- 4% Serious injury (1 troop wounded, rep loss)
- 1% Training death (1 troop loss, investigation)

**Skill modifiers:** Leadership 60+ reduces accidents, 80+ converts death to serious. Medicine 40+ can save serious cases.

**Chain triggers:** Multiple successes trigger mentor events. Accidents trigger resentment. Deaths trigger investigation.

---

### Decision Frequency

| Category | How Often Usable | Limiting Factor |
|----------|------------------|-----------------|
| Self-Care | As needed | State (already rested?) |
| Training | 1-2/day max | Rest drain |
| Social | 1/day social, 1/week letter | Cooldowns |
| Economic | 1/day gambling | Cooldown + must have gold |
| Career | 1/week audience | Long cooldowns |
| Info | Every 1-2 days | Short cooldowns |
| Risk-Taking | 1/week | Long cooldowns, high danger |

### Risk/Reward Balance

| Risk Level | Injury Chance | Reward Size |
|------------|---------------|-------------|
| Safe | 0-5% | Small XP, minor rep |
| Moderate | 10-20% | Good XP, decent rep/gold |
| Risky | 25-40% | Great XP, significant rep/gold |
| Dangerous | 50%+ | Exceptional rewards, real danger |

---

## 3. EVENTS

Situations that arise based on context and state. Player responds.

### Firing Rules
- **Frequency**: Average 1 every 2-3 days, sometimes quiet
- **Selection**: Weighted by context, role, and variety (not player optimization)
- **Escalation events**: 100% trigger when threshold crossed

### Event Selection Weights
```
+2 matches player role
+1 matches current context (war/peace/siege)
-3 same category as last event
-2 seen this event in last 30 days
+1 random variance
```

### Event Categories

**Escalation: Scrutiny** (Crime suspicion)
| Threshold | Event | Stakes |
|-----------|-------|--------|
| Scrutiny 2 | Watchful Eyes | Someone notices you |
| Scrutiny 4 | Questions Asked | Officer inquires |
| Scrutiny 6 | Under Investigation | Formal investigation |
| Scrutiny 8 | Evidence Found | They have proof |
| Scrutiny 10 | Arrest | Detained |

**Escalation: Discipline** (Rule-breaking)
| Threshold | Event | Stakes |
|-----------|-------|--------|
| Disc 2 | Verbal Warning | NCO notices |
| Disc 4 | Formal Reprimand | On record |
| Disc 6 | Restricted | Confined to camp |
| Disc 8 | Disciplinary Hearing | Face the captain |
| Disc 10 | Court Martial | Trial |

**Escalation: Medical**
| Threshold | Event | Stakes |
|-----------|-------|--------|
| Med 2 | Feeling Unwell (`medical_onset`) | Minor symptoms |
| Med 3 | Worsening (`medical_worsening`) | Performance impact |
| Med 4 | Complication (`medical_complication`) | Serious complications |
| Med 5 | Emergency (`medical_emergency`) | Life-threatening |

**Reputation Milestones** (First time crossing threshold)
| Track | Thresholds | Effect |
|-------|------------|--------|
| Lord | 20, 40, 60, 80, -20, -40 | Unlock content, change treatment |
| Officer | 20, 40, 60, 80, -20, -40 | Better/worse assignments |
| Soldier | 20, 40, -20, -40 | Social standing in unit |

**Role Events** (Weighted toward your role)

| Role | Count | Themes |
|------|-------|--------|
| Scout | 6 | Reconnaissance, tracking, terrain intel, enemy movements |
| Medic | 6 | Triage decisions, treatment choices, camp health crises |
| Engineer | 5 | Fortifications, siege work, equipment repairs |
| Officer | 6 | Command decisions, discipline enforcement, morale calls |
| Operative | 5 | Covert actions, black market, information trading |
| NCO | 5 | Training recruits, managing soldiers, squad disputes |
| Soldier | 8 | Universal grunt life, camp situations |

**Universal Events** (8 events, anyone can see)
Camp life, moral dilemmas, social situations. Not skill-gated but may have skill-gated options within them.

---

## 4. MAP INCIDENTS

Events triggered by map actions. Leverage natural gameplay moments.

### Triggers
| Trigger | When | Cooldown | Chance/Condition |
|---------|------|----------|------------------|
| LeavingBattle | After player battle ends | 1 hour | When not on cooldown |
| DuringSiege | Hourly while besieging settlement | 4 hours | 10% chance per hour |
| EnteringTown | Opening town/castle menu | 12 hours | When not on cooldown |
| EnteringVillage | Opening village menu | 12 hours | When not on cooldown |
| LeavingSettlement | Leaving any settlement | 12 hours | When not on cooldown |
| WaitingInSettlement | Hourly while lord stationed in town/castle | 8 hours | 15% chance per hour |

### Content by Trigger

**LeavingBattle** (11 incidents)
Post-combat moments. Adrenaline fading, death around you.
- Loot decisions, wounded comrades, enemy survivors
- Recognition from officers, first-kill processing, battle trophies
- Skill variants: Medicine (triage), Scouting (intel found), Leadership (casualty report)

**DuringSiege** (10 incidents)
Attrition and waiting. Boredom, desperation, tempers fraying.
- Water rationing, disease rumors, assault prep
- Deserters, gambling, supply theft, spoiled food
- Skill variants: Engineering (wall assessment), Medicine (disease), Scouting (sortie intel)

**EnteringTown** (8 incidents)
Shore leave. Temptation, release, civilian world.
- Tavern opportunities, market spending, old acquaintances
- Messages from home, criminal contacts, brawls
- Skill variants: Roguery (black market), Charm (negotiations), Trade (opportunities)

**EnteringVillage** (6 incidents)
Rural interactions. Simpler folk, different tensions.
- Local gratitude or resentment, foraging results
- Recruitment interest, rumors, theft accusations
- Skill variants: Scouting (tracks noticed), Medicine (village sick), Charm (locals)

**LeavingSettlement** (6 incidents)
Departure moments. Last chances, what's ahead.
- Farewells, stolen property discovered, hangovers
- Intelligence about destination, stowaways, last-minute purchases
- Skill variants: Scouting (route assessment), Leadership (organizing departure)

**WaitingInSettlement** (4 incidents)
Time passing while lord's party is garrisoned. Idle time in town or castle.
- Unexpected opportunities, chance encounters
- Trouble brewing among the men, boredom setting in
- Fires during garrison duty: 15% chance per hour when lord stationed in settlement

---

## Event Design Principles

### 1. Opportunities, Not Assignments
Events present situations. Player chooses response. 
Don't target player weaknesses—let them choose whether to engage.

### 2. Every Choice Has Trade-offs
No obviously correct answer. Gain here, lose there.
Reputation tracks should conflict (lord vs. soldiers).

### 3. Skill-Gated Options, Not Events
Everyone sees the event. Higher skills unlock better options.
Low-skill players always have something to do.

### 4. Tier Shapes Responsibility
- T1-3: Grunt experiences, survival focus
- T4-6: Specialist situations, identity forming
- T7-9: Command decisions, weight of leadership

### 5. Time Adds Flavor
Veteran status (1+ years) changes descriptions and NPC treatment.
Doesn't give free XP—earned through engagement.

---

---

## JSON + XML Localization

### How It Works

**JSON** contains structure, IDs, and effects.  
**XML** contains all player-visible text for translation.

JSON references XML string IDs:
```json
"titleId": "evt_broken_shield_title",
"setupId": "evt_broken_shield_setup",
"textId": "evt_broken_shield_opt_keep_text",
"resultTextId": "evt_broken_shield_opt_keep_outcome"
```

XML contains the actual text:
```xml
<string id="evt_broken_shield_title" text="The Broken Shield" />
<string id="evt_broken_shield_setup" text="After the battle, Sergeant Varn tosses you a dead man's shield..." />
<string id="evt_broken_shield_opt_keep_text" text="Keep it quietly" />
<string id="evt_broken_shield_opt_keep_outcome" text="You tuck the shield with your gear. Varn nods." />
```

### Order Schema (JSON)

```json
{
  "id": "order_guard_duty",
  "title": "Guard Duty",
  "description": "Stand watch through the night. Keep your eyes sharp and your blade ready.",
  "issuer": "Sergeant",
  "tags": ["soldier", "camp", "routine", "defense"],
  "strategic_tags": ["defense", "camp_routine"],
  "requirements": {
    "tier_min": 1,
    "tier_max": 3
  },
  "consequences": {
    "success": {
      "reputation": { "officer": 8 },
      "company_needs": { "Readiness": 6 },
      "trait_xp": { "Vigor": 12, "Discipline": 10 },
      "skill_xp": { "Athletics": 25 },
      "text": "A quiet night. The sergeant commends your vigilance as dawn breaks."
    },
    "failure": {
      "reputation": { "officer": -10 },
      "company_needs": { "Readiness": -8 },
      "text": "You dozed off at your post. A kicked bucket woke the camp. The shame burns."
    },
    "decline": {
      "reputation": { "officer": -12 },
      "textId": "order_guard_night_decline"
    }
  }
}
```

### Order Strings (XML)

```xml
<string id="order_guard_night_title" text="Night Watch" />
<string id="order_guard_night_desc" text="Stand watch through the night. Keep your eyes sharp." />
<string id="order_guard_night_success" text="A quiet night. The sergeant commends your vigilance." />
<string id="order_guard_night_failure" text="You dozed off at your post. The shame burns." />
<string id="order_guard_night_decline" text="'Too tired for watch duty?' Contempt drips from his voice." />
```

### Event Schema (JSON)

```json
{
  "id": "incident_post_battle_spoils",
  "category": "map_incident",
  "metadata": {
    "tier_range": { "min": 1, "max": 9 }
  },
  "delivery": {
    "method": "automatic",
    "channel": "inquiry",
    "incident_trigger": "leaving_battle"
  },
  "triggers": {
    "all": ["is_enlisted", "ai_safe"],
    "any": [],
    "time_of_day": ["any"]
  },
  "requirements": {
    "tier": { "min": 1, "max": 9 }
  },
  "timing": {
    "cooldown_days": 7,
    "priority": "normal",
    "one_time": false
  },
  "content": {
    "titleId": "evt_broken_shield_title",
    "setupId": "evt_broken_shield_setup",
    "options": [
      {
        "id": "keep_quiet",
        "textId": "evt_broken_shield_opt_keep_text",
        "condition": null,
        "risk": "safe",
        "costs": { "fatigue": 0, "gold": 0 },
        "rewards": { "xp": {}, "gold": 0 },
        "effects": {
          "scrutiny": 1,
          "soldier_reputation": 5
        },
        "resultTextId": "evt_broken_shield_opt_keep_outcome"
      },
      {
        "id": "turn_in",
        "textId": "evt_broken_shield_opt_turnin_text",
        "condition": null,
        "risk": "safe",
        "effects": {
          "soldier_reputation": -5,
          "officer_reputation": 8
        },
        "resultTextId": "evt_broken_shield_opt_turnin_outcome"
      },
      {
        "id": "suggest_split",
        "textId": "evt_broken_shield_opt_split_text",
        "condition": { "min_skill": { "charm": 40 } },
        "risk": "safe",
        "rewards": { "gold": 25 },
        "effects": {
          "scrutiny": 1,
          "soldier_reputation": 8
        },
        "resultTextId": "evt_broken_shield_opt_split_outcome"
      }
    ]
  }
}
```

### Event Strings (XML)

```xml
<!-- The Broken Shield - Map Incident -->
<string id="evt_broken_shield_title" text="The Broken Shield" />
<string id="evt_broken_shield_setup" text="After the battle, Sergeant Varn tosses you a dead man's shield. 'Yours now. He won't need it.'\n\nIt's better than your issued gear. But equipment is supposed to go through the quartermaster." />

<string id="evt_broken_shield_opt_keep_text" text="Keep it quietly" />
<string id="evt_broken_shield_opt_keep_outcome" text="You tuck the shield with your gear. Varn nods approvingly." />

<string id="evt_broken_shield_opt_turnin_text" text="Turn it in properly" />
<string id="evt_broken_shield_opt_turnin_outcome" text="The quartermaster logs it. Varn looks disappointed." />

<string id="evt_broken_shield_opt_split_text" text="'How about we split the value?'" />
<string id="evt_broken_shield_opt_split_outcome" text="Varn grins. 'Smart lad.' You pocket your share." />
```

### String ID Naming Convention

```
{type}_{event_id}_{element}

Orders:
  order_{id}_title
  order_{id}_desc
  order_{id}_success
  order_{id}_failure
  order_{id}_decline

Events:
  evt_{id}_title
  evt_{id}_setup
  evt_{id}_opt_{option_id}_text
  evt_{id}_opt_{option_id}_outcome
  evt_{id}_opt_{option_id}_failure  (if risky)
```

### Key Schema Fields

**delivery.incident_trigger** values for Map Incidents:
- `leaving_battle`
- `during_siege`
- `entering_town`
- `entering_village`
- `entering_castle`
- `leaving_settlement`
- `waiting_in_settlement`

**option.condition** for skill/trait gates:
```json
{ "min_skill": { "scouting": 60 } }
{ "min_trait": { "valor": 1 } }
{ "min_tier": 5 }
{ "has_role": "scout" }
```

**effects** fields:
- `scrutiny`, `discipline`, `medical_risk` (escalation)
- `soldier_reputation`, `officer_reputation`, `lord_reputation`
- `company_needs`: `{ "Readiness": 5, "Morale": 3 }`

**rewards.xp** for skills:
```json
{ "xp": { "scouting": 25, "athletics": 15 } }
```

### Decision Schema (JSON)

Decisions have costs, cooldowns, requirements, and weighted outcomes:

```json
{
  "id": "decision_spar",
  "textId": "decision_spar_text",
  "descriptionId": "decision_spar_desc",
  "category": "training",
  "costs": {
    "time_hours": 2,
    "rest": 20,
    "gold": 0
  },
  "cooldown_days": 2,
  "requirements": {
    "rest_min": 40,
    "not_wounded": true,
    "context": ["camp"],
    "not_in_battle": true
  },
  "outcomes": [
    {
      "id": "win",
      "chance": 35,
      "effects": {
        "skill_xp": { "OneHanded": 30, "Athletics": 15 },
        "soldier_reputation": 8,
        "trait_xp": { "Valor": 10 }
      },
      "resultTextId": "decision_spar_win"
    },
    {
      "id": "lose",
      "chance": 35,
      "effects": {
        "skill_xp": { "OneHanded": 20 },
        "soldier_reputation": -3
      },
      "resultTextId": "decision_spar_lose"
    },
    {
      "id": "minor_injury",
      "chance": 20,
      "effects": {
        "skill_xp": { "OneHanded": 15 },
        "hp_change": -15,
        "medical_risk": 1
      },
      "resultTextId": "decision_spar_hurt"
    },
    {
      "id": "serious_injury",
      "chance": 10,
      "effects": {
        "hp_change": -35,
        "apply_wound": true,
        "medical_risk": 2
      },
      "resultTextId": "decision_spar_wounded"
    }
  ]
}
```

### Decision Strings (XML)

```xml
<string id="decision_spar_text" text="Spar with a fellow soldier" />
<string id="decision_spar_desc" text="Practice combat with training weapons. Risk of injury." />
<string id="decision_spar_win" text="You best your opponent handily. The men watching nod with respect." />
<string id="decision_spar_lose" text="You're put on your back. 'Better luck next time,' your opponent grins." />
<string id="decision_spar_hurt" text="A practice blade catches your ribs. You'll feel that for days." />
<string id="decision_spar_wounded" text="The blade slips. Real blood. The surgeon shakes his head as he stitches you up." />
```

### Decision Effect Fields

| Field | Type | Description |
|-------|------|-------------|
| `hp_change` | int | Direct HP modification (negative = damage) |
| `apply_wound` | bool | Apply native Bannerlord wound |
| `medical_risk` | int | Add to Medical Risk escalation |
| `skill_xp` | object | Skill name → XP amount |
| `trait_xp` | object | Trait name → XP amount |
| `soldier_reputation` | int | Reputation change |
| `officer_reputation` | int | Reputation change |
| `lord_reputation` | int | Reputation change |
| `scrutiny` | int | Add to Scrutiny escalation |
| `discipline` | int | Add to Discipline escalation |
| `rest` | int | Modify Rest (can be positive) |
| `gold` | int | Gold change (positive = gain) |
| `triggers_event` | string | Event ID to fire after this outcome |
| `troop_loss` | int | Remove N troops from party (rare, dramatic) |
| `troop_wounded` | int | Wound N troops (temporary roster reduction) |
| `troop_xp` | int | Give XP to lord's T1-T3 troops (NCO training) |
| `food_loss` | int | Remove N food items from party inventory |
| `item_loss` | object | `{ "item_id": N }` Remove specific items |

---

---

## Delivery Mechanisms

### How Each Content Type Is Displayed

| Type | UI Method | Code Pattern |
|------|-----------|--------------|
| **Orders** | `InquiryData` popup | Accept/Decline buttons, immediate response |
| **Decisions** | Game Menu option | Camp Hub menu entries → submenu or action |
| **Events** | `MultiSelectionInquiryData` popup | Multiple choice options, callbacks |
| **Map Incidents** | Native `MapState.NextIncident` | Full incident UI with options |

### Native Incident System (Map Incidents)

For map incidents, we use Bannerlord's native incident UI:

```csharp
// Register incident once at session start
var incident = Game.Current.ObjectManager.RegisterPresumedObject(
    new Incident("incident_post_battle_spoils"));

incident.Initialize(
    "{=evt_broken_shield_title}The Broken Shield",  // XML string ID
    "{=evt_broken_shield_setup}After the battle...", // XML string ID  
    IncidentsCampaignBehaviour.IncidentTrigger.LeavingBattle,
    IncidentsCampaignBehaviour.IncidentType.PostBattle,
    CampaignTime.Days(7f),  // Cooldown
    description => CheckIncidentConditions());  // Condition func

incident.AddOption(
    "{=evt_broken_shield_opt_keep}Keep it quietly",
    new List<IncidentEffect> { 
        IncidentEffect.Custom(
            condition: () => true,
            consequence: () => { ApplyKeepEffects(); return hints; },
            hint: _ => hintList)
    });

// Trigger by setting MapState.NextIncident
var mapState = GameStateManager.Current?.LastOrDefault<MapState>();
mapState.NextIncident = incident;
```

### Inquiry Popup (Events, Orders)

For events/orders that need immediate response:

```csharp
var inquiry = new MultiSelectionInquiryData(
    title.ToString(),
    description.ToString(),
    options,  // List<InquiryElement>
    isExitShown: false,
    minSelectableCount: 1,
    maxSelectableCount: 1,
    affirmativeText: "Continue",
    negativeText: "Cancel",
    onAffirmative: selection => HandleChoice(selection),
    onNegative: _ => HandleCancel());

MBInformationManager.ShowMultiSelectionInquiry(inquiry);
```

### Game Menu (Decisions)

For player-initiated decisions, add menu options to Camp Hub:

```csharp
// In EnlistedMenuBehavior or CampMenuHandler
starter.AddGameMenuOption(
    "enlisted_camp_hub",
    "decision_rest",
    "{=decision_rest}Rest and recover",
    args => { args.optionLeaveType = LeaveType.Wait; return true; },
    args => ShowRestDecisionInquiry(),
    false,
    priority: 10);
```

### Current Menus Available

| Menu ID | Purpose | Where Decisions Go |
|---------|---------|-------------------|
| `enlisted_status` | Main status display | Status info only |
| `enlisted_camp_hub` | Camp hub | **Decisions go here** |
| `enlisted_service_records` | Service records | Records viewing |
| `enlisted_troop_selection` | Promotion/equipment | Quartermaster actions |

### Trigger → Delivery Flow

```
MAP INCIDENTS:
  MapEventEnded / GameMenuOpened / HourlyTick
    → EnlistedMapIncidentBehavior checks conditions
    → Selects random eligible incident  
    → Sets MapState.NextIncident
    → Native UI displays

EVENTS:
  State threshold crossed / Context match
    → EventManager selects event
    → Shows MultiSelectionInquiryData
    → Player selects option
    → Effects applied

ORDERS:
  Daily tick checks order interval
    → OrderManager.IssueOrder()
    → Shows InquiryData (Accept/Decline)
    → On accept: order tracked, timer starts
    → On complete: consequences applied

DECISIONS:
  Player opens Camp Hub menu
    → Sees available decision options
    → Selects one → submenu or inquiry
    → Player makes choice
    → Effects applied
```

---

## Placeholder Variables

All text in events, orders, and decisions can use placeholder variables that are replaced at runtime with actual game data.

### Player & Identity
| Variable | Description | Example |
|----------|-------------|---------|
| `{PLAYER_NAME}` | Player's first name | "Aldric" |
| `{PLAYER_RANK}` | Current enlisted rank | "Veteran Soldier" |

### NCO & Officers
| Variable | Description | Example |
|----------|-------------|---------|
| `{SERGEANT}` | NCO full name with rank | "Sergeant Bjorn" |
| `{SERGEANT_NAME}` | Same as {SERGEANT} | "Sergeant Bjorn" |
| `{NCO_NAME}` | Same as {SERGEANT} | "Sergeant Bjorn" |
| `{NCO_RANK}` | NCO rank title only | "Sergeant" |
| `{OFFICER_NAME}` | Generic officer reference | "Sergeant Bjorn" |
| `{CAPTAIN_NAME}` | Captain reference | "the Captain" |

### Fellow Soldiers
| Variable | Description | Example |
|----------|-------------|---------|
| `{COMRADE_NAME}` | Random soldier name | "Erik" |
| `{SOLDIER_NAME}` | Same as {COMRADE_NAME} | "Erik" |
| `{VETERAN_1_NAME}` | Veteran soldier (ringleaders, experienced) | "Magnus" |
| `{VETERAN_2_NAME}` | Second veteran soldier | "Olaf" |
| `{RECRUIT_NAME}` | New recruit name | "Young Tomas" |
| `{SECOND_SHORT}` | Another soldier name | "Sven" |

### Naval Crew (War Sails DLC)
| Variable | Description | Example |
|----------|-------------|---------|
| `{BOATSWAIN_NAME}` | Ship's boatswain | "the Boatswain" |
| `{NAVIGATOR_NAME}` | Ship's navigator | "the Navigator" |
| `{FIELD_MEDIC_NAME}` | Medical specialist | "the Field Medic" |
| `{SHIP_NAME}` | Ship name | "Sea Wolf" |
| `{DESTINATION_PORT}` | Destination port | "port" |
| `{DAYS_AT_SEA}` | Days at sea | "7" |

### Lord & Faction
| Variable | Description | Example |
|----------|-------------|---------|
| `{LORD_NAME}` | Enlisted lord's name | "Derthert" |
| `{LORD_TITLE}` | Lord or Lady | "Lord" |
| `{PREVIOUS_LORD}` | Previous lord (transfer events) | "your previous lord" |
| `{ALLIED_LORD}` | Allied lord reference | "an allied lord" |
| `{FACTION_NAME}` | Your faction's name | "Vlandia" |
| `{KINGDOM_NAME}` | Kingdom name | "Kingdom of Vlandia" |
| `{ENEMY_FACTION_ADJECTIVE}` | Enemy descriptor | "enemy" |

### Location & Party
| Variable | Description | Example |
|----------|-------------|---------|
| `{SETTLEMENT_NAME}` | Current settlement | "Sargot" |
| `{COMPANY_NAME}` | Your party name | "Derthert's Retinue" |
| `{TROOP_COUNT}` | Total troop count | "87" |

### Rank Progression
| Variable | Description | Example |
|----------|-------------|---------|
| `{NEXT_RANK}` | Next rank in progression | "the next rank" |
| `{SECOND_RANK}` | Current rank (variant) | "Veteran Soldier" |

### Medical Events
| Variable | Description | Example |
|----------|-------------|---------|
| `{CONDITION_TYPE}` | Type of illness/injury | "illness" |
| `{CONDITION_LOCATION}` | Body part affected | "your arm" |
| `{COMPLICATION_NAME}` | Medical complication | "infection" |
| `{REMEDY_NAME}` | Treatment/medicine | "medicine" |

### Usage Notes

These variables are automatically populated when events are displayed. Character names are generated from culture-appropriate name pools based on your enlisted lord's faction. If a variable can't be resolved (e.g., no settlement nearby), it uses a reasonable fallback like "the settlement" or "the Captain".

---

## File Organization

```
ModuleData/Enlisted/
├── Events/                         (Loaded recursively by EventCatalog)
│   ├── camp_events.json            (General camp life events)
│   ├── events_general.json         (Universal soldier events)
│   ├── events_escalation_thresholds.json  (Scrutiny/Discipline/Medical thresholds)
│   ├── events_onboarding.json      (First-enlistment guaranteed events)
│   ├── events_promotion.json       (Promotion and proving events)
│   ├── events_training.json        (Training-related events)
│   ├── events_retinue.json         (Retinue events for T7+ commanders)
│   ├── events_duty_scout.json      (Scout duty events)
│   ├── events_duty_medic.json      (Medic duty events)
│   ├── events_duty_engineer.json   (Engineer duty events)
│   ├── events_duty_*.json          (7 more duty role files)
│   ├── events_pay_tension.json     (Pay tension events)
│   ├── events_pay_loyal.json       (Pay loyalty events)
│   ├── events_pay_mutiny.json      (Pay mutiny events)
│   ├── events_player_decisions.json (Player-initiated events, legacy)
│   ├── incidents_battle.json       (LeavingBattle incidents)
│   ├── incidents_siege.json        (DuringSiege incidents)
│   ├── incidents_town.json         (EnteringTown incidents)
│   ├── incidents_village.json      (EnteringVillage incidents)
│   ├── incidents_leaving.json      (LeavingSettlement incidents)
│   ├── incidents_waiting.json      (WaitingInSettlement incidents)
│   ├── incidents_retinue.json      (Retinue post-battle incidents)
│   ├── muster_events.json          (Muster and recruitment events)
│   ├── schema_version.json         (Schema metadata, not loaded)
│   └── Role/
│       └── scout_events.json       (Scout role-specific events)
├── Decisions/                      (Loaded recursively by EventCatalog)
│   └── decisions.json              (34 player-initiated Camp Hub decisions, dec_* prefix)
└── Orders/                         (Loaded by OrderCatalog)
    ├── orders_t1_t3.json           (6 basic orders)
    ├── orders_t4_t6.json           (6 specialist orders)
    └── orders_t7_t9.json           (5 leadership orders)
```

**Note:** EventCatalog loads all .json files from Events/ and Decisions/ directories recursively, excluding schema_version.json. OrderCatalog loads orders separately from the Orders/ directory.
