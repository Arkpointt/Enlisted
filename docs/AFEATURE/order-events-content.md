# Order Events Content Catalog

**Summary:** Complete catalog of events that fire during order execution. Events are tied to specific order types and fire during "slot" phases based on world state. Each event includes setup text, options with skill checks, and outcomes.

**Status:** 📋 Specification  
**Last Updated:** 2025-12-24  
**Related Docs:** [Order Progression System](order-progression-system.md), [Orders Content](orders-content.md), [Content Orchestrator](content-orchestrator-plan.md)

---

## Index

1. [Overview](#overview)
2. [Event Schema](#event-schema)
3. [Guard & Sentry Events](#guard--sentry-events)
4. [Patrol Events](#patrol-events)
5. [Labor Events](#labor-events)
6. [Scout Events](#scout-events)
7. [Medical Events](#medical-events)
8. [Leadership Events](#leadership-events)
9. [Craft & Repair Events](#craft--repair-events)
10. [Escort Events](#escort-events)
11. [Implementation Notes](#implementation-notes)

---

## Overview

Order events fire during "slot" phases when the player is on duty. They are contextual to the order type - guard duty events are different from scout events.

### Event Pool Summary

| Order Type | Event Count | Focus |
|------------|-------------|-------|
| Guard/Sentry | 11 events | Vigilance, discipline, night dangers |
| Patrol | 12 events | Movement, soldiers, terrain |
| Labor (Firewood, Latrine, Cleaning) | 9 events | Physical work, social |
| Scout | 8 events | Recon, enemy contact, terrain |
| Medical | 6 events | Treatment, patients, infection |
| Leadership (Lead Patrol, Train) | 13 events | Command, morale, tactics |
| Craft/Repair | 5 events | Materials, quality, discovery |
| Escort | 6 events | Danger, cargo, VIPs |
| **Total** | **70 events** | |

### World State Weighting

Events are weighted by current activity level:

| Activity Level | Effect |
|----------------|--------|
| Quiet (garrison) | Social/boredom events weighted higher |
| Routine (peacetime) | Balanced pool |
| Active (campaign) | Danger/opportunity events weighted higher |
| Intense (siege) | Combat/crisis events weighted higher |

---

## Event Schema

Each order event follows this JSON structure:

```json
{
  "id": "guard_strange_noise",
  "order_types": ["order_guard_post", "order_sentry_duty"],
  
  "titleId": "order_evt_guard_noise_title",
  "title": "Strange Noise",
  
  "setupId": "order_evt_guard_noise_setup",
  "setup": "A sound in the darkness. Footsteps? An animal? Your hand moves to your weapon.",
  
  "requirements": {
    "phase": ["Night", "Dusk"],
    "world_state": null
  },
  
  "weight": {
    "base": 1.0,
    "quiet": 0.5,
    "routine": 1.0,
    "active": 1.5,
    "intense": 2.0
  },
  
  "options": [
    {
      "id": "investigate",
      "textId": "order_evt_guard_noise_opt_investigate",
      "text": "Investigate quietly.",
      "tooltip": "Perception check. Success: identify source. Failure: waste time, +fatigue.",
      "skill_check": { "skill": "Perception", "difficulty": 40 },
      "success": {
        "order_progress": 5,
        "skill_xp": { "Perception": 15 },
        "resultTextId": "order_evt_guard_noise_investigate_success",
        "resultText": "A stray dog, hunting rats. You return to your post."
      },
      "failure": {
        "fatigue": 8,
        "resultTextId": "order_evt_guard_noise_investigate_fail",
        "resultText": "You search for an hour. Nothing. Just shadows and nerves."
      }
    },
    {
      "id": "challenge",
      "textId": "order_evt_guard_noise_opt_challenge",
      "text": "Call out a challenge.",
      "tooltip": "Safe choice. Reveals presence but establishes authority.",
      "effects": {
        "reputation": { "officer": 2 },
        "resultTextId": "order_evt_guard_noise_challenge_result",
        "resultText": "'Who goes there!' Silence. Then a sheepish soldier emerges. Late to his tent."
      }
    },
    {
      "id": "ignore",
      "textId": "order_evt_guard_noise_opt_ignore",
      "text": "Ignore it. Probably nothing.",
      "tooltip": "Risky. 20% chance something was actually there.",
      "risk_roll": {
        "chance": 0.20,
        "failure": {
          "reputation": { "officer": -8 },
          "company_needs": { "Readiness": -5 },
          "resultTextId": "order_evt_guard_noise_ignore_bad",
          "resultText": "Morning comes. A supply crate is missing. You said nothing."
        },
        "success": {
          "resultTextId": "order_evt_guard_noise_ignore_ok",
          "resultText": "Dawn breaks. Nothing happened. Probably was nothing."
        }
      }
    }
  ]
}
```

---

## Guard & Sentry Events

### guard_drunk_soldier

**Drunk Soldier** - An intoxicated soldier stumbles into your post.

| Property | Value |
|----------|-------|
| Order Types | Guard Post, Sentry Duty |
| Phase | Night, Dusk |
| Weight | Higher in Quiet/Routine |

**Options:**
1. **Help him to his tent** (Charm check)
   - Success: +3 Soldier Rep, he owes you
   - Failure: He vomits on you, +5 fatigue
   
2. **Report him to the sergeant**
   - +5 Officer Rep, -5 Soldier Rep
   
3. **Ignore him**
   - No effect, but 15% chance he causes trouble (Readiness -3)

---

### guard_strange_noise

**Strange Noise** - Something stirs in the darkness.

| Property | Value |
|----------|-------|
| Order Types | Guard Post, Sentry Duty |
| Phase | Night, Dusk |
| Weight | Higher in Active/Intense |

**Options:**
1. **Investigate quietly** (Perception 40)
   - Success: Identify source, +15 Perception XP
   - Failure: Waste time, +8 fatigue
   
2. **Call out a challenge**
   - Safe, +2 Officer Rep
   
3. **Ignore it**
   - 20% chance something was there → -8 Officer Rep, -5 Readiness

---

### guard_officer_inspection

**Officer Inspection** - An officer approaches to check your post.

| Property | Value |
|----------|-------|
| Order Types | Guard Post, Sentry Duty, Muster Inspection |
| Phase | Any |
| Weight | Higher when High Scrutiny |

**Options:**
1. **Snap to attention, perfect report** (Discipline check)
   - Success: +5 Officer Rep, +3 Lord Rep
   - Failure: +2 Officer Rep (tried at least)
   
2. **Standard response**
   - +2 Officer Rep
   
3. **Fumble nervously**
   - -3 Officer Rep, +5 Scrutiny

---

### guard_caught_sneaking

**Caught Sneaking** - You spot someone moving where they shouldn't be.

| Property | Value |
|----------|-------|
| Order Types | Guard Post, Sentry Duty |
| Phase | Night |
| Weight | Higher in Intense |

**Options:**
1. **Intercept and challenge** (Perception 50)
   - Success: Caught! +8 Officer Rep, +10 Lord Rep (if spy/saboteur)
   - Failure: They escape, -5 Officer Rep
   
2. **Raise the alarm**
   - +5 Officer Rep, safe choice
   - But if false alarm: -3 Officer Rep
   
3. **Observe first** (Scouting 60)
   - Success: Gather intel, then decide
   - Failure: Lose them, -3 Officer Rep

---

### guard_relieved_early

**Relieved Early** - Your replacement arrives ahead of schedule.

| Property | Value |
|----------|-------|
| Order Types | Guard Post, Sentry Duty |
| Phase | Dawn, Midday |
| Weight | Higher in Quiet |

**Effects:**
- -5 Fatigue (lucky break)
- +3 Rest
- "The sergeant's feeling generous. Rare."

---

### guard_double_shift

**Double Shift** - Your replacement is late. Very late.

| Property | Value |
|----------|-------|
| Order Types | Guard Post, Sentry Duty |
| Phase | Night, Dusk |
| Weight | Higher in Active/Intense |

**Options:**
1. **Wait patiently**
   - +15 Fatigue, +3 Officer Rep (noticed your dedication)
   
2. **Send word to the sergeant**
   - +8 Fatigue, replacement found
   
3. **Abandon post to find them**
   - -10 Officer Rep, order may fail (dereliction)

---

### sentry_movement_spotted

**Movement Spotted** - Definite movement at the edge of visibility.

| Property | Value |
|----------|-------|
| Order Types | Sentry Duty |
| Phase | Night, Dusk |
| Weight | Higher in Active/Intense |

**Options:**
1. **Identify before acting** (Perception 60)
   - Success: Correct identification, appropriate response
   - Failure: Waste time, 30% it was enemy
   
2. **Immediate alarm**
   - If enemy: +10 Officer Rep, +5 Lord Rep
   - If friendly: -5 Officer Rep (false alarm)
   
3. **Ready weapon and wait**
   - Balanced approach, +2 Officer Rep

---

### sentry_fell_asleep

**Fell Asleep** - Exhaustion claims you. You wake with a start.

| Property | Value |
|----------|-------|
| Order Types | Sentry Duty, Guard Post |
| Phase | Night |
| Requirements | Fatigue > 60 |
| Weight | Higher when fatigued |

**Automatic trigger if fatigue high. Not a choice event.**

**Outcomes:**
- 50% caught: -15 Officer Rep, +10 Scrutiny, +10 Discipline
- 50% not caught: Just guilt, +5 Scrutiny (suspicious behavior)

---

### sentry_challenged_approach

**Approaching Figure** - Someone approaches your position.

| Property | Value |
|----------|-------|
| Order Types | Sentry Duty |
| Phase | Any |
| Weight | Balanced |

**Options:**
1. **Formal challenge by the book**
   - +3 Officer Rep
   - Reveals: Officer (impressed), Soldier (annoyed), Enemy (combat)
   
2. **Casual greeting**
   - If officer: -5 Officer Rep (improper)
   - If soldier: +2 Soldier Rep
   
3. **Ready weapon, wait for them to speak**
   - Neutral, works for all outcomes

---

### sentry_relief_late

**Late Relief** - Your shift should be over. No one's coming.

| Property | Value |
|----------|-------|
| Order Types | Sentry Duty |
| Phase | Dawn |
| Weight | Balanced |

**Effects:**
- +10 Fatigue
- Order extends by 1 phase
- "Typical. Always the last to be relieved."

---

### sentry_heard_something

**Heard Something** - A sound. Not footsteps. Something else.

| Property | Value |
|----------|-------|
| Order Types | Sentry Duty, Guard Post |
| Phase | Night |
| Weight | Higher in Active |

**Options:**
1. **Identify the sound** (Perception 45)
   - Success: Correctly identify (animal, wind, enemy)
   - Failure: Uncertain, must decide blind
   
2. **Defensive position and wait**
   - +2 Officer Rep, safe choice
   
3. **Ignore - wind probably**
   - 25% it was enemy scouts → -10 Officer Rep

---

## Patrol Events

### patrol_lost_item

**Lost Item** - A soldier reports missing gear along the patrol route.

| Property | Value |
|----------|-------|
| Order Types | Camp Patrol |
| Phase | Any |
| Weight | Balanced |

**Options:**
1. **Help search** (Perception 35)
   - Success: Found it, +5 Soldier Rep
   - Failure: Time wasted, +5 fatigue
   
2. **Report to quartermaster**
   - Neutral, soldier handles it himself
   
3. **Ignore - not your problem**
   - -3 Soldier Rep

---

### patrol_soldier_argument

**Soldier Argument** - Two soldiers are about to come to blows.

| Property | Value |
|----------|-------|
| Order Types | Camp Patrol, Lead Patrol |
| Phase | Any |
| Weight | Higher when Low Morale |

**Options:**
1. **Break it up with authority** (Leadership 40)
   - Success: +5 Officer Rep, +3 Morale
   - Failure: They don't listen, must report
   
2. **Report to sergeant immediately**
   - +3 Officer Rep, sergeant handles it
   
3. **Let them fight it out**
   - -5 Morale, 30% someone gets hurt

---

### patrol_suspicious_activity

**Suspicious Activity** - Something's not right here.

| Property | Value |
|----------|-------|
| Order Types | Camp Patrol |
| Phase | Night, Dusk |
| Weight | Higher in Active/Intense |

**Options:**
1. **Investigate thoroughly** (Perception 50)
   - Success: Found contraband/sabotage, +10 Officer Rep
   - Failure: Nothing found, just paranoid
   
2. **Report observations**
   - +3 Officer Rep, someone else investigates
   
3. **Note and continue**
   - Neutral, but 20% it was real → missed opportunity

---

### patrol_officer_tags_along

**Officer Tags Along** - A junior officer joins your patrol "for exercise."

| Property | Value |
|----------|-------|
| Order Types | Camp Patrol |
| Phase | Dawn, Midday |
| Weight | Higher when High Scrutiny |

**Effects:**
- +5 Scrutiny for this patrol
- Extra pressure on all skill checks (-10%)
- If patrol goes well: +5 Lord Rep
- If patrol has problems: +10 Scrutiny

---

### patrol_shortcut

**Shortcut** - You know a faster route, but it's against regulations.

| Property | Value |
|----------|-------|
| Order Types | Camp Patrol, Lead Patrol |
| Phase | Midday |
| Weight | Balanced |

**Options:**
1. **Take the shortcut** (Athletics 40)
   - Success: -3 fatigue, faster completion
   - Failure: Get stuck, +5 fatigue
   
2. **Follow the assigned route**
   - Standard completion, no bonus or penalty
   
3. **Ask patrol members' opinion**
   - +2 Soldier Rep (consultative), then decide

---

### patrol_twisted_ankle

**Twisted Ankle** - Rough ground. Your foot turns wrong.

| Property | Value |
|----------|-------|
| Order Types | Camp Patrol, Lead Patrol, March Formation |
| Phase | Any |
| Weight | Higher when fatigued |

**Options:**
1. **Walk it off** (Athletics 45)
   - Success: Minor pain, continue
   - Failure: -10 HP, +10 fatigue, limp for order duration
   
2. **Report injury, request replacement**
   - Order ends early (Partial outcome)
   - +5 Medical Risk
   
3. **Grit teeth and continue silently**
   - +15 fatigue, -5 HP, +3 Discipline XP

---

## Labor Events

### firewood_axe_slip

**Axe Slip** - The wood is wet. Your swing goes wrong.

| Property | Value |
|----------|-------|
| Order Types | Firewood Detail |
| Phase | Midday, Dusk |
| Weight | Higher when fatigued |

**Options:**
1. **React fast** (Athletics 50)
   - Success: Close call, no harm
   - Failure: -15 HP, +10 Medical Risk
   
2. **Accept the hit**
   - -10 HP, minor wound, continue

---

### firewood_found_something

**Found Something** - Buried under leaves. Something metal.

| Property | Value |
|----------|-------|
| Order Types | Firewood Detail, Forage Supplies |
| Phase | Any |
| Weight | Balanced |

**Options:**
1. **Investigate carefully** (Perception 40)
   - Success: Old cache - find coins (25g) or weapon
   - Failure: Rusty trap, -5 HP
   
2. **Report and leave it**
   - +3 Officer Rep, someone else handles it
   
3. **Mark and continue - deal with later**
   - Neutral, chance to return after order

---

### firewood_wildlife

**Wildlife Encounter** - An animal. Not aggressive yet.

| Property | Value |
|----------|-------|
| Order Types | Firewood Detail, Forage Supplies, Scout Route |
| Phase | Any |
| Weight | Balanced |

**Options:**
1. **Slowly back away** (Scouting 35)
   - Success: Animal leaves, continue work
   - Failure: Startled, animal charges → combat or flee
   
2. **Scare it off aggressively**
   - 70% works, 30% it charges
   
3. **Try to hunt it** (Combat trigger)
   - If successful: +Supplies, +10 Soldier Rep (extra meat)

---

### firewood_work_song

**Work Song** - Someone starts a marching song. Others join.

| Property | Value |
|----------|-------|
| Order Types | Firewood Detail |
| Phase | Midday |
| Weight | Higher in Quiet/Routine |

**Options:**
1. **Join in enthusiastically**
   - +5 Soldier Rep, +5 Morale, -5 fatigue
   
2. **Hum along quietly**
   - +2 Soldier Rep
   
3. **Keep working silently**
   - Neutral, but seen as aloof

---

### firewood_competition

**Chopping Competition** - Who can split the most wood?

| Property | Value |
|----------|-------|
| Order Types | Firewood Detail |
| Phase | Midday |
| Weight | Balanced |

**Options:**
1. **Accept the challenge** (Athletics 55)
   - Success: +8 Soldier Rep, +20 Athletics XP, +10 fatigue
   - Failure: +5 fatigue, no rep
   
2. **Decline - conserve energy**
   - Neutral, seen as practical
   
3. **Suggest a different competition**
   - +3 Soldier Rep (good sport)

---

### latrine_overheard_rumor

**Overheard Rumor** - Soldiers talk freely here. You hear something interesting.

| Property | Value |
|----------|-------|
| Order Types | Latrine Duty |
| Phase | Any |
| Weight | Balanced |

**Effects:**
- Learn one piece of information (random):
  - Upcoming battle plans
  - Officer having troubles
  - Supply problems
  - Soldier desertion plans
- Can act on this later (flag set)

---

### latrine_officer_complains

**Officer Complains** - An officer finds fault with your work.

| Property | Value |
|----------|-------|
| Order Types | Latrine Duty |
| Phase | Midday, Dusk |
| Weight | Higher when High Scrutiny |

**Options:**
1. **Accept criticism, work harder**
   - +3 Discipline XP, +5 fatigue
   
2. **Politely explain situation**
   - 50% accepted, 50% makes it worse
   
3. **Stay silent**
   - -3 Officer Rep (seen as surly)

---

### latrine_soldier_gratitude

**Soldier's Gratitude** - "Thanks for doing this duty. Most avoid it."

| Property | Value |
|----------|-------|
| Order Types | Latrine Duty |
| Phase | Any |
| Weight | Balanced |

**Effects:**
- +5 Soldier Rep
- +3 Morale (personal)
- "Grunt work, but someone's got to do it."

---

### cleaning_found_damage

**Found Damage** - This gear has been sabotaged. Deliberately.

| Property | Value |
|----------|-------|
| Order Types | Equipment Cleaning, Repair Equipment |
| Phase | Any |
| Weight | Higher in Intense |

**Options:**
1. **Report immediately**
   - +8 Officer Rep, investigation begins
   
2. **Quietly repair it** (Crafting 55)
   - Success: Fixed, no fuss
   - Failure: Damage worse, must report
   
3. **Investigate yourself first** (Perception 60)
   - Success: Identify saboteur, +10 Lord Rep
   - Failure: Trail goes cold

---

### cleaning_contraband_found

**Contraband Found** - Someone's hidden something in this equipment.

| Property | Value |
|----------|-------|
| Order Types | Equipment Cleaning |
| Phase | Any |
| Weight | Balanced |

**Options:**
1. **Report to sergeant**
   - +5 Officer Rep, owner punished
   - -5 Soldier Rep (snitch)
   
2. **Put it back, say nothing**
   - Neutral, but you know something
   
3. **Confront the owner privately**
   - They owe you a favor OR +10 Scrutiny (if they're connected)

---

## Scout Events

### scout_tracks_found

**Tracks Found** - Fresh tracks. Something passed this way recently.

| Property | Value |
|----------|-------|
| Order Types | Scout Route |
| Phase | Any |
| Weight | Higher in Active |

**Options:**
1. **Analyze thoroughly** (Scouting 55)
   - Success: Identify enemy movement, +15 Scouting XP, +order progress
   - Failure: Misread them, potential bad intel
   
2. **Mark location, continue route**
   - Standard intel, +5 Scouting XP
   
3. **Follow them** (Scouting 70)
   - Success: Find enemy position, +25 Scouting XP, +10 Lord Rep
   - Failure: Walk into danger

---

### scout_enemy_patrol

**Enemy Patrol** - Movement ahead. Enemy soldiers.

| Property | Value |
|----------|-------|
| Order Types | Scout Route |
| Phase | Any |
| World State | War, Campaign, Siege |
| Weight | Higher in Active/Intense |

**Options:**
1. **Go to ground, let them pass** (Scouting 50)
   - Success: Evade, continue mission
   - Failure: Spotted, must flee
   
2. **Follow and observe** (Scouting 70)
   - Success: Intel on patrol route, +10 Lord Rep
   - Failure: Detected, arrow clips arm (-15 HP)
   
3. **Withdraw and report**
   - Safe, order ends early (Partial)

---

### scout_terrain_obstacle

**Terrain Obstacle** - The planned route is blocked. Landslide? Flooding?

| Property | Value |
|----------|-------|
| Order Types | Scout Route, Escort Duty |
| Phase | Any |
| Weight | Balanced |

**Options:**
1. **Find alternate route** (Scouting 50)
   - Success: Route found, continue
   - Failure: Lost time, +10 fatigue
   
2. **Push through anyway** (Athletics 60)
   - Success: Made it, +5 Athletics XP
   - Failure: Minor injury, -10 HP
   
3. **Report obstacle, return**
   - Order ends early (Partial), but useful intel

---

### scout_injured_ankle

**Injured Ankle** - Rough terrain claims its toll.

| Property | Value |
|----------|-------|
| Order Types | Scout Route |
| Phase | Any |
| Weight | Higher when fatigued |

**Options:**
1. **Push through the pain** (Athletics 55)
   - Success: Continue mission, +10 fatigue
   - Failure: Worse injury, -20 HP, order at risk
   
2. **Rest briefly, then continue**
   - +15 fatigue, mission continues
   
3. **Abort mission**
   - Order fails, -5 Officer Rep, but safe

---

### scout_shortcut_found

**Shortcut Discovered** - A faster route the army could use.

| Property | Value |
|----------|-------|
| Order Types | Scout Route |
| Phase | Any |
| Weight | Balanced |

**Options:**
1. **Verify thoroughly** (Scouting 60)
   - Success: Confirmed safe route, +15 Lord Rep, +order bonus
   - Failure: Missed hidden danger, route used → problems later
   
2. **Note for verification by others**
   - +5 Lord Rep, someone else checks
   
3. **Ignore - stick to mission**
   - Neutral, focused approach

---

### scout_lost_trail

**Lost the Trail** - The terrain confused you. Which way now?

| Property | Value |
|----------|-------|
| Order Types | Scout Route |
| Phase | Any |
| Weight | Balanced |

**Options:**
1. **Backtrack and reorient** (Scouting 45)
   - Success: Found again, continue
   - Failure: More time lost, +15 fatigue
   
2. **Climb for better view** (Athletics 50)
   - Success: Spotted landmarks
   - Failure: Dangerous climb, 20% fall injury
   
3. **Return, report difficulty**
   - Order ends (Partial), honest about limits

---

### scout_ambush

**Ambush!** - Arrows fly. You've been spotted.

| Property | Value |
|----------|-------|
| Order Types | Scout Route, Lead Patrol, Escort Duty |
| Phase | Any |
| World State | War, Campaign, Siege |
| Weight | Higher in Intense |

**This is a CRITICAL event - high stakes.**

**Options:**
1. **Fight through** (Combat trigger)
   - Victory: Escape, +20 Renown, -HP
   - Defeat: Captured, order fails
   
2. **Flee immediately** (Athletics 60)
   - Success: Escape with intel
   - Failure: Hit while running, -25 HP
   
3. **Surrender**
   - Captured, order fails, prisoner event chain

---

### scout_enemy_camp

**Enemy Camp** - From the ridge, you see their encampment.

| Property | Value |
|----------|-------|
| Order Types | Scout Route |
| Phase | Dawn, Midday |
| World State | War, Campaign, Siege |
| Weight | Higher in Active |

**Options:**
1. **Detailed observation** (Scouting 65)
   - Success: Full intel - strength, dispositions, leaders. +20 Lord Rep
   - Failure: Spotted by patrol, must flee
   
2. **Quick count and leave**
   - +10 Lord Rep, basic intel, safe
   
3. **Mark location, withdraw**
   - +5 Lord Rep, minimal intel but safe

---

## Medical Events

### treat_shortage

**Supply Shortage** - Not enough bandages. Must improvise.

| Property | Value |
|----------|-------|
| Order Types | Treat Wounded |
| Phase | Any |
| Weight | Higher when Low Supplies |

**Options:**
1. **Improvise with what's available** (Medicine 55)
   - Success: Patient saved, +15 Medicine XP
   - Failure: Infection risk, patient worsens
   
2. **Request emergency supplies**
   - Delay treatment, but proper materials
   - -3 Supplies
   
3. **Prioritize critical patients**
   - Triage, some may not make it

---

### treat_difficult_case

**Difficult Case** - This wound is beyond normal treatment.

| Property | Value |
|----------|-------|
| Order Types | Treat Wounded |
| Phase | Any |
| Weight | Higher after battles |

**Options:**
1. **Attempt risky treatment** (Medicine 70)
   - Success: Patient lives, +10 Soldier Rep, +25 Medicine XP
   - Failure: Patient dies, -5 Morale
   
2. **Request experienced surgeon**
   - Delay, but patient has better chance
   
3. **Comfort care only**
   - Patient dies, but peacefully, neutral rep

---

### treat_infection_risk

**Infection Spreading** - Wounds are festering. Could spread.

| Property | Value |
|----------|-------|
| Order Types | Treat Wounded |
| Phase | Any |
| Weight | Higher when Low Supplies |

**Options:**
1. **Aggressive treatment** (Medicine 60)
   - Success: Infection stopped
   - Failure: Spreads to you (+15 Medical Risk)
   
2. **Isolate affected patients**
   - Stops spread, but harder to treat
   
3. **Request full quarantine**
   - +5 Officer Rep (initiative), major response

---

### treat_grateful_soldier

**Grateful Patient** - A soldier you treated recovers well.

| Property | Value |
|----------|-------|
| Order Types | Treat Wounded |
| Phase | Any |
| Weight | Balanced |

**Effects:**
- +8 Soldier Rep
- "He'll remember what you did."
- May appear in future events as ally

---

### treat_officer_wounded

**Officer Wounded** - A lieutenant needs treatment. High stakes.

| Property | Value |
|----------|-------|
| Order Types | Treat Wounded |
| Phase | Any |
| Weight | Balanced |

**Options:**
1. **Personal treatment** (Medicine 65)
   - Success: +10 Officer Rep, +8 Lord Rep
   - Failure: -10 Officer Rep, -10 Lord Rep
   
2. **Request senior healer**
   - Safer, but you miss opportunity
   
3. **Treat as you would any soldier**
   - +5 Soldier Rep, officer may be offended

---

### treat_contagion

**Caught Something** - Working with the sick. Now you feel ill.

| Property | Value |
|----------|-------|
| Order Types | Treat Wounded |
| Phase | Any |
| Requirements | Multiple treatment events |
| Weight | Balanced |

**Effects:**
- +15 Medical Risk
- +10 Fatigue
- Triggers Medical Progression System
- "The price of the work."

---

## Leadership Events

### patrol_lead_soldier_behind

**Soldier Falling Behind** - One of your patrol can't keep pace.

| Property | Value |
|----------|-------|
| Order Types | Lead Patrol, March Formation |
| Phase | Any |
| Weight | Higher when fatigued |

**Options:**
1. **Slow the patrol for them** (Leadership 45)
   - Success: +5 Soldier Rep, patrol continues together
   - Failure: Patrol falls behind schedule
   
2. **Send them back alone**
   - Risky for them, but patrol continues
   - 20% they get lost or hurt
   
3. **Redistribute their load**
   - +3 Soldier Rep, others share burden

---

### patrol_lead_route_dispute

**Route Dispute** - A soldier insists you're going the wrong way.

| Property | Value |
|----------|-------|
| Order Types | Lead Patrol |
| Phase | Any |
| Weight | Balanced |

**Options:**
1. **Assert your authority** (Leadership 50)
   - Success: They follow, +3 Officer Rep
   - Failure: Grumbling, -3 Morale
   
2. **Consider their input** (Tactics 45)
   - Success: Better route found, +5 Soldier Rep
   - Failure: Their way was wrong, +3 Officer Rep (yours was right)
   
3. **Let them lead that section**
   - Delegation, but risky if they're wrong

---

### patrol_lead_enemy_contact

**Enemy Contact** - Your patrol spots enemy soldiers.

| Property | Value |
|----------|-------|
| Order Types | Lead Patrol |
| Phase | Any |
| World State | War, Campaign |
| Weight | Higher in Active/Intense |

**Options:**
1. **Engage** (Tactics 55)
   - Success: Victory, prisoners, +15 Lord Rep
   - Failure: Losses, retreat, -10 Officer Rep
   
2. **Evade and report** (Scouting 50)
   - Success: Intel delivered, +8 Lord Rep
   - Failure: Spotted while evading, forced fight
   
3. **Hold position, send runner**
   - Safe, but slow response
   - +5 Officer Rep (following protocol)

---

### patrol_lead_morale_drop

**Morale Dropping** - The patrol is tired, scared, or discouraged.

| Property | Value |
|----------|-------|
| Order Types | Lead Patrol |
| Phase | Dusk, Night |
| Weight | Higher when Low Morale |

**Options:**
1. **Rally them with words** (Leadership 55)
   - Success: +5 Morale, continue strong
   - Failure: Words ring hollow, no effect
   
2. **Lead by example** (Athletics 50)
   - Success: They follow your energy, +3 Morale
   - Failure: You're tired too, everyone struggles
   
3. **Rest briefly**
   - +10 Fatigue for patrol, but Morale stabilizes

---

### patrol_lead_man_injured

**Man Injured** - Someone's hurt. Twisted ankle, bad fall.

| Property | Value |
|----------|-------|
| Order Types | Lead Patrol |
| Phase | Any |
| Weight | Balanced |

**Options:**
1. **Field treatment** (Medicine 45)
   - Success: Patched up, patrol continues
   - Failure: Worse injury, must carry them
   
2. **Send them back with escort**
   - Patrol continues, minus two people
   
3. **Abort patrol, everyone returns**
   - Order ends early (Partial), but everyone safe

---

### patrol_lead_ambush

**Ambush!** - Your patrol walks into a trap.

| Property | Value |
|----------|-------|
| Order Types | Lead Patrol |
| Phase | Any |
| World State | War, Campaign |
| Weight | Higher in Intense |

**CRITICAL EVENT - Combat or crisis**

**Options:**
1. **Counter-attack** (Tactics 60 + Combat)
   - Success: Break ambush, +15 Lord Rep
   - Failure: Losses, forced retreat
   
2. **Fighting retreat** (Tactics 55)
   - Success: Most escape, some losses
   - Failure: Heavy losses
   
3. **Scatter and regroup**
   - Everyone runs, 70% make it back, patrol fails

---

### patrol_lead_good_find

**Good Find** - Your patrol discovers something valuable.

| Property | Value |
|----------|-------|
| Order Types | Lead Patrol, Scout Route |
| Phase | Any |
| Weight | Balanced |

**Outcomes (random):**
- Abandoned supplies: +15 Supplies
- Hidden cache: +50g reward
- Enemy intelligence: +15 Lord Rep
- Shortcut: Future benefit

---

### train_difficult_recruit

**Difficult Recruit** - This one doesn't listen. Attitude problem.

| Property | Value |
|----------|-------|
| Order Types | Train Recruits |
| Phase | Any |
| Weight | Balanced |

**Options:**
1. **Firm discipline** (Leadership 55)
   - Success: Falls in line, +5 Officer Rep
   - Failure: Resentment, -3 Soldier Rep
   
2. **Patient instruction** (Charm 50)
   - Success: Comes around, +5 Soldier Rep
   - Failure: Seen as soft
   
3. **Report to sergeant for discipline**
   - +3 Officer Rep, recruit punished

---

### train_injury_during

**Training Injury** - A recruit is hurt during drills.

| Property | Value |
|----------|-------|
| Order Types | Train Recruits |
| Phase | Any |
| Weight | Balanced |

**Options:**
1. **Treat immediately** (Medicine 40)
   - Success: Minor injury, training continues
   - Failure: Worse injury, recruit out
   
2. **Send to surgeon**
   - Training paused, but proper care
   
3. **Push through - toughen them up**
   - -5 Soldier Rep (harsh), but +3 Discipline

---

### train_officer_observes

**Officer Observing** - A senior officer watches your training session.

| Property | Value |
|----------|-------|
| Order Types | Train Recruits |
| Phase | Midday |
| Weight | Higher when High Scrutiny |

**Effects:**
- +10 Scrutiny for this session
- All checks -10% (pressure)
- If session goes well: +10 Lord Rep
- If problems: +15 Scrutiny

---

### train_recruit_question

**Recruit's Question** - "Sir, what's it really like? The fighting?"

| Property | Value |
|----------|-------|
| Order Types | Train Recruits |
| Phase | Any |
| Weight | Balanced |

**Options:**
1. **Honest answer**
   - +5 Soldier Rep, recruits respect truth
   
2. **Inspiring answer**
   - +3 Morale, +3 Soldier Rep
   
3. **Harsh answer**
   - +3 Discipline, -2 Morale

---

## Craft & Repair Events

### repair_missing_parts

**Missing Parts** - Need a specific component. Don't have it.

| Property | Value |
|----------|-------|
| Order Types | Repair Equipment |
| Phase | Any |
| Weight | Higher when Low Supplies |

**Options:**
1. **Improvise substitute** (Crafting 60)
   - Success: Works, +15 Crafting XP
   - Failure: Doesn't hold, equipment unusable
   
2. **Request from quartermaster**
   - Delay, but proper parts
   - -2 Supplies
   
3. **Cannibalize another item**
   - Works, but -3 Equipment (other item lost)

---

### repair_discovered_sabotage

**Sabotage Discovered** - This damage was deliberate.

| Property | Value |
|----------|-------|
| Order Types | Repair Equipment |
| Phase | Any |
| Weight | Higher in Intense |

**Options:**
1. **Report immediately**
   - +10 Officer Rep, investigation starts
   
2. **Investigate quietly** (Perception 55)
   - Success: Find culprit, +15 Lord Rep
   - Failure: Trail cold, but you're suspicious
   
3. **Repair and stay alert**
   - No report, but watch for more

---

### repair_helped_smith

**Smith's Assistance** - The company smith offers guidance.

| Property | Value |
|----------|-------|
| Order Types | Repair Equipment |
| Phase | Any |
| Weight | Balanced |

**Effects:**
- +25 Crafting XP (learning opportunity)
- +3 Soldier Rep
- Potential mentor relationship

---

### repair_rush_job

**Rush Job** - Officer needs this NOW. No time for quality.

| Property | Value |
|----------|-------|
| Order Types | Repair Equipment |
| Phase | Any |
| Weight | Higher in Active/Intense |

**Options:**
1. **Quick repair** (Crafting 50)
   - Success: Done fast, +5 Officer Rep
   - Failure: Breaks again later, -5 Officer Rep
   
2. **Explain need for time**
   - 50% accepted, 50% pushed anyway
   
3. **Do it properly anyway**
   - -3 Officer Rep (slow), but quality work

---

### repair_quality_work

**Exceptional Work** - This piece came out better than expected.

| Property | Value |
|----------|-------|
| Order Types | Repair Equipment |
| Phase | Any |
| Requirements | Crafting > 60 |
| Weight | Balanced |

**Effects:**
- +8 Officer Rep
- +5 Equipment
- +20 Crafting XP
- "Sergeant noticed. Word gets around."

---

## Escort Events

### escort_bandit_scouts

**Bandit Scouts** - Riders watching from a distance.

| Property | Value |
|----------|-------|
| Order Types | Escort Duty |
| Phase | Any |
| Weight | Higher in Active |

**Options:**
1. **Chase them off** (Athletics 50 + mounted)
   - Success: Scattered, +5 Officer Rep
   - Failure: They escape, watching continues
   
2. **Defensive formation**
   - +3 Officer Rep (professional)
   - They may still attack later
   
3. **Ignore - focus on escort**
   - 40% ambush later

---

### escort_ambush

**Ambush!** - They attack the convoy.

| Property | Value |
|----------|-------|
| Order Types | Escort Duty |
| Phase | Any |
| Weight | Higher in Active/Intense |

**CRITICAL EVENT - Combat required**

**Options:**
1. **Defend the cargo** (Combat + Tactics 55)
   - Success: Cargo safe, bandits routed, +15 Lord Rep
   - Failure: Losses, cargo may be damaged
   
2. **Sacrifice cargo, save lives**
   - Order fails, but escort survives
   - -15 Lord Rep, +5 Soldier Rep
   
3. **All-out attack** (Combat)
   - Victory: Everything saved, +20 Lord Rep
   - Defeat: Heavy losses

---

### escort_difficult_terrain

**Difficult Terrain** - The road is washed out / blocked / dangerous.

| Property | Value |
|----------|-------|
| Order Types | Escort Duty |
| Phase | Any |
| Weight | Balanced |

**Options:**
1. **Find alternate route** (Scouting 50)
   - Success: Route found, continue
   - Failure: Lost time, +fatigue
   
2. **Clear/repair the path** (Athletics 55)
   - Success: Path cleared
   - Failure: Injury risk, equipment damage
   
3. **Wait for conditions to improve**
   - Safe, but delay (order extends)

---

### escort_cargo_problem

**Cargo Problem** - Wheel broken, horse lame, rope snapped.

| Property | Value |
|----------|-------|
| Order Types | Escort Duty |
| Phase | Any |
| Weight | Balanced |

**Options:**
1. **Quick field repair** (Crafting 50)
   - Success: Fixed, continue
   - Failure: Worse damage, major delay
   
2. **Redistribute load**
   - Slower movement, but functional
   
3. **Send for replacement**
   - Major delay, but proper solution

---

### escort_vip_demands

**VIP Demands** - The noble you're escorting is being difficult.

| Property | Value |
|----------|-------|
| Order Types | Escort Duty |
| Phase | Any |
| Requirements | Escort type is VIP |
| Weight | Balanced |

**Options:**
1. **Politely refuse** (Charm 55)
   - Success: They accept, +5 Officer Rep
   - Failure: Complaint to lord, -5 Lord Rep
   
2. **Accommodate request**
   - +5 delay, +10 fatigue, but VIP happy
   
3. **Firmly assert security needs** (Leadership 50)
   - Success: They respect authority
   - Failure: Conflict, -10 Lord Rep (VIP complains)

---

### escort_shortcut_option

**Shortcut Available** - A faster route, but potentially more dangerous.

| Property | Value |
|----------|-------|
| Order Types | Escort Duty |
| Phase | Any |
| Weight | Balanced |

**Options:**
1. **Take the shortcut** (Tactics 55)
   - Success: Faster arrival, +10 Lord Rep
   - Failure: Ambush or obstacle, trouble
   
2. **Stick to safe route**
   - Standard completion, +3 Officer Rep (reliable)
   
3. **Scout ahead first** (Scouting 50)
   - Success: Make informed decision
   - Failure: Time wasted either way

---

## Implementation Notes

### JSON File Structure

```
ModuleData/Enlisted/Orders/order_events/
├── guard_sentry_events.json    (11 events)
├── patrol_events.json          (12 events)
├── labor_events.json           (9 events)
├── scout_events.json           (8 events)
├── medical_events.json         (6 events)
├── leadership_events.json      (13 events)
├── craft_events.json           (5 events)
└── escort_events.json          (6 events)
```

### Localization

All text fields have corresponding ID fields for XML localization:
- `titleId` / `title`
- `setupId` / `setup`
- `textId` / `text` (per option)
- `resultTextId` / `resultText` (per outcome)

Add strings to `ModuleData/Languages/enlisted_strings.xml`.

### Skill Check Formula

```
Success Chance = 50% + (PlayerSkill / 2.5)

Modifiers:
  Fatigue > 50: -10%
  Fatigue > 75: -20%
  Night phase: -5%
  Recent injury: -10%
  Good equipment: +5%
  High morale: +5%
```

### Weight System

Base weight is 1.0. World state modifiers:

| Weight Key | When Applied |
|------------|--------------|
| `quiet` | Activity Level = Quiet |
| `routine` | Activity Level = Routine |
| `active` | Activity Level = Active |
| `intense` | Activity Level = Intense |

Higher weight = more likely to be selected from pool.

---

**End of Document**

