# Orders Content Catalog

**Summary:** Complete catalog of all orders for the Order Progression System. Each order includes duration, skills, fatigue, injury risk, phase blocks, and event pool references. This is the content specification for JSON implementation.

**Status:** 📋 Specification  
**Last Updated:** 2025-12-31 (Added context-variant text for sea/land awareness)  
**Related Docs:** [Order Progression System](order-progression-system.md), [Order Events Master](order-events-master.md), [Content Orchestrator](content-orchestrator-plan.md), [Event System Schemas](../Features/Content/event-system-schemas.md#order-context-variants-sealand-awareness)

---

## Index

1. [Overview](#overview)
2. [Order Schema](#order-schema)
3. [T1-T3 Basic Soldier Orders](#t1-t3-basic-soldier-orders)
4. [T4-T6 Specialist Orders](#t4-t6-specialist-orders)
5. [Implementation Notes](#implementation-notes)

---

## Overview

Orders are multi-day duty assignments that progress through phases (4 per day). Each order defines:
- **Duration** - How many days/phases
- **Skills** - Primary and secondary skills for checks
- **Consequences** - Fatigue accumulation, injury risk
- **Blocks** - Sequence of phases with types (routine, slot, checkpoint, resolve)
- **Event Pool** - Which events can fire during this order

### Order Count Summary

| Tier Range | Order Count | Focus |
|------------|-------------|-------|
| T1-T3 | 8 orders | Menial, physical, routine |
| T4-T6 | 8 orders | Specialist, skilled, leadership |
| **Total** | **16 orders** | |

---

## Order Schema

Each order follows this JSON structure:

```json
{
  "id": "order_guard_post",
  "titleId": "order_guard_post_title",
  "title": "Guard Post",
  "descriptionId": "order_guard_post_desc",
  "description": "Stand watch at your assigned post. Stay alert. Report anything unusual.",
  
  "issuer": "Sergeant",
  "tier_min": 1,
  "tier_max": 3,
  
  "context_variants": {
    "sea": {
      "title": "Deck Watch",
      "titleId": "order_guard_post_sea_title",
      "description": "Stand watch on the foredeck. Keep your eyes on the horizon.",
      "descriptionId": "order_guard_post_sea_desc"
    }
  },
  
  "duration_days": 2,
  "primary_skill": "Perception",
  "secondary_skill": null,
  
  "fatigue_weight": "Light",
  "base_injury_risk": 0.01,
  
  "blocks": [
    { "day": 1, "phase": "Dawn",   "type": "routine",    "statusId": "...", "status": "You take your post..." },
    { "day": 1, "phase": "Midday", "type": "routine",    "statusId": "...", "status": "Sun overhead..." },
    { "day": 1, "phase": "Dusk",   "type": "slot",       "statusId": "...", "status": "Evening settles..." },
    { "day": 1, "phase": "Night",  "type": "slot!",      "statusId": "...", "status": "Dark and quiet..." },
    { "day": 2, "phase": "Dawn",   "type": "slot",       "statusId": "...", "status": "Morning rounds..." },
    { "day": 2, "phase": "Midday", "type": "routine",    "statusId": "...", "status": "Almost done..." },
    { "day": 2, "phase": "Dusk",   "type": "checkpoint", "statusId": "...", "status": "Shift ends soon..." },
    { "day": 2, "phase": "Night",  "type": "resolve" }
  ],
  
  "event_pool": [
    "guard_drunk_soldier",
    "guard_strange_noise",
    "guard_officer_inspection",
    "guard_caught_sneaking",
    "guard_relieved_early",
    "guard_double_shift"
  ],
  
  "resolution": {
    "success": {
      "reputation": { "officer": 8 },
      "company_needs": { "Readiness": 5 },
      "skill_xp": { "Perception": 15 },
      "textId": "order_guard_post_success",
      "text": "Quiet watch. Nothing to report. Sergeant nods approval."
    },
    "partial": {
      "reputation": { "officer": 3 },
      "skill_xp": { "Perception": 8 },
      "textId": "order_guard_post_partial",
      "text": "Watch complete. Could have been more alert."
    },
    "failure": {
      "reputation": { "officer": -10 },
      "company_needs": { "Readiness": -5 },
      "textId": "order_guard_post_failure",
      "text": "Something happened on your watch. The sergeant is furious."
    }
  },
  
  "strategic_tags": ["camp_routine", "defense", "garrison"],
  "cooldown_days": 3
}
```

---

## T1-T3 Basic Soldier Orders

### order_guard_post

**Guard Post** - Stand watch at your assigned position.

| Property | Value |
|----------|-------|
| Duration | 2 days (8 phases) |
| Issuer | Sergeant |
| Primary Skill | Perception |
| Fatigue | Light (+5/day) |
| Injury Risk | Very Low (1%) |

**Blocks:**
```
Day 1: [routine] [routine] [slot] [slot!]
Day 2: [slot] [routine] [checkpoint] [resolve]
```

**Event Pool:**
- `guard_drunk_soldier` - Charm check to handle
- `guard_strange_noise` - Perception check to identify
- `guard_officer_inspection` - Discipline check
- `guard_caught_sneaking` - Perception check, catch them
- `guard_relieved_early` - Lucky, fatigue reduced
- `guard_double_shift` - Unlucky, extra fatigue

**Resolution:**
- Success: +8 Officer Rep, +5 Readiness, +15 Perception XP
- Partial: +3 Officer Rep, +8 Perception XP
- Failure: -10 Officer Rep, -5 Readiness

---

### order_camp_patrol

**Camp Patrol** - Walk the camp perimeter, maintain order.

| Property | Value |
|----------|-------|
| Duration | 2 days (8 phases) |
| Issuer | Sergeant |
| Primary Skill | Athletics |
| Fatigue | Medium (+10/day) |
| Injury Risk | Low (3%) |

**Blocks:**
```
Day 1: [routine] [slot] [routine] [slot]
Day 2: [slot] [routine] [routine] [resolve]
```

**Event Pool:**
- `patrol_lost_item` - Perception check, find it
- `patrol_soldier_argument` - Leadership check, break it up
- `patrol_suspicious_activity` - Perception check, investigate
- `patrol_officer_tags_along` - Social pressure event
- `patrol_shortcut` - Athletics check, save time
- `patrol_twisted_ankle` - Athletics check, avoid injury

**Resolution:**
- Success: +7 Officer Rep, +5 Soldier Rep, +20 Athletics XP
- Partial: +3 Officer Rep, +10 Athletics XP
- Failure: -8 Officer Rep, -3 Soldier Rep

---

### order_firewood_detail

**Firewood Detail** - Gather and chop wood for camp fires.

| Property | Value |
|----------|-------|
| Duration | 1 day (4 phases) |
| Issuer | Sergeant |
| Primary Skill | Athletics |
| Fatigue | High (+15/day) |
| Injury Risk | Medium (5%) |

**Blocks:**
```
Day 1: [routine] [slot] [slot] [resolve]
```

**Event Pool:**
- `firewood_axe_slip` - Athletics check, avoid injury
- `firewood_found_something` - Perception check, what is it?
- `firewood_wildlife` - Scouting check, animal encounter
- `firewood_work_song` - Morale event, join in or not
- `firewood_competition` - Athletics check, impress others

**Resolution:**
- Success: +6 Soldier Rep, +5 Morale, +15 Athletics XP
- Partial: +3 Soldier Rep, +8 Athletics XP
- Failure: -5 Officer Rep (didn't complete task)

---

### order_latrine_duty

**Latrine Duty** - Maintain the camp latrines. Punishment duty.

| Property | Value |
|----------|-------|
| Duration | 1 day (4 phases) |
| Issuer | Sergeant |
| Primary Skill | None |
| Fatigue | Medium (+10/day) |
| Injury Risk | Low (2%) |

**Blocks:**
```
Day 1: [routine] [routine] [routine] [resolve]
```

**Event Pool:**
- `latrine_overheard_rumor` - Intelligence gained
- `latrine_officer_complains` - Discipline check
- `latrine_soldier_gratitude` - Rep bonus event

**Notes:** Low-event order. Often assigned as punishment. Minimal rewards.

**Resolution:**
- Success: +3 Officer Rep, +5 Discipline XP
- Partial: +1 Officer Rep
- Failure: -5 Officer Rep, +5 Scrutiny

---

### order_equipment_cleaning

**Equipment Cleaning** - Clean and maintain company gear.

| Property | Value |
|----------|-------|
| Duration | 1 day (4 phases) |
| Issuer | Sergeant |
| Primary Skill | Crafting |
| Fatigue | Low (+5/day) |
| Injury Risk | Very Low (1%) |

**Blocks:**
```
Day 1: [routine] [slot] [routine] [resolve]
```

**Event Pool:**
- `cleaning_found_damage` - Crafting check, report or fix
- `cleaning_helped_comrade` - Social event
- `cleaning_officer_inspection` - Discipline check
- `cleaning_contraband_found` - Decision, report or ignore

**Resolution:**
- Success: +6 Officer Rep, +5 Equipment, +15 Crafting XP
- Partial: +3 Officer Rep, +8 Crafting XP
- Failure: -5 Officer Rep

---

### order_muster_inspection

**Muster Inspection** - Fall in for formal inspection.

| Property | Value |
|----------|-------|
| Duration | 1 day (4 phases) |
| Issuer | Sergeant |
| Primary Skill | None |
| Fatigue | Low (+3/day) |
| Injury Risk | None (0%) |

**Blocks:**
```
Day 1: [routine] [slot] [routine] [resolve]
```

**Event Pool:**
- `muster_kit_problem` - Decision, borrow or face consequences
- `muster_praised` - Rep bonus
- `muster_singled_out` - Scrutiny event

**Resolution:**
- Success: +6 Officer Rep, +4 Lord Rep
- Partial: +2 Officer Rep
- Failure: -8 Officer Rep, +10 Scrutiny

---

### order_sentry_duty

**Sentry Duty** - Guard a specific entrance or position.

| Property | Value |
|----------|-------|
| Duration | 1 day (4 phases) |
| Issuer | Sergeant |
| Primary Skill | Perception |
| Fatigue | Medium (+8/day) |
| Injury Risk | Low (2%), Night: Medium (4%) |

**Blocks:**
```
Day 1: [routine] [slot!] [routine] [resolve]
```

**Event Pool:**
- `sentry_movement_spotted` - Perception check
- `sentry_fell_asleep` - Fatigue check, consequences
- `sentry_challenged_approach` - Decision, challenge or let pass
- `sentry_relief_late` - Extra fatigue
- `sentry_heard_something` - Perception check

**Resolution:**
- Success: +7 Officer Rep, +6 Readiness, +15 Perception XP
- Partial: +3 Officer Rep, +8 Perception XP
- Failure: -12 Officer Rep, -5 Readiness

---

### order_march_formation

**March Formation** - Maintain position during army movement.

| Property | Value |
|----------|-------|
| Duration | Variable (depends on travel) |
| Issuer | Sergeant |
| Primary Skill | Athletics |
| Fatigue | High (+12/day) |
| Injury Risk | Low (3%) |

**Blocks (per day):**
```
Per day: [routine] [slot] [slot] [routine]
Final:   [routine] [routine] [checkpoint] [resolve]
```

**Event Pool:**
- `march_fell_behind` - Athletics check
- `march_helped_comrade` - Decision, slow down to help
- `march_equipment_problem` - Crafting check, quick fix
- `march_terrain_hazard` - Athletics check, avoid injury
- `march_foraging_opportunity` - Scouting check, find food

**Resolution:**
- Success: +5 Officer Rep, +5 Soldier Rep, +20 Athletics XP
- Partial: +2 Officer Rep, +10 Athletics XP
- Failure: -10 Officer Rep, "fell behind"

---

## T4-T6 Specialist Orders

### order_scout_route

**Scout the Route** - Reconnoiter ahead of the army.

| Property | Value |
|----------|-------|
| Duration | 3 days (12 phases) |
| Issuer | Captain |
| Primary Skill | Scouting |
| Tier Required | T4+ |
| Fatigue | Medium (+10/day) |
| Injury Risk | High (4%) |
| Gold Reward | 50g |

**Blocks:**
```
Day 1: [routine] [slot] [slot] [slot!]
Day 2: [slot] [slot!] [routine] [slot]
Day 3: [routine] [slot] [checkpoint] [resolve]
```

**Event Pool:**
- `scout_tracks_found` - Scouting check, identify threat
- `scout_enemy_patrol` - Tactics check, evade/report/observe
- `scout_terrain_obstacle` - Scouting check, alternate route
- `scout_injured_ankle` - Athletics check, push through
- `scout_shortcut_found` - Scouting check, faster completion
- `scout_lost_trail` - Scouting check, find it again
- `scout_ambush` - Combat or flee
- `scout_enemy_camp` - Decision, observe/report/withdraw

**Critical Failure:** Ambush, HP loss, possible capture

**Resolution:**
- Success: +8 Lord Rep, +10 Officer Rep, +50g, +25 Scouting XP, +5 Renown
- Partial: +4 Lord Rep, +5 Officer Rep, +25g, +15 Scouting XP
- Failure: -10 Lord Rep, -15 Officer Rep

---

### order_treat_wounded

**Treat the Wounded** - Care for injured soldiers.

| Property | Value |
|----------|-------|
| Duration | 2 days (8 phases) |
| Issuer | Captain |
| Primary Skill | Medicine |
| Tier Required | T4+ |
| Fatigue | Medium (+8/day) |
| Injury Risk | Low (2%) |

**Blocks:**
```
Day 1: [routine] [slot] [slot] [routine]
Day 2: [slot] [routine] [checkpoint] [resolve]
```

**Event Pool:**
- `treat_shortage` - Medicine check, improvise
- `treat_difficult_case` - Medicine check, save or lose
- `treat_infection_risk` - Medicine check, prevent spread
- `treat_grateful_soldier` - Rep bonus event
- `treat_officer_wounded` - High-stakes case
- `treat_contagion` - Medical Risk to self

**Resolution:**
- Success: +8 Officer Rep, +10 Soldier Rep, +5 Morale, +25 Medicine XP
- Partial: +4 Officer Rep, +5 Soldier Rep, +15 Medicine XP
- Failure: -8 Soldier Rep, -5 Morale

---

### order_lead_patrol

**Lead a Patrol** - Command a small patrol group.

| Property | Value |
|----------|-------|
| Duration | 3 days (12 phases) |
| Issuer | Captain |
| Primary Skill | Tactics |
| Secondary Skill | Leadership |
| Tier Required | T5+ |
| Fatigue | High (+12/day) |
| Injury Risk | Medium (3%) |
| Gold Reward | 75g |

**Blocks:**
```
Day 1: [routine] [slot] [slot!] [slot]
Day 2: [slot] [slot] [checkpoint] [slot!]
Day 3: [slot] [routine] [routine] [resolve]
```

**Event Pool:**
- `patrol_lead_soldier_behind` - Leadership check, wait or leave
- `patrol_lead_route_dispute` - Tactics check, who's right
- `patrol_lead_enemy_contact` - Tactics check, engage or evade
- `patrol_lead_morale_drop` - Leadership check, rally them
- `patrol_lead_terrain_hazard` - Scouting check, safe path
- `patrol_lead_man_injured` - Medicine check, field treatment
- `patrol_lead_ambush` - Combat trigger
- `patrol_lead_good_find` - Opportunity, supplies or intel

**Critical Failure:** Men lost, reputation crash

**Resolution:**
- Success: +10 Lord Rep, +12 Officer Rep, +75g, +30 Tactics XP, +5 Renown
- Partial: +5 Lord Rep, +6 Officer Rep, +40g, +18 Tactics XP
- Failure: -15 Lord Rep, -20 Officer Rep, possible troop loss

---

### order_repair_equipment

**Repair Equipment** - Fix and maintain company gear.

| Property | Value |
|----------|-------|
| Duration | 2 days (8 phases) |
| Issuer | Captain |
| Primary Skill | Crafting |
| Tier Required | T4+ |
| Fatigue | Low (+5/day) |
| Injury Risk | Low (2%) |

**Blocks:**
```
Day 1: [routine] [slot] [routine] [routine]
Day 2: [slot] [routine] [routine] [resolve]
```

**Event Pool:**
- `repair_missing_parts` - Crafting check, improvise
- `repair_discovered_sabotage` - Decision, report or investigate
- `repair_helped_smith` - Social event
- `repair_rush_job` - Crafting check under pressure
- `repair_quality_work` - Bonus if high skill

**Resolution:**
- Success: +8 Officer Rep, +8 Equipment, +25 Crafting XP
- Partial: +4 Officer Rep, +4 Equipment, +15 Crafting XP
- Failure: -6 Officer Rep

---

### order_forage_supplies

**Forage for Supplies** - Gather food and materials from the land.

| Property | Value |
|----------|-------|
| Duration | 2 days (8 phases) |
| Issuer | Captain |
| Primary Skill | Scouting |
| Tier Required | T4+ |
| Fatigue | High (+12/day) |
| Injury Risk | Medium (4%) |

**Blocks:**
```
Day 1: [routine] [slot] [slot!] [slot]
Day 2: [slot] [routine] [routine] [resolve]
```

**Event Pool:**
- `forage_rich_find` - Scouting check, bonus supplies
- `forage_wildlife` - Combat or flee
- `forage_farmer_encounter` - Charm check, buy/steal/leave
- `forage_spoiled_cache` - Bad luck, wasted effort
- `forage_enemy_territory` - Stealth check, avoid detection
- `forage_injured` - Athletics check, avoid injury

**Resolution:**
- Success: +6 Officer Rep, +15 Supplies, +20 Scouting XP
- Partial: +3 Officer Rep, +8 Supplies, +12 Scouting XP
- Failure: -5 Officer Rep, possible spoiled food (Medical Risk)

---

### order_train_recruits

**Train Recruits** - Drill new soldiers in basics.

| Property | Value |
|----------|-------|
| Duration | 2 days (8 phases) |
| Issuer | Captain |
| Primary Skill | Leadership |
| Secondary Skill | Athletics |
| Tier Required | T5+ |
| Fatigue | Medium (+8/day) |
| Injury Risk | Low (2%) |

**Blocks:**
```
Day 1: [routine] [slot] [slot] [routine]
Day 2: [slot] [routine] [checkpoint] [resolve]
```

**Event Pool:**
- `train_difficult_recruit` - Leadership check
- `train_injury_during` - Medicine check, handle it
- `train_impressive_performance` - Bonus rep
- `train_officer_observes` - Pressure event
- `train_recruit_question` - Decision, answer truthfully or lie

**Resolution:**
- Success: +8 Soldier Rep, +6 Officer Rep, +25 Leadership XP
- Partial: +4 Soldier Rep, +3 Officer Rep, +15 Leadership XP
- Failure: -5 Soldier Rep, -5 Officer Rep

**Note:** May unlock retinue candidate at T7+

---

### order_inspect_defenses

**Inspect Defenses** - Check fortifications and defensive positions.

| Property | Value |
|----------|-------|
| Duration | 2 days (8 phases) |
| Issuer | Captain |
| Primary Skill | Engineering |
| Tier Required | T4+ |
| Fatigue | Low (+5/day) |
| Injury Risk | Low (2%) |

**Blocks:**
```
Day 1: [routine] [slot] [routine] [slot]
Day 2: [routine] [routine] [routine] [resolve]
```

**Event Pool:**
- `inspect_found_weakness` - Decision, report or fix quietly
- `inspect_officer_dispute` - Charm check, handle politics
- `inspect_sabotage_discovered` - Major event
- `inspect_improvement_idea` - Engineering check, implement

**Context Note:** More events during siege.

**Resolution:**
- Success: +8 Officer Rep, +8 Readiness, +25 Engineering XP
- Partial: +4 Officer Rep, +4 Readiness, +15 Engineering XP
- Failure: -6 Officer Rep

---

### order_escort_duty

**Escort Duty** - Protect cargo or VIP during transport.

| Property | Value |
|----------|-------|
| Duration | 3 days (12 phases) |
| Issuer | Captain |
| Primary Skill | Athletics |
| Secondary Skill | Tactics |
| Tier Required | T4+ |
| Fatigue | High (+12/day) |
| Injury Risk | Medium (3%) |
| Gold Reward | 60g |

**Blocks:**
```
Day 1: [routine] [slot] [slot] [slot!]
Day 2: [slot!] [slot] [routine] [slot]
Day 3: [routine] [slot] [routine] [resolve]
```

**Event Pool:**
- `escort_bandit_scouts` - Perception check, spotted
- `escort_ambush` - Combat or evade
- `escort_difficult_terrain` - Athletics check
- `escort_cargo_problem` - Crafting check, quick fix
- `escort_vip_demands` - Charm check, handle noble
- `escort_shortcut_option` - Tactics check, risk vs reward

**Critical Failure:** Lost cargo, major rep damage

**Resolution:**
- Success: +10 Lord Rep, +8 Officer Rep, +60g, +25 Athletics XP
- Partial: +5 Lord Rep, +4 Officer Rep, +30g, +15 Athletics XP
- Failure: -15 Lord Rep, -12 Officer Rep, cargo lost

---

## Implementation Notes

### JSON File Structure

```
ModuleData/Enlisted/Orders/
├── orders_t1_t3.json         (8 basic orders)
├── orders_t4_t6.json         (8 specialist orders)
└── order_events/
    ├── guard_events.json       (order_guard_post)
    ├── sentry_events.json      (order_sentry_duty)
    ├── patrol_events.json      (order_camp_patrol)
    ├── patrol_lead_events.json (order_lead_patrol)
    ├── scout_events.json       (order_scout_route)
    ├── escort_events.json      (order_escort_duty)
    ├── firewood_events.json    (order_firewood_detail)
    ├── latrine_events.json     (order_latrine_duty)
    ├── cleaning_events.json    (order_equipment_cleaning)
    ├── muster_events.json      (order_muster_inspection)
    ├── march_events.json       (order_march_formation)
    ├── medical_events.json     (order_treat_wounded)
    ├── repair_events.json      (order_repair_equipment)
    ├── forage_events.json      (order_forage_supplies)
    ├── training_events.json    (order_train_recruits)
    └── defenses_events.json    (order_inspect_defenses)
```

See [Order Events Master](order-events-master.md) for complete event definitions and consequences.

### Localization

All text fields have corresponding ID fields for XML localization:
- `titleId` / `title`
- `descriptionId` / `description`
- `statusId` / `status` (per block)
- `textId` / `text` (in resolution)

Add strings to `ModuleData/Languages/enlisted_strings.xml`.

### Block Types Reference

| Type | Base Event Chance | Behavior |
|------|-------------------|----------|
| `routine` | 0% | Status text only |
| `slot` | 15% (modified by activity) | May fire event |
| `slot!` | 35% (modified by activity) | Likely fires event |
| `checkpoint` | 0% | Status + mini XP reward |
| `resolve` | N/A | Order completion |

### Fatigue Weight Reference

| Weight | Fatigue/Day | Night Bonus |
|--------|-------------|-------------|
| Light | +5 | +3 |
| Medium | +10 | +5 |
| Heavy | +15 | +8 |
| Physical | +20 | +10 |

---

**End of Document**

