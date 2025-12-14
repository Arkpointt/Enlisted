# Temporary Leave System

## Overview
Temporary Leave lets enlisted players suspend active service for a limited time, returning to normal (vanilla) world interaction while preserving enlistment progression for later return.

Leave is **time-limited**. If it expires, the player is treated as having abandoned their post and penalties apply.

## How it works

### Request Leave
- While enlisted, you can request leave via the enlisted UI / lord dialog (depending on availability).
- On approval, you enter a leave state:
  - Player party becomes visible/active again
  - Normal encounters and movement resume
  - Enlistment progression state is preserved

### Leave timer
- Default maximum leave: **14 days** (configurable).
- The UI warns you when leave is close to expiring.

### Return to service
- While on leave, you can return to service by speaking with your current/previous lord.
- Returning restores enlisted behavior: following, battle participation, and enlisted menus.

### Transfer service (while on leave)
- While on leave, you can transfer to a different lord **in the same faction**.
- Transfer preserves:
  - Tier and XP
  - Service date and term tracking
  - Kill tracking and service record continuity

### Leave expiration (desertion via leave)
If leave exceeds the maximum duration:
- Service is terminated as an abandonment.
- Relation penalties apply.
- Re-enlistment with the same faction may be blocked for a cooldown window.

## Leave vs Discharge vs Desert
- **Leave**: Temporary suspension; you must return before the timer expires.
- **Request Discharge (Final Muster)**: Managed separation requested from Camp ("Camp"); resolves at the next pay muster and can award severance/pension.
- **Desert the Army**: Immediate abandonment from the enlisted status menu with severe penalties.

## Related docs
- [Enlistment](../Core/enlistment.md)
- [Menu Interface](../UI/menu-interface.md)
- [Pay System](../Core/pay-system-rework.md)
