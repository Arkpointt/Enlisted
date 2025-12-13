# Promotion System — Lance Life

This document defines the promotion system for Tiers 1-6, including eligibility requirements, proving events, and the narrative moments that define a soldier's career.

---

## Table of Contents

1. [Overview](#overview)
2. [Design Philosophy](#design-philosophy)
3. [Promotion Requirements](#promotion-requirements)
4. [Proving Events](#proving-events)
5. [Formation Selection (T2)](#formation-selection-t2)
6. [Failure and Recovery](#failure-and-recovery)
7. [Implementation Notes](#implementation-notes)

---

## Overview

Promotion is no longer automatic XP threshold → rank up. Instead:

1. **Eligibility** — Meet XP, time, activity, and standing requirements
2. **Recommendation** — Your lance leader (or lord) must support you
3. **Proving Event** — A story moment where you demonstrate readiness
4. **Choice** — Key promotions include meaningful decisions that shape your career

### Tier Summary

| Promotion | Inspiration | Core Moment | Key Choice |
|-----------|-------------|-------------|------------|
| T1→T2 | XCOM first blood | "You've shown [X], where do you fit?" | **Formation** |
| T2→T3 | Kingdom Come testing | Sergeant tests your judgment | Duty focus |
| T3→T4 | Darkest Dungeon crisis | Crisis reveals character | Define leadership style |
| T4→T5 | Battle Brothers bonds | Lance mates speak for you | — |
| T5→T6 | Crusader Kings politics | Lord summons you | Political stance |

---

## Design Philosophy

### Promotions Should Feel Earned

- **Activity matters:** You can't AFK to promotion. Events completed, battles survived.
- **Relationships matter:** Your lance leader must vouch for you. Being hated blocks advancement.
- **Clean record matters:** High discipline means you're a problem, not promotion material.

### Promotions Should Be Memorable

- Each tier has a unique proving event with different tone and stakes
- Your choices during these events have lasting effects
- Failure isn't just "try again" — it's a story beat

### Promotions Should Shape Your Character

- T2: What kind of soldier are you? (Formation)
- T3: What are you good at? (Duty)
- T4: Who are you under pressure? (Character)
- T5: Do your people respect you? (Bonds)
- T6: Can you play the game? (Politics)

---

## Promotion Requirements

### Eligibility Table

#### Enlisted Track (T1-T6) — 3 Years to Peak

| Promotion | XP | Days in Rank | Events | Battles | Lance Rep | Leader Rel | Lord Rel | Max Disc |
|-----------|-----|--------------|--------|---------|-----------|------------|----------|----------|
| T1→T2 | 700 | 14 | 5 | 2 | ≥0 | ≥0 | — | <8 |
| T2→T3 | 2,200 | 35 | 12 | 6 | ≥10 | ≥10 | — | <7 |
| T3→T4 | 4,400 | 56 | 25 | 12 | ≥20 | ≥20 | — | <6 |
| T4→T5 | 6,600 | 56 | 40 | 20 | ≥30 | ≥30 | — | <5 |
| T5→T6 | 8,800 | 56 | 55 | 30 | ≥40 | — | ≥15 | <4 |

#### Commander Track (T7-T9) — 4.5 Additional Years

| Promotion | XP | Days in Rank | Battles Cmd | Survival | Lord Rel | Faction Rel | Max Disc |
|-----------|-----|--------------|-------------|----------|----------|-------------|----------|
| T6→T7 | 11,000 | 84 | — | — | ≥30 | — | <3 |
| T7→T8 | 14,000 | 126 | 15 | ≥70% | ≥40 | — | <3 |
| T8→T9 | 18,000 | 168 | 30 | — | ≥50 | ≥20 | <2 |

### Timeline Summary

| Milestone | Day | In-Game Years | Key Unlock |
|-----------|-----|---------------|------------|
| Enlist | 0 | 0 | — |
| T2 | 21 | 0.25 | Formation choice |
| T3 | 63 | 0.75 | Duty professions |
| T4 | 126 | 1.5 | Character tag |
| T5 | 189 | 2.25 | Lance leader |
| T6 | 252 | 3.0 | Loyalty tag, peak enlisted |
| T7 | 378 | 4.5 | Retinue (15 soldiers) |
| T8 | 504 | 6.0 | Command style, retinue (25) |
| T9 | 630 | 7.5 | Strategic style, retinue (35) |

---

## Culture-Specific Ranks

Rank names vary by culture. The tier system is universal, but each culture has its own titles.

### Empire (Legion / Discipline)

| Tier | Rank | Translation/Meaning |
|------|------|---------------------|
| T1 | Tiro | Recruit |
| T2 | Miles | Soldier |
| T3 | Immunes | Exempt (Specialist) |
| T4 | Principalis | Principal (NCO) |
| T5 | Evocatus | Recalled Veteran |
| T6 | Centurion | Century Commander |
| T7 | Primus Pilus | First Spear / Captain |
| T8 | Tribune | Staff Officer |
| T9 | Legate | General / Warlord |

### Vlandia (Feudal / Chivalry)

| Tier | Rank |
|------|------|
| T1 | Peasant |
| T2 | Levy |
| T3 | Footman |
| T4 | Man-at-Arms |
| T5 | Sergeant |
| T6 | Knight Bachelor |
| T7 | Cavalier |
| T8 | Banneret |
| T9 | Castellan |

### Sturgia (Tribal / Shield Wall)

| Tier | Rank | Translation/Meaning |
|------|------|---------------------|
| T1 | Thrall | Bonded Servant |
| T2 | Ceorl | Freeman |
| T3 | Fyrdman | Militia |
| T4 | Drengr | Bold Warrior |
| T5 | Huskarl | Household Guard |
| T6 | Varangian | Elite Guard |
| T7 | Champion | The Elite |
| T8 | Thane | Local Chieftain |
| T9 | High Warlord | — |

### Khuzait (Steppe / Horde)

| Tier | Rank | Translation/Meaning |
|------|------|---------------------|
| T1 | Outsider | — |
| T2 | Nomad | — |
| T3 | Noker | Companion |
| T4 | Warrior | — |
| T5 | Veteran | — |
| T6 | Bahadur | Hero/Brave |
| T7 | Arban | Leader of 10 |
| T8 | Zuun | Leader of 100 |
| T9 | Noyan | Noble Commander |

### Battania (Celtic / Guerrilla)

| Tier | Rank |
|------|------|
| T1 | Woodrunner |
| T2 | Clan Warrior |
| T3 | Skirmisher |
| T4 | Raider |
| T5 | Oathsworn |
| T6 | Fian |
| T7 | Highland Champion |
| T8 | Clan Chief |
| T9 | High King's Guard |

### Aserai (Desert / Mercantile)

| Tier | Rank | Translation/Meaning |
|------|------|---------------------|
| T1 | Tribesman | — |
| T2 | Skirmisher | — |
| T3 | Footman | — |
| T4 | Veteran | — |
| T5 | Guard | — |
| T6 | Faris | Knight/Horseman |
| T7 | Emir's Chosen | — |
| T8 | Sheikh | Minor Leader |
| T9 | Grand Vizier | Strategic Leader |

### Mercenary / Universal (Generic Fallback)

| Tier | Rank |
|------|------|
| T1 | Follower |
| T2 | Recruit |
| T3 | Free Sword |
| T4 | Veteran |
| T5 | Blade |
| T6 | Chosen |
| T7 | Captain |
| T8 | Commander |
| T9 | Marshal |

### Rank Configuration Schema

```json
{
  "culture_ranks": {
    "empire": {
      "t1": "Tiro",
      "t2": "Miles",
      "t3": "Immunes",
      "t4": "Principalis",
      "t5": "Evocatus",
      "t6": "Centurion",
      "t7": "Primus Pilus",
      "t8": "Tribune",
      "t9": "Legate"
    },
    "vlandia": {
      "t1": "Peasant",
      "t2": "Levy",
      "t3": "Footman",
      "t4": "Man-at-Arms",
      "t5": "Sergeant",
      "t6": "Knight Bachelor",
      "t7": "Cavalier",
      "t8": "Banneret",
      "t9": "Castellan"
    },
    "sturgia": {
      "t1": "Thrall",
      "t2": "Ceorl",
      "t3": "Fyrdman",
      "t4": "Drengr",
      "t5": "Huskarl",
      "t6": "Varangian",
      "t7": "Champion",
      "t8": "Thane",
      "t9": "High Warlord"
    },
    "khuzait": {
      "t1": "Outsider",
      "t2": "Nomad",
      "t3": "Noker",
      "t4": "Warrior",
      "t5": "Veteran",
      "t6": "Bahadur",
      "t7": "Arban",
      "t8": "Zuun",
      "t9": "Noyan"
    },
    "battania": {
      "t1": "Woodrunner",
      "t2": "Clan Warrior",
      "t3": "Skirmisher",
      "t4": "Raider",
      "t5": "Oathsworn",
      "t6": "Fian",
      "t7": "Highland Champion",
      "t8": "Clan Chief",
      "t9": "High King's Guard"
    },
    "aserai": {
      "t1": "Tribesman",
      "t2": "Skirmisher",
      "t3": "Footman",
      "t4": "Veteran",
      "t5": "Guard",
      "t6": "Faris",
      "t7": "Emir's Chosen",
      "t8": "Sheikh",
      "t9": "Grand Vizier"
    },
    "mercenary": {
      "t1": "Follower",
      "t2": "Recruit",
      "t3": "Free Sword",
      "t4": "Veteran",
      "t5": "Blade",
      "t6": "Chosen",
      "t7": "Captain",
      "t8": "Commander",
      "t9": "Marshal"
    }
  }
}
```

### Rank Resolution

```csharp
public string GetRankTitle(int tier, string cultureId)
{
    // Try culture-specific rank
    if (_cultureRanks.TryGetValue(cultureId, out var ranks))
    {
        if (ranks.TryGetValue($"t{tier}", out var title))
            return title;
    }
    
    // Fallback to mercenary/universal ranks
    return _cultureRanks["mercenary"][$"t{tier}"];
}
```

### Requirement Definitions

| Requirement | Description |
|-------------|-------------|
| **XP** | Total enlisted XP accumulated (daily + battle + kills) |
| **Days in Rank** | Minimum days at current tier before eligible |
| **Events Done** | Total story events completed this enlistment term |
| **Lance Rep** | Current lance reputation track value |
| **Max Discipline** | Must be below this threshold (clean record) |
| **Battles** | Battles survived this enlistment term |
| **Leader Relation** | Relationship with lance leader (T1-5) or lord (T6) |

### Status Display

When player is eligible but blocked:

```
Ready for {NEXT_RANK}
├── ✓ XP: 850/800
├── ✓ Days in rank: 9/7
├── ✓ Events: 4/3
├── ✓ Battles: 2/1
├── ✗ Lance reputation: -5 (need 0+)
├── ✓ Discipline: 3/7
└── ✗ Lance leader: -12 (need 0+)

You need to improve your standing with the lance before 
{LANCE_LEADER_SHORT} will recommend you for promotion.
```

---

## Proving Events

Each promotion has a unique proving event that fires when all eligibility requirements are met.

---

### T1→T2: "Finding Your Place"

**Inspiration:** XCOM first blood — your actions define your class

**Trigger:** All T1→T2 requirements met

**Concept:** The sergeant has watched you. Based on what you've done (events completed, behaviors shown), they offer you a path. Your choice determines your formation.

#### Event: `promotion_t1_t2_finding_your_place`

```json
{
  "id": "promotion_t1_t2_finding_your_place",
  "category": "promotion",
  
  "metadata": {
    "from_tier": 1,
    "to_tier": 2,
    "tier_range": { "min": 1, "max": 1 }
  },
  
  "delivery": {
    "method": "automatic",
    "menu": null
  },
  
  "triggers": {
    "all": ["is_enlisted", "ai_safe", "promotion_eligible_t2"]
  },
  
  "timing": {
    "cooldown_days": 0,
    "priority": "critical",
    "one_time": true
  }
}
```

#### Setup Text

**Variant A: Balanced (no clear pattern)**
> The evening drill ends and the lance disperses. {LANCE_LEADER_SHORT} catches your eye and gestures you over.
>
> "You've made it past the first cut, {PLAYER_NAME}. Most don't." They look you over with an appraising eye. "Time to figure out where you belong."
>
> They nod toward different parts of the camp. "Infantry needs steady hands for the shield wall. Archers need sharp eyes. Cavalry..." they shrug, "needs riders who won't fall off."
>
> "So. Where do you see yourself?"

**Variant B: Combat-focused (many battle events)**
> {LANCE_LEADER_SHORT} finds you cleaning blood off your gear. "You don't flinch," they say. It's not a question.
>
> "Seen a lot of green soldiers freeze up. Piss themselves. Run." They sit down across from you. "You didn't. That matters."
>
> "So now I need to know — where do you want to put that steadiness to use?"

**Variant C: Support-focused (many duty events)**
> "You've been busy." {LANCE_LEADER_SHORT} tosses a duty roster at your feet. "Quartermaster says you're reliable. Surgeon says you don't faint at blood. Scout sergeant says you've got decent eyes."
>
> They lean against a post. "Army needs soldiers who do their jobs without being told twice. You're one of those."
>
> "Question is, what job do you want?"

#### Options

| ID | Text | Condition | Result | Effects |
|----|------|-----------|--------|---------|
| `choose_infantry` | "The shield wall. I'll hold the line." | always | Infantry formation | `formation: infantry`, `+5 lance_rep` |
| `choose_archer` | "Give me a bow. I'll strike from distance." | always | Archer formation | `formation: archer`, `+5 lance_rep` |
| `choose_cavalry` | "Put me on a horse." | always | Cavalry formation | `formation: cavalry`, `+5 lance_rep` |
| `choose_horse_archer` | "I can shoot from the saddle." | `faction_has_horse_archers` | Horse Archer formation | `formation: horse_archer`, `+5 lance_rep` |
| `let_them_choose` | "Wherever you need me most, {LANCE_LEADER_SHORT}." | always | Assigned by army need | `formation: [weighted random]`, `+10 lance_rep`, `+5 leader_relation` |

#### Outcomes

**Infantry:**
> {LANCE_LEADER_SHORT} nods slowly. "Shield wall it is. You'll train with {VETERAN_1_NAME} — they'll teach you how to hold when everything in you says run."
>
> They clasp your shoulder. "Welcome to the line, {NEXT_RANK}."

**Archer:**
> "Good eyes are worth more than strong arms in this army." {LANCE_LEADER_SHORT} gestures toward the archery range. "Report to the bow sergeant tomorrow at dawn. Miss the first session and you're back to hauling supplies."
>
> A hint of a smile. "Don't miss."

**Cavalry:**
> {LANCE_LEADER_SHORT} raises an eyebrow. "You can ride?" They don't wait for an answer. "We'll find out. The horse master will test you tomorrow. If you fall off, you're infantry."
>
> "Don't fall off, {NEXT_RANK}."

**Horse Archer (Aserai/Khuzait):**
> {LANCE_LEADER_SHORT}'s expression shifts to something like respect. "Shooting from horseback. Not many can do it. Fewer can do it well."
>
> "You'll train with the {CULTURE_HORSE_ARCHER_NAME}. They don't suffer fools, and they don't repeat themselves. Watch and learn, {NEXT_RANK}."

**Let Them Choose:**
> {LANCE_LEADER_SHORT} studies you for a long moment. "Humble. Or smart. Either works."
>
> They glance at the camp. "We're short on {NEEDED_FORMATION}. That's where you'll go." A firm nod. "I appreciate a soldier who goes where they're needed. Remember that."

#### Ceremony

After the choice:

> The next morning, {LANCE_LEADER_SHORT} calls the lance to formation. You stand a little straighter than before.
>
> "This one's made it," they announce, gesturing at you. "{PLAYER_NAME}. No longer fresh meat. {NEXT_RANK}, {FORMATION} formation."
>
> A few nods from the veterans. {VETERAN_1_NAME} thumps your shoulder as the formation breaks. "Don't make us look bad."

**Effects:**
- Tier → 2
- Formation set
- Lance reputation +5 (or +10 for humble choice)
- Lance leader relation +5 (humble choice only)
- Unlocks: Formation-specific training events, better equipment access

---

### T2→T3: "The Sergeant's Test"

**Inspiration:** Kingdom Come: Deliverance — a specific NPC tests your judgment

**Trigger:** All T2→T3 requirements met

**Concept:** The sergeant gives you a task that tests your judgment, not just your obedience. How you handle it determines if you're NCO material.

#### Event: `promotion_t2_t3_sergeants_test`

#### Setup Text

> Three days into a long march, {LANCE_LEADER_SHORT} pulls you aside.
>
> "Got a job for you. Not a test." The look in their eyes says otherwise. "We're low on water. There's a village half a day east — might have a well. Might have hostiles. Might have nothing."
>
> They hand you a waterskin. "Take two soldiers. Find water. Get back before dark."
>
> "How you do it is up to you."

#### Options

| ID | Text | Risk | Result | Effects |
|----|------|------|--------|---------|
| `careful_approach` | Scout carefully, approach the village slowly, assess before contact. | Safe | Methodical success — takes longer but safe | `+30 scouting XP`, `+5 lance_rep`, promotes |
| `direct_approach` | March straight in, show we're not afraid, negotiate openly. | Risky 60% | Bold success / Walked into trouble | Success: `+20 charm XP`, `+8 lance_rep`, promotes / Fail: `+2 discipline`, delays promotion 7 days |
| `avoid_village` | Skip the village, search for streams or other water sources. | Safe | Independent thinking — might work, might not | `+25 scouting XP`, `+3 lance_rep`, promotes (outcome varies) |
| `delegate_decision` | Ask your two soldiers what they think, then decide together. | Safe | Collaborative approach | `+20 leadership XP`, `+10 lance_rep`, promotes |

#### Outcomes

**Careful Approach:**
> You return at dusk with full waterskins. The village was friendly — once they saw you weren't raiders.
>
> {LANCE_LEADER_SHORT} takes the water without comment. Then: "You took your time. Came back with everyone. Came back with water." A nod. "That's the job."

**Direct Approach (Success):**
> You march in like you own the place. The villagers are scared — then relieved when you offer coin for water instead of taking it.
>
> {LANCE_LEADER_SHORT} listens to your report. "Bold. Could've gone wrong." They shrug. "Didn't. Sometimes bold is right."

**Direct Approach (Failure):**
> The village wasn't friendly. You walked into a ambush of angry farmers with pitchforks. No one died, but you're lucky to get back at all.
>
> {LANCE_LEADER_SHORT} listens in silence. "You'll learn. Or you won't." They walk away. "Not yet, {PLAYER_NAME}. Not yet."

**Avoid Village:**
> You find a stream two miles south. Clean water, no risk. The soldiers with you are impressed — or confused.
>
> {LANCE_LEADER_SHORT} considers your choice. "Didn't do what I said. Did what was needed." A long pause. "I can work with that."

**Delegate Decision:**
> You return with water and two soldiers who respect you more than when they left.
>
> {LANCE_LEADER_SHORT} notices. "Asked them, did you? Listened?" They nod slowly. "A sergeant who listens lives longer. So do their soldiers."

#### Ceremony

> {LANCE_LEADER_SHORT} addresses the lance at morning muster.
>
> "{PLAYER_NAME}. You've shown judgment. That's rarer than courage and more useful than strength."
>
> They toss you a worn leather cord — a sergeant's marker. "{NEXT_RANK}. Don't let it go to your head."
>
> {VETERAN_1_NAME} grins. {SECOND_NAME} gives you an evaluating look. The recruits eye you differently now.

**Effects:**
- Tier → 3
- Military professions unlock (duties expand)
- Lance reputation +5 to +10
- Lance leader relation +5
- New duty events available

---

### T3→T4: "Crisis Reveals Character"

**Inspiration:** Darkest Dungeon — stress and crisis reveal who you really are

**Trigger:** All T3→T4 requirements met + situational trigger (after battle OR camp crisis)

**Concept:** Something goes wrong. Not a test — a real crisis. How you respond defines your character for the rest of your service.

#### Event: `promotion_t3_t4_crisis_reveals`

#### Setup Text

**Variant A: After Battle**
> The battle is over. You won — barely.
>
> {WOUNDED_SOLDIER_NAME} is bleeding out by the supply wagon. The surgeon is overwhelmed. {LANCE_LEADER_SHORT} is unconscious — took a blow to the head.
>
> The lance looks at you. The other sergeants are busy with their own problems.
>
> Someone has to take charge. Right now.

**Variant B: Camp Crisis**
> It starts with shouting. Then screaming.
>
> Fire in the supply tents. Panic spreading. {LANCE_LEADER_SHORT} is somewhere in the chaos — you can't see them.
>
> {LANCE_NAME} is frozen, waiting for orders. Looking at you.
>
> What do you do?

#### Options

| ID | Text | Result | Character Tag | Effects |
|----|------|--------|---------------|---------|
| `take_command_calm` | Take charge calmly — clear orders, steady voice, prioritize the wounded. | Steady leader | `character: steady` | `+10 lance_rep`, `+30 leadership XP`, promotes |
| `take_command_fierce` | Take charge fiercely — bark orders, move fast, inspire through intensity. | Fierce leader | `character: fierce` | `+8 lance_rep`, `+25 leadership XP`, `+20 athletics XP`, promotes |
| `support_others` | Don't take charge — support whoever steps up, do the critical tasks yourself. | Quiet competence | `character: supportive` | `+12 lance_rep`, `+20 medicine XP`, `+15 leadership XP`, promotes |
| `freeze` | Hesitate. The moment passes. Someone else takes charge. | (Failure) | — | `-10 lance_rep`, promotion delayed 14 days, follow-up event |

#### Outcomes

**Calm Command:**
> Your voice cuts through the chaos. "You — pressure on that wound. You two — clear a path to the surgeon. Move."
>
> They move. Not because you're loud. Because you're certain.
>
> Later, when the crisis passes, {VETERAN_1_NAME} finds you. "Didn't know you had that in you, {PLAYER_RANK}." A pause. "Neither did you, probably."

**Fierce Command:**
> "MOVE!" You're already running, dragging {RECRUIT_NAME} with you. "Get the wounded clear! NOW!"
>
> Your intensity is contagious. Fear becomes motion. The lance responds to your energy like dry kindling to flame.
>
> Later, {SECOND_NAME} approaches. "You scared the shit out of the recruits." They almost smile. "Worked, though."

**Support Others:**
> You don't shout orders. You're too busy doing.
>
> Tourniquet on {WOUNDED_SOLDIER_NAME}. Bucket line to the surgeon. Calm word to the panicking recruit.
>
> When {LANCE_LEADER_SHORT} recovers, they find the crisis handled. "Who took charge?" someone asks. Fingers point at you, even though you never gave an order.

**Freeze (Failure):**
> The moment stretches. You should do something. You know you should.
>
> {SECOND_NAME} pushes past you, shouting orders. The lance responds. The crisis passes.
>
> No one says anything. They don't have to. You saw the look in their eyes.

#### Freeze Follow-up Event: `promotion_t3_t4_passed_over`

Fires 2 days after freeze:

> {LANCE_LEADER_SHORT} calls you aside. Their expression is unreadable.
>
> "You froze." Not an accusation. A fact. "It happens. Happens to everyone at least once."
>
> They meet your eyes. "Question is what you do next."

**Options:**
| ID | Text | Effects |
|----|------|---------|
| `accept_own_it` | "I froze. Won't happen again." | `+5 lance_rep`, `+5 leader_relation`, can re-attempt after 14 days |
| `make_excuses` | "I was assessing the situation..." | `-5 lance_rep`, `-5 leader_relation`, re-attempt after 21 days |
| `ask_for_guidance` | "How do I make sure it doesn't happen again?" | `+8 lance_rep`, `+10 leader_relation`, `+20 leadership XP`, re-attempt after 14 days |

#### Ceremony

> The army makes camp. Wounds are dressed. The dead are counted.
>
> {LORD_NAME}'s adjutant finds you among the tents. "You. {PLAYER_RANK} {PLAYER_NAME}?"
>
> You nod.
>
> "His lordship noticed your conduct during the crisis. You're to be recognized." A formal document is pressed into your hands. "{NEXT_RANK}. Congratulations."
>
> The adjutant leaves. {LANCE_NAME} gathers around, and for the first time, they look at you as someone who might matter.

**Effects:**
- Tier → 4
- Character tag set (affects future event tone)
- Lance reputation +8 to +12
- Lord relation +5
- Recognition from command structure

---

### T4→T5: "Those Who Follow"

**Inspiration:** Battle Brothers — bonds forged in shared experience pay off

**Trigger:** All T4→T5 requirements met

**Concept:** You're being considered for lance leader. But it's not up to you or even the sergeant — it's up to the soldiers who would serve under you. They're asked. What they say determines your fate.

#### Event: `promotion_t4_t5_those_who_follow`

#### Setup Text

> {LANCE_LEADER_SHORT} is being promoted. Moving up to the command staff. That means {LANCE_NAME} needs a new leader.
>
> "It's between you and {RIVAL_NAME}," they tell you. "{COMMANDER_TITLE} is going to ask the lance. What they say matters."
>
> They give you a long look. "I've put in my word. Rest is up to them."
>
> The lance will be asked tomorrow. Today... well. Today is today.

#### Pre-Vote Phase

The player doesn't directly control the vote, but they've been building toward this through all previous events. The vote outcome is determined by:

- Lance reputation (heavily weighted)
- Specific relationships with key NPCs (lance leader, second, veterans)
- Character tag from T3→T4 event
- Any outstanding debts/favors from previous events

#### Vote Resolution

**Success (Lance Rep ≥30, Leader Relation ≥30):**

> They ask the lance one by one. You're not there — wouldn't be proper.
>
> {VETERAN_1_NAME} speaks first. "Solid. Doesn't panic. I'd follow."
>
> {SECOND_NAME}: "{PLAYER_NAME} listens. That's rare."
>
> Even {DIFFICULT_SOLDIER_NAME}, who argues with everyone: "Better than {RIVAL_NAME}. At least {PLAYER_NAME} doesn't think they're better than us."
>
> When {LANCE_LEADER_SHORT} finds you, you already know from their expression. "It's yours. Don't mess it up."

**Close Call (Lance Rep 20-29 or mixed relations):**

> The vote is split. Some for you, some for {RIVAL_NAME}, some abstaining.
>
> {LANCE_LEADER_SHORT} makes the call. "I'm recommending {PLAYER_NAME}. They've earned it."
>
> Not everyone agrees. You can see it in their faces. {RIVAL_NAME} won't meet your eyes.
>
> You'll have to prove the choice was right.

**Failure (Lance Rep <20 or Leader Relation <20):**

> The lance speaks. You hear the verdict in the silence when {LANCE_LEADER_SHORT} returns.
>
> "{RIVAL_NAME} got the nod." They don't apologize. "You've got work to do, {PLAYER_NAME}. With the lance, with yourself."
>
> {RIVAL_NAME} is the new lance leader. And you serve under them now.

#### Success Options

After successful vote:

| ID | Text | Effects |
|----|------|---------|
| `humble_acceptance` | "I'll do my best to be worthy of their trust." | `+10 lance_rep`, `+5 all lance mate relations` |
| `confident_acceptance` | "I won't let them down." | `+5 lance_rep`, `+10 leader_relation` |
| `acknowledge_rival` | Find {RIVAL_NAME} later, acknowledge them, no hard feelings. | `+5 lance_rep`, `+15 rival_relation`, rival becomes ally |

#### Failure Follow-up: `promotion_t4_t5_under_rival`

If the player fails, they now serve under their rival:

> {RIVAL_NAME} calls the lance to formation. Their first act as lance leader.
>
> They look at you. You look at them. The whole lance watches.
>
> "We've got a job to do," {RIVAL_NAME} says. "Personal feelings don't matter. We clear?"
>
> How you respond will define your relationship going forward.

**Options:**
| ID | Text | Effects |
|----|------|---------|
| `loyal_service` | Nod. Fall in. Do your job without complaint. | `+10 rival_relation`, `+5 lance_rep`, faster re-attempt |
| `bitter_compliance` | Fall in. Say nothing. Everyone sees the resentment. | `-5 lance_rep`, `-10 rival_relation`, re-attempt delayed |
| `challenge_authority` | "Clear. Sir." Make it clear this isn't over. | `-15 lance_rep`, `+2 discipline`, creates ongoing conflict |

#### Ceremony (Success)

> The change of command is simple. {LANCE_LEADER_SHORT} — now moving to the command staff — stands you before {LANCE_NAME}.
>
> "This is your lance leader now. {PLAYER_NAME}. {NEXT_RANK}."
>
> They hand you the lance standard. It's heavier than it looks.
>
> "{LANCE_NAME} is yours. Keep them alive."

**Effects:**
- Tier → 5
- Lance leader role (narrative authority in events)
- Lance reputation +5 to +10
- Rival relationship resolved or ongoing
- New leadership events unlock

---

### T5→T6: "The Lord's Eye"

**Inspiration:** Crusader Kings — politics and patronage at the highest level

**Trigger:** All T5→T6 requirements met

**Concept:** You've risen as far as merit alone can take you. Now you need the lord's direct patronage. This is politics, not soldiering. A different game entirely.

#### Event: `promotion_t5_t6_lords_eye`

#### Setup Text

> A page finds you among the tents. "{PLAYER_RANK} {PLAYER_NAME}? His lordship requests your presence."
>
> This isn't a summons to punishment — those come with guards. This is something else.
>
> {LORD_NAME}'s command tent is larger than most houses. Maps on every surface. Officers and nobles coming and going.
>
> And there, at the center, {LORD_TITLE} {LORD_NAME} looks up as you enter.
>
> "Ah. The one {LANCE_LEADER_SHORT} — forgive me, the one the {COMMANDER_TITLE} speaks so highly of." A gesture. "Sit."

#### The Conversation

The lord asks several questions. Your answers shape the outcome:

**Question 1: Background**
> "Tell me about yourself, {PLAYER_NAME}. Before the army."

| ID | Text | Effect |
|----|------|--------|
| `honest_humble` | Tell the truth simply, without embellishment. | `+5 lord_relation` (lords value honesty) |
| `impressive_spin` | Frame your past to sound more impressive. | Risky: Success `+8 lord_relation` / Fail `-5 lord_relation` (lords spot lies) |
| `deflect_service` | "My past matters less than my service, my lord." | `+3 lord_relation`, `+3 lance_rep` |

**Question 2: Loyalty**
> "If I gave you an order you disagreed with — an order you thought was wrong — what would you do?"

| ID | Text | Effect |
|----|------|--------|
| `absolute_loyalty` | "I would obey, my lord. That is a soldier's duty." | `+5 lord_relation`, sets `loyalty_tag: absolute` |
| `voice_then_obey` | "I would voice my concerns privately, then obey." | `+8 lord_relation`, sets `loyalty_tag: counselor` |
| `moral_limits` | "I serve you, my lord, but I won't commit atrocities." | `+3 lord_relation`, `+10 lance_rep`, sets `loyalty_tag: principled` |

**Question 3: Ambition**
> "Where do you see yourself in five years, {PLAYER_RANK}?"

| ID | Text | Effect |
|----|------|--------|
| `serve_faithfully` | "Serving you faithfully, my lord, wherever you need me." | `+5 lord_relation` |
| `rise_higher` | "Rising as high as my abilities allow, my lord." | Risky: Success `+10 lord_relation` / Fail `-3 lord_relation` (too ambitious) |
| `protect_soldiers` | "Keeping my soldiers alive, my lord. That's enough." | `+3 lord_relation`, `+10 lance_rep` |

#### Resolution

Based on accumulated lord_relation through conversation:

**Strong Approval (relation gains ≥15):**
> {LORD_NAME} studies you for a long moment. Then they smile — a rare expression on that weathered face.
>
> "I think we understand each other, {PLAYER_NAME}." They reach for a document — already prepared, you realize. "Welcome to my {NEXT_RANK}. Don't make me regret this."

**Moderate Approval (relation gains 8-14):**
> {LORD_NAME} nods slowly. "You'll do. You're not a courtier, and that's probably for the best."
>
> A signed document slides across the table. "{NEXT_RANK}. You'll be closer to command now. Learn fast."

**Lukewarm (relation gains 3-7):**
> {LORD_NAME}'s expression gives nothing away. "I'm told you're competent. That will have to be enough, for now."
>
> The promotion comes, but it's clear — you're on probation. The lord will be watching.

**Failure (relation gains <3 or critical misstep):**
> {LORD_NAME}'s expression cools. "I see." They set down their cup. "Perhaps in time, {PLAYER_RANK}. For now, return to your duties."
>
> You've been dismissed. The page won't meet your eyes on the way out.

#### Ceremony (Success)

> The promotion is announced at the morning assembly. Not in front of the lance — in front of the army.
>
> {LORD_NAME} speaks your name. "For distinguished service and proven loyalty, {PLAYER_NAME} is elevated to {NEXT_RANK}."
>
> Officers nod. Common soldiers murmur. You feel the weight of hundreds of eyes.
>
> You're no longer just a soldier. You're one of the lord's own.

**Effects:**
- Tier → 6 ({T6_RANK})
- Direct relationship with lord established
- Loyalty tag set (affects future high-level events)
- Lord relation +10 to +20
- Access to command staff interactions
- Peak enlisted rank achieved

---

## Commander Track (T7-T9)

The Commander track is fundamentally different from enlisted service. You're no longer following orders — you're leading troops on the battlefield.

### Overview

| Tier | Rank | Role | Retinue Size | Time to Reach |
|------|------|------|--------------|---------------|
| T7 | {T7_RANK} | Junior Commander | 15 soldiers | Day 378 (~4.5 years) |
| T8 | {T8_RANK} | Company Commander | 25 soldiers | Day 504 (~6 years) |
| T9 | {T9_RANK} | Senior Officer | 35 soldiers | Day 630 (~7.5 years) |

**Note:** Companions do not count toward retinue cap.

### Entering the Commander Track

After reaching T6 ({T6_RANK}), the player doesn't automatically progress. They need:

1. **Lord's approval** — High lord relation (≥30)
2. **Commission event** — Lord offers command
3. **Player choice** — Can decline and stay at T6

Staying at T6 is valid. Not everyone wants command responsibility. The player can remain as a trusted advisor/elite soldier indefinitely.

---

### T6→T7: "The Commission"

**Inspiration:** The moment you stop being a soldier and become an officer.

**Requirements:**

| Requirement | Value | Notes |
|-------------|-------|-------|
| XP | 11,000 | Cumulative |
| Days at T6 | 84 (1 year) | Prove T6 competence |
| Lord relation | ≥30 | Lord must want to promote you |
| Events completed | 70 | Total career |
| Battles survived | 40 | Total career |
| Discipline | <3 | Near-perfect record |

**Event Concept:**

> {LORD_NAME} summons you to their command tent. This isn't a social call.
>
> "You've served well, {PLAYER_NAME}. Better than most. Better than many of my officers, if I'm honest."
>
> They slide a document across the table. A commission. Your name is already written on it.
>
> "I'm offering you command. Fifteen soldiers. Your soldiers. You train them, you lead them, you're responsible for them."
>
> A pause. "It's not an easy path. Some prefer to stay where they are. No shame in that."
>
> "What say you?"

**Options:**

| ID | Text | Result |
|----|------|--------|
| `accept_eager` | "I'm ready, my lord. I won't let you down." | Promotes, `+10 lord_relation` |
| `accept_humble` | "I'll do my best to be worthy of the trust." | Promotes, `+5 lord_relation`, `+10 lance_rep` |
| `decline_not_ready` | "I'm honored, but I don't think I'm ready yet." | Stays T6, can re-attempt in 30 days |
| `decline_prefer_current` | "I serve better where I am, my lord." | Stays T6, flag set `declined_commission` |

**On Accept:**
- Tier → 7
- Retinue system unlocks (15 soldier cap)
- New event category unlocks (Commander events)
- Relationship with lord deepens
- Lance reputation resets (new unit, new relationships)

---

### T7→T8: "The Test of Command"

**Inspiration:** Band of Brothers — your soldiers are tested, your leadership judged.

**Requirements:**

| Requirement | Value | Notes |
|-------------|-------|-------|
| XP | 14,000 | Cumulative |
| Days at T7 | 126 (1.5 years) | Command experience |
| Lord relation | ≥40 | Growing trust |
| Battles commanded | 15 | As retinue leader |
| Soldier survival rate | ≥70% | Leadership effectiveness |
| Retinue average battles | ≥3 | Veteran troops |

**Event Concept:**

> The battle was costly. Your retinue held — barely.
>
> {LORD_NAME} walks the field afterward, surveying the dead. They stop at your position. Your soldiers. Your losses.
>
> "How many?" they ask.
>
> You tell them. The ones who fell. The ones who held. The ones who ran and the ones who stayed.
>
> {LORD_NAME} listens. Then: "A {T8_RANK} needs to make hard choices. Send soldiers to die so others can live. Can you do that, {PLAYER_RANK}?"
>
> The question hangs in the air. This isn't hypothetical.

**Options:**

| ID | Text | Result |
|----|------|--------|
| `yes_duty` | "It's the duty, my lord. I'll bear it." | Promotes, sets `command_style: dutiful` |
| `yes_but_hate` | "I can. I'll hate it every time. But I can." | Promotes, sets `command_style: reluctant`, `+5 soldier_loyalty` |
| `protect_my_soldiers` | "I'll spend their lives if I must, but never waste them." | Promotes, sets `command_style: protective`, `+10 soldier_loyalty` |
| `hesitate` | Hesitate. Say nothing. | Fails, `-10 lord_relation`, retry in 60 days |

**On Promote:**
- Tier → 8
- Retinue cap → 25 soldiers
- Command style tag set (affects future events)
- Recognized as company-level commander

---

### T8→T9: "Council of War"

**Inspiration:** Crusader Kings / Game of Thrones — you're in the room where it happens.

**Requirements:**

| Requirement | Value | Notes |
|-------------|-------|-------|
| XP | 18,000 | Cumulative |
| Days at T8 | 168 (2 years) | Senior experience |
| Lord relation | ≥50 | Trusted advisor |
| Faction leader relation | ≥20 | Kingdom recognition |
| Battles commanded | 30 | Extensive command |
| Campaign victories | 3+ | Strategic success |

**Event Concept:**

> You're summoned to the war council. Not as an observer. As a voice.
>
> {FACTION_LEADER_NAME} presides. The great lords argue. Maps spread across the table, markers representing armies, lives, futures.
>
> {LORD_NAME} speaks: "My {PLAYER_RANK} has thoughts on this matter." All eyes turn to you.
>
> This is it. The moment you become more than a soldier, more than a commander. A player in the game.
>
> What do you say?

**Options:**

| ID | Text | Result |
|----|------|--------|
| `strategic_bold` | Propose a bold strategy — high risk, high reward. | Promotes, sets `strategic_style: bold`, lord/faction reaction varies |
| `strategic_cautious` | Counsel caution — preserve forces, wait for advantage. | Promotes, sets `strategic_style: cautious`, lord/faction reaction varies |
| `support_lord` | Support {LORD_NAME}'s position publicly. | Promotes, `+15 lord_relation`, `-5 faction_leader_relation` |
| `defer` | "I'm a soldier, not a strategist. I'll execute whatever you decide." | Fails, `-5 lord_relation`, "not ready for senior command", retry 90 days |

**On Promote:**
- Tier → 9
- Retinue cap → 35 soldiers
- Strategic style tag set
- Access to war council events
- Peak rank achieved

---

### Commander Track Requirements Table

| Promotion | XP | Days | Battles Cmd | Survival | Lord Rel | Faction Rel | Max Disc | Special |
|-----------|-----|------|-------------|----------|----------|-------------|----------|---------|
| T6→T7 | 11,000 | 84 | — | — | ≥30 | — | <3 | Commission, retinue 15 |
| T7→T8 | 14,000 | 126 | 15 | ≥70% | ≥40 | — | <3 | Command style tag, retinue 25 |
| T8→T9 | 18,000 | 168 | 30 | — | ≥50 | ≥20 | <2 | Strategic style tag, retinue 35 |

---

### Commander-Specific Tracking

New stats tracked at T7+:

| Stat | Description |
|------|-------------|
| `battles_commanded` | Battles where player led retinue |
| `soldiers_recruited` | Total soldiers ever recruited |
| `soldiers_lost` | Total soldiers killed |
| `soldiers_wounded` | Total soldiers wounded |
| `soldier_survival_rate` | % of soldiers who survive battles |
| `retinue_average_veterancy` | Average battles survived by current retinue |
| `campaign_victories` | Major campaign wins |
| `campaign_defeats` | Major campaign losses |

---

### Commander Tags

**Command Style** (set at T7→T8):

| Tag | Description | Effect |
|-----|-------------|--------|
| `dutiful` | Accepts losses as necessary | Neutral soldier loyalty, lord appreciates pragmatism |
| `reluctant` | Hates sending soldiers to die | +5 soldier loyalty, events reflect internal conflict |
| `protective` | Never wastes lives | +10 soldier loyalty, may conflict with lord orders |

**Strategic Style** (set at T8→T9):

| Tag | Description | Effect |
|-----|-------------|--------|
| `bold` | Favors aggressive strategies | Higher risk/reward in command events |
| `cautious` | Favors defensive strategies | Lower casualties, slower campaign progress |

---

## Formation Selection (T2)

### Available Formations by Culture

| Culture | Infantry | Archer | Cavalry | Horse Archer |
|---------|----------|--------|---------|--------------|
| Vlandia | ✅ | ✅ | ✅ | ❌ |
| Sturgia | ✅ | ✅ | ✅ | ❌ |
| Empire | ✅ | ✅ | ✅ | ❌ |
| Battania | ✅ | ✅ | ✅ | ❌ |
| Aserai | ✅ | ✅ | ✅ | ✅ |
| Khuzait | ✅ | ✅ | ✅ | ✅ |

### "Let Them Choose" Weighting

If player selects "Wherever you need me," formation is assigned by army need:

```
weight[formation] = base_weight + shortage_bonus + culture_bonus

Infantry:     base 30 + (infantry_shortage * 10)
Archer:       base 25 + (archer_shortage * 10)
Cavalry:      base 20 + (cavalry_shortage * 10)
Horse Archer: base 15 + (horse_archer_shortage * 10) [if available]
```

The sergeant explains the choice narratively: "We're short on {FORMATION}. That's where you'll go."

---

## Failure and Recovery

### Failure Types

| Tier | Failure Condition | Consequence |
|------|-------------------|-------------|
| T1→T2 | Can't fail (always choose) | — |
| T2→T3 | Risky option fails | +2 Discipline, retry in 7 days |
| T3→T4 | Freeze in crisis | -10 Lance Rep, follow-up event, retry in 14 days |
| T4→T5 | Lance votes against you | Serve under rival, retry in 30 days |
| T5→T6 | Lord not impressed | Retry in 30 days, need +20 lord relation |

### Recovery Events

Each failure can trigger a follow-up event that gives the player a chance to respond and potentially shorten the retry timer:

| Failure | Follow-up Event | Good Response | Bad Response |
|---------|-----------------|---------------|--------------|
| T2→T3 fail | "Learning from Failure" | Own mistake, ask guidance (14 day retry → 7) | Make excuses (7 day retry → 14) |
| T3→T4 freeze | "Passed Over" | Accept, grow from it (14 → 10) | Bitter, blame others (14 → 21) |
| T4→T5 lost vote | "Under Your Rival" | Loyal service (30 → 20) | Challenge authority (30 → 45) |
| T5→T6 dismissed | "Prove Your Worth" | Accomplish visible deed (30 → 20) | Sulk, avoid lord (30 → 45) |

---

## Rank Placeholders

The following placeholders are used in promotion events and resolve based on the player's culture:

| Placeholder | Description | Example (Empire T3) |
|-------------|-------------|---------------------|
| `{PLAYER_RANK}` | Current rank title | "Immunes" |
| `{NEXT_RANK}` | Next tier's rank title | "Principalis" |
| `{T1_RANK}` - `{T9_RANK}` | Specific tier rank | Varies by culture |
| `{COMMANDER_TITLE}` | Generic commander reference | Culture-appropriate officer title |

### Resolution Logic

```csharp
public string ResolveRankPlaceholder(string placeholder, string cultureId, int currentTier)
{
    return placeholder switch
    {
        "{PLAYER_RANK}" => GetRankTitle(currentTier, cultureId),
        "{NEXT_RANK}" => GetRankTitle(currentTier + 1, cultureId),
        "{T1_RANK}" => GetRankTitle(1, cultureId),
        "{T2_RANK}" => GetRankTitle(2, cultureId),
        // ... etc
        "{COMMANDER_TITLE}" => GetCommanderTitle(cultureId),
        _ => placeholder
    };
}

private string GetCommanderTitle(string cultureId)
{
    // Returns appropriate officer title for culture
    return cultureId switch
    {
        "empire" => "Tribune",
        "vlandia" => "Captain",
        "sturgia" => "Thane",
        "khuzait" => "Noyan",
        "battania" => "Clan Chief",
        "aserai" => "Sheikh",
        _ => "Captain"
    };
}
```

---

## Implementation Notes

### Eligibility Tracking

```csharp
public class PromotionEligibility
{
    public int CurrentTier;
    public int XP;
    public int DaysInRank;
    public int EventsCompleted;
    public int BattlesSurvived;
    public int LanceReputation;
    public int Discipline;
    public int LanceLeaderRelation;
    public int LordRelation;
    
    public bool IsEligibleFor(int targetTier);
    public List<string> GetBlockingReasons(int targetTier);
}
```

### Proving Event Priority

Proving events fire at `priority: critical` — they take precedence over all other automatic events except system events (discharge, capture, etc.).

### Character Tags

The T3→T4 event sets a character tag that affects future event tone:

| Tag | Set By | Effect on Future Events |
|-----|--------|------------------------|
| `character: steady` | Calm command in crisis | Events offer calm/methodical options |
| `character: fierce` | Fierce command in crisis | Events offer bold/aggressive options |
| `character: supportive` | Supported others in crisis | Events offer team-focused options |

### Loyalty Tags

The T5→T6 event sets a loyalty tag that affects high-level events:

| Tag | Set By | Effect |
|-----|--------|--------|
| `loyalty: absolute` | "I would obey without question" | Lord trusts you with morally gray orders |
| `loyalty: counselor` | "I would voice concerns, then obey" | Lord asks your opinion before decisions |
| `loyalty: principled` | "I have moral limits" | Lord respects you but may exclude you from certain orders |

### Persistence

All promotion-related state persists via `IDataStore.SyncData`:

- Current eligibility values
- Retry cooldowns (timestamp)
- Character tag (string)
- Loyalty tag (string)
- Rival relationship (if applicable)
- Vote outcome history

---

## JSON Schema Reference

See `lance_life_schemas.md` for full schema. Key additions for promotion events:

```json
{
  "metadata": {
    "from_tier": 1,
    "to_tier": 2
  },
  "content": {
    "options": [
      {
        "effects": {
          "formation": "infantry",
          "character_tag": "steady",
          "loyalty_tag": "counselor"
        }
      }
    ]
  }
}
```

---

*Document version: 1.0*
*For use with: Lance Life Event System, Tiers 1-6*
