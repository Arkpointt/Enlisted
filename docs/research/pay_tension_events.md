# Pay Tension Events — Lance Life

This document defines events that trigger when pay is delayed, giving players three paths: corruption, loyalty, or desertion.

---

## Overview

When pay is late, soldiers get desperate. PayTension (0-100) tracks this desperation and unlocks options:

### Three Paths at High Tension

| Path | Actions | Outcome |
|------|---------|---------|
| **Corrupt** | Theft, black market, skim supplies | Gold now, Heat accumulates |
| **Loyal** | Help lord missions (collect debts, escort, raid) | Build lord_rel, reduce tension |
| **Leave** | Desert freely (no penalty at 60+) | Clean break, start fresh |

### What Unlocks When

| Tension | Corrupt Options | Loyal Options | Leave Options |
|---------|-----------------|---------------|---------------|
| 0-19 | None | None | Normal desertion (penalties) |
| 20-39 | Grumbling events | None | Normal desertion |
| 40-49 | Bribe clerk, skim supplies | Collect Debts mission | Normal desertion |
| 50-59 | Black market, loot dead | Escort Merchant mission | Normal desertion |
| 60-69 | Sell equipment, confrontation | Negotiate Loan mission | **Free desertion** |
| 70-84 | Desertion talk | Raid Enemy mission | Free desertion |
| 85-100 | Mutiny brewing | All missions | Free desertion |

---

## Delivery Methods

All pay tension events are delivered through two channels:

| Delivery | Menu Type | Description |
|----------|-----------|-------------|
| `camp_activities` | GameMenu | Player navigates: Enlisted Status → Camp Activities |
| `post_battle` | InquiryPopup | Modal dialog after `MapEventEnded`, must respond before continuing |

### Menu Types Explained

**GameMenu (`camp_activities`):**
- Standard Bannerlord game menu
- Player chooses to engage
- Options appear/disappear based on PayTension threshold
- Located in: `enlisted_activities` menu

**InquiryPopup (`post_battle`):**
- Modal dialog box (like native Bannerlord inquiries)
- Fires automatically after battle via `MapEventEnded` hook
- Player must select an option to continue
- Cannot be ignored or dismissed without choice

### Menu Structure (from menu_system_update.md)

```
Enlisted Status Menu (enlisted_status)
│
├── [🏃] Camp Activities (enlisted_activities)  ← GameMenu
│       ├── — TRAINING —
│       ├── — CAMP TASKS —
│       ├── — SOCIAL —
│       ├── — DESPERATE MEASURES — [Only if PayTension >= 40]
│       │   ├── [Tension 40+] Visit Paymaster's Clerk
│       │   ├── [Tension 40+, QM/Armorer] Skim Supplies  
│       │   ├── [Tension 50+] Find the Black Market
│       │   ├── [Tension 60+] Sell Your Gear
│       │   └── [Tension 70+] Listen to Desertion Talk
│       │
│       └── — HELP THE LORD — [Only if PayTension >= 40]
│           ├── [Tension 40+] Collect Debts
│           ├── [Tension 50+] Escort Merchant
│           ├── [Tension 60+] Negotiate Loan
│           └── [Tension 70+] Volunteer for Raid
│
├── [🏕] Camp (enlisted_camp)
│       ├── Service Records
│       ├── Pay & Pension Status  ← Shows PayTension here
│       └── ...
│
└── [⚠] Desert the Army
        └── [Tension 60+] Desert freely (no penalty)
```

### Post-Battle Inquiry Flow

```
Battle Ends (MapEventEnded)
    │
    ▼
Check PayTension
    │
    ▼
Queue InquiryPopup(s)
    │
    ├─► Victory Celebration (if won)
    │
    └─► Pay Tension Event (based on threshold)
        ├── Grumbling (20+)
        ├── Theft Invitation (45+, 15% chance)
        ├── Loot the Dead (50+)
        ├── Confrontation (60+)
        └── Mutiny Brewing (85+)
```

---

## Trigger Conditions

| Trigger | Condition | Description |
|---------|-----------|-------------|
| `pay_tension_low` | PayTension 20-40 | Grumbling begins |
| `pay_tension_medium` | PayTension 40-60 | Desperation sets in |
| `pay_tension_high` | PayTension 60-80 | Near breaking point |
| `pay_tension_crisis` | PayTension 80+ | Mutiny territory |

---

## What Tension Does

PayTension (0-100) tracks how desperate soldiers are due to late pay. It has three effects:

### 1. Unlocks Events

Higher tension unlocks more desperate options:

| Tension | Events Unlocked |
|---------|-----------------|
| 0-19 | None — pay is fine |
| 20-39 | Grumbling (post-battle) |
| 40-49 | Bribe clerk, skim supplies, theft invitation |
| 50-59 | Black market, loot the dead |
| 60-69 | Confrontation, sell equipment |
| 70-84 | Desertion talk |
| 85-100 | Mutiny brewing |

### 2. Morale Penalty

High tension reduces party morale:

| Tension | Morale Modifier |
|---------|-----------------|
| 0-19 | 0 (no effect) |
| 20-39 | -3 morale |
| 40-59 | -6 morale |
| 60-79 | -10 morale |
| 80-100 | -15 morale |

```csharp
int GetPayTensionMoralePenalty()
{
    if (PayTension < 20) return 0;
    if (PayTension < 40) return -3;
    if (PayTension < 60) return -6;
    if (PayTension < 80) return -10;
    return -15;
}
```

### 3. Discipline Modifier

High tension makes discipline harder to maintain:

| Tension | Discipline Effect |
|---------|-------------------|
| 0-39 | Normal |
| 40-59 | +5% chance of discipline incidents |
| 60-79 | +10% chance of discipline incidents |
| 80-100 | +20% chance of discipline incidents |

Discipline incidents include: insubordination, fighting, desertion attempts.

### 4. Desertion Risk (NPC Soldiers)

At high tension, NPC soldiers in the party may desert:

| Tension | Daily Desertion Check |
|---------|----------------------|
| 0-59 | 0% |
| 60-79 | 1% per soldier |
| 80-89 | 3% per soldier |
| 90-100 | 5% per soldier |

```csharp
void CheckDesertions()
{
    if (PayTension < 60) return;
    
    float desertionChance = PayTension switch
    {
        >= 90 => 0.05f,
        >= 80 => 0.03f,
        >= 60 => 0.01f,
        _ => 0f
    };
    
    foreach (var soldier in Party.Soldiers)
    {
        if (MBRandom.RandomFloat < desertionChance)
        {
            // Soldier deserts
            RemoveSoldier(soldier);
            ShowNotification($"{soldier.Name} has deserted due to unpaid wages.");
        }
    }
}
```

### How Tension Changes

**Increases:**
- +10-15 per week pay is delayed
- +5 after lost battle (if already delayed)
- +10 if lord visibly wealthy but not paying

**Decreases:**
- -30 when full backpay received
- -10 when partial backpay received
- -5 per week pay is on time (slowly recovers)
- Reset to 0 if player transfers to new lord

---

## Events by Tension Level

### Low Tension (20-40) — Grumbling

---

#### `pay_tension_grumbling`

**ID:** `pay_tension_grumbling`
**Category:** pay
**Menu Type:** `InquiryPopup`
**Delivery:** Post-battle popup via `MapEventEnded` hook
**Trigger:** `pay_tension >= 20` + `after_battle`
**Cooldown:** 7 days

**Setup:**
> Complaints about pay have become the main topic around the fire. Every conversation circles back to it.
>
> "Two weeks now," {VETERAN_1_NAME} mutters. "Two bloody weeks."
>
> Eyes turn to you. Where do you stand?

**Options:**

| Option | Risk | Cost | Reward | Notes |
|--------|------|------|--------|-------|
| Join the grumbling | Safe | — | +5 lance_rep, -3 leader_rel | Solidarity |
| Stay quiet | Safe | — | — | Neutral |
| Defend the lord | Safe | — | -5 lance_rep, +5 leader_rel | Loyalist |
| "Maybe we should do something about it" | Safe | — | +10 lance_rep, sets up future event | Rabble-rouser |

---

### Medium Tension (40-60) — Desperation

---

#### `pay_tension_bribe_clerk`

**ID:** `pay_tension_bribe_clerk`
**Category:** pay, corruption
**Menu Type:** `GameMenu` (enlisted_activities)
**Delivery:** Camp Activities → Desperate Measures → "Visit Paymaster's Clerk"
**Trigger:** `pay_tension >= 40`
**Cooldown:** Once per pay crisis

**Setup:**
> A clerk from the paymaster's office catches your eye in the market. He sidles over.
>
> "Your name could move up the priority list. When coin does arrive." He pauses. "For a small consideration."
>
> Twenty denars. That's what he wants. To make sure you get paid before the others.

**Options:**

| Option | Risk | Cost | Reward | Notes |
|--------|------|------|--------|-------|
| Pay the bribe | Corrupt | -20 gold, +1 Heat | Priority pay next muster, +15 Roguery XP | Works |
| Negotiate down | Risky | -10 gold, +1 Heat | 50% chance priority pay, +20 Trade XP | Might not work |
| Report the corruption | Safe | — | +10 lord_rel, +15 Leadership XP, clerk becomes enemy | Righteous |
| Refuse quietly | Safe | — | — | Clean hands |

**Success Text (bribe):**
> The clerk pockets the coin smoothly. "Consider it done. When pay comes, you're first in line."

**Success Text (negotiate):**
> "Ten? Fine. But you owe me." The clerk takes the coin. Whether he'll follow through... you'll see.

**Failure Text (negotiate):**
> "Ten's an insult." The clerk walks away. You're on your own.

---

#### `pay_tension_supply_skim`

**ID:** `pay_tension_supply_skim`
**Category:** pay, theft
**Menu Type:** `GameMenu` (enlisted_activities)
**Delivery:** Camp Activities → Desperate Measures → "Skim Supplies"
**Trigger:** `pay_tension >= 40` + (`duty == quartermaster` OR `duty == armorer`)
**Cooldown:** 14 days

**Setup:**
> You're alone with the supplies. Counting, cataloging. The ledger's in your hand.
>
> Nobody's watching. Easy enough to adjust the numbers. Skim a bit off the top. Sell it in town.
>
> Pay's late. Who knows when it's coming. The army won't miss a few rations. A spare sword. Some cloth.

**Options:**

| Option | Risk | Cost | Reward | Notes |
|--------|------|------|--------|-------|
| Skim a little | Corrupt | +2 Heat | +25-40 gold, +20 Roguery XP | Conservative |
| Skim a lot | Corrupt | +4 Heat | +50-80 gold, +25 Roguery XP | Greedy |
| Don't do it | Safe | — | — | Clean |
| Report the opportunity | Safe | — | +5 leader_rel, +10 Leadership XP | Extra loyal |

**Success Text (skim):**
> You adjust the numbers. A crate here, a bundle there. Gone from the ledger, into your pocket when you visit town.
>
> Easy money. Just don't make a habit of it.

**Caught Text (5% chance per Heat):**
> {LANCE_LEADER_SHORT} is waiting when you finish. Ledger in one hand, inventory list in the other.
>
> "Numbers don't match. Want to explain?"

**If Caught:** +3 Discipline, -15 leader_rel, -10 lance_rep, confiscate gold

---

#### `pay_tension_theft_invitation`

**ID:** `pay_tension_theft_invitation`
**Category:** pay, theft
**Menu Type:** `InquiryPopup`
**Delivery:** Post-battle popup via `MapEventEnded` hook
**Trigger:** `pay_tension >= 45` + `after_battle` + random chance (15%)
**Cooldown:** 21 days

**Setup:**
> {SOLDIER_NAME} pulls you aside after evening meal. Checks that no one's watching.
>
> "Supply wagon. Third one from the end. Guard changes at midnight — there's a gap. Five minutes, maybe more."
>
> They look at you. "We grab some, sell it in town, split the coin. You in?"
>
> Pay's late. Lords don't care about us. Why should we care about their supplies?

**Options:**

| Option | Risk | Cost | Reward | Notes |
|--------|------|------|--------|-------|
| "I'm in. Let's do it." | Risky | +3 Heat | +40-70 gold, +25 Roguery XP, +15 {SOLDIER_NAME} rel | Partner in crime |
| "I'll keep watch. You do the work." | Risky | +2 Heat | +20-35 gold, +15 Roguery XP, +10 {SOLDIER_NAME} rel | Accomplice |
| "Too risky. Count me out." | Safe | — | -10 {SOLDIER_NAME} rel | Cautious |
| Report them | Safe | — | -25 lance_rep, +15 lord_rel, +20 Leadership XP | Snitch |

**Success Text:**
> Midnight. The gap's there, just like they said. You move fast, quiet. A sack of goods into the shadows.
>
> By morning, it's coin in your pocket. {SOLDIER_NAME} grins. "Not bad, eh? Same time next week?"

**Failure Text (15% chance):**
> The guard comes back early. You freeze. They see you.
>
> "Hey! What are you—"
>
> You run. Behind you, shouting. The alarm goes up.

**If Failed:** +5 Heat, +2 Discipline, investigation event queued

---

#### `pay_tension_black_market`

**ID:** `pay_tension_black_market`
**Category:** pay, corruption
**Menu Type:** `GameMenu` (enlisted_activities)
**Delivery:** Camp Activities → Desperate Measures → "Find the Black Market"
**Trigger:** `pay_tension >= 50`
**Cooldown:** 14 days

**Setup:**
> The tavern's back room. A merchant you don't recognize. Army goods spread on the table — food, medicine, weapons. All of it stolen.
>
> "Looking to buy? Or sell?" He grins. "Either way, I can help. Soldier's discount."
>
> No questions asked. Cash only.

**Options:**

| Option | Risk | Cost | Reward | Notes |
|--------|------|------|--------|-------|
| Buy supplies cheap | Corrupt | -30 gold, +1 Heat | Supplies worth 50+ gold, +15 Trade XP | Good deal |
| Sell your loot | Safe | +2 Heat | Convert loot items to 60% value gold | Fence |
| "Know anyone hiring?" | Risky | +1 Heat | Contact for future desertion, +10 Roguery XP | Escape route |
| Leave | Safe | — | — | Clean |
| Report location to officers | Safe | — | +10 lord_rel, merchant becomes enemy | Righteous |

---

### High Tension (60-80) — Breaking Point

---

#### `pay_tension_loot_the_dead`

**ID:** `pay_tension_loot_the_dead`
**Category:** pay, theft
**Menu Type:** `InquiryPopup`
**Delivery:** Post-battle popup via `MapEventEnded` hook
**Trigger:** `pay_tension >= 50` + `after_battle`
**Cooldown:** Once per battle

**Setup:**
> The battle's done. Bodies everywhere — enemy and our own. The burial detail hasn't started yet.
>
> Nobody's watching the dead too closely. Not yet.
>
> You could check their pockets. Coin, rings, anything valuable. Pay's late anyway — consider it compensation.

**Options:**

| Option | Risk | Cost | Reward | Notes |
|--------|------|------|--------|-------|
| Search enemy dead only | Risky | +2 Heat | +15-45 gold, +15 Roguery XP | Acceptable |
| Search everyone | Risky | +4 Heat | +30-80 gold, +25 Roguery XP | Taboo |
| Take a dead man's weapon | Risky | +1 Heat | Random weapon, +10 Roguery XP | Practical |
| Leave the dead in peace | Safe | — | — | Respectful |
| Stop others from looting | Safe | — | +5 leader_rel, +15 Leadership XP, -10 lance_rep | Enforcer |

**Success Text (enemy):**
> You move among the fallen, quick and quiet. A few coins here. A ring there. Nobody notices. Nobody cares.

**Success Text (everyone):**
> Even our own dead have coin. Had coin. You try not to look at faces you recognize.
>
> {LANCE_MATE_NAME}'s purse is heavy. Was heavy. Now it's yours.

**Caught Text (10% chance):**
> {LANCE_LEADER_SHORT} catches you elbow-deep in a dead man's kit.
>
> "The hell are you doing?"

**If Caught:** +2 Discipline, -15 leader_rel, -10 lance_rep

---

#### `pay_tension_confrontation`

**ID:** `pay_tension_confrontation`
**Category:** pay
**Menu Type:** `InquiryPopup`
**Delivery:** Post-battle popup via `MapEventEnded` hook (high priority)
**Trigger:** `pay_tension >= 60` + `after_battle`
**Cooldown:** Once per crisis

**Setup:**
> {VETERAN_1_NAME} is gathering soldiers. Quietly, but with purpose.
>
> "We're going to the paymaster. All of us. Demand what we're owed." They look at you. "You coming?"
>
> It's not mutiny. Not yet. Just soldiers asking for what they earned. But it's close to the line.

**Options:**

| Option | Risk | Cost | Reward | Notes |
|--------|------|------|--------|-------|
| Join the delegation | Risky | — | +15 lance_rep, -10 lord_rel, triggers resolution event | Solidarity |
| Lead the delegation | Risky | — | +25 lance_rep, -15 lord_rel, +20 Leadership XP | Take charge |
| Talk them down | Safe | — | -10 lance_rep, +10 leader_rel, +15 Charm XP | Peacemaker |
| Stay out of it | Safe | — | — | Neutral |
| Warn the officers | Safe | — | -30 lance_rep, +20 lord_rel, +15 Leadership XP | Informant |

**Resolution Event (if delegation happens):**
> The paymaster sees you coming. Forty soldiers, armed, angry. He goes pale.
>
> "The coin's coming," he stammers. "Three days. I swear it."
>
> Three days. You'll hold him to that.

**Effect:** Pay arrives in 3 days OR triggers `pay_written_off` event

---

#### `pay_tension_sell_equipment`

**ID:** `pay_tension_sell_equipment`
**Category:** pay, theft
**Menu Type:** `GameMenu` (enlisted_activities)
**Delivery:** Camp Activities → Desperate Measures → "Sell Your Gear"
**Trigger:** `pay_tension >= 60`
**Cooldown:** 21 days

**Setup:**
> Your army-issued kit. Sword, armor, gear. It's not yours — it belongs to the lord. But right now, it's in your hands.
>
> A merchant in town would pay good coin for it. You could claim it was lost in battle. Stolen. Damaged beyond repair.
>
> Pay's not coming. But this coin could be in your pocket tonight.

**Options:**

| Option | Risk | Cost | Reward | Notes |
|--------|------|------|--------|-------|
| Sell your sword | Risky | +3 Heat, lose weapon | +60-120 gold, +20 Roguery XP | Desperate |
| Sell your armor | Risky | +4 Heat, lose armor | +80-200 gold, +25 Roguery XP | Very desperate |
| Sell spare gear | Risky | +2 Heat | +30-50 gold, +15 Roguery XP | Safer |
| Don't do it | Safe | — | — | Keep your kit |

**Consequence Event (triggers later):**
> Equipment inspection. {LANCE_LEADER_SHORT} is checking everyone's gear.
>
> "Where's your sword, soldier?"

**Options for consequence:**
| Option | Effect |
|--------|--------|
| "Lost it in the last battle" | Roguery check — success: believed, fail: +2 Discipline |
| "It was stolen" | -5 lance_rep (someone gets blamed) |
| "I sold it" | +4 Discipline, -20 leader_rel, replacement cost deducted from pay |

---

### Crisis Tension (80+) — Mutiny

---

#### `pay_tension_desertion_talk`

**ID:** `pay_tension_desertion_talk`
**Category:** pay, desertion
**Menu Type:** `GameMenu` (enlisted_activities)
**Delivery:** Camp Activities → Desperate Measures → "Listen to Desertion Talk"
**Trigger:** `pay_tension >= 70`
**Cooldown:** 14 days

**Setup:**
> Late night. Away from the fire. Whispers in the dark.
>
> {SOLDIER_NAME} spots you. Hesitates. Waves you over.
>
> "We're thinking about leaving. All of us. Why stay? They don't pay. They don't care." They look you in the eye. "You coming?"

**Options:**

| Option | Risk | Cost | Reward | Notes |
|--------|------|------|--------|-------|
| "I'm with you. When?" | — | — | Join desertion plot, +20 {SOLDIER_NAME} rel | Committed |
| "I need to think about it" | — | — | Future event, +5 {SOLDIER_NAME} rel | On the fence |
| "I'm staying. But I didn't hear this." | Safe | — | -15 {SOLDIER_NAME} rel | Silent |
| "I'm staying. And I'm reporting this." | Safe | — | -35 lance_rep, +20 lord_rel, multiple enemies | Loyal |

**If joined desertion plot:** Triggers `desertion_planning` event chain

---

#### `pay_tension_mutiny_brewing`

**ID:** `pay_tension_mutiny_brewing`
**Category:** pay, mutiny
**Menu Type:** `InquiryPopup`
**Delivery:** Post-battle popup via `MapEventEnded` hook (critical priority)
**Trigger:** `pay_tension >= 85` + `after_battle`
**Cooldown:** Once per crisis

**Setup:**
> It's gone beyond talk. Beyond delegation. Beyond asking.
>
> Soldiers are gathering. Weapons drawn. Not pointed at the enemy — pointed at the officer's tents.
>
> {VETERAN_1_NAME} finds you. "It's happening. Tonight. Either you're with us, or you're with them."
>
> There's no middle ground now.

**Options:**

| Option | Risk | Cost | Reward | Notes |
|--------|------|------|--------|-------|
| Join the mutiny | — | — | Mutiny path, +30 lance_rep | Revolutionary |
| Try to stop it | Risky | — | Leadership check, +25 lord_rel OR -40 lance_rep | Dangerous |
| Slip away in the chaos | Risky | — | Desert without consequences, +20 Roguery XP | Opportunist |
| Stand with the officers | — | — | -50 lance_rep, +40 lord_rel, combat likely | Loyalist |

**Mutiny Outcomes:** Separate event chain based on choice

---

## Loyal Path — Help the Lord Missions

These missions let players help the lord recover financially, reducing tension and building reputation.

---

#### `mission_collect_debts`

**ID:** `mission_collect_debts`
**Category:** pay, loyal
**Menu Type:** `GameMenu` (enlisted_activities)
**Delivery:** Camp Activities → Help the Lord → "Collect Debts"
**Trigger:** `pay_tension >= 40`
**Cooldown:** 14 days

**Setup:**
> The lord is owed money. Merchants, minor nobles, old debts gathering dust. Someone needs to collect.
>
> {LANCE_LEADER_SHORT} mentions they're looking for volunteers. "Delicate work. Requires... persuasion."
>
> Do this right, and the lord remembers. Maybe pay comes faster.

**Options:**

| Option | Risk | Cost | Reward | Notes |
|--------|------|------|--------|-------|
| Collect politely | Safe | — | +10 lord_rel, +15 Charm XP, -10 PayTension | Diplomatic |
| Collect aggressively | Risky | +2 Heat | +15 lord_rel, +20 Roguery XP, -15 PayTension | Intimidating |
| Skim some for yourself | Corrupt | +3 Heat | +5 lord_rel, +25 Roguery XP, -5 PayTension, +30 gold | Dishonest |
| Decline | Safe | — | — | Not interested |

**Success Text (polite):**
> You knock on doors. Remind them of obligations. Most pay — eventually. The lord gets their coin, and you get noticed.

**Success Text (aggressive):**
> You don't ask twice. A firm grip, a hard stare, a mention of consequences. They pay. Quickly.

---

#### `mission_escort_merchant`

**ID:** `mission_escort_merchant`
**Category:** pay, loyal
**Menu Type:** `GameMenu` (enlisted_activities)
**Delivery:** Camp Activities → Help the Lord → "Escort Merchant"
**Trigger:** `pay_tension >= 50`
**Cooldown:** 21 days

**Setup:**
> A merchant owes the lord goods, not gold. Cloth, iron, supplies. They're willing to deliver — but the roads are dangerous.
>
> "Need an escort," {LANCE_LEADER_SHORT} says. "Three days, maybe five. Volunteer, and the lord owes you a favor."

**Options:**

| Option | Risk | Cost | Reward | Notes |
|--------|------|------|--------|-------|
| Volunteer | Risky | 3-5 days travel | +15 lord_rel, -15 PayTension on success | Commitment |
| Decline | Safe | — | — | Too busy |

**Mission Flow:**
1. Player joins merchant caravan (temporary party)
2. Travel for 3-5 days
3. 40% chance: Bandit encounter (combat)
4. Arrive at destination
5. Return to lord's party

**Success Text:**
> The merchant arrives safely. Goods delivered. The lord nods when they hear your name.
>
> "Good work. I won't forget."

**Failure Text (if merchant dies):**
> The bandits were too many. The merchant didn't make it. Neither did the goods.
>
> The lord's disappointment is palpable. This didn't help.

---

#### `mission_negotiate_loan`

**ID:** `mission_negotiate_loan`
**Category:** pay, loyal
**Menu Type:** `GameMenu` (enlisted_activities)
**Delivery:** Camp Activities → Help the Lord → "Negotiate Loan"
**Trigger:** `pay_tension >= 60`
**Requirements:** `Charm > 80` OR `Trade > 60`

**Setup:**
> The lord needs coin. Badly. A moneylender in town might help — but they need convincing.
>
> "Your reputation precedes you," {LANCE_LEADER_SHORT} says. "The lord thinks you could tip the scales. Interested?"

**Options:**

| Option | Risk | Cost | Reward | Notes |
|--------|------|------|--------|-------|
| Use charm (Charm check) | Risky | — | +20 lord_rel, -20 PayTension on success | Persuasion |
| Use intimidation (Roguery check) | Risky | +2 Heat | +15 lord_rel, -15 PayTension on success | Threats |
| Offer your own savings | Safe | -100 gold | +25 lord_rel, -25 PayTension | Personal sacrifice |
| Decline | Safe | — | — | Not my problem |

**Success Text (charm):**
> Words flow. You paint pictures of future profits, of stability, of opportunity. The moneylender hesitates... then nods.
>
> "Very well. Tell your lord they have their loan."

**Success Text (intimidation):**
> You lean close. Mention certain... consequences. The moneylender's face goes pale.
>
> "Fine. Fine! Take the money. Just... leave."

**Success Text (own savings):**
> You hand over your coin. Every denar you've saved. The lord stares.
>
> "I... will not forget this. You have my word."

---

#### `mission_raid_enemy`

**ID:** `mission_raid_enemy`
**Category:** pay, loyal
**Menu Type:** `GameMenu` (enlisted_activities)
**Delivery:** Camp Activities → Help the Lord → "Volunteer for Raid"
**Trigger:** `pay_tension >= 70` + `faction_at_war`
**Cooldown:** 14 days

**Setup:**
> The lord's broke. But the enemy isn't.
>
> "Supply convoy," {LANCE_LEADER_SHORT} says quietly. "Poorly guarded. We hit it fast, take what we can, disappear before their army responds."
>
> Dangerous. But profitable. And the lord would owe you.

**Options:**

| Option | Risk | Cost | Reward | Notes |
|--------|------|------|--------|-------|
| Volunteer | Risky | Combat encounter | +20 lord_rel, -25 PayTension, 1.5x loot share | High risk, high reward |
| Decline | Safe | — | — | Too dangerous |

**Mission Flow:**
1. Player joins raid party (5-10 soldiers)
2. Combat encounter against enemy supply train
3. If victorious: Loot distributed, return to lord
4. If defeated: Captured or wounded

**Success Text:**
> The convoy never saw you coming. By the time they reacted, it was over.
>
> Wagons full of supplies. Coin. Weapons. Enough to pay the army for a month.
>
> The lord claps you on the shoulder. "This changes things. Well done."

---

## Leave Path — Free Desertion

At Tension 60+, the player can desert **without penalty**. The lord "understands" — you weren't paid.

---

#### Free Desertion

**Menu Location:** Enlisted Status → Desert the Army
**Trigger:** `pay_tension >= 60`

**Normal Desertion (Tension < 60):**
- 500 denar bounty placed
- -20 lord relation
- -10 faction relation  
- Hunted by patrols

**Free Desertion (Tension 60+):**
- No bounty
- -5 lord relation (mild disappointment)
- No faction penalty
- Keep your gear and gold
- Clean break

**Setup (Free Desertion):**
> You approach {LANCE_LEADER_SHORT}. They already know what you're going to say.
>
> "Can't blame you," they say quietly. "Pay's late. Lord's broke. No one would fault you for leaving."
>
> They extend a hand. "Go. Find something better. No hard feelings."

**Options:**

| Option | Effect |
|--------|--------|
| "Thank you. I'm leaving." | End enlistment, -5 lord_rel, no other penalties |
| "Actually, I'll stay." | Cancel desertion, +5 lance_rep (loyalty recognized) |

**Implementation:**

```csharp
void ShowDesertionMenu()
{
    bool freeDesertion = PayTension >= 60;
    
    if (freeDesertion)
    {
        InformationManager.ShowInquiry(new InquiryData(
            "Desert the Army",
            "Pay has been delayed too long. You can leave without penalty — no bounty, no faction hate. The lord understands.",
            true, true,
            "Leave (no penalty)",
            "Stay",
            () => ProcessFreeDesertion(),
            () => CancelDesertion()
        ));
    }
    else
    {
        InformationManager.ShowInquiry(new InquiryData(
            "Desert the Army",
            "Deserting will place a 500 denar bounty on your head. The lord and faction will remember this betrayal.",
            true, true,
            "Desert anyway",
            "Stay",
            () => ProcessNormalDesertion(),
            () => CancelDesertion()
        ));
    }
}

void ProcessFreeDesertion()
{
    ChangeLordRelation(-5);
    // No bounty, no faction penalty
    EndEnlistment();
    ShowMessage("You leave quietly. No one blames you — you weren't paid.");
}

void ProcessNormalDesertion()
{
    PlaceBounty(500);
    ChangeLordRelation(-20);
    ChangeFactionRelation(-10);
    EndEnlistment();
    ShowMessage("You're now a deserter. Watch your back.");
}
```

---

## Implementation

### PayTension Escalation Logic

```csharp
void OnDailyTick()
{
    if (!IsEnlisted()) return;
    
    var payState = GetPayState();
    
    // Check if pay is due
    if (payState.DaysSincePay >= 7)
    {
        bool canPay = CanLordPay();
        bool inSettlement = IsInSettlement();
        
        if (canPay && inSettlement)
        {
            // Pay muster happens
            ProcessPayMuster();
        }
        else
        {
            // Pay delayed
            payState.PayTension += GetTensionIncrease();
            CheckPayTensionEvents();
        }
    }
}

int GetTensionIncrease()
{
    int daysMissed = (DaysSincePay - 7) / 7; // Weeks overdue
    return 10 + (daysMissed * 5); // Escalates over time
}

void CheckPayTensionEvents()
{
    var tension = PayState.PayTension;
    
    if (tension >= 85 && !HasFired("pay_tension_mutiny_brewing"))
        QueueEvent("pay_tension_mutiny_brewing");
    else if (tension >= 70 && !HasFired("pay_tension_desertion_talk"))
        QueueEvent("pay_tension_desertion_talk");
    else if (tension >= 60 && !HasFired("pay_tension_confrontation"))
        QueueEvent("pay_tension_confrontation");
    // ... etc
}
```

### Event Selection by Tension

```csharp
List<string> GetAvailablePayTensionEvents(int tension)
{
    var events = new List<string>();
    
    // Low tension (20-40)
    if (tension >= 20)
        events.Add("pay_tension_grumbling");
    
    // Medium tension (40-60)
    if (tension >= 40)
    {
        events.Add("pay_tension_bribe_clerk");
        if (HasDuty("quartermaster") || HasDuty("armorer"))
            events.Add("pay_tension_supply_skim");
        events.Add("pay_tension_theft_invitation");
    }
    
    if (tension >= 50)
    {
        events.Add("pay_tension_black_market");
        if (RecentBattle())
            events.Add("pay_tension_loot_the_dead");
    }
    
    // High tension (60-80)
    if (tension >= 60)
    {
        events.Add("pay_tension_confrontation");
        events.Add("pay_tension_sell_equipment");
    }
    
    // Crisis (80+)
    if (tension >= 70)
        events.Add("pay_tension_desertion_talk");
    
    if (tension >= 85)
        events.Add("pay_tension_mutiny_brewing");
    
    return FilterByCooldown(events);
}
```

### Config Schema

```json
{
  "pay_tension_events": {
    "tension_thresholds": {
      "low": 20,
      "medium": 40,
      "high": 60,
      "crisis": 80
    },
    "events": {
      "pay_tension_grumbling": {
        "min_tension": 20,
        "cooldown_days": 7,
        "triggers": ["days_since_pay >= 8"]
      },
      "pay_tension_bribe_clerk": {
        "min_tension": 40,
        "cooldown_days": 0,
        "once_per_crisis": true,
        "triggers": ["in_settlement"]
      },
      "pay_tension_supply_skim": {
        "min_tension": 40,
        "cooldown_days": 14,
        "triggers": ["duty_quartermaster OR duty_armorer"]
      },
      "pay_tension_theft_invitation": {
        "min_tension": 45,
        "cooldown_days": 21,
        "random_chance": 0.15
      },
      "pay_tension_black_market": {
        "min_tension": 50,
        "cooldown_days": 14,
        "triggers": ["in_settlement"]
      },
      "pay_tension_loot_the_dead": {
        "min_tension": 50,
        "cooldown_days": 0,
        "once_per_battle": true,
        "triggers": ["after_battle"]
      },
      "pay_tension_confrontation": {
        "min_tension": 60,
        "cooldown_days": 0,
        "once_per_crisis": true,
        "triggers": ["days_since_pay >= 21"]
      },
      "pay_tension_sell_equipment": {
        "min_tension": 60,
        "cooldown_days": 21,
        "triggers": ["in_settlement"]
      },
      "pay_tension_desertion_talk": {
        "min_tension": 70,
        "cooldown_days": 14
      },
      "pay_tension_mutiny_brewing": {
        "min_tension": 85,
        "cooldown_days": 0,
        "once_per_crisis": true,
        "triggers": ["days_since_pay >= 28"]
      }
    },
    "theft_rewards": {
      "supply_skim_small": { "min": 25, "max": 40 },
      "supply_skim_large": { "min": 50, "max": 80 },
      "theft_wagon": { "min": 40, "max": 70 },
      "loot_enemy": { "min": 15, "max": 45 },
      "loot_all": { "min": 30, "max": 80 },
      "sell_sword": { "min": 60, "max": 120 },
      "sell_armor": { "min": 80, "max": 200 }
    },
    "caught_chances": {
      "supply_skim": 0.05,
      "loot_dead": 0.10,
      "theft_wagon": 0.15
    }
  }
}
```

---

## Event Flow Summary

```
Pay Delayed
    │
    ▼
PayTension Increases (+10-15 per week)
    │
    ├─► 20-40: Grumbling events
    │
    ├─► 40-60: Theft opportunities
    │   ├── Bribe clerk
    │   ├── Skim supplies (if QM/Armorer)
    │   ├── Theft invitation from soldier
    │   └── Black market contact
    │
    ├─► 60-80: Desperate measures
    │   ├── Loot the dead (after battle)
    │   ├── Confront paymaster
    │   └── Sell your equipment
    │
    └─► 80+: Breaking point
        ├── Desertion talk
        └── Mutiny brewing
```

---

## Event Delivery Summary

| Event | Tension | Menu Type | Location |
|-------|---------|-----------|----------|
| `pay_tension_grumbling` | 20+ | InquiryPopup | Post-battle (`MapEventEnded`) |
| `pay_tension_bribe_clerk` | 40+ | GameMenu | enlisted_activities → Desperate Measures |
| `pay_tension_supply_skim` | 40+ | GameMenu | enlisted_activities → Desperate Measures |
| `pay_tension_theft_invitation` | 45+ | InquiryPopup | Post-battle (`MapEventEnded`) |
| `pay_tension_black_market` | 50+ | GameMenu | enlisted_activities → Desperate Measures |
| `pay_tension_loot_the_dead` | 50+ | InquiryPopup | Post-battle (`MapEventEnded`) |
| `pay_tension_confrontation` | 60+ | InquiryPopup | Post-battle (`MapEventEnded`) |
| `pay_tension_sell_equipment` | 60+ | GameMenu | enlisted_activities → Desperate Measures |
| `pay_tension_desertion_talk` | 70+ | GameMenu | enlisted_activities → Desperate Measures |
| `pay_tension_mutiny_brewing` | 85+ | InquiryPopup | Post-battle (`MapEventEnded`) |

### Implementation: GameMenu (Camp Activities)

```csharp
// In enlisted_activities menu builder
void BuildDesperateMeasuresSection(GameMenuStarterObject starter)
{
    if (PayTension < 40) return;
    
    // Section header
    starter.AddGameMenuOption("enlisted_activities", "desperate_header",
        "— DESPERATE MEASURES —",
        args => false, // Not clickable, just header
        args => PayTension >= 40);
    
    // Tension 40+
    starter.AddGameMenuOption("enlisted_activities", "pay_bribe_clerk",
        "Visit Paymaster's Clerk",
        args => { LaunchEvent("pay_tension_bribe_clerk"); },
        args => PayTension >= 40 && !HasFired("pay_tension_bribe_clerk"));
    
    if (CurrentDuty == "quartermaster" || CurrentDuty == "armorer")
    {
        starter.AddGameMenuOption("enlisted_activities", "pay_skim_supplies",
            "Skim Supplies",
            args => { LaunchEvent("pay_tension_supply_skim"); },
            args => PayTension >= 40 && OffCooldown("pay_tension_supply_skim"));
    }
    
    // Tension 50+
    starter.AddGameMenuOption("enlisted_activities", "pay_black_market",
        "Find the Black Market",
        args => { LaunchEvent("pay_tension_black_market"); },
        args => PayTension >= 50 && OffCooldown("pay_tension_black_market"));
    
    // Tension 60+
    starter.AddGameMenuOption("enlisted_activities", "pay_sell_gear",
        "Sell Your Gear",
        args => { LaunchEvent("pay_tension_sell_equipment"); },
        args => PayTension >= 60 && OffCooldown("pay_tension_sell_equipment"));
    
    // Tension 70+
    starter.AddGameMenuOption("enlisted_activities", "pay_desertion_talk",
        "Listen to Desertion Talk",
        args => { LaunchEvent("pay_tension_desertion_talk"); },
        args => PayTension >= 70 && OffCooldown("pay_tension_desertion_talk"));
}
```

### Implementation: InquiryPopup (Post-Battle)

```csharp
// Hook into MapEventEnded
void OnMapEventEnded(MapEvent mapEvent)
{
    if (!IsEnlisted()) return;
    if (!mapEvent.IsPlayerInvolved()) return;
    
    // Queue post-battle events
    var events = new List<string>();
    
    // Victory celebration (if won)
    if (mapEvent.WinningSide == PlayerSide)
        events.Add("gen_victory_celebration");
    
    // Pay tension events (priority order - only highest fires)
    string tensionEvent = GetPayTensionEvent();
    if (tensionEvent != null)
        events.Add(tensionEvent);
    
    // Fire as InquiryPopups in sequence
    foreach (var eventId in events)
    {
        ShowInquiryPopup(eventId);
    }
}

string GetPayTensionEvent()
{
    if (PayTension >= 85 && !HasFired("pay_tension_mutiny_brewing"))
        return "pay_tension_mutiny_brewing";
    if (PayTension >= 60 && !HasFired("pay_tension_confrontation"))
        return "pay_tension_confrontation";
    if (PayTension >= 50)
        return "pay_tension_loot_the_dead";
    if (PayTension >= 45 && MBRandom.RandomFloat < 0.15f && OffCooldown("pay_tension_theft_invitation"))
        return "pay_tension_theft_invitation";
    if (PayTension >= 20 && OffCooldown("pay_tension_grumbling"))
        return "pay_tension_grumbling";
    return null;
}

void ShowInquiryPopup(string eventId)
{
    var eventData = GetEventData(eventId);
    
    InformationManager.ShowInquiry(new InquiryData(
        eventData.Title,
        eventData.Setup,
        true, true,
        eventData.Options[0].Text,
        eventData.Options[1].Text,
        () => HandleOption(eventId, 0),
        () => HandleOption(eventId, 1)
    ));
}
```

---

## Cross-References

- **Pay System:** `/mnt/user-data/outputs/pay_system.md`
- **Heat System:** `/mnt/user-data/outputs/escalation_system.md`
- **Lance Life Schemas:** `/mnt/user-data/outputs/lance_life_schemas.md`

---

*Document Version: 1.0*
*For use with: Lance Life Pay System*
