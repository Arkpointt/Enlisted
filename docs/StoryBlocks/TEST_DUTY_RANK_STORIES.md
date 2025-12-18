# T1-T2 Enlisted Duty Stories - Test Document

> **Purpose**: This document reorganizes and rewrites ALL existing duty/event content for T1-T2 (Enlisted) rank with proper:
> - Gold rewards/costs
> - Skill checks scaled for T1-T2 (difficulty 15-35)
> - Rep/Lance reputation changes
> - Rich outcome texts
> - Notes on which duties continue into higher tiers

**Last Updated**: December 18, 2025

---

## Rank Context

**T1-T2: ENLISTED (Man-at-Arms / Soldier)**
- Position: Bottom of chain of command
- Authority: NONE - takes orders from everyone
- Role: Execute duties personally, follow orders
- Gold Range: 10-100g per event
- XP Range: 10-30 per skill
- Skill Check Difficulty: 15 (Easy), 25 (Medium), 35 (Hard)

---

## Duty Categories for T1-T2

### Duties That Continue Into Higher Tiers:
These duties exist at all tiers but the PLAYER'S ROLE changes:
- ✓ **Scout Duty** → T1-2: Perform scouting | T3-4: Lead scout team | T5-6: Plan reconnaissance
- ✓ **Guard Duty** → T1-2: Stand watch | T3-4: Assign rotation | T5-6: Plan security
- ✓ **Patrol Duty** → T1-2: Walk patrol | T3-4: Lead patrol | T5-6: Plan routes
- ✓ **Quartermaster Duty** → T1-2: Count supplies | T3-4: Manage distribution | T5-6: Negotiate suppliers
- ✓ **Engineering Duty** → T1-2: Dig/build | T3-4: Supervise crews | T5-6: Design fortifications

### Duties Exclusive to T1-T2 (Enlisted):
These are grunt work that NCOs/Officers don't do:
- ✗ **Stable Duty** (T1-2 only)
- ✗ **Kitchen Duty** (T1-2 only)
- ✗ **Latrine Duty** (T1-2 only)
- ✗ **Water Carry** (T1-2 only)
- ✗ **Firewood Duty** (T1-2 only)

---

## SCOUT DUTY EVENTS (T1-T2)

### 1. Terrain Reconnaissance

**Original ID**: `scout_terrain_recon`
**Continues to Higher Tiers**: Yes (role changes)

**CURRENT VERSION (Weak)**:
```
Title: "Terrain Reconnaissance"
Setup: Lance Leader sends you to scout terrain before dawn.
Options: All give XP only, no gold, no rep, weak outcomes.
```

**REWRITTEN VERSION (T1-T2 Enlisted)**:

```json
{
  "id": "scout_terrain_recon_t1",
  "tier_range": { "min": 1, "max": 2 },
  "title": "Terrain Reconnaissance",
  "setup": "{LANCE_LEADER_SHORT} pulls you aside before dawn.\n\n\"Scout duty. The captain needs to know what we're walking into — terrain, cover, escape routes. Don't get spotted, don't get killed.\"\n\nThe enemy is somewhere ahead. Finding them before they find us could save lives.",
  
  "options": [
    {
      "id": "standard_recon",
      "text": "[Standard] Careful reconnaissance, stay hidden",
      "risk": "safe",
      "fatigue": 2,
      "rewards": {
        "xp": { "scouting": 25, "tactics": 15 },
        "gold": 0,
        "rep": 1
      },
      "outcome": "You move carefully through the terrain, noting every ridge and gully. By mid-morning you've mapped three escape routes and two ambush positions. {LANCE_LEADER_SHORT} nods when you report back. \"Good work. Exactly what we needed.\""
    },
    {
      "id": "push_deep",
      "text": "[Risky] Push deep, get close to enemy positions (Scouting 25+)",
      "risk": "risky",
      "skill_check": { "skill": "scouting", "difficulty": 25 },
      "fatigue": 3,
      "success": {
        "xp": { "scouting": 40, "tactics": 25 },
        "gold": 50,
        "rep": 3,
        "outcome": "You slip past their outer sentries and get within bowshot of their camp. You count tents, horses, supply wagons. This is valuable intelligence. The captain personally thanks you — and presses a small purse into your hand. \"The lord rewards those who take risks.\""
      },
      "failure": {
        "xp": { "scouting": 15 },
        "discipline": 1,
        "injury_chance": 20,
        "outcome": "You push too far. A sentry spots movement in the brush. You barely escape, arrows whistling past. You return with little to show for it. {LANCE_LEADER_SHORT} is not pleased. \"Next time, know your limits.\""
      }
    },
    {
      "id": "climb_high",
      "text": "[Physical] Climb high ground for overview (Athletics 20+)",
      "risk": "safe",
      "skill_check": { "skill": "athletics", "difficulty": 20 },
      "fatigue": 3,
      "success": {
        "xp": { "scouting": 30, "athletics": 20 },
        "gold": 25,
        "rep": 2,
        "outcome": "The climb is brutal, but the view is worth it. You can see for miles — the enemy camp, their picket lines, even their morning cook fires. You sketch a rough map and descend with valuable intelligence. The sergeant approves a small bonus for your initiative."
      },
      "failure": {
        "xp": { "athletics": 10 },
        "injury_chance": 15,
        "outcome": "Halfway up, loose rock gives way. You scramble for purchase, scraping your hands raw. You reach the top but you're exhausted and the view is obscured by morning mist. A wasted effort."
      }
    },
    {
      "id": "quick_sweep",
      "text": "[Lazy] Quick sweep, report early",
      "risk": "safe",
      "fatigue": 1,
      "rewards": {
        "xp": { "scouting": 10 },
        "gold": 0,
        "rep": -1
      },
      "outcome": "You make a cursory loop and head back. Your report is thin — 'Terrain's rough, didn't see enemies.' {LANCE_LEADER_SHORT} frowns. \"That's it? I expected better from you.\" You've saved energy but earned nothing."
    }
  ]
}
```

---

### 2. Enemy Patrol Spotted

**Original ID**: `scout_enemy_contact`
**Continues to Higher Tiers**: Yes

**REWRITTEN VERSION (T1-T2 Enlisted)**:

```json
{
  "id": "scout_enemy_contact_t1",
  "tier_range": { "min": 1, "max": 2 },
  "title": "Enemy Patrol Spotted",
  "setup": "Movement ahead. {ENEMY_FACTION_ADJECTIVE} patrol — four riders, maybe five. They haven't seen you yet.\n\nYou're alone out here. Your choice.",
  
  "options": [
    {
      "id": "observe",
      "text": "[Safe] Stay hidden, observe and count",
      "risk": "safe",
      "fatigue": 1,
      "rewards": {
        "xp": { "scouting": 30 },
        "gold": 0,
        "rep": 1
      },
      "outcome": "You press into the undergrowth and watch. Five riders, light cavalry, armed with bows. Scouts like you. You note their direction and timing, then slip away unseen. Solid intelligence for the captain."
    },
    {
      "id": "shadow",
      "text": "[Risky] Shadow them back to their camp (Scouting 30+)",
      "risk": "risky",
      "skill_check": { "skill": "scouting", "difficulty": 30 },
      "fatigue": 4,
      "success": {
        "xp": { "scouting": 50, "tactics": 30 },
        "gold": 75,
        "rep": 4,
        "outcome": "Hours of careful trailing. You learn their rotation patterns, their camp location, even overhear them discussing supply problems. This is gold. The captain rewards you handsomely. \"This could change the battle.\""
      },
      "failure": {
        "xp": { "scouting": 15 },
        "rep": -1,
        "injury_chance": 25,
        "outcome": "One of them doubles back. Eyes meet across the clearing. You run. They pursue. An arrow catches your arm. You escape, but barely, and you've learned nothing of value. The surgeon mutters while stitching you up."
      }
    },
    {
      "id": "distract",
      "text": "[Bold] Create distraction, lead them away from army (Athletics 25+)",
      "risk": "risky",
      "skill_check": { "skill": "athletics", "difficulty": 25 },
      "fatigue": 3,
      "success": {
        "xp": { "athletics": 35, "scouting": 25 },
        "gold": 50,
        "rep": 3,
        "outcome": "You let them see you, then run — leading them on a wild chase in the wrong direction. By the time they give up, they're miles from our column. You've bought the army valuable time. {LANCE_LEADER_SHORT} claps you on the back. \"Brave. Stupid, but brave.\""
      },
      "failure": {
        "xp": { "athletics": 15 },
        "discipline": 1,
        "injury_chance": 30,
        "outcome": "Your distraction works — but you can't outrun horses. They run you down and you barely escape with a sword cut across your back. The surgeon is busy for an hour. {LANCE_LEADER_SHORT} is not amused. \"Heroics get men killed.\""
      }
    },
    {
      "id": "flee",
      "text": "[Coward] Slip away immediately",
      "risk": "safe",
      "fatigue": 1,
      "rewards": {
        "xp": { "scouting": 10 },
        "gold": 0,
        "rep": 0
      },
      "outcome": "You melt back into the trees and circle wide. Safe, but you learned almost nothing. \"Saw a patrol, ran away\" isn't the report of a valued scout."
    }
  ]
}
```

---

### 3. Foraging Route

**Original ID**: `scout_night_patrol`
**Continues to Higher Tiers**: Yes

**REWRITTEN VERSION (T1-T2 Enlisted)**:

```json
{
  "id": "scout_foraging_route_t1",
  "tier_range": { "min": 1, "max": 2 },
  "title": "Foraging Route",
  "setup": "Supplies are thin. {SECOND_SHORT} needs you to find foraging opportunities — farms, orchards, game trails, anything.\n\n\"Don't come back empty-handed.\"",
  
  "options": [
    {
      "id": "systematic",
      "text": "[Thorough] Systematic search, cover all ground",
      "risk": "safe",
      "fatigue": 3,
      "rewards": {
        "xp": { "scouting": 25, "steward": 20 },
        "gold": 30,
        "rep": 2
      },
      "outcome": "You quarter the terrain methodically. By afternoon you've found an abandoned orchard, a stream full of fish, and a meadow where deer bed down at dusk. The quartermaster is pleased. \"Good eyes. Here's something for your trouble.\""
    },
    {
      "id": "locals",
      "text": "[Social] Ask locals, if friendly territory (Charm 20+)",
      "risk": "safe",
      "skill_check": { "skill": "charm", "difficulty": 20 },
      "fatigue": 1,
      "success": {
        "xp": { "charm": 25, "scouting": 15 },
        "gold": 40,
        "rep": 2,
        "outcome": "A farmer's wife points you toward a hidden spring and a grove of nut trees. \"Don't tell the taxman,\" she says with a wink. You return with directions to a week's worth of provisions. The quartermaster slips you a few coins."
      },
      "failure": {
        "xp": { "charm": 10 },
        "rep": -1,
        "outcome": "The villagers eye you with suspicion. \"Soldiers stole our pigs last month,\" one says. \"Why should we help you?\" You leave empty-handed and feeling the weight of their hostility."
      }
    },
    {
      "id": "hunt",
      "text": "[Hunter] Hunt along the way (Bow 25+)",
      "risk": "risky",
      "skill_check": { "skill": "bow", "difficulty": 25 },
      "fatigue": 2,
      "success": {
        "xp": { "bow": 30, "scouting": 20 },
        "gold": 50,
        "rep": 3,
        "outcome": "You bring down two rabbits and a pheasant. Fresh meat is always welcome. {LANCE_LEADER_SHORT} declares it goes in the pot tonight. \"Well done, hunter.\" The men's gratitude is worth as much as the coin."
      },
      "failure": {
        "xp": { "bow": 15 },
        "discipline": 1,
        "outcome": "You spend hours tracking deer that vanish like ghosts. You return with nothing, arrows wasted. The sergeant is unimpressed. \"Next time, focus on the actual mission.\""
      }
    },
    {
      "id": "requisition",
      "text": "[Corrupt] \"Requisition\" from a farm (Roguery 20+)",
      "risk": "corrupt",
      "skill_check": { "skill": "roguery", "difficulty": 20 },
      "fatigue": 1,
      "success": {
        "xp": { "roguery": 20, "scouting": 10 },
        "gold": 75,
        "heat": 3,
        "rep": -1,
        "outcome": "The farmer isn't home. You help yourself to a chicken and a sack of vegetables. Quick and quiet — nobody saw. But you know what you did, and the weight of it sits in your chest."
      },
      "failure": {
        "discipline": 2,
        "heat": 4,
        "rep": -3,
        "outcome": "The farmer's son sees you. He runs screaming. You flee with nothing, but complaints reach the sergeant by nightfall. You're on latrine duty for a week."
      }
    }
  ]
}
```

---

### 4. Night Infiltration

**Original ID**: `scout_route_finding`
**Continues to Higher Tiers**: Yes (becomes mission planning at T5+)

**REWRITTEN VERSION (T1-T2 Enlisted)**:

```json
{
  "id": "scout_infiltration_t1",
  "tier_range": { "min": 1, "max": 2 },
  "title": "Night Infiltration",
  "setup": "{LORD_NAME} wants someone inside. Not to fight — just to look. Guard rotations, weak points, supply levels.\n\n\"You're small enough to slip through. Interested?\"",
  
  "options": [
    {
      "id": "accept_careful",
      "text": "[Accept] Move careful, observe only (Scouting 30+)",
      "risk": "risky",
      "skill_check": { "skill": "scouting", "difficulty": 30 },
      "fatigue": 4,
      "success": {
        "xp": { "scouting": 50, "roguery": 30 },
        "gold": 100,
        "rep": 5,
        "outcome": "You slip through their perimeter like a ghost. For two hours you count sentries, note guard changes, sketch the layout. You escape unseen. {LORD_NAME} personally thanks you. \"Excellent work. This will save lives.\" The gold purse is substantial."
      },
      "failure": {
        "xp": { "scouting": 20 },
        "discipline": 2,
        "injury_chance": 30,
        "rep": -2,
        "outcome": "A dog smells you. Then the shouting starts. You scramble over a wall, arrows chasing you into the dark. You escape, but you've warned them we're watching. {LORD_NAME} is displeased."
      }
    },
    {
      "id": "accept_sabotage",
      "text": "[Bold] Try to sabotage something while inside (Roguery 35+)",
      "risk": "risky",
      "skill_check": { "skill": "roguery", "difficulty": 35 },
      "fatigue": 4,
      "success": {
        "xp": { "roguery": 60, "scouting": 30 },
        "gold": 150,
        "rep": 6,
        "outcome": "You find their supply tent and cut open the grain sacks. Rats will do the rest. When you slip back to friendly lines, you're carrying their battle plans — lifted from an officer's tent. {LORD_NAME} is speechless. Then he laughs and presses gold into your hands."
      },
      "failure": {
        "xp": { "roguery": 15 },
        "discipline": 3,
        "injury_chance": 40,
        "rep": -3,
        "gold": -50,
        "outcome": "You're spotted setting fire to their supplies. The chase is brutal. You escape over the wall but lose your purse and nearly your life. The mission is a disaster. {LORD_NAME}'s silence is worse than shouting."
      }
    },
    {
      "id": "decline",
      "text": "[Refuse] Decline — too dangerous",
      "risk": "safe",
      "fatigue": 0,
      "rewards": {
        "xp": {},
        "gold": 0,
        "rep": -1
      },
      "outcome": "\"Too dangerous,\" you say. {LORD_NAME}'s expression doesn't change, but you see the disappointment. \"Very well. Someone else will be asked.\" You're safe, but you've marked yourself as cautious."
    },
    {
      "id": "suggest_other",
      "text": "[Deflect] Suggest someone else",
      "risk": "safe",
      "fatigue": 0,
      "rewards": {
        "xp": {},
        "gold": 0,
        "rep": 0
      },
      "outcome": "You mention {LANCE_MATE_NAME} — smaller, quicker. {LORD_NAME} considers. \"Perhaps. But I was asking you.\" The conversation ends. You've neither gained nor lost, but the opportunity is gone."
    }
  ]
}
```

---

### 5. Lost Patrol

**Original ID**: `scout_map_update`
**Continues to Higher Tiers**: Yes

**REWRITTEN VERSION (T1-T2 Enlisted)**:

```json
{
  "id": "scout_lost_patrol_t1",
  "tier_range": { "min": 1, "max": 2 },
  "title": "Lost Patrol",
  "setup": "A patrol hasn't reported back. Six soldiers, including {LANCE_MATE_NAME}'s friend.\n\n{LANCE_LEADER_SHORT} looks at you. \"You know the ground. Find them.\"",
  
  "options": [
    {
      "id": "systematic",
      "text": "[Thorough] Track them systematically (Scouting 25+)",
      "risk": "safe",
      "skill_check": { "skill": "scouting", "difficulty": 25 },
      "fatigue": 4,
      "success": {
        "xp": { "scouting": 40, "tactics": 20 },
        "gold": 50,
        "rep": 4,
        "outcome": "You find their trail and follow it for hours. They're holed up in a farmhouse, wounded but alive — ambushed by bandits. You guide a relief party back. {LANCE_MATE_NAME}'s friend claps your shoulder with tears in his eyes. The sergeant presses coin into your palm."
      },
      "failure": {
        "xp": { "scouting": 20 },
        "rep": 1,
        "outcome": "The trail goes cold at a stream crossing. You search until dark but find nothing. They were found by another search party. You did your best, but it wasn't enough."
      }
    },
    {
      "id": "fast",
      "text": "[Speed] Move fast, cover more ground (Athletics 25+)",
      "risk": "risky",
      "skill_check": { "skill": "athletics", "difficulty": 25 },
      "fatigue": 3,
      "success": {
        "xp": { "athletics": 35, "scouting": 25 },
        "gold": 40,
        "rep": 3,
        "outcome": "You run. For hours. Your lungs burn but you find them — pinned down by bandits in a ravine. Your shouts bring help. Two dead, four alive. It could have been worse. {LANCE_LEADER_SHORT} acknowledges your effort."
      },
      "failure": {
        "xp": { "athletics": 15 },
        "injury_chance": 20,
        "rep": -1,
        "outcome": "Speed costs you accuracy. You cover ground but miss their trail entirely. By the time you double back, another team has found them. Your haste accomplished nothing."
      }
    },
    {
      "id": "group",
      "text": "[Careful] Bring help, search as a group",
      "risk": "safe",
      "fatigue": 2,
      "rewards": {
        "xp": { "scouting": 20, "leadership": 15 },
        "gold": 20,
        "rep": 2
      },
      "outcome": "You organize a proper search party. It takes longer, but you find them together — and have the numbers to fight off the bandits still lingering nearby. Slow but effective."
    },
    {
      "id": "report_lost",
      "text": "[Cold] Report them lost, conserve resources",
      "risk": "safe",
      "fatigue": 0,
      "rewards": {
        "xp": {},
        "gold": 0,
        "rep": -3,
        "discipline": 0
      },
      "outcome": "You tell {LANCE_LEADER_SHORT} they're probably dead — no point wasting more men. The look in his eyes is cold. \"We don't leave our own.\" Someone else goes. They're found alive. You are not looked at the same way after that."
    }
  ]
}
```

---

## QUARTERMASTER DUTY EVENTS (T1-T2)

### 1. Supply Inventory

**Original ID**: `qm_supply_inventory`
**Continues to Higher Tiers**: Yes (role changes to supervision/management)

**REWRITTEN VERSION (T1-T2 Enlisted)**:

```json
{
  "id": "qm_supply_inventory_t1",
  "tier_range": { "min": 1, "max": 2 },
  "title": "Supply Inventory",
  "setup": "{LANCE_LEADER_SHORT} catches your eye. \"Quartermaster duty, {PLAYER_NAME}. We need a full count before we resupply. Get it done.\"\n\nThe supply wagons are a mess. Someone's been helping themselves, or the count was wrong to begin with.",
  
  "options": [
    {
      "id": "count_properly",
      "text": "[Honest] Count everything properly, note discrepancies",
      "risk": "safe",
      "fatigue": 3,
      "rewards": {
        "xp": { "steward": 30, "trade": 15 },
        "gold": 25,
        "rep": 2
      },
      "outcome": "Hours of counting, weighing, checking seals. You find three barrels short and salt that's been cut with sand. Your report is thorough. The quartermaster is impressed. \"Good eyes. Here's something for your trouble. And I'll remember your name.\""
    },
    {
      "id": "quick_estimate",
      "text": "[Lazy] Quick count, estimate the rest (Steward 20+)",
      "risk": "risky",
      "skill_check": { "skill": "steward", "difficulty": 20 },
      "fatigue": 1,
      "success": {
        "xp": { "steward": 20 },
        "gold": 0,
        "rep": 0,
        "outcome": "You sample a few wagons and extrapolate. Your numbers are close enough that nobody questions them. Quick and dirty, but it works."
      },
      "failure": {
        "discipline": 2,
        "rep": -2,
        "outcome": "Your estimates are wildly wrong. When we run short of flour a week later, the quartermaster remembers who did the count. You're on punishment detail."
      }
    },
    {
      "id": "cover_shortage",
      "text": "[Pay] Cover the shortage from your own coin",
      "risk": "safe",
      "fatigue": 1,
      "rewards": {
        "xp": { "steward": 25, "charm": 15 },
        "gold": -50,
        "rep": 3
      },
      "outcome": "The shortage is real, but you pocket the difference from your own coin. Fifty gold lighter, but the books balance and no one asks questions. Sometimes reputation costs money."
    },
    {
      "id": "adjust_numbers",
      "text": "[Corrupt] Adjust the numbers to hide the shortage",
      "risk": "corrupt",
      "fatigue": 0,
      "rewards": {
        "xp": { "steward": 20, "roguery": 20 },
        "gold": 0,
        "heat": 3,
        "rep": 0
      },
      "outcome": "You change a three to an eight, scratch out a line. The books balance now. But you know, and if anyone ever checks the original manifests... Heat rises in your chest. This is how corruption starts."
    }
  ]
}
```

---

### 2. Merchant Negotiation

**Original ID**: `qm_merchant_negotiation`
**Continues to Higher Tiers**: Yes (becomes major supply contracts at T5+)

**REWRITTEN VERSION (T1-T2 Enlisted)**:

```json
{
  "id": "qm_merchant_t1",
  "tier_range": { "min": 1, "max": 2 },
  "title": "Merchant Negotiation",
  "setup": "The merchant knows we're desperate. He's asking double for grain, and {LORD_NAME}'s coffers aren't bottomless.\n\n\"Take it or leave it,\" he shrugs. \"There's a war on.\"",
  
  "options": [
    {
      "id": "pay_price",
      "text": "[Accept] Pay the price, secure supplies",
      "risk": "safe",
      "fatigue": 0,
      "rewards": {
        "xp": { "steward": 20 },
        "gold": 0,
        "rep": 0
      },
      "outcome": "You pay. The army eats. But the quartermaster shakes his head when he sees the receipts. \"Highway robbery.\" At least we won't starve."
    },
    {
      "id": "haggle",
      "text": "[Negotiate] Haggle hard, refuse to be gouged (Trade 25+)",
      "risk": "risky",
      "skill_check": { "skill": "trade", "difficulty": 25 },
      "fatigue": 1,
      "success": {
        "xp": { "trade": 40 },
        "gold": 40,
        "rep": 3,
        "outcome": "You talk, argue, walk away twice. Finally he meets you in the middle. You've saved the army forty gold. The quartermaster nods approvingly. \"You've got a gift for this. Here's your cut.\""
      },
      "failure": {
        "xp": { "trade": 15 },
        "rep": -1,
        "outcome": "The merchant is unmoved. \"Fine, don't buy. Someone else will.\" You're forced to pay full price anyway, and you've wasted time. The quartermaster is not impressed."
      }
    },
    {
      "id": "alternative",
      "text": "[Scout] Find alternative suppliers (Scouting 20+)",
      "risk": "safe",
      "skill_check": { "skill": "scouting", "difficulty": 20 },
      "fatigue": 3,
      "success": {
        "xp": { "scouting": 25, "trade": 20 },
        "gold": 30,
        "rep": 2,
        "outcome": "You spend the day asking around. A farmer's wife has grain to sell — her husband was conscripted and she needs coin. Fair price, good quality. You've undercut the merchant and helped a widow."
      },
      "failure": {
        "xp": { "scouting": 10 },
        "fatigue": 3,
        "outcome": "You search all day and find nothing. By evening, the merchant's price has gone up. Wasted effort."
      }
    },
    {
      "id": "intimidate",
      "text": "[Threaten] \"Suggest\" he reconsider (Roguery 25+)",
      "risk": "risky",
      "skill_check": { "skill": "roguery", "difficulty": 25 },
      "fatigue": 0,
      "success": {
        "xp": { "roguery": 35 },
        "gold": 50,
        "heat": 2,
        "rep": 0,
        "outcome": "You lean close. \"The lord doesn't like being gouged. Accidents happen on these roads.\" The merchant pales. The price drops. You've saved coin, but your methods are noted."
      },
      "failure": {
        "discipline": 3,
        "rep": -3,
        "heat": 3,
        "outcome": "The merchant runs to the town guard. Within the hour, you're explaining yourself to the sergeant. Threatening civilians reflects poorly on the army. You're on latrine duty for a week."
      }
    }
  ]
}
```

---

## ENGINEER DUTY EVENTS (T1-T2)

### 1. Fortification Work

**Original ID**: `eng_fortification_work`
**Continues to Higher Tiers**: Yes (becomes design/supervision at T5+)

**REWRITTEN VERSION (T1-T2 Enlisted)**:

```json
{
  "id": "eng_fortification_t1",
  "tier_range": { "min": 1, "max": 2 },
  "title": "Fortification Work",
  "setup": "{LORD_NAME} wants earthworks by nightfall. Trenches, palisades, something the enemy can't just ride through.\n\nThe ground is hard. The men are tired. The deadline is real.",
  
  "options": [
    {
      "id": "lead_by_example",
      "text": "[Physical] Work alongside the men, lead by example",
      "risk": "safe",
      "fatigue": 4,
      "rewards": {
        "xp": { "engineering": 35, "athletics": 25 },
        "gold": 0,
        "rep": 4
      },
      "injury_chance": 10,
      "outcome": "You dig until your hands blister, then dig more. The men see you suffering alongside them. By nightfall, the earthworks are done. Your back aches for days, but the lance respects you more."
    },
    {
      "id": "supervise",
      "text": "[Smart] Supervise and direct, maximize efficiency (Engineering 25+)",
      "risk": "safe",
      "skill_check": { "skill": "engineering", "difficulty": 25 },
      "fatigue": 2,
      "success": {
        "xp": { "engineering": 40, "leadership": 20 },
        "gold": 25,
        "rep": 2,
        "outcome": "You organize the work crews, set up a rotation for breaks, identify the hardest ground and assign the strongest men. The work finishes early. The sergeant is impressed. \"You've got a head for this.\""
      },
      "failure": {
        "xp": { "engineering": 20 },
        "rep": -1,
        "outcome": "Your organization falls apart. Men standing around, tools getting lost. The work gets done, but barely, and the sergeant has words with you afterward."
      }
    },
    {
      "id": "focus_critical",
      "text": "[Tactical] Focus on critical sections only",
      "risk": "safe",
      "fatigue": 2,
      "rewards": {
        "xp": { "engineering": 30, "tactics": 20 },
        "gold": 0,
        "rep": 1
      },
      "outcome": "You can't do everything, so you prioritize. The main approach gets the deepest trench. Flanks get wooden stakes. It's not complete coverage, but it's defensible. The captain approves your thinking."
    },
    {
      "id": "push_hard",
      "text": "[Driver] Push the men harder, finish early (Leadership 25+)",
      "risk": "risky",
      "skill_check": { "skill": "leadership", "difficulty": 25 },
      "fatigue": 3,
      "success": {
        "xp": { "engineering": 35, "leadership": 25 },
        "gold": 30,
        "rep": 1,
        "outcome": "You drive them hard. Shouts, threats, curses. The work finishes two hours early. The men are exhausted but the camp is secure. The sergeant tosses you a few coins. \"Effective. Just don't break them.\""
      },
      "failure": {
        "discipline": 1,
        "rep": -2,
        "outcome": "You push too hard. A man collapses from heat. Another cuts himself with a pickaxe. The work gets done, but there's grumbling. The sergeant pulls you aside. \"They're soldiers, not slaves.\""
      }
    }
  ]
}
```

---

## GUARD DUTY EVENTS (T1-T2) - NEW

### 1. Night Watch

**NEW EVENT** (replacing simple popup)
**Continues to Higher Tiers**: Yes (becomes assignment/planning at T3+)

```json
{
  "id": "guard_night_watch_t1",
  "tier_range": { "min": 1, "max": 2 },
  "title": "Night Watch",
  "setup": "You're posted at the north gate. Hours pass slowly in the dark.\n\nAround midnight, you hear something — movement near the supply wagons. Too deliberate to be an animal.",
  
  "options": [
    {
      "id": "investigate",
      "text": "[Investigate] Move quietly toward the sound (Scouting 25+)",
      "risk": "risky",
      "skill_check": { "skill": "scouting", "difficulty": 25 },
      "fatigue": 1,
      "success": {
        "xp": { "scouting": 30 },
        "gold": 75,
        "rep": 4,
        "outcome": "You catch a deserter red-handed, loading supplies onto a mule. He surrenders without a fight. The sergeant rewards you handsomely. \"Good work. This is what proper guards do.\""
      },
      "failure": {
        "xp": { "scouting": 10 },
        "discipline": 1,
        "rep": -1,
        "outcome": "You stumble over a rope in the dark. The noise alerts whoever was there — they're gone when you arrive. The supplies are disturbed but nothing's missing. The sergeant isn't pleased you let them escape."
      }
    },
    {
      "id": "alarm",
      "text": "[Safe] Raise the alarm immediately",
      "risk": "safe",
      "fatigue": 0,
      "rewards": {
        "xp": { "leadership": 15 },
        "gold": 0,
        "rep": 1
      },
      "outcome": "You shout for the guard. Within minutes, torches are lit and men are searching. They find nothing — the intruder fled at your first shout. No harm done, but no glory either. \"Better safe than sorry,\" the sergeant shrugs."
    },
    {
      "id": "ignore",
      "text": "[Lazy] Ignore it — probably an animal",
      "risk": "risky",
      "fatigue": 0,
      "success_chance": 30,
      "success": {
        "xp": {},
        "gold": 0,
        "rep": 0,
        "outcome": "You stay at your post. Morning comes. Nothing missing. Maybe it was nothing. Or maybe you got lucky."
      },
      "failure": {
        "discipline": 3,
        "rep": -3,
        "heat": 2,
        "gold": -30,
        "outcome": "In the morning, the quartermaster is furious. Three sacks of grain, a barrel of salt — gone. And you were on watch. The sergeant docks your pay and adds you to the punishment roster."
      }
    },
    {
      "id": "profit",
      "text": "[Corrupt] Let them finish — for a cut",
      "risk": "corrupt",
      "fatigue": 0,
      "rewards": {
        "xp": { "roguery": 25 },
        "gold": 50,
        "heat": 4,
        "rep": 0
      },
      "outcome": "You approach quietly. It's a supply clerk, skimming supplies for black market sale. He offers to cut you in. Fifty gold finds its way into your pocket. But you're complicit now. And if he's caught, he knows your face."
    }
  ]
}
```

---

## WORK DETAIL EVENTS (T1-T2) - NEW

### 1. Rusty Weapon Discovery

**NEW EVENT** (expanded from popup)
**T1-T2 Only**: Yes (grunt work)

```json
{
  "id": "work_rusty_weapon_t1",
  "tier_range": { "min": 1, "max": 2 },
  "title": "Work Detail - Rusty Weapon",
  "setup": "While maintaining equipment, you find a rusted but salvageable cavalry sword buried at the bottom of an old supply wagon. Good steel under the rust.\n\nNo one's claimed it. No one saw you find it.",
  
  "options": [
    {
      "id": "repair",
      "text": "[Skill] Repair it yourself (Smithing 25+)",
      "risk": "risky",
      "skill_check": { "skill": "smithing", "difficulty": 25 },
      "fatigue": 2,
      "success": {
        "xp": { "smithing": 35 },
        "gold": 100,
        "rep": 0,
        "outcome": "You spend your evening with oil, stone, and patience. The blade cleans up beautifully — old work, but quality. You sell it to a passing merchant for a hundred gold. Nobody needs to know."
      },
      "failure": {
        "xp": { "smithing": 15 },
        "gold": -25,
        "outcome": "You grind too hard and nick the edge. Trying to fix it, you make it worse. The sword is ruined. You toss it away and feel the loss of what could have been."
      }
    },
    {
      "id": "report",
      "text": "[Honest] Report it to the quartermaster",
      "risk": "safe",
      "fatigue": 0,
      "rewards": {
        "xp": { "steward": 15 },
        "gold": 25,
        "rep": 2
      },
      "outcome": "You turn it in. The quartermaster nods approvingly and hands you a small finder's fee. \"Honest soldiers are rare. I'll remember this.\" Twenty-five gold and a good reputation."
    },
    {
      "id": "hide",
      "text": "[Corrupt] Keep it hidden — sell to a merchant later",
      "risk": "corrupt",
      "fatigue": 0,
      "rewards": {
        "xp": { "roguery": 20 },
        "gold": 75,
        "heat": 3,
        "rep": 0
      },
      "outcome": "You wrap it in a spare blanket and stash it in your kit. When the army reaches town, you find a back-alley dealer. Seventy-five gold, no questions. But you're stealing from the army now."
    },
    {
      "id": "leave",
      "text": "[Ignore] Leave it — not worth the trouble",
      "risk": "safe",
      "fatigue": 0,
      "rewards": {
        "xp": {},
        "gold": 0,
        "rep": 0
      },
      "outcome": "You toss it back in the wagon. Someone else's problem. Or opportunity."
    }
  ]
}
```

---

## SUMMARY: T1-T2 EVENT INVENTORY

### Scout Duties (5 events)
| ID | Title | Gold Range | Key Skills | Continues to Higher Tiers |
|----|-------|------------|------------|---------------------------|
| scout_terrain_recon_t1 | Terrain Reconnaissance | 0-100g | Scouting, Athletics | Yes (T3+: Lead scout team) |
| scout_enemy_contact_t1 | Enemy Patrol Spotted | 0-75g | Scouting, Athletics | Yes |
| scout_foraging_route_t1 | Foraging Route | 0-75g | Scouting, Bow, Charm | Yes |
| scout_infiltration_t1 | Night Infiltration | 0-150g | Scouting, Roguery | Yes (T5+: Plan infiltration) |
| scout_lost_patrol_t1 | Lost Patrol | 0-50g | Scouting, Athletics | Yes |

### Quartermaster Duties (5 events)
| ID | Title | Gold Range | Key Skills | Continues to Higher Tiers |
|----|-------|------------|------------|---------------------------|
| qm_supply_inventory_t1 | Supply Inventory | -50 to 25g | Steward, Trade | Yes |
| qm_merchant_t1 | Merchant Negotiation | -50 to 50g | Trade, Roguery | Yes |
| qm_spoiled_supplies_t1 | Spoiled Supplies | 0-30g | Steward, Medicine | Yes |
| qm_equipment_dist_t1 | Equipment Distribution | 0-20g | Steward, Leadership | Yes |
| qm_missing_wagon_t1 | The Missing Wagon | 0-50g | Steward, Scouting | Yes |

### Engineer Duties (5 events)
| ID | Title | Gold Range | Key Skills | Continues to Higher Tiers |
|----|-------|------------|------------|---------------------------|
| eng_fortification_t1 | Fortification Work | 0-30g | Engineering, Athletics | Yes |
| eng_siege_repair_t1 | Siege Engine Repair | 0-40g | Engineering, Smithing | Yes |
| eng_bridge_t1 | Bridge Assessment | 0-25g | Engineering, Scouting | Yes |
| eng_mining_t1 | Mining Operation | 0-50g | Engineering, Athletics | Yes |
| eng_camp_layout_t1 | Camp Layout | 0-20g | Engineering, Steward | Yes |

### Guard Duty (NEW - 3 events)
| ID | Title | Gold Range | Key Skills | Continues to Higher Tiers |
|----|-------|------------|------------|---------------------------|
| guard_night_watch_t1 | Night Watch | -30 to 75g | Scouting, Roguery | Yes (T3+: Assign rotation) |
| guard_perimeter_t1 | Perimeter Check | 0-50g | Scouting, Athletics | Yes |
| guard_supply_t1 | Supply Guard | -25 to 60g | Scouting, Trade | Yes |

### Work Detail (NEW - 3 events)
| ID | Title | Gold Range | Key Skills | Continues to Higher Tiers |
|----|-------|------------|------------|---------------------------|
| work_rusty_weapon_t1 | Rusty Weapon Discovery | -25 to 100g | Smithing, Roguery | No (T1-T2 only) |
| work_hidden_cache_t1 | Hidden Cache | 0-150g | Scouting, Roguery | No (T1-T2 only) |
| work_equipment_maint_t1 | Equipment Maintenance | 0-25g | Smithing, Steward | No (T1-T2 only) |

### Grunt Work (NEW - T1-T2 EXCLUSIVE)
| ID | Title | Gold Range | Key Skills | Continues to Higher Tiers |
|----|-------|------------|------------|---------------------------|
| grunt_stable_duty_t1 | Stable Duty | 0-20g | Riding, Athletics | No |
| grunt_kitchen_duty_t1 | Kitchen Duty | 0-15g | Steward, Charm | No |
| grunt_latrine_duty_t1 | Latrine Duty | 0-10g | Athletics | No |
| grunt_firewood_t1 | Firewood Detail | 0-25g | Athletics, Scouting | No |
| grunt_water_carry_t1 | Water Carry | 0-15g | Athletics | No |

---

## NEXT STEPS

1. **T3-T4 Version**: Create NCO versions where player ASSIGNS/SUPERVISES these duties
2. **T5-T6 Version**: Create Officer versions where player PLANS these operations
3. **T7-T9 Version**: Create Commander versions (or remove grunt duties entirely)
4. **Chaining**: Add `triggers_event` for follow-up events (e.g., finding rusty weapon → Lance Leader Praise)
5. **Testing**: Verify skill check difficulties are appropriate for tier progression

---

## NOTES FOR IMPLEMENTATION

### Skill Check Difficulties by Tier
| Difficulty | T1-T2 | T3-T4 | T5-T6 | T7-T9 |
|------------|-------|-------|-------|-------|
| Easy | 15 | 25 | 35 | 50 |
| Medium | 25 | 35 | 50 | 70 |
| Hard | 35 | 50 | 70 | 90 |
| Expert | 45 | 65 | 85 | 110 |

### Gold Rewards by Tier
| Size | T1-T2 | T3-T4 | T5-T6 | T7-T9 |
|------|-------|-------|-------|-------|
| Small | 10-25g | 25-75g | 100-200g | 300-600g |
| Medium | 25-75g | 75-200g | 200-500g | 500-1200g |
| Large | 75-150g | 200-500g | 500-1200g | 1200-3000g |

### XP Rewards by Tier
| Size | T1-T2 | T3-T4 | T5-T6 | T7-T9 |
|------|-------|-------|-------|-------|
| Small | 10-20 | 20-35 | 35-60 | 60-100 |
| Medium | 20-35 | 35-60 | 60-100 | 100-150 |
| Large | 35-50 | 60-100 | 100-150 | 150-250 |

