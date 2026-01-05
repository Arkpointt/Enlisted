# Enlisted Error Codes

**Canonical Reference:** `docs/Features/Content/content-system-architecture.md#error-codes--diagnostics`

## Error Code Format

- `E-*-###` = Error (breaks functionality)
- `W-*-###` = Warning (degraded but functional)

## Orchestrator Codes (W-ORCH-*)

| Code | Meaning | User Action |
|------|---------|-------------|
| W-ORCH-001 | CampOpportunityGenerator not available | Decisions won't appear - check game loaded properly |
| W-ORCH-002 | ConsumeOpportunity called with null ID | Decision may not disappear from menu |
| W-ORCH-003 | Opportunity not found in schedule | Decision may reappear after phase change |
| W-ORCH-004 | No schedule for current phase | Check if orchestrator is active |
| W-ORCH-005 | Tomorrow scheduling returned 0 opportunities | Check budget/candidates |

## Camp Life Codes (W-CAMP-*, E-CAMP-*)

| Code | Meaning | User Action |
|------|---------|-------------|
| W-CAMP-001 | Definitions loaded via lazy init | Normal on first access |
| W-CAMP-002 | No definitions after loading | Check camp_opportunities.json |
| W-CAMP-003 | No candidates passed filtering | Check tier/context requirements |
| E-CAMP-001 | camp_opportunities.json not found | Verify mod installation |
| E-CAMP-002 | No 'opportunities' array in JSON | File corrupt - reinstall |
| E-CAMP-003 | Failed to parse JSON | Check JSON syntax |

## Event Delivery Codes (W-EVT-*, E-EVT-*)

| Code | Meaning | User Action |
|------|---------|-------------|
| W-EVT-001 | Attempted to queue null event | Check event ID and catalog |
| W-EVT-002 | Event has no valid options | Check requirements in JSON |
| E-EVT-001 | Selected option not an EventOption | Report with log |

## Log Locations

| Log Type | Location |
|----------|----------|
| Session logs | `Modules/Enlisted/Debugging/Session-A_*.log` (current) |
| Previous session | `Modules/Enlisted/Debugging/Session-B_*.log` |
| Conflicts | `Modules/Enlisted/Debugging/Conflicts-A_*.log` |
| Native crash | `%USERPROFILE%\Documents\Mount and Blade II Bannerlord\crashes\` |

## Log Levels (per-category)

Configurable in `ModuleData/Enlisted/Config/settings.json`:
```json
{
  "log_levels": {
    "default": "Info",
    "Orchestrator": "Debug"
  }
}
```

Levels: `Off` < `Error` < `Warn` < `Info` (default) < `Debug` < `Trace`
