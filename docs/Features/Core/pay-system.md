# Pay System

**Summary:** The pay system manages daily wages, muster payouts, bonuses, battle loot sharing, and pay tension for enlisted soldiers. Pay accumulates daily and is distributed every 12 days at muster, with tier-based wage scaling and various bonus mechanisms including unit fund contributions and loot shares.

**Status:** ✅ Current  
**Last Updated:** 2025-12-22  
**Related Docs:** [Enlistment](enlistment.md), [Core Gameplay](core-gameplay.md)

---

## Index

- [Overview](#overview)
- [Daily Wage Calculation](#daily-wage-calculation)
- [Pay Muster](#pay-muster)
- [Unit Fund](#unit-fund)
- [Pay Tension](#pay-tension)
- [Free Desertion](#free-desertion)
- [Battle Loot Share](#battle-loot-share)
- [Tier-Gated Loot](#tier-gated-loot)

---

## Overview

| Component | Purpose |
|-----------|---------|
| **Daily Wage** | Base pay + modifiers, calculated each day. |
| **Pay Muster** | Periodic payout events (~12 days). |
| **Pay Tension** | Escalation counter (0-100) when pay is late or disrupted. |
| **Battle Loot Share** | Gold bonus from victories (T1-T3). |
| **Unit Fund** | 5% deduction for shared unit supplies (replaces legacy Lance Fund). |
| **Tier-Gated Loot** | T4+ access native loot screens. |

---

## Daily Wage Calculation

Wages are calculated daily based on your rank, performance, and the lord's financial state.

### Base Pay by Tier

| Tier | Base Pay | Role Group |
|------|----------|------------|
| T1 | 5 | Enlisted |
| T2 | 10 | Enlisted |
| T3 | 20 | Enlisted |
| T4 | 40 | Enlisted |
| T5 | 60 | Officer |
| T6 | 80 | Officer |
| T7 | 100 | Commander |
| T8 | 125 | Commander |
| T9 | 150 | Commander |

### Modifiers

- **Wartime Modifier**: +20% hazard pay when the lord is at war.
- **Lord Wealth Status**: Wages are scaled by the lord's treasury (from -25% for "Broke" to +25% for "Wealthy").
- **Order Multipliers**: Successful completion of high-priority orders can grant temporary wage bonuses.
- **Probation**: Soldiers on probation (after desertion or washout) receive a reduced wage multiplier.

---

## Pay Muster

Every ~12 days, a pay muster event occurs. This is delivered as a native map incident or an inquiry.

### Payment Outcomes

- **Full Payment**: Pays all owed backpay. Resets Pay Tension.
- **Partial Payment**: Pays a portion of backpay. Pay Tension reduced proportionally.
- **Promissory Note (IOU)**: Available when the company is under financial strain. No gold is paid, but backpay is preserved and a retry is scheduled in 3 days.
- **No Payment**: Backpay accumulates, and Pay Tension increases significantly.

---

## Unit Fund

A 5% deduction is taken from each payment for the **Unit Fund** (shared supplies).
- These funds are used by the unit for general maintenance and supplies.
- The balance is returned to the player upon an **Honorable Discharge**.
- The balance is forfeited upon **Desertion**.

---

## Pay Tension (0-100)

Tracks the dissatisfaction of the men when pay is late or the unit is poorly supplied.

### Effects
- **Low (0-39)**: No major effects.
- **Elevated (40-59)**: Minor morale penalties; increased chance of negative camp events.
- **High (60-79)**: Significant morale hit; risk of individual desertion.
- **Critical (80+)**: Severe morale penalties; risk of mutiny or unit-wide desertion.

---

## Free Desertion

When **Pay Tension ≥ 60**, the player can leave service without the usual heavy penalties:
- Minimal relation hit with the lord.
- No crime rating increase.
- Keep all current equipment.
- Note: Pensions and Unit Fund are still forfeited.

---

## Battle Loot Share

To simulate the life of a rank-and-file soldier, T1-T3 soldiers do not receive native loot from battles. Instead, they receive a **Gold Share** based on:
- Tier base share.
- Enemy casualties (higher for victories).
- Player's personal kill count in the battle.

---

## Tier-Gated Loot

| Tier | Loot Access |
|------|-------------|
| T1-T3 | **Blocked** (compensated via gold share). |
| T4+ | **Allowed** (native loot screens enabled). |
