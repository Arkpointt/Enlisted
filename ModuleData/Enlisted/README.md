# ModuleData/Enlisted

Data files for the Enlisted mod. All gameplay content and configuration.

**Spec Document:** `docs/Content/content-index.md`

---

## Principles

1. **Content is data** - Gameplay tuning and narrative content live in JSON, not hardcoded
2. **Localization in XML** - Player-facing text in `Languages/enlisted_strings.xml`, referenced by `{=stringId}`
3. **Docs are truth** - Feature behavior documented in `docs/Features/`, content catalog in `docs/Content/`

---

## Config Files

| File | Purpose |
|------|---------|
| `enlisted_config.json` | Master feature flags, version info, gameplay tuning |
| `settings.json` | Logging levels, debug flags |
| `progression_config.json` | Tier XP thresholds, culture-specific rank names |
| `strategic_context_config.json` | War stance definitions, strategic contexts |
| `menu_config.json` | Game menu definitions (future use) |
| `retinue_config.json` | Retinue capacity rules, trickle rates, economics |
| `equipment_pricing.json` | Quartermaster pricing, retirement costs |

---

## Content Folders

### Events/
Narrative events triggered by context, role, or player action.

| File | Content | Count |
|------|---------|-------|
| `events_general.json` | Universal camp/march events | ~20 |
| `events_training.json` | Training decisions and outcomes | ~10 |
| `events_onboarding.json` | New recruit introduction chain | ~8 |
| `events_promotion.json` | Promotion events | ~5 |
| `events_escalation_thresholds.json` | Scrutiny/Discipline/Medical threshold events | 14 |
| `events_pay.json` | Pay tension, loyalty, mutiny events | ~15 |
| `camp_events.json` | Camp context events | ~8 |
| `muster_events.json` | Muster cycle events | ~6 |

**Duty Events (10 files):**
| File | Role |
|------|------|
| `events_duty_armorer.json` | Armorer duty |
| `events_duty_scout.json` | Scout duty |
| `events_duty_field_medic.json` | Field Medic duty |
| `events_duty_quartermaster.json` | Quartermaster duty |
| `events_duty_engineer.json` | Engineer duty |
| `events_duty_lookout.json` | Lookout duty |
| `events_duty_messenger.json` | Messenger duty |
| `events_duty_runner.json` | Runner duty |
| `events_duty_boatswain.json` | Boatswain duty (War Sails) |
| `events_duty_navigator.json` | Navigator duty (War Sails) |

**Map Incidents:**
| File | Trigger |
|------|---------|
| `incidents_battle.json` | After battle ends |
| `incidents_settlement.json` | Entering/leaving settlements |

### Orders/
Military directives from chain of command. Loaded dynamically from JSON at runtime.

| File | Tier Range | Count |
|------|------------|-------|
| `orders_t1_t3.json` | Basic Soldier (T1-T3) | 6 |
| `orders_t4_t6.json` | Specialist (T4-T6) | 6 |
| `orders_t7_t9.json` | Leadership (T7-T9) | 5 |

**Order Outcome Types:**
- `success` - Player completed the order successfully
- `failure` - Player failed the order (normal failure)
- `critical_failure` - Player failed badly (bottom 15% of failure zone)
- `decline` - Player refused the order

**Outcome Effect Fields:**
| Field | Type | Description |
|-------|------|-------------|
| `skill_xp` | object | Skill XP rewards (e.g., `{"Scouting": 50}`) |
| `trait_xp` | object | Trait XP rewards (e.g., `{"Valor": 10}`) |
| `reputation` | object | Reputation changes (`lord`, `officer`, `soldier`) |
| `company_needs` | object | Company need changes (e.g., `{"Readiness": -10}`) |
| `escalation` | object | Escalation changes (`scrutiny`, `discipline`) |
| `medical_risk` | int | Medical risk escalation delta (illness/disease exposure) |
| `denars` | int | Gold reward/penalty |
| `renown` | int | Renown reward/penalty |
| `hp_loss` | int | Player HP damage (wounds) |
| `troop_loss` | object | Troop casualties (`{"min": 1, "max": 3}`) |
| `text` | string | Narrative feedback shown to player |

### Decisions/
Player-initiated actions from camp menu.

| File | Content |
|------|---------|
| `decisions.json` | All 34 player decisions |

### Data/
Static reference data.

| Path | Content |
|------|---------|
| `Activities/activities.json` | Camp activity definitions |
| `Conditions/condition_defs.json` | Player condition definitions (injury, illness) |

---

## Localization

All player-facing strings are in `../Languages/enlisted_strings.xml`.

**String ID Prefixes:**
| Prefix | Usage |
|--------|-------|
| `evt_*` | Event text |
| `dec_*` | Decision text |
| `ord_*` | Order text |
| `inc_*` | Map incident text |
| `ui_*` | UI labels |

---

## Schema

Event files use schema version 2. See `Events/schema_version.json`.

**Event Structure:**
```json
{
  "event_id": "evt_example",
  "titleId": "evt_example_title",
  "title": "Fallback Title",
  "setupId": "evt_example_setup",
  "setup": "Fallback description text.",
  "requirements": { "tier_min": 1, "context": ["camp"] },
  "options": [
    {
      "option_id": "opt_1",
      "textId": "evt_example_opt1",
      "text": "Option text",
      "effects": { "soldierRep": 5, "officerRep": 0 }
    }
  ]
}
```

**Effect Fields:**
| Field | Type | Description |
|-------|------|-------------|
| `soldierRep` | int | Soldier reputation change |
| `officerRep` | int | Officer reputation change |
| `lordRep` | int | Lord reputation change |
| `scrutiny` | int | Scrutiny meter change |
| `discipline` | int | Discipline meter change |
| `gold` | int | Gold change |
| `xp` | int | XP reward |

---

## Content Counts

| Category | Count | Spec Reference |
|----------|-------|----------------|
| Orders | 17 | content-index.md §Orders |
| Decisions | 34 | content-index.md §Decisions |
| Events | 68+ | content-index.md §Events |
| Map Incidents | 45 | content-index.md §Map Incidents |

---

## Adding Content

1. Add event/order/decision to appropriate JSON file
2. Add all strings to `enlisted_strings.xml`
3. Update `docs/Content/content-index.md` with the new content
4. Test in-game

See `docs/Features/Content/content-system-architecture.md` for full details.
