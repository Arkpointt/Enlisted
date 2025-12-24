# Pay System

**Summary:** The pay system manages daily wages, muster payouts, battle loot sharing, and pay tension for enlisted soldiers. Pay accumulates daily based on a dynamic formula and is distributed every 12 days at muster, with progression scaling and various bonus mechanisms.

**Status:** ✅ Current  
**Last Updated:** 2025-12-23  
**Related Docs:** [Enlistment](enlistment.md), [Core Gameplay](core-gameplay.md)

---

## Index

- [Overview](#overview)
- [Daily Wage Calculation](#daily-wage-calculation)
- [Pay Muster](#pay-muster)
- [Pay Tension](#pay-tension)
- [Free Desertion](#free-desertion)
- [Battle Loot Share](#battle-loot-share)
- [Tier-Gated Loot](#tier-gated-loot)
- [News Integration](#news-integration)

---

## Overview

| Component | Purpose |
|-----------|---------|
| **Daily Wage** | Dynamic formula based on tier, level, XP, and modifiers. |
| **Pay Muster** | Periodic payout events (every 12 days ±1 day jitter). |
| **Pay Tension** | Escalation counter (0-100) when pay is late or disrupted. |
| **Battle Loot Share** | Gold bonus from victories, with tier-based percentages. |
| **Tier-Gated Loot** | T4+ soldiers access native loot screens. |

---

## Daily Wage Calculation

Daily wages are calculated using a formula from `enlisted_config.json` that scales with your progression.

### Wage Formula

```
DailyWage = (BaseWage + Level×LevelMult + Tier×TierMult + XP/XPDivisor) × Modifiers
```

**Config Values** (from `enlisted_config.json`):
- `base_wage`: 10
- `level_multiplier`: 1
- `tier_multiplier`: 5
- `xp_divisor`: 200
- Minimum: 24 denars/day
- Maximum: 150 denars/day

### Example Calculations

| Tier | Level | XP | Base Formula | Daily Wage |
|------|-------|----|--------------|-----------:|
| T1 | 5 | 0 | 10 + 5×1 + 1×5 + 0 | 20 denars |
| T3 | 10 | 600 | 10 + 10×1 + 3×5 + 3 | 38 denars |
| T5 | 15 | 2000 | 10 + 15×1 + 5×5 + 10 | 60 denars |
| T7 | 20 | 5000 | 10 + 20×1 + 7×5 + 25 | 90 denars |
| T9 | 25 | 10000 | 10 + 25×1 + 9×5 + 50 | 130 denars |

### Active Modifiers

- **Army Bonus**: ×1.2 when your lord's party is in an active army (config: `army_bonus_multiplier`)
- **Probation Penalty**: ×0.5 when on probation after desertion or washout (config: `probation_wage_multiplier`)

---

## Pay Muster

Every 12 days (±1 day jitter from config), a pay muster event triggers, delivered as a map incident popup.

### Payment Outcomes

The lord's financial status determines what happens at muster:

**Full Payment**
- Lord pays all pending wages plus any backpay
- Pay Tension reduced by 30
- Clears consecutive delay counter
- Player receives full amount immediately

**Partial Payment**
- Lord pays current period + 50% of backpay
- Pay Tension reduced by 10
- Remaining backpay carries forward
- Message indicates still-owed amount

**Payment Delayed**
- Lord cannot afford wages
- Backpay accumulates
- Pay Tension increases (10 base + 5 per week overdue)
- Consecutive delays counter increments

**Alternative Options** (available in pay muster popup):
- **Promissory Note (IOU)**: Accept delay, retry in 3 days
- **Side Deal**: Take 40% payout immediately, costs 6 fatigue
- **Corruption**: Skill check to get 95% payout through illicit channels

### Lord Wealth Checks

Lord affordability is checked with buffers to prevent draining them:
- Full payment: `LordGold >= Amount + 500`
- Partial payment: `LordGold >= (Amount/2) + 200`

Wealth thresholds for reference:
- Wealthy: >50,000 denars
- Comfortable: >20,000 denars
- Struggling: >5,000 denars
- Poor: >1,000 denars
- Broke: ≤1,000 denars

---

## Pay Tension (0-100)

Tracks soldier dissatisfaction when pay is delayed or irregular.

### Tension Increases

- **Pay Delayed**: 10 base + 5 per week overdue
- **Consecutive Delays**: Compounds tension buildup
- **Max Value**: 100

### Tension Decreases

- **Full Payment**: -30 tension
- **Partial Payment**: -10 tension
- **Player Assists Lord**: Custom reduction from events

### Effects by Level

| Tension | Morale Penalty | Discipline Risk | NPC Desertion Risk |
|---------|----------------|-----------------|---------------------|
| 0-19 | None | None | None |
| 20-39 | -3 | None | None |
| 40-59 | -6 | +5% chance | Low |
| 60-79 | -10 | +10% chance | Medium |
| 80+ | -15 | +20% chance | High |

When tension reaches 80+, NPC soldiers may desert from the lord's party. The player can check tension in the Camp Hub menu.

---

## Free Desertion

When **Pay Tension ≥ 60**, the player can leave service without standard desertion penalties:

**Standard Desertion Consequences**
- Major lord relation penalty
- Crime rating increase
- Gear may be stripped
- Pension forfeited

**Free Desertion (High Pay Tension)**
- Minimal lord relation hit
- No crime rating penalty
- Keep all current equipment
- Pension still forfeited

The lord understands if soldiers leave when wages aren't being paid.

---

## Battle Loot Share

Enlisted soldiers receive automatic gold payouts after victories based on tier. This compensates for restricted loot access at lower ranks.

### Loot Share Formula

```
EstimatedLoot = EnemyCasualties × 20 denars
LootPool = EstimatedLoot × 0.5 (T1-T6) or EstimatedLoot (T7-T9)
GoldEarned = LootPool × SharePercent + VictoryBonus
```

### Share Percentages by Tier

| Tier | Share % | Pool Type | Example (100 casualties) |
|------|---------|-----------|--------------------------|
| T1 | 5% | Troop (50%) | 50 denars |
| T2 | 10% | Troop (50%) | 100 denars |
| T3 | 10% | Troop (50%) | 100 denars |
| T4 | 15% | Troop (50%) | 150 denars |
| T5 | 15% | Troop (50%) | 150 denars |
| T6 | 15% | Troop (50%) | 150 denars |
| T7 | 10% | Total (100%) | 200 denars |
| T8 | 15% | Total (100%) | 300 denars |
| T9 | 20% | Total (100%) | 400 denars |

**Victory Bonus**: 5-20 denars based on battle size (5 + casualties/10, max 20)

**Commander Privilege (T7+)**: Commanders take their share from total loot before the lord's split, representing their command authority.

---

## Tier-Gated Loot

| Tier | Loot Access | Compensation |
|------|-------------|--------------|
| T1-T3 | **Blocked** | Automatic gold share only |
| T4-T6 | **Allowed** | Native loot screens + gold share |
| T7-T9 | **Allowed** | Native loot screens + commander share |

T4+ soldiers can use the standard Bannerlord post-battle loot interface and still receive their automatic gold share on top of any loot they take.

---

## News Integration

Pay muster outcomes are recorded to the Personal Feed for historical review. Players can track their payment history and identify patterns like repeated delays.

### Personal Feed Entries

Each muster generates an outcome-specific headline:

| Outcome | Personal Feed Entry |
|---------|---------------------|
| **Full Payment** | "The paymaster counts out your coin. {AMOUNT} denars received in full." |
| **Partial Payment** | "The paymaster's purse is light. {AMOUNT} denars paid, {OWED} still owed." |
| **Delayed** | "The paymaster shakes his head. No coin today. {OWED} denars now owed." |
| **Promissory Note** | "A promissory note replaces coin. The lord's seal promises payment soon." |
| **Corruption** | "Coin changes hands in the shadows. {AMOUNT} denars, and no questions asked." |
| **Side Deal** | "A deal was struck aside the muster line. {AMOUNT} denars, quick and quiet." |

### Implementation

Called from `EnlistmentBehavior` payment processing methods:

```csharp
// In ProcessFullPayment, ProcessPartialPayment, ProcessPayDelay
EnlistedNewsBehavior.Instance?.AddPayMusterNews(outcome, amountPaid, amountOwed);
```

Localization strings use `News_PayMuster_{Outcome}` keys in `enlisted_strings.xml`.

### Player Value

This historical record helps players:
- Identify lords who consistently fail to pay
- Track total backpay owed over time
- Make informed decisions about service continuation
- Understand why Pay Tension might be elevated
