# Mechanics Quick Reference

## Index
- [Heat](#heat)
- [Discipline](#discipline)
- [Pay Tension](#pay-tension)
- [Fatigue](#fatigue)
- [Supplies (Lance Need)](#supplies-lance-need)
- [Lance Reputation](#lance-reputation)
- [Medical Risk](#medical-risk)

## Heat
What it is: Attention from shady/corrupt choices (audits, shakedowns, getting watched).

**Causes Heat**
- `corruption.ledger_skim_v1` → `slip_a_tip` (+2)
- `ll_evt_pay_loot_dead` → `loot_enemy` (+2)
- `ll_evt_pay_loot_dead` → `loot_everyone` (+4)
- `ll_evt_mission_debts` → `collect_skim` (+3)
- `ll_evt_mutiny_after` → `execute_lord` (+10)

**Reduces Heat**
- `corruption.ledger_skim_v1` → `refuse_clean` (-1)
- `heat_warning` → `clean_up` (-1)
- `heat_shakedown` → `pay_off` (-2)
- `heat_audit` → `confess` (-4)
- `heat_exposed` → `pay_fine` (-10)
- Passive decay over time (escalation system)

---

## Discipline
What it is: Your discipline trouble record (extra duty, hearings, blocked promotion).

**Causes Discipline**
- `ll_evt_mutiny_trial` → `beg_mercy` (+5)
- `ll_evt_mutiny_trial` → `blame_others` (+3)
- `heat_shakedown` → `comply` (+1)
- `heat_audit` → `confess` (+2)
- High Pay Tension increases discipline incident chance (`GetPayTensionDisciplineModifier()`)

**Reduces Discipline**
- `discipline_extra_duty` → `do_it` (-1)
- `discipline_hearing` → `own_it` (-2)
- Passive decay over time (escalation system)

---

## Pay Tension
What it is: How angry the company is about delayed pay (0–100).

**Raises Pay Tension**
- Pay delayed / overdue (enlistment pay system)

**Reduces Pay Tension**
- `ll_evt_mission_debts` → `collect_polite` (-10)
- `ll_evt_mission_debts` → `collect_aggressive` (-15)
- `ll_evt_pay_loyal` (some options apply `pay_tension_change`, e.g. -15 / -5)
- “Help the Lord” camp actions that call `ReducePayTension(amount)`

---

## Fatigue
What it is: Your personal energy for extra actions (low fatigue = exhausted).

**Spends Fatigue**
- Decisions menu training: `ft_training_formation` (5), `ft_training_combat` (5), `ft_training_specialist` (6)
- Many event options have `costs.fatigue`

**Recovers Fatigue**
- Rest / night recovery (fatigue recovery system)
- Some events grant fatigue relief

---

## Supplies (Lance Need)
What it is: How supplied the lance is (0–100).

**Recover Supplies**
- Schedule activity `foraging` → `need_recovery.Supplies = 15`
- Schedule activity `runner_camp_errands` → `need_recovery.Supplies = 8`

**Loss Supplies**
- Base daily degradation: `lance_needs.base_degradation_rates.Supplies = 5`
- Other schedule activities can add more degradation (configured per activity)

---

## Lance Reputation
What it is: How your lance feels about you (trust vs resentment).

**Raises Lance Reputation**
- Event choice `effects.lance_reputation > 0` (story packs / event defs)

**Reduces Lance Reputation**
- Event choice `effects.lance_reputation < 0` (story packs / event defs)

---

## Medical Risk
What it is: How risky it is to ignore your injuries/illness (0–5).

**Raises Medical Risk**
- Event choice `effects.medical_risk > 0` (story packs / event defs)

**Reduces Medical Risk**
- Treatment / recovery paths (medical system)
- Escalation system can decay/reset it depending on treatment/rest paths


