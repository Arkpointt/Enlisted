# Pay System Rework — Plan

## Index
- Goals
- Current Implementation
- What to Retire/Replace
- Proposed System Overview
- Pay Muster Design
- Discharge & Final Muster
- Integration Notes
- Config & Localization
- Logging & Safety
- Testing Plan
- Implementation Sequence
- Open Items

## Goals
- Let vanilla clan finances and native wages run untouched so other mods can hook them.
- Pay soldiers from a self-contained “Munsters” payroll every ~12 days via a map incident choice.
- Fold fatigue costs and skill checks into the payout decision; stop enlistment pay from leaking into clan gold.
- Keep workshop income as a personal asset managed by the custom system, not by clan finances.
- Replace the old retirement/desert menu flow with a camp-based Retire + Final Muster that grants tuned rewards/pension by service length, with a pending-discharge window.
- Preserve enlistment history in a reservist record for future re-entry offers.

## Current Implementation (today)
- Daily pay is computed in `EnlistmentBehavior.OnDailyTick` using `CalculateDailyWage()` and `GetWageBreakdown()`.
- Harmony patches on `DefaultClanFinanceModel` inject wages/tooltip lines and zero expenses:
  - `ClanFinanceEnlistmentIncomePatch` (adds wage + workshop income, isolates player income).
  - `ClanFinanceEnlistmentExpensePatch` (returns 0 expenses).
  - `ClanFinanceEnlistmentGoldChangePatch` (forces gold change to use the patched income/expense).
  - `EnlistmentExpenseIsolationPatch` (skips `AddPartyExpense` for player party when enlisted/captured).
  - `MercenaryIncomeSuppressionPatch` (blocks native mercenary award multipliers/income).
- Map incidents are currently suppressed while enlisted (`IncidentsSuppressionPatch`); only the bag-check incident is registered in `EnlistedIncidentsBehavior`.

## What to Retire/Replace
- Remove all enlistment finance Harmony patches so vanilla `DefaultClanFinanceModel` runs normally (income, expenses, gold change, party expense, mercenary multipliers).
- Remove the daily auto-pay side effect; move payment to a scheduled, player-facing incident.
- Re-home workshop income and enlistment pay into the custom ledger so nothing touches clan finances; other mods can freely patch vanilla finance without conflict.

## Proposed System Overview
- Add a Payday scheduler (new behavior or inside `EnlistmentBehavior`) that:
  - Tracks `CampaignTime _nextPayday`, `CampaignTime _lastPayday`, and an accrued `int _pendingMunsters`.
  - Accrues daily wages while enlisted and not captured: `_pendingMunsters += CalculateDailyWage()`.
  - Pauses accrual if on leave, in grace, or captured (configurable).
  - On daily tick, if `CampaignTime.Now >= _nextPayday`, queue a pay incident and lock further accrual until resolved.
- Currency handling:
  - “Munsters” is an internal ledger; payout converts 1:1 to gold via `Hero.MainHero.ChangeHeroGold` (no clan finance UI/tooltips involved).
  - Store ledger + next-payday timestamps in `SyncData`; expose a tiny UI read (command tent/service record) showing `next payday`, `pending amount`, and last outcome.
- Workshop income:
  - Run a lightweight `CollectWorkshopIncome()` daily (or at payday) that sums `CalculateOwnerIncomeFromWorkshop`, withdraws capital, and adds to `pendingMunsters` with its own log line—completely outside clan finance.
- Retire flow (camp map incident + Final Muster):
  - Retire action lives in Camp, replaces the main-menu desert option.
  - Request Discharge sets `IsPendingDischarge`; resolve at the next pay muster (Final Muster branch). Player can cancel before muster.
  - Rewards/pension scale by service days:
    - Washout: -10 lord / -10 faction; no pension.
    - Honorable (100–199 days): +10 lord / +5 faction; 3,000 gold; 50 gold/day pension.
    - Veteran/Heroic (200+ days): +30 lord / +15 faction; 3,000 gold (optional topper); 100 gold/day pension.
  - Pension stops if relation < 0, at war with that kingdom, or criminal rating > 0. Reactivation rules TBD (default: none).
  - Preserve service data into a reservist record for future re-entry/commission offers.

## Map Incident Design (Pay Muster)
- Register a new incident (e.g., `incident_enlisted_pay_muster`) in `EnlistedIncidentsBehavior`, using `MapState.NextIncident` to display native UI; inquiry fallback if incidents are blocked.
- Fire cadence: default every 12 days (configurable, e.g., 11–13 day jitter to avoid clockwork feel).
- Required text keys: title/body, option labels, success/failure toasts, relation warnings, and fatigue hints.
- Whitelist: adjust `IncidentsSuppressionPatch` to allow this incident even while enlisted.

### Option 1 — Standard Pay (“Accept Your Pay and Dismiss”)
- Fatigue: 0.
- Reward: 100% of `pendingMunsters`; reset pending to 0; advance `_nextPayday` by interval.
- Log to session diagnostics + service record.

### Option 2 — Corruption Challenge (“Demand a Recount”)
- Requirement: `Roguery > 20` OR `Charm > 20`.
- Fatigue: 10 (fail-fast via `TryConsumeFatigue`).
- Roll: base 70% success; add +0.5% per point over 20 in the better of Roguery/Charm; cap at 90%.
- Success: payout `pendingMunsters * 1.20`; small lord relation bump (+1?).
- Failure: payout `pendingMunsters * 0.95` (5% penalty fine), apply -5 relation with enlisted lord, log the failed bluff. No XP/progression loss.
- Always reset pending and advance next payday after resolution.

### Option 3 — Side Deal (“Bribe for ‘Select’ Equipment”)
- Fatigue: 6.
- Cost: immediately deduct 60% of `pendingMunsters`; remaining 40% (or minimum 1 day’s base wage) is paid out in gold.
- Benefit: open a limited Quartermaster “select gear” picker:
  - Source: reuse `QuartermasterManager`/`QuartermasterEquipmentSelectorVM` but restrict to a small curated list (e.g., culture-appropriate tiered items flagged as “select”).
  - If no items available, refund the 60% and fall back to Standard Pay flow.
- Optional relation hook: +1 with Quartermaster/officer on success; no change on decline.
- Reset pending and advance next payday after resolution.

## Discharge & Final Muster (delayed discharge)
- Camp menu “Request Discharge”:
  - Sets `IsPendingDischarge = true`; shows a projected discharge report (days served, relation, projected tier: Washout vs Honorable vs Veteran/Heroic) and time until muster. Allow cancel.
- Final Muster branch (runs when pay muster fires and `IsPendingDischarge` is true):
  - Branching by service days and relation:
    - Washout (<100 days): -10 lord / -10 faction; gear = “Stripped/Rags” (equip culture-mapped civilian template). Challenge path: pay fatigue + intimidate to keep “Basic Armor” (culture-mapped civilian battle template); fail → rags and HP hit (set HP to 20%). Smuggle path → deserter: keep all gear, -50 lord, crime +30; fail → jail (prisoner).
    - Honorable (100–199 days, relation ≥ 0): +10 lord / +5 faction; 3,000 gold; pension 50/day. Gear: keep armor (slots 6–9), remove weapons (0–3) and horse/harness (10–11) to stash/delete. Option: charm/rel >20 to keep main weapon; fail → no weapon keep.
    - Veteran/Heroic (200+ days, relation ≥ 0): +30 lord / +15 faction; 3,000 gold (optional topper); pension 100/day. Same gear as Honorable. Smuggle path can keep all (no slot changes) but may add a small risk/flag if desired.
 - Gear handling definitions:
  - Stripped/Rags: clear equipment; equip culture-specific civilian template (e.g., `aserai_troop_civilian_template_t1`); resolve via config mapping per culture; fallback template if missing.
  - Keep Basic Armor: clear equipment; equip culture-specific civilian “battle” template (light gambeson + sidearm), resolved via config mapping per culture; fallback template if missing.
  - Keep Armor / Return Weapons: keep helm/body/gloves/boots (slots 6–9); move weapons (0–3) and horse/harness (10–11) to stash/delete; armor slots (Head/Body/Leg/Gloves/Cape) remain.
  - Keep All: no equipment changes.
 - Pension stop rules: stop paying if relation < 0, at war with the kingdom, or criminal rating > 0; pensions pause on re-enlistment (no double-dipping). Restart only on a new honorable discharge (v1: no automatic restart).
 - Reservist data: on discharge, persist service metrics (days served band, relations at exit, tier/xp) to `ReservistData` for future re-entry/commission offers.

## Technical lock-ins (answers to raised ambiguities)
- Culture resolution & fallback:
  - Use the enlisted lord’s culture (`EnlistedLord?.Culture`); do not use army leader. Map via config per culture; if missing/null, fall back to `culture_empire`. Wrap template assignment safely; if it fails, equip a safe civilian template to avoid naked mesh.
- Numeric defaults (config):
  - `severance_honorable` = 3,000; `severance_veteran` = 10,000.
  - `smuggle_roguery_dc` = 40; `intimidate_skill_dc` = 100 (Athletics or Charm); `hp_fail_set_value` = 20% HP; `deserter_crime_penalty` = 30.
- Incident priority (polite queue):
  - If `_nextPayday` due but player is in a menu/incident/encounter, defer by +0.1 days and retry (no forcing). Late-pay text handles the delay.
- Save/load persistence:
  - Persist: `IsPendingDischarge`, `PendingMunsters`, `NextPayday`, `IsPensionPaused`, `PensionFactionId` in SyncData. On load, if `NextPayday` missing, set to `CampaignTime.Now`.
- War/crime checks target:
  - Pension gating checks against the pension’s originating faction (`PensionFactionId`): `GetCriminalRating(faction)`, `faction.IsAtWarWith(Hero.MainHero.MapFaction)`, and relation to the pension lord.
- Re-entry branching:
  - Use `ReservistData`: Washout → raw recruit (Tier 1); Honorable → NCO path (Tier 3); Heroic with Renown > 300 → Commission path.
- Localization coverage:
  - Add IDs: `str_enlisted_discharge_smuggle_option`, `str_enlisted_discharge_intimidate_option`, `str_enlisted_discharge_jail_outcome`, `str_enlisted_discharge_deserter_outcome`, `str_enlisted_pension_revoked_crime`, `str_enlisted_pension_suspended_war` (plus existing honorable/washout/smuggle/late-pay strings).
- Gear safety:
  - Slot assumptions: Weapons 0–3; Armor Head/Body/Leg/Gloves/Cape; Horse 10, Harness 11. Config toggle `debug_skip_gear_stripping` (default false).
- Duty wage multiplier:
  - Ensure `CalculateDailyWage()` uses `GetDutiesWageMultiplier()` and that ledger accrual/pay muster consume that result after finance patches are removed.
- Discharge order of operations:
  - Strip first (by band): remove weapons (0–3), mounts (10–11), and armor per band rules; respect `debug_skip_gear_stripping`.
  - Equip next: apply culture-mapped civilian/rags template with fallback.
  - Return stash: transfer stash → main inventory after stripping/equipping.
  - Capacity check: if overweight, open loot exchange to let the player drop/adjust before finalizing.

## Integration Notes
- Daily tick changes:
  - Replace wage logging with accrual into `_pendingMunsters`.
  - Gate accrual on enlistment alive-state; pause while captured/leave.
- Patches:
  - Remove all enlistment finance patches (`ClanFinanceEnlistmentIncomePatch`, `ClanFinanceEnlistmentExpensePatch`, `ClanFinanceEnlistmentGoldChangePatch`, `EnlistmentExpenseIsolationPatch`, `MercenaryIncomeSuppressionPatch`) so vanilla finance is fully restored for compatibility.
  - Handle workshop isolation inside the payday flow (or a small helper), not via clan finance patches.
- UI/feedback:
  - Add a payday status line to command tent/service record UI: `Pending Munsters`, `Next payday in X days`, `Last payout result`.
  - Tooltip strings should no longer surface in clan finance; use incident text and command tent summaries instead.
- Retire:
  - Move “Retire” to Camp actions; remove “Desert” from main menus.
  - Register `incident_enlisted_retire` alongside pay muster; uses `MapState.NextIncident` with inquiry fallback. Incident IDs: `incident_enlisted_pay_muster`, `incident_enlisted_discharge`.
  - Pension payments flow through the same custom ledger/daily hook; stop when relation falls below threshold; pause on re-enlistment.
- Harmony patch removal checklist (Phase 1):
  - Delete/disable: `ClanFinanceEnlistmentIncomePatch`, `ClanFinanceEnlistmentExpensePatch`, `ClanFinanceEnlistmentGoldChangePatch`, `EnlistmentExpenseIsolationPatch`, `MercenaryIncomeSuppressionPatch`.
  - Update `IncidentsSuppressionPatch` to whitelist pay muster and retire incidents while enlisted.
- Reservist data:
  - Add a persisted `ReservistData` to `SyncData` on discharge (service days, tier/xp snapshot, last lord/kingdom, relation at exit).
  - Re-entry dialog can branch on reservist status (commission/NCO offers instead of raw recruit).

## Config & Localization
- JSON config (ModuleData/Enlisted/enlisted_config.json):
  - `finance.payday_interval_days`, `finance.payday_jitter_days`, `finance.pay_options` (fatigue, bonuses, penalties).
  - Retire/final muster block: service-day bands (washout <100, honorable 100–199, veteran 200+), pension daily amounts (50/100), relation thresholds for pension stop, crime/war stop, gear template IDs mapped per culture (e.g., `culture_templates.civilian` and `culture_templates.civilian_battle`), fallback culture template, toggles for pending discharge enablement, `debug_skip_gear_stripping`, deserter/crime penalties, smuggle/intimidate DCs, HP fail value.
  - Retirement/pension keys: `retirement.days_washout_threshold` (100), `retirement.pension_honorable_daily` (50), `retirement.pension_veteran_daily` (100), `retirement.severance_honorable` (3000), `retirement.severance_veteran` (10000), `pay.muster_interval_days` (12).
  - Consider a toggle to opt back into daily autopay for compatibility (default off).
- XML strings (ModuleData/Languages/enlisted_strings.xml):
  - Incident title/body, option labels, success/failure toasts, fatigue warnings, relation warnings.
  - Late/queued pay text variants and all Final Muster branches (washout/honorable/veteran, smuggle/deserter/jail outcomes).
  - Explicit IDs: `str_enlisted_discharge_honorable_text`, `str_enlisted_discharge_washout_text`, `str_enlisted_discharge_smuggle_success`, `str_enlisted_discharge_smuggle_fail`, `str_enlisted_discharge_late_pay_text`.

## Logging & Safety
- Log accrual and payout outcomes to `SessionDiagnostics` (include option chosen, payout, relation deltas, fatigue deltas).
- Ensure incident only triggers when safe: not in battle, not captive, map state available; otherwise delay to next tick.
- Add telemetry counter for “missed payday” if the player is captured or absent; resume when free.
- Log retire outcomes: service days, rewards granted, pension start/stop events, relation at time of discharge.
- Backpay safety:
  - If `CampaignTime.Now > _nextPayday`, mark pay muster as Pending and fire immediately upon entering a safe `MapState`.
  - If delayed > 2 days, use a late-payment text variant (“The Paymaster finally catches up…”).
- Persist pending discharge/backpay state across save/load; ensure Final Muster branch cannot be skipped by combat/encounter loops.
 - Daily pension gating: compute stop conditions each day using native read-only war/crime/relation queries against the pension’s faction/lord. Pause pension on re-enlistment.

## Testing Plan
- Unit-ish checks: simulate 15–30 day cycles with enlistment on/off, leave, capture; verify accrual pauses/resumes and incidents fire once per interval.
- Skill-gated option: verify 19 vs. 21 skill gating and probability caps; ensure fatigue pre-check blocks the action.
- Side deal: ensure the gear picker opens, refunds on empty stock, and respects Soldier Tax/selection filters.
- Regression: confirm clan finance UI shows zero enlistment wages/expenses; mercenary income stays suppressed; workshops still pay out via the new path.
- Retire: verify camp action availability, incident safety gating, reward scaling by service days, pension accrual/stop-on-relation-drop, and relation changes applied correctly.
- Final Muster: validate pending-discharge queueing, branch selection (washout/honorable/veteran), gear handling (templates/slots), late-text swap, pension stop rules, reservist data persistence, and smuggle/deserter/jail outcomes.

## Implementation Sequence (keep this order)
- Step 1: Compatibility reset
  - Remove enlistment finance patches; restore vanilla clan finance/wages fully.
  - Implement ledger accrual and pay muster incident (Standard, Corruption Challenge, Side Deal).
  - Ensure wage accrual uses `CalculateDailyWage()` (with duty multiplier) for the ledger; no clan finance involvement.
  - Wire UI status (pending/next payday) and config for cadence/options.
- Step 2: Workshop and pension plumbing
  - Move workshop income into ledger collection; ensure withdrawals happen outside clan finance.
  - Add pension payment hook (50/100 by service band, relation/war/crime-gated), pause on re-enlistment, no double-dipping.
  - Telemetry/logging for accrual, payouts, pension start/stop.
- Step 3: Retire incident
  - Replace main-menu desert with camp-based Retire action that sets pending discharge.
  - Implement `incident_enlisted_retire` / Final Muster branch at the next pay muster: washout vs honorable vs veteran paths, gear handling, relation bonuses per band, pensions per band, smuggle/deserter/jail outcomes.
  - Persist reservist data on discharge; add safety gating and inquiry fallback.
- Step 4: Polish and balance
  - Tune service-day bands, success odds, penalties/bonuses; localize all strings.
  - Edge-case hardening: capture/leave/grace interactions, relation-threshold pension suspension, mod compatibility checks.

## Resolved Design Decisions (Locked)
- Penalty Pay: 5% fine on failed recount (`0.95` multiplier). No XP/progression loss.
- Currency: Munsters ledger always converts to Gold at 1:1.
- Side Deal Relation: Target is the Quartermaster. If relation > 20, add +10% success chance on the roll. Repeated bribery ignored for V1.
- Smuggle/Deserter Penalties:
  - Smuggle Success: Deserter, +30 Crime, -50 Lord Relation, Keep All Gear.
  - Smuggle Failure: Jailed, +30 Crime, -50 Lord Relation, Gear Stripped.
  - Intimidate Failure: HP set to 20% (Beaten).
  - Gold Bonus: 3,000 only for Honorable/Veteran; Washout = 0.
- Pension Rules:
  - Restart: Never restarts if revoked for crime/bad relation.
  - Re-enlistment: Pension pauses (IsPensionPaused = true); no double-dipping.
  - Re-retirement: Pension amount updates to the new band if discharge is Honorable/Veteran.
