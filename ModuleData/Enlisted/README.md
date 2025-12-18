# ModuleData/Enlisted (Blueprint)

This folder holds the **shipping, editable data** for Enlisted.

Principles:
- **Content is data**: where possible, gameplay knobs and story/action content live in JSON, not hardcoded.
- **Localization lives in XML**: player-facing text is in `ModuleData/Languages/enlisted_strings.xml` and referenced by `{=id}`.

## What’s here (shipping)

### Core configs (JSON)
| File | Purpose |
|------|---------|
| `settings.json` | Logging levels + runtime behavior flags |
| `enlisted_config.json` | Feature flags + tuning (camp life, escalation, personas, conditions, activities) plus finance/retirement/gameplay |
| `progression_config.json` | Tier thresholds + rank names |
| `duties_system.json` | Duties/professions + formation training tuning |
| `equipment_kits.json` | Troop equipment pools for Quartermaster |
| `equipment_pricing.json` | Pricing multipliers and tuning |
| `lances_config.json` | Lance styles, lance names, and culture mapping |

### Content folders (JSON)
| Path | Purpose |
|------|---------|
| `StoryPacks/LanceLife/*.json` | Lance Life stories (data-driven incidents) |
| `LancePersonas/name_pools.json` | Name pools for text-only lance personas |
| `Conditions/condition_defs.json` | Player condition definitions (injury/illness) |
| `Activities/activities.json` | Camp Activities menu actions (data-driven XP/fatigue) |
| `Events/*.json` | Lance Life Events catalog packs (delivery via menu/inquiry/incident) |
| `Events/schema_version.json` | Schema marker for Events packs |

### Event Packs
| File | Purpose |
|------|---------|
| `events_general.json` | General camp and training events |
| `events_onboarding.json` | New recruit onboarding chain |
| `events_training.json` | Formation training events |
| `events_pay_tension.json` | Pay grumbling, theft, confrontation, mutiny brewing |
| `events_pay_loyal.json` | Loyal path missions (collect debts, escort, raid) |
| `events_pay_mutiny.json` | Desertion planning, mutiny resolution chains |

## Where behavior is documented
Docs are the source-of-truth for feature behavior (not this file):
- `docs/Features/index.md`
