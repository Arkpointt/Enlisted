# Event Validation Tools

Tools for validating Enlisted mod content (events, decisions, orders).

## Tools

### validate_content.py (Enhanced Validator)

Comprehensive validation tool that implements all rules from `docs/Features/Technical/conflict-detection-system.md`.

**Usage:**
```bash
# Standard validation
python tools/events/validate_content.py

# Strict mode (warnings block merge)
python tools/events/validate_content.py --strict

# From project root
cd C:\Dev\Enlisted\Enlisted
python tools/events/validate_content.py
```

**Validation Phases:**

1. **Structure Validation**
   - JSON schema correctness
   - Required fields present
   - Valid enum values (category, role, context)
   - Option counts (2-4 range)
   - Tooltip presence and length

2. **Reference Validation**
   - All `titleId`, `setupId`, `textId`, `resultTextId` exist in `enlisted_strings.xml`
   - All skill names match Bannerlord skills
   - All trait names valid

3. **Logical Validation**
   - No impossible tier × role combinations (e.g., T1 Officer)
   - Context requirements achievable for delivery mechanism
   - Skill requirements match role expectations
   - Escalation thresholds within valid ranges
   - Reasonable cooldown values
   - HP requirements only in medical events
   - Priority appropriate for event type

4. **Consistency Checks**
   - Flags referenced but never set
   - Flags set but never referenced (terminal flags)
   - Multi-stage event sequences
   - One-time events have appropriate priority

**Exit Codes:**
- `0` - Validation passed (may have warnings)
- `1` - Validation failed (critical errors found)
- `2` - Could not run (missing files, invalid arguments)

**Output Example:**
```
[ERROR] events_general.json:evt_test [logic] Impossible tier×role: role 'Officer' requires tier 5+, but minTier=1
[ERROR] events_general.json:evt_sample [structure] Option 'opt_1' missing tooltip (tooltips cannot be null)
[WARNING] decisions.json:dec_rest [reference] textId 'dec_rest_text' not found in enlisted_strings.xml
[WARNING] events_general.json:evt_flavor [structure] Option 'opt_2' tooltip exceeds 80 chars (95)

SUMMARY:
  Total Events: 234
  Errors: 1
  Warnings: 12
  Info: 5
```

**When to Run:**
- Before committing content changes
- After adding new events/decisions
- Before creating pull requests
- During content reviews

---

### validate_events.py (Legacy Validator)

Simple validator that checks:
- Duplicate event IDs
- Option counts (2-4 range)
- Missing IDs

**Status:** Legacy tool, use `validate_content.py` instead.

**Usage:**
```bash
python tools/events/validate_events.py
```

---

## Integration with Development Workflow

### Pre-Commit Hook

Add to `.git/hooks/pre-commit`:
```bash
#!/bin/sh
python tools/events/validate_content.py --strict
if [ $? -ne 0 ]; then
    echo "Content validation failed. Fix issues before committing."
    exit 1
fi
```

### CI/CD Integration

Add to GitHub Actions workflow:
```yaml
- name: Validate Content
  run: python tools/events/validate_content.py --strict
```

---

## Validation Rules Reference

See `docs/Features/Technical/conflict-detection-system.md` for complete rule documentation:

**Critical Rules (Block Merge):**
- Duplicate event IDs
- Invalid JSON structure
- Missing required fields
- Impossible tier × role combinations
- Invalid skill/trait references

**High Priority (Fix Before Release):**
- Missing localization references
- Broken cooldowns
- Invalid context for delivery mechanism

**Medium Priority (Fix Soon):**
- Overlapping event requirements
- Suspicious skill checks
- Priority mismatches

**Low Priority (Polish):**
- Tooltip improvements
- Consistency enhancements
- Rare edge cases

---

## Adding New Validation Rules

To add a new validation rule:

1. **Document the rule** in `docs/Features/Technical/conflict-detection-system.md`
2. **Add validation logic** to appropriate phase in `validate_content.py`:
   - Phase 1: Structure issues (JSON, required fields)
   - Phase 2: Reference issues (missing strings, invalid names)
   - Phase 3: Logic issues (impossible combinations)
   - Phase 4: Consistency issues (cross-event relationships)
3. **Add test cases** covering valid and invalid examples
4. **Update this README** with the new rule

---

## Common Issues and Fixes

### Missing Localization Strings

**Issue:**
```
[WARNING] textId 'dec_new_decision_text' not found in enlisted_strings.xml
```

**Fix:**
Add entry to `ModuleData/Languages/enlisted_strings.xml`:
```xml
<string id="dec_new_decision_text" text="Your fallback text here" />
```

---

### Impossible Tier × Role

**Issue:**
```
[ERROR] role 'Officer' requires tier 5+, but minTier=1
```

**Fix:**
Update requirements in event JSON:
```json
{
  "requirements": {
    "tier": { "min": 5 },
    "role": "Officer"
  }
}
```

---

### Invalid Skill Name

**Issue:**
```
[ERROR] Invalid skill name: 'Swordsmanship'
```

**Fix:**
Use correct Bannerlord skill name:
```json
{
  "requirements": {
    "minSkills": {
      "OneHanded": 50
    }
  }
}
```

Valid skills: `OneHanded`, `TwoHanded`, `Polearm`, `Bow`, `Crossbow`, `Throwing`, `Riding`, `Athletics`, `Crafting`, `Scouting`, `Tactics`, `Roguery`, `Charm`, `Leadership`, `Trade`, `Stewardship`, `Medicine`, `Engineering`

---

### Flag Never Set

**Issue:**
```
[WARNING] Flag 'mutiny_joined' referenced but never set
```

**Fix:**
Add flag setter in event option:
```json
{
  "options": [
    {
      "id": "join_mutiny",
      "effects": {
        "setFlags": ["mutiny_joined"]
      }
    }
  ]
}
```

---

## Related Documentation

- [Conflict Detection System](../../docs/Features/Technical/conflict-detection-system.md) - Complete validation rules
- [Event System Schemas](../../docs/Features/Content/event-system-schemas.md) - JSON field definitions
- [Content System Architecture](../../docs/Features/Content/content-system-architecture.md) - How content delivery works
- [Event Catalog](../../docs/Content/event-catalog-by-system.md) - All existing content

---

## Future Enhancements

Potential additions to validation tool:

- [ ] Detect events with identical requirements (potential duplicates)
- [ ] Validate placeholder variable usage (`{PLAYER_NAME}`, `{LORD_NAME}`, etc.)
- [ ] Check for balanced reputation changes (positive/negative options)
- [ ] Validate gold costs vs tier income expectations
- [ ] Detect orphaned follow-up events (flag set but follow-up missing)
- [ ] Validate time_hours costs are reasonable
- [ ] Check skill XP rewards align with difficulty
- [ ] Validate formation requirements match content
- [ ] Detect contradictory trigger conditions
- [ ] Generate content coverage reports (which tiers/roles have events)

