# Pay System — Lance Life

This document defines soldier pay, payment schedules, deductions, bonuses, and economic balance for the enlisted experience.

---

## Implementation Status

> **Note:** Bannerlord provides all the native APIs needed. We just need to write the logic to use them.

### ✅ Native Bannerlord APIs Available

| Data | Access | Notes |
|------|--------|-------|
| Lord's gold | `hero.Gold` | Direct property |
| Player's gold | `Hero.MainHero.Gold` | Direct property |
| War state | `FactionManager.IsAtWarAgainstFaction()` | Check if faction at war |
| Siege state | `Settlement.IsUnderSiege` | Check settlement |
| Culture | `hero.Culture.StringId` | String ID lookup |
| Troop wages | `CharacterObject.TroopWage` | Native wage calc |
| Time tracking | `CampaignTime` | Days, hours, etc. |
| Settlement check | `Settlement.CurrentSettlement` | Where player is |
| Battle end hook | `MapEventEnded` | Post-battle trigger |
| Hourly tick | `HourlyTickEvent` | Time-based checks |

### Needs Implementation (Logic Only)

| System | What to Build | Complexity |
|--------|---------------|------------|
| PayState class | Track days_since_pay, owed_amount, pay_tension | Simple |
| Base pay lookup | Dictionary of tier → pay | Simple |
| Pay muster trigger | Check days elapsed + in settlement | Simple |
| Lord wealth modifier | Check lord.Gold against thresholds | Simple |
| Wartime modifier | Check faction war state | Simple |
| Culture modifier | Dictionary lookup | Simple |
| Duty modifier | Read from duty config | Simple |
| Pay tension escalation | Increment on delay, trigger events | Medium |
| Pay events | Wire to event delivery system | Medium |
| Battle bonuses | Hook MapEventEnded, calc bonus | Medium |
| Loot share | % of loot based on tier + item chance roll | Medium |
| Lance fund | Deduct 5%, track balance | Simple |

### Implementation Order (Recommended)

```
Phase A (Core Pay):
1. PayState class
2. Base pay lookup table  
3. Pay muster hook (HourlyTick + settlement + 7 days)
4. Pay delivery (add gold to player)

Phase B (Modifiers):
5. Lord wealth modifier (lord.Gold thresholds)
6. Duty modifier (read wage_multiplier from config)
7. Wartime modifier (faction war check)
8. Culture modifier (culture lookup)

Phase C (Events):
9. Pay muster events (normal, delayed, partial)
10. Pay tension escalation logic
11. Pay tension threshold events

Phase D (Advanced):
12. Battle bonuses (post-battle hook)
13. Loot share system
14. Lance fund tracking
```

---

## Table of Contents

1. [Design Philosophy](#design-philosophy)
2. [Base Pay by Tier](#base-pay-by-tier)
3. [Pay Modifiers](#pay-modifiers)
4. [Payment Schedule](#payment-schedule)
5. [Deductions](#deductions)
6. [Bonuses & Windfalls](#bonuses--windfalls)
7. [Economic Balance](#economic-balance)
8. [Pay Events](#pay-events)
9. [Implementation](#implementation)

---

## Design Philosophy

### Soldiers Should Be Broke (But Surviving)

The enlisted experience is about being a grunt, not a merchant prince. Pay should:

1. **Cover basics** — Food, basic repairs, small luxuries
2. **Not cover gear** — Takes weeks/months to save for equipment upgrades
3. **Create tension** — Pay delays matter, bonuses feel meaningful
4. **Reward progression** — Higher tiers earn noticeably more
5. **Match Bannerlord** — Consistent with native troop wage economy

### Reference: Bannerlord Economy

| Item | Cost (Denars) |
|------|---------------|
| Cheap meal | 5-10 |
| Tavern drink | 2-5 |
| Basic sword | 100-300 |
| Good sword | 500-1500 |
| Basic armor | 200-500 |
| Good armor | 2000-10000 |
| Horse (riding) | 200-400 |
| Horse (war) | 800-2000 |
| Recruit troop (native) | ~1-2/day |
| Elite troop (native) | ~15-30/day |

### Target Feel

| Tier | Economic Feel |
|------|---------------|
| T1 | Scraping by. Can afford food, nothing else. |
| T2 | Stable but poor. Can save slowly for basic gear. |
| T3 | Comfortable grunt. Can afford decent meals, occasional drink. |
| T4 | Professional soldier. Can save for good gear over months. |
| T5 | Well-paid veteran. Some spending money. |
| T6 | Elite. Lord's household means better everything. |
| T7+ | Officer pay. Actually comfortable. |

---

## Base Pay by Tier

### Daily Base Pay

| Tier | Rank (Generic) | Base Pay/Day | Weekly (7d) | Monthly (28d) | Annual (336d) |
|------|----------------|--------------|-------------|---------------|---------------|
| T1 | Levy | 3 | 21 | 84 | 1,008 |
| T2 | Recruit | 6 | 42 | 168 | 2,016 |
| T3 | Soldier | 10 | 70 | 280 | 3,360 |
| T4 | Veteran | 16 | 112 | 448 | 5,376 |
| T5 | Elite | 25 | 175 | 700 | 8,400 |
| T6 | Household | 40 | 280 | 1,120 | 13,440 |
| T7 | Lieutenant | 60 | 420 | 1,680 | 20,160 |
| T8 | Captain | 85 | 595 | 2,380 | 28,560 |
| T9 | Commander | 120 | 840 | 3,360 | 40,320 |

### Why These Numbers?

**T1 (3/day):** Still poor, but can afford a tavern drink. After a month (84 denars), could buy a cheap dagger.

**T2 (6/day):** After a month (168 denars), can afford a basic sword. Starting to feel like getting somewhere.

**T3 (10/day):** A month gets you 280 denars. Can buy a decent sword in 1-2 months. Proper soldier money.

**T4 (16/day):** Monthly 448 denars. Good sword in 1-2 months, basic armor in 1-2 months. Professional soldier.

**T5 (25/day):** Monthly 700 denars. Can afford quality gear. Starting to build real wealth.

**T6 (40/day):** Monthly 1,120 denars. Household guard gets treated well. Good armor in 2-3 months.

**T7+ (60-120/day):** Officer pay. Can afford to maintain a retinue and buy quality gear.

---

## Pay Modifiers

### Duty Modifiers

Certain duties adjust base pay:

| Duty | Modifier | Reason |
|------|----------|--------|
| Runner | 0.9x | Grunt work, easily replaced |
| Lookout | 1.0x | Standard |
| Quartermaster | 1.3x | Responsibility, trust |
| Field Medic | 1.25x | Skilled, essential |
| Armorer | 1.2x | Skilled trade |
| Scout | 1.15x | Dangerous, independent |
| Messenger | 1.1x | Mobile, trusted |
| Engineer | 1.3x | Specialized knowledge |
| Boatswain | 1.4x | Naval specialist |
| Navigator | 1.5x | Rare skill |

**Implementation:** Read from `DutyDefinition.wage_multiplier` in duty config

**Example:** T3 Quartermaster = 4 × 1.3 = 5.2/day (rounded to 5)

### Culture Modifiers

Some cultures pay better or worse:

| Culture | Modifier | Reason |
|---------|----------|--------|
| Empire | 1.1x | Organized, wealthy, professional army |
| Aserai | 1.1x | Trade wealth, mercantile culture |
| Vlandia | 1.0x | Standard feudal |
| Sturgia | 0.9x | Harsh land, less coin-based |
| Battania | 0.85x | Tribal, gift economy, less coin |
| Khuzait | 0.8x | Loot-based economy, pay in plunder |

**Implementation:** Simple dictionary lookup by `enlistment.EnlistedLord.Culture.StringId`

**Example:** T3 Empire soldier = 4 × 1.1 = 4.4/day (rounded to 4)

### Lord Wealth Modifier

Lord's financial situation affects pay reliability and amount:

| Lord Treasury | Modifier | Pay Reliability |
|---------------|----------|-----------------|
| Wealthy (>50k) | 1.1x | Always on time |
| Comfortable (20-50k) | 1.0x | Usually on time |
| Struggling (5-20k) | 0.9x | Sometimes delayed |
| Poor (<5k) | 0.75x | Often delayed, partial pay |
| Broke (<1k) | 0.5x | Rarely paid, mutiny risk |

**Implementation:**
```csharp
float GetLordWealthModifier(Hero lord)
{
    int gold = lord.Gold;
    if (gold > 50000) return 1.1f;
    if (gold > 20000) return 1.0f;
    if (gold > 5000) return 0.9f;
    if (gold > 1000) return 0.75f;
    return 0.5f;
}
```

### Wartime Modifier

| Situation | Modifier | Notes |
|-----------|----------|-------|
| Peacetime | 1.0x | Standard |
| Active War | 1.15x | Hazard pay |
| Siege (defending) | 1.25x | Extreme hazard |
| Siege (attacking) | 1.2x | Hazard pay |
| Retreat/Rout | 0.5x | Lucky to be paid at all |

**Implementation:** Check `FactionManager.IsAtWarAgainstFaction()` and `Settlement.IsUnderSiege`

---

## Payment Schedule

### Pay Muster (Weekly)

Pay is distributed every 7 days at **Pay Muster**:

- **When:** Every 7 days since enlistment
- **Where:** Town or castle (settlement required)
- **Who:** Paymaster or Quartermaster NPC
- **How:** Automatic if in settlement, delayed if on campaign

### Pay Delay Conditions

| Condition | Effect |
|-----------|--------|
| Not in settlement | Pay held until next settlement visit |
| Lord poor | Pay delayed 1-7 days |
| On campaign (active combat) | Pay held until campaign ends |
| Army supply crisis | Pay delayed, possible partial pay |
| Siege ongoing | Pay delayed until siege ends |

### Pay Status Tracker

```
pay_status:
  last_pay_date: CampaignTime
  days_since_pay: int
  owed_amount: int (accumulated unpaid wages)
  pay_tension: int (0-100, affects morale/events)
```

### Pay Tension Effects

| Days Overdue | Pay Tension | Effect |
|--------------|-------------|--------|
| 0-7 | 0-10 | Normal |
| 8-14 | 20-40 | Grumbling, minor morale penalty |
| 15-21 | 50-70 | Complaints, discipline issues |
| 22-28 | 80-90 | Near mutiny, desertion risk |
| 29+ | 100 | Mutiny events, mass desertion |

### Backpay Resolution

When pay is delayed, the owed amount accumulates. When pay finally comes through:

**Normal Resolution (Lord pays up):**
- Player receives current week's pay + all owed backpay
- PayTension reduced by 30
- Event: `pay_muster_backpay`

**Partial Resolution (Lord still struggling):**
- Player receives 50% of owed amount
- Remaining 50% still owed
- PayTension reduced by 10
- Event: `pay_muster_partial_backpay`

**Written Off (Lord broke, changes lord, etc.):**
- Owed amount written off (player loses it)
- PayTension reset to 0
- -10 lord_relation
- Event: `pay_muster_written_off`

#### Backpay Events

**`pay_muster_backpay`**
**Trigger:** OwedAmount > 0 AND lord can pay
**Setup:**
> The paymaster's table is heavy with coin today. {PAYMASTER_NAME} counts out your wages — and keeps counting.
>
> "Back pay," they say. "What you were owed. {OWED_AMOUNT} denars."
>
> About bloody time.

**Outcome:** Receive weekly pay + OwedAmount, -30 PayTension, +5 morale

---

**`pay_muster_partial_backpay`**
**Trigger:** OwedAmount > 0 AND lord partially pays
**Setup:**
> {PAYMASTER_NAME} counts out coin, but stops halfway. "Half now. Rest when we can."
>
> Better than nothing. But only just.

**Outcome:** Receive weekly pay + 50% OwedAmount, remaining 50% still owed, -10 PayTension

---

**`pay_muster_written_off`**
**Trigger:** OwedAmount > 0 AND lord cannot/will not pay (lord broke, player transferred, etc.)
**Setup:**
> The paymaster won't meet your eyes. "The back pay... it's not coming. Lord's orders. Fresh start."
>
> {OWED_AMOUNT} denars. Gone. Just like that.

**Options:**
| Option | Effect |
|--------|--------|
| Swallow it | OwedAmount = 0, PayTension = 0, -5 morale |
| Complain loudly | OwedAmount = 0, PayTension = 0, -10 lord_relation, +5 lance_rep |
| "This isn't over" | OwedAmount = 0, PayTension = 0, +1 Heat, sets flag for future event |

---

### Backpay Accumulation Example

Week 1: Pay due, lord broke → +70 denars owed (T3)
Week 2: Pay due, still broke → +70 denars owed (total: 140)
Week 3: Pay due, lord has money → Receive 70 (current) + 140 (backpay) = **210 denars**

---

## Deductions

### Mandatory Deductions

| Deduction | Amount | Frequency | Notes |
|-----------|--------|-----------|-------|
| Rations | 0 | — | Provided by army (already deducted from lord) |
| Equipment maintenance | 0 | — | Part of army logistics |
| Lance fund | 5% | Per pay | Communal fund for wounded, burials |

### Optional/Situational Deductions

| Deduction | Amount | Trigger | Notes |
|-----------|--------|---------|-------|
| Lost/damaged equipment | 10-100 | Event | Replace army-issued gear |
| Discipline fine | 5-50 | Discipline event | Punishment for infractions |
| Debt repayment | Variable | Debt event | Borrowed from lance mate |
| Gambling loss | Variable | Event | Lost at dice/cards |
| Medical treatment | 10-30 | Injury event | Surgeon fees for serious wounds |

### The Lance Fund

Every soldier contributes 5% of pay to the lance fund:

- Pays for burials of fallen lance mates
- Supports wounded soldiers during recovery
- Covers emergency expenses
- Lance leader manages the fund
- Can become a source of drama (embezzlement events)

---

## Bonuses & Windfalls

### Battle Bonuses

| Achievement | Bonus | Notes |
|-------------|-------|-------|
| Battle survived | +1-5 | Scales with battle size |
| Victory bonus | +5-20 | Win share, scales with battle |
| Enemy killed (verified) | +2-5 per kill | Must be witnessed/confirmed |
| Officer killed | +10-25 | Bonus for killing enemy officers |
| Capture bonus | +5-15 | Per prisoner taken |
| Heroic action | +20-50 | Recognized bravery (event) |

### Loot Share

After battles, loot is distributed based on tier. Lower ranks get scraps, higher ranks get real shares.

#### Enlisted Loot (T1-T6)

| Tier | Gold Share | Item Chance | Notes |
|------|------------|-------------|-------|
| T1 | 5% | 20% | Scraps, but decent chance at items |
| T2 | 10% | 30% | Better cut |
| T3 | 10% | 30% | Soldier's share |
| T4 | 15% | 40% | Veteran's share |
| T5 | 15% | 50% | Elite gets priority |
| T6 | 15% | 60% | Household guard picks first |

**Gold Share:** Percentage of total battle loot value (after lord's take)
**Item Chance:** Chance to receive a random item from battle loot to sell at QM

**Example:** Battle generates 1000 denar loot, lord takes 50% (500), leaving 500 for troops.
- T1 soldier: 5% of 500 = 25 denars + 20% chance for item
- T3 soldier: 10% of 500 = 50 denars + 30% chance for item
- T6 soldier: 15% of 500 = 75 denars + 60% chance for item

#### Commander Loot (T7-T9)

Commanders get a real percentage of total battle loot (before lord's take):

| Tier | Loot Share | Notes |
|------|------------|-------|
| T7 | 10% | Junior commander's cut |
| T8 | 15% | Captain's share |
| T9 | 20% | Senior commander takes a fifth |

**Example:** Battle generates 1000 denar loot.
- T7 commander: 10% of 1000 = 100 denars
- T8 commander: 15% of 1000 = 150 denars
- T9 commander: 20% of 1000 = 200 denars

Plus commanders can claim equipment for their retinue.

#### Item Loot

When a soldier wins the item chance roll:
- Random item from battle loot pool (weapon, armor, horse, trade good)
- Item quality scales loosely with tier (T1-T2 get junk, T5-T6 get decent stuff)
- Player can equip the item or sell to Quartermaster
- QM buys at 50% market value

**Implementation:**
```csharp
void ProcessBattleLoot(int totalLootValue, int playerTier)
{
    float goldShare = GetGoldSharePercent(playerTier);
    float itemChance = GetItemChancePercent(playerTier);
    
    // Gold share (enlisted takes from post-lord pool, commanders from total)
    int pool = playerTier >= 7 ? totalLootValue : (int)(totalLootValue * 0.5f);
    int goldEarned = (int)(pool * goldShare);
    Hero.MainHero.Gold += goldEarned;
    
    // Item chance
    if (MBRandom.RandomFloat < itemChance)
    {
        var item = GetRandomLootItem(playerTier);
        // Add to player inventory or show loot event
    }
}

float GetGoldSharePercent(int tier) => tier switch
{
    1 => 0.05f,
    2 => 0.10f,
    3 => 0.10f,
    4 => 0.15f,
    5 => 0.15f,
    6 => 0.15f,
    7 => 0.10f,
    8 => 0.15f,
    9 => 0.20f,
    _ => 0.05f
};

float GetItemChancePercent(int tier) => tier switch
{
    1 => 0.20f,
    2 => 0.30f,
    3 => 0.30f,
    4 => 0.40f,
    5 => 0.50f,
    6 => 0.60f,
    _ => 0f // Commanders take equipment directly
};
```

### Special Bonuses

| Bonus | Amount | Trigger |
|-------|--------|---------|
| Promotion bonus | 1 week pay | On promotion |
| Hazard duty completion | +10-30 | Dangerous mission |
| Lord's favor | +20-100 | Personal recognition |
| Campaign completion | +50-200 | End of major campaign |
| Festival/holiday | +5-20 | Cultural celebrations |

### Gambling & Side Income

> **Trigger:** Post-battle celebration event fires after victories. Player can gamble, drink, or rest.

#### Post-Battle Celebration Event

**Event ID:** `gen_victory_celebration`
**Category:** general
**Trigger:** `after_battle` + `battle_won` + `evening`
**Delivery:** automatic (fires evening after a victory)
**Cooldown:** Once per battle

**Setup:**
> The battle's won. Tonight, the camp celebrates.
>
> Someone's found a cask of wine — or liberated it from the enemy baggage. Dice are rattling behind the supply wagons. {LANCE_MATE_NAME} is already three cups deep and challenging all comers to arm wrestling.
>
> {VETERAN_1_NAME} catches your eye and raises a cup. "You earned it today, {PLAYER_SHORT}."

**Options:**

| Option | Risk | Cost | Reward | Notes |
|--------|------|------|--------|-------|
| Join the dice game (small stakes) | Risky | -20 gold | +20 Roguery XP, +10 lance_rep, ±30 gold | 50% win chance |
| Join the dice game (high stakes) | Risky | -50 gold | +25 Roguery XP, +15 lance_rep, ±80 gold | 40% win chance |
| Drink with the lads | Safe | -5 gold | +15 Charm XP, +10 lance_rep, -2 Fatigue | Social bonding |
| Arm wrestling for coin | Risky | -10 gold | +15 Athletics XP, ±20 gold | Athletics check |
| Buy a round for the lance | Safe | -30 gold | +20 Charm XP, +20 lance_rep | Generous |
| Turn in early | Safe | — | -3 Fatigue | Skip celebration |

**Outcome Text Examples:**

*Dice Win:*
> The dice love you tonight. {SOLDIER_NAME} groans as you rake in the pot. "Bastard's got the devil's luck."

*Dice Loss:*
> The dice betray you. Your coins vanish into {SOLDIER_NAME}'s grinning hands. "Better luck next battle, eh?"

*Drinking:*
> You drink. You laugh. You tell lies about the fight that get bigger with each cup. By the end of the night, you'd swear {LANCE_MATE_NAME} killed a hundred men single-handed.
>
> This is what you fight for. These people. This feeling.

*Arm Wrestling Win:*
> You slam {SOLDIER_NAME}'s hand down. The crowd roars. Coins change hands — most of them toward you.

*Arm Wrestling Loss:*
> {SOLDIER_NAME}'s arm is like iron. Yours hits the table hard. Laughter all around. "Not bad," they say. "But not good enough."

*Buy a Round:*
> "This one's on me!" The cheer that goes up is worth more than the coin. {LANCE_LEADER_SHORT} raises their cup in your direction. You've bought goodwill tonight.

*Turn in Early:*
> You leave them to it. The sounds of celebration fade as you find your bedroll. Tomorrow there'll be work. Tonight, you rest.

#### Gambling Mechanics

**Dice Game Resolution:**
```
Small Stakes (-20 gold entry):
- Win (50%): +30 gold (net +10)
- Lose (50%): -30 gold (net -50 total with entry)

High Stakes (-50 gold entry):
- Win (40%): +80 gold (net +30)
- Lose (60%): -80 gold (net -130 total with entry)
```

**Arm Wrestling Resolution:**
```
Base win chance: 50%
+ Athletics > 100: +10%
+ Athletics > 150: +20%
+ Fatigued: -15%

Win: +20 gold
Lose: -20 gold
```

#### Side Income Activities

Beyond post-battle gambling, soldiers can earn extra through:

| Activity | Potential Gain | Potential Loss | Risk | How |
|----------|----------------|----------------|------|-----|
| Dice games | +10-80 | -50-130 | Medium | Post-battle event |
| Arm wrestling | +20 | -20 | Low | Post-battle event |
| Selling scavenged items | +5-50 | 0 | Low | Loot item → QM |
| Black market deals | +20-100 | 0 | High | Corruption events |
| Winning bets | +10-50 | -10-50 | Medium | Various events |

#### Implementation

```csharp
// Post-battle celebration check
void OnBattleEnded(MapEvent mapEvent)
{
    if (!IsEnlisted()) return;
    if (!mapEvent.IsPlayerWon()) return;
    
    // Queue celebration event for evening
    QueueEvent("gen_victory_celebration", 
        deliveryTime: GetNextEvening(),
        priority: EventPriority.Normal);
}

// Gambling resolution
GamblingResult ResolveGamble(GambleType type, int stake)
{
    float winChance = type switch
    {
        GambleType.DiceSmall => 0.50f,
        GambleType.DiceHigh => 0.40f,
        GambleType.ArmWrestle => GetArmWrestleChance(),
        _ => 0.50f
    };
    
    bool won = MBRandom.RandomFloat < winChance;
    int payout = GetPayout(type, won);
    
    return new GamblingResult(won, payout);
}

float GetArmWrestleChance()
{
    float chance = 0.50f;
    int athletics = Hero.MainHero.GetSkillValue(DefaultSkills.Athletics);
    
    if (athletics > 150) chance += 0.20f;
    else if (athletics > 100) chance += 0.10f;
    
    if (IsPlayerFatigued()) chance -= 0.15f;
    
    return chance;
}
```

#### Config Schema

```json
{
  "gambling": {
    "dice_small": {
      "entry_cost": 20,
      "win_chance": 0.50,
      "win_payout": 30,
      "lose_payout": -30,
      "xp_reward": { "roguery": 20 },
      "lance_rep": 10
    },
    "dice_high": {
      "entry_cost": 50,
      "win_chance": 0.40,
      "win_payout": 80,
      "lose_payout": -80,
      "xp_reward": { "roguery": 25 },
      "lance_rep": 15
    },
    "arm_wrestling": {
      "entry_cost": 10,
      "base_win_chance": 0.50,
      "athletics_bonus_100": 0.10,
      "athletics_bonus_150": 0.20,
      "fatigue_penalty": 0.15,
      "win_payout": 20,
      "lose_payout": -20,
      "xp_reward": { "athletics": 15 }
    },
    "buy_round": {
      "cost": 30,
      "xp_reward": { "charm": 20 },
      "lance_rep": 20
    },
    "drink_with_lads": {
      "cost": 5,
      "xp_reward": { "charm": 15 },
      "lance_rep": 10,
      "fatigue_reduction": 2
    }
  }
}
```

---

## Economic Balance

### What Can Soldiers Afford?

#### T1 Levy (3/day, 84/month)

| Item | Cost | Time to Save |
|------|------|--------------|
| Tavern drink | 3 | 1 day |
| Cheap meal | 8 | 3 days |
| Dagger | 50 | 2-3 weeks |
| Cheap sword | 150 | 2 months |

**Reality:** Still poor, but can afford small things. Basic gear takes a while.

#### T2 Recruit (6/day, 168/month)

| Item | Cost | Time to Save |
|------|------|--------------|
| Tavern night | 15 | 2-3 days |
| Basic knife | 30 | 5 days |
| Cheap sword | 150 | 1 month |
| Basic helmet | 200 | 5 weeks |

**Reality:** Can afford basic gear. Tavern is no longer a luxury.

#### T3 Soldier (10/day, 280/month)

| Item | Cost | Time to Save |
|------|------|--------------|
| Tavern night | 15 | 1-2 days |
| Decent sword | 300 | 1 month |
| Basic armor piece | 400 | 6 weeks |
| Good boots | 100 | 10 days |

**Reality:** Proper soldier pay. Can upgrade gear steadily.

#### T4 Veteran (16/day, 448/month)

| Item | Cost | Time to Save |
|------|------|--------------|
| Good sword | 600 | 5-6 weeks |
| Decent armor | 800 | 2 months |
| Riding horse | 300 | 3 weeks |

**Reality:** Professional soldier. Can afford quality gear.

#### T5 Elite (25/day, 700/month)

| Item | Cost | Time to Save |
|------|------|--------------|
| Good armor | 1500 | 2 months |
| Fine sword | 1000 | 6 weeks |
| War horse | 1200 | 7 weeks |

**Reality:** Well-paid veteran. Quality gear is accessible.

#### T6 Household (40/day, 1120/month)

| Item | Cost | Time to Save |
|------|------|--------------|
| Excellent armor | 3000 | 3 months |
| Excellent weapon | 1500 | 5-6 weeks |
| War horse | 1200 | 1 month |

**Reality:** Comfortable. Can afford the best gear. Lord may also provide.

### Savings Projection

Assuming 5% lance fund deduction and occasional expenses (tavern, gambling, minor purchases) eating ~30% of pay:

| Tier | Monthly Take-Home | Yearly Savings (est.) |
|------|-------------------|----------------------|
| T1 | ~55 | ~400 |
| T2 | ~110 | ~800 |
| T3 | ~180 | ~1,500 |
| T4 | ~290 | ~2,500 |
| T5 | ~450 | ~4,000 |
| T6 | ~720 | ~6,500 |

**Time to Tier Progression:**
- T1→T2: 21 days = ~120 denars saved
- T2→T3: 42 days = ~300 denars saved
- T3→T4: 63 days = ~750 denars saved
- T4→T5: 63 days = ~1,200 denars saved
- T5→T6: 63 days = ~1,900 denars saved

By T6, a player might have accumulated ~4,000-5,000 denars through savings and bonuses. Enough for good gear.

---

## Pay Events

### Pay Muster Events

#### `pay_muster_normal`
**Trigger:** Pay day, no issues
**Setup:** Line forms at the paymaster's table. When your name is called, you step forward and receive your wages.
**Outcome:** Receive pay, +5 morale

#### `pay_muster_bonus`
**Trigger:** After significant victory
**Setup:** {PAYMASTER_NAME} has a rare smile. "Battle bonus," they say, counting out extra coin.
**Outcome:** Receive pay + battle bonus, +10 morale

#### `pay_muster_delayed`
**Trigger:** Lord poor or supply issues
**Setup:** The paymaster's table is empty. "Coin's coming," someone mutters. "They always say that."
**Outcome:** No pay, +10 pay_tension, grumbling

#### `pay_muster_partial`
**Trigger:** Lord very poor
**Setup:** {PAYMASTER_NAME} counts out half your usual pay. "Times are hard. You'll get the rest when—" They shrug.
**Outcome:** Receive 50% pay, +20 pay_tension, -5 morale

### Pay Tension Events

#### `pay_tension_grumbling` (tension 20-40)
**Trigger:** Pay delayed 8-14 days
**Setup:** Complaints about pay have become the main topic around the fire.

**Options:**
| Option | Effect |
|--------|--------|
| Join the grumbling | +5 lance_rep, -3 leader_rel |
| Stay quiet | No effect |
| Defend the lord | -5 lance_rep, +5 leader_rel |

#### `pay_tension_bribe_opportunity` (tension 40-60)
**Trigger:** Pay delayed 14+ days, entered town
**Setup:** A clerk from the paymaster's office approaches. "Your name could move up the priority list. When coin does arrive. For a small consideration."

**Options:**
| Option | Effect |
|--------|--------|
| Pay the bribe (20 denars) | -20 gold, priority pay next muster |
| Report the corruption | +5 lord_rel, clerk becomes enemy |
| Refuse quietly | No effect |

#### `pay_tension_confrontation` (tension 60-80)
**Trigger:** Pay delayed 21+ days
**Setup:** {VETERAN_1_NAME} is organizing soldiers to confront the paymaster. "We've fought. We've bled. We deserve what's owed."

**Options:**
| Option | Effect |
|--------|--------|
| Join the delegation | +10 lance_rep, -5 lord_rel, possible pay resolution |
| Talk them down | -5 lance_rep, +5 leader_rel |
| Stay out of it | No effect |

#### `pay_tension_mutiny_brewing` (tension 80+)
**Trigger:** Pay delayed 28+ days
**Setup:** The talk has turned from complaints to action. Desertion. Maybe worse. You can feel the tension in every conversation.

**Options:**
| Option | Effect |
|--------|--------|
| Side with the soldiers | Join potential mutiny path |
| Inform the officers | -20 lance_rep, +15 lord_rel, mutiny prevented |
| Try to mediate | Risky: success calms things, failure accelerates |

---

## Implementation

### Pay Calculation

```csharp
public int CalculateDailyPay(EnlistmentBehavior enlistment)
{
    int basePay = GetBasePay(enlistment.EnlistmentTier);
    
    // Apply duty modifier
    float dutyMod = GetDutyModifier(enlistment.ActiveDuty);
    
    // Apply culture modifier
    float cultureMod = GetCultureModifier(enlistment.EnlistedLord.Culture.StringId);
    
    // Apply lord wealth modifier
    float wealthMod = GetLordWealthModifier(enlistment.EnlistedLord);
    
    // Apply wartime modifier
    float warMod = GetWartimeModifier(enlistment);
    
    float totalPay = basePay * dutyMod * cultureMod * wealthMod * warMod;
    
    return Math.Max(1, (int)Math.Round(totalPay));
}

private int GetBasePay(int tier)
{
    return tier switch
    {
        1 => 3,
        2 => 6,
        3 => 10,
        4 => 16,
        5 => 25,
        6 => 40,
        7 => 60,
        8 => 85,
        9 => 120,
        _ => 3
    };
}
```

### Pay State

```csharp
public class PayState
{
    public CampaignTime LastPayDate { get; set; }
    public int DaysSincePay => (int)(CampaignTime.Now - LastPayDate).ToDays;
    public int OwedAmount { get; set; }
    public int PayTension { get; set; }
    
    // Normal pay with backpay
    public void ProcessPayMuster(int weeklyPay, bool includeBackpay = true)
    {
        int totalPay = weeklyPay;
        
        if (includeBackpay && OwedAmount > 0)
        {
            totalPay += OwedAmount;
            OwedAmount = 0;
            PayTension = Math.Max(0, PayTension - 30);
            // Fire backpay event
        }
        else
        {
            PayTension = Math.Max(0, PayTension - 10);
        }
        
        Hero.MainHero.Gold += totalPay;
        LastPayDate = CampaignTime.Now;
    }
    
    // Partial backpay (lord struggling)
    public void ProcessPartialBackpay(int weeklyPay)
    {
        int backpayPortion = OwedAmount / 2;
        int totalPay = weeklyPay + backpayPortion;
        
        Hero.MainHero.Gold += totalPay;
        OwedAmount -= backpayPortion; // Half still owed
        PayTension = Math.Max(0, PayTension - 10);
        LastPayDate = CampaignTime.Now;
    }
    
    // Pay delayed - accumulate debt
    public void ProcessPayDelay(int weeklyPay)
    {
        OwedAmount += weeklyPay;
        PayTension = Math.Min(100, PayTension + 15);
        // Don't update LastPayDate - pay didn't happen
    }
    
    // Write off debt (transfer, lord broke, etc.)
    public void WriteOffDebt()
    {
        OwedAmount = 0;
        PayTension = 0;
    }
}
```

### Config Schema

```json
{
  "pay_system": {
    "base_pay": {
      "t1": 3, "t2": 6, "t3": 10, "t4": 16,
      "t5": 25, "t6": 40, "t7": 60, "t8": 85, "t9": 120
    },
    "duty_modifiers": {
      "runner": 0.9,
      "lookout": 1.0,
      "quartermaster": 1.3,
      "field_medic": 1.25,
      "armorer": 1.2,
      "scout": 1.15,
      "messenger": 1.1,
      "engineer": 1.3,
      "boatswain": 1.4,
      "navigator": 1.5
    },
    "culture_modifiers": {
      "empire": 1.1,
      "aserai": 1.1,
      "vlandia": 1.0,
      "sturgia": 0.9,
      "battania": 0.85,
      "khuzait": 0.8
    },
    "lord_wealth_thresholds": {
      "wealthy": 50000,
      "comfortable": 20000,
      "struggling": 5000,
      "poor": 1000
    },
    "pay_schedule": {
      "frequency_days": 7,
      "lance_fund_percent": 5
    },
    "tension_thresholds": {
      "grumbling": 20,
      "complaints": 50,
      "near_mutiny": 80,
      "mutiny": 100
    }
  }
}
```

### Triggers & Conditions

New triggers for events:

| Trigger | Condition |
|---------|-----------|
| `pay_day` | Days since pay % 7 == 0 |
| `pay_current` | PayTension < 20 |
| `pay_delayed` | PayTension 20-50 |
| `pay_overdue` | PayTension 50-80 |
| `pay_crisis` | PayTension 80+ |
| `pay_owed` | OwedAmount > 0 |

---

## UI Integration

### Enlisted Status Display

```
Pay: 4/day (28/week) | Next: 3 days | Status: On time
```

Or if delayed:
```
Pay: 4/day (28/week) | OVERDUE: 12 days | Owed: 48 denars
```

### Pay Breakdown Tooltip

```
Base Pay (T3): 4/day
Duty (Quartermaster): +30%
Culture (Empire): +10%
───────────────────
Daily Pay: 5 denars
Weekly Pay: 35 denars

Lance Fund: -5% (2 denars)
Net Weekly: 33 denars
```

---

## Summary Tables

### Quick Reference: Daily Pay

| Tier | Base | +QM Duty | +Empire | Full Calc Example |
|------|------|----------|---------|-------------------|
| T1 | 3 | 3.9 | 3.3 | Empire QM T1: 3×1.3×1.1 = 4.3 → 4 |
| T2 | 6 | 7.8 | 6.6 | Empire QM T2: 6×1.3×1.1 = 8.6 → 9 |
| T3 | 10 | 13 | 11 | Empire QM T3: 10×1.3×1.1 = 14.3 → 14 |
| T4 | 16 | 20.8 | 17.6 | Empire QM T4: 16×1.3×1.1 = 22.9 → 23 |
| T5 | 25 | 32.5 | 27.5 | Empire QM T5: 25×1.3×1.1 = 35.8 → 36 |
| T6 | 40 | 52 | 44 | Empire QM T6: 40×1.3×1.1 = 57.2 → 57 |

### Quick Reference: Time to Buy Gear

| Item | Cost | T1 Time | T3 Time | T5 Time |
|------|------|---------|---------|---------|
| Cheap sword | 150 | 7 weeks | 2 weeks | 1 week |
| Decent sword | 400 | 4+ months | 6 weeks | 2 weeks |
| Good sword | 800 | 9+ months | 3 months | 5 weeks |
| Basic armor | 300 | 3+ months | 1 month | 2 weeks |
| Good armor | 1500 | Never | 5 months | 2 months |

---

*Document Version: 1.0*
*For use with: Lance Life Enlistment System*
*Reference: promotion_system.md, phase_7_quartermaster_formation_promotion.md*

---

## Appendix A: Commander Track Pay (T7-T9)

Commanders have additional income and expenses.

### Commander Expenses

| Expense | Cost | Frequency | Notes |
|---------|------|-----------|-------|
| Retinue wages | ~5-15/soldier/day | Daily | Based on soldier tier |
| Recruitment (purchased) | 100 + 50×tier | One-time | Buy from settlement |
| Recruitment (battlefield) | 0 | One-time | Free but may be wounded |
| Recruitment (assigned) | 0 | One-time | Lord provides troops |
| Equipment replacement | Variable | On loss | If lord doesn't cover |

### Retinue Wage Formula

Commanders must pay their own soldiers:

```
Soldier Daily Wage = Native Bannerlord troop wage
```

This uses native Bannerlord wage calculations:
- T1 soldier: ~1-2/day
- T3 soldier: ~4-6/day
- T5 soldier: ~12-18/day
- T6 elite: ~20-30/day

### Commander Income vs Expenses

| Tier | Commander Pay | Max Retinue | Est. Retinue Cost | Net |
|------|---------------|-------------|-------------------|-----|
| T7 | 35/day | 15 soldiers | ~60-100/day | Negative |
| T8 | 55/day | 25 soldiers | ~100-180/day | Negative |
| T9 | 80/day | 35 soldiers | ~150-250/day | Negative |

**Design Intent:** Commander pay alone doesn't cover retinue costs. Commanders rely on:
- Battle loot (officer's share)
- Lord's supplemental funding
- Campaign bonuses
- Personal wealth accumulated during enlisted career

This creates pressure to:
- Win battles (loot)
- Keep soldiers alive (avoid replacement costs)
- Maintain lord's favor (supplemental funding)

---

## Appendix B: Quartermaster Gear Pricing

Gear bought from the Quartermaster uses market pricing with QM discount.

### QM Price Formula

```
QM Price = Market Price × 0.8 (20% military discount)
```

### Typical Gear Price Ranges

| Category | Tier | Market Price | QM Price | T3 Save Time |
|----------|------|--------------|----------|--------------|
| **Weapons** |
| Basic dagger | T1 | 30-50 | 24-40 | 1-2 weeks |
| Basic sword | T1-T2 | 100-200 | 80-160 | 3-5 weeks |
| Good sword | T2-T3 | 300-500 | 240-400 | 2-3 months |
| Fine sword | T3-T4 | 600-1000 | 480-800 | 4-6 months |
| Excellent sword | T5-T6 | 1200-2000 | 960-1600 | 6+ months |
| **Armor (Body)** |
| Padded cloth | T1 | 50-100 | 40-80 | 1-3 weeks |
| Leather armor | T2 | 200-400 | 160-320 | 1-2 months |
| Mail shirt | T3 | 500-800 | 400-640 | 3-5 months |
| Scale armor | T4 | 1000-1500 | 800-1200 | 6-9 months |
| Lamellar/Plate | T5-T6 | 2000-5000 | 1600-4000 | 1+ year |
| **Helmets** |
| Leather cap | T1 | 30-60 | 24-48 | 1-2 weeks |
| Iron cap | T2 | 100-200 | 80-160 | 3-5 weeks |
| Nasal helm | T3 | 300-500 | 240-400 | 2-3 months |
| Full helm | T4-T5 | 600-1200 | 480-960 | 4-7 months |
| **Horses** |
| Riding horse | T2 | 200-400 | 160-320 | 1-2 months |
| Light war horse | T3-T4 | 500-800 | 400-640 | 3-5 months |
| War horse | T5-T6 | 1000-2000 | 800-1600 | 6-10 months |

### Availability by Tier

From `phase_7_quartermaster_formation_promotion.md`:

| Player Tier | Can Buy Gear From |
|-------------|-------------------|
| T1 | T1 troops only |
| T2 | T1-T2 troops |
| T3 | T1-T3 troops |
| T4 | T1-T4 troops |
| T5 | T1-T5 troops |
| T6 | T1-T6 troops |

---

## Appendix C: Event Cost Alignment

Event gold costs/rewards should align with pay:

### Cost Guidelines for Event Writers

| Cost Level | Gold Amount | Feels Like | Example Events |
|------------|-------------|------------|----------------|
| Trivial | 1-5 | A drink | Tip someone, small bribe |
| Minor | 10-20 | A day's pay | Help lance mate, small gambling |
| Moderate | 25-50 | A week's pay | Cover shortage, moderate bribe, decent gambling |
| Significant | 75-100 | Two weeks pay | Major bribe, high stakes gambling |
| Major | 150-200 | A month's pay | Serious debt, major purchase |
| Severe | 300+ | Multiple months | Life-changing expense |

### Reward Guidelines

| Reward Level | Gold Amount | Source | Example |
|--------------|-------------|--------|---------|
| Token | 1-5 | Tip, found coin | Minor discovery |
| Minor | 10-25 | Small bribe, corruption | Looking the other way |
| Moderate | 30-50 | Corruption, side deal | Black market cut |
| Significant | 75-100 | Major corruption | Serious criminal activity |
| Windfall | 150-300 | Heroic action reward | Lord's recognition |
| Jackpot | 500+ | Rare loot, treasure | Once-in-career find |

### Cross-Reference: Existing Events

From `lance_life_events_content_library.md`:

| Event | Cost | Days of T3 Pay |
|-------|------|----------------|
| Cover shortage | -25 | 6 days |
| Black market deal | -50 | 12 days |
| Trade for food | -15 | 4 days |
| Pay debt | -20 | 5 days |
| Small gambling | -10 | 2.5 days |
| High gambling | -50 | 12 days |

These align well — moderate costs equal about 1-2 weeks pay for a T3 soldier.

---

## Appendix D: Loot Distribution (Detailed)

### Battle Loot Flow

```
TOTAL BATTLE LOOT (e.g., 1000 denars)
│
├── Lord's Take: 50% (500 denars)
│
└── Troop Pool: 50% (500 denars)
    │
    ├── Enlisted (T1-T6): Get % of troop pool + item chance
    │   ├── T1: 5% gold (25 denars) + 20% item chance
    │   ├── T2: 10% gold (50 denars) + 30% item chance
    │   ├── T3: 10% gold (50 denars) + 30% item chance
    │   ├── T4: 15% gold (75 denars) + 40% item chance
    │   ├── T5: 15% gold (75 denars) + 50% item chance
    │   └── T6: 15% gold (75 denars) + 60% item chance
    │
    └── Commanders (T7-T9): Get % of TOTAL loot (before lord's take)
        ├── T7: 10% (100 denars)
        ├── T8: 15% (150 denars)
        └── T9: 20% (200 denars)
```

### Example: 1000 Denar Battle

| Tier | Gold Share | Gold Earned | Item Chance |
|------|------------|-------------|-------------|
| T1 | 5% of 500 | 25 denars | 20% |
| T2 | 10% of 500 | 50 denars | 30% |
| T3 | 10% of 500 | 50 denars | 30% |
| T4 | 15% of 500 | 75 denars | 40% |
| T5 | 15% of 500 | 75 denars | 50% |
| T6 | 15% of 500 | 75 denars | 60% |
| T7 | 10% of 1000 | 100 denars | Takes equipment |
| T8 | 15% of 1000 | 150 denars | Takes equipment |
| T9 | 20% of 1000 | 200 denars | Takes equipment |

### Item Loot Details

When the item chance roll succeeds:

1. **Random item selected** from battle loot pool
2. **Quality scales with tier:**
   - T1-T2: Low-tier items (daggers, cloth, trade goods)
   - T3-T4: Mid-tier items (swords, leather armor, tools)
   - T5-T6: Higher-tier items (quality weapons, mail, horses)
3. **Player choice:** Equip or sell to Quartermaster
4. **QM purchase price:** 50% of market value

### Loot Events

After significant battles, a loot event can fire:

**`loot_item_found`**
> After the battle, you're sorting through the dead when something catches your eye — {ITEM_NAME}. Not bad. Yours now, if you're quick about it.

**Options:**
| Option | Effect |
|--------|--------|
| Keep it | Add item to inventory |
| Sell to QM later | Mark for sale at next QM visit |
| "I didn't see anything" | Skip loot, +5 lance_rep (seen as humble) |

### Commander Loot (T7-T9)

Commanders don't roll for items — they can directly claim equipment for themselves or their retinue:

- After battle, commanders choose from available loot
- Can equip soldiers with captured gear
- Can sell excess at full market rates (officer privilege)
- Must balance personal gain vs retinue needs (events)

---

## Appendix E: Placeholders for Pay Events

| Placeholder | Description | Example |
|-------------|-------------|---------|
| `{DAILY_PAY}` | Player's daily pay | "4" |
| `{WEEKLY_PAY}` | Player's weekly pay | "28" |
| `{PAY_STATUS}` | Current pay status | "On time" / "Delayed" |
| `{DAYS_SINCE_PAY}` | Days since last pay | "12" |
| `{OWED_AMOUNT}` | Total owed if delayed | "48" |
| `{NEXT_PAY_DAYS}` | Days until next pay | "3" |
| `{LANCE_FUND_AMOUNT}` | Lance fund balance | "150" |
| `{PAYMASTER_NAME}` | Paymaster NPC name | "Clerk Aldric" |

---

*End of Document*
