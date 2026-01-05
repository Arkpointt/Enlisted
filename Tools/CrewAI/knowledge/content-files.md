# Enlisted Content Files Inventory

## JSON Content Folders
All content JSON files are located under `ModuleData/Enlisted/`.

**CRITICAL:** This file is a REFERENCE ONLY. It may be out of date.
When referencing JSON files or IDs in planning docs:
1. ALWAYS use `verify_file_exists_tool` to confirm file paths exist
2. ALWAYS use `list_json_event_ids_tool` to get the current list of IDs
3. NEVER assume an ID exists without verification - IDs change frequently

### Decisions/ (Camp Opportunities & Decisions)
- `camp_opportunities.json` - 36+ opportunities (opp_* IDs) - USE TOOL TO GET CURRENT LIST
- `camp_decisions.json` - Decision tree content
- `decisions.json` - General decisions
- `medical_decisions.json` - Medical system decisions

**Camp Opportunity IDs:** DO NOT use this list - call `list_json_event_ids_tool("Decisions")` instead.
This static list is for reference only and may be incomplete:

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
