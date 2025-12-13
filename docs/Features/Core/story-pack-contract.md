# Content Pack Contract — Lance Life Story Packs

This document defines the **non-negotiable contract** for adding or changing Lance Life story content without touching code.

If you follow this contract:
- Stories are easy to add/remove.
- Content is organized (multiple small packs).
- Everything is localizable.
- Bad content fails safely (skipped, not crash).

## Status (important)
The shipping **Lance Life Stories (StoryPacks)** implementation loads story packs from:
- `ModuleData/Enlisted/StoryPacks/LanceLife/*.json`

Legacy note:
- `ModuleData/Enlisted/lance_stories.json` is considered legacy and is no longer the primary content path.

Important distinction:
- This contract applies to the **StoryPacks** system gated by `lance_life.enabled`.
- The newer **Lance Life Events** system uses a different schema and lives under `ModuleData/Enlisted/Events/*.json` (see `docs/Features/Technical/lance_life_schemas.md`).

## Folder layout (required)
All Lance Life story content lives here:
- `ModuleData/Enlisted/StoryPacks/LanceLife/*.json`

Recommended organization (one theme per file):
- `core.json`
- `training.json`
- `logistics.json`
- `morale.json`
- `corruption.json`
- `discipline.json`
- `medical.json`
- `escalation_thresholds.json` (Phase 4: queued threshold consequence events)

## Pack schema (required)
Each `.json` file is a **pack**:

```json
{
  "schemaVersion": 1,
  "packId": "corruption",
  "stories": [ /* array of story objects */ ]
}
```

Rules:
- `schemaVersion` is required.
- `packId` is required and must be unique across all packs.
- If a file fails to parse, the pack is skipped and a warning is logged.

## Story schema (required)

```json
{
  "id": "corruption.ledger_skim_v1",

  "category": "corruption",
  "tags": ["quartermaster", "town_only"],

  "titleId": "ll_story_corruption_ledger_skim_title",
  "bodyId": "ll_story_corruption_ledger_skim_body",

  "title": "The Ledger and the Smile",
  "body": "Fallback English text…",

  "tierMin": 3,
  "tierMax": 6,
  "requireFinalLance": true,

  "cooldownDays": 8,
  "maxPerTerm": 2,

  "triggers": {
    "all": ["entered_town", "pay_tension_high"],
    "any": []
  },

  "options": [ /* 2–4 options */ ]
}
```

Rules:
- `id` is required and must be globally unique across all packs.
- `category` is required (used for balancing and enable/disable controls).
- `titleId` / `bodyId` are required for translation. `title` / `body` are fallback English.
- `options` must be 2–4 entries.
- At least one option must be **safe** (see option schema).

## Option schema (required)

```json
{
  "id": "refuse_clean",

  "textId": "ll_story_corruption_ledger_skim_opt_refuse_text",
  "hintId": "ll_story_corruption_ledger_skim_opt_refuse_hint",

  "text": "Refuse. Pay the posted price.",
  "hint": "No heat. No favors owed.",

  "risk": "safe",

  "costs": { "fatigue": 0, "gold": 0, "heat": 0, "discipline": 0 },
  "rewards": { "skillXp": { "Charm": 10 }, "gold": 0, "fatigueRelief": 0 },

  "effects": { "heat": -1, "discipline": 0, "lance_reputation": 0, "medical_risk": 0 },

  "injury": {
    "chance": 0.10,
    "types": ["sprain", "bruise"],
    "severity_weights": { "minor": 0.80, "moderate": 0.20 },
    "location_weights": { "arm": 0.50, "leg": 0.50 }
  },
  "illness": {
    "chance": 0.05,
    "types": ["camp_fever"],
    "severity_weights": { "minor": 1.0 }
  },

  "resultTextId": "ll_story_corruption_ledger_skim_opt_refuse_result",
  "resultText": ""
}
```

Rules:
- `textId` is required (localization). `text` is fallback English.
- `hintId` is optional but recommended. `hint` is fallback English.
- `risk` must be one of: `safe`, `risky`, `corrupt`.
- All costs/rewards are **declarative**. No event-specific code paths.
- `effects` is optional and defaults to all-zero.
- `injury` / `illness` are optional and only apply when the player condition system is enabled (enabled by default; can be disabled via config).

Important:
- `costs.heat` and `costs.discipline` exist for forward compatibility but are **currently ignored by code**. Use `effects` for escalation tracks.

## Triggers & requirements (design contract)
Trigger vocabulary must stay consistent across packs:
- Campaign state: `entered_town`, `leaving_battle`, etc.
- Camp conditions: `logistics_high`, `morale_low`, `pay_tension_high`, `heat_high`.
- Escalation thresholds (Phase 4): `heat_3`, `heat_5`, `heat_7`, `heat_10`, `discipline_3`, `discipline_5`, `discipline_7`, `discipline_10`, `lance_rep_20`, `lance_rep_40`, `lance_rep_-20`, `lance_rep_-40`, `medical_3`, `medical_4`, `medical_5`.
- Player conditions (Phase 5): `has_injury`, `has_illness`, `has_condition`.
- Safety: enforced by code (no battle/encounter/prisoner), not by pack authors.

Pack authors should not invent new trigger names without first updating the Feature Specs (and implementing support).

Source of truth:
- Trigger tokens are defined in code: `src/Mod.Core/Triggers/CampaignTriggerTokens.cs`

## Localization contract (required)
All player-facing text must be translatable:
- Packs supply `*Id` fields (e.g., `titleId`, `textId`).
- Translations live in `ModuleData/Languages/enlisted_strings.xml`.
- Fallback English (`title`, `body`, `text`, `hint`) is allowed so missing translations never crash or blank the UI.

Placeholders are allowed in localized strings (examples):
- `{PLAYER_NAME}`, `{LORD_NAME}`, `{LANCE_NAME}`
- (Phase 5) Lance persona placeholders (examples): `{LANCE_LEADER_RANK}`, `{LANCE_LEADER_NAME}`, `{SECOND_RANK}`, `{SECOND_NAME}`

## Validation & fail-safe behavior (required)
On load, we validate:
- Duplicate story IDs across packs
- Missing required fields (`id`, `category`, `titleId`, `bodyId`, `options`, etc.)
- Invalid enum values (`risk`, unknown category if category lists are enforced)
- Invalid skill names in `skillXp`

Behavior:
- Invalid story entries are **skipped**.
- Pack load errors do **not** crash the campaign.
- A clear diagnostic log summarizes what was loaded and what was skipped.

## Enable/disable controls (required)
Config must support disabling without editing packs:
- Disable by **event ID**
- Disable by **category**
- Optional: disable by **packId**

## References
- `docs/Features/Gameplay/lance-life.md` (Feature Spec)
- `docs/Features/Core/implementation-roadmap.md` (Phased plan)
- `ModuleData/Languages/enlisted_strings.xml` (translations)

