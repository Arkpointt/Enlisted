# Systems Quick Reference - Cheat Sheet

**Quick lookup guide for system values, thresholds, and story block effects.**

Last Updated: December 13, 2025

---

## System Ranges & Thresholds

### Heat (Corruption Attention)
```
Range: 0-10
Decay: -1 per 7 days

Thresholds:
  3  → heat_warning (lance leader warns)
  5  → heat_shakedown (kit inspection)
  7  → heat_audit (formal audit)
  10 → heat_exposed (caught, consequences)
```

### Discipline
```
Range: 0-10
Decay: -1 per 14 days

Thresholds:
  3  → discipline_extra_duty
  5  → discipline_hearing
  7  → discipline_blocked (no promotion)
  10 → discipline_discharge
```

### Pay Tension
```
Range: 0-100
Decay: Event-driven

Thresholds:
  20 → Grumbling events
  45 → Theft invitation
  50 → Loot dead events
  60 → Confrontation events
  85 → Mutiny events
```

### Lance Reputation
```
Range: -50 to +50
Decay: ±1 per 14 days toward 0

Thresholds:
  +40 → lance_bonded (family)
  +20 → lance_trusted (solid)
   -20 → lance_isolated (outcast)
   -40 → lance_sabotage (enemy)
```

### Medical Risk
```
Range: 0-5
Decay: -1 per day (with rest)

Thresholds:
  3 → medical_worsening
  4 → medical_complication
  5 → medical_emergency
```

### Fatigue
```
Range: 0-30+
Decay: Natural recovery

Typical:
  0-10  → Normal
  11-20 → Tired
  21+   → Exhausted
```

---

## Story Effects Lookup

### By Heat Value

**Add Heat:**
```
+1   corruption: refuse_clean (clean action reduces by 1)
+1   pay_tension: take_weapon
+2   corruption: slip_a_tip
+2   pay_tension: loot_enemy, keep_watch
+3   pay_tension: join_theft
+4   pay_tension: loot_everyone
```

**Reduce Heat:**
```
-1   corruption: refuse_clean
-1   escalation: heat_warning → clean up
-2   escalation: heat_shakedown → pass inspection
```

---

### By Discipline Value

**Add Discipline:**
```
+1   Various infractions caught
+2   Moderate rule-breaking
+3   Major infractions
```

**Reduce Discipline:**
```
-1   escalation: discipline_blocked → accept
-2   escalation: discipline_blocked → successful appeal
```

---

### By Lance Reputation Value

**Add Lance Rep:**
```
+5   pay_tension: join_grumbling
+10  pay_tension: rabble_rouse
+15  pay_tension: join_delegation
+25  pay_tension: lead_delegation
+30  pay_tension: join_mutiny
```

**Reduce Lance Rep:**
```
-5   pay_tension: defend_lord
-10  pay_tension: talk_down, stop_others
-25  pay_tension: report_theft
-30  pay_tension: warn_officers
-40  pay_tension: stop_mutiny
-50  pay_tension: stand_with_officers
```

---

### By Fatigue Cost

**0 Fatigue:**
```
discipline: look_away, standard_checks
morale: listen, turn_in (restores 1)
logistics: do_nothing
medical: watch, leave
training: let_them_run, turn_in (restores 1)
pay_tension: most dialogue options
```

**1 Fatigue:**
```
discipline: step_in
morale: lead_song
medical: take_seriously
pay_tension: various risky actions
```

**2 Fatigue:**
```
discipline: polish_kit
logistics: forage_proper
medical: help
training: lead_drill
pay_tension: intense confrontations
```

---

## Story Pack Files

### corruption.json
```
Stories: 1
Systems: Heat
Location: Town only

corruption.ledger_skim_v1
  → refuse_clean: -1 Heat, 0 Fatigue
  → slip_a_tip: +2 Heat, 0 Fatigue, costs 100 gold
```

### discipline.json
```
Stories: 2
Systems: Fatigue
Location: Camp

discipline.mess_line_fight_v1 (day)
  → step_in: +1 Fatigue, Leadership XP
  → break_it_up: 0 Fatigue
  → look_away: 0 Fatigue

discipline.dawn_inspection_v1 (dawn)
  → polish: +2 Fatigue, Steward XP
  → standard: 0 Fatigue
```

### morale.json
```
Stories: 2
Systems: Fatigue, Lance Rep (minor)
Location: Camp

morale.campfire_song_v1 (night)
  → lead: +1 Fatigue, Charm+Leadership XP
  → listen: 0 Fatigue, Leadership XP
  → turn_in: -1 Fatigue (rest)

morale.after_battle_words_v1 (leaving_battle, night)
  → steady: 0 Fatigue, Leadership XP
  → gentle: 0 Fatigue, Charm XP
```

### logistics.json
```
Stories: 1
Systems: Fatigue, Gold
Location: Camp

logistics.thin_wagons_v1
  → forage_proper: +2 Fatigue, Scouting+Steward XP
  → quietly_buy: 0 Fatigue, costs 50 gold, Charm XP
  → do_nothing: 0 Fatigue
```

### medical.json
```
Stories: 2
Systems: Fatigue
Location: Camp

medical.aid_tent_shift_v1 (day)
  → help: +2 Fatigue, Medicine XP
  → watch: 0 Fatigue, Medicine XP (small)
  → leave: 0 Fatigue

medical.bandage_drill_v1 (dusk)
  → take_seriously: +1 Fatigue, Medicine XP
  → half_listen: 0 Fatigue, Medicine XP (small)
```

### training.json
```
Stories: 1
Systems: Fatigue, Medical Risk (5% injury)
Location: Camp

training.lance_drill_night_sparring_v1 (night)
  → lead: +2 Fatigue, Athletics+Polearm XP, 5% injury
  → delegate: 0 Fatigue, Leadership XP
  → turn_in: -1 Fatigue (rest)
```

---

## Event Pack Files

### events_pay_tension.json
```
Events: 5
Trigger: Pay Tension thresholds + LeavingBattle
Systems: Heat, Discipline, Lance Rep

pay_tension_grumbling (≥20)
  Major effects: Lance Rep -5 to +10

pay_tension_loot_the_dead (≥50)
  Major effects: Heat +1 to +4, Lance Rep -10

pay_tension_theft_invitation (≥45)
  Major effects: Heat +2 to +3, Lance Rep -25

pay_tension_confrontation (≥60)
  Major effects: Lance Rep -30 to +25

pay_tension_mutiny_brewing (≥85)
  Major effects: Lance Rep -50 to +30, Discipline variable
```

### events_escalation_thresholds.json
```
Events: 16
Trigger: Escalation thresholds reached
Systems: All escalation systems

Heat Events: 4
  heat_warning (≥3)
  heat_shakedown (≥5)
  heat_audit (≥7)
  heat_exposed (=10)

Discipline Events: 4
  discipline_extra_duty (≥3)
  discipline_hearing (≥5)
  discipline_blocked (≥7)
  discipline_discharge (=10)

Lance Rep Events: 4
  lance_trusted (≥+20)
  lance_bonded (≥+40)
  lance_isolated (≤-20)
  lance_sabotage (≤-40)

Medical Events: 3
  medical_worsening (≥3)
  medical_complication (≥4)
  medical_emergency (=5)
```

---

## Common Patterns

### High-Risk Corruption Path
```
Story/Event                          Heat    Discipline    Lance Rep
────────────────────────────────────────────────────────────────────
corruption.ledger_skim (bribe)       +2         —            —
pay_tension.loot_everyone            +4         —            —
pay_tension.join_theft               +3         —            —
                                    ─────    ─────        ─────
Total                                +9         0            0

Triggers: heat_warning (3), heat_shakedown (5), heat_audit (7)
```

### Solidarity Path (Build Lance Rep)
```
Story/Event                          Heat    Discipline    Lance Rep
────────────────────────────────────────────────────────────────────
pay_tension.rabble_rouse              —          —           +10
pay_tension.join_delegation           —          —           +15
pay_tension.lead_delegation           —          —           +25
                                    ─────    ─────        ─────
Total                                 0          0           +50

Triggers: lance_trusted (+20), lance_bonded (+40)
```

### Betrayal Path (Lose Lance Rep)
```
Story/Event                          Heat    Discipline    Lance Rep
────────────────────────────────────────────────────────────────────
pay_tension.defend_lord               —          —            -5
pay_tension.report_theft              —          —           -25
pay_tension.warn_officers             —          —           -30
pay_tension.stand_with_officers       —          —           -50
                                    ─────    ─────        ─────
Total                                 0          0          -110 (capped at -50)

Triggers: lance_isolated (-20), lance_sabotage (-40)
```

### Clean Path (Avoid Heat)
```
Story/Event                          Heat    Discipline    Lance Rep
────────────────────────────────────────────────────────────────────
corruption.refuse_clean              -1          —            —
pay_tension.leave_dead_alone          0          —            —
pay_tension.decline_theft             0          —            —
escalation.heat_warning (clean up)   -1          —            —
                                    ─────    ─────        ─────
Total                                -2          0            0

Keeps Heat low or reduces it
```

---

## System Interactions

### Pay Tension → Multiple Systems
```
Pay Tension events are unique in affecting 3+ systems:

Event: pay_tension_loot_the_dead
  Option: loot_everyone
    Heat:           +4 (corruption)
    Fatigue:        +1 (physical work)
    Lance Rep:      0 (solo activity)
    Gold:           +55 (reward)
    XP:             Roguery +25
    Risk:           15% failure → +Discipline
```

### Training → Medical Risk + Fatigue
```
Event: training.lance_drill_night_sparring_v1
  Option: lead_drill
    Fatigue:        +2 (intense activity)
    Medical Risk:   5% injury chance → sets initial risk
    XP:             Athletics +35, Polearm +35
    
If injured:
  Medical Risk: 3 (from injury)
  Triggers: medical_worsening if untreated
  Fatigue: +accumulated from injury effects
```

### Lance Rep + Heat Conflict
```
Building solidarity through corrupt acts:

pay_tension.rabble_rouse
  Heat:           0 (talking only)
  Lance Rep:      +10 (solidarity)
  
pay_tension.join_theft
  Heat:           +3 (criminal act)
  Lance Rep:      0 (secret activity)
  Gold:           +55
  
Result: High Lance Rep, High Heat
Outcome: Lance trusts you, but officers investigating
```

---

## Config Values

From `enlisted_config.json`:

### Thresholds
```json
{
  "camp_life": {
    "pay_tension_high_threshold": 70,
    "heat_high_threshold": 70,
    "morale_low_threshold": 70
  }
}
```

### Decay Intervals
```json
{
  "escalation": {
    "heat_decay_interval_days": 7,
    "discipline_decay_interval_days": 14,
    "lance_rep_decay_interval_days": 14,
    "medical_risk_decay_interval_days": 1,
    "threshold_event_cooldown_days": 7
  }
}
```

### Lance Life
```json
{
  "lance_life": {
    "max_stories_per_week": 2,
    "min_days_between_stories": 2
  },
  "lance_life_events": {
    "automatic": {
      "evaluation_cadence_hours": 6,
      "max_events_per_day": 1,
      "min_hours_between_events": 12
    }
  }
}
```

---

## Time-of-Day Triggers

```
dawn   → discipline.dawn_inspection_v1
day    → discipline.mess_line_fight_v1
       → medical.aid_tent_shift_v1
dusk   → medical.bandage_drill_v1
night  → morale.campfire_song_v1
       → morale.after_battle_words_v1
       → training.lance_drill_night_sparring_v1
```

---

## Location Triggers

```
entered_town      → corruption.ledger_skim_v1
leaving_battle    → morale.after_battle_words_v1
LeavingBattle     → pay_tension events (all)
any/camp          → most other stories
```

---

## XP Rewards by Skill

### Leadership
```
20 XP: discipline.step_in
       morale.listen
       pay_tension.defend_lord
       pay_tension.stop_others
30 XP: pay_tension.stop_mutiny
```

### Charm
```
10 XP: corruption.refuse_clean
15 XP: morale.lead (with Leadership)
       pay_tension.talk_down
20 XP: morale.gentle
25 XP: logistics.quietly_buy
```

### Roguery
```
15 XP: pay_tension.rabble_rouse
       pay_tension.keep_watch
20 XP: pay_tension.slip_away
25 XP: pay_tension.join_theft, loot_everyone
30 XP: corruption.slip_a_tip
```

### Steward
```
20 XP: discipline.polish
30 XP: logistics.forage_proper (with Scouting)
```

### Medicine
```
5-10 XP: medical.half_listen, watch
20 XP: medical.take_seriously
25 XP: medical.help
```

### Combat Skills
```
35 XP: training.lead_drill (Athletics + Polearm)
20 XP: pay_tension.stand_with_officers (Leadership + One-handed)
```

### Scouting
```
30 XP: logistics.forage_proper (with Steward)
```

---

## Risk Levels

```
safe     → No failure chance, guaranteed outcome
risky    → Chance of failure with alternate outcome
corrupt  → Adds Heat, may have failure chance
```

### Risky Options with Failure Chances

```
5%   training.lead_drill (injury)
10%  pay_tension.loot_enemy (caught)
     pay_tension.keep_watch (caught)
     pay_tension.slip_away (caught)
15%  pay_tension.loot_everyone (caught)
     pay_tension.join_theft (caught)
30%  pay_tension.stand_with_officers (death)
40%  pay_tension.stop_mutiny (beaten)
```

---

## Gold Costs & Rewards

### Costs
```
50   logistics.quietly_buy
100  corruption.slip_a_tip
```

### Rewards
```
30   pay_tension.loot_enemy
50   pay_tension.stand_with_officers
55   pay_tension.loot_everyone, join_theft
```

---

## Finding Specific Content

### "I want stories with NO fatigue cost"
```
corruption: refuse_clean
discipline: look_away, standard_checks
morale: listen
logistics: do_nothing
medical: watch, leave
training: delegate
All pay_tension dialogue options
```

### "I want stories that restore fatigue"
```
morale.campfire_song_v1 → turn_in (-1)
training.lance_drill_night_sparring_v1 → turn_in (-1)
```

### "I want stories with injury risk"
```
training.lance_drill_night_sparring_v1 → lead_drill (5%)
pay_tension.stand_with_officers (30% death on failure)
```

### "I want stories that give Medicine XP"
```
medical.aid_tent_shift_v1 (all options)
medical.bandage_drill_v1 (all options)
```

### "I want stories that affect Heat"
```
corruption.json (all stories)
events_pay_tension.json (looting, theft events)
events_escalation_thresholds.json (threshold consequences)
```

### "I want stories with big Lance Rep swings"
```
pay_tension.lead_delegation (+25)
pay_tension.join_mutiny (+30)
pay_tension.warn_officers (-30)
pay_tension.stop_mutiny (-40)
pay_tension.stand_with_officers (-50)
```

---

## Adding New Content Checklist

```
[ ] Choose category (corruption, discipline, morale, etc.)
[ ] Define trigger conditions (time, location, thresholds)
[ ] Set tier range (tierMin, tierMax)
[ ] Set cooldown (typically 5-9 days)
[ ] For each option:
    [ ] Define risk level (safe/risky/corrupt)
    [ ] Set fatigue cost (0-3)
    [ ] Set gold cost/reward (if any)
    [ ] Set XP rewards (skill + amount)
    [ ] Set system effects (heat, discipline, lance_rep, medical)
    [ ] Write outcome text
    [ ] Write failure text (if risky)
[ ] Update this quick reference
[ ] Validate JSON
[ ] Test in-game
```

---

## Document Navigation

- Full details: `SYSTEMS_AND_STORY_BLOCKS_REFERENCE.md`
- Escalation design: `docs/research/escalation_system.md`
- Pay Tension: `docs/research/pay_tension_events.md`
- Event metadata: `docs/research/event_metadata_index.md`
