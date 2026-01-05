# Enlisted Content Files Inventory

## JSON Content Folders
All content JSON files are located under `ModuleData/Enlisted/`.

**IMPORTANT:** When referencing JSON files or IDs in planning docs, use the `verify_file_exists_tool` and `list_json_event_ids_tool` to confirm they exist. Do NOT fabricate IDs.

### Decisions/ (Camp Opportunities & Decisions)
- `camp_opportunities.json` - 29 opportunities (opp_* IDs)
- `camp_decisions.json` - Decision tree content
- `decisions.json` - General decisions
- `medical_decisions.json` - Medical system decisions

**Camp Opportunity IDs (from camp_opportunities.json):**
- opp_weapon_drill, opp_card_game, opp_dice_game, opp_war_stories
- opp_rest_tent, opp_help_wounded, opp_equipment_maintenance
- opp_mess_tent, opp_letter_home, opp_mending_clothes
- opp_market_browse, opp_tavern_visit, opp_volunteer_duty
- opp_campfire_songs, opp_extra_rations, opp_pray_shrine
- opp_wrestling_match, opp_archery_practice, opp_horse_grooming
- opp_foraging_trip, opp_fishing_trip, opp_hunting_trip
- opp_night_watch, opp_morning_exercises, opp_swimming
- opp_gather_firewood, opp_cook_meal, opp_repair_tent
- opp_laundry, opp_sharpen_weapons

### Events/ (Event Definitions)
- `events_escalation_thresholds.json` - Reputation milestone events
- `events_promotion.json` - Tier promotion events
- `events_pay_*.json` - Pay day events (loyal, tension, mutiny)
- `events_retinue.json` - Commander retinue events
- `events_baggage_stowage.json` - Baggage system events
- `illness_onset.json` - Sickness events
- `incidents_*.json` - Context-specific incidents (battle, siege, town, village, waiting, leaving, retinue)
- `schema_version.json` - Schema versioning

### Orders/ (Order System)
- `orders_t1_t3.json` - Tier 1-3 orders
- `orders_t4_t6.json` - Tier 4-6 orders  
- `orders_t7_t9.json` - Tier 7-9 orders
- `order_events/*.json` - 16 order event files (one per order type)

**Order Event Files:**
- camp_patrol_events.json, equipment_cleaning_events.json
- escort_duty_events.json, firewood_detail_events.json
- forage_supplies_events.json, guard_post_events.json
- inspect_defenses_events.json, latrine_duty_events.json
- lead_patrol_events.json, march_formation_events.json
- muster_inspection_events.json, repair_equipment_events.json
- scout_route_events.json, sentry_duty_events.json
- train_recruits_events.json, treat_wounded_events.json

### Config/ (Configuration Files)
- `enlisted_config.json` - Main mod configuration
- `progression_config.json` - Tier progression thresholds
- `simulation_config.json` - Company simulation settings
- `camp_schedule.json` - Day phase schedules
- `orchestrator_overrides.json` - Content pacing overrides
- `settings.json` - Debug/runtime settings
- `baggage_config.json`, `retinue_config.json`, etc.

### Dialogue/ (QM Dialogue)
- `qm_*.json` - Quartermaster dialogue trees

### Other
- `Conditions/condition_defs.json` - Condition definitions
- `Content/injuries.json` - Injury definitions

## Verification Workflow
Before including file paths or event IDs in planning documents:
1. Use `verify_file_exists_tool("path/to/file.cs")` for C# files
2. Use `list_json_event_ids_tool("Decisions")` to get valid opportunity IDs
3. Use `list_json_event_ids_tool("Events")` to get valid event IDs
4. Use `list_json_event_ids_tool("Orders")` to get valid order event IDs
