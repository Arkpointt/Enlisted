# Camp & Lance Activities — Lance Life

This document defines player-initiated activities available in the Camp Activities menu. These are things the player can **click on and do** to recover, socialize, earn XP, and build relationships.

---

## Menu Structure

```
Enlisted Status Menu (enlisted_status)
│
├── [🏃] Camp Activities (enlisted_activities)
│       │
│       ├── — TRAINING —
│       │   ├── Formation Drill
│       │   ├── Sparring Circle
│       │   ├── Weapons Practice
│       │   └── [formation-specific options]
│       │
│       ├── — CAMP TASKS —
│       │   ├── Help the Surgeon
│       │   ├── Work the Forge
│       │   ├── Forage for Camp
│       │   └── Maintain Equipment
│       │
│       ├── — SOCIAL —
│       │   ├── Fire Circle (evening/dusk)
│       │   ├── Drink with the Lads
│       │   ├── Play Dice
│       │   ├── Rest and Relax
│       │   └── Write a Letter
│       │
│       ├── — LANCE —
│       │   ├── Talk to Lance Leader
│       │   ├── Check on Lance Mates
│       │   ├── Help a Struggling Soldier
│       │   ├── Share Your Rations
│       │   └── Settle a Dispute
│       │
│       ├── — DESPERATE MEASURES — [Only if PayTension >= 40]
│       │   └── [see pay_tension_events.md]
│       │
│       └── — HELP THE LORD — [Only if PayTension >= 40]
│           └── [see pay_tension_events.md]
```

---

## Fatigue System

Fatigue is simple but punishing.

### Daily Fatigue Budget

You have **24 Fatigue points per day** to spend on activities.

| Fatigue Used | Consequence |
|--------------|-------------|
| 0-15 | No penalty |
| 16+ | Lose 15% of max health (immediately) |
| 24 (all of it) | Drop to 30% health |

Health lost to exhaustion must heal naturally over time.

### Fatigue Recovery

Fatigue recovers **hour-by-hour from dusk to dawn** (~12 hours), with rate based on your rank:

| Rank | Night Recovery | Per Night (~12 hrs) |
|------|----------------|---------------------|
| T1-T2 (Raw/Trained) | 0.5/hour | ~6 Fatigue |
| T3-T4 (Soldier/Veteran) | 0.75/hour | ~9 Fatigue |
| T5-T6 (Seasoned/Elite) | 1.0/hour | ~12 Fatigue |
| T7+ (NCO/Officer) | 1.25/hour | ~15 Fatigue |

**Why rank matters:**
- T1-T2: Worst duties, worst sleeping spots, constantly woken for watch
- T3-T4: Better tent placement, lighter night duties
- T5-T6: Respected, juniors handle the grunt work
- T7+: Own tent, servants handle menial tasks

**Progression feel:**
- T1-T2: Can safely do ~2-3 medium activities/day
- T3-T4: Can safely do ~4 medium activities/day
- T5-T6: Can safely do ~5-6 medium activities/day
- T7+: Almost unlimited freedom

### Boosting Recovery

| Method | Bonus | Cost | Limit |
|--------|-------|------|-------|
| Warm meal (camp kitchen) | +5 Fatigue | 50 gold | Once per day |
| Drinking | +10 Fatigue | 20 gold | Once per day |
| In settlement | +2 Fatigue/hour (any time) | — | While in settlement |

### Activity Cost Guidelines

| Activity Type | Fatigue Cost | Examples |
|---------------|--------------|----------|
| Light | +1 | Fire circle, write letter, watch sparring, just sit |
| Medium | +2 to +3 | Drill, weapons practice, help surgeon, check on mates |
| Heavy | +4 to +5 | Intensive sparring, forge work, full foraging expedition |
| Extreme | +6 | Raid mission, all-day labor |

This lets players do **4-5 medium activities** or **2-3 heavy ones** before hitting the danger zone (at T3+).

### Strategic Notes

**Settlement visits are valuable:**
- +2/hour any time means a 4-hour town visit = +8 recovery
- Requesting leave becomes strategic, not just for shopping
- Being stuck in the field is punishing

**Rank progression matters:**
- Early game (T1-T2) is a grind — manage fatigue carefully
- Mid game (T3-T4) opens up more options
- Late game (T5+) you have real freedom

### UI Display

Camp Activities menu header shows current fatigue:

```
═══════════════════════════════════════════════════════════════
                    CAMP ACTIVITIES
═══════════════════════════════════════════════════════════════

Fatigue: 11/24 ██████████████░░░░░░░░░░
⚠ 5 more until health loss

Recovery Rate: 0.75/hour (Rank T3)

═══════════════════════════════════════════════════════════════
```

---

## SHIRK DUTY (Risky Rest)

Need more fatigue but don't want to pay for meals or drinks? You can skip out on duties and rest — but there's a risk.

---

### `activity_shirk_duty`

**Menu Text:** "Shirk Duty [Risky Rest]"
**Menu Type:** `GameMenu` (enlisted_activities)
**Time Available:** Any
**Cooldown:** 8 hours

**Setup:**
> You're exhausted. The sergeant is looking the other way. Maybe you could slip off for a bit...
>
> Of course, if someone notices you're gone, there'll be hell to pay.

**Options:**

| Option | Fatigue Recovery | Caught Chance | If Caught |
|--------|------------------|---------------|-----------|
| Light rest (nap in wagon) | +4 | 10% | +1 Heat, -5 lance_rep |
| Hide and rest | +6 | 25% | +2 Heat, -10 lance_rep, -5 leader_rel |
| Fake sick | +8 | 35% | +3 Heat, -15 lance_rep, -10 leader_rel |
| Full shirk (disappear for hours) | +10 | 50% | +4 Heat, -20 lance_rep, -15 leader_rel, +1 Discipline |
| Never mind | — | — | — |

**Success Text (not caught):**
> You find a quiet spot. Close your eyes. When you wake, nobody's the wiser.
>
> Back to work, refreshed.

**Caught Text:**
> "Where the hell have you been?"
>
> {LANCE_LEADER_SHORT} is standing over you, arms crossed. This won't end well.

### Modifiers

**Someone covers for you:**
- If `lance_rep >= 50`, there's a 50% chance a lance mate notices and covers
- "I told them you were helping me with something."
- Caught chance reduced to 0% for that attempt

**Caught by a friend:**
- If `leader_rel >= 30` and caught, 50% chance they warn you instead of reporting
- "I didn't see you. But next time I will. Get back to work."
- No penalties, but next shirk has +15% caught chance

**Word gets around:**
- If you shirk **3+ times in 7 days**, automatic notification to lord
- -10 lord_rel
- "I hear you've been slacking. Don't make me regret keeping you."

**Repeat offender:**
- Each shirk in the same week adds +5% to caught chance
- First shirk: base chance
- Second shirk: +5%
- Third shirk: +10%
- Resets weekly

### Implementation

```csharp
void ProcessShirkDuty(string option)
{
    var shirk = GetShirkData(option);
    float caughtChance = shirk.BaseCaughtChance;
    
    // Repeat offender modifier
    caughtChance += (ShirksThisWeek * 0.05f);
    
    // Check if lance mate covers
    if (LanceRep >= 50 && MBRandom.RandomFloat < 0.5f)
    {
        ShowMessage("A lance mate covers for you.");
        ApplyFatigueRecovery(shirk.FatigueRecovery);
        ShirksThisWeek++;
        return;
    }
    
    // Roll for caught
    if (MBRandom.RandomFloat < caughtChance)
    {
        // Check if leader gives warning
        if (LeaderRel >= 30 && MBRandom.RandomFloat < 0.5f)
        {
            ShowMessage("Leader catches you but lets it slide — this time.");
            NextShirkCaughtBonus += 0.15f;
        }
        else
        {
            // Full consequences
            ApplyHeat(shirk.HeatPenalty);
            ChangeLanceRep(-shirk.LanceRepPenalty);
            ChangeLeaderRel(-shirk.LeaderRelPenalty);
            if (shirk.DisciplinePenalty > 0)
                AddDiscipline(shirk.DisciplinePenalty);
        }
    }
    else
    {
        ShowMessage("You rest unnoticed.");
    }
    
    ApplyFatigueRecovery(shirk.FatigueRecovery);
    ShirksThisWeek++;
    
    // Check for lord notification
    if (ShirksThisWeek >= 3)
    {
        ChangeLordRel(-10);
        ShowMessage("Word of your slacking reaches the lord.");
        ShirksThisWeek = 0; // Reset after lord finds out
    }
}
```

### Config

```json
{
  "shirk_duty": {
    "options": {
      "light_rest": {
        "fatigue_recovery": 4,
        "caught_chance": 0.10,
        "heat": 1,
        "lance_rep": -5,
        "leader_rel": 0,
        "discipline": 0
      },
      "hide_and_rest": {
        "fatigue_recovery": 6,
        "caught_chance": 0.25,
        "heat": 2,
        "lance_rep": -10,
        "leader_rel": -5,
        "discipline": 0
      },
      "fake_sick": {
        "fatigue_recovery": 8,
        "caught_chance": 0.35,
        "heat": 3,
        "lance_rep": -15,
        "leader_rel": -10,
        "discipline": 0
      },
      "full_shirk": {
        "fatigue_recovery": 10,
        "caught_chance": 0.50,
        "heat": 4,
        "lance_rep": -20,
        "leader_rel": -15,
        "discipline": 1
      }
    },
    "modifiers": {
      "lance_cover_threshold": 50,
      "lance_cover_chance": 0.50,
      "leader_warning_threshold": 30,
      "leader_warning_chance": 0.50,
      "repeat_offender_bonus": 0.05,
      "lord_notification_threshold": 3,
      "lord_notification_penalty": -10
    }
  }
}
```

---

## Edge Cases & Clarifications

### Fatigue at Day Boundary
- Recovery is hour-by-hour from dusk to dawn
- Whatever fatigue remains at dawn carries over
- Example: T3 player at 14 fatigue recovers 9 overnight → starts next day at 5

### Fatigue + Battle
- Battles cost fatigue: +3 (skirmish), +4 (battle), +6 (major battle/siege)
- This happens automatically after combat
- Can push player over 16 threshold → health loss after victory

### Hangover + Shirk Stacking
- Yes, penalties stack
- Hangover (+3 start) + caught shirking (+1 Discipline) = bad day
- Bad decisions compound

### Settlement + Night Recovery
- Both stack
- Settlement at night = rank recovery + 2/hour bonus
- Best possible recovery situation

### Shirk During Battle Prep
- If `battle_imminent` flag is true, shirk caught chance = 90%
- Almost guaranteed to get caught when army needs everyone

### Drinking Cooldown
- "1/day" limit is shared across all drinking sources
- Can't do "Drink with Lads" AND "Rest → Buy a drink" same day
- Pick one

### Minimum/Maximum Values
- Fatigue minimum: 0 (can't go negative)
- Fatigue maximum: 24 (can't exceed)
- Health from exhaustion: minimum 1% (exhaustion can't kill you directly)
- But at 1% health, anything else will

### Health Loss Timing
- Happens immediately when crossing 16 threshold
- Player sees warning, then health drops
- Not end-of-day calculation

### Wounded + Exhausted
- Penalties stack
- Exhaustion drops you to 30% + existing wounds
- Can end up at 1% health (minimum)
- Extremely vulnerable state

---

## TRAINING Activities

These build combat skills. Cost fatigue, reward XP.

---

### `activity_formation_drill`

**Menu Text:** "Formation Drill [+30 Tactics, +15 Athletics | +3 Fatigue]"
**Menu Type:** `GameMenu` (enlisted_activities)
**Time Available:** Morning, Afternoon
**Cooldown:** 8 hours

**Setup:**
> The sergeant is running drills. Shield walls, pike squares, cavalry charges — depending on your formation.
>
> "Fall in! Again! Tighter! You call that a line?"

**Options:**

| Option | Cost | Reward |
|--------|------|--------|
| Drill hard | +4 Fatigue | +40 Tactics XP, +25 Athletics XP, +10 One-Handed XP |
| Drill normally | +3 Fatigue | +30 Tactics XP, +15 Athletics XP |
| Go through the motions | +1 Fatigue | +10 Tactics XP, -5 leader_rel |

---

### `activity_sparring_circle`

**Menu Text:** "Sparring Circle [+35 Combat, +20 Athletics | +3 Fatigue]"
**Menu Type:** `GameMenu` (enlisted_activities)
**Time Available:** Any (not night)
**Cooldown:** 12 hours

**Setup:**
> A ring of soldiers. Two in the middle with practice weapons. Bets being placed.
>
> "Next!" someone shouts. Eyes turn to you.

**Options:**

| Option | Cost | Reward |
|--------|------|--------|
| Fight to win | +4 Fatigue | Athletics check: Win = +50 One-Handed XP, +30 Athletics XP, +15 lance_rep. Lose = +30 One-Handed XP, +20 Athletics XP |
| Fight carefully | +3 Fatigue | +35 One-Handed XP, +15 Athletics XP |
| Just watch | +1 Fatigue | +15 One-Handed XP, +10 Tactics XP |
| Bet on the fight | — | 50% chance: +20 gold or -10 gold, +15 Roguery XP |

---

### `activity_weapons_practice`

**Menu Text:** "Weapons Practice [+30 Weapon Skill, +15 Athletics | +2 Fatigue]"
**Menu Type:** `GameMenu` (enlisted_activities)
**Time Available:** Any (not night)
**Cooldown:** 8 hours

**Setup:**
> Training dummies. Targets. A quiet corner of camp where you can work on your form.

**Options:**

| Option | Cost | Reward |
|--------|------|--------|
| Practice your main weapon | +2 Fatigue | +35 XP in equipped weapon skill, +15 Athletics XP |
| Try a new weapon type | +3 Fatigue | +25 XP in chosen weapon skill, +20 Athletics XP |
| Practice archery/throwing | +2 Fatigue | +30 Bow/Crossbow/Throwing XP |
| Intensive drill (all weapons) | +5 Fatigue | +20 One-Handed, +20 Two-Handed, +20 Polearm, +15 Athletics XP |

---

## CAMP TASKS

These help the camp and build non-combat skills. Some require conditions.

---

### `activity_help_surgeon`

**Menu Text:** "Help the Surgeon [+40 Medicine | +3 Fatigue]"
**Menu Type:** `GameMenu` (enlisted_activities)
**Condition:** Wounded soldiers in party
**Time Available:** Any
**Cooldown:** 12 hours

**Setup:**
> The surgeon's tent is busy. Wounded from the last battle. Extra hands are needed.
>
> "You. Hold this. Don't let go."

**Options:**

| Option | Cost | Reward |
|--------|------|--------|
| Assist with surgery | +4 Fatigue | +50 Medicine XP, +15 Athletics XP, +10 lance_rep |
| Fetch supplies and bandages | +2 Fatigue | +30 Medicine XP, +10 Athletics XP |
| Comfort the wounded | +2 Fatigue | +20 Medicine XP, +25 Charm XP, +10 lance_rep |
| Help all day | +6 Fatigue | +70 Medicine XP, +20 Charm XP, +20 lance_rep |

---

### `activity_work_forge`

**Menu Text:** "Work the Forge [+40 Smithing | +3 Fatigue]"
**Menu Type:** `GameMenu` (enlisted_activities)
**Condition:** Army has smithing facilities
**Time Available:** Morning, Afternoon
**Cooldown:** 12 hours

**Setup:**
> The camp forge glows hot. The smith is swamped with repairs.
>
> "Know how to swing a hammer? Get over here."

**Options:**

| Option | Cost | Reward |
|--------|------|--------|
| Do skilled work | +4 Fatigue, Smithing > 50 | +50 Smithing XP, +15 Athletics XP |
| Do grunt work | +3 Fatigue | +35 Smithing XP, +20 Athletics XP |
| Sharpen weapons | +2 Fatigue | +20 Smithing XP, personal gear bonus |
| Full day at the forge | +6 Fatigue | +70 Smithing XP, +25 Athletics XP, +10 lance_rep |

---

### `activity_forage`

**Menu Text:** "Forage for Camp [+35 Scouting | +3 Fatigue]"
**Menu Type:** `GameMenu` (enlisted_activities)
**Condition:** Not in settlement
**Time Available:** Morning, Afternoon
**Cooldown:** 24 hours

**Setup:**
> The quartermaster needs more supplies. Firewood, herbs, game — anything helps.
>
> A few hours outside camp could make a difference.

**Options:**

| Option | Cost | Reward |
|--------|------|--------|
| Hunt for game | +4 Fatigue | Scouting check: +40 Scouting XP, +20 Bow XP, food supplies |
| Gather herbs | +2 Fatigue | +30 Scouting XP, +20 Medicine XP, herbs |
| Collect firewood | +3 Fatigue | +25 Scouting XP, +20 Athletics XP, +10 lance_rep |
| Look for valuables | +3 Fatigue, +2 Heat | +25 Scouting XP, +25 Roguery XP, chance of loot |
| Full foraging expedition | +5 Fatigue | +50 Scouting XP, +15 Medicine XP, +15 Athletics XP, supplies |

---

### `activity_maintain_equipment`

**Menu Text:** "Maintain Equipment [+25 Smithing | +2 Fatigue]"
**Menu Type:** `GameMenu` (enlisted_activities)
**Time Available:** Any
**Cooldown:** 24 hours

**Setup:**
> Your gear needs attention. Rust on the blade. Loose straps. A dent in the helmet.

**Options:**

| Option | Cost | Reward |
|--------|------|--------|
| Full maintenance | +3 Fatigue | +35 Smithing XP, +10 Trade XP, gear condition bonus |
| Quick clean | +1 Fatigue | +20 Smithing XP |
| Help lance mates with their gear | +4 Fatigue | +30 Smithing XP, +15 Charm XP, +15 lance_rep |
| Repair and improve | +5 Fatigue, Smithing > 80 | +50 Smithing XP, gear upgrade chance |

---

## SOCIAL Activities

These recover fatigue, build relationships, and provide entertainment.

---

### `activity_fire_circle`

**Menu Text:** "Fire Circle [+25 Charm | +1 Fatigue]"
**Menu Type:** `GameMenu` (enlisted_activities)
**Time Available:** Evening, Dusk, Night
**Cooldown:** 8 hours

**Setup:**
> The fire crackles. {LANCE_NAME} gathers around, passing a skin of something. {VETERAN_1_NAME} starts a story about a battle years past.
>
> The war feels far away for a moment.

**Options:**

| Option | Cost | Reward |
|--------|------|--------|
| Listen and learn | +1 Fatigue | +20 Tactics XP, +15 Charm XP |
| Share your own story | +1 Fatigue | Charm check: Success = +35 Charm XP, +20 Leadership XP, +15 lance_rep. Fail = +10 Charm XP |
| Tell jokes | +1 Fatigue | +30 Charm XP, +10 Roguery XP, +5 lance_rep |
| Just sit by the fire | +0 Fatigue | +10 Charm XP |

---

### `activity_drink_with_lads`

**Menu Text:** "Drink with the Lads"
**Menu Type:** `GameMenu` (enlisted_activities)
**Time Available:** Evening, Night
**Cooldown:** 24 hours

**Setup:**
> {LANCE_MATE_NAME} waves you over. "We're splitting a cask. You in?"
>
> It's not fancy, but it's wet and it's here.

**Options:**

| Option | Cost | Reward |
|--------|------|--------|
| Drink moderately | 20 gold | +10 Fatigue recovery, +20 Charm XP, +10 lance_rep |
| Drink heavily | 30 gold, +2 Heat | +10 Fatigue recovery, +30 Charm XP, +15 Roguery XP, +15 lance_rep, 40% hangover |
| Buy a round for everyone | 50 gold | +10 Fatigue recovery, +40 Charm XP, +20 Leadership XP, +25 lance_rep |
| Challenge someone to drinking contest | 25 gold, +2 Heat | +10 Fatigue recovery, Athletics check: Win = +35 Charm XP, +20 gold, +20 lance_rep. Lose = +15 Charm XP, 60% hangover |

**Note:** Drinking gives +10 Fatigue recovery but only works once per day.

**Hangover (next day):**
> Morning comes hard. Head pounding. Stomach churning.
> 
> Start the next day at 3 Fatigue instead of 0.

---

### `activity_play_dice`

**Menu Text:** "Play Dice [Gambling]"
**Menu Type:** `GameMenu` (enlisted_activities)
**Time Available:** Evening, Night
**Cooldown:** 12 hours

**Setup:**
> Dice rattle behind the supply wagons. Coins changing hands. {SOLDIER_NAME} is running the game.
>
> "Room for one more?"

**Options:**

| Option | Cost | Reward |
|--------|------|--------|
| Small stakes | -15 gold | 50% win: +30 gold, +20 Roguery XP, +10 Charm XP. Lose: +10 Roguery XP |
| High stakes | -40 gold | 40% win: +80 gold, +35 Roguery XP, +15 Charm XP. Lose: +15 Roguery XP |
| Just watch and learn | +1 Fatigue | +15 Roguery XP, +15 Tactics XP (reading people) |
| Run a side game | -25 gold, +2 Heat | 45% win: +50 gold, +30 Roguery XP, +20 Trade XP |
| Try to cheat | -20 gold, +3 Heat | Roguery check: Win = +60 gold, +40 Roguery XP. Fail = caught, -20 lance_rep, fight |

---

### `activity_rest_relax`

**Menu Text:** "Rest and Relax"
**Menu Type:** `GameMenu` (enlisted_activities)
**Time Available:** Any
**Cooldown:** None

**Setup:**
> Sometimes you need to take it easy. The camp kitchen is serving hot food, and there's always someone selling drink.

**Options:**

| Option | Cost | Reward |
|--------|------|--------|
| Buy a warm meal | 50 gold | +5 Fatigue recovery, 1/day limit |
| Buy a drink | 20 gold | +10 Fatigue recovery, 1/day limit |
| Just sit and wait | — | Normal recovery (0.5/hour at night only) |

---

### `activity_write_letter`

**Menu Text:** "Write a Letter [+20 Charm]"
**Menu Type:** `GameMenu` (enlisted_activities)
**Time Available:** Any
**Cooldown:** 72 hours (3 days)

**Setup:**
> Paper is precious. Ink more so. But sometimes you need to write home.

**Options:**

| Option | Cost | Reward |
|--------|------|--------|
| Write to family | -1 Fatigue | +25 Charm XP, +15 Leadership XP (responsibility) |
| Write to a lover | -1 Fatigue | +30 Charm XP, sets up future letter event |
| Write to an old friend | -1 Fatigue | +20 Charm XP, +20 Trade XP (networking) |
| Practice your letters | -1 Fatigue | +15 Charm XP, +15 Steward XP (if low literacy) |
| Write a report to sell | -1 Fatigue, +2 Heat | +15 Charm XP, +25 Roguery XP, +20 gold (selling intel) |

---

## LANCE Activities

These focus on your lance mates — building relationships, helping each other, and dealing with problems.

---

### `activity_talk_lance_leader`

**Menu Text:** "Talk to Lance Leader [+20 Leadership]"
**Menu Type:** `GameMenu` (enlisted_activities)
**Time Available:** Any
**Cooldown:** 24 hours

**Setup:**
> {LANCE_LEADER_SHORT} is checking equipment. They notice you approach.
>
> "Something on your mind?"

**Options:**

| Option | Cost | Reward |
|--------|------|--------|
| Ask about your performance | +1 Fatigue | Feedback on stats, +15 Leadership XP, +10 leader_rel |
| Ask about the campaign | +1 Fatigue | +25 Tactics XP, +15 Leadership XP, intel on upcoming events |
| Ask for combat advice | +1 Fatigue | +20 One-Handed XP, +20 Tactics XP |
| Report a concern | — | Depends on concern — can trigger events, +10 Leadership XP |
| Just chat | -1 Fatigue | +15 Charm XP, +10 leader_rel |
| Ask to train with them | +3 Fatigue | +35 One-Handed XP, +25 Athletics XP, +15 leader_rel |

---

### `activity_check_lance_mates`

**Menu Text:** "Check on Lance Mates [+15 Charm, +15 Leadership]"
**Menu Type:** `GameMenu` (enlisted_activities)
**Time Available:** Any
**Cooldown:** 12 hours

**Setup:**
> Your lance. Your people. Worth knowing how they're doing.

**Options:**

| Option | Cost | Reward |
|--------|------|--------|
| Make the rounds | +2 Fatigue | +20 Charm XP, +20 Leadership XP, +15 lance_rep, status info |
| Focus on the wounded | +2 Fatigue | +25 Medicine XP, +15 Charm XP, +10 lance_rep |
| Focus on the new recruits | +2 Fatigue | +25 Leadership XP, +15 Charm XP, +10 lance_rep |
| Look for problems | +1 Fatigue | +15 Roguery XP, +15 Tactics XP, may reveal lance issues |
| Drill them yourself | +4 Fatigue | +30 Leadership XP, +20 Tactics XP, +15 lance_rep |

---

### `activity_help_struggling_soldier`

**Menu Text:** "Help a Struggling Soldier [+25 Charm]"
**Menu Type:** `GameMenu` (enlisted_activities)
**Condition:** Lance has soldier with low morale, wounded, or in debt
**Time Available:** Any
**Cooldown:** 24 hours

**Setup:**
> {LANCE_MATE_NAME} is having a hard time. You can see it in their face. Maybe you can help.

**Options:**

| Option | Cost | Reward |
|--------|------|--------|
| Listen to their problems | +2 Fatigue | +25 Charm XP, +15 Leadership XP, +20 {LANCE_MATE} relation |
| Help with their duties | +4 Fatigue | +20 Athletics XP, +15 Leadership XP, +15 lance_rep, +25 {LANCE_MATE} relation |
| Lend them money | -25 gold | +30 Charm XP, +30 {LANCE_MATE} relation, debt owed to you |
| Train them | +4 Fatigue | +25 Leadership XP, +20 One-Handed XP, +20 {LANCE_MATE} relation |
| Give tough love | +1 Fatigue | Charm check: Success = +20 Leadership XP, +15 {LANCE_MATE} relation. Fail = -15 relation |
| Share your own struggles | +1 Fatigue | +30 Charm XP, +25 {LANCE_MATE} relation, +10 lance_rep |

---

### `activity_share_rations`

**Menu Text:** "Share Your Rations [+20 Lance Rep]"
**Menu Type:** `GameMenu` (enlisted_activities)
**Condition:** Food is limited (supply issues)
**Time Available:** Meal times
**Cooldown:** 24 hours

**Setup:**
> Rations are thin. Your portion is small, but some have less.
>
> {LANCE_MATE_NAME} is eyeing your food. Their portion was even smaller.

**Options:**

| Option | Cost | Reward |
|--------|------|--------|
| Share your food | +2 Fatigue (hunger) | +20 Charm XP, +15 Leadership XP, +20 lance_rep, +25 {LANCE_MATE} relation |
| Give them all of it | +4 Fatigue (hunger) | +30 Charm XP, +25 Leadership XP, +30 lance_rep, +35 {LANCE_MATE} relation |
| Trade for a favor | +1 Fatigue | +15 Trade XP, +15 Roguery XP, +15 lance_rep, favor owed |
| Keep your food | — | -5 lance_rep (they notice) |
| Organize a fair distribution | +2 Fatigue | +25 Leadership XP, +20 Steward XP, +20 lance_rep |

---

### `activity_settle_dispute`

**Menu Text:** "Settle a Dispute [+30 Leadership]"
**Menu Type:** `GameMenu` (enlisted_activities)
**Condition:** Lance has active conflict between members
**Time Available:** Any
**Cooldown:** 48 hours

**Setup:**
> {SOLDIER_1_NAME} and {SOLDIER_2_NAME} are at each other's throats again. Arguing over something stupid. It could get worse.

**Options:**

| Option | Cost | Reward |
|--------|------|--------|
| Mediate fairly | +2 Fatigue | Charm check: +35 Leadership XP, +25 Charm XP, +15 lance_rep, conflict resolved |
| Side with {SOLDIER_1} | +1 Fatigue | +15 Leadership XP, +20 {SOLDIER_1} relation, -20 {SOLDIER_2} relation |
| Side with {SOLDIER_2} | +1 Fatigue | +15 Leadership XP, +20 {SOLDIER_2} relation, -20 {SOLDIER_1} relation |
| Let them fight it out | +1 Fatigue | +20 Roguery XP, one soldier wounded, conflict "resolved" |
| Report to lance leader | — | +10 leader_rel, +10 Leadership XP, -15 lance_rep (seen as snitch) |
| Challenge the aggressor | +3 Fatigue | +25 Leadership XP, +20 Athletics XP, +20 lance_rep, fight |

---

## Implementation

### Menu Builder

```csharp
void BuildCampActivitiesMenu(CampaignGameStarter starter)
{
    // === TRAINING SECTION ===
    AddSectionHeader(starter, "training_header", "— TRAINING —");
    
    AddActivityOption(starter, "activity_formation_drill",
        "Formation Drill [+20 Tactics | +1 Fatigue]",
        condition: () => IsTimeOfDay(Morning, Afternoon) && OffCooldown("formation_drill"),
        consequence: () => LaunchActivity("activity_formation_drill"));
    
    AddActivityOption(starter, "activity_sparring",
        "Sparring Circle [+25 Combat | +2 Fatigue]",
        condition: () => !IsTimeOfDay(Night) && OffCooldown("sparring"),
        consequence: () => LaunchActivity("activity_sparring_circle"));
    
    AddActivityOption(starter, "activity_weapons",
        "Weapons Practice [+20 Weapon Skill | +1 Fatigue]",
        condition: () => !IsTimeOfDay(Night) && OffCooldown("weapons_practice"),
        consequence: () => LaunchActivity("activity_weapons_practice"));
    
    // === CAMP TASKS SECTION ===
    AddSectionHeader(starter, "tasks_header", "— CAMP TASKS —");
    
    AddActivityOption(starter, "activity_surgeon",
        "Help the Surgeon [+25 Medicine | +1 Fatigue]",
        condition: () => HasWoundedInParty() && OffCooldown("help_surgeon"),
        consequence: () => LaunchActivity("activity_help_surgeon"));
    
    AddActivityOption(starter, "activity_forge",
        "Work the Forge [+25 Smithing | +2 Fatigue]",
        condition: () => HasSmithingFacility() && OffCooldown("work_forge"),
        consequence: () => LaunchActivity("activity_work_forge"));
    
    AddActivityOption(starter, "activity_forage",
        "Forage for Camp [+20 Scouting | +2 Fatigue]",
        condition: () => !IsInSettlement() && OffCooldown("forage"),
        consequence: () => LaunchActivity("activity_forage"));
    
    AddActivityOption(starter, "activity_maintain",
        "Maintain Equipment [+15 Smithing | +1 Fatigue]",
        condition: () => OffCooldown("maintain_equipment"),
        consequence: () => LaunchActivity("activity_maintain_equipment"));
    
    // === SOCIAL SECTION ===
    AddSectionHeader(starter, "social_header", "— SOCIAL —");
    
    AddActivityOption(starter, "activity_fire",
        "Fire Circle [+15 Charm | -1 Fatigue]",
        condition: () => IsTimeOfDay(Evening, Dusk, Night) && OffCooldown("fire_circle"),
        consequence: () => LaunchActivity("activity_fire_circle"));
    
    AddActivityOption(starter, "activity_drink",
        "Drink with the Lads [-5 Gold | -2 Fatigue]",
        condition: () => IsTimeOfDay(Evening, Night) && HasGold(5) && OffCooldown("drink"),
        consequence: () => LaunchActivity("activity_drink_with_lads"));
    
    AddActivityOption(starter, "activity_dice",
        "Play Dice [Gambling]",
        condition: () => IsTimeOfDay(Evening, Night) && OffCooldown("dice"),
        consequence: () => LaunchActivity("activity_play_dice"));
    
    AddActivityOption(starter, "activity_rest",
        "Rest and Relax [-3 Fatigue]",
        condition: () => OffCooldown("rest"),
        consequence: () => LaunchActivity("activity_rest_relax"));
    
    AddActivityOption(starter, "activity_letter",
        "Write a Letter [+10 Charm]",
        condition: () => OffCooldown("letter"),
        consequence: () => LaunchActivity("activity_write_letter"));
    
    // === LANCE SECTION ===
    AddSectionHeader(starter, "lance_header", "— LANCE —");
    
    AddActivityOption(starter, "activity_talk_leader",
        "Talk to Lance Leader",
        condition: () => OffCooldown("talk_leader"),
        consequence: () => LaunchActivity("activity_talk_lance_leader"));
    
    AddActivityOption(starter, "activity_check_mates",
        "Check on Lance Mates",
        condition: () => OffCooldown("check_mates"),
        consequence: () => LaunchActivity("activity_check_lance_mates"));
    
    AddActivityOption(starter, "activity_help_soldier",
        "Help a Struggling Soldier",
        condition: () => HasStrugglingLanceMate() && OffCooldown("help_soldier"),
        consequence: () => LaunchActivity("activity_help_struggling_soldier"));
    
    AddActivityOption(starter, "activity_share_rations",
        "Share Your Rations",
        condition: () => IsSupplyLow() && OffCooldown("share_rations"),
        consequence: () => LaunchActivity("activity_share_rations"));
    
    AddActivityOption(starter, "activity_settle_dispute",
        "Settle a Dispute",
        condition: () => HasLanceConflict() && OffCooldown("settle_dispute"),
        consequence: () => LaunchActivity("activity_settle_dispute"));
    
    // === DESPERATE MEASURES (pay tension) ===
    if (PayTension >= 40)
    {
        AddSectionHeader(starter, "desperate_header", "— DESPERATE MEASURES —");
        // ... see pay_tension_events.md
    }
    
    // === HELP THE LORD (pay tension) ===
    if (PayTension >= 40)
    {
        AddSectionHeader(starter, "help_lord_header", "— HELP THE LORD —");
        // ... see pay_tension_events.md
    }
}
```

### Activity Launcher

```csharp
void LaunchActivity(string activityId)
{
    var activity = GetActivityData(activityId);
    
    // Show inquiry with options
    var options = new List<InquiryElement>();
    foreach (var option in activity.Options)
    {
        options.Add(new InquiryElement(
            option.Id,
            option.Text,
            null,
            option.CanChoose(),
            option.GetTooltip()
        ));
    }
    
    MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
        activity.Title,
        activity.Setup,
        options,
        true, 1, 1,
        "Select",
        "Cancel",
        (selected) => ProcessActivityChoice(activityId, selected[0].Identifier as string),
        (cancelled) => { }
    ));
}
```

---

## Config Schema

```json
{
  "camp_activities": {
    "training": {
      "formation_drill": {
        "cooldown_hours": 8,
        "time_available": ["morning", "afternoon"],
        "fatigue_cost": { "hard": 2, "normal": 1, "slack": 0 },
        "xp_reward": { "hard": 30, "normal": 20, "slack": 10 }
      },
      "sparring_circle": {
        "cooldown_hours": 12,
        "time_available": ["morning", "afternoon", "evening"],
        "fatigue_cost": 2,
        "xp_reward": { "win": 35, "lose": 20, "watch": 10 }
      },
      "weapons_practice": {
        "cooldown_hours": 8,
        "time_available": ["morning", "afternoon", "evening"],
        "fatigue_cost": 1,
        "xp_reward": 20
      }
    },
    "camp_tasks": {
      "help_surgeon": {
        "cooldown_hours": 12,
        "condition": "has_wounded",
        "fatigue_cost": { "assist": 2, "fetch": 1, "comfort": 1 },
        "xp_reward": { "assist": 35, "fetch": 20, "comfort": 15 }
      },
      "work_forge": {
        "cooldown_hours": 12,
        "condition": "has_smithing_facility",
        "fatigue_cost": 2,
        "xp_reward": { "skilled": 35, "grunt": 20, "sharpen": 15 }
      },
      "forage": {
        "cooldown_hours": 24,
        "condition": "not_in_settlement",
        "fatigue_cost": 2,
        "xp_reward": { "hunt": 25, "herbs": 20, "firewood": 15, "valuables": 15 }
      },
      "maintain_equipment": {
        "cooldown_hours": 24,
        "fatigue_cost": { "full": 2, "quick": 1, "help_others": 2 },
        "xp_reward": { "full": 20, "quick": 10, "help_others": 15 }
      }
    },
    "social": {
      "fire_circle": {
        "cooldown_hours": 8,
        "time_available": ["evening", "dusk", "night"],
        "fatigue_change": -1,
        "xp_reward": { "listen": 15, "share": 25, "drink": 20, "enjoy": 0 }
      },
      "drink_with_lads": {
        "cooldown_hours": 24,
        "time_available": ["evening", "night"],
        "gold_cost": { "moderate": 5, "heavy": 10, "round": 25, "one": 2 },
        "fatigue_change": { "moderate": -2, "heavy": -3, "round": -2, "one": -1 },
        "hangover_chance": 0.30
      },
      "play_dice": {
        "cooldown_hours": 12,
        "time_available": ["evening", "night"],
        "stakes": {
          "small": { "cost": 10, "win_chance": 0.50, "payout": 20 },
          "high": { "cost": 30, "win_chance": 0.40, "payout": 60 },
          "side_bet": { "cost": 20, "win_chance": 0.45, "payout": 40 }
        }
      },
      "rest_relax": {
        "cooldown_hours": 4,
        "fatigue_change": { "nap": -3, "quiet": -2, "sleep_in": -4 }
      },
      "write_letter": {
        "cooldown_hours": 72,
        "xp_reward": { "family": 15, "lover": 20, "friend": 10, "practice": 10 }
      }
    },
    "lance": {
      "talk_lance_leader": {
        "cooldown_hours": 24,
        "relation_change": { "chat": 5, "performance": 5 }
      },
      "check_lance_mates": {
        "cooldown_hours": 12,
        "fatigue_cost": 1,
        "lance_rep_change": 10
      },
      "help_struggling_soldier": {
        "cooldown_hours": 24,
        "condition": "has_struggling_soldier",
        "options": {
          "listen": { "fatigue": 1, "relation": 15 },
          "help_duties": { "fatigue": 2, "relation": 20 },
          "lend_money": { "gold": 20, "relation": 25 },
          "train": { "fatigue": 2, "relation": 15 }
        }
      },
      "share_rations": {
        "cooldown_hours": 24,
        "condition": "supply_low",
        "lance_rep_change": { "share": 15, "all": 25, "trade": 10 }
      },
      "settle_dispute": {
        "cooldown_hours": 48,
        "condition": "has_lance_conflict"
      }
    }
  }
}
```

---

## Summary Tables

### Training Activities

| Activity | Fatigue Cost | XP Rewards | Cooldown |
|----------|--------------|------------|----------|
| Formation Drill | +1 to +4 | +10-40 Tactics, +15-25 Athletics | 8 hours |
| Sparring Circle | +1 to +4 | +15-50 Combat, +10-30 Athletics | 12 hours |
| Weapons Practice | +2 to +5 | +20-35 Weapon, +15-20 Athletics | 8 hours |

### Camp Tasks

| Activity | Condition | Fatigue Cost | XP Rewards | Cooldown |
|----------|-----------|--------------|------------|----------|
| Help Surgeon | Wounded | +2 to +6 | +20-70 Medicine, +15-25 Charm | 12 hours |
| Work Forge | Smithing | +2 to +6 | +20-70 Smithing, +15-25 Athletics | 12 hours |
| Forage | Not in town | +2 to +5 | +25-50 Scouting, +15-20 Medicine/Bow | 24 hours |
| Maintain Gear | Always | +1 to +5 | +20-50 Smithing, +10-15 Trade | 24 hours |

### Social Activities

| Activity | Time | Cost | Fatigue | XP Rewards | Cooldown |
|----------|------|------|---------|------------|----------|
| Fire Circle | Evening+ | — | +0 to +1 | +10-35 Charm, +15-20 Tactics/Leadership | 8 hours |
| Drink with Lads | Evening+ | 20-50 gold | +10 recovery | +20-40 Charm, +15-20 Leadership | 24 hours |
| Play Dice | Evening+ | 15-40 gold | +1 | +10-40 Roguery, +10-15 Charm | 12 hours |
| Rest (warm meal) | Any | 50 gold | +5 recovery | — | 1/day |
| Rest (drink) | Any | 20 gold | +10 recovery | — | 1/day |
| Write a Letter | Any | — | +1 | +15-30 Charm, +15-25 Leadership/Trade | 72 hours |

### Lance Activities

| Activity | Condition | Fatigue Cost | XP Rewards | Cooldown |
|----------|-----------|--------------|------------|----------|
| Talk to Leader | Always | +1 to +3 | +15-35 Leadership/Combat, +15-25 Tactics | 24 hours |
| Check on Mates | Always | +1 to +4 | +15-30 Leadership, +15-25 Charm | 12 hours |
| Help Struggling | Has struggling | +1 to +4 | +20-30 Charm, +15-25 Leadership | 24 hours |
| Share Rations | Low supplies | +1 to +4 | +15-30 Charm, +15-25 Leadership | 24 hours |
| Settle Dispute | Has conflict | +1 to +3 | +15-35 Leadership, +20-25 Charm | 48 hours |

### Fatigue Quick Reference

**Daily Budget:** 24 Fatigue points

| Fatigue Used | Consequence |
|--------------|-------------|
| 0-15 | No penalty |
| 16+ | Lose 15% health |
| 24 | Drop to 30% health |

**Night Recovery (by Rank):**

| Rank | Rate | Per Night |
|------|------|-----------|
| T1-T2 | 0.5/hour | ~6 |
| T3-T4 | 0.75/hour | ~9 |
| T5-T6 | 1.0/hour | ~12 |
| T7+ | 1.25/hour | ~15 |

**Paid Recovery:**
- Warm meal: +5 (50 gold, 1/day)
- Drinking: +10 (20 gold, 1/day)
- In settlement: +2/hour (any time)

**Risky Recovery (Shirk Duty):**

| Option | Recovery | Caught % | If Caught |
|--------|----------|----------|-----------|
| Light rest | +4 | 10% | +1 Heat, -5 lance_rep |
| Hide and rest | +6 | 25% | +2 Heat, -10 lance_rep |
| Fake sick | +8 | 35% | +3 Heat, -15 lance_rep |
| Full shirk | +10 | 50% | +4 Heat, -20 lance_rep, +1 Discipline |

---

## Cross-References

- **Menu System:** `menu_system_update.md` — Menu structure
- **Pay Tension Events:** `pay_tension_events.md` — Desperate Measures and Help Lord sections
- **Events Library:** `lance_life_events_content_library.md` — Related random events

---

*Document Version: 1.0*
*For use with: Lance Life Camp Activities Menu*
