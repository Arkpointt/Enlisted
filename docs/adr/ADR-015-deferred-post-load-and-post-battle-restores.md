# ADR-015: Deferred Post-Load and Post-Battle Restore Operations

Status: Accepted
Date: 2025-08-31
Deciders: Enlisted Team

## Context

Applying escort/camera/tracker immediately during save-load and battle exit can collide with engine UI transitions and cause instability/asserts. We need a safe, deterministic way to reapply enlisted effects.

## Decision

- Defer post-load setup until there is no active menu or encounter, and a short safety timer elapses. Then apply:
  - Escort AI to commander (or army leader)
  - Visual tracker registration for the commander
  - Camera follow to commander party
- Defer post-battle restore similarly until menus/encounters clear. Then:
  - Re-hide/deactivate `MainParty`
  - Re-apply escort/camera
  - Optionally re-open the enlisted status menu if it was open before joining
- Emit structured Debug logs on defer/apply transitions to aid diagnostics.

## Consequences

Positive:
- Avoids asserts/race conditions during load and battle cleanup.
- Predictable reinstatement of the enlisted experience.

Negative:
- Small perceived delay before full effects re-apply (bounded by safety timer).

## Compliance

- Blueprint Sections: 4.7 (Deferred Operations).
- Code: `EnlistmentBehavior` tick loop and `ReapplyEnlistmentState()`.

## References

- Bannerlord API 1.2.12 `Campaign`, `GameMenu`, `PlayerEncounter`
- ADR-012 (Menu/Time control)


