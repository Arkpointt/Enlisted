# Lance Life Events — Complete Content Library

This document contains all event content for the Lance Life system. Events are organized by type and ready for implementation.

---

## Table of Contents

1. [Duty Events](#duty-events) (50 events)
   - Quartermaster (5)
   - Scout (5)
   - Field Medic (5)
   - Messenger (5)
   - Armorer (5)
   - Runner (5)
   - Lookout (5)
   - Engineer (5)
   - Boatswain (5) — Naval
   - Navigator (5) — Naval
2. [Training Events](#training-events) (16 events)
   - Infantry (4)
   - Cavalry (4)
   - Archer (4)
   - Naval (4)
3. [General Events by Time of Day](#general-events-by-time-of-day) (18 events)
   - Dawn (3)
   - Morning/Afternoon (3)
   - Evening (3)
   - Dusk (3)
   - Night (3)
   - Late Night (3)

---

# Duty Events

Each duty has 5 events that fire based on relevant triggers. Players must have the duty assigned to see these events.

## Delivery Metadata — Duty Events

| Field | Value |
|-------|-------|
| **Delivery** | Automatic |
| **Menu** | None (indicator shown in Report for Duty menu) |
| **Triggered By** | System fires when trigger conditions + `ai_safe` |
| **Presentation** | Inquiry Popup |

**Implementation:** Events fire automatically via `LanceLifeEventManager.CheckDutyEvents()` during hourly tick. Player does NOT click to start these — they appear as popups when conditions match.

---

## Quartermaster Events

### QM-01: Supply Inventory

**Type:** Duty Event
**Duty Required:** Quartermaster
**Time of Day:** Morning, Afternoon
**Trigger:** `entered_settlement` OR `weekly_tick`

**Setup:**
{LANCE_LEADER_SHORT} catches your eye. "Quartermaster duty, {PLAYER_NAME}. We need a full count before we resupply. Get it done."

The supply wagons are a mess. Someone's been helping themselves, or the count was wrong to begin with.

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Count everything properly, note discrepancies | Safe | +2 Fatigue | +30 Steward, +15 Trade | — |
| Do a quick count, estimate the rest | Risky | +1 Fatigue | +15 Steward | — |
| Cover the shortage from your own coin | Safe | −25 Gold | +25 Steward, +10 Charm, +relation with leader | — |
| Adjust the numbers to hide the shortage | Corrupt | +2 Heat | +20 Steward, +15 Roguery | — |

**Escalation:** Hiding shortages repeatedly leads to camp-wide supply crisis event.

---

### QM-02: Merchant Negotiation

**Type:** Duty Event
**Duty Required:** Quartermaster
**Time of Day:** Morning, Afternoon
**Trigger:** `in_settlement` AND `logistics_strain > 30`

**Setup:**
The merchant knows we're desperate. He's asking double for grain, and {LORD_NAME}'s coffers aren't bottomless.

"Take it or leave it," he shrugs. "There's a war on."

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Pay the price, secure supplies | Safe | — | +20 Steward | — |
| Haggle hard, refuse to be gouged | Risky | — | +35 Trade (if success), supplies at fair price | — |
| Find alternative suppliers (takes time) | Safe | +2 Fatigue | +25 Trade, +15 Scouting | — |
| "Suggest" he reconsider (intimidate) | Risky | +1 Discipline | +30 Roguery (if success), cheap supplies | — |

**Escalation:** Failed intimidation leads to merchant refusing to deal with the army.

---

### QM-03: Spoiled Supplies

**Type:** Duty Event
**Duty Required:** Quartermaster
**Time of Day:** Morning
**Trigger:** `days_from_town > 5` AND `weekly_tick`

**Setup:**
The smell hits you before you open the barrel. Rot. A third of the salted meat has gone bad — either the salt wasn't enough or someone sold us garbage.

{SECOND_SHORT} is already there. "How bad?"

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Report it honestly, take the blame | Safe | — | +25 Steward, −relation with some | — |
| Salvage what you can, stretch the good stuff | Safe | +2 Fatigue | +30 Steward, +15 Medicine | — |
| Find who sold us this, document for later | Safe | +1 Fatigue | +20 Steward, +20 Trade | — |
| Mix the bad with the good, hope nobody notices | Corrupt | +3 Heat | +15 Roguery | — |

**Escalation:** If mixed, soldiers get sick → morale crisis event.

---

### QM-04: Equipment Distribution

**Type:** Duty Event
**Duty Required:** Quartermaster
**Time of Day:** Morning, Afternoon
**Trigger:** `after_battle` OR `new_recruits_joined`

**Setup:**
New equipment arrived, but there's not enough to go around. The veterans want first pick. The recruits need it more. {LANCE_LEADER_SHORT} left it to you.

"Your call, Quartermaster."

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Veterans first — they've earned it | Safe | — | +20 Steward, +relation veterans, −relation recruits | — |
| Recruits first — they'll die without it | Safe | — | +20 Steward, +relation recruits, −relation veterans | — |
| Split it fairly by lottery | Safe | +1 Fatigue | +25 Steward, +15 Leadership | — |
| Keep the best piece for yourself | Corrupt | +2 Heat | +20 Steward, better equipment | — |

**Escalation:** Playing favorites repeatedly causes lance friction event.

---

### QM-05: The Missing Wagon

**Type:** Duty Event
**Duty Required:** Quartermaster
**Time of Day:** Any
**Trigger:** `army_moving` AND `random_chance`

**Setup:**
A supply wagon didn't make it to camp. It was there at the last stop. {LORD_NAME} wants answers.

The driver claims bandits. The guards claim the driver ran off. Someone is lying.

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Investigate thoroughly, find the truth | Safe | +3 Fatigue | +35 Steward, +25 Scouting | — |
| Trust the guards, report bandits | Safe | — | +15 Steward | — |
| Trust the driver, blame the guards | Risky | — | +15 Steward, potential enemy in guards | — |
| Cover it up, absorb the loss quietly | Corrupt | +3 Heat | +20 Roguery | — |

**Escalation:** Finding the truth reveals embezzlement ring if investigated.

---

## Scout Events

### SC-01: Terrain Reconnaissance

**Type:** Duty Event
**Duty Required:** Scout
**Time of Day:** Dawn, Morning
**Trigger:** `before_battle` OR `army_moving_to_enemy`

**Setup:**
{LANCE_LEADER_SHORT} pulls you aside before dawn. "Scout duty. The captain needs to know what we're walking into. Terrain, cover, escape routes."

The enemy is somewhere ahead. The question is where.

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Standard reconnaissance, stay cautious | Safe | +2 Fatigue | +30 Scouting, +20 Tactics | — |
| Push deep, get close to enemy positions | Risky | +2 Fatigue | +45 Scouting, +30 Tactics | 15% Minor-Moderate |
| Climb high ground for overview | Safe | +3 Fatigue | +35 Scouting, +15 Athletics | 5% Minor (fall) |
| Quick sweep, report early | Safe | +1 Fatigue | +20 Scouting | — |

**Escalation:** Deep reconnaissance may trigger ambush event if unlucky.

---

### SC-02: Enemy Patrol Spotted

**Type:** Duty Event
**Duty Required:** Scout
**Time of Day:** Any
**Trigger:** `in_enemy_territory` AND `random_chance`

**Setup:**
Movement ahead. {ENEMY_FACTION_ADJECTIVE} patrol — four riders, maybe five. They haven't seen you yet.

You're alone out here. Your choice.

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Stay hidden, observe and count | Safe | +1 Fatigue | +35 Scouting, valuable intel | — |
| Shadow them back to their camp | Risky | +3 Fatigue | +50 Scouting, +25 Tactics, major intel | 10% Minor |
| Slip away, report what you saw | Safe | +1 Fatigue | +25 Scouting | — |
| Create a distraction, lead them away from army | Risky | +2 Fatigue | +40 Athletics, +30 Scouting | 20% Minor-Moderate |

**Escalation:** Shadowing may lead to discovering enemy ambush plans.

---

### SC-03: Foraging Route

**Type:** Duty Event
**Duty Required:** Scout
**Time of Day:** Morning, Afternoon
**Trigger:** `logistics_strain > 40` AND `not_in_settlement`

**Setup:**
Supplies are tight. {SECOND_SHORT} needs you to find foraging opportunities — farms, orchards, game trails, anything.

"Don't come back empty-handed."

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Systematic search, cover all ground | Safe | +3 Fatigue | +30 Scouting, +20 Steward, supplies found | — |
| Ask locals (if friendly territory) | Safe | +1 Fatigue | +20 Scouting, +25 Charm | — |
| Hunt along the way | Risky | +2 Fatigue | +25 Scouting, +30 Bow, food secured | 5% Minor |
| "Requisition" from a farm | Corrupt | +2 Heat | +20 Scouting, +15 Roguery, supplies | — |

**Escalation:** Requisitioning damages local relations and may trigger peasant resistance.

---

### SC-04: Night Infiltration

**Type:** Duty Event
**Duty Required:** Scout
**Time of Day:** Night
**Trigger:** `siege_ongoing` OR `before_major_battle`

**Setup:**
{LORD_NAME} wants someone inside. Not to fight — just to look. Guard rotations, weak points, supply levels.

"You're small enough to slip through. Interested?"

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Accept — move careful, observe only | Risky | +3 Fatigue | +50 Scouting, +30 Roguery, critical intel | 15% Minor-Moderate |
| Accept — try to sabotage something while inside | Risky | +3 Fatigue | +40 Scouting, +40 Roguery, sabotage bonus | 25% Moderate |
| Decline — too dangerous | Safe | — | — | — |
| Suggest someone else | Safe | — | −relation with lord | — |

**Escalation:** Getting caught triggers imprisonment or injury event.

---

### SC-05: Lost Patrol

**Type:** Duty Event
**Duty Required:** Scout
**Time of Day:** Any
**Trigger:** `after_battle` OR `army_scattered`

**Setup:**
A patrol hasn't reported back. Six soldiers, including {LANCE_MATE_NAME}'s friend. 

{LANCE_LEADER_SHORT} looks at you. "You know the ground. Find them."

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Track them systematically | Safe | +3 Fatigue | +35 Scouting, +20 Tactics | — |
| Move fast, cover more ground | Risky | +2 Fatigue | +30 Scouting, +25 Athletics | 10% Minor |
| Bring help, search as a group | Safe | +2 Fatigue | +25 Scouting, +15 Leadership | — |
| Report them lost, conserve resources | Safe | — | −relation with lance mates | — |

**Escalation:** Finding them reveals they're wounded/captured → rescue event.

---

## Field Medic Events

### MED-01: Battle Triage

**Type:** Duty Event
**Duty Required:** Field Medic
**Time of Day:** Any
**Trigger:** `after_battle` AND `casualties > 0`

**Setup:**
The fighting's done. The dying isn't. Wounded soldiers everywhere — more than you can treat. You have to choose who gets help first.

{VETERAN_1_NAME} is among them. So is a recruit you barely know.

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Triage by severity — worst first | Safe | +3 Fatigue | +40 Medicine, +20 Steward | — |
| Save who you can save — skip the hopeless | Safe | +2 Fatigue | +35 Medicine, more survive | — |
| Focus on {VETERAN_1_NAME} | Safe | +2 Fatigue | +30 Medicine, +relation veteran | — |
| Work until you drop | Risky | +5 Fatigue | +50 Medicine, +25 Leadership | 10% Exhaustion |

**Escalation:** Choices affect who survives → potential lance mate death notification.

---

### MED-02: Camp Sickness

**Type:** Duty Event
**Duty Required:** Field Medic
**Time of Day:** Morning
**Trigger:** `siege_ongoing` OR `days_from_town > 7`

**Setup:**
It started with one soldier. Now it's a dozen. Camp fever — or worse. The surgeon is overwhelmed.

"You've got steady hands, {PLAYER_NAME}. I need you in the sick tent."

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Work the sick tent all day | Safe | +3 Fatigue | +40 Medicine, contain spread | — |
| Focus on the worst cases | Safe | +2 Fatigue | +30 Medicine | — |
| Identify the source, stop the spread | Safe | +2 Fatigue | +25 Medicine, +25 Steward | — |
| Volunteer for plague duty (high exposure) | Risky | +3 Fatigue | +50 Medicine | 20% Illness (Mild-Moderate) |

**Escalation:** Failing to contain leads to army-wide sickness event.

---

### MED-03: Surgery Assistance

**Type:** Duty Event
**Duty Required:** Field Medic
**Time of Day:** Any
**Trigger:** `lance_mate_severe_wound` OR `after_heavy_battle`

**Setup:**
The surgeon's hands are full — literally. He's got a soldier on the table with an arrow in his gut. 

"Hold him down. Hand me what I need. Don't look away."

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Assist calmly, follow instructions | Safe | +2 Fatigue | +35 Medicine, soldier survives | — |
| Take initiative, anticipate needs | Risky | +2 Fatigue | +45 Medicine (if correct), soldier survives faster | — |
| Comfort the patient, keep him calm | Safe | +1 Fatigue | +25 Medicine, +20 Charm | — |
| Ask to learn — "show me how" | Safe | +3 Fatigue | +40 Medicine, +15 Engineering | — |

**Escalation:** Good performance unlocks advanced medical events.

---

### MED-04: Herb Gathering

**Type:** Duty Event
**Duty Required:** Field Medic
**Time of Day:** Morning, Afternoon
**Trigger:** `not_in_settlement` AND `medical_supplies_low`

**Setup:**
The surgeon's kit is running low. Bandages can be improvised, but medicine needs ingredients.

"Know your plants, {PLAYER_NAME}? We need wound-wort, fever-leaf, anything useful."

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Search methodically, identify carefully | Safe | +3 Fatigue | +30 Medicine, +20 Scouting, supplies | — |
| Quick gathering, common plants only | Safe | +1 Fatigue | +20 Medicine, basic supplies | — |
| Ask locals for guidance | Safe | +1 Fatigue | +25 Medicine, +20 Charm | — |
| Venture into dangerous terrain for rare herbs | Risky | +3 Fatigue | +40 Medicine, +25 Scouting, good supplies | 10% Minor |

**Escalation:** Rare herbs enable better treatment options in future events.

---

### MED-05: Infected Wound

**Type:** Duty Event
**Duty Required:** Field Medic
**Time of Day:** Any
**Trigger:** `lance_mate_wounded` AND `days_since_wound > 2`

**Setup:**
{SOLDIER_NAME}'s wound isn't healing right. The skin around it is hot, angry red. You've seen this before.

Without treatment, they'll lose the arm. Maybe more.

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Clean it properly, treat aggressively | Safe | +2 Fatigue | +35 Medicine, soldier recovers | — |
| Bring it to the surgeon's attention | Safe | +1 Fatigue | +20 Medicine, +relation surgeon | — |
| Try a poultice you learned from home | Risky | +1 Fatigue | +40 Medicine (if works), +30 if fails | — |
| Prepare them for amputation | Safe | +2 Fatigue | +30 Medicine, soldier lives but maimed | — |

**Escalation:** Successful treatment builds reputation as skilled medic.

---

## Messenger Events

### MSG-01: Urgent Dispatch

**Type:** Duty Event
**Duty Required:** Messenger
**Time of Day:** Any
**Trigger:** `in_army` AND `random_chance`

**Setup:**
{LORD_NAME} hands you a sealed letter. "To Lord {ALLIED_LORD} at {SETTLEMENT_NAME}. Fast as you can. Don't open it."

The roads aren't safe. They never are.

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Ride hard, main roads | Safe | +2 Fatigue | +30 Riding, +20 Athletics | — |
| Ride harder, push your horse | Risky | +3 Fatigue | +40 Riding, +25 Athletics, faster delivery | 10% Minor (fall) |
| Take back roads, avoid trouble | Safe | +3 Fatigue | +25 Riding, +30 Scouting | — |
| Read the letter first | Corrupt | +2 Heat | +20 Riding, +25 Roguery, secret knowledge | — |

**Escalation:** Reading dispatches repeatedly leads to being suspected of spying.

---

### MSG-02: Roadside Trouble

**Type:** Duty Event
**Duty Required:** Messenger
**Time of Day:** Morning, Afternoon
**Trigger:** `on_dispatch` AND `random_chance`

**Setup:**
Riders ahead. Could be bandits. Could be {ENEMY_FACTION_ADJECTIVE} scouts. Could be nothing.

Your message is more important than your life. {LORD_NAME} made that clear.

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Avoid — detour through rough terrain | Safe | +2 Fatigue | +25 Riding, +30 Scouting | — |
| Hide and wait them out | Safe | +1 Fatigue | +20 Scouting | — |
| Ride through fast, trust your horse | Risky | +2 Fatigue | +35 Riding, +20 Athletics | 15% Minor-Moderate |
| Destroy the message if captured | Safe | — | Message lost, duty protected | — |

**Escalation:** Getting caught leads to interrogation event.

---

### MSG-03: Wrong Place, Wrong Time

**Type:** Duty Event
**Duty Required:** Messenger
**Time of Day:** Any
**Trigger:** `approaching_battle` AND `on_dispatch`

**Setup:**
You rode into a battle. Not your battle — two lords fighting over something that doesn't concern you.

Your message won't deliver itself. But riding through that mess might kill you.

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Wait for it to end | Safe | +1 Fatigue | +15 Scouting, delayed delivery | — |
| Ride around, long way | Safe | +3 Fatigue | +25 Riding, +20 Scouting | — |
| Ride through the edge | Risky | +2 Fatigue | +35 Riding, +25 Athletics | 20% Minor-Moderate |
| Join the winning side briefly | Risky | +3 Fatigue | +30 Riding, +combat XP, gratitude | 25% Minor-Moderate |

**Escalation:** Joining a battle makes friends and enemies.

---

### MSG-04: Verbal Message

**Type:** Duty Event
**Duty Required:** Messenger
**Time of Day:** Any
**Trigger:** `in_army` AND `sensitive_orders`

**Setup:**
No letter this time. {LORD_NAME} leans close. "Memorize this. Word for word. Don't write it down."

The message is... complicated. And dangerous if overheard.

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Memorize it perfectly, repeat twice | Safe | +1 Fatigue | +30 Charm, +25 Steward | — |
| Ask clarifying questions | Safe | +1 Fatigue | +35 Charm, clearer message | — |
| Write it in code anyway | Risky | +1 Fatigue | +25 Charm, +30 Roguery | — |
| "I understand, my lord" (wing it) | Risky | +1 Fatigue | +20 Charm (if lucky), disaster if wrong | — |

**Escalation:** Delivering wrong message has serious consequences.

---

### MSG-05: The Reply

**Type:** Duty Event
**Duty Required:** Messenger
**Time of Day:** Any
**Trigger:** `delivered_message` AND `awaiting_reply`

**Setup:**
Lord {ALLIED_LORD} reads the message. His face changes. He looks at you.

"Tell your lord... no. Wait. Tell him this instead."

He gives you a reply that contradicts the original message.

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Carry the reply faithfully | Safe | +1 Fatigue | +25 Charm, duty done | — |
| Ask for clarification (diplomatic) | Safe | +1 Fatigue | +30 Charm, clearer understanding | — |
| Suggest putting it in writing | Safe | +1 Fatigue | +25 Charm, +20 Steward, evidence | — |
| Warn your lord privately about the tone | Safe | +1 Fatigue | +20 Charm, +relation with lord | — |

**Escalation:** Being trusted with sensitive replies leads to higher-stakes missions.

---

## Armorer Events

### ARM-01: Pre-Battle Inspection

**Type:** Duty Event
**Duty Required:** Armorer
**Time of Day:** Dawn, Morning
**Trigger:** `before_battle`

**Setup:**
Battle tomorrow — maybe today. {LANCE_LEADER_SHORT} wants every blade sharp, every strap tight.

"Check everything, {PLAYER_NAME}. I don't want anyone dying because their buckle broke."

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Thorough inspection, fix everything | Safe | +3 Fatigue | +35 Smithing, +25 Engineering | — |
| Focus on weapons, they matter most | Safe | +2 Fatigue | +30 Smithing | — |
| Quick check, trust the soldiers to maintain | Safe | +1 Fatigue | +15 Smithing | — |
| Work through the night | Risky | +4 Fatigue | +45 Smithing, +30 Engineering | 5% Minor (fatigue) |

**Escalation:** Thorough work prevents equipment failure event in battle.

---

### ARM-02: Broken Blade

**Type:** Duty Event
**Duty Required:** Armorer
**Time of Day:** Morning, Afternoon
**Trigger:** `after_battle`

**Setup:**
{VETERAN_1_NAME} brings you a sword — snapped clean in half. Good steel, but it failed at the wrong moment.

"Can you fix it? It was my father's."

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Reforge it properly (takes time) | Safe | +3 Fatigue | +40 Smithing, +relation veteran | — |
| Patch it — functional but not pretty | Safe | +2 Fatigue | +25 Smithing | — |
| Explain it's beyond repair, offer alternative | Safe | +1 Fatigue | +20 Smithing, +15 Charm | — |
| Claim materials are short, refuse | Safe | — | −relation veteran | — |

**Escalation:** Quality work builds reputation, brings more requests.

---

### ARM-03: Forge Fire

**Type:** Duty Event
**Duty Required:** Armorer
**Time of Day:** Morning, Afternoon
**Trigger:** `in_settlement` OR `camp_established`

**Setup:**
The field forge is temperamental today. Too hot, then too cold. The bellows need fixing and the coals are uneven.

Good steel needs good fire.

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Take time to fix it properly | Safe | +2 Fatigue | +30 Smithing, +25 Engineering | — |
| Work around it — adapt your technique | Risky | +2 Fatigue | +35 Smithing (if success), wasted materials if fail | — |
| Ask the local smith for help | Safe | +1 Fatigue | +20 Smithing, +20 Charm | — |
| Push through, accept imperfect results | Safe | +2 Fatigue | +25 Smithing | — |

**Escalation:** Proper forge maintenance enables better equipment events.

---

### ARM-04: Captured Arms

**Type:** Duty Event
**Duty Required:** Armorer
**Time of Day:** Morning, Afternoon
**Trigger:** `after_battle` AND `loot_captured`

**Setup:**
{ENEMY_FACTION_ADJECTIVE} weapons piled in the wagons. Some are better than what our soldiers carry.

{SECOND_SHORT} wants them sorted. "What's worth keeping? What do we melt down?"

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Evaluate everything carefully | Safe | +3 Fatigue | +35 Smithing, +25 Trade | — |
| Keep the best, scrap the rest | Safe | +2 Fatigue | +30 Smithing, +15 Steward | — |
| Mark the best pieces for reforging | Safe | +2 Fatigue | +30 Smithing, +20 Engineering | — |
| Set aside the finest piece for yourself | Corrupt | +2 Heat | +25 Smithing, better equipment | — |

**Escalation:** Hidden loot may be discovered in shakedown event.

---

### ARM-05: Armor Fitting

**Type:** Duty Event
**Duty Required:** Armorer
**Time of Day:** Any
**Trigger:** `new_equipment_distributed` OR `promotion_occurred`

**Setup:**
{RECRUIT_NAME} got new armor — but it doesn't fit. Too loose in the shoulders, too tight at the waist.

"Can't fight in this. Feels like I'm wearing a barrel."

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Adjust it properly — takes hours | Safe | +3 Fatigue | +35 Smithing, +15 Engineering, +relation recruit | — |
| Quick adjustments, good enough | Safe | +1 Fatigue | +20 Smithing | — |
| Teach them to adjust it themselves | Safe | +2 Fatigue | +25 Smithing, +20 Leadership | — |
| "Figure it out yourself" | Safe | — | −relation recruit | — |

**Escalation:** Helping recruits builds reputation as reliable armorer.

---

## Runner Events

### RUN-01: Hot Meal Delivery

**Type:** Duty Event
**Duty Required:** Runner
**Time of Day:** Morning, Evening
**Trigger:** `camp_established` AND `daily_tick`

**Setup:**
The cook shoves a pot into your hands. "Sentries on the north line haven't eaten since yesterday. Get this to them before it goes cold."

The north line is far. The pot is heavy. And it's starting to rain.

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Run fast, food still warm | Safe | +2 Fatigue | +30 Athletics, +relation sentries | — |
| Walk carefully, don't spill | Safe | +1 Fatigue | +20 Athletics, +15 Steward | — |
| Sprint — race the rain | Risky | +2 Fatigue | +35 Athletics | 10% Minor (slip) |
| Eat some yourself on the way | Corrupt | +1 Heat | +20 Athletics, −relation sentries | — |

**Escalation:** Being known as reliable runner leads to important message duties.

---

### RUN-02: Urgent Summons

**Type:** Duty Event
**Duty Required:** Runner
**Time of Day:** Any
**Trigger:** `battle_imminent` OR `emergency`

**Setup:**
"Get {LANCE_LEADER_SHORT}! Now! The captain wants all sergeants!"

You don't know why. You don't need to know why. You just need to run.

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Sprint — every second counts | Risky | +2 Fatigue | +35 Athletics | 5% Minor |
| Run steadily, find him fast | Safe | +2 Fatigue | +30 Athletics, +15 Scouting | — |
| Shout the message, keep moving | Safe | +1 Fatigue | +25 Athletics | — |
| Ask what's happening first | Safe | +1 Fatigue | +20 Athletics, know the situation | — |

**Escalation:** Fast runners get trusted with critical messages.

---

### RUN-03: Lost Orders

**Type:** Duty Event
**Duty Required:** Runner
**Time of Day:** Any
**Trigger:** `in_army` AND `confusion`

**Setup:**
The orders got confused somewhere. Third company went left when they should've gone right. Someone needs to sort this out — on foot, through a camp that's half-packed and chaotic.

{SECOND_SHORT} points at you. "Go."

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Find each unit, deliver corrections | Safe | +3 Fatigue | +35 Athletics, +25 Scouting | — |
| Run to the officers first, let them handle it | Safe | +2 Fatigue | +30 Athletics, +15 Leadership | — |
| Sprint to the most critical unit | Risky | +2 Fatigue | +35 Athletics, +20 Tactics | 5% Minor |
| Grab others to help spread the word | Safe | +2 Fatigue | +25 Athletics, +20 Leadership | — |

**Escalation:** Handling confusion well leads to recognition from officers.

---

### RUN-04: Wounded Evacuation

**Type:** Duty Event
**Duty Required:** Runner
**Time of Day:** Any
**Trigger:** `during_battle` OR `after_battle`

**Setup:**
Fighting's moved on, but the wounded are still out there. Someone needs to guide the stretcher bearers, mark where the fallen lie.

Running through a battlefield. Not everyone you find will be friendly. Or alive.

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Systematic search, mark everyone | Safe | +3 Fatigue | +30 Athletics, +25 Scouting, +15 Medicine | — |
| Focus on our soldiers first | Safe | +2 Fatigue | +25 Athletics, +20 Scouting | — |
| Help carry the wounded yourself | Risky | +4 Fatigue | +35 Athletics, +20 Medicine | 10% Minor (strain) |
| Check for valuables while you search | Corrupt | +3 Heat | +25 Athletics, +20 Roguery, loot | — |

**Escalation:** Looting the dead may be witnessed → discipline event.

---

### RUN-05: Water Run

**Type:** Duty Event
**Duty Required:** Runner
**Time of Day:** Morning, Afternoon
**Trigger:** `siege_ongoing` OR `hot_weather` OR `logistics_strain > 50`

**Setup:**
The water barrels are dry. The stream is half a mile away, through open ground that might be watched.

"Six trips minimum," {SECOND_SHORT} says. "Try not to get shot."

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Make all six trips, steady pace | Safe | +4 Fatigue | +40 Athletics, +20 Steward | — |
| Recruit help, organize a water chain | Safe | +2 Fatigue | +25 Athletics, +30 Leadership | — |
| Find a closer source (risky scouting) | Risky | +3 Fatigue | +30 Athletics, +35 Scouting | 10% Minor |
| Make three trips, report barrels full | Corrupt | +2 Heat | +20 Athletics | — |

**Escalation:** Lying about water leads to supply crisis.

---

## Lookout Events

### LOOK-01: Night Watch

**Type:** Duty Event
**Duty Required:** Lookout
**Time of Day:** Night, Late Night
**Trigger:** `camp_established` AND `not_in_settlement`

**Setup:**
Your watch. Four hours staring into darkness, listening for what doesn't belong.

The camp sleeps behind you. Everything beyond the firelight is enemy territory.

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Stay sharp, walk the perimeter | Safe | +2 Fatigue | +30 Scouting, +20 Athletics | — |
| Find a good vantage point, stay still | Safe | +2 Fatigue | +25 Scouting, +15 Tactics | — |
| Practice with your weapon (quietly) | Safe | +2 Fatigue | +20 Scouting, +25 (weapon skill) | — |
| Rest your eyes, trust your ears | Risky | +1 Fatigue | +15 Scouting | 15% discipline if caught |

**Escalation:** Sleeping on watch leads to formal discipline event.

---

### LOOK-02: Movement Spotted

**Type:** Duty Event
**Duty Required:** Lookout
**Time of Day:** Any
**Trigger:** `on_watch` AND `random_chance`

**Setup:**
Something moved out there. Too big for an animal. Too quiet for a friend.

Wake the camp? Investigate? Could be nothing. Could be a raid.

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Sound the alarm immediately | Safe | +1 Fatigue | +25 Scouting | — |
| Investigate first, confirm the threat | Risky | +2 Fatigue | +35 Scouting, +20 Athletics | 15% Minor-Moderate |
| Alert {LANCE_LEADER_SHORT} quietly | Safe | +1 Fatigue | +30 Scouting, +15 Leadership | — |
| Wait and watch, confirm before acting | Risky | +2 Fatigue | +35 Scouting | 10% if attack occurs |

**Escalation:** False alarms lose credibility. Missed attacks are worse.

---

### LOOK-03: Signal Fire

**Type:** Duty Event
**Duty Required:** Lookout
**Time of Day:** Night
**Trigger:** `in_army` AND `separated_units`

**Setup:**
{LORD_NAME}'s orders: watch for the signal fire. When you see it, we move.

Hours pass. No fire. Then — is that it? Or just a farmhouse burning?

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Report it as the signal | Safe | +1 Fatigue | +25 Scouting, +20 Tactics | — |
| Watch longer, confirm the pattern | Safe | +2 Fatigue | +30 Scouting, +25 Tactics | — |
| Send a runner to verify | Safe | +1 Fatigue | +25 Scouting, +15 Leadership | — |
| Guess it's the signal, wake the camp | Risky | +1 Fatigue | +20 Scouting (if right), disaster if wrong | — |

**Escalation:** Correct identification builds trust for important watches.

---

### LOOK-04: Weather Watch

**Type:** Duty Event
**Duty Required:** Lookout
**Time of Day:** Dawn, Morning
**Trigger:** `army_planning_movement` AND `weather_unclear`

**Setup:**
{LORD_NAME} wants to know if we can move today. Storm clouds on the horizon, but they might pass.

Your eyes have been watching the sky. What do you tell him?

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Report honestly — uncertain | Safe | — | +20 Scouting | — |
| Predict clear — advise movement | Risky | — | +30 Scouting, +20 Tactics (if right) | — |
| Predict storm — advise waiting | Risky | — | +30 Scouting, +20 Tactics (if right) | — |
| Suggest sending a scout ahead | Safe | — | +25 Scouting, +15 Leadership | — |

**Escalation:** Accurate predictions build reputation as reliable lookout.

---

### LOOK-05: Counting the Enemy

**Type:** Duty Event
**Duty Required:** Lookout
**Time of Day:** Dawn, Morning
**Trigger:** `enemy_visible` AND `before_battle`

**Setup:**
{ENEMY_FACTION_ADJECTIVE} forces on the ridge. {LORD_NAME} needs numbers before he commits.

"How many? Infantry? Cavalry? Be exact, {PLAYER_NAME}."

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Count carefully, report accurately | Safe | +2 Fatigue | +35 Scouting, +30 Tactics | — |
| Estimate by formations and standards | Safe | +1 Fatigue | +25 Scouting, +25 Tactics | — |
| Move closer for a better look | Risky | +2 Fatigue | +40 Scouting, +35 Tactics | 10% Minor (spotted) |
| Round up — better safe than sorry | Safe | +1 Fatigue | +20 Scouting | — |

**Escalation:** Accurate counts lead to better battle outcomes.

---

## Engineer Events

### ENG-01: Fortification Work

**Type:** Duty Event
**Duty Required:** Engineer
**Time of Day:** Morning, Afternoon
**Trigger:** `siege_ongoing` OR `camp_fortifying`

**Setup:**
{LORD_NAME} wants earthworks by nightfall. Trenches, palisades, something the enemy can't just ride through.

The ground is hard. The men are tired. The deadline is real.

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Work alongside the men, lead by example | Safe | +4 Fatigue | +40 Engineering, +25 Athletics, +relation soldiers | 5% Minor (strain) |
| Supervise and direct, maximize efficiency | Safe | +2 Fatigue | +35 Engineering, +25 Leadership | — |
| Focus on the critical sections only | Safe | +2 Fatigue | +30 Engineering, +20 Tactics | — |
| Push the men harder, get it done early | Risky | +3 Fatigue | +35 Engineering, +relation lord, −relation soldiers | — |

**Escalation:** Quality fortifications provide bonus in defensive events.

---

### ENG-02: Siege Engine Repair

**Type:** Duty Event
**Duty Required:** Engineer
**Time of Day:** Any
**Trigger:** `siege_ongoing` AND `siege_engine_damaged`

**Setup:**
The trebuchet arm cracked. Counter-weight's bent. {LORD_NAME} wants it firing again by morning.

"Can you fix it, {PLAYER_NAME}?"

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Full repair, do it right | Safe | +4 Fatigue | +45 Engineering, +25 Smithing | — |
| Patch it — it'll hold for a few shots | Safe | +2 Fatigue | +30 Engineering | — |
| Improvise with available materials | Risky | +3 Fatigue | +40 Engineering (if success), failure breaks it worse | — |
| Report it beyond repair | Safe | — | −relation lord | — |

**Escalation:** Keeping siege engines running wins recognition from commanders.

---

### ENG-03: Bridge Assessment

**Type:** Duty Event
**Duty Required:** Engineer
**Time of Day:** Morning, Afternoon
**Trigger:** `army_moving` AND `river_crossing`

**Setup:**
The bridge looks old. The army's heavy. {LORD_NAME} needs to know — can it hold?

"Your expertise, {PLAYER_NAME}. Speak plainly."

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Inspect thoroughly, give accurate assessment | Safe | +2 Fatigue | +35 Engineering, +25 Scouting | — |
| Reinforce the weak points first | Safe | +3 Fatigue | +40 Engineering | — |
| Test it with a small group first | Safe | +1 Fatigue | +25 Engineering, +20 Leadership | — |
| It'll hold (confident guess) | Risky | — | +20 Engineering (if right), disaster if wrong | — |

**Escalation:** Bridge collapse kills soldiers if assessment wrong.

---

### ENG-04: Mining Operation

**Type:** Duty Event
**Duty Required:** Engineer
**Time of Day:** Any
**Trigger:** `siege_ongoing` AND `siege_day > 5`

**Setup:**
{LORD_NAME} wants to mine the walls. Tunnel under, collapse it, open a breach.

Dangerous work. Collapses. Counter-mines. Defenders above.

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Lead the mining crew personally | Risky | +4 Fatigue | +50 Engineering, +30 Athletics | 20% Minor-Moderate (collapse) |
| Direct from the surface, let others dig | Safe | +2 Fatigue | +35 Engineering, +20 Leadership | — |
| Shore up the tunnel properly, slow but safe | Safe | +3 Fatigue | +40 Engineering | — |
| Volunteer for the breach moment | Risky | +3 Fatigue | +45 Engineering, glory | 25% Moderate (fighting) |

**Escalation:** Successful mine creates breach → assault opportunity.

---

### ENG-05: Camp Layout

**Type:** Duty Event
**Duty Required:** Engineer
**Time of Day:** Afternoon, Evening
**Trigger:** `army_halted` AND `making_camp`

**Setup:**
New campsite. {SECOND_SHORT} wants it organized right — latrines downhill, fires safe distance, wagons protecting the perimeter.

"You've got an eye for this. Lay it out."

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Plan it carefully, explain to the sergeants | Safe | +2 Fatigue | +35 Engineering, +25 Steward | — |
| Quick layout, trust experience | Safe | +1 Fatigue | +25 Engineering | — |
| Walk the ground first, find the best spots | Safe | +2 Fatigue | +30 Engineering, +25 Scouting | — |
| Delegate to the sergeants, approve their work | Safe | +1 Fatigue | +20 Engineering, +20 Leadership | — |

**Escalation:** Good camp layout prevents sickness and surprise attack events.

---

## Boatswain Events (Naval)

### BOAT-01: Deck Discipline

**Type:** Duty Event
**Duty Required:** Boatswain
**Time of Day:** Morning
**Trigger:** `at_sea`

**Setup:**
{CAPTAIN_NAME} runs a tight ship. That's your job — making sure the crew follows orders.

Someone slacked on the night watch. Everyone knows who. Now they're watching to see what you do.

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Public discipline — make an example | Safe | +1 Fatigue | +30 Boatswain, +20 Leadership, −relation with offender | — |
| Private warning — give them one chance | Safe | +1 Fatigue | +25 Boatswain, +20 Charm | — |
| Extra duty — punishment through work | Safe | +1 Fatigue | +30 Boatswain | — |
| Ignore it — not worth the trouble | Corrupt | — | −20 Leadership, −discipline | — |

**Escalation:** Ignored discipline leads to larger problems.

---

### BOAT-02: Rigging Emergency

**Type:** Duty Event
**Duty Required:** Boatswain
**Time of Day:** Any
**Trigger:** `at_sea` AND `rough_weather`

**Setup:**
Line snapped in the wind. The sail's loose, the mast is groaning, and {SHIP_NAME} is fighting the storm.

"Boatswain! Get that rigging fixed!"

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Lead the crew up yourself | Risky | +3 Fatigue | +45 Boatswain, +30 Athletics | 20% Minor-Moderate (fall) |
| Direct from deck, guide the sailors | Safe | +2 Fatigue | +35 Boatswain, +25 Leadership | — |
| Send the most experienced sailor up | Safe | +1 Fatigue | +25 Boatswain, +20 Leadership | — |
| Cut the line, lose the sail, save the mast | Safe | +1 Fatigue | +30 Boatswain, ship slower | — |

**Escalation:** Excellent rigging work prevents worse storm damage.

---

### BOAT-03: Supply Rationing

**Type:** Duty Event
**Duty Required:** Boatswain
**Time of Day:** Morning
**Trigger:** `at_sea` AND `voyage_long` AND `supplies_low`

**Setup:**
{DAYS_AT_SEA} days out. Food's running low. Water's worse.

{CAPTAIN_NAME} wants strict rationing. The crew won't like it.

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Implement fair rationing, explain why | Safe | +1 Fatigue | +30 Boatswain, +25 Steward | — |
| Short the rations quietly, don't announce | Risky | +1 Fatigue | +25 Boatswain, +20 Roguery | crew anger if caught |
| Catch fish to supplement | Safe | +3 Fatigue | +25 Boatswain, +25 Scouting, more food | — |
| Officers eat full, crew gets cut | Corrupt | — | +20 Boatswain, −relation crew | — |

**Escalation:** Unfair rationing leads to crew unrest event.

---

### BOAT-04: Man Overboard

**Type:** Duty Event
**Duty Required:** Boatswain
**Time of Day:** Any
**Trigger:** `at_sea` AND `random_chance`

**Setup:**
"Man overboard!"

The cry goes up. Someone's in the water. The ship's moving fast. The sea is cold.

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Organize immediate rescue | Safe | +2 Fatigue | +35 Boatswain, +25 Leadership, +relation crew | — |
| Jump in yourself | Risky | +3 Fatigue | +40 Boatswain, +35 Athletics | 15% Minor (cold/strain) |
| Throw the line, guide from deck | Safe | +1 Fatigue | +30 Boatswain | — |
| He's gone — don't risk more lives | Safe | — | +15 Boatswain, −relation crew | — |

**Escalation:** Saving the sailor builds enormous loyalty.

---

### BOAT-05: Crew Dispute

**Type:** Duty Event
**Duty Required:** Boatswain
**Time of Day:** Evening
**Trigger:** `at_sea` AND `morale_low`

**Setup:**
Two sailors at each other's throats. Something about stolen rations, a woman back home, some old grudge. Doesn't matter — it's spreading.

The crew's watching you.

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Separate them, hear both sides | Safe | +1 Fatigue | +30 Boatswain, +25 Charm | — |
| Let them fight it out (controlled) | Risky | +1 Fatigue | +25 Boatswain, dispute resolved, risk injury to them | — |
| Punish both — no fighting aboard | Safe | +1 Fatigue | +30 Boatswain, +20 Leadership | — |
| Take sides with the senior man | Corrupt | — | +20 Boatswain, −relation with other | — |

**Escalation:** Unresolved disputes fester into mutiny seeds.

---

## Navigator Events (Naval)

### NAV-01: Course Setting

**Type:** Duty Event
**Duty Required:** Navigator
**Time of Day:** Dawn, Morning
**Trigger:** `at_sea` AND `voyage_start`

**Setup:**
{CAPTAIN_NAME} spreads the charts. {DESTINATION_PORT} is the goal.

"Plot our course, Navigator. Weather's your concern, shoals are your concern, time is mine."

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Safe route — longer but reliable | Safe | +1 Fatigue | +30 Shipmaster, +20 Scouting | — |
| Direct route — fastest but riskier | Risky | +1 Fatigue | +35 Shipmaster, +25 Tactics | 10% bad weather |
| Coastal route — landmarks, safe harbors | Safe | +1 Fatigue | +30 Shipmaster, +25 Scouting | — |
| Suggest waiting for better weather | Safe | +1 Fatigue | +25 Shipmaster, delay | — |

**Escalation:** Route choice affects encounter chances and voyage time.

---

### NAV-02: Storm Navigation

**Type:** Duty Event
**Duty Required:** Navigator
**Time of Day:** Any
**Trigger:** `at_sea` AND `storm`

**Setup:**
The storm hit hard. Stars are gone. Landmarks — what landmarks? {SHIP_NAME} is somewhere in a lot of angry water.

{CAPTAIN_NAME} needs a heading. Now.

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Dead reckoning — trust your calculations | Risky | +2 Fatigue | +45 Shipmaster (if right), lost if wrong | — |
| Heave to — ride out the storm, recalculate after | Safe | +2 Fatigue | +30 Shipmaster | — |
| Run before the wind — go where it takes us | Safe | +1 Fatigue | +25 Shipmaster, off course | — |
| Admit uncertainty, advise caution | Safe | +1 Fatigue | +25 Shipmaster | — |

**Escalation:** Getting lost leads to supply crisis or unknown waters event.

---

### NAV-03: Unknown Waters

**Type:** Duty Event
**Duty Required:** Navigator
**Time of Day:** Morning, Afternoon
**Trigger:** `at_sea` AND `off_known_routes`

**Setup:**
The charts end here. {CAPTAIN_NAME} wants to push on — profit in unknown ports, they say.

Your charts are blank. Your instruments are useless. Now it's just experience.

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Map as you go — careful progress | Safe | +3 Fatigue | +40 Shipmaster, +30 Scouting, new charts | — |
| Use stars, currents, bird signs | Risky | +2 Fatigue | +45 Shipmaster (if success) | — |
| Keep the coastline in sight | Safe | +2 Fatigue | +30 Shipmaster, +25 Scouting | — |
| Advise turning back | Safe | +1 Fatigue | +20 Shipmaster, −relation captain | — |

**Escalation:** Successful navigation in unknown waters brings fame and better assignments.

---

### NAV-04: Shoal Warning

**Type:** Duty Event
**Duty Required:** Navigator
**Time of Day:** Any
**Trigger:** `at_sea` AND `approaching_shallow_waters`

**Setup:**
The water's changing color. Charts say we're clear, but charts lie.

"Depth?" {CAPTAIN_NAME} asks.

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Sound the depths, adjust course | Safe | +2 Fatigue | +35 Shipmaster, +25 Scouting | — |
| Trust the charts, maintain speed | Risky | +1 Fatigue | +25 Shipmaster (if clear), damage if wrong | — |
| Post lookouts, slow to half | Safe | +1 Fatigue | +30 Shipmaster | — |
| Drop anchor, scout in small boat | Safe | +2 Fatigue | +30 Shipmaster, +25 Scouting | — |

**Escalation:** Grounding the ship is career-ending.

---

### NAV-05: Landfall

**Type:** Duty Event
**Duty Required:** Navigator
**Time of Day:** Dawn, Morning
**Trigger:** `at_sea` AND `approaching_destination`

**Setup:**
"Land!" The cry every sailor loves.

But is it the right land? {DESTINATION_PORT} should be... there? Or there?

{CAPTAIN_NAME} looks at you. "Where are we, Navigator?"

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Study the coastline, identify landmarks | Safe | +2 Fatigue | +35 Shipmaster, +25 Scouting | — |
| That's {DESTINATION_PORT}, I'm certain | Risky | +1 Fatigue | +40 Shipmaster (if right), embarrassment if wrong | — |
| Signal for pilot boat, get local guidance | Safe | +1 Fatigue | +25 Shipmaster | — |
| Approach cautiously, sound as we go | Safe | +2 Fatigue | +30 Shipmaster, +20 Scouting | — |

**Escalation:** Accurate landfalls build captain's confidence in you.

---

# Training Events

Training events are player-initiated from the Camp Activities menu. They provide the primary source of formation-based skill XP.

## Delivery Metadata — Training Events

| Field | Value |
|-------|-------|
| **Delivery** | Player-Initiated |
| **Menu** | Camp Activities → Training Section (`enlisted_activities`) |
| **Triggered By** | Player clicks menu option |
| **Presentation** | Inquiry Popup |

**Implementation:** Each training event is a menu option in `EnlistedActivitiesMenuBehavior`. When player clicks, call `TriggerStoryEvent(eventId)` which shows the event as an inquiry popup. Menu checks cooldown, fatigue, formation, and injury restrictions before enabling the option.

**Menu Option Format:**
```csharp
starter.AddGameMenuOption("enlisted_activities", "train_{id}",
    "{Display Name} [{Reward Preview} | +{Fatigue} Fatigue]",
    args => {
        if (GetPlayerFormation() != requiredFormation) return false;
        // Check cooldown, fatigue, injuries
        return true;
    },
    args => TriggerStoryEvent("{event_id}"));
```

---

## Infantry Training

### INF-TRAIN-01: Shield Wall Drill

**Type:** Training Event
**Formation:** Infantry
**Time of Day:** Morning, Afternoon
**Cooldown:** 2 days

**Setup:**
{SERGEANT_NAME} forms up {LANCE_NAME}. "Shield wall! Tight formation! I want to hear those shields lock!"

The drill is brutal — hours holding the wall, taking practice blows, advancing as one.

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Hold the line, no breaks | Safe | +2 Fatigue | +25 Polearm, +20 OneHanded, +15 Athletics | — |
| Push to the front, take more hits | Risky | +3 Fatigue | +35 Polearm, +25 OneHanded, +20 Athletics | 10% Minor |
| Focus on footwork and positioning | Safe | +2 Fatigue | +20 Polearm, +15 OneHanded, +25 Athletics | — |
| Help the weaker soldiers hold | Safe | +2 Fatigue | +20 Polearm, +15 OneHanded, +20 Leadership | — |

---

### INF-TRAIN-02: Sparring Circle

**Type:** Training Event
**Formation:** Infantry
**Time of Day:** Morning, Afternoon
**Cooldown:** 1 day

**Setup:**
A circle forms near the cook fires. Wooden weapons, real bruises. 

{VETERAN_1_NAME} cracks their knuckles. "Who's first?"

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Spar carefully, focus on technique | Safe | +1 Fatigue | +20 OneHanded, +15 Athletics | — |
| Fight hard, don't hold back | Risky | +2 Fatigue | +35 OneHanded, +20 Athletics | 15% Minor |
| Challenge {VETERAN_1_NAME} | Risky | +2 Fatigue | +40 OneHanded, +relation veteran (if good showing) | 20% Minor |
| Watch and learn, spar later | Safe | +1 Fatigue | +15 OneHanded, +20 Tactics | — |

---

### INF-TRAIN-03: March Conditioning

**Type:** Training Event
**Formation:** Infantry
**Time of Day:** Morning
**Cooldown:** 2 days

**Setup:**
Full kit march. {LANCE_LEADER_SHORT} sets a punishing pace.

"Any army can fight. Winners can fight after a hard march."

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Match the pace, endure | Safe | +3 Fatigue | +30 Athletics, +15 Polearm | — |
| Push ahead, set the pace | Risky | +4 Fatigue | +40 Athletics, +15 Leadership | 10% Minor (strain) |
| Help stragglers keep up | Safe | +3 Fatigue | +25 Athletics, +20 Charm | — |
| Pace yourself smart, conserve energy | Safe | +2 Fatigue | +25 Athletics | — |

---

### INF-TRAIN-04: Two-Handed Weapons Drill

**Type:** Training Event
**Formation:** Infantry
**Time of Day:** Morning, Afternoon
**Cooldown:** 2 days

**Setup:**
{LANCE_LEADER_SHORT} holds up a training greatsword. "Who thinks they can handle this? Real fighters, not show-offs."

Two-handed work needs space, timing, and strength.

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Practice basic forms | Safe | +2 Fatigue | +25 TwoHanded, +15 Athletics | — |
| Work on power strikes | Risky | +2 Fatigue | +35 TwoHanded, +20 Athletics | 10% Minor |
| Focus on defensive recovery | Safe | +2 Fatigue | +25 TwoHanded, +15 Tactics | — |
| Partner drill — strike and counter | Safe | +2 Fatigue | +30 TwoHanded, +15 Polearm | 5% Minor |

---

## Cavalry Training

### CAV-TRAIN-01: Horse Rotation

**Type:** Training Event
**Formation:** Cavalry
**Time of Day:** Morning
**Cooldown:** 1 day

**Setup:**
"Rotate mounts! Every rider needs to know every horse!"

{LORD_NAME}'s stables rotate warhorses regularly. You need to bond fast.

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Take time with each horse | Safe | +2 Fatigue | +30 Riding, +15 Charm | — |
| Focus on the difficult horse | Risky | +2 Fatigue | +40 Riding | 10% Minor (thrown) |
| Help others with their mounts | Safe | +2 Fatigue | +25 Riding, +20 Leadership | — |
| Quick assessment, basic handling | Safe | +1 Fatigue | +20 Riding | — |

---

### CAV-TRAIN-02: Mounted Combat Drill

**Type:** Training Event
**Formation:** Cavalry
**Time of Day:** Morning, Afternoon
**Cooldown:** 2 days

**Setup:**
Quintain practice. Rings on posts. Moving targets. {LANCE_LEADER_SHORT} watches from horseback.

"Hit the target, not your horse."

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Practice lance work | Safe | +2 Fatigue | +25 Riding, +25 Polearm | — |
| Practice sword from horseback | Safe | +2 Fatigue | +25 Riding, +25 OneHanded | — |
| Full speed passes | Risky | +3 Fatigue | +35 Riding, +30 Polearm | 15% Minor |
| Work on archery from horseback | Risky | +2 Fatigue | +30 Riding, +30 Bow | 5% Minor |

---

### CAV-TRAIN-03: Charge Practice

**Type:** Training Event
**Formation:** Cavalry
**Time of Day:** Morning
**Cooldown:** 3 days

**Setup:**
The infantry holds a line of dummies. Your job — hit it at full gallop without dying.

{LANCE_LEADER_SHORT}: "Control. Timing. Impact. In that order."

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Standard charge, proper form | Safe | +2 Fatigue | +30 Riding, +25 Polearm | — |
| Full speed, maximum impact | Risky | +3 Fatigue | +40 Riding, +35 Polearm | 20% Minor |
| Focus on approach and timing | Safe | +2 Fatigue | +25 Riding, +20 Tactics | — |
| Lead the practice charge | Risky | +3 Fatigue | +35 Riding, +25 Leadership | 15% Minor |

---

### CAV-TRAIN-04: Skirmish Tactics

**Type:** Training Event
**Formation:** Cavalry
**Time of Day:** Morning, Afternoon
**Cooldown:** 2 days

**Setup:**
{LANCE_LEADER_SHORT} sets up a mock skirmish. Ride in, throw, ride out. Don't get caught.

"You're not knights. You're killers. Get in, get out."

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Practice javelin throws | Safe | +2 Fatigue | +25 Riding, +25 Throwing | — |
| Work on bow from horseback | Safe | +2 Fatigue | +25 Riding, +25 Bow | — |
| Full mock battle | Risky | +3 Fatigue | +35 Riding, +30 Throwing, +20 Tactics | 10% Minor |
| Focus on evasion and retreat | Safe | +2 Fatigue | +30 Riding, +20 Athletics | — |

---

## Archer Training

### ARCH-TRAIN-01: Target Practice

**Type:** Training Event
**Formation:** Archer
**Time of Day:** Morning, Afternoon
**Cooldown:** 1 day

**Setup:**
Straw targets at fifty paces. Then a hundred. Then whatever distance {LANCE_LEADER_SHORT} decides is funny.

"Hit the target. Not the sky. Not the dirt. The target."

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Steady practice, work on consistency | Safe | +1 Fatigue | +25 Bow, +15 Crossbow | — |
| Long range shots, push your limits | Risky | +2 Fatigue | +35 Bow | 5% Minor (strain) |
| Speed shooting — loose fast | Safe | +2 Fatigue | +30 Bow, +15 Athletics | — |
| Help newer archers with technique | Safe | +2 Fatigue | +20 Bow, +20 Leadership | — |

---

### ARCH-TRAIN-02: Volley Drill

**Type:** Training Event
**Formation:** Archer
**Time of Day:** Morning, Afternoon
**Cooldown:** 2 days

**Setup:**
{LANCE_LEADER_SHORT} raises an arm. When it falls, you loose. Not before. Not after. Together.

"A volley breaks armies. Individuals break wind."

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Focus on timing with the unit | Safe | +2 Fatigue | +25 Bow, +20 Tactics | — |
| Work on rapid reload | Safe | +2 Fatigue | +30 Bow, +15 Athletics | — |
| Call the volleys (leadership practice) | Safe | +2 Fatigue | +20 Bow, +25 Leadership | — |
| High arc shots — hit behind cover | Risky | +2 Fatigue | +35 Bow, +20 Tactics | — |

---

### ARCH-TRAIN-03: Moving Target Practice

**Type:** Training Event
**Formation:** Archer
**Time of Day:** Morning, Afternoon
**Cooldown:** 2 days

**Setup:**
Runners drag targets on ropes. Cavalry makes passes. Nothing sits still in battle.

"Lead your target. Anticipate. Don't chase."

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Focus on tracking and lead | Safe | +2 Fatigue | +30 Bow, +20 Scouting | — |
| Practice against cavalry targets | Safe | +2 Fatigue | +30 Bow, +20 Tactics | — |
| Speed over accuracy — volume fire | Risky | +2 Fatigue | +35 Bow | — |
| Mixed practice — various targets | Safe | +2 Fatigue | +25 Bow, +15 Throwing | — |

---

### ARCH-TRAIN-04: Hunting Party

**Type:** Training Event
**Formation:** Archer
**Time of Day:** Morning
**Cooldown:** 3 days
**Condition:** `not_in_settlement` AND `wilderness_nearby`

**Setup:**
{LANCE_LEADER_SHORT} got permission for a hunting party. Real targets, real meat, real skills.

"Don't come back empty-handed."

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Hunt carefully, stalk properly | Safe | +3 Fatigue | +30 Bow, +30 Scouting | — |
| Range far, find good game | Risky | +4 Fatigue | +35 Bow, +35 Scouting | 10% Minor (terrain) |
| Set snares, combine methods | Safe | +2 Fatigue | +20 Bow, +25 Scouting, +20 Roguery | — |
| Let others hunt, watch and learn | Safe | +1 Fatigue | +15 Bow, +20 Scouting | — |

---

## Naval Training

### NAV-TRAIN-01: Boarding Drill

**Type:** Training Event
**Formation:** Naval
**Time of Day:** Morning, Afternoon
**Trigger:** `at_sea`
**Cooldown:** 2 days

**Setup:**
{CAPTAIN_NAME} wants the crew sharp for boarding action. Ropes, hooks, blades, chaos.

"When we hit their deck, you have seconds. Make them count."

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Practice rope work and swings | Safe | +2 Fatigue | +30 Mariner, +25 Athletics | — |
| Full mock boarding | Risky | +3 Fatigue | +40 Mariner, +30 OneHanded | 15% Minor |
| Focus on defending our deck | Safe | +2 Fatigue | +30 Mariner, +20 Polearm | — |
| Lead a boarding team | Risky | +3 Fatigue | +35 Mariner, +25 Leadership | 10% Minor |

---

### NAV-TRAIN-02: Rigging Practice

**Type:** Training Event
**Formation:** Naval
**Time of Day:** Morning, Afternoon
**Trigger:** `at_sea`
**Cooldown:** 1 day

**Setup:**
{BOATSWAIN_NAME} points at the mast. "Up. Now. Don't fall."

Working the rigging is half the job. The other half is surviving it.

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Climb and work steadily | Safe | +2 Fatigue | +30 Mariner, +25 Athletics | — |
| Race to the top | Risky | +2 Fatigue | +35 Mariner, +30 Athletics | 15% Minor (fall) |
| Practice knots and line work | Safe | +1 Fatigue | +25 Mariner, +15 Engineering | — |
| Help newer sailors learn | Safe | +2 Fatigue | +25 Mariner, +20 Leadership | — |

---

### NAV-TRAIN-03: Navigation Lesson

**Type:** Training Event
**Formation:** Naval
**Time of Day:** Evening, Night
**Trigger:** `at_sea`
**Cooldown:** 2 days

**Setup:**
Clear night. {NAVIGATOR_NAME} spreads the charts on deck, points at stars.

"A sailor who can't navigate is cargo."

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Study star positions and charts | Safe | +1 Fatigue | +30 Shipmaster, +20 Scouting | — |
| Practice dead reckoning | Safe | +1 Fatigue | +25 Shipmaster, +20 Steward | — |
| Ask about unusual situations | Safe | +1 Fatigue | +30 Shipmaster, +15 Charm | — |
| Calculate our current position | Risky | +2 Fatigue | +40 Shipmaster (if right) | — |

---

### NAV-TRAIN-04: Storm Drill

**Type:** Training Event
**Formation:** Naval
**Time of Day:** Any
**Trigger:** `at_sea`
**Cooldown:** 3 days

**Setup:**
{CAPTAIN_NAME} calls storm drill. Everyone has a job when the sea gets angry.

"When the real storm hits, you won't have time to think."

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Secure cargo below | Safe | +2 Fatigue | +25 Mariner, +25 Steward | — |
| Man the pumps | Safe | +3 Fatigue | +30 Mariner, +25 Athletics | — |
| Handle emergency rigging | Risky | +3 Fatigue | +35 Mariner, +30 Athletics | 10% Minor |
| Coordinate crew positions | Safe | +2 Fatigue | +30 Mariner, +25 Leadership | — |

---

# General Events by Time of Day

General events fire based on campaign state and time of day. They're available regardless of duty.

## Delivery Metadata — General Events

| Field | Value |
|-------|-------|
| **Delivery** | Automatic |
| **Menu** | None |
| **Triggered By** | System fires when time of day + conditions + `ai_safe` |
| **Presentation** | Inquiry Popup |

**Implementation:** Events fire automatically via `LanceLifeEventManager.CheckGeneralEvents()` during hourly tick. System checks current time of day, selects eligible events, and fires one randomly (respecting cooldowns and rate limits).

---

## Dawn Events (5:00–7:00)

### DAWN-01: Morning Muster

**Type:** General Event
**Time of Day:** Dawn
**Trigger:** `daily_tick` AND `chance`

**Setup:**
{LANCE_LEADER_SHORT}'s voice shatters the dawn quiet. "On your feet, {LANCE_NAME}! Muster in five!"

Around you, soldiers groan and fumble for boots. {LANCE_MATE_NAME} is still snoring.

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Fall in first, look sharp | Safe | +1 Fatigue | +15 Leadership, +relation leader | — |
| Fall in with the rest | Safe | — | — | — |
| Kick {LANCE_MATE_NAME} awake, fall in together | Safe | +1 Fatigue | +15 Charm, +relation mate | — |
| Sleep through it | Risky | — | +1 Discipline, extra rest | — |

---

### DAWN-02: Cold Start

**Type:** General Event
**Time of Day:** Dawn
**Trigger:** `cold_weather` AND `not_in_settlement`

**Setup:**
Frost on the blankets. Your breath clouds. The fire died sometime in the night and nobody noticed.

Getting moving will hurt. Staying still will hurt worse.

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Get up first, restart the fire | Safe | +1 Fatigue | +20 Steward, +15 Athletics, +relation lance | — |
| Exercises to warm up | Safe | +1 Fatigue | +20 Athletics | — |
| Wait for someone else to fix it | Safe | — | −relation lance | — |
| [Field Medic] Check for frostbite in the lance | Safe | +1 Fatigue | +25 Medicine, +relation lance | — |

---

### DAWN-03: Sick Call

**Type:** General Event
**Time of Day:** Dawn
**Trigger:** `daily_tick` AND `player_has_condition` AND `chance`

**Setup:**
The surgeon's tent is open at dawn. A line forms — the coughers, the limpers, the quietly desperate.

{LANCE_MATE_NAME} nudges you. "You should go."

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Go, get checked out | Safe | +1 Fatigue | Treatment, +recovery speed | — |
| Tough it out another day | Risky | — | — | 10% condition worsens |
| Go but say it's fine | Safe | — | +15 Charm (convincing surgeon) | — |
| Ask {LANCE_MATE_NAME} to cover for you | Safe | — | +relation mate | — |

---

## Morning/Afternoon Events (7:00–17:00)

### DAY-01: Quartermaster's Offer

**Type:** General Event
**Time of Day:** Morning, Afternoon
**Trigger:** `in_settlement` OR `quartermaster_nearby`

**Setup:**
The quartermaster corners you near the supply wagons. He's got a proposition.

"Nice piece of kit came through. Not on the manifest, if you understand. Could be yours. Fifty denars."

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| "Not interested." | Safe | — | — | — |
| "I should report this." | Safe | — | +20 Leadership, −relation quartermaster | — |
| "Show me." (negotiate) | Risky | — | +25 Trade, possible equipment | — |
| "Deal." | Corrupt | −50 Gold, +3 Heat | Equipment, +20 Roguery | — |

---

### DAY-02: Work Detail

**Type:** General Event
**Time of Day:** Morning, Afternoon
**Trigger:** `not_training` AND `chance`

**Setup:**
{SECOND_SHORT} is assigning work details. Latrine duty, wood gathering, water hauling.

"You, {PLAYER_NAME}. Pick one or I pick for you."

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Latrine duty (worst, fastest) | Safe | +1 Fatigue | +15 Steward | — |
| Wood gathering (moderate) | Safe | +2 Fatigue | +15 Steward, +15 Athletics | — |
| Water hauling (heavy) | Safe | +2 Fatigue | +15 Steward, +20 Athletics | — |
| "I've got training, {SECOND_RANK}." | Risky | — | Skip if convincing, extra duty if not | — |

---

### DAY-03: Petty Conflict

**Type:** General Event
**Time of Day:** Morning, Afternoon
**Trigger:** `morale_shock > 20` OR `random_chance`

**Setup:**
{LANCE_MATE_NAME} and {SOLDIER_NAME} are at each other's throats. Something about borrowed equipment, insults, old grudges.

It's about to turn physical.

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Step between them, break it up | Safe | +1 Fatigue | +25 Charm, +relation both | — |
| Let them fight, keep watch for officers | Risky | — | +15 Roguery, they settle it | — |
| Get {LANCE_LEADER_SHORT} | Safe | — | +relation leader, −relation fighters | — |
| Take sides (with {LANCE_MATE_NAME}) | Risky | — | +relation mate, −relation soldier | 5% Minor (brawl) |

---

## Evening Events (17:00–20:00)

### EVE-01: Evening Mess

**Type:** General Event
**Time of Day:** Evening
**Trigger:** `daily_tick`

**Setup:**
The cook bangs the pot. Evening meal. The line forms quickly — those at the back get the dregs.

{VETERAN_1_NAME} is already near the front.

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Get in line early | Safe | — | Better portion, −1 Fatigue | — |
| Help serve | Safe | +1 Fatigue | +15 Steward, cook remembers | — |
| Trade for better food | Risky | −15 Gold | Better portion, +10 Trade | — |
| Skip the line (intimidate) | Corrupt | +1 Discipline | Best portion, −relation lance | — |

---

### EVE-02: Pay Your Debts

**Type:** General Event
**Time of Day:** Evening
**Trigger:** `player_has_debt` OR `random_chance`

**Setup:**
{LANCE_MATE_NAME} finds you after dinner. "You owe me from that dice game. Three days ago. Remember?"

You remember. You were hoping they forgot.

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Pay up (honest) | Safe | −20 Gold | +relation mate, debt cleared | — |
| "Double or nothing?" | Risky | — | +25 Roguery (if win), −40 Gold (if lose) | — |
| Ask for more time | Safe | — | −5 relation mate | — |
| "What debt?" (deny) | Risky | +1 Discipline | −20 relation mate, possible fight | — |

---

### EVE-03: Letter from Home

**Type:** General Event
**Time of Day:** Evening
**Trigger:** `days_enlisted > 14` AND `random_chance`

**Setup:**
The supply wagons brought mail. Rare enough that everyone crowds around.

{RECRUIT_NAME} gets a letter. Their face falls as they read.

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Ask if they're alright | Safe | — | +20 Charm, +relation recruit | — |
| Give them space | Safe | — | — | — |
| Offer to help write a reply | Safe | +1 Fatigue | +25 Charm, +relation recruit | — |
| Mind your own business | Safe | — | — | — |

---

## Dusk Events (20:00–22:00)

### DUSK-01: Fire Circle

**Type:** General Event
**Time of Day:** Dusk
**Trigger:** `not_in_settlement` AND `morale_shock < 50`

**Setup:**
The fire crackles. {LANCE_NAME} gathers, passing a skin of something. {VETERAN_1_NAME} starts a story about a battle years past.

The war feels far away for a moment.

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Listen and learn | Safe | — | +15 Tactics, −1 Fatigue | — |
| Share your own story | Risky | — | +20 Charm (if good), embarrassment if bad | — |
| Accept a drink | Risky | +1 Heat | +15 Charm, −2 Fatigue, +relation lance | — |
| Turn in early | Safe | — | −1 Fatigue | — |

---

### DUSK-02: Gambling Circle

**Type:** General Event
**Time of Day:** Dusk, Evening
**Trigger:** `not_in_battle_soon` AND `random_chance`

**Setup:**
Dice rattle. Coins change hands. {SOLDIER_NAME} is running a game behind the supply wagons.

"Room for one more, {PLAYER_NAME}?"

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Play a few rounds (small stakes) | Risky | −10 Gold | +20 Roguery, +15 Charm, ±20 Gold | — |
| Play big (high stakes) | Risky | −50 Gold | +25 Roguery, ±100 Gold | — |
| Just watch | Safe | — | +10 Roguery, +10 Charm | — |
| "Gambling's trouble." (leave) | Safe | — | — | — |

---

### DUSK-03: Contraband Run

**Type:** General Event
**Time of Day:** Dusk
**Trigger:** `heat > 0` AND `random_chance`

**Setup:**
A soldier you don't recognize approaches. "Heard you know how to keep quiet. I've got goods to move. Split the profit?"

It's obviously stolen. Probably from the army's own supplies.

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| "I'm not interested." | Safe | — | — | — |
| "I should report you." | Safe | — | +15 Leadership, may make enemy | — |
| "What's the split?" (negotiate) | Corrupt | +3 Heat | +30 Roguery, +25 Trade, +50 Gold | — |
| "I'll take a cut to stay quiet." | Corrupt | +2 Heat | +20 Roguery, +25 Gold | — |

---

## Night Events (22:00–2:00)

### NIGHT-01: Night Watch

**Type:** General Event
**Time of Day:** Night
**Trigger:** `assigned_watch` OR `random_chance`

**Setup:**
Your watch. The camp sleeps around you, fires burning low. The darkness beyond the perimeter is absolute.

Four hours of staring into nothing.

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Stay sharp, walk the line | Safe | +2 Fatigue | +25 Scouting, +15 Athletics | — |
| Find a good spot, stay still and watch | Safe | +2 Fatigue | +20 Scouting | — |
| Practice with your weapon (quiet) | Safe | +2 Fatigue | +15 Scouting, +20 (weapon skill) | — |
| Rest your eyes, trust your ears | Risky | +1 Fatigue | — | 15% discipline if caught |

---

### NIGHT-02: Can't Sleep

**Type:** General Event
**Time of Day:** Night
**Trigger:** `player_stressed` OR `after_battle_recently` OR `random_chance`

**Setup:**
Sleep won't come. Too many faces. Too many sounds. The battle keeps replaying behind your eyes.

{LANCE_MATE_NAME} is awake too, staring at the stars.

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Talk to them | Safe | — | +20 Charm, +relation mate, −stress | — |
| Walk the camp | Safe | +1 Fatigue | +15 Scouting, −stress | — |
| Write in a journal | Safe | — | +15 Steward, −stress | — |
| Drink until you sleep | Risky | +1 Heat | −2 Fatigue, −stress, possible hangover | — |

---

### NIGHT-03: Night Alarm

**Type:** General Event
**Time of Day:** Night, Late Night
**Trigger:** `enemy_nearby` OR `random_chance`

**Setup:**
"To arms! To arms!"

The cry goes up. Torches flare. You're on your feet before you're awake, grabbing for weapons.

It might be nothing. It might be everything.

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Form up with {LANCE_NAME} | Safe | +1 Fatigue | +15 Tactics, +15 Leadership | — |
| Rush toward the alarm | Risky | +1 Fatigue | +20 Athletics, +15 Tactics | 10% Minor if fighting |
| Help others get ready | Safe | +1 Fatigue | +20 Leadership, +relation lance | — |
| Stay alert, watch for secondary threats | Safe | +1 Fatigue | +25 Scouting, +15 Tactics | — |

---

## Late Night Events (2:00–5:00)

### LATE-01: Something's Wrong

**Type:** General Event
**Time of Day:** Late Night
**Trigger:** `on_watch` AND `random_chance`

**Setup:**
You heard something. Or didn't hear something that should be there. The night feels wrong.

Could be your imagination. Could be death coming.

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Investigate quietly | Risky | +1 Fatigue | +35 Scouting (if something there) | 15% if ambush |
| Sound the alarm | Safe | +1 Fatigue | +20 Scouting, possible false alarm | — |
| Alert the nearest soldier quietly | Safe | +1 Fatigue | +25 Scouting, +15 Leadership | — |
| It's nothing, continue watch | Risky | — | — | 20% if something there |

---

### LATE-02: Dying Man

**Type:** General Event
**Time of Day:** Late Night
**Trigger:** `after_battle_recently` AND `wounded_in_camp`

**Setup:**
Moaning from the surgeon's tent. Someone who won't make it to dawn. 

{FIELD_MEDIC_NAME} catches your eye. "Nothing more to do. Except be there."

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Sit with them | Safe | +1 Fatigue | +25 Charm, +20 Medicine, heavy moment | — |
| Help ease the pain (if supplies) | Safe | +1 Fatigue | +30 Medicine | — |
| Pray with them (if religious) | Safe | +1 Fatigue | +25 Charm | — |
| You can't face it (leave) | Safe | — | −relation if seen | — |

---

### LATE-03: Deserter Spotted

**Type:** General Event
**Time of Day:** Late Night
**Trigger:** `morale_shock > 50` OR `random_chance`

**Setup:**
Movement at the edge of camp. Someone's leaving — pack on their back, moving quiet.

You recognize them. {SOLDIER_NAME}. Running.

**Options:**

| Option | Risk | Cost | Reward | Injury |
|--------|------|------|--------|--------|
| Sound the alarm | Safe | — | +15 Leadership, {SOLDIER_NAME} caught | — |
| Stop them yourself | Risky | +1 Fatigue | +25 Athletics, +20 Leadership | 10% if fight |
| Let them go | Safe | +1 Discipline if seen | — | — |
| Talk to them first | Safe | +1 Fatigue | +25 Charm, might convince them to stay | — |

---

# Quick Reference Tables

## Duty Event Summary

| Duty | Events | Primary Skills |
|------|--------|----------------|
| Quartermaster | 5 | Steward, Trade, Roguery |
| Scout | 5 | Scouting, Tactics, Athletics |
| Field Medic | 5 | Medicine, Steward, Charm |
| Messenger | 5 | Riding, Athletics, Charm |
| Armorer | 5 | Smithing, Engineering, Leadership |
| Runner | 5 | Athletics, Scouting, Leadership |
| Lookout | 5 | Scouting, Tactics, Leadership |
| Engineer | 5 | Engineering, Smithing, Athletics |
| Boatswain | 5 | Boatswain, Leadership, Athletics |
| Navigator | 5 | Shipmaster, Scouting, Tactics |

## Training Event Summary

| Formation | Events | Primary Skills |
|-----------|--------|----------------|
| Infantry | 4 | Polearm, OneHanded, TwoHanded, Athletics |
| Cavalry | 4 | Riding, Polearm, Throwing, Bow |
| Archer | 4 | Bow, Crossbow, Scouting, Athletics |
| Naval | 4 | Mariner, Shipmaster, Athletics, Leadership |

## General Event Summary

| Time Period | Events | Themes |
|-------------|--------|--------|
| Dawn | 3 | Waking, muster, condition |
| Day | 3 | Work, conflict, corruption |
| Evening | 3 | Food, debts, social |
| Dusk | 3 | Campfire, gambling, contraband |
| Night | 3 | Watch, insomnia, alarms |
| Late Night | 3 | Danger, death, desertion |

---

*Document version: 1.0*
*Total Events: 84 (50 Duty + 16 Training + 18 General)*
*For use with: Lance Life Events System*
