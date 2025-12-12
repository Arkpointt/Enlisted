# Pay System — Muster Ledger & Pay Muster

## Overview
While enlisted, your wages do **not** flow through clan finance. Instead, wages accrue into a private **muster ledger** and are paid out periodically via a **Pay Muster** prompt.

This system also owns:
- Pay muster choices (standard pay / corruption / side deal)
- Discharge via **Pending Discharge → Final Muster**
- Pensions (daily stipend) and pension gating
- Reservist snapshots and re-entry boosts/probation

## How wages work (Muster Ledger)
- **Accrual**: Each in-game day, the mod computes your daily wage and adds it to `PendingMusterPay`.
- **Schedule**: A payday timer (`NextPayday`) triggers roughly every ~12 days (configurable, with optional jitter).
- **No clan finance**: No Harmony patching of `DefaultClanFinanceModel`; vanilla finance/workshops remain native.

### Wage inputs
Daily wage is based on:
- Base wage formula (config)
- Player level bonus
- Tier bonus (Tier 1–6)
- Service seniority (XP-based)
- Army bonus (when your lord is in an army)
- Duty/profession wage multiplier
- **Probation multiplier** (if on probation)
- Wage is clamped after multipliers (minimum **24/day**, maximum **150/day**)

## Pay Muster (payout prompt)
When `NextPayday` is reached and it is safe to show UI, the mod queues a **Pay Muster** prompt.

**Current UI**: Uses an inquiry fallback (not native incident UI).

### Options
- **Standard Pay**
  - Pays **100%** of `PendingMusterPay`.
  - Clears probation (if active).
  - Resets `PendingMusterPay` to 0 and schedules the next payday.

- **Corruption Challenge**
  - Fatigue cost: **10**.
  - Requirement: **Roguery > 20** or **Charm > 20**.
  - Success chance: 70% base, +0.5% per point above 20 (cap 90%).
  - **Success**: pays `PendingMusterPay * 1.20`, +1 relation with the enlisted lord.
  - **Failure**: pays `PendingMusterPay * 0.95`, -5 relation with the enlisted lord.
  - Resets `PendingMusterPay` and schedules the next payday.

- **Side Deal**
  - Fatigue cost: **6**.
  - Pays out **40%** of `PendingMusterPay`, and burns the other 60%.
  - If the side deal payout would be less than your projected daily wage, it falls back to **Standard Pay**.
  - (Gear picker is a future extension; currently payout-only.)
  - Resets `PendingMusterPay` and schedules the next payday.

## Discharge & Final Muster
Discharge is handled as a **pending state** which resolves at the next pay muster.

### Request / Cancel
In **Camp ("My Camp")**, you can:
- **Request Discharge (Final Muster)** → sets `IsPendingDischarge = true`
- **Cancel Pending Discharge**

### Final Muster resolution
At pay muster, if `IsPendingDischarge` is true, the pay muster prompt includes Final Muster outcomes.

**Bands (by days served)**
- **Washout** (<100 days)
  - Relation: -10 lord / -10 faction leader
  - Pension: none
  - Gear: stripped (unless `debug_skip_gear_stripping`)

- **Honorable** (100–199 days, relation ≥ 0)
  - Relation: +10 lord / +5 faction leader
  - Severance: `severance_honorable`
  - Pension: `pension_honorable_daily`
  - Gear: keep armor; weapons (0–3) and mount/harness (10–11) moved to inventory

- **Veteran/Heroic** (200+ days, relation ≥ 0)
  - Relation: +30 lord / +15 faction leader
  - Severance: `severance_veteran`
  - Pension: `pension_veteran_daily`
  - Gear: same as Honorable

- **Smuggle (Deserter path)**
  - Relation: -50 lord / -50 faction leader
  - Crime: +30
  - Pension: none
  - Gear: keep all

### Reservist snapshot
On discharge, a reservist snapshot is recorded (band, days served, tier/XP, faction/lord, relation at exit). This snapshot can be consumed on re-entry.

## Pensions
Pensions are a daily stipend granted on Honorable/Veteran discharge.

- **Paid daily** while not enlisted.
- **Paused on re-enlistment** (no double-dipping).
- **Stops** when any of these are true:
  - Relation to the pension faction leader drops below `pension_relation_stop_threshold`
  - You are at war with the pension faction
  - Your crime rating against the pension faction is > 0

## Re-entry (Reservist boosts & probation)
When you re-enlist with the **same faction**, and a reservist snapshot exists (and has not been consumed):
- **Washout / Deserter** → start Tier 1, **probation** enabled
- **Honorable** → start Tier 3, +500 XP, +5 relation bonus
- **Veteran/Heroic** → start Tier 4, +1000 XP, +10 relation bonus

### Probation
Probation is a temporary status applied on washout/deserter re-entry:
- Wage multiplier reduced via `probation_wage_multiplier`
- Fatigue cap reduced via `probation_fatigue_cap`
- Clears on pay muster resolution (and/or after `probation_days`)

## UI surfaces
- **Camp ("My Camp")** shows:
  - `PendingMusterPay`, `NextPayday`, last pay outcome, pay-muster pending flag
  - Pension amount and pension paused state
  - Pending discharge state and discharge actions

## Configuration
All settings live in JSON under `ModuleData/Enlisted/`.

Relevant keys (see `enlisted_config.json`):
- `finance.payday_interval_days`
- `finance.payday_jitter_days`
- `retirement.severance_honorable`
- `retirement.severance_veteran`
- `retirement.pension_honorable_daily`
- `retirement.pension_veteran_daily`
- `retirement.pension_relation_stop_threshold`
- `retirement.debug_skip_gear_stripping`
- `retirement.probation_days`
- `retirement.probation_wage_multiplier`
- `retirement.probation_fatigue_cap`
