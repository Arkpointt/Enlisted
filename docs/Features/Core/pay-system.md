# Pay System

The pay system tracks wages, bonuses, and pay tension for enlisted soldiers.

## Overview

| Component | Purpose |
|-----------|---------|
| **Daily Wage** | Base pay + modifiers, calculated each day |
| **Pay Muster** | Periodic payout events (~12 days) |
| **Pay Tension** | Escalation counter (0-100) when pay is late |
| **Battle Loot Share** | Gold bonus from victories |
| **Lance Fund** | 5% deduction for shared supplies |
| **Tier-Gated Loot** | T4+ access native loot screens |

---

## Daily Wage Calculation

```
Final Wage = Base Pay × Culture Modifier × Wartime Modifier × Lord Wealth Modifier × Duty Multiplier
```

### Base Pay by Tier

| Tier | Base Pay | Track |
|------|----------|-------|
| T1 | 3 | Enlisted |
| T2 | 6 | Enlisted |
| T3 | 12 | Enlisted |
| T4 | 24 | Enlisted |
| T5 | 40 | Officer |
| T6 | 60 | Officer |
| T7 | 80 | Commander |
| T8 | 100 | Commander |
| T9 | 120 | Commander |

### Modifiers

**Culture Modifier:**
| Culture | Modifier |
|---------|----------|
| Aserai | +10% |
| Empire | +5% |
| Vlandia | 0% |
| Battania | -5% |
| Khuzait | -5% |
| Sturgia | -10% |

**Wartime Modifier:** +20% hazard pay when lord is at war.

**Lord Wealth Status:**
| Status | Modifier | Condition |
|--------|----------|-----------|
| Wealthy | +25% | Gold > 50,000 |
| Comfortable | +10% | Gold > 20,000 |
| Adequate | 0% | Gold > 5,000 |
| Strained | -10% | Gold > 1,000 |
| Broke | -25% | Gold ≤ 1,000 |

---

## Pay Muster

Every ~12 days, a pay muster event occurs. The lord attempts to pay accumulated wages.

### Payment Outcomes

| Scenario | Result |
|----------|--------|
| **Full Payment** | Reset PayTension to 0, -5 per day since paid |
| **Partial Payment** | Some backpay, PayTension reduced proportionally |
| **Promissory Note (IOU)** | No payment today, backpay remains owed, retry in 3 days |
| **No Payment** | Backpay accumulates, +5 PayTension per missed payment |

### Promissory Notes (IOU)

When camp conditions disrupt payroll (high pay tension, logistics strain, territory losses), the player can accept a promissory note instead of waiting for full payment.

**How It Works:**
- Available when `payDisrupted` flag is true (driven by camp conditions)
- Menu option: "Accept a promissory note (IOU)"
- No payment occurs, all backpay remains in ledger
- Next pay muster scheduled in 3 days (instead of ~12 days)
- Player can resolve pay when conditions improve

**Benefits:**
- Avoids long wait for next regular muster
- No penalties or consequences
- Backpay preserved and tracked
- Quick retry allows flexible resolution

**Implementation:**
```csharp
internal void ResolvePromissoryMuster()
{
    _lastPayOutcome = $"promissory:{_pendingMusterPay}";
    _payMusterPending = false;
    _nextPayday = CampaignTime.Now + CampaignTime.Days(3f);
}
```

### Lance Fund

5% of each payment is deducted for lance shared supplies.
- Accumulates in `LanceFundBalance`
- Returned on honorable discharge
- Forfeit on desertion

---

## Pay Tension (0-100)

Tracks soldier desperation when pay is late.

### Effects

| Tension | Morale | Discipline | NPC Desertion |
|---------|--------|------------|---------------|
| 0-19 | 0 | Normal | 0% |
| 20-39 | -3 | Normal | 0% |
| 40-59 | -6 | +5% incidents | 0% |
| 60-79 | -10 | +10% incidents | 1%/day |
| 80-89 | -15 | +20% incidents | 3%/day |
| 90-100 | -15 | +20% incidents | 5%/day |

### Tension Changes

| Event | Change |
|-------|--------|
| Full payment | -5 per day since paid, reset to 0 |
| Partial payment | Proportional reduction |
| Promissory note (IOU) | No change (deferred, retry in 3 days) |
| Missed payment | +5 per event |
| Loyal path mission | -10 to -25 depending on mission |

---

## Free Desertion

When PayTension ≥ 60, the player can leave without penalty:
- -5 relation with lord only (they understand)
- No crime penalty
- Keep equipment
- No pension
- Menu option: "Leave Without Penalty"

---

## Battle Loot Share

T1-T3 soldiers don't access loot screens but receive gold compensation.

### Gold Share Calculation

```csharp
int baseShare = tier switch
{
    1 => 5,
    2 => 10,
    3 => 20,
    _ => 0  // T4+ use native loot
};

int bonus = playerWon ? enemyCasualties : enemyCasualties / 2;
int total = baseShare + bonus;
```

---

## Tier-Gated Loot

| Tier | Loot Access | Compensation |
|------|-------------|--------------|
| T1-T3 | **Blocked** | Gold share |
| T4+ | **Allowed** | Native loot screens |

Implementation: `LootBlockPatch.ShouldBlockLoot()` checks tier.

---

## Events

### Pay Tension Events

| Event | Tension | Trigger |
|-------|---------|---------|
| Grumbling | 20+ | Post-battle |
| Theft Invitation | 45+ | Post-battle (15% chance) |
| Loot the Dead | 50+ | Post-battle |
| Confrontation | 60+ | Post-battle |
| Mutiny Brewing | 85+ | Post-battle |

### Loyal Path Missions

| Mission | Tension | Effect |
|---------|---------|--------|
| Collect Debts | 40+ | -10 to -15 tension |
| Escort Merchant | 50+ | -15 tension |
| Negotiate Loan | 60+ | -15 to -25 tension |
| Raid Enemy | 70+ | -25 tension |

### Mutiny & Desertion Chains

| Event | Trigger |
|-------|---------|
| Desertion Planning | After joining desertion plot |
| Mutiny Resolution | After joining mutiny |
| Mutiny Aftermath | After mutiny success |
| Mutiny Trial | After mutiny failure |

---

## API Reference

### EnlistmentBehavior Properties

| Property | Type | Description |
|----------|------|-------------|
| `PayTension` | int | Current tension (0-100) |
| `OwedBackpay` | int | Accumulated unpaid wages |
| `DaysSincePay` | int | Days since last payment |
| `IsPayOverdue` | bool | True if > 7 days since pay |
| `LanceFundBalance` | int | Accumulated 5% deductions |
| `IsFreeDesertionAvailable` | bool | True if tension ≥ 60 |

### EnlistmentBehavior Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `CalculateDailyWage()` | int | Wage with all modifiers |
| `GetWageBreakdown()` | string | Tooltip text with breakdown |
| `GetPayTensionMoralePenalty()` | int | -3 to -15 based on tension |
| `GetPayTensionDisciplineModifier()` | float | 0.05 to 0.20 incident chance |
| `ProcessFreeDesertion()` | void | Execute free desertion |
| `AwardBattleLootShare(MapEvent)` | int | Calculate and award gold |
| `ResolvePromissoryMuster()` | void | Accept IOU, defer payment, schedule retry in 3 days |

---

## File Locations

| File | Purpose |
|------|---------|
| `EnlistmentBehavior.cs` | Core pay state and methods (including `ResolvePromissoryMuster`) |
| `EnlistedIncidentsBehavior.cs` | Pay muster inquiry presentation (IOU option) |
| `CampLifeBehavior.cs` | Camp conditions that trigger `payDisrupted` flag |
| `LootBlockPatch.cs` | Tier-gated loot blocking |
| `events_pay_tension.json` | Tension event definitions |
| `events_pay_loyal.json` | Loyal path missions |
| `events_pay_mutiny.json` | Mutiny/desertion chains |
| `enlisted_strings.xml` | Localization strings (including `enlisted_pay_iou`) |

