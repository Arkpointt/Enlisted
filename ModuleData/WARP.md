# Content Rules - Enlisted

This WARP.md applies when working in ModuleData/. Root WARP.md still applies.

## JSON Field Ordering (CRITICAL)

Fallback fields MUST immediately follow their ID fields:

✅ **Correct:**

```json
{
  "titleId": "event_title_key",
  "title": "Fallback Title",
  "setupId": "event_setup_key",
  "setup": "Fallback setup text..."
}
```

❌ **Wrong (breaks localization):**

```json
{
  "titleId": "...", 
  "setupId": "...",
  "title": "...", 
  "setup": "..."
}
```

## Tooltips (REQUIRED)

* Every option MUST have a tooltip
* Under 80 characters
* Factual description of consequences
* Format: action + side effects + cooldown

**Example:**

```json
"tooltip": "Train with sergeant. +50 XP, -10 gold. 3-day cooldown."
```

## Order Events

* MUST include `effects.skillXp` — breaks progression otherwise
* Validator will fail if missing

## Validation Commands

```powershell

python Tools/Validation/validate_content.py
python Tools/Validation/sync_event_strings.py
```

## Full Reference

See `docs/Features/Content/writing-style-guide.md` for voice/tone.
See `docs/Features/Content/event-system-schemas.md` for schemas.
