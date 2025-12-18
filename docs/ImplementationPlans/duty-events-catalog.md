# Duty Events Story Catalog - Content Reference

> **Note**: This is a content reference document. For the complete implementation plan and technical details, see `docs/Features/enlisted-interface-master-plan.md`.

## Overview

This document catalogs all duty-triggered events, their narrative content, player choices, skill checks, and outcome chains. Each event is organized by duty type with clear chain relationships.

## Event Chain Format

```
EVENT NAME (event_id)
├── Setup: [Description of situation]
├── Options:
│   ├── Option 1: [Choice text] (Skill Check)
│   │   ├── Success → [Immediate effects] → [Follow-up event]
│   │   └── Failure → [Immediate effects] → [Follow-up event]
│   ├── Option 2: [Choice text] (Standard/Safe)
│   │   └── Outcome → [Effects]
│   └── Option 3: [Choice text]
│       └── Outcome → [Effects]
└── Cooldown: X days
```

---

## Work Detail Events

### 1. Rusty Weapon Found (rusty_weapon_found)

**Setup**:
While maintaining equipment, you discover a rusted sword left by a careless soldier. It needs work, but with skill it could be salvaged.

**Options**:
1. **"Repair it properly"** (Smithing 30+)
   - Cost: 2 Fatigue
   - Success:
     - Immediate: +15 Smithing XP, -2 Fatigue
     - Follow-up: "rusty_weapon_outcome_success" (2 hours later)
   - Failure:
     - Immediate: -2 Fatigue (wasted effort)
     - Follow-up: "rusty_weapon_outcome_failure" (2 hours later)

2. **"Quick patch job"** (Standard)
   - Cost: 1 Fatigue
   - Outcome: +5 Smithing XP, weapon is mediocre, no follow-up

3. **"Leave it for the smith"** (Safe)
   - Cost: None
   - Outcome: No effects, no follow-up

**Cooldown**: 3 days

**Chain Events**:

#### Rusty Weapon Success (rusty_weapon_outcome_success)
**Setup**: The Lance Leader inspects the equipment and notices your repaired sword.

"Fine craftsmanship, soldier! You've earned your pay today."

**Outcome**: 
- +50 Gold
- +1 Reputation with Lance Leader
- +10 Smithing XP
- [Continue]

---

#### Rusty Weapon Failure (rusty_weapon_outcome_failure)
**Setup**: The sword snapped during your repair attempt. The Quartermaster is furious.

"That came out of your wages, fool! And you'll work extra hours to make up for it."

**Outcome**:
- -20 Gold
- -1 Reputation with Lance Leader
- +5 Fatigue (punishment detail)
- [Continue]

---

### 2. Broken Wagon Wheel (broken_wagon_wheel)

**Setup**:
The supply wagon has a cracked wheel. If it breaks on the road, the whole column will be delayed.

**Options**:
1. **"Reinforce the wheel"** (Engineering 25+)
   - Cost: 2 Fatigue
   - Success:
     - Immediate: +20 Engineering XP, -2 Fatigue
     - Follow-up: "wagon_wheel_holds" (4 hours later, if traveling)
   - Failure:
     - Immediate: -2 Fatigue
     - Follow-up: "wagon_wheel_breaks" (2 hours later, if traveling)

2. **"Temporary fix"** (Standard)
   - Cost: 1 Fatigue
   - Outcome: +10 Engineering XP, 50% chance of "wagon_wheel_breaks" later

3. **"Report it to the wheelwright"** (Safe)
   - Cost: None
   - Outcome: Wheelwright fixes it (no XP, no risks)

**Cooldown**: 5 days

**Chain Events**:

#### Wagon Wheel Holds (wagon_wheel_holds)
**Setup**: Hours of travel pass, and your reinforced wheel holds firm. The column makes good time.

**Outcome**:
- +1 Reputation
- +15 Engineering XP
- [Continue]

---

#### Wagon Wheel Breaks (wagon_wheel_breaks)
**Setup**: The wheel shatters on rough terrain. The column grinds to a halt for repairs.

"Who was on maintenance duty?! This delay is YOUR fault!"

**Outcome**:
- -2 Reputation
- Entire party -1 Morale
- [Continue]

---

### 3. Sharpening Stones (sharpening_stones_task)

**Setup**:
You're assigned to sharpen weapons for the company. It's tedious work, but attention to detail matters.

**Options**:
1. **"Do it right"** (Smithing 20+)
   - Cost: 2 Fatigue
   - Success:
     - Immediate: +15 Smithing XP, -2 Fatigue
     - Follow-up: "sharp_weapons_noticed" (1 day later, only if battle occurs)
   - Failure:
     - Immediate: -2 Fatigue
     - Follow-up: None

2. **"Rush through it"** (Standard)
   - Cost: 1 Fatigue
   - Outcome: +5 Smithing XP, weapons are adequate

3. **"Half-ass it"** (Roguery 15+)
   - Cost: 0 Fatigue (you slack off)
   - Success: Get away with it, no consequences
   - Failure: Caught by sergeant, -1 Reputation, +2 Fatigue (punishment)

**Cooldown**: 2 days

**Chain Events**:

#### Sharp Weapons Noticed (sharp_weapons_noticed)
**Trigger**: Only fires if battle occurs within 1 day of sharpening

**Setup**: After the battle, a veteran soldier thanks you.

"My blade cut through that brigand's mail like butter. You do good work, friend."

**Outcome**:
- +1 Reputation with Lance
- +10 Charm XP
- [Continue]

---

## Patrol Duty Events

### 4. Suspicious Tracks (suspicious_tracks)

**Setup**:
While on patrol, you spot fresh tracks leading away from the road. They could be bandits, deserters, or just travelers.

**Options**:
1. **"Investigate carefully"** (Scouting 35+)
   - Cost: 2 Fatigue
   - Success:
     - Immediate: +20 Scouting XP, -2 Fatigue
     - Follow-up: "tracks_investigation_success" (1 hour later)
   - Failure:
     - Immediate: -2 Fatigue (got lost tracking)
     - Follow-up: None

2. **"Follow at a distance"** (Scouting 20+)
   - Cost: 1 Fatigue
   - Success:
     - Immediate: +10 Scouting XP
     - Follow-up: "tracks_distant_observation" (1 hour later)
   - Failure:
     - Immediate: Lost trail, no follow-up

3. **"Report it to the sergeant"** (Safe)
   - Cost: None
   - Outcome: Sergeant investigates, +5 Leadership XP (for proper protocol)

**Cooldown**: 4 days

**Chain Events**:

#### Tracks Investigation Success (tracks_investigation_success)
**Setup**: You track the footprints to a hidden camp. Bandits! You count 8 men, poorly armed.

**Options**:
1. **"Report their location"** (Standard)
   - Outcome: Patrol sent to capture them, +2 Reputation, +20 Gold reward
   
2. **"Set up an ambush"** (Tactics 30+)
   - Success: Capture bandits solo, +5 Reputation, +50 Gold, +Hero renown
   - Failure: Outnumbered, wounded (Bruised condition), no rewards

3. **"Ignore them"** (Coward)
   - Outcome: -1 Reputation if discovered you knew about them

---

#### Tracks Distant Observation (tracks_distant_observation)
**Setup**: You observe from a distance. They're refugees, not bandits. Scared families fleeing war.

**Options**:
1. **"Offer them safe passage"** (Charm 25+)
   - Success: They join the camp followers, +2 Reputation, +Charm XP
   - Failure: They flee in fear

2. **"Give them food"** (Cost: 5 Gold)
   - Outcome: +1 Reputation, +10 Charm XP, -5 Gold

3. **"Let them be"**
   - Outcome: No effects

---

### 5. Lost Traveler (lost_traveler_encounter)

**Setup**:
You encounter a lone traveler on the road. He claims to be a merchant who was separated from his caravan.

**Options**:
1. **"Question him"** (Charm 20+ OR Roguery 25+)
   - Success: Realize he's lying (he's a spy/thief)
     - Follow-up: "traveler_revealed_spy" (immediate)
   - Failure: Believe him, he steals 10 Gold during conversation

2. **"Escort him to camp"** (Standard)
   - Outcome: He's legitimate, +10 Gold reward for helping, +Charm XP

3. **"Ignore him"** (Safe)
   - Outcome: No effects

**Cooldown**: 7 days

**Chain Events**:

#### Traveler Revealed as Spy (traveler_revealed_spy)
**Setup**: Your questioning reveals inconsistencies. He breaks and runs!

**Options**:
1. **"Chase him down"** (Athletics 30+)
   - Success: Capture him, +3 Reputation, +30 Gold reward, +Renown
   - Failure: He escapes

2. **"Shoot him"** (Bow/Crossbow 40+)
   - Success: Kill him (discover stolen goods on body), +20 Gold, +Roguery XP
   - Failure: He escapes

3. **"Let him go"**
   - Outcome: -1 Reputation (failed to stop spy)

---

## Sentry Duty Events

### 6. Night Watch Disturbance (night_watch_disturbance)

**Setup**:
During your night watch, you hear movement in the darkness. Could be an animal, could be trouble.

**Options**:
1. **"Investigate quietly"** (Scouting 25+)
   - Cost: 1 Fatigue
   - Success:
     - Follow-up: "night_watch_thief_caught" (immediate)
   - Failure:
     - Follow-up: "night_watch_false_alarm" (immediate)

2. **"Raise the alarm"** (Standard)
   - Outcome: 
     - 50% chance: False alarm, -1 Reputation (woke everyone)
     - 50% chance: Real threat spotted, +1 Reputation

3. **"Ignore it"** (Risky)
   - Outcome:
     - 30% chance: Nothing happens
     - 70% chance: Thief steals supplies, -2 Reputation (you were asleep)

**Cooldown**: 5 days

**Chain Events**:

#### Night Watch Thief Caught (night_watch_thief_caught)
**Setup**: You spot a camp follower sneaking toward the supply wagons. You catch him red-handed!

**Options**:
1. **"Arrest him"** (Standard)
   - Outcome: +2 Reputation, +10 Gold reward, +Leadership XP

2. **"Beat him and let him go"** (Vigor 25+)
   - Outcome: He flees, +Roguery XP, -Honor

3. **"Demand a bribe"** (Roguery 30+)
   - Success: +30 Gold, he leaves camp
   - Failure: He reports you, -2 Reputation

---

#### Night Watch False Alarm (night_watch_false_alarm)
**Setup**: It's just a stray dog rooting through garbage. You wasted time investigating.

**Outcome**:
- No effects (but you didn't fail your duty either)
- [Continue]

---

### 7. Officer's Inspection (officer_inspection_round)

**Setup**:
An officer makes a surprise inspection during your watch. Your alertness is being tested.

**Options**:
1. **"Stand at attention"** (Standard)
   - Outcome: Pass inspection, +5 Leadership XP

2. **"Engage in conversation"** (Charm 30+)
   - Success: Impress the officer, +1 Reputation, +15 Charm XP
   - Failure: Seen as brown-nosing, no effects

3. **"Act drowsy"** (If caught sleeping)
   - Outcome: -1 Reputation, +2 Fatigue (punishment detail)

**Cooldown**: 10 days

---

## Training Drill Events

### 8. Formation Drill Excellence (formation_drill_moment)

**Setup**:
During formation practice, the Drill Sergeant singles you out as an example.

"Look at this one! THAT'S how you hold the line!"

**Options**:
1. **"Thank him professionally"** (Standard)
   - Outcome: +1 Reputation, +10 Leadership XP

2. **"Show off"** (Athletics 35+)
   - Success: +2 Reputation, +20 Athletics XP
   - Failure: Stumble, -1 Reputation (embarrassed)

3. **"Stay humble"** (Charm 25+)
   - Outcome: +15 Charm XP, earn respect of peers

**Cooldown**: 7 days

---

### 9. Sparring Match Challenge (sparring_challenge)

**Setup**:
A rival soldier challenges you to a sparring match. Others are watching.

**Options**:
1. **"Accept the challenge"** (One-Handed 30+)
   - Cost: 2 Fatigue
   - Success: Win match, +2 Reputation, +25 One-Handed XP
   - Failure: Lose match, -1 Reputation, +10 One-Handed XP, Bruised

2. **"Decline politely"** (Charm 20+)
   - Success: Avoid fight without shame, +10 Charm XP
   - Failure: Seen as coward, -2 Reputation

3. **"Sucker punch him"** (Roguery 25+)
   - Success: Win instantly but dishonorably, -Honor, +Roguery XP
   - Failure: Caught cheating, -3 Reputation, punished

**Cooldown**: 5 days

---

## Foraging Events

### 10. Hidden Cache Discovery (foraging_cache_found)

**Setup**:
While foraging, you discover a hidden cache of supplies. Someone stashed this here deliberately.

**Options**:
1. **"Report it to command"** (Standard)
   - Outcome: +1 Reputation, +Steward XP

2. **"Keep it for yourself"** (Roguery 20+)
   - Cost: Risk
   - Success: +30 Gold, +Food items, +Roguery XP
   - Failure: Caught stealing, -3 Reputation, lose items

3. **"Share it with your lance"** (Leadership 25+)
   - Outcome: +2 Reputation with Lance, +Leadership XP

**Cooldown**: 10 days

---

## Event Chain Summary

### Simple Events (No Follow-Up)
- Sharpening Stones (rush/half-ass options)
- Lost Traveler (escort option)
- Night Watch False Alarm
- Officer's Inspection
- Formation Drill Excellence
- Sparring Match
- Hidden Cache

### 2-Event Chains
- Rusty Weapon → Success/Failure outcome
- Wagon Wheel → Holds/Breaks outcome
- Night Watch → Thief Caught/False Alarm
- Tracks Distant → Refugee options

### 3-Event Chains
- Suspicious Tracks → Investigation Success → Ambush/Report options
- Lost Traveler → Spy Revealed → Chase/Shoot/Release options

### Conditional Chains (Trigger Requirements)
- Sharpening Stones → Sharp Weapons Noticed (requires battle within 1 day)
- Wagon Wheel → Holds/Breaks (requires traveling)

---

## Event Pool Weights (Recommended)

### Work Detail
- rusty_weapon_found: 1.0
- broken_wagon_wheel: 0.8
- sharpening_stones_task: 1.2 (more common)

### Patrol Duty
- suspicious_tracks: 1.0
- lost_traveler_encounter: 0.7 (less common)

### Sentry Duty
- night_watch_disturbance: 1.0
- officer_inspection_round: 0.5 (rare)

### Training Drill
- formation_drill_moment: 0.8
- sparring_challenge: 0.6

### Foraging
- hidden_cache_found: 0.5 (rare)

---

## Future Event Ideas (Not Yet Implemented)

### Work Detail
- Armor Repair Crisis
- Supply Inventory Discrepancy
- Tool Theft Mystery

### Patrol
- Wounded Enemy Soldier
- Friendly Patrol Encounter
- Dangerous Wildlife

### Sentry
- Assassination Attempt
- Messenger Arrival
- Supernatural Sighting

### Training
- Weapon Breaking During Drill
- Instructor's Special Lesson
- Competition for Promotion

### Foraging
- Poisonous Plants Warning
- Local Hunter Encounter
- Hostile Wildlife Attack

### Scouting
- Enemy Camp Discovery
- Terrain Hazard
- Ancient Ruins

---

## Balancing Guidelines

### XP Rewards
- Standard option: 5-10 XP
- Skill check success: 15-25 XP
- Chain completion bonus: +10 XP

### Gold Rewards
- Minor success: 10-20 Gold
- Major success: 30-50 Gold
- Heroic outcome: 50-100 Gold

### Reputation
- Standard good deed: +1
- Exceptional performance: +2 to +3
- Failure/mistake: -1 to -2
- Serious misconduct: -3 to -5

### Cooldowns
- Simple events: 2-3 days
- Complex events: 5-7 days
- Rare events: 10-14 days

### Skill Check Difficulties
- Easy: 15-20 (70-80% success for average soldier)
- Moderate: 25-30 (50-60% success)
- Hard: 35-40 (30-40% success)
- Very Hard: 45+ (10-20% success, heroic)

