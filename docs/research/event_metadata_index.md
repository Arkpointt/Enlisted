# Event Metadata Index

Parseable metadata for all Lance Life events. Use with content documents for full story text.

---

## Quick Reference

**Note:** All events below are for **Tier 1-6 (Enlisted and Officer)** only. T7-9 Commander events are not yet implemented.

| Category | Count | Tiers | Delivery | Menu | Content Doc |
|----------|-------|-------|----------|------|-------------|
| Duty | 50 | T1-6 | Automatic | None | lance_life_events_content_library.md |
| Training | 16 | T1-6 | Player-Initiated | enlisted_activities | lance_life_events_content_library.md |
| General | 18 | T1-6 | Automatic | None | lance_life_events_content_library.md |
| Onboarding | 9 | T1-9* | Automatic | None | onboarding_story_pack.md |
| Escalation | 16 | T1-6 | Automatic | None | escalation_threshold_events.md |

*Onboarding has 3 events each for Enlisted (T1-4), Officer (T5-6), and Commander (T7-9) tracks.

---

## Metadata Schema

```json
{
  "id": "event_id",
  "category": "duty|training|general|onboarding|escalation",
  "content_doc": "document containing story content",
  
  "delivery": {
    "method": "automatic|player_initiated",
    "menu": "menu_id or null",
    "menu_section": "training|tasks|social or null"
  },
  
  "triggers": {
    "all": ["conditions that must ALL be true"],
    "any": ["conditions where ANY can be true"],
    "time_of_day": ["valid times"],
    "escalation_requirements": { "track": { "min": N, "max": N } }
  },
  
  "requirements": {
    "duty": "duty_id or any",
    "formation": "infantry|cavalry|archer|naval|any",
    "tier": { "min": 1, "max": 9 }
  },
  
  "timing": {
    "cooldown_days": N,
    "priority": "normal|high|critical",
    "one_time": true|false
  },
  
  "escalation_effects": {
    "heat": { "min": N, "max": N },
    "discipline": { "min": N, "max": N },
    "lance_reputation": { "min": N, "max": N },
    "medical_risk": { "min": N, "max": N }
  },
  
  "skill_rewards": ["skill_names"]
}
```

---

## Duty Events (50)

**Tiers:** T1-6 only | **Delivery:** Automatic | **Menu:** None | **Base triggers:** `["is_enlisted", "ai_safe", "has_duty:{duty_id}", "tier <= 6"]`

### Quartermaster (5)

| ID | Time | Additional Triggers | Cooldown | Escalation Effects |
|----|------|---------------------|----------|-------------------|
| qm_supply_inventory | morning, afternoon | entered_settlement OR weekly_tick | 7 | Heat 0/+2 |
| qm_merchant_negotiation | any | in_settlement, logistics_strain > 30 | 5 | Discipline 0/+1 |
| qm_spoiled_supplies | morning | not_in_settlement, days_from_town > 5 | 7 | Heat 0/+2, Discipline 0/+1 |
| qm_requisition_request | any | after_battle OR weekly_tick | 5 | Heat 0/+2, Lance Rep -3/+3 |
| qm_accounting_discrepancy | any | heat >= 2 | 10 | Heat -3/+2 |

### Scout (5)

| ID | Time | Additional Triggers | Cooldown | Escalation Effects |
|----|------|---------------------|----------|-------------------|
| scout_terrain_recon | dawn-afternoon | before_battle OR army_moving | 3 | Lance Rep -2/+3, Discipline 0/+1 |
| scout_enemy_contact | dawn-dusk | enemy_nearby | 2 | Lance Rep -5/+5, Discipline 0/+2 |
| scout_night_patrol | night, late_night | not_in_settlement | 2 | Discipline 0/+2, Lance Rep -2/+2 |
| scout_route_finding | dawn, morning | army_moving | 3 | Lance Rep -3/+3 |
| scout_map_update | any | entered_new_region OR weekly_tick | 7 | Heat 0/+1 |

### Field Medic (5)

| ID | Time | Additional Triggers | Cooldown | Escalation Effects |
|----|------|---------------------|----------|-------------------|
| medic_treating_wounded | morning-evening | wounded_in_camp | 1 | Lance Rep 0/+5 |
| medic_disease_outbreak | any | camp_crowded OR days_from_town > 7 | 14 | Medical Risk 0/+1, Lance Rep -2/+5 |
| medic_surgical_assist | morning-evening | after_battle | 3 | Lance Rep -2/+5 |
| medic_medicine_shortage | any | not_in_settlement, logistics_strain > 40 | 7 | Heat 0/+2, Lance Rep -3/+3 |
| medic_lance_mate_injury | any | lance_mate_injured OR after_battle | 3 | Lance Rep -5/+10 |

### Messenger (5)

| ID | Time | Additional Triggers | Cooldown | Escalation Effects |
|----|------|---------------------|----------|-------------------|
| msg_urgent_dispatch | any | before_battle OR army_moving | 2 | Discipline 0/+2, Lance Rep -2/+3 |
| msg_enemy_lines | any | enemy_nearby | 5 | Discipline 0/+3, Lance Rep -5/+5 |
| msg_verbal_orders | any | before_battle | 3 | Discipline 0/+3, Lance Rep -3/+2 |
| msg_intercepted | any | enemy_nearby, after_patrol | 7 | Heat 0/+2, Discipline 0/+2, Lance Rep -3/+5 |
| msg_return_report | any | completed_delivery OR weekly_tick | 5 | Heat 0/+1, Lance Rep 0/+3 |

### Armorer (5)

| ID | Time | Additional Triggers | Cooldown | Escalation Effects |
|----|------|---------------------|----------|-------------------|
| arm_equipment_inspection | any | weekly_tick OR before_battle | 5 | Discipline 0/+1, Lance Rep -2/+3 |
| arm_battle_repairs | morning, afternoon | after_battle | 2 | Lance Rep 0/+5 |
| arm_material_shortage | any | not_in_settlement, logistics_strain > 50 | 7 | Heat 0/+2, Lance Rep 0/+3 |
| arm_weapon_modification | any | weekly_tick OR soldier_request | 5 | Discipline 0/+1, Lance Rep -2/+5 |
| arm_captured_equipment | any | after_battle | 5 | Heat 0/+3, Lance Rep -2/+3 |

### Runner (5)

| ID | Time | Additional Triggers | Cooldown | Escalation Effects |
|----|------|---------------------|----------|-------------------|
| run_supply_run | any | daily_tick OR supply_request | 2 | Lance Rep 0/+3 |
| run_battle_runner | any | during_battle OR after_battle | 1 | Discipline 0/+2, Lance Rep -3/+5 |
| run_night_summons | night, late_night | — | 3 | Discipline 0/+2 |
| run_camp_coordination | morning, afternoon | camp_established | 3 | Lance Rep 0/+2 |
| run_water_run | any | not_in_settlement, hot_weather | 2 | Heat 0/+1, Lance Rep -2/+5 |

### Lookout (5)

| ID | Time | Additional Triggers | Cooldown | Escalation Effects |
|----|------|---------------------|----------|-------------------|
| look_night_watch | night, late_night | not_in_settlement, camp_established | 2 | Discipline 0/+2, Lance Rep -2/+2 |
| look_movement_spotted | any | on_watch, enemy_nearby | 3 | Discipline 0/+1, Lance Rep -5/+5 |
| look_signal_fire | night | army_split | 7 | Discipline 0/+2, Lance Rep -3/+3 |
| look_weather_watch | dawn, morning | army_planning_movement | 5 | Lance Rep -2/+3 |
| look_relief_late | any | on_watch | 5 | Discipline 0/+2, Lance Rep -2/+5 |

### Engineer (5)

| ID | Time | Additional Triggers | Cooldown | Escalation Effects |
|----|------|---------------------|----------|-------------------|
| eng_fortification_work | morning, afternoon | camp_established | 3 | Lance Rep 0/+3 |
| eng_bridge_assessment | any | army_moving, river_crossing | 5 | Discipline 0/+2, Lance Rep -3/+3 |
| eng_siege_preparation | any | siege_imminent OR siege_ongoing | 5 | Lance Rep 0/+5 |
| eng_equipment_repair | any | after_battle OR after_siege | 3 | Lance Rep 0/+3 |
| eng_mining_operation | any | siege_ongoing | 7 | Lance Rep -2/+5 |

### Boatswain — Naval (5)

| ID | Time | Additional Triggers | Cooldown | Escalation Effects |
|----|------|---------------------|----------|-------------------|
| boat_deck_management | morning, afternoon | at_sea | 2 | Lance Rep 0/+3 |
| boat_storm_rigging | any | at_sea, storm_coming | 3 | Lance Rep -3/+5 |
| boat_supply_rationing | morning | at_sea, voyage_long, logistics > 50 | 5 | Lance Rep -5/+3 |
| boat_man_overboard | any | at_sea | 7 | Lance Rep -10/+10 |
| boat_crew_dispute | evening | at_sea, morale_low | 5 | Discipline 0/+2, Lance Rep -3/+5 |

### Navigator — Naval (5)

| ID | Time | Additional Triggers | Cooldown | Escalation Effects |
|----|------|---------------------|----------|-------------------|
| nav_course_setting | dawn, morning | at_sea, voyage_start | 5 | Lance Rep -3/+3 |
| nav_dead_reckoning | any | at_sea, overcast OR fog | 3 | Lance Rep -5/+3 |
| nav_reef_warning | any | at_sea, near_coast | 5 | Discipline 0/+2, Lance Rep -10/+5 |
| nav_star_navigation | night, late_night | at_sea, clear_sky | 3 | Lance Rep 0/+2 |
| nav_landfall | any | at_sea, approaching_land | 7 | Lance Rep -3/+5 |

---

## Training Events (16)

**Tiers:** T1-6 only | **Delivery:** Player-Initiated | **Menu:** enlisted_activities | **Section:** training

### Infantry (4)

| ID | Formation | Time | Fatigue | Cooldown | Injury Risk | Skills |
|----|-----------|------|---------|----------|-------------|--------|
| inf_train_shield_wall | infantry | morning, afternoon | 2 | 2 | 10% minor | polearm, one_handed, athletics |
| inf_train_sparring | infantry | morning, afternoon | 2 | 1 | 15-20% minor | one_handed, athletics, tactics |
| inf_train_march | infantry | morning | 3 | 2 | 10% minor | athletics, polearm, leadership |
| inf_train_twohanded | infantry | morning, afternoon | 2 | 2 | 10% minor | two_handed, athletics, tactics |

### Cavalry (4)

| ID | Formation | Time | Fatigue | Cooldown | Injury Risk | Skills |
|----|-----------|------|---------|----------|-------------|--------|
| cav_train_horse_rotation | cavalry | morning | 2 | 1 | 10% minor | riding, charm, leadership |
| cav_train_mounted_combat | cavalry | morning, afternoon | 2 | 2 | 15% minor | riding, polearm, one_handed |
| cav_train_charge | cavalry | morning, afternoon | 2 | 2 | 15-20% min-mod | riding, polearm, leadership |
| cav_train_skirmish | cavalry | morning, afternoon | 2 | 2 | None | riding, throwing, bow, scouting |

### Archer (4)

| ID | Formation | Time | Fatigue | Cooldown | Injury Risk | Skills |
|----|-----------|------|---------|----------|-------------|--------|
| arch_train_target | archer | morning, afternoon | 1 | 1 | None | bow, crossbow |
| arch_train_volley | archer | morning, afternoon | 2 | 2 | None | bow, tactics, leadership |
| arch_train_moving | archer | morning, afternoon | 2 | 2 | None | bow, scouting, athletics |
| arch_train_hunting | archer | morning, afternoon | 3 | 3 | 10% minor | bow, scouting, athletics |

### Naval (4)

| ID | Formation | Time | Fatigue | Cooldown | Injury Risk | Skills |
|----|-----------|------|---------|----------|-------------|--------|
| nav_train_boarding | naval | any (at_sea) | 2 | 2 | 10% minor | mariner, athletics, one_handed |
| nav_train_rigging | naval | any (at_sea) | 2 | 2 | 10% min-mod | mariner, athletics, leadership |
| nav_train_navigation | naval | evening, night | 1 | 2 | None | shipmaster, scouting, steward |
| nav_train_storm_drill | naval | any (at_sea) | 3 | 3 | 10% minor | mariner, steward, athletics |

---

## General Events (18)

**Tiers:** T1-6 only | **Delivery:** Automatic | **Menu:** None | **Base triggers:** `["is_enlisted", "ai_safe", "tier <= 6"]`

| ID | Time | Additional Triggers | Cooldown | Escalation Effects |
|----|------|---------------------|----------|-------------------|
| gen_dawn_muster | dawn | daily_tick | 3 | Discipline 0/+1, Lance Rep -1/+2 |
| gen_dawn_cold_start | dawn | cold_weather, not_in_settlement | 5 | Lance Rep -2/+3 |
| gen_dawn_early_duty | dawn | random_chance | 5 | Discipline 0/+1, Lance Rep 0/+2 |
| gen_day_drill_summons | morning, afternoon | random_chance | 3 | Lance Rep -2/+3 |
| gen_day_officer_inspection | morning, afternoon | random_chance OR weekly | 7 | Discipline 0/+2, Lance Rep -2/+2 |
| gen_day_lance_mate_request | morning, afternoon | random_chance | 5 | Lance Rep -3/+5 |
| gen_evening_fire_circle | evening | — | 3 | Lance Rep -2/+5 |
| gen_evening_letter_home | evening | in_settlement OR random | 14 | — |
| gen_evening_gambling | evening, dusk | — | 5 | Heat 0/+1, Lance Rep -3/+5 |
| gen_dusk_sunset_watch | dusk | — | 7 | Lance Rep 0/+3 |
| gen_dusk_veteran_story | dusk, evening | — | 7 | Lance Rep 0/+3 |
| gen_dusk_supply_arrival | dusk, evening | supply_wagon OR in_settlement | 7 | Heat 0/+2, Lance Rep -2/+3 |
| gen_night_disturbance | night | not_in_settlement | 5 | Discipline 0/+2, Lance Rep -2/+3 |
| gen_night_confession | night, late_night | random_chance | 14 | Lance Rep 0/+10 |
| gen_night_cant_sleep | night, late_night | after_battle OR random | 7 | — |
| gen_late_night_emergency | late_night | random_chance | 10 | Discipline 0/+1, Lance Rep -3/+5 |
| gen_late_night_nightmare | late_night | after_battle OR random | 14 | Lance Rep 0/+3 |
| gen_late_night_watch_end | late_night | not_in_settlement | 5 | Discipline 0/+1, Lance Rep -1/+2 |

---

## Onboarding Events (9)

All onboarding: **Delivery:** Automatic | **Menu:** None | **Priority:** High | **One-time:** Yes

**Content doc:** onboarding_story_pack.md

### Enlisted Track (Tier 1-4)

| ID | Stage | Time | Trigger | Variants |
|----|-------|------|---------|----------|
| enlisted_onboard_01_meet_lance | 1 | any | days_since_enlistment < 1 | first_time, transfer, return |
| enlisted_onboard_02_fire_circle | 2 | evening, dusk | onboarding_stage_2 | first_time, transfer, return |
| enlisted_onboard_03_prove_yourself | 3 | dawn, morning | onboarding_stage_3 | first_time, transfer, return |

### Officer Track (Tier 5-6)

| ID | Stage | Time | Trigger | Variants |
|----|-------|------|---------|----------|
| officer_onboard_01_shoulder_tabs | 1 | any | days_since_enlistment < 1 | first_time, transfer, return |
| officer_onboard_02_first_command | 2 | morning, afternoon | onboarding_stage_2 | all |
| officer_onboard_03_tested | 3 | morning-evening | onboarding_stage_3 | all |

### Commander Track (Tier 7-9)

| ID | Stage | Time | Trigger | Variants |
|----|-------|------|---------|----------|
| commander_onboard_01_commission | 1 | any | days_since_enlistment < 1 | first_time, transfer, return |
| commander_onboard_02_meet_troops | 2 | morning, afternoon | onboarding_stage_2 | all |
| commander_onboard_03_first_blood | 3 | any | after_battle OR days >= 2 | all |

---

## Escalation Threshold Events (16)

**Tiers:** T1-6 only | **Delivery:** Automatic | **Menu:** None

**Content doc:** escalation_threshold_events.md

### Heat Track

| ID | Threshold | Priority | Cooldown | Effects (min/max) |
|----|-----------|----------|----------|-------------------|
| heat_warning | 3 (Watched) | high | 7 | Heat -2/+1 |
| heat_shakedown | 5 (Shakedown) | high | 7 | Heat -3/+3, Discipline 0/+3 |
| heat_audit | 7 (Audit) | high | 7 | Heat -5/+3, Discipline 0/+4 |
| heat_exposed | 10 (Exposed) | critical | 14 | Heat reset, Discipline 0/+7 |

### Discipline Track

| ID | Threshold | Priority | Cooldown | Effects (min/max) |
|----|-----------|----------|----------|-------------------|
| discipline_warning | 2 (On Notice) | high | 7 | Discipline -1/+1 |
| discipline_extra_duty | 3 (Extra Duty) | high | 7 | Discipline -3/+2 |
| discipline_hearing | 5 (Hearing) | high | 14 | Discipline -4/+2, Lance Rep -15/+15 |
| discipline_blocked | 7 (Blocked) | high | 14 | Discipline -2/+1 |
| discipline_discharge | 10 (Discharge) | critical | 14 | Discharge or Discipline reset |

### Lance Reputation Track

| ID | Threshold | Priority | Cooldown | Effects (min/max) |
|----|-----------|----------|----------|-------------------|
| lance_trusted | +20 (Trusted) | high | 14 | Lance Rep -10/+15 |
| lance_bonded | +40 (Bonded) | high | 14 | Lance Rep 0/+15, Heat -3/0 |
| lance_isolated | -20 (Disliked) | high | 14 | Lance Rep -5/+5 |
| lance_sabotage | -40 (Hostile) | critical | 14 | Lance Rep -5/+5, Discipline 0/+2 |

### Medical Risk Track

| ID | Threshold | Priority | Cooldown | Effects (min/max) |
|----|-----------|----------|----------|-------------------|
| medical_worsening | 3 (Worsening) | high | 3 | Medical Risk -3/+2 |
| medical_complication | 4 (Complication) | high | 3 | Medical Risk -4/+1 |
| medical_emergency | 5 (Emergency) | critical | 7 | Medical Risk reset |

---

## Trigger Reference

| Condition | Meaning |
|-----------|---------|
| `is_enlisted` | Player is enlisted |
| `ai_safe` | Not in battle/encounter/dialog |
| `has_duty:{id}` | Player has duty |
| `in_settlement` / `not_in_settlement` | Location |
| `before_battle` / `after_battle` | Battle timing |
| `at_sea` | Naval context |
| `camp_established` | Army camped |
| `wounded_in_camp` | Wounded present |
| `enemy_nearby` | Enemies close |
| `logistics_strain > N` | Supply pressure |
| `onboarding_stage_X` | Onboarding progress |

## Time of Day

| Value | Hours |
|-------|-------|
| dawn | 5:00-7:00 |
| morning | 7:00-12:00 |
| afternoon | 12:00-17:00 |
| evening | 17:00-20:00 |
| dusk | 20:00-21:00 |
| night | 21:00-1:00 |
| late_night | 1:00-5:00 |

---

*Use with content documents for full story text.*
